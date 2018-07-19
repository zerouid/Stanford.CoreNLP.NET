using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>Character level segmentation and tagging metric from (Tsarfaty, 2006).</summary>
	/// <remarks>
	/// Character level segmentation and tagging metric from (Tsarfaty, 2006). For evaluating parse
	/// trees at the character level, use
	/// <see cref="Evalb"/>
	/// with the charLevel flag set to true.
	/// NOTE: If segmentation markers (e.g. "+") appear in the input, then they should be stripped
	/// prior to running this metric.
	/// </remarks>
	/// <author>Spence Green</author>
	public class TsarfatyEval : AbstractEval
	{
		private readonly bool useTag;

		private readonly IConstituentFactory cf = new LabeledScoredConstituentFactory();

		public TsarfatyEval(string str, bool tags)
			: base(str, false)
		{
			useTag = tags;
		}

		protected internal override ICollection<object> MakeObjects(Tree tree)
		{
			ICollection<Constituent> deps = Generics.NewHashSet();
			if (tree != null)
			{
				ExtractDeps(tree, 0, deps);
			}
			return deps;
		}

		private int ExtractDeps(Tree t, int left, ICollection<Constituent> deps)
		{
			int position = left;
			// Segmentation constituents
			if (!useTag && t.IsLeaf())
			{
				position += t.Label().Value().Length;
				deps.Add(cf.NewConstituent(left, position - 1, t.Label(), 0.0));
			}
			else
			{
				// POS tag constituents
				if (useTag && t.IsPreTerminal())
				{
					position += t.FirstChild().Label().Value().Length;
					deps.Add(cf.NewConstituent(left, position - 1, t.Label(), 0.0));
				}
				else
				{
					Tree[] kids = t.Children();
					foreach (Tree kid in kids)
					{
						position = ExtractDeps(kid, position, deps);
					}
				}
			}
			return position;
		}

		private const int minArgs = 2;

		private static readonly StringBuilder usage = new StringBuilder();

		static TsarfatyEval()
		{
			usage.Append(string.Format("Usage: java %s [OPTS] gold guess\n\n", typeof(Edu.Stanford.Nlp.Parser.Metrics.TsarfatyEval).FullName));
			usage.Append("Options:\n");
			usage.Append("  -v         : Verbose mode.\n");
			usage.Append("  -l lang    : Select language settings from " + typeof(Language).FullName + "\n");
			usage.Append("  -y num     : Skip gold trees with yields longer than num.\n");
			usage.Append("  -g num     : Skip guess trees with yields longer than num.\n");
			usage.Append("  -t         : Tagging mode (default: segmentation).\n");
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
			int maxGuessYield = int.MaxValue;
			bool Verbose = false;
			bool skipGuess = false;
			bool tagMode = false;
			string guessFile = null;
			string goldFile = null;
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i])
					{
						case "-l":
						{
							Language lang = Language.ValueOf(args[++i].Trim());
							tlpp = lang.@params;
							break;
						}

						case "-y":
						{
							maxGoldYield = System.Convert.ToInt32(args[++i].Trim());
							break;
						}

						case "-t":
						{
							tagMode = true;
							break;
						}

						case "-v":
						{
							Verbose = true;
							break;
						}

						case "-g":
						{
							maxGuessYield = System.Convert.ToInt32(args[++i].Trim());
							skipGuess = true;
							break;
						}

						default:
						{
							System.Console.Out.WriteLine(usage.ToString());
							System.Environment.Exit(-1);
							break;
						}
					}
				}
				else
				{
					//Required parameters
					goldFile = args[i++];
					guessFile = args[i];
					break;
				}
			}
			PrintWriter pwOut = tlpp.Pw();
			Treebank guessTreebank = tlpp.DiskTreebank();
			guessTreebank.LoadPath(guessFile);
			pwOut.Println("GUESS TREEBANK:");
			pwOut.Println(guessTreebank.TextualSummary());
			Treebank goldTreebank = tlpp.DiskTreebank();
			goldTreebank.LoadPath(goldFile);
			pwOut.Println("GOLD TREEBANK:");
			pwOut.Println(goldTreebank.TextualSummary());
			string evalName = (tagMode) ? "TsarfatyTAG" : "TsarfatySEG";
			Edu.Stanford.Nlp.Parser.Metrics.TsarfatyEval eval = new Edu.Stanford.Nlp.Parser.Metrics.TsarfatyEval(evalName, tagMode);
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
				List<ILabel> guessSent = guess.Yield();
				string guessChars = SentenceUtils.ListToString(guessSent).ReplaceAll("\\s+", string.Empty);
				if (guessSent.Count > maxGuessYield)
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
					List<ILabel> goldSent = gold.Yield();
					string goldChars = SentenceUtils.ListToString(goldSent).ReplaceAll("\\s+", string.Empty);
					if (goldSent.Count > maxGoldYield)
					{
						continue;
					}
					else
					{
						if (goldChars.Length != guessChars.Length)
						{
							pwOut.Printf("Char level yield mismatch at line %d (guess: %d gold: %d)\n", goldLineId, guessChars.Length, goldChars.Length);
							skippedGuessTrees++;
							break;
						}
					}
					//Default evalb behavior -- skip this guess tree
					eval.Evaluate(evalGuess, evalGold, ((Verbose) ? pwOut : null));
					doneEval = true;
				}
			}
			//Move to the next guess parse
			pwOut.Println("================================================================================");
			if (skippedGuessTrees != 0)
			{
				pwOut.Printf("%s %d guess trees\n", ((skipGuess) ? "Skipped" : "Unable to evaluate"), skippedGuessTrees);
			}
			eval.Display(true, pwOut);
			pwOut.Println();
			pwOut.Close();
		}
	}
}
