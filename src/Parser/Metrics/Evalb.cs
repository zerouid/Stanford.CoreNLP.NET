using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>A Java re-implementation of the evalb bracket scoring metric (Collins, 1997) that accepts Unicode input.</summary>
	/// <remarks>
	/// A Java re-implementation of the evalb bracket scoring metric (Collins, 1997) that accepts Unicode input.
	/// "Collinization" should be performed on input trees prior to invoking the package programmatically.
	/// "Collinization" refers to normalization of trees for things not counted in evaluation,
	/// such as equivalencing PRT and ADVP, which has standardly been done in English evaluation.
	/// A main method is provided that performs Collinization according to language specific settings.
	/// <p>
	/// This implementation assumes that the guess/gold input files are of equal length, and have one tree per
	/// line.
	/// <p>
	/// This implementation was last validated against EVALB20080701 (http://nlp.cs.nyu.edu/evalb/)
	/// by Spence Green on 22 Jan. 2010.  Notwithstanding this, Sekine and collins' EVALB script has been
	/// the common standard for constituency evaluation of parsers for the last decade.  We always validate
	/// any numbers we report with it, and we suggest that you do the same.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Spence Green</author>
	public class Evalb : AbstractEval
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Metrics.Evalb));

		private readonly IConstituentFactory cf;

		public Evalb(string str, bool runningAverages)
			: base(str, runningAverages)
		{
			cf = new LabeledScoredConstituentFactory();
		}

		/// <summary>
		/// evalb only evaluates phrasal categories, thus constituents() does not
		/// return objects for terminals and pre-terminals.
		/// </summary>
		protected internal override ICollection<object> MakeObjects(Tree tree)
		{
			ICollection<Constituent> set = Generics.NewHashSet();
			if (tree != null)
			{
				Sharpen.Collections.AddAll(set, tree.Constituents(cf));
			}
			return set;
		}

		public override void Evaluate(Tree guess, Tree gold, PrintWriter pw)
		{
			if (gold == null || guess == null)
			{
				System.Console.Error.Printf("%s: Cannot compare against a null gold or guess tree!\n", this.GetType().FullName);
				return;
			}
			else
			{
				if (guess.Yield().Count != gold.Yield().Count)
				{
					log.Info("Warning: yield differs:");
					log.Info("Guess: " + SentenceUtils.ListToString(guess.Yield()));
					log.Info("Gold:  " + SentenceUtils.ListToString(gold.Yield()));
				}
			}
			base.Evaluate(guess, gold, pw);
		}

		public class CBEval : Evalb
		{
			private double cb = 0.0;

			private double num = 0.0;

			private double zeroCB = 0.0;

			protected internal virtual void CheckCrossing(ICollection<Constituent> s1, ICollection<Constituent> s2)
			{
				double c = 0.0;
				foreach (Constituent constit in s1)
				{
					if (constit.Crosses(s2))
					{
						c += 1.0;
					}
				}
				if (c == 0.0)
				{
					zeroCB += 1.0;
				}
				cb += c;
				num += 1.0;
			}

			public override void Evaluate(Tree t1, Tree t2, PrintWriter pw)
			{
				ICollection<Constituent> b1 = ((ICollection<Constituent>)MakeObjects(t1));
				ICollection<Constituent> b2 = ((ICollection<Constituent>)MakeObjects(t2));
				CheckCrossing(b1, b2);
				if (pw != null && runningAverages)
				{
					pw.Println("AvgCB: " + ((int)(10000.0 * cb / num)) / 100.0 + " ZeroCB: " + ((int)(10000.0 * zeroCB / num)) / 100.0 + " N: " + GetNum());
				}
			}

			public override void Display(bool verbose, PrintWriter pw)
			{
				pw.Println(str + " AvgCB: " + ((int)(10000.0 * cb / num)) / 100.0 + " ZeroCB: " + ((int)(10000.0 * zeroCB / num)) / 100.0);
			}

			public CBEval(string str, bool runningAverages)
				: base(str, runningAverages)
			{
			}
		}

		private const int minArgs = 2;

		private static string Usage()
		{
			StringBuilder sb = new StringBuilder();
			string nl = Runtime.GetProperty("line.separator");
			sb.Append(string.Format("Usage: java %s [OPTS] gold guess%n%n", typeof(Evalb).FullName));
			sb.Append("Options:").Append(nl);
			sb.Append("  -v         : Verbose mode.").Append(nl);
			sb.Append("  -l lang    : Select language settings from ").Append(Language.langList).Append(nl);
			sb.Append("  -y num     : Skip gold trees with yields longer than num.").Append(nl);
			sb.Append("  -s num     : Sort the trees by F1 and output the num lowest F1 trees.").Append(nl);
			sb.Append("  -c         : Compute LP/LR/F1 by category.").Append(nl);
			sb.Append("  -f regex   : Compute category level evaluation for categories that match this regex.").Append(nl);
			sb.Append("  -e         : Input encoding.").Append(nl);
			return sb.ToString();
		}

		private static IDictionary<string, int> OptionArgDefs()
		{
			IDictionary<string, int> optionArgDefs = Generics.NewHashMap();
			optionArgDefs["v"] = 0;
			optionArgDefs["l"] = 1;
			optionArgDefs["y"] = 1;
			optionArgDefs["s"] = 1;
			optionArgDefs["c"] = 0;
			optionArgDefs["e"] = 0;
			optionArgDefs["f"] = 1;
			return optionArgDefs;
		}

		/// <summary>Run the Evalb scoring metric on guess/gold input.</summary>
		/// <remarks>Run the Evalb scoring metric on guess/gold input. The default language is English.</remarks>
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length < minArgs)
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			Properties options = StringUtils.ArgsToProperties(args, OptionArgDefs());
			Language language = PropertiesUtils.Get(options, "l", Language.English, typeof(Language));
			ITreebankLangParserParams tlpp = language.@params;
			int maxGoldYield = PropertiesUtils.GetInt(options, "y", int.MaxValue);
			bool Verbose = PropertiesUtils.GetBool(options, "v", false);
			bool sortByF1 = PropertiesUtils.HasProperty(options, "s");
			int worstKTreesToEmit = PropertiesUtils.GetInt(options, "s", 0);
			PriorityQueue<Triple<double, Tree, Tree>> queue = sortByF1 ? new PriorityQueue<Triple<double, Tree, Tree>>(2000, new Evalb.F1Comparator()) : null;
			bool doCatLevel = PropertiesUtils.GetBool(options, "c", false);
			string labelRegex = options.GetProperty("f", null);
			string encoding = options.GetProperty("e", "UTF-8");
			string[] parsedArgs = options.GetProperty(string.Empty, string.Empty).Split("\\s+");
			if (parsedArgs.Length != minArgs)
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			string goldFile = parsedArgs[0];
			string guessFile = parsedArgs[1];
			// Command-line has been parsed. Configure the metric for evaluation.
			tlpp.SetInputEncoding(encoding);
			PrintWriter pwOut = tlpp.Pw();
			Treebank guessTreebank = tlpp.DiskTreebank();
			guessTreebank.LoadPath(guessFile);
			pwOut.Println("GUESS TREEBANK:");
			pwOut.Println(guessTreebank.TextualSummary());
			Treebank goldTreebank = tlpp.DiskTreebank();
			goldTreebank.LoadPath(goldFile);
			pwOut.Println("GOLD TREEBANK:");
			pwOut.Println(goldTreebank.TextualSummary());
			Evalb metric = new Evalb("Evalb LP/LR", true);
			EvalbByCat evalbCat = (doCatLevel) ? new EvalbByCat("EvalbByCat LP/LR", true, labelRegex) : null;
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
				if (goldYield.Count > maxGoldYield)
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
				if (doCatLevel)
				{
					evalbCat.Evaluate(evalGuess, evalGold, ((Verbose) ? pwOut : null));
				}
				if (sortByF1)
				{
					StoreTrees(queue, guessTree, goldTree, metric.GetLastF1());
				}
			}
			if (guessItr.MoveNext() || goldItr.MoveNext())
			{
				System.Console.Error.Printf("Guess/gold files do not have equal lengths (guess: %d gold: %d)%n.", guessLineId, goldLineId);
			}
			pwOut.Println("================================================================================");
			if (skippedGuessTrees != 0)
			{
				pwOut.Printf("%s %d guess trees\n", "Unable to evaluate", skippedGuessTrees);
			}
			metric.Display(true, pwOut);
			pwOut.Println();
			if (doCatLevel)
			{
				evalbCat.Display(true, pwOut);
				pwOut.Println();
			}
			if (sortByF1)
			{
				EmitSortedTrees(queue, worstKTreesToEmit, guessFile);
			}
			pwOut.Close();
		}

		private static void EmitSortedTrees(PriorityQueue<Triple<double, Tree, Tree>> queue, int worstKTreesToEmit, string filePrefix)
		{
			if (queue == null)
			{
				log.Info("Queue was not initialized properly");
			}
			try
			{
				PrintWriter guessPw = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(filePrefix + ".kworst.guess"), "UTF-8")));
				PrintWriter goldPw = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(filePrefix + ".kworst.gold"), "UTF-8")));
				IConstituentFactory cFact = new LabeledScoredConstituentFactory();
				PrintWriter guessDepPw = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(filePrefix + ".kworst.guess.deps"), "UTF-8")));
				PrintWriter goldDepPw = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(filePrefix + ".kworst.gold.deps"), "UTF-8")));
				System.Console.Out.Printf("F1s of %d worst trees:\n", worstKTreesToEmit);
				for (int i = 0; queue.Peek() != null && i < worstKTreesToEmit; i++)
				{
					Triple<double, Tree, Tree> trees = queue.Poll();
					System.Console.Out.WriteLine(trees.First());
					//Output the trees
					goldPw.Println(trees.Second().ToString());
					guessPw.Println(trees.Third().ToString());
					//Output the set differences
					ICollection<Constituent> goldDeps = Generics.NewHashSet();
					Sharpen.Collections.AddAll(goldDeps, trees.Second().Constituents(cFact));
					goldDeps.RemoveAll(trees.Third().Constituents(cFact));
					foreach (Constituent c in goldDeps)
					{
						goldDepPw.Print(c.ToString() + "  ");
					}
					goldDepPw.Println();
					ICollection<Constituent> guessDeps = Generics.NewHashSet();
					Sharpen.Collections.AddAll(guessDeps, trees.Third().Constituents(cFact));
					guessDeps.RemoveAll(trees.Second().Constituents(cFact));
					foreach (Constituent c_1 in guessDeps)
					{
						guessDepPw.Print(c_1.ToString() + "  ");
					}
					guessDepPw.Println();
				}
				guessPw.Close();
				goldPw.Close();
				goldDepPw.Close();
				guessDepPw.Close();
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		private static void StoreTrees(PriorityQueue<Triple<double, Tree, Tree>> queue, Tree guess, Tree gold, double curF1)
		{
			if (queue == null)
			{
				return;
			}
			queue.Add(new Triple<double, Tree, Tree>(curF1, gold, guess));
		}

		private class F1Comparator : IComparator<Triple<double, Tree, Tree>>
		{
			public virtual int Compare(Triple<double, Tree, Tree> o1, Triple<double, Tree, Tree> o2)
			{
				double firstF1 = o1.First();
				double secondF1 = o2.First();
				if (firstF1 < secondF1)
				{
					return -1;
				}
				else
				{
					if (firstF1 == secondF1)
					{
						return 0;
					}
				}
				return 1;
			}
		}
	}
}
