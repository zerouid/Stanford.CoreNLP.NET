using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <summary>
	/// A
	/// <c>SemgrexMatcher</c>
	/// can be used to match a
	/// <see cref="SemgrexPattern"/>
	/// against a
	/// <see cref="Edu.Stanford.Nlp.Semgraph.SemanticGraph"/>
	/// .
	/// <p>
	/// Usage should be the same as
	/// <see cref="Java.Util.Regex.Matcher"/>
	/// .
	/// </summary>
	/// <author>Chloe Kiddon</author>
	public abstract class SemgrexMatcher
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexMatcher));

		internal readonly SemanticGraph sg;

		internal readonly IDictionary<string, IndexedWord> namesToNodes;

		internal readonly IDictionary<string, string> namesToRelations;

		internal readonly VariableStrings variableStrings;

		internal IndexedWord node;

		internal readonly Alignment alignment;

		internal readonly SemanticGraph sg_aligned;

		internal readonly bool hyp;

		private IEnumerator<IndexedWord> findIterator;

		private IndexedWord findCurrent;

		internal SemgrexMatcher(SemanticGraph sg, Alignment alignment, SemanticGraph sg_aligned, bool hyp, IndexedWord node, IDictionary<string, IndexedWord> namesToNodes, IDictionary<string, string> namesToRelations, VariableStrings variableStrings
			)
		{
			// to be used for patterns involving "@"
			// these things are used by "find"
			this.sg = sg;
			this.alignment = alignment;
			this.sg_aligned = sg_aligned;
			this.hyp = hyp;
			this.node = node;
			this.namesToNodes = namesToNodes;
			this.namesToRelations = namesToRelations;
			this.variableStrings = variableStrings;
		}

		internal SemgrexMatcher(SemanticGraph sg, IndexedWord node, IDictionary<string, IndexedWord> namesToNodes, IDictionary<string, string> namesToRelations, VariableStrings variableStrings)
			: this(sg, null, null, true, node, namesToNodes, namesToRelations, variableStrings)
		{
		}

		/// <summary>Resets the matcher so that its search starts over.</summary>
		public virtual void Reset()
		{
			findIterator = null;
			namesToNodes.Clear();
			namesToRelations.Clear();
		}

		/// <summary>
		/// Resets the matcher to start searching on the given node for matching
		/// subexpressions.
		/// </summary>
		internal virtual void ResetChildIter(IndexedWord node)
		{
			this.node = node;
			ResetChildIter();
		}

		/// <summary>Resets the matcher to restart search for matching subexpressions</summary>
		internal virtual void ResetChildIter()
		{
		}

		/// <summary>
		/// Does the pattern match the graph?  It's actually closer to
		/// java.util.regex's "lookingAt" in that the root of the graph has to match
		/// the root of the pattern but the whole tree does not have to be "accounted
		/// for".
		/// </summary>
		/// <remarks>
		/// Does the pattern match the graph?  It's actually closer to
		/// java.util.regex's "lookingAt" in that the root of the graph has to match
		/// the root of the pattern but the whole tree does not have to be "accounted
		/// for".  Like with lookingAt the beginning of the string has to match the
		/// pattern, but the whole string doesn't have to be "accounted for".
		/// </remarks>
		/// <returns>whether the node matches the pattern</returns>
		public abstract bool Matches();

		/// <summary>
		/// Rests the matcher and tests if it matches in the graph when rooted at
		/// <paramref name="node"/>
		/// .
		/// </summary>
		/// <returns>whether the matcher matches at node</returns>
		public virtual bool MatchesAt(IndexedWord node)
		{
			ResetChildIter(node);
			return Matches();
		}

		/// <summary>
		/// Get the last matching node -- that is, the node that matches the root node
		/// of the pattern.
		/// </summary>
		/// <remarks>
		/// Get the last matching node -- that is, the node that matches the root node
		/// of the pattern.  Returns null if there has not been a match.
		/// </remarks>
		/// <returns>last match</returns>
		public abstract IndexedWord GetMatch();

		/// <summary>
		/// Topological sorting actually takes a rather large amount of time, if you call multiple
		/// patterns on the same tree.
		/// </summary>
		/// <remarks>
		/// Topological sorting actually takes a rather large amount of time, if you call multiple
		/// patterns on the same tree.
		/// This is a weak cache that stores all the trees sorted since the garbage collector last kicked in.
		/// The key on this map is the identity hash code (i.e., memory address) of the semantic graph; the
		/// value is the sorted list of vertices.
		/// <p>
		/// Note that this optimization will cause strange things to happen if you mutate a semantic graph between
		/// calls to Semgrex.
		/// </remarks>
		private static readonly WeakHashMap<int, IList<IndexedWord>> topologicalSortCache = new WeakHashMap<int, IList<IndexedWord>>();

		private void SetupFindIterator()
		{
			try
			{
				if (hyp)
				{
					lock (topologicalSortCache)
					{
						IList<IndexedWord> topoSort = topologicalSortCache[Runtime.IdentityHashCode(sg)];
						if (topoSort == null || topoSort.Count != sg.Size())
						{
							// size check to mitigate a stale cache
							topoSort = sg.TopologicalSort();
							topologicalSortCache[Runtime.IdentityHashCode(sg)] = topoSort;
						}
						findIterator = topoSort.GetEnumerator();
					}
				}
				else
				{
					if (sg_aligned == null)
					{
						return;
					}
					else
					{
						lock (topologicalSortCache)
						{
							IList<IndexedWord> topoSort = topologicalSortCache[Runtime.IdentityHashCode(sg_aligned)];
							if (topoSort == null || topoSort.Count != sg_aligned.Size())
							{
								// size check to mitigate a stale cache
								topoSort = sg_aligned.TopologicalSort();
								topologicalSortCache[Runtime.IdentityHashCode(sg_aligned)] = topoSort;
							}
							findIterator = topoSort.GetEnumerator();
						}
					}
				}
			}
			catch (Exception)
			{
				if (hyp)
				{
					findIterator = sg.VertexSet().GetEnumerator();
				}
				else
				{
					if (sg_aligned == null)
					{
						return;
					}
					else
					{
						findIterator = sg_aligned.VertexSet().GetEnumerator();
					}
				}
			}
		}

		/// <summary>Find the next match of the pattern in the graph.</summary>
		/// <returns>whether there is a match somewhere in the graph</returns>
		public virtual bool Find()
		{
			// log.info("hyp: " + hyp);
			if (findIterator == null)
			{
				SetupFindIterator();
			}
			if (findIterator == null)
			{
				return false;
			}
			//  System.out.println("first");
			if (findCurrent != null && Matches())
			{
				//		log.info("find first: " + findCurrent.word());
				return true;
			}
			//log.info("here");
			while (findIterator.MoveNext())
			{
				findCurrent = findIterator.Current;
				// System.out.println("final: " + namesToNodes);
				ResetChildIter(findCurrent);
				// System.out.println("after reset: " + namesToNodes);
				// Should not be necessary to reset namesToNodes here, since it
				// gets cleaned up by resetChildIter
				//namesToNodes.clear();
				//namesToRelations.clear();
				if (Matches())
				{
					//  log.info("find second: " + findCurrent.word());
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Find the next match of the pattern in the graph such that the matching node
		/// (that is, the node matching the root node of the pattern) differs from the
		/// previous matching node.
		/// </summary>
		/// <returns>true iff another matching node is found.</returns>
		public virtual bool FindNextMatchingNode()
		{
			IndexedWord lastMatchingNode = GetMatch();
			while (Find())
			{
				if (GetMatch() != lastMatchingNode)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns the node labeled with
		/// <paramref name="name"/>
		/// in the pattern.
		/// </summary>
		/// <param name="name">the name of the node, specified in the pattern.</param>
		/// <returns>node labeled by the name</returns>
		public virtual IndexedWord GetNode(string name)
		{
			return namesToNodes[name];
		}

		public virtual string GetRelnString(string name)
		{
			return namesToRelations[name];
		}

		/// <summary>Returns the set of names for named nodes in this pattern.</summary>
		/// <remarks>
		/// Returns the set of names for named nodes in this pattern.
		/// This is used as a convenience routine, when there are numerous patterns
		/// with named nodes to track.
		/// </remarks>
		public virtual ICollection<string> GetNodeNames()
		{
			return namesToNodes.Keys;
		}

		/// <summary>Returns the set of names for named relations in this pattern.</summary>
		public virtual ICollection<string> GetRelationNames()
		{
			return namesToRelations.Keys;
		}

		public abstract override string ToString();

		/// <summary>Returns the graph associated with this match.</summary>
		public virtual SemanticGraph GetGraph()
		{
			return sg;
		}
	}
}
