using System.Collections.Generic;
using Edu.Stanford.Nlp.International.Arabic;
using Edu.Stanford.Nlp.Trees.International.Arabic;
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
	[System.Serializable]
	public class DefaultLexicalMapper : IMapper
	{
		private const long serialVersionUID = -3798804368296999785L;

		private readonly Pattern utf8ArabicChart = Pattern.Compile("[\u0600-\u06FF]");

		private readonly string bwAlefChar = "A";

		private readonly Pattern bwDiacritics = Pattern.Compile("F|N|K|a|u|i|\\~|o");

		private readonly Pattern bwTatweel = Pattern.Compile("_");

		private readonly Pattern bwAlef = Pattern.Compile("\\{|\\||>|<");

		private readonly Pattern bwQuran = Pattern.Compile("`");

		private readonly Pattern bwNullAnaphoraMarker = Pattern.Compile("\\[nll\\]");

		public readonly Pattern latinPunc = Pattern.Compile("([\u0021-\u002F\u003A-\u0040\\u005B-\u0060\u007B-\u007E\u00A1-\u00BF\u00F7\u2010-\u2027\u2030-\u205E\u20A0-\u20BA])+");

		public readonly Pattern arabicPunc = Pattern.Compile("([\u00AB\u00BB\u0609-\u060D\u061B-\u061F\u066A\u066C-\u066D\u06D4])+");

		public readonly Pattern arabicDigit = Pattern.Compile("([\u06F0-\u06F9\u0660-\u0669])+");

		private readonly Pattern utf8Diacritics = Pattern.Compile("َ|ً|ُ|ٌ|ِ|ٍ|ّ|ْ|\u0670");

		private readonly Pattern utf8Tatweel = Pattern.Compile("ـ");

		private readonly Pattern utf8Alef = Pattern.Compile("ا|إ|أ|آ|\u0671");

		private readonly Pattern utf8Quran = Pattern.Compile("[\u0615-\u061A\u06D6-\u06E5]");

		private readonly Pattern utf8ProDrop = Pattern.Compile("\\[نلل\\]");

		public readonly Pattern segmentationMarker = Pattern.Compile("^-+|-+$");

		private readonly Pattern morphemeBoundary = Pattern.Compile("\\+");

		private readonly Pattern hasDigit = Pattern.Compile("\\d+");

		private bool useATBVocalizedSectionMapping = false;

		private bool stripMorphemeMarkersInUTF8 = false;

		private bool stripSegmentationMarkersInUTF8 = false;

		private readonly string parentTagString = "PUNC LATIN -NONE-";

		private readonly ICollection<string> parentTagsToEscape;

		private readonly string utf8CliticString = "ل ف و ما ه ها هم هن نا كم تن تم ى ي هما ك ب م";

		private readonly ICollection<string> bwClitics;

		public DefaultLexicalMapper()
		{
			//Buckwalter patterns
			//U+0627
			//TODO Extend coverage to entire Arabic code chart
			//Obviously Buckwalter is a lossful conversion, but no assumptions should be made about
			//UTF-8 input from "the wild"
			//Patterns to fix segmentation issues observed in the ATB
			// Process the vocalized section for parsing
			// Strip morpheme boundary markers in the vocalized section
			// Strip all morpheme and segmentation markers in UTF-8 Arabic
			//wsg: "LATIN" does not appear in the Bies tagset, so be sure to pass
			//in the extended POS tags during normalization
			//  private final Set<String> utf8Clitics;
			parentTagsToEscape = Java.Util.Collections.UnmodifiableSet(Generics.NewHashSet(Arrays.AsList(parentTagString.Split("\\s+"))));
			//    utf8Clitics =
			//      Collections.unmodifiableSet(Generics.newHashSet(Arrays.asList(utf8CliticString.split("\\s+"))));
			Buckwalter bw = new Buckwalter(true);
			string bwString = bw.Apply(utf8CliticString);
			bwClitics = Java.Util.Collections.UnmodifiableSet(Generics.NewHashSet(Arrays.AsList(bwString.Split("\\s+"))));
		}

		private string MapUtf8(string element)
		{
			Matcher latinPuncOnly = latinPunc.Matcher(element);
			Matcher arbPuncOnly = arabicPunc.Matcher(element);
			if (latinPuncOnly.Matches() || arbPuncOnly.Matches())
			{
				return element;
			}
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
			Matcher rmProDrop = utf8ProDrop.Matcher(element);
			element = rmProDrop.ReplaceAll(string.Empty);
			if (stripMorphemeMarkersInUTF8)
			{
				Matcher rmMorphemeBoundary = morphemeBoundary.Matcher(element);
				string strippedElem = rmMorphemeBoundary.ReplaceAll(string.Empty);
				if (strippedElem.Length > 0)
				{
					element = strippedElem;
				}
			}
			if (stripSegmentationMarkersInUTF8)
			{
				string strippedElem = segmentationMarker.Matcher(element).ReplaceAll(string.Empty);
				if (strippedElem.Length > 0)
				{
					element = strippedElem;
				}
			}
			return element;
		}

		private string MapBuckwalter(string element)
		{
			Matcher puncOnly = latinPunc.Matcher(element);
			if (puncOnly.Matches())
			{
				return element;
			}
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
			Matcher rmProDrop = bwNullAnaphoraMarker.Matcher(element);
			element = rmProDrop.ReplaceAll(string.Empty);
			// This conditional is used for normalizing raw ATB trees
			// Morpheme boundaries are removed, and segmentation markers are retained on
			// segmented morphemes (not the tokens to which the morphemes were attached)
			if (useATBVocalizedSectionMapping && element.Length > 1)
			{
				Matcher rmMorphemeBoundary = morphemeBoundary.Matcher(element);
				element = rmMorphemeBoundary.ReplaceAll(string.Empty);
				//wsg: This is hairy due to tokens like this in the vocalized section:
				//        layos-+-a
				Matcher cliticMarker = segmentationMarker.Matcher(element);
				if (cliticMarker.Find() && !hasDigit.Matcher(element).Find())
				{
					string strippedElem = cliticMarker.ReplaceAll(string.Empty);
					if (strippedElem.Length > 0)
					{
						element = bwClitics.Contains(strippedElem) ? element : strippedElem;
					}
				}
			}
			else
			{
				if (element.Length > 1 && !ATBTreeUtils.reservedWords.Contains(element))
				{
					Matcher rmCliticMarker = segmentationMarker.Matcher(element);
					element = rmCliticMarker.ReplaceAll(string.Empty);
				}
			}
			return element;
		}

		public virtual string Map(string parent, string element)
		{
			string elem = element.Trim();
			if (parent != null && parentTagsToEscape.Contains(parent))
			{
				return elem;
			}
			Matcher utf8Encoding = utf8ArabicChart.Matcher(elem);
			return (utf8Encoding.Find()) ? MapUtf8(elem) : MapBuckwalter(elem);
		}

		public virtual void Setup(File path, params string[] options)
		{
			if (options == null)
			{
				return;
			}
			foreach (string opt in options)
			{
				switch (opt)
				{
					case "ATBVocalizedSection":
					{
						useATBVocalizedSectionMapping = true;
						break;
					}

					case "StripSegMarkersInUTF8":
					{
						stripSegmentationMarkersInUTF8 = true;
						break;
					}

					case "StripMorphMarkersInUTF8":
					{
						stripMorphemeMarkersInUTF8 = true;
						break;
					}
				}
			}
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
			Matcher numMatcher = hasDigit.Matcher(element);
			return !(numMatcher.Find() || parentTagsToEscape.Contains(parent));
		}

		public static void Main(string[] args)
		{
			IMapper m = new Edu.Stanford.Nlp.International.Arabic.Pipeline.DefaultLexicalMapper();
			System.Console.Out.Printf("< :-> %s\n", m.Map(null, "FNKqq"));
		}
	}
}
