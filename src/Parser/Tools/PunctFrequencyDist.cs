using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Tools
{
	/// <summary>Prints out a frequency distribution of all punctuation tags in a treebank.</summary>
	/// <author>Spence Green</author>
	public sealed class PunctFrequencyDist
	{
		private const int minArgs = 2;

		private static readonly StringBuilder usage = new StringBuilder();

		static PunctFrequencyDist()
		{
			usage.Append(string.Format("Usage: java %s [OPTS] punct_tag tree_file \n\n", typeof(PunctFrequencyDist).FullName));
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
			string puncTag = null;
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
					puncTag = args[i++];
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
					tb.LoadPath(args[i]);
				}
			}
			ICounter<string> puncTypes = new ClassicCounter<string>();
			foreach (Tree t in tb)
			{
				IList<CoreLabel> yield = t.TaggedLabeledYield();
				foreach (CoreLabel word in yield)
				{
					if (word.Tag().Equals(puncTag))
					{
						puncTypes.IncrementCount(word.Word());
					}
				}
			}
			IList<string> biggestKeys = new List<string>(puncTypes.KeySet());
			biggestKeys.Sort(Counters.ToComparatorDescending(puncTypes));
			PrintWriter pw = tlpp.Pw();
			foreach (string wordType in biggestKeys)
			{
				pw.Printf("%s\t%d%n", wordType, (int)puncTypes.GetCount(wordType));
			}
			pw.Close();
		}
	}
}
