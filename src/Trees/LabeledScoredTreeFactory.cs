using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>LabeledScoredTreeFactory</code> acts as a factory for creating
	/// trees with labels and scores.
	/// </summary>
	/// <remarks>
	/// A <code>LabeledScoredTreeFactory</code> acts as a factory for creating
	/// trees with labels and scores.  Unless another <code>LabelFactory</code>
	/// is supplied, it will use a <code>CoreLabel</code> by default.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class LabeledScoredTreeFactory : SimpleTreeFactory
	{
		private ILabelFactory lf;

		/// <summary>Make a TreeFactory that produces LabeledScoredTree trees.</summary>
		/// <remarks>
		/// Make a TreeFactory that produces LabeledScoredTree trees.
		/// The labels are of class <code>CoreLabel</code>.
		/// </remarks>
		public LabeledScoredTreeFactory()
			: this(CoreLabel.Factory())
		{
		}

		/// <summary>
		/// Make a TreeFactory that uses LabeledScoredTree trees, where the
		/// labels are as specified by the user.
		/// </summary>
		/// <param name="lf">the <code>LabelFactory</code> to be used to create labels</param>
		public LabeledScoredTreeFactory(ILabelFactory lf)
		{
			this.lf = lf;
		}

		public override Tree NewLeaf(string word)
		{
			return new LabeledScoredTreeNode(lf.NewLabel(word));
		}

		/// <summary>Create a new leaf node with the given label</summary>
		/// <param name="label">the label for the leaf node</param>
		/// <returns>A new tree leaf</returns>
		public override Tree NewLeaf(ILabel label)
		{
			return new LabeledScoredTreeNode(lf.NewLabel(label));
		}

		public override Tree NewTreeNode(string parent, IList<Tree> children)
		{
			return new LabeledScoredTreeNode(lf.NewLabel(parent), children);
		}

		/// <summary>Create a new non-leaf tree node with the given label</summary>
		/// <param name="parentLabel">The label for the node</param>
		/// <param name="children">
		/// A <code>List</code> of the children of this node,
		/// each of which should itself be a <code>LabeledScoredTree</code>
		/// </param>
		/// <returns>A new internal tree node</returns>
		public override Tree NewTreeNode(ILabel parentLabel, IList<Tree> children)
		{
			return new LabeledScoredTreeNode(lf.NewLabel(parentLabel), children);
		}
	}
}
