using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Pascal;
using Edu.Stanford.Nlp.IE.Regexp;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Time;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.IE
{
	/// <summary>
	/// Various methods for normalizing Money, Date, Percent, Time, and
	/// Number, Ordinal amounts.
	/// </summary>
	/// <remarks>
	/// Various methods for normalizing Money, Date, Percent, Time, and
	/// Number, Ordinal amounts.
	/// These matchers are generous in that they try to quantify something
	/// that's already been labelled by an NER system; don't use them to make
	/// classification decisions.  This class has a twin in the pipeline world:
	/// <see cref="Edu.Stanford.Nlp.Pipeline.QuantifiableEntityNormalizingAnnotator"/>
	/// .
	/// Please keep the substantive content here, however, so as to lessen code
	/// duplication.
	/// <i>Implementation note:</i> The extensive test code for this class is
	/// now in a separate JUnit Test class.  This class depends on the background
	/// symbol for NER being the default background symbol.  This should be fixed
	/// at some point.
	/// </remarks>
	/// <author>Chris Cox</author>
	/// <author>Christopher Manning (extended for RTE)</author>
	/// <author>Anna Rafferty</author>
	public class QuantifiableEntityNormalizer
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.QuantifiableEntityNormalizer));

		private const bool Debug = false;

		private const bool Debug2 = false;

		public static string BackgroundSymbol = SeqClassifierFlags.DefaultBackgroundSymbol;

		private static readonly Pattern timePattern = Pattern.Compile("([0-2]?[0-9])((?::[0-5][0-9]){0,2})([PpAa]\\.?[Mm]\\.?)?");

		private static readonly Pattern moneyPattern = Pattern.Compile("([$\u00A3\u00A5\u20AC#]?)(-?[0-9,]*)(\\.[0-9]*)?+");

		private static readonly Pattern scorePattern = Pattern.Compile(" *([0-9]+) *- *([0-9]+) *");

		private static readonly ICollection<string> quantifiable;

		private static readonly ICollection<string> collapseBeforeParsing;

		private static readonly ICollection<string> timeUnitWords;

		private static readonly IDictionary<string, double> moneyMultipliers;

		private static readonly IDictionary<string, int> moneyMultipliers2;

		private static readonly IDictionary<string, char> currencyWords;

		public static readonly ClassicCounter<string> wordsToValues;

		public static readonly ClassicCounter<string> ordinalsToValues;

		static QuantifiableEntityNormalizer()
		{
			// String normalizing functions
			// this isn't a constant; it's set by the QuantifiableEntityNormalizingAnnotator
			//Collections of entity types
			//Entity types that are quantifiable
			quantifiable = Generics.NewHashSet();
			quantifiable.Add("MONEY");
			quantifiable.Add("TIME");
			quantifiable.Add("DATE");
			quantifiable.Add("PERCENT");
			quantifiable.Add("NUMBER");
			quantifiable.Add("ORDINAL");
			quantifiable.Add("DURATION");
			collapseBeforeParsing = Generics.NewHashSet();
			collapseBeforeParsing.Add("PERSON");
			collapseBeforeParsing.Add("ORGANIZATION");
			collapseBeforeParsing.Add("LOCATION");
			timeUnitWords = Generics.NewHashSet();
			timeUnitWords.Add("second");
			timeUnitWords.Add("seconds");
			timeUnitWords.Add("minute");
			timeUnitWords.Add("minutes");
			timeUnitWords.Add("hour");
			timeUnitWords.Add("hours");
			timeUnitWords.Add("day");
			timeUnitWords.Add("days");
			timeUnitWords.Add("week");
			timeUnitWords.Add("weeks");
			timeUnitWords.Add("month");
			timeUnitWords.Add("months");
			timeUnitWords.Add("year");
			timeUnitWords.Add("years");
			currencyWords = Generics.NewHashMap();
			currencyWords["dollars?"] = '$';
			currencyWords["cents?"] = '$';
			currencyWords["pounds?"] = '\u00A3';
			currencyWords["pence|penny"] = '\u00A3';
			currencyWords["yen"] = '\u00A5';
			currencyWords["euros?"] = '\u20AC';
			currencyWords["won"] = '\u20A9';
			currencyWords["\\$"] = '$';
			currencyWords["\u00A2"] = '$';
			// cents
			currencyWords["\u00A3"] = '\u00A3';
			// pounds
			currencyWords["#"] = '\u00A3';
			// for Penn treebank
			currencyWords["\u00A5"] = '\u00A5';
			// Yen
			currencyWords["\u20AC"] = '\u20AC';
			// Euro
			currencyWords["\u20A9"] = '\u20A9';
			// Won
			currencyWords["yuan"] = '\u5143';
			// Yuan
			moneyMultipliers = Generics.NewHashMap();
			moneyMultipliers["trillion"] = 1000000000000.0;
			// can't be an integer
			moneyMultipliers["billion"] = 1000000000.0;
			moneyMultipliers["bn"] = 1000000000.0;
			moneyMultipliers["million"] = 1000000.0;
			moneyMultipliers["thousand"] = 1000.0;
			moneyMultipliers["hundred"] = 100.0;
			moneyMultipliers["b."] = 1000000000.0;
			moneyMultipliers["m."] = 1000000.0;
			moneyMultipliers[" m "] = 1000000.0;
			moneyMultipliers[" k "] = 1000.0;
			moneyMultipliers2 = Generics.NewHashMap();
			moneyMultipliers2["[0-9](m)(?:[^a-zA-Z]|$)"] = 1000000;
			moneyMultipliers2["[0-9](b)(?:[^a-zA-Z]|$)"] = 1000000000;
			wordsToValues = new ClassicCounter<string>();
			wordsToValues.SetCount("zero", 0.0);
			wordsToValues.SetCount("one", 1.0);
			wordsToValues.SetCount("two", 2.0);
			wordsToValues.SetCount("three", 3.0);
			wordsToValues.SetCount("four", 4.0);
			wordsToValues.SetCount("five", 5.0);
			wordsToValues.SetCount("six", 6.0);
			wordsToValues.SetCount("seven", 7.0);
			wordsToValues.SetCount("eight", 8.0);
			wordsToValues.SetCount("nine", 9.0);
			wordsToValues.SetCount("ten", 10.0);
			wordsToValues.SetCount("eleven", 11.0);
			wordsToValues.SetCount("twelve", 12.0);
			wordsToValues.SetCount("thirteen", 13.0);
			wordsToValues.SetCount("fourteen", 14.0);
			wordsToValues.SetCount("fifteen", 15.0);
			wordsToValues.SetCount("sixteen", 16.0);
			wordsToValues.SetCount("seventeen", 17.0);
			wordsToValues.SetCount("eighteen", 18.0);
			wordsToValues.SetCount("nineteen", 19.0);
			wordsToValues.SetCount("twenty", 20.0);
			wordsToValues.SetCount("thirty", 30.0);
			wordsToValues.SetCount("forty", 40.0);
			wordsToValues.SetCount("fifty", 50.0);
			wordsToValues.SetCount("sixty", 60.0);
			wordsToValues.SetCount("seventy", 70.0);
			wordsToValues.SetCount("eighty", 80.0);
			wordsToValues.SetCount("ninety", 90.0);
			wordsToValues.SetCount("hundred", 100.0);
			wordsToValues.SetCount("thousand", 1000.0);
			wordsToValues.SetCount("million", 1000000.0);
			wordsToValues.SetCount("billion", 1000000000.0);
			wordsToValues.SetCount("bn", 1000000000.0);
			wordsToValues.SetCount("trillion", 1000000000000.0);
			wordsToValues.SetCount("dozen", 12.0);
			ordinalsToValues = new ClassicCounter<string>();
			ordinalsToValues.SetCount("zeroth", 0.0);
			ordinalsToValues.SetCount("first", 1.0);
			ordinalsToValues.SetCount("second", 2.0);
			ordinalsToValues.SetCount("third", 3.0);
			ordinalsToValues.SetCount("fourth", 4.0);
			ordinalsToValues.SetCount("fifth", 5.0);
			ordinalsToValues.SetCount("sixth", 6.0);
			ordinalsToValues.SetCount("seventh", 7.0);
			ordinalsToValues.SetCount("eighth", 8.0);
			ordinalsToValues.SetCount("ninth", 9.0);
			ordinalsToValues.SetCount("tenth", 10.0);
			ordinalsToValues.SetCount("eleventh", 11.0);
			ordinalsToValues.SetCount("twelfth", 12.0);
			ordinalsToValues.SetCount("thirteenth", 13.0);
			ordinalsToValues.SetCount("fourteenth", 14.0);
			ordinalsToValues.SetCount("fifteenth", 15.0);
			ordinalsToValues.SetCount("sixteenth", 16.0);
			ordinalsToValues.SetCount("seventeenth", 17.0);
			ordinalsToValues.SetCount("eighteenth", 18.0);
			ordinalsToValues.SetCount("nineteenth", 19.0);
			ordinalsToValues.SetCount("twentieth", 20.0);
			ordinalsToValues.SetCount("twenty-first", 21.0);
			ordinalsToValues.SetCount("twenty-second", 22.0);
			ordinalsToValues.SetCount("twenty-third", 23.0);
			ordinalsToValues.SetCount("twenty-fourth", 24.0);
			ordinalsToValues.SetCount("twenty-fifth", 25.0);
			ordinalsToValues.SetCount("twenty-sixth", 26.0);
			ordinalsToValues.SetCount("twenty-seventh", 27.0);
			ordinalsToValues.SetCount("twenty-eighth", 28.0);
			ordinalsToValues.SetCount("twenty-ninth", 29.0);
			ordinalsToValues.SetCount("thirtieth", 30.0);
			ordinalsToValues.SetCount("thirty-first", 31.0);
			ordinalsToValues.SetCount("fortieth", 40.0);
			ordinalsToValues.SetCount("fiftieth", 50.0);
			ordinalsToValues.SetCount("sixtieth", 60.0);
			ordinalsToValues.SetCount("seventieth", 70.0);
			ordinalsToValues.SetCount("eightieth", 80.0);
			ordinalsToValues.SetCount("ninetieth", 90.0);
			ordinalsToValues.SetCount("hundredth", 100.0);
			ordinalsToValues.SetCount("thousandth", 1000.0);
			ordinalsToValues.SetCount("millionth", 1000000.0);
			ordinalsToValues.SetCount("billionth", 1000000000.0);
			ordinalsToValues.SetCount("trillionth", 1000000000000.0);
		}

		private QuantifiableEntityNormalizer()
		{
		}

		// this is all static
		/// <summary>
		/// This method returns the closest match in set such that the match
		/// has more than three letters and differs from word only by one substitution,
		/// deletion, or insertion.
		/// </summary>
		/// <remarks>
		/// This method returns the closest match in set such that the match
		/// has more than three letters and differs from word only by one substitution,
		/// deletion, or insertion.  If not match exists, returns null.
		/// </remarks>
		private static string GetOneSubstitutionMatch(string word, ICollection<string> set)
		{
			// TODO (?) pass the EditDistance around more places to make this
			// more efficient.  May not really matter.
			EditDistance ed = new EditDistance();
			foreach (string cur in set)
			{
				if (IsOneSubstitutionMatch(word, cur, ed))
				{
					return cur;
				}
			}
			return null;
		}

		private static bool IsOneSubstitutionMatch(string word, string match, EditDistance ed)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(word, match))
			{
				return true;
			}
			if (match.Length > 3)
			{
				if (ed.Score(word, match) <= 1)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Convert the content of a List of CoreMaps to a single
		/// space-separated String.
		/// </summary>
		/// <remarks>
		/// Convert the content of a List of CoreMaps to a single
		/// space-separated String.  This grabs stuff based on the get(CoreAnnotations.NamedEntityTagAnnotation.class) field.
		/// [CDM: Changed to look at NamedEntityTagAnnotation not AnswerClass Jun 2010, hoping that will fix a bug.]
		/// </remarks>
		/// <param name="l">The List</param>
		/// <returns>one string containing all words in the list, whitespace separated</returns>
		private static string SingleEntityToString<E>(IList<E> l)
			where E : ICoreMap
		{
			string entityType = l[0].Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
			StringBuilder sb = new StringBuilder();
			foreach (E w in l)
			{
				System.Diagnostics.Debug.Assert((w.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Equals(entityType)));
				sb.Append(w.Get(typeof(CoreAnnotations.TextAnnotation)));
				sb.Append(' ');
			}
			return sb.ToString();
		}

		/// <summary>
		/// Currently this populates a
		/// <c>List&lt;CoreLabel&gt;</c>
		/// with words from the passed List,
		/// but NER entities are collapsed and
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// constituents of entities have
		/// NER information in their "quantity" fields.
		/// NOTE: This now seems to be used nowhere.  The collapsing is done elsewhere.
		/// That's probably appropriate; it doesn't seem like this should be part of
		/// QuantifiableEntityNormalizer, since it's set to collapse non-quantifiable
		/// entities....
		/// </summary>
		/// <param name="l">a list of CoreLabels with NER labels,</param>
		/// <returns>a Sentence where PERSON, ORG, LOC, entities are collapsed.</returns>
		public static IList<CoreLabel> CollapseNERLabels(IList<CoreLabel> l)
		{
			IList<CoreLabel> s = new List<CoreLabel>();
			string lastEntity = BackgroundSymbol;
			StringBuilder entityStringCollector = null;
			//Iterate through each word....
			foreach (CoreLabel w in l)
			{
				string entityType = w.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
				//if we've just completed an entity and we're looking at a non-continuation,
				//we want to add that now.
				if (entityStringCollector != null && !entityType.Equals(lastEntity))
				{
					CoreLabel nextWord = new CoreLabel();
					nextWord.SetWord(entityStringCollector.ToString());
					nextWord.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "NNP");
					nextWord.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), lastEntity);
					s.Add(nextWord);
					entityStringCollector = null;
				}
				//If its not to be collapsed, toss it onto the sentence.
				if (!collapseBeforeParsing.Contains(entityType))
				{
					s.Add(w);
				}
				else
				{
					//If it is to be collapsed....
					//if its a continuation of the last entity, add it to the
					//current buffer.
					if (entityType.Equals(lastEntity))
					{
						System.Diagnostics.Debug.Assert(entityStringCollector != null);
						entityStringCollector.Append('_');
						entityStringCollector.Append(w.Get(typeof(CoreAnnotations.TextAnnotation)));
					}
					else
					{
						//and its NOT a continuation, make a new buffer.
						entityStringCollector = new StringBuilder();
						entityStringCollector.Append(w.Get(typeof(CoreAnnotations.TextAnnotation)));
					}
				}
				lastEntity = entityType;
			}
			// if the last token was a named-entity, we add it here.
			if (entityStringCollector != null)
			{
				CoreLabel nextWord = new CoreLabel();
				nextWord.SetWord(entityStringCollector.ToString());
				nextWord.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "NNP");
				nextWord.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), lastEntity);
				s.Add(nextWord);
			}
			foreach (CoreLabel w_1 in s)
			{
				log.Info("<<" + w_1.Get(typeof(CoreAnnotations.TextAnnotation)) + "::" + w_1.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)) + "::" + w_1.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)) + ">>");
			}
			return s;
		}

		/// <summary>Provided for backwards compatibility; see normalizedDateString(s, openRangeMarker)</summary>
		internal static string NormalizedDateString(string s, Timex timexFromSUTime)
		{
			return NormalizedDateString(s, ISODateInstance.NoRange, timexFromSUTime);
		}

		/// <summary>
		/// Returns a string that represents either a single date or a range of
		/// dates.
		/// </summary>
		/// <remarks>
		/// Returns a string that represents either a single date or a range of
		/// dates.  Representation pattern is roughly ISO8601, with some extensions
		/// for greater expressivity; see
		/// <see cref="Edu.Stanford.Nlp.IE.Pascal.ISODateInstance"/>
		/// for details.
		/// </remarks>
		/// <param name="s">Date string to normalize</param>
		/// <param name="openRangeMarker">
		/// a marker for whether this date is not involved in
		/// an open range, is involved in an open range that goes forever backward and
		/// stops at s, or is involved in an open range that goes forever forward and
		/// starts at s.  See
		/// <see cref="Edu.Stanford.Nlp.IE.Pascal.ISODateInstance"/>
		/// .
		/// </param>
		/// <returns>A yyyymmdd format normalized date</returns>
		private static string NormalizedDateString(string s, string openRangeMarker, Timex timexFromSUTime)
		{
			if (timexFromSUTime != null)
			{
				if (timexFromSUTime.Value() != null)
				{
					// fully disambiguated temporal
					return timexFromSUTime.Value();
				}
				else
				{
					// this is a relative date, e.g., "yesterday"
					return timexFromSUTime.AltVal();
				}
			}
			ISODateInstance d = new ISODateInstance(s, openRangeMarker);
			return d.GetDateString();
		}

		private static string NormalizedDurationString(string s, Timex timexFromSUTime)
		{
			if (timexFromSUTime != null)
			{
				if (timexFromSUTime.Value() != null)
				{
					// fully disambiguated temporal
					return timexFromSUTime.Value();
				}
				else
				{
					// something else
					return timexFromSUTime.AltVal();
				}
			}
			// TODO: normalize duration ourselves
			return null;
		}

		/// <summary>Tries to heuristically determine if the given word is a year</summary>
		private static bool IsYear(ICoreMap word)
		{
			string wordString = word.Get(typeof(CoreAnnotations.TextAnnotation));
			if (word.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)) == null || word.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).Equals("CD"))
			{
				//one possibility: it's a two digit year with an apostrophe: '90
				if (wordString.Length == 3 && wordString.StartsWith("'"))
				{
					wordString = Sharpen.Runtime.Substring(wordString, 1);
					try
					{
						System.Convert.ToInt32(wordString);
						return true;
					}
					catch (Exception)
					{
						return false;
					}
				}
				//if it is 4 digits, with first one <3 (usually we're not talking about
				//the far future, say it's a year
				if (wordString.Length == 4)
				{
					try
					{
						int num = System.Convert.ToInt32(wordString);
						if (num < 3000)
						{
							return true;
						}
					}
					catch (Exception)
					{
						return false;
					}
				}
			}
			return false;
		}

		private const string dateRangeAfterOneWord = "after|since";

		private const string dateRangeBeforeOneWord = "before|until";

		private static readonly IList<Pair<string, string>> dateRangeBeforePairedOneWord;

		static QuantifiableEntityNormalizer()
		{
			dateRangeBeforePairedOneWord = new List<Pair<string, string>>();
			dateRangeBeforePairedOneWord.Add(new Pair<string, string>("between", "and"));
			dateRangeBeforePairedOneWord.Add(new Pair<string, string>("from", "to"));
			dateRangeBeforePairedOneWord.Add(new Pair<string, string>("from", "-"));
		}

		private const string datePrepositionAfterWord = "in|of";

		/// <summary>
		/// Takes the strings of the one previous and 3 next words to a date to
		/// detect date range modifiers like "before" or "between
		/// <c>&lt;date&gt;</c>
		/// and
		/// <c>&lt;date&gt;</c>
		/// .
		/// </summary>
		/// <?/>
		private static string DetectDateRangeModifier<E>(IList<E> date, IList<E> list, int beforeIndex, int afterIndex)
			where E : ICoreMap
		{
			E prev = (beforeIndex >= 0) ? list[beforeIndex] : null;
			int sz = list.Count;
			E next = (afterIndex < sz) ? list[afterIndex] : null;
			E next2 = (afterIndex + 1 < sz) ? list[afterIndex + 1] : null;
			E next3 = (afterIndex + 2 < sz) ? list[afterIndex + 2] : null;
			//sometimes the year gets tagged as CD but not as a date - if this happens, we want to add it in
			if (next != null && IsYear(next))
			{
				date.Add(next);
				next.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), "DATE");
				afterIndex++;
			}
			if (next2 != null && IsYear(next2))
			{
				// This code here just seems wrong.... why are we marking next as a date without checking anything?
				date.Add(next);
				System.Diagnostics.Debug.Assert((next != null));
				// keep the static analysis happy.
				next.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), "DATE");
				date.Add(next2);
				next2.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), "DATE");
				afterIndex += 2;
			}
			//sometimes the date will be stated in a form like "June of 1984" -> we'd like this to be 198406
			if (next != null && next.Get(typeof(CoreAnnotations.TextAnnotation)).Matches(datePrepositionAfterWord))
			{
				//check if the next next word is a year or month
				if (next2 != null && (IsYear(next2)))
				{
					//TODO: implement month!
					date.Add(next);
					date.Add(next2);
					afterIndex += 2;
				}
			}
			//String range = detectTwoSidedRangeModifier(date.get(0), list, beforeIndex, afterIndex);
			//if(range !=ISODateInstance.NO_RANGE) return range;
			//check if it's an open range - two sided ranges get checked elsewhere
			//based on the prev word
			if (prev != null)
			{
				string prevWord = prev.Get(typeof(CoreAnnotations.TextAnnotation)).ToLower();
				if (prevWord.Matches(dateRangeBeforeOneWord))
				{
					//we have an open range of the before type - e.g., Before June 6, John was 5
					prev.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "DATE_MOD");
					return ISODateInstance.OpenRangeBefore;
				}
				else
				{
					if (prevWord.Matches(dateRangeAfterOneWord))
					{
						//we have an open range of the after type - e.g., After June 6, John was 6
						prev.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "DATE_MOD");
						return ISODateInstance.OpenRangeAfter;
					}
				}
			}
			return ISODateInstance.NoRange;
		}

		// Version of above without any weird stuff
		private static string DetectDateRangeModifier<E>(E prev)
			where E : ICoreMap
		{
			if (prev != null)
			{
				string prevWord = prev.Get(typeof(CoreAnnotations.TextAnnotation)).ToLower();
				if (prevWord.Matches(dateRangeBeforeOneWord))
				{
					//we have an open range of the before type - e.g., Before June 6, John was 5
					return ISODateInstance.OpenRangeBefore;
				}
				else
				{
					if (prevWord.Matches(dateRangeAfterOneWord))
					{
						//we have an open range of the after type - e.g., After June 6, John was 6
						return ISODateInstance.OpenRangeAfter;
					}
				}
			}
			return ISODateInstance.NoRange;
		}

		/// <summary>
		/// This should detect things like "between 5 and 5 million" and "from April 3 to June 6"
		/// Each side of the range is tagged with the correct numeric quantity (e.g., 5/5x10E6 or
		/// ****0403/****0606) and the other words (e.g., "between", "and", "from", "to") are
		/// tagged as quantmod to avoid penalizing them for lack of alignment/matches.
		/// </summary>
		/// <remarks>
		/// This should detect things like "between 5 and 5 million" and "from April 3 to June 6"
		/// Each side of the range is tagged with the correct numeric quantity (e.g., 5/5x10E6 or
		/// ****0403/****0606) and the other words (e.g., "between", "and", "from", "to") are
		/// tagged as quantmod to avoid penalizing them for lack of alignment/matches.
		/// This method should be called after other collapsing is complete (e.g. 5 million should already be
		/// concatenated)
		/// </remarks>
		/// <?/>
		private static IList<E> DetectTwoSidedRangeModifier<E>(E firstDate, IList<E> list, int beforeIndex, int afterIndex, bool concatenate)
			where E : ICoreMap
		{
			E prev = (beforeIndex >= 0) ? list[beforeIndex] : null;
			//E cur = list.get(0);
			int sz = list.Count;
			E next = (afterIndex < sz) ? list[afterIndex] : null;
			E next2 = (afterIndex + 1 < sz) ? list[afterIndex + 1] : null;
			IList<E> toRemove = new List<E>();
			string curNER = (firstDate == null ? string.Empty : firstDate.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)));
			if (curNER == null)
			{
				curNER = string.Empty;
			}
			if (firstDate == null || firstDate.Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation)) == null)
			{
				return toRemove;
			}
			//TODO: make ranges actually work
			//first check if it's of the form "between <date> and <date>"/etc
			if (prev != null)
			{
				foreach (Pair<string, string> ranges in dateRangeBeforePairedOneWord)
				{
					if (prev.Get(typeof(CoreAnnotations.TextAnnotation)).Matches(ranges.First()))
					{
						if (next != null && next2 != null)
						{
							string nerNext2 = next2.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
							if (next.Get(typeof(CoreAnnotations.TextAnnotation)).Matches(ranges.Second()) && nerNext2 != null && nerNext2.Equals(curNER))
							{
								//Add rest in
								prev.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "QUANT_MOD");
								string rangeString;
								if (curNER.Equals("DATE"))
								{
									ISODateInstance c = new ISODateInstance(new ISODateInstance(firstDate.Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation))), new ISODateInstance(next2.Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation))));
									rangeString = c.GetDateString();
								}
								else
								{
									rangeString = firstDate.Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation)) + '-' + next2.Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation));
								}
								firstDate.Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), rangeString);
								next2.Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), rangeString);
								next.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), nerNext2);
								next.Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), rangeString);
								if (concatenate)
								{
									IList<E> numberWords = new List<E>();
									numberWords.Add(firstDate);
									numberWords.Add(next);
									numberWords.Add(next2);
									ConcatenateNumericString(numberWords, toRemove);
								}
							}
						}
					}
				}
			}
			return toRemove;
		}

		/// <summary>
		/// Concatenates separate words of a date or other numeric quantity into one node (e.g., 3 November -&gt; 3_November)
		/// Tag is CD or NNP, and other words are added to the remove list
		/// </summary>
		private static void ConcatenateNumericString<E>(IList<E> words, IList<E> toRemove)
			where E : ICoreMap
		{
			if (words.Count <= 1)
			{
				return;
			}
			bool first = true;
			StringBuilder newText = new StringBuilder();
			E foundEntity = null;
			foreach (E word in words)
			{
				if (foundEntity == null && (word.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).Equals("CD") || word.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).Equals("NNP")))
				{
					foundEntity = word;
				}
				if (first)
				{
					first = false;
				}
				else
				{
					newText.Append('_');
				}
				newText.Append(word.Get(typeof(CoreAnnotations.TextAnnotation)));
			}
			if (foundEntity == null)
			{
				foundEntity = words[0];
			}
			//if we didn't find one with the appropriate tag, just take the first one
			Sharpen.Collections.AddAll(toRemove, words);
			toRemove.Remove(foundEntity);
			foundEntity.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "CD");
			// cdm 2008: is this actually good for dates??
			string collapsed = newText.ToString();
			foundEntity.Set(typeof(CoreAnnotations.TextAnnotation), collapsed);
			foundEntity.Set(typeof(CoreAnnotations.OriginalTextAnnotation), collapsed);
		}

		public static string NormalizedTimeString(string s, Timex timexFromSUTime)
		{
			return NormalizedTimeString(s, null, timexFromSUTime);
		}

		private static string NormalizedTimeString(string s, string ampm, Timex timexFromSUTime)
		{
			if (timexFromSUTime != null)
			{
				if (timexFromSUTime.Value() != null)
				{
					// this timex is fully disambiguated
					return timexFromSUTime.Value();
				}
				else
				{
					// not disambiguated; contains some relative date
					return timexFromSUTime.AltVal();
				}
			}
			s = s.ReplaceAll("[ \t\n\x0\f\r]", string.Empty);
			Matcher m = timePattern.Matcher(s);
			if (Sharpen.Runtime.EqualsIgnoreCase(s, "noon"))
			{
				return "12:00pm";
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(s, "midnight"))
				{
					return "00:00am";
				}
				else
				{
					// or "12:00am" ?
					if (Sharpen.Runtime.EqualsIgnoreCase(s, "morning"))
					{
						return "M";
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(s, "afternoon"))
						{
							return "A";
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(s, "evening"))
							{
								return "EN";
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(s, "night"))
								{
									return "N";
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(s, "day"))
									{
										return "D";
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(s, "suppertime"))
										{
											return "EN";
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(s, "lunchtime"))
											{
												return "MD";
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(s, "midday"))
												{
													return "MD";
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(s, "teatime"))
													{
														return "A";
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(s, "dinnertime"))
														{
															return "EN";
														}
														else
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(s, "dawn"))
															{
																return "EM";
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(s, "dusk"))
																{
																	return "EN";
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(s, "sundown"))
																	{
																		return "EN";
																	}
																	else
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(s, "sunup"))
																		{
																			return "EM";
																		}
																		else
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(s, "daybreak"))
																			{
																				return "EM";
																			}
																			else
																			{
																				if (m.Matches())
																				{
																					// group 1 is hours, group 2 is minutes and maybe seconds; group 3 is am/pm
																					StringBuilder sb = new StringBuilder();
																					sb.Append(m.Group(1));
																					if (m.Group(2) == null || string.Empty.Equals(m.Group(2)))
																					{
																						sb.Append(":00");
																					}
																					else
																					{
																						sb.Append(m.Group(2));
																					}
																					if (m.Group(3) != null)
																					{
																						string suffix = m.Group(3);
																						suffix = suffix.ReplaceAll("\\.", string.Empty);
																						suffix = suffix.ToLower();
																						sb.Append(suffix);
																					}
																					else
																					{
																						if (ampm != null)
																						{
																							sb.Append(ampm);
																						}
																					}
																					// } else {
																					// Do nothing; leave ambiguous
																					// sb.append("pm");
																					return sb.ToString();
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Heuristically decides if s is in American (42.33) or European (42,33) number format
		/// and tries to turn European version into American.
		/// </summary>
		private static string ConvertToAmerican(string s)
		{
			if (s.Contains(","))
			{
				//turn all but the last into blanks - this isn't really correct, but it's close enough for now
				while (s.IndexOf(',') != s.LastIndexOf(','))
				{
					s = s.ReplaceFirst(",", string.Empty);
				}
				int place = s.LastIndexOf(',');
				//if it's american, should have at least three characters after it
				if (place >= s.Length - 3 && place != s.Length - 1)
				{
					s = Sharpen.Runtime.Substring(s, 0, place) + '.' + Sharpen.Runtime.Substring(s, place + 1);
				}
				else
				{
					s = s.Replace(",", string.Empty);
				}
			}
			return s;
		}

		internal static string NormalizedMoneyString(string s, Number numberFromSUTime)
		{
			//first, see if it looks like european style
			s = ConvertToAmerican(s);
			// clean up string
			s = s.ReplaceAll("[ \t\n\x0\f\r,]", string.Empty);
			s = s.ToLower();
			double multiplier = 1.0;
			// do currency words
			char currencySign = '$';
			foreach (KeyValuePair<string, char> stringCharacterEntry in currencyWords)
			{
				string key = stringCharacterEntry.Key;
				if (StringUtils.Find(s, key))
				{
					if (key.Equals("pence|penny") || key.Equals("cents?") || key.Equals("\u00A2"))
					{
						multiplier *= 0.01;
					}
					// if(DEBUG) { log.info("Quantifiable: Found "+ currencyWord); }
					s = s.ReplaceAll(key, string.Empty);
					currencySign = (char)stringCharacterEntry.Value;
				}
			}
			// process rest as number
			string value = NormalizedNumberStringQuiet(s, multiplier, string.Empty, numberFromSUTime);
			if (value == null)
			{
				return null;
			}
			else
			{
				return currencySign + value;
			}
		}

		public static string NormalizedNumberString(string s, string nextWord, Number numberFromSUTime)
		{
			return NormalizedNumberStringQuiet(s, 1.0, nextWord, numberFromSUTime);
		}

		private static readonly Pattern allSpaces = Pattern.Compile(" *");

		public static string NormalizedNumberStringQuiet(string s, double multiplier, string nextWord, Number numberFromSUTime)
		{
			// normalizations from SUTime take precedence, if available
			if (numberFromSUTime != null)
			{
				double v = double.ValueOf(numberFromSUTime.ToString());
				return double.ToString(v * multiplier);
			}
			// clean up string
			string origSClean = s.ReplaceAll("[\t\n\x0\f\r]", string.Empty);
			if (allSpaces.Matcher(origSClean).Matches())
			{
				return s;
			}
			string[] origSSplit = origSClean.Split(" ");
			s = s.ReplaceAll("[ \t\n\x0\f\r]", string.Empty);
			//see if it looks like european style
			s = ConvertToAmerican(s);
			// remove parenthesis around numbers
			// if PTBTokenized, this next bit should be a no-op
			// in some contexts parentheses might indicate a negative number, but ignore that.
			if (s.StartsWith("(") && s.EndsWith(")"))
			{
				s = Sharpen.Runtime.Substring(s, 1, s.Length - 1);
			}
			s = s.ToLower();
			// get multipliers like "billion"
			bool foundMultiplier = false;
			foreach (KeyValuePair<string, double> stringDoubleEntry in moneyMultipliers)
			{
				string moneyTag = stringDoubleEntry.Key;
				if (s.Contains(moneyTag))
				{
					// if (DEBUG) {err.println("Quantifiable: Found "+ moneyTag);}
					//special case check: m can mean either meters or million - if nextWord is high or long, we assume meters - this is a huge and bad hack!!!
					if (moneyTag.Equals("m") && (nextWord.Equals("high") || nextWord.Equals("long")))
					{
						continue;
					}
					s = s.ReplaceAll(moneyTag, string.Empty);
					multiplier *= stringDoubleEntry.Value;
					foundMultiplier = true;
				}
			}
			foreach (KeyValuePair<string, int> stringIntegerEntry in moneyMultipliers2)
			{
				string moneyTag = stringIntegerEntry.Key;
				Matcher m = Pattern.Compile(moneyTag).Matcher(s);
				if (m.Find())
				{
					// if(DEBUG){err.println("Quantifiable: Found "+ moneyTag);}
					multiplier *= stringIntegerEntry.Value;
					foundMultiplier = true;
					int start = m.Start(1);
					int end = m.End(1);
					// err.print("Deleting from " + s);
					s = Sharpen.Runtime.Substring(s, 0, start) + Sharpen.Runtime.Substring(s, end);
				}
			}
			// err.println("; Result is " + s);
			if (!foundMultiplier)
			{
				EditDistance ed = new EditDistance();
				foreach (KeyValuePair<string, double> stringDoubleEntry_1 in moneyMultipliers)
				{
					string moneyTag = stringDoubleEntry_1.Key;
					if (IsOneSubstitutionMatch(origSSplit[origSSplit.Length - 1], moneyTag, ed))
					{
						s = s.ReplaceAll(moneyTag, string.Empty);
						multiplier *= stringDoubleEntry_1.Value;
					}
				}
			}
			// handle numbers written in words
			string[] parts = s.Split("[ -]");
			bool processed = false;
			double dd = 0.0;
			foreach (string part in parts)
			{
				if (wordsToValues.ContainsKey(part))
				{
					dd += wordsToValues.GetCount(part);
					processed = true;
				}
				else
				{
					string partMatch = GetOneSubstitutionMatch(part, wordsToValues.KeySet());
					if (partMatch != null)
					{
						dd += wordsToValues.GetCount(partMatch);
						processed = true;
					}
				}
			}
			if (processed)
			{
				dd *= multiplier;
				return double.ToString(dd);
			}
			// handle numbers written as numbers
			//  s = s.replaceAll("-", ""); //This is bad: it lets 22-7 be the number 227!
			s = s.ReplaceAll("[A-Za-z]", string.Empty);
			// handle scores or range
			Matcher m2 = scorePattern.Matcher(s);
			if (m2.Matches())
			{
				double d1 = double.ParseDouble(m2.Group(1));
				double d2 = double.ParseDouble(m2.Group(2));
				return double.ToString(d1) + " - " + double.ToString(d2);
			}
			// check for hyphenated word like 4-Ghz: delete final -
			if (s.EndsWith("-"))
			{
				s = Sharpen.Runtime.Substring(s, 0, s.Length - 1);
			}
			Matcher m_1 = moneyPattern.Matcher(s);
			if (m_1.Matches())
			{
				try
				{
					double d = 0.0;
					if (m_1.Group(2) != null && !m_1.Group(2).IsEmpty())
					{
						d = double.ParseDouble(m_1.Group(2));
					}
					if (m_1.Group(3) != null && !m_1.Group(3).IsEmpty())
					{
						d += double.ParseDouble(m_1.Group(3));
					}
					if (d == 0.0 && multiplier != 1.0)
					{
						// we'd found a multiplier
						d = 1.0;
					}
					d *= multiplier;
					return double.ToString(d);
				}
				catch (Exception e)
				{
					return null;
				}
			}
			else
			{
				if (multiplier != 1.0)
				{
					// we found a multiplier, so we have something
					return double.ToString(multiplier);
				}
				else
				{
					return null;
				}
			}
		}

		public static string NormalizedOrdinalString(string s, Number numberFromSUTime)
		{
			return NormalizedOrdinalStringQuiet(s, numberFromSUTime);
		}

		private static readonly Pattern numberPattern = Pattern.Compile("([0-9.]+)");

		private static string NormalizedOrdinalStringQuiet(string s, Number numberFromSUTime)
		{
			// clean up string
			s = s.ReplaceAll("[ \t\n\x0\f\r,]", string.Empty);
			// remove parenthesis around numbers
			// if PTBTokenized, this next bit should be a no-op
			// in some contexts parentheses might indicate a negative number, but ignore that.
			if (s.StartsWith("(") && s.EndsWith(")"))
			{
				s = Sharpen.Runtime.Substring(s, 1, s.Length - 1);
			}
			s = s.ToLower();
			if (char.IsDigit(s[0]))
			{
				Matcher matcher = numberPattern.Matcher(s);
				matcher.Find();
				// just parse number part, assuming last two letters are st/nd/rd
				return NormalizedNumberStringQuiet(matcher.Group(), 1.0, string.Empty, numberFromSUTime);
			}
			else
			{
				if (ordinalsToValues.ContainsKey(s))
				{
					return double.ToString(ordinalsToValues.GetCount(s));
				}
				else
				{
					string val = GetOneSubstitutionMatch(s, ordinalsToValues.KeySet());
					if (val != null)
					{
						return double.ToString(ordinalsToValues.GetCount(val));
					}
					else
					{
						return null;
					}
				}
			}
		}

		public static string NormalizedPercentString(string s, Number numberFromSUTime)
		{
			s = s.ReplaceAll("\\s", string.Empty);
			s = s.ToLower();
			if (s.Contains("%") || s.Contains("percent"))
			{
				s = s.ReplaceAll("percent|%", string.Empty);
			}
			string norm = NormalizedNumberStringQuiet(s, 1.0, string.Empty, numberFromSUTime);
			if (norm == null)
			{
				return null;
			}
			return '%' + norm;
		}

		/// <summary>Fetches the first encountered Number set by SUTime</summary>
		private static Number FetchNumberFromSUTime<E>(IList<E> l)
			where E : ICoreMap
		{
			foreach (E e in l)
			{
				if (e.ContainsKey(typeof(CoreAnnotations.NumericCompositeValueAnnotation)))
				{
					return e.Get(typeof(CoreAnnotations.NumericCompositeValueAnnotation));
				}
			}
			return null;
		}

		private static Timex FetchTimexFromSUTime<E>(IList<E> l)
			where E : ICoreMap
		{
			foreach (E e in l)
			{
				if (e.ContainsKey(typeof(TimeAnnotations.TimexAnnotation)))
				{
					return e.Get(typeof(TimeAnnotations.TimexAnnotation));
				}
			}
			return null;
		}

		private static IList<E> ProcessEntity<E>(IList<E> l, string entityType, string compModifier, string nextWord)
			where E : ICoreMap
		{
			System.Diagnostics.Debug.Assert((quantifiable.Contains(entityType)));
			string s;
			if (entityType.Equals("TIME"))
			{
				s = TimeEntityToString(l);
			}
			else
			{
				s = SingleEntityToString(l);
			}
			Number numberFromSUTime = FetchNumberFromSUTime(l);
			Timex timexFromSUTime = FetchTimexFromSUTime(l);
			string p = null;
			switch (entityType)
			{
				case "NUMBER":
				{
					p = string.Empty;
					if (compModifier != null)
					{
						p = compModifier;
					}
					string q = NormalizedNumberString(s, nextWord, numberFromSUTime);
					if (q != null)
					{
						p = p.Concat(q);
					}
					else
					{
						p = null;
					}
					break;
				}

				case "ORDINAL":
				{
					p = NormalizedOrdinalString(s, numberFromSUTime);
					break;
				}

				case "DURATION":
				{
					// SUTime marks some ordinals, e.g., "22nd time", as durations
					p = NormalizedDurationString(s, timexFromSUTime);
					break;
				}

				case "MONEY":
				{
					p = string.Empty;
					if (compModifier != null)
					{
						p = compModifier;
					}
					string q = NormalizedMoneyString(s, numberFromSUTime);
					if (q != null)
					{
						p = p.Concat(q);
					}
					else
					{
						p = null;
					}
					break;
				}

				case "DATE":
				{
					p = NormalizedDateString(s, timexFromSUTime);
					break;
				}

				case "TIME":
				{
					p = string.Empty;
					if (compModifier != null && !compModifier.Matches("am|pm"))
					{
						p = compModifier;
					}
					string q = NormalizedTimeString(s, compModifier != null ? compModifier : string.Empty, timexFromSUTime);
					if (q != null && q.Length == 1 && !q.Equals("D"))
					{
						p = p.Concat(q);
					}
					else
					{
						p = q;
					}
					break;
				}

				case "PERCENT":
				{
					p = string.Empty;
					if (compModifier != null)
					{
						p = compModifier;
					}
					string q = NormalizedPercentString(s, numberFromSUTime);
					if (q != null)
					{
						p = p.Concat(q);
					}
					else
					{
						p = null;
					}
					break;
				}
			}
			int i = 0;
			foreach (E wi in l)
			{
				if (p != null)
				{
					wi.Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), p);
				}
				//currently we also write this into the answers;
				//wi.setAnswer(wi.get(CoreAnnotations.AnswerAnnotation.class)+"("+p+")");
				i++;
			}
			return l;
		}

		/// <param name="l">The list of tokens in a time entity</param>
		/// <returns>the word in the time word list that should be normalized</returns>
		private static string TimeEntityToString<E>(IList<E> l)
			where E : ICoreMap
		{
			string entityType = l[0].Get(typeof(CoreAnnotations.AnswerAnnotation));
			int size = l.Count;
			foreach (E w in l)
			{
				System.Diagnostics.Debug.Assert((w.Get(typeof(CoreAnnotations.AnswerAnnotation)) == null || w.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals(entityType)));
				Matcher m = timePattern.Matcher(w.Get(typeof(CoreAnnotations.TextAnnotation)));
				if (m.Matches())
				{
					return w.Get(typeof(CoreAnnotations.TextAnnotation));
				}
			}
			return l[size - 1].Get(typeof(CoreAnnotations.TextAnnotation));
		}

		/// <summary>
		/// Takes the output of an
		/// <see cref="AbstractSequenceClassifier{IN}"/>
		/// and marks up
		/// each document by normalizing quantities. Each
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// in any
		/// of the documents which is normalizable will receive a "normalizedQuantity"
		/// attribute.
		/// </summary>
		/// <param name="l">
		/// a
		/// <see cref="System.Collections.IList{E}"/>
		/// of
		/// <see cref="System.Collections.IList{E}"/>
		/// s of
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s
		/// </param>
		/// <returns>The list with normalized entity fields filled in</returns>
		public static IList<IList<CoreLabel>> NormalizeClassifierOutput(IList<IList<CoreLabel>> l)
		{
			foreach (IList<CoreLabel> doc in l)
			{
				AddNormalizedQuantitiesToEntities(doc);
			}
			return l;
		}

		private const string lessEqualThreeWords = "no (?:more|greater|higher) than|as (?:many|much) as";

		private const string greaterEqualThreeWords = "no (?:less|fewer) than|as few as";

		private const string greaterThanTwoWords = "(?:more|greater|larger|higher) than";

		private const string lessThanTwoWords = "(?:less|fewer|smaller) than|at most";

		private const string lessEqualTwoWords = "no (?:more|greater)_than|or less|up to";

		private const string greaterEqualTwoWords = "no (?:less|fewer)_than|or more|at least";

		private const string approxTwoWords = "just (?:over|under)|or so";

		private const string greaterThanOneWord = "(?:above|over|more_than|greater_than)";

		private const string lessThanOneWord = "(?:below|under|less_than)";

		private const string lessEqualOneWord = "(?:up_to|within)";

		private const string approxOneWord = "(?:approximately|estimated|nearly|around|about|almost|just_over|just_under)";

		private const string other = "other";

		// note that ones like "nearly" or "almost" can be above or below:
		// "almost 500 killed", "almost zero inflation"
		/// <summary>
		/// Takes the strings of the three previous and next words to a quantity and
		/// detects a
		/// quantity modifier like "less than", "more than", etc.
		/// </summary>
		/// <remarks>
		/// Takes the strings of the three previous and next words to a quantity and
		/// detects a
		/// quantity modifier like "less than", "more than", etc.
		/// Any of these words may be
		/// <see langword="null"/>
		/// or an empty String.
		/// </remarks>
		private static string DetectQuantityModifier<E>(IList<E> list, int beforeIndex, int afterIndex)
			where E : ICoreMap
		{
			string prev = (beforeIndex >= 0) ? list[beforeIndex].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string prev2 = (beforeIndex - 1 >= 0) ? list[beforeIndex - 1].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string prev3 = (beforeIndex - 2 >= 0) ? list[beforeIndex - 2].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			int sz = list.Count;
			string next = (afterIndex < sz) ? list[afterIndex].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string next2 = (afterIndex + 1 < sz) ? list[afterIndex + 1].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string next3 = (afterIndex + 2 < sz) ? list[afterIndex + 2].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string longPrev = prev3 + ' ' + prev2 + ' ' + prev;
			if (longPrev.Matches(lessEqualThreeWords))
			{
				return "<=";
			}
			if (longPrev.Matches(greaterEqualThreeWords))
			{
				return ">=";
			}
			longPrev = prev2 + ' ' + prev;
			if (longPrev.Matches(greaterThanTwoWords))
			{
				return ">";
			}
			if (longPrev.Matches(lessEqualTwoWords))
			{
				return "<=";
			}
			if (longPrev.Matches(greaterEqualTwoWords))
			{
				return ">=";
			}
			if (longPrev.Matches(lessThanTwoWords))
			{
				return "<";
			}
			if (longPrev.Matches(approxTwoWords))
			{
				return "~";
			}
			string longNext = next + ' ' + next2;
			if (longNext.Matches(greaterEqualTwoWords))
			{
				return ">=";
			}
			if (longNext.Matches(lessEqualTwoWords))
			{
				return "<=";
			}
			if (prev.Matches(greaterThanOneWord))
			{
				return ">";
			}
			if (prev.Matches(lessThanOneWord))
			{
				return "<";
			}
			if (prev.Matches(lessEqualOneWord))
			{
				return "<=";
			}
			if (prev.Matches(approxOneWord))
			{
				return "~";
			}
			if (next.Matches(other))
			{
				return ">=";
			}
			return null;
		}

		private const string earlyOneWord = "early";

		private const string earlyTwoWords = "(?:dawn|eve|beginning) of";

		private const string earlyThreeWords = "early in the";

		private const string lateOneWord = "late";

		private const string lateTwoWords = "late at|end of";

		private const string lateThreeWords = "end of the";

		private const string middleTwoWords = "(?:middle|midst) of";

		private const string middleThreeWords = "(?:middle|midst) of the";

		private const string amOneWord = "[Aa]\\.?[Mm]\\.?";

		private const string pmOneWord = "[Pp]\\.?[Mm]\\.?";

		private const string amThreeWords = "in the morning";

		private const string pmTwoWords = "at night";

		private const string pmThreeWords = "in the (?:afternoon|evening)";

		/// <summary>
		/// Takes the strings of the three previous words to a quantity and detects a
		/// quantity modifier like "less than", "more than", etc.
		/// </summary>
		/// <remarks>
		/// Takes the strings of the three previous words to a quantity and detects a
		/// quantity modifier like "less than", "more than", etc.
		/// Any of these words may be
		/// <see langword="null"/>
		/// or an empty String.
		/// </remarks>
		private static string DetectTimeOfDayModifier<E>(IList<E> list, int beforeIndex, int afterIndex)
			where E : ICoreMap
		{
			string prev = (beforeIndex >= 0) ? list[beforeIndex].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string prev2 = (beforeIndex - 1 >= 0) ? list[beforeIndex - 1].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string prev3 = (beforeIndex - 2 >= 0) ? list[beforeIndex - 2].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			int sz = list.Count;
			string next = (afterIndex < sz) ? list[afterIndex].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string next2 = (afterIndex + 1 < sz) ? list[afterIndex + 1].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string next3 = (afterIndex + 2 < sz) ? list[afterIndex + 2].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string longPrev = prev3 + ' ' + prev2 + ' ' + prev;
			if (longPrev.Matches(earlyThreeWords))
			{
				return "E";
			}
			else
			{
				if (longPrev.Matches(lateThreeWords))
				{
					return "L";
				}
				else
				{
					if (longPrev.Matches(middleThreeWords))
					{
						return "M";
					}
				}
			}
			longPrev = prev2 + ' ' + prev;
			if (longPrev.Matches(earlyTwoWords))
			{
				return "E";
			}
			else
			{
				if (longPrev.Matches(lateTwoWords))
				{
					return "L";
				}
				else
				{
					if (longPrev.Matches(middleTwoWords))
					{
						return "M";
					}
				}
			}
			if (prev.Matches(earlyOneWord) || prev2.Matches(earlyOneWord))
			{
				return "E";
			}
			else
			{
				if (prev.Matches(lateOneWord) || prev2.Matches(lateOneWord))
				{
					return "L";
				}
			}
			string longNext = next3 + ' ' + next2 + ' ' + next;
			if (longNext.Matches(pmThreeWords))
			{
				return "pm";
			}
			if (longNext.Matches(amThreeWords))
			{
				return "am";
			}
			longNext = next2 + ' ' + next;
			if (longNext.Matches(pmTwoWords))
			{
				return "pm";
			}
			if (next.Matches(amOneWord) || next2.Matches("morning") || next3.Matches("morning"))
			{
				return "am";
			}
			if (next.Matches(pmOneWord) || next2.Matches("afternoon") || next3.Matches("afternoon") || next2.Matches("night") || next3.Matches("night") || next2.Matches("evening") || next3.Matches("evening"))
			{
				return "pm";
			}
			return string.Empty;
		}

		/// <summary>
		/// Identifies contiguous MONEY, TIME, DATE, or PERCENT entities
		/// and tags each of their constituents with a "normalizedQuantity"
		/// label which contains the appropriate normalized string corresponding to
		/// the full quantity.
		/// </summary>
		/// <remarks>
		/// Identifies contiguous MONEY, TIME, DATE, or PERCENT entities
		/// and tags each of their constituents with a "normalizedQuantity"
		/// label which contains the appropriate normalized string corresponding to
		/// the full quantity. Quantities are not concatenated
		/// </remarks>
		/// <param name="l">
		/// A list of
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// s representing a single
		/// document.  Note: the Labels are updated in place.
		/// </param>
		public static void AddNormalizedQuantitiesToEntities<E>(IList<E> l)
			where E : ICoreMap
		{
			AddNormalizedQuantitiesToEntities(l, false, false);
		}

		public static void AddNormalizedQuantitiesToEntities<E>(IList<E> l, bool concatenate)
			where E : ICoreMap
		{
			AddNormalizedQuantitiesToEntities(l, concatenate, false);
		}

		public static bool IsCompatible<E>(string tag, E prev, E cur)
			where E : ICoreMap
		{
			if ("NUMBER".Equals(tag) || "ORDINAL".Equals(tag) || "PERCENT".Equals(tag))
			{
				// Get NumericCompositeValueAnnotation and say two entities are incompatible if they are different
				Number n1 = cur.Get(typeof(CoreAnnotations.NumericCompositeValueAnnotation));
				Number n2 = prev.Get(typeof(CoreAnnotations.NumericCompositeValueAnnotation));
				// Special case for % sign
				if ("PERCENT".Equals(tag) && n1 == null)
				{
					return true;
				}
				bool compatible = Objects.Equals(n1, n2);
				if (!compatible)
				{
					return false;
				}
			}
			if ("TIME".Equals(tag) || "SET".Equals(tag) || "DATE".Equals(tag) || "DURATION".Equals(tag))
			{
				// Check timex...
				Timex timex1 = cur.Get(typeof(TimeAnnotations.TimexAnnotation));
				Timex timex2 = prev.Get(typeof(TimeAnnotations.TimexAnnotation));
				string tid1 = (timex1 != null) ? timex1.Tid() : null;
				string tid2 = (timex2 != null) ? timex2.Tid() : null;
				bool compatible = Objects.Equals(tid1, tid2);
				if (!compatible)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Identifies contiguous MONEY, TIME, DATE, or PERCENT entities
		/// and tags each of their constituents with a "normalizedQuantity"
		/// label which contains the appropriate normalized string corresponding to
		/// the full quantity.
		/// </summary>
		/// <param name="list">
		/// A list of
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// s representing a single
		/// document.  Note: the Labels are updated in place.
		/// </param>
		/// <param name="concatenate">true if quantities should be concatenated into one label, false otherwise</param>
		public static void AddNormalizedQuantitiesToEntities<E>(IList<E> list, bool concatenate, bool usesSUTime)
			where E : ICoreMap
		{
			IList<E> toRemove = new List<E>();
			// list for storing those objects we're going to remove at the end (e.g., if concatenate, we replace 3 November with 3_November, have to remove one of the originals)
			// Goes through tokens and tries to fix up NER annotations
			FixupNerBeforeNormalization(list);
			// Now that NER tags has been fixed up, we do another pass to add the normalization
			string prevNerTag = BackgroundSymbol;
			string timeModifier = string.Empty;
			int beforeIndex = -1;
			List<E> collector = new List<E>();
			for (int i = 0; i <= sz; i++)
			{
				E wi = null;
				string currNerTag = null;
				string nextWord = string.Empty;
				if (i < list.Count)
				{
					wi = list[i];
					if ((i + 1) < sz)
					{
						nextWord = list[i + 1].Get(typeof(CoreAnnotations.TextAnnotation));
						if (nextWord == null)
						{
							nextWord = string.Empty;
						}
					}
					currNerTag = wi.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
					if ("TIME".Equals(currNerTag))
					{
						if (timeModifier.IsEmpty())
						{
							timeModifier = DetectTimeOfDayModifier(list, i - 1, i + 1);
						}
					}
				}
				E wprev = (i > 0) ? list[i - 1] : null;
				// if the current wi is a non-continuation and the last one was a
				// quantity, we close and process the last segment.
				if ((currNerTag == null || !currNerTag.Equals(prevNerTag) || !IsCompatible(prevNerTag, wprev, wi)) && quantifiable.Contains(prevNerTag))
				{
					string compModifier = null;
					switch (prevNerTag)
					{
						case "TIME":
						{
							// special handling of TIME
							ProcessEntity(collector, prevNerTag, timeModifier, nextWord);
							break;
						}

						case ("DATE"):
						{
							//detect date range modifiers by looking at nearby words
							E prev = (beforeIndex >= 0) ? list[beforeIndex] : null;
							if (usesSUTime)
							{
								// If sutime was used don't do any weird relabeling of more things as DATE
								compModifier = DetectDateRangeModifier(prev);
							}
							else
							{
								compModifier = DetectDateRangeModifier(collector, list, beforeIndex, i);
							}
							if (!compModifier.Equals(ISODateInstance.BoundedRange))
							{
								ProcessEntity(collector, prevNerTag, compModifier, nextWord);
							}
							//now repair this date if it's more than one word
							//doesn't really matter which one we keep ideally we should be doing lemma/etc matching anyway
							//but we vaguely try to deal with this by choosing the NNP or the CD
							if (concatenate)
							{
								ConcatenateNumericString(collector, toRemove);
							}
							break;
						}

						default:
						{
							// detect "more than", "nearly", etc. by looking at nearby words.
							if (prevNerTag.Equals("MONEY") || prevNerTag.Equals("NUMBER") || prevNerTag.Equals("PERCENT"))
							{
								compModifier = DetectQuantityModifier(list, beforeIndex, i);
							}
							ProcessEntity(collector, prevNerTag, compModifier, nextWord);
							if (concatenate)
							{
								ConcatenateNumericString(collector, toRemove);
							}
							break;
						}
					}
					collector = new List<E>();
					timeModifier = string.Empty;
				}
				// if the current wi is a quantity, we add it to the collector.
				// if its the first word in a quantity, we record index before it
				if (quantifiable.Contains(currNerTag))
				{
					if (collector.IsEmpty())
					{
						beforeIndex = i - 1;
					}
					collector.Add(wi);
				}
				prevNerTag = currNerTag;
			}
			if (concatenate)
			{
				list.RemoveAll(toRemove);
			}
			IList<E> moreRemoves = new List<E>();
			for (int i_1 = 0; i_1 < sz; i_1++)
			{
				E wi = list[i_1];
				Sharpen.Collections.AddAll(moreRemoves, DetectTwoSidedRangeModifier(wi, list, i_1 - 1, i_1 + 1, concatenate));
			}
			if (concatenate)
			{
				list.RemoveAll(moreRemoves);
			}
		}

		private static void FixupNerBeforeNormalization<E>(IList<E> list)
			where E : ICoreMap
		{
			// Goes through tokens and tries to fix up NER annotations
			string prevNerTag = BackgroundSymbol;
			string prevNumericType = null;
			Timex prevTimex = null;
			for (int i = 0; i < sz; i++)
			{
				E wi = list[i];
				Timex timex = wi.Get(typeof(TimeAnnotations.TimexAnnotation));
				string numericType = wi.Get(typeof(CoreAnnotations.NumericCompositeTypeAnnotation));
				string curWord = (wi.Get(typeof(CoreAnnotations.TextAnnotation)) != null ? wi.Get(typeof(CoreAnnotations.TextAnnotation)) : string.Empty);
				string currNerTag = wi.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
				// Attempts repairs to NER tags only if not marked by SUTime already
				if (timex == null && numericType == null)
				{
					// repairs commas in between dates...  String constant first in equals() in case key has null value....
					if ((i + 1) < sz && ",".Equals(wi.Get(typeof(CoreAnnotations.TextAnnotation))) && "DATE".Equals(prevNerTag))
					{
						if (prevTimex == null && prevNumericType == null)
						{
							E nextToken = list[i + 1];
							string nextNER = nextToken.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
							if (nextNER != null && nextNER.Equals("DATE"))
							{
								wi.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), "DATE");
							}
						}
					}
					//repairs mistagged multipliers after a numeric quantity
					if (!curWord.IsEmpty() && (moneyMultipliers.Contains(curWord) || (GetOneSubstitutionMatch(curWord, moneyMultipliers.Keys) != null)) && prevNerTag != null && (prevNerTag.Equals("MONEY") || prevNerTag.Equals("NUMBER")))
					{
						wi.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), prevNerTag);
					}
					//repairs four digit ranges (2002-2004) that have not been tagged as years - maybe bad? (empirically useful)
					if (curWord.Contains("-"))
					{
						string[] sides = curWord.Split("-");
						if (sides.Length == 2)
						{
							try
							{
								int first = System.Convert.ToInt32(sides[0]);
								int second = System.Convert.ToInt32(sides[1]);
								//they're both integers, see if they're both between 1000-3000 (likely years)
								if (1000 <= first && first <= 3000 && 1000 <= second && second <= 3000)
								{
									wi.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), "DATE");
									string dateStr = new ISODateInstance(new ISODateInstance(sides[0]), new ISODateInstance(sides[1])).GetDateString();
									wi.Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), dateStr);
									continue;
								}
							}
							catch (Exception)
							{
							}
						}
					}
					// they weren't numbers.
					// Marks time units as DURATION if they are preceded by a NUMBER tag.  e.g. "two years" or "5 minutes"
					if (timeUnitWords.Contains(curWord) && (currNerTag == null || !"DURATION".Equals(currNerTag)) && ("NUMBER".Equals(prevNerTag)))
					{
						wi.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), "DURATION");
						for (int j = i - 1; j > 0; j--)
						{
							E prev = list[j];
							if ("NUMBER".Equals(prev.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation))))
							{
								prev.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), "DURATION");
							}
						}
					}
				}
				else
				{
					// Fixup SUTime marking of twenty-second
					if ("DURATION".Equals(currNerTag) && ordinalsToValues.ContainsKey(curWord) && curWord.EndsWith("second") && timex.Text().Equals(curWord))
					{
						wi.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), "ORDINAL");
					}
				}
				prevNerTag = currNerTag;
				prevNumericType = numericType;
				prevTimex = timex;
			}
		}

		/// <summary>
		/// Runs a deterministic named entity classifier which is good at recognizing
		/// numbers and money and date expressions not recognized by our statistical
		/// NER.
		/// </summary>
		/// <remarks>
		/// Runs a deterministic named entity classifier which is good at recognizing
		/// numbers and money and date expressions not recognized by our statistical
		/// NER.  It then changes any BACKGROUND_SYMBOL's from the list to
		/// the value tagged by this deterministic NER.
		/// It then adds normalized values for quantifiable entities.
		/// </remarks>
		/// <param name="l">A document to label</param>
		/// <returns>The list with results of 'specialized' (rule-governed) NER filled in</returns>
		public static IList<E> ApplySpecializedNER<E>(IList<E> l)
			where E : CoreLabel
		{
			int sz = l.Count;
			// copy l
			IList<CoreLabel> copyL = new List<CoreLabel>(sz);
			for (int i = 0; i < sz; i++)
			{
				copyL.Add(new CoreLabel(l[i]));
			}
			// run NumberSequenceClassifier
			AbstractSequenceClassifier<CoreLabel> nsc = new NumberSequenceClassifier();
			copyL = nsc.Classify(copyL);
			// update entity only if it was not O
			for (int i_1 = 0; i_1 < sz; i_1++)
			{
				E before = l[i_1];
				CoreLabel nscAnswer = copyL[i_1];
				if (before.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)) == null && before.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Equals(BackgroundSymbol) && (nscAnswer.Get(typeof(CoreAnnotations.AnswerAnnotation)) != null && !nscAnswer
					.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals(BackgroundSymbol)))
				{
					log.Info("Quantifiable: updating class for " + before.Get(typeof(CoreAnnotations.TextAnnotation)) + '/' + before.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)) + " to " + nscAnswer.Get(typeof(CoreAnnotations.AnswerAnnotation)));
					before.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), nscAnswer.Get(typeof(CoreAnnotations.AnswerAnnotation)));
				}
			}
			AddNormalizedQuantitiesToEntities(l);
			return l;
		}
		// end applySpecializedNER
	}
}
