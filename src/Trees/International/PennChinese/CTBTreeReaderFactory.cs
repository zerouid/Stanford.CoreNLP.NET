using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>
	/// The
	/// <c>CTBTreeReaderFactory</c>
	/// is a factory for creating a
	/// TreeReader suitable for the Penn Chinese Treebank (CTB).
	/// It knows how to ignore the SGML tags in those files.
	/// The default reader doesn't delete empty nodes, but an
	/// additional static class is provided whose default constructor
	/// does give a TreeReader that deletes empty nodes in trees.
	/// </summary>
	/// <author>Christopher Manning</author>
	public class CTBTreeReaderFactory : ITreeReaderFactory
	{
		private readonly TreeNormalizer tn;

		private readonly bool discardFrags;

		public CTBTreeReaderFactory()
			: this(new TreeNormalizer())
		{
		}

		public CTBTreeReaderFactory(TreeNormalizer tn)
			: this(tn, false)
		{
		}

		public CTBTreeReaderFactory(TreeNormalizer tn, bool discardFrags)
		{
			this.tn = tn;
			this.discardFrags = discardFrags;
		}

		/// <summary>
		/// Create a new
		/// <c>TreeReader</c>
		/// using the provided
		/// <c>Reader</c>
		/// .
		/// </summary>
		/// <param name="in">
		/// The
		/// <c>Reader</c>
		/// to build on
		/// </param>
		/// <returns>The new TreeReader</returns>
		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			if (discardFrags)
			{
				return new FragDiscardingPennTreeReader(@in, new LabeledScoredTreeFactory(), tn, new CHTBTokenizer(@in));
			}
			else
			{
				return new PennTreeReader(@in, new LabeledScoredTreeFactory(), tn, new CHTBTokenizer(@in));
			}
		}

		public class NoEmptiesCTBTreeReaderFactory : CTBTreeReaderFactory
		{
			public NoEmptiesCTBTreeReaderFactory()
				: base(new BobChrisTreeNormalizer())
			{
			}
		}
		// end static class NoEmptiesCTBTreeReaderFactory
	}
}
