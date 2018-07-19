using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>Computes POS tagging P/R/F1 from guess/gold trees.</summary>
	/// <remarks>
	/// Computes POS tagging P/R/F1 from guess/gold trees. This version assumes that the yields match. For
	/// trees with potentially different yields, use
	/// <see cref="TsarfatyEval"/>
	/// .
	/// <p>
	/// This implementation assumes that the guess/gold input files are of equal length, and have one tree per
	/// line.
	/// </remarks>
	/// <author>Spence Green</author>
	public class TaggingEval : AbstractEval
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Metrics.TaggingEval));

		private readonly ILexicon lex;

		private static bool doCatLevelEval = false;

		private ICounter<string> precisions;

		private ICounter<string> recalls;

		private ICounter<string> f1s;

		private ICounter<string> precisions2;

		private ICounter<string> recalls2;

		private ICounter<string> pnums2;

		private ICounter<string> rnums2;

		private ICounter<string> percentOOV;

		private ICounter<string> percentOOV2;

		public TaggingEval(string str)
			: this(str, true, null)
		{
		}

		public TaggingEval(string str, bool runningAverages, ILexicon lex)
			: base(str, runningAverages)
		{
			this.lex = lex;
			if (doCatLevelEval)
			{
				precisions = new ClassicCounter<string>();
				recalls = new ClassicCounter<string>();
				f1s = new ClassicCounter<string>();
				precisions2 = new ClassicCounter<string>();
				recalls2 = new ClassicCounter<string>();
				pnums2 = new ClassicCounter<string>();
				rnums2 = new ClassicCounter<string>();
				percentOOV = new ClassicCounter<string>();
				percentOOV2 = new ClassicCounter<string>();
			}
		}

		protected internal override ICollection<object> MakeObjects(Tree tree)
		{
			return (tree == null) ? Generics.NewHashSet<IHasTag>() : Generics.NewHashSet<IHasTag>(tree.TaggedLabeledYield());
		}

		private static IDictionary<string, ICollection<ILabel>> MakeObjectsByCat(Tree t)
		{
			IDictionary<string, ICollection<ILabel>> catMap = Generics.NewHashMap();
			IList<CoreLabel> tly = t.TaggedLabeledYield();
			foreach (CoreLabel label in tly)
			{
				if (catMap.Contains(label.Value()))
				{
					catMap[label.Value()].Add(label);
				}
				else
				{
					ICollection<ILabel> catSet = Generics.NewHashSet();
					catSet.Add(label);
					catMap[label.Value()] = catSet;
				}
			}
			return catMap;
		}

		public override void Evaluate(Tree guess, Tree gold, PrintWriter pw)
		{
			if (gold == null || guess == null)
			{
				System.Console.Error.Printf("%s: Cannot compare against a null gold or guess tree!\n", this.GetType().FullName);
				return;
			}
			//Do regular evaluation
			base.Evaluate(guess, gold, pw);
			if (doCatLevelEval)
			{
				IDictionary<string, ICollection<ILabel>> guessCats = MakeObjectsByCat(guess);
				IDictionary<string, ICollection<ILabel>> goldCats = MakeObjectsByCat(gold);
				ICollection<string> allCats = Generics.NewHashSet();
				Sharpen.Collections.AddAll(allCats, guessCats.Keys);
				Sharpen.Collections.AddAll(allCats, goldCats.Keys);
				foreach (string cat in allCats)
				{
					ICollection<ILabel> thisGuessCats = guessCats[cat];
					ICollection<ILabel> thisGoldCats = goldCats[cat];
					if (thisGuessCats == null)
					{
						thisGuessCats = Generics.NewHashSet();
					}
					if (thisGoldCats == null)
					{
						thisGoldCats = Generics.NewHashSet();
					}
					double currentPrecision = Precision(thisGuessCats, thisGoldCats);
					double currentRecall = Precision(thisGoldCats, thisGuessCats);
					double currentF1 = (currentPrecision > 0.0 && currentRecall > 0.0 ? 2.0 / (1.0 / currentPrecision + 1.0 / currentRecall) : 0.0);
					precisions.IncrementCount(cat, currentPrecision);
					recalls.IncrementCount(cat, currentRecall);
					f1s.IncrementCount(cat, currentF1);
					precisions2.IncrementCount(cat, thisGuessCats.Count * currentPrecision);
					pnums2.IncrementCount(cat, thisGuessCats.Count);
					recalls2.IncrementCount(cat, thisGoldCats.Count * currentRecall);
					rnums2.IncrementCount(cat, thisGoldCats.Count);
					if (lex != null)
					{
						MeasureOOV(guess, gold);
					}
					if (pw != null && runningAverages)
					{
						pw.Println(cat + "\tP: " + ((int)(currentPrecision * 10000)) / 100.0 + " (sent ave " + ((int)(precisions.GetCount(cat) * 10000 / num)) / 100.0 + ") (evalb " + ((int)(precisions2.GetCount(cat) * 10000 / pnums2.GetCount(cat))) / 100.0 + ")");
						pw.Println("\tR: " + ((int)(currentRecall * 10000)) / 100.0 + " (sent ave " + ((int)(recalls.GetCount(cat) * 10000 / num)) / 100.0 + ") (evalb " + ((int)(recalls2.GetCount(cat) * 10000 / rnums2.GetCount(cat))) / 100.0 + ")");
						double cF1 = 2.0 / (rnums2.GetCount(cat) / recalls2.GetCount(cat) + pnums2.GetCount(cat) / precisions2.GetCount(cat));
						string emit = str + " F1: " + ((int)(currentF1 * 10000)) / 100.0 + " (sent ave " + ((int)(10000 * f1s.GetCount(cat) / num)) / 100.0 + ", evalb " + ((int)(10000 * cF1)) / 100.0 + ")";
						pw.Println(emit);
					}
				}
				if (pw != null && runningAverages)
				{
					pw.Println("========================================");
				}
			}
		}

		/// <summary>Measures the percentage of incorrect taggings that can be attributed to OOV words.</summary>
		/// <param name="guess"/>
		/// <param name="gold"/>
		private void MeasureOOV(Tree guess, Tree gold)
		{
			IList<CoreLabel> goldTagging = gold.TaggedLabeledYield();
			IList<CoreLabel> guessTagging = guess.TaggedLabeledYield();
			System.Diagnostics.Debug.Assert(goldTagging.Count == guessTagging.Count);
			for (int i = 0; i < goldTagging.Count; i++)
			{
				if (!(goldTagging[i] == guessTagging[i]))
				{
					percentOOV2.IncrementCount(goldTagging[i].Tag());
					if (!lex.IsKnown(goldTagging[i].Word()))
					{
						percentOOV.IncrementCount(goldTagging[i].Tag());
					}
				}
			}
		}

		public override void Display(bool verbose, PrintWriter pw)
		{
			base.Display(verbose, pw);
			if (doCatLevelEval)
			{
				NumberFormat nf = new DecimalFormat("0.00");
				ICollection<string> cats = Generics.NewHashSet();
				Random rand = new Random();
				Sharpen.Collections.AddAll(cats, precisions.KeySet());
				Sharpen.Collections.AddAll(cats, recalls.KeySet());
				IDictionary<double, string> f1Map = new SortedDictionary<double, string>();
				foreach (string cat in cats)
				{
					double pnum2 = pnums2.GetCount(cat);
					double rnum2 = rnums2.GetCount(cat);
					double prec = precisions2.GetCount(cat) / pnum2;
					double rec = recalls2.GetCount(cat) / rnum2;
					double f1 = 2.0 / (1.0 / prec + 1.0 / rec);
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
				pw.Println("============================================================");
				pw.Println("Tagging Performance by Category -- final statistics");
				pw.Println("============================================================");
				foreach (string cat_1 in f1Map.Values)
				{
					double pnum2 = pnums2.GetCount(cat_1);
					double rnum2 = rnums2.GetCount(cat_1);
					double prec = precisions2.GetCount(cat_1) / pnum2;
					prec *= 100.0;
					double rec = recalls2.GetCount(cat_1) / rnum2;
					rec *= 100.0;
					double f1 = 2.0 / (1.0 / prec + 1.0 / rec);
					double oovRate = (lex == null) ? -1.0 : percentOOV.GetCount(cat_1) / percentOOV2.GetCount(cat_1);
					pw.Println(cat_1 + "\tLP: " + ((pnum2 == 0.0) ? " N/A" : nf.Format(prec)) + "\tguessed: " + (int)pnum2 + "\tLR: " + ((rnum2 == 0.0) ? " N/A" : nf.Format(rec)) + "\tgold:  " + (int)rnum2 + "\tF1: " + ((pnum2 == 0.0 || rnum2 == 0.0) ? " N/A" : 
						nf.Format(f1)) + "\tOOV: " + ((lex == null) ? " N/A" : nf.Format(oovRate)));
				}
				pw.Println("============================================================");
			}
		}

		private const int minArgs = 2;

		private static readonly StringBuilder usage = new StringBuilder();

		static TaggingEval()
		{
			usage.Append(string.Format("Usage: java %s [OPTS] gold guess\n\n", typeof(Edu.Stanford.Nlp.Parser.Metrics.TaggingEval).FullName));
			usage.Append("Options:\n");
			usage.Append("  -v         : Verbose mode.\n");
			usage.Append("  -l lang    : Select language settings from " + Language.langList + "\n");
			usage.Append("  -y num     : Skip gold trees with yields longer than num.\n");
			usage.Append("  -c         : Compute LP/LR/F1 by category.\n");
			usage.Append("  -e         : Input encoding.\n");
		}

		public static readonly IDictionary<string, int> optionArgDefs = Generics.NewHashMap();

		static TaggingEval()
		{
			optionArgDefs["-v"] = 0;
			optionArgDefs["-l"] = 1;
			optionArgDefs["-y"] = 1;
			optionArgDefs["-c"] = 0;
			optionArgDefs["-e"] = 0;
		}

		/// <summary>Run the scoring metric on guess/gold input.</summary>
		/// <remarks>
		/// Run the scoring metric on guess/gold input. This method performs "Collinization."
		/// The default language is English.
		/// </remarks>
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length < minArgs)
			{
				System.Console.Out.WriteLine(usage.ToString());
				System.Environment.Exit(-1);
			}
			ITreebankLangParserParams tlpp = new EnglishTreebankParserParams();
			int maxGoldYield = int.MaxValue;
			bool Verbose = false;
			string encoding = "UTF-8";
			string guessFile = null;
			string goldFile = null;
			IDictionary<string, string[]> argsMap = StringUtils.ArgsToMap(args, optionArgDefs);
			foreach (KeyValuePair<string, string[]> opt in argsMap)
			{
				if (opt.Key == null)
				{
					continue;
				}
				if (opt.Key.Equals("-l"))
				{
					Language lang = Language.ValueOf(opt.Value[0].Trim());
					tlpp = lang.@params;
				}
				else
				{
					if (opt.Key.Equals("-y"))
					{
						maxGoldYield = System.Convert.ToInt32(opt.Value[0].Trim());
					}
					else
					{
						if (opt.Key.Equals("-v"))
						{
							Verbose = true;
						}
						else
						{
							if (opt.Key.Equals("-c"))
							{
								Edu.Stanford.Nlp.Parser.Metrics.TaggingEval.doCatLevelEval = true;
							}
							else
							{
								if (opt.Key.Equals("-e"))
								{
									encoding = opt.Value[0];
								}
								else
								{
									log.Info(usage.ToString());
									System.Environment.Exit(-1);
								}
							}
						}
					}
				}
				//Non-option arguments located at key null
				string[] rest = argsMap[null];
				if (rest == null || rest.Length < minArgs)
				{
					log.Info(usage.ToString());
					System.Environment.Exit(-1);
				}
				goldFile = rest[0];
				guessFile = rest[1];
			}
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
			Edu.Stanford.Nlp.Parser.Metrics.TaggingEval metric = new Edu.Stanford.Nlp.Parser.Metrics.TaggingEval("Tagging LP/LR");
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
			pwOut.Close();
		}
	}
}
