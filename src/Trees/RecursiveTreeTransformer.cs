using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A tool to recursively alter a tree in various ways.</summary>
	/// <remarks>
	/// A tool to recursively alter a tree in various ways.  For example,
	/// <see cref="BasicCategoryTreeTransformer"/>
	/// 
	/// turns all the non-leaf labels of a tree into their basic categories
	/// given a set of treebank parameters which describe how to turn the
	/// basic categories.
	/// <br />
	/// There are three easy places to override and implement the needed
	/// behavior.  transformTerminalLabel changes the labels of the
	/// terminals, transformNonterminalLabel changes the labels of the
	/// non-terminals, and transformLabel changes all labels.  If the tree
	/// needs to be changed in different ways, transformTerminal or
	/// transformNonterminal can be used instead.
	/// </remarks>
	/// <author>John Bauer</author>
	public abstract class RecursiveTreeTransformer : ITreeTransformer
	{
		public virtual Tree TransformTree(Tree tree)
		{
			return TransformHelper(tree);
		}

		public virtual Tree TransformHelper(Tree tree)
		{
			if (tree.IsLeaf())
			{
				return TransformTerminal(tree);
			}
			else
			{
				return TransformNonterminal(tree);
			}
		}

		public virtual Tree TransformTerminal(Tree tree)
		{
			return tree.TreeFactory().NewLeaf(TransformTerminalLabel(tree));
		}

		public virtual Tree TransformNonterminal(Tree tree)
		{
			IList<Tree> children = new List<Tree>(tree.Children().Length);
			foreach (Tree child in tree.Children())
			{
				children.Add(TransformHelper(child));
			}
			return tree.TreeFactory().NewTreeNode(TransformNonterminalLabel(tree), children);
		}

		public virtual ILabel TransformTerminalLabel(Tree tree)
		{
			return TransformLabel(tree);
		}

		public virtual ILabel TransformNonterminalLabel(Tree tree)
		{
			return TransformLabel(tree);
		}

		public virtual ILabel TransformLabel(Tree tree)
		{
			if (tree.Label() == null)
			{
				return null;
			}
			return tree.Label().LabelFactory().NewLabel(tree.Label());
		}

		public abstract Tree Apply(Tree arg1);
	}
}
