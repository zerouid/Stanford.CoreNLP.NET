using System.Collections.Generic;


using NUnit.Framework;


namespace Edu.Stanford.Nlp.Util
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class CollectionUtilsTest
	{
		internal File outputDir;

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			outputDir = File.CreateTempFile("IOUtilsTest", ".dir");
			NUnit.Framework.Assert.IsTrue(outputDir.Delete());
			NUnit.Framework.Assert.IsTrue(outputDir.Mkdir());
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.TearDown]
		protected virtual void TearDown()
		{
			this.Remove(this.outputDir);
		}

		protected internal virtual void Remove(File file)
		{
			if (file.IsDirectory())
			{
				foreach (File child in file.ListFiles())
				{
					this.Remove(child);
				}
			}
			file.Delete();
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestLoadCollection()
		{
			File collectionFile = new File(this.outputDir, "string.collection");
			StringUtils.PrintToFile(collectionFile, "-1\n42\n122\n-3.14");
			ICollection<string> actualSet = new HashSet<string>();
			CollectionUtils.LoadCollection<string>(collectionFile, actualSet);
			ICollection<string> expectedSet = new HashSet<string>(Arrays.AsList("-1 42 122 -3.14".Split(" ")));
			NUnit.Framework.Assert.AreEqual(expectedSet, actualSet);
			IList<CollectionUtilsTest.TestDouble> actualList = new List<CollectionUtilsTest.TestDouble>();
			actualList.Add(new CollectionUtilsTest.TestDouble("95.2"));
			CollectionUtils.LoadCollection<CollectionUtilsTest.TestDouble>(collectionFile.GetPath(), actualList);
			IList<CollectionUtilsTest.TestDouble> expectedList = new List<CollectionUtilsTest.TestDouble>();
			expectedList.Add(new CollectionUtilsTest.TestDouble("95.2"));
			expectedList.Add(new CollectionUtilsTest.TestDouble("-1"));
			expectedList.Add(new CollectionUtilsTest.TestDouble("42"));
			expectedList.Add(new CollectionUtilsTest.TestDouble("122"));
			expectedList.Add(new CollectionUtilsTest.TestDouble("-3.14"));
			NUnit.Framework.Assert.AreEqual(expectedList, actualList);
		}

		public class TestDouble
		{
			public double d;

			public TestDouble(string @string)
			{
				this.d = double.ParseDouble(@string);
			}

			public override bool Equals(object other)
			{
				return this.d == ((CollectionUtilsTest.TestDouble)other).d;
			}

			public override string ToString()
			{
				return string.Format("%f", this.d);
			}
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestSorted()
		{
			IList<int> inputInts = Arrays.AsList(5, 4, 3, 2, 1);
			IList<int> expectedInts = Arrays.AsList(1, 2, 3, 4, 5);
			NUnit.Framework.Assert.AreEqual(expectedInts, CollectionUtils.Sorted(inputInts));
			ICollection<string> inputStrings = new HashSet<string>(Arrays.AsList("d a c b".Split(" ")));
			IList<string> expectedStrings = Arrays.AsList("a b c d".Split(" "));
			NUnit.Framework.Assert.AreEqual(expectedStrings, CollectionUtils.Sorted(inputStrings));
		}

		[NUnit.Framework.Test]
		public virtual void TestToList()
		{
			IEnumerable<string> iter = Iterables.Take(Arrays.AsList("a", "b", "c"), 2);
			NUnit.Framework.Assert.AreEqual(Arrays.AsList("a", "b"), CollectionUtils.ToList(iter));
		}

		[NUnit.Framework.Test]
		public virtual void TestToSet()
		{
			IEnumerable<string> iter = Iterables.Drop(Arrays.AsList("c", "a", "b", "a"), 1);
			NUnit.Framework.Assert.AreEqual(new HashSet<string>(Arrays.AsList("a", "b")), CollectionUtils.ToSet(iter));
		}

		[NUnit.Framework.Test]
		public virtual void TestGetNGrams()
		{
			IList<string> items;
			IList<IList<string>> expected;
			IList<IList<string>> actual;
			items = SplitOne("a#b#c#d#e");
			expected = Split("a b c d e");
			actual = CollectionUtils.GetNGrams(items, 1, 1);
			NUnit.Framework.Assert.AreEqual(expected, actual);
			items = SplitOne("a#b#c#d#e");
			expected = Split("a#b b#c c#d d#e");
			actual = CollectionUtils.GetNGrams(items, 2, 2);
			NUnit.Framework.Assert.AreEqual(expected, actual);
			items = SplitOne("a#b#c#d#e");
			expected = Split("a a#b b b#c c c#d d d#e e");
			actual = CollectionUtils.GetNGrams(items, 1, 2);
			NUnit.Framework.Assert.AreEqual(expected, actual);
			items = SplitOne("a#b#c#d#e");
			expected = Split("a#b#c#d a#b#c#d#e b#c#d#e");
			actual = CollectionUtils.GetNGrams(items, 4, 6);
			NUnit.Framework.Assert.AreEqual(expected, actual);
			items = SplitOne("a#b#c#d#e");
			expected = new List<IList<string>>();
			actual = CollectionUtils.GetNGrams(items, 6, 6);
			NUnit.Framework.Assert.AreEqual(expected, actual);
		}

		private static IList<string> SplitOne(string wordString)
		{
			return Arrays.AsList(wordString.Split("#"));
		}

		private static IList<IList<string>> Split(string wordListsString)
		{
			IList<IList<string>> result = new List<IList<string>>();
			foreach (string wordString in wordListsString.Split(" "))
			{
				result.Add(SplitOne(wordString));
			}
			return result;
		}

		[NUnit.Framework.Test]
		public virtual void TestGetIndex()
		{
			int startIndex = 4;
			IList<string> list = Arrays.AsList("this", "is", "a", "test", "which", "test", "is", "it");
			int index = CollectionUtils.GetIndex(list, "test", startIndex);
			NUnit.Framework.Assert.AreEqual(5, index);
			startIndex = 0;
			list = Arrays.AsList("Biology", "is", "a", "test", "which", "test", "is", "it");
			index = CollectionUtils.GetIndex(list, "Biology", startIndex);
			NUnit.Framework.Assert.AreEqual(0, index);
		}

		[NUnit.Framework.Test]
		public virtual void TestContainsAny()
		{
			IList<string> list = Arrays.AsList("this", "is", "a", "test", "which", "test", "is", "it");
			IList<string> toCheck = Arrays.AsList("a", "which");
			NUnit.Framework.Assert.IsTrue(CollectionUtils.ContainsAny(list, toCheck));
			toCheck = Arrays.AsList("not", "a");
			NUnit.Framework.Assert.IsTrue(CollectionUtils.ContainsAny(list, toCheck));
			toCheck = Arrays.AsList("not", "here");
			NUnit.Framework.Assert.IsFalse(CollectionUtils.ContainsAny(list, toCheck));
		}

		[NUnit.Framework.Test]
		public virtual void TestIsSubList()
		{
			IList<string> t1 = Arrays.AsList("this", "is", "test");
			IList<string> t2 = Arrays.AsList("well", "this", "this", "again", "is", "test");
			NUnit.Framework.Assert.IsTrue(CollectionUtils.IsSubList(t1, t2));
			t1 = Arrays.AsList("test", "this", "is");
			NUnit.Framework.Assert.IsFalse(CollectionUtils.IsSubList(t1, t2));
		}

		[NUnit.Framework.Test]
		public virtual void TestMaxIndex()
		{
			IList<int> t1 = Arrays.AsList(2, -1, 4);
			NUnit.Framework.Assert.AreEqual(2, CollectionUtils.MaxIndex(t1));
		}

		[NUnit.Framework.Test]
		public virtual void TestIteratorConcatEmpty()
		{
			IEnumerator<string> iter = CollectionUtils.ConcatIterators();
			NUnit.Framework.Assert.IsFalse(iter.MoveNext());
		}

		[NUnit.Framework.Test]
		public virtual void TestIteratorConcatSingleIter()
		{
			IEnumerator<string> iter = CollectionUtils.ConcatIterators(new _List_176().GetEnumerator());
			NUnit.Framework.Assert.IsTrue(iter.MoveNext());
			NUnit.Framework.Assert.AreEqual("foo", iter.Current);
			NUnit.Framework.Assert.IsFalse(iter.MoveNext());
		}

		private sealed class _List_176 : List<string>
		{
			public _List_176()
			{
				{
					this.Add("foo");
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestIteratorConcatMultiIter()
		{
			IEnumerator<string> iter = CollectionUtils.ConcatIterators(new _List_184().GetEnumerator(), new _List_185().GetEnumerator(), new _List_186().GetEnumerator());
			NUnit.Framework.Assert.IsTrue(iter.MoveNext());
			NUnit.Framework.Assert.AreEqual("foo", iter.Current);
			NUnit.Framework.Assert.IsTrue(iter.MoveNext());
			NUnit.Framework.Assert.AreEqual("bar", iter.Current);
			NUnit.Framework.Assert.IsTrue(iter.MoveNext());
			NUnit.Framework.Assert.AreEqual("baz", iter.Current);
			NUnit.Framework.Assert.IsTrue(iter.MoveNext());
			NUnit.Framework.Assert.AreEqual("boo", iter.Current);
			NUnit.Framework.Assert.IsFalse(iter.MoveNext());
		}

		private sealed class _List_184 : List<string>
		{
			public _List_184()
			{
				{
					this.Add("foo");
				}
			}
		}

		private sealed class _List_185 : List<string>
		{
			public _List_185()
			{
				{
					this.Add("bar");
					this.Add("baz");
				}
			}
		}

		private sealed class _List_186 : List<string>
		{
			public _List_186()
			{
				{
					this.Add("boo");
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestIteratorConcatEmptyIter()
		{
			IEnumerator<string> iter = CollectionUtils.ConcatIterators(new _List_197().GetEnumerator(), new _List_198().GetEnumerator(), new _List_199().GetEnumerator());
			NUnit.Framework.Assert.IsTrue(iter.MoveNext());
			NUnit.Framework.Assert.AreEqual("foo", iter.Current);
			NUnit.Framework.Assert.IsTrue(iter.MoveNext());
			NUnit.Framework.Assert.AreEqual("boo", iter.Current);
			NUnit.Framework.Assert.IsFalse(iter.MoveNext());
		}

		private sealed class _List_197 : List<string>
		{
			public _List_197()
			{
				{
					this.Add("foo");
				}
			}
		}

		private sealed class _List_198 : List<string>
		{
			public _List_198()
			{
				{
				}
			}
		}

		private sealed class _List_199 : List<string>
		{
			public _List_199()
			{
				{
					this.Add("boo");
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestIteratorConcaatRemove()
		{
			List<string> a = new _List_207();
			List<string> b = new _List_208();
			List<string> c = new _List_209();
			IEnumerator<string> iter = CollectionUtils.ConcatIterators(a.GetEnumerator(), b.GetEnumerator(), c.GetEnumerator());
			NUnit.Framework.Assert.IsTrue(iter.MoveNext());
			NUnit.Framework.Assert.AreEqual("foo", iter.Current);
			NUnit.Framework.Assert.IsTrue(iter.MoveNext());
			NUnit.Framework.Assert.AreEqual("bar", iter.Current);
			iter.Remove();
			NUnit.Framework.Assert.IsTrue(iter.MoveNext());
			NUnit.Framework.Assert.AreEqual("baz", iter.Current);
			NUnit.Framework.Assert.AreEqual(new _List_215(), a);
			NUnit.Framework.Assert.AreEqual(new _List_216(), b);
			NUnit.Framework.Assert.AreEqual(new _List_217(), c);
		}

		private sealed class _List_207 : List<string>
		{
			public _List_207()
			{
				{
					this.Add("foo");
				}
			}
		}

		private sealed class _List_208 : List<string>
		{
			public _List_208()
			{
				{
				}
			}
		}

		private sealed class _List_209 : List<string>
		{
			public _List_209()
			{
				{
					this.Add("bar");
					this.Add("baz");
				}
			}
		}

		private sealed class _List_215 : List<string>
		{
			public _List_215()
			{
				{
					this.Add("foo");
				}
			}
		}

		private sealed class _List_216 : List<string>
		{
			public _List_216()
			{
				{
				}
			}
		}

		private sealed class _List_217 : List<string>
		{
			public _List_217()
			{
				{
					this.Add("baz");
				}
			}
		}
	}
}
