using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Coref
{
	/// <summary>Useful utilities for coreference resolution.</summary>
	/// <author>Kevin Clark</author>
	public class CorefUtils
	{
		public static IList<Mention> GetSortedMentions(Document document)
		{
			IList<Mention> mentions = new List<Mention>(document.predictedMentionsByID.Values);
			mentions.Sort(null);
			return mentions;
		}

		public static IList<Pair<int, int>> GetMentionPairs(Document document)
		{
			IList<Pair<int, int>> pairs = new List<Pair<int, int>>();
			IList<Mention> mentions = GetSortedMentions(document);
			for (int i = 0; i < mentions.Count; i++)
			{
				for (int j = 0; j < i; j++)
				{
					pairs.Add(new Pair<int, int>(mentions[j].mentionID, mentions[i].mentionID));
				}
			}
			return pairs;
		}

		public static IDictionary<Pair<int, int>, bool> GetUnlabeledMentionPairs(Document document)
		{
			return CorefUtils.GetMentionPairs(document).Stream().Collect(Collectors.ToMap(null, null));
		}

		public static IDictionary<Pair<int, int>, bool> GetLabeledMentionPairs(Document document)
		{
			IDictionary<Pair<int, int>, bool> mentionPairs = GetUnlabeledMentionPairs(document);
			foreach (CorefCluster c in document.goldCorefClusters.Values)
			{
				IList<Mention> clusterMentions = new List<Mention>(c.GetCorefMentions());
				foreach (Mention clusterMention in clusterMentions)
				{
					foreach (Mention clusterMention2 in clusterMentions)
					{
						Pair<int, int> mentionPair = new Pair<int, int>(clusterMention.mentionID, clusterMention2.mentionID);
						if (mentionPairs.Contains(mentionPair))
						{
							mentionPairs[mentionPair] = true;
						}
					}
				}
			}
			return mentionPairs;
		}

		public static void MergeCoreferenceClusters(Pair<int, int> mentionPair, Document document)
		{
			Mention m1 = document.predictedMentionsByID[mentionPair.first];
			Mention m2 = document.predictedMentionsByID[mentionPair.second];
			if (m1.corefClusterID == m2.corefClusterID)
			{
				return;
			}
			int removeId = m1.corefClusterID;
			CorefCluster c1 = document.corefClusters[m1.corefClusterID];
			CorefCluster c2 = document.corefClusters[m2.corefClusterID];
			CorefCluster.MergeClusters(c2, c1);
			Sharpen.Collections.Remove(document.corefClusters, removeId);
		}

		public static void RemoveSingletonClusters(Document document)
		{
			foreach (CorefCluster c in new List<CorefCluster>(document.corefClusters.Values))
			{
				if (c.GetCorefMentions().Count == 1)
				{
					Sharpen.Collections.Remove(document.corefClusters, c.clusterID);
				}
			}
		}

		public static void CheckForInterrupt()
		{
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
		}

		public static IDictionary<int, IList<int>> HeuristicFilter(IList<Mention> sortedMentions, int maxMentionDistance, int maxMentionDistanceWithStringMatch)
		{
			IDictionary<string, IList<Mention>> wordToMentions = new Dictionary<string, IList<Mention>>();
			for (int i = 0; i < sortedMentions.Count; i++)
			{
				Mention m = sortedMentions[i];
				foreach (string word in GetContentWords(m))
				{
					wordToMentions.PutIfAbsent(word, new List<Mention>());
					wordToMentions[word].Add(m);
				}
			}
			IDictionary<int, IList<int>> mentionToCandidateAntecedents = new Dictionary<int, IList<int>>();
			for (int i_1 = 0; i_1 < sortedMentions.Count; i_1++)
			{
				Mention m = sortedMentions[i_1];
				IList<int> candidateAntecedents = new List<int>();
				for (int j = Math.Max(0, i_1 - maxMentionDistance); j < i_1; j++)
				{
					candidateAntecedents.Add(sortedMentions[j].mentionID);
				}
				foreach (string word in GetContentWords(m))
				{
					IList<Mention> withStringMatch = wordToMentions[word];
					if (withStringMatch != null)
					{
						foreach (Mention match in withStringMatch)
						{
							if (match.mentionNum < m.mentionNum && match.mentionNum >= m.mentionNum - maxMentionDistanceWithStringMatch)
							{
								if (!candidateAntecedents.Contains(match.mentionID))
								{
									candidateAntecedents.Add(match.mentionID);
								}
							}
						}
					}
				}
				if (!candidateAntecedents.IsEmpty())
				{
					mentionToCandidateAntecedents[m.mentionID] = candidateAntecedents;
				}
			}
			return mentionToCandidateAntecedents;
		}

		private static IList<string> GetContentWords(Mention m)
		{
			IList<string> words = new List<string>();
			for (int i = m.startIndex; i < m.endIndex; i++)
			{
				CoreLabel cl = m.sentenceWords[i];
				string Pos = cl.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
				if (Pos.Equals("NN") || Pos.Equals("NNS") || Pos.Equals("NNP") || Pos.Equals("NNPS"))
				{
					words.Add(cl.Word().ToLower());
				}
			}
			return words;
		}
	}
}
