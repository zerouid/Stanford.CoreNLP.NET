using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Lang.Reflect;
using Java.Util;
using Java.Util.Concurrent;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Maximizes the conditional likelihood with a given prior.</summary>
	/// <author>Dan Klein</author>
	/// <author>Galen Andrew</author>
	/// <author>Chris Cox (merged w/ SumConditionalObjectiveFunction, 2/16/05)</author>
	/// <author>
	/// Sarah Spikes (Templatization, allowing an
	/// <c>Iterable&lt;Datum&lt;L, F&gt;&gt;</c>
	/// to be passed in instead of a
	/// <c>GeneralDataset&lt;L, F&gt;</c>
	/// )
	/// </author>
	/// <author>Angel Chang (support in place SGD - extend AbstractStochasticCachingDiffUpdateFunction)</author>
	/// <author>Christopher Manning (cleaned out the cruft and sped it up in 2014)</author>
	/// <author>Keenon Werling added some multithreading to the batch evaluations</author>
	public class LogConditionalObjectiveFunction<L, F> : AbstractStochasticCachingDiffUpdateFunction
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.LogConditionalObjectiveFunction));

		protected internal readonly LogPrior prior;

		protected internal readonly int numFeatures;

		protected internal readonly int numClasses;

		/// <summary>Normally, this contains the data.</summary>
		/// <remarks>
		/// Normally, this contains the data. The first index is the datum number,
		/// and then there is an array of feature indices for each datum.
		/// </remarks>
		protected internal readonly int[][] data;

		/// <summary>
		/// Alternatively, the data may be available from an Iterable in not yet
		/// indexed form.
		/// </summary>
		/// <remarks>
		/// Alternatively, the data may be available from an Iterable in not yet
		/// indexed form.  (In 2014, it's not clear any code actually uses this option.)
		/// And then you need an index for both.
		/// </remarks>
		protected internal readonly IEnumerable<IDatum<L, F>> dataIterable;

		protected internal readonly IIndex<L> labelIndex;

		protected internal readonly IIndex<F> featureIndex;

		/// <summary>Same size as data if the features have values; null if the features are binary.</summary>
		protected internal readonly double[][] values;

		/// <summary>The label of each data index.</summary>
		protected internal readonly int[] labels;

		protected internal readonly float[] dataWeights;

		protected internal readonly bool useSummedConditionalLikelihood;

		/// <summary>This is used to cache the numerator in batch methods.</summary>
		protected internal double[] derivativeNumerator = null;

		/// <summary>The only reason this is around is because the Prior Functions don't handle stochastic calculations yet.</summary>
		protected internal double[] priorDerivative = null;

		/// <summary>The flag to tell the gradient computations to multithread over the data.</summary>
		/// <remarks>
		/// The flag to tell the gradient computations to multithread over the data.
		/// keenon (june 2015): On my machine,
		/// </remarks>
		protected internal bool parallelGradientCalculation = true;

		/// <summary>Multithreading gradient calculations is a bit cheaper if you reuse the threads.</summary>
		protected internal int threads = ArgumentParser.threads;

		//whether to use sumConditional or logConditional
		public override int DomainDimension()
		{
			return numFeatures * numClasses;
		}

		public override int DataDimension()
		{
			return data.Length;
		}

		private int ClassOf(int index)
		{
			return index % numClasses;
		}

		private int FeatureOf(int index)
		{
			return index / numClasses;
		}

		/// <summary>Converts a Phi feature number and class index into an f(x,y) feature index.</summary>
		protected internal virtual int IndexOf(int f, int c)
		{
			// [cdm2014: Tried inline this; no big gains.]
			return f * numClasses + c;
		}

		public virtual double[][] To2D(double[] x)
		{
			double[][] x2 = new double[numFeatures][];
			for (int i = 0; i < numFeatures; i++)
			{
				for (int j = 0; j < numClasses; j++)
				{
					x2[i][j] = x[IndexOf(i, j)];
				}
			}
			return x2;
		}

		/// <summary>Calculate the conditional likelihood.</summary>
		/// <remarks>
		/// Calculate the conditional likelihood.
		/// If
		/// <c>useSummedConditionalLikelihood</c>
		/// is
		/// <see langword="false"/>
		/// (the default),
		/// this calculates standard(product) CL, otherwise this calculates summed CL.
		/// What's the difference?  See Klein and Manning's 2002 EMNLP paper.
		/// </remarks>
		protected internal override void Calculate(double[] x)
		{
			//If the batchSize is 0 then use the regular calculate methods
			if (useSummedConditionalLikelihood)
			{
				CalculateSCL(x);
			}
			else
			{
				CalculateCL(x);
			}
		}

		/// <summary>
		/// This function is used to come up with an estimate of the value / gradient based on only a small
		/// portion of the data (referred to as the batchSize for lack of a better term.
		/// </summary>
		/// <remarks>
		/// This function is used to come up with an estimate of the value / gradient based on only a small
		/// portion of the data (referred to as the batchSize for lack of a better term.  In this case batch does
		/// not mean All!!  It should be thought of in the sense of "a small batch of the data".
		/// </remarks>
		public override void CalculateStochastic(double[] x, double[] v, int[] batch)
		{
			if (method.CalculatesHessianVectorProduct() && v != null)
			{
				//  This is used for Stochastic Methods that involve second order information (SMD for example)
				if (method.Equals(StochasticCalculateMethods.AlgorithmicDifferentiation))
				{
					CalculateStochasticAlgorithmicDifferentiation(x, v, batch);
				}
				else
				{
					if (method.Equals(StochasticCalculateMethods.IncorporatedFiniteDifference))
					{
						CalculateStochasticFiniteDifference(x, v, finiteDifferenceStepSize, batch);
					}
				}
			}
			else
			{
				//This is used for Stochastic Methods that don't need anything but the gradient (SGD)
				CalculateStochasticGradientLocal(x, batch);
			}
		}

		/// <summary>
		/// Calculate the summed conditional likelihood of this data by summing
		/// conditional estimates.
		/// </summary>
		private void CalculateSCL(double[] x)
		{
			//System.out.println("Checking at: "+x[0]+" "+x[1]+" "+x[2]);
			value = 0.0;
			Arrays.Fill(derivative, 0.0);
			double[] sums = new double[numClasses];
			double[] probs = new double[numClasses];
			// double[] counts = new double[numClasses];
			// Arrays.fill(counts, 0.0); // not needed; Java arrays zero initialized
			for (int d = 0; d < data.Length; d++)
			{
				int[] features = data[d];
				// activation
				Arrays.Fill(sums, 0.0);
				for (int c = 0; c < numClasses; c++)
				{
					foreach (int feature in features)
					{
						int i = IndexOf(feature, c);
						sums[c] += x[i];
					}
				}
				// expectation (slower routine replaced by fast way)
				// double total = Double.NEGATIVE_INFINITY;
				// for (int c=0; c<numClasses; c++) {
				//   total = SloppyMath.logAdd(total, sums[c]);
				// }
				double total = ArrayMath.LogSum(sums);
				int ld = labels[d];
				for (int c_1 = 0; c_1 < numClasses; c_1++)
				{
					probs[c_1] = System.Math.Exp(sums[c_1] - total);
					foreach (int feature in features)
					{
						int i = IndexOf(feature, c_1);
						derivative[i] += probs[ld] * probs[c_1];
					}
				}
				// observed
				foreach (int feature_1 in features)
				{
					int i = IndexOf(feature_1, labels[d]);
					derivative[i] -= probs[ld];
				}
				value -= probs[ld];
			}
			// priors
			if (true)
			{
				for (int i = 0; i < x.Length; i++)
				{
					double k = 1.0;
					double w = x[i];
					value += k * w * w / 2.0;
					derivative[i] += k * w;
				}
			}
		}

		/// <summary>
		/// Calculate the conditional likelihood of this data by multiplying
		/// conditional estimates.
		/// </summary>
		/// <remarks>
		/// Calculate the conditional likelihood of this data by multiplying
		/// conditional estimates. Full dataset batch estimation.
		/// </remarks>
		private void CalculateCL(double[] x)
		{
			if (values != null)
			{
				Rvfcalculate(x);
			}
			else
			{
				if (dataIterable != null)
				{
					CalculateCLiterable(x);
				}
				else
				{
					CalculateCLbatch(x);
				}
			}
		}

		private class CLBatchDerivativeCalculation : IRunnable
		{
			internal int numThreads;

			internal int threadIdx;

			internal double localValue = 0.0;

			internal double[] x;

			internal int[] batch;

			internal double[] localDerivative;

			internal CountDownLatch latch;

			public CLBatchDerivativeCalculation(LogConditionalObjectiveFunction<L, F> _enclosing, int numThreads, int threadIdx, int[] batch, double[] x, int derivativeSize, CountDownLatch latch)
			{
				this._enclosing = _enclosing;
				this.numThreads = numThreads;
				this.threadIdx = threadIdx;
				this.x = x;
				this.batch = batch;
				this.localDerivative = new double[derivativeSize];
				this.latch = latch;
			}

			public virtual void Run()
			{
				double[] sums = new double[this._enclosing.numClasses];
				double[] probs = new double[this._enclosing.numClasses];
				// TODO: could probably get slightly better speedup if threads took linear subsequences, for cacheing
				int batchSize = this.batch == null ? this._enclosing.data.Length : this.batch.Length;
				for (int m = this.threadIdx; m < batchSize; m += this.numThreads)
				{
					int d = this.batch == null ? m : this.batch[m];
					// activation
					Arrays.Fill(sums, 0.0);
					int[] featuresArr = this._enclosing.data[d];
					for (int c = 0; c < this._enclosing.numClasses; c++)
					{
						foreach (int feature in featuresArr)
						{
							int i = this._enclosing.IndexOf(feature, c);
							sums[c] += this.x[i];
						}
					}
					// expectation (slower routine replaced by fast way)
					// double total = Double.NEGATIVE_INFINITY;
					// for (int c=0; c<numClasses; c++) {
					//   total = SloppyMath.logAdd(total, sums[c]);
					// }
					double total = ArrayMath.LogSum(sums);
					for (int c_1 = 0; c_1 < this._enclosing.numClasses; c_1++)
					{
						probs[c_1] = System.Math.Exp(sums[c_1] - total);
						if (this._enclosing.dataWeights != null)
						{
							probs[c_1] *= this._enclosing.dataWeights[d];
						}
					}
					for (int c_2 = 0; c_2 < this._enclosing.numClasses; c_2++)
					{
						foreach (int feature in featuresArr)
						{
							int i = this._enclosing.IndexOf(feature, c_2);
							this.localDerivative[i] += probs[c_2];
						}
					}
					int labelindex = this._enclosing.labels[d];
					double dV = sums[labelindex] - total;
					if (this._enclosing.dataWeights != null)
					{
						dV *= this._enclosing.dataWeights[d];
					}
					this.localValue -= dV;
				}
				this.latch.CountDown();
			}

			private readonly LogConditionalObjectiveFunction<L, F> _enclosing;
		}

		private void CalculateCLbatch(double[] x)
		{
			//System.out.println("Checking at: "+x[0]+" "+x[1]+" "+x[2]);
			value = 0.0;
			// [cdm Mar 2014] This next bit seems unnecessary: derivative is allocated by ensure() in AbstractCachingDiffFunction
			// before calculate() is called; and after the next block, derivativeNumerator is copied into it.
			// if (derivative == null) {
			//   derivative = new double[x.length];
			// } else {
			//   Arrays.fill(derivative, 0.0);
			// }
			if (derivativeNumerator == null)
			{
				derivativeNumerator = new double[x.Length];
				for (int d = 0; d < data.Length; d++)
				{
					int[] features = data[d];
					foreach (int feature in features)
					{
						int i = IndexOf(feature, labels[d]);
						if (dataWeights == null)
						{
							derivativeNumerator[i] -= 1;
						}
						else
						{
							derivativeNumerator[i] -= dataWeights[d];
						}
					}
				}
			}
			Copy(derivative, derivativeNumerator);
			//    Arrays.fill(derivative, 0.0);
			//    double[] counts = new double[numClasses];
			//    Arrays.fill(counts, 0.0);
			if (parallelGradientCalculation && threads > 1)
			{
				// Launch several threads (reused out of our fixed pool) to handle the computation
				LogConditionalObjectiveFunction.CLBatchDerivativeCalculation[] runnables = (LogConditionalObjectiveFunction.CLBatchDerivativeCalculation[])System.Array.CreateInstance(typeof(LogConditionalObjectiveFunction.CLBatchDerivativeCalculation), threads
					);
				CountDownLatch latch = new CountDownLatch(threads);
				for (int i = 0; i < threads; i++)
				{
					runnables[i] = new LogConditionalObjectiveFunction.CLBatchDerivativeCalculation(this, threads, i, null, x, derivative.Length, latch);
					new Thread(runnables[i]).Start();
				}
				try
				{
					latch.Await();
				}
				catch (Exception e)
				{
					throw new RuntimeInterruptedException(e);
				}
				for (int i_1 = 0; i_1 < threads; i_1++)
				{
					value += runnables[i_1].localValue;
					for (int j = 0; j < derivative.Length; j++)
					{
						derivative[j] += runnables[i_1].localDerivative[j];
					}
				}
			}
			else
			{
				double[] sums = new double[numClasses];
				double[] probs = new double[numClasses];
				for (int d = 0; d < data.Length; d++)
				{
					// activation
					Arrays.Fill(sums, 0.0);
					int[] featuresArr = data[d];
					foreach (int feature in featuresArr)
					{
						for (int c = 0; c < numClasses; c++)
						{
							int i = IndexOf(feature, c);
							sums[c] += x[i];
						}
					}
					// expectation (slower routine replaced by fast way)
					// double total = Double.NEGATIVE_INFINITY;
					// for (int c=0; c<numClasses; c++) {
					//   total = SloppyMath.logAdd(total, sums[c]);
					// }
					double total = ArrayMath.LogSum(sums);
					for (int c_1 = 0; c_1 < numClasses; c_1++)
					{
						probs[c_1] = System.Math.Exp(sums[c_1] - total);
						if (dataWeights != null)
						{
							probs[c_1] *= dataWeights[d];
						}
					}
					foreach (int feature_1 in featuresArr)
					{
						for (int c = 0; c_1 < numClasses; c_1++)
						{
							int i = IndexOf(feature_1, c_1);
							derivative[i] += probs[c_1];
						}
					}
					int labelindex = labels[d];
					double dV = sums[labelindex] - total;
					if (dataWeights != null)
					{
						dV *= dataWeights[d];
					}
					value -= dV;
				}
			}
			value += prior.Compute(x, derivative);
		}

		private void CalculateCLiterable(double[] x)
		{
			//System.out.println("Checking at: "+x[0]+" "+x[1]+" "+x[2]);
			value = 0.0;
			// [cdm Mar 2014] This next bit seems unnecessary: derivative is allocated by ensure() in AbstractCachingDiffFunction
			// before calculate() is called; and after the next block, derivativeNumerator is copied into it.
			// if (derivative == null) {
			//   derivative = new double[x.length];
			// } else {
			//   Arrays.fill(derivative, 0.0);
			// }
			if (derivativeNumerator == null)
			{
				derivativeNumerator = new double[x.Length];
				//use dataIterable if data is null & vice versa
				//TODO: Make sure this work as expected!!
				//int index = 0;
				foreach (IDatum<L, F> datum in dataIterable)
				{
					ICollection<F> features = datum.AsFeatures();
					foreach (F feature in features)
					{
						int i = IndexOf(featureIndex.IndexOf(feature), labelIndex.IndexOf(datum.Label()));
						if (dataWeights == null)
						{
							derivativeNumerator[i] -= 1;
						}
					}
				}
			}
			/*else {
			derivativeNumerator[i] -= dataWeights[index];
			}*/
			Copy(derivative, derivativeNumerator);
			//    Arrays.fill(derivative, 0.0);
			double[] sums = new double[numClasses];
			double[] probs = new double[numClasses];
			//    double[] counts = new double[numClasses];
			//    Arrays.fill(counts, 0.0);
			foreach (IDatum<L, F> datum_1 in dataIterable)
			{
				// activation
				Arrays.Fill(sums, 0.0);
				ICollection<F> features = datum_1.AsFeatures();
				foreach (F feature in features)
				{
					for (int c = 0; c < numClasses; c++)
					{
						int i = IndexOf(featureIndex.IndexOf(feature), c);
						sums[c] += x[i];
					}
				}
				// expectation (slower routine replaced by fast way)
				// double total = Double.NEGATIVE_INFINITY;
				// for (int c=0; c<numClasses; c++) {
				//   total = SloppyMath.logAdd(total, sums[c]);
				// }
				double total = ArrayMath.LogSum(sums);
				for (int c_1 = 0; c_1 < numClasses; c_1++)
				{
					probs[c_1] = System.Math.Exp(sums[c_1] - total);
				}
				foreach (F feature_1 in features)
				{
					for (int c = 0; c_1 < numClasses; c_1++)
					{
						int i = IndexOf(featureIndex.IndexOf(feature_1), c_1);
						derivative[i] += probs[c_1];
					}
				}
				int label = this.labelIndex.IndexOf(datum_1.Label());
				double dV = sums[label] - total;
				value -= dV;
			}
			value += prior.Compute(x, derivative);
		}

		public virtual void CalculateStochasticFiniteDifference(double[] x, double[] v, double h, int[] batch)
		{
			//  THOUGHTS:
			//  does applying the renormalization (g(x+hv)-g(x)) / h at each step along the way
			//  introduce too much error to makes this method numerically accurate?
			//  akleeman Feb 23 2007
			//  Answer to my own question:     Feb 25th
			//      Doesn't look like it!!  With h = 1e-4 it seems like the Finite Difference makes almost
			//     exactly the same step as the exact hessian vector product calculated through AD.
			//     That said it's probably (in the case of the Log Conditional Objective function) logical
			//     to only use finite difference.  Unless of course the function is somehow nearly singular,
			//     in which case finite difference could turn what is a convex problem into a singular proble... NOT GOOD.
			if (values != null)
			{
				Rvfcalculate(x);
				return;
			}
			value = 0.0;
			if (priorDerivative == null)
			{
				priorDerivative = new double[x.Length];
			}
			double priorFactor = batch.Length / (data.Length * prior.GetSigma() * prior.GetSigma());
			derivative = ArrayMath.Multiply(x, priorFactor);
			HdotV = ArrayMath.Multiply(v, priorFactor);
			//Arrays.fill(derivative, 0.0);
			double[] sums = new double[numClasses];
			double[] sumsV = new double[numClasses];
			double[] probs = new double[numClasses];
			double[] probsV = new double[numClasses];
			foreach (int m in batch)
			{
				//Sets the index based on the current batch
				int[] features = data[m];
				// activation
				Arrays.Fill(sums, 0.0);
				Arrays.Fill(sumsV, 0.0);
				for (int c = 0; c < numClasses; c++)
				{
					foreach (int feature in features)
					{
						int i = IndexOf(feature, c);
						sums[c] += x[i];
						sumsV[c] += x[i] + h * v[i];
					}
				}
				double total = ArrayMath.LogSum(sums);
				double totalV = ArrayMath.LogSum(sumsV);
				for (int c_1 = 0; c_1 < numClasses; c_1++)
				{
					probs[c_1] = System.Math.Exp(sums[c_1] - total);
					probsV[c_1] = System.Math.Exp(sumsV[c_1] - totalV);
					if (dataWeights != null)
					{
						probs[c_1] *= dataWeights[m];
						probsV[c_1] *= dataWeights[m];
					}
					foreach (int feature in features)
					{
						int i = IndexOf(feature, c_1);
						//derivative[i] += (-1);
						derivative[i] += probs[c_1];
						HdotV[i] += (probsV[c_1] - probs[c_1]) / h;
						if (c_1 == labels[m])
						{
							derivative[i] -= 1;
						}
					}
				}
				double dV = sums[labels[m]] - total;
				if (dataWeights != null)
				{
					dV *= dataWeights[m];
				}
				value -= dV;
			}
			//Why was this being copied?  -akleeman
			//double[] tmpDeriv = new double[derivative.length];
			//System.arraycopy(derivative,0,tmpDeriv,0,derivative.length);
			value += ((double)batch.Length) / ((double)data.Length) * prior.Compute(x, priorDerivative);
		}

		public virtual void CalculateStochasticGradientLocal(double[] x, int[] batch)
		{
			if (values != null)
			{
				Rvfcalculate(x);
				return;
			}
			value = 0.0;
			int batchSize = batch.Length;
			if (priorDerivative == null)
			{
				priorDerivative = new double[x.Length];
			}
			double priorFactor = batchSize / (data.Length * prior.GetSigma() * prior.GetSigma());
			derivative = ArrayMath.Multiply(x, priorFactor);
			//Arrays.fill(derivative, 0.0);
			double[] sums = new double[numClasses];
			//double[] sumsV = new double[numClasses];
			double[] probs = new double[numClasses];
			//double[] probsV = new double[numClasses];
			foreach (int m in batch)
			{
				//Sets the index based on the current batch
				int[] features = data[m];
				// activation
				Arrays.Fill(sums, 0.0);
				//Arrays.fill(sumsV,0.0);
				for (int c = 0; c < numClasses; c++)
				{
					foreach (int feature in features)
					{
						int i = IndexOf(feature, c);
						sums[c] += x[i];
					}
				}
				double total = ArrayMath.LogSum(sums);
				//double totalV = ArrayMath.logSum(sumsV);
				for (int c_1 = 0; c_1 < numClasses; c_1++)
				{
					probs[c_1] = System.Math.Exp(sums[c_1] - total);
					//probsV[c] = Math.exp(sumsV[c]- totalV);
					if (dataWeights != null)
					{
						probs[c_1] *= dataWeights[m];
					}
					//probsV[c] *= dataWeights[m];
					foreach (int feature in features)
					{
						int i = IndexOf(feature, c_1);
						//derivative[i] += (-1);
						derivative[i] += probs[c_1];
						if (c_1 == labels[m])
						{
							derivative[i] -= 1;
						}
					}
				}
				double dV = sums[labels[m]] - total;
				if (dataWeights != null)
				{
					dV *= dataWeights[m];
				}
				value -= dV;
			}
			value += ((double)batchSize) / ((double)data.Length) * prior.Compute(x, priorDerivative);
		}

		public override double ValueAt(double[] x, double xscale, int[] batch)
		{
			value = 0.0;
			double[] sums = new double[numClasses];
			foreach (int m in batch)
			{
				//Sets the index based on the current batch
				int[] features = data[m];
				Arrays.Fill(sums, 0.0);
				for (int c = 0; c < numClasses; c++)
				{
					for (int f = 0; f < features.Length; f++)
					{
						int i = IndexOf(features[f], c);
						if (values != null)
						{
							sums[c] += x[i] * xscale * values[m][f];
						}
						else
						{
							sums[c] += x[i] * xscale;
						}
					}
				}
				double total = ArrayMath.LogSum(sums);
				double dV = sums[labels[m]] - total;
				if (dataWeights != null)
				{
					dV *= dataWeights[m];
				}
				value -= dV;
			}
			return value;
		}

		public override double CalculateStochasticUpdate(double[] x, double xscale, int[] batch, double gain)
		{
			value = 0.0;
			// Double check that we don't have a mismatch between parallel and batch size settings
			if (parallelGradientCalculation && threads > 1)
			{
				int examplesPerProcessor = 50;
				if (batch.Length <= Runtime.GetRuntime().AvailableProcessors() * examplesPerProcessor)
				{
					log.Info("\n\n***************");
					log.Info("CONFIGURATION ERROR: YOUR BATCH SIZE DOESN'T MEET PARALLEL MINIMUM SIZE FOR PERFORMANCE");
					log.Info("Batch size: " + batch.Length);
					log.Info("CPUS: " + Runtime.GetRuntime().AvailableProcessors());
					log.Info("Minimum batch size per CPU: " + examplesPerProcessor);
					log.Info("MINIMIM BATCH SIZE ON THIS MACHINE: " + (Runtime.GetRuntime().AvailableProcessors() * examplesPerProcessor));
					log.Info("TURNING OFF PARALLEL GRADIENT COMPUTATION");
					log.Info("***************\n");
					parallelGradientCalculation = false;
				}
			}
			if (parallelGradientCalculation && threads > 1)
			{
				// Launch several threads (reused out of our fixed pool) to handle the computation
				LogConditionalObjectiveFunction.CLBatchDerivativeCalculation[] runnables = (LogConditionalObjectiveFunction.CLBatchDerivativeCalculation[])System.Array.CreateInstance(typeof(LogConditionalObjectiveFunction.CLBatchDerivativeCalculation), threads
					);
				CountDownLatch latch = new CountDownLatch(threads);
				for (int i = 0; i < threads; i++)
				{
					runnables[i] = new LogConditionalObjectiveFunction.CLBatchDerivativeCalculation(this, threads, i, batch, x, x.Length, latch);
					new Thread(runnables[i]).Start();
				}
				try
				{
					latch.Await();
				}
				catch (Exception e)
				{
					throw new RuntimeInterruptedException(e);
				}
				for (int i_1 = 0; i_1 < threads; i_1++)
				{
					value += runnables[i_1].localValue;
					for (int j = 0; j < x.Length; j++)
					{
						x[j] += runnables[i_1].localDerivative[j] * xscale * gain;
					}
				}
			}
			else
			{
				double[] sums = new double[numClasses];
				double[] probs = new double[numClasses];
				foreach (int m in batch)
				{
					// Sets the index based on the current batch
					int[] features = data[m];
					// activation
					Arrays.Fill(sums, 0.0);
					for (int c = 0; c < numClasses; c++)
					{
						for (int f = 0; f < features.Length; f++)
						{
							int i = IndexOf(features[f], c);
							if (values != null)
							{
								sums[c] += x[i] * xscale * values[m][f];
							}
							else
							{
								sums[c] += x[i] * xscale;
							}
						}
					}
					for (int f_1 = 0; f_1 < features.Length; f_1++)
					{
						int i = IndexOf(features[f_1], labels[m]);
						double v = (values != null) ? values[m][f_1] : 1;
						double delta = (dataWeights != null) ? dataWeights[m] * v : v;
						x[i] += delta * gain;
					}
					double total = ArrayMath.LogSum(sums);
					for (int c_1 = 0; c_1 < numClasses; c_1++)
					{
						probs[c_1] = System.Math.Exp(sums[c_1] - total);
						if (dataWeights != null)
						{
							probs[c_1] *= dataWeights[m];
						}
						for (int f = 0; f_1 < features.Length; f_1++)
						{
							int i = IndexOf(features[f_1], c_1);
							double v = (values != null) ? values[m][f_1] : 1;
							double delta = probs[c_1] * v;
							x[i] -= delta * gain;
						}
					}
					double dV = sums[labels[m]] - total;
					if (dataWeights != null)
					{
						dV *= dataWeights[m];
					}
					value -= dV;
				}
			}
			return value;
		}

		public override void CalculateStochasticGradient(double[] x, int[] batch)
		{
			if (derivative == null)
			{
				derivative = new double[DomainDimension()];
			}
			Arrays.Fill(derivative, 0.0);
			double[] sums = new double[numClasses];
			double[] probs = new double[numClasses];
			//double[] counts = new double[numClasses];
			// Arrays.fill(counts, 0.0); // not needed; Java arrays zero initialized
			foreach (int d in batch)
			{
				//Sets the index based on the current batch
				int[] features = data[d];
				// activation
				Arrays.Fill(sums, 0.0);
				for (int c = 0; c < numClasses; c++)
				{
					foreach (int feature in features)
					{
						int i = IndexOf(feature, c);
						sums[c] += x[i];
					}
				}
				// expectation (slower routine replaced by fast way)
				// double total = Double.NEGATIVE_INFINITY;
				// for (int c=0; c<numClasses; c++) {
				//   total = SloppyMath.logAdd(total, sums[c]);
				// }
				double total = ArrayMath.LogSum(sums);
				int ld = labels[d];
				for (int c_1 = 0; c_1 < numClasses; c_1++)
				{
					probs[c_1] = System.Math.Exp(sums[c_1] - total);
					foreach (int feature in features)
					{
						int i = IndexOf(feature, c_1);
						derivative[i] += probs[ld] * probs[c_1];
					}
				}
				// observed
				foreach (int feature_1 in features)
				{
					int i = IndexOf(feature_1, labels[d]);
					derivative[i] -= probs[ld];
				}
			}
		}

		protected internal virtual void CalculateStochasticAlgorithmicDifferentiation(double[] x, double[] v, int[] batch)
		{
			log.Info("*");
			//Initialize
			value = 0.0;
			//initialize any variables
			DoubleAD[] derivativeAD = new DoubleAD[x.Length];
			for (int i = 0; i < x.Length; i++)
			{
				derivativeAD[i] = new DoubleAD(0.0, 0.0);
			}
			DoubleAD[] xAD = new DoubleAD[x.Length];
			for (int i_1 = 0; i_1 < x.Length; i_1++)
			{
				xAD[i_1] = new DoubleAD(x[i_1], v[i_1]);
			}
			// Initialize the sums
			DoubleAD[] sums = new DoubleAD[numClasses];
			for (int c = 0; c < numClasses; c++)
			{
				sums[c] = new DoubleAD(0, 0);
			}
			DoubleAD[] probs = new DoubleAD[numClasses];
			for (int c_1 = 0; c_1 < numClasses; c_1++)
			{
				probs[c_1] = new DoubleAD(0, 0);
			}
			//long curTime = System.currentTimeMillis();
			// Copy the Derivative numerator, and set up the vector V to be used for Hess*V
			for (int i_2 = 0; i_2 < x.Length; i_2++)
			{
				xAD[i_2].Set(x[i_2], v[i_2]);
				derivativeAD[i_2].Set(0.0, 0.0);
			}
			//log.info(System.currentTimeMillis() - curTime + " - ");
			//curTime = System.currentTimeMillis();
			for (int d = 0; d < batch.Length; d++)
			{
				//Sets the index based on the current batch
				int m = (curElement + d) % data.Length;
				int[] features = data[m];
				for (int c_2 = 0; c_2 < numClasses; c_2++)
				{
					sums[c_2].Set(0.0, 0.0);
				}
				for (int c_3 = 0; c_3 < numClasses; c_3++)
				{
					foreach (int feature in features)
					{
						int i_3 = IndexOf(feature, c_3);
						sums[c_3] = ADMath.Plus(sums[c_3], xAD[i_3]);
					}
				}
				DoubleAD total = ADMath.LogSum(sums);
				for (int c_4 = 0; c_4 < numClasses; c_4++)
				{
					probs[c_4] = ADMath.Exp(ADMath.Minus(sums[c_4], total));
					if (dataWeights != null)
					{
						probs[c_4] = ADMath.MultConst(probs[c_4], dataWeights[d]);
					}
					foreach (int feature in features)
					{
						int i_3 = IndexOf(feature, c_4);
						if (c_4 == labels[m])
						{
							derivativeAD[i_3].PlusEqualsConst(-1.0);
						}
						derivativeAD[i_3].PlusEquals(probs[c_4]);
					}
				}
				double dV = sums[labels[m]].Getval() - total.Getval();
				if (dataWeights != null)
				{
					dV *= dataWeights[d];
				}
				value -= dV;
			}
			// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			// DANGEROUS!!!!!!! Divide by Zero possible!!!!!!!!!!
			// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			// Need to modify the prior class to handle AD  -akleeman
			//log.info(System.currentTimeMillis() - curTime + " - ");
			//curTime = System.currentTimeMillis();
			double[] tmp = new double[x.Length];
			for (int i_4 = 0; i_4 < x.Length; i_4++)
			{
				tmp[i_4] = derivativeAD[i_4].Getval();
				derivativeAD[i_4].PlusEquals(ADMath.MultConst(xAD[i_4], batch.Length / (data.Length * prior.GetSigma() * prior.GetSigma())));
				derivative[i_4] = derivativeAD[i_4].Getval();
				HdotV[i_4] = derivativeAD[i_4].Getdot();
			}
			value += ((double)batch.Length) / ((double)data.Length) * prior.Compute(x, tmp);
		}

		private class RVFDerivativeCalculation : IRunnable
		{
			internal int numThreads;

			internal int threadIdx;

			internal double localValue = 0.0;

			internal double[] x;

			internal double[] localDerivative;

			internal CountDownLatch latch;

			public RVFDerivativeCalculation(LogConditionalObjectiveFunction<L, F> _enclosing, int numThreads, int threadIdx, double[] x, int derivativeSize, CountDownLatch latch)
			{
				this._enclosing = _enclosing;
				//log.info(System.currentTimeMillis() - curTime + " - ");
				//log.info("");
				this.numThreads = numThreads;
				this.threadIdx = threadIdx;
				this.x = x;
				this.localDerivative = new double[derivativeSize];
				this.latch = latch;
			}

			public virtual void Run()
			{
				double[] sums = new double[this._enclosing.numClasses];
				double[] probs = new double[this._enclosing.numClasses];
				for (int d = this.threadIdx; d < this._enclosing.data.Length; d += this.numThreads)
				{
					int[] features = this._enclosing.data[d];
					double[] vals = this._enclosing.values[d];
					// activation
					Arrays.Fill(sums, 0.0);
					for (int c = 0; c < this._enclosing.numClasses; c++)
					{
						for (int f = 0; f < features.Length; f++)
						{
							int feature = features[f];
							double val = vals[f];
							int i = this._enclosing.IndexOf(feature, c);
							sums[c] += this.x[i] * val;
						}
					}
					// expectation (slower routine replaced by fast way)
					// double total = Double.NEGATIVE_INFINITY;
					// for (int c=0; c<numClasses; c++) {
					//   total = SloppyMath.logAdd(total, sums[c]);
					// }
					// it is faster to split these two loops. More striding
					double total = ArrayMath.LogSum(sums);
					for (int c_1 = 0; c_1 < this._enclosing.numClasses; c_1++)
					{
						probs[c_1] = System.Math.Exp(sums[c_1] - total);
						if (this._enclosing.dataWeights != null)
						{
							probs[c_1] *= this._enclosing.dataWeights[d];
						}
					}
					for (int c_2 = 0; c_2 < this._enclosing.numClasses; c_2++)
					{
						for (int f = 0; f < features.Length; f++)
						{
							int feature = features[f];
							double val = vals[f];
							int i = this._enclosing.IndexOf(feature, c_2);
							this.localDerivative[i] += probs[c_2] * val;
						}
					}
					double dV = sums[this._enclosing.labels[d]] - total;
					if (this._enclosing.dataWeights != null)
					{
						dV *= this._enclosing.dataWeights[d];
					}
					this.localValue -= dV;
				}
				this.latch.CountDown();
			}

			private readonly LogConditionalObjectiveFunction<L, F> _enclosing;
		}

		/// <summary>Calculate conditional likelihood for datasets with real-valued features.</summary>
		/// <remarks>
		/// Calculate conditional likelihood for datasets with real-valued features.
		/// Currently this can calculate CL only (no support for SCL).
		/// TODO: sum-conditional obj. fun. with RVFs.
		/// </remarks>
		protected internal virtual void Rvfcalculate(double[] x)
		{
			value = 0.0;
			// This is only calculated once per training run, not worth the effort to multi-thread properly
			if (derivativeNumerator == null)
			{
				derivativeNumerator = new double[x.Length];
				for (int d = 0; d < data.Length; d++)
				{
					int[] features = data[d];
					double[] vals = values[d];
					for (int f = 0; f < features.Length; f++)
					{
						int i = IndexOf(features[f], labels[d]);
						if (dataWeights == null)
						{
							derivativeNumerator[i] -= vals[f];
						}
						else
						{
							derivativeNumerator[i] -= dataWeights[d] * vals[f];
						}
					}
				}
			}
			Copy(derivative, derivativeNumerator);
			//    Arrays.fill(derivative, 0.0);
			//    double[] counts = new double[numClasses];
			//    Arrays.fill(counts, 0.0);
			if (parallelGradientCalculation && threads > 1)
			{
				// Launch several threads (reused out of our fixed pool) to handle the computation
				LogConditionalObjectiveFunction.RVFDerivativeCalculation[] runnables = (LogConditionalObjectiveFunction.RVFDerivativeCalculation[])System.Array.CreateInstance(typeof(LogConditionalObjectiveFunction.RVFDerivativeCalculation), threads);
				CountDownLatch latch = new CountDownLatch(threads);
				for (int i = 0; i < threads; i++)
				{
					runnables[i] = new LogConditionalObjectiveFunction.RVFDerivativeCalculation(this, threads, i, x, derivative.Length, latch);
					new Thread(runnables[i]).Start();
				}
				try
				{
					latch.Await();
				}
				catch (Exception e)
				{
					throw new RuntimeInterruptedException(e);
				}
				for (int i_1 = 0; i_1 < threads; i_1++)
				{
					value += runnables[i_1].localValue;
					for (int j = 0; j < derivative.Length; j++)
					{
						derivative[j] += runnables[i_1].localDerivative[j];
					}
				}
			}
			else
			{
				// Do the calculation locally on this thread
				double[] sums = new double[numClasses];
				double[] probs = new double[numClasses];
				for (int d = 0; d < data.Length; d++)
				{
					int[] features = data[d];
					double[] vals = values[d];
					// activation
					Arrays.Fill(sums, 0.0);
					for (int f = 0; f < features.Length; f++)
					{
						int feature = features[f];
						double val = vals[f];
						for (int c = 0; c < numClasses; c++)
						{
							int i = IndexOf(feature, c);
							sums[c] += x[i] * val;
						}
					}
					// expectation (slower routine replaced by fast way)
					// double total = Double.NEGATIVE_INFINITY;
					// for (int c=0; c<numClasses; c++) {
					//   total = SloppyMath.logAdd(total, sums[c]);
					// }
					// it is faster to split these two loops. More striding
					double total = ArrayMath.LogSum(sums);
					for (int c_1 = 0; c_1 < numClasses; c_1++)
					{
						probs[c_1] = System.Math.Exp(sums[c_1] - total);
						if (dataWeights != null)
						{
							probs[c_1] *= dataWeights[d];
						}
					}
					for (int f_1 = 0; f_1 < features.Length; f_1++)
					{
						int feature = features[f_1];
						double val = vals[f_1];
						for (int c = 0; c_1 < numClasses; c_1++)
						{
							int i = IndexOf(feature, c_1);
							derivative[i] += probs[c_1] * val;
						}
					}
					double dV = sums[labels[d]] - total;
					if (dataWeights != null)
					{
						dV *= dataWeights[d];
					}
					value -= dV;
				}
			}
			value += prior.Compute(x, derivative);
		}

		public LogConditionalObjectiveFunction(GeneralDataset<L, F> dataset)
			: this(dataset, new LogPrior(LogPrior.LogPriorType.Quadratic))
		{
		}

		public LogConditionalObjectiveFunction(GeneralDataset<L, F> dataset, LogPrior prior)
			: this(dataset, prior, false)
		{
		}

		public LogConditionalObjectiveFunction(GeneralDataset<L, F> dataset, float[] dataWeights, LogPrior prior)
			: this(dataset, prior, false, dataWeights)
		{
		}

		public LogConditionalObjectiveFunction(GeneralDataset<L, F> dataset, LogPrior prior, bool useSumCondObjFun)
			: this(dataset, prior, useSumCondObjFun, null)
		{
		}

		/// <summary>Version passing in a GeneralDataset, which may be binary or real-valued features.</summary>
		public LogConditionalObjectiveFunction(GeneralDataset<L, F> dataset, LogPrior prior, bool useSumCondObjFun, float[] dataWeights)
		{
			this.prior = prior;
			this.useSummedConditionalLikelihood = useSumCondObjFun;
			this.numFeatures = dataset.NumFeatures();
			this.numClasses = dataset.NumClasses();
			this.data = dataset.GetDataArray();
			this.labels = dataset.GetLabelsArray();
			this.values = dataset.GetValuesArray();
			if (dataWeights != null)
			{
				this.dataWeights = dataWeights;
			}
			else
			{
				if (dataset is WeightedDataset<object, object>)
				{
					this.dataWeights = ((WeightedDataset<L, F>)dataset).GetWeights();
				}
				else
				{
					if (dataset is WeightedRVFDataset<object, object>)
					{
						this.dataWeights = ((WeightedRVFDataset<L, F>)dataset).GetWeights();
					}
					else
					{
						this.dataWeights = null;
					}
				}
			}
			this.labelIndex = null;
			this.featureIndex = null;
			this.dataIterable = null;
		}

		/// <summary>Version where an Iterable is passed in for the data.</summary>
		/// <remarks>Version where an Iterable is passed in for the data. Doesn't support dataWeights.</remarks>
		public LogConditionalObjectiveFunction(IEnumerable<IDatum<L, F>> dataIterable, LogPrior logPrior, IIndex<F> featureIndex, IIndex<L> labelIndex)
		{
			//TODO: test this [none of our code actually even uses it].
			this.prior = logPrior;
			this.useSummedConditionalLikelihood = false;
			this.numFeatures = featureIndex.Size();
			this.numClasses = labelIndex.Size();
			this.data = null;
			this.dataIterable = dataIterable;
			this.labelIndex = labelIndex;
			this.featureIndex = featureIndex;
			this.labels = null;
			//dataset.getLabelsArray();
			this.values = null;
			//dataset.getValuesArray();
			this.dataWeights = null;
		}

		public LogConditionalObjectiveFunction(int numFeatures, int numClasses, int[][] data, int[] labels, bool useSumCondObjFun)
			: this(numFeatures, numClasses, data, labels, null, new LogPrior(LogPrior.LogPriorType.Quadratic), useSumCondObjFun)
		{
		}

		public LogConditionalObjectiveFunction(int numFeatures, int numClasses, int[][] data, int[] labels)
			: this(numFeatures, numClasses, data, labels, new LogPrior(LogPrior.LogPriorType.Quadratic))
		{
		}

		public LogConditionalObjectiveFunction(int numFeatures, int numClasses, int[][] data, int[] labels, LogPrior prior)
			: this(numFeatures, numClasses, data, labels, null, prior)
		{
		}

		public LogConditionalObjectiveFunction(int numFeatures, int numClasses, int[][] data, int[] labels, float[] dataWeights)
			: this(numFeatures, numClasses, data, labels, dataWeights, new LogPrior(LogPrior.LogPriorType.Quadratic))
		{
		}

		public LogConditionalObjectiveFunction(int numFeatures, int numClasses, int[][] data, int[] labels, float[] dataWeights, LogPrior prior)
			: this(numFeatures, numClasses, data, labels, dataWeights, prior, false)
		{
		}

		public LogConditionalObjectiveFunction(int numFeatures, int numClasses, int[][] data, int[] labels, float[] dataWeights, LogPrior prior, bool useSummedConditionalLikelihood)
		{
			/* For binary features. Supports dataWeights. */
			this.numFeatures = numFeatures;
			this.numClasses = numClasses;
			this.data = data;
			this.values = null;
			this.labels = labels;
			this.prior = prior;
			this.dataWeights = dataWeights;
			this.labelIndex = null;
			this.featureIndex = null;
			this.dataIterable = null;
			this.useSummedConditionalLikelihood = useSummedConditionalLikelihood;
		}

		public LogConditionalObjectiveFunction(int numFeatures, int numClasses, int[][] data, int[] labels, int intPrior, double sigma, double epsilon)
			: this(numFeatures, numClasses, data, null, labels, intPrior, sigma, epsilon)
		{
		}

		/// <summary>For real-valued features.</summary>
		/// <remarks>For real-valued features. Passing in processed data set.</remarks>
		public LogConditionalObjectiveFunction(int numFeatures, int numClasses, int[][] data, double[][] values, int[] labels, int intPrior, double sigma, double epsilon)
		{
			this.numFeatures = numFeatures;
			this.numClasses = numClasses;
			this.data = data;
			this.values = values;
			this.labels = labels;
			this.prior = new LogPrior(intPrior, sigma, epsilon);
			this.labelIndex = null;
			this.featureIndex = null;
			this.dataIterable = null;
			this.useSummedConditionalLikelihood = false;
			this.dataWeights = null;
		}
	}
}
