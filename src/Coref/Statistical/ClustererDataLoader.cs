using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>
	/// Loads the data used to train
	/// <see cref="Clusterer"/>
	/// .
	/// </summary>
	/// <author>Kevin Clark</author>
	public class ClustererDataLoader
	{
		public class ClustererDoc
		{
			public readonly int id;

			public readonly ICounter<Pair<int, int>> classificationScores;

			public readonly ICounter<Pair<int, int>> rankingScores;

			public readonly ICounter<int> anaphoricityScores;

			public readonly IList<IList<int>> goldClusters;

			public readonly IDictionary<int, IList<int>> mentionToGold;

			public readonly IList<int> mentions;

			public readonly IDictionary<int, string> mentionTypes;

			public readonly ICollection<Pair<int, int>> positivePairs;

			public readonly IDictionary<int, int> mentionIndices;

			public ClustererDoc(int id, ICounter<Pair<int, int>> classificationScores, ICounter<Pair<int, int>> rankingScores, ICounter<int> anaphoricityScores, IDictionary<Pair<int, int>, bool> labeledPairs, IList<IList<int>> goldClusters, IDictionary<
				int, string> mentionTypes)
			{
				this.id = id;
				this.classificationScores = classificationScores;
				this.rankingScores = rankingScores;
				this.goldClusters = goldClusters;
				this.mentionTypes = mentionTypes;
				this.anaphoricityScores = anaphoricityScores;
				positivePairs = labeledPairs.Keys.Stream().Filter(null).Collect(Collectors.ToSet());
				ICollection<int> mentionsSet = new HashSet<int>();
				foreach (Pair<int, int> pair in labeledPairs.Keys)
				{
					mentionsSet.Add(pair.first);
					mentionsSet.Add(pair.second);
				}
				mentions = new List<int>(mentionsSet);
				mentions.Sort(null);
				mentionIndices = new Dictionary<int, int>();
				for (int i = 0; i < mentions.Count; i++)
				{
					mentionIndices[mentions[i]] = i;
				}
				mentionToGold = new Dictionary<int, IList<int>>();
				if (goldClusters != null)
				{
					foreach (IList<int> gold in goldClusters)
					{
						foreach (int m in gold)
						{
							mentionToGold[m] = gold;
						}
					}
				}
			}
		}

		/// <exception cref="System.Exception"/>
		public static IList<ClustererDataLoader.ClustererDoc> LoadDocuments(int maxDocs)
		{
			IDictionary<int, IDictionary<Pair<int, int>, bool>> labeledPairs = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.datasetFile);
			IDictionary<int, IDictionary<int, string>> mentionTypes = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.mentionTypesFile);
			IDictionary<int, IList<IList<int>>> goldClusters = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.goldClustersFile);
			IDictionary<int, ICounter<Pair<int, int>>> classificationScores = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.pairwiseModelsPath + StatisticalCorefTrainer.ClassificationModel + "/" + StatisticalCorefTrainer.predictionsName + ".ser");
			IDictionary<int, ICounter<Pair<int, int>>> rankingScores = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.pairwiseModelsPath + StatisticalCorefTrainer.RankingModel + "/" + StatisticalCorefTrainer.predictionsName + ".ser");
			IDictionary<int, ICounter<Pair<int, int>>> anaphoricityScoresLoaded = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.pairwiseModelsPath + StatisticalCorefTrainer.AnaphoricityModel + "/" + StatisticalCorefTrainer.predictionsName + ".ser");
			IDictionary<int, ICounter<int>> anaphoricityScores = new Dictionary<int, ICounter<int>>();
			foreach (KeyValuePair<int, ICounter<Pair<int, int>>> e in anaphoricityScoresLoaded)
			{
				ICounter<int> scores = new ClassicCounter<int>();
				e.Value.EntrySet().ForEach(null);
				anaphoricityScores[e.Key] = scores;
			}
			return labeledPairs.Keys.Stream().Sorted().Limit(maxDocs).Map(null).Collect(Collectors.ToList());
		}
	}
}
