using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International.Arabic;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Process
{
	/// <summary>
	/// A class for converting strings to input suitable for processing by
	/// an IOB sequence model.
	/// </summary>
	/// <author>Spence Green</author>
	/// <author>Will Monroe</author>
	public class IOBUtils
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Process.IOBUtils));

		private enum TokenType
		{
			BeginMarker,
			EndMarker,
			BothMarker,
			NoMarker
		}

		public const string BeginSymbol = "BEGIN";

		public const string ContinuationSymbol = "CONT";

		public const string NosegSymbol = "NOSEG";

		public const string RewriteSymbol = "REW";

		[System.ObsoleteAttribute(@"use RewriteSymbol instead")]
		public const string RewriteTahSymbol = "REWTA";

		[System.ObsoleteAttribute(@"use RewriteSymbol instead")]
		public const string RewriteTareefSymbol = "REWAL";

		private const string BoundarySymbol = ".##.";

		private const string BoundaryChar = ".#.";

		private static readonly Pattern isPunc = Pattern.Compile("\\p{Punct}+");

		private static readonly Pattern isDigit = Pattern.Compile("\\p{Digit}+");

		private static readonly Pattern notUnicodeArabic = Pattern.Compile("\\P{InArabic}+");

		private static readonly ICollection<string> arPrefixSet;

		private static readonly ICollection<string> arSuffixSet;

		static IOBUtils()
		{
			// Training token types.
			// Label inventory
			// Patterns for tokens that should not be segmented.
			// Sets of known clitics for tagging when reconstructing the segmented sequences.
			string arabicPrefixString = "ل ف و م ما ح حا ه ها ك ب س";
			arPrefixSet = Java.Util.Collections.UnmodifiableSet(Generics.NewHashSet(Arrays.AsList(arabicPrefixString.Split("\\s+"))));
			string arabicSuffixString = "ل و ما ه ها هم هن نا كم تن تم ى ي هما ك ب ش";
			arSuffixSet = Java.Util.Collections.UnmodifiableSet(Generics.NewHashSet(Arrays.AsList(arabicSuffixString.Split("\\s+"))));
		}

		private IOBUtils()
		{
		}

		// Only static methods
		public static string GetBoundaryCharacter()
		{
			return BoundaryChar;
		}

		/// <summary>
		/// Convert a String to a list of characters suitable for labeling in an IOB
		/// segmentation model.
		/// </summary>
		/// <param name="tokenList"/>
		/// <param name="segMarker"/>
		/// <param name="applyRewriteRules">add rewrite labels (for training data)</param>
		public static IList<CoreLabel> StringToIOB(IList<CoreLabel> tokenList, char segMarker, bool applyRewriteRules)
		{
			return StringToIOB(tokenList, segMarker, applyRewriteRules, false, null, null);
		}

		/// <summary>
		/// Convert a String to a list of characters suitable for labeling in an IOB
		/// segmentation model.
		/// </summary>
		/// <param name="tokenList"/>
		/// <param name="segMarker"/>
		/// <param name="applyRewriteRules">add rewrite labels (for training data)</param>
		/// <param name="tf">a TokenizerFactory returning ArabicTokenizers (for determining original segment boundaries)</param>
		/// <param name="origText">the original string before tokenization (for determining original segment boundaries)</param>
		public static IList<CoreLabel> StringToIOB(IList<CoreLabel> tokenList, char segMarker, bool applyRewriteRules, ITokenizerFactory<CoreLabel> tf, string origText)
		{
			return StringToIOB(tokenList, segMarker, applyRewriteRules, false, tf, origText);
		}

		/// <summary>
		/// Convert a String to a list of characters suitable for labeling in an IOB
		/// segmentation model.
		/// </summary>
		/// <param name="tokenList"/>
		/// <param name="segMarker"/>
		/// <param name="applyRewriteRules">add rewrite labels (for training data)</param>
		/// <param name="stripRewrites">
		/// revert training data to old Green and DeNero model (remove
		/// rewrite labels but still rewrite to try to preserve raw text)
		/// </param>
		public static IList<CoreLabel> StringToIOB(IList<CoreLabel> tokenList, char segMarker, bool applyRewriteRules, bool stripRewrites)
		{
			return StringToIOB(tokenList, segMarker, applyRewriteRules, stripRewrites, null, null);
		}

		/// <summary>
		/// Convert a String to a list of characters suitable for labeling in an IOB
		/// segmentation model.
		/// </summary>
		/// <param name="tokenList"/>
		/// <param name="segMarker"/>
		/// <param name="applyRewriteRules">add rewrite labels (for training data)</param>
		/// <param name="stripRewrites">
		/// revert training data to old Green and DeNero model (remove
		/// rewrite labels but still rewrite to try to preserve raw text)
		/// </param>
		/// <param name="tf">a TokenizerFactory returning ArabicTokenizers (for determining original segment boundaries)</param>
		/// <param name="origText">the original string before tokenization (for determining original segment boundaries)</param>
		public static IList<CoreLabel> StringToIOB(IList<CoreLabel> tokenList, char segMarker, bool applyRewriteRules, bool stripRewrites, ITokenizerFactory<CoreLabel> tf, string origText)
		{
			IList<CoreLabel> iobList = new List<CoreLabel>(tokenList.Count * 7 + tokenList.Count);
			string strSegMarker = segMarker.ToString();
			bool addWhitespace = false;
			int numTokens = tokenList.Count;
			string lastToken = string.Empty;
			string currentWord = string.Empty;
			int wordStartIndex = 0;
			foreach (CoreLabel cl in tokenList)
			{
				// What type of token is this
				if (addWhitespace)
				{
					FillInWordStatistics(iobList, currentWord, wordStartIndex);
					currentWord = string.Empty;
					wordStartIndex = iobList.Count + 1;
					iobList.Add(CreateDatum(cl, BoundaryChar, BoundarySymbol));
					CoreLabel boundaryDatum = iobList[iobList.Count - 1];
					boundaryDatum.SetIndex(0);
					boundaryDatum.SetWord(string.Empty);
					addWhitespace = false;
				}
				string token = cl.Word();
				IOBUtils.TokenType tokType = GetTokenType(token, strSegMarker);
				token = StripSegmentationMarkers(token, tokType);
				System.Diagnostics.Debug.Assert(token.Length != 0);
				if (ShouldNotSegment(token))
				{
					iobList.Add(CreateDatum(cl, token, NosegSymbol));
					addWhitespace = true;
				}
				else
				{
					// Iterate over the characters in the token
					TokenToDatums(iobList, cl, token, tokType, cl, lastToken, applyRewriteRules, stripRewrites, tf, origText);
					addWhitespace = (tokType == IOBUtils.TokenType.BeginMarker || tokType == IOBUtils.TokenType.NoMarker);
				}
				currentWord += token;
				lastToken = token;
			}
			FillInWordStatistics(iobList, currentWord, wordStartIndex);
			return iobList;
		}

		/// <summary>
		/// Loops back through all the datums inserted for the most recent word
		/// and inserts statistics about the word they are a part of.
		/// </summary>
		/// <remarks>
		/// Loops back through all the datums inserted for the most recent word
		/// and inserts statistics about the word they are a part of. This needs to
		/// be post hoc because the CoreLabel lists coming from testing data sets
		/// are pre-segmented (so treating each of those CoreLabels as a "word" lets
		/// us cheat and get 100% classification accuracy by just looking at whether
		/// we're at the beginning of a "word").
		/// </remarks>
		/// <param name="iobList"/>
		/// <param name="currentWord"/>
		/// <param name="wordStartIndex"/>
		private static void FillInWordStatistics(IList<CoreLabel> iobList, string currentWord, int wordStartIndex)
		{
			for (int j = wordStartIndex; j < iobList.Count; j++)
			{
				CoreLabel tok = iobList[j];
				tok.SetIndex(j - wordStartIndex);
				tok.SetWord(currentWord);
			}
		}

		/// <summary>Convert token to a sequence of datums and add to iobList.</summary>
		/// <param name="iobList"/>
		/// <param name="token"/>
		/// <param name="tokType"/>
		/// <param name="tokenLabel"/>
		/// <param name="lastToken"/>
		/// <param name="applyRewriteRules"/>
		/// <param name="tf">a TokenizerFactory returning ArabicTokenizers (for determining original segment boundaries)</param>
		/// <param name="origText">the original string before tokenization (for determining original segment boundaries)</param>
		private static void TokenToDatums(IList<CoreLabel> iobList, CoreLabel cl, string token, IOBUtils.TokenType tokType, CoreLabel tokenLabel, string lastToken, bool applyRewriteRules, bool stripRewrites, ITokenizerFactory<CoreLabel> tf, string origText
			)
		{
			if (token.IsEmpty())
			{
				return;
			}
			string lastLabel = ContinuationSymbol;
			string firstLabel = BeginSymbol;
			string rewritten = cl.Get(typeof(ArabicDocumentReaderAndWriter.RewrittenArabicAnnotation));
			bool crossRefRewrites = true;
			if (rewritten == null)
			{
				rewritten = token;
				crossRefRewrites = false;
			}
			else
			{
				rewritten = StripSegmentationMarkers(rewritten, tokType);
			}
			if (applyRewriteRules)
			{
				// Apply Arabic-specific re-write rules
				string rawToken = tokenLabel.Word();
				string tag = tokenLabel.Tag();
				MorphoFeatureSpecification featureSpec = new ArabicMorphoFeatureSpecification();
				featureSpec.Activate(MorphoFeatureSpecification.MorphoFeatureType.Ngen);
				featureSpec.Activate(MorphoFeatureSpecification.MorphoFeatureType.Nnum);
				featureSpec.Activate(MorphoFeatureSpecification.MorphoFeatureType.Def);
				featureSpec.Activate(MorphoFeatureSpecification.MorphoFeatureType.Tense);
				MorphoFeatures features = featureSpec.StrToFeatures(tag);
				// Rule #1 : ت --> ة
				if (features.GetValue(MorphoFeatureSpecification.MorphoFeatureType.Ngen).Equals("F") && features.GetValue(MorphoFeatureSpecification.MorphoFeatureType.Nnum).Equals("SG") && rawToken.EndsWith("ت-") && !stripRewrites)
				{
					lastLabel = RewriteSymbol;
				}
				else
				{
					if (rawToken.EndsWith("ة-"))
					{
						System.Diagnostics.Debug.Assert(token.EndsWith("ة"));
						token = Sharpen.Runtime.Substring(token, 0, token.Length - 1) + "ت";
						lastLabel = RewriteSymbol;
					}
				}
				// Rule #2 : لل --> ل ال
				if (lastToken.Equals("ل") && features.GetValue(MorphoFeatureSpecification.MorphoFeatureType.Def).Equals("D"))
				{
					if (rawToken.StartsWith("-ال"))
					{
						if (!token.StartsWith("ا"))
						{
							log.Info("Bad REWAL: " + rawToken + " / " + token);
						}
						token = Sharpen.Runtime.Substring(token, 1);
						rewritten = Sharpen.Runtime.Substring(rewritten, 1);
						if (!stripRewrites)
						{
							firstLabel = RewriteSymbol;
						}
					}
					else
					{
						if (rawToken.StartsWith("-ل"))
						{
							if (!token.StartsWith("ل"))
							{
								log.Info("Bad REWAL: " + rawToken + " / " + token);
							}
							if (!stripRewrites)
							{
								firstLabel = RewriteSymbol;
							}
						}
						else
						{
							log.Info("Ignoring REWAL: " + rawToken + " / " + token);
						}
					}
				}
				// Rule #3 : ي --> ى
				// Rule #4 : ا --> ى
				if (rawToken.EndsWith("ى-"))
				{
					if (features.GetValue(MorphoFeatureSpecification.MorphoFeatureType.Tense) != null)
					{
						// verb: ى becomes ا
						token = Sharpen.Runtime.Substring(token, 0, token.Length - 1) + "ا";
					}
					else
					{
						// assume preposition:
						token = Sharpen.Runtime.Substring(token, 0, token.Length - 1) + "ي";
					}
					if (!stripRewrites)
					{
						lastLabel = RewriteSymbol;
					}
				}
				else
				{
					if (rawToken.Equals("علي-") || rawToken.Equals("-علي-"))
					{
						if (!stripRewrites)
						{
							lastLabel = RewriteSymbol;
						}
					}
				}
			}
			string origWord;
			if (origText == null)
			{
				origWord = tokenLabel.Word();
			}
			else
			{
				origWord = Sharpen.Runtime.Substring(origText, cl.BeginPosition(), cl.EndPosition());
			}
			int origIndex = 0;
			while (origIndex < origWord.Length && IsDeletedCharacter(origWord[origIndex], tf))
			{
				++origIndex;
			}
			// Create datums and add to iobList
			if (token.IsEmpty())
			{
				log.Info("Rewriting resulted in empty token: " + tokenLabel.Word());
			}
			string firstChar = token[0].ToString();
			// Start at 0 to make sure we include the whole token according to the tokenizer
			iobList.Add(CreateDatum(cl, firstChar, firstLabel, 0, origIndex + 1));
			int numChars = token.Length;
			if (crossRefRewrites && rewritten.Length != numChars)
			{
				System.Console.Error.Printf("Rewritten annotation doesn't have correct length: %s>>>%s%n", token, rewritten);
				crossRefRewrites = false;
			}
			++origIndex;
			for (int j = 1; j < numChars; ++j, ++origIndex)
			{
				while (origIndex < origWord.Length && IsDeletedCharacter(origWord[origIndex], tf))
				{
					++origIndex;
				}
				if (origIndex >= origWord.Length)
				{
					origIndex = origWord.Length - 1;
				}
				string charLabel = (j == numChars - 1) ? lastLabel : ContinuationSymbol;
				string thisChar = token[j].ToString();
				if (crossRefRewrites && !rewritten[j].ToString().Equals(thisChar))
				{
					charLabel = RewriteSymbol;
				}
				if (charLabel == ContinuationSymbol && thisChar.Equals("ى") && j != numChars - 1)
				{
					charLabel = RewriteSymbol;
				}
				// Assume all mid-word alef maqsura are supposed to be yah
				iobList.Add(CreateDatum(cl, thisChar, charLabel, origIndex, origIndex + 1));
			}
			// End at endPosition to make sure we include the whole token according to the tokenizer
			if (!iobList.IsEmpty())
			{
				iobList[iobList.Count - 1].SetEndPosition(cl.EndPosition());
			}
		}

		private static bool IsDeletedCharacter(char ch, ITokenizerFactory<CoreLabel> tf)
		{
			IList<CoreLabel> tokens = tf.GetTokenizer(new StringReader(char.ToString(ch))).Tokenize();
			return tokens.IsEmpty();
		}

		/// <summary>Identify tokens that should not be segmented.</summary>
		private static bool ShouldNotSegment(string token)
		{
			return (isDigit.Matcher(token).Find() || isPunc.Matcher(token).Find() || notUnicodeArabic.Matcher(token).Find());
		}

		/// <summary>Strip segmentation markers.</summary>
		private static string StripSegmentationMarkers(string tok, IOBUtils.TokenType tokType)
		{
			int beginOffset = (tokType == IOBUtils.TokenType.BeginMarker || tokType == IOBUtils.TokenType.BothMarker) ? 1 : 0;
			int endOffset = (tokType == IOBUtils.TokenType.EndMarker || tokType == IOBUtils.TokenType.BothMarker) ? tok.Length - 1 : tok.Length;
			return tokType == IOBUtils.TokenType.NoMarker ? tok : Sharpen.Runtime.Substring(tok, beginOffset, endOffset);
		}

		private static CoreLabel CreateDatum(CoreLabel cl, string token, string label)
		{
			int endOffset = cl.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)) - cl.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			return CreateDatum(cl, token, label, 0, endOffset);
		}

		/// <summary>Create a datum from a string.</summary>
		/// <remarks>
		/// Create a datum from a string. The CoreAnnotations must correspond to those used by
		/// SequenceClassifier. The following annotations are copied from the provided
		/// CoreLabel cl, if present:
		/// DomainAnnotation
		/// startOffset and endOffset will be added to the
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.CharacterOffsetBeginAnnotation"/>
		/// of
		/// the
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// cl to give the
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.CharacterOffsetBeginAnnotation"/>
		/// and
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.CharacterOffsetEndAnnotation"/>
		/// of the resulting datum.
		/// </remarks>
		private static CoreLabel CreateDatum(CoreLabel cl, string token, string label, int startOffset, int endOffset)
		{
			CoreLabel newTok = new CoreLabel();
			newTok.Set(typeof(CoreAnnotations.TextAnnotation), token);
			newTok.Set(typeof(CoreAnnotations.CharAnnotation), token);
			newTok.Set(typeof(CoreAnnotations.AnswerAnnotation), label);
			newTok.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), label);
			newTok.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), cl.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) + startOffset);
			newTok.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), cl.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) + endOffset);
			if (cl != null && cl.ContainsKey(typeof(CoreAnnotations.DomainAnnotation)))
			{
				newTok.Set(typeof(CoreAnnotations.DomainAnnotation), cl.Get(typeof(CoreAnnotations.DomainAnnotation)));
			}
			return newTok;
		}

		/// <summary>Deterministically classify a token.</summary>
		private static IOBUtils.TokenType GetTokenType(string token, string segMarker)
		{
			if (segMarker == null || token.Equals(segMarker))
			{
				return IOBUtils.TokenType.NoMarker;
			}
			IOBUtils.TokenType tokType = IOBUtils.TokenType.NoMarker;
			bool startsWithMarker = token.StartsWith(segMarker);
			bool endsWithMarker = token.EndsWith(segMarker);
			if (startsWithMarker && endsWithMarker)
			{
				tokType = IOBUtils.TokenType.BothMarker;
			}
			else
			{
				if (startsWithMarker)
				{
					tokType = IOBUtils.TokenType.BeginMarker;
				}
				else
				{
					if (endsWithMarker)
					{
						tokType = IOBUtils.TokenType.EndMarker;
					}
				}
			}
			return tokType;
		}

		/// <summary>
		/// This version is for turning an unsegmented string to an IOB input, i.e.,
		/// for processing raw text.
		/// </summary>
		public static IList<CoreLabel> StringToIOB(string @string)
		{
			return StringToIOB(@string, null);
		}

		public static IList<CoreLabel> StringToIOB(string str, char segMarker)
		{
			// Whitespace tokenization
			IList<CoreLabel> toks = SentenceUtils.ToCoreLabelList(str.Trim().Split("\\s+"));
			return StringToIOB(toks, segMarker, false);
		}

		/// <summary>Convert a list of labeled characters to a String.</summary>
		/// <remarks>
		/// Convert a list of labeled characters to a String. Include segmentation markers
		/// for prefixes and suffixes in the string, and add a space at segmentations.
		/// </remarks>
		public static string IOBToString(IList<CoreLabel> labeledSequence, string prefixMarker, string suffixMarker)
		{
			return IOBToString(labeledSequence, prefixMarker, suffixMarker, true, true, 0, labeledSequence.Count);
		}

		/// <summary>Convert a list of labeled characters to a String.</summary>
		/// <remarks>
		/// Convert a list of labeled characters to a String. Include segmentation markers
		/// for prefixes and suffixes in the string, and add a space at segmentations.
		/// </remarks>
		public static string IOBToString(IList<CoreLabel> labeledSequence, string prefixMarker, string suffixMarker, int startIndex, int endIndex)
		{
			return IOBToString(labeledSequence, prefixMarker, suffixMarker, true, true, startIndex, endIndex);
		}

		/// <summary>Convert a list of labeled characters to a String.</summary>
		/// <remarks>
		/// Convert a list of labeled characters to a String. Include segmentation markers
		/// (but no spaces) at segmentation boundaries.
		/// </remarks>
		public static string IOBToString(IList<CoreLabel> labeledSequence, string segmentationMarker)
		{
			return IOBToString(labeledSequence, segmentationMarker, null, false, true, 0, labeledSequence.Count);
		}

		/// <summary>Convert a list of labeled characters to a String.</summary>
		/// <remarks>Convert a list of labeled characters to a String. Preserve the original (unsegmented) text.</remarks>
		public static string IOBToString(IList<CoreLabel> labeledSequence)
		{
			return IOBToString(labeledSequence, null, null, false, false, 0, labeledSequence.Count);
		}

		private static string IOBToString(IList<CoreLabel> labeledSequence, string prefixMarker, string suffixMarker, bool addSpace, bool applyRewrites, int startIndex, int endIndex)
		{
			StringBuilder sb = new StringBuilder();
			string lastLabel = string.Empty;
			bool addPrefixMarker = prefixMarker != null && prefixMarker.Length > 0;
			bool addSuffixMarker = suffixMarker != null && suffixMarker.Length > 0;
			if (addPrefixMarker || addSuffixMarker)
			{
				AnnotateMarkers(labeledSequence);
			}
			for (int i = startIndex; i < endIndex; ++i)
			{
				CoreLabel labeledChar = labeledSequence[i];
				string token = labeledChar.Get(typeof(CoreAnnotations.CharAnnotation));
				if (addPrefixMarker && token.Equals(prefixMarker))
				{
					token = "#pm#";
				}
				if (addSuffixMarker && token.Equals(suffixMarker))
				{
					token = "#sm#";
				}
				string label = labeledChar.Get(typeof(CoreAnnotations.AnswerAnnotation));
				if (token.Equals(BoundaryChar))
				{
					sb.Append(" ");
				}
				else
				{
					if (label.Equals(BeginSymbol))
					{
						if (lastLabel.Equals(ContinuationSymbol) || lastLabel.Equals(BeginSymbol) || lastLabel.Equals(RewriteSymbol))
						{
							if (addPrefixMarker && (!addSpace || AddPrefixMarker(i, labeledSequence)))
							{
								sb.Append(prefixMarker);
							}
							if (addSpace)
							{
								sb.Append(" ");
							}
							if (addSuffixMarker && (!addSpace || AddSuffixMarker(i, labeledSequence)))
							{
								sb.Append(suffixMarker);
							}
						}
						sb.Append(token);
					}
					else
					{
						if (label.Equals(ContinuationSymbol) || label.Equals(BoundarySymbol))
						{
							sb.Append(token);
						}
						else
						{
							if (label.Equals(NosegSymbol))
							{
								if (!lastLabel.Equals(BoundarySymbol) && addSpace)
								{
									sb.Append(" ");
								}
								sb.Append(token);
							}
							else
							{
								if (label.Equals(RewriteSymbol) || label.Equals("REWAL") || label.Equals("REWTA"))
								{
									switch (token)
									{
										case "ت":
										case "ه":
										{
											sb.Append(applyRewrites ? "ة" : token);
											break;
										}

										case "ل":
										{
											sb.Append((addPrefixMarker ? prefixMarker : string.Empty) + (addSpace ? " " : string.Empty) + (applyRewrites ? "ال" : "ل"));
											break;
										}

										case "ي":
										case "ا":
										{
											sb.Append(applyRewrites ? "ى" : token);
											break;
										}

										case "ى":
										{
											sb.Append(applyRewrites ? "ي" : token);
											break;
										}

										default:
										{
											// Nonsense rewrite predicted by the classifier--just assume CONT
											sb.Append(token);
											break;
										}
									}
								}
								else
								{
									throw new Exception("Unknown label: " + label);
								}
							}
						}
					}
				}
				lastLabel = label;
			}
			return sb.ToString().Trim();
		}

		private class PrefixMarkerAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		private class SuffixMarkerAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		private static void AnnotateMarkers(IList<CoreLabel> labeledSequence)
		{
			StringBuilder segment = new StringBuilder();
			IList<string> segments = CollectionUtils.MakeList();
			int wordBegin = 0;
			for (int i = 0; i < labeledSequence.Count; i++)
			{
				string token = labeledSequence[i].Get(typeof(CoreAnnotations.CharAnnotation));
				string label = labeledSequence[i].Get(typeof(CoreAnnotations.AnswerAnnotation));
				switch (label)
				{
					case BeginSymbol:
					{
						if (i != wordBegin)
						{
							segments.Add(segment.ToString());
							segment.Length = 0;
						}
						segment.Append(token);
						break;
					}

					case BoundarySymbol:
					{
						segments.Add(segment.ToString());
						segment.Length = 0;
						AnnotateMarkersOnWord(labeledSequence, wordBegin, i, segments);
						segments.Clear();
						wordBegin = i + 1;
						break;
					}

					default:
					{
						segment.Append(token);
						break;
					}
				}
			}
			segments.Add(segment.ToString());
			AnnotateMarkersOnWord(labeledSequence, wordBegin, labeledSequence.Count, segments);
		}

		private static void AnnotateMarkersOnWord(IList<CoreLabel> labeledSequence, int wordBegin, int wordEnd, IList<string> segments)
		{
			Pair<int, int> headBounds = GetHeadBounds(segments);
			int currentIndex = 0;
			for (int i = wordBegin; i < wordEnd; i++)
			{
				string label = labeledSequence[i].Get(typeof(CoreAnnotations.AnswerAnnotation));
				labeledSequence[i].Set(typeof(IOBUtils.PrefixMarkerAnnotation), false);
				labeledSequence[i].Set(typeof(IOBUtils.SuffixMarkerAnnotation), false);
				if (label.Equals(BeginSymbol))
				{
					// Add prefix markers for BEGIN characters up to and including the start of the head
					// (but don't add prefix markers if there aren't any prefixes)
					if (currentIndex <= headBounds.first && currentIndex != 0)
					{
						labeledSequence[i].Set(typeof(IOBUtils.PrefixMarkerAnnotation), true);
					}
					// Add suffix markers for BEGIN characters starting one past the end of the head
					// (headBounds.second is one past the end, no need to add one)
					if (currentIndex >= headBounds.second)
					{
						labeledSequence[i].Set(typeof(IOBUtils.SuffixMarkerAnnotation), true);
					}
					currentIndex++;
				}
			}
		}

		private static Pair<int, int> GetHeadBounds(IList<string> segments)
		{
			int NotFound = -1;
			int potentialSuffix = segments.Count - 1;
			int nonSuffix = NotFound;
			int potentialPrefix = 0;
			int nonPrefix = NotFound;
			// Heuristic algorithm for finding the head of a segmented word:
			while (true)
			{
				/* Alternate considering suffixes and prefixes (starting with suffix).
				*
				* If the current segment is a known Arabic {suffix|prefix}, mark it as
				* such. Otherwise, stop considering tokens from that direction.
				*/
				if (nonSuffix == NotFound)
				{
					if (arSuffixSet.Contains(segments[potentialSuffix]))
					{
						potentialSuffix--;
					}
					else
					{
						nonSuffix = potentialSuffix;
					}
				}
				if (potentialSuffix < potentialPrefix)
				{
					break;
				}
				if (nonPrefix == NotFound)
				{
					if (arPrefixSet.Contains(segments[potentialPrefix]))
					{
						potentialPrefix++;
					}
					else
					{
						nonPrefix = potentialPrefix;
					}
				}
				if (potentialSuffix < potentialPrefix || (nonSuffix != NotFound && nonPrefix != NotFound))
				{
					break;
				}
			}
			/* Once we have exhausted all known prefixes and suffixes, take the longest
			* segment that remains to be the head. Break length ties by picking the first one.
			*
			* Note that in some cases, no segments will remain (e.g. b# +y), so a
			* segmented word may have zero or one heads, but never more than one.
			*/
			if (potentialSuffix < potentialPrefix)
			{
				// no head--start and end are index of first suffix
				if (potentialSuffix + 1 != potentialPrefix)
				{
					throw new Exception("Suffix pointer moved too far!");
				}
				return Pair.MakePair(potentialSuffix + 1, potentialSuffix + 1);
			}
			else
			{
				int headIndex = nonPrefix;
				for (int i = nonPrefix + 1; i <= nonSuffix; i++)
				{
					if (segments[i].Length > segments[headIndex].Length)
					{
						headIndex = i;
					}
				}
				return Pair.MakePair(headIndex, headIndex + 1);
			}
		}

		private static bool AddPrefixMarker(int focus, IList<CoreLabel> labeledSequence)
		{
			return labeledSequence[focus].Get(typeof(IOBUtils.PrefixMarkerAnnotation));
		}

		private static bool AddSuffixMarker(int focus, IList<CoreLabel> labeledSequence)
		{
			return labeledSequence[focus].Get(typeof(IOBUtils.SuffixMarkerAnnotation));
		}

		public static void LabelDomain(IList<CoreLabel> tokenList, string domain)
		{
			foreach (CoreLabel cl in tokenList)
			{
				cl.Set(typeof(CoreAnnotations.DomainAnnotation), domain);
			}
		}

		public static IList<IntPair> TokenSpansForIOB(IList<CoreLabel> labeledSequence)
		{
			IList<IntPair> spans = CollectionUtils.MakeList();
			string lastLabel = string.Empty;
			bool inToken = false;
			int tokenStart = 0;
			int sequenceLength = labeledSequence.Count;
			for (int i = 0; i < sequenceLength; ++i)
			{
				CoreLabel labeledChar = labeledSequence[i];
				string token = labeledChar.Get(typeof(CoreAnnotations.CharAnnotation));
				string label = labeledChar.Get(typeof(CoreAnnotations.AnswerAnnotation));
				if (token.Equals(BoundaryChar))
				{
					if (inToken)
					{
						spans.Add(new IntPair(tokenStart, i));
					}
					inToken = false;
				}
				else
				{
					switch (label)
					{
						case BeginSymbol:
						{
							if (lastLabel.Equals(ContinuationSymbol) || lastLabel.Equals(BeginSymbol) || lastLabel.Equals(RewriteSymbol))
							{
								if (inToken)
								{
									spans.Add(new IntPair(tokenStart, i));
								}
								inToken = true;
								tokenStart = i;
							}
							else
							{
								if (!inToken)
								{
									inToken = true;
									tokenStart = i;
								}
							}
							break;
						}

						case ContinuationSymbol:
						{
							if (!inToken)
							{
								inToken = true;
								tokenStart = i;
							}
							break;
						}

						case BoundarySymbol:
						case NosegSymbol:
						{
							if (inToken)
							{
								spans.Add(new IntPair(tokenStart, i));
							}
							inToken = true;
							tokenStart = i;
							break;
						}

						case RewriteSymbol:
						case "REWAL":
						case "REWTA":
						{
							if (token.Equals("ل"))
							{
								if (inToken)
								{
									spans.Add(new IntPair(tokenStart, i));
								}
								inToken = true;
								tokenStart = i;
							}
							else
							{
								if (!inToken)
								{
									inToken = true;
									tokenStart = i;
								}
							}
							break;
						}
					}
				}
				lastLabel = label;
			}
			if (inToken)
			{
				spans.Add(new IntPair(tokenStart, sequenceLength));
			}
			return spans;
		}
	}
}
