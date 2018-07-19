using System.Collections;
using System.Collections.Generic;
using Java.Util;
using Java.Util.Function;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Unit tests for Iterables utility class.</summary>
	/// <author>dramage</author>
	[NUnit.Framework.TestFixture]
	public class IterablesTest
	{
		[Test]
		public virtual void TestZip()
		{
			string[] s1 = new string[] { "a", "b", "c" };
			int[] s2 = new int[] { 1, 2, 3, 4 };
			int count = 0;
			foreach (Pair<string, int> pair in Iterables.Zip(s1, s2))
			{
				NUnit.Framework.Assert.AreEqual(pair.first, s1[count]);
				NUnit.Framework.Assert.AreEqual(pair.second, s2[count]);
				count++;
			}
			NUnit.Framework.Assert.AreEqual(s1.Length < s2.Length ? s1.Length : s2.Length, count);
		}

		[Test]
		public virtual void TestChain()
		{
			IList<string> s1 = Arrays.AsList(new string[] { "hi", "there" });
			IList<string> s2 = Arrays.AsList(new string[] {  });
			IList<string> s3 = Arrays.AsList(new string[] { "yoo" });
			IList<string> s4 = Arrays.AsList(new string[] {  });
			IList<string> answer = Arrays.AsList(new string[] { "yoo", "hi", "there", "yoo" });
			IList<string> chained = new List<string>();
			foreach (string s in Iterables.Chain(s3, s1, s2, s3, s4))
			{
				chained.Add(s);
			}
			NUnit.Framework.Assert.AreEqual(answer, chained);
		}

		[Test]
		public virtual void TestFilter()
		{
			IList<string> values = Arrays.AsList("a", "HI", "tHere", "YO");
			IEnumerator<string> iterator = Iterables.Filter(values, null).GetEnumerator();
			NUnit.Framework.Assert.IsTrue(iterator.MoveNext());
			NUnit.Framework.Assert.AreEqual(iterator.Current, "HI");
			NUnit.Framework.Assert.AreEqual(iterator.Current, "YO");
			NUnit.Framework.Assert.IsFalse(iterator.MoveNext());
		}

		[Test]
		public virtual void TestTransform()
		{
			IList<int> values = Arrays.AsList(1, 2, 3, 4);
			IList<int> squares = Arrays.AsList(1, 4, 9, 16);
			IFunction<int, int> squarer = null;
			foreach (Pair<int, int> pair in Iterables.Zip(Iterables.Transform(values, squarer), squares))
			{
				NUnit.Framework.Assert.AreEqual(pair.first, pair.second);
			}
		}

		[Test]
		public virtual void TestMerge()
		{
			IList<string> a = Arrays.AsList("a", "b", "d", "e");
			IList<string> b = Arrays.AsList("b", "c", "d", "e");
			IComparator<string> comparator = IComparer.NaturalOrder();
			IEnumerator<Pair<string, string>> iter = Iterables.Merge(a, b, comparator).GetEnumerator();
			NUnit.Framework.Assert.AreEqual(iter.Current, new Pair<string, string>("b", "b"));
			NUnit.Framework.Assert.AreEqual(iter.Current, new Pair<string, string>("d", "d"));
			NUnit.Framework.Assert.AreEqual(iter.Current, new Pair<string, string>("e", "e"));
			NUnit.Framework.Assert.IsTrue(!iter.MoveNext());
		}

		[Test]
		public virtual void TestMerge3()
		{
			IList<string> a = Arrays.AsList("a", "b", "d", "e");
			IList<string> b = Arrays.AsList("b", "c", "d", "e");
			IList<string> c = Arrays.AsList("a", "b", "c", "e", "f");
			IComparator<string> comparator = IComparer.NaturalOrder();
			IEnumerator<Triple<string, string, string>> iter = Iterables.Merge(a, b, c, comparator).GetEnumerator();
			NUnit.Framework.Assert.AreEqual(iter.Current, new Triple<string, string, string>("b", "b", "b"));
			NUnit.Framework.Assert.AreEqual(iter.Current, new Triple<string, string, string>("e", "e", "e"));
			NUnit.Framework.Assert.IsTrue(!iter.MoveNext());
		}

		[Test]
		public virtual void TestGroup()
		{
			string[] input = new string[] { "0 ab", "0 bb", "0 cc", "1 dd", "2 dd", "2 kj", "3 kj", "3 kk" };
			int[] counts = new int[] { 3, 1, 2, 2 };
			IComparator<string> fieldOne = IComparer.Comparing(null);
			int index = 0;
			int group = 0;
			foreach (IEnumerable<string> set in Iterables.Group(Arrays.AsList(input), fieldOne))
			{
				string sharedKey = null;
				int thisCount = 0;
				foreach (string line in set)
				{
					string thisKey = line.Split(" ")[0];
					if (sharedKey == null)
					{
						sharedKey = thisKey;
					}
					else
					{
						NUnit.Framework.Assert.AreEqual(sharedKey, thisKey, "Wrong key");
					}
					NUnit.Framework.Assert.AreEqual(line, input[index++], "Wrong input line");
					thisCount++;
				}
				NUnit.Framework.Assert.AreEqual(counts[group++], thisCount, "Wrong number of items in this iterator");
			}
			NUnit.Framework.Assert.AreEqual(input.Length, index, "Didn't get all inputs");
			NUnit.Framework.Assert.AreEqual(counts.Length, group, "Wrong number of groups");
		}

		[Test]
		public virtual void TestSample()
		{
			// make sure correct number of items is sampled and items are in range
			IEnumerable<int> items = Arrays.AsList(5, 4, 3, 2, 1);
			int count = 0;
			foreach (int item in Iterables.Sample(items, 5, 2, new Random()))
			{
				++count;
				NUnit.Framework.Assert.IsTrue(item <= 5);
				NUnit.Framework.Assert.IsTrue(item >= 1);
			}
			NUnit.Framework.Assert.AreEqual(2, count);
		}
	}
}
