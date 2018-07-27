using System;
using Edu.Stanford.Nlp.International.French;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	[System.Serializable]
	public class FrenchUnknownWordModel : BaseUnknownWordModel
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.FrenchUnknownWordModel));

		private const long serialVersionUID = -776564693549194424L;

		protected internal readonly bool smartMutation;

		protected internal readonly int unknownSuffixSize;

		protected internal readonly int unknownPrefixSize;

		public FrenchUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, ClassicCounter<IntTaggedWord> unSeenCounter)
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
		public FrenchUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex)
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
					//Marie's initial attempt
					sb.Append(FrenchUnknownWordSignatures.NounSuffix(word));
					if (sb.ToString().Equals(BaseLabel))
					{
						sb.Append(FrenchUnknownWordSignatures.AdjSuffix(word));
						if (sb.ToString().Equals(BaseLabel))
						{
							sb.Append(FrenchUnknownWordSignatures.VerbSuffix(word));
							if (sb.ToString().Equals(BaseLabel))
							{
								sb.Append(FrenchUnknownWordSignatures.AdvSuffix(word));
							}
						}
					}
					sb.Append(FrenchUnknownWordSignatures.PossiblePlural(word));
					string hasDigit = FrenchUnknownWordSignatures.HasDigit(word);
					string isDigit = FrenchUnknownWordSignatures.IsDigit(word);
					if (!hasDigit.Equals(string.Empty))
					{
						if (isDigit.Equals(string.Empty))
						{
							sb.Append(hasDigit);
						}
						else
						{
							sb.Append(isDigit);
						}
					}
					//        if(FrenchUnknownWordSignatures.isPunc(word).equals(""))
					sb.Append(FrenchUnknownWordSignatures.HasPunc(word));
					//        else
					//          sb.append(FrenchUnknownWordSignatures.isPunc(word));
					sb.Append(FrenchUnknownWordSignatures.IsAllCaps(word));
					if (loc > 0)
					{
						if (FrenchUnknownWordSignatures.IsAllCaps(word).Equals(string.Empty))
						{
							sb.Append(FrenchUnknownWordSignatures.IsCapitalized(word));
						}
					}
					//Backoff to suffix if we haven't matched anything else
					if (unknownSuffixSize > 0 && sb.ToString().Equals(BaseLabel))
					{
						int min = word.Length < unknownSuffixSize ? word.Length : unknownSuffixSize;
						sb.Append('-').Append(Sharpen.Runtime.Substring(word, word.Length - min));
					}
					break;
				}

				default:
				{
					System.Console.Error.Printf("%s: Invalid unknown word signature! (%d)%n", this.GetType().FullName, unknownLevel);
					break;
				}
			}
			return sb.ToString();
		}
	}
}
