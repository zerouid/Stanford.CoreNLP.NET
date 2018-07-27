using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// This class is meant for training SVMs (
	/// <see cref="SVMLightClassifier{L, F}"/>
	/// s).  It actually calls SVM Light, or
	/// SVM Struct for multiclass SVMs, or SVM perf is the option is enabled, on the command line, reads in the produced
	/// model file and creates a Linear Classifier.  A Platt model is also trained
	/// (unless otherwise specified) on top of the SVM so that probabilities can
	/// be produced. For multiclass classifier, you have to set C using setC otherwise the code will not run (by sonalg).
	/// </summary>
	/// <author>Jenny Finkel</author>
	/// <author>Aria Haghighi</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (templatization)</author>
	[System.Serializable]
	public class SVMLightClassifierFactory<L, F> : IClassifierFactory<L, F, SVMLightClassifier<L, F>>
	{
		private const long serialVersionUID = 1L;

		/// <summary>C can be tuned using held-out set or cross-validation.</summary>
		/// <remarks>
		/// C can be tuned using held-out set or cross-validation.
		/// For binary SVM, if C=0, svmlight uses default of 1/(avg x*x).
		/// </remarks>
		protected internal double C = -1.0;

		private bool useSigmoid = false;

		protected internal bool verbose = true;

		private string svmLightLearn = "/u/nlp/packages/svm_light/svm_learn";

		private string svmStructLearn = "/u/nlp/packages/svm_multiclass/svm_multiclass_learn";

		private string svmPerfLearn = "/u/nlp/packages/svm_perf/svm_perf_learn";

		private string svmLightClassify = "/u/nlp/packages/svm_light/svm_classify";

		private string svmStructClassify = "/u/nlp/packages/svm_multiclass/svm_multiclass_classify";

		private string svmPerfClassify = "/u/nlp/packages/svm_perf/svm_perf_classify";

		private bool useAlphaFile = false;

		protected internal File alphaFile;

		private bool deleteTempFilesOnExit = true;

		private int svmLightVerbosity = 0;

		private bool doEval = false;

		private bool useSVMPerf = false;

		internal static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.SVMLightClassifierFactory));

		/// <param name="svmLightLearn">is the fullPathname of the training program of svmLight with default value "/u/nlp/packages/svm_light/svm_learn"</param>
		/// <param name="svmStructLearn">is the fullPathname of the training program of svmMultiClass with default value "/u/nlp/packages/svm_multiclass/svm_multiclass_learn"</param>
		/// <param name="svmPerfLearn">is the fullPathname of the training program of svmMultiClass with default value "/u/nlp/packages/svm_perf/svm_perf_learn"</param>
		public SVMLightClassifierFactory(string svmLightLearn, string svmStructLearn, string svmPerfLearn)
		{
			//extends AbstractLinearClassifierFactory {
			// not verbose
			this.svmLightLearn = svmLightLearn;
			this.svmStructLearn = svmStructLearn;
			this.svmPerfLearn = svmPerfLearn;
		}

		public SVMLightClassifierFactory()
		{
		}

		public SVMLightClassifierFactory(bool useSVMPerf)
		{
			this.useSVMPerf = useSVMPerf;
		}

		/// <summary>Set the C parameter (for the slack variables) for training the SVM.</summary>
		public virtual void SetC(double C)
		{
			this.C = C;
		}

		/// <summary>Get the C parameter (for the slack variables) for training the SVM.</summary>
		public virtual double GetC()
		{
			return C;
		}

		/// <summary>
		/// Specify whether or not to train an overlying platt (sigmoid)
		/// model for producing meaningful probabilities.
		/// </summary>
		public virtual void SetUseSigmoid(bool useSigmoid)
		{
			this.useSigmoid = useSigmoid;
		}

		/// <summary>
		/// Get whether or not to train an overlying platt (sigmoid)
		/// model for producing meaningful probabilities.
		/// </summary>
		public virtual bool GetUseSigma()
		{
			return useSigmoid;
		}

		public virtual bool GetDeleteTempFilesOnExitFlag()
		{
			return deleteTempFilesOnExit;
		}

		public virtual void SetDeleteTempFilesOnExitFlag(bool deleteTempFilesOnExit)
		{
			this.deleteTempFilesOnExit = deleteTempFilesOnExit;
		}

		/// <summary>Reads in a model file in svm light format.</summary>
		/// <remarks>
		/// Reads in a model file in svm light format.  It needs to know if its multiclass or not
		/// because it affects the number of header lines.  Maybe there is another way to tell and we
		/// can remove this flag?
		/// </remarks>
		private static Pair<double, ClassicCounter<int>> ReadModel(File modelFile, bool multiclass)
		{
			int modelLineCount = 0;
			try
			{
				int numLinesToSkip = multiclass ? 13 : 10;
				string stopToken = "#";
				BufferedReader @in = new BufferedReader(new FileReader(modelFile));
				for (int i = 0; i < numLinesToSkip; i++)
				{
					@in.ReadLine();
					modelLineCount++;
				}
				IList<Pair<double, ClassicCounter<int>>> supportVectors = new List<Pair<double, ClassicCounter<int>>>();
				// Read Threshold
				string thresholdLine = @in.ReadLine();
				modelLineCount++;
				string[] pieces = thresholdLine.Split("\\s+");
				double threshold = double.ParseDouble(pieces[0]);
				// Read Support Vectors
				while (@in.Ready())
				{
					string svLine = @in.ReadLine();
					modelLineCount++;
					pieces = svLine.Split("\\s+");
					// First Element is the alpha_i * y_i
					double alpha = double.ParseDouble(pieces[0]);
					ClassicCounter<int> supportVector = new ClassicCounter<int>();
					for (int i_1 = 1; i_1 < pieces.Length; ++i_1)
					{
						string piece = pieces[i_1];
						if (piece.Equals(stopToken))
						{
							break;
						}
						// Each in featureIndex:num class
						string[] indexNum = piece.Split(":");
						string featureIndex = indexNum[0];
						// mihai: we may see "qid" as indexNum[0]. just skip this piece. this is the block id useful only for reranking, which we don't do here.
						if (!featureIndex.Equals("qid"))
						{
							double count = double.ParseDouble(indexNum[1]);
							supportVector.IncrementCount(int.Parse(featureIndex), count);
						}
					}
					supportVectors.Add(new Pair<double, ClassicCounter<int>>(alpha, supportVector));
				}
				@in.Close();
				return new Pair<double, ClassicCounter<int>>(threshold, GetWeights(supportVectors));
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				throw new Exception("Error reading SVM model (line " + modelLineCount + " in file " + modelFile.GetAbsolutePath() + ")");
			}
		}

		/// <summary>
		/// Takes all the support vectors, and their corresponding alphas, and computes a weight
		/// vector that can be used in a vanilla LinearClassifier.
		/// </summary>
		/// <remarks>
		/// Takes all the support vectors, and their corresponding alphas, and computes a weight
		/// vector that can be used in a vanilla LinearClassifier.  This only works because
		/// we are using a linear kernel.  The Counter is over the feature indices (+1 cos for
		/// some reason svm_light is 1-indexed), not features.
		/// </remarks>
		private static ClassicCounter<int> GetWeights(IList<Pair<double, ClassicCounter<int>>> supportVectors)
		{
			ClassicCounter<int> weights = new ClassicCounter<int>();
			foreach (Pair<double, ClassicCounter<int>> sv in supportVectors)
			{
				ClassicCounter<int> c = new ClassicCounter<int>(sv.Second());
				Counters.MultiplyInPlace(c, sv.First());
				Counters.AddInPlace(weights, c);
			}
			return weights;
		}

		/// <summary>
		/// Converts the weight Counter to be from indexed, svm_light format, to a format
		/// we can use in our LinearClassifier.
		/// </summary>
		private ClassicCounter<Pair<F, L>> ConvertWeights(ClassicCounter<int> weights, IIndex<F> featureIndex, IIndex<L> labelIndex, bool multiclass)
		{
			return multiclass ? ConvertSVMStructWeights(weights, featureIndex, labelIndex) : ConvertSVMLightWeights(weights, featureIndex, labelIndex);
		}

		/// <summary>
		/// Converts the svm_light weight Counter (which uses feature indices) into a weight Counter
		/// using the actual features and labels.
		/// </summary>
		/// <remarks>
		/// Converts the svm_light weight Counter (which uses feature indices) into a weight Counter
		/// using the actual features and labels.  Because this is svm_light, and not svm_struct, the
		/// weights for the +1 class (which correspond to labelIndex.get(0)) and the -1 class
		/// (which correspond to labelIndex.get(1)) are just the negation of one another.
		/// </remarks>
		private ClassicCounter<Pair<F, L>> ConvertSVMLightWeights(ClassicCounter<int> weights, IIndex<F> featureIndex, IIndex<L> labelIndex)
		{
			ClassicCounter<Pair<F, L>> newWeights = new ClassicCounter<Pair<F, L>>();
			foreach (int i in weights.KeySet())
			{
				F f = featureIndex.Get(i - 1);
				double w = weights.GetCount(i);
				// the first guy in the labelIndex was the +1 class and the second guy
				// was the -1 class
				newWeights.IncrementCount(new Pair<F, L>(f, labelIndex.Get(0)), w);
				newWeights.IncrementCount(new Pair<F, L>(f, labelIndex.Get(1)), -w);
			}
			return newWeights;
		}

		/// <summary>
		/// Converts the svm_struct weight Counter (in which the weight for a feature/label pair
		/// correspondes to ((labelIndex * numFeatures)+(featureIndex+1))) into a weight Counter
		/// using the actual features and labels.
		/// </summary>
		private ClassicCounter<Pair<F, L>> ConvertSVMStructWeights(ClassicCounter<int> weights, IIndex<F> featureIndex, IIndex<L> labelIndex)
		{
			// int numLabels = labelIndex.size();
			int numFeatures = featureIndex.Size();
			ClassicCounter<Pair<F, L>> newWeights = new ClassicCounter<Pair<F, L>>();
			foreach (int i in weights.KeySet())
			{
				L l = labelIndex.Get((i - 1) / numFeatures);
				// integer division on purpose
				F f = featureIndex.Get((i - 1) % numFeatures);
				double w = weights.GetCount(i);
				newWeights.IncrementCount(new Pair<F, L>(f, l), w);
			}
			return newWeights;
		}

		/// <summary>Builds a sigmoid model to turn the classifier outputs into probabilities.</summary>
		private LinearClassifier<L, L> FitSigmoid(SVMLightClassifier<L, F> classifier, GeneralDataset<L, F> dataset)
		{
			RVFDataset<L, L> plattDataset = new RVFDataset<L, L>();
			for (int i = 0; i < dataset.Size(); i++)
			{
				RVFDatum<L, F> d = dataset.GetRVFDatum(i);
				ICounter<L> scores = classifier.ScoresOf((IDatum<L, F>)d);
				scores.IncrementCount(null);
				plattDataset.Add(new RVFDatum<L, L>(scores, d.Label()));
			}
			LinearClassifierFactory<L, L> factory = new LinearClassifierFactory<L, L>();
			factory.SetPrior(new LogPrior(LogPrior.LogPriorType.Null));
			return factory.TrainClassifier(plattDataset);
		}

		/// <summary>
		/// This method will cross validate on the given data and number of folds
		/// to find the optimal C.
		/// </summary>
		/// <remarks>
		/// This method will cross validate on the given data and number of folds
		/// to find the optimal C.  The scorer is how you determine what to
		/// optimize for (F-score, accuracy, etc).  The C is then saved, so that
		/// if you train a classifier after calling this method, that C will be used.
		/// </remarks>
		public virtual void CrossValidateSetC(GeneralDataset<L, F> dataset, int numFolds, IScorer<L> scorer, ILineSearcher minimizer)
		{
			System.Console.Out.WriteLine("in Cross Validate");
			useAlphaFile = true;
			bool oldUseSigmoid = useSigmoid;
			useSigmoid = false;
			CrossValidator<L, F> crossValidator = new CrossValidator<L, F>(dataset, numFolds);
			IToDoubleFunction<Triple<GeneralDataset<L, F>, GeneralDataset<L, F>, CrossValidator.SavedState>> score = null;
			//train(trainSet,true,true);
			IDoubleUnaryOperator negativeScorer = null;
			C = minimizer.Minimize(negativeScorer);
			useAlphaFile = false;
			useSigmoid = oldUseSigmoid;
		}

		public virtual void HeldOutSetC(GeneralDataset<L, F> train, double percentHeldOut, IScorer<L> scorer, ILineSearcher minimizer)
		{
			Pair<GeneralDataset<L, F>, GeneralDataset<L, F>> data = train.Split(percentHeldOut);
			HeldOutSetC(data.First(), data.Second(), scorer, minimizer);
		}

		/// <summary>
		/// This method will cross validate on the given data and number of folds
		/// to find the optimal C.
		/// </summary>
		/// <remarks>
		/// This method will cross validate on the given data and number of folds
		/// to find the optimal C.  The scorer is how you determine what to
		/// optimize for (F-score, accuracy, etc).  The C is then saved, so that
		/// if you train a classifier after calling this method, that C will be used.
		/// </remarks>
		public virtual void HeldOutSetC(GeneralDataset<L, F> trainSet, GeneralDataset<L, F> devSet, IScorer<L> scorer, ILineSearcher minimizer)
		{
			useAlphaFile = true;
			bool oldUseSigmoid = useSigmoid;
			useSigmoid = false;
			IDoubleUnaryOperator negativeScorer = null;
			C = minimizer.Minimize(negativeScorer);
			useAlphaFile = false;
			useSigmoid = oldUseSigmoid;
		}

		private bool tuneHeldOut = false;

		private bool tuneCV = false;

		private IScorer<L> scorer = new MultiClassAccuracyStats<L>();

		private ILineSearcher tuneMinimizer = new GoldenSectionLineSearch(true);

		private int folds;

		private double heldOutPercent;

		public virtual double GetHeldOutPercent()
		{
			return heldOutPercent;
		}

		public virtual void SetHeldOutPercent(double heldOutPercent)
		{
			this.heldOutPercent = heldOutPercent;
		}

		public virtual int GetFolds()
		{
			return folds;
		}

		public virtual void SetFolds(int folds)
		{
			this.folds = folds;
		}

		public virtual ILineSearcher GetTuneMinimizer()
		{
			return tuneMinimizer;
		}

		public virtual void SetTuneMinimizer(ILineSearcher minimizer)
		{
			this.tuneMinimizer = minimizer;
		}

		public virtual IScorer GetScorer()
		{
			return scorer;
		}

		public virtual void SetScorer(IScorer<L> scorer)
		{
			this.scorer = scorer;
		}

		public virtual bool GetTuneCV()
		{
			return tuneCV;
		}

		public virtual void SetTuneCV(bool tuneCV)
		{
			this.tuneCV = tuneCV;
		}

		public virtual bool GetTuneHeldOut()
		{
			return tuneHeldOut;
		}

		public virtual void SetTuneHeldOut(bool tuneHeldOut)
		{
			this.tuneHeldOut = tuneHeldOut;
		}

		public virtual int GetSvmLightVerbosity()
		{
			return svmLightVerbosity;
		}

		public virtual void SetSvmLightVerbosity(int svmLightVerbosity)
		{
			this.svmLightVerbosity = svmLightVerbosity;
		}

		public virtual SVMLightClassifier<L, F> TrainClassifier(GeneralDataset<L, F> dataset)
		{
			if (tuneHeldOut)
			{
				HeldOutSetC(dataset, heldOutPercent, scorer, tuneMinimizer);
			}
			else
			{
				if (tuneCV)
				{
					CrossValidateSetC(dataset, folds, scorer, tuneMinimizer);
				}
			}
			return TrainClassifierBasic(dataset);
		}

		internal Pattern whitespacePattern = Pattern.Compile("\\s+");

		public virtual SVMLightClassifier<L, F> TrainClassifierBasic(GeneralDataset<L, F> dataset)
		{
			IIndex<L> labelIndex = dataset.LabelIndex();
			IIndex<F> featureIndex = dataset.featureIndex;
			bool multiclass = (dataset.NumClasses() > 2);
			try
			{
				// this is the file that the model will be saved to
				File modelFile = File.CreateTempFile("svm-", ".model");
				if (deleteTempFilesOnExit)
				{
					modelFile.DeleteOnExit();
				}
				// this is the file that the svm light formated dataset
				// will be printed to
				File dataFile = File.CreateTempFile("svm-", ".data");
				if (deleteTempFilesOnExit)
				{
					dataFile.DeleteOnExit();
				}
				// print the dataset
				PrintWriter pw = new PrintWriter(new FileWriter(dataFile));
				dataset.PrintSVMLightFormat(pw);
				pw.Close();
				// -v 0 makes it not verbose
				// -m 400 gives it a larger cache, for faster training
				string cmd = (multiclass ? svmStructLearn : (useSVMPerf ? svmPerfLearn : svmLightLearn)) + " -v " + svmLightVerbosity + " -m 400 ";
				// set the value of C if we have one specified
				if (C > 0.0)
				{
					cmd = cmd + " -c " + C + " ";
				}
				else
				{
					// C value
					if (useSVMPerf)
					{
						cmd = cmd + " -c " + 0.01 + " ";
					}
				}
				//It's required to specify this parameter for SVM perf
				// Alpha File
				if (useAlphaFile)
				{
					File newAlphaFile = File.CreateTempFile("svm-", ".alphas");
					if (deleteTempFilesOnExit)
					{
						newAlphaFile.DeleteOnExit();
					}
					cmd = cmd + " -a " + newAlphaFile.GetAbsolutePath();
					if (alphaFile != null)
					{
						cmd = cmd + " -y " + alphaFile.GetAbsolutePath();
					}
					alphaFile = newAlphaFile;
				}
				// File and Model Data
				cmd = cmd + " " + dataFile.GetAbsolutePath() + " " + modelFile.GetAbsolutePath();
				if (verbose)
				{
					logger.Info("<< " + cmd + " >>");
				}
				/*Process p = Runtime.getRuntime().exec(cmd);
				
				p.waitFor();
				
				if (p.exitValue() != 0) throw new RuntimeException("Error Training SVM Light exit value: " + p.exitValue());
				p.destroy();   */
				SystemUtils.Run(new ProcessBuilder(whitespacePattern.Split(cmd)), new PrintWriter(System.Console.Error), new PrintWriter(System.Console.Error));
				if (doEval)
				{
					File predictFile = File.CreateTempFile("svm-", ".pred");
					if (deleteTempFilesOnExit)
					{
						predictFile.DeleteOnExit();
					}
					string evalCmd = (multiclass ? svmStructClassify : (useSVMPerf ? svmPerfClassify : svmLightClassify)) + " " + dataFile.GetAbsolutePath() + " " + modelFile.GetAbsolutePath() + " " + predictFile.GetAbsolutePath();
					if (verbose)
					{
						logger.Info("<< " + evalCmd + " >>");
					}
					SystemUtils.Run(new ProcessBuilder(whitespacePattern.Split(evalCmd)), new PrintWriter(System.Console.Error), new PrintWriter(System.Console.Error));
				}
				// read in the model file
				Pair<double, ClassicCounter<int>> weightsAndThresh = ReadModel(modelFile, multiclass);
				double threshold = weightsAndThresh.First();
				ClassicCounter<Pair<F, L>> weights = ConvertWeights(weightsAndThresh.Second(), featureIndex, labelIndex, multiclass);
				ClassicCounter<L> thresholds = new ClassicCounter<L>();
				if (!multiclass)
				{
					thresholds.SetCount(labelIndex.Get(0), -threshold);
					thresholds.SetCount(labelIndex.Get(1), threshold);
				}
				SVMLightClassifier<L, F> classifier = new SVMLightClassifier<L, F>(weights, thresholds);
				if (doEval)
				{
					File predictFile = File.CreateTempFile("svm-", ".pred2");
					if (deleteTempFilesOnExit)
					{
						predictFile.DeleteOnExit();
					}
					PrintWriter pw2 = new PrintWriter(predictFile);
					NumberFormat nf = NumberFormat.GetNumberInstance();
					nf.SetMaximumFractionDigits(5);
					foreach (IDatum<L, F> datum in dataset)
					{
						ICounter<L> scores = classifier.ScoresOf(datum);
						pw2.Println(Counters.ToString(scores, nf));
					}
					pw2.Close();
				}
				if (useSigmoid)
				{
					if (verbose)
					{
						System.Console.Out.Write("fitting sigmoid...");
					}
					classifier.SetPlatt(FitSigmoid(classifier, dataset));
					if (verbose)
					{
						System.Console.Out.WriteLine("done");
					}
				}
				return classifier;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}
	}
}
