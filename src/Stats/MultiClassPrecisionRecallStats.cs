using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Stats
{
	/// <author>Jenny Finkel</author>
	public class MultiClassPrecisionRecallStats<L> : IScorer<L>
	{
		/// <summary>Count of true positives.</summary>
		protected internal int[] tpCount;

		/// <summary>Count of false positives.</summary>
		protected internal int[] fpCount;

		/// <summary>Count of false negatives.</summary>
		protected internal int[] fnCount;

		protected internal IIndex<L> labelIndex;

		protected internal L negLabel;

		protected internal int negIndex = -1;

		public MultiClassPrecisionRecallStats(IClassifier<L, F> classifier, GeneralDataset<L, F> data, L negLabel)
		{
			this.negLabel = negLabel;
			Score(classifier, data);
		}

		public MultiClassPrecisionRecallStats(L negLabel)
		{
			this.negLabel = negLabel;
		}

		public virtual L GetNegLabel()
		{
			return negLabel;
		}

		public virtual double Score<F>(IProbabilisticClassifier<L, F> classifier, GeneralDataset<L, F> data)
		{
			return Score((IClassifier<L, F>)classifier, data);
		}

		public virtual double Score<F>(IClassifier<L, F> classifier, GeneralDataset<L, F> data)
		{
			IList<L> guesses = new List<L>();
			IList<L> labels = new List<L>();
			for (int i = 0; i < data.Size(); i++)
			{
				IDatum<L, F> d = data.GetRVFDatum(i);
				L guess = classifier.ClassOf(d);
				guesses.Add(guess);
			}
			int[] labelsArr = data.GetLabelsArray();
			labelIndex = data.labelIndex;
			for (int i_1 = 0; i_1 < data.Size(); i_1++)
			{
				labels.Add(labelIndex.Get(labelsArr[i_1]));
			}
			labelIndex = new HashIndex<L>();
			labelIndex.AddAll(data.LabelIndex().ObjectsList());
			labelIndex.AddAll(classifier.Labels());
			int numClasses = labelIndex.Size();
			tpCount = new int[numClasses];
			fpCount = new int[numClasses];
			fnCount = new int[numClasses];
			negIndex = labelIndex.IndexOf(negLabel);
			for (int i_2 = 0; i_2 < guesses.Count; ++i_2)
			{
				L guess = guesses[i_2];
				int guessIndex = labelIndex.IndexOf(guess);
				L label = labels[i_2];
				int trueIndex = labelIndex.IndexOf(label);
				if (guessIndex == trueIndex)
				{
					if (guessIndex != negIndex)
					{
						tpCount[guessIndex]++;
					}
				}
				else
				{
					if (guessIndex != negIndex)
					{
						fpCount[guessIndex]++;
					}
					if (trueIndex != negIndex)
					{
						fnCount[trueIndex]++;
					}
				}
			}
			return GetFMeasure();
		}

		/// <summary>Returns the current precision: <tt>tp/(tp+fp)</tt>.</summary>
		/// <remarks>
		/// Returns the current precision: <tt>tp/(tp+fp)</tt>.
		/// Returns 1.0 if tp and fp are both 0.
		/// </remarks>
		public virtual Triple<double, int, int> GetPrecisionInfo(L label)
		{
			int i = labelIndex.IndexOf(label);
			if (tpCount[i] == 0 && fpCount[i] == 0)
			{
				return new Triple<double, int, int>(1.0, tpCount[i], fpCount[i]);
			}
			return new Triple<double, int, int>((((double)tpCount[i]) / (tpCount[i] + fpCount[i])), tpCount[i], fpCount[i]);
		}

		public virtual double GetPrecision(L label)
		{
			return GetPrecisionInfo(label).First();
		}

		public virtual Triple<double, int, int> GetPrecisionInfo()
		{
			int tp = 0;
			int fp = 0;
			for (int i = 0; i < labelIndex.Size(); i++)
			{
				if (i == negIndex)
				{
					continue;
				}
				tp += tpCount[i];
				fp += fpCount[i];
			}
			return new Triple<double, int, int>((((double)tp) / (tp + fp)), tp, fp);
		}

		public virtual double GetPrecision()
		{
			return GetPrecisionInfo().First();
		}

		/// <summary>Returns a String summarizing precision that will print nicely.</summary>
		public virtual string GetPrecisionDescription(int numDigits)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			Triple<double, int, int> prec = GetPrecisionInfo();
			return nf.Format(prec.First()) + "  (" + prec.Second() + "/" + (prec.Second() + prec.Third()) + ")";
		}

		public virtual string GetPrecisionDescription(int numDigits, L label)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			Triple<double, int, int> prec = GetPrecisionInfo(label);
			return nf.Format(prec.First()) + "  (" + prec.Second() + "/" + (prec.Second() + prec.Third()) + ")";
		}

		public virtual Triple<double, int, int> GetRecallInfo(L label)
		{
			int i = labelIndex.IndexOf(label);
			if (tpCount[i] == 0 && fnCount[i] == 0)
			{
				return new Triple<double, int, int>(1.0, tpCount[i], fnCount[i]);
			}
			return new Triple<double, int, int>((((double)tpCount[i]) / (tpCount[i] + fnCount[i])), tpCount[i], fnCount[i]);
		}

		public virtual double GetRecall(L label)
		{
			return GetRecallInfo(label).First();
		}

		public virtual Triple<double, int, int> GetRecallInfo()
		{
			int tp = 0;
			int fn = 0;
			for (int i = 0; i < labelIndex.Size(); i++)
			{
				if (i == negIndex)
				{
					continue;
				}
				tp += tpCount[i];
				fn += fnCount[i];
			}
			return new Triple<double, int, int>((((double)tp) / (tp + fn)), tp, fn);
		}

		public virtual double GetRecall()
		{
			return GetRecallInfo().First();
		}

		/// <summary>Returns a String summarizing precision that will print nicely.</summary>
		public virtual string GetRecallDescription(int numDigits)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			Triple<double, int, int> recall = GetRecallInfo();
			return nf.Format(recall.First()) + "  (" + recall.Second() + "/" + (recall.Second() + recall.Third()) + ")";
		}

		public virtual string GetRecallDescription(int numDigits, L label)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			Triple<double, int, int> recall = GetRecallInfo(label);
			return nf.Format(recall.First()) + "  (" + recall.Second() + "/" + (recall.Second() + recall.Third()) + ")";
		}

		public virtual double GetFMeasure(L label)
		{
			double p = GetPrecision(label);
			double r = GetRecall(label);
			double f = (2 * p * r) / (p + r);
			return f;
		}

		public virtual double GetFMeasure()
		{
			double p = GetPrecision();
			double r = GetRecall();
			double f = (2 * p * r) / (p + r);
			return f;
		}

		/// <summary>Returns a String summarizing F1 that will print nicely.</summary>
		public virtual string GetF1Description(int numDigits)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			return nf.Format(GetFMeasure());
		}

		public virtual string GetF1Description(int numDigits, L label)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			return nf.Format(GetFMeasure(label));
		}

		/// <summary>Returns a String summarizing F1 that will print nicely.</summary>
		public virtual string GetDescription(int numDigits)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("--- PR Stats ---").Append("\n");
			foreach (L label in labelIndex)
			{
				if (label == null || label.Equals(negLabel))
				{
					continue;
				}
				sb.Append("** ").Append(label.ToString()).Append(" **\n");
				sb.Append("\tPrec:   ").Append(GetPrecisionDescription(numDigits, label)).Append("\n");
				sb.Append("\tRecall: ").Append(GetRecallDescription(numDigits, label)).Append("\n");
				sb.Append("\tF1:     ").Append(GetF1Description(numDigits, label)).Append("\n");
			}
			sb.Append("** Overall **\n");
			sb.Append("\tPrec:   ").Append(GetPrecisionDescription(numDigits)).Append("\n");
			sb.Append("\tRecall: ").Append(GetRecallDescription(numDigits)).Append("\n");
			sb.Append("\tF1:     ").Append(GetF1Description(numDigits));
			return sb.ToString();
		}
	}
}
