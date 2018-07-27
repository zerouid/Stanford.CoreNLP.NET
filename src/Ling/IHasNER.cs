

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>This token is able to produce NER tags</summary>
	/// <author>Gabor Angeli</author>
	public interface IHasNER
	{
		/// <summary>Return the named entity class of the label (or null if none).</summary>
		/// <returns>The NER class for the label</returns>
		string Ner();

		/// <summary>Set the named entity class of the label.</summary>
		/// <param name="ner">The NER class for the label</param>
		void SetNER(string ner);
	}
}
