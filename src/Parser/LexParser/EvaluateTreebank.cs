using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Text;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class EvaluateTreebank
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.EvaluateTreebank));

		private readonly Options op;

		private readonly ITreeTransformer debinarizer;

		private readonly ITreeTransformer subcategoryStripper;

		private readonly ITreeTransformer collinizer;

		private readonly ITreeTransformer boundaryRemover;

		private readonly ParserGrammar pqFactory;

		internal IList<IEval> evals = null;

		internal IList<IParserQueryEval> parserQueryEvals = null;

		private readonly bool summary;

		private readonly bool tsv;

		private readonly TreeAnnotatorAndBinarizer binarizerOnly;

		internal AbstractEval pcfgLB = null;

		internal AbstractEval pcfgChildSpecific = null;

		internal LeafAncestorEval pcfgLA = null;

		internal AbstractEval pcfgCB = null;

		internal AbstractEval pcfgDA = null;

		internal AbstractEval pcfgTA = null;

		internal AbstractEval depDA = null;

		internal AbstractEval depTA = null;

		internal AbstractEval factLB = null;

		internal AbstractEval factChildSpecific = null;

		internal LeafAncestorEval factLA = null;

		internal AbstractEval factCB = null;

		internal AbstractEval factDA = null;

		internal AbstractEval factTA = null;

		internal AbstractEval pcfgRUO = null;

		internal AbstractEval pcfgCUO = null;

		internal AbstractEval pcfgCatE = null;

		internal AbstractEval.ScoreEval pcfgLL = null;

		internal AbstractEval.ScoreEval depLL = null;

		internal AbstractEval.ScoreEval factLL = null;

		internal AbstractEval kGoodLB = null;

		private readonly IList<BestOfTopKEval> topKEvals = new List<BestOfTopKEval>();

		private int kbestPCFG = 0;

		private int numSkippedEvals = 0;

		private bool saidMemMessage = false;

		/// <summary>The tagger optionally used before parsing.</summary>
		/// <remarks>
		/// The tagger optionally used before parsing.
		/// <br />
		/// We keep it here as a function rather than a MaxentTagger so that
		/// we can distribute a version of the parser that doesn't include
		/// the entire tagger.
		/// </remarks>
		protected internal readonly IFunction<IList<IHasWord>, IList<TaggedWord>> tagger;

		public EvaluateTreebank(LexicalizedParser parser)
			: this(parser.GetOp(), parser.lex, parser)
		{
		}

		public EvaluateTreebank(Options op, ILexicon lex, ParserGrammar pqFactory)
			: this(op, lex, pqFactory, pqFactory.LoadTagger())
		{
		}

		public EvaluateTreebank(Options op, ILexicon lex, ParserGrammar pqFactory, IFunction<IList<IHasWord>, IList<TaggedWord>> tagger)
		{
			// private final Lexicon lex;
			// no annotation
			this.op = op;
			this.debinarizer = new Debinarizer(op.forceCNF);
			this.subcategoryStripper = op.tlpParams.SubcategoryStripper();
			this.evals = Generics.NewArrayList();
			Sharpen.Collections.AddAll(evals, pqFactory.GetExtraEvals());
			this.parserQueryEvals = pqFactory.GetParserQueryEvals();
			// this.lex = lex;
			this.pqFactory = pqFactory;
			this.tagger = tagger;
			collinizer = op.tlpParams.Collinizer();
			boundaryRemover = new BoundaryRemover();
			bool runningAverages = bool.ParseBoolean(op.testOptions.evals.GetProperty("runningAverages"));
			summary = bool.ParseBoolean(op.testOptions.evals.GetProperty("summary"));
			tsv = bool.ParseBoolean(op.testOptions.evals.GetProperty("tsv"));
			if (!op.trainOptions.leftToRight)
			{
				binarizerOnly = new TreeAnnotatorAndBinarizer(op.tlpParams, op.forceCNF, false, false, op);
			}
			else
			{
				binarizerOnly = new TreeAnnotatorAndBinarizer(op.tlpParams.HeadFinder(), new LeftHeadFinder(), op.tlpParams, op.forceCNF, false, false, op);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("pcfgLB")))
			{
				pcfgLB = new Evalb("pcfg LP/LR", runningAverages);
			}
			// TODO: might be nice to allow more than one child-specific scorer
			if (op.testOptions.evals.GetProperty("pcfgChildSpecific") != null)
			{
				string filter = op.testOptions.evals.GetProperty("pcfgChildSpecific");
				pcfgChildSpecific = FilteredEval.ChildFilteredEval("pcfg children matching " + filter + " LP/LR", runningAverages, op.Langpack(), filter);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("pcfgLA")))
			{
				pcfgLA = new LeafAncestorEval("pcfg LeafAncestor");
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("pcfgCB")))
			{
				pcfgCB = new Evalb.CBEval("pcfg CB", runningAverages);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("pcfgDA")))
			{
				pcfgDA = new UnlabeledAttachmentEval("pcfg DA", runningAverages, op.Langpack().HeadFinder());
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("pcfgTA")))
			{
				pcfgTA = new TaggingEval("pcfg Tag", runningAverages, lex);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("depDA")))
			{
				depDA = new UnlabeledAttachmentEval("dep DA", runningAverages, null, op.Langpack().PunctuationWordRejectFilter());
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("depTA")))
			{
				depTA = new TaggingEval("dep Tag", runningAverages, lex);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("factLB")))
			{
				factLB = new Evalb("factor LP/LR", runningAverages);
			}
			if (op.testOptions.evals.GetProperty("factChildSpecific") != null)
			{
				string filter = op.testOptions.evals.GetProperty("factChildSpecific");
				factChildSpecific = FilteredEval.ChildFilteredEval("fact children matching " + filter + " LP/LR", runningAverages, op.Langpack(), filter);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("factLA")))
			{
				factLA = new LeafAncestorEval("factor LeafAncestor");
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("factCB")))
			{
				factCB = new Evalb.CBEval("fact CB", runningAverages);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("factDA")))
			{
				factDA = new UnlabeledAttachmentEval("factor DA", runningAverages, null);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("factTA")))
			{
				factTA = new TaggingEval("factor Tag", runningAverages, lex);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("pcfgRUO")))
			{
				pcfgRUO = new AbstractEval.RuleErrorEval("pcfg Rule under/over");
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("pcfgCUO")))
			{
				pcfgCUO = new AbstractEval.CatErrorEval("pcfg Category under/over");
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("pcfgCatE")))
			{
				pcfgCatE = new EvalbByCat("pcfg Category Eval", runningAverages);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("pcfgLL")))
			{
				pcfgLL = new AbstractEval.ScoreEval("pcfgLL", runningAverages);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("depLL")))
			{
				depLL = new AbstractEval.ScoreEval("depLL", runningAverages);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("factLL")))
			{
				factLL = new AbstractEval.ScoreEval("factLL", runningAverages);
			}
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("topMatch")))
			{
				evals.Add(new TopMatchEval("topMatch", runningAverages));
			}
			// this one is for the various k Good/Best options.  Just for individual results
			kGoodLB = new Evalb("kGood LP/LR", false);
			if (bool.ParseBoolean(op.testOptions.evals.GetProperty("pcfgTopK")))
			{
				topKEvals.Add(new BestOfTopKEval(new Evalb("pcfg top k comparisons", false), new Evalb("pcfg top k LP/LR", runningAverages)));
			}
			if (topKEvals.Count > 0)
			{
				kbestPCFG = op.testOptions.evalPCFGkBest;
			}
			if (op.testOptions.printPCFGkBest > 0)
			{
				kbestPCFG = Math.Max(kbestPCFG, op.testOptions.printPCFGkBest);
			}
		}

		public virtual double GetLBScore()
		{
			if (factLB != null)
			{
				return factLB.GetEvalbF1Percent();
			}
			if (pcfgLB != null)
			{
				return pcfgLB.GetEvalbF1Percent();
			}
			return 0.0;
		}

		public virtual double GetTagScore()
		{
			if (factTA != null)
			{
				return factTA.GetEvalbF1Percent();
			}
			if (pcfgTA != null)
			{
				return pcfgTA.GetEvalbF1Percent();
			}
			return 0.0;
		}

		/// <summary>Remove tree scores, so they don't print.</summary>
		/// <remarks>
		/// Remove tree scores, so they don't print.
		/// <br />
		/// TODO: The printing architecture should be fixed up in the trees package
		/// sometime.
		/// </remarks>
		private static void NanScores(Tree tree)
		{
			tree.SetScore(double.NaN);
			Tree[] kids = tree.Children();
			foreach (Tree kid in kids)
			{
				NanScores(kid);
			}
		}

		/// <summary>Returns the input sentence for the parser.</summary>
		private IList<CoreLabel> GetInputSentence(Tree t)
		{
			if (op.testOptions.forceTags)
			{
				if (op.testOptions.preTag)
				{
					IList<TaggedWord> s = tagger.Apply(t.YieldWords());
					if (op.testOptions.verbose)
					{
						log.Info("Guess tags: " + Arrays.ToString(Sharpen.Collections.ToArray(s)));
						log.Info("Gold tags: " + t.LabeledYield().ToString());
					}
					return SentenceUtils.ToCoreLabelList(s);
				}
				else
				{
					if (op.testOptions.noFunctionalForcing)
					{
						List<IHasWord> s = t.TaggedYield();
						foreach (IHasWord word in s)
						{
							string tag = ((IHasTag)word).Tag();
							tag = tag.Split("-")[0];
							((IHasTag)word).SetTag(tag);
						}
						return SentenceUtils.ToCoreLabelList(s);
					}
					else
					{
						return SentenceUtils.ToCoreLabelList(t.TaggedYield());
					}
				}
			}
			else
			{
				return SentenceUtils.ToCoreLabelList(t.YieldWords());
			}
		}

		public virtual void ProcessResults(IParserQuery pq, Tree goldTree, PrintWriter pwErr, PrintWriter pwOut, PrintWriter pwFileOut, PrintWriter pwStats, TreePrint treePrint)
		{
			if (pq.SaidMemMessage())
			{
				saidMemMessage = true;
			}
			Tree tree;
			IList<IHasWord> sentence = pq.OriginalSentence();
			try
			{
				tree = pq.GetBestParse();
			}
			catch (NoSuchParseException)
			{
				tree = null;
			}
			IList<ScoredObject<Tree>> kbestPCFGTrees = null;
			if (tree != null && kbestPCFG > 0)
			{
				kbestPCFGTrees = pq.GetKBestPCFGParses(kbestPCFG);
			}
			//combo parse goes to pwOut (System.out)
			if (op.testOptions.verbose)
			{
				pwOut.Println("ComboParser best");
				Tree ot = tree;
				if (ot != null && !op.tlpParams.TreebankLanguagePack().IsStartSymbol(ot.Value()))
				{
					ot = ot.TreeFactory().NewTreeNode(op.tlpParams.TreebankLanguagePack().StartSymbol(), Java.Util.Collections.SingletonList(ot));
				}
				treePrint.PrintTree(ot, pwOut);
			}
			else
			{
				treePrint.PrintTree(tree, pwOut);
			}
			// **OUTPUT**
			// print various n-best like outputs (including 1-best)
			// print various statistics
			if (tree != null)
			{
				if (op.testOptions.printAllBestParses)
				{
					IList<ScoredObject<Tree>> parses = pq.GetBestPCFGParses();
					int sz = parses.Count;
					if (sz > 1)
					{
						pwOut.Println("There were " + sz + " best PCFG parses with score " + parses[0].Score() + '.');
						Tree transGoldTree = collinizer.TransformTree(goldTree);
						int iii = 0;
						foreach (ScoredObject<Tree> sot in parses)
						{
							iii++;
							Tree tb = sot.Object();
							Tree tbd = debinarizer.TransformTree(tb);
							tbd = subcategoryStripper.TransformTree(tbd);
							pq.RestoreOriginalWords(tbd);
							pwOut.Println("PCFG Parse #" + iii + " with score " + tbd.Score());
							tbd.PennPrint(pwOut);
							Tree tbtr = collinizer.TransformTree(tbd);
							// pwOut.println("Tree size = " + tbtr.size() + "; depth = " + tbtr.depth());
							kGoodLB.Evaluate(tbtr, transGoldTree, pwErr);
						}
					}
				}
				else
				{
					// Huang and Chiang (2006) Algorithm 3 output from the PCFG parser
					if (op.testOptions.printPCFGkBest > 0 && op.testOptions.outputkBestEquivocation == null)
					{
						IList<ScoredObject<Tree>> trees = kbestPCFGTrees.SubList(0, op.testOptions.printPCFGkBest);
						Tree transGoldTree = collinizer.TransformTree(goldTree);
						int i = 0;
						foreach (ScoredObject<Tree> tp in trees)
						{
							i++;
							pwOut.Println("PCFG Parse #" + i + " with score " + tp.Score());
							Tree tbd = tp.Object();
							tbd.PennPrint(pwOut);
							Tree tbtr = collinizer.TransformTree(tbd);
							kGoodLB.Evaluate(tbtr, transGoldTree, pwErr);
						}
					}
					else
					{
						// Chart parser (factored) n-best list
						if (op.testOptions.printFactoredKGood > 0 && pq.HasFactoredParse())
						{
							// DZ: debug n best trees
							IList<ScoredObject<Tree>> trees = pq.GetKGoodFactoredParses(op.testOptions.printFactoredKGood);
							Tree transGoldTree = collinizer.TransformTree(goldTree);
							int ii = 0;
							foreach (ScoredObject<Tree> tp in trees)
							{
								ii++;
								pwOut.Println("Factored Parse #" + ii + " with score " + tp.Score());
								Tree tbd = tp.Object();
								tbd.PennPrint(pwOut);
								Tree tbtr = collinizer.TransformTree(tbd);
								kGoodLB.Evaluate(tbtr, transGoldTree, pwOut);
							}
						}
						else
						{
							//1-best output
							if (pwFileOut != null)
							{
								pwFileOut.Println(tree.ToString());
							}
						}
					}
				}
				//Print the derivational entropy
				if (op.testOptions.outputkBestEquivocation != null && op.testOptions.printPCFGkBest > 0)
				{
					IList<ScoredObject<Tree>> trees = kbestPCFGTrees.SubList(0, op.testOptions.printPCFGkBest);
					double[] logScores = new double[trees.Count];
					int treeId = 0;
					foreach (ScoredObject<Tree> kBestTree in trees)
					{
						logScores[treeId++] = kBestTree.Score();
					}
					//Re-normalize
					double entropy = 0.0;
					double denom = ArrayMath.LogSum(logScores);
					foreach (double logScore in logScores)
					{
						double logPr = logScore - denom;
						entropy += System.Math.Exp(logPr) * (logPr / System.Math.Log(2));
					}
					entropy *= -1;
					//Convert to bits
					pwStats.Printf("%f\t%d\t%d\n", entropy, trees.Count, sentence.Count);
				}
			}
			// **EVALUATION**
			// Perform various evaluations specified by the user
			if (tree != null)
			{
				//Strip subcategories and remove punctuation for evaluation
				tree = subcategoryStripper.TransformTree(tree);
				Tree treeFact = collinizer.TransformTree(tree);
				//Setup the gold tree
				if (op.testOptions.verbose)
				{
					pwOut.Println("Correct parse");
					treePrint.PrintTree(goldTree, pwOut);
				}
				Tree transGoldTree = collinizer.TransformTree(goldTree);
				if (transGoldTree != null)
				{
					transGoldTree = subcategoryStripper.TransformTree(transGoldTree);
				}
				//Can't do evaluation in these two cases
				if (transGoldTree == null)
				{
					pwErr.Println("Couldn't transform gold tree for evaluation, skipping eval. Gold tree was:");
					goldTree.PennPrint(pwErr);
					numSkippedEvals++;
					return;
				}
				else
				{
					if (treeFact == null)
					{
						pwErr.Println("Couldn't transform hypothesis tree for evaluation, skipping eval. Tree was:");
						tree.PennPrint(pwErr);
						numSkippedEvals++;
						return;
					}
					else
					{
						if (treeFact.Yield().Count != transGoldTree.Yield().Count)
						{
							IList<ILabel> fYield = treeFact.Yield();
							IList<ILabel> gYield = transGoldTree.Yield();
							pwErr.Println("WARNING: Evaluation could not be performed due to gold/parsed yield mismatch.");
							pwErr.Printf("  sizes: gold: %d (transf) %d (orig); parsed: %d (transf) %d (orig).%n", gYield.Count, goldTree.Yield().Count, fYield.Count, tree.Yield().Count);
							pwErr.Println("  gold: " + SentenceUtils.ListToString(gYield, true));
							pwErr.Println("  pars: " + SentenceUtils.ListToString(fYield, true));
							numSkippedEvals++;
							return;
						}
					}
				}
				if (topKEvals.Count > 0)
				{
					IList<Tree> transGuesses = new List<Tree>();
					int kbest = System.Math.Min(op.testOptions.evalPCFGkBest, kbestPCFGTrees.Count);
					foreach (ScoredObject<Tree> guess in kbestPCFGTrees.SubList(0, kbest))
					{
						transGuesses.Add(collinizer.TransformTree(guess.Object()));
					}
					foreach (BestOfTopKEval eval in topKEvals)
					{
						eval.Evaluate(transGuesses, transGoldTree, pwErr);
					}
				}
				//PCFG eval
				Tree treePCFG = pq.GetBestPCFGParse();
				if (treePCFG != null)
				{
					Tree treePCFGeval = collinizer.TransformTree(treePCFG);
					if (pcfgLB != null)
					{
						pcfgLB.Evaluate(treePCFGeval, transGoldTree, pwErr);
					}
					if (pcfgChildSpecific != null)
					{
						pcfgChildSpecific.Evaluate(treePCFGeval, transGoldTree, pwErr);
					}
					if (pcfgLA != null)
					{
						pcfgLA.Evaluate(treePCFGeval, transGoldTree, pwErr);
					}
					if (pcfgCB != null)
					{
						pcfgCB.Evaluate(treePCFGeval, transGoldTree, pwErr);
					}
					if (pcfgDA != null)
					{
						// Re-index the leaves after Collinization, stripping traces, etc.
						treePCFGeval.IndexLeaves(true);
						transGoldTree.IndexLeaves(true);
						pcfgDA.Evaluate(treePCFGeval, transGoldTree, pwErr);
					}
					if (pcfgTA != null)
					{
						pcfgTA.Evaluate(treePCFGeval, transGoldTree, pwErr);
					}
					if (pcfgLL != null && pq.GetPCFGParser() != null)
					{
						pcfgLL.RecordScore(pq.GetPCFGParser(), pwErr);
					}
					if (pcfgRUO != null)
					{
						pcfgRUO.Evaluate(treePCFGeval, transGoldTree, pwErr);
					}
					if (pcfgCUO != null)
					{
						pcfgCUO.Evaluate(treePCFGeval, transGoldTree, pwErr);
					}
					if (pcfgCatE != null)
					{
						pcfgCatE.Evaluate(treePCFGeval, transGoldTree, pwErr);
					}
				}
				//Dependency eval
				// todo: is treeDep really useful here, or should we really use depDAEval tree (debinarized) throughout? We use it for parse, and it sure seems like we could use it for tag eval, but maybe not factDA?
				Tree treeDep = pq.GetBestDependencyParse(false);
				if (treeDep != null)
				{
					Tree goldTreeB = binarizerOnly.TransformTree(goldTree);
					Tree goldTreeEval = goldTree.DeepCopy();
					goldTreeEval.IndexLeaves(true);
					goldTreeEval.PercolateHeads(op.Langpack().HeadFinder());
					Tree depDAEval = pq.GetBestDependencyParse(true);
					depDAEval.IndexLeaves(true);
					depDAEval.PercolateHeadIndices();
					if (depDA != null)
					{
						depDA.Evaluate(depDAEval, goldTreeEval, pwErr);
					}
					if (depTA != null)
					{
						Tree undoneTree = debinarizer.TransformTree(treeDep);
						undoneTree = subcategoryStripper.TransformTree(undoneTree);
						pq.RestoreOriginalWords(undoneTree);
						// pwErr.println("subcategoryStripped tree: " + undoneTree.toStructureDebugString());
						depTA.Evaluate(undoneTree, goldTree, pwErr);
					}
					if (depLL != null && pq.GetDependencyParser() != null)
					{
						depLL.RecordScore(pq.GetDependencyParser(), pwErr);
					}
					Tree factTreeB;
					if (pq.HasFactoredParse())
					{
						factTreeB = pq.GetBestFactoredParse();
					}
					else
					{
						factTreeB = treeDep;
					}
					if (factDA != null)
					{
						factDA.Evaluate(factTreeB, goldTreeB, pwErr);
					}
				}
				//Factored parser (1best) eval
				if (factLB != null)
				{
					factLB.Evaluate(treeFact, transGoldTree, pwErr);
				}
				if (factChildSpecific != null)
				{
					factChildSpecific.Evaluate(treeFact, transGoldTree, pwErr);
				}
				if (factLA != null)
				{
					factLA.Evaluate(treeFact, transGoldTree, pwErr);
				}
				if (factTA != null)
				{
					factTA.Evaluate(tree, boundaryRemover.TransformTree(goldTree), pwErr);
				}
				if (factLL != null && pq.GetFactoredParser() != null)
				{
					factLL.RecordScore(pq.GetFactoredParser(), pwErr);
				}
				if (factCB != null)
				{
					factCB.Evaluate(treeFact, transGoldTree, pwErr);
				}
				foreach (IEval eval_1 in evals)
				{
					eval_1.Evaluate(treeFact, transGoldTree, pwErr);
				}
				if (parserQueryEvals != null)
				{
					foreach (IParserQueryEval eval in parserQueryEvals)
					{
						eval_1.Evaluate(pq, transGoldTree, pwErr);
					}
				}
				if (op.testOptions.evalb)
				{
					// empty out scores just in case
					NanScores(tree);
					EvalbFormatWriter.WriteEVALBline(treeFact, transGoldTree);
				}
			}
			pwErr.Println();
		}

		/// <summary>Test the parser on a treebank.</summary>
		/// <remarks>
		/// Test the parser on a treebank. Parses will be written to stdout, and
		/// various other information will be written to stderr and stdout,
		/// particularly if <code>op.testOptions.verbose</code> is true.
		/// </remarks>
		/// <param name="testTreebank">The treebank to parse</param>
		/// <returns>
		/// The labeled precision/recall F<sub>1</sub> (EVALB measure)
		/// of the parser on the treebank.
		/// </returns>
		public virtual double TestOnTreebank(Treebank testTreebank)
		{
			log.Info("Testing on treebank");
			Timing treebankTotalTimer = new Timing();
			TreePrint treePrint = op.testOptions.TreePrint(op.tlpParams);
			ITreebankLangParserParams tlpParams = op.tlpParams;
			ITreebankLanguagePack tlp = op.Langpack();
			PrintWriter pwOut;
			PrintWriter pwErr;
			if (op.testOptions.quietEvaluation)
			{
				NullOutputStream quiet = new NullOutputStream();
				pwOut = tlpParams.Pw(quiet);
				pwErr = tlpParams.Pw(quiet);
			}
			else
			{
				pwOut = tlpParams.Pw();
				pwErr = tlpParams.Pw(System.Console.Error);
			}
			if (op.testOptions.verbose)
			{
				pwErr.Print("Testing ");
				pwErr.Println(testTreebank.TextualSummary(tlp));
			}
			if (op.testOptions.evalb)
			{
				EvalbFormatWriter.InitEVALBfiles(tlpParams);
			}
			PrintWriter pwFileOut = null;
			if (op.testOptions.writeOutputFiles)
			{
				string fname = op.testOptions.outputFilesPrefix + "." + op.testOptions.outputFilesExtension;
				try
				{
					pwFileOut = op.tlpParams.Pw(new FileOutputStream(fname));
				}
				catch (IOException ioe)
				{
					Sharpen.Runtime.PrintStackTrace(ioe);
				}
			}
			PrintWriter pwStats = null;
			if (op.testOptions.outputkBestEquivocation != null)
			{
				try
				{
					pwStats = op.tlpParams.Pw(new FileOutputStream(op.testOptions.outputkBestEquivocation));
				}
				catch (IOException ioe)
				{
					Sharpen.Runtime.PrintStackTrace(ioe);
				}
			}
			if (op.testOptions.testingThreads != 1)
			{
				MulticoreWrapper<IList<IHasWord>, IParserQuery> wrapper = new MulticoreWrapper<IList<IHasWord>, IParserQuery>(op.testOptions.testingThreads, new ParsingThreadsafeProcessor(pqFactory, pwErr));
				LinkedList<Tree> goldTrees = new LinkedList<Tree>();
				foreach (Tree goldTree in testTreebank)
				{
					IList<IHasWord> sentence = GetInputSentence(goldTree);
					goldTrees.Add(goldTree);
					pwErr.Println("Parsing [len. " + sentence.Count + "]: " + SentenceUtils.ListToString(sentence));
					wrapper.Put(sentence);
					while (wrapper.Peek())
					{
						IParserQuery pq = wrapper.Poll();
						goldTree = goldTrees.Poll();
						ProcessResults(pq, goldTree, pwErr, pwOut, pwFileOut, pwStats, treePrint);
					}
				}
				// for tree iterator
				wrapper.Join();
				while (wrapper.Peek())
				{
					IParserQuery pq = wrapper.Poll();
					Tree goldTree_1 = goldTrees.Poll();
					ProcessResults(pq, goldTree_1, pwErr, pwOut, pwFileOut, pwStats, treePrint);
				}
			}
			else
			{
				IParserQuery pq = pqFactory.ParserQuery();
				foreach (Tree goldTree in testTreebank)
				{
					IList<CoreLabel> sentence = GetInputSentence(goldTree);
					pwErr.Println("Parsing [len. " + sentence.Count + "]: " + SentenceUtils.ListToString(sentence));
					pq.ParseAndReport(sentence, pwErr);
					ProcessResults(pq, goldTree, pwErr, pwOut, pwFileOut, pwStats, treePrint);
				}
			}
			// for tree iterator
			//Done parsing...print the results of the evaluations
			treebankTotalTimer.Done("Testing on treebank");
			if (op.testOptions.quietEvaluation)
			{
				pwErr = tlpParams.Pw(System.Console.Error);
			}
			if (saidMemMessage)
			{
				ParserUtils.PrintOutOfMemory(pwErr);
			}
			if (op.testOptions.evalb)
			{
				EvalbFormatWriter.CloseEVALBfiles();
			}
			if (numSkippedEvals != 0)
			{
				pwErr.Printf("Unable to evaluate %d parser hypotheses due to yield mismatch\n", numSkippedEvals);
			}
			// only created here so we know what parser types are supported...
			IParserQuery pq_1 = pqFactory.ParserQuery();
			if (summary)
			{
				if (pcfgLB != null)
				{
					pcfgLB.Display(false, pwErr);
				}
				if (pcfgChildSpecific != null)
				{
					pcfgChildSpecific.Display(false, pwErr);
				}
				if (pcfgLA != null)
				{
					pcfgLA.Display(false, pwErr);
				}
				if (pcfgCB != null)
				{
					pcfgCB.Display(false, pwErr);
				}
				if (pcfgDA != null)
				{
					pcfgDA.Display(false, pwErr);
				}
				if (pcfgTA != null)
				{
					pcfgTA.Display(false, pwErr);
				}
				if (pcfgLL != null && pq_1.GetPCFGParser() != null)
				{
					pcfgLL.Display(false, pwErr);
				}
				if (depDA != null)
				{
					depDA.Display(false, pwErr);
				}
				if (depTA != null)
				{
					depTA.Display(false, pwErr);
				}
				if (depLL != null && pq_1.GetDependencyParser() != null)
				{
					depLL.Display(false, pwErr);
				}
				if (factLB != null)
				{
					factLB.Display(false, pwErr);
				}
				if (factChildSpecific != null)
				{
					factChildSpecific.Display(false, pwErr);
				}
				if (factLA != null)
				{
					factLA.Display(false, pwErr);
				}
				if (factCB != null)
				{
					factCB.Display(false, pwErr);
				}
				if (factDA != null)
				{
					factDA.Display(false, pwErr);
				}
				if (factTA != null)
				{
					factTA.Display(false, pwErr);
				}
				if (factLL != null && pq_1.GetFactoredParser() != null)
				{
					factLL.Display(false, pwErr);
				}
				if (pcfgCatE != null)
				{
					pcfgCatE.Display(false, pwErr);
				}
				foreach (IEval eval in evals)
				{
					eval.Display(false, pwErr);
				}
				foreach (BestOfTopKEval eval_1 in topKEvals)
				{
					eval_1.Display(false, pwErr);
				}
			}
			// these ones only have a display mode, so display if turned on!!
			if (pcfgRUO != null)
			{
				pcfgRUO.Display(true, pwErr);
			}
			if (pcfgCUO != null)
			{
				pcfgCUO.Display(true, pwErr);
			}
			if (tsv)
			{
				NumberFormat nf = new DecimalFormat("0.00");
				pwErr.Println("factF1\tfactDA\tfactEx\tpcfgF1\tdepDA\tfactTA\tnum");
				if (factLB != null)
				{
					pwErr.Print(nf.Format(factLB.GetEvalbF1Percent()));
				}
				pwErr.Print("\t");
				if (pq_1.GetDependencyParser() != null && factDA != null)
				{
					pwErr.Print(nf.Format(factDA.GetEvalbF1Percent()));
				}
				pwErr.Print("\t");
				if (factLB != null)
				{
					pwErr.Print(nf.Format(factLB.GetExactPercent()));
				}
				pwErr.Print("\t");
				if (pcfgLB != null)
				{
					pwErr.Print(nf.Format(pcfgLB.GetEvalbF1Percent()));
				}
				pwErr.Print("\t");
				if (pq_1.GetDependencyParser() != null && depDA != null)
				{
					pwErr.Print(nf.Format(depDA.GetEvalbF1Percent()));
				}
				pwErr.Print("\t");
				if (pq_1.GetPCFGParser() != null && factTA != null)
				{
					pwErr.Print(nf.Format(factTA.GetEvalbF1Percent()));
				}
				pwErr.Print("\t");
				if (factLB != null)
				{
					pwErr.Print(factLB.GetNum());
				}
				pwErr.Println();
			}
			double f1 = 0.0;
			if (factLB != null)
			{
				f1 = factLB.GetEvalbF1();
			}
			//Close files (if necessary)
			if (pwFileOut != null)
			{
				pwFileOut.Close();
			}
			if (pwStats != null)
			{
				pwStats.Close();
			}
			if (parserQueryEvals != null)
			{
				foreach (IParserQueryEval parserQueryEval in parserQueryEvals)
				{
					parserQueryEval.Display(false, pwErr);
				}
			}
			return f1;
		}
		// end testOnTreebank()
	}
}
