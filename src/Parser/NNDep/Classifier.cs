using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Nndep
{
	/// <summary>Neural network classifier which powers a transition-based dependency parser.</summary>
	/// <remarks>
	/// Neural network classifier which powers a transition-based dependency parser.
	/// This classifier is built to accept distributed-representation
	/// inputs, and feeds back errors to these input layers as it learns.
	/// <p>
	/// In order to train a classifier, instantiate this class using the
	/// <see cref="Classifier(Config, Dataset, double[][], double[][], double[], double[][], System.Collections.Generic.IList{E})"/>
	/// constructor. (The presence of a non-null dataset signals that we
	/// wish to train.) After training by alternating calls to
	/// <see cref="ComputeCostFunction(int, double, double)"/>
	/// and,
	/// <see cref="TakeAdaGradientStep(Cost, double, double)"/>
	/// ,
	/// be sure to call
	/// <see cref="FinalizeTraining()"/>
	/// in order to allow the
	/// classifier to clean up resources used during training.
	/// </remarks>
	/// <author>Danqi Chen</author>
	/// <author>Jon Gauthier</author>
	public class Classifier
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Nndep.Classifier));

		private readonly double[][] W1;

		private readonly double[][] W2;

		private readonly double[][] E;

		private readonly double[] b1;

		private double[][] gradSaved;

		private double[][] eg2W1;

		private double[][] eg2W2;

		private double[][] eg2E;

		private double[] eg2b1;

		/// <summary>Pre-computed hidden layer unit activations.</summary>
		/// <remarks>
		/// Pre-computed hidden layer unit activations. Each double array
		/// within this data is an entire hidden layer. The sub-arrays are
		/// indexed somewhat arbitrarily; in order to find hidden-layer unit
		/// activations for a given feature ID, use
		/// <see cref="preMap"/>
		/// to find
		/// the proper index into this data.
		/// </remarks>
		private double[][] saved;

		/// <summary>Describes features which should be precomputed.</summary>
		/// <remarks>
		/// Describes features which should be precomputed. Each entry maps a
		/// feature ID to its destined index in the saved hidden unit
		/// activation data (see
		/// <see cref="saved"/>
		/// ).
		/// </remarks>
		private readonly IDictionary<int, int> preMap;

		/// <summary>
		/// Initial training state is dependent on how the classifier is
		/// initialized.
		/// </summary>
		/// <remarks>
		/// Initial training state is dependent on how the classifier is
		/// initialized. We use this flag to determine whether calls to
		/// <see cref="ComputeCostFunction(int, double, double)"/>
		/// , etc. are valid.
		/// </remarks>
		private bool isTraining;

		/// <summary>All training examples.</summary>
		private readonly Dataset dataset;

		/// <summary>We use MulticoreWrapper to parallelize mini-batch training.</summary>
		/// <remarks>
		/// We use MulticoreWrapper to parallelize mini-batch training.
		/// <p>
		/// Threaded job input: partition of minibatch;
		/// current weights + params
		/// Threaded job output: cost value, weight gradients for partition of
		/// minibatch
		/// </remarks>
		private readonly MulticoreWrapper<Pair<ICollection<Example>, Classifier.FeedforwardParams>, Classifier.Cost> jobHandler;

		private readonly Config config;

		/// <summary>
		/// Number of possible dependency relation labels among which this
		/// classifier will choose.
		/// </summary>
		private readonly int numLabels;

		/// <summary>
		/// Instantiate a classifier with previously learned parameters in
		/// order to perform new inference.
		/// </summary>
		/// <param name="config"/>
		/// <param name="E"/>
		/// <param name="W1"/>
		/// <param name="b1"/>
		/// <param name="W2"/>
		/// <param name="preComputed"/>
		public Classifier(Config config, double[][] E, double[][] W1, double[] b1, double[][] W2, IList<int> preComputed)
			: this(config, null, E, W1, b1, W2, preComputed)
		{
		}

		/// <summary>
		/// Instantiate a classifier with training data and randomly
		/// initialized parameter matrices in order to begin training.
		/// </summary>
		/// <param name="config"/>
		/// <param name="dataset"/>
		/// <param name="E"/>
		/// <param name="W1"/>
		/// <param name="b1"/>
		/// <param name="W2"/>
		/// <param name="preComputed"/>
		public Classifier(Config config, Dataset dataset, double[][] E, double[][] W1, double[] b1, double[][] W2, IList<int> preComputed)
		{
			// E: numFeatures x embeddingSize
			// W1: hiddenSize x (embeddingSize x numFeatures)
			// b1: hiddenSize
			// W2: numLabels x hiddenSize
			// Weight matrices
			// Global gradSaved
			// Gradient histories
			this.config = config;
			this.dataset = dataset;
			this.E = E;
			this.W1 = W1;
			this.b1 = b1;
			this.W2 = W2;
			InitGradientHistories();
			numLabels = W2.Length;
			preMap = new Dictionary<int, int>();
			for (int i = 0; i < preComputed.Count && i < config.numPreComputed; ++i)
			{
				preMap[preComputed[i]] = i;
			}
			isTraining = dataset != null;
			if (isTraining)
			{
				jobHandler = new MulticoreWrapper<Pair<ICollection<Example>, Classifier.FeedforwardParams>, Classifier.Cost>(config.trainingThreads, new Classifier.CostFunction(this), false);
			}
			else
			{
				jobHandler = null;
			}
		}

		/// <summary>
		/// Evaluates the training cost of a particular subset of training
		/// examples given the current learned weights.
		/// </summary>
		/// <remarks>
		/// Evaluates the training cost of a particular subset of training
		/// examples given the current learned weights.
		/// This function will be evaluated in parallel on different data in
		/// separate threads, and accesses the classifier's weights stored in
		/// the outer class instance.
		/// Each nested class instance accumulates its own weight gradients;
		/// these gradients will be merged on a main thread after all cost
		/// function runs complete.
		/// </remarks>
		/// <seealso cref="Classifier.ComputeCostFunction(int, double, double)"/>
		private class CostFunction : IThreadsafeProcessor<Pair<ICollection<Example>, Classifier.FeedforwardParams>, Classifier.Cost>
		{
			private double[][] gradW1;

			private double[] gradb1;

			private double[][] gradW2;

			private double[][] gradE;

			public virtual Classifier.Cost Process(Pair<ICollection<Example>, Classifier.FeedforwardParams> input)
			{
				ICollection<Example> examples = input.First();
				Classifier.FeedforwardParams @params = input.Second();
				// We can't fix the seed used with ThreadLocalRandom
				// TODO: Is this a serious problem?
				ThreadLocalRandom random = ThreadLocalRandom.Current();
				this.gradW1 = new double[this._enclosing.W1.Length][];
				this.gradb1 = new double[this._enclosing.b1.Length];
				this.gradW2 = new double[this._enclosing.W2.Length][];
				this.gradE = new double[this._enclosing.E.Length][];
				double cost = 0.0;
				double correct = 0.0;
				foreach (Example ex in examples)
				{
					IList<int> feature = ex.GetFeature();
					IList<int> label = ex.GetLabel();
					double[] scores = new double[this._enclosing.numLabels];
					double[] hidden = new double[this._enclosing.config.hiddenSize];
					double[] hidden3 = new double[this._enclosing.config.hiddenSize];
					// Run dropout: randomly drop some hidden-layer units. `ls`
					// contains the indices of those units which are still active
					int[] ls = IIntStream.Range(0, this._enclosing.config.hiddenSize).Filter(null).ToArray();
					int offset = 0;
					for (int j = 0; j < this._enclosing.config.numTokens; ++j)
					{
						int tok = feature[j];
						int index = tok * this._enclosing.config.numTokens + j;
						if (this._enclosing.preMap.Contains(index))
						{
							// Unit activations for this input feature value have been
							// precomputed
							int id = this._enclosing.preMap[index];
							// Only extract activations for those nodes which are still
							// activated (`ls`)
							foreach (int nodeIndex in ls)
							{
								hidden[nodeIndex] += this._enclosing.saved[id][nodeIndex];
							}
						}
						else
						{
							foreach (int nodeIndex in ls)
							{
								for (int k = 0; k < this._enclosing.config.embeddingSize; ++k)
								{
									hidden[nodeIndex] += this._enclosing.W1[nodeIndex][offset + k] * this._enclosing.E[tok][k];
								}
							}
						}
						offset += this._enclosing.config.embeddingSize;
					}
					// Add bias term and apply activation function
					foreach (int nodeIndex_1 in ls)
					{
						hidden[nodeIndex_1] += this._enclosing.b1[nodeIndex_1];
						hidden3[nodeIndex_1] = Math.Pow(hidden[nodeIndex_1], 3);
					}
					// Feed forward to softmax layer (no activation yet)
					int optLabel = -1;
					for (int i = 0; i < this._enclosing.numLabels; ++i)
					{
						if (label[i] >= 0)
						{
							foreach (int nodeIndex in ls)
							{
								scores[i] += this._enclosing.W2[i][nodeIndex_1] * hidden3[nodeIndex_1];
							}
							if (optLabel < 0 || scores[i] > scores[optLabel])
							{
								optLabel = i;
							}
						}
					}
					double sum1 = 0.0;
					double sum2 = 0.0;
					double maxScore = scores[optLabel];
					for (int i_1 = 0; i_1 < this._enclosing.numLabels; ++i_1)
					{
						if (label[i_1] >= 0)
						{
							scores[i_1] = Math.Exp(scores[i_1] - maxScore);
							if (label[i_1] == 1)
							{
								sum1 += scores[i_1];
							}
							sum2 += scores[i_1];
						}
					}
					cost += (Math.Log(sum2) - Math.Log(sum1)) / @params.GetBatchSize();
					if (label[optLabel] == 1)
					{
						correct += +1.0 / @params.GetBatchSize();
					}
					double[] gradHidden3 = new double[this._enclosing.config.hiddenSize];
					for (int i_2 = 0; i_2 < this._enclosing.numLabels; ++i_2)
					{
						if (label[i_2] >= 0)
						{
							double delta = -(label[i_2] - scores[i_2] / sum2) / @params.GetBatchSize();
							foreach (int nodeIndex in ls)
							{
								this.gradW2[i_2][nodeIndex_1] += delta * hidden3[nodeIndex_1];
								gradHidden3[nodeIndex_1] += delta * this._enclosing.W2[i_2][nodeIndex_1];
							}
						}
					}
					double[] gradHidden = new double[this._enclosing.config.hiddenSize];
					foreach (int nodeIndex_2 in ls)
					{
						gradHidden[nodeIndex_2] = gradHidden3[nodeIndex_2] * 3 * hidden[nodeIndex_2] * hidden[nodeIndex_2];
						this.gradb1[nodeIndex_2] += gradHidden[nodeIndex_2];
					}
					offset = 0;
					for (int j_1 = 0; j_1 < this._enclosing.config.numTokens; ++j_1)
					{
						int tok = feature[j_1];
						int index = tok * this._enclosing.config.numTokens + j_1;
						if (this._enclosing.preMap.Contains(index))
						{
							int id = this._enclosing.preMap[index];
							foreach (int nodeIndex in ls)
							{
								this._enclosing.gradSaved[id][nodeIndex_2] += gradHidden[nodeIndex_2];
							}
						}
						else
						{
							foreach (int nodeIndex in ls)
							{
								for (int k = 0; k < this._enclosing.config.embeddingSize; ++k)
								{
									this.gradW1[nodeIndex_2][offset + k] += gradHidden[nodeIndex_2] * this._enclosing.E[tok][k];
									this.gradE[tok][k] += gradHidden[nodeIndex_2] * this._enclosing.W1[nodeIndex_2][offset + k];
								}
							}
						}
						offset += this._enclosing.config.embeddingSize;
					}
				}
				return new Classifier.Cost(this, cost, correct, this.gradW1, this.gradb1, this.gradW2, this.gradE);
			}

			/// <summary>Return a new threadsafe instance.</summary>
			public virtual IThreadsafeProcessor<Pair<ICollection<Example>, Classifier.FeedforwardParams>, Classifier.Cost> NewInstance()
			{
				return new Classifier.CostFunction(this);
			}

			internal CostFunction(Classifier _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly Classifier _enclosing;
		}

		/// <summary>
		/// Describes the parameters for a particular invocation of a cost
		/// function.
		/// </summary>
		private class FeedforwardParams
		{
			/// <summary>
			/// Size of the entire mini-batch (not just the chunk that might be
			/// fed-forward at this moment).
			/// </summary>
			private readonly int batchSize;

			private readonly double dropOutProb;

			private FeedforwardParams(int batchSize, double dropOutProb)
			{
				this.batchSize = batchSize;
				this.dropOutProb = dropOutProb;
			}

			public virtual int GetBatchSize()
			{
				return batchSize;
			}

			public virtual double GetDropOutProb()
			{
				return dropOutProb;
			}
		}

		/// <summary>
		/// Describes the result of feedforward + backpropagation through
		/// the neural network for the batch provided to a `CostFunction.`
		/// <p>
		/// The members of this class represent weight deltas computed by
		/// backpropagation.
		/// </summary>
		/// <seealso cref="CostFunction"/>
		public class Cost
		{
			private double cost;

			private double percentCorrect;

			private readonly double[][] gradW1;

			private readonly double[] gradb1;

			private readonly double[][] gradW2;

			private readonly double[][] gradE;

			private Cost(Classifier _enclosing, double cost, double percentCorrect, double[][] gradW1, double[] gradb1, double[][] gradW2, double[][] gradE)
			{
				this._enclosing = _enclosing;
				// Percent of training examples predicted correctly
				// Weight deltas
				this.cost = cost;
				this.percentCorrect = percentCorrect;
				this.gradW1 = gradW1;
				this.gradb1 = gradb1;
				this.gradW2 = gradW2;
				this.gradE = gradE;
			}

			/// <summary>
			/// Merge the given
			/// <c>Cost</c>
			/// data with the data in this
			/// instance.
			/// </summary>
			/// <param name="otherCost"/>
			public virtual void Merge(Classifier.Cost otherCost)
			{
				this.cost += otherCost.GetCost();
				this.percentCorrect += otherCost.GetPercentCorrect();
				ArrayMath.AddInPlace(this.gradW1, otherCost.GetGradW1());
				ArrayMath.PairwiseAddInPlace(this.gradb1, otherCost.GetGradb1());
				ArrayMath.AddInPlace(this.gradW2, otherCost.GetGradW2());
				ArrayMath.AddInPlace(this.gradE, otherCost.GetGradE());
			}

			/// <summary>
			/// Backpropagate gradient values from gradSaved into the gradients
			/// for the E vectors that generated them.
			/// </summary>
			/// <param name="featuresSeen">
			/// Feature IDs observed during training for
			/// which gradSaved values need to be backprop'd
			/// into gradE
			/// </param>
			private void BackpropSaved(ICollection<int> featuresSeen)
			{
				foreach (int x in featuresSeen)
				{
					int mapX = this._enclosing.preMap[x];
					int tok = x / this._enclosing.config.numTokens;
					int offset = (x % this._enclosing.config.numTokens) * this._enclosing.config.embeddingSize;
					for (int j = 0; j < this._enclosing.config.hiddenSize; ++j)
					{
						double delta = this._enclosing.gradSaved[mapX][j];
						for (int k = 0; k < this._enclosing.config.embeddingSize; ++k)
						{
							this.gradW1[j][offset + k] += delta * this._enclosing.E[tok][k];
							this.gradE[tok][k] += delta * this._enclosing.W1[j][offset + k];
						}
					}
				}
			}

			/// <summary>
			/// Add L2 regularization cost to the gradients associated with this
			/// instance.
			/// </summary>
			private void AddL2Regularization(double regularizationWeight)
			{
				for (int i = 0; i < this._enclosing.W1.Length; ++i)
				{
					for (int j = 0; j < this._enclosing.W1[i].Length; ++j)
					{
						this.cost += regularizationWeight * this._enclosing.W1[i][j] * this._enclosing.W1[i][j] / 2.0;
						this.gradW1[i][j] += regularizationWeight * this._enclosing.W1[i][j];
					}
				}
				for (int i_1 = 0; i_1 < this._enclosing.b1.Length; ++i_1)
				{
					this.cost += regularizationWeight * this._enclosing.b1[i_1] * this._enclosing.b1[i_1] / 2.0;
					this.gradb1[i_1] += regularizationWeight * this._enclosing.b1[i_1];
				}
				for (int i_2 = 0; i_2 < this._enclosing.W2.Length; ++i_2)
				{
					for (int j = 0; j < this._enclosing.W2[i_2].Length; ++j)
					{
						this.cost += regularizationWeight * this._enclosing.W2[i_2][j] * this._enclosing.W2[i_2][j] / 2.0;
						this.gradW2[i_2][j] += regularizationWeight * this._enclosing.W2[i_2][j];
					}
				}
				for (int i_3 = 0; i_3 < this._enclosing.E.Length; ++i_3)
				{
					for (int j = 0; j < this._enclosing.E[i_3].Length; ++j)
					{
						this.cost += regularizationWeight * this._enclosing.E[i_3][j] * this._enclosing.E[i_3][j] / 2.0;
						this.gradE[i_3][j] += regularizationWeight * this._enclosing.E[i_3][j];
					}
				}
			}

			public virtual double GetCost()
			{
				return this.cost;
			}

			public virtual double GetPercentCorrect()
			{
				return this.percentCorrect;
			}

			public virtual double[][] GetGradW1()
			{
				return this.gradW1;
			}

			public virtual double[] GetGradb1()
			{
				return this.gradb1;
			}

			public virtual double[][] GetGradW2()
			{
				return this.gradW2;
			}

			public virtual double[][] GetGradE()
			{
				return this.gradE;
			}

			private readonly Classifier _enclosing;
		}

		/// <summary>
		/// Determine the feature IDs which need to be pre-computed for
		/// training with these examples.
		/// </summary>
		private ICollection<int> GetToPreCompute(IList<Example> examples)
		{
			ICollection<int> featureIDs = new HashSet<int>();
			foreach (Example ex in examples)
			{
				IList<int> feature = ex.GetFeature();
				for (int j = 0; j < config.numTokens; j++)
				{
					int tok = feature[j];
					int index = tok * config.numTokens + j;
					if (preMap.Contains(index))
					{
						featureIDs.Add(index);
					}
				}
			}
			double percentagePreComputed = featureIDs.Count / (float)config.numPreComputed;
			log.Info(string.Format("Percent actually necessary to pre-compute: %f%%%n", percentagePreComputed * 100));
			return featureIDs;
		}

		/// <summary>
		/// Determine the total cost on the dataset associated with this
		/// classifier using the current learned parameters.
		/// </summary>
		/// <remarks>
		/// Determine the total cost on the dataset associated with this
		/// classifier using the current learned parameters. This cost is
		/// evaluated using mini-batch adaptive gradient descent.
		/// This method launches multiple threads, each of which evaluates
		/// training cost on a partition of the mini-batch.
		/// </remarks>
		/// <param name="batchSize"/>
		/// <param name="regParameter">Regularization parameter (lambda)</param>
		/// <param name="dropOutProb">
		/// Drop-out probability. Hidden-layer units in the
		/// neural network will be randomly turned off
		/// while training a particular example with this
		/// probability.
		/// </param>
		/// <returns>
		/// A
		/// <see cref="Cost"/>
		/// object which describes the total cost of the given
		/// weights, and includes gradients to be used for further
		/// training
		/// </returns>
		public virtual Classifier.Cost ComputeCostFunction(int batchSize, double regParameter, double dropOutProb)
		{
			ValidateTraining();
			IList<Example> examples = Edu.Stanford.Nlp.Parser.Nndep.Util.GetRandomSubList(dataset.examples, batchSize);
			// Redo precomputations for only those features which are triggered
			// by examples in this mini-batch.
			ICollection<int> toPreCompute = GetToPreCompute(examples);
			PreCompute(toPreCompute);
			// Set up parameters for feedforward
			Classifier.FeedforwardParams @params = new Classifier.FeedforwardParams(batchSize, dropOutProb);
			// Zero out saved-embedding gradients
			gradSaved = new double[][] {  };
			int numChunks = config.trainingThreads;
			IList<IList<Example>> chunks = CollectionUtils.PartitionIntoFolds(examples, numChunks);
			// Submit chunks for processing on separate threads
			foreach (ICollection<Example> chunk in chunks)
			{
				jobHandler.Put(new Pair<ICollection<Example>, Classifier.FeedforwardParams>(chunk, @params));
			}
			jobHandler.Join(false);
			// Join costs from each chunk
			Classifier.Cost cost = null;
			while (jobHandler.Peek())
			{
				Classifier.Cost otherCost = jobHandler.Poll();
				if (cost == null)
				{
					cost = otherCost;
				}
				else
				{
					cost.Merge(otherCost);
				}
			}
			if (cost == null)
			{
				return null;
			}
			// Backpropagate gradients on saved pre-computed values to actual
			// embeddings
			cost.BackpropSaved(toPreCompute);
			cost.AddL2Regularization(regParameter);
			return cost;
		}

		/// <summary>
		/// Update classifier weights using the given training cost
		/// information.
		/// </summary>
		/// <param name="cost">
		/// Cost information as returned by
		/// <see cref="ComputeCostFunction(int, double, double)"/>
		/// .
		/// </param>
		/// <param name="adaAlpha">Global AdaGrad learning rate</param>
		/// <param name="adaEps">
		/// Epsilon value for numerical stability in AdaGrad's
		/// division
		/// </param>
		public virtual void TakeAdaGradientStep(Classifier.Cost cost, double adaAlpha, double adaEps)
		{
			ValidateTraining();
			double[][] gradW1 = cost.GetGradW1();
			double[][] gradW2 = cost.GetGradW2();
			double[][] gradE = cost.GetGradE();
			double[] gradb1 = cost.GetGradb1();
			for (int i = 0; i < W1.Length; ++i)
			{
				for (int j = 0; j < W1[i].Length; ++j)
				{
					eg2W1[i][j] += gradW1[i][j] * gradW1[i][j];
					W1[i][j] -= adaAlpha * gradW1[i][j] / System.Math.Sqrt(eg2W1[i][j] + adaEps);
				}
			}
			for (int i_1 = 0; i_1 < b1.Length; ++i_1)
			{
				eg2b1[i_1] += gradb1[i_1] * gradb1[i_1];
				b1[i_1] -= adaAlpha * gradb1[i_1] / System.Math.Sqrt(eg2b1[i_1] + adaEps);
			}
			for (int i_2 = 0; i_2 < W2.Length; ++i_2)
			{
				for (int j = 0; j < W2[i_2].Length; ++j)
				{
					eg2W2[i_2][j] += gradW2[i_2][j] * gradW2[i_2][j];
					W2[i_2][j] -= adaAlpha * gradW2[i_2][j] / System.Math.Sqrt(eg2W2[i_2][j] + adaEps);
				}
			}
			if (config.doWordEmbeddingGradUpdate)
			{
				for (int i_3 = 0; i_3 < E.Length; ++i_3)
				{
					for (int j = 0; j < E[i_3].Length; ++j)
					{
						eg2E[i_3][j] += gradE[i_3][j] * gradE[i_3][j];
						E[i_3][j] -= adaAlpha * gradE[i_3][j] / System.Math.Sqrt(eg2E[i_3][j] + adaEps);
					}
				}
			}
		}

		private void InitGradientHistories()
		{
			eg2E = new double[E.Length][];
			eg2W1 = new double[W1.Length][];
			eg2b1 = new double[b1.Length];
			eg2W2 = new double[W2.Length][];
		}

		/// <summary>Clear all gradient histories used for AdaGrad training.</summary>
		/// <exception cref="System.InvalidOperationException">If not training</exception>
		public virtual void ClearGradientHistories()
		{
			ValidateTraining();
			InitGradientHistories();
		}

		private void ValidateTraining()
		{
			if (!isTraining)
			{
				throw new InvalidOperationException("Not training, or training was already finalized");
			}
		}

		/// <summary>Finish training this classifier; prepare for a shutdown.</summary>
		public virtual void FinalizeTraining()
		{
			ValidateTraining();
			// Destroy threadpool
			jobHandler.Join(true);
			isTraining = false;
		}

		/// <seealso cref="PreCompute(System.Collections.Generic.ICollection{E})"/>
		public virtual void PreCompute()
		{
			PreCompute(preMap.Keys);
		}

		/// <summary>
		/// Pre-compute hidden layer activations for some set of possible
		/// feature inputs.
		/// </summary>
		/// <param name="toPreCompute">
		/// Set of feature IDs for which hidden layer
		/// activations should be precomputed
		/// </param>
		public virtual void PreCompute(ICollection<int> toPreCompute)
		{
			long startTime = Runtime.CurrentTimeMillis();
			// NB: It'd make sense to just make the first dimension of this
			// array the same size as `toPreCompute`, then recalculate all
			// `preMap` indices to map into this denser array. But this
			// actually hurt training performance! (See experiments with
			// "smallMap.")
			saved = new double[][] {  };
			int numTokens = config.numTokens;
			int embeddingSize = config.embeddingSize;
			foreach (int x in toPreCompute)
			{
				int mapX = preMap[x];
				int tok = x / numTokens;
				int pos = x % numTokens;
				MatrixMultiplySliceSum(saved[mapX], W1, E[tok], pos * embeddingSize);
			}
			log.Info("PreComputed " + toPreCompute.Count + ", Elapsed Time: " + (Runtime.CurrentTimeMillis() - startTime) / 1000.0 + " (s)");
		}

		internal virtual double[] ComputeScores(int[] feature)
		{
			return ComputeScores(feature, preMap);
		}

		/// <summary>Feed a feature vector forward through the network.</summary>
		/// <remarks>
		/// Feed a feature vector forward through the network. Returns the
		/// values of the output layer.
		/// </remarks>
		private double[] ComputeScores(int[] feature, IDictionary<int, int> preMap)
		{
			double[] hidden = new double[config.hiddenSize];
			int numTokens = config.numTokens;
			int embeddingSize = config.embeddingSize;
			int offset = 0;
			for (int j = 0; j < feature.Length; j++)
			{
				int tok = feature[j];
				int index = tok * numTokens + j;
				int idInteger = preMap[index];
				if (idInteger != null)
				{
					ArrayMath.PairwiseAddInPlace(hidden, saved[idInteger]);
				}
				else
				{
					MatrixMultiplySliceSum(hidden, W1, E[tok], offset);
				}
				offset += embeddingSize;
			}
			AddCubeInPlace(hidden, b1);
			return MatrixMultiply(W2, hidden);
		}

		// extracting these small methods makes things faster; hotspot likes them
		private static double[] MatrixMultiply(double[][] matrix, double[] vector)
		{
			double[] result = new double[matrix.Length];
			for (int i = 0; i < matrix.Length; i++)
			{
				result[i] = ArrayMath.DotProduct(matrix[i], vector);
			}
			return result;
		}

		private static void MatrixMultiplySliceSum(double[] sum, double[][] matrix, double[] vector, int leftColumnOffset)
		{
			for (int i = 0; i < matrix.Length; i++)
			{
				for (int j = 0; j < vector.Length; j++)
				{
					sum[i] += matrix[i][leftColumnOffset + j] * vector[j];
				}
			}
		}

		private static void AddCubeInPlace(double[] vector, double[] bias)
		{
			for (int i = 0; i < vector.Length; i++)
			{
				vector[i] += bias[i];
				// add bias
				vector[i] = vector[i] * vector[i] * vector[i];
			}
		}

		// cube nonlinearity
		public virtual double[][] GetW1()
		{
			return W1;
		}

		public virtual double[] Getb1()
		{
			return b1;
		}

		public virtual double[][] GetW2()
		{
			return W2;
		}

		public virtual double[][] GetE()
		{
			return E;
		}
	}
}
