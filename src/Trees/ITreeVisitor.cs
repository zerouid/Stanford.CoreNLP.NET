

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This is a simple strategy-type interface for operations that are applied to
	/// <c>Tree</c>
	/// . It typically is called iteratively over
	/// trees in a
	/// <c>Treebank</c>
	/// .  The convention is for
	/// <c>TreeVisitor</c>
	/// implementing
	/// classes not to affect
	/// <c>Tree</c>
	/// instances they operate on, but to accomplish things via
	/// side effects (like counting statistics over trees, etc.).
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <author>Roger Levy</author>
	public interface ITreeVisitor
	{
		/// <summary>Does whatever one needs to do to a particular parse tree.</summary>
		/// <param name="t">
		/// A tree.  Classes implementing this interface can assume
		/// that the tree passed in is not
		/// <see langword="null"/>
		/// .
		/// </param>
		void VisitTree(Tree t);
	}
}
