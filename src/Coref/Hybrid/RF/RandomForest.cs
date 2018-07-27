using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Coref.Hybrid.RF
{
	[System.Serializable]
	public class RandomForest
	{
		private const long serialVersionUID = -2736377471905671276L;

		public readonly DecisionTree[] trees;

		public readonly IIndex<string> featureIndex;

		public RandomForest(IIndex<string> featureIndex, int numTrees)
		{
			this.featureIndex = featureIndex;
			this.trees = new DecisionTree[numTrees];
		}

		public virtual double ProbabilityOfTrue(RVFDatum<bool, string> datum)
		{
			return ProbabilityOfTrue(datum.AsFeaturesCounter());
		}

		public virtual double ProbabilityOfTrue(ICounter<string> features)
		{
			double probTrue = 0;
			foreach (DecisionTree tree in trees)
			{
				probTrue += tree.ProbabilityOfTrue(features);
			}
			return probTrue / trees.Length;
		}
	}
}
