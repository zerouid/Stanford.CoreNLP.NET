using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International.French;
using Edu.Stanford.Nlp.International.French.Pipeline;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.French;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.International.French.Scripts
{
	/// <summary>Performs the pre-processing of raw (XML) FTB trees for the EMNLP2011 and CL2011 experiments.</summary>
	/// <author>John Bauer</author>
	/// <author>Spence Green</author>
	public sealed class SplitCanditoTrees
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.French.Scripts.SplitCanditoTrees));

		/// <summary>
		/// true -- mwetoolkit experiments, factored lexicon experiments
		/// false -- basic parsing experiments
		/// </summary>
		private const bool LemmasAsLeaves = false;

		/// <summary>
		/// true -- factored lexicon experiments
		/// false -- mwetoolkit experiments, basic parsing experiments
		/// </summary>
		private const bool AddMorphoToLeaves = false;

		/// <summary>
		/// true -- Use the CC tagset
		/// false -- Use the default tagset
		/// </summary>
		private const bool CcTagset = true;

		/// <summary>Output Morfette training files instead of PTB-style trees</summary>
		private const bool MorfetteOutput = false;

		private static int nTokens = 0;

		private static int nMorphAnalyses = 0;

		private static readonly int[] fSizes = new int[] { 1235, 1235, 9881, 10000000 };

		private static readonly string[] fNames = new string[] { "candito.test", "candito.dev", "candito.train", "candito.train.extended" };

		private SplitCanditoTrees()
		{
		}

		// Statistics
		// static main method only
		/// <exception cref="System.IO.IOException"/>
		internal static IList<string> ReadIds(string filename)
		{
			IList<string> ids = new List<string>();
			BufferedReader fin = new BufferedReader(new InputStreamReader(new FileInputStream(filename), "ISO8859_1"));
			string line;
			while ((line = fin.ReadLine()) != null)
			{
				string[] pieces = line.Split("\t");
				ids.Add(pieces[0].Trim());
			}
			return ids;
		}

		/// <exception cref="System.IO.IOException"/>
		internal static IDictionary<string, Tree> ReadTrees(string[] filenames)
		{
			// TODO: perhaps we can just pass in CC_TAGSET and get rid of replacePOSTags
			// need to test that
			ITreeReaderFactory trf = new FrenchXMLTreeReaderFactory(false);
			IDictionary<string, Tree> treeMap = Generics.NewHashMap();
			foreach (string filename in filenames)
			{
				File file = new File(filename);
				string canonicalFilename = Sharpen.Runtime.Substring(file.GetName(), 0, file.GetName().LastIndexOf('.'));
				FrenchXMLTreeReader tr = (FrenchXMLTreeReader)trf.NewTreeReader(new BufferedReader(new InputStreamReader(new FileInputStream(file), "ISO8859_1")));
				Tree t = null;
				int numTrees;
				for (numTrees = 0; (t = tr.ReadTree()) != null; numTrees++)
				{
					string id = canonicalFilename + "-" + ((CoreLabel)t.Label()).Get(typeof(CoreAnnotations.SentenceIDAnnotation));
					treeMap[id] = t;
				}
				tr.Close();
				System.Console.Error.Printf("%s: %d trees%n", file.GetName(), numTrees);
			}
			return treeMap;
		}

		internal static void PreprocessMWEs(IDictionary<string, Tree> treeMap)
		{
			TwoDimensionalCounter<string, string> labelTerm = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> termLabel = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> labelPreterm = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> pretermLabel = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> unigramTagger = new TwoDimensionalCounter<string, string>();
			foreach (Tree t in treeMap.Values)
			{
				MWEPreprocessor.CountMWEStatistics(t, unigramTagger, labelPreterm, pretermLabel, labelTerm, termLabel);
			}
			foreach (Tree t_1 in treeMap.Values)
			{
				MWEPreprocessor.TraverseAndFix(t_1, pretermLabel, unigramTagger);
			}
		}

		public static void MungeLeaves(Tree tree, bool lemmasAsLeaves, bool addMorphoToLeaves)
		{
			IList<ILabel> labels = tree.Yield();
			foreach (ILabel label in labels)
			{
				++nTokens;
				if (!(label is CoreLabel))
				{
					throw new ArgumentException("Only works with CoreLabels trees");
				}
				CoreLabel coreLabel = (CoreLabel)label;
				string lemma = coreLabel.Lemma();
				//PTB escaping since we're going to put this in the leaf
				if (lemma == null)
				{
					// No lemma, so just add the surface form
					lemma = coreLabel.Word();
				}
				else
				{
					if (lemma.Equals("("))
					{
						lemma = "-LRB-";
					}
					else
					{
						if (lemma.Equals(")"))
						{
							lemma = "-RRB-";
						}
					}
				}
				if (lemmasAsLeaves)
				{
					string escapedLemma = lemma;
					coreLabel.SetWord(escapedLemma);
					coreLabel.SetValue(escapedLemma);
					coreLabel.SetLemma(lemma);
				}
				if (addMorphoToLeaves)
				{
					string morphStr = coreLabel.OriginalText();
					if (morphStr == null || morphStr.Equals(string.Empty))
					{
						morphStr = MorphoFeatureSpecification.NoAnalysis;
					}
					else
					{
						++nMorphAnalyses;
					}
					// Normalize punctuation analyses
					if (morphStr.StartsWith("PONCT"))
					{
						morphStr = "PUNC";
					}
					string newLeaf = string.Format("%s%s%s%s%s", coreLabel.Value(), MorphoFeatureSpecification.MorphoMark, lemma, MorphoFeatureSpecification.LemmaMark, morphStr);
					coreLabel.SetValue(newLeaf);
					coreLabel.SetWord(newLeaf);
				}
			}
		}

		private static void ReplacePOSTags(Tree tree)
		{
			IList<ILabel> yield = tree.Yield();
			IList<ILabel> preYield = tree.PreTerminalYield();
			System.Diagnostics.Debug.Assert(yield.Count == preYield.Count);
			MorphoFeatureSpecification spec = new FrenchMorphoFeatureSpecification();
			for (int i = 0; i < yield.Count; i++)
			{
				// Morphological Analysis
				string morphStr = ((CoreLabel)yield[i]).OriginalText();
				if (morphStr == null || morphStr.Equals(string.Empty))
				{
					morphStr = preYield[i].Value();
					// POS subcategory
					string subCat = ((CoreLabel)yield[i]).Category();
					if (subCat != null && subCat != string.Empty)
					{
						morphStr += "-" + subCat + "--";
					}
					else
					{
						morphStr += "---";
					}
				}
				MorphoFeatures feats = spec.StrToFeatures(morphStr);
				if (feats.GetAltTag() != null && !feats.GetAltTag().Equals(string.Empty))
				{
					CoreLabel cl = (CoreLabel)preYield[i];
					cl.SetValue(feats.GetAltTag());
					cl.SetTag(feats.GetAltTag());
				}
			}
		}

		/// <summary>Right now this outputs trees in PTB format.</summary>
		/// <remarks>
		/// Right now this outputs trees in PTB format.  It outputs one tree
		/// at a time until we have output enough trees to fill the given
		/// file, then moves on to the next file.  Trees are output in the
		/// order given in the <code>ids</code> list.
		/// <br />
		/// Trees have their words replaced with the words' lemmas, if those
		/// lemmas exist.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static void OutputSplits(IList<string> ids, IDictionary<string, Tree> treeMap)
		{
			IQueue<int> fSizeQueue = new LinkedList<int>(Arrays.AsList(fSizes));
			IQueue<string> fNameQueue = new LinkedList<string>(Arrays.AsList(fNames));
			TregexPattern pBadTree = TregexPattern.Compile("@SENT <: @PUNC");
			TregexPattern pBadTree2 = TregexPattern.Compile("@SENT <1 @PUNC <2 @PUNC !<3 __");
			ITreeTransformer tt = new FTBCorrector();
			int size = fSizeQueue.Remove();
			string filename = fNameQueue.Remove();
			log.Info("Outputing " + filename);
			PrintWriter writer = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(filename), "UTF-8")));
			int outputCount = 0;
			foreach (string id in ids)
			{
				if (!treeMap.Contains(id))
				{
					log.Info("Missing id: " + id);
					continue;
				}
				Tree tree = treeMap[id];
				TregexMatcher m = pBadTree.Matcher(tree);
				TregexMatcher m2 = pBadTree2.Matcher(tree);
				if (m.Find() || m2.Find())
				{
					log.Info("Discarding tree: " + tree.ToString());
					continue;
				}
				// Punctuation normalization, etc.
				Tree backupCopy = tree.DeepCopy();
				tree = tt.TransformTree(tree);
				if (tree.FirstChild().Children().Length == 0)
				{
					// Some trees have only punctuation. Tregex will mangle these. Don't throw those away.
					log.Info("Saving tree: " + tree.ToString());
					log.Info("Backup: " + backupCopy.ToString());
					tree = backupCopy;
				}
				if (LemmasAsLeaves || AddMorphoToLeaves)
				{
					MungeLeaves(tree, LemmasAsLeaves, AddMorphoToLeaves);
				}
				ReplacePOSTags(tree);
				writer.Println(tree.ToString());
				++outputCount;
				if (outputCount == size)
				{
					outputCount = 0;
					size = fSizeQueue.Remove();
					filename = fNameQueue.Remove();
					log.Info("Outputing " + filename);
					writer.Close();
					writer = new PrintWriter(new BufferedWriter(new OutputStreamWriter(new FileOutputStream(filename), "UTF-8")));
				}
			}
			writer.Close();
		}

		/// <summary>Converts a tree to the Morfette training format.</summary>
		private static string TreeToMorfette(Tree tree)
		{
			StringBuilder sb = new StringBuilder();
			IList<ILabel> yield = tree.Yield();
			IList<ILabel> tagYield = tree.PreTerminalYield();
			System.Diagnostics.Debug.Assert(yield.Count == tagYield.Count);
			int listLen = yield.Count;
			for (int i = 0; i < listLen; ++i)
			{
				CoreLabel token = (CoreLabel)yield[i];
				CoreLabel tag = (CoreLabel)tagYield[i];
				string morphStr = token.OriginalText();
				if (morphStr == null || morphStr.Equals(string.Empty))
				{
					morphStr = tag.Value();
				}
				string lemma = token.Lemma();
				if (lemma == null || lemma.Equals(string.Empty))
				{
					lemma = token.Value();
				}
				sb.Append(string.Format("%s %s %s%n", token.Value(), lemma, morphStr));
			}
			return sb.ToString();
		}

		/// <summary>
		/// Sample command line:
		/// <br />
		/// java edu.stanford.nlp.international.french.scripts.SplitCanditoTrees
		/// projects/core/src/edu/stanford/nlp/international/french/pipeline/splits/ftb-uc-2010.id_mrg
		/// ../data/french/corpus-fonctions/*.xml
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				System.Console.Error.Printf("Usage: java %s id_file [xml files]%n", typeof(Edu.Stanford.Nlp.International.French.Scripts.SplitCanditoTrees).FullName);
				System.Environment.Exit(-1);
			}
			// first arg is expected to be the file of IDs
			// all subsequent args are .xml files with the trees in them
			IList<string> ids = ReadIds(args[0]);
			log.Info("Read " + ids.Count + " ids");
			string[] newArgs = new string[args.Length - 1];
			for (int i = 1; i < args.Length; ++i)
			{
				newArgs[i - 1] = args[i];
			}
			IDictionary<string, Tree> treeMap = ReadTrees(newArgs);
			log.Info("Read " + treeMap.Count + " trees");
			PreprocessMWEs(treeMap);
			OutputSplits(ids, treeMap);
			if (nTokens != 0)
			{
				log.Info("CORPUS STATISTICS");
				System.Console.Error.Printf("#tokens:\t%d%n", nTokens);
				System.Console.Error.Printf("#with morph:\t%d%n", nMorphAnalyses);
			}
		}
	}
}
