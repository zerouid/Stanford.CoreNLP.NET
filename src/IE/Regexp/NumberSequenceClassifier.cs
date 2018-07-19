using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Time;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Regexp
{
	/// <summary>
	/// A set of deterministic rules for marking certain entities, to add
	/// categories and to correct for failures of statistical NER taggers.
	/// </summary>
	/// <remarks>
	/// A set of deterministic rules for marking certain entities, to add
	/// categories and to correct for failures of statistical NER taggers.
	/// This is an extremely simple and ungeneralized implementation of
	/// AbstractSequenceClassifier that was written for PASCAL RTE.
	/// It could profitably be extended and generalized.
	/// It marks a NUMBER category based on part-of-speech tags in a
	/// deterministic manner.
	/// It marks an ORDINAL category based on word form in a deterministic manner.
	/// It tags as MONEY currency signs and things tagged CD after a currency sign.
	/// It marks a number before a month name as a DATE.
	/// It marks as a DATE a word of the form xx/xx/xxxx
	/// (where x is a digit from a suitable range).
	/// It marks as a TIME a word of the form x(x):xx (where x is a digit).
	/// It marks everything else tagged "CD" as a NUMBER, and instances
	/// of "and" appearing between CD tags in contexts suggestive of a number.
	/// It requires text to be POS-tagged (have the getString(TagAnnotation.class) attribute).
	/// Effectively these rules assume that
	/// this classifier will be used as a secondary classifier by
	/// code such as ClassifierCombiner: it will mark most CD as NUMBER, and it
	/// is assumed that something else with higher priority is marking ones that
	/// are PERCENT, ADDRESS, etc.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Mihai (integrated with NumberNormalizer, SUTime)</author>
	public class NumberSequenceClassifier : AbstractSequenceClassifier<CoreLabel>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Regexp.NumberSequenceClassifier));

		private const bool Debug = false;

		private readonly bool useSUTime;

		public static readonly bool UseSutimeDefault = TimeExpressionExtractorFactory.DefaultExtractorPresent;

		public const string UseSutimeProperty = "ner.useSUTime";

		public const string UseSutimePropertyBase = "useSUTime";

		public const string SutimeProperty = "sutime";

		private readonly ITimeExpressionExtractor timexExtractor;

		public NumberSequenceClassifier()
			: this(new Properties(), UseSutimeDefault, new Properties())
		{
			if (!CurrencyWordPattern.Matcher("pounds").Matches())
			{
				log.Info("NumberSequence: Currency pattern broken");
			}
		}

		public NumberSequenceClassifier(bool useSUTime)
			: this(new Properties(), useSUTime, new Properties())
		{
		}

		public NumberSequenceClassifier(Properties props, bool useSUTime, Properties sutimeProps)
			: base(props)
		{
			this.useSUTime = useSUTime;
			if (this.useSUTime)
			{
				this.timexExtractor = TimeExpressionExtractorFactory.CreateExtractor(SutimeProperty, sutimeProps);
			}
			else
			{
				this.timexExtractor = null;
			}
		}

		/// <summary>
		/// Classify a
		/// <see cref="System.Collections.IList{E}"/>
		/// of
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s.
		/// </summary>
		/// <param name="document">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s.
		/// </param>
		/// <returns>
		/// the same
		/// <see cref="System.Collections.IList{E}"/>
		/// , but with the elements annotated
		/// with their answers.
		/// </returns>
		public override IList<CoreLabel> Classify(IList<CoreLabel> document)
		{
			return ClassifyWithGlobalInformation(document, null, null);
		}

		public override IList<CoreLabel> ClassifyWithGlobalInformation(IList<CoreLabel> tokens, ICoreMap document, ICoreMap sentence)
		{
			if (useSUTime)
			{
				return ClassifyWithSUTime(tokens, document, sentence);
			}
			return ClassifyOld(tokens);
		}

		public override void FinalizeClassification(ICoreMap document)
		{
			if (useSUTime)
			{
				timexExtractor.Finalize(document);
			}
		}

		// todo [cdm, 2013]: Where does this call NumberNormalizer?  Is it the call buried in SUTime's TimeExpressionExtractorImpl?
		/// <summary>Modular classification using NumberNormalizer for numbers, SUTime for date/time.</summary>
		/// <remarks>
		/// Modular classification using NumberNormalizer for numbers, SUTime for date/time.
		/// Note: this is slower than classifyOld because it runs multiple passes
		/// over the tokens (one for numbers and dates, and others for money and ordinals).
		/// However, the slowdown is not substantial since the passes are fast. Plus,
		/// the code is much cleaner than before...
		/// </remarks>
		/// <param name="tokenSequence"/>
		private IList<CoreLabel> ClassifyWithSUTime(IList<CoreLabel> tokenSequence, ICoreMap document, ICoreMap sentence)
		{
			//
			// set everything to "O" by default
			//
			foreach (CoreLabel token in tokenSequence)
			{
				if (token.Get(typeof(CoreAnnotations.AnswerAnnotation)) == null)
				{
					token.Set(typeof(CoreAnnotations.AnswerAnnotation), flags.backgroundSymbol);
				}
			}
			//
			// run SUTime
			// note: SUTime requires TextAnnotation to be set at document/sent level and
			//   that the Character*Offset annotations be aligned with the token words.
			//   This is guaranteed because here we work on a copy generated by copyTokens()
			//
			ICoreMap timeSentence = (sentence != null ? AlignSentence(sentence) : BuildSentenceFromTokens(tokenSequence));
			IList<ICoreMap> timeExpressions = RunSUTime(timeSentence, document);
			IList<ICoreMap> numbers = timeSentence.Get(typeof(CoreAnnotations.NumerizedTokensAnnotation));
			//
			// store DATE and TIME
			//
			if (timeExpressions != null)
			{
				foreach (ICoreMap timeExpression in timeExpressions)
				{
					// todo [cdm 2013]: We should also store these in the Sentence, but we've just got the list of tokens here
					int start = timeExpression.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
					int end = timeExpression.Get(typeof(CoreAnnotations.TokenEndAnnotation));
					int offset = 0;
					if (sentence != null && sentence.ContainsKey(typeof(CoreAnnotations.TokenBeginAnnotation)))
					{
						offset = sentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
					}
					Timex timex = timeExpression.Get(typeof(TimeAnnotations.TimexAnnotation));
					if (timex != null)
					{
						// for(Class key: timeExpression.keySet()) log.info("\t" + key + ": " + timeExpression.get(key));
						string label = timex.TimexType();
						for (int i = start; i < end; i++)
						{
							CoreLabel token_1 = tokenSequence[i - offset];
							if (token_1.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals(flags.backgroundSymbol))
							{
								token_1.Set(typeof(CoreAnnotations.AnswerAnnotation), label);
								token_1.Set(typeof(TimeAnnotations.TimexAnnotation), timex);
							}
						}
					}
				}
			}
			//
			// store the numbers found by SUTime as NUMBER if they are not part of anything else
			//
			if (numbers != null)
			{
				foreach (ICoreMap number in numbers)
				{
					if (number.ContainsKey(typeof(CoreAnnotations.NumericCompositeValueAnnotation)))
					{
						int start = number.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
						int end = number.Get(typeof(CoreAnnotations.TokenEndAnnotation));
						int offset = 0;
						if (sentence != null && sentence.ContainsKey(typeof(CoreAnnotations.TokenBeginAnnotation)))
						{
							offset = sentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
						}
						string type = number.Get(typeof(CoreAnnotations.NumericCompositeTypeAnnotation));
						Number value = number.Get(typeof(CoreAnnotations.NumericCompositeValueAnnotation));
						if (type != null)
						{
							for (int i = start; i < end; i++)
							{
								CoreLabel token_1 = tokenSequence[i - offset];
								if (token_1.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals(flags.backgroundSymbol))
								{
									token_1.Set(typeof(CoreAnnotations.AnswerAnnotation), type);
									if (value != null)
									{
										token_1.Set(typeof(CoreAnnotations.NumericCompositeValueAnnotation), value);
									}
								}
							}
						}
					}
				}
			}
			// everything tagged as CD is also a number
			// NumberNormalizer probably catches these but let's be safe
			// use inverted "CD".equals() because tag could be null (if no POS info available)
			foreach (CoreLabel token_2 in tokenSequence)
			{
				if ("CD".Equals(token_2.Tag()) && token_2.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals(flags.backgroundSymbol))
				{
					token_2.Set(typeof(CoreAnnotations.AnswerAnnotation), "NUMBER");
				}
			}
			// extract money and percents
			MoneyAndPercentRecognizer(tokenSequence);
			// ordinals
			// NumberNormalizer probably catches these but let's be safe
			OrdinalRecognizer(tokenSequence);
			return tokenSequence;
		}

		/// <summary>Copies one sentence replicating only information necessary for SUTime</summary>
		/// <param name="sentence"/>
		public static ICoreMap AlignSentence(ICoreMap sentence)
		{
			string text = sentence.Get(typeof(CoreAnnotations.TextAnnotation));
			if (text != null)
			{
				// original text is preserved; no need to align anything
				return sentence;
			}
			ICoreMap newSentence = BuildSentenceFromTokens(sentence.Get(typeof(CoreAnnotations.TokensAnnotation)), sentence.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)), sentence.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
			newSentence.Set(typeof(CoreAnnotations.TokenBeginAnnotation), sentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation)));
			newSentence.Set(typeof(CoreAnnotations.TokenEndAnnotation), sentence.Get(typeof(CoreAnnotations.TokenEndAnnotation)));
			return newSentence;
		}

		private static ICoreMap BuildSentenceFromTokens(IList<CoreLabel> tokens)
		{
			return BuildSentenceFromTokens(tokens, null, null);
		}

		private static ICoreMap BuildSentenceFromTokens(IList<CoreLabel> tokens, int characterOffsetStart, int characterOffsetEnd)
		{
			//
			// Recover the sentence text:
			// a) try to get it from TextAnnotation
			// b) if not present, build it from the OriginalTextAnnotation of each token
			// c) if not present, build it from the TextAnnotation of each token
			//
			bool adjustCharacterOffsets = false;
			// try to recover the text from the original tokens
			string text = BuildText(tokens, typeof(CoreAnnotations.OriginalTextAnnotation));
			if (text == null)
			{
				text = BuildText(tokens, typeof(CoreAnnotations.TextAnnotation));
				// character offset will point to the original tokens
				//   so we need to align them to the text built from normalized tokens
				adjustCharacterOffsets = true;
				if (text == null)
				{
					throw new Exception("ERROR: to use SUTime, sentences must have TextAnnotation set, or the individual tokens must have OriginalTextAnnotation or TextAnnotation set!");
				}
			}
			// make sure token character offsets are aligned with text
			IList<CoreLabel> tokenSequence = CopyTokens(tokens, adjustCharacterOffsets, false);
			Annotation newSentence = new Annotation(text);
			newSentence.Set(typeof(CoreAnnotations.TokensAnnotation), tokenSequence);
			if (!adjustCharacterOffsets && characterOffsetStart != null && characterOffsetEnd != null)
			{
				newSentence.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), characterOffsetStart);
				newSentence.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), characterOffsetEnd);
			}
			else
			{
				int tokenCharStart = tokenSequence[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				int tokenCharEnd = tokenSequence[tokenSequence.Count - 1].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				newSentence.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), tokenCharStart);
				newSentence.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), tokenCharEnd);
			}
			// some default token offsets
			newSentence.Set(typeof(CoreAnnotations.TokenBeginAnnotation), 0);
			newSentence.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokenSequence.Count);
			return newSentence;
		}

		private static string BuildText(IList<CoreLabel> tokens, Type textAnnotation)
		{
			StringBuilder os = new StringBuilder();
			for (int i = 0; i < sz; i++)
			{
				CoreLabel crt = tokens[i];
				// System.out.println("\t" + crt.word() + "\t" + crt.get(CoreAnnotations.CharacterOffsetBeginAnnotation.class) + "\t" + crt.get(CoreAnnotations.CharacterOffsetEndAnnotation.class));
				if (i > 0)
				{
					CoreLabel prev = tokens[i - 1];
					int spaces = 1;
					if (crt.ContainsKey(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)))
					{
						spaces = crt.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) - prev.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
					}
					while (spaces > 0)
					{
						os.Append(' ');
						spaces--;
					}
				}
				string word = crt.Get(textAnnotation);
				if (word == null)
				{
					// this annotation does not exist; bail out
					return null;
				}
				os.Append(word);
			}
			return os.ToString();
		}

		/// <summary>Runs SUTime and converts its output into NamedEntityTagAnnotations</summary>
		/// <param name="sentence"/>
		/// <param name="document">Contains document-level annotations such as DocDateAnnotation</param>
		private IList<ICoreMap> RunSUTime(ICoreMap sentence, ICoreMap document)
		{
			/*
			log.info("PARSING SENTENCE: " + sentence.get(CoreAnnotations.TextAnnotation.class));
			for(CoreLabel t: sentence.get(CoreAnnotations.TokensAnnotation.class)){
			log.info("TOKEN: \"" + t.word() + "\" \"" + t.get(CoreAnnotations.OriginalTextAnnotation.class) + "\" " + t.get(CoreAnnotations.CharacterOffsetBeginAnnotation.class) + " " + t.get(CoreAnnotations.CharacterOffsetEndAnnotation.class));
			}
			*/
			IList<ICoreMap> timeExpressions = timexExtractor.ExtractTimeExpressionCoreMaps(sentence, document);
			if (timeExpressions != null)
			{
			}
			return timeExpressions;
		}

		/// <summary>Recognizes money and percents.</summary>
		/// <remarks>
		/// Recognizes money and percents.
		/// This accepts currency symbols (e.g., $) both before and after numbers; but it accepts units
		/// (e.g., "dollar") only after numbers.
		/// </remarks>
		/// <param name="tokenSequence">The list of tokens to find money and percents in</param>
		private void MoneyAndPercentRecognizer(IList<CoreLabel> tokenSequence)
		{
			for (int i = 0; i < tokenSequence.Count; i++)
			{
				CoreLabel crt = tokenSequence[i];
				CoreLabel next = (i < tokenSequence.Count - 1 ? tokenSequence[i + 1] : null);
				CoreLabel prev = (i > 0 ? tokenSequence[i - 1] : null);
				// $5
				if (CurrencySymbolPattern.Matcher(crt.Word()).Matches() && next != null && (next.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("NUMBER") || "CD".Equals(next.Tag())))
				{
					crt.Set(typeof(CoreAnnotations.AnswerAnnotation), "MONEY");
					i = ChangeLeftToRight(tokenSequence, i + 1, next.Get(typeof(CoreAnnotations.AnswerAnnotation)), next.Tag(), "MONEY") - 1;
				}
				else
				{
					// 5$, 5 dollars
					if ((CurrencyWordPattern.Matcher(crt.Word()).Matches() || CurrencySymbolPattern.Matcher(crt.Word()).Matches()) && prev != null && (prev.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("NUMBER") || "CD".Equals(prev.Tag())) && !LeftScanFindsWeightWord
						(tokenSequence, i))
					{
						crt.Set(typeof(CoreAnnotations.AnswerAnnotation), "MONEY");
						ChangeRightToLeft(tokenSequence, i - 1, prev.Get(typeof(CoreAnnotations.AnswerAnnotation)), prev.Tag(), "MONEY");
					}
					else
					{
						// 5%, 5 percent
						if ((PercentWordPattern.Matcher(crt.Word()).Matches() || PercentSymbolPattern.Matcher(crt.Word()).Matches()) && prev != null && (prev.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("NUMBER") || "CD".Equals(prev.Tag())))
						{
							crt.Set(typeof(CoreAnnotations.AnswerAnnotation), "PERCENT");
							ChangeRightToLeft(tokenSequence, i - 1, prev.Get(typeof(CoreAnnotations.AnswerAnnotation)), prev.Tag(), "PERCENT");
						}
					}
				}
			}
		}

		/// <summary>Recognizes ordinal numbers</summary>
		/// <param name="tokenSequence"/>
		private void OrdinalRecognizer(IList<CoreLabel> tokenSequence)
		{
			foreach (CoreLabel crt in tokenSequence)
			{
				if ((crt.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals(flags.backgroundSymbol) || crt.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("NUMBER")) && OrdinalPattern.Matcher(crt.Word()).Matches())
				{
					crt.Set(typeof(CoreAnnotations.AnswerAnnotation), "ORDINAL");
				}
			}
		}

		private int ChangeLeftToRight(IList<CoreLabel> tokens, int start, string oldTag, string posTag, string newTag)
		{
			while (start < tokens.Count)
			{
				CoreLabel crt = tokens[start];
				// we are scanning for a NER tag and found something different
				if (!oldTag.Equals(flags.backgroundSymbol) && !crt.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals(oldTag))
				{
					break;
				}
				// the NER tag is not set, so we scan for similar POS tags
				if (oldTag.Equals(flags.backgroundSymbol) && !crt.Tag().Equals(posTag))
				{
					break;
				}
				crt.Set(typeof(CoreAnnotations.AnswerAnnotation), newTag);
				start++;
			}
			return start;
		}

		private int ChangeRightToLeft(IList<CoreLabel> tokens, int start, string oldTag, string posTag, string newTag)
		{
			while (start >= 0)
			{
				CoreLabel crt = tokens[start];
				// we are scanning for a NER tag and found something different
				if (!oldTag.Equals(flags.backgroundSymbol) && !crt.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals(oldTag))
				{
					break;
				}
				// the NER tag is not set, so we scan for similar POS tags
				if (oldTag.Equals(flags.backgroundSymbol) && !crt.Tag().Equals(posTag))
				{
					break;
				}
				crt.Set(typeof(CoreAnnotations.AnswerAnnotation), newTag);
				start--;
			}
			return start;
		}

		/// <summary>
		/// Aligns the character offsets of these tokens with the actual text stored in each token
		/// Note that this copies the list ONLY when we need to adjust the character offsets.
		/// </summary>
		/// <remarks>
		/// Aligns the character offsets of these tokens with the actual text stored in each token
		/// Note that this copies the list ONLY when we need to adjust the character offsets. Otherwise, it keeps the original list.
		/// Note that this looks first at OriginalTextAnnotation and only when null at TextAnnotation.
		/// </remarks>
		/// <param name="srcList"/>
		/// <param name="adjustCharacterOffsets">If true, it adjust the character offsets to match exactly with the token lengths</param>
		private static IList<CoreLabel> CopyTokens(IList<CoreLabel> srcList, bool adjustCharacterOffsets, bool forceCopy)
		{
			// no need to adjust anything; use the original list
			if (!adjustCharacterOffsets && !forceCopy)
			{
				return srcList;
			}
			IList<CoreLabel> dstList = new List<CoreLabel>();
			int adjustment = 0;
			int offset = 0;
			// for when offsets are not available
			foreach (CoreLabel src in srcList)
			{
				if (adjustCharacterOffsets)
				{
					int wordLength = (src.ContainsKey(typeof(CoreAnnotations.OriginalTextAnnotation))) ? src.Get(typeof(CoreAnnotations.OriginalTextAnnotation)).Length : src.Word().Length;
					// We try to preserve the old character offsets but they just don't work well for normalized token text
					// Also, in some cases, these offsets are not set
					if (src.ContainsKey(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) && src.ContainsKey(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)))
					{
						int start = src.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
						int end = src.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
						int origLength = end - start;
						start += adjustment;
						end = start + wordLength;
						dstList.Add(CopyCoreLabel(src, start, end));
						adjustment += wordLength - origLength;
					}
					else
					{
						int start = offset;
						int end = start + wordLength;
						offset = end + 1;
						// allow for one space character
						dstList.Add(CopyCoreLabel(src, start, end));
					}
				}
				else
				{
					dstList.Add(CopyCoreLabel(src, null, null));
				}
			}
			return dstList;
		}

		/// <summary>Transfer from src to dst all annotations generated bu SUTime and NumberNormalizer</summary>
		/// <param name="src"/>
		/// <param name="dst"/>
		public static void TransferAnnotations(CoreLabel src, CoreLabel dst)
		{
			//
			// annotations potentially set by NumberNormalizer
			//
			if (src.ContainsKey(typeof(CoreAnnotations.NumericCompositeValueAnnotation)))
			{
				dst.Set(typeof(CoreAnnotations.NumericCompositeValueAnnotation), src.Get(typeof(CoreAnnotations.NumericCompositeValueAnnotation)));
			}
			if (src.ContainsKey(typeof(CoreAnnotations.NumericCompositeTypeAnnotation)))
			{
				dst.Set(typeof(CoreAnnotations.NumericCompositeTypeAnnotation), src.Get(typeof(CoreAnnotations.NumericCompositeTypeAnnotation)));
			}
			//
			// annotations set by SUTime
			//
			if (src.ContainsKey(typeof(TimeAnnotations.TimexAnnotation)))
			{
				dst.Set(typeof(TimeAnnotations.TimexAnnotation), src.Get(typeof(TimeAnnotations.TimexAnnotation)));
			}
		}

		/// <summary>Create a copy of srcTokens, detecting on the fly if character offsets need adjusting</summary>
		/// <param name="srcTokens"/>
		/// <param name="srcSentence"/>
		public static IList<CoreLabel> CopyTokens(IList<CoreLabel> srcTokens, ICoreMap srcSentence)
		{
			bool adjustCharacterOffsets = false;
			if (srcSentence == null || srcSentence.Get(typeof(CoreAnnotations.TextAnnotation)) == null || srcTokens.IsEmpty() || srcTokens[0].Get(typeof(CoreAnnotations.OriginalTextAnnotation)) == null)
			{
				adjustCharacterOffsets = true;
			}
			return CopyTokens(srcTokens, adjustCharacterOffsets, true);
		}

		/// <summary>Copies only the fields required for numeric entity extraction into  the new CoreLabel.</summary>
		/// <param name="src">Source CoreLabel to copy.</param>
		private static CoreLabel CopyCoreLabel(CoreLabel src, int startOffset, int endOffset)
		{
			CoreLabel dst = new CoreLabel();
			dst.SetWord(src.Word());
			dst.SetTag(src.Tag());
			if (src.ContainsKey(typeof(CoreAnnotations.OriginalTextAnnotation)))
			{
				dst.Set(typeof(CoreAnnotations.OriginalTextAnnotation), src.Get(typeof(CoreAnnotations.OriginalTextAnnotation)));
			}
			if (startOffset == null)
			{
				dst.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), src.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)));
			}
			else
			{
				dst.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), startOffset);
			}
			if (endOffset == null)
			{
				dst.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), src.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
			}
			else
			{
				dst.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), endOffset);
			}
			TransferAnnotations(src, dst);
			return dst;
		}

		private static readonly Pattern MonthPattern = Pattern.Compile("January|Jan\\.?|February|Feb\\.?|March|Mar\\.?|April|Apr\\.?|May|June|Jun\\.?|July|Jul\\.?|August|Aug\\.?|September|Sept?\\.?|October|Oct\\.?|November|Nov\\.?|December|Dec\\.");

		private static readonly Pattern YearPattern = Pattern.Compile("[1-3][0-9]{3}|'?[0-9]{2}");

		private static readonly Pattern DayPattern = Pattern.Compile("(?:[1-9]|[12][0-9]|3[01])(?:st|nd|rd)?");

		private static readonly Pattern DatePattern = Pattern.Compile("(?:[1-9]|[0-3][0-9])\\\\?/(?:[1-9]|[0-3][0-9])\\\\?/[1-3][0-9]{3}");

		private static readonly Pattern DatePattern2 = Pattern.Compile("[12][0-9]{3}[-/](?:0?[1-9]|1[0-2])[-/][0-3][0-9]");

		private static readonly Pattern TimePattern = Pattern.Compile("[0-2]?[0-9]:[0-5][0-9]");

		private static readonly Pattern TimePattern2 = Pattern.Compile("[0-2][0-9]:[0-5][0-9]:[0-5][0-9]");

		private static readonly Pattern AmPm = Pattern.Compile("(a\\.?m\\.?)|(p\\.?m\\.?)", Pattern.CaseInsensitive);

		public static readonly Pattern CurrencyWordPattern = Pattern.Compile("(?:dollar|cent|euro|pound)s?|penny|pence|yen|yuan|won", Pattern.CaseInsensitive);

		public static readonly Pattern CurrencySymbolPattern = Pattern.Compile("\\$|#|&#163;|&pound;|\u00A3|\u00A5|\u20AC|\u20A9|(?:US|HK|A|C|NT|S|NZ)\\$", Pattern.CaseInsensitive);

		public static readonly Pattern OrdinalPattern = Pattern.Compile("(?i)[2-9]?1st|[2-9]?2nd|[2-9]?3rd|1[0-9]th|[2-9]?[04-9]th|100+th|zeroth|first|second|third|fourth|fifth|sixth|seventh|eighth|ninth|tenth|eleventh|twelfth|thirteenth|fourteenth|fifteenth|sixteenth|seventeenth|eighteenth|nineteenth|twentieth|twenty-first|twenty-second|twenty-third|twenty-fourth|twenty-fifth|twenty-sixth|twenty-seventh|twenty-eighth|twenty-ninth|thirtieth|thirty-first|fortieth|fiftieth|sixtieth|seventieth|eightieth|ninetieth|hundredth|thousandth|millionth"
			);

		public static readonly Pattern ArmyTimeMorning = Pattern.Compile("0([0-9])([0-9]){2}");

		public static readonly Pattern GenericTimeWords = Pattern.Compile("(morning|evening|night|noon|midnight|teatime|lunchtime|dinnertime|suppertime|afternoon|midday|dusk|dawn|sunup|sundown|daybreak|day)");

		public static readonly Pattern PercentWordPattern = Pattern.Compile("percent", Pattern.CaseInsensitive);

		public static readonly Pattern PercentSymbolPattern = Pattern.Compile("%");

		// pattern matches: dollar, pound sign XML escapes; pound sign, yen sign, euro, won; other country dollars; now omit # for pound
		// TODO: Delete # as currency.  But doing this involves changing PTBTokenizer currency normalization rules
		// Code \u0023 '#' was used for pound '£' in the ISO version of ASCII (ISO 646), and this is found in very old materials
		// e.g., the 1999 Penn Treebank, but we now don't recognize this, as it now doesn't occur and wrongly recognizes
		// currency whenever someone refers to the #4 country etc.
		// TODO: No longer include archaic # for pound
		private IList<CoreLabel> ClassifyOld(IList<CoreLabel> document)
		{
			// if (DEBUG) { log.info("NumberSequenceClassifier tagging"); }
			PaddedList<CoreLabel> pl = new PaddedList<CoreLabel>(document, pad);
			for (int i = 0; i < sz; i++)
			{
				CoreLabel me = pl[i];
				CoreLabel prev = pl[i - 1];
				CoreLabel next = pl[i + 1];
				CoreLabel next2 = pl[i + 2];
				//if (DEBUG) { log.info("Tagging:" + me.word()); }
				me.Set(typeof(CoreAnnotations.AnswerAnnotation), flags.backgroundSymbol);
				if (CurrencySymbolPattern.Matcher(me.Word()).Matches() && (prev.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("CD") || next.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("CD")))
				{
					// dollar, pound, pound, yen,
					// Penn Treebank ancient # as pound, euro,
					me.Set(typeof(CoreAnnotations.AnswerAnnotation), "MONEY");
				}
				else
				{
					if (me.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("CD"))
					{
						if (TimePattern.Matcher(me.Word()).Matches())
						{
							me.Set(typeof(CoreAnnotations.AnswerAnnotation), "TIME");
						}
						else
						{
							if (TimePattern2.Matcher(me.Word()).Matches())
							{
								me.Set(typeof(CoreAnnotations.AnswerAnnotation), "TIME");
							}
							else
							{
								if (DatePattern.Matcher(me.Word()).Matches())
								{
									me.Set(typeof(CoreAnnotations.AnswerAnnotation), "DATE");
								}
								else
								{
									if (DatePattern2.Matcher(me.Word()).Matches())
									{
										me.Set(typeof(CoreAnnotations.AnswerAnnotation), "DATE");
									}
									else
									{
										if (next.Get(typeof(CoreAnnotations.TextAnnotation)) != null && me.Get(typeof(CoreAnnotations.TextAnnotation)) != null && DayPattern.Matcher(me.Get(typeof(CoreAnnotations.TextAnnotation))).Matches() && MonthPattern.Matcher(next.Get(typeof(CoreAnnotations.TextAnnotation
											))).Matches())
										{
											// deterministically make DATE for British-style number before month
											me.Set(typeof(CoreAnnotations.AnswerAnnotation), "DATE");
										}
										else
										{
											if (prev.Get(typeof(CoreAnnotations.TextAnnotation)) != null && MonthPattern.Matcher(prev.Get(typeof(CoreAnnotations.TextAnnotation))).Matches() && me.Get(typeof(CoreAnnotations.TextAnnotation)) != null && DayPattern.Matcher(me.Get(typeof(CoreAnnotations.TextAnnotation
												))).Matches())
											{
												// deterministically make DATE for number after month
												me.Set(typeof(CoreAnnotations.AnswerAnnotation), "DATE");
											}
											else
											{
												if (RightScanFindsMoneyWord(pl, i) && !LeftScanFindsWeightWord(pl, i))
												{
													me.Set(typeof(CoreAnnotations.AnswerAnnotation), "MONEY");
												}
												else
												{
													if (ArmyTimeMorning.Matcher(me.Word()).Matches())
													{
														me.Set(typeof(CoreAnnotations.AnswerAnnotation), "TIME");
													}
													else
													{
														if (YearPattern.Matcher(me.Word()).Matches() && prev.GetString<CoreAnnotations.AnswerAnnotation>().Equals("DATE") && (MonthPattern.Matcher(prev.Word()).Matches() || pl[i - 2].Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("DATE")))
														{
															me.Set(typeof(CoreAnnotations.AnswerAnnotation), "DATE");
														}
														else
														{
															if (prev.GetString<CoreAnnotations.AnswerAnnotation>().Equals("MONEY"))
															{
																me.Set(typeof(CoreAnnotations.AnswerAnnotation), "MONEY");
															}
															else
															{
																me.Set(typeof(CoreAnnotations.AnswerAnnotation), "NUMBER");
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
					else
					{
						if (AmPm.Matcher(me.Word()).Matches() && prev.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("TIME"))
						{
							me.Set(typeof(CoreAnnotations.AnswerAnnotation), "TIME");
						}
						else
						{
							if (me.GetString<CoreAnnotations.PartOfSpeechAnnotation>() != null && me.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals(",") && prev.GetString<CoreAnnotations.AnswerAnnotation>().Equals("DATE") && next.Word() != null && YearPattern
								.Matcher(next.Word()).Matches())
							{
								me.Set(typeof(CoreAnnotations.AnswerAnnotation), "DATE");
							}
							else
							{
								if (me.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("NNP") && MonthPattern.Matcher(me.Word()).Matches())
								{
									if (prev.GetString<CoreAnnotations.AnswerAnnotation>().Equals("DATE") || next.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("CD"))
									{
										me.Set(typeof(CoreAnnotations.AnswerAnnotation), "DATE");
									}
								}
								else
								{
									if (me.GetString<CoreAnnotations.PartOfSpeechAnnotation>() != null && me.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("CC"))
									{
										if (prev.Tag() != null && prev.Tag().Equals("CD") && next.Tag() != null && next.Tag().Equals("CD") && me.Get(typeof(CoreAnnotations.TextAnnotation)) != null && Sharpen.Runtime.EqualsIgnoreCase(me.Get(typeof(CoreAnnotations.TextAnnotation)), 
											"and"))
										{
											string wd = prev.Word();
											if (Sharpen.Runtime.EqualsIgnoreCase(wd, "hundred") || Sharpen.Runtime.EqualsIgnoreCase(wd, "thousand") || Sharpen.Runtime.EqualsIgnoreCase(wd, "million") || Sharpen.Runtime.EqualsIgnoreCase(wd, "billion") || Sharpen.Runtime.EqualsIgnoreCase
												(wd, "trillion"))
											{
												me.Set(typeof(CoreAnnotations.AnswerAnnotation), "NUMBER");
											}
										}
									}
									else
									{
										if (me.GetString<CoreAnnotations.PartOfSpeechAnnotation>() != null && (me.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("NN") || me.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("NNS")))
										{
											if (CurrencyWordPattern.Matcher(me.Word()).Matches())
											{
												if (prev.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("CD") && prev.GetString<CoreAnnotations.AnswerAnnotation>().Equals("MONEY"))
												{
													me.Set(typeof(CoreAnnotations.AnswerAnnotation), "MONEY");
												}
											}
											else
											{
												if (me.Word().Equals("m") || me.Word().Equals("b"))
												{
													// could be metres, but it's probably million or billion in our
													// applications
													if (prev.GetString<CoreAnnotations.AnswerAnnotation>().Equals("MONEY"))
													{
														me.Set(typeof(CoreAnnotations.AnswerAnnotation), "MONEY");
													}
													else
													{
														me.Set(typeof(CoreAnnotations.AnswerAnnotation), "NUMBER");
													}
												}
												else
												{
													if (OrdinalPattern.Matcher(me.Word()).Matches())
													{
														if ((next.Word() != null && MonthPattern.Matcher(next.Word()).Matches()) || (next.Word() != null && Sharpen.Runtime.EqualsIgnoreCase(next.Word(), "of") && next2.Word() != null && MonthPattern.Matcher(next2.Word()).Matches()))
														{
															me.Set(typeof(CoreAnnotations.AnswerAnnotation), "DATE");
														}
													}
													else
													{
														if (GenericTimeWords.Matcher(me.Word()).Matches())
														{
															me.Set(typeof(CoreAnnotations.AnswerAnnotation), "TIME");
														}
													}
												}
											}
										}
										else
										{
											if (me.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("JJ"))
											{
												if ((next.Word() != null && MonthPattern.Matcher(next.Word()).Matches()) || next.Word() != null && Sharpen.Runtime.EqualsIgnoreCase(next.Word(), "of") && next2.Word() != null && MonthPattern.Matcher(next2.Word()).Matches())
												{
													me.Set(typeof(CoreAnnotations.AnswerAnnotation), "DATE");
												}
												else
												{
													if (OrdinalPattern.Matcher(me.Word()).Matches())
													{
														// don't do other tags: don't want 'second' as noun, or 'first' as adverb
														// introducing reasons
														me.Set(typeof(CoreAnnotations.AnswerAnnotation), "ORDINAL");
													}
												}
											}
											else
											{
												if (me.GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("IN") && Sharpen.Runtime.EqualsIgnoreCase(me.Word(), "of"))
												{
													if (prev.Get(typeof(CoreAnnotations.TextAnnotation)) != null && OrdinalPattern.Matcher(prev.Get(typeof(CoreAnnotations.TextAnnotation))).Matches() && next.Get(typeof(CoreAnnotations.TextAnnotation)) != null && MonthPattern.Matcher(next.Get(typeof(
														CoreAnnotations.TextAnnotation))).Matches())
													{
														me.Set(typeof(CoreAnnotations.AnswerAnnotation), "DATE");
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return document;
		}

		/// <summary>
		/// Look for a distance of up to 3 for something that indicates weight not
		/// money.
		/// </summary>
		/// <param name="pl">The list of CoreLabel</param>
		/// <param name="i">The position to scan right from</param>
		/// <returns>whether a weight word is found</returns>
		private static bool LeftScanFindsWeightWord(IList<CoreLabel> pl, int i)
		{
			for (int j = i - 1; j >= 0 && j >= i - 3; j--)
			{
				CoreLabel fl = pl[j];
				if (fl.Word().StartsWith("weigh"))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Look along CD words and see if next thing is a money word
		/// like cents or pounds.
		/// </summary>
		/// <param name="pl">The list of CoreLabel</param>
		/// <param name="i">The position to scan right from</param>
		/// <returns>Whether a money word is found</returns>
		private static bool RightScanFindsMoneyWord(IList<CoreLabel> pl, int i)
		{
			int j = i;
			int sz = pl.Count;
			while (j < sz && pl[j].GetString<CoreAnnotations.PartOfSpeechAnnotation>().Equals("CD"))
			{
				j++;
			}
			if (j >= sz)
			{
				return false;
			}
			string tag = pl[j].GetString<CoreAnnotations.PartOfSpeechAnnotation>();
			string word = pl[j].Word();
			return (tag.Equals("NN") || tag.Equals("NNS")) && CurrencyWordPattern.Matcher(word).Matches();
		}

		// Implement other methods of AbstractSequenceClassifier interface
		public override void Train(ICollection<IList<CoreLabel>> docs, IDocumentReaderAndWriter<CoreLabel> readerAndWriter)
		{
		}

		public override void SerializeClassifier(string serializePath)
		{
			log.Info("Serializing classifier to " + serializePath + "...");
			log.Info("done.");
		}

		public override void SerializeClassifier(ObjectOutputStream oos)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public override void LoadClassifier(ObjectInputStream @in, Properties props)
		{
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			Edu.Stanford.Nlp.IE.Regexp.NumberSequenceClassifier nsc = new Edu.Stanford.Nlp.IE.Regexp.NumberSequenceClassifier(props, true, props);
			string trainFile = nsc.flags.trainFile;
			string testFile = nsc.flags.testFile;
			string textFile = nsc.flags.textFile;
			string loadPath = nsc.flags.loadClassifier;
			string serializeTo = nsc.flags.serializeTo;
			if (loadPath != null)
			{
				nsc.LoadClassifierNoExceptions(loadPath);
				nsc.flags.SetProperties(props);
			}
			else
			{
				if (trainFile != null)
				{
					nsc.Train(trainFile);
				}
			}
			if (serializeTo != null)
			{
				nsc.SerializeClassifier(serializeTo);
			}
			if (testFile != null)
			{
				nsc.ClassifyAndWriteAnswers(testFile, nsc.MakeReaderAndWriter(), true);
			}
			if (textFile != null)
			{
				IDocumentReaderAndWriter<CoreLabel> readerAndWriter = new PlainTextDocumentReaderAndWriter<CoreLabel>();
				nsc.ClassifyAndWriteAnswers(textFile, readerAndWriter, false);
			}
		}
		// end main
	}
}
