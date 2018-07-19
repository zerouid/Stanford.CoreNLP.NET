using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Dcoref
{
	public class ScorerPairwise : CorefScorer
	{
		public ScorerPairwise()
			: base(CorefScorer.ScoreType.Pairwise)
		{
		}

		protected internal override void CalculateRecall(Document doc)
		{
			int rDen = 0;
			int rNum = 0;
			IDictionary<int, Mention> predictedMentions = doc.allPredictedMentions;
			foreach (CorefCluster g in doc.goldCorefClusters.Values)
			{
				int clusterSize = g.GetCorefMentions().Count;
				rDen += clusterSize * (clusterSize - 1) / 2;
				foreach (Mention m1 in g.GetCorefMentions())
				{
					Mention predictedM1 = predictedMentions[m1.mentionID];
					if (predictedM1 == null)
					{
						continue;
					}
					foreach (Mention m2 in g.GetCorefMentions())
					{
						if (m1.mentionID >= m2.mentionID)
						{
							continue;
						}
						Mention predictedM2 = predictedMentions[m2.mentionID];
						if (predictedM2 == null)
						{
							continue;
						}
						if (predictedM1.corefClusterID == predictedM2.corefClusterID)
						{
							rNum++;
						}
					}
				}
			}
			recallDenSum += rDen;
			recallNumSum += rNum;
		}

		protected internal override void CalculatePrecision(Document doc)
		{
			int pDen = 0;
			int pNum = 0;
			IDictionary<int, Mention> goldMentions = doc.allGoldMentions;
			foreach (CorefCluster c in doc.corefClusters.Values)
			{
				int clusterSize = c.GetCorefMentions().Count;
				pDen += clusterSize * (clusterSize - 1) / 2;
				foreach (Mention m1 in c.GetCorefMentions())
				{
					Mention goldM1 = goldMentions[m1.mentionID];
					if (goldM1 == null)
					{
						continue;
					}
					foreach (Mention m2 in c.GetCorefMentions())
					{
						if (m1.mentionID >= m2.mentionID)
						{
							continue;
						}
						Mention goldM2 = goldMentions[m2.mentionID];
						if (goldM2 == null)
						{
							continue;
						}
						if (goldM1.goldCorefClusterID == goldM2.goldCorefClusterID)
						{
							pNum++;
						}
					}
				}
			}
			precisionDenSum += pDen;
			precisionNumSum += pNum;
		}
	}
}
