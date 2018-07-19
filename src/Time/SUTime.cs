using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling.Tokensregex.Types;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Time;
using Java.Time.Format;
using Java.Time.Temporal;
using Java.Util;
using Java.Util.Regex;
using Org.Joda.Time;
using Org.Joda.Time.Format;
using Sharpen;

namespace Edu.Stanford.Nlp.Time
{
	/// <summary>
	/// SUTime is a collection of data structures to represent various temporal
	/// concepts and operations between them.
	/// </summary>
	/// <remarks>
	/// SUTime is a collection of data structures to represent various temporal
	/// concepts and operations between them.
	/// Different types of time expressions:
	/// <ul>
	/// <li>Time - A time point on a time scale  In most cases, we only know partial information
	/// (with a certain granularity) about a point in time (8:00pm)</li>
	/// <li>Duration - A length of time (3 days) </li>
	/// <li>Interval - A range of time with start and end points</li>
	/// <li>Set - A set of time: Can be periodic (Friday every week) or union (Thursday or Friday)</li>
	/// </ul>
	/// <p>
	/// Use
	/// <see cref="TimeAnnotator"/>
	/// to annotate documents within an Annotation pipeline such as CoreNLP.
	/// Use
	/// <see cref="SUTimeMain"/>
	/// for standalone testing.
	/// </remarks>
	/// <author>Angel Chang</author>
	public class SUTime
	{
		/// <summary>A logger for this class</summary>
		private SUTime()
		{
		}

		public enum TimexType
		{
			Date,
			Time,
			Duration,
			Set
		}

		[System.Serializable]
		public sealed class TimexMod
		{
			public static readonly SUTime.TimexMod Before = new SUTime.TimexMod("<");

			public static readonly SUTime.TimexMod After = new SUTime.TimexMod(">");

			public static readonly SUTime.TimexMod OnOrBefore = new SUTime.TimexMod("<=");

			public static readonly SUTime.TimexMod OnOrAfter = new SUTime.TimexMod("<=");

			public static readonly SUTime.TimexMod LessThan = new SUTime.TimexMod("<");

			public static readonly SUTime.TimexMod MoreThan = new SUTime.TimexMod(">");

			public static readonly SUTime.TimexMod EqualOrLess = new SUTime.TimexMod("<=");

			public static readonly SUTime.TimexMod EqualOrMore = new SUTime.TimexMod(">=");

			public static readonly SUTime.TimexMod Start = new SUTime.TimexMod();

			public static readonly SUTime.TimexMod Mid = new SUTime.TimexMod();

			public static readonly SUTime.TimexMod End = new SUTime.TimexMod();

			public static readonly SUTime.TimexMod Approx = new SUTime.TimexMod("~");

			public static readonly SUTime.TimexMod Early = new SUTime.TimexMod();

			public static readonly SUTime.TimexMod Late = new SUTime.TimexMod();

			private string symbol;

			internal TimexMod()
			{
			}

			internal TimexMod(string symbol)
			{
				// import edu.stanford.nlp.util.logging.Redwood;
				// private static Redwood.RedwoodChannels log = Redwood.channels(SUTime.class);
				// TODO:
				// 1. Decrease dependency on JodaTime...
				// 2. Number parsing
				// - Improve Number detection/normalization
				// - Handle four-years, one thousand two hundred and sixty years
				// - Currently custom word to number combo - integrate with Number classifier,
				// QuantifiableEntityNormalizer
				// - Stop repeated conversions of word to numbers
				// 3. Durations
				// - Underspecified durations
				// 4. Date Time
				// - Patterns
				// -- 1st/last week(end) of blah blah
				// -- Don't treat all 3 to 5 as times
				// - Holidays
				// - Too many classes - reduce number of classes
				// 5. Nest time expressions
				// - Before annotating: Can remove nested time expressions
				// - After annotating: types to combine time expressions
				// 6. Set of times (Timex3 standard is weird, timex2 makes more sense?)
				// - freq, quant
				// 7. Ground with respect to reference time - figure out what is reference
				// time to use for what
				// - news... things happen in the past, so favor resolving to past?
				// - Use heuristics from GUTime to figure out direction to resolve to
				// - tids for anchor times...., valueFromFunctions for resolved relative times
				// (option to keep some nested times)?
				// 8. Composite time patterns
				// - Composite time operators
				// 9. Ranges
				// - comparing times (before, after, ...
				// - intersect, mid, resolving
				// - specify clear start/end for range (sonal)
				// 10. Clean up formatting
				// 11. ISO/Timex3/Custom
				// 12. Keep modifiers
				// 13. Handle mid- (token not separated)
				// 14. future, plurals
				// 15. Resolve to future.... with year specified....
				// 16. Check recursive calls
				// 17. Add TimeWithFields (that doesn't use jodatime and is only field based?
				/* GUTIME */
				/* GUTIME */
				this.symbol = symbol;
			}

			public string GetSymbol()
			{
				return SUTime.TimexMod.symbol;
			}
		}

		public enum TimexDocFunc
		{
			CreationTime,
			ExpirationTime,
			ModificationTime,
			PublicationTime,
			ReleaseTime,
			ReceptionTime,
			None
		}

		public enum TimexAttr
		{
			type,
			value,
			tid,
			beginPoint,
			endPoint,
			quant,
			freq,
			mod,
			anchorTimeID,
			comment,
			valueFromFunction,
			temporalFunction,
			functionInDocument
		}

		public const string PadFieldUnknown = "X";

		public const string PadFieldUnknown2 = "XX";

		public const string PadFieldUnknown4 = "XXXX";

		public const int ResolveNow = unchecked((int)(0x01));

		public const int ResolveToThis = unchecked((int)(0x20));

		public const int ResolveToPast = unchecked((int)(0x40));

		public const int ResolveToFuture = unchecked((int)(0x80));

		public const int ResolveToClosest = unchecked((int)(0x200));

		public const int DurResolveToAsRef = unchecked((int)(0x1000));

		public const int DurResolveFromAsRef = unchecked((int)(0x2000));

		public const int RangeResolveTimeRef = unchecked((int)(0x100000));

		public const int RelativeOffsetInexact = unchecked((int)(0x0100));

		public const int RangeOffsetBegin = unchecked((int)(0x0001));

		public const int RangeOffsetEnd = unchecked((int)(0x0002));

		public const int RangeExpandFixBegin = unchecked((int)(0x0010));

		public const int RangeExpandFixEnd = unchecked((int)(0x0020));

		/// <summary>Flags for how to pad when converting times into ranges</summary>
		public const int RangeFlagsPadMask = unchecked((int)(0x000f));

		/// <summary>Simple range (without padding)</summary>
		public const int RangeFlagsPadNone = unchecked((int)(0x0001));

		/// <summary>Automatic range (whatever padding we think is most appropriate, default)</summary>
		public const int RangeFlagsPadAuto = unchecked((int)(0x0002));

		/// <summary>Pad to most specific (whatever that is)</summary>
		public const int RangeFlagsPadFinest = unchecked((int)(0x0003));

		/// <summary>Pad to specified granularity</summary>
		public const int RangeFlagsPadSpecified = unchecked((int)(0x0004));

		public const int FormatIso = unchecked((int)(0x01));

		public const int FormatTimex3Value = unchecked((int)(0x02));

		public const int FormatFull = unchecked((int)(0x04));

		public const int FormatPadUnknown = unchecked((int)(0x1000));

		protected internal const int timexVersion = 3;

		// Flags for how to resolve a time expression
		// Resolve to a past time
		// Resolve to a future time
		// Resolve to closest time
		// Pad type
		public static SUTime.Time GetCurrentTime()
		{
			return new SUTime.GroundedTime(new DateTime());
		}

		public class TimeIndex
		{
			internal IIndex<TimeExpression> temporalExprIndex = new HashIndex<TimeExpression>();

			internal IIndex<SUTime.Temporal> temporalIndex = new HashIndex<SUTime.Temporal>();

			internal IIndex<SUTime.Temporal> temporalFuncIndex = new HashIndex<SUTime.Temporal>();

			internal SUTime.Time docDate;

			public TimeIndex()
			{
				// Index of time id to temporal object
				AddTemporal(SUTime.TimeRef);
			}

			public virtual void Clear()
			{
				temporalExprIndex.Clear();
				temporalIndex.Clear();
				temporalFuncIndex.Clear();
				// t0 is the document date (reserve)
				temporalExprIndex.Add(null);
				AddTemporal(SUTime.TimeRef);
			}

			public virtual int GetNumberOfTemporals()
			{
				return temporalIndex.Size();
			}

			public virtual int GetNumberOfTemporalExprs()
			{
				return temporalExprIndex.Size();
			}

			public virtual int GetNumberOfTemporalFuncs()
			{
				return temporalFuncIndex.Size();
			}

			private static readonly Pattern IdPattern = Pattern.Compile("([a-zA-Z]*)(\\d+)");

			public virtual TimeExpression GetTemporalExpr(string s)
			{
				Matcher m = IdPattern.Matcher(s);
				if (m.Matches())
				{
					string prefix = m.Group(1);
					int id = System.Convert.ToInt32(m.Group(2));
					if ("t".Equals(prefix) || prefix.IsEmpty())
					{
						return temporalExprIndex.Get(id);
					}
				}
				return null;
			}

			public virtual SUTime.Temporal GetTemporal(string s)
			{
				Matcher m = IdPattern.Matcher(s);
				if (m.Matches())
				{
					string prefix = m.Group(1);
					int id = System.Convert.ToInt32(m.Group(2));
					if ("t".Equals(prefix))
					{
						TimeExpression te = temporalExprIndex.Get(id);
						return (te != null) ? te.GetTemporal() : null;
					}
					else
					{
						if (prefix.IsEmpty())
						{
							return temporalIndex.Get(id);
						}
					}
				}
				return null;
			}

			public virtual TimeExpression GetTemporalExpr(int i)
			{
				return temporalExprIndex.Get(i);
			}

			public virtual SUTime.Temporal GetTemporal(int i)
			{
				return temporalIndex.Get(i);
			}

			public virtual SUTime.Temporal GetTemporalFunc(int i)
			{
				return temporalFuncIndex.Get(i);
			}

			public virtual bool AddTemporalExpr(TimeExpression t)
			{
				SUTime.Temporal temp = t.GetTemporal();
				if (temp != null)
				{
					AddTemporal(temp);
				}
				return temporalExprIndex.Add(t);
			}

			public virtual bool AddTemporal(SUTime.Temporal t)
			{
				return temporalIndex.Add(t);
			}

			public virtual bool AddTemporalFunc(SUTime.Temporal t)
			{
				return temporalFuncIndex.Add(t);
			}

			public virtual int AddToIndexTemporalExpr(TimeExpression t)
			{
				return temporalExprIndex.AddToIndex(t);
			}

			public virtual int AddToIndexTemporal(SUTime.Temporal t)
			{
				return temporalIndex.AddToIndex(t);
			}

			public virtual int AddToIndexTemporalFunc(SUTime.Temporal t)
			{
				return temporalFuncIndex.AddToIndex(t);
			}
		}

		/// <summary>Basic temporal object.</summary>
		/// <remarks>
		/// Basic temporal object.
		/// <p>
		/// There are 4 main types of temporal objects
		/// <ol>
		/// <li>Time - Conceptually a point in time
		/// <br />NOTE: Due to limitation in precision, it is
		/// difficult to get an exact point in time
		/// </li>
		/// <li>Duration - Amount of time in a time interval
		/// <ul><li>DurationWithMillis - Duration specified in milliseconds
		/// (wrapper around JodaTime Duration)</li>
		/// <li>DurationWithFields - Duration specified with
		/// fields like day, year, etc (wrapper around JodaTime Period)</lI>
		/// <li>DurationRange - A duration that falls in a particular range (with min to max)</li>
		/// </ul>
		/// </li>
		/// <li>Range - Time Interval with a start time, end time, and duration</li>
		/// <li>TemporalSet - A set of temporal objects
		/// <ul><li>ExplicitTemporalSet - Explicit set of temporals (not used)
		/// <br />Ex: Tuesday 1-2pm, Wednesday night</li>
		/// <li>PeriodicTemporalSet - Reoccurring times
		/// <br />Ex: Every Tuesday</li>
		/// </ul>
		/// </li>
		/// </ol>
		/// </remarks>
		[System.Serializable]
		public abstract class Temporal : ICloneable
		{
			public string mod;

			public bool approx;

			internal SUTime.StandardTemporalType standardTemporalType;

			public string timeLabel;

			public SUTime.Duration uncertaintyGranularity;

			public Temporal()
			{
			}

			public Temporal(SUTime.Temporal t)
			{
				// Duration after which the time is uncertain (what is there is an estimate)
				this.mod = t.mod;
				this.approx = t.approx;
				this.uncertaintyGranularity = t.uncertaintyGranularity;
			}

			//      this.standardTimeType = t.standardTimeType;
			//      this.timeLabel = t.timeLabel;
			public abstract bool IsGrounded();

			// Returns time representation for Temporal (if available)
			public abstract SUTime.Time GetTime();

			// Returns duration (estimate of how long the temporal expression is for)
			public abstract SUTime.Duration GetDuration();

			// Returns range (start/end points of temporal, automatic granularity)
			public virtual SUTime.Range GetRange()
			{
				return GetRange(RangeFlagsPadAuto);
			}

			// Returns range (start/end points of temporal)
			public virtual SUTime.Range GetRange(int flags)
			{
				return GetRange(flags, null);
			}

			// Returns range (start/end points of temporal), using specified flags
			public abstract SUTime.Range GetRange(int flags, SUTime.Duration granularity);

			// Returns how often this time would repeat
			// Ex: friday repeat weekly, hour repeat hourly, hour in a day repeat daily
			public virtual SUTime.Duration GetPeriod()
			{
				/*    TimeLabel tl = getTimeLabel();
				if (tl != null) {
				return tl.getPeriod();
				} */
				SUTime.StandardTemporalType tlt = GetStandardTemporalType();
				if (tlt != null)
				{
					return tlt.GetPeriod();
				}
				return null;
			}

			// Returns the granularity to which this time or duration is specified
			// Typically the most specific time unit
			public virtual SUTime.Duration GetGranularity()
			{
				SUTime.StandardTemporalType tlt = GetStandardTemporalType();
				if (tlt != null)
				{
					return tlt.GetGranularity();
				}
				return null;
			}

			public virtual SUTime.Duration GetUncertaintyGranularity()
			{
				if (uncertaintyGranularity != null)
				{
					return uncertaintyGranularity;
				}
				return GetGranularity();
			}

			// Resolves this temporal expression with respect to the specified reference
			// time using flags
			public virtual SUTime.Temporal Resolve(SUTime.Time refTime)
			{
				return Resolve(refTime, 0);
			}

			public abstract SUTime.Temporal Resolve(SUTime.Time refTime, int flags);

			public virtual SUTime.StandardTemporalType GetStandardTemporalType()
			{
				return standardTemporalType;
			}

			// Returns if the current temporal expression is an reference
			public virtual bool IsRef()
			{
				return false;
			}

			// Return sif the current temporal expression is approximate
			public virtual bool IsApprox()
			{
				return approx;
			}

			// TIMEX related functions
			public virtual int GetTid(SUTime.TimeIndex timeIndex)
			{
				return timeIndex.AddToIndexTemporal(this);
			}

			public virtual string GetTidString(SUTime.TimeIndex timeIndex)
			{
				return "t" + GetTid(timeIndex);
			}

			public virtual int GetTfid(SUTime.TimeIndex timeIndex)
			{
				return timeIndex.AddToIndexTemporalFunc(this);
			}

			public virtual string GetTfidString(SUTime.TimeIndex timeIndex)
			{
				return "tf" + GetTfid(timeIndex);
			}

			// Returns attributes to convert this temporal expression into timex object
			public virtual bool IncludeTimexAltValue()
			{
				return false;
			}

			public virtual IDictionary<string, string> GetTimexAttributes(SUTime.TimeIndex timeIndex)
			{
				IDictionary<string, string> map = new LinkedHashMap<string, string>();
				map[SUTime.TimexAttr.tid.ToString()] = GetTidString(timeIndex);
				// NOTE: GUTime used "VAL" instead of TIMEX3 standard "value"
				// NOTE: attributes are case sensitive, GUTIME used mostly upper case
				// attributes....
				string val = GetTimexValue();
				if (val != null)
				{
					map[SUTime.TimexAttr.value.ToString()] = val;
				}
				if (val == null || IncludeTimexAltValue())
				{
					string str = ToFormattedString(FormatFull);
					if (str != null)
					{
						map["alt_value"] = str;
					}
				}
				/*     Range r = getRange();
				if (r != null) map.put("range", r.toString());    */
				/*     map.put("str", toString());        */
				map[SUTime.TimexAttr.type.ToString()] = GetTimexType().ToString();
				if (mod != null)
				{
					map[SUTime.TimexAttr.mod.ToString()] = mod;
				}
				return map;
			}

			// Returns the timex type
			public virtual SUTime.TimexType GetTimexType()
			{
				if (GetStandardTemporalType() != null)
				{
					return GetStandardTemporalType().GetTimexType();
				}
				else
				{
					return null;
				}
			}

			// Returns timex value (by default it is the ISO string representation of
			// this object)
			public virtual string GetTimexValue()
			{
				return ToFormattedString(FormatTimex3Value);
			}

			public virtual string ToISOString()
			{
				return ToFormattedString(FormatIso);
			}

			public override string ToString()
			{
				// TODO: Full string representation
				return ToFormattedString(FormatFull);
			}

			public virtual string GetTimeLabel()
			{
				return timeLabel;
			}

			public virtual string ToFormattedString(int flags)
			{
				return GetTimeLabel();
			}

			// Temporal operations...
			public static SUTime.Temporal SetTimeZone(SUTime.Temporal t, DateTimeZone tz)
			{
				if (t == null)
				{
					return null;
				}
				return t.SetTimeZone(tz);
			}

			public virtual SUTime.Temporal SetTimeZone(DateTimeZone tz)
			{
				return this;
			}

			public virtual SUTime.Temporal SetTimeZone(int offsetHours)
			{
				return SetTimeZone(DateTimeZone.ForOffsetHours(offsetHours));
			}

			// public abstract Temporal add(Duration offset);
			public virtual SUTime.Temporal Next()
			{
				SUTime.Duration per = GetPeriod();
				if (per != null)
				{
					if (this is SUTime.Duration)
					{
						return new SUTime.RelativeTime(new SUTime.RelativeTime(SUTime.TemporalOp.This, this, DurResolveToAsRef), SUTime.TemporalOp.Offset, per);
					}
					else
					{
						// return new RelativeTime(new RelativeTime(TemporalOp.THIS, this),
						// TemporalOp.OFFSET, per);
						return SUTime.TemporalOp.Offset.Apply(this, per);
					}
				}
				return null;
			}

			public virtual SUTime.Temporal Prev()
			{
				SUTime.Duration per = GetPeriod();
				if (per != null)
				{
					if (this is SUTime.Duration)
					{
						return new SUTime.RelativeTime(new SUTime.RelativeTime(SUTime.TemporalOp.This, this, DurResolveFromAsRef), SUTime.TemporalOp.Offset, per.MultiplyBy(-1));
					}
					else
					{
						// return new RelativeTime(new RelativeTime(TemporalOp.THIS, this),
						// TemporalOp.OFFSET, per.multiplyBy(-1));
						return SUTime.TemporalOp.Offset.Apply(this, per.MultiplyBy(-1));
					}
				}
				return null;
			}

			public virtual SUTime.Temporal Intersect(SUTime.Temporal t)
			{
				/* abstract*/
				return null;
			}

			public virtual string GetMod()
			{
				return mod;
			}

			/*   public void setMod(String mod) {
			this.mod = mod;
			} */
			public virtual SUTime.Temporal AddMod(string mod)
			{
				SUTime.Temporal t = (SUTime.Temporal)this.MemberwiseClone();
				t.mod = mod;
				return t;
			}

			public virtual SUTime.Temporal AddModApprox(string mod, bool approx)
			{
				SUTime.Temporal t = (SUTime.Temporal)this.MemberwiseClone();
				t.mod = mod;
				t.approx = approx;
				return t;
			}

			private const long serialVersionUID = 1;
		}

		public static T CreateTemporal<T>(SUTime.StandardTemporalType timeType, T temporal)
			where T : SUTime.Temporal
		{
			temporal.standardTemporalType = timeType;
			return temporal;
		}

		public static T CreateTemporal<T>(SUTime.StandardTemporalType timeType, string label, T temporal)
			where T : SUTime.Temporal
		{
			temporal.standardTemporalType = timeType;
			temporal.timeLabel = label;
			return temporal;
		}

		public static T CreateTemporal<T>(SUTime.StandardTemporalType timeType, string label, string mod, T temporal)
			where T : SUTime.Temporal
		{
			temporal.standardTemporalType = timeType;
			temporal.timeLabel = label;
			temporal.mod = mod;
			return temporal;
		}

		private sealed class _DurationWithFields_567 : SUTime.DurationWithFields
		{
			public _DurationWithFields_567(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			// Basic time units (durations)
			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { DateTimeFieldType.Year(), DateTimeFieldType.YearOfCentury(), DateTimeFieldType.YearOfEra() };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Year = new _DurationWithFields_567(Period.Years(1));

		private sealed class _DurationWithFields_575 : SUTime.DurationWithFields
		{
			public _DurationWithFields_575(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { DateTimeFieldType.DayOfMonth(), DateTimeFieldType.DayOfWeek(), DateTimeFieldType.DayOfYear() };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Day = new _DurationWithFields_575(Period.Days(1));

		private sealed class _DurationWithFields_583 : SUTime.DurationWithFields
		{
			public _DurationWithFields_583(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { DateTimeFieldType.WeekOfWeekyear() };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Week = new _DurationWithFields_583(Period.Weeks(1));

		public static readonly SUTime.Duration Fortnight = new SUTime.DurationWithFields(Period.Weeks(2));

		private sealed class _DurationWithFields_593 : SUTime.DurationWithFields
		{
			public _DurationWithFields_593(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { DateTimeFieldType.MonthOfYear() };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Month = new _DurationWithFields_593(Period.Months(1));

		private sealed class _DurationWithFields_603 : SUTime.DurationWithFields
		{
			public _DurationWithFields_603(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			// public static final Duration QUARTER = new DurationWithFields(new
			// Period(JodaTimeUtils.Quarters)) {
			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { JodaTimeUtils.QuarterOfYear };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Quarter = new _DurationWithFields_603(Period.Months(3));

		private sealed class _DurationWithFields_611 : SUTime.DurationWithFields
		{
			public _DurationWithFields_611(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { JodaTimeUtils.HalfYearOfYear };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Halfyear = new _DurationWithFields_611(Period.Months(6));

		private sealed class _DurationWithFields_619 : SUTime.DurationWithFields
		{
			public _DurationWithFields_619(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { DateTimeFieldType.MillisOfSecond(), DateTimeFieldType.MillisOfDay() };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Millis = new _DurationWithFields_619(Period.Millis(1));

		private sealed class _DurationWithFields_627 : SUTime.DurationWithFields
		{
			public _DurationWithFields_627(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { DateTimeFieldType.SecondOfMinute(), DateTimeFieldType.SecondOfDay() };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Second = new _DurationWithFields_627(Period.Seconds(1));

		private sealed class _DurationWithFields_635 : SUTime.DurationWithFields
		{
			public _DurationWithFields_635(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { DateTimeFieldType.MinuteOfHour(), DateTimeFieldType.MinuteOfDay() };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Minute = new _DurationWithFields_635(Period.Minutes(1));

		private sealed class _DurationWithFields_643 : SUTime.DurationWithFields
		{
			public _DurationWithFields_643(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { DateTimeFieldType.HourOfDay(), DateTimeFieldType.HourOfHalfday() };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Hour = new _DurationWithFields_643(Period.Hours(1));

		public static readonly SUTime.Duration Halfhour = new SUTime.DurationWithFields(Period.Minutes(30));

		public static readonly SUTime.Duration Quarterhour = new SUTime.DurationWithFields(Period.Minutes(15));

		private sealed class _DurationWithFields_655 : SUTime.DurationWithFields
		{
			public _DurationWithFields_655(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { JodaTimeUtils.DecadeOfCentury };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Decade = new _DurationWithFields_655(Period.Years(10));

		private sealed class _DurationWithFields_663 : SUTime.DurationWithFields
		{
			public _DurationWithFields_663(IReadablePeriod baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			public override DateTimeFieldType[] GetDateTimeFields()
			{
				return new DateTimeFieldType[] { DateTimeFieldType.CenturyOfEra() };
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Duration Century = new _DurationWithFields_663(Period.Years(100));

		public static readonly SUTime.Duration Millennium = new SUTime.DurationWithFields(Period.Years(1000));

		private sealed class _RefTime_673 : SUTime.RefTime
		{
			public _RefTime_673(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1;
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Time TimeRef = new _RefTime_673("REF");

		public static readonly SUTime.Time TimeRefUnknown = new SUTime.RefTime("UNKNOWN");

		public static readonly SUTime.Time TimeUnknown = new SUTime.SimpleTime("UNKNOWN");

		public static readonly SUTime.Time TimeNone = null;

		public static readonly SUTime.Time TimeNoneOk = new SUTime.SimpleTime("NOTIME");

		public static readonly SUTime.Time TimeNow = new SUTime.RefTime(SUTime.StandardTemporalType.Reftime, "PRESENT_REF", "NOW");

		public static readonly SUTime.Time TimePresent = CreateTemporal(SUTime.StandardTemporalType.Refdate, "PRESENT_REF", new SUTime.InexactTime(new SUTime.Range(TimeNow, TimeNow)));

		public static readonly SUTime.Time TimePast = CreateTemporal(SUTime.StandardTemporalType.Refdate, "PAST_REF", new SUTime.InexactTime(new SUTime.Range(TimeUnknown, TimeNow)));

		public static readonly SUTime.Time TimeFuture = CreateTemporal(SUTime.StandardTemporalType.Refdate, "FUTURE_REF", new SUTime.InexactTime(new SUTime.Range(TimeNow, TimeUnknown)));

		public static readonly SUTime.Duration DurationUnknown = new SUTime.DurationWithFields();

		public static readonly SUTime.Duration DurationNone = new SUTime.DurationWithFields(Period.Zero);

		public static readonly SUTime.Time Monday = new SUTime.PartialTime(SUTime.StandardTemporalType.DayOfWeek, new Partial(DateTimeFieldType.DayOfWeek(), 1));

		public static readonly SUTime.Time Tuesday = new SUTime.PartialTime(SUTime.StandardTemporalType.DayOfWeek, new Partial(DateTimeFieldType.DayOfWeek(), 2));

		public static readonly SUTime.Time Wednesday = new SUTime.PartialTime(SUTime.StandardTemporalType.DayOfWeek, new Partial(DateTimeFieldType.DayOfWeek(), 3));

		public static readonly SUTime.Time Thursday = new SUTime.PartialTime(SUTime.StandardTemporalType.DayOfWeek, new Partial(DateTimeFieldType.DayOfWeek(), 4));

		public static readonly SUTime.Time Friday = new SUTime.PartialTime(SUTime.StandardTemporalType.DayOfWeek, new Partial(DateTimeFieldType.DayOfWeek(), 5));

		public static readonly SUTime.Time Saturday = new SUTime.PartialTime(SUTime.StandardTemporalType.DayOfWeek, new Partial(DateTimeFieldType.DayOfWeek(), 6));

		public static readonly SUTime.Time Sunday = new SUTime.PartialTime(SUTime.StandardTemporalType.DayOfWeek, new Partial(DateTimeFieldType.DayOfWeek(), 7));

		private sealed class _InexactTime_706 : SUTime.InexactTime
		{
			public _InexactTime_706(SUTime.Time baseArg1, SUTime.Duration baseArg2, SUTime.Range baseArg3)
				: base(baseArg1, baseArg2, baseArg3)
			{
				this.serialVersionUID = 1;
			}

			// No time
			// The special time of now
			// Basic dates/times
			// Day of week
			// Use constructors rather than calls to
			// StandardTemporalType.createTemporal because sometimes the class
			// loader seems to load objects in an incorrect order, resulting in
			// an exception.  This is especially evident when deserializing
			public override SUTime.Duration GetDuration()
			{
				return SUTime.Day;
			}

			private const long serialVersionUID;
		}

		public static readonly SUTime.Time Weekday = CreateTemporal(SUTime.StandardTemporalType.DaysOfWeek, "WD", new _InexactTime_706(null, SUTime.Day, new SUTime.Range(SUTime.Monday, SUTime.Friday)));

		public static readonly SUTime.Time Weekend = CreateTemporal(SUTime.StandardTemporalType.DaysOfWeek, "WE", new SUTime.TimeWithRange(new SUTime.Range(SUTime.Saturday, SUTime.Sunday, SUTime.Day.MultiplyBy(2))));

		public static readonly SUTime.Time January = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 1, -1);

		public static readonly SUTime.Time February = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 2, -1);

		public static readonly SUTime.Time March = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 3, -1);

		public static readonly SUTime.Time April = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 4, -1);

		public static readonly SUTime.Time May = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 5, -1);

		public static readonly SUTime.Time June = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 6, -1);

		public static readonly SUTime.Time July = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 7, -1);

		public static readonly SUTime.Time August = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 8, -1);

		public static readonly SUTime.Time September = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 9, -1);

		public static readonly SUTime.Time October = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 10, -1);

		public static readonly SUTime.Time November = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 11, -1);

		public static readonly SUTime.Time December = new SUTime.IsoDate(SUTime.StandardTemporalType.MonthOfYear, -1, 12, -1);

		public static readonly SUTime.Time SpringEquinox = CreateTemporal(SUTime.StandardTemporalType.DayOfYear, "SP", new SUTime.InexactTime(new SUTime.Range(new SUTime.IsoDate(-1, 3, 20), new SUTime.IsoDate(-1, 3, 21))));

		public static readonly SUTime.Time SummerSolstice = CreateTemporal(SUTime.StandardTemporalType.DayOfYear, "SU", new SUTime.InexactTime(new SUTime.Range(new SUTime.IsoDate(-1, 6, 20), new SUTime.IsoDate(-1, 6, 21))));

		public static readonly SUTime.Time WinterSolstice = CreateTemporal(SUTime.StandardTemporalType.DayOfYear, "WI", new SUTime.InexactTime(new SUTime.Range(new SUTime.IsoDate(-1, 12, 21), new SUTime.IsoDate(-1, 12, 22))));

		public static readonly SUTime.Time FallEquinox = CreateTemporal(SUTime.StandardTemporalType.DayOfYear, "FA", new SUTime.InexactTime(new SUTime.Range(new SUTime.IsoDate(-1, 9, 22), new SUTime.IsoDate(-1, 9, 23))));

		public static readonly SUTime.Time Spring = CreateTemporal(SUTime.StandardTemporalType.SeasonOfYear, "SP", new SUTime.InexactTime(SpringEquinox, Quarter, new SUTime.Range(SUTime.March, SUTime.June, SUTime.Quarter)));

		public static readonly SUTime.Time Summer = CreateTemporal(SUTime.StandardTemporalType.SeasonOfYear, "SU", new SUTime.InexactTime(SummerSolstice, Quarter, new SUTime.Range(SUTime.June, SUTime.September, SUTime.Quarter)));

		public static readonly SUTime.Time Fall = CreateTemporal(SUTime.StandardTemporalType.SeasonOfYear, "FA", new SUTime.InexactTime(FallEquinox, Quarter, new SUTime.Range(SUTime.September, SUTime.December, SUTime.Quarter)));

		public static readonly SUTime.Time Winter = CreateTemporal(SUTime.StandardTemporalType.SeasonOfYear, "WI", new SUTime.InexactTime(WinterSolstice, Quarter, new SUTime.Range(SUTime.December, SUTime.March, SUTime.Quarter)));

		public static readonly SUTime.PartialTime Noon = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "MI", new SUTime.IsoTime(12, 0, -1));

		public static readonly SUTime.PartialTime Midnight = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, new SUTime.IsoTime(0, 0, -1));

		public static readonly SUTime.Time Morning = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "MO", new SUTime.InexactTime(new SUTime.Range(new SUTime.InexactTime(new Partial(DateTimeFieldType.HourOfDay(), 6)), Noon)));

		public static readonly SUTime.Time Afternoon = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "AF", new SUTime.InexactTime(new SUTime.Range(Noon, new SUTime.InexactTime(new Partial(DateTimeFieldType.HourOfDay(), 18)))));

		public static readonly SUTime.Time Evening = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "EV", new SUTime.InexactTime(new SUTime.Range(new SUTime.InexactTime(new Partial(DateTimeFieldType.HourOfDay(), 18)), new SUTime.InexactTime(new 
			Partial(DateTimeFieldType.HourOfDay(), 20)))));

		public static readonly SUTime.Time Night = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "NI", new SUTime.InexactTime(Midnight, new SUTime.Range(new SUTime.InexactTime(new Partial(DateTimeFieldType.HourOfDay(), 14)), Hour.MultiplyBy(
			10))));

		public static readonly SUTime.Time Sunrise = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "MO", SUTime.TimexMod.Early.ToString(), new SUTime.PartialTime());

		public static readonly SUTime.Time Sunset = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "EV", SUTime.TimexMod.Early.ToString(), new SUTime.PartialTime());

		public static readonly SUTime.Time Dawn = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "MO", SUTime.TimexMod.Early.ToString(), new SUTime.PartialTime());

		public static readonly SUTime.Time Dusk = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "EV", new SUTime.PartialTime());

		public static readonly SUTime.Time Daytime = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "DT", new SUTime.InexactTime(new SUTime.Range(Sunrise, Sunset)));

		public static readonly SUTime.Time Lunchtime = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "MI", new SUTime.InexactTime(new SUTime.Range(new SUTime.InexactTime(new Partial(DateTimeFieldType.HourOfDay(), 12)), new SUTime.InexactTime
			(new Partial(DateTimeFieldType.HourOfDay(), 14)))));

		public static readonly SUTime.Time Teatime = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "AF", new SUTime.InexactTime(new SUTime.Range(new SUTime.InexactTime(new Partial(DateTimeFieldType.HourOfDay(), 15)), new SUTime.InexactTime(new 
			Partial(DateTimeFieldType.HourOfDay(), 17)))));

		public static readonly SUTime.Time Dinnertime = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "EV", new SUTime.InexactTime(new SUTime.Range(new SUTime.InexactTime(new Partial(DateTimeFieldType.HourOfDay(), 18)), new SUTime.InexactTime
			(new Partial(DateTimeFieldType.HourOfDay(), 20)))));

		public static readonly SUTime.Time MorningTwilight = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "MO", new SUTime.InexactTime(new SUTime.Range(Dawn, Sunrise)));

		public static readonly SUTime.Time EveningTwilight = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "EV", new SUTime.InexactTime(new SUTime.Range(Sunset, Dusk)));

		public static readonly SUTime.TemporalSet Twilight = CreateTemporal(SUTime.StandardTemporalType.TimeOfDay, "NI", new SUTime.ExplicitTemporalSet(EveningTwilight, MorningTwilight));

		public static readonly SUTime.RelativeTime Yesterday = new SUTime.RelativeTime(Day.MultiplyBy(-1));

		public static readonly SUTime.RelativeTime Tomorrow = new SUTime.RelativeTime(Day.MultiplyBy(+1));

		public static readonly SUTime.RelativeTime Today = new SUTime.RelativeTime(SUTime.TemporalOp.This, SUTime.Day);

		public static readonly SUTime.RelativeTime Tonight = new SUTime.RelativeTime(SUTime.TemporalOp.This, SUTime.Night);

		[System.Serializable]
		public sealed class TimeUnit
		{
			public static readonly SUTime.TimeUnit Millis = new SUTime.TimeUnit(SUTime.Millis);

			public static readonly SUTime.TimeUnit Second = new SUTime.TimeUnit(SUTime.Second);

			public static readonly SUTime.TimeUnit Minute = new SUTime.TimeUnit(SUTime.Minute);

			public static readonly SUTime.TimeUnit Hour = new SUTime.TimeUnit(SUTime.Hour);

			public static readonly SUTime.TimeUnit Day = new SUTime.TimeUnit(SUTime.Day);

			public static readonly SUTime.TimeUnit Week = new SUTime.TimeUnit(SUTime.Week);

			public static readonly SUTime.TimeUnit Month = new SUTime.TimeUnit(SUTime.Month);

			public static readonly SUTime.TimeUnit Quarter = new SUTime.TimeUnit(SUTime.Quarter);

			public static readonly SUTime.TimeUnit Halfyear = new SUTime.TimeUnit(SUTime.Halfyear);

			public static readonly SUTime.TimeUnit Year = new SUTime.TimeUnit(SUTime.Year);

			public static readonly SUTime.TimeUnit Decade = new SUTime.TimeUnit(SUTime.Decade);

			public static readonly SUTime.TimeUnit Century = new SUTime.TimeUnit(SUTime.Century);

			public static readonly SUTime.TimeUnit Millennium = new SUTime.TimeUnit(SUTime.Millennium);

			public static readonly SUTime.TimeUnit Unknown = new SUTime.TimeUnit(SUTime.DurationUnknown);

			protected internal SUTime.Duration duration;

			internal TimeUnit(SUTime.Duration d)
			{
				// Months
				// Use constructors rather than calls to
				// StandardTemporalType.createTemporal because sometimes the class
				// loader seems to load objects in an incorrect order, resulting in
				// an exception.  This is especially evident when deserializing
				// Dates are rough with respect to northern hemisphere (actual
				// solstice/equinox days depend on the year)
				// Dates for seasons are rough with respect to northern hemisphere
				// Time of day
				// Relative days
				// Basic time units
				this.duration = d;
			}

			public SUTime.Duration GetDuration()
			{
				return SUTime.TimeUnit.duration;
			}

			// How long does this time last?
			public SUTime.Duration GetPeriod()
			{
				return SUTime.TimeUnit.duration;
			}

			// How often does this type of time occur?
			public SUTime.Duration GetGranularity()
			{
				return SUTime.TimeUnit.duration;
			}

			// What is the granularity of this time?
			public SUTime.Temporal CreateTemporal(int n)
			{
				return SUTime.TimeUnit.duration.MultiplyBy(n);
			}
		}

		[System.Serializable]
		public sealed class StandardTemporalType
		{
			public static readonly SUTime.StandardTemporalType Refdate = new SUTime.StandardTemporalType(SUTime.TimexType.Date);

			public static readonly SUTime.StandardTemporalType Reftime = new SUTime.StandardTemporalType(SUTime.TimexType.Time);

			public static readonly SUTime.StandardTemporalType TimeOfDay = new SUTime.StandardTemporalType(SUTime.TimexType.Time, SUTime.TimeUnit.Hour, SUTime.Day);

			public static readonly SUTime.StandardTemporalType DayOfYear = new SUTime.StandardTemporalType(SUTime.TimexType.Date, SUTime.TimeUnit.Day, SUTime.Year);

			public static readonly SUTime.StandardTemporalType DayOfWeek = new SUTime.StandardTemporalType(SUTime.TimexType.Date, SUTime.TimeUnit.Day, SUTime.Week);

			public static readonly SUTime.StandardTemporalType DaysOfWeek = new SUTime.StandardTemporalType(SUTime.TimexType.Date, SUTime.TimeUnit.Day, SUTime.Week);

			public static readonly SUTime.StandardTemporalType WeekOfYear = new SUTime.StandardTemporalType(SUTime.TimexType.Date, SUTime.TimeUnit.Week, SUTime.Year);

			public static readonly SUTime.StandardTemporalType MonthOfYear = new SUTime.StandardTemporalType(SUTime.TimexType.Date, SUTime.TimeUnit.Month, SUTime.Year);

			public static readonly SUTime.StandardTemporalType PartOfYear = new SUTime.StandardTemporalType(SUTime.TimexType.Date, SUTime.TimeUnit.Day, SUTime.Year);

			public static readonly SUTime.StandardTemporalType SeasonOfYear = new SUTime.StandardTemporalType(SUTime.TimexType.Date, SUTime.TimeUnit.Quarter, SUTime.Year);

			public static readonly SUTime.StandardTemporalType QuarterOfYear = new SUTime.StandardTemporalType(SUTime.TimexType.Date, SUTime.TimeUnit.Quarter, SUTime.Year);

			public static readonly SUTime.StandardTemporalType HalfOfYear = new SUTime.StandardTemporalType(SUTime.TimexType.Date, SUTime.TimeUnit.Halfyear, SUTime.Year);

			internal readonly SUTime.TimexType timexType;

			internal SUTime.TimeUnit unit = SUTime.TimeUnit.Unknown;

			internal SUTime.Duration period = SUTime.DurationNone;

			internal StandardTemporalType(SUTime.TimexType timexType)
			{
				/*   MILLIS(TimexType.TIME, TimeUnit.MILLIS),
				SECOND(TimexType.TIME, TimeUnit.SECOND),
				MINUTE(TimexType.TIME, TimeUnit.MINUTE),
				HOUR(TimexType.TIME, TimeUnit.HOUR),
				DAY(TimexType.TIME, TimeUnit.DAY),
				WEEK(TimexType.TIME, TimeUnit.WEEK),
				MONTH(TimexType.TIME, TimeUnit.MONTH),
				QUARTER(TimexType.TIME, TimeUnit.QUARTER),
				YEAR(TimexType.TIME, TimeUnit.YEAR),  */
				//return new PartialTime(new Partial(DateTimeFieldType.monthOfYear(), n));
				this.timexType = timexType;
			}

			internal StandardTemporalType(SUTime.TimexType timexType, SUTime.TimeUnit unit)
			{
				this.timexType = timexType;
				this.unit = unit;
				this.period = unit.GetPeriod();
			}

			internal StandardTemporalType(SUTime.TimexType timexType, SUTime.TimeUnit unit, SUTime.Duration period)
			{
				this.timexType = timexType;
				this.unit = unit;
				this.period = period;
			}

			public SUTime.TimexType GetTimexType()
			{
				return SUTime.StandardTemporalType.timexType;
			}

			public SUTime.Duration GetDuration()
			{
				return SUTime.StandardTemporalType.unit.GetDuration();
			}

			// How long does this time last?
			public SUTime.Duration GetPeriod()
			{
				return SUTime.StandardTemporalType.period;
			}

			// How often does this type of time occur?
			public SUTime.Duration GetGranularity()
			{
				return SUTime.StandardTemporalType.unit.GetGranularity();
			}

			// What is the granularity of this time?
			protected internal SUTime.Temporal _createTemporal(int n)
			{
				return null;
			}

			public SUTime.Temporal CreateTemporal(int n)
			{
				SUTime.Temporal t = _createTemporal(n);
				if (t != null)
				{
					t.standardTemporalType = this;
				}
				return t;
			}

			public static SUTime.Temporal Create(Expressions.CompositeValue compositeValue)
			{
				SUTime.StandardTemporalType temporalType = compositeValue.Get("type");
				string label = compositeValue.Get("label");
				string modifier = compositeValue.Get("modifier");
				SUTime.Temporal temporal = compositeValue.Get("value");
				if (temporal == null)
				{
					temporal = new SUTime.PartialTime();
				}
				return SUTime.CreateTemporal(temporalType, label, modifier, temporal);
			}
		}

		[System.Serializable]
		public sealed class TemporalOp
		{
			public static readonly SUTime.TemporalOp Next = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp NextImmediate = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp This = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp Prev = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp PrevImmediate = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp Union = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp Intersect = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp In = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp Offset = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp Minus = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp Plus = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp Min = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp Max = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp Multiply = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp Divide = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp Create = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp AddModifier = new SUTime.TemporalOp();

			public static readonly SUTime.TemporalOp OffsetExact = new SUTime.TemporalOp();

			// Temporal operators (currently operates on two temporals and returns another
			// temporal)
			// Can add operators for:
			// lookup of temporal from string
			// creating durations, dates
			// public interface TemporalOp extends Function<Temporal,Temporal>();
			// For durations: possible interpretation of next/prev:
			// next month, next week
			// NEXT: on Thursday, next week = week starting on next monday
			// ??? on Thursday, next week = one week starting from now
			// prev month, prev week
			// PREV: on Thursday, last week = week starting on the monday one week
			// before this monday
			// ??? on Thursday, last week = one week going back starting from now
			// NEXT: on June 19, next month = July 1 to July 31
			// ???:  on June 19, next month = July 19 to August 19
			//
			//
			// For partial dates: two kind of next
			// next tuesday, next winter, next january
			// NEXT (PARENT UNIT, FAVOR): Example: on monday, next tuesday = tuesday of
			// the week after this
			// NEXT IMMEDIATE (NOT FAVORED): Example: on monday, next saturday =
			// saturday of this week
			// last saturday, last winter, last january
			// PREV (PARENT UNIT, FAVOR): Example: on wednesday, last tuesday = tuesday
			// of the week before this
			// PREV IMMEDIATE (NOT FAVORED): Example: on saturday, last tuesday =
			// tuesday of this week
			// (successor) Next week/day/...
			// TODO: flags?
			/* RESOLVE_TO_FUTURE */
			// This coming week/friday
			// Temporal arg2Next = arg2.next();
			// if (arg1 == null || arg2Next == null) { return arg2Next; }
			// TODO: flags?
			// Use arg1 as reference to resolve arg2 (take more general fields from arg1
			// and apply to arg2)
			// TODO: flags?
			// (predecessor) Previous week/day/...
			// TODO: flags?
			/*RESOLVE_TO_PAST */
			// This past week/friday
			// Temporal arg2Prev = arg2.prev();
			// if (arg1 == null || arg2Prev == null) { return arg2Prev; }
			// TODO: flags?
			// return arg1.union(arg2);
			// throw new
			// UnsupportedOperationException("INTERSECT not implemented for arg1=" +
			// arg1.getClass() + ", arg2="+arg2.getClass());
			// arg2 is "in" arg1, composite datetime
			// TODO: flags?
			// There is inexact offset where we remove anything from the result that is more granular than the duration
			// There is exact offset (more granular parts than the duration are kept)
			public SUTime.Temporal Apply(SUTime.Temporal arg1, SUTime.Temporal arg2, int flags)
			{
				throw new NotSupportedException("apply(Temporal, Temporal, int) not implemented for TemporalOp " + this);
			}

			public SUTime.Temporal Apply(SUTime.Temporal arg1, SUTime.Temporal arg2)
			{
				return Apply(arg1, arg2, 0);
			}

			public SUTime.Temporal Apply(params SUTime.Temporal[] args)
			{
				if (args.Length == 2)
				{
					return Apply(args[0], args[1]);
				}
				throw new NotSupportedException("apply(Temporal...) not implemented for TemporalOp " + this);
			}

			public SUTime.Temporal Apply(params object[] args)
			{
				throw new NotSupportedException("apply(Object...) not implemented for TemporalOp " + this);
			}
		}

		/// <summary>Time represents a time point on some time scale.</summary>
		/// <remarks>
		/// Time represents a time point on some time scale.
		/// It is the base class for representing various types of time points.
		/// Typically, since most time scales have marks with certain granularity
		/// each time point can be represented as an interval.
		/// </remarks>
		[System.Serializable]
		public abstract class Time : SUTime.Temporal, FuzzyInterval.IFuzzyComparable<SUTime.Time>, IHasInterval<SUTime.Time>
		{
			public Time()
			{
			}

			public Time(SUTime.Time t)
				: base(t)
			{
			}

			/*this.hasTime = t.hasTime; */
			// Represents a point in time - there is typically some
			// uncertainty/imprecision in the exact time
			public override bool IsGrounded()
			{
				return false;
			}

			// A time is defined by a begin and end point, and a duration
			public override SUTime.Time GetTime()
			{
				return this;
			}

			// Default is a instant in time with same begin and end point
			// Every time should return a non-null range
			public override SUTime.Range GetRange(int flags, SUTime.Duration granularity)
			{
				return new SUTime.Range(this, this);
			}

			// Default duration is zero
			public override SUTime.Duration GetDuration()
			{
				return DurationNone;
			}

			public override SUTime.Duration GetGranularity()
			{
				SUTime.StandardTemporalType tlt = GetStandardTemporalType();
				if (tlt != null)
				{
					return tlt.GetGranularity();
				}
				Partial p = this.GetJodaTimePartial();
				return SUTime.Duration.GetDuration(JodaTimeUtils.GetJodaTimePeriod(p));
			}

			public virtual Interval<SUTime.Time> GetInterval()
			{
				SUTime.Range r = GetRange();
				if (r != null)
				{
					return r.GetInterval();
				}
				else
				{
					return null;
				}
			}

			public virtual bool IsComparable(SUTime.Time t)
			{
				Instant i = this.GetJodaTimeInstant();
				Instant i2 = t.GetJodaTimeInstant();
				return (i != null && i2 != null);
			}

			public virtual int CompareTo(SUTime.Time t)
			{
				Instant i = this.GetJodaTimeInstant();
				Instant i2 = t.GetJodaTimeInstant();
				return i.CompareTo(i2);
			}

			public virtual bool HasTime()
			{
				return false;
			}

			public override SUTime.TimexType GetTimexType()
			{
				if (GetStandardTemporalType() != null)
				{
					return GetStandardTemporalType().GetTimexType();
				}
				return (HasTime()) ? SUTime.TimexType.Time : SUTime.TimexType.Date;
			}

			// Time operations
			public virtual bool Contains(SUTime.Time t)
			{
				// Check if this time contains other time
				return GetRange().Contains(t.GetRange());
			}

			// public boolean isBefore(Time t);
			// public boolean isAfter(Time t);
			// public boolean overlaps(Time t);
			public virtual SUTime.Time ReduceGranularityTo(SUTime.Duration d)
			{
				return this;
			}

			// Add duration to time
			public abstract SUTime.Time Add(SUTime.Duration offset);

			public virtual SUTime.Time Offset(SUTime.Duration offset, int flags)
			{
				SUTime.Time res = Add(offset);
				if ((flags & RelativeOffsetInexact) != 0)
				{
					// Mark as uncertain anything not as granular as the granularity of the offset
					res.uncertaintyGranularity = offset.GetGranularity();
					return res;
				}
				else
				{
					return res;
				}
			}

			public virtual SUTime.Time Subtract(SUTime.Duration offset)
			{
				return Add(offset.MultiplyBy(-1));
			}

			// Return closest time
			public static SUTime.Time Closest(SUTime.Time @ref, params SUTime.Time[] times)
			{
				SUTime.Time res = null;
				long refMillis = @ref.GetJodaTimeInstant().GetMillis();
				long min = 0;
				foreach (SUTime.Time t in times)
				{
					long d = Math.Abs(refMillis - t.GetJodaTimeInstant().GetMillis());
					if (res == null || d < min)
					{
						res = t;
						min = d;
					}
				}
				return res;
			}

			// Get absolute difference between times
			public static SUTime.Duration Distance(SUTime.Time t1, SUTime.Time t2)
			{
				if (t1.CompareTo(t2) < 0)
				{
					return Difference(t1, t2);
				}
				else
				{
					return Difference(t2, t1);
				}
			}

			// Get difference between times
			public static SUTime.Duration Difference(SUTime.Time t1, SUTime.Time t2)
			{
				// TODO: Difference does not work between days of the week
				// Get duration from this t1 to t2
				if (t1 == null || t2 == null)
				{
					return null;
				}
				Instant i1 = t1.GetJodaTimeInstant();
				Instant i2 = t2.GetJodaTimeInstant();
				if (i1 == null || i2 == null)
				{
					return null;
				}
				SUTime.Duration d = new SUTime.DurationWithMillis(i2.GetMillis() - i1.GetMillis());
				SUTime.Duration g1 = t1.GetGranularity();
				SUTime.Duration g2 = t2.GetGranularity();
				SUTime.Duration g = SUTime.Duration.Max(g1, g2);
				if (g != null)
				{
					Period p = g.GetJodaTimePeriod();
					p = p.NormalizedStandard();
					Period p2 = JodaTimeUtils.DiscardMoreSpecificFields(d.GetJodaTimePeriod(), p.GetFieldType(p.Size() - 1), i1.GetChronology());
					return new SUTime.DurationWithFields(p2);
				}
				else
				{
					return d;
				}
			}

			public static SUTime.CompositePartialTime MakeComposite(SUTime.PartialTime pt, SUTime.Time t)
			{
				SUTime.CompositePartialTime cp = null;
				SUTime.StandardTemporalType tlt = t.GetStandardTemporalType();
				if (tlt != null)
				{
					switch (tlt)
					{
						case SUTime.StandardTemporalType.TimeOfDay:
						{
							cp = new SUTime.CompositePartialTime(pt, null, null, t);
							break;
						}

						case SUTime.StandardTemporalType.PartOfYear:
						case SUTime.StandardTemporalType.QuarterOfYear:
						case SUTime.StandardTemporalType.SeasonOfYear:
						{
							cp = new SUTime.CompositePartialTime(pt, t, null, null);
							break;
						}

						case SUTime.StandardTemporalType.DaysOfWeek:
						{
							cp = new SUTime.CompositePartialTime(pt, null, t, null);
							break;
						}
					}
				}
				return cp;
			}

			public override SUTime.Temporal Resolve(SUTime.Time t, int flags)
			{
				return this;
			}

			public override SUTime.Temporal Intersect(SUTime.Temporal t)
			{
				if (t == null)
				{
					return this;
				}
				if (t == TimeUnknown || t == DurationUnknown)
				{
					return this;
				}
				if (t is SUTime.Time)
				{
					return Intersect((SUTime.Time)t);
				}
				else
				{
					if (t is SUTime.Range)
					{
						return t.Intersect(this);
					}
					else
					{
						if (t is SUTime.Duration)
						{
							return new SUTime.RelativeTime(this, SUTime.TemporalOp.Intersect, t);
						}
					}
				}
				return null;
			}

			protected internal virtual SUTime.Time Intersect(SUTime.Time t)
			{
				return null;
			}

			//new RelativeTime(this, TemporalOp.INTERSECT, t);
			protected internal static SUTime.Time Intersect(SUTime.Time t1, SUTime.Time t2)
			{
				if (t1 == null)
				{
					return t2;
				}
				if (t2 == null)
				{
					return t1;
				}
				return t1.Intersect(t2);
			}

			public static SUTime.Time Min(SUTime.Time t1, SUTime.Time t2)
			{
				if (t2 == null)
				{
					return t1;
				}
				if (t1 == null)
				{
					return t2;
				}
				if (t1.IsComparable(t2))
				{
					int c = t1.CompareTo(t2);
					return (c < 0) ? t1 : t2;
				}
				return t1;
			}

			public static SUTime.Time Max(SUTime.Time t1, SUTime.Time t2)
			{
				if (t1 == null)
				{
					return t2;
				}
				if (t2 == null)
				{
					return t1;
				}
				if (t1.IsComparable(t2))
				{
					int c = t1.CompareTo(t2);
					return (c >= 0) ? t1 : t2;
				}
				return t2;
			}

			// Conversions to joda time
			public virtual Instant GetJodaTimeInstant()
			{
				return null;
			}

			public virtual Partial GetJodaTimePartial()
			{
				return null;
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>Reference time (some kind of reference time).</summary>
		[System.Serializable]
		public class RefTime : SUTime.Time
		{
			internal string label;

			public RefTime(string label)
			{
				this.label = label;
			}

			public RefTime(SUTime.StandardTemporalType timeType, string timeLabel, string label)
			{
				this.standardTemporalType = timeType;
				this.timeLabel = timeLabel;
				this.label = label;
			}

			public override bool IsRef()
			{
				return true;
			}

			public override string ToFormattedString(int flags)
			{
				if (GetTimeLabel() != null)
				{
					return GetTimeLabel();
				}
				if ((flags & FormatIso) != 0)
				{
					return null;
				}
				// TODO: is there iso standard?
				return label;
			}

			public override SUTime.Time Add(SUTime.Duration offset)
			{
				return new SUTime.RelativeTime(this, SUTime.TemporalOp.OffsetExact, offset);
			}

			public override SUTime.Time Offset(SUTime.Duration offset, int offsetFlags)
			{
				if ((offsetFlags & RelativeOffsetInexact) != 0)
				{
					return new SUTime.RelativeTime(this, SUTime.TemporalOp.Offset, offset);
				}
				else
				{
					return new SUTime.RelativeTime(this, SUTime.TemporalOp.OffsetExact, offset);
				}
			}

			public override SUTime.Temporal Resolve(SUTime.Time refTime, int flags)
			{
				if (this == TimeRef)
				{
					return refTime;
				}
				else
				{
					if (this == TimeNow && (flags & ResolveNow) != 0)
					{
						return refTime;
					}
					else
					{
						return this;
					}
				}
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>Simple time (vague time that we don't really know what to do with)</summary>
		[System.Serializable]
		public class SimpleTime : SUTime.Time
		{
			internal string label;

			public SimpleTime(string label)
			{
				this.label = label;
			}

			public override string ToFormattedString(int flags)
			{
				if (GetTimeLabel() != null)
				{
					return GetTimeLabel();
				}
				if ((flags & FormatIso) != 0)
				{
					return null;
				}
				// TODO: is there iso standard?
				return label;
			}

			public override SUTime.Time Add(SUTime.Duration offset)
			{
				SUTime.Time t = new SUTime.RelativeTime(this, SUTime.TemporalOp.OffsetExact, offset);
				// t.approx = this.approx;
				// t.mod = this.mod;
				return t;
			}

			private const long serialVersionUID = 1;
		}

		[System.Serializable]
		public class CompositePartialTime : SUTime.PartialTime
		{
			internal SUTime.Time tod;

			internal SUTime.Time dow;

			internal SUTime.Time poy;

			public CompositePartialTime(SUTime.PartialTime t, SUTime.Time poy, SUTime.Time dow, SUTime.Time tod)
				: base(t)
			{
				// Composite time - like PartialTime but with more, approximate fields
				// Summer weekend morning in June
				// Time of day
				// Day of week
				// Part of year
				// Duration duration; // Underspecified time (like day in June)
				this.poy = poy;
				this.dow = dow;
				this.tod = tod;
			}

			public CompositePartialTime(SUTime.PartialTime t, Partial p, SUTime.Time poy, SUTime.Time dow, SUTime.Time tod)
				: this(t, poy, dow, tod)
			{
				this.@base = p;
			}

			public override Instant GetJodaTimeInstant()
			{
				Partial p = @base;
				if (tod != null)
				{
					Partial p2 = tod.GetJodaTimePartial();
					if (p2 != null && JodaTimeUtils.IsCompatible(p, p2))
					{
						p = JodaTimeUtils.Combine(p, p2);
					}
				}
				if (dow != null)
				{
					Partial p2 = dow.GetJodaTimePartial();
					if (p2 != null && JodaTimeUtils.IsCompatible(p, p2))
					{
						p = JodaTimeUtils.Combine(p, p2);
					}
				}
				if (poy != null)
				{
					Partial p2 = poy.GetJodaTimePartial();
					if (p2 != null && JodaTimeUtils.IsCompatible(p, p2))
					{
						p = JodaTimeUtils.Combine(p, p2);
					}
				}
				return JodaTimeUtils.GetInstant(p);
			}

			public override SUTime.Duration GetDuration()
			{
				/*      TimeLabel tl = getTimeLabel();
				if (tl != null) {
				return tl.getDuration();
				} */
				SUTime.StandardTemporalType tlt = GetStandardTemporalType();
				if (tlt != null)
				{
					return tlt.GetDuration();
				}
				SUTime.Duration bd = (@base != null) ? SUTime.Duration.GetDuration(JodaTimeUtils.GetJodaTimePeriod(@base)) : null;
				if (tod != null)
				{
					SUTime.Duration d = tod.GetDuration();
					return (bd.CompareTo(d) < 0) ? bd : d;
				}
				if (dow != null)
				{
					SUTime.Duration d = dow.GetDuration();
					return (bd.CompareTo(d) < 0) ? bd : d;
				}
				if (poy != null)
				{
					SUTime.Duration d = poy.GetDuration();
					return (bd.CompareTo(d) < 0) ? bd : d;
				}
				return bd;
			}

			public override SUTime.Duration GetPeriod()
			{
				/*    TimeLabel tl = getTimeLabel();
				if (tl != null) {
				return tl.getPeriod();
				} */
				SUTime.StandardTemporalType tlt = GetStandardTemporalType();
				if (tlt != null)
				{
					return tlt.GetPeriod();
				}
				SUTime.Duration bd = null;
				if (@base != null)
				{
					DateTimeFieldType mostGeneral = JodaTimeUtils.GetMostGeneral(@base);
					DurationFieldType df = mostGeneral.GetRangeDurationType();
					if (df == null)
					{
						df = mostGeneral.GetDurationType();
					}
					if (df != null)
					{
						bd = new SUTime.DurationWithFields(new Period().WithField(df, 1));
					}
				}
				if (poy != null)
				{
					SUTime.Duration d = poy.GetPeriod();
					return (bd.CompareTo(d) > 0) ? bd : d;
				}
				if (dow != null)
				{
					SUTime.Duration d = dow.GetPeriod();
					return (bd.CompareTo(d) > 0) ? bd : d;
				}
				if (tod != null)
				{
					SUTime.Duration d = tod.GetPeriod();
					return (bd.CompareTo(d) > 0) ? bd : d;
				}
				return bd;
			}

			private static SUTime.Range GetIntersectedRange(SUTime.CompositePartialTime cpt, SUTime.Range r, SUTime.Duration d)
			{
				SUTime.Time beginTime = r.BeginTime();
				SUTime.Time endTime = r.EndTime();
				if (beginTime != TimeUnknown && endTime != TimeUnknown)
				{
					SUTime.Time t1 = cpt.Intersect(r.BeginTime());
					if (t1 is SUTime.PartialTime)
					{
						((SUTime.PartialTime)t1).WithStandardFields();
					}
					SUTime.Time t2 = cpt.Intersect(r.EndTime());
					if (t2 is SUTime.PartialTime)
					{
						((SUTime.PartialTime)t2).WithStandardFields();
					}
					return new SUTime.Range(t1, t2, d);
				}
				else
				{
					if (beginTime != TimeUnknown && endTime == TimeUnknown)
					{
						SUTime.Time t1 = cpt.Intersect(r.BeginTime());
						if (t1 is SUTime.PartialTime)
						{
							((SUTime.PartialTime)t1).WithStandardFields();
						}
						SUTime.Time t2 = t1.Add(d);
						if (t2 is SUTime.PartialTime)
						{
							((SUTime.PartialTime)t2).WithStandardFields();
						}
						return new SUTime.Range(t1, t2, d);
					}
					else
					{
						throw new Exception("Unsupported range: " + r);
					}
				}
			}

			public override SUTime.Range GetRange(int flags, SUTime.Duration granularity)
			{
				SUTime.Duration d = GetDuration();
				if (tod != null)
				{
					SUTime.Range r = tod.GetRange(flags, granularity);
					if (r != null)
					{
						SUTime.CompositePartialTime cpt = new SUTime.CompositePartialTime(this, poy, dow, null);
						return GetIntersectedRange(cpt, r, d);
					}
					else
					{
						return base.GetRange(flags, granularity);
					}
				}
				if (dow != null)
				{
					SUTime.Range r = dow.GetRange(flags, granularity);
					if (r != null)
					{
						SUTime.CompositePartialTime cpt = new SUTime.CompositePartialTime(this, poy, dow, null);
						return GetIntersectedRange(cpt, r, d);
					}
					else
					{
						return base.GetRange(flags, granularity);
					}
				}
				if (poy != null)
				{
					SUTime.Range r = poy.GetRange(flags, granularity);
					if (r != null)
					{
						SUTime.CompositePartialTime cpt = new SUTime.CompositePartialTime(this, poy, null, null);
						return GetIntersectedRange(cpt, r, d);
					}
					else
					{
						return base.GetRange(flags, granularity);
					}
				}
				return base.GetRange(flags, granularity);
			}

			protected internal override SUTime.Time Intersect(SUTime.Time t)
			{
				if (t == null || t == TimeUnknown)
				{
					return this;
				}
				if (@base == null)
				{
					return t;
				}
				if (t is SUTime.PartialTime)
				{
					Pair<SUTime.PartialTime, SUTime.PartialTime> compatible = GetCompatible(this, (SUTime.PartialTime)t);
					if (compatible == null)
					{
						return null;
					}
					Partial p = JodaTimeUtils.Combine(compatible.first.@base, compatible.second.@base);
					if (t is SUTime.CompositePartialTime)
					{
						SUTime.CompositePartialTime cpt = (SUTime.CompositePartialTime)t;
						SUTime.Time ntod = SUTime.Time.Intersect(tod, cpt.tod);
						SUTime.Time ndow = SUTime.Time.Intersect(dow, cpt.dow);
						SUTime.Time npoy = SUTime.Time.Intersect(poy, cpt.poy);
						if (ntod == null && (tod != null || cpt.tod != null))
						{
							return null;
						}
						if (ndow == null && (dow != null || cpt.dow != null))
						{
							return null;
						}
						if (npoy == null && (poy != null || cpt.poy != null))
						{
							return null;
						}
						return new SUTime.CompositePartialTime(this, p, npoy, ndow, ntod);
					}
					else
					{
						return new SUTime.CompositePartialTime(this, p, poy, dow, tod);
					}
				}
				else
				{
					return base.Intersect(t);
				}
			}

			protected internal override SUTime.PartialTime AddSupported(Period p, int scalar)
			{
				return new SUTime.CompositePartialTime(this, @base.WithPeriodAdded(p, 1), poy, dow, tod);
			}

			protected internal override SUTime.PartialTime AddUnsupported(Period p, int scalar)
			{
				return new SUTime.CompositePartialTime(this, JodaTimeUtils.AddForce(@base, p, scalar), poy, dow, tod);
			}

			public override SUTime.Time ReduceGranularityTo(SUTime.Duration granularity)
			{
				Partial p = JodaTimeUtils.DiscardMoreSpecificFields(@base, JodaTimeUtils.GetMostSpecific(granularity.GetJodaTimePeriod()));
				return new SUTime.CompositePartialTime(this, p, poy.ReduceGranularityTo(granularity), dow.ReduceGranularityTo(granularity), tod.ReduceGranularityTo(granularity));
			}

			public override SUTime.Temporal Resolve(SUTime.Time @ref, int flags)
			{
				if (@ref == null || @ref == TimeUnknown || @ref == TimeRef)
				{
					return this;
				}
				if (this == TimeRef)
				{
					return @ref;
				}
				if (this == TimeUnknown)
				{
					return this;
				}
				Partial partialRef = @ref.GetJodaTimePartial();
				if (partialRef == null)
				{
					throw new NotSupportedException("Cannot resolve if reftime is of class: " + @ref.GetType());
				}
				DateTimeFieldType mgf = null;
				if (poy != null)
				{
					mgf = JodaTimeUtils.QuarterOfYear;
				}
				else
				{
					if (dow != null)
					{
						mgf = DateTimeFieldType.DayOfWeek();
					}
					else
					{
						if (tod != null)
						{
							mgf = DateTimeFieldType.HalfdayOfDay();
						}
					}
				}
				Partial p = (@base != null) ? JodaTimeUtils.CombineMoreGeneralFields(@base, partialRef, mgf) : partialRef;
				if (p.IsSupported(DateTimeFieldType.DayOfWeek()))
				{
					p = JodaTimeUtils.ResolveDowToDay(p, partialRef);
				}
				else
				{
					if (dow != null)
					{
						p = JodaTimeUtils.ResolveWeek(p, partialRef);
					}
				}
				if (p == @base)
				{
					return this;
				}
				else
				{
					return new SUTime.CompositePartialTime(this, p, poy, dow, tod);
				}
			}

			protected internal override DateTimeFormatter GetFormatter(int flags)
			{
				DateTimeFormatterBuilder builder = new DateTimeFormatterBuilder();
				bool hasDate = AppendDateFormats(builder, flags);
				if (poy != null)
				{
					if (!JodaTimeUtils.HasField(@base, DateTimeFieldType.MonthOfYear()))
					{
						// Assume poy is compatible with whatever was built and
						// poy.toISOString() does the correct thing
						builder.AppendLiteral("-");
						builder.AppendLiteral(poy.ToISOString());
						hasDate = true;
					}
				}
				if (dow != null)
				{
					if (!JodaTimeUtils.HasField(@base, DateTimeFieldType.MonthOfYear()) && !JodaTimeUtils.HasField(@base, DateTimeFieldType.DayOfWeek()))
					{
						builder.AppendLiteral("-");
						builder.AppendLiteral(dow.ToISOString());
						hasDate = true;
					}
				}
				if (HasTime())
				{
					if (!hasDate)
					{
						builder.Clear();
					}
					AppendTimeFormats(builder, flags);
				}
				else
				{
					if (tod != null)
					{
						if (!hasDate)
						{
							builder.Clear();
						}
						// Assume tod is compatible with whatever was built and
						// tod.toISOString() does the correct thing
						builder.AppendLiteral("T");
						builder.AppendLiteral(tod.ToISOString());
					}
				}
				return builder.ToFormatter();
			}

			public override SUTime.TimexType GetTimexType()
			{
				if (tod != null)
				{
					return SUTime.TimexType.Time;
				}
				return base.GetTimexType();
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>The nth temporal.</summary>
		/// <remarks>
		/// The nth temporal.
		/// Example: The tenth week (of something, don't know yet)
		/// The second friday
		/// </remarks>
		[System.Serializable]
		public class OrdinalTime : SUTime.Time
		{
			internal SUTime.Temporal @base;

			internal int n;

			public OrdinalTime(SUTime.Temporal @base, int n)
			{
				this.@base = @base;
				this.n = n;
			}

			public OrdinalTime(SUTime.Temporal @base, long n)
			{
				this.@base = @base;
				this.n = (int)n;
			}

			public override SUTime.Time Add(SUTime.Duration offset)
			{
				return new SUTime.RelativeTime(this, SUTime.TemporalOp.OffsetExact, offset);
			}

			public override string ToFormattedString(int flags)
			{
				if (GetTimeLabel() != null)
				{
					return GetTimeLabel();
				}
				if ((flags & FormatIso) != 0)
				{
					return null;
				}
				// TODO: is there iso standard?
				if ((flags & FormatTimex3Value) != 0)
				{
					return null;
				}
				// TODO: is there timex3 standard?
				if (@base != null)
				{
					string str = @base.ToFormattedString(flags);
					if (str != null)
					{
						return str + "-#" + n;
					}
				}
				return null;
			}

			protected internal override SUTime.Time Intersect(SUTime.Time t)
			{
				if (@base is SUTime.PartialTime && t is SUTime.PartialTime)
				{
					return new SUTime.OrdinalTime(@base.Intersect(t), n);
				}
				else
				{
					return new SUTime.RelativeTime(t, SUTime.TemporalOp.Intersect, this);
				}
			}

			public override SUTime.Temporal Resolve(SUTime.Time t, int flags)
			{
				if (t == null)
				{
					return this;
				}
				// No resolving to be done?
				if (@base is SUTime.PartialTime)
				{
					SUTime.PartialTime pt = (SUTime.PartialTime)@base.Resolve(t, flags);
					IList<SUTime.Temporal> list = pt.ToList();
					if (list != null && list.Count >= n)
					{
						return list[n - 1];
					}
				}
				else
				{
					if (@base is SUTime.Duration)
					{
						SUTime.Duration d = ((SUTime.Duration)@base).MultiplyBy(n - 1);
						SUTime.Time temp = t.GetRange().Begin();
						return temp.Offset(d, 0).ReduceGranularityTo(d.GetDuration());
					}
				}
				return this;
			}

			private const long serialVersionUID = 1;
		}

		[System.Serializable]
		public class TimeWithRange : SUTime.Time
		{
			internal SUTime.Range range;

			public TimeWithRange(SUTime.TimeWithRange t, SUTime.Range range)
				: base(t)
			{
				// end static class OrdinalTim
				// Time with a range (most times have a range...)
				// guess at range
				this.range = range;
			}

			public TimeWithRange(SUTime.Range range)
			{
				this.range = range;
			}

			public override SUTime.Temporal SetTimeZone(DateTimeZone tz)
			{
				return new SUTime.TimeWithRange(this, (SUTime.Range)SUTime.Temporal.SetTimeZone(range, tz));
			}

			public override SUTime.Duration GetDuration()
			{
				if (range != null)
				{
					return range.GetDuration();
				}
				else
				{
					return null;
				}
			}

			public override SUTime.Range GetRange(int flags, SUTime.Duration granularity)
			{
				if (range != null)
				{
					return range.GetRange(flags, granularity);
				}
				else
				{
					return null;
				}
			}

			public override SUTime.Time Add(SUTime.Duration offset)
			{
				// TODO: Check logic
				//      if (getTimeLabel() != null) {
				if (GetStandardTemporalType() != null)
				{
					// Time has some meaning, keep as is
					return new SUTime.RelativeTime(this, SUTime.TemporalOp.OffsetExact, offset);
				}
				else
				{
					return new SUTime.TimeWithRange(this, range.Offset(offset, 0));
				}
			}

			protected internal override SUTime.Time Intersect(SUTime.Time t)
			{
				if (t == null || t == TimeUnknown)
				{
					return this;
				}
				if (t is SUTime.CompositePartialTime)
				{
					return t.Intersect(this);
				}
				else
				{
					if (t is SUTime.PartialTime)
					{
						return t.Intersect(this);
					}
					else
					{
						if (t is SUTime.GroundedTime)
						{
							return t.Intersect(this);
						}
						else
						{
							return new SUTime.TimeWithRange((SUTime.Range)range.Intersect(t));
						}
					}
				}
			}

			public override SUTime.Temporal Resolve(SUTime.Time refTime, int flags)
			{
				SUTime.CompositePartialTime cpt = MakeComposite(new SUTime.PartialTime(new Partial()), this);
				if (cpt != null)
				{
					return ((SUTime.Time)cpt.Resolve(refTime, flags));
				}
				SUTime.Range groundedRange = null;
				if (range != null)
				{
					groundedRange = ((SUTime.Range)range.Resolve(refTime, flags)).GetRange();
				}
				return CreateTemporal(standardTemporalType, timeLabel, new SUTime.TimeWithRange(this, groundedRange));
			}

			//return new TimeWithRange(this, groundedRange);
			public override string ToFormattedString(int flags)
			{
				if (GetTimeLabel() != null)
				{
					return GetTimeLabel();
				}
				if ((flags & FormatTimex3Value) != 0)
				{
					flags |= FormatIso;
				}
				return range.ToFormattedString(flags);
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>Inexact time, not sure when this is, but have some guesses.</summary>
		[System.Serializable]
		public class InexactTime : SUTime.Time
		{
			internal SUTime.Time @base;

			internal SUTime.Duration duration;

			internal SUTime.Range range;

			public InexactTime(Partial partial)
			{
				// best guess
				// how long the time lasts
				// guess at range in which the time occurs
				this.@base = new SUTime.PartialTime(partial);
				this.range = @base.GetRange();
				this.approx = true;
			}

			public InexactTime(SUTime.Time @base, SUTime.Duration duration, SUTime.Range range)
			{
				this.@base = @base;
				this.duration = duration;
				this.range = range;
				this.approx = true;
			}

			public InexactTime(SUTime.Time @base, SUTime.Range range)
			{
				this.@base = @base;
				this.range = range;
				this.approx = true;
			}

			public InexactTime(SUTime.InexactTime t, SUTime.Time @base, SUTime.Duration duration, SUTime.Range range)
				: base(t)
			{
				this.@base = @base;
				this.duration = duration;
				this.range = range;
				this.approx = true;
			}

			public InexactTime(SUTime.Range range)
			{
				this.@base = range.Mid();
				this.range = range;
				this.approx = true;
			}

			public override int CompareTo(SUTime.Time t)
			{
				if (this.@base != null)
				{
					return (this.@base.CompareTo(t));
				}
				if (this.range != null)
				{
					if (this.range.Begin() != null && this.range.Begin().CompareTo(t) > 0)
					{
						return 1;
					}
					else
					{
						if (this.range.End() != null && this.range.End().CompareTo(t) < 0)
						{
							return -1;
						}
						else
						{
							return this.range.GetTime().CompareTo(t);
						}
					}
				}
				return 0;
			}

			public override SUTime.Temporal SetTimeZone(DateTimeZone tz)
			{
				return new SUTime.InexactTime(this, (SUTime.Time)SUTime.Temporal.SetTimeZone(@base, tz), duration, (SUTime.Range)SUTime.Temporal.SetTimeZone(range, tz));
			}

			public override SUTime.Time GetTime()
			{
				return this;
			}

			public override SUTime.Duration GetDuration()
			{
				if (duration != null)
				{
					return duration;
				}
				if (range != null)
				{
					return range.GetDuration();
				}
				else
				{
					if (@base != null)
					{
						return @base.GetDuration();
					}
					else
					{
						return null;
					}
				}
			}

			public override SUTime.Range GetRange(int flags, SUTime.Duration granularity)
			{
				if (range != null)
				{
					return range.GetRange(flags, granularity);
				}
				else
				{
					if (@base != null)
					{
						return @base.GetRange(flags, granularity);
					}
					else
					{
						return null;
					}
				}
			}

			public override SUTime.Time Add(SUTime.Duration offset)
			{
				//if (getTimeLabel() != null) {
				if (GetStandardTemporalType() != null)
				{
					// Time has some meaning, keep as is
					return new SUTime.RelativeTime(this, SUTime.TemporalOp.OffsetExact, offset);
				}
				else
				{
					// Some other time, who know what it means
					// Try to do offset
					return new SUTime.InexactTime(this, (SUTime.Time)SUTime.TemporalOp.OffsetExact.Apply(@base, offset), duration, (SUTime.Range)SUTime.TemporalOp.OffsetExact.Apply(range, offset));
				}
			}

			public override SUTime.Temporal Resolve(SUTime.Time refTime, int flags)
			{
				SUTime.CompositePartialTime cpt = MakeComposite(new SUTime.PartialTime(this, new Partial()), this);
				if (cpt != null)
				{
					return ((SUTime.Time)cpt.Resolve(refTime, flags));
				}
				SUTime.Time groundedBase = null;
				if (@base == TimeRef)
				{
					groundedBase = refTime;
				}
				else
				{
					if (@base != null)
					{
						groundedBase = @base.Resolve(refTime, flags).GetTime();
					}
				}
				SUTime.Range groundedRange = null;
				if (range != null)
				{
					groundedRange = ((SUTime.Range)range.Resolve(refTime, flags)).GetRange();
				}
				/*    if (groundedRange == range && groundedBase == base) {
				return this;
				} */
				return CreateTemporal(standardTemporalType, timeLabel, mod, new SUTime.InexactTime(groundedBase, duration, groundedRange));
			}

			//return new InexactTime(groundedBase, duration, groundedRange);
			public override Instant GetJodaTimeInstant()
			{
				Instant p = null;
				if (@base != null)
				{
					p = @base.GetJodaTimeInstant();
				}
				if (p == null && range != null)
				{
					p = range.Mid().GetJodaTimeInstant();
				}
				return p;
			}

			public override Partial GetJodaTimePartial()
			{
				Partial p = null;
				if (@base != null)
				{
					p = @base.GetJodaTimePartial();
				}
				if (p == null && range != null && range.Mid() != null)
				{
					p = range.Mid().GetJodaTimePartial();
				}
				return p;
			}

			public override string ToFormattedString(int flags)
			{
				if (GetTimeLabel() != null)
				{
					return GetTimeLabel();
				}
				if ((flags & FormatIso) != 0)
				{
					return null;
				}
				// TODO: is there iso standard?
				if ((flags & FormatTimex3Value) != 0)
				{
					return null;
				}
				// TODO: is there timex3 standard?
				StringBuilder sb = new StringBuilder();
				sb.Append("~(");
				if (@base != null)
				{
					sb.Append(@base.ToFormattedString(flags));
				}
				if (duration != null)
				{
					sb.Append(":");
					sb.Append(duration.ToFormattedString(flags));
				}
				if (range != null)
				{
					sb.Append(" IN ");
					sb.Append(range.ToFormattedString(flags));
				}
				sb.Append(")");
				return sb.ToString();
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>Relative Time (something not quite resolved).</summary>
		[System.Serializable]
		public class RelativeTime : SUTime.Time
		{
			private SUTime.Time @base = TimeRef;

			private SUTime.TemporalOp tempOp;

			private SUTime.Temporal tempArg;

			private int opFlags;

			public RelativeTime(SUTime.Time @base, SUTime.TemporalOp tempOp, SUTime.Temporal tempArg, int flags)
				: base(@base)
			{
				this.@base = @base;
				this.tempOp = tempOp;
				this.tempArg = tempArg;
				this.opFlags = flags;
			}

			public RelativeTime(SUTime.Time @base, SUTime.TemporalOp tempOp, SUTime.Temporal tempArg)
				: base(@base)
			{
				this.@base = @base;
				this.tempOp = tempOp;
				this.tempArg = tempArg;
			}

			public RelativeTime(SUTime.TemporalOp tempOp, SUTime.Temporal tempArg)
			{
				this.tempOp = tempOp;
				this.tempArg = tempArg;
			}

			public RelativeTime(SUTime.TemporalOp tempOp, SUTime.Temporal tempArg, int flags)
			{
				this.tempOp = tempOp;
				this.tempArg = tempArg;
				this.opFlags = flags;
			}

			public RelativeTime(SUTime.Duration offset)
				: this(TimeRef, SUTime.TemporalOp.Offset, offset)
			{
			}

			public RelativeTime(SUTime.Time @base, SUTime.Duration offset)
				: this(@base, SUTime.TemporalOp.Offset, offset)
			{
			}

			public RelativeTime(SUTime.Time @base)
				: base(@base)
			{
				this.@base = @base;
			}

			public virtual SUTime.Time GetBase()
			{
				return @base;
			}

			public virtual SUTime.TemporalOp GetTemporalOp()
			{
				return tempOp;
			}

			public virtual SUTime.Temporal GetTemporalArg()
			{
				return tempArg;
			}

			public virtual int GetOpFlags()
			{
				return opFlags;
			}

			public override bool IsGrounded()
			{
				return (@base != null) && @base.IsGrounded();
			}

			// TODO: compute duration/range => uncertainty of this time
			public override SUTime.Duration GetDuration()
			{
				return null;
			}

			public override SUTime.Range GetRange(int flags, SUTime.Duration granularity)
			{
				return new SUTime.Range(this, this);
			}

			public override IDictionary<string, string> GetTimexAttributes(SUTime.TimeIndex timeIndex)
			{
				IDictionary<string, string> map = base.GetTimexAttributes(timeIndex);
				string tfid = GetTfidString(timeIndex);
				map[SUTime.TimexAttr.temporalFunction.ToString()] = "true";
				map[SUTime.TimexAttr.valueFromFunction.ToString()] = tfid;
				if (@base != null)
				{
					map[SUTime.TimexAttr.anchorTimeID.ToString()] = @base.GetTidString(timeIndex);
				}
				return map;
			}

			// / NOTE: This is not ISO or timex standard
			public override string ToFormattedString(int flags)
			{
				if (GetTimeLabel() != null)
				{
					return GetTimeLabel();
				}
				if ((flags & FormatIso) != 0)
				{
					return null;
				}
				// TODO: is there iso standard?
				if ((flags & FormatTimex3Value) != 0)
				{
					return null;
				}
				// TODO: is there timex3 standard?
				StringBuilder sb = new StringBuilder();
				if (@base != null && @base != TimeRef)
				{
					sb.Append(@base.ToFormattedString(flags));
				}
				if (tempOp != null)
				{
					if (sb.Length > 0)
					{
						sb.Append(" ");
					}
					sb.Append(tempOp);
					if (tempArg != null)
					{
						sb.Append(" ").Append(tempArg.ToFormattedString(flags));
					}
				}
				return sb.ToString();
			}

			public override SUTime.Temporal Resolve(SUTime.Time refTime, int flags)
			{
				SUTime.Temporal groundedBase = null;
				if (@base == TimeRef)
				{
					groundedBase = refTime;
				}
				else
				{
					if (@base != null)
					{
						groundedBase = @base.Resolve(refTime, flags);
					}
				}
				if (tempOp != null)
				{
					// NOTE: Should be always safe to resolve and then apply since
					// we will terminate here (no looping hopefully)
					SUTime.Temporal t = tempOp.Apply(groundedBase, tempArg, opFlags);
					if (t != null)
					{
						t = t.AddModApprox(mod, approx);
						return t;
					}
					else
					{
						// NOTE: this can be difficult if applying op
						// gives back same stuff as before
						// Try applying op and then resolving
						t = tempOp.Apply(@base, tempArg, opFlags);
						if (t != null)
						{
							t = t.AddModApprox(mod, approx);
							if (!this.Equals(t))
							{
								return t.Resolve(refTime, flags);
							}
							else
							{
								// Applying op doesn't do much....
								return this;
							}
						}
						else
						{
							return null;
						}
					}
				}
				else
				{
					return (groundedBase != null) ? groundedBase.AddModApprox(mod, approx) : null;
				}
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (o == null || GetType() != o.GetType())
				{
					return false;
				}
				SUTime.RelativeTime that = (SUTime.RelativeTime)o;
				if (opFlags != that.opFlags)
				{
					return false;
				}
				if (@base != null ? !@base.Equals(that.@base) : that.@base != null)
				{
					return false;
				}
				if (tempArg != null ? !tempArg.Equals(that.tempArg) : that.tempArg != null)
				{
					return false;
				}
				if (tempOp != that.tempOp)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = @base != null ? @base.GetHashCode() : 0;
				result = 31 * result + (tempOp != null ? tempOp.GetHashCode() : 0);
				result = 31 * result + (tempArg != null ? tempArg.GetHashCode() : 0);
				result = 31 * result + opFlags;
				return result;
			}

			public override SUTime.Time Add(SUTime.Duration offset)
			{
				SUTime.Time t;
				SUTime.Duration d = offset;
				if (this.tempOp == null)
				{
					t = new SUTime.RelativeTime(@base, d);
					t.approx = this.approx;
					t.mod = this.mod;
				}
				else
				{
					if (this.tempOp == SUTime.TemporalOp.Offset)
					{
						d = ((SUTime.Duration)this.tempArg).Add(offset);
						t = new SUTime.RelativeTime(@base, d);
						t.approx = this.approx;
						t.mod = this.mod;
					}
					else
					{
						t = new SUTime.RelativeTime(this, d);
					}
				}
				return t;
			}

			public override SUTime.Temporal Intersect(SUTime.Temporal t)
			{
				return new SUTime.RelativeTime(this, SUTime.TemporalOp.Intersect, t);
			}

			protected internal override SUTime.Time Intersect(SUTime.Time t)
			{
				if (@base == TimeRef || @base == null)
				{
					if (t is SUTime.PartialTime && tempOp == SUTime.TemporalOp.Offset)
					{
						SUTime.RelativeTime rt = new SUTime.RelativeTime(this, tempOp, tempArg);
						rt.@base = t;
						return rt;
					}
				}
				return new SUTime.RelativeTime(this, SUTime.TemporalOp.Intersect, t);
			}

			private const long serialVersionUID = 1;
		}

		[System.Serializable]
		public class PartialTime : SUTime.Time
		{
			internal Partial @base;

			internal DateTimeZone dateTimeZone;

			public PartialTime(SUTime.Time t, Partial p)
				: base(t)
			{
				// end static class RelativeTime
				// Partial time with Joda Time fields
				// There is typically some uncertainty/imprecision in the time
				// For representing partial absolute time
				// Datetime zone associated with this time
				// private static DateTimeFormatter isoDateFormatter =
				// ISODateTimeFormat.basicDate();
				// private static DateTimeFormatter isoDateTimeFormatter =
				// ISODateTimeFormat.basicDateTimeNoMillis();
				// private static DateTimeFormatter isoTimeFormatter =
				// ISODateTimeFormat.basicTTimeNoMillis();
				// private static DateTimeFormatter isoDateFormatter =
				// ISODateTimeFormat.date();
				// private static DateTimeFormatter isoDateTimeFormatter =
				// ISODateTimeFormat.dateTimeNoMillis();
				// private static DateTimeFormatter isoTimeFormatter =
				// ISODateTimeFormat.tTimeNoMillis();
				if (t is SUTime.PartialTime)
				{
					this.dateTimeZone = ((SUTime.PartialTime)t).dateTimeZone;
				}
				this.@base = p;
			}

			public PartialTime(SUTime.PartialTime pt)
				: base(pt)
			{
				this.dateTimeZone = pt.dateTimeZone;
				this.@base = pt.@base;
			}

			public PartialTime(Partial @base)
			{
				// public PartialTime(Partial base, String mod) { this.base = base; this.mod
				// = mod; }
				this.@base = @base;
			}

			public PartialTime(SUTime.StandardTemporalType temporalType, Partial @base)
			{
				this.@base = @base;
				this.standardTemporalType = temporalType;
			}

			public PartialTime()
			{
			}

			public override SUTime.Temporal SetTimeZone(DateTimeZone tz)
			{
				SUTime.PartialTime tzPt = new SUTime.PartialTime(this, @base);
				tzPt.dateTimeZone = tz;
				return tzPt;
			}

			public override Instant GetJodaTimeInstant()
			{
				return JodaTimeUtils.GetInstant(@base);
			}

			public override Partial GetJodaTimePartial()
			{
				return @base;
			}

			public override bool HasTime()
			{
				if (@base == null)
				{
					return false;
				}
				DateTimeFieldType sdft = JodaTimeUtils.GetMostSpecific(@base);
				if (sdft != null && JodaTimeUtils.IsMoreGeneral(DateTimeFieldType.DayOfMonth(), sdft, @base.GetChronology()))
				{
					return true;
				}
				else
				{
					return false;
				}
			}

			public override SUTime.TimexType GetTimexType()
			{
				if (@base == null)
				{
					return null;
				}
				return base.GetTimexType();
			}

			protected internal virtual bool AppendDateFormats(DateTimeFormatterBuilder builder, int flags)
			{
				bool alwaysPad = ((flags & FormatPadUnknown) != 0);
				bool hasDate = true;
				bool isISO = ((flags & FormatIso) != 0);
				bool isTimex3 = ((flags & FormatTimex3Value) != 0);
				// ERA
				if (JodaTimeUtils.HasField(@base, DateTimeFieldType.Era()))
				{
					int era = @base.Get(DateTimeFieldType.Era());
					if (era == 0)
					{
						builder.AppendLiteral('-');
					}
					else
					{
						if (era == 1)
						{
							builder.AppendLiteral('+');
						}
					}
				}
				// YEAR
				if (JodaTimeUtils.HasField(@base, DateTimeFieldType.CenturyOfEra()) || JodaTimeUtils.HasField(@base, JodaTimeUtils.DecadeOfCentury) || JodaTimeUtils.HasField(@base, DateTimeFieldType.YearOfCentury()))
				{
					if (JodaTimeUtils.HasField(@base, DateTimeFieldType.CenturyOfEra()))
					{
						builder.AppendCenturyOfEra(2, 2);
					}
					else
					{
						builder.AppendLiteral(PadFieldUnknown2);
					}
					if (JodaTimeUtils.HasField(@base, JodaTimeUtils.DecadeOfCentury))
					{
						builder.AppendDecimal(JodaTimeUtils.DecadeOfCentury, 1, 1);
						builder.AppendLiteral(PadFieldUnknown);
					}
					else
					{
						if (JodaTimeUtils.HasField(@base, DateTimeFieldType.YearOfCentury()))
						{
							builder.AppendYearOfCentury(2, 2);
						}
						else
						{
							builder.AppendLiteral(PadFieldUnknown2);
						}
					}
				}
				else
				{
					if (JodaTimeUtils.HasField(@base, DateTimeFieldType.Year()))
					{
						builder.AppendYear(4, 4);
					}
					else
					{
						if (JodaTimeUtils.HasField(@base, DateTimeFieldType.Weekyear()))
						{
							builder.AppendWeekyear(4, 4);
						}
						else
						{
							builder.AppendLiteral(PadFieldUnknown4);
							hasDate = false;
						}
					}
				}
				// Decide whether to include HALF, QUARTER, MONTH/DAY, or WEEK/WEEKDAY
				bool appendHalf = false;
				bool appendQuarter = false;
				bool appendMonthDay = false;
				bool appendWeekDay = false;
				if (isISO || isTimex3)
				{
					if (JodaTimeUtils.HasField(@base, DateTimeFieldType.MonthOfYear()) && JodaTimeUtils.HasField(@base, DateTimeFieldType.DayOfMonth()))
					{
						appendMonthDay = true;
					}
					else
					{
						if (JodaTimeUtils.HasField(@base, DateTimeFieldType.WeekOfWeekyear()) || JodaTimeUtils.HasField(@base, DateTimeFieldType.DayOfWeek()))
						{
							appendWeekDay = true;
						}
						else
						{
							if (JodaTimeUtils.HasField(@base, DateTimeFieldType.MonthOfYear()) || JodaTimeUtils.HasField(@base, DateTimeFieldType.DayOfMonth()))
							{
								appendMonthDay = true;
							}
							else
							{
								if (JodaTimeUtils.HasField(@base, JodaTimeUtils.QuarterOfYear))
								{
									if (!isISO)
									{
										appendQuarter = true;
									}
								}
								else
								{
									if (JodaTimeUtils.HasField(@base, JodaTimeUtils.HalfYearOfYear))
									{
										if (!isISO)
										{
											appendHalf = true;
										}
									}
								}
							}
						}
					}
				}
				else
				{
					appendHalf = true;
					appendQuarter = true;
					appendMonthDay = true;
					appendWeekDay = true;
				}
				// Half - Not ISO standard
				if (appendHalf && JodaTimeUtils.HasField(@base, JodaTimeUtils.HalfYearOfYear))
				{
					builder.AppendLiteral("-H");
					builder.AppendDecimal(JodaTimeUtils.HalfYearOfYear, 1, 1);
				}
				// Quarter  - Not ISO standard
				if (appendQuarter && JodaTimeUtils.HasField(@base, JodaTimeUtils.QuarterOfYear))
				{
					builder.AppendLiteral("-Q");
					builder.AppendDecimal(JodaTimeUtils.QuarterOfYear, 1, 1);
				}
				// MONTH
				if (appendMonthDay && (JodaTimeUtils.HasField(@base, DateTimeFieldType.MonthOfYear()) || JodaTimeUtils.HasField(@base, DateTimeFieldType.DayOfMonth())))
				{
					hasDate = true;
					builder.AppendLiteral('-');
					if (JodaTimeUtils.HasField(@base, DateTimeFieldType.MonthOfYear()))
					{
						builder.AppendMonthOfYear(2);
					}
					else
					{
						builder.AppendLiteral(PadFieldUnknown2);
					}
					// Don't indicate day of month if not specified
					if (JodaTimeUtils.HasField(@base, DateTimeFieldType.DayOfMonth()))
					{
						builder.AppendLiteral('-');
						builder.AppendDayOfMonth(2);
					}
					else
					{
						if (alwaysPad)
						{
							builder.AppendLiteral(PadFieldUnknown2);
						}
					}
				}
				if (appendWeekDay && (JodaTimeUtils.HasField(@base, DateTimeFieldType.WeekOfWeekyear()) || JodaTimeUtils.HasField(@base, DateTimeFieldType.DayOfWeek())))
				{
					hasDate = true;
					builder.AppendLiteral("-W");
					if (JodaTimeUtils.HasField(@base, DateTimeFieldType.WeekOfWeekyear()))
					{
						builder.AppendWeekOfWeekyear(2);
					}
					else
					{
						builder.AppendLiteral(PadFieldUnknown2);
					}
					// Don't indicate the day of the week if not specified
					if (JodaTimeUtils.HasField(@base, DateTimeFieldType.DayOfWeek()))
					{
						builder.AppendLiteral("-");
						builder.AppendDayOfWeek(1);
					}
				}
				return hasDate;
			}

			protected internal virtual bool AppendTimeFormats(DateTimeFormatterBuilder builder, int flags)
			{
				bool alwaysPad = ((flags & FormatPadUnknown) != 0);
				bool hasTime = HasTime();
				DateTimeFieldType sdft = JodaTimeUtils.GetMostSpecific(@base);
				if (hasTime)
				{
					builder.AppendLiteral("T");
					if (JodaTimeUtils.HasField(@base, DateTimeFieldType.HourOfDay()))
					{
						builder.AppendHourOfDay(2);
					}
					else
					{
						if (JodaTimeUtils.HasField(@base, DateTimeFieldType.ClockhourOfDay()))
						{
							builder.AppendClockhourOfDay(2);
						}
						else
						{
							builder.AppendLiteral(PadFieldUnknown2);
						}
					}
					if (JodaTimeUtils.HasField(@base, DateTimeFieldType.MinuteOfHour()))
					{
						builder.AppendLiteral(":");
						builder.AppendMinuteOfHour(2);
					}
					else
					{
						if (alwaysPad || JodaTimeUtils.IsMoreGeneral(DateTimeFieldType.MinuteOfHour(), sdft, @base.GetChronology()))
						{
							builder.AppendLiteral(":");
							builder.AppendLiteral(PadFieldUnknown2);
						}
					}
					if (JodaTimeUtils.HasField(@base, DateTimeFieldType.SecondOfMinute()))
					{
						builder.AppendLiteral(":");
						builder.AppendSecondOfMinute(2);
					}
					else
					{
						if (alwaysPad || JodaTimeUtils.IsMoreGeneral(DateTimeFieldType.SecondOfMinute(), sdft, @base.GetChronology()))
						{
							builder.AppendLiteral(":");
							builder.AppendLiteral(PadFieldUnknown2);
						}
					}
					if (JodaTimeUtils.HasField(@base, DateTimeFieldType.MillisOfSecond()))
					{
						builder.AppendLiteral(".");
						builder.AppendMillisOfSecond(3);
					}
				}
				// builder.append(isoTimeFormatter);
				return hasTime;
			}

			protected internal virtual DateTimeFormatter GetFormatter(int flags)
			{
				DateTimeFormatterBuilder builder = new DateTimeFormatterBuilder();
				bool hasDate = AppendDateFormats(builder, flags);
				bool hasTime = HasTime();
				if (hasTime)
				{
					if (!hasDate)
					{
						builder.Clear();
					}
					AppendTimeFormats(builder, flags);
				}
				return builder.ToFormatter();
			}

			public override bool IsGrounded()
			{
				return false;
			}

			// TODO: compute duration/range => uncertainty of this time
			public override SUTime.Duration GetDuration()
			{
				/*      TimeLabel tl = getTimeLabel();
				if (tl != null) {
				return tl.getDuration();
				} */
				SUTime.StandardTemporalType tlt = GetStandardTemporalType();
				if (tlt != null)
				{
					return tlt.GetDuration();
				}
				return SUTime.Duration.GetDuration(JodaTimeUtils.GetJodaTimePeriod(@base));
			}

			public override SUTime.Range GetRange(int flags, SUTime.Duration inputGranularity)
			{
				SUTime.Duration d = GetDuration();
				if (d != null)
				{
					int padType = (flags & RangeFlagsPadMask);
					SUTime.Time start = this;
					SUTime.Duration granularity = inputGranularity;
					switch (padType)
					{
						case RangeFlagsPadNone:
						{
							// The most basic range
							start = this;
							break;
						}

						case RangeFlagsPadAuto:
						{
							// More complex range
							if (HasTime())
							{
								granularity = SUTime.Millis;
							}
							else
							{
								granularity = SUTime.Day;
							}
							start = PadMoreSpecificFields(granularity);
							break;
						}

						case RangeFlagsPadFinest:
						{
							granularity = SUTime.Millis;
							start = PadMoreSpecificFields(granularity);
							break;
						}

						case RangeFlagsPadSpecified:
						{
							start = PadMoreSpecificFields(granularity);
							break;
						}

						default:
						{
							throw new NotSupportedException("Unsupported pad type for getRange: " + flags);
						}
					}
					if (start is SUTime.PartialTime)
					{
						((SUTime.PartialTime)start).WithStandardFields();
					}
					SUTime.Time end = start.Add(d);
					if (granularity != null)
					{
						end = end.Subtract(granularity);
					}
					return new SUTime.Range(start, end, d);
				}
				else
				{
					return new SUTime.Range(this, this);
				}
			}

			protected internal virtual void WithStandardFields()
			{
				if (@base.IsSupported(DateTimeFieldType.DayOfWeek()))
				{
					@base = JodaTimeUtils.ResolveDowToDay(@base);
				}
				else
				{
					if (@base.IsSupported(DateTimeFieldType.MonthOfYear()) && @base.IsSupported(DateTimeFieldType.DayOfMonth()))
					{
						if (@base.IsSupported(DateTimeFieldType.WeekOfWeekyear()))
						{
							@base = @base.Without(DateTimeFieldType.WeekOfWeekyear());
						}
						if (@base.IsSupported(DateTimeFieldType.DayOfWeek()))
						{
							@base = @base.Without(DateTimeFieldType.DayOfWeek());
						}
					}
				}
			}

			public override SUTime.Time ReduceGranularityTo(SUTime.Duration granularity)
			{
				Partial pbase = @base;
				if (JodaTimeUtils.HasField(granularity.GetJodaTimePeriod(), DurationFieldType.Weeks()))
				{
					// Make sure the partial time has weeks in it
					if (!JodaTimeUtils.HasField(pbase, DateTimeFieldType.WeekOfWeekyear()))
					{
						// Add week year to it
						pbase = JodaTimeUtils.ResolveWeek(pbase);
					}
				}
				Partial p = JodaTimeUtils.DiscardMoreSpecificFields(pbase, JodaTimeUtils.GetMostSpecific(granularity.GetJodaTimePeriod()));
				return new SUTime.PartialTime(this, p);
			}

			public virtual SUTime.PartialTime PadMoreSpecificFields(SUTime.Duration granularity)
			{
				Period period = null;
				if (granularity != null)
				{
					period = granularity.GetJodaTimePeriod();
				}
				Partial p = JodaTimeUtils.PadMoreSpecificFields(@base, period);
				return new SUTime.PartialTime(this, p);
			}

			public override string ToFormattedString(int flags)
			{
				if (GetTimeLabel() != null)
				{
					return GetTimeLabel();
				}
				string s;
				// Initialized below
				if (@base != null)
				{
					// String s = ISODateTimeFormat.basicDateTime().print(base);
					// return s.replace('\ufffd', 'X');
					DateTimeFormatter formatter = GetFormatter(flags);
					s = formatter.Print(@base);
				}
				else
				{
					s = "XXXX-XX-XX";
				}
				if (dateTimeZone != null)
				{
					DateTimeFormatter formatter = DateTimeFormat.ForPattern("Z");
					formatter = formatter.WithZone(dateTimeZone);
					s = s + formatter.Print(0);
				}
				return s;
			}

			public override SUTime.Temporal Resolve(SUTime.Time @ref, int flags)
			{
				if (@ref == null || @ref == TimeUnknown || @ref == TimeRef)
				{
					return this;
				}
				if (this == TimeRef)
				{
					return @ref;
				}
				if (this == TimeUnknown)
				{
					return this;
				}
				Partial partialRef = @ref.GetJodaTimePartial();
				if (partialRef == null)
				{
					throw new NotSupportedException("Cannot resolve if reftime is of class: " + @ref.GetType());
				}
				Partial p = (@base != null) ? JodaTimeUtils.CombineMoreGeneralFields(@base, partialRef) : partialRef;
				p = JodaTimeUtils.ResolveDowToDay(p, partialRef);
				SUTime.Time resolved;
				if (p == @base)
				{
					resolved = this;
				}
				else
				{
					resolved = new SUTime.PartialTime(this, p);
				}
				// log.info("Resolved " + this + " to " + resolved + ", ref=" + ref);
				SUTime.Duration resolvedGranularity = resolved.GetGranularity();
				SUTime.Duration refGranularity = @ref.GetGranularity();
				// log.info("refGranularity is " + refGranularity);
				// log.info("resolvedGranularity is " + resolvedGranularity);
				if (resolvedGranularity != null && refGranularity != null && resolvedGranularity.CompareTo(refGranularity) >= 0)
				{
					if ((flags & ResolveToPast) != 0)
					{
						if (resolved.CompareTo(@ref) > 0)
						{
							SUTime.Time t = (SUTime.Time)this.Prev();
							if (t != null)
							{
								resolved = (SUTime.Time)t.Resolve(@ref, 0);
							}
						}
					}
					else
					{
						// log.info("Resolved " + this + " to past " + resolved + ", ref=" + ref);
						if ((flags & ResolveToFuture) != 0)
						{
							if (resolved.CompareTo(@ref) < 0)
							{
								SUTime.Time t = (SUTime.Time)this.Next();
								if (t != null)
								{
									resolved = (SUTime.Time)t.Resolve(@ref, 0);
								}
							}
						}
						else
						{
							// log.info("Resolved " + this + " to future " + resolved + ", ref=" + ref);
							if ((flags & ResolveToClosest) != 0)
							{
								if (resolved.CompareTo(@ref) > 0)
								{
									SUTime.Time t = (SUTime.Time)this.Prev();
									if (t != null)
									{
										SUTime.Time resolved2 = (SUTime.Time)t.Resolve(@ref, 0);
										resolved = SUTime.Time.Closest(@ref, resolved, resolved2);
									}
								}
								if (resolved.CompareTo(@ref) < 0)
								{
									SUTime.Time t = (SUTime.Time)this.Next();
									if (t != null)
									{
										SUTime.Time resolved2 = (SUTime.Time)t.Resolve(@ref, 0);
										resolved = SUTime.Time.Closest(@ref, resolved, resolved2);
									}
								}
							}
						}
					}
				}
				// log.info("Resolved " + this + " to closest " + resolved + ", ref=" + ref);
				return resolved;
			}

			public virtual bool IsCompatible(SUTime.PartialTime time)
			{
				return JodaTimeUtils.IsCompatible(@base, time.@base);
			}

			public static Pair<SUTime.PartialTime, SUTime.PartialTime> GetCompatible(SUTime.PartialTime t1, SUTime.PartialTime t2)
			{
				// Incompatible timezones
				if (t1.dateTimeZone != null && t2.dateTimeZone != null && !t1.dateTimeZone.Equals(t2.dateTimeZone))
				{
					return null;
				}
				if (t1.IsCompatible(t2))
				{
					return Pair.MakePair(t1, t2);
				}
				if (t1.uncertaintyGranularity != null && t2.uncertaintyGranularity == null)
				{
					if (t1.uncertaintyGranularity.CompareTo(t2.GetDuration()) > 0)
					{
						// Drop the uncertain fields from t1
						SUTime.Duration d = t1.uncertaintyGranularity;
						SUTime.PartialTime t1b = ((SUTime.PartialTime)t1.ReduceGranularityTo(d));
						if (t1b.IsCompatible(t2))
						{
							return Pair.MakePair(t1b, t2);
						}
					}
				}
				else
				{
					if (t1.uncertaintyGranularity == null && t2.uncertaintyGranularity != null)
					{
						if (t2.uncertaintyGranularity.CompareTo(t1.GetDuration()) > 0)
						{
							// Drop the uncertain fields from t2
							SUTime.Duration d = t2.uncertaintyGranularity;
							SUTime.PartialTime t2b = ((SUTime.PartialTime)t2.ReduceGranularityTo(d));
							if (t1.IsCompatible(t2b))
							{
								return Pair.MakePair(t1, t2b);
							}
						}
					}
					else
					{
						if (t1.uncertaintyGranularity != null && t2.uncertaintyGranularity != null)
						{
							SUTime.Duration d1 = SUTime.Duration.Max(t1.uncertaintyGranularity, t2.GetDuration());
							SUTime.Duration d2 = SUTime.Duration.Max(t2.uncertaintyGranularity, t1.GetDuration());
							SUTime.PartialTime t1b = ((SUTime.PartialTime)t1.ReduceGranularityTo(d1));
							SUTime.PartialTime t2b = ((SUTime.PartialTime)t2.ReduceGranularityTo(d2));
							if (t1b.IsCompatible(t2b))
							{
								return Pair.MakePair(t1b, t2b);
							}
						}
					}
				}
				return null;
			}

			public override SUTime.Duration GetPeriod()
			{
				/*    TimeLabel tl = getTimeLabel();
				if (tl != null) {
				return tl.getPeriod();
				} */
				SUTime.StandardTemporalType tlt = GetStandardTemporalType();
				if (tlt != null)
				{
					return tlt.GetPeriod();
				}
				if (@base == null)
				{
					return null;
				}
				DateTimeFieldType mostGeneral = JodaTimeUtils.GetMostGeneral(@base);
				DurationFieldType df = mostGeneral.GetRangeDurationType();
				// if (df == null) {
				// df = mostGeneral.getDurationType();
				// }
				if (df != null)
				{
					try
					{
						return new SUTime.DurationWithFields(new Period().WithField(df, 1));
					}
					catch (Exception)
					{
					}
				}
				// TODO: Do something intelligent here
				return null;
			}

			public virtual IList<SUTime.Temporal> ToList()
			{
				if (JodaTimeUtils.HasField(@base, DateTimeFieldType.Year()) && JodaTimeUtils.HasField(@base, DateTimeFieldType.MonthOfYear()) && JodaTimeUtils.HasField(@base, DateTimeFieldType.DayOfWeek()))
				{
					IList<SUTime.Temporal> list = new List<SUTime.Temporal>();
					Partial pt = new Partial();
					pt = JodaTimeUtils.SetField(pt, DateTimeFieldType.Year(), @base.Get(DateTimeFieldType.Year()));
					pt = JodaTimeUtils.SetField(pt, DateTimeFieldType.MonthOfYear(), @base.Get(DateTimeFieldType.MonthOfYear()));
					pt = JodaTimeUtils.SetField(pt, DateTimeFieldType.DayOfMonth(), 1);
					Partial candidate = JodaTimeUtils.ResolveDowToDay(@base, pt);
					if (candidate.Get(DateTimeFieldType.MonthOfYear()) != @base.Get(DateTimeFieldType.MonthOfYear()))
					{
						pt = JodaTimeUtils.SetField(pt, DateTimeFieldType.DayOfMonth(), 8);
						candidate = JodaTimeUtils.ResolveDowToDay(@base, pt);
						if (candidate.Get(DateTimeFieldType.MonthOfYear()) != @base.Get(DateTimeFieldType.MonthOfYear()))
						{
							// give up
							return null;
						}
					}
					try
					{
						while (candidate.Get(DateTimeFieldType.MonthOfYear()) == @base.Get(DateTimeFieldType.MonthOfYear()))
						{
							list.Add(new SUTime.PartialTime(this, candidate));
							pt = JodaTimeUtils.SetField(pt, DateTimeFieldType.DayOfMonth(), pt.Get(DateTimeFieldType.DayOfMonth()) + 7);
							candidate = JodaTimeUtils.ResolveDowToDay(@base, pt);
						}
					}
					catch (IllegalFieldValueException)
					{
					}
					return list;
				}
				else
				{
					return null;
				}
			}

			protected internal override SUTime.Time Intersect(SUTime.Time t)
			{
				if (t == null || t == TimeUnknown)
				{
					return this;
				}
				if (@base == null)
				{
					if (dateTimeZone != null)
					{
						return (SUTime.Time)t.SetTimeZone(dateTimeZone);
					}
					else
					{
						return t;
					}
				}
				if (t is SUTime.CompositePartialTime)
				{
					return t.Intersect(this);
				}
				else
				{
					if (t is SUTime.PartialTime)
					{
						Pair<SUTime.PartialTime, SUTime.PartialTime> compatible = GetCompatible(this, (SUTime.PartialTime)t);
						if (compatible == null)
						{
							return null;
						}
						Partial p = JodaTimeUtils.Combine(compatible.first.@base, compatible.second.@base);
						// Take timezone if there is one
						DateTimeZone dtz = (dateTimeZone != null) ? dateTimeZone : ((SUTime.PartialTime)t).dateTimeZone;
						SUTime.PartialTime res = new SUTime.PartialTime(p);
						if (dtz != null)
						{
							return ((SUTime.PartialTime)res.SetTimeZone(dtz));
						}
						else
						{
							return res;
						}
					}
					else
					{
						if (t is SUTime.OrdinalTime)
						{
							SUTime.Temporal temp = t.Resolve(this);
							if (temp is SUTime.PartialTime)
							{
								return (SUTime.Time)temp;
							}
							else
							{
								return t.Intersect(this);
							}
						}
						else
						{
							if (t is SUTime.GroundedTime)
							{
								return t.Intersect(this);
							}
							else
							{
								if (t is SUTime.RelativeTime)
								{
									return t.Intersect(this);
								}
								else
								{
									SUTime.Time cpt = MakeComposite(this, t);
									if (cpt != null)
									{
										return cpt;
									}
									if (t is SUTime.InexactTime)
									{
										return t.Intersect(this);
									}
								}
							}
						}
					}
				}
				return null;
			}

			// return new RelativeTime(this, TemporalOp.INTERSECT, t);
			/*public Temporal intersect(Temporal t) {
			if (t == null)
			return this;
			if (t == TIME_UNKNOWN || t == DURATION_UNKNOWN)
			return this;
			if (base == null)
			return t;
			if (t instanceof Time) {
			return intersect((Time) t);
			} else if (t instanceof Range) {
			return t.intersect(this);
			} else if (t instanceof Duration) {
			return new RelativeTime(this, TemporalOp.INTERSECT, t);
			}
			return null;
			}        */
			protected internal virtual SUTime.PartialTime AddSupported(Period p, int scalar)
			{
				return new SUTime.PartialTime(@base.WithPeriodAdded(p, scalar));
			}

			protected internal virtual SUTime.PartialTime AddUnsupported(Period p, int scalar)
			{
				return new SUTime.PartialTime(this, JodaTimeUtils.AddForce(@base, p, scalar));
			}

			public override SUTime.Time Add(SUTime.Duration offset)
			{
				if (@base == null)
				{
					return this;
				}
				Period per = offset.GetJodaTimePeriod();
				SUTime.PartialTime p = AddSupported(per, 1);
				Period unsupported = JodaTimeUtils.GetUnsupportedDurationPeriod(p.@base, per);
				SUTime.Time t = p;
				if (unsupported != null)
				{
					if (JodaTimeUtils.HasField(unsupported, DurationFieldType.Weeks()) && JodaTimeUtils.HasField(p.@base, DateTimeFieldType.Year()) && JodaTimeUtils.HasField(p.@base, DateTimeFieldType.MonthOfYear()) && JodaTimeUtils.HasField(p.@base, DateTimeFieldType
						.DayOfMonth()))
					{
						/*unsupported.size() == 1 && */
						// What if there are other unsupported fields...
						t = p.AddUnsupported(per, 1);
					}
					else
					{
						if (JodaTimeUtils.HasField(unsupported, DurationFieldType.Months()) && unsupported.GetMonths() % 3 == 0 && JodaTimeUtils.HasField(p.@base, JodaTimeUtils.QuarterOfYear))
						{
							Partial p2 = p.@base.WithFieldAddWrapped(JodaTimeUtils.Quarters, unsupported.GetMonths() / 3);
							p = new SUTime.PartialTime(p, p2);
							unsupported = unsupported.WithMonths(0);
						}
						if (JodaTimeUtils.HasField(unsupported, DurationFieldType.Months()) && unsupported.GetMonths() % 6 == 0 && JodaTimeUtils.HasField(p.@base, JodaTimeUtils.HalfYearOfYear))
						{
							Partial p2 = p.@base.WithFieldAddWrapped(JodaTimeUtils.HalfYears, unsupported.GetMonths() / 6);
							p = new SUTime.PartialTime(p, p2);
							unsupported = unsupported.WithMonths(0);
						}
						if (JodaTimeUtils.HasField(unsupported, DurationFieldType.Years()) && unsupported.GetYears() % 10 == 0 && JodaTimeUtils.HasField(p.@base, JodaTimeUtils.DecadeOfCentury))
						{
							Partial p2 = p.@base.WithFieldAddWrapped(JodaTimeUtils.Decades, unsupported.GetYears() / 10);
							p = new SUTime.PartialTime(p, p2);
							unsupported = unsupported.WithYears(0);
						}
						if (JodaTimeUtils.HasField(unsupported, DurationFieldType.Years()) && unsupported.GetYears() % 100 == 0 && JodaTimeUtils.HasField(p.@base, DateTimeFieldType.CenturyOfEra()))
						{
							Partial p2 = p.@base.WithField(DateTimeFieldType.CenturyOfEra(), p.@base.Get(DateTimeFieldType.CenturyOfEra()) + unsupported.GetYears() / 100);
							p = new SUTime.PartialTime(p, p2);
							unsupported = unsupported.WithYears(0);
						}
						//          if (unsupported.getDays() != 0 && !JodaTimeUtils.hasField(p.base, DateTimeFieldType.dayOfYear()) && !JodaTimeUtils.hasField(p.base, DateTimeFieldType.dayOfMonth())
						//              && !JodaTimeUtils.hasField(p.base, DateTimeFieldType.dayOfWeek()) && JodaTimeUtils.hasField(p.base, DateTimeFieldType.monthOfYear())) {
						//            if (p.getGranularity().compareTo(DAY) <= 0) {
						//              // We are granular enough for this
						//              Partial p2 = p.base.with(DateTimeFieldType.dayOfMonth(), unsupported.getDays());
						//              p = new PartialTime(p, p2);
						//              unsupported = unsupported.withDays(0);
						//            }
						//          }
						if (!unsupported.Equals(Period.Zero))
						{
							t = new SUTime.RelativeTime(p, new SUTime.DurationWithFields(unsupported));
							t.approx = this.approx;
							t.mod = this.mod;
						}
						else
						{
							t = p;
						}
					}
				}
				return t;
			}

			private const long serialVersionUID = 1;
		}

		public const int EraBc = 0;

		public const int EraAd = 1;

		public const int EraUnknown = -1;

		[System.Serializable]
		public class IsoDate : SUTime.PartialTime
		{
			/// <summary>Era: BC is era 0, AD is era 1, Unknown is -1</summary>
			public int era = EraUnknown;

			/// <summary>Year of Era</summary>
			public int year = -1;

			/// <summary>Month of Year</summary>
			public int month = -1;

			/// <summary>Day of Month</summary>
			public int day = -1;

			public IsoDate(int y, int m, int d)
				: this(null, y, m, d)
			{
			}

			public IsoDate(SUTime.StandardTemporalType temporalType, int y, int m, int d)
			{
				/*
				* This is mostly a helper class but it is also the most standard type of date that people are
				* used to working with.
				*/
				// TODO: We are also using this class for partial dates
				//       with just decade or century, but it is difficult
				//       to get that information out without using the underlying joda classes
				this.year = y;
				this.month = m;
				this.day = d;
				InitBase();
				this.standardTemporalType = temporalType;
			}

			public IsoDate(Number y, Number m, Number d)
				: this(y, m, d, null, null)
			{
			}

			public IsoDate(Number y, Number m, Number d, Number era, bool yearEraAdjustNeeded)
			{
				// TODO: Added for grammar parsing
				this.year = (y != null) ? y : -1;
				this.month = (m != null) ? m : -1;
				this.day = (d != null) ? d : -1;
				this.era = (era != null) ? era : EraUnknown;
				if (yearEraAdjustNeeded != null && yearEraAdjustNeeded && this.era == EraBc)
				{
					if (this.year > 0)
					{
						this.year--;
					}
				}
				InitBase();
			}

			public IsoDate(string y, string m, string d)
			{
				// Assumes y, m, d are ISO formatted
				if (y != null && !PadFieldUnknown4.Equals(y))
				{
					if (!y.Matches("[+-]?[0-9X]{4}"))
					{
						throw new ArgumentException("Year not in ISO format " + y);
					}
					if (y.StartsWith("-"))
					{
						y = Sharpen.Runtime.Substring(y, 1);
						era = EraBc;
					}
					else
					{
						// BC
						if (y.StartsWith("+"))
						{
							y = Sharpen.Runtime.Substring(y, 1);
							era = EraAd;
						}
					}
					// AD
					if (y.Contains(PadFieldUnknown))
					{
					}
					else
					{
						year = System.Convert.ToInt32(y);
					}
				}
				else
				{
					y = PadFieldUnknown4;
				}
				if (m != null && !PadFieldUnknown2.Equals(m))
				{
					month = System.Convert.ToInt32(m);
				}
				else
				{
					m = PadFieldUnknown2;
				}
				if (d != null && !PadFieldUnknown2.Equals(d))
				{
					day = System.Convert.ToInt32(d);
				}
				else
				{
					d = PadFieldUnknown2;
				}
				InitBase();
				if (year < 0 && !PadFieldUnknown4.Equals(y))
				{
					if (char.IsDigit(y[0]) && char.IsDigit(y[1]))
					{
						int century = System.Convert.ToInt32(Sharpen.Runtime.Substring(y, 0, 2));
						@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.CenturyOfEra(), century);
					}
					if (char.IsDigit(y[2]) && char.IsDigit(y[3]))
					{
						int cy = System.Convert.ToInt32(Sharpen.Runtime.Substring(y, 2, 4));
						@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.YearOfCentury(), cy);
					}
					else
					{
						if (char.IsDigit(y[2]))
						{
							int decade = System.Convert.ToInt32(Sharpen.Runtime.Substring(y, 2, 3));
							@base = JodaTimeUtils.SetField(@base, JodaTimeUtils.DecadeOfCentury, decade);
						}
					}
				}
			}

			private void InitBase()
			{
				if (era >= 0)
				{
					@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.Era(), era);
				}
				if (year >= 0)
				{
					@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.Year(), year);
				}
				if (month >= 0)
				{
					@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.MonthOfYear(), month);
				}
				if (day >= 0)
				{
					@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.DayOfMonth(), day);
				}
			}

			public override string ToString()
			{
				// TODO: is the right way to print this object?
				StringBuilder os = new StringBuilder();
				if (era == EraBc)
				{
					os.Append("-");
				}
				else
				{
					if (era == EraAd)
					{
						os.Append("+");
					}
				}
				if (year >= 0)
				{
					os.Append(year);
				}
				else
				{
					os.Append("XXXX");
				}
				os.Append("-");
				if (month >= 0)
				{
					os.Append(month);
				}
				else
				{
					os.Append("XX");
				}
				os.Append("-");
				if (day >= 0)
				{
					os.Append(day);
				}
				else
				{
					os.Append("XX");
				}
				return os.ToString();
			}

			public virtual int GetYear()
			{
				return year;
			}

			// TODO: Should we allow setters??? Most time classes are immutable
			public virtual void SetYear(int y)
			{
				this.year = y;
				InitBase();
			}

			public virtual int GetMonth()
			{
				return month;
			}

			// TODO: Should we allow setters??? Most time classes are immutable
			public virtual void SetMonth(int m)
			{
				this.month = m;
				InitBase();
			}

			public virtual int GetDay()
			{
				return day;
			}

			// TODO: Should we allow setters??? Most time classes are immutable
			public virtual void SetDay(int d)
			{
				this.day = d;
				InitBase();
			}

			// TODO: Should we allow setters??? Most time classes are immutable
			public virtual void SetDate(int y, int m, int d)
			{
				this.year = y;
				this.month = m;
				this.day = d;
				InitBase();
			}

			private const long serialVersionUID = 1;
		}

		public const int HalfdayAm = 0;

		public const int HalfdayPm = 1;

		public const int HalfdayUnknown = -1;

		[System.Serializable]
		protected internal class IsoTime : SUTime.PartialTime
		{
			public int hour = -1;

			public int minute = -1;

			public int second = -1;

			public int millis = -1;

			public int halfday = HalfdayUnknown;

			public IsoTime(int h, int m, int s)
				: this(h, m, s, -1, -1)
			{
			}

			public IsoTime(Number h, Number m, Number s)
				: this(h, m, s, null, null)
			{
			}

			public IsoTime(int h, int m, int s, int ms, int halfday)
			{
				// Helper time class
				// 0 = am, 1 = pm
				// TODO: Added for reading types from file
				this.hour = h;
				this.minute = m;
				this.second = s;
				this.millis = ms;
				this.halfday = halfday;
				// Some error checks
				second += millis / 1000;
				millis = millis % 1000;
				minute += second / 60;
				second = second % 60;
				hour += hour / 60;
				minute = minute % 60;
				// Error checks done
				InitBase();
			}

			public IsoTime(Number h, Number m, Number s, Number ms, Number halfday)
				: this((h != null) ? h : -1, (m != null) ? m : -1, (s != null) ? s : -1, (ms != null) ? ms : -1, (halfday != null) ? halfday : -1)
			{
			}

			public IsoTime(string h, string m, string s)
				: this(h, m, s, null)
			{
			}

			public IsoTime(string h, string m, string s, string ms)
			{
				// TODO: Added for reading types from file
				if (h != null)
				{
					hour = System.Convert.ToInt32(h);
				}
				if (m != null)
				{
					minute = System.Convert.ToInt32(m);
				}
				if (s != null)
				{
					second = System.Convert.ToInt32(s);
				}
				if (ms != null)
				{
					millis = System.Convert.ToInt32(s);
				}
				InitBase();
			}

			public override bool HasTime()
			{
				return true;
			}

			private void InitBase()
			{
				if (hour >= 0)
				{
					if (hour < 24)
					{
						@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.HourOfDay(), hour);
					}
					else
					{
						@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.ClockhourOfDay(), hour);
					}
				}
				if (minute >= 0)
				{
					@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.MinuteOfHour(), minute);
				}
				if (second >= 0)
				{
					@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.SecondOfMinute(), second);
				}
				if (millis >= 0)
				{
					@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.MillisOfSecond(), millis);
				}
				if (halfday >= 0)
				{
					@base = JodaTimeUtils.SetField(@base, DateTimeFieldType.HalfdayOfDay(), halfday);
				}
			}

			private const long serialVersionUID = 1;
		}

		[System.Serializable]
		protected internal class IsoDateTime : SUTime.PartialTime
		{
			private readonly SUTime.IsoDate date;

			private readonly SUTime.IsoTime time;

			public IsoDateTime(SUTime.IsoDate date, SUTime.IsoTime time)
			{
				this.date = date;
				this.time = time;
				@base = JodaTimeUtils.Combine(date.@base, time.@base);
			}

			public override bool HasTime()
			{
				return (time != null);
			}

			private const long serialVersionUID = 1;
			/*    public String toISOString()
			{
			return date.toISOString() + time.toISOString();
			}  */
		}

		private static readonly Pattern PatternIso = Pattern.Compile("(\\d\\d\\d\\d)-?(\\d\\d?)-?(\\d\\d?)(-?(?:T(\\d\\d):?(\\d\\d)?:?(\\d\\d)?(?:[.,](\\d{1,3}))?([+-]\\d\\d:?\\d\\d)?))?");

		private static readonly Pattern PatternIsoDatetime = Pattern.Compile("(\\d\\d\\d\\d)(\\d\\d)(\\d\\d):(\\d\\d)(\\d\\d)");

		private static readonly Pattern PatternIsoTime = Pattern.Compile("T(\\d\\d):?(\\d\\d)?:?(\\d\\d)?(?:[.,](\\d{1,3}))?([+-]\\d\\d:?\\d\\d)?");

		private static readonly Pattern PatternIsoDate1 = Pattern.Compile(".*(\\d\\d\\d\\d)\\/(\\d\\d?)\\/(\\d\\d?).*");

		private static readonly Pattern PatternIsoDate2 = Pattern.Compile(".*(\\d\\d\\d\\d)\\-(\\d\\d?)\\-(\\d\\d?).*");

		private static readonly Pattern PatternIsoDatePartial = Pattern.Compile("([0-9X]{4})[-]?([0-9X][0-9X])[-]?([0-9X][0-9X])");

		private static readonly Pattern PatternIsoAmbiguous1 = Pattern.Compile(".*(\\d\\d?)\\/(\\d\\d?)\\/(\\d\\d(\\d\\d)?).*");

		private static readonly Pattern PatternIsoAmbiguous2 = Pattern.Compile(".*(\\d\\d?)\\-(\\d\\d?)\\-(\\d\\d(\\d\\d)?).*");

		private static readonly Pattern PatternIsoAmbiguous3 = Pattern.Compile(".*(\\d\\d?)\\.(\\d\\d?)\\.(\\d\\d(\\d\\d)?).*");

		private static readonly Pattern PatternIsoTimeOfDay = Pattern.Compile(".*(\\d?\\d):(\\d\\d)(:(\\d\\d)(\\.\\d+)?)?(\\s*([AP])\\.?M\\.?)?(\\s+([+\\-]\\d+|[A-Z][SD]T|GMT([+\\-]\\d+)?))?.*");

		/// <summary>A bunch of formats to parse into</summary>
		private static readonly IList<DateTimeFormatter> DateTimeFormats = Arrays.AsList(DateTimeFormatter.IsoOffsetDateTime, DateTimeFormatter.IsoDateTime, DateTimeFormatter.IsoZonedDateTime, DateTimeFormatter.IsoLocalDateTime, DateTimeFormatter.IsoInstant
			, DateTimeFormatter.IsoOffsetDate, DateTimeFormatter.IsoDate, DateTimeFormatter.IsoLocalDate, DateTimeFormatter.IsoOffsetDate, DateTimeFormatter.IsoLocalTime, new DateTimeFormatterBuilder().AppendValue(ChronoField.Year, 4, 10, SignStyle.ExceedsPad
			).AppendValue(ChronoField.MonthOfYear, 2).AppendValue(ChronoField.DayOfMonth, 2).AppendLiteral("T").AppendValue(ChronoField.HourOfDay, 2).AppendValue(ChronoField.MinuteOfHour, 2).AppendValue(ChronoField.SecondOfMinute, 2).ToFormatter(), new 
			DateTimeFormatterBuilder().AppendValue(ChronoField.Year, 4, 10, SignStyle.ExceedsPad).AppendValue(ChronoField.MonthOfYear, 2).AppendValue(ChronoField.DayOfMonth, 2).AppendLiteral("T").AppendValue(ChronoField.HourOfDay, 2).AppendValue(ChronoField
			.MinuteOfHour, 2).AppendValue(ChronoField.SecondOfMinute, 2).AppendZoneOrOffsetId().ToFormatter(), new DateTimeFormatterBuilder().AppendValue(ChronoField.Year, 4, 10, SignStyle.ExceedsPad).AppendValue(ChronoField.MonthOfYear, 2).AppendValue
			(ChronoField.DayOfMonth, 2).ToFormatter());

		// TODO: Timezone...
		// Ambiguous pattern - interpret as MM/DD/YY(YY)
		// Ambiguous pattern - interpret as MM-DD-YY(YY)
		// Euro date
		// Ambiguous pattern - interpret as DD.MM.YY(YY)
		/// <summary>
		/// Try parsing a given string into an
		/// <see cref="Org.Joda.Time.Instant"/>
		/// in as many ways as we know how.
		/// Dates will be normalized to the start of their days.
		/// </summary>
		/// <param name="value">The instant we are parsing.</param>
		/// <param name="timezone">The timezone, if none is given in the instant.</param>
		/// <returns>An instant corresponding to the value, if it could be parsed.</returns>
		public static Optional<Instant> ParseInstant(string value, Optional<ZoneId> timezone)
		{
			foreach (DateTimeFormatter formatter in DateTimeFormats)
			{
				try
				{
					ITemporalAccessor datetime = formatter.Parse(value);
					ZoneId parsedTimezone = datetime.Query(TemporalQueries.ZoneId());
					ZoneOffset parsedOffset = datetime.Query(TemporalQueries.Offset());
					if (parsedTimezone != null)
					{
						return Optional.Of(Instant.From(datetime));
					}
					else
					{
						if (parsedOffset != null)
						{
							try
							{
								return Optional.Of(Instant.OfEpochSecond(datetime.GetLong(ChronoField.InstantSeconds)));
							}
							catch (UnsupportedTemporalTypeException)
							{
								return Optional.Of(LocalDate.Of(datetime.Get(ChronoField.Year), datetime.Get(ChronoField.MonthOfYear), datetime.Get(ChronoField.DayOfMonth)).AtStartOfDay().ToInstant(parsedOffset));
							}
						}
						else
						{
							if (timezone.IsPresent())
							{
								Instant reference = LocalDate.Of(datetime.Get(ChronoField.Year), datetime.Get(ChronoField.MonthOfYear), datetime.Get(ChronoField.DayOfMonth)).AtStartOfDay().ToInstant(ZoneOffset.Utc);
								ZoneOffset currentOffsetForMyZone = timezone.Get().GetRules().GetOffset(reference);
								try
								{
									return Optional.Of(LocalDateTime.Of(datetime.Get(ChronoField.Year), datetime.Get(ChronoField.MonthOfYear), datetime.Get(ChronoField.DayOfMonth), datetime.Get(ChronoField.HourOfDay), datetime.Get(ChronoField.MinuteOfHour), datetime.Get(ChronoField
										.SecondOfMinute)).ToInstant(currentOffsetForMyZone));
								}
								catch (UnsupportedTemporalTypeException)
								{
									return Optional.Of(LocalDate.Of(datetime.Get(ChronoField.Year), datetime.Get(ChronoField.MonthOfYear), datetime.Get(ChronoField.DayOfMonth)).AtStartOfDay().ToInstant(currentOffsetForMyZone));
								}
							}
						}
					}
				}
				catch (DateTimeParseException)
				{
				}
			}
			return Optional.Empty();
		}

		/// <summary>
		/// Converts a string that represents some kind of date into ISO 8601 format and
		/// returns it as a SUTime.Time
		/// YYYYMMDDThhmmss
		/// </summary>
		/// <param name="dateStr">The serialized date we are parsing to a document date.</param>
		/// <param name="allowPartial">(allow partial ISO)</param>
		public static SUTime.Time ParseDateTime(string dateStr, bool allowPartial)
		{
			if (dateStr == null)
			{
				return null;
			}
			Optional<Instant> refInstant = ParseInstant(dateStr, Optional.Empty());
			if (refInstant.IsPresent())
			{
				return new SUTime.GroundedTime(new Instant(refInstant.Get().ToEpochMilli()));
			}
			Matcher m = PatternIso.Matcher(dateStr);
			if (m.Matches())
			{
				string time = m.Group(4);
				SUTime.IsoDate isoDate = new SUTime.IsoDate(m.Group(1), m.Group(2), m.Group(3));
				if (time != null)
				{
					SUTime.IsoTime isoTime = new SUTime.IsoTime(m.Group(5), m.Group(6), m.Group(7), m.Group(8));
					return new SUTime.IsoDateTime(isoDate, isoTime);
				}
				else
				{
					return isoDate;
				}
			}
			m = PatternIsoDatetime.Matcher(dateStr);
			if (m.Matches())
			{
				SUTime.IsoDate date = new SUTime.IsoDate(m.Group(1), m.Group(2), m.Group(3));
				SUTime.IsoTime time = new SUTime.IsoTime(m.Group(4), m.Group(5), null);
				return new SUTime.IsoDateTime(date, time);
			}
			m = PatternIsoTime.Matcher(dateStr);
			if (m.Matches())
			{
				return new SUTime.IsoTime(m.Group(1), m.Group(2), m.Group(3), m.Group(4));
			}
			SUTime.IsoDate isoDate_1 = null;
			if (isoDate_1 == null)
			{
				m = PatternIsoDate1.Matcher(dateStr);
				if (m.Matches())
				{
					isoDate_1 = new SUTime.IsoDate(m.Group(1), m.Group(2), m.Group(3));
				}
			}
			if (isoDate_1 == null)
			{
				m = PatternIsoDate2.Matcher(dateStr);
				if (m.Matches())
				{
					isoDate_1 = new SUTime.IsoDate(m.Group(1), m.Group(2), m.Group(3));
				}
			}
			if (allowPartial)
			{
				m = PatternIsoDatePartial.Matcher(dateStr);
				if (m.Matches())
				{
					if (!(m.Group(1).Equals("XXXX") && m.Group(2).Equals("XX") && m.Group(3).Equals("XX")))
					{
						isoDate_1 = new SUTime.IsoDate(m.Group(1), m.Group(2), m.Group(3));
					}
				}
			}
			if (isoDate_1 == null)
			{
				m = PatternIsoAmbiguous1.Matcher(dateStr);
				if (m.Matches())
				{
					isoDate_1 = new SUTime.IsoDate(m.Group(3), m.Group(1), m.Group(2));
				}
			}
			if (isoDate_1 == null)
			{
				m = PatternIsoAmbiguous2.Matcher(dateStr);
				if (m.Matches())
				{
					isoDate_1 = new SUTime.IsoDate(m.Group(3), m.Group(1), m.Group(2));
				}
			}
			if (isoDate_1 == null)
			{
				m = PatternIsoAmbiguous3.Matcher(dateStr);
				if (m.Matches())
				{
					isoDate_1 = new SUTime.IsoDate(m.Group(3), m.Group(2), m.Group(1));
				}
			}
			// Now add Time of Day
			SUTime.IsoTime isoTime_1 = null;
			if (isoTime_1 == null)
			{
				m = PatternIsoTimeOfDay.Matcher(dateStr);
				if (m.Matches())
				{
					// TODO: Fix
					isoTime_1 = new SUTime.IsoTime(m.Group(1), m.Group(2), m.Group(4));
				}
			}
			if (isoDate_1 != null && isoTime_1 != null)
			{
				return new SUTime.IsoDateTime(isoDate_1, isoTime_1);
			}
			else
			{
				if (isoDate_1 != null)
				{
					return isoDate_1;
				}
				else
				{
					return isoTime_1;
				}
			}
		}

		public static SUTime.Time ParseDateTime(string dateStr)
		{
			return ParseDateTime(dateStr, false);
		}

		[System.Serializable]
		public class GroundedTime : SUTime.Time
		{
			internal IReadableInstant @base;

			public GroundedTime(SUTime.Time p, IReadableInstant @base)
				: base(p)
			{
				// Represents an absolute time
				this.@base = @base;
			}

			public GroundedTime(IReadableInstant @base)
			{
				this.@base = @base;
			}

			public override SUTime.Temporal SetTimeZone(DateTimeZone tz)
			{
				MutableDateTime tzBase = @base.ToInstant().ToMutableDateTime();
				tzBase.SetZone(tz);
				// TODO: setZoneRetainFields?
				return new SUTime.GroundedTime(this, tzBase);
			}

			public override bool HasTime()
			{
				return true;
			}

			public override bool IsGrounded()
			{
				return true;
			}

			public override SUTime.Duration GetDuration()
			{
				return DurationNone;
			}

			public override SUTime.Range GetRange(int flags, SUTime.Duration granularity)
			{
				return new SUTime.Range(this, this);
			}

			public override string ToFormattedString(int flags)
			{
				return @base.ToString();
			}

			public override SUTime.Temporal Resolve(SUTime.Time refTime, int flags)
			{
				return this;
			}

			public override SUTime.Time Add(SUTime.Duration offset)
			{
				Period p = offset.GetJodaTimePeriod();
				SUTime.GroundedTime g = new SUTime.GroundedTime(@base.ToInstant().WithDurationAdded(p.ToDurationFrom(@base), 1));
				g.approx = this.approx;
				g.mod = this.mod;
				return g;
			}

			protected internal override SUTime.Time Intersect(SUTime.Time t)
			{
				if (t.GetRange().Contains(this.GetRange()))
				{
					return this;
				}
				else
				{
					return null;
				}
			}

			public override SUTime.Temporal Intersect(SUTime.Temporal other)
			{
				if (other == null)
				{
					return this;
				}
				if (other == TimeUnknown)
				{
					return this;
				}
				if (other.GetRange().Contains(this.GetRange()))
				{
					return this;
				}
				else
				{
					return null;
				}
			}

			public override Instant GetJodaTimeInstant()
			{
				return @base.ToInstant();
			}

			public override Partial GetJodaTimePartial()
			{
				return JodaTimeUtils.GetPartial(@base.ToInstant(), JodaTimeUtils.EmptyIsoPartial);
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>A Duration represents a period of time (without endpoints).</summary>
		/// <remarks>
		/// A Duration represents a period of time (without endpoints).
		/// <br />
		/// We have 3 types of durations:
		/// <ol>
		/// <li> DurationWithFields - corresponds to JodaTime Period,
		/// where we have fields like hours, weeks, etc </li>
		/// <li> DurationWithMillis -
		/// corresponds to JodaTime Duration, where the duration is specified in millis
		/// this gets rid of certain ambiguities such as a month with can be 28, 30, or
		/// 31 days </li>
		/// <li>InexactDuration - duration that is under determined (like a few
		/// days)</li>
		/// </ol>
		/// </remarks>
		[System.Serializable]
		public abstract class Duration : SUTime.Temporal, FuzzyInterval.IFuzzyComparable<SUTime.Duration>
		{
			public Duration()
			{
			}

			public Duration(SUTime.Duration d)
				: base(d)
			{
			}

			// Duration classes
			public static SUTime.Duration GetDuration(IReadablePeriod p)
			{
				return new SUTime.DurationWithFields(p);
			}

			public static SUTime.Duration GetDuration(Org.Joda.Time.Duration d)
			{
				return new SUTime.DurationWithMillis(d);
			}

			public static SUTime.Duration GetInexactDuration(IReadablePeriod p)
			{
				return new SUTime.InexactDuration(p);
			}

			public static SUTime.Duration GetInexactDuration(Org.Joda.Time.Duration d)
			{
				return new SUTime.InexactDuration(d.ToPeriod());
			}

			// Returns the inexact version of the duration
			public virtual SUTime.InexactDuration MakeInexact()
			{
				return new SUTime.InexactDuration(GetJodaTimePeriod());
			}

			public virtual DateTimeFieldType[] GetDateTimeFields()
			{
				return null;
			}

			public override bool IsGrounded()
			{
				return false;
			}

			public override SUTime.Time GetTime()
			{
				return null;
			}

			// There is no time associated with a duration?
			public virtual SUTime.Time ToTime(SUTime.Time refTime)
			{
				return ToTime(refTime, 0);
			}

			public virtual SUTime.Time ToTime(SUTime.Time refTime, int flags)
			{
				{
					// if ((flags & (DUR_RESOLVE_FROM_AS_REF | DUR_RESOLVE_TO_AS_REF)) == 0)
					Partial p = refTime.GetJodaTimePartial();
					if (p != null)
					{
						// For durations that have corresponding date time fields
						// this = current time without more specific fields than the duration
						DateTimeFieldType[] dtFieldTypes = GetDateTimeFields();
						if (dtFieldTypes != null)
						{
							SUTime.Time t = null;
							foreach (DateTimeFieldType dtft in dtFieldTypes)
							{
								if (p.IsSupported(dtft))
								{
									t = new SUTime.PartialTime(JodaTimeUtils.DiscardMoreSpecificFields(p, dtft));
								}
							}
							if (t == null)
							{
								Instant instant = refTime.GetJodaTimeInstant();
								if (instant != null)
								{
									foreach (DateTimeFieldType dtft_1 in dtFieldTypes)
									{
										if (instant.IsSupported(dtft_1))
										{
											Partial p2 = JodaTimeUtils.GetPartial(instant, p.With(dtft_1, 1));
											t = new SUTime.PartialTime(JodaTimeUtils.DiscardMoreSpecificFields(p2, dtft_1));
										}
									}
								}
							}
							if (t != null)
							{
								if ((flags & ResolveToPast) != 0)
								{
									// Check if this time is in the past, if not, subtract duration
									if (t.CompareTo(refTime) >= 0)
									{
										return t.Subtract(this);
									}
								}
								else
								{
									if ((flags & ResolveToFuture) != 0)
									{
										// Check if this time is in the future, if not, subtract
										// duration
										if (t.CompareTo(refTime) <= 0)
										{
											return t.Add(this);
										}
									}
								}
							}
							return t;
						}
					}
				}
				SUTime.Time minTime = refTime.Subtract(this);
				SUTime.Time maxTime = refTime.Add(this);
				SUTime.Range likelyRange = null;
				if ((flags & (DurResolveFromAsRef | ResolveToFuture)) != 0)
				{
					likelyRange = new SUTime.Range(refTime, maxTime, this);
				}
				else
				{
					if ((flags & (DurResolveToAsRef | ResolveToPast)) != 0)
					{
						likelyRange = new SUTime.Range(minTime, refTime, this);
					}
					else
					{
						SUTime.Duration halfDuration = this.DivideBy(2);
						likelyRange = new SUTime.Range(refTime.Subtract(halfDuration), refTime.Add(halfDuration), this);
					}
				}
				return new SUTime.TimeWithRange(likelyRange);
			}

			//      if ((flags & (RESOLVE_TO_FUTURE | RESOLVE_TO_PAST)) != 0) {
			//        return new TimeWithRange(likelyRange);
			//      }
			//      Range r = new Range(minTime, maxTime, this.multiplyBy(2));
			//      return new InexactTime(new TimeWithRange(likelyRange), this, r);
			public override SUTime.Duration GetDuration()
			{
				return this;
			}

			public override SUTime.Range GetRange(int flags, SUTime.Duration granularity)
			{
				return new SUTime.Range(null, null, this);
			}

			// Unanchored range
			public override SUTime.TimexType GetTimexType()
			{
				return SUTime.TimexType.Duration;
			}

			public abstract Period GetJodaTimePeriod();

			public abstract Org.Joda.Time.Duration GetJodaTimeDuration();

			public override string ToFormattedString(int flags)
			{
				if (GetTimeLabel() != null)
				{
					return GetTimeLabel();
				}
				Period p = GetJodaTimePeriod();
				string s = (p != null) ? p.ToString() : "PXX";
				if ((flags & (FormatIso | FormatTimex3Value)) == 0)
				{
					string m = GetMod();
					if (m != null)
					{
						try
						{
							SUTime.TimexMod tm = SUTime.TimexMod.ValueOf(m);
							if (tm.GetSymbol() != null)
							{
								s = tm.GetSymbol() + s;
							}
						}
						catch (Exception)
						{
						}
					}
				}
				return s;
			}

			public override SUTime.Duration GetPeriod()
			{
				/*    TimeLabel tl = getTimeLabel();
				if (tl != null) {
				return tl.getPeriod();
				} */
				SUTime.StandardTemporalType tlt = GetStandardTemporalType();
				if (tlt != null)
				{
					return tlt.GetPeriod();
				}
				return this;
			}

			// Rough approximate ordering of durations
			public virtual int CompareTo(SUTime.Duration d)
			{
				Org.Joda.Time.Duration d1 = GetJodaTimeDuration();
				Org.Joda.Time.Duration d2 = d.GetJodaTimeDuration();
				if (d1 == null && d2 == null)
				{
					return 0;
				}
				else
				{
					if (d1 == null)
					{
						return 1;
					}
					else
					{
						if (d2 == null)
						{
							return -1;
						}
					}
				}
				int cmp = d1.CompareTo(d2);
				if (cmp == 0)
				{
					if (d.IsApprox() && !this.IsApprox())
					{
						// Put exact in front of approx
						return -1;
					}
					else
					{
						if (!d.IsApprox() && this.IsApprox())
						{
							return 1;
						}
						else
						{
							return 0;
						}
					}
				}
				else
				{
					return cmp;
				}
			}

			public virtual bool IsComparable(SUTime.Duration d)
			{
				// TODO: When is two durations comparable?
				return true;
			}

			// Operations with durations
			public abstract SUTime.Duration Add(SUTime.Duration d);

			public abstract SUTime.Duration MultiplyBy(int m);

			public abstract SUTime.Duration DivideBy(int m);

			public virtual SUTime.Duration Subtract(SUTime.Duration d)
			{
				return Add(d.MultiplyBy(-1));
			}

			public override SUTime.Temporal Resolve(SUTime.Time refTime, int flags)
			{
				return this;
			}

			public override SUTime.Temporal Intersect(SUTime.Temporal t)
			{
				if (t == null)
				{
					return this;
				}
				if (t == TimeUnknown || t == DurationUnknown)
				{
					return this;
				}
				if (t is SUTime.Time)
				{
					SUTime.RelativeTime rt = new SUTime.RelativeTime((SUTime.Time)t, SUTime.TemporalOp.Intersect, this);
					rt = (SUTime.RelativeTime)rt.AddMod(this.GetMod());
					return rt;
				}
				else
				{
					if (t is SUTime.Range)
					{
					}
					else
					{
						// return new TemporalSet(t, TemporalOp.INTERSECT, this);
						if (t is SUTime.Duration)
						{
							SUTime.Duration d = (SUTime.Duration)t;
							return Intersect(d);
						}
					}
				}
				return null;
			}

			public virtual SUTime.Duration Intersect(SUTime.Duration d)
			{
				if (d == null || d == DurationUnknown)
				{
					return this;
				}
				int cmp = CompareTo(d);
				if (cmp < 0)
				{
					return this;
				}
				else
				{
					return d;
				}
			}

			public static SUTime.Duration Min(SUTime.Duration d1, SUTime.Duration d2)
			{
				if (d2 == null)
				{
					return d1;
				}
				if (d1 == null)
				{
					return d2;
				}
				if (d1.IsComparable(d2))
				{
					int c = d1.CompareTo(d2);
					return (c < 0) ? d1 : d2;
				}
				return d1;
			}

			public static SUTime.Duration Max(SUTime.Duration d1, SUTime.Duration d2)
			{
				if (d1 == null)
				{
					return d2;
				}
				if (d2 == null)
				{
					return d1;
				}
				if (d1.IsComparable(d2))
				{
					int c = d1.CompareTo(d2);
					return (c >= 0) ? d1 : d2;
				}
				return d2;
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>Duration that is specified using fields such as milliseconds, days, etc.</summary>
		[System.Serializable]
		public class DurationWithFields : SUTime.Duration
		{
			internal IReadablePeriod period;

			public DurationWithFields()
			{
				// Use Inexact duration to be able to specify duration with uncertain number
				// Like a few years
				this.period = null;
			}

			public DurationWithFields(IReadablePeriod period)
			{
				this.period = period;
			}

			public DurationWithFields(SUTime.Duration d, IReadablePeriod period)
				: base(d)
			{
				this.period = period;
			}

			public override SUTime.Duration MultiplyBy(int m)
			{
				if (m == 1 || period == null)
				{
					return this;
				}
				else
				{
					MutablePeriod p = period.ToMutablePeriod();
					for (int i = 0; i < period.Size(); i++)
					{
						p.SetValue(i, period.GetValue(i) * m);
					}
					return new SUTime.DurationWithFields(p);
				}
			}

			public override SUTime.Duration DivideBy(int m)
			{
				if (m == 1 || period == null)
				{
					return this;
				}
				else
				{
					MutablePeriod p = new MutablePeriod();
					for (int i = 0; i < period.Size(); i++)
					{
						int oldVal = period.GetValue(i);
						DurationFieldType field = period.GetFieldType(i);
						int remainder = oldVal % m;
						p.Add(field, oldVal - remainder);
						if (remainder != 0)
						{
							DurationFieldType f;
							int standardUnit = 1;
							// TODO: This seems silly, how to do this with jodatime???
							if (DurationFieldType.Centuries().Equals(field))
							{
								f = DurationFieldType.Years();
								standardUnit = 100;
							}
							else
							{
								if (DurationFieldType.Years().Equals(field))
								{
									f = DurationFieldType.Months();
									standardUnit = 12;
								}
								else
								{
									if (DurationFieldType.Halfdays().Equals(field))
									{
										f = DurationFieldType.Hours();
										standardUnit = 12;
									}
									else
									{
										if (DurationFieldType.Days().Equals(field))
										{
											f = DurationFieldType.Hours();
											standardUnit = 24;
										}
										else
										{
											if (DurationFieldType.Hours().Equals(field))
											{
												f = DurationFieldType.Minutes();
												standardUnit = 60;
											}
											else
											{
												if (DurationFieldType.Minutes().Equals(field))
												{
													f = DurationFieldType.Seconds();
													standardUnit = 60;
												}
												else
												{
													if (DurationFieldType.Seconds().Equals(field))
													{
														f = DurationFieldType.Millis();
														standardUnit = 1000;
													}
													else
													{
														if (DurationFieldType.Months().Equals(field))
														{
															f = DurationFieldType.Days();
															standardUnit = 30;
														}
														else
														{
															if (DurationFieldType.Weeks().Equals(field))
															{
																f = DurationFieldType.Days();
																standardUnit = 7;
															}
															else
															{
																if (DurationFieldType.Millis().Equals(field))
																{
																	// No more granularity units....
																	f = DurationFieldType.Millis();
																	standardUnit = 0;
																}
																else
																{
																	throw new NotSupportedException("Unsupported duration type: " + field + " when dividing");
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
							p.Add(f, standardUnit * remainder);
						}
					}
					for (int i_1 = 0; i_1 < p.Size(); i_1++)
					{
						p.SetValue(i_1, p.GetValue(i_1) / m);
					}
					return new SUTime.DurationWithFields(p);
				}
			}

			public override Period GetJodaTimePeriod()
			{
				return (period != null) ? period.ToPeriod() : null;
			}

			public override Duration GetJodaTimeDuration()
			{
				return (period != null) ? period.ToPeriod().ToDurationFrom(JodaTimeUtils.InstantZero) : null;
			}

			public override SUTime.Temporal Resolve(SUTime.Time refTime, int flags)
			{
				Instant instant = (refTime != null) ? refTime.GetJodaTimeInstant() : null;
				if (instant != null)
				{
					if ((flags & DurResolveFromAsRef) != 0)
					{
						return new SUTime.DurationWithMillis(this, period.ToPeriod().ToDurationFrom(instant));
					}
					else
					{
						if ((flags & DurResolveToAsRef) != 0)
						{
							return new SUTime.DurationWithMillis(this, period.ToPeriod().ToDurationTo(instant));
						}
					}
				}
				return this;
			}

			public override SUTime.Duration Add(SUTime.Duration d)
			{
				Period p = period.ToPeriod().Plus(d.GetJodaTimePeriod());
				if (this is SUTime.InexactDuration || d is SUTime.InexactDuration)
				{
					return new SUTime.InexactDuration(this, p);
				}
				else
				{
					return new SUTime.DurationWithFields(this, p);
				}
			}

			public override SUTime.Duration GetGranularity()
			{
				Period res = new Period();
				res = res.WithField(JodaTimeUtils.GetMostSpecific(GetJodaTimePeriod()), 1);
				return SUTime.Duration.GetDuration(res);
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>Duration specified in terms of milliseconds.</summary>
		[System.Serializable]
		public class DurationWithMillis : SUTime.Duration
		{
			private readonly IReadableDuration @base;

			public DurationWithMillis(long ms)
			{
				this.@base = new Duration(ms);
			}

			public DurationWithMillis(IReadableDuration @base)
			{
				this.@base = @base;
			}

			public DurationWithMillis(SUTime.Duration d, IReadableDuration @base)
				: base(d)
			{
				this.@base = @base;
			}

			public override SUTime.Duration MultiplyBy(int m)
			{
				if (m == 1)
				{
					return this;
				}
				else
				{
					long ms = @base.GetMillis();
					return new SUTime.DurationWithMillis(ms * m);
				}
			}

			public override SUTime.Duration DivideBy(int m)
			{
				if (m == 1)
				{
					return this;
				}
				else
				{
					long ms = @base.GetMillis();
					return new SUTime.DurationWithMillis(ms / m);
				}
			}

			public override Period GetJodaTimePeriod()
			{
				return @base.ToPeriod();
			}

			public override Duration GetJodaTimeDuration()
			{
				return @base.ToDuration();
			}

			public override SUTime.Duration Add(SUTime.Duration d)
			{
				if (d is SUTime.DurationWithMillis)
				{
					return new SUTime.DurationWithMillis(this, @base.ToDuration().Plus(((SUTime.DurationWithMillis)d).@base));
				}
				else
				{
					if (d is SUTime.DurationWithFields)
					{
						return ((SUTime.DurationWithFields)d).Add(this);
					}
					else
					{
						throw new NotSupportedException("Unknown duration type in add: " + d.GetType());
					}
				}
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>A range of durations.</summary>
		/// <remarks>A range of durations.  For instance, 2 to 3 days.</remarks>
		[System.Serializable]
		public class DurationRange : SUTime.Duration
		{
			private readonly SUTime.Duration minDuration;

			private readonly SUTime.Duration maxDuration;

			public DurationRange(SUTime.DurationRange d, SUTime.Duration min, SUTime.Duration max)
				: base(d)
			{
				this.minDuration = min;
				this.maxDuration = max;
			}

			public DurationRange(SUTime.Duration min, SUTime.Duration max)
			{
				this.minDuration = min;
				this.maxDuration = max;
			}

			public override bool IncludeTimexAltValue()
			{
				return true;
			}

			public override string ToFormattedString(int flags)
			{
				if ((flags & (FormatIso | FormatTimex3Value)) != 0)
				{
					// return super.toFormattedString(flags);
					return null;
				}
				StringBuilder sb = new StringBuilder();
				if (minDuration != null)
				{
					sb.Append(minDuration.ToFormattedString(flags));
				}
				sb.Append("/");
				if (maxDuration != null)
				{
					sb.Append(maxDuration.ToFormattedString(flags));
				}
				return sb.ToString();
			}

			public override Period GetJodaTimePeriod()
			{
				if (minDuration == null)
				{
					return maxDuration.GetJodaTimePeriod();
				}
				if (maxDuration == null)
				{
					return minDuration.GetJodaTimePeriod();
				}
				SUTime.Duration mid = minDuration.Add(maxDuration).DivideBy(2);
				return mid.GetJodaTimePeriod();
			}

			public override Duration GetJodaTimeDuration()
			{
				if (minDuration == null)
				{
					return maxDuration.GetJodaTimeDuration();
				}
				if (maxDuration == null)
				{
					return minDuration.GetJodaTimeDuration();
				}
				SUTime.Duration mid = minDuration.Add(maxDuration).DivideBy(2);
				return mid.GetJodaTimeDuration();
			}

			public override SUTime.Duration Add(SUTime.Duration d)
			{
				SUTime.Duration min2 = (minDuration != null) ? minDuration.Add(d) : null;
				SUTime.Duration max2 = (maxDuration != null) ? maxDuration.Add(d) : null;
				return new SUTime.DurationRange(this, min2, max2);
			}

			public override SUTime.Duration MultiplyBy(int m)
			{
				SUTime.Duration min2 = (minDuration != null) ? minDuration.MultiplyBy(m) : null;
				SUTime.Duration max2 = (maxDuration != null) ? maxDuration.MultiplyBy(m) : null;
				return new SUTime.DurationRange(this, min2, max2);
			}

			public override SUTime.Duration DivideBy(int m)
			{
				SUTime.Duration min2 = (minDuration != null) ? minDuration.DivideBy(m) : null;
				SUTime.Duration max2 = (maxDuration != null) ? maxDuration.DivideBy(m) : null;
				return new SUTime.DurationRange(this, min2, max2);
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>Duration that is inexact.</summary>
		/// <remarks>
		/// Duration that is inexact.  Use for durations such as "several days"
		/// in which case, we know the field is DAY, but we don't know the exact
		/// number of days
		/// </remarks>
		[System.Serializable]
		public class InexactDuration : SUTime.DurationWithFields
		{
			public InexactDuration(IReadablePeriod period)
			{
				// Original duration is estimate of how long this duration is
				// but since some aspects of it is unknown....
				// for now all fields are inexact
				// TODO: Have inexact duration in which some fields are exact
				// add/toISOString
				// boolean[] exactFields;
				this.period = period;
				// exactFields = new boolean[period.size()];
				this.approx = true;
			}

			public InexactDuration(SUTime.Duration d)
				: base(d, d.GetJodaTimePeriod())
			{
				this.approx = true;
			}

			public InexactDuration(SUTime.Duration d, IReadablePeriod period)
				: base(d, period)
			{
				this.approx = true;
			}

			public override string ToFormattedString(int flags)
			{
				string s = base.ToFormattedString(flags);
				return s.ReplaceAll("\\d+", PadFieldUnknown);
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>A time interval</summary>
		[System.Serializable]
		public class Range : SUTime.Temporal, IHasInterval<SUTime.Time>
		{
			private readonly SUTime.Time begin;

			private readonly SUTime.Time end;

			private readonly SUTime.Duration duration;

			public Range(SUTime.Time begin, SUTime.Time end)
			{
				// = TIME_UNKNOWN;
				// = TIME_UNKNOWN;
				// = DURATION_UNKNOWN;
				this.begin = begin;
				this.end = end;
				this.duration = SUTime.Time.Difference(begin, end);
			}

			public Range(SUTime.Time begin, SUTime.Time end, SUTime.Duration duration)
			{
				this.begin = begin;
				this.end = end;
				this.duration = duration;
			}

			public Range(SUTime.Time begin, SUTime.Duration duration)
			{
				this.begin = begin;
				this.end = TimeUnknown;
				this.duration = duration;
			}

			public Range(SUTime.Range r, SUTime.Time begin, SUTime.Time end, SUTime.Duration duration)
				: base(r)
			{
				this.begin = begin;
				this.end = end;
				this.duration = duration;
			}

			public override SUTime.Temporal SetTimeZone(DateTimeZone tz)
			{
				return new SUTime.Range(this, (SUTime.Time)SUTime.Temporal.SetTimeZone(begin, tz), (SUTime.Time)SUTime.Temporal.SetTimeZone(end, tz), duration);
			}

			public virtual Interval<SUTime.Time> GetInterval()
			{
				return FuzzyInterval.ToInterval(begin, end);
			}

			public virtual Interval GetJodaTimeInterval()
			{
				return new Interval(begin.GetJodaTimeInstant(), end.GetJodaTimeInstant());
			}

			public override bool IsGrounded()
			{
				return begin.IsGrounded() && end.IsGrounded();
			}

			public override SUTime.Time GetTime()
			{
				return begin;
			}

			// TODO: return something that makes sense for time...
			public override SUTime.Duration GetDuration()
			{
				return duration;
			}

			public override SUTime.Range GetRange(int flags, SUTime.Duration granularity)
			{
				return this;
			}

			public override SUTime.TimexType GetTimexType()
			{
				return SUTime.TimexType.Duration;
			}

			public override IDictionary<string, string> GetTimexAttributes(SUTime.TimeIndex timeIndex)
			{
				string beginTidStr = (begin != null) ? begin.GetTidString(timeIndex) : null;
				string endTidStr = (end != null) ? end.GetTidString(timeIndex) : null;
				IDictionary<string, string> map = base.GetTimexAttributes(timeIndex);
				if (beginTidStr != null)
				{
					map[SUTime.TimexAttr.beginPoint.ToString()] = beginTidStr;
				}
				if (endTidStr != null)
				{
					map[SUTime.TimexAttr.endPoint.ToString()] = endTidStr;
				}
				return map;
			}

			// public boolean includeTimexAltValue() { return true; }
			public override string ToFormattedString(int flags)
			{
				if ((flags & (FormatIso | FormatTimex3Value)) != 0)
				{
					if (GetTimeLabel() != null)
					{
						return GetTimeLabel();
					}
					string beginStr = (begin != null) ? begin.ToFormattedString(flags) : null;
					string endStr = (end != null) ? end.ToFormattedString(flags) : null;
					string durationStr = (duration != null) ? duration.ToFormattedString(flags) : null;
					if ((flags & FormatIso) != 0)
					{
						if (beginStr != null && endStr != null)
						{
							return beginStr + "/" + endStr;
						}
						else
						{
							if (beginStr != null && durationStr != null)
							{
								return beginStr + "/" + durationStr;
							}
							else
							{
								if (durationStr != null && endStr != null)
								{
									return durationStr + "/" + endStr;
								}
							}
						}
					}
					return durationStr;
				}
				else
				{
					StringBuilder sb = new StringBuilder();
					sb.Append("(");
					if (begin != null)
					{
						sb.Append(begin);
					}
					sb.Append(",");
					if (end != null)
					{
						sb.Append(end);
					}
					sb.Append(",");
					if (duration != null)
					{
						sb.Append(duration);
					}
					sb.Append(")");
					return sb.ToString();
				}
			}

			public override SUTime.Temporal Resolve(SUTime.Time refTime, int flags)
			{
				if (refTime == null)
				{
					return this;
				}
				if (IsGrounded())
				{
					return this;
				}
				if ((flags & RangeResolveTimeRef) != 0 && (begin == TimeRef || end == TimeRef))
				{
					SUTime.Time groundedBegin = begin;
					SUTime.Duration groundedDuration = duration;
					if (begin == TimeRef)
					{
						groundedBegin = (SUTime.Time)begin.Resolve(refTime, flags);
						groundedDuration = (duration != null) ? ((SUTime.Duration)duration.Resolve(refTime, flags | DurResolveFromAsRef)) : null;
					}
					SUTime.Time groundedEnd = end;
					if (end == TimeRef)
					{
						groundedEnd = (SUTime.Time)end.Resolve(refTime, flags);
						groundedDuration = (duration != null) ? ((SUTime.Duration)duration.Resolve(refTime, flags | DurResolveToAsRef)) : null;
					}
					return new SUTime.Range(this, groundedBegin, groundedEnd, groundedDuration);
				}
				else
				{
					return this;
				}
			}

			// TODO: Implement some range operations....
			public virtual SUTime.Range Offset(SUTime.Duration d, int offsetFlags)
			{
				return Offset(d, offsetFlags, RangeOffsetBegin | RangeOffsetEnd);
			}

			public virtual SUTime.Range Offset(SUTime.Duration d, int offsetFlags, int rangeFlags)
			{
				SUTime.Time b2 = begin;
				if ((rangeFlags & RangeOffsetBegin) != 0)
				{
					b2 = (begin != null) ? begin.Offset(d, offsetFlags) : null;
				}
				SUTime.Time e2 = end;
				if ((rangeFlags & RangeOffsetEnd) != 0)
				{
					e2 = (end != null) ? end.Offset(d, offsetFlags) : null;
				}
				return new SUTime.Range(this, b2, e2, duration);
			}

			public virtual SUTime.Range Subtract(SUTime.Duration d)
			{
				return Subtract(d, RangeExpandFixBegin);
			}

			public virtual SUTime.Range Subtract(SUTime.Duration d, int flags)
			{
				return Add(d.MultiplyBy(-1), RangeExpandFixBegin);
			}

			public virtual SUTime.Range Add(SUTime.Duration d)
			{
				return Add(d, RangeExpandFixBegin);
			}

			public virtual SUTime.Range Add(SUTime.Duration d, int flags)
			{
				SUTime.Duration d2 = duration.Add(d);
				SUTime.Time b2 = begin;
				SUTime.Time e2 = end;
				if ((flags & RangeExpandFixBegin) == 0)
				{
					b2 = (end != null) ? end.Offset(d2.MultiplyBy(-1), 0) : null;
				}
				else
				{
					if ((flags & RangeExpandFixEnd) == 0)
					{
						e2 = (begin != null) ? begin.Offset(d2, 0) : null;
					}
				}
				return new SUTime.Range(this, b2, e2, d2);
			}

			public virtual SUTime.Time Begin()
			{
				return begin;
			}

			public virtual SUTime.Time End()
			{
				return end;
			}

			public virtual SUTime.Time BeginTime()
			{
				if (begin != null)
				{
					SUTime.Range r = begin.GetRange();
					if (r != null && !begin.Equals(r.begin))
					{
						return r.begin;
					}
				}
				return begin;
			}

			public virtual SUTime.Time EndTime()
			{
				/*    if (end != null) {
				Range r = end.getRange();
				if (r != null && !end.equals(r.end)) {
				//return r.endTime();
				return r.end;
				}
				}        */
				return end;
			}

			public virtual SUTime.Time Mid()
			{
				if (duration != null && begin != null)
				{
					SUTime.Time b = begin.GetRange(RangeFlagsPadSpecified, duration.GetGranularity()).Begin();
					return b.Add(duration.DivideBy(2));
				}
				else
				{
					if (duration != null && end != null)
					{
						return end.Subtract(duration.DivideBy(2));
					}
					else
					{
						if (begin != null && end != null)
						{
						}
						else
						{
							// TODO: ....
							if (begin != null)
							{
								return begin;
							}
							else
							{
								if (end != null)
								{
									return end;
								}
							}
						}
					}
				}
				return null;
			}

			// TODO: correct implementation
			public override SUTime.Temporal Intersect(SUTime.Temporal t)
			{
				if (t is SUTime.Time)
				{
					return new SUTime.RelativeTime((SUTime.Time)t, SUTime.TemporalOp.Intersect, this);
				}
				else
				{
					if (t is SUTime.Range)
					{
						SUTime.Range rt = (SUTime.Range)t;
						// Assume begin/end defined (TODO: handle if duration defined)
						SUTime.Time b = SUTime.Time.Max(begin, rt.begin);
						SUTime.Time e = SUTime.Time.Min(end, rt.end);
						return new SUTime.Range(b, e);
					}
					else
					{
						if (t is SUTime.Duration)
						{
							return new SUTime.InexactTime(null, (SUTime.Duration)t, this);
						}
					}
				}
				return null;
			}

			/// <summary>Checks if the provided range r is within the current range.</summary>
			/// <remarks>
			/// Checks if the provided range r is within the current range.
			/// Note that equal ranges also returns true.
			/// </remarks>
			/// <param name="r">range</param>
			/// <returns>true if range r is contained in r</returns>
			public virtual bool Contains(SUTime.Range r)
			{
				if ((this.BeginTime().GetJodaTimeInstant().IsBefore(r.BeginTime().GetJodaTimeInstant()) || this.BeginTime().GetJodaTimeInstant().IsEqual(r.BeginTime().GetJodaTimeInstant())) && (this.EndTime().GetJodaTimeInstant().IsAfter(r.EndTime().GetJodaTimeInstant
					()) || this.EndTime().GetJodaTimeInstant().IsEqual(r.EndTime().GetJodaTimeInstant())))
				{
					return true;
				}
				return false;
			}

			/// <summary>Checks if the provided time is within the current range.</summary>
			/// <param name="t">A time to check containment for</param>
			/// <returns>Returns whether the provided time is within the current range</returns>
			public virtual bool Contains(SUTime.Time t)
			{
				return this.GetJodaTimeInterval().Contains(t.GetJodaTimeInstant());
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>Exciting set of times</summary>
		[System.Serializable]
		public abstract class TemporalSet : SUTime.Temporal
		{
			public TemporalSet()
			{
			}

			public TemporalSet(SUTime.TemporalSet t)
				: base(t)
			{
			}

			// public boolean includeTimexAltValue() { return true; }
			public override SUTime.TimexType GetTimexType()
			{
				return SUTime.TimexType.Set;
			}

			private const long serialVersionUID = 1;
		}

		/// <summary>Explicit set of times: like tomorrow and next week, not really used</summary>
		[System.Serializable]
		public class ExplicitTemporalSet : SUTime.TemporalSet
		{
			private readonly ICollection<SUTime.Temporal> temporals;

			public ExplicitTemporalSet(params SUTime.Temporal[] temporals)
			{
				this.temporals = CollectionUtils.AsSet(temporals);
			}

			public ExplicitTemporalSet(ICollection<SUTime.Temporal> temporals)
			{
				this.temporals = temporals;
			}

			public ExplicitTemporalSet(SUTime.ExplicitTemporalSet p, ICollection<SUTime.Temporal> temporals)
				: base(p)
			{
				this.temporals = temporals;
			}

			public override SUTime.Temporal SetTimeZone(DateTimeZone tz)
			{
				ICollection<SUTime.Temporal> tzTemporals = Generics.NewHashSet(temporals.Count);
				foreach (SUTime.Temporal t in temporals)
				{
					tzTemporals.Add(SUTime.Temporal.SetTimeZone(t, tz));
				}
				return new SUTime.ExplicitTemporalSet(this, tzTemporals);
			}

			public override bool IsGrounded()
			{
				return false;
			}

			public override SUTime.Time GetTime()
			{
				return null;
			}

			public override SUTime.Duration GetDuration()
			{
				// TODO: Return difference between min/max of set
				return null;
			}

			public override SUTime.Range GetRange(int flags, SUTime.Duration granularity)
			{
				// TODO: Return min/max of set
				return null;
			}

			public override SUTime.Temporal Resolve(SUTime.Time refTime, int flags)
			{
				SUTime.Temporal[] newTemporals = new SUTime.Temporal[temporals.Count];
				int i = 0;
				foreach (SUTime.Temporal t in temporals)
				{
					newTemporals[i] = t.Resolve(refTime, flags);
					i++;
				}
				return new SUTime.ExplicitTemporalSet(newTemporals);
			}

			public override string ToFormattedString(int flags)
			{
				if (GetTimeLabel() != null)
				{
					return GetTimeLabel();
				}
				if ((flags & FormatIso) != 0)
				{
					// TODO: is there iso standard?
					return null;
				}
				if ((flags & FormatTimex3Value) != 0)
				{
					// TODO: is there timex3 standard?
					return null;
				}
				return "{" + StringUtils.Join(temporals, ", ") + "}";
			}

			public override SUTime.Temporal Intersect(SUTime.Temporal other)
			{
				if (other == null)
				{
					return this;
				}
				if (other == TimeUnknown || other == DurationUnknown)
				{
					return this;
				}
				ICollection<SUTime.Temporal> newTemporals = Generics.NewHashSet();
				foreach (SUTime.Temporal t in temporals)
				{
					SUTime.Temporal t2 = t.Intersect(other);
					if (t2 != null)
					{
						newTemporals.Add(t2);
					}
				}
				return new SUTime.ExplicitTemporalSet(newTemporals);
			}

			private const long serialVersionUID = 1;
		}

		public static readonly SUTime.PeriodicTemporalSet Hourly = new SUTime.PeriodicTemporalSet(null, Hour, "EVERY", "P1X");

		public static readonly SUTime.PeriodicTemporalSet Nightly = new SUTime.PeriodicTemporalSet(Night, Day, "EVERY", "P1X");

		public static readonly SUTime.PeriodicTemporalSet Daily = new SUTime.PeriodicTemporalSet(null, Day, "EVERY", "P1X");

		public static readonly SUTime.PeriodicTemporalSet Monthly = new SUTime.PeriodicTemporalSet(null, Month, "EVERY", "P1X");

		public static readonly SUTime.PeriodicTemporalSet Quarterly = new SUTime.PeriodicTemporalSet(null, Quarter, "EVERY", "P1X");

		public static readonly SUTime.PeriodicTemporalSet Yearly = new SUTime.PeriodicTemporalSet(null, Year, "EVERY", "P1X");

		public static readonly SUTime.PeriodicTemporalSet Weekly = new SUTime.PeriodicTemporalSet(null, Week, "EVERY", "P1X");

		/// <summary>PeriodicTemporalSet represent a set of times that occurs with some frequency.</summary>
		/// <remarks>
		/// PeriodicTemporalSet represent a set of times that occurs with some frequency.
		/// Example: At 2-3pm every friday from September 1, 2011 to December 30, 2011.
		/// </remarks>
		[System.Serializable]
		public class PeriodicTemporalSet : SUTime.TemporalSet
		{
			/// <summary>
			/// Start and end times for when this set of times is suppose to be happening
			/// (e.g.
			/// </summary>
			/// <remarks>
			/// Start and end times for when this set of times is suppose to be happening
			/// (e.g. 2011-09-01 to 2011-12-30)
			/// </remarks>
			internal SUTime.Range occursIn;

			/// <summary>Temporal that re-occurs (e.g.</summary>
			/// <remarks>Temporal that re-occurs (e.g. Friday 2-3pm)</remarks>
			internal SUTime.Temporal @base;

			/// <summary>The periodicity of re-occurrence (e.g.</summary>
			/// <remarks>The periodicity of re-occurrence (e.g. week)</remarks>
			internal SUTime.Duration periodicity;

			/// <summary>Quantifier - every, every other</summary>
			internal string quant;

			/// <summary>String representation of frequency (3 days = P3D, 3 times = P3X)</summary>
			internal string freq;

			public PeriodicTemporalSet(SUTime.Temporal @base, SUTime.Duration periodicity, string quant, string freq)
			{
				// How often (once, twice)
				// int count;
				// public ExplicitTemporalSet toExplicitTemporalSet();
				this.@base = @base;
				this.periodicity = periodicity;
				this.quant = quant;
				this.freq = freq;
			}

			public PeriodicTemporalSet(SUTime.PeriodicTemporalSet p, SUTime.Temporal @base, SUTime.Duration periodicity, SUTime.Range range, string quant, string freq)
				: base(p)
			{
				this.occursIn = range;
				this.@base = @base;
				this.periodicity = periodicity;
				this.quant = quant;
				this.freq = freq;
			}

			public override SUTime.Temporal SetTimeZone(DateTimeZone tz)
			{
				return new SUTime.PeriodicTemporalSet(this, SUTime.Temporal.SetTimeZone(@base, tz), periodicity, (SUTime.Range)SUTime.Temporal.SetTimeZone(occursIn, tz), quant, freq);
			}

			public virtual SUTime.PeriodicTemporalSet MultiplyDurationBy(int scale)
			{
				return new SUTime.PeriodicTemporalSet(this, this.@base, periodicity.MultiplyBy(scale), this.occursIn, this.quant, this.freq);
			}

			public virtual SUTime.PeriodicTemporalSet DivideDurationBy(int scale)
			{
				return new SUTime.PeriodicTemporalSet(this, this.@base, periodicity.DivideBy(scale), this.occursIn, this.quant, this.freq);
			}

			public override bool IsGrounded()
			{
				return (occursIn != null && occursIn.IsGrounded());
			}

			public override SUTime.Duration GetPeriod()
			{
				return periodicity;
			}

			public override SUTime.Time GetTime()
			{
				return null;
			}

			public override SUTime.Duration GetDuration()
			{
				return null;
			}

			public override SUTime.Range GetRange(int flags, SUTime.Duration granularity)
			{
				return occursIn;
			}

			public override IDictionary<string, string> GetTimexAttributes(SUTime.TimeIndex timeIndex)
			{
				IDictionary<string, string> map = base.GetTimexAttributes(timeIndex);
				if (quant != null)
				{
					map[SUTime.TimexAttr.quant.ToString()] = quant;
				}
				if (freq != null)
				{
					map[SUTime.TimexAttr.freq.ToString()] = freq;
				}
				if (periodicity != null)
				{
					map["periodicity"] = periodicity.GetTimexValue();
				}
				return map;
			}

			public override SUTime.Temporal Resolve(SUTime.Time refTime, int flags)
			{
				SUTime.Range resolvedOccursIn = (occursIn != null) ? ((SUTime.Range)occursIn.Resolve(refTime, flags)) : null;
				SUTime.Temporal resolvedBase = (@base != null) ? @base.Resolve(null, 0) : null;
				return new SUTime.PeriodicTemporalSet(this, resolvedBase, this.periodicity, resolvedOccursIn, this.quant, this.freq);
			}

			public override string ToFormattedString(int flags)
			{
				if (GetTimeLabel() != null)
				{
					return GetTimeLabel();
				}
				if ((flags & FormatIso) != 0)
				{
					// TODO: is there iso standard?
					return null;
				}
				if (@base != null)
				{
					return @base.ToFormattedString(flags);
				}
				else
				{
					if (periodicity != null)
					{
						return periodicity.ToFormattedString(flags);
					}
				}
				return null;
			}

			public override SUTime.Temporal Intersect(SUTime.Temporal t)
			{
				if (t is SUTime.Range)
				{
					if (occursIn == null)
					{
						return new SUTime.PeriodicTemporalSet(this, @base, periodicity, (SUTime.Range)t, quant, freq);
					}
				}
				else
				{
					if (@base != null)
					{
						SUTime.Temporal merged = @base.Intersect(t);
						return new SUTime.PeriodicTemporalSet(this, merged, periodicity, occursIn, quant, freq);
					}
					else
					{
						return new SUTime.PeriodicTemporalSet(this, t, periodicity, occursIn, quant, freq);
					}
				}
				return null;
			}

			private const long serialVersionUID = 1;
		}
	}
}
