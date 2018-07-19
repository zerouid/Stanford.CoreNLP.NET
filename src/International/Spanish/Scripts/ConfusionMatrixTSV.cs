using System.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Spanish.Scripts
{
	public class ConfusionMatrixTSV
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ConfusionMatrixTSV));

		public static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				System.Console.Error.Printf("Usage: java %s answers_file%n", typeof(ConfusionMatrix).FullName);
				System.Environment.Exit(-1);
			}
			try
			{
				ConfusionMatrix<string> cm = new ConfusionMatrix<string>();
				string answersFile = args[0];
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(answersFile), "UTF-8"));
				string line = br.ReadLine();
				for (; line != null; line = br.ReadLine())
				{
					string[] tokens = line.Split("\\s");
					if (tokens.Length != 3)
					{
						System.Console.Error.Printf("ignoring bad line");
						continue;
					}
					//System.exit(-1);
					cm.Add(tokens[2], tokens[1]);
				}
				System.Console.Out.WriteLine(cm.ToString());
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
