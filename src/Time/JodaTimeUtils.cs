using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Time;
using Java.Util;
using Org.Joda.Time;
using Org.Joda.Time.Chrono;
using Org.Joda.Time.Field;
using Sharpen;

namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Extensions to Joda time.</summary>
	/// <author>Angel Chang</author>
	/// <author>Gabor Angeli</author>
	public class JodaTimeUtils
	{
		private JodaTimeUtils()
		{
		}

		protected internal static readonly ZoneId Utc = ZoneId.Of("UTC");

		protected internal static readonly DateTimeFieldType[] standardISOFields = new DateTimeFieldType[] { DateTimeFieldType.Year(), DateTimeFieldType.MonthOfYear(), DateTimeFieldType.DayOfMonth(), DateTimeFieldType.HourOfDay(), DateTimeFieldType.
			MinuteOfHour(), DateTimeFieldType.SecondOfMinute(), DateTimeFieldType.MillisOfSecond() };

		protected internal static readonly DateTimeFieldType[] standardISOWeekFields = new DateTimeFieldType[] { DateTimeFieldType.Year(), DateTimeFieldType.WeekOfWeekyear(), DateTimeFieldType.DayOfWeek(), DateTimeFieldType.HourOfDay(), DateTimeFieldType
			.MinuteOfHour(), DateTimeFieldType.SecondOfMinute(), DateTimeFieldType.MillisOfSecond() };

		protected internal static readonly DateTimeFieldType[] standardISODateFields = new DateTimeFieldType[] { DateTimeFieldType.Year(), DateTimeFieldType.MonthOfYear(), DateTimeFieldType.DayOfMonth() };

		protected internal static readonly DateTimeFieldType[] standardISOTimeFields = new DateTimeFieldType[] { DateTimeFieldType.HourOfDay(), DateTimeFieldType.MinuteOfHour(), DateTimeFieldType.SecondOfMinute(), DateTimeFieldType.MillisOfSecond() };

		public static readonly Partial EmptyIsoPartial = new Partial(standardISOFields, new int[] { 0, 1, 1, 0, 0, 0, 0 });

		public static readonly Partial EmptyIsoWeekPartial = new Partial(standardISOWeekFields, new int[] { 0, 1, 1, 0, 0, 0, 0 });

		public static readonly Partial EmptyIsoDatePartial = new Partial(standardISODateFields, new int[] { 0, 1, 1 });

		public static readonly Partial EmptyIsoTimePartial = new Partial(standardISOTimeFields, new int[] { 0, 0, 0, 0 });

		public static readonly Instant InstantZero = new Instant(0);

		private sealed class _DurationFieldType_69 : DurationFieldType
		{
			public _DurationFieldType_69(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -8167713675442491871L;
			}

			private const long serialVersionUID;

			// static methods only
			// Standard ISO fields
			// should this be weekyear()?
			// Extensions to Joda time fields
			// Duration Fields
			public override DurationField GetField(Chronology chronology)
			{
				return new ScaledDurationField(chronology.Months(), Edu.Stanford.Nlp.Time.JodaTimeUtils.Quarters, 3);
			}
		}

		public static readonly DurationFieldType Quarters = new _DurationFieldType_69("quarters");

		private sealed class _DurationFieldType_77 : DurationFieldType
		{
			public _DurationFieldType_77(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -8167713675442491872L;
			}

			private const long serialVersionUID;

			public override DurationField GetField(Chronology chronology)
			{
				return new ScaledDurationField(chronology.Months(), Edu.Stanford.Nlp.Time.JodaTimeUtils.HalfYears, 6);
			}
		}

		public static readonly DurationFieldType HalfYears = new _DurationFieldType_77("halfyear");

		private sealed class _DurationFieldType_85 : DurationFieldType
		{
			public _DurationFieldType_85(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -4594189766036833410L;
			}

			private const long serialVersionUID;

			public override DurationField GetField(Chronology chronology)
			{
				return new ScaledDurationField(chronology.Years(), Edu.Stanford.Nlp.Time.JodaTimeUtils.Decades, 10);
			}
		}

		public static readonly DurationFieldType Decades = new _DurationFieldType_85("decades");

		private sealed class _DurationFieldType_93 : DurationFieldType
		{
			public _DurationFieldType_93(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -7268694266711862790L;
			}

			private const long serialVersionUID;

			public override DurationField GetField(Chronology chronology)
			{
				return new ScaledDurationField(chronology.Years(), Edu.Stanford.Nlp.Time.JodaTimeUtils.Centuries, 100);
			}
		}

		public static readonly DurationFieldType Centuries = new _DurationFieldType_93("centuries");

		private sealed class _DateTimeFieldType_102 : DateTimeFieldType
		{
			public _DateTimeFieldType_102(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -5677872459807379123L;
			}

			private const long serialVersionUID;

			// DateTimeFields
			public override DurationFieldType GetDurationType()
			{
				return Edu.Stanford.Nlp.Time.JodaTimeUtils.Quarters;
			}

			public override DurationFieldType GetRangeDurationType()
			{
				return DurationFieldType.Years();
			}

			public override DateTimeField GetField(Chronology chronology)
			{
				return new OffsetDateTimeField(new DividedDateTimeField(new OffsetDateTimeField(chronology.MonthOfYear(), -1), Edu.Stanford.Nlp.Time.JodaTimeUtils.QuarterOfYear, 3), 1);
			}
		}

		public static readonly DateTimeFieldType QuarterOfYear = new _DateTimeFieldType_102("quarterOfYear");

		private sealed class _DateTimeFieldType_118 : DateTimeFieldType
		{
			public _DateTimeFieldType_118(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -5677872459807379123L;
			}

			private const long serialVersionUID;

			public override DurationFieldType GetDurationType()
			{
				return Edu.Stanford.Nlp.Time.JodaTimeUtils.HalfYears;
			}

			public override DurationFieldType GetRangeDurationType()
			{
				return DurationFieldType.Years();
			}

			public override DateTimeField GetField(Chronology chronology)
			{
				return new OffsetDateTimeField(new DividedDateTimeField(new OffsetDateTimeField(chronology.MonthOfYear(), -1), Edu.Stanford.Nlp.Time.JodaTimeUtils.HalfYearOfYear, 6), 1);
			}
		}

		public static readonly DateTimeFieldType HalfYearOfYear = new _DateTimeFieldType_118("halfYearOfYear");

		private sealed class _DateTimeFieldType_134 : DateTimeFieldType
		{
			public _DateTimeFieldType_134(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -5677872459807379123L;
			}

			private const long serialVersionUID;

			public override DurationFieldType GetDurationType()
			{
				return DurationFieldType.Months();
			}

			public override DurationFieldType GetRangeDurationType()
			{
				return Edu.Stanford.Nlp.Time.JodaTimeUtils.Quarters;
			}

			public override DateTimeField GetField(Chronology chronology)
			{
				return new OffsetDateTimeField(new RemainderDateTimeField(new OffsetDateTimeField(chronology.MonthOfYear(), -1), Edu.Stanford.Nlp.Time.JodaTimeUtils.MonthOfQuarter, 3), 1);
			}
		}

		public static readonly DateTimeFieldType MonthOfQuarter = new _DateTimeFieldType_134("monthOfQuarter");

		private sealed class _DateTimeFieldType_150 : DateTimeFieldType
		{
			public _DateTimeFieldType_150(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -5677872459807379123L;
			}

			private const long serialVersionUID;

			public override DurationFieldType GetDurationType()
			{
				return DurationFieldType.Months();
			}

			public override DurationFieldType GetRangeDurationType()
			{
				return Edu.Stanford.Nlp.Time.JodaTimeUtils.HalfYears;
			}

			public override DateTimeField GetField(Chronology chronology)
			{
				return new OffsetDateTimeField(new RemainderDateTimeField(new OffsetDateTimeField(chronology.MonthOfYear(), -1), Edu.Stanford.Nlp.Time.JodaTimeUtils.MonthOfHalfYear, 6), 1);
			}
		}

		public static readonly DateTimeFieldType MonthOfHalfYear = new _DateTimeFieldType_150("monthOfHalfYear");

		private sealed class _DateTimeFieldType_166 : DateTimeFieldType
		{
			public _DateTimeFieldType_166(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 8676056306203579438L;
			}

			private const long serialVersionUID;

			public override DurationFieldType GetDurationType()
			{
				return DurationFieldType.Weeks();
			}

			public override DurationFieldType GetRangeDurationType()
			{
				return DurationFieldType.Months();
			}

			public override DateTimeField GetField(Chronology chronology)
			{
				return new OffsetDateTimeField(new RemainderDateTimeField(new OffsetDateTimeField(chronology.WeekOfWeekyear(), -1), Edu.Stanford.Nlp.Time.JodaTimeUtils.WeekOfMonth, 4), 1);
			}
		}

		public static readonly DateTimeFieldType WeekOfMonth = new _DateTimeFieldType_166("weekOfMonth");

		private sealed class _DateTimeFieldType_182 : DateTimeFieldType
		{
			public _DateTimeFieldType_182(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 4301444712229535664L;
			}

			private const long serialVersionUID;

			public override DurationFieldType GetDurationType()
			{
				return Edu.Stanford.Nlp.Time.JodaTimeUtils.Decades;
			}

			public override DurationFieldType GetRangeDurationType()
			{
				return DurationFieldType.Centuries();
			}

			public override DateTimeField GetField(Chronology chronology)
			{
				return new DividedDateTimeField(chronology.YearOfCentury(), Edu.Stanford.Nlp.Time.JodaTimeUtils.DecadeOfCentury, 10);
			}
		}

		public static readonly DateTimeFieldType DecadeOfCentury = new _DateTimeFieldType_182("decadeOfCentury");

		private sealed class _DateTimeFieldType_198 : DateTimeFieldType
		{
			public _DateTimeFieldType_198(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 4301444712229535664L;
			}

			private const long serialVersionUID;

			public override DurationFieldType GetDurationType()
			{
				return DurationFieldType.Years();
			}

			public override DurationFieldType GetRangeDurationType()
			{
				return Edu.Stanford.Nlp.Time.JodaTimeUtils.Decades;
			}

			public override DateTimeField GetField(Chronology chronology)
			{
				return new DividedDateTimeField(chronology.YearOfCentury(), Edu.Stanford.Nlp.Time.JodaTimeUtils.YearOfDecade, 10);
			}
		}

		public static readonly DateTimeFieldType YearOfDecade = new _DateTimeFieldType_198("yearOfDecade");

		// Helper functions for working with joda time type
		protected internal static bool HasField(IReadablePartial @base, DateTimeFieldType field)
		{
			if (@base == null)
			{
				return false;
			}
			else
			{
				return @base.IsSupported(field);
			}
		}

		protected internal static bool HasYYYYMMDD(IReadablePartial @base)
		{
			if (@base == null)
			{
				return false;
			}
			else
			{
				return @base.IsSupported(DateTimeFieldType.Year()) && @base.IsSupported(DateTimeFieldType.MonthOfYear()) && @base.IsSupported(DateTimeFieldType.DayOfMonth());
			}
		}

		protected internal static bool HasYYMMDD(IReadablePartial @base)
		{
			if (@base == null)
			{
				return false;
			}
			else
			{
				return @base.IsSupported(DateTimeFieldType.YearOfCentury()) && @base.IsSupported(DateTimeFieldType.MonthOfYear()) && @base.IsSupported(DateTimeFieldType.DayOfMonth());
			}
		}

		protected internal static bool HasField(IReadablePeriod @base, DurationFieldType field)
		{
			if (@base == null)
			{
				return false;
			}
			else
			{
				return @base.IsSupported(field);
			}
		}

		protected internal static Partial SetField(Partial @base, DateTimeFieldType field, int value)
		{
			if (@base == null)
			{
				return new Partial(field, value);
			}
			else
			{
				return @base.With(field, value);
			}
		}

		public static ICollection<DurationFieldType> GetSupportedDurationFields(Partial p)
		{
			ICollection<DurationFieldType> supportedDurations = Generics.NewHashSet();
			for (int i = 0; i < p.Size(); i++)
			{
				supportedDurations.Add(p.GetFieldType(i).GetDurationType());
			}
			return supportedDurations;
		}

		public static Period GetUnsupportedDurationPeriod(Partial p, Period offset)
		{
			if (offset == null)
			{
				return null;
			}
			ICollection<DurationFieldType> supported = GetSupportedDurationFields(p);
			Period res = null;
			for (int i = 0; i < offset.Size(); i++)
			{
				if (!supported.Contains(offset.GetFieldType(i)))
				{
					if (offset.GetValue(i) != 0)
					{
						if (res == null)
						{
							res = new Period();
						}
						res = res.WithField(offset.GetFieldType(i), offset.GetValue(i));
					}
				}
			}
			return res;
		}

		public static Partial Combine(Partial p1, Partial p2)
		{
			if (p1 == null)
			{
				return p2;
			}
			if (p2 == null)
			{
				return p1;
			}
			Partial p = p1;
			for (int i = 0; i < p2.Size(); i++)
			{
				DateTimeFieldType fieldType = p2.GetFieldType(i);
				if (fieldType == DateTimeFieldType.Year())
				{
					if (p.IsSupported(DateTimeFieldType.YearOfCentury()))
					{
						if (!p.IsSupported(DateTimeFieldType.CenturyOfEra()))
						{
							int yoc = p.Get(DateTimeFieldType.YearOfCentury());
							int refYear = p2.GetValue(i);
							int century = refYear / 100;
							int y2 = yoc + century * 100;
							// TODO: Figure out which way to go
							if (refYear < y2)
							{
								y2 -= 100;
							}
							p = p.Without(DateTimeFieldType.YearOfCentury());
							p = p.With(DateTimeFieldType.Year(), y2);
						}
						continue;
					}
					else
					{
						if (p.IsSupported(DateTimeFieldType.CenturyOfEra()))
						{
							continue;
						}
					}
				}
				else
				{
					if (fieldType == DateTimeFieldType.YearOfCentury())
					{
						if (p.IsSupported(DateTimeFieldType.Year()))
						{
							continue;
						}
					}
					else
					{
						if (fieldType == DateTimeFieldType.CenturyOfEra())
						{
							if (p.IsSupported(DateTimeFieldType.Year()))
							{
								continue;
							}
						}
					}
				}
				if (!p.IsSupported(fieldType))
				{
					p = p.With(fieldType, p2.GetValue(i));
				}
			}
			if (!p.IsSupported(DateTimeFieldType.Year()))
			{
				if (p.IsSupported(DateTimeFieldType.YearOfCentury()) && p.IsSupported(DateTimeFieldType.CenturyOfEra()))
				{
					int year = p.Get(DateTimeFieldType.YearOfCentury()) + p.Get(DateTimeFieldType.CenturyOfEra()) * 100;
					p = p.With(DateTimeFieldType.Year(), year);
					p = p.Without(DateTimeFieldType.YearOfCentury());
					p = p.Without(DateTimeFieldType.CenturyOfEra());
				}
			}
			if (p.IsSupported(DateTimeFieldType.HalfdayOfDay()))
			{
				int hour = -1;
				if (p.IsSupported(DateTimeFieldType.HourOfHalfday()))
				{
					hour = p.Get(DateTimeFieldType.HourOfHalfday());
					p = p.Without(DateTimeFieldType.HourOfHalfday());
				}
				else
				{
					if (p.IsSupported(DateTimeFieldType.ClockhourOfHalfday()))
					{
						hour = p.Get(DateTimeFieldType.ClockhourOfHalfday()) - 1;
						p = p.Without(DateTimeFieldType.ClockhourOfHalfday());
					}
					else
					{
						if (p.IsSupported(DateTimeFieldType.ClockhourOfDay()))
						{
							hour = p.Get(DateTimeFieldType.ClockhourOfDay()) - 1;
							p = p.Without(DateTimeFieldType.ClockhourOfDay());
						}
						else
						{
							if (p.IsSupported(DateTimeFieldType.HourOfDay()))
							{
								hour = p.Get(DateTimeFieldType.HourOfDay());
								p = p.Without(DateTimeFieldType.HourOfDay());
							}
						}
					}
				}
				if (hour >= 0)
				{
					if (p.Get(DateTimeFieldType.HalfdayOfDay()) == SUTime.HalfdayPm)
					{
						if (hour < 12)
						{
							hour = hour + 12;
						}
					}
					else
					{
						if (hour == 12)
						{
							hour = 0;
						}
					}
					if (hour < 24)
					{
						p = p.With(DateTimeFieldType.HourOfDay(), hour);
					}
					else
					{
						p = p.With(DateTimeFieldType.ClockhourOfDay(), hour);
					}
				}
			}
			return p;
		}

		protected internal static DateTimeFieldType GetMostGeneral(Partial p)
		{
			if (p.Size() > 0)
			{
				return p.GetFieldType(0);
			}
			return null;
		}

		protected internal static DateTimeFieldType GetMostSpecific(Partial p)
		{
			if (p.Size() > 0)
			{
				return p.GetFieldType(p.Size() - 1);
			}
			return null;
		}

		protected internal static DurationFieldType GetMostGeneral(Period p)
		{
			for (int i = 0; i < p.Size(); i++)
			{
				if (p.GetValue(i) != 0)
				{
					return p.GetFieldType(i);
				}
			}
			return null;
		}

		protected internal static DurationFieldType GetMostSpecific(Period p)
		{
			for (int i = p.Size() - 1; i >= 0; i--)
			{
				if (p.GetValue(i) != 0)
				{
					return p.GetFieldType(i);
				}
			}
			return null;
		}

		protected internal static Period GetJodaTimePeriod(Partial p)
		{
			if (p.Size() > 0)
			{
				DateTimeFieldType dtType = p.GetFieldType(p.Size() - 1);
				DurationFieldType dType = dtType.GetDurationType();
				Period period = new Period();
				if (period.IsSupported(dType))
				{
					return period.WithField(dType, 1);
				}
				else
				{
					DurationField df = dType.GetField(p.GetChronology());
					if (df is ScaledDurationField)
					{
						ScaledDurationField sdf = (ScaledDurationField)df;
						return period.WithField(sdf.GetWrappedField().GetType(), sdf.GetScalar());
					}
				}
			}
			// PeriodType.forFields(new DurationFieldType[]{dType});
			// return new Period(df.getUnitMillis(), PeriodType.forFields(new DurationFieldType[]{dType}));
			return null;
		}

		public static Partial CombineMoreGeneralFields(Partial p1, Partial p2)
		{
			return CombineMoreGeneralFields(p1, p2, null);
		}

		// Combines more general fields from p2 to p1
		public static Partial CombineMoreGeneralFields(Partial p1, Partial p2, DateTimeFieldType mgf)
		{
			Partial p = p1;
			Chronology c1 = p1.GetChronology();
			Chronology c2 = p2.GetChronology();
			if (!c1.Equals(c2))
			{
				throw new Exception("Different chronology: c1=" + c1 + ", c2=" + c2);
			}
			DateTimeFieldType p1MostGeneralField = null;
			if (p1.Size() > 0)
			{
				p1MostGeneralField = p1.GetFieldType(0);
			}
			// Assume fields ordered from most general to least....
			if (mgf == null || (p1MostGeneralField != null && IsMoreGeneral(p1MostGeneralField, mgf, c1)))
			{
				mgf = p1MostGeneralField;
			}
			for (int i = 0; i < p2.Size(); i++)
			{
				DateTimeFieldType fieldType = p2.GetFieldType(i);
				if (fieldType == DateTimeFieldType.Year())
				{
					if (p.IsSupported(DateTimeFieldType.YearOfCentury()))
					{
						if (!p.IsSupported(DateTimeFieldType.CenturyOfEra()))
						{
							int yoc = p.Get(DateTimeFieldType.YearOfCentury());
							int refYear = p2.GetValue(i);
							int century = refYear / 100;
							int y2 = yoc + century * 100;
							// TODO: Figure out which way to go
							if (refYear < y2)
							{
								y2 -= 100;
							}
							p = p.Without(DateTimeFieldType.YearOfCentury());
							p = p.With(DateTimeFieldType.Year(), y2);
						}
						continue;
					}
					else
					{
						if (p.IsSupported(Edu.Stanford.Nlp.Time.JodaTimeUtils.DecadeOfCentury))
						{
							if (!p.IsSupported(DateTimeFieldType.CenturyOfEra()))
							{
								int decade = p.Get(Edu.Stanford.Nlp.Time.JodaTimeUtils.DecadeOfCentury);
								int refYear = p2.GetValue(i);
								int century = refYear / 100;
								int y2 = decade * 10 + century * 100;
								// TODO: Figure out which way to go
								if (refYear < y2)
								{
									century--;
								}
								p = p.With(DateTimeFieldType.CenturyOfEra(), century);
							}
							continue;
						}
					}
				}
				if (mgf == null || IsMoreGeneral(fieldType, mgf, c1))
				{
					if (!p.IsSupported(fieldType))
					{
						p = p.With(fieldType, p2.GetValue(i));
					}
				}
				else
				{
					break;
				}
			}
			if (!p.IsSupported(DateTimeFieldType.Year()))
			{
				if (p.IsSupported(DateTimeFieldType.YearOfCentury()) && p.IsSupported(DateTimeFieldType.CenturyOfEra()))
				{
					int year = p.Get(DateTimeFieldType.YearOfCentury()) + p.Get(DateTimeFieldType.CenturyOfEra()) * 100;
					p = p.With(DateTimeFieldType.Year(), year);
					p = p.Without(DateTimeFieldType.YearOfCentury());
					p = p.Without(DateTimeFieldType.CenturyOfEra());
				}
			}
			return p;
		}

		public static Partial DiscardMoreSpecificFields(Partial p, DateTimeFieldType d)
		{
			Partial res = new Partial();
			for (int i = 0; i < p.Size(); i++)
			{
				DateTimeFieldType fieldType = p.GetFieldType(i);
				if (fieldType.Equals(d) || IsMoreGeneral(fieldType, d, p.GetChronology()))
				{
					res = res.With(fieldType, p.GetValue(i));
				}
			}
			if (res.IsSupported(Edu.Stanford.Nlp.Time.JodaTimeUtils.DecadeOfCentury) && !res.IsSupported(DateTimeFieldType.CenturyOfEra()))
			{
				if (p.IsSupported(DateTimeFieldType.Year()))
				{
					res = res.With(DateTimeFieldType.CenturyOfEra(), p.Get(DateTimeFieldType.Year()) / 100);
				}
			}
			return res;
		}

		public static Partial DiscardMoreSpecificFields(Partial p, DurationFieldType dft)
		{
			DurationField df = dft.GetField(p.GetChronology());
			Partial res = new Partial();
			for (int i = 0; i < p.Size(); i++)
			{
				DateTimeFieldType fieldType = p.GetFieldType(i);
				DurationField f = fieldType.GetDurationType().GetField(p.GetChronology());
				int cmp = df.CompareTo(f);
				if (cmp <= 0)
				{
					res = res.With(fieldType, p.GetValue(i));
				}
			}
			return res;
		}

		public static Period DiscardMoreSpecificFields(Period p, DurationFieldType dft, Chronology chronology)
		{
			DurationField df = dft.GetField(chronology);
			Period res = new Period();
			for (int i = 0; i < p.Size(); i++)
			{
				DurationFieldType fieldType = p.GetFieldType(i);
				DurationField f = fieldType.GetField(chronology);
				int cmp = df.CompareTo(f);
				if (cmp <= 0)
				{
					res = res.WithField(fieldType, p.GetValue(i));
				}
			}
			return res;
		}

		public static Partial PadMoreSpecificFields(Partial p, Period granularity)
		{
			DateTimeFieldType msf = GetMostSpecific(p);
			if (IsMoreGeneral(msf, DateTimeFieldType.Year(), p.GetChronology()) || IsMoreGeneral(msf, DateTimeFieldType.YearOfCentury(), p.GetChronology()))
			{
				if (p.IsSupported(DateTimeFieldType.YearOfCentury()))
				{
				}
				else
				{
					// OKAY
					if (p.IsSupported(Edu.Stanford.Nlp.Time.JodaTimeUtils.DecadeOfCentury))
					{
						if (p.IsSupported(DateTimeFieldType.CenturyOfEra()))
						{
							int year = p.Get(DateTimeFieldType.CenturyOfEra()) * 100 + p.Get(Edu.Stanford.Nlp.Time.JodaTimeUtils.DecadeOfCentury) * 10;
							p = p.Without(Edu.Stanford.Nlp.Time.JodaTimeUtils.DecadeOfCentury);
							p = p.Without(DateTimeFieldType.CenturyOfEra());
							p = p.With(DateTimeFieldType.Year(), year);
						}
						else
						{
							int year = p.Get(Edu.Stanford.Nlp.Time.JodaTimeUtils.DecadeOfCentury) * 10;
							p = p.Without(Edu.Stanford.Nlp.Time.JodaTimeUtils.DecadeOfCentury);
							p = p.With(DateTimeFieldType.YearOfCentury(), year);
						}
					}
					else
					{
						if (p.IsSupported(DateTimeFieldType.CenturyOfEra()))
						{
							int year = p.Get(DateTimeFieldType.CenturyOfEra()) * 100;
							p = p.Without(DateTimeFieldType.CenturyOfEra());
							p = p.With(DateTimeFieldType.Year(), year);
						}
					}
				}
			}
			bool useWeek = false;
			if (p.IsSupported(DateTimeFieldType.WeekOfWeekyear()))
			{
				if (!p.IsSupported(DateTimeFieldType.DayOfMonth()) && !p.IsSupported(DateTimeFieldType.DayOfWeek()))
				{
					p = p.With(DateTimeFieldType.DayOfWeek(), 1);
					if (p.IsSupported(DateTimeFieldType.MonthOfYear()))
					{
						p = p.Without(DateTimeFieldType.MonthOfYear());
					}
				}
				useWeek = true;
			}
			Partial p2 = useWeek ? EmptyIsoWeekPartial : EmptyIsoPartial;
			for (int i = 0; i < p2.Size(); i++)
			{
				DateTimeFieldType fieldType = p2.GetFieldType(i);
				if (msf == null || IsMoreSpecific(fieldType, msf, p.GetChronology()))
				{
					if (!p.IsSupported(fieldType))
					{
						if (fieldType == DateTimeFieldType.MonthOfYear())
						{
							if (p.IsSupported(QuarterOfYear))
							{
								p = p.With(DateTimeFieldType.MonthOfYear(), (p.Get(QuarterOfYear) - 1) * 3 + 1);
								continue;
							}
							else
							{
								if (p.IsSupported(HalfYearOfYear))
								{
									p = p.With(DateTimeFieldType.MonthOfYear(), (p.Get(HalfYearOfYear) - 1) * 6 + 1);
									continue;
								}
							}
						}
						p = p.With(fieldType, p2.GetValue(i));
					}
				}
			}
			if (granularity != null)
			{
				DurationFieldType mostSpecific = GetMostSpecific(granularity);
				p = DiscardMoreSpecificFields(p, mostSpecific);
			}
			return p;
		}

		public static bool IsCompatible(Partial p1, Partial p2)
		{
			if (p1 == null)
			{
				return true;
			}
			if (p2 == null)
			{
				return true;
			}
			for (int i = 0; i < p1.Size(); i++)
			{
				DateTimeFieldType type = p1.GetFieldType(i);
				int v = p1.GetValue(i);
				if (Edu.Stanford.Nlp.Time.JodaTimeUtils.HasField(p2, type))
				{
					if (v != p2.Get(type))
					{
						return false;
					}
				}
			}
			return true;
		}

		// Uses p2 to resolve dow for p1
		public static Partial ResolveDowToDay(Partial p1, Partial p2)
		{
			// Discard anything that's more specific than dayOfMonth for p2
			p2 = Edu.Stanford.Nlp.Time.JodaTimeUtils.DiscardMoreSpecificFields(p2, DateTimeFieldType.DayOfMonth());
			if (IsCompatible(p1, p2))
			{
				if (p1.IsSupported(DateTimeFieldType.DayOfWeek()))
				{
					if (!p1.IsSupported(DateTimeFieldType.DayOfMonth()))
					{
						if (p2.IsSupported(DateTimeFieldType.DayOfMonth()) && p2.IsSupported(DateTimeFieldType.MonthOfYear()) && p2.IsSupported(DateTimeFieldType.Year()))
						{
							Instant t2 = GetInstant(p2);
							DateTime t1 = p1.ToDateTime(t2);
							return GetPartial(t1.ToInstant(), p1.With(DateTimeFieldType.DayOfMonth(), 1));
						}
					}
				}
			}
			/*.with(DateTimeFieldType.weekOfWeekyear(), 1) */
			return p1;
		}

		public static Partial WithWeekYear(Partial p)
		{
			Partial res = new Partial();
			for (int i = 0; i < p.Size(); i++)
			{
				DateTimeFieldType fieldType = p.GetFieldType(i);
				if (fieldType == DateTimeFieldType.Year())
				{
					res = res.With(DateTimeFieldType.Weekyear(), p.GetValue(i));
				}
				else
				{
					res = res.With(fieldType, p.GetValue(i));
				}
			}
			return res;
		}

		// Resolve dow for p1
		public static Partial ResolveDowToDay(Partial p)
		{
			if (p.IsSupported(DateTimeFieldType.DayOfWeek()))
			{
				if (!p.IsSupported(DateTimeFieldType.DayOfMonth()))
				{
					if (p.IsSupported(DateTimeFieldType.WeekOfWeekyear()) && (p.IsSupported(DateTimeFieldType.Year())))
					{
						// Convert from year to weekyear (to avoid weirdness when the weekyear and year don't match at the beginning of the year)
						Partial pwy = WithWeekYear(p);
						Instant t2 = GetInstant(pwy);
						DateTime t1 = pwy.ToDateTime(t2);
						Partial res = GetPartial(t1.ToInstant(), EmptyIsoPartial);
						DateTimeFieldType mostSpecific = GetMostSpecific(p);
						res = DiscardMoreSpecificFields(res, mostSpecific.GetDurationType());
						return res;
					}
				}
			}
			return p;
		}

		// Uses p2 to resolve week for p1
		public static Partial ResolveWeek(Partial p1, Partial p2)
		{
			if (IsCompatible(p1, p2))
			{
				if (!p1.IsSupported(DateTimeFieldType.DayOfMonth()))
				{
					if (p2.IsSupported(DateTimeFieldType.DayOfMonth()) && p2.IsSupported(DateTimeFieldType.MonthOfYear()) && p2.IsSupported(DateTimeFieldType.Year()))
					{
						Instant t2 = GetInstant(p2);
						DateTime t1 = p1.ToDateTime(t2);
						return GetPartial(t1.ToInstant(), p1.Without(DateTimeFieldType.DayOfMonth()).Without(DateTimeFieldType.MonthOfYear()).With(DateTimeFieldType.WeekOfWeekyear(), 1));
					}
				}
			}
			return p1;
		}

		public static Partial ResolveWeek(Partial p)
		{
			// Figure out week
			if (p.IsSupported(DateTimeFieldType.DayOfMonth()) && p.IsSupported(DateTimeFieldType.MonthOfYear()) && p.IsSupported(DateTimeFieldType.Year()))
			{
				Instant t = GetInstant(p);
				//      return getPartial(t.toInstant(), p.without(DateTimeFieldType.dayOfMonth()).without(DateTimeFieldType.monthOfYear()).with(DateTimeFieldType.weekOfWeekyear(), 1));
				return GetPartial(t.ToInstant(), p.With(DateTimeFieldType.WeekOfWeekyear(), 1));
			}
			else
			{
				return p;
			}
		}

		public static Instant GetInstant(Partial p)
		{
			return GetInstant(p, Utc);
		}

		public static Instant GetInstant(Partial p, ZoneId timezone)
		{
			if (p == null)
			{
				return null;
			}
			int year = p.IsSupported(DateTimeFieldType.Year()) ? p.Get(DateTimeFieldType.Year()) : 0;
			if (!p.IsSupported(DateTimeFieldType.Year()))
			{
				if (p.IsSupported(DateTimeFieldType.CenturyOfEra()))
				{
					year += 100 * p.Get(DateTimeFieldType.CenturyOfEra());
				}
				if (p.IsSupported(DateTimeFieldType.YearOfCentury()))
				{
					year += p.Get(DateTimeFieldType.YearOfCentury());
				}
				else
				{
					if (p.IsSupported(DecadeOfCentury))
					{
						year += 10 * p.Get(DecadeOfCentury);
					}
				}
			}
			int moy = p.IsSupported(DateTimeFieldType.MonthOfYear()) ? p.Get(DateTimeFieldType.MonthOfYear()) : 1;
			if (!p.IsSupported(DateTimeFieldType.MonthOfYear()))
			{
				if (p.IsSupported(QuarterOfYear))
				{
					moy += 3 * (p.Get(QuarterOfYear) - 1);
				}
			}
			int dom = p.IsSupported(DateTimeFieldType.DayOfMonth()) ? p.Get(DateTimeFieldType.DayOfMonth()) : 1;
			int hod = p.IsSupported(DateTimeFieldType.HourOfDay()) ? p.Get(DateTimeFieldType.HourOfDay()) : 0;
			int moh = p.IsSupported(DateTimeFieldType.MinuteOfHour()) ? p.Get(DateTimeFieldType.MinuteOfHour()) : 0;
			int som = p.IsSupported(DateTimeFieldType.SecondOfMinute()) ? p.Get(DateTimeFieldType.SecondOfMinute()) : 0;
			int msos = p.IsSupported(DateTimeFieldType.MillisOfSecond()) ? p.Get(DateTimeFieldType.MillisOfSecond()) : 0;
			return new DateTime(year, moy, dom, hod, moh, som, msos, FromTimezone(timezone)).ToInstant();
		}

		private static ISOChronology FromTimezone(ZoneId timezone)
		{
			if (timezone == Utc)
			{
				return ISOChronology.GetInstanceUTC();
			}
			else
			{
				return ISOChronology.GetInstance(DateTimeZone.ForTimeZone(TimeZone.GetTimeZone(timezone)));
			}
		}

		// <-- Jesus Christ, Java...
		public static Partial GetPartial(Instant t, Partial p)
		{
			Partial res = new Partial(p);
			for (int i = 0; i < p.Size(); i++)
			{
				res = res.WithField(p.GetFieldType(i), t.Get(p.GetFieldType(i)));
			}
			return res;
		}

		// Add duration to partial
		public static Partial AddForce(Partial p, Period d, int scalar)
		{
			Instant t = GetInstant(p);
			t = t.WithDurationAdded(d.ToDurationFrom(InstantZero), scalar);
			return GetPartial(t, p);
		}

		// Returns if df1 is more general than df2
		public static bool IsMoreGeneral(DateTimeFieldType df1, DateTimeFieldType df2, Chronology chronology)
		{
			DurationFieldType df1DurationFieldType = df1.GetDurationType();
			DurationFieldType df2DurationFieldType = df2.GetDurationType();
			if (!df2DurationFieldType.Equals(df1DurationFieldType))
			{
				DurationField df1Unit = df1DurationFieldType.GetField(chronology);
				DurationFieldType p = df2.GetRangeDurationType();
				if (p != null)
				{
					DurationField df2Unit = df2DurationFieldType.GetField(chronology);
					int cmp = df1Unit.CompareTo(df2Unit);
					if (cmp > 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Returns if df1 is more specific than df2
		public static bool IsMoreSpecific(DateTimeFieldType df1, DateTimeFieldType df2, Chronology chronology)
		{
			DurationFieldType df1DurationFieldType = df1.GetDurationType();
			DurationFieldType df2DurationFieldType = df2.GetDurationType();
			if (!df2DurationFieldType.Equals(df1DurationFieldType))
			{
				DurationField df2Unit = df2DurationFieldType.GetField(chronology);
				DurationFieldType p = df1.GetRangeDurationType();
				if (p != null)
				{
					DurationField df1Unit = df1DurationFieldType.GetField(chronology);
					int cmp = df1Unit.CompareTo(df2Unit);
					if (cmp < 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static string ZeroPad(int value, int padding)
		{
			StringBuilder b = new StringBuilder();
			b.Append(value);
			while (b.Length < padding)
			{
				b.Insert(0, "0");
			}
			return b.ToString();
		}

		private static bool NoFurtherFields(DateTimeFieldType smallestFieldSet, IReadableDateTime begin, IReadableDateTime end)
		{
			//--Get Indices
			//(standard fields)
			int indexInStandard = -1;
			for (int i = 0; i < standardISOFields.Length; i++)
			{
				if (standardISOFields[i] == smallestFieldSet)
				{
					indexInStandard = i + 1;
				}
			}
			//(week-based fields)
			int indexInWeek = -1;
			for (int i_1 = 0; i_1 < standardISOWeekFields.Length; i_1++)
			{
				if (standardISOWeekFields[i_1] == smallestFieldSet)
				{
					indexInWeek = i_1 + 1;
				}
			}
			//(special fields)
			if (smallestFieldSet == QuarterOfYear)
			{
				for (int i_2 = 0; i_2 < standardISOFields.Length; i_2++)
				{
					if (standardISOFields[i_2] == DateTimeFieldType.MonthOfYear())
					{
						indexInStandard = i_2;
					}
				}
			}
			//(get data)
			int index = -1;
			DateTimeFieldType[] toCheck = null;
			if (indexInStandard >= 0)
			{
				index = indexInStandard;
				toCheck = standardISOFields;
			}
			else
			{
				if (indexInWeek >= 0)
				{
					index = indexInWeek;
					toCheck = standardISOWeekFields;
				}
				else
				{
					throw new ArgumentException("Field is not in my list of fields: " + smallestFieldSet);
				}
			}
			//--Perform Check
			for (int i_3 = index; i_3 < toCheck.Length; i_3++)
			{
				int minValue = MinimumValue(toCheck[i_3], begin);
				if (begin.Get(toCheck[i_3]) != minValue || end.Get(toCheck[i_3]) != minValue)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Return the minimum value of a field, closest to the reference time</summary>
		public static int MinimumValue(DateTimeFieldType type, IReadableDateTime reference)
		{
			return reference.ToDateTime().Property(type).GetMinimumValue();
		}

		/// <summary>Return the maximum value of a field, closest to the reference time</summary>
		public static int MaximumValue(DateTimeFieldType type, IReadableDateTime reference)
		{
			return reference.ToDateTime().Property(type).GetMaximumValue();
		}

		/// <summary>Return the TIMEX string for the time given</summary>
		public static string TimexTimeValue(IReadableDateTime time)
		{
			return time.GetYear().ToString() + '-' + ZeroPad(time.GetMonthOfYear(), 2) + '-' + ZeroPad(time.GetDayOfMonth(), 2) + 'T' + ZeroPad(time.GetHourOfDay(), 2) + ':' + ZeroPad(time.GetMinuteOfHour(), 2);
		}

		public class ConversionOptions
		{
			/// <summary>
			/// If true, give a "best guess" of the right date; if false, backoff to giving a duration
			/// for malformed dates.
			/// </summary>
			public bool forceDate = false;

			/// <summary>Force particular units -- e.g., force 20Y to be 20Y (20 years) rather than 2E (2 decades)</summary>
			public string[] forceUnits = new string[0];

			/// <summary>Treat durations as approximate</summary>
			public bool approximate = false;
		}

		public static string TimexDateValue(IReadableDateTime begin, IReadableDateTime end)
		{
			return TimexDateValue(begin, end, new JodaTimeUtils.ConversionOptions());
		}

		/// <summary>Return the TIMEX string for the range of dates given.</summary>
		/// <remarks>
		/// Return the TIMEX string for the range of dates given.
		/// For example, (2011-12-3:00:00,000 to 2011-12-4:00:00.000) would give 2011-12-3.
		/// </remarks>
		/// <param name="begin">The begin time for the timex</param>
		/// <param name="end">The end time for the timex</param>
		/// <param name="opts">Tweaks in the heuristic conversion</param>
		/// <returns>The string representation of a DATE type Timex3 expression</returns>
		public static string TimexDateValue(IReadableDateTime begin, IReadableDateTime end, JodaTimeUtils.ConversionOptions opts)
		{
			//--Special Cases
			if (begin.GetYear() < -100000)
			{
				return "PAST_REF";
			}
			else
			{
				if (end.GetYear() > 100000)
				{
					return "FUTURE_REF";
				}
				else
				{
					if (begin.Equals(end))
					{
						return TimexTimeValue(begin);
					}
				}
			}
			StringBuilder value = new StringBuilder();
			bool shouldBeDone = false;
			//--Differences
			int monthDiff = (end.GetMonthOfYear() - begin.GetMonthOfYear()) + (end.GetYear() - begin.GetYear()) * 12;
			int weekDiff = end.GetWeekOfWeekyear() - begin.GetWeekOfWeekyear() + (end.GetYear() - begin.GetYear()) * MaximumValue(DateTimeFieldType.WeekOfWeekyear(), begin);
			int dayDiff = end.GetDayOfMonth() - begin.GetDayOfMonth() + monthDiff * MaximumValue(DateTimeFieldType.DayOfMonth(), begin);
			int hrDiff = end.GetHourOfDay() - begin.GetHourOfDay() + dayDiff * 24;
			int minDiff = end.GetMinuteOfHour() - begin.GetMinuteOfHour() + hrDiff * 60;
			int secDiff = end.GetSecondOfMinute() - begin.GetSecondOfMinute() + minDiff * 60;
			//--Years
			if (NoFurtherFields(DateTimeFieldType.Year(), begin, end))
			{
				int diff = end.GetYear() - begin.GetYear();
				if (diff == 100 && (opts.forceDate || begin.GetYear() % 100 == 0))
				{
					//(case: century)
					value.Append((begin.GetYear() / 100)).Append("XX");
				}
				else
				{
					if (diff == 10 && (opts.forceDate || begin.GetYear() % 10 == 0))
					{
						//(case: decade)
						value.Append((begin.GetYear() / 10));
					}
					else
					{
						if (diff == 1 || opts.forceDate)
						{
							//(case: year)
							value.Append(begin.GetYear());
						}
						else
						{
							//(case: duration)
							return TimexDurationValue(begin, end);
						}
					}
				}
				return value.ToString();
			}
			else
			{
				if (monthDiff < 12 || opts.forceDate)
				{
					//(case: year and more)
					value.Append(begin.GetYear());
				}
				else
				{
					//(case: treat as duration)
					return TimexDurationValue(begin, end);
				}
			}
			//--Week/Month/Quarters
			value.Append("-");
			if (NoFurtherFields(DateTimeFieldType.MonthOfYear(), begin, end) || NoFurtherFields(DateTimeFieldType.WeekOfWeekyear(), begin, end))
			{
				bool monthTerminal = NoFurtherFields(DateTimeFieldType.MonthOfYear(), begin, end);
				bool weekTerminal = NoFurtherFields(DateTimeFieldType.WeekOfWeekyear(), begin, end);
				//(Month/Quarter)
				if (monthTerminal && monthDiff == 6 && (begin.GetMonthOfYear() - 1) % 6 == 0)
				{
					//(case: half of year)
					value.Append("H").Append((begin.GetMonthOfYear() - 1) / 6 + 1);
				}
				else
				{
					if (monthTerminal && monthDiff == 3 && (begin.GetMonthOfYear() - 1) % 3 == 0)
					{
						//(case: quarter of year)
						value.Append("Q").Append((begin.GetMonthOfYear() - 1) / 3 + 1);
					}
					else
					{
						if (monthTerminal && monthDiff == 3 && begin.GetMonthOfYear() % 3 == 0)
						{
							switch (begin.GetMonthOfYear())
							{
								case 12:
								{
									//(case: season)
									value.Append("WI");
									break;
								}

								case 3:
								{
									value.Append("SP");
									break;
								}

								case 6:
								{
									value.Append("SU");
									break;
								}

								case 9:
								{
									value.Append("FA");
									break;
								}

								default:
								{
									throw new InvalidOperationException("Season start month is unknown");
								}
							}
						}
						else
						{
							if (weekTerminal && weekDiff == 1)
							{
								//(case: a week)
								value.Append("W").Append(ZeroPad(begin.GetWeekOfWeekyear(), 2));
							}
							else
							{
								if (monthTerminal && monthDiff == 1 && weekDiff != 1 || opts.forceDate)
								{
									//(case: a month)
									value.Append(ZeroPad(begin.GetMonthOfYear(), 2));
								}
								else
								{
									//(case: treat as duration)
									return TimexDurationValue(begin, end);
								}
							}
						}
					}
				}
				return value.ToString();
			}
			else
			{
				if (NoFurtherFields(DateTimeFieldType.DayOfWeek(), begin, end) && dayDiff == 2 && begin.GetDayOfWeek() == 6)
				{
					//(case: a weekend)
					value.Append("W").Append(ZeroPad(begin.GetWeekOfWeekyear(), 2)).Append("-WE");
					return value.ToString();
				}
				else
				{
					if (dayDiff < MaximumValue(DateTimeFieldType.DayOfMonth(), begin) || opts.forceDate)
					{
						//(case: month and more)
						value.Append(ZeroPad(begin.GetMonthOfYear(), 2));
					}
					else
					{
						//(case: treat as duration)
						return TimexDurationValue(begin, end);
					}
				}
			}
			//--Weekday/Day
			value.Append("-");
			if (NoFurtherFields(DateTimeFieldType.DayOfMonth(), begin, end))
			{
				if (dayDiff == 1 || opts.forceDate)
				{
					//(case: a day)
					value.Append(ZeroPad(begin.GetDayOfMonth(), 2));
				}
				else
				{
					//(case: treat as duration)
					return TimexDurationValue(begin, end);
				}
				return value.ToString();
			}
			else
			{
				if (hrDiff < 24 || opts.forceDate)
				{
					//(case: day and more)
					value.Append(ZeroPad(begin.GetDayOfMonth(), 2));
				}
				else
				{
					//(case: treat as duration)
					return TimexDurationValue(begin, end);
				}
			}
			//--Hour/TimeOfDay
			value.Append("T");
			if (NoFurtherFields(DateTimeFieldType.HourOfDay(), begin, end))
			{
				//((case: half day)
				if (hrDiff == 12 && begin.GetHourOfDay() == 0)
				{
					value.Append("H1");
				}
				else
				{
					if (hrDiff == 12 && begin.GetHourOfDay() == 12)
					{
						value.Append("H2");
					}
					else
					{
						//(case: time of day)
						if (hrDiff == 4 && begin.GetHourOfDay() == 8)
						{
							value.Append("MO");
						}
						else
						{
							if (hrDiff == 4 && begin.GetHourOfDay() == 12)
							{
								value.Append("AF");
							}
							else
							{
								if (hrDiff == 4 && begin.GetHourOfDay() == 16)
								{
									value.Append("EV");
								}
								else
								{
									if (hrDiff == 4 && begin.GetHourOfDay() == 20)
									{
										value.Append("NI");
									}
									else
									{
										if (hrDiff == 1 || opts.forceDate)
										{
											//(case: an hour)
											value.Append(ZeroPad(begin.GetHourOfDay() + 1, 2));
										}
										else
										{
											//(case: treat as duration)
											return TimexDurationValue(begin, end);
										}
									}
								}
							}
						}
					}
				}
				return value.ToString();
			}
			else
			{
				if (minDiff <= 60 || opts.forceDate)
				{
					//(case: hour and more)
					value.Append(ZeroPad(begin.GetHourOfDay(), 2));
				}
				else
				{
					//(case: treat as duration)
					return TimexDurationValue(begin, end);
				}
			}
			//--Minute/Second
			value.Append(":");
			value.Append(ZeroPad(begin.GetMinuteOfHour(), 2));
			return value.ToString();
		}

		private static bool ConsistentWithForced(string cand, string[] forcedList)
		{
			//--Check If Forced
			foreach (string forced in forcedList)
			{
				if (forced.Equals(cand))
				{
					return true;
				}
			}
			//--Get Ordering
			string[] ordering = new string[] { "L", "C", "E", "Y", "Q", "M", "W", "D", "H", "m", "S" };
			int candIndex = -1;
			for (int i = 0; i < ordering.Length; i++)
			{
				if (ordering[i].Equals(cand))
				{
					candIndex = i;
					break;
				}
			}
			System.Diagnostics.Debug.Assert(candIndex >= 0);
			//--Check If Lower Priority Forced
			for (int candI = candIndex + 1; candI < ordering.Length; candI++)
			{
				foreach (string forced_1 in forcedList)
				{
					if (ordering[candI].Equals(forced_1))
					{
						return false;
					}
				}
			}
			//--OK
			return true;
		}

		/// <summary>
		/// Return the TIMEX string for the duration represented by the given period; approximately if
		/// approximate is set to true.
		/// </summary>
		/// <param name="duration">The JodaTime period representing this duration</param>
		/// <param name="opts">Options for the conversion (e.g., mark duration as approximates)</param>
		/// <returns>The string representation of a DURATION type Timex3 expression</returns>
		public static string TimexDurationValue(IReadablePeriod duration, JodaTimeUtils.ConversionOptions opts)
		{
			StringBuilder b = new StringBuilder().Append("P");
			bool seenTime = false;
			int years = duration.Get(DurationFieldType.Years());
			//(millenia)
			if (years >= 1000 && ConsistentWithForced("L", opts.forceUnits))
			{
				b.Append(opts.approximate ? "X" : years / 1000).Append("L");
				years = years % 1000;
			}
			//(centuries)
			if (years >= 100 && ConsistentWithForced("C", opts.forceUnits))
			{
				b.Append(opts.approximate ? "X" : years / 100).Append("C");
				years = years % 100;
			}
			//(decades)
			if (years >= 10 && ConsistentWithForced("E", opts.forceUnits))
			{
				b.Append(opts.approximate ? "X" : years / 10).Append("E");
				years = years % 10;
			}
			//(years)
			if (years != 0 && ConsistentWithForced("Y", opts.forceUnits))
			{
				b.Append(opts.approximate ? "X" : years).Append("Y");
			}
			//(months)
			int months = duration.Get(DurationFieldType.Months());
			if (months != 0)
			{
				if (months % 3 == 0 && ConsistentWithForced("Q", opts.forceUnits))
				{
					b.Append(opts.approximate ? "X" : months / 3).Append("Q");
					months = months % 3;
				}
				else
				{
					b.Append(opts.approximate ? "X" : months).Append("M");
				}
			}
			//(weeks)
			if (duration.Get(DurationFieldType.Weeks()) != 0)
			{
				b.Append(opts.approximate ? "X" : duration.Get(DurationFieldType.Weeks())).Append("W");
			}
			//(days)
			if (duration.Get(DurationFieldType.Days()) != 0)
			{
				b.Append(opts.approximate ? "X" : duration.Get(DurationFieldType.Days())).Append("D");
			}
			//(hours)
			if (duration.Get(DurationFieldType.Hours()) != 0)
			{
				if (!seenTime)
				{
					b.Append("T");
					seenTime = true;
				}
				b.Append(opts.approximate ? "X" : duration.Get(DurationFieldType.Hours())).Append("H");
			}
			//(minutes)
			if (duration.Get(DurationFieldType.Minutes()) != 0)
			{
				if (!seenTime)
				{
					b.Append("T");
					seenTime = true;
				}
				b.Append(opts.approximate ? "X" : duration.Get(DurationFieldType.Minutes())).Append("M");
			}
			//(seconds)
			if (duration.Get(DurationFieldType.Seconds()) != 0)
			{
				if (!seenTime)
				{
					b.Append("T");
					seenTime = true;
				}
				b.Append(opts.approximate ? "X" : duration.Get(DurationFieldType.Seconds())).Append("S");
			}
			return b.ToString();
		}

		public static string TimexDurationValue(IReadablePeriod duration)
		{
			return TimexDurationValue(duration, new JodaTimeUtils.ConversionOptions());
		}

		/// <summary>
		/// Return the TIMEX string for the difference between two dates
		/// TODO not really sure if this works...
		/// </summary>
		public static string TimexDurationValue(IReadableDateTime begin, IReadableDateTime end)
		{
			return TimexDurationValue(new Period(end.GetMillis() - begin.GetMillis()));
		}
	}
}
