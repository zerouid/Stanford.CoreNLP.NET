using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>
	/// A CTB TreeReaderFactory that deletes empty nodes, and makes some corrections
	/// to trees while reading them in.
	/// </summary>
	/// <author>Christopher Manning</author>
	public class NoEmptiesCTBTreeReaderFactory : CTBTreeReaderFactory
	{
		public NoEmptiesCTBTreeReaderFactory()
			: base(new CTBErrorCorrectingTreeNormalizer(false, false, false, false), false)
		{
		}
	}
}
