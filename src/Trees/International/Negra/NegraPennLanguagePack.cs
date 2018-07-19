using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Negra
{
	/// <summary>
	/// Language pack for Negra and Tiger treebanks <em>after</em> conversion to
	/// PTB format.
	/// </summary>
	/// <author>Roger Levy</author>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class NegraPennLanguagePack : AbstractTreebankLanguagePack
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Negra.NegraPennLanguagePack));

		private const long serialVersionUID = 9081305982861675328L;

		/// <summary>Grammatical function parameters.</summary>
		/// <remarks>Grammatical function parameters.  If this is true, keep subj, obj, iobj functional tags, only.</remarks>
		private bool leaveGF = false;

		private static string[] gfToKeepArray = new string[] { "SB", "OA", "DA" };

		/// <summary>Gives a handle to the TreebankLanguagePack</summary>
		public NegraPennLanguagePack()
			: this(false, AbstractTreebankLanguagePack.DefaultGfChar)
		{
		}

		/// <summary>Gives a handle to the TreebankLanguagePack</summary>
		public NegraPennLanguagePack(bool leaveGF)
			: this(leaveGF, AbstractTreebankLanguagePack.DefaultGfChar)
		{
		}

		/// <summary>
		/// Make a new language pack with grammatical functions used based on the value of leaveGF
		/// and marked with the character gfChar.
		/// </summary>
		/// <remarks>
		/// Make a new language pack with grammatical functions used based on the value of leaveGF
		/// and marked with the character gfChar.  gfChar should *not* be an annotation introducing character.
		/// </remarks>
		public NegraPennLanguagePack(bool leaveGF, char gfChar)
			: base(gfChar)
		{
			this.leaveGF = leaveGF;
		}

		private const string NegraEncoding = "ISO-8859-1";

		private static readonly string[] evalBignoredTags = new string[] { "$.", "$," };

		private static readonly string[] negraSFPunctTags = new string[] { "$." };

		private static readonly string[] negraSFPunctWords = new string[] { ".", "!", "?" };

		private static readonly string[] negraPunctTags = new string[] { "$.", "$,", "$*LRB*" };

		/// <summary>The unicode escape is for a middle dot character</summary>
		private static readonly string[] negraPunctWords = new string[] { "-", ",", ";", ":", "!", "?", "/", ".", "...", "\u00b7", "'", "\"", "(", ")", "*LRB*", "*RRB*" };

		/// <summary>
		/// The first 3 are used by the Penn Treebank; # is used by the
		/// BLLIP corpus, and ^ and ~ are used by Klein's lexparser.
		/// </summary>
		private static char[] annotationIntroducingChars = new char[] { '-', '%', '=', '|', '#', '^', '~' };

		/// <summary>This is valid for "BobChrisTreeNormalizer" conventions only.</summary>
		private static string[] pennStartSymbols = new string[] { "ROOT" };

		/// <summary>Returns a String array of punctuation tags for this treebank/language.</summary>
		/// <returns>The punctuation tags</returns>
		public override string[] PunctuationTags()
		{
			return negraPunctTags;
		}

		/// <summary>Returns a String array of punctuation words for this treebank/language.</summary>
		/// <returns>The punctuation words</returns>
		public override string[] PunctuationWords()
		{
			return negraPunctWords;
		}

		/// <summary>
		/// Returns a String array of sentence final punctuation tags for this
		/// treebank/language.
		/// </summary>
		/// <returns>The sentence final punctuation tags</returns>
		public override string[] SentenceFinalPunctuationTags()
		{
			return negraSFPunctTags;
		}

		/// <summary>
		/// Returns a String array of sentence final punctuation words for this
		/// treebank/language.
		/// </summary>
		/// <returns>The sentence final punctuation tags</returns>
		public override string[] SentenceFinalPunctuationWords()
		{
			return negraSFPunctWords;
		}

		//wsg2010: Disabled limited grammatical functions for now, which decrease F1 by ~10.0.
		public override string BasicCategory(string category)
		{
			string basicCat;
			if (leaveGF)
			{
				basicCat = StripGF(category);
			}
			else
			{
				basicCat = base.BasicCategory(category);
			}
			// log.info("NPLP stripping " + category + " with leaveGF = " + leaveGF + " gives " + basicCat);
			return basicCat;
		}

		public override string StripGF(string category)
		{
			if (category == null)
			{
				return null;
			}
			int index = category.LastIndexOf(gfCharacter);
			if (index > 0)
			{
				if (!ContainsKeptGF(category, index))
				{
					category = Sharpen.Runtime.Substring(category, 0, index);
				}
			}
			return category;
		}

		/// <summary>
		/// Helper method for determining if the gf in category
		/// is one of those in the array gfToKeepArray.
		/// </summary>
		/// <remarks>
		/// Helper method for determining if the gf in category
		/// is one of those in the array gfToKeepArray.  Index is the
		/// index where the gfCharacter appears.
		/// </remarks>
		private static bool ContainsKeptGF(string category, int index)
		{
			foreach (string gf in gfToKeepArray)
			{
				int gfLength = gf.Length;
				if (gfLength < (category.Length - index))
				{
					if (Sharpen.Runtime.Substring(category, index + 1, index + 1 + gfLength).Equals(gf))
					{
						return true;
					}
				}
			}
			return false;
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
			return evalBignoredTags;
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

		/// <summary>Return the input Charset encoding for the Treebank.</summary>
		/// <remarks>
		/// Return the input Charset encoding for the Treebank.
		/// See documentation for the <code>Charset</code> class.
		/// </remarks>
		/// <returns>Name of Charset</returns>
		public override string GetEncoding()
		{
			return NegraEncoding;
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

		public virtual bool IsLeaveGF()
		{
			return leaveGF;
		}

		public virtual void SetLeaveGF(bool leaveGF)
		{
			this.leaveGF = leaveGF;
		}

		public override ITreeReaderFactory TreeReaderFactory()
		{
			return new NegraPennTreeReaderFactory(this);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder HeadFinder()
		{
			return new NegraHeadFinder(this);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IHeadFinder TypedDependencyHeadFinder()
		{
			return new NegraHeadFinder(this);
		}

		/// <summary>
		/// Return a tokenizer which might be suitable for tokenizing text that
		/// will be used with this Treebank/Language pair, without tokenizing carriage
		/// returns (i.e., treating them as white space).
		/// </summary>
		/// <remarks>
		/// Return a tokenizer which might be suitable for tokenizing text that
		/// will be used with this Treebank/Language pair, without tokenizing carriage
		/// returns (i.e., treating them as white space).  For German (Negra) we used
		/// to only provide a
		/// <see cref="Edu.Stanford.Nlp.Process.WhitespaceTokenizer{T}"/>
		/// ,
		/// but people didn't much like that.
		/// So now we provide
		/// <see cref="Edu.Stanford.Nlp.Process.PTBTokenizer{T}"/>
		/// . It's not customized to German, but
		/// will nevertheless do better than WhitespaceTokenizer at tokenizing German!
		/// </remarks>
		/// <returns>A tokenizer</returns>
		public override ITokenizerFactory<IHasWord> GetTokenizerFactory()
		{
			return PTBTokenizer.Factory();
		}
	}
}
