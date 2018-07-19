using Edu.Stanford.Nlp.Ling;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This class implements a <code>TreeReaderFactory</code> that produces
	/// labeled, scored array-based Trees, which have been cleaned up to
	/// delete empties, etc.
	/// </summary>
	/// <remarks>
	/// This class implements a <code>TreeReaderFactory</code> that produces
	/// labeled, scored array-based Trees, which have been cleaned up to
	/// delete empties, etc.  This seems to be a common case.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <version>2000/12/29</version>
	public class StringLabeledScoredTreeReaderFactory : ITreeReaderFactory
	{
		/// <summary>An implementation of the <code>TreeReaderFactory</code> interface.</summary>
		/// <remarks>
		/// An implementation of the <code>TreeReaderFactory</code> interface.
		/// It creates a simple <code>TreeReader</code> which literally
		/// reproduces trees in the treebank as <code>LabeledScoredTree</code>
		/// objects, with <code>StringLabel</code> labels.
		/// </remarks>
		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			return new PennTreeReader(@in, new LabeledScoredTreeFactory(new StringLabelFactory()));
		}
	}
}
