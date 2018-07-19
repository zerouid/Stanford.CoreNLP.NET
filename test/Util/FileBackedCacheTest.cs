using System;
using System.Collections.Generic;
using Java.IO;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Basic tests for the FileBackedCache</summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class FileBackedCacheTest
	{
		[System.Serializable]
		private class CustomHash
		{
			private int unique;

			private int hash;

			private CustomHash(int unique, int hash)
			{
				this.unique = unique;
				this.hash = hash;
			}

			public override int GetHashCode()
			{
				return hash;
			}

			public override bool Equals(object o)
			{
				return (o is FileBackedCacheTest.CustomHash) && ((FileBackedCacheTest.CustomHash)o).unique == unique;
			}

			public override string ToString()
			{
				return "CustomHash(id=" + unique + ", hash=" + hash + ")";
			}
		}

		private FileBackedCache<string, string> cache;

		private FileBackedCache<int, IDictionary<string, List<string>>> mapCache;

		[SetUp]
		public virtual void SetUp()
		{
			try
			{
				// (regular cache)
				File cacheDir = File.CreateTempFile("cache", ".dir");
				cacheDir.Delete();
				cache = new FileBackedCache<string, string>(cacheDir);
				NUnit.Framework.Assert.AreEqual(0, cacheDir.ListFiles().Length);
				// (map cache)
				File mapCacheDir = File.CreateTempFile("cache", ".dir");
				mapCacheDir.Delete();
				mapCache = new FileBackedCache<int, IDictionary<string, List<string>>>(mapCacheDir);
				NUnit.Framework.Assert.AreEqual(0, mapCacheDir.ListFiles().Length);
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		[TearDown]
		public virtual void TearDown()
		{
			if (cache.cacheDir.ListFiles() != null)
			{
				foreach (File c in cache.cacheDir.ListFiles())
				{
					NUnit.Framework.Assert.IsTrue(c.Delete());
				}
				NUnit.Framework.Assert.IsTrue(cache.cacheDir.Delete());
			}
			if (mapCache.cacheDir.ListFiles() != null)
			{
				foreach (File c in mapCache.cacheDir.ListFiles())
				{
					NUnit.Framework.Assert.IsTrue(c.Delete());
				}
				NUnit.Framework.Assert.IsTrue(mapCache.cacheDir.Delete());
			}
		}

		[Test]
		public virtual void TestContainsLocal()
		{
			cache["key"] = "value";
			cache["key2"] = "value2";
			NUnit.Framework.Assert.IsTrue(cache.Contains("key"));
			NUnit.Framework.Assert.IsTrue(cache.Contains("key2"));
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		[Test]
		public virtual void TestGetLocal()
		{
			cache["key"] = "value";
			cache["key2"] = "value2";
			NUnit.Framework.Assert.AreEqual("value", cache["key"]);
			NUnit.Framework.Assert.AreEqual("value2", cache["key2"]);
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		[Test]
		public virtual void TestPutLocal()
		{
			NUnit.Framework.Assert.AreEqual(null, cache["key"] = "value");
			NUnit.Framework.Assert.AreEqual(null, cache["key2"] = "value2");
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		[Test]
		public virtual void TestCacheWritingToDisk()
		{
			NUnit.Framework.Assert.AreEqual(0, cache.cacheDir.ListFiles().Length);
			cache["key"] = "value";
			NUnit.Framework.Assert.AreEqual(1, cache.cacheDir.ListFiles().Length);
			cache["key2"] = "value2";
			NUnit.Framework.Assert.AreEqual(2, cache.cacheDir.ListFiles().Length);
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		[Test]
		public virtual void TestSize()
		{
			NUnit.Framework.Assert.AreEqual(0, cache.SizeInMemory());
			NUnit.Framework.Assert.AreEqual(0, cache.Count);
			cache["key"] = "value";
			cache["key2"] = "value2";
			NUnit.Framework.Assert.AreEqual(2, cache.SizeInMemory());
			// assume no GC
			NUnit.Framework.Assert.AreEqual(2, cache.Count);
			cache.Clear();
			NUnit.Framework.Assert.AreEqual(0, cache.SizeInMemory());
			NUnit.Framework.Assert.AreEqual(2, cache.cacheDir.ListFiles().Length);
			NUnit.Framework.Assert.AreEqual(2, cache.Count);
			NUnit.Framework.Assert.AreEqual(2, cache.SizeInMemory());
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		[Test]
		public virtual void TestContainsFile()
		{
			cache["key"] = "value";
			cache["key2"] = "value2";
			cache.Clear();
			NUnit.Framework.Assert.IsTrue(cache.Contains("key"));
			NUnit.Framework.Assert.IsTrue(cache.Contains("key2"));
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		[Test]
		public virtual void TestContainsRemoveFromMemory()
		{
			cache["key"] = "value";
			cache["key2"] = "value2";
			cache.RemoveFromMemory("key");
			NUnit.Framework.Assert.AreEqual(1, cache.SizeInMemory());
			NUnit.Framework.Assert.IsTrue(cache.Contains("key"));
			NUnit.Framework.Assert.IsTrue(cache.Contains("key2"));
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		[Test]
		public virtual void TestPutFile()
		{
			// Case 1: simple put
			cache["key"] = "value";
			NUnit.Framework.Assert.AreEqual("value", cache["key"] = "valueReplaced");
			NUnit.Framework.Assert.AreEqual("valueReplaced", cache["key"]);
			// Case 2: put then clear
			cache["key"] = "value";
			cache.Clear();
			NUnit.Framework.Assert.AreEqual("value", cache["key"] = "valueReplaced");
			cache.Clear();
			NUnit.Framework.Assert.AreEqual("valueReplaced", cache["key"]);
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		[Test]
		public virtual void TestGetFile()
		{
			cache["key"] = "value";
			cache["key2"] = "value2";
			cache.Clear();
			NUnit.Framework.Assert.AreEqual("value", cache["key"]);
			NUnit.Framework.Assert.AreEqual("value2", cache["key2"]);
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		[Test]
		public virtual void TestIterator()
		{
			cache["key"] = "value";
			cache["key2"] = "value2";
			int count = 0;
			IEnumerator<KeyValuePair<string, string>> iterator = cache.GetEnumerator();
			while (iterator.MoveNext())
			{
				KeyValuePair<string, string> entry = iterator.Current;
				if (entry.Key == "key")
				{
					NUnit.Framework.Assert.AreEqual("value", entry.Value);
				}
				if (entry.Key == "key2")
				{
					NUnit.Framework.Assert.AreEqual("value2", entry.Value);
				}
				count += 1;
			}
			NUnit.Framework.Assert.AreEqual(2, count);
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		[Test]
		public virtual void TestComprehension()
		{
			cache["key"] = "value";
			cache["key2"] = "value2";
			int count = 0;
			foreach (KeyValuePair<string, string> entry in cache)
			{
				if (entry.Key == "key")
				{
					NUnit.Framework.Assert.AreEqual("value", entry.Value);
				}
				if (entry.Key == "key2")
				{
					NUnit.Framework.Assert.AreEqual("value2", entry.Value);
				}
				count += 1;
			}
			NUnit.Framework.Assert.AreEqual(2, count);
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		/// <exception cref="System.IO.IOException"/>
		[Test]
		public virtual void TestCollision()
		{
			// Custom Setup
			File cacheDir = File.CreateTempFile("cache", ".dir");
			cacheDir.Delete();
			FileBackedCache<FileBackedCacheTest.CustomHash, string> myCache = new FileBackedCache<FileBackedCacheTest.CustomHash, string>(cacheDir);
			NUnit.Framework.Assert.AreEqual(0, cacheDir.ListFiles().Length);
			// Test
			myCache[new FileBackedCacheTest.CustomHash(0, 0)] = "zero";
			myCache[new FileBackedCacheTest.CustomHash(1, 0)] = "one";
			myCache[new FileBackedCacheTest.CustomHash(1, 1)] = "one'";
			NUnit.Framework.Assert.AreEqual("zero", myCache[new FileBackedCacheTest.CustomHash(0, 0)]);
			NUnit.Framework.Assert.AreEqual("one", myCache[new FileBackedCacheTest.CustomHash(1, 0)]);
			NUnit.Framework.Assert.AreEqual("one'", myCache[new FileBackedCacheTest.CustomHash(1, 1)]);
			myCache.Clear();
			NUnit.Framework.Assert.AreEqual(0, myCache.SizeInMemory());
			NUnit.Framework.Assert.AreEqual("zero", myCache[new FileBackedCacheTest.CustomHash(0, 0)]);
			NUnit.Framework.Assert.AreEqual("one", myCache[new FileBackedCacheTest.CustomHash(1, 0)]);
			NUnit.Framework.Assert.AreEqual("one'", myCache[new FileBackedCacheTest.CustomHash(1, 1)]);
			// Retest
			FileBackedCache<FileBackedCacheTest.CustomHash, string> reload = new FileBackedCache<FileBackedCacheTest.CustomHash, string>(cacheDir);
			NUnit.Framework.Assert.AreEqual("zero", reload[new FileBackedCacheTest.CustomHash(0, 0)]);
			NUnit.Framework.Assert.AreEqual("one", reload[new FileBackedCacheTest.CustomHash(1, 0)]);
			NUnit.Framework.Assert.AreEqual("one'", reload[new FileBackedCacheTest.CustomHash(1, 1)]);
			reload[new FileBackedCacheTest.CustomHash(2, 0)] = "two";
			NUnit.Framework.Assert.AreEqual("two", reload[new FileBackedCacheTest.CustomHash(2, 0)]);
			// Custom Teardown
			foreach (File c in cache.cacheDir.ListFiles())
			{
				NUnit.Framework.Assert.IsTrue(c.Delete());
			}
			NUnit.Framework.Assert.IsTrue(cache.cacheDir.Delete());
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		/// <exception cref="System.IO.IOException"/>
		[Test]
		public virtual void TestMerge()
		{
			cache["key"] = "value";
			// (create constituents)
			File constituent1File = File.CreateTempFile("cache", ".dir");
			NUnit.Framework.Assert.IsTrue(constituent1File.Delete());
			FileBackedCache<string, string> constituent1 = new FileBackedCache<string, string>(constituent1File);
			File constituent2File = File.CreateTempFile("cache", ".dir");
			NUnit.Framework.Assert.IsTrue(constituent2File.Delete());
			FileBackedCache<string, string> constituent2 = new FileBackedCache<string, string>(constituent2File);
			// (populate constituents)
			constituent1["c1Key1"] = "constituent1a";
			constituent1["c1Key2"] = "constituent1b";
			constituent1["c1Key3"] = "overlap";
			constituent2["c2Key1"] = "constituent2a";
			constituent2["c2Key2"] = "constituent2b";
			constituent2["c1Key3"] = "overlapReplaced";
			constituent1.Clear();
			constituent2.Clear();
			// (merge)
			FileBackedCache.Merge(cache, new FileBackedCache[] { constituent1, constituent2 });
			NUnit.Framework.Assert.AreEqual("value", cache["key"]);
			// (checks)
			cache.Clear();
			NUnit.Framework.Assert.AreEqual("constituent1a", cache["c1Key1"]);
			NUnit.Framework.Assert.AreEqual("constituent1b", cache["c1Key2"]);
			NUnit.Framework.Assert.AreEqual("constituent2a", cache["c2Key1"]);
			NUnit.Framework.Assert.AreEqual("constituent2b", cache["c2Key2"]);
			NUnit.Framework.Assert.AreEqual("overlapReplaced", cache["c1Key3"]);
			// (clean up)
			if (constituent1File.ListFiles() != null)
			{
				foreach (File c in constituent1File.ListFiles())
				{
					NUnit.Framework.Assert.IsTrue(c.Delete());
				}
				NUnit.Framework.Assert.IsTrue(constituent1File.Delete());
			}
			if (constituent2File.ListFiles() != null)
			{
				foreach (File c in constituent2File.ListFiles())
				{
					NUnit.Framework.Assert.IsTrue(c.Delete());
				}
				NUnit.Framework.Assert.IsTrue(constituent2File.Delete());
			}
			NUnit.Framework.Assert.IsTrue(FileBackedCache.LocksHeld().IsEmpty());
		}

		/// <exception cref="System.IO.IOException"/>
		[Test]
		public virtual void TestMapValueGoodPattern()
		{
			Dictionary<string, List<string>> map = new Dictionary<string, List<string>>();
			map["foo"] = new List<string>();
			mapCache[42] = map;
			NUnit.Framework.Assert.AreEqual(1, mapCache[42].Count);
			mapCache.Clear();
			NUnit.Framework.Assert.AreEqual(1, mapCache[42].Count);
		}
	}
}
