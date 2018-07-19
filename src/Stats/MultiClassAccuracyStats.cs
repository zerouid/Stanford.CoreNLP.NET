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
	/// <author>Jenny Finkel</author>
	public class MultiClassAccuracyStats<L> : IScorer<L>
	{
		internal double[] scores;

		internal bool[] isCorrect;

		internal double logLikelihood;

		internal double accuracy;

		internal static string saveFile = null;

		internal static int saveIndex = 1;

		public const int UseAccuracy = 1;

		public const int UseLoglikelihood = 2;

		private int scoreType = UseAccuracy;

		public MultiClassAccuracyStats()
		{
		}

		public MultiClassAccuracyStats(int scoreType)
		{
			//sorted scores
			// is the i-th example correct
			this.scoreType = scoreType;
		}

		public MultiClassAccuracyStats(string file)
			: this(file, UseAccuracy)
		{
		}

		public MultiClassAccuracyStats(string file, int scoreType)
		{
			saveFile = file;
			this.scoreType = scoreType;
		}

		public MultiClassAccuracyStats(IProbabilisticClassifier<L, F> classifier, GeneralDataset<L, F> data, string file)
			: this(classifier, data, file, UseAccuracy)
		{
		}

		public MultiClassAccuracyStats(IProbabilisticClassifier<L, F> classifier, GeneralDataset<L, F> data, string file, int scoreType)
		{
			saveFile = file;
			this.scoreType = scoreType;
			InitMC(classifier, data);
		}

		internal int correct = 0;

		internal int total = 0;

		public virtual double Score<F>(IProbabilisticClassifier<L, F> classifier, GeneralDataset<L, F> data)
		{
			InitMC(classifier, data);
			return Score();
		}

		public virtual double Score()
		{
			if (scoreType == UseAccuracy)
			{
				return accuracy;
			}
			else
			{
				if (scoreType == UseLoglikelihood)
				{
					return logLikelihood;
				}
				else
				{
					throw new Exception("Unknown score type: " + scoreType);
				}
			}
		}

		public virtual int NumSamples()
		{
			return scores.Length;
		}

		public virtual double ConfidenceWeightedAccuracy()
		{
			double acc = 0;
			for (int recall = 1; recall <= NumSamples(); recall++)
			{
				acc += NumCorrect(recall) / (double)recall;
			}
			return acc / NumSamples();
		}

		public virtual void InitMC<F>(IProbabilisticClassifier<L, F> classifier, GeneralDataset<L, F> data)
		{
			//if (!(gData instanceof Dataset)) {
			//  throw new UnsupportedOperationException("Can only handle Datasets, not "+gData.getClass().getName());
			//}
			//
			//Dataset data = (Dataset)gData;
			IPriorityQueue<Pair<int, Pair<double, bool>>> q = new BinaryHeapPriorityQueue<Pair<int, Pair<double, bool>>>();
			total = 0;
			correct = 0;
			logLikelihood = 0.0;
			for (int i = 0; i < data.Size(); i++)
			{
				IDatum<L, F> d = data.GetRVFDatum(i);
				ICounter<L> scores = classifier.LogProbabilityOf(d);
				L guess = Counters.Argmax(scores);
				L correctLab = d.Label();
				double guessScore = scores.GetCount(guess);
				double correctScore = scores.GetCount(correctLab);
				int guessInd = data.LabelIndex().IndexOf(guess);
				int correctInd = data.LabelIndex().IndexOf(correctLab);
				total++;
				if (guessInd == correctInd)
				{
					correct++;
				}
				logLikelihood += correctScore;
				q.Add(new Pair<int, Pair<double, bool>>(int.Parse(i), new Pair<double, bool>(guessScore, bool.ValueOf(guessInd == correctInd))), -guessScore);
			}
			accuracy = (double)correct / (double)total;
			IList<Pair<int, Pair<double, bool>>> sorted = q.ToSortedList();
			scores = new double[sorted.Count];
			isCorrect = new bool[sorted.Count];
			for (int i_1 = 0; i_1 < sorted.Count; i_1++)
			{
				Pair<double, bool> next = sorted[i_1].Second();
				scores[i_1] = next.First();
				isCorrect[i_1] = next.Second();
			}
		}

		/// <summary>how many correct do we have if we return the most confident num recall ones</summary>
		public virtual int NumCorrect(int recall)
		{
			int correct = 0;
			for (int j = scores.Length - 1; j >= scores.Length - recall; j--)
			{
				if (isCorrect[j])
				{
					correct++;
				}
			}
			return correct;
		}

		public virtual int[] GetAccCoverage()
		{
			int[] arr = new int[NumSamples()];
			for (int recall = 1; recall <= NumSamples(); recall++)
			{
				arr[recall - 1] = NumCorrect(recall);
			}
			return arr;
		}

		public virtual string GetDescription(int numDigits)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			StringBuilder sb = new StringBuilder();
			double confWeightedAccuracy = ConfidenceWeightedAccuracy();
			sb.Append("--- Accuracy Stats ---").Append("\n");
			sb.Append("accuracy: ").Append(nf.Format(accuracy)).Append(" (").Append(correct).Append("/").Append(total).Append(")\n");
			sb.Append("confidence weighted accuracy :").Append(nf.Format(confWeightedAccuracy)).Append("\n");
			sb.Append("log-likelihood: ").Append(logLikelihood).Append("\n");
			if (saveFile != null)
			{
				string f = saveFile + "-" + saveIndex;
				sb.Append("saving accuracy info to ").Append(f).Append(".accuracy\n");
				StringUtils.PrintToFile(f + ".accuracy", AccuracyStats.ToStringArr(GetAccCoverage()));
				saveIndex++;
			}
			//sb.append("accuracy coverage: ").append(toStringArr(accrecall)).append("\n");
			//sb.append("optimal accuracy coverage: ").append(toStringArr(optaccrecall));
			return sb.ToString();
		}

		public override string ToString()
		{
			string accuracyType = null;
			if (scoreType == UseAccuracy)
			{
				accuracyType = "classification_accuracy";
			}
			else
			{
				if (scoreType == UseLoglikelihood)
				{
					accuracyType = "log_likelihood";
				}
				else
				{
					accuracyType = "unknown";
				}
			}
			return "MultiClassAccuracyStats(" + accuracyType + ")" + scoreType + UseAccuracy + UseLoglikelihood;
		}
	}
}
