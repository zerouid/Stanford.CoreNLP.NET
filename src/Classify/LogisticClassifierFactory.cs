using System;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Builds a classifier for binary logistic regression problems.</summary>
	/// <remarks>
	/// Builds a classifier for binary logistic regression problems.
	/// This uses the standard statistics textbook formulation of binary
	/// logistic regression, which is more efficient than using the
	/// LinearClassifier class.
	/// </remarks>
	/// <author>Ramesh Nallapati nmramesh@cs.stanford.edu</author>
	[System.Serializable]
	public class LogisticClassifierFactory<L, F> : IClassifierFactory<L, F, LogisticClassifier<L, F>>
	{
		private const long serialVersionUID = 1L;

		private double[] weights;

		private IIndex<F> featureIndex;

		private L[] classes = ErasureUtils.MkTArray<L>(typeof(object), 2);

		public virtual LogisticClassifier<L, F> TrainWeightedData(GeneralDataset<L, F> data, float[] dataWeights)
		{
			if (data is RVFDataset)
			{
				((RVFDataset<L, F>)data).EnsureRealValues();
			}
			if (data.labelIndex.Size() != 2)
			{
				throw new Exception("LogisticClassifier is only for binary classification!");
			}
			IMinimizer<IDiffFunction> minim;
			LogisticObjectiveFunction lof = null;
			if (data is Dataset<object, object>)
			{
				lof = new LogisticObjectiveFunction(data.NumFeatureTypes(), data.GetDataArray(), data.GetLabelsArray(), new LogPrior(LogPrior.LogPriorType.Quadratic), dataWeights);
			}
			else
			{
				if (data is RVFDataset<object, object>)
				{
					lof = new LogisticObjectiveFunction(data.NumFeatureTypes(), data.GetDataArray(), data.GetValuesArray(), data.GetLabelsArray(), new LogPrior(LogPrior.LogPriorType.Quadratic), dataWeights);
				}
			}
			minim = new QNMinimizer(lof);
			weights = minim.Minimize(lof, 1e-4, new double[data.NumFeatureTypes()]);
			featureIndex = data.featureIndex;
			classes[0] = data.labelIndex.Get(0);
			classes[1] = data.labelIndex.Get(1);
			return new LogisticClassifier<L, F>(weights, featureIndex, classes);
		}

		public virtual LogisticClassifier<L, F> TrainClassifier(GeneralDataset<L, F> data)
		{
			return TrainClassifier(data, 0.0);
		}

		public virtual LogisticClassifier<L, F> TrainClassifier(GeneralDataset<L, F> data, LogPrior prior, bool biased)
		{
			return TrainClassifier(data, 0.0, 1e-4, prior, biased);
		}

		public virtual LogisticClassifier<L, F> TrainClassifier(GeneralDataset<L, F> data, double l1reg)
		{
			return TrainClassifier(data, l1reg, 1e-4);
		}

		public virtual LogisticClassifier<L, F> TrainClassifier(GeneralDataset<L, F> data, double l1reg, double tol)
		{
			return TrainClassifier(data, l1reg, tol, new LogPrior(LogPrior.LogPriorType.Quadratic), false);
		}

		public virtual LogisticClassifier<L, F> TrainClassifier(GeneralDataset<L, F> data, double l1reg, double tol, LogPrior prior)
		{
			return TrainClassifier(data, l1reg, tol, prior, false);
		}

		public virtual LogisticClassifier<L, F> TrainClassifier(GeneralDataset<L, F> data, double l1reg, double tol, bool biased)
		{
			return TrainClassifier(data, l1reg, tol, new LogPrior(LogPrior.LogPriorType.Quadratic), biased);
		}

		public virtual LogisticClassifier<L, F> TrainClassifier(GeneralDataset<L, F> data, double l1reg, double tol, LogPrior prior, bool biased)
		{
			if (data is RVFDataset)
			{
				((RVFDataset<L, F>)data).EnsureRealValues();
			}
			if (data.labelIndex.Size() != 2)
			{
				throw new Exception("LogisticClassifier is only for binary classification!");
			}
			IMinimizer<IDiffFunction> minim;
			if (!biased)
			{
				LogisticObjectiveFunction lof = null;
				if (data is Dataset<object, object>)
				{
					lof = new LogisticObjectiveFunction(data.NumFeatureTypes(), data.GetDataArray(), data.GetLabelsArray(), prior);
				}
				else
				{
					if (data is RVFDataset<object, object>)
					{
						lof = new LogisticObjectiveFunction(data.NumFeatureTypes(), data.GetDataArray(), data.GetValuesArray(), data.GetLabelsArray(), prior);
					}
				}
				if (l1reg > 0.0)
				{
					minim = ReflectionLoading.LoadByReflection("edu.stanford.nlp.optimization.OWLQNMinimizer", l1reg);
				}
				else
				{
					minim = new QNMinimizer(lof);
				}
				weights = minim.Minimize(lof, tol, new double[data.NumFeatureTypes()]);
			}
			else
			{
				BiasedLogisticObjectiveFunction lof = new BiasedLogisticObjectiveFunction(data.NumFeatureTypes(), data.GetDataArray(), data.GetLabelsArray(), prior);
				if (l1reg > 0.0)
				{
					minim = ReflectionLoading.LoadByReflection("edu.stanford.nlp.optimization.OWLQNMinimizer", l1reg);
				}
				else
				{
					minim = new QNMinimizer(lof);
				}
				weights = minim.Minimize(lof, tol, new double[data.NumFeatureTypes()]);
			}
			featureIndex = data.featureIndex;
			classes[0] = data.labelIndex.Get(0);
			classes[1] = data.labelIndex.Get(1);
			return new LogisticClassifier<L, F>(weights, featureIndex, classes);
		}
	}
}
