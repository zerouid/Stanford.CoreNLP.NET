using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Coref.Hybrid.RF
{
	[System.Serializable]
	public class DecisionTree
	{
		private const long serialVersionUID = -4198470422641238244L;

		public DecisionTreeNode root;

		public IIndex<string> featureIndex;

		public DecisionTree(IIndex<string> featureIndex)
		{
			this.featureIndex = featureIndex;
			this.root = null;
		}

		public virtual double ProbabilityOfTrue(RVFDatum<bool, string> datum)
		{
			return ProbabilityOfTrue(datum.AsFeaturesCounter());
		}

		public virtual double ProbabilityOfTrue(ICounter<string> features)
		{
			DecisionTreeNode cur = root;
			while (!cur.IsLeaf())
			{
				double value = features.GetCount(featureIndex.Get(cur.idx));
				cur = (value < cur.split) ? cur.children[0] : cur.children[1];
			}
			return (cur.split);
		}
		// at the leaf node, idx represents true or false. 1: true, 0: false, split represents probability of true.
	}
}
