using System.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Hebrew
{
	/// <summary>Makes Tsarfaty's canonical split of HTBv2 (see her PhD thesis).</summary>
	/// <remarks>
	/// Makes Tsarfaty's canonical split of HTBv2 (see her PhD thesis). This is also
	/// the split that appears in Yoav Goldberg's work.
	/// </remarks>
	/// <author>Spence Green</author>
	public class SplitMaker
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(SplitMaker));

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.Error.Printf("Usage: java %s tree_file%n", typeof(SplitMaker).FullName);
				System.Environment.Exit(-1);
			}
			ITreebankLanguagePack tlp = new HebrewTreebankLanguagePack();
			string inputFile = args[0];
			File treeFile = new File(inputFile);
			try
			{
				ITreeReaderFactory trf = new HebrewTreeReaderFactory();
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(treeFile), tlp.GetEncoding()));
				ITreeReader tr = trf.NewTreeReader(br);
				PrintWriter pwDev = new PrintWriter(new TextWriter(new FileOutputStream(inputFile + ".clean.dev"), false, tlp.GetEncoding()));
				PrintWriter pwTrain = new PrintWriter(new TextWriter(new FileOutputStream(inputFile + ".clean.train"), false, tlp.GetEncoding()));
				PrintWriter pwTest = new PrintWriter(new TextWriter(new FileOutputStream(inputFile + ".clean.test"), false, tlp.GetEncoding()));
				int numTrees = 0;
				for (Tree t; ((t = tr.ReadTree()) != null); numTrees++)
				{
					if (numTrees < 483)
					{
						pwDev.Println(t.ToString());
					}
					else
					{
						if (numTrees >= 483 && numTrees < 5724)
						{
							pwTrain.Println(t.ToString());
						}
						else
						{
							pwTest.Println(t.ToString());
						}
					}
				}
				tr.Close();
				pwDev.Close();
				pwTrain.Close();
				pwTest.Close();
				System.Console.Error.Printf("Processed %d trees.%n", numTrees);
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
