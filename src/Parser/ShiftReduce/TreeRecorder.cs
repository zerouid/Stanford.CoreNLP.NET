using System;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>Outputs either binarized or debinarized trees to a given filename.</summary>
	/// <remarks>
	/// Outputs either binarized or debinarized trees to a given filename.
	/// Useful for seeing the intermediate results of the ShiftReduceParser
	/// </remarks>
	/// <author>John Bauer</author>
	public class TreeRecorder : IParserQueryEval
	{
		public enum Mode
		{
			Binarized,
			Debinarized
		}

		private readonly TreeRecorder.Mode mode;

		private readonly BufferedWriter @out;

		public TreeRecorder(TreeRecorder.Mode mode, string filename)
		{
			this.mode = mode;
			try
			{
				@out = new BufferedWriter(new FileWriter(filename));
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public virtual void Evaluate(IParserQuery query, Tree gold, PrintWriter pw)
		{
			if (!(query is ShiftReduceParserQuery))
			{
				throw new ArgumentException("This evaluator only works for the ShiftReduceParser");
			}
			ShiftReduceParserQuery srquery = (ShiftReduceParserQuery)query;
			try
			{
				switch (mode)
				{
					case TreeRecorder.Mode.Binarized:
					{
						@out.Write(srquery.GetBestBinarizedParse().ToString());
						break;
					}

					case TreeRecorder.Mode.Debinarized:
					{
						@out.Write(srquery.debinarized.ToString());
						break;
					}

					default:
					{
						throw new ArgumentException("Unknown mode " + mode);
					}
				}
				@out.NewLine();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public virtual void Display(bool verbose, PrintWriter pw)
		{
			try
			{
				@out.Close();
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}
	}
}
