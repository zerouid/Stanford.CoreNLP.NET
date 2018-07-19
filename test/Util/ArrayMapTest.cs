using System.Collections.Generic;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Tests the ArrayMap class by running it through some standard
	/// map operations.
	/// </summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ArrayMapTest
	{
		internal ArrayMap<string, int> map;

		internal Dictionary<string, int> hmap;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			map = new ArrayMap<string, int>();
			hmap = new Dictionary<string, int>();
			map["Foo"] = 5;
			map["Bar"] = 50;
			map["Baz"] = 500;
			hmap["Foo"] = 5;
			hmap["Bar"] = 50;
			hmap["Baz"] = 500;
		}

		[NUnit.Framework.Test]
		public virtual void TestEquals()
		{
			NUnit.Framework.Assert.AreEqual(map, map);
			NUnit.Framework.Assert.IsTrue(map.Equals(map));
			NUnit.Framework.Assert.AreEqual(map, hmap);
			NUnit.Framework.Assert.AreEqual(hmap, map);
		}

		[NUnit.Framework.Test]
		public virtual void TestClear()
		{
			NUnit.Framework.Assert.IsFalse(map.IsEmpty());
			map.Clear();
			NUnit.Framework.Assert.IsTrue(map.IsEmpty());
			map["aaa"] = 5;
			NUnit.Framework.Assert.AreEqual(1, map.Count);
		}

		[NUnit.Framework.Test]
		public virtual void TestPutAll()
		{
			map.Clear();
			NUnit.Framework.Assert.IsTrue(map.IsEmpty());
			map.PutAll(hmap);
			TestEquals();
			Dictionary<string, int> newmap = new Dictionary<string, int>();
			newmap.PutAll(map);
			NUnit.Framework.Assert.AreEqual(newmap, map);
			NUnit.Framework.Assert.AreEqual(map, newmap);
		}

		[NUnit.Framework.Test]
		public virtual void TestEntrySet()
		{
			ICollection<KeyValuePair<string, int>> entries = map;
			KeyValuePair<string, int> entry = entries.GetEnumerator().Current;
			entries.Remove(entry);
			NUnit.Framework.Assert.IsFalse(map.Contains(entry.Key));
			NUnit.Framework.Assert.AreEqual(2, map.Count);
			entries.Clear();
			NUnit.Framework.Assert.AreEqual(0, map.Count);
			NUnit.Framework.Assert.IsTrue(map.IsEmpty());
		}

		[NUnit.Framework.Test]
		public virtual void TestValues()
		{
			ICollection<int> hmapValues = new HashSet<int>();
			Sharpen.Collections.AddAll(hmapValues, hmap.Values);
			ICollection<int> mapValues = new HashSet<int>();
			Sharpen.Collections.AddAll(mapValues, map.Values);
			NUnit.Framework.Assert.AreEqual(hmapValues, mapValues);
		}

		[NUnit.Framework.Test]
		public virtual void TestPutDuplicateValues()
		{
			map.Clear();
			map["Foo"] = 6;
			NUnit.Framework.Assert.AreEqual(6, map["Foo"]);
			NUnit.Framework.Assert.AreEqual(1, map.Count);
			map["Foo"] = 5;
			NUnit.Framework.Assert.AreEqual(5, map["Foo"]);
			NUnit.Framework.Assert.AreEqual(1, map.Count);
		}
	}
}
