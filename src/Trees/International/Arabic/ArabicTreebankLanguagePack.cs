using Edu.Stanford.Nlp.International.Arabic;
using Edu.Stanford.Nlp.International.Arabic.Process;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Arabic
{
	/// <summary>
	/// Specifies the treebank/language specific components needed for
	/// parsing the Penn Arabic Treebank (ATB).
	/// </summary>
	/// <remarks>
	/// Specifies the treebank/language specific components needed for
	/// parsing the Penn Arabic Treebank (ATB). This language pack has been updated for
	/// ATB1v4, ATB2v3, and ATB3v3.2
	/// <p>
	/// The encoding for the ATB is the default UTF-8 specified in AbstractTreebankLanguagePack.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Mona Diab</author>
	/// <author>Roger Levy</author>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class ArabicTreebankLanguagePack : AbstractTreebankLanguagePack
	{
		private const long serialVersionUID = 9081305982861675328L;

		private static readonly string[] collinsPunctTags = new string[] { "PUNC" };

		private static readonly string[] pennPunctTags = new string[] { "PUNC" };

		private static readonly string[] pennPunctWords = new string[] { ".", "\"", ",", "-LRB-", "-RRB-", "-", ":", "/", "?", "_", "*", "%", "!", ">", "-PLUS-", "...", ";", "..", "&", "=", "ر", "'", "\\", "`", "......" };

		private static readonly string[] pennSFPunctTags = new string[] { "PUNC" };

		private static readonly string[] pennSFPunctWords = new string[] { ".", "!", "?" };

		/// <summary>
		/// The first 3 are used by the Penn Treebank; # is used by the
		/// BLLIP corpus, and ^ and ~ are used by Klein's lexparser.
		/// </summary>
		/// <remarks>
		/// The first 3 are used by the Penn Treebank; # is used by the
		/// BLLIP corpus, and ^ and ~ are used by Klein's lexparser.
		/// Chris deleted '_' for Arabic as it appears in tags (NO_FUNC).
		/// June 2006: CDM tested _ again with true (new) Treebank tags to see if it
		/// was useful for densening up the tag space, but the results were negative.
		/// Roger added + for Arabic but Chris deleted it again, since unless you've
		/// recoded determiners, it screws up DET+NOUN, etc.  (That is, it would only be useful if
		/// you always wanted to cut at the first '+', but in practice that is not viable, certainly
		/// not with the IBM ATB processing either.)
		/// </remarks>
		private static readonly char[] annotationIntroducingChars = new char[] { '-', '=', '|', '#', '^', '~' };

		/// <summary>This is valid for "BobChrisTreeNormalizer" conventions only.</summary>
		/// <remarks>
		/// This is valid for "BobChrisTreeNormalizer" conventions only.
		/// wsg: "ROOT" should always be the first value. See
		/// <see cref="Edu.Stanford.Nlp.Trees.AbstractTreebankLanguagePack.StartSymbol()"/>
		/// in
		/// the parent class.
		/// </remarks>
		private static readonly string[] pennStartSymbols = new string[] { "ROOT" };

		/// <summary>Returns a String array of punctuation tags for this treebank/language.</summary>
		/// <returns>The punctuation tags</returns>
		public override string[] PunctuationTags()
		{
			return pennPunctTags;
		}

		/// <summary>Returns a String array of punctuation words for this treebank/language.</summary>
		/// <returns>The punctuation words</returns>
		public override string[] PunctuationWords()
		{
			return pennPunctWords;
		}

		/// <summary>
		/// Returns a String array of sentence final punctuation tags for this
		/// treebank/language.
		/// </summary>
		/// <returns>The sentence final punctuation tags</returns>
		public override string[] SentenceFinalPunctuationTags()
		{
			return pennSFPunctTags;
		}

		/// <summary>
		/// Returns a String array of sentence final punctuation words for this
		/// treebank/language.
		/// </summary>
		/// <returns>The sentence final punctuation tags</returns>
		public override string[] SentenceFinalPunctuationWords()
		{
			return pennSFPunctWords;
		}

		/// <summary>
		/// Returns a String array of punctuation tags that EVALB-style evaluation
		/// should ignore for this treebank/language.
		/// </summary>
		/// <remarks>
		/// Returns a String array of punctuation tags that EVALB-style evaluation
		/// should ignore for this treebank/language.
		/// Traditionally, EVALB has ignored a subset of the total set of
		/// punctuation tags in the English Penn Treebank (quotes and
		/// period, comma, colon, etc., but not brackets)
		/// </remarks>
		/// <returns>Whether this is a EVALB-ignored punctuation tag</returns>
		public override string[] EvalBIgnoredPunctuationTags()
		{
			return collinsPunctTags;
		}

		/// <summary>
		/// Return an array of characters at which a String should be
		/// truncated to give the basic syntactic category of a label.
		/// </summary>
		/// <remarks>
		/// Return an array of characters at which a String should be
		/// truncated to give the basic syntactic category of a label.
		/// The idea here is that Penn treebank style labels follow a syntactic
		/// category with various functional and crossreferencing information
		/// introduced by special characters (such as "NP-SBJ=1").  This would
		/// be truncated to "NP" by the array containing '-' and "=".
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
			return pennStartSymbols;
		}

		/// <summary>TODO: there is no way to change this using options.</summary>
		private ITokenizerFactory<IHasWord> tf = ArabicTokenizer.AtbFactory();

		/// <summary>
		/// Return a tokenizer which might be suitable for tokenizing text
		/// that will be used with this Treebank/Language pair.
		/// </summary>
		/// <remarks>
		/// Return a tokenizer which might be suitable for tokenizing text
		/// that will be used with this Treebank/Language pair.  We tokenize
		/// the Arabic using the ArabicTokenizer class.
		/// </remarks>
		/// <returns>A tokenizer</returns>
		public override ITokenizerFactory<IHasWord> GetTokenizerFactory()
		{
			return tf;
		}

		/// <summary>Returns the extension of treebank files for this treebank.</summary>
		/// <remarks>
		/// Returns the extension of treebank files for this treebank.
		/// This is "tree".
		/// </remarks>
		public override string TreebankFileExtension()
		{
			return "tree";
		}

		public override ITreeReaderFactory TreeReaderFactory()
		{
			return new ArabicTreeReaderFactory();
		}

		public override string ToString()
		{
			return "ArabicTreebankLanguagePack";
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder HeadFinder()
		{
			return new ArabicHeadFinder(this);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder TypedDependencyHeadFinder()
		{
			return new ArabicHeadFinder(this);
		}

		public override MorphoFeatureSpecification MorphFeatureSpec()
		{
			return new ArabicMorphoFeatureSpecification();
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			ITreebankLanguagePack tlp = new PennTreebankLanguagePack();
			System.Console.Out.WriteLine("Start symbol: " + tlp.StartSymbol());
			string start = tlp.StartSymbol();
			System.Console.Out.WriteLine("Should be true: " + (tlp.IsStartSymbol(start)));
			string[] strs = new string[] { "-", "-LLB-", "NP-2", "NP=3", "NP-LGS", "NP-TMP=3" };
			foreach (string str in strs)
			{
				System.Console.Out.WriteLine("String: " + str + " basic: " + tlp.BasicCategory(str) + " basicAndFunc: " + tlp.CategoryAndFunction(str));
			}
		}
	}
}
