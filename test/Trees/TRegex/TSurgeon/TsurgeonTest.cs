using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>Tests a few random tsurgeon operations.</summary>
	/// <remarks>
	/// Tests a few random tsurgeon operations.
	/// TODO: needs more coverage.
	/// </remarks>
	/// <author>John Bauer</author>
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TsurgeonTest
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

		/// <summary>This was buggy in 2009 since the label started pointing to the node with ~n on it.</summary>
		[NUnit.Framework.Test]
		public virtual void TestBackReference()
		{
			TregexPattern tregex = TregexPattern.Compile("__ <1 B=n <2 ~n");
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel n X");
			RunTest(tregex, tsurgeon, "(A (B w) (B w))", "(A (X w) (B w))");
		}

		[NUnit.Framework.Test]
		public virtual void TestForeign()
		{
			TregexPattern tregex = TregexPattern.Compile("atentát=test");
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel test perform_atentát");
			RunTest(tregex, tsurgeon, "(foo atentát)", "(foo perform_atentát)");
		}

		[NUnit.Framework.Test]
		public virtual void TestAdjoin()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("adjoin (FOO (BAR@)) foo");
			TregexPattern tregex = TregexPattern.Compile("B=foo");
			RunTest(tregex, tsurgeon, "(A (B 1 2))", "(A (FOO (BAR 1 2)))");
			RunTest(tregex, tsurgeon, "(A (C 1 2))", "(A (C 1 2))");
			RunTest(tregex, tsurgeon, "(A (B (B 1 2)))", "(A (FOO (BAR (FOO (BAR 1 2)))))");
			Tree tree = TreeFromString("(A (B 1 2))");
			TregexMatcher matcher = tregex.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(B 1 2)", matcher.GetNode("foo").ToString());
			Tree updated = tsurgeon.Matcher().Evaluate(tree, matcher);
			NUnit.Framework.Assert.AreEqual("(A (FOO (BAR 1 2)))", updated.ToString());
			// TODO: do we want the tsurgeon to implicitly update the matched node?
			// System.err.println(matcher.getNode("foo"));
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestAdjoinH()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("adjoinH (FOO (BAR@)) foo");
			TregexPattern tregex = TregexPattern.Compile("B=foo !< BAR");
			RunTest(tregex, tsurgeon, "(A (B 1 2))", "(A (B (BAR 1 2)))");
			RunTest(tregex, tsurgeon, "(A (C 1 2))", "(A (C 1 2))");
			RunTest(tregex, tsurgeon, "(A (B (B 1 2)))", "(A (B (BAR (B (BAR 1 2)))))");
			Tree tree = TreeFromString("(A (B 1 2))");
			TregexMatcher matcher = tregex.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(B 1 2)", matcher.GetNode("foo").ToString());
			Tree updated = tsurgeon.Matcher().Evaluate(tree, matcher);
			NUnit.Framework.Assert.AreEqual("(A (B (BAR 1 2)))", updated.ToString());
			NUnit.Framework.Assert.AreEqual("(B (BAR 1 2))", matcher.GetNode("foo").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestAdjoinF()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("adjoinF (FOO (BAR@)) foo");
			TregexPattern tregex = TregexPattern.Compile("B=foo !> FOO");
			RunTest(tregex, tsurgeon, "(A (B 1 2))", "(A (FOO (B 1 2)))");
			RunTest(tregex, tsurgeon, "(A (C 1 2))", "(A (C 1 2))");
			RunTest(tregex, tsurgeon, "(A (B (B 1 2)))", "(A (FOO (B (FOO (B 1 2)))))");
			Tree tree = TreeFromString("(A (B 1 2))");
			TregexMatcher matcher = tregex.Matcher(tree);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("(B 1 2)", matcher.GetNode("foo").ToString());
			Tree updated = tsurgeon.Matcher().Evaluate(tree, matcher);
			NUnit.Framework.Assert.AreEqual("(A (FOO (B 1 2)))", updated.ToString());
			NUnit.Framework.Assert.AreEqual("(B 1 2)", matcher.GetNode("foo").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestAdjoinWithNamedNode()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoinF (D (E=target foot@)) bar] " + "[insert (G 1) $+ target]");
			TregexPattern tregex = TregexPattern.Compile("B=bar !>> D");
			RunTest(tregex, tsurgeon, "(A (B C))", "(A (D (G 1) (E (B C))))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoinF (D (E=target foot@)) bar] " + "[insert (G 1) >0 target]");
			tregex = TregexPattern.Compile("B=bar !>> D");
			RunTest(tregex, tsurgeon, "(A (B C))", "(A (D (E (G 1) (B C))))");
			// Named leaf
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoinF (D (E foot@) F=target) bar] " + "[insert (G 1) >0 target]");
			tregex = TregexPattern.Compile("B=bar !>> D");
			RunTest(tregex, tsurgeon, "(A (B C))", "(A (D (E (B C)) (F (G 1))))");
		}

		[NUnit.Framework.Test]
		public virtual void TestAuxiliaryTreeErrors()
		{
			TsurgeonPattern tsurgeon;
			try
			{
				tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("adjoin (FOO (BAR)) foo");
				throw new Exception("Should have failed for not having a foot");
			}
			catch (TsurgeonParseException)
			{
			}
			// yay
			try
			{
				tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("adjoin (FOO (BAR@) (BAZ@)) foo");
				throw new Exception("Should have failed for having two feet");
			}
			catch (TsurgeonParseException)
			{
			}
			// yay
			try
			{
				tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("adjoin (FOO@ (BAR)) foo");
				throw new Exception("Non-leaves cannot be foot nodes");
			}
			catch (TsurgeonParseException)
			{
			}
		}

		// yay
		[NUnit.Framework.Test]
		public virtual void TestCreateSubtrees()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree FOO left right");
			TregexPattern tregex = TregexPattern.Compile("A < B=left < C=right");
			// Verify when there are only two nodes
			RunTest(tregex, tsurgeon, "(A (B 1) (C 2))", "(A (FOO (B 1) (C 2)))");
			// We allow backwards nodes as well
			RunTest(tregex, tsurgeon, "(A (C 1) (B 2))", "(A (FOO (C 1) (B 2)))");
			// Check nodes in between
			RunTest(tregex, tsurgeon, "(A (B 1) (D 3) (C 2))", "(A (FOO (B 1) (D 3) (C 2)))");
			// Check nodes outside the span
			RunTest(tregex, tsurgeon, "(A (D 3) (B 1) (C 2))", "(A (D 3) (FOO (B 1) (C 2)))");
			RunTest(tregex, tsurgeon, "(A (B 1) (C 2) (D 3))", "(A (FOO (B 1) (C 2)) (D 3))");
			RunTest(tregex, tsurgeon, "(A (D 3) (B 1) (C 2) (E 4))", "(A (D 3) (FOO (B 1) (C 2)) (E 4))");
			// Check when the two endpoints are the same
			tregex = TregexPattern.Compile("A < B=left < B=right");
			RunTest(tregex, tsurgeon, "(A (B 1) (C 2))", "(A (FOO (B 1)) (C 2))");
			// Check double operation - should make two FOO nodes and then stop
			RunTest(tregex, tsurgeon, "(A (B 1) (B 2))", "(A (FOO (B 1)) (FOO (B 2)))");
			// Check when we only have one argument to createSubtree
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree FOO child");
			tregex = TregexPattern.Compile("A < B=child");
			RunTest(tregex, tsurgeon, "(A (B 1) (C 2))", "(A (FOO (B 1)) (C 2))");
			RunTest(tregex, tsurgeon, "(A (B 1) (B 2))", "(A (FOO (B 1)) (FOO (B 2)))");
			// Check that incorrectly formatted operations don't successfully parse
			try
			{
				tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree FOO");
				throw new AssertionError("Expected to fail parsing");
			}
			catch (TsurgeonParseException)
			{
			}
			// yay
			try
			{
				tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree FOO a b c");
				throw new AssertionError("Expected to fail parsing");
			}
			catch (TsurgeonParseException)
			{
			}
			// yay
			// Verify that it fails when the parents are different
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree FOO left right");
			tregex = TregexPattern.Compile("A << B=left << C=right");
			try
			{
				RunTest(tregex, tsurgeon, "(A (B 1) (D (C 2)))", "(A (B 1) (D (C 2)))");
				throw new AssertionError("Expected a runtime failure");
			}
			catch (TsurgeonRuntimeException)
			{
			}
		}

		// yay
		// Extended syntax for createSubtree: support arbitrary tree literals
		[NUnit.Framework.Test]
		public virtual void TestCreateSubtreesExtended()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree (F (G 1) H@ I) left right");
			TregexPattern tregex = TregexPattern.Compile("A < B=left < C=right");
			// Verify when there are only two nodes
			RunTest(tregex, tsurgeon, "(A (B 1) (C 2))", "(A (F (G 1) (H (B 1) (C 2)) I))");
			// We allow backwards nodes as well
			RunTest(tregex, tsurgeon, "(A (C 1) (B 2))", "(A (F (G 1) (H (C 1) (B 2)) I))");
			// Check nodes in between
			RunTest(tregex, tsurgeon, "(A (B 1) (D 3) (C 2))", "(A (F (G 1) (H (B 1) (D 3) (C 2)) I))");
			// Check nodes outside the span
			RunTest(tregex, tsurgeon, "(A (D 3) (B 1) (C 2))", "(A (D 3) (F (G 1) (H (B 1) (C 2)) I))");
			RunTest(tregex, tsurgeon, "(A (B 1) (C 2) (D 3))", "(A (F (G 1) (H (B 1) (C 2)) I) (D 3))");
			RunTest(tregex, tsurgeon, "(A (D 3) (B 1) (C 2) (E 4))", "(A (D 3) (F (G 1) (H (B 1) (C 2)) I) (E 4))");
			// Check when the two endpoints are the same
			tregex = TregexPattern.Compile("A < B=left < B=right");
			RunTest(tregex, tsurgeon, "(A (B 1) (C 2))", "(A (F (G 1) (H (B 1)) I) (C 2))");
			// Check double operation - should make two F nodes and then stop
			RunTest(tregex, tsurgeon, "(A (B 1) (B 2))", "(A (F (G 1) (H (B 1)) I) (F (G 1) (H (B 2)) I))");
			// Check when we only have one argument to createSubtree
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree (F (G 1) H@ I) child");
			tregex = TregexPattern.Compile("A < B=child");
			RunTest(tregex, tsurgeon, "(A (B 1) (C 2))", "(A (F (G 1) (H (B 1)) I) (C 2))");
			RunTest(tregex, tsurgeon, "(A (B 1) (B 2))", "(A (F (G 1) (H (B 1)) I) (F (G 1) (H (B 2)) I))");
			// Check that incorrectly formatted operations don't successfully parse
			try
			{
				tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree (F (G 1) H@ I)");
				throw new AssertionError("Expected to fail parsing");
			}
			catch (TsurgeonParseException)
			{
			}
			// yay
			try
			{
				tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree (F (G 1) H@ I) a b c");
				throw new AssertionError("Expected to fail parsing");
			}
			catch (TsurgeonParseException)
			{
			}
			// yay
			// Missing foot
			try
			{
				tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree (F (G 1) H I) a b c");
				throw new AssertionError("Expected to fail parsing");
			}
			catch (TsurgeonParseException)
			{
			}
			// yay
			// Verify that it fails when the parents are different
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree (F (G 1) H@ I) left right");
			tregex = TregexPattern.Compile("A << B=left << C=right");
			try
			{
				RunTest(tregex, tsurgeon, "(A (B 1) (D (C 2)))", "(A (B 1) (D (C 2)))");
				throw new AssertionError("Expected a runtime failure");
			}
			catch (TsurgeonRuntimeException)
			{
			}
		}

		// yay
		[NUnit.Framework.Test]
		public virtual void TestDelete()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("delete bob");
			TregexPattern tregex = TregexPattern.Compile("B=bob");
			RunTest(tregex, tsurgeon, "(A (B (C 1)))", "A");
			RunTest(tregex, tsurgeon, "(A (foo 1) (B (C 1)))", "(A (foo 1))");
			RunTest(tregex, tsurgeon, "(A (B 1) (B (C 1)))", "A");
			RunTest(tregex, tsurgeon, "(A (foo 1) (bar (C 1)))", "(A (foo 1) (bar (C 1)))");
			tregex = TregexPattern.Compile("C=bob");
			RunTest(tregex, tsurgeon, "(A (B (C 1)))", "(A B)");
			RunTest(tregex, tsurgeon, "(A (foo 1) (B (C 1)))", "(A (foo 1) B)");
			RunTest(tregex, tsurgeon, "(A (B 1) (B (C 1)))", "(A (B 1) B)");
			RunTest(tregex, tsurgeon, "(A (foo 1) (bar (C 1)))", "(A (foo 1) bar)");
		}

		[NUnit.Framework.Test]
		public virtual void TestPrune()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("prune bob");
			TregexPattern tregex = TregexPattern.Compile("B=bob");
			RunTest(tregex, tsurgeon, "(A (B (C 1)))", null);
			RunTest(tregex, tsurgeon, "(A (foo 1) (B (C 1)))", "(A (foo 1))");
			RunTest(tregex, tsurgeon, "(A (B 1) (B (C 1)))", null);
			RunTest(tregex, tsurgeon, "(A (foo 1) (bar (C 1)))", "(A (foo 1) (bar (C 1)))");
			tregex = TregexPattern.Compile("C=bob");
			RunTest(tregex, tsurgeon, "(A (B (C 1)))", null);
			RunTest(tregex, tsurgeon, "(A (foo 1) (B (C 1)))", "(A (foo 1))");
			RunTest(tregex, tsurgeon, "(A (B 1) (B (C 1)))", "(A (B 1))");
			RunTest(tregex, tsurgeon, "(A (foo 1) (bar (C 1)))", "(A (foo 1))");
		}

		[NUnit.Framework.Test]
		public virtual void TestInsert()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("insert (D (E 6)) $+ bar");
			TregexPattern tregex = TregexPattern.Compile("B=bar !$ D");
			RunTest(tregex, tsurgeon, "(A (B 0) (C 1))", "(A (D (E 6)) (B 0) (C 1))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("insert (D (E 6)) $- bar");
			RunTest(tregex, tsurgeon, "(A (B 0) (C 1))", "(A (B 0) (D (E 6)) (C 1))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("insert (D (E 6)) >0 bar");
			tregex = TregexPattern.Compile("B=bar !<D");
			RunTest(tregex, tsurgeon, "(A (B 0) (C 1))", "(A (B (D (E 6)) 0) (C 1))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("insert foo >0 bar");
			tregex = TregexPattern.Compile("B=bar !<C $C=foo");
			RunTest(tregex, tsurgeon, "(A (B 0) (C 1))", "(A (B (C 1) 0) (C 1))");
			// the name will be cut off
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("insert (D (E=blah 6)) >0 bar");
			tregex = TregexPattern.Compile("B=bar !<D");
			RunTest(tregex, tsurgeon, "(A (B 0) (C 1))", "(A (B (D (E 6)) 0) (C 1))");
			// the name should not be cut off, with the escaped = unescaped now
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("insert (D (E\\=blah 6)) >0 bar");
			tregex = TregexPattern.Compile("B=bar !<D");
			RunTest(tregex, tsurgeon, "(A (B 0) (C 1))", "(A (B (D (E=blah 6)) 0) (C 1))");
			// the name should be cut off again, with a \ at the end of the new node
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("insert (D (E\\\\=blah 6)) >0 bar");
			tregex = TregexPattern.Compile("B=bar !<D");
			RunTest(tregex, tsurgeon, "(A (B 0) (C 1))", "(A (B (D (E\\ 6)) 0) (C 1))");
		}

		[NUnit.Framework.Test]
		public virtual void TestInsertWithNamedNode()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[insert (D=target E) $+ bar] " + "[insert (F 1) >0 target]");
			TregexPattern tregex = TregexPattern.Compile("B=bar !$- D");
			RunTest(tregex, tsurgeon, "(A (B C))", "(A (D (F 1) E) (B C))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[insert (D=target E) $+ bar] " + "[insert (F 1) $+ target]");
			tregex = TregexPattern.Compile("B=bar !$- D");
			RunTest(tregex, tsurgeon, "(A (B C))", "(A (F 1) (D E) (B C))");
			// Named leaf
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[insert (D E=target) $+ bar] " + "[insert (F 1) $+ target]");
			tregex = TregexPattern.Compile("B=bar !$- D");
			RunTest(tregex, tsurgeon, "(A (B C))", "(A (D (F 1) E) (B C))");
		}

		[NUnit.Framework.Test]
		public virtual void TestRelabel()
		{
			TsurgeonPattern tsurgeon;
			TregexPattern tregex;
			tregex = TregexPattern.Compile("/^((?!_head).)*$/=preTerminal < (__=terminal !< __)");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel preTerminal /^(.*)$/$1_head=={terminal}/");
			RunTest(tregex, tsurgeon, "($ $)", "($_head=$ $)");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel foo blah");
			tregex = TregexPattern.Compile("B=foo");
			RunTest(tregex, tsurgeon, "(A (B 0) (C 1))", "(A (blah 0) (C 1))");
			RunTest(tregex, tsurgeon, "(A (B 0) (B 1))", "(A (blah 0) (blah 1))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel foo /\\//");
			tregex = TregexPattern.Compile("B=foo");
			RunTest(tregex, tsurgeon, "(A (B 0) (C 1))", "(A (/ 0) (C 1))");
			RunTest(tregex, tsurgeon, "(A (B 0) (B 1))", "(A (/ 0) (/ 1))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel foo /.*(voc.*)/$1/");
			tregex = TregexPattern.Compile("/^a.*t/=foo");
			RunTest(tregex, tsurgeon, "(A (avocet 0) (C 1))", "(A (vocet 0) (C 1))");
			RunTest(tregex, tsurgeon, "(A (avocet 0) (advocate 1))", "(A (vocet 0) (vocate 1))");
			tregex = TregexPattern.Compile("curlew=baz < /^a(.*)t/#1%bar=foo");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel baz /cu(rle)w/={foo}/");
			RunTest(tregex, tsurgeon, "(curlew (avocet 0))", "(avocet (avocet 0))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel baz /cu(rle)w/%{bar}/");
			RunTest(tregex, tsurgeon, "(curlew (avocet 0))", "(voce (avocet 0))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel baz /cu(rle)w/$1/");
			RunTest(tregex, tsurgeon, "(curlew (avocet 0))", "(rle (avocet 0))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel baz /cu(rle)w/$1={foo}/");
			RunTest(tregex, tsurgeon, "(curlew (avocet 0))", "(rleavocet (avocet 0))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel baz /cu(rle)w/%{bar}$1={foo}/");
			RunTest(tregex, tsurgeon, "(curlew (avocet 0))", "(vocerleavocet (avocet 0))");
			tregex = TregexPattern.Compile("A=baz < /curlew.*/=foo < /avocet.*/=bar");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel baz /^.*$/={foo}={bar}/");
			RunTest(tregex, tsurgeon, "(A (curlewfoo 0) (avocetzzz 1))", "(curlewfooavocetzzz (curlewfoo 0) (avocetzzz 1))");
			tregex = TregexPattern.Compile("A=baz < /curle.*/=foo < /avo(.*)/#1%bar");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel baz /^(.*)$/={foo}$1%{bar}/");
			RunTest(tregex, tsurgeon, "(A (curlew 0) (avocet 1))", "(curlewAcet (curlew 0) (avocet 1))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel baz /^(.*)$/=foo$1%bar/");
			RunTest(tregex, tsurgeon, "(A (curlew 0) (avocet 1))", "(=fooA%bar (curlew 0) (avocet 1))");
			tregex = TregexPattern.Compile("/foo/=foo");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel foo /foo/bar/");
			RunTest(tregex, tsurgeon, "(foofoo (curlew 0) (avocet 1))", "(barbar (curlew 0) (avocet 1))");
			tregex = TregexPattern.Compile("/foo/=foo < /cur.*/=bar");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel foo /foo/={bar}/");
			RunTest(tregex, tsurgeon, "(foofoo (curlew 0) (avocet 1))", "(curlewcurlew (curlew 0) (avocet 1))");
			tregex = TregexPattern.Compile("/^foo(.*)$/=foo");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel foo /foo(.*)$/bar$1/");
			RunTest(tregex, tsurgeon, "(foofoo (curlew 0) (avocet 1))", "(barfoo (curlew 0) (avocet 1))");
		}

		[NUnit.Framework.Test]
		public virtual void TestReplaceNode()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace foo blah");
			TregexPattern tregex = TregexPattern.Compile("B=foo : C=blah");
			RunTest(tregex, tsurgeon, "(A (B 0) (C 1))", "(A (C 1) (C 1))");
			// This test was a bug reported by a user; only one of the -NONE-
			// nodes was being replaced.  This was because the replace was
			// reusing existing tree nodes instead of creating new ones, which
			// caused tregex to fail to find the second replacement
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace dest src");
			tregex = TregexPattern.Compile("(/-([0-9]+)$/#1%i=src > /^FILLER$/) : (/^-NONE-/=dest <: /-([0-9]+)$/#1%i)");
			RunTest(tregex, tsurgeon, "( (S (FILLER (NP-SBJ-1 (NNP Koito))) (VP (VBZ has) (VP (VBN refused) (S (NP-SBJ (-NONE- *-1)) (VP (TO to) (VP (VB grant) (NP (NNP Mr.) (NNP Pickens)) (NP (NP (NNS seats)) (PP-LOC (IN on) (NP (PRP$ its) (NN board))))))) (, ,) (S-ADV (NP-SBJ (-NONE- *-1)) (VP (VBG asserting) (SBAR (-NONE- 0) (S (NP-SBJ (PRP he)) (VP (VBZ is) (NP-PRD (NP (DT a) (NN greenmailer)) (VP (VBG trying) (S (NP-SBJ (-NONE- *)) (VP (TO to) (VP (VB pressure) (NP (NP (NNP Koito) (POS 's)) (JJ other) (NNS shareholders)) (PP-CLR (IN into) (S-NOM (NP-SBJ (-NONE- *)) (VP (VBG buying) (NP (PRP him)) (PRT (RP out)) (PP-MNR (IN at) (NP (DT a) (NN profit)))))))))))))))))) (. .)))"
				, "( (S (FILLER (NP-SBJ-1 (NNP Koito))) (VP (VBZ has) (VP (VBN refused) (S (NP-SBJ (NP-SBJ-1 (NNP Koito))) (VP (TO to) (VP (VB grant) (NP (NNP Mr.) (NNP Pickens)) (NP (NP (NNS seats)) (PP-LOC (IN on) (NP (PRP$ its) (NN board))))))) (, ,) (S-ADV (NP-SBJ (NP-SBJ-1 (NNP Koito))) (VP (VBG asserting) (SBAR (-NONE- 0) (S (NP-SBJ (PRP he)) (VP (VBZ is) (NP-PRD (NP (DT a) (NN greenmailer)) (VP (VBG trying) (S (NP-SBJ (-NONE- *)) (VP (TO to) (VP (VB pressure) (NP (NP (NNP Koito) (POS 's)) (JJ other) (NNS shareholders)) (PP-CLR (IN into) (S-NOM (NP-SBJ (-NONE- *)) (VP (VBG buying) (NP (PRP him)) (PRT (RP out)) (PP-MNR (IN at) (NP (DT a) (NN profit)))))))))))))))))) (. .)))"
				);
		}

		[NUnit.Framework.Test]
		public virtual void TestReplaceTree()
		{
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace foo (BAR 1)");
			TregexPattern tregex = TregexPattern.Compile("B=foo");
			RunTest(tregex, tsurgeon, "(A (B 0) (B 1) (C 2))", "(A (BAR 1) (BAR 1) (C 2))");
			// test that a single replacement at the root is allowed
			RunTest(tregex, tsurgeon, "(B (C 1))", "(BAR 1)");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace foo (BAR 1) (BAZ 2)");
			RunTest(tregex, tsurgeon, "(A (B 0) (B 1) (C 2))", "(A (BAR 1) (BAZ 2) (BAR 1) (BAZ 2) (C 2))");
			try
			{
				RunTest(tregex, tsurgeon, "(B 0)", "(B 0)");
				throw new Exception("Expected a failure");
			}
			catch (TsurgeonRuntimeException)
			{
			}
			// good, we expected to fail if you try to replace the root node with two nodes
			// it is possible for numbers to work and words to not work if
			// the tsurgeon parser is not correct
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace foo (BAR blah)");
			tregex = TregexPattern.Compile("B=foo");
			RunTest(tregex, tsurgeon, "(A (B 0) (B 1) (C 2))", "(A (BAR blah) (BAR blah) (C 2))");
		}

		// public void testKeywords() {
		//   TsurgeonPattern tsurgeon = Tsurgeon.parseOperation("replace foo replace");
		// }
		/// <summary>Test (part of) an actual tree that we use in the Chinese transforming reader</summary>
		[NUnit.Framework.Test]
		public virtual void TestChineseReplaceTree()
		{
			string input = "(IP (IP (PP (P 像) (NP (NP (NR 赖斯) (PU ，) (NR 赖斯)) (NP (PN 本身)))) (PU 她｛) (NP (NN ｂｒｅａｔｈ)) (PU ｝) (IJ 呃) (VP (VV 担任) (NP (NN 国务卿)) (VP (ADVP (AD 比较)) (VP (VA 晚))))))";
			string expected = "(IP (IP (PP (P 像) (NP (NP (NR 赖斯) (PU ，) (NR 赖斯)) (NP (PN 本身)))) (PN 她) (PU ｛) (NP (NN ｂｒｅａｔｈ)) (PU ｝) (IJ 呃) (VP (VV 担任) (NP (NN 国务卿)) (VP (ADVP (AD 比较)) (VP (VA 晚))))))";
			TregexPattern tregex = TregexPattern.Compile("PU=punc < 她｛");
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace punc (PN 她) (PU ｛)");
			RunTest(tregex, tsurgeon, input, expected);
		}

		[NUnit.Framework.Test]
		public virtual void TestInsertDelete()
		{
			// The same bug as the Replace bug, but for a sequence of
			// insert/delete operations
			IList<Pair<TregexPattern, TsurgeonPattern>> surgery = new List<Pair<TregexPattern, TsurgeonPattern>>();
			TregexPattern tregex = TregexPattern.Compile("(/-([0-9]+)$/#1%i=src > /^FILLER$/) : (/^-NONE-/=dest <: /-([0-9]+)$/#1%i !$ ~src)");
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("insert src $+ dest");
			surgery.Add(new Pair<TregexPattern, TsurgeonPattern>(tregex, tsurgeon));
			tregex = TregexPattern.Compile("(/-([0-9]+)$/#1%i=src > /^FILLER$/) : (/^-NONE-/=dest <: /-([0-9]+)$/#1%i)");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("delete dest");
			surgery.Add(new Pair<TregexPattern, TsurgeonPattern>(tregex, tsurgeon));
			RunTest(surgery, "( (S (FILLER (NP-SBJ-1 (NNP Koito))) (VP (VBZ has) (VP (VBN refused) (S (NP-SBJ (-NONE- *-1)) (VP (TO to) (VP (VB grant) (NP (NNP Mr.) (NNP Pickens)) (NP (NP (NNS seats)) (PP-LOC (IN on) (NP (PRP$ its) (NN board))))))) (, ,) (S-ADV (NP-SBJ (-NONE- *-1)) (VP (VBG asserting) (SBAR (-NONE- 0) (S (NP-SBJ (PRP he)) (VP (VBZ is) (NP-PRD (NP (DT a) (NN greenmailer)) (VP (VBG trying) (S (NP-SBJ (-NONE- *)) (VP (TO to) (VP (VB pressure) (NP (NP (NNP Koito) (POS 's)) (JJ other) (NNS shareholders)) (PP-CLR (IN into) (S-NOM (NP-SBJ (-NONE- *)) (VP (VBG buying) (NP (PRP him)) (PRT (RP out)) (PP-MNR (IN at) (NP (DT a) (NN profit)))))))))))))))))) (. .)))"
				, "( (S (FILLER (NP-SBJ-1 (NNP Koito))) (VP (VBZ has) (VP (VBN refused) (S (NP-SBJ (NP-SBJ-1 (NNP Koito))) (VP (TO to) (VP (VB grant) (NP (NNP Mr.) (NNP Pickens)) (NP (NP (NNS seats)) (PP-LOC (IN on) (NP (PRP$ its) (NN board))))))) (, ,) (S-ADV (NP-SBJ (NP-SBJ-1 (NNP Koito))) (VP (VBG asserting) (SBAR (-NONE- 0) (S (NP-SBJ (PRP he)) (VP (VBZ is) (NP-PRD (NP (DT a) (NN greenmailer)) (VP (VBG trying) (S (NP-SBJ (-NONE- *)) (VP (TO to) (VP (VB pressure) (NP (NP (NNP Koito) (POS 's)) (JJ other) (NNS shareholders)) (PP-CLR (IN into) (S-NOM (NP-SBJ (-NONE- *)) (VP (VBG buying) (NP (PRP him)) (PRT (RP out)) (PP-MNR (IN at) (NP (DT a) (NN profit)))))))))))))))))) (. .)))"
				);
		}

		/// <summary>
		/// There was a bug where repeated children with the same exact
		/// structure meant that each of the children would be repeated, even
		/// if some of them wouldn't match the tree structure.
		/// </summary>
		/// <remarks>
		/// There was a bug where repeated children with the same exact
		/// structure meant that each of the children would be repeated, even
		/// if some of them wouldn't match the tree structure.  For example,
		/// if you had the tree <code>(NP NP , NP , NP , CC NP)</code> and
		/// tried to replace with <code>@NP &lt; (/^,/=comma $+ CC)</code>,
		/// all of the commas would be replaced, not just the one next to CC.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestReplaceWithRepeats()
		{
			TsurgeonPattern tsurgeon;
			TregexPattern tregex;
			tregex = TregexPattern.Compile("@NP < (/^,/=comma $+ CC)");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace comma (COMMA)");
			RunTest(tregex, tsurgeon, "(NP NP , NP , NP , CC NP)", "(NP NP , NP , NP COMMA CC NP)");
		}

		[NUnit.Framework.Test]
		public virtual void TestCoindex()
		{
			TregexPattern tregex = TregexPattern.Compile("A=foo << B=bar << C=baz");
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("coindex foo bar baz");
			RunTest(tregex, tsurgeon, "(A (B (C foo)))", "(A-1 (B-1 (C-1 foo)))");
			// note that the indexing does not happen a second time, since the labels are now changed
			RunTest(tregex, tsurgeon, "(A (B foo) (C foo) (C bar))", "(A-1 (B-1 foo) (C-1 foo) (C bar))");
			// Test that it indexes at 2 instead of 1
			RunTest(tregex, tsurgeon, "(A (B foo) (C-1 bar) (C baz))", "(A-2 (B-2 foo) (C-1 bar) (C-2 baz))");
		}

		/// <summary>
		/// Since tsurgeon uses a lot of keywords, those keywords would not
		/// be allowed in the operations unless you handle them correctly
		/// (for example, using lexical states).
		/// </summary>
		/// <remarks>
		/// Since tsurgeon uses a lot of keywords, those keywords would not
		/// be allowed in the operations unless you handle them correctly
		/// (for example, using lexical states).  This tests that this is
		/// done correctly.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestKeyword()
		{
			// This should successfully compile, assuming the keyword parsing is correct
			TregexPattern tregex = TregexPattern.Compile("A=foo << B=bar << C=baz");
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel foo relabel");
			RunTest(tregex, tsurgeon, "(A (B foo) (C foo) (C bar))", "(relabel (B foo) (C foo) (C bar))");
		}

		/// <summary>
		/// You can compile multiple patterns into one node with the syntax
		/// [pattern1] [pattern2]
		/// Test that it does what it is supposed to do
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestMultiplePatterns()
		{
			TregexPattern tregex = TregexPattern.Compile("A=foo < B=bar < C=baz");
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel baz BAZ] [move baz >-1 bar]");
			RunTest(tregex, tsurgeon, "(A (B foo) (C foo) (C bar))", "(A (B foo (BAZ foo) (BAZ bar)))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel baz /^.*$/={bar}={baz}FOO/] [move baz >-1 bar]");
			RunTest(tregex, tsurgeon, "(A (B foo) (C foo) (C bar))", "(A (B foo (BCFOO foo) (BCFOO bar)))");
			// This in particular was a problem until we required "/" to be escaped
			tregex = TregexPattern.Compile("A=foo < B=bar < C=baz < D=biff");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel baz /^.*$/={bar}={baz}/] [relabel biff /^.*$/={bar}={biff}/]");
			RunTest(tregex, tsurgeon, "(A (B foo) (C bar) (D baz))", "(A (B foo) (BC bar) (BD baz))");
		}

		[NUnit.Framework.Test]
		public virtual void TestIfExists()
		{
			// This should successfully compile, assuming the keyword parsing is correct
			TregexPattern tregex = TregexPattern.Compile("A=foo [ << B=bar | << C=baz ]");
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("if exists bar relabel bar BAR");
			RunTest(tregex, tsurgeon, "(A (B foo))", "(A (BAR foo))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[if exists bar relabel bar BAR] [if exists baz relabel baz BAZ]");
			RunTest(tregex, tsurgeon, "(A (B foo))", "(A (BAR foo))");
			RunTest(tregex, tsurgeon, "(A (C foo))", "(A (BAZ foo))");
			RunTest(tregex, tsurgeon, "(A (B foo) (C foo))", "(A (BAR foo) (BAZ foo))");
			string tree = new string("(ROOT (INTJ (CC But) (S (NP (DT the) (NNP RTC)) (ADVP (RB also)) (VP (VBZ requires) (`` ``) (S (FRAG (VBG working) ('' '') (NP (NP (NN capital)) (S (VP (TO to) (VP (VB maintain) (SBAR (S (NP (NP (DT the) (JJ bad) (NNS assets)) (PP (IN of) (NP (NP (NNS thrifts)) (SBAR (WHNP (WDT that)) (S (VP (VBP are) (VBN sold) (, ,) (PP (IN until) (NP (DT the) (NNS assets))))))))) (VP (MD can) (VP (VB be) (VP (VBN sold) (ADVP (RB separately))))))))))))))) (S (VP (. .)))))"
				);
			string expected = new string("(ROOT (INTJ (CC But) (S (NP (DT the) (NNP RTC)) (ADVP (RB also)) (VP (VBZ requires) (`` ``) (S (FRAG (VBG working) ('' '') (NP (NP (NN capital)) (S (VP (TO to) (VP (VB maintain) (SBAR (S (NP (NP (DT the) (JJ bad) (NNS assets)) (PP (IN of) (NP (NP (NNS thrifts)) (SBAR (WHNP (WDT that)) (S (VP (VBP are) (VBN sold) (, ,) (PP (IN until) (NP (DT the) (NNS assets))))))))) (VP (MD can) (VP (VB be) (VP (VBN sold) (ADVP (RB separately))))))))))))))) (. .)))"
				);
			tregex = TregexPattern.Compile("__ !> __ <- (__=top <- (__ <<- (/[.]|PU/=punc < /[.!?。！？]/ ?> (__=single <: =punc))))");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[move punc >-1 top] [if exists single prune single]");
			RunTest(tregex, tsurgeon, tree, expected);
		}

		[NUnit.Framework.Test]
		public virtual void TestExcise()
		{
			// TODO: needs more meat to this test
			TregexPattern tregex = TregexPattern.Compile("__=repeat <: (~repeat < __)");
			TsurgeonPattern tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("excise repeat repeat");
			RunTest(tregex, tsurgeon, "(A (B (B foo)))", "(A (B foo))");
			// Test that if a deleted root is excised down to a level that has
			// just one child, that one child gets returned as the new tree
			RunTest(tregex, tsurgeon, "(B (B foo))", "(B foo)");
			tregex = TregexPattern.Compile("A=root");
			tsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("excise root root");
			RunTest(tregex, tsurgeon, "(A (B bar) (C foo))", null);
		}

		public static void RunTest(TregexPattern tregex, TsurgeonPattern tsurgeon, string input, string expected)
		{
			Tree result = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(tregex, tsurgeon, TreeFromString(input));
			if (expected == null)
			{
				NUnit.Framework.Assert.AreEqual(null, result);
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(expected, result.ToString());
			}
			// run the test on both a list and as a single pattern just to
			// make sure the underlying code works for both
			Pair<TregexPattern, TsurgeonPattern> surgery = new Pair<TregexPattern, TsurgeonPattern>(tregex, tsurgeon);
			RunTest(Java.Util.Collections.SingletonList(surgery), input, expected);
		}

		public static void RunTest(IList<Pair<TregexPattern, TsurgeonPattern>> surgery, string input, string expected)
		{
			Tree result = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(surgery, TreeFromString(input));
			if (expected == null)
			{
				NUnit.Framework.Assert.AreEqual(null, result);
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(expected, result.ToString());
			}
		}

		public static void OutputResults(TregexPattern tregex, TsurgeonPattern tsurgeon, string input, string expected)
		{
			OutputResults(tregex, tsurgeon, input);
		}

		public static void OutputResults(TregexPattern tregex, TsurgeonPattern tsurgeon, string input)
		{
			System.Console.Out.WriteLine("Tsurgeon: " + tsurgeon);
			System.Console.Out.WriteLine("Tregex: " + tregex);
			TregexMatcher m = tregex.Matcher(TreeFromString(input));
			if (m.Find())
			{
				System.Console.Error.WriteLine(" Matched");
			}
			else
			{
				System.Console.Error.WriteLine(" Did not match");
			}
			Tree result = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(tregex, tsurgeon, TreeFromString(input));
			System.Console.Out.WriteLine(result);
		}
	}
}
