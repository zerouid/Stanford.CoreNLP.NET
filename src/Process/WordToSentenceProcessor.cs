using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// Transforms a List of words into a List of Lists of words (that is, a List
	/// of sentences), by grouping the words.
	/// </summary>
	/// <remarks>
	/// Transforms a List of words into a List of Lists of words (that is, a List
	/// of sentences), by grouping the words.  The word stream is assumed to
	/// already be adequately tokenized, and this class just divides the List into
	/// sentences, perhaps discarding some separator tokens as it goes.
	/// <p>
	/// The main behavior is to look for sentence ending tokens like "." or "?!?",
	/// and to split after them and any following sentence closers like ")".
	/// Overlaid on this is an overall choice of state: The WordToSentenceProcessor
	/// can be a non-splitter, which always returns one sentence. Otherwise, the
	/// WordToSentenceProcessor will also split based on paragraphs using one of
	/// these three states: (1) Ignore line breaks in splitting sentences,
	/// (2) Treat each line as a separate paragraph, or (3) Treat two consecutive
	/// line breaks as marking the end of a paragraph. The details of sentence
	/// breaking within paragraphs is controlled based on the following three
	/// variables:
	/// <ul>
	/// <li>sentenceBoundaryTokens are tokens that are left in a sentence, but are
	/// to be regarded as ending a sentence.  A canonical example is a period.
	/// If two of these follow each other, the second will be a sentence
	/// consisting of only the sentenceBoundaryToken.
	/// <li>sentenceBoundaryFollowers are tokens that are left in a sentence, and
	/// which can follow a sentenceBoundaryToken while still belonging to
	/// the previous sentence.  They cannot begin a sentence (except at the
	/// beginning of a document).  A canonical example is a close parenthesis
	/// ')'.
	/// <li>sentenceBoundaryToDiscard are tokens which separate sentences and
	/// which should be thrown away.  In web documents, a typical example would
	/// be a '
	/// <c>&lt;p&gt;</c>
	/// ' tag.  If two of these follow each other, they are
	/// coalesced: no empty Sentence is output.  The end-of-file is not
	/// represented in this Set, but the code behaves as if it were a member.
	/// <li>regionElementRegex A regular expression for element names containing
	/// a sentence region. Only tokens in such elements will be included in
	/// sentences. The start and end tags themselves are not included in the
	/// sentence.
	/// </ul>
	/// Instances of this class are now immutable. ☺
	/// </remarks>
	/// <author>Joseph Smarr (jsmarr@stanford.edu)</author>
	/// <author>Christopher Manning</author>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	public class WordToSentenceProcessor<In> : IListProcessor<IN, IList<IN>>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Process.WordToSentenceProcessor));

		/// <summary>Turning this on is good for debugging sentence splitting.</summary>
		private const bool Debug = false;

		public enum NewlineIsSentenceBreak
		{
			Never,
			Always,
			TwoConsecutive
		}

		/// <summary>Default pattern for sentence ending punctuation.</summary>
		/// <remarks>Default pattern for sentence ending punctuation. Now Chinese-friendly as well as English.</remarks>
		public const string DefaultBoundaryRegex = "[.。]|[!?！？]+";

		/// <summary>
		/// Pe = Close_Punctuation (close brackets), Pf = Final_Punctuation (close quotes);
		/// add straight quotes, PTB escaped right brackets (-RRB-, etc.), greater than as close angle bracket,
		/// and those forms in full width range.
		/// </summary>
		public const string DefaultBoundaryFollowersRegex = "[\\p{Pe}\\p{Pf}\"'>＂＇＞]|''|-R[CRS]B-";

		public static readonly ICollection<string> DefaultSentenceBoundariesToDiscard = Java.Util.Collections.UnmodifiableSet(Generics.NewHashSet(Arrays.AsList(WhitespaceLexer.Newline, PTBTokenizer.GetNewlineToken())));

		/// <summary>Regex for tokens (Strings) that qualify as sentence-final tokens.</summary>
		private readonly Pattern sentenceBoundaryTokenPattern;

		/// <summary>Regex for multi token sequences that qualify as sentence-final tokens.</summary>
		/// <remarks>
		/// Regex for multi token sequences that qualify as sentence-final tokens.
		/// (i.e. use if you want to sentence split on 2 or more newlines)
		/// </remarks>
		private readonly SequencePattern<IN> sentenceBoundaryMultiTokenPattern;

		/// <summary>
		/// Regex for tokens (Strings) that qualify as tokens that can follow
		/// what normally counts as an end of sentence token, and which are
		/// attributed to the preceding sentence.
		/// </summary>
		/// <remarks>
		/// Regex for tokens (Strings) that qualify as tokens that can follow
		/// what normally counts as an end of sentence token, and which are
		/// attributed to the preceding sentence.  For example ")" coming after
		/// a period.
		/// </remarks>
		private readonly Pattern sentenceBoundaryFollowersPattern;

		/// <summary>List of regex Pattern that are sentence boundaries to be discarded.</summary>
		/// <remarks>
		/// List of regex Pattern that are sentence boundaries to be discarded.
		/// This is normally newline tokens or representations of them.
		/// </remarks>
		private readonly ICollection<string> sentenceBoundaryToDiscard;

		/// <summary>Patterns that match the start and end tags of XML elements.</summary>
		/// <remarks>
		/// Patterns that match the start and end tags of XML elements. These will
		/// be discarded, but taken to mark a sentence boundary.
		/// The value will be null if there are no such elements being used
		/// (for efficiency).
		/// </remarks>
		private readonly IList<Pattern> xmlBreakElementsToDiscard;

		/// <summary>
		/// List of regex Patterns that are not to be treated as sentence boundaries but should be discarded
		/// (i.e.
		/// </summary>
		/// <remarks>
		/// List of regex Patterns that are not to be treated as sentence boundaries but should be discarded
		/// (i.e. these may have been used with context to identify sentence boundaries but are not needed any more)
		/// </remarks>
		private readonly IList<Pattern> tokenPatternsToDiscard;

		private readonly Pattern sentenceRegionBeginPattern;

		private readonly Pattern sentenceRegionEndPattern;

		private readonly WordToSentenceProcessor.NewlineIsSentenceBreak newlineIsSentenceBreak;

		private readonly bool isOneSentence;

		/// <summary>Whether to output empty sentences.</summary>
		private readonly bool allowEmptySentences;

		// todo [cdm Aug 2012]: This should be unified with the PlainTextIterator
		// in DocumentPreprocessor, perhaps by making this one implement Iterator.
		// (DocumentProcessor once used to use this class, but now doesn't....)
		public static WordToSentenceProcessor.NewlineIsSentenceBreak StringToNewlineIsSentenceBreak(string name)
		{
			if ("always".Equals(name))
			{
				return WordToSentenceProcessor.NewlineIsSentenceBreak.Always;
			}
			else
			{
				if ("never".Equals(name))
				{
					return WordToSentenceProcessor.NewlineIsSentenceBreak.Never;
				}
				else
				{
					if (name != null && name.Contains("two"))
					{
						return WordToSentenceProcessor.NewlineIsSentenceBreak.TwoConsecutive;
					}
					else
					{
						throw new ArgumentException("Not a valid NewlineIsSentenceBreak name: '" + name + "' (should be one of 'always', 'never', 'two')");
					}
				}
			}
		}

		/// <summary>This is a sort of hacked in other way to end sentences.</summary>
		/// <remarks>
		/// This is a sort of hacked in other way to end sentences.
		/// Tokens with the ForcedSentenceEndAnnotation set to true
		/// will also end a sentence.
		/// </remarks>
		private static bool IsForcedEndToken(object o)
		{
			if (o is ICoreMap)
			{
				bool forcedEndValue = ((ICoreMap)o).Get(typeof(CoreAnnotations.ForcedSentenceEndAnnotation));
				string originalText = ((ICoreMap)o).Get(typeof(CoreAnnotations.OriginalTextAnnotation));
				return (forcedEndValue != null && forcedEndValue) || (originalText != null && originalText.Equals("\u2029"));
			}
			else
			{
				return false;
			}
		}

		private static string GetString(object o)
		{
			if (o is IHasWord)
			{
				IHasWord h = (IHasWord)o;
				return h.Word();
			}
			else
			{
				if (o is string)
				{
					return (string)o;
				}
				else
				{
					if (o is ICoreMap)
					{
						return ((ICoreMap)o).Get(typeof(CoreAnnotations.TextAnnotation));
					}
					else
					{
						throw new Exception("Expected token to be either Word or String.");
					}
				}
			}
		}

		private static bool Matches(IList<Pattern> patterns, string word)
		{
			foreach (Pattern p in patterns)
			{
				Matcher m = p.Matcher(word);
				if (m.Matches())
				{
					return true;
				}
			}
			return false;
		}

		private bool MatchesXmlBreakElementToDiscard(string word)
		{
			return Matches(xmlBreakElementsToDiscard, word);
		}

		private bool MatchesTokenPatternsToDiscard(string word)
		{
			return Matches(tokenPatternsToDiscard, word);
		}

		/// <summary>
		/// Returns a List of Lists where each element is built from a run
		/// of Words in the input Document.
		/// </summary>
		/// <remarks>
		/// Returns a List of Lists where each element is built from a run
		/// of Words in the input Document. Specifically, reads through each word in
		/// the input document and breaks off a sentence after finding a valid
		/// sentence boundary token or end of file.
		/// Note that for this to work, the words in the
		/// input document must have been tokenized with a tokenizer that makes
		/// sentence boundary tokens their own tokens (e.g.,
		/// <see cref="PTBTokenizer{T}"/>
		/// ).
		/// </remarks>
		/// <param name="words">A list of already tokenized words (must implement HasWord or be a String).</param>
		/// <returns>A list of sentences.</returns>
		/// <seealso cref="WordToSentenceProcessor{IN}.WordToSentenceProcessor(string, string, Java.Util.ISet{E}, Java.Util.ISet{E}, string, NewlineIsSentenceBreak, Edu.Stanford.Nlp.Ling.Tokensregex.SequencePattern{T}, Java.Util.ISet{E}, bool, bool)"/>
		public virtual IList<IList<IN>> Process<_T0>(IList<_T0> words)
			where _T0 : IN
		{
			// todo [cdm 2016]: Should really sort out generics here so don't need to have extra list copying
			if (isOneSentence)
			{
				// put all the words in one sentence
				IList<IList<IN>> sentences = Generics.NewArrayList();
				sentences.Add(new List<IN>(words));
				return sentences;
			}
			else
			{
				return WordsToSentences(words);
			}
		}

		/// <summary>
		/// Returns a List of Lists where each element is built from a run
		/// of Words in the input Document.
		/// </summary>
		/// <remarks>
		/// Returns a List of Lists where each element is built from a run
		/// of Words in the input Document. Specifically, reads through each word in
		/// the input document and breaks off a sentence after finding a valid
		/// sentence boundary token or end of file.
		/// Note that for this to work, the words in the
		/// input document must have been tokenized with a tokenizer that makes
		/// sentence boundary tokens their own tokens (e.g.,
		/// <see cref="PTBTokenizer{T}"/>
		/// ).
		/// </remarks>
		/// <param name="words">A list of already tokenized words (must implement HasWord or be a String).</param>
		/// <returns>A list of sentences.</returns>
		/// <seealso cref="WordToSentenceProcessor{IN}.WordToSentenceProcessor(string, string, Java.Util.ISet{E}, Java.Util.ISet{E}, string, NewlineIsSentenceBreak, Edu.Stanford.Nlp.Ling.Tokensregex.SequencePattern{T}, Java.Util.ISet{E}, bool, bool)"/>
		private IList<IList<IN>> WordsToSentences<_T0>(IList<_T0> words)
			where _T0 : IN
		{
			IdentityHashMap<object, bool> isSentenceBoundary = null;
			// is null unless used by sentenceBoundaryMultiTokenPattern
			if (sentenceBoundaryMultiTokenPattern != null)
			{
				// Do initial pass using TokensRegex to identify multi token patterns that need to be matched
				// and add the last token of a match to our table of sentence boundary tokens.
				isSentenceBoundary = new IdentityHashMap<object, bool>();
				SequenceMatcher<IN> matcher = sentenceBoundaryMultiTokenPattern.GetMatcher(words);
				while (matcher.Find())
				{
					IList<IN> nodes = matcher.GroupNodes();
					if (nodes != null && !nodes.IsEmpty())
					{
						isSentenceBoundary[nodes[nodes.Count - 1]] = true;
					}
				}
			}
			// Split tokens into sentences!!!
			IList<IList<IN>> sentences = Generics.NewArrayList();
			IList<IN> currentSentence = new List<IN>();
			IList<IN> lastSentence = null;
			bool insideRegion = false;
			bool inWaitForForcedEnd = false;
			bool lastTokenWasNewline = false;
			bool lastSentenceEndForced = false;
			foreach (IN o in words)
			{
				string word = GetString(o);
				bool forcedEnd = IsForcedEndToken(o);
				// if (DEBUG) { if (forcedEnd) { log.info("Word is " + word + "; marks forced end of sentence [cont.]"); } }
				bool inMultiTokenExpr = false;
				bool discardToken = false;
				if (o is ICoreMap)
				{
					// Hacky stuff to ensure sentence breaks do not happen in certain cases
					ICoreMap cm = (ICoreMap)o;
					if (!forcedEnd)
					{
						bool forcedUntilEndValue = cm.Get(typeof(CoreAnnotations.ForcedSentenceUntilEndAnnotation));
						if (forcedUntilEndValue != null && forcedUntilEndValue)
						{
							// if (DEBUG) { log.info("Word is " + word + "; starting wait for forced end of sentence [cont.]"); }
							inWaitForForcedEnd = true;
						}
						else
						{
							MultiTokenTag mt = cm.Get(typeof(CoreAnnotations.MentionTokenAnnotation));
							if (mt != null && !mt.IsEnd())
							{
								// In the middle of a multi token mention, make sure sentence is not ended here
								// if (DEBUG) { log.info("Word is " + word + "; inside multi-token mention [cont.]"); }
								inMultiTokenExpr = true;
							}
						}
					}
				}
				if (tokenPatternsToDiscard != null)
				{
					discardToken = MatchesTokenPatternsToDiscard(word);
				}
				if (sentenceRegionBeginPattern != null && !insideRegion)
				{
					if (sentenceRegionBeginPattern.Matcher(word).Matches())
					{
						insideRegion = true;
					}
					lastTokenWasNewline = false;
					continue;
				}
				if (!lastSentenceEndForced && lastSentence != null && currentSentence.IsEmpty() && !lastTokenWasNewline && sentenceBoundaryFollowersPattern.Matcher(word).Matches())
				{
					if (!discardToken)
					{
						lastSentence.Add(o);
					}
					lastTokenWasNewline = false;
					continue;
				}
				bool newSentForced = false;
				bool newSent = false;
				string debugText = (discardToken) ? "discarded" : "added to current";
				if (inWaitForForcedEnd && !forcedEnd)
				{
					if (sentenceBoundaryToDiscard.Contains(word))
					{
						// there can be newlines even in something to keep together
						discardToken = true;
					}
					if (!discardToken)
					{
						currentSentence.Add(o);
					}
				}
				else
				{
					if (inMultiTokenExpr && !forcedEnd)
					{
						if (!discardToken)
						{
							currentSentence.Add(o);
						}
					}
					else
					{
						if (sentenceBoundaryToDiscard.Contains(word))
						{
							if (forcedEnd)
							{
								// sentence boundary can easily be forced end
								inWaitForForcedEnd = false;
								newSentForced = true;
							}
							else
							{
								if (newlineIsSentenceBreak == WordToSentenceProcessor.NewlineIsSentenceBreak.Always)
								{
									newSentForced = true;
								}
								else
								{
									if (newlineIsSentenceBreak == WordToSentenceProcessor.NewlineIsSentenceBreak.TwoConsecutive && lastTokenWasNewline)
									{
										newSentForced = true;
									}
								}
							}
							lastTokenWasNewline = true;
						}
						else
						{
							lastTokenWasNewline = false;
							bool isb;
							if (xmlBreakElementsToDiscard != null && MatchesXmlBreakElementToDiscard(word))
							{
								newSentForced = true;
							}
							else
							{
								if (sentenceRegionEndPattern != null && sentenceRegionEndPattern.Matcher(word).Matches())
								{
									insideRegion = false;
									newSentForced = true;
								}
								else
								{
									// Marked sentence boundaries
									if ((isSentenceBoundary != null) && ((isb = isSentenceBoundary[o]) != null) && isb)
									{
										if (!discardToken)
										{
											currentSentence.Add(o);
										}
										newSent = true;
									}
									else
									{
										if (sentenceBoundaryTokenPattern.Matcher(word).Matches())
										{
											if (!discardToken)
											{
												currentSentence.Add(o);
											}
											newSent = true;
										}
										else
										{
											if (forcedEnd)
											{
												if (!discardToken)
												{
													currentSentence.Add(o);
												}
												inWaitForForcedEnd = false;
												newSentForced = true;
											}
											else
											{
												if (!discardToken)
												{
													currentSentence.Add(o);
												}
												// chris added this next test in 2017; a bit weird, but KBP setup doesn't have newline in sentenceBoundary patterns, just in toDiscard
												if (AbstractTokenizer.NewlineToken.Equals(word))
												{
													lastTokenWasNewline = true;
												}
											}
										}
									}
								}
							}
						}
					}
				}
				if ((newSentForced || newSent) && (!currentSentence.IsEmpty() || allowEmptySentences))
				{
					sentences.Add(currentSentence);
					// adds this sentence now that it's complete
					lastSentenceEndForced = ((lastSentence == null || lastSentence.IsEmpty()) && lastSentenceEndForced) || newSentForced;
					lastSentence = currentSentence;
					currentSentence = new List<IN>();
				}
				else
				{
					// clears the current sentence
					if (newSentForced)
					{
						lastSentenceEndForced = true;
					}
				}
			}
			// add any words at the end, even if there isn't a sentence
			// terminator at the end of file
			if (!currentSentence.IsEmpty())
			{
				sentences.Add(currentSentence);
			}
			// adds last sentence
			return sentences;
		}

		public virtual IDocument<L, F, IList<IN>> ProcessDocument<L, F>(IDocument<L, F, IN> @in)
		{
			IDocument<L, F, IList<IN>> doc = @in.BlankDocument();
			Sharpen.Collections.AddAll(doc, Process(@in));
			return doc;
		}

		/// <summary>
		/// Create a
		/// <c>WordToSentenceProcessor</c>
		/// using a sensible default
		/// list of tokens for sentence ending for English/Latin writing systems.
		/// The default set is: {".","?","!"} and
		/// any combination of ! or ?, as in !!!?!?!?!!!?!!?!!!.
		/// A sequence of two or more consecutive line breaks is taken as a paragraph break
		/// which also splits sentences. This is the usual constructor for sentence
		/// breaking reasonable text, which uses hard-line breaking, so two
		/// blank lines indicate a paragraph break.
		/// People commonly use this constructor.
		/// </summary>
		public WordToSentenceProcessor()
			: this(false)
		{
		}

		/// <summary>
		/// Create a
		/// <c>WordToSentenceProcessor</c>
		/// using a sensible default
		/// list of tokens for sentence ending for English/Latin writing systems.
		/// The default set is: {".","?","!"} and
		/// any combination of ! or ?, as in !!!?!?!?!!!?!!?!!!.
		/// You can specify the treatment of newlines as sentence breaks as one
		/// of ignored, every newline is a sentence break, or only two or more
		/// consecutive newlines are a sentence break.
		/// </summary>
		/// <param name="newlineIsSentenceBreak">
		/// Strategy for treating newlines as
		/// paragraph breaks.
		/// </param>
		public WordToSentenceProcessor(WordToSentenceProcessor.NewlineIsSentenceBreak newlineIsSentenceBreak)
			: this(DefaultBoundaryRegex, newlineIsSentenceBreak, false)
		{
		}

		/// <summary>
		/// Create a
		/// <c>WordToSentenceProcessor</c>
		/// which never breaks the input
		/// into multiple sentences. If the argument is true, the input stream
		/// is always output as one sentence. (If it is false, this is
		/// equivalent to the no argument constructor, so why use this?)
		/// </summary>
		/// <param name="isOneSentence">
		/// Marker argument: true means to treat input
		/// as one sentence
		/// </param>
		public WordToSentenceProcessor(bool isOneSentence)
			: this(DefaultBoundaryRegex, WordToSentenceProcessor.NewlineIsSentenceBreak.TwoConsecutive, isOneSentence)
		{
		}

		/// <summary>
		/// Set the set of Strings that will mark the end of a sentence,
		/// and which will be discarded after doing so.
		/// </summary>
		/// <remarks>
		/// Set the set of Strings that will mark the end of a sentence,
		/// and which will be discarded after doing so.
		/// This constructor is used for, and usually only for, doing
		/// one-sentence-per-line sentence splitting.  Since in such cases, you
		/// generally want to strictly preserve the set of lines in the input,
		/// it preserves empty lines as empty sentences in the output.
		/// </remarks>
		/// <param name="boundaryToDiscard">
		/// A Set of String that will be matched
		/// with .equals() and will mark an
		/// end of sentence and be discarded.
		/// </param>
		public WordToSentenceProcessor(ICollection<string> boundaryToDiscard)
			: this(string.Empty, string.Empty, boundaryToDiscard, null, null, WordToSentenceProcessor.NewlineIsSentenceBreak.Always, null, null, false, true)
		{
		}

		/// <summary>
		/// Create a basic
		/// <c>WordToSentenceProcessor</c>
		/// specifying just a few top-level options.
		/// </summary>
		/// <param name="boundaryTokenRegex">The set of boundary tokens</param>
		/// <param name="newlineIsSentenceBreak">Strategy for treating newlines as sentence breaks</param>
		/// <param name="isOneSentence">
		/// Whether to treat whole text as one sentence
		/// (if true, the other two parameters are ignored).
		/// </param>
		public WordToSentenceProcessor(string boundaryTokenRegex, WordToSentenceProcessor.NewlineIsSentenceBreak newlineIsSentenceBreak, bool isOneSentence)
			: this(boundaryTokenRegex, DefaultBoundaryFollowersRegex, DefaultSentenceBoundariesToDiscard, null, null, newlineIsSentenceBreak, null, null, isOneSentence, false)
		{
		}

		/// <summary>
		/// Flexibly set the set of acceptable sentence boundary tokens, but with
		/// a default set of allowed boundary following tokens.
		/// </summary>
		/// <remarks>
		/// Flexibly set the set of acceptable sentence boundary tokens, but with
		/// a default set of allowed boundary following tokens. Also can set sentence boundary
		/// to discard tokens and xmlBreakElementsToDiscard and set the treatment of newlines
		/// (boundaryToDiscard) as sentence ends.
		/// This one is convenient in allowing any of the first 3 arguments to be null,
		/// and then the usual defaults are substituted for it.
		/// The allowed set of boundary followers is the regex: "[\\p{Pe}\\p{Pf}'\"]|''|-R[CRS]B-".
		/// The default set of discarded separator tokens includes the
		/// newline tokens used by WhitespaceLexer and PTBLexer.
		/// </remarks>
		/// <param name="boundaryTokenRegex">The regex of boundary tokens. If null, use default.</param>
		/// <param name="boundaryFollowersRegex">
		/// The regex of boundary following tokens. If null, use default.
		/// These are tokens which should normally be added on to the current sentence
		/// even after something normally sentence ending has been seen. For example,
		/// typically a close parenthesis or close quotes goes with the current sentence,
		/// even after a period or question mark have been seen.
		/// </param>
		/// <param name="boundaryToDiscard">
		/// The set of regex for sentence boundary tokens that should be discarded.
		/// If null, use default.
		/// </param>
		/// <param name="xmlBreakElementsToDiscard">
		/// xml element names like "p", which will be recognized,
		/// treated as sentence ends, and discarded.
		/// If null, use none.
		/// </param>
		/// <param name="newlineIsSentenceBreak">Strategy for counting line ends (boundaryToDiscard) as sentence ends.</param>
		public WordToSentenceProcessor(string boundaryTokenRegex, string boundaryFollowersRegex, ICollection<string> boundaryToDiscard, ICollection<string> xmlBreakElementsToDiscard, WordToSentenceProcessor.NewlineIsSentenceBreak newlineIsSentenceBreak
			, SequencePattern<IN> sentenceBoundaryMultiTokenPattern, ICollection<string> tokenRegexesToDiscard)
			: this(boundaryTokenRegex == null ? DefaultBoundaryRegex : boundaryTokenRegex, boundaryFollowersRegex == null ? DefaultBoundaryFollowersRegex : boundaryFollowersRegex, boundaryToDiscard == null || boundaryToDiscard.IsEmpty() ? DefaultSentenceBoundariesToDiscard
				 : boundaryToDiscard, xmlBreakElementsToDiscard == null ? Java.Util.Collections.EmptySet() : xmlBreakElementsToDiscard, null, newlineIsSentenceBreak, sentenceBoundaryMultiTokenPattern, tokenRegexesToDiscard, false, false)
		{
		}

		/// <summary>Configure all parameters for converting a list of tokens into sentences.</summary>
		/// <remarks>
		/// Configure all parameters for converting a list of tokens into sentences.
		/// The whole enchilada.
		/// </remarks>
		/// <param name="boundaryTokenRegex">
		/// Tokens that match this regex will end a
		/// sentence, but are retained at the end of
		/// the sentence. Substantive value must be supplied.
		/// </param>
		/// <param name="boundaryFollowersRegex">
		/// This is a Set of String that are matched with
		/// .equals() which are allowed to be tacked onto
		/// the end of a sentence after a sentence boundary
		/// token, for example ")". Substantive value must be supplied.
		/// </param>
		/// <param name="boundariesToDiscard">
		/// This is normally used for newline tokens if
		/// they are included in the tokenization. They
		/// may end the sentence (depending on the setting
		/// of newlineIsSentenceBreak), but at any rate
		/// are deleted from sentences in the output.
		/// Substantive value must be supplied.
		/// </param>
		/// <param name="xmlBreakElementsToDiscard">
		/// These are elements like "p" or "sent",
		/// which will be wrapped into regex for
		/// approximate XML matching. They will be
		/// deleted in the output, and will always
		/// trigger a sentence boundary.
		/// May be null; means discard none.
		/// </param>
		/// <param name="regionElementRegex">
		/// XML element name regex to delimit regions processed.
		/// Tokens outside one of these elements are discarded.
		/// May be null; means to not filter by regions
		/// </param>
		/// <param name="newlineIsSentenceBreak">How to treat newlines. Must have substantive value.</param>
		/// <param name="sentenceBoundaryMultiTokenPattern">
		/// A TokensRegex multi-token pattern for finding boundaries.
		/// May be null; means that there are no such patterns.
		/// </param>
		/// <param name="tokenRegexesToDiscard">
		/// Regex for tokens to discard.
		/// May be null; means that no tokens are discarded in this way.
		/// </param>
		/// <param name="isOneSentence">
		/// Whether to treat whole of input as one sentence regardless.
		/// Must have substantive value. Overrides anything else.
		/// </param>
		/// <param name="allowEmptySentences">
		/// Whether to allow empty sentences to be output
		/// Must have substantive value. Often suppressed, but don't want that in things like
		/// strict one-sentence-per-line mode.
		/// </param>
		public WordToSentenceProcessor(string boundaryTokenRegex, string boundaryFollowersRegex, ICollection<string> boundariesToDiscard, ICollection<string> xmlBreakElementsToDiscard, string regionElementRegex, WordToSentenceProcessor.NewlineIsSentenceBreak
			 newlineIsSentenceBreak, SequencePattern<IN> sentenceBoundaryMultiTokenPattern, ICollection<string> tokenRegexesToDiscard, bool isOneSentence, bool allowEmptySentences)
		{
			/* ---------- Constructors --------- */
			sentenceBoundaryTokenPattern = Pattern.Compile(boundaryTokenRegex);
			sentenceBoundaryFollowersPattern = Pattern.Compile(boundaryFollowersRegex);
			sentenceBoundaryToDiscard = Java.Util.Collections.UnmodifiableSet(boundariesToDiscard);
			if (xmlBreakElementsToDiscard == null || xmlBreakElementsToDiscard.IsEmpty())
			{
				this.xmlBreakElementsToDiscard = null;
			}
			else
			{
				this.xmlBreakElementsToDiscard = new List<Pattern>(xmlBreakElementsToDiscard.Count);
				foreach (string s in xmlBreakElementsToDiscard)
				{
					string regex = "<\\s*(?:/\\s*)?(?:" + s + ")(?:\\s+[^>]+?|\\s*(?:/\\s*)?)>";
					// log.info("Regex is |" + regex + "|");
					// todo: Historically case insensitive, but maybe better and more proper to make case sensitive?
					this.xmlBreakElementsToDiscard.Add(Pattern.Compile(regex, Pattern.CaseInsensitive | Pattern.UnicodeCase));
				}
			}
			if (regionElementRegex != null)
			{
				sentenceRegionBeginPattern = Pattern.Compile("<\\s*(?:" + regionElementRegex + ")(?:\\s+[^>]+?)?>");
				sentenceRegionEndPattern = Pattern.Compile("<\\s*/\\s*(?:" + regionElementRegex + ")\\s*>");
			}
			else
			{
				sentenceRegionBeginPattern = null;
				sentenceRegionEndPattern = null;
			}
			this.newlineIsSentenceBreak = newlineIsSentenceBreak;
			this.sentenceBoundaryMultiTokenPattern = sentenceBoundaryMultiTokenPattern;
			if (tokenRegexesToDiscard != null)
			{
				this.tokenPatternsToDiscard = new List<Pattern>(tokenRegexesToDiscard.Count);
				foreach (string s in tokenRegexesToDiscard)
				{
					this.tokenPatternsToDiscard.Add(Pattern.Compile(s));
				}
			}
			else
			{
				this.tokenPatternsToDiscard = null;
			}
			this.isOneSentence = isOneSentence;
			this.allowEmptySentences = allowEmptySentences;
		}
	}
}
