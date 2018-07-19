using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Dcoref
{
	public class ScorerMUC : CorefScorer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Dcoref.ScorerMUC));

		public ScorerMUC()
			: base(CorefScorer.ScoreType.Muc)
		{
		}

		protected internal override void CalculateRecall(Document doc)
		{
			int rDen = 0;
			int rNum = 0;
			IDictionary<int, Mention> predictedMentions = doc.allPredictedMentions;
			foreach (CorefCluster g in doc.goldCorefClusters.Values)
			{
				if (g.corefMentions.Count == 0)
				{
					SieveCoreferenceSystem.logger.Warning("NO MENTIONS for cluster " + g.GetClusterID());
					continue;
				}
				rDen += g.corefMentions.Count - 1;
				rNum += g.corefMentions.Count;
				ICollection<CorefCluster> partitions = Generics.NewHashSet();
				foreach (Mention goldMention in g.corefMentions)
				{
					if (!predictedMentions.Contains(goldMention.mentionID))
					{
						// twinless goldmention
						rNum--;
					}
					else
					{
						partitions.Add(doc.corefClusters[predictedMentions[goldMention.mentionID].corefClusterID]);
					}
				}
				rNum -= partitions.Count;
			}
			if (rDen != doc.allGoldMentions.Count - doc.goldCorefClusters.Values.Count)
			{
				log.Info("rDen is " + rDen);
				log.Info("doc.allGoldMentions.size() is " + doc.allGoldMentions.Count);
				log.Info("doc.goldCorefClusters.values().size() is " + doc.goldCorefClusters.Values.Count);
			}
			System.Diagnostics.Debug.Assert((rDen == (doc.allGoldMentions.Count - doc.goldCorefClusters.Values.Count)));
			recallNumSum += rNum;
			recallDenSum += rDen;
		}

		protected internal override void CalculatePrecision(Document doc)
		{
			int pDen = 0;
			int pNum = 0;
			IDictionary<int, Mention> goldMentions = doc.allGoldMentions;
			foreach (CorefCluster c in doc.corefClusters.Values)
			{
				if (c.corefMentions.Count == 0)
				{
					continue;
				}
				pDen += c.corefMentions.Count - 1;
				pNum += c.corefMentions.Count;
				ICollection<CorefCluster> partitions = Generics.NewHashSet();
				foreach (Mention predictedMention in c.corefMentions)
				{
					if (!goldMentions.Contains(predictedMention.mentionID))
					{
						// twinless goldmention
						pNum--;
					}
					else
					{
						partitions.Add(doc.goldCorefClusters[goldMentions[predictedMention.mentionID].goldCorefClusterID]);
					}
				}
				pNum -= partitions.Count;
			}
			System.Diagnostics.Debug.Assert((pDen == (doc.allPredictedMentions.Count - doc.corefClusters.Values.Count)));
			precisionDenSum += pDen;
			precisionNumSum += pNum;
		}
	}
}
