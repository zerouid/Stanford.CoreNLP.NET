using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This interface specifies language/treebank specific information for a
	/// Treebank, which a parser or other treebank user might need to know.
	/// </summary>
	/// <remarks>
	/// This interface specifies language/treebank specific information for a
	/// Treebank, which a parser or other treebank user might need to know.
	/// Some of this is fixed for a (treebank,language) pair, but some of it
	/// reflects feature extraction decisions, so it can be sensible to have
	/// multiple implementations of this interface for the same
	/// (treebank,language) pair.
	/// So far this covers punctuation, character encodings, and characters
	/// reserved for label annotations.  It should probably be expanded to
	/// cover other stuff (unknown words?).
	/// Various methods in this class return arrays.  You should treat them
	/// as read-only, even though one cannot enforce that in Java.
	/// Implementations in this class do not call basicCategory() on arguments
	/// before testing them, so if needed, you should explicitly call
	/// basicCategory() yourself before passing arguments to these routines for
	/// testing.
	/// This class should be able to be an immutable singleton.  It contains
	/// data on various things, but no state.  At some point we should make it
	/// a real immutable singleton.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <version>1.1, Mar 2003</version>
	public interface ITreebankLanguagePack
	{
		/// <summary>
		/// Accepts a String that is a punctuation
		/// tag name, and rejects everything else.
		/// </summary>
		/// <param name="str">The string to check</param>
		/// <returns>Whether this is a punctuation tag</returns>
		bool IsPunctuationTag(string str);

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
		/// <param name="str">The string to check</param>
		/// <returns>Whether this is a punctuation word</returns>
		bool IsPunctuationWord(string str);

		/// <summary>
		/// Accepts a String that is a sentence end
		/// punctuation tag, and rejects everything else.
		/// </summary>
		/// <param name="str">The string to check</param>
		/// <returns>Whether this is a sentence final punctuation tag</returns>
		bool IsSentenceFinalPunctuationTag(string str);

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
		/// <param name="str">The string to check</param>
		/// <returns>Whether this is a EVALB-ignored punctuation tag</returns>
		bool IsEvalBIgnoredPunctuationTag(string str);

		/// <summary>
		/// Return a filter that accepts a String that is a punctuation
		/// tag name, and rejects everything else.
		/// </summary>
		/// <returns>The filter</returns>
		IPredicate<string> PunctuationTagAcceptFilter();

		/// <summary>
		/// Return a filter that rejects a String that is a punctuation
		/// tag name, and accepts everything else.
		/// </summary>
		/// <returns>The filter</returns>
		IPredicate<string> PunctuationTagRejectFilter();

		/// <summary>
		/// Returns a filter that accepts a String that is a punctuation
		/// word, and rejects everything else.
		/// </summary>
		/// <remarks>
		/// Returns a filter that accepts a String that is a punctuation
		/// word, and rejects everything else.
		/// If one can't tell for sure (as for ' in the Penn Treebank), it
		/// maks the best guess that it can.
		/// </remarks>
		/// <returns>The Filter</returns>
		IPredicate<string> PunctuationWordAcceptFilter();

		/// <summary>
		/// Returns a filter that accepts a String that is not a punctuation
		/// word, and rejects punctuation.
		/// </summary>
		/// <remarks>
		/// Returns a filter that accepts a String that is not a punctuation
		/// word, and rejects punctuation.
		/// If one can't tell for sure (as for ' in the Penn Treebank), it
		/// makes the best guess that it can.
		/// </remarks>
		/// <returns>The Filter</returns>
		IPredicate<string> PunctuationWordRejectFilter();

		/// <summary>
		/// Returns a filter that accepts a String that is a sentence end
		/// punctuation tag, and rejects everything else.
		/// </summary>
		/// <returns>The Filter</returns>
		IPredicate<string> SentenceFinalPunctuationTagAcceptFilter();

		/// <summary>
		/// Returns a filter that accepts a String that is a punctuation
		/// tag that should be ignored by EVALB-style evaluation,
		/// and rejects everything else.
		/// </summary>
		/// <remarks>
		/// Returns a filter that accepts a String that is a punctuation
		/// tag that should be ignored by EVALB-style evaluation,
		/// and rejects everything else.
		/// Traditionally, EVALB has ignored a subset of the total set of
		/// punctuation tags in the English Penn Treebank (quotes and
		/// period, comma, colon, etc., but not brackets)
		/// </remarks>
		/// <returns>The Filter</returns>
		IPredicate<string> EvalBIgnoredPunctuationTagAcceptFilter();

		/// <summary>
		/// Returns a filter that accepts everything except a String that is a
		/// punctuation tag that should be ignored by EVALB-style evaluation.
		/// </summary>
		/// <remarks>
		/// Returns a filter that accepts everything except a String that is a
		/// punctuation tag that should be ignored by EVALB-style evaluation.
		/// Traditionally, EVALB has ignored a subset of the total set of
		/// punctuation tags in the English Penn Treebank (quotes and
		/// period, comma, colon, etc., but not brackets)
		/// </remarks>
		/// <returns>The Filter</returns>
		IPredicate<string> EvalBIgnoredPunctuationTagRejectFilter();

		/// <summary>Returns a String array of punctuation tags for this treebank/language.</summary>
		/// <returns>The punctuation tags</returns>
		string[] PunctuationTags();

		/// <summary>Returns a String array of punctuation words for this treebank/language.</summary>
		/// <returns>The punctuation words</returns>
		string[] PunctuationWords();

		/// <summary>
		/// Returns a String array of sentence final punctuation tags for this
		/// treebank/language.
		/// </summary>
		/// <remarks>
		/// Returns a String array of sentence final punctuation tags for this
		/// treebank/language.  The first in the list is assumed to be the most
		/// basic one.
		/// </remarks>
		/// <returns>The sentence final punctuation tags</returns>
		string[] SentenceFinalPunctuationTags();

		/// <summary>
		/// Returns a String array of sentence final punctuation words for
		/// this treebank/language.
		/// </summary>
		/// <returns>The punctuation words</returns>
		string[] SentenceFinalPunctuationWords();

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
		string[] EvalBIgnoredPunctuationTags();

		/// <summary>Return a GrammaticalStructureFactory suitable for this language/treebank.</summary>
		/// <returns>A GrammaticalStructureFactory suitable for this language/treebank</returns>
		IGrammaticalStructureFactory GrammaticalStructureFactory();

		/// <summary>Return a GrammaticalStructureFactory suitable for this language/treebank.</summary>
		/// <param name="puncFilter">A filter which should reject punctuation words (as Strings)</param>
		/// <returns>A GrammaticalStructureFactory suitable for this language/treebank</returns>
		IGrammaticalStructureFactory GrammaticalStructureFactory(IPredicate<string> puncFilter);

		/// <summary>Return a GrammaticalStructureFactory suitable for this language/treebank.</summary>
		/// <param name="puncFilter">A filter which should reject punctuation words (as Strings)</param>
		/// <param name="typedDependencyHF">A HeadFinder which finds heads for typed dependencies</param>
		/// <returns>A GrammaticalStructureFactory suitable for this language/treebank</returns>
		IGrammaticalStructureFactory GrammaticalStructureFactory(IPredicate<string> puncFilter, IHeadFinder typedDependencyHF);

		/// <summary>Whether or not we have typed dependencies for this language.</summary>
		/// <remarks>
		/// Whether or not we have typed dependencies for this language.  If
		/// this method returns false, a call to grammaticalStructureFactory
		/// will cause an exception.
		/// </remarks>
		bool SupportsGrammaticalStructures();

		/// <summary>Return the charset encoding of the Treebank.</summary>
		/// <remarks>
		/// Return the charset encoding of the Treebank.  See
		/// documentation for the
		/// <c>Charset</c>
		/// class.
		/// </remarks>
		/// <returns>Name of Charset</returns>
		string GetEncoding();

		/// <summary>
		/// Return a tokenizer factory which might be suitable for tokenizing text
		/// that will be used with this Treebank/Language pair.
		/// </summary>
		/// <remarks>
		/// Return a tokenizer factory which might be suitable for tokenizing text
		/// that will be used with this Treebank/Language pair.  This is for
		/// real text of this language pair, not for reading stuff inside the
		/// treebank files.
		/// </remarks>
		/// <returns>A tokenizer</returns>
		ITokenizerFactory<IHasWord> GetTokenizerFactory();

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
		/// be truncated to "NP" by the array containing '-' and "=". <br />
		/// Note that these are never deleted as the first character as a label
		/// (so they are okay as one character tags, etc.), but only when
		/// subsequent characters.
		/// </remarks>
		/// <returns>An array of characters that set off label name suffixes</returns>
		char[] LabelAnnotationIntroducingCharacters();

		/// <summary>
		/// Say whether this character is an annotation introducing
		/// character.
		/// </summary>
		/// <param name="ch">A char</param>
		/// <returns>Whether this char introduces functional annotations</returns>
		bool IsLabelAnnotationIntroducingCharacter(char ch);

		/// <summary>
		/// Returns the basic syntactic category of a String by truncating
		/// stuff after a (non-word-initial) occurrence of one of the
		/// <c>labelAnnotationIntroducingCharacters()</c>
		/// .  This
		/// function should work on phrasal category and POS tag labels,
		/// but needn't (and couldn't be expected to) work on arbitrary
		/// Word strings.
		/// </summary>
		/// <param name="category">The whole String name of the label</param>
		/// <returns>The basic category of the String</returns>
		string BasicCategory(string category);

		/// <summary>
		/// Returns the category for a String with everything following
		/// the gf character (which may be language specific) stripped.
		/// </summary>
		/// <param name="category">The String name of the label (may previously have had basic category called on it)</param>
		/// <returns>The String stripped of grammatical functions</returns>
		string StripGF(string category);

		/// <summary>
		/// Returns a
		/// <see cref="Java.Util.Function.IFunction{T, R}">IFunction{T, R}</see>
		/// object that maps Strings to Strings according
		/// to this TreebankLanguagePack's basicCategory method.
		/// </summary>
		/// <returns>the String-&gt;String Function object</returns>
		IFunction<string, string> GetBasicCategoryFunction();

		/// <summary>Returns the syntactic category and 'function' of a String.</summary>
		/// <remarks>
		/// Returns the syntactic category and 'function' of a String.
		/// This normally involves truncating numerical coindexation
		/// showing coreference, etc.  By 'function', this means
		/// keeping, say, Penn Treebank functional tags or ICE phrasal functions,
		/// perhaps returning them as
		/// <c>category-function</c>
		/// .
		/// </remarks>
		/// <param name="category">The whole String name of the label</param>
		/// <returns>A String giving the category and function</returns>
		string CategoryAndFunction(string category);

		/// <summary>
		/// Returns a
		/// <see cref="Java.Util.Function.IFunction{T, R}">IFunction{T, R}</see>
		/// object that maps Strings to Strings according
		/// to this TreebankLanguagePack's categoryAndFunction method.
		/// </summary>
		/// <returns>the String-&gt;String Function object</returns>
		IFunction<string, string> GetCategoryAndFunctionFunction();

		/// <summary>Accepts a String that is a start symbol of the treebank.</summary>
		/// <param name="str">The str to test</param>
		/// <returns>Whether this is a start symbol</returns>
		bool IsStartSymbol(string str);

		/// <summary>
		/// Return a filter that accepts a String that is a start symbol
		/// of the treebank, and rejects everything else.
		/// </summary>
		/// <returns>The filter</returns>
		IPredicate<string> StartSymbolAcceptFilter();

		/// <summary>Returns a String array of treebank start symbols.</summary>
		/// <returns>The start symbols</returns>
		string[] StartSymbols();

		/// <summary>
		/// Returns a String which is the first (perhaps unique) start symbol
		/// of the treebank, or null if none is defined.
		/// </summary>
		/// <returns>The start symbol</returns>
		string StartSymbol();

		/// <summary>Returns the extension of treebank files for this treebank.</summary>
		/// <remarks>
		/// Returns the extension of treebank files for this treebank.
		/// This should be passed as an argument to Treebank loading classes.
		/// It might be "mrg" or "fid" or whatever.  Don't include the period.
		/// </remarks>
		/// <returns>the extension on files for this treebank</returns>
		string TreebankFileExtension();

		/// <summary>Sets the grammatical function indicating character to gfCharacter.</summary>
		/// <param name="gfCharacter">
		/// Sets the character in label names that sets of
		/// grammatical function marking (from the phrase label).
		/// </param>
		void SetGfCharacter(char gfCharacter);

		/// <summary>
		/// Returns a TreeReaderFactory suitable for general purpose use
		/// with this language/treebank.
		/// </summary>
		/// <returns>
		/// A TreeReaderFactory suitable for general purpose use
		/// with this language/treebank.
		/// </returns>
		ITreeReaderFactory TreeReaderFactory();

		/// <summary>Return a TokenizerFactory for Trees of this language/treebank.</summary>
		/// <returns>A TokenizerFactory for Trees of this language/treebank.</returns>
		ITokenizerFactory<Tree> TreeTokenizerFactory();

		/// <summary>The HeadFinder to use for your treebank.</summary>
		/// <returns>A suitable HeadFinder</returns>
		IHeadFinder HeadFinder();

		/// <summary>The HeadFinder to use when making typed dependencies.</summary>
		/// <returns>A suitable HeadFinder</returns>
		IHeadFinder TypedDependencyHeadFinder();

		/// <summary>The morphological feature specification for the language.</summary>
		/// <returns>A language-specific MorphoFeatureSpecification</returns>
		MorphoFeatureSpecification MorphFeatureSpec();

		/// <summary>
		/// Used for languages where an original Stanford Dependency
		/// converter and a Universal Dependency converter exists.
		/// </summary>
		void SetGenerateOriginalDependencies(bool generateOriginalDependencies);

		/// <summary>
		/// Used for languages where an original Stanford Dependency
		/// converter and a Universal Dependency converter exists.
		/// </summary>
		bool GenerateOriginalDependencies();
	}

	public static class TreebankLanguagePackConstants
	{
		/// <summary>
		/// Use this as the default encoding for Readers and Writers of
		/// Treebank data.
		/// </summary>
		public const string DefaultEncoding = "UTF-8";
	}
}
