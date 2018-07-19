using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>Adjoin in a tree (like in TAG).</summary>
	/// <author>Roger Levy (rog@nlp.stanford.edu)</author>
	internal class AdjoinNode : TsurgeonPattern
	{
		private readonly AuxiliaryTree adjunctionTree;

		public AdjoinNode(AuxiliaryTree t, TsurgeonPattern p)
			: this("adjoin", t, p)
		{
		}

		public AdjoinNode(string name, AuxiliaryTree t, TsurgeonPattern p)
			: base(name, new TsurgeonPattern[] { p })
		{
			if (t == null || p == null)
			{
				throw new ArgumentNullException("AdjoinNode: illegal null argument, t=" + t + ", p=" + p);
			}
			adjunctionTree = t;
		}

		protected internal virtual AuxiliaryTree AdjunctionTree()
		{
			return adjunctionTree;
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new AdjoinNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(AdjoinNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				// find match and get its parent
				Tree targetNode = this.childMatcher[0].Evaluate(tree, tregex);
				Tree parent = targetNode.Parent(tree);
				// put children underneath target in foot of auxilary tree
				AuxiliaryTree ft = this._enclosing.adjunctionTree.Copy(this, tree.TreeFactory(), tree.Label().LabelFactory());
				ft.foot.SetChildren(targetNode.GetChildrenAsList());
				// replace match with root of auxiliary tree
				if (parent == null)
				{
					return ft.tree;
				}
				else
				{
					int i = parent.ObjectIndexOf(targetNode);
					parent.SetChild(i, ft.tree);
					return tree;
				}
			}

			private readonly AdjoinNode _enclosing;
		}

		public override string ToString()
		{
			return base.ToString() + "<-" + adjunctionTree.ToString();
		}
	}
}
