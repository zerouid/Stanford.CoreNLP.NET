using System;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>A class to find the forward entailments warranted by a particular sentence or clause.</summary>
	/// <remarks>
	/// A class to find the forward entailments warranted by a particular sentence or clause.
	/// Note that this will _only_ do deletions -- it will neither consider insertions, nor mutations of
	/// the original sentence.
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class ForwardEntailer : IBiFunction<SemanticGraph, bool, ForwardEntailerSearchProblem>
	{
		/// <summary>The maximum number of ticks top search for.</summary>
		/// <remarks>The maximum number of ticks top search for. Otherwise, the search will be exhaustive.</remarks>
		public readonly int maxTicks;

		/// <summary>The maximum number of results to return from a single search.</summary>
		public readonly int maxResults;

		/// <summary>The weights to use for entailment.</summary>
		public readonly NaturalLogicWeights weights;

		/// <summary>Create a new searcher with the specified parameters.</summary>
		/// <param name="maxResults">The maximum number of results to return from a single search.</param>
		/// <param name="maxTicks">The maximum number of ticks to search for.</param>
		/// <param name="weights">The natural logic weights to use for the searches.</param>
		public ForwardEntailer(int maxResults, int maxTicks, NaturalLogicWeights weights)
		{
			this.maxResults = maxResults;
			this.maxTicks = maxTicks;
			this.weights = weights;
		}

		/// <seealso cref="ForwardEntailer(int, int, NaturalLogicWeights)"/>
		public ForwardEntailer(int maxResults, NaturalLogicWeights weights)
			: this(maxResults, maxResults * 25, weights)
		{
		}

		/// <seealso cref="ForwardEntailer(int, int, NaturalLogicWeights)"/>
		public ForwardEntailer(NaturalLogicWeights weights)
			: this(int.MaxValue, int.MaxValue, weights)
		{
		}

		/// <summary>
		/// Create a new search problem instance, given a sentence (possibly fragment), and the corresponding
		/// parse tree.
		/// </summary>
		/// <param name="parseTree">The original tree of the sentence we are beginning with</param>
		/// <param name="truthOfPremise">The truth of the premise. In most applications, this will just be true.</param>
		/// <returns>A new search problem instance.</returns>
		public virtual ForwardEntailerSearchProblem Apply(SemanticGraph parseTree, bool truthOfPremise)
		{
			foreach (IndexedWord vertex in parseTree.VertexSet())
			{
				CoreLabel token = vertex.BackingLabel();
				if (token != null && !token.ContainsKey(typeof(NaturalLogicAnnotations.PolarityAnnotation)))
				{
					throw new ArgumentException("Cannot run Natural Logic forward entailment without polarity annotations set. See " + typeof(NaturalLogicAnnotator).GetSimpleName());
				}
			}
			return new ForwardEntailerSearchProblem(parseTree, truthOfPremise, maxResults, maxTicks, weights);
		}
	}
}
