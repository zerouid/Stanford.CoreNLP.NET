using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>
	/// Implementation of the Leaf Ancestor metric first described by Sampson and Babarczy (2003) and
	/// later analyzed more completely by Clegg and Shepherd (2005).
	/// </summary>
	/// <remarks>
	/// Implementation of the Leaf Ancestor metric first described by Sampson and Babarczy (2003) and
	/// later analyzed more completely by Clegg and Shepherd (2005).
	/// <p>
	/// This implementation assumes that the guess/gold input files are of equal length, and have one tree per
	/// line.
	/// <p>
	/// TODO (spenceg): This implementation doesn't insert the "boundary symbols" as described by both
	/// Sampson and Clegg. Need to add those.
	/// </remarks>
	/// <author>Spence Green</author>
	public class LeafAncestorEval
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Metrics.LeafAncestorEval));

		private readonly string name;

		private const bool Debug = false;

		private double sentAvg = 0.0;

		private double sentNum = 0.0;

		private int sentExact = 0;

		private double corpusAvg = 0.0;

		private double corpusNum = 0.0;

		private readonly IDictionary<IList<CoreLabel>, double> catAvg;

		private readonly IDictionary<IList<CoreLabel>, double> catNum;

		public LeafAncestorEval(string str)
		{
			//Corpus level (macro-averaged)
			//Sentence level (micro-averaged)
			//Category level
			this.name = str;
			catAvg = Generics.NewHashMap();
			catNum = Generics.NewHashMap();
		}

		/// <summary>
		/// Depth-first (post-order) search through the tree, recording the stack state as the
		/// lineage every time a terminal is reached.
		/// </summary>
		/// <remarks>
		/// Depth-first (post-order) search through the tree, recording the stack state as the
		/// lineage every time a terminal is reached.
		/// This implementation uses the Index annotation to store depth. If CoreLabels are
		/// not present in the trees (or at least something that implements HasIndex), an exception will result.
		/// </remarks>
		/// <param name="t">The tree</param>
		/// <returns>A list of lineages</returns>
		private static IList<IList<CoreLabel>> MakeLineages(Tree t)
		{
			if (t == null)
			{
				return null;
			}
			((IHasIndex)t.Label()).SetIndex(0);
			Stack<Tree> treeStack = new Stack<Tree>();
			treeStack.Push(t);
			Stack<CoreLabel> labelStack = new Stack<CoreLabel>();
			CoreLabel rootLabel = new CoreLabel(t.Label());
			rootLabel.SetIndex(0);
			labelStack.Push(rootLabel);
			IList<IList<CoreLabel>> lineages = new List<IList<CoreLabel>>();
			while (!treeStack.IsEmpty())
			{
				Tree node = treeStack.Pop();
				int nodeDepth = ((IHasIndex)node.Label()).Index();
				while (!labelStack.IsEmpty() && labelStack.Peek().Index() != nodeDepth - 1)
				{
					labelStack.Pop();
				}
				if (node.IsPreTerminal())
				{
					IList<CoreLabel> lin = new List<CoreLabel>(labelStack);
					lineages.Add(lin);
				}
				else
				{
					foreach (Tree kid in node.Children())
					{
						((IHasIndex)kid.Label()).SetIndex(nodeDepth + 1);
						treeStack.Push(kid);
					}
					CoreLabel nodeLabel = new CoreLabel(node.Label());
					nodeLabel.SetIndex(nodeDepth);
					labelStack.Add(nodeLabel);
				}
			}
			return lineages;
		}

		private void UpdateCatAverages(IList<CoreLabel> lineage, double score)
		{
			if (catAvg[lineage] == null)
			{
				catAvg[lineage] = score;
				catNum[lineage] = 1.0;
			}
			else
			{
				double newAvg = catAvg[lineage] + score;
				catAvg[lineage] = newAvg;
				double newNum = catNum[lineage] + 1.0;
				catNum[lineage] = newNum;
			}
		}

		public virtual void Evaluate(Tree guess, Tree gold, PrintWriter pw)
		{
			if (gold == null || guess == null)
			{
				System.Console.Error.Printf("%s: Cannot compare against a null gold or guess tree!%n", this.GetType().FullName);
				return;
			}
			IList<IList<CoreLabel>> guessLineages = MakeLineages(guess);
			IList<IList<CoreLabel>> goldLineages = MakeLineages(gold);
			if (guessLineages.Count == goldLineages.Count)
			{
				double localScores = 0.0;
				for (int i = 0; i < guessLineages.Count; i++)
				{
					IList<CoreLabel> guessLin = guessLineages[i];
					IList<CoreLabel> goldLin = goldLineages[i];
					double levDist = EditDistance(guessLin, goldLin);
					double la = 1.0 - (levDist / (double)(guessLin.Count + goldLin.Count));
					localScores += la;
					UpdateCatAverages(goldLin, la);
				}
				corpusAvg += localScores;
				corpusNum += goldLineages.Count;
				double localSentAvg = localScores / goldLineages.Count;
				if (localSentAvg == 1.0)
				{
					sentExact++;
				}
				sentAvg += localSentAvg;
				sentNum++;
			}
			else
			{
				System.Console.Error.Printf("%s: Number of guess (%d) gold (%d) don't match!%n", this.GetType().FullName, guessLineages.Count, goldLineages.Count);
				log.Info("Cannot evaluate!");
				System.Console.Error.Printf("GUESS tree:%n%s%n", guess.ToString());
				System.Console.Error.Printf("GOLD tree:%n%s%n", gold.ToString());
			}
		}

		/// <summary>Computes Levenshtein edit distance between two lists of labels;</summary>
		/// <param name="l1"/>
		/// <param name="l2"/>
		private static int EditDistance(IList<CoreLabel> l1, IList<CoreLabel> l2)
		{
			int[][] m = new int[][] {  };
			for (int i = 1; i <= l1.Count; i++)
			{
				m[i][0] = i;
			}
			for (int j = 1; j <= l2.Count; j++)
			{
				m[0][j] = j;
			}
			for (int i_1 = 1; i_1 <= l1.Count; i_1++)
			{
				for (int j_1 = 1; j_1 <= l2.Count; j_1++)
				{
					m[i_1][j_1] = Math.Min(m[i_1 - 1][j_1 - 1] + ((l1[i_1 - 1].Equals(l2[j_1 - 1])) ? 0 : 1), m[i_1 - 1][j_1] + 1);
					m[i_1][j_1] = Math.Min(m[i_1][j_1], m[i_1][j_1 - 1] + 1);
				}
			}
			return m[l1.Count][l2.Count];
		}

		private static string ToString(IList<CoreLabel> lineage)
		{
			StringBuilder sb = new StringBuilder();
			foreach (CoreLabel cl in lineage)
			{
				sb.Append(cl.Value());
				sb.Append(" <-- ");
			}
			return sb.ToString();
		}

		public virtual void Display(bool verbose, PrintWriter pw)
		{
			Random rand = new Random();
			double corpusLevel = corpusAvg / corpusNum;
			double sentLevel = sentAvg / sentNum;
			double sentEx = 100.0 * sentExact / sentNum;
			if (verbose)
			{
				IDictionary<double, IList<CoreLabel>> avgMap = new SortedDictionary<double, IList<CoreLabel>>();
				foreach (KeyValuePair<IList<CoreLabel>, double> entry in catAvg)
				{
					double avg = entry.Value / catNum[entry.Key];
					if (double.IsNaN(avg))
					{
						avg = -1.0;
					}
					if (avgMap.Contains(avg))
					{
						avgMap[avg + (rand.NextDouble() / 10000.0)] = entry.Key;
					}
					else
					{
						avgMap[avg] = entry.Key;
					}
				}
				pw.Println("============================================================");
				pw.Println("Leaf Ancestor Metric" + "(" + name + ") -- final statistics");
				pw.Println("============================================================");
				pw.Println("#Sentences: " + (int)sentNum);
				pw.Println();
				pw.Println("Sentence-level (macro-averaged)");
				pw.Printf(" Avg: %.3f%n", sentLevel);
				pw.Printf(" Exact: %.2f%%%n", sentEx);
				pw.Println();
				pw.Println("Corpus-level (micro-averaged)");
				pw.Printf(" Avg: %.3f%n", corpusLevel);
				pw.Println("============================================================");
				foreach (IList<CoreLabel> lineage in avgMap.Values)
				{
					if (catNum[lineage] < 30.0)
					{
						continue;
					}
					double avg = catAvg[lineage] / catNum[lineage];
					pw.Printf(" %.3f\t%d\t%s%n", avg, (int)((double)catNum[lineage]), ToString(lineage));
				}
				pw.Println("============================================================");
			}
			else
			{
				pw.Printf("%s summary: corpus: %.3f sent: %.3f sent-ex: %.2f%n", name, corpusLevel, sentLevel, sentEx);
			}
		}

		private static readonly string Usage = string.Format("Usage: java %s [OPTS] goldFile guessFile%n%nOptions:%n  -l lang   : Language name %s%n" + "  -y num    : Skip gold trees with yields longer than num.%n  -v        : Verbose output%n", typeof(
			Edu.Stanford.Nlp.Parser.Metrics.LeafAncestorEval).FullName, Language.langList);

		private const int MinArgs = 2;

		private static bool Verbose = false;

		private static Language Language = Language.English;

		private static int MaxGoldYield = int.MaxValue;

		private static File guessFile = null;

		private static File goldFile = null;

		public static readonly IDictionary<string, int> optionArgDefs = Generics.NewHashMap();

		static LeafAncestorEval()
		{
			//Command line options
			optionArgDefs["-y"] = 1;
			optionArgDefs["-l"] = 1;
			optionArgDefs["-v"] = 0;
		}

		private static bool ValidateCommandLine(string[] args)
		{
			IDictionary<string, string[]> argsMap = StringUtils.ArgsToMap(args, optionArgDefs);
			foreach (KeyValuePair<string, string[]> opt in argsMap)
			{
				string key = opt.Key;
				if (key != null)
				{
					switch (key)
					{
						case "-y":
						{
							MaxGoldYield = System.Convert.ToInt32(opt.Value[0]);
							break;
						}

						case "-l":
						{
							Language = Language.ValueOf(opt.Value[0]);
							break;
						}

						case "-v":
						{
							Verbose = true;
							break;
						}

						default:
						{
							return false;
						}
					}
				}
			}
			//Regular arguments
			string[] rest = argsMap[null];
			if (rest == null || rest.Length != MinArgs)
			{
				return false;
			}
			else
			{
				goldFile = new File(rest[0]);
				guessFile = new File(rest[1]);
			}
			return true;
		}

		/// <summary>Execute with no arguments for usage.</summary>
		public static void Main(string[] args)
		{
			if (!ValidateCommandLine(args))
			{
				log.Info(Usage);
				System.Environment.Exit(-1);
			}
			ITreebankLangParserParams tlpp = Language.@params;
			PrintWriter pwOut = tlpp.Pw();
			Treebank guessTreebank = tlpp.DiskTreebank();
			guessTreebank.LoadPath(guessFile);
			pwOut.Println("GUESS TREEBANK:");
			pwOut.Println(guessTreebank.TextualSummary());
			Treebank goldTreebank = tlpp.DiskTreebank();
			goldTreebank.LoadPath(goldFile);
			pwOut.Println("GOLD TREEBANK:");
			pwOut.Println(goldTreebank.TextualSummary());
			Edu.Stanford.Nlp.Parser.Metrics.LeafAncestorEval metric = new Edu.Stanford.Nlp.Parser.Metrics.LeafAncestorEval("LeafAncestor");
			ITreeTransformer tc = tlpp.Collinizer();
			//The evalb ref implementation assigns status for each tree pair as follows:
			//
			//   0 - Ok (yields match)
			//   1 - length mismatch
			//   2 - null parse e.g. (()).
			//
			//In the cases of 1,2, evalb does not include the tree pair in the LP/LR computation.
			IEnumerator<Tree> goldItr = goldTreebank.GetEnumerator();
			IEnumerator<Tree> guessItr = guessTreebank.GetEnumerator();
			int goldLineId = 0;
			int guessLineId = 0;
			int skippedGuessTrees = 0;
			while (guessItr.MoveNext() && goldItr.MoveNext())
			{
				Tree guessTree = guessItr.Current;
				IList<ILabel> guessYield = guessTree.Yield();
				guessLineId++;
				Tree goldTree = goldItr.Current;
				IList<ILabel> goldYield = goldTree.Yield();
				goldLineId++;
				// Check that we should evaluate this tree
				if (goldYield.Count > MaxGoldYield)
				{
					skippedGuessTrees++;
					continue;
				}
				// Only trees with equal yields can be evaluated
				if (goldYield.Count != guessYield.Count)
				{
					pwOut.Printf("Yield mismatch gold: %d tokens vs. guess: %d tokens (lines: gold %d guess %d)%n", goldYield.Count, guessYield.Count, goldLineId, guessLineId);
					skippedGuessTrees++;
					continue;
				}
				Tree evalGuess = tc.TransformTree(guessTree);
				Tree evalGold = tc.TransformTree(goldTree);
				metric.Evaluate(evalGuess, evalGold, ((Verbose) ? pwOut : null));
			}
			if (guessItr.MoveNext() || goldItr.MoveNext())
			{
				System.Console.Error.Printf("Guess/gold files do not have equal lengths (guess: %d gold: %d)%n.", guessLineId, goldLineId);
			}
			pwOut.Println("================================================================================");
			if (skippedGuessTrees != 0)
			{
				pwOut.Printf("%s %d guess trees%n", "Unable to evaluate", skippedGuessTrees);
			}
			metric.Display(true, pwOut);
			pwOut.Close();
		}
	}
}
