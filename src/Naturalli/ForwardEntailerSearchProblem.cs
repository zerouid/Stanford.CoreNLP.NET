using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>A particular instance of a search problem for finding entailed sentences.</summary>
	/// <remarks>
	/// A particular instance of a search problem for finding entailed sentences.
	/// This problem already specifies the options for the search, as well as the sentence to search from.
	/// Note, again, that this only searches for deletions and not insertions or mutations.
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class ForwardEntailerSearchProblem
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Naturalli.ForwardEntailerSearchProblem));

		/// <summary>The parse of this fragment.</summary>
		/// <remarks>
		/// The parse of this fragment. The vertices in the parse tree should be a subset
		/// (possibly not strict) of the tokens above.
		/// </remarks>
		public readonly SemanticGraph parseTree;

		/// <summary>The truth of the premise -- determines the direction we can mutate the sentences.</summary>
		public readonly bool truthOfPremise;

		/// <summary>The maximum number of ticks top search for.</summary>
		/// <remarks>The maximum number of ticks top search for. Otherwise, the search will be exhaustive.</remarks>
		public readonly int maxTicks;

		/// <summary>The maximum number of results to return from a single search.</summary>
		public readonly int maxResults;

		/// <summary>The weights to use for entailment.</summary>
		public readonly NaturalLogicWeights weights;

		/// <summary>A result from the search over possible shortenings of the sentence.</summary>
		private class SearchResult
		{
			public readonly SemanticGraph tree;

			public readonly IList<string> deletedEdges;

			public readonly double confidence;

			private SearchResult(SemanticGraph tree, IList<string> deletedEdges, double confidence)
			{
				this.tree = tree;
				this.deletedEdges = deletedEdges;
				this.confidence = confidence;
			}

			public override string ToString()
			{
				return StringUtils.Join(tree.VertexListSorted().Stream().Map(null), " ");
			}
		}

		/// <summary>A state in the search, denoting a partial shortening of the sentence.</summary>
		private class SearchState
		{
			public readonly BitSet deletionMask;

			public readonly int currentIndex;

			public readonly SemanticGraph tree;

			public readonly string lastDeletedEdge;

			public readonly ForwardEntailerSearchProblem.SearchState source;

			public readonly double score;

			private SearchState(BitSet deletionMask, int currentIndex, SemanticGraph tree, string lastDeletedEdge, ForwardEntailerSearchProblem.SearchState source, double score)
			{
				this.deletionMask = (BitSet)deletionMask.Clone();
				this.currentIndex = currentIndex;
				this.tree = tree;
				this.lastDeletedEdge = lastDeletedEdge;
				this.source = source;
				this.score = score;
			}
		}

		/// <summary>Create a new search problem, fully specified.</summary>
		/// <seealso cref="ForwardEntailer"/>
		protected internal ForwardEntailerSearchProblem(SemanticGraph parseTree, bool truthOfPremise, int maxResults, int maxTicks, NaturalLogicWeights weights)
		{
			this.parseTree = parseTree;
			this.truthOfPremise = truthOfPremise;
			this.maxResults = maxResults;
			this.maxTicks = maxTicks;
			this.weights = weights;
		}

		/// <summary>Run a search from this entailer.</summary>
		/// <remarks>
		/// Run a search from this entailer. This will return a list of sentence fragments
		/// that are entailed by the original sentence / fragment.
		/// </remarks>
		/// <returns>A list of entailed fragments.</returns>
		public virtual IList<SentenceFragment> Search()
		{
			return SearchImplementation().Stream().Map(null).Filter(null).Collect(Collectors.ToList());
		}

		/// <summary>The search algorithm, starting with a full sentence and iteratively shortening it to its entailed sentences.</summary>
		/// <returns>A list of search results, corresponding to shortenings of the sentence.</returns>
		private IList<ForwardEntailerSearchProblem.SearchResult> SearchImplementation()
		{
			// Pre-process the tree
			SemanticGraph parseTree = new SemanticGraph(this.parseTree);
			System.Diagnostics.Debug.Assert(Edu.Stanford.Nlp.Naturalli.Util.IsTree(parseTree));
			// (remove common determiners)
			IList<string> determinerRemovals = new List<string>();
			parseTree.GetLeafVertices().Stream().Filter(null).ForEach(null);
			// (cut conj_and nodes)
			ICollection<SemanticGraphEdge> andsToAdd = new HashSet<SemanticGraphEdge>();
			foreach (IndexedWord vertex in parseTree.VertexSet())
			{
				if (parseTree.InDegree(vertex) > 1)
				{
					SemanticGraphEdge conjAnd = null;
					foreach (SemanticGraphEdge edge in parseTree.IncomingEdgeIterable(vertex))
					{
						if ("conj:and".Equals(edge.GetRelation().ToString()))
						{
							conjAnd = edge;
						}
					}
					if (conjAnd != null)
					{
						parseTree.RemoveEdge(conjAnd);
						System.Diagnostics.Debug.Assert(Edu.Stanford.Nlp.Naturalli.Util.IsTree(parseTree));
						andsToAdd.Add(conjAnd);
					}
				}
			}
			// Clean the tree
			Edu.Stanford.Nlp.Naturalli.Util.CleanTree(parseTree);
			System.Diagnostics.Debug.Assert(Edu.Stanford.Nlp.Naturalli.Util.IsTree(parseTree));
			// Find the subject / object split
			// This takes max O(n^2) time, expected O(n*log(n)) time.
			// Optimal is O(n), but I'm too lazy to implement it.
			BitSet isSubject = new BitSet(256);
			foreach (IndexedWord vertex_1 in parseTree.VertexSet())
			{
				// Search up the tree for a subj node; if found, mark that vertex as a subject.
				IEnumerator<SemanticGraphEdge> incomingEdges = parseTree.IncomingEdgeIterator(vertex_1);
				SemanticGraphEdge edge = null;
				if (incomingEdges.MoveNext())
				{
					edge = incomingEdges.Current;
				}
				int numIters = 0;
				while (edge != null)
				{
					if (edge.GetRelation().ToString().EndsWith("subj"))
					{
						System.Diagnostics.Debug.Assert(vertex_1.Index() > 0);
						isSubject.Set(vertex_1.Index() - 1);
						break;
					}
					incomingEdges = parseTree.IncomingEdgeIterator(edge.GetGovernor());
					if (incomingEdges.MoveNext())
					{
						edge = incomingEdges.Current;
					}
					else
					{
						edge = null;
					}
					numIters += 1;
					if (numIters > 100)
					{
						//          log.error("tree has apparent depth > 100");
						return Java.Util.Collections.EmptyList;
					}
				}
			}
			// Outputs
			IList<ForwardEntailerSearchProblem.SearchResult> results = new List<ForwardEntailerSearchProblem.SearchResult>();
			if (!determinerRemovals.IsEmpty())
			{
				if (andsToAdd.IsEmpty())
				{
					double score = Math.Pow(weights.DeletionProbability("det"), (double)determinerRemovals.Count);
					System.Diagnostics.Debug.Assert(!double.IsNaN(score));
					System.Diagnostics.Debug.Assert(!double.IsInfinite(score));
					results.Add(new ForwardEntailerSearchProblem.SearchResult(parseTree, determinerRemovals, score));
				}
				else
				{
					SemanticGraph treeWithAnds = new SemanticGraph(parseTree);
					System.Diagnostics.Debug.Assert(Edu.Stanford.Nlp.Naturalli.Util.IsTree(treeWithAnds));
					foreach (SemanticGraphEdge and in andsToAdd)
					{
						treeWithAnds.AddEdge(and.GetGovernor(), and.GetDependent(), and.GetRelation(), double.NegativeInfinity, false);
					}
					System.Diagnostics.Debug.Assert(Edu.Stanford.Nlp.Naturalli.Util.IsTree(treeWithAnds));
					results.Add(new ForwardEntailerSearchProblem.SearchResult(treeWithAnds, determinerRemovals, Math.Pow(weights.DeletionProbability("det"), (double)determinerRemovals.Count)));
				}
			}
			// Initialize the search
			System.Diagnostics.Debug.Assert(Edu.Stanford.Nlp.Naturalli.Util.IsTree(parseTree));
			IList<IndexedWord> topologicalVertices;
			try
			{
				topologicalVertices = parseTree.TopologicalSort();
			}
			catch (InvalidOperationException)
			{
				//      log.info("Could not topologically sort the vertices! Using left-to-right traversal.");
				topologicalVertices = parseTree.VertexListSorted();
			}
			if (topologicalVertices.IsEmpty())
			{
				return results;
			}
			Stack<ForwardEntailerSearchProblem.SearchState> fringe = new Stack<ForwardEntailerSearchProblem.SearchState>();
			fringe.Push(new ForwardEntailerSearchProblem.SearchState(new BitSet(256), 0, parseTree, null, null, 1.0));
			// Start the search
			int numTicks = 0;
			while (!fringe.IsEmpty())
			{
				// Overhead with popping a node.
				if (numTicks >= maxTicks)
				{
					return results;
				}
				numTicks += 1;
				if (results.Count >= maxResults)
				{
					return results;
				}
				ForwardEntailerSearchProblem.SearchState state = fringe.Pop();
				System.Diagnostics.Debug.Assert(state.score > 0.0);
				IndexedWord currentWord = topologicalVertices[state.currentIndex];
				// Push the case where we don't delete
				int nextIndex = state.currentIndex + 1;
				int numIters = 0;
				while (nextIndex < topologicalVertices.Count)
				{
					IndexedWord nextWord = topologicalVertices[nextIndex];
					System.Diagnostics.Debug.Assert(nextWord.Index() > 0);
					if (!state.deletionMask.Get(nextWord.Index() - 1))
					{
						fringe.Push(new ForwardEntailerSearchProblem.SearchState(state.deletionMask, nextIndex, state.tree, null, state, state.score));
						break;
					}
					else
					{
						nextIndex += 1;
					}
					numIters += 1;
					if (numIters > 10000)
					{
						//          log.error("logic error (apparent infinite loop); returning");
						return results;
					}
				}
				// Check if we can delete this subtree
				bool canDelete = !state.tree.GetFirstRoot().Equals(currentWord);
				foreach (SemanticGraphEdge edge in state.tree.IncomingEdgeIterable(currentWord))
				{
					if ("CD".Equals(edge.GetGovernor().Tag()))
					{
						canDelete = false;
					}
					else
					{
						// Get token information
						CoreLabel token = edge.GetDependent().BackingLabel();
						OperatorSpec @operator;
						NaturalLogicRelation lexicalRelation;
						Polarity tokenPolarity = token.Get(typeof(NaturalLogicAnnotations.PolarityAnnotation));
						if (tokenPolarity == null)
						{
							tokenPolarity = Polarity.Default;
						}
						// Get the relation for this deletion
						if ((@operator = token.Get(typeof(NaturalLogicAnnotations.OperatorAnnotation))) != null)
						{
							lexicalRelation = @operator.instance.deleteRelation;
						}
						else
						{
							System.Diagnostics.Debug.Assert(edge.GetDependent().Index() > 0);
							lexicalRelation = NaturalLogicRelation.ForDependencyDeletion(edge.GetRelation().ToString(), isSubject.Get(edge.GetDependent().Index() - 1));
						}
						NaturalLogicRelation projectedRelation = tokenPolarity.ProjectLexicalRelation(lexicalRelation);
						// Make sure this is a valid entailment
						if (!projectedRelation.ApplyToTruthValue(truthOfPremise).IsTrue())
						{
							canDelete = false;
						}
					}
				}
				if (canDelete)
				{
					// Register the deletion
					Lazy<Pair<SemanticGraph, BitSet>> treeWithDeletionsAndNewMask = Lazy.Of(null);
					// Compute the score of the sentence
					double newScore = state.score;
					foreach (SemanticGraphEdge edge_1 in state.tree.IncomingEdgeIterable(currentWord))
					{
						double multiplier = weights.DeletionProbability(edge_1, state.tree.OutgoingEdgeIterable(edge_1.GetGovernor()));
						System.Diagnostics.Debug.Assert(!double.IsNaN(multiplier));
						System.Diagnostics.Debug.Assert(!double.IsInfinite(multiplier));
						newScore *= multiplier;
					}
					// Register the result
					if (newScore > 0.0)
					{
						SemanticGraph resultTree = new SemanticGraph(treeWithDeletionsAndNewMask.Get().first);
						andsToAdd.Stream().Filter(null).ForEach(null);
						results.Add(new ForwardEntailerSearchProblem.SearchResult(resultTree, AggregateDeletedEdges(state, state.tree.IncomingEdgeIterable(currentWord), determinerRemovals), newScore));
						// Push the state with this subtree deleted
						nextIndex = state.currentIndex + 1;
						numIters = 0;
						while (nextIndex < topologicalVertices.Count)
						{
							IndexedWord nextWord = topologicalVertices[nextIndex];
							BitSet newMask = treeWithDeletionsAndNewMask.Get().second;
							SemanticGraph treeWithDeletions = treeWithDeletionsAndNewMask.Get().first;
							if (!newMask.Get(nextWord.Index() - 1))
							{
								System.Diagnostics.Debug.Assert(treeWithDeletions.ContainsVertex(topologicalVertices[nextIndex]));
								fringe.Push(new ForwardEntailerSearchProblem.SearchState(newMask, nextIndex, treeWithDeletions, null, state, newScore));
								break;
							}
							else
							{
								nextIndex += 1;
							}
							numIters += 1;
							if (numIters > 10000)
							{
								//              log.error("logic error (apparent infinite loop); returning");
								return results;
							}
						}
					}
				}
			}
			// Return
			return results;
		}

		/// <summary>Backtrace from a search state, collecting all of the deleted edges used to get there.</summary>
		/// <param name="state">The final search state.</param>
		/// <param name="justDeleted">The edges we have just deleted.</param>
		/// <param name="otherEdges">Other deletions we want to register</param>
		/// <returns>A list of deleted edges for that search state.</returns>
		private static IList<string> AggregateDeletedEdges(ForwardEntailerSearchProblem.SearchState state, IEnumerable<SemanticGraphEdge> justDeleted, IEnumerable<string> otherEdges)
		{
			IList<string> rtn = new List<string>();
			foreach (SemanticGraphEdge edge in justDeleted)
			{
				rtn.Add(edge.GetRelation().ToString());
			}
			foreach (string edge_1 in otherEdges)
			{
				rtn.Add(edge_1);
			}
			while (state != null)
			{
				if (state.lastDeletedEdge != null)
				{
					rtn.Add(state.lastDeletedEdge);
				}
				state = state.source;
			}
			return rtn;
		}
	}
}
