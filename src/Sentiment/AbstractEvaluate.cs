using System.Collections.Generic;
using Edu.Stanford.Nlp.Neural.Rnn;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Sentiment
{
	/// <author>John Bauer</author>
	/// <author>Michael Haas <haas@cl.uni-heidelberg.de> (extracted this abstract class from Evaluate)</author>
	public abstract class AbstractEvaluate
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sentiment.AbstractEvaluate));

		internal string[] equivalenceClassNames;

		internal int labelsCorrect;

		internal int labelsIncorrect;

		internal int[][] labelConfusion;

		internal int rootLabelsCorrect;

		internal int rootLabelsIncorrect;

		internal int[][] rootLabelConfusion;

		internal IntCounter<int> lengthLabelsCorrect;

		internal IntCounter<int> lengthLabelsIncorrect;

		internal TopNGramRecord ngrams;

		internal const int NumNgrams = 5;

		internal int[][] equivalenceClasses;

		protected internal static readonly NumberFormat Nf = new DecimalFormat("0.000000");

		private RNNOptions op = null;

		public AbstractEvaluate(RNNOptions options)
		{
			// the matrix will be [gold][predicted]
			// TODO: make this an option
			this.op = options;
			this.Reset();
		}

		protected internal static void PrintConfusionMatrix(string name, int[][] confusion)
		{
			log.Info(name + " confusion matrix");
			ConfusionMatrix<int> confusionMatrix = new ConfusionMatrix<int>();
			confusionMatrix.SetUseRealLabels(true);
			for (int i = 0; i < confusion.Length; ++i)
			{
				for (int j = 0; j < confusion[i].Length; ++j)
				{
					confusionMatrix.Add(j, i, confusion[i][j]);
				}
			}
			log.Info(confusionMatrix);
		}

		protected internal static double[] ApproxAccuracy(int[][] confusion, int[][] classes)
		{
			int[] correct = new int[classes.Length];
			int[] total = new int[classes.Length];
			double[] results = new double[classes.Length];
			for (int i = 0; i < classes.Length; ++i)
			{
				for (int j = 0; j < classes[i].Length; ++j)
				{
					for (int k = 0; k < classes[i].Length; ++k)
					{
						correct[i] += confusion[classes[i][j]][classes[i][k]];
					}
					for (int k_1 = 0; k_1 < confusion[classes[i][j]].Length; ++k_1)
					{
						total[i] += confusion[classes[i][j]][k_1];
					}
				}
				results[i] = ((double)correct[i]) / ((double)(total[i]));
			}
			return results;
		}

		protected internal static double ApproxCombinedAccuracy(int[][] confusion, int[][] classes)
		{
			int correct = 0;
			int total = 0;
			foreach (int[] aClass in classes)
			{
				for (int j = 0; j < aClass.Length; ++j)
				{
					for (int k = 0; k < aClass.Length; ++k)
					{
						correct += confusion[aClass[j]][aClass[k]];
					}
					for (int k_1 = 0; k_1 < confusion[aClass[j]].Length; ++k_1)
					{
						total += confusion[aClass[j]][k_1];
					}
				}
			}
			return ((double)correct) / ((double)(total));
		}

		public virtual void Reset()
		{
			labelsCorrect = 0;
			labelsIncorrect = 0;
			labelConfusion = new int[op.numClasses][];
			rootLabelsCorrect = 0;
			rootLabelsIncorrect = 0;
			rootLabelConfusion = new int[op.numClasses][];
			lengthLabelsCorrect = new IntCounter<int>();
			lengthLabelsIncorrect = new IntCounter<int>();
			equivalenceClasses = op.equivalenceClasses;
			equivalenceClassNames = op.equivalenceClassNames;
			if (op.testOptions.ngramRecordSize > 0)
			{
				ngrams = new TopNGramRecord(op.numClasses, op.testOptions.ngramRecordSize, op.testOptions.ngramRecordMaximumLength);
			}
			else
			{
				ngrams = null;
			}
		}

		public virtual void Eval(IList<Tree> trees)
		{
			this.PopulatePredictedLabels(trees);
			foreach (Tree tree in trees)
			{
				Eval(tree);
			}
		}

		public virtual void Eval(Tree tree)
		{
			//cag.forwardPropagateTree(tree);
			CountTree(tree);
			CountRoot(tree);
			CountLengthAccuracy(tree);
			if (ngrams != null)
			{
				ngrams.CountTree(tree);
			}
		}

		protected internal virtual int CountLengthAccuracy(Tree tree)
		{
			if (tree.IsLeaf())
			{
				return 0;
			}
			int gold = RNNCoreAnnotations.GetGoldClass(tree);
			int predicted = RNNCoreAnnotations.GetPredictedClass(tree);
			int length;
			if (tree.IsPreTerminal())
			{
				length = 1;
			}
			else
			{
				length = 0;
				foreach (Tree child in tree.Children())
				{
					length += CountLengthAccuracy(child);
				}
			}
			if (gold >= 0)
			{
				if (gold.Equals(predicted))
				{
					lengthLabelsCorrect.IncrementCount(length);
				}
				else
				{
					lengthLabelsIncorrect.IncrementCount(length);
				}
			}
			return length;
		}

		protected internal virtual void CountTree(Tree tree)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			foreach (Tree child in tree.Children())
			{
				CountTree(child);
			}
			int gold = RNNCoreAnnotations.GetGoldClass(tree);
			int predicted = RNNCoreAnnotations.GetPredictedClass(tree);
			if (gold >= 0)
			{
				if (gold.Equals(predicted))
				{
					labelsCorrect++;
				}
				else
				{
					labelsIncorrect++;
				}
				labelConfusion[gold][predicted]++;
			}
		}

		protected internal virtual void CountRoot(Tree tree)
		{
			int gold = RNNCoreAnnotations.GetGoldClass(tree);
			int predicted = RNNCoreAnnotations.GetPredictedClass(tree);
			if (gold >= 0)
			{
				if (gold.Equals(predicted))
				{
					rootLabelsCorrect++;
				}
				else
				{
					rootLabelsIncorrect++;
				}
				rootLabelConfusion[gold][predicted]++;
			}
		}

		public virtual double ExactNodeAccuracy()
		{
			return (double)labelsCorrect / ((double)(labelsCorrect + labelsIncorrect));
		}

		public virtual double ExactRootAccuracy()
		{
			return (double)rootLabelsCorrect / ((double)(rootLabelsCorrect + rootLabelsIncorrect));
		}

		public virtual ICounter<int> LengthAccuracies()
		{
			ICollection<int> keys = Generics.NewHashSet();
			Sharpen.Collections.AddAll(keys, lengthLabelsCorrect.KeySet());
			Sharpen.Collections.AddAll(keys, lengthLabelsIncorrect.KeySet());
			ICounter<int> results = new ClassicCounter<int>();
			foreach (int key in keys)
			{
				results.SetCount(key, lengthLabelsCorrect.GetCount(key) / (lengthLabelsCorrect.GetCount(key) + lengthLabelsIncorrect.GetCount(key)));
			}
			return results;
		}

		public virtual void PrintLengthAccuracies()
		{
			ICounter<int> accuracies = LengthAccuracies();
			ICollection<int> keys = Generics.NewTreeSet();
			Sharpen.Collections.AddAll(keys, accuracies.KeySet());
			log.Info("Label accuracy at various lengths:");
			foreach (int key in keys)
			{
				log.Info(StringUtils.PadLeft(int.ToString(key), 4) + ": " + Nf.Format(accuracies.GetCount(key)));
			}
		}

		public virtual void PrintSummary()
		{
			log.Info("EVALUATION SUMMARY");
			log.Info("Tested " + (labelsCorrect + labelsIncorrect) + " labels");
			log.Info("  " + labelsCorrect + " correct");
			log.Info("  " + labelsIncorrect + " incorrect");
			log.Info("  " + Nf.Format(ExactNodeAccuracy()) + " accuracy");
			log.Info("Tested " + (rootLabelsCorrect + rootLabelsIncorrect) + " roots");
			log.Info("  " + rootLabelsCorrect + " correct");
			log.Info("  " + rootLabelsIncorrect + " incorrect");
			log.Info("  " + Nf.Format(ExactRootAccuracy()) + " accuracy");
			PrintConfusionMatrix("Label", labelConfusion);
			PrintConfusionMatrix("Root label", rootLabelConfusion);
			if (equivalenceClasses != null && equivalenceClassNames != null)
			{
				double[] approxLabelAccuracy = ApproxAccuracy(labelConfusion, equivalenceClasses);
				for (int i = 0; i < equivalenceClassNames.Length; ++i)
				{
					log.Info("Approximate " + equivalenceClassNames[i] + " label accuracy: " + Nf.Format(approxLabelAccuracy[i]));
				}
				log.Info("Combined approximate label accuracy: " + Nf.Format(ApproxCombinedAccuracy(labelConfusion, equivalenceClasses)));
				double[] approxRootLabelAccuracy = ApproxAccuracy(rootLabelConfusion, equivalenceClasses);
				for (int i_1 = 0; i_1 < equivalenceClassNames.Length; ++i_1)
				{
					log.Info("Approximate " + equivalenceClassNames[i_1] + " root label accuracy: " + Nf.Format(approxRootLabelAccuracy[i_1]));
				}
				log.Info("Combined approximate root label accuracy: " + Nf.Format(ApproxCombinedAccuracy(rootLabelConfusion, equivalenceClasses)));
				log.Info();
			}
			if (op.testOptions.ngramRecordSize > 0)
			{
				log.Info(ngrams);
			}
			if (op.testOptions.printLengthAccuracies)
			{
				PrintLengthAccuracies();
			}
		}

		/// <summary>Sets the predicted sentiment label for all trees given.</summary>
		/// <remarks>
		/// Sets the predicted sentiment label for all trees given.
		/// This method sets the
		/// <see cref="Edu.Stanford.Nlp.Neural.Rnn.RNNCoreAnnotations.PredictedClass"/>
		/// annotation
		/// for all nodes in all trees.
		/// </remarks>
		/// <param name="trees">List of Trees to be annotated</param>
		public abstract void PopulatePredictedLabels(IList<Tree> trees);
	}
}
