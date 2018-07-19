using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>TreeReaderFactory</code> is a factory for creating objects of
	/// class <code>TreeReader</code>, or some descendant class.
	/// </summary>
	/// <author>Christopher Manning</author>
	public interface ITreeReaderFactory
	{
		/// <summary>
		/// Create a new <code>TreeReader</code> using the provided
		/// <code>Reader</code>.
		/// </summary>
		/// <param name="in">The <code>Reader</code> to build on</param>
		/// <returns>The new TreeReader</returns>
		ITreeReader NewTreeReader(Reader @in);
	}
}
