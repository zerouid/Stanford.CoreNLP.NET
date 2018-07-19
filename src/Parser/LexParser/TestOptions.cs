using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// Options to the parser which affect performance only at testing (parsing)
	/// time.
	/// </summary>
	/// <remarks>
	/// Options to the parser which affect performance only at testing (parsing)
	/// time.
	/// <br />
	/// The Options class that stores the TestOptions stores the
	/// TestOptions as a transient object.  This means that whatever
	/// options get set at creation time are forgotten when the parser is
	/// serialized.  If you want an option to be remembered when the parser
	/// is reloaded, put it in either TrainOptions or in Options itself.
	/// </remarks>
	/// <author>Dan Klein</author>
	[System.Serializable]
	public class TestOptions
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.TestOptions));

		internal const string DefaultPreTagger = "/u/nlp/data/pos-tagger/distrib/wsj-0-18-bidirectional-nodistsim.tagger";

		public TestOptions()
		{
			forceTags = preTag;
			noFunctionalForcing = preTag;
			evals = new Properties();
			evals.SetProperty("pcfgLB", "true");
			evals.SetProperty("depDA", "true");
			evals.SetProperty("factLB", "true");
			evals.SetProperty("factTA", "true");
			evals.SetProperty("summary", "true");
		}

		/// <summary>
		/// If false, then failure of the PCFG parser to parse a sentence
		/// will trigger allowing all tags for words in parse recovery mode,
		/// with a log probability of -1000.
		/// </summary>
		/// <remarks>
		/// If false, then failure of the PCFG parser to parse a sentence
		/// will trigger allowing all tags for words in parse recovery mode,
		/// with a log probability of -1000.
		/// If true, these extra taggings are not added.
		/// It is false by default. Use option -noRecoveryTagging to set
		/// to true.
		/// </remarks>
		public bool noRecoveryTagging = false;

		/// <summary>
		/// If true, then  failure of the PCFG factor to parse a sentence
		/// will trigger parse recovery mode.
		/// </summary>
		public bool doRecovery = true;

		/// <summary>If true, the n^4 "speed-up" is not used with the Factored Parser.</summary>
		public bool useN5 = false;

		/// <summary>
		/// If true, use approximate factored algorithm, which just rescores
		/// PCFG k best, rather than exact factored algorithm.
		/// </summary>
		/// <remarks>
		/// If true, use approximate factored algorithm, which just rescores
		/// PCFG k best, rather than exact factored algorithm.  This algorithm
		/// requires the dependency grammar to exist for rescoring, but not for
		/// the dependency grammar to be run.  Hence the correct usage for
		/// guarding code only required for exact A* factored parsing is now
		/// if (op.doPCFG &amp;&amp; op.doDep &amp;&amp; ! Test.useFastFactored).
		/// </remarks>
		public bool useFastFactored = false;

		/// <summary>If true, use faster iterative deepening CKY algorithm.</summary>
		public bool iterativeCKY = false;

		/// <summary>The maximum sentence length (including punctuation, etc.) to parse.</summary>
		public int maxLength = -unchecked((int)(0xDEADBEEF));

		/// <summary>
		/// The maximum number of edges and hooks combined that the factored parser
		/// will build before giving up.
		/// </summary>
		/// <remarks>
		/// The maximum number of edges and hooks combined that the factored parser
		/// will build before giving up.  This number should probably be relative to
		/// the sentence length parsed. In general, though, if the parser cannot parse
		/// a sentence after this much work then there is no good parse consistent
		/// between the PCFG and Dependency parsers.  (Normally, depending on other
		/// flags), the parser will then just return the best PCFG parse.)
		/// </remarks>
		public int MaxItems = 200000;

		/// <summary>The amount of smoothing put in (as an m-estimate) for unknown words.</summary>
		/// <remarks>
		/// The amount of smoothing put in (as an m-estimate) for unknown words.
		/// If negative, set by the code in the lexicon class.
		/// </remarks>
		public double unseenSmooth = -1.0;

		/// <summary>Parse trees in test treebank in order of increasing length.</summary>
		public bool increasingLength = false;

		/// <summary>Tag the sentences first, then parse given those (coarse) tags.</summary>
		public bool preTag = false;

		/// <summary>Parse using only tags given from correct answer or the POS tagger</summary>
		public bool forceTags;

		public bool forceTagBeginnings = false;

		/// <summary>POS tagger model used when preTag is enabled.</summary>
		public string taggerSerializedFile = DefaultPreTagger;

		/// <summary>
		/// Only valid with force tags - strips away functionals when forcing
		/// the tags, meaning tags have to start
		/// appropriately but the parser will assign the functional part.
		/// </summary>
		public bool noFunctionalForcing;

		/// <summary>Write EvalB-readable output files.</summary>
		public bool evalb = false;

		/// <summary>Print a lot of extra output as you parse.</summary>
		public bool verbose = false;

		public readonly bool exhaustiveTest = false;

		/// <summary>
		/// If this variable is true, and the sum of the inside and outside score
		/// for a constituent is worse than the best known score for a sentence by
		/// more than <code>pcfgThresholdValue</code>, then -Inf is returned as the
		/// outside Score by <code>oScore()</code> (while otherwise the true
		/// outside score is returned).
		/// </summary>
		public readonly bool pcfgThreshold = false;

		public readonly double pcfgThresholdValue = -2.0;

		/// <summary>Print out all best PCFG parses.</summary>
		public bool printAllBestParses = false;

		/// <summary>Weighting on dependency log probs.</summary>
		/// <remarks>
		/// Weighting on dependency log probs.  The dependency grammar negative log
		/// probability scores are simply multiplied by this number.
		/// </remarks>
		public double depWeight = 1.0;

		public bool prunePunc = false;

		/// <summary>
		/// If a token list does not have sentence final punctuation near the
		/// end, then automatically add the default one.
		/// </summary>
		/// <remarks>
		/// If a token list does not have sentence final punctuation near the
		/// end, then automatically add the default one.
		/// This might help parsing if the treebank is all punctuated.
		/// Not done if reading a treebank.
		/// </remarks>
		public bool addMissingFinalPunctuation;

		/// <summary>Determines format of output trees: choose among penn, oneline</summary>
		public string outputFormat = "penn";

		public string outputFormatOptions = string.Empty;

		/// <summary>
		/// If true, write files parsed to a new file with the same name except
		/// for an added ".stp" extension.
		/// </summary>
		public bool writeOutputFiles;

		/// <summary>
		/// If the writeOutputFiles option is true, then output files appear in
		/// this directory.
		/// </summary>
		/// <remarks>
		/// If the writeOutputFiles option is true, then output files appear in
		/// this directory.  An unset value (<code>null</code>) means to use
		/// the directory of the source files.  Use <code>""</code> or <code>.</code>
		/// for the current directory.
		/// </remarks>
		public string outputFilesDirectory;

		/// <summary>
		/// If the writeOutputFiles option is true, then output files appear with
		/// this extension.
		/// </summary>
		/// <remarks>
		/// If the writeOutputFiles option is true, then output files appear with
		/// this extension. Use <code>""</code> for no extension.
		/// </remarks>
		public string outputFilesExtension = "stp";

		/// <summary>
		/// If the writeOutputFiles option is true, then output files appear with
		/// this prefix.
		/// </summary>
		public string outputFilesPrefix = "parses";

		/// <summary>If this option is not null, output the k-best equivocation.</summary>
		/// <remarks>
		/// If this option is not null, output the k-best equivocation. Must be specified
		/// with printPCFGkBest.
		/// </remarks>
		public string outputkBestEquivocation;

		/// <summary>The largest span to consider for word-hood.</summary>
		/// <remarks>
		/// The largest span to consider for word-hood.  Used for parsing unsegmented
		/// Chinese text and parsing lattices.  Keep it at 1 unless you know what
		/// you're doing.
		/// </remarks>
		public int maxSpanForTags = 1;

		/// <summary>Turns on normalizing scores for sentence length.</summary>
		/// <remarks>
		/// Turns on normalizing scores for sentence length.  Makes no difference
		/// (except decreased efficiency) unless maxSpanForTags is greater than one.
		/// Works only for PCFG (so far).
		/// </remarks>
		public bool lengthNormalization = false;

		/// <summary>
		/// Used when you want to generate sample parses instead of finding the best
		/// parse.
		/// </summary>
		/// <remarks>
		/// Used when you want to generate sample parses instead of finding the best
		/// parse.  (NOT YET USED.)
		/// </remarks>
		public bool sample = false;

		/// <summary>Printing k-best parses from PCFG, when k &gt; 0.</summary>
		public int printPCFGkBest = 0;

		/// <summary>If using a kBest eval, use this many trees.</summary>
		public int evalPCFGkBest = 100;

		/// <summary>Printing k-best parses from PCFG, when k &gt; 0.</summary>
		public int printFactoredKGood = 0;

		/// <summary>
		/// What evaluations to report and how to report them
		/// (using LexicalizedParser).
		/// </summary>
		/// <remarks>
		/// What evaluations to report and how to report them
		/// (using LexicalizedParser). Known evaluations
		/// are: pcfgLB, pcfgCB, pcfgDA, pcfgTA, pcfgLL, pcfgRUO, pcfgCUO, pcfgCatE,
		/// pcfgChildSpecific,
		/// depDA, depTA, depLL,
		/// factLB, factCB, factDA, factTA, factLL, factChildSpecific.
		/// The default is pcfgLB,depDA,factLB,factTA.  You need to negate those
		/// ones out (e.g., <code>-evals "depDA=false"</code>) if you don't want
		/// them.
		/// LB = ParseEval labeled bracketing,   <br />
		/// CB = crossing brackets and zero crossing bracket rate,   <br />
		/// DA = dependency accuracy, TA = tagging accuracy,   <br />
		/// LL = log likelihood score,   <br />
		/// RUO/CUO = rules/categories under and over proposed,  <br />
		/// CatE = evaluation by phrasal category.   <br />
		/// ChildSpecific: supply an argument with =.  F1 will be returned
		/// for only the nodes which have at least one child that matches
		/// this regular expression. <br />
		/// Known styles are: runningAverages, summary, tsv. <br />
		/// The default style is summary.
		/// You need to negate it out if you don't want it.
		/// Invalid names in the argument to this option are not reported!
		/// </remarks>
		public Properties evals;

		/// <summary>
		/// This variable says to find k good fast factored parses, how many times
		/// k of the best PCFG parses should be examined.
		/// </summary>
		public int fastFactoredCandidateMultiplier = 3;

		/// <summary>
		/// This variable says to find k good factored parses, how many added on
		/// best PCFG parses should be examined.
		/// </summary>
		public int fastFactoredCandidateAddend = 50;

		/// <summary>
		/// If this is true, the Lexicon is used to score P(w|t) in the backoff inside the
		/// dependency grammar.
		/// </summary>
		/// <remarks>
		/// If this is true, the Lexicon is used to score P(w|t) in the backoff inside the
		/// dependency grammar.  (Otherwise, a MLE is used is w is seen, and a constant if
		/// w is unseen.
		/// </remarks>
		public bool useLexiconToScoreDependencyPwGt = false;

		/// <summary>If this is true, perform non-projective dependency parsing.</summary>
		public bool useNonProjectiveDependencyParser = false;

		/// <summary>Number of threads to use at test time.</summary>
		/// <remarks>
		/// Number of threads to use at test time.  For example,
		/// -testTreebank can use this to go X times faster, with the
		/// negative consequence that output is not quite as nicely ordered.
		/// </remarks>
		public int testingThreads = 1;

		/// <summary>When evaluating, don't print out tons of text.</summary>
		/// <remarks>When evaluating, don't print out tons of text.  Only print out the final scores</remarks>
		public bool quietEvaluation = false;

		// initial value is -0xDEADBEEF (actually positive because of 2s complement)
		// Don't change this; set with -v
		/// <summary>Determines method for print trees on output.</summary>
		/// <param name="tlpParams">The treebank parser params</param>
		/// <returns>A suitable tree printing object</returns>
		public virtual Edu.Stanford.Nlp.Trees.TreePrint TreePrint(ITreebankLangParserParams tlpParams)
		{
			ITreebankLanguagePack tlp = tlpParams.TreebankLanguagePack();
			return new Edu.Stanford.Nlp.Trees.TreePrint(outputFormat, outputFormatOptions, tlp, tlpParams.HeadFinder(), tlpParams.TypedDependencyHeadFinder());
		}

		public virtual void Display()
		{
			string str = ToString();
			log.Info(str);
		}

		public override string ToString()
		{
			return ("Test parameters" + " maxLength=" + maxLength + " preTag=" + preTag + " outputFormat=" + outputFormat + " outputFormatOptions=" + outputFormatOptions + " printAllBestParses=" + printAllBestParses + " testingThreads=" + testingThreads
				 + " quietEvaluation=" + quietEvaluation);
		}

		private const long serialVersionUID = 7256526346598L;
	}
}
