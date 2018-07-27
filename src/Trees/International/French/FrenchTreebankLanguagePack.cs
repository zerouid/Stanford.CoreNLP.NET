using Edu.Stanford.Nlp.International.French;
using Edu.Stanford.Nlp.International.French.Process;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Trees.International.French
{
	/// <summary>Language pack for the French treebank.</summary>
	/// <author>mcdm</author>
	[System.Serializable]
	public class FrenchTreebankLanguagePack : AbstractTreebankLanguagePack
	{
		private const long serialVersionUID = -7338244949063822519L;

		public const string FtbEncoding = "ISO8859_1";

		private static readonly string[] frenchPunctTags = new string[] { "PUNC" };

		private static readonly string[] frenchSFPunctTags = new string[] { "PUNC" };

		private static readonly string[] frenchPunctWords = new string[] { "=", "*", "/", "\\", "]", "[", "\"", "''", "'", "``", "`", "-LRB-", "-RRB-", "-LCB-", "-RCB-", ".", "?", "!", ",", ":", "-", "--", "...", ";", "&quot;" };

		private static readonly string[] frenchSFPunctWords = new string[] { ".", "!", "?" };

		private static readonly char[] annotationIntroducingChars = new char[] { '-', '=', '|', '#', '^', '~' };

		private static readonly string[] frenchStartSymbols = new string[] { "ROOT" };

		//wsg2011: The distributed treebank is encoding in ISO8859_1, but
		//the current FrenchTreebankParserParams is currently configured to
		//read UTF-8, PTB style trees that have been extracted from the XML
		//files.
		//The raw treebank uses "PONCT". Change to LDC convention.
		public override ITokenizerFactory<IHasWord> GetTokenizerFactory()
		{
			return FrenchTokenizer.FtbFactory();
		}

		public override string GetEncoding()
		{
			return FtbEncoding;
		}

		/// <summary>Returns a String array of punctuation tags for this treebank/language.</summary>
		/// <returns>The punctuation tags</returns>
		public override string[] PunctuationTags()
		{
			return frenchPunctTags;
		}

		/// <summary>Returns a String array of punctuation words for this treebank/language.</summary>
		/// <returns>The punctuation words</returns>
		public override string[] PunctuationWords()
		{
			return frenchPunctWords;
		}

		/// <summary>
		/// Returns a String array of sentence final punctuation tags for this
		/// treebank/language.
		/// </summary>
		/// <returns>The sentence final punctuation tags</returns>
		public override string[] SentenceFinalPunctuationTags()
		{
			return frenchSFPunctTags;
		}

		/// <summary>
		/// Returns a String array of sentence final punctuation words for this
		/// treebank/language.
		/// </summary>
		/// <returns>The sentence final punctuation tags</returns>
		public override string[] SentenceFinalPunctuationWords()
		{
			return frenchSFPunctWords;
		}

		/// <summary>
		/// Return an array of characters at which a String should be
		/// truncated to give the basic syntactic category of a label.
		/// </summary>
		/// <remarks>
		/// Return an array of characters at which a String should be
		/// truncated to give the basic syntactic category of a label.
		/// The idea here is that French treebank style labels follow a syntactic
		/// category with various functional and crossreferencing information
		/// introduced by special characters (such as "NP-SUBJ").  This would
		/// be truncated to "NP" by the array containing '-'.
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
			return frenchStartSymbols;
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
			return new FrenchHeadFinder(this);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder TypedDependencyHeadFinder()
		{
			return new FrenchHeadFinder(this);
		}

		public override MorphoFeatureSpecification MorphFeatureSpec()
		{
			return new FrenchMorphoFeatureSpecification();
		}
	}
}
