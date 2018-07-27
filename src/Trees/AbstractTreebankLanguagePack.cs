using System;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This provides an implementation of parts of the TreebankLanguagePack
	/// API to reduce the load on fresh implementations.
	/// </summary>
	/// <remarks>
	/// This provides an implementation of parts of the TreebankLanguagePack
	/// API to reduce the load on fresh implementations.  Only the abstract
	/// methods below need to be implemented to give a reasonable solution for
	/// a new language.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <version>1.1</version>
	[System.Serializable]
	public abstract class AbstractTreebankLanguagePack : ITreebankLanguagePack
	{
		/// <summary>So changed versions deserialize correctly.</summary>
		private const long serialVersionUID = -6506749780512708352L;

		/// <summary>
		/// Default character for indicating that something is a grammatical fn; probably should be overridden by
		/// lang specific ones
		/// </summary>
		protected internal char gfCharacter;

		protected internal const char DefaultGfChar = '-';

		/// <summary>
		/// Use this as the default encoding for Readers and Writers of
		/// Treebank data.
		/// </summary>
		public const string DefaultEncoding = "UTF-8";

		/// <summary>
		/// For languages where a Universal Dependency converter
		/// exists this variable determines whether the original
		/// or the Universal converter will be used.
		/// </summary>
		protected internal bool generateOriginalDependencies;

		/// <summary>Gives a handle to the TreebankLanguagePack.</summary>
		public AbstractTreebankLanguagePack()
			: this(DefaultGfChar)
		{
			punctTagStringAcceptFilter = Filters.CollectionAcceptFilter(PunctuationTags());
			punctWordStringAcceptFilter = Filters.CollectionAcceptFilter(PunctuationWords());
			sFPunctTagStringAcceptFilter = Filters.CollectionAcceptFilter(SentenceFinalPunctuationTags());
			eIPunctTagStringAcceptFilter = Filters.CollectionAcceptFilter(EvalBIgnoredPunctuationTags());
			startSymbolAcceptFilter = Filters.CollectionAcceptFilter(StartSymbols());
		}

		/// <summary>Gives a handle to the TreebankLanguagePack.</summary>
		/// <param name="gfChar">The character that sets of grammatical functions in node labels.</param>
		public AbstractTreebankLanguagePack(char gfChar)
		{
			punctTagStringAcceptFilter = Filters.CollectionAcceptFilter(PunctuationTags());
			punctWordStringAcceptFilter = Filters.CollectionAcceptFilter(PunctuationWords());
			sFPunctTagStringAcceptFilter = Filters.CollectionAcceptFilter(SentenceFinalPunctuationTags());
			eIPunctTagStringAcceptFilter = Filters.CollectionAcceptFilter(EvalBIgnoredPunctuationTags());
			startSymbolAcceptFilter = Filters.CollectionAcceptFilter(StartSymbols());
			//Grammatical function parameters
			this.gfCharacter = gfChar;
		}

		/// <summary>Returns a String array of punctuation tags for this treebank/language.</summary>
		/// <returns>The punctuation tags</returns>
		public abstract string[] PunctuationTags();

		/// <summary>Returns a String array of punctuation words for this treebank/language.</summary>
		/// <returns>The punctuation words</returns>
		public abstract string[] PunctuationWords();

		/// <summary>
		/// Returns a String array of sentence final punctuation tags for this
		/// treebank/language.
		/// </summary>
		/// <returns>The sentence final punctuation tags</returns>
		public abstract string[] SentenceFinalPunctuationTags();

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
		public virtual string[] EvalBIgnoredPunctuationTags()
		{
			return PunctuationTags();
		}

		/// <summary>
		/// Accepts a String that is a punctuation
		/// tag name, and rejects everything else.
		/// </summary>
		/// <returns>Whether this is a punctuation tag</returns>
		public virtual bool IsPunctuationTag(string str)
		{
			return punctTagStringAcceptFilter.Test(str);
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
		public virtual bool IsPunctuationWord(string str)
		{
			return punctWordStringAcceptFilter.Test(str);
		}

		/// <summary>
		/// Accepts a String that is a sentence end
		/// punctuation tag, and rejects everything else.
		/// </summary>
		/// <returns>Whether this is a sentence final punctuation tag</returns>
		public virtual bool IsSentenceFinalPunctuationTag(string str)
		{
			return sFPunctTagStringAcceptFilter.Test(str);
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
		public virtual bool IsEvalBIgnoredPunctuationTag(string str)
		{
			return eIPunctTagStringAcceptFilter.Test(str);
		}

		/// <summary>
		/// Return a filter that accepts a String that is a punctuation
		/// tag name, and rejects everything else.
		/// </summary>
		/// <returns>The filter</returns>
		public virtual IPredicate<string> PunctuationTagAcceptFilter()
		{
			return punctTagStringAcceptFilter;
		}

		/// <summary>
		/// Return a filter that rejects a String that is a punctuation
		/// tag name, and rejects everything else.
		/// </summary>
		/// <returns>The filter</returns>
		public virtual IPredicate<string> PunctuationTagRejectFilter()
		{
			return Filters.NotFilter(punctTagStringAcceptFilter);
		}

		/// <summary>
		/// Returns a filter that accepts a String that is a punctuation
		/// word, and rejects everything else.
		/// </summary>
		/// <remarks>
		/// Returns a filter that accepts a String that is a punctuation
		/// word, and rejects everything else.
		/// If one can't tell for sure (as for ' in the Penn Treebank), it
		/// makes the best guess that it can.
		/// </remarks>
		/// <returns>The Filter</returns>
		public virtual IPredicate<string> PunctuationWordAcceptFilter()
		{
			return punctWordStringAcceptFilter;
		}

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
		public virtual IPredicate<string> PunctuationWordRejectFilter()
		{
			return Filters.NotFilter(punctWordStringAcceptFilter);
		}

		/// <summary>
		/// Returns a filter that accepts a String that is a sentence end
		/// punctuation tag, and rejects everything else.
		/// </summary>
		/// <returns>The Filter</returns>
		public virtual IPredicate<string> SentenceFinalPunctuationTagAcceptFilter()
		{
			return sFPunctTagStringAcceptFilter;
		}

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
		public virtual IPredicate<string> EvalBIgnoredPunctuationTagAcceptFilter()
		{
			return eIPunctTagStringAcceptFilter;
		}

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
		public virtual IPredicate<string> EvalBIgnoredPunctuationTagRejectFilter()
		{
			return Filters.NotFilter(eIPunctTagStringAcceptFilter);
		}

		/// <summary>Return the input Charset encoding for the Treebank.</summary>
		/// <remarks>
		/// Return the input Charset encoding for the Treebank.
		/// See documentation for the <code>Charset</code> class.
		/// </remarks>
		/// <returns>Name of Charset</returns>
		public virtual string GetEncoding()
		{
			return DefaultEncoding;
		}

		private static readonly char[] EmptyCharArray = new char[0];

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
		public virtual char[] LabelAnnotationIntroducingCharacters()
		{
			return EmptyCharArray;
		}

		/// <summary>
		/// Returns the index of the first character that is after the basic
		/// label.
		/// </summary>
		/// <remarks>
		/// Returns the index of the first character that is after the basic
		/// label.  That is, if category is "NP-LGS", it returns 2.
		/// This routine assumes category != null.
		/// This routine returns 0 iff the String is of length 0.
		/// This routine always returns a number &lt;= category.length(), and
		/// so it is safe to pass it as an argument to category.substring().
		/// <p>
		/// NOTE: the routine should never allow the first character of a label
		/// to be taken as the annotation introducing character, because in the
		/// Penn Treebank, "-" is a valid tag, but also the character used to
		/// set off functional and co-indexing annotations. If the first letter is
		/// such a character then a matched character is also not used, for
		/// -LRB- etc., iff there is an intervening character (so --PU becomes -).
		/// </remarks>
		/// <param name="category">Phrasal category</param>
		/// <returns>
		/// The index of the first character that is after the basic
		/// label
		/// </returns>
		private int PostBasicCategoryIndex(string category)
		{
			bool sawAtZero = false;
			char seenAtZero = '\u0000';
			int i = 0;
			for (int leng = category.Length; i < leng; i++)
			{
				char ch = category[i];
				if (IsLabelAnnotationIntroducingCharacter(ch))
				{
					if (i == 0)
					{
						sawAtZero = true;
						seenAtZero = ch;
					}
					else
					{
						if (sawAtZero && i > 1 && ch == seenAtZero)
						{
							sawAtZero = false;
						}
						else
						{
							// still skip past identical ones for weird negra-penn "---CJ" (should we just delete it?)
							// if (i + 1 < leng && category.charAt(i + 1) == ch) {
							// keep looping
							// } else {
							break;
						}
					}
				}
			}
			// }
			return i;
		}

		/// <summary>Returns the basic syntactic category of a String.</summary>
		/// <remarks>
		/// Returns the basic syntactic category of a String.
		/// This implementation basically truncates
		/// stuff after an occurrence of one of the
		/// <code>labelAnnotationIntroducingCharacters()</code>.
		/// However, there is also special case stuff to deal with
		/// labelAnnotationIntroducingCharacters in category labels:
		/// (i) if the first char is in this set, it's never truncated
		/// (e.g., '-' or '=' as a token), and (ii) if it starts with
		/// one of this set, a second instance of the same item from this set is
		/// also excluded (to deal with '-LLB-', '-RCB-', etc.).
		/// </remarks>
		/// <param name="category">The whole String name of the label</param>
		/// <returns>The basic category of the String</returns>
		public virtual string BasicCategory(string category)
		{
			if (category == null)
			{
				return null;
			}
			return Sharpen.Runtime.Substring(category, 0, PostBasicCategoryIndex(category));
		}

		public virtual string StripGF(string category)
		{
			if (category == null)
			{
				return null;
			}
			int index = category.LastIndexOf(gfCharacter);
			if (index > 0)
			{
				category = Sharpen.Runtime.Substring(category, 0, index);
			}
			return category;
		}

		/// <summary>
		/// Returns a
		/// <see cref="Java.Util.Function.IFunction{T, R}">IFunction{T, R}</see>
		/// object that maps Strings to Strings according
		/// to this TreebankLanguagePack's basicCategory() method.
		/// </summary>
		/// <returns>The String-&gt;String Function object</returns>
		public virtual IFunction<string, string> GetBasicCategoryFunction()
		{
			return new AbstractTreebankLanguagePack.BasicCategoryStringFunction(this);
		}

		[System.Serializable]
		private class BasicCategoryStringFunction : IFunction<string, string>
		{
			private const long serialVersionUID = 1L;

			private ITreebankLanguagePack tlp;

			internal BasicCategoryStringFunction(ITreebankLanguagePack tlp)
			{
				this.tlp = tlp;
			}

			public virtual string Apply(string @in)
			{
				return tlp.BasicCategory(@in);
			}
		}

		[System.Serializable]
		private class CategoryAndFunctionStringFunction : IFunction<string, string>
		{
			private const long serialVersionUID = 1L;

			private ITreebankLanguagePack tlp;

			internal CategoryAndFunctionStringFunction(ITreebankLanguagePack tlp)
			{
				this.tlp = tlp;
			}

			public virtual string Apply(string @in)
			{
				return tlp.CategoryAndFunction(@in);
			}
		}

		/// <summary>Returns the syntactic category and 'function' of a String.</summary>
		/// <remarks>
		/// Returns the syntactic category and 'function' of a String.
		/// This normally involves truncating numerical coindexation
		/// showing coreference, etc.  By 'function', this means
		/// keeping, say, Penn Treebank functional tags or ICE phrasal functions,
		/// perhaps returning them as <code>category-function</code>.
		/// <p/>
		/// This implementation strips numeric tags after label introducing
		/// characters (assuming that non-numeric things are functional tags).
		/// </remarks>
		/// <param name="category">The whole String name of the label</param>
		/// <returns>A String giving the category and function</returns>
		public virtual string CategoryAndFunction(string category)
		{
			if (category == null)
			{
				return null;
			}
			string catFunc = category;
			int i = LastIndexOfNumericTag(catFunc);
			while (i >= 0)
			{
				catFunc = Sharpen.Runtime.Substring(catFunc, 0, i);
				i = LastIndexOfNumericTag(catFunc);
			}
			return catFunc;
		}

		/// <summary>
		/// Returns the index within this string of the last occurrence of a
		/// isLabelAnnotationIntroducingCharacter which is followed by only
		/// digits, corresponding to a numeric tag at the end of the string.
		/// </summary>
		/// <remarks>
		/// Returns the index within this string of the last occurrence of a
		/// isLabelAnnotationIntroducingCharacter which is followed by only
		/// digits, corresponding to a numeric tag at the end of the string.
		/// Example: <code>lastIndexOfNumericTag("NP-TMP-1") returns
		/// 6</code>.
		/// </remarks>
		/// <param name="category">A String category</param>
		/// <returns>
		/// The index within this string of the last occurrence of a
		/// isLabelAnnotationIntroducingCharacter which is followed by only
		/// digits
		/// </returns>
		private int LastIndexOfNumericTag(string category)
		{
			if (category == null)
			{
				return -1;
			}
			int last = -1;
			for (int i = category.Length - 1; i >= 0; i--)
			{
				if (IsLabelAnnotationIntroducingCharacter(category[i]))
				{
					bool onlyDigitsFollow = false;
					for (int j = i + 1; j < category.Length; j++)
					{
						onlyDigitsFollow = true;
						if (!(char.IsDigit(category[j])))
						{
							onlyDigitsFollow = false;
							break;
						}
					}
					if (onlyDigitsFollow)
					{
						last = i;
					}
				}
			}
			return last;
		}

		/// <summary>
		/// Returns a
		/// <see cref="Java.Util.Function.IFunction{T, R}">IFunction{T, R}</see>
		/// object that maps Strings to Strings according
		/// to this TreebankLanguagePack's categoryAndFunction() method.
		/// </summary>
		/// <returns>The String-&gt;String Function object</returns>
		public virtual IFunction<string, string> GetCategoryAndFunctionFunction()
		{
			return new AbstractTreebankLanguagePack.CategoryAndFunctionStringFunction(this);
		}

		/// <summary>
		/// Say whether this character is an annotation introducing
		/// character.
		/// </summary>
		/// <param name="ch">The character to check</param>
		/// <returns>Whether it is an annotation introducing character</returns>
		public virtual bool IsLabelAnnotationIntroducingCharacter(char ch)
		{
			char[] cutChars = LabelAnnotationIntroducingCharacters();
			foreach (char cutChar in cutChars)
			{
				if (ch == cutChar)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Accepts a String that is a start symbol of the treebank.</summary>
		/// <returns>Whether this is a start symbol</returns>
		public virtual bool IsStartSymbol(string str)
		{
			return startSymbolAcceptFilter.Test(str);
		}

		/// <summary>
		/// Return a filter that accepts a String that is a start symbol
		/// of the treebank, and rejects everything else.
		/// </summary>
		/// <returns>The filter</returns>
		public virtual IPredicate<string> StartSymbolAcceptFilter()
		{
			return startSymbolAcceptFilter;
		}

		/// <summary>Returns a String array of treebank start symbols.</summary>
		/// <returns>The start symbols</returns>
		public abstract string[] StartSymbols();

		/// <summary>
		/// Returns a String which is the first (perhaps unique) start symbol
		/// of the treebank, or null if none is defined.
		/// </summary>
		/// <returns>The start symbol</returns>
		public virtual string StartSymbol()
		{
			string[] ssyms = StartSymbols();
			if (ssyms == null || ssyms.Length == 0)
			{
				return null;
			}
			return ssyms[0];
		}

		private readonly IPredicate<string> punctTagStringAcceptFilter;

		private readonly IPredicate<string> punctWordStringAcceptFilter;

		private readonly IPredicate<string> sFPunctTagStringAcceptFilter;

		private readonly IPredicate<string> eIPunctTagStringAcceptFilter;

		private readonly IPredicate<string> startSymbolAcceptFilter;

		/// <summary>
		/// Return a tokenizer which might be suitable for tokenizing text that
		/// will be used with this Treebank/Language pair, without tokenizing carriage returns (i.e., treating them as white space).
		/// </summary>
		/// <remarks>
		/// Return a tokenizer which might be suitable for tokenizing text that
		/// will be used with this Treebank/Language pair, without tokenizing carriage returns (i.e., treating them as white space).  The implementation in AbstractTreebankLanguagePack
		/// returns a factory for
		/// <see cref="Edu.Stanford.Nlp.Process.WhitespaceTokenizer{T}"/>
		/// .
		/// </remarks>
		/// <returns>A tokenizer</returns>
		public virtual ITokenizerFactory<IHasWord> GetTokenizerFactory()
		{
			return WhitespaceTokenizer.Factory(false);
		}

		/// <summary>Return a GrammaticalStructureFactory suitable for this language/treebank.</summary>
		/// <remarks>
		/// Return a GrammaticalStructureFactory suitable for this language/treebank.
		/// (To be overridden in subclasses.)
		/// </remarks>
		/// <returns>A GrammaticalStructureFactory suitable for this language/treebank</returns>
		public virtual IGrammaticalStructureFactory GrammaticalStructureFactory()
		{
			throw new NotSupportedException("No GrammaticalStructureFactory (typed dependencies) available for language/treebank " + GetType().FullName);
		}

		/// <summary>Return a GrammaticalStructureFactory suitable for this language/treebank.</summary>
		/// <remarks>
		/// Return a GrammaticalStructureFactory suitable for this language/treebank.
		/// (To be overridden in subclasses.)
		/// </remarks>
		/// <returns>A GrammaticalStructureFactory suitable for this language/treebank</returns>
		public virtual IGrammaticalStructureFactory GrammaticalStructureFactory(IPredicate<string> puncFilt)
		{
			return GrammaticalStructureFactory();
		}

		/// <summary>Return a GrammaticalStructureFactory suitable for this language/treebank.</summary>
		/// <remarks>
		/// Return a GrammaticalStructureFactory suitable for this language/treebank.
		/// (To be overridden in subclasses.)
		/// </remarks>
		/// <returns>A GrammaticalStructureFactory suitable for this language/treebank</returns>
		public virtual IGrammaticalStructureFactory GrammaticalStructureFactory(IPredicate<string> puncFilt, IHeadFinder typedDependencyHeadFinder)
		{
			return GrammaticalStructureFactory();
		}

		public virtual bool SupportsGrammaticalStructures()
		{
			return false;
		}

		public virtual char GetGfCharacter()
		{
			return gfCharacter;
		}

		public virtual void SetGfCharacter(char gfCharacter)
		{
			this.gfCharacter = gfCharacter;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ITreeReaderFactory TreeReaderFactory()
		{
			return new PennTreeReaderFactory();
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ITokenizerFactory<Tree> TreeTokenizerFactory()
		{
			return new TreeTokenizerFactory(TreeReaderFactory());
		}

		/// <summary>Returns a morphological feature specification for words in this language.</summary>
		public virtual MorphoFeatureSpecification MorphFeatureSpec()
		{
			return null;
		}

		public virtual void SetGenerateOriginalDependencies(bool generateOriginalDependencies)
		{
			this.generateOriginalDependencies = generateOriginalDependencies;
		}

		public virtual bool GenerateOriginalDependencies()
		{
			return this.generateOriginalDependencies;
		}

		public abstract IHeadFinder HeadFinder();

		public abstract string[] SentenceFinalPunctuationWords();

		public abstract string TreebankFileExtension();

		public abstract IHeadFinder TypedDependencyHeadFinder();
	}
}
