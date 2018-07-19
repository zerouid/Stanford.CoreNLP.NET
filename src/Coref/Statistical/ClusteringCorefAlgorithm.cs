using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>Builds up coreference clusters incrementally with agglomerative clustering.</summary>
	/// <remarks>
	/// Builds up coreference clusters incrementally with agglomerative clustering.
	/// The model is described in
	/// Kevin Clark and Christopher D. Manning. 2015.
	/// <a href="http://nlp.stanford.edu/pubs/clark-manning-acl15-entity.pdf">
	/// Entity-Centric Coreference Resolution with Model Stacking</a>.
	/// In Association for Computational Linguistics.
	/// See
	/// <see cref="StatisticalCorefTrainer"/>
	/// for training a new model.
	/// </remarks>
	/// <author>Kevin Clark</author>
	public class ClusteringCorefAlgorithm : ICorefAlgorithm
	{
		private readonly Clusterer clusterer;

		private readonly PairwiseModel classificationModel;

		private readonly PairwiseModel rankingModel;

		private readonly PairwiseModel anaphoricityModel;

		private readonly FeatureExtractor extractor;

		public ClusteringCorefAlgorithm(Properties props, Dictionaries dictionaries)
			: this(props, dictionaries, StatisticalCorefProperties.ClusteringModelPath(props), StatisticalCorefProperties.ClassificationModelPath(props), StatisticalCorefProperties.RankingModelPath(props), StatisticalCorefProperties.AnaphoricityModelPath
				(props), StatisticalCorefProperties.WordCountsPath(props))
		{
		}

		public ClusteringCorefAlgorithm(Properties props, Dictionaries dictionaries, string clusteringPath, string classificationPath, string rankingPath, string anaphoricityPath, string wordCountsPath)
		{
			clusterer = new Clusterer(clusteringPath);
			classificationModel = PairwiseModel.NewBuilder("classification", MetaFeatureExtractor.NewBuilder().Build()).ModelPath(classificationPath).Build();
			rankingModel = PairwiseModel.NewBuilder("ranking", MetaFeatureExtractor.NewBuilder().Build()).ModelPath(rankingPath).Build();
			anaphoricityModel = PairwiseModel.NewBuilder("anaphoricity", MetaFeatureExtractor.AnaphoricityMFE()).ModelPath(anaphoricityPath).Build();
			extractor = new FeatureExtractor(props, dictionaries, null, wordCountsPath);
		}

		public virtual void RunCoref(Document document)
		{
			IDictionary<Pair<int, int>, bool> mentionPairs = CorefUtils.GetUnlabeledMentionPairs(document);
			if (mentionPairs.Count == 0)
			{
				return;
			}
			Compressor<string> compressor = new Compressor<string>();
			DocumentExamples examples = extractor.Extract(0, document, mentionPairs, compressor);
			ICounter<Pair<int, int>> classificationScores = new ClassicCounter<Pair<int, int>>();
			ICounter<Pair<int, int>> rankingScores = new ClassicCounter<Pair<int, int>>();
			ICounter<int> anaphoricityScores = new ClassicCounter<int>();
			foreach (Example example in examples.examples)
			{
				CorefUtils.CheckForInterrupt();
				Pair<int, int> mentionPair = new Pair<int, int>(example.mentionId1, example.mentionId2);
				classificationScores.IncrementCount(mentionPair, classificationModel.Predict(example, examples.mentionFeatures, compressor));
				rankingScores.IncrementCount(mentionPair, rankingModel.Predict(example, examples.mentionFeatures, compressor));
				if (!anaphoricityScores.ContainsKey(example.mentionId2))
				{
					anaphoricityScores.IncrementCount(example.mentionId2, anaphoricityModel.Predict(new Example(example, false), examples.mentionFeatures, compressor));
				}
			}
			ClustererDataLoader.ClustererDoc doc = new ClustererDataLoader.ClustererDoc(0, classificationScores, rankingScores, anaphoricityScores, mentionPairs, null, document.predictedMentionsByID.Stream().Collect(Collectors.ToMap(null, null)));
			foreach (Pair<int, int> mentionPair_1 in clusterer.GetClusterMerges(doc))
			{
				CorefUtils.MergeCoreferenceClusters(mentionPair_1, document);
			}
		}
	}
}
