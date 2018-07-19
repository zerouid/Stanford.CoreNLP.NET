using System.Collections.Generic;
using Edu.Stanford.Nlp.Loglinear.Inference;
using Edu.Stanford.Nlp.Loglinear.Model;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Benchmarks
{
	/// <summary>Created on 9/11/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This simulates game-player-like activity, with a few CoNLL CliqueTrees playing host to lots and lots of manipulations
	/// by adding and removing human "observations". In real life, this kind of behavior occurs during sampling lookahead for
	/// LENSE-like systems.
	/// <p>
	/// In order to measure only the realistic parts of behavior, and not the random generation of numbers, we pre-cache a
	/// few hundred ConcatVectors representing human obs features, then our feature function is just indexing into that cache.
	/// The cache is designed to require a bit of L1 cache eviction to page through, so that we don't see artificial speed
	/// gains during dot products b/c we already have both features and weights in L1 cache.
	/// </author>
	public class GamePlayerBenchmark
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(GamePlayerBenchmark));

		internal const string DataPath = "/u/nlp/data/ner/conll/";

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			//////////////////////////////////////////////////////////////
			// Generate the CoNLL CliqueTrees to use during gameplay
			//////////////////////////////////////////////////////////////
			CoNLLBenchmark coNLL = new CoNLLBenchmark();
			IList<CoNLLBenchmark.CoNLLSentence> train = coNLL.GetSentences(DataPath + "conll.iob.4class.train");
			IList<CoNLLBenchmark.CoNLLSentence> testA = coNLL.GetSentences(DataPath + "conll.iob.4class.testa");
			IList<CoNLLBenchmark.CoNLLSentence> testB = coNLL.GetSentences(DataPath + "conll.iob.4class.testb");
			IList<CoNLLBenchmark.CoNLLSentence> allData = new List<CoNLLBenchmark.CoNLLSentence>();
			Sharpen.Collections.AddAll(allData, train);
			Sharpen.Collections.AddAll(allData, testA);
			Sharpen.Collections.AddAll(allData, testB);
			ICollection<string> tagsSet = new HashSet<string>();
			foreach (CoNLLBenchmark.CoNLLSentence sentence in allData)
			{
				foreach (string nerTag in sentence.ner)
				{
					tagsSet.Add(nerTag);
				}
			}
			IList<string> tags = new List<string>();
			Sharpen.Collections.AddAll(tags, tagsSet);
			coNLL.embeddings = coNLL.GetEmbeddings(DataPath + "google-300-trimmed.ser.gz", allData);
			log.Info("Making the training set...");
			ConcatVectorNamespace @namespace = new ConcatVectorNamespace();
			int trainSize = train.Count;
			GraphicalModel[] trainingSet = new GraphicalModel[trainSize];
			for (int i = 0; i < trainSize; i++)
			{
				if (i % 10 == 0)
				{
					log.Info(i + "/" + trainSize);
				}
				trainingSet[i] = coNLL.GenerateSentenceModel(@namespace, train[i], tags);
			}
			//////////////////////////////////////////////////////////////
			// Generate the random human observation feature vectors that we'll use
			//////////////////////////////////////////////////////////////
			Random r = new Random(10);
			int numFeatures = 5;
			int featureLength = 30;
			ConcatVector[] humanFeatureVectors = new ConcatVector[1000];
			for (int i_1 = 0; i_1 < humanFeatureVectors.Length; i_1++)
			{
				humanFeatureVectors[i_1] = new ConcatVector(numFeatures);
				for (int j = 0; j < numFeatures; j++)
				{
					if (r.NextBoolean())
					{
						humanFeatureVectors[i_1].SetSparseComponent(j, r.NextInt(featureLength), r.NextDouble());
					}
					else
					{
						double[] dense = new double[featureLength];
						for (int k = 0; k < dense.Length; k++)
						{
							dense[k] = r.NextDouble();
						}
						humanFeatureVectors[i_1].SetDenseComponent(j, dense);
					}
				}
			}
			ConcatVector weights = new ConcatVector(numFeatures);
			for (int i_2 = 0; i_2 < numFeatures; i_2++)
			{
				double[] dense = new double[featureLength];
				for (int j = 0; j < dense.Length; j++)
				{
					dense[j] = r.NextDouble();
				}
				weights.SetDenseComponent(i_2, dense);
			}
			//////////////////////////////////////////////////////////////
			// Actually perform gameplay-like random mutations
			//////////////////////////////////////////////////////////////
			log.Info("Warming up the JIT...");
			for (int i_3 = 0; i_3 < 10; i_3++)
			{
				log.Info(i_3);
				Gameplay(r, trainingSet[i_3], weights, humanFeatureVectors);
			}
			log.Info("Timing actual run...");
			long start = Runtime.CurrentTimeMillis();
			for (int i_4 = 0; i_4 < 10; i_4++)
			{
				log.Info(i_4);
				Gameplay(r, trainingSet[i_4], weights, humanFeatureVectors);
			}
			long duration = Runtime.CurrentTimeMillis() - start;
			log.Info("Duration: " + duration);
		}

		//////////////////////////////////////////////////////////////
		// This is an implementation of something like MCTS, trying to take advantage of the general speed gains due to fast
		// CliqueTree caching of dot products. It doesn't actually do any clever selection, preferring to select observations
		// at random.
		//////////////////////////////////////////////////////////////
		private static void Gameplay(Random r, GraphicalModel model, ConcatVector weights, ConcatVector[] humanFeatureVectors)
		{
			IList<int> variablesList = new List<int>();
			IList<int> variableSizesList = new List<int>();
			foreach (GraphicalModel.Factor f in model.factors)
			{
				for (int i = 0; i < f.neigborIndices.Length; i++)
				{
					int j = f.neigborIndices[i];
					if (!variablesList.Contains(j))
					{
						variablesList.Add(j);
						variableSizesList.Add(f.featuresTable.GetDimensions()[i]);
					}
				}
			}
			int[] variables = variablesList.Stream().MapToInt(null).ToArray();
			int[] variableSizes = variableSizesList.Stream().MapToInt(null).ToArray();
			IList<GamePlayerBenchmark.SampleState> childrenOfRoot = new List<GamePlayerBenchmark.SampleState>();
			CliqueTree tree = new CliqueTree(model, weights);
			int initialFactors = model.factors.Count;
			// Run some "samples"
			long start = Runtime.CurrentTimeMillis();
			long marginalsTime = 0;
			for (int i_1 = 0; i_1 < 1000; i_1++)
			{
				log.Info("\tTaking sample " + i_1);
				Stack<GamePlayerBenchmark.SampleState> stack = new Stack<GamePlayerBenchmark.SampleState>();
				GamePlayerBenchmark.SampleState state = SelectOrCreateChildAtRandom(r, model, variables, variableSizes, childrenOfRoot, humanFeatureVectors);
				long localMarginalsTime = 0;
				// Each "sample" is 10 moves deep
				for (int j = 0; j < 10; j++)
				{
					// log.info("\t\tFrame "+j);
					state.Push(model);
					System.Diagnostics.Debug.Assert((model.factors.Count == initialFactors + j + 1));
					///////////////////////////////////////////////////////////
					// This is the thing we're really benchmarking
					///////////////////////////////////////////////////////////
					if (state.cachedMarginal == null)
					{
						long s = Runtime.CurrentTimeMillis();
						state.cachedMarginal = tree.CalculateMarginalsJustSingletons();
						localMarginalsTime += Runtime.CurrentTimeMillis() - s;
					}
					stack.Push(state);
					state = SelectOrCreateChildAtRandom(r, model, variables, variableSizes, state.children, humanFeatureVectors);
				}
				log.Info("\t\t" + localMarginalsTime + " ms");
				marginalsTime += localMarginalsTime;
				while (!stack.Empty())
				{
					stack.Pop().Pop(model);
				}
				System.Diagnostics.Debug.Assert((model.factors.Count == initialFactors));
			}
			log.Info("Marginals time: " + marginalsTime + " ms");
			log.Info("Avg time per marginal: " + (marginalsTime / 200) + " ms");
			log.Info("Total time: " + (Runtime.CurrentTimeMillis() - start));
		}

		private static GamePlayerBenchmark.SampleState SelectOrCreateChildAtRandom(Random r, GraphicalModel model, int[] variables, int[] variableSizes, IList<GamePlayerBenchmark.SampleState> children, ConcatVector[] humanFeatureVectors)
		{
			int i = r.NextInt(variables.Length);
			int variable = variables[i];
			int observation = r.NextInt(variableSizes[i]);
			foreach (GamePlayerBenchmark.SampleState s in children)
			{
				if (s.variable == variable && s.observation == observation)
				{
					return s;
				}
			}
			int humanObservationVariable = 0;
			foreach (GraphicalModel.Factor f in model.factors)
			{
				foreach (int j in f.neigborIndices)
				{
					if (j >= humanObservationVariable)
					{
						humanObservationVariable = j + 1;
					}
				}
			}
			GraphicalModel.Factor f_1 = model.AddFactor(new int[] { variable, humanObservationVariable }, new int[] { variableSizes[i], variableSizes[i] }, null);
			model.factors.Remove(f_1);
			GamePlayerBenchmark.SampleState newState = new GamePlayerBenchmark.SampleState(f_1, variable, observation);
			children.Add(newState);
			return newState;
		}

		public class SampleState
		{
			public GraphicalModel.Factor addedFactor;

			public int variable;

			public int observation;

			public IList<GamePlayerBenchmark.SampleState> children = new List<GamePlayerBenchmark.SampleState>();

			public double[][] cachedMarginal = null;

			public SampleState(GraphicalModel.Factor addedFactor, int variable, int observation)
			{
				this.addedFactor = addedFactor;
				this.variable = variable;
				this.observation = observation;
			}

			/// <summary>This applies this SampleState to the model.</summary>
			/// <remarks>
			/// This applies this SampleState to the model. The name comes from an analogy to a stack. If we take a sample
			/// path, involving a number of steps through the model, we push() each SampleState onto the model one at a time,
			/// then when we return from the sample we can pop() each SampleState off the model, and be left with our
			/// original model state.
			/// </remarks>
			/// <param name="model">the model to push this SampleState onto</param>
			public virtual void Push(GraphicalModel model)
			{
				System.Diagnostics.Debug.Assert((!model.factors.Contains(addedFactor)));
				model.factors.Add(addedFactor);
				model.GetVariableMetaDataByReference(variable)[CliqueTree.VariableObservedValue] = string.Empty + observation;
			}

			/// <summary>See push() for an explanation.</summary>
			/// <param name="model">the model to pop this SampleState from</param>
			public virtual void Pop(GraphicalModel model)
			{
				System.Diagnostics.Debug.Assert((model.factors.Contains(addedFactor)));
				model.factors.Remove(addedFactor);
				Sharpen.Collections.Remove(model.GetVariableMetaDataByReference(variable), CliqueTree.VariableObservedValue);
			}
		}
	}
}
