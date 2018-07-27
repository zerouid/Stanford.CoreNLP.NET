


namespace Edu.Stanford.Nlp.International.French
{
	/// <summary>
	/// Contains patterns for matching certain word types in French, such
	/// as common suffices for nouns, verbs, adjectives and adverbs.
	/// </summary>
	public class FrenchUnknownWordSignatures
	{
		private static readonly Pattern pNounSuffix = Pattern.Compile("(?:ier|ière|ité|ion|ison|isme|ysme|iste|esse|eur|euse|ence|eau|erie|ng|ette|age|ade|ance|ude|ogue|aphe|ate|duc|anthe|archie|coque|érèse|ergie|ogie|lithe|mètre|métrie|odie|pathie|phie|phone|phore|onyme|thèque|scope|some|pole|ôme|chromie|pie)s?$"
			);

		private static readonly Pattern pAdjSuffix = Pattern.Compile("(?:iste|ième|uple|issime|aire|esque|atoire|ale|al|able|ible|atif|ique|if|ive|eux|aise|ent|ois|oise|ante|el|elle|ente|oire|ain|aine)s?$");

		private static readonly Pattern pHasDigit = Pattern.Compile("\\d+");

		private static readonly Pattern pIsDigit = Pattern.Compile("^\\d+$");

		private static readonly Pattern pPosPlural = Pattern.Compile("(?:s|ux)$");

		private static readonly Pattern pVerbSuffix = Pattern.Compile("(?:ir|er|re|ez|ont|ent|ant|ais|ait|ra|era|eras|é|és|ées|isse|it)$");

		private static readonly Pattern pAdvSuffix = Pattern.Compile("(?:iment|ement|emment|amment)$");

		private static readonly Pattern pHasPunc = Pattern.Compile("(?:[\u0021-\u002F\u003A-\u0040\\u005B-\u0060\u007B-\u007E\u00A1-\u00BF\u00F7\u2010-\u2027\u2030-\u205E\u20A0-\u20BA])+");

		private static readonly Pattern pIsPunc = Pattern.Compile("^(?:[\u0021-\u002F\u003A-\u0040\\u005B-\u0060\u007B-\u007E\u00A1-\u00BF\u00F7\u2010-\u2027\u2030-\u205E\u20A0-\u20BA])+$");

		private static readonly Pattern pAllCaps = Pattern.Compile("^[A-Z\u00C0-\u00D6\u00D8-\u00DE]+$");

		private FrenchUnknownWordSignatures()
		{
		}

		// static methods
		public static bool HasNounSuffix(string s)
		{
			return pNounSuffix.Matcher(s).Find();
		}

		public static string NounSuffix(string s)
		{
			return HasNounSuffix(s) ? "-noun" : string.Empty;
		}

		public static bool HasAdjSuffix(string s)
		{
			return pAdjSuffix.Matcher(s).Find();
		}

		public static string AdjSuffix(string s)
		{
			return HasAdjSuffix(s) ? "-adj" : string.Empty;
		}

		public static string HasDigit(string s)
		{
			return pHasDigit.Matcher(s).Find() ? "-num" : string.Empty;
		}

		public static string IsDigit(string s)
		{
			return pIsDigit.Matcher(s).Find() ? "-isNum" : string.Empty;
		}

		public static bool HasVerbSuffix(string s)
		{
			return pVerbSuffix.Matcher(s).Find();
		}

		public static string VerbSuffix(string s)
		{
			return HasVerbSuffix(s) ? "-verb" : string.Empty;
		}

		public static bool HasPossiblePlural(string s)
		{
			return pPosPlural.Matcher(s).Find();
		}

		public static string PossiblePlural(string s)
		{
			return HasPossiblePlural(s) ? "-plural" : string.Empty;
		}

		public static bool HasAdvSuffix(string s)
		{
			return pAdvSuffix.Matcher(s).Find();
		}

		public static string AdvSuffix(string s)
		{
			return HasAdvSuffix(s) ? "-adv" : string.Empty;
		}

		public static string HasPunc(string s)
		{
			return pHasPunc.Matcher(s).Find() ? "-hpunc" : string.Empty;
		}

		public static string IsPunc(string s)
		{
			return pIsPunc.Matcher(s).Matches() ? "-ipunc" : string.Empty;
		}

		public static string IsAllCaps(string s)
		{
			return pAllCaps.Matcher(s).Matches() ? "-allcap" : string.Empty;
		}

		public static string IsCapitalized(string s)
		{
			if (s.Length > 0)
			{
				char ch = s[0];
				return char.IsUpperCase((char)ch) ? "-upper" : string.Empty;
			}
			return string.Empty;
		}
	}
}
