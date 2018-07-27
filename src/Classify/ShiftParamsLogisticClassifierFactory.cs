using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Classify
{
	/// <author>jtibs</author>
	[System.Serializable]
	public class ShiftParamsLogisticClassifierFactory<L, F> : IClassifierFactory<L, F, MultinomialLogisticClassifier<L, F>>
	{
		private const long serialVersionUID = -8977510677251295037L;

		private int[][] data;

		private double[][] dataValues;

		private int[] labels;

		private int numClasses;

		private int numFeatures;

		private LogPrior prior;

		private double lambda;

		public ShiftParamsLogisticClassifierFactory()
			: this(new LogPrior(LogPrior.LogPriorType.Null), 0.1)
		{
		}

		public ShiftParamsLogisticClassifierFactory(double lambda)
			: this(new LogPrior(LogPrior.LogPriorType.Null), lambda)
		{
		}

		public ShiftParamsLogisticClassifierFactory(LogPrior prior, double lambda)
		{
			// NOTE: the current implementation only supports quadratic priors (or no prior)
			this.prior = prior;
			this.lambda = lambda;
		}

		public virtual MultinomialLogisticClassifier<L, F> TrainClassifier(GeneralDataset<L, F> dataset)
		{
			numClasses = dataset.NumClasses();
			numFeatures = dataset.NumFeatures();
			data = dataset.GetDataArray();
			if (dataset is RVFDataset<object, object>)
			{
				dataValues = dataset.GetValuesArray();
			}
			else
			{
				dataValues = LogisticUtils.InitializeDataValues(data);
			}
			AugmentFeatureMatrix(data, dataValues);
			labels = dataset.GetLabelsArray();
			return new MultinomialLogisticClassifier<L, F>(TrainWeights(), dataset.featureIndex, dataset.labelIndex);
		}

		private double[][] TrainWeights()
		{
			QNMinimizer minimizer = new QNMinimizer(15, true);
			minimizer.UseOWLQN(true, lambda);
			IDiffFunction objective = new ShiftParamsLogisticObjectiveFunction(data, dataValues, ConvertLabels(labels), numClasses, numFeatures + data.Length, numFeatures, prior);
			double[] augmentedThetas = new double[(numClasses - 1) * (numFeatures + data.Length)];
			augmentedThetas = minimizer.Minimize(objective, 1e-4, augmentedThetas);
			// calculate number of non-zero parameters, for debugging
			int count = 0;
			for (int j = numFeatures; j < augmentedThetas.Length; j++)
			{
				if (augmentedThetas[j] != 0)
				{
					count++;
				}
			}
			Redwood.Log("NUM NONZERO PARAMETERS: " + count);
			double[][] thetas = new double[][] {  };
			LogisticUtils.Unflatten(augmentedThetas, thetas);
			return thetas;
		}

		// augments the feature matrix to account for shift parameters, setting X := [X|I]
		private void AugmentFeatureMatrix(int[][] data, double[][] dataValues)
		{
			for (int i = 0; i < data.Length; i++)
			{
				int newLength = data[i].Length + 1;
				data[i] = Arrays.CopyOf(data[i], newLength);
				data[i][newLength - 1] = i + numFeatures;
				dataValues[i] = Arrays.CopyOf(dataValues[i], newLength);
				dataValues[i][newLength - 1] = 1;
			}
		}

		// convert labels to form that the objective function expects
		private int[][] ConvertLabels(int[] labels)
		{
			int[][] result = new int[labels.Length][];
			for (int i = 0; i < labels.Length; i++)
			{
				result[i][labels[i]] = 1;
			}
			return result;
		}
	}
}
