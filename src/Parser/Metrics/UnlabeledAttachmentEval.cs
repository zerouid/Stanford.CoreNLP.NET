using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>Dependency unlabeled attachment score.</summary>
	/// <remarks>
	/// Dependency unlabeled attachment score.
	/// <p>
	/// If Collinization has not been performed prior to evaluation, then
	/// it is customary (for reporting results) to pass in a filter that rejects
	/// dependencies with punctuation dependents.
	/// </remarks>
	/// <author>Spence Green</author>
	public class UnlabeledAttachmentEval : AbstractEval
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Metrics.UnlabeledAttachmentEval));

		private readonly IHeadFinder headFinder;

		private readonly IPredicate<string> punctRejectWordFilter;

		private readonly IPredicate<IDependency<ILabel, ILabel, object>> punctRejectFilter;

		/// <param name="headFinder">
		/// If a headFinder is provided, then head percolation will be done
		/// for trees. Otherwise, it must be called separately.
		/// </param>
		public UnlabeledAttachmentEval(string str, bool runningAverages, IHeadFinder headFinder)
			: this(str, runningAverages, headFinder, Filters.AcceptFilter<string>())
		{
		}

		public UnlabeledAttachmentEval(string str, bool runningAverages, IHeadFinder headFinder, IPredicate<string> punctRejectFilter)
			: base(str, runningAverages)
		{
			this.headFinder = headFinder;
			this.punctRejectWordFilter = punctRejectFilter;
			this.punctRejectFilter = new _IPredicate_58(this);
		}

		private sealed class _IPredicate_58 : IPredicate<IDependency<ILabel, ILabel, object>>
		{
			private const long serialVersionUID = 649358302237611081L;
			public _IPredicate_58(UnlabeledAttachmentEval _enclosing)
			{
				this._enclosing = _enclosing;
				this.serialVersionUID = serialVersionUID;
			}

			// Semantics of this method are weird. If accept() returns true, then the dependent is
			// *not* a punctuation item. This filter thus accepts everything except punctuation
			// dependencies.
			public bool Test(IDependency<ILabel, ILabel, object> dep)
			{
				string depString = dep.Dependent().Value();
				return this._enclosing.punctRejectWordFilter.Test(depString);
			}

			private readonly UnlabeledAttachmentEval _enclosing;
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

		/// <summary>Build the set of dependencies for evaluation.</summary>
		/// <remarks>
		/// Build the set of dependencies for evaluation.  This set excludes
		/// all dependencies for which the argument is a punctuation tag.
		/// </remarks>
		protected internal override ICollection<object> MakeObjects(Tree tree)
		{
			if (tree == null)
			{
				log.Info("Warning: null tree");
				return Generics.NewHashSet();
			}
			if (headFinder != null)
			{
				tree.PercolateHeads(headFinder);
			}
			ICollection<IDependency<ILabel, ILabel, object>> deps = tree.Dependencies(punctRejectFilter);
			return deps;
		}

		private const int minArgs = 2;

		private static readonly StringBuilder usage = new StringBuilder();

		static UnlabeledAttachmentEval()
		{
			usage.Append(string.Format("Usage: java %s [OPTS] gold guess\n\n", typeof(Edu.Stanford.Nlp.Parser.Metrics.UnlabeledAttachmentEval).FullName));
			usage.Append("Options:\n");
			usage.Append("  -v         : Verbose mode.\n");
			usage.Append("  -l lang    : Select language settings from ").Append(Language.langList).Append('\n');
			usage.Append("  -y num     : Skip gold trees with yields longer than num.\n");
			usage.Append("  -e         : Input encoding.\n");
		}

		public static readonly IDictionary<string, int> optionArgDefs = Generics.NewHashMap();

		static UnlabeledAttachmentEval()
		{
			optionArgDefs["-v"] = 0;
			optionArgDefs["-l"] = 1;
			optionArgDefs["-y"] = 1;
			optionArgDefs["-e"] = 0;
		}

		/// <summary>Run the Evalb scoring metric on guess/gold input.</summary>
		/// <remarks>Run the Evalb scoring metric on guess/gold input. The default language is English.</remarks>
		/// <param name="args"/>
		public static void Main(string[] args)
		{
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
			Edu.Stanford.Nlp.Parser.Metrics.UnlabeledAttachmentEval metric = new Edu.Stanford.Nlp.Parser.Metrics.UnlabeledAttachmentEval("UAS LP/LR", true, tlpp.HeadFinder());
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
				evalGuess.IndexLeaves(true);
				Tree evalGold = tc.TransformTree(goldTree);
				evalGold.IndexLeaves(true);
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
