using System.Collections.Generic;
using System.Reflection;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class contains mappings from strings to language properties files.</summary>
	public class LanguageInfo
	{
		/// <summary>languages supported</summary>
		public enum HumanLanguage
		{
			Arabic,
			Chinese,
			English,
			French,
			German,
			Spanish
		}

		/// <summary>list of properties files for each language</summary>
		public const string ArabicProperties = "StanfordCoreNLP-arabic.properties";

		public const string ChineseProperties = "StanfordCoreNLP-chinese.properties";

		public const string EnglishProperties = "StanfordCoreNLP.properties";

		public const string FrenchProperties = "StanfordCoreNLP-french.properties";

		public const string GermanProperties = "StanfordCoreNLP-german.properties";

		public const string SpanishProperties = "StanfordCoreNLP-spanish.properties";

		/// <summary>map enum to properties file</summary>
		public static readonly IDictionary<LanguageInfo.HumanLanguage, string> languageToPropertiesFile;

		static LanguageInfo()
		{
			languageToPropertiesFile = new EnumMap<LanguageInfo.HumanLanguage, string>(typeof(LanguageInfo.HumanLanguage));
			languageToPropertiesFile[LanguageInfo.HumanLanguage.Arabic] = ArabicProperties;
			languageToPropertiesFile[LanguageInfo.HumanLanguage.Chinese] = ChineseProperties;
			languageToPropertiesFile[LanguageInfo.HumanLanguage.English] = EnglishProperties;
			languageToPropertiesFile[LanguageInfo.HumanLanguage.French] = FrenchProperties;
			languageToPropertiesFile[LanguageInfo.HumanLanguage.German] = GermanProperties;
			languageToPropertiesFile[LanguageInfo.HumanLanguage.Spanish] = SpanishProperties;
		}

		private LanguageInfo()
		{
		}

		/// <summary>Go through all of the paths via reflection, and print them out in a TSV format.</summary>
		/// <remarks>
		/// Go through all of the paths via reflection, and print them out in a TSV format.
		/// This is useful for command line scripts.
		/// </remarks>
		/// <param name="args">Ignored.</param>
		/// <exception cref="System.MemberAccessException"/>
		public static void Main(string[] args)
		{
			foreach (FieldInfo field in typeof(Edu.Stanford.Nlp.Pipeline.LanguageInfo).GetFields())
			{
				System.Console.Out.WriteLine(field.Name + "\t" + field.GetValue(null));
			}
		}

		/// <summary>return the properties file name for a specific language</summary>
		public static string GetLanguagePropertiesFile(string inputString)
		{
			return languageToPropertiesFile[GetLanguageFromString(inputString)];
		}

		/// <summary>convert various input strings to language enum</summary>
		public static LanguageInfo.HumanLanguage GetLanguageFromString(string inputString)
		{
			if (inputString.ToLower().Equals("arabic") || inputString.ToLower().Equals("ar"))
			{
				return LanguageInfo.HumanLanguage.Arabic;
			}
			if (inputString.ToLower().Equals("english") || inputString.ToLower().Equals("en"))
			{
				return LanguageInfo.HumanLanguage.English;
			}
			if (inputString.ToLower().Equals("chinese") || inputString.ToLower().Equals("zh"))
			{
				return LanguageInfo.HumanLanguage.Chinese;
			}
			if (inputString.ToLower().Equals("french") || inputString.ToLower().Equals("fr"))
			{
				return LanguageInfo.HumanLanguage.French;
			}
			if (inputString.ToLower().Equals("german") || inputString.ToLower().Equals("de"))
			{
				return LanguageInfo.HumanLanguage.German;
			}
			if (inputString.ToLower().Equals("spanish") || inputString.ToLower().Equals("es"))
			{
				return LanguageInfo.HumanLanguage.Spanish;
			}
			else
			{
				return null;
			}
		}

		/// <summary>Check if language is a segmenter language, return boolean.</summary>
		public static bool IsSegmenterLanguage(LanguageInfo.HumanLanguage language)
		{
			return language == LanguageInfo.HumanLanguage.Arabic || language == LanguageInfo.HumanLanguage.Chinese;
		}

		public static bool IsSegmenterLanguage(string inputString)
		{
			return IsSegmenterLanguage(GetLanguageFromString(inputString));
		}
	}
}
