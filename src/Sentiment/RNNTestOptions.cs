using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Sentiment
{
	/// <summary>Evaluation-only options for the RNN models</summary>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class RNNTestOptions
	{
		public int ngramRecordSize = 0;

		public int ngramRecordMaximumLength = 0;

		public bool printLengthAccuracies = false;

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			result.Append("TEST OPTIONS\n");
			result.Append("ngramRecordSize=" + ngramRecordSize + "\n");
			result.Append("ngramRecordMaximumLength=" + ngramRecordMaximumLength + "\n");
			result.Append("printLengthAccuracies=" + printLengthAccuracies + "\n");
			return result.ToString();
		}

		public virtual int SetOption(string[] args, int argIndex)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-ngramRecordSize"))
			{
				ngramRecordSize = System.Convert.ToInt32(args[argIndex + 1]);
				return argIndex + 2;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-ngramRecordMaximumLength"))
				{
					ngramRecordMaximumLength = System.Convert.ToInt32(args[argIndex + 1]);
					return argIndex + 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-printLengthAccuracies"))
					{
						printLengthAccuracies = true;
						return argIndex + 1;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-noprintLengthAccuracies"))
						{
							printLengthAccuracies = false;
							return argIndex + 1;
						}
					}
				}
			}
			return argIndex;
		}
	}
}
