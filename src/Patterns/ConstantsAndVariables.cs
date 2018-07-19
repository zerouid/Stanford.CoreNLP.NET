using System;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Patterns.Dep;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Regex;
using Javax.Json;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns
{
	[System.Serializable]
	public class ConstantsAndVariables
	{
		private const long serialVersionUID = 1L;

		/// <summary>Maximum number of iterations to run</summary>
		public int numIterationsForPatterns = 10;

		/// <summary>Maximum number of patterns learned in each iteration</summary>
		public int numPatterns = 10;

		/// <summary>
		/// The output directory where the justifications of learning patterns and
		/// phrases would be saved.
		/// </summary>
		/// <remarks>
		/// The output directory where the justifications of learning patterns and
		/// phrases would be saved. These are needed for visualization
		/// </remarks>
		public string outDir = null;

		/// <summary>Cached file of all patterns for all tokens</summary>
		public string allPatternsDir = null;

		/// <summary>If all patterns should be computed.</summary>
		/// <remarks>
		/// If all patterns should be computed. Otherwise patterns are read from
		/// allPatternsFile
		/// </remarks>
		public bool computeAllPatterns = true;

		/// <summary>Pattern Scoring mechanism.</summary>
		/// <remarks>
		/// Pattern Scoring mechanism. See
		/// <see cref="PatternScoring"/>
		/// for options.
		/// </remarks>
		public GetPatternsFromDataMultiClass.PatternScoring patternScoring = GetPatternsFromDataMultiClass.PatternScoring.PosNegUnlabOdds;

		/// <summary>Threshold for learning a pattern</summary>
		public double thresholdSelectPattern = 1.0;

		/// <summary>Currently, does not work correctly.</summary>
		/// <remarks>
		/// Currently, does not work correctly. TODO: make this work. Ideally this
		/// would label words only when they occur in the context of any learned
		/// pattern. This comment seems old. Test it!
		/// </remarks>
		public bool restrictToMatched = false;

		/// <summary>
		/// Label words that are learned so that in further iterations we have more
		/// information
		/// </summary>
		public bool usePatternResultAsLabel = true;

		/// <summary>Debug flag for learning patterns.</summary>
		/// <remarks>Debug flag for learning patterns. 0 means no output, 1 means necessary output, 2 means necessary output+some justification, 3 means extreme debug output</remarks>
		public int debug = 1;

		/// <summary>Save this run as ...</summary>
		public string identifier = "getpatterns";

		/// <summary>
		/// Use the actual dictionary matching phrase(s) instead of the token word or
		/// lemma in calculating the stats
		/// </summary>
		public bool useMatchingPhrase = true;

		/// <summary>
		/// Reduce pattern threshold (=0.8*current_value) to extract as many patterns
		/// as possible (still restricted by <code>numPatterns</code>)
		/// </summary>
		public bool tuneThresholdKeepRunning = false;

		/// <summary>Maximum number of words to learn</summary>
		public int maxExtractNumWords = int.MaxValue;

		/// <summary>
		/// use the seed dictionaries and the new words learned for the other labels in
		/// the previous iterations as negative
		/// </summary>
		public bool useOtherLabelsWordsasNegative = true;

		/// <summary>
		/// If not null, write the output like
		/// "w1 w2 <label1> w3 <label2>w4</label2> </label1> w5 ...
		/// </summary>
		/// <remarks>
		/// If not null, write the output like
		/// "w1 w2 <label1> w3 <label2>w4</label2> </label1> w5 ... " if w3 w4 have
		/// label1 and w4 has label 2
		/// </remarks>
		internal string markedOutputTextFile = null;

		/// <summary>If you want output of form "word\tlabels-separated-by-comma" in newlines</summary>
		internal string columnOutputFile = null;

		/// <summary>Lowercase the context words/lemmas</summary>
		public static bool matchLowerCaseContext = true;

		/// <summary>
		/// Initials of all POS tags to use if
		/// <code>usePOS4Pattern</code> is true, separated by comma.
		/// </summary>
		public string targetAllowedTagsInitialsStr = null;

		public IDictionary<string, ICollection<string>> allowedTagsInitials = null;

		/// <summary>Allowed NERs for labels.</summary>
		/// <remarks>
		/// Allowed NERs for labels. Format is label1,NER1,NER11;label2,NER2,NER21,NER22;label3,...
		/// <code>useTargetNERRestriction</code> flag should be true
		/// </remarks>
		public string targetAllowedNERs = null;

		public IDictionary<string, ICollection<string>> allowedNERsforLabels = null;

		/// <summary>Number of words to learn in each iteration</summary>
		public int numWordsToAdd = 10;

		public double thresholdNumPatternsApplied = 2;

		public GetPatternsFromDataMultiClass.WordScoring wordScoring = GetPatternsFromDataMultiClass.WordScoring.Weightednorm;

		public double thresholdWordExtract = 0.2;

		public bool justify = false;

		/// <summary>
		/// Sigma for L2 regularization in Logisitic regression, if a classifier is
		/// used to score phrases
		/// </summary>
		public double LRSigma = 1.0;

		/// <summary>English words that are not labeled when labeling using seed dictionaries</summary>
		public string englishWordsFiles = null;

		private ICollection<string> englishWords = new HashSet<string>();

		/// <summary>
		/// Words to be ignored when learning phrases if
		/// <code>removePhrasesWithStopWords</code> or
		/// <code>removeStopWordsFromSelectedPhrases</code> is true.
		/// </summary>
		/// <remarks>
		/// Words to be ignored when learning phrases if
		/// <code>removePhrasesWithStopWords</code> or
		/// <code>removeStopWordsFromSelectedPhrases</code> is true. Also, these words
		/// are considered negative when scoring a pattern (similar to
		/// othersemanticclasses).
		/// </remarks>
		public string commonWordsPatternFiles = null;

		private ICollection<string> commonEngWords = null;

		/// <summary>List of dictionary phrases that are negative for all labels to be learned.</summary>
		/// <remarks>
		/// List of dictionary phrases that are negative for all labels to be learned.
		/// Format is file_1,file_2,... where file_i has each phrase in a different
		/// line
		/// </remarks>
		public string otherSemanticClassesFiles = null;

		private ICollection<CandidatePhrase> otherSemanticClassesWords = null;

		/// <summary>Seed dictionary, set in the class that uses this class</summary>
		private IDictionary<string, ICollection<CandidatePhrase>> seedLabelDictionary = new Dictionary<string, ICollection<CandidatePhrase>>();

		/// <summary>Just the set of labels</summary>
		private ICollection<string> labels = new HashSet<string>();

		private IDictionary<string, Type> answerClass = null;

		/// <summary>Can be used only when using the API - using the appropriate constructor.</summary>
		/// <remarks>
		/// Can be used only when using the API - using the appropriate constructor.
		/// Tokens with specified classes set (has to be boolean return value, even
		/// though this variable says object) will be ignored.
		/// </remarks>
		private IDictionary<string, IDictionary<Type, object>> ignoreWordswithClassesDuringSelection = null;

		/// <summary>These classes will be generalized.</summary>
		/// <remarks>
		/// These classes will be generalized. It can only be used via the API using
		/// the appropriate constructor. All label classes are by default generalized.
		/// </remarks>
		private static IDictionary<string, Type> generalizeClasses = new Dictionary<string, Type>();

		/// <summary>Minimum length of words that can be matched fuzzily</summary>
		public int minLen4FuzzyForPattern = 6;

		/// <summary>Do not learn phrases that match this regex.</summary>
		public string wordIgnoreRegex = "[^a-zA-Z]*";

		/// <summary>Number of threads</summary>
		public int numThreads = 1;

		/// <summary>Words that are not learned.</summary>
		/// <remarks>
		/// Words that are not learned. Patterns are not created around these words.
		/// And, if useStopWordsBeforeTerm in
		/// <see cref="Edu.Stanford.Nlp.Patterns.Surface.CreatePatterns{E}"/>
		/// is true.
		/// </remarks>
		public string stopWordsPatternFiles = null;

		private static ICollection<CandidatePhrase> stopWords = null;

		/// <summary>
		/// Environment for
		/// <see cref="Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern"/>
		/// </summary>
		public IDictionary<string, Env> env = new Dictionary<string, Env>();

		public static Env globalEnv = TokenSequencePattern.GetNewEnv();

		public bool removeStopWordsFromSelectedPhrases = false;

		public bool removePhrasesWithStopWords = false;

		private bool alreadySetUp = false;

		/// <summary>Cluster file, in which each line is word/phrase<tab>clusterid</summary>
		internal string wordClassClusterFile = null;

		private IDictionary<string, int> wordClassClusters = new Dictionary<string, int>();

		/// <summary>
		/// General cluster file, if you wanna use it somehow, in which each line is
		/// word/phrase<tab>clusterid
		/// </summary>
		internal string generalWordClassClusterFile = null;

		private IDictionary<string, int> generalWordClassClusters = null;

		public string externalFeatureWeightsDir = null;

		public bool doNotApplyPatterns = false;

		/// <summary>If score for a pattern is square rooted</summary>
		public bool sqrtPatScore = false;

		/// <summary>Remove patterns that have number of unlabeled words is less than this.</summary>
		public int minUnlabPhraseSupportForPat = 0;

		/// <summary>Remove patterns that have number of positive words less than this.</summary>
		public int minPosPhraseSupportForPat = 1;

		/// <summary>For example, if positive seed dict contains "cancer" and "breast cancer" then "breast" is included as negative</summary>
		public bool addIndvWordsFromPhrasesExceptLastAsNeg = false;

		/// <summary>Cached files</summary>
		private ConcurrentHashMap<string, double> editDistanceFromEnglishWords = new ConcurrentHashMap<string, double>();

		/// <summary>Cached files</summary>
		private ConcurrentHashMap<string, string> editDistanceFromEnglishWordsMatches = new ConcurrentHashMap<string, string>();

		/// <summary>Cached files</summary>
		private ConcurrentHashMap<string, double> editDistanceFromOtherSemanticClasses = new ConcurrentHashMap<string, double>();

		/// <summary>Cached files</summary>
		private ConcurrentHashMap<string, string> editDistanceFromOtherSemanticClassesMatches = new ConcurrentHashMap<string, string>();

		/// <summary>Cached files</summary>
		private ConcurrentHashMap<string, double> editDistanceFromThisClass = new ConcurrentHashMap<string, double>();

		/// <summary>Cached files</summary>
		private ConcurrentHashMap<string, string> editDistanceFromThisClassMatches = new ConcurrentHashMap<string, string>();

		private ConcurrentHashMap<string, ICounter<string>> wordShapesForLabels = new ConcurrentHashMap<string, ICounter<string>>();

		internal string channelNameLogger = "settingUp";

		public IDictionary<string, ICounter<int>> distSimWeights = new Dictionary<string, ICounter<int>>();

		public IDictionary<string, ICounter<CandidatePhrase>> dictOddsWeights = new Dictionary<string, ICounter<CandidatePhrase>>();

		public Type invertedIndexClass = typeof(InvertedIndexByTokens);

		/// <summary>Where the inverted index (either in memory or lucene) is stored</summary>
		public string invertedIndexDirectory;

		public bool clubNeighboringLabeledWords = false;

		public PatternFactory.PatternType patternType = PatternFactory.PatternType.Surface;

		public bool subsampleUnkAsNegUsingSim = false;

		public bool expandPositivesWhenSampling = false;

		public bool expandNegativesWhenSampling = false;

		public double similarityThresholdHighPrecision = 0.7;

		public double positiveSimilarityThresholdLowPrecision = 0.5;

		public string wordVectorFile = null;

		public bool useWordVectorsToComputeSim;

		internal string logFileVectorSimilarity = null;

		public string goldEntitiesEvalFiles = null;

		public bool evaluate = false;

		internal IDictionary<string, IDictionary<string, bool>> goldEntities = new Dictionary<string, IDictionary<string, bool>>();

		public int featureCountThreshold = 1;

		public int expandPhrasesNumTopSimilar = 1;

		/// <summary>Whether to do a fuzzy matching when matching seeds to text.</summary>
		/// <remarks>Whether to do a fuzzy matching when matching seeds to text. You can tune minLen4FuzzyForPattern parameter.</remarks>
		public bool fuzzyMatch = false;

		/// <summary>Ignore case when matching seed words.</summary>
		/// <remarks>Ignore case when matching seed words. It's a map so something like {name-&gt;true,place-&gt;false}</remarks>
		public IDictionary<string, string> ignoreCaseSeedMatch = new Dictionary<string, string>();

		public string sentsOutFile = null;

		public bool savePatternsWordsDir = true;

		public bool learn = true;

		// @Option(name = "removeRedundantPatterns")
		// public boolean removeRedundantPatterns = true;
		//  /**
		//   * Do not learn patterns that do not extract any unlabeled tokens (kind of
		//   * useless)
		//   */
		//  @Option(name = "discardPatternsWithNoUnlabSupport")
		//  public boolean discardPatternsWithNoUnlabSupport = true;
		//@Option(name = "ignorePatWithLabeledNeigh")
		//public boolean ignorePatWithLabeledNeigh = false;
		// set of words that are considered negative for all classes
		//  @Option(name = "includeExternalFeatures")
		//  public boolean includeExternalFeatures = false;
		//  @Option(name="subSampleUnkAsNegUsingSimPercentage", gloss="When using subsampleUnkAsNegUsingSim, select bottom %")
		//  public double subSampleUnkAsNegUsingSimPercentage = 0.95;
		//  @Option(name="subSampleUnkAsPosUsingSimPercentage", gloss="When using expandPositivesWhenSampling, select top % after applying the threshold")
		//  public double subSampleUnkAsPosUsingSimPercentage = 0.05;
		public virtual ICollection<string> GetLabels()
		{
			return labels;
		}

		//  public void addLearnedWords(String trainLabel, Counter<CandidatePhrase> identifiedWords) {
		//    if(!learnedWords.containsKey(trainLabel))
		//      learnedWords.put(trainLabel, new ClassicCounter<CandidatePhrase>());
		//    this.learnedWords.get(trainLabel).addAll(identifiedWords);
		//  }
		public virtual IDictionary<string, string> GetAllOptions()
		{
			IDictionary<string, string> values = new Dictionary<string, string>();
			if (props != null)
			{
				props.ForEach(null);
			}
			Type thisClass;
			try
			{
				thisClass = Sharpen.Runtime.GetType(this.GetType().FullName);
				FieldInfo[] aClassFields = Sharpen.Runtime.GetDeclaredFields(thisClass);
				foreach (FieldInfo f in aClassFields)
				{
					if (f.GetType().GetType().IsPrimitive || Arrays.BinarySearch(GetPatternsFromDataMultiClass.printOptionClass, f.GetType()) >= 0)
					{
						string fName = f.Name;
						object fvalue = f.GetValue(this);
						values[fName] = fvalue == null ? "null" : fvalue.ToString();
					}
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return values;
		}

		public virtual bool HasSeedWordOrOtherSem(CandidatePhrase p)
		{
			foreach (KeyValuePair<string, ICollection<CandidatePhrase>> seeds in this.seedLabelDictionary)
			{
				if (seeds.Value.Contains(p))
				{
					return true;
				}
			}
			if (otherSemanticClassesWords.Contains(p))
			{
				return true;
			}
			return false;
		}

		public virtual SortedDictionary<int, ICounter<CandidatePhrase>> GetLearnedWordsEachIter(string label)
		{
			return learnedWordsEachIter[label];
		}

		public virtual IDictionary<string, SortedDictionary<int, ICounter<CandidatePhrase>>> GetLearnedWordsEachIter()
		{
			return learnedWordsEachIter;
		}

		public virtual void SetLearnedWordsEachIter(SortedDictionary<int, ICounter<CandidatePhrase>> words, string label)
		{
			this.learnedWordsEachIter[label] = words;
		}

		public class ScorePhraseMeasures : IComparable
		{
			internal string name;

			internal static int num = 0;

			internal int numObj;

			internal static IDictionary<string, ConstantsAndVariables.ScorePhraseMeasures> createdObjects = new ConcurrentHashMap<string, ConstantsAndVariables.ScorePhraseMeasures>();

			//PatternFactory.PatternType.SURFACE;
			//  public PatternIndex getPatternIndex() {
			//    return patternIndex;
			//  }
			//
			//  public void setPatternIndex(PatternIndex patternIndex) {
			//    this.patternIndex = patternIndex;
			//  }
			public static ConstantsAndVariables.ScorePhraseMeasures Create(string n)
			{
				if (createdObjects.Contains(n))
				{
					return createdObjects[n];
				}
				else
				{
					return new ConstantsAndVariables.ScorePhraseMeasures(n);
				}
			}

			private ScorePhraseMeasures(string n)
			{
				this.name = n;
				numObj = num++;
				createdObjects[n] = this;
			}

			public override string ToString()
			{
				return name;
			}

			public override bool Equals(object o)
			{
				if (!(o is ConstantsAndVariables.ScorePhraseMeasures))
				{
					return false;
				}
				return ((ConstantsAndVariables.ScorePhraseMeasures)o).numObj == (this.numObj);
			}

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Distsim = new ConstantsAndVariables.ScorePhraseMeasures("DistSim");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Googlengram = new ConstantsAndVariables.ScorePhraseMeasures("GoogleNGram");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Patwtbyfreq = new ConstantsAndVariables.ScorePhraseMeasures("PatWtByFreq");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Editdistsame = new ConstantsAndVariables.ScorePhraseMeasures("EditDistSame");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Editdistother = new ConstantsAndVariables.ScorePhraseMeasures("EditDistOther");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Domainngram = new ConstantsAndVariables.ScorePhraseMeasures("DomainNgram");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Semanticodds = new ConstantsAndVariables.ScorePhraseMeasures("SemanticOdds");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Wordshape = new ConstantsAndVariables.ScorePhraseMeasures("WordShape");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Wordvecpossimavg = new ConstantsAndVariables.ScorePhraseMeasures("WordVecPosSimAvg");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Wordvecpossimmax = new ConstantsAndVariables.ScorePhraseMeasures("WordVecPosSimMax");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Wordvecnegsimavg = new ConstantsAndVariables.ScorePhraseMeasures("WordVecNegSimAvg");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Wordvecnegsimmax = new ConstantsAndVariables.ScorePhraseMeasures("WordVecNegSimMax");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Isfirstcapital = new ConstantsAndVariables.ScorePhraseMeasures("IsFirstLetterCapital");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Wordshapestr = new ConstantsAndVariables.ScorePhraseMeasures("WordShapeStr");

			internal static readonly ConstantsAndVariables.ScorePhraseMeasures Bow = new ConstantsAndVariables.ScorePhraseMeasures("Word");

			public virtual int CompareTo(object o)
			{
				if (!(o is ConstantsAndVariables.ScorePhraseMeasures))
				{
					return -1;
				}
				else
				{
					return string.CompareOrdinal(o.ToString(), this.ToString());
				}
			}
		}

		/// <summary>Keeps only one label for each token, whichever has the longest</summary>
		public bool removeOverLappingLabelsFromSeed = false;

		/// <summary>Only works if you have single label.</summary>
		/// <remarks>Only works if you have single label. And the word classes are given.</remarks>
		public bool usePhraseEvalWordClass = false;

		/// <summary>Only works if you have single label.</summary>
		/// <remarks>Only works if you have single label. And the word vectors are given.</remarks>
		public bool usePhraseEvalWordVector = false;

		/// <summary>use google tf-idf for learning phrases.</summary>
		/// <remarks>
		/// use google tf-idf for learning phrases. Need to also provide googleNgram_dbname,
		/// googleNgram_username and googleNgram_host
		/// </remarks>
		public bool usePhraseEvalGoogleNgram = false;

		/// <summary>use domain tf-idf for learning phrases</summary>
		public bool usePhraseEvalDomainNgram = false;

		/// <summary>
		/// use \sum_allpat pattern_wt_that_extracted_phrase/phrase_freq for learning
		/// phrases
		/// </summary>
		public bool usePhraseEvalPatWtByFreq = true;

		/// <summary>odds of the phrase freq in the label dictionary vs other dictionaries</summary>
		public bool usePhraseEvalSemanticOdds = false;

		/// <summary>
		/// Edit distance between this phrase and the other phrases in the label
		/// dictionary
		/// </summary>
		public bool usePhraseEvalEditDistSame = false;

		/// <summary>Edit distance between this phrase and other phrases in other dictionaries</summary>
		public bool usePhraseEvalEditDistOther = false;

		public bool usePhraseEvalWordShape = false;

		public bool usePhraseEvalWordShapeStr = false;

		public bool usePhraseEvalFirstCapital;

		/// <summary>use bag of words</summary>
		public bool usePhraseEvalBOW = false;

		/// <summary>
		/// Used only if
		/// <see cref="patternScoring"/>
		/// is <code>PhEvalInPat</code> or
		/// <code>PhEvalInPat</code>. See usePhrase* for meanings.
		/// </summary>
		public bool usePatternEvalWordClass = false;

		/// <summary>
		/// Used only if
		/// <see cref="patternScoring"/>
		/// is <code>PhEvalInPat</code> or
		/// <code>PhEvalInPat</code>. See usePhrase* for meanings.
		/// </summary>
		public bool usePatternEvalWordShape = false;

		public bool usePatternEvalWordShapeStr = false;

		public bool usePatternEvalFirstCapital;

		/// <summary>
		/// Used only if
		/// <see cref="patternScoring"/>
		/// is <code>PhEvalInPat</code> or
		/// <code>PhEvalInPat</code>. See usePhrase* for meanings.
		/// </summary>
		public bool usePatternEvalGoogleNgram = false;

		/// <summary>
		/// Used only if
		/// <see cref="patternScoring"/>
		/// is <code>PhEvalInPat</code> or
		/// <code>PhEvalInPat</code>. See usePhrase* for meanings. Need to also provide googleNgram_dbname,
		/// googleNgram_username and googleNgram_host
		/// </summary>
		public bool usePatternEvalDomainNgram = false;

		/// <summary>
		/// Used only if
		/// <see cref="patternScoring"/>
		/// is <code>PhEvalInPat</code> or
		/// <code>PhEvalInPatLogP</code>. See usePhrase* for meanings.
		/// </summary>
		public bool usePatternEvalSemanticOdds = false;

		/// <summary>
		/// Used only if
		/// <see cref="patternScoring"/>
		/// is <code>PhEvalInPat</code> or
		/// <code>PhEvalInPatLogP</code>. See usePhrase* for meanings.
		/// </summary>
		public bool usePatternEvalEditDistSame = false;

		/// <summary>
		/// Used only if
		/// <see cref="patternScoring"/>
		/// is <code>PhEvalInPat</code> or
		/// <code>PhEvalInPatLogP</code>. See usePhrase* for meanings.
		/// </summary>
		public bool usePatternEvalEditDistOther = false;

		/// <summary>use bag of words</summary>
		public bool usePatternEvalBOW = false;

		/// <summary>These are used to learn weights for features if using logistic regression.</summary>
		/// <remarks>
		/// These are used to learn weights for features if using logistic regression.
		/// Percentage of non-labeled tokens selected as negative.
		/// </remarks>
		public double perSelectRand = 0.01;

		/// <summary>These are used to learn weights for features if using logistic regression.</summary>
		/// <remarks>
		/// These are used to learn weights for features if using logistic regression.
		/// Percentage of negative tokens selected as negative.
		/// </remarks>
		public double perSelectNeg = 1;

		/// <summary>Especially useful for multi word phrase extraction.</summary>
		/// <remarks>
		/// Especially useful for multi word phrase extraction. Do not extract a phrase
		/// if any word is labeled with any other class.
		/// </remarks>
		public bool doNotExtractPhraseAnyWordLabeledOtherClass = true;

		/// <summary>You can save the inverted index.</summary>
		/// <remarks>You can save the inverted index. Lucene index is saved by default to <code>invertedIndexDirectory</code> if given.</remarks>
		public bool saveInvertedIndex = false;

		/// <summary>You can load the inverted index using this file.</summary>
		/// <remarks>
		/// You can load the inverted index using this file.
		/// If false and using lucene index, the existing directory is deleted and new index is made.
		/// </remarks>
		public bool loadInvertedIndex = false;

		public ConstantsAndVariables.PatternForEachTokenWay storePatsForEachToken = ConstantsAndVariables.PatternForEachTokenWay.Memory;

		internal double sampleSentencesForSufficientStats = 1.0;

		public static string backgroundSymbol = "O";

		internal int wordShaper = WordShapeClassifier.Wordshapechris2;

		private ConcurrentHashMap<string, string> wordShapeCache = new ConcurrentHashMap<string, string>();

		public SentenceIndex invertedIndex;

		public static string extremedebug = "extremePatDebug";

		public static string minimaldebug = "minimaldebug";

		internal Properties props;

		public enum PatternForEachTokenWay
		{
			Memory,
			Lucene,
			Db
		}

		public enum PatternIndexWay
		{
			Memory,
			Openhft,
			Lucene
		}

		public IList<string> functionWords = Arrays.AsList("a", "an", "the", "of", "at", "on", "in", "he", "she", "him", "her", "they", "them", "and", "no", "not", "nor", "as", "do");

		/// <exception cref="System.IO.IOException"/>
		public ConstantsAndVariables(Properties props, ICollection<string> labels, IDictionary<string, Type> answerClass, IDictionary<string, Type> generalizeClasses, IDictionary<string, IDictionary<Type, object>> ignoreClasses)
		{
			//
			//  @Option(name = "storePatsIndex", gloss="used for storing patterns index")
			//  public PatternIndexWay storePatsIndex = PatternIndexWay.MEMORY;
			//  /**
			//   * Directory where to save the sentences ser files.
			//   */
			//  @Option(name="saveSentencesSerDir")
			//  public File saveSentencesSerDir = null;
			//
			//  public boolean usingDirForSentsInIndex = false;
			// @Option(name = "wekaOptions")
			// public String wekaOptions = "";
			this.labels = labels;
			foreach (string label in labels)
			{
				this.seedLabelDictionary[label] = new HashSet<CandidatePhrase>();
			}
			this.answerClass = answerClass;
			this.generalizeClasses = generalizeClasses;
			if (this.generalizeClasses == null)
			{
				this.generalizeClasses = new Dictionary<string, Type>();
			}
			this.generalizeClasses.PutAll(answerClass);
			this.ignoreWordswithClassesDuringSelection = ignoreClasses;
			SetUp(props);
		}

		/// <exception cref="System.IO.IOException"/>
		public ConstantsAndVariables(Properties props, IDictionary<string, ICollection<CandidatePhrase>> labelDictionary, IDictionary<string, Type> answerClass, IDictionary<string, Type> generalizeClasses, IDictionary<string, IDictionary<Type, object
			>> ignoreClasses)
		{
			//make the list unmodifiable!
			foreach (KeyValuePair<string, ICollection<CandidatePhrase>> en2 in labelDictionary)
			{
				seedLabelDictionary[en2.Key] = Java.Util.Collections.UnmodifiableSet(en2.Value);
			}
			this.labels = labelDictionary.Keys;
			this.answerClass = answerClass;
			this.generalizeClasses = generalizeClasses;
			if (this.generalizeClasses == null)
			{
				this.generalizeClasses = new Dictionary<string, Type>();
			}
			this.generalizeClasses.PutAll(answerClass);
			this.ignoreWordswithClassesDuringSelection = ignoreClasses;
			SetUp(props);
		}

		/// <exception cref="System.IO.IOException"/>
		public ConstantsAndVariables(Properties props, ICollection<string> labels, IDictionary<string, Type> answerClass)
		{
			this.labels = labels;
			foreach (string label in labels)
			{
				this.seedLabelDictionary[label] = new HashSet<CandidatePhrase>();
			}
			this.answerClass = answerClass;
			this.generalizeClasses = new Dictionary<string, Type>();
			this.generalizeClasses.PutAll(answerClass);
			SetUp(props);
		}

		/// <exception cref="System.IO.IOException"/>
		public ConstantsAndVariables(Properties props, string label, Type answerClass)
		{
			this.labels = new HashSet<string>();
			this.labels.Add(label);
			this.seedLabelDictionary[label] = new HashSet<CandidatePhrase>();
			this.answerClass = new Dictionary<string, Type>();
			this.answerClass[label] = answerClass;
			this.generalizeClasses = new Dictionary<string, Type>();
			this.generalizeClasses.PutAll(this.answerClass);
			SetUp(props);
		}

		/// <exception cref="System.IO.IOException"/>
		public ConstantsAndVariables(Properties props, ICollection<string> labels, IDictionary<string, Type> answerClass, IDictionary<string, Type> generalizeClasses)
		{
			this.labels = labels;
			foreach (string label in labels)
			{
				this.seedLabelDictionary[label] = new HashSet<CandidatePhrase>();
			}
			this.answerClass = answerClass;
			this.generalizeClasses = generalizeClasses;
			if (this.generalizeClasses == null)
			{
				this.generalizeClasses = new Dictionary<string, Type>();
			}
			this.generalizeClasses.PutAll(answerClass);
			SetUp(props);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void SetUp(Properties props)
		{
			if (alreadySetUp)
			{
				return;
			}
			Redwood.Log(Redwood.Dbg, "Setting up ConstantsAndVariables");
			ArgumentParser.FillOptions(this, props);
			ArgumentParser.FillOptions(typeof(PatternFactory), props);
			ArgumentParser.FillOptions(typeof(SurfacePatternFactory), props);
			ArgumentParser.FillOptions(typeof(DepPatternFactory), props);
			if (wordIgnoreRegex != null && !wordIgnoreRegex.IsEmpty())
			{
				Redwood.Log(Redwood.Dbg, "Ignore word regex is " + wordIgnoreRegex);
				PatternFactory.ignoreWordRegex = Pattern.Compile(wordIgnoreRegex);
			}
			foreach (string label in labels)
			{
				env[label] = TokenSequencePattern.GetNewEnv();
				// env.get(label).bind("answer", answerClass.get(label));
				foreach (KeyValuePair<string, Type> en in this.answerClass)
				{
					env[label].Bind(en.Key, en.Value);
				}
				foreach (KeyValuePair<string, Type> en_1 in generalizeClasses)
				{
					env[label].Bind(en_1.Key, en_1.Value);
				}
			}
			Redwood.Log(Redwood.Dbg, channelNameLogger, "Running with debug output");
			stopWords = new HashSet<CandidatePhrase>();
			if (stopWordsPatternFiles != null)
			{
				Redwood.Log(ConstantsAndVariables.minimaldebug, channelNameLogger, "Reading stop words from " + stopWordsPatternFiles);
				foreach (string stopwfile in stopWordsPatternFiles.Split("[;,]"))
				{
					foreach (string word in IOUtils.ReadLines(stopwfile))
					{
						if (!word.Trim().IsEmpty())
						{
							stopWords.Add(CandidatePhrase.CreateOrGet(word.Trim()));
						}
					}
				}
			}
			englishWords = new HashSet<string>();
			if (englishWordsFiles != null)
			{
				System.Console.Out.WriteLine("Reading english words from " + englishWordsFiles);
				foreach (string englishWordsFile in englishWordsFiles.Split("[;,]"))
				{
					Sharpen.Collections.AddAll(englishWords, IOUtils.LinesFromFile(englishWordsFile));
				}
			}
			if (commonWordsPatternFiles != null)
			{
				commonEngWords = Java.Util.Collections.SynchronizedSet(new HashSet<string>());
				foreach (string file in commonWordsPatternFiles.Split("[;,]"))
				{
					Sharpen.Collections.AddAll(commonEngWords, IOUtils.LinesFromFile(file));
				}
			}
			if (otherSemanticClassesFiles != null)
			{
				if (otherSemanticClassesWords == null)
				{
					otherSemanticClassesWords = Java.Util.Collections.SynchronizedSet(new HashSet<CandidatePhrase>());
				}
				foreach (string file in otherSemanticClassesFiles.Split("[;,]"))
				{
					foreach (File f in ListFileIncludingItself(file))
					{
						foreach (string w in IOUtils.ReadLines(f))
						{
							string[] t = w.Split("\\s+");
							if (t.Length <= PatternFactory.numWordsCompoundMax)
							{
								otherSemanticClassesWords.Add(CandidatePhrase.CreateOrGet(w));
							}
						}
					}
				}
				System.Console.Out.WriteLine("Size of othersemantic class variables is " + otherSemanticClassesWords.Count);
			}
			else
			{
				otherSemanticClassesWords = Java.Util.Collections.SynchronizedSet(new HashSet<CandidatePhrase>());
				System.Console.Out.WriteLine("Size of othersemantic class variables is " + 0);
			}
			string stopStr = "/";
			int i = 0;
			foreach (CandidatePhrase s in stopWords)
			{
				if (i > 0)
				{
					stopStr += "|";
				}
				stopStr += Pattern.Quote(s.GetPhrase().ReplaceAll("\\\\", "\\\\\\\\"));
				i++;
			}
			stopStr += "/";
			foreach (string label_1 in labels)
			{
				env[label_1].Bind("$FILLER", "/" + StringUtils.Join(PatternFactory.fillerWords, "|") + "/");
				env[label_1].Bind("$STOPWORD", stopStr);
				env[label_1].Bind("$MOD", "[{tag:/JJ.*/}]");
				if (matchLowerCaseContext)
				{
					env[label_1].SetDefaultStringMatchFlags(NodePattern.CaseInsensitive | NodePattern.UnicodeCase);
					env[label_1].SetDefaultStringPatternFlags(Pattern.CaseInsensitive | Pattern.UnicodeCase);
				}
				env[label_1].Bind("OTHERSEM", typeof(PatternsAnnotations.OtherSemanticLabel));
				env[label_1].Bind("grandparentparsetag", typeof(CoreAnnotations.GrandparentAnnotation));
			}
			if (wordClassClusterFile != null)
			{
				wordClassClusters = new Dictionary<string, int>();
				foreach (string line in IOUtils.ReadLines(wordClassClusterFile))
				{
					string[] t = line.Split("\t");
					wordClassClusters[t[0]] = System.Convert.ToInt32(t[1]);
				}
			}
			if (generalWordClassClusterFile != null)
			{
				SetGeneralWordClassClusters(new Dictionary<string, int>());
				foreach (string line in IOUtils.ReadLines(generalWordClassClusterFile))
				{
					string[] t = line.Split("\t");
					GetGeneralWordClassClusters()[t[0]] = System.Convert.ToInt32(t[1]);
				}
			}
			if (targetAllowedTagsInitialsStr != null)
			{
				allowedTagsInitials = new Dictionary<string, ICollection<string>>();
				foreach (string labelstr in targetAllowedTagsInitialsStr.Split(";"))
				{
					string[] t = labelstr.Split(",");
					ICollection<string> st = new HashSet<string>();
					for (int j = 1; j < t.Length; j++)
					{
						st.Add(t[j]);
					}
					allowedTagsInitials[t[0]] = st;
				}
			}
			if (PatternFactory.useTargetNERRestriction && targetAllowedNERs != null)
			{
				allowedNERsforLabels = new Dictionary<string, ICollection<string>>();
				foreach (string labelstr in targetAllowedNERs.Split(";"))
				{
					string[] t = labelstr.Split(",");
					ICollection<string> st = new HashSet<string>();
					for (int j = 1; j < t.Length; j++)
					{
						st.Add(t[j]);
					}
					allowedNERsforLabels[t[0]] = st;
				}
			}
			foreach (string label_2 in labels)
			{
				learnedWordsEachIter[label_2] = new SortedDictionary<int, ICounter<CandidatePhrase>>();
			}
			if (usePhraseEvalGoogleNgram || usePatternEvalDomainNgram)
			{
				Data.usingGoogleNgram = true;
				ArgumentParser.FillOptions(typeof(GoogleNGramsSQLBacked), props);
			}
			if (goldEntitiesEvalFiles != null && evaluate)
			{
				goldEntities = ReadGoldEntities(goldEntitiesEvalFiles);
			}
			alreadySetUp = true;
		}

		public static IEnumerable<File> ListFileIncludingItself(string file)
		{
			File f = new File(file);
			if (!f.IsDirectory())
			{
				return Arrays.AsList(f);
			}
			else
			{
				return IOUtils.IterFilesRecursive(f);
			}
		}

		// The format of goldEntitiesEvalFiles is assumed same as
		// seedwordsfiles: label,file;label2,file2;...
		// Each file of gold entities consists of each entity in newline with
		// incorrect entities marked with "#" at the end of the entity.
		// Learned entities not present in the gold file are considered
		// negative.
		internal static IDictionary<string, IDictionary<string, bool>> ReadGoldEntities(string goldEntitiesEvalFiles)
		{
			IDictionary<string, IDictionary<string, bool>> goldWords = new Dictionary<string, IDictionary<string, bool>>();
			if (goldEntitiesEvalFiles != null)
			{
				foreach (string gfile in goldEntitiesEvalFiles.Split(";"))
				{
					string[] t = gfile.Split(",");
					string label = t[0];
					string goldfile = t[1];
					IDictionary<string, bool> goldWords4Label = new Dictionary<string, bool>();
					foreach (string line in IOUtils.ReadLines(goldfile))
					{
						line = line.Trim();
						if (line.IsEmpty())
						{
							continue;
						}
						if (line.EndsWith("#"))
						{
							goldWords4Label[Sharpen.Runtime.Substring(line, 0, line.Length - 1)] = false;
						}
						else
						{
							goldWords4Label[line] = true;
						}
					}
					goldWords[label] = goldWords4Label;
				}
			}
			return goldWords;
		}

		public class DataSentsIterator : IEnumerator<Pair<IDictionary<string, DataInstance>, File>>
		{
			internal bool readInMemory = false;

			internal IEnumerator<File> sentfilesIter = null;

			internal bool batchProcessSents;

			public DataSentsIterator(bool batchProcessSents)
			{
				//streams sents, files-from-which-sents-were read
				this.batchProcessSents = batchProcessSents;
				if (batchProcessSents)
				{
					sentfilesIter = Data.sentsFiles.GetEnumerator();
				}
			}

			public virtual bool MoveNext()
			{
				if (batchProcessSents)
				{
					return sentfilesIter.MoveNext();
				}
				else
				{
					return !readInMemory;
				}
			}

			public virtual Pair<IDictionary<string, DataInstance>, File> Current
			{
				get
				{
					if (batchProcessSents)
					{
						try
						{
							File f = sentfilesIter.Current;
							return new Pair<IDictionary<string, DataInstance>, File>(IOUtils.ReadObjectFromFile(f), f);
						}
						catch (Exception e)
						{
							throw new Exception(e);
						}
					}
					else
					{
						readInMemory = true;
						return new Pair<IDictionary<string, DataInstance>, File>(Data.sents, new File(Data.inMemorySaveFileLocation));
					}
				}
			}
		}

		public virtual IDictionary<string, ICounter<string>> GetWordShapesForLabels()
		{
			return wordShapesForLabels;
		}

		//  public void setWordShapesForLabels(ConcurrentHashMap<String, Counter<String>> wordShapesForLabels) {
		//    this.wordShapesForLabels = wordShapesForLabels;
		//  }
		//  public void addGeneralizeClasses(Map<String, Class> gen) {
		//    this.generalizeClasses.putAll(gen);
		//  }
		public static IDictionary<string, Type> GetGeneralizeClasses()
		{
			return generalizeClasses;
		}

		public static ICollection<CandidatePhrase> GetStopWords()
		{
			return stopWords;
		}

		public virtual void AddWordShapes(string label, ICollection<CandidatePhrase> words)
		{
			if (!this.wordShapesForLabels.Contains(label))
			{
				this.wordShapesForLabels[label] = new ClassicCounter<string>();
			}
			foreach (CandidatePhrase wc in words)
			{
				string w = wc.GetPhrase();
				string ws = null;
				if (wordShapeCache.Contains(w))
				{
					ws = wordShapeCache[w];
				}
				else
				{
					ws = WordShapeClassifier.WordShape(w, wordShaper);
					wordShapeCache[w] = ws;
				}
				wordShapesForLabels[label].IncrementCount(ws);
			}
		}

		//  public void setSeedLabelDictionary(Map<String, Set<CandidatePhrase>> seedSets) {
		//    this.seedLabelDictionary = seedSets;
		//
		//    if(usePhraseEvalWordShape || usePatternEvalWordShape){
		//      this.wordShapesForLabels.clear();
		//     for(Entry<String, Set<CandidatePhrase>> en: seedSets.entrySet())
		//       addWordShapes(en.getKey(), en.getValue());
		//    }
		//  }
		public virtual IDictionary<string, ICollection<CandidatePhrase>> GetSeedLabelDictionary()
		{
			return this.seedLabelDictionary;
		}

		internal IDictionary<string, SortedDictionary<int, ICounter<CandidatePhrase>>> learnedWordsEachIter = new Dictionary<string, SortedDictionary<int, ICounter<CandidatePhrase>>>();

		//Map<String, Counter<CandidatePhrase>> learnedWords = new HashMap<String, Counter<CandidatePhrase>>();
		public virtual ICounter<CandidatePhrase> GetLearnedWords(string label)
		{
			ICounter<CandidatePhrase> learned = Counters.Flatten(learnedWordsEachIter[label]);
			if (learned == null)
			{
				learned = new ClassicCounter<CandidatePhrase>();
				learnedWordsEachIter[label] = new SortedDictionary<int, ICounter<CandidatePhrase>>();
			}
			return learned;
		}

		//  public Map<String, Counter<CandidatePhrase>> getLearnedWords() {
		//    return Counters.flatten(learnedWordsEachIter);
		//  }
		//public void setLearnedWords(Counter<CandidatePhrase> words, String label) {
		//  this.learnedWords.put(label, words);
		//}
		public virtual string GetLearnedWordsAsJson()
		{
			IJsonObjectBuilder obj = Javax.Json.Json.CreateObjectBuilder();
			foreach (string label in GetLabels())
			{
				ICounter<CandidatePhrase> learnedWords = GetLearnedWords(label);
				IJsonArrayBuilder arr = Javax.Json.Json.CreateArrayBuilder();
				foreach (CandidatePhrase k in learnedWords.KeySet())
				{
					arr.Add(k.GetPhrase());
				}
				obj.Add(label, arr);
			}
			return obj.Build().ToString();
		}

		public virtual string GetLearnedWordsAsJsonLastIteration()
		{
			IJsonObjectBuilder obj = Javax.Json.Json.CreateObjectBuilder();
			foreach (string label in GetLabels())
			{
				ICounter<CandidatePhrase> learnedWords = GetLearnedWordsEachIter(label).LastEntry().Value;
				IJsonArrayBuilder arr = Javax.Json.Json.CreateArrayBuilder();
				foreach (CandidatePhrase k in learnedWords.KeySet())
				{
					arr.Add(k.GetPhrase());
				}
				obj.Add(label, arr);
			}
			return obj.Build().ToString();
		}

		public virtual string GetSetWordsAsJson(IDictionary<string, ICounter<CandidatePhrase>> words)
		{
			IJsonObjectBuilder obj = Javax.Json.Json.CreateObjectBuilder();
			foreach (string label in GetLabels())
			{
				IJsonArrayBuilder arr = Javax.Json.Json.CreateArrayBuilder();
				foreach (CandidatePhrase k in words[label].KeySet())
				{
					arr.Add(k.GetPhrase());
				}
				obj.Add(label, arr);
			}
			return obj.Build().ToString();
		}

		public virtual ICollection<string> GetEnglishWords()
		{
			return this.englishWords;
		}

		public virtual ICollection<string> GetCommonEngWords()
		{
			return this.commonEngWords;
		}

		public virtual ICollection<CandidatePhrase> GetOtherSemanticClassesWords()
		{
			return this.otherSemanticClassesWords;
		}

		public virtual void SetOtherSemanticClassesWords(ICollection<CandidatePhrase> other)
		{
			this.otherSemanticClassesWords = other;
		}

		public virtual IDictionary<string, int> GetWordClassClusters()
		{
			return this.wordClassClusters;
		}

		private Pair<string, double> GetEditDist(ICollection<CandidatePhrase> words, string ph)
		{
			double minD = editDistMax;
			string minPh = ph;
			foreach (CandidatePhrase ec in words)
			{
				string e = ec.GetPhrase();
				if (e.Equals(ph))
				{
					return new Pair<string, double>(ph, 0.0);
				}
				double d = EditDistanceDamerauLevenshteinLike.EditDistance(e, ph, 3);
				if (d == 1)
				{
					return new Pair<string, double>(e, d);
				}
				if (d == -1)
				{
					d = editDistMax;
				}
				if (d < minD)
				{
					minD = d;
					minPh = e;
				}
			}
			return new Pair<string, double>(minPh, minD);
		}

		internal readonly double editDistMax = 1000;

		/// <summary>Use this option if you are limited by memory ; ignored if fileFormat is ser.</summary>
		public bool batchProcessSents = false;

		public bool writeMatchedTokensFiles = false;

		public bool writeMatchedTokensIdsForEachPhrase = false;

		public virtual Pair<string, double> GetEditDistanceFromThisClass(string label, string ph, int minLen)
		{
			if (ph.Length < minLen)
			{
				return new Pair<string, double>(ph, editDistMax);
			}
			//    if (editDistanceFromThisClass.containsKey(ph))
			//      return new Pair<String, Double>(editDistanceFromThisClassMatches.get(ph),
			//          editDistanceFromThisClass.get(ph));
			ICollection<CandidatePhrase> words = new HashSet<CandidatePhrase>(seedLabelDictionary[label]);
			Sharpen.Collections.AddAll(words, GetLearnedWords(label).KeySet());
			Pair<string, double> minD = GetEditDist(words, ph);
			double minDtotal = minD.Second();
			string minPh = minD.First();
			System.Diagnostics.Debug.Assert((!minPh.IsEmpty()));
			//    editDistanceFromThisClass.putIfAbsent(ph, minDtotal);
			//    editDistanceFromThisClassMatches.putIfAbsent(ph, minPh);
			return new Pair<string, double>(minPh, minDtotal);
		}

		public virtual Pair<string, double> GetEditDistanceFromOtherClasses(string label, string ph, int minLen)
		{
			if (ph.Length < minLen)
			{
				return new Pair<string, double>(ph, editDistMax);
			}
			//    if (editDistanceFromOtherSemanticClasses.containsKey(ph))
			//      return new Pair<String, Double>(
			//          editDistanceFromOtherSemanticClassesMatches.get(ph),
			//          editDistanceFromOtherSemanticClasses.get(ph));
			Pair<string, double> minD = GetEditDist(otherSemanticClassesWords, ph);
			string minPh = minD.First();
			double minDfinal = minD.Second();
			foreach (string l in labels)
			{
				if (l.Equals(label))
				{
					continue;
				}
				Pair<string, double> editMatch = GetEditDistanceFromThisClass(l, ph, minLen);
				if (editMatch.Second() < minDfinal)
				{
					minDfinal = editMatch.Second();
					minPh = editMatch.First();
				}
			}
			// double minDtotal = editDistMax;
			// String minPh = "";
			// if (minD.second() == editDistMax && ph.contains(" ")) {
			// for (String s : ph.split("\\s+")) {
			// Pair<String, Double> minDSingle = getEditDist(otherSemanticClassesWords, s);
			// if (minDSingle.second() < minDtotal) {
			// minDtotal = minDSingle.second;
			// }
			// minPh += " " + minDSingle.first();
			// }
			// minPh = minPh.trim();
			// } else {
			// }
			System.Diagnostics.Debug.Assert((!minPh.IsEmpty()));
			//    editDistanceFromOtherSemanticClasses.putIfAbsent(ph, minDtotal);
			//    editDistanceFromOtherSemanticClassesMatches.putIfAbsent(ph, minPh);
			return new Pair<string, double>(minPh, minDfinal);
		}

		//  public double getEditDistanceFromEng(String ph, int minLen) {
		//    if (ph.length() < minLen)
		//      return editDistMax;
		//    if (editDistanceFromEnglishWords.containsKey(ph))
		//      return editDistanceFromEnglishWords.get(ph);
		//    Pair<String, Double> d = getEditDist(commonEngWords, ph);
		//    double minD = d.second();
		//    String minPh = d.first();
		//    if (d.second() > 2) {
		//      Pair<String, Double> minD2 = getEditDist(CandidatePhrase.convertToString(otherSemanticClassesWords), ph);
		//      if (minD2.second < minD) {
		//        minD = minD2.second();
		//        minPh = minD2.first();
		//      }
		//    }
		//
		//    editDistanceFromEnglishWords.putIfAbsent(ph, minD);
		//    editDistanceFromEnglishWordsMatches.putIfAbsent(ph, minPh);
		//    return minD;
		//  }
		public virtual ConcurrentHashMap<string, double> GetEditDistanceFromEnglishWords()
		{
			return this.editDistanceFromEnglishWords;
		}

		public virtual ConcurrentHashMap<string, string> GetEditDistanceFromEnglishWordsMatches()
		{
			return this.editDistanceFromEnglishWordsMatches;
		}

		public virtual double GetEditDistanceScoresOtherClass(string label, string g)
		{
			double editDist;
			string editDistPh;
			//    if (editDistanceFromOtherSemanticClasses.containsKey(g)) {
			//      editDist = editDistanceFromOtherSemanticClasses.get(g);
			//      editDistPh = editDistanceFromOtherSemanticClassesMatches.get(g);
			//    } else {
			Pair<string, double> editMatch = GetEditDistanceFromOtherClasses(label, g, 4);
			editDist = editMatch.Second();
			editDistPh = editMatch.First();
			//    }
			System.Diagnostics.Debug.Assert((!editDistPh.IsEmpty()));
			return (editDist == editDistMax ? 1.0 : (editDist / (double)Math.Max(g.Length, editDistPh.Length)));
		}

		/// <summary>1 if lies in edit distance, 0 if not close to any words</summary>
		/// <param name="g"/>
		/// <returns/>
		public virtual double GetEditDistanceScoresOtherClassThreshold(string label, string g)
		{
			double editDistRatio = GetEditDistanceScoresOtherClass(label, g);
			if (editDistRatio < 0.2)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}

		public virtual double GetEditDistanceScoresThisClassThreshold(string label, string g)
		{
			double editDistRatio = GetEditDistanceScoresThisClass(label, g);
			if (editDistRatio < 0.2)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}

		public virtual double GetEditDistanceScoresThisClass(string label, string g)
		{
			double editDist;
			string editDistPh;
			//    if (editDistanceFromThisClass.containsKey(g)) {
			//      editDist = editDistanceFromThisClass.get(g);
			//      editDistPh = editDistanceFromThisClassMatches.get(g);
			//      assert (!editDistPh.isEmpty());
			//    } else {
			//
			Pair<string, double> editMatch = GetEditDistanceFromThisClass(label, g, 4);
			editDist = editMatch.Second();
			editDistPh = editMatch.First();
			System.Diagnostics.Debug.Assert((!editDistPh.IsEmpty()));
			//}
			return ((editDist == editDistMax) ? 1.0 : (editDist / (double)Math.Max(g.Length, editDistPh.Length)));
		}

		public static bool IsFuzzyMatch(string w1, string w2, int minLen4Fuzzy)
		{
			EditDistance editDistance = new EditDistance(true);
			if (w1.Equals(w2))
			{
				return true;
			}
			if (w2.Length > minLen4Fuzzy)
			{
				double d = editDistance.Score(w1, w2);
				if (d == 1)
				{
					return true;
				}
			}
			return false;
		}

		public static CandidatePhrase ContainsFuzzy(ICollection<CandidatePhrase> words, CandidatePhrase w, int minLen4Fuzzy)
		{
			foreach (CandidatePhrase w1 in words)
			{
				if (IsFuzzyMatch(w1.GetPhrase(), w.GetPhrase(), minLen4Fuzzy))
				{
					return w1;
				}
			}
			return null;
		}

		public virtual IDictionary<string, int> GetGeneralWordClassClusters()
		{
			return generalWordClassClusters;
		}

		public virtual void SetGeneralWordClassClusters(IDictionary<string, int> generalWordClassClusters)
		{
			this.generalWordClassClusters = generalWordClassClusters;
		}

		public virtual IDictionary<string, string> GetWordShapeCache()
		{
			return wordShapeCache;
		}

		public virtual IDictionary<string, Type> GetAnswerClass()
		{
			return answerClass;
		}

		public virtual IDictionary<string, IDictionary<Type, object>> GetIgnoreWordswithClassesDuringSelection()
		{
			return ignoreWordswithClassesDuringSelection;
		}

		/// <exception cref="System.Exception"/>
		public virtual void AddSeedWords(string label, ICollection<CandidatePhrase> seeds)
		{
			if (!seedLabelDictionary.Contains(label))
			{
				throw new Exception("label not present in the model");
			}
			ICollection<CandidatePhrase> seedWords = new HashSet<CandidatePhrase>(seedLabelDictionary[label]);
			Sharpen.Collections.AddAll(seedWords, seeds);
			seedLabelDictionary[label] = Java.Util.Collections.UnmodifiableSet(seedWords);
		}
	}
}
