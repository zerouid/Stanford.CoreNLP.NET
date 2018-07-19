using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Concurrent
{
	/// <author>Spence Green</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ConcurrentHashIndexTest
	{
		protected internal IIndex<string> index;

		protected internal IIndex<string> index2;

		protected internal IIndex<string> index3;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			index = new ConcurrentHashIndex<string>();
			index.Add("The");
			index.Add("Beast");
			index2 = new ConcurrentHashIndex<string>();
			index2.Add("Beauty");
			index2.Add("And");
			index2.Add("The");
			index2.Add("Beast");
			index3 = new ConcurrentHashIndex<string>();
			index3.Add("Markov");
			index3.Add("The");
			index3.Add("Beast");
		}

		[NUnit.Framework.Test]
		public virtual void TestSize()
		{
			NUnit.Framework.Assert.AreEqual(2, index.Size());
		}

		[NUnit.Framework.Test]
		public virtual void TestGet()
		{
			NUnit.Framework.Assert.AreEqual(2, index.Size());
			NUnit.Framework.Assert.AreEqual("The", index.Get(0));
			NUnit.Framework.Assert.AreEqual("Beast", index.Get(1));
		}

		[NUnit.Framework.Test]
		public virtual void TestIndexOf()
		{
			NUnit.Framework.Assert.AreEqual(2, index.Size());
			NUnit.Framework.Assert.AreEqual(0, index.IndexOf("The"));
			NUnit.Framework.Assert.AreEqual(1, index.IndexOf("Beast"));
		}

		[NUnit.Framework.Test]
		public virtual void TestIterator()
		{
			IEnumerator<string> i = index.GetEnumerator();
			NUnit.Framework.Assert.AreEqual("The", i.Current);
			NUnit.Framework.Assert.AreEqual("Beast", i.Current);
			NUnit.Framework.Assert.AreEqual(false, i.MoveNext());
		}

		/*
		public void testRemove() {
		index2.remove("Sebastian");
		index2.remove("Beast");
		assertEquals(3, index2.size());
		assertEquals(0, index2.indexOf("Beauty"));
		assertEquals(1, index2.indexOf("And"));
		assertEquals(3, index2.indexOf("Beast"));
		index2.removeAll(index3.objectsList());
		}
		*/
		[NUnit.Framework.Test]
		public virtual void TestToArray()
		{
			string[] strs = new string[2];
			strs = Sharpen.Collections.ToArray(index.ObjectsList(), strs);
			NUnit.Framework.Assert.AreEqual(2, strs.Length);
			NUnit.Framework.Assert.IsTrue(index.Contains(strs[0]));
			NUnit.Framework.Assert.IsTrue(index.Contains(strs[1]));
		}

		[NUnit.Framework.Test]
		public virtual void TestObjects()
		{
			IList<string> foo = (IList<string>)index2.Objects(new int[] { 0, 3 });
			NUnit.Framework.Assert.AreEqual("Beauty", foo[0]);
			NUnit.Framework.Assert.AreEqual("Beast", foo[1]);
		}
	}
}
