using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	public class CrossValidateTestOptions
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(CrossValidateTestOptions));

		public static readonly double[] weights = new double[] { 0.0, 0.05, 0.1, 0.15, 0.2, 0.25, 0.3, 0.4, 0.5, 1.0 };

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			string dvmodelFile = null;
			string lexparserFile = null;
			string testTreebankPath = null;
			IFileFilter testTreebankFilter = null;
			IList<string> unusedArgs = new List<string>();
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-lexparser"))
				{
					lexparserFile = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-testTreebank"))
					{
						Pair<string, IFileFilter> treebankDescription = ArgUtils.GetTreebankDescription(args, argIndex, "-testTreebank");
						argIndex = argIndex + ArgUtils.NumSubArgs(args, argIndex) + 1;
						testTreebankPath = treebankDescription.First();
						testTreebankFilter = treebankDescription.Second();
					}
					else
					{
						unusedArgs.Add(args[argIndex++]);
					}
				}
			}
			log.Info("Loading lexparser from: " + lexparserFile);
			string[] newArgs = Sharpen.Collections.ToArray(unusedArgs, new string[unusedArgs.Count]);
			LexicalizedParser lexparser = ((LexicalizedParser)LexicalizedParser.LoadModel(lexparserFile, newArgs));
			log.Info("... done");
			Treebank testTreebank = null;
			if (testTreebankPath != null)
			{
				log.Info("Reading in trees from " + testTreebankPath);
				if (testTreebankFilter != null)
				{
					log.Info("Filtering on " + testTreebankFilter);
				}
				testTreebank = lexparser.GetOp().tlpParams.MemoryTreebank();
				testTreebank.LoadPath(testTreebankPath, testTreebankFilter);
				log.Info("Read in " + testTreebank.Count + " trees for testing");
			}
			double[] labelResults = new double[weights.Length];
			double[] tagResults = new double[weights.Length];
			for (int i = 0; i < weights.Length; ++i)
			{
				lexparser.GetOp().baseParserWeight = weights[i];
				EvaluateTreebank evaluator = new EvaluateTreebank(lexparser);
				evaluator.TestOnTreebank(testTreebank);
				labelResults[i] = evaluator.GetLBScore();
				tagResults[i] = evaluator.GetTagScore();
			}
			for (int i_1 = 0; i_1 < weights.Length; ++i_1)
			{
				log.Info("LexicalizedParser weight " + weights[i_1] + ": labeled " + labelResults[i_1] + " tag " + tagResults[i_1]);
			}
		}
	}
}
