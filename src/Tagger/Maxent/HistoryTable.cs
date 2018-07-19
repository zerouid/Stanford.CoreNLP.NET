using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>
	/// <i>Notes:</i> This maintains a two way lookup between a History and
	/// an Integer index.
	/// </summary>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class HistoryTable
	{
		private const int capacity = 1000000;

		private readonly IIndex<History> idx;

		public HistoryTable()
		{
			// todo cdm: just remove this class and use the Index<History> directly where uses of it appears?
			idx = new HashIndex<History>(capacity);
		}

		internal virtual void Release()
		{
			idx.Clear();
		}

		internal virtual int Add(History h)
		{
			return idx.AddToIndex(h);
		}

		internal virtual History GetHistory(int index)
		{
			return idx.Get(index);
		}

		internal virtual int GetIndex(History h)
		{
			return idx.IndexOf(h);
		}

		internal virtual int Size()
		{
			return idx.Size();
		}
	}
}
