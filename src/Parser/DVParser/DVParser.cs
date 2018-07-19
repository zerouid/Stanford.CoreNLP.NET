using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	/// <author>John Bauer &amp; Richard Socher</author>
	public class DVParser
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Dvparser.DVParser));

		internal DVModel dvModel;

		internal LexicalizedParser parser;

		internal Options op;

		public virtual Options GetOp()
		{
			return op;
		}

		internal virtual DVModel GetDVModel()
		{
			return dvModel;
		}

		private static readonly NumberFormat Nf = new DecimalFormat("0.00");

		private static readonly NumberFormat Filename = new DecimalFormat("0000");

		public static IList<Tree> GetTopParsesForOneTree(LexicalizedParser parser, int dvKBest, Tree tree, ITreeTransformer transformer)
		{
			IParserQuery pq = parser.ParserQuery();
			IList<Word> sentence = tree.YieldWords();
			// Since the trees are binarized and otherwise manipulated, we
			// need to chop off the last word in order to remove the end of
			// sentence symbol
			if (sentence.Count <= 1)
			{
				return null;
			}
			sentence = sentence.SubList(0, sentence.Count - 1);
			if (!pq.Parse(sentence))
			{
				log.Info("Failed to use the given parser to reparse sentence \"" + sentence + "\"");
				return null;
			}
			IList<Tree> parses = new List<Tree>();
			IList<ScoredObject<Tree>> bestKParses = pq.GetKBestPCFGParses(dvKBest);
			foreach (ScoredObject<Tree> so in bestKParses)
			{
				Tree result = so.Object();
				if (transformer != null)
				{
					result = transformer.TransformTree(result);
				}
				parses.Add(result);
			}
			return parses;
		}

		internal static IdentityHashMap<Tree, IList<Tree>> GetTopParses(LexicalizedParser parser, Options op, ICollection<Tree> trees, ITreeTransformer transformer, bool outputUpdates)
		{
			IdentityHashMap<Tree, IList<Tree>> topParses = new IdentityHashMap<Tree, IList<Tree>>();
			foreach (Tree tree in trees)
			{
				IList<Tree> parses = GetTopParsesForOneTree(parser, op.trainOptions.dvKBest, tree, transformer);
				topParses[tree] = parses;
				if (outputUpdates && topParses.Count % 10 == 0)
				{
					log.Info("Processed " + topParses.Count + " trees");
				}
			}
			if (outputUpdates)
			{
				log.Info("Finished processing " + topParses.Count + " trees");
			}
			return topParses;
		}

		internal virtual IdentityHashMap<Tree, IList<Tree>> GetTopParses(IList<Tree> trees, ITreeTransformer transformer)
		{
			return GetTopParses(parser, op, trees, transformer, false);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void Train(IList<Tree> sentences, IdentityHashMap<Tree, byte[]> compressedParses, Treebank testTreebank, string modelPath, string resultsRecordPath)
		{
			// process:
			//   we come up with a cost and a derivative for the model
			//   we always use the gold tree as the example to train towards
			//   every time through, we will look at the top N trees from
			//     the LexicalizedParser and pick the best one according to
			//     our model (at the start, this is essentially random)
			// we use QN to minimize the cost function for the model
			// to do this minimization, we turn all of the matrices in the
			//   DVModel into one big Theta, which is the set of variables to
			//   be optimized by the QN.
			Timing timing = new Timing();
			long maxTrainTimeMillis = op.trainOptions.maxTrainTimeSeconds * 1000;
			int batchCount = 0;
			int debugCycle = 0;
			double bestLabelF1 = 0.0;
			if (op.trainOptions.useContextWords)
			{
				foreach (Tree tree in sentences)
				{
					Edu.Stanford.Nlp.Trees.Trees.ConvertToCoreLabels(tree);
					tree.SetSpans();
				}
			}
			// for AdaGrad
			double[] sumGradSquare = new double[dvModel.TotalParamSize()];
			Arrays.Fill(sumGradSquare, 1.0);
			int numBatches = sentences.Count / op.trainOptions.batchSize + 1;
			log.Info("Training on " + sentences.Count + " trees in " + numBatches + " batches");
			log.Info("Times through each training batch: " + op.trainOptions.trainingIterations);
			log.Info("QN iterations per batch: " + op.trainOptions.qnIterationsPerBatch);
			for (int iter = 0; iter < op.trainOptions.trainingIterations; ++iter)
			{
				IList<Tree> shuffledSentences = new List<Tree>(sentences);
				Java.Util.Collections.Shuffle(shuffledSentences, dvModel.rand);
				for (int batch = 0; batch < numBatches; ++batch)
				{
					++batchCount;
					// This did not help performance
					//log.info("Setting AdaGrad's sum of squares to 1...");
					//Arrays.fill(sumGradSquare, 1.0);
					log.Info("======================================");
					log.Info("Iteration " + iter + " batch " + batch);
					// Each batch will be of the specified batch size, except the
					// last batch will include any leftover trees at the end of
					// the list
					int startTree = batch * op.trainOptions.batchSize;
					int endTree = (batch + 1) * op.trainOptions.batchSize;
					if (endTree > shuffledSentences.Count)
					{
						endTree = shuffledSentences.Count;
					}
					ExecuteOneTrainingBatch(shuffledSentences.SubList(startTree, endTree), compressedParses, sumGradSquare);
					long totalElapsed = timing.Report();
					log.Info("Finished iteration " + iter + " batch " + batch + "; total training time " + totalElapsed + " ms");
					if (maxTrainTimeMillis > 0 && totalElapsed > maxTrainTimeMillis)
					{
						// no need to debug output, we're done now
						break;
					}
					if (op.trainOptions.debugOutputFrequency > 0 && batchCount % op.trainOptions.debugOutputFrequency == 0)
					{
						log.Info("Finished " + batchCount + " total batches, running evaluation cycle");
						// Time for debugging output!
						double tagF1 = 0.0;
						double labelF1 = 0.0;
						if (testTreebank != null)
						{
							EvaluateTreebank evaluator = new EvaluateTreebank(AttachModelToLexicalizedParser());
							evaluator.TestOnTreebank(testTreebank);
							labelF1 = evaluator.GetLBScore();
							tagF1 = evaluator.GetTagScore();
							if (labelF1 > bestLabelF1)
							{
								bestLabelF1 = labelF1;
							}
							log.Info("Best label f1 on dev set so far: " + Nf.Format(bestLabelF1));
						}
						string tempName = null;
						if (modelPath != null)
						{
							tempName = modelPath;
							if (modelPath.EndsWith(".ser.gz"))
							{
								tempName = Sharpen.Runtime.Substring(modelPath, 0, modelPath.Length - 7) + "-" + Filename.Format(debugCycle) + "-" + Nf.Format(labelF1) + ".ser.gz";
							}
							SaveModel(tempName);
						}
						string statusLine = ("CHECKPOINT:" + " iteration " + iter + " batch " + batch + " labelF1 " + Nf.Format(labelF1) + " tagF1 " + Nf.Format(tagF1) + " bestLabelF1 " + Nf.Format(bestLabelF1) + " model " + tempName + op.trainOptions + " word vectors: "
							 + op.lexOptions.wordVectorFile + " numHid: " + op.lexOptions.numHid);
						log.Info(statusLine);
						if (resultsRecordPath != null)
						{
							FileWriter fout = new FileWriter(resultsRecordPath, true);
							// append
							fout.Write(statusLine);
							fout.Write("\n");
							fout.Close();
						}
						++debugCycle;
					}
				}
				long totalElapsed_1 = timing.Report();
				if (maxTrainTimeMillis > 0 && totalElapsed_1 > maxTrainTimeMillis)
				{
					// no need to debug output, we're done now
					log.Info("Max training time exceeded, exiting");
					break;
				}
			}
		}

		internal const int Minimizer = 3;

		public virtual void ExecuteOneTrainingBatch(IList<Tree> trainingBatch, IdentityHashMap<Tree, byte[]> compressedParses, double[] sumGradSquare)
		{
			Timing convertTiming = new Timing();
			convertTiming.Doing("Converting trees");
			IdentityHashMap<Tree, IList<Tree>> topParses = CacheParseHypotheses.ConvertToTrees(trainingBatch, compressedParses, op.trainOptions.trainingThreads);
			convertTiming.Done();
			DVParserCostAndGradient gcFunc = new DVParserCostAndGradient(trainingBatch, topParses, dvModel, op);
			double[] theta = dvModel.ParamsToVector();
			switch (Minimizer)
			{
				case (1):
				{
					//maxFuncIter = 10;
					// 1: QNMinimizer, 2: SGD
					QNMinimizer qn = new QNMinimizer(op.trainOptions.qnEstimates, true);
					qn.UseMinPackSearch();
					qn.UseDiagonalScaling();
					qn.TerminateOnAverageImprovement(true);
					qn.TerminateOnNumericalZero(true);
					qn.TerminateOnRelativeNorm(true);
					theta = qn.Minimize(gcFunc, op.trainOptions.qnTolerance, theta, op.trainOptions.qnIterationsPerBatch);
					break;
				}

				case 2:
				{
					//Minimizer smd = new SGDMinimizer();    	double tol = 1e-4;    	theta = smd.minimize(gcFunc,tol,theta,op.trainOptions.qnIterationsPerBatch);
					double lastCost = 0;
					double currCost = 0;
					bool firstTime = true;
					for (int i = 0; i < op.trainOptions.qnIterationsPerBatch; i++)
					{
						//gcFunc.calculate(theta);
						double[] grad = gcFunc.DerivativeAt(theta);
						currCost = gcFunc.ValueAt(theta);
						log.Info("batch cost: " + currCost);
						//    		if(!firstTime){
						//    			if(currCost > lastCost){
						//    				System.out.println("HOW IS FUNCTION VALUE INCREASING????!!! ... still updating theta");
						//    			}
						//    			if(Math.abs(currCost - lastCost) < 0.0001){
						//    				System.out.println("function value is not decreasing. stop");
						//    			}
						//    		}else{
						//    			firstTime = false;
						//    		}
						lastCost = currCost;
						ArrayMath.AddMultInPlace(theta, grad, -1 * op.trainOptions.learningRate);
					}
					break;
				}

				case 3:
				{
					// AdaGrad
					double eps = 1e-3;
					double currCost = 0;
					for (int i = 0; i < op.trainOptions.qnIterationsPerBatch; i++)
					{
						double[] gradf = gcFunc.DerivativeAt(theta);
						currCost = gcFunc.ValueAt(theta);
						log.Info("batch cost: " + currCost);
						for (int feature = 0; feature < gradf.Length; feature++)
						{
							sumGradSquare[feature] = sumGradSquare[feature] + gradf[feature] * gradf[feature];
							theta[feature] = theta[feature] - (op.trainOptions.learningRate * gradf[feature] / (System.Math.Sqrt(sumGradSquare[feature]) + eps));
						}
					}
					break;
				}

				default:
				{
					throw new ArgumentException("Unsupported minimizer " + Minimizer);
				}
			}
			dvModel.VectorToParams(theta);
		}

		public DVParser(DVModel model, LexicalizedParser parser)
		{
			this.parser = parser;
			this.op = parser.GetOp();
			this.dvModel = model;
		}

		public DVParser(LexicalizedParser parser)
		{
			this.parser = parser;
			this.op = parser.GetOp();
			if (op.trainOptions.randomSeed == 0)
			{
				op.trainOptions.randomSeed = Runtime.NanoTime();
				log.Info("Random seed not set, using randomly chosen seed of " + op.trainOptions.randomSeed);
			}
			else
			{
				log.Info("Random seed set to " + op.trainOptions.randomSeed);
			}
			log.Info("Word vector file: " + op.lexOptions.wordVectorFile);
			log.Info("Size of word vectors: " + op.lexOptions.numHid);
			log.Info("Number of hypothesis trees to train against: " + op.trainOptions.dvKBest);
			log.Info("Number of trees in one batch: " + op.trainOptions.batchSize);
			log.Info("Number of iterations of trees: " + op.trainOptions.trainingIterations);
			log.Info("Number of qn iterations per batch: " + op.trainOptions.qnIterationsPerBatch);
			log.Info("Learning rate: " + op.trainOptions.learningRate);
			log.Info("Delta margin: " + op.trainOptions.deltaMargin);
			log.Info("regCost: " + op.trainOptions.regCost);
			log.Info("Using unknown word vector for numbers: " + op.trainOptions.unknownNumberVector);
			log.Info("Using unknown dashed word vector heuristics: " + op.trainOptions.unknownDashedWordVectors);
			log.Info("Using unknown word vector for capitalized words: " + op.trainOptions.unknownCapsVector);
			log.Info("Using unknown number vector for Chinese words: " + op.trainOptions.unknownChineseNumberVector);
			log.Info("Using unknown year vector for Chinese words: " + op.trainOptions.unknownChineseYearVector);
			log.Info("Using unknown percent vector for Chinese words: " + op.trainOptions.unknownChinesePercentVector);
			log.Info("Initial matrices scaled by: " + op.trainOptions.scalingForInit);
			log.Info("Training will use " + op.trainOptions.trainingThreads + " thread(s)");
			log.Info("Context words are " + ((op.trainOptions.useContextWords) ? "on" : "off"));
			log.Info("Model will " + ((op.trainOptions.dvSimplifiedModel) ? string.Empty : "not ") + "be simplified");
			this.dvModel = new DVModel(op, parser.stateIndex, parser.ug, parser.bg);
			if (dvModel.unaryTransform.Count != dvModel.unaryScore.Count)
			{
				throw new AssertionError("Unary transform and score size not the same");
			}
			if (dvModel.binaryTransform.Size() != dvModel.binaryScore.Size())
			{
				throw new AssertionError("Binary transform and score size not the same");
			}
		}

		public virtual bool RunGradientCheck(IList<Tree> sentences, IdentityHashMap<Tree, byte[]> compressedParses)
		{
			log.Info("Gradient check: converting " + sentences.Count + " compressed trees");
			IdentityHashMap<Tree, IList<Tree>> topParses = CacheParseHypotheses.ConvertToTrees(sentences, compressedParses, op.trainOptions.trainingThreads);
			log.Info("Done converting trees");
			DVParserCostAndGradient gcFunc = new DVParserCostAndGradient(sentences, topParses, dvModel, op);
			return gcFunc.GradientCheck(1000, 50, dvModel.ParamsToVector());
		}

		public static ITreeTransformer BuildTrainTransformer(Options op)
		{
			CompositeTreeTransformer transformer = LexicalizedParser.BuildTrainTransformer(op);
			return transformer;
		}

		public virtual LexicalizedParser AttachModelToLexicalizedParser()
		{
			LexicalizedParser newParser = LexicalizedParser.CopyLexicalizedParser(parser);
			DVModelReranker reranker = new DVModelReranker(dvModel);
			newParser.reranker = reranker;
			return newParser;
		}

		public virtual void SaveModel(string filename)
		{
			log.Info("Saving serialized model to " + filename);
			LexicalizedParser newParser = AttachModelToLexicalizedParser();
			newParser.SaveParserToSerialized(filename);
			log.Info("... done");
		}

		public static Edu.Stanford.Nlp.Parser.Dvparser.DVParser LoadModel(string filename, string[] args)
		{
			log.Info("Loading serialized model from " + filename);
			Edu.Stanford.Nlp.Parser.Dvparser.DVParser dvparser;
			try
			{
				dvparser = IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(filename);
				dvparser.op.SetOptions(args);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			catch (TypeLoadException e)
			{
				throw new RuntimeIOException(e);
			}
			log.Info("... done");
			return dvparser;
		}

		public static DVModel GetModelFromLexicalizedParser(LexicalizedParser parser)
		{
			if (!(parser.reranker is DVModelReranker))
			{
				throw new ArgumentException("This parser does not contain a DVModel reranker");
			}
			DVModelReranker reranker = (DVModelReranker)parser.reranker;
			return reranker.GetModel();
		}

		public static void Help()
		{
			log.Info("Options supplied by this file:");
			log.Info("  -model <name>: When training, the name of the model to save.  Otherwise, the name of the model to load.");
			log.Info("  -parser <name>: When training, the LexicalizedParser to use as the base model.");
			log.Info("  -cachedTrees <name>: The name of the file containing a treebank with cached parses.  See CacheParseHypotheses.java");
			log.Info("  -treebank <name> [filter]: A treebank to use instead of cachedTrees.  Trees will be reparsed.  Slow.");
			log.Info("  -testTreebank <name> [filter]: A treebank for testing the model.");
			log.Info("  -train: Run training over the treebank, testing on the testTreebank.");
			log.Info("  -continueTraining <name>: The name of a file to continue training.");
			log.Info("  -nofilter: Rules for the parser will not be filtered based on the training treebank.");
			log.Info("  -runGradientCheck: Run a gradient check.");
			log.Info("  -resultsRecord: A file for recording info on intermediate results");
			log.Info();
			log.Info("Options overlapping the parser:");
			log.Info("  -trainingThreads <int>: How many threads to use when training.");
			log.Info("  -dvKBest <int>: How many hypotheses to use from the underlying parser.");
			log.Info("  -trainingIterations <int>: When training, how many times to go through the train set.");
			log.Info("  -regCost <double>: How large of a cost to put on regularization.");
			log.Info("  -batchSize <int>: How many trees to use in each batch of the training.");
			log.Info("  -qnIterationsPerBatch <int>: How many steps to take per batch.");
			log.Info("  -qnEstimates <int>: Parameter for qn optimization.");
			log.Info("  -qnTolerance <double>: Tolerance for early exit when optimizing a batch.");
			log.Info("  -debugOutputFrequency <int>: How frequently to score a model when training and write out intermediate models.");
			log.Info("  -maxTrainTimeSeconds <int>: How long to train before terminating.");
			log.Info("  -randomSeed <long>: A starting point for the random number generator.  Setting this should lead to repeatable results, even taking into account randomness.  Otherwise, a new random seed will be picked.");
			log.Info("  -wordVectorFile <name>: A filename to load word vectors from.");
			log.Info("  -numHid: The size of the matrices.  In most circumstances, should be set to the size of the word vectors.");
			log.Info("  -learningRate: The rate of optimization when training");
			log.Info("  -deltaMargin: How much we punish trees for being incorrect when training");
			log.Info("  -(no)unknownNumberVector: Whether or not to use a word vector for unknown numbers");
			log.Info("  -(no)unknownDashedWordVectors: Whether or not to split unknown dashed words");
			log.Info("  -(no)unknownCapsVector: Whether or not to use a word vector for unknown words with capitals");
			log.Info("  -dvSimplifiedModel: Use a greatly dumbed down DVModel");
			log.Info("  -scalingForInit: How much to scale matrices when creating a new DVModel");
			log.Info("  -baseParserWeight: A weight to give the original LexicalizedParser when testing (0.2 seems to work well for English)");
			log.Info("  -unkWord: The vector representing unknown word in the word vectors file");
			log.Info("  -transformMatrixType: A couple different methods for initializing transform matrices");
			log.Info("  -(no)trainWordVectors: whether or not to train the word vectors along with the matrices.  True by default");
		}

		/// <summary>
		/// An example command line for training a new parser:
		/// <br />
		/// nohup java -mx6g edu.stanford.nlp.parser.dvparser.DVParser -cachedTrees /scr/nlp/data/dvparser/wsj/cached.wsj.train.simple.ser.gz -train -testTreebank  /afs/ir/data/linguistic-data/Treebank/3/parsed/mrg/wsj/22 2200-2219 -debugOutputFrequency 400 -nofilter -trainingThreads 5 -parser /u/nlp/data/lexparser/wsjPCFG.nocompact.simple.ser.gz -trainingIterations 40 -batchSize 25 -model /scr/nlp/data/dvparser/wsj/wsj.combine.v2.ser.gz -unkWord "*UNK*" -dvCombineCategories &gt; /scr/nlp/data/dvparser/wsj/wsj.combine.v2.out 2&gt;&amp;1 &amp;
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Help();
				System.Environment.Exit(2);
			}
			log.Info("Running DVParser with arguments:");
			foreach (string arg in args)
			{
				log.Info("  " + arg);
			}
			log.Info();
			string parserPath = null;
			string trainTreebankPath = null;
			IFileFilter trainTreebankFilter = null;
			string cachedTrainTreesPath = null;
			bool runGradientCheck = false;
			bool runTraining = false;
			string testTreebankPath = null;
			IFileFilter testTreebankFilter = null;
			string initialModelPath = null;
			string modelPath = null;
			bool filter = true;
			string resultsRecordPath = null;
			IList<string> unusedArgs = new List<string>();
			// These parameters can be null or 0 if the model was not
			// serialized with the new parameters.  Setting the options at the
			// command line will override these defaults.
			// TODO: if/when we integrate back into the main branch and
			// rebuild models, we can get rid of this
			IList<string> argsWithDefaults = new List<string>(Arrays.AsList(new string[] { "-wordVectorFile", Options.LexOptions.DefaultWordVectorFile, "-dvKBest", int.ToString(TrainOptions.DefaultKBest), "-batchSize", int.ToString(TrainOptions.DefaultBatchSize
				), "-trainingIterations", int.ToString(TrainOptions.DefaultTrainingIterations), "-qnIterationsPerBatch", int.ToString(TrainOptions.DefaultQnIterationsPerBatch), "-regCost", double.ToString(TrainOptions.DefaultRegcost), "-learningRate", double
				.ToString(TrainOptions.DefaultLearningRate), "-deltaMargin", double.ToString(TrainOptions.DefaultDeltaMargin), "-unknownNumberVector", "-unknownDashedWordVectors", "-unknownCapsVector", "-unknownchinesepercentvector", "-unknownchinesenumbervector"
				, "-unknownchineseyearvector", "-unkWord", "*UNK*", "-transformMatrixType", "DIAGONAL", "-scalingForInit", double.ToString(TrainOptions.DefaultScalingForInit), "-trainWordVectors" }));
			Sharpen.Collections.AddAll(argsWithDefaults, Arrays.AsList(args));
			args = Sharpen.Collections.ToArray(argsWithDefaults, new string[argsWithDefaults.Count]);
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-parser"))
				{
					parserPath = args[argIndex + 1];
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
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-treebank"))
						{
							Pair<string, IFileFilter> treebankDescription = ArgUtils.GetTreebankDescription(args, argIndex, "-treebank");
							argIndex = argIndex + ArgUtils.NumSubArgs(args, argIndex) + 1;
							trainTreebankPath = treebankDescription.First();
							trainTreebankFilter = treebankDescription.Second();
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-cachedTrees"))
							{
								cachedTrainTreesPath = args[argIndex + 1];
								argIndex += 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-runGradientCheck"))
								{
									runGradientCheck = true;
									argIndex++;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-train"))
									{
										runTraining = true;
										argIndex++;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-model"))
										{
											modelPath = args[argIndex + 1];
											argIndex += 2;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-nofilter"))
											{
												filter = false;
												argIndex++;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-continueTraining"))
												{
													runTraining = true;
													filter = false;
													initialModelPath = args[argIndex + 1];
													argIndex += 2;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-resultsRecord"))
													{
														resultsRecordPath = args[argIndex + 1];
														argIndex += 2;
													}
													else
													{
														unusedArgs.Add(args[argIndex++]);
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			if (parserPath == null && modelPath == null)
			{
				throw new ArgumentException("Must supply either a base parser model with -parser or a serialized DVParser with -model");
			}
			if (!runTraining && modelPath == null && !runGradientCheck)
			{
				throw new ArgumentException("Need to either train a new model, run the gradient check or specify a model to load with -model");
			}
			string[] newArgs = Sharpen.Collections.ToArray(unusedArgs, new string[unusedArgs.Count]);
			Edu.Stanford.Nlp.Parser.Dvparser.DVParser dvparser = null;
			LexicalizedParser lexparser = null;
			if (initialModelPath != null)
			{
				lexparser = ((LexicalizedParser)LexicalizedParser.LoadModel(initialModelPath, newArgs));
				DVModel model = GetModelFromLexicalizedParser(lexparser);
				dvparser = new Edu.Stanford.Nlp.Parser.Dvparser.DVParser(model, lexparser);
			}
			else
			{
				if (runTraining || runGradientCheck)
				{
					lexparser = ((LexicalizedParser)LexicalizedParser.LoadModel(parserPath, newArgs));
					dvparser = new Edu.Stanford.Nlp.Parser.Dvparser.DVParser(lexparser);
				}
				else
				{
					if (modelPath != null)
					{
						lexparser = ((LexicalizedParser)LexicalizedParser.LoadModel(modelPath, newArgs));
						DVModel model = GetModelFromLexicalizedParser(lexparser);
						dvparser = new Edu.Stanford.Nlp.Parser.Dvparser.DVParser(model, lexparser);
					}
				}
			}
			IList<Tree> trainSentences = new List<Tree>();
			IdentityHashMap<Tree, byte[]> trainCompressedParses = Generics.NewIdentityHashMap();
			if (cachedTrainTreesPath != null)
			{
				foreach (string path in cachedTrainTreesPath.Split(","))
				{
					IList<Pair<Tree, byte[]>> cache = IOUtils.ReadObjectFromFile(path);
					foreach (Pair<Tree, byte[]> pair in cache)
					{
						trainSentences.Add(pair.First());
						trainCompressedParses[pair.First()] = pair.Second();
					}
					log.Info("Read in " + cache.Count + " trees from " + path);
				}
			}
			if (trainTreebankPath != null)
			{
				// TODO: make the transformer a member of the model?
				ITreeTransformer transformer = BuildTrainTransformer(dvparser.GetOp());
				Treebank treebank = dvparser.GetOp().tlpParams.MemoryTreebank();
				treebank.LoadPath(trainTreebankPath, trainTreebankFilter);
				treebank = treebank.Transform(transformer);
				log.Info("Read in " + treebank.Count + " trees from " + trainTreebankPath);
				CacheParseHypotheses cacher = new CacheParseHypotheses(dvparser.parser);
				CacheParseHypotheses.CacheProcessor processor = new CacheParseHypotheses.CacheProcessor(cacher, lexparser, dvparser.op.trainOptions.dvKBest, transformer);
				foreach (Tree tree in treebank)
				{
					trainSentences.Add(tree);
					trainCompressedParses[tree] = processor.Process(tree).second;
				}
				//System.out.println(tree);
				log.Info("Finished parsing " + treebank.Count + " trees, getting " + dvparser.op.trainOptions.dvKBest + " hypotheses each");
			}
			if ((runTraining || runGradientCheck) && filter)
			{
				log.Info("Filtering rules for the given training set");
				dvparser.dvModel.SetRulesForTrainingSet(trainSentences, trainCompressedParses);
				log.Info("Done filtering rules; " + dvparser.dvModel.numBinaryMatrices + " binary matrices, " + dvparser.dvModel.numUnaryMatrices + " unary matrices, " + dvparser.dvModel.wordVectors.Count + " word vectors");
			}
			//dvparser.dvModel.printAllMatrices();
			Treebank testTreebank = null;
			if (testTreebankPath != null)
			{
				log.Info("Reading in trees from " + testTreebankPath);
				if (testTreebankFilter != null)
				{
					log.Info("Filtering on " + testTreebankFilter);
				}
				testTreebank = dvparser.GetOp().tlpParams.MemoryTreebank();
				testTreebank.LoadPath(testTreebankPath, testTreebankFilter);
				log.Info("Read in " + testTreebank.Count + " trees for testing");
			}
			//    runGradientCheck= true;
			if (runGradientCheck)
			{
				log.Info("Running gradient check on " + trainSentences.Count + " trees");
				dvparser.RunGradientCheck(trainSentences, trainCompressedParses);
			}
			if (runTraining)
			{
				log.Info("Training the RNN parser");
				log.Info("Current train options: " + dvparser.GetOp().trainOptions);
				dvparser.Train(trainSentences, trainCompressedParses, testTreebank, modelPath, resultsRecordPath);
				if (modelPath != null)
				{
					dvparser.SaveModel(modelPath);
				}
			}
			if (testTreebankPath != null)
			{
				EvaluateTreebank evaluator = new EvaluateTreebank(dvparser.AttachModelToLexicalizedParser());
				evaluator.TestOnTreebank(testTreebank);
			}
			log.Info("Successfully ran DVParser");
		}
	}
}
