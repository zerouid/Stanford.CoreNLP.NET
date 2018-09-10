using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Simple
{
	/// <summary>
	/// <p>
	/// A set of common utility algorithms for working with sentences (e.g., finding the head of a span).
	/// </summary>
	/// <remarks>
	/// <p>
	/// A set of common utility algorithms for working with sentences (e.g., finding the head of a span).
	/// These are not intended to be perfect, or even the canonical version of these algorithms.
	/// They should only be trusted for prototyping, and more careful attention should be paid in cases
	/// where the performance of the task is important or the domain is unusual.
	/// </p>
	/// <p>
	/// For developers: this class is intended to be where <i>domain independent</i> and
	/// <i>broadly useful</i> functions on a sentence would go, rather than polluting the
	/// <see cref="Sentence"/>
	/// class itself.
	/// </p>
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class SentenceAlgorithms
	{
		/// <summary>
		/// The underlying
		/// <see cref="Sentence"/>
		/// .
		/// </summary>
		public readonly Sentence sentence;

		/// <summary>Create a new algorithms object, based off of a sentence.</summary>
		/// <seealso cref="Sentence.Algorithms()"/>
		public SentenceAlgorithms(Sentence impl)
		{
			this.sentence = impl;
		}

		/// <summary>Returns a collection of keyphrases, defined as relevant noun phrases and verbs in the sentence.</summary>
		/// <remarks>
		/// Returns a collection of keyphrases, defined as relevant noun phrases and verbs in the sentence.
		/// Each token of the sentence is consumed at most once.
		/// What counts as a keyphrase is in general quite subjective -- this method is just one possible interpretation
		/// (in particular, Gabor's interpretation).
		/// Please don't rely on this method to produce exactly your interpretation of what a keyphrase is.
		/// </remarks>
		/// <returns>A list of spans in the sentence, where each one corresponds to a keyphrase.</returns>
		/// <author>Gabor Angeli</author>
		public virtual IList<Span> KeyphraseSpans()
		{
			//
			// Implementation note:
			//   This is implemented roughly as a finite state automata, looking for sequences of nouns, adjective+nouns, verbs,
			//   and a few special cases of prepositions.
			//   The code defines a transition matrix, based on POS tags and lemmas, where at each word we update the valid next
			//   tags/words based on the current tag/word we see.
			// Note: The tag 'B' is used for the verb "to be", rather than the usual 'V' tag.
			// Note: The tag 'X' is used for proper nouns, rather than the usual 'N' tag.
			// Note: The tag 'Z' is used for possessives, rather than the usual 'P' tag.
			//
			// The output
			IList<Span> spans = new List<Span>();
			// The marker for where the last span began
			int spanBegin = -1;
			// The expected next states
			ICollection<char> expectNextTag = new HashSet<char>();
			ICollection<string> expectNextLemma = new HashSet<string>();
			// A special marker for when we look-ahead and only accept the last word if
			// the word after it is ok (e.g., PP attachments).
			bool inLookahead = false;
			// The transition matrix, over POS tags.
			IConsumer<char> updateExpectation = null;
			// 'water freezing' is fishy, but 'freezing water' is ok.
			// Run the FSA:
			for (int i = 0; i < sentence.Length(); ++i)
			{
				// Get some variables
				string tag = sentence.PosTag(i);
				char coarseTag = char.ToUpperCase(tag[0]);
				string lemma = sentence.Lemma(i).ToLower();
				// Tweak the tag
				if (coarseTag == 'V' && lemma.Equals("be"))
				{
					coarseTag = 'B';
				}
				else
				{
					if (tag.StartsWith("NNP"))
					{
						coarseTag = 'X';
					}
					else
					{
						if (tag.StartsWith("POS"))
						{
							coarseTag = 'Z';
						}
					}
				}
				// (don't collapse 'ing' nouns)
				if (coarseTag == 'N' && sentence.Word(i).EndsWith("ing"))
				{
					coarseTag = 'G';
				}
				// Transition
				if (spanBegin < 0 && !sentence.Word(i).Equals("%") && (coarseTag == 'N' || coarseTag == 'V' || coarseTag == 'J' || coarseTag == 'X' || coarseTag == 'G'))
				{
					// Case: we were not in a span, but we hit a valid start tag.
					spanBegin = i;
					updateExpectation.Accept(coarseTag);
					inLookahead = false;
				}
				else
				{
					if (spanBegin >= 0)
					{
						// Case: we're in a span
						if (expectNextTag.Contains(coarseTag))
						{
							// Case: we hit a valid expected POS tag.
							//       update the transition matrix.
							updateExpectation.Accept(coarseTag);
							inLookahead = false;
						}
						else
						{
							if (expectNextLemma.Contains(lemma))
							{
								switch (lemma)
								{
									case "of":
									{
										// Case: we hit a valid word. Do something special.
										// These prepositions are valid to subsume into a noun phrase.
										// Update the transition matrix, and mark this as conditionally ok.
										updateExpectation.Accept('I');
										inLookahead = true;
										break;
									}

									case "'s":
									{
										// Possessives often denote a longer compound phrase
										updateExpectation.Accept('Z');
										inLookahead = true;
										break;
									}

									default:
									{
										throw new InvalidOperationException("Unknown special lemma: " + lemma);
									}
								}
							}
							else
							{
								// Case: We have transitioned to an 'invalid' state, and therefore the span should end.
								if (inLookahead)
								{
									// If we were in a lookahead token, ignore the last token (as per the lookahead definition)
									spans.Add(Span.FromValues(spanBegin, i - 1));
								}
								else
								{
									// Otherwise, add the span
									spans.Add(Span.FromValues(spanBegin, i));
								}
								// We may also have started a new span.
								// Check to see if we have started a new span.
								if (coarseTag == 'N' || coarseTag == 'V' || coarseTag == 'J' || coarseTag == 'X' || coarseTag == 'G')
								{
									spanBegin = i;
									updateExpectation.Accept(coarseTag);
								}
								else
								{
									spanBegin = -1;
								}
								inLookahead = false;
							}
						}
					}
				}
			}
			// Add a potential last span
			if (spanBegin >= 0)
			{
				spans.Add(Span.FromValues(spanBegin, inLookahead ? sentence.Length() - 1 : sentence.Length()));
			}
			// Return
			return spans;
		}

		/// <summary>Get the keyphrases of the sentence as a list of Strings.</summary>
		/// <param name="toString">The function to use to convert a span to a string. The canonical case is Sentence::words</param>
		/// <returns>A list of keyphrases, as Strings.</returns>
		/// <seealso cref="KeyphraseSpans()"/>
		public virtual IList<string> Keyphrases(Func<Sentence, IList<string>> toString)
		{
			return KeyphraseSpans().Stream().Map(null).Collect(Collectors.ToList());
		}

		/// <summary>The keyphrases of the sentence, using the words of the sentence to convert a span into a keyphrase.</summary>
		/// <returns>A list of String keyphrases in the sentence.</returns>
		/// <seealso cref="KeyphraseSpans()"/>
		public virtual IList<string> Keyphrases()
		{
			return Keyphrases(null);
		}

		/// <summary>Get the index of the head word for a given span, based off of the dependency parse.</summary>
		/// <param name="tokenSpan">The span of tokens we are finding the head of.</param>
		/// <returns>The head index of the given span of tokens.</returns>
		public virtual int HeadOfSpan(Span tokenSpan)
		{
			// Error checks
			if (tokenSpan.Size() == 0)
			{
				throw new ArgumentException("Cannot find head word of empty span!");
			}
			IList<Optional<int>> governors = sentence.Governors();
			if (tokenSpan.Start() >= governors.Count)
			{
				throw new ArgumentException("Span is out of range: " + tokenSpan + "; sentence: " + sentence);
			}
			if (tokenSpan.End() > governors.Count)
			{
				throw new ArgumentException("Span is out of range: " + tokenSpan + "; sentence: " + sentence);
			}
			// Find where to start searching up the dependency tree
			int candidateStart = tokenSpan.End() - 1;
			Optional<int> parent;
			while (!(parent = governors[candidateStart]).IsPresent())
			{
				candidateStart -= 1;
				if (candidateStart < tokenSpan.Start())
				{
					// Case: nothing in this span has a head. Default to right-most element.
					return tokenSpan.End() - 1;
				}
			}
			int candidate = candidateStart;
			// Search up the dependency tree
			ICollection<int> seen = new HashSet<int>();
			while (parent.IsPresent() && parent.Get() >= tokenSpan.Start() && parent.Get() < tokenSpan.End())
			{
				candidate = parent.Get();
				if (seen.Contains(candidate))
				{
					return candidate;
				}
				seen.Add(candidate);
				parent = governors[candidate];
			}
			// Return
			return candidate;
		}

		/// <summary>Return all the spans of a sentence.</summary>
		/// <remarks>
		/// Return all the spans of a sentence. So, for example, a sentence "a b c" would return:
		/// [a], [b], [c], [a b], [b c], [a b c].
		/// </remarks>
		/// <param name="selector">
		/// The function to apply to each token. For example,
		/// <see cref="Sentence.Words()"/>
		/// .
		/// For that example, you can use <code>allSpans(Sentence::words)</code>.
		/// </param>
		/// <param name="maxLength">
		/// The maximum length of the spans to extract. The default to extract all spans
		/// is to set this to <code>sentence.length()</code>.
		/// </param>
		/// <?/>
		/// <returns>A streaming iterable of spans for this sentence.</returns>
		public virtual IEnumerable<IList<E>> AllSpans<E>(Func<Sentence, IList<E>> selector, int maxLength)
		{
			return null;
		}

		// Get the term
		// Update the state
		// Return
		/// <seealso cref="AllSpans{E}(Java.Util.Function.Func{T, R}, int)"></seealso>
		public virtual IEnumerable<IList<E>> AllSpans<E>(Func<Sentence, IList<E>> selector)
		{
			return AllSpans(selector, sentence.Length());
		}

		/// <seealso cref="AllSpans{E}(Java.Util.Function.Func{T, R}, int)"></seealso>
		public virtual IEnumerable<IList<string>> AllSpans()
		{
			return AllSpans(null, sentence.Length());
		}

		/// <summary>Select the most common element of the given type in the given span.</summary>
		/// <remarks>
		/// Select the most common element of the given type in the given span.
		/// This is useful for, e.g., finding the most likely NER span of a given span, or the most
		/// likely POS tag of a given span.
		/// Null entries are removed.
		/// </remarks>
		/// <param name="span">The span of the sentence to find the mode element in. This must be entirely contained in the sentence.</param>
		/// <param name="selector">The property of the sentence we are getting the mode of. For example, <code>Sentence::posTags</code></param>
		/// <?/>
		/// <returns>The most common element of the given property in the sentence.</returns>
		public virtual E ModeInSpan<E>(Span span, Func<Sentence, IList<E>> selector)
		{
			if (!Span.FromValues(0, sentence.Length()).Contains(span))
			{
				throw new ArgumentException("Span must be entirely contained in the sentence: " + span + " (sentence length=" + sentence.Length() + ")");
			}
			ICounter<E> candidates = new ClassicCounter<E>();
			foreach (int i in span)
			{
				candidates.IncrementCount(selector.Apply(sentence)[i]);
			}
			candidates.Remove(null);
			return Counters.Argmax(candidates);
		}

		/// <summary>Run a proper BFS over a dependency graph, finding the shortest path between two vertices.</summary>
		/// <param name="start">The start index.</param>
		/// <param name="end">The end index.</param>
		/// <param name="selector">The selector to use for the word nodes.</param>
		/// <returns>
		/// A path string, analogous to
		/// <see cref="DependencyPathBetween(int, int)"/>
		/// </returns>
		protected internal virtual IList<string> LoopyDependencyPathBetween(int start, int end, Optional<Func<Sentence, IList<string>>> selector)
		{
			// Find the start and end
			SemanticGraph graph = this.sentence.DependencyGraph();
			IndexedWord[] indexedWords = new IndexedWord[this.sentence.Length()];
			foreach (IndexedWord vertex in graph.VertexSet())
			{
				indexedWords[vertex.Index() - 1] = vertex;
			}
			// Set up the search
			BitSet seen = new BitSet();
			int[] backpointers = new int[sentence.Length()];
			Arrays.Fill(backpointers, -1);
			IQueue<IndexedWord> fringe = new LinkedList<IndexedWord>();
			fringe.Add(indexedWords[start]);
			// Run the search
			while (!fringe.IsEmpty())
			{
				IndexedWord vertex_1 = fringe.Poll();
				int vertexIndex = vertex_1.Index() - 1;
				if (seen.Get(vertexIndex))
				{
					continue;
				}
				// should not reach here
				seen.Set(vertexIndex);
				foreach (SemanticGraphEdge inEdge in graph.IncomingEdgeIterable(vertex_1))
				{
					IndexedWord governor = inEdge.GetGovernor();
					int govIndex = governor.Index() - 1;
					if (!seen.Get(govIndex))
					{
						backpointers[govIndex] = vertexIndex;
						if (govIndex == end)
						{
							break;
						}
						else
						{
							fringe.Add(governor);
						}
					}
				}
				foreach (SemanticGraphEdge outEdge in graph.OutgoingEdgeIterable(vertex_1))
				{
					IndexedWord dependent = outEdge.GetDependent();
					int depIndex = dependent.Index() - 1;
					if (!seen.Get(depIndex))
					{
						backpointers[depIndex] = vertexIndex;
						if (depIndex == end)
						{
							break;
						}
						else
						{
							fringe.Add(dependent);
						}
					}
				}
			}
			// Infer the path
			List<string> path = new List<string>();
			Optional<IList<string>> words = selector.Map(null);
			int vertex_2 = end;
			while (vertex_2 != start)
			{
				// 1. Add the word
				if (words.IsPresent())
				{
					path.Add(words.Get()[vertex_2]);
				}
				// 2. Find the parent
				foreach (SemanticGraphEdge inEdge in graph.IncomingEdgeIterable(indexedWords[vertex_2]))
				{
					int governor = inEdge.GetGovernor().Index() - 1;
					if (backpointers[vertex_2] == governor)
					{
						path.Add("-" + inEdge.GetRelation().ToString() + "->");
						break;
					}
				}
				foreach (SemanticGraphEdge outEdge in graph.OutgoingEdgeIterable(indexedWords[vertex_2]))
				{
					int dependent = outEdge.GetDependent().Index() - 1;
					if (backpointers[vertex_2] == dependent)
					{
						path.Add("<-" + outEdge.GetRelation().ToString() + "-");
						break;
					}
				}
				// 3. Update the node
				vertex_2 = backpointers[vertex_2];
			}
			words.IfPresent(null);
			Java.Util.Collections.Reverse(path);
			return path;
		}

		/// <summary>Find the dependency path between two words in a sentence.</summary>
		/// <param name="start">The start word, 0-indexed.</param>
		/// <param name="end">The end word, 0-indexed.</param>
		/// <param name="selector">The selector for the strings between the path, if any. If left empty, these will be omitted from the list.</param>
		/// <returns>A list encoding the dependency path between the vertices, suitable for inclusion as features.</returns>
		public virtual IList<string> DependencyPathBetween(int start, int end, Optional<Func<Sentence, IList<string>>> selector)
		{
			// Get paths from a node to the root of the sentence
			LinkedList<int> rootToStart = new LinkedList<int>();
			LinkedList<int> rootToEnd = new LinkedList<int>();
			int startAncestor = start;
			IList<Optional<int>> governors = sentence.Governors();
			ICollection<int> seenVertices = new HashSet<int>();
			while (startAncestor >= 0 && governors[startAncestor].IsPresent())
			{
				if (seenVertices.Contains(startAncestor))
				{
					// Found loopiness -- revert to BFS
					return LoopyDependencyPathBetween(start, end, selector);
				}
				seenVertices.Add(startAncestor);
				rootToStart.AddFirst(startAncestor);
				startAncestor = governors[startAncestor].Get();
			}
			if (startAncestor == -1)
			{
				rootToStart.AddFirst(-1);
			}
			int endAncestor = end;
			seenVertices.Clear();
			while (endAncestor >= 0 && governors[endAncestor].IsPresent())
			{
				if (seenVertices.Contains(endAncestor))
				{
					// Found loopiness -- revert to BFS
					return LoopyDependencyPathBetween(start, end, selector);
				}
				seenVertices.Add(endAncestor);
				rootToEnd.AddFirst(endAncestor);
				endAncestor = governors[endAncestor].Get();
			}
			if (endAncestor == -1)
			{
				rootToEnd.AddFirst(-1);
			}
			// Get least common node
			int leastCommonNodeIndex = (rootToStart.Count == 0 || rootToEnd.Count == 0 || !rootToStart[0].Equals(rootToEnd[0])) ? -1 : 0;
			for (int i = 1; i < Math.Min(rootToStart.Count, rootToEnd.Count); ++i)
			{
				if (rootToStart[i].Equals(rootToEnd[i]))
				{
					leastCommonNodeIndex = i;
				}
			}
			// Construct the path
			if (leastCommonNodeIndex < 0)
			{
				return Java.Util.Collections.EmptyList();
			}
			IList<string> path = new List<string>();
			Optional<IList<string>> words = selector.Map(null);
			for (int i_1 = rootToStart.Count - 1; i_1 > leastCommonNodeIndex; --i_1)
			{
				int index = i_1;
				words.IfPresent(null);
				path.Add("<-" + sentence.IncomingDependencyLabel(rootToStart[i_1]).OrElse("dep") + "-");
			}
			if (words.IsPresent())
			{
				path.Add(words.Get()[rootToStart[leastCommonNodeIndex]]);
			}
			for (int i_2 = leastCommonNodeIndex + 1; i_2 < rootToEnd.Count; ++i_2)
			{
				int index = i_2;
				path.Add("-" + sentence.IncomingDependencyLabel(rootToEnd[i_2]).OrElse("dep") + "->");
				words.IfPresent(null);
			}
			return path;
		}

		public virtual IList<string> DependencyPathBetween(int start, int end)
		{
			return DependencyPathBetween(start, end, Optional.Of(null));
		}

		/// <summary>A funky little helper method to interpret each token of the sentence as an HTML string, and translate it back to text.</summary>
		/// <remarks>
		/// A funky little helper method to interpret each token of the sentence as an HTML string, and translate it back to text.
		/// Note that this is <b>in place</b>.
		/// </remarks>
		public virtual void UnescapeHTML()
		{
			// Change in the protobuf
			for (int i = 0; i < sentence.Length(); ++i)
			{
				CoreNLPProtos.Token.Builder token = sentence.RawToken(i);
				token.SetWord(StringUtils.UnescapeHtml3(token.GetWord()));
				token.SetLemma(StringUtils.UnescapeHtml3(token.GetLemma()));
			}
			// Change in the annotation
			ICoreMap cm = sentence.document.AsAnnotation().Get(typeof(CoreAnnotations.SentencesAnnotation))[sentence.SentenceIndex()];
			foreach (CoreLabel token_1 in cm.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				token_1.SetWord(StringUtils.UnescapeHtml3(token_1.Word()));
				token_1.SetLemma(StringUtils.UnescapeHtml3(token_1.Lemma()));
			}
		}
	}
}
