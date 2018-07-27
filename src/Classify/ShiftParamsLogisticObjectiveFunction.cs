using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Optimization;



namespace Edu.Stanford.Nlp.Classify
{
	/// <author>jtibs</author>
	public class ShiftParamsLogisticObjectiveFunction : AbstractCachingDiffFunction, IHasRegularizerParamRange
	{
		private readonly int[][] data;

		private readonly double[][] dataValues;

		private readonly int numClasses;

		private readonly int numFeatures;

		private readonly int[][] labels;

		private readonly int numL2Parameters;

		private readonly LogPrior prior;

		public ShiftParamsLogisticObjectiveFunction(int[][] data, double[][] dataValues, int[][] labels, int numClasses, int numFeatures, int numL2Parameters, LogPrior prior)
		{
			this.data = data;
			this.dataValues = dataValues;
			this.labels = labels;
			this.numClasses = numClasses;
			this.numFeatures = numFeatures;
			this.numL2Parameters = numL2Parameters;
			this.prior = prior;
		}

		public override int DomainDimension()
		{
			return (numClasses - 1) * numFeatures;
		}

		protected internal override void Calculate(double[] thetasArray)
		{
			ClearResults();
			double[][] thetas = new double[][] {  };
			LogisticUtils.Unflatten(thetasArray, thetas);
			for (int i = 0; i < data.Length; i++)
			{
				int[] featureIndices = data[i];
				double[] featureValues = dataValues[i];
				double[] sums = LogisticUtils.CalculateSums(thetas, featureIndices, featureValues);
				for (int c = 0; c < numClasses; c++)
				{
					double sum = sums[c];
					value -= sum * labels[i][c];
					if (c == 0)
					{
						continue;
					}
					int offset = (c - 1) * numFeatures;
					double error = Math.Exp(sum) - labels[i][c];
					for (int f = 0; f < featureIndices.Length; f++)
					{
						int index = featureIndices[f];
						double x = featureValues[f];
						derivative[offset + index] -= error * x;
					}
				}
			}
			// incorporate prior
			if (prior.GetType().Equals(LogPrior.LogPriorType.Null))
			{
				return;
			}
			double sigma = prior.GetSigma();
			for (int c_1 = 0; c_1 < numClasses; c_1++)
			{
				if (c_1 == 0)
				{
					continue;
				}
				int offset = (c_1 - 1) * numFeatures;
				for (int j = 0; j < numL2Parameters; j++)
				{
					double theta = thetasArray[offset + j];
					value += theta * theta / (sigma * 2.0);
					derivative[offset + j] += theta / sigma;
				}
			}
		}

		private void ClearResults()
		{
			value = 0.0;
			Arrays.Fill(derivative, 0.0);
		}

		public virtual ICollection<int> GetRegularizerParamRange(double[] x)
		{
			ICollection<int> result = new HashSet<int>();
			for (int i = numL2Parameters; i < x.Length; i++)
			{
				result.Add(i);
			}
			return result;
		}
	}
}
