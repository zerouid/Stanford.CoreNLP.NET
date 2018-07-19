using System;
using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <author>Sebastian Riedel</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class HashIndexTest
	{
		protected internal IIndex<string> index;

		protected internal IIndex<string> index2;

		protected internal IIndex<string> index3;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			index = new HashIndex<string>();
			index.Add("The");
			index.Add("Beast");
			index2 = new HashIndex<string>();
			index2.Add("Beauty");
			index2.Add("And");
			index2.Add("The");
			index2.Add("Beast");
			index3 = new HashIndex<string>();
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
			NUnit.Framework.Assert.AreEqual("The", strs[0]);
			NUnit.Framework.Assert.AreEqual("Beast", strs[1]);
			NUnit.Framework.Assert.AreEqual(2, strs.Length);
		}

		[NUnit.Framework.Test]
		public virtual void TestUnmodifiableViewEtc()
		{
			IList<string> list = new List<string>();
			list.Add("A");
			list.Add("B");
			list.Add("A");
			list.Add("C");
			HashIndex<string> index4 = new HashIndex<string>(list);
			HashIndex<string> index5 = new HashIndex<string>();
			Sharpen.Collections.AddAll(index5, list);
			NUnit.Framework.Assert.AreEqual("Equality failure", index4, index5);
			index5.AddToIndex("D");
			index5.AddToIndex("E");
			index5.IndexOf("F");
			Sharpen.Collections.AddAll(index5, list);
			NUnit.Framework.Assert.AreEqual(5, index5.Count);
			NUnit.Framework.Assert.AreEqual(3, index4.Count);
			NUnit.Framework.Assert.IsTrue(index4.Contains("A"));
			NUnit.Framework.Assert.AreEqual(0, index4.IndexOf("A"));
			NUnit.Framework.Assert.AreEqual(1, index4.IndexOf("B"));
			NUnit.Framework.Assert.AreEqual(2, index4.IndexOf("C"));
			NUnit.Framework.Assert.AreEqual("A", index4.Get(0));
			IIndex<string> index4u = index4.UnmodifiableView();
			NUnit.Framework.Assert.AreEqual(3, index4u.Size());
			NUnit.Framework.Assert.IsTrue(index4u.Contains("A"));
			NUnit.Framework.Assert.AreEqual(0, index4u.IndexOf("A"));
			NUnit.Framework.Assert.AreEqual(1, index4u.IndexOf("B"));
			NUnit.Framework.Assert.AreEqual(2, index4u.IndexOf("C"));
			NUnit.Framework.Assert.AreEqual("A", index4u.Get(0));
			NUnit.Framework.Assert.AreEqual(-1, index4u.AddToIndex("D"));
			bool okay = false;
			try
			{
				index4u.Unlock();
			}
			catch (NotSupportedException)
			{
				okay = true;
			}
			finally
			{
				NUnit.Framework.Assert.IsTrue(okay);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestCopyConstructor()
		{
			IIndex<string> test = new HashIndex<string>();
			test.Add("Beauty");
			test.Add("And");
			test.Add("The");
			test.Add("Beast");
			HashIndex<string> copy = new HashIndex<string>(test);
			NUnit.Framework.Assert.AreEqual(test, copy);
		}
	}
}
