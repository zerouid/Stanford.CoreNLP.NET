using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Checks the coverage of rules in a grammar on a test treebank.</summary>
	/// <author>Teg Grenager</author>
	public class GrammarCoverageChecker
	{
		private Options op;

		private void TestOnTreebank(LexicalizedParser pd, ITreebankLangParserParams tlpParams, Treebank testTreebank, string treebankRoot, IIndex<string> stateIndex)
		{
			Timing.StartTime();
			ITreeTransformer annotator = new TreeAnnotator(tlpParams.HeadFinder(), tlpParams, op);
			// CDM: Aug 2004: With new implementation of treebank split categories,
			// I've hardwired this to load English ones.  Otherwise need training data.
			// op.trainOptions.splitters = new HashSet(Arrays.asList(op.tlpParams.splitters()));
			op.trainOptions.splitters = ParentAnnotationStats.GetEnglishSplitCategories(treebankRoot);
			op.trainOptions.sisterSplitters = Generics.NewHashSet(Arrays.AsList(op.tlpParams.SisterSplitters()));
			foreach (Tree goldTree in testTreebank)
			{
				goldTree = annotator.TransformTree(goldTree);
				//      System.out.println();
				//      System.out.println("Checking tree: " + goldTree);
				foreach (Tree localTree in goldTree)
				{
					// now try to use the grammar to score this local tree
					if (localTree.IsLeaf() || localTree.IsPreTerminal() || localTree.Children().Length < 2)
					{
						continue;
					}
					System.Console.Out.WriteLine(LocalTreeToRule(localTree));
					double score = ComputeLocalTreeScore(localTree, stateIndex, pd);
					if (score == double.NegativeInfinity)
					{
					}
					//          System.out.println(localTreeToRule(localTree));
					System.Console.Out.WriteLine("score: " + score);
				}
			}
		}

		private static string LocalTreeToRule(Tree localTree)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(localTree.Value()).Append(" -> ");
			for (int i = 0; i < localTree.Children().Length - 1; i++)
			{
				sb.Append(localTree.Children()[i].Value()).Append(" ");
			}
			sb.Append(localTree.Children()[localTree.Children().Length - 1].Value());
			return sb.ToString();
		}

		private static double ComputeLocalTreeScore(Tree localTree, IIndex<string> stateIndex, LexicalizedParser pd)
		{
			try
			{
				string parent = localTree.Value();
				int parentState = stateIndex.IndexOf(parent);
				//      System.out.println("parentState: " + parentState);
				Tree[] children = localTree.Children();
				// let's find the unary to kick things off with the left child (since we assume a left to right grammar
				// first we create the synthetic parent of the leftmost child
				string nextChild = children[0].Value();
				// childState = stateIndex.indexOf(nextChild);
				string current = "@" + parent + "| [ [" + nextChild + "] ";
				int currentState = stateIndex.IndexOf(current);
				IList<UnaryRule> rules = pd.ug.RulesByParent(currentState);
				UnaryRule ur = rules[0];
				//      System.out.println("rule: " + ur);
				double localTreeScore = ur.Score();
				// go through rest of rules
				for (int i = 1; i < children.Length; i++)
				{
					// find rules in BinaryGrammar that can extend this state
					//        System.out.println("currentState: " + currentState);
					nextChild = children[i].Value();
					int childState = stateIndex.IndexOf(nextChild);
					//        System.out.println("childState: " + childState);
					IList<BinaryRule> l = pd.bg.RuleListByLeftChild(currentState);
					BinaryRule foundBR = null;
					if (i < children.Length - 1)
					{
						// need to the rewrite that doesn't rewrite to the parent
						foreach (BinaryRule br in l)
						{
							//            System.out.println("\t\trule: " + br + " parent: " + br.parent + " right: " + br.rightChild);
							if (br.rightChild == childState && br.parent != parentState)
							{
								foundBR = br;
								break;
							}
						}
					}
					else
					{
						// this is the last rule, need to find the rewrite to the parent of the whole local tree
						foreach (BinaryRule br in l)
						{
							//            System.out.println("\t\trule: " + br + " parent: " + br.parent + " right: " + br.rightChild);
							if (br.rightChild == childState && br.parent == parentState)
							{
								foundBR = br;
								break;
							}
						}
					}
					if (foundBR == null)
					{
						// we never found a matching rule!
						//          System.out.println("broke on " + nextChild);
						return double.NegativeInfinity;
					}
					//        System.out.println("rule: " + foundBR);
					currentState = foundBR.parent;
					localTreeScore += foundBR.score;
				}
				// end loop through children
				return localTreeScore;
			}
			catch (NoSuchElementException)
			{
				// we couldn't find a state for one of the needed categories
				//      System.out.println("no state found: " + e.toString());
				//      List tempRules = pd.ug.rulesByChild(childState);
				//      for (Iterator iter = tempRules.iterator(); iter.hasNext();) {
				//        UnaryRule ur = (UnaryRule) iter.next();
				//        System.out.println("\t\t\trule with child: " + ur);
				//      }
				return double.NegativeInfinity;
			}
		}

		/// <summary>Usage: java edu.stanford.nlp.parser.lexparser.GrammarCoverageChecker parserFile treebankPath low high [optionFlags*]</summary>
		public static void Main(string[] args)
		{
			new GrammarCoverageChecker().RunTest(args);
		}

		public virtual void RunTest(string[] args)
		{
			// get a parser from file
			LexicalizedParser pd = ((LexicalizedParser)LexicalizedParser.LoadModel(args[0]));
			op = pd.GetOp();
			// in case a serialized options was read in
			Treebank testTreebank = op.tlpParams.MemoryTreebank();
			int testlow = System.Convert.ToInt32(args[2]);
			int testhigh = System.Convert.ToInt32(args[3]);
			testTreebank.LoadPath(args[1], new NumberRangeFileFilter(testlow, testhigh, true));
			op.SetOptionsOrWarn(args, 4, args.Length);
			TestOnTreebank(pd, new EnglishTreebankParserParams(), testTreebank, args[1], pd.stateIndex);
		}
	}
}
