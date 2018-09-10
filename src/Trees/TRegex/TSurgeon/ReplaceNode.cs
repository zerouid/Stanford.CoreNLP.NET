using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <author>Roger Levy (rog@stanford.edu)</author>
	internal class ReplaceNode : TsurgeonPattern
	{
		public ReplaceNode(TsurgeonPattern oldNode, params TsurgeonPattern[] newNodes)
			: base("replace", ArrayUtils.Concatenate(new TsurgeonPattern[] { oldNode }, newNodes))
		{
		}

		public ReplaceNode(TsurgeonPattern oldNode, IList<AuxiliaryTree> trees)
			: this(oldNode, Sharpen.Collections.ToArray(CollectionUtils.TransformAsList(trees, convertAuxiliaryToHold), new TsurgeonPattern[trees.Count]))
		{
		}

		private static readonly Func<AuxiliaryTree, HoldTreeNode> convertAuxiliaryToHold = null;

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new ReplaceNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(ReplaceNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				Tree oldNode = this.childMatcher[0].Evaluate(tree, tregex);
				if (oldNode == tree)
				{
					if (this._enclosing.children.Length > 2)
					{
						throw new TsurgeonRuntimeException("Attempted to replace a root node with more than one node, unable to proceed");
					}
					return this.childMatcher[1].Evaluate(tree, tregex);
				}
				Tree parent = oldNode.Parent(tree);
				int i = parent.ObjectIndexOf(oldNode);
				parent.RemoveChild(i);
				for (int j = 1; j < this._enclosing.children.Length; ++j)
				{
					Tree newNode = this.childMatcher[j].Evaluate(tree, tregex);
					parent.InsertDtr(newNode.DeepCopy(), i + j - 1);
				}
				return tree;
			}

			private readonly ReplaceNode _enclosing;
		}
	}
}
