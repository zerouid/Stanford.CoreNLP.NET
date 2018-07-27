using Edu.Stanford.Nlp.Objectbank;



namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// A TokenizerFactory is a factory that can build a Tokenizer (an extension of Iterator)
	/// from a java.io.Reader.
	/// </summary>
	/// <remarks>
	/// A TokenizerFactory is a factory that can build a Tokenizer (an extension of Iterator)
	/// from a java.io.Reader.
	/// <i>IMPORTANT NOTE:</i>
	/// A TokenizerFactory should also provide two static methods:
	/// <c>public static TokenizerFactory&lt;? extends HasWord&gt; newTokenizerFactory();</c>
	/// <c>public static TokenizerFactory&lt;Word&gt; newWordTokenizerFactory(String options);</c>
	/// These are expected by certain JavaNLP code (e.g., LexicalizedParser),
	/// which wants to produce a TokenizerFactory by reflection.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <?/>
	public interface ITokenizerFactory<T> : IIteratorFromReaderFactory<T>
	{
		/// <summary>Get a tokenizer for this reader.</summary>
		/// <param name="r">A Reader (which is assumed to already by buffered, if appropriate)</param>
		/// <returns>A Tokenizer</returns>
		ITokenizer<T> GetTokenizer(Reader r);

		/// <summary>Get a tokenizer for this reader.</summary>
		/// <param name="r">A Reader (which is assumed to already by buffered, if appropriate)</param>
		/// <param name="extraOptions">Options for how this tokenizer should behave</param>
		/// <returns>A Tokenizer</returns>
		ITokenizer<T> GetTokenizer(Reader r, string extraOptions);

		/// <summary>Sets default options for how tokenizers built from this factory should behave.</summary>
		/// <param name="options">Options for how this tokenizer should behave</param>
		void SetOptions(string options);
	}
}
