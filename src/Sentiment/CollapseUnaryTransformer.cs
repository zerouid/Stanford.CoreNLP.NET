using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Sentiment
{
	/// <summary>
	/// This transformer collapses chains of unary nodes so that the top
	/// node is the only node left.
	/// </summary>
	/// <remarks>
	/// This transformer collapses chains of unary nodes so that the top
	/// node is the only node left.  The Sentiment model does not handle
	/// unary nodes, so this simplifies them to make a binary tree consist
	/// entirely of binary nodes and preterminals.  A new tree with new
	/// nodes and labels is returned; the original tree is unchanged.
	/// </remarks>
	/// <author>John Bauer</author>
	public class CollapseUnaryTransformer : ITreeTransformer
	{
		public virtual Tree TransformTree(Tree tree)
		{
			if (tree.IsPreTerminal() || tree.IsLeaf())
			{
				return tree.DeepCopy();
			}
			ILabel label = tree.Label().LabelFactory().NewLabel(tree.Label());
			Tree[] children = tree.Children();
			while (children.Length == 1 && !children[0].IsLeaf())
			{
				children = children[0].Children();
			}
			IList<Tree> processedChildren = Generics.NewArrayList();
			foreach (Tree child in children)
			{
				processedChildren.Add(TransformTree(child));
			}
			return tree.TreeFactory().NewTreeNode(label, processedChildren);
		}
	}
}
