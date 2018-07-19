using System;
using Edu.Stanford.Nlp.International.Spanish;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	[System.Serializable]
	public class SpanishUnknownWordModel : BaseUnknownWordModel
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.SpanishUnknownWordModel));

		protected internal readonly bool smartMutation;

		protected internal readonly int unknownSuffixSize;

		protected internal readonly int unknownPrefixSize;

		public SpanishUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, ClassicCounter<IntTaggedWord> unSeenCounter)
			: base(op, lex, wordIndex, tagIndex, unSeenCounter, null, null, null)
		{
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
		public SpanishUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex)
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

		/// <summary>TODO Can add various signatures, setting the signature via Options.</summary>
		/// <param name="word">The word to make a signature for</param>
		/// <param name="loc">
		/// Its position in the sentence (mainly so sentence-initial
		/// capitalized words can be treated differently)
		/// </param>
		/// <returns>A String that is its signature (equivalence class)</returns>
		public override string GetSignature(string word, int loc)
		{
			string BaseLabel = "UNK";
			StringBuilder sb = new StringBuilder(BaseLabel);
			switch (unknownLevel)
			{
				case 1:
				{
					if (StringUtils.IsNumeric(word))
					{
						sb.Append('#');
						break;
					}
					else
					{
						if (StringUtils.IsPunct(word))
						{
							sb.Append('!');
							break;
						}
					}
					// Mutually exclusive patterns
					sb.Append(SpanishUnknownWordSignatures.ConditionalSuffix(word));
					sb.Append(SpanishUnknownWordSignatures.ImperfectSuffix(word));
					sb.Append(SpanishUnknownWordSignatures.InfinitiveSuffix(word));
					sb.Append(SpanishUnknownWordSignatures.AdverbSuffix(word));
					// Broad coverage patterns -- only apply if we haven't yet matched at all
					if (sb.ToString().Equals(BaseLabel))
					{
						if (SpanishUnknownWordSignatures.HasVerbFirstPersonPluralSuffix(word))
						{
							sb.Append("-vb1p");
						}
						else
						{
							if (SpanishUnknownWordSignatures.HasGerundSuffix(word))
							{
								sb.Append("-ger");
							}
							else
							{
								if (word.EndsWith("s"))
								{
									sb.Append("-s");
								}
							}
						}
					}
					// Backoff to suffix if we haven't matched anything else
					if (unknownSuffixSize > 0 && sb.ToString().Equals(BaseLabel))
					{
						int min = word.Length < unknownSuffixSize ? word.Length : unknownSuffixSize;
						sb.Append('-').Append(Sharpen.Runtime.Substring(word, word.Length - min));
					}
					char first = word[0];
					if ((char.IsUpperCase(first) || char.IsTitleCase(first)) && !IsUpperCase(word))
					{
						sb.Append("-C");
					}
					else
					{
						sb.Append("-c");
					}
					break;
				}

				default:
				{
					log.Error(string.Format("%s: Invalid unknown word signature! (%d)%n", this.GetType().FullName, unknownLevel));
					break;
				}
			}
			return sb.ToString();
		}

		private static bool IsUpperCase(string s)
		{
			for (int i = 0; i < s.Length; i++)
			{
				if (char.IsLowerCase(s[i]))
				{
					return false;
				}
			}
			return true;
		}

		private const long serialVersionUID = 5370429530690606644L;
	}
}
