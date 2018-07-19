using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>Language pack for the UPenn/Colorado/Brandeis Chinese treebank.</summary>
	/// <remarks>
	/// Language pack for the UPenn/Colorado/Brandeis Chinese treebank.
	/// The native character set for the Chinese Treebank was GB18030, but later became UTF-8.
	/// This file (like the rest of JavaNLP) is in UTF-8.
	/// </remarks>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public class ChineseTreebankLanguagePack : AbstractTreebankLanguagePack
	{
		private const long serialVersionUID = 5757403475523638802L;

		private ITokenizerFactory<IHasWord> tf;

		public virtual void SetTokenizerFactory<_T0>(ITokenizerFactory<_T0> tf)
			where _T0 : IHasWord
		{
			this.tf = tf;
		}

		public override ITokenizerFactory<IHasWord> GetTokenizerFactory()
		{
			if (tf != null)
			{
				return tf;
			}
			else
			{
				return base.GetTokenizerFactory();
			}
		}

		public const string Encoding = "utf-8";

		/// <summary>Return the input Charset encoding for the Treebank.</summary>
		/// <remarks>
		/// Return the input Charset encoding for the Treebank.
		/// See documentation for the <code>Charset</code> class.
		/// </remarks>
		/// <returns>Name of Charset</returns>
		public override string GetEncoding()
		{
			return Encoding;
		}

		/// <summary>
		/// Accepts a String that is a punctuation
		/// tag name, and rejects everything else.
		/// </summary>
		/// <returns>Whether this is a punctuation tag</returns>
		public override bool IsPunctuationTag(string str)
		{
			return str.Equals("PU");
		}

		/// <summary>
		/// Accepts a String that is a punctuation
		/// word, and rejects everything else.
		/// </summary>
		/// <remarks>
		/// Accepts a String that is a punctuation
		/// word, and rejects everything else.
		/// If one can't tell for sure (as for ' in the Penn Treebank), it
		/// maks the best guess that it can.
		/// </remarks>
		/// <returns>Whether this is a punctuation word</returns>
		public override bool IsPunctuationWord(string str)
		{
			return ChineseCommaAcceptFilter().Test(str) || ChineseEndSentenceAcceptFilter().Test(str) || ChineseDouHaoAcceptFilter().Test(str) || ChineseQuoteMarkAcceptFilter().Test(str) || ChineseParenthesisAcceptFilter().Test(str) || ChineseColonAcceptFilter
				().Test(str) || ChineseDashAcceptFilter().Test(str) || ChineseOtherAcceptFilter().Test(str);
		}

		/// <summary>
		/// Accepts a String that is a sentence end
		/// punctuation tag, and rejects everything else.
		/// </summary>
		/// <remarks>
		/// Accepts a String that is a sentence end
		/// punctuation tag, and rejects everything else.
		/// TODO FIXME: this is testing whether it is a sentence final word,
		/// not a sentence final tag.
		/// </remarks>
		/// <returns>Whether this is a sentence final punctuation tag</returns>
		public override bool IsSentenceFinalPunctuationTag(string str)
		{
			return ChineseEndSentenceAcceptFilter().Test(str);
		}

		/// <summary>Returns a String array of punctuation tags for this treebank/language.</summary>
		/// <returns>The punctuation tags</returns>
		public override string[] PunctuationTags()
		{
			return tags;
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
			return tags;
		}

		/// <summary>
		/// Returns a String array of sentence final punctuation words for this
		/// treebank/language.
		/// </summary>
		/// <returns>The sentence final punctuation tags</returns>
		public override string[] SentenceFinalPunctuationWords()
		{
			return endSentence;
		}

		/// <summary>
		/// Accepts a String that is a punctuation
		/// tag that should be ignored by EVALB-style evaluation,
		/// and rejects everything else.
		/// </summary>
		/// <remarks>
		/// Accepts a String that is a punctuation
		/// tag that should be ignored by EVALB-style evaluation,
		/// and rejects everything else.
		/// Traditionally, EVALB has ignored a subset of the total set of
		/// punctuation tags in the English Penn Treebank (quotes and
		/// period, comma, colon, etc., but not brackets)
		/// </remarks>
		/// <returns>Whether this is a EVALB-ignored punctuation tag</returns>
		public override bool IsEvalBIgnoredPunctuationTag(string str)
		{
			return Filters.CollectionAcceptFilter(tags).Test(str);
		}

		/// <summary>
		/// The first 3 are used by the Penn Treebank; # is used by the
		/// BLLIP corpus, and ^ and ~ are used by Klein's
		/// lexparser.
		/// </summary>
		/// <remarks>
		/// The first 3 are used by the Penn Treebank; # is used by the
		/// BLLIP corpus, and ^ and ~ are used by Klein's
		/// lexparser. Identical to PennTreebankLanguagePack.
		/// </remarks>
		private static readonly char[] annotationIntroducingChars = new char[] { '-', '=', '|', '#', '^', '~' };

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

		/// <summary>
		/// This is valid for "BobChrisTreeNormalizer" conventions
		/// only.
		/// </summary>
		/// <remarks>
		/// This is valid for "BobChrisTreeNormalizer" conventions
		/// only. Again, identical to PennTreebankLanguagePack.
		/// </remarks>
		private static readonly string[] startSymbols = new string[] { "ROOT" };

		/// <summary>Returns a String array of treebank start symbols.</summary>
		/// <returns>The start symbols</returns>
		public override string[] StartSymbols()
		{
			return startSymbols;
		}

		private static readonly string[] tags = new string[] { "PU" };

		private static readonly string[] comma = new string[] { ",", "，", "　" };

		private static readonly string[] endSentence = new string[] { "。", "．", "！", "？", "?", "!", "." };

		private static readonly string[] douHao = new string[] { "、" };

		private static readonly string[] quoteMark = new string[] { "“", "”", "‘", "’", "《", "》", "『", "』", "〈", "〉", "「", "」", "＂", "＜", "＞", "'", "`", "＇", "｀", "｢", "｣" };

		private static readonly string[] parenthesis = new string[] { "（", "）", "［", "］", "｛", "｝", "-LRB-", "-RRB-", "【", "】", "〔", "〖", "〘", "〚", "｟", "〕", "〗", "〙", "〛", "｠" };

		private static readonly string[] colon = new string[] { "：", "；", "∶", ":" };

		private static readonly string[] dash = new string[] { "…", "―", "——", "———", "————", "—", "——", "———", "－", "--", "---", "－－", "－－－", "－－－－", "－－－－－", "－－－－－－", "──", "━", "━━", "—－", "-", "----", "~", "~~", "~~~", "~~~~", "~~~~~", "……", "～"
			, "．．．" };

		private static readonly string[] other = new string[] { "·", "／", "／", "＊", "＆", "/", "//", "*", "※", "■", "●", "｜" };

		private static readonly string[] leftQuoteMark = new string[] { "“", "‘", "《", "『", "〈", "「", "＜", "`", "｀", "｢" };

		private static readonly string[] rightQuoteMark = new string[] { "”", "’", "》", "』", "〉", "」", "＞", "＇", "｣" };

		private static readonly string[] leftParenthesis = new string[] { "（", "-LRB-", "［", "｛", "【", "〔", "〖", "〘", "〚", "｟" };

		private static readonly string[] rightParenthesis = new string[] { "）", "-RRB-", "］", "｝", "】", "〕", "〗", "〙", "〛", "｠" };

		private static readonly string[] punctWords;

		static ChineseTreebankLanguagePack()
		{
			// 　last is an "ideographic space"...?
			// ( and ) still must be escaped
			/* 3 full width dots as ellipsis */
			// slashes are used in urls
			// Note that these next four should contain only things in quoteMark and parenthesis.  All such things are there but straight quotes
			// "〔", "〖", "〘", "〚", "｟", "〕", "〗", "〙", "〛", "｠"
			int n = comma.Length + endSentence.Length + douHao.Length + quoteMark.Length + parenthesis.Length + colon.Length + dash.Length + other.Length + leftQuoteMark.Length + rightQuoteMark.Length + leftParenthesis.Length + rightParenthesis.Length;
			punctWords = new string[n];
			int m = 0;
			System.Array.Copy(comma, 0, punctWords, m, comma.Length);
			m += comma.Length;
			System.Array.Copy(endSentence, 0, punctWords, m, endSentence.Length);
			m += endSentence.Length;
			System.Array.Copy(douHao, 0, punctWords, m, douHao.Length);
			m += douHao.Length;
			System.Array.Copy(quoteMark, 0, punctWords, m, quoteMark.Length);
			m += quoteMark.Length;
			System.Array.Copy(parenthesis, 0, punctWords, m, parenthesis.Length);
			m += parenthesis.Length;
			System.Array.Copy(colon, 0, punctWords, m, colon.Length);
			m += colon.Length;
			System.Array.Copy(dash, 0, punctWords, m, dash.Length);
			m += dash.Length;
			System.Array.Copy(other, 0, punctWords, m, other.Length);
			m += other.Length;
		}

		public static IPredicate<string> ChineseCommaAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(comma);
		}

		public static IPredicate<string> ChineseEndSentenceAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(endSentence);
		}

		public static IPredicate<string> ChineseDouHaoAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(douHao);
		}

		public static IPredicate<string> ChineseQuoteMarkAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(quoteMark);
		}

		public static IPredicate<string> ChineseParenthesisAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(parenthesis);
		}

		public static IPredicate<string> ChineseColonAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(colon);
		}

		public static IPredicate<string> ChineseDashAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(dash);
		}

		public static IPredicate<string> ChineseOtherAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(other);
		}

		public static IPredicate<string> ChineseLeftParenthesisAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(leftParenthesis);
		}

		public static IPredicate<string> ChineseRightParenthesisAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(rightParenthesis);
		}

		public static IPredicate<string> ChineseLeftQuoteMarkAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(leftQuoteMark);
		}

		public static IPredicate<string> ChineseRightQuoteMarkAcceptFilter()
		{
			return Filters.CollectionAcceptFilter(rightQuoteMark);
		}

		/// <summary>Returns the extension of treebank files for this treebank.</summary>
		/// <remarks>
		/// Returns the extension of treebank files for this treebank.
		/// This is "fid".
		/// </remarks>
		public override string TreebankFileExtension()
		{
			return "fid";
		}

		public override IGrammaticalStructureFactory GrammaticalStructureFactory()
		{
			if (this.GenerateOriginalDependencies())
			{
				return new ChineseGrammaticalStructureFactory();
			}
			else
			{
				return new UniversalChineseGrammaticalStructureFactory();
			}
		}

		public override IGrammaticalStructureFactory GrammaticalStructureFactory(IPredicate<string> puncFilt)
		{
			if (this.GenerateOriginalDependencies())
			{
				return new ChineseGrammaticalStructureFactory(puncFilt);
			}
			else
			{
				return new UniversalChineseGrammaticalStructureFactory(puncFilt);
			}
		}

		public override IGrammaticalStructureFactory GrammaticalStructureFactory(IPredicate<string> puncFilt, IHeadFinder hf)
		{
			if (this.GenerateOriginalDependencies())
			{
				return new ChineseGrammaticalStructureFactory(puncFilt, hf);
			}
			else
			{
				return new UniversalChineseGrammaticalStructureFactory(puncFilt, hf);
			}
		}

		public override bool SupportsGrammaticalStructures()
		{
			return true;
		}

		public override ITreeReaderFactory TreeReaderFactory()
		{
			TreeNormalizer tn = new BobChrisTreeNormalizer();
			return new CTBTreeReaderFactory(tn);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder HeadFinder()
		{
			return new ChineseHeadFinder(this);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder TypedDependencyHeadFinder()
		{
			if (this.GenerateOriginalDependencies())
			{
				return new ChineseSemanticHeadFinder(this);
			}
			else
			{
				return new UniversalChineseSemanticHeadFinder();
			}
		}

		public override bool GenerateOriginalDependencies()
		{
			return generateOriginalDependencies;
		}
	}
}
