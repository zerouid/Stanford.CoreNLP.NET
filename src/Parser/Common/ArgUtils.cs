using System;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Parser.Common
{
	/// <summary>
	/// Utility methods or common blocks of code for dealing with parser
	/// arguments, such as extracting Treebank information
	/// </summary>
	public class ArgUtils
	{
		private ArgUtils()
		{
		}

		// helper function
		public static int NumSubArgs(string[] args, int index)
		{
			int i = index;
			while (i + 1 < args.Length && args[i + 1][0] != '-')
			{
				i++;
			}
			return i - index;
		}

		public static void PrintArgs(string[] args, TextWriter ps)
		{
			ps.Write("Parser invoked with arguments:");
			foreach (string arg in args)
			{
				ps.Write(' ' + arg);
			}
			ps.WriteLine();
		}

		internal static readonly Pattern DoublePattern = Pattern.Compile("[-]?[0-9]+[.][0-9]+");

		public static Pair<string, IFileFilter> GetTreebankDescription(string[] args, int argIndex, string flag)
		{
			Triple<string, IFileFilter, double> description = GetWeightedTreebankDescription(args, argIndex, flag);
			return Pair.MakePair(description.First(), description.Second());
		}

		public static Triple<string, IFileFilter, double> GetWeightedTreebankDescription(string[] args, int argIndex, string flag)
		{
			string path = null;
			IFileFilter filter = null;
			double weight = 1.0;
			// the next arguments are the treebank path and maybe the range for testing
			int numSubArgs = NumSubArgs(args, argIndex);
			if (numSubArgs > 0 && numSubArgs < 4)
			{
				argIndex++;
				path = args[argIndex++];
				bool hasWeight = false;
				if (numSubArgs > 1 && DoublePattern.Matcher(args[argIndex + numSubArgs - 2]).Matches())
				{
					weight = double.Parse(args[argIndex + numSubArgs - 2]);
					hasWeight = true;
					numSubArgs--;
				}
				if (numSubArgs == 2)
				{
					filter = new NumberRangesFileFilter(args[argIndex++], true);
				}
				else
				{
					if (numSubArgs == 3)
					{
						try
						{
							int low = System.Convert.ToInt32(args[argIndex]);
							int high = System.Convert.ToInt32(args[argIndex + 1]);
							filter = new NumberRangeFileFilter(low, high, true);
							argIndex += 2;
						}
						catch (NumberFormatException)
						{
							// maybe it's a ranges expression?
							filter = new NumberRangesFileFilter(args[argIndex++], true);
						}
					}
				}
				if (hasWeight)
				{
					argIndex++;
				}
			}
			else
			{
				throw new ArgumentException("Bad arguments after " + flag);
			}
			return Triple.MakeTriple(path, filter, weight);
		}
	}
}
