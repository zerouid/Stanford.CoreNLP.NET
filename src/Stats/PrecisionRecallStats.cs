using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Ling;
using Java.Text;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// Utility class for aggregating counts of true positives, false positives, and
	/// false negatives and computing precision/recall/F1 stats.
	/// </summary>
	/// <remarks>
	/// Utility class for aggregating counts of true positives, false positives, and
	/// false negatives and computing precision/recall/F1 stats. Can be used for a single
	/// collection of stats, or to aggregate stats from a bunch of runs.
	/// </remarks>
	/// <author>Joseph Smarr</author>
	public class PrecisionRecallStats
	{
		/// <summary>Count of true positives.</summary>
		protected internal int tpCount = 0;

		/// <summary>Count of false positives.</summary>
		protected internal int fpCount = 0;

		/// <summary>Count of false negatives.</summary>
		protected internal int fnCount = 0;

		/// <summary>Constructs a new PrecisionRecallStats with initially 0 counts.</summary>
		public PrecisionRecallStats()
			: this(0, 0, 0)
		{
		}

		public PrecisionRecallStats(IClassifier<L, F> classifier, Dataset<L, F> data, L positiveClass)
		{
			for (int i = 0; i < data.Size(); ++i)
			{
				IDatum<L, F> d = data.GetDatum(i);
				L guess = classifier.ClassOf(d);
				L label = d.Label();
				bool guessPositive = guess.Equals(positiveClass);
				bool isPositive = label.Equals(positiveClass);
				if (isPositive && guessPositive)
				{
					tpCount++;
				}
				if (isPositive && !guessPositive)
				{
					fnCount++;
				}
				if (!isPositive && guessPositive)
				{
					fpCount++;
				}
			}
		}

		/// <summary>Constructs a new PrecisionRecallStats with the given initial counts.</summary>
		public PrecisionRecallStats(int tp, int fp, int fn)
		{
			tpCount = tp;
			fpCount = fp;
			fnCount = fn;
		}

		/// <summary>Returns the current count of true positives.</summary>
		public virtual int GetTP()
		{
			return tpCount;
		}

		/// <summary>Returns the current count of false positives.</summary>
		public virtual int GetFP()
		{
			return fpCount;
		}

		/// <summary>Returns the current count of false negatives.</summary>
		public virtual int GetFN()
		{
			return fnCount;
		}

		/// <summary>Adds the given number to the count of true positives.</summary>
		public virtual void AddTP(int count)
		{
			tpCount += count;
		}

		/// <summary>Adds one to the count of true positives.</summary>
		public virtual void IncrementTP()
		{
			AddTP(1);
		}

		/// <summary>Adds the given number to the count of false positives.</summary>
		public virtual void AddFP(int count)
		{
			fpCount += count;
		}

		/// <summary>Adds one to the count of false positives.</summary>
		public virtual void IncrementFP()
		{
			AddFP(1);
		}

		/// <summary>Adds the given number to the count of false negatives.</summary>
		public virtual void AddFN(int count)
		{
			fnCount += count;
		}

		/// <summary>Adds one to the count of false negatives.</summary>
		public virtual void IncrementFN()
		{
			AddFN(1);
		}

		/// <summary>Adds the counts from the given stats to the counts of this stats.</summary>
		public virtual void AddCounts(Edu.Stanford.Nlp.Stats.PrecisionRecallStats prs)
		{
			AddTP(prs.GetTP());
			AddFP(prs.GetFP());
			AddFN(prs.GetFN());
		}

		/// <summary>Returns the current precision: <tt>tp/(tp+fp)</tt>.</summary>
		/// <remarks>
		/// Returns the current precision: <tt>tp/(tp+fp)</tt>.
		/// Returns 1.0 if tp and fp are both 0.
		/// </remarks>
		public virtual double GetPrecision()
		{
			if (tpCount == 0 && fpCount == 0)
			{
				return 1.0;
			}
			return ((double)tpCount) / (tpCount + fpCount);
		}

		/// <summary>Returns a String summarizing precision that will print nicely.</summary>
		public virtual string GetPrecisionDescription(int numDigits)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			return nf.Format(GetPrecision()) + "  (" + tpCount + "/" + (tpCount + fpCount) + ")";
		}

		/// <summary>Returns the current recall: <tt>tp/(tp+fn)</tt>.</summary>
		/// <remarks>
		/// Returns the current recall: <tt>tp/(tp+fn)</tt>.
		/// Returns 1.0 if tp and fn are both 0.
		/// </remarks>
		public virtual double GetRecall()
		{
			if (tpCount == 0 && fnCount == 0)
			{
				return 1.0;
			}
			return ((double)tpCount) / (tpCount + fnCount);
		}

		/// <summary>Returns a String summarizing recall that will print nicely.</summary>
		public virtual string GetRecallDescription(int numDigits)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			return nf.Format(GetRecall()) + "  (" + tpCount + "/" + (tpCount + fnCount) + ")";
		}

		/// <summary>Returns the current F1 measure (<tt>alpha=0.5</tt>).</summary>
		public virtual double GetFMeasure()
		{
			return GetFMeasure(0.5);
		}

		/// <summary>Returns the F-Measure with the given mixing parameter (must be between 0 and 1).</summary>
		/// <remarks>
		/// Returns the F-Measure with the given mixing parameter (must be between 0 and 1).
		/// If either precision or recall are 0, return 0.0.
		/// <tt>F(alpha) = 1/(alpha/precision + (1-alpha)/recall)</tt>
		/// </remarks>
		public virtual double GetFMeasure(double alpha)
		{
			double pr = GetPrecision();
			double re = GetRecall();
			if (pr == 0 || re == 0)
			{
				return 0.0;
			}
			return 1.0 / ((alpha / pr) + (1.0 - alpha) / re);
		}

		/// <summary>Returns a String summarizing F1 that will print nicely.</summary>
		public virtual string GetF1Description(int numDigits)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			return nf.Format(GetFMeasure());
		}

		/// <summary>Returns a String representation of this PrecisionRecallStats, indicating the number of tp, fp, fn counts.</summary>
		public override string ToString()
		{
			return "PrecisionRecallStats[tp=" + GetTP() + ",fp=" + GetFP() + ",fn=" + GetFN() + "]";
		}

		public virtual string ToString(int numDigits)
		{
			return "PrecisionRecallStats[tp=" + GetTP() + ",fp=" + GetFP() + ",fn=" + GetFN() + ",p=" + GetPrecisionDescription(numDigits) + ",r=" + GetRecallDescription(numDigits) + ",f1=" + GetF1Description(numDigits) + "]";
		}
	}
}
