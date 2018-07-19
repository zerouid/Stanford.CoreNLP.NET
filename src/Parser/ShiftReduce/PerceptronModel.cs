using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Text;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	[System.Serializable]
	public class PerceptronModel : BaseModel
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Shiftreduce.PerceptronModel));

		private float learningRate = 1.0f;

		internal IDictionary<string, Weight> featureWeights;

		internal readonly FeatureFactory featureFactory;

		public PerceptronModel(ShiftReduceOptions op, IIndex<ITransition> transitionIndex, ICollection<string> knownStates, ICollection<string> rootStates, ICollection<string> rootOnlyStates)
			: base(op, transitionIndex, knownStates, rootStates, rootOnlyStates)
		{
			// Serializable
			this.featureWeights = Generics.NewHashMap();
			string[] classes = op.featureFactoryClass.Split(";");
			if (classes.Length == 1)
			{
				this.featureFactory = ReflectionLoading.LoadByReflection(classes[0]);
			}
			else
			{
				FeatureFactory[] factories = new FeatureFactory[classes.Length];
				for (int i = 0; i < classes.Length; ++i)
				{
					int paren = classes[i].IndexOf('(');
					if (paren >= 0)
					{
						string arg = Sharpen.Runtime.Substring(classes[i], paren + 1, classes[i].Length - 1);
						factories[i] = ReflectionLoading.LoadByReflection(Sharpen.Runtime.Substring(classes[i], 0, paren), arg);
					}
					else
					{
						factories[i] = ReflectionLoading.LoadByReflection(classes[i]);
					}
				}
				this.featureFactory = new CombinationFeatureFactory(factories);
			}
		}

		public PerceptronModel(Edu.Stanford.Nlp.Parser.Shiftreduce.PerceptronModel other)
			: base(other)
		{
			this.featureFactory = other.featureFactory;
			this.featureWeights = Generics.NewHashMap();
			foreach (string feature in other.featureWeights.Keys)
			{
				featureWeights[feature] = new Weight(other.featureWeights[feature]);
			}
		}

		private static readonly NumberFormat Nf = new DecimalFormat("0.00");

		private static readonly NumberFormat Filename = new DecimalFormat("0000");

		public virtual void AverageScoredModels(ICollection<ScoredObject<Edu.Stanford.Nlp.Parser.Shiftreduce.PerceptronModel>> scoredModels)
		{
			if (scoredModels.IsEmpty())
			{
				throw new ArgumentException("Cannot average empty models");
			}
			log.Info("Averaging " + scoredModels.Count + " models with scores");
			foreach (ScoredObject<Edu.Stanford.Nlp.Parser.Shiftreduce.PerceptronModel> model in scoredModels)
			{
				log.Info(" " + Nf.Format(model.Score()));
			}
			log.Info();
			IList<Edu.Stanford.Nlp.Parser.Shiftreduce.PerceptronModel> models = CollectionUtils.TransformAsList(scoredModels, null);
			AverageModels(models);
		}

		public virtual void AverageModels(ICollection<Edu.Stanford.Nlp.Parser.Shiftreduce.PerceptronModel> models)
		{
			if (models.IsEmpty())
			{
				throw new ArgumentException("Cannot average empty models");
			}
			ICollection<string> features = Generics.NewHashSet();
			foreach (Edu.Stanford.Nlp.Parser.Shiftreduce.PerceptronModel model in models)
			{
				foreach (string feature in model.featureWeights.Keys)
				{
					features.Add(feature);
				}
			}
			featureWeights = Generics.NewHashMap();
			foreach (string feature_1 in features)
			{
				featureWeights[feature_1] = new Weight();
			}
			int numModels = models.Count;
			foreach (string feature_2 in features)
			{
				foreach (Edu.Stanford.Nlp.Parser.Shiftreduce.PerceptronModel model_1 in models)
				{
					if (!model_1.featureWeights.Contains(feature_2))
					{
						continue;
					}
					featureWeights[feature_2].AddScaled(model_1.featureWeights[feature_2], 1.0f / numModels);
				}
			}
		}

		/// <summary>Iterate over the feature weight map.</summary>
		/// <remarks>
		/// Iterate over the feature weight map.
		/// For each feature, remove all transitions with score of 0.
		/// Any feature with no transitions left is then removed
		/// </remarks>
		private void CondenseFeatures()
		{
			IEnumerator<string> featureIt = featureWeights.Keys.GetEnumerator();
			while (featureIt.MoveNext())
			{
				string feature = featureIt.Current;
				Weight weights = featureWeights[feature];
				weights.Condense();
				if (weights.Size() == 0)
				{
					featureIt.Remove();
				}
			}
		}

		private void FilterFeatures(ICollection<string> keep)
		{
			IEnumerator<string> featureIt = featureWeights.Keys.GetEnumerator();
			while (featureIt.MoveNext())
			{
				if (!keep.Contains(featureIt.Current))
				{
					featureIt.Remove();
				}
			}
		}

		/// <summary>Output some random facts about the model</summary>
		public virtual void OutputStats()
		{
			log.Info("Number of known features: " + featureWeights.Count);
			int numWeights = 0;
			foreach (KeyValuePair<string, Weight> stringWeightEntry in featureWeights)
			{
				numWeights += stringWeightEntry.Value.Size();
			}
			log.Info("Number of non-zero weights: " + numWeights);
			int wordLength = 0;
			foreach (string feature in featureWeights.Keys)
			{
				wordLength += feature.Length;
			}
			log.Info("Total word length: " + wordLength);
			log.Info("Number of transitions: " + transitionIndex.Size());
		}

		/// <summary>Reconstruct the tag set that was used to train the model by decoding some of the features.</summary>
		/// <remarks>
		/// Reconstruct the tag set that was used to train the model by decoding some of the features.
		/// This is slow and brittle but should work!  Only if "-" is not in the tag set....
		/// </remarks>
		internal override ICollection<string> TagSet()
		{
			ICollection<string> tags = Generics.NewHashSet();
			Pattern p1 = Pattern.Compile("Q0TQ1T-([^-]+)-.*");
			Pattern p2 = Pattern.Compile("S0T-(.*)");
			foreach (string feat in featureWeights.Keys)
			{
				Matcher m1 = p1.Matcher(feat);
				if (m1.Matches())
				{
					tags.Add(m1.Group(1));
				}
				Matcher m2 = p2.Matcher(feat);
				if (m2.Matches())
				{
					tags.Add(m2.Group(1));
				}
			}
			// Add the end of sentence tag!
			// The SR model doesn't use it, but other models do and report it.
			// todo [cdm 2014]: Maybe we should reverse the convention here?!?
			tags.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosTag);
			return tags;
		}

		/// <summary>Convenience method: returns one highest scoring transition, without any ParserConstraints</summary>
		private ScoredObject<int> FindHighestScoringTransition(State state, IList<string> features, bool requireLegal)
		{
			ICollection<ScoredObject<int>> transitions = FindHighestScoringTransitions(state, features, requireLegal, 1, null);
			if (transitions.IsEmpty())
			{
				return null;
			}
			return transitions.GetEnumerator().Current;
		}

		public override ICollection<ScoredObject<int>> FindHighestScoringTransitions(State state, bool requireLegal, int numTransitions, IList<ParserConstraint> constraints)
		{
			IList<string> features = featureFactory.Featurize(state);
			return FindHighestScoringTransitions(state, features, requireLegal, numTransitions, constraints);
		}

		private ICollection<ScoredObject<int>> FindHighestScoringTransitions(State state, IList<string> features, bool requireLegal, int numTransitions, IList<ParserConstraint> constraints)
		{
			float[] scores = new float[transitionIndex.Size()];
			foreach (string feature in features)
			{
				Weight weight = featureWeights[feature];
				if (weight == null)
				{
					// Features not in our index are ignored
					continue;
				}
				weight.Score(scores);
			}
			PriorityQueue<ScoredObject<int>> queue = new PriorityQueue<ScoredObject<int>>(numTransitions + 1, ScoredComparator.AscendingComparator);
			for (int i = 0; i < scores.Length; ++i)
			{
				if (!requireLegal || transitionIndex.Get(i).IsLegal(state, constraints))
				{
					queue.Add(new ScoredObject<int>(i, scores[i]));
					if (queue.Count > numTransitions)
					{
						queue.Poll();
					}
				}
			}
			return queue;
		}

		private class Update
		{
			internal readonly IList<string> features;

			internal readonly int goldTransition;

			internal readonly int predictedTransition;

			internal readonly float delta;

			internal Update(IList<string> features, int goldTransition, int predictedTransition, float delta)
			{
				this.features = features;
				this.goldTransition = goldTransition;
				this.predictedTransition = predictedTransition;
				this.delta = delta;
			}
		}

		private Pair<int, int> TrainTree(int index, IList<Tree> binarizedTrees, IList<IList<ITransition>> transitionLists, IList<PerceptronModel.Update> updates, Oracle oracle)
		{
			int numCorrect = 0;
			int numWrong = 0;
			Tree tree = binarizedTrees[index];
			ReorderingOracle reorderer = null;
			if (op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.ReorderOracle || op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.ReorderBeam)
			{
				reorderer = new ReorderingOracle(op);
			}
			// TODO.  This training method seems to be working in that it
			// trains models just like the gold and early termination methods do.
			// However, it causes the feature space to go crazy.  Presumably
			// leaving out features with low weights or low frequencies would
			// significantly help with that.  Otherwise, not sure how to keep
			// it under control.
			if (op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.Oracle)
			{
				State state = ShiftReduceParser.InitialStateFromGoldTagTree(tree);
				while (!state.IsFinished())
				{
					IList<string> features = featureFactory.Featurize(state);
					ScoredObject<int> prediction = FindHighestScoringTransition(state, features, true);
					if (prediction == null)
					{
						throw new AssertionError("Did not find a legal transition");
					}
					int predictedNum = prediction.Object();
					ITransition predicted = transitionIndex.Get(predictedNum);
					OracleTransition gold = oracle.GoldTransition(index, state);
					if (gold.IsCorrect(predicted))
					{
						numCorrect++;
						if (gold.transition != null && !gold.transition.Equals(predicted))
						{
							int transitionNum = transitionIndex.IndexOf(gold.transition);
							if (transitionNum < 0)
							{
								// TODO: do we want to add unary transitions which are
								// only possible when the parser has gone off the rails?
								continue;
							}
							updates.Add(new PerceptronModel.Update(features, transitionNum, -1, learningRate));
						}
					}
					else
					{
						numWrong++;
						int transitionNum = -1;
						if (gold.transition != null)
						{
							transitionNum = transitionIndex.IndexOf(gold.transition);
						}
						// TODO: this can theoretically result in a -1 gold
						// transition if the transition exists, but is a
						// CompoundUnaryTransition which only exists because the
						// parser is wrong.  Do we want to add those transitions?
						updates.Add(new PerceptronModel.Update(features, transitionNum, predictedNum, learningRate));
					}
					state = predicted.Apply(state);
				}
			}
			else
			{
				if (op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.Beam || op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.ReorderBeam)
				{
					if (op.TrainOptions().beamSize <= 0)
					{
						throw new ArgumentException("Illegal beam size " + op.TrainOptions().beamSize);
					}
					IList<ITransition> transitions = Generics.NewLinkedList(transitionLists[index]);
					PriorityQueue<State> agenda = new PriorityQueue<State>(op.TrainOptions().beamSize + 1, ScoredComparator.AscendingComparator);
					State goldState = ShiftReduceParser.InitialStateFromGoldTagTree(tree);
					agenda.Add(goldState);
					// int transitionCount = 0;
					while (transitions.Count > 0)
					{
						ITransition goldTransition = transitions[0];
						ITransition highestScoringTransitionFromGoldState = null;
						double highestScoreFromGoldState = 0.0;
						PriorityQueue<State> newAgenda = new PriorityQueue<State>(op.TrainOptions().beamSize + 1, ScoredComparator.AscendingComparator);
						State highestScoringState = null;
						State highestCurrentState = null;
						foreach (State currentState in agenda)
						{
							bool isGoldState = (op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.ReorderBeam && goldState.AreTransitionsEqual(currentState));
							IList<string> features = featureFactory.Featurize(currentState);
							ICollection<ScoredObject<int>> stateTransitions = FindHighestScoringTransitions(currentState, features, true, op.TrainOptions().beamSize, null);
							foreach (ScoredObject<int> transition in stateTransitions)
							{
								State newState = transitionIndex.Get(transition.Object()).Apply(currentState, transition.Score());
								newAgenda.Add(newState);
								if (newAgenda.Count > op.TrainOptions().beamSize)
								{
									newAgenda.Poll();
								}
								if (highestScoringState == null || highestScoringState.Score() < newState.Score())
								{
									highestScoringState = newState;
									highestCurrentState = currentState;
								}
								if (isGoldState && (highestScoringTransitionFromGoldState == null || transition.Score() > highestScoreFromGoldState))
								{
									highestScoringTransitionFromGoldState = transitionIndex.Get(transition.Object());
									highestScoreFromGoldState = transition.Score();
								}
							}
						}
						// This can happen if the REORDER_BEAM method backs itself
						// into a corner, such as transitioning to something that
						// can't have a FinalizeTransition applied.  This doesn't
						// happen for the BEAM method because in that case the correct
						// state (eg one with ROOT) isn't on the agenda so it stops.
						if (op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.ReorderBeam && highestScoringTransitionFromGoldState == null)
						{
							break;
						}
						State newGoldState = goldTransition.Apply(goldState, 0.0);
						// if highest scoring state used the correct transition, no training
						// otherwise, down the last transition, up the correct
						if (!newGoldState.AreTransitionsEqual(highestScoringState))
						{
							++numWrong;
							IList<string> goldFeatures = featureFactory.Featurize(goldState);
							int lastTransition = transitionIndex.IndexOf(highestScoringState.transitions.Peek());
							updates.Add(new PerceptronModel.Update(featureFactory.Featurize(highestCurrentState), -1, lastTransition, learningRate));
							updates.Add(new PerceptronModel.Update(goldFeatures, transitionIndex.IndexOf(goldTransition), -1, learningRate));
							if (op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.Beam)
							{
								// If the correct state has fallen off the agenda, break
								if (!ShiftReduceUtils.FindStateOnAgenda(newAgenda, newGoldState))
								{
									break;
								}
								else
								{
									transitions.Remove(0);
								}
							}
							else
							{
								if (op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.ReorderBeam)
								{
									if (!ShiftReduceUtils.FindStateOnAgenda(newAgenda, newGoldState))
									{
										if (!reorderer.Reorder(goldState, highestScoringTransitionFromGoldState, transitions))
										{
											break;
										}
										newGoldState = highestScoringTransitionFromGoldState.Apply(goldState);
										if (!ShiftReduceUtils.FindStateOnAgenda(newAgenda, newGoldState))
										{
											break;
										}
									}
									else
									{
										transitions.Remove(0);
									}
								}
							}
						}
						else
						{
							++numCorrect;
							transitions.Remove(0);
						}
						goldState = newGoldState;
						agenda = newAgenda;
					}
				}
				else
				{
					if (op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.ReorderOracle || op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.EarlyTermination || op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod
						.Gold)
					{
						State state = ShiftReduceParser.InitialStateFromGoldTagTree(tree);
						IList<ITransition> transitions = transitionLists[index];
						transitions = Generics.NewLinkedList(transitions);
						bool keepGoing = true;
						while (transitions.Count > 0 && keepGoing)
						{
							ITransition transition = transitions[0];
							int transitionNum = transitionIndex.IndexOf(transition);
							IList<string> features = featureFactory.Featurize(state);
							int predictedNum = FindHighestScoringTransition(state, features, false).Object();
							ITransition predicted = transitionIndex.Get(predictedNum);
							if (transitionNum == predictedNum)
							{
								transitions.Remove(0);
								state = transition.Apply(state);
								numCorrect++;
							}
							else
							{
								numWrong++;
								// TODO: allow weighted features, weighted training, etc
								updates.Add(new PerceptronModel.Update(features, transitionNum, predictedNum, learningRate));
								switch (op.TrainOptions().trainingMethod)
								{
									case ShiftReduceTrainOptions.TrainingMethod.EarlyTermination:
									{
										keepGoing = false;
										break;
									}

									case ShiftReduceTrainOptions.TrainingMethod.Gold:
									{
										transitions.Remove(0);
										state = transition.Apply(state);
										break;
									}

									case ShiftReduceTrainOptions.TrainingMethod.ReorderOracle:
									{
										keepGoing = reorderer.Reorder(state, predicted, transitions);
										if (keepGoing)
										{
											state = predicted.Apply(state);
										}
										break;
									}

									default:
									{
										throw new ArgumentException("Unexpected method " + op.TrainOptions().trainingMethod);
									}
								}
							}
						}
					}
				}
			}
			return Pair.MakePair(numCorrect, numWrong);
		}

		private class TrainTreeProcessor : IThreadsafeProcessor<int, Pair<int, int>>
		{
			internal IList<Tree> binarizedTrees;

			internal IList<IList<ITransition>> transitionLists;

			internal IList<PerceptronModel.Update> updates;

			internal Oracle oracle;

			public TrainTreeProcessor(PerceptronModel _enclosing, IList<Tree> binarizedTrees, IList<IList<ITransition>> transitionLists, IList<PerceptronModel.Update> updates, Oracle oracle)
			{
				this._enclosing = _enclosing;
				// this needs to be a synchronized list
				this.binarizedTrees = binarizedTrees;
				this.transitionLists = transitionLists;
				this.updates = updates;
				this.oracle = oracle;
			}

			public virtual Pair<int, int> Process(int index)
			{
				return this._enclosing.TrainTree(index, this.binarizedTrees, this.transitionLists, this.updates, this.oracle);
			}

			public virtual PerceptronModel.TrainTreeProcessor NewInstance()
			{
				// already threadsafe
				return this;
			}

			private readonly PerceptronModel _enclosing;
		}

		/// <summary>
		/// Trains a batch of trees and returns the following: a list of
		/// Update objects, the number of transitions correct, and the number
		/// of transitions wrong.
		/// </summary>
		/// <remarks>
		/// Trains a batch of trees and returns the following: a list of
		/// Update objects, the number of transitions correct, and the number
		/// of transitions wrong.
		/// If the model is trained with multiple threads, it is expected
		/// that a valid MulticoreWrapper is passed in which does the
		/// processing.  In that case, the processing is done on all of the
		/// trees without updating any weights, which allows the results for
		/// multithreaded training to be reproduced.
		/// </remarks>
		private Triple<IList<PerceptronModel.Update>, int, int> TrainBatch(IList<int> indices, IList<Tree> binarizedTrees, IList<IList<ITransition>> transitionLists, IList<PerceptronModel.Update> updates, Oracle oracle, MulticoreWrapper<int, Pair<int
			, int>> wrapper)
		{
			int numCorrect = 0;
			int numWrong = 0;
			if (op.trainOptions.trainingThreads == 1)
			{
				foreach (int index in indices)
				{
					Pair<int, int> count = TrainTree(index, binarizedTrees, transitionLists, updates, oracle);
					numCorrect += count.first;
					numWrong += count.second;
				}
			}
			else
			{
				foreach (int index in indices)
				{
					wrapper.Put(index);
				}
				wrapper.Join(false);
				while (wrapper.Peek())
				{
					Pair<int, int> result = wrapper.Poll();
					numCorrect += result.first;
					numWrong += result.second;
				}
			}
			return new Triple<IList<PerceptronModel.Update>, int, int>(updates, numCorrect, numWrong);
		}

		private void TrainModel(string serializedPath, Edu.Stanford.Nlp.Tagger.Common.Tagger tagger, Random random, IList<Tree> binarizedTrees, IList<IList<ITransition>> transitionLists, Treebank devTreebank, int nThreads, ICollection<string> allowedFeatures
			)
		{
			double bestScore = 0.0;
			int bestIteration = 0;
			PriorityQueue<ScoredObject<PerceptronModel>> bestModels = null;
			if (op.TrainOptions().averagedModels > 0)
			{
				bestModels = new PriorityQueue<ScoredObject<PerceptronModel>>(op.TrainOptions().averagedModels + 1, ScoredComparator.AscendingComparator);
			}
			IList<int> indices = Generics.NewArrayList();
			for (int i = 0; i < binarizedTrees.Count; ++i)
			{
				indices.Add(i);
			}
			Oracle oracle = null;
			if (op.TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.Oracle)
			{
				oracle = new Oracle(binarizedTrees, op.compoundUnaries, rootStates);
			}
			IList<PerceptronModel.Update> updates = Generics.NewArrayList();
			MulticoreWrapper<int, Pair<int, int>> wrapper = null;
			if (nThreads != 1)
			{
				updates = Java.Util.Collections.SynchronizedList(updates);
				wrapper = new MulticoreWrapper<int, Pair<int, int>>(op.trainOptions.trainingThreads, new PerceptronModel.TrainTreeProcessor(this, binarizedTrees, transitionLists, updates, oracle));
			}
			IntCounter<string> featureFrequencies = null;
			if (op.TrainOptions().featureFrequencyCutoff > 1)
			{
				featureFrequencies = new IntCounter<string>();
			}
			for (int iteration = 1; iteration <= op.trainOptions.trainingIterations; ++iteration)
			{
				Timing trainingTimer = new Timing();
				int numCorrect = 0;
				int numWrong = 0;
				Java.Util.Collections.Shuffle(indices, random);
				for (int start = 0; start < indices.Count; start += op.trainOptions.batchSize)
				{
					int end = Math.Min(start + op.trainOptions.batchSize, indices.Count);
					Triple<IList<PerceptronModel.Update>, int, int> result = TrainBatch(indices.SubList(start, end), binarizedTrees, transitionLists, updates, oracle, wrapper);
					numCorrect += result.second;
					numWrong += result.third;
					foreach (PerceptronModel.Update update in result.first)
					{
						foreach (string feature in update.features)
						{
							if (allowedFeatures != null && !allowedFeatures.Contains(feature))
							{
								continue;
							}
							Weight weights = featureWeights[feature];
							if (weights == null)
							{
								weights = new Weight();
								featureWeights[feature] = weights;
							}
							weights.UpdateWeight(update.goldTransition, update.delta);
							weights.UpdateWeight(update.predictedTransition, -update.delta);
							if (featureFrequencies != null)
							{
								featureFrequencies.IncrementCount(feature, (update.goldTransition >= 0 && update.predictedTransition >= 0) ? 2 : 1);
							}
						}
					}
					updates.Clear();
				}
				trainingTimer.Done("Iteration " + iteration);
				log.Info("While training, got " + numCorrect + " transitions correct and " + numWrong + " transitions wrong");
				OutputStats();
				double labelF1 = 0.0;
				if (devTreebank != null)
				{
					EvaluateTreebank evaluator = new EvaluateTreebank(op, null, new ShiftReduceParser(op, this), tagger);
					evaluator.TestOnTreebank(devTreebank);
					labelF1 = evaluator.GetLBScore();
					log.Info("Label F1 after " + iteration + " iterations: " + labelF1);
					if (labelF1 > bestScore)
					{
						log.Info("New best dev score (previous best " + bestScore + ")");
						bestScore = labelF1;
						bestIteration = iteration;
					}
					else
					{
						log.Info("Failed to improve for " + (iteration - bestIteration) + " iteration(s) on previous best score of " + bestScore);
						if (op.trainOptions.stalledIterationLimit > 0 && (iteration - bestIteration >= op.trainOptions.stalledIterationLimit))
						{
							log.Info("Failed to improve for too long, stopping training");
							break;
						}
					}
					log.Info();
					if (bestModels != null)
					{
						bestModels.Add(new ScoredObject<PerceptronModel>(new PerceptronModel(this), labelF1));
						if (bestModels.Count > op.TrainOptions().averagedModels)
						{
							bestModels.Poll();
						}
					}
				}
				if (op.TrainOptions().saveIntermediateModels && serializedPath != null && op.trainOptions.debugOutputFrequency > 0)
				{
					string tempName = Sharpen.Runtime.Substring(serializedPath, 0, serializedPath.Length - 7) + "-" + Filename.Format(iteration) + "-" + Nf.Format(labelF1) + ".ser.gz";
					ShiftReduceParser temp = new ShiftReduceParser(op, this);
					temp.SaveModel(tempName);
				}
				// TODO: we could save a cutoff version of the model,
				// especially if we also get a dev set number for it, but that
				// might be overkill
				if (iteration % 10 == 0 && op.TrainOptions().decayLearningRate > 0.0)
				{
					learningRate *= op.TrainOptions().decayLearningRate;
				}
			}
			// end for iterations
			if (wrapper != null)
			{
				wrapper.Join();
			}
			if (bestModels != null)
			{
				if (op.TrainOptions().cvAveragedModels && devTreebank != null)
				{
					IList<ScoredObject<PerceptronModel>> models = Generics.NewArrayList();
					while (bestModels.Count > 0)
					{
						models.Add(bestModels.Poll());
					}
					Java.Util.Collections.Reverse(models);
					double bestF1 = 0.0;
					int bestSize = 0;
					for (int i_1 = 1; i_1 <= models.Count; ++i_1)
					{
						log.Info("Testing with " + i_1 + " models averaged together");
						// TODO: this is kind of ugly, would prefer a separate object
						AverageScoredModels(models.SubList(0, i_1));
						ShiftReduceParser temp = new ShiftReduceParser(op, this);
						EvaluateTreebank evaluator = new EvaluateTreebank(temp.GetOp(), null, temp, tagger);
						evaluator.TestOnTreebank(devTreebank);
						double labelF1 = evaluator.GetLBScore();
						log.Info("Label F1 for " + i_1 + " models: " + labelF1);
						if (labelF1 > bestF1)
						{
							bestF1 = labelF1;
							bestSize = i_1;
						}
					}
					AverageScoredModels(models.SubList(0, bestSize));
				}
				else
				{
					AverageScoredModels(bestModels);
				}
			}
			// TODO: perhaps we should filter the features and then get dev
			// set scores.  That way we can merge the models which are best
			// after filtering.
			if (featureFrequencies != null)
			{
				FilterFeatures(featureFrequencies.KeysAbove(op.TrainOptions().featureFrequencyCutoff));
			}
			CondenseFeatures();
		}

		/// <summary>
		/// Will train the model on the given treebank, using devTreebank as
		/// a dev set.
		/// </summary>
		/// <remarks>
		/// Will train the model on the given treebank, using devTreebank as
		/// a dev set.  If op.retrainAfterCutoff is set, will rerun training
		/// after the first time through on a limited set of features.
		/// </remarks>
		public override void TrainModel(string serializedPath, Edu.Stanford.Nlp.Tagger.Common.Tagger tagger, Random random, IList<Tree> binarizedTrees, IList<IList<ITransition>> transitionLists, Treebank devTreebank, int nThreads)
		{
			if (op.TrainOptions().retrainAfterCutoff && op.TrainOptions().featureFrequencyCutoff > 0)
			{
				string tempName = Sharpen.Runtime.Substring(serializedPath, 0, serializedPath.Length - 7) + "-" + "temp.ser.gz";
				TrainModel(tempName, tagger, random, binarizedTrees, transitionLists, devTreebank, nThreads, null);
				ShiftReduceParser temp = new ShiftReduceParser(op, this);
				temp.SaveModel(tempName);
				ICollection<string> features = featureWeights.Keys;
				featureWeights = Generics.NewHashMap();
				TrainModel(serializedPath, tagger, random, binarizedTrees, transitionLists, devTreebank, nThreads, features);
			}
			else
			{
				TrainModel(serializedPath, tagger, random, binarizedTrees, transitionLists, devTreebank, nThreads, null);
			}
		}

		private const long serialVersionUID = 1;
	}
}
