using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Sentiment
{
	public class SentimentTraining
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sentiment.SentimentTraining));

		private static readonly NumberFormat Nf = new DecimalFormat("0.00");

		private static readonly NumberFormat Filename = new DecimalFormat("0000");

		private SentimentTraining()
		{
		}

		// static methods
		private static void ExecuteOneTrainingBatch(SentimentModel model, IList<Tree> trainingBatch, double[] sumGradSquare)
		{
			SentimentCostAndGradient gcFunc = new SentimentCostAndGradient(model, trainingBatch);
			double[] theta = model.ParamsToVector();
			// AdaGrad
			double eps = 1e-3;
			// TODO: do we want to iterate multiple times per batch?
			double[] gradf = gcFunc.DerivativeAt(theta);
			double currCost = gcFunc.ValueAt(theta);
			log.Info("batch cost: " + currCost);
			for (int feature = 0; feature < gradf.Length; feature++)
			{
				sumGradSquare[feature] = sumGradSquare[feature] + gradf[feature] * gradf[feature];
				theta[feature] = theta[feature] - (model.op.trainOptions.learningRate * gradf[feature] / (Math.Sqrt(sumGradSquare[feature]) + eps));
			}
			model.VectorToParams(theta);
		}

		public static void Train(SentimentModel model, string modelPath, IList<Tree> trainingTrees, IList<Tree> devTrees)
		{
			Timing timing = new Timing();
			long maxTrainTimeMillis = model.op.trainOptions.maxTrainTimeSeconds * 1000;
			int debugCycle = 0;
			// double bestAccuracy = 0.0;
			// train using AdaGrad (seemed to work best during the dvparser project)
			double[] sumGradSquare = new double[model.TotalParamSize()];
			Arrays.Fill(sumGradSquare, model.op.trainOptions.initialAdagradWeight);
			int numBatches = trainingTrees.Count / model.op.trainOptions.batchSize + 1;
			log.Info("Training on " + trainingTrees.Count + " trees in " + numBatches + " batches");
			log.Info("Times through each training batch: " + model.op.trainOptions.epochs);
			for (int epoch = 0; epoch < model.op.trainOptions.epochs; ++epoch)
			{
				log.Info("======================================");
				log.Info("Starting epoch " + epoch);
				if (epoch > 0 && model.op.trainOptions.adagradResetFrequency > 0 && (epoch % model.op.trainOptions.adagradResetFrequency == 0))
				{
					log.Info("Resetting adagrad weights to " + model.op.trainOptions.initialAdagradWeight);
					Arrays.Fill(sumGradSquare, model.op.trainOptions.initialAdagradWeight);
				}
				IList<Tree> shuffledSentences = Generics.NewArrayList(trainingTrees);
				if (model.op.trainOptions.shuffleMatrices)
				{
					Java.Util.Collections.Shuffle(shuffledSentences, model.rand);
				}
				for (int batch = 0; batch < numBatches; ++batch)
				{
					log.Info("======================================");
					log.Info("Epoch " + epoch + " batch " + batch);
					// Each batch will be of the specified batch size, except the
					// last batch will include any leftover trees at the end of
					// the list
					int startTree = batch * model.op.trainOptions.batchSize;
					int endTree = (batch + 1) * model.op.trainOptions.batchSize;
					if (endTree > shuffledSentences.Count)
					{
						endTree = shuffledSentences.Count;
					}
					ExecuteOneTrainingBatch(model, shuffledSentences.SubList(startTree, endTree), sumGradSquare);
					long totalElapsed = timing.Report();
					log.Info("Finished epoch " + epoch + " batch " + batch + "; total training time " + totalElapsed + " ms");
					if (maxTrainTimeMillis > 0 && totalElapsed > maxTrainTimeMillis)
					{
						// no need to debug output, we're done now
						break;
					}
					if (batch == (numBatches - 1) && model.op.trainOptions.debugOutputEpochs > 0 && (epoch + 1) % model.op.trainOptions.debugOutputEpochs == 0)
					{
						double score = 0.0;
						if (devTrees != null)
						{
							Evaluate eval = new Evaluate(model);
							eval.Eval(devTrees);
							eval.PrintSummary();
							score = eval.ExactNodeAccuracy() * 100.0;
						}
						// output an intermediate model
						if (modelPath != null)
						{
							string tempPath;
							if (modelPath.EndsWith(".ser.gz"))
							{
								tempPath = Sharpen.Runtime.Substring(modelPath, 0, modelPath.Length - 7) + "-" + Filename.Format(debugCycle) + "-" + Nf.Format(score) + ".ser.gz";
							}
							else
							{
								if (modelPath.EndsWith(".gz"))
								{
									tempPath = Sharpen.Runtime.Substring(modelPath, 0, modelPath.Length - 3) + "-" + Filename.Format(debugCycle) + "-" + Nf.Format(score) + ".gz";
								}
								else
								{
									tempPath = Sharpen.Runtime.Substring(modelPath, 0, modelPath.Length - 3) + "-" + Filename.Format(debugCycle) + "-" + Nf.Format(score);
								}
							}
							model.SaveSerialized(tempPath);
						}
						++debugCycle;
					}
				}
				long totalElapsed_1 = timing.Report();
				if (maxTrainTimeMillis > 0 && totalElapsed_1 > maxTrainTimeMillis)
				{
					log.Info("Max training time exceeded, exiting");
					break;
				}
			}
		}

		public static bool RunGradientCheck(SentimentModel model, IList<Tree> trees)
		{
			SentimentCostAndGradient gcFunc = new SentimentCostAndGradient(model, trees);
			return gcFunc.GradientCheck(model.TotalParamSize(), 50, model.ParamsToVector());
		}

		/// <summary>Trains a sentiment model.</summary>
		/// <remarks>
		/// Trains a sentiment model.
		/// The -trainPath argument points to a labeled sentiment treebank.
		/// The trees in this data will be used to train the model parameters (also to seed the model vocabulary).
		/// The -devPath argument points to a second labeled sentiment treebank.
		/// The trees in this data will be used to periodically evaluate the performance of the model.
		/// We won't train on this data; it will only be used to test how well the model generalizes to unseen data.
		/// The -model argument specifies where to save the learned sentiment model.
		/// </remarks>
		/// <param name="args">Command line arguments</param>
		public static void Main(string[] args)
		{
			RNNOptions op = new RNNOptions();
			string trainPath = "sentimentTreesDebug.txt";
			string devPath = null;
			bool runGradientCheck = false;
			bool runTraining = false;
			bool filterUnknown = false;
			string modelPath = null;
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-train"))
				{
					runTraining = true;
					argIndex++;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-gradientcheck"))
					{
						runGradientCheck = true;
						argIndex++;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-trainpath"))
						{
							trainPath = args[argIndex + 1];
							argIndex += 2;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-devpath"))
							{
								devPath = args[argIndex + 1];
								argIndex += 2;
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
									if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-filterUnknown"))
									{
										filterUnknown = true;
										argIndex++;
									}
									else
									{
										int newArgIndex = op.SetOption(args, argIndex);
										if (newArgIndex == argIndex)
										{
											throw new ArgumentException("Unknown argument " + args[argIndex]);
										}
										argIndex = newArgIndex;
									}
								}
							}
						}
					}
				}
			}
			// read in the trees
			IList<Tree> trainingTrees = SentimentUtils.ReadTreesWithGoldLabels(trainPath);
			log.Info("Read in " + trainingTrees.Count + " training trees");
			if (filterUnknown)
			{
				trainingTrees = SentimentUtils.FilterUnknownRoots(trainingTrees);
				log.Info("Filtered training trees: " + trainingTrees.Count);
			}
			IList<Tree> devTrees = null;
			if (devPath != null)
			{
				devTrees = SentimentUtils.ReadTreesWithGoldLabels(devPath);
				log.Info("Read in " + devTrees.Count + " dev trees");
				if (filterUnknown)
				{
					devTrees = SentimentUtils.FilterUnknownRoots(devTrees);
					log.Info("Filtered dev trees: " + devTrees.Count);
				}
			}
			// TODO: binarize the trees, then collapse the unary chains.
			// Collapsed unary chains always have the label of the top node in
			// the chain
			// Note: the sentiment training data already has this done.
			// However, when we handle trees given to us from the Stanford Parser,
			// we will have to perform this step
			// build an uninitialized SentimentModel from the binary productions
			log.Info("Sentiment model options:\n" + op);
			SentimentModel model = new SentimentModel(op, trainingTrees);
			if (op.trainOptions.initialMatrixLogPath != null)
			{
				StringUtils.PrintToFile(new File(op.trainOptions.initialMatrixLogPath), model.ToString(), false, false, "utf-8");
			}
			// TODO: need to handle unk rules somehow... at test time the tree
			// structures might have something that we never saw at training
			// time.  for example, we could put a threshold on all of the
			// rules at training time and anything that doesn't meet that
			// threshold goes into the unk.  perhaps we could also use some
			// component of the accepted training rules to build up the "unk"
			// parameter in case there are no rules that don't meet the
			// threshold
			if (runGradientCheck)
			{
				RunGradientCheck(model, trainingTrees);
			}
			if (runTraining)
			{
				Train(model, modelPath, trainingTrees, devTrees);
				model.SaveSerialized(modelPath);
			}
		}
	}
}
