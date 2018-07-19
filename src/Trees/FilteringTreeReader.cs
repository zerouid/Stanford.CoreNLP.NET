using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A <code>FilteringTreeReader</code> filters the output of another TreeReader.</summary>
	/// <remarks>
	/// A <code>FilteringTreeReader</code> filters the output of another TreeReader.
	/// It applies a Filter&lt;Tree&gt; to each returned tree and only returns trees
	/// that are accepted by the Filter.  The Filter should accept trees that it
	/// wants returned.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <version>2006/11</version>
	public class FilteringTreeReader : ITreeReader
	{
		private ITreeReader tr;

		private IPredicate<Tree> f;

		public FilteringTreeReader(ITreeReader tr, IPredicate<Tree> f)
		{
			this.tr = tr;
			this.f = f;
		}

		/// <summary>Reads a single tree.</summary>
		/// <returns>A single tree, or <code>null</code> at end of file.</returns>
		/// <exception cref="System.IO.IOException"/>
		public virtual Tree ReadTree()
		{
			Tree t;
			do
			{
				t = tr.ReadTree();
			}
			while (t != null && !f.Test(t));
			return t;
		}

		/// <summary>Close the Reader behind this <code>TreeReader</code>.</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual void Close()
		{
			tr.Close();
		}
	}
}
