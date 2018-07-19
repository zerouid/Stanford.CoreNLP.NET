using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util.Function;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Takes a HasWord or String and returns an Americanized version of it.</summary>
	/// <remarks>
	/// Takes a HasWord or String and returns an Americanized version of it.
	/// Optionally, it does some month/day name normalization to capitalized.
	/// This is deterministic spelling conversion, and so cannot deal with
	/// certain cases involving complex ambiguities, but it can do most of the
	/// simple case of English to American conversion.
	/// <p>
	/// <i>This list is still quite incomplete, but does some of the
	/// most common cases found when running our parser or doing biomedical
	/// processing. to expand this list, we should probably look at:</i>
	/// <c>http://wordlist.sourceforge.net/</c>
	/// or
	/// <c>http://home.comcast.net/~helenajole/Harry.html</c>
	/// .
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class Americanize : IFunction<IHasWord, IHasWord>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Process.Americanize));

		/// <summary>Whether to capitalize month and day names.</summary>
		/// <remarks>Whether to capitalize month and day names. The default is true.</remarks>
		private readonly bool capitalizeTimex;

		public const int DontCapitalizeTimex = unchecked((int)(0x1));

		/// <summary>No word shorter in length than this is changed by Americanize</summary>
		private const int MinimumLengthChanged = 4;

		/// <summary>No word shorter in length than this can match a Pattern</summary>
		private const int MinimumLengthPatternMatch = 6;

		public Americanize()
			: this(0)
		{
		}

		/// <summary>Make an object for Americanizing spelling.</summary>
		/// <param name="flags">
		/// An integer representing bit flags. At present the only
		/// recognized flag is DONT_CAPITALIZE_TIMEX = 1 which suppresses
		/// capitalization of days of the week and months.
		/// </param>
		public Americanize(int flags)
		{
			capitalizeTimex = (flags & DontCapitalizeTimex) == 0;
		}

		/// <summary>Americanize the HasWord or String coming in.</summary>
		/// <param name="w">A HasWord or String to covert to American if needed.</param>
		/// <returns>Either the input or an Americanized version of it.</returns>
		public virtual IHasWord Apply(IHasWord w)
		{
			string str = w.Word();
			string outStr = Americanize(str, capitalizeTimex);
			if (!outStr.Equals(str))
			{
				w.SetWord(outStr);
			}
			return w;
		}

		/// <summary>Convert the spelling of a word from British to American English.</summary>
		/// <remarks>
		/// Convert the spelling of a word from British to American English.
		/// This is deterministic spelling conversion, and so cannot deal with
		/// certain cases involving complex ambiguities, but it can do most of the
		/// simple cases of English to American conversion. Month and day names will
		/// be capitalized unless you have changed the default setting.
		/// </remarks>
		/// <param name="str">The String to be Americanized</param>
		/// <returns>The American spelling of the word.</returns>
		public static string Americanize(string str)
		{
			return Americanize(str, true);
		}

		/// <summary>Convert the spelling of a word from British to American English.</summary>
		/// <remarks>
		/// Convert the spelling of a word from British to American English.
		/// This is deterministic spelling conversion, and so cannot deal with
		/// certain cases involving complex ambiguities, but it can do most of the
		/// simple cases of English to American conversion.
		/// </remarks>
		/// <param name="str">The String to be Americanized</param>
		/// <param name="capitalizeTimex">Whether to capitalize time expressions like month names in return value</param>
		/// <returns>The American spelling of the word.</returns>
		public static string Americanize(string str, bool capitalizeTimex)
		{
			// log.info("str is |" + str + "|");
			// log.info("timexMapping.contains is " +
			//            timexMapping.containsKey(str));
			// No ver short words are changed, so short circuit them
			int length = str.Length;
			if (length < MinimumLengthChanged)
			{
				return str;
			}
			string result;
			if (capitalizeTimex)
			{
				result = timexMapping[str];
				if (result != null)
				{
					return result;
				}
			}
			result = mapping[str];
			if (result != null)
			{
				return result;
			}
			if (length < MinimumLengthPatternMatch)
			{
				return str;
			}
			// first do one disjunctive regex and return unless matches. Faster!
			// (But still allocates matcher each time; avoiding this would make this class not threadsafe....)
			if (!disjunctivePattern.Matcher(str).Find())
			{
				return str;
			}
			for (int i = 0; i < pats.Length; i++)
			{
				Matcher m = pats[i].Matcher(str);
				if (m.Find())
				{
					Pattern ex = excepts[i];
					if (ex != null)
					{
						Matcher me = ex.Matcher(str);
						if (me.Find())
						{
							continue;
						}
					}
					// log.info("Replacing " + word + " with " +
					//             pats[i].matcher(word).replaceAll(reps[i]));
					return m.ReplaceAll(reps[i]);
				}
			}
			return str;
		}

		private static readonly string[] patStrings = new string[] { "haem(at)?o", "aemia$", "([lL])eukaem", "programme(s?)$", "^([a-z]{3,})our(s?)$" };

		private static readonly Pattern[] pats = new Pattern[patStrings.Length];

		private static readonly Pattern disjunctivePattern;

		static Americanize()
		{
			StringBuilder foo = new StringBuilder();
			for (int i = 0; i < len; i++)
			{
				pats[i] = Pattern.Compile(patStrings[i]);
				if (i > 0)
				{
					foo.Append('|');
				}
				foo.Append("(?:");
				// Remove groups from String before appending for speed
				foo.Append(patStrings[i].ReplaceAll("[()]", string.Empty));
				foo.Append(')');
			}
			disjunctivePattern = Pattern.Compile(foo.ToString());
		}

		private static readonly string[] OurExceptions = new string[] { "abatjour", "beflour", "bonjour", "calambour", "carrefour", "cornflour", "contour", "de[tv]our", "dortour", "dyvour", "downpour", "giaour", "glamour", "holour", "inpour", "outpour"
			, "pandour", "paramour", "pompadour", "recontour", "repour", "ryeflour", "sompnour", "tambour", "troubadour", "tregetour", "velour" };

		private static readonly Pattern[] excepts = new Pattern[] { null, null, null, null, Pattern.Compile(StringUtils.Join(OurExceptions, "|")) };

		private static readonly string[] reps = new string[] { "hem$1o", "emia", "$1eukem", "program$1", "$1or$2" };

		/// <summary>
		/// Do some normalization and British -&gt; American mapping!
		/// Notes:
		/// <ul>
		/// <li>in PTB, you get dialogue not dialog, 17 times to 1.
		/// </summary>
		/// <remarks>
		/// Do some normalization and British -&gt; American mapping!
		/// Notes:
		/// <ul>
		/// <li>in PTB, you get dialogue not dialog, 17 times to 1.
		/// <li>We don't in general deal with capitalized words, only a couple of cases like Labour, Defence for the department.
		/// </ul>
		/// </remarks>
		private static readonly string[] converters = new string[] { "anaesthetic", "analogue", "analogues", "analyse", "analysed", "analysing", "armoured", "cancelled", "cancelling", "capitalise", "capitalised", "capitalisation", "centre", "chimaeric"
			, "coloured", "colouring", "colourful", "defence", "Defence", "discoloured", "discolouring", "encyclopaedia", "endeavoured", "endeavouring", "favoured", "favouring", "favourite", "favourites", "fibre", "fibres", "finalise", "finalised", "finalising"
			, "flavoured", "flavouring", "grey", "homologue", "homologues", "honoured", "honouring", "honourable", "humoured", "humouring", "kerb", "labelled", "labelling", "Labour", "laboured", "labouring", "leant", "learnt", "localise", "localised", 
			"manoeuvre", "manoeuvres", "maximise", "maximised", "maximising", "meagre", "minimise", "minimised", "minimising", "modernise", "modernised", "modernising", "neighbourhood", "neighbourhoods", "oestrogen", "oestrogens", "organisation", "organisations"
			, "penalise", "penalised", "popularise", "popularised", "popularises", "popularising", "practise", "practised", "pressurise", "pressurised", "pressurises", "pressurising", "realise", "realised", "realising", "realises", "recognise", "recognised"
			, "recognising", "recognises", "rumoured", "rumouring", "savoured", "savouring", "theatre", "theatres", "titre", "titres", "travelled", "travelling" };

		private static readonly string[] converted = new string[] { "anesthetic", "analog", "analogs", "analyze", "analyzed", "analyzing", "armored", "canceled", "canceling", "capitalize", "capitalized", "capitalization", "center", "chimeric", "colored"
			, "coloring", "colorful", "defense", "Defense", "discolored", "discoloring", "encyclopedia", "endeavored", "endeavoring", "favored", "favoring", "favorite", "favorites", "fiber", "fibers", "finalize", "finalized", "finalizing", "flavored", 
			"flavoring", "gray", "homolog", "homologs", "honored", "honoring", "honorable", "humored", "humoring", "curb", "labeled", "labeling", "Labor", "labored", "laboring", "leaned", "learned", "localize", "localized", "maneuver", "maneuvers", "maximize"
			, "maximized", "maximizing", "meager", "minimize", "minimized", "minimizing", "modernize", "modernized", "modernizing", "neighborhood", "neighborhoods", "estrogen", "estrogens", "organization", "organizations", "penalize", "penalized", "popularize"
			, "popularized", "popularizes", "popularizing", "practice", "practiced", "pressurize", "pressurized", "pressurizes", "pressurizing", "realize", "realized", "realizing", "realizes", "recognize", "recognized", "recognizing", "recognizes", "rumored"
			, "rumoring", "savored", "savoring", "theater", "theaters", "titer", "titers", "traveled", "traveling" };

		private static readonly string[] timexConverters = new string[] { "january", "february", "april", "june", "july", "august", "september", "october", "november", "december", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"
			 };

		private static readonly string[] timexConverted = new string[] { "January", "February", "April", "June", "July", "August", "September", "October", "November", "December", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
			 };

		private static readonly IDictionary<string, string> mapping = Generics.NewHashMap();

		private static readonly IDictionary<string, string> timexMapping = Generics.NewHashMap();

		static Americanize()
		{
			/* not analyses NNS */
			/* "dialogue", "dialogues", */
			/* "dialog", "dialogs", */
			/* not "march" ! */
			/* Not "may"! */
			/* not "march" ! */
			/* Not "may"! */
			// static initialization block
			//noinspection ConstantConditions
			if (converters.Length != converted.Length || timexConverters.Length != timexConverted.Length || pats.Length != reps.Length || pats.Length != excepts.Length)
			{
				throw new Exception("Americanize: Bad initialization data");
			}
			for (int i = 0; i < converters.Length; i++)
			{
				mapping[converters[i]] = converted[i];
			}
			for (int i_1 = 0; i_1 < timexConverters.Length; i_1++)
			{
				timexMapping[timexConverters[i_1]] = timexConverted[i_1];
			}
		}

		public override string ToString()
		{
			return ("Americanize[capitalizeTimex is " + capitalizeTimex + "; " + "mapping has " + mapping.Count + " mappings; " + "timexMapping has " + timexMapping.Count + " mappings]");
		}

		/// <summary>Americanize and print the command line arguments.</summary>
		/// <remarks>
		/// Americanize and print the command line arguments.
		/// This main method is just for debugging.
		/// </remarks>
		/// <param name="args">Command line arguments: a list of words</param>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			log.Info(new Edu.Stanford.Nlp.Process.Americanize());
			if (args.Length == 0)
			{
				// stdin -> stdout:
				using (BufferedReader buf = new BufferedReader(new InputStreamReader(Runtime.@in)))
				{
					for (string line; (line = buf.ReadLine()) != null; )
					{
						foreach (string w in line.Split("\\s+"))
						{
							System.Console.Out.Write(Edu.Stanford.Nlp.Process.Americanize.Americanize(w));
							System.Console.Out.Write(' ');
						}
						System.Console.Out.WriteLine();
					}
				}
			}
			foreach (string arg in args)
			{
				System.Console.Out.Write(arg);
				System.Console.Out.Write(" --> ");
				System.Console.Out.WriteLine(Americanize(arg));
			}
		}
	}
}
