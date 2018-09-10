using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;








namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>Extension of MultiClassPrecisionRecallStats that also computes accuracy</summary>
	/// <author>Angel Chang</author>
	public class MultiClassPrecisionRecallExtendedStats<L> : MultiClassPrecisionRecallStats<L>
	{
		protected internal IntCounter<L> correctGuesses;

		protected internal IntCounter<L> foundCorrect;

		protected internal IntCounter<L> foundGuessed;

		protected internal int tokensCount = 0;

		protected internal int tokensCorrect = 0;

		protected internal int noLabel = 0;

		protected internal Func<string, L> stringConverter;

		public MultiClassPrecisionRecallExtendedStats(IClassifier<L, F> classifier, GeneralDataset<L, F> data, L negLabel)
			: base(classifier, data, negLabel)
		{
		}

		public MultiClassPrecisionRecallExtendedStats(L negLabel)
			: base(negLabel)
		{
		}

		public MultiClassPrecisionRecallExtendedStats(IIndex<L> dataLabelIndex, L negLabel)
			: this(negLabel)
		{
			SetLabelIndex(dataLabelIndex);
		}

		public virtual void SetLabelIndex(IIndex<L> dataLabelIndex)
		{
			labelIndex = dataLabelIndex;
			negIndex = labelIndex.IndexOf(negLabel);
		}

		public override double Score<F>(IClassifier<L, F> classifier, GeneralDataset<L, F> data)
		{
			labelIndex = new HashIndex<L>();
			labelIndex.AddAll(classifier.Labels());
			labelIndex.AddAll(data.labelIndex.ObjectsList());
			ClearCounts();
			int[] labelsArr = data.GetLabelsArray();
			for (int i = 0; i < data.Size(); i++)
			{
				IDatum<L, F> d = data.GetRVFDatum(i);
				L guess = classifier.ClassOf(d);
				AddGuess(guess, labelIndex.Get(labelsArr[i]));
			}
			FinalizeCounts();
			return GetFMeasure();
		}

		/// <summary>Returns the score (F1) for the given list of guesses</summary>
		/// <param name="guesses">- Guesses by classifier</param>
		/// <param name="trueLabels">- Gold labels to compare guesses against</param>
		/// <param name="dataLabelIndex">- Index of labels</param>
		/// <returns>F1 score</returns>
		public virtual double Score(IList<L> guesses, IList<L> trueLabels, IIndex<L> dataLabelIndex)
		{
			SetLabelIndex(dataLabelIndex);
			return Score(guesses, trueLabels);
		}

		/// <summary>Returns the score (F1) for the given list of guesses</summary>
		/// <param name="guesses">- Guesses by classifier</param>
		/// <param name="trueLabels">- Gold labels to compare guesses against</param>
		/// <returns>F1 score</returns>
		public virtual double Score(IList<L> guesses, IList<L> trueLabels)
		{
			ClearCounts();
			AddGuesses(guesses, trueLabels);
			FinalizeCounts();
			return GetFMeasure();
		}

		public virtual double Score()
		{
			FinalizeCounts();
			return GetFMeasure();
		}

		public virtual void ClearCounts()
		{
			if (foundCorrect != null)
			{
				foundCorrect.Clear();
			}
			else
			{
				foundCorrect = new IntCounter<L>();
			}
			if (foundGuessed != null)
			{
				foundGuessed.Clear();
			}
			else
			{
				foundGuessed = new IntCounter<L>();
			}
			if (correctGuesses != null)
			{
				correctGuesses.Clear();
			}
			else
			{
				correctGuesses = new IntCounter<L>();
			}
			if (tpCount != null)
			{
				Arrays.Fill(tpCount, 0);
			}
			if (fnCount != null)
			{
				Arrays.Fill(fnCount, 0);
			}
			if (fpCount != null)
			{
				Arrays.Fill(fpCount, 0);
			}
			tokensCount = 0;
			tokensCorrect = 0;
		}

		protected internal virtual void FinalizeCounts()
		{
			negIndex = labelIndex.IndexOf(negLabel);
			int numClasses = labelIndex.Size();
			if (tpCount == null || tpCount.Length != numClasses)
			{
				tpCount = new int[numClasses];
			}
			if (fpCount == null || fpCount.Length != numClasses)
			{
				fpCount = new int[numClasses];
			}
			if (fnCount == null || fnCount.Length != numClasses)
			{
				fnCount = new int[numClasses];
			}
			for (int i = 0; i < numClasses; i++)
			{
				L label = labelIndex.Get(i);
				tpCount[i] = correctGuesses.GetIntCount(label);
				fnCount[i] = foundCorrect.GetIntCount(label) - tpCount[i];
				fpCount[i] = foundGuessed.GetIntCount(label) - tpCount[i];
			}
		}

		protected internal virtual void MarkBoundary()
		{
		}

		protected internal virtual void AddGuess(L guess, L label)
		{
			AddGuess(guess, label, true);
		}

		protected internal virtual void AddGuess(L guess, L label, bool addUnknownLabels)
		{
			if (label == null)
			{
				noLabel++;
				return;
			}
			if (addUnknownLabels)
			{
				if (labelIndex == null)
				{
					labelIndex = new HashIndex<L>();
				}
				labelIndex.Add(guess);
				labelIndex.Add(label);
			}
			if (guess.Equals(label))
			{
				correctGuesses.IncrementCount(label);
				tokensCorrect++;
			}
			if (!guess.Equals(negLabel))
			{
				foundGuessed.IncrementCount(guess);
			}
			if (!label.Equals(negLabel))
			{
				foundCorrect.IncrementCount(label);
			}
			tokensCount++;
		}

		public virtual void AddGuesses(IList<L> guesses, IList<L> trueLabels)
		{
			for (int i = 0; i < guesses.Count; ++i)
			{
				L guess = guesses[i];
				L label = trueLabels[i];
				AddGuess(guess, label);
			}
		}

		/// <summary>Return overall number of correct answers</summary>
		public virtual int GetCorrect()
		{
			return correctGuesses.TotalIntCount();
		}

		public virtual int GetCorrect(L label)
		{
			return correctGuesses.GetIntCount(label);
		}

		public virtual int GetRetrieved(L label)
		{
			return foundGuessed.GetIntCount(label);
		}

		public virtual int GetRetrieved()
		{
			return foundGuessed.TotalIntCount();
		}

		public virtual int GetRelevant(L label)
		{
			return foundCorrect.GetIntCount(label);
		}

		public virtual int GetRelevant()
		{
			return foundCorrect.TotalIntCount();
		}

		/// <summary>Return overall per token accuracy</summary>
		public virtual Triple<double, int, int> GetAccuracyInfo()
		{
			int totalCorrect = tokensCorrect;
			int totalWrong = tokensCount - tokensCorrect;
			return new Triple<double, int, int>((((double)totalCorrect) / tokensCount), totalCorrect, totalWrong);
		}

		public virtual double GetAccuracy()
		{
			return GetAccuracyInfo().First();
		}

		/// <summary>Returns a String summarizing overall accuracy that will print nicely.</summary>
		public virtual string GetAccuracyDescription(int numDigits)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(numDigits);
			Triple<double, int, int> accu = GetAccuracyInfo();
			return nf.Format(accu.First()) + "  (" + accu.Second() + "/" + (accu.Second() + accu.Third()) + ")";
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual double Score(string filename, string delimiter)
		{
			return Score(filename, delimiter, null);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual double Score(string filename, string delimiter, string boundary)
		{
			return Score(IOUtils.GetBufferedFileReader(filename), delimiter, boundary);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual double Score(BufferedReader br, string delimiter)
		{
			return Score(br, delimiter, null);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual double Score(BufferedReader br, string delimiter, string boundary)
		{
			int TokenIndex = 0;
			int AnswerIndex = 1;
			int GuessIndex = 2;
			string line;
			Pattern delimPattern = Pattern.Compile(delimiter);
			ClearCounts();
			while ((line = br.ReadLine()) != null)
			{
				line = line.Trim();
				if (line.Length > 0)
				{
					string[] fields = delimPattern.Split(line);
					if (boundary != null && boundary.Equals(fields[TokenIndex]))
					{
						MarkBoundary();
					}
					else
					{
						L answer = stringConverter.Apply(fields[AnswerIndex]);
						L guess = stringConverter.Apply(fields[GuessIndex]);
						AddGuess(guess, answer);
					}
				}
				else
				{
					MarkBoundary();
				}
			}
			FinalizeCounts();
			return GetFMeasure();
		}

		public virtual IList<L> GetLabels()
		{
			return labelIndex.ObjectsList();
		}

		public virtual string GetConllEvalString()
		{
			return GetConllEvalString(true);
		}

		public virtual string GetConllEvalString(bool ignoreNegLabel)
		{
			IList<L> labels = GetLabels();
			if (labels.Count > 1 && labels[0] is IComparable)
			{
				IList<IComparable> sortedLabels = (IList<IComparable>)labels;
				sortedLabels.Sort();
			}
			return GetConllEvalString(labels, ignoreNegLabel);
		}

		private string GetConllEvalString(IList<L> orderedLabels, bool ignoreNegLabel)
		{
			StringBuilder sb = new StringBuilder();
			int correctPhrases = GetCorrect() - GetCorrect(negLabel);
			Triple<double, int, int> accuracyInfo = GetAccuracyInfo();
			int totalCount = accuracyInfo.Second() + accuracyInfo.Third();
			sb.Append("processed " + totalCount + " tokens with " + GetRelevant() + " phrases; ");
			sb.Append("found: " + GetRetrieved() + " phrases; correct: " + correctPhrases + "\n");
			Formatter formatter = new Formatter(sb, Locale.Us);
			formatter.Format("accuracy: %6.2f%%; ", accuracyInfo.First() * 100);
			formatter.Format("precision: %6.2f%%; ", GetPrecision() * 100);
			formatter.Format("recall: %6.2f%%; ", GetRecall() * 100);
			formatter.Format("FB1: %6.2f\n", GetFMeasure() * 100);
			foreach (L label in orderedLabels)
			{
				if (ignoreNegLabel && label.Equals(negLabel))
				{
					continue;
				}
				formatter.Format("%17s: ", label);
				formatter.Format("precision: %6.2f%%; ", GetPrecision(label) * 100);
				formatter.Format("recall: %6.2f%%; ", GetRecall(label) * 100);
				formatter.Format("FB1: %6.2f  %d\n", GetFMeasure(label) * 100, GetRetrieved(label));
			}
			return sb.ToString();
		}

		public class StringStringConverter : Func<string, string>
		{
			public virtual string Apply(string str)
			{
				return str;
			}
		}

		public class MultiClassStringLabelStats : MultiClassPrecisionRecallExtendedStats<string>
		{
			public MultiClassStringLabelStats(IClassifier<string, F> classifier, GeneralDataset<string, F> data, string negLabel)
				: base(classifier, data, negLabel)
			{
				stringConverter = new MultiClassPrecisionRecallExtendedStats.StringStringConverter();
			}

			public MultiClassStringLabelStats(string negLabel)
				: base(negLabel)
			{
				stringConverter = new MultiClassPrecisionRecallExtendedStats.StringStringConverter();
			}

			public MultiClassStringLabelStats(IIndex<string> dataLabelIndex, string negLabel)
				: this(negLabel)
			{
				SetLabelIndex(dataLabelIndex);
			}
		}
	}
}
