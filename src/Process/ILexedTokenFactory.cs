

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// Constructs a token (of arbitrary type) from a String and its position
	/// in the underlying text.
	/// </summary>
	/// <remarks>
	/// Constructs a token (of arbitrary type) from a String and its position
	/// in the underlying text.  This is used to create tokens in JFlex lexers
	/// such as PTBTokenizer.
	/// </remarks>
	public interface ILexedTokenFactory<T>
	{
		/// <summary>
		/// Constructs a token (of arbitrary type) from a String and its position
		/// in the underlying text.
		/// </summary>
		/// <remarks>
		/// Constructs a token (of arbitrary type) from a String and its position
		/// in the underlying text. The int arguments are used just to record token
		/// character offsets in an underlying text. This method does not take
		/// a substring of
		/// <paramref name="str"/>
		/// .
		/// </remarks>
		/// <param name="str">The String extracted by the lexer.</param>
		/// <param name="begin">The offset in the document of the first character in this string.</param>
		/// <param name="length">The number of characters the string takes up in the document.</param>
		/// <returns>The token of type T.</returns>
		T MakeToken(string str, int begin, int length);
	}
}
