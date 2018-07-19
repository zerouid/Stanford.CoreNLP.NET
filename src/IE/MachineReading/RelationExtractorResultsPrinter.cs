using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Text;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading
{
	public class RelationExtractorResultsPrinter : ResultsPrinter
	{
		protected internal bool createUnrelatedRelations;

		protected internal readonly RelationMentionFactory relationMentionFactory;

		public RelationExtractorResultsPrinter(RelationMentionFactory factory)
			: this(factory, true)
		{
		}

		public RelationExtractorResultsPrinter()
			: this(new RelationMentionFactory(), true)
		{
		}

		public RelationExtractorResultsPrinter(bool createUnrelatedRelations)
			: this(new RelationMentionFactory(), createUnrelatedRelations)
		{
		}

		public RelationExtractorResultsPrinter(RelationMentionFactory factory, bool createUnrelatedRelations)
		{
			this.createUnrelatedRelations = createUnrelatedRelations;
			this.relationMentionFactory = factory;
		}

		private const int MaxLabelLength = 31;

		public override void PrintResults(PrintWriter pw, IList<ICoreMap> goldStandard, IList<ICoreMap> extractorOutput)
		{
			ResultsPrinter.Align(goldStandard, extractorOutput);
			// the mention factory cannot be null here
			System.Diagnostics.Debug.Assert(relationMentionFactory != null, "ERROR: RelationExtractorResultsPrinter.relationMentionFactory cannot be null in printResults!");
			// Count predicted-actual relation type pairs
			ICounter<Pair<string, string>> results = new ClassicCounter<Pair<string, string>>();
			ClassicCounter<string> labelCount = new ClassicCounter<string>();
			// TODO: assumes binary relations
			for (int goldSentenceIndex = 0; goldSentenceIndex < goldStandard.Count; goldSentenceIndex++)
			{
				foreach (RelationMention goldRelation in AnnotationUtils.GetAllRelations(relationMentionFactory, goldStandard[goldSentenceIndex], createUnrelatedRelations))
				{
					ICoreMap extractorSentence = extractorOutput[goldSentenceIndex];
					IList<RelationMention> extractorRelations = AnnotationUtils.GetRelations(relationMentionFactory, extractorSentence, goldRelation.GetArg(0), goldRelation.GetArg(1));
					labelCount.IncrementCount(goldRelation.GetType());
					foreach (RelationMention extractorRelation in extractorRelations)
					{
						results.IncrementCount(new Pair<string, string>(extractorRelation.GetType(), goldRelation.GetType()));
					}
				}
			}
			PrintResultsInternal(pw, results, labelCount);
		}

		private void PrintResultsInternal(PrintWriter pw, ICounter<Pair<string, string>> results, ClassicCounter<string> labelCount)
		{
			ClassicCounter<string> correct = new ClassicCounter<string>();
			ClassicCounter<string> predictionCount = new ClassicCounter<string>();
			bool countGoldLabels = false;
			if (labelCount == null)
			{
				labelCount = new ClassicCounter<string>();
				countGoldLabels = true;
			}
			foreach (Pair<string, string> predictedActual in results.KeySet())
			{
				string predicted = predictedActual.first;
				string actual = predictedActual.second;
				if (predicted.Equals(actual))
				{
					correct.IncrementCount(actual, results.GetCount(predictedActual));
				}
				predictionCount.IncrementCount(predicted, results.GetCount(predictedActual));
				if (countGoldLabels)
				{
					labelCount.IncrementCount(actual, results.GetCount(predictedActual));
				}
			}
			DecimalFormat formatter = new DecimalFormat();
			formatter.SetMaximumFractionDigits(1);
			formatter.SetMinimumFractionDigits(1);
			double totalCount = 0;
			double totalCorrect = 0;
			double totalPredicted = 0;
			pw.Println("Label\tCorrect\tPredict\tActual\tPrecn\tRecall\tF");
			IList<string> labels = new List<string>(labelCount.KeySet());
			labels.Sort();
			foreach (string label in labels)
			{
				double numcorrect = correct.GetCount(label);
				double predicted = predictionCount.GetCount(label);
				double trueCount = labelCount.GetCount(label);
				double precision = (predicted > 0) ? (numcorrect / predicted) : 0;
				double recall = numcorrect / trueCount;
				double f = (precision + recall > 0) ? 2 * precision * recall / (precision + recall) : 0.0;
				pw.Println(StringUtils.PadOrTrim(label, MaxLabelLength) + "\t" + numcorrect + "\t" + predicted + "\t" + trueCount + "\t" + formatter.Format(precision * 100) + "\t" + formatter.Format(100 * recall) + "\t" + formatter.Format(100 * f));
				if (!RelationMention.IsUnrelatedLabel(label))
				{
					totalCount += trueCount;
					totalCorrect += numcorrect;
					totalPredicted += predicted;
				}
			}
			double precision_1 = (totalPredicted > 0) ? (totalCorrect / totalPredicted) : 0;
			double recall_1 = totalCorrect / totalCount;
			double f_1 = (totalPredicted > 0 && totalCorrect > 0) ? 2 * precision_1 * recall_1 / (precision_1 + recall_1) : 0.0;
			pw.Println("Total\t" + totalCorrect + "\t" + totalPredicted + "\t" + totalCount + "\t" + formatter.Format(100 * precision_1) + "\t" + formatter.Format(100 * recall_1) + "\t" + formatter.Format(100 * f_1));
		}

		public override void PrintResultsUsingLabels(PrintWriter pw, IList<string> goldStandard, IList<string> extractorOutput)
		{
			// Count predicted-actual relation type pairs
			ICounter<Pair<string, string>> results = new ClassicCounter<Pair<string, string>>();
			System.Diagnostics.Debug.Assert((goldStandard.Count == extractorOutput.Count));
			for (int i = 0; i < goldStandard.Count; i++)
			{
				results.IncrementCount(new Pair<string, string>(extractorOutput[i], goldStandard[i]));
			}
			PrintResultsInternal(pw, results, null);
		}
	}
}
