using System;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>This is a basic unknown word model for Arabic.</summary>
	/// <remarks>
	/// This is a basic unknown word model for Arabic.  It supports 4 different
	/// types of feature modeling; see
	/// <see cref="GetSignature(string, int)"/>
	/// .
	/// <i>Implementation note: the contents of this class tend to overlap somewhat
	/// with
	/// <see cref="EnglishUnknownWordModel"/>
	/// and were originally included in
	/// <see cref="BaseLexicon"/>
	/// .
	/// </remarks>
	/// <author>Roger Levy</author>
	/// <author>Christopher Manning</author>
	/// <author>Anna Rafferty</author>
	[System.Serializable]
	public class ArabicUnknownWordModel : BaseUnknownWordModel
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ArabicUnknownWordModel));

		private const long serialVersionUID = 4825624957364628771L;

		private const int MinUnknown = 6;

		private const int MaxUnknown = 10;

		protected internal readonly bool smartMutation;

		protected internal readonly int unknownSuffixSize;

		protected internal readonly int unknownPrefixSize;

		public ArabicUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, ClassicCounter<IntTaggedWord> unSeenCounter)
			: base(op, lex, wordIndex, tagIndex, unSeenCounter, null, null, null)
		{
			if (unknownLevel < MinUnknown || unknownLevel > MaxUnknown)
			{
				throw new ArgumentException("Invalid value for useUnknownWordSignatures: " + unknownLevel);
			}
			this.smartMutation = op.lexOptions.smartMutation;
			this.unknownSuffixSize = op.lexOptions.unknownSuffixSize;
			this.unknownPrefixSize = op.lexOptions.unknownPrefixSize;
		}

		/// <summary>This constructor creates an UWM with empty data structures.</summary>
		/// <remarks>
		/// This constructor creates an UWM with empty data structures.  Only
		/// use if loading in the data separately, such as by reading in text
		/// lines containing the data.
		/// </remarks>
		public ArabicUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: this(op, lex, wordIndex, tagIndex, new ClassicCounter<IntTaggedWord>())
		{
		}

		public override float Score(IntTaggedWord iTW, int loc, double c_Tseen, double total, double smooth, string word)
		{
			double pb_W_T;
			// always set below
			//  unknown word model for P(T|S)
			int wordSig = GetSignatureIndex(iTW.word, loc, word);
			IntTaggedWord temp = new IntTaggedWord(wordSig, iTW.tag);
			double c_TS = unSeenCounter.GetCount(temp);
			temp = new IntTaggedWord(wordSig, nullTag);
			double c_S = unSeenCounter.GetCount(temp);
			double c_U = unSeenCounter.GetCount(NullItw);
			temp = new IntTaggedWord(nullWord, iTW.tag);
			double c_T = unSeenCounter.GetCount(temp);
			double p_T_U = c_T / c_U;
			if (unknownLevel == 0)
			{
				c_TS = 0;
				c_S = 0;
			}
			double pb_T_S = (c_TS + smooth * p_T_U) / (c_S + smooth);
			double p_T = (c_Tseen / total);
			double p_W = 1.0 / total;
			pb_W_T = Math.Log(pb_T_S * p_W / p_T);
			return (float)pb_W_T;
		}

		/// <summary>
		/// Returns the index of the signature of the word numbered wordIndex, where
		/// the signature is the String representation of unknown word features.
		/// </summary>
		public override int GetSignatureIndex(int index, int sentencePosition, string word)
		{
			string uwSig = GetSignature(word, sentencePosition);
			int sig = wordIndex.AddToIndex(uwSig);
			return sig;
		}

		/// <summary>6-9 were added for Arabic.</summary>
		/// <remarks>
		/// 6-9 were added for Arabic. 6 looks for the prefix Al- (and
		/// knows that Buckwalter uses various symbols as letters), while 7 just looks
		/// for numbers and last letter. 8 looks for Al-, looks for several useful
		/// suffixes, and tracks the first letter of the word. (note that the first
		/// letter seems a bit more informative than the last letter, overall.)
		/// 9 tries to build on 8, but avoiding some of its perceived flaws: really it
		/// was using the first AND last letter.
		/// </remarks>
		/// <param name="word">The word to make a signature for</param>
		/// <param name="loc">
		/// Its position in the sentence (mainly so sentence-initial
		/// capitalized words can be treated differently)
		/// </param>
		/// <returns>A String that is its signature (equivalence class)</returns>
		public override string GetSignature(string word, int loc)
		{
			StringBuilder sb = new StringBuilder("UNK");
			switch (unknownLevel)
			{
				case 10:
				{
					//Anna's attempt at improving Chris' attempt, April 2008
					bool allDigitPlus = ArabicUnknownWordSignatures.AllDigitPlus(word);
					int leng = word.Length;
					if (allDigitPlus)
					{
						sb.Append("-NUM");
					}
					else
					{
						if (word.StartsWith("Al") || word.StartsWith("\u0627\u0644"))
						{
							sb.Append("-Al");
						}
						else
						{
							// the first letters of a word seem more informative overall than the
							// last letters.
							// Alternatively we could add on the first two letters, if there's
							// enough data.
							if (unknownPrefixSize > 0)
							{
								int min = leng < unknownPrefixSize ? leng : unknownPrefixSize;
								sb.Append('-').Append(Sharpen.Runtime.Substring(word, 0, min));
							}
						}
					}
					if (word.Length == 1)
					{
						//add in the unicode type for the char
						sb.Append(char.GetType(word[0]));
					}
					sb.Append(ArabicUnknownWordSignatures.LikelyAdjectivalSuffix(word));
					sb.Append(ArabicUnknownWordSignatures.PastTenseVerbNumberSuffix(word));
					sb.Append(ArabicUnknownWordSignatures.PresentTenseVerbNumberSuffix(word));
					string ans = ArabicUnknownWordSignatures.AbstractionNounSuffix(word);
					if (!string.Empty.Equals(ans))
					{
						sb.Append(ans);
					}
					else
					{
						sb.Append(ArabicUnknownWordSignatures.TaaMarbuuTaSuffix(word));
					}
					if (unknownSuffixSize > 0 && !allDigitPlus)
					{
						int min = leng < unknownSuffixSize ? leng : unknownSuffixSize;
						sb.Append('-').Append(Sharpen.Runtime.Substring(word, word.Length - min));
					}
					break;
				}

				case 9:
				{
					// Chris' attempt at improving Roger's Arabic attempt, Nov 2006.
					bool allDigitPlus = ArabicUnknownWordSignatures.AllDigitPlus(word);
					int leng = word.Length;
					if (allDigitPlus)
					{
						sb.Append("-NUM");
					}
					else
					{
						if (word.StartsWith("Al") || word.StartsWith("\u0627\u0644"))
						{
							sb.Append("-Al");
						}
						else
						{
							// the first letters of a word seem more informative overall than the
							// last letters.
							// Alternatively we could add on the first two letters, if there's
							// enough data.
							if (unknownPrefixSize > 0)
							{
								int min = leng < unknownPrefixSize ? leng : unknownPrefixSize;
								sb.Append('-').Append(Sharpen.Runtime.Substring(word, 0, min));
							}
						}
					}
					sb.Append(ArabicUnknownWordSignatures.LikelyAdjectivalSuffix(word));
					sb.Append(ArabicUnknownWordSignatures.PastTenseVerbNumberSuffix(word));
					sb.Append(ArabicUnknownWordSignatures.PresentTenseVerbNumberSuffix(word));
					string ans = ArabicUnknownWordSignatures.AbstractionNounSuffix(word);
					if (!string.Empty.Equals(ans))
					{
						sb.Append(ans);
					}
					else
					{
						sb.Append(ArabicUnknownWordSignatures.TaaMarbuuTaSuffix(word));
					}
					if (unknownSuffixSize > 0 && !allDigitPlus)
					{
						int min = leng < unknownSuffixSize ? leng : unknownSuffixSize;
						sb.Append('-').Append(Sharpen.Runtime.Substring(word, word.Length - min));
					}
					break;
				}

				case 8:
				{
					// Roger's attempt at an Arabic UWM, May 2006.
					if (word.StartsWith("Al"))
					{
						sb.Append("-Al");
					}
					bool allDigitPlus = ArabicUnknownWordSignatures.AllDigitPlus(word);
					if (allDigitPlus)
					{
						sb.Append("-NUM");
					}
					else
					{
						// the first letters of a word seem more informative overall than the
						// last letters.
						// Alternatively we could add on the first two letters, if there's
						// enough data.
						sb.Append('-').Append(word[0]);
					}
					sb.Append(ArabicUnknownWordSignatures.LikelyAdjectivalSuffix(word));
					sb.Append(ArabicUnknownWordSignatures.PastTenseVerbNumberSuffix(word));
					sb.Append(ArabicUnknownWordSignatures.PresentTenseVerbNumberSuffix(word));
					sb.Append(ArabicUnknownWordSignatures.TaaMarbuuTaSuffix(word));
					sb.Append(ArabicUnknownWordSignatures.AbstractionNounSuffix(word));
					break;
				}

				case 7:
				{
					// For Arabic with Al's separated off (cdm, May 2006)
					// { -NUM, -lastChar }
					bool allDigitPlus = ArabicUnknownWordSignatures.AllDigitPlus(word);
					if (allDigitPlus)
					{
						sb.Append("-NUM");
					}
					else
					{
						sb.Append(word[word.Length - 1]);
					}
					break;
				}

				case 6:
				{
					// For Arabic (cdm, May 2006), with Al- as part of word
					// { -Al, 0 } +
					// { -NUM, -last char(s) }
					if (word.StartsWith("Al"))
					{
						sb.Append("-Al");
					}
					bool allDigitPlus = ArabicUnknownWordSignatures.AllDigitPlus(word);
					if (allDigitPlus)
					{
						sb.Append("-NUM");
					}
					else
					{
						sb.Append(word[word.Length - 1]);
					}
					break;
				}

				default:
				{
					break;
				}
			}
			// 0 = do nothing so it just stays as "UNK"
			// end switch (unknownLevel)
			// log.info("Summarized " + word + " to " + sb.toString());
			return sb.ToString();
		}

		// end getSignature()
		public override int GetUnknownLevel()
		{
			return unknownLevel;
		}
	}
}
