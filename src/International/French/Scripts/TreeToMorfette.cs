using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.French;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.International.French.Scripts
{
	/// <summary>Writes out an FTB tree file in s-notation to Morfette format.</summary>
	/// <author>Spence Green</author>
	public class TreeToMorfette
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(TreeToMorfette));

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.Error.Printf("Usage: java %s tree_file%n", typeof(TreeToMorfette).FullName);
				System.Environment.Exit(-1);
			}
			string treeFile = args[0];
			ITreeReaderFactory trf = new FrenchTreeReaderFactory();
			try
			{
				ITreeReader tr = trf.NewTreeReader(new BufferedReader(new InputStreamReader(new FileInputStream(treeFile), "UTF-8")));
				for (Tree tree1; (tree1 = tr.ReadTree()) != null; )
				{
					IList<ILabel> pretermYield = tree1.PreTerminalYield();
					IList<ILabel> yield = tree1.Yield();
					int yieldLen = yield.Count;
					for (int i = 0; i < yieldLen; ++i)
					{
						CoreLabel rawToken = (CoreLabel)yield[i];
						string word = rawToken.Value();
						string morphStr = rawToken.OriginalText();
						Pair<string, string> lemmaMorph = MorphoFeatureSpecification.SplitMorphString(word, morphStr);
						string lemma = lemmaMorph.First();
						string morph = lemmaMorph.Second();
						if (morph == null || morph.Equals(string.Empty) || morph.Equals("XXX"))
						{
							morph = ((CoreLabel)pretermYield[i]).Value();
						}
						System.Console.Out.Printf("%s %s %s%n", word, lemma, morph);
					}
					System.Console.Out.WriteLine();
				}
				tr.Close();
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
