using System;
using Edu.Stanford.Nlp.Optimization;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Maximizes the conditional likelihood with a given prior.</summary>
	/// <remarks>
	/// Maximizes the conditional likelihood with a given prior.
	/// Because the problem is binary, optimizations are possible that
	/// cannot be done in LogConditionalObjectiveFunction.
	/// </remarks>
	/// <author>Galen Andrew</author>
	public class LogisticObjectiveFunction : AbstractCachingDiffFunction
	{
		private readonly int numFeatures;

		private readonly int[][] data;

		private readonly double[][] dataValues;

		private readonly int[] labels;

		protected internal float[] dataweights = null;

		private readonly LogPrior prior;

		public override int DomainDimension()
		{
			return numFeatures;
		}

		protected internal override void Calculate(double[] x)
		{
			if (dataValues != null)
			{
				CalculateRVF(x);
				return;
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
				for (int f = 0; f < features.Length; f++)
				{
					sum += x[features[f]] * values[f];
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
				for (int f_1 = 0; f_1 < features.Length; f_1++)
				{
					derivative[features[f_1]] += values[f_1] * derivativeIncrement;
				}
			}
			value += prior.Compute(x, derivative);
		}

		public LogisticObjectiveFunction(int numFeatures, int[][] data, int[] labels)
			: this(numFeatures, data, labels, new LogPrior(LogPrior.LogPriorType.Quadratic))
		{
		}

		public LogisticObjectiveFunction(int numFeatures, int[][] data, int[] labels, LogPrior prior)
			: this(numFeatures, data, labels, prior, null)
		{
		}

		public LogisticObjectiveFunction(int numFeatures, int[][] data, int[] labels, float[] dataweights)
			: this(numFeatures, data, labels, new LogPrior(LogPrior.LogPriorType.Quadratic), dataweights)
		{
		}

		public LogisticObjectiveFunction(int numFeatures, int[][] data, int[] labels, LogPrior prior, float[] dataweights)
			: this(numFeatures, data, null, labels, prior, dataweights)
		{
		}

		public LogisticObjectiveFunction(int numFeatures, int[][] data, double[][] values, int[] labels)
			: this(numFeatures, data, values, labels, new LogPrior(LogPrior.LogPriorType.Quadratic))
		{
		}

		public LogisticObjectiveFunction(int numFeatures, int[][] data, double[][] values, int[] labels, LogPrior prior)
			: this(numFeatures, data, values, labels, prior, null)
		{
		}

		public LogisticObjectiveFunction(int numFeatures, int[][] data, double[][] values, int[] labels, float[] dataweights)
			: this(numFeatures, data, values, labels, new LogPrior(LogPrior.LogPriorType.Quadratic), dataweights)
		{
		}

		public LogisticObjectiveFunction(int numFeatures, int[][] data, double[][] values, int[] labels, LogPrior prior, float[] dataweights)
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
