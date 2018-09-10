using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>Just a convenience alias for a clause splitting search problem factory.</summary>
	/// <remarks>
	/// Just a convenience alias for a clause splitting search problem factory.
	/// Mostly here to form a nice parallel with
	/// <see cref="ForwardEntailer"/>
	/// .
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public interface IClauseSplitter : IBiFunction<SemanticGraph, bool, ClauseSplitterSearchProblem>
	{
		[System.Serializable]
		public sealed class ClauseClassifierLabel
		{
			public static readonly ClauseSplitter.ClauseClassifierLabel ClauseSplit = new ClauseSplitter.ClauseClassifierLabel(2);

			public static readonly ClauseSplitter.ClauseClassifierLabel ClauseInterm = new ClauseSplitter.ClauseClassifierLabel(1);

			public static readonly ClauseSplitter.ClauseClassifierLabel NotAClause = new ClauseSplitter.ClauseClassifierLabel(0);

			public readonly byte index;

			internal ClauseClassifierLabel(int val)
			{
				this.index = unchecked((byte)val);
			}

			/// <summary>Seriously, why would Java not have this by default?</summary>
			public override string ToString()
			{
				return this.ToString();
			}

			public static ClauseSplitter.ClauseClassifierLabel FromIndex(int index)
			{
				switch (index)
				{
					case 0:
					{
						return ClauseSplitter.ClauseClassifierLabel.NotAClause;
					}

					case 1:
					{
						return ClauseSplitter.ClauseClassifierLabel.ClauseInterm;
					}

					case 2:
					{
						return ClauseSplitter.ClauseClassifierLabel.ClauseSplit;
					}

					default:
					{
						throw new ArgumentException("Not a valid index: " + index);
					}
				}
			}
		}

		/// <summary>Train a clause searcher factory.</summary>
		/// <remarks>
		/// Train a clause searcher factory. That is, train a classifier for which arcs should be
		/// new clauses.
		/// </remarks>
		/// <param name="trainingData">
		/// The training data. This is a stream of triples of:
		/// <ol>
		/// <li>The sentence containing a known extraction.</li>
		/// <li>The span of the subject in the sentence, as a token span.</li>
		/// <li>The span of the object in the sentence, as a token span.</li>
		/// </ol>
		/// </param>
		/// <param name="modelPath">
		/// The path to save the model to. This is useful for
		/// <see cref="Load(string)"/>
		/// .
		/// </param>
		/// <param name="trainingDataDump">The path to save the training data, as a set of labeled featurized datums.</param>
		/// <param name="featurizer">The featurizer to use for this classifier.</param>
		/// <returns>A factory for creating searchers from a given dependency tree.</returns>
		IClauseSplitter Train(IEnumerable<Pair<ICoreMap, ICollection<Pair<Span, Span>>>> trainingData, Optional<File> modelPath, Optional<File> trainingDataDump, ClauseSplitterSearchProblem.IFeaturizer featurizer);

		// Parse options
		// Generally useful objects
		// Step 1: Loop over data
		// Parse training datum
		// Create raw clause searcher (no classifier)
		// Run search
		// Parse the search callback
		// Search for extractions
		// Clean up the guesses
		// Check if it matches
		// Process the datum
		// Convert the path to datums
		// If this is a "true" path, add the k-1 decisions as INTERM and the last decision as a SPLIT
		// If this is a "false" path, then we know at least the last decision was bad.
		// If this is an "unknown" path, only add it if it was the result of vanilla splits
		// (check if it is a sequence of simple splits)
		// (if so, add it as if it were a True example)
		// Add the datums
		// (create datum)
		// (dump datum to debug log)
		// (add datum to dataset)
		// Debug info
		// Close the file
		// Step 2: Train classifier
		// Step 3: Check accuracy of classifier
		// Step 5: return factory
		IClauseSplitter Train(IEnumerable<Pair<ICoreMap, ICollection<Pair<Span, Span>>>> trainingData, File modelPath, File trainingDataDump);

		/// <summary>Load a factory model from a given path.</summary>
		/// <remarks>
		/// Load a factory model from a given path. This can be trained with
		/// <see cref="Train(Java.Util.Stream.IStream{T}, Java.Util.Optional{T}, Java.Util.Optional{T}, IFeaturizer)"/>
		/// .
		/// </remarks>
		/// <returns>A function taking a dependency tree, and returning a clause searcher.</returns>
		/// <exception cref="System.IO.IOException"/>
		IClauseSplitter Load(string serializedModel);
	}

	public static class ClauseSplitterConstants
	{
		/// <summary>A logger for this class</summary>
		public const Redwood.RedwoodChannels log = Redwood.Channels(typeof(IClauseSplitter));
	}
}
