

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// Something that implements the <code>HasWord</code> interface
	/// knows about words.
	/// </summary>
	/// <author>Christopher Manning</author>
	public interface IHasWord
	{
		/// <summary>Return the word value of the label (or null if none).</summary>
		/// <returns>String the word value for the label</returns>
		string Word();

		/// <summary>Set the word value for the label (if one is stored).</summary>
		/// <param name="word">The word value for the label</param>
		void SetWord(string word);
	}
}
