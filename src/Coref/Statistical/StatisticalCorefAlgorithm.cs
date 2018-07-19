using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>
	/// Does best-first coreference resolution by linking each mention to its highest scoring candidate
	/// antecedent if that score is above a threshold.
	/// </summary>
	/// <remarks>
	/// Does best-first coreference resolution by linking each mention to its highest scoring candidate
	/// antecedent if that score is above a threshold. The model is described in
	/// Kevin Clark and Christopher D. Manning. 2015.
	/// <a href="http://nlp.stanford.edu/pubs/clark-manning-acl15-entity.pdf">
	/// Entity-Centric Coreference Resolution with Model Stacking</a>.
	/// In Association for Computational Linguistics.
	/// See
	/// <see cref="StatisticalCorefTrainer"/>
	/// for training a new model.
	/// </remarks>
	/// <author>Kevin Clark</author>
	public class StatisticalCorefAlgorithm : ICorefAlgorithm
	{
		private readonly IDictionary<Pair<bool, bool>, double> thresholds;

		private readonly FeatureExtractor extractor;

		private readonly PairwiseModel classifier;

		private readonly int maxMentionDistance;

		private readonly int maxMentionDistanceWithStringMatch;

		public StatisticalCorefAlgorithm(Properties props, Dictionaries dictionaries)
			: this(props, dictionaries, StatisticalCorefProperties.WordCountsPath(props), StatisticalCorefProperties.RankingModelPath(props), CorefProperties.MaxMentionDistance(props), CorefProperties.MaxMentionDistanceWithStringMatch(props), StatisticalCorefProperties
				.PairwiseScoreThresholds(props))
		{
		}

		public StatisticalCorefAlgorithm(Properties props, Dictionaries dictionaries, string wordCountsFile, string modelFile, int maxMentionDistance, int maxMentionDistanceWithStringMatch, double threshold)
			: this(props, dictionaries, wordCountsFile, modelFile, maxMentionDistance, maxMentionDistanceWithStringMatch, new double[] { threshold, threshold, threshold, threshold })
		{
		}

		public StatisticalCorefAlgorithm(Properties props, Dictionaries dictionaries, string wordCountsFile, string modelPath, int maxMentionDistance, int maxMentionDistanceWithStringMatch, double[] thresholds)
		{
			extractor = new FeatureExtractor(props, dictionaries, null, wordCountsFile);
			classifier = PairwiseModel.NewBuilder("classifier", MetaFeatureExtractor.NewBuilder().Build()).ModelPath(modelPath).Build();
			this.maxMentionDistance = maxMentionDistance;
			this.maxMentionDistanceWithStringMatch = maxMentionDistanceWithStringMatch;
			this.thresholds = MakeThresholds(thresholds);
		}

		private static IDictionary<Pair<bool, bool>, double> MakeThresholds(double[] thresholds)
		{
			IDictionary<Pair<bool, bool>, double> thresholdsMap = new Dictionary<Pair<bool, bool>, double>();
			thresholdsMap[new Pair<bool, bool>(true, true)] = thresholds[0];
			thresholdsMap[new Pair<bool, bool>(true, false)] = thresholds[1];
			thresholdsMap[new Pair<bool, bool>(false, true)] = thresholds[2];
			thresholdsMap[new Pair<bool, bool>(false, false)] = thresholds[3];
			return thresholdsMap;
		}

		public virtual void RunCoref(Document document)
		{
			Compressor<string> compressor = new Compressor<string>();
			if (Thread.Interrupted())
			{
				// Allow interrupting
				throw new RuntimeInterruptedException();
			}
			IDictionary<Pair<int, int>, bool> pairs = new Dictionary<Pair<int, int>, bool>();
			foreach (KeyValuePair<int, IList<int>> e in CorefUtils.HeuristicFilter(CorefUtils.GetSortedMentions(document), maxMentionDistance, maxMentionDistanceWithStringMatch))
			{
				foreach (int m1 in e.Value)
				{
					pairs[new Pair<int, int>(m1, e.Key)] = true;
				}
			}
			DocumentExamples examples = extractor.Extract(0, document, pairs, compressor);
			ICounter<Pair<int, int>> pairwiseScores = new ClassicCounter<Pair<int, int>>();
			foreach (Example mentionPair in examples.examples)
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting
					throw new RuntimeInterruptedException();
				}
				pairwiseScores.IncrementCount(new Pair<int, int>(mentionPair.mentionId1, mentionPair.mentionId2), classifier.Predict(mentionPair, examples.mentionFeatures, compressor));
			}
			IList<Pair<int, int>> mentionPairs = new List<Pair<int, int>>(pairwiseScores.KeySet());
			mentionPairs.Sort(null);
			ICollection<int> seenAnaphors = new HashSet<int>();
			foreach (Pair<int, int> pair in mentionPairs)
			{
				if (seenAnaphors.Contains(pair.second))
				{
					continue;
				}
				if (Thread.Interrupted())
				{
					// Allow interrupting
					throw new RuntimeInterruptedException();
				}
				seenAnaphors.Add(pair.second);
				Dictionaries.MentionType mt1 = document.predictedMentionsByID[pair.first].mentionType;
				Dictionaries.MentionType mt2 = document.predictedMentionsByID[pair.second].mentionType;
				if (pairwiseScores.GetCount(pair) > thresholds[new Pair<bool, bool>(mt1 == Dictionaries.MentionType.Pronominal, mt2 == Dictionaries.MentionType.Pronominal)])
				{
					CorefUtils.MergeCoreferenceClusters(pair, document);
				}
			}
		}
	}
}
