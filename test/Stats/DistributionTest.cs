

namespace Edu.Stanford.Nlp.Stats
{
	/// <author>lmthang</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class DistributionTest
	{
		[NUnit.Framework.Test]
		public virtual void TestGetDistributionFromLogValues()
		{
			ICounter<string> c1 = new ClassicCounter<string>();
			c1.SetCount("p", 1.0);
			c1.SetCount("q", 2.0);
			c1.SetCount("r", 3.0);
			c1.SetCount("s", 4.0);
			// take log
			Counters.LogInPlace(c1);
			// now call distribution
			Distribution<string> distribution = Distribution.GetDistributionFromLogValues(c1);
			// test
			NUnit.Framework.Assert.AreEqual(distribution.KeySet().Count, 4);
			// size
			// keys
			NUnit.Framework.Assert.AreEqual(distribution.ContainsKey("p"), true);
			NUnit.Framework.Assert.AreEqual(distribution.ContainsKey("q"), true);
			NUnit.Framework.Assert.AreEqual(distribution.ContainsKey("r"), true);
			NUnit.Framework.Assert.AreEqual(distribution.ContainsKey("s"), true);
			// values
			NUnit.Framework.Assert.AreEqual(distribution.GetCount("p"), 1.0E-1, 1E-10);
			NUnit.Framework.Assert.AreEqual(distribution.GetCount("q"), 2.0E-1, 1E-10);
			NUnit.Framework.Assert.AreEqual(distribution.GetCount("r"), 3.0E-1, 1E-10);
			NUnit.Framework.Assert.AreEqual(distribution.GetCount("s"), 4.0E-1, 1E-10);
		}
	}
}
