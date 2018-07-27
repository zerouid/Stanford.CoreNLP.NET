using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.French;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.International.French.Pipeline
{
	/// <summary>Various modifications to the MWEs in the treebank.</summary>
	/// <author>Spence Green</author>
	public sealed class MWEPreprocessor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.French.Pipeline.MWEPreprocessor));

		private const bool ResolveDummyTags = true;

		private static int nMissingPOS = 0;

		private static int nMissingPhrasal = 0;

		private MWEPreprocessor()
		{
		}

		private class ManualUWModel
		{
			private static readonly ICollection<string> nouns = Generics.NewHashSet();

			private const string nStr = "A. Alezais alfa Annick Appliances Ardenne Artois baptiste Bargue Bellanger Bregenz clefs Coeurs ...conomie consumer " + "contrôleur Coopérative Coppée cuisson dédoublement demandeuse défraie Domestic dépistage Elektra Elettrodomestici "
				 + "Essonnes Fair Finparcom Gelisim gorge Happy Indesit Italia jockey Lawrence leone Levi machinisme Mc.Donnel MD Merloni " + "Meydan ménagers Muenchener Parcel Prost R. sam Sara Siège silos SPA Stateman Valley Vanity VF Vidal Vives Yorker Young Zemment";

			private static readonly ICollection<string> adjectives = Generics.NewHashSet();

			private const string aStr = "astral bis bovin gracieux intégrante italiano sanguin sèche";

			private static readonly ICollection<string> preps = Generics.NewHashSet();

			private const string pStr = "c o t";

			private static int nUnknownWordTypes;

			static ManualUWModel()
			{
				//UW words extracted from June2010 revision of FTB
				//TODO wsg2011: défraie is a verb
				Sharpen.Collections.AddAll(nouns, Arrays.AsList(nStr.Split("\\s+")));
				Sharpen.Collections.AddAll(adjectives, Arrays.AsList(aStr.Split("\\s+")));
				Sharpen.Collections.AddAll(preps, Arrays.AsList(pStr.Split("\\s+")));
				nUnknownWordTypes = nouns.Count + adjectives.Count + preps.Count;
			}

			private static readonly Pattern digit = Pattern.Compile("\\d+");

			public static string GetTag(string word)
			{
				if (digit.Matcher(word).Find())
				{
					return "N";
				}
				else
				{
					//This isn't right, but its close enough....
					if (nouns.Contains(word))
					{
						return "N";
					}
					else
					{
						if (adjectives.Contains(word))
						{
							return "A";
						}
						else
						{
							if (preps.Contains(word))
							{
								return "P";
							}
						}
					}
				}
				log.Info("No POS tag for " + word);
				return "N";
			}
		}

		public static void PrintCounter(TwoDimensionalCounter<string, string> cnt, string fname)
		{
			try
			{
				PrintWriter pw = new PrintWriter(new TextWriter(new FileOutputStream(new File(fname)), false, "UTF-8"));
				foreach (string key in cnt.FirstKeySet())
				{
					foreach (string val in cnt.GetCounter(key).KeySet())
					{
						pw.Printf("%s\t%s\t%d%n", key, val, (int)cnt.GetCount(key, val));
					}
				}
				pw.Close();
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

		public static void UpdateTagger(TwoDimensionalCounter<string, string> tagger, Tree t)
		{
			IList<CoreLabel> yield = t.TaggedLabeledYield();
			foreach (CoreLabel cl in yield)
			{
				if (ResolveDummyTags && cl.Tag().Equals(FrenchXMLTreeReader.MissingPos))
				{
					continue;
				}
				else
				{
					tagger.IncrementCount(cl.Word(), cl.Tag());
				}
			}
		}

		public static void TraverseAndFix(Tree t, TwoDimensionalCounter<string, string> pretermLabel, TwoDimensionalCounter<string, string> unigramTagger)
		{
			if (t.IsPreTerminal())
			{
				if (t.Value().Equals(FrenchXMLTreeReader.MissingPos))
				{
					nMissingPOS++;
					string word = t.FirstChild().Value();
					string tag = (unigramTagger.FirstKeySet().Contains(word)) ? Counters.Argmax(unigramTagger.GetCounter(word)) : MWEPreprocessor.ManualUWModel.GetTag(word);
					t.SetValue(tag);
				}
				return;
			}
			foreach (Tree kid in t.Children())
			{
				TraverseAndFix(kid, pretermLabel, unigramTagger);
			}
			//Post-order visit
			if (t.Value().Equals(FrenchXMLTreeReader.MissingPhrasal))
			{
				nMissingPhrasal++;
				StringBuilder sb = new StringBuilder();
				foreach (Tree kid_1 in t.Children())
				{
					sb.Append(kid_1.Value()).Append(" ");
				}
				string posSequence = sb.ToString().Trim();
				if (pretermLabel.FirstKeySet().Contains(posSequence))
				{
					string phrasalCat = Counters.Argmax(pretermLabel.GetCounter(posSequence));
					t.SetValue(phrasalCat);
				}
				else
				{
					System.Console.Out.WriteLine("No phrasal cat for: " + posSequence);
				}
			}
		}

		private static void ResolveDummyTags(File treeFile, TwoDimensionalCounter<string, string> pretermLabel, TwoDimensionalCounter<string, string> unigramTagger)
		{
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(treeFile), "UTF-8"));
				ITreeReaderFactory trf = new FrenchTreeReaderFactory();
				ITreeReader tr = trf.NewTreeReader(br);
				PrintWriter pw = new PrintWriter(new TextWriter(new FileOutputStream(new File(treeFile + ".fixed")), false, "UTF-8"));
				int nTrees = 0;
				for (Tree t; (t = tr.ReadTree()) != null; nTrees++)
				{
					TraverseAndFix(t, pretermLabel, unigramTagger);
					pw.Println(t.ToString());
				}
				pw.Close();
				tr.Close();
				System.Console.Out.WriteLine("Processed " + nTrees + " trees");
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

		internal static readonly TregexPattern pMWE = TregexPattern.Compile("/^MW/");

		public static void CountMWEStatistics(Tree t, TwoDimensionalCounter<string, string> unigramTagger, TwoDimensionalCounter<string, string> labelPreterm, TwoDimensionalCounter<string, string> pretermLabel, TwoDimensionalCounter<string, string> 
			labelTerm, TwoDimensionalCounter<string, string> termLabel)
		{
			UpdateTagger(unigramTagger, t);
			//Count MWE statistics
			TregexMatcher m = pMWE.Matcher(t);
			while (m.FindNextMatchingNode())
			{
				Tree match = m.GetMatch();
				string label = match.Value();
				if (ResolveDummyTags && label.Equals(FrenchXMLTreeReader.MissingPhrasal))
				{
					continue;
				}
				string preterm = SentenceUtils.ListToString(match.PreTerminalYield());
				string term = SentenceUtils.ListToString(match.Yield());
				labelPreterm.IncrementCount(label, preterm);
				pretermLabel.IncrementCount(preterm, label);
				labelTerm.IncrementCount(label, term);
				termLabel.IncrementCount(term, label);
			}
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.Error.Printf("Usage: java %s file%n", typeof(MWEPreprocessor).FullName);
				System.Environment.Exit(-1);
			}
			File treeFile = new File(args[0]);
			TwoDimensionalCounter<string, string> labelTerm = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> termLabel = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> labelPreterm = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> pretermLabel = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> unigramTagger = new TwoDimensionalCounter<string, string>();
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(treeFile), "UTF-8"));
				ITreeReaderFactory trf = new FrenchTreeReaderFactory();
				ITreeReader tr = trf.NewTreeReader(br);
				for (Tree t; (t = tr.ReadTree()) != null; )
				{
					CountMWEStatistics(t, unigramTagger, labelPreterm, pretermLabel, labelTerm, termLabel);
				}
				tr.Close();
				//Closes the underlying reader
				System.Console.Out.WriteLine("Generating {MWE Type -> Terminal}");
				PrintCounter(labelTerm, "label_term.csv");
				System.Console.Out.WriteLine("Generating {Terminal -> MWE Type}");
				PrintCounter(termLabel, "term_label.csv");
				System.Console.Out.WriteLine("Generating {MWE Type -> POS sequence}");
				PrintCounter(labelPreterm, "label_pos.csv");
				System.Console.Out.WriteLine("Generating {POS sequence -> MWE Type}");
				PrintCounter(pretermLabel, "pos_label.csv");
				System.Console.Out.WriteLine("Resolving DUMMY tags");
				ResolveDummyTags(treeFile, pretermLabel, unigramTagger);
				System.Console.Out.WriteLine("#Unknown Word Types: " + MWEPreprocessor.ManualUWModel.nUnknownWordTypes);
				System.Console.Out.WriteLine("#Missing POS: " + nMissingPOS);
				System.Console.Out.WriteLine("#Missing Phrasal: " + nMissingPhrasal);
				System.Console.Out.WriteLine("Done!");
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
