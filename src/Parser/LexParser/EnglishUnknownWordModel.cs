// Stanford Parser -- a probabilistic lexicalized NL CFG parser
// Copyright (c) 2002, 2003, 2004, 2005, 2008 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/ .
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    parser-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/lex-parser.html
using System;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>This is a basic unknown word model for English.</summary>
	/// <remarks>
	/// This is a basic unknown word model for English.  It supports 5 different
	/// types of feature modeling; see
	/// <see cref="GetSignature(string, int)"/>
	/// .
	/// <i>Implementation note: the contents of this class tend to overlap somewhat
	/// with
	/// <see cref="ArabicUnknownWordModel"/>
	/// and were originally included in
	/// <see cref="BaseLexicon"/>
	/// .
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Galen Andrew</author>
	/// <author>Christopher Manning</author>
	/// <author>Anna Rafferty</author>
	[System.Serializable]
	public class EnglishUnknownWordModel : BaseUnknownWordModel
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.EnglishUnknownWordModel));

		private const long serialVersionUID = 4825624957364628770L;

		private const bool DebugUwm = false;

		protected internal readonly bool smartMutation;

		protected internal readonly int unknownSuffixSize;

		protected internal readonly int unknownPrefixSize;

		protected internal readonly string wordClassesFile;

		private const int MinUnknown = 0;

		private const int MaxUnknown = 8;

		public EnglishUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, ClassicCounter<IntTaggedWord> unSeenCounter)
			: base(op, lex, wordIndex, tagIndex, unSeenCounter, null, null, null)
		{
			if (unknownLevel < MinUnknown || unknownLevel > MaxUnknown)
			{
				throw new ArgumentException("Invalid value for useUnknownWordSignatures: " + unknownLevel);
			}
			this.smartMutation = op.lexOptions.smartMutation;
			this.unknownSuffixSize = op.lexOptions.unknownSuffixSize;
			this.unknownPrefixSize = op.lexOptions.unknownPrefixSize;
			wordClassesFile = op.lexOptions.wordClassesFile;
		}

		/// <summary>This constructor creates an UWM with empty data structures.</summary>
		/// <remarks>
		/// This constructor creates an UWM with empty data structures.  Only
		/// use if loading in the data separately, such as by reading in text
		/// lines containing the data.
		/// </remarks>
		public EnglishUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: this(op, lex, wordIndex, tagIndex, new ClassicCounter<IntTaggedWord>())
		{
		}

		public override float Score(IntTaggedWord iTW, int loc, double c_Tseen, double total, double smooth, string word)
		{
			double pb_T_S = ScoreProbTagGivenWordSignature(iTW, loc, smooth, word);
			double p_T = (c_Tseen / total);
			double p_W = 1.0 / total;
			double pb_W_T = Math.Log(pb_T_S * p_W / p_T);
			if (pb_W_T > -100.0)
			{
				return (float)pb_W_T;
			}
			return float.NegativeInfinity;
		}

		// end score()
		/// <summary>Calculate P(Tag|Signature) with Bayesian smoothing via just P(Tag|Unknown)</summary>
		public override double ScoreProbTagGivenWordSignature(IntTaggedWord iTW, int loc, double smooth, string word)
		{
			// iTW.tag = nullTag;
			// double c_W = ((BaseLexicon) l).getCount(iTW);
			// iTW.tag = tag;
			// unknown word model for P(T|S)
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
			return (c_TS + smooth * p_T_U) / (c_S + smooth);
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

		/// <summary>
		/// This routine returns a String that is the "signature" of the class of a
		/// word.
		/// </summary>
		/// <remarks>
		/// This routine returns a String that is the "signature" of the class of a
		/// word. For, example, it might represent whether it is a number of ends in
		/// -s. The strings returned by convention matches the pattern UNK(-.+)? ,
		/// which is just assumed to not match any real word. Behavior depends on the
		/// unknownLevel (-uwm flag) passed in to the class. The recognized numbers are
		/// 1-5: 5 is fairly English-specific; 4, 3, and 2 look for various word
		/// features (digits, dashes, etc.) which are only vaguely English-specific; 1
		/// uses the last two characters combined with a simple classification by
		/// capitalization.
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
				case 8:
				{
					GetSignature8(word, sb);
					break;
				}

				case 7:
				{
					GetSignature7(word, loc, sb);
					break;
				}

				case 6:
				{
					GetSignature6(word, loc, sb);
					break;
				}

				case 5:
				{
					GetSignature5(word, loc, sb);
					break;
				}

				case 4:
				{
					GetSignature4(word, loc, sb);
					break;
				}

				case 3:
				{
					GetSignature3(word, loc, sb);
					break;
				}

				case 2:
				{
					GetSignature2(word, loc, sb);
					break;
				}

				case 1:
				{
					GetSignature1(word, loc, sb);
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
		private static void GetSignature7(string word, int loc, StringBuilder sb)
		{
			// New Sep 2008. Like 2 but rely more on Caps somewhere than initial Caps
			// {-ALLC, -INIT, -UC somewhere, -LC, zero} +
			// {-DASH, zero} +
			// {-NUM, -DIG, zero} +
			// {lowerLastChar, zeroIfShort}
			bool hasDigit = false;
			bool hasNonDigit = false;
			bool hasLower = false;
			bool hasUpper = false;
			bool hasDash = false;
			int wlen = word.Length;
			for (int i = 0; i < wlen; i++)
			{
				char ch = word[i];
				if (char.IsDigit(ch))
				{
					hasDigit = true;
				}
				else
				{
					hasNonDigit = true;
					if (char.IsLetter(ch))
					{
						if (char.IsLowerCase(ch) || char.IsTitleCase(ch))
						{
							hasLower = true;
						}
						else
						{
							hasUpper = true;
						}
					}
					else
					{
						if (ch == '-')
						{
							hasDash = true;
						}
					}
				}
			}
			if (wlen > 0 && hasUpper)
			{
				if (!hasLower)
				{
					sb.Append("-ALLC");
				}
				else
				{
					if (loc == 0)
					{
						sb.Append("-INIT");
					}
					else
					{
						sb.Append("-UC");
					}
				}
			}
			else
			{
				if (hasLower)
				{
					// if (Character.isLowerCase(word.charAt(0))) {
					sb.Append("-LC");
				}
			}
			// no suffix = no (lowercase) letters
			if (hasDash)
			{
				sb.Append("-DASH");
			}
			if (hasDigit)
			{
				if (!hasNonDigit)
				{
					sb.Append("-NUM");
				}
				else
				{
					sb.Append("-DIG");
				}
			}
			else
			{
				if (wlen > 3)
				{
					// don't do for very short words: "yes" isn't an "-es" word
					// try doing to lower for further densening and skipping digits
					char ch = word[word.Length - 1];
					sb.Append(char.ToLowerCase(ch));
				}
			}
		}

		// no suffix = short non-number, non-alphabetic
		private void GetSignature6(string word, int loc, StringBuilder sb)
		{
			// New Sep 2008. Like 5 but rely more on Caps somewhere than initial Caps
			// { -INITC, -CAPS, (has) -CAP, -LC lowercase, 0 } +
			// { -KNOWNLC, 0 } + [only for INITC]
			// { -NUM, 0 } +
			// { -DASH, 0 } +
			// { -last lowered char(s) if known discriminating suffix, 0}
			int wlen = word.Length;
			int numCaps = 0;
			bool hasDigit = false;
			bool hasDash = false;
			bool hasLower = false;
			for (int i = 0; i < wlen; i++)
			{
				char ch = word[i];
				if (char.IsDigit(ch))
				{
					hasDigit = true;
				}
				else
				{
					if (ch == '-')
					{
						hasDash = true;
					}
					else
					{
						if (char.IsLetter(ch))
						{
							if (char.IsLowerCase(ch))
							{
								hasLower = true;
							}
							else
							{
								if (char.IsTitleCase(ch))
								{
									hasLower = true;
									numCaps++;
								}
								else
								{
									numCaps++;
								}
							}
						}
					}
				}
			}
			string lowered = word.ToLower();
			if (numCaps > 1)
			{
				sb.Append("-CAPS");
			}
			else
			{
				if (numCaps > 0)
				{
					if (loc == 0)
					{
						sb.Append("-INITC");
						if (GetLexicon().IsKnown(lowered))
						{
							sb.Append("-KNOWNLC");
						}
					}
					else
					{
						sb.Append("-CAP");
					}
				}
				else
				{
					if (hasLower)
					{
						// (Character.isLowerCase(ch0)) {
						sb.Append("-LC");
					}
				}
			}
			if (hasDigit)
			{
				sb.Append("-NUM");
			}
			if (hasDash)
			{
				sb.Append("-DASH");
			}
			if (lowered.EndsWith("s") && wlen >= 3)
			{
				// here length 3, so you don't miss out on ones like 80s
				char ch2 = lowered[wlen - 2];
				// not -ess suffixes or greek/latin -us, -is
				if (ch2 != 's' && ch2 != 'i' && ch2 != 'u')
				{
					sb.Append("-s");
				}
			}
			else
			{
				if (word.Length >= 5 && !hasDash && !(hasDigit && numCaps > 0))
				{
					// don't do for very short words;
					// Implement common discriminating suffixes
					if (lowered.EndsWith("ed"))
					{
						sb.Append("-ed");
					}
					else
					{
						if (lowered.EndsWith("ing"))
						{
							sb.Append("-ing");
						}
						else
						{
							if (lowered.EndsWith("ion"))
							{
								sb.Append("-ion");
							}
							else
							{
								if (lowered.EndsWith("er"))
								{
									sb.Append("-er");
								}
								else
								{
									if (lowered.EndsWith("est"))
									{
										sb.Append("-est");
									}
									else
									{
										if (lowered.EndsWith("ly"))
										{
											sb.Append("-ly");
										}
										else
										{
											if (lowered.EndsWith("ity"))
											{
												sb.Append("-ity");
											}
											else
											{
												if (lowered.EndsWith("y"))
												{
													sb.Append("-y");
												}
												else
												{
													if (lowered.EndsWith("al"))
													{
														sb.Append("-al");
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
		}

		// } else if (lowered.endsWith("ble")) {
		// sb.append("-ble");
		// } else if (lowered.endsWith("e")) {
		// sb.append("-e");
		private void GetSignature5(string word, int loc, StringBuilder sb)
		{
			// Reformed Mar 2004 (cdm); hopefully better now.
			// { -CAPS, -INITC ap, -LC lowercase, 0 } +
			// { -KNOWNLC, 0 } + [only for INITC]
			// { -NUM, 0 } +
			// { -DASH, 0 } +
			// { -last lowered char(s) if known discriminating suffix, 0}
			int wlen = word.Length;
			int numCaps = 0;
			bool hasDigit = false;
			bool hasDash = false;
			bool hasLower = false;
			for (int i = 0; i < wlen; i++)
			{
				char ch = word[i];
				if (char.IsDigit(ch))
				{
					hasDigit = true;
				}
				else
				{
					if (ch == '-')
					{
						hasDash = true;
					}
					else
					{
						if (char.IsLetter(ch))
						{
							if (char.IsLowerCase(ch))
							{
								hasLower = true;
							}
							else
							{
								if (char.IsTitleCase(ch))
								{
									hasLower = true;
									numCaps++;
								}
								else
								{
									numCaps++;
								}
							}
						}
					}
				}
			}
			char ch0 = word[0];
			string lowered = word.ToLower();
			if (char.IsUpperCase(ch0) || char.IsTitleCase(ch0))
			{
				if (loc == 0 && numCaps == 1)
				{
					sb.Append("-INITC");
					if (GetLexicon().IsKnown(lowered))
					{
						sb.Append("-KNOWNLC");
					}
				}
				else
				{
					sb.Append("-CAPS");
				}
			}
			else
			{
				if (!char.IsLetter(ch0) && numCaps > 0)
				{
					sb.Append("-CAPS");
				}
				else
				{
					if (hasLower)
					{
						// (Character.isLowerCase(ch0)) {
						sb.Append("-LC");
					}
				}
			}
			if (hasDigit)
			{
				sb.Append("-NUM");
			}
			if (hasDash)
			{
				sb.Append("-DASH");
			}
			if (lowered.EndsWith("s") && wlen >= 3)
			{
				// here length 3, so you don't miss out on ones like 80s
				char ch2 = lowered[wlen - 2];
				// not -ess suffixes or greek/latin -us, -is
				if (ch2 != 's' && ch2 != 'i' && ch2 != 'u')
				{
					sb.Append("-s");
				}
			}
			else
			{
				if (word.Length >= 5 && !hasDash && !(hasDigit && numCaps > 0))
				{
					// don't do for very short words;
					// Implement common discriminating suffixes
					if (lowered.EndsWith("ed"))
					{
						sb.Append("-ed");
					}
					else
					{
						if (lowered.EndsWith("ing"))
						{
							sb.Append("-ing");
						}
						else
						{
							if (lowered.EndsWith("ion"))
							{
								sb.Append("-ion");
							}
							else
							{
								if (lowered.EndsWith("er"))
								{
									sb.Append("-er");
								}
								else
								{
									if (lowered.EndsWith("est"))
									{
										sb.Append("-est");
									}
									else
									{
										if (lowered.EndsWith("ly"))
										{
											sb.Append("-ly");
										}
										else
										{
											if (lowered.EndsWith("ity"))
											{
												sb.Append("-ity");
											}
											else
											{
												if (lowered.EndsWith("y"))
												{
													sb.Append("-y");
												}
												else
												{
													if (lowered.EndsWith("al"))
													{
														sb.Append("-al");
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
		}

		// } else if (lowered.endsWith("ble")) {
		// sb.append("-ble");
		// } else if (lowered.endsWith("e")) {
		// sb.append("-e");
		private static void GetSignature4(string word, int loc, StringBuilder sb)
		{
			bool hasDigit = false;
			bool hasNonDigit = false;
			bool hasLetter = false;
			bool hasLower = false;
			bool hasDash = false;
			bool hasPeriod = false;
			bool hasComma = false;
			for (int i = 0; i < word.Length; i++)
			{
				char ch = word[i];
				if (char.IsDigit(ch))
				{
					hasDigit = true;
				}
				else
				{
					hasNonDigit = true;
					if (char.IsLetter(ch))
					{
						hasLetter = true;
						if (char.IsLowerCase(ch) || char.IsTitleCase(ch))
						{
							hasLower = true;
						}
					}
					else
					{
						if (ch == '-')
						{
							hasDash = true;
						}
						else
						{
							if (ch == '.')
							{
								hasPeriod = true;
							}
							else
							{
								if (ch == ',')
								{
									hasComma = true;
								}
							}
						}
					}
				}
			}
			// 6 way on letters
			if (char.IsUpperCase(word[0]) || char.IsTitleCase(word[0]))
			{
				if (!hasLower)
				{
					sb.Append("-AC");
				}
				else
				{
					if (loc == 0)
					{
						sb.Append("-SC");
					}
					else
					{
						sb.Append("-C");
					}
				}
			}
			else
			{
				if (hasLower)
				{
					sb.Append("-L");
				}
				else
				{
					if (hasLetter)
					{
						sb.Append("-U");
					}
					else
					{
						// no letter
						sb.Append("-S");
					}
				}
			}
			// 3 way on number
			if (hasDigit && !hasNonDigit)
			{
				sb.Append("-N");
			}
			else
			{
				if (hasDigit)
				{
					sb.Append("-n");
				}
			}
			// binary on period, dash, comma
			if (hasDash)
			{
				sb.Append("-H");
			}
			if (hasPeriod)
			{
				sb.Append("-P");
			}
			if (hasComma)
			{
				sb.Append("-C");
			}
			if (word.Length > 3)
			{
				// don't do for very short words: "yes" isn't an "-es" word
				// try doing to lower for further densening and skipping digits
				char ch = word[word.Length - 1];
				if (char.IsLetter(ch))
				{
					sb.Append('-');
					sb.Append(char.ToLowerCase(ch));
				}
			}
		}

		private static void GetSignature3(string word, int loc, StringBuilder sb)
		{
			// This basically works right, except note that 'S' is applied to all
			// capitalized letters in first word of sentence, not just first....
			sb.Append('-');
			char lastClass = '-';
			// i.e., nothing
			int num = 0;
			for (int i = 0; i < word.Length; i++)
			{
				char ch = word[i];
				char newClass;
				if (char.IsUpperCase(ch) || char.IsTitleCase(ch))
				{
					if (loc == 0)
					{
						newClass = 'S';
					}
					else
					{
						newClass = 'L';
					}
				}
				else
				{
					if (char.IsLetter(ch))
					{
						newClass = 'l';
					}
					else
					{
						if (char.IsDigit(ch))
						{
							newClass = 'd';
						}
						else
						{
							if (ch == '-')
							{
								newClass = 'h';
							}
							else
							{
								if (ch == '.')
								{
									newClass = 'p';
								}
								else
								{
									newClass = 's';
								}
							}
						}
					}
				}
				if (newClass != lastClass)
				{
					lastClass = newClass;
					sb.Append(lastClass);
					num = 1;
				}
				else
				{
					if (num < 2)
					{
						sb.Append('+');
					}
					num++;
				}
			}
			if (word.Length > 3)
			{
				// don't do for very short words: "yes" isn't an "-es" word
				// try doing to lower for further densening and skipping digits
				char ch = char.ToLowerCase(word[word.Length - 1]);
				sb.Append('-');
				sb.Append(ch);
			}
		}

		private static void GetSignature2(string word, int loc, StringBuilder sb)
		{
			// {-ALLC, -INIT, -UC, -LC, zero} +
			// {-DASH, zero} +
			// {-NUM, -DIG, zero} +
			// {lowerLastChar, zeroIfShort}
			bool hasDigit = false;
			bool hasNonDigit = false;
			bool hasLower = false;
			int wlen = word.Length;
			for (int i = 0; i < wlen; i++)
			{
				char ch = word[i];
				if (char.IsDigit(ch))
				{
					hasDigit = true;
				}
				else
				{
					hasNonDigit = true;
					if (char.IsLetter(ch))
					{
						if (char.IsLowerCase(ch) || char.IsTitleCase(ch))
						{
							hasLower = true;
						}
					}
				}
			}
			if (wlen > 0 && (char.IsUpperCase(word[0]) || char.IsTitleCase(word[0])))
			{
				if (!hasLower)
				{
					sb.Append("-ALLC");
				}
				else
				{
					if (loc == 0)
					{
						sb.Append("-INIT");
					}
					else
					{
						sb.Append("-UC");
					}
				}
			}
			else
			{
				if (hasLower)
				{
					// if (Character.isLowerCase(word.charAt(0))) {
					sb.Append("-LC");
				}
			}
			// no suffix = no (lowercase) letters
			if (word.IndexOf('-') >= 0)
			{
				sb.Append("-DASH");
			}
			if (hasDigit)
			{
				if (!hasNonDigit)
				{
					sb.Append("-NUM");
				}
				else
				{
					sb.Append("-DIG");
				}
			}
			else
			{
				if (wlen > 3)
				{
					// don't do for very short words: "yes" isn't an "-es" word
					// try doing toLower for further densening and skipping digits
					char ch = word[word.Length - 1];
					sb.Append(char.ToLowerCase(ch));
				}
			}
		}

		// no suffix = short non-number, non-alphabetic
		private static void GetSignature1(string word, int loc, StringBuilder sb)
		{
			sb.Append('-');
			sb.Append(Sharpen.Runtime.Substring(word, Math.Max(word.Length - 2, 0), word.Length));
			sb.Append('-');
			if (char.IsLowerCase(word[0]))
			{
				sb.Append("LOWER");
			}
			else
			{
				if (char.IsUpperCase(word[0]))
				{
					if (loc == 0)
					{
						sb.Append("INIT");
					}
					else
					{
						sb.Append("UPPER");
					}
				}
				else
				{
					sb.Append("OTHER");
				}
			}
		}

		private void GetSignature8(string word, StringBuilder sb)
		{
			sb.Append('-');
			bool digit = true;
			for (int i = 0; i < word.Length; i++)
			{
				char c = word[i];
				if (!(char.IsDigit(c) || c == '.' || c == ',' || (i == 0 && (c == '-' || c == '+'))))
				{
					digit = false;
				}
			}
			// digit = false;  // todo: Just turned off while we test it.
			if (digit)
			{
				sb.Append("NUMBER");
			}
			else
			{
				if (distSim == null)
				{
					distSim = new DistSimClassifier(wordClassesFile, false, true);
				}
				// todo XXXX booleans depend on distsim file; need more options
				string cluster = distSim.DistSimClass(word);
				if (cluster == null)
				{
					cluster = "NULL";
				}
				sb.Append(cluster);
			}
		}

		[System.NonSerialized]
		private DistSimClassifier distSim;
	}
}
