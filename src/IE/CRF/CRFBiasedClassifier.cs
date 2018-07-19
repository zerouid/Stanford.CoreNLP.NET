using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>
	/// CRFBiasedClassifier is used to adjust the precision-recall tradeoff
	/// of any CRF model implemented using CRFClassifier.
	/// </summary>
	/// <remarks>
	/// CRFBiasedClassifier is used to adjust the precision-recall tradeoff
	/// of any CRF model implemented using CRFClassifier. This adjustment is
	/// performed after CRF training.  The method is described in Minkov,
	/// Wang, Tomasic, and Cohen (2006): "NER Systems that Suit User's
	/// Preferences: Adjusting the Recall-Precision Trade-off for Entity
	/// Extraction".  CRFBiasedClassifier can import any model serialized
	/// with
	/// <see cref="CRFClassifier{IN}"/>
	/// and supports most command-line parameters
	/// available in
	/// <see cref="CRFClassifier{IN}"/>
	/// .  In addition to this,
	/// CRFBiasedClassifier also interprets the parameter -classBias, as in:
	/// <c>java -server -mx500m edu.stanford.nlp.ie.crf.CRFBiasedClassifier -loadClassifier model.gz -testFile test.txt -classBias A:0.5,B:1.5</c>
	/// The command above sets a bias of 0.5 towards class A and a bias of
	/// 1.5 towards class B. These biases (which internally are treated as
	/// feature weights in the log-linear model underpinning the CRF
	/// classifier) can take any real value. As the weight of A tends towards plus
	/// infinity, the classifier will only predict A labels, and as it tends
	/// towards minus infinity, it will never predict A labels.
	/// </remarks>
	/// <author>Michel Galley</author>
	/// <author>Sonal Gupta (made the class generic)</author>
	public class CRFBiasedClassifier<In> : CRFClassifier<IN>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Crf.CRFBiasedClassifier));

		private const string Bias = "@@@DECODING_CLASS_BIAS@@@";

		private bool testTime = false;

		public CRFBiasedClassifier(Properties props)
			: base(props)
		{
		}

		public CRFBiasedClassifier(SeqClassifierFlags flags)
			: base(flags)
		{
		}

		public override CRFDatum<IList<string>, CRFLabel> MakeDatum(IList<IN> info, int loc, IList<FeatureFactory<IN>> featureFactories)
		{
			pad.Set(typeof(CoreAnnotations.AnswerAnnotation), flags.backgroundSymbol);
			PaddedList<IN> pInfo = new PaddedList<IN>(info, pad);
			IList<IList<string>> features = new List<IList<string>>();
			ICollection<Clique> done = Generics.NewHashSet();
			for (int i = 0; i < windowSize; i++)
			{
				IList<string> featuresC = new List<string>();
				IList<Clique> windowCliques = FeatureFactory.GetCliques(i, 0);
				windowCliques.RemoveAll(done);
				Sharpen.Collections.AddAll(done, windowCliques);
				foreach (Clique c in windowCliques)
				{
					foreach (FeatureFactory<IN> featureFactory in featureFactories)
					{
						Sharpen.Collections.AddAll(featuresC, featureFactory.GetCliqueFeatures(pInfo, loc, c));
					}
				}
				if (testTime && i == 0)
				{
					// this feature is only present at test time and only appears
					// in cliques of size 1 (i.e., cliques with window=0)
					featuresC.Add(Bias);
				}
				features.Add(featuresC);
			}
			int[] labels = new int[windowSize];
			for (int i_1 = 0; i_1 < windowSize; i_1++)
			{
				string answer = pInfo[loc + i_1 - windowSize + 1].Get(typeof(CoreAnnotations.AnswerAnnotation));
				labels[i_1] = classIndex.IndexOf(answer);
			}
			return new CRFDatum<IList<string>, CRFLabel>(features, new CRFLabel(labels), null);
		}

		private void AddBiasFeature()
		{
			if (!featureIndex.Contains(Bias))
			{
				featureIndex.Add(Bias);
				double[][] newWeights = new double[weights.Length + 1][];
				System.Array.Copy(weights, 0, newWeights, 0, weights.Length);
				newWeights[weights.Length] = new double[classIndex.Size()];
				weights = newWeights;
			}
		}

		public virtual void SetBiasWeight(string cname, double weight)
		{
			int ci = classIndex.IndexOf(cname);
			SetBiasWeight(ci, weight);
		}

		public virtual void SetBiasWeight(int cindex, double weight)
		{
			AddBiasFeature();
			int fi = featureIndex.IndexOf(Bias);
			weights[fi][cindex] = weight;
		}

		public override IList<IN> Classify(IList<IN> document)
		{
			testTime = true;
			IList<IN> l = base.Classify(document);
			testTime = false;
			return l;
		}

		internal class CRFBiasedClassifierOptimizer : IDoubleUnaryOperator
		{
			private readonly CRFBiasedClassifier<IN> crf;

			private readonly IDoubleUnaryOperator evalFunction;

			internal CRFBiasedClassifierOptimizer(CRFBiasedClassifier<In> _enclosing, CRFBiasedClassifier<IN> c, IDoubleUnaryOperator e)
			{
				this._enclosing = _enclosing;
				this.crf = c;
				this.evalFunction = e;
			}

			public virtual double ApplyAsDouble(double w)
			{
				this.crf.SetBiasWeight(0, w);
				return this.evalFunction.ApplyAsDouble(w);
			}

			private readonly CRFBiasedClassifier<In> _enclosing;
		}

		// end class class CRFBiasedClassifierOptimizer
		/// <summary>Adjust the bias parameter to optimize some objective function.</summary>
		/// <remarks>
		/// Adjust the bias parameter to optimize some objective function.
		/// Note that this function only tunes the bias parameter of one class
		/// (class of index 0), and is thus only useful for binary classification
		/// problems.
		/// </remarks>
		public virtual void AdjustBias(IList<IList<IN>> develData, IDoubleUnaryOperator evalFunction, double low, double high)
		{
			ILineSearcher ls = new GoldenSectionLineSearch(true, 1e-2, low, high);
			CRFBiasedClassifier.CRFBiasedClassifierOptimizer optimizer = new CRFBiasedClassifier.CRFBiasedClassifierOptimizer(this, this, evalFunction);
			double optVal = ls.Minimize(optimizer);
			int bi = featureIndex.IndexOf(Bias);
			log.Info("Class bias of " + weights[bi][0] + " reaches optimal value " + optVal);
		}

		/// <summary>The main method, which is essentially the same as in CRFClassifier.</summary>
		/// <remarks>The main method, which is essentially the same as in CRFClassifier. See the class documentation.</remarks>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			StringUtils.LogInvocationString(log, args);
			Properties props = StringUtils.ArgsToProperties(args);
			CRFBiasedClassifier<CoreLabel> crf = new CRFBiasedClassifier<CoreLabel>(props);
			string testFile = crf.flags.testFile;
			string loadPath = crf.flags.loadClassifier;
			if (loadPath != null)
			{
				crf.LoadClassifierNoExceptions(loadPath, props);
			}
			else
			{
				if (crf.flags.loadJarClassifier != null)
				{
					// legacy support of old option
					crf.LoadClassifierNoExceptions(crf.flags.loadJarClassifier, props);
				}
				else
				{
					crf.LoadDefaultClassifier();
				}
			}
			if (crf.flags.classBias != null)
			{
				StringTokenizer biases = new StringTokenizer(crf.flags.classBias, ",");
				while (biases.HasMoreTokens())
				{
					StringTokenizer bias = new StringTokenizer(biases.NextToken(), ":");
					string cname = bias.NextToken();
					double w = double.ParseDouble(bias.NextToken());
					crf.SetBiasWeight(cname, w);
					log.Info("Setting bias for class " + cname + " to " + w);
				}
			}
			if (testFile != null)
			{
				IDocumentReaderAndWriter<CoreLabel> readerAndWriter = crf.MakeReaderAndWriter();
				if (crf.flags.printFirstOrderProbs)
				{
					crf.PrintFirstOrderProbs(testFile, readerAndWriter);
				}
				else
				{
					if (crf.flags.printProbs)
					{
						crf.PrintProbs(testFile, readerAndWriter);
					}
					else
					{
						if (crf.flags.useKBest)
						{
							int k = crf.flags.kBest;
							crf.ClassifyAndWriteAnswersKBest(testFile, k, readerAndWriter);
						}
						else
						{
							crf.ClassifyAndWriteAnswers(testFile, readerAndWriter, true);
						}
					}
				}
			}
		}
		// end main
	}
}
