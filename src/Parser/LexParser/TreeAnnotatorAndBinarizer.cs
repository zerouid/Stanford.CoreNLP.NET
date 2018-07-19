using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class TreeAnnotatorAndBinarizer : ITreeTransformer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.TreeAnnotatorAndBinarizer));

		private readonly ITreeFactory tf;

		private readonly ITreebankLanguagePack tlp;

		private readonly ITreeTransformer annotator;

		private readonly TreeBinarizer binarizer;

		private readonly PostSplitter postSplitter;

		private readonly bool forceCNF;

		private readonly TrainOptions trainOptions;

		private readonly ClassicCounter<Tree> annotatedRuleCounts;

		private readonly ClassicCounter<string> annotatedStateCounts;

		public TreeAnnotatorAndBinarizer(ITreebankLangParserParams tlpParams, bool forceCNF, bool insideFactor, bool doSubcategorization, Options op)
			: this(tlpParams.HeadFinder(), tlpParams.HeadFinder(), tlpParams, forceCNF, insideFactor, doSubcategorization, op)
		{
		}

		public TreeAnnotatorAndBinarizer(IHeadFinder annotationHF, IHeadFinder binarizationHF, ITreebankLangParserParams tlpParams, bool forceCNF, bool insideFactor, bool doSubcategorization, Options op)
		{
			this.trainOptions = op.trainOptions;
			if (doSubcategorization)
			{
				annotator = new TreeAnnotator(annotationHF, tlpParams, op);
			}
			else
			{
				annotator = new TreeAnnotatorAndBinarizer.TreeNullAnnotator(annotationHF);
			}
			binarizer = new TreeBinarizer(binarizationHF, tlpParams.TreebankLanguagePack(), insideFactor, trainOptions.markovFactor, trainOptions.markovOrder, trainOptions.CompactGrammar() > 0, trainOptions.CompactGrammar() > 1, trainOptions.HselCut, trainOptions
				.markFinalStates, trainOptions.simpleBinarizedLabels, trainOptions.noRebinarization);
			if (trainOptions.selectivePostSplit)
			{
				postSplitter = new PostSplitter(tlpParams, op);
			}
			else
			{
				postSplitter = null;
			}
			this.tf = new LabeledScoredTreeFactory(new CategoryWordTagFactory());
			this.tlp = tlpParams.TreebankLanguagePack();
			this.forceCNF = forceCNF;
			if (trainOptions.printAnnotatedRuleCounts)
			{
				annotatedRuleCounts = new ClassicCounter<Tree>();
			}
			else
			{
				annotatedRuleCounts = null;
			}
			if (trainOptions.printAnnotatedStateCounts)
			{
				annotatedStateCounts = new ClassicCounter<string>();
			}
			else
			{
				annotatedStateCounts = null;
			}
		}

		public virtual void DumpStats()
		{
			if (trainOptions.selectivePostSplit)
			{
				postSplitter.DumpStats();
			}
		}

		public virtual void SetDoSelectiveSplit(bool doSelectiveSplit)
		{
			binarizer.SetDoSelectiveSplit(doSelectiveSplit);
		}

		/// <summary>Changes the ROOT label, and adds a Lexicon.BOUNDARY daughter to it.</summary>
		/// <remarks>
		/// Changes the ROOT label, and adds a Lexicon.BOUNDARY daughter to it.
		/// This is needed for the dependency parser.
		/// <i>Note:</i> This is a destructive operation on the tree passed in!!
		/// </remarks>
		/// <param name="t">The current tree into which a boundary is inserted</param>
		public virtual void AddRoot(Tree t)
		{
			if (t.IsLeaf())
			{
				log.Info("Warning: tree is leaf: " + t);
				t = tf.NewTreeNode(tlp.StartSymbol(), Collections.SingletonList(t));
			}
			t.SetLabel(new CategoryWordTag(tlp.StartSymbol(), LexiconConstants.Boundary, LexiconConstants.BoundaryTag));
			IList<Tree> preTermChildList = new List<Tree>();
			Tree boundaryTerm = tf.NewLeaf(new Word(LexiconConstants.Boundary));
			//CategoryWordTag(Lexicon.BOUNDARY,Lexicon.BOUNDARY,""));
			preTermChildList.Add(boundaryTerm);
			Tree boundaryPreTerm = tf.NewTreeNode(new CategoryWordTag(LexiconConstants.BoundaryTag, LexiconConstants.Boundary, LexiconConstants.BoundaryTag), preTermChildList);
			IList<Tree> childList = t.GetChildrenAsList();
			childList.Add(boundaryPreTerm);
			t.SetChildren(childList);
		}

		/// <summary>
		/// The tree t is normally expected to be a Penn-Treebank-style tree
		/// in which the top node is an extra node that has a unary expansion.
		/// </summary>
		/// <remarks>
		/// The tree t is normally expected to be a Penn-Treebank-style tree
		/// in which the top node is an extra node that has a unary expansion.
		/// If this isn't the case, an extra node is added and the user is warned.
		/// </remarks>
		public virtual Tree TransformTree(Tree t)
		{
			if (trainOptions.printTreeTransformations > 0)
			{
				TrainOptions.PrintTrainTree(null, "ORIGINAL TREE:", t);
			}
			Tree trTree = annotator.TransformTree(t);
			if (trainOptions.selectivePostSplit)
			{
				trTree = postSplitter.TransformTree(trTree);
			}
			if (trainOptions.printTreeTransformations > 0)
			{
				TrainOptions.PrintTrainTree(trainOptions.printAnnotatedPW, "ANNOTATED TREE:", trTree);
			}
			if (trainOptions.printAnnotatedRuleCounts)
			{
				Tree tr2 = trTree.DeepCopy(new LabeledScoredTreeFactory(), new StringLabelFactory());
				ICollection<Tree> localTrees = tr2.LocalTrees();
				foreach (Tree tr in localTrees)
				{
					annotatedRuleCounts.IncrementCount(tr);
				}
			}
			if (trainOptions.printAnnotatedStateCounts)
			{
				foreach (Tree subt in trTree)
				{
					if (!subt.IsLeaf())
					{
						annotatedStateCounts.IncrementCount(subt.Label().Value());
					}
				}
			}
			// if we add the ROOT first, then we don't know how to percolate the heads at the top
			AddRoot(trTree);
			// this creates a few non-binarized rules at the top
			Tree binarizedTree = binarizer.TransformTree(trTree);
			if (trainOptions.printTreeTransformations > 0)
			{
				TrainOptions.PrintTrainTree(trainOptions.printBinarizedPW, "BINARIZED TREE:", binarizedTree);
				trainOptions.printTreeTransformations--;
			}
			if (forceCNF)
			{
				binarizedTree = new CNFTransformers.ToCNFTransformer().TransformTree(binarizedTree);
			}
			//        System.out.println("BinarizedCNF:\n");
			//        binarizedTree.pennPrint();
			return binarizedTree;
		}

		public virtual void PrintRuleCounts()
		{
			log.Info();
			foreach (Tree t in annotatedRuleCounts.KeySet())
			{
				log.Info(annotatedRuleCounts.GetCount(t) + "\t" + t.Label().Value() + " -->");
				foreach (Tree dtr in t.GetChildrenAsList())
				{
					log.Info(" ");
					log.Info(dtr.Label().Value());
				}
				log.Info();
			}
		}

		public virtual void PrintStateCounts()
		{
			log.Info();
			log.Info("Annotated state counts");
			ICollection<string> keys = annotatedStateCounts.KeySet();
			IList<string> keyList = new List<string>(keys);
			keyList.Sort();
			foreach (string s in keyList)
			{
				log.Info(s + "\t" + annotatedStateCounts.GetCount(s));
			}
		}

		// main helper function
		private static int NumSubArgs(string[] args, int index)
		{
			int i = index;
			while (i + 1 < args.Length && args[i + 1][0] != '-')
			{
				i++;
			}
			return i - index;
		}

		private static void RemoveDeleteSplittersFromSplitters(ITreebankLanguagePack tlp, Options op)
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

		/// <returns>A Triple of binaryTrainTreebank, binarySecondaryTreebank, binaryTuneTreebank.</returns>
		public static Triple<Treebank, Treebank, Treebank> GetAnnotatedBinaryTreebankFromTreebank(Treebank trainTreebank, Treebank secondaryTreebank, Treebank tuneTreebank, Options op)
		{
			// setup tree transforms
			ITreebankLangParserParams tlpParams = op.tlpParams;
			ITreebankLanguagePack tlp = tlpParams.TreebankLanguagePack();
			if (op.testOptions.verbose)
			{
				PrintWriter pwErr = tlpParams.Pw(System.Console.Error);
				pwErr.Print("Training ");
				pwErr.Println(trainTreebank.TextualSummary(tlp));
				if (secondaryTreebank != null)
				{
					pwErr.Print("Secondary training ");
					pwErr.Println(secondaryTreebank.TextualSummary(tlp));
				}
			}
			CompositeTreeTransformer trainTransformer = new CompositeTreeTransformer();
			if (op.trainOptions.preTransformer != null)
			{
				trainTransformer.AddTransformer(op.trainOptions.preTransformer);
			}
			if (op.trainOptions.collinsPunc)
			{
				CollinsPuncTransformer collinsPuncTransformer = new CollinsPuncTransformer(tlp);
				trainTransformer.AddTransformer(collinsPuncTransformer);
			}
			log.Info("Binarizing trees...");
			Edu.Stanford.Nlp.Parser.Lexparser.TreeAnnotatorAndBinarizer binarizer;
			if (!op.trainOptions.leftToRight)
			{
				binarizer = new Edu.Stanford.Nlp.Parser.Lexparser.TreeAnnotatorAndBinarizer(tlpParams, op.forceCNF, !op.trainOptions.OutsideFactor(), !op.trainOptions.predictSplits, op);
			}
			else
			{
				binarizer = new Edu.Stanford.Nlp.Parser.Lexparser.TreeAnnotatorAndBinarizer(tlpParams.HeadFinder(), new LeftHeadFinder(), tlpParams, op.forceCNF, !op.trainOptions.OutsideFactor(), !op.trainOptions.predictSplits, op);
			}
			trainTransformer.AddTransformer(binarizer);
			if (op.wordFunction != null)
			{
				ITreeTransformer wordFunctionTransformer = new TreeLeafLabelTransformer(op.wordFunction);
				trainTransformer.AddTransformer(wordFunctionTransformer);
			}
			Treebank wholeTreebank;
			if (secondaryTreebank == null)
			{
				wholeTreebank = trainTreebank;
			}
			else
			{
				wholeTreebank = new CompositeTreebank(trainTreebank, secondaryTreebank);
			}
			if (op.trainOptions.selectiveSplit)
			{
				op.trainOptions.splitters = ParentAnnotationStats.GetSplitCategories(wholeTreebank, op.trainOptions.tagSelectiveSplit, 0, op.trainOptions.selectiveSplitCutOff, op.trainOptions.tagSelectiveSplitCutOff, tlp);
				RemoveDeleteSplittersFromSplitters(tlp, op);
				if (op.testOptions.verbose)
				{
					IList<string> list = new List<string>(op.trainOptions.splitters);
					list.Sort();
					log.Info("Parent split categories: " + list);
				}
			}
			if (op.trainOptions.selectivePostSplit)
			{
				// Do all the transformations once just to learn selective splits on annotated categories
				ITreeTransformer myTransformer = new TreeAnnotator(tlpParams.HeadFinder(), tlpParams, op);
				wholeTreebank = wholeTreebank.Transform(myTransformer);
				op.trainOptions.postSplitters = ParentAnnotationStats.GetSplitCategories(wholeTreebank, true, 0, op.trainOptions.selectivePostSplitCutOff, op.trainOptions.tagSelectivePostSplitCutOff, tlp);
				if (op.testOptions.verbose)
				{
					log.Info("Parent post annotation split categories: " + op.trainOptions.postSplitters);
				}
			}
			if (op.trainOptions.hSelSplit)
			{
				// We run through all the trees once just to gather counts for hSelSplit!
				int ptt = op.trainOptions.printTreeTransformations;
				op.trainOptions.printTreeTransformations = 0;
				binarizer.SetDoSelectiveSplit(false);
				foreach (Tree tree in wholeTreebank)
				{
					trainTransformer.TransformTree(tree);
				}
				binarizer.SetDoSelectiveSplit(true);
				op.trainOptions.printTreeTransformations = ptt;
			}
			// we've done all the setup now. here's where the train treebank is transformed.
			trainTreebank = trainTreebank.Transform(trainTransformer);
			if (secondaryTreebank != null)
			{
				secondaryTreebank = secondaryTreebank.Transform(trainTransformer);
			}
			if (op.trainOptions.printAnnotatedStateCounts)
			{
				binarizer.PrintStateCounts();
			}
			if (op.trainOptions.printAnnotatedRuleCounts)
			{
				binarizer.PrintRuleCounts();
			}
			if (tuneTreebank != null)
			{
				tuneTreebank = tuneTreebank.Transform(trainTransformer);
			}
			if (op.testOptions.verbose)
			{
				binarizer.DumpStats();
			}
			return new Triple<Treebank, Treebank, Treebank>(trainTreebank, secondaryTreebank, tuneTreebank);
		}

		/// <summary>Lets you test out the TreeAnnotatorAndBinarizer on the command line.</summary>
		/// <param name="args">
		/// Command line arguments: All flags accepted by FactoredParser.setOptionFlag
		/// and -train treebankPath [fileRanges]
		/// </param>
		public static void Main(string[] args)
		{
			Options op = new Options();
			string treebankPath = null;
			IFileFilter trainFilter = null;
			int i = 0;
			while (i < args.Length && args[i].StartsWith("-"))
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-train"))
				{
					int numSubArgs = NumSubArgs(args, i);
					i++;
					if (numSubArgs >= 1)
					{
						treebankPath = args[i];
						i++;
					}
					else
					{
						throw new Exception("Error: -train option must have treebankPath as first argument.");
					}
					if (numSubArgs == 2)
					{
						trainFilter = new NumberRangesFileFilter(args[i++], true);
					}
					else
					{
						if (numSubArgs >= 3)
						{
							int low = System.Convert.ToInt32(args[i]);
							int high = System.Convert.ToInt32(args[i + 1]);
							trainFilter = new NumberRangeFileFilter(low, high, true);
							i += 2;
						}
					}
				}
				else
				{
					i = op.SetOption(args, i);
				}
			}
			if (i < args.Length)
			{
				log.Info("usage: java TreeAnnotatorAndBinarizer options*");
				log.Info("  Options are like for lexicalized parser including -train treebankPath fileRange]");
				return;
			}
			log.Info("Annotating from treebank dir: " + treebankPath);
			Treebank trainTreebank = op.tlpParams.DiskTreebank();
			if (trainFilter == null)
			{
				trainTreebank.LoadPath(treebankPath);
			}
			else
			{
				trainTreebank.LoadPath(treebankPath, trainFilter);
			}
			Treebank binaryTrainTreebank = GetAnnotatedBinaryTreebankFromTreebank(trainTreebank, null, null, op).First();
			IEnumerator<Tree> it = trainTreebank.GetEnumerator();
			foreach (Tree t in binaryTrainTreebank)
			{
				System.Console.Out.WriteLine("Original tree:");
				it.Current.PennPrint();
				System.Console.Out.WriteLine("Binarized tree:");
				t.PennPrint();
				System.Console.Out.WriteLine();
			}
		}

		/// <summary>
		/// This does nothing but a function to change the tree nodes into
		/// CategoryWordTag, while the leaves are StringLabels.
		/// </summary>
		/// <remarks>
		/// This does nothing but a function to change the tree nodes into
		/// CategoryWordTag, while the leaves are StringLabels. That's what the
		/// rest of the code assumes.
		/// </remarks>
		internal class TreeNullAnnotator : ITreeTransformer
		{
			private readonly ITreeFactory tf = new LabeledScoredTreeFactory(new CategoryWordTagFactory());

			private readonly IHeadFinder hf;

			// end main
			public virtual Tree TransformTree(Tree t)
			{
				// make a defensive copy which the helper method can then mangle
				Tree copy = t.TreeSkeletonCopy(tf);
				return TransformTreeHelper(copy);
			}

			private Tree TransformTreeHelper(Tree t)
			{
				if (t != null)
				{
					string cat = t.Label().Value();
					if (t.IsLeaf())
					{
						ILabel label = new Word(cat);
						//new CategoryWordTag(cat,cat,"");
						t.SetLabel(label);
					}
					else
					{
						Tree[] kids = t.Children();
						foreach (Tree child in kids)
						{
							TransformTreeHelper(child);
						}
						// recursive call
						Tree headChild = hf.DetermineHead(t);
						string tag;
						string word;
						if (headChild == null)
						{
							log.Error("null head for tree\n" + t.ToString());
							word = null;
							tag = null;
						}
						else
						{
							if (headChild.IsLeaf())
							{
								tag = cat;
								word = headChild.Label().Value();
							}
							else
							{
								CategoryWordTag headLabel = (CategoryWordTag)headChild.Label();
								word = headLabel.Word();
								tag = headLabel.Tag();
							}
						}
						ILabel label = new CategoryWordTag(cat, word, tag);
						t.SetLabel(label);
					}
				}
				return t;
			}

			public TreeNullAnnotator(IHeadFinder hf)
			{
				this.hf = hf;
			}
		}
		// end static class TreeNullAnnotator
	}
}
