using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Maximizes the conditional likelihood with a given prior.</summary>
	/// <author>Jenny Finkel</author>
	public class BiasedLogConditionalObjectiveFunction : AbstractCachingDiffFunction
	{
		public virtual void SetPrior(LogPrior prior)
		{
			this.prior = prior;
		}

		protected internal LogPrior prior;

		protected internal int numFeatures = 0;

		protected internal int numClasses = 0;

		protected internal int[][] data = null;

		protected internal int[] labels = null;

		private double[][] confusionMatrix;

		public override int DomainDimension()
		{
			return numFeatures * numClasses;
		}

		internal virtual int ClassOf(int index)
		{
			return index % numClasses;
		}

		internal virtual int FeatureOf(int index)
		{
			return index / numClasses;
		}

		protected internal virtual int IndexOf(int f, int c)
		{
			return f * numClasses + c;
		}

		public virtual double[][] To2D(double[] x)
		{
			double[][] x2 = new double[numFeatures][];
			for (int i = 0; i < numFeatures; i++)
			{
				x2[i] = new double[numClasses];
				for (int j = 0; j < numClasses; j++)
				{
					x2[i][j] = x[IndexOf(i, j)];
				}
			}
			return x2;
		}

		protected internal override void Calculate(double[] x)
		{
			if (derivative == null)
			{
				derivative = new double[x.Length];
			}
			else
			{
				Arrays.Fill(derivative, 0.0);
			}
			value = 0.0;
			double[] sums = new double[numClasses];
			double[] probs = new double[numClasses];
			double[] weightedProbs = new double[numClasses];
			for (int d = 0; d < data.Length; d++)
			{
				int[] features = data[d];
				int observedLabel = labels[d];
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
				double[] weightedSums = new double[numClasses];
				for (int trueLabel = 0; trueLabel < numClasses; trueLabel++)
				{
					weightedSums[trueLabel] = System.Math.Log(confusionMatrix[observedLabel][trueLabel]) + sums[trueLabel];
				}
				double weightedTotal = ArrayMath.LogSum(weightedSums);
				for (int c_1 = 0; c_1 < numClasses; c_1++)
				{
					probs[c_1] = System.Math.Exp(sums[c_1] - total);
					weightedProbs[c_1] = System.Math.Exp(weightedSums[c_1] - weightedTotal);
					foreach (int feature in features)
					{
						int i = IndexOf(feature, c_1);
						derivative[i] += probs[c_1] - weightedProbs[c_1];
					}
				}
				double tmpValue = 0.0;
				for (int c_2 = 0; c_2 < numClasses; c_2++)
				{
					tmpValue += confusionMatrix[observedLabel][c_2] * System.Math.Exp(sums[c_2] - total);
				}
				value -= System.Math.Log(tmpValue);
			}
			value += prior.Compute(x, derivative);
		}

		public BiasedLogConditionalObjectiveFunction(GeneralDataset<object, object> dataset, double[][] confusionMatrix)
			: this(dataset, confusionMatrix, new LogPrior(LogPrior.LogPriorType.Quadratic))
		{
		}

		public BiasedLogConditionalObjectiveFunction(GeneralDataset<object, object> dataset, double[][] confusionMatrix, LogPrior prior)
			: this(dataset.NumFeatures(), dataset.NumClasses(), dataset.GetDataArray(), dataset.GetLabelsArray(), confusionMatrix, prior)
		{
		}

		public BiasedLogConditionalObjectiveFunction(int numFeatures, int numClasses, int[][] data, int[] labels, double[][] confusionMatrix)
			: this(numFeatures, numClasses, data, labels, confusionMatrix, new LogPrior(LogPrior.LogPriorType.Quadratic))
		{
		}

		public BiasedLogConditionalObjectiveFunction(int numFeatures, int numClasses, int[][] data, int[] labels, double[][] confusionMatrix, LogPrior prior)
		{
			this.numFeatures = numFeatures;
			this.numClasses = numClasses;
			this.data = data;
			this.labels = labels;
			this.prior = prior;
			this.confusionMatrix = confusionMatrix;
		}
	}
}
