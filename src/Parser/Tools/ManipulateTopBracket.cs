using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Parser.Tools
{
	/// <summary>Reads in a set of treebank files and either adds (default) or removes a top bracket.</summary>
	/// <author>Spence Green</author>
	public class ManipulateTopBracket
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ManipulateTopBracket));

		private const int minArgs = 1;

		private static string Usage()
		{
			StringBuilder usage = new StringBuilder();
			string nl = Runtime.GetProperty("line.separator");
			usage.Append(string.Format("Usage: java %s [OPTS] file(s) > bracketed_trees%n%n", typeof(ManipulateTopBracket).FullName));
			usage.Append("Options:").Append(nl);
			usage.Append("  -v         : Verbose mode.").Append(nl);
			usage.Append("  -r         : Remove top bracket.").Append(nl);
			usage.Append("  -l lang    : Select language settings from " + Language.langList).Append(nl);
			usage.Append("  -e enc     : Encoding.").Append(nl);
			return usage.ToString();
		}

		private static IDictionary<string, int> ArgDefs()
		{
			IDictionary<string, int> argDefs = Generics.NewHashMap();
			argDefs["e"] = 1;
			argDefs["v"] = 0;
			argDefs["l"] = 1;
			argDefs["r"] = 0;
			return argDefs;
		}

		public static void Main(string[] args)
		{
			if (args.Length < minArgs)
			{
				System.Console.Out.WriteLine(Usage());
				System.Environment.Exit(-1);
			}
			Properties options = StringUtils.ArgsToProperties(args, ArgDefs());
			Language language = PropertiesUtils.Get(options, "l", Language.English, typeof(Language));
			ITreebankLangParserParams tlpp = language.@params;
			DiskTreebank tb = null;
			string encoding = options.GetProperty("l", "UTF-8");
			bool removeBracket = PropertiesUtils.GetBool(options, "b", false);
			tlpp.SetInputEncoding(encoding);
			tlpp.SetOutputEncoding(encoding);
			tb = tlpp.DiskTreebank();
			string[] files = options.GetProperty(string.Empty, string.Empty).Split("\\s+");
			if (files.Length != 0)
			{
				foreach (string filename in files)
				{
					tb.LoadPath(filename);
				}
			}
			else
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			PrintWriter pwo = tlpp.Pw();
			string startSymbol = tlpp.TreebankLanguagePack().StartSymbol();
			ITreeFactory tf = new LabeledScoredTreeFactory();
			int nTrees = 0;
			foreach (Tree t in tb)
			{
				if (removeBracket)
				{
					if (t.Value().Equals(startSymbol))
					{
						t = t.FirstChild();
					}
				}
				else
				{
					if (!t.Value().Equals(startSymbol))
					{
						//Add a bracket if it isn't already there
						t = tf.NewTreeNode(startSymbol, Java.Util.Collections.SingletonList(t));
					}
				}
				pwo.Println(t.ToString());
				nTrees++;
			}
			pwo.Close();
			System.Console.Error.Printf("Processed %d trees.%n", nTrees);
		}
	}
}
