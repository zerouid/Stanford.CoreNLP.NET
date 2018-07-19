using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Text;
using Java.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>TODO(gabor) JavaDoc</summary>
	/// <author>Gabor Angeli</author>
	public class Util
	{
		/// <summary>TODO(gabor) JavaDoc</summary>
		/// <param name="tokens"/>
		/// <param name="span"/>
		/// <returns/>
		public static string GuessNER(IList<CoreLabel> tokens, Span span)
		{
			ICounter<string> nerGuesses = new ClassicCounter<string>();
			foreach (int i in span)
			{
				nerGuesses.IncrementCount(tokens[i].Ner());
			}
			nerGuesses.Remove("O");
			nerGuesses.Remove(null);
			if (nerGuesses.Size() > 0 && Counters.Max(nerGuesses) >= span.Size() / 2)
			{
				return Counters.Argmax(nerGuesses);
			}
			else
			{
				return "O";
			}
		}

		/// <summary>TODO(gabor) JavaDoc</summary>
		/// <param name="tokens"/>
		/// <returns/>
		public static string GuessNER(IList<CoreLabel> tokens)
		{
			return GuessNER(tokens, new Span(0, tokens.Count));
		}

		/// <summary>Returns a coherent NER span from a list of tokens.</summary>
		/// <param name="tokens">The tokens of the entire sentence.</param>
		/// <param name="seed">The seed span of the intended NER span that should be expanded.</param>
		/// <returns>A 0 indexed span corresponding to a coherent NER chunk from the given seed.</returns>
		public static Span ExtractNER(IList<CoreLabel> tokens, Span seed)
		{
			// Error checks
			if (seed == null)
			{
				return new Span(0, 1);
			}
			if (seed.Start() < 0 || seed.End() < 0)
			{
				return new Span(0, 0);
			}
			if (seed.Start() >= tokens.Count || seed.End() > tokens.Count)
			{
				return new Span(tokens.Count, tokens.Count);
			}
			if (tokens[seed.Start()].Ner() == null)
			{
				return seed;
			}
			if (seed.Start() < 0 || seed.End() > tokens.Count)
			{
				return Span.FromValues(Math.Max(0, seed.Start()), Math.Min(tokens.Count, seed.End()));
			}
			// Find the span's beginning
			int begin = seed.Start();
			while (begin < seed.End() - 1 && "O".Equals(tokens[begin].Ner()))
			{
				begin += 1;
			}
			string beginNER = tokens[begin].Ner();
			if (!"O".Equals(beginNER))
			{
				while (begin > 0 && tokens[begin - 1].Ner().Equals(beginNER))
				{
					begin -= 1;
				}
			}
			else
			{
				begin = seed.Start();
			}
			// Find the span's end
			int end = seed.End() - 1;
			while (end > begin && "O".Equals(tokens[end].Ner()))
			{
				end -= 1;
			}
			string endNER = tokens[end].Ner();
			if (!"O".Equals(endNER))
			{
				while (end < tokens.Count - 1 && tokens[end + 1].Ner().Equals(endNER))
				{
					end += 1;
				}
			}
			else
			{
				end = seed.End() - 1;
			}
			// Check that the NER of the beginning and end are the same
			if (beginNER.Equals(endNER))
			{
				return Span.FromValues(begin, end + 1);
			}
			else
			{
				string bestNER = GuessNER(tokens, Span.FromValues(begin, end + 1));
				if (beginNER.Equals(bestNER))
				{
					return ExtractNER(tokens, Span.FromValues(begin, begin + 1));
				}
				else
				{
					if (endNER.Equals(bestNER))
					{
						return ExtractNER(tokens, Span.FromValues(end, end + 1));
					}
					else
					{
						// Something super funky is going on...
						return Span.FromValues(begin, end + 1);
					}
				}
			}
		}

		/// <summary>TODO(gabor) JavaDoc</summary>
		/// <param name="sentence"/>
		/// <param name="pipeline"/>
		public static void Annotate(ICoreMap sentence, AnnotationPipeline pipeline)
		{
			Annotation ann = new Annotation(StringUtils.Join(sentence.Get(typeof(CoreAnnotations.TokensAnnotation)), " "));
			ann.Set(typeof(CoreAnnotations.TokensAnnotation), sentence.Get(typeof(CoreAnnotations.TokensAnnotation)));
			ann.Set(typeof(CoreAnnotations.SentencesAnnotation), Java.Util.Collections.SingletonList(sentence));
			pipeline.Annotate(ann);
		}

		/// <summary>Fix some bizarre peculiarities with certain trees.</summary>
		/// <remarks>
		/// Fix some bizarre peculiarities with certain trees.
		/// So far, these include:
		/// <ul>
		/// <li>Sometimes there's a node from a word to itself. This seems wrong.</li>
		/// </ul>
		/// </remarks>
		/// <param name="tree">The tree to clean (in place!).</param>
		/// <returns>A list of extra edges, which are valid but were removed.</returns>
		public static IList<SemanticGraphEdge> CleanTree(SemanticGraph tree)
		{
			//    assert !isCyclic(tree);
			// Clean nodes
			IList<IndexedWord> toDelete = new List<IndexedWord>();
			foreach (IndexedWord vertex in tree.VertexSet())
			{
				// Clean punctuation
				if (vertex.Tag() == null)
				{
					continue;
				}
				char tag = vertex.BackingLabel().Tag()[0];
				if (tag == '.' || tag == ',' || tag == '(' || tag == ')' || tag == ':')
				{
					if (!tree.OutgoingEdgeIterator(vertex).MoveNext())
					{
						// This should really never happen, but it does.
						toDelete.Add(vertex);
					}
				}
			}
			toDelete.ForEach(null);
			// Clean edges
			IEnumerator<SemanticGraphEdge> iter = tree.EdgeIterable().GetEnumerator();
			IList<Triple<IndexedWord, IndexedWord, SemanticGraphEdge>> toAdd = new List<Triple<IndexedWord, IndexedWord, SemanticGraphEdge>>();
			toDelete.Clear();
			while (iter.MoveNext())
			{
				SemanticGraphEdge edge = iter.Current;
				if (edge.GetDependent().Index() == edge.GetGovernor().Index())
				{
					// Clean up copy-edges
					if (edge.GetDependent().IsCopy(edge.GetGovernor()))
					{
						foreach (SemanticGraphEdge toCopy in tree.OutgoingEdgeIterable(edge.GetDependent()))
						{
							toAdd.Add(Triple.MakeTriple(edge.GetGovernor(), toCopy.GetDependent(), toCopy));
						}
						toDelete.Add(edge.GetDependent());
					}
					if (edge.GetGovernor().IsCopy(edge.GetDependent()))
					{
						foreach (SemanticGraphEdge toCopy in tree.OutgoingEdgeIterable(edge.GetGovernor()))
						{
							toAdd.Add(Triple.MakeTriple(edge.GetDependent(), toCopy.GetDependent(), toCopy));
						}
						toDelete.Add(edge.GetGovernor());
					}
					// Clean self-edges
					iter.Remove();
				}
				else
				{
					if (edge.GetRelation().ToString().Equals("punct"))
					{
						// Clean punctuation (again)
						if (!tree.OutgoingEdgeIterator(edge.GetDependent()).MoveNext())
						{
							// This should really never happen, but it does.
							iter.Remove();
						}
					}
				}
			}
			// (add edges we wanted to add)
			toDelete.ForEach(null);
			foreach (Triple<IndexedWord, IndexedWord, SemanticGraphEdge> edge_1 in toAdd)
			{
				tree.AddEdge(edge_1.first, edge_1.second, edge_1.third.GetRelation(), edge_1.third.GetWeight(), edge_1.third.IsExtra());
			}
			// Handle extra edges.
			// Two cases:
			// (1) the extra edge is a subj/obj edge and the main edge is a conj:.*
			//     in this case, keep the extra
			// (2) otherwise, delete the extra
			IList<SemanticGraphEdge> extraEdges = new List<SemanticGraphEdge>();
			foreach (SemanticGraphEdge edge_2 in tree.EdgeIterable())
			{
				if (edge_2.IsExtra())
				{
					IList<SemanticGraphEdge> incomingEdges = tree.IncomingEdgeList(edge_2.GetDependent());
					SemanticGraphEdge toKeep = null;
					foreach (SemanticGraphEdge candidate in incomingEdges)
					{
						if (toKeep == null)
						{
							toKeep = candidate;
						}
						else
						{
							if (toKeep.GetRelation().ToString().StartsWith("conj") && candidate.GetRelation().ToString().Matches(".subj.*|.obj.*"))
							{
								toKeep = candidate;
							}
							else
							{
								if (!candidate.IsExtra() && !(candidate.GetRelation().ToString().StartsWith("conj") && toKeep.GetRelation().ToString().Matches(".subj.*|.obj.*")))
								{
									toKeep = candidate;
								}
							}
						}
					}
					foreach (SemanticGraphEdge candidate_1 in incomingEdges)
					{
						if (candidate_1 != toKeep)
						{
							extraEdges.Add(candidate_1);
						}
					}
				}
			}
			extraEdges.ForEach(null);
			// Add apposition edges (simple coref)
			foreach (SemanticGraphEdge extraEdge in new List<SemanticGraphEdge>(extraEdges))
			{
				// note[gabor] prevent concurrent modification exception
				foreach (SemanticGraphEdge candidateAppos in tree.IncomingEdgeIterable(extraEdge.GetDependent()))
				{
					if (candidateAppos.GetRelation().ToString().Equals("appos"))
					{
						extraEdges.Add(new SemanticGraphEdge(extraEdge.GetGovernor(), candidateAppos.GetGovernor(), extraEdge.GetRelation(), extraEdge.GetWeight(), extraEdge.IsExtra()));
					}
				}
				foreach (SemanticGraphEdge candidateAppos_1 in tree.OutgoingEdgeIterable(extraEdge.GetDependent()))
				{
					if (candidateAppos_1.GetRelation().ToString().Equals("appos"))
					{
						extraEdges.Add(new SemanticGraphEdge(extraEdge.GetGovernor(), candidateAppos_1.GetDependent(), extraEdge.GetRelation(), extraEdge.GetWeight(), extraEdge.IsExtra()));
					}
				}
			}
			// Brute force ensure tree
			// Remove incoming edges from roots
			IList<SemanticGraphEdge> rootIncomingEdges = new List<SemanticGraphEdge>();
			foreach (IndexedWord root in tree.GetRoots())
			{
				foreach (SemanticGraphEdge incomingEdge in tree.IncomingEdgeIterable(root))
				{
					rootIncomingEdges.Add(incomingEdge);
				}
			}
			rootIncomingEdges.ForEach(null);
			// Loop until it becomes a tree.
			bool changed = true;
			while (changed)
			{
				// I just want trees to be trees; is that so much to ask!?
				changed = false;
				IList<IndexedWord> danglingNodes = new List<IndexedWord>();
				IList<SemanticGraphEdge> invalidEdges = new List<SemanticGraphEdge>();
				foreach (IndexedWord vertex_1 in tree.VertexSet())
				{
					// Collect statistics
					IEnumerator<SemanticGraphEdge> incomingIter = tree.IncomingEdgeIterator(vertex_1);
					bool hasIncoming = incomingIter.MoveNext();
					bool hasMultipleIncoming = false;
					if (hasIncoming)
					{
						incomingIter.Current;
						hasMultipleIncoming = incomingIter.MoveNext();
					}
					// Register actions
					if (!hasIncoming && !tree.GetRoots().Contains(vertex_1))
					{
						danglingNodes.Add(vertex_1);
					}
					else
					{
						if (hasMultipleIncoming)
						{
							foreach (SemanticGraphEdge edge in new IterableIterator<SemanticGraphEdge>(incomingIter))
							{
								invalidEdges.Add(edge_2);
							}
						}
					}
				}
				// Perform actions
				foreach (IndexedWord vertex_2 in danglingNodes)
				{
					tree.RemoveVertex(vertex_2);
					changed = true;
				}
				foreach (SemanticGraphEdge edge_3 in invalidEdges)
				{
					tree.RemoveEdge(edge_3);
					changed = true;
				}
			}
			// Edge case: remove duplicate dobj to "that."
			//            This is a common parse error.
			foreach (IndexedWord vertex_3 in tree.VertexSet())
			{
				SemanticGraphEdge thatEdge = null;
				int dobjCount = 0;
				foreach (SemanticGraphEdge edge in tree.OutgoingEdgeIterable(vertex_3))
				{
					if (Sharpen.Runtime.EqualsIgnoreCase("that", edge_2.GetDependent().Word()))
					{
						thatEdge = edge_2;
					}
					if ("dobj".Equals(edge_2.GetRelation().ToString()))
					{
						dobjCount += 1;
					}
				}
				if (dobjCount > 1 && thatEdge != null)
				{
					// Case: there are two dobj edges, one of which goes to the word "that"
					// Action: rewrite the dobj edge to "that" to be a "mark" edge.
					tree.RemoveEdge(thatEdge);
					tree.AddEdge(thatEdge.GetGovernor(), thatEdge.GetDependent(), GrammaticalRelation.ValueOf(thatEdge.GetRelation().GetLanguage(), "mark"), thatEdge.GetWeight(), thatEdge.IsExtra());
				}
			}
			// Return
			System.Diagnostics.Debug.Assert(IsTree(tree));
			return extraEdges;
		}

		/// <summary>Strip away case edges, if the incoming edge is a preposition.</summary>
		/// <remarks>
		/// Strip away case edges, if the incoming edge is a preposition.
		/// This replicates the behavior of the old Stanford dependencies on universal dependencies.
		/// </remarks>
		/// <param name="tree">The tree to modify in place.</param>
		public static void StripPrepCases(SemanticGraph tree)
		{
			// Find incoming case edges that have an 'nmod' incoming edge
			IList<SemanticGraphEdge> toClean = new List<SemanticGraphEdge>();
			foreach (SemanticGraphEdge edge in tree.EdgeIterable())
			{
				if ("case".Equals(edge.GetRelation().ToString()))
				{
					bool isPrepTarget = false;
					foreach (SemanticGraphEdge incoming in tree.IncomingEdgeIterable(edge.GetGovernor()))
					{
						if ("nmod".Equals(incoming.GetRelation().GetShortName()))
						{
							isPrepTarget = true;
							break;
						}
					}
					if (isPrepTarget && !tree.OutgoingEdgeIterator(edge.GetDependent()).MoveNext())
					{
						toClean.Add(edge);
					}
				}
			}
			// Delete these edges
			foreach (SemanticGraphEdge edge_1 in toClean)
			{
				tree.RemoveEdge(edge_1);
				tree.RemoveVertex(edge_1.GetDependent());
				System.Diagnostics.Debug.Assert(IsTree(tree));
			}
		}

		/// <summary>Determine if a tree is cyclic.</summary>
		/// <param name="tree">The tree to check.</param>
		/// <returns>True if the tree has at least once cycle in it.</returns>
		public static bool IsCyclic(SemanticGraph tree)
		{
			foreach (IndexedWord vertex in tree.VertexSet())
			{
				if (tree.GetRoots().Contains(vertex))
				{
					continue;
				}
				IndexedWord node = tree.IncomingEdgeIterator(vertex).Current.GetGovernor();
				ICollection<IndexedWord> seen = new HashSet<IndexedWord>();
				seen.Add(vertex);
				while (node != null)
				{
					if (seen.Contains(node))
					{
						return true;
					}
					seen.Add(node);
					if (tree.IncomingEdgeIterator(node).MoveNext())
					{
						node = tree.IncomingEdgeIterator(node).Current.GetGovernor();
					}
					else
					{
						node = null;
					}
				}
			}
			return false;
		}

		/// <summary>A little utility function to make sure a SemanticGraph is a tree.</summary>
		/// <param name="tree">The tree to check.</param>
		/// <returns>
		/// True if this
		/// <see cref="Edu.Stanford.Nlp.Semgraph.SemanticGraph"/>
		/// is a tree (versus a DAG, or Graph).
		/// </returns>
		public static bool IsTree(SemanticGraph tree)
		{
			foreach (IndexedWord vertex in tree.VertexSet())
			{
				// Check one and only one incoming edge
				if (tree.GetRoots().Contains(vertex))
				{
					if (tree.IncomingEdgeIterator(vertex).MoveNext())
					{
						return false;
					}
				}
				else
				{
					IEnumerator<SemanticGraphEdge> iter = tree.IncomingEdgeIterator(vertex);
					if (!iter.MoveNext())
					{
						return false;
					}
					iter.Current;
					if (iter.MoveNext())
					{
						return false;
					}
				}
				// Check incoming and outgoing edges match
				foreach (SemanticGraphEdge edge in tree.OutgoingEdgeIterable(vertex))
				{
					bool foundReverse = false;
					foreach (SemanticGraphEdge reverse in tree.IncomingEdgeIterable(edge.GetDependent()))
					{
						if (reverse == edge)
						{
							foundReverse = true;
						}
					}
					if (!foundReverse)
					{
						return false;
					}
				}
				foreach (SemanticGraphEdge edge_1 in tree.IncomingEdgeIterable(vertex))
				{
					bool foundReverse = false;
					foreach (SemanticGraphEdge reverse in tree.OutgoingEdgeIterable(edge_1.GetGovernor()))
					{
						if (reverse == edge_1)
						{
							foundReverse = true;
						}
					}
					if (!foundReverse)
					{
						return false;
					}
				}
			}
			// Check for cycles
			if (IsCyclic(tree))
			{
				return false;
			}
			// Check topological sort -- sometimes fails?
			//    try {
			//      tree.topologicalSort();
			//    } catch (Exception e) {
			//      e.printStackTrace();
			//      return false;
			//    }
			return true;
		}

		/// <summary>Returns true if the given two spans denote the same consistent NER chunk.</summary>
		/// <remarks>
		/// Returns true if the given two spans denote the same consistent NER chunk. That is, if we call
		/// <see cref="ExtractNER(System.Collections.Generic.IList{E}, Edu.Stanford.Nlp.IE.Machinereading.Structure.Span)"/>
		/// on these two spans, they would return the same span.
		/// </remarks>
		/// <param name="tokens">The tokens in the sentence.</param>
		/// <param name="a">The first span.</param>
		/// <param name="b">The second span.</param>
		/// <param name="parse">The parse tree to traverse looking for coreference chains to exploit.</param>
		/// <returns>True if these two spans contain exactly the same NER.</returns>
		public static bool NerOverlap(IList<CoreLabel> tokens, Span a, Span b, Optional<SemanticGraph> parse)
		{
			Span nerA = ExtractNER(tokens, a);
			Span nerB = ExtractNER(tokens, b);
			return nerA.Equals(nerB);
		}

		/// <seealso cref="NerOverlap(System.Collections.Generic.IList{E}, Edu.Stanford.Nlp.IE.Machinereading.Structure.Span, Edu.Stanford.Nlp.IE.Machinereading.Structure.Span, Java.Util.Optional{T})"></seealso>
		public static bool NerOverlap(IList<CoreLabel> tokens, Span a, Span b)
		{
			return NerOverlap(tokens, a, b, Optional.Empty());
		}

		/// <summary>A helper function for dumping the accuracy of the trained classifier.</summary>
		/// <param name="classifier">The classifier to evaluate.</param>
		/// <param name="dataset">The dataset to evaluate the classifier on.</param>
		public static void DumpAccuracy(IClassifier<ClauseSplitter.ClauseClassifierLabel, string> classifier, GeneralDataset<ClauseSplitter.ClauseClassifierLabel, string> dataset)
		{
			DecimalFormat df = new DecimalFormat("0.00%");
			Redwood.Log("size:         " + dataset.Size());
			Redwood.Log("split count:  " + StreamSupport.Stream(dataset.Spliterator(), false).Filter(null).Collect(Collectors.ToList()).Count);
			Redwood.Log("interm count: " + StreamSupport.Stream(dataset.Spliterator(), false).Filter(null).Collect(Collectors.ToList()).Count);
			Pair<double, double> pr = classifier.EvaluatePrecisionAndRecall(dataset, ClauseSplitter.ClauseClassifierLabel.ClauseSplit);
			Redwood.Log("p  (split):   " + df.Format(pr.first));
			Redwood.Log("r  (split):   " + df.Format(pr.second));
			Redwood.Log("f1 (split):   " + df.Format(2 * pr.first * pr.second / (pr.first + pr.second)));
			pr = classifier.EvaluatePrecisionAndRecall(dataset, ClauseSplitter.ClauseClassifierLabel.ClauseInterm);
			Redwood.Log("p  (interm):  " + df.Format(pr.first));
			Redwood.Log("r  (interm):  " + df.Format(pr.second));
			Redwood.Log("f1 (interm):  " + df.Format(2 * pr.first * pr.second / (pr.first + pr.second)));
		}

		private sealed class _HashSet_494 : HashSet<string>
		{
			public _HashSet_494()
			{
				{
					this.Add("believed");
					this.Add("debatable");
					this.Add("disputed");
					this.Add("dubious");
					this.Add("hypothetical");
					this.Add("impossible");
					this.Add("improbable");
					this.Add("plausible");
					this.Add("putative");
					this.Add("questionable");
					this.Add("so called");
					this.Add("supposed");
					this.Add("suspicious");
					this.Add("theoretical");
					this.Add("uncertain");
					this.Add("unlikely");
					this.Add("would - be");
					this.Add("apparent");
					this.Add("arguable");
					this.Add("assumed");
					this.Add("likely");
					this.Add("ostensible");
					this.Add("possible");
					this.Add("potential");
					this.Add("predicted");
					this.Add("presumed");
					this.Add("probable");
					this.Add("seeming");
					this.Add("anti");
					this.Add("fake");
					this.Add("fictional");
					this.Add("fictitious");
					this.Add("imaginary");
					this.Add("mythical");
					this.Add("phony");
					this.Add("false");
					this.Add("artificial");
					this.Add("erroneous");
					this.Add("mistaken");
					this.Add("mock");
					this.Add("pseudo");
					this.Add("simulated");
					this.Add("spurious");
					this.Add("deputy");
					this.Add("faulty");
					this.Add("virtual");
					this.Add("doubtful");
					this.Add("erstwhile");
					this.Add("ex");
					this.Add("expected");
					this.Add("former");
					this.Add("future");
					this.Add("onetime");
					this.Add("past");
					this.Add("proposed");
				}
			}
		}

		/// <summary>The dictionary of privative adjectives, as per http://hci.stanford.edu/cstr/reports/2014-04.pdf</summary>
		public static readonly ICollection<string> PrivativeAdjectives = Java.Util.Collections.UnmodifiableSet(new _HashSet_494());

		/// <summary>Construct the spanning span of the given list of tokens.</summary>
		/// <param name="tokens">The tokens that should define the span.</param>
		/// <returns>A span (0-indexed) that covers all of the tokens.</returns>
		public static Span TokensToSpan<_T0>(IList<_T0> tokens)
			where _T0 : IHasIndex
		{
			int min = int.MaxValue;
			int max = int.MinValue;
			foreach (IHasIndex token in tokens)
			{
				min = Math.Min(token.Index() - 1, min);
				max = Math.Max(token.Index(), max);
			}
			if (min < 0 || max == int.MaxValue)
			{
				throw new ArgumentException("Could not compute span from tokens!");
			}
			else
			{
				if (min >= max)
				{
					throw new InvalidOperationException("Either logic is broken or Gabor can't code.");
				}
				else
				{
					return new Span(min, max);
				}
			}
		}
	}
}
