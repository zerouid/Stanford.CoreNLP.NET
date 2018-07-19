using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Tools
{
	/// <summary>
	/// A simple tool to change flags embedded
	/// in a LexicalizedParser model.
	/// </summary>
	/// <remarks>
	/// A simple tool to change flags embedded
	/// in a LexicalizedParser model.
	/// <br />
	/// Expected arguments: <br />
	/// <code> -input model </code> <br />
	/// <code> -output model </code> <br />
	/// <code> [list of arguments to set] </code> <br />
	/// </remarks>
	/// <author>John Bauer</author>
	public class UpdateParserOptions
	{
		public static void Main(string[] args)
		{
			string input = null;
			string output = null;
			IList<string> extraArgs = Generics.NewArrayList();
			for (int argIndex = 0; argIndex < args.Length; )
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-input"))
				{
					input = args[argIndex + 1];
					argIndex += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-output"))
					{
						output = args[argIndex + 1];
						argIndex += 2;
					}
					else
					{
						extraArgs.Add(args[argIndex++]);
					}
				}
			}
			LexicalizedParser parser = LexicalizedParser.LoadModel(input, extraArgs);
			parser.SaveParserToSerialized(output);
		}
	}
}
