


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Interface for a Visitor pattern for Files.</summary>
	/// <remarks>
	/// Interface for a Visitor pattern for Files.
	/// This interface is used by some existing code, but new code should
	/// probably use FileArrayList or FileSequentialCollection, which fit
	/// better with the Collections orientation of recent Java releases.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public interface IFileProcessor
	{
		/// <summary>Apply this predicate to a <code>File</code>.</summary>
		/// <remarks>
		/// Apply this predicate to a <code>File</code>.  This method can
		/// assume the <code>file</code> is a file and not a directory.
		/// </remarks>
		/// <seealso cref="FilePathProcessor">for traversing directories</seealso>
		void ProcessFile(File file);
	}
}
