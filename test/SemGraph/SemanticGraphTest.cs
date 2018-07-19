using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph
{
	/// <author>David McClosky</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class SemanticGraphTest
	{
		private SemanticGraph graph;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			lock (typeof(SemanticGraphTest))
			{
				if (graph == null)
				{
					graph = MakeGraph();
				}
			}
		}

		private static SemanticGraph MakeGraph()
		{
			Tree tree;
			try
			{
				tree = new PennTreeReader(new StringReader("(S1 (S (S (S (NP (DT The) (NN CD14) (NN LPS) (NN receptor)) (VP (VBZ is) (, ,) (ADVP (RB however)) (, ,) (ADVP (RB up)) (VP (VBN regulated) (PRN (-LRB- -LRB-) (FRAG (RB not) (ADJP (RB down) (VBN regulated))) (-RRB- -RRB-)) (PP (IN in) (NP (JJ tolerant) (NNS cells)))))) (, ,) (CC and) (S (NP (NN LPS)) (VP (MD can) (, ,) (PP (IN in) (NP (NN fact))) (, ,) (ADVP (RB still)) (VP (VB lead) (PP (TO to) (NP (NP (NN activation)) (PP (IN of) (NP (JJ tolerant) (NNS cells))))) (SBAR (IN as) (S (VP (VBN evidenced) (PP (IN by) (NP (NP (NN mobilization)) (PP (IN of) (NP (DT the) (NN transcription) (NN factor) (NP (NP (JJ nuclear) (NN factor) (NN kappa) (NN B)) (PRN (-LRB- -LRB-) (NP (NN NF-kappa) (NN B)) (-RRB- -RRB-)))))))))))))) (. .)))"
					), new LabeledScoredTreeFactory()).ReadTree();
			}
			catch (IOException e)
			{
				// the tree should parse correctly
				throw new Exception(e);
			}
			return SemanticGraphFactory.MakeFromTree(tree, SemanticGraphFactory.Mode.Basic, GrammaticalStructure.Extras.Maximal);
		}

		[NUnit.Framework.Test]
		public virtual void TestShortestPath()
		{
			//graph.prettyPrint();
			IndexedWord word1 = graph.GetNodeByIndex(10);
			IndexedWord word2 = graph.GetNodeByIndex(14);
			// System.out.println("word1: " + word1);
			// System.out.println("word1: " + word1.hashCode());
			// System.out.println("word2: " + word2);
			// System.out.println("word2: " + word2.hashCode());
			// System.out.println("word eq: " + word1.equals(word2));
			// System.out.println("word eq: " + (word1.hashCode() == word2.hashCode()));
			// System.out.println("word eq: " + (word1.toString().equals(word2.toString())));
			IList<SemanticGraphEdge> edges = graph.GetShortestUndirectedPathEdges(word1, word2);
			// System.out.println("path: " + edges);
			NUnit.Framework.Assert.IsNotNull(edges);
			IList<IndexedWord> nodes = graph.GetShortestUndirectedPathNodes(word1, word2);
			// System.out.println("path: " + nodes);
			NUnit.Framework.Assert.IsNotNull(nodes);
			NUnit.Framework.Assert.AreEqual(word1, nodes[0]);
			NUnit.Framework.Assert.AreEqual(word2, nodes[nodes.Count - 1]);
			edges = graph.GetShortestUndirectedPathEdges(word1, word1);
			// System.out.println("path: " + edges);
			NUnit.Framework.Assert.IsNotNull(edges);
			NUnit.Framework.Assert.AreEqual(0, edges.Count);
			nodes = graph.GetShortestUndirectedPathNodes(word1, word1);
			// System.out.println("path: " + nodes);
			NUnit.Framework.Assert.IsNotNull(nodes);
			NUnit.Framework.Assert.AreEqual(1, nodes.Count);
			NUnit.Framework.Assert.AreEqual(word1, nodes[0]);
		}

		[NUnit.Framework.Test]
		public virtual void TestGetCommonAncestor()
		{
			IndexedWord common = graph.GetCommonAncestor(graph.GetNodeByIndex(43), graph.GetNodeByIndex(44));
			NUnit.Framework.Assert.AreEqual(45, common.Index());
			common = graph.GetCommonAncestor(graph.GetNodeByIndex(41), graph.GetNodeByIndex(39));
			NUnit.Framework.Assert.AreEqual(41, common.Index());
			common = graph.GetCommonAncestor(graph.GetNodeByIndex(39), graph.GetNodeByIndex(41));
			NUnit.Framework.Assert.AreEqual(41, common.Index());
			common = graph.GetCommonAncestor(graph.GetNodeByIndex(40), graph.GetNodeByIndex(42));
			NUnit.Framework.Assert.AreEqual(41, common.Index());
			// too far for this method
			common = graph.GetCommonAncestor(graph.GetNodeByIndex(10), graph.GetNodeByIndex(42));
			NUnit.Framework.Assert.AreEqual(null, common);
			common = graph.GetCommonAncestor(graph.GetNodeByIndex(10), graph.GetNodeByIndex(10));
			NUnit.Framework.Assert.AreEqual(10, common.Index());
			common = graph.GetCommonAncestor(graph.GetNodeByIndex(40), graph.GetNodeByIndex(40));
			NUnit.Framework.Assert.AreEqual(40, common.Index());
			// a couple tests at the top of the graph
			common = graph.GetCommonAncestor(graph.GetNodeByIndex(10), graph.GetNodeByIndex(1));
			NUnit.Framework.Assert.AreEqual(10, common.Index());
			common = graph.GetCommonAncestor(graph.GetNodeByIndex(1), graph.GetNodeByIndex(10));
			NUnit.Framework.Assert.AreEqual(10, common.Index());
		}

		[NUnit.Framework.Test]
		public virtual void TestCommonAncestor()
		{
			NUnit.Framework.Assert.AreEqual(1, graph.CommonAncestor(graph.GetNodeByIndex(43), graph.GetNodeByIndex(44)));
			NUnit.Framework.Assert.AreEqual(1, graph.CommonAncestor(graph.GetNodeByIndex(41), graph.GetNodeByIndex(39)));
			NUnit.Framework.Assert.AreEqual(1, graph.CommonAncestor(graph.GetNodeByIndex(39), graph.GetNodeByIndex(41)));
			NUnit.Framework.Assert.AreEqual(2, graph.CommonAncestor(graph.GetNodeByIndex(40), graph.GetNodeByIndex(42)));
			NUnit.Framework.Assert.AreEqual(2, graph.CommonAncestor(graph.GetNodeByIndex(42), graph.GetNodeByIndex(40)));
			// too far for this method
			NUnit.Framework.Assert.AreEqual(-1, graph.CommonAncestor(graph.GetNodeByIndex(10), graph.GetNodeByIndex(42)));
			// assertEquals(null, common);
			NUnit.Framework.Assert.AreEqual(0, graph.CommonAncestor(graph.GetNodeByIndex(10), graph.GetNodeByIndex(10)));
			NUnit.Framework.Assert.AreEqual(0, graph.CommonAncestor(graph.GetNodeByIndex(40), graph.GetNodeByIndex(40)));
			// assertEquals(40, common.index());
			// a couple tests at the top of the graph
			NUnit.Framework.Assert.AreEqual(2, graph.CommonAncestor(graph.GetNodeByIndex(10), graph.GetNodeByIndex(1)));
			NUnit.Framework.Assert.AreEqual(2, graph.CommonAncestor(graph.GetNodeByIndex(1), graph.GetNodeByIndex(10)));
		}

		[NUnit.Framework.Test]
		public virtual void TestTopologicalSort()
		{
			SemanticGraph gr = SemanticGraph.ValueOf("[ate subj>Bill dobj>[muffins compound>blueberry]]");
			VerifyTopologicalSort(gr);
			IList<IndexedWord> vertices = gr.VertexListSorted();
			gr.AddEdge(vertices[1], vertices[2], UniversalEnglishGrammaticalRelations.DirectObject, 1.0, false);
			VerifyTopologicalSort(gr);
			gr = SemanticGraph.ValueOf("[ate subj>Bill dobj>[muffins compound>blueberry]]");
			vertices = gr.VertexListSorted();
			gr.AddEdge(vertices[2], vertices[1], UniversalEnglishGrammaticalRelations.DirectObject, 1.0, false);
			VerifyTopologicalSort(gr);
			gr = SemanticGraph.ValueOf("[ate subj>Bill dobj>[muffins compound>blueberry]]");
			vertices = gr.VertexListSorted();
			gr.AddEdge(vertices[1], vertices[3], UniversalEnglishGrammaticalRelations.DirectObject, 1.0, false);
			VerifyTopologicalSort(gr);
			// now create a graph with a directed loop, which we should not
			// be able to topologically sort
			gr = SemanticGraph.ValueOf("[ate subj>Bill dobj>[muffins compound>blueberry]]");
			vertices = gr.VertexListSorted();
			gr.AddEdge(vertices[3], vertices[0], UniversalEnglishGrammaticalRelations.DirectObject, 1.0, false);
			try
			{
				VerifyTopologicalSort(gr);
				throw new Exception("Expected to fail");
			}
			catch (InvalidOperationException)
			{
			}
		}

		// yay, correctly caught error
		/// <summary>
		/// Tests that a particular topological sort is correct by verifying
		/// for each node that it appears in the sort and all of its children
		/// occur later in the sort
		/// </summary>
		private static void VerifyTopologicalSort(SemanticGraph graph)
		{
			IList<IndexedWord> sorted = graph.TopologicalSort();
			IDictionary<IndexedWord, int> indices = Generics.NewHashMap();
			for (int index = 0; index < sorted.Count; ++index)
			{
				indices[sorted[index]] = index;
			}
			foreach (IndexedWord parent in graph.VertexSet())
			{
				NUnit.Framework.Assert.IsTrue(indices.Contains(parent));
				int parentIndex = indices[parent];
				foreach (IndexedWord child in graph.GetChildren(parent))
				{
					NUnit.Framework.Assert.IsTrue(indices.Contains(child));
					int childIndex = indices[child];
					NUnit.Framework.Assert.IsTrue(parentIndex < childIndex);
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestGetPathToRoot()
		{
			VerifyPath(graph.GetPathToRoot(graph.GetNodeByIndex(1)), 4, 10);
			VerifyPath(graph.GetPathToRoot(graph.GetNodeByIndex(10)));
			// empty path
			VerifyPath(graph.GetPathToRoot(graph.GetNodeByIndex(34)), 35, 28, 10);
		}

		private static void VerifyPath(IList<IndexedWord> path, params int[] expected)
		{
			NUnit.Framework.Assert.AreEqual(expected.Length, path.Count);
			for (int i = 0; i < expected.Length; ++i)
			{
				NUnit.Framework.Assert.AreEqual(expected[i], path[i].Index());
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestGetSiblings()
		{
			VerifySet(graph.GetSiblings(graph.GetNodeByIndex(43)), 42, 44, 48);
			VerifySet(graph.GetSiblings(graph.GetNodeByIndex(10)));
			// empty set
			VerifySet(graph.GetSiblings(graph.GetNodeByIndex(42)), 43, 44, 48);
		}

		private static void VerifySet(ICollection<IndexedWord> nodes, params int[] expected)
		{
			ICollection<int> results = Generics.NewTreeSet();
			foreach (IndexedWord node in nodes)
			{
				results.Add(node.Index());
			}
			ICollection<int> expectedIndices = Generics.NewTreeSet();
			foreach (int index in expected)
			{
				expectedIndices.Add(index);
			}
			NUnit.Framework.Assert.AreEqual(expectedIndices, results);
		}

		[NUnit.Framework.Test]
		public virtual void TestIsAncestor()
		{
			//System.err.println(graph.toString(CoreLabel.VALUE_TAG_INDEX_FORMAT));
			NUnit.Framework.Assert.AreEqual(1, graph.IsAncestor(graph.GetNodeByIndex(42), graph.GetNodeByIndex(45)));
			NUnit.Framework.Assert.AreEqual(2, graph.IsAncestor(graph.GetNodeByIndex(40), graph.GetNodeByIndex(37)));
			NUnit.Framework.Assert.AreEqual(-1, graph.IsAncestor(graph.GetNodeByIndex(40), graph.GetNodeByIndex(38)));
			NUnit.Framework.Assert.AreEqual(-1, graph.IsAncestor(graph.GetNodeByIndex(40), graph.GetNodeByIndex(10)));
			NUnit.Framework.Assert.AreEqual(-1, graph.IsAncestor(graph.GetNodeByIndex(45), graph.GetNodeByIndex(42)));
		}

		[NUnit.Framework.Test]
		public virtual void TestHasChildren()
		{
			SemanticGraph gr = SemanticGraph.ValueOf("[ate subj>Bill dobj>[muffins compound>blueberry]]");
			IList<IndexedWord> vertices = gr.VertexListSorted();
			foreach (IndexedWord word in vertices)
			{
				if (word.Word().Equals("ate") || word.Word().Equals("muffins"))
				{
					NUnit.Framework.Assert.IsTrue(gr.HasChildren(word));
				}
				else
				{
					NUnit.Framework.Assert.IsFalse(gr.HasChildren(word));
				}
			}
		}
	}
}
