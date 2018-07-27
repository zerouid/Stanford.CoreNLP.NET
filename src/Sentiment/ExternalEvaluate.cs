using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Neural.Rnn;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Sentiment
{
	/// <summary>Evaluate predictions made outside of the RNTN.</summary>
	/// <author>
	/// Michael Haas
	/// <literal><haas@cl.uni-heidelberg.de></literal>
	/// </author>
	public class ExternalEvaluate : AbstractEvaluate
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sentiment.ExternalEvaluate));

		private IList<Tree> predicted;

		public ExternalEvaluate(RNNOptions op, IList<Tree> predictedTrees)
			: base(op)
		{
			this.predicted = predictedTrees;
		}

		public override void PopulatePredictedLabels(IList<Tree> trees)
		{
			if (trees.Count != this.predicted.Count)
			{
				throw new ArgumentException("Number of gold and predicted trees not equal!");
			}
			for (int i = 0; i < trees.Count; i++)
			{
				IEnumerator<Tree> goldTree = trees[i].GetEnumerator();
				IEnumerator<Tree> predictedTree = this.predicted[i].GetEnumerator();
				while (goldTree.MoveNext() || predictedTree.MoveNext())
				{
					Tree goldNode = goldTree.Current;
					Tree predictedNode = predictedTree.Current;
					if (goldNode == null || predictedNode == null)
					{
						throw new ArgumentException("Trees not of equal length");
					}
					if (goldNode.IsLeaf())
					{
						continue;
					}
					CoreLabel label = (CoreLabel)goldNode.Label();
					label.Set(typeof(RNNCoreAnnotations.PredictedClass), RNNCoreAnnotations.GetPredictedClass(predictedNode));
				}
			}
		}

		/// <summary>
		/// Expected arguments are
		/// <c>-gold gold -predicted predicted</c>
		/// For example <br />
		/// <c>java edu.stanford.nlp.sentiment.ExternalEvaluate annotatedTrees.txt predictedTrees.txt</c>
		/// </summary>
		public static void Main(string[] args)
		{
			RNNOptions curOptions = new RNNOptions();
			string goldPath = null;
			string predictedPath = null;
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-gold"))
				{
					goldPath = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-predicted"))
					{
						predictedPath = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						int newArgIndex = curOptions.SetOption(args, argIndex);
						if (newArgIndex == argIndex)
						{
							throw new ArgumentException("Unknown argument " + args[argIndex]);
						}
						argIndex = newArgIndex;
					}
				}
			}
			if (goldPath == null)
			{
				log.Info("goldPath not set. Exit.");
				System.Environment.Exit(-1);
			}
			if (predictedPath == null)
			{
				log.Info("predictedPath not set. Exit.");
				System.Environment.Exit(-1);
			}
			// filterUnknown not supported because I'd need to know which sentences
			// are removed to remove them from predicted
			IList<Tree> goldTrees = SentimentUtils.ReadTreesWithGoldLabels(goldPath);
			IList<Tree> predictedTrees = SentimentUtils.ReadTreesWithPredictedLabels(predictedPath);
			Edu.Stanford.Nlp.Sentiment.ExternalEvaluate evaluator = new Edu.Stanford.Nlp.Sentiment.ExternalEvaluate(curOptions, predictedTrees);
			evaluator.Eval(goldTrees);
			evaluator.PrintSummary();
		}
	}
}
