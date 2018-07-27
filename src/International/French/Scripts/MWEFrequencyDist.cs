using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.French;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.International.French.Scripts
{
	/// <summary>Prints a frequency distribution of MWEs in French.</summary>
	/// <author>Spence Green</author>
	public sealed class MWEFrequencyDist
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.French.Scripts.MWEFrequencyDist));

		private MWEFrequencyDist()
		{
		}

		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.Error.Printf("Usage: java %s file%n", typeof(Edu.Stanford.Nlp.International.French.Scripts.MWEFrequencyDist).FullName);
				System.Environment.Exit(-1);
			}
			File treeFile = new File(args[0]);
			TwoDimensionalCounter<string, string> mweLabelToString = new TwoDimensionalCounter<string, string>();
			ICollection<string> uniquePOSSequences = Generics.NewHashSet();
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(treeFile), "UTF-8"));
				ITreeReaderFactory trf = new FrenchTreeReaderFactory();
				ITreeReader tr = trf.NewTreeReader(br);
				TregexPattern pMWE = TregexPattern.Compile("/^MW/");
				for (Tree t; (t = tr.ReadTree()) != null; )
				{
					//Count MWE statistics
					TregexMatcher m = pMWE.Matcher(t);
					while (m.FindNextMatchingNode())
					{
						Tree match = m.GetMatch();
						string label = match.Value();
						IList<CoreLabel> yield = match.TaggedLabeledYield();
						StringBuilder termYield = new StringBuilder();
						StringBuilder posYield = new StringBuilder();
						foreach (CoreLabel cl in yield)
						{
							termYield.Append(cl.Word()).Append(" ");
							posYield.Append(cl.Tag()).Append(" ");
						}
						mweLabelToString.IncrementCount(label, termYield.ToString().Trim());
						uniquePOSSequences.Add(posYield.ToString().Trim());
					}
				}
				tr.Close();
				//Closes the underlying reader
				System.Console.Out.Printf("Type\t#Type\t#Single\t%%Single\t%%Total%n");
				double nMWEs = mweLabelToString.TotalCount();
				int nAllSingletons = 0;
				int nTokens = 0;
				foreach (string mweLabel in mweLabelToString.FirstKeySet())
				{
					int nSingletons = 0;
					double totalCount = mweLabelToString.TotalCount(mweLabel);
					ICounter<string> mc = mweLabelToString.GetCounter(mweLabel);
					foreach (string term in mc.KeySet())
					{
						if (mc.GetCount(term) == 1.0)
						{
							nSingletons++;
						}
						nTokens += term.Split("\\s+").Length * (int)mc.GetCount(term);
					}
					nAllSingletons += nSingletons;
					System.Console.Out.Printf("%s\t%d\t%d\t%.2f\t%.2f%n", mweLabel, (int)totalCount, nSingletons, 100.0 * nSingletons / totalCount, 100.0 * totalCount / nMWEs);
				}
				System.Console.Out.Printf("TOTAL:\t%d\t%d\t%.2f%n", (int)nMWEs, nAllSingletons, 100.0 * nAllSingletons / nMWEs);
				System.Console.Out.WriteLine("#tokens = " + nTokens);
				System.Console.Out.WriteLine("#unique MWE POS sequences = " + uniquePOSSequences.Count);
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (TregexParseException e)
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
