using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Java.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Tools
{
	/// <summary>Create frequency distribution for RHS of grammar rules.</summary>
	/// <author>Spence Green</author>
	public class RHSFrequency
	{
		private const int minArgs = 2;

		private static readonly StringBuilder usage = new StringBuilder();

		static RHSFrequency()
		{
			usage.Append(string.Format("Usage: java %s [OPTS] lhs tree_file \n\n", typeof(RHSFrequency).FullName));
			usage.Append("Options:\n");
			usage.Append("  -l lang    : Select language settings from " + Language.langList + "\n");
			usage.Append("  -e enc     : Encoding.\n");
		}

		public static void Main(string[] args)
		{
			if (args.Length < minArgs)
			{
				System.Console.Out.WriteLine(usage.ToString());
				System.Environment.Exit(-1);
			}
			ITreebankLangParserParams tlpp = new EnglishTreebankParserParams();
			DiskTreebank tb = null;
			string encoding = "UTF-8";
			TregexPattern rootMatch = null;
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

						case "-e":
						{
							encoding = args[++i];
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
					rootMatch = TregexPattern.Compile("@" + args[i++]);
					if (tb == null)
					{
						if (tlpp == null)
						{
							System.Console.Out.WriteLine(usage.ToString());
							System.Environment.Exit(-1);
						}
						else
						{
							tlpp.SetInputEncoding(encoding);
							tlpp.SetOutputEncoding(encoding);
							tb = tlpp.DiskTreebank();
						}
					}
					tb.LoadPath(args[i++]);
				}
			}
			ICounter<string> rhsCounter = new ClassicCounter<string>();
			foreach (Tree t in tb)
			{
				TregexMatcher m = rootMatch.Matcher(t);
				while (m.FindNextMatchingNode())
				{
					Tree match = m.GetMatch();
					StringBuilder sb = new StringBuilder();
					foreach (Tree kid in match.Children())
					{
						sb.Append(kid.Value()).Append(" ");
					}
					rhsCounter.IncrementCount(sb.ToString().Trim());
				}
			}
			IList<string> biggestKeys = new List<string>(rhsCounter.KeySet());
			biggestKeys.Sort(Counters.ToComparatorDescending(rhsCounter));
			PrintWriter pw = tlpp.Pw();
			foreach (string rhs in biggestKeys)
			{
				pw.Printf("%s\t%d%n", rhs, (int)rhsCounter.GetCount(rhs));
			}
			pw.Close();
		}
	}
}
