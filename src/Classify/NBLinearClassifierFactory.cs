using System;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// Provides a medium-weight implementation of Bernoulli (or binary)
	/// Naive Bayes via a linear classifier.
	/// </summary>
	/// <remarks>
	/// Provides a medium-weight implementation of Bernoulli (or binary)
	/// Naive Bayes via a linear classifier.  It's medium weight in that
	/// it uses dense arrays for counts and calculation (but, hey, NB is
	/// efficient to estimate).  Each feature is treated as an independent
	/// binary variable.
	/// <p>
	/// CDM Jun 2003: I added a dirty trick so that if there is a feature
	/// that is always on in input examples, then its weight is turned into
	/// a prior feature!  (This will work well iff it is also always on at
	/// test time.)  In fact, this is done for each such feature, so by
	/// having several such features, one can even get an integral prior
	/// boost out of this.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public class NBLinearClassifierFactory<L, F> : AbstractLinearClassifierFactory<L, F>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.NBLinearClassifierFactory));

		private const bool Verbose = false;

		private double sigma;

		private readonly bool interpretAlwaysOnFeatureAsPrior;

		private const double epsilon = 1e-30;

		private bool tuneSigma = false;

		private int folds;

		// amount of add-k smoothing of evidence
		// fudge to keep nonzero
		protected internal override double[][] TrainWeights(GeneralDataset<L, F> data)
		{
			return TrainWeights(data.GetDataArray(), data.GetLabelsArray());
		}

		/// <summary>Train weights.</summary>
		/// <remarks>
		/// Train weights.
		/// If tuneSigma is true, the optimal sigma value is found using cross-validation:
		/// the number of folds is determined by the
		/// <c>folds</c>
		/// variable,
		/// if there are less training examples than folds,
		/// leave-one-out is used.
		/// </remarks>
		internal virtual double[][] TrainWeights(int[][] data, int[] labels)
		{
			if (tuneSigma)
			{
				TuneSigma(data, labels);
			}
			int numFeatures = NumFeatures();
			int numClasses = NumClasses();
			double[][] weights = new double[numFeatures][];
			// find P(C|F)/P(C)
			int num = 0;
			double[] numc = new double[numClasses];
			double n = 0;
			// num active features in whole dataset
			double[] n_c = new double[numClasses];
			// num active features in class c items
			double[] n_f = new double[numFeatures];
			// num data items for which feature is active
			double[][] n_fc = new double[numFeatures][];
			// num times feature active in class c
			for (int d = 0; d < data.Length; d++)
			{
				num++;
				numc[labels[d]]++;
				for (int i = 0; i < data[d].Length; i++)
				{
					n++;
					n_c[labels[d]]++;
					n_f[data[d][i]]++;
					n_fc[data[d][i]][labels[d]]++;
				}
			}
			for (int c = 0; c < numClasses; c++)
			{
				for (int f = 0; f < numFeatures; f++)
				{
					if (interpretAlwaysOnFeatureAsPrior && n_f[f] == data.Length)
					{
						// interpret always on feature as prior!
						weights[f][c] = Math.Log(numc[c] / num);
					}
					else
					{
						// p_c_f = (N(f,c)+k)/(N(f)+|C|k) = Paddk(c|f)
						// set lambda = log (P()/P())
						double p_c = (n_c[c] + epsilon) / (n + numClasses * epsilon);
						double p_c_f = (n_fc[f][c] + sigma) / (n_f[f] + sigma * numClasses);
						weights[f][c] = Math.Log(p_c_f / p_c);
					}
				}
			}
			return weights;
		}

		internal virtual double[][] Weights(int[][] data, int[] labels, int testMin, int testMax, double trialSigma, int foldSize)
		{
			int numFeatures = NumFeatures();
			int numClasses = NumClasses();
			double[][] weights = new double[numFeatures][];
			// find P(C|F)/P(C)
			int num = 0;
			double[] numc = new double[numClasses];
			double n = 0;
			// num active features in whole dataset
			double[] n_c = new double[numClasses];
			// num active features in class c items
			double[] n_f = new double[numFeatures];
			// num data items for which feature is active
			double[][] n_fc = new double[numFeatures][];
			// num times feature active in class c
			for (int d = 0; d < data.Length; d++)
			{
				if (d == testMin)
				{
					d = testMax - 1;
					continue;
				}
				num++;
				numc[labels[d]]++;
				for (int i = 0; i < data[d].Length; i++)
				{
					if (i == testMin)
					{
						i = testMax - 1;
						continue;
					}
					n++;
					n_c[labels[d]]++;
					n_f[data[d][i]]++;
					n_fc[data[d][i]][labels[d]]++;
				}
			}
			for (int c = 0; c < numClasses; c++)
			{
				for (int f = 0; f < numFeatures; f++)
				{
					if (interpretAlwaysOnFeatureAsPrior && n_f[f] == data.Length - foldSize)
					{
						// interpret always on feature as prior!
						weights[f][c] = Math.Log(numc[c] / num);
					}
					else
					{
						// p_c_f = (N(f,c)+k)/(N(f)+|C|k) = Paddk(c|f)
						// set lambda = log (P()/P())
						double p_c = (n_c[c] + epsilon) / (n + numClasses * epsilon);
						double p_c_f = (n_fc[f][c] + trialSigma) / (n_f[f] + trialSigma * numClasses);
						weights[f][c] = Math.Log(p_c_f / p_c);
					}
				}
			}
			return weights;
		}

		private void TuneSigma(int[][] data, int[] labels)
		{
			IDoubleUnaryOperator CVSigmaToPerplexity = null;
			//test if enough training data
			//leave-one-out
			//System.out.println("CV j: "+ j);
			//System.out.println("test i: "+ i + " "+ new BasicDatum(featureIndex.objects(data[i])));
			//System.err.printf("%d: %8g%n", j, score);
			GoldenSectionLineSearch gsls = new GoldenSectionLineSearch(true);
			sigma = gsls.Minimize(CVSigmaToPerplexity, 0.01, 0.0001, 2.0);
			System.Console.Out.WriteLine("Sigma used: " + sigma);
		}

		/// <summary>Create a ClassifierFactory.</summary>
		public NBLinearClassifierFactory()
			: this(1.0)
		{
		}

		/// <summary>Create a ClassifierFactory.</summary>
		/// <param name="sigma">The amount of add-sigma smoothing of evidence</param>
		public NBLinearClassifierFactory(double sigma)
			: this(sigma, false)
		{
		}

		/// <summary>Create a ClassifierFactory.</summary>
		/// <param name="sigma">The amount of add-sigma smoothing of evidence</param>
		/// <param name="interpretAlwaysOnFeatureAsPrior">
		/// If true, a feature that is in every
		/// data item is interpreted as an indication to include a prior
		/// factor over classes.  (If there are multiple such features, an
		/// integral "prior boost" will occur.)  If false, an always on
		/// feature is interpreted as an evidence feature (and, following
		/// the standard math) will have no effect on the model.
		/// </param>
		public NBLinearClassifierFactory(double sigma, bool interpretAlwaysOnFeatureAsPrior)
		{
			this.sigma = sigma;
			this.interpretAlwaysOnFeatureAsPrior = interpretAlwaysOnFeatureAsPrior;
		}

		/// <summary>
		/// setTuneSigmaCV sets the
		/// <c>tuneSigma</c>
		/// flag: when turned on,
		/// the sigma is tuned by cross-validation.
		/// If there is less data than the number of folds, leave-one-out is used.
		/// The default for tuneSigma is false.
		/// </summary>
		/// <param name="folds">Number of folds for cross validation</param>
		public virtual void SetTuneSigmaCV(int folds)
		{
			tuneSigma = true;
			this.folds = folds;
		}

		private const long serialVersionUID = 1;
	}
}
