using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>Pruning differs from deleting in that if a non-terminal node winds up having no children, it is pruned as well.</summary>
	/// <author>Roger Levy (rog@nlp.stanford.edu)</author>
	internal class PruneNode : TsurgeonPattern
	{
		public PruneNode(TsurgeonPattern[] children)
			: base("prune", children)
		{
		}

		public PruneNode(IList<TsurgeonPattern> children)
			: this(Sharpen.Collections.ToArray(children, new TsurgeonPattern[children.Count]))
		{
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new PruneNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(PruneNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				bool prunedWholeTree = false;
				foreach (TsurgeonMatcher child in this.childMatcher)
				{
					Tree nodeToPrune = child.Evaluate(tree, tregex);
					if (PruneNode.PruneHelper(tree, nodeToPrune) == null)
					{
						prunedWholeTree = true;
					}
				}
				return prunedWholeTree ? null : tree;
			}

			private readonly PruneNode _enclosing;
		}

		private static Tree PruneHelper(Tree root, Tree nodeToPrune)
		{
			if (nodeToPrune == root)
			{
				return null;
			}
			Tree parent = nodeToPrune.Parent(root);
			parent.RemoveChild(Edu.Stanford.Nlp.Trees.Trees.ObjectEqualityIndexOf(parent, nodeToPrune));
			if (parent.Children().Length == 0)
			{
				return PruneHelper(root, parent);
			}
			return root;
		}
	}
}
