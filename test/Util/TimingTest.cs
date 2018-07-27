using System;





namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// It seems like because of the way junit parallelizes tests that you just can't
	/// test timing to any degree of accuracy.
	/// </summary>
	/// <remarks>
	/// It seems like because of the way junit parallelizes tests that you just can't
	/// test timing to any degree of accuracy. So just try to make sure we're not
	/// off by an order of magnitude.
	/// </remarks>
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TimingTest
	{
		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			Locale.SetDefault(Locale.Us);
		}

		/// <summary>There's a lot of time slop in these tests so they don't fire by mistake.</summary>
		/// <remarks>
		/// There's a lot of time slop in these tests so they don't fire by mistake.
		/// You definitely get them more than 50% off sometimes. :(
		/// And then we got a test failure that was over 70% off on the first test. :(
		/// So, really this only tests that the answers are right to an order of magnitude.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestTiming()
		{
			Timing t = new Timing();
			SleepTen();
			long val2 = t.ReportNano();
			NUnit.Framework.Assert.IsTrue(string.Format("Wrong nanosleep %d", val2), val2 < 30_000_000);
			NUnit.Framework.Assert.IsTrue(string.Format("Wrong nanosleep %d", val2), val2 > 3_000_000);
			SleepTen();
			long val = t.Report();
			// System.err.println(val);
			NUnit.Framework.Assert.AreEqual("Wrong sleep", 20, val, 20);
			for (int i = 0; i < 8; i++)
			{
				SleepTen();
			}
			long val3 = t.Report();
			NUnit.Framework.Assert.AreEqual("Wrong formatted time", new DecimalFormat("0.0").Format(0.1), Timing.ToSecondsString(val3));
		}

		private static void SleepTen()
		{
			try
			{
				Thread.Sleep(10);
			}
			catch (Exception)
			{
			}
		}
		// do nothing
	}
}
