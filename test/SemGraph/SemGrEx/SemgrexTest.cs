using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using NUnit.Framework;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class SemgrexTest
	{
		[NUnit.Framework.Test]
		public virtual void TestMatchAll()
		{
			SemanticGraph graph = SemanticGraph.ValueOf("[ate subj>Bill dobj>[muffins compound>blueberry]]");
			ICollection<IndexedWord> words = graph.VertexSet();
			SemgrexPattern pattern = SemgrexPattern.Compile("{}");
			SemgrexMatcher matcher = pattern.Matcher(graph);
			string[] expectedMatches = new string[] { "ate", "Bill", "muffins", "blueberry" };
			for (int i = 0; i < expectedMatches.Length; ++i)
			{
				NUnit.Framework.Assert.IsTrue(matcher.FindNextMatchingNode());
			}
			NUnit.Framework.Assert.IsFalse(matcher.FindNextMatchingNode());
		}

		[NUnit.Framework.Test]
		public virtual void TestTest()
		{
			RunTest("{}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "Bill", "muffins", "blueberry");
			try
			{
				RunTest("{}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "Bill", "muffins", "foo");
				throw new Exception();
			}
			catch (AssertionFailedError)
			{
			}
			// yay
			try
			{
				RunTest("{}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "Bill", "muffins");
				throw new Exception();
			}
			catch (AssertionFailedError)
			{
			}
			// yay
			try
			{
				RunTest("{}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "Bill", "muffins", "blueberry", "blueberry");
				throw new Exception();
			}
			catch (AssertionFailedError)
			{
			}
		}

		// yay
		/// <summary>This also tests negated node matches</summary>
		[NUnit.Framework.Test]
		public virtual void TestWordMatch()
		{
			RunTest("{word:Bill}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill");
			RunTest("!{word:Bill}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "muffins", "blueberry");
			RunTest("!{word:Fred}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "Bill", "muffins", "blueberry");
			RunTest("!{word:ate}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill", "muffins", "blueberry");
			RunTest("{word:/^(?!Bill).*$/}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "muffins", "blueberry");
			RunTest("{word:/^(?!Fred).*$/}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "Bill", "muffins", "blueberry");
			RunTest("{word:/^(?!ate).*$/}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill", "muffins", "blueberry");
			RunTest("{word:muffins} >compound {word:blueberry}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "muffins");
			RunTest("{} << {word:ate}=a", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill", "muffins", "blueberry");
			RunTest("{} << !{word:ate}=a", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "blueberry");
			// blueberry should match twice because it has two ancestors
			RunTest("{} << {}=a", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill", "muffins", "blueberry", "blueberry");
		}

		[NUnit.Framework.Test]
		public virtual void TestSimpleDependency()
		{
			// blueberry has two ancestors
			RunTest("{} << {}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill", "muffins", "blueberry", "blueberry");
			// ate has three descendants
			RunTest("{} >> {}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "ate", "ate", "muffins");
			RunTest("{} < {}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill", "muffins", "blueberry");
			RunTest("{} > {}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "ate", "muffins");
		}

		[NUnit.Framework.Test]
		public virtual void TestNamedDependency()
		{
			RunTest("{} << {word:ate}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill", "muffins", "blueberry");
			RunTest("{} >> {word:blueberry}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "muffins");
			RunTest("{} >> {word:Bill}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate");
			RunTest("{} < {word:ate}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill", "muffins");
			RunTest("{} > {word:blueberry}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "muffins");
			RunTest("{} > {word:muffins}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate");
		}

		[NUnit.Framework.Test]
		public virtual void TestNamedGovernor()
		{
			RunTest("{word:blueberry} << {}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "blueberry");
			RunTest("{word:ate} << {}", "[ate subj>Bill dobj>[muffins compound>blueberry]]");
			RunTest("{word:blueberry} >> {}", "[ate subj>Bill dobj>[muffins compound>blueberry]]");
			RunTest("{word:muffins} >> {}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "muffins");
			RunTest("{word:Bill} >> {}", "[ate subj>Bill dobj>[muffins compound>blueberry]]");
			RunTest("{word:muffins} < {}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "muffins");
			RunTest("{word:muffins} > {}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "muffins");
		}

		[NUnit.Framework.Test]
		public virtual void TestTwoDependencies()
		{
			RunTest("{} >> ({} >> {})", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate");
			RunTest("{} >> {word:Bill} >> {word:muffins}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate");
			RunTest("{}=a >> {}=b >> {word:muffins}=c", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "ate", "ate");
			RunTest("{}=a >> {word:Bill}=b >> {}=c", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "ate", "ate");
			RunTest("{}=a >> {}=b >> {}=c", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "ate", "ate", "ate", "ate", "ate", "ate", "ate", "ate", "ate", "muffins");
		}

		[NUnit.Framework.Test]
		public virtual void TestRegex()
		{
			RunTest("{word:/Bill/}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill");
			RunTest("{word:/ill/}", "[ate subj>Bill dobj>[muffins compound>blueberry]]");
			RunTest("{word:/.*ill/}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill");
			RunTest("{word:/.*il/}", "[ate subj>Bill dobj>[muffins compound>blueberry]]");
			RunTest("{word:/.*il.*/}", "[ate subj>Bill dobj>[muffins compound>blueberry]]", "Bill");
		}

		[NUnit.Framework.Test]
		public virtual void TestReferencedRegex()
		{
			RunTest("{word:/Bill/}", "[ate subj>Bill dobj>[bill det>the]]", "Bill");
			RunTest("{word:/.*ill/}", "[ate subj>Bill dobj>[bill det>the]]", "Bill", "bill");
			RunTest("{word:/[Bb]ill/}", "[ate subj>Bill dobj>[bill det>the]]", "Bill", "bill");
		}

		// TODO: implement referencing regexes
		public static SemanticGraph MakeComplicatedGraph()
		{
			SemanticGraph graph = new SemanticGraph();
			string[] words = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
			IndexedWord[] nodes = new IndexedWord[words.Length];
			for (int i = 0; i < words.Length; ++i)
			{
				IndexedWord word = new IndexedWord("test", 1, i + 1);
				word.SetWord(words[i]);
				word.SetValue(words[i]);
				nodes[i] = word;
				graph.AddVertex(word);
			}
			graph.SetRoot(nodes[0]);
			// this graph isn't supposed to make sense
			graph.AddEdge(nodes[0], nodes[1], EnglishGrammaticalRelations.Modifier, 1.0, false);
			graph.AddEdge(nodes[0], nodes[2], EnglishGrammaticalRelations.DirectObject, 1.0, false);
			graph.AddEdge(nodes[0], nodes[3], EnglishGrammaticalRelations.IndirectObject, 1.0, false);
			graph.AddEdge(nodes[1], nodes[4], EnglishGrammaticalRelations.Marker, 1.0, false);
			graph.AddEdge(nodes[2], nodes[4], EnglishGrammaticalRelations.Expletive, 1.0, false);
			graph.AddEdge(nodes[3], nodes[4], EnglishGrammaticalRelations.AdjectivalComplement, 1.0, false);
			graph.AddEdge(nodes[4], nodes[5], EnglishGrammaticalRelations.AdjectivalModifier, 1.0, false);
			graph.AddEdge(nodes[4], nodes[6], EnglishGrammaticalRelations.AdverbialModifier, 1.0, false);
			graph.AddEdge(nodes[4], nodes[8], EnglishGrammaticalRelations.Modifier, 1.0, false);
			graph.AddEdge(nodes[5], nodes[7], EnglishGrammaticalRelations.PossessionModifier, 1.0, false);
			graph.AddEdge(nodes[6], nodes[7], EnglishGrammaticalRelations.PossessiveModifier, 1.0, false);
			graph.AddEdge(nodes[7], nodes[8], EnglishGrammaticalRelations.Agent, 1.0, false);
			graph.AddEdge(nodes[8], nodes[9], EnglishGrammaticalRelations.Determiner, 1.0, false);
			return graph;
		}

		/// <summary>
		/// Test that governors, dependents, ancestors, descendants are all
		/// returned with multiplicity 1 if there are multiple paths to the
		/// same node.
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestComplicatedGraph()
		{
			SemanticGraph graph = MakeComplicatedGraph();
			RunTest("{} < {word:A}", graph, "B", "C", "D");
			RunTest("{} > {word:E}", graph, "B", "C", "D");
			RunTest("{} > {word:J}", graph, "I");
			RunTest("{} < {word:E}", graph, "F", "G", "I");
			RunTest("{} < {word:I}", graph, "J");
			RunTest("{} << {word:A}", graph, "B", "C", "D", "E", "F", "G", "H", "I", "J");
			RunTest("{} << {word:B}", graph, "E", "F", "G", "H", "I", "J");
			RunTest("{} << {word:C}", graph, "E", "F", "G", "H", "I", "J");
			RunTest("{} << {word:D}", graph, "E", "F", "G", "H", "I", "J");
			RunTest("{} << {word:E}", graph, "F", "G", "H", "I", "J");
			RunTest("{} << {word:F}", graph, "H", "I", "J");
			RunTest("{} << {word:G}", graph, "H", "I", "J");
			RunTest("{} << {word:H}", graph, "I", "J");
			RunTest("{} << {word:I}", graph, "J");
			RunTest("{} << {word:J}", graph);
			RunTest("{} << {word:K}", graph);
			RunTest("{} >> {word:A}", graph);
			RunTest("{} >> {word:B}", graph, "A");
			RunTest("{} >> {word:C}", graph, "A");
			RunTest("{} >> {word:D}", graph, "A");
			RunTest("{} >> {word:E}", graph, "A", "B", "C", "D");
			RunTest("{} >> {word:F}", graph, "A", "B", "C", "D", "E");
			RunTest("{} >> {word:G}", graph, "A", "B", "C", "D", "E");
			RunTest("{} >> {word:H}", graph, "A", "B", "C", "D", "E", "F", "G");
			RunTest("{} >> {word:I}", graph, "A", "B", "C", "D", "E", "F", "G", "H");
			RunTest("{} >> {word:J}", graph, "A", "B", "C", "D", "E", "F", "G", "H", "I");
			RunTest("{} >> {word:K}", graph);
		}

		[NUnit.Framework.Test]
		public virtual void TestRelationType()
		{
			SemanticGraph graph = MakeComplicatedGraph();
			RunTest("{} <<mod {}", graph, "B", "E", "F", "G", "H", "I", "I", "J", "J");
			RunTest("{} >>det {}", graph, "A", "B", "C", "D", "E", "F", "G", "H", "I");
			RunTest("{} >>det {word:J}", graph, "A", "B", "C", "D", "E", "F", "G", "H", "I");
		}

		[NUnit.Framework.Test]
		public virtual void TestExactDepthRelations()
		{
			SemanticGraph graph = MakeComplicatedGraph();
			RunTest("{} 2,3<< {word:A}", graph, "E", "F", "G", "I");
			RunTest("{} 2,2<< {word:A}", graph, "E");
			RunTest("{} 1,2<< {word:A}", graph, "B", "C", "D", "E");
			RunTest("{} 0,2<< {word:A}", graph, "B", "C", "D", "E");
			RunTest("{} 0,10<< {word:A}", graph, "B", "C", "D", "E", "F", "G", "H", "I", "J");
			RunTest("{} 0,10>> {word:J}", graph, "A", "B", "C", "D", "E", "F", "G", "H", "I");
			RunTest("{} 2,3>> {word:J}", graph, "B", "C", "D", "E", "F", "G", "H");
			RunTest("{} 2,2>> {word:J}", graph, "E", "H");
			// use this method to avoid the toString() test, since we expect it
			// to use 2,2>> instead of 2>>
			RunTest(SemgrexPattern.Compile("{} 2>> {word:J}"), graph, "E", "H");
			RunTest("{} 1,2>> {word:J}", graph, "E", "H", "I");
		}

		/// <summary>Tests that if there are different paths from A to I, those paths show up for exactly the right depths</summary>
		[NUnit.Framework.Test]
		public virtual void TestMultipleDepths()
		{
			SemanticGraph graph = MakeComplicatedGraph();
			RunTest("{} 3,3<< {word:A}", graph, "F", "G", "I");
			RunTest("{} 4,4<< {word:A}", graph, "H", "J");
			RunTest("{} 5,5<< {word:A}", graph, "I");
			RunTest("{} 6,6<< {word:A}", graph, "J");
		}

		[NUnit.Framework.Test]
		public virtual void TestNamedNode()
		{
			SemanticGraph graph = MakeComplicatedGraph();
			RunTest("{} >dobj ({} >expl {})", graph, "A");
			SemgrexPattern pattern = SemgrexPattern.Compile("{} >dobj ({} >expl {}=foo)");
			SemgrexMatcher matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(1, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("E", matcher.GetNode("foo").ToString());
			NUnit.Framework.Assert.AreEqual("A", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = SemgrexPattern.Compile("{} >dobj ({} >expl {}=foo) >mod {}");
			matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(1, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("E", matcher.GetNode("foo").ToString());
			NUnit.Framework.Assert.AreEqual("A", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = SemgrexPattern.Compile("{} >dobj ({} >expl {}=foo) >mod ({} >mark {})");
			matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(1, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("E", matcher.GetNode("foo").ToString());
			NUnit.Framework.Assert.AreEqual("A", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = SemgrexPattern.Compile("{} >dobj ({} >expl {}=foo) >mod ({} > {})");
			matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(1, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("E", matcher.GetNode("foo").ToString());
			NUnit.Framework.Assert.AreEqual("A", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = SemgrexPattern.Compile("{} >dobj ({} >expl {}=foo) >mod ({} > {}=foo)");
			matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(1, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("E", matcher.GetNode("foo").ToString());
			NUnit.Framework.Assert.AreEqual("A", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = SemgrexPattern.Compile("{} >dobj ({} >expl {}=foo) >mod ({}=foo > {})");
			matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestPartition()
		{
			SemanticGraph graph = MakeComplicatedGraph();
			RunTest("{}=a >> {word:E}", graph, "A", "B", "C", "D");
			RunTest("{}=a >> {word:E} : {}=a >> {word:B}", graph, "A");
		}

		[NUnit.Framework.Test]
		public virtual void TestEqualsRelation()
		{
			SemanticGraph graph = SemanticGraph.ValueOf("[ate subj>Bill dobj>[muffins compound>blueberry]]");
			SemgrexPattern pattern = SemgrexPattern.Compile("{} >> ({}=a == {}=b)");
			SemgrexMatcher matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			// This split pattern should also work
			pattern = SemgrexPattern.Compile("{} >> {}=a >> {}=b : {}=a == {}=b");
			matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		/// <summary>
		/// In this test, the graph should find matches with pairs of nodes
		/// which are different from each other.
		/// </summary>
		/// <remarks>
		/// In this test, the graph should find matches with pairs of nodes
		/// which are different from each other.  Since "muffins" only has
		/// one dependent, there should not be any matches with "muffins" as
		/// the head, for example.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestNotEquals()
		{
			SemanticGraph graph = SemanticGraph.ValueOf("[ate subj>Bill dobj>[muffins compound>blueberry]]");
			SemgrexPattern pattern = SemgrexPattern.Compile("{} >> {}=a >> {}=b : {}=a !== {}=b");
			SemgrexMatcher matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			// same as the first test, essentially, but with a more compact expression
			pattern = SemgrexPattern.Compile("{} >> {}=a >> ({}=b !== {}=a)");
			matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("Bill", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(2, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		[NUnit.Framework.Test]
		public virtual void TestInitialConditions()
		{
			SemanticGraph graph = MakeComplicatedGraph();
			SemgrexPattern pattern = SemgrexPattern.Compile("{}=a >> {}=b : {}=a >> {}=c");
			IDictionary<string, IndexedWord> variables = new Dictionary<string, IndexedWord>();
			variables["b"] = graph.GetNodeByIndex(5);
			variables["c"] = graph.GetNodeByIndex(2);
			SemgrexMatcher matcher = pattern.Matcher(graph, variables);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual(3, matcher.GetNodeNames().Count);
			NUnit.Framework.Assert.AreEqual("A", matcher.GetNode("a").ToString());
			NUnit.Framework.Assert.AreEqual("E", matcher.GetNode("b").ToString());
			NUnit.Framework.Assert.AreEqual("B", matcher.GetNode("c").ToString());
			NUnit.Framework.Assert.AreEqual("A", matcher.GetMatch().ToString());
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		/// <summary>Test that a particular AnnotationLookup is honored</summary>
		[NUnit.Framework.Test]
		public virtual void TestIndex()
		{
			SemanticGraph graph = SemanticGraph.ValueOf("[ate subj>Bill dobj>[muffins compound>blueberry]]");
			RunTest("{idx:0}", graph, "ate");
			RunTest("{idx:1}", graph, "Bill");
			RunTest("{idx:2}", graph, "muffins");
			RunTest("{idx:3}", graph, "blueberry");
			RunTest("{idx:4}", graph);
		}

		[NUnit.Framework.Test]
		public virtual void TestLemma()
		{
			SemanticGraph graph = SemanticGraph.ValueOf("[ate subj>Bill dobj>[muffins compound>blueberry]]");
			foreach (IndexedWord word in graph.VertexSet())
			{
				word.SetLemma(word.Word());
			}
			RunTest("{lemma:ate}", graph, "ate");
			Tree tree = Tree.ValueOf("(ROOT (S (NP (PRP I)) (VP (VBP love) (NP (DT the) (NN display))) (. .)))");
			graph = SemanticGraphFactory.GenerateCCProcessedDependencies(tree);
			foreach (IndexedWord word_1 in graph.VertexSet())
			{
				word_1.SetLemma(word_1.Word());
			}
			// This set of three tests also provides some coverage for a
			// bizarre error a user found where multiple copies of the same
			// IndexedWord were created
			RunTest("{}=Obj <dobj {lemma:love}=Pred", graph, "display/NN");
			RunTest("{}=Obj <dobj {}=Pred", graph, "display/NN");
			RunTest("{lemma:love}=Pred >dobj {}=Obj ", graph, "love/VBP");
		}

		[NUnit.Framework.Test]
		public virtual void TestNamedRelation()
		{
			SemanticGraph graph = SemanticGraph.ValueOf("[ate subj>Bill dobj>[muffins compound>blueberry]]");
			SemgrexPattern pattern = SemgrexPattern.Compile("{idx:0}=gov >>=foo {idx:3}=dep");
			SemgrexMatcher matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetNode("gov").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("dep").ToString());
			NUnit.Framework.Assert.AreEqual("compound", matcher.GetRelnString("foo"));
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = SemgrexPattern.Compile("{idx:3}=dep <<=foo {idx:0}=gov");
			matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("ate", matcher.GetNode("gov").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("dep").ToString());
			NUnit.Framework.Assert.AreEqual("dobj", matcher.GetRelnString("foo"));
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = SemgrexPattern.Compile("{idx:3}=dep <=foo {idx:2}=gov");
			matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("gov").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("dep").ToString());
			NUnit.Framework.Assert.AreEqual("compound", matcher.GetRelnString("foo"));
			NUnit.Framework.Assert.IsFalse(matcher.Find());
			pattern = SemgrexPattern.Compile("{idx:2}=gov >=foo {idx:3}=dep");
			matcher = pattern.Matcher(graph);
			NUnit.Framework.Assert.IsTrue(matcher.Find());
			NUnit.Framework.Assert.AreEqual("muffins", matcher.GetNode("gov").ToString());
			NUnit.Framework.Assert.AreEqual("blueberry", matcher.GetNode("dep").ToString());
			NUnit.Framework.Assert.AreEqual("compound", matcher.GetRelnString("foo"));
			NUnit.Framework.Assert.IsFalse(matcher.Find());
		}

		public static void OutputResults(string pattern, string graph, params string[] ignored)
		{
			OutputResults(SemgrexPattern.Compile(pattern), SemanticGraph.ValueOf(graph));
		}

		public static void OutputResults(string pattern, SemanticGraph graph, params string[] ignored)
		{
			OutputResults(SemgrexPattern.Compile(pattern), graph);
		}

		public static void OutputResults(SemgrexPattern pattern, SemanticGraph graph, params string[] ignored)
		{
			System.Console.Out.WriteLine("Matching pattern " + pattern + " to\n" + graph + "  :" + (pattern.Matcher(graph).Matches() ? "matches" : "doesn't match"));
			System.Console.Out.WriteLine();
			pattern.PrettyPrint();
			System.Console.Out.WriteLine();
			SemgrexMatcher matcher = pattern.Matcher(graph);
			while (matcher.Find())
			{
				System.Console.Out.WriteLine("  " + matcher.GetMatch());
				ICollection<string> nodeNames = matcher.GetNodeNames();
				if (nodeNames != null && nodeNames.Count > 0)
				{
					foreach (string name in nodeNames)
					{
						System.Console.Out.WriteLine("    " + name + ": " + matcher.GetNode(name));
					}
				}
				ICollection<string> relNames = matcher.GetRelationNames();
				if (relNames != null)
				{
					foreach (string name in relNames)
					{
						System.Console.Out.WriteLine("    " + name + ": " + matcher.GetRelnString(name));
					}
				}
			}
		}

		public static void ComparePatternToString(string pattern)
		{
			SemgrexPattern semgrex = SemgrexPattern.Compile(pattern);
			string tostring = semgrex.ToString();
			tostring = tostring.ReplaceAll(" +", " ");
			NUnit.Framework.Assert.AreEqual(pattern.Trim(), tostring.Trim());
		}

		public static void RunTest(string pattern, string graph, params string[] expectedMatches)
		{
			ComparePatternToString(pattern);
			RunTest(SemgrexPattern.Compile(pattern), SemanticGraph.ValueOf(graph), expectedMatches);
		}

		public static void RunTest(string pattern, SemanticGraph graph, params string[] expectedMatches)
		{
			ComparePatternToString(pattern);
			RunTest(SemgrexPattern.Compile(pattern), graph, expectedMatches);
		}

		public static void RunTest(SemgrexPattern pattern, SemanticGraph graph, params string[] expectedMatches)
		{
			// results are not in the order I would expect.  Using a counter
			// allows them to be in any order
			IntCounter<string> counts = new IntCounter<string>();
			for (int i = 0; i < expectedMatches.Length; ++i)
			{
				counts.IncrementCount(expectedMatches[i]);
			}
			IntCounter<string> originalCounts = new IntCounter<string>(counts);
			SemgrexMatcher matcher = pattern.Matcher(graph);
			for (int i_1 = 0; i_1 < expectedMatches.Length; ++i_1)
			{
				if (!matcher.Find())
				{
					throw new AssertionFailedError("Expected " + expectedMatches.Length + " matches for pattern " + pattern + " on " + graph + ", only got " + i_1);
				}
				string match = matcher.GetMatch().ToString();
				if (!counts.ContainsKey(match))
				{
					throw new AssertionFailedError("Unexpected match " + match + " for pattern " + pattern + " on " + graph);
				}
				counts.DecrementCount(match);
				if (counts.GetCount(match) < 0)
				{
					throw new AssertionFailedError("Found too many matches for " + match + " for pattern " + pattern + " on " + graph);
				}
			}
			if (matcher.FindNextMatchingNode())
			{
				throw new AssertionFailedError("Found more than " + expectedMatches.Length + " matches for pattern " + pattern + " on " + graph + "... extra match is " + matcher.GetMatch());
			}
		}
	}
}
