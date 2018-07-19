using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Non-language-specific options for training a grammar from a treebank.</summary>
	/// <remarks>
	/// Non-language-specific options for training a grammar from a treebank.
	/// These options are not used at parsing time.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class TrainOptions
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.TrainOptions));

		public string trainTreeFile = null;

		public TrainOptions()
		{
		}

		public int trainLengthLimit = 100000;

		/// <summary>Add all test set trees to training data for PCFG.</summary>
		/// <remarks>
		/// Add all test set trees to training data for PCFG.
		/// (Currently only supported in FactoredParser main.)
		/// </remarks>
		public bool cheatPCFG = false;

		/// <summary>Whether to do "horizontal Markovization" (as in ACL 2003 paper).</summary>
		/// <remarks>
		/// Whether to do "horizontal Markovization" (as in ACL 2003 paper).
		/// False means regular PCFG expansions.
		/// </remarks>
		public bool markovFactor = false;

		public int markovOrder = 1;

		public bool hSelSplit = false;

		public int HselCut = 10;

		/// <summary>Whether or not to mark final states in binarized grammar.</summary>
		/// <remarks>
		/// Whether or not to mark final states in binarized grammar.
		/// This must be off to get most value out of grammar compaction.
		/// </remarks>
		public bool markFinalStates = true;

		/// <summary>
		/// A POS tag has to have been attributed to more than this number of word
		/// types before it is regarded as an open-class tag.
		/// </summary>
		/// <remarks>
		/// A POS tag has to have been attributed to more than this number of word
		/// types before it is regarded as an open-class tag.  Unknown words will
		/// only possibly be tagged as open-class tags (unless flexiTag is on).
		/// If flexiTag is on, unknown words will be able to be tagged any POS for
		/// which the unseenMap has nonzero count (that is, the tag was seen for
		/// a new word after unseen signature counting was started).
		/// </remarks>
		public int openClassTypesThreshold = 50;

		/// <summary>
		/// Start to aggregate signature-tag pairs only for words unseen in the first
		/// this fraction of the data.
		/// </summary>
		public double fractionBeforeUnseenCounting = 0.5;

		// same for me -- Teg
		/* THESE OPTIONS AFFECT ONLY TRAIN TIME */
		// good with true;
		/// <summary>If true, declare early -- leave this on except maybe with markov on.</summary>
		/// <returns>Whether to do outside factorization in binarization of the grammar</returns>
		public virtual bool OutsideFactor()
		{
			return !markovFactor;
		}

		/// <summary>This variable controls doing parent annotation of phrasal nodes.</summary>
		/// <remarks>This variable controls doing parent annotation of phrasal nodes.  Good.</remarks>
		public bool Pa = true;

		/// <summary>This variable controls doing 2 levels of parent annotation.</summary>
		/// <remarks>This variable controls doing 2 levels of parent annotation.  Bad.</remarks>
		public bool gPA = false;

		public bool postPA = false;

		public bool postGPA = false;

		/// <summary>Only split the "common high KL divergence" parent categories....</summary>
		/// <remarks>Only split the "common high KL divergence" parent categories.... Good.</remarks>
		public bool selectiveSplit = false;

		public double selectiveSplitCutOff = 0.0;

		public bool selectivePostSplit = false;

		public double selectivePostSplitCutOff = 0.0;

		/// <summary>
		/// Whether, in post-splitting of categories, nodes are annotated with the
		/// (grand)parent's base category or with its complete subcategorized
		/// category.
		/// </summary>
		public bool postSplitWithBaseCategory = false;

		/// <summary>Selective Sister annotation.</summary>
		public bool sisterAnnotate = false;

		public ICollection<string> sisterSplitters;

		/// <summary>Mark all unary nodes specially.</summary>
		/// <remarks>
		/// Mark all unary nodes specially.  Good for just PCFG. Bad for factored.
		/// markUnary affects phrasal nodes. A value of 0 means to do nothing;
		/// a value of 1 means to mark the parent (higher) node of a unary rewrite.
		/// A value of 2 means to mark the child (lower) node of a unary rewrie.
		/// Values of 1 and 2 only apply if the child (lower) node is phrasal.
		/// (A value of 1 is better than 2 in combos.)  A value of 1 corresponds
		/// to the old boolean -unary flag.
		/// </remarks>
		public int markUnary = 0;

		/// <summary>Mark POS tags which are the sole member of their phrasal constituent.</summary>
		/// <remarks>
		/// Mark POS tags which are the sole member of their phrasal constituent.
		/// This is like markUnary=2, applied to POS tags.
		/// </remarks>
		public bool markUnaryTags = false;

		/// <summary>Mark all pre-preterminals (also does splitBaseNP: don't need both)</summary>
		public bool splitPrePreT = false;

		/// <summary>Parent annotation on tags.</summary>
		/// <remarks>Parent annotation on tags.  Good (for PCFG?)</remarks>
		public bool tagPA = false;

		/// <summary>Do parent annotation on tags selectively.</summary>
		/// <remarks>Do parent annotation on tags selectively.  Neutral, but less splits.</remarks>
		public bool tagSelectiveSplit = false;

		public double tagSelectiveSplitCutOff = 0.0;

		public bool tagSelectivePostSplit = false;

		public double tagSelectivePostSplitCutOff = 0.0;

		/// <summary>Right edge is right-recursive (X &lt;&lt; X) Bad.</summary>
		/// <remarks>Right edge is right-recursive (X &lt;&lt; X) Bad. (NP only is good)</remarks>
		public bool rightRec = false;

		/// <summary>Left edge is right-recursive (X &lt;&lt; X)  Bad.</summary>
		public bool leftRec = false;

		/// <summary>Promote/delete punctuation like Collins.</summary>
		/// <remarks>Promote/delete punctuation like Collins.  Bad (!)</remarks>
		public bool collinsPunc = false;

		/// <summary>Set the splitter strings.</summary>
		/// <remarks>
		/// Set the splitter strings.  These are a set of parent and/or grandparent
		/// annotated categories which should be split off.
		/// </remarks>
		public ICollection<string> splitters;

		public ISet postSplitters;

		public ICollection<string> deleteSplitters;

		/// <summary>Just for debugging: check that your tree transforms work correctly.</summary>
		/// <remarks>
		/// Just for debugging: check that your tree transforms work correctly.  This
		/// will print the transformations of the first printTreeTransformations trees.
		/// </remarks>
		public int printTreeTransformations = 0;

		public PrintWriter printAnnotatedPW;

		public PrintWriter printBinarizedPW;

		public bool printStates = false;

		/// <summary>How to compact grammars as FSMs.</summary>
		/// <remarks>
		/// How to compact grammars as FSMs.
		/// 0 = no compaction [uses makeSyntheticLabel1],
		/// 1 = no compaction but use label names that wrap from right to left in binarization [uses makeSyntheticLabel2],
		/// 2 = wrapping labels and materialize unary at top rewriting passive to active,
		/// 3 = ExactGrammarCompactor,
		/// 4 = LossyGrammarCompactor,
		/// 5 = CategoryMergingGrammarCompactor.
		/// (May 2007 CDM note: options 4 and 5 don't seem to be functioning sensibly.  0, 1, and 3
		/// seem to be the 'good' options. 2 is only useful as input to 3.  There seems to be
		/// no reason not to use 0, despite the default.)
		/// </remarks>
		public int compactGrammar = 3;

		public bool leftToRight = false;

		//true;
		//true;
		//true;
		// todo [cdm nov 2012]: At present this does nothing. It should print the list of all states of a grammar it trains
		// Maybe just make it an anytime option and print it at the same time that verbose printing of tags is done?
		// exact compaction on by default
		// whether to binarize left to right or head out
		public virtual int CompactGrammar()
		{
			if (markovFactor)
			{
				return compactGrammar;
			}
			return 0;
		}

		public bool noTagSplit = false;

		/// <summary>
		/// Enables linear rule smoothing during grammar extraction
		/// but before grammar compaction.
		/// </summary>
		/// <remarks>
		/// Enables linear rule smoothing during grammar extraction
		/// but before grammar compaction. The alpha term is the same
		/// as that described in Petrov et al. (2006), and has range [0,1].
		/// </remarks>
		public bool ruleSmoothing = false;

		public double ruleSmoothingAlpha = 0.0;

		/// <summary>
		/// TODO wsg2011: This is the old grammar smoothing parameter that no
		/// longer does anything in the parser.
		/// </summary>
		/// <remarks>
		/// TODO wsg2011: This is the old grammar smoothing parameter that no
		/// longer does anything in the parser. It should be removed.
		/// </remarks>
		public bool smoothing = false;

		/// <summary>Discounts the count of BinaryRule's (only, apparently) in training data.</summary>
		public double ruleDiscount = 0.0;

		public bool printAnnotatedRuleCounts = false;

		public bool printAnnotatedStateCounts = false;

		/// <summary>Where to use the basic or split tags in the dependency grammar</summary>
		public bool basicCategoryTagsInDependencyGrammar = false;

		/// <summary>
		/// A transformer to use on the training data before any other
		/// processing step.
		/// </summary>
		/// <remarks>
		/// A transformer to use on the training data before any other
		/// processing step.  This is specified by using the -preTransformer
		/// flag when training the parser.  A comma separated list of classes
		/// will be turned into a CompositeTransformer.  This can be used to
		/// strip subcategories, to run a tsurgeon pattern, or any number of
		/// other useful operations.
		/// </remarks>
		public ITreeTransformer preTransformer = null;

		/// <summary>A set of files to use as extra information in the lexicon.</summary>
		/// <remarks>
		/// A set of files to use as extra information in the lexicon.  This
		/// can provide tagged words which are not part of trees
		/// </remarks>
		public string taggedFiles = null;

		/// <summary>
		/// Use the method reported by Berkeley for splitting and recombining
		/// states.
		/// </summary>
		/// <remarks>
		/// Use the method reported by Berkeley for splitting and recombining
		/// states.  This is an experimental and still in development
		/// reimplementation of that work.
		/// </remarks>
		public bool predictSplits = false;

		/// <summary>If we are predicting splits, we loop this many times</summary>
		public int splitCount = 1;

		/// <summary>If we are predicting splits, we recombine states at this rate every loop</summary>
		public double splitRecombineRate = 0.0;

		/// <summary>When binarizing trees, don't annotate the labels with anything</summary>
		public bool simpleBinarizedLabels = false;

		/// <summary>When binarizing trees, don't binarize trees with two children.</summary>
		/// <remarks>
		/// When binarizing trees, don't binarize trees with two children.
		/// Only applies when using inside markov binarization for now.
		/// </remarks>
		public bool noRebinarization = false;

		/// <summary>
		/// If the training algorithm allows for parallelization, how many
		/// threads to use
		/// </summary>
		public int trainingThreads = 1;

		/// <summary>
		/// When training the DV parsing method, how many of the top K trees
		/// to analyze from the underlying parser
		/// </summary>
		public const int DefaultKBest = 100;

		public int dvKBest = DefaultKBest;

		/// <summary>
		/// When training a parsing method where the training has a (max)
		/// number of iterations, how many iterations to loop
		/// </summary>
		public const int DefaultTrainingIterations = 40;

		public int trainingIterations = DefaultTrainingIterations;

		/// <summary>
		/// When training using batches of trees, such as in the DVParser,
		/// how many trees to use in one batch
		/// </summary>
		public const int DefaultBatchSize = 25;

		public int batchSize = DefaultBatchSize;

		/// <summary>regularization constant</summary>
		public const double DefaultRegcost = 0.0001;

		public double regCost = DefaultRegcost;

		/// <summary>
		/// When training the DV parsing method, how many iterations to loop
		/// for one batch of trees
		/// </summary>
		public const int DefaultQnIterationsPerBatch = 1;

		public int qnIterationsPerBatch = DefaultQnIterationsPerBatch;

		/// <summary>
		/// When training the DV parsing method, how many estimates to keep
		/// for the qn approximation.
		/// </summary>
		public int qnEstimates = 15;

		/// <summary>
		/// When training the DV parsing method, the tolerance to use if we
		/// want to stop qn early
		/// </summary>
		public double qnTolerance = 15;

		/// <summary>
		/// If larger than 0, the parser may choose to output debug information
		/// every X seconds, X iterations, or some other similar metric
		/// </summary>
		public int debugOutputFrequency = 0;

		public long randomSeed = 0;

		public const double DefaultLearningRate = 0.1;

		/// <summary>How fast to learn (can mean different things for different algorithms)</summary>
		public double learningRate = DefaultLearningRate;

		public const double DefaultDeltaMargin = 0.1;

		/// <summary>
		/// How much to penalize the wrong trees for how different they are
		/// from the gold tree when training
		/// </summary>
		public double deltaMargin = DefaultDeltaMargin;

		/// <summary>Whether or not to build an unknown word vector specifically for numbers</summary>
		public bool unknownNumberVector = true;

		/// <summary>Whether or not to handle unknown dashed words by taking the last part</summary>
		public bool unknownDashedWordVectors = true;

		/// <summary>Whether or not to build an unknown word vector for words with caps in them</summary>
		public bool unknownCapsVector = true;

		/// <summary>Make the dv model as simple as possible</summary>
		public bool dvSimplifiedModel = false;

		/// <summary>Whether or not to build an unknown word vector to match Chinese years</summary>
		public bool unknownChineseYearVector = true;

		/// <summary>Whether or not to build an unknown word vector to match Chinese numbers</summary>
		public bool unknownChineseNumberVector = true;

		/// <summary>Whether or not to build an unknown word vector to match Chinese percentages</summary>
		public bool unknownChinesePercentVector = true;

		public const double DefaultScalingForInit = 0.5;

		/// <summary>How much to scale certain parameters when initializing models.</summary>
		/// <remarks>
		/// How much to scale certain parameters when initializing models.
		/// For example, the DVParser uses this to rescale its initial
		/// matrices.
		/// </remarks>
		public double scalingForInit = DefaultScalingForInit;

		public int maxTrainTimeSeconds = 0;

		public const string DefaultUnkWord = "*UNK*";

		/// <summary>
		/// Some models will use external data sources which contain
		/// information about unknown words.
		/// </summary>
		/// <remarks>
		/// Some models will use external data sources which contain
		/// information about unknown words.  This variable is a way to
		/// provide the name of the unknown word in the external data source.
		/// </remarks>
		public string unkWord = DefaultUnkWord;

		/// <summary>Whether or not to lowercase word vectors</summary>
		public bool lowercaseWordVectors = false;

		public enum TransformMatrixType
		{
			Diagonal,
			Random,
			OffDiagonal,
			RandomZeros
		}

		public TrainOptions.TransformMatrixType transformMatrixType = TrainOptions.TransformMatrixType.Diagonal;

		/// <summary>
		/// Specifically for the DVModel, uses words on either side of a
		/// context when combining constituents.
		/// </summary>
		/// <remarks>
		/// Specifically for the DVModel, uses words on either side of a
		/// context when combining constituents.  Gives perhaps a microscopic
		/// improvement in performance but causes a large slowdown.
		/// </remarks>
		public bool useContextWords = false;

		/// <summary>
		/// Do we want a model that uses word vectors (such as the DVParser)
		/// to train those word vectors when training the model?
		/// <br />
		/// Note: models prior to 2014-02-13 may have incorrect values in
		/// this field, as it was originally a compile time constant
		/// </summary>
		public bool trainWordVectors = true;

		public const int DefaultStalledIterationLimit = 12;

		/// <summary>
		/// How many iterations to allow training to stall before taking the
		/// best model, if training in an iterative manner
		/// </summary>
		public int stalledIterationLimit = DefaultStalledIterationLimit;

		/// <summary>Horton-Strahler number/dimension (Maximilian Schlund)</summary>
		public bool markStrahler;

		/*  public boolean factorOut = false;
		public boolean rightBonus = false;
		public boolean brokenDep = false;*/
		//public boolean outsideFilter = false;
		public virtual void Display()
		{
			log.Info(ToString());
		}

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			result.Append("Train parameters:\n");
			result.Append(" smooth=" + smoothing + "\n");
			result.Append(" PA=" + Pa + "\n");
			result.Append(" GPA=" + gPA + "\n");
			result.Append(" selSplit=" + selectiveSplit + "\n");
			result.Append(" (" + selectiveSplitCutOff + ((deleteSplitters != null) ? ("; deleting " + deleteSplitters) : string.Empty) + ")" + "\n");
			result.Append(" mUnary=" + markUnary + "\n");
			result.Append(" mUnaryTags=" + markUnaryTags + "\n");
			result.Append(" sPPT=" + splitPrePreT + "\n");
			result.Append(" tagPA=" + tagPA + "\n");
			result.Append(" tagSelSplit=" + tagSelectiveSplit + " (" + tagSelectiveSplitCutOff + ")" + "\n");
			result.Append(" rightRec=" + rightRec + "\n");
			result.Append(" leftRec=" + leftRec + "\n");
			result.Append(" collinsPunc=" + collinsPunc + "\n");
			result.Append(" markov=" + markovFactor + "\n");
			result.Append(" mOrd=" + markovOrder + "\n");
			result.Append(" hSelSplit=" + hSelSplit + " (" + HselCut + ")" + "\n");
			result.Append(" compactGrammar=" + CompactGrammar() + "\n");
			result.Append(" postPA=" + postPA + "\n");
			result.Append(" postGPA=" + postGPA + "\n");
			result.Append(" selPSplit=" + selectivePostSplit + " (" + selectivePostSplitCutOff + ")" + "\n");
			result.Append(" tagSelPSplit=" + tagSelectivePostSplit + " (" + tagSelectivePostSplitCutOff + ")" + "\n");
			result.Append(" postSplitWithBase=" + postSplitWithBaseCategory + "\n");
			result.Append(" fractionBeforeUnseenCounting=" + fractionBeforeUnseenCounting + "\n");
			result.Append(" openClassTypesThreshold=" + openClassTypesThreshold + "\n");
			result.Append(" preTransformer=" + preTransformer + "\n");
			result.Append(" taggedFiles=" + taggedFiles + "\n");
			result.Append(" predictSplits=" + predictSplits + "\n");
			result.Append(" splitCount=" + splitCount + "\n");
			result.Append(" splitRecombineRate=" + splitRecombineRate + "\n");
			result.Append(" simpleBinarizedLabels=" + simpleBinarizedLabels + "\n");
			result.Append(" noRebinarization=" + noRebinarization + "\n");
			result.Append(" trainingThreads=" + trainingThreads + "\n");
			result.Append(" dvKBest=" + dvKBest + "\n");
			result.Append(" trainingIterations=" + trainingIterations + "\n");
			result.Append(" batchSize=" + batchSize + "\n");
			result.Append(" regCost=" + regCost + "\n");
			result.Append(" qnIterationsPerBatch=" + qnIterationsPerBatch + "\n");
			result.Append(" qnEstimates=" + qnEstimates + "\n");
			result.Append(" qnTolerance=" + qnTolerance + "\n");
			result.Append(" debugOutputFrequency=" + debugOutputFrequency + "\n");
			result.Append(" randomSeed=" + randomSeed + "\n");
			result.Append(" learningRate=" + learningRate + "\n");
			result.Append(" deltaMargin=" + deltaMargin + "\n");
			result.Append(" unknownNumberVector=" + unknownNumberVector + "\n");
			result.Append(" unknownDashedWordVectors=" + unknownDashedWordVectors + "\n");
			result.Append(" unknownCapsVector=" + unknownCapsVector + "\n");
			result.Append(" unknownChineseYearVector=" + unknownChineseYearVector + "\n");
			result.Append(" unknownChineseNumberVector=" + unknownChineseNumberVector + "\n");
			result.Append(" unknownChinesePercentVector=" + unknownChinesePercentVector + "\n");
			result.Append(" dvSimplifiedModel=" + dvSimplifiedModel + "\n");
			result.Append(" scalingForInit=" + scalingForInit + "\n");
			result.Append(" maxTrainTimeSeconds=" + maxTrainTimeSeconds + "\n");
			result.Append(" unkWord=" + unkWord + "\n");
			result.Append(" lowercaseWordVectors=" + lowercaseWordVectors + "\n");
			result.Append(" transformMatrixType=" + transformMatrixType + "\n");
			result.Append(" useContextWords=" + useContextWords + "\n");
			result.Append(" trainWordVectors=" + trainWordVectors + "\n");
			result.Append(" stalledIterationLimit=" + stalledIterationLimit + "\n");
			result.Append(" markStrahler=" + markStrahler + "\n");
			return result.ToString();
		}

		public static void PrintTrainTree(PrintWriter pw, string message, Tree t)
		{
			PrintWriter myPW;
			if (pw == null)
			{
				myPW = new PrintWriter(System.Console.Out, true);
			}
			else
			{
				myPW = pw;
			}
			if (message != null && pw == null)
			{
				// hard coded to not print message if using file output!
				myPW.Println(message);
			}
			// TODO FIXME:  wtf is this shit
			bool previousState = CategoryWordTag.printWordTag;
			CategoryWordTag.printWordTag = false;
			t.PennPrint(myPW);
			CategoryWordTag.printWordTag = previousState;
		}

		private const long serialVersionUID = 72571349843538L;
	}
}
