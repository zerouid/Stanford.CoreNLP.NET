using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Regexp;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.IE
{
	/// <summary>Provides functions for converting words to numbers.</summary>
	/// <remarks>
	/// Provides functions for converting words to numbers.
	/// Unlike QuantifiableEntityNormalizer that normalizes various
	/// types of quantifiable entities like money and dates,
	/// NumberNormalizer only normalizes numeric expressions
	/// (e.g., one =&gt; 1, two hundred =&gt; 200.0 )
	/// This code is somewhat hacked together, so should be reworked.
	/// There is a library in perl for parsing english numbers:
	/// http://blog.cordiner.net/2010/01/02/parsing-english-numbers-with-perl/
	/// TODO: To be merged into QuantifiableEntityNormalizer.
	/// It can be used by QuantifiableEntityNormalizer
	/// to first convert numbers expressed as words
	/// into numeric quantities before figuring
	/// out how to do higher level combos
	/// (like one hundred dollars and five cents)
	/// TODO: Known to not handle the following:
	/// oh: two oh one
	/// non-integers: one and a half, one point five, three fifth
	/// funky numbers: pi
	/// TODO: This class is very language dependent
	/// Should really be AmericanEnglishNumberNormalizer
	/// TODO: Make things not static
	/// </remarks>
	/// <author>Angel Chang</author>
	public class NumberNormalizer
	{
		private NumberNormalizer()
		{
		}

		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.NumberNormalizer));

		// static class
		// TODO: make this not static, let different NumberNormalizers use different loggers
		public static void SetVerbose(bool verbose)
		{
			if (verbose)
			{
				RedwoodConfiguration.DebugLevel().Apply();
			}
			else
			{
				RedwoodConfiguration.ErrorLevel().Apply();
			}
		}

		private static readonly Pattern numUnitPattern = Pattern.Compile("(?i)(hundred|thousand|million|billion|trillion)");

		private static readonly Pattern numEndUnitPattern = Pattern.Compile("(?i)(gross|dozen|score)");

		private static readonly Pattern numNotStandaloneUnitPattern = Pattern.Compile("(?i)(gross|score)");

		private static readonly Pattern numberTermPattern = Pattern.Compile("(?i)(zero|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety|hundred|thousand|million|billion|trillion|first|second|third|fourth|fifth|sixth|seventh|eighth|ninth|tenth|eleventh|twelfth|thirteenth|fourteenth|fifteenth|sixteenth|seventeenth|eighteenth|nineteenth|twentieth|thirtieth|fortieth|fiftieth|sixtieth|seventieth|eightieth|ninetieth|hundred?th|thousandth|millionth|billionth|trillionth)"
			);

		private static readonly Pattern numberTermPattern2 = Pattern.Compile("(?i)(" + numberTermPattern.Pattern() + "(-" + numberTermPattern.Pattern() + ")?)");

		private static readonly Pattern ordinalUnitPattern = Pattern.Compile("(?i)(hundredth|thousandth|millionth)");

		private static readonly Pattern digitsPattern = Pattern.Compile("\\d+");

		private static readonly Pattern digitsPatternExtended = Pattern.Compile("(\\d+\\.?\\d*)(dozen|score|hundred|thousand|million|billion|trillion)?");

		private static readonly Pattern numPattern = Pattern.Compile("[-+]?(?:\\d+(?:,\\d\\d\\d)*(?:\\.\\d*)?|\\.\\d+)");

		private static readonly Pattern numRangePattern = Pattern.Compile("(" + numPattern.Pattern() + ")-(" + numPattern.Pattern() + ")");

		private static readonly IDictionary<string, Number> word2NumMap = Generics.NewHashMap();

		static NumberNormalizer()
		{
			// Need these in order - first must come after 21st
			//public static final Pattern teOrdinalWords = Pattern.compile("(?i)(tenth|eleventh|twelfth|thirteenth|fourteenth|fifteenth|sixteenth|seventeenth|eighteenth|nineteenth|twentieth|twenty-first|twenty-second|twenty-third|twenty-fourth|twenty-fifth|twenty-sixth|twenty-seventh|twenty-eighth|twenty-ninth|thirtieth|thirty-first|first|second|third|fourth|fifth|sixth|seventh|eighth|ninth)");
			//static final Pattern teNumOrds = Pattern.compile("(?i)([23]?1-?st|11-?th|[23]?2-?nd|12-?th|[12]?3-?rd|13-?th|[12]?[4-90]-?th|30-?th)");
			//static final Pattern unitNumsPattern = Pattern.compile("(?i)(one|two|three|four|five|six|seven|eight|nine)");
			//static final Pattern uniqueNumsPattern  = Pattern.compile("(?i)(ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen)");
			//static final Pattern tensNumsPattern = Pattern.compile("(?i)(twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety)");
			// private static final String[] unitWords = {"trillion", "billion", "million", "thousand", "hundred"};
			// private static final String[] endUnitWords = {"gross", "dozen", "score"};
			// Converts numbers in words to numeric form
			// works through trillions
			// this is really just second-guessing the tokenizer
			// private static final Pattern[] endUnitWordsPattern = new Pattern[endUnitWords.length];
			// private static final Pattern[] unitWordsPattern = new Pattern[unitWords.length];
			// static {
			//   int i = 0;
			//   for (String uw:endUnitWords) {
			//     endUnitWordsPattern[i] = Pattern.compile("(.*)\\s*" + Pattern.quote(uw) + "\\s*(.*)");
			//     i++;
			//   }
			//   int ii = 0;
			//   for (String uw:unitWords) {
			//     unitWordsPattern[ii] = Pattern.compile("(.*)\\s*" + Pattern.quote(uw) + "\\s*(.*)");
			//     ii++;
			//   }
			// }
			// TODO: similar to QuantifiableEntityNormalizer.wordsToValues
			//       QuantifiableEntityNormalizer also has bn (for billion)
			//       should consolidate
			//       here we use Number representation instead of double...
			// Special words for numbers
			word2NumMap["dozen"] = 12;
			word2NumMap["score"] = 20;
			word2NumMap["gross"] = 144;
			word2NumMap["quarter"] = 0.25;
			word2NumMap["half"] = 0.5;
			word2NumMap["oh"] = 0;
			word2NumMap["a"] = 1;
			word2NumMap["an"] = 1;
			// Standard words for numbers
			word2NumMap["zero"] = 0;
			word2NumMap["one"] = 1;
			word2NumMap["two"] = 2;
			word2NumMap["three"] = 3;
			word2NumMap["four"] = 4;
			word2NumMap["five"] = 5;
			word2NumMap["six"] = 6;
			word2NumMap["seven"] = 7;
			word2NumMap["eight"] = 8;
			word2NumMap["nine"] = 9;
			word2NumMap["ten"] = 10;
			word2NumMap["eleven"] = 11;
			word2NumMap["twelve"] = 12;
			word2NumMap["thirteen"] = 13;
			word2NumMap["fourteen"] = 14;
			word2NumMap["fifteen"] = 15;
			word2NumMap["sixteen"] = 16;
			word2NumMap["seventeen"] = 17;
			word2NumMap["eighteen"] = 18;
			word2NumMap["nineteen"] = 19;
			word2NumMap["twenty"] = 20;
			word2NumMap["thirty"] = 30;
			word2NumMap["forty"] = 40;
			word2NumMap["fifty"] = 50;
			word2NumMap["sixty"] = 60;
			word2NumMap["seventy"] = 70;
			word2NumMap["eighty"] = 80;
			word2NumMap["ninety"] = 90;
			word2NumMap["hundred"] = 100;
			word2NumMap["thousand"] = 1000;
			word2NumMap["million"] = 1000000;
			word2NumMap["billion"] = 1000000000;
			word2NumMap["trillion"] = 1000000000000L;
		}

		private static readonly IDictionary<string, Number> ordWord2NumMap = Generics.NewHashMap();

		static NumberNormalizer()
		{
			// similar to QuantifiableEntityNormalizer.ordinalsToValues
			ordWord2NumMap["zeroth"] = 0;
			ordWord2NumMap["first"] = 1;
			ordWord2NumMap["second"] = 2;
			ordWord2NumMap["third"] = 3;
			ordWord2NumMap["fourth"] = 4;
			ordWord2NumMap["fifth"] = 5;
			ordWord2NumMap["sixth"] = 6;
			ordWord2NumMap["seventh"] = 7;
			ordWord2NumMap["eighth"] = 8;
			ordWord2NumMap["ninth"] = 9;
			ordWord2NumMap["tenth"] = 10;
			ordWord2NumMap["eleventh"] = 11;
			ordWord2NumMap["twelfth"] = 12;
			ordWord2NumMap["thirteenth"] = 13;
			ordWord2NumMap["fourteenth"] = 14;
			ordWord2NumMap["fifteenth"] = 15;
			ordWord2NumMap["sixteenth"] = 16;
			ordWord2NumMap["seventeenth"] = 17;
			ordWord2NumMap["eighteenth"] = 18;
			ordWord2NumMap["nineteenth"] = 19;
			ordWord2NumMap["twentieth"] = 20;
			ordWord2NumMap["thirtieth"] = 30;
			ordWord2NumMap["fortieth"] = 40;
			ordWord2NumMap["fiftieth"] = 50;
			ordWord2NumMap["sixtieth"] = 60;
			ordWord2NumMap["seventieth"] = 70;
			ordWord2NumMap["eightieth"] = 80;
			ordWord2NumMap["ninetieth"] = 90;
			ordWord2NumMap["hundredth"] = 100;
			ordWord2NumMap["hundreth"] = 100;
			// really a spelling error
			ordWord2NumMap["thousandth"] = 1000;
			ordWord2NumMap["millionth"] = 1000000;
			ordWord2NumMap["billionth"] = 1000000000;
			ordWord2NumMap["trillionth"] = 1000000000000L;
		}

		private static readonly Pattern alphaPattern = Pattern.Compile("([a-zA-Z]+)");

		private static readonly Pattern wsPattern = Pattern.Compile("\\s+");

		/// <summary>All the different shitty forms of unicode whitespace.</summary>
		private const string whitespaceCharsRegex = "[" + "\\u0009" + "\\u000A" + "\\u000B" + "\\u000C" + "\\u000D" + "\\u0020" + "\\u0085" + "\\u00A0" + "\\u1680" + "\\u180E" + "\\u2000" + "\\u2001" + "\\u2002" + "\\u2003" + "\\u2004" + "\\u2005" +
			 "\\u2006" + "\\u2007" + "\\u2008" + "\\u2009" + "\\u200A" + "\\u2028" + "\\u2029" + "\\u202F" + "\\u205F" + "\\u3000" + "]";

		// Seems to work better than quantifiable entity normalizer's numeric conversion
		/* dummy empty string for homogeneity */
		// CHARACTER TABULATION
		// LINE FEED (LF)
		// LINE TABULATION
		// FORM FEED (FF)
		// CARRIAGE RETURN (CR)
		// SPACE
		// NEXT LINE (NEL)
		// NO-BREAK SPACE
		// OGHAM SPACE MARK
		// MONGOLIAN VOWEL SEPARATOR
		// EN QUAD
		// EM QUAD
		// EN SPACE
		// EM SPACE
		// THREE-PER-EM SPACE
		// FOUR-PER-EM SPACE
		// SIX-PER-EM SPACE
		// FIGURE SPACE
		// PUNCTUATION SPACE
		// THIN SPACE
		// HAIR SPACE
		// LINE SEPARATOR
		// PARAGRAPH SEPARATOR
		// NARROW NO-BREAK SPACE
		// MEDIUM MATHEMATICAL SPACE
		// IDEOGRAPHIC SPACE
		private static Number ParseNumberPart(string input, string originalString, int curIndex)
		{
			Matcher matcher = digitsPatternExtended.Matcher(input);
			if (matcher.Matches())
			{
				string numPart = matcher.Group(1);
				string magnitudePart = matcher.Group(2);
				if (magnitudePart != null)
				{
					long magnitude = 1;
					switch (magnitudePart.ToLower())
					{
						case "dozen":
						{
							magnitude = 12L;
							break;
						}

						case "score":
						{
							magnitude = 20L;
							break;
						}

						case "hundred":
						{
							magnitude = 100L;
							break;
						}

						case "thousand":
						{
							magnitude = 1000L;
							break;
						}

						case "million":
						{
							magnitude = 1000000L;
							break;
						}

						case "billion":
						{
							magnitude = 1000000000L;
							break;
						}

						case "trillion":
						{
							magnitude = 1000000000000L;
							break;
						}

						default:
						{
							// unknown magnitude! Ignore it.
							break;
						}
					}
					if (digitsPattern.Matcher(numPart).Matches())
					{
						return long.Parse(numPart) * magnitude;
					}
					else
					{
						return double.Parse(numPart) * magnitude;
					}
				}
				else
				{
					if (digitsPattern.Matcher(numPart).Matches())
					{
						return long.Parse(numPart);
					}
					else
					{
						return double.Parse(numPart);
					}
				}
			}
			else
			{
				throw new NumberFormatException("Bad number put into wordToNumber.  Word is: \"" + input + "\", originally part of \"" + originalString + "\", piece # " + curIndex);
			}
		}

		/// <summary>
		/// Fairly generous utility function to convert a string representing
		/// a number (hopefully) to a Number.
		/// </summary>
		/// <remarks>
		/// Fairly generous utility function to convert a string representing
		/// a number (hopefully) to a Number.
		/// Assumes that something else has somehow determined that the string
		/// makes ONE suitable number.
		/// The value of the number is determined by:
		/// 0. Breaking up the string into pieces using whitespace
		/// (stuff like "and", "-", "," is turned into whitespace);
		/// 1. Determining the numeric value of the pieces;
		/// 2. Finding the numeric value of each piece;
		/// 3. Combining the pieces together to form the overall value:
		/// a. Find the largest component and its value (X),
		/// b. Let B = overall value of pieces to the left (recursive),
		/// c. Let C = overall value of pieces to the right recursive),
		/// d. The overall value = B*X + C.
		/// </remarks>
		/// <param name="str">The String to convert</param>
		/// <returns>numeric value of string</returns>
		public static Number WordToNumber(string str)
		{
			// Trims and lowercases stuff
			string originalString = str;
			str = str.Trim();
			if (str.IsEmpty())
			{
				return null;
			}
			str = str.ToLower();
			bool neg = str.StartsWith("-");
			// eliminate hyphens, commas, and the word "and"
			str = str.ReplaceAll("\\band\\b", " ");
			str = str.ReplaceAll("-", " ");
			str = str.ReplaceAll("(\\d),(\\d)", "$1$2");
			// Maybe something like 4,233,000 ??
			str = str.ReplaceAll(",", " ");
			// str = str.replaceAll("(\\d)(\\w)","$1 $2");
			// Trims again (do we need this?)
			str = str.Trim();
			// TODO: error checking....
			//if string starts with "a ", as in "a hundred", replace it with "one"
			if (str.StartsWith("a "))
			{
				str = str.Replace("a", "one");
			}
			// cut off some trailing s
			if (str.EndsWith("sands"))
			{
				// thousands
				str = Sharpen.Runtime.Substring(str, 0, str.Length - 1);
			}
			else
			{
				if (str.EndsWith("ions"))
				{
					// millions, billions, etc
					str = Sharpen.Runtime.Substring(str, 0, str.Length - 1);
				}
			}
			// now count words
			string[] fields = wsPattern.Split(str);
			Number[] numFields = new Number[fields.Length];
			int numWords = fields.Length;
			// get numeric value of each word piece
			for (int curIndex = 0; curIndex < numWords; curIndex++)
			{
				string curPart = fields[curIndex] == null ? string.Empty : fields[curIndex].ReplaceAll(whitespaceCharsRegex + "+", string.Empty).Trim();
				Matcher m = alphaPattern.Matcher(curPart);
				if (m.Find())
				{
					// Some part of the word has alpha characters
					Number curNum;
					if (word2NumMap.Contains(curPart))
					{
						curNum = word2NumMap[curPart];
					}
					else
					{
						if (ordWord2NumMap.Contains(curPart))
						{
							if (curIndex == numWords - 1)
							{
								curNum = ordWord2NumMap[curPart];
							}
							else
							{
								throw new NumberFormatException("Error in wordToNumber function.");
							}
						}
						else
						{
							if (curIndex > 0 && (curPart.EndsWith("ths") || curPart.EndsWith("rds")))
							{
								// Fractions?
								curNum = ordWord2NumMap[Sharpen.Runtime.Substring(curPart, 0, curPart.Length - 1)];
								if (curNum != null)
								{
									curNum = 1 / curNum;
								}
								else
								{
									throw new NumberFormatException("Bad number put into wordToNumber.  Word is: \"" + curPart + "\", originally part of \"" + originalString + "\", piece # " + curIndex);
								}
							}
							else
							{
								if (char.IsDigit(curPart[0]))
								{
									if (curPart.EndsWith("th") || curPart.EndsWith("rd") || curPart.EndsWith("nd") || curPart.EndsWith("st"))
									{
										curPart = Sharpen.Runtime.Substring(curPart, 0, curPart.Length - 2).Trim();
									}
									curNum = ParseNumberPart(curPart, originalString, curIndex);
								}
								else
								{
									throw new NumberFormatException("Bad number put into wordToNumber.  Word is: \"" + curPart + "\", originally part of \"" + originalString + "\", piece # " + curIndex);
								}
							}
						}
					}
					numFields[curIndex] = curNum;
				}
				else
				{
					// Word is all numeric
					Matcher matcher = digitsPatternExtended.Matcher(curPart);
					if (matcher.Matches())
					{
						numFields[curIndex] = ParseNumberPart(curPart, originalString, curIndex);
					}
					else
					{
						if (numPattern.Matcher(curPart).Matches())
						{
							numFields[curIndex] = new BigDecimal(curPart);
						}
						else
						{
							// Hmm, strange number
							throw new NumberFormatException("Bad number put into wordToNumber.  Word is: \"" + curPart + "\", originally part of \"" + originalString + "\", piece # " + curIndex);
						}
					}
				}
			}
			Number n = WordToNumberRecurse(numFields);
			return (neg) ? -n : n;
		}

		private static Number WordToNumberRecurse(Number[] numFields)
		{
			return WordToNumberRecurse(numFields, 0, numFields.Length);
		}

		private static Number WordToNumberRecurse(Number[] numFields, int start, int end)
		{
			// return solitary number
			if (end <= start)
			{
				return 0;
			}
			if (end - start == 1)
			{
				return numFields[start];
			}
			// first, find highest number in string
			Number highestNum = double.NegativeInfinity;
			int highestNumIndex = start;
			for (int i = start; i < end; i++)
			{
				Number curNum = numFields[i];
				if (curNum != null && curNum >= highestNum)
				{
					highestNum = curNum;
					highestNumIndex = i;
				}
			}
			Number beforeNum = 1;
			if (highestNumIndex > start)
			{
				beforeNum = WordToNumberRecurse(numFields, start, highestNumIndex);
				if (beforeNum == null)
				{
					beforeNum = 1;
				}
			}
			Number afterNum = WordToNumberRecurse(numFields, highestNumIndex + 1, end);
			if (afterNum == null)
			{
				afterNum = 0;
			}
			// TODO: Everything is treated as double... losing precision information here
			//       Sufficient for now
			//       Should we usually use BigDecimal to do our calculations?
			//       There are also fractions to consider.
			Number evaluatedNumber = ((beforeNum * highestNum) + afterNum);
			return evaluatedNumber;
		}

		public static Env GetNewEnv()
		{
			Env env = TokenSequencePattern.GetNewEnv();
			// Do case insensitive matching
			env.SetDefaultStringPatternFlags(Pattern.CaseInsensitive | Pattern.UnicodeCase);
			InitEnv(env);
			return env;
		}

		private static void InitEnv(Env env)
		{
			// Custom binding for numeric values expressions
			env.Bind("numtype", typeof(CoreAnnotations.NumericTypeAnnotation));
			env.Bind("numvalue", typeof(CoreAnnotations.NumericValueAnnotation));
			env.Bind("numcomptype", typeof(CoreAnnotations.NumericCompositeTypeAnnotation));
			env.Bind("numcompvalue", typeof(CoreAnnotations.NumericCompositeValueAnnotation));
			env.Bind("$NUMCOMPTERM", " [ { numcomptype::EXISTS } & !{ numcomptype:NUMBER_RANGE } ] ");
			env.Bind("$NUMTERM", " [ { numtype::EXISTS } & !{ numtype:NUMBER_RANGE } ] ");
			env.Bind("$NUMRANGE", " [ { numtype:NUMBER_RANGE } ] ");
			// TODO: Improve code to only recognize integers
			env.Bind("$INTTERM", " [ { numtype::EXISTS } & !{ numtype:NUMBER_RANGE } & !{ word:/.*\\.\\d+.*/} ] ");
			env.Bind("$POSINTTERM", " [ { numvalue>0 } & !{ word:/.*\\.\\d+.*/} ] ");
			env.Bind("$ORDTERM", " [ { numtype:ORDINAL } ] ");
			env.Bind("$BEFORE_WS", " [ { before:/\\s*/ } | !{ before::EXISTS} ]");
			env.Bind("$AFTER_WS", " [ { after:/\\s*/ } | !{ after::EXISTS} ]");
			env.Bind("$BEFORE_AFTER_WS", " [ $BEFORE_WS & $AFTER_WS ]");
		}

		private static readonly Env env = GetNewEnv();

		private static readonly TokenSequencePattern numberPattern = ((TokenSequencePattern)TokenSequencePattern.Compile(env, "$NUMTERM ( [/,/ & $BEFORE_WS]? [$POSINTTERM & $BEFORE_WS]  )* ( [/,/ & $BEFORE_WS]? [/and/ & $BEFORE_WS] [$POSINTTERM & $BEFORE_WS]+ )? "
			));

		/// <summary>
		/// Find and mark numbers (does not need NumberSequenceClassifier)
		/// Each token is annotated with the numeric value and type:
		/// - CoreAnnotations.NumericTypeAnnotation.class: ORDINAL, UNIT (hundred, thousand,..., dozen, gross,...), NUMBER
		/// - CoreAnnotations.NumericValueAnnotation.class: Number representing the numeric value of the token
		/// ( two thousand =&gt; 2 1000 ).
		/// </summary>
		/// <remarks>
		/// Find and mark numbers (does not need NumberSequenceClassifier)
		/// Each token is annotated with the numeric value and type:
		/// - CoreAnnotations.NumericTypeAnnotation.class: ORDINAL, UNIT (hundred, thousand,..., dozen, gross,...), NUMBER
		/// - CoreAnnotations.NumericValueAnnotation.class: Number representing the numeric value of the token
		/// ( two thousand =&gt; 2 1000 ).
		/// Tries also to separate individual numbers like four five six,
		/// while keeping numbers like four hundred and seven together
		/// Annotate tokens belonging to each composite number with
		/// - CoreAnnotations.NumericCompositeTypeAnnotation.class: ORDINAL (1st, 2nd), NUMBER (one hundred)
		/// - CoreAnnotations.NumericCompositeValueAnnotation.class: Number representing the composite numeric value
		/// ( two thousand =&gt; 2000 2000 ).
		/// Also returns list of CoreMap representing the identified numbers.
		/// The function is overly aggressive in marking possible numbers
		/// - should either do more checks or use in conjunction with NumberSequenceClassifier
		/// to avoid marking certain tokens (like second/NN) as numbers...
		/// </remarks>
		/// <param name="annotation">The annotation structure</param>
		/// <returns>list of CoreMap representing the identified numbers</returns>
		public static IList<ICoreMap> FindNumbers(ICoreMap annotation)
		{
			IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			CoreLabel lastToken = null;
			foreach (CoreLabel token in tokens)
			{
				string w = token.Word();
				w = w.Trim().ToLower();
				if (Edu.Stanford.Nlp.IE.NumberNormalizer.numPattern.Matcher(w).Matches() || Edu.Stanford.Nlp.IE.NumberNormalizer.numberTermPattern2.Matcher(w).Matches() || NumberSequenceClassifier.OrdinalPattern.Matcher(w).Matches() || Edu.Stanford.Nlp.IE.NumberNormalizer
					.numEndUnitPattern.Matcher(w).Matches())
				{
					// TODO: first ADVERB and second NN shouldn't be marked as ordinals
					// But maybe we don't care, this can just mark the potential numbers, something else can disregard those
					// Don't count as number if an adverb (e.g., First/RB) or verb (e.g. second/VB)
					// Too unreliable
					// String pos = token.get(CoreAnnotations.PartOfSpeechAnnotation.class);
					// if ("RB".equals(pos) || "VBP".equals(pos)) {
					//   continue;
					// }
					// Don't count an end unit if not previous number
					if (Edu.Stanford.Nlp.IE.NumberNormalizer.numNotStandaloneUnitPattern.Matcher(w).Matches())
					{
						if (lastToken == null || !lastToken.ContainsKey(typeof(CoreAnnotations.NumericValueAnnotation)))
						{
							continue;
						}
					}
					try
					{
						token.Set(typeof(CoreAnnotations.NumericValueAnnotation), Edu.Stanford.Nlp.IE.NumberNormalizer.WordToNumber(w));
						if (NumberSequenceClassifier.OrdinalPattern.Matcher(w).Find())
						{
							token.Set(typeof(CoreAnnotations.NumericTypeAnnotation), "ORDINAL");
						}
						else
						{
							if (Edu.Stanford.Nlp.IE.NumberNormalizer.numUnitPattern.Matcher(w).Matches())
							{
								token.Set(typeof(CoreAnnotations.NumericTypeAnnotation), "UNIT");
							}
							else
							{
								if (Edu.Stanford.Nlp.IE.NumberNormalizer.numEndUnitPattern.Matcher(w).Matches())
								{
									token.Set(typeof(CoreAnnotations.NumericTypeAnnotation), "UNIT");
								}
								else
								{
									token.Set(typeof(CoreAnnotations.NumericTypeAnnotation), "NUMBER");
								}
							}
						}
					}
					catch (Exception ex)
					{
						logger.Warning("Error interpreting number " + w + ": " + ex.Message);
					}
				}
				lastToken = token;
			}
			// TODO: Should we allow "," in written out numbers?
			// TODO: Handle "-" that is not with token?
			TokenSequenceMatcher matcher = ((TokenSequenceMatcher)numberPattern.GetMatcher(tokens));
			IList<ICoreMap> numbers = new List<ICoreMap>();
			while (matcher.Find())
			{
				IList<ICoreMap> matchedTokens = matcher.GroupNodes();
				int numStart = matcher.Start();
				int possibleNumEnd = -1;
				int lastUnitPos = -1;
				int possibleNumStart = -1;
				Number possibleNumEndUnit = null;
				Number lastUnit = null;
				// Check if we need to split matched chunk up more
				for (int i = matcher.Start(); i < matcher.End(); i++)
				{
					CoreLabel token_1 = tokens[i];
					CoreLabel prev = (i > matcher.Start()) ? tokens[i - 1] : null;
					Number num = token_1.Get(typeof(CoreAnnotations.NumericValueAnnotation));
					Number prevNum = (prev != null) ? prev.Get(typeof(CoreAnnotations.NumericValueAnnotation)) : null;
					string w = token_1.Word();
					w = w.Trim().ToLower();
					switch (w)
					{
						case ",":
						{
							if (lastUnit != null && lastUnitPos == i - 1)
							{
								// OKAY, this may be one big number
								possibleNumEnd = i;
								possibleNumEndUnit = lastUnit;
							}
							else
							{
								// Not one big number
								if (numStart < i)
								{
									numbers.Add(ChunkAnnotationUtils.GetAnnotatedChunk(annotation, numStart, i));
									numStart = i + 1;
									possibleNumEnd = -1;
									possibleNumEndUnit = null;
									lastUnit = null;
									lastUnitPos = -1;
								}
							}
							if (numStart == i)
							{
								numStart = i + 1;
							}
							break;
						}

						case "and":
						{
							// Check if number before and was unit
							string prevWord = prev.Word();
							if (lastUnitPos == i - 1 || (lastUnitPos == i - 2 && ",".Equals(prevWord)))
							{
							}
							else
							{
								// Okay
								// Two separate numbers
								if (numStart < possibleNumEnd)
								{
									numbers.Add(ChunkAnnotationUtils.GetAnnotatedChunk(annotation, numStart, possibleNumEnd));
									if (possibleNumStart >= possibleNumEnd)
									{
										numStart = possibleNumStart;
									}
									else
									{
										numStart = i + 1;
									}
								}
								else
								{
									if (numStart < i)
									{
										numbers.Add(ChunkAnnotationUtils.GetAnnotatedChunk(annotation, numStart, i));
										numStart = i + 1;
									}
								}
								if (lastUnitPos < numStart)
								{
									lastUnit = null;
									lastUnitPos = -1;
								}
								possibleNumEnd = -1;
								possibleNumEndUnit = null;
							}
							break;
						}

						default:
						{
							// NUMBER or ORDINAL
							string numType = token_1.Get(typeof(CoreAnnotations.NumericTypeAnnotation));
							if ("UNIT".Equals(numType))
							{
								// Compare this unit with previous
								if (lastUnit == null || lastUnit > num)
								{
								}
								else
								{
									// lastUnit larger than this unit
									// maybe four thousand two hundred?
									// OKAY, probably one big number
									if (numStart < possibleNumEnd)
									{
										// Units are increasing - check if this unit is >= unit before "," (if so, need to split into chunks)
										// Not one big number  ( had a comma )
										if (num >= possibleNumEndUnit)
										{
											numbers.Add(ChunkAnnotationUtils.GetAnnotatedChunk(annotation, numStart, possibleNumEnd));
											if (possibleNumStart >= possibleNumEnd)
											{
												numStart = possibleNumStart;
											}
											else
											{
												numStart = i;
											}
											possibleNumEnd = -1;
											possibleNumEndUnit = null;
										}
									}
								}
								// unit is increasing - can be okay, maybe five hundred thousand?
								// what about four hundred five thousand
								// unit might also be the same, as in thousand thousand,
								// which we convert to million
								lastUnit = num;
								lastUnitPos = i;
							}
							else
							{
								// Normal number
								if (num == null)
								{
									logger.Warning("NO NUMBER: " + token_1.Word());
									continue;
								}
								if (prevNum != null)
								{
									if (num > 0)
									{
										if (num < 10)
										{
											// This number is a digit
											// Treat following as two separate numbers
											//    \d+ [0-9]
											//    [one to nine]  [0-9]
											if (Edu.Stanford.Nlp.IE.NumberNormalizer.numPattern.Matcher(prev.Word()).Matches() || prevNum < 10 || prevNum % 10 != 0)
											{
												// two separate numbers
												if (numStart < i)
												{
													numbers.Add(ChunkAnnotationUtils.GetAnnotatedChunk(annotation, numStart, i));
												}
												numStart = i;
												possibleNumEnd = -1;
												possibleNumEndUnit = null;
												lastUnit = null;
												lastUnitPos = -1;
											}
										}
										else
										{
											string prevNumType = prev.Get(typeof(CoreAnnotations.NumericTypeAnnotation));
											if ("UNIT".Equals(prevNumType))
											{
											}
											else
											{
												// OKAY
												if (!ordinalUnitPattern.Matcher(w).Matches())
												{
													// Start of new number
													if (numStart < i)
													{
														numbers.Add(ChunkAnnotationUtils.GetAnnotatedChunk(annotation, numStart, i));
													}
													numStart = i;
													possibleNumEnd = -1;
													possibleNumEndUnit = null;
													lastUnit = null;
													lastUnitPos = -1;
												}
											}
										}
									}
								}
								if ("ORDINAL".Equals(numType))
								{
									if (possibleNumEnd >= 0)
									{
										if (numStart < possibleNumEnd)
										{
											numbers.Add(ChunkAnnotationUtils.GetAnnotatedChunk(annotation, numStart, possibleNumEnd));
										}
										if (possibleNumStart > possibleNumEnd)
										{
											numbers.Add(ChunkAnnotationUtils.GetAnnotatedChunk(annotation, possibleNumStart, i + 1));
										}
										else
										{
											numbers.Add(ChunkAnnotationUtils.GetAnnotatedChunk(annotation, possibleNumEnd + 1, i + 1));
										}
									}
									else
									{
										if (numStart < i + 1)
										{
											numbers.Add(ChunkAnnotationUtils.GetAnnotatedChunk(annotation, numStart, i + 1));
										}
									}
									numStart = i + 1;
									possibleNumEnd = -1;
									possibleNumEndUnit = null;
									lastUnit = null;
									lastUnitPos = -1;
								}
								if (possibleNumStart < possibleNumEnd)
								{
									possibleNumStart = i;
								}
							}
							break;
						}
					}
				}
				if (numStart < matcher.End())
				{
					numbers.Add(ChunkAnnotationUtils.GetAnnotatedChunk(annotation, numStart, matcher.End()));
				}
			}
			foreach (ICoreMap n in numbers)
			{
				string exp = n.Get(typeof(CoreAnnotations.TextAnnotation));
				if (exp.Trim().Equals(string.Empty))
				{
					continue;
				}
				IList<CoreLabel> ts = n.Get(typeof(CoreAnnotations.TokensAnnotation));
				string label = ts[ts.Count - 1].Get(typeof(CoreAnnotations.NumericTypeAnnotation));
				if ("UNIT".Equals(label))
				{
					label = "NUMBER";
				}
				try
				{
					Number num = Edu.Stanford.Nlp.IE.NumberNormalizer.WordToNumber(exp);
					if (num == null)
					{
						logger.Warning("NO NUMBER FOR: \"" + exp + "\"");
					}
					n.Set(typeof(CoreAnnotations.NumericCompositeValueAnnotation), num);
					n.Set(typeof(CoreAnnotations.NumericCompositeTypeAnnotation), label);
					foreach (CoreLabel t in ts)
					{
						t.Set(typeof(CoreAnnotations.NumericCompositeValueAnnotation), num);
						t.Set(typeof(CoreAnnotations.NumericCompositeTypeAnnotation), label);
					}
				}
				catch (NumberFormatException ex)
				{
					logger.Warning("Invalid number for: \"" + exp + "\"", ex);
				}
			}
			return numbers;
		}

		private static readonly TokenSequencePattern rangePattern = ((TokenSequencePattern)TokenSequencePattern.Compile(env, "(?:$NUMCOMPTERM /-|to/ $NUMCOMPTERM) | $NUMRANGE"));

		/// <summary>Find and mark number ranges.</summary>
		/// <remarks>
		/// Find and mark number ranges.
		/// Ranges are NUM1 [-|to] NUM2 where NUM2 &gt; NUM1.
		/// Each number range is marked with
		/// - CoreAnnotations.NumericTypeAnnotation.class: NUMBER_RANGE
		/// - CoreAnnotations.NumericObjectAnnotation.class:
		/// <c>Pair&lt;Number&gt;</c>
		/// representing the start/end of the range.
		/// </remarks>
		/// <param name="annotation">- annotation where numbers have already been identified</param>
		/// <returns>list of CoreMap representing the identified number ranges</returns>
		private static IList<ICoreMap> FindNumberRanges(ICoreMap annotation)
		{
			IList<ICoreMap> numerizedTokens = annotation.Get(typeof(CoreAnnotations.NumerizedTokensAnnotation));
			foreach (ICoreMap token in numerizedTokens)
			{
				string w = token.Get(typeof(CoreAnnotations.TextAnnotation));
				w = w.Trim().ToLower();
				Matcher rangeMatcher = Edu.Stanford.Nlp.IE.NumberNormalizer.numRangePattern.Matcher(w);
				if (rangeMatcher.Matches())
				{
					try
					{
						string w1 = rangeMatcher.Group(1);
						string w2 = rangeMatcher.Group(2);
						Number v1 = Edu.Stanford.Nlp.IE.NumberNormalizer.WordToNumber(w1);
						Number v2 = Edu.Stanford.Nlp.IE.NumberNormalizer.WordToNumber(w2);
						if (v1 != null && v2 != null && v2 > v1)
						{
							token.Set(typeof(CoreAnnotations.NumericTypeAnnotation), "NUMBER_RANGE");
							token.Set(typeof(CoreAnnotations.NumericCompositeTypeAnnotation), "NUMBER_RANGE");
							Pair<Number, Number> range = new Pair<Number, Number>(v1, v2);
							token.Set(typeof(CoreAnnotations.NumericCompositeObjectAnnotation), range);
						}
					}
					catch (Exception ex)
					{
						logger.Warning("Error interpreting number range " + w + ": " + ex.Message);
					}
				}
			}
			IList<ICoreMap> numberRanges = new List<ICoreMap>();
			TokenSequenceMatcher matcher = ((TokenSequenceMatcher)rangePattern.GetMatcher(numerizedTokens));
			while (matcher.Find())
			{
				IList<ICoreMap> matched = matcher.GroupNodes();
				if (matched.Count == 1)
				{
					numberRanges.Add(matched[0]);
				}
				else
				{
					Number v1 = matched[0].Get(typeof(CoreAnnotations.NumericCompositeValueAnnotation));
					Number v2 = matched[matched.Count - 1].Get(typeof(CoreAnnotations.NumericCompositeValueAnnotation));
					if (v2 > v1)
					{
						ICoreMap newChunk = CoreMapAggregator.GetDefaultAggregator().Merge(numerizedTokens, matcher.Start(), matcher.End());
						newChunk.Set(typeof(CoreAnnotations.NumericCompositeTypeAnnotation), "NUMBER_RANGE");
						Pair<Number, Number> range = new Pair<Number, Number>(v1, v2);
						newChunk.Set(typeof(CoreAnnotations.NumericCompositeObjectAnnotation), range);
						numberRanges.Add(newChunk);
					}
				}
			}
			return numberRanges;
		}

		/// <summary>Takes annotation and identifies numbers in the annotation.</summary>
		/// <remarks>
		/// Takes annotation and identifies numbers in the annotation.
		/// Returns a list of tokens (as CoreMaps) with numbers merged.
		/// As by product, also marks each individual token with the TokenBeginAnnotation and TokenEndAnnotation
		/// - this is mainly to make it easier to the rest of the code to figure out what the token offsets are.
		/// Note that this copies the annotation, since it modifies token offsets in the original.
		/// </remarks>
		/// <param name="annotationRaw">The annotation to find numbers in</param>
		/// <returns>list of CoreMap representing the identified numbers</returns>
		public static IList<ICoreMap> FindAndMergeNumbers(ICoreMap annotationRaw)
		{
			//copy annotation to preserve its integrity
			ICoreMap annotation = new ArrayCoreMap(annotationRaw);
			// Find and label numbers
			IList<ICoreMap> numbers = Edu.Stanford.Nlp.IE.NumberNormalizer.FindNumbers(annotation);
			CoreMapAggregator numberAggregator = CoreMapAggregator.GetAggregator(CoreMapAttributeAggregator.DefaultNumericAggregators, typeof(CoreAnnotations.TokensAnnotation));
			// We are going to mark the token begin and token end for each token so we can more easily deal with
			// ensuring correct token offsets for merging
			//get sentence offset
			int startTokenOffset = annotation.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
			if (startTokenOffset == null)
			{
				startTokenOffset = 0;
			}
			//set token offsets
			int i = 0;
			IList<int> savedTokenBegins = new LinkedList<int>();
			IList<int> savedTokenEnds = new LinkedList<int>();
			foreach (ICoreMap c in annotation.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				//set token begin
				if ((i == 0 && c.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) != null) || (i > 0 && !savedTokenBegins.IsEmpty()))
				{
					savedTokenBegins.Add(c.Get(typeof(CoreAnnotations.TokenBeginAnnotation)));
				}
				c.Set(typeof(CoreAnnotations.TokenBeginAnnotation), i + startTokenOffset);
				i++;
				//set token end
				if ((i == 1 && c.Get(typeof(CoreAnnotations.TokenEndAnnotation)) != null) || (i > 1 && !savedTokenEnds.IsEmpty()))
				{
					savedTokenEnds.Add(c.Get(typeof(CoreAnnotations.TokenEndAnnotation)));
				}
				c.Set(typeof(CoreAnnotations.TokenEndAnnotation), i + startTokenOffset);
			}
			//merge numbers
			int startTokenOffsetFinal = startTokenOffset;
			IList<ICoreMap> mergedNumbers = numberAggregator.Merge(annotation.Get(typeof(CoreAnnotations.TokensAnnotation)), numbers, null);
			//restore token offsets
			if (!savedTokenBegins.IsEmpty() && !savedTokenEnds.IsEmpty())
			{
				foreach (ICoreMap c_1 in mergedNumbers)
				{
					// get new indices
					int newBegin = c_1.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) - startTokenOffset;
					int newEnd = c_1.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - startTokenOffset;
					// get token offsets for those indices
					c_1.Set(typeof(CoreAnnotations.TokenBeginAnnotation), savedTokenBegins[newBegin]);
					c_1.Set(typeof(CoreAnnotations.TokenEndAnnotation), savedTokenEnds[newEnd - 1]);
				}
			}
			//return
			return mergedNumbers;
		}

		public static IList<ICoreMap> FindAndAnnotateNumericExpressions(ICoreMap annotation)
		{
			IList<ICoreMap> mergedNumbers = Edu.Stanford.Nlp.IE.NumberNormalizer.FindAndMergeNumbers(annotation);
			annotation.Set(typeof(CoreAnnotations.NumerizedTokensAnnotation), mergedNumbers);
			return mergedNumbers;
		}

		public static IList<ICoreMap> FindAndAnnotateNumericExpressionsWithRanges(ICoreMap annotation)
		{
			int startTokenOffset = annotation.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
			if (startTokenOffset == null)
			{
				startTokenOffset = 0;
			}
			IList<ICoreMap> mergedNumbers = Edu.Stanford.Nlp.IE.NumberNormalizer.FindAndMergeNumbers(annotation);
			annotation.Set(typeof(CoreAnnotations.NumerizedTokensAnnotation), mergedNumbers);
			// Find and label number ranges
			IList<ICoreMap> numberRanges = Edu.Stanford.Nlp.IE.NumberNormalizer.FindNumberRanges(annotation);
			int startTokenOffsetFinal = startTokenOffset;
			IList<ICoreMap> mergedNumbersWithRanges = CollectionUtils.MergeListWithSortedMatchedPreAggregated(annotation.Get(typeof(CoreAnnotations.NumerizedTokensAnnotation)), numberRanges, null);
			annotation.Set(typeof(CoreAnnotations.NumerizedTokensAnnotation), mergedNumbersWithRanges);
			return mergedNumbersWithRanges;
		}
	}
}
