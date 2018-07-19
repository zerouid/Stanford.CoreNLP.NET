using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Spanish
{
	/// <summary>
	/// Provides a utility function for removing attached pronouns from
	/// Spanish verb forms.
	/// </summary>
	/// <author>Jon Gauthier</author>
	/// <author>Ishita Prasad</author>
	[System.Serializable]
	public sealed class SpanishVerbStripper
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Spanish.SpanishVerbStripper));

		/// <summary>A struct describing the result of verb stripping.</summary>
		public class StrippedVerb
		{
			private string stem;

			private string originalStem;

			private IList<string> pronouns;

			public StrippedVerb(string originalStem, IList<string> pronouns)
			{
				// The following three classes of verb forms can carry attached
				// pronouns:
				//
				//   - Infinitives
				//   - Gerunds
				//   - Affirmative imperatives
				this.originalStem = originalStem;
				this.pronouns = pronouns;
			}

			public virtual void SetStem(string stem)
			{
				this.stem = stem;
			}

			/// <summary>
			/// Return the normalized stem of the verb -- the way it would appear in
			/// isolation without attached pronouns.
			/// </summary>
			/// <remarks>
			/// Return the normalized stem of the verb -- the way it would appear in
			/// isolation without attached pronouns.
			/// Here are example mappings from original verb to normalized stem:
			/// <ul>
			/// <li>sentaos -&gt; sentad</li>
			/// <li>vámonos -&gt; vamos</li>
			/// </ul>
			/// </remarks>
			public virtual string GetStem()
			{
				return stem;
			}

			/// <summary>Returns the original stem of the verb, simply split off from pronouns.</summary>
			/// <remarks>
			/// Returns the original stem of the verb, simply split off from pronouns.
			/// (Contrast with
			/// <see cref="GetStem()"/>
			/// , which returns a normalized form.)
			/// </remarks>
			public virtual string GetOriginalStem()
			{
				return originalStem;
			}

			public virtual IList<string> GetPronouns()
			{
				return pronouns;
			}
		}

		private static readonly IDictionary<string, SpanishVerbStripper> instances = new Dictionary<string, SpanishVerbStripper>();

		private readonly Dictionary<string, string> dict;

		private const string DefaultDict = "edu/stanford/nlp/international/spanish/enclitic-inflections.data";

		/// <summary>Any attached pronouns.</summary>
		/// <remarks>Any attached pronouns. The extra grouping around this pattern allows it to be used in String concatenations.</remarks>
		private const string PatternAttachedPronouns = "(?:(?:[mts]e|n?os|les?)(?:l[oa]s?)?|l[oa]s?)$";

		private static readonly Pattern pTwoAttachedPronouns = Pattern.Compile("([mts]e|n?os|les?)(l[eoa]s?)$");

		private static readonly Pattern pOneAttachedPronoun = Pattern.Compile("([mts]e|n?os|les?|l[oa]s?)$");

		/// <summary>Matches infinitives and gerunds with attached pronouns.</summary>
		/// <remarks>
		/// Matches infinitives and gerunds with attached pronouns.
		/// Original: Pattern.compile("(?:[aeiáéí]r|[áé]ndo)" + PATTERN_ATTACHED_PRONOUNS);
		/// </remarks>
		private static readonly Pattern pStrippable = Pattern.Compile("(?:[aeiáéí]r|[áé]ndo|[aeáé]n?|[aeáé]mos?|[aeiáéí](?:d(?!os)|(?=os)))" + PatternAttachedPronouns);

		/// <summary>
		/// Matches irregular imperatives:
		/// decir = di, hacer = haz, ver = ve, poner = pon, salir = sal,
		/// ser = sé, tener = ten, venir = ven
		/// And id + os = idos, not ios
		/// </summary>
		private static readonly Pattern pIrregulars = Pattern.Compile("^(?:d[ií]|h[aá]z|v[eé]|p[oó]n|s[aá]l|sé|t[eé]n|v[eé]n|(?:id(?=os$)))" + PatternAttachedPronouns);

		/* HashMap of singleton instances */
		/// <summary>Sets up dictionary of valid verbs and their POS info from an input file.</summary>
		/// <remarks>
		/// Sets up dictionary of valid verbs and their POS info from an input file.
		/// The input file must be a list of whitespace-separated verb-lemma-POS triples, one verb
		/// form per line.
		/// </remarks>
		/// <param name="dictPath">the path to the dictionary file</param>
		private static Dictionary<string, string> SetupDictionary(string dictPath)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			BufferedReader br = null;
			try
			{
				br = IOUtils.ReaderFromString(dictPath);
				for (string line; (line = br.ReadLine()) != null; )
				{
					string[] words = line.Trim().Split("\\s");
					if (words.Length < 3)
					{
						System.Console.Error.Printf("SpanishVerbStripper: adding words to dict, missing fields, ignoring line: %s%n", line);
					}
					else
					{
						dictionary[words[0]] = words[2];
					}
				}
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException)
			{
				log.Info("Could not load Spanish data file " + dictPath);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(br);
			}
			return dictionary;
		}

		private static readonly Pair<Pattern, string>[] accentFixes = new Pair[] { new Pair(Pattern.Compile("á"), "a"), new Pair(Pattern.Compile("é"), "e"), new Pair(Pattern.Compile("í"), "i"), new Pair(Pattern.Compile("ó"), "o"), new Pair(Pattern.Compile
			("ú"), "u") };

		/// <summary>Access via the singleton-like getInstance() methods.</summary>
		private SpanishVerbStripper(string dictPath)
		{
			// CONSTRUCTOR
			dict = SetupDictionary(dictPath);
		}

		/// <summary>Singleton pattern function for getting a default verb stripper.</summary>
		public static SpanishVerbStripper GetInstance()
		{
			return GetInstance(DefaultDict);
		}

		/// <summary>
		/// Singleton pattern function for getting a verb stripper based on
		/// the dictionary at dictPath.
		/// </summary>
		/// <param name="dictPath">the path to the dictionary for this verb stripper.</param>
		public static SpanishVerbStripper GetInstance(string dictPath)
		{
			SpanishVerbStripper svs = instances[dictPath];
			if (svs == null)
			{
				svs = new SpanishVerbStripper(dictPath);
				instances[dictPath] = svs;
			}
			return svs;
		}

		/// <summary>
		/// The verbs in this set have accents in their infinitive forms;
		/// don't remove the accents when stripping pronouns!
		/// </summary>
		private static readonly ICollection<string> accentedInfinitives = new HashSet<string>(Arrays.AsList("desleír", "desoír", "embaír", "engreír", "entreoír", "freír", "oír", "refreír", "reír", "sofreír", "sonreír"));

		// STATIC FUNCTIONS
		/// <summary>Determine if the given word is a verb which needs to be stripped.</summary>
		public static bool IsStrippable(string word)
		{
			return pStrippable.Matcher(word).Find() || pIrregulars.Matcher(word).Find();
		}

		private static string RemoveAccents(string word)
		{
			if (accentedInfinitives.Contains(word))
			{
				return word;
			}
			string stripped = word;
			foreach (Pair<Pattern, string> accentFix in accentFixes)
			{
				stripped = accentFix.First().Matcher(stripped).ReplaceAll(accentFix.Second());
			}
			return stripped;
		}

		/// <summary>
		/// Determines the case of the letter as if it had been part of the
		/// original string
		/// </summary>
		/// <param name="letter">The character whose case must be determined</param>
		/// <param name="original">The string we are modelling the case on</param>
		private static char GetCase(string original, char letter)
		{
			if (char.IsUpperCase(original[original.Length - 1]))
			{
				return char.ToUpperCase(letter);
			}
			else
			{
				return char.ToLowerCase(letter);
			}
		}

		private static readonly Pattern nosse = Pattern.Compile("nos|se");

		/// <summary>Validate and normalize the given verb stripper result.</summary>
		/// <remarks>
		/// Validate and normalize the given verb stripper result.
		/// Returns <tt>true</tt> if the given data is a valid pairing of verb form
		/// and clitic pronoun(s).
		/// May modify <tt>pair</tt> in place in order to make the pair valid.
		/// For example, if the pair <tt>(senta, os)</tt> is provided, this
		/// method will return <tt>true</tt> and modify the pair to be
		/// <tt>(sentad, os)</tt>.
		/// </remarks>
		private bool NormalizeStrippedVerb(SpanishVerbStripper.StrippedVerb verb)
		{
			string normalized = RemoveAccents(verb.GetOriginalStem());
			string firstPron = verb.GetPronouns()[0].ToLower();
			// Look up verb in dictionary.
			string verbKey = normalized.ToLower();
			string pos = dict[verbKey];
			bool valid = false;
			// System.out.println(verbKey + " " + dict.containsKey(verbKey + 's'));
			// Validate resulting split verb and normalize the new form at the same
			// time.
			if (pos != null)
			{
				// Check not invalid combination of verb root and pronoun.
				// (If we combine a second-person plural imperative and the
				// second person plural object pronoun, we expect to see an
				// elided verb root, not the normal one that's in the
				// dictionary.)
				valid = !(pos.Equals("VMM02P0") && Sharpen.Runtime.EqualsIgnoreCase(firstPron, "os"));
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(firstPron, "os") && dict.Contains(verbKey + 'd'))
				{
					// Special case: de-elide elided verb root in the case of a second
					// person plural imperative + second person object pronoun
					//
					// (e.g., given (senta, os), return (sentad, os))
					normalized = normalized + GetCase(normalized, 'd');
					valid = true;
				}
				else
				{
					if (nosse.Matcher(firstPron).Matches() && dict.Contains(verbKey + 's'))
					{
						// Special case: de-elide elided verb root in the case of a first
						// person plural imperative + object pronoun
						//
						// (vámo, nos) -> (vámos, nos)
						normalized = normalized + GetCase(normalized, 's');
						valid = true;
					}
				}
			}
			if (valid)
			{
				// Update normalized form.
				verb.SetStem(normalized);
				return true;
			}
			return false;
		}

		/// <summary>Separate attached pronouns from the given verb.</summary>
		/// <param name="word">A valid Spanish verb with clitic pronouns attached.</param>
		/// <param name="pSuffix">A pattern to match these attached pronouns.</param>
		/// <returns>
		/// A
		/// <see cref="StrippedVerb"/>
		/// instance or <tt>null</tt> if no attached
		/// pronouns were found.
		/// </returns>
		private SpanishVerbStripper.StrippedVerb StripSuffix(string word, Pattern pSuffix)
		{
			Matcher m = pSuffix.Matcher(word);
			if (m.Find())
			{
				string stripped = Sharpen.Runtime.Substring(word, 0, m.Start());
				IList<string> attached = new List<string>();
				for (int i = 0; i < m.GroupCount(); i++)
				{
					attached.Add(m.Group(i + 1));
				}
				return new SpanishVerbStripper.StrippedVerb(stripped, attached);
			}
			return null;
		}

		/// <summary>Attempt to separate attached pronouns from the given verb.</summary>
		/// <param name="verb">Spanish verb</param>
		/// <returns>
		/// Returns a StrippedVerb struct/tuple <tt>(originalStem, normalizedStem, pronouns)</tt>,
		/// or <tt>null</tt> if no pronouns could be located and separated.
		/// <ul>
		/// <li><tt>originalStem</tt>: The verb stem simply split from the following pronouns.</li>
		/// <li><tt>normalizedStem</tt>: The verb stem normalized to dictionary form, i.e. in the
		/// form it would appear with the same conjugation but no pronouns.</li>
		/// <li><tt>pronouns</tt>: Pronouns which were attached to the verb.</li>
		/// </ul>
		/// </returns>
		public SpanishVerbStripper.StrippedVerb SeparatePronouns(string verb)
		{
			SpanishVerbStripper.StrippedVerb result;
			// Try to strip just one pronoun first
			result = StripSuffix(verb, pOneAttachedPronoun);
			if (result != null && NormalizeStrippedVerb(result))
			{
				return result;
			}
			// Now two
			result = StripSuffix(verb, pTwoAttachedPronouns);
			if (result != null && NormalizeStrippedVerb(result))
			{
				return result;
			}
			return null;
		}

		/// <summary>Remove attached pronouns from a strippable Spanish verb form.</summary>
		/// <remarks>
		/// Remove attached pronouns from a strippable Spanish verb form. (Use
		/// <see cref="IsStrippable(string)"/>
		/// to determine if a word is a
		/// strippable verb.)
		/// Converts, e.g.,
		/// <ul>
		/// <li> decírmelo -&gt; decir
		/// <li> mudarse -&gt; mudar
		/// <li> contándolos -&gt; contando
		/// <li> hazlo -&gt; haz
		/// </ul>
		/// </remarks>
		/// <returns>
		/// A verb form stripped of attached pronouns, or <tt>null</tt>
		/// if no pronouns were located / stripped.
		/// </returns>
		public string StripVerb(string verb)
		{
			SpanishVerbStripper.StrippedVerb separated = SeparatePronouns(verb);
			if (separated != null)
			{
				return separated.GetStem();
			}
			return null;
		}

		private const long serialVersionUID = -4780144226395772354L;
	}
}
