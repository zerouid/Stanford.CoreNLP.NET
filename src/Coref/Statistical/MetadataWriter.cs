using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>Writes various pieces of information about coreference documents to disk.</summary>
	/// <author>Kevin Clark</author>
	public class MetadataWriter : ICorefDocumentProcessor
	{
		private readonly IDictionary<int, IDictionary<int, string>> mentionTypes;

		private readonly IDictionary<int, IList<IList<int>>> goldClusters;

		private readonly ICounter<string> wordCounts;

		private readonly IDictionary<int, IDictionary<Pair<int, int>, bool>> mentionPairs;

		private readonly bool countWords;

		public MetadataWriter(bool countWords)
		{
			this.countWords = countWords;
			mentionTypes = new Dictionary<int, IDictionary<int, string>>();
			goldClusters = new Dictionary<int, IList<IList<int>>>();
			wordCounts = new ClassicCounter<string>();
			try
			{
				mentionPairs = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.datasetFile);
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public virtual void Process(int id, Document document)
		{
			// Mention types
			mentionTypes[id] = document.predictedMentionsByID.Stream().Collect(Collectors.ToMap(null, null));
			// Gold clusters
			IList<IList<int>> clusters = new List<IList<int>>();
			foreach (CorefCluster c in document.goldCorefClusters.Values)
			{
				IList<int> cluster = new List<int>();
				foreach (Mention m in c.GetCorefMentions())
				{
					cluster.Add(m.mentionID);
				}
				clusters.Add(cluster);
			}
			goldClusters[id] = clusters;
			// Word counting
			if (countWords && mentionPairs.Contains(id))
			{
				ICollection<Pair<int, int>> pairs = mentionPairs[id].Keys;
				ICollection<int> mentions = new HashSet<int>();
				foreach (Pair<int, int> pair in pairs)
				{
					mentions.Add(pair.first);
					mentions.Add(pair.second);
					Mention m1 = document.predictedMentionsByID[pair.first];
					Mention m2 = document.predictedMentionsByID[pair.second];
					wordCounts.IncrementCount("h_" + m1.headWord.Word().ToLower() + "_" + m2.headWord.Word().ToLower());
				}
				IDictionary<int, IList<CoreLabel>> sentences = new Dictionary<int, IList<CoreLabel>>();
				foreach (int mention in mentions)
				{
					Mention m = document.predictedMentionsByID[mention];
					if (!sentences.Contains(m.sentNum))
					{
						sentences[m.sentNum] = m.sentenceWords;
					}
				}
				foreach (IList<CoreLabel> sentence in sentences.Values)
				{
					for (int i = 0; i < sentence.Count; i++)
					{
						CoreLabel cl = sentence[i];
						if (cl == null)
						{
							continue;
						}
						string w = cl.Word().ToLower();
						wordCounts.IncrementCount(w);
						if (i > 0)
						{
							CoreLabel clp = sentence[i - 1];
							if (clp == null)
							{
								continue;
							}
							string wp = clp.Word().ToLower();
							wordCounts.IncrementCount(wp + "_" + w);
						}
					}
				}
			}
		}

		/// <exception cref="System.Exception"/>
		public virtual void Finish()
		{
			IOUtils.WriteObjectToFile(mentionTypes, StatisticalCorefTrainer.mentionTypesFile);
			IOUtils.WriteObjectToFile(goldClusters, StatisticalCorefTrainer.goldClustersFile);
			if (countWords)
			{
				IOUtils.WriteObjectToFile(wordCounts, StatisticalCorefTrainer.wordCountsFile);
			}
		}
	}
}
