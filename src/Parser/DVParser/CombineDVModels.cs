using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	/// <summary>
	/// This class combines multiple DVParsers into one by adding their
	/// scores.
	/// </summary>
	/// <remarks>
	/// This class combines multiple DVParsers into one by adding their
	/// scores.  If the models are somewhat different but have similar
	/// accuracy, this gives a slight accuracy increase.
	/// </remarks>
	/// <author>John Bauer</author>
	public class CombineDVModels
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(CombineDVModels));

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			string modelPath = null;
			IList<string> baseModelPaths = null;
			string testTreebankPath = null;
			IFileFilter testTreebankFilter = null;
			IList<string> unusedArgs = new List<string>();
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-model"))
				{
					modelPath = args[argIndex + 1];
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
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-baseModels"))
						{
							argIndex++;
							baseModelPaths = new List<string>();
							while (argIndex < args.Length && args[argIndex][0] != '-')
							{
								baseModelPaths.Add(args[argIndex++]);
							}
							if (baseModelPaths.Count == 0)
							{
								throw new ArgumentException("Found an argument -baseModels with no actual models named");
							}
						}
						else
						{
							unusedArgs.Add(args[argIndex++]);
						}
					}
				}
			}
			string[] newArgs = Sharpen.Collections.ToArray(unusedArgs, new string[unusedArgs.Count]);
			LexicalizedParser underlyingParser = null;
			Options options = null;
			LexicalizedParser combinedParser = null;
			if (baseModelPaths != null)
			{
				IList<DVModel> dvparsers = new List<DVModel>();
				foreach (string baseModelPath in baseModelPaths)
				{
					log.Info("Loading serialized DVParser from " + baseModelPath);
					LexicalizedParser dvparser = ((LexicalizedParser)LexicalizedParser.LoadModel(baseModelPath));
					IReranker reranker = dvparser.reranker;
					if (!(reranker is DVModelReranker))
					{
						throw new ArgumentException("Expected parsers with DVModel embedded");
					}
					dvparsers.Add(((DVModelReranker)reranker).GetModel());
					if (underlyingParser == null)
					{
						underlyingParser = dvparser;
						options = underlyingParser.GetOp();
						// TODO: other parser's options?
						options.SetOptions(newArgs);
					}
					log.Info("... done");
				}
				combinedParser = LexicalizedParser.CopyLexicalizedParser(underlyingParser);
				CombinedDVModelReranker reranker_1 = new CombinedDVModelReranker(options, dvparsers);
				combinedParser.reranker = reranker_1;
				combinedParser.SaveParserToSerialized(modelPath);
			}
			else
			{
				throw new ArgumentException("Need to specify -model to load an already prepared CombinedParser");
			}
			Treebank testTreebank = null;
			if (testTreebankPath != null)
			{
				log.Info("Reading in trees from " + testTreebankPath);
				if (testTreebankFilter != null)
				{
					log.Info("Filtering on " + testTreebankFilter);
				}
				testTreebank = combinedParser.GetOp().tlpParams.MemoryTreebank();
				testTreebank.LoadPath(testTreebankPath, testTreebankFilter);
				log.Info("Read in " + testTreebank.Count + " trees for testing");
				EvaluateTreebank evaluator = new EvaluateTreebank(combinedParser.GetOp(), null, combinedParser);
				evaluator.TestOnTreebank(testTreebank);
			}
		}
	}
}
