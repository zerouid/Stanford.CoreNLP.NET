using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.French;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.International.French.Scripts
{
	/// <summary>Places predicted morphological analyses in the leaves of gold FTB parse trees.</summary>
	/// <author>Spence Green</author>
	public sealed class MungeTreesWithMorfetteAnalyses
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(MungeTreesWithMorfetteAnalyses));

		private class MorfetteFileIterator : IEnumerator<IList<CoreLabel>>
		{
			private BufferedReader reader;

			private IList<CoreLabel> nextList;

			private int lineId = 0;

			public MorfetteFileIterator(string filename)
			{
				try
				{
					reader = new BufferedReader(new InputStreamReader(new FileInputStream(filename), "UTF-8"));
					PrimeNext();
				}
				catch (UnsupportedEncodingException e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
				}
				catch (FileNotFoundException e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
				}
			}

			private void PrimeNext()
			{
				try
				{
					nextList = new List<CoreLabel>(40);
					for (string line; (line = reader.ReadLine()) != null; ++lineId)
					{
						line = line.Trim();
						if (line.Equals(string.Empty))
						{
							++lineId;
							break;
						}
						string[] toks = line.Split("\\s+");
						if (toks.Length != 3)
						{
							log.Info(toks.Length);
							log.Info(line);
							log.Info(lineId);
							throw new Exception(string.Format("line %d: Morfette format is |word lemma tag|: |%s|", lineId, line));
						}
						CoreLabel cl = new CoreLabel();
						string word = toks[0];
						string lemma = toks[1];
						string tag = toks[2];
						cl.SetWord(word);
						cl.SetValue(word);
						cl.SetLemma(lemma);
						cl.SetTag(tag);
						nextList.Add(cl);
					}
					// File is exhausted
					if (nextList.Count == 0)
					{
						reader.Close();
						nextList = null;
					}
				}
				catch (IOException e)
				{
					System.Console.Error.Printf("Problem reading file at line %d%n", lineId);
					Sharpen.Runtime.PrintStackTrace(e);
					nextList = null;
				}
			}

			public virtual bool MoveNext()
			{
				return nextList != null;
			}

			public virtual IList<CoreLabel> Current
			{
				get
				{
					if (MoveNext())
					{
						IList<CoreLabel> next = nextList;
						PrimeNext();
						return next;
					}
					return null;
				}
			}

			public virtual void Remove()
			{
				throw new NotSupportedException();
			}
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				System.Console.Error.Printf("Usage: java %s tree_file morfette_tnt_file%n", typeof(MungeTreesWithMorfetteAnalyses).FullName);
				System.Environment.Exit(-1);
			}
			string treeFile = args[0];
			string morfetteFile = args[1];
			ITreeReaderFactory trf = new FrenchTreeReaderFactory();
			try
			{
				ITreeReader tr = trf.NewTreeReader(new BufferedReader(new InputStreamReader(new FileInputStream(treeFile), "UTF-8")));
				IEnumerator<IList<CoreLabel>> morfetteItr = new MungeTreesWithMorfetteAnalyses.MorfetteFileIterator(morfetteFile);
				for (Tree tree; (tree = tr.ReadTree()) != null && morfetteItr.MoveNext(); )
				{
					IList<CoreLabel> analysis = morfetteItr.Current;
					IList<ILabel> yield = tree.Yield();
					System.Diagnostics.Debug.Assert(analysis.Count == yield.Count);
					int yieldLen = yield.Count;
					for (int i = 0; i < yieldLen; ++i)
					{
						CoreLabel tokenAnalysis = analysis[i];
						ILabel token = yield[i];
						string lemma = GetLemma(token.Value(), tokenAnalysis.Lemma());
						string newLeaf = string.Format("%s%s%s%s%s", token.Value(), MorphoFeatureSpecification.MorphoMark, lemma, MorphoFeatureSpecification.LemmaMark, tokenAnalysis.Tag());
						((CoreLabel)token).SetValue(newLeaf);
					}
					System.Console.Out.WriteLine(tree.ToString());
				}
				if (tr.ReadTree() != null || morfetteItr.MoveNext())
				{
					log.Info("WARNING: Uneven input files!");
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

		private static readonly Pattern pIsPunct = Pattern.Compile("\\p{Punct}+");

		private static readonly Pattern pAllUpper = Pattern.Compile("\\p{Upper}+");

		private static string GetLemma(string rawToken, string lemma)
		{
			bool isUpper = char.IsUpperCase(rawToken[0]);
			bool isAllUpper = pAllUpper.Matcher(rawToken).Matches();
			bool isParen = rawToken.Equals("-RRB-") || rawToken.Equals("-LRB-");
			bool isPunc = pIsPunct.Matcher(rawToken).Matches();
			if (isParen || isPunc || isAllUpper)
			{
				return rawToken;
			}
			if (isUpper)
			{
				char firstChar = char.ToUpperCase(lemma[0]);
				lemma = firstChar + Sharpen.Runtime.Substring(lemma, 1, lemma.Length);
			}
			return lemma;
		}
	}
}
