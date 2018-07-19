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

namespace Edu.Stanford.Nlp.Parser.Tools
{
	/// <summary>Counts the number of trees in a (PTB format) treebank file.</summary>
	/// <remarks>
	/// Counts the number of trees in a (PTB format) treebank file. Also provides
	/// flags for printing (after processing by the various language packs)
	/// and flattening the trees.
	/// </remarks>
	/// <author>Spence Green</author>
	public class CountTrees
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(CountTrees));

		private const int minArgs = 1;

		private static readonly string usage;

		static CountTrees()
		{
			StringBuilder sb = new StringBuilder();
			string nl = Runtime.GetProperty("line.separator");
			sb.Append(string.Format("Usage: java %s [OPTS] tree_file%s%s", typeof(CountTrees).FullName, nl, nl));
			sb.Append("Options:\n");
			sb.Append("  -l lang    : Select language settings from " + Language.langList).Append(nl);
			sb.Append("  -e enc     : Encoding.").Append(nl);
			sb.Append("  -y len     : Only print trees with yields <= len.").Append(nl);
			sb.Append("  -a         : Only print the pre-terminal yields, one per line.").Append(nl);
			sb.Append("  -p         : Print the trees to stdout.").Append(nl);
			sb.Append("  -f         : Flatten the trees and print to stdout.").Append(nl);
			sb.Append("  -t         : Print TnT style output.").Append(nl);
			usage = sb.ToString();
		}

		public static readonly IDictionary<string, int> optionArgDefinitions = Generics.NewHashMap();

		static CountTrees()
		{
			optionArgDefinitions["l"] = 1;
			optionArgDefinitions["e"] = 1;
			optionArgDefinitions["y"] = 1;
			optionArgDefinitions["a"] = 0;
			optionArgDefinitions["p"] = 0;
			optionArgDefinitions["f"] = 0;
			optionArgDefinitions["t"] = 0;
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
			int maxLen = PropertiesUtils.GetInt(options, "y", int.MaxValue);
			bool printTrees = PropertiesUtils.GetBool(options, "p", false);
			bool flattenTrees = PropertiesUtils.GetBool(options, "f", false);
			bool printPOS = PropertiesUtils.GetBool(options, "a", false);
			bool printTnT = PropertiesUtils.GetBool(options, "t", false);
			Language language = PropertiesUtils.Get(options, "l", Language.English, typeof(Language));
			ITreebankLangParserParams tlpp = language.@params;
			string encoding = options.GetProperty("e", "UTF-8");
			tlpp.SetInputEncoding(encoding);
			tlpp.SetOutputEncoding(encoding);
			DiskTreebank tb = tlpp.DiskTreebank();
			tb.LoadPath(fileName);
			// Read the treebank
			PrintWriter pw = tlpp.Pw();
			int numTrees = 0;
			foreach (Tree tree in tb)
			{
				if (tree.Yield().Count > maxLen)
				{
					continue;
				}
				++numTrees;
				if (printTrees)
				{
					pw.Println(tree.ToString());
				}
				else
				{
					if (flattenTrees)
					{
						pw.Println(SentenceUtils.ListToString(tree.Yield()));
					}
					else
					{
						if (printPOS)
						{
							pw.Println(SentenceUtils.ListToString(tree.PreTerminalYield()));
						}
						else
						{
							if (printTnT)
							{
								IList<CoreLabel> yield = tree.TaggedLabeledYield();
								foreach (CoreLabel label in yield)
								{
									pw.Printf("%s\t%s%n", label.Word(), label.Tag());
								}
								pw.Println();
							}
						}
					}
				}
			}
			System.Console.Error.Printf("Read %d trees.%n", numTrees);
		}
	}
}
