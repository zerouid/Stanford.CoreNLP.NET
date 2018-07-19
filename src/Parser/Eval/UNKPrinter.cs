using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Eval
{
	/// <summary>Prints a frequency distribution of the unknown word signatures realized in a given treebank file.</summary>
	/// <author>Spence Green</author>
	public class UNKPrinter
	{
		private const int minArgs = 1;

		private static readonly StringBuilder usage = new StringBuilder();

		static UNKPrinter()
		{
			usage.Append(string.Format("Usage: java %s [OPTS] tree_file \n\n", typeof(UNKPrinter).FullName));
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
			Language lang = Language.English;
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-"))
				{
					switch (args[i])
					{
						case "-l":
						{
							lang = Language.ValueOf(args[++i].Trim());
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
			PrintWriter pw = tlpp.Pw();
			Options op = new Options();
			Options.LexOptions lexOptions = op.lexOptions;
			if (lang == Language.French)
			{
				lexOptions.useUnknownWordSignatures = 1;
				lexOptions.smartMutation = false;
				lexOptions.unknownSuffixSize = 2;
				lexOptions.unknownPrefixSize = 1;
			}
			else
			{
				if (lang == Language.Arabic)
				{
					lexOptions.smartMutation = false;
					lexOptions.useUnknownWordSignatures = 9;
					lexOptions.unknownPrefixSize = 1;
					lexOptions.unknownSuffixSize = 1;
				}
			}
			IIndex<string> wordIndex = new HashIndex<string>();
			IIndex<string> tagIndex = new HashIndex<string>();
			ILexicon lex = tlpp.Lex(op, wordIndex, tagIndex);
			int computeAfter = (int)(0.50 * tb.Count);
			ICounter<string> vocab = new ClassicCounter<string>();
			ICounter<string> unkCounter = new ClassicCounter<string>();
			int treeId = 0;
			foreach (Tree t in tb)
			{
				IList<ILabel> yield = t.Yield();
				int posId = 0;
				foreach (ILabel word in yield)
				{
					vocab.IncrementCount(word.Value());
					if (treeId > computeAfter && vocab.GetCount(word.Value()) < 2.0)
					{
						//          if(lex.getUnknownWordModel().getSignature(word.value(), posId++).equals("UNK"))
						//            pw.println(word.value());
						unkCounter.IncrementCount(lex.GetUnknownWordModel().GetSignature(word.Value(), posId++));
					}
				}
				treeId++;
			}
			IList<string> biggestKeys = new List<string>(unkCounter.KeySet());
			biggestKeys.Sort(Counters.ToComparatorDescending(unkCounter));
			foreach (string wordType in biggestKeys)
			{
				pw.Printf("%s\t%d%n", wordType, (int)unkCounter.GetCount(wordType));
			}
			pw.Close();
			pw.Close();
		}
	}
}
