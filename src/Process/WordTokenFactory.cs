using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Constructs a Word from a String.</summary>
	/// <remarks>
	/// Constructs a Word from a String. This is the default
	/// TokenFactory for PTBLexer. It discards the positional information.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public class WordTokenFactory : ILexedTokenFactory<Word>
	{
		public virtual Word MakeToken(string str, int begin, int length)
		{
			return new Word(str, begin, begin + length);
		}
	}
}
