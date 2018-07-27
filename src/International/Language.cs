using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.International
{
	/// <summary>Constants and parameters for multilingual NLP (primarily, parsing).</summary>
	/// <author>Spence Green (original Languages class for parsing)</author>
	/// <author>Gabor Angeli (factor out Language enum)</author>
	[System.Serializable]
	public sealed class Language
	{
		public static readonly Edu.Stanford.Nlp.International.Language Any = new Edu.Stanford.Nlp.International.Language(new EnglishTreebankParserParams());

		public static readonly Edu.Stanford.Nlp.International.Language Arabic = new Edu.Stanford.Nlp.International.Language(new ArabicTreebankParserParams());

		public static readonly Edu.Stanford.Nlp.International.Language Chinese = new Edu.Stanford.Nlp.International.Language(new ChineseTreebankParserParams());

		private sealed class _EnglishTreebankParserParams_18 : EnglishTreebankParserParams
		{
			public _EnglishTreebankParserParams_18()
			{
				{
					this.SetGenerateOriginalDependencies(true);
				}
			}
		}

		public static readonly Edu.Stanford.Nlp.International.Language English = new Edu.Stanford.Nlp.International.Language(new _EnglishTreebankParserParams_18());

		public static readonly Edu.Stanford.Nlp.International.Language German = new Edu.Stanford.Nlp.International.Language(new NegraPennTreebankParserParams());

		public static readonly Edu.Stanford.Nlp.International.Language French = new Edu.Stanford.Nlp.International.Language(new FrenchTreebankParserParams());

		public static readonly Edu.Stanford.Nlp.International.Language Hebrew = new Edu.Stanford.Nlp.International.Language(new HebrewTreebankParserParams());

		public static readonly Edu.Stanford.Nlp.International.Language Spanish = new Edu.Stanford.Nlp.International.Language(new SpanishTreebankParserParams());

		public static readonly Edu.Stanford.Nlp.International.Language UniversalChinese = new Edu.Stanford.Nlp.International.Language(new ChineseTreebankParserParams());

		public static readonly Edu.Stanford.Nlp.International.Language UniversalEnglish = new Edu.Stanford.Nlp.International.Language(new EnglishTreebankParserParams());

		public static readonly Edu.Stanford.Nlp.International.Language Unknown = new Edu.Stanford.Nlp.International.Language(new EnglishTreebankParserParams());

		public static readonly string langList = StringUtils.Join(Arrays.AsList(Edu.Stanford.Nlp.International.Language.Values()), " ");

		public readonly ITreebankLangParserParams @params;

		internal Language(ITreebankLangParserParams @params)
		{
			this.@params = @params;
		}

		/// <summary>Returns whether these two languages can be considered compatible with each other.</summary>
		/// <remarks>
		/// Returns whether these two languages can be considered compatible with each other.
		/// Mostly here to handle the "Any" language value.
		/// </remarks>
		public bool CompatibleWith(Edu.Stanford.Nlp.International.Language other)
		{
			return this == other || this == Edu.Stanford.Nlp.International.Language.Any || other == Edu.Stanford.Nlp.International.Language.Any;
		}
	}
}
