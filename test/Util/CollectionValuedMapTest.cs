using System.Collections.Generic;
using Java.Util;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	[NUnit.Framework.TestFixture]
	public class CollectionValuedMapTest
	{
		/// <summary>
		/// Tests add(), isEmpty(), get(), keySet(), values(), allValues(), entrySet(),
		/// containsKey(), remove(), and clear().
		/// </summary>
		[Test]
		public virtual void TestBasicOperations()
		{
			CollectionValuedMap<string, int> cvm = new CollectionValuedMap<string, int>();
			NUnit.Framework.Assert.IsTrue(cvm.IsEmpty());
			cvm.Add("key1", 1);
			cvm.Add("key1", 2);
			cvm.Add("key1", 3);
			cvm.Add("key2", 4);
			cvm.Add("key3", 7);
			NUnit.Framework.Assert.AreEqual(cvm["key1"].Count, 3);
			NUnit.Framework.Assert.AreEqual(cvm["key2"].Count, 1);
			NUnit.Framework.Assert.AreEqual(cvm["keyX"].Count, 0);
			NUnit.Framework.Assert.AreEqual(cvm.Keys.Count, 3);
			NUnit.Framework.Assert.AreEqual(cvm.Values.Count, 3);
			NUnit.Framework.Assert.AreEqual(cvm.Count, 3);
			NUnit.Framework.Assert.AreEqual(cvm.Count, 3);
			ICollection<int> allValues = cvm.AllValues();
			NUnit.Framework.Assert.AreEqual(allValues.Count, 5);
			NUnit.Framework.Assert.IsTrue(cvm.Contains("key1"));
			NUnit.Framework.Assert.IsTrue(cvm.Contains("key2"));
			NUnit.Framework.Assert.IsTrue(cvm.Contains("key3"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("keyX"));
			NUnit.Framework.Assert.IsTrue(allValues.Contains(1));
			NUnit.Framework.Assert.IsTrue(allValues.Contains(2));
			NUnit.Framework.Assert.IsTrue(allValues.Contains(3));
			NUnit.Framework.Assert.IsTrue(allValues.Contains(4));
			NUnit.Framework.Assert.IsFalse(allValues.Contains(5));
			NUnit.Framework.Assert.IsFalse(cvm.IsEmpty());
			Sharpen.Collections.Remove(cvm, "key3");
			NUnit.Framework.Assert.IsTrue(cvm.Contains("key1"));
			NUnit.Framework.Assert.IsTrue(cvm.Contains("key2"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("key3"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("keyX"));
			NUnit.Framework.Assert.AreEqual(cvm.Count, 2);
			NUnit.Framework.Assert.AreEqual(cvm.Count, 2);
			NUnit.Framework.Assert.AreEqual(cvm.AllValues().Count, 4);
			NUnit.Framework.Assert.AreEqual(cvm.Keys.Count, 2);
			NUnit.Framework.Assert.AreEqual(cvm.Values.Count, 2);
			Sharpen.Collections.Remove(cvm, "keyX");
			// removing a non-existing key
			NUnit.Framework.Assert.IsTrue(cvm.Contains("key1"));
			NUnit.Framework.Assert.IsTrue(cvm.Contains("key2"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("key3"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("keyX"));
			NUnit.Framework.Assert.AreEqual(cvm.Count, 2);
			NUnit.Framework.Assert.AreEqual(cvm.Count, 2);
			NUnit.Framework.Assert.AreEqual(cvm.AllValues().Count, 4);
			NUnit.Framework.Assert.AreEqual(cvm.Keys.Count, 2);
			NUnit.Framework.Assert.AreEqual(cvm.Values.Count, 2);
			cvm.Add("key4", 3);
			cvm.RemoveAll(Arrays.AsList("key1", "key4"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("key1"));
			NUnit.Framework.Assert.IsTrue(cvm.Contains("key2"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("key3"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("key4"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("keyX"));
			cvm.Clear();
			NUnit.Framework.Assert.IsFalse(cvm.Contains("key1"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("key2"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("key3"));
			NUnit.Framework.Assert.IsFalse(cvm.Contains("keyX"));
			NUnit.Framework.Assert.AreEqual(cvm.Count, 0);
			NUnit.Framework.Assert.AreEqual(cvm.AllValues().Count, 0);
			NUnit.Framework.Assert.AreEqual(cvm.Count, 0);
			NUnit.Framework.Assert.AreEqual(cvm.Keys.Count, 0);
			NUnit.Framework.Assert.AreEqual(cvm.Values.Count, 0);
		}

		/// <summary>Tests various forms of addAll()/constructors, clone(), and equality.</summary>
		[Test]
		public virtual void TestMergingOperations()
		{
			CollectionValuedMap<string, int> cvm = new CollectionValuedMap<string, int>();
			cvm.Add("key1", 1);
			cvm.Add("key2", 2);
			cvm.Add("key3", 3);
			IDictionary<string, int> map = new Dictionary<string, int>();
			map["key1"] = 1;
			map["key2"] = 2;
			map["key3"] = 3;
			CollectionValuedMap<string, int> cvmFromMap = new CollectionValuedMap<string, int>();
			cvmFromMap.AddAll(map);
			NUnit.Framework.Assert.AreEqual(cvm, cvmFromMap);
			CollectionValuedMap<string, int> cvmFromCvm = new CollectionValuedMap<string, int>(cvm);
			NUnit.Framework.Assert.AreEqual(cvm, cvmFromCvm);
			// CollectionValuedMap<String, Integer> cvmFromClone = cvm.clone();
			// Assert.assertEquals(cvm, cvmFromClone);
			CollectionValuedMap<string, int> cvmToMerge = new CollectionValuedMap<string, int>();
			cvmToMerge.Add("key1", 11);
			cvmToMerge.Add("key5", 55);
			NUnit.Framework.Assert.IsFalse(cvmToMerge.Equals(cvm));
			cvm.AddAll(cvmToMerge);
			CollectionValuedMap<string, int> expectedMerge = new CollectionValuedMap<string, int>();
			expectedMerge.Add("key1", 1);
			expectedMerge.Add("key1", 11);
			expectedMerge.Add("key2", 2);
			expectedMerge.Add("key3", 3);
			expectedMerge.Add("key5", 55);
			NUnit.Framework.Assert.AreEqual(cvm, expectedMerge);
		}

		/// <summary>Tests add/remove (again).</summary>
		[Test]
		public virtual void TestAddRemove()
		{
			CollectionValuedMap<int, int> fooMap = new CollectionValuedMap<int, int>();
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					fooMap.Add(i, j);
				}
			}
			Sharpen.Collections.Remove(fooMap, 2);
			NUnit.Framework.Assert.AreEqual("{0=[0, 1, 2, 3], 1=[0, 1, 2, 3], 3=[0, 1, 2, 3]}", fooMap.ToString());
		}

		/// <summary>Tests add/remove (again).</summary>
		[Test]
		public virtual void TestRandomAddRemoveAndDelta()
		{
			CollectionValuedMap<int, int> originalMap = new CollectionValuedMap<int, int>();
			Random r = new Random();
			for (int i = 0; i < 800; i++)
			{
				int rInt1 = int.Parse(r.NextInt(400));
				int rInt2 = int.Parse(r.NextInt(400));
				originalMap.Add(rInt1, rInt2);
			}
			// System.out.println("Adding " + rInt1 + ' ' + rInt2);
			CollectionValuedMap<int, int> originalCopyMap = new CollectionValuedMap<int, int>(originalMap);
			CollectionValuedMap<int, int> deltaCopyMap = new CollectionValuedMap<int, int>(originalMap);
			CollectionValuedMap<int, int> deltaMap = new DeltaCollectionValuedMap<int, int>(originalMap);
			CollectionValuedMap<int, int> delta2Map = originalMap.DeltaCopy();
			// now make a lot of changes to deltaMap;
			// add and change some stuff
			for (int i_1 = 0; i_1 < 400; i_1++)
			{
				int rInt1 = int.Parse(r.NextInt(400));
				int rInt2 = int.Parse(r.NextInt(400) + 1000);
				deltaMap.Add(rInt1, rInt2);
				delta2Map.Add(rInt1, rInt2);
				deltaCopyMap.Add(rInt1, rInt2);
			}
			// System.out.println("Adding " + rInt1 + ' ' + rInt2);
			// remove some stuff
			for (int i_2 = 0; i_2 < 400; i_2++)
			{
				int rInt1 = int.Parse(r.NextInt(1400));
				int rInt2 = int.Parse(r.NextInt(1400));
				deltaMap.RemoveMapping(rInt1, rInt2);
				delta2Map.RemoveMapping(rInt1, rInt2);
				deltaCopyMap.RemoveMapping(rInt1, rInt2);
			}
			// System.out.println("Removing " + rInt1 + ' ' + rInt2);
			// System.out.println("original: " + originalMap);
			// System.out.println("orig cop: " + originalCopyMap);
			// System.out.println("dcopy: " + deltaCopyMap);
			// System.out.println("delta: " + deltaMap);
			NUnit.Framework.Assert.AreEqual(originalMap, originalCopyMap, "Copy map not identical");
			NUnit.Framework.Assert.AreEqual(deltaCopyMap, deltaMap, "Delta map not equal to copy");
			NUnit.Framework.Assert.AreEqual(deltaCopyMap, delta2Map, "Delta2Map not equal to copy");
		}
	}
}
