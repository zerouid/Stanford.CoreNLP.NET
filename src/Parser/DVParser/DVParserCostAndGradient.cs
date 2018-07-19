using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Neural;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Org.Ejml.Simple;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	public class DVParserCostAndGradient : AbstractCachingDiffFunction
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Dvparser.DVParserCostAndGradient));

		internal IList<Tree> trainingBatch;

		internal IdentityHashMap<Tree, IList<Tree>> topParses;

		internal DVModel dvModel;

		internal Options op;

		public DVParserCostAndGradient(IList<Tree> trainingBatch, IdentityHashMap<Tree, IList<Tree>> topParses, DVModel dvModel, Options op)
		{
			this.trainingBatch = trainingBatch;
			this.topParses = topParses;
			this.dvModel = dvModel;
			this.op = op;
		}

		/// <summary>
		/// Return a null list if we don't care about context words, return a
		/// list of the words at the leaves of the tree if we do care
		/// </summary>
		private IList<string> GetContextWords(Tree tree)
		{
			IList<string> words = null;
			if (op.trainOptions.useContextWords)
			{
				words = Generics.NewArrayList();
				IList<ILabel> leaves = tree.Yield();
				foreach (ILabel word in leaves)
				{
					words.Add(word.Value());
				}
			}
			return words;
		}

		private SimpleMatrix ConcatenateContextWords(SimpleMatrix childVec, IntPair span, IList<string> words)
		{
			// TODO: factor out getting the words
			SimpleMatrix left = (span.GetSource() < 0) ? dvModel.GetStartWordVector() : dvModel.GetWordVector(words[span.GetSource()]);
			SimpleMatrix right = (span.GetTarget() >= words.Count) ? dvModel.GetEndWordVector() : dvModel.GetWordVector(words[span.GetTarget()]);
			return NeuralUtils.Concatenate(childVec, left, right);
		}

		public static void OutputSpans(Tree tree)
		{
			log.Info(tree.GetSpan() + " ");
			foreach (Tree child in tree.Children())
			{
				OutputSpans(child);
			}
		}

		// TODO: make this part of DVModel or DVParser?
		public virtual double Score(Tree tree, IdentityHashMap<Tree, SimpleMatrix> nodeVectors)
		{
			IList<string> words = GetContextWords(tree);
			// score of the entire tree is the sum of the scores of all of
			// its nodes
			// TODO: make the node vectors part of the tree itself?
			IdentityHashMap<Tree, double> scores = new IdentityHashMap<Tree, double>();
			try
			{
				ForwardPropagateTree(tree, words, nodeVectors, scores);
			}
			catch (AssertionError e)
			{
				log.Info("Failed to correctly process tree " + tree);
				throw;
			}
			double score = 0.0;
			foreach (Tree node in scores.Keys)
			{
				score += scores[node];
			}
			//log.info(Double.toString(score));
			return score;
		}

		private void ForwardPropagateTree(Tree tree, IList<string> words, IdentityHashMap<Tree, SimpleMatrix> nodeVectors, IdentityHashMap<Tree, double> scores)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			if (tree.IsPreTerminal())
			{
				Tree wordNode = tree.Children()[0];
				string word = wordNode.Label().Value();
				SimpleMatrix wordVector = dvModel.GetWordVector(word);
				wordVector = NeuralUtils.ElementwiseApplyTanh(wordVector);
				nodeVectors[tree] = wordVector;
				return;
			}
			foreach (Tree child in tree.Children())
			{
				ForwardPropagateTree(child, words, nodeVectors, scores);
			}
			// at this point, nodeVectors contains the vectors for all of
			// the children of tree
			SimpleMatrix childVec;
			if (tree.Children().Length == 2)
			{
				childVec = NeuralUtils.ConcatenateWithBias(nodeVectors[tree.Children()[0]], nodeVectors[tree.Children()[1]]);
			}
			else
			{
				childVec = NeuralUtils.ConcatenateWithBias(nodeVectors[tree.Children()[0]]);
			}
			if (op.trainOptions.useContextWords)
			{
				childVec = ConcatenateContextWords(childVec, tree.GetSpan(), words);
			}
			SimpleMatrix W = dvModel.GetWForNode(tree);
			if (W == null)
			{
				string error = "Could not find W for tree " + tree;
				if (op.testOptions.verbose)
				{
					log.Info(error);
				}
				throw new NoSuchParseException(error);
			}
			SimpleMatrix currentVector = W.Mult(childVec);
			currentVector = NeuralUtils.ElementwiseApplyTanh(currentVector);
			nodeVectors[tree] = currentVector;
			SimpleMatrix scoreW = dvModel.GetScoreWForNode(tree);
			if (scoreW == null)
			{
				string error = "Could not find scoreW for tree " + tree;
				if (op.testOptions.verbose)
				{
					log.Info(error);
				}
				throw new NoSuchParseException(error);
			}
			double score = scoreW.Dot(currentVector);
			//score = NeuralUtils.sigmoid(score);
			scores[tree] = score;
		}

		//log.info(Double.toString(score)+" ");
		public override int DomainDimension()
		{
			// TODO: cache this for speed?
			return dvModel.TotalParamSize();
		}

		internal const double TrainLambda = 1.0;

		public virtual IList<DeepTree> GetAllHighestScoringTreesTest(IList<Tree> trees)
		{
			IList<DeepTree> allBestTrees = new List<DeepTree>();
			foreach (Tree tree in trees)
			{
				allBestTrees.Add(GetHighestScoringTree(tree, 0));
			}
			return allBestTrees;
		}

		public virtual DeepTree GetHighestScoringTree(Tree tree, double lambda)
		{
			IList<Tree> hypotheses = topParses[tree];
			if (hypotheses == null || hypotheses.Count == 0)
			{
				throw new AssertionError("Failed to get any hypothesis trees for " + tree);
			}
			double bestScore = double.NegativeInfinity;
			Tree bestTree = null;
			IdentityHashMap<Tree, SimpleMatrix> bestVectors = null;
			foreach (Tree hypothesis in hypotheses)
			{
				IdentityHashMap<Tree, SimpleMatrix> nodeVectors = new IdentityHashMap<Tree, SimpleMatrix>();
				double scoreHyp = Score(hypothesis, nodeVectors);
				double deltaMargin = 0;
				if (lambda != 0)
				{
					//TODO: RS: Play around with this parameter to prevent blowing up of scores
					deltaMargin = op.trainOptions.deltaMargin * lambda * GetMargin(tree, hypothesis);
				}
				scoreHyp = scoreHyp + deltaMargin;
				if (bestTree == null || scoreHyp > bestScore)
				{
					bestTree = hypothesis;
					bestScore = scoreHyp;
					bestVectors = nodeVectors;
				}
			}
			DeepTree returnTree = new DeepTree(bestTree, bestVectors, bestScore);
			return returnTree;
		}

		internal class ScoringProcessor : IThreadsafeProcessor<Tree, Pair<DeepTree, DeepTree>>
		{
			public virtual Pair<DeepTree, DeepTree> Process(Tree tree)
			{
				// For each tree, move in the direction of the gold tree, and
				// move away from the direction of the best scoring hypothesis
				IdentityHashMap<Tree, SimpleMatrix> goldVectors = new IdentityHashMap<Tree, SimpleMatrix>();
				double scoreGold = this._enclosing.Score(tree, goldVectors);
				DeepTree bestTree = this._enclosing.GetHighestScoringTree(tree, DVParserCostAndGradient.TrainLambda);
				DeepTree goldTree = new DeepTree(tree, goldVectors, scoreGold);
				return Pair.MakePair(goldTree, bestTree);
			}

			public virtual IThreadsafeProcessor<Tree, Pair<DeepTree, DeepTree>> NewInstance()
			{
				// should be threadsafe
				return this;
			}

			internal ScoringProcessor(DVParserCostAndGradient _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly DVParserCostAndGradient _enclosing;
		}

		// fill value & derivative
		protected internal override void Calculate(double[] theta)
		{
			dvModel.VectorToParams(theta);
			double localValue = 0.0;
			double[] localDerivative = new double[theta.Length];
			TwoDimensionalMap<string, string, SimpleMatrix> binaryW_dfsG;
			TwoDimensionalMap<string, string, SimpleMatrix> binaryW_dfsB;
			binaryW_dfsG = TwoDimensionalMap.TreeMap();
			binaryW_dfsB = TwoDimensionalMap.TreeMap();
			TwoDimensionalMap<string, string, SimpleMatrix> binaryScoreDerivativesG;
			TwoDimensionalMap<string, string, SimpleMatrix> binaryScoreDerivativesB;
			binaryScoreDerivativesG = TwoDimensionalMap.TreeMap();
			binaryScoreDerivativesB = TwoDimensionalMap.TreeMap();
			IDictionary<string, SimpleMatrix> unaryW_dfsG;
			IDictionary<string, SimpleMatrix> unaryW_dfsB;
			unaryW_dfsG = new SortedDictionary<string, SimpleMatrix>();
			unaryW_dfsB = new SortedDictionary<string, SimpleMatrix>();
			IDictionary<string, SimpleMatrix> unaryScoreDerivativesG;
			IDictionary<string, SimpleMatrix> unaryScoreDerivativesB;
			unaryScoreDerivativesG = new SortedDictionary<string, SimpleMatrix>();
			unaryScoreDerivativesB = new SortedDictionary<string, SimpleMatrix>();
			IDictionary<string, SimpleMatrix> wordVectorDerivativesG = new SortedDictionary<string, SimpleMatrix>();
			IDictionary<string, SimpleMatrix> wordVectorDerivativesB = new SortedDictionary<string, SimpleMatrix>();
			foreach (TwoDimensionalMap.Entry<string, string, SimpleMatrix> entry in dvModel.binaryTransform)
			{
				int numRows = entry.GetValue().NumRows();
				int numCols = entry.GetValue().NumCols();
				binaryW_dfsG.Put(entry.GetFirstKey(), entry.GetSecondKey(), new SimpleMatrix(numRows, numCols));
				binaryW_dfsB.Put(entry.GetFirstKey(), entry.GetSecondKey(), new SimpleMatrix(numRows, numCols));
				binaryScoreDerivativesG.Put(entry.GetFirstKey(), entry.GetSecondKey(), new SimpleMatrix(1, numRows));
				binaryScoreDerivativesB.Put(entry.GetFirstKey(), entry.GetSecondKey(), new SimpleMatrix(1, numRows));
			}
			foreach (KeyValuePair<string, SimpleMatrix> entry_1 in dvModel.unaryTransform)
			{
				int numRows = entry_1.Value.NumRows();
				int numCols = entry_1.Value.NumCols();
				unaryW_dfsG[entry_1.Key] = new SimpleMatrix(numRows, numCols);
				unaryW_dfsB[entry_1.Key] = new SimpleMatrix(numRows, numCols);
				unaryScoreDerivativesG[entry_1.Key] = new SimpleMatrix(1, numRows);
				unaryScoreDerivativesB[entry_1.Key] = new SimpleMatrix(1, numRows);
			}
			if (op.trainOptions.trainWordVectors)
			{
				foreach (KeyValuePair<string, SimpleMatrix> entry_2 in dvModel.wordVectors)
				{
					int numRows = entry_2.Value.NumRows();
					int numCols = entry_2.Value.NumCols();
					wordVectorDerivativesG[entry_2.Key] = new SimpleMatrix(numRows, numCols);
					wordVectorDerivativesB[entry_2.Key] = new SimpleMatrix(numRows, numCols);
				}
			}
			// Some optimization methods prints out a line without an end, so our
			// debugging statements are misaligned
			Timing scoreTiming = new Timing();
			scoreTiming.Doing("Scoring trees");
			int treeNum = 0;
			MulticoreWrapper<Tree, Pair<DeepTree, DeepTree>> wrapper = new MulticoreWrapper<Tree, Pair<DeepTree, DeepTree>>(op.trainOptions.trainingThreads, new DVParserCostAndGradient.ScoringProcessor(this));
			foreach (Tree tree in trainingBatch)
			{
				wrapper.Put(tree);
			}
			wrapper.Join();
			scoreTiming.Done();
			while (wrapper.Peek())
			{
				Pair<DeepTree, DeepTree> result = wrapper.Poll();
				DeepTree goldTree = result.first;
				DeepTree bestTree = result.second;
				StringBuilder treeDebugLine = new StringBuilder();
				Formatter formatter = new Formatter(treeDebugLine);
				bool isDone = (Math.Abs(bestTree.GetScore() - goldTree.GetScore()) <= 0.00001 || goldTree.GetScore() > bestTree.GetScore());
				string done = isDone ? "done" : string.Empty;
				formatter.Format("Tree %6d Highest tree: %12.4f Correct tree: %12.4f %s", treeNum, bestTree.GetScore(), goldTree.GetScore(), done);
				log.Info(treeDebugLine.ToString());
				if (!isDone)
				{
					// if the gold tree is better than the best hypothesis tree by
					// a large enough margin, then the score difference will be 0
					// and we ignore the tree
					double valueDelta = bestTree.GetScore() - goldTree.GetScore();
					//double valueDelta = Math.max(0.0, - scoreGold + bestScore);
					localValue += valueDelta;
					// get the context words for this tree - should be the same
					// for either goldTree or bestTree
					IList<string> words = GetContextWords(goldTree.GetTree());
					// The derivatives affected by this tree are only based on the
					// nodes present in this tree, eg not all matrix derivatives
					// will be affected by this tree
					BackpropDerivative(goldTree.GetTree(), words, goldTree.GetVectors(), binaryW_dfsG, unaryW_dfsG, binaryScoreDerivativesG, unaryScoreDerivativesG, wordVectorDerivativesG);
					BackpropDerivative(bestTree.GetTree(), words, bestTree.GetVectors(), binaryW_dfsB, unaryW_dfsB, binaryScoreDerivativesB, unaryScoreDerivativesB, wordVectorDerivativesB);
				}
				++treeNum;
			}
			double[] localDerivativeGood;
			double[] localDerivativeB;
			if (op.trainOptions.trainWordVectors)
			{
				localDerivativeGood = NeuralUtils.ParamsToVector(theta.Length, binaryW_dfsG.ValueIterator(), unaryW_dfsG.Values.GetEnumerator(), binaryScoreDerivativesG.ValueIterator(), unaryScoreDerivativesG.Values.GetEnumerator(), wordVectorDerivativesG.Values
					.GetEnumerator());
				localDerivativeB = NeuralUtils.ParamsToVector(theta.Length, binaryW_dfsB.ValueIterator(), unaryW_dfsB.Values.GetEnumerator(), binaryScoreDerivativesB.ValueIterator(), unaryScoreDerivativesB.Values.GetEnumerator(), wordVectorDerivativesB.Values
					.GetEnumerator());
			}
			else
			{
				localDerivativeGood = NeuralUtils.ParamsToVector(theta.Length, binaryW_dfsG.ValueIterator(), unaryW_dfsG.Values.GetEnumerator(), binaryScoreDerivativesG.ValueIterator(), unaryScoreDerivativesG.Values.GetEnumerator());
				localDerivativeB = NeuralUtils.ParamsToVector(theta.Length, binaryW_dfsB.ValueIterator(), unaryW_dfsB.Values.GetEnumerator(), binaryScoreDerivativesB.ValueIterator(), unaryScoreDerivativesB.Values.GetEnumerator());
			}
			// correct - highest
			for (int i = 0; i < localDerivativeGood.Length; i++)
			{
				localDerivative[i] = localDerivativeB[i] - localDerivativeGood[i];
			}
			// TODO: this is where we would combine multiple costs if we had parallelized the calculation
			value = localValue;
			derivative = localDerivative;
			// normalizing by training batch size
			value = (1.0 / trainingBatch.Count) * value;
			ArrayMath.MultiplyInPlace(derivative, (1.0 / trainingBatch.Count));
			// add regularization to cost:
			double[] currentParams = dvModel.ParamsToVector();
			double regCost = 0;
			foreach (double currentParam in currentParams)
			{
				regCost += currentParam * currentParam;
			}
			regCost = op.trainOptions.regCost * 0.5 * regCost;
			value += regCost;
			// add regularization to gradient
			ArrayMath.MultiplyInPlace(currentParams, op.trainOptions.regCost);
			ArrayMath.PairwiseAddInPlace(derivative, currentParams);
		}

		public virtual double GetMargin(Tree goldTree, Tree bestHypothesis)
		{
			return TreeSpanScoring.CountSpanErrors(op.Langpack(), goldTree, bestHypothesis);
		}

		public virtual void BackpropDerivative(Tree tree, IList<string> words, IdentityHashMap<Tree, SimpleMatrix> nodeVectors, TwoDimensionalMap<string, string, SimpleMatrix> binaryW_dfs, IDictionary<string, SimpleMatrix> unaryW_dfs, TwoDimensionalMap
			<string, string, SimpleMatrix> binaryScoreDerivatives, IDictionary<string, SimpleMatrix> unaryScoreDerivatives, IDictionary<string, SimpleMatrix> wordVectorDerivatives)
		{
			SimpleMatrix delta = new SimpleMatrix(op.lexOptions.numHid, 1);
			BackpropDerivative(tree, words, nodeVectors, binaryW_dfs, unaryW_dfs, binaryScoreDerivatives, unaryScoreDerivatives, wordVectorDerivatives, delta);
		}

		public virtual void BackpropDerivative(Tree tree, IList<string> words, IdentityHashMap<Tree, SimpleMatrix> nodeVectors, TwoDimensionalMap<string, string, SimpleMatrix> binaryW_dfs, IDictionary<string, SimpleMatrix> unaryW_dfs, TwoDimensionalMap
			<string, string, SimpleMatrix> binaryScoreDerivatives, IDictionary<string, SimpleMatrix> unaryScoreDerivatives, IDictionary<string, SimpleMatrix> wordVectorDerivatives, SimpleMatrix deltaUp)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			if (tree.IsPreTerminal())
			{
				if (op.trainOptions.trainWordVectors)
				{
					string word = tree.Children()[0].Label().Value();
					word = dvModel.GetVocabWord(word);
					//        SimpleMatrix currentVector = nodeVectors.get(tree);
					//        SimpleMatrix currentVectorDerivative = nonlinearityVectorToDerivative(currentVector);
					//        SimpleMatrix derivative = deltaUp.elementMult(currentVectorDerivative);
					SimpleMatrix derivative = deltaUp;
					wordVectorDerivatives[word] = wordVectorDerivatives[word].Plus(derivative);
				}
				return;
			}
			SimpleMatrix currentVector = nodeVectors[tree];
			SimpleMatrix currentVectorDerivative = NeuralUtils.ElementwiseApplyTanhDerivative(currentVector);
			SimpleMatrix scoreW = dvModel.GetScoreWForNode(tree);
			currentVectorDerivative = currentVectorDerivative.ElementMult(scoreW.Transpose());
			// the delta that is used at the current nodes
			SimpleMatrix deltaCurrent = deltaUp.Plus(currentVectorDerivative);
			SimpleMatrix W = dvModel.GetWForNode(tree);
			SimpleMatrix WTdelta = W.Transpose().Mult(deltaCurrent);
			if (tree.Children().Length == 2)
			{
				//TODO: RS: Change to the nice "getWForNode" setup?
				string leftLabel = dvModel.BasicCategory(tree.Children()[0].Label().Value());
				string rightLabel = dvModel.BasicCategory(tree.Children()[1].Label().Value());
				binaryScoreDerivatives.Put(leftLabel, rightLabel, binaryScoreDerivatives.Get(leftLabel, rightLabel).Plus(currentVector.Transpose()));
				SimpleMatrix leftVector = nodeVectors[tree.Children()[0]];
				SimpleMatrix rightVector = nodeVectors[tree.Children()[1]];
				SimpleMatrix childrenVector = NeuralUtils.ConcatenateWithBias(leftVector, rightVector);
				if (op.trainOptions.useContextWords)
				{
					childrenVector = ConcatenateContextWords(childrenVector, tree.GetSpan(), words);
				}
				SimpleMatrix W_df = deltaCurrent.Mult(childrenVector.Transpose());
				binaryW_dfs.Put(leftLabel, rightLabel, binaryW_dfs.Get(leftLabel, rightLabel).Plus(W_df));
				// and then recurse
				SimpleMatrix leftDerivative = NeuralUtils.ElementwiseApplyTanhDerivative(leftVector);
				SimpleMatrix rightDerivative = NeuralUtils.ElementwiseApplyTanhDerivative(rightVector);
				SimpleMatrix leftWTDelta = WTdelta.ExtractMatrix(0, deltaCurrent.NumRows(), 0, 1);
				SimpleMatrix rightWTDelta = WTdelta.ExtractMatrix(deltaCurrent.NumRows(), deltaCurrent.NumRows() * 2, 0, 1);
				BackpropDerivative(tree.Children()[0], words, nodeVectors, binaryW_dfs, unaryW_dfs, binaryScoreDerivatives, unaryScoreDerivatives, wordVectorDerivatives, leftDerivative.ElementMult(leftWTDelta));
				BackpropDerivative(tree.Children()[1], words, nodeVectors, binaryW_dfs, unaryW_dfs, binaryScoreDerivatives, unaryScoreDerivatives, wordVectorDerivatives, rightDerivative.ElementMult(rightWTDelta));
			}
			else
			{
				if (tree.Children().Length == 1)
				{
					string childLabel = dvModel.BasicCategory(tree.Children()[0].Label().Value());
					unaryScoreDerivatives[childLabel] = unaryScoreDerivatives[childLabel].Plus(currentVector.Transpose());
					SimpleMatrix childVector = nodeVectors[tree.Children()[0]];
					SimpleMatrix childVectorWithBias = NeuralUtils.ConcatenateWithBias(childVector);
					if (op.trainOptions.useContextWords)
					{
						childVectorWithBias = ConcatenateContextWords(childVectorWithBias, tree.GetSpan(), words);
					}
					SimpleMatrix W_df = deltaCurrent.Mult(childVectorWithBias.Transpose());
					// System.out.println("unary backprop derivative for " + childLabel);
					// System.out.println("Old transform:");
					// System.out.println(unaryW_dfs.get(childLabel));
					// System.out.println(" Delta:");
					// System.out.println(W_df.scale(scale));
					unaryW_dfs[childLabel] = unaryW_dfs[childLabel].Plus(W_df);
					// and then recurse
					SimpleMatrix childDerivative = NeuralUtils.ElementwiseApplyTanhDerivative(childVector);
					//SimpleMatrix childDerivative = childVector;
					SimpleMatrix childWTDelta = WTdelta.ExtractMatrix(0, deltaCurrent.NumRows(), 0, 1);
					BackpropDerivative(tree.Children()[0], words, nodeVectors, binaryW_dfs, unaryW_dfs, binaryScoreDerivatives, unaryScoreDerivatives, wordVectorDerivatives, childDerivative.ElementMult(childWTDelta));
				}
			}
		}
	}
}
