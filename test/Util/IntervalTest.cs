

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Test for intervals</summary>
	/// <author>Angel Chang</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class IntervalTest
	{
		private string ToHexString(int n)
		{
			return string.Format("%08x", n);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestIntervalOverlaps()
		{
			Interval<int> i1_10 = Interval.ToInterval(1, 10);
			Interval<int> i2_9 = Interval.ToInterval(2, 9);
			Interval<int> i5_10 = Interval.ToInterval(5, 10);
			Interval<int> i1_5 = Interval.ToInterval(1, 5);
			Interval<int> i1_15 = Interval.ToInterval(1, 15);
			Interval<int> i5_20 = Interval.ToInterval(5, 20);
			Interval<int> i10_20 = Interval.ToInterval(10, 20);
			Interval<int> i15_20 = Interval.ToInterval(15, 20);
			Interval<int> i1_10b = Interval.ToInterval(1, 10);
			NUnit.Framework.Assert.IsTrue(i1_10.Overlaps(i2_9));
			NUnit.Framework.Assert.IsTrue(i1_10.Overlaps(i5_10));
			NUnit.Framework.Assert.IsTrue(i1_10.Overlaps(i1_5));
			NUnit.Framework.Assert.IsTrue(i1_10.Overlaps(i1_15));
			NUnit.Framework.Assert.IsTrue(i1_10.Overlaps(i5_20));
			NUnit.Framework.Assert.IsTrue(i1_10.Overlaps(i10_20));
			NUnit.Framework.Assert.IsFalse(i1_10.Overlaps(i15_20));
			NUnit.Framework.Assert.IsTrue(i1_10.Overlaps(i1_10b));
			NUnit.Framework.Assert.IsTrue(i2_9.Overlaps(i1_10));
			NUnit.Framework.Assert.IsTrue(i5_10.Overlaps(i1_10));
			NUnit.Framework.Assert.IsTrue(i1_5.Overlaps(i1_10));
			NUnit.Framework.Assert.IsTrue(i1_15.Overlaps(i1_10));
			NUnit.Framework.Assert.IsTrue(i5_20.Overlaps(i1_10));
			NUnit.Framework.Assert.IsTrue(i10_20.Overlaps(i1_10));
			NUnit.Framework.Assert.IsFalse(i15_20.Overlaps(i1_10));
			NUnit.Framework.Assert.IsTrue(i1_10b.Overlaps(i1_10));
			int openFlags = Interval.IntervalOpenBegin | Interval.IntervalOpenEnd;
			Interval<int> i1_10_open = Interval.ToInterval(1, 10, openFlags);
			Interval<int> i2_9_open = Interval.ToInterval(2, 9, openFlags);
			Interval<int> i5_10_open = Interval.ToInterval(5, 10, openFlags);
			Interval<int> i1_5_open = Interval.ToInterval(1, 5, openFlags);
			Interval<int> i1_15_open = Interval.ToInterval(1, 15, openFlags);
			Interval<int> i5_20_open = Interval.ToInterval(5, 20, openFlags);
			Interval<int> i10_20_open = Interval.ToInterval(10, 20, openFlags);
			Interval<int> i15_20_open = Interval.ToInterval(15, 20, openFlags);
			Interval<int> i1_10b_open = Interval.ToInterval(1, 10, openFlags);
			NUnit.Framework.Assert.IsTrue(i1_10_open.Overlaps(i2_9_open));
			NUnit.Framework.Assert.IsTrue(i1_10_open.Overlaps(i5_10_open));
			NUnit.Framework.Assert.IsTrue(i1_10_open.Overlaps(i1_5_open));
			NUnit.Framework.Assert.IsTrue(i1_10_open.Overlaps(i1_15_open));
			NUnit.Framework.Assert.IsTrue(i1_10_open.Overlaps(i5_20_open));
			NUnit.Framework.Assert.IsFalse(i1_10_open.Overlaps(i10_20_open));
			NUnit.Framework.Assert.IsFalse(i1_10_open.Overlaps(i15_20_open));
			NUnit.Framework.Assert.IsTrue(i1_10_open.Overlaps(i1_10b_open));
			NUnit.Framework.Assert.IsTrue(i2_9_open.Overlaps(i1_10_open));
			NUnit.Framework.Assert.IsTrue(i5_10_open.Overlaps(i1_10_open));
			NUnit.Framework.Assert.IsTrue(i1_5_open.Overlaps(i1_10_open));
			NUnit.Framework.Assert.IsTrue(i1_15_open.Overlaps(i1_10_open));
			NUnit.Framework.Assert.IsTrue(i5_20_open.Overlaps(i1_10_open));
			NUnit.Framework.Assert.IsFalse(i10_20_open.Overlaps(i1_10_open));
			NUnit.Framework.Assert.IsFalse(i15_20_open.Overlaps(i1_10_open));
			NUnit.Framework.Assert.IsTrue(i1_10b_open.Overlaps(i1_10_open));
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestIntervalContains()
		{
			Interval<int> i1_10 = Interval.ToInterval(1, 10);
			Interval<int> i2_9 = Interval.ToInterval(2, 9);
			Interval<int> i5_10 = Interval.ToInterval(5, 10);
			Interval<int> i1_5 = Interval.ToInterval(1, 5);
			Interval<int> i1_15 = Interval.ToInterval(1, 15);
			Interval<int> i5_20 = Interval.ToInterval(5, 20);
			Interval<int> i10_20 = Interval.ToInterval(10, 20);
			Interval<int> i15_20 = Interval.ToInterval(15, 20);
			Interval<int> i1_10b = Interval.ToInterval(1, 10);
			NUnit.Framework.Assert.IsTrue(i1_10.Contains(i2_9));
			NUnit.Framework.Assert.IsTrue(i1_10.Contains(i5_10));
			NUnit.Framework.Assert.IsTrue(i1_10.Contains(i1_5));
			NUnit.Framework.Assert.IsFalse(i1_10.Contains(i1_15));
			NUnit.Framework.Assert.IsFalse(i1_10.Contains(i5_20));
			NUnit.Framework.Assert.IsFalse(i1_10.Contains(i10_20));
			NUnit.Framework.Assert.IsFalse(i1_10.Contains(i15_20));
			NUnit.Framework.Assert.IsTrue(i1_10.Contains(i1_10b));
			NUnit.Framework.Assert.IsFalse(i2_9.Contains(i1_10));
			NUnit.Framework.Assert.IsFalse(i5_10.Contains(i1_10));
			NUnit.Framework.Assert.IsFalse(i1_5.Contains(i1_10));
			NUnit.Framework.Assert.IsTrue(i1_15.Contains(i1_10));
			NUnit.Framework.Assert.IsFalse(i5_20.Contains(i1_10));
			NUnit.Framework.Assert.IsFalse(i10_20.Contains(i1_10));
			NUnit.Framework.Assert.IsFalse(i15_20.Contains(i1_10));
			NUnit.Framework.Assert.IsTrue(i1_10b.Contains(i1_10));
			int openFlags = Interval.IntervalOpenBegin | Interval.IntervalOpenEnd;
			Interval<int> i1_10_open = Interval.ToInterval(1, 10, openFlags);
			Interval<int> i2_9_open = Interval.ToInterval(2, 9, openFlags);
			Interval<int> i5_10_open = Interval.ToInterval(5, 10, openFlags);
			Interval<int> i1_5_open = Interval.ToInterval(1, 5, openFlags);
			Interval<int> i1_15_open = Interval.ToInterval(1, 15, openFlags);
			Interval<int> i5_20_open = Interval.ToInterval(5, 20, openFlags);
			Interval<int> i10_20_open = Interval.ToInterval(10, 20, openFlags);
			Interval<int> i15_20_open = Interval.ToInterval(15, 20, openFlags);
			Interval<int> i1_10b_open = Interval.ToInterval(1, 10, openFlags);
			NUnit.Framework.Assert.IsTrue(i1_10_open.Contains(i2_9_open));
			NUnit.Framework.Assert.IsTrue(i1_10_open.Contains(i5_10_open));
			NUnit.Framework.Assert.IsTrue(i1_10_open.Contains(i1_5_open));
			NUnit.Framework.Assert.IsFalse(i1_10_open.Contains(i1_15_open));
			NUnit.Framework.Assert.IsFalse(i1_10_open.Contains(i5_20_open));
			NUnit.Framework.Assert.IsFalse(i1_10_open.Contains(i10_20_open));
			NUnit.Framework.Assert.IsFalse(i1_10_open.Contains(i15_20_open));
			NUnit.Framework.Assert.IsTrue(i1_10_open.Contains(i1_10b_open));
			NUnit.Framework.Assert.IsFalse(i2_9_open.Contains(i1_10_open));
			NUnit.Framework.Assert.IsFalse(i5_10_open.Contains(i1_10_open));
			NUnit.Framework.Assert.IsFalse(i1_5_open.Contains(i1_10_open));
			NUnit.Framework.Assert.IsTrue(i1_15_open.Contains(i1_10_open));
			NUnit.Framework.Assert.IsFalse(i5_20_open.Contains(i1_10_open));
			NUnit.Framework.Assert.IsFalse(i10_20_open.Contains(i1_10_open));
			NUnit.Framework.Assert.IsFalse(i15_20_open.Contains(i1_10_open));
			NUnit.Framework.Assert.IsTrue(i1_10b_open.Contains(i1_10_open));
			int openClosedFlags = Interval.IntervalOpenBegin;
			Interval<int> i1_10_openClosed = Interval.ToInterval(1, 10, openClosedFlags);
			Interval<int> i2_9_openClosed = Interval.ToInterval(2, 9, openClosedFlags);
			Interval<int> i5_10_openClosed = Interval.ToInterval(5, 10, openClosedFlags);
			Interval<int> i1_5_openClosed = Interval.ToInterval(1, 5, openClosedFlags);
			//    Interval<Integer> i1_15_openClosed = Interval.toInterval(1,15, openClosedFlags);
			//    Interval<Integer> i5_20_openClosed = Interval.toInterval(5,20, openClosedFlags);
			//    Interval<Integer> i10_20_openClosed = Interval.toInterval(10,20, openClosedFlags);
			//    Interval<Integer> i15_20_openClosed = Interval.toInterval(15,20, openClosedFlags);
			Interval<int> i1_10b_openClosed = Interval.ToInterval(1, 10, openClosedFlags);
			int closedOpenFlags = Interval.IntervalOpenEnd;
			Interval<int> i1_10_closedOpen = Interval.ToInterval(1, 10, closedOpenFlags);
			Interval<int> i2_9_closedOpen = Interval.ToInterval(2, 9, closedOpenFlags);
			Interval<int> i5_10_closedOpen = Interval.ToInterval(5, 10, closedOpenFlags);
			Interval<int> i1_5_closedOpen = Interval.ToInterval(1, 5, closedOpenFlags);
			//    Interval<Integer> i1_15_closedOpen = Interval.toInterval(1,15, closedOpenFlags);
			//    Interval<Integer> i5_20_closedOpen = Interval.toInterval(5,20, closedOpenFlags);
			//    Interval<Integer> i10_20_closedOpen = Interval.toInterval(10,20, closedOpenFlags);
			//    Interval<Integer> i15_20_closedOpen = Interval.toInterval(15,20, closedOpenFlags);
			Interval<int> i1_10b_closedOpen = Interval.ToInterval(1, 10, closedOpenFlags);
			NUnit.Framework.Assert.IsTrue(i1_10_closedOpen.Contains(i2_9_openClosed));
			NUnit.Framework.Assert.IsTrue(i1_10.Contains(i2_9_openClosed));
			NUnit.Framework.Assert.IsTrue(i1_10_openClosed.Contains(i2_9_openClosed));
			NUnit.Framework.Assert.IsTrue(i1_10_closedOpen.Contains(i2_9_closedOpen));
			NUnit.Framework.Assert.IsTrue(i1_10.Contains(i2_9_closedOpen));
			NUnit.Framework.Assert.IsTrue(i1_10_openClosed.Contains(i2_9_closedOpen));
			NUnit.Framework.Assert.IsFalse(i1_10_closedOpen.Contains(i5_10_openClosed));
			NUnit.Framework.Assert.IsTrue(i1_10.Contains(i5_10_openClosed));
			NUnit.Framework.Assert.IsTrue(i1_10_openClosed.Contains(i5_10_openClosed));
			NUnit.Framework.Assert.IsTrue(i1_10_closedOpen.Contains(i5_10_closedOpen));
			NUnit.Framework.Assert.IsTrue(i1_10.Contains(i5_10_closedOpen));
			NUnit.Framework.Assert.IsTrue(i1_10_openClosed.Contains(i5_10_closedOpen));
			NUnit.Framework.Assert.IsTrue(i1_10_closedOpen.Contains(i1_5_openClosed));
			NUnit.Framework.Assert.IsTrue(i1_10.Contains(i1_5_openClosed));
			NUnit.Framework.Assert.IsTrue(i1_10_openClosed.Contains(i1_5_openClosed));
			NUnit.Framework.Assert.IsTrue(i1_10_closedOpen.Contains(i1_5_closedOpen));
			NUnit.Framework.Assert.IsTrue(i1_10.Contains(i1_5_closedOpen));
			NUnit.Framework.Assert.IsFalse(i1_10_openClosed.Contains(i1_5_closedOpen));
			NUnit.Framework.Assert.IsTrue(i1_10_openClosed.Contains(i1_10b_openClosed));
			NUnit.Framework.Assert.IsFalse(i1_10_openClosed.Contains(i1_10b_closedOpen));
			NUnit.Framework.Assert.IsFalse(i1_10_closedOpen.Contains(i1_10b_openClosed));
			NUnit.Framework.Assert.IsTrue(i1_10_closedOpen.Contains(i1_10b_closedOpen));
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestIntervalRelations()
		{
			Interval<int> i1_10 = Interval.ToInterval(1, 10);
			Interval<int> i2_9 = Interval.ToInterval(2, 9);
			Interval<int> i5_10 = Interval.ToInterval(5, 10);
			Interval<int> i1_5 = Interval.ToInterval(1, 5);
			Interval<int> i1_15 = Interval.ToInterval(1, 15);
			Interval<int> i5_20 = Interval.ToInterval(5, 20);
			Interval<int> i10_20 = Interval.ToInterval(10, 20);
			Interval<int> i15_20 = Interval.ToInterval(15, 20);
			Interval<int> i1_10b = Interval.ToInterval(1, 10);
			Interval.RelType rel = i1_10.GetRelation(null);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.None, rel);
			int flags = i1_10.GetRelationFlags(null);
			NUnit.Framework.Assert.AreEqual(0, flags);
			rel = i1_10.GetRelation(i2_9);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Contain, rel);
			flags = i1_10.GetRelationFlags(i2_9);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsBefore | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeAfter | Interval.RelFlagsIntervalContain | Interval.RelFlagsIntervalOverlap), ToHexString(flags
				));
			rel = i1_10.GetRelation(i1_5);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Contain, rel);
			flags = i1_10.GetRelationFlags(i1_5);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsSame | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeAfter | Interval.RelFlagsIntervalContain | Interval.RelFlagsIntervalOverlap), ToHexString(flags));
			rel = i1_10.GetRelation(i1_15);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Inside, rel);
			flags = i1_10.GetRelationFlags(i1_15);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsSame | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeBefore | Interval.RelFlagsIntervalInside | Interval.RelFlagsIntervalOverlap), ToHexString(flags));
			rel = i1_10.GetRelation(i5_10);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Contain, rel);
			flags = i1_10.GetRelationFlags(i5_10);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsBefore | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeSame | Interval.RelFlagsIntervalContain | Interval.RelFlagsIntervalOverlap), ToHexString(flags)
				);
			rel = i1_10.GetRelation(i5_20);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Overlap, rel);
			flags = i1_10.GetRelationFlags(i5_20);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsBefore | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeBefore | Interval.RelFlagsIntervalOverlap), ToHexString(flags));
			rel = i1_10.GetRelation(i10_20);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.EndMeetBegin, rel);
			flags = i1_10.GetRelationFlags(i10_20);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsBefore | Interval.RelFlagsSeBefore | Interval.RelFlagsEsSame | Interval.RelFlagsEeBefore | Interval.RelFlagsIntervalOverlap), ToHexString(flags));
			rel = i1_10.GetRelation(i15_20);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Before, rel);
			flags = i1_10.GetRelationFlags(i15_20);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsBefore | Interval.RelFlagsSeBefore | Interval.RelFlagsEsBefore | Interval.RelFlagsEeBefore | Interval.RelFlagsIntervalBefore), ToHexString(flags));
			rel = i1_10.GetRelation(i1_10b);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Equal, rel);
			flags = i1_10.GetRelationFlags(i1_10b);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsSame | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeSame | Interval.RelFlagsIntervalSame | Interval.RelFlagsIntervalOverlap | Interval.RelFlagsIntervalContain
				 | Interval.RelFlagsIntervalInside), ToHexString(flags));
			///////////////////////////////////////////////////
			rel = i2_9.GetRelation(i1_10);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Inside, rel);
			flags = i2_9.GetRelationFlags(i1_10);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsAfter | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeBefore | Interval.RelFlagsIntervalInside | Interval.RelFlagsIntervalOverlap), ToHexString(flags)
				);
			rel = i1_5.GetRelation(i1_10);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Inside, rel);
			flags = i1_5.GetRelationFlags(i1_10);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsSame | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeBefore | Interval.RelFlagsIntervalInside | Interval.RelFlagsIntervalOverlap), ToHexString(flags));
			rel = i1_15.GetRelation(i1_10);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Contain, rel);
			flags = i1_15.GetRelationFlags(i1_10);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsSame | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeAfter | Interval.RelFlagsIntervalContain | Interval.RelFlagsIntervalOverlap), ToHexString(flags));
			rel = i5_10.GetRelation(i1_10);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Inside, rel);
			flags = i5_10.GetRelationFlags(i1_10);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsAfter | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeSame | Interval.RelFlagsIntervalInside | Interval.RelFlagsIntervalOverlap), ToHexString(flags));
			rel = i5_20.GetRelation(i1_10);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Overlap, rel);
			flags = i5_20.GetRelationFlags(i1_10);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsAfter | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeAfter | Interval.RelFlagsIntervalOverlap), ToHexString(flags));
			rel = i10_20.GetRelation(i1_10);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.BeginMeetEnd, rel);
			flags = i10_20.GetRelationFlags(i1_10);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsAfter | Interval.RelFlagsSeSame | Interval.RelFlagsEsAfter | Interval.RelFlagsEeAfter | Interval.RelFlagsIntervalOverlap), ToHexString(flags));
			rel = i15_20.GetRelation(i1_10);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.After, rel);
			flags = i15_20.GetRelationFlags(i1_10);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsAfter | Interval.RelFlagsSeAfter | Interval.RelFlagsEsAfter | Interval.RelFlagsEeAfter | Interval.RelFlagsIntervalAfter), ToHexString(flags));
			rel = i1_10b.GetRelation(i1_10);
			NUnit.Framework.Assert.AreEqual(Interval.RelType.Equal, rel);
			flags = i1_10b.GetRelationFlags(i1_10);
			NUnit.Framework.Assert.AreEqual(ToHexString(Interval.RelFlagsSsSame | Interval.RelFlagsSeBefore | Interval.RelFlagsEsAfter | Interval.RelFlagsEeSame | Interval.RelFlagsIntervalSame | Interval.RelFlagsIntervalOverlap | Interval.RelFlagsIntervalContain
				 | Interval.RelFlagsIntervalInside), ToHexString(flags));
		}
	}
}
