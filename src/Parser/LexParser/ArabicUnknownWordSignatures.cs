using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Unknown word signatures for the Arabic Treebank.</summary>
	/// <remarks>
	/// Unknown word signatures for the Arabic Treebank.
	/// These handle unvocalized Arabic, in either Buckwalter or Unicode.
	/// </remarks>
	/// <author>Roger Levy (rog@csli.stanford.edu)</author>
	/// <author>Christopher Manning (extended to handle UTF-8)</author>
	internal class ArabicUnknownWordSignatures
	{
		private ArabicUnknownWordSignatures()
		{
		}

		internal static bool AllDigitPlus(string word)
		{
			bool allDigitPlus = true;
			bool seenDigit = false;
			for (int i = 0; i < wlen; i++)
			{
				char ch = word[i];
				if (char.IsDigit(ch))
				{
					seenDigit = true;
				}
				else
				{
					if (ch == '-' || ch == '.' || ch == ',' || ch == '\u066B' || ch == '\u066C' || ch == '\u2212')
					{
					}
					else
					{
						// U+066B = Arabic decimal separator
						// U+066C = Arabic thousands separator
						// U+2212 = Minus sign
						allDigitPlus = false;
					}
				}
			}
			return allDigitPlus && seenDigit;
		}

		/// <summary>
		/// nisba suffix for deriving adjectives: (i)yy(n) [masc]
		/// or -(i)yya [fem].
		/// </summary>
		/// <remarks>
		/// nisba suffix for deriving adjectives: (i)yy(n) [masc]
		/// or -(i)yya [fem].  Other adjectives are made in the binyanim system
		/// by vowel changes.
		/// </remarks>
		private static readonly Pattern adjectivalSuffixPattern = Pattern.Compile("[y\u064A][y\u064A](?:[t\u062A]?[n\u0646])?$");

		internal static string LikelyAdjectivalSuffix(string word)
		{
			if (adjectivalSuffixPattern.Matcher(word).Find())
			{
				return "-AdjSuffix";
			}
			else
			{
				return string.Empty;
			}
		}

		private static readonly Pattern singularPastTenseSuffixPattern = Pattern.Compile("[t\u062A]$");

		private static readonly Pattern pluralFirstPersonPastTenseSuffixPattern = Pattern.Compile("[n\u0646][A\u0627]$");

		private static readonly Pattern pluralThirdPersonMasculinePastTenseSuffixPattern = Pattern.Compile("[w\u0648]$");

		// could be used but doesn't seem very discriminating
		// private static final Pattern pluralThirdPersonFemininePastTenseSuffixPattern = Pattern.compile("[n\u0646]$");
		// there doesn't seem to be second-person marking in the corpus, just first
		// and non-first (?)
		internal static string PastTenseVerbNumberSuffix(string word)
		{
			if (singularPastTenseSuffixPattern.Matcher(word).Find())
			{
				return "-PV.sg";
			}
			if (pluralFirstPersonPastTenseSuffixPattern.Matcher(word).Find())
			{
				return "-PV.pl1";
			}
			if (pluralThirdPersonMasculinePastTenseSuffixPattern.Matcher(word).Find())
			{
				return "-PV.pl3m";
			}
			return string.Empty;
		}

		private static readonly Pattern pluralThirdPersonMasculinePresentTenseSuffixPattern = Pattern.Compile("[w\u0648][\u0646n]$");

		internal static string PresentTenseVerbNumberSuffix(string word)
		{
			return pluralThirdPersonMasculinePresentTenseSuffixPattern.Matcher(word).Find() ? "-IV.pl3m" : string.Empty;
		}

		private static readonly Pattern taaMarbuuTaSuffixPattern = Pattern.Compile("[\u0629p]$");

		// almost always ADJ or NOUN
		internal static string TaaMarbuuTaSuffix(string word)
		{
			return taaMarbuuTaSuffixPattern.Matcher(word).Find() ? "-taaMarbuuTa" : string.Empty;
		}

		private static readonly Pattern abstractionNounSuffixPattern = Pattern.Compile("[y\u064a][p\u0629]$");

		// Roger wrote: "ironically, this seems to be a better indicator of ADJ than
		// of NOUN", but Chris thinks it may just have been a bug in his code
		internal static string AbstractionNounSuffix(string word)
		{
			return abstractionNounSuffixPattern.Matcher(word).Find() ? "-AbstractionSuffix" : string.Empty;
		}

		private static readonly Pattern masdarPrefixPattern = Pattern.Compile("^[t\u062A]");

		internal static string MasdarPrefix(string word)
		{
			return masdarPrefixPattern.Matcher(word).Find() ? "-maSdr" : string.Empty;
		}
	}
}
