using System;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Ling.Tokensregex.Types;
using Edu.Stanford.Nlp.Util;





using Org.Joda.Time;
using Org.Joda.Time.Format;


namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Time specific patterns and formatting</summary>
	/// <author>Angel Chang</author>
	public class TimeFormatter
	{
		private TimeFormatter()
		{
		}

		public class JavaDateFormatExtractor : IFunction<ICoreMap, IValue>
		{
			private static readonly Type textAnnotationField = typeof(CoreAnnotations.TextAnnotation);

			private readonly SimpleDateFormat format;

			public JavaDateFormatExtractor(string pattern)
			{
				// static methods/classes
				this.format = new SimpleDateFormat(pattern);
			}

			public virtual IValue Apply(ICoreMap m)
			{
				try
				{
					// TODO: Allow specification of locale, pivot year (set2DigitYearStart) for interpreting 2 digit years
					string str = m.Get(textAnnotationField);
					DateTime d = format.Parse(str);
					return new Expressions.PrimitiveValue("GroundedTime", new SUTime.GroundedTime(new Instant(d.GetTime())));
				}
				catch (ParseException)
				{
					return null;
				}
			}
		}

		public class JodaDateTimeFormatExtractor : IFunction<ICoreMap, IValue>
		{
			private static readonly Type textAnnotationField = typeof(CoreAnnotations.TextAnnotation);

			private readonly DateTimeFormatter formatter;

			public JodaDateTimeFormatExtractor(DateTimeFormatter formatter)
			{
				this.formatter = formatter;
			}

			public JodaDateTimeFormatExtractor(string pattern)
			{
				this.formatter = DateTimeFormat.ForPattern(pattern);
			}

			public virtual IValue Apply(ICoreMap m)
			{
				try
				{
					string str = m.Get(textAnnotationField);
					// TODO: Allow specification of pivot year (withPivotYear) for interpreting 2 digit years
					DateTime d = formatter.ParseDateTime(str);
					return new Expressions.PrimitiveValue("GroundedTime", new SUTime.GroundedTime(d));
				}
				catch (ArgumentException)
				{
					return null;
				}
			}
		}

		internal class ApplyActionWrapper<I, O> : IFunction<I, O>
		{
			private readonly Env env;

			private readonly IFunction<I, O> @base;

			private readonly IExpression action;

			internal ApplyActionWrapper(Env env, IFunction<I, O> @base, IExpression action)
			{
				this.env = env;
				this.@base = @base;
				this.action = action;
			}

			public virtual O Apply(I @in)
			{
				O v = @base.Apply(@in);
				if (action != null)
				{
					action.Evaluate(env, v);
				}
				return v;
			}
		}

		internal class TimePatternExtractRuleCreator : SequenceMatchRules.AnnotationExtractRuleCreator
		{
			private static void UpdateExtractRule(SequenceMatchRules.AnnotationExtractRule r, Env env, Pattern pattern, IFunction<string, IValue> extractor)
			{
				MatchedExpression.SingleAnnotationExtractor annotationExtractor = SequenceMatchRules.CreateAnnotationExtractor(env, r);
				annotationExtractor.valueExtractor = new SequenceMatchRules.CoreMapFunctionApplier<string, IValue>(env, r.annotationField, extractor);
				r.extractRule = new SequenceMatchRules.CoreMapExtractRule<string, MatchedExpression>(env, r.annotationField, new SequenceMatchRules.StringPatternExtractRule<MatchedExpression>(pattern, new SequenceMatchRules.StringMatchedExpressionExtractor(
					annotationExtractor, r.matchedExpressionGroup)));
				r.filterRule = new SequenceMatchRules.AnnotationMatchedFilter(annotationExtractor);
				r.pattern = pattern;
			}

			private static void UpdateExtractRule(SequenceMatchRules.AnnotationExtractRule r, Env env, IFunction<ICoreMap, IValue> extractor)
			{
				MatchedExpression.SingleAnnotationExtractor annotationExtractor = SequenceMatchRules.CreateAnnotationExtractor(env, r);
				annotationExtractor.valueExtractor = extractor;
				r.extractRule = new SequenceMatchRules.CoreMapExtractRule<IList<ICoreMap>, MatchedExpression>(env, r.annotationField, new SequenceMatchRules.BasicSequenceExtractRule(annotationExtractor));
				r.filterRule = new SequenceMatchRules.AnnotationMatchedFilter(annotationExtractor);
			}

			public override SequenceMatchRules.AnnotationExtractRule Create(Env env, IDictionary<string, object> attributes)
			{
				SequenceMatchRules.AnnotationExtractRule r = base.Create(env, attributes);
				if (r.ruleType == null)
				{
					r.ruleType = "time";
				}
				string expr = Expressions.AsObject(env, attributes["pattern"]);
				string formatter = Expressions.AsObject(env, attributes["formatter"]);
				IExpression action = Expressions.AsExpression(env, attributes["action"]);
				string localeString = Expressions.AsObject(env, attributes["locale"]);
				r.pattern = expr;
				if (formatter == null)
				{
					if (r.annotationField == null)
					{
						r.annotationField = EnvLookup.GetDefaultTextAnnotationKey(env);
					}
					/* Parse pattern and figure out what the result should be.... */
					TimeFormatter.CustomDateFormatExtractor formatExtractor = new TimeFormatter.CustomDateFormatExtractor(expr, localeString);
					//SequenceMatchRules.Expression result = (SequenceMatchRules.Expression) attributes.get("result");
					UpdateExtractRule(r, env, formatExtractor.GetTextPattern(), new TimeFormatter.ApplyActionWrapper<string, IValue>(env, formatExtractor, action));
				}
				else
				{
					if ("org.joda.time.format.DateTimeFormat".Equals(formatter))
					{
						if (r.annotationField == null)
						{
							r.annotationField = r.tokensAnnotationField;
						}
						UpdateExtractRule(r, env, new TimeFormatter.ApplyActionWrapper<ICoreMap, IValue>(env, new TimeFormatter.JodaDateTimeFormatExtractor(expr), action));
					}
					else
					{
						if ("org.joda.time.format.ISODateTimeFormat".Equals(formatter))
						{
							if (r.annotationField == null)
							{
								r.annotationField = r.tokensAnnotationField;
							}
							try
							{
								MethodInfo m = typeof(ISODateTimeFormat).GetMethod(expr);
								DateTimeFormatter dtf = (DateTimeFormatter)m.Invoke(null);
								UpdateExtractRule(r, env, new TimeFormatter.ApplyActionWrapper<ICoreMap, IValue>(env, new TimeFormatter.JodaDateTimeFormatExtractor(expr), action));
							}
							catch (Exception ex)
							{
								throw new Exception("Error creating DateTimeFormatter", ex);
							}
						}
						else
						{
							if ("java.text.SimpleDateFormat".Equals(formatter))
							{
								if (r.annotationField == null)
								{
									r.annotationField = r.tokensAnnotationField;
								}
								UpdateExtractRule(r, env, new TimeFormatter.ApplyActionWrapper<ICoreMap, IValue>(env, new TimeFormatter.JavaDateFormatExtractor(expr), action));
							}
							else
							{
								throw new ArgumentException("Unsupported formatter: " + formatter);
							}
						}
					}
				}
				return r;
			}
		}

		/// <summary>Converts time string pattern to text pattern.</summary>
		public class CustomDateFormatExtractor : IFunction<string, IValue>
		{
			private readonly TimeFormatter.FormatterBuilder builder;

			private readonly string timePattern;

			private readonly Pattern textPattern;

			public CustomDateFormatExtractor(string timePattern, string localeString)
			{
				/*
				* Rules for parsing time specific patterns.
				* Patterns are similar to time patterns used by JodaTime combined with a simplified regex expression
				*
				# y       year                         year          1996                         y
				# M       month of year                month         July; Jul; 07                M
				# d       day of month                 number        10                           d
				# H       hour of day (0~23)           number        0                            H
				# k       clockhour of day (1~24)      number        24                           k
				# m       minute of hour               number        30                           m
				# s       second of minute             number        55                           s
				# S       fraction of second           number        978                          S (Millisecond)
				# a       half day of day marker       am/pm
				*/
				Locale locale = (localeString != null) ? new Locale(localeString) : Locale.GetDefault();
				this.timePattern = timePattern;
				builder = new TimeFormatter.FormatterBuilder();
				builder.locale = locale;
				ParsePatternTo(builder, timePattern);
				textPattern = builder.ToTextPattern();
			}

			public virtual Pattern GetTextPattern()
			{
				return textPattern;
			}

			public virtual IValue Apply(string str)
			{
				IValue v = null;
				Matcher m = textPattern.Matcher(str);
				if (m.Matches())
				{
					return Apply(m);
				}
				return v;
			}

			public virtual IValue Apply(IMatchResult m)
			{
				SUTime.Temporal t = new SUTime.PartialTime();
				foreach (TimeFormatter.FormatComponent fc in builder.pieces)
				{
					int group = fc.GetGroup();
					if (group > 0)
					{
						string fieldValueStr = m.Group(group);
						if (fieldValueStr != null)
						{
							try
							{
								t = fc.UpdateTemporal(t, fieldValueStr);
							}
							catch (ArgumentException)
							{
								return null;
							}
						}
					}
				}
				return new Expressions.PrimitiveValue("Temporal", t);
			}
		}

		private abstract class FormatComponent
		{
			internal int group = -1;

			internal string quantifier = null;

			public virtual void AppendQuantifier(string str)
			{
				if (quantifier != null)
				{
					quantifier = quantifier + str;
				}
				else
				{
					quantifier = str;
				}
			}

			public virtual StringBuilder AppendRegex(StringBuilder sb)
			{
				if (group > 0)
				{
					sb.Append('(');
				}
				AppendRegex0(sb);
				if (quantifier != null)
				{
					sb.Append(quantifier);
				}
				if (group > 0)
				{
					sb.Append(')');
				}
				return sb;
			}

			protected internal abstract StringBuilder AppendRegex0(StringBuilder sb);

			public virtual SUTime.Temporal UpdateTemporal(SUTime.Temporal t, string fieldValueStr)
			{
				return t;
			}

			public virtual int GetGroup()
			{
				return group;
			}
		}

		private abstract class DateTimeFieldComponent : TimeFormatter.FormatComponent
		{
			internal DateTimeFieldType fieldType;

			public virtual int ParseValue(string str)
			{
				return null;
			}

			public virtual DateTimeFieldType GetDateTimeFieldType()
			{
				return fieldType;
			}

			public override SUTime.Temporal UpdateTemporal(SUTime.Temporal t, string fieldValueStr)
			{
				DateTimeFieldType dt = GetDateTimeFieldType();
				if (fieldValueStr != null && dt != null)
				{
					int v = ParseValue(fieldValueStr);
					if (v != null)
					{
						Partial pt = new Partial();
						pt = JodaTimeUtils.SetField(pt, dt, v);
						t = t.Intersect(new SUTime.PartialTime(pt));
					}
					else
					{
						throw new ArgumentException("Cannot interpret " + fieldValueStr + " for " + fieldType);
					}
				}
				return t;
			}
		}

		private class NumericDateComponent : TimeFormatter.DateTimeFieldComponent
		{
			private readonly int minValue;

			private readonly int maxValue;

			private readonly int minDigits;

			private readonly int maxDigits;

			public NumericDateComponent(DateTimeFieldType fieldType, int minDigits, int maxDigits)
			{
				this.fieldType = fieldType;
				this.minDigits = minDigits;
				this.maxDigits = maxDigits;
				MutableDateTime dt = new MutableDateTime(0L, DateTimeZone.Utc);
				MutableDateTime.Property property = dt.Property(fieldType);
				minValue = property.GetMinimumValueOverall();
				maxValue = property.GetMaximumValueOverall();
			}

			protected internal override StringBuilder AppendRegex0(StringBuilder sb)
			{
				if (maxDigits > 5 || minDigits != maxDigits)
				{
					sb.Append("\\d{").Append(minDigits).Append(',').Append(maxDigits).Append('}');
				}
				else
				{
					for (int i = 0; i < minDigits; i++)
					{
						sb.Append("\\d");
					}
				}
				return sb;
			}

			public override int ParseValue(string str)
			{
				int v = System.Convert.ToInt32(str);
				if (v >= minValue && v <= maxValue)
				{
					return v;
				}
				else
				{
					return null;
				}
			}
		}

		private class RelaxedNumericDateComponent : TimeFormatter.FormatComponent
		{
			internal TimeFormatter.NumericDateComponent[] possibleNumericDateComponents;

			internal int minDigits;

			internal int maxDigits;

			public RelaxedNumericDateComponent(DateTimeFieldType[] fieldTypes, int minDigits, int maxDigits)
			{
				this.minDigits = minDigits;
				this.maxDigits = maxDigits;
				possibleNumericDateComponents = new TimeFormatter.NumericDateComponent[fieldTypes.Length];
				for (int i = 0; i < fieldTypes.Length; i++)
				{
					possibleNumericDateComponents[i] = new TimeFormatter.NumericDateComponent(fieldTypes[i], minDigits, maxDigits);
				}
			}

			protected internal override StringBuilder AppendRegex0(StringBuilder sb)
			{
				if (maxDigits > 5 || minDigits != maxDigits)
				{
					sb.Append("\\d{").Append(minDigits).Append(",").Append(maxDigits).Append("}");
				}
				else
				{
					for (int i = 0; i < minDigits; i++)
					{
						sb.Append("\\d");
					}
				}
				return sb;
			}

			public override SUTime.Temporal UpdateTemporal(SUTime.Temporal t, string fieldValueStr)
			{
				if (fieldValueStr != null)
				{
					foreach (TimeFormatter.NumericDateComponent c in possibleNumericDateComponents)
					{
						int v = c.ParseValue(fieldValueStr);
						if (v != null)
						{
							t = c.UpdateTemporal(t, fieldValueStr);
							return t;
						}
					}
					throw new ArgumentException("Cannot interpret " + fieldValueStr);
				}
				return t;
			}
		}

		private static readonly IComparator<string> StringLengthRevComparator = null;

		private class TextDateComponent : TimeFormatter.DateTimeFieldComponent
		{
			internal IDictionary<string, int> valueMapping;

			internal IList<string> validValues;

			internal Locale locale;

			internal int minValue;

			internal int maxValue;

			internal bool isShort;

			public TextDateComponent()
			{
			}

			public TextDateComponent(DateTimeFieldType fieldType, Locale locale, bool isShort)
			{
				this.fieldType = fieldType;
				this.locale = locale;
				this.isShort = isShort;
				MutableDateTime dt = new MutableDateTime(0L, DateTimeZone.Utc);
				MutableDateTime.Property property = dt.Property(fieldType);
				minValue = property.GetMinimumValueOverall();
				maxValue = property.GetMaximumValueOverall();
				this.validValues = new List<string>(maxValue - minValue + 1);
				this.valueMapping = Generics.NewHashMap();
				for (int i = minValue; i <= maxValue; i++)
				{
					property.Set(i);
					if (isShort != null)
					{
						if (isShort)
						{
							AddValue(property.GetAsShortText(locale), i);
						}
						else
						{
							AddValue(property.GetAsText(locale), i);
						}
					}
					else
					{
						AddValue(property.GetAsShortText(locale), i);
						AddValue(property.GetAsText(locale), i);
					}
				}
				// Order by length for regex
				validValues.Sort(StringLengthRevComparator);
			}

			public virtual void AddValue(string str, int v)
			{
				validValues.Add(str);
				valueMapping[str.ToLower(locale)] = v;
			}

			public override int ParseValue(string str)
			{
				str = str.ToLower(locale);
				int v = valueMapping[str];
				return v;
			}

			protected internal override StringBuilder AppendRegex0(StringBuilder sb)
			{
				bool first = true;
				foreach (string v in validValues)
				{
					if (first)
					{
						first = false;
					}
					else
					{
						sb.Append("|");
					}
					sb.Append(Pattern.Quote(v));
				}
				return sb;
			}
		}

		private class TimeZoneOffsetComponent : TimeFormatter.FormatComponent
		{
			internal string zeroOffsetParseText;

			public TimeZoneOffsetComponent(string zeroOffsetParseText)
			{
				// Text indicating timezone offset is zero
				// TimezoneOffset is + or - followed by
				// hh
				// hhmm
				// hhmmss
				// hhmmssSSS
				// hh:mm
				// hh:mm:ss
				// hh:mm:ss.SSS
				this.zeroOffsetParseText = zeroOffsetParseText;
			}

			protected internal override StringBuilder AppendRegex0(StringBuilder sb)
			{
				sb.Append("[+-]\\d\\d(?::?\\d\\d(?::?\\d\\d(?:[.,]?\\d{1,3})?)?)?");
				if (zeroOffsetParseText != null)
				{
					sb.Append("|").Append(Pattern.Quote(zeroOffsetParseText));
				}
				return sb;
			}

			private static int ParseInteger(string str, int pos, int length)
			{
				return System.Convert.ToInt32(Sharpen.Runtime.Substring(str, pos, pos + length));
			}

			public virtual int ParseOffsetMillis(string str)
			{
				int offset = 0;
				if (zeroOffsetParseText != null && Sharpen.Runtime.EqualsIgnoreCase(str, zeroOffsetParseText))
				{
					return offset;
				}
				bool negative = false;
				if (str.StartsWith("+"))
				{
				}
				else
				{
					if (str.StartsWith("-"))
					{
						negative = true;
					}
					else
					{
						throw new ArgumentException("Invalid date time zone offset " + str);
					}
				}
				int pos = 1;
				// Parse hours
				offset += DateTimeConstants.MillisPerHour * ParseInteger(str, pos, 2);
				pos += 2;
				if (pos < str.Length)
				{
					// Parse minutes
					if (!char.IsDigit(str[pos]))
					{
						pos++;
					}
					offset += DateTimeConstants.MillisPerMinute * ParseInteger(str, pos, 2);
					pos += 2;
					if (pos < str.Length)
					{
						// Parse seconds
						if (!char.IsDigit(str[pos]))
						{
							pos++;
						}
						offset += DateTimeConstants.MillisPerSecond * ParseInteger(str, pos, 2);
						pos += 2;
						if (pos < str.Length)
						{
							// Parse fraction of seconds
							if (!char.IsDigit(str[pos]))
							{
								pos++;
							}
							int digits = str.Length - pos;
							if (digits > 0)
							{
								if (digits <= 3)
								{
									int frac = ParseInteger(str, pos, digits);
									if (digits == 1)
									{
										offset += frac * 100;
									}
									else
									{
										if (digits == 2)
										{
											offset += frac * 10;
										}
										else
										{
											if (digits == 3)
											{
												offset += frac;
											}
										}
									}
								}
								else
								{
									throw new ArgumentException("Invalid date time zone offset " + str);
								}
							}
						}
					}
				}
				if (negative)
				{
					offset = -offset;
				}
				return offset;
			}

			public override SUTime.Temporal UpdateTemporal(SUTime.Temporal t, string fieldValueStr)
			{
				int offset = ParseOffsetMillis(fieldValueStr);
				DateTimeZone dtz = DateTimeZone.ForOffsetMillis(offset);
				return t.SetTimeZone(dtz);
			}
		}

		private static string MakeRegex(IList<string> strs)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (string v in strs)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					sb.Append("|");
				}
				sb.Append(Pattern.Quote(v));
			}
			return sb.ToString();
		}

		private class TimeZoneIdComponent : TimeFormatter.FormatComponent
		{
			internal static readonly IDictionary<string, DateTimeZone> timeZonesById;

			internal static readonly IList<string> timeZoneIds;

			internal static readonly string timeZoneIdsRegex;

			static TimeZoneIdComponent()
			{
				// Timezones
				//  ID - US/Pacific
				//  Name - Pacific Standard Time (or Pacific Daylight Time)
				//  ShortName  PST (or PDT depending on input milliseconds)
				//  NameKey    PST (or PDT depending on input milliseconds)
				timeZoneIds = new List<string>(DateTimeZone.GetAvailableIDs());
				timeZonesById = Generics.NewHashMap();
				foreach (string str in timeZoneIds)
				{
					DateTimeZone dtz = DateTimeZone.ForID(str);
					timeZonesById[str.ToLower()] = dtz;
				}
				//        System.out.println(str);
				//        long time = System.currentTimeMillis();
				//        System.out.println(dtz.getShortName(time));
				//        System.out.println(dtz.getName(time));
				//        System.out.println(dtz.getNameKey(time));
				//        System.out.println();
				// Order by length for regex
				timeZoneIds.Sort(StringLengthRevComparator);
				timeZoneIdsRegex = MakeRegex(timeZoneIds);
			}

			public TimeZoneIdComponent()
			{
			}

			private static DateTimeZone ParseDateTimeZone(string str)
			{
				str = str.ToLower();
				DateTimeZone v = timeZonesById[str];
				return v;
			}

			protected internal override StringBuilder AppendRegex0(StringBuilder sb)
			{
				sb.Append(timeZoneIdsRegex);
				return sb;
			}

			public override SUTime.Temporal UpdateTemporal(SUTime.Temporal t, string fieldValueStr)
			{
				if (fieldValueStr != null)
				{
					DateTimeZone dtz = ParseDateTimeZone(fieldValueStr);
					return t.SetTimeZone(dtz);
				}
				return t;
			}
		}

		private class TimeZoneComponent : TimeFormatter.FormatComponent
		{
			internal Locale locale;

			internal static IDictionary<Locale, CollectionValuedMap<string, DateTimeZone>> timeZonesByName = Generics.NewHashMap();

			internal static IDictionary<Locale, IList<string>> timeZoneNames = Generics.NewHashMap();

			internal static IDictionary<Locale, string> timeZoneRegexes = Generics.NewHashMap();

			public TimeZoneComponent(Locale locale)
			{
				this.locale = locale;
				lock (typeof(TimeFormatter.TimeZoneComponent))
				{
					string regex = timeZoneRegexes[locale];
					if (regex == null)
					{
						UpdateTimeZoneNames(locale);
					}
				}
			}

			private static void UpdateTimeZoneNames(Locale locale)
			{
				long time1 = new SUTime.IsoDate(2013, 1, 1).GetJodaTimeInstant().GetMillis();
				long time2 = new SUTime.IsoDate(2013, 6, 1).GetJodaTimeInstant().GetMillis();
				CollectionValuedMap<string, DateTimeZone> tzMap = new CollectionValuedMap<string, DateTimeZone>();
				foreach (DateTimeZone dtz in TimeFormatter.TimeZoneIdComponent.timeZonesById.Values)
				{
					// standard timezones
					tzMap.Add(dtz.GetShortName(time1, locale).ToLower(), dtz);
					tzMap.Add(dtz.GetName(time1, locale).ToLower(), dtz);
					// Add about half a year to get day light savings timezones...
					tzMap.Add(dtz.GetShortName(time2, locale).ToLower(), dtz);
					tzMap.Add(dtz.GetName(time2, locale).ToLower(), dtz);
				}
				//      tzMap.add(dtz.getNameKey(time).toLowerCase(), dtz);
				//      tzMap.add(dtz.getID().toLowerCase(), dtz);
				// Order by length for regex
				IList<string> tzNames = new List<string>(tzMap.Keys);
				tzNames.Sort(StringLengthRevComparator);
				string tzRegex = MakeRegex(tzNames);
				lock (typeof(TimeFormatter.TimeZoneComponent))
				{
					timeZoneNames[locale] = tzNames;
					timeZonesByName[locale] = tzMap;
					timeZoneRegexes[locale] = tzRegex;
				}
			}

			public virtual DateTimeZone ParseDateTimeZone(string str)
			{
				// TODO: do something about these multiple timezones that match the same name...
				// pick one based on location
				str = str.ToLower();
				CollectionValuedMap<string, DateTimeZone> tzMap = timeZonesByName[locale];
				ICollection<DateTimeZone> v = tzMap[str];
				if (v == null || v.IsEmpty())
				{
					return null;
				}
				else
				{
					return v.GetEnumerator().Current;
				}
			}

			protected internal override StringBuilder AppendRegex0(StringBuilder sb)
			{
				string regex = timeZoneRegexes[locale];
				sb.Append(regex);
				return sb;
			}

			public override SUTime.Temporal UpdateTemporal(SUTime.Temporal t, string fieldValueStr)
			{
				if (fieldValueStr != null)
				{
					DateTimeZone dtz = ParseDateTimeZone(fieldValueStr);
					return t.SetTimeZone(dtz);
				}
				return t;
			}
		}

		private class LiteralComponent : TimeFormatter.FormatComponent
		{
			private readonly string text;

			public LiteralComponent(string str)
			{
				this.text = str;
			}

			protected internal override StringBuilder AppendRegex0(StringBuilder sb)
			{
				sb.Append(Pattern.Quote(text));
				return sb;
			}
		}

		private class RegexComponent : TimeFormatter.FormatComponent
		{
			private readonly string regex;

			public RegexComponent(string regex)
			{
				this.regex = regex;
			}

			protected internal override StringBuilder AppendRegex0(StringBuilder sb)
			{
				sb.Append(regex);
				return sb;
			}
		}

		private class FormatterBuilder
		{
			internal bool useRelaxedHour = true;

			internal Locale locale;

			internal DateTimeFormatterBuilder builder = new DateTimeFormatterBuilder();

			internal IList<TimeFormatter.FormatComponent> pieces = new List<TimeFormatter.FormatComponent>();

			internal int curGroup = 0;

			public virtual DateTimeFormatter ToFormatter()
			{
				return builder.ToFormatter();
			}

			public virtual string ToTextRegex()
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("\\b");
				foreach (TimeFormatter.FormatComponent fc in pieces)
				{
					fc.AppendRegex(sb);
				}
				sb.Append("\\b");
				return sb.ToString();
			}

			public virtual Pattern ToTextPattern()
			{
				return Pattern.Compile(ToTextRegex(), Pattern.CaseInsensitive | Pattern.UnicodeCase);
			}

			private void AppendNumericFields(DateTimeFieldType[] fieldTypes, int digits)
			{
				AppendNumericFields(fieldTypes, digits, digits);
			}

			private void AppendNumericFields(DateTimeFieldType[] fieldTypes, int minDigits, int maxDigits)
			{
				AppendComponent(new TimeFormatter.RelaxedNumericDateComponent(fieldTypes, minDigits, maxDigits), true);
			}

			private void AppendNumericField(DateTimeFieldType fieldType, int digits)
			{
				AppendNumericField(fieldType, digits, digits);
			}

			private void AppendNumericField(DateTimeFieldType fieldType, int minDigits, int maxDigits)
			{
				AppendComponent(new TimeFormatter.NumericDateComponent(fieldType, minDigits, maxDigits), true);
			}

			private void AppendTextField(DateTimeFieldType fieldType, bool isShort)
			{
				AppendComponent(new TimeFormatter.TextDateComponent(fieldType, locale, isShort), true);
			}

			private void AppendComponent(TimeFormatter.FormatComponent fc, bool hasGroup)
			{
				if (hasGroup)
				{
					fc.group = ++curGroup;
				}
				pieces.Add(fc);
			}

			private void AppendLiteralField(string s)
			{
				AppendComponent(new TimeFormatter.LiteralComponent(s), false);
			}

			private void AppendRegexPart(string s)
			{
				AppendComponent(new TimeFormatter.RegexComponent(s), false);
			}

			protected internal virtual void AppendEraText()
			{
				builder.AppendEraText();
				AppendTextField(DateTimeFieldType.Era(), false);
			}

			protected internal virtual void AppendCenturyOfEra(int minDigits, int maxDigits)
			{
				builder.AppendCenturyOfEra(minDigits, maxDigits);
				AppendNumericField(DateTimeFieldType.CenturyOfEra(), minDigits, maxDigits);
			}

			protected internal virtual void AppendYearOfEra(int minDigits, int maxDigits)
			{
				builder.AppendYearOfEra(minDigits, maxDigits);
				AppendNumericField(DateTimeFieldType.YearOfEra(), minDigits, maxDigits);
			}

			protected internal virtual void AppendYear(int minDigits, int maxDigits)
			{
				builder.AppendYear(minDigits, maxDigits);
				AppendNumericField(DateTimeFieldType.Year(), minDigits, maxDigits);
			}

			protected internal virtual void AppendTwoDigitYear(int pivot, bool lenient)
			{
				builder.AppendTwoDigitYear(pivot, lenient);
				AppendNumericField(DateTimeFieldType.YearOfCentury(), 2);
			}

			protected internal virtual void AppendWeekyear(int minDigits, int maxDigits)
			{
				builder.AppendWeekyear(minDigits, maxDigits);
				AppendNumericField(DateTimeFieldType.Weekyear(), minDigits, maxDigits);
			}

			protected internal virtual void AppendTwoDigitWeekyear(int pivot, bool lenient)
			{
				builder.AppendTwoDigitYear(pivot, lenient);
				AppendNumericField(DateTimeFieldType.YearOfCentury(), 2);
			}

			protected internal virtual void AppendWeekOfWeekyear(int digits)
			{
				builder.AppendWeekOfWeekyear(digits);
				AppendNumericField(DateTimeFieldType.WeekOfWeekyear(), digits);
			}

			protected internal virtual void AppendMonthOfYear(int digits)
			{
				builder.AppendMonthOfYear(digits);
				AppendNumericField(DateTimeFieldType.MonthOfYear(), digits);
			}

			protected internal virtual void AppendMonthOfYearShortText()
			{
				builder.AppendMonthOfYearShortText();
				AppendTextField(DateTimeFieldType.MonthOfYear(), true);
			}

			protected internal virtual void AppendMonthOfYearText()
			{
				builder.AppendMonthOfYearText();
				AppendTextField(DateTimeFieldType.MonthOfYear(), false);
			}

			protected internal virtual void AppendDayOfYear(int digits)
			{
				builder.AppendDayOfYear(digits);
				AppendNumericField(DateTimeFieldType.DayOfYear(), digits);
			}

			protected internal virtual void AppendDayOfMonth(int digits)
			{
				builder.AppendDayOfMonth(digits);
				AppendNumericField(DateTimeFieldType.DayOfMonth(), digits);
			}

			protected internal virtual void AppendDayOfWeek(int digits)
			{
				builder.AppendDayOfWeek(digits);
				AppendNumericField(DateTimeFieldType.DayOfWeek(), digits);
			}

			protected internal virtual void AppendDayOfWeekText()
			{
				builder.AppendDayOfWeekText();
				AppendTextField(DateTimeFieldType.DayOfWeek(), false);
			}

			protected internal virtual void AppendDayOfWeekShortText()
			{
				builder.AppendDayOfWeekShortText();
				AppendTextField(DateTimeFieldType.DayOfWeek(), true);
			}

			protected internal virtual void AppendHalfdayOfDayText()
			{
				builder.AppendHalfdayOfDayText();
				AppendTextField(DateTimeFieldType.HalfdayOfDay(), false);
			}

			protected internal virtual void AppendClockhourOfDay(int digits)
			{
				builder.AppendDayOfYear(digits);
				AppendNumericField(DateTimeFieldType.ClockhourOfDay(), digits);
			}

			protected internal virtual void AppendClockhourOfHalfday(int digits)
			{
				builder.AppendClockhourOfHalfday(digits);
				AppendNumericField(DateTimeFieldType.ClockhourOfHalfday(), digits);
			}

			protected internal virtual void AppendHourOfDay(int digits)
			{
				if (useRelaxedHour)
				{
					builder.AppendHourOfDay(digits);
					AppendNumericFields(new DateTimeFieldType[] { DateTimeFieldType.HourOfDay(), DateTimeFieldType.ClockhourOfDay() }, digits);
				}
				else
				{
					builder.AppendHourOfDay(digits);
					AppendNumericField(DateTimeFieldType.HourOfDay(), digits);
				}
			}

			protected internal virtual void AppendHourOfHalfday(int digits)
			{
				builder.AppendHourOfHalfday(digits);
				AppendNumericField(DateTimeFieldType.HourOfHalfday(), digits);
			}

			protected internal virtual void AppendMinuteOfHour(int digits)
			{
				builder.AppendMinuteOfHour(digits);
				AppendNumericField(DateTimeFieldType.MinuteOfHour(), digits);
			}

			protected internal virtual void AppendSecondOfMinute(int digits)
			{
				builder.AppendSecondOfMinute(digits);
				AppendNumericField(DateTimeFieldType.SecondOfMinute(), digits);
			}

			protected internal virtual void AppendFractionOfSecond(int minDigits, int maxDigits)
			{
				builder.AppendFractionOfSecond(minDigits, maxDigits);
				AppendNumericField(DateTimeFieldType.MillisOfSecond(), minDigits, maxDigits);
			}

			protected internal virtual void AppendTimeZoneOffset(string zeroOffsetText, string zeroOffsetParseText, bool showSeparators, int minFields, int maxFields)
			{
				builder.AppendTimeZoneOffset(zeroOffsetText, zeroOffsetParseText, showSeparators, minFields, maxFields);
				AppendComponent(new TimeFormatter.TimeZoneOffsetComponent(zeroOffsetParseText), true);
			}

			protected internal virtual void AppendTimeZoneId()
			{
				builder.AppendTimeZoneId();
				AppendComponent(new TimeFormatter.TimeZoneIdComponent(), true);
			}

			protected internal virtual void AppendTimeZoneName()
			{
				builder.AppendTimeZoneName();
				// TODO: TimeZoneName
				AppendComponent(new TimeFormatter.TimeZoneComponent(locale), true);
			}

			protected internal virtual void AppendTimeZoneShortName()
			{
				builder.AppendTimeZoneShortName();
				// TODO: TimeZoneName
				AppendComponent(new TimeFormatter.TimeZoneComponent(locale), true);
			}

			protected internal virtual void AppendQuantifier(string str)
			{
				if (pieces.Count > 0)
				{
					TimeFormatter.FormatComponent last = pieces[pieces.Count - 1];
					last.AppendQuantifier(str);
				}
				else
				{
					throw new ArgumentException("Illegal quantifier at beginning of pattern: " + str);
				}
			}

			protected internal virtual void AppendGroupStart()
			{
				AppendRegexPart("(?:");
			}

			protected internal virtual void AppendGroupEnd()
			{
				AppendRegexPart(")");
			}

			protected internal virtual void AppendLiteral(char c)
			{
				builder.AppendLiteral(c);
				AppendLiteralField(c.ToString());
			}

			protected internal virtual void AppendLiteral(string s)
			{
				builder.AppendLiteral(s);
				AppendLiteralField(s);
			}
		}

		private static void ParsePatternTo(TimeFormatter.FormatterBuilder builder, string pattern)
		{
			int length = pattern.Length;
			int[] indexRef = new int[1];
			for (int i = 0; i < length; i++)
			{
				indexRef[0] = i;
				string token = ParseToken(pattern, indexRef);
				i = indexRef[0];
				int tokenLen = token.Length;
				if (tokenLen == 0)
				{
					break;
				}
				char c = token[0];
				switch (c)
				{
					case 'G':
					{
						// era designator (text)
						builder.AppendEraText();
						break;
					}

					case 'C':
					{
						// century of era (number)
						builder.AppendCenturyOfEra(tokenLen, tokenLen);
						break;
					}

					case 'x':
					case 'y':
					case 'Y':
					{
						// weekyear (number)
						// year (number)
						// year of era (number)
						if (tokenLen == 2)
						{
							bool lenientParse = true;
							// Peek ahead to next token.
							if (i + 1 < length)
							{
								indexRef[0]++;
								if (IsNumericToken(ParseToken(pattern, indexRef)))
								{
									// If next token is a number, cannot support
									// lenient parse, because it will consume digits
									// that it should not.
									lenientParse = false;
								}
								indexRef[0]--;
							}
							switch (c)
							{
								case 'x':
								{
									// TODO: fixed pivots doesn't make sense, we want pivots that can change....
									// Use pivots which are compatible with SimpleDateFormat.
									builder.AppendTwoDigitWeekyear(new DateTime().GetWeekyear() - 30, lenientParse);
									break;
								}

								case 'y':
								case 'Y':
								default:
								{
									builder.AppendTwoDigitYear(new DateTime().GetYear() - 30, lenientParse);
									break;
								}
							}
						}
						else
						{
							/* // Try to support long year values.
							int maxDigits = 9;
							
							// Peek ahead to next token.
							if (i + 1 < length) {
							indexRef[0]++;
							if (isNumericToken(parseToken(pattern, indexRef))) {
							// If next token is a number, cannot support long years.
							maxDigits = tokenLen;
							}
							indexRef[0]--;
							} */
							int maxDigits = 4;
							switch (c)
							{
								case 'x':
								{
									builder.AppendWeekyear(tokenLen, maxDigits);
									break;
								}

								case 'y':
								{
									builder.AppendYear(tokenLen, maxDigits);
									break;
								}

								case 'Y':
								{
									builder.AppendYearOfEra(tokenLen, maxDigits);
									break;
								}
							}
						}
						break;
					}

					case 'M':
					{
						// month of year (text and number)
						if (tokenLen >= 3)
						{
							if (tokenLen >= 4)
							{
								builder.AppendMonthOfYearText();
							}
							else
							{
								builder.AppendMonthOfYearShortText();
							}
						}
						else
						{
							builder.AppendMonthOfYear(tokenLen);
						}
						break;
					}

					case 'd':
					{
						// day of month (number)
						builder.AppendDayOfMonth(tokenLen);
						break;
					}

					case 'a':
					{
						// am/pm marker (text)
						builder.AppendHalfdayOfDayText();
						break;
					}

					case 'h':
					{
						// clockhour of halfday (number, 1..12)
						builder.AppendClockhourOfHalfday(tokenLen);
						break;
					}

					case 'H':
					{
						// hour of day (number, 0..23)
						builder.AppendHourOfDay(tokenLen);
						break;
					}

					case 'k':
					{
						// clockhour of day (1..24)
						builder.AppendClockhourOfDay(tokenLen);
						break;
					}

					case 'K':
					{
						// hour of halfday (0..11)
						builder.AppendHourOfHalfday(tokenLen);
						break;
					}

					case 'm':
					{
						// minute of hour (number)
						builder.AppendMinuteOfHour(tokenLen);
						break;
					}

					case 's':
					{
						// second of minute (number)
						builder.AppendSecondOfMinute(tokenLen);
						break;
					}

					case 'S':
					{
						// fraction of second (number)
						builder.AppendFractionOfSecond(tokenLen, tokenLen);
						break;
					}

					case 'e':
					{
						// day of week (number)
						builder.AppendDayOfWeek(tokenLen);
						break;
					}

					case 'E':
					{
						// dayOfWeek (text)
						if (tokenLen >= 4)
						{
							builder.AppendDayOfWeekText();
						}
						else
						{
							builder.AppendDayOfWeekShortText();
						}
						break;
					}

					case 'D':
					{
						// day of year (number)
						builder.AppendDayOfYear(tokenLen);
						break;
					}

					case 'w':
					{
						// week of weekyear (number)
						builder.AppendWeekOfWeekyear(tokenLen);
						break;
					}

					case 'z':
					{
						// time zone (text)
						if (tokenLen >= 4)
						{
							builder.AppendTimeZoneName();
						}
						else
						{
							builder.AppendTimeZoneShortName();
						}
						break;
					}

					case 'Z':
					{
						// time zone offset
						if (tokenLen == 1)
						{
							builder.AppendTimeZoneOffset(null, "Z", false, 2, 2);
						}
						else
						{
							if (tokenLen == 2)
							{
								builder.AppendTimeZoneOffset(null, "Z", true, 2, 2);
							}
							else
							{
								builder.AppendTimeZoneId();
							}
						}
						break;
					}

					case '(':
					{
						builder.AppendGroupStart();
						break;
					}

					case ')':
					{
						builder.AppendGroupEnd();
						break;
					}

					case '{':
					case '*':
					case '?':
					{
						builder.AppendQuantifier(token);
						break;
					}

					case '[':
					case '.':
					case '|':
					case '\\':
					{
						builder.AppendRegexPart(token);
						break;
					}

					case '\'':
					{
						// literal text
						string sub = Sharpen.Runtime.Substring(token, 1);
						if (sub.Length == 1)
						{
							builder.AppendLiteral(sub[0]);
						}
						else
						{
							// Create copy of sub since otherwise the temporary quoted
							// string would still be referenced internally.
							builder.AppendLiteral(new string(sub));
						}
						break;
					}

					default:
					{
						throw new ArgumentException("Illegal pattern component: " + token);
					}
				}
			}
		}

		private static readonly char[] SpecialRegexChars = new char[] { '[', ']', '(', ')', '{', '}', '?', '*', '.', '|', '\\' };

		private static bool IsSpecialRegexChar(char c)
		{
			foreach (char SpecialRegexChar in SpecialRegexChars)
			{
				if (c == SpecialRegexChar)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Parses an individual token.</summary>
		/// <param name="pattern">the pattern string</param>
		/// <param name="indexRef">
		/// a single element array, where the input is the start
		/// location and the output is the location after parsing the token
		/// </param>
		/// <returns>the parsed token</returns>
		private static string ParseToken(string pattern, int[] indexRef)
		{
			StringBuilder buf = new StringBuilder();
			int i = indexRef[0];
			int length = pattern.Length;
			char c = pattern[i];
			if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z')
			{
				// Scan a run of the same character, which indicates a time
				// pattern.
				buf.Append(c);
				while (i + 1 < length)
				{
					char peek = pattern[i + 1];
					if (peek == c)
					{
						buf.Append(c);
						i++;
					}
					else
					{
						break;
					}
				}
			}
			else
			{
				if (IsSpecialRegexChar(c))
				{
					buf.Append(c);
					if (c == '[')
					{
						// Look for end ']'
						// Assume no nesting
						i++;
						for (; i < length; i++)
						{
							c = pattern[i];
							buf.Append(c);
							if (c == ']')
							{
								break;
							}
						}
					}
					else
					{
						if (c == '{')
						{
							// Look for end '}'
							// Assume no nesting
							i++;
							for (; i < length; i++)
							{
								c = pattern[i];
								buf.Append(c);
								if (c == '}')
								{
									break;
								}
							}
						}
						else
						{
							if (c == '\\')
							{
								// Used to escape characters
								i++;
								if (i < length)
								{
									c = pattern[i];
									buf.Append(c);
								}
							}
						}
					}
				}
				else
				{
					// This will identify token as text.
					buf.Append('\'');
					bool inLiteral = false;
					for (; i < length; i++)
					{
						c = pattern[i];
						if (c == '\'')
						{
							if (i + 1 < length && pattern[i + 1] == '\'')
							{
								// '' is treated as escaped '
								i++;
								buf.Append(c);
							}
							else
							{
								inLiteral = !inLiteral;
							}
						}
						else
						{
							if (!inLiteral && (IsSpecialRegexChar(c) || (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z')))
							{
								i--;
								break;
							}
							else
							{
								buf.Append(c);
							}
						}
					}
				}
			}
			indexRef[0] = i;
			return buf.ToString();
		}

		/// <summary>Returns true if token should be parsed as a numeric field.</summary>
		/// <param name="token">the token to parse</param>
		/// <returns>true if numeric field</returns>
		private static bool IsNumericToken(string token)
		{
			int tokenLen = token.Length;
			if (tokenLen > 0)
			{
				char c = token[0];
				switch (c)
				{
					case 'c':
					case 'C':
					case 'x':
					case 'y':
					case 'Y':
					case 'd':
					case 'h':
					case 'H':
					case 'm':
					case 's':
					case 'S':
					case 'e':
					case 'D':
					case 'F':
					case 'w':
					case 'W':
					case 'k':
					case 'K':
					{
						// century (number)
						// century of era (number)
						// weekyear (number)
						// year (number)
						// year of era (number)
						// day of month (number)
						// hour of day (number, 1..12)
						// hour of day (number, 0..23)
						// minute of hour (number)
						// second of minute (number)
						// fraction of second (number)
						// day of week (number)
						// day of year (number)
						// day of week in month (number)
						// week of year (number)
						// week of month (number)
						// hour of day (1..24)
						// hour of day (0..11)
						return true;
					}

					case 'M':
					{
						// month of year (text and number)
						if (tokenLen <= 2)
						{
							return true;
						}
						break;
					}
				}
			}
			return false;
		}
	}
}
