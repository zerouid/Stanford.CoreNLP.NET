using Edu.Stanford.Nlp.Coref.Data;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Docreader
{
	public interface IDocReader
	{
		/// <summary>Read raw, CoNLL, ACE, or MUC document and return InputDoc</summary>
		InputDoc NextDoc();

		void Reset();
	}
}
