using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid.RF
{
	[System.Serializable]
	public class DecisionTreeNode
	{
		private const long serialVersionUID = 8566766017320577273L;

		internal int idx;

		internal float split;

		internal Edu.Stanford.Nlp.Coref.Hybrid.RF.DecisionTreeNode[] children;

		internal DecisionTreeNode()
		{
			// if not leaf, feature index. if leaf, idx=1 -> true, idx=0 -> false.
			// if not leaf, split point. if leaf, true probability.
			// go left if value is less than split
			idx = -1;
			split = float.NaN;
			children = null;
		}

		public DecisionTreeNode(int label, float prob)
			: this()
		{
			idx = label;
			split = prob;
		}

		public DecisionTreeNode(int idx, float split, Edu.Stanford.Nlp.Coref.Hybrid.RF.DecisionTreeNode[] children)
		{
			this.idx = idx;
			this.split = split;
			this.children = children;
		}

		public virtual bool IsLeaf()
		{
			return (children == null);
		}
	}
}
