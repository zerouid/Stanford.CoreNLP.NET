using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Tokenizers break up text into individual Objects.</summary>
	/// <remarks>
	/// Tokenizers break up text into individual Objects. These objects may be
	/// Strings, Words, or other Objects.  A Tokenizer extends the Iterator
	/// interface, but provides a lookahead operation
	/// <c>peek()</c>
	/// .  An
	/// implementation of this interface is expected to have a constructor that
	/// takes a single argument, a Reader.
	/// </remarks>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	public interface ITokenizer<T> : IEnumerator<T>
	{
		/* Provides the standard Iterator methods: next(), hasNext().
		* remove() will normally not be implemented.
		*/
		/// <summary>
		/// Returns the next token, without removing it, from the Tokenizer, so
		/// that the same token will be again returned on the next call to
		/// next() or peek().
		/// </summary>
		/// <returns>the next token in the token stream.</returns>
		/// <exception cref="Java.Util.NoSuchElementException">If the token stream has no more tokens.</exception>
		T Peek();

		/// <summary>Returns all tokens of this Tokenizer as a List for convenience.</summary>
		/// <returns>A list of all the tokens</returns>
		IList<T> Tokenize();
	}
}
