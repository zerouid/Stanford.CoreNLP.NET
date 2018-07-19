using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// Something that implements the <code>HasLemma</code> interface
	/// knows about lemmas.
	/// </summary>
	/// <author>John Bauer</author>
	public interface IHasLemma
	{
		/// <summary>Return the lemma value of the label (or null if none).</summary>
		/// <returns>String the lemma value for the label</returns>
		string Lemma();

		/// <summary>Set the lemma value for the label (if one is stored).</summary>
		/// <param name="lemma">The lemma value for the label</param>
		void SetLemma(string lemma);
	}
}
