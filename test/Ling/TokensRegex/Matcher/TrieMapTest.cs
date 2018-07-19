using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex.Matcher
{
	/// <summary>Test case for TrieMap</summary>
	/// <author>Angel Chang</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TrieMapTest
	{
		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestTrieBasic()
		{
			TrieMap<string, bool> trieMap = new TrieMap<string, bool>();
			trieMap.Put(new string[] { "a", "white", "cat" }, true);
			trieMap.Put(new string[] { "a", "white", "hat" }, true);
			trieMap.Put(new string[] { "a", "black", "cat" }, true);
			trieMap.Put(new string[] { "a", "black", "cat", "climbed", "on", "the", "sofa" }, true);
			System.Console.Out.WriteLine(trieMap);
			System.Console.Out.WriteLine(trieMap.ToFormattedString());
			// Test get and remove
			NUnit.Framework.Assert.IsTrue(trieMap.Get(new string[] { "a", "white", "hat" }));
			NUnit.Framework.Assert.IsNull(trieMap.Get(new string[] { "a", "white" }));
			trieMap.Remove(new string[] { "a", "white", "hat" });
			NUnit.Framework.Assert.IsTrue(trieMap.Get(new string[] { "a", "white", "cat" }));
			NUnit.Framework.Assert.IsNull(trieMap.Get(new string[] { "a", "white", "hat" }));
			// Test keys
			NUnit.Framework.Assert.IsTrue(trieMap.Contains(new string[] { "a", "white", "cat" }));
			NUnit.Framework.Assert.IsFalse(trieMap.Contains(new string[] { "white", "cat" }));
			NUnit.Framework.Assert.AreEqual(3, trieMap.Count);
			NUnit.Framework.Assert.AreEqual(3, trieMap.Keys.Count);
			// Test putAll
			IDictionary<IList<string>, bool> m = new Dictionary<IList<string>, bool>();
			m[Arrays.AsList("a", "purple", "giraffe")] = true;
			m[Arrays.AsList("four", "orange", "bears")] = true;
			trieMap.PutAll(m);
			NUnit.Framework.Assert.IsTrue(trieMap.Contains(new string[] { "a", "purple", "giraffe" }));
			NUnit.Framework.Assert.IsTrue(trieMap.Contains(new string[] { "four", "orange", "bears" }));
			NUnit.Framework.Assert.AreEqual(5, trieMap.Count);
			NUnit.Framework.Assert.AreEqual(5, trieMap.Keys.Count);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestTrieFindAll()
		{
			TrieMap<string, bool> trieMap = new TrieMap<string, bool>();
			trieMap.Put(new string[] { "a", "white", "cat" }, true);
			trieMap.Put(new string[] { "a", "white", "hat" }, true);
			trieMap.Put(new string[] { "a", "black", "cat" }, true);
			trieMap.Put(new string[] { "a", "black", "cat", "climbed", "on", "the", "sofa" }, true);
			trieMap.Put(new string[] { "white" }, true);
			TrieMapMatcher<string, bool> matcher = new TrieMapMatcher<string, bool>(trieMap);
			IList<Match<string, bool>> matches = matcher.FindAllMatches("a", "white", "cat", "is", "wearing", "a", "white", "hat");
			IList<Match<string, bool>> expected = new List<Match<string, bool>>();
			expected.Add(new Match<string, bool>(Arrays.AsList("a", "white", "cat"), true, 0, 3));
			expected.Add(new Match<string, bool>(Arrays.AsList("white"), true, 1, 2));
			expected.Add(new Match<string, bool>(Arrays.AsList("a", "white", "hat"), true, 5, 8));
			expected.Add(new Match<string, bool>(Arrays.AsList("white"), true, 6, 7));
			NUnit.Framework.Assert.AreEqual("Expecting " + expected.Count + " matches: got " + matches, expected.Count, matches.Count);
			NUnit.Framework.Assert.AreEqual("Expecting " + expected + ", got " + matches, expected, matches);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestTrieFindNonOverlapping()
		{
			TrieMap<string, bool> trieMap = new TrieMap<string, bool>();
			trieMap.Put(new string[] { "a", "white", "cat" }, true);
			trieMap.Put(new string[] { "a", "white", "hat" }, true);
			trieMap.Put(new string[] { "a", "black", "cat" }, true);
			trieMap.Put(new string[] { "a", "black", "cat", "climbed", "on", "the", "sofa" }, true);
			trieMap.Put(new string[] { "white" }, true);
			TrieMapMatcher<string, bool> matcher = new TrieMapMatcher<string, bool>(trieMap);
			IList<Match<string, bool>> matches = matcher.FindNonOverlapping("a", "white", "cat", "is", "wearing", "a", "white", "hat", "and", "a", "black", "cat", "climbed", "on", "the", "sofa");
			IList<Match<string, bool>> expected = new List<Match<string, bool>>();
			expected.Add(new Match<string, bool>(Arrays.AsList("a", "white", "cat"), true, 0, 3));
			expected.Add(new Match<string, bool>(Arrays.AsList("a", "white", "hat"), true, 5, 8));
			expected.Add(new Match<string, bool>(Arrays.AsList("a", "black", "cat", "climbed", "on", "the", "sofa"), true, 9, 16));
			NUnit.Framework.Assert.AreEqual("Expecting " + expected.Count + " matches: got " + matches, expected.Count, matches.Count);
			NUnit.Framework.Assert.AreEqual("Expecting " + expected + ", got " + matches, expected, matches);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestTrieSegment()
		{
			TrieMap<string, bool> trieMap = new TrieMap<string, bool>();
			trieMap.Put(new string[] { "a", "white", "cat" }, true);
			trieMap.Put(new string[] { "a", "white", "hat" }, true);
			trieMap.Put(new string[] { "a", "black", "cat" }, true);
			trieMap.Put(new string[] { "a", "black", "cat", "climbed", "on", "the", "sofa" }, true);
			trieMap.Put(new string[] { "white" }, true);
			TrieMapMatcher<string, bool> matcher = new TrieMapMatcher<string, bool>(trieMap);
			IList<Match<string, bool>> matches = matcher.Segment("a", "white", "cat", "is", "wearing", "a", "white", "hat", "and", "a", "black", "cat", "climbed", "on", "the", "sofa");
			IList<Match<string, bool>> expected = new List<Match<string, bool>>();
			expected.Add(new Match<string, bool>(Arrays.AsList("a", "white", "cat"), true, 0, 3));
			expected.Add(new Match<string, bool>(Arrays.AsList("is", "wearing"), null, 3, 5));
			expected.Add(new Match<string, bool>(Arrays.AsList("a", "white", "hat"), true, 5, 8));
			expected.Add(new Match<string, bool>(Arrays.AsList("and"), null, 8, 9));
			expected.Add(new Match<string, bool>(Arrays.AsList("a", "black", "cat", "climbed", "on", "the", "sofa"), true, 9, 16));
			NUnit.Framework.Assert.AreEqual("Expecting " + expected.Count + " matches: got " + matches, expected.Count, matches.Count);
			NUnit.Framework.Assert.AreEqual("Expecting " + expected + ", got " + matches, expected, matches);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestTrieFindClosest()
		{
			TrieMap<string, bool> trieMap = new TrieMap<string, bool>();
			trieMap.Put(new string[] { "a", "white", "cat" }, true);
			trieMap.Put(new string[] { "a", "white", "hat" }, true);
			trieMap.Put(new string[] { "a", "black", "cat" }, true);
			trieMap.Put(new string[] { "a", "black", "hat" }, true);
			trieMap.Put(new string[] { "a", "colored", "hat" }, true);
			TrieMapMatcher<string, bool> matcher = new TrieMapMatcher<string, bool>(trieMap);
			IList<ApproxMatch<string, bool>> matches = matcher.FindClosestMatches(new string[] { "the", "black", "hat" }, 2);
			IList<ApproxMatch<string, bool>> expected = new List<ApproxMatch<string, bool>>();
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "hat"), true, 0, 3, 1.0));
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "cat"), true, 0, 3, 2.0));
			NUnit.Framework.Assert.AreEqual("\nExpecting " + expected + ",\n got " + matches, expected, matches);
			//System.out.println(matches);
			// TODO: ordering of results with same score
			matches = matcher.FindClosestMatches(new string[] { "the", "black" }, 5);
			expected = new List<ApproxMatch<string, bool>>();
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "cat"), true, 0, 2, 2.0));
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "hat"), true, 0, 2, 2.0));
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "colored", "hat"), true, 0, 2, 3.0));
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "white", "cat"), true, 0, 2, 3.0));
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "white", "hat"), true, 0, 2, 3.0));
			NUnit.Framework.Assert.AreEqual("\nExpecting " + StringUtils.Join(expected, "\n") + ",\ngot " + StringUtils.Join(matches, "\n"), expected, matches);
			//System.out.println(matches);
			matches = matcher.FindClosestMatches(new string[] { "the", "black", "cat", "is", "wearing", "a", "white", "hat" }, 5);
			expected = new List<ApproxMatch<string, bool>>();
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "white", "hat"), true, 0, 8, 5.0));
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "cat"), true, 0, 8, 6.0));
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "hat"), true, 0, 8, 6.0));
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "colored", "hat"), true, 0, 8, 6.0));
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "white", "cat"), true, 0, 8, 6.0));
			NUnit.Framework.Assert.AreEqual("Expecting " + StringUtils.Join(expected, "\n") + ",\ngot " + StringUtils.Join(matches, "\n"), expected, matches);
			//System.out.println(matches);
			matches = matcher.FindClosestMatches(new string[] { "the", "black", "cat", "is", "wearing", "a", "white", "hat" }, 6, true, true);
			//   [([[a, black, cat]-[a, white, hat]] -> true-true at (0,8),3.0),
			expected = new List<ApproxMatch<string, bool>>();
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "cat", "a", "white", "hat"), true, 0, 8, Arrays.AsList(new Match<string, bool>(Arrays.AsList("a", "black", "cat"), true, 0, 3), new Match<string, bool>(Arrays.AsList("a", 
				"white", "hat"), true, 5, 8)), 3.0));
			// ([[a, black, hat]-[a, black, hat]] -> true-true at (0,8),4.0),
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "cat", "a", "black", "hat"), true, 0, 8, Arrays.AsList(new Match<string, bool>(Arrays.AsList("a", "black", "cat"), true, 0, 3), new Match<string, bool>(Arrays.AsList("a", 
				"black", "hat"), true, 5, 8)), 4.0));
			// ([[a, black, hat]-[a, colored, hat]] -> true-true at (0,8),4.0),
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "cat", "a", "colored", "hat"), true, 0, 8, Arrays.AsList(new Match<string, bool>(Arrays.AsList("a", "black", "cat"), true, 0, 3), new Match<string, bool>(Arrays.AsList("a"
				, "colored", "hat"), true, 5, 8)), 4.0));
			// ([[a, black, cat]-[a, white, cat]] -> true-true at (0,8),4.0),
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "cat", "a", "white", "cat"), true, 0, 8, Arrays.AsList(new Match<string, bool>(Arrays.AsList("a", "black", "cat"), true, 0, 3), new Match<string, bool>(Arrays.AsList("a", 
				"white", "cat"), true, 5, 8)), 4.0));
			// ([[a, black, cat]-[a, white, hat]] -> true-true at (0,8),4.0),
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "hat", "a", "white", "hat"), true, 0, 8, Arrays.AsList(new Match<string, bool>(Arrays.AsList("a", "black", "hat"), true, 0, 3), new Match<string, bool>(Arrays.AsList("a", 
				"white", "hat"), true, 5, 8)), 4.0));
			// ([[a, black, cat]-[a, black, cat]-[a, white, hat]] -> true-true at (0,8),4.0)]
			expected.Add(new ApproxMatch<string, bool>(Arrays.AsList("a", "black", "cat", "a", "black", "cat", "a", "white", "hat"), true, 0, 8, Arrays.AsList(new Match<string, bool>(Arrays.AsList("a", "black", "cat"), true, 0, 3), new Match<string, bool
				>(Arrays.AsList("a", "black", "cat"), true, 3, 5), new Match<string, bool>(Arrays.AsList("a", "white", "hat"), true, 5, 8)), 4.0));
			NUnit.Framework.Assert.AreEqual("\nExpecting " + StringUtils.Join(expected, "\n") + ",\ngot " + StringUtils.Join(matches, "\n"), expected, matches);
		}
	}
}
