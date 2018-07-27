using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Parser.Tools
{
	/// <summary>Loads a LexicalizedParser and tries to get its tag list.</summary>
	/// <author>John Bauer</author>
	public class PrintTagList
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(PrintTagList));

		public static void Main(string[] args)
		{
			string parserFile = null;
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-model"))
				{
					parserFile = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					string error = "Unknown argument " + args[argIndex];
					log.Info(error);
					throw new Exception(error);
				}
			}
			if (parserFile == null)
			{
				log.Info("Must specify a model file with -model");
				System.Environment.Exit(2);
			}
			LexicalizedParser parser = ((LexicalizedParser)LexicalizedParser.LoadModel(parserFile));
			ICollection<string> tags = Generics.NewTreeSet();
			foreach (string tag in parser.tagIndex)
			{
				tags.Add(parser.TreebankLanguagePack().BasicCategory(tag));
			}
			System.Console.Out.WriteLine("Basic tags: " + tags.Count);
			foreach (string tag_1 in tags)
			{
				System.Console.Out.Write("  " + tag_1);
			}
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("All tags size: " + parser.tagIndex.Size());
			ICollection<string> states = Generics.NewTreeSet();
			foreach (string state in parser.stateIndex)
			{
				states.Add(parser.TreebankLanguagePack().BasicCategory(state));
			}
			System.Console.Out.WriteLine("Basic states: " + states.Count);
			foreach (string tag_2 in states)
			{
				System.Console.Out.Write("  " + tag_2);
			}
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("All states size: " + parser.stateIndex.Size());
			System.Console.Out.WriteLine("Unary grammar size: " + parser.ug.NumRules());
			System.Console.Out.WriteLine("Binary grammar size: " + parser.bg.NumRules());
		}
	}
}
