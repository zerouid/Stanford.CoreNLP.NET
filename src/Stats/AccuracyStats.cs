using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
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
	/// <author>Kristina Toutanova</author>
	/// <author>Jenny Finkel</author>
	public class AccuracyStats<L> : IScorer<L>
	{
		internal double confWeightedAccuracy;

		internal double accuracy;

		internal double optAccuracy;

		internal double optConfWeightedAccuracy;

		internal double logLikelihood;

		internal int[] accrecall;

		internal int[] optaccrecall;

		internal L posLabel;

		internal string saveFile;

		internal static int saveIndex = 1;

		public AccuracyStats(IProbabilisticClassifier<L, F> classifier, GeneralDataset<L, F> data, L posLabel)
		{
			// = null;
			this.posLabel = posLabel;
			Score(classifier, data);
		}

		public AccuracyStats(L posLabel, string saveFile)
		{
			this.posLabel = posLabel;
			this.saveFile = saveFile;
		}

		public virtual double Score<F>(IProbabilisticClassifier<L, F> classifier, GeneralDataset<L, F> data)
		{
			List<Pair<double, int>> dataScores = new List<Pair<double, int>>();
			for (int i = 0; i < data.Size(); i++)
			{
				IDatum<L, F> d = data.GetRVFDatum(i);
				ICounter<L> scores = classifier.LogProbabilityOf(d);
				int labelD = d.Label().Equals(posLabel) ? 1 : 0;
				dataScores.Add(new Pair<double, int>(Math.Exp(scores.GetCount(posLabel)), labelD));
			}
			PRCurve prc = new PRCurve(dataScores);
			confWeightedAccuracy = prc.Cwa();
			accuracy = prc.Accuracy();
			optAccuracy = prc.OptimalAccuracy();
			optConfWeightedAccuracy = prc.OptimalCwa();
			logLikelihood = prc.LogLikelihood();
			accrecall = prc.CwaArray();
			optaccrecall = prc.OptimalCwaArray();
			return accuracy;
		}

		public virtual string GetDescription(int numDigits)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			StringBuilder sb = new StringBuilder();
			sb.Append("--- Accuracy Stats ---").Append('\n');
			sb.Append("accuracy: ").Append(nf.Format(accuracy)).Append('\n');
			sb.Append("optimal fn accuracy: ").Append(nf.Format(optAccuracy)).Append('\n');
			sb.Append("confidence weighted accuracy :").Append(nf.Format(confWeightedAccuracy)).Append('\n');
			sb.Append("optimal confidence weighted accuracy: ").Append(nf.Format(optConfWeightedAccuracy)).Append('\n');
			sb.Append("log-likelihood: ").Append(logLikelihood).Append('\n');
			if (saveFile != null)
			{
				string f = saveFile + '-' + saveIndex;
				sb.Append("saving accuracy info to ").Append(f).Append(".accuracy\n");
				StringUtils.PrintToFile(f + ".accuracy", ToStringArr(accrecall));
				sb.Append("saving optimal accuracy info to ").Append(f).Append(".optimal_accuracy\n");
				StringUtils.PrintToFile(f + ".optimal_accuracy", ToStringArr(optaccrecall));
				saveIndex++;
			}
			//sb.append("accuracy coverage: ").append(toStringArr(accrecall)).append("\n");
			//sb.append("optimal accuracy coverage: ").append(toStringArr(optaccrecall));
			return sb.ToString();
		}

		public static string ToStringArr(int[] acc)
		{
			StringBuilder sb = new StringBuilder();
			int total = acc.Length;
			for (int i = 0; i < acc.Length; i++)
			{
				double coverage = (i + 1) / (double)total;
				double accuracy = acc[i] / (double)(i + 1);
				coverage *= 1000000;
				accuracy *= 1000000;
				sb.Append(((int)coverage) / 10000);
				sb.Append('\t');
				sb.Append(((int)accuracy) / 10000);
				sb.Append('\n');
			}
			return sb.ToString();
		}
	}
}
