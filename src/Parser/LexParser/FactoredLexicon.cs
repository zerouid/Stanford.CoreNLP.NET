using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.International.Arabic;
using Edu.Stanford.Nlp.International.French;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Spence Green</author>
	[System.Serializable]
	public class FactoredLexicon : BaseLexicon
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.FactoredLexicon));

		private const long serialVersionUID = -744693222804176489L;

		private const bool Debug = false;

		private MorphoFeatureSpecification morphoSpec;

		private const string NoMorphAnalysis = "xXxNONExXx";

		private IIndex<string> morphIndex = new HashIndex<string>();

		private TwoDimensionalIntCounter<int, int> wordTag = new TwoDimensionalIntCounter<int, int>(40000);

		private ICounter<int> wordTagUnseen = new ClassicCounter<int>(500);

		private TwoDimensionalIntCounter<int, int> lemmaTag = new TwoDimensionalIntCounter<int, int>(40000);

		private ICounter<int> lemmaTagUnseen = new ClassicCounter<int>(500);

		private TwoDimensionalIntCounter<int, int> morphTag = new TwoDimensionalIntCounter<int, int>(500);

		private ICounter<int> morphTagUnseen = new ClassicCounter<int>(500);

		private ICounter<int> tagCounter = new ClassicCounter<int>(300);

		public FactoredLexicon(MorphoFeatureSpecification morphoSpec, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: base(wordIndex, tagIndex)
		{
			this.morphoSpec = morphoSpec;
		}

		public FactoredLexicon(Options op, MorphoFeatureSpecification morphoSpec, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: base(op, wordIndex, tagIndex)
		{
			this.morphoSpec = morphoSpec;
		}

		/// <summary>Rule table is lemmas.</summary>
		/// <remarks>Rule table is lemmas. So isKnown() is slightly trickier.</remarks>
		public override IEnumerator<IntTaggedWord> RuleIteratorByWord(int word, int loc, string featureSpec)
		{
			if (word == wordIndex.IndexOf(LexiconConstants.Boundary))
			{
				// Deterministic tagging of the boundary symbol
				return rulesWithWord[word].GetEnumerator();
			}
			else
			{
				if (IsKnown(word))
				{
					// Strict lexical tagging for seen *lemma* types
					// We need to copy the word form into the rules, which currently have lemmas in them
					return rulesWithWord[word].GetEnumerator();
				}
				else
				{
					// Unknown word signatures
					ICollection<IntTaggedWord> lexRules = Generics.NewHashSet(10);
					IList<IntTaggedWord> uwRules = rulesWithWord[wordIndex.IndexOf(LexiconConstants.UnknownWord)];
					// Inject the word into these rules instead of the UW signature
					foreach (IntTaggedWord iTW in uwRules)
					{
						lexRules.Add(new IntTaggedWord(word, iTW.tag));
					}
					return lexRules.GetEnumerator();
				}
			}
		}

		public override float Score(IntTaggedWord iTW, int loc, string word, string featureSpec)
		{
			int wordId = iTW.Word();
			int tagId = iTW.Tag();
			// Force 1-best path to go through the boundary symbol
			// (deterministic tagging)
			int boundaryId = wordIndex.IndexOf(LexiconConstants.Boundary);
			int boundaryTagId = tagIndex.IndexOf(LexiconConstants.BoundaryTag);
			if (wordId == boundaryId && tagId == boundaryTagId)
			{
				return 0.0f;
			}
			// Morphological features
			string tag = tagIndex.Get(iTW.Tag());
			Pair<string, string> lemmaMorph = MorphoFeatureSpecification.SplitMorphString(word, featureSpec);
			string lemma = lemmaMorph.First();
			int lemmaId = wordIndex.IndexOf(lemma);
			string richMorphTag = lemmaMorph.Second();
			string reducedMorphTag = morphoSpec.StrToFeatures(richMorphTag).ToString().Trim();
			reducedMorphTag = reducedMorphTag.Length == 0 ? NoMorphAnalysis : reducedMorphTag;
			int morphId = morphIndex.AddToIndex(reducedMorphTag);
			// Score the factors and create the rule score p_W_T
			double p_W_Tf = Math.Log(ProbWordTag(word, loc, wordId, tagId));
			//    double p_L_T = Math.log(probLemmaTag(word, loc, tagId, lemmaId));
			double p_L_T = 0.0;
			double p_M_T = Math.Log(ProbMorphTag(tagId, morphId));
			double p_W_T = p_W_Tf + p_L_T + p_M_T;
			//      String tag = tagIndex.get(tagId);
			// Filter low probability taggings
			return p_W_T > -100.0 ? (float)p_W_T : float.NegativeInfinity;
		}

		private double ProbWordTag(string word, int loc, int wordId, int tagId)
		{
			double cW = wordTag.TotalCount(wordId);
			double cWT = wordTag.GetCount(wordId, tagId);
			// p_L
			double p_W = cW / wordTag.TotalCount();
			// p_T
			double cTseen = tagCounter.GetCount(tagId);
			double p_T = cTseen / tagCounter.TotalCount();
			// p_T_L
			double p_W_T = 0.0;
			if (cW > 0.0)
			{
				// Seen lemma
				double p_T_W = 0.0;
				if (cW > 100.0 && cWT > 0.0)
				{
					p_T_W = cWT / cW;
				}
				else
				{
					double cTunseen = wordTagUnseen.GetCount(tagId);
					// TODO p_T_U is 0?
					double p_T_U = cTunseen / wordTagUnseen.TotalCount();
					p_T_W = (cWT + smooth[1] * p_T_U) / (cW + smooth[1]);
				}
				p_W_T = p_T_W * p_W / p_T;
			}
			else
			{
				// Unseen word. Score based on the word signature (of the surface form)
				IntTaggedWord iTW = new IntTaggedWord(wordId, tagId);
				double c_T = tagCounter.GetCount(tagId);
				p_W_T = Math.Exp(GetUnknownWordModel().Score(iTW, loc, c_T, tagCounter.TotalCount(), smooth[0], word));
			}
			return p_W_T;
		}

		/// <summary>This method should never return 0!!</summary>
		private double ProbLemmaTag(string word, int loc, int tagId, int lemmaId)
		{
			double cL = lemmaTag.TotalCount(lemmaId);
			double cLT = lemmaTag.GetCount(lemmaId, tagId);
			// p_L
			double p_L = cL / lemmaTag.TotalCount();
			// p_T
			double cTseen = tagCounter.GetCount(tagId);
			double p_T = cTseen / tagCounter.TotalCount();
			// p_T_L
			double p_L_T = 0.0;
			if (cL > 0.0)
			{
				// Seen lemma
				double p_T_L = 0.0;
				if (cL > 100.0 && cLT > 0.0)
				{
					p_T_L = cLT / cL;
				}
				else
				{
					double cTunseen = lemmaTagUnseen.GetCount(tagId);
					// TODO(spenceg): p_T_U is 0??
					double p_T_U = cTunseen / lemmaTagUnseen.TotalCount();
					p_T_L = (cLT + smooth[1] * p_T_U) / (cL + smooth[1]);
				}
				p_L_T = p_T_L * p_L / p_T;
			}
			else
			{
				// Unseen lemma. Score based on the word signature (of the surface form)
				// Hack
				double cTunseen = lemmaTagUnseen.GetCount(tagId);
				p_L_T = cTunseen / tagCounter.TotalCount();
			}
			//      int wordId = wordIndex.indexOf(word);
			//      IntTaggedWord iTW = new IntTaggedWord(wordId, tagId);
			//      double c_T = tagCounter.getCount(tagId);
			//      p_L_T = Math.exp(getUnknownWordModel().score(iTW, loc, c_T, tagCounter.totalCount(), smooth[0], word));
			return p_L_T;
		}

		/// <summary>This method should never return 0!</summary>
		private double ProbMorphTag(int tagId, int morphId)
		{
			double cM = morphTag.TotalCount(morphId);
			double cMT = morphTag.GetCount(morphId, tagId);
			// p_M
			double p_M = cM / morphTag.TotalCount();
			// p_T
			double cTseen = tagCounter.GetCount(tagId);
			double p_T = cTseen / tagCounter.TotalCount();
			double p_M_T = 0.0;
			if (cM > 100.0 && cMT > 0.0)
			{
				double p_T_M = cMT / cM;
				//      else {
				//        double cTunseen = morphTagUnseen.getCount(tagId);
				//        double p_T_U = cTunseen / morphTagUnseen.totalCount();
				//        p_T_M = (cMT + smooth[1]*p_T_U) / (cM + smooth[1]);
				//      }
				p_M_T = p_T_M * p_M / p_T;
			}
			else
			{
				// Unseen morphological analysis
				// Hack....unseen morph tags are extremely rare
				// Add+1 smoothing
				p_M_T = 1.0 / (morphTag.TotalCount() + tagIndex.Size() + 1.0);
			}
			return p_M_T;
		}

		/// <summary>This method should populate wordIndex, tagIndex, and morphIndex.</summary>
		public override void Train(ICollection<Tree> trees, ICollection<Tree> rawTrees)
		{
			double weight = 1.0;
			// Train uw model on words
			uwModelTrainer.Train(trees, weight);
			double numTrees = trees.Count;
			IEnumerator<Tree> rawTreesItr = rawTrees == null ? null : rawTrees.GetEnumerator();
			IEnumerator<Tree> treeItr = trees.GetEnumerator();
			// Train factored lexicon on lemmas and morph tags
			int treeId = 0;
			while (treeItr.MoveNext())
			{
				Tree tree = treeItr.Current;
				// CoreLabels, with morph analysis in the originalText annotation
				IList<ILabel> yield = rawTrees == null ? tree.Yield() : rawTreesItr.Current.Yield();
				// Annotated, binarized tree for the tags (labels are usually CategoryWordTag)
				IList<ILabel> pretermYield = tree.PreTerminalYield();
				int yieldLen = yield.Count;
				for (int i = 0; i < yieldLen; ++i)
				{
					string word = yield[i].Value();
					int wordId = wordIndex.AddToIndex(word);
					// Don't do anything with words
					string tag = pretermYield[i].Value();
					int tagId = tagIndex.AddToIndex(tag);
					// Use the word as backup if there is no lemma
					string featureStr = ((CoreLabel)yield[i]).OriginalText();
					Pair<string, string> lemmaMorph = MorphoFeatureSpecification.SplitMorphString(word, featureStr);
					string lemma = lemmaMorph.First();
					int lemmaId = wordIndex.AddToIndex(lemma);
					string richMorphTag = lemmaMorph.Second();
					string reducedMorphTag = morphoSpec.StrToFeatures(richMorphTag).ToString().Trim();
					reducedMorphTag = reducedMorphTag.IsEmpty() ? NoMorphAnalysis : reducedMorphTag;
					int morphId = morphIndex.AddToIndex(reducedMorphTag);
					// Seen event counts
					wordTag.IncrementCount(wordId, tagId);
					lemmaTag.IncrementCount(lemmaId, tagId);
					morphTag.IncrementCount(morphId, tagId);
					tagCounter.IncrementCount(tagId);
					// Unseen event counts
					if (treeId > op.trainOptions.fractionBeforeUnseenCounting * numTrees)
					{
						if (!wordTag.FirstKeySet().Contains(wordId) || wordTag.GetCounter(wordId).TotalCount() < 2)
						{
							wordTagUnseen.IncrementCount(tagId);
						}
						if (!lemmaTag.FirstKeySet().Contains(lemmaId) || lemmaTag.GetCounter(lemmaId).TotalCount() < 2)
						{
							lemmaTagUnseen.IncrementCount(tagId);
						}
						if (!morphTag.FirstKeySet().Contains(morphId) || morphTag.GetCounter(morphId).TotalCount() < 2)
						{
							morphTagUnseen.IncrementCount(tagId);
						}
					}
				}
				++treeId;
				if (Debug && (treeId % 100) == 0)
				{
					System.Console.Error.Printf("[%d]", treeId);
				}
				if (Debug && (treeId % 10000) == 0)
				{
					log.Info();
				}
			}
		}

		/// <summary>Rule table is lemmas!</summary>
		protected internal override void InitRulesWithWord()
		{
			// Add synthetic symbols to the indices
			int unkWord = wordIndex.AddToIndex(LexiconConstants.UnknownWord);
			int boundaryWordId = wordIndex.AddToIndex(LexiconConstants.Boundary);
			int boundaryTagId = tagIndex.AddToIndex(LexiconConstants.BoundaryTag);
			// Initialize rules table
			int numWords = wordIndex.Size();
			rulesWithWord = new IList[numWords];
			for (int w = 0; w < numWords; w++)
			{
				rulesWithWord[w] = new List<IntTaggedWord>(1);
			}
			// Collect rules, indexed by word
			ICollection<IntTaggedWord> lexRules = Generics.NewHashSet(40000);
			foreach (int wordId in wordTag.FirstKeySet())
			{
				foreach (int tagId in wordTag.GetCounter(wordId).KeySet())
				{
					lexRules.Add(new IntTaggedWord(wordId, tagId));
					lexRules.Add(new IntTaggedWord(nullWord, tagId));
				}
			}
			// Known words and signatures
			foreach (IntTaggedWord iTW in lexRules)
			{
				if (iTW.Word() == nullWord)
				{
					// Mix in UW signature rules for open class types
					double types = uwModel.UnSeenCounter().GetCount(iTW);
					if (types > trainOptions.openClassTypesThreshold)
					{
						IntTaggedWord iTU = new IntTaggedWord(unkWord, iTW.tag);
						if (!rulesWithWord[unkWord].Contains(iTU))
						{
							rulesWithWord[unkWord].Add(iTU);
						}
					}
				}
				else
				{
					// Known word
					rulesWithWord[iTW.word].Add(iTW);
				}
			}
			log.Info("The " + rulesWithWord[unkWord].Count + " open class tags are: [");
			foreach (IntTaggedWord item in rulesWithWord[unkWord])
			{
				log.Info(" " + tagIndex.Get(item.Tag()));
			}
			log.Info(" ] ");
			// Boundary symbol has one tagging
			rulesWithWord[boundaryWordId].Add(new IntTaggedWord(boundaryWordId, boundaryTagId));
		}

		/// <summary>
		/// Convert a treebank to factored lexicon events for fast iteration in the
		/// optimizer.
		/// </summary>
		private static IList<FactoredLexiconEvent> TreebankToLexiconEvents(IList<Tree> treebank, Edu.Stanford.Nlp.Parser.Lexparser.FactoredLexicon lexicon)
		{
			IList<FactoredLexiconEvent> events = new List<FactoredLexiconEvent>(70000);
			foreach (Tree tree in treebank)
			{
				IList<ILabel> yield = tree.Yield();
				IList<ILabel> preterm = tree.PreTerminalYield();
				System.Diagnostics.Debug.Assert(yield.Count == preterm.Count);
				int yieldLen = yield.Count;
				for (int i = 0; i < yieldLen; ++i)
				{
					string tag = preterm[i].Value();
					int tagId = lexicon.tagIndex.IndexOf(tag);
					string word = yield[i].Value();
					int wordId = lexicon.wordIndex.IndexOf(word);
					// Two checks to see if we keep this example
					if (tagId < 0)
					{
						log.Info("Discarding training example: " + word + " " + tag);
						continue;
					}
					//        if (counts.probWordTag(wordId, tagId) == 0.0) {
					//          log.info("Discarding low counts <w,t> pair: " + word + " " + tag);
					//          continue;
					//        }
					string featureStr = ((CoreLabel)yield[i]).OriginalText();
					Pair<string, string> lemmaMorph = MorphoFeatureSpecification.SplitMorphString(word, featureStr);
					string lemma = lemmaMorph.First();
					string richTag = lemmaMorph.Second();
					string reducedTag = lexicon.morphoSpec.StrToFeatures(richTag).ToString();
					reducedTag = reducedTag.Length == 0 ? NoMorphAnalysis : reducedTag;
					int lemmaId = lexicon.wordIndex.IndexOf(lemma);
					int morphId = lexicon.morphIndex.IndexOf(reducedTag);
					FactoredLexiconEvent @event = new FactoredLexiconEvent(wordId, tagId, lemmaId, morphId, i, word, featureStr);
					events.Add(@event);
				}
			}
			return events;
		}

		private static IList<FactoredLexiconEvent> GetTuningSet(Treebank devTreebank, Edu.Stanford.Nlp.Parser.Lexparser.FactoredLexicon lexicon, ITreebankLangParserParams tlpp)
		{
			IList<Tree> devTrees = new List<Tree>(3000);
			foreach (Tree tree in devTreebank)
			{
				foreach (Tree subTree in tree)
				{
					if (!subTree.IsLeaf())
					{
						tlpp.TransformTree(subTree, tree);
					}
				}
				devTrees.Add(tree);
			}
			IList<FactoredLexiconEvent> tuningSet = TreebankToLexiconEvents(devTrees, lexicon);
			return tuningSet;
		}

		private static Options GetOptions(Language language)
		{
			Options options = new Options();
			if (language.Equals(Language.Arabic))
			{
				options.lexOptions.useUnknownWordSignatures = 9;
				options.lexOptions.unknownPrefixSize = 1;
				options.lexOptions.unknownSuffixSize = 1;
				options.lexOptions.uwModelTrainer = "edu.stanford.nlp.parser.lexparser.ArabicUnknownWordModelTrainer";
			}
			else
			{
				if (language.Equals(Language.French))
				{
					options.lexOptions.useUnknownWordSignatures = 1;
					options.lexOptions.unknownPrefixSize = 1;
					options.lexOptions.unknownSuffixSize = 2;
					options.lexOptions.uwModelTrainer = "edu.stanford.nlp.parser.lexparser.FrenchUnknownWordModelTrainer";
				}
				else
				{
					throw new NotSupportedException();
				}
			}
			return options;
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 4)
			{
				System.Console.Error.Printf("Usage: java %s language features train_file dev_file%n", typeof(Edu.Stanford.Nlp.Parser.Lexparser.FactoredLexicon).FullName);
				System.Environment.Exit(-1);
			}
			// Command line options
			Language language = Language.ValueOf(args[0]);
			ITreebankLangParserParams tlpp = language.@params;
			Treebank trainTreebank = tlpp.DiskTreebank();
			trainTreebank.LoadPath(args[2]);
			Treebank devTreebank = tlpp.DiskTreebank();
			devTreebank.LoadPath(args[3]);
			MorphoFeatureSpecification morphoSpec;
			Options options = GetOptions(language);
			if (language.Equals(Language.Arabic))
			{
				morphoSpec = new ArabicMorphoFeatureSpecification();
				string[] languageOptions = new string[] { "-arabicFactored" };
				tlpp.SetOptionFlag(languageOptions, 0);
			}
			else
			{
				if (language.Equals(Language.French))
				{
					morphoSpec = new FrenchMorphoFeatureSpecification();
					string[] languageOptions = new string[] { "-frenchFactored" };
					tlpp.SetOptionFlag(languageOptions, 0);
				}
				else
				{
					throw new NotSupportedException();
				}
			}
			string featureList = args[1];
			string[] features = featureList.Trim().Split(",");
			foreach (string feature in features)
			{
				morphoSpec.Activate(MorphoFeatureSpecification.MorphoFeatureType.ValueOf(feature));
			}
			System.Console.Out.WriteLine("Language: " + language.ToString());
			System.Console.Out.WriteLine("Features: " + args[1]);
			// Create word and tag indices
			// Save trees in a collection since the interface requires that....
			System.Console.Out.Write("Loading training trees...");
			IList<Tree> trainTrees = new List<Tree>(19000);
			IIndex<string> wordIndex = new HashIndex<string>();
			IIndex<string> tagIndex = new HashIndex<string>();
			foreach (Tree tree in trainTreebank)
			{
				foreach (Tree subTree in tree)
				{
					if (!subTree.IsLeaf())
					{
						tlpp.TransformTree(subTree, tree);
					}
				}
				trainTrees.Add(tree);
			}
			System.Console.Out.Printf("Done! (%d trees)%n", trainTrees.Count);
			// Setup and train the lexicon.
			System.Console.Out.Write("Collecting sufficient statistics for lexicon...");
			Edu.Stanford.Nlp.Parser.Lexparser.FactoredLexicon lexicon = new Edu.Stanford.Nlp.Parser.Lexparser.FactoredLexicon(options, morphoSpec, wordIndex, tagIndex);
			lexicon.InitializeTraining(trainTrees.Count);
			lexicon.Train(trainTrees, null);
			lexicon.FinishTraining();
			System.Console.Out.WriteLine("Done!");
			trainTrees = null;
			// Load the tuning set
			System.Console.Out.Write("Loading tuning set...");
			IList<FactoredLexiconEvent> tuningSet = GetTuningSet(devTreebank, lexicon, tlpp);
			System.Console.Out.Printf("...Done! (%d events)%n", tuningSet.Count);
			// Print the probabilities that we obtain
			// TODO(spenceg): Implement tagging accuracy with FactLex
			int nCorrect = 0;
			ICounter<string> errors = new ClassicCounter<string>();
			foreach (FactoredLexiconEvent @event in tuningSet)
			{
				IEnumerator<IntTaggedWord> itr = lexicon.RuleIteratorByWord(@event.Word(), @event.GetLoc(), @event.FeatureStr());
				ICounter<int> logScores = new ClassicCounter<int>();
				bool noRules = true;
				int goldTagId = -1;
				while (itr.MoveNext())
				{
					noRules = false;
					IntTaggedWord iTW = itr.Current;
					if (iTW.Tag() == @event.TagId())
					{
						log.Info("GOLD-");
						goldTagId = iTW.Tag();
					}
					float tagScore = lexicon.Score(iTW, @event.GetLoc(), @event.Word(), @event.FeatureStr());
					logScores.IncrementCount(iTW.Tag(), tagScore);
				}
				if (noRules)
				{
					System.Console.Error.Printf("NO TAGGINGS: %s %s%n", @event.Word(), @event.FeatureStr());
				}
				else
				{
					// Score the tagging
					int hypTagId = Counters.Argmax(logScores);
					if (hypTagId == goldTagId)
					{
						++nCorrect;
					}
					else
					{
						string goldTag = goldTagId < 0 ? "UNSEEN" : lexicon.tagIndex.Get(goldTagId);
						errors.IncrementCount(goldTag);
					}
				}
				log.Info();
			}
			// Output accuracy
			double acc = (double)nCorrect / (double)tuningSet.Count;
			System.Console.Error.Printf("%n%nACCURACY: %.2f%n%n", acc * 100.0);
			log.Info("% of errors by type:");
			IList<string> biggestKeys = new List<string>(errors.KeySet());
			biggestKeys.Sort(Counters.ToComparator(errors, false, true));
			Counters.Normalize(errors);
			foreach (string key in biggestKeys)
			{
				System.Console.Error.Printf("%s\t%.2f%n", key, errors.GetCount(key) * 100.0);
			}
		}
	}
}
