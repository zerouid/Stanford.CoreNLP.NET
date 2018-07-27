using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees.Treebank;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>
	/// Applies a default set of lexical transformations that have been empirically validated
	/// in various Arabic tasks.
	/// </summary>
	/// <remarks>
	/// Applies a default set of lexical transformations that have been empirically validated
	/// in various Arabic tasks. This class automatically detects the input encoding and applies
	/// the appropriate set of transformations.
	/// </remarks>
	/// <author>Spence Green</author>
	public class GaleP4LexMapper : IMapper
	{
		private static readonly Pattern utf8ArabicChart = Pattern.Compile("[\u0600-\u06FF]");

		private const string bwAlefChar = "A";

		private static readonly Pattern bwDiacritics = Pattern.Compile("F|N|K|a|u|i|\\~|o");

		private static readonly Pattern bwTatweel = Pattern.Compile("_");

		private static readonly Pattern bwAlef = Pattern.Compile("\\{");

		private static readonly Pattern bwQuran = Pattern.Compile("`");

		private static readonly Pattern utf8Diacritics = Pattern.Compile("َ|ً|ُ|ٌ|ِ|ٍ|ّ|ْ");

		private static readonly Pattern utf8Tatweel = Pattern.Compile("ـ");

		private static readonly Pattern utf8Alef = Pattern.Compile("\u0671");

		private static readonly Pattern utf8Quran = Pattern.Compile("[\u0615-\u061A]|[\u06D6-\u06E5]");

		private static readonly Pattern cliticMarker = Pattern.Compile("^-|-$");

		private static readonly Pattern hasNum = Pattern.Compile("\\d+");

		private readonly ICollection<string> parentTagsToEscape;

		public GaleP4LexMapper()
		{
			//Buckwalter patterns
			//U+0627
			//TODO Extend coverage to entire Arabic code chart
			//Obviously Buckwalter is a lossful conversion, but no assumptions should be made about
			//UTF-8 input from "the wild"
			//Patterns to fix segmentation issues observed in the ATB
			//Tags for the canChangeEncoding() method
			parentTagsToEscape = Generics.NewHashSet();
			parentTagsToEscape.Add("PUNC");
			parentTagsToEscape.Add("LATIN");
			parentTagsToEscape.Add("-NONE-");
		}

		private string MapUtf8(string element)
		{
			//Remove diacritics
			Matcher rmDiacritics = utf8Diacritics.Matcher(element);
			element = rmDiacritics.ReplaceAll(string.Empty);
			if (element.Length > 1)
			{
				Matcher rmTatweel = utf8Tatweel.Matcher(element);
				element = rmTatweel.ReplaceAll(string.Empty);
			}
			//Normalize alef
			Matcher normAlef = utf8Alef.Matcher(element);
			element = normAlef.ReplaceAll("ا");
			//Remove characters that only appear in the Qur'an
			Matcher rmQuran = utf8Quran.Matcher(element);
			element = rmQuran.ReplaceAll(string.Empty);
			if (element.Length > 1)
			{
				Matcher rmCliticMarker = cliticMarker.Matcher(element);
				element = rmCliticMarker.ReplaceAll(string.Empty);
			}
			return element;
		}

		private string MapBuckwalter(string element)
		{
			//Remove diacritics
			Matcher rmDiacritics = bwDiacritics.Matcher(element);
			element = rmDiacritics.ReplaceAll(string.Empty);
			//Remove tatweel
			if (element.Length > 1)
			{
				Matcher rmTatweel = bwTatweel.Matcher(element);
				element = rmTatweel.ReplaceAll(string.Empty);
			}
			//Normalize alef
			Matcher normAlef = bwAlef.Matcher(element);
			element = normAlef.ReplaceAll(bwAlefChar);
			//Remove characters that only appear in the Qur'an
			Matcher rmQuran = bwQuran.Matcher(element);
			element = rmQuran.ReplaceAll(string.Empty);
			if (element.Length > 1)
			{
				Matcher rmCliticMarker = cliticMarker.Matcher(element);
				element = rmCliticMarker.ReplaceAll(string.Empty);
			}
			return element;
		}

		public virtual string Map(string parent, string element)
		{
			string elem = element.Trim();
			if (parentTagsToEscape.Contains(parent))
			{
				return elem;
			}
			Matcher utf8Encoding = utf8ArabicChart.Matcher(elem);
			return (utf8Encoding.Find()) ? MapUtf8(elem) : MapBuckwalter(elem);
		}

		public virtual void Setup(File path, params string[] options)
		{
		}

		//Whether or not the encoding of this word can be converted to another encoding
		//from its current encoding (Buckwalter or UTF-8)
		public virtual bool CanChangeEncoding(string parent, string element)
		{
			parent = parent.Trim();
			element = element.Trim();
			//Hack for LDC2008E22 idiosyncrasy
			//This is NUMERIC_COMMA in the raw trees. We allow conversion of this
			//token to UTF-8 since it would appear in this encoding in arbitrary
			//UTF-8 text input
			if (parent.Contains("NUMERIC_COMMA") || (parent.Contains("PUNC") && element.Equals("r")))
			{
				//Numeric comma
				return true;
			}
			Matcher numMatcher = hasNum.Matcher(element);
			if (numMatcher.Find() || parentTagsToEscape.Contains(parent))
			{
				return false;
			}
			return true;
		}
	}
}
