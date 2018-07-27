using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;




namespace Edu.Stanford.Nlp.Parser.Tools
{
	/// <summary>Prints a frequency distribution from a collection of trees.</summary>
	/// <author>Spence Green</author>
	public class VocabFrequency
	{
		private const int minArgs = 1;

		private static readonly StringBuilder usage = new StringBuilder();

		static VocabFrequency()
		{
			usage.Append(string.Format("Usage: java %s [OPTS] tree_file \n\n", typeof(VocabFrequency).FullName));
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
			ICounter<string> vocab = new ClassicCounter<string>();
			foreach (Tree t in tb)
			{
				IList<ILabel> yield = t.Yield();
				foreach (ILabel word in yield)
				{
					vocab.IncrementCount(word.Value());
				}
			}
			IList<string> biggestKeys = new List<string>(vocab.KeySet());
			biggestKeys.Sort(Counters.ToComparatorDescending(vocab));
			PrintWriter pw = tlpp.Pw();
			foreach (string wordType in biggestKeys)
			{
				pw.Printf("%s\t%d%n", wordType, (int)vocab.GetCount(wordType));
			}
			pw.Close();
		}
	}
}
