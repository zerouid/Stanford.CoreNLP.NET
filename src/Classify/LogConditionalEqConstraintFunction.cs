using System;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Util;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Maximizes the conditional likelihood with a given prior.</summary>
	/// <remarks>
	/// Maximizes the conditional likelihood with a given prior.
	/// Constrains parameters for the same history to sum to 1
	/// Adapted from
	/// <see cref="LogConditionalObjectiveFunction{L, F}"/>
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	public class LogConditionalEqConstraintFunction : AbstractCachingDiffFunction
	{
		public const int NoPrior = 0;

		public const int QuadraticPrior = 1;

		public const int HuberPrior = 2;

		public const int QuarticPrior = 3;

		protected internal int numFeatures = 0;

		protected internal int numClasses = 0;

		protected internal int[][] data = null;

		protected internal int[] labels = null;

		protected internal int[] numValues = null;

		private int prior;

		private double sigma = 1.0;

		private double epsilon;

		private IIndex<IntTuple> featureIndex;

		/* Use a Huber robust regression penalty (L1 except very near 0) not L2 */
		public override int DomainDimension()
		{
			return featureIndex.Size();
		}

		internal virtual int ClassOf(int index)
		{
			IntTuple i = featureIndex.Get(index);
			return i.Get(0);
		}

		/// <summary>the feature number of the original feature or -1 if this is for a prior</summary>
		internal virtual int FeatureOf(int index)
		{
			IntTuple i = featureIndex.Get(index);
			if (i.Length() == 1)
			{
				return -1;
			}
			return i.Get(1);
		}

		/// <returns>the index of the prior for class c</returns>
		protected internal virtual int IndexOf(int c)
		{
			return featureIndex.IndexOf(new IntUni(c));
		}

		protected internal virtual int IndexOf(int f, int c, int val)
		{
			return featureIndex.IndexOf(new IntTriple(c, f, val));
		}

		/// <summary>create an index for each parameter - the prior probs and the features with all of their values</summary>
		protected internal virtual IIndex<IntTuple> CreateIndex()
		{
			IIndex<IntTuple> index = new HashIndex<IntTuple>();
			for (int c = 0; c < numClasses; c++)
			{
				index.Add(new IntUni(c));
				for (int f = 0; f < numFeatures; f++)
				{
					for (int val = 0; val < numValues[f]; val++)
					{
						index.Add(new IntTriple(c, f, val));
					}
				}
			}
			return index;
		}

		public virtual double[][][] To3D(double[] x1)
		{
			double[] x = Normalize(x1);
			double[][][] x2 = new double[numClasses][][];
			for (int c = 0; c < numClasses; c++)
			{
				for (int f = 0; f < numFeatures; f++)
				{
					x2[c][f] = new double[numValues[f]];
					for (int val = 0; val < numValues[f]; val++)
					{
						x2[c][f][val] = x[IndexOf(f, c, val)];
					}
				}
			}
			return x2;
		}

		public virtual double[] Priors(double[] x1)
		{
			double[] x = Normalize(x1);
			double[] x2 = new double[numClasses];
			for (int c = 0; c < numClasses; c++)
			{
				x2[c] = x[IndexOf(c)];
			}
			return x2;
		}

		/// <summary>normalize the parameters s.t qi=log(e^li/Z);</summary>
		private double[] Normalize(double[] x)
		{
			double[] x1 = new double[x.Length];
			Copy(x1, x);
			//the priors
			double[] sums = new double[numClasses];
			for (int c = 0; c < numClasses; c++)
			{
				int priorc = IndexOf(c);
				sums[c] += x[priorc];
			}
			double total = ArrayMath.LogSum(sums);
			for (int c_1 = 0; c_1 < numClasses; c_1++)
			{
				int priorc = IndexOf(c_1);
				x1[priorc] -= total;
			}
			//the features
			for (int c_2 = 0; c_2 < numClasses; c_2++)
			{
				for (int f = 0; f < numFeatures; f++)
				{
					double[] vals = new double[numValues[f]];
					for (int val = 0; val < numValues[f]; val++)
					{
						int index = IndexOf(f, c_2, val);
						vals[val] = x[index];
					}
					total = ArrayMath.LogSum(vals);
					for (int val_1 = 0; val_1 < numValues[f]; val_1++)
					{
						int index = IndexOf(f, c_2, val_1);
						x1[index] -= total;
					}
				}
			}
			return x1;
		}

		protected internal override void Calculate(double[] x1)
		{
			double[] x = Normalize(x1);
			double[] xExp = new double[x.Length];
			for (int i = 0; i < x.Length; i++)
			{
				xExp[i] = System.Math.Exp(x[i]);
			}
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
					int priorc = IndexOf(c);
					sums[c] += x[priorc];
					for (int f = 0; f < features.Length; f++)
					{
						int i_1 = IndexOf(f, c, features[f]);
						sums[c] += x[i_1];
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
					int priorc = IndexOf(c_1);
					derivative[priorc] += probs[c_1];
					for (int f = 0; f < features.Length; f++)
					{
						for (int val = 0; val < numValues[f]; val++)
						{
							int i_1 = IndexOf(f, c_1, val);
							double thetha = xExp[i_1];
							derivative[i_1] -= probs[c_1] * thetha;
							if (labels[d] == c_1)
							{
								derivative[i_1] += thetha;
							}
						}
					}
				}
				// observed
				for (int f_1 = 0; f_1 < features.Length; f_1++)
				{
					int i_1 = IndexOf(f_1, labels[d], features[f_1]);
					derivative[i_1] -= 1.0;
					for (int c_2 = 0; c_2 < numClasses; c_2++)
					{
						int i1 = IndexOf(f_1, c_2, features[f_1]);
						derivative[i1] += probs[c_2];
					}
				}
				value -= sums[labels[d]] - total;
				int priorc_1 = IndexOf(labels[d]);
				derivative[priorc_1] -= 1;
			}
			// priors
			if (prior == QuadraticPrior)
			{
				double sigmaSq = sigma * sigma;
				for (int i_1 = 0; i_1 < x1.Length; i_1++)
				{
					double k = 1.0;
					double w = x1[i_1];
					value += k * w * w / 2.0 / sigmaSq;
					derivative[i_1] += k * w / sigmaSq;
				}
			}
			else
			{
				if (prior == HuberPrior)
				{
					double sigmaSq = sigma * sigma;
					for (int i_1 = 0; i_1 < x1.Length; i_1++)
					{
						double w = x1[i_1];
						double wabs = System.Math.Abs(w);
						if (wabs < epsilon)
						{
							value += w * w / 2.0 / epsilon / sigmaSq;
							derivative[i_1] += w / epsilon / sigmaSq;
						}
						else
						{
							value += (wabs - epsilon / 2) / sigmaSq;
							derivative[i_1] += ((w < 0.0) ? -1.0 : 1.0) / sigmaSq;
						}
					}
				}
				else
				{
					if (prior == QuarticPrior)
					{
						double sigmaQu = sigma * sigma * sigma * sigma;
						for (int i_1 = 0; i_1 < x.Length; i_1++)
						{
							double k = 1.0;
							double w = x1[i_1];
							value += k * w * w * w * w / 2.0 / sigmaQu;
							derivative[i_1] += k * w / sigmaQu;
						}
					}
				}
			}
		}

		public LogConditionalEqConstraintFunction(int numFeatures, int numClasses, int[][] data, int[] labels)
			: this(numFeatures, numClasses, data, labels, 1.0)
		{
		}

		public LogConditionalEqConstraintFunction(int numFeatures, int numClasses, int[][] data, int[] labels, double sigma)
			: this(numFeatures, numClasses, data, labels, QuadraticPrior, sigma, 0.0)
		{
		}

		public LogConditionalEqConstraintFunction(int numFeatures, int numClasses, int[][] data, int[] labels, int prior, double sigma, double epsilon)
		{
			// else no prior
			/*
			System.out.println("N: "+data.length);
			System.out.println("Value: "+value);
			double ds = 0.0;
			for (int i=0; i<x.length; i++) {
			ds += derivative[i];
			System.out.println(i+" is: "+derivative[i]);
			}
			*/
			//System.out.println("Deriv sum is: "+ds);
			this.numFeatures = numFeatures;
			this.numClasses = numClasses;
			this.data = data;
			this.labels = labels;
			if (prior >= 0 && prior <= QuarticPrior)
			{
				this.prior = prior;
			}
			else
			{
				throw new ArgumentException("Invalid prior: " + prior);
			}
			this.epsilon = epsilon;
			this.sigma = sigma;
			numValues = NaiveBayesClassifierFactory.NumberValues(data, numFeatures);
			for (int i = 0; i < numValues.Length; i++)
			{
				System.Console.Out.WriteLine("numValues " + i + " " + numValues[i]);
			}
			featureIndex = CreateIndex();
		}

		/// <summary>use a random starting point uniform -1 1</summary>
		public override double[] Initial()
		{
			double[] initial = new double[DomainDimension()];
			for (int i = 0; i < initial.Length; i++)
			{
				double r = System.Math.Random();
				r -= .5;
				initial[i] = r;
			}
			return initial;
		}
	}
}
