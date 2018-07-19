using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Debinarizes a binary tree from the parser.</summary>
	/// <remarks>
	/// Debinarizes a binary tree from the parser.
	/// Node values with a '@' in them anywhere are assumed to be inserted
	/// nodes for the purpose of binarization, and are removed.
	/// The code also removes the last child of the root node, assuming
	/// that it is an inserted dependency root.
	/// </remarks>
	public class Debinarizer : ITreeTransformer
	{
		private readonly ITreeFactory tf;

		private readonly bool forceCNF;

		private readonly ITreeTransformer boundaryRemover;

		protected internal virtual Tree TransformTreeHelper(Tree t)
		{
			if (t.IsLeaf())
			{
				Tree leaf = tf.NewLeaf(t.Label());
				leaf.SetScore(t.Score());
				return leaf;
			}
			IList<Tree> newChildren = new List<Tree>();
			for (int childNum = 0; childNum < numKids; childNum++)
			{
				Tree child = t.GetChild(childNum);
				Tree newChild = TransformTreeHelper(child);
				if ((!newChild.IsLeaf()) && newChild.Label().Value().IndexOf('@') >= 0)
				{
					Sharpen.Collections.AddAll(newChildren, newChild.GetChildrenAsList());
				}
				else
				{
					newChildren.Add(newChild);
				}
			}
			Tree node = tf.NewTreeNode(t.Label(), newChildren);
			node.SetScore(t.Score());
			return node;
		}

		public virtual Tree TransformTree(Tree t)
		{
			Tree result = TransformTreeHelper(t);
			if (forceCNF)
			{
				result = new CNFTransformers.FromCNFTransformer().TransformTree(result);
			}
			return boundaryRemover.TransformTree(result);
		}

		public Debinarizer(bool forceCNF)
			: this(forceCNF, CoreLabel.Factory())
		{
		}

		public Debinarizer(bool forceCNF, ILabelFactory lf)
		{
			this.forceCNF = forceCNF;
			tf = new LabeledScoredTreeFactory(lf);
			boundaryRemover = new BoundaryRemover();
		}
	}
}
