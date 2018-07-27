using System;
using Edu.Stanford.Nlp.Optimization;

namespace Edu.Stanford.Nlp.Classify
{
	/// <author>jrfinkel</author>
	public class BiasedLogisticObjectiveFunction : AbstractCachingDiffFunction
	{
		private readonly int numFeatures;

		private readonly int[][] data;

		private readonly double[][] dataValues;

		private readonly int[] labels;

		protected internal float[] dataweights = null;

		private readonly LogPrior prior;

		internal double probCorrect = 0.7;

		public override int DomainDimension()
		{
			return numFeatures;
		}

		protected internal override void Calculate(double[] x)
		{
			if (dataValues != null)
			{
				throw new Exception();
			}
			value = 0.0;
			Arrays.Fill(derivative, 0.0);
			for (int d = 0; d < data.Length; d++)
			{
				int[] features = data[d];
				double sum = 0;
				foreach (int feature1 in features)
				{
					sum += x[feature1];
				}
				double expSum;
				double derivativeIncrement;
				if (dataweights != null)
				{
					throw new Exception();
				}
				if (labels[d] == 1)
				{
					expSum = Math.Exp(-sum);
					double g = (1 / (1 + expSum));
					value -= Math.Log(g);
					derivativeIncrement = (g - 1);
				}
				else
				{
					//         expSum = Math.exp(-sum);
					//         double g = (1 / (1 + expSum));
					//         value -= Math.log(1-g);
					//         derivativeIncrement = (g);
					//       }
					expSum = Math.Exp(-sum);
					double g = (1 / (1 + expSum));
					double e = (1 - probCorrect) * g + (probCorrect) * (1 - g);
					value -= Math.Log(e);
					derivativeIncrement = -(g * (1 - g) * (1 - 2 * probCorrect)) / (e);
				}
				foreach (int feature in features)
				{
					derivative[feature] += derivativeIncrement;
				}
			}
			value += prior.Compute(x, derivative);
		}

		protected internal virtual void CalculateRVF(double[] x)
		{
			value = 0.0;
			Arrays.Fill(derivative, 0.0);
			for (int d = 0; d < data.Length; d++)
			{
				int[] features = data[d];
				double[] values = dataValues[d];
				double sum = 0;
				foreach (int feature1 in features)
				{
					sum += x[feature1] * values[feature1];
				}
				double expSum;
				double derivativeIncrement;
				if (labels[d] == 0)
				{
					expSum = Math.Exp(sum);
					derivativeIncrement = 1.0 / (1.0 + (1.0 / expSum));
				}
				else
				{
					expSum = Math.Exp(-sum);
					derivativeIncrement = -1.0 / (1.0 + (1.0 / expSum));
				}
				if (dataweights == null)
				{
					value += Math.Log(1.0 + expSum);
				}
				else
				{
					value += Math.Log(1.0 + expSum) * dataweights[d];
					derivativeIncrement *= dataweights[d];
				}
				foreach (int feature in features)
				{
					derivative[feature] += values[feature] * derivativeIncrement;
				}
			}
			value += prior.Compute(x, derivative);
		}

		public BiasedLogisticObjectiveFunction(int numFeatures, int[][] data, int[] labels)
			: this(numFeatures, data, labels, new LogPrior(LogPrior.LogPriorType.Quadratic))
		{
		}

		public BiasedLogisticObjectiveFunction(int numFeatures, int[][] data, int[] labels, LogPrior prior)
			: this(numFeatures, data, labels, prior, null)
		{
		}

		public BiasedLogisticObjectiveFunction(int numFeatures, int[][] data, int[] labels, float[] dataweights)
			: this(numFeatures, data, labels, new LogPrior(LogPrior.LogPriorType.Quadratic), dataweights)
		{
		}

		public BiasedLogisticObjectiveFunction(int numFeatures, int[][] data, int[] labels, LogPrior prior, float[] dataweights)
			: this(numFeatures, data, null, labels, prior, dataweights)
		{
		}

		public BiasedLogisticObjectiveFunction(int numFeatures, int[][] data, double[][] values, int[] labels)
			: this(numFeatures, data, values, labels, new LogPrior(LogPrior.LogPriorType.Quadratic))
		{
		}

		public BiasedLogisticObjectiveFunction(int numFeatures, int[][] data, double[][] values, int[] labels, LogPrior prior)
			: this(numFeatures, data, values, labels, prior, null)
		{
		}

		public BiasedLogisticObjectiveFunction(int numFeatures, int[][] data, double[][] values, int[] labels, LogPrior prior, float[] dataweights)
		{
			this.numFeatures = numFeatures;
			this.data = data;
			this.labels = labels;
			this.prior = prior;
			this.dataweights = dataweights;
			this.dataValues = values;
		}
	}
}
