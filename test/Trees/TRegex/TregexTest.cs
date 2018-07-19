using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Java.Util;
using Java.Util.Function;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TregexTest
	{
		// We don't use valueOf because we sometimes use trees such as
		// (bar (foo (foo 1))), and the default valueOf uses a
		// TreeNormalizer that removes nodes from such a tree
		public static Tree TreeFromString(string s)
		{
			try
			{
				ITreeReader tr = new PennTreeReader(new StringReader(s), new LabeledScoredTreeFactory());
				return tr.ReadTree();
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		public static Tree[] TreesFromString(params string[] s)
		{
			Tree[] trees = new Tree[s.Length];
			for (int i = 0; i < s.Length; ++i)
			{
				trees[i] = TreeFromString(s[i]);
			}
			return trees;
		}

		/// <summary>This was buggy in 2010.</summary>
		/// <remarks>This was buggy in 2010. But John Bauer fixed it.</remarks>
		[NUnit.Framework.Test]
		public virtual void TestJoãoSilva()
		{
			TregexPattern tregex1 = TregexPattern.Compile("PNT=p >>- (__=l >, (__=t <- (__=r <, __=m <- (__ <, CONJ <- __=z))))");
			TregexPattern tregex2 = TregexPattern.Compile("PNT=p >>- (/(.+)/#1%var=l >, (__=t <- (__=r <, /(.+)/#1%var=m <- (__ <, CONJ <- /(.+)/#1%var=z))))");
			TregexPattern tregex3 = TregexPattern.Compile("PNT=p >>- (__=l >, (__=t <- (__=r <, ~l <- (__ <, CONJ <- ~l))))");
			Tree tree = TreeFromString("(T (X (N (N Moe (PNT ,)))) (NP (X (N Curly)) (NP (CONJ and) (X (N Larry)))))");
			TregexMatcher matcher1 = tregex1.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher1.Find());
			TregexMatcher matcher2 = tregex2.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher2.Find());
			TregexMatcher matcher3 = tregex3.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher3.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestNoResults()
		{
			TregexPattern pMWE = TregexPattern.Compile("/^MW/");
			Tree tree = TreeFromString("(Foo)");
			TregexMatcher matcher = pMWE.Matcher(tree);
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestOneResult()
		{
			TregexPattern pMWE = TregexPattern.Compile("/^MW/");
			Tree tree = TreeFromString("(ROOT (MWE (N 1) (N 2) (N 3)))");
			TregexMatcher matcher = pMWE.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			Tree match = matcher.GetMatch();
			NUnit.Framework.Assert.AreEqual("(MWE (N 1) (N 2) (N 3))", match.ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestTwoResults()
		{
			TregexPattern pMWE = TregexPattern.Compile("/^MW/");
			Tree tree = TreeFromString("(ROOT (MWE (N 1) (N 2) (N 3)) (MWV (A B)))");
			TregexMatcher matcher = pMWE.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			Tree match = matcher.GetMatch();
			NUnit.Framework.Assert.AreEqual("(MWE (N 1) (N 2) (N 3))", match.ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			match = matcher.GetMatch();
			NUnit.Framework.Assert.AreEqual("(MWV (A B))", match.ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		/// <summary>a tregex pattern should be able to go more than once.</summary>
		/// <remarks>
		/// a tregex pattern should be able to go more than once.
		/// just like me.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestReuse()
		{
			TregexPattern pMWE = TregexPattern.Compile("/^MW/");
			Tree tree = TreeFromString("(ROOT (MWE (N 1) (N 2) (N 3)) (MWV (A B)))");
			TregexMatcher matcher = pMWE.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			tree = TreeFromString("(ROOT (MWE (N 1) (N 2) (N 3)))");
			matcher = pMWE.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			tree = TreeFromString("(Foo)");
			matcher = pMWE.Matcher(tree);
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		/// <summary>
		/// reruns one of the simpler tests using the test class to make sure
		/// the test class works
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestTest()
		{
			RunTest("/^MW/", "(ROOT (MWE (N 1) (N 2) (N 3)) (MWV (A B)))", "(MWE (N 1) (N 2) (N 3))", "(MWV (A B))");
		}

		[NUnit.Framework.Test]
		public virtual void TestWordDisjunction()
		{
			TregexPattern pattern = TregexPattern.Compile("a|b|c << bar");
			RunTest(pattern, "(a (bar 1))", "(a (bar 1))");
			RunTest(pattern, "(b (bar 1))", "(b (bar 1))");
			RunTest(pattern, "(c (bar 1))", "(c (bar 1))");
			RunTest(pattern, "(d (bar 1))");
			RunTest(pattern, "(e (bar 1))");
			RunTest(pattern, "(f (bar 1))");
			RunTest(pattern, "(g (bar 1))");
			pattern = TregexPattern.Compile("a|b|c|d|e|f << bar");
			RunTest(pattern, "(a (bar 1))", "(a (bar 1))");
			RunTest(pattern, "(b (bar 1))", "(b (bar 1))");
			RunTest(pattern, "(c (bar 1))", "(c (bar 1))");
			RunTest(pattern, "(d (bar 1))", "(d (bar 1))");
			RunTest(pattern, "(e (bar 1))", "(e (bar 1))");
			RunTest(pattern, "(f (bar 1))", "(f (bar 1))");
			RunTest(pattern, "(g (bar 1))");
		}

		[NUnit.Framework.Test]
		public virtual void TestDominates()
		{
			TregexPattern dominatesPattern = TregexPattern.Compile("foo << bar");
			RunTest(dominatesPattern, "(foo (bar 1))", "(foo (bar 1))");
			RunTest(dominatesPattern, "(foo (a (bar 1)))", "(foo (a (bar 1)))");
			RunTest(dominatesPattern, "(foo (a (b (bar 1))))", "(foo (a (b (bar 1))))");
			RunTest(dominatesPattern, "(foo (a (b 1) (bar 2)))", "(foo (a (b 1) (bar 2)))");
			RunTest(dominatesPattern, "(foo (a (b 1) (c 2) (bar 3)))", "(foo (a (b 1) (c 2) (bar 3)))");
			RunTest(dominatesPattern, "(foo (baz 1))");
			RunTest(dominatesPattern, "(a (foo (bar 1)))", "(foo (bar 1))");
			RunTest(dominatesPattern, "(a (foo (baz (bar 1))))", "(foo (baz (bar 1)))");
			RunTest(dominatesPattern, "(a (foo (bar 1)) (foo (bar 2)))", "(foo (bar 1))", "(foo (bar 2))");
			TregexPattern dominatedPattern = TregexPattern.Compile("foo >> bar");
			RunTest(dominatedPattern, "(foo (bar 1))");
			RunTest(dominatedPattern, "(foo (a (bar 1)))");
			RunTest(dominatedPattern, "(foo (a (b (bar 1))))");
			RunTest(dominatedPattern, "(foo (a (b 1) (bar 2)))");
			RunTest(dominatedPattern, "(foo (a (b 1) (c 2) (bar 3)))");
			RunTest(dominatedPattern, "(bar (foo 1))", "(foo 1)");
			RunTest(dominatedPattern, "(bar (a (foo 1)))", "(foo 1)");
			RunTest(dominatedPattern, "(bar (a (foo (b 1))))", "(foo (b 1))");
			RunTest(dominatedPattern, "(bar (a (foo 1) (foo 2)))", "(foo 1)", "(foo 2)");
			RunTest(dominatedPattern, "(bar (foo (foo 1)))", "(foo (foo 1))", "(foo 1)");
			RunTest(dominatedPattern, "(a (bar (foo 1)))", "(foo 1)");
		}

		[NUnit.Framework.Test]
		public virtual void TestImmediatelyDominates()
		{
			TregexPattern dominatesPattern = TregexPattern.Compile("foo < bar");
			RunTest(dominatesPattern, "(foo (bar 1))", "(foo (bar 1))");
			RunTest(dominatesPattern, "(foo (a (bar 1)))");
			RunTest(dominatesPattern, "(a (foo (bar 1)))", "(foo (bar 1))");
			RunTest(dominatesPattern, "(a (foo (baz 1) (bar 2)))", "(foo (baz 1) (bar 2))");
			RunTest(dominatesPattern, "(a (foo (bar 1)) (foo (bar 2)))", "(foo (bar 1))", "(foo (bar 2))");
			TregexPattern dominatedPattern = TregexPattern.Compile("foo > bar");
			RunTest(dominatedPattern, "(foo (bar 1))");
			RunTest(dominatedPattern, "(foo (a (bar 1)))");
			RunTest(dominatedPattern, "(foo (a (b (bar 1))))");
			RunTest(dominatedPattern, "(foo (a (b 1) (bar 2)))");
			RunTest(dominatedPattern, "(foo (a (b 1) (c 2) (bar 3)))");
			RunTest(dominatedPattern, "(bar (foo 1))", "(foo 1)");
			RunTest(dominatedPattern, "(bar (a (foo 1)))");
			RunTest(dominatedPattern, "(bar (foo 1) (foo 2))", "(foo 1)", "(foo 2)");
			RunTest(dominatedPattern, "(bar (foo (foo 1)))", "(foo (foo 1))");
			RunTest(dominatedPattern, "(a (bar (foo 1)))", "(foo 1)");
		}

		[NUnit.Framework.Test]
		public virtual void TestSister()
		{
			TregexPattern pattern = TregexPattern.Compile("/.*/ $ foo");
			RunTest(pattern, "(a (foo 1) (bar 2))", "(bar 2)");
			RunTest(pattern, "(a (bar 1) (foo 2))", "(bar 1)");
			RunTest(pattern, "(a (foo 1) (bar 2) (baz 3))", "(bar 2)", "(baz 3)");
			RunTest(pattern, "(a (foo (bar 2)) (baz 3))", "(baz 3)");
			RunTest(pattern, "(a (foo (bar 2)) (baz (bif 3)))", "(baz (bif 3))");
			RunTest(pattern, "(a (foo (bar 2)))");
			RunTest(pattern, "(a (foo 1))");
			pattern = TregexPattern.Compile("bar|baz $ foo");
			RunTest(pattern, "(a (foo 1) (bar 2))", "(bar 2)");
			RunTest(pattern, "(a (bar 1) (foo 2))", "(bar 1)");
			RunTest(pattern, "(a (foo 1) (bar 2) (baz 3))", "(bar 2)", "(baz 3)");
			RunTest(pattern, "(a (foo (bar 2)) (baz 3))", "(baz 3)");
			RunTest(pattern, "(a (foo (bar 2)) (baz (bif 3)))", "(baz (bif 3))");
			RunTest(pattern, "(a (foo (bar 2)))");
			RunTest(pattern, "(a (foo 1))");
			pattern = TregexPattern.Compile("/.*/ $ foo");
			RunTest(pattern, "(a (foo 1) (foo 2))", "(foo 1)", "(foo 2)");
			RunTest(pattern, "(a (foo 1))");
			pattern = TregexPattern.Compile("foo $ foo");
			RunTest(pattern, "(a (foo 1) (foo 2))", "(foo 1)", "(foo 2)");
			RunTest(pattern, "(a (foo 1))");
			pattern = TregexPattern.Compile("foo $ foo=a");
			Tree tree = TreeFromString("(a (foo 1) (foo 2) (foo 3))");
			TregexMatcher matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(foo 1)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(foo 2)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(foo 1)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(foo 3)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(foo 2)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(foo 1)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(foo 2)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(foo 3)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(foo 3)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(foo 1)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(foo 3)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(foo 2)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			RunTest("foo $ foo", "(a (foo 1))");
		}

		[NUnit.Framework.Test]
		public virtual void TestPrecedesFollows()
		{
			TregexPattern pattern = TregexPattern.Compile("/.*/ .. foo");
			RunTest(pattern, "(a (foo 1) (bar 2))");
			RunTest(pattern, "(a (bar 1) (foo 2))", "(bar 1)", "(1)");
			RunTest(pattern, "(a (bar 1) (baz 2) (foo 3))", "(bar 1)", "(1)", "(baz 2)", "(2)");
			RunTest(pattern, "(a (foo 1) (baz 2) (bar 3))");
			RunTest(pattern, "(a (bar (foo 1)) (baz 2))");
			RunTest(pattern, "(a (bar 1) (baz (foo 2)))", "(bar 1)", "(1)");
			RunTest(pattern, "(a (bar 1) (baz 2) (bif (foo 3)))", "(bar 1)", "(1)", "(baz 2)", "(2)");
			RunTest(pattern, "(a (bar (foo 1)) (baz 2) (bif 3))");
			RunTest(pattern, "(a (bar 1) (baz 2) (foo (bif 3)))", "(bar 1)", "(1)", "(baz 2)", "(2)");
			RunTest(pattern, "(a (bar 1) (foo (bif 2)) (baz 3))", "(bar 1)", "(1)");
			pattern = TregexPattern.Compile("/.*/ ,, foo");
			RunTest(pattern, "(a (foo 1) (bar 2))", "(bar 2)", "(2)");
			RunTest(pattern, "(a (bar 1) (foo 2))");
			RunTest(pattern, "(a (bar 1) (baz 2) (foo 3))");
			RunTest(pattern, "(a (foo 1) (baz 2) (bar 3))", "(baz 2)", "(2)", "(bar 3)", "(3)");
			RunTest(pattern, "(a (bar (foo 1)) (baz 2))", "(baz 2)", "(2)");
			RunTest(pattern, "(a (bar 1) (baz (foo 2)))");
			RunTest(pattern, "(a (bar 1) (baz 2) (bif (foo 3)))");
			RunTest(pattern, "(a (bar (foo 1)) (baz 2) (bif 3))", "(baz 2)", "(2)", "(bif 3)", "(3)");
			RunTest(pattern, "(a (bar 1) (baz 2) (foo (bif 3)))");
			RunTest(pattern, "(a (foo (bif 1)) (bar 2) (baz 3))", "(bar 2)", "(2)", "(baz 3)", "(3)");
			RunTest(pattern, "(a (bar 1) (foo (bif 2)) (baz 3))", "(baz 3)", "(3)");
		}

		[NUnit.Framework.Test]
		public virtual void TestImmediatePrecedesFollows()
		{
			// immediate precedes
			TregexPattern pattern = TregexPattern.Compile("/.*/ . foo");
			RunTest(pattern, "(a (foo 1) (bar 2))");
			RunTest(pattern, "(a (bar 1) (foo 2))", "(bar 1)", "(1)");
			RunTest(pattern, "(a (bar 1) (baz 2) (foo 3))", "(baz 2)", "(2)");
			RunTest(pattern, "(a (foo 1) (baz 2) (bar 3))");
			RunTest(pattern, "(a (bar (foo 1)) (baz 2))");
			RunTest(pattern, "(a (bar 1) (baz (foo 2)))", "(bar 1)", "(1)");
			RunTest(pattern, "(a (bar 1) (baz 2) (bif (foo 3)))", "(baz 2)", "(2)");
			RunTest(pattern, "(a (bar (foo 1)) (baz 2) (bif 3))");
			RunTest(pattern, "(a (bar 1) (baz 2) (foo (bif 3)))", "(baz 2)", "(2)");
			RunTest(pattern, "(a (bar 1) (foo (bif 2)) (baz 3))", "(bar 1)", "(1)");
			RunTest(pattern, "(a (bar 1) (foo 2) (baz 3) (foo 4) (bif 5))", "(bar 1)", "(1)", "(baz 3)", "(3)");
			RunTest(pattern, "(a (bar 1) (foo 2) (foo 3) (baz 4))", "(bar 1)", "(1)", "(foo 2)", "(2)");
			RunTest(pattern, "(a (b (c 1) (d 2)) (foo))", "(b (c 1) (d 2))", "(d 2)", "(2)");
			RunTest(pattern, "(a (b (c 1) (d 2)) (bar (foo 3)))", "(b (c 1) (d 2))", "(d 2)", "(2)");
			RunTest(pattern, "(a (b (c 1) (d 2)) (bar (baz 3) (foo 4)))", "(baz 3)", "(3)");
			RunTest(pattern, "(a (b (c 1) (d 2)) (bar (baz 2 3) (foo 4)))", "(baz 2 3)", "(3)");
			// immediate follows
			pattern = TregexPattern.Compile("/.*/ , foo");
			RunTest(pattern, "(a (foo 1) (bar 2))", "(bar 2)", "(2)");
			RunTest(pattern, "(a (bar 1) (foo 2))");
			RunTest(pattern, "(a (bar 1) (baz 2) (foo 3))");
			RunTest(pattern, "(a (foo 1) (baz 2) (bar 3))", "(baz 2)", "(2)");
			RunTest(pattern, "(a (bar (foo 1)) (baz 2))", "(baz 2)", "(2)");
			RunTest(pattern, "(a (bar 1) (baz (foo 2)))");
			RunTest(pattern, "(a (bar 1) (baz 2) (bif (foo 3)))");
			RunTest(pattern, "(a (bar (foo 1)) (baz 2) (bif 3))", "(baz 2)", "(2)");
			RunTest(pattern, "(a (bar 1) (baz 2) (foo (bif 3)))");
			RunTest(pattern, "(a (foo (bif 1)) (bar 2) (baz 3))", "(bar 2)", "(2)");
			RunTest(pattern, "(a (bar 1) (foo (bif 2)) (baz 3))", "(baz 3)", "(3)");
			RunTest(pattern, "(a (bar 1) (foo 2) (baz 3) (foo 4) (bif 5))", "(baz 3)", "(3)", "(bif 5)", "(5)");
			RunTest(pattern, "(a (bar 1) (foo 2) (foo 3) (baz 4))", "(foo 3)", "(3)", "(baz 4)", "(4)");
			RunTest(pattern, "(a (foo) (b (c 1) (d 2)))", "(b (c 1) (d 2))", "(c 1)", "(1)");
			RunTest(pattern, "(a (bar (foo 3)) (b (c 1) (d 2)))", "(b (c 1) (d 2))", "(c 1)", "(1)");
			RunTest(pattern, "(a (bar (baz 3) (foo 4)) (b (c 1) (d 2)))", "(b (c 1) (d 2))", "(c 1)", "(1)");
			RunTest(pattern, "(a (bar (foo 4) (baz 3)) (b (c 1) (d 2)))", "(baz 3)", "(3)");
		}

		[NUnit.Framework.Test]
		public virtual void TestLeftRightMostDescendant()
		{
			// B leftmost descendant of A
			RunTest("/.*/ <<, /1/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(a (foo 1 2) (bar 3 4))", "(foo 1 2)");
			RunTest("/.*/ <<, /2/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <<, foo", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(a (foo 1 2) (bar 3 4))");
			RunTest("/.*/ <<, baz", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(b (baz 5))");
			// B rightmost descendant of A
			RunTest("/.*/ <<- /1/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <<- /2/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(foo 1 2)");
			RunTest("/.*/ <<- /4/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(a (foo 1 2) (bar 3 4))", "(bar 3 4)");
			// A leftmost descendant of B
			RunTest("/.*/ >>, root", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(a (foo 1 2) (bar 3 4))", "(foo 1 2)", "(1)");
			RunTest("/.*/ >>, a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(foo 1 2)", "(1)");
			RunTest("/.*/ >>, bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(3)");
			// A rightmost descendant of B
			RunTest("/.*/ >>- root", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(b (baz 5))", "(baz 5)", "(5)");
			RunTest("/.*/ >>- a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(bar 3 4)", "(4)");
			RunTest("/.*/ >>- /1/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
		}

		[NUnit.Framework.Test]
		public virtual void TestFirstLastChild()
		{
			// A is the first child of B
			RunTest("/.*/ >, root", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(a (foo 1 2) (bar 3 4))");
			RunTest("/.*/ >, a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(foo 1 2)");
			RunTest("/.*/ >, foo", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(1)");
			RunTest("/.*/ >, bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(3)");
			// A is the last child of B
			RunTest("/.*/ >- root", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(b (baz 5))");
			RunTest("/.*/ >- a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(bar 3 4)");
			RunTest("/.*/ >- foo", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(2)");
			RunTest("/.*/ >- bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(4)");
			RunTest("/.*/ >- b", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(baz 5)");
			// B is the first child of A
			RunTest("/.*/ <, root", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <, a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <, /1/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(foo 1 2)");
			RunTest("/.*/ <, /2/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <, bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <, /3/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(bar 3 4)");
			RunTest("/.*/ <, /4/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			// B is the last child of A
			RunTest("/.*/ <- root", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <- a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <- /1/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <- /2/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(foo 1 2)");
			RunTest("/.*/ <- bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(a (foo 1 2) (bar 3 4))");
			RunTest("/.*/ <- /3/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <- /4/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(bar 3 4)");
		}

		[NUnit.Framework.Test]
		public virtual void TestIthChild()
		{
			// A is the ith child of B
			RunTest("/.*/ >1 root", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(a (foo 1 2) (bar 3 4))");
			RunTest("/.*/ >1 a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(foo 1 2)");
			RunTest("/.*/ >2 a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(bar 3 4)");
			RunTest("/.*/ >1 foo", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(1)");
			RunTest("/.*/ >2 foo", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(2)");
			RunTest("/.*/ >1 bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(3)");
			RunTest("/.*/ >2 bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(4)");
			// A is the -ith child of B
			RunTest("/.*/ >-1 root", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(b (baz 5))");
			RunTest("/.*/ >-1 a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(bar 3 4)");
			RunTest("/.*/ >-2 a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(foo 1 2)");
			RunTest("/.*/ >-1 foo", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(2)");
			RunTest("/.*/ >-2 bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(3)");
			RunTest("/.*/ >-1 bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(4)");
			RunTest("/.*/ >-1 b", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(baz 5)");
			RunTest("/.*/ >-2 b", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			// B is the ith child of A
			RunTest("/.*/ <1 root", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <1 a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <1 /1/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(foo 1 2)");
			RunTest("/.*/ <1 /2/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <1 bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <2 bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(a (foo 1 2) (bar 3 4))");
			RunTest("/.*/ <3 bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <1 /3/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(bar 3 4)");
			RunTest("/.*/ <1 /4/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <2 /4/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(bar 3 4)");
			// B is the -ith child of A
			RunTest("/.*/ <-1 root", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <-1 a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <-2 a", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <-1 /1/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <-2 /1/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(foo 1 2)");
			RunTest("/.*/ <-1 /2/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(foo 1 2)");
			RunTest("/.*/ <-2 /2/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <-1 bar", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(a (foo 1 2) (bar 3 4))");
			RunTest("/.*/ <-1 /3/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))");
			RunTest("/.*/ <-2 /3/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(bar 3 4)");
			RunTest("/.*/ <-1 /4/", "(root (a (foo 1 2) (bar 3 4)) (b (baz 5)))", "(bar 3 4)");
		}

		[NUnit.Framework.Test]
		public virtual void TestOnlyChild()
		{
			RunTest("foo <: bar", "(foo (bar 1))", "(foo (bar 1))");
			RunTest("foo <: bar", "(foo (bar 1) (bar 2))");
			RunTest("foo <: bar", "(foo)");
			RunTest("foo <: bar", "(foo (baz (bar))))");
			RunTest("foo <: bar", "(foo 1)");
			RunTest("bar >: foo", "(foo (bar 1))", "(bar 1)");
			RunTest("bar >: foo", "(foo (bar 1) (bar 2))");
			RunTest("bar >: foo", "(foo)");
			RunTest("bar >: foo", "(foo (baz (bar))))");
			RunTest("bar >: foo", "(bar (foo 1))");
			RunTest("/.*/ >: foo", "(a (foo (bar 1)) (foo (baz 2)))", "(bar 1)", "(baz 2)");
		}

		[NUnit.Framework.Test]
		public virtual void TestDominateUnaryChain()
		{
			RunTest("foo <<: bar", "(a (foo (b (c (d (bar))))))", "(foo (b (c (d (bar)))))");
			RunTest("foo <<: bar", "(a (foo (b (c (d (bar) (baz))))))");
			RunTest("foo <<: bar", "(a (foo (b (c (d (bar)) (baz)))))");
			RunTest("foo <<: bar", "(a (foo (b (c (d (bar))) (baz))))");
			RunTest("foo <<: bar", "(a (foo (b (c (d (bar)))) (baz)))");
			RunTest("foo <<: bar", "(a (foo (b (c (d (bar))))) (baz))", "(foo (b (c (d (bar)))))");
			RunTest("foo <<: bar", "(a (foo (b (c (bar)))))", "(foo (b (c (bar))))");
			RunTest("foo <<: bar", "(a (foo (b (bar))))", "(foo (b (bar)))");
			RunTest("foo <<: bar", "(a (foo (bar)))", "(foo (bar))");
			RunTest("bar >>: foo", "(a (foo (b (c (d (bar))))))", "(bar)");
			RunTest("bar >>: foo", "(a (foo (b (c (d (bar) (baz))))))");
			RunTest("bar >>: foo", "(a (foo (b (c (d (bar)) (baz)))))");
			RunTest("bar >>: foo", "(a (foo (b (c (d (bar))) (baz))))");
			RunTest("bar >>: foo", "(a (foo (b (c (d (bar)))) (baz)))");
			RunTest("bar >>: foo", "(a (foo (b (c (d (bar))))) (baz))", "(bar)");
			RunTest("bar >>: foo", "(a (foo (b (c (bar)))))", "(bar)");
			RunTest("bar >>: foo", "(a (foo (b (bar))))", "(bar)");
			RunTest("bar >>: foo", "(a (foo (bar)))", "(bar)");
		}

		[NUnit.Framework.Test]
		public virtual void TestPrecedingFollowingSister()
		{
			// test preceding sisters
			TregexPattern preceding = TregexPattern.Compile("/.*/ $.. baz");
			RunTest(preceding, "(a (foo 1) (bar 2) (baz 3))", "(foo 1)", "(bar 2)");
			RunTest(preceding, "(root (b (foo 1)) (a (foo 1) (bar 2) (baz 3)))", "(foo 1)", "(bar 2)");
			RunTest(preceding, "(root (a (foo 1) (bar 2) (baz 3)) (b (foo 1)))", "(foo 1)", "(bar 2)");
			RunTest(preceding, "(a (foo 1) (baz 2) (bar 3))", "(foo 1)");
			RunTest(preceding, "(a (baz 1) (foo 2) (bar 3))");
			// test immediately preceding sisters
			TregexPattern impreceding = TregexPattern.Compile("/.*/ $. baz");
			RunTest(impreceding, "(a (foo 1) (bar 2) (baz 3))", "(bar 2)");
			RunTest(impreceding, "(root (b (foo 1)) (a (foo 1) (bar 2) (baz 3)))", "(bar 2)");
			RunTest(impreceding, "(root (a (foo 1) (bar 2) (baz 3)) (b (foo 1)))", "(bar 2)");
			RunTest(impreceding, "(a (foo 1) (baz 2) (bar 3))", "(foo 1)");
			RunTest(impreceding, "(a (baz 1) (foo 2) (bar 3))");
			// test following sisters
			TregexPattern following = TregexPattern.Compile("/.*/ $,, baz");
			RunTest(following, "(a (foo 1) (bar 2) (baz 3))");
			RunTest(following, "(root (b (foo 1)) (a (foo 1) (bar 2) (baz 3)))");
			RunTest(following, "(root (a (foo 1) (bar 2) (baz 3)) (b (foo 1)))");
			RunTest(following, "(root (a (baz 1) (bar 2) (foo 3)) (b (foo 1)))", "(bar 2)", "(foo 3)");
			RunTest(following, "(a (foo 1) (baz 2) (bar 3))", "(bar 3)");
			RunTest(following, "(a (baz 1) (foo 2) (bar 3))", "(foo 2)", "(bar 3)");
			// test immediately following sisters
			TregexPattern imfollowing = TregexPattern.Compile("/.*/ $, baz");
			RunTest(imfollowing, "(a (foo 1) (bar 2) (baz 3))");
			RunTest(imfollowing, "(root (b (foo 1)) (a (foo 1) (bar 2) (baz 3)))");
			RunTest(imfollowing, "(root (a (foo 1) (bar 2) (baz 3)) (b (foo 1)))");
			RunTest(imfollowing, "(root (a (baz 1) (bar 2) (foo 3)) (b (foo 1)))", "(bar 2)");
			RunTest(imfollowing, "(a (foo 1) (baz 2) (bar 3))", "(bar 3)");
			RunTest(imfollowing, "(a (baz 1) (foo 2) (bar 3))", "(foo 2)");
		}

		[NUnit.Framework.Test]
		public virtual void TestCategoryFunctions()
		{
			IFunction<string, string> fooCategory = new _IFunction_584();
			TregexPatternCompiler fooCompiler = new TregexPatternCompiler(fooCategory);
			TregexPattern fooTregex = fooCompiler.Compile("@foo > bar");
			RunTest(fooTregex, "(bar (foo 0))", "(foo 0)");
			RunTest(fooTregex, "(bar (bar 0))", "(bar 0)");
			RunTest(fooTregex, "(foo (foo 0))");
			RunTest(fooTregex, "(foo (bar 0))");
			IFunction<string, string> barCategory = new _IFunction_603();
			TregexPatternCompiler barCompiler = new TregexPatternCompiler(barCategory);
			TregexPattern barTregex = barCompiler.Compile("@bar > foo");
			RunTest(barTregex, "(bar (foo 0))");
			RunTest(barTregex, "(bar (bar 0))");
			RunTest(barTregex, "(foo (foo 0))", "(foo 0)");
			RunTest(barTregex, "(foo (bar 0))", "(bar 0)");
			// These should still work, since the tregex patterns have
			// different category functions.  Old enough versions of tregex do
			// not allow for that.
			RunTest(fooTregex, "(bar (foo 0))", "(foo 0)");
			RunTest(fooTregex, "(bar (bar 0))", "(bar 0)");
			RunTest(fooTregex, "(foo (foo 0))");
			RunTest(fooTregex, "(foo (bar 0))");
		}

		private sealed class _IFunction_584 : IFunction<string, string>
		{
			public _IFunction_584()
			{
			}

			public string Apply(string label)
			{
				if (label == null)
				{
					return label;
				}
				if (label.Equals("bar"))
				{
					return "foo";
				}
				return label;
			}
		}

		private sealed class _IFunction_603 : IFunction<string, string>
		{
			public _IFunction_603()
			{
			}

			public string Apply(string label)
			{
				if (label == null)
				{
					return label;
				}
				if (label.Equals("foo"))
				{
					return "bar";
				}
				return label;
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestCategoryDisjunction()
		{
			IFunction<string, string> abCategory = new _IFunction_632();
			TregexPatternCompiler abCompiler = new TregexPatternCompiler(abCategory);
			TregexPattern aaaTregex = abCompiler.Compile("foo > @aaa");
			RunTest(aaaTregex, "(aaa (foo 0))", "(foo 0)");
			RunTest(aaaTregex, "(abc (foo 0))", "(foo 0)");
			RunTest(aaaTregex, "(bbb (foo 0))");
			RunTest(aaaTregex, "(bcd (foo 0))");
			RunTest(aaaTregex, "(ccc (foo 0))");
			TregexPattern bbbTregex = abCompiler.Compile("foo > @bbb");
			RunTest(bbbTregex, "(aaa (foo 0))");
			RunTest(bbbTregex, "(abc (foo 0))");
			RunTest(bbbTregex, "(bbb (foo 0))", "(foo 0)");
			RunTest(bbbTregex, "(bcd (foo 0))", "(foo 0)");
			RunTest(bbbTregex, "(ccc (foo 0))");
			TregexPattern bothTregex = abCompiler.Compile("foo > @aaa|bbb");
			RunTest(bothTregex, "(aaa (foo 0))", "(foo 0)");
			RunTest(bothTregex, "(abc (foo 0))", "(foo 0)");
			RunTest(bothTregex, "(bbb (foo 0))", "(foo 0)");
			RunTest(bothTregex, "(bcd (foo 0))", "(foo 0)");
			RunTest(bothTregex, "(ccc (foo 0))");
		}

		private sealed class _IFunction_632 : IFunction<string, string>
		{
			public _IFunction_632()
			{
			}

			public string Apply(string label)
			{
				if (label == null)
				{
					return label;
				}
				if (label.StartsWith("a"))
				{
					return "aaa";
				}
				if (label.StartsWith("b"))
				{
					return "bbb";
				}
				return label;
			}
		}

		// tests for following/preceding described chains
		[NUnit.Framework.Test]
		public virtual void TestPrecedesDescribedChain()
		{
			RunTest("DT .+(JJ) NN", "(NP (DT the) (JJ large) (JJ green) (NN house))", "(DT the)");
			RunTest("DT .+(@JJ) /^NN/", "(NP (PDT both) (DT the) (JJ-SIZE large) (JJ-COLOUR green) (NNS houses))", "(DT the)");
			RunTest("NN ,+(JJ) DT", "(NP (DT the) (JJ large) (JJ green) (NN house))", "(NN house)");
			RunTest("NNS ,+(@JJ) /^DT/", "(NP (PDT both) (DT the) (JJ-SIZE large) (JJ-COLOUR green) (NNS houses))", "(NNS houses)");
			RunTest("NNS ,+(/^(JJ|DT).*$/) PDT", "(NP (PDT both) (DT the) (JJ-SIZE large) (JJ-COLOUR green) (NNS houses))", "(NNS houses)");
			RunTest("NNS ,+(@JJ) JJ", "(NP (PDT both) (DT the) (JJ large) (JJ-COLOUR green) (NNS houses))", "(NNS houses)");
		}

		// TODO: The patterns below should work but don't
		// runTest("DT .+(JJ) JJ", "(NP (DT the) (JJ large) (JJ green) (NN house))", "(DT the)");
		// runTest("NNS ,+(@JJ) /JJ/", "(NP (PDT both) (DT the) (JJ large) (JJ-COLOUR green) (NNS houses))", "(NNS houses)");
		// runTest("NNS ,+(@JJ) /^JJ$/", "(NP (PDT both) (DT the) (JJ large) (JJ-COLOUR green) (NNS houses))", "(NNS houses)");
		// runTest("NNS ,+(@JJ) @JJ", "(NP (PDT both) (DT the) (JJ large) (JJ-COLOUR green) (NNS houses))", "(NNS houses)");
		// TODO: tests for patterns made with different headfinders,
		// which will verify the thread safety of using different headfinders
		[NUnit.Framework.Test]
		public virtual void TestDominateDescribedChain()
		{
			RunTest("foo <+(bar) baz", "(a (foo (baz)))", "(foo (baz))");
			RunTest("foo <+(bar) baz", "(a (foo (bar (baz))))", "(foo (bar (baz)))");
			RunTest("foo <+(bar) baz", "(a (foo (bar (bar (baz)))))", "(foo (bar (bar (baz))))");
			RunTest("foo <+(bar) baz", "(a (foo (bif (baz))))");
			RunTest("foo <+(!bif) baz", "(a (foo (bif (baz))))");
			RunTest("foo <+(!bif) baz", "(a (foo (bar (baz))))", "(foo (bar (baz)))");
			RunTest("foo <+(/b/) baz", "(a (foo (bif (baz))))", "(foo (bif (baz)))");
			RunTest("foo <+(/b/) baz", "(a (foo (bar (bif (baz)))))", "(foo (bar (bif (baz))))");
			RunTest("foo <+(bar) baz", "(a (foo (bar (blah 1) (bar (baz)))))", "(foo (bar (blah 1) (bar (baz))))");
			RunTest("baz >+(bar) foo", "(a (foo (baz)))", "(baz)");
			RunTest("baz >+(bar) foo", "(a (foo (bar (baz))))", "(baz)");
			RunTest("baz >+(bar) foo", "(a (foo (bar (bar (baz)))))", "(baz)");
			RunTest("baz >+(bar) foo", "(a (foo (bif (baz))))");
			RunTest("baz >+(!bif) foo", "(a (foo (bif (baz))))");
			RunTest("baz >+(!bif) foo", "(a (foo (bar (baz))))", "(baz)");
			RunTest("baz >+(/b/) foo", "(a (foo (bif (baz))))", "(baz)");
			RunTest("baz >+(/b/) foo", "(a (foo (bar (bif (baz)))))", "(baz)");
			RunTest("baz >+(bar) foo", "(a (foo (bar (blah 1) (bar (baz)))))", "(baz)");
		}

		[NUnit.Framework.Test]
		public virtual void TestSegmentedAndEqualsExpressions()
		{
			RunTest("foo : bar", "(a (foo) (bar))", "(foo)");
			RunTest("foo : bar", "(a (foo))");
			RunTest("(foo << bar) : (foo << baz)", "(a (foo (bar 1)) (foo (baz 2)))", "(foo (bar 1))");
			RunTest("(foo << bar) : (foo << baz)", "(a (foo (bar 1)) (foo (baz 2)))", "(foo (bar 1))");
			RunTest("(foo << bar) == (foo << baz)", "(a (foo (bar)) (foo (baz)))");
			RunTest("(foo << bar) : (foo << baz)", "(a (foo (bar) (baz)))", "(foo (bar) (baz))");
			RunTest("(foo << bar) == (foo << baz)", "(a (foo (bar) (baz)))", "(foo (bar) (baz))");
			RunTest("(foo << bar) : (baz >> a)", "(a (foo (bar) (baz)))", "(foo (bar) (baz))");
			RunTest("(foo << bar) == (baz >> a)", "(a (foo (bar) (baz)))");
			RunTest("foo == foo", "(a (foo (bar)))", "(foo (bar))");
			RunTest("foo << bar == foo", "(a (foo (bar)) (foo (baz)))", "(foo (bar))");
			RunTest("foo << bar == foo", "(a (foo (bar) (baz)))", "(foo (bar) (baz))");
			RunTest("foo << bar == foo << baz", "(a (foo (bar) (baz)))", "(foo (bar) (baz))");
			RunTest("foo << bar : (foo << baz)", "(a (foo (bar)) (foo (baz)))", "(foo (bar))");
		}

		[NUnit.Framework.Test]
		public virtual void TestTwoChildren()
		{
			RunTest("foo << bar << baz", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))");
			// this is a poorly written pattern that will match 4 times
			RunTest("foo << __ << baz", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))");
			// this one also matches 4 times
			RunTest("foo << bar << __", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))");
			// this one also matches 4 times
			RunTest("foo << __ << __", "(foo (bar 1))", "(foo (bar 1))", "(foo (bar 1))", "(foo (bar 1))", "(foo (bar 1))");
			// same thing, just making sure variable assignment doesn't throw
			// it off
			RunTest("foo << __=a << __=b", "(foo (bar 1))", "(foo (bar 1))", "(foo (bar 1))", "(foo (bar 1))", "(foo (bar 1))");
			// 16 times!  hopefully no one writes patterns like this
			RunTest("foo << __ << __", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))"
				, "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))");
			// note: this matches because we set a=(bar 1), b=(1)
			RunTest("(foo << __=a << __=b) : (=a !== =b)", "(foo (bar 1))", "(foo (bar 1))", "(foo (bar 1))");
			RunTest("(foo < __=a < __=b) : (=a !== =b)", "(foo (bar 1))");
			// 12 times: 16 possible ways to match the nodes, but 4 of them
			// are ruled out because they are the same node matching twice
			RunTest("(foo << __=a << __=b) : (=a !== =b)", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))"
				, "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))", "(foo (bar 1) (baz 2))");
			// would need three unique descendants, but we only have two, so
			// this pattern doesn't match anything
			RunTest("(foo << __=a << __=b << __=c) : " + "(=a !== =b) : (=a !== =c) : (=b !== =c)", "(foo (bar 1))");
		}

		// TODO: this should work, but it doesn't even parse
		//runTest("foo << __=a << __=b : !(=a == =b)", "(foo (bar 1))");
		[NUnit.Framework.Test]
		public virtual void TestDocExamples()
		{
			RunTest("S < VP < NP", "(S (VP) (NP))", "(S (VP) (NP))");
			RunTest("S < VP < NP", "(a (S (VP) (NP)) (S (NP) (VP)))", "(S (VP) (NP))", "(S (NP) (VP))");
			RunTest("S < VP < NP", "(S (VP (NP)))");
			RunTest("S < VP & < NP", "(S (VP) (NP))", "(S (VP) (NP))");
			RunTest("S < VP & < NP", "(a (S (VP) (NP)) (S (NP) (VP)))", "(S (VP) (NP))", "(S (NP) (VP))");
			RunTest("S < VP & < NP", "(S (VP (NP)))");
			RunTest("S < VP << NP", "(S (VP (NP)))", "(S (VP (NP)))");
			RunTest("S < VP << NP", "(S (VP) (foo NP))", "(S (VP) (foo NP))");
			RunTest("S < (VP < NP)", "(S (VP (NP)))", "(S (VP (NP)))");
			RunTest("S < (NP $++ VP)", "(S (NP) (VP))", "(S (NP) (VP))");
			RunTest("S < (NP $++ VP)", "(S (NP VP))");
			RunTest("(NP < NN | < NNS)", "((NP NN) (NP foo) (NP NNS))", "(NP NN)", "(NP NNS)");
			RunTest("(NP (< NN | < NNS) & > S)", "(foo (S (NP NN) (NP foo) (NP NNS)) (NP NNS))", "(NP NN)", "(NP NNS)");
			RunTest("(NP [< NN | < NNS] & > S)", "(foo (S (NP NN) (NP foo) (NP NNS)) (NP NNS))", "(NP NN)", "(NP NNS)");
		}

		/// <summary>
		/// An example from our code which looks for month-day-year patterns
		/// in PTB.
		/// </summary>
		/// <remarks>
		/// An example from our code which looks for month-day-year patterns
		/// in PTB.  Relies on the pattern splitting and variable matching
		/// features.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestMonthDayYear()
		{
			string MonthRegex = "January|February|March|April|May|June|July|August|September|October|November|December|Jan\\.|Feb\\.|Mar\\.|Apr\\.|Aug\\.|Sep\\.|Sept\\.|Oct\\.|Nov\\.|Dec\\.";
			string testPattern = "NP=root <1 (NP=monthdayroot <1 (NNP=month <: /" + MonthRegex + "/) <2 (CD=day <: __)) <2 (/^,$/=comma <: /^,$/) <3 (NP=yearroot <: (CD=year <: __)) : (=root <- =yearroot) : (=monthdayroot <- =day)";
			RunTest(testPattern, "(ROOT (S (NP (NNP Mr.) (NNP Good)) (VP (VBZ devotes) (NP (RB much) (JJ serious) (NN space)) (PP (TO to) (NP (NP (DT the) (NNS events)) (PP (IN of) (NP (NP (NP (NNP Feb.) (CD 25)) (, ,) (NP (CD 1942))) (, ,) (SBAR (WHADVP (WRB when)) (S (NP (JJ American) (NNS gunners)) (VP (VBD spotted) (NP (NP (JJ strange) (NNS lights)) (PP (IN in) (NP (NP (DT the) (NN sky)) (PP (IN above) (NP (NNP Los) (NNP Angeles)))))))))))))) (. .)))"
				, "(NP (NP (NNP Feb.) (CD 25)) (, ,) (NP (CD 1942)))");
			RunTest(testPattern, "(ROOT (S (NP (DT The) (JJ preferred) (NNS shares)) (VP (MD will) (VP (VB carry) (NP (NP (DT a) (JJ floating) (JJ annual) (NN dividend)) (ADJP (JJ equal) (PP (TO to) (NP (NP (CD 72) (NN %)) (PP (IN of) (NP (NP (DT the) (JJ 30-day) (NNS bankers) (POS ')) (NN acceptance) (NN rate))))))) (PP (IN until) (NP (NP (NNP Dec.) (CD 31)) (, ,) (NP (CD 1994)))))) (. .)))"
				, "(NP (NP (NNP Dec.) (CD 31)) (, ,) (NP (CD 1994)))");
			RunTest(testPattern, "(ROOT (S (NP (PRP It)) (VP (VBD said) (SBAR (S (NP (NN debt)) (VP (VBD remained) (PP (IN at) (NP (NP (DT the) (QP ($ $) (CD 1.22) (CD billion))) (SBAR (WHNP (DT that)) (S (VP (VBZ has) (VP (VBD prevailed) (PP (IN since) (NP (JJ early) (CD 1989))))))))) (, ,) (SBAR (IN although) (S (NP (IN that)) (VP (VBN compared) (PP (IN with) (NP (NP (QP ($ $) (CD 911) (CD million))) (PP (IN at) (NP (NP (NNP Sept.) (CD 30)) (, ,) (NP (CD 1988))))))))))))) (. .)))"
				, "(NP (NP (NNP Sept.) (CD 30)) (, ,) (NP (CD 1988)))");
			RunTest(testPattern, "(ROOT (S (NP (DT The) (JJ new) (NNS notes)) (VP (MD will) (VP (VB bear) (NP (NN interest)) (PP (PP (IN at) (NP (NP (CD 5.5) (NN %)) (PP (IN through) (NP (NP (NNP July) (CD 31)) (, ,) (NP (CD 1991)))))) (, ,) (CC and) (ADVP (RB thereafter)) (PP (IN at) (NP (CD 10) (NN %)))))) (. .)))"
				, "(NP (NP (NNP July) (CD 31)) (, ,) (NP (CD 1991)))");
			RunTest(testPattern, "(ROOT (S (NP (NP (NNP Francis) (NNP M.) (NNP Wheat)) (, ,) (NP (NP (DT a) (JJ former) (NNPS Securities)) (CC and) (NP (NNP Exchange) (NNP Commission) (NN member))) (, ,)) (VP (VBD headed) (NP (NP (DT the) (NN panel)) (SBAR (WHNP (WDT that)) (S (VP (VBD had) (VP (VP (VBN studied) (NP (DT the) (NNS issues)) (PP (IN for) (NP (DT a) (NN year)))) (CC and) (VP (VBD proposed) (NP (DT the) (NNP FASB)) (PP (IN on) (NP (NP (NNP March) (CD 30)) (, ,) (NP (CD 1972))))))))))) (. .)))"
				, "(NP (NP (NNP March) (CD 30)) (, ,) (NP (CD 1972)))");
			RunTest(testPattern, "(ROOT (S (NP (DT The) (NNP FASB)) (VP (VBD had) (NP (PRP$ its) (JJ initial) (NN meeting)) (PP (IN on) (NP (NP (NNP March) (CD 28)) (, ,) (NP (CD 1973))))) (. .)))", "(NP (NP (NNP March) (CD 28)) (, ,) (NP (CD 1973)))");
			RunTest(testPattern, "(ROOT (S (S (PP (IN On) (NP (NP (NNP Dec.) (CD 13)) (, ,) (NP (CD 1973)))) (, ,) (NP (PRP it)) (VP (VBD issued) (NP (PRP$ its) (JJ first) (NN rule)))) (: ;) (S (NP (PRP it)) (VP (VBD required) (S (NP (NNS companies)) (VP (TO to) (VP (VB disclose) (NP (NP (JJ foreign) (NN currency) (NNS translations)) (PP (IN in) (NP (NNP U.S.) (NNS dollars))))))))) (. .)))"
				, "(NP (NP (NNP Dec.) (CD 13)) (, ,) (NP (CD 1973)))");
			RunTest(testPattern, "(ROOT (S (NP (NP (NNP Fidelity) (NNPS Investments)) (, ,) (NP (NP (DT the) (NN nation) (POS 's)) (JJS largest) (NN fund) (NN company)) (, ,)) (VP (VBD said) (SBAR (S (NP (NN phone) (NN volume)) (VP (VBD was) (NP (NP (QP (RBR more) (IN than) (JJ double)) (PRP$ its) (JJ typical) (NN level)) (, ,) (CC but) (ADVP (RB still)) (NP (NP (NN half) (DT that)) (PP (IN of) (NP (NP (NNP Oct.) (CD 19)) (, ,) (NP (CD 1987)))))))))) (. .)))"
				, "(NP (NP (NNP Oct.) (CD 19)) (, ,) (NP (CD 1987)))");
			RunTest(testPattern, "(ROOT (S (NP (JJ SOFT) (NN CONTACT) (NNS LENSES)) (VP (VP (VBP WON) (NP (JJ federal) (NN blessing)) (PP (IN on) (NP (NP (NNP March) (CD 18)) (, ,) (NP (CD 1971))))) (, ,) (CC and) (VP (ADVP (RB quickly)) (VBD became) (NP (NN eye) (NNS openers)) (PP (IN for) (NP (PRP$ their) (NNS makers))))) (. .)))"
				, "(NP (NP (NNP March) (CD 18)) (, ,) (NP (CD 1971)))");
			RunTest(testPattern, "(ROOT (NP (NP (NP (VBN Annualized) (NN interest) (NNS rates)) (PP (IN on) (NP (JJ certain) (NNS investments))) (SBAR (IN as) (S (VP (VBN reported) (PP (IN by) (NP (DT the) (NNP Federal) (NNP Reserve) (NNP Board))) (PP (IN on) (NP (DT a) (JJ weekly-average) (NN basis))))))) (: :) (NP-TMP (NP (CD 1989)) (CC and) (NP (NP (NNP Wednesday)) (NP (NP (NNP October) (CD 4)) (, ,) (NP (CD 1989))))) (. .)))"
				, "(NP (NP (NNP October) (CD 4)) (, ,) (NP (CD 1989)))");
			RunTest(testPattern, "(ROOT (S (S (ADVP (RB Together))) (, ,) (NP (DT the) (CD two) (NNS stocks)) (VP (VP (VBD wreaked) (NP (NN havoc)) (PP (IN among) (NP (NN takeover) (NN stock) (NNS traders)))) (, ,) (CC and) (VP (VBD caused) (NP (NP (DT a) (ADJP (CD 7.3) (NN %)) (NN drop)) (PP (IN in) (NP (DT the) (NNP Dow) (NNP Jones) (NNP Transportation) (NNP Average))) (, ,) (ADJP (JJ second) (PP (IN in) (NP (NN size))) (PP (RB only) (TO to) (NP (NP (DT the) (NN stock-market) (NN crash)) (PP (IN of) (NP (NP (NNP Oct.) (CD 19)) (, ,) (NP (CD 1987)))))))))) (. .)))"
				, "(NP (NP (NNP Oct.) (CD 19)) (, ,) (NP (CD 1987)))");
		}

		/// <summary>More complex tests, often based on examples from our source code</summary>
		[NUnit.Framework.Test]
		public virtual void TestComplex()
		{
			string testPattern = "S < (NP=m1 $.. (VP < ((/VB/ < /^(am|are|is|was|were|'m|'re|'s|be)$/) $.. NP=m2)))";
			string testTree = "(S (NP (NP (DT The) (JJ next) (NN stop)) (PP (IN on) (NP (DT the) (NN itinerary)))) (VP (VBD was) (NP (NP (NNP Chad)) (, ,) (SBAR (WHADVP (WRB where)) (S (NP (NNP Chen)) (VP (VBD dined) (PP (IN with) (NP (NP (NNP Chad) (POS \'s)) (NNP President) (NNP Idris) (NNP Debi)))))))) (. .))";
			RunTest(testPattern, "(ROOT " + testTree + ")", testTree);
			testTree = "(S (NP (NNP Chen) (NNP Shui) (HYPH -) (NNP bian)) (VP (VBZ is) (NP (NP (DT the) (JJ first) (NML (NNP ROC) (NN president))) (SBAR (S (ADVP (RB ever)) (VP (TO to) (VP (VB travel) (PP (IN to) (NP (JJ western) (NNP Africa))))))))) (. .))";
			RunTest(testPattern, "(ROOT " + testTree + ")", testTree);
			testTree = "(ROOT (S (NP (PRP$ My) (NN dog)) (VP (VBZ is) (VP (VBG eating) (NP (DT a) (NN sausage)))) (. .)))";
			RunTest(testPattern, testTree);
			testTree = "(ROOT (S (NP (PRP He)) (VP (MD will) (VP (VB be) (ADVP (RB here) (RB soon)))) (. .)))";
			RunTest(testPattern, testTree);
			testPattern = "/^NP(?:-TMP|-ADV)?$/=m1 < (NP=m2 $- /^,$/ $-- NP=m3 !$ CC|CONJP)";
			testTree = "(ROOT (S (NP (NP (NP (NP (DT The) (NNP ROC) (POS \'s)) (NN ambassador)) (PP (IN to) (NP (NNP Nicaragua)))) (, ,) (NP (NNP Antonio) (NNP Tsai)) (, ,)) (ADVP (RB bluntly)) (VP (VBD argued) (PP (IN in) (NP (NP (DT a) (NN briefing)) (PP (IN with) (NP (NNP Chen))))) (SBAR (IN that) (S (NP (NP (NP (NNP Taiwan) (POS \'s)) (JJ foreign) (NN assistance)) (PP (IN to) (NP (NNP Nicaragua)))) (VP (VBD was) (VP (VBG being) (ADJP (JJ misused))))))) (. .)))";
			string expectedResult = "(NP (NP (NP (NP (DT The) (NNP ROC) (POS 's)) (NN ambassador)) (PP (IN to) (NP (NNP Nicaragua)))) (, ,) (NP (NNP Antonio) (NNP Tsai)) (, ,))";
			RunTest(testPattern, testTree, expectedResult);
			testTree = "(ROOT (S (PP (IN In) (NP (NP (DT the) (NN opinion)) (PP (IN of) (NP (NP (NNP Norman) (NNP Hsu)) (, ,) (NP (NP (NN vice) (NN president)) (PP (IN of) (NP (NP (DT a) (NNS foods) (NN company)) (SBAR (WHNP (WHNP (WP$ whose) (NN family)) (PP (IN of) (NP (CD four)))) (S (VP (VBD had) (VP (VBN spent) (NP (QP (DT a) (JJ few)) (NNS years)) (PP (IN in) (NP (NNP New) (NNP Zealand))) (PP (IN before) (S (VP (VBG moving) (PP (IN to) (NP (NNP Dongguan))))))))))))))))) (, ,) (`` \") (NP (NP (DT The) (JJ first) (NN thing)) (VP (TO to) (VP (VB do)))) (VP (VBZ is) (S (VP (VB ask) (NP (DT the) (NNS children)) (NP (PRP$ their) (NN reason)) (PP (IN for) (S (VP (VBG saying) (NP (JJ such) (NNS things)))))))) (. .)))";
			expectedResult = "(NP (NP (NNP Norman) (NNP Hsu)) (, ,) (NP (NP (NN vice) (NN president)) (PP (IN of) (NP (NP (DT a) (NNS foods) (NN company)) (SBAR (WHNP (WHNP (WP$ whose) (NN family)) (PP (IN of) (NP (CD four)))) (S (VP (VBD had) (VP (VBN spent) (NP (QP (DT a) (JJ few)) (NNS years)) (PP (IN in) (NP (NNP New) (NNP Zealand))) (PP (IN before) (S (VP (VBG moving) (PP (IN to) (NP (NNP Dongguan))))))))))))))";
			RunTest(testPattern, testTree, expectedResult);
			testTree = "(ROOT (S (NP (NP (NNP Banana)) (, ,) (NP (NN orange)) (, ,) (CC and) (NP (NN apple))) (VP (VBP are) (NP (NNS fruits))) (. .)))";
			RunTest(testPattern, testTree);
			testTree = "(ROOT (S (NP (PRP He)) (, ,) (ADVP (RB however)) (, ,) (VP (VBZ does) (RB not) (VP (VB look) (ADJP (JJ fine)))) (. .)))";
			RunTest(testPattern, testTree);
		}

		/// <summary>More complex patterns to test</summary>
		[NUnit.Framework.Test]
		public virtual void TestComplex2()
		{
			string[] inputTrees = new string[] { "(ROOT (S (NP (PRP You)) (VP (VBD did) (VP (VB go) (WHADVP (WRB How) (JJ long)) (PP (IN for)))) (. .)))", "(ROOT (S (NP (NNS Raccoons)) (VP (VBP do) (VP (VB come) (WHADVP (WRB When)) (PRT (RP out)))) (. .)))"
				, "(ROOT (S (NP (PRP She)) (VP (VBZ is) (VP (WHADVP (WRB Where)) (VBG working))) (. .)))", "(ROOT (S (NP (PRP You)) (VP (VBD did) (VP (WHNP (WP What)) (VB do))) (. .)))", "(ROOT (S (NP (PRP You)) (VP (VBD did) (VP (VB do) (PP (IN in) (NP (NNP Australia))) (WHNP (WP What)))) (. .)))"
				 };
			string pattern = "WHADVP=whadvp > VP $+ /[A-Z]*/=last ![$++ (PP < NP)]";
			RunTest(pattern, inputTrees[0], "(WHADVP (WRB How) (JJ long))");
			RunTest(pattern, inputTrees[1], "(WHADVP (WRB When))");
			RunTest(pattern, inputTrees[2], "(WHADVP (WRB Where))");
			RunTest(pattern, inputTrees[3]);
			RunTest(pattern, inputTrees[4]);
			pattern = "VP < (/^WH/=wh $++ /^VB/=vb)";
			RunTest(pattern, inputTrees[0]);
			RunTest(pattern, inputTrees[1]);
			RunTest(pattern, inputTrees[2], "(VP (WHADVP (WRB Where)) (VBG working))");
			RunTest(pattern, inputTrees[3], "(VP (WHNP (WP What)) (VB do))");
			RunTest(pattern, inputTrees[4]);
			pattern = "PP=pp > VP $+ WHNP=whnp";
			RunTest(pattern, inputTrees[0]);
			RunTest(pattern, inputTrees[1]);
			RunTest(pattern, inputTrees[2]);
			RunTest(pattern, inputTrees[3]);
			RunTest(pattern, inputTrees[4], "(PP (IN in) (NP (NNP Australia)))");
		}

		[NUnit.Framework.Test]
		public virtual void TestNamed()
		{
			Tree tree = TreeFromString("(a (foo 1) (bar 2) (bar 3))");
			TregexPattern pattern = TregexPattern.Compile("foo=a $ bar=b");
			TregexMatcher matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(foo 1)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(foo 1)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("(bar 2)", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(foo 1)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(foo 1)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("(bar 3)", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestLink()
		{
			// matched node will be (bar 3), the next bar matches (bar 2), and
			// the foo at the end obviously matches the (foo 1)
			RunTest("bar $- (bar $- foo)", "(a (foo 1) (bar 2) (bar 3))", "(bar 3)");
			// same thing, but this tests the link functionality, as the
			// second match should also be (bar 2)
			RunTest("bar=a $- (~a $- foo)", "(a (foo 1) (bar 2) (bar 3))", "(bar 3)");
			// won't work, since (bar 3) doesn't satisfy the next-to-foo
			// relation, and (bar 2) isn't the same node as (bar 3)
			RunTest("bar=a $- (=a $- foo)", "(a (foo 1) (bar 2) (bar 3))");
			// links can be saved as named nodes as well, so this should work
			RunTest("bar=a $- (~a=b $- foo)", "(a (foo 1) (bar 2) (bar 3))", "(bar 3)");
			// run a few of the same tests, but this time dissect the results
			// to make sure the captured nodes are the correct nodes
			Tree tree = TreeFromString("(a (foo 1) (bar 2) (bar 3))");
			TregexPattern pattern = TregexPattern.Compile("bar=a $- (~a $- foo)");
			TregexMatcher matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(bar 3)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(bar 3)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			tree = TreeFromString("(a (foo 1) (bar 2) (bar 3))");
			pattern = TregexPattern.Compile("bar=a $- (~a=b $- foo)");
			matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(bar 3)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(bar 3)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("(bar 2)", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			tree = TreeFromString("(a (foo 1) (bar 2) (bar 3))");
			pattern = TregexPattern.Compile("bar=a $- (~a=b $- foo=c)");
			matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(bar 3)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(bar 3)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("(bar 2)", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.AreEqual("(foo 1)", matcher.GetNode("c").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestNonsense()
		{
			// can't name a variable twice
			try
			{
				TregexPattern pattern = TregexPattern.Compile("foo=a $ bar=a");
				throw new Exception("Expected a parse exception");
			}
			catch (TregexParseException)
			{
			}
			// yay, passed
			// another way of doing the same thing
			try
			{
				TregexPattern pattern = TregexPattern.Compile("foo=a > bar=b $ ~a=b");
				throw new Exception("Expected a parse exception");
			}
			catch (TregexParseException)
			{
			}
			// yay, passed
			// ... but this should work
			TregexPattern.Compile("foo=a > bar=b $ ~a");
			// can't link to a variable that doesn't exist yet
			try
			{
				TregexPattern pattern = TregexPattern.Compile("~a $- (bar=a $- foo)");
				throw new Exception("Expected a parse exception");
			}
			catch (TregexParseException)
			{
			}
			// yay, passed
			// can't reference a variable that doesn't exist yet
			try
			{
				TregexPattern pattern = TregexPattern.Compile("=a $- (bar=a $- foo)");
				throw new Exception("Expected a parse exception");
			}
			catch (TregexParseException)
			{
			}
			// yay, passed
			// you'd have to be really demented to do this
			try
			{
				TregexPattern pattern = TregexPattern.Compile("~a=a $- (bar=b $- foo)");
				throw new Exception("Expected a parse exception");
			}
			catch (TregexParseException)
			{
			}
			// yay, passed
			// This should work... no reason this would barf
			TregexPattern.Compile("foo=a : ~a");
			TregexPattern.Compile("a < foo=a | < bar=a");
			// can't have a link in one part of a disjunction to a variable in
			// another part of the disjunction; it won't be set if you get to
			// the ~a part, after all
			try
			{
				TregexPattern.Compile("a < foo=a | < ~a");
				throw new Exception("Expected a parse exception");
			}
			catch (TregexParseException)
			{
			}
			// yay, passed
			// same, but for references
			try
			{
				TregexPattern.Compile("a < foo=a | < =a");
				throw new Exception("Expected a parse exception");
			}
			catch (TregexParseException)
			{
			}
			// yay, passed
			// can't name a variable under a negation
			try
			{
				TregexPattern pattern = TregexPattern.Compile("__ ! > __=a");
				throw new Exception("Expected a parse exception");
			}
			catch (TregexParseException)
			{
			}
		}

		// yay, passed
		[NUnit.Framework.Test]
		public virtual void TestHeadOfPhrase()
		{
			RunTest("NP <# NNS", "(NP (NN work) (NNS practices))", "(NP (NN work) (NNS practices))");
			RunTest("NP <# NN", "(NP (NN work) (NNS practices))");
			// should have no results
			RunTest("NP <<# NNS", "(NP (NP (NN work) (NNS practices)) (PP (IN in) (NP (DT the) (JJ former) (NNP Soviet) (NNP Union))))", "(NP (NP (NN work) (NNS practices)) (PP (IN in) (NP (DT the) (JJ former) (NNP Soviet) (NNP Union))))", "(NP (NN work) (NNS practices))"
				);
			RunTest("NP !<# NNS <<# NNS", "(NP (NP (NN work) (NNS practices)) (PP (IN in) (NP (DT the) (JJ former) (NNP Soviet) (NNP Union))))", "(NP (NP (NN work) (NNS practices)) (PP (IN in) (NP (DT the) (JJ former) (NNP Soviet) (NNP Union))))");
			RunTest("NP !<# NNP <<# NNP", "(NP (NP (NN work) (NNS practices)) (PP (IN in) (NP (DT the) (JJ former) (NNP Soviet) (NNP Union))))");
			// no results
			RunTest("NNS ># NP", "(NP (NP (NN work) (NNS practices)) (PP (IN in) (NP (DT the) (JJ former) (NNP Soviet) (NNP Union))))", "(NNS practices)");
			RunTest("NNS ># (NP < PP)", "(NP (NP (NN work) (NNS practices)) (PP (IN in) (NP (DT the) (JJ former) (NNP Soviet) (NNP Union))))");
			// no results
			RunTest("NNS >># (NP < PP)", "(NP (NP (NN work) (NNS practices)) (PP (IN in) (NP (DT the) (JJ former) (NNP Soviet) (NNP Union))))", "(NNS practices)");
			RunTest("NP <<# /^NN/", "(NP (NP (NN work) (NNS practices)) (PP (IN in) (NP (DT the) (JJ former) (NNP Soviet) (NNP Union))))", "(NP (NP (NN work) (NNS practices)) (PP (IN in) (NP (DT the) (JJ former) (NNP Soviet) (NNP Union))))", "(NP (NN work) (NNS practices))"
				, "(NP (DT the) (JJ former) (NNP Soviet) (NNP Union)))");
		}

		[NUnit.Framework.Test]
		public virtual void TestOnlyMatchRoot()
		{
			string treeString = "(a (foo 1) (bar 2))";
			Tree tree = TreeFromString(treeString);
			TregexPattern pattern = TregexPattern.Compile("__=a ! > __");
			TregexMatcher matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(treeString, matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual(treeString, matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestRepeatedVariables()
		{
			Tree tree = TreeFromString("(root (a (foo 1)) (a (bar 2)))");
			TregexPattern pattern = TregexPattern.Compile("a < foo=a | < bar=a");
			TregexMatcher matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(a (foo 1))", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(foo 1)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(a (bar 2))", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(bar 2)", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		/// <summary>A test case provided by a user which leverages variable names.</summary>
		/// <remarks>
		/// A test case provided by a user which leverages variable names.
		/// Goal is to match this tree: <br />
		/// (T
		/// (X
		/// (N
		/// (N Moe
		/// (PNT ,))))
		/// (NP
		/// (X
		/// (N Curly))
		/// (NP
		/// (CONJ and)
		/// (X
		/// (N Larry)))))
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestMoeCurlyLarry()
		{
			string testString = ("(T (X (N (N Moe (PNT ,)))) (NP (X (N Curly)) " + "(NP (CONJ and) (X (N Larry)))))");
			Tree tree = TreeFromString(testString);
			TregexPattern pattern = TregexPattern.Compile("PNT=p >>- (__=l >, (__=t <- (__=r <, __=m <- (__ <, CONJ <- __=z))))");
			TregexMatcher matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(PNT ,)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(PNT ,)", matcher.GetNode("p").ToString());
			NUnit.Framework.Assert.AreEqual("(X (N (N Moe (PNT ,))))", matcher.GetNode("l").ToString());
			NUnit.Framework.Assert.AreEqual(testString, matcher.GetNode("t").ToString());
			NUnit.Framework.Assert.AreEqual("(NP (X (N Curly)) (NP (CONJ and) (X (N Larry))))", matcher.GetNode("r").ToString());
			NUnit.Framework.Assert.AreEqual("(X (N Curly))", matcher.GetNode("m").ToString());
			NUnit.Framework.Assert.AreEqual("(X (N Larry))", matcher.GetNode("z").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = TregexPattern.Compile("PNT=p >>- (/(.+)/#1%var=l >, (__=t <- (__=r <, /(.+)/#1%var=m <- (__ <, CONJ <- /(.+)/#1%var=z))))");
			matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(PNT ,)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(PNT ,)", matcher.GetNode("p").ToString());
			NUnit.Framework.Assert.AreEqual("(X (N (N Moe (PNT ,))))", matcher.GetNode("l").ToString());
			NUnit.Framework.Assert.AreEqual(testString, matcher.GetNode("t").ToString());
			NUnit.Framework.Assert.AreEqual("(NP (X (N Curly)) (NP (CONJ and) (X (N Larry))))", matcher.GetNode("r").ToString());
			NUnit.Framework.Assert.AreEqual("(X (N Curly))", matcher.GetNode("m").ToString());
			NUnit.Framework.Assert.AreEqual("(X (N Larry))", matcher.GetNode("z").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = TregexPattern.Compile("PNT=p >>- (__=l >, (__=t <- (__=r <, ~l <- (__ <, CONJ <- ~l))))");
			matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(PNT ,)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(PNT ,)", matcher.GetNode("p").ToString());
			NUnit.Framework.Assert.AreEqual("(X (N (N Moe (PNT ,))))", matcher.GetNode("l").ToString());
			NUnit.Framework.Assert.AreEqual(testString, matcher.GetNode("t").ToString());
			NUnit.Framework.Assert.AreEqual("(NP (X (N Curly)) (NP (CONJ and) (X (N Larry))))", matcher.GetNode("r").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = TregexPattern.Compile("PNT=p >>- (__=l >, (__=t <- (__=r <, ~l=m <- (__ <, CONJ <- ~l=z))))");
			matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(PNT ,)", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(PNT ,)", matcher.GetNode("p").ToString());
			NUnit.Framework.Assert.AreEqual("(X (N (N Moe (PNT ,))))", matcher.GetNode("l").ToString());
			NUnit.Framework.Assert.AreEqual(testString, matcher.GetNode("t").ToString());
			NUnit.Framework.Assert.AreEqual("(NP (X (N Curly)) (NP (CONJ and) (X (N Larry))))", matcher.GetNode("r").ToString());
			NUnit.Framework.Assert.AreEqual("(X (N Curly))", matcher.GetNode("m").ToString());
			NUnit.Framework.Assert.AreEqual("(X (N Larry))", matcher.GetNode("z").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		/// <summary>
		/// Test a pattern with chinese characters in it, just to make sure
		/// that also works
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestChinese()
		{
			TregexPattern pattern = TregexPattern.Compile("DEG|DEC < 的");
			RunTest("DEG|DEC < 的", "(DEG (的 1))", "(DEG (的 1))");
		}

		/// <summary>
		/// Add a few more tests for immediate sister to make sure that $+
		/// doesn't accidentally match things that aren't non-immediate
		/// sisters, which should only be matched by $++
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestImmediateSister()
		{
			RunTest("@NP < (/^,/=comma $+ CC)", "((NP NP , NP , NP , CC NP))", "(NP NP , NP , NP , CC NP)");
			RunTest("@NP < (/^,/=comma $++ CC)", "((NP NP , NP , NP , CC NP))", "(NP NP , NP , NP , CC NP)", "(NP NP , NP , NP , CC NP)", "(NP NP , NP , NP , CC NP)");
			RunTest("@NP < (@/^,/=comma $+ @CC)", "((NP NP , NP , NP , CC NP))", "(NP NP , NP , NP , CC NP)");
			TregexPattern pattern = TregexPattern.Compile("@NP < (/^,/=comma $+ CC)");
			string treeString = "(NP NP (, 1) NP (, 2) NP (, 3) CC NP)";
			Tree tree = TreeFromString(treeString);
			TregexMatcher matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(treeString, matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("(, 3)", matcher.GetNode("comma").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			treeString = "(NP NP , NP , NP , CC NP)";
			tree = TreeFromString(treeString);
			matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(treeString, matcher.GetMatch().ToString());
			Tree node = matcher.GetNode("comma");
			NUnit.Framework.Assert.AreEqual(",", node.ToString());
			NUnit.Framework.Assert.AreSame(tree.Children()[5], node);
			NUnit.Framework.Assert.AreNotSame(tree.Children()[3], node);
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestVariableGroups()
		{
			string treeString = "(albatross (foo 1) (bar 2))";
			Tree tree = TreeFromString(treeString);
			TregexPattern pattern = TregexPattern.Compile("/(.*)/#1%name < foo");
			TregexMatcher matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(treeString, matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("albatross", matcher.GetVariableString("name"));
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = TregexPattern.Compile("/(.*)/#1%name < /foo(.*)/#1%blah");
			matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(treeString, matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("albatross", matcher.GetVariableString("name"));
			NUnit.Framework.Assert.AreEqual(string.Empty, matcher.GetVariableString("blah"));
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = TregexPattern.Compile("/(.*)/#1%name < (/(.*)/#1%blah < __)");
			matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(treeString, matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("albatross", matcher.GetVariableString("name"));
			NUnit.Framework.Assert.AreEqual("foo", matcher.GetVariableString("blah"));
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(treeString, matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("albatross", matcher.GetVariableString("name"));
			NUnit.Framework.Assert.AreEqual("bar", matcher.GetVariableString("blah"));
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			treeString = "(albatross (foo foo_albatross) (bar foo_albatross))";
			tree = TreeFromString(treeString);
			pattern = TregexPattern.Compile("/(.*)/#1%name < (/(.*)/#1%blah < /(.*)_(.*)/#2%name)");
			matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(treeString, matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("albatross", matcher.GetVariableString("name"));
			NUnit.Framework.Assert.AreEqual("foo", matcher.GetVariableString("blah"));
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(treeString, matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("albatross", matcher.GetVariableString("name"));
			NUnit.Framework.Assert.AreEqual("bar", matcher.GetVariableString("blah"));
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = TregexPattern.Compile("/(.*)/#1%name < (/(.*)/#1%blah < /(.*)_(.*)/#1%blah#2%name)");
			matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(treeString, matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("albatross", matcher.GetVariableString("name"));
			NUnit.Framework.Assert.AreEqual("foo", matcher.GetVariableString("blah"));
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		// TODO: there is a subtle bug in Tregex hinted at by the
		// construction of this test.  Suppose you have two regex patterns
		// such as /(.*)/#1%name and /(.*)/#1%blah in the pattern.  You
		// should then be able to write another regex
		// /(.*)(.*)/#1%blah#2%name and have it match the concatenation of
		// the two patterns.  However, this doesn't work, as the first two
		// characters of the node get matched regardless of what "blah"
		// and "name" are, resulting in the groups not matching.
		[NUnit.Framework.Test]
		public virtual void TestParenthesizedExpressions()
		{
			string[] treeStrings = new string[] { "( (S (S (PP (IN In) (NP (CD 1941) )) (, ,) (NP (NP (NNP Raeder) ) (CC and) (NP (DT the) (JJ German) (NN navy) )) (VP (VBD threatened) (S (VP (TO to) (VP (VB attack) (NP (DT the) (NNP Panama) (NNP Canal) )))))) (, ,) (RB so) (S (NP (PRP we) ) (VP (VBD created) (NP (NP (DT the) (NNP Southern) (NNP Command) ) (PP-LOC (IN in) (NP (NNP Panama) ))))) (. .) ))"
				, "(S (S (NP-SBJ (NNP Japan) ) (VP (MD can) (VP (VP (VB grow) ) (CC and) (VP (RB not) (VB cut) (PRT (RB back) ))))) (, ,) (CC and) (RB so) (S (ADVP (RB too) ) (, ,) (NP (NP (NNP New) (NNP Zealand) )) ))))", "( (S (S (NP-SBJ (PRP You) ) (VP (VBP make) (NP (DT a) (NN forecast) ))) (, ,) (CC and) (RB then) (S (NP-SBJ (PRP you) ) (VP (VBP become) (NP-PRD (PRP$ its) (NN prisoner) ))) (. .)))"
				 };
			Tree[] trees = TreesFromString(treeStrings);
			// First pattern: no parenthesized expressions.  All three trees should match once.
			TregexPattern pattern = TregexPattern.Compile("/^S/ < (/^S/ $++ (/^[,]|CC|CONJP$/ $+ (RB=adv $+ /^S/)))");
			TregexMatcher matcher = pattern.Matcher(trees[0]);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[1]);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[2]);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			// Second pattern: single relation in parentheses.  First tree should not match.
			pattern = TregexPattern.Compile("/^S/ < (/^S/ $++ (/^[,]|CC|CONJP$/ (< and) $+ (RB=adv $+ /^S/)))");
			matcher = pattern.Matcher(trees[0]);
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[1]);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[2]);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			// Third pattern: single relation in parentheses and negated.  Only first tree should match.
			pattern = TregexPattern.Compile("/^S/ < (/^S/ $++ (/^[,]|CC|CONJP$/ !(< and) $+ (RB=adv $+ /^S/)))");
			matcher = pattern.Matcher(trees[0]);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[1]);
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[2]);
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			// Fourth pattern: double relation in parentheses, no negation.
			pattern = TregexPattern.Compile("/^S/ < (/^S/ $++ (/^[,]|CC|CONJP$/ (< and $+ RB) $+ (RB=adv $+ /^S/)))");
			matcher = pattern.Matcher(trees[0]);
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[1]);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[2]);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			// Fifth pattern: double relation in parentheses, negated.
			pattern = TregexPattern.Compile("/^S/ < (/^S/ $++ (/^[,]|CC|CONJP$/ !(< and $+ RB) $+ (RB=adv $+ /^S/)))");
			matcher = pattern.Matcher(trees[0]);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[1]);
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[2]);
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			// Six pattern: double relation in parentheses, negated.  The only
			// tree with "and then" is the third one, so that is the one tree
			// that should not match.
			pattern = TregexPattern.Compile("/^S/ < (/^S/ $++ (/^[,]|CC|CONJP$/ !(< and $+ (RB < then)) $+ (RB=adv $+ /^S/)))");
			matcher = pattern.Matcher(trees[0]);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[1]);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			matcher = pattern.Matcher(trees[2]);
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		/// <summary>
		/// The PARENT_EQUALS relation allows for a simplification of what
		/// would have been a pair of rules in the dependencies.
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestParentEquals()
		{
			RunTest("A <= B", "(A (B 1))", "(A (B 1))");
			// Note that if the child node is the same as the parent node, a
			// double match is expected if there is nothing to eliminate it in
			// the expression
			RunTest("A <= A", "(A (A 1) (B 2))", "(A (A 1) (B 2))", "(A (A 1) (B 2))", "(A 1)");
			// This is the kind of expression where this relation can be useful
			RunTest("A <= (A < B)", "(A (A (B 1)))", "(A (A (B 1)))", "(A (B 1))");
			RunTest("A <= (A < B)", "(A (A (B 1)) (A (C 2)))", "(A (A (B 1)) (A (C 2)))", "(A (B 1))");
			RunTest("A <= (A < B)", "(A (A (C 2)))");
		}

		/// <summary>Test a few possible ways to make disjunctions at the root level.</summary>
		/// <remarks>
		/// Test a few possible ways to make disjunctions at the root level.
		/// Note that disjunctions at lower levels can always be created by
		/// repeating the relation, but that is not true at the root, since
		/// the root "relation" is implicit.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestRootDisjunction()
		{
			RunTest("A | B", "(A (B 1))", "(A (B 1))", "(B 1)");
			RunTest("(A) | (B)", "(A (B 1))", "(A (B 1))", "(B 1)");
			RunTest("A < B | A < C", "(A (B 1) (C 2))", "(A (B 1) (C 2))", "(A (B 1) (C 2))");
			RunTest("A < B | B < C", "(A (B 1) (C 2))", "(A (B 1) (C 2))");
			RunTest("A < B | B < C", "(A (B (C 1)) (C 2))", "(A (B (C 1)) (C 2))", "(B (C 1))");
			RunTest("A | B | C", "(A (B (C 1)) (C 2))", "(A (B (C 1)) (C 2))", "(B (C 1))", "(C 1)", "(C 2)");
			// The binding of the | should look like this:
			// A ( (< B) | (< C) )
			RunTest("A < B | < C", "(A (B 1))", "(A (B 1))");
			RunTest("A < B | < C", "(A (B 1) (C 2))", "(A (B 1) (C 2))", "(A (B 1) (C 2))");
			RunTest("A < B | < C", "(B (C 1))");
		}

		/// <summary>
		/// Tests the subtree pattern, <code>&lt;...</code>, which checks for
		/// an exact subtree under our current tree
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestSubtreePattern()
		{
			// test the obvious expected matches and several expected match failures
			RunTest("A <... { B ; C ; D }", "(A (B 1) (C 2) (D 3))", "(A (B 1) (C 2) (D 3))");
			RunTest("A <... { B ; C ; D }", "(Z (A (B 1) (C 2) (D 3)))", "(A (B 1) (C 2) (D 3))");
			RunTest("A <... { B ; C ; D }", "(A (B 1) (C 2) (D 3) (E 4))");
			RunTest("A <... { B ; C ; D }", "(A (E 4) (B 1) (C 2) (D 3))");
			RunTest("A <... { B ; C ; D }", "(A (B 1) (C 2) (E 4) (D 3))");
			RunTest("A <... { B ; C ; D }", "(A (B 1) (C 2))");
			// every test above should return the opposite when negated
			RunTest("A !<... { B ; C ; D }", "(A (B 1) (C 2) (D 3))");
			RunTest("A !<... { B ; C ; D }", "(Z (A (B 1) (C 2) (D 3)))");
			RunTest("A !<... { B ; C ; D }", "(A (B 1) (C 2) (D 3) (E 4))", "(A (B 1) (C 2) (D 3) (E 4))");
			RunTest("A !<... { B ; C ; D }", "(A (E 4) (B 1) (C 2) (D 3))", "(A (E 4) (B 1) (C 2) (D 3))");
			RunTest("A !<... { B ; C ; D }", "(A (B 1) (C 2) (E 4) (D 3))", "(A (B 1) (C 2) (E 4) (D 3))");
			RunTest("A !<... { B ; C ; D }", "(A (B 1) (C 2))", "(A (B 1) (C 2))");
			// test a couple various forms of nesting
			RunTest("A <... { (B < C) ; D }", "(A (B (C 2)) (D 3))", "(A (B (C 2)) (D 3))");
			RunTest("A <... { (B <... { C ; D }) ; E }", "(A (B (C 2) (D 3)) (E 4))", "(A (B (C 2) (D 3)) (E 4))");
			RunTest("A <... { (B !< C) ; D }", "(A (B (C 2)) (D 3))");
		}

		[NUnit.Framework.Test]
		public virtual void TestDisjunctionVariableAssignments()
		{
			Tree tree = TreeFromString("(NP (UCP (NNP U.S.) (CC and) (ADJP (JJ northern) (JJ European))) (NNS diplomats))");
			TregexPattern pattern = TregexPattern.Compile("UCP [ <- (ADJP=adjp < JJR) | <, NNP=np ]");
			TregexMatcher matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(NNP U.S.)", matcher.GetNode("np").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestOptional()
		{
			Tree tree = TreeFromString("(A (B (C 1)) (B 2))");
			TregexPattern pattern = TregexPattern.Compile("B ? < C=c");
			TregexMatcher matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(C 1)", matcher.GetNode("c").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(null, matcher.GetNode("c"));
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			tree = TreeFromString("(ROOT (INTJ (CC But) (S (NP (DT the) (NNP RTC)) (ADVP (RB also)) (VP (VBZ requires) (`` ``) (S (FRAG (VBG working) ('' '') (NP (NP (NN capital)) (S (VP (TO to) (VP (VB maintain) (SBAR (S (NP (NP (DT the) (JJ bad) (NNS assets)) (PP (IN of) (NP (NP (NNS thrifts)) (SBAR (WHNP (WDT that)) (S (VP (VBP are) (VBN sold) (, ,) (PP (IN until) (NP (DT the) (NNS assets))))))))) (VP (MD can) (VP (VB be) (VP (VBN sold) (ADVP (RB separately))))))))))))))) (S (VP (. .)))))"
				);
			// a pattern used to rearrange punctuation nodes in the srparser
			pattern = TregexPattern.Compile("__ !> __ <- (__=top <- (__ <<- (/[.]|PU/=punc < /[.!?。！？]/ ?> (__=single <: =punc))))");
			matcher = pattern.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(. .)", matcher.GetNode("punc").ToString());
			NUnit.Framework.Assert.AreEqual("(VP (. .))", matcher.GetNode("single").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		/// <summary>Stores an input and the expected output.</summary>
		/// <remarks>
		/// Stores an input and the expected output.  Obviously this is only
		/// expected to work with a given pattern, but this is a bit more
		/// convenient than calling the same pattern by hand over and over
		/// </remarks>
		public class TreeTestExample
		{
			internal Tree input;

			internal Tree[] expectedOutput;

			public TreeTestExample(string input, params string[] expectedOutput)
			{
				this.input = TreeFromString(input);
				this.expectedOutput = new Tree[expectedOutput.Length];
				for (int i = 0; i < expectedOutput.Length; ++i)
				{
					this.expectedOutput[i] = TreeFromString(expectedOutput[i]);
				}
			}

			public virtual void OutputResults(TregexPattern pattern)
			{
				System.Console.Out.WriteLine(pattern + " found the following matches on input " + input);
				TregexMatcher matcher = pattern.Matcher(input);
				bool output = false;
				while (matcher.Find())
				{
					output = true;
					System.Console.Out.WriteLine("  " + matcher.GetMatch());
					ICollection<string> namesToNodes = matcher.GetNodeNames();
					foreach (string name in namesToNodes)
					{
						System.Console.Out.WriteLine("    " + name + ": " + matcher.GetNode(name));
					}
				}
				if (!output)
				{
					System.Console.Out.WriteLine("  Nothing!  Absolutely nothing!");
				}
			}

			public virtual void RunTest(TregexPattern pattern)
			{
				IdentityHashMap<Tree, object> matchedTrees = new IdentityHashMap<Tree, object>();
				TregexMatcher matcher = pattern.Matcher(input);
				for (int i = 0; i < expectedOutput.Length; ++i)
				{
					try
					{
						NUnit.Framework.Assert.IsTrue(matcher.Find());
					}
					catch (AssertionFailedError e)
					{
						throw new Exception("Pattern " + pattern + " failed on input " + input.ToString() + " [expected " + expectedOutput.Length + " results, got " + i + "]", e);
					}
					Tree match = matcher.GetMatch();
					string result = match.ToString();
					string expectedResult = expectedOutput[i].ToString();
					try
					{
						NUnit.Framework.Assert.AreEqual(expectedResult, result);
					}
					catch (AssertionFailedError e)
					{
						throw new Exception("Pattern " + pattern + " matched the wrong tree on input " + input.ToString() + " [expected " + expectedOutput[i] + " got " + matcher.GetMatch() + "]", e);
					}
					matchedTrees[match] = null;
				}
				try
				{
					NUnit.Framework.Assert.IsFalse(matcher.Find());
				}
				catch (AssertionFailedError e)
				{
					throw new Exception("Pattern " + pattern + " failed on input " + input.ToString() + " [expected " + expectedOutput.Length + " results, got more than that]", e);
				}
				foreach (Tree subtree in input)
				{
					if (matchedTrees.Contains(subtree))
					{
						NUnit.Framework.Assert.IsTrue(matcher.MatchesAt(subtree));
					}
					else
					{
						NUnit.Framework.Assert.IsFalse(matcher.MatchesAt(subtree));
					}
				}
			}
		}

		/// <summary>
		/// Check that running the Tregex pattern on the tree gives the
		/// results shown in results.
		/// </summary>
		public static void RunTest(string pattern, string tree, params string[] expectedResults)
		{
			RunTest(TregexPattern.Compile(pattern), tree, expectedResults);
		}

		public static void RunTest(TregexPattern pattern, string tree, params string[] expectedResults)
		{
			TregexTest.TreeTestExample test = new TregexTest.TreeTestExample(tree, expectedResults);
			test.RunTest(pattern);
		}

		/// <summary>
		/// runs a given pattern on many of the above test objects,
		/// outputting the results matched for test test case
		/// </summary>
		public static void OutputResults(TregexPattern pattern, params TregexTest.TreeTestExample[] tests)
		{
			foreach (TregexTest.TreeTestExample test in tests)
			{
				test.OutputResults(pattern);
			}
		}

		public static void OutputResults(string pattern, params string[] trees)
		{
			foreach (string tree in trees)
			{
				TregexTest.TreeTestExample test = new TregexTest.TreeTestExample(tree);
				test.OutputResults(TregexPattern.Compile(pattern));
			}
		}
	}
}
