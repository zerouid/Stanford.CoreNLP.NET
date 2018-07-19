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
	/// delete empties, etc.   This seems to be a common case (for English).
	/// By default, the labels are of type CategoryWordTag,
	/// but a different Label type can be specified by the user.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class LabeledScoredTreeReaderFactory : ITreeReaderFactory
	{
		private readonly ILabelFactory lf;

		private readonly TreeNormalizer tm;

		/// <summary>Create a new TreeReaderFactory with CoreLabel labels.</summary>
		public LabeledScoredTreeReaderFactory()
		{
			lf = CoreLabel.Factory();
			tm = new BobChrisTreeNormalizer();
		}

		public LabeledScoredTreeReaderFactory(ILabelFactory lf)
		{
			this.lf = lf;
			tm = new BobChrisTreeNormalizer();
		}

		public LabeledScoredTreeReaderFactory(TreeNormalizer tm)
		{
			lf = CoreLabel.Factory();
			this.tm = tm;
		}

		public LabeledScoredTreeReaderFactory(ILabelFactory lf, TreeNormalizer tm)
		{
			this.lf = lf;
			this.tm = tm;
		}

		/// <summary>An implementation of the <code>TreeReaderFactory</code> interface.</summary>
		/// <remarks>
		/// An implementation of the <code>TreeReaderFactory</code> interface.
		/// It creates a <code>TreeReader</code> which normalizes trees using
		/// the <code>BobChrisTreeNormalizer</code>, and makes
		/// <code>LabeledScoredTree</code> objects with
		/// <code>CategoryWordTag</code> labels (unless otherwise specified on
		/// construction).
		/// </remarks>
		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			return new PennTreeReader(@in, new LabeledScoredTreeFactory(lf), tm);
		}
	}
}
