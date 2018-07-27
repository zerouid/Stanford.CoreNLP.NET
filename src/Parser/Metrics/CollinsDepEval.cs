using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>Compute P/R/F1 for the dependency representation of Collins (1999; 2003).</summary>
	/// <author>Spence Green</author>
	public class CollinsDepEval : AbstractEval
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Metrics.CollinsDepEval));

		private const bool Debug = false;

		private readonly IHeadFinder hf;

		private readonly string startSymbol;

		private readonly ICounter<CollinsRelation> precisions;

		private readonly ICounter<CollinsRelation> recalls;

		private readonly ICounter<CollinsRelation> f1s;

		private readonly ICounter<CollinsRelation> precisions2;

		private readonly ICounter<CollinsRelation> recalls2;

		private readonly ICounter<CollinsRelation> pnums2;

		private readonly ICounter<CollinsRelation> rnums2;

		public CollinsDepEval(string str, bool runningAverages, IHeadFinder hf, string startSymbol)
			: base(str, runningAverages)
		{
			this.hf = hf;
			this.startSymbol = startSymbol;
			precisions = new ClassicCounter<CollinsRelation>();
			recalls = new ClassicCounter<CollinsRelation>();
			f1s = new ClassicCounter<CollinsRelation>();
			precisions2 = new ClassicCounter<CollinsRelation>();
			recalls2 = new ClassicCounter<CollinsRelation>();
			pnums2 = new ClassicCounter<CollinsRelation>();
			rnums2 = new ClassicCounter<CollinsRelation>();
		}

		protected internal override ICollection<object> MakeObjects(Tree tree)
		{
			log.Info(this.GetType().FullName + ": Function makeObjects() not implemented");
			return null;
		}

		private IDictionary<CollinsRelation, ICollection<CollinsDependency>> MakeCollinsObjects(Tree t)
		{
			IDictionary<CollinsRelation, ICollection<CollinsDependency>> relMap = Generics.NewHashMap();
			ICollection<CollinsDependency> deps = CollinsDependency.ExtractNormalizedFromTree(t, startSymbol, hf);
			foreach (CollinsDependency dep in deps)
			{
				if (relMap[dep.GetRelation()] == null)
				{
					relMap[dep.GetRelation()] = Generics.NewHashSet<CollinsDependency>();
				}
				relMap[dep.GetRelation()].Add(dep);
			}
			return relMap;
		}

		public override void Evaluate(Tree guess, Tree gold, PrintWriter pw)
		{
			if (gold == null || guess == null)
			{
				System.Console.Error.Printf("%s: Cannot compare against a null gold or guess tree!\n", this.GetType().FullName);
				return;
			}
			IDictionary<CollinsRelation, ICollection<CollinsDependency>> guessDeps = MakeCollinsObjects(guess);
			IDictionary<CollinsRelation, ICollection<CollinsDependency>> goldDeps = MakeCollinsObjects(gold);
			ICollection<CollinsRelation> relations = Generics.NewHashSet();
			Sharpen.Collections.AddAll(relations, guessDeps.Keys);
			Sharpen.Collections.AddAll(relations, goldDeps.Keys);
			num += 1.0;
			foreach (CollinsRelation rel in relations)
			{
				ICollection<CollinsDependency> thisGuessDeps = guessDeps[rel];
				ICollection<CollinsDependency> thisGoldDeps = goldDeps[rel];
				if (thisGuessDeps == null)
				{
					thisGuessDeps = Generics.NewHashSet();
				}
				if (thisGoldDeps == null)
				{
					thisGoldDeps = Generics.NewHashSet();
				}
				double currentPrecision = Precision(thisGuessDeps, thisGoldDeps);
				double currentRecall = Precision(thisGoldDeps, thisGuessDeps);
				double currentF1 = (currentPrecision > 0.0 && currentRecall > 0.0 ? 2.0 / (1.0 / currentPrecision + 1.0 / currentRecall) : 0.0);
				precisions.IncrementCount(rel, currentPrecision);
				recalls.IncrementCount(rel, currentRecall);
				f1s.IncrementCount(rel, currentF1);
				precisions2.IncrementCount(rel, thisGuessDeps.Count * currentPrecision);
				pnums2.IncrementCount(rel, thisGuessDeps.Count);
				recalls2.IncrementCount(rel, thisGoldDeps.Count * currentRecall);
				rnums2.IncrementCount(rel, thisGoldDeps.Count);
				if (pw != null && runningAverages)
				{
					pw.Println(rel + "\tP: " + ((int)(currentPrecision * 10000)) / 100.0 + " (sent ave " + ((int)(precisions.GetCount(rel) * 10000 / num)) / 100.0 + ") (evalb " + ((int)(precisions2.GetCount(rel) * 10000 / pnums2.GetCount(rel))) / 100.0 + ")");
					pw.Println("\tR: " + ((int)(currentRecall * 10000)) / 100.0 + " (sent ave " + ((int)(recalls.GetCount(rel) * 10000 / num)) / 100.0 + ") (evalb " + ((int)(recalls2.GetCount(rel) * 10000 / rnums2.GetCount(rel))) / 100.0 + ")");
					double cF1 = 2.0 / (rnums2.GetCount(rel) / recalls2.GetCount(rel) + pnums2.GetCount(rel) / precisions2.GetCount(rel));
					string emit = str + " F1: " + ((int)(currentF1 * 10000)) / 100.0 + " (sent ave " + ((int)(10000 * f1s.GetCount(rel) / num)) / 100.0 + ", evalb " + ((int)(10000 * cF1)) / 100.0 + ")";
					pw.Println(emit);
				}
			}
			if (pw != null && runningAverages)
			{
				pw.Println("================================================================================");
			}
		}

		public override void Display(bool verbose, PrintWriter pw)
		{
			NumberFormat nf = new DecimalFormat("0.00");
			ICollection<CollinsRelation> cats = Generics.NewHashSet();
			Random rand = new Random();
			Sharpen.Collections.AddAll(cats, precisions.KeySet());
			Sharpen.Collections.AddAll(cats, recalls.KeySet());
			IDictionary<double, CollinsRelation> f1Map = new SortedDictionary<double, CollinsRelation>();
			foreach (CollinsRelation cat in cats)
			{
				double pnum2 = pnums2.GetCount(cat);
				double rnum2 = rnums2.GetCount(cat);
				double prec = precisions2.GetCount(cat) / pnum2;
				//(num > 0.0 ? precision/num : 0.0);
				double rec = recalls2.GetCount(cat) / rnum2;
				//(num > 0.0 ? recall/num : 0.0);
				double f1 = 2.0 / (1.0 / prec + 1.0 / rec);
				//(num > 0.0 ? f1/num : 0.0);
				if (f1.Equals(double.NaN))
				{
					f1 = -1.0;
				}
				if (f1Map.Contains(f1))
				{
					f1Map[f1 + (rand.NextDouble() / 1000.0)] = cat;
				}
				else
				{
					f1Map[f1] = cat;
				}
			}
			pw.Println(" Abstract Collins Dependencies -- final statistics");
			pw.Println("================================================================================");
			foreach (CollinsRelation cat_1 in f1Map.Values)
			{
				double pnum2 = pnums2.GetCount(cat_1);
				double rnum2 = rnums2.GetCount(cat_1);
				double prec = precisions2.GetCount(cat_1) / pnum2;
				//(num > 0.0 ? precision/num : 0.0);
				double rec = recalls2.GetCount(cat_1) / rnum2;
				//(num > 0.0 ? recall/num : 0.0);
				double f1 = 2.0 / (1.0 / prec + 1.0 / rec);
				//(num > 0.0 ? f1/num : 0.0);
				pw.Println(cat_1 + "\tLP: " + ((pnum2 == 0.0) ? " N/A" : nf.Format(prec)) + "\tguessed: " + (int)pnum2 + "\tLR: " + ((rnum2 == 0.0) ? " N/A" : nf.Format(rec)) + "\tgold:  " + (int)rnum2 + "\tF1: " + ((pnum2 == 0.0 || rnum2 == 0.0) ? " N/A" : 
					nf.Format(f1)));
			}
			pw.Println("================================================================================");
		}

		private const int MinArgs = 2;

		private static string Usage()
		{
			StringBuilder usage = new StringBuilder();
			string nl = Runtime.GetProperty("line.separator");
			usage.Append(string.Format("Usage: java %s [OPTS] goldFile guessFile%n%n", typeof(Edu.Stanford.Nlp.Parser.Metrics.CollinsDepEval).FullName));
			usage.Append("Options:").Append(nl);
			usage.Append("  -v        : Verbose output").Append(nl);
			usage.Append("  -l lang   : Language name " + Language.langList).Append(nl);
			usage.Append("  -y num    : Max yield of gold trees").Append(nl);
			usage.Append("  -g num    : Max yield of guess trees").Append(nl);
			return usage.ToString();
		}

		private static IDictionary<string, int> OptionArgDefs()
		{
			IDictionary<string, int> optionArgDefs = Generics.NewHashMap();
			optionArgDefs["v"] = 0;
			optionArgDefs["l"] = 1;
			optionArgDefs["g"] = 1;
			optionArgDefs["y"] = 1;
			return optionArgDefs;
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length < MinArgs)
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			Properties options = StringUtils.ArgsToProperties(args, OptionArgDefs());
			bool Verbose = PropertiesUtils.GetBool(options, "v", false);
			Language Language = PropertiesUtils.Get(options, "l", Language.English, typeof(Language));
			int MaxGoldYield = PropertiesUtils.GetInt(options, "g", int.MaxValue);
			int MaxGuessYield = PropertiesUtils.GetInt(options, "y", int.MaxValue);
			string[] parsedArgs = options.GetProperty(string.Empty, string.Empty).Split("\\s+");
			if (parsedArgs.Length != MinArgs)
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			File goldFile = new File(parsedArgs[0]);
			File guessFile = new File(parsedArgs[1]);
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
			Edu.Stanford.Nlp.Parser.Metrics.CollinsDepEval depEval = new Edu.Stanford.Nlp.Parser.Metrics.CollinsDepEval("CollinsDep", true, tlpp.HeadFinder(), tlpp.TreebankLanguagePack().StartSymbol());
			ITreeTransformer tc = tlpp.Collinizer();
			//PennTreeReader skips over null/malformed parses. So when the yields of the gold/guess trees
			//don't match, we need to keep looking for the next gold tree that matches.
			//The evalb ref implementation differs slightly as it expects one tree per line. It assigns
			//status as follows:
			//
			//   0 - Ok (yields match)
			//   1 - length mismatch
			//   2 - null parse e.g. (()).
			//
			//In the cases of 1,2, evalb does not include the tree pair in the LP/LR computation.
			IEnumerator<Tree> goldItr = goldTreebank.GetEnumerator();
			int goldLineId = 0;
			int skippedGuessTrees = 0;
			foreach (Tree guess in guessTreebank)
			{
				Tree evalGuess = tc.TransformTree(guess);
				if (guess.Yield().Count > MaxGuessYield)
				{
					skippedGuessTrees++;
					continue;
				}
				bool doneEval = false;
				while (goldItr.MoveNext() && !doneEval)
				{
					Tree gold = goldItr.Current;
					Tree evalGold = tc.TransformTree(gold);
					goldLineId++;
					if (gold.Yield().Count > MaxGoldYield)
					{
						continue;
					}
					else
					{
						if (evalGold.Yield().Count != evalGuess.Yield().Count)
						{
							pwOut.Println("Yield mismatch at gold line " + goldLineId);
							skippedGuessTrees++;
							break;
						}
					}
					//Default evalb behavior -- skip this guess tree
					depEval.Evaluate(evalGuess, evalGold, ((Verbose) ? pwOut : null));
					doneEval = true;
				}
			}
			//Move to the next guess parse
			pwOut.Println("================================================================================");
			if (skippedGuessTrees != 0)
			{
				pwOut.Printf("%s %d guess trees\n", ((MaxGuessYield < int.MaxValue) ? "Skipped" : "Unable to evaluate"), skippedGuessTrees);
			}
			depEval.Display(true, pwOut);
			pwOut.Close();
		}
	}
}
