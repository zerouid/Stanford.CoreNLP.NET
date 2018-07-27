


namespace Edu.Stanford.Nlp.International.Spanish
{
	/// <summary>
	/// Contains patterns for matching certain word types in Spanish, such
	/// as common suffices for nouns, verbs, adjectives and adverbs.
	/// </summary>
	/// <remarks>
	/// Contains patterns for matching certain word types in Spanish, such
	/// as common suffices for nouns, verbs, adjectives and adverbs.
	/// These utilities are used to characterize unknown words within the
	/// POS tagger and the parser.
	/// </remarks>
	/// <seealso cref="Edu.Stanford.Nlp.Tagger.Maxent.ExtractorFramesRare"/>
	/// <seealso cref="Edu.Stanford.Nlp.Parser.Lexparser.SpanishUnknownWordModel"/>
	/// <author>Jon Gauthier</author>
	public class SpanishUnknownWordSignatures
	{
		private static readonly Pattern pMasculine = Pattern.Compile("os?$");

		private static readonly Pattern pFeminine = Pattern.Compile("as?$");

		private static readonly Pattern pConditionalSuffix = Pattern.Compile("[aei]ría(?:s|mos|is|n)?$");

		private static readonly Pattern pImperfectErIrSuffix = Pattern.Compile("[^r]ía(?:s|mos|is|n)?$");

		private static readonly Pattern pImperfect = Pattern.Compile("(?:aba(?:[sn]|is)?|ábamos|[^r]ía(?:s|mos|is|n)?)$");

		private static readonly Pattern pInfinitive = Pattern.Compile("[aei]r$");

		private static readonly Pattern pAdverb = Pattern.Compile("mente$");

		private static readonly Pattern pVerbFirstPersonPlural = Pattern.Compile("(?<!últ|máx|mín|án|próx|ís|cént|[np]ón|prést|gít|ínt|pár" + "|^extr|^supr|^tr?|^[Rr]?|gr)[eia]mos$");

		private static readonly Pattern pGerund = Pattern.Compile("(?i)((?<!^([bmn]|bl|com|contrab|cu|[fh]ern))a" + "|(?<!^(asci|ati|atu|compr|condesci|conti|desati|desci|desenti|disti|divid|enci|enti|estup" + "|exti|fi|hi|malenti|pret|refer|rever|sobreenti|subti|ti|transci|trasci|trem))e)ndo$"
			);

		private SpanishUnknownWordSignatures()
		{
		}

		// The following patterns help to distinguish between verbs in the
		// conditional tense and -er, -ir verbs in the indicative imperfect.
		// Words in these two forms have matching suffixes and are otherwise
		// difficult to distinguish.
		// Most of the words disguised as first-person plural verb forms have
		// contrastive stress.. yay, easy to match!
		// static methods
		public static bool HasMasculineSuffix(string s)
		{
			return pMasculine.Matcher(s).Find();
		}

		public static bool HasFeminineSuffix(string s)
		{
			return pFeminine.Matcher(s).Find();
		}

		public static bool HasConditionalSuffix(string s)
		{
			return pConditionalSuffix.Matcher(s).Find();
		}

		public static bool HasImperfectErIrSuffix(string s)
		{
			return pImperfectErIrSuffix.Matcher(s).Find();
		}

		public static bool HasImperfectSuffix(string s)
		{
			return pImperfect.Matcher(s).Find();
		}

		public static bool HasInfinitiveSuffix(string s)
		{
			return pInfinitive.Matcher(s).Find();
		}

		public static bool HasAdverbSuffix(string s)
		{
			return pAdverb.Matcher(s).Find();
		}

		public static bool HasVerbFirstPersonPluralSuffix(string s)
		{
			return pVerbFirstPersonPlural.Matcher(s).Find();
		}

		public static bool HasGerundSuffix(string s)
		{
			return pGerund.Matcher(s).Find();
		}

		// The *Suffix methods are used by the SpanishUnknownWordModel to
		// build a representation of an unknown word.
		public static string ConditionalSuffix(string s)
		{
			return HasConditionalSuffix(s) ? "-cond" : string.Empty;
		}

		public static string ImperfectSuffix(string s)
		{
			return HasImperfectSuffix(s) ? "-imp" : string.Empty;
		}

		public static string InfinitiveSuffix(string s)
		{
			return HasInfinitiveSuffix(s) ? "-inf" : string.Empty;
		}

		public static string AdverbSuffix(string s)
		{
			return HasAdverbSuffix(s) ? "-adv" : string.Empty;
		}
	}
}
