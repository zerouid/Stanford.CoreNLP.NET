

namespace Edu.Stanford.Nlp.Ling
{
	/// <author>grenager</author>
	public interface IHasContext
	{
		/// <returns>the String before the word</returns>
		string Before();

		/// <summary>Set the whitespace String before the word.</summary>
		/// <param name="before">the whitespace String before the word</param>
		void SetBefore(string before);

		/// <summary>Return the String which is the original character sequence of the token.</summary>
		/// <returns>The original character sequence of the token</returns>
		string OriginalText();

		/// <summary>Set the String which is the original character sequence of the token.</summary>
		/// <param name="originalText">The original character sequence of the token</param>
		void SetOriginalText(string originalText);

		/// <summary>Return the whitespace String after the word.</summary>
		/// <returns>The whitespace String after the word</returns>
		string After();

		/// <summary>Set the whitespace String after the word.</summary>
		/// <param name="after">The whitespace String after the word</param>
		void SetAfter(string after);
	}
}
