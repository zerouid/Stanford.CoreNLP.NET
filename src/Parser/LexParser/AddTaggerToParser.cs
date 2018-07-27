using System;
using Edu.Stanford.Nlp.Tagger.Maxent;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>A simple tool to add a tagger to the parser for reranking purposes.</summary>
	/// <author>John Bauer</author>
	public class AddTaggerToParser
	{
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			string taggerFile = null;
			string inputFile = null;
			string outputFile = null;
			double weight = 1.0;
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-tagger"))
				{
					taggerFile = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-input"))
					{
						inputFile = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-output"))
						{
							outputFile = args[argIndex + 1];
							argIndex += 2;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-weight"))
							{
								weight = double.ValueOf(args[argIndex + 1]);
								argIndex += 2;
							}
							else
							{
								throw new ArgumentException("Unknown argument: " + args[argIndex]);
							}
						}
					}
				}
			}
			LexicalizedParser parser = ((LexicalizedParser)LexicalizedParser.LoadModel(inputFile));
			MaxentTagger tagger = new MaxentTagger(taggerFile);
			parser.reranker = new TaggerReranker(tagger, parser.GetOp());
			parser.SaveParserToSerialized(outputFile);
		}
	}
}
