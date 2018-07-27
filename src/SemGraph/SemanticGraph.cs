using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Graph;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Semgraph
{
	/// <summary>
	/// Represents a semantic graph of a sentence or document, with IndexedWord
	/// objects for nodes.
	/// </summary>
	/// <remarks>
	/// Represents a semantic graph of a sentence or document, with IndexedWord
	/// objects for nodes.
	/// Notes:
	/// The root is not at present represented as a vertex in the graph.
	/// At present you need to get a root/roots
	/// from the separate roots variable and to know about it.
	/// This should maybe be changed, because otherwise, doing things like
	/// simply getting the set of nodes or edges from the graph doesn't give
	/// you root nodes or edges.
	/// Given the kinds of representations that we normally use with
	/// typedDependenciesCollapsed, there can be (small) cycles in a
	/// SemanticGraph, and these cycles may involve the node that is conceptually the
	/// root of the graph, so there may be no node without a parent node. You can
	/// better get at the root(s) via the variable and methods provided.
	/// There is no mechanism for returning all edges at once (e.g.,
	/// <c>edgeSet()</c>
	/// ).
	/// This is intentional.  Use
	/// <c>edgeIterable()</c>
	/// to iterate over the edges if necessary.
	/// </remarks>
	/// <author>Christopher Cox</author>
	/// <author>Teg Grenager</author>
	/// <seealso cref="SemanticGraphEdge"/>
	/// <seealso cref="Edu.Stanford.Nlp.Ling.IndexedWord"/>
	[System.Serializable]
	public class SemanticGraph
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Semgraph.SemanticGraph));

		public const bool addSRLArcs = false;

		private static readonly SemanticGraphFormatter formatter = new SemanticGraphFormatter();

		/// <summary>The distinguished root vertices, if known.</summary>
		private readonly ICollection<IndexedWord> roots;

		private readonly DirectedMultiGraph<IndexedWord, SemanticGraphEdge> graph;

		private static readonly MapFactory<IndexedWord, IDictionary<IndexedWord, IList<SemanticGraphEdge>>> outerMapFactory = MapFactory.HashMapFactory();

		private static readonly MapFactory<IndexedWord, IList<SemanticGraphEdge>> innerMapFactory = MapFactory.HashMapFactory();

		private static readonly MapFactory<IndexedWord, IndexedWord> wordMapFactory = MapFactory.HashMapFactory();

		private LinkedList<string> comments = new LinkedList<string>();

		// todo [cdm 2013]: The treatment of roots in this class should probably be redone.
		// todo [cdm 2013]: Probably we should put fake root node in graph and arc(s) from it.
		// todo [cdm 2013]: At any rate, printing methods should print the root
		public virtual int EdgeCount()
		{
			return graph.GetNumEdges();
		}

		public virtual int OutDegree(IndexedWord vertex)
		{
			return graph.GetOutDegree(vertex);
		}

		public virtual int InDegree(IndexedWord vertex)
		{
			return graph.GetInDegree(vertex);
		}

		public virtual IList<SemanticGraphEdge> GetAllEdges(IndexedWord gov, IndexedWord dep)
		{
			return graph.GetEdges(gov, dep);
		}

		// TODO: this is a bad method to use because there can be multiple
		// edges.  All users of this method should be switched to iterating
		// over getAllEdges.  This has already been done for all uses
		// outside RTE.
		public virtual SemanticGraphEdge GetEdge(IndexedWord gov, IndexedWord dep)
		{
			IList<SemanticGraphEdge> edges = graph.GetEdges(gov, dep);
			if (edges == null || edges.IsEmpty())
			{
				return null;
			}
			return edges[0];
		}

		public virtual void AddVertex(IndexedWord vertex)
		{
			graph.AddVertex(vertex);
		}

		public virtual bool ContainsVertex(IndexedWord vertex)
		{
			return graph.ContainsVertex(vertex);
		}

		public virtual bool ContainsEdge(IndexedWord source, IndexedWord target)
		{
			return graph.IsEdge(source, target);
		}

		public virtual bool ContainsEdge(SemanticGraphEdge edge)
		{
			return ContainsEdge(edge.GetSource(), edge.GetTarget());
		}

		public virtual ICollection<IndexedWord> VertexSet()
		{
			return graph.GetAllVertices();
		}

		public virtual bool RemoveEdge(SemanticGraphEdge e)
		{
			return graph.RemoveEdge(e.GetSource(), e.GetTarget(), e);
		}

		public virtual bool RemoveVertex(IndexedWord vertex)
		{
			return graph.RemoveVertex(vertex);
		}

		/// <summary>
		/// This returns an ordered list of vertices (based upon their
		/// indices in the sentence).
		/// </summary>
		/// <remarks>
		/// This returns an ordered list of vertices (based upon their
		/// indices in the sentence). This creates and sorts a list, so
		/// prefer vertexSet unless you have a good reason to want nodes in
		/// index order.
		/// </remarks>
		/// <returns>Ordered list of vertices</returns>
		public virtual IList<IndexedWord> VertexListSorted()
		{
			List<IndexedWord> vList = new List<IndexedWord>(VertexSet());
			vList.Sort();
			return vList;
		}

		/// <summary>Returns an ordered list of edges in the graph.</summary>
		/// <remarks>
		/// Returns an ordered list of edges in the graph.
		/// This creates and sorts a list, so prefer edgeIterable().
		/// </remarks>
		/// <returns>A ordered list of edges in the graph.</returns>
		public virtual IList<SemanticGraphEdge> EdgeListSorted()
		{
			List<SemanticGraphEdge> edgeList = new List<SemanticGraphEdge>();
			foreach (SemanticGraphEdge edge in EdgeIterable())
			{
				edgeList.Add(edge);
			}
			edgeList.Sort(SemanticGraphEdge.OrderByTargetComparator());
			return edgeList;
		}

		public virtual IEnumerable<SemanticGraphEdge> EdgeIterable()
		{
			return graph.EdgeIterable();
		}

		public virtual IEnumerator<SemanticGraphEdge> OutgoingEdgeIterator(IndexedWord v)
		{
			return graph.OutgoingEdgeIterator(v);
		}

		public virtual IEnumerable<SemanticGraphEdge> OutgoingEdgeIterable(IndexedWord v)
		{
			return graph.OutgoingEdgeIterable(v);
		}

		public virtual IEnumerator<SemanticGraphEdge> IncomingEdgeIterator(IndexedWord v)
		{
			return graph.IncomingEdgeIterator(v);
		}

		public virtual IEnumerable<SemanticGraphEdge> IncomingEdgeIterable(IndexedWord v)
		{
			return graph.IncomingEdgeIterable(v);
		}

		public virtual IList<SemanticGraphEdge> OutgoingEdgeList(IndexedWord v)
		{
			return CollectionUtils.ToList(OutgoingEdgeIterable(v));
		}

		public virtual IList<SemanticGraphEdge> IncomingEdgeList(IndexedWord v)
		{
			return CollectionUtils.ToList(IncomingEdgeIterable(v));
		}

		public virtual bool IsEmpty()
		{
			return graph.IsEmpty();
		}

		/// <summary>
		/// Searches up to 2 levels to determine how far ancestor is from child (i.e.,
		/// returns 1 if "ancestor" is a parent, or 2 if ancestor is a grandparent.
		/// </summary>
		/// <param name="child">candidate child</param>
		/// <param name="ancestor">candidate ancestor</param>
		/// <returns>
		/// the number of generations between "child" and "ancestor" (1 is an
		/// immediate parent), or -1 if there is no relationship found.
		/// </returns>
		public virtual int IsAncestor(IndexedWord child, IndexedWord ancestor)
		{
			ICollection<IndexedWord> parents = this.GetParents(child);
			if (parents.Contains(ancestor))
			{
				return 1;
			}
			foreach (IndexedWord parent in parents)
			{
				ICollection<IndexedWord> grandparents = this.GetParents(parent);
				if (grandparents.Contains(ancestor))
				{
					return 2;
				}
			}
			return -1;
		}

		/// <summary>Return the maximum distance to a least common ancestor.</summary>
		/// <remarks>
		/// Return the maximum distance to a least common ancestor. We only search as
		/// high as grandparents. We return -1 if no common parent or grandparent is
		/// found.
		/// </remarks>
		/// <returns>The maximum distance to a least common ancestor.</returns>
		public virtual int CommonAncestor(IndexedWord v1, IndexedWord v2)
		{
			if (v1.Equals(v2))
			{
				return 0;
			}
			ICollection<IndexedWord> v1Parents = this.GetParents(v1);
			ICollection<IndexedWord> v2Parents = this.GetParents(v2);
			ICollection<IndexedWord> v1GrandParents = wordMapFactory.NewSet();
			ICollection<IndexedWord> v2GrandParents = wordMapFactory.NewSet();
			if (v1Parents.Contains(v2) || v2Parents.Contains(v1))
			{
				return 1;
			}
			// does v1 have any parents that are v2's parents?
			foreach (IndexedWord v1Parent in v1Parents)
			{
				if (v2Parents.Contains(v1Parent))
				{
					return 1;
				}
				Sharpen.Collections.AddAll(v1GrandParents, this.GetParents(v1Parent));
			}
			// build v2 grandparents
			foreach (IndexedWord v2Parent in v2Parents)
			{
				Sharpen.Collections.AddAll(v2GrandParents, this.GetParentList(v2Parent));
			}
			if (v1GrandParents.Contains(v2) || v2GrandParents.Contains(v1))
			{
				return 2;
			}
			// Are any of v1's parents a grandparent of v2?
			foreach (IndexedWord v2GrandParent in v2GrandParents)
			{
				if (v1Parents.Contains(v2GrandParent))
				{
					return 2;
				}
			}
			// Are any of v2's parents a grandparent of v1?
			foreach (IndexedWord v1GrandParent in v1GrandParents)
			{
				if (v2Parents.Contains(v1GrandParent))
				{
					return 2;
				}
			}
			foreach (IndexedWord v2GrandParent_1 in v2GrandParents)
			{
				if (v1GrandParents.Contains(v2GrandParent_1))
				{
					return 2;
				}
			}
			return -1;
		}

		/// <summary>Returns the least common ancestor.</summary>
		/// <remarks>
		/// Returns the least common ancestor. We only search as high as grandparents.
		/// We return null if no common parent or grandparent is found. Any of the
		/// input words can also be the answer if one is the parent or grandparent of
		/// other, or if the input words are the same.
		/// </remarks>
		/// <returns>The least common ancestor.</returns>
		public virtual IndexedWord GetCommonAncestor(IndexedWord v1, IndexedWord v2)
		{
			if (v1.Equals(v2))
			{
				return v1;
			}
			if (this.IsAncestor(v1, v2) >= 1)
			{
				return v2;
			}
			if (this.IsAncestor(v2, v1) >= 1)
			{
				return v1;
			}
			ICollection<IndexedWord> v1Parents = this.GetParents(v1);
			ICollection<IndexedWord> v2Parents = this.GetParents(v2);
			ICollection<IndexedWord> v1GrandParents = wordMapFactory.NewSet();
			ICollection<IndexedWord> v2GrandParents = wordMapFactory.NewSet();
			// does v1 have any parents that are v2's parents?
			foreach (IndexedWord v1Parent in v1Parents)
			{
				if (v2Parents.Contains(v1Parent))
				{
					return v1Parent;
				}
				Sharpen.Collections.AddAll(v1GrandParents, this.GetParents(v1Parent));
			}
			// does v1 have any grandparents that are v2's parents?
			foreach (IndexedWord v1GrandParent in v1GrandParents)
			{
				if (v2Parents.Contains(v1GrandParent))
				{
					return v1GrandParent;
				}
			}
			// build v2 grandparents
			foreach (IndexedWord v2Parent in v2Parents)
			{
				Sharpen.Collections.AddAll(v2GrandParents, this.GetParents(v2Parent));
			}
			// does v1 have any parents or grandparents that are v2's grandparents?
			foreach (IndexedWord v2GrandParent in v2GrandParents)
			{
				if (v1Parents.Contains(v2GrandParent))
				{
					return v2GrandParent;
				}
				if (v1GrandParents.Contains(v2GrandParent))
				{
					return v2GrandParent;
				}
			}
			return null;
		}

		// todo [cdm 2013]: Completely RTE-specific methods like this one should be used to a static class of helper methods under RTE
		// If "det" is true, the search for a child is restricted to the "determiner"
		// grammatical relation.
		public virtual bool MatchPatternToVertex(string pattern, IndexedWord vertex, bool det)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			string pat = pattern.ReplaceAll("<", ",<");
			pat = pat.ReplaceAll(">", ",>");
			string[] nodePath = pat.Split(",");
			foreach (string s in nodePath)
			{
				if (s.IsEmpty())
				{
					continue;
				}
				string word = Sharpen.Runtime.Substring(s, 1);
				char dir = s[0];
				if (dir == '<')
				{
					// look for a matching parent
					bool match = false;
					foreach (IndexedWord parent in GetParents(vertex))
					{
						string lemma = parent.Get(typeof(CoreAnnotations.LemmaAnnotation));
						if (lemma.Equals(word))
						{
							match = true;
							break;
						}
					}
					if (!match)
					{
						return false;
					}
				}
				else
				{
					if (dir == '>')
					{
						if (det)
						{
							// look for a matching child with "det" relation
							ICollection<IndexedWord> children = wordMapFactory.NewSet();
							Sharpen.Collections.AddAll(children, GetChildrenWithReln(vertex, EnglishGrammaticalRelations.Determiner));
							Sharpen.Collections.AddAll(children, GetChildrenWithReln(vertex, EnglishGrammaticalRelations.Predeterminer));
							bool match = false;
							foreach (IndexedWord child in children)
							{
								string lemma = child.Get(typeof(CoreAnnotations.LemmaAnnotation));
								if (lemma.IsEmpty())
								{
									lemma = child.Word().ToLower();
								}
								if (lemma.Equals(word))
								{
									match = true;
									break;
								}
							}
							if (!match)
							{
								return false;
							}
						}
						else
						{
							// take any relation, except "det"
							IList<Pair<GrammaticalRelation, IndexedWord>> children = ChildPairs(vertex);
							bool match = false;
							foreach (Pair<GrammaticalRelation, IndexedWord> pair in children)
							{
								if (pair.First().ToString().Equals("det"))
								{
									continue;
								}
								IndexedWord child = pair.Second();
								string lemma = child.Get(typeof(CoreAnnotations.LemmaAnnotation));
								if (lemma.IsEmpty())
								{
									lemma = child.Word().ToLower();
								}
								if (lemma.Equals(word))
								{
									match = true;
									break;
								}
							}
							if (!match)
							{
								return false;
							}
						}
					}
					else
					{
						throw new Exception("Warning: bad pattern \"%s\"\n" + pattern);
					}
				}
			}
			return true;
		}

		// todo [cdm 2013]: Completely RTE-specific methods like this one should be used to a static class of helper methods under RTE
		public virtual bool MatchPatternToVertex(string pattern, IndexedWord vertex)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			string pat = pattern.ReplaceAll("<", ",<");
			pat = pat.ReplaceAll(">", ",>");
			string[] nodePath = pat.Split(",");
			foreach (string s in nodePath)
			{
				if (s.IsEmpty())
				{
					continue;
				}
				string word = Sharpen.Runtime.Substring(s, 1);
				char dir = s[0];
				if (dir == '<')
				{
					// look for a matching parent
					bool match = false;
					foreach (IndexedWord parent in GetParents(vertex))
					{
						string lemma = parent.Get(typeof(CoreAnnotations.LemmaAnnotation));
						if (lemma.Equals(word))
						{
							match = true;
							break;
						}
					}
					if (!match)
					{
						return false;
					}
				}
				else
				{
					if (dir == '>')
					{
						// look for a matching child
						bool match = false;
						foreach (IndexedWord child in GetChildren(vertex))
						{
							string lemma = child.Get(typeof(CoreAnnotations.LemmaAnnotation));
							if (lemma == null || lemma.IsEmpty())
							{
								lemma = child.Word().ToLower();
							}
							if (lemma.Equals(word))
							{
								match = true;
								break;
							}
						}
						if (!match)
						{
							return false;
						}
					}
					else
					{
						throw new Exception("Warning: bad pattern \"%s\"\n" + pattern);
					}
				}
			}
			return true;
		}

		public virtual IList<IndexedWord> GetChildList(IndexedWord vertex)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			IList<IndexedWord> result = new List<IndexedWord>(GetChildren(vertex));
			result.Sort();
			return result;
		}

		public virtual ICollection<IndexedWord> GetChildren(IndexedWord vertex)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			return graph.GetChildren(vertex);
		}

		public virtual bool HasChildren(IndexedWord vertex)
		{
			return OutgoingEdgeIterator(vertex).MoveNext();
		}

		public virtual IList<SemanticGraphEdge> GetIncomingEdgesSorted(IndexedWord vertex)
		{
			IList<SemanticGraphEdge> edges = IncomingEdgeList(vertex);
			edges.Sort();
			return edges;
		}

		public virtual IList<SemanticGraphEdge> GetOutEdgesSorted(IndexedWord vertex)
		{
			IList<SemanticGraphEdge> edges = OutgoingEdgeList(vertex);
			edges.Sort();
			return edges;
		}

		public virtual IList<IndexedWord> GetParentList(IndexedWord vertex)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			IList<IndexedWord> result = new List<IndexedWord>(GetParents(vertex));
			result.Sort();
			return result;
		}

		public virtual ICollection<IndexedWord> GetParents(IndexedWord vertex)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			return graph.GetParents(vertex);
		}

		/// <summary>Method for getting the siblings of a particular node.</summary>
		/// <remarks>
		/// Method for getting the siblings of a particular node. Siblings are the
		/// other children of your parent, where parent is determined as the parent
		/// returned by getParent
		/// </remarks>
		/// <returns>
		/// collection of sibling nodes (does not include vertex)
		/// the collection is empty if your parent is null
		/// </returns>
		public virtual ICollection<IndexedWord> GetSiblings(IndexedWord vertex)
		{
			IndexedWord parent = this.GetParent(vertex);
			if (parent != null)
			{
				ICollection<IndexedWord> result = wordMapFactory.NewSet();
				Sharpen.Collections.AddAll(result, this.GetChildren(parent));
				result.Remove(vertex);
				//remove this vertex - you're not your own sibling
				return result;
			}
			else
			{
				return Java.Util.Collections.EmptySet();
			}
		}

		/// <summary>Helper function for the public function with the same name.</summary>
		/// <remarks>
		/// Helper function for the public function with the same name.
		/// <br />
		/// Builds up the list backwards.
		/// </remarks>
		private IList<IndexedWord> GetPathToRoot(IndexedWord vertex, IList<IndexedWord> used)
		{
			used.Add(vertex);
			// TODO: Apparently the order of the nodes in the path to the root
			// makes a difference for the RTE system.  Look into this some more
			IList<IndexedWord> parents = GetParentList(vertex);
			// Set<IndexedWord> parents = wordMapFactory.newSet();
			// parents.addAll(getParents(vertex));
			parents.RemoveAll(used);
			if (roots.Contains(vertex) || (parents.IsEmpty()))
			{
				used.Remove(used.Count - 1);
				if (roots.Contains(vertex))
				{
					return Generics.NewArrayList();
				}
				else
				{
					return null;
				}
			}
			// no path found
			foreach (IndexedWord parent in parents)
			{
				IList<IndexedWord> path = GetPathToRoot(parent, used);
				if (path != null)
				{
					path.Add(parent);
					used.Remove(used.Count - 1);
					return path;
				}
			}
			used.Remove(used.Count - 1);
			return null;
		}

		/// <summary>Find the path from the given node to a root.</summary>
		/// <remarks>
		/// Find the path from the given node to a root. The path does not include the
		/// given node. Returns an empty list if vertex is a root. Returns null if a
		/// root is inaccessible (should never happen).
		/// </remarks>
		public virtual IList<IndexedWord> GetPathToRoot(IndexedWord vertex)
		{
			IList<IndexedWord> path = GetPathToRoot(vertex, Generics.NewArrayList());
			if (path != null)
			{
				Java.Util.Collections.Reverse(path);
			}
			return path;
		}

		/// <summary>Return the real syntactic parent of vertex.</summary>
		public virtual IndexedWord GetParent(IndexedWord vertex)
		{
			IList<IndexedWord> path = GetPathToRoot(vertex);
			if (path != null && path.Count > 0)
			{
				return path[0];
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Returns the <em>first</em>
		/// <see cref="Edu.Stanford.Nlp.Ling.IndexedWord">IndexedWord</see>
		/// in this
		/// <c>SemanticGraph</c>
		/// having the given integer index,
		/// or throws
		/// <c>IllegalArgumentException</c>
		/// if no such node is found.
		/// </summary>
		/// <exception cref="System.ArgumentException"/>
		public virtual IndexedWord GetNodeByIndex(int index)
		{
			IndexedWord node = GetNodeByIndexSafe(index);
			if (node == null)
			{
				throw new ArgumentException("No SemanticGraph vertex with index " + index);
			}
			else
			{
				return node;
			}
		}

		/// <summary>
		/// Same as above, but returns
		/// <see langword="null"/>
		/// if the index does not exist
		/// (instead of throwing an exception).
		/// </summary>
		public virtual IndexedWord GetNodeByIndexSafe(int index)
		{
			foreach (IndexedWord vertex in VertexSet())
			{
				if (vertex.Index() == index)
				{
					return vertex;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns the <em>first</em>
		/// <see cref="Edu.Stanford.Nlp.Ling.IndexedWord">IndexedWord</see>
		/// in this
		/// <c>SemanticGraph</c>
		/// having the given integer index,
		/// or throws
		/// <c>IllegalArgumentException</c>
		/// if no such node is found.
		/// </summary>
		/// <exception cref="System.ArgumentException"/>
		public virtual IndexedWord GetNodeByIndexAndCopyCount(int index, int copyCount)
		{
			IndexedWord node = GetNodeByIndexAndCopyCountSafe(index, copyCount);
			if (node == null)
			{
				throw new ArgumentException("No SemanticGraph vertex with index " + index + " and copyCount " + copyCount);
			}
			else
			{
				return node;
			}
		}

		/// <summary>
		/// Same as above, but returns
		/// <see langword="null"/>
		/// if the index does not exist
		/// (instead of throwing an exception).
		/// </summary>
		public virtual IndexedWord GetNodeByIndexAndCopyCountSafe(int index, int copyCount)
		{
			foreach (IndexedWord vertex in VertexSet())
			{
				if (vertex.Index() == index && vertex.CopyCount() == copyCount)
				{
					return vertex;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns the <i>first</i>
		/// <see cref="Edu.Stanford.Nlp.Ling.IndexedWord">IndexedWord</see>
		/// in this
		/// <c>SemanticGraph</c>
		/// having the given word or
		/// regex, or return null if no such found.
		/// </summary>
		public virtual IndexedWord GetNodeByWordPattern(string pattern)
		{
			Pattern p = Pattern.Compile(pattern);
			foreach (IndexedWord vertex in VertexSet())
			{
				string w = vertex.Word();
				if ((w == null && pattern == null) || w != null && p.Matcher(w).Matches())
				{
					return vertex;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns all nodes of type
		/// <see cref="Edu.Stanford.Nlp.Ling.IndexedWord">IndexedWord</see>
		/// in this
		/// <c>SemanticGraph</c>
		/// having the given word or
		/// regex, or returns empty list if no such found.
		/// </summary>
		public virtual IList<IndexedWord> GetAllNodesByWordPattern(string pattern)
		{
			Pattern p = Pattern.Compile(pattern);
			IList<IndexedWord> nodes = new List<IndexedWord>();
			foreach (IndexedWord vertex in VertexSet())
			{
				string w = vertex.Word();
				if ((w == null && pattern == null) || w != null && p.Matcher(w).Matches())
				{
					nodes.Add(vertex);
				}
			}
			return nodes;
		}

		public virtual IList<IndexedWord> GetAllNodesByPartOfSpeechPattern(string pattern)
		{
			Pattern p = Pattern.Compile(pattern);
			IList<IndexedWord> nodes = new List<IndexedWord>();
			foreach (IndexedWord vertex in VertexSet())
			{
				string pos = vertex.Tag();
				if ((pos == null && pattern == null) || pos != null && p.Matcher(pos).Matches())
				{
					nodes.Add(vertex);
				}
			}
			return nodes;
		}

		/// <summary>Returns the set of descendants governed by this node in the graph.</summary>
		public virtual ICollection<IndexedWord> Descendants(IndexedWord vertex)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			// Do a depth first search
			ICollection<IndexedWord> descendantSet = wordMapFactory.NewSet();
			DescendantsHelper(vertex, descendantSet);
			return descendantSet;
		}

		private void DescendantsHelper(IndexedWord curr, ICollection<IndexedWord> descendantSet)
		{
			if (descendantSet.Contains(curr))
			{
				return;
			}
			descendantSet.Add(curr);
			foreach (IndexedWord child in GetChildren(curr))
			{
				DescendantsHelper(child, descendantSet);
			}
		}

		/// <summary>
		/// Returns a list of pairs of a relation name and the child
		/// IndexedFeatureLabel that bears that relation.
		/// </summary>
		public virtual IList<Pair<GrammaticalRelation, IndexedWord>> ChildPairs(IndexedWord vertex)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			IList<Pair<GrammaticalRelation, IndexedWord>> childPairs = Generics.NewArrayList();
			foreach (SemanticGraphEdge e in OutgoingEdgeIterable(vertex))
			{
				childPairs.Add(new Pair<GrammaticalRelation, IndexedWord>(e.GetRelation(), e.GetTarget()));
			}
			return childPairs;
		}

		/// <summary>
		/// Returns a list of pairs of a relation name and the parent
		/// IndexedFeatureLabel to which we bear that relation.
		/// </summary>
		public virtual IList<Pair<GrammaticalRelation, IndexedWord>> ParentPairs(IndexedWord vertex)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			IList<Pair<GrammaticalRelation, IndexedWord>> parentPairs = Generics.NewArrayList();
			foreach (SemanticGraphEdge e in IncomingEdgeIterable(vertex))
			{
				parentPairs.Add(new Pair<GrammaticalRelation, IndexedWord>(e.GetRelation(), e.GetSource()));
			}
			return parentPairs;
		}

		/// <summary>Returns a set of relations which this node has with its parents.</summary>
		/// <returns>The set of relations which this node has with its parents.</returns>
		public virtual ICollection<GrammaticalRelation> Relns(IndexedWord vertex)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			ICollection<GrammaticalRelation> relns = Generics.NewHashSet();
			IList<Pair<GrammaticalRelation, IndexedWord>> pairs = ParentPairs(vertex);
			foreach (Pair<GrammaticalRelation, IndexedWord> p in pairs)
			{
				relns.Add(p.First());
			}
			return relns;
		}

		/// <summary>Returns the relation that node a has with node b.</summary>
		/// <remarks>
		/// Returns the relation that node a has with node b.
		/// Note: there may be multiple arcs between
		/// <paramref name="a"/>
		/// and
		/// <paramref name="b"/>
		/// , and this method only returns one relation.
		/// </remarks>
		public virtual GrammaticalRelation Reln(IndexedWord a, IndexedWord b)
		{
			if (!ContainsVertex(a))
			{
				throw new ArgumentException();
			}
			IList<Pair<GrammaticalRelation, IndexedWord>> pairs = ChildPairs(a);
			foreach (Pair<GrammaticalRelation, IndexedWord> p in pairs)
			{
				if (p.Second().Equals(b))
				{
					return p.First();
				}
			}
			return null;
		}

		/// <summary>Returns a list of relations which this node has with its children.</summary>
		public virtual ICollection<GrammaticalRelation> ChildRelns(IndexedWord vertex)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			ICollection<GrammaticalRelation> relns = Generics.NewHashSet();
			IList<Pair<GrammaticalRelation, IndexedWord>> pairs = ChildPairs(vertex);
			foreach (Pair<GrammaticalRelation, IndexedWord> p in pairs)
			{
				relns.Add(p.First());
			}
			return relns;
		}

		public virtual ICollection<IndexedWord> GetRoots()
		{
			return roots;
		}

		/// <summary>Initially looks for nodes which have no incoming arcs.</summary>
		/// <remarks>
		/// Initially looks for nodes which have no incoming arcs. If there are any, it
		/// returns a list of them. If not, it looks for nodes from which every other
		/// node is reachable. If there are any, it returns a list of them. Otherwise,
		/// it returns an empty list.
		/// </remarks>
		/// <returns>A list of root nodes or an empty list.</returns>
		private IList<IndexedWord> GetVerticesWithoutParents()
		{
			IList<IndexedWord> result = new List<IndexedWord>();
			foreach (IndexedWord v in VertexSet())
			{
				int inDegree = InDegree(v);
				if (inDegree == 0)
				{
					result.Add(v);
				}
			}
			result.Sort();
			return result;
		}

		/// <summary>Returns the (first) root of this SemanticGraph.</summary>
		public virtual IndexedWord GetFirstRoot()
		{
			if (roots.IsEmpty())
			{
				throw new Exception("No roots in graph:\n" + this + "\nFind where this graph was created and make sure you're adding roots.");
			}
			return roots.GetEnumerator().Current;
		}

		public virtual void AddRoot(IndexedWord root)
		{
			AddVertex(root);
			roots.Add(root);
		}

		/// <summary>This method should not be used if possible.</summary>
		/// <remarks>
		/// This method should not be used if possible. TODO: delete it
		/// Recomputes the roots, based of actual candidates. This is done to
		/// ensure a rooted tree after a sequence of edits. If the none of the vertices
		/// can act as a root (due to a cycle), keep old rootset, retaining only the
		/// existing vertices on that list.
		/// TODO: this cannot deal with "Hamburg is a city which everyone likes", as
		/// the intended root node,'Hamburg, is also the dobj of the relative clause. A
		/// possible solution would be to create edgeset routines that allow filtering
		/// over a predicate, and specifically filter out dobj relations for choosing
		/// next best candidate. This could also be useful for dealing with
		/// non-syntactic arcs in the future. TODO: There is also the possibility the
		/// roots could be empty at the end, and will need to be resolved. TODO:
		/// determine if this is a reasonably correct solution.
		/// </remarks>
		public virtual void ResetRoots()
		{
			ICollection<IndexedWord> newRoots = GetVerticesWithoutParents();
			if (newRoots.Count > 0)
			{
				roots.Clear();
				Sharpen.Collections.AddAll(roots, newRoots);
				return;
			}
			/*
			* else { Collection<IndexedWord> oldRoots = new
			* ArrayList<IndexedWord>(roots); for (IndexedWord oldRoot : oldRoots) { if
			* (!containsVertex(oldRoot)) removeVertex(oldRoot); } }
			*/
			// If no apparent root candidates are available, likely due to loop back
			// edges (rcmod), find the node that dominates the most nodes, and let
			// that be the new root. Note this implementation epitomizes K.I.S.S., and
			// is brain dead and non-optimal, and will require further work.
			TwoDimensionalCounter<IndexedWord, IndexedWord> nodeDists = TwoDimensionalCounter.IdentityHashMapCounter();
			foreach (IndexedWord node1 in VertexSet())
			{
				foreach (IndexedWord node2 in VertexSet())
				{
					// want directed paths only
					IList<SemanticGraphEdge> path = GetShortestDirectedPathEdges(node1, node2);
					if (path != null)
					{
						int dist = path.Count;
						nodeDists.SetCount(node1, node2, dist);
					}
				}
			}
			// K.I.S.S. alg: just sum up and see who's on top, values don't have much
			// meaning outside of determining dominance.
			ClassicCounter<IndexedWord> dominatedEdgeCount = ClassicCounter.IdentityHashMapCounter();
			foreach (IndexedWord outer in VertexSet())
			{
				foreach (IndexedWord inner in VertexSet())
				{
					dominatedEdgeCount.IncrementCount(outer, nodeDists.GetCount(outer, inner));
				}
			}
			IndexedWord winner = Counters.Argmax(dominatedEdgeCount);
			// TODO: account for multiply rooted graphs later
			SetRoot(winner);
		}

		public virtual void SetRoot(IndexedWord word)
		{
			roots.Clear();
			roots.Add(word);
		}

		public virtual void SetRoots(ICollection<IndexedWord> words)
		{
			roots.Clear();
			Sharpen.Collections.AddAll(roots, words);
		}

		/// <returns>A sorted list of the vertices</returns>
		/// <exception cref="System.InvalidOperationException">if this graph is not a DAG</exception>
		public virtual IList<IndexedWord> TopologicalSort()
		{
			return graph.TopologicalSort();
		}

		/// <summary>
		/// Does the given
		/// <paramref name="vertex"/>
		/// have at least one child with the given
		/// <paramref name="reln"/>
		/// and the lemma
		/// <paramref name="childLemma"/>
		/// ?
		/// </summary>
		public virtual bool HasChild(IndexedWord vertex, GrammaticalRelation reln, string childLemma)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			foreach (SemanticGraphEdge edge in OutgoingEdgeIterable(vertex))
			{
				if (edge.GetRelation().Equals(reln))
				{
					if (edge.GetTarget().Get(typeof(CoreAnnotations.LemmaAnnotation)).Equals(childLemma))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Does the given
		/// <paramref name="vertex"/>
		/// have at least one child with the given
		/// <paramref name="reln"/>
		/// ?
		/// </summary>
		public virtual bool HasChildWithReln(IndexedWord vertex, GrammaticalRelation reln)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			foreach (SemanticGraphEdge edge in OutgoingEdgeIterable(vertex))
			{
				if (edge.GetRelation().Equals(reln))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Returns true if vertex has an incoming relation reln</summary>
		/// <param name="vertex">A node in this graph</param>
		/// <param name="reln">The relation we want to check</param>
		/// <returns>true if vertex has an incoming relation reln</returns>
		public virtual bool HasParentWithReln(IndexedWord vertex, GrammaticalRelation reln)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			foreach (SemanticGraphEdge edge in IncomingEdgeIterable(vertex))
			{
				if (edge.GetRelation().Equals(reln))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns the first IndexedFeatureLabel bearing a certain grammatical
		/// relation, or null if none.
		/// </summary>
		public virtual IndexedWord GetChildWithReln(IndexedWord vertex, GrammaticalRelation reln)
		{
			if (vertex.Equals(IndexedWord.NoWord))
			{
				return null;
			}
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			foreach (SemanticGraphEdge edge in OutgoingEdgeIterable(vertex))
			{
				if (edge.GetRelation().Equals(reln))
				{
					return edge.GetTarget();
				}
			}
			return null;
		}

		/// <summary>
		/// Returns a set of all parents bearing a certain grammatical relation, or an
		/// empty set if none.
		/// </summary>
		public virtual ICollection<IndexedWord> GetParentsWithReln(IndexedWord vertex, GrammaticalRelation reln)
		{
			if (vertex.Equals(IndexedWord.NoWord))
			{
				return Java.Util.Collections.EmptySet();
			}
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			ICollection<IndexedWord> parentList = wordMapFactory.NewSet();
			foreach (SemanticGraphEdge edge in IncomingEdgeIterable(vertex))
			{
				if (edge.GetRelation().Equals(reln))
				{
					parentList.Add(edge.GetSource());
				}
			}
			return parentList;
		}

		/// <summary>
		/// Returns a set of all children bearing a certain grammatical relation, or
		/// an empty set if none.
		/// </summary>
		public virtual ICollection<IndexedWord> GetChildrenWithReln(IndexedWord vertex, GrammaticalRelation reln)
		{
			if (vertex.Equals(IndexedWord.NoWord))
			{
				return Java.Util.Collections.EmptySet();
			}
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			ICollection<IndexedWord> childList = wordMapFactory.NewSet();
			foreach (SemanticGraphEdge edge in OutgoingEdgeIterable(vertex))
			{
				if (edge.GetRelation().Equals(reln))
				{
					childList.Add(edge.GetTarget());
				}
			}
			return childList;
		}

		/// <summary>
		/// Returns a set of all children bearing one of a set of grammatical
		/// relations, or an empty set if none.
		/// </summary>
		/// <remarks>
		/// Returns a set of all children bearing one of a set of grammatical
		/// relations, or an empty set if none.
		/// NOTE: this will only work for relation types that are classes. Those that
		/// are collapsed are currently not handled correctly since they are identified
		/// by strings.
		/// </remarks>
		public virtual ICollection<IndexedWord> GetChildrenWithRelns(IndexedWord vertex, ICollection<GrammaticalRelation> relns)
		{
			if (vertex.Equals(IndexedWord.NoWord))
			{
				return Java.Util.Collections.EmptySet();
			}
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			ICollection<IndexedWord> childList = wordMapFactory.NewSet();
			foreach (SemanticGraphEdge edge in OutgoingEdgeIterable(vertex))
			{
				if (relns.Contains(edge.GetRelation()))
				{
					childList.Add(edge.GetTarget());
				}
			}
			return childList;
		}

		/// <summary>
		/// Given a governor, dependent, and the relation between them, returns the
		/// SemanticGraphEdge object of that arc if it exists, otherwise returns null.
		/// </summary>
		public virtual SemanticGraphEdge GetEdge(IndexedWord gov, IndexedWord dep, GrammaticalRelation reln)
		{
			ICollection<SemanticGraphEdge> edges = GetAllEdges(gov, dep);
			if (edges != null)
			{
				foreach (SemanticGraphEdge edge in edges)
				{
					if (!edge.GetSource().Equals(gov))
					{
						continue;
					}
					if ((edge.GetRelation().Equals(reln)))
					{
						return edge;
					}
				}
			}
			return null;
		}

		public virtual bool IsNegatedVertex(IndexedWord vertex)
		{
			if (vertex == IndexedWord.NoWord)
			{
				return false;
			}
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException("Vertex " + vertex + " not in graph " + this);
			}
			return (HasChildWithReln(vertex, EnglishGrammaticalRelations.NegationModifier) || HasChild(vertex, GrammaticalRelation.Dependent, "nor"));
		}

		private bool IsNegatedVerb(IndexedWord vertex)
		{
			if (!ContainsVertex(vertex))
			{
				throw new ArgumentException();
			}
			return (vertex.Tag().StartsWith("VB") && IsNegatedVertex(vertex));
		}

		/// <summary>Check if the vertex is in a "conditional" context.</summary>
		/// <remarks>
		/// Check if the vertex is in a "conditional" context. Right now it's only
		/// returning true if vertex has an "if" marker attached to it, i.e. the vertex
		/// is in a clause headed by "if".
		/// </remarks>
		public virtual bool IsInConditionalContext(IndexedWord vertex)
		{
			foreach (IndexedWord child in GetChildrenWithReln(vertex, EnglishGrammaticalRelations.Marker))
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(child.Word(), "if"))
				{
					return true;
				}
			}
			return false;
		}

		// Obsolete; use functions in rte.feat.NegPolarityFeaturizers instead
		public virtual bool AttachedNegatedVerb(IndexedWord vertex)
		{
			foreach (IndexedWord parent in GetParents(vertex))
			{
				if (IsNegatedVerb(parent))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns true iff this vertex stands in the "aux" relation to (any of)
		/// its parent(s).
		/// </summary>
		public virtual bool IsAuxiliaryVerb(IndexedWord vertex)
		{
			ICollection<GrammaticalRelation> relns = Relns(vertex);
			if (relns.IsEmpty())
			{
				return false;
			}
			bool result = relns.Contains(EnglishGrammaticalRelations.AuxModifier) || relns.Contains(EnglishGrammaticalRelations.AuxPassiveModifier);
			// log.info("I say " + vertex + (result ? " is" : " is not") +
			// " an aux");
			return result;
		}

		public virtual ICollection<IndexedWord> GetLeafVertices()
		{
			ICollection<IndexedWord> result = wordMapFactory.NewSet();
			foreach (IndexedWord v in VertexSet())
			{
				if (OutDegree(v) == 0)
				{
					result.Add(v);
				}
			}
			return result;
		}

		/// <summary>Returns the number of nodes in the graph</summary>
		public virtual int Size()
		{
			return this.VertexSet().Count;
		}

		/// <summary>
		/// Returns all nodes reachable from
		/// <paramref name="root"/>
		/// .
		/// </summary>
		/// <param name="root">the root node of the subgraph</param>
		/// <returns>all nodes in subgraph</returns>
		public virtual ICollection<IndexedWord> GetSubgraphVertices(IndexedWord root)
		{
			ICollection<IndexedWord> result = wordMapFactory.NewSet();
			result.Add(root);
			IList<IndexedWord> queue = Generics.NewLinkedList();
			queue.Add(root);
			while (!queue.IsEmpty())
			{
				IndexedWord current = queue.Remove(0);
				foreach (IndexedWord child in this.GetChildren(current))
				{
					if (!result.Contains(child))
					{
						result.Add(child);
						queue.Add(child);
					}
				}
			}
			return result;
		}

		/// <returns>true if the graph contains no cycles.</returns>
		public virtual bool IsDag()
		{
			ICollection<IndexedWord> unused = wordMapFactory.NewSet();
			Sharpen.Collections.AddAll(unused, VertexSet());
			while (!unused.IsEmpty())
			{
				IndexedWord arbitrary = unused.GetEnumerator().Current;
				bool result = IsDagHelper(arbitrary, unused, wordMapFactory.NewSet());
				if (result)
				{
					return false;
				}
			}
			return true;
		}

		/// <param name="root">root node of the subgraph.</param>
		/// <returns>
		/// true if the subgraph rooted at
		/// <paramref name="root"/>
		/// contains no cycles.
		/// </returns>
		public virtual bool IsDag(IndexedWord root)
		{
			ICollection<IndexedWord> unused = wordMapFactory.NewSet();
			Sharpen.Collections.AddAll(unused, this.GetSubgraphVertices(root));
			while (!unused.IsEmpty())
			{
				IndexedWord arbitrary = unused.GetEnumerator().Current;
				bool result = IsDagHelper(arbitrary, unused, wordMapFactory.NewSet());
				if (result)
				{
					return false;
				}
			}
			return true;
		}

		private bool IsDagHelper(IndexedWord current, ICollection<IndexedWord> unused, ICollection<IndexedWord> trail)
		{
			if (trail.Contains(current))
			{
				return true;
			}
			else
			{
				if (!unused.Contains(current))
				{
					return false;
				}
			}
			unused.Remove(current);
			trail.Add(current);
			foreach (IndexedWord child in GetChildren(current))
			{
				bool result = IsDagHelper(child, unused, trail);
				if (result)
				{
					return true;
				}
			}
			trail.Remove(current);
			return false;
		}

		// ============================================================================
		// String display
		// ============================================================================
		/// <summary>Recursive depth first traversal.</summary>
		/// <remarks>
		/// Recursive depth first traversal. Returns a structured representation of the
		/// dependency graph.
		/// Example:
		/// <pre>
		/// -&gt; need-3 (root)
		/// -&gt; We-0 (nsubj)
		/// -&gt; do-1 (aux)
		/// -&gt; n't-2 (neg)
		/// -&gt; badges-6 (dobj)
		/// -&gt; no-4 (det)
		/// -&gt; stinking-5 (amod)
		/// </pre>
		/// This is a quite ugly way to print a SemanticGraph.
		/// You might instead want to try
		/// <see cref="ToString(OutputFormat)"/>
		/// .
		/// </remarks>
		public override string ToString()
		{
			return ToString(CoreLabel.OutputFormat.ValueTag);
		}

		public virtual string ToString(CoreLabel.OutputFormat wordFormat)
		{
			ICollection<IndexedWord> rootNodes = GetRoots();
			if (rootNodes.IsEmpty())
			{
				// Shouldn't happen, but return something!
				return ToString(SemanticGraph.OutputFormat.Readable);
			}
			StringBuilder sb = new StringBuilder();
			ICollection<IndexedWord> used = wordMapFactory.NewSet();
			foreach (IndexedWord root in rootNodes)
			{
				sb.Append("-> ").Append(root.ToString(wordFormat)).Append(" (root)\n");
				RecToString(root, wordFormat, sb, 1, used);
			}
			ICollection<IndexedWord> nodes = wordMapFactory.NewSet();
			Sharpen.Collections.AddAll(nodes, VertexSet());
			nodes.RemoveAll(used);
			while (!nodes.IsEmpty())
			{
				IndexedWord node = nodes.GetEnumerator().Current;
				sb.Append(node.ToString(wordFormat)).Append("\n");
				RecToString(node, wordFormat, sb, 1, used);
				nodes.RemoveAll(used);
			}
			return sb.ToString();
		}

		// helper for toString()
		private void RecToString(IndexedWord curr, CoreLabel.OutputFormat wordFormat, StringBuilder sb, int offset, ICollection<IndexedWord> used)
		{
			used.Add(curr);
			IList<SemanticGraphEdge> edges = OutgoingEdgeList(curr);
			edges.Sort();
			foreach (SemanticGraphEdge edge in edges)
			{
				IndexedWord target = edge.GetTarget();
				sb.Append(Space(2 * offset)).Append("-> ").Append(target.ToString(wordFormat)).Append(" (").Append(edge.GetRelation()).Append(")\n");
				if (!used.Contains(target))
				{
					// recurse
					RecToString(target, wordFormat, sb, offset + 1, used);
				}
			}
		}

		private static string Space(int width)
		{
			StringBuilder b = new StringBuilder();
			for (int i = 0; i < width; i++)
			{
				b.Append(' ');
			}
			return b.ToString();
		}

		public virtual string ToRecoveredSentenceString()
		{
			StringBuilder sb = new StringBuilder();
			bool pastFirst = false;
			foreach (IndexedWord word in VertexListSorted())
			{
				if (pastFirst)
				{
					sb.Append(' ');
				}
				pastFirst = true;
				sb.Append(word.Word());
			}
			return sb.ToString();
		}

		public virtual string ToRecoveredSentenceStringWithIndexMarking()
		{
			StringBuilder sb = new StringBuilder();
			bool pastFirst = false;
			int index = 0;
			foreach (IndexedWord word in VertexListSorted())
			{
				if (pastFirst)
				{
					sb.Append(' ');
				}
				pastFirst = true;
				sb.Append(word.Word());
				sb.Append("(");
				sb.Append(index++);
				sb.Append(")");
			}
			return sb.ToString();
		}

		/// <summary>
		/// Similar to
		/// <c>toRecoveredString</c>
		/// , but will fill in words that were
		/// collapsed into relations (i.e. prep_for --&gt; 'for'). Mostly to deal with
		/// collapsed dependency trees.
		/// TODO: consider merging with toRecoveredString() NOTE: assumptions currently
		/// are for English. NOTE: currently takes immediate successors to current word
		/// and expands them. This assumption may not be valid for other conditions or
		/// languages?
		/// </summary>
		public virtual string ToEnUncollapsedSentenceString()
		{
			IList<IndexedWord> uncompressedList = Generics.NewLinkedList(VertexSet());
			IList<Pair<string, IndexedWord>> specifics = Generics.NewArrayList();
			// Collect the specific relations and the governed nodes, and then process
			// them one by one,
			// to avoid concurrent modification exceptions.
			foreach (IndexedWord word in VertexSet())
			{
				foreach (SemanticGraphEdge edge in GetIncomingEdgesSorted(word))
				{
					GrammaticalRelation relation = edge.GetRelation();
					// Extract the specific: need to account for possibility that relation
					// can
					// be a String or GrammaticalRelation (how did it happen this way?)
					string specific = relation.GetSpecific();
					if (specific == null)
					{
						if (edge.GetRelation().Equals(EnglishGrammaticalRelations.Agent))
						{
							specific = "by";
						}
					}
					// Insert the specific at the leftmost token that is not governed by
					// this node.
					if (specific != null)
					{
						Pair<string, IndexedWord> specPair = new Pair<string, IndexedWord>(specific, word);
						specifics.Add(specPair);
					}
				}
			}
			foreach (Pair<string, IndexedWord> tuple in specifics)
			{
				InsertSpecificIntoList(tuple.First(), tuple.Second(), uncompressedList);
			}
			return StringUtils.Join(uncompressedList, " ");
		}

		/// <summary>
		/// Inserts the given specific portion of an uncollapsed relation back into the
		/// targetList
		/// </summary>
		/// <param name="specific">Specific relation to put in.</param>
		/// <param name="relnTgtNode">Node governed by the uncollapsed relation</param>
		/// <param name="tgtList">Target List of words</param>
		private void InsertSpecificIntoList(string specific, IndexedWord relnTgtNode, IList<IndexedWord> tgtList)
		{
			int currIndex = tgtList.IndexOf(relnTgtNode);
			ICollection<IndexedWord> descendants = Descendants(relnTgtNode);
			IndexedWord specificNode = new IndexedWord();
			specificNode.Set(typeof(CoreAnnotations.LemmaAnnotation), specific);
			specificNode.Set(typeof(CoreAnnotations.TextAnnotation), specific);
			specificNode.Set(typeof(CoreAnnotations.OriginalTextAnnotation), specific);
			while ((currIndex >= 1) && descendants.Contains(tgtList[currIndex - 1]))
			{
				currIndex--;
			}
			tgtList.Add(currIndex, specificNode);
		}

		public enum OutputFormat
		{
			List,
			Xml,
			Readable,
			Recursive
		}

		/// <summary>
		/// Returns a String representation of the result of this set of typed
		/// dependencies in a user-specified format.
		/// </summary>
		/// <remarks>
		/// Returns a String representation of the result of this set of typed
		/// dependencies in a user-specified format. Currently, four formats are
		/// supported (
		/// <see cref="OutputFormat"/>
		/// ):
		/// <dl>
		/// <dt>list</dt>
		/// <dd>(Default.) Formats the dependencies as logical relations, as
		/// exemplified by the following:
		/// <pre>
		/// nsubj(died-1, Sam-0)
		/// tmod(died-1, today-2)
		/// </pre>
		/// </dd>
		/// <dt>readable</dt>
		/// <dd>Formats the dependencies as a table with columns
		/// <c>dependent</c>
		/// ,
		/// <c>relation</c>
		/// , and
		/// <c>governor</c>
		/// ,
		/// as exemplified by the following:
		/// <pre>
		/// Sam-0               nsubj               died-1
		/// today-2             tmod                died-1
		/// </pre>
		/// </dd>
		/// <dt>xml</dt>
		/// <dd>Formats the dependencies as XML, as exemplified by the following:
		/// <pre>
		/// &lt;dependencies&gt;
		/// &lt;dep type="nsubj"&gt;
		/// &lt;governor idx="1"&gt;died&lt;/governor&gt;
		/// &lt;dependent idx="0"&gt;Sam&lt;/dependent&gt;
		/// &lt;/dep&gt;
		/// &lt;dep type="tmod"&gt;
		/// &lt;governor idx="1"&gt;died&lt;/governor&gt;
		/// &lt;dependent idx="2"&gt;today&lt;/dependent&gt;
		/// &lt;/dep&gt;
		/// &lt;/dependencies&gt;
		/// </pre>
		/// </dd>
		/// <dt>recursive</dt>
		/// <dd>
		/// The default output for
		/// <see cref="ToString()"/>
		/// </dd>
		/// </dl>
		/// </remarks>
		/// <param name="format">
		/// A
		/// <c>String</c>
		/// specifying the desired format
		/// </param>
		/// <returns>
		/// A
		/// <c>String</c>
		/// representation of the typed dependencies in
		/// this
		/// <c>GrammaticalStructure</c>
		/// </returns>
		public virtual string ToString(SemanticGraph.OutputFormat format)
		{
			switch (format)
			{
				case SemanticGraph.OutputFormat.Xml:
				{
					return ToXMLString();
				}

				case SemanticGraph.OutputFormat.Readable:
				{
					return ToReadableString();
				}

				case SemanticGraph.OutputFormat.List:
				{
					return ToList();
				}

				case SemanticGraph.OutputFormat.Recursive:
				{
					return ToString();
				}

				default:
				{
					throw new ArgumentException("Unsupported format " + format);
				}
			}
		}

		/// <summary>
		/// Returns a String representation of this graph as a list of typed
		/// dependencies, as exemplified by the following:
		/// <pre>
		/// nsubj(died-6, Sam-3)
		/// tmod(died-6, today-9)
		/// </pre>
		/// </summary>
		/// <returns>
		/// a
		/// <c>String</c>
		/// representation of this set of typed dependencies
		/// </returns>
		public virtual string ToList()
		{
			StringBuilder buf = new StringBuilder();
			foreach (IndexedWord root in GetRoots())
			{
				buf.Append("root(ROOT-0, ");
				buf.Append(root.ToString(CoreLabel.OutputFormat.ValueIndex)).Append(")\n");
			}
			foreach (SemanticGraphEdge edge in this.EdgeListSorted())
			{
				buf.Append(edge.GetRelation()).Append("(");
				buf.Append(edge.GetSource().ToString(CoreLabel.OutputFormat.ValueIndex)).Append(", ");
				buf.Append(edge.GetTarget().ToString(CoreLabel.OutputFormat.ValueIndex)).Append(")\n");
			}
			return buf.ToString();
		}

		/// <summary>Similar to toList(), but uses POS tags instead of word and index.</summary>
		public virtual string ToPOSList()
		{
			StringBuilder buf = new StringBuilder();
			foreach (SemanticGraphEdge edge in this.EdgeListSorted())
			{
				buf.Append(edge.GetRelation()).Append("(");
				buf.Append(edge.GetSource()).Append(",");
				buf.Append(edge.GetTarget()).Append(")\n");
			}
			return buf.ToString();
		}

		private string ToReadableString()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append(string.Format("%-20s%-20s%-20s%n", "dep", "reln", "gov"));
			buf.Append(string.Format("%-20s%-20s%-20s%n", "---", "----", "---"));
			foreach (IndexedWord root in GetRoots())
			{
				buf.Append(string.Format("%-20s%-20s%-20s%n", root.ToString(CoreLabel.OutputFormat.ValueTagIndex), "root", "root"));
			}
			foreach (SemanticGraphEdge edge in this.EdgeListSorted())
			{
				buf.Append(string.Format("%-20s%-20s%-20s%n", edge.GetTarget().ToString(CoreLabel.OutputFormat.ValueTagIndex), edge.GetRelation().ToString(), edge.GetSource().ToString(CoreLabel.OutputFormat.ValueTagIndex)));
			}
			return buf.ToString();
		}

		private string ToXMLString()
		{
			StringBuilder buf = new StringBuilder("<dependencies style=\"typed\">\n");
			foreach (SemanticGraphEdge edge in this.EdgeListSorted())
			{
				string reln = edge.GetRelation().ToString();
				string gov = (edge.GetSource()).Word();
				int govIdx = (edge.GetSource()).Index();
				string dep = (edge.GetTarget()).Word();
				int depIdx = (edge.GetTarget()).Index();
				buf.Append("  <dep type=\"").Append(reln).Append("\">\n");
				buf.Append("    <governor idx=\"").Append(govIdx).Append("\">").Append(gov).Append("</governor>\n");
				buf.Append("    <dependent idx=\"").Append(depIdx).Append("\">").Append(dep).Append("</dependent>\n");
				buf.Append("  </dep>\n");
			}
			buf.Append("</dependencies>\n");
			return buf.ToString();
		}

		public virtual string ToCompactString()
		{
			return ToCompactString(false);
		}

		public virtual string ToCompactString(bool showTags)
		{
			StringBuilder sb = new StringBuilder();
			ICollection<IndexedWord> used = wordMapFactory.NewSet();
			ICollection<IndexedWord> roots = GetRoots();
			if (roots.IsEmpty())
			{
				if (Size() == 0)
				{
					return "[EMPTY_SEMANTIC_GRAPH]";
				}
				else
				{
					return "[UNROOTED_SEMANTIC_GRAPH]";
				}
			}
			// return toString("readable");
			foreach (IndexedWord root in roots)
			{
				ToCompactStringHelper(root, sb, used, showTags);
			}
			return sb.ToString();
		}

		private void ToCompactStringHelper(IndexedWord node, StringBuilder sb, ICollection<IndexedWord> used, bool showTags)
		{
			used.Add(node);
			try
			{
				bool isntLeaf = (OutDegree(node) > 0);
				if (isntLeaf)
				{
					sb.Append("[");
				}
				sb.Append(node.Word());
				if (showTags)
				{
					sb.Append("/");
					sb.Append(node.Tag());
				}
				foreach (SemanticGraphEdge edge in GetOutEdgesSorted(node))
				{
					IndexedWord target = edge.GetTarget();
					sb.Append(" ").Append(edge.GetRelation()).Append(">");
					if (!used.Contains(target))
					{
						// avoid infinite loop
						ToCompactStringHelper(target, sb, used, showTags);
					}
					else
					{
						sb.Append(target.Word());
						if (showTags)
						{
							sb.Append("/");
							sb.Append(target.Tag());
						}
					}
				}
				if (isntLeaf)
				{
					sb.Append("]");
				}
			}
			catch (ArgumentException e)
			{
				log.Info("WHOA!  SemanticGraph.toCompactStringHelper() ran into problems at node " + node);
				throw new ArgumentException(e);
			}
		}

		/// <summary>
		/// Returns a
		/// <c>String</c>
		/// representation of this semantic graph,
		/// formatted by the default semantic graph formatter.
		/// </summary>
		public virtual string ToFormattedString()
		{
			return formatter.FormatSemanticGraph(this);
		}

		/// <summary>
		/// Returns a
		/// <c>String</c>
		/// representation of this semantic graph,
		/// formatted by the supplied semantic graph formatter.
		/// </summary>
		public virtual string ToFormattedString(SemanticGraphFormatter formatter)
		{
			return formatter.FormatSemanticGraph(this);
		}

		/// <summary>
		/// Pretty-prints this semantic graph to
		/// <c>System.out</c>
		/// , formatted by
		/// the supplied semantic graph formatter.
		/// </summary>
		public virtual void PrettyPrint(SemanticGraphFormatter formatter)
		{
			System.Console.Out.WriteLine(formatter.FormatSemanticGraph(this));
		}

		/// <summary>
		/// Pretty-prints this semantic graph to
		/// <c>System.out</c>
		/// , formatted by
		/// the default semantic graph formatter.
		/// </summary>
		public virtual void PrettyPrint()
		{
			System.Console.Out.WriteLine(formatter.FormatSemanticGraph(this));
		}

		/// <summary>Returns an unnamed dot format digraph.</summary>
		/// <remarks>
		/// Returns an unnamed dot format digraph.
		/// Nodes will be labeled with the word and edges will be labeled
		/// with the dependency.
		/// </remarks>
		public virtual string ToDotFormat()
		{
			return ToDotFormat(string.Empty);
		}

		/// <summary>Returns a dot format digraph with the given name.</summary>
		/// <remarks>
		/// Returns a dot format digraph with the given name.
		/// Nodes will be labeled with the word and edges will be labeled
		/// with the dependency.
		/// </remarks>
		public virtual string ToDotFormat(string graphname)
		{
			return ToDotFormat(graphname, CoreLabel.OutputFormat.ValueTagIndex);
		}

		public virtual string ToDotFormat(string graphname, CoreLabel.OutputFormat indexedWordFormat)
		{
			StringBuilder output = new StringBuilder();
			output.Append("digraph " + graphname + " {\n");
			foreach (IndexedWord word in graph.GetAllVertices())
			{
				output.Append("  N_" + word.Index() + " [label=\"" + word.ToString(indexedWordFormat) + "\"];\n");
			}
			foreach (SemanticGraphEdge edge in graph.EdgeIterable())
			{
				output.Append("  N_" + edge.GetSource().Index() + " -> N_" + edge.GetTarget().Index() + " [label=\"" + edge.GetRelation() + "\"];\n");
			}
			output.Append("}\n");
			return output.ToString();
		}

		public virtual SemanticGraphEdge AddEdge(IndexedWord s, IndexedWord d, GrammaticalRelation reln, double weight, bool isExtra)
		{
			SemanticGraphEdge newEdge = new SemanticGraphEdge(s, d, reln, weight, isExtra);
			graph.Add(s, d, newEdge);
			return newEdge;
		}

		public virtual SemanticGraphEdge AddEdge(SemanticGraphEdge edge)
		{
			SemanticGraphEdge newEdge = new SemanticGraphEdge(edge.GetGovernor(), edge.GetDependent(), edge.GetRelation(), edge.GetWeight(), edge.IsExtra());
			graph.Add(edge.GetGovernor(), edge.GetDependent(), newEdge);
			return newEdge;
		}

		// =======================================================================
		/// <summary>Tries to parse a String representing a SemanticGraph.</summary>
		/// <remarks>
		/// Tries to parse a String representing a SemanticGraph. Right now it's fairly
		/// dumb, could be made more sophisticated.
		/// <p/>
		/// Example: "[ate subj&gt;Bill dobj&gt;[muffins compound&gt;blueberry]]"
		/// <p/>
		/// This is the same format generated by toCompactString().
		/// </remarks>
		public static Edu.Stanford.Nlp.Semgraph.SemanticGraph ValueOf(string s, Language language)
		{
			return (new SemanticGraph.SemanticGraphParsingTask(s, language)).Parse();
		}

		/// <seealso cref="ValueOf(string, Edu.Stanford.Nlp.International.Language)"/>
		public static Edu.Stanford.Nlp.Semgraph.SemanticGraph ValueOf(string s)
		{
			return ValueOf(s, Language.UniversalEnglish);
		}

		public SemanticGraph()
		{
			graph = new DirectedMultiGraph<IndexedWord, SemanticGraphEdge>(outerMapFactory, innerMapFactory);
			roots = wordMapFactory.NewSet();
		}

		/// <summary>Returns a new SemanticGraph which is a copy of the supplied SemanticGraph.</summary>
		/// <remarks>
		/// Returns a new SemanticGraph which is a copy of the supplied SemanticGraph.
		/// Both the nodes (
		/// <see cref="Edu.Stanford.Nlp.Ling.IndexedWord"/>
		/// s) and the edges (SemanticGraphEdges)
		/// are copied.
		/// </remarks>
		public SemanticGraph(Edu.Stanford.Nlp.Semgraph.SemanticGraph g)
		{
			graph = new DirectedMultiGraph<IndexedWord, SemanticGraphEdge>(g.graph);
			roots = wordMapFactory.NewSet(g.roots);
		}

		/// <summary>
		/// Copies a the current graph, but also sets the mapping from the old to new
		/// graph.
		/// </summary>
		public SemanticGraph(Edu.Stanford.Nlp.Semgraph.SemanticGraph g, IDictionary<IndexedWord, IndexedWord> prevToNewMap)
		{
			graph = new DirectedMultiGraph<IndexedWord, SemanticGraphEdge>(outerMapFactory, innerMapFactory);
			if (prevToNewMap == null)
			{
				prevToNewMap = wordMapFactory.NewMap();
			}
			ICollection<IndexedWord> vertexes = g.VertexSet();
			foreach (IndexedWord vertex in vertexes)
			{
				IndexedWord newVertex = new IndexedWord(vertex);
				newVertex.SetCopyCount(vertex.CopyCount());
				AddVertex(newVertex);
				prevToNewMap[vertex] = newVertex;
			}
			roots = wordMapFactory.NewSet();
			foreach (IndexedWord oldRoot in g.GetRoots())
			{
				roots.Add(prevToNewMap[oldRoot]);
			}
			foreach (SemanticGraphEdge edge in g.EdgeIterable())
			{
				IndexedWord newGov = prevToNewMap[edge.GetGovernor()];
				IndexedWord newDep = prevToNewMap[edge.GetDependent()];
				AddEdge(newGov, newDep, edge.GetRelation(), edge.GetWeight(), edge.IsExtra());
			}
		}

		/// <summary>This is the constructor used by the parser.</summary>
		public SemanticGraph(ICollection<TypedDependency> dependencies)
		{
			graph = new DirectedMultiGraph<IndexedWord, SemanticGraphEdge>(outerMapFactory, innerMapFactory);
			roots = wordMapFactory.NewSet();
			foreach (TypedDependency d in dependencies)
			{
				IndexedWord gov = d.Gov();
				IndexedWord dep = d.Dep();
				GrammaticalRelation reln = d.Reln();
				if (reln != GrammaticalRelation.Root)
				{
					// the root relation only points to the root: the governor is a fake node that we don't want to add in the graph
					// It is unnecessary to call addVertex, since addEdge will
					// implicitly add vertices if needed
					//addVertex(gov);
					//addVertex(dep);
					AddEdge(gov, dep, reln, double.NegativeInfinity, d.Extra());
				}
				else
				{
					//it's the root and we add it
					AddVertex(dep);
					roots.Add(dep);
				}
			}
		}

		// there used to be an if clause that filtered out the case of empty
		// dependencies. However, I could not understand (or replicate) the error
		// it alluded to, and it led to empty dependency graphs for very short
		// fragments,
		// which meant they were ignored by the RTE system. Changed. (pado)
		// See also SemanticGraphFactory.makeGraphFromTree().
		/// <summary>
		/// Returns the nodes in the shortest undirected path between two edges in the
		/// graph.
		/// </summary>
		/// <remarks>
		/// Returns the nodes in the shortest undirected path between two edges in the
		/// graph. if source == target, returns a singleton list
		/// </remarks>
		/// <param name="source">node</param>
		/// <param name="target">node</param>
		/// <returns>
		/// nodes along shortest undirected path from source to target, in
		/// order
		/// </returns>
		public virtual IList<IndexedWord> GetShortestUndirectedPathNodes(IndexedWord source, IndexedWord target)
		{
			return graph.GetShortestPath(source, target, false);
		}

		public virtual IList<SemanticGraphEdge> GetShortestUndirectedPathEdges(IndexedWord source, IndexedWord target)
		{
			return graph.GetShortestPathEdges(source, target, false);
		}

		/// <summary>Returns the shortest directed path between two edges in the graph.</summary>
		/// <param name="source">node</param>
		/// <param name="target">node</param>
		/// <returns>shortest directed path from source to target</returns>
		public virtual IList<IndexedWord> GetShortestDirectedPathNodes(IndexedWord source, IndexedWord target)
		{
			return graph.GetShortestPath(source, target, true);
		}

		public virtual IList<SemanticGraphEdge> GetShortestDirectedPathEdges(IndexedWord source, IndexedWord target)
		{
			return graph.GetShortestPathEdges(source, target, true);
		}

		public virtual Edu.Stanford.Nlp.Semgraph.SemanticGraph MakeSoftCopy()
		{
			Edu.Stanford.Nlp.Semgraph.SemanticGraph newSg = new Edu.Stanford.Nlp.Semgraph.SemanticGraph();
			if (!this.roots.IsEmpty())
			{
				newSg.SetRoot(this.GetFirstRoot());
			}
			foreach (SemanticGraphEdge edge in this.EdgeIterable())
			{
				newSg.AddEdge(edge.GetSource(), edge.GetTarget(), edge.GetRelation(), edge.GetWeight(), edge.IsExtra());
			}
			return newSg;
		}

		private static readonly Pattern WordAndIndexPattern = Pattern.Compile("([^-]+)-([0-9]+)");

		/// <summary>This nested class is a helper for valueOf().</summary>
		/// <remarks>
		/// This nested class is a helper for valueOf(). It represents the task of
		/// parsing a specific String representing a SemanticGraph.
		/// </remarks>
		private class SemanticGraphParsingTask : StringParsingTask<SemanticGraph>
		{
			private SemanticGraph sg;

			private ICollection<int> indexesUsed = Generics.NewHashSet();

			private Language language;

			public SemanticGraphParsingTask(string s)
				: this(s, Language.UniversalEnglish)
			{
			}

			public SemanticGraphParsingTask(string s, Language language)
				: base(s)
			{
				// ============================================================================
				this.language = language;
			}

			public override SemanticGraph Parse()
			{
				sg = new SemanticGraph();
				try
				{
					ReadWhiteSpace();
					if (!IsLeftBracket(Peek()))
					{
						return null;
					}
					ReadDep(null, null);
					return sg;
				}
				catch (StringParsingTask.ParserException e)
				{
					log.Info("SemanticGraphParser warning: " + e.Message);
					return null;
				}
			}

			private void ReadDep(IndexedWord gov, string reln)
			{
				ReadWhiteSpace();
				if (!IsLeftBracket(Peek()))
				{
					// it's a leaf
					string label = ReadName();
					IndexedWord dep = MakeVertex(label);
					sg.AddVertex(dep);
					if (gov == null)
					{
						sg.roots.Add(dep);
					}
					sg.AddEdge(gov, dep, GrammaticalRelation.ValueOf(this.language, reln), double.NegativeInfinity, false);
				}
				else
				{
					ReadLeftBracket();
					string label = ReadName();
					IndexedWord dep = MakeVertex(label);
					sg.AddVertex(dep);
					if (gov == null)
					{
						sg.roots.Add(dep);
					}
					if (gov != null && reln != null)
					{
						sg.AddEdge(gov, dep, GrammaticalRelation.ValueOf(this.language, reln), double.NegativeInfinity, false);
					}
					ReadWhiteSpace();
					while (!IsRightBracket(Peek()) && !isEOF)
					{
						reln = ReadName();
						ReadRelnSeparator();
						ReadDep(dep, reln);
						ReadWhiteSpace();
					}
					ReadRightBracket();
				}
			}

			private IndexedWord MakeVertex(string word)
			{
				int index;
				// initialized below
				Pair<string, int> wordAndIndex = ReadWordAndIndex(word);
				if (wordAndIndex != null)
				{
					word = wordAndIndex.First();
					index = wordAndIndex.Second();
				}
				else
				{
					index = GetNextFreeIndex();
				}
				indexesUsed.Add(index);
				// Note that, despite the use of indexesUsed and getNextFreeIndex(),
				// nothing is actually enforcing that no indexes are used twice. This
				// could occur if some words in the string representation being parsed
				// come with index markers and some do not.
				IndexedWord ifl = new IndexedWord(null, 0, index);
				// log.info("SemanticGraphParsingTask>>> word = " + word);
				// log.info("SemanticGraphParsingTask>>> index = " + index);
				// log.info("SemanticGraphParsingTask>>> indexesUsed = " +
				// indexesUsed);
				string[] wordAndTag = word.Split("/");
				ifl.Set(typeof(CoreAnnotations.TextAnnotation), wordAndTag[0]);
				ifl.Set(typeof(CoreAnnotations.ValueAnnotation), wordAndTag[0]);
				if (wordAndTag.Length > 1)
				{
					ifl.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), wordAndTag[1]);
				}
				return ifl;
			}

			private static Pair<string, int> ReadWordAndIndex(string word)
			{
				Matcher matcher = WordAndIndexPattern.Matcher(word);
				if (!matcher.Matches())
				{
					return null;
				}
				else
				{
					word = matcher.Group(1);
					int index = int.Parse(matcher.Group(2));
					return new Pair<string, int>(word, index);
				}
			}

			private int GetNextFreeIndex()
			{
				int i = 0;
				while (indexesUsed.Contains(i))
				{
					i++;
				}
				return i;
			}

			private void ReadLeftBracket()
			{
				// System.out.println("Read left.");
				ReadWhiteSpace();
				char ch = Read();
				if (!IsLeftBracket(ch))
				{
					throw new StringParsingTask.ParserException("Expected left paren!");
				}
			}

			private void ReadRightBracket()
			{
				// System.out.println("Read right.");
				ReadWhiteSpace();
				char ch = Read();
				if (!IsRightBracket(ch))
				{
					throw new StringParsingTask.ParserException("Expected right paren!");
				}
			}

			private void ReadRelnSeparator()
			{
				ReadWhiteSpace();
				if (IsRelnSeparator(Peek()))
				{
					Read();
				}
			}

			private static bool IsLeftBracket(char ch)
			{
				return ch == '[';
			}

			private static bool IsRightBracket(char ch)
			{
				return ch == ']';
			}

			private static bool IsRelnSeparator(char ch)
			{
				return ch == '>';
			}

			protected internal override bool IsPunct(char ch)
			{
				return IsLeftBracket(ch) || IsRightBracket(ch) || IsRelnSeparator(ch);
			}
		}

		// end SemanticGraphParsingTask
		// =======================================================================
		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (!(o is SemanticGraph))
			{
				return false;
			}
			SemanticGraph g = (SemanticGraph)o;
			return graph.Equals(g.graph) && roots.Equals(g.roots);
		}

		public override int GetHashCode()
		{
			return graph.GetHashCode();
		}

		/// <summary>
		/// Given a semantic graph, and a target relation, returns a list of all
		/// relations (edges) matching.
		/// </summary>
		public virtual IList<SemanticGraphEdge> FindAllRelns(GrammaticalRelation tgtRelation)
		{
			List<SemanticGraphEdge> relns = new List<SemanticGraphEdge>();
			foreach (SemanticGraphEdge edge in EdgeIterable())
			{
				GrammaticalRelation edgeRelation = edge.GetRelation();
				if ((edgeRelation != null) && (edgeRelation.Equals(tgtRelation)))
				{
					relns.Add(edge);
				}
			}
			return relns;
		}

		/// <summary>Delete all duplicate edges.</summary>
		public virtual void DeleteDuplicateEdges()
		{
			graph.DeleteDuplicateEdges();
		}

		/// <summary>Returns a list of TypedDependency in the graph.</summary>
		/// <remarks>
		/// Returns a list of TypedDependency in the graph.
		/// This method goes through all SemanticGraphEdge and converts them
		/// to TypedDependency.
		/// </remarks>
		/// <returns>A List of TypedDependency in the graph</returns>
		public virtual ICollection<TypedDependency> TypedDependencies()
		{
			ICollection<TypedDependency> dependencies = new List<TypedDependency>();
			IndexedWord root = null;
			foreach (IndexedWord node in roots)
			{
				if (root == null)
				{
					root = new IndexedWord(node.DocID(), node.SentIndex(), 0);
					root.SetValue("ROOT");
				}
				TypedDependency dependency = new TypedDependency(GrammaticalRelation.Root, root, node);
				dependencies.Add(dependency);
			}
			foreach (SemanticGraphEdge e in this.EdgeIterable())
			{
				TypedDependency dependency = new TypedDependency(e.GetRelation(), e.GetGovernor(), e.GetDependent());
				if (e.IsExtra())
				{
					dependency.SetExtra();
				}
				dependencies.Add(dependency);
			}
			return dependencies;
		}

		/// <summary>Returns the span of the subtree yield of this node.</summary>
		/// <remarks>
		/// Returns the span of the subtree yield of this node. That is, the span of all the nodes under it.
		/// In the case of projective graphs, the words in this span are also the yield of the constituent rooted
		/// at this node.
		/// </remarks>
		/// <param name="word">The word acting as the root of the constituent we are finding.</param>
		/// <returns>A span, represented as a pair of integers. The span is zero indexed. The begin is inclusive and the end is exclusive.</returns>
		public virtual Pair<int, int> YieldSpan(IndexedWord word)
		{
			int min = int.MaxValue;
			int max = int.MinValue;
			Stack<IndexedWord> fringe = new Stack<IndexedWord>();
			fringe.Push(word);
			while (!fringe.IsEmpty())
			{
				IndexedWord parent = fringe.Pop();
				min = Math.Min(min, parent.Index() - 1);
				max = Math.Max(max, parent.Index());
				foreach (SemanticGraphEdge edge in OutgoingEdgeIterable(parent))
				{
					if (!edge.IsExtra())
					{
						fringe.Push(edge.GetDependent());
					}
				}
			}
			return Pair.MakePair(min, max);
		}

		/// <summary>Store a comment line with this semantic graph.</summary>
		/// <param name="comment"/>
		public virtual void AddComment(string comment)
		{
			this.comments.Add(comment);
		}

		/// <summary>Return the list of comments stored with this graph.</summary>
		/// <returns>A list of comments.</returns>
		public virtual IList<string> GetComments()
		{
			return this.comments;
		}

		private const long serialVersionUID = 1L;
	}
}
