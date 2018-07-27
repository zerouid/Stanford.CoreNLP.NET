using System;

namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>Wrapper for a coreference resolution score: MUC, B cubed, Pairwise.</summary>
	public abstract class CorefScorer
	{
		internal enum SubScoreType
		{
			Recall,
			Precision,
			F1
		}

		internal enum ScoreType
		{
			Muc,
			BCubed,
			Pairwise
		}

		internal double precisionNumSum;

		internal double precisionDenSum;

		internal double recallNumSum;

		internal double recallDenSum;

		private readonly CorefScorer.ScoreType scoreType;

		internal CorefScorer(CorefScorer.ScoreType st)
		{
			scoreType = st;
			precisionNumSum = 0.0;
			precisionDenSum = 0.0;
			recallNumSum = 0.0;
			recallDenSum = 0.0;
		}

		public virtual double GetScore(CorefScorer.SubScoreType subScoreType)
		{
			switch (subScoreType)
			{
				case CorefScorer.SubScoreType.Precision:
				{
					return GetPrecision();
				}

				case CorefScorer.SubScoreType.Recall:
				{
					return GetRecall();
				}

				case CorefScorer.SubScoreType.F1:
				{
					return GetF1();
				}

				default:
				{
					throw new ArgumentException("Unsupported subScoreType: " + subScoreType);
				}
			}
		}

		public virtual double GetPrecision()
		{
			return precisionDenSum == 0.0 ? 0.0 : precisionNumSum / precisionDenSum;
		}

		public virtual double GetRecall()
		{
			return recallDenSum == 0.0 ? 0.0 : recallNumSum / recallDenSum;
		}

		public virtual double GetF1()
		{
			double p = GetPrecision();
			double r = GetRecall();
			return (p + r == 0.0) ? 0.0 : 2.0 * p * r / (p + r);
		}

		public virtual void CalculateScore(Document doc)
		{
			CalculatePrecision(doc);
			CalculateRecall(doc);
		}

		protected internal abstract void CalculatePrecision(Document doc);

		protected internal abstract void CalculateRecall(Document doc);

		public virtual void PrintF1(Logger logger, bool printF1First)
		{
			NumberFormat nf = new DecimalFormat("0.0000");
			double r = GetRecall();
			double p = GetPrecision();
			double f1 = GetF1();
			string R = nf.Format(r);
			string P = nf.Format(p);
			string F1 = nf.Format(f1);
			NumberFormat nf2 = new DecimalFormat("00.0");
			string Rr = nf2.Format(r * 100);
			string Pp = nf2.Format(p * 100);
			string F1f1 = nf2.Format(f1 * 100);
			if (printF1First)
			{
				string str = "F1 = " + F1 + ", P = " + P + " (" + (int)precisionNumSum + "/" + (int)precisionDenSum + "), R = " + R + " (" + (int)recallNumSum + "/" + (int)recallDenSum + ")";
				if (scoreType == CorefScorer.ScoreType.Pairwise)
				{
					logger.Fine("Pairwise " + str);
				}
				else
				{
					if (scoreType == CorefScorer.ScoreType.BCubed)
					{
						logger.Fine("B cubed  " + str);
					}
					else
					{
						logger.Fine("MUC      " + str);
					}
				}
			}
			else
			{
				logger.Fine("& " + Pp + " & " + Rr + " & " + F1f1);
			}
		}

		public virtual void PrintF1(Logger logger)
		{
			PrintF1(logger, true);
		}
	}
}
