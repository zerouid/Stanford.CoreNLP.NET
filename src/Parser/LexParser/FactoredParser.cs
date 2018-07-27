// StanfordLexicalizedParser -- a probabilistic lexicalized NL CFG parser
// Copyright (c) 2002, 2003, 2004, 2005 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/ .
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    parser-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/lex-parser.html
using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Dan Klein (original version)</author>
	/// <author>Christopher Manning (better features, ParserParams, serialization)</author>
	/// <author>Roger Levy (internationalization)</author>
	/// <author>Teg Grenager (grammar compaction, etc., tokenization, etc.)</author>
	/// <author>Galen Andrew (lattice parsing)</author>
	/// <author>Philip Resnik and Dan Zeman (n good parses)</author>
	public class FactoredParser
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.FactoredParser));

		/* some documentation for Roger's convenience
		* {pcfg,dep,combo}{PE,DE,TE} are precision/dep/tagging evals for the models
		
		* parser is the PCFG parser
		* dparser is the dependency parser
		* bparser is the combining parser
		
		* during testing:
		* tree is the test tree (gold tree)
		* binaryTree is the gold tree binarized
		* tree2b is the best PCFG paser, binarized
		* tree2 is the best PCFG parse (debinarized)
		* tree3 is the dependency parse, binarized
		* tree3db is the dependency parser, debinarized
		* tree4 is the best combo parse, binarized and then debinarized
		* tree4b is the best combo parse, binarized
		*/
		public static void Main(string[] args)
		{
			Options op = new Options(new EnglishTreebankParserParams());
			// op.tlpParams may be changed to something else later, so don't use it till
			// after options are parsed.
			StringUtils.LogInvocationString(log, args);
			string path = "/u/nlp/stuff/corpora/Treebank3/parsed/mrg/wsj";
			int trainLow = 200;
			int trainHigh = 2199;
			int testLow = 2200;
			int testHigh = 2219;
			string serializeFile = null;
			int i = 0;
			while (i < args.Length && args[i].StartsWith("-"))
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-path") && (i + 1 < args.Length))
				{
					path = args[i + 1];
					i += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-train") && (i + 2 < args.Length))
					{
						trainLow = System.Convert.ToInt32(args[i + 1]);
						trainHigh = System.Convert.ToInt32(args[i + 2]);
						i += 3;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-test") && (i + 2 < args.Length))
						{
							testLow = System.Convert.ToInt32(args[i + 1]);
							testHigh = System.Convert.ToInt32(args[i + 2]);
							i += 3;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-serialize") && (i + 1 < args.Length))
							{
								serializeFile = args[i + 1];
								i += 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-tLPP") && (i + 1 < args.Length))
								{
									try
									{
										op.tlpParams = (ITreebankLangParserParams)System.Activator.CreateInstance(Sharpen.Runtime.GetType(args[i + 1]));
									}
									catch (TypeLoadException e)
									{
										log.Info("Class not found: " + args[i + 1]);
										throw new Exception(e);
									}
									catch (InstantiationException e)
									{
										log.Info("Couldn't instantiate: " + args[i + 1] + ": " + e.ToString());
										throw new Exception(e);
									}
									catch (MemberAccessException e)
									{
										log.Info("illegal access" + e);
										throw new Exception(e);
									}
									i += 2;
								}
								else
								{
									if (args[i].Equals("-encoding"))
									{
										// sets encoding for TreebankLangParserParams
										op.tlpParams.SetInputEncoding(args[i + 1]);
										op.tlpParams.SetOutputEncoding(args[i + 1]);
										i += 2;
									}
									else
									{
										i = op.SetOptionOrWarn(args, i);
									}
								}
							}
						}
					}
				}
			}
			// System.out.println(tlpParams.getClass());
			ITreebankLanguagePack tlp = op.tlpParams.TreebankLanguagePack();
			op.trainOptions.sisterSplitters = Generics.NewHashSet(Arrays.AsList(op.tlpParams.SisterSplitters()));
			//    BinarizerFactory.TreeAnnotator.setTreebankLang(tlpParams);
			PrintWriter pw = op.tlpParams.Pw();
			op.testOptions.Display();
			op.trainOptions.Display();
			op.Display();
			op.tlpParams.Display();
			// setup tree transforms
			Treebank trainTreebank = op.tlpParams.MemoryTreebank();
			MemoryTreebank testTreebank = op.tlpParams.TestMemoryTreebank();
			// Treebank blippTreebank = ((EnglishTreebankParserParams) tlpParams).diskTreebank();
			// String blippPath = "/afs/ir.stanford.edu/data/linguistic-data/BLLIP-WSJ/";
			// blippTreebank.loadPath(blippPath, "", true);
			Timing.StartTime();
			log.Info("Reading trees...");
			testTreebank.LoadPath(path, new NumberRangeFileFilter(testLow, testHigh, true));
			if (op.testOptions.increasingLength)
			{
				testTreebank.Sort(new TreeLengthComparator());
			}
			trainTreebank.LoadPath(path, new NumberRangeFileFilter(trainLow, trainHigh, true));
			Timing.Tick("done.");
			log.Info("Binarizing trees...");
			TreeAnnotatorAndBinarizer binarizer;
			if (!op.trainOptions.leftToRight)
			{
				binarizer = new TreeAnnotatorAndBinarizer(op.tlpParams, op.forceCNF, !op.trainOptions.OutsideFactor(), true, op);
			}
			else
			{
				binarizer = new TreeAnnotatorAndBinarizer(op.tlpParams.HeadFinder(), new LeftHeadFinder(), op.tlpParams, op.forceCNF, !op.trainOptions.OutsideFactor(), true, op);
			}
			CollinsPuncTransformer collinsPuncTransformer = null;
			if (op.trainOptions.collinsPunc)
			{
				collinsPuncTransformer = new CollinsPuncTransformer(tlp);
			}
			ITreeTransformer debinarizer = new Debinarizer(op.forceCNF);
			IList<Tree> binaryTrainTrees = new List<Tree>();
			if (op.trainOptions.selectiveSplit)
			{
				op.trainOptions.splitters = ParentAnnotationStats.GetSplitCategories(trainTreebank, op.trainOptions.tagSelectiveSplit, 0, op.trainOptions.selectiveSplitCutOff, op.trainOptions.tagSelectiveSplitCutOff, op.tlpParams.TreebankLanguagePack());
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
					log.Info("Removed from vertical splitters: " + deleted);
				}
			}
			if (op.trainOptions.selectivePostSplit)
			{
				ITreeTransformer myTransformer = new TreeAnnotator(op.tlpParams.HeadFinder(), op.tlpParams, op);
				Treebank annotatedTB = trainTreebank.Transform(myTransformer);
				op.trainOptions.postSplitters = ParentAnnotationStats.GetSplitCategories(annotatedTB, true, 0, op.trainOptions.selectivePostSplitCutOff, op.trainOptions.tagSelectivePostSplitCutOff, op.tlpParams.TreebankLanguagePack());
			}
			if (op.trainOptions.hSelSplit)
			{
				binarizer.SetDoSelectiveSplit(false);
				foreach (Tree tree in trainTreebank)
				{
					if (op.trainOptions.collinsPunc)
					{
						tree = collinsPuncTransformer.TransformTree(tree);
					}
					//tree.pennPrint(tlpParams.pw());
					tree = binarizer.TransformTree(tree);
				}
				//binaryTrainTrees.add(tree);
				binarizer.SetDoSelectiveSplit(true);
			}
			foreach (Tree tree_1 in trainTreebank)
			{
				if (op.trainOptions.collinsPunc)
				{
					tree_1 = collinsPuncTransformer.TransformTree(tree_1);
				}
				tree_1 = binarizer.TransformTree(tree_1);
				binaryTrainTrees.Add(tree_1);
			}
			if (op.testOptions.verbose)
			{
				binarizer.DumpStats();
			}
			IList<Tree> binaryTestTrees = new List<Tree>();
			foreach (Tree tree_2 in testTreebank)
			{
				if (op.trainOptions.collinsPunc)
				{
					tree_2 = collinsPuncTransformer.TransformTree(tree_2);
				}
				tree_2 = binarizer.TransformTree(tree_2);
				binaryTestTrees.Add(tree_2);
			}
			Timing.Tick("done.");
			// binarization
			BinaryGrammar bg = null;
			UnaryGrammar ug = null;
			IDependencyGrammar dg = null;
			// DependencyGrammar dgBLIPP = null;
			ILexicon lex = null;
			IIndex<string> stateIndex = new HashIndex<string>();
			// extract grammars
			IExtractor<Pair<UnaryGrammar, BinaryGrammar>> bgExtractor = new BinaryGrammarExtractor(op, stateIndex);
			//Extractor bgExtractor = new SmoothedBinaryGrammarExtractor();//new BinaryGrammarExtractor();
			// Extractor lexExtractor = new LexiconExtractor();
			//Extractor dgExtractor = new DependencyMemGrammarExtractor();
			if (op.doPCFG)
			{
				log.Info("Extracting PCFG...");
				Pair<UnaryGrammar, BinaryGrammar> bgug = null;
				if (op.trainOptions.cheatPCFG)
				{
					IList<Tree> allTrees = new List<Tree>(binaryTrainTrees);
					Sharpen.Collections.AddAll(allTrees, binaryTestTrees);
					bgug = bgExtractor.Extract(allTrees);
				}
				else
				{
					bgug = bgExtractor.Extract(binaryTrainTrees);
				}
				bg = bgug.second;
				bg.SplitRules();
				ug = bgug.first;
				ug.PurgeRules();
				Timing.Tick("done.");
			}
			log.Info("Extracting Lexicon...");
			IIndex<string> wordIndex = new HashIndex<string>();
			IIndex<string> tagIndex = new HashIndex<string>();
			lex = op.tlpParams.Lex(op, wordIndex, tagIndex);
			lex.InitializeTraining(binaryTrainTrees.Count);
			lex.Train(binaryTrainTrees);
			lex.FinishTraining();
			Timing.Tick("done.");
			if (op.doDep)
			{
				log.Info("Extracting Dependencies...");
				binaryTrainTrees.Clear();
				IExtractor<IDependencyGrammar> dgExtractor = new MLEDependencyGrammarExtractor(op, wordIndex, tagIndex);
				// dgBLIPP = (DependencyGrammar) dgExtractor.extract(new ConcatenationIterator(trainTreebank.iterator(),blippTreebank.iterator()),new TransformTreeDependency(tlpParams,true));
				// DependencyGrammar dg1 = dgExtractor.extract(trainTreebank.iterator(), new TransformTreeDependency(op.tlpParams, true));
				//dgBLIPP=(DependencyGrammar)dgExtractor.extract(blippTreebank.iterator(),new TransformTreeDependency(tlpParams));
				//dg = (DependencyGrammar) dgExtractor.extract(new ConcatenationIterator(trainTreebank.iterator(),blippTreebank.iterator()),new TransformTreeDependency(tlpParams));
				// dg=new DependencyGrammarCombination(dg1,dgBLIPP,2);
				dg = dgExtractor.Extract(binaryTrainTrees);
				//uses information whether the words are known or not, discards unknown words
				Timing.Tick("done.");
				//System.out.print("Extracting Unknown Word Model...");
				//UnknownWordModel uwm = (UnknownWordModel)uwmExtractor.extract(binaryTrainTrees);
				//Timing.tick("done.");
				System.Console.Out.Write("Tuning Dependency Model...");
				dg.Tune(binaryTestTrees);
				//System.out.println("TUNE DEPS: "+tuneDeps);
				Timing.Tick("done.");
			}
			BinaryGrammar boundBG = bg;
			UnaryGrammar boundUG = ug;
			IGrammarProjection gp = new NullGrammarProjection(bg, ug);
			// serialization
			if (serializeFile != null)
			{
				log.Info("Serializing parser...");
				LexicalizedParser parser = new LexicalizedParser(lex, bg, ug, dg, stateIndex, wordIndex, tagIndex, op);
				parser.SaveParserToSerialized(serializeFile);
				Timing.Tick("done.");
			}
			// test: pcfg-parse and output
			ExhaustivePCFGParser parser_1 = null;
			if (op.doPCFG)
			{
				parser_1 = new ExhaustivePCFGParser(boundBG, boundUG, lex, op, stateIndex, wordIndex, tagIndex);
			}
			ExhaustiveDependencyParser dparser = ((op.doDep && !op.testOptions.useFastFactored) ? new ExhaustiveDependencyParser(dg, lex, op, wordIndex, tagIndex) : null);
			IScorer scorer = (op.doPCFG ? new TwinScorer(new ProjectionScorer(parser_1, gp, op), dparser) : null);
			//Scorer scorer = parser;
			BiLexPCFGParser bparser = null;
			if (op.doPCFG && op.doDep)
			{
				bparser = (op.testOptions.useN5) ? new BiLexPCFGParser.N5BiLexPCFGParser(scorer, parser_1, dparser, bg, ug, dg, lex, op, gp, stateIndex, wordIndex, tagIndex) : new BiLexPCFGParser(scorer, parser_1, dparser, bg, ug, dg, lex, op, gp, stateIndex
					, wordIndex, tagIndex);
			}
			Evalb pcfgPE = new Evalb("pcfg  PE", true);
			Evalb comboPE = new Evalb("combo PE", true);
			AbstractEval pcfgCB = new Evalb.CBEval("pcfg  CB", true);
			AbstractEval pcfgTE = new TaggingEval("pcfg  TE");
			AbstractEval comboTE = new TaggingEval("combo TE");
			AbstractEval pcfgTEnoPunct = new TaggingEval("pcfg nopunct TE");
			AbstractEval comboTEnoPunct = new TaggingEval("combo nopunct TE");
			AbstractEval depTE = new TaggingEval("depnd TE");
			AbstractEval depDE = new UnlabeledAttachmentEval("depnd DE", true, null, tlp.PunctuationWordRejectFilter());
			AbstractEval comboDE = new UnlabeledAttachmentEval("combo DE", true, null, tlp.PunctuationWordRejectFilter());
			if (op.testOptions.evalb)
			{
				EvalbFormatWriter.InitEVALBfiles(op.tlpParams);
			}
			// int[] countByLength = new int[op.testOptions.maxLength+1];
			// Use a reflection ruse, so one can run this without needing the
			// tagger.  Using a function rather than a MaxentTagger means we
			// can distribute a version of the parser that doesn't include the
			// entire tagger.
			IFunction<IList<IHasWord>, List<TaggedWord>> tagger = null;
			if (op.testOptions.preTag)
			{
				try
				{
					Type[] argsClass = new Type[] { typeof(string) };
					object[] arguments = new object[] { op.testOptions.taggerSerializedFile };
					tagger = (IFunction<IList<IHasWord>, List<TaggedWord>>)Sharpen.Runtime.GetType("edu.stanford.nlp.tagger.maxent.MaxentTagger").GetConstructor(argsClass).NewInstance(arguments);
				}
				catch (Exception e)
				{
					log.Info(e);
					log.Info("Warning: No pretagging of sentences will be done.");
				}
			}
			for (int tNum = 0; tNum < ttSize; tNum++)
			{
				Tree tree = testTreebank[tNum];
				int testTreeLen = tree_2.Yield().Count;
				if (testTreeLen > op.testOptions.maxLength)
				{
					continue;
				}
				Tree binaryTree = binaryTestTrees[tNum];
				// countByLength[testTreeLen]++;
				System.Console.Out.WriteLine("-------------------------------------");
				System.Console.Out.WriteLine("Number: " + (tNum + 1));
				System.Console.Out.WriteLine("Length: " + testTreeLen);
				//tree.pennPrint(pw);
				// System.out.println("XXXX The binary tree is");
				// binaryTree.pennPrint(pw);
				//System.out.println("Here are the tags in the lexicon:");
				//System.out.println(lex.showTags());
				//System.out.println("Here's the tagnumberer:");
				//System.out.println(Numberer.getGlobalNumberer("tags").toString());
				long timeMil1 = Runtime.CurrentTimeMillis();
				Timing.Tick("Starting parse.");
				if (op.doPCFG)
				{
					//log.info(op.testOptions.forceTags);
					if (op.testOptions.forceTags)
					{
						if (tagger != null)
						{
							//System.out.println("Using a tagger to set tags");
							//System.out.println("Tagged sentence as: " + tagger.processSentence(cutLast(wordify(binaryTree.yield()))).toString(false));
							parser_1.Parse(AddLast(tagger.Apply(CutLast(Wordify(binaryTree.Yield())))));
						}
						else
						{
							//System.out.println("Forcing tags to match input.");
							parser_1.Parse(CleanTags(binaryTree.TaggedYield(), tlp));
						}
					}
					else
					{
						// System.out.println("XXXX Parsing " + binaryTree.yield());
						parser_1.Parse(binaryTree.YieldHasWord());
					}
				}
				//Timing.tick("Done with pcfg phase.");
				if (op.doDep)
				{
					dparser.Parse(binaryTree.YieldHasWord());
				}
				//Timing.tick("Done with dependency phase.");
				bool bothPassed = false;
				if (op.doPCFG && op.doDep)
				{
					bothPassed = bparser.Parse(binaryTree.YieldHasWord());
				}
				//Timing.tick("Done with combination phase.");
				long timeMil2 = Runtime.CurrentTimeMillis();
				long elapsed = timeMil2 - timeMil1;
				log.Info("Time: " + ((int)(elapsed / 100)) / 10.00 + " sec.");
				//System.out.println("PCFG Best Parse:");
				Tree tree2b = null;
				Tree tree2 = null;
				//System.out.println("Got full best parse...");
				if (op.doPCFG)
				{
					tree2b = parser_1.GetBestParse();
					tree2 = debinarizer.TransformTree(tree2b);
				}
				//System.out.println("Debinarized parse...");
				//tree2.pennPrint();
				//System.out.println("DepG Best Parse:");
				Tree tree3 = null;
				Tree tree3db = null;
				if (op.doDep)
				{
					tree3 = dparser.GetBestParse();
					// was: but wrong Tree tree3db = debinarizer.transformTree(tree2);
					tree3db = debinarizer.TransformTree(tree3);
					tree3.PennPrint(pw);
				}
				//tree.pennPrint();
				//((Tree)binaryTrainTrees.get(tNum)).pennPrint();
				//System.out.println("Combo Best Parse:");
				Tree tree4 = null;
				if (op.doPCFG && op.doDep)
				{
					try
					{
						tree4 = bparser.GetBestParse();
						if (tree4 == null)
						{
							tree4 = tree2b;
						}
					}
					catch (ArgumentNullException)
					{
						log.Info("Blocked, using PCFG parse!");
						tree4 = tree2b;
					}
				}
				if (op.doPCFG && !bothPassed)
				{
					tree4 = tree2b;
				}
				//tree4.pennPrint();
				if (op.doDep)
				{
					depDE.Evaluate(tree3, binaryTree, pw);
					depTE.Evaluate(tree3db, tree_2, pw);
				}
				ITreeTransformer tc = op.tlpParams.Collinizer();
				ITreeTransformer tcEvalb = op.tlpParams.CollinizerEvalb();
				if (op.doPCFG)
				{
					// System.out.println("XXXX Best PCFG was: ");
					// tree2.pennPrint();
					// System.out.println("XXXX Transformed best PCFG is: ");
					// tc.transformTree(tree2).pennPrint();
					//System.out.println("True Best Parse:");
					//tree.pennPrint();
					//tc.transformTree(tree).pennPrint();
					pcfgPE.Evaluate(tc.TransformTree(tree2), tc.TransformTree(tree_2), pw);
					pcfgCB.Evaluate(tc.TransformTree(tree2), tc.TransformTree(tree_2), pw);
					Tree tree4b = null;
					if (op.doDep)
					{
						comboDE.Evaluate((bothPassed ? tree4 : tree3), binaryTree, pw);
						tree4b = tree4;
						tree4 = debinarizer.TransformTree(tree4);
						if (op.nodePrune)
						{
							NodePruner np = new NodePruner(parser_1, debinarizer);
							tree4 = np.Prune(tree4);
						}
						//tree4.pennPrint();
						comboPE.Evaluate(tc.TransformTree(tree4), tc.TransformTree(tree_2), pw);
					}
					//pcfgTE.evaluate(tree2, tree);
					pcfgTE.Evaluate(tcEvalb.TransformTree(tree2), tcEvalb.TransformTree(tree_2), pw);
					pcfgTEnoPunct.Evaluate(tc.TransformTree(tree2), tc.TransformTree(tree_2), pw);
					if (op.doDep)
					{
						comboTE.Evaluate(tcEvalb.TransformTree(tree4), tcEvalb.TransformTree(tree_2), pw);
						comboTEnoPunct.Evaluate(tc.TransformTree(tree4), tc.TransformTree(tree_2), pw);
					}
					System.Console.Out.WriteLine("PCFG only: " + parser_1.ScoreBinarizedTree(tree2b, 0));
					//tc.transformTree(tree2).pennPrint();
					tree2.PennPrint(pw);
					if (op.doDep)
					{
						System.Console.Out.WriteLine("Combo: " + parser_1.ScoreBinarizedTree(tree4b, 0));
						// tc.transformTree(tree4).pennPrint(pw);
						tree4.PennPrint(pw);
					}
					System.Console.Out.WriteLine("Correct:" + parser_1.ScoreBinarizedTree(binaryTree, 0));
					/*
					if (parser.scoreBinarizedTree(tree2b,true) < parser.scoreBinarizedTree(binaryTree,true)) {
					System.out.println("SCORE INVERSION");
					parser.validateBinarizedTree(binaryTree,0);
					}
					*/
					tree_2.PennPrint(pw);
				}
				// end if doPCFG
				if (op.testOptions.evalb)
				{
					if (op.doPCFG && op.doDep)
					{
						EvalbFormatWriter.WriteEVALBline(tcEvalb.TransformTree(tree_2), tcEvalb.TransformTree(tree4));
					}
					else
					{
						if (op.doPCFG)
						{
							EvalbFormatWriter.WriteEVALBline(tcEvalb.TransformTree(tree_2), tcEvalb.TransformTree(tree2));
						}
						else
						{
							if (op.doDep)
							{
								EvalbFormatWriter.WriteEVALBline(tcEvalb.TransformTree(tree_2), tcEvalb.TransformTree(tree3db));
							}
						}
					}
				}
			}
			// end for each tree in test treebank
			if (op.testOptions.evalb)
			{
				EvalbFormatWriter.CloseEVALBfiles();
			}
			// op.testOptions.display();
			if (op.doPCFG)
			{
				pcfgPE.Display(false, pw);
				System.Console.Out.WriteLine("Grammar size: " + stateIndex.Size());
				pcfgCB.Display(false, pw);
				if (op.doDep)
				{
					comboPE.Display(false, pw);
				}
				pcfgTE.Display(false, pw);
				pcfgTEnoPunct.Display(false, pw);
				if (op.doDep)
				{
					comboTE.Display(false, pw);
					comboTEnoPunct.Display(false, pw);
				}
			}
			if (op.doDep)
			{
				depTE.Display(false, pw);
				depDE.Display(false, pw);
			}
			if (op.doPCFG && op.doDep)
			{
				comboDE.Display(false, pw);
			}
		}

		// pcfgPE.printGoodBad();
		private static IList<TaggedWord> CleanTags(IList<TaggedWord> twList, ITreebankLanguagePack tlp)
		{
			int sz = twList.Count;
			IList<TaggedWord> l = new List<TaggedWord>(sz);
			foreach (TaggedWord tw in twList)
			{
				TaggedWord tw2 = new TaggedWord(tw.Word(), tlp.BasicCategory(tw.Tag()));
				l.Add(tw2);
			}
			return l;
		}

		private static List<Word> Wordify(IList wList)
		{
			List<Word> s = new List<Word>();
			foreach (object obj in wList)
			{
				s.Add(new Word(obj.ToString()));
			}
			return s;
		}

		private static List<Word> CutLast(List<Word> s)
		{
			return new List<Word>(s.SubList(0, s.Count - 1));
		}

		private static List<Word> AddLast<_T0>(List<_T0> s)
			where _T0 : Word
		{
			List<Word> s2 = new List<Word>(s);
			//s2.add(new StringLabel(Lexicon.BOUNDARY));
			s2.Add(new Word(LexiconConstants.Boundary));
			return s2;
		}

		/// <summary>Not an instantiable class</summary>
		private FactoredParser()
		{
		}
	}
}
