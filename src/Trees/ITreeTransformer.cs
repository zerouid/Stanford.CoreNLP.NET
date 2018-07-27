


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This is a simple interface for a function that alters a
	/// local
	/// <c>Tree</c>
	/// .
	/// </summary>
	/// <author>Christopher Manning.</author>
	public interface ITreeTransformer : IFunction<Tree, Tree>
	{
		/// <summary>Does whatever one needs to do to a particular tree.</summary>
		/// <remarks>
		/// Does whatever one needs to do to a particular tree.
		/// This routine is passed a whole
		/// <c>Tree</c>
		/// , and could itself
		/// work recursively, but the canonical usage is to invoke this method
		/// via the
		/// <c>Tree.transform()</c>
		/// method, which will apply the
		/// transformer in a bottom-up manner to each local
		/// <c>Tree</c>
		/// ,
		/// and hence the implementation of
		/// <c>TreeTransformer</c>
		/// should
		/// merely examine and change a local (one-level)
		/// <c>Tree</c>
		/// .
		/// </remarks>
		/// <param name="t">
		/// A tree.  Classes implementing this interface can assume
		/// that the tree passed in is not
		/// <see langword="null"/>
		/// .
		/// </param>
		/// <returns>
		/// The transformed
		/// <c>Tree</c>
		/// </returns>
		Tree TransformTree(Tree t);

		Tree Apply(Tree t);
	}
}
