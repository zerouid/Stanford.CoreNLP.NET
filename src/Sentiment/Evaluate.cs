using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Sentiment
{
	/// <author>John Bauer</author>
	public class Evaluate : AbstractEvaluate
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sentiment.Evaluate));

		internal readonly SentimentCostAndGradient cag;

		internal readonly SentimentModel model;

		public Evaluate(SentimentModel model)
			: base(model.op)
		{
			this.model = model;
			this.cag = new SentimentCostAndGradient(model, null);
		}

		public override void PopulatePredictedLabels(IList<Tree> trees)
		{
			foreach (Tree tree in trees)
			{
				cag.ForwardPropagateTree(tree);
			}
		}

		/// <summary>
		/// Expected arguments are <code> -model model -treebank treebank </code>
		/// <br />
		/// For example <br />
		/// <code>
		/// java edu.stanford.nlp.sentiment.Evaluate
		/// edu/stanford/nlp/models/sentiment/sentiment.ser.gz
		/// /u/nlp/data/sentiment/trees/dev.txt
		/// </code>
		/// Other arguments are available, for example <code> -numClasses</code>.
		/// </summary>
		/// <remarks>
		/// Expected arguments are <code> -model model -treebank treebank </code>
		/// <br />
		/// For example <br />
		/// <code>
		/// java edu.stanford.nlp.sentiment.Evaluate
		/// edu/stanford/nlp/models/sentiment/sentiment.ser.gz
		/// /u/nlp/data/sentiment/trees/dev.txt
		/// </code>
		/// Other arguments are available, for example <code> -numClasses</code>.
		/// See RNNOptions.java, RNNTestOptions.java and RNNTrainOptions.java for
		/// more arguments.
		/// The configuration is usually derived from the RNN model file, which is
		/// not available here as the predictions are external. It is the caller's
		/// responsibility to provide a configuration matching the settings of
		/// the external predictor. Flags of interest include
		/// <code> -equivalenceClasses </code>.
		/// </remarks>
		public static void Main(string[] args)
		{
			string modelPath = null;
			string treePath = null;
			bool filterUnknown = false;
			IList<string> remainingArgs = Generics.NewArrayList();
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-model"))
				{
					modelPath = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-treebank"))
					{
						treePath = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-filterUnknown"))
						{
							filterUnknown = true;
							argIndex++;
						}
						else
						{
							remainingArgs.Add(args[argIndex]);
							argIndex++;
						}
					}
				}
			}
			string[] newArgs = new string[remainingArgs.Count];
			Sharpen.Collections.ToArray(remainingArgs, newArgs);
			SentimentModel model = SentimentModel.LoadSerialized(modelPath);
			for (int argIndex_1 = 0; argIndex_1 < newArgs.Length; )
			{
				int newIndex = model.op.SetOption(newArgs, argIndex_1);
				if (argIndex_1 == newIndex)
				{
					log.Info("Unknown argument " + newArgs[argIndex_1]);
					throw new ArgumentException("Unknown argument " + newArgs[argIndex_1]);
				}
				argIndex_1 = newIndex;
			}
			IList<Tree> trees = SentimentUtils.ReadTreesWithGoldLabels(treePath);
			if (filterUnknown)
			{
				trees = SentimentUtils.FilterUnknownRoots(trees);
			}
			Edu.Stanford.Nlp.Sentiment.Evaluate eval = new Edu.Stanford.Nlp.Sentiment.Evaluate(model);
			eval.Eval(trees);
			eval.PrintSummary();
		}
	}
}
