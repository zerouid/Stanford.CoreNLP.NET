

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>An interface for finding the "head" daughter of a phrase structure tree.</summary>
	/// <remarks>
	/// An interface for finding the "head" daughter of a phrase structure tree.
	/// This could potentially be any sense of "head", but has mainly been used
	/// to find the lexical head for lexicalized PCFG parsing.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public interface IHeadFinder
	{
		/// <summary>Determine which daughter of the current parse tree is the head.</summary>
		/// <param name="t">The parse tree to examine the daughters of</param>
		/// <returns>
		/// The daughter tree that is the head.  This will always be
		/// non-null. An Exception will be thrown if no head can be determined.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// If a subclass has missing or badly
		/// formatted head rule data
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// If the argument Tree has unexpected
		/// phrasal categories in it (and the implementation doesn't just use
		/// some heuristic to always determine some head).
		/// </exception>
		Tree DetermineHead(Tree t);

		/// <summary>
		/// Determine which daughter of the current parse tree is the head
		/// given the parent of the tree.
		/// </summary>
		/// <param name="t">The parse tree to examine the daughters of</param>
		/// <param name="parent">The parent of tree t</param>
		/// <returns>
		/// The daughter tree that is the head.  This will always be
		/// non-null. An Exception will be thrown if no head can be determined.
		/// </returns>
		Tree DetermineHead(Tree t, Tree parent);
	}
}
