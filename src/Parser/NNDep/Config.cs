using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Nndep
{
	/// <summary>
	/// Defines configuration settings for training and testing the
	/// neural-network dependency parser.
	/// </summary>
	/// <seealso cref="DependencyParser"/>
	/// <author>Danqi Chen</author>
	/// <author>Jon Gauthier</author>
	public class Config
	{
		/// <summary>Out-of-vocabulary token string.</summary>
		public const string Unknown = "-UNKNOWN-";

		/// <summary>Root token string.</summary>
		public const string Root = "-ROOT-";

		/// <summary>Non-existent token string.</summary>
		public const string Null = "-NULL-";

		/// <summary>Represent a non-existent token.</summary>
		public const int Nonexist = -1;

		/// <summary>For printing messages.</summary>
		public const string Separator = "###################";

		/// <summary>The language being parsed.</summary>
		public Language language = Language.UniversalEnglish;

		/// <summary>Number of threads to use during training.</summary>
		/// <remarks>
		/// Number of threads to use during training. Also indirectly controls
		/// how mini-batches are partitioned (more threads =&gt; more partitions
		/// =&gt; smaller partitions).
		/// </remarks>
		public int trainingThreads = 1;

		/// <summary>
		/// Refuse to train on words which have a corpus frequency less than
		/// this number.
		/// </summary>
		public int wordCutOff = 1;

		/// <summary>
		/// Model weights will be initialized to random values within the
		/// range
		/// <c>[-initRange, initRange]</c>
		/// .
		/// </summary>
		public double initRange = 0.01;

		/// <summary>Maximum number of iterations for training</summary>
		public int maxIter = 20000;

		/// <summary>Size of mini-batch for training.</summary>
		/// <remarks>
		/// Size of mini-batch for training. A random subset of training
		/// examples of this size will be used to train the classifier on each
		/// iteration.
		/// </remarks>
		public int batchSize = 10000;

		/// <summary>
		/// An epsilon value added to the denominator of the AdaGrad
		/// expression for numerical stability
		/// </summary>
		public double adaEps = 1e-6;

		/// <summary>Initial global learning rate for AdaGrad training</summary>
		public double adaAlpha = 0.01;

		/// <summary>Regularization parameter.</summary>
		/// <remarks>
		/// Regularization parameter. All weight updates are scaled by this
		/// single parameter.
		/// </remarks>
		public double regParameter = 1e-8;

		/// <summary>Dropout probability.</summary>
		/// <remarks>
		/// Dropout probability. For each training example we randomly choose
		/// some amount of units to disable in the neural network classifier.
		/// This probability controls the proportion of units "dropped out."
		/// </remarks>
		public double dropProb = 0.5;

		/// <summary>Size of the neural network hidden layer.</summary>
		public int hiddenSize = 200;

		/// <summary>Dimensionality of the word embeddings used</summary>
		public int embeddingSize = 50;

		/// <summary>Total number of tokens provided as input to the classifier.</summary>
		/// <remarks>
		/// Total number of tokens provided as input to the classifier. (Each
		/// token is provided in word embedding form.)
		/// </remarks>
		public const int numTokens = 48;

		/// <summary>
		/// Number of input tokens for which we should compute hidden-layer
		/// unit activations.
		/// </summary>
		/// <remarks>
		/// Number of input tokens for which we should compute hidden-layer
		/// unit activations.
		/// If zero, the parser will skip the pre-computation step.
		/// </remarks>
		public int numPreComputed = 100000;

		/// <summary>
		/// During training, run a full UAS evaluation after every
		/// <c>evalPerIter</c>
		/// iterations.
		/// </summary>
		public int evalPerIter = 100;

		/// <summary>
		/// During training, clear AdaGrad gradient histories after every
		/// <c>clearGradientsPerIter</c>
		/// iterations. (If zero, never clear
		/// gradients.)
		/// </summary>
		public int clearGradientsPerIter = 0;

		/// <summary>
		/// Save an intermediate model file whenever we see an improved UAS
		/// evaluation.
		/// </summary>
		/// <remarks>
		/// Save an intermediate model file whenever we see an improved UAS
		/// evaluation. (The frequency of these evaluations is configurable as
		/// well; see
		/// <see cref="evalPerIter"/>
		/// .)
		/// </remarks>
		public bool saveIntermediate = true;

		/// <summary>Train a labeled parser if labeled = true, and a unlabeled one otherwise.</summary>
		public bool unlabeled = false;

		/// <summary>Use coarse POS instead of fine-grained POS if cPOS = true.</summary>
		public bool cPOS = false;

		/// <summary>Exclude punctuations in evaluation if noPunc = true.</summary>
		public bool noPunc = true;

		/// <summary>Update word embeddings when performing gradient descent.</summary>
		/// <remarks>
		/// Update word embeddings when performing gradient descent.
		/// Set to false if you provide embeddings and do not want to finetune.
		/// </remarks>
		public bool doWordEmbeddingGradUpdate = true;

		/// <summary>
		/// Describes language-specific properties necessary for training and
		/// testing.
		/// </summary>
		/// <remarks>
		/// Describes language-specific properties necessary for training and
		/// testing. By default,
		/// <see cref="Edu.Stanford.Nlp.Trees.PennTreebankLanguagePack"/>
		/// will be
		/// used.
		/// </remarks>
		public ITreebankLanguagePack tlp;

		/// <summary>
		/// If non-null, when parsing raw text assume sentences have already
		/// been split and are separated by the given delimiter.
		/// </summary>
		/// <remarks>
		/// If non-null, when parsing raw text assume sentences have already
		/// been split and are separated by the given delimiter.
		/// If null, the parser splits sentences automatically.
		/// </remarks>
		public string sentenceDelimiter = null;

		/// <summary>Defines a word-escaper to use when parsing raw sentences.</summary>
		/// <remarks>
		/// Defines a word-escaper to use when parsing raw sentences.
		/// As a command-line option, you should provide the fully qualified
		/// class name of a valid escaper (that is, a class which implements
		/// <c>Function&lt;List&lt;HasWord&gt;, List&lt;HasWord&gt;&gt;</c>
		/// ).
		/// </remarks>
		public IFunction<IList<IHasWord>, IList<IHasWord>> escaper = null;

		/// <summary>
		/// Path to a tagger file compatible with
		/// <see cref="Edu.Stanford.Nlp.Tagger.Maxent.MaxentTagger"/>
		/// .
		/// </summary>
		public string tagger = MaxentTagger.DefaultJarPath;

		public Config(Properties properties)
		{
			// TODO: we can figure this out automatically based on features used.
			// Should remove this option once we make feature templates / dynamic features
			// --- Runtime parsing options
			SetProperties(properties);
		}

		private void SetProperties(Properties props)
		{
			trainingThreads = PropertiesUtils.GetInt(props, "trainingThreads", trainingThreads);
			wordCutOff = PropertiesUtils.GetInt(props, "wordCutOff", wordCutOff);
			initRange = PropertiesUtils.GetDouble(props, "initRange", initRange);
			maxIter = PropertiesUtils.GetInt(props, "maxIter", maxIter);
			batchSize = PropertiesUtils.GetInt(props, "batchSize", batchSize);
			adaEps = PropertiesUtils.GetDouble(props, "adaEps", adaEps);
			adaAlpha = PropertiesUtils.GetDouble(props, "adaAlpha", adaAlpha);
			regParameter = PropertiesUtils.GetDouble(props, "regParameter", regParameter);
			dropProb = PropertiesUtils.GetDouble(props, "dropProb", dropProb);
			hiddenSize = PropertiesUtils.GetInt(props, "hiddenSize", hiddenSize);
			embeddingSize = PropertiesUtils.GetInt(props, "embeddingSize", embeddingSize);
			numPreComputed = PropertiesUtils.GetInt(props, "numPreComputed", numPreComputed);
			evalPerIter = PropertiesUtils.GetInt(props, "evalPerIter", evalPerIter);
			clearGradientsPerIter = PropertiesUtils.GetInt(props, "clearGradientsPerIter", clearGradientsPerIter);
			saveIntermediate = PropertiesUtils.GetBool(props, "saveIntermediate", saveIntermediate);
			unlabeled = PropertiesUtils.GetBool(props, "unlabeled", unlabeled);
			cPOS = PropertiesUtils.GetBool(props, "cPOS", cPOS);
			noPunc = PropertiesUtils.GetBool(props, "noPunc", noPunc);
			doWordEmbeddingGradUpdate = PropertiesUtils.GetBool(props, "doWordEmbeddingGradUpdate", doWordEmbeddingGradUpdate);
			// Runtime parsing options
			sentenceDelimiter = PropertiesUtils.GetString(props, "sentenceDelimiter", sentenceDelimiter);
			tagger = PropertiesUtils.GetString(props, "tagger.model", tagger);
			string escaperClass = props.GetProperty("escaper");
			escaper = escaperClass != null ? ReflectionLoading.LoadByReflection(escaperClass) : null;
			// Language options
			language = props.Contains("language") ? GetLanguage(props.GetProperty("language")) : language;
			tlp = language.@params.TreebankLanguagePack();
			// if a tlp was specified go with that
			string tlpCanonicalName = props.GetProperty("tlp");
			if (tlpCanonicalName != null)
			{
				try
				{
					tlp = ReflectionLoading.LoadByReflection(tlpCanonicalName);
					System.Console.Error.WriteLine("Loaded TreebankLanguagePack: " + tlpCanonicalName);
				}
				catch (Exception)
				{
					System.Console.Error.WriteLine("Error: Failed to load TreebankLanguagePack: " + tlpCanonicalName);
				}
			}
		}

		/// <summary>
		/// Get the
		/// <see cref="Edu.Stanford.Nlp.International.Language"/>
		/// object corresponding to the given language string.
		/// </summary>
		/// <returns>
		/// A
		/// <see cref="Edu.Stanford.Nlp.International.Language"/>
		/// or
		/// <see langword="null"/>
		/// if no instance matches the given string.
		/// </returns>
		public static Language GetLanguage(string languageStr)
		{
			foreach (Language l in Language.Values())
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(l.ToString(), languageStr))
				{
					return l;
				}
			}
			return null;
		}

		public virtual void PrintParameters()
		{
			System.Console.Error.Printf("language = %s%n", language);
			System.Console.Error.Printf("trainingThreads = %d%n", trainingThreads);
			System.Console.Error.Printf("wordCutOff = %d%n", wordCutOff);
			System.Console.Error.Printf("initRange = %.2g%n", initRange);
			System.Console.Error.Printf("maxIter = %d%n", maxIter);
			System.Console.Error.Printf("batchSize = %d%n", batchSize);
			System.Console.Error.Printf("adaEps = %.2g%n", adaEps);
			System.Console.Error.Printf("adaAlpha = %.2g%n", adaAlpha);
			System.Console.Error.Printf("regParameter = %.2g%n", regParameter);
			System.Console.Error.Printf("dropProb = %.2g%n", dropProb);
			System.Console.Error.Printf("hiddenSize = %d%n", hiddenSize);
			System.Console.Error.Printf("embeddingSize = %d%n", embeddingSize);
			System.Console.Error.Printf("numPreComputed = %d%n", numPreComputed);
			System.Console.Error.Printf("evalPerIter = %d%n", evalPerIter);
			System.Console.Error.Printf("clearGradientsPerIter = %d%n", clearGradientsPerIter);
			System.Console.Error.Printf("saveItermediate = %b%n", saveIntermediate);
			System.Console.Error.Printf("unlabeled = %b%n", unlabeled);
			System.Console.Error.Printf("cPOS = %b%n", cPOS);
			System.Console.Error.Printf("noPunc = %b%n", noPunc);
			System.Console.Error.Printf("doWordEmbeddingGradUpdate = %b%n", doWordEmbeddingGradUpdate);
		}
	}
}
