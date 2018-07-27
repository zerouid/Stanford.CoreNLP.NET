using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Trees.International.Arabic
{
	/// <summary>This class contains tools for dealing with arabic text, in particular conversion to IBM normalized Arabic.</summary>
	/// <remarks>
	/// This class contains tools for dealing with arabic text, in particular conversion to IBM normalized Arabic.
	/// The code was adapted to java from the perl script ar_normalize_v5.pl
	/// </remarks>
	/// <author>Alex Kleeman</author>
	public class ArabicUtils
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ArabicUtils));

		public static IDictionary<string, string> PresToLogicalMap()
		{
			IDictionary<string, string> rules = Generics.NewHashMap();
			// PRESENTATION FORM TO LOGICAL FORM NORMALIZATION (presentation form is rarely used - but some UN documents have it).
			rules["\\ufc5e"] = "\u0020\u064c\u0651";
			// ligature shadda with dammatan isloated
			rules["\\ufc5f"] = "\u0020\u064d\u0651";
			// ligature shadda with kasratan isloated
			rules["\\ufc60"] = "\u0020\u064e\u0651";
			// ligature shadda with fatha isloated
			rules["\\ufc61"] = "\u0020\u064f\u0651";
			// ligature shadda with damma isloated
			rules["\\ufc62"] = "\u0020\u0650\u0651";
			// ligature shadda with kasra isloated
			// Arabic Presentation Form-B to Logical Form
			rules["\\ufe80"] = "\u0621";
			// isolated hamza
			rules["[\\ufe81\\ufe82]"] = "\u0622";
			// alef with madda
			rules["[\\ufe83\\ufe84]"] = "\u0623";
			// alef with hamza above
			rules["[\\ufe85\\ufe86]"] = "\u0624";
			// waw with hamza above
			rules["[\\ufe87\\ufe88]"] = "\u0625";
			// alef with hamza below
			rules["[\\ufe89\\ufe8a\\ufe8b\\ufe8c]"] = "\u0626";
			// yeh with hamza above
			rules["[\\ufe8d\\ufe8e]"] = "\u0627";
			// alef
			rules["[\\ufe8f\\ufe90\\ufe91\\ufe92]"] = "\u0628";
			// beh
			rules["[\\ufe93\\ufe94]"] = "\u0629";
			// teh marbuta
			rules["[\\ufe95\\ufe96\\ufe97\\ufe98]"] = "\u062a";
			// teh
			rules["[\\ufe99\\ufe9a\\ufe9b\\ufe9c]"] = "\u062b";
			// theh
			rules["[\\ufe9d\\ufe9e\\ufe9f\\ufea0]"] = "\u062c";
			// jeem
			rules["[\\ufea1\\ufea2\\ufea3\\ufea4]"] = "\u062d";
			// haa
			rules["[\\ufea5\\ufea6\\ufea7\\ufea8]"] = "\u062e";
			// khaa
			rules["[\\ufea9\\ufeaa]"] = "\u062f";
			// dal
			rules["[\\ufeab\\ufeac]"] = "\u0630";
			// dhal
			rules["[\\ufead\\ufeae]"] = "\u0631";
			// reh
			rules["[\\ufeaf\\ufeb0]"] = "\u0632";
			// zain
			rules["[\\ufeb1\\ufeb2\\ufeb3\\ufeb4]"] = "\u0633";
			// seen
			rules["[\\ufeb5\\ufeb6\\ufeb7\\ufeb8]"] = "\u0634";
			// sheen
			rules["[\\ufeb9\\ufeba\\ufebb\\ufebc]"] = "\u0635";
			// sad
			rules["[\\ufebd\\ufebe\\ufebf\\ufec0]"] = "\u0636";
			// dad
			rules["[\\ufec1\\ufec2\\ufec3\\ufec4]"] = "\u0637";
			// tah
			rules["[\\ufec5\\ufec6\\ufec7\\ufec8]"] = "\u0638";
			// zah
			rules["[\\ufec9\\ufeca\\ufecb\\ufecc]"] = "\u0639";
			// ain
			rules["[\\ufecd\\ufece\\ufecf\\ufed0]"] = "\u063a";
			// ghain
			rules["[\\ufed1\\ufed2\\ufed3\\ufed4]"] = "\u0641";
			// feh
			rules["[\\ufed5\\ufed6\\ufed7\\ufed8]"] = "\u0642";
			// qaf
			rules["[\\ufed9\\ufeda\\ufedb\\ufedc]"] = "\u0643";
			// kaf
			rules["[\\ufedd\\ufede\\ufedf\\ufee0]"] = "\u0644";
			// ghain
			rules["[\\ufee1\\ufee2\\ufee3\\ufee4]"] = "\u0645";
			// meem
			rules["[\\ufee5\\ufee6\\ufee7\\ufee8]"] = "\u0646";
			// noon
			rules["[\\ufee9\\ufeea\\ufeeb\\ufeec]"] = "\u0647";
			// heh
			rules["[\\ufeed\\ufeee]"] = "\u0648";
			// waw
			rules["[\\ufeef\\ufef0]"] = "\u0649";
			// alef maksura
			rules["[\\ufef1\\ufef2\\ufef3\\ufef4]"] = "\u064a";
			// yeh
			rules["[\\ufef5\\ufef6]"] = "\u0644\u0622";
			// ligature: lam and alef with madda above
			rules["[\\ufef7\\ufef8]"] = "\u0644\u0623";
			// ligature: lam and alef with hamza above
			rules["[\\ufef9\\ufefa]"] = "\u0644\u0625";
			// ligature: lam and alef with hamza below
			rules["[\\ufefb\\ufefc]"] = "\u0644\u0627";
			// ligature: lam and alef
			return rules;
		}

		public static IDictionary<string, string> GetArabicIBMNormalizerMap()
		{
			IDictionary<string, string> rules = Generics.NewHashMap();
			try
			{
				rules["[\\u0622\\u0623\\u0625]"] = "\u0627";
				// hamza normalization: maddah-n-alef, hamza-on-alef, hamza-under-alef mapped to bare alef
				rules["[\\u0649]"] = "\u064A";
				// 'alif maqSuura mapped to yaa
				rules["[\\u064B\\u064C\\u064D\\u064E\\u064F\\u0650\\u0651\\u0652\\u0653\\u0670]"] = string.Empty;
				//  fatHatayn, Dammatayn, kasratayn, fatHa, Damma, kasra, shaddah, sukuun, and dagger alef (delete)
				rules["\\u0640(?=\\s*\\S)"] = string.Empty;
				// tatweel, delete except when trailing
				rules["(\\S)\\u0640"] = "$1";
				// tatweel, delete if preceeded by non-white-space
				rules["[\\ufeff\\u00a0]"] = " ";
				// white space normalization
				// punctuation normalization
				rules["\\u060c"] = ",";
				// Arabic comma
				rules["\\u061b"] = ";";
				// Arabic semicolon
				rules["\\u061f"] = "?";
				// Arabic question mark
				rules["\\u066a"] = "%";
				// Arabic percent sign
				rules["\\u066b"] = ".";
				// Arabic decimal separator
				rules["\\u066c"] = ",";
				// Arabic thousand separator (comma)
				rules["\\u066d"] = "*";
				// Arabic asterisk
				rules["\\u06d4"] = ".";
				// Arabic full stop
				// Arabic/Arabic indic/eastern Arabic/ digits normalization
				rules["[\\u0660\\u06f0\\u0966]"] = "0";
				rules["[\\u0661\\u06f1\\u0967]"] = "1";
				rules["[\\u0662\\u06f2\\u0968]"] = "2";
				rules["[\\u0663\\u06f3\\u0969]"] = "3";
				rules["[\\u0664\\u06f4\\u096a]"] = "4";
				rules["[\\u0665\\u06f5\\u096b]"] = "5";
				rules["[\\u0666\\u06f6\\u096c]"] = "6";
				rules["[\\u0667\\u06f7\\u096d]"] = "7";
				rules["[\\u0668\\u06f8\\u096e]"] = "8";
				rules["[\\u0669\\u06f9\\u096f]"] = "9";
				// Arabic combining hamza above/below and dagger(superscript)  alef
				rules["[\\u0654\\u0655\\u0670]"] = string.Empty;
				// replace yaa followed by hamza with hamza on kursi (yaa)
				rules["\\u064A\\u0621"] = "\u0626";
				// Normalization Rules Suggested by Ralf Brown (CMU):
				rules["\\u2013"] = "-";
				// EN-dash to ASCII hyphen
				rules["\\u2014"] = "--";
				// EM-dash to double ASII hyphen
				// code point 0x91 - latin-1 left single quote
				// code point 0x92 - latin-1 right single quote
				// code point 0x2018 = left single quote; convert to ASCII single quote
				// code point 0x2019 = right single quote; convert to ASCII single quote
				rules["[\\u0091\\u0092\\u2018\\u2019]"] = "\'";
				// code point 0x93 - latin-1 left double quote
				// code point 0x94 - latin-1 right double quote
				// code points 0x201C/201D = left/right double quote -> ASCII double quote
				rules["[\\u0093\\u0094\\u201C\\u201D]"] = "\"";
			}
			catch (Exception e)
			{
				log.Info("Caught exception creating Arabic normalizer map: " + e.ToString());
			}
			return rules;
		}

		/// <summary>
		/// This will normalize a Unicode String by applying all the normalization rules from the IBM normalization and
		/// conversion from Presentation to Logical from.
		/// </summary>
		/// <param name="in">The String to be normalized</param>
		public static string Normalize(string @in)
		{
			IDictionary<string, string> ruleMap = GetArabicIBMNormalizerMap();
			//Get the IBM Normalization rules
			ruleMap.PutAll(PresToLogicalMap());
			//  Get the presentation to logical form rules
			ICollection<KeyValuePair<string, string>> rules = ruleMap;
			IEnumerator<KeyValuePair<string, string>> ruleIter = rules.GetEnumerator();
			string @out = @in;
			//Iteratively apply each rule to the string.
			while (ruleIter.MoveNext())
			{
				KeyValuePair<string, string> thisRule = ruleIter.Current;
				@out = @out.ReplaceAll(thisRule.Key, thisRule.Value);
			}
			return @out;
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Properties p = StringUtils.ArgsToProperties(args);
			if (p.Contains("input"))
			{
				FileInputStream fis = new FileInputStream(p.GetProperty("input"));
				InputStreamReader isr = new InputStreamReader(fis, "UTF-8");
				BufferedReader reader = new BufferedReader(isr);
				string thisLine;
				while ((thisLine = reader.ReadLine()) != null)
				{
					EncodingPrintWriter.Out.Println(Normalize(thisLine), "UTF-8");
				}
			}
		}
	}
}
