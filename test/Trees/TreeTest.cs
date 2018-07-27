using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Test Tree.java</summary>
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TreeTest
	{
		/// <summary>
		/// Test that using an iterator() straight off a tree gives the same
		/// results as building a subTrees collection and then doing an
		/// iterator off of that.
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestTreeIterator()
		{
			Tree t = Tree.ValueOf("(ROOT (S (NP (DT The) (ADJP (RB very) (JJ proud)) (NN woman)) (VP (VBD yawned) (ADVP (RB loudly))) (. .)))");
			if (t == null)
			{
				Fail("testTreeIterator failed to construct tree");
			}
			ICollection<Tree> m1 = new HashSet<Tree>();
			ICollection<Tree> m2 = new HashSet<Tree>();
			// build iterator List
			foreach (Tree sub in t)
			{
				m1.Add(sub);
			}
			foreach (Tree sub_1 in t.SubTrees())
			{
				m2.Add(sub_1);
			}
			NUnit.Framework.Assert.AreEqual(m1, m2);
		}

		[NUnit.Framework.Test]
		public virtual void TestDeeperCopy()
		{
			Tree t1 = null;
			try
			{
				t1 = Tree.ValueOf("(ROOT (S (NP I) (VP ran)))");
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			if (t1 == null)
			{
				Fail("testDeeperCopy failed to construct tree");
			}
			Tree t2 = t1.DeepCopy();
			NUnit.Framework.Assert.AreEqual(t1, t2);
			// make sure trees are equal
			NUnit.Framework.Assert.IsTrue(t1 != t2);
			// make sure trees are not ==
			ILabel l1 = t1.FirstChild().FirstChild().FirstChild().Label();
			ILabel l2 = t2.FirstChild().FirstChild().FirstChild().Label();
			NUnit.Framework.Assert.AreEqual(l1, l2);
			// make sure labels are equal (redundant)
			NUnit.Framework.Assert.IsTrue(l1 != l2);
		}

		// make sure labels are not ==
		[NUnit.Framework.Test]
		public virtual void TestRemove()
		{
			Tree t = Tree.ValueOf("(ROOT (S (NP (DT The) (ADJP (RB very) (JJ proud)) (NN woman)) (VP (VBD yawned) (ADVP (RB loudly))) (. .)))");
			Tree kid = t.FirstChild();
			try
			{
				t.Remove(kid);
				Fail("Tree remove should be unimplemented.");
			}
			catch (Exception)
			{
			}
			// we're good
			try
			{
				t.Remove(kid);
				Fail("Tree removeAll should be unimplemented.");
			}
			catch (Exception)
			{
			}
			// we're good
			kid.RemoveChild(0);
			NUnit.Framework.Assert.AreEqual("(ROOT (S (VP (VBD yawned) (ADVP (RB loudly))) (. .)))", t.ToString());
			t.RemoveChild(0);
			NUnit.Framework.Assert.AreEqual("ROOT", t.ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestDominates()
		{
			Tree t = Tree.ValueOf("(A (B this) (C (D is) (E a) (F small)) (G test))");
			NUnit.Framework.Assert.IsFalse(t.Dominates(t));
			foreach (Tree child in t.Children())
			{
				NUnit.Framework.Assert.IsTrue(t.Dominates(child));
				NUnit.Framework.Assert.IsFalse(child.Dominates(t));
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestPennPrint()
		{
			// a Label with a null value should print as "" not null.
			Tree t = Tree.ValueOf("( (SBARQ (WHNP (WP What)) (SQ (VBP are) (NP (DT the) (NNP Valdez) (NNS Principles))) (. ?)))", new LabeledScoredTreeReaderFactory(new TreeNormalizer()));
			NUnit.Framework.Assert.IsNull("Root of tree should have null label if none in String", t.Label().Value());
			string separator = Runtime.GetProperty("line.separator");
			string answer = ("( (SBARQ" + separator + "    (WHNP (WP What))" + separator + "    (SQ (VBP are)" + separator + "      (NP (DT the) (NNP Valdez) (NNS Principles)))" + separator + "    (. ?)))" + separator);
			NUnit.Framework.Assert.AreEqual(answer, t.PennString());
		}
	}
}
