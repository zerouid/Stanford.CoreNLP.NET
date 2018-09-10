using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Semgraph
{
	/// <summary>
	/// Generic utilities for dealing with Dependency graphs and other structures, useful for
	/// text simplification and rewriting.
	/// </summary>
	/// <remarks>
	/// Generic utilities for dealing with Dependency graphs and other structures, useful for
	/// text simplification and rewriting.
	/// TODO: Migrate some of the functions (that make sense) into SemanticGraph proper.
	/// BUT BEWARE: This class has methods that use jgraph (as opposed to jgrapht).
	/// We don't want our core code to become dependent on jgraph, so methods in
	/// SemanticGraph shouldn't call methods in this class, and methods that use
	/// jgraph shouldn't be moved into SemanticGraph.
	/// </remarks>
	/// <author>Eric Yeh</author>
	public class SemanticGraphUtils
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Semgraph.SemanticGraphUtils));

		private SemanticGraphUtils()
		{
		}

		/// <summary>
		/// Given a collection of nodes from srcGraph, generates a new
		/// SemanticGraph based off the subset represented by those nodes.
		/// </summary>
		/// <remarks>
		/// Given a collection of nodes from srcGraph, generates a new
		/// SemanticGraph based off the subset represented by those nodes.
		/// This uses the same vertices as in the original graph, which
		/// allows for equality and comparisons between the two graphs.
		/// </remarks>
		public static SemanticGraph MakeGraphFromNodes(ICollection<IndexedWord> nodes, SemanticGraph srcGraph)
		{
			if (nodes.Count == 1)
			{
				SemanticGraph retSg = new SemanticGraph();
				foreach (IndexedWord node in nodes)
				{
					retSg.AddVertex(node);
				}
				return retSg;
			}
			if (nodes.IsEmpty())
			{
				return null;
			}
			// TODO: if any nodes are not connected to edges in the original
			// graph, this will leave them out
			IList<SemanticGraphEdge> edges = new List<SemanticGraphEdge>();
			foreach (IndexedWord nodeG in nodes)
			{
				foreach (IndexedWord nodeD in nodes)
				{
					ICollection<SemanticGraphEdge> existingEdges = srcGraph.GetAllEdges(nodeG, nodeD);
					if (existingEdges != null)
					{
						Sharpen.Collections.AddAll(edges, existingEdges);
					}
				}
			}
			return SemanticGraphFactory.MakeFromEdges(edges);
		}

		//----------------------------------------------------------------------------------------
		//Query routines (obtaining sets of edges/vertices over predicates, etc)
		//----------------------------------------------------------------------------------------
		/// <summary>Finds the vertex in the given SemanticGraph that corresponds to the given node.</summary>
		/// <remarks>
		/// Finds the vertex in the given SemanticGraph that corresponds to the given node.
		/// Returns null if cannot find. Uses first match on index, sentIndex, and word values.
		/// </remarks>
		public static IndexedWord FindMatchingNode(IndexedWord node, SemanticGraph sg)
		{
			foreach (IndexedWord tgt in sg.VertexSet())
			{
				if ((tgt.Index() == node.Index()) && (tgt.SentIndex() == node.SentIndex()) && (tgt.Word().Equals(node.Word())))
				{
					return tgt;
				}
			}
			return null;
		}

		/// <summary>
		/// Given a starting vertice, grabs the subtree encapsulated by portion of the semantic graph, excluding
		/// a given edge.
		/// </summary>
		/// <remarks>
		/// Given a starting vertice, grabs the subtree encapsulated by portion of the semantic graph, excluding
		/// a given edge.  A tabu list is maintained, in order to deal with cyclical relations (such as between a
		/// rcmod (relative clause) and its nsubj).
		/// </remarks>
		public static ICollection<SemanticGraphEdge> GetSubTreeEdges(IndexedWord vertice, SemanticGraph sg, SemanticGraphEdge excludedEdge)
		{
			ICollection<SemanticGraphEdge> tabu = Generics.NewHashSet();
			tabu.Add(excludedEdge);
			GetSubTreeEdgesHelper(vertice, sg, tabu);
			tabu.Remove(excludedEdge);
			// Do not want this in the returned edges
			return tabu;
		}

		public static void GetSubTreeEdgesHelper(IndexedWord vertice, SemanticGraph sg, ICollection<SemanticGraphEdge> tabuEdges)
		{
			foreach (SemanticGraphEdge edge in sg.OutgoingEdgeIterable(vertice))
			{
				if (!tabuEdges.Contains(edge))
				{
					IndexedWord dep = edge.GetDependent();
					tabuEdges.Add(edge);
					GetSubTreeEdgesHelper(dep, sg, tabuEdges);
				}
			}
		}

		/// <summary>
		/// Given a set of nodes from a SemanticGraph, returns the set of
		/// edges that are spanned between these nodes.
		/// </summary>
		public static ICollection<SemanticGraphEdge> GetEdgesSpannedByVertices(ICollection<IndexedWord> nodes, SemanticGraph sg)
		{
			ICollection<SemanticGraphEdge> ret = Generics.NewHashSet();
			foreach (IndexedWord n1 in nodes)
			{
				foreach (IndexedWord n2 in nodes)
				{
					if (n1 != n2)
					{
						ICollection<SemanticGraphEdge> edges = sg.GetAllEdges(n1, n2);
						if (edges != null)
						{
							Sharpen.Collections.AddAll(ret, edges);
						}
					}
				}
			}
			return ret;
		}

		/// <summary>Returns a list of all children bearing a grammatical relation starting with the given string, relnPrefix</summary>
		public static IList<IndexedWord> GetChildrenWithRelnPrefix(SemanticGraph graph, IndexedWord vertex, string relnPrefix)
		{
			if (vertex.Equals(IndexedWord.NoWord))
			{
				return new List<IndexedWord>();
			}
			if (!graph.ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			IList<IndexedWord> childList = new List<IndexedWord>();
			foreach (SemanticGraphEdge edge in graph.OutgoingEdgeIterable(vertex))
			{
				if (edge.GetRelation().ToString().StartsWith(relnPrefix))
				{
					childList.Add(edge.GetTarget());
				}
			}
			return childList;
		}

		/// <summary>Returns a list of all children bearing a grammatical relation starting with the given set of relation prefixes</summary>
		public static IList<IndexedWord> GetChildrenWithRelnPrefix(SemanticGraph graph, IndexedWord vertex, ICollection<string> relnPrefixes)
		{
			if (vertex.Equals(IndexedWord.NoWord))
			{
				return new List<IndexedWord>();
			}
			if (!graph.ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			IList<IndexedWord> childList = new List<IndexedWord>();
			foreach (SemanticGraphEdge edge in graph.OutgoingEdgeIterable(vertex))
			{
				string edgeString = edge.GetRelation().ToString();
				foreach (string relnPrefix in relnPrefixes)
				{
					if (edgeString.StartsWith(relnPrefix))
					{
						childList.Add(edge.GetTarget());
						break;
					}
				}
			}
			return childList;
		}

		/// <summary>
		/// Since graphs can be have preps collapsed, finds all the immediate children of this node
		/// that are linked by a collapsed preposition edge.
		/// </summary>
		public static IList<IndexedWord> GetChildrenWithPrepC(SemanticGraph sg, IndexedWord vertex)
		{
			IList<IndexedWord> ret = new List<IndexedWord>();
			//  Collection<GrammaticalRelation> prepCs = EnglishGrammaticalRelations.getPrepsC();
			//  for (SemanticGraphEdge edge : sg.outgoingEdgesOf(vertex)) {
			//  if (prepCs.contains(edge.getRelation()))
			foreach (SemanticGraphEdge edge in sg.OutgoingEdgeIterable(vertex))
			{
				if (edge.GetRelation().ToString().StartsWith("prep"))
				{
					ret.Add(edge.GetDependent());
				}
			}
			return ret;
		}

		/// <summary>
		/// Returns the set of incoming edges for the given node that have the given
		/// relation.
		/// </summary>
		/// <remarks>
		/// Returns the set of incoming edges for the given node that have the given
		/// relation.
		/// Because certain edges may remain in string form (prepcs), check for both
		/// string and object form of relations.
		/// </remarks>
		public static IList<SemanticGraphEdge> IncomingEdgesWithReln(IndexedWord node, SemanticGraph sg, GrammaticalRelation reln)
		{
			return EdgesWithReln(sg.IncomingEdgeIterable(node), reln);
		}

		/// <summary>
		/// Checks for outgoing edges of the node, in the given graph, which contain
		/// the given relation.
		/// </summary>
		/// <remarks>
		/// Checks for outgoing edges of the node, in the given graph, which contain
		/// the given relation.  Relations are matched on if they are GrammaticalRelation
		/// objects or strings.
		/// </remarks>
		public static IList<SemanticGraphEdge> OutgoingEdgesWithReln(IndexedWord node, SemanticGraph sg, GrammaticalRelation reln)
		{
			return EdgesWithReln(sg.OutgoingEdgeIterable(node), reln);
		}

		/// <summary>
		/// Given a list of edges, returns those which match the given relation (can be string or
		/// GrammaticalRelation object).
		/// </summary>
		public static IList<SemanticGraphEdge> EdgesWithReln(IEnumerable<SemanticGraphEdge> edges, GrammaticalRelation reln)
		{
			IList<SemanticGraphEdge> found = Generics.NewArrayList();
			foreach (SemanticGraphEdge edge in edges)
			{
				GrammaticalRelation tgtReln = edge.GetRelation();
				if (tgtReln.Equals(reln))
				{
					found.Add(edge);
				}
			}
			return found;
		}

		/// <summary>
		/// Given a semantic graph, and a relation prefix, returns a list of all relations (edges)
		/// that start with the given prefix (e.g., prefix "prep" gives you all the prep relations: prep_by, pref_in,etc.)
		/// </summary>
		public static IList<SemanticGraphEdge> FindAllRelnsWithPrefix(SemanticGraph sg, string prefix)
		{
			List<SemanticGraphEdge> relns = new List<SemanticGraphEdge>();
			foreach (SemanticGraphEdge edge in sg.EdgeIterable())
			{
				GrammaticalRelation edgeRelation = edge.GetRelation();
				if (edgeRelation.ToString().StartsWith(prefix))
				{
					relns.Add(edge);
				}
			}
			return relns;
		}

		/// <summary>Finds the descendents of the given node in graph, avoiding the given set of nodes</summary>
		public static ICollection<IndexedWord> TabuDescendants(SemanticGraph sg, IndexedWord vertex, ICollection<IndexedWord> tabu)
		{
			if (!sg.ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			// Do a depth first search
			ICollection<IndexedWord> descendantSet = Generics.NewHashSet();
			TabuDescendantsHelper(sg, vertex, descendantSet, tabu, null, null);
			return descendantSet;
		}

		/// <summary>
		/// Finds the set of descendants for a node in the graph, avoiding the set of nodes and the
		/// set of edge relations.
		/// </summary>
		/// <remarks>
		/// Finds the set of descendants for a node in the graph, avoiding the set of nodes and the
		/// set of edge relations.  NOTE: these edges are encountered from the downward cull,
		/// from governor to dependent.
		/// </remarks>
		public static ICollection<IndexedWord> TabuDescendants(SemanticGraph sg, IndexedWord vertex, ICollection<IndexedWord> tabu, ICollection<GrammaticalRelation> tabuRelns)
		{
			if (!sg.ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			// Do a depth first search
			ICollection<IndexedWord> descendantSet = Generics.NewHashSet();
			TabuDescendantsHelper(sg, vertex, descendantSet, tabu, tabuRelns, null);
			return descendantSet;
		}

		public static ICollection<IndexedWord> DescendantsTabuRelns(SemanticGraph sg, IndexedWord vertex, ICollection<GrammaticalRelation> tabuRelns)
		{
			if (!sg.ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			// Do a depth first search
			ICollection<IndexedWord> descendantSet = Generics.NewHashSet();
			TabuDescendantsHelper(sg, vertex, descendantSet, Generics.NewHashSet<IndexedWord>(), tabuRelns, null);
			return descendantSet;
		}

		public static ICollection<IndexedWord> DescendantsTabuTestAndRelns(SemanticGraph sg, IndexedWord vertex, ICollection<GrammaticalRelation> tabuRelns, IndexedWordUnaryPred tabuTest)
		{
			if (!sg.ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			// Do a depth first search
			ICollection<IndexedWord> descendantSet = Generics.NewHashSet();
			TabuDescendantsHelper(sg, vertex, descendantSet, Generics.NewHashSet<IndexedWord>(), tabuRelns, tabuTest);
			return descendantSet;
		}

		public static ICollection<IndexedWord> DescendantsTabuTestAndRelns(SemanticGraph sg, IndexedWord vertex, ICollection<IndexedWord> tabuNodes, ICollection<GrammaticalRelation> tabuRelns, IndexedWordUnaryPred tabuTest)
		{
			if (!sg.ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			// Do a depth first search
			ICollection<IndexedWord> descendantSet = Generics.NewHashSet();
			TabuDescendantsHelper(sg, vertex, descendantSet, tabuNodes, tabuRelns, tabuTest);
			return descendantSet;
		}

		/// <summary>
		/// Performs a cull for the descendents of the given node in the
		/// graph, subject to the tabu nodes to avoid, relations to avoid
		/// crawling over, and child nodes to avoid traversing to based upon
		/// a predicate test.
		/// </summary>
		private static void TabuDescendantsHelper(SemanticGraph sg, IndexedWord curr, ICollection<IndexedWord> descendantSet, ICollection<IndexedWord> tabu, ICollection<GrammaticalRelation> relnsToAvoid, IndexedWordUnaryPred tabuTest)
		{
			if (tabu.Contains(curr))
			{
				return;
			}
			if (descendantSet.Contains(curr))
			{
				return;
			}
			descendantSet.Add(curr);
			foreach (IndexedWord child in sg.GetChildren(curr))
			{
				foreach (SemanticGraphEdge edge in sg.GetAllEdges(curr, child))
				{
					if (relnsToAvoid != null && relnsToAvoid.Contains(edge.GetRelation()))
					{
						continue;
					}
					if (tabuTest != null && tabuTest.Test(edge.GetDependent(), sg))
					{
						continue;
					}
					TabuDescendantsHelper(sg, child, descendantSet, tabu, relnsToAvoid, tabuTest);
				}
			}
		}

		//------------------------------------------------------------------------------------
		//"Constituent" extraction and manipulation
		//------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the vertice that is "leftmost."  Note this requires that the IndexedFeatureLabels present actually have
		/// ordering information.
		/// </summary>
		/// <remarks>
		/// Returns the vertice that is "leftmost."  Note this requires that the IndexedFeatureLabels present actually have
		/// ordering information.
		/// TODO: can be done more efficiently?
		/// </remarks>
		public static IndexedWord LeftMostChildVertice(IndexedWord startNode, SemanticGraph sg)
		{
			TreeSet<IndexedWord> vertices = new TreeSet<IndexedWord>();
			foreach (IndexedWord vertex in sg.Descendants(startNode))
			{
				vertices.Add(vertex);
			}
			return vertices.First();
		}

		/// <summary>
		/// Returns the vertices that are "leftmost, rightmost"  Note this requires that the IndexedFeatureLabels present actually have
		/// ordering information.
		/// </summary>
		/// <remarks>
		/// Returns the vertices that are "leftmost, rightmost"  Note this requires that the IndexedFeatureLabels present actually have
		/// ordering information.
		/// TODO: can be done more efficiently?
		/// </remarks>
		public static Pair<IndexedWord, IndexedWord> LeftRightMostChildVertices(IndexedWord startNode, SemanticGraph sg)
		{
			TreeSet<IndexedWord> vertices = new TreeSet<IndexedWord>();
			foreach (IndexedWord vertex in sg.Descendants(startNode))
			{
				vertices.Add(vertex);
			}
			return Pair.MakePair(vertices.First(), vertices.Last());
		}

		/// <summary>
		/// Given a SemanticGraph, and a set of nodes, finds the "blanket" of nodes that are one
		/// edge away from the set of nodes passed in.
		/// </summary>
		/// <remarks>
		/// Given a SemanticGraph, and a set of nodes, finds the "blanket" of nodes that are one
		/// edge away from the set of nodes passed in.  This is similar to the idea of a Markov
		/// Blanket, except in the context of a SemanticGraph.
		/// TODO: optimize
		/// </remarks>
		public static ICollection<IndexedWord> GetDependencyBlanket(SemanticGraph sg, ICollection<IndexedWord> assertedNodes)
		{
			ICollection<IndexedWord> retSet = Generics.NewHashSet();
			foreach (IndexedWord curr in sg.VertexSet())
			{
				if (!assertedNodes.Contains(curr) && !retSet.Contains(curr))
				{
					foreach (IndexedWord assertedNode in assertedNodes)
					{
						if (sg.ContainsEdge(assertedNode, curr) || sg.ContainsEdge(curr, assertedNode))
						{
							retSet.Add(curr);
						}
					}
				}
			}
			return retSet;
		}

		/// <summary>
		/// Resets the indices for the vertices in the graph, using the current
		/// ordering returned by vertexList (presumably in order).
		/// </summary>
		/// <remarks>
		/// Resets the indices for the vertices in the graph, using the current
		/// ordering returned by vertexList (presumably in order).  This is to ensure
		/// accesses to the InfoFile word table do not fall off after a SemanticGraph has
		/// been edited.
		/// <br />
		/// NOTE: the vertices will be replaced, as JGraphT does not permit
		/// in-place modification of the nodes.  (TODO: we no longer use
		/// JGraphT, so this should be fixed)
		/// </remarks>
		public static SemanticGraph ResetVerticeOrdering(SemanticGraph sg)
		{
			SemanticGraph nsg = new SemanticGraph();
			IList<IndexedWord> vertices = sg.VertexListSorted();
			int index = 1;
			IDictionary<IndexedWord, IndexedWord> oldToNewVertices = Generics.NewHashMap();
			IList<IndexedWord> newVertices = new List<IndexedWord>();
			foreach (IndexedWord vertex in vertices)
			{
				IndexedWord newVertex = new IndexedWord(vertex);
				newVertex.SetIndex(index++);
				oldToNewVertices[vertex] = newVertex;
				///sg.removeVertex(vertex);
				newVertices.Add(newVertex);
			}
			foreach (IndexedWord nv in newVertices)
			{
				nsg.AddVertex(nv);
			}
			IList<IndexedWord> newRoots = new List<IndexedWord>();
			foreach (IndexedWord or in sg.GetRoots())
			{
				newRoots.Add(oldToNewVertices[or]);
			}
			nsg.SetRoots(newRoots);
			foreach (SemanticGraphEdge edge in sg.EdgeIterable())
			{
				IndexedWord newGov = oldToNewVertices[edge.GetGovernor()];
				IndexedWord newDep = oldToNewVertices[edge.GetDependent()];
				nsg.AddEdge(newGov, newDep, edge.GetRelation(), edge.GetWeight(), edge.IsExtra());
			}
			return nsg;
		}

		/// <summary>
		/// Given a graph, ensures all edges are EnglishGrammaticalRelations
		/// NOTE: this is English specific
		/// NOTE: currently EnglishGrammaticalRelations does not link collapsed prep string forms
		/// back to their object forms, for its valueOf relation.
		/// </summary>
		/// <remarks>
		/// Given a graph, ensures all edges are EnglishGrammaticalRelations
		/// NOTE: this is English specific
		/// NOTE: currently EnglishGrammaticalRelations does not link collapsed prep string forms
		/// back to their object forms, for its valueOf relation.  This may need to be repaired if
		/// generated edges indeed do have collapsed preps as strings.
		/// </remarks>
		public static void EnRepairEdges(SemanticGraph sg, bool verbose)
		{
			foreach (SemanticGraphEdge edge in sg.EdgeIterable())
			{
				if (edge.GetRelation().IsFromString())
				{
					GrammaticalRelation newReln = EnglishGrammaticalRelations.ValueOf(edge.GetRelation().ToString());
					if (newReln != null)
					{
						IndexedWord gov = edge.GetGovernor();
						IndexedWord dep = edge.GetDependent();
						double weight = edge.GetWeight();
						bool isExtra = edge.IsExtra();
						sg.RemoveEdge(edge);
						sg.AddEdge(gov, dep, newReln, weight, isExtra);
					}
					else
					{
						if (verbose)
						{
							log.Info("Warning, could not find matching GrammaticalRelation for reln=" + edge.GetRelation());
						}
					}
				}
			}
		}

		public static void EnRepairEdges(SemanticGraph sg)
		{
			EnRepairEdges(sg, false);
		}

		/// <summary>
		/// Deletes all nodes that are not rooted (such as dangling vertices after a series of
		/// edges have been chopped).
		/// </summary>
		public static void KillNonRooted(SemanticGraph sg)
		{
			IList<IndexedWord> nodes = new List<IndexedWord>(sg.VertexSet());
			// Hack: store all of the nodes we know are in the rootset
			ICollection<IndexedWord> guaranteed = Generics.NewHashSet();
			foreach (IndexedWord root in sg.GetRoots())
			{
				guaranteed.Add(root);
				Sharpen.Collections.AddAll(guaranteed, sg.Descendants(root));
			}
			foreach (IndexedWord node in nodes)
			{
				if (!guaranteed.Contains(node))
				{
					sg.RemoveVertex(node);
				}
			}
		}

		/// <summary>
		/// Replaces a node in the given SemanticGraph with the new node,
		/// replacing its position in the node edges.
		/// </summary>
		public static void ReplaceNode(IndexedWord newNode, IndexedWord oldNode, SemanticGraph sg)
		{
			// Obtain the edges where the old node was the governor and the dependent.
			// Remove the old node, insert the new, and re-insert the edges.
			// Save the edges in a list so that remove operations don't affect
			// the iterator or our ability to find the edges in the first place
			IList<SemanticGraphEdge> govEdges = sg.OutgoingEdgeList(oldNode);
			IList<SemanticGraphEdge> depEdges = sg.IncomingEdgeList(oldNode);
			bool oldNodeRemoved = sg.RemoveVertex(oldNode);
			if (oldNodeRemoved)
			{
				// If the new node is not present, be sure to add it in.
				if (!sg.ContainsVertex(newNode))
				{
					sg.AddVertex(newNode);
				}
				foreach (SemanticGraphEdge govEdge in govEdges)
				{
					sg.RemoveEdge(govEdge);
					sg.AddEdge(newNode, govEdge.GetDependent(), govEdge.GetRelation(), govEdge.GetWeight(), govEdge.IsExtra());
				}
				foreach (SemanticGraphEdge depEdge in depEdges)
				{
					sg.RemoveEdge(depEdge);
					sg.AddEdge(depEdge.GetGovernor(), newNode, depEdge.GetRelation(), depEdge.GetWeight(), depEdge.IsExtra());
				}
			}
			else
			{
				log.Info("SemanticGraphUtils.replaceNode: previous node does not exist");
			}
		}

		public const string WildcardVerticeToken = "WILDCARD";

		public static readonly IndexedWord WildcardVertice = new IndexedWord();

		static SemanticGraphUtils()
		{
			WildcardVertice.SetWord("*");
			WildcardVertice.SetValue("*");
			WildcardVertice.SetOriginalText("*");
		}

		/// <summary>
		/// GIven an iterable set of distinct vertices, creates a new mapping that maps the
		/// original vertices to a set of "generic" versions.
		/// </summary>
		/// <remarks>
		/// GIven an iterable set of distinct vertices, creates a new mapping that maps the
		/// original vertices to a set of "generic" versions.  Used for generalizing tokens in discovered rules.
		/// </remarks>
		/// <param name="verts">Vertices to anonymize</param>
		/// <param name="prefix">Prefix to assign to this anonymization</param>
		public static IDictionary<IndexedWord, IndexedWord> AnonymyizeNodes(IEnumerable<IndexedWord> verts, string prefix)
		{
			IDictionary<IndexedWord, IndexedWord> retMap = Generics.NewHashMap();
			int index = 1;
			foreach (IndexedWord orig in verts)
			{
				IndexedWord genericVert = new IndexedWord(orig);
				genericVert.Set(typeof(CoreAnnotations.LemmaAnnotation), string.Empty);
				string genericValue = prefix + index;
				genericVert.SetValue(genericValue);
				genericVert.SetWord(genericValue);
				genericVert.SetOriginalText(genericValue);
				index++;
				retMap[orig] = genericVert;
			}
			return retMap;
		}

		public const string SharedNodeAnonPrefix = "A";

		public const string BlanketNodeAnonPrefix = "B";

		/// <summary>
		/// Used to make a mapping that lets you create "anonymous" versions of shared nodes between two
		/// graphs (given in the arg) using the shared prefix.
		/// </summary>
		public static IDictionary<IndexedWord, IndexedWord> MakeGenericVertices(IEnumerable<IndexedWord> verts)
		{
			return AnonymyizeNodes(verts, SharedNodeAnonPrefix);
		}

		/// <summary>Used to assign generic labels to the nodes in the "blanket" for a set of vertices in a graph.</summary>
		/// <remarks>
		/// Used to assign generic labels to the nodes in the "blanket" for a set of vertices in a graph.  Here, a "blanket" node is
		/// similar to nodes in a Markov Blanket, i.e. nodes that are one edge away from a set of asserted vertices in a
		/// SemanticGraph.
		/// </remarks>
		public static IDictionary<IndexedWord, IndexedWord> MakeBlanketVertices(IEnumerable<IndexedWord> verts)
		{
			return AnonymyizeNodes(verts, BlanketNodeAnonPrefix);
		}

		/// <summary>
		/// Given a set of edges, and a mapping between the replacement and target vertices that comprise the
		/// vertices of the edges, returns a new set of edges with the replacement vertices.
		/// </summary>
		/// <remarks>
		/// Given a set of edges, and a mapping between the replacement and target vertices that comprise the
		/// vertices of the edges, returns a new set of edges with the replacement vertices.  If a replacement
		/// is not present, the WILDCARD_VERTICE is used in its place (i.e. can be anything).
		/// Currently used to generate "generic" versions of Semantic Graphs, when given a list of generic
		/// vertices to replace with, but can conceivably be used for other purposes where vertices must
		/// be replaced.
		/// </remarks>
		public static IList<SemanticGraphEdge> MakeReplacedEdges(IEnumerable<SemanticGraphEdge> edges, IDictionary<IndexedWord, IndexedWord> vertReplacementMap, bool useGenericReplacement)
		{
			IList<SemanticGraphEdge> retList = new List<SemanticGraphEdge>();
			foreach (SemanticGraphEdge edge in edges)
			{
				IndexedWord gov = edge.GetGovernor();
				IndexedWord dep = edge.GetDependent();
				IndexedWord newGov = vertReplacementMap[gov];
				IndexedWord newDep = vertReplacementMap[dep];
				if (useGenericReplacement)
				{
					if (newGov == null)
					{
						newGov = new IndexedWord(gov);
						newGov.Set(typeof(CoreAnnotations.TextAnnotation), WildcardVerticeToken);
						newGov.Set(typeof(CoreAnnotations.OriginalTextAnnotation), WildcardVerticeToken);
						newGov.Set(typeof(CoreAnnotations.LemmaAnnotation), WildcardVerticeToken);
					}
					if (newDep == null)
					{
						newDep = new IndexedWord(dep);
						newDep.Set(typeof(CoreAnnotations.TextAnnotation), WildcardVerticeToken);
						newDep.Set(typeof(CoreAnnotations.OriginalTextAnnotation), WildcardVerticeToken);
						newDep.Set(typeof(CoreAnnotations.LemmaAnnotation), WildcardVerticeToken);
					}
				}
				else
				{
					if (newGov == null)
					{
						newGov = edge.GetGovernor();
					}
					if (newDep == null)
					{
						newDep = edge.GetDependent();
					}
				}
				SemanticGraphEdge newEdge = new SemanticGraphEdge(newGov, newDep, edge.GetRelation(), edge.GetWeight(), edge.IsExtra());
				retList.Add(newEdge);
			}
			return retList;
		}

		/// <summary>
		/// Given a set of vertices from the same graph, returns the set of all edges between these
		/// vertices.
		/// </summary>
		public static ICollection<SemanticGraphEdge> AllEdgesInSet(IEnumerable<IndexedWord> vertices, SemanticGraph sg)
		{
			ICollection<SemanticGraphEdge> edges = Generics.NewHashSet();
			foreach (IndexedWord v1 in vertices)
			{
				foreach (SemanticGraphEdge edge in sg.OutgoingEdgeIterable(v1))
				{
					edges.Add(edge);
				}
				foreach (SemanticGraphEdge edge_1 in sg.IncomingEdgeIterable(v1))
				{
					edges.Add(edge_1);
				}
			}
			return edges;
		}

		/// <summary>
		/// Given two iterable sequences of edges, returns a pair containing the set of
		/// edges in the first graph not in the second, and edges in the second not in the first.
		/// </summary>
		/// <remarks>
		/// Given two iterable sequences of edges, returns a pair containing the set of
		/// edges in the first graph not in the second, and edges in the second not in the first.
		/// Edge equality is determined using an object that implements ISemanticGraphEdgeEql.
		/// </remarks>
		public static SemanticGraphUtils.EdgeDiffResult DiffEdges(ICollection<SemanticGraphEdge> edges1, ICollection<SemanticGraphEdge> edges2, SemanticGraph sg1, SemanticGraph sg2, IISemanticGraphEdgeEql compareObj)
		{
			ICollection<SemanticGraphEdge> remainingEdges1 = Generics.NewHashSet();
			ICollection<SemanticGraphEdge> remainingEdges2 = Generics.NewHashSet();
			ICollection<SemanticGraphEdge> sameEdges = Generics.NewHashSet();
			List<SemanticGraphEdge> edges2Cache = new List<SemanticGraphEdge>(edges2);
			foreach (SemanticGraphEdge edge1 in edges1)
			{
				foreach (SemanticGraphEdge edge2 in edges2Cache)
				{
					if (compareObj.Equals(edge1, edge2, sg1, sg2))
					{
						sameEdges.Add(edge1);
						edges2Cache.Remove(edge2);
						goto edge1Loop_continue;
					}
				}
				remainingEdges1.Add(edge1);
			}
edge1Loop_break: ;
			List<SemanticGraphEdge> edges1Cache = new List<SemanticGraphEdge>(edges1);
			foreach (SemanticGraphEdge edge2_1 in edges2)
			{
				foreach (SemanticGraphEdge edge1_1 in edges1)
				{
					if (compareObj.Equals(edge1_1, edge2_1, sg1, sg2))
					{
						edges1Cache.Remove(edge1_1);
						goto edge2Loop_continue;
					}
				}
				remainingEdges2.Add(edge2_1);
			}
edge2Loop_break: ;
			return new SemanticGraphUtils.EdgeDiffResult(sameEdges, remainingEdges1, remainingEdges2);
		}

		public class EdgeDiffResult
		{
			internal ICollection<SemanticGraphEdge> sameEdges;

			internal ICollection<SemanticGraphEdge> remaining1;

			internal ICollection<SemanticGraphEdge> remaining2;

			public EdgeDiffResult(ICollection<SemanticGraphEdge> sameEdges, ICollection<SemanticGraphEdge> remaining1, ICollection<SemanticGraphEdge> remaining2)
			{
				this.sameEdges = sameEdges;
				this.remaining1 = remaining1;
				this.remaining2 = remaining2;
			}

			public virtual ICollection<SemanticGraphEdge> GetRemaining1()
			{
				return remaining1;
			}

			public virtual ICollection<SemanticGraphEdge> GetRemaining2()
			{
				return remaining2;
			}

			public virtual ICollection<SemanticGraphEdge> GetSameEdges()
			{
				return sameEdges;
			}
		}

		/// <summary>Pretty printers</summary>
		public static string PrintEdges(IEnumerable<SemanticGraphEdge> edges)
		{
			StringWriter buf = new StringWriter();
			foreach (SemanticGraphEdge edge in edges)
			{
				buf.Append("\t");
				buf.Append(edge.GetRelation().ToString());
				buf.Append("(");
				buf.Append(edge.GetGovernor().ToString());
				buf.Append(", ");
				buf.Append(edge.GetDependent().ToString());
				buf.Append(")\n");
			}
			return buf.ToString();
		}

		public class PrintVerticeParams
		{
			public bool showWord = true;

			public bool showIndex = true;

			public bool showSentIndex = false;

			public bool showPOS = false;

			public int wrapAt = 8;
		}

		public static string PrintVertices(SemanticGraph sg)
		{
			return PrintVertices(sg, new SemanticGraphUtils.PrintVerticeParams());
		}

		public static string PrintVertices(SemanticGraph sg, SemanticGraphUtils.PrintVerticeParams @params)
		{
			StringWriter buf = new StringWriter();
			int count = 0;
			foreach (IndexedWord word in sg.VertexListSorted())
			{
				count++;
				if (count % @params.wrapAt == 0)
				{
					buf.Write("\n\t");
				}
				if (@params.showIndex)
				{
					buf.Write(word.Index().ToString());
					buf.Write(":");
				}
				if (@params.showSentIndex)
				{
					buf.Write("s");
					buf.Write(word.SentIndex().ToString());
					buf.Write("/");
				}
				if (@params.showPOS)
				{
					buf.Write(word.Tag());
					buf.Write("/");
				}
				if (@params.showWord)
				{
					buf.Write(word.Word());
				}
				buf.Write(" ");
			}
			return buf.ToString();
		}

		/// <summary>Given a SemanticGraph, creates a SemgrexPattern string based off of this graph.</summary>
		/// <remarks>
		/// Given a SemanticGraph, creates a SemgrexPattern string based off of this graph.
		/// NOTE: the word() value of the vertice is the name to reference
		/// NOTE: currently presumes there is only one root in this graph.
		/// TODO: see if Semgrex can allow multiroot patterns
		/// </remarks>
		/// <param name="sg">SemanticGraph to base this pattern on.</param>
		/// <exception cref="System.Exception"/>
		public static string SemgrexFromGraph(SemanticGraph sg, bool matchTag, bool matchWord, IDictionary<IndexedWord, string> nodeNameMap)
		{
			return SemgrexFromGraph(sg, null, matchTag, matchWord, nodeNameMap);
		}

		/// <exception cref="System.Exception"/>
		public static string SemgrexFromGraph(SemanticGraph sg, ICollection<IndexedWord> wildcardNodes, bool useTag, bool useWord, IDictionary<IndexedWord, string> nodeNameMap)
		{
			Func<IndexedWord, string> transformNode = null;
			return SemgrexFromGraph(sg, wildcardNodes, nodeNameMap, transformNode);
		}

		/// <summary>nodeValuesTranformation is a function that converts a vertex (IndexedWord) to the value.</summary>
		/// <remarks>
		/// nodeValuesTranformation is a function that converts a vertex (IndexedWord) to the value.
		/// For an example, see
		/// <c>semgrexFromGraph</c>
		/// function implementations (if useWord and useTag is true, the value is "{word: vertex.word; tag: vertex.tag}").
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public static string SemgrexFromGraph(SemanticGraph sg, ICollection<IndexedWord> wildcardNodes, IDictionary<IndexedWord, string> nodeNameMap, Func<IndexedWord, string> wordTransformation)
		{
			IndexedWord patternRoot = sg.GetFirstRoot();
			StringWriter buf = new StringWriter();
			ICollection<IndexedWord> tabu = Generics.NewHashSet();
			ICollection<SemanticGraphEdge> seenEdges = Generics.NewHashSet();
			buf.Append(SemgrexFromGraphHelper(patternRoot, sg, tabu, seenEdges, true, true, wildcardNodes, nodeNameMap, false, wordTransformation));
			string patternString = buf.ToString();
			return patternString;
		}

		/// <summary>
		/// Given a set of edges that form a rooted and connected graph, returns a Semgrex pattern
		/// corresponding to it.
		/// </summary>
		/// <exception cref="System.Exception"/>
		public static string SemgrexFromGraph(IEnumerable<SemanticGraphEdge> edges, bool matchTag, bool matchWord, IDictionary<IndexedWord, string> nodeNameMap)
		{
			SemanticGraph sg = SemanticGraphFactory.MakeFromEdges(edges);
			return SemgrexFromGraph(sg, matchTag, matchWord, nodeNameMap);
		}

		/// <summary>Recursive call to generate the Semgrex pattern based off of this SemanticGraph.</summary>
		/// <remarks>
		/// Recursive call to generate the Semgrex pattern based off of this SemanticGraph.
		/// nodeValuesTranformation is a function that converts a vertex (IndexedWord) to the value. For an example, see
		/// <c>semgrexFromGraph</c>
		/// function implementations.
		/// </remarks>
		protected internal static string SemgrexFromGraphHelper(IndexedWord vertice, SemanticGraph sg, ICollection<IndexedWord> tabu, ICollection<SemanticGraphEdge> seenEdges, bool useWordAsLabel, bool nameEdges, ICollection<IndexedWord> wildcardNodes
			, IDictionary<IndexedWord, string> nodeNameMap, bool orderedNodes, Func<IndexedWord, string> nodeValuesTransformation)
		{
			StringWriter buf = new StringWriter();
			// If the node is a wildcarded one, treat it as a {}, meaning any match.  Currently these will not
			// be labeled, but this may change later.
			if (wildcardNodes != null && wildcardNodes.Contains(vertice))
			{
				buf.Append("{}");
			}
			else
			{
				string vertexStr = nodeValuesTransformation.Apply(vertice);
				if (vertexStr != null && !vertexStr.IsEmpty())
				{
					buf.Append(vertexStr);
				}
			}
			//      buf.append("{");
			//      int i = 0;
			//      for(String corekey: useNodeCoreAnnotations){
			//        AnnotationLookup.KeyLookup lookup = AnnotationLookup.getCoreKey(corekey);
			//        assert lookup != null : "Invalid key " + corekey;
			//        if(i > 0)
			//          buf.append("; ");
			//        String value = vertice.containsKey(lookup.coreKey) ? vertice.get(lookup.coreKey).toString() : "null";
			//        buf.append(corekey+":"+nodeValuesTransformation.apply(value));
			//        i++;
			//      }
			//      if (useTag) {
			//
			//        buf.append("tag:"); buf.append(vertice.tag());
			//        if (useWord)
			//          buf.append(";");
			//      }
			//      if (useWord) {
			//        buf.append("word:"); buf.append(wordTransformation.apply(vertice.word()));
			//      }
			//      buf.append("}");
			if (nodeNameMap != null)
			{
				buf.Append("=");
				buf.Append(nodeNameMap[vertice]);
				buf.Append(" ");
			}
			else
			{
				if (useWordAsLabel)
				{
					buf.Append("=");
					buf.Append(SanitizeForSemgrexName(vertice.Word()));
					buf.Append(" ");
				}
			}
			tabu.Add(vertice);
			IEnumerable<SemanticGraphEdge> edgeIter = null;
			if (!orderedNodes)
			{
				edgeIter = sg.OutgoingEdgeIterable(vertice);
			}
			else
			{
				edgeIter = CollectionUtils.Sorted(sg.OutgoingEdgeList(vertice), null);
			}
			// For each edge, record the edge, but do not traverse to the vertice if it is already in the
			// tabu list.  If it already is, we emit the edge and the target vertice, as
			// we will not be continuing in that vertex, but we wish to record the relation.
			// If we will proceed down that node, add parens if it will continue recursing down.
			foreach (SemanticGraphEdge edge in edgeIter)
			{
				seenEdges.Add(edge);
				IndexedWord tgtVert = edge.GetDependent();
				bool applyParens = sg.OutDegree(tgtVert) > 0 && !tabu.Contains(tgtVert);
				buf.Append(" >");
				buf.Append(edge.GetRelation().ToString());
				if (nameEdges)
				{
					buf.Append("=E");
					buf.Write(seenEdges.Count.ToString());
				}
				buf.Append(" ");
				if (applyParens)
				{
					buf.Append("(");
				}
				if (tabu.Contains(tgtVert))
				{
					buf.Append("{tag:");
					buf.Append(tgtVert.Tag());
					buf.Append("}");
					if (useWordAsLabel)
					{
						buf.Append("=");
						buf.Append(tgtVert.Word());
						buf.Append(" ");
					}
				}
				else
				{
					buf.Append(SemgrexFromGraphHelper(tgtVert, sg, tabu, seenEdges, useWordAsLabel, nameEdges, wildcardNodes, nodeNameMap, orderedNodes, nodeValuesTransformation));
					if (applyParens)
					{
						buf.Append(")");
					}
				}
			}
			return buf.ToString();
		}

		/// <summary>Same as semgrexFromGraph except the node traversal is ordered by sorting</summary>
		/// <exception cref="System.Exception"/>
		public static string SemgrexFromGraphOrderedNodes(SemanticGraph sg, ICollection<IndexedWord> wildcardNodes, IDictionary<IndexedWord, string> nodeNameMap, Func<IndexedWord, string> wordTransformation)
		{
			IndexedWord patternRoot = sg.GetFirstRoot();
			StringWriter buf = new StringWriter();
			ICollection<IndexedWord> tabu = Generics.NewHashSet();
			ICollection<SemanticGraphEdge> seenEdges = Generics.NewHashSet();
			buf.Append(SemgrexFromGraphHelper(patternRoot, sg, tabu, seenEdges, true, true, wildcardNodes, nodeNameMap, true, wordTransformation));
			string patternString = buf.ToString();
			return patternString;
		}

		/// <summary>Sanitizes the given string into a Semgrex friendly name</summary>
		public static string SanitizeForSemgrexName(string text)
		{
			text = text.ReplaceAll("\\.", "_DOT_");
			text = text.ReplaceAll("\\,", "_COMMA_");
			text = text.ReplaceAll("\\\\", "_BSLASH_");
			text = text.ReplaceAll("\\/", "_BSLASH_");
			text = text.ReplaceAll("\\?", "_QUES_");
			text = text.ReplaceAll("\\!", "_BANG_");
			text = text.ReplaceAll("\\$", "_DOL_");
			text = text.ReplaceAll("\\!", "_BANG_");
			text = text.ReplaceAll("\\&", "_AMP_");
			text = text.ReplaceAll("\\:", "_COL_");
			text = text.ReplaceAll("\\;", "_SCOL_");
			text = text.ReplaceAll("\\#", "_PND_");
			text = text.ReplaceAll("\\@", "_AND_");
			text = text.ReplaceAll("\\%", "_PER_");
			text = text.ReplaceAll("\\(", "_LRB_");
			text = text.ReplaceAll("\\)", "_RRB_");
			return text;
		}

		/// <summary>
		/// Given a
		/// <c>SemanticGraph</c>
		/// , sets the lemmas on its label
		/// objects based on their word and tag.
		/// </summary>
		public static void Lemmatize(SemanticGraph sg)
		{
			foreach (IndexedWord node in sg.VertexSet())
			{
				node.SetLemma(Morphology.LemmaStatic(node.Word(), node.Tag()));
			}
		}

		/// <summary>GIven a graph, returns a new graph with the the new sentence index enforced.</summary>
		/// <remarks>
		/// GIven a graph, returns a new graph with the the new sentence index enforced.
		/// NOTE: new vertices are inserted.
		/// TODO: is this ok?  rewrite this?
		/// </remarks>
		public static SemanticGraph SetSentIndex(SemanticGraph sg, int newSentIndex)
		{
			SemanticGraph newGraph = new SemanticGraph(sg);
			IList<IndexedWord> prevRoots = new List<IndexedWord>(newGraph.GetRoots());
			IList<IndexedWord> newRoots = new List<IndexedWord>();
			// TODO: we are using vertexListSorted here because we're changing
			// vertices while iterating.  Perhaps there is a better way to do it.
			foreach (IndexedWord node in newGraph.VertexListSorted())
			{
				IndexedWord newWord = new IndexedWord(node);
				newWord.SetSentIndex(newSentIndex);
				SemanticGraphUtils.ReplaceNode(newWord, node, newGraph);
				if (prevRoots.Contains(node))
				{
					newRoots.Add(newWord);
				}
			}
			newGraph.SetRoots(newRoots);
			return newGraph;
		}

		//-----------------------------------------------------------------------------------------------
		//   Graph redundancy checks
		//-----------------------------------------------------------------------------------------------
		/// <summary>
		/// Removes duplicate graphs from the set, using the string form of the graph
		/// as the key (obviating issues with object equality).
		/// </summary>
		public static ICollection<SemanticGraph> RemoveDuplicates(ICollection<SemanticGraph> graphs)
		{
			IDictionary<string, SemanticGraph> map = Generics.NewHashMap();
			foreach (SemanticGraph sg in graphs)
			{
				string keyVal = string.Intern(sg.ToString());
				map[keyVal] = sg;
			}
			return map.Values;
		}

		/// <summary>
		/// Given the set of graphs to remove duplicates from, also removes those on the tabu graphs
		/// (and does not include them in the return set).
		/// </summary>
		public static ICollection<SemanticGraph> RemoveDuplicates(ICollection<SemanticGraph> graphs, ICollection<SemanticGraph> tabuGraphs)
		{
			IDictionary<string, SemanticGraph> tabuMap = Generics.NewHashMap();
			foreach (SemanticGraph tabuSg in tabuGraphs)
			{
				string keyVal = string.Intern(tabuSg.ToString());
				tabuMap[keyVal] = tabuSg;
			}
			IDictionary<string, SemanticGraph> map = Generics.NewHashMap();
			foreach (SemanticGraph sg in graphs)
			{
				string keyVal = string.Intern(sg.ToString());
				if (tabuMap.Contains(keyVal))
				{
					continue;
				}
				map[keyVal] = sg;
			}
			return map.Values;
		}

		public static ICollection<SemanticGraph> RemoveDuplicates(ICollection<SemanticGraph> graphs, SemanticGraph tabuGraph)
		{
			ICollection<SemanticGraph> tabuSet = Generics.NewHashSet();
			tabuSet.Add(tabuGraph);
			return RemoveDuplicates(graphs, tabuSet);
		}

		// -----------------------------------------------------------------------------------------------
		// Tree matching code
		// -----------------------------------------------------------------------------------------------
		/// <summary>
		/// Given a CFG Tree parse, and the equivalent SemanticGraph derived from that Tree, generates a mapping
		/// from each of the tree terminals to the best-guess SemanticGraph node(s).
		/// </summary>
		/// <remarks>
		/// Given a CFG Tree parse, and the equivalent SemanticGraph derived from that Tree, generates a mapping
		/// from each of the tree terminals to the best-guess SemanticGraph node(s).
		/// This is performed using lexical matching, finding the nth match.
		/// NOTE: not all tree nodes may match a Semgraph node, esp. for tokens removed in a collapsed Semgraph,
		/// such as prepositions.
		/// </remarks>
		public static IDictionary<SemanticGraphUtils.PositionedTree, IndexedWord> MapTreeToSg(Tree tree, SemanticGraph sg)
		{
			// In order to keep track of positions, we store lists, in order encountered, of lex terms.
			// e.g. lexToTreeNode.get("the").get(2) should point to the same word as lexToSemNode.get("the").get(2)
			// Because IndexedWords may be collapsed together "A B" -> "A_B", we check the value of current(), and
			// split on whitespace if present.
			MapList<string, SemanticGraphUtils.TreeNodeProxy> lexToTreeNode = new MapList<string, SemanticGraphUtils.TreeNodeProxy>();
			MapList<string, SemanticGraphUtils.IndexedWordProxy> lexToSemNode = new MapList<string, SemanticGraphUtils.IndexedWordProxy>();
			foreach (Tree child in tree.GetLeaves())
			{
				IList<SemanticGraphUtils.TreeNodeProxy> leafProxies = SemanticGraphUtils.TreeNodeProxy.Create(child, tree);
				foreach (SemanticGraphUtils.TreeNodeProxy proxy in leafProxies)
				{
					lexToTreeNode.Add(proxy.lex, proxy);
				}
			}
			IDictionary<IndexedWord, int> depthMap = Generics.NewHashMap();
			foreach (IndexedWord node in sg.VertexSet())
			{
				IList<IndexedWord> path = sg.GetPathToRoot(node);
				if (path != null)
				{
					depthMap[node] = path.Count;
				}
				else
				{
					depthMap[node] = 99999;
				}
				// Use an arbitrarily deep depth value, to trick it into never being used.
				IList<SemanticGraphUtils.IndexedWordProxy> nodeProxies = SemanticGraphUtils.IndexedWordProxy.Create(node);
				foreach (SemanticGraphUtils.IndexedWordProxy proxy in nodeProxies)
				{
					lexToSemNode.Add(proxy.lex, proxy);
				}
			}
			// Now the map-lists (string->position encountered indices) are populated,
			// simply go through, finding matches.
			// NOTE: we use TreeNodeProxy instead of keying off of Tree, as
			// hash codes for Tree nodes do not consider position of the tree
			// within a tree: two subtrees with the same layout and child
			// labels will be equal.
			IDictionary<SemanticGraphUtils.PositionedTree, IndexedWord> map = Generics.NewHashMap();
			foreach (string lex in lexToTreeNode.KeySet())
			{
				for (int i = 0; i < lexToTreeNode.Size(lex) && i < lexToSemNode.Size(lex); i++)
				{
					map[new SemanticGraphUtils.PositionedTree(lexToTreeNode.Get(lex, i).treeNode, tree)] = lexToSemNode.Get(lex, i).node;
				}
			}
			// Now that a terminals to terminals map has been generated, account for the
			// tree non-terminals.
			foreach (Tree nonTerm in tree)
			{
				if (!nonTerm.IsLeaf())
				{
					IndexedWord bestNode = null;
					int bestScore = 99999;
					foreach (Tree curr in nonTerm)
					{
						IndexedWord equivNode = map[new SemanticGraphUtils.PositionedTree(curr, tree)];
						if ((equivNode == null) || !depthMap.Contains(equivNode))
						{
							continue;
						}
						int currScore = depthMap[equivNode];
						if (currScore < bestScore)
						{
							bestScore = currScore;
							bestNode = equivNode;
						}
					}
					if (bestNode != null)
					{
						map[new SemanticGraphUtils.PositionedTree(nonTerm, tree)] = bestNode;
					}
				}
			}
			return map;
		}

		/// <summary>
		/// Private helper class for
		/// <c>mapTreeToSg</c>
		/// .   Acts to
		/// map between a Tree node and a lexical value.
		/// </summary>
		/// <author>Eric Yeh</author>
		private class TreeNodeProxy
		{
			internal Tree treeNode;

			internal string lex;

			internal Tree root;

			public override string ToString()
			{
				return lex + " -> " + treeNode.ToString() + ", #=" + treeNode.NodeNumber(root);
			}

			private TreeNodeProxy(Tree intree, string lex, Tree root)
			{
				this.treeNode = intree;
				this.lex = lex;
				this.root = root;
			}

			public static IList<SemanticGraphUtils.TreeNodeProxy> Create(Tree intree, Tree root)
			{
				IList<SemanticGraphUtils.TreeNodeProxy> ret = new List<SemanticGraphUtils.TreeNodeProxy>();
				if (intree.IsLeaf())
				{
					ret.Add(new SemanticGraphUtils.TreeNodeProxy(intree, intree.Label().Value(), root));
				}
				else
				{
					foreach (LabeledWord lword in intree.LabeledYield())
					{
						ret.Add(new SemanticGraphUtils.TreeNodeProxy(intree, lword.Word(), root));
					}
				}
				return ret;
			}
		}

		/// <summary>
		/// This is used to uniquely index trees within a
		/// Tree, maintaining the position of this subtree
		/// within the context of the root.
		/// </summary>
		/// <author>Eric Yeh</author>
		public class PositionedTree
		{
			internal Tree tree;

			internal Tree root;

			internal int nodeNumber;

			public override string ToString()
			{
				return tree + "." + nodeNumber;
			}

			public PositionedTree(Tree tree, Tree root)
			{
				this.tree = tree;
				this.root = root;
				this.nodeNumber = tree.NodeNumber(root);
			}

			public override bool Equals(object obj)
			{
				if (obj is SemanticGraphUtils.PositionedTree)
				{
					SemanticGraphUtils.PositionedTree tgt = (SemanticGraphUtils.PositionedTree)obj;
					return tree.Equals(tgt.tree) && root.Equals(tgt.root) && tgt.nodeNumber == nodeNumber;
				}
				return false;
			}

			/// <summary>TODO: verify this is correct</summary>
			public override int GetHashCode()
			{
				int hc = tree.GetHashCode() ^ (root.GetHashCode() << 8);
				hc ^= (2 ^ nodeNumber);
				return hc;
			}
		}

		/// <summary>
		/// Private helper class for
		/// <c>mapTreeToSg</c>
		/// .  Acts to
		/// map between an IndexedWord (in a SemanticGraph) and a lexical value.
		/// </summary>
		/// <author>lumberjack</author>
		private sealed class IndexedWordProxy
		{
			internal IndexedWord node;

			internal string lex;

			public override string ToString()
			{
				return lex + " -> " + node.Word() + ":" + node.SentIndex() + "." + node.Index();
			}

			private IndexedWordProxy(IndexedWord node, string lex)
			{
				this.node = node;
				this.lex = lex;
			}

			/// <summary>Generates a set of IndexedWordProxy objects.</summary>
			/// <remarks>
			/// Generates a set of IndexedWordProxy objects.  If the current() field is present, splits the tokens by
			/// a space, and for each, creates a new IndexedWordProxy, in order encountered, referencing this current
			/// node, but using the lexical value of the current split token.  Otherwise just use the value of word().
			/// This is used to retain attribution to the originating node.
			/// </remarks>
			public static IList<SemanticGraphUtils.IndexedWordProxy> Create(IndexedWord node)
			{
				IList<SemanticGraphUtils.IndexedWordProxy> ret = new List<SemanticGraphUtils.IndexedWordProxy>();
				if (node.OriginalText().Length > 0)
				{
					foreach (string token in node.OriginalText().Split(" "))
					{
						ret.Add(new SemanticGraphUtils.IndexedWordProxy(node, token));
					}
				}
				else
				{
					ret.Add(new SemanticGraphUtils.IndexedWordProxy(node, node.Word()));
				}
				return ret;
			}
		}

		/// <summary>Checks whether a given SemanticGraph is a strict surface syntax tree.</summary>
		/// <param name="sg"/>
		/// <returns/>
		public static bool IsTree(SemanticGraph sg)
		{
			if (sg.GetRoots().Count != 1)
			{
				return false;
			}
			IndexedWord root = sg.GetFirstRoot();
			ICollection<IndexedWord> visitedNodes = Generics.NewHashSet();
			IQueue<IndexedWord> queue = Generics.NewLinkedList();
			queue.Add(root);
			while (!queue.IsEmpty())
			{
				IndexedWord current = queue.Remove();
				visitedNodes.Add(current);
				foreach (SemanticGraphEdge edge in sg.OutgoingEdgeIterable(current))
				{
					IndexedWord dep = edge.GetDependent();
					if (visitedNodes.Contains(dep))
					{
						return false;
					}
					if (dep.CopyCount() > 0)
					{
						return false;
					}
					queue.Add(dep);
				}
			}
			return visitedNodes.Count == sg.Size();
		}
	}
}
