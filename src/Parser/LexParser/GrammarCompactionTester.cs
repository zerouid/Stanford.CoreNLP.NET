using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Fsm;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Teg Grenager (grenager@cs.stanford.edu)</author>
	public class GrammarCompactionTester
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(GrammarCompactionTester));

		internal ExhaustivePCFGParser parser = null;

		internal ExhaustiveDependencyParser dparser = null;

		internal BiLexPCFGParser bparser = null;

		internal IScorer scorer = null;

		internal Options op;

		internal GrammarCompactor compactor = null;

		internal IDictionary<string, IList<IList<string>>> allTestPaths = Generics.NewHashMap();

		internal IDictionary<string, IList<IList<string>>> allTrainPaths = Generics.NewHashMap();

		internal string asciiOutputPath = null;

		internal string path = "/u/nlp/stuff/corpora/Treebank3/parsed/mrg/wsj";

		internal int trainLow = 200;

		internal int trainHigh = 2199;

		internal int testLow = 2200;

		internal int testHigh = 2219;

		internal string suffixOrderString = null;

		internal string minArcNumString = null;

		internal string maxMergeCostString = null;

		internal string sizeCutoffString = null;

		internal string minPortionArcsString = null;

		internal string ignoreUnsupportedSuffixesString = "false";

		internal string splitParamString = null;

		internal string costModelString = null;

		internal string verboseString = null;

		internal string minArcCostString = null;

		internal string trainThresholdString = null;

		internal string heldoutThresholdString = null;

		internal int markovOrder = -1;

		internal string smoothParamString = null;

		internal string scoringData = null;

		internal string allowEpsilonsString = null;

		internal bool saveGraphs = false;

		private int indexRangeLow;

		private int indexRangeHigh;

		private string outputFile = null;

		private string inputFile = null;

		private bool toy = false;

		// for debugging
		//  public static MergeableGraph debugGraph = null;
		//  TreebankLangParserParams tlpParams = new EnglishTreebankParserParams();
		// tlpParams may be changed to something else later, so don't use it till
		// after options are parsed.
		public virtual IDictionary<string, IList<IList<string>>> ExtractPaths(string path, int low, int high, bool annotate)
		{
			// setup tree transforms
			Treebank trainTreebank = op.tlpParams.MemoryTreebank();
			// this is a new one
			ITreebankLanguagePack tlp = op.Langpack();
			trainTreebank.LoadPath(path, new NumberRangeFileFilter(low, high, true));
			if (op.trainOptions.selectiveSplit)
			{
				op.trainOptions.splitters = ParentAnnotationStats.GetSplitCategories(trainTreebank, op.trainOptions.selectiveSplitCutOff, op.tlpParams.TreebankLanguagePack());
			}
			if (op.trainOptions.selectivePostSplit)
			{
				ITreeTransformer myTransformer = new TreeAnnotator(op.tlpParams.HeadFinder(), op.tlpParams, op);
				Treebank annotatedTB = trainTreebank.Transform(myTransformer);
				op.trainOptions.postSplitters = ParentAnnotationStats.GetSplitCategories(annotatedTB, op.trainOptions.selectivePostSplitCutOff, op.tlpParams.TreebankLanguagePack());
			}
			IList<Tree> trainTrees = new List<Tree>();
			IHeadFinder hf = null;
			if (op.trainOptions.leftToRight)
			{
				hf = new LeftHeadFinder();
			}
			else
			{
				hf = op.tlpParams.HeadFinder();
			}
			ITreeTransformer annotator = new TreeAnnotator(hf, op.tlpParams, op);
			foreach (Tree tree in trainTreebank)
			{
				if (annotate)
				{
					tree = annotator.TransformTree(tree);
				}
				trainTrees.Add(tree);
			}
			IExtractor<IDictionary<string, IList<IList<string>>>> pExtractor = new PathExtractor(hf, op);
			IDictionary<string, IList<IList<string>>> allPaths = pExtractor.Extract(trainTrees);
			return allPaths;
		}

		public static void Main(string[] args)
		{
			new GrammarCompactionTester().RunTest(args);
		}

		public virtual void RunTest(string[] args)
		{
			System.Console.Out.WriteLine("Currently " + new DateTime());
			System.Console.Out.Write("Invoked with arguments:");
			foreach (string arg in args)
			{
				System.Console.Out.Write(" " + arg);
			}
			System.Console.Out.WriteLine();
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
					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-saveToAscii") && (i + 1 < args.Length))
					{
						asciiOutputPath = args[i + 1];
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
								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-index") && (i + 2 < args.Length))
								{
									indexRangeLow = System.Convert.ToInt32(args[i + 1]);
									indexRangeHigh = System.Convert.ToInt32(args[i + 2]);
									i += 3;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-outputFile"))
									{
										outputFile = args[i + 1];
										i += 2;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-inputFile"))
										{
											inputFile = args[i + 1];
											i += 2;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-suffixOrder"))
											{
												suffixOrderString = args[i + 1];
												i += 2;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-minArcNum"))
												{
													minArcNumString = args[i + 1];
													i += 2;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-maxMergeCost"))
													{
														maxMergeCostString = args[i + 1];
														i += 2;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-sizeCutoff"))
														{
															sizeCutoffString = args[i + 1];
															i += 2;
														}
														else
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-minPortionArcs"))
															{
																minPortionArcsString = args[i + 1];
																i += 2;
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-ignoreUnsupportedSuffixes"))
																{
																	ignoreUnsupportedSuffixesString = args[i + 1];
																	i += 2;
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-trainThreshold"))
																	{
																		trainThresholdString = args[i + 1];
																		i += 2;
																	}
																	else
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-heldoutThreshold"))
																		{
																			heldoutThresholdString = args[i + 1];
																			i += 2;
																		}
																		else
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-minArcCost"))
																			{
																				minArcCostString = args[i + 1];
																				i += 2;
																			}
																			else
																			{
																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitParam"))
																				{
																					splitParamString = args[i + 1];
																					i += 2;
																				}
																				else
																				{
																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-costModel"))
																					{
																						costModelString = args[i + 1];
																						i += 2;
																					}
																					else
																					{
																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-scoringData"))
																						{
																							scoringData = args[i + 1];
																							i += 2;
																						}
																						else
																						{
																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-verbose"))
																							{
																								verboseString = args[i + 1];
																								i += 2;
																							}
																							else
																							{
																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-allowEpsilons"))
																								{
																									allowEpsilonsString = args[i + 1];
																									i += 2;
																								}
																								else
																								{
																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-saveGraphs"))
																									{
																										saveGraphs = true;
																										i++;
																									}
																									else
																									{
																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-toy"))
																										{
																											toy = true;
																											i++;
																										}
																										else
																										{
																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markovOrder"))
																											{
																												markovOrder = System.Convert.ToInt32(args[i + 1]);
																												i += 2;
																											}
																											else
																											{
																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-smoothParam"))
																												{
																													smoothParamString = args[i + 1];
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
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			op.trainOptions.sisterSplitters = Generics.NewHashSet(Arrays.AsList(op.tlpParams.SisterSplitters()));
			if (op.trainOptions.CompactGrammar() == 4)
			{
				System.Console.Out.WriteLine("Instantiating fsm.LossyGrammarCompactor");
				try
				{
					Type[] argTypes = new Type[13];
					Type strClass = typeof(string);
					for (int j = 0; j < argTypes.Length; j++)
					{
						argTypes[j] = strClass;
					}
					object[] cArgs = new object[13];
					cArgs[0] = suffixOrderString;
					cArgs[1] = minArcNumString;
					cArgs[2] = trainThresholdString;
					cArgs[3] = heldoutThresholdString;
					cArgs[4] = sizeCutoffString;
					cArgs[5] = minPortionArcsString;
					cArgs[6] = splitParamString;
					cArgs[7] = ignoreUnsupportedSuffixesString;
					cArgs[8] = minArcCostString;
					cArgs[9] = smoothParamString;
					cArgs[10] = costModelString;
					cArgs[11] = scoringData;
					cArgs[12] = verboseString;
					compactor = (GrammarCompactor)Sharpen.Runtime.GetType("fsm.LossyGrammarCompactor").GetConstructor(argTypes).NewInstance(cArgs);
				}
				catch (Exception e)
				{
					log.Info("Couldn't instantiate GrammarCompactor: " + e);
					Sharpen.Runtime.PrintStackTrace(e);
				}
			}
			else
			{
				if (op.trainOptions.CompactGrammar() == 5)
				{
					System.Console.Out.WriteLine("Instantiating fsm.CategoryMergingGrammarCompactor");
					try
					{
						Type[] argTypes = new Type[6];
						Type strClass = typeof(string);
						for (int j = 0; j < argTypes.Length; j++)
						{
							argTypes[j] = strClass;
						}
						object[] cArgs = new object[6];
						cArgs[0] = splitParamString;
						cArgs[1] = trainThresholdString;
						cArgs[2] = heldoutThresholdString;
						cArgs[3] = minArcCostString;
						cArgs[4] = ignoreUnsupportedSuffixesString;
						cArgs[5] = smoothParamString;
						compactor = (GrammarCompactor)Sharpen.Runtime.GetType("fsm.CategoryMergingGrammarCompactor").GetConstructor(argTypes).NewInstance(cArgs);
					}
					catch (Exception e)
					{
						throw new Exception("Couldn't instantiate CategoryMergingGrammarCompactor." + e);
					}
				}
				else
				{
					if (op.trainOptions.CompactGrammar() == 3)
					{
						System.Console.Out.WriteLine("Instantiating fsm.ExactGrammarCompactor");
						compactor = new ExactGrammarCompactor(op, saveGraphs, true);
					}
					else
					{
						if (op.trainOptions.CompactGrammar() > 0)
						{
						}
					}
				}
			}
			if (markovOrder >= 0)
			{
				op.trainOptions.markovOrder = markovOrder;
				op.trainOptions.hSelSplit = false;
			}
			if (toy)
			{
				BuildAndCompactToyGrammars();
			}
			else
			{
				TestGrammarCompaction();
			}
		}

		/*
		private static void testOneAtATimeMerging() {
		
		// use the parser constructor to extract the grammars from the treebank once
		LexicalizedParser lp = new LexicalizedParser(path, new NumberRangeFileFilter(trainLow, trainHigh, true), tlpParams);
		
		ParserData pd = lp.parserData();
		Pair originalGrammar = new Pair(pd.ug, pd.bg);
		
		// extract a bunch of paths
		Timing.startTime();
		System.out.print("Extracting other paths...");
		allTrainPaths = extractPaths(path, trainLow, trainHigh, true);
		allTestPaths = extractPaths(path, testLow, testHigh, true);
		Timing.tick("done");
		
		List mergePairs = null;
		if (inputFile != null) {
		// read merge pairs from file and do them and parse
		System.out.println("getting pairs from file: " + inputFile);
		mergePairs = getMergePairsFromFile(inputFile);
		}
		// try one merge at a time and parse afterwards
		Numberer originalNumberer = Numberer.getGlobalNumberer("states");
		String header = "index\tmergePair\tmergeCost\tparseF1\n";
		StringUtils.printToFile(outputFile, header, true);
		
		for (int i = indexRangeLow; i < indexRangeHigh; i++) {
		
		Timing.startTime();
		Numberer.getNumberers().put("states", originalNumberer);
		if (mergePairs != null)
		System.out.println("passing merge pairs to compactor: " + mergePairs);
		CategoryMergingGrammarCompactor compactor = new CategoryMergingGrammarCompactor(mergePairs, i);
		System.out.println("Compacting grammars with index " + i);
		Pair compactedGrammar = compactor.compactGrammar(originalGrammar, allTrainPaths, allTestPaths);
		Pair mergePair = null;
		double mergeCosts = Double.NEGATIVE_INFINITY;
		List mergeList = compactor.getCompletedMergeList();
		if (mergeList != null && mergeList.size() > 0) {
		mergePair = (Pair) mergeList.get(0);
		mergeCosts = compactor.getActualScores().getCount(mergePair);
		}
		
		
		ParserData newPd = new ParserData(pd.lex,
		(BinaryGrammar) compactedGrammar.second, (UnaryGrammar) compactedGrammar.first,
		pd.dg, pd.numbs, pd.pt);
		
		lp = new LexicalizedParser(newPd);
		Timing.tick("done.");
		
		Treebank testTreebank = tlpParams.testMemoryTreebank();
		testTreebank.loadPath(path, new NumberRangeFileFilter(testLow, testHigh, true));
		System.out.println("Currently " + new Date());
		double f1 = lp.testOnTreebank(testTreebank);
		System.out.println("Currently " + new Date());
		
		String resultString = i + "\t" + mergePair + "\t" + mergeCosts + "\t" + f1 + "\n";
		StringUtils.printToFile(outputFile, resultString, true);
		}
		}
		
		private static List getMergePairsFromFile(String filename) {
		List result = new ArrayList();
		try {
		String fileString = StringUtils.slurpFile(new File(filename));
		StringTokenizer st = new StringTokenizer(fileString);
		while (st.hasMoreTokens()) {
		String token1 = st.nextToken();
		if (st.hasMoreTokens()) {
		String token2 = st.nextToken();
		UnorderedPair pair = new UnorderedPair(token1, token2);
		result.add(pair);
		}
		}
		} catch (Exception e) {
		throw new RuntimeException("couldn't access file: " + filename);
		}
		return result;
		}
		*/
		/*
		//    System.out.println(MergeableGraph.areIsomorphic(graphs[0], graphs[1], graphs[0].getStartNode(), graphs[1].getStartNode()));
		//    System.out.println(MergeableGraph.areIsomorphic(graphs[1], graphs[2], graphs[1].getStartNode(), graphs[2].getStartNode()));
		//    System.out.println(MergeableGraph.areIsomorphic(graphs[2], graphs[0], graphs[2].getStartNode(), graphs[0].getStartNode()));
		
		// now go through the grammars themselves and see if they are equal
		System.out.println("UR 0 and 1: " + equalsUnary(((UnaryGrammar)grammars[0].first).rules(),((UnaryGrammar)grammars[1].first).rules()));
		System.out.println("UR 1 and 2: "  + equalsUnary(((UnaryGrammar)grammars[1].first).rules(),((UnaryGrammar)grammars[2].first).rules()));
		System.out.println("UR 2 and 0: "  + equalsUnary(((UnaryGrammar)grammars[2].first).rules(),((UnaryGrammar)grammars[0].first).rules()));
		
		System.out.println("BR 0 and 1: "  + equalsBinary(((BinaryGrammar)grammars[0].second).rules(),((BinaryGrammar)grammars[1].second).rules()));
		System.out.println("BR 1 and 2: " + equalsBinary(((BinaryGrammar)grammars[1].second).rules(),((BinaryGrammar)grammars[2].second).rules()));
		System.out.println("BR 2 and 0: " + equalsBinary(((BinaryGrammar)grammars[2].second).rules(),((BinaryGrammar)grammars[0].second).rules()));
		
		System.exit(0);
		
		// now go through the grammars we made and see if they are equal!
		Set[] unaryRules = new Set[3];
		Set[] binaryRules = new Set[3];
		for (int i=0; i<grammars.length; i++) {
		unaryRules[i] = new HashSet();
		System.out.println(i + " size: " + ((UnaryGrammar)grammars[i].first()).numRules());
		for (Iterator unRuleI = ((UnaryGrammar)grammars[i].first()).iterator(); unRuleI.hasNext();) {
		UnaryRule ur = (UnaryRule) unRuleI.next();
		String parent = (String) stateNumberers[i].object(ur.parent);
		String child = (String) stateNumberers[i].object(ur.child);
		unaryRules[i].add(new StringUnaryRule(parent, child, ur.score));
		}
		binaryRules[i] = new HashSet();
		System.out.println(i + " size: " + ((BinaryGrammar)grammars[i].second()).numRules());
		for (Iterator binRuleI = ((BinaryGrammar)grammars[i].second()).iterator(); binRuleI.hasNext();) {
		BinaryRule br = (BinaryRule) binRuleI.next();
		String parent = (String) stateNumberers[i].object(br.parent);
		String leftChild = (String) stateNumberers[i].object(br.leftChild);
		String rightChild = (String) stateNumberers[i].object(br.rightChild);
		binaryRules[i].add(new StringBinaryRule(parent, leftChild, rightChild, br.score));
		}
		}
		
		System.out.println("uR 0 and 1: " + equals(unaryRules[0],unaryRules[1]));
		System.out.println("uR 1 and 2: " + equals(unaryRules[1],unaryRules[2]));
		System.out.println("uR 2 and 0: " + equals(unaryRules[2],unaryRules[0]));
		
		System.out.println("bR 0 and 1: " + equals(binaryRules[0],binaryRules[1]));
		System.out.println("bR 1 and 2: " + equals(binaryRules[1],binaryRules[2]));
		System.out.println("bR 2 and 0: " + equals(binaryRules[2],binaryRules[0]));
		
		}
		*/
		/*
		public static void testCategoryMergingProblem() {
		LexicalizedParser lp = new LexicalizedParser(path, new NumberRangeFileFilter(trainLow, trainHigh, true), tlpParams);
		
		// test it without the change
		Treebank testTreebank = tlpParams.testMemoryTreebank();
		testTreebank.loadPath(path, new NumberRangeFileFilter(testLow, testHigh, true));
		System.out.println("Currently " + new Date());
		lp.testOnTreebank(testTreebank);
		System.out.println("Currently " + new Date());
		
		// pull out the rules and consistently change the name of one of the states
		ParserData pd = lp.parserData();
		BinaryGrammar bg = pd.bg;
		UnaryGrammar ug = pd.ug;
		Numberer stateNumberer = Numberer.getGlobalNumberer("states");
		UnaryGrammar newUG = new UnaryGrammar(stateNumberer.total()+1);
		for (Iterator urIter = ug.iterator(); urIter.hasNext();) {
		UnaryRule rule = (UnaryRule) urIter.next();
		rule.parent = changeIfNecessary(rule.parent, stateNumberer);
		rule.child = changeIfNecessary(rule.child, stateNumberer);
		newUG.addRule(rule);
		}
		BinaryGrammar newBG = new BinaryGrammar(stateNumberer.total()+1);
		for (Iterator urIter = bg.iterator(); urIter.hasNext();) {
		BinaryRule rule = (BinaryRule) urIter.next();
		rule.parent = changeIfNecessary(rule.parent, stateNumberer);
		rule.leftChild = changeIfNecessary(rule.leftChild, stateNumberer);
		rule.rightChild = changeIfNecessary(rule.rightChild, stateNumberer);
		newBG.addRule(rule);
		}
		newUG.purgeRules();
		newBG.splitRules();
		pd.ug = newUG;
		pd.bg = newBG;
		lp = new LexicalizedParser(pd);
		
		// test it with the change
		testTreebank = tlpParams.testMemoryTreebank();
		testTreebank.loadPath(path, new NumberRangeFileFilter(testLow, testHigh, true));
		System.out.println("Currently " + new Date());
		lp.testOnTreebank(testTreebank);
		System.out.println("Currently " + new Date());
		}
		*/
		public virtual Pair<UnaryGrammar, BinaryGrammar> TranslateAndSort(Pair<UnaryGrammar, BinaryGrammar> grammar, IIndex<string> oldIndex, IIndex<string> newIndex)
		{
			System.Console.Out.WriteLine("oldIndex.size()" + oldIndex.Size() + " newIndex.size()" + newIndex.Size());
			UnaryGrammar ug = grammar.first;
			IList<UnaryRule> unaryRules = new List<UnaryRule>();
			foreach (UnaryRule rule in ug.Rules())
			{
				rule.parent = Translate(rule.parent, oldIndex, newIndex);
				rule.child = Translate(rule.child, oldIndex, newIndex);
				unaryRules.Add(rule);
			}
			unaryRules.Sort();
			UnaryGrammar newUG = new UnaryGrammar(newIndex);
			foreach (UnaryRule unaryRule in unaryRules)
			{
				newUG.AddRule(unaryRule);
			}
			newUG.PurgeRules();
			BinaryGrammar bg = grammar.second;
			IList<BinaryRule> binaryRules = new List<BinaryRule>();
			foreach (BinaryRule rule_1 in bg.Rules())
			{
				rule_1.parent = Translate(rule_1.parent, oldIndex, newIndex);
				rule_1.leftChild = Translate(rule_1.leftChild, oldIndex, newIndex);
				rule_1.rightChild = Translate(rule_1.rightChild, oldIndex, newIndex);
				binaryRules.Add(rule_1);
			}
			unaryRules.Sort();
			BinaryGrammar newBG = new BinaryGrammar(newIndex);
			foreach (BinaryRule binaryRule in binaryRules)
			{
				newBG.AddRule(binaryRule);
			}
			newBG.SplitRules();
			return Generics.NewPair(newUG, newBG);
		}

		private static int Translate(int i, IIndex<string> oldIndex, IIndex<string> newIndex)
		{
			return newIndex.AddToIndex(oldIndex.Get(i));
		}

		// WTF is this?
		public virtual int ChangeIfNecessary(int i, IIndex<string> n)
		{
			string s = n.Get(i);
			if (s.Equals("NP^PP"))
			{
				System.Console.Out.WriteLine("changed");
				return n.AddToIndex("NP-987928374");
			}
			return i;
		}

		public virtual bool EqualsBinary(IList<BinaryRule> l1, IList<BinaryRule> l2)
		{
			// put each into a map to itself
			IDictionary<BinaryRule, BinaryRule> map1 = Generics.NewHashMap();
			foreach (BinaryRule o in l1)
			{
				map1[o] = o;
			}
			IDictionary<BinaryRule, BinaryRule> map2 = Generics.NewHashMap();
			foreach (BinaryRule o_1 in l2)
			{
				map2[o_1] = o_1;
			}
			bool isEqual = true;
			foreach (BinaryRule rule1 in map1.Keys)
			{
				BinaryRule rule2 = map2[rule1];
				if (rule2 == null)
				{
					System.Console.Out.WriteLine("no rule for " + rule1);
					isEqual = false;
				}
				else
				{
					Sharpen.Collections.Remove(map2, rule2);
					if (rule1.score != rule2.score)
					{
						System.Console.Out.WriteLine(rule1 + " and " + rule2 + " have diff scores");
						isEqual = false;
					}
				}
			}
			System.Console.Out.WriteLine("left over: " + map2.Keys);
			return isEqual;
		}

		public virtual bool EqualsUnary(IList<UnaryRule> l1, IList<UnaryRule> l2)
		{
			// put each into a map to itself
			IDictionary<UnaryRule, UnaryRule> map1 = Generics.NewHashMap();
			foreach (UnaryRule o in l1)
			{
				map1[o] = o;
			}
			IDictionary<UnaryRule, UnaryRule> map2 = Generics.NewHashMap();
			foreach (UnaryRule o_1 in l2)
			{
				map2[o_1] = o_1;
			}
			bool isEqual = true;
			foreach (UnaryRule rule1 in map1.Keys)
			{
				UnaryRule rule2 = map2[rule1];
				if (rule2 == null)
				{
					System.Console.Out.WriteLine("no rule for " + rule1);
					isEqual = false;
				}
				else
				{
					Sharpen.Collections.Remove(map2, rule2);
					if (rule1.score != rule2.score)
					{
						System.Console.Out.WriteLine(rule1 + " and " + rule2 + " have diff scores");
						isEqual = false;
					}
				}
			}
			System.Console.Out.WriteLine("left over: " + map2.Keys);
			return isEqual;
		}

		private static bool EqualSets<T>(ICollection<T> set1, ICollection<T> set2)
		{
			bool isEqual = true;
			if (set1.Count != set2.Count)
			{
				System.Console.Out.WriteLine("sizes different: " + set1.Count + " vs. " + set2.Count);
				isEqual = false;
			}
			ICollection<T> newSet1 = (ICollection<T>)((HashSet<T>)set1).Clone();
			newSet1.RemoveAll(set2);
			if (newSet1.Count > 0)
			{
				isEqual = false;
				System.Console.Out.WriteLine("set1 left with: " + newSet1);
			}
			ICollection<T> newSet2 = (ICollection<T>)((HashSet<T>)set2).Clone();
			newSet2.RemoveAll(set1);
			if (newSet2.Count > 0)
			{
				isEqual = false;
				System.Console.Out.WriteLine("set2 left with: " + newSet2);
			}
			return isEqual;
		}

		/*
		public static void testAutomatonCompaction() {
		// make our LossyAutomatonCompactor from the parameters passed at command line
		// now set up the compactor2 constructor args
		// extract a bunch of paths
		Timing.startTime();
		System.out.print("Extracting paths from treebank...");
		allTrainPaths = extractPaths(path, trainLow, trainHigh, false);
		allTestPaths = extractPaths(path, testLow, testHigh, false);
		Timing.tick("done");
		
		// for each category, construct an automaton and then compact it
		for (Iterator catIter = allTrainPaths.keySet().iterator(); catIter.hasNext();) {
		// construct an automaton from the paths
		String category = (String) catIter.next();
		List trainPaths = (List) allTrainPaths.get(category);
		List testPaths = (List) allTestPaths.get(category);
		if (testPaths == null) testPaths = new ArrayList();
		// now make the graph with the training paths (the LossyAutomatonCompactor will reestimate the weights anyway)
		TransducerGraph graph = TransducerGraph.createGraphFromPaths(trainPaths, 3);
		System.out.println("Created graph for: " + category);
		
		System.out.println();
		int numArcs1 = graph.getArcs().size();
		
		LossyAutomatonCompactor compactor = new LossyAutomatonCompactor(3, // horizonOrder, 1 means that only exactly compatible merges are considered
		0, // min nmber of arcs
		10000000.0, // maxMergeCost
		0.5, // splitParam
		false, //  ignoreUnsupportedSuffixes
		-1000, // minArcCost
		trainPaths,
		testPaths,
		LossyAutomatonCompactor.DATA_LIKELIHOOD_COST, // costModel
		false); // verbose
		
		TransducerGraph result = compactor.compactFA(graph);
		//do we need this?      result = new TransducerGraph(result, ntsp);  // pull out strings from sets returned by minimizer
		int numArcs2 = result.getArcs().size();
		System.out.println("LossyGrammarCompactor compacted "+category+" from " + numArcs1 + " to " + numArcs2 + " arcs");
		
		}
		
		
		}
		*/
		private static int NumTokens<T>(IList<IList<T>> paths)
		{
			int result = 0;
			foreach (IList<T> path in paths)
			{
				result += path.Count;
			}
			return result;
		}

		public virtual void BuildAndCompactToyGrammars()
		{
			// extract a bunch of paths
			System.Console.Out.Write("Extracting other paths...");
			allTrainPaths = ExtractPaths(path, trainLow, trainHigh, true);
			TransducerGraph.INodeProcessor ntsp = new TransducerGraph.SetToStringNodeProcessor(new PennTreebankLanguagePack());
			TransducerGraph.INodeProcessor otsp = new TransducerGraph.ObjectToSetNodeProcessor();
			TransducerGraph.IArcProcessor isp = new TransducerGraph.InputSplittingProcessor();
			TransducerGraph.IArcProcessor ocp = new TransducerGraph.OutputCombiningProcessor();
			TransducerGraph.IGraphProcessor normalizer = new TransducerGraph.NormalizingGraphProcessor(false);
			TransducerGraph.IGraphProcessor quasiDeterminizer = new QuasiDeterminizer();
			IAutomatonMinimizer exactMinimizer = new FastExactAutomatonMinimizer();
			foreach (string key in allTrainPaths.Keys)
			{
				System.Console.Out.WriteLine("creating graph for " + key);
				IList<IList<string>> paths = allTrainPaths[key];
				ClassicCounter<IList<string>> pathCounter = new ClassicCounter<IList<string>>();
				foreach (IList<string> o in paths)
				{
					pathCounter.IncrementCount(o);
				}
				ClassicCounter<IList<string>> newPathCounter = RemoveLowCountPaths(pathCounter, 2);
				paths.RetainAll(newPathCounter.KeySet());
				// get rid of the low count ones
				TransducerGraph result = TransducerGraph.CreateGraphFromPaths(newPathCounter, 1000);
				// exact compaction
				int numArcs = result.GetArcs().Count;
				int numNodes = result.GetNodes().Count;
				if (numArcs == 0)
				{
					continue;
				}
				System.Console.Out.WriteLine("initial graph has " + numArcs + " arcs and " + numNodes + " nodes.");
				GrammarCompactor.WriteFile(result, "unminimized", key);
				// do exact minimization
				result = normalizer.ProcessGraph(result);
				// normalize it so that exact minimization works properly
				result = quasiDeterminizer.ProcessGraph(result);
				// push probabilities left or down
				result = new TransducerGraph(result, ocp);
				// combine outputs into inputs
				result = exactMinimizer.MinimizeFA(result);
				// minimize the thing
				result = new TransducerGraph(result, ntsp);
				// pull out strings from sets returned by minimizer
				result = new TransducerGraph(result, isp);
				// split outputs from inputs
				numArcs = result.GetArcs().Count;
				numNodes = result.GetNodes().Count;
				System.Console.Out.WriteLine("after exact minimization graph has " + numArcs + " arcs and " + numNodes + " nodes.");
				GrammarCompactor.WriteFile(result, "exactminimized", key);
			}
		}

		// do additional lossy minimization
		/*
		NewLossyAutomatonCompactor compactor2 = new NewLossyAutomatonCompactor(paths, true);
		result = compactor2.compactFA(result);
		result = new TransducerGraph(result, ntsp);  // pull out strings from sets returned by minimizer
		numArcs = result.getArcs().size();
		numNodes = result.getNodes().size();
		
		System.out.println("after lossy minimization graph has " + numArcs + " arcs and " + numNodes + " nodes.");
		GrammarCompactor.writeFile(result, "lossyminimized", key);
		*/
		private static ClassicCounter<IList<string>> RemoveLowCountPaths(ClassicCounter<IList<string>> paths, double thresh)
		{
			ClassicCounter<IList<string>> result = new ClassicCounter<IList<string>>();
			int numRetained = 0;
			foreach (IList<string> path in paths.KeySet())
			{
				double count = paths.GetCount(path);
				if (count >= thresh)
				{
					result.SetCount(path, count);
					numRetained++;
				}
			}
			System.Console.Out.WriteLine("retained " + numRetained);
			return result;
		}

		public virtual void TestGrammarCompaction()
		{
			// these for testing against the markov 3rd order baseline
			// use the parser constructor to extract the grammars from the treebank
			op = new Options();
			LexicalizedParser lp = LexicalizedParser.TrainFromTreebank(path, new NumberRangeFileFilter(trainLow, trainHigh, true), op);
			// compact grammars
			if (compactor != null)
			{
				// extract a bunch of paths
				Timing.StartTime();
				System.Console.Out.Write("Extracting other paths...");
				allTrainPaths = ExtractPaths(path, trainLow, trainHigh, true);
				allTestPaths = ExtractPaths(path, testLow, testHigh, true);
				Timing.Tick("done");
				// compact grammars
				Timing.StartTime();
				System.Console.Out.Write("Compacting grammars...");
				Pair<UnaryGrammar, BinaryGrammar> grammar = Generics.NewPair(lp.ug, lp.bg);
				Triple<IIndex<string>, UnaryGrammar, BinaryGrammar> compactedGrammar = compactor.CompactGrammar(grammar, allTrainPaths, allTestPaths, lp.stateIndex);
				lp.stateIndex = compactedGrammar.First();
				lp.ug = compactedGrammar.Second();
				lp.bg = compactedGrammar.Third();
				Timing.Tick("done.");
			}
			if (asciiOutputPath != null)
			{
				lp.SaveParserToTextFile(asciiOutputPath);
			}
			// test it
			Treebank testTreebank = op.tlpParams.TestMemoryTreebank();
			testTreebank.LoadPath(path, new NumberRangeFileFilter(testLow, testHigh, true));
			System.Console.Out.WriteLine("Currently " + new DateTime());
			EvaluateTreebank evaluator = new EvaluateTreebank(lp);
			evaluator.TestOnTreebank(testTreebank);
			System.Console.Out.WriteLine("Currently " + new DateTime());
		}
	}

	internal class StringUnaryRule
	{
		public string parent;

		public string child;

		public double score;

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Parser.Lexparser.StringUnaryRule))
			{
				return false;
			}
			Edu.Stanford.Nlp.Parser.Lexparser.StringUnaryRule stringUnaryRule = (Edu.Stanford.Nlp.Parser.Lexparser.StringUnaryRule)o;
			if (score != stringUnaryRule.score)
			{
				return false;
			}
			if (child != null ? !child.Equals(stringUnaryRule.child) : stringUnaryRule.child != null)
			{
				return false;
			}
			if (parent != null ? !parent.Equals(stringUnaryRule.parent) : stringUnaryRule.parent != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result;
			long temp;
			result = (parent != null ? parent.GetHashCode() : 0);
			result = 29 * result + (child != null ? child.GetHashCode() : 0);
			temp = double.DoubleToLongBits(score);
			result = 29 * result + (int)(temp ^ ((long)(((ulong)temp) >> 32)));
			return result;
		}

		public override string ToString()
		{
			return "UR:::::" + parent + ":::::" + child + ":::::" + score;
		}

		public StringUnaryRule(string parent, string child, double score)
		{
			this.parent = parent;
			this.child = child;
			this.score = score;
		}
	}

	internal class StringBinaryRule
	{
		public string parent;

		public string leftChild;

		public string rightChild;

		public double score;

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Parser.Lexparser.StringBinaryRule))
			{
				return false;
			}
			Edu.Stanford.Nlp.Parser.Lexparser.StringBinaryRule stringBinaryRule = (Edu.Stanford.Nlp.Parser.Lexparser.StringBinaryRule)o;
			if (score != stringBinaryRule.score)
			{
				return false;
			}
			if (leftChild != null ? !leftChild.Equals(stringBinaryRule.leftChild) : stringBinaryRule.leftChild != null)
			{
				return false;
			}
			if (parent != null ? !parent.Equals(stringBinaryRule.parent) : stringBinaryRule.parent != null)
			{
				return false;
			}
			if (rightChild != null ? !rightChild.Equals(stringBinaryRule.rightChild) : stringBinaryRule.rightChild != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result;
			long temp;
			result = (parent != null ? parent.GetHashCode() : 0);
			result = 29 * result + (leftChild != null ? leftChild.GetHashCode() : 0);
			result = 29 * result + (rightChild != null ? rightChild.GetHashCode() : 0);
			temp = double.DoubleToLongBits(score);
			result = 29 * result + (int)(temp ^ ((long)(((ulong)temp) >> 32)));
			return result;
		}

		public override string ToString()
		{
			return "BR:::::" + parent + ":::::" + leftChild + ":::::" + rightChild + ":::::" + score;
		}

		public StringBinaryRule(string parent, string leftChild, string rightChild, double score)
		{
			this.parent = parent;
			this.leftChild = leftChild;
			this.rightChild = rightChild;
			this.score = score;
		}
	}
}
