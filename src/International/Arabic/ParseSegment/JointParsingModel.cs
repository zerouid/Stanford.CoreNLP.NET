using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Parsesegment
{
	public class JointParsingModel
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(JointParsingModel));

		private bool Verbose = false;

		private static ExhaustivePCFGParser pparser;

		private static ExhaustiveDependencyParser dparser;

		private BiLexPCFGParser bparser;

		private Options op;

		private LexicalizedParser lp;

		private ITreeTransformer debinarizer;

		private ITreeTransformer subcategoryStripper;

		private TreePrint treePrint;

		private static IList<CoreLabel> bestSegmentationB;

		private bool serInput = false;

		private int maxSentLen = 5000;

		private const int trainLengthLimit = 100000;

		//Factored parsing models (Klein and Manning, 2002)
		//Parser objects
		public virtual void SetVerbose(bool b)
		{
			Verbose = b;
			op.testOptions.verbose = b;
			op.trainOptions.printAnnotatedStateCounts = b;
			op.trainOptions.printAnnotatedRuleCounts = b;
		}

		public virtual void SetSerInput(bool ser_input)
		{
			serInput = ser_input;
		}

		public virtual void SetMaxEvalSentLen(int maxSentLen)
		{
			this.maxSentLen = maxSentLen;
		}

		private void RemoveDeleteSplittersFromSplitters(ITreebankLanguagePack tlp)
		{
			if (op.trainOptions.deleteSplitters != null)
			{
				IList<string> deleted = new List<string>();
				foreach (string del in op.trainOptions.deleteSplitters)
				{
					string baseDel = tlp.BasicCategory(del);
					bool checkBasic = del.Equals(baseDel);
					for (IEnumerator<string> it = op.trainOptions.splitters.GetEnumerator(); it.MoveNext(); )
					{
						string elem = it.Current;
						string baseElem = tlp.BasicCategory(elem);
						bool delStr = checkBasic && baseElem.Equals(baseDel) || elem.Equals(del);
						if (delStr)
						{
							it.Remove();
							deleted.Add(elem);
						}
					}
				}
				if (op.testOptions.verbose)
				{
					log.Info("Removed from vertical splitters: " + deleted);
				}
			}
		}

		public virtual IList<Tree> GetAnnotatedBinaryTreebankFromTreebank(Treebank trainTreebank)
		{
			ITreebankLangParserParams tlpParams = op.tlpParams;
			ITreebankLanguagePack tlp = tlpParams.TreebankLanguagePack();
			if (Verbose)
			{
				log.Info("\n\n" + trainTreebank.TextualSummary(tlp));
			}
			log.Info("Binarizing trees...");
			TreeAnnotatorAndBinarizer binarizer = new TreeAnnotatorAndBinarizer(tlpParams, op.forceCNF, !op.trainOptions.OutsideFactor(), true, op);
			Timing.Tick("done.");
			if (op.trainOptions.selectiveSplit)
			{
				op.trainOptions.splitters = ParentAnnotationStats.GetSplitCategories(trainTreebank, op.trainOptions.tagSelectiveSplit, 0, op.trainOptions.selectiveSplitCutOff, op.trainOptions.tagSelectiveSplitCutOff, tlp);
				RemoveDeleteSplittersFromSplitters(tlp);
				if (op.testOptions.verbose)
				{
					IList<string> list = new List<string>(op.trainOptions.splitters);
					list.Sort();
					log.Info("Parent split categories: " + list);
				}
			}
			//		if (op.trainOptions.selectivePostSplit) {
			//			// Do all the transformations once just to learn selective splits on annotated categories
			//			TreeTransformer myTransformer = new TreeAnnotator(tlpParams.headFinder(), tlpParams);
			//			Treebank annotatedTB = trainTreebank.transform(myTransformer);
			//			op.trainOptions.postSplitters = ParentAnnotationStats.getSplitCategories(annotatedTB, true, 0, op.trainOptions.selectivePostSplitCutOff, op.trainOptions.tagSelectivePostSplitCutOff, tlp);
			//			if (op.testOptions.verbose) {
			//				log.info("Parent post annotation split categories: " + op.trainOptions.postSplitters);
			//			}
			//		}
			if (op.trainOptions.hSelSplit)
			{
				// We run through all the trees once just to gather counts for hSelSplit!
				int ptt = op.trainOptions.printTreeTransformations;
				op.trainOptions.printTreeTransformations = 0;
				binarizer.SetDoSelectiveSplit(false);
				foreach (Tree tree in trainTreebank)
				{
					binarizer.TransformTree(tree);
				}
				binarizer.SetDoSelectiveSplit(true);
				op.trainOptions.printTreeTransformations = ptt;
			}
			//Tree transformation
			//
			IList<Tree> binaryTrainTrees = new List<Tree>();
			foreach (Tree tree_1 in trainTreebank)
			{
				tree_1 = binarizer.TransformTree(tree_1);
				if (tree_1.Yield().Count - 1 <= trainLengthLimit)
				{
					binaryTrainTrees.Add(tree_1);
				}
			}
			// WSGDEBUG: Lot's of stuff on the grammar
			//    if(VERBOSE) {
			//      binarizer.printStateCounts();
			//      binarizer.printRuleCounts();
			//    binarizer.dumpStats();
			//    }
			return binaryTrainTrees;
		}

		public virtual LexicalizedParser GetParserDataFromTreebank(Treebank trainTreebank)
		{
			log.Info("Binarizing training trees...");
			IList<Tree> binaryTrainTrees = GetAnnotatedBinaryTreebankFromTreebank(trainTreebank);
			Timing.Tick("done.");
			IIndex<string> stateIndex = new HashIndex<string>();
			log.Info("Extracting PCFG...");
			IExtractor<Pair<UnaryGrammar, BinaryGrammar>> bgExtractor = new BinaryGrammarExtractor(op, stateIndex);
			Pair<UnaryGrammar, BinaryGrammar> bgug = bgExtractor.Extract(binaryTrainTrees);
			BinaryGrammar bg = bgug.second;
			bg.SplitRules();
			UnaryGrammar ug = bgug.first;
			ug.PurgeRules();
			Timing.Tick("done.");
			log.Info("Extracting Lexicon...");
			IIndex<string> wordIndex = new HashIndex<string>();
			IIndex<string> tagIndex = new HashIndex<string>();
			ILexicon lex = op.tlpParams.Lex(op, wordIndex, tagIndex);
			lex.InitializeTraining(binaryTrainTrees.Count);
			lex.Train(binaryTrainTrees);
			lex.FinishTraining();
			Timing.Tick("done.");
			IExtractor<IDependencyGrammar> dgExtractor = op.tlpParams.DependencyGrammarExtractor(op, wordIndex, tagIndex);
			IDependencyGrammar dg = null;
			if (op.doDep)
			{
				log.Info("Extracting Dependencies...");
				dg = dgExtractor.Extract(binaryTrainTrees);
				dg.SetLexicon(lex);
				Timing.Tick("done.");
			}
			log.Info("Done extracting grammars and lexicon.");
			return new LexicalizedParser(lex, bg, ug, dg, stateIndex, wordIndex, tagIndex, op);
		}

		private void MakeParsers()
		{
			if (lp == null)
			{
				throw new Exception(this.GetType().FullName + ": Parser grammar does not exist");
			}
			//a la (Klein and Manning, 2002)
			pparser = new ExhaustivePCFGParser(lp.bg, lp.ug, lp.lex, op, lp.stateIndex, lp.wordIndex, lp.tagIndex);
			dparser = new ExhaustiveDependencyParser(lp.dg, lp.lex, op, lp.wordIndex, lp.tagIndex);
			bparser = new BiLexPCFGParser(new JointParsingModel.GenericLatticeScorer(), pparser, dparser, lp.bg, lp.ug, lp.dg, lp.lex, op, lp.stateIndex, lp.wordIndex, lp.tagIndex);
		}

		private bool Parse(InputStream inputStream)
		{
			LatticeXMLReader reader = new LatticeXMLReader();
			if (!reader.Load(inputStream, serInput))
			{
				System.Console.Error.Printf("%s: Error loading input lattice xml from stdin%n", this.GetType().FullName);
				return false;
			}
			System.Console.Error.Printf("%s: Entering main parsing loop...%n", this.GetType().FullName);
			int latticeNum = 0;
			int parseable = 0;
			int successes = 0;
			int fParseSucceeded = 0;
			foreach (Lattice lattice in reader)
			{
				if (lattice.GetNumNodes() > op.testOptions.maxLength + 1)
				{
					// + 1 for boundary symbol
					System.Console.Error.Printf("%s: Lattice %d too big! (%d nodes)%n", this.GetType().FullName, latticeNum, lattice.GetNumNodes());
					latticeNum++;
					continue;
				}
				parseable++;
				//TODO This doesn't work for what we want. Check the implementation in ExhaustivePCFG parser
				//op.testOptions.constraints = lattice.getConstraints();
				try
				{
					Tree rawTree = null;
					if (op.doPCFG && pparser.Parse(lattice))
					{
						rawTree = pparser.GetBestParse();
						//1best segmentation
						//bestSegmentationB still has boundary symbol in it
						bestSegmentationB = rawTree.Yield(new List<CoreLabel>());
						// NOTE! Type is need here for JDK8 compilation (maybe bad typing somewhere)
						if (op.doDep && dparser.Parse(bestSegmentationB))
						{
							System.Console.Error.Printf("%s: Dependency parse succeeded!%n", this.GetType().FullName);
							if (bparser.Parse(bestSegmentationB))
							{
								System.Console.Error.Printf("%s: Factored parse succeeded!%n", this.GetType().FullName);
								rawTree = bparser.GetBestParse();
								fParseSucceeded++;
							}
						}
						else
						{
							System.Console.Out.Printf("%s: Dependency parse failed. Backing off to PCFG...%n", this.GetType().FullName);
						}
					}
					else
					{
						System.Console.Out.Printf("%s: WARNING: parsing failed for lattice %d%n", this.GetType().FullName, latticeNum);
					}
					//Post-process the tree
					if (rawTree == null)
					{
						System.Console.Out.Printf("%s: WARNING: Could not extract best parse for lattice %d%n", this.GetType().FullName, latticeNum);
					}
					else
					{
						Tree t = debinarizer.TransformTree(rawTree);
						t = subcategoryStripper.TransformTree(t);
						treePrint.PrintTree(t);
						successes++;
					}
				}
				catch (Exception e)
				{
					//When a best parse can't be extracted
					System.Console.Out.Printf("%s: WARNING: Could not extract best parse for lattice %d%n", this.GetType().FullName, latticeNum);
					Sharpen.Runtime.PrintStackTrace(e);
				}
				latticeNum++;
			}
			log.Info("===================================================================");
			log.Info("===================================================================");
			log.Info("Post mortem:");
			log.Info("  Input:     " + latticeNum);
			log.Info("  Parseable: " + parseable);
			log.Info("  Parsed:    " + successes);
			log.Info("  f_Parsed:  " + fParseSucceeded);
			log.Info("  String %:  " + (int)((double)successes * 10000.0 / (double)parseable) / 100.0);
			return true;
		}

		public virtual bool Run(File trainTreebankFile, File testTreebankFile, InputStream inputStream)
		{
			op = new Options();
			op.tlpParams = new ArabicTreebankParserParams();
			op.SetOptions("-arabicFactored");
			op.testOptions.maxLength = maxSentLen;
			op.testOptions.MaxItems = 5000000;
			//500000 is the default for Arabic, but we have substantially more edges now
			op.testOptions.outputFormatOptions = "removeTopBracket,includePunctuationDependencies";
			// WSG: Just set this to some high value so that extractBestParse()
			// actually calls the lattice reader (e.g., this says that we can't have a word longer than
			// 80 characters...seems sensible for Arabic
			op.testOptions.maxSpanForTags = 80;
			treePrint = op.testOptions.TreePrint(op.tlpParams);
			debinarizer = new Debinarizer(op.forceCNF, new CategoryWordTagFactory());
			subcategoryStripper = op.tlpParams.SubcategoryStripper();
			Timing.StartTime();
			Treebank trainTreebank = op.tlpParams.DiskTreebank();
			trainTreebank.LoadPath(trainTreebankFile);
			lp = GetParserDataFromTreebank(trainTreebank);
			MakeParsers();
			if (Verbose)
			{
				op.Display();
				string lexNumRules = (pparser != null) ? int.ToString(lp.lex.NumRules()) : string.Empty;
				log.Info("Grammar\tStates\tTags\tWords\tUnaryR\tBinaryR\tTaggings");
				log.Info("Grammar\t" + lp.stateIndex.Size() + '\t' + lp.tagIndex.Size() + '\t' + lp.wordIndex.Size() + '\t' + (pparser != null ? lp.ug.NumRules() : string.Empty) + '\t' + (pparser != null ? lp.bg.NumRules() : string.Empty) + '\t' + lexNumRules
					);
				log.Info("ParserPack is " + op.tlpParams.GetType().FullName);
				log.Info("Lexicon is " + lp.lex.GetType().FullName);
			}
			return Parse(inputStream);
		}

		private class GenericLatticeScorer : ILatticeScorer
		{
			/*
			* pparser chart uses segmentation interstices; dparser uses 1best word
			* interstices. Convert between the two here for bparser.
			*/
			public virtual Item ConvertItemSpan(Item item)
			{
				if (bestSegmentationB == null || bestSegmentationB.IsEmpty())
				{
					throw new Exception(this.GetType().FullName + ": No 1best segmentation available");
				}
				item.start = bestSegmentationB[item.start].BeginPosition();
				item.end = bestSegmentationB[item.end - 1].EndPosition();
				return item;
			}

			public virtual double OScore(Edge edge)
			{
				Edge latticeEdge = (Edge)ConvertItemSpan(new Edge(edge));
				double pOscore = pparser.OScore(latticeEdge);
				double dOscore = dparser.OScore(edge);
				return pOscore + dOscore;
			}

			public virtual double IScore(Edge edge)
			{
				Edge latticeEdge = (Edge)ConvertItemSpan(new Edge(edge));
				double pIscore = pparser.IScore(latticeEdge);
				double dIscore = dparser.IScore(edge);
				return pIscore + dIscore;
			}

			public virtual bool OPossible(Hook hook)
			{
				Hook latticeHook = (Hook)ConvertItemSpan(new Hook(hook));
				return pparser.OPossible(latticeHook) && dparser.OPossible(hook);
			}

			public virtual bool IPossible(Hook hook)
			{
				Hook latticeHook = (Hook)ConvertItemSpan(new Hook(hook));
				return pparser.IPossible(latticeHook) && dparser.IPossible(hook);
			}

			public virtual bool Parse<_T0>(IList<_T0> words)
				where _T0 : IHasWord
			{
				throw new NotSupportedException(this.GetType().FullName + ": Does not support parse operation.");
			}
		}
	}
}
