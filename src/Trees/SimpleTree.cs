using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>SimpleTree</code> is a minimal concrete implementation of an
	/// unlabeled, unscored <code>Tree</code>.
	/// </summary>
	/// <remarks>
	/// A <code>SimpleTree</code> is a minimal concrete implementation of an
	/// unlabeled, unscored <code>Tree</code>.  It has a tree structure, but
	/// nothing is stored at a node (no label or score).
	/// So, most of the time, this is the wrong class to use!
	/// Look at
	/// <c>LabeledScoredTreeNode</c>
	/// .
	/// </remarks>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class SimpleTree : Tree
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.SimpleTree));

		private const long serialVersionUID = -8075763706877132926L;

		/// <summary>Daughters of the parse tree.</summary>
		private Tree[] daughterTrees;

		/// <summary>Create an empty parse tree.</summary>
		public SimpleTree()
		{
			daughterTrees = EmptyTreeArray;
		}

		/// <summary>Create parse tree with given root and null daughters.</summary>
		/// <param name="label">
		/// root label of new tree to construct.  For a SimpleTree
		/// this parameter is ignored.
		/// </param>
		public SimpleTree(ILabel label)
			: this()
		{
		}

		/// <summary>Create parse tree with given root and array of daughter trees.</summary>
		/// <param name="label">
		/// root label of tree to construct.  For a SimpleTree
		/// this parameter is ignored
		/// </param>
		/// <param name="daughterTreesList">list of daughter trees to construct.</param>
		public SimpleTree(ILabel label, IList<Tree> daughterTreesList)
		{
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

		/// <summary>Sets the children of this <code>Tree</code>.</summary>
		/// <remarks>
		/// Sets the children of this <code>Tree</code>.  If given
		/// <code>null</code>, this method sets the Tree's children to a
		/// unique zero-length Tree[] array.
		/// </remarks>
		/// <param name="children">An array of child trees</param>
		public override void SetChildren(Tree[] children)
		{
			if (children == null)
			{
				log.Info("Warning -- you tried to set the children of a SimpleTree to null.\nYou should be really using a zero-length array instead.");
				daughterTrees = EmptyTreeArray;
			}
			else
			{
				daughterTrees = children;
			}
		}

		private class TreeFactoryHolder
		{
			internal static readonly ITreeFactory tf = new SimpleTreeFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// Return a <code>TreeFactory</code> that produces trees of the
		/// <code>SimpleTree</code> type.
		/// </summary>
		/// <remarks>
		/// Return a <code>TreeFactory</code> that produces trees of the
		/// <code>SimpleTree</code> type.
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>a factory to produce simple (unlabelled) trees</returns>
		public override ITreeFactory TreeFactory()
		{
			return SimpleTree.TreeFactoryHolder.tf;
		}

		/// <summary>
		/// Return a <code>TreeFactory</code> that produces trees of the
		/// <code>SimpleTree</code> type.
		/// </summary>
		/// <remarks>
		/// Return a <code>TreeFactory</code> that produces trees of the
		/// <code>SimpleTree</code> type.
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>a factory to produce simple (unlabelled) trees</returns>
		public static ITreeFactory Factory()
		{
			return SimpleTree.TreeFactoryHolder.tf;
		}
	}
}
