using Edu.Stanford.Nlp.Util;

using NUnit.Framework;
using Org.Joda.Time;


namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Tests basic SUTime operations</summary>
	/// <author>Angel Chang</author>
	[NUnit.Framework.TestFixture]
	public class SUTimeTest
	{
		private static void ResolveAndCheckRange(string message, SUTime.Temporal t, SUTime.Time anchor, string expected)
		{
			SUTime.Temporal res = t.Resolve(anchor);
			SUTime.Range range = res.GetRange();
			NUnit.Framework.Assert.AreEqual(expected, range.ToISOString(), message);
		}

		[Test]
		public virtual void TestResolveDowToDay()
		{
			Partial p = new Partial(JodaTimeUtils.standardISOWeekFields, new int[] { 2016, 1, 1, 0, 0, 0, 0 });
			NUnit.Framework.Assert.AreEqual("[year=2016, weekOfWeekyear=1, dayOfWeek=1, hourOfDay=0, minuteOfHour=0, secondOfMinute=0, millisOfSecond=0]", p.ToString());
			Partial p2 = JodaTimeUtils.ResolveDowToDay(p);
			NUnit.Framework.Assert.AreEqual("2016-01-04T00:00:00.000", p2.ToString());
		}

		[Test]
		public virtual void TestNext()
		{
			SUTime.Time anchorTime = new SUTime.IsoDate(2016, 6, 19);
			// Sunday
			Pair<SUTime.Temporal, string>[] testPairs = ErasureUtils.UncheckedCast(new Pair[] { Pair.MakePair(SUTime.Monday, "2016-06-20/2016-06-20"), Pair.MakePair(SUTime.Tuesday, "2016-06-21/2016-06-21"), Pair.MakePair(SUTime.Wednesday, "2016-06-22/2016-06-22"
				), Pair.MakePair(SUTime.Thursday, "2016-06-23/2016-06-23"), Pair.MakePair(SUTime.Friday, "2016-06-24/2016-06-24"), Pair.MakePair(SUTime.Saturday, "2016-06-25/2016-06-25"), Pair.MakePair(SUTime.Sunday, "2016-06-26/2016-06-26"), Pair.MakePair
				(SUTime.Morning, "2016-06-20T06:00:00.000/2016-06-20T12:00"), Pair.MakePair(SUTime.Afternoon, "2016-06-20T12:00:00.000/PT6H"), Pair.MakePair(SUTime.Evening, "2016-06-20T18:00:00.000/PT2H"), Pair.MakePair(SUTime.Night, "2016-06-20T14:00:00.000/2016-06-21T00:00:00.000"
				), Pair.MakePair(SUTime.Day, "2016-06-20/2016-06-20"), Pair.MakePair(SUTime.Week, "2016-06-20/2016-06-26"), Pair.MakePair(SUTime.Month, "2016-07-01/2016-07-31"), Pair.MakePair(SUTime.Month.MultiplyBy(3), "2016-06-19/2016-09-19"), Pair.MakePair
				(SUTime.Quarter, "2016-07-01/2016-09-30"), Pair.MakePair(SUTime.Year, "2017-01-01/2017-12-31"), Pair.MakePair(SUTime.Winter, "2017-12-01/2017-03"), Pair.MakePair(SUTime.Spring, "2017-03-01/2017-06"), Pair.MakePair(SUTime.Summer, "2017-06-01/2017-09"
				), Pair.MakePair(SUTime.Fall, "2017-09-01/2017-12") });
			// TODO: Check this...
			// TODO: Check this...
			for (int i = 0; i < testPairs.Length; i++)
			{
				Pair<SUTime.Temporal, string> p = testPairs[i];
				SUTime.RelativeTime rel1 = new SUTime.RelativeTime(SUTime.TimeRef, SUTime.TemporalOp.Next, p.First());
				ResolveAndCheckRange("Next for " + p.First() + " (" + i + ')', rel1, anchorTime, p.Second());
			}
		}

		[Test]
		public virtual void TestThis()
		{
			SUTime.Time anchorTime = new SUTime.IsoDate(2016, 6, 19);
			// Sunday
			Pair<SUTime.Temporal, string>[] testPairs = ErasureUtils.UncheckedCast(new Pair[] { Pair.MakePair(SUTime.Monday, "2016-06-13/2016-06-13"), Pair.MakePair(SUTime.Tuesday, "2016-06-14/2016-06-14"), Pair.MakePair(SUTime.Wednesday, "2016-06-15/2016-06-15"
				), Pair.MakePair(SUTime.Thursday, "2016-06-16/2016-06-16"), Pair.MakePair(SUTime.Friday, "2016-06-17/2016-06-17"), Pair.MakePair(SUTime.Saturday, "2016-06-18/2016-06-18"), Pair.MakePair(SUTime.Sunday, "2016-06-19/2016-06-19"), Pair.MakePair
				(SUTime.Morning, "2016-06-19T06:00:00.000/2016-06-19T12:00"), Pair.MakePair(SUTime.Afternoon, "2016-06-19T12:00:00.000/PT6H"), Pair.MakePair(SUTime.Evening, "2016-06-19T18:00:00.000/PT2H"), Pair.MakePair(SUTime.Night, "2016-06-19T14:00:00.000/2016-06-20T00:00:00.000"
				), Pair.MakePair(SUTime.Day, "2016-06-19/2016-06-19"), Pair.MakePair(SUTime.Week, "2016-06-13/2016-06-19"), Pair.MakePair(SUTime.Month, "2016-06-01/2016-06-30"), Pair.MakePair(SUTime.Month.MultiplyBy(3), "2016-05-04/2016-08-03"), Pair.MakePair
				(SUTime.Quarter, "2016-04-01/2016-06-30"), Pair.MakePair(SUTime.Year, "2016-01-01/2016-12-31"), Pair.MakePair(SUTime.Winter, "2016-12-01/2016-03"), Pair.MakePair(SUTime.Spring, "2016-03-01/2016-06"), Pair.MakePair(SUTime.Summer, "2016-06-01/2016-09"
				), Pair.MakePair(SUTime.Fall, "2016-09-01/2016-12") });
			// TODO: is this section right, should this be interpreted to be in the past?
			// TODO: Check this...
			// TODO: Check this...
			// TODO: is this right (Sunday is a weird day...)
			/*, "2016-06-01/2016-08-31"*/
			// TODO: check...
			for (int i = 0; i < testPairs.Length; i++)
			{
				Pair<SUTime.Temporal, string> p = testPairs[i];
				SUTime.RelativeTime rel1 = new SUTime.RelativeTime(SUTime.TimeRef, SUTime.TemporalOp.This, p.First());
				ResolveAndCheckRange("This for " + p.First() + " (" + i + ')', rel1, anchorTime, p.Second());
			}
		}

		[Test]
		public virtual void ParseDateTimeStandardInstantFormat()
		{
			NUnit.Framework.Assert.AreEqual(Instant.Parse("2017-11-02T19:30:00Z").ToEpochMilli(), SUTime.ParseDateTime("2017-11-02T19:30:00Z", true).GetJodaTimeInstant().GetMillis());
		}

		[Ignore]
		[Test]
		public virtual void ParseDateTimeStandardLocalDateTimeFormat()
		{
			LocalDateTime expected = LocalDateTime.Parse("2017-11-02T15:30");
			SUTime.Time actual = SUTime.ParseDateTime("2017-11-02T15:30", true);
			NUnit.Framework.Assert.AreEqual(expected.ToInstant(ZoneId.SystemDefault().GetRules().GetOffset(expected.ToInstant(ZoneOffset.Utc))).ToEpochMilli(), actual.GetJodaTimeInstant().GetMillis());
		}
	}
}
