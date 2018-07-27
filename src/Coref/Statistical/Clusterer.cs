using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>System for building up coreference clusters incrementally, merging a pair of clusters each step.</summary>
	/// <remarks>
	/// System for building up coreference clusters incrementally, merging a pair of clusters each step.
	/// Trained with a variant of the SEARN imitation learning algorithm.
	/// </remarks>
	/// <author>Kevin Clark</author>
	public class Clusterer
	{
		private const bool UseClassification = true;

		private const bool UseRanking = true;

		private const bool LeftToRight = false;

		private const bool ExactLoss = false;

		private const double MucWeight = 0.25;

		private const double ExpertDecay = 0.0;

		private const double LearningRate = 0.05;

		private const int BufferSizeMultiplier = 20;

		private const int MaxDocs = 1000;

		private const int RetrainIterations = 100;

		private const int NumEpochs = 15;

		private const int EvalFrequency = 1;

		private const int MinPairs = 10;

		private const double MinPairwiseScore = 0.15;

		private const int EarlyStopThreshold = 1000;

		private const double EarlyStopVal = 1500 / 0.2;

		public static int currentDocId = 0;

		public static int isTraining = 1;

		private readonly Clusterer.ClustererClassifier classifier;

		private readonly Random random;

		public Clusterer()
		{
			random = new Random(0);
			classifier = new Clusterer.ClustererClassifier(LearningRate);
		}

		public Clusterer(string modelPath)
		{
			random = new Random(0);
			classifier = new Clusterer.ClustererClassifier(modelPath, LearningRate);
		}

		public virtual IList<Pair<int, int>> GetClusterMerges(ClustererDataLoader.ClustererDoc doc)
		{
			IList<Pair<int, int>> merges = new List<Pair<int, int>>();
			Clusterer.State currentState = new Clusterer.State(doc);
			while (!currentState.IsComplete())
			{
				Pair<int, int> currentPair = currentState.mentionPairs[currentState.currentIndex];
				if (currentState.DoBestAction(classifier))
				{
					merges.Add(currentPair);
				}
			}
			return merges;
		}

		public virtual void DoTraining(string modelName)
		{
			classifier.SetWeight("bias", -0.3);
			classifier.SetWeight("anaphorSeen", -1);
			classifier.SetWeight("max-ranking", 1);
			classifier.SetWeight("bias-single", -0.3);
			classifier.SetWeight("anaphorSeen-single", -1);
			classifier.SetWeight("max-ranking-single", 1);
			string outputPath = StatisticalCorefTrainer.clusteringModelsPath + modelName + "/";
			File outDir = new File(outputPath);
			if (!outDir.Exists())
			{
				outDir.Mkdir();
			}
			PrintWriter progressWriter;
			IList<ClustererDataLoader.ClustererDoc> trainDocs;
			try
			{
				PrintWriter configWriter = new PrintWriter(outputPath + "config", "UTF-8");
				configWriter.Print(StatisticalCorefTrainer.FieldValues(this));
				configWriter.Close();
				progressWriter = new PrintWriter(outputPath + "progress", "UTF-8");
				Redwood.Log("scoref.train", "Loading training data");
				StatisticalCorefTrainer.SetDataPath("dev");
				trainDocs = ClustererDataLoader.LoadDocuments(MaxDocs);
			}
			catch (Exception e)
			{
				throw new Exception("Error setting up training", e);
			}
			double bestTrainScore = 0;
			IList<IList<Pair<Clusterer.CandidateAction, Clusterer.CandidateAction>>> examples = new List<IList<Pair<Clusterer.CandidateAction, Clusterer.CandidateAction>>>();
			for (int iteration = 0; iteration < RetrainIterations; iteration++)
			{
				Redwood.Log("scoref.train", "ITERATION " + iteration);
				classifier.PrintWeightVector(null);
				Redwood.Log("scoref.train", string.Empty);
				try
				{
					classifier.WriteWeights(outputPath + "model");
					classifier.PrintWeightVector(IOUtils.GetPrintWriter(outputPath + "weights"));
				}
				catch (Exception)
				{
					throw new Exception();
				}
				long start = Runtime.CurrentTimeMillis();
				Java.Util.Collections.Shuffle(trainDocs, random);
				examples = examples.SubList(Math.Max(0, examples.Count - BufferSizeMultiplier * trainDocs.Count), examples.Count);
				TrainPolicy(examples);
				if (iteration % EvalFrequency == 0)
				{
					double trainScore = EvaluatePolicy(trainDocs, true);
					if (trainScore > bestTrainScore)
					{
						bestTrainScore = trainScore;
						WriteModel("best", outputPath);
					}
					if (iteration % 10 == 0)
					{
						WriteModel("iter_" + iteration, outputPath);
					}
					WriteModel("last", outputPath);
					double timeElapsed = (Runtime.CurrentTimeMillis() - start) / 1000.0;
					double ffhr = Clusterer.State.ffHits / (double)(Clusterer.State.ffHits + Clusterer.State.ffMisses);
					double shr = Clusterer.State.sHits / (double)(Clusterer.State.sHits + Clusterer.State.sMisses);
					double fhr = featuresCacheHits / (double)(featuresCacheHits + featuresCacheMisses);
					Redwood.Log("scoref.train", modelName);
					Redwood.Log("scoref.train", string.Format("Best train: %.4f", bestTrainScore));
					Redwood.Log("scoref.train", string.Format("Time elapsed: %.2f", timeElapsed));
					Redwood.Log("scoref.train", string.Format("Cost hit rate: %.4f", ffhr));
					Redwood.Log("scoref.train", string.Format("Score hit rate: %.4f", shr));
					Redwood.Log("scoref.train", string.Format("Features hit rate: %.4f", fhr));
					Redwood.Log("scoref.train", string.Empty);
					progressWriter.Write(iteration + " " + trainScore + " " + " " + timeElapsed + " " + ffhr + " " + shr + " " + fhr + "\n");
					progressWriter.Flush();
				}
				foreach (ClustererDataLoader.ClustererDoc trainDoc in trainDocs)
				{
					examples.Add(RunPolicy(trainDoc, Math.Pow(ExpertDecay, (iteration + 1))));
				}
			}
			progressWriter.Close();
		}

		private void WriteModel(string name, string modelPath)
		{
			try
			{
				classifier.WriteWeights(modelPath + name + "_model.ser");
				classifier.PrintWeightVector(IOUtils.GetPrintWriter(modelPath + name + "_weights"));
			}
			catch (Exception)
			{
				throw new Exception();
			}
		}

		private void TrainPolicy(IList<IList<Pair<Clusterer.CandidateAction, Clusterer.CandidateAction>>> examples)
		{
			IList<Pair<Clusterer.CandidateAction, Clusterer.CandidateAction>> flattenedExamples = new List<Pair<Clusterer.CandidateAction, Clusterer.CandidateAction>>();
			examples.Stream().ForEach(null);
			for (int epoch = 0; epoch < NumEpochs; epoch++)
			{
				Java.Util.Collections.Shuffle(flattenedExamples, random);
				flattenedExamples.ForEach(null);
			}
			double totalCost = flattenedExamples.Stream().MapToDouble(null).Sum();
			Redwood.Log("scoref.train", string.Format("Training cost: %.4f", 100 * totalCost / flattenedExamples.Count));
		}

		private double EvaluatePolicy(IList<ClustererDataLoader.ClustererDoc> docs, bool training)
		{
			isTraining = 0;
			EvalUtils.B3Evaluator evaluator = new EvalUtils.B3Evaluator();
			foreach (ClustererDataLoader.ClustererDoc doc in docs)
			{
				Clusterer.State currentState = new Clusterer.State(doc);
				while (!currentState.IsComplete())
				{
					currentState.DoBestAction(classifier);
				}
				currentState.UpdateEvaluator(evaluator);
			}
			isTraining = 1;
			double score = evaluator.GetF1();
			Redwood.Log("scoref.train", string.Format("B3 F1 score on %s: %.4f", training ? "train" : "validate", score));
			return score;
		}

		private IList<Pair<Clusterer.CandidateAction, Clusterer.CandidateAction>> RunPolicy(ClustererDataLoader.ClustererDoc doc, double beta)
		{
			IList<Pair<Clusterer.CandidateAction, Clusterer.CandidateAction>> examples = new List<Pair<Clusterer.CandidateAction, Clusterer.CandidateAction>>();
			Clusterer.State currentState = new Clusterer.State(doc);
			while (!currentState.IsComplete())
			{
				Pair<Clusterer.CandidateAction, Clusterer.CandidateAction> actions = currentState.GetActions(classifier);
				if (actions == null)
				{
					continue;
				}
				examples.Add(actions);
				bool useExpert = random.NextDouble() < beta;
				double action1Score = useExpert ? -actions.first.cost : classifier.WeightFeatureProduct(actions.first.features);
				double action2Score = useExpert ? -actions.second.cost : classifier.WeightFeatureProduct(actions.second.features);
				currentState.DoAction(action1Score >= action2Score);
			}
			return examples;
		}

		private class GlobalFeatures
		{
			public bool anaphorSeen;

			public int currentIndex;

			public int size;

			public double docSize;
		}

		private class State
		{
			private static int sHits;

			private static int sMisses;

			private static int ffHits;

			private static int ffMisses;

			private readonly IDictionary<Clusterer.MergeKey, bool> hashedScores;

			private readonly IDictionary<long, double> hashedCosts;

			private readonly ClustererDataLoader.ClustererDoc doc;

			private readonly IList<Clusterer.Cluster> clusters;

			private readonly IDictionary<int, Clusterer.Cluster> mentionToCluster;

			private readonly IList<Pair<int, int>> mentionPairs;

			private readonly IList<Clusterer.GlobalFeatures> globalFeatures;

			private int currentIndex;

			private Clusterer.Cluster c1;

			private Clusterer.Cluster c2;

			private long hash;

			public State(ClustererDataLoader.ClustererDoc doc)
			{
				currentDocId = doc.id;
				this.doc = doc;
				this.hashedScores = new Dictionary<Clusterer.MergeKey, bool>();
				this.hashedCosts = new Dictionary<long, double>();
				this.clusters = new List<Clusterer.Cluster>();
				this.hash = 0;
				mentionToCluster = new Dictionary<int, Clusterer.Cluster>();
				foreach (int m in doc.mentions)
				{
					Clusterer.Cluster c = new Clusterer.Cluster(m);
					clusters.Add(c);
					mentionToCluster[m] = c;
					hash ^= c.hash * 7;
				}
				IList<Pair<int, int>> allPairs = new List<Pair<int, int>>(doc.classificationScores.KeySet());
				ICounter<Pair<int, int>> scores = UseRanking ? doc.rankingScores : doc.classificationScores;
				allPairs.Sort(null);
				int i = 0;
				for (i = 0; i < allPairs.Count; i++)
				{
					double score = scores.GetCount(allPairs[i]);
					if (score < MinPairwiseScore && i > MinPairs)
					{
						break;
					}
					if (i >= EarlyStopThreshold && i / score > EarlyStopVal)
					{
						break;
					}
				}
				mentionPairs = allPairs.SubList(0, i);
				ICounter<int> seenAnaphors = new ClassicCounter<int>();
				ICounter<int> seenAntecedents = new ClassicCounter<int>();
				globalFeatures = new List<Clusterer.GlobalFeatures>();
				for (int j = 0; j < allPairs.Count; j++)
				{
					Pair<int, int> mentionPair = allPairs[j];
					Clusterer.GlobalFeatures gf = new Clusterer.GlobalFeatures();
					gf.currentIndex = j;
					gf.anaphorSeen = seenAnaphors.ContainsKey(mentionPair.second);
					gf.size = mentionPairs.Count;
					gf.docSize = doc.mentions.Count / 300.0;
					globalFeatures.Add(gf);
					seenAnaphors.IncrementCount(mentionPair.second);
					seenAntecedents.IncrementCount(mentionPair.first);
				}
				currentIndex = 0;
				SetClusters();
			}

			public State(Clusterer.State state)
			{
				this.hashedScores = state.hashedScores;
				this.hashedCosts = state.hashedCosts;
				this.doc = state.doc;
				this.hash = state.hash;
				this.mentionPairs = state.mentionPairs;
				this.currentIndex = state.currentIndex;
				this.globalFeatures = state.globalFeatures;
				this.clusters = new List<Clusterer.Cluster>();
				this.mentionToCluster = new Dictionary<int, Clusterer.Cluster>();
				foreach (Clusterer.Cluster c in state.clusters)
				{
					Clusterer.Cluster copy = new Clusterer.Cluster(c);
					clusters.Add(copy);
					foreach (int m in copy.mentions)
					{
						mentionToCluster[m] = copy;
					}
				}
				SetClusters();
			}

			public virtual void SetClusters()
			{
				Pair<int, int> currentPair = mentionPairs[currentIndex];
				c1 = mentionToCluster[currentPair.first];
				c2 = mentionToCluster[currentPair.second];
			}

			public virtual void DoAction(bool isMerge)
			{
				if (isMerge)
				{
					if (c2.Size() > c1.Size())
					{
						Clusterer.Cluster tmp = c1;
						c1 = c2;
						c2 = tmp;
					}
					hash ^= 7 * c1.hash;
					hash ^= 7 * c2.hash;
					c1.Merge(c2);
					foreach (int m in c2.mentions)
					{
						mentionToCluster[m] = c1;
					}
					clusters.Remove(c2);
					hash ^= 7 * c1.hash;
				}
				currentIndex++;
				if (!IsComplete())
				{
					SetClusters();
				}
				while (c1 == c2)
				{
					currentIndex++;
					if (IsComplete())
					{
						break;
					}
					SetClusters();
				}
			}

			public virtual bool DoBestAction(Clusterer.ClustererClassifier classifier)
			{
				bool doMerge = hashedScores[new Clusterer.MergeKey(c1, c2, currentIndex)];
				if (doMerge == null)
				{
					ICounter<string> features = GetFeatures(doc, c1, c2, globalFeatures[currentIndex]);
					doMerge = classifier.WeightFeatureProduct(features) > 0;
					hashedScores[new Clusterer.MergeKey(c1, c2, currentIndex)] = doMerge;
					sMisses += isTraining;
				}
				else
				{
					sHits += isTraining;
				}
				DoAction(doMerge);
				return doMerge;
			}

			public virtual bool IsComplete()
			{
				return currentIndex >= mentionPairs.Count;
			}

			public virtual double GetFinalCost(Clusterer.ClustererClassifier classifier)
			{
				while (ExactLoss && !IsComplete())
				{
					if (hashedCosts.Contains(hash))
					{
						ffHits += isTraining;
						return hashedCosts[hash];
					}
					DoBestAction(classifier);
				}
				ffMisses += isTraining;
				double cost = EvalUtils.GetCombinedF1(MucWeight, doc.goldClusters, clusters, doc.mentionToGold, mentionToCluster);
				hashedCosts[hash] = cost;
				return cost;
			}

			public virtual void UpdateEvaluator(EvalUtils.IEvaluator evaluator)
			{
				evaluator.Update(doc.goldClusters, clusters, doc.mentionToGold, mentionToCluster);
			}

			public virtual Pair<Clusterer.CandidateAction, Clusterer.CandidateAction> GetActions(Clusterer.ClustererClassifier classifier)
			{
				ICounter<string> mergeFeatures = GetFeatures(doc, c1, c2, globalFeatures[currentIndex]);
				double mergeScore = Math.Exp(classifier.WeightFeatureProduct(mergeFeatures));
				hashedScores[new Clusterer.MergeKey(c1, c2, currentIndex)] = mergeScore > 0.5;
				Clusterer.State merge = new Clusterer.State(this);
				merge.DoAction(true);
				double mergeB3 = merge.GetFinalCost(classifier);
				Clusterer.State noMerge = new Clusterer.State(this);
				noMerge.DoAction(false);
				double noMergeB3 = noMerge.GetFinalCost(classifier);
				double weight = doc.mentions.Count / 100.0;
				double maxB3 = Math.Max(mergeB3, noMergeB3);
				return new Pair<Clusterer.CandidateAction, Clusterer.CandidateAction>(new Clusterer.CandidateAction(mergeFeatures, weight * (maxB3 - mergeB3)), new Clusterer.CandidateAction(new ClassicCounter<string>(), weight * (maxB3 - noMergeB3)));
			}
		}

		private class MergeKey
		{
			private readonly int hash;

			public MergeKey(Clusterer.Cluster c1, Clusterer.Cluster c2, int ind)
			{
				hash = (int)(c1.hash ^ c2.hash) + (2003 * ind) + currentDocId;
			}

			public override int GetHashCode()
			{
				return hash;
			}

			public override bool Equals(object o)
			{
				return ((Clusterer.MergeKey)o).hash == hash;
			}
		}

		public class Cluster
		{
			private static readonly IDictionary<Pair<int, int>, long> MentionHashes = new Dictionary<Pair<int, int>, long>();

			private static readonly Random Random = new Random(0);

			public readonly IList<int> mentions;

			public long hash;

			public Cluster(int m)
			{
				mentions = new List<int>();
				mentions.Add(m);
				hash = GetMentionHash(m);
			}

			public Cluster(Clusterer.Cluster c)
			{
				mentions = new List<int>(c.mentions);
				hash = c.hash;
			}

			public virtual void Merge(Clusterer.Cluster c)
			{
				Sharpen.Collections.AddAll(mentions, c.mentions);
				hash ^= c.hash;
			}

			public virtual int Size()
			{
				return mentions.Count;
			}

			public virtual long GetHash()
			{
				return hash;
			}

			private static long GetMentionHash(int m)
			{
				Pair<int, int> pair = new Pair<int, int>(m, currentDocId);
				long hash = MentionHashes[pair];
				if (hash == null)
				{
					hash = Random.NextLong();
					MentionHashes[pair] = hash;
				}
				return hash;
			}
		}

		private static int featuresCacheHits;

		private static int featuresCacheMisses;

		private static IDictionary<Clusterer.MergeKey, CompressedFeatureVector> featuresCache = new Dictionary<Clusterer.MergeKey, CompressedFeatureVector>();

		private static Compressor<string> compressor = new Compressor<string>();

		private static ICounter<string> GetFeatures(ClustererDataLoader.ClustererDoc doc, Pair<int, int> mentionPair, ICounter<Pair<int, int>> scores)
		{
			ICounter<string> features = new ClassicCounter<string>();
			if (!scores.ContainsKey(mentionPair))
			{
				mentionPair = new Pair<int, int>(mentionPair.second, mentionPair.first);
			}
			double score = scores.GetCount(mentionPair);
			features.IncrementCount("max", score);
			return features;
		}

		private static ICounter<string> GetFeatures(ClustererDataLoader.ClustererDoc doc, IList<Pair<int, int>> mentionPairs, ICounter<Pair<int, int>> scores)
		{
			ICounter<string> features = new ClassicCounter<string>();
			double maxScore = 0;
			double minScore = 1;
			ICounter<string> totals = new ClassicCounter<string>();
			ICounter<string> totalsLog = new ClassicCounter<string>();
			ICounter<string> counts = new ClassicCounter<string>();
			foreach (Pair<int, int> mentionPair in mentionPairs)
			{
				if (!scores.ContainsKey(mentionPair))
				{
					mentionPair = new Pair<int, int>(mentionPair.second, mentionPair.first);
				}
				double score = scores.GetCount(mentionPair);
				double logScore = CappedLog(score);
				string mt1 = doc.mentionTypes[mentionPair.first];
				string mt2 = doc.mentionTypes[mentionPair.second];
				mt1 = mt1.Equals("PRONOMINAL") ? "PRONOMINAL" : "NON_PRONOMINAL";
				mt2 = mt2.Equals("PRONOMINAL") ? "PRONOMINAL" : "NON_PRONOMINAL";
				string conj = "_" + mt1 + "_" + mt2;
				maxScore = Math.Max(maxScore, score);
				minScore = Math.Min(minScore, score);
				totals.IncrementCount(string.Empty, score);
				totalsLog.IncrementCount(string.Empty, logScore);
				counts.IncrementCount(string.Empty);
				totals.IncrementCount(conj, score);
				totalsLog.IncrementCount(conj, logScore);
				counts.IncrementCount(conj);
			}
			features.IncrementCount("max", maxScore);
			features.IncrementCount("min", minScore);
			foreach (string key in counts.KeySet())
			{
				features.IncrementCount("avg" + key, totals.GetCount(key) / mentionPairs.Count);
				features.IncrementCount("avgLog" + key, totalsLog.GetCount(key) / mentionPairs.Count);
			}
			return features;
		}

		private static int EarliestMention(Clusterer.Cluster c, ClustererDataLoader.ClustererDoc doc)
		{
			int earliest = -1;
			foreach (int m in c.mentions)
			{
				int pos = doc.mentionIndices[m];
				if (earliest == -1 || pos < doc.mentionIndices[earliest])
				{
					earliest = m;
				}
			}
			return earliest;
		}

		private static ICounter<string> GetFeatures(ClustererDataLoader.ClustererDoc doc, Clusterer.Cluster c1, Clusterer.Cluster c2, Clusterer.GlobalFeatures gf)
		{
			Clusterer.MergeKey key = new Clusterer.MergeKey(c1, c2, gf.currentIndex);
			CompressedFeatureVector cfv = featuresCache[key];
			ICounter<string> features = cfv == null ? null : compressor.Uncompress(cfv);
			if (features != null)
			{
				featuresCacheHits += isTraining;
				return features;
			}
			featuresCacheMisses += isTraining;
			features = new ClassicCounter<string>();
			if (gf.anaphorSeen)
			{
				features.IncrementCount("anaphorSeen");
			}
			features.IncrementCount("docSize", gf.docSize);
			features.IncrementCount("percentComplete", gf.currentIndex / (double)gf.size);
			features.IncrementCount("bias", 1.0);
			int earliest1 = EarliestMention(c1, doc);
			int earliest2 = EarliestMention(c2, doc);
			if (doc.mentionIndices[earliest1] > doc.mentionIndices[earliest2])
			{
				int tmp = earliest1;
				earliest1 = earliest2;
				earliest2 = tmp;
			}
			features.IncrementCount("anaphoricity", doc.anaphoricityScores.GetCount(earliest2));
			if (c1.mentions.Count == 1 && c2.mentions.Count == 1)
			{
				Pair<int, int> mentionPair = new Pair<int, int>(c1.mentions[0], c2.mentions[0]);
				features.AddAll(AddSuffix(GetFeatures(doc, mentionPair, doc.classificationScores), "-classification"));
				features.AddAll(AddSuffix(GetFeatures(doc, mentionPair, doc.rankingScores), "-ranking"));
				features = AddSuffix(features, "-single");
			}
			else
			{
				IList<Pair<int, int>> between = new List<Pair<int, int>>();
				foreach (int m1 in c1.mentions)
				{
					foreach (int m2 in c2.mentions)
					{
						between.Add(new Pair<int, int>(m1, m2));
					}
				}
				features.AddAll(AddSuffix(GetFeatures(doc, between, doc.classificationScores), "-classification"));
				features.AddAll(AddSuffix(GetFeatures(doc, between, doc.rankingScores), "-ranking"));
			}
			featuresCache[key] = compressor.Compress(features);
			return features;
		}

		private static ICounter<string> AddSuffix(ICounter<string> features, string suffix)
		{
			ICounter<string> withSuffix = new ClassicCounter<string>();
			foreach (KeyValuePair<string, double> e in features.EntrySet())
			{
				withSuffix.IncrementCount(e.Key + suffix, e.Value);
			}
			return withSuffix;
		}

		private static double CappedLog(double x)
		{
			return Math.Log(Math.Max(x, 1e-8));
		}

		private class ClustererClassifier : SimpleLinearClassifier
		{
			public ClustererClassifier(double learningRate)
				: base(SimpleLinearClassifier.Risk(), SimpleLinearClassifier.Constant(learningRate), 0)
			{
			}

			public ClustererClassifier(string modelFile, double learningRate)
				: base(SimpleLinearClassifier.Risk(), SimpleLinearClassifier.Constant(learningRate), 0, modelFile)
			{
			}

			public virtual Clusterer.CandidateAction BestAction(Pair<Clusterer.CandidateAction, Clusterer.CandidateAction> actions)
			{
				return WeightFeatureProduct(actions.first.features) > WeightFeatureProduct(actions.second.features) ? actions.first : actions.second;
			}

			public virtual void Learn(Pair<Clusterer.CandidateAction, Clusterer.CandidateAction> actions)
			{
				Clusterer.CandidateAction goodAction = actions.first;
				Clusterer.CandidateAction badAction = actions.second;
				if (badAction.cost == 0)
				{
					Clusterer.CandidateAction tmp = goodAction;
					goodAction = badAction;
					badAction = tmp;
				}
				ICounter<string> features = new ClassicCounter<string>(goodAction.features);
				foreach (KeyValuePair<string, double> e in badAction.features.EntrySet())
				{
					features.DecrementCount(e.Key, e.Value);
				}
				Learn(features, 0, badAction.cost);
			}
		}

		private class CandidateAction
		{
			public readonly ICounter<string> features;

			public readonly double cost;

			public CandidateAction(ICounter<string> features, double cost)
			{
				this.features = features;
				this.cost = cost;
			}
		}
	}
}
