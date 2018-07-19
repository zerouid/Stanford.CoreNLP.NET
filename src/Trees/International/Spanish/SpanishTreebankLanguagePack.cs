using Edu.Stanford.Nlp.International.Spanish.Process;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Spanish
{
	/// <summary>Language pack for the Spanish treebank.</summary>
	/// <author>mcdm</author>
	[System.Serializable]
	public class SpanishTreebankLanguagePack : AbstractTreebankLanguagePack
	{
		private const long serialVersionUID = -7059939700276532428L;

		private static readonly string[] punctTags = new string[] { "faa", "fat", "fc", "fca", "fct", "fd", "fe", "fg", "fh", "fia", "fit", "fla", "flt", "fp", "fpa", "fpt", "fra", "frc", "fs", "ft", "fx", "fz", "f0" };

		private static readonly string[] sentenceFinalPunctTags = new string[] { "fat", "fit", "fp", "fs" };

		private static readonly string[] punctWords = new string[] { "¡", "!", ",", "[", "]", ":", "\"", "-", "/", "¿", "?", "{", "}", ".", "=LRB=", "=RRB=", "«", "»", "…", "...", "%", ";", "_", "+", "=", "&", "@" };

		private static readonly string[] sentenceFinalPunctWords = new string[] { "!", "?", ".", "…", "..." };

		private static readonly string[] startSymbols = new string[] { "ROOT" };

		private static readonly char[] annotationIntroducingChars = new char[] { '^', '[', '-' };

		/// <summary>
		/// Return a tokenizer which might be suitable for tokenizing text that will be used with this
		/// Treebank/Language pair, without tokenizing carriage returns (i.e., treating them as white
		/// space).
		/// </summary>
		/// <remarks>
		/// Return a tokenizer which might be suitable for tokenizing text that will be used with this
		/// Treebank/Language pair, without tokenizing carriage returns (i.e., treating them as white
		/// space).  The implementation in AbstractTreebankLanguagePack returns a factory for
		/// <see cref="Edu.Stanford.Nlp.Process.WhitespaceTokenizer{T}"/>
		/// .
		/// </remarks>
		/// <returns>A tokenizer</returns>
		public override ITokenizerFactory<IHasWord> GetTokenizerFactory()
		{
			return SpanishTokenizer.Factory(new CoreLabelTokenFactory(), "invertible,ptb3Escaping=true,splitAll=true");
		}

		/// <summary>Returns a String array of punctuation tags for this treebank/language.</summary>
		/// <returns>The punctuation tags</returns>
		public override string[] PunctuationTags()
		{
			return punctTags;
		}

		/// <summary>Returns a String array of punctuation words for this treebank/language.</summary>
		/// <returns>The punctuation words</returns>
		public override string[] PunctuationWords()
		{
			return punctWords;
		}

		/// <summary>
		/// Returns a String array of sentence final punctuation tags for this
		/// treebank/language.
		/// </summary>
		/// <returns>The sentence final punctuation tags</returns>
		public override string[] SentenceFinalPunctuationTags()
		{
			return sentenceFinalPunctTags;
		}

		/// <summary>
		/// Returns a String array of sentence final punctuation words for this
		/// treebank/language.
		/// </summary>
		/// <returns>The sentence final punctuation tags</returns>
		public override string[] SentenceFinalPunctuationWords()
		{
			return sentenceFinalPunctWords;
		}

		/// <summary>
		/// Return an array of characters at which a String should be truncated to give the basic syntactic
		/// category of a label.
		/// </summary>
		/// <remarks>
		/// Return an array of characters at which a String should be truncated to give the basic syntactic
		/// category of a label. The idea here is that Penn treebank style labels follow a syntactic
		/// category with various functional and crossreferencing information introduced by special
		/// characters (such as "NP-SBJ=1").  This would be truncated to "NP" by the array containing '-'
		/// and "=".
		/// </remarks>
		/// <returns>An array of characters that set off label name suffixes</returns>
		public override char[] LabelAnnotationIntroducingCharacters()
		{
			return annotationIntroducingChars;
		}

		/// <summary>Returns a String array of treebank start symbols.</summary>
		/// <returns>The start symbols</returns>
		public override string[] StartSymbols()
		{
			return startSymbols;
		}

		/// <summary>Returns the extension of treebank files for this treebank.</summary>
		public override string TreebankFileExtension()
		{
			return "xml";
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder HeadFinder()
		{
			return new SpanishHeadFinder(this);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder TypedDependencyHeadFinder()
		{
			return new SpanishHeadFinder(this);
		}
	}
}
