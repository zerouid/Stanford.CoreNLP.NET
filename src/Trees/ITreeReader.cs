using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>TreeReader</code> adds functionality to another <code>Reader</code>
	/// by reading in Trees, or some descendant class.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <author>Roger Levy (mod. 2003/01)</author>
	/// <version>2003/01</version>
	public interface ITreeReader : ICloseable
	{
		/// <summary>Reads a single tree.</summary>
		/// <returns>A single tree, or <code>null</code> at end of file.</returns>
		/// <exception cref="System.IO.IOException">If I/O problem</exception>
		Tree ReadTree();

		/// <summary>Close the Reader behind this <code>TreeReader</code>.</summary>
		/// <exception cref="System.IO.IOException"/>
		void Close();
	}
}
