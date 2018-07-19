using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>B^3 scorer</summary>
	/// <author>heeyoung</author>
	public class ScorerBCubed : CorefScorer
	{
		protected internal enum BCubedType
		{
			B0,
			Ball,
			Brahman,
			Bcai,
			Bconll
		}

		private readonly ScorerBCubed.BCubedType type;

		public ScorerBCubed(ScorerBCubed.BCubedType _type)
			: base(CorefScorer.ScoreType.BCubed)
		{
			type = _type;
		}

		protected internal override void CalculatePrecision(Document doc)
		{
			switch (type)
			{
				case ScorerBCubed.BCubedType.Bcai:
				{
					CalculatePrecisionBcai(doc);
					break;
				}

				case ScorerBCubed.BCubedType.Ball:
				{
					CalculatePrecisionBall(doc);
					break;
				}

				case ScorerBCubed.BCubedType.Bconll:
				{
					CalculatePrecisionBconll(doc);
					break;
				}
			}
		}

		// same as Bcai
		protected internal override void CalculateRecall(Document doc)
		{
			switch (type)
			{
				case ScorerBCubed.BCubedType.Bcai:
				{
					CalculateRecallBcai(doc);
					break;
				}

				case ScorerBCubed.BCubedType.Ball:
				{
					CalculateRecallBall(doc);
					break;
				}

				case ScorerBCubed.BCubedType.Bconll:
				{
					CalculateRecallBconll(doc);
					break;
				}
			}
		}

		private void CalculatePrecisionBall(Document doc)
		{
			int pDen = 0;
			double pNum = 0.0;
			IDictionary<int, Mention> goldMentions = doc.allGoldMentions;
			IDictionary<int, Mention> predictedMentions = doc.allPredictedMentions;
			foreach (Mention m in predictedMentions.Values)
			{
				double correct = 0.0;
				double total = 0.0;
				foreach (Mention m2 in doc.corefClusters[m.corefClusterID].GetCorefMentions())
				{
					if (m == m2 || (goldMentions.Contains(m.mentionID) && goldMentions.Contains(m2.mentionID) && goldMentions[m.mentionID].goldCorefClusterID == goldMentions[m2.mentionID].goldCorefClusterID))
					{
						correct++;
					}
					total++;
				}
				pNum += correct / total;
				pDen++;
			}
			precisionDenSum += pDen;
			precisionNumSum += pNum;
		}

		private void CalculateRecallBall(Document doc)
		{
			int rDen = 0;
			double rNum = 0.0;
			IDictionary<int, Mention> goldMentions = doc.allGoldMentions;
			IDictionary<int, Mention> predictedMentions = doc.allPredictedMentions;
			foreach (Mention m in goldMentions.Values)
			{
				double correct = 0.0;
				double total = 0.0;
				foreach (Mention m2 in doc.goldCorefClusters[m.goldCorefClusterID].GetCorefMentions())
				{
					if (m == m2 || (predictedMentions.Contains(m.mentionID) && predictedMentions.Contains(m2.mentionID) && predictedMentions[m.mentionID].corefClusterID == predictedMentions[m2.mentionID].corefClusterID))
					{
						correct++;
					}
					total++;
				}
				rNum += correct / total;
				rDen++;
			}
			recallDenSum += rDen;
			recallNumSum += rNum;
		}

		private void CalculatePrecisionBcai(Document doc)
		{
			int pDen = 0;
			double pNum = 0.0;
			IDictionary<int, Mention> goldMentions = doc.allGoldMentions;
			IDictionary<int, Mention> predictedMentions = doc.allPredictedMentions;
			foreach (Mention m in predictedMentions.Values)
			{
				if (!goldMentions.Contains(m.mentionID) && doc.corefClusters[m.corefClusterID].GetCorefMentions().Count == 1)
				{
					continue;
				}
				double correct = 0.0;
				double total = 0.0;
				foreach (Mention m2 in doc.corefClusters[m.corefClusterID].GetCorefMentions())
				{
					if (m == m2 || (goldMentions.Contains(m.mentionID) && goldMentions.Contains(m2.mentionID) && goldMentions[m.mentionID].goldCorefClusterID == goldMentions[m2.mentionID].goldCorefClusterID))
					{
						correct++;
					}
					total++;
				}
				pNum += correct / total;
				pDen++;
			}
			foreach (int id in goldMentions.Keys)
			{
				if (!predictedMentions.Contains(id))
				{
					pNum++;
					pDen++;
				}
			}
			precisionDenSum += pDen;
			precisionNumSum += pNum;
		}

		private void CalculateRecallBcai(Document doc)
		{
			int rDen = 0;
			double rNum = 0.0;
			IDictionary<int, Mention> goldMentions = doc.allGoldMentions;
			IDictionary<int, Mention> predictedMentions = doc.allPredictedMentions;
			foreach (Mention m in goldMentions.Values)
			{
				double correct = 0.0;
				double total = 0.0;
				foreach (Mention m2 in doc.goldCorefClusters[m.goldCorefClusterID].GetCorefMentions())
				{
					if (m == m2 || (predictedMentions.Contains(m.mentionID) && predictedMentions.Contains(m2.mentionID) && predictedMentions[m.mentionID].corefClusterID == predictedMentions[m2.mentionID].corefClusterID))
					{
						correct++;
					}
					total++;
				}
				rNum += correct / total;
				rDen++;
			}
			recallDenSum += rDen;
			recallNumSum += rNum;
		}

		private void CalculatePrecisionBconll(Document doc)
		{
			// same as Bcai
			CalculatePrecisionBcai(doc);
		}

		private void CalculateRecallBconll(Document doc)
		{
			int rDen = 0;
			double rNum = 0.0;
			IDictionary<int, Mention> goldMentions = doc.allGoldMentions;
			IDictionary<int, Mention> predictedMentions = doc.allPredictedMentions;
			foreach (Mention m in goldMentions.Values)
			{
				double correct = 0.0;
				double total = 0.0;
				foreach (Mention m2 in doc.goldCorefClusters[m.goldCorefClusterID].GetCorefMentions())
				{
					if (m == m2 || (predictedMentions.Contains(m.mentionID) && predictedMentions.Contains(m2.mentionID) && predictedMentions[m.mentionID].corefClusterID == predictedMentions[m2.mentionID].corefClusterID))
					{
						correct++;
					}
					total++;
				}
				rNum += correct / total;
				rDen++;
			}
			// this part is different from Bcai
			foreach (Mention m_1 in predictedMentions.Values)
			{
				if (!goldMentions.Contains(m_1.mentionID) && doc.corefClusters[m_1.corefClusterID].GetCorefMentions().Count != 1)
				{
					rNum++;
					rDen++;
				}
			}
			recallDenSum += rDen;
			recallNumSum += rNum;
		}
	}
}
