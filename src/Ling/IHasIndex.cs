

namespace Edu.Stanford.Nlp.Ling
{
	/// <author>grenager</author>
	public interface IHasIndex
	{
		string DocID();

		void SetDocID(string docID);

		int SentIndex();

		void SetSentIndex(int sentIndex);

		int Index();

		void SetIndex(int index);
	}
}
