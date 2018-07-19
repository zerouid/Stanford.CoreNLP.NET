using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>Adjoin in a tree (like in TAG), but retain the target of adjunction as the root of the auxiliary tree.</summary>
	/// <author>Roger Levy (rog@nlp.stanford.edu)</author>
	internal class AdjoinToHeadNode : AdjoinNode
	{
		public AdjoinToHeadNode(AuxiliaryTree t, TsurgeonPattern p)
			: base("adjoinH", t, p)
		{
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new AdjoinToHeadNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(AdjoinToHeadNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				// find match
				Tree targetNode = this.childMatcher[0].Evaluate(tree, tregex);
				// put children underneath target in foot of auxilary tree
				AuxiliaryTree ft = this._enclosing.AdjunctionTree().Copy(this, tree.TreeFactory(), tree.Label().LabelFactory());
				ft.foot.SetChildren(targetNode.GetChildrenAsList());
				// put children of auxiliary tree under target.  root of auxiliary tree is ignored.  root of original is maintained.
				targetNode.SetChildren(ft.tree.GetChildrenAsList());
				return tree;
			}

			private readonly AdjoinToHeadNode _enclosing;
		}
	}
}
