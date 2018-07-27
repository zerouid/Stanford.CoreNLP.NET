using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Parser.Tools
{
	/// <summary>Counts the rule branching factor (and other rule statistics) in a treebank.</summary>
	/// <author>Spence Green</author>
	public class RuleBranchingFactor
	{
		private static string TreeToRuleString(Tree tree)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(tree.Value()).Append(":").Append(tree.FirstChild().Value());
			for (int i = 1; i < tree.NumChildren(); ++i)
			{
				Tree kid = tree.Children()[i];
				sb.Append("-").Append(kid.Value());
			}
			return sb.ToString();
		}

		private const int minArgs = 1;

		private static readonly string usage;

		static RuleBranchingFactor()
		{
			StringBuilder sb = new StringBuilder();
			string nl = Runtime.GetProperty("line.separator");
			sb.Append(string.Format("Usage: java %s [OPTS] tree_file%s%s", typeof(CountTrees).FullName, nl, nl));
			sb.Append("Options:\n");
			sb.Append("  -l lang    : Select language settings from " + Language.langList).Append(nl);
			sb.Append("  -e enc     : Encoding.").Append(nl);
			usage = sb.ToString();
		}

		public static readonly IDictionary<string, int> optionArgDefinitions = Generics.NewHashMap();

		static RuleBranchingFactor()
		{
			optionArgDefinitions["l"] = 1;
			optionArgDefinitions["e"] = 1;
		}

		public static void Main(string[] args)
		{
			if (args.Length < minArgs)
			{
				System.Console.Out.WriteLine(usage);
				System.Environment.Exit(-1);
			}
			// Process command-line options
			Properties options = StringUtils.ArgsToProperties(args, optionArgDefinitions);
			string fileName = options.GetProperty(string.Empty);
			if (fileName == null || fileName.Equals(string.Empty))
			{
				System.Console.Out.WriteLine(usage);
				System.Environment.Exit(-1);
			}
			Language language = PropertiesUtils.Get(options, "l", Language.English, typeof(Language));
			ITreebankLangParserParams tlpp = language.@params;
			string encoding = options.GetProperty("e", "UTF-8");
			tlpp.SetInputEncoding(encoding);
			tlpp.SetOutputEncoding(encoding);
			DiskTreebank tb = tlpp.DiskTreebank();
			tb.LoadPath(fileName);
			// Statistics
			ICounter<string> binaryRuleTypes = new ClassicCounter<string>(20000);
			IList<int> branchingFactors = new List<int>(20000);
			int nTrees = 0;
			int nUnaryRules = 0;
			int nBinaryRules = 0;
			int binaryBranchingFactors = 0;
			// Read the treebank
			PrintWriter pw = tlpp.Pw();
			foreach (Tree tree in tb)
			{
				if (tree.Value().Equals("ROOT"))
				{
					tree = tree.FirstChild();
				}
				++nTrees;
				foreach (Tree subTree in tree)
				{
					if (subTree.IsPhrasal())
					{
						if (subTree.NumChildren() > 1)
						{
							++nBinaryRules;
							branchingFactors.Add(subTree.NumChildren());
							binaryBranchingFactors += subTree.NumChildren();
							binaryRuleTypes.IncrementCount(TreeToRuleString(subTree));
						}
						else
						{
							++nUnaryRules;
						}
					}
				}
			}
			double mean = (double)binaryBranchingFactors / (double)nBinaryRules;
			System.Console.Out.Printf("#trees:\t%d%n", nTrees);
			System.Console.Out.Printf("#binary:\t%d%n", nBinaryRules);
			System.Console.Out.Printf("#binary types:\t%d%n", binaryRuleTypes.KeySet().Count);
			System.Console.Out.Printf("mean branching:\t%.4f%n", mean);
			System.Console.Out.Printf("stddev branching:\t%.4f%n", StandardDeviation(branchingFactors, mean));
			System.Console.Out.Printf("rule entropy:\t%.5f%n", Counters.Entropy(binaryRuleTypes));
			System.Console.Out.Printf("#unaries:\t%d%n", nUnaryRules);
		}

		private static double StandardDeviation(IList<int> branchingFactors, double mean)
		{
			double variance = 0.0;
			foreach (int i in branchingFactors)
			{
				variance += (i - mean) * (i - mean);
			}
			return Math.Sqrt(variance / (branchingFactors.Count - 1));
		}
	}
}
