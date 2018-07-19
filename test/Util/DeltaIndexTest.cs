using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class DeltaIndexTest
	{
		internal HashIndex<string> underlying;

		internal DeltaIndex<string> spillover;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			underlying = new HashIndex<string>();
			underlying.Add("foo0");
			underlying.Add("foo1");
			underlying.Add("foo2");
			underlying.Add("foo3");
			underlying.Add("foo4");
			NUnit.Framework.Assert.AreEqual(5, underlying.Count);
			spillover = new DeltaIndex<string>(underlying);
			spillover.Add("foo1");
			spillover.Add("foo5");
			spillover.Add("foo6");
		}

		[NUnit.Framework.Test]
		public virtual void TestSize()
		{
			NUnit.Framework.Assert.AreEqual(5, underlying.Count);
			NUnit.Framework.Assert.AreEqual(7, spillover.Count);
		}

		[NUnit.Framework.Test]
		public virtual void TestContains()
		{
			NUnit.Framework.Assert.IsTrue(underlying.Contains("foo1"));
			NUnit.Framework.Assert.IsFalse(underlying.Contains("foo5"));
			NUnit.Framework.Assert.IsFalse(underlying.Contains("foo7"));
			NUnit.Framework.Assert.IsTrue(spillover.Contains("foo1"));
			NUnit.Framework.Assert.IsTrue(spillover.Contains("foo5"));
			NUnit.Framework.Assert.IsFalse(spillover.Contains("foo7"));
		}

		[NUnit.Framework.Test]
		public virtual void TestIndex()
		{
			NUnit.Framework.Assert.AreEqual(4, spillover.IndexOf("foo4"));
			NUnit.Framework.Assert.AreEqual(6, spillover.IndexOf("foo6"));
			NUnit.Framework.Assert.AreEqual(-1, spillover.IndexOf("foo7"));
		}

		[NUnit.Framework.Test]
		public virtual void TestGet()
		{
			NUnit.Framework.Assert.AreEqual("foo4", spillover.Get(4));
			NUnit.Framework.Assert.AreEqual("foo5", spillover.Get(5));
			NUnit.Framework.Assert.AreEqual("foo6", spillover.Get(6));
		}
	}
}
