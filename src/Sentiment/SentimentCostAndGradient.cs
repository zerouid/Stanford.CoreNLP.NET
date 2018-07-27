using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Neural;
using Edu.Stanford.Nlp.Neural.Rnn;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;

using Org.Ejml.Simple;


namespace Edu.Stanford.Nlp.Sentiment
{
	public class SentimentCostAndGradient : AbstractCachingDiffFunction
	{
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sentiment.SentimentCostAndGradient));

		private readonly SentimentModel model;

		private readonly IList<Tree> trainingBatch;

		public SentimentCostAndGradient(SentimentModel model, IList<Tree> trainingBatch)
		{
			// TODO: get rid of the word Sentiment everywhere
			this.model = model;
			this.trainingBatch = trainingBatch;
		}

		public override int DomainDimension()
		{
			// TODO: cache this for speed?
			return model.TotalParamSize();
		}

		private static double SumError(Tree tree)
		{
			if (tree.IsLeaf())
			{
				return 0.0;
			}
			else
			{
				if (tree.IsPreTerminal())
				{
					return RNNCoreAnnotations.GetPredictionError(tree);
				}
				else
				{
					double error = 0.0;
					foreach (Tree child in tree.Children())
					{
						error += SumError(child);
					}
					return RNNCoreAnnotations.GetPredictionError(tree) + error;
				}
			}
		}

		/// <summary>
		/// Returns the index with the highest value in the
		/// <paramref name="predictions"/>
		/// matrix.
		/// Indexed from 0.
		/// </summary>
		private static int GetPredictedClass(SimpleMatrix predictions)
		{
			int argmax = 0;
			for (int i = 1; i < predictions.GetNumElements(); ++i)
			{
				if (predictions.Get(i) > predictions.Get(argmax))
				{
					argmax = i;
				}
			}
			return argmax;
		}

		private class ModelDerivatives
		{
			public readonly TwoDimensionalMap<string, string, SimpleMatrix> binaryTD;

			public readonly TwoDimensionalMap<string, string, SimpleTensor> binaryTensorTD;

			public readonly TwoDimensionalMap<string, string, SimpleMatrix> binaryCD;

			public readonly IDictionary<string, SimpleMatrix> unaryCD;

			public readonly IDictionary<string, SimpleMatrix> wordVectorD;

			public double error = 0.0;

			public ModelDerivatives(SentimentModel model)
			{
				// We use TreeMap for each of these so that they stay in a canonical sorted order
				// binaryTD stands for Transform Derivatives (see the SentimentModel)
				// the derivatives of the tensors for the binary nodes
				// will be empty if we aren't using tensors
				// binaryCD stands for Classification Derivatives
				// if we combined classification derivatives, we just use an empty map
				// unaryCD stands for Classification Derivatives
				// word vector derivatives
				// will be filled on an as-needed basis, as opposed to having all
				// the words with a lot of empty vectors
				binaryTD = InitDerivatives(model.binaryTransform);
				binaryTensorTD = (model.op.useTensors) ? InitTensorDerivatives(model.binaryTensors) : TwoDimensionalMap.TreeMap();
				binaryCD = (!model.op.combineClassification) ? InitDerivatives(model.binaryClassification) : TwoDimensionalMap.TreeMap();
				unaryCD = InitDerivatives(model.unaryClassification);
				// wordVectorD will be filled on an as-needed basis
				wordVectorD = Generics.NewTreeMap();
			}

			public virtual void Add(SentimentCostAndGradient.ModelDerivatives other)
			{
				AddMatrices(binaryTD, other.binaryTD);
				AddTensors(binaryTensorTD, other.binaryTensorTD);
				AddMatrices(binaryCD, other.binaryCD);
				AddMatrices(unaryCD, other.unaryCD);
				AddMatrices(wordVectorD, other.wordVectorD);
				error += other.error;
			}

			/// <summary>Add matrices from the second map to the first map, in place.</summary>
			public static void AddMatrices(TwoDimensionalMap<string, string, SimpleMatrix> first, TwoDimensionalMap<string, string, SimpleMatrix> second)
			{
				foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry in first)
				{
					if (second.Contains(entry.GetFirstKey(), entry.GetSecondKey()))
					{
						first.Put(entry.GetFirstKey(), entry.GetSecondKey(), entry.GetValue().Plus(second.Get(entry.GetFirstKey(), entry.GetSecondKey())));
					}
				}
				foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry_1 in second)
				{
					if (!first.Contains(entry_1.GetFirstKey(), entry_1.GetSecondKey()))
					{
						first.Put(entry_1.GetFirstKey(), entry_1.GetSecondKey(), entry_1.GetValue());
					}
				}
			}

			/// <summary>Add tensors from the second map to the first map, in place.</summary>
			public static void AddTensors(TwoDimensionalMap<string, string, SimpleTensor> first, TwoDimensionalMap<string, string, SimpleTensor> second)
			{
				foreach (TwoDimensionalMap.Entry<string, string, SimpleTensor> entry in first)
				{
					if (second.Contains(entry.GetFirstKey(), entry.GetSecondKey()))
					{
						first.Put(entry.GetFirstKey(), entry.GetSecondKey(), entry.GetValue().Plus(second.Get(entry.GetFirstKey(), entry.GetSecondKey())));
					}
				}
				foreach (TwoDimensionalMap.Entry<string, string, SimpleTensor> entry_1 in second)
				{
					if (!first.Contains(entry_1.GetFirstKey(), entry_1.GetSecondKey()))
					{
						first.Put(entry_1.GetFirstKey(), entry_1.GetSecondKey(), entry_1.GetValue());
					}
				}
			}

			/// <summary>Add matrices from the second map to the first map, in place.</summary>
			public static void AddMatrices(IDictionary<string, SimpleMatrix> first, IDictionary<string, SimpleMatrix> second)
			{
				foreach (KeyValuePair<string, SimpleMatrix> entry in first)
				{
					if (second.Contains(entry.Key))
					{
						first[entry.Key] = entry.Value.Plus(second[entry.Key]);
					}
				}
				foreach (KeyValuePair<string, SimpleMatrix> entry_1 in second)
				{
					if (!first.Contains(entry_1.Key))
					{
						first[entry_1.Key] = entry_1.Value;
					}
				}
			}

			/// <summary>Init a TwoDimensionalMap with 0 matrices for all the matrices in the original map.</summary>
			private static TwoDimensionalMap<string, string, SimpleMatrix> InitDerivatives(TwoDimensionalMap<string, string, SimpleMatrix> map)
			{
				TwoDimensionalMap<string, string, SimpleMatrix> derivatives = TwoDimensionalMap.TreeMap();
				foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry in map)
				{
					int numRows = entry.GetValue().NumRows();
					int numCols = entry.GetValue().NumCols();
					derivatives.Put(entry.GetFirstKey(), entry.GetSecondKey(), new SimpleMatrix(numRows, numCols));
				}
				return derivatives;
			}

			/// <summary>Init a TwoDimensionalMap with 0 tensors for all the tensors in the original map.</summary>
			private static TwoDimensionalMap<string, string, SimpleTensor> InitTensorDerivatives(TwoDimensionalMap<string, string, SimpleTensor> map)
			{
				TwoDimensionalMap<string, string, SimpleTensor> derivatives = TwoDimensionalMap.TreeMap();
				foreach (TwoDimensionalMap.Entry<string, string, SimpleTensor> entry in map)
				{
					int numRows = entry.GetValue().NumRows();
					int numCols = entry.GetValue().NumCols();
					int numSlices = entry.GetValue().NumSlices();
					derivatives.Put(entry.GetFirstKey(), entry.GetSecondKey(), new SimpleTensor(numRows, numCols, numSlices));
				}
				return derivatives;
			}

			/// <summary>Init a Map with 0 matrices for all the matrices in the original map.</summary>
			private static IDictionary<string, SimpleMatrix> InitDerivatives(IDictionary<string, SimpleMatrix> map)
			{
				IDictionary<string, SimpleMatrix> derivatives = Generics.NewTreeMap();
				foreach (KeyValuePair<string, SimpleMatrix> entry in map)
				{
					int numRows = entry.Value.NumRows();
					int numCols = entry.Value.NumCols();
					derivatives[entry.Key] = new SimpleMatrix(numRows, numCols);
				}
				return derivatives;
			}
		}

		private SentimentCostAndGradient.ModelDerivatives ScoreDerivatives(IList<Tree> trainingBatch)
		{
			// "final" makes this as fast as having separate maps declared in this function
			SentimentCostAndGradient.ModelDerivatives derivatives = new SentimentCostAndGradient.ModelDerivatives(model);
			IList<Tree> forwardPropTrees = Generics.NewArrayList();
			foreach (Tree tree in trainingBatch)
			{
				Tree trainingTree = tree.DeepCopy();
				// this will attach the error vectors and the node vectors
				// to each node in the tree
				ForwardPropagateTree(trainingTree);
				forwardPropTrees.Add(trainingTree);
			}
			foreach (Tree tree_1 in forwardPropTrees)
			{
				BackpropDerivativesAndError(tree_1, derivatives.binaryTD, derivatives.binaryCD, derivatives.binaryTensorTD, derivatives.unaryCD, derivatives.wordVectorD);
				derivatives.error += SumError(tree_1);
			}
			return derivatives;
		}

		internal class ScoringProcessor : IThreadsafeProcessor<IList<Tree>, SentimentCostAndGradient.ModelDerivatives>
		{
			public virtual SentimentCostAndGradient.ModelDerivatives Process(IList<Tree> trainingBatch)
			{
				return this._enclosing.ScoreDerivatives(trainingBatch);
			}

			public virtual IThreadsafeProcessor<IList<Tree>, SentimentCostAndGradient.ModelDerivatives> NewInstance()
			{
				// should be threadsafe
				return this;
			}

			internal ScoringProcessor(SentimentCostAndGradient _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly SentimentCostAndGradient _enclosing;
		}

		protected internal override void Calculate(double[] theta)
		{
			model.VectorToParams(theta);
			SentimentCostAndGradient.ModelDerivatives derivatives;
			if (model.op.trainOptions.nThreads == 1)
			{
				derivatives = ScoreDerivatives(trainingBatch);
			}
			else
			{
				// TODO: because some addition operations happen in different
				// orders now, this results in slightly different values, which
				// over time add up to significantly different models even when
				// given the same random seed.  Probably not a big deal.
				// To be more specific, for trees T1, T2, T3, ... Tn,
				// when using one thread, we sum the derivatives T1 + T2 ...
				// When using multiple threads, we first sum T1 + ... + Tk,
				// then sum Tk+1 + ... + T2k, etc, for split size k.
				// The splits are then summed in order.
				// This different sum order results in slightly different numbers.
				MulticoreWrapper<IList<Tree>, SentimentCostAndGradient.ModelDerivatives> wrapper = new MulticoreWrapper<IList<Tree>, SentimentCostAndGradient.ModelDerivatives>(model.op.trainOptions.nThreads, new SentimentCostAndGradient.ScoringProcessor(this
					));
				// use wrapper.nThreads in case the number of threads was automatically changed
				foreach (IList<Tree> chunk in CollectionUtils.PartitionIntoFolds(trainingBatch, wrapper.NThreads()))
				{
					wrapper.Put(chunk);
				}
				wrapper.Join();
				derivatives = new SentimentCostAndGradient.ModelDerivatives(model);
				while (wrapper.Peek())
				{
					SentimentCostAndGradient.ModelDerivatives batchDerivatives = wrapper.Poll();
					derivatives.Add(batchDerivatives);
				}
			}
			// scale the error by the number of sentences so that the
			// regularization isn't drowned out for large training batchs
			double scale = (1.0 / trainingBatch.Count);
			value = derivatives.error * scale;
			value += ScaleAndRegularize(derivatives.binaryTD, model.binaryTransform, scale, model.op.trainOptions.regTransformMatrix, false);
			value += ScaleAndRegularize(derivatives.binaryCD, model.binaryClassification, scale, model.op.trainOptions.regClassification, true);
			value += ScaleAndRegularizeTensor(derivatives.binaryTensorTD, model.binaryTensors, scale, model.op.trainOptions.regTransformTensor);
			value += ScaleAndRegularize(derivatives.unaryCD, model.unaryClassification, scale, model.op.trainOptions.regClassification, false, true);
			value += ScaleAndRegularize(derivatives.wordVectorD, model.wordVectors, scale, model.op.trainOptions.regWordVector, true, false);
			derivative = NeuralUtils.ParamsToVector(theta.Length, derivatives.binaryTD.ValueIterator(), derivatives.binaryCD.ValueIterator(), SimpleTensor.IteratorSimpleMatrix(derivatives.binaryTensorTD.ValueIterator()), derivatives.unaryCD.Values.GetEnumerator
				(), derivatives.wordVectorD.Values.GetEnumerator());
		}

		private static double ScaleAndRegularize(TwoDimensionalMap<string, string, SimpleMatrix> derivatives, TwoDimensionalMap<string, string, SimpleMatrix> currentMatrices, double scale, double regCost, bool dropBiasColumn)
		{
			double cost = 0.0;
			// the regularization cost
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry in currentMatrices)
			{
				SimpleMatrix D = derivatives.Get(entry.GetFirstKey(), entry.GetSecondKey());
				SimpleMatrix regMatrix = entry.GetValue();
				if (dropBiasColumn)
				{
					regMatrix = new SimpleMatrix(regMatrix);
					regMatrix.InsertIntoThis(0, regMatrix.NumCols() - 1, new SimpleMatrix(regMatrix.NumRows(), 1));
				}
				D = D.Scale(scale).Plus(regMatrix.Scale(regCost));
				derivatives.Put(entry.GetFirstKey(), entry.GetSecondKey(), D);
				cost += regMatrix.ElementMult(regMatrix).ElementSum() * regCost / 2.0;
			}
			return cost;
		}

		private static double ScaleAndRegularize(IDictionary<string, SimpleMatrix> derivatives, IDictionary<string, SimpleMatrix> currentMatrices, double scale, double regCost, bool activeMatricesOnly, bool dropBiasColumn)
		{
			double cost = 0.0;
			// the regularization cost
			foreach (KeyValuePair<string, SimpleMatrix> entry in currentMatrices)
			{
				SimpleMatrix D = derivatives[entry.Key];
				if (activeMatricesOnly && D == null)
				{
					// Fill in an emptpy matrix so the length of theta can match.
					// TODO: might want to allow for sparse parameter vectors
					derivatives[entry.Key] = new SimpleMatrix(entry.Value.NumRows(), entry.Value.NumCols());
					continue;
				}
				SimpleMatrix regMatrix = entry.Value;
				if (dropBiasColumn)
				{
					regMatrix = new SimpleMatrix(regMatrix);
					regMatrix.InsertIntoThis(0, regMatrix.NumCols() - 1, new SimpleMatrix(regMatrix.NumRows(), 1));
				}
				D = D.Scale(scale).Plus(regMatrix.Scale(regCost));
				derivatives[entry.Key] = D;
				cost += regMatrix.ElementMult(regMatrix).ElementSum() * regCost / 2.0;
			}
			return cost;
		}

		private static double ScaleAndRegularizeTensor(TwoDimensionalMap<string, string, SimpleTensor> derivatives, TwoDimensionalMap<string, string, SimpleTensor> currentMatrices, double scale, double regCost)
		{
			double cost = 0.0;
			// the regularization cost
			foreach (TwoDimensionalMap.Entry<string, string, SimpleTensor> entry in currentMatrices)
			{
				SimpleTensor D = derivatives.Get(entry.GetFirstKey(), entry.GetSecondKey());
				D = D.Scale(scale).Plus(entry.GetValue().Scale(regCost));
				derivatives.Put(entry.GetFirstKey(), entry.GetSecondKey(), D);
				cost += entry.GetValue().ElementMult(entry.GetValue()).ElementSum() * regCost / 2.0;
			}
			return cost;
		}

		private void BackpropDerivativesAndError(Tree tree, TwoDimensionalMap<string, string, SimpleMatrix> binaryTD, TwoDimensionalMap<string, string, SimpleMatrix> binaryCD, TwoDimensionalMap<string, string, SimpleTensor> binaryTensorTD, IDictionary
			<string, SimpleMatrix> unaryCD, IDictionary<string, SimpleMatrix> wordVectorD)
		{
			SimpleMatrix delta = new SimpleMatrix(model.op.numHid, 1);
			BackpropDerivativesAndError(tree, binaryTD, binaryCD, binaryTensorTD, unaryCD, wordVectorD, delta);
		}

		private void BackpropDerivativesAndError(Tree tree, TwoDimensionalMap<string, string, SimpleMatrix> binaryTD, TwoDimensionalMap<string, string, SimpleMatrix> binaryCD, TwoDimensionalMap<string, string, SimpleTensor> binaryTensorTD, IDictionary
			<string, SimpleMatrix> unaryCD, IDictionary<string, SimpleMatrix> wordVectorD, SimpleMatrix deltaUp)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			SimpleMatrix currentVector = RNNCoreAnnotations.GetNodeVector(tree);
			string category = tree.Label().Value();
			category = model.BasicCategory(category);
			// Build a vector that looks like 0,0,1,0,0 with an indicator for the correct class
			SimpleMatrix goldLabel = new SimpleMatrix(model.numClasses, 1);
			int goldClass = RNNCoreAnnotations.GetGoldClass(tree);
			if (goldClass >= 0)
			{
				goldLabel.Set(goldClass, 1.0);
			}
			double nodeWeight = model.op.trainOptions.GetClassWeight(goldClass);
			SimpleMatrix predictions = RNNCoreAnnotations.GetPredictions(tree);
			// If this is an unlabeled class, set deltaClass to 0.  We could
			// make this more efficient by eliminating various of the below
			// calculations, but this would be the easiest way to handle the
			// unlabeled class
			SimpleMatrix deltaClass = goldClass >= 0 ? predictions.Minus(goldLabel).Scale(nodeWeight) : new SimpleMatrix(predictions.NumRows(), predictions.NumCols());
			SimpleMatrix localCD = deltaClass.Mult(NeuralUtils.ConcatenateWithBias(currentVector).Transpose());
			double error = -(NeuralUtils.ElementwiseApplyLog(predictions).ElementMult(goldLabel).ElementSum());
			error = error * nodeWeight;
			RNNCoreAnnotations.SetPredictionError(tree, error);
			if (tree.IsPreTerminal())
			{
				// below us is a word vector
				unaryCD[category] = unaryCD[category].Plus(localCD);
				string word = tree.Children()[0].Label().Value();
				word = model.GetVocabWord(word);
				//SimpleMatrix currentVectorDerivative = NeuralUtils.elementwiseApplyTanhDerivative(currentVector);
				//SimpleMatrix deltaFromClass = model.getUnaryClassification(category).transpose().mult(deltaClass);
				//SimpleMatrix deltaFull = deltaFromClass.extractMatrix(0, model.op.numHid, 0, 1).plus(deltaUp);
				//SimpleMatrix wordDerivative = deltaFull.elementMult(currentVectorDerivative);
				//wordVectorD.put(word, wordVectorD.get(word).plus(wordDerivative));
				SimpleMatrix currentVectorDerivative = NeuralUtils.ElementwiseApplyTanhDerivative(currentVector);
				SimpleMatrix deltaFromClass = model.GetUnaryClassification(category).Transpose().Mult(deltaClass);
				deltaFromClass = deltaFromClass.ExtractMatrix(0, model.op.numHid, 0, 1).ElementMult(currentVectorDerivative);
				SimpleMatrix deltaFull = deltaFromClass.Plus(deltaUp);
				SimpleMatrix oldWordVectorD = wordVectorD[word];
				if (oldWordVectorD == null)
				{
					wordVectorD[word] = deltaFull;
				}
				else
				{
					wordVectorD[word] = oldWordVectorD.Plus(deltaFull);
				}
			}
			else
			{
				// Otherwise, this must be a binary node
				string leftCategory = model.BasicCategory(tree.Children()[0].Label().Value());
				string rightCategory = model.BasicCategory(tree.Children()[1].Label().Value());
				if (model.op.combineClassification)
				{
					unaryCD[string.Empty] = unaryCD[string.Empty].Plus(localCD);
				}
				else
				{
					binaryCD.Put(leftCategory, rightCategory, binaryCD.Get(leftCategory, rightCategory).Plus(localCD));
				}
				SimpleMatrix currentVectorDerivative = NeuralUtils.ElementwiseApplyTanhDerivative(currentVector);
				SimpleMatrix deltaFromClass = model.GetBinaryClassification(leftCategory, rightCategory).Transpose().Mult(deltaClass);
				deltaFromClass = deltaFromClass.ExtractMatrix(0, model.op.numHid, 0, 1).ElementMult(currentVectorDerivative);
				SimpleMatrix deltaFull = deltaFromClass.Plus(deltaUp);
				SimpleMatrix leftVector = RNNCoreAnnotations.GetNodeVector(tree.Children()[0]);
				SimpleMatrix rightVector = RNNCoreAnnotations.GetNodeVector(tree.Children()[1]);
				SimpleMatrix childrenVector = NeuralUtils.ConcatenateWithBias(leftVector, rightVector);
				SimpleMatrix W_df = deltaFull.Mult(childrenVector.Transpose());
				binaryTD.Put(leftCategory, rightCategory, binaryTD.Get(leftCategory, rightCategory).Plus(W_df));
				SimpleMatrix deltaDown;
				if (model.op.useTensors)
				{
					SimpleTensor Wt_df = GetTensorGradient(deltaFull, leftVector, rightVector);
					binaryTensorTD.Put(leftCategory, rightCategory, binaryTensorTD.Get(leftCategory, rightCategory).Plus(Wt_df));
					deltaDown = ComputeTensorDeltaDown(deltaFull, leftVector, rightVector, model.GetBinaryTransform(leftCategory, rightCategory), model.GetBinaryTensor(leftCategory, rightCategory));
				}
				else
				{
					deltaDown = model.GetBinaryTransform(leftCategory, rightCategory).Transpose().Mult(deltaFull);
				}
				SimpleMatrix leftDerivative = NeuralUtils.ElementwiseApplyTanhDerivative(leftVector);
				SimpleMatrix rightDerivative = NeuralUtils.ElementwiseApplyTanhDerivative(rightVector);
				SimpleMatrix leftDeltaDown = deltaDown.ExtractMatrix(0, deltaFull.NumRows(), 0, 1);
				SimpleMatrix rightDeltaDown = deltaDown.ExtractMatrix(deltaFull.NumRows(), deltaFull.NumRows() * 2, 0, 1);
				BackpropDerivativesAndError(tree.Children()[0], binaryTD, binaryCD, binaryTensorTD, unaryCD, wordVectorD, leftDerivative.ElementMult(leftDeltaDown));
				BackpropDerivativesAndError(tree.Children()[1], binaryTD, binaryCD, binaryTensorTD, unaryCD, wordVectorD, rightDerivative.ElementMult(rightDeltaDown));
			}
		}

		private static SimpleMatrix ComputeTensorDeltaDown(SimpleMatrix deltaFull, SimpleMatrix leftVector, SimpleMatrix rightVector, SimpleMatrix W, SimpleTensor Wt)
		{
			SimpleMatrix WTDelta = W.Transpose().Mult(deltaFull);
			SimpleMatrix WTDeltaNoBias = WTDelta.ExtractMatrix(0, deltaFull.NumRows() * 2, 0, 1);
			int size = deltaFull.GetNumElements();
			SimpleMatrix deltaTensor = new SimpleMatrix(size * 2, 1);
			SimpleMatrix fullVector = NeuralUtils.Concatenate(leftVector, rightVector);
			for (int slice = 0; slice < size; ++slice)
			{
				SimpleMatrix scaledFullVector = fullVector.Scale(deltaFull.Get(slice));
				deltaTensor = deltaTensor.Plus(Wt.GetSlice(slice).Plus(Wt.GetSlice(slice).Transpose()).Mult(scaledFullVector));
			}
			return deltaTensor.Plus(WTDeltaNoBias);
		}

		private static SimpleTensor GetTensorGradient(SimpleMatrix deltaFull, SimpleMatrix leftVector, SimpleMatrix rightVector)
		{
			int size = deltaFull.GetNumElements();
			SimpleTensor Wt_df = new SimpleTensor(size * 2, size * 2, size);
			// TODO: combine this concatenation with computeTensorDeltaDown?
			SimpleMatrix fullVector = NeuralUtils.Concatenate(leftVector, rightVector);
			for (int slice = 0; slice < size; ++slice)
			{
				Wt_df.SetSlice(slice, fullVector.Scale(deltaFull.Get(slice)).Mult(fullVector.Transpose()));
			}
			return Wt_df;
		}

		/// <summary>
		/// This is the method to call for assigning labels and node vectors
		/// to the Tree.
		/// </summary>
		/// <remarks>
		/// This is the method to call for assigning labels and node vectors
		/// to the Tree.  After calling this, each of the non-leaf nodes will
		/// have the node vector and the predictions of their classes
		/// assigned to that subtree's node.  The annotations filled in are
		/// the RNNCoreAnnotations.NodeVector, Predictions, and
		/// PredictedClass.  In general, PredictedClass will be the most
		/// useful annotation except when training.
		/// </remarks>
		public virtual void ForwardPropagateTree(Tree tree)
		{
			SimpleMatrix nodeVector;
			// initialized below or Exception thrown // = null;
			SimpleMatrix classification;
			// initialized below or Exception thrown // = null;
			if (tree.IsLeaf())
			{
				// We do nothing for the leaves.  The preterminals will
				// calculate the classification for this word/tag.  In fact, the
				// recursion should not have gotten here (unless there are
				// degenerate trees of just one leaf)
				log.Info("SentimentCostAndGradient: warning: We reached leaves in forwardPropagate: " + tree);
				throw new AssertionError("We should not have reached leaves in forwardPropagate");
			}
			else
			{
				if (tree.IsPreTerminal())
				{
					classification = model.GetUnaryClassification(tree.Label().Value());
					string word = tree.Children()[0].Label().Value();
					SimpleMatrix wordVector = model.GetWordVector(word);
					nodeVector = NeuralUtils.ElementwiseApplyTanh(wordVector);
				}
				else
				{
					if (tree.Children().Length == 1)
					{
						log.Info("SentimentCostAndGradient: warning: Non-preterminal nodes of size 1: " + tree);
						throw new AssertionError("Non-preterminal nodes of size 1 should have already been collapsed");
					}
					else
					{
						if (tree.Children().Length == 2)
						{
							ForwardPropagateTree(tree.Children()[0]);
							ForwardPropagateTree(tree.Children()[1]);
							string leftCategory = tree.Children()[0].Label().Value();
							string rightCategory = tree.Children()[1].Label().Value();
							SimpleMatrix W = model.GetBinaryTransform(leftCategory, rightCategory);
							classification = model.GetBinaryClassification(leftCategory, rightCategory);
							SimpleMatrix leftVector = RNNCoreAnnotations.GetNodeVector(tree.Children()[0]);
							SimpleMatrix rightVector = RNNCoreAnnotations.GetNodeVector(tree.Children()[1]);
							SimpleMatrix childrenVector = NeuralUtils.ConcatenateWithBias(leftVector, rightVector);
							if (model.op.useTensors)
							{
								SimpleTensor tensor = model.GetBinaryTensor(leftCategory, rightCategory);
								SimpleMatrix tensorIn = NeuralUtils.Concatenate(leftVector, rightVector);
								SimpleMatrix tensorOut = tensor.BilinearProducts(tensorIn);
								nodeVector = NeuralUtils.ElementwiseApplyTanh(W.Mult(childrenVector).Plus(tensorOut));
							}
							else
							{
								nodeVector = NeuralUtils.ElementwiseApplyTanh(W.Mult(childrenVector));
							}
						}
						else
						{
							log.Info("SentimentCostAndGradient: warning: Tree not correctly binarized: " + tree);
							throw new AssertionError("Tree not correctly binarized");
						}
					}
				}
			}
			SimpleMatrix predictions = NeuralUtils.Softmax(classification.Mult(NeuralUtils.ConcatenateWithBias(nodeVector)));
			int index = GetPredictedClass(predictions);
			if (!(tree.Label() is CoreLabel))
			{
				log.Info("SentimentCostAndGradient: warning: No CoreLabels in nodes: " + tree);
				throw new AssertionError("Expected CoreLabels in the nodes");
			}
			CoreLabel label = (CoreLabel)tree.Label();
			label.Set(typeof(RNNCoreAnnotations.Predictions), predictions);
			label.Set(typeof(RNNCoreAnnotations.PredictedClass), index);
			label.Set(typeof(RNNCoreAnnotations.NodeVector), nodeVector);
		}
		// end forwardPropagateTree
	}
}
