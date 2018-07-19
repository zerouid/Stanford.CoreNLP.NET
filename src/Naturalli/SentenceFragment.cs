using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>A representation of a sentence fragment.</summary>
	/// <author>Gabor Angeli</author>
	public class SentenceFragment
	{
		/// <summary>The words in this sentence fragment (e.g., for use as the gloss of the fragment).</summary>
		public readonly IList<CoreLabel> words = new List<CoreLabel>();

		/// <summary>The parse tree for this sentence fragment.</summary>
		public readonly SemanticGraph parseTree;

		/// <summary>The assumed truth of this fragment; this is relevant for what entailments are supported</summary>
		public readonly bool assumedTruth;

		/// <summary>A score for this fragment.</summary>
		/// <remarks>A score for this fragment. This is 1.0 by default.</remarks>
		public double score = 1.0;

		public SentenceFragment(SemanticGraph tree, bool assumedTruth, bool copy)
		{
			if (copy)
			{
				this.parseTree = new SemanticGraph(tree);
			}
			else
			{
				this.parseTree = tree;
			}
			this.assumedTruth = assumedTruth;
			Sharpen.Collections.AddAll(words, this.parseTree.VertexListSorted().Stream().Map(null).Collect(Collectors.ToList()));
		}

		/// <summary>The length of this fragment, in words</summary>
		public virtual int Length()
		{
			return words.Count;
		}

		/// <summary>Changes the score of this fragment in place.</summary>
		/// <param name="score">The new score of the fragment</param>
		/// <returns>This sentence fragment.</returns>
		public virtual Edu.Stanford.Nlp.Naturalli.SentenceFragment ChangeScore(double score)
		{
			this.score = score;
			return this;
		}

		/// <summary>
		/// Return the tokens in this fragment, but padded with null so that the index in this
		/// sentence matches the index of the parse tree.
		/// </summary>
		public virtual IList<CoreLabel> PaddedWords()
		{
			int maxIndex = -1;
			foreach (IndexedWord vertex in parseTree.VertexSet())
			{
				maxIndex = Math.Max(maxIndex, vertex.Index());
			}
			IList<CoreLabel> tokens = new List<CoreLabel>(maxIndex);
			for (int i = 0; i < maxIndex; ++i)
			{
				tokens.Add(null);
			}
			foreach (CoreLabel token in this.words)
			{
				tokens.Set(token.Index() - 1, token);
			}
			return tokens;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Naturalli.SentenceFragment))
			{
				return false;
			}
			Edu.Stanford.Nlp.Naturalli.SentenceFragment that = (Edu.Stanford.Nlp.Naturalli.SentenceFragment)o;
			return this.parseTree.VertexSet().Equals((that.parseTree.VertexSet()));
		}

		public override int GetHashCode()
		{
			return this.parseTree.VertexSet().GetHashCode();
		}

		public override string ToString()
		{
			IList<Pair<string, int>> glosses = new List<Pair<string, int>>();
			foreach (CoreLabel word in words)
			{
				// Add the word itself
				glosses.Add(Pair.MakePair(word.Word(), word.Index() - 1));
				string addedConnective = null;
				// Find additional connectives
				foreach (SemanticGraphEdge edge in parseTree.IncomingEdgeIterable(new IndexedWord(word)))
				{
					string rel = edge.GetRelation().ToString();
					if (rel.Contains("_"))
					{
						// for Stanford dependencies only
						addedConnective = Sharpen.Runtime.Substring(rel, rel.IndexOf('_') + 1);
					}
				}
				if (addedConnective != null)
				{
					// Found a connective (e.g., a preposition or conjunction)
					Pair<int, int> yield = parseTree.YieldSpan(new IndexedWord(word));
					glosses.Add(Pair.MakePair(addedConnective.ReplaceAll("_", " "), yield.first - 1));
				}
			}
			// Sort the sentence
			glosses.Sort(null);
			// Return the sentence
			return StringUtils.Join(glosses.Stream().Map(null), " ");
		}
	}
}
