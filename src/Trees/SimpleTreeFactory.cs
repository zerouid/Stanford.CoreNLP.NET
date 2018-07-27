using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A
	/// <c>SimpleTreeFactory</c>
	/// acts as a factory for creating objects
	/// of class
	/// <c>SimpleTree</c>
	/// .
	/// <p/>
	/// <i>NB: A SimpleTree stores tree geometries but no node labels.  Make sure
	/// this is what you really want.</i>
	/// </summary>
	/// <author>Christopher Manning</author>
	public class SimpleTreeFactory : ITreeFactory
	{
		/// <summary>Creates a new <code>TreeFactory</code>.</summary>
		/// <remarks>
		/// Creates a new <code>TreeFactory</code>.  A
		/// <code>SimpleTree</code> stores no <code>Label</code>, so no
		/// <code>LabelFactory</code> is built.
		/// </remarks>
		public SimpleTreeFactory()
		{
		}

		public virtual Tree NewLeaf(string word)
		{
			return new SimpleTree();
		}

		public virtual Tree NewLeaf(ILabel word)
		{
			return new SimpleTree();
		}

		public virtual Tree NewTreeNode(string parent, IList<Tree> children)
		{
			return new SimpleTree(null, children);
		}

		public virtual Tree NewTreeNode(ILabel parentLabel, IList<Tree> children)
		{
			return new SimpleTree(parentLabel, children);
		}
	}
}
