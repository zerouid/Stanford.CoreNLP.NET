using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;


namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <author>Roger Levy (rog@stanford.edu)</author>
	internal class HoldTreeNode : TsurgeonPattern
	{
		internal AuxiliaryTree subTree;

		public HoldTreeNode(AuxiliaryTree t)
			: base("hold", TsurgeonPattern.EmptyTsurgeonPatternArray)
		{
			this.subTree = t;
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new HoldTreeNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(HoldTreeNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				return this._enclosing.subTree.Copy(this, tree.TreeFactory(), tree.Label().LabelFactory()).tree;
			}

			private readonly HoldTreeNode _enclosing;
		}

		public override string ToString()
		{
			return subTree.ToString();
		}
	}
}
