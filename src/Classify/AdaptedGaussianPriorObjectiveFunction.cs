using System;
using Edu.Stanford.Nlp.Math;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Adapt the mean of the Gaussian Prior by shifting the mean to the previously trained weights</summary>
	/// <author>Pi-Chuan Chang</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	/// <?/>
	public class AdaptedGaussianPriorObjectiveFunction<L, F> : LogConditionalObjectiveFunction<L, F>
	{
		internal double[] weights;

		/// <summary>Calculate the conditional likelihood.</summary>
		protected internal override void Calculate(double[] x)
		{
			if (useSummedConditionalLikelihood)
			{
				CalculateSCL(x);
			}
			else
			{
				CalculateCL(x);
			}
		}

		private void CalculateSCL(double[] x)
		{
			throw new NotSupportedException();
		}

		private void CalculateCL(double[] x)
		{
			value = 0.0;
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
			double[] sums = new double[numClasses];
			double[] probs = new double[numClasses];
			for (int d_1 = 0; d_1 < data.Length; d_1++)
			{
				int[] features = data[d_1];
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
				double total = ArrayMath.LogSum(sums);
				for (int c_1 = 0; c_1 < numClasses; c_1++)
				{
					probs[c_1] = System.Math.Exp(sums[c_1] - total);
					if (dataWeights != null)
					{
						probs[c_1] *= dataWeights[d_1];
					}
					foreach (int feature in features)
					{
						int i = IndexOf(feature, c_1);
						derivative[i] += probs[c_1];
					}
				}
				double dV = sums[labels[d_1]] - total;
				if (dataWeights != null)
				{
					dV *= dataWeights[d_1];
				}
				value -= dV;
			}
			//Logging.logger(this.getClass()).info("x length="+x.length);
			//Logging.logger(this.getClass()).info("weights length="+weights.length);
			double[] newX = ArrayMath.PairwiseSubtract(x, weights);
			value += prior.Compute(newX, derivative);
		}

		protected internal override void Rvfcalculate(double[] x)
		{
			throw new NotSupportedException();
		}

		public AdaptedGaussianPriorObjectiveFunction(GeneralDataset<L, F> dataset, LogPrior prior, double[][] weights)
			: base(dataset, prior)
		{
			this.weights = To1D(weights);
		}

		public virtual double[] To1D(double[][] x2)
		{
			double[] x = new double[numFeatures * numClasses];
			for (int i = 0; i < numFeatures; i++)
			{
				for (int j = 0; j < numClasses; j++)
				{
					x[IndexOf(i, j)] = x2[i][j];
				}
			}
			return x;
		}
	}
}
