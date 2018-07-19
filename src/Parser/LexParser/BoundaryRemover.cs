using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// Removes a boundary symbol (Lexicon.BOUNDARY_TAG or Lexicon.BOUNDARY), which
	/// is the rightmost daughter of a tree.
	/// </summary>
	/// <remarks>
	/// Removes a boundary symbol (Lexicon.BOUNDARY_TAG or Lexicon.BOUNDARY), which
	/// is the rightmost daughter of a tree.  Otherwise does nothing.
	/// This is needed because the dependency parser uses such symbols.
	/// <p/>
	/// <i>Note:</i> This method is a function and not destructive. A new root tree is returned.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class BoundaryRemover : ITreeTransformer
	{
		public BoundaryRemover()
		{
		}

		public virtual Tree TransformTree(Tree tree)
		{
			Tree last = tree.LastChild();
			if (last.Label().Value().Equals(LexiconConstants.BoundaryTag) || last.Label().Value().Equals(LexiconConstants.Boundary))
			{
				IList<Tree> childList = tree.GetChildrenAsList();
				IList<Tree> lastGoneList = childList.SubList(0, childList.Count - 1);
				return tree.TreeFactory().NewTreeNode(tree.Label(), lastGoneList);
			}
			else
			{
				return tree;
			}
		}
	}
}
