using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees.International.Arabic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic
{
	/// <summary>
	/// This class can convert between Unicode and Buckwalter encodings of
	/// Arabic.
	/// </summary>
	/// <remarks>
	/// This class can convert between Unicode and Buckwalter encodings of
	/// Arabic.
	/// <p>
	/// Sources
	/// <p>
	/// "MORPHOLOGICAL ANALYSIS &amp; POS ANNOTATION," v3.8. LDC. 08 June 2009.
	/// http://www.ldc.upenn.edu/myl/morph/buckwalter.html
	/// http://www.qamus.org/transliteration.htm (Tim Buckwalter's site)
	/// http://www.livingflowers.com/Arabic_transliteration (many but hard to use)
	/// http://www.cis.upenn.edu/~cis639/arabic/info/romanization.html
	/// http://www.nongnu.org/aramorph/english/index.html (Java AraMorph)
	/// BBN's MBuckWalter2Unicode.tab
	/// see also my GALE-NOTES.txt file for other mappings ROSETTA people do.
	/// Normalization of decomposed characters to composed:
	/// ARABIC LETTER ALEF (\u0627), ARABIC MADDAH ABOVE (\u0653) -&gt;
	/// ARABIC LETTER ALEF WITH MADDA ABOVE
	/// ARABIC LETTER ALEF (\u0627), ARABIC HAMZA ABOVE (\u0654) -&gt;
	/// ARABIC LETTER ALEF WITH HAMZA ABOVE (\u0623)
	/// ARABIC LETTER WAW, ARABIC HAMZA ABOVE -&gt;
	/// ARABIC LETTER WAW WITH HAMZA ABOVE
	/// ARABIC LETTER ALEF, ARABIC HAMZA BELOW (\u0655) -&gt;
	/// ARABIC LETTER ALEF WITH HAMZA BELOW
	/// ARABIC LETTER YEH, ARABIC HAMZA ABOVE -&gt;
	/// ARABIC LETTER YEH WITH HAMZA ABOVE
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class Buckwalter : ISerializableFunction<string, string>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Buckwalter));

		private const long serialVersionUID = 4351710914246859336L;

		/// <summary>
		/// If true (include flag "-o"), outputs space separated
		/// unicode values (e.g., "\u0621" rather than the character version of those values.
		/// </summary>
		/// <remarks>
		/// If true (include flag "-o"), outputs space separated
		/// unicode values (e.g., "\u0621" rather than the character version of those values.
		/// Only applicable for Buckwalter to Arabic conversion.
		/// </remarks>
		internal bool outputUnicodeValues = false;

		private static readonly char[] arabicChars = new char[] { '\u0621', '\u0622', '\u0623', '\u0624', '\u0625', '\u0626', '\u0627', '\u0628', '\u0629', '\u062A', '\u062B', '\u062C', '\u062D', '\u062E', '\u062F', '\u0630', '\u0631', '\u0632', '\u0633'
			, '\u0634', '\u0635', '\u0636', '\u0637', '\u0638', '\u0639', '\u063A', '\u0640', '\u0641', '\u0642', '\u0643', '\u0644', '\u0645', '\u0646', '\u0647', '\u0648', '\u0649', '\u064A', '\u064B', '\u064C', '\u064D', '\u064E', '\u064F', '\u0650'
			, '\u0651', '\u0652', '\u0670', '\u0671', '\u067E', '\u0686', '\u0698', '\u06A4', '\u06AF', '\u0625', '\u0623', '\u0624', '\u060C', '\u061B', '\u061F', '\u066A', '\u066B', '\u06F0', '\u06F1', '\u06F2', '\u06F3', '\u06F4', '\u06F5', '\u06F6'
			, '\u06F7', '\u06F8', '\u06F9', '\u0660', '\u0661', '\u0662', '\u0663', '\u0664', '\u0665', '\u0666', '\u0667', '\u0668', '\u0669', '\u00AB', '\u00BB' };

		private static readonly char[] buckChars = new char[] { '\'', '|', '>', '&', '<', '}', 'A', 'b', 'p', 't', 'v', 'j', 'H', 'x', 'd', '*', 'r', 'z', 's', '$', 'S', 'D', 'T', 'Z', 'E', 'g', '_', 'f', 'q', 'k', 'l', 'm', 'n', 'h', 'w', 'Y', 'y', 
			'F', 'N', 'K', 'a', 'u', 'i', '~', 'o', '`', '{', 'P', 'J', 'R', 'V', 'G', 'I', 'O', 'W', ',', ';', '?', '%', '.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '"', '"' };

		private bool unicode2Buckwalter = false;

		private readonly IDictionary<char, char> u2bMap;

		private readonly IDictionary<char, char> b2uMap;

		private ClassicCounter<string> unmappable;

		private static bool Debug = false;

		private const bool PassAsciiInUnicode = true;

		private static bool SuppressDigitMappingInB2a = true;

		private static bool SuppressPuncMappingInB2a = true;

		private static readonly Pattern latinPunc = Pattern.Compile("[\"\\?%,-;\\._]+");

		public Buckwalter()
		{
			// add Tim's "XML-friendly" just in case
			// from BBN script; status unknown
			// from IBM script
			//Farsi/Urdu cardinals
			// French quotes used in e.g. Gulf newswire
			// end 062x
			// end 063x
			// end 0064x
			// U+0698 is Farsi Jeh: R to ATB POS guidelines
			// add Tim's "XML-friendly" versions just in case
			// from BBN script; status unknown
			// from IBM script
			// French quotes used in e.g. Gulf newswire
			/* BBN also maps to @: 0x007B 0x066C 0x066D 0x0660 0x0661 0x0662 0x0663
			0x0664 0x0665 0x0666 0x0667 0x0668 0x0669 0x066A
			0x0686 0x06AF 0x066D 0x06AF 0x06AA 0x06AB 0x06B1
			0x06F0 0x06EC 0x06DF 0x06DF 0x06F4 0x002A 0x274A
			0x00E9 0x00C9 0x00AB 0x00BB 0x00A0 0x00A4
			*/
			/* BBNWalter dispreferring punct chars:
			'\u0624', '\u0625', '\u0626',  -> 'L', 'M', 'Q',
			'\u0630', -> 'C', '\u0640', -> '@', '\u0651', -> 'B',
			*/
			/* IBM also deletes: 654 655 670 */
			//wsg: I have included _ in this list, which actually maps to tatweel.
			//In practice we strip tatweel as part of orthographic normalization,
			//so any instances of _ in the Buckwalter should actually be treated as
			//punctuation.
			if (arabicChars.Length != buckChars.Length)
			{
				throw new Exception(this.GetType().FullName + ": Inconsistent u2b/b2u arrays.");
			}
			u2bMap = Generics.NewHashMap(arabicChars.Length);
			b2uMap = Generics.NewHashMap(buckChars.Length);
			for (int i = 0; i < arabicChars.Length; i++)
			{
				char charU = char.ValueOf(arabicChars[i]);
				char charB = char.ValueOf(buckChars[i]);
				u2bMap[charU] = charB;
				b2uMap[charB] = charU;
			}
			if (Debug)
			{
				unmappable = new ClassicCounter<string>();
			}
		}

		public Buckwalter(bool unicodeToBuckwalter)
			: this()
		{
			unicode2Buckwalter = unicodeToBuckwalter;
		}

		public virtual void SuppressBuckDigitConversion(bool b)
		{
			SuppressDigitMappingInB2a = b;
		}

		public virtual void SuppressBuckPunctConversion(bool b)
		{
			SuppressPuncMappingInB2a = b;
		}

		public virtual string Apply(string @in)
		{
			return Convert(@in, unicode2Buckwalter);
		}

		public virtual string BuckwalterToUnicode(string @in)
		{
			return Convert(@in, false);
		}

		public virtual string UnicodeToBuckwalter(string @in)
		{
			return Convert(@in, true);
		}

		private string Convert(string @in, bool unicodeToBuckwalter)
		{
			StringTokenizer st = new StringTokenizer(@in);
			StringBuilder result = new StringBuilder(@in.Length);
			while (st.HasMoreTokens())
			{
				string token = st.NextToken();
				for (int i = 0; i < token.Length; i++)
				{
					if (ATBTreeUtils.reservedWords.Contains(token))
					{
						result.Append(token);
						break;
					}
					char inCh = char.ValueOf(token[i]);
					char outCh = null;
					if (unicodeToBuckwalter)
					{
						outCh = (PassAsciiInUnicode && inCh < 127) ? inCh : u2bMap[inCh];
					}
					else
					{
						if ((SuppressDigitMappingInB2a && char.IsDigit((char)inCh)) || (SuppressPuncMappingInB2a && latinPunc.Matcher(inCh.ToString()).Matches()))
						{
							outCh = inCh;
						}
						else
						{
							outCh = b2uMap[inCh];
						}
					}
					if (outCh == null)
					{
						if (Debug)
						{
							string key = inCh + "[U+" + StringUtils.PadLeft(int.ToString(inCh, 16).ToUpper(), 4, '0') + ']';
							unmappable.IncrementCount(key);
						}
						result.Append(inCh);
					}
					else
					{
						// pass through char
						if (outputUnicodeValues)
						{
							result.Append("\\u").Append(StringUtils.PadLeft(int.ToString(inCh, 16).ToUpper(), 4, '0'));
						}
						else
						{
							result.Append(outCh);
						}
					}
				}
				result.Append(" ");
			}
			return result.ToString().Trim();
		}

		private static readonly StringBuilder usage = new StringBuilder();

		static Buckwalter()
		{
			usage.Append("Usage: java Buckwalter [OPTS] file   (or < file)\n");
			usage.Append("Options:\n");
			usage.Append("          -u2b : Unicode -> Buckwalter (default is Buckwalter -> Unicode).\n");
			usage.Append("          -d   : Debug mode.\n");
			usage.Append("          -o   : Output unicode values.\n");
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			bool unicodeToBuck = false;
			bool outputUnicodeValues = false;
			File inputFile = null;
			foreach (string arg in args)
			{
				if (arg.StartsWith("-"))
				{
					switch (arg)
					{
						case "-u2b":
						{
							unicodeToBuck = true;
							break;
						}

						case "-o":
						{
							outputUnicodeValues = true;
							break;
						}

						case "-d":
						{
							Debug = true;
							break;
						}

						default:
						{
							System.Console.Out.WriteLine(usage.ToString());
							return;
						}
					}
				}
				else
				{
					inputFile = new File(arg);
					break;
				}
			}
			Edu.Stanford.Nlp.International.Arabic.Buckwalter b = new Edu.Stanford.Nlp.International.Arabic.Buckwalter(unicodeToBuck);
			b.outputUnicodeValues = outputUnicodeValues;
			int j = (b.outputUnicodeValues ? 2 : int.MaxValue);
			if (j < args.Length)
			{
				for (; j < args.Length; j++)
				{
					EncodingPrintWriter.Out.Println(args[j] + " -> " + b.Apply(args[j]), "utf-8");
				}
			}
			else
			{
				int numLines = 0;
				try
				{
					BufferedReader br = (inputFile == null) ? new BufferedReader(new InputStreamReader(Runtime.@in, "utf-8")) : new BufferedReader(new InputStreamReader(new FileInputStream(inputFile), "utf-8"));
					System.Console.Error.Printf("Reading input...");
					string line;
					while ((line = br.ReadLine()) != null)
					{
						EncodingPrintWriter.Out.Println(b.Apply(line), "utf-8");
						numLines++;
					}
					br.Close();
					System.Console.Error.Printf("done.\nConverted %d lines from %s.\n", numLines, (unicodeToBuck ? "UTF-8 to Buckwalter" : "Buckwalter to UTF-8"));
				}
				catch (UnsupportedEncodingException)
				{
					log.Error("File system does not support UTF-8 encoding.");
				}
				catch (FileNotFoundException)
				{
					log.Error("File does not exist: " + inputFile.GetPath());
				}
				catch (IOException)
				{
					System.Console.Error.Printf("ERROR: IO exception while reading file (line %d).\n", numLines);
				}
			}
			if (Debug)
			{
				if (!b.unmappable.KeySet().IsEmpty())
				{
					EncodingPrintWriter.Err.Println("Characters that could not be converted [passed through!]:", "utf-8");
					EncodingPrintWriter.Err.Println(b.unmappable.ToString(), "utf-8");
				}
				else
				{
					EncodingPrintWriter.Err.Println("All characters successfully converted!", "utf-8");
				}
			}
		}
	}
}
