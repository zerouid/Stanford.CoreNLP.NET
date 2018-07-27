

namespace Edu.Stanford.Nlp.Stats
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TwoDimensionalCounterTest
	{
		private TwoDimensionalCounter<string, string> c;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			c = new TwoDimensionalCounter<string, string>();
			c.SetCount("a", "a", 1.0);
			c.SetCount("a", "b", 2.0);
			c.SetCount("a", "c", 3.0);
			c.SetCount("b", "a", 4.0);
			c.SetCount("b", "b", 5.0);
			c.SetCount("c", "a", 6.0);
		}

		[NUnit.Framework.Test]
		public virtual void TestTotalCount()
		{
			NUnit.Framework.Assert.AreEqual(c.TotalCount(), 21.0);
		}

		[NUnit.Framework.Test]
		public virtual void TestSetCount()
		{
			NUnit.Framework.Assert.AreEqual(c.TotalCount(), 21.0);
			c.SetCount("p", "q", 1.0);
			NUnit.Framework.Assert.AreEqual(c.TotalCount(), 22.0);
			NUnit.Framework.Assert.AreEqual(c.TotalCount("p"), 1.0);
			NUnit.Framework.Assert.AreEqual(c.GetCount("p", "q"), 1.0);
			c.Remove("p", "q");
		}

		[NUnit.Framework.Test]
		public virtual void TestIncrement()
		{
			NUnit.Framework.Assert.AreEqual(c.TotalCount(), 21.0);
			NUnit.Framework.Assert.AreEqual(c.GetCount("b", "b"), 5.0);
			NUnit.Framework.Assert.AreEqual(c.TotalCount("b"), 9.0);
			c.IncrementCount("b", "b", 2.0);
			NUnit.Framework.Assert.AreEqual(c.GetCount("b", "b"), 7.0);
			NUnit.Framework.Assert.AreEqual(c.TotalCount("b"), 11.0);
			NUnit.Framework.Assert.AreEqual(c.TotalCount(), 23.0);
			c.IncrementCount("b", "b", -2.0);
			NUnit.Framework.Assert.AreEqual(c.GetCount("b", "b"), 5.0);
			NUnit.Framework.Assert.AreEqual(c.TotalCount("b"), 9.0);
			NUnit.Framework.Assert.AreEqual(c.TotalCount(), 21.0);
		}
	}
}
