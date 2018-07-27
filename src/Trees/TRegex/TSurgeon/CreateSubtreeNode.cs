using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>
	/// Given the start and end children of a particular node, takes all
	/// children between start and end (including the endpoints) and
	/// combines them in a new node with the given label.
	/// </summary>
	/// <author>John Bauer</author>
	public class CreateSubtreeNode : TsurgeonPattern
	{
		private AuxiliaryTree auxTree;

		public CreateSubtreeNode(TsurgeonPattern start, AuxiliaryTree tree)
			: this(start, null, tree)
		{
		}

		public CreateSubtreeNode(TsurgeonPattern start, TsurgeonPattern end, AuxiliaryTree tree)
			: base("combineSubtrees", (end == null) ? new TsurgeonPattern[] { start } : new TsurgeonPattern[] { start, end })
		{
			this.auxTree = tree;
			FindFoot();
		}

		/// <summary>
		/// We want to support a command syntax where a simple node label can
		/// be given (i.e., without using a tree literal).
		/// </summary>
		/// <remarks>
		/// We want to support a command syntax where a simple node label can
		/// be given (i.e., without using a tree literal).
		/// Check if this syntax is being used, and simulate a foot if so.
		/// </remarks>
		private void FindFoot()
		{
			if (auxTree.foot == null)
			{
				if (!auxTree.tree.IsLeaf())
				{
					throw new TsurgeonParseException("No foot node found for " + auxTree);
				}
				// Pretend this leaf is a foot node
				auxTree.foot = auxTree.tree;
			}
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new CreateSubtreeNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(CreateSubtreeNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			/// <summary>
			/// Combines all nodes between start and end into one subtree, then
			/// replaces those nodes with the new subtree in the corresponding
			/// location under parent
			/// </summary>
			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				Tree startChild = this.childMatcher[0].Evaluate(tree, tregex);
				Tree endChild = (this.childMatcher.Length == 2) ? this.childMatcher[1].Evaluate(tree, tregex) : startChild;
				Tree parent = startChild.Parent(tree);
				// sanity check
				if (parent != endChild.Parent(tree))
				{
					throw new TsurgeonRuntimeException("Parents did not match for trees when applied to " + this);
				}
				AuxiliaryTree treeCopy = this._enclosing.auxTree.Copy(this, tree.TreeFactory(), tree.Label().LabelFactory());
				// Collect all the children of the parent of the node we care
				// about.  If the child is one of the nodes we care about, or
				// between those two nodes, we add it to a list of inner children.
				// When we reach the second endpoint, we turn that list of inner
				// children into a new node using the newly created label.  All
				// other children are kept in an outer list, with the new node
				// added at the appropriate location.
				IList<Tree> children = Generics.NewArrayList();
				IList<Tree> innerChildren = Generics.NewArrayList();
				bool insideSpan = false;
				foreach (Tree child in parent.Children())
				{
					if (child == startChild || child == endChild)
					{
						if (!insideSpan && startChild != endChild)
						{
							insideSpan = true;
							innerChildren.Add(child);
						}
						else
						{
							insideSpan = false;
							innerChildren.Add(child);
							// All children have been collected; place these beneath the foot of the auxiliary tree
							treeCopy.foot.SetChildren(innerChildren);
							children.Add(treeCopy.tree);
						}
					}
					else
					{
						if (insideSpan)
						{
							innerChildren.Add(child);
						}
						else
						{
							children.Add(child);
						}
					}
				}
				parent.SetChildren(children);
				return tree;
			}

			private readonly CreateSubtreeNode _enclosing;
		}
	}
}
