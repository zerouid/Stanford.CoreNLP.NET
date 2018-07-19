using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <author>Roger Levy (rog@stanford.edu)</author>
	internal class DeleteNode : TsurgeonPattern
	{
		public DeleteNode(TsurgeonPattern[] children)
			: base("delete", children)
		{
		}

		public DeleteNode(IList<TsurgeonPattern> children)
			: this(Sharpen.Collections.ToArray(children, new TsurgeonPattern[children.Count]))
		{
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new DeleteNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(DeleteNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				Tree result = tree;
				foreach (TsurgeonMatcher child in this.childMatcher)
				{
					Tree nodeToDelete = child.Evaluate(tree, tregex);
					if (nodeToDelete == tree)
					{
						result = null;
					}
					Tree parent = nodeToDelete.Parent(tree);
					parent.RemoveChild(Edu.Stanford.Nlp.Trees.Trees.ObjectEqualityIndexOf(parent, nodeToDelete));
				}
				return result;
			}

			private readonly DeleteNode _enclosing;
		}
	}
}
