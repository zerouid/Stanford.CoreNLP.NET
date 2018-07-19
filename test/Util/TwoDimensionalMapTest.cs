using System.Collections.Generic;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Tests the 2-D hash map in various ways</summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TwoDimensionalMapTest
	{
		[NUnit.Framework.Test]
		public virtual void TestBasicOperations()
		{
			TwoDimensionalMap<string, string, string> map = new TwoDimensionalMap<string, string, string>();
			NUnit.Framework.Assert.AreEqual(0, map.Size());
			NUnit.Framework.Assert.IsTrue(map.IsEmpty());
			map.Put("A", "B", "C");
			NUnit.Framework.Assert.AreEqual("C", map.Get("A", "B"));
			NUnit.Framework.Assert.AreEqual(1, map.Size());
			NUnit.Framework.Assert.IsFalse(map.IsEmpty());
			NUnit.Framework.Assert.IsTrue(map.Contains("A", "B"));
			NUnit.Framework.Assert.IsFalse(map.Contains("A", "C"));
			NUnit.Framework.Assert.IsFalse(map.Contains("B", "F"));
			map.Put("A", "B", "D");
			NUnit.Framework.Assert.AreEqual("D", map.Get("A", "B"));
			NUnit.Framework.Assert.AreEqual(1, map.Size());
			NUnit.Framework.Assert.IsFalse(map.IsEmpty());
			NUnit.Framework.Assert.IsTrue(map.Contains("A", "B"));
			NUnit.Framework.Assert.IsFalse(map.Contains("A", "C"));
			NUnit.Framework.Assert.IsFalse(map.Contains("B", "F"));
			map.Put("A", "C", "E");
			NUnit.Framework.Assert.AreEqual("D", map.Get("A", "B"));
			NUnit.Framework.Assert.AreEqual("E", map.Get("A", "C"));
			NUnit.Framework.Assert.AreEqual(2, map.Size());
			NUnit.Framework.Assert.IsFalse(map.IsEmpty());
			NUnit.Framework.Assert.IsTrue(map.Contains("A", "B"));
			NUnit.Framework.Assert.IsTrue(map.Contains("A", "C"));
			NUnit.Framework.Assert.IsFalse(map.Contains("B", "F"));
			map.Put("B", "F", "G");
			NUnit.Framework.Assert.AreEqual("D", map.Get("A", "B"));
			NUnit.Framework.Assert.AreEqual("E", map.Get("A", "C"));
			NUnit.Framework.Assert.AreEqual("G", map.Get("B", "F"));
			NUnit.Framework.Assert.AreEqual(3, map.Size());
			NUnit.Framework.Assert.IsFalse(map.IsEmpty());
			NUnit.Framework.Assert.IsTrue(map.Contains("A", "B"));
			NUnit.Framework.Assert.IsTrue(map.Contains("A", "C"));
			NUnit.Framework.Assert.IsTrue(map.Contains("B", "F"));
			map.Clear();
			NUnit.Framework.Assert.AreEqual(0, map.Size());
			NUnit.Framework.Assert.IsTrue(map.IsEmpty());
		}

		/// <summary>Test that basic operations on a TwoDimensionalMap iterator work.</summary>
		[NUnit.Framework.Test]
		public virtual void TestBasicIterator()
		{
			TwoDimensionalMap<string, string, string> map = new TwoDimensionalMap<string, string, string>();
			IEnumerator<TwoDimensionalMap.Entry<string, string, string>> mapIterator = map.GetEnumerator();
			NUnit.Framework.Assert.IsFalse(mapIterator.MoveNext());
			map.Put("A", "B", "C");
			mapIterator = map.GetEnumerator();
			NUnit.Framework.Assert.IsTrue(mapIterator.MoveNext());
			TwoDimensionalMap.Entry<string, string, string> entry = mapIterator.Current;
			NUnit.Framework.Assert.AreEqual("A", entry.GetFirstKey());
			NUnit.Framework.Assert.AreEqual("B", entry.GetSecondKey());
			NUnit.Framework.Assert.AreEqual("C", entry.GetValue());
			NUnit.Framework.Assert.IsFalse(mapIterator.MoveNext());
			map.Put("A", "E", "F");
			map.Put("D", "E", "F");
			map.Put("G", "H", "I");
			map.Put("J", "K", "L");
			NUnit.Framework.Assert.AreEqual(5, map.Size());
			int count = 0;
			ICollection<string> firstKeys = new HashSet<string>();
			ICollection<string> values = new HashSet<string>();
			foreach (TwoDimensionalMap.Entry<string, string, string> e in map)
			{
				++count;
				firstKeys.Add(e.GetFirstKey());
				values.Add(e.GetValue());
			}
			NUnit.Framework.Assert.IsTrue(firstKeys.Contains("A"));
			NUnit.Framework.Assert.IsTrue(firstKeys.Contains("D"));
			NUnit.Framework.Assert.IsTrue(firstKeys.Contains("G"));
			NUnit.Framework.Assert.IsTrue(firstKeys.Contains("J"));
			NUnit.Framework.Assert.IsTrue(values.Contains("C"));
			NUnit.Framework.Assert.IsTrue(values.Contains("F"));
			NUnit.Framework.Assert.IsTrue(values.Contains("I"));
			NUnit.Framework.Assert.IsTrue(values.Contains("L"));
			NUnit.Framework.Assert.AreEqual(5, count);
			NUnit.Framework.Assert.AreEqual(4, firstKeys.Count);
			NUnit.Framework.Assert.AreEqual(4, values.Count);
		}

		/// <summary>Tests that a different map factory is used when asked for.</summary>
		/// <remarks>
		/// Tests that a different map factory is used when asked for.  An
		/// identity map will store two of the same key if the objects
		/// themselves are different.  We can test for that.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestMapFactory()
		{
			TwoDimensionalMap<string, string, string> map = new TwoDimensionalMap<string, string, string>(MapFactory.IdentityHashMapFactory<string, IDictionary<string, string>>(), MapFactory.IdentityHashMapFactory<string, string>());
			map.Put(new string("A"), "B", "C");
			map.Put(new string("A"), "B", "C");
			NUnit.Framework.Assert.AreEqual(2, map.Size());
		}

		/// <summary>
		/// Now that we know the MapFactory constructor should work and the
		/// iterator should work, we can really test both by using a TreeMap
		/// and checking that the iterated elements are sorted
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestTreeMapIterator()
		{
			TwoDimensionalMap<string, string, string> map = new TwoDimensionalMap<string, string, string>(MapFactory.TreeMapFactory<string, IDictionary<string, string>>(), MapFactory.TreeMapFactory<string, string>());
			map.Put("A", "B", "C");
			map.Put("Z", "Y", "X");
			map.Put("Z", "B", "C");
			map.Put("A", "Y", "X");
			map.Put("D", "D", "D");
			map.Put("D", "F", "E");
			map.Put("K", "G", "B");
			map.Put("G", "F", "E");
			map.Put("D", "D", "E");
			// sneaky overwritten entry
			NUnit.Framework.Assert.AreEqual(8, map.Size());
			IEnumerator<TwoDimensionalMap.Entry<string, string, string>> mapIterator = map.GetEnumerator();
			TwoDimensionalMap.Entry<string, string, string> entry = mapIterator.Current;
			NUnit.Framework.Assert.AreEqual("A", entry.GetFirstKey());
			NUnit.Framework.Assert.AreEqual("B", entry.GetSecondKey());
			NUnit.Framework.Assert.AreEqual("C", entry.GetValue());
			entry = mapIterator.Current;
			NUnit.Framework.Assert.AreEqual("A", entry.GetFirstKey());
			NUnit.Framework.Assert.AreEqual("Y", entry.GetSecondKey());
			NUnit.Framework.Assert.AreEqual("X", entry.GetValue());
			entry = mapIterator.Current;
			NUnit.Framework.Assert.AreEqual("D", entry.GetFirstKey());
			NUnit.Framework.Assert.AreEqual("D", entry.GetSecondKey());
			NUnit.Framework.Assert.AreEqual("E", entry.GetValue());
			entry = mapIterator.Current;
			NUnit.Framework.Assert.AreEqual("D", entry.GetFirstKey());
			NUnit.Framework.Assert.AreEqual("F", entry.GetSecondKey());
			NUnit.Framework.Assert.AreEqual("E", entry.GetValue());
			entry = mapIterator.Current;
			NUnit.Framework.Assert.AreEqual("G", entry.GetFirstKey());
			NUnit.Framework.Assert.AreEqual("F", entry.GetSecondKey());
			NUnit.Framework.Assert.AreEqual("E", entry.GetValue());
			entry = mapIterator.Current;
			NUnit.Framework.Assert.AreEqual("K", entry.GetFirstKey());
			NUnit.Framework.Assert.AreEqual("G", entry.GetSecondKey());
			NUnit.Framework.Assert.AreEqual("B", entry.GetValue());
			entry = mapIterator.Current;
			NUnit.Framework.Assert.AreEqual("Z", entry.GetFirstKey());
			NUnit.Framework.Assert.AreEqual("B", entry.GetSecondKey());
			NUnit.Framework.Assert.AreEqual("C", entry.GetValue());
			entry = mapIterator.Current;
			NUnit.Framework.Assert.AreEqual("Z", entry.GetFirstKey());
			NUnit.Framework.Assert.AreEqual("Y", entry.GetSecondKey());
			NUnit.Framework.Assert.AreEqual("X", entry.GetValue());
			NUnit.Framework.Assert.IsFalse(mapIterator.MoveNext());
			IEnumerator<string> valueIterator = map.ValueIterator();
			NUnit.Framework.Assert.IsTrue(valueIterator.MoveNext());
			NUnit.Framework.Assert.AreEqual("C", valueIterator.Current);
			NUnit.Framework.Assert.AreEqual("X", valueIterator.Current);
			NUnit.Framework.Assert.AreEqual("E", valueIterator.Current);
			NUnit.Framework.Assert.AreEqual("E", valueIterator.Current);
			NUnit.Framework.Assert.AreEqual("E", valueIterator.Current);
			NUnit.Framework.Assert.AreEqual("B", valueIterator.Current);
			NUnit.Framework.Assert.AreEqual("C", valueIterator.Current);
			NUnit.Framework.Assert.AreEqual("X", valueIterator.Current);
			NUnit.Framework.Assert.IsFalse(valueIterator.MoveNext());
		}

		/// <summary>Tests that addAll works.</summary>
		/// <remarks>Tests that addAll works.  Also includes a sneaky equals() test</remarks>
		[NUnit.Framework.Test]
		public virtual void TestAddAll()
		{
			TwoDimensionalMap<string, string, string> m1 = TwoDimensionalMap.HashMap();
			m1.Put("A", "B", "1");
			m1.Put("Z", "Y", "2");
			m1.Put("Z", "B", "3");
			m1.Put("A", "Y", "4");
			m1.Put("D", "D", "5");
			m1.Put("D", "F", "6");
			m1.Put("K", "G", "7");
			m1.Put("G", "F", "8");
			TwoDimensionalMap<string, string, string> m2 = TwoDimensionalMap.TreeMap();
			m2.AddAll(m1, Functions.IdentityFunction<string>());
			NUnit.Framework.Assert.AreEqual(m1, m2);
			IFunction<string, int> valueOf = null;
			TwoDimensionalMap<string, string, int> m3 = TwoDimensionalMap.HashMap();
			m3.AddAll(m1, valueOf);
			NUnit.Framework.Assert.AreEqual(m1.Size(), m3.Size());
			NUnit.Framework.Assert.AreEqual(3, m3.Get("Z", "B"));
		}
	}
}
