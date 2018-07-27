using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;




namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A
	/// <c>LabeledScoredTreeNode</c>
	/// represents a tree composed of a root
	/// label, a score,
	/// and an array of daughter parse trees.  A parse tree derived from a rule
	/// provides information about the category of the root as well as a composite
	/// of the daughter categories.
	/// </summary>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class LabeledScoredTreeNode : Tree
	{
		private const long serialVersionUID = -8992385140984593817L;

		/// <summary>Label of the parse tree.</summary>
		private ILabel label;

		/// <summary>
		/// Score of
		/// <c>TreeNode</c>
		/// </summary>
		private double score = double.NaN;

		/// <summary>Daughters of the parse tree.</summary>
		private Tree[] daughterTrees;

		/// <summary>Create an empty parse tree.</summary>
		public LabeledScoredTreeNode()
		{
			// = null;
			// = null;
			SetChildren(EmptyTreeArray);
		}

		/// <summary>Create a leaf parse tree with given word.</summary>
		/// <param name="label">
		/// the
		/// <c>Label</c>
		/// representing the <i>word</i> for
		/// this new tree leaf.
		/// </param>
		public LabeledScoredTreeNode(ILabel label)
			: this(label, double.NaN)
		{
		}

		/// <summary>Create a leaf parse tree with given word and score.</summary>
		/// <param name="label">
		/// The
		/// <c>Label</c>
		/// representing the <i>word</i> for
		/// </param>
		/// <param name="score">
		/// The score for the node
		/// this new tree leaf.
		/// </param>
		public LabeledScoredTreeNode(ILabel label, double score)
			: this()
		{
			this.label = label;
			this.score = score;
		}

		/// <summary>Create parse tree with given root and array of daughter trees.</summary>
		/// <param name="label">root label of tree to construct.</param>
		/// <param name="daughterTreesList">List of daughter trees to construct.</param>
		public LabeledScoredTreeNode(ILabel label, IList<Tree> daughterTreesList)
		{
			this.label = label;
			SetChildren(daughterTreesList);
		}

		/// <summary>
		/// Returns an array of children for the current node, or null
		/// if it is a leaf.
		/// </summary>
		public override Tree[] Children()
		{
			return daughterTrees;
		}

		/// <summary>
		/// Sets the children of this
		/// <c>Tree</c>
		/// .  If given
		/// <see langword="null"/>
		/// , this method sets the Tree's children to
		/// the canonical zero-length Tree[] array.
		/// </summary>
		/// <param name="children">An array of child trees</param>
		public override void SetChildren(Tree[] children)
		{
			if (children == null)
			{
				daughterTrees = EmptyTreeArray;
			}
			else
			{
				daughterTrees = children;
			}
		}

		/// <summary>
		/// Returns the label associated with the current node, or null
		/// if there is no label
		/// </summary>
		public override ILabel Label()
		{
			return label;
		}

		/// <summary>Sets the label associated with the current node, if there is one.</summary>
		public override void SetLabel(ILabel label)
		{
			this.label = label;
		}

		/// <summary>
		/// Returns the score associated with the current node, or Nan
		/// if there is no score
		/// </summary>
		public override double Score()
		{
			return score;
		}

		/// <summary>Sets the score associated with the current node, if there is one</summary>
		public override void SetScore(double score)
		{
			this.score = score;
		}

		/// <summary>
		/// Return a
		/// <c>TreeFactory</c>
		/// that produces trees of the
		/// same type as the current
		/// <c>Tree</c>
		/// .  That is, this
		/// implementation, will produce trees of type
		/// <c>LabeledScoredTree(Node|Leaf)</c>
		/// .
		/// The
		/// <c>Label</c>
		/// of
		/// <c>this</c>
		/// is examined, and providing it is not
		/// <see langword="null"/>
		/// , a
		/// <c>LabelFactory</c>
		/// which will produce that kind of
		/// <c>Label</c>
		/// is supplied to the
		/// <c>TreeFactory</c>
		/// .
		/// If the
		/// <c>Label</c>
		/// is
		/// <see langword="null"/>
		/// , a
		/// <c>StringLabelFactory</c>
		/// will be used.
		/// The factories returned on different calls a different: a new one is
		/// allocated each time.
		/// </summary>
		/// <returns>a factory to produce labeled, scored trees</returns>
		public override ITreeFactory TreeFactory()
		{
			ILabelFactory lf = (Label() == null) ? CoreLabel.Factory() : Label().LabelFactory();
			return new LabeledScoredTreeFactory(lf);
		}

		private class TreeFactoryHolder
		{
			internal static readonly ITreeFactory tf = new LabeledScoredTreeFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// Return a
		/// <c>TreeFactory</c>
		/// that produces trees of the
		/// <c/>
		/// LabeledScoredTree{Node|Leaf}} type.
		/// The factory returned is always the same one (a singleton).
		/// </summary>
		/// <returns>a factory to produce labeled, scored trees</returns>
		public static ITreeFactory Factory()
		{
			return LabeledScoredTreeNode.TreeFactoryHolder.tf;
		}

		/// <summary>
		/// Return a
		/// <c>TreeFactory</c>
		/// that produces trees of the
		/// <c/>
		/// LabeledScoredTree{Node|Leaf}} type, with
		/// the
		/// <c>Label</c>
		/// made with the supplied
		/// <c>LabelFactory</c>
		/// .
		/// The factory returned is a different one each time
		/// </summary>
		/// <param name="lf">The LabelFactory to use</param>
		/// <returns>a factory to produce labeled, scored trees</returns>
		public static ITreeFactory Factory(ILabelFactory lf)
		{
			return new LabeledScoredTreeFactory(lf);
		}

		private static readonly NumberFormat nf = new DecimalFormat("0.000");

		public override string NodeString()
		{
			StringBuilder buff = new StringBuilder();
			buff.Append(base.NodeString());
			if (!double.IsNaN(score))
			{
				buff.Append(" [").Append(nf.Format(-score)).Append(']');
			}
			return buff.ToString();
		}
	}
}
