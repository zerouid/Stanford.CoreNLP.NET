using System.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Hebrew
{
	/// <author>Spence Green</author>
	[System.Serializable]
	public class HebrewTreeReaderFactory : ITreeReaderFactory
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(HebrewTreeReaderFactory));

		private const long serialVersionUID = 818065349424602548L;

		public virtual ITreeReader NewTreeReader(Reader @in)
		{
			return new PennTreeReader(@in, new LabeledScoredTreeFactory(), new HebrewTreeNormalizer(), new PennTreebankTokenizer(@in));
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.Error.Printf("Usage: java %s tree_file > trees%n", typeof(HebrewTreeReaderFactory).FullName);
				System.Environment.Exit(-1);
			}
			ITreebankLanguagePack tlp = new HebrewTreebankLanguagePack();
			File treeFile = new File(args[0]);
			try
			{
				ITreeReaderFactory trf = new HebrewTreeReaderFactory();
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(treeFile), tlp.GetEncoding()));
				ITreeReader tr = trf.NewTreeReader(br);
				int numTrees = 0;
				for (Tree t; ((t = tr.ReadTree()) != null); numTrees++)
				{
					System.Console.Out.WriteLine(t.ToString());
				}
				tr.Close();
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
