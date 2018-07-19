using Java.Util.Concurrent.Atomic;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A simple test for the
	/// <see cref="Lazy{E}"/>
	/// class.
	/// </summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class LazyTest
	{
		[Test]
		public virtual void TestFrom()
		{
			Lazy<string> x = Lazy.From("foo");
			NUnit.Framework.Assert.AreEqual("foo", x.GetIfDefined());
			NUnit.Framework.Assert.AreEqual("foo", x.Get());
		}

		[Test]
		public virtual void TestOf()
		{
			Lazy<string> x = Lazy.Of(null);
			NUnit.Framework.Assert.AreEqual(null, x.GetIfDefined());
			NUnit.Framework.Assert.AreEqual("foo", x.Get());
		}

		[Test]
		public virtual void TestCached()
		{
			Lazy<string> x = Lazy.Cache(null);
			NUnit.Framework.Assert.AreEqual(null, x.GetIfDefined());
			NUnit.Framework.Assert.AreEqual("foo", x.Get());
		}

		[Test]
		public virtual void TestOfCalledOnlyOnce()
		{
			AtomicInteger callCount = new AtomicInteger(0);
			Lazy<string> x = Lazy.Of(null);
			NUnit.Framework.Assert.AreEqual(null, x.GetIfDefined());
			NUnit.Framework.Assert.AreEqual("foo", x.Get());
			x.SimulateGC();
			NUnit.Framework.Assert.AreEqual("foo", x.Get());
			NUnit.Framework.Assert.AreEqual(1, callCount.Get());
		}

		[Test]
		public virtual void TestCachedCalledOnlyOnce()
		{
			AtomicInteger callCount = new AtomicInteger(0);
			Lazy<string> x = Lazy.Cache(null);
			NUnit.Framework.Assert.AreEqual(null, x.GetIfDefined());
			NUnit.Framework.Assert.AreEqual("foo", x.Get());
			NUnit.Framework.Assert.AreEqual("foo", x.Get());
			NUnit.Framework.Assert.AreEqual(1, callCount.Get());
		}

		[Test]
		public virtual void TestCachedGC()
		{
			AtomicInteger callCount = new AtomicInteger(0);
			Lazy<string> x = Lazy.Cache(null);
			NUnit.Framework.Assert.AreEqual(null, x.GetIfDefined());
			NUnit.Framework.Assert.AreEqual("foo", x.Get());
			x.SimulateGC();
			NUnit.Framework.Assert.AreEqual("foo", x.Get());
			NUnit.Framework.Assert.AreEqual(2, callCount.Get());
		}
	}
}
