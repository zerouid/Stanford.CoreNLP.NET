using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;



namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Specifies the treebank/language specific components needed for
	/// parsing the English Penn Treebank.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <version>1.2</version>
	[System.Serializable]
	public class PennTreebankLanguagePack : AbstractTreebankLanguagePack
	{
		/// <summary>Gives a handle to the TreebankLanguagePack</summary>
		public PennTreebankLanguagePack()
		{
		}

		public static readonly string[] pennPunctTags = new string[] { "''", "``", "-LRB-", "-RRB-", ".", ":", "," };

		private static readonly string[] pennSFPunctTags = new string[] { "." };

		private static readonly string[] collinsPunctTags = new string[] { "''", "``", ".", ":", "," };

		private static readonly string[] pennPunctWords = new string[] { "''", "'", "``", "`", "-LRB-", "-RRB-", "-LCB-", "-RCB-", ".", "?", "!", ",", ":", "-", "--", "...", ";" };

		private static readonly string[] pennSFPunctWords = new string[] { ".", "!", "?" };

		/// <summary>
		/// The first 3 are used by the Penn Treebank; # is used by the
		/// BLLIP corpus, and ^ and ~ are used by Klein's lexparser.
		/// </summary>
		/// <remarks>
		/// The first 3 are used by the Penn Treebank; # is used by the
		/// BLLIP corpus, and ^ and ~ are used by Klein's lexparser.
		/// Teg added _ (let me know if it hurts).
		/// John Bauer added [ on account of category annotations added when
		/// printing out lexicalized dependencies.  Note that ] ought to be
		/// unnecessary, since it would end the annotation, not start it.
		/// </remarks>
		private static readonly char[] annotationIntroducingChars = new char[] { '-', '=', '|', '#', '^', '~', '_', '[' };

		/// <summary>This is valid for "BobChrisTreeNormalizer" conventions only.</summary>
		private static readonly string[] pennStartSymbols = new string[] { "ROOT", "TOP" };

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

		/// <summary>
		/// Returns a factory for
		/// <see cref="Edu.Stanford.Nlp.Process.PTBTokenizer{T}"/>
		/// .
		/// </summary>
		/// <returns>A tokenizer</returns>
		public override ITokenizerFactory<IHasWord> GetTokenizerFactory()
		{
			return PTBTokenizer.CoreLabelFactory();
		}

		/// <summary>Returns the extension of treebank files for this treebank.</summary>
		/// <remarks>
		/// Returns the extension of treebank files for this treebank.
		/// This is "mrg".
		/// </remarks>
		public override string TreebankFileExtension()
		{
			return "mrg";
		}

		/// <summary>Return a GrammaticalStructure suitable for this language/treebank.</summary>
		/// <returns>A GrammaticalStructure suitable for this language/treebank.</returns>
		public override IGrammaticalStructureFactory GrammaticalStructureFactory()
		{
			if (generateOriginalDependencies)
			{
				return new EnglishGrammaticalStructureFactory();
			}
			else
			{
				return new UniversalEnglishGrammaticalStructureFactory();
			}
		}

		/// <summary>Return a GrammaticalStructure suitable for this language/treebank.</summary>
		/// <remarks>
		/// Return a GrammaticalStructure suitable for this language/treebank.
		/// <p>
		/// <i>Note:</i> This is loaded by reflection so basic treebank use does not require all the Stanford Dependencies code.
		/// </remarks>
		/// <returns>A GrammaticalStructure suitable for this language/treebank.</returns>
		public override IGrammaticalStructureFactory GrammaticalStructureFactory(IPredicate<string> puncFilter)
		{
			if (generateOriginalDependencies)
			{
				return new EnglishGrammaticalStructureFactory(puncFilter);
			}
			else
			{
				return new UniversalEnglishGrammaticalStructureFactory(puncFilter);
			}
		}

		public override IGrammaticalStructureFactory GrammaticalStructureFactory(IPredicate<string> puncFilter, IHeadFinder hf)
		{
			if (generateOriginalDependencies)
			{
				return new EnglishGrammaticalStructureFactory(puncFilter, hf);
			}
			else
			{
				return new UniversalEnglishGrammaticalStructureFactory(puncFilter, hf);
			}
		}

		public override bool SupportsGrammaticalStructures()
		{
			return true;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder HeadFinder()
		{
			return new ModCollinsHeadFinder(this);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder TypedDependencyHeadFinder()
		{
			if (generateOriginalDependencies)
			{
				return new SemanticHeadFinder(this, true);
			}
			else
			{
				return new UniversalSemanticHeadFinder(this, true);
			}
		}

		/// <summary>Prints a few aspects of the TreebankLanguagePack, just for debugging.</summary>
		public static void Main(string[] args)
		{
			ITreebankLanguagePack tlp = new Edu.Stanford.Nlp.Trees.PennTreebankLanguagePack();
			System.Console.Out.WriteLine("Start symbol: " + tlp.StartSymbol());
			string start = tlp.StartSymbol();
			System.Console.Out.WriteLine("Should be true: " + (tlp.IsStartSymbol(start)));
			string[] strs = new string[] { "-", "-LLB-", "NP-2", "NP=3", "NP-LGS", "NP-TMP=3" };
			foreach (string str in strs)
			{
				System.Console.Out.WriteLine("String: " + str + " basic: " + tlp.BasicCategory(str) + " basicAndFunc: " + tlp.CategoryAndFunction(str));
			}
		}

		private const long serialVersionUID = 9081305982861675328L;
	}
}
