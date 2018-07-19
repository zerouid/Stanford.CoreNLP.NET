using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A class for tree normalization.</summary>
	/// <remarks>
	/// A class for tree normalization.  The default one does no normalization.
	/// Other tree normalizers will change various node labels, or perhaps the
	/// whole tree geometry (by doing such things as deleting functional tags or
	/// empty elements).  Another operation that a
	/// <c>TreeNormalizer</c>
	/// may wish to perform is interning the
	/// <c>String</c>
	/// s passed to
	/// it.  Can be reused as a Singleton.  Designed to be extended.
	/// <p/>
	/// The
	/// <c>TreeNormalizer</c>
	/// methods are in two groups.
	/// The contract for this class is that first normalizeTerminal or
	/// normalizeNonterminal will be called on each
	/// <c>String</c>
	/// that will
	/// be put into a
	/// <c>Tree</c>
	/// , when they are read from files or
	/// otherwise created.  Then
	/// <c>normalizeWholeTree</c>
	/// will
	/// be called on the
	/// <c>Tree</c>
	/// .  It normally walks the
	/// <c>Tree</c>
	/// making whatever modifications it wishes to. A
	/// <c>TreeNormalizer</c>
	/// need not make a deep copy of a
	/// <c>Tree</c>
	/// .  It is assumed to be able to work destructively,
	/// because afterwards we will only use the normalized
	/// <c>Tree</c>
	/// .
	/// <p/>
	/// <i>Implementation note:</i> This is a very old legacy class used in conjunction
	/// with PennTreeReader.  It seems now that it would be better to move the
	/// String normalization into the tokenizer, and then we are just left with a
	/// (possibly destructive) TreeTransformer.
	/// </remarks>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class TreeNormalizer
	{
		public TreeNormalizer()
		{
		}

		/// <summary>Normalizes a leaf contents (and maybe intern it).</summary>
		/// <param name="leaf">The String that decorates the leaf</param>
		/// <returns>The normalized form of this leaf String</returns>
		public virtual string NormalizeTerminal(string leaf)
		{
			return leaf;
		}

		/// <summary>Normalizes a nonterminal contents (and maybe intern it).</summary>
		/// <param name="category">The String that decorates this nonterminal node</param>
		/// <returns>The normalized form of this nonterminal String</returns>
		public virtual string NormalizeNonterminal(string category)
		{
			return category;
		}

		/// <summary>
		/// Normalize a whole tree -- this method assumes that the argument
		/// that it is passed is the root of a complete
		/// <c>Tree</c>
		/// .
		/// It is normally implemented as a Tree-walking routine.
		/// <p>
		/// This method may return
		/// <see langword="null"/>
		/// . This is interpreted to
		/// mean that this is a tree that should not be included in further
		/// processing.  PennTreeReader recognizes this return value, and
		/// asks for another Tree from the input Reader.
		/// </summary>
		/// <param name="tree">The tree to be normalized</param>
		/// <param name="tf">the TreeFactory to create new nodes (if needed)</param>
		/// <returns>The normalized tree. May be null which means to not use this tree at all.</returns>
		public virtual Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			return tree;
		}

		private const long serialVersionUID = 1540681875853883387L;
	}
}
