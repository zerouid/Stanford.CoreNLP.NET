using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Regexp;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.IE
{
	/// <summary>
	/// A Chinese correspondence of the
	/// <see cref="QuantifiableEntityNormalizer"/>
	/// that normalizes NUMBER, DATE, TIME,
	/// MONEY, PERCENT and ORDINAL amounts expressed in Chinese.
	/// Note that this class is originally designed for the Chinese KBP Challenge, so it only
	/// supports minimal functionalities. This needs to be completed in the future.
	/// </summary>
	/// <author>Yuhao Zhang</author>
	/// <author>Peng Qi</author>
	public class ChineseQuantifiableEntityNormalizer
	{
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.ChineseQuantifiableEntityNormalizer));

		private const bool Debug = false;

		public static string BackgroundSymbol = SeqClassifierFlags.DefaultBackgroundSymbol;

		private static readonly ICollection<string> quantifiable;

		private static readonly ClassicCounter<string> wordsToValues;

		private static readonly ClassicCounter<string> quantityUnitToValues;

		private static readonly IDictionary<string, char> multiCharCurrencyWords;

		private static readonly IDictionary<string, char> oneCharCurrencyWords;

		private static readonly IDictionary<string, string> fullDigitToHalfDigit;

		private static readonly IDictionary<string, int> yearModifiers;

		private static readonly IDictionary<string, int> monthDayModifiers;

		private const string LiteralDecimalPoint = "点";

		private static readonly Pattern ArabicNumbersPattern = Pattern.Compile("[-+]?\\d*\\.?\\d+");

		private static readonly Pattern ChineseLiteralNumberSequencePattern = Pattern.Compile("[一二三四五六七八九零〇]+");

		private static readonly Pattern ChineseLiteralDecimalPattern = ChineseLiteralNumberSequencePattern;

		private const string greaterEqualThreeWords = "(?:大|多|高)于或者?等于";

		private const string lessEqualThreeWords = "(?:小|少|低)于或者?等于";

		private const string greaterEqualTwoWords = "(?:大|多)于等于|不(?:少|小|低)于";

		private const string lessEqualTwoWords = "(?:小|少)于等于|不(?:大|少|高)于|不超过";

		private const string approxTwoWords = "大(?:概|约|致)(?:是|为)|大概其";

		private const string greaterThanOneWord = "(?:大|多|高)于|(?:超|高|多)过";

		private const string lessThanOneWord = "(?:小|少|低)于|不(?:到|够|足)";

		private const string approxOneWord = "大(?:约|概|致)|接?近|差不多|几乎|左右|上下|约(?:为|略)";

		private const string NumberTag = "NUMBER";

		private const string DateTag = "DATE";

		private const string TimeTag = "TIME";

		private const string MoneyTag = "MONEY";

		private const string OrdinalTag = "ORDINAL";

		private const string PercentTag = "PERCENT";

		static ChineseQuantifiableEntityNormalizer()
		{
			//Entity types that are quantifiable
			// used by money
			// used by money
			// Patterns we need
			// TODO (yuhao): here we are not considering 1) negative numbers, 2) Chinese traditional characters
			// This is the all-literal-number-characters sequence, excluding unit characters like 十 or 万
			// The decimal part of a float number should be exactly literal number sequence without units
			// Used by quantity modifiers
			// All the tags we need
			// static initialization of useful properties
			quantifiable = Generics.NewHashSet();
			quantifiable.Add(NumberTag);
			quantifiable.Add(DateTag);
			quantifiable.Add(TimeTag);
			quantifiable.Add(MoneyTag);
			quantifiable.Add(PercentTag);
			quantifiable.Add(OrdinalTag);
			quantityUnitToValues = new ClassicCounter<string>();
			quantityUnitToValues.SetCount("十", 10.0);
			quantityUnitToValues.SetCount("百", 100.0);
			quantityUnitToValues.SetCount("千", 1000.0);
			quantityUnitToValues.SetCount("万", 10000.0);
			quantityUnitToValues.SetCount("亿", 100000000.0);
			wordsToValues = new ClassicCounter<string>();
			wordsToValues.SetCount("零", 0.0);
			wordsToValues.SetCount("〇", 0.0);
			wordsToValues.SetCount("一", 1.0);
			wordsToValues.SetCount("二", 2.0);
			wordsToValues.SetCount("两", 2.0);
			wordsToValues.SetCount("三", 3.0);
			wordsToValues.SetCount("四", 4.0);
			wordsToValues.SetCount("五", 5.0);
			wordsToValues.SetCount("六", 6.0);
			wordsToValues.SetCount("七", 7.0);
			wordsToValues.SetCount("八", 8.0);
			wordsToValues.SetCount("九", 9.0);
			wordsToValues.AddAll(quantityUnitToValues);
			// all units are also quantifiable individually
			multiCharCurrencyWords = Generics.NewHashMap();
			multiCharCurrencyWords["美元"] = '$';
			multiCharCurrencyWords["美分"] = '$';
			multiCharCurrencyWords["英镑"] = '£';
			multiCharCurrencyWords["先令"] = '£';
			multiCharCurrencyWords["便士"] = '£';
			multiCharCurrencyWords["欧元"] = '€';
			multiCharCurrencyWords["日元"] = '¥';
			multiCharCurrencyWords["韩元"] = '₩';
			oneCharCurrencyWords = Generics.NewHashMap();
			oneCharCurrencyWords["刀"] = '$';
			oneCharCurrencyWords["镑"] = '£';
			oneCharCurrencyWords["元"] = '元';
			// We follow the tradition in English to use 元 instead of ¥ for RMB
			// For all other currency, we use default currency symbol $
			yearModifiers = Generics.NewHashMap();
			yearModifiers["前"] = -2;
			yearModifiers["去"] = -1;
			yearModifiers["上"] = -1;
			yearModifiers["今"] = 0;
			yearModifiers["同"] = 0;
			yearModifiers["此"] = 0;
			yearModifiers["该"] = 0;
			yearModifiers["本"] = 0;
			yearModifiers["明"] = 1;
			yearModifiers["来"] = 1;
			yearModifiers["下"] = 1;
			yearModifiers["后"] = 2;
			monthDayModifiers = Generics.NewHashMap();
			monthDayModifiers["昨"] = -1;
			monthDayModifiers["上"] = -1;
			monthDayModifiers["今"] = 0;
			monthDayModifiers["同"] = 0;
			monthDayModifiers["此"] = 0;
			monthDayModifiers["该"] = 0;
			monthDayModifiers["本"] = 0;
			monthDayModifiers["来"] = 1;
			monthDayModifiers["明"] = 1;
			monthDayModifiers["下"] = 1;
			fullDigitToHalfDigit = Generics.NewHashMap();
			fullDigitToHalfDigit["１"] = "1";
			fullDigitToHalfDigit["２"] = "2";
			fullDigitToHalfDigit["３"] = "3";
			fullDigitToHalfDigit["４"] = "4";
			fullDigitToHalfDigit["５"] = "5";
			fullDigitToHalfDigit["６"] = "6";
			fullDigitToHalfDigit["７"] = "7";
			fullDigitToHalfDigit["８"] = "8";
			fullDigitToHalfDigit["９"] = "9";
			fullDigitToHalfDigit["０"] = "0";
		}

		private const string ChineseDateNumeralsPattern = "[一二三四五六七八九零十〇]";

		private const string ChineseAndArabicNumeralsPattern = "[一二三四五六七八九零十〇\\d]";

		private const string ChineseAndArabicNumeralsPatternWoTen = "[一二三四五六七八九零〇\\d]";

		private static readonly string YearModifierPattern = "[" + string.Join(string.Empty, yearModifiers.Keys) + "]";

		private static readonly string MonthDayModifierPattern = "[" + string.Join(string.Empty, monthDayModifiers.Keys) + "]";

		private static readonly string BasicDdPattern = "(" + ChineseAndArabicNumeralsPattern + "{1,3}|" + MonthDayModifierPattern + ")[日号&&[^年月]]?";

		private static readonly string BasicMmddPattern = "(" + ChineseAndArabicNumeralsPattern + "{1,2}|" + MonthDayModifierPattern + ")(?:月份?|\\-|/|\\.)(?:" + BasicDdPattern + ")?";

		private static readonly string BasicYyyymmddPattern = "(" + ChineseAndArabicNumeralsPatternWoTen + "{2,4}|" + YearModifierPattern + ")(?:年[份度]?|\\-|/|\\.)?" + "(?:" + BasicMmddPattern + ")?";

		private const string EnglishMmddyyyyPattern = "(\\d{1,2})[/\\-\\.](\\d{1,2})(?:[/\\-\\.](\\d{4}))?";

		private const string RelativeTimePattern = "([昨今明])[天晨晚夜早]";

		private const string BirthDecadePattern = "(" + ChineseAndArabicNumeralsPattern + "[0零〇5五])后";

		private ChineseQuantifiableEntityNormalizer()
		{
		}

		// Patterns used by DATE and TIME (must be after the static initializers to make use of the modifiers)
		// static methods
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
		/// the full quantity.
		/// Unlike the English normalizer, this method currently does not support
		/// concatenation or SUTime.
		/// </remarks>
		/// <param name="list">
		/// A list of
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// s representing a single document.
		/// Note: We assume the NERs has been labelled and the labels
		/// will be updated in place.
		/// </param>
		/// <param name="document"/>
		/// <param name="sentence"/>
		/// <?/>
		public static void AddNormalizedQuantitiesToEntities<E>(IList<E> list, ICoreMap document, ICoreMap sentence)
			where E : ICoreMap
		{
			// Fix the NER sequence if necessay
			FixupNerBeforeNormalization(list);
			// Now that NER tags has been fixed up, we do another pass to add the normalization
			string prevNerTag = BackgroundSymbol;
			int beforeIndex = -1;
			List<E> collector = new List<E>();
			for (int i = 0; i <= sz; i++)
			{
				// we should always keep list.size() unchanged inside the loop
				E wi = null;
				string currNerTag = null;
				string nextWord = string.Empty;
				if (i < sz)
				{
					wi = list[i];
					if (i + 1 < sz)
					{
						nextWord = list[i + 1].Get(typeof(CoreAnnotations.TextAnnotation));
						if (nextWord == null)
						{
							nextWord = string.Empty;
						}
					}
					// We assume NERs have been set by previous NER taggers
					currNerTag = wi.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
				}
				// TODO: may need to detect TIME modifier here?
				E wprev = (i > 0) ? list[i - 1] : null;
				// if the current wi is a non-continuation and the last one was a
				// quantity, we close and process the last segment.
				// TODO: also need to check compatibility as the English normalizer does
				if ((currNerTag == null || !currNerTag.Equals(prevNerTag)) && quantifiable.Contains(prevNerTag))
				{
					string modifier = null;
					switch (prevNerTag)
					{
						case TimeTag:
						{
							// Need different handling for different tags
							// TODO: add TIME
							break;
						}

						case DateTag:
						{
							ProcessEntity(collector, prevNerTag, modifier, nextWord, document);
							break;
						}

						default:
						{
							if (prevNerTag.Equals(NumberTag) || prevNerTag.Equals(PercentTag) || prevNerTag.Equals(MoneyTag))
							{
								// we are doing for prev tag so afterIndex should really be i
								modifier = DetectQuantityModifier(list, beforeIndex, i);
							}
							ProcessEntity(collector, prevNerTag, modifier, nextWord);
							break;
						}
					}
					collector = new List<E>();
				}
				// If currNerTag is quantifiable, we add it into collector
				if (quantifiable.Contains(currNerTag))
				{
					if (collector.IsEmpty())
					{
						beforeIndex = i - 1;
					}
					collector.Add(wi);
				}
				// move on and update prev pointer
				prevNerTag = currNerTag;
			}
		}

		/// <summary>Detect the quantity modifiers ahead of a numeric string.</summary>
		/// <remarks>
		/// Detect the quantity modifiers ahead of a numeric string. This method will look at three words ahead
		/// and one word afterwards at most. Examples of modifiers are "大约", "多于".
		/// </remarks>
		/// <param name="list"/>
		/// <param name="beforeIndex"/>
		/// <param name="afterIndex"/>
		/// <?/>
		/// <returns/>
		private static string DetectQuantityModifier<E>(IList<E> list, int beforeIndex, int afterIndex)
			where E : ICoreMap
		{
			string prev = (beforeIndex >= 0) ? list[beforeIndex].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string prev2 = (beforeIndex - 1 >= 0) ? list[beforeIndex - 1].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			string prev3 = (beforeIndex - 2 >= 0) ? list[beforeIndex - 2].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			int sz = list.Count;
			string next = (afterIndex < sz) ? list[afterIndex].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower() : string.Empty;
			// output space for clarity
			// Actually spaces won't be used for Chinese
			string longPrev = prev3 + prev2 + prev;
			if (longPrev.Matches(lessEqualThreeWords))
			{
				return "<=";
			}
			if (longPrev.Matches(greaterEqualThreeWords))
			{
				return ">=";
			}
			longPrev = prev2 + prev;
			if (longPrev.Matches(greaterEqualTwoWords))
			{
				return ">=";
			}
			if (longPrev.Matches(lessEqualTwoWords))
			{
				return "<=";
			}
			if (longPrev.Matches(approxTwoWords))
			{
				return "~";
			}
			if (prev.Matches(greaterThanOneWord))
			{
				return ">";
			}
			if (prev.Matches(lessThanOneWord))
			{
				return "<";
			}
			if (prev.Matches(approxOneWord))
			{
				return "~";
			}
			if (next.Matches(approxOneWord))
			{
				return "~";
			}
			// As backup, we also check whether prev matches a two-word pattern, just in case the segmenter fails
			// This happens to <= or >= patterns sometime as observed.
			if (prev.Matches(greaterEqualTwoWords))
			{
				return ">=";
			}
			if (prev.Matches(lessEqualTwoWords))
			{
				return "<=";
			}
			// otherwise, not modifier detected and return null
			return null;
		}

		private static IList<E> ProcessEntity<E>(IList<E> l, string entityType, string compModifier, string nextWord)
			where E : ICoreMap
		{
			return ProcessEntity(l, entityType, compModifier, nextWord, null);
		}

		/// <summary>Process an entity given the NER tag, extracted modifier and the next word in the document.</summary>
		/// <remarks>
		/// Process an entity given the NER tag, extracted modifier and the next word in the document.
		/// The normalized quantity will be written in place.
		/// </remarks>
		/// <param name="l">A collector that collects annotations for the entity.</param>
		/// <param name="entityType">Quantifiable NER tag.</param>
		/// <param name="compModifier">
		/// The extracted modifier around the entity of interest. Different NER tags should
		/// have different extraction rules.
		/// </param>
		/// <param name="nextWord">Next word in the document.</param>
		/// <param name="document">Reference to the document.</param>
		/// <?/>
		/// <returns/>
		private static IList<E> ProcessEntity<E>(IList<E> l, string entityType, string compModifier, string nextWord, ICoreMap document)
			where E : ICoreMap
		{
			// convert the entity annotations into a string
			string s = SingleEntityToString(l);
			StringBuilder sb = new StringBuilder();
			// convert all full digits to half digits
			for (int i = 0; i < sz; i++)
			{
				string ch = Sharpen.Runtime.Substring(s, i, i + 1);
				if (fullDigitToHalfDigit.Contains(ch))
				{
					ch = fullDigitToHalfDigit[ch];
				}
				sb.Append(ch);
			}
			s = sb.ToString();
			string p = null;
			switch (entityType)
			{
				case NumberTag:
				{
					p = string.Empty;
					if (compModifier != null)
					{
						p = compModifier;
					}
					string q = NormalizedNumberString(s, nextWord, 1.0);
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

				case OrdinalTag:
				{
					// ordinal won't have modifier
					p = NormalizedOrdinalString(s, nextWord);
					break;
				}

				case PercentTag:
				{
					p = NormalizedPercentString(s, nextWord);
					break;
				}

				case MoneyTag:
				{
					p = string.Empty;
					if (compModifier != null)
					{
						p = compModifier;
					}
					q = NormalizedMoneyString(s, nextWord);
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

				case DateTag:
				{
					if (s.Matches(BasicYyyymmddPattern) || s.Matches(BasicMmddPattern) || s.Matches(EnglishMmddyyyyPattern) || s.Matches(BasicDdPattern) || s.Matches(RelativeTimePattern) || s.Matches(BirthDecadePattern))
					{
						string docdate = document.Get(typeof(CoreAnnotations.DocDateAnnotation));
						p = NormalizeDateString(s, docdate);
					}
					break;
				}

				case TimeTag:
				{
					break;
				}
			}
			// Write the normalized NER values in place
			foreach (E wi in l)
			{
				if (p != null)
				{
					wi.Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), p);
				}
			}
			// This return value is not necessarily useful as the labelling is done in place.
			return l;
		}

		/// <summary>Normalize a money string.</summary>
		/// <remarks>
		/// Normalize a money string. A currency symbol will be added accordingly.
		/// The assumption is that the money string will be clean enough: either lead by a currency sign (like $),
		/// or trailed by a currency word. Otherwise we give up normalization.
		/// </remarks>
		/// <param name="s"/>
		/// <param name="nextWord"/>
		/// <returns/>
		private static string NormalizedMoneyString(string s, string nextWord)
		{
			// default multiplier is 1
			double multiplier = 1.0;
			char currencySign = '$';
			// by default we use $, following English
			bool notMatched = true;
			// We check multiCharCurrencyWords first
			foreach (string currencyWord in multiCharCurrencyWords.Keys)
			{
				if (notMatched && StringUtils.Find(s, currencyWord))
				{
					switch (currencyWord)
					{
						case "美分":
						{
							multiplier = 0.01;
							break;
						}

						case "先令":
						{
							multiplier = 0.05;
							break;
						}

						case "便士":
						{
							multiplier = 1.0 / 240;
							break;
						}
					}
					s = s.ReplaceAll(currencyWord, string.Empty);
					currencySign = (char)multiCharCurrencyWords[currencyWord];
					notMatched = false;
				}
			}
			// Then we check oneCharCurrencyWords
			if (notMatched)
			{
				foreach (string currencyWord_1 in oneCharCurrencyWords.Keys)
				{
					if (notMatched && StringUtils.Find(s, currencyWord_1))
					{
						// TODO: change multiplier
						s = s.ReplaceAll(currencyWord_1, string.Empty);
						currencySign = (char)oneCharCurrencyWords[currencyWord_1];
						notMatched = false;
					}
				}
			}
			// We check all other currency cases if we miss both dictionaries above
			if (notMatched)
			{
				foreach (string currencyWord_1 in ChineseNumberSequenceClassifier.CurrencyWordsValues)
				{
					if (notMatched && StringUtils.Find(s, currencyWord_1))
					{
						s = s.ReplaceAll(currencyWord_1, string.Empty);
						break;
					}
				}
			}
			// Now we assert the string should be all numbers
			string value = NormalizedNumberString(s, nextWord, multiplier);
			if (value == null)
			{
				return null;
			}
			else
			{
				return currencySign + value;
			}
		}

		/// <summary>Normalize a percent string.</summary>
		/// <remarks>Normalize a percent string. We handle both % and ‰.</remarks>
		/// <param name="s"/>
		/// <param name="nextWord"/>
		/// <returns/>
		private static string NormalizedPercentString(string s, string nextWord)
		{
			string ns = string.Empty;
			if (s.StartsWith("百分之"))
			{
				ns = NormalizedNumberString(Sharpen.Runtime.Substring(s, 3), nextWord, 1.0);
				if (ns != null)
				{
					ns += "%";
				}
			}
			else
			{
				if (s.StartsWith("千分之"))
				{
					ns = NormalizedNumberString(Sharpen.Runtime.Substring(s, 3), nextWord, 1.0);
					if (ns != null)
					{
						ns += "‰";
					}
				}
				else
				{
					if (s.EndsWith("%"))
					{
						// we also handle the case where the percent ends with a % character
						ns = NormalizedNumberString(Sharpen.Runtime.Substring(s, 0, s.Length - 1), nextWord, 1.0);
						if (ns != null)
						{
							ns += "%";
						}
					}
					else
					{
						if (s.EndsWith("‰"))
						{
							ns = NormalizedNumberString(Sharpen.Runtime.Substring(s, 0, s.Length - 1), nextWord, 1.0);
							ns += "‰";
						}
						else
						{
							// otherwise we assume the entire percent is a number
							ns = NormalizedNumberString(s, nextWord, 1.0);
							if (ns != null)
							{
								ns += "%";
							}
						}
					}
				}
			}
			return ns;
		}

		/// <summary>Normalize an ordinal string.</summary>
		/// <remarks>
		/// Normalize an ordinal string.
		/// If the string starts with "第", we assume the number is followed; otherwise
		/// we assume the entire body is a number.
		/// </remarks>
		/// <param name="s"/>
		/// <param name="nextWord"/>
		/// <returns/>
		private static string NormalizedOrdinalString(string s, string nextWord)
		{
			if (s.StartsWith("第"))
			{
				return NormalizedNumberString(Sharpen.Runtime.Substring(s, 1), nextWord, 1.0);
			}
			else
			{
				return NormalizedNumberString(s, nextWord, 1.0);
			}
		}

		/// <summary>Normalize a string into the corresponding standard numerical values (in String form).</summary>
		/// <remarks>
		/// Normalize a string into the corresponding standard numerical values (in String form).
		/// Note that this can only handle a string of pure numerical expressions, like
		/// "两万三千零七十二点五六" or "23072.56". Other NERs like MONEY or DATE needs to be handled
		/// in their own methods.
		/// In any case we fail, this method will just return a null.
		/// </remarks>
		/// <param name="s">The string input.</param>
		/// <param name="nextWord">The next word in sequence. This is likely to be useless for Chinese.</param>
		/// <param name="multiplier">A multiplier to make things simple for callers</param>
		/// <returns/>
		private static string NormalizedNumberString(string s, string nextWord, double multiplier)
		{
			// First remove unnecessary characters in the string
			s = s.Trim();
			s = s.ReplaceAll("[ \t\n\x0\f\r,]", string.Empty);
			// remove all unnecessary characters
			// In case of pure arabic numbers, return the straight value of it
			if (ArabicNumbersPattern.Matcher(s).Matches())
			{
				return PrettyNumber(string.Format("%f", multiplier * double.ValueOf(s)));
			}
			// If this is not all arabic, we assume it to be either Chinese literal or mix of Chinese literal and arabic
			// We handle decimal point first
			int decimalIndex = s.IndexOf(LiteralDecimalPoint);
			double decimalValue = double.ValueOf(0);
			if (decimalIndex != -1)
			{
				// handle decimal part
				decimalValue = NormalizeLiteralDecimalString(Sharpen.Runtime.Substring(s, decimalIndex + 1));
				// if fails at parsing decimal value, return null
				if (decimalValue == null)
				{
					return null;
				}
				// update s to be the integer part
				s = Sharpen.Runtime.Substring(s, 0, decimalIndex);
			}
			double integerValue = RecurNormalizeLiteralIntegerString(s);
			if (integerValue == null)
			{
				return null;
			}
			// both decimal and integer part are parsable, we combine them to form the final result
			// the formatting of numbers in Java is really annoying
			return PrettyNumber(string.Format("%f", multiplier * double.ValueOf(integerValue + decimalValue)));
		}

		/// <summary>Recursively parse a integer String expressed in either Chinese or a mix of Chinese and arabic numbers.</summary>
		/// <param name="s"/>
		/// <returns/>
		private static double RecurNormalizeLiteralIntegerString(string s)
		{
			// If empty, return 0
			if (s.IsEmpty())
			{
				return double.ValueOf(0);
			}
			// TODO: check if it is valid. It is possible that this is a vague number like "五六十" which cannot be parsed by current implementation.
			// In case of pure arabic numbers, return the straight value of it
			if (ArabicNumbersPattern.Matcher(s).Matches())
			{
				return double.ValueOf(s);
			}
			//If s has more than 1 char and first char is 零 or 〇, it is likely
			// to be useless
			if (s.Length > 1 && (s.StartsWith("零") || s.StartsWith("〇")))
			{
				s = Sharpen.Runtime.Substring(s, 1);
			}
			//If there is only one char left and we can quantify it, we return the value of it
			if (s.Length == 1 && wordsToValues.ContainsKey(s))
			{
				return double.ValueOf(wordsToValues.GetCount(s));
			}
			// Now parse the integer, making use of the compositionality of Chinese literal numbers
			double value;
			value = CompositeAtUnitIfExists(s, "亿");
			if (value != null)
			{
				return value;
			}
			else
			{
				value = CompositeAtUnitIfExists(s, "万");
			}
			if (value != null)
			{
				return value;
			}
			else
			{
				value = CompositeAtUnitIfExists(s, "千");
			}
			if (value != null)
			{
				return value;
			}
			else
			{
				value = CompositeAtUnitIfExists(s, "百");
			}
			if (value != null)
			{
				return value;
			}
			else
			{
				value = CompositeAtUnitIfExists(s, "十");
			}
			if (value != null)
			{
				return value;
			}
			// otherwise we fail to parse and just return null
			return null;
		}

		/// <summary>Check if a unit exists in the literal string.</summary>
		/// <remarks>
		/// Check if a unit exists in the literal string. If so, parse it by making use of
		/// the compositionality; otherwise return null.
		/// </remarks>
		/// <param name="s"/>
		/// <param name="unit"/>
		/// <returns/>
		private static double CompositeAtUnitIfExists(string s, string unit)
		{
			// invalid unit
			if (!quantityUnitToValues.ContainsKey(unit))
			{
				return null;
			}
			int idx = s.IndexOf(unit);
			if (idx != -1)
			{
				double first = double.ValueOf(1.0);
				// Here we need special handling for 十 and 百 when they occur as the first char
				// As in Chinese 十二 is very common, 百二十 is sometimes valid as well.
				if (("十".Equals(unit) || "百".Equals(unit)) && idx == 0)
				{
				}
				else
				{
					// do nothing
					// otherwise we try to parse the value before the unit
					first = RecurNormalizeLiteralIntegerString(Sharpen.Runtime.Substring(s, 0, idx));
				}
				double second = RecurNormalizeLiteralIntegerString(Sharpen.Runtime.Substring(s, idx + 1));
				if (first != null && second != null)
				{
					return double.ValueOf(first * quantityUnitToValues.GetCount(unit) + second);
				}
			}
			// return null if unit is not present or fails to parse
			return null;
		}

		/// <summary>Normalize decimal part of the string.</summary>
		/// <remarks>Normalize decimal part of the string. Note that this only handles Chinese literal expressions.</remarks>
		/// <param name="s"/>
		/// <returns/>
		private static double NormalizeLiteralDecimalString(string s)
		{
			// if s is empty return 0
			if (s.IsEmpty())
			{
				return double.ValueOf(0);
			}
			// if s is not valid Chinese literal decimal expressions, return null
			if (!ChineseLiteralDecimalPattern.Matcher(s).Matches())
			{
				return null;
			}
			// after checking we assume the decimal part should be correct
			double decimalValue = 0;
			double @base = 1;
			for (int i = 0; i < sz; i++)
			{
				// update base
				@base *= 0.1;
				string c = char.ToString(s[i]);
				if (!wordsToValues.ContainsKey(c))
				{
					// some uncatchable character is present, return null
					return null;
				}
				double v = wordsToValues.GetCount(c);
				decimalValue += v * @base;
			}
			return double.ValueOf(decimalValue);
		}

		private static string NormalizeMonthOrDay(string s, string context)
		{
			int ctx = -1;
			if (!context.Equals("XX"))
			{
				ctx = System.Convert.ToInt32(context);
			}
			if (monthDayModifiers.Contains(s))
			{
				if (ctx >= 0)
				{
					// todo: this is unsafe as it's not bound-checked for validity
					return string.Format("%02d", ctx + monthDayModifiers[s]);
				}
				else
				{
					return "XX";
				}
			}
			else
			{
				string candidate;
				if (s == null)
				{
					return "XX";
				}
				else
				{
					if (s.Matches(ChineseDateNumeralsPattern + "+"))
					{
						candidate = PrettyNumber(string.Format("%f", RecurNormalizeLiteralIntegerString(s)));
					}
					else
					{
						candidate = s;
					}
				}
				if (candidate.Length < 2)
				{
					candidate = "0" + candidate;
				}
				return candidate;
			}
		}

		private static string NormalizeYear(string s, string contextYear)
		{
			return NormalizeYear(s, contextYear, false);
		}

		private static string NormalizeYear(string s, string contextYear, bool strict)
		{
			int ctx = -1;
			if (!contextYear.Equals("XXXX"))
			{
				ctx = System.Convert.ToInt32(contextYear);
			}
			if (yearModifiers.Contains(s))
			{
				if (ctx >= 0)
				{
					return string.Format("%d", ctx + yearModifiers[s]);
				}
				else
				{
					return "XXXX";
				}
			}
			else
			{
				string candidate;
				StringBuilder yearcandidate = new StringBuilder();
				for (int i = 0; i < s.Length; i++)
				{
					string t = s[i].ToString();
					if (ChineseLiteralDecimalPattern.Matcher(t).Matches())
					{
						if (wordsToValues.ContainsKey(t))
						{
							yearcandidate.Append((int)wordsToValues.GetCount(t));
						}
						else
						{
							// something unexpected happened
							return null;
						}
					}
					else
					{
						yearcandidate.Append(t);
					}
				}
				candidate = yearcandidate.ToString();
				if (candidate.Length != 2)
				{
					return candidate;
				}
				if (ctx < 0)
				{
					// use the current year as reference point for two digit year normalization by default
					ctx = System.Convert.ToInt32(new SimpleDateFormat("yyyy").Format(new DateTime()));
				}
				// note: this is a very crude heuristic for determining actual year from two digit expressions
				int cand = int.Parse(candidate);
				if ((strict && cand >= (ctx % 100)) || cand > (ctx % 100 + 10))
				{
					// referring to the previous century
					cand += (ctx / 100 - 1) * 100;
				}
				else
				{
					// referring to the same century
					cand += (ctx / 100) * 100;
				}
				return string.Format("%d", cand);
			}
		}

		/// <summary>Normalizes date strings.</summary>
		/// <param name="s">Input date string</param>
		/// <param name="ctxdate">Context date (usually doc_date)</param>
		/// <returns>Normalized Timex expression of the input date string</returns>
		public static string NormalizeDateString(string s, string ctxdate)
		{
			// TODO [pengqi]: need to handle basic localization ("在七月二日到[八日]间")
			// TODO [pengqi]: need to handle literal numeral dates (usually used in events, e.g. "三一五" for 03-15)
			// TODO [pengqi]: might need to add a pattern for centuries ("上世纪90年代")?
			Pattern p;
			Matcher m;
			string ctxyear = "XXXX";
			string ctxmonth = "XX";
			string ctxday = "XX";
			// set up context date
			if (ctxdate != null)
			{
				p = Pattern.Compile("^" + BasicYyyymmddPattern + "$");
				m = p.Matcher(ctxdate);
				if (m.Find() && m.GroupCount() == 3)
				{
					ctxyear = m.Group(1);
					ctxmonth = m.Group(2);
					ctxday = m.Group(3);
				}
			}
			p = Pattern.Compile("^" + BirthDecadePattern + "$");
			m = p.Matcher(s);
			if (m.Find() && m.GroupCount() == 1)
			{
				StringBuilder res = new StringBuilder();
				res.Append(Sharpen.Runtime.Substring(NormalizeYear(m.Group(1), ctxyear, true), 0, 3) + "X");
				res.Append("-XX-XX");
				return res.ToString();
			}
			p = Pattern.Compile("^" + RelativeTimePattern + "$");
			m = p.Matcher(s);
			if (m.Find() && m.GroupCount() == 1)
			{
				StringBuilder res = new StringBuilder();
				res.Append(ctxyear);
				res.Append("-");
				res.Append(ctxmonth);
				res.Append("-");
				res.Append(NormalizeMonthOrDay(m.Group(1), ctxday));
				return res.ToString();
			}
			p = Pattern.Compile("^" + BasicYyyymmddPattern + "$");
			m = p.Matcher(s);
			if (m.Find() && m.GroupCount() == 3)
			{
				StringBuilder res = new StringBuilder();
				res.Append(NormalizeYear(m.Group(1), ctxyear));
				res.Append("-");
				res.Append(NormalizeMonthOrDay(m.Group(2), ctxmonth));
				res.Append("-");
				res.Append(NormalizeMonthOrDay(m.Group(3), ctxday));
				return res.ToString();
			}
			p = Pattern.Compile("^" + BasicMmddPattern + "$");
			m = p.Matcher(s);
			if (m.Find() && m.GroupCount() == 2)
			{
				StringBuilder res = new StringBuilder();
				res.Append(ctxyear);
				res.Append("-");
				res.Append(NormalizeMonthOrDay(m.Group(1), ctxmonth));
				res.Append("-");
				res.Append(NormalizeMonthOrDay(m.Group(2), ctxday));
				return res.ToString();
			}
			p = Pattern.Compile("^" + BasicDdPattern + "$");
			m = p.Matcher(s);
			if (m.Find() && m.GroupCount() == 1)
			{
				StringBuilder res = new StringBuilder();
				res.Append(ctxyear);
				res.Append("-");
				res.Append(ctxmonth);
				res.Append("-");
				res.Append(NormalizeMonthOrDay(m.Group(1), ctxday));
				return res.ToString();
			}
			p = Pattern.Compile("^" + EnglishMmddyyyyPattern + "$");
			m = p.Matcher(s);
			if (m.Find() && m.GroupCount() == 3)
			{
				StringBuilder res = new StringBuilder();
				if (m.Group(3) == null)
				{
					res.Append(ctxyear);
				}
				else
				{
					res.Append(NormalizeYear(m.Group(3), ctxyear));
				}
				res.Append("-");
				res.Append(NormalizeMonthOrDay(m.Group(1), ctxmonth));
				res.Append("-");
				res.Append(NormalizeMonthOrDay(m.Group(2), ctxday));
				return res.ToString();
			}
			return s;
		}

		/// <summary>Concatenate entity annotations to a String.</summary>
		/// <remarks>
		/// Concatenate entity annotations to a String. Note that Chinese does not use space to separate
		/// tokens so we will follow this convention here.
		/// </remarks>
		/// <param name="l"/>
		/// <?/>
		/// <returns/>
		private static string SingleEntityToString<E>(IList<E> l)
			where E : ICoreMap
		{
			string entityType = l[0].Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
			StringBuilder sb = new StringBuilder();
			foreach (E w in l)
			{
				if (!w.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Equals(entityType))
				{
					log.Error("differing NER tags detected in entity: " + l);
					throw new Exception("Error with entity construction, two tokens had inconsistent NER tags");
				}
				sb.Append(w.Get(typeof(CoreAnnotations.TextAnnotation)));
			}
			return sb.ToString();
		}

		private static string PrettyNumber(string s)
		{
			if (s == null)
			{
				return null;
			}
			s = !s.Contains(".") ? s : s.ReplaceAll("0*$", string.Empty).ReplaceAll("\\.$", string.Empty);
			return s;
		}

		/// <summary>Fix up the NER sequence in case this is necessary.</summary>
		/// <param name="list"/>
		/// <?/>
		private static void FixupNerBeforeNormalization<E>(IList<E> list)
			where E : ICoreMap
		{
		}
	}
}
