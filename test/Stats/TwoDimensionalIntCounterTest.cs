using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TwoDimensionalIntCounterTest
	{
		[NUnit.Framework.Test]
		public virtual void TestTraditionalMain()
		{
			TwoDimensionalIntCounter<string, string> cc = new TwoDimensionalIntCounter<string, string>();
			cc.SetCount("a", "c", 1.0);
			cc.SetCount("b", "c", 1.0);
			cc.SetCount("a", "d", 1.0);
			cc.SetCount("a", "d", -1.0);
			cc.SetCount("b", "d", 1.0);
			NUnit.Framework.Assert.AreEqual("Error in counter setup", 1.0, cc.GetCount("a", "c"), 1e-8);
			NUnit.Framework.Assert.AreEqual("Error in counter setup", 1.0, cc.GetCount("b", "c"), 1e-8);
			NUnit.Framework.Assert.AreEqual("Error in counter setup", -1.0, cc.GetCount("a", "d"), 1e-8);
			NUnit.Framework.Assert.AreEqual("Error in counter setup", 1.0, cc.GetCount("b", "d"), 1e-8);
			NUnit.Framework.Assert.AreEqual("Error in counter setup", 0.0, cc.GetCount("a", "a"), 1e-8);
			cc.IncrementCount("b", "d", 1.0);
			NUnit.Framework.Assert.AreEqual("Error in counter increment", -1.0, cc.GetCount("a", "d"), 1e-8);
			NUnit.Framework.Assert.AreEqual("Error in counter increment", 2.0, cc.GetCount("b", "d"), 1e-8);
			NUnit.Framework.Assert.AreEqual("Error in counter increment", 0.0, cc.GetCount("a", "a"), 1e-8);
			TwoDimensionalIntCounter<string, string> cc2 = TwoDimensionalIntCounter.ReverseIndexOrder(cc);
			NUnit.Framework.Assert.AreEqual("Error in counter reverseIndexOrder", 1.0, cc2.GetCount("c", "a"), 1e-8);
			NUnit.Framework.Assert.AreEqual("Error in counter reverseIndexOrder", 1.0, cc2.GetCount("c", "b"), 1e-8);
			NUnit.Framework.Assert.AreEqual("Error in counter reverseIndexOrder", -1.0, cc2.GetCount("d", "a"), 1e-8);
			NUnit.Framework.Assert.AreEqual("Error in counter reverseIndexOrder", 2.0, cc2.GetCount("d", "b"), 1e-8);
			NUnit.Framework.Assert.AreEqual("Error in counter reverseIndexOrder", 0.0, cc2.GetCount("a", "a"), 1e-8);
			NUnit.Framework.Assert.AreEqual("Error in counter reverseIndexOrder", 0.0, cc2.GetCount("a", "c"), 1e-8);
		}
	}
}
