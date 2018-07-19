using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Time
{
	[NUnit.Framework.TestFixture]
	public class DurationTest : TestSuite
	{
		[Test]
		public virtual void TestDurationContainsDuration()
		{
			SUTime.Range range1 = new SUTime.Range(new SUTime.IsoDate(1990, 2, 1), new SUTime.IsoDate(1990, 2, 28));
			// 1.2.1990 - 28.2.1990
			SUTime.Range range2 = new SUTime.Range(new SUTime.IsoDate(1990, 2, 3), new SUTime.IsoDate(1990, 2, 25));
			// 3.2.1990 - 25.2.1990
			SUTime.Range range3 = new SUTime.Range(new SUTime.IsoDate(1990, 1, 3), new SUTime.IsoDate(1990, 2, 25));
			// 3.1.1990 - 25.2.1990
			SUTime.Range range4 = new SUTime.Range(new SUTime.IsoDate(1990, 2, 3), new SUTime.IsoDate(1990, 3, 25));
			// 3.2.1990 - 25.3.1990
			NUnit.Framework.Assert.AreEqual(range1.Contains(range2), true);
			// 1-28. February contains 3-25 February
			NUnit.Framework.Assert.AreEqual(range1.Contains(range1), true);
			// 1-28. Feb. contains 1-28 Feb
			NUnit.Framework.Assert.AreEqual(range2.Contains(range1), false);
			//3-25  February contains not 1-28. February		
			NUnit.Framework.Assert.AreEqual(range1.Contains(range3), false);
			//1-28 Feb. contains not 3.1 - 25.2 (partially overlapping before)
			NUnit.Framework.Assert.AreEqual(range3.Contains(range1), false);
			NUnit.Framework.Assert.AreEqual(range1.Contains(range4), false);
			//1-28 Feb. contains not 3.2 - 25.3 (partially overlapping after)
			NUnit.Framework.Assert.AreEqual(range1.Contains(range4), false);
		}

		[Test]
		public virtual void TestDurationContainsTime()
		{
			SUTime.Range range1 = new SUTime.Range(new SUTime.IsoDate(1990, 2, 1), new SUTime.IsoDate(1990, 2, 28));
			// 1.2.1990 - 28.2.1990
			NUnit.Framework.Assert.AreEqual(range1.Contains(new SUTime.IsoDate(1990, 2, 1)), true);
			NUnit.Framework.Assert.AreEqual(range1.Contains(new SUTime.IsoDate(1990, 2, 2)), true);
			NUnit.Framework.Assert.AreEqual(range1.Contains(new SUTime.IsoDate(1990, 1, 2)), false);
			NUnit.Framework.Assert.AreEqual(range1.Contains(new SUTime.IsoDate(1990, 3, 1)), false);
		}
	}
}
