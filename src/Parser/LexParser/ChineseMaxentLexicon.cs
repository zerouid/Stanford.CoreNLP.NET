using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// A Lexicon class that computes the score of word|tag according to a maxent model
	/// of tag|word (divided by MLE estimate of P(tag)).
	/// </summary>
	/// <remarks>
	/// A Lexicon class that computes the score of word|tag according to a maxent model
	/// of tag|word (divided by MLE estimate of P(tag)).
	/// <p/>
	/// It would be nice to factor out a superclass MaxentLexicon that takes a WordFeatureExtractor
	/// </remarks>
	/// <author>Galen Andrew</author>
	[System.Serializable]
	public class ChineseMaxentLexicon : ILexicon
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ChineseMaxentLexicon));

		private const long serialVersionUID = 238834703409896852L;

		private const bool verbose = true;

		public const bool seenTagsOnly = false;

		private ChineseWordFeatureExtractor featExtractor;

		public const bool fixUnkFunctionWords = false;

		private static readonly Pattern wordPattern = Pattern.Compile(".*-W");

		private static readonly Pattern charPattern = Pattern.Compile(".*-.C");

		private static readonly Pattern bigramPattern = Pattern.Compile(".*-.B");

		private static readonly Pattern conjPattern = Pattern.Compile(".*&&.*");

		private readonly Pair<Pattern, int> wordThreshold = new Pair<Pattern, int>(wordPattern, 0);

		private readonly Pair<Pattern, int> charThreshold = new Pair<Pattern, int>(charPattern, 2);

		private readonly Pair<Pattern, int> bigramThreshold = new Pair<Pattern, int>(bigramPattern, 3);

		private readonly Pair<Pattern, int> conjThreshold = new Pair<Pattern, int>(conjPattern, 3);

		private readonly IList<Pair<Pattern, int>> featureThresholds = new List<Pair<Pattern, int>>();

		private readonly int universalThreshold = 0;

		private LinearClassifier scorer;

		private IDictionary<string, string> functionWordTags = Generics.NewHashMap();

		private Distribution<string> tagDist;

		private readonly IIndex<string> wordIndex;

		private readonly IIndex<string> tagIndex;

		[System.NonSerialized]
		private ICounter<string> logProbs;

		private double iteratorCutoffFactor = 4;

		[System.NonSerialized]
		private int lastWord = -1;

		internal string initialWeightFile = null;

		internal bool trainFloat = false;

		private const string featureDir = "gbfeatures";

		private double tol = 1e-4;

		private double sigma = 0.4;

		internal const bool tuneSigma = false;

		internal const int trainCountThreshold = 5;

		internal readonly int featureLevel;

		internal const int DefaultFeatureLevel = 2;

		private bool trainOnLowCount = false;

		private bool trainByType = false;

		private readonly ITreebankLangParserParams tlpParams;

		private readonly ITreebankLanguagePack ctlp;

		private readonly Options op;

		public virtual bool IsKnown(int word)
		{
			return IsKnown(wordIndex.Get(word));
		}

		public virtual bool IsKnown(string word)
		{
			return tagsForWord.Contains(word);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ICollection<string> TagSet(Func<string, string> basicCategoryFunction)
		{
			ICollection<string> tagSet = new HashSet<string>();
			foreach (string tag in tagIndex.ObjectsList())
			{
				tagSet.Add(basicCategoryFunction.Apply(tag));
			}
			return tagSet;
		}

		private void EnsureProbs(int word)
		{
			EnsureProbs(word, true);
		}

		private void EnsureProbs(int word, bool subtractTagScore)
		{
			if (word == lastWord)
			{
				return;
			}
			lastWord = word;
			if (functionWordTags.Contains(wordIndex.Get(word)))
			{
				logProbs = new ClassicCounter<string>();
				string trueTag = functionWordTags[wordIndex.Get(word)];
				foreach (string tag in tagIndex.ObjectsList())
				{
					if (ctlp.BasicCategory(tag).Equals(trueTag))
					{
						logProbs.SetCount(tag, 0);
					}
					else
					{
						logProbs.SetCount(tag, double.NegativeInfinity);
					}
				}
				return;
			}
			IDatum datum = new BasicDatum(featExtractor.MakeFeatures(wordIndex.Get(word)));
			logProbs = scorer.LogProbabilityOf(datum);
			if (subtractTagScore)
			{
				ICollection<string> tagSet = logProbs.KeySet();
				foreach (string tag in tagSet)
				{
					logProbs.IncrementCount(tag, -Math.Log(tagDist.ProbabilityOf(tag)));
				}
			}
		}

		public CollectionValuedMap<string, string> tagsForWord = new CollectionValuedMap<string, string>();

		public virtual IEnumerator<IntTaggedWord> RuleIteratorByWord(int word, int loc, string featureSpec)
		{
			EnsureProbs(word);
			IList<IntTaggedWord> rules = new List<IntTaggedWord>();
			double max = Counters.Max(logProbs);
			for (int tag = 0; tag < tagIndex.Size(); tag++)
			{
				IntTaggedWord iTW = new IntTaggedWord(word, tag);
				double score = logProbs.GetCount(tagIndex.Get(tag));
				if (score > max - iteratorCutoffFactor)
				{
					rules.Add(iTW);
				}
			}
			return rules.GetEnumerator();
		}

		public virtual IEnumerator<IntTaggedWord> RuleIteratorByWord(string word, int loc, string featureSpec)
		{
			return RuleIteratorByWord(wordIndex.IndexOf(word), loc, featureSpec);
		}

		/// <summary>Returns the number of rules (tag rewrites as word) in the Lexicon.</summary>
		/// <remarks>
		/// Returns the number of rules (tag rewrites as word) in the Lexicon.
		/// This method isn't yet implemented in this class.
		/// It currently just returns 0, which may or may not be helpful.
		/// </remarks>
		public virtual int NumRules()
		{
			int accumulated = 0;
			for (int w = 0; w < tot; w++)
			{
				IEnumerator<IntTaggedWord> iter = RuleIteratorByWord(w, 0, null);
				while (iter.MoveNext())
				{
					iter.Current;
					accumulated++;
				}
			}
			return accumulated;
		}

		private string GetTag(string word)
		{
			int iW = wordIndex.AddToIndex(word);
			EnsureProbs(iW, false);
			return Counters.Argmax(logProbs);
		}

		private void Verbose(string s)
		{
			log.Info(s);
		}

		public ChineseMaxentLexicon(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex, int featureLevel)
		{
			this.op = op;
			this.tlpParams = op.tlpParams;
			this.ctlp = op.tlpParams.TreebankLanguagePack();
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
			this.featureLevel = featureLevel;
		}

		[System.NonSerialized]
		internal IntCounter<TaggedWord> datumCounter;

		// only used at training time
		public virtual void InitializeTraining(double numTrees)
		{
			Verbose("Training ChineseMaxentLexicon.");
			Verbose("trainOnLowCount = " + trainOnLowCount + ", trainByType = " + trainByType + ", featureLevel = " + featureLevel + ", tuneSigma = " + tuneSigma);
			Verbose("Making dataset...");
			if (featExtractor == null)
			{
				featExtractor = new ChineseWordFeatureExtractor(featureLevel);
			}
			this.datumCounter = new IntCounter<TaggedWord>();
		}

		/// <summary>Add the given collection of trees to the statistics counted.</summary>
		/// <remarks>
		/// Add the given collection of trees to the statistics counted.  Can
		/// be called multiple times with different trees.
		/// </remarks>
		public void Train(ICollection<Tree> trees)
		{
			Train(trees, 1.0);
		}

		/// <summary>Add the given collection of trees to the statistics counted.</summary>
		/// <remarks>
		/// Add the given collection of trees to the statistics counted.  Can
		/// be called multiple times with different trees.
		/// </remarks>
		public virtual void Train(ICollection<Tree> trees, double weight)
		{
			foreach (Tree tree in trees)
			{
				Train(tree, weight);
			}
		}

		/// <summary>Add the given tree to the statistics counted.</summary>
		/// <remarks>
		/// Add the given tree to the statistics counted.  Can
		/// be called multiple times with different trees.
		/// </remarks>
		public virtual void Train(Tree tree, double weight)
		{
			Train(tree.TaggedYield(), weight);
		}

		/// <summary>Add the given sentence to the statistics counted.</summary>
		/// <remarks>
		/// Add the given sentence to the statistics counted.  Can
		/// be called multiple times with different sentences.
		/// </remarks>
		public virtual void Train(IList<TaggedWord> sentence, double weight)
		{
			featExtractor.Train(sentence, weight);
			foreach (TaggedWord word in sentence)
			{
				datumCounter.IncrementCount(word, weight);
				tagsForWord.Add(word.Word(), word.Tag());
			}
		}

		public virtual void TrainUnannotated(IList<TaggedWord> sentence, double weight)
		{
			// TODO: for now we just punt on these
			throw new NotSupportedException("This version of the parser does not support non-tree training data");
		}

		public virtual void IncrementTreesRead(double weight)
		{
			throw new NotSupportedException();
		}

		public virtual void Train(TaggedWord tw, int loc, double weight)
		{
			throw new NotSupportedException();
		}

		public virtual void FinishTraining()
		{
			IntCounter<string> tagCounter = new IntCounter<string>();
			WeightedDataset data = new WeightedDataset(datumCounter.Size());
			foreach (TaggedWord word in datumCounter.KeySet())
			{
				int count = datumCounter.GetIntCount(word);
				if (trainOnLowCount && count > trainCountThreshold)
				{
					continue;
				}
				if (functionWordTags.Contains(word.Word()))
				{
					continue;
				}
				tagCounter.IncrementCount(word.Tag());
				if (trainByType)
				{
					count = 1;
				}
				data.Add(new BasicDatum(featExtractor.MakeFeatures(word.Word()), word.Tag()), count);
			}
			datumCounter = null;
			tagDist = Distribution.LaplaceSmoothedDistribution(tagCounter, tagCounter.Size(), 0.5);
			tagCounter = null;
			ApplyThresholds(data);
			Verbose("Making classifier...");
			QNMinimizer minim = new QNMinimizer();
			//new ResultStoringMonitor(5, "weights"));
			//    minim.shutUp();
			LinearClassifierFactory factory = new LinearClassifierFactory(minim);
			factory.SetTol(tol);
			factory.SetSigma(sigma);
			scorer = factory.TrainClassifier(data);
			Verbose("Done training.");
		}

		private void ApplyThresholds(WeightedDataset data)
		{
			if (wordThreshold.second > 0)
			{
				featureThresholds.Add(wordThreshold);
			}
			if (featExtractor.chars && charThreshold.second > 0)
			{
				featureThresholds.Add(charThreshold);
			}
			if (featExtractor.bigrams && bigramThreshold.second > 0)
			{
				featureThresholds.Add(bigramThreshold);
			}
			if ((featExtractor.conjunctions || featExtractor.mildConjunctions) && conjThreshold.second > 0)
			{
				featureThresholds.Add(conjThreshold);
			}
			int types = data.NumFeatureTypes();
			if (universalThreshold > 0)
			{
				data.ApplyFeatureCountThreshold(universalThreshold);
			}
			if (featureThresholds.Count > 0)
			{
				data.ApplyFeatureCountThreshold(featureThresholds);
			}
			int numRemoved = types - data.NumFeatureTypes();
			if (numRemoved > 0)
			{
				Verbose("Thresholding removed " + numRemoved + " features.");
			}
		}

		public static void Main(string[] args)
		{
			ITreebankLangParserParams tlpParams = new ChineseTreebankParserParams();
			ITreebankLanguagePack ctlp = tlpParams.TreebankLanguagePack();
			Options op = new Options(tlpParams);
			TreeAnnotator ta = new TreeAnnotator(tlpParams.HeadFinder(), tlpParams, op);
			log.Info("Reading Trees...");
			IFileFilter trainFilter = new NumberRangesFileFilter(args[1], true);
			Treebank trainTreebank = tlpParams.MemoryTreebank();
			trainTreebank.LoadPath(args[0], trainFilter);
			log.Info("Annotating trees...");
			ICollection<Tree> trainTrees = new List<Tree>();
			foreach (Tree tree in trainTreebank)
			{
				trainTrees.Add(ta.TransformTree(tree));
			}
			trainTreebank = null;
			// saves memory
			log.Info("Training lexicon...");
			IIndex<string> wordIndex = new HashIndex<string>();
			IIndex<string> tagIndex = new HashIndex<string>();
			int featureLevel = DefaultFeatureLevel;
			if (args.Length > 3)
			{
				featureLevel = System.Convert.ToInt32(args[3]);
			}
			Edu.Stanford.Nlp.Parser.Lexparser.ChineseMaxentLexicon lex = new Edu.Stanford.Nlp.Parser.Lexparser.ChineseMaxentLexicon(op, wordIndex, tagIndex, featureLevel);
			lex.InitializeTraining(trainTrees.Count);
			lex.Train(trainTrees);
			lex.FinishTraining();
			log.Info("Testing");
			IFileFilter testFilter = new NumberRangesFileFilter(args[2], true);
			Treebank testTreebank = tlpParams.MemoryTreebank();
			testTreebank.LoadPath(args[0], testFilter);
			IList<TaggedWord> testWords = new List<TaggedWord>();
			foreach (Tree t in testTreebank)
			{
				foreach (TaggedWord tw in t.TaggedYield())
				{
					testWords.Add(tw);
				}
			}
			//testWords.addAll(t.taggedYield());
			int[] totalAndCorrect = lex.TestOnTreebank(testWords);
			log.Info("done.");
			System.Console.Out.WriteLine(totalAndCorrect[1] + " correct out of " + totalAndCorrect[0] + " -- ACC: " + ((double)totalAndCorrect[1]) / totalAndCorrect[0]);
		}

		private int[] TestOnTreebank(ICollection<TaggedWord> testWords)
		{
			int[] totalAndCorrect = new int[2];
			totalAndCorrect[0] = 0;
			totalAndCorrect[1] = 0;
			foreach (TaggedWord word in testWords)
			{
				string goldTag = word.Tag();
				string guessTag = ctlp.BasicCategory(GetTag(word.Word()));
				totalAndCorrect[0]++;
				if (goldTag.Equals(guessTag))
				{
					totalAndCorrect[1]++;
				}
			}
			return totalAndCorrect;
		}

		public virtual float Score(IntTaggedWord iTW, int loc, string word, string featureSpec)
		{
			EnsureProbs(iTW.Word());
			double max = Counters.Max(logProbs);
			double score = logProbs.GetCount(iTW.TagString(tagIndex));
			if (score > max - iteratorCutoffFactor)
			{
				return (float)score;
			}
			else
			{
				return float.NegativeInfinity;
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void WriteData(TextWriter w)
		{
			throw new NotSupportedException();
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadData(BufferedReader @in)
		{
			throw new NotSupportedException();
		}

		public virtual IUnknownWordModel GetUnknownWordModel()
		{
			// TODO Auto-generated method stub
			return null;
		}

		public virtual void SetUnknownWordModel(IUnknownWordModel uwm)
		{
		}

		// TODO Auto-generated method stub
		public virtual void Train(ICollection<Tree> trees, ICollection<Tree> rawTrees)
		{
			Train(trees);
		}
	}
}
