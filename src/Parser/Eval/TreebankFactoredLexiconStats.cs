using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.International.Arabic;
using Edu.Stanford.Nlp.International.French;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Eval
{
	/// <summary>Computes gross statistics for morphological annotations in a treebank.</summary>
	/// <author>Spence Green</author>
	public class TreebankFactoredLexiconStats
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(TreebankFactoredLexiconStats));

		//  private static String stripTag(String tag) {
		//    if (tag.startsWith("DT")) {
		//      String newTag = tag.substring(2, tag.length());
		//      return newTag.length() > 0 ? newTag : tag;
		//    }
		//    return tag;
		//  }
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 3)
			{
				System.Console.Error.Printf("Usage: java %s language filename features%n", typeof(TreebankFactoredLexiconStats).FullName);
				System.Environment.Exit(-1);
			}
			Language language = Language.ValueOf(args[0]);
			ITreebankLangParserParams tlpp = language.@params;
			if (language.Equals(Language.Arabic))
			{
				string[] options = new string[] { "-arabicFactored" };
				tlpp.SetOptionFlag(options, 0);
			}
			else
			{
				string[] options = new string[] { "-frenchFactored" };
				tlpp.SetOptionFlag(options, 0);
			}
			Treebank tb = tlpp.DiskTreebank();
			tb.LoadPath(args[1]);
			MorphoFeatureSpecification morphoSpec = language.Equals(Language.Arabic) ? new ArabicMorphoFeatureSpecification() : new FrenchMorphoFeatureSpecification();
			string[] features = args[2].Trim().Split(",");
			foreach (string feature in features)
			{
				morphoSpec.Activate(MorphoFeatureSpecification.MorphoFeatureType.ValueOf(feature));
			}
			// Counters
			ICounter<string> wordTagCounter = new ClassicCounter<string>(30000);
			ICounter<string> morphTagCounter = new ClassicCounter<string>(500);
			//    Counter<String> signatureTagCounter = new ClassicCounter<String>();
			ICounter<string> morphCounter = new ClassicCounter<string>(500);
			ICounter<string> wordCounter = new ClassicCounter<string>(30000);
			ICounter<string> tagCounter = new ClassicCounter<string>(300);
			ICounter<string> lemmaCounter = new ClassicCounter<string>(25000);
			ICounter<string> lemmaTagCounter = new ClassicCounter<string>(25000);
			ICounter<string> richTagCounter = new ClassicCounter<string>(1000);
			ICounter<string> reducedTagCounter = new ClassicCounter<string>(500);
			ICounter<string> reducedTagLemmaCounter = new ClassicCounter<string>(500);
			IDictionary<string, ICollection<string>> wordLemmaMap = Generics.NewHashMap();
			TwoDimensionalIntCounter<string, string> lemmaReducedTagCounter = new TwoDimensionalIntCounter<string, string>(30000);
			TwoDimensionalIntCounter<string, string> reducedTagTagCounter = new TwoDimensionalIntCounter<string, string>(500);
			TwoDimensionalIntCounter<string, string> tagReducedTagCounter = new TwoDimensionalIntCounter<string, string>(300);
			int numTrees = 0;
			foreach (Tree tree in tb)
			{
				foreach (Tree subTree in tree)
				{
					if (!subTree.IsLeaf())
					{
						tlpp.TransformTree(subTree, tree);
					}
				}
				IList<ILabel> pretermList = tree.PreTerminalYield();
				IList<ILabel> yield = tree.Yield();
				System.Diagnostics.Debug.Assert(yield.Count == pretermList.Count);
				int yieldLen = yield.Count;
				for (int i = 0; i < yieldLen; ++i)
				{
					string tag = pretermList[i].Value();
					string word = yield[i].Value();
					string morph = ((CoreLabel)yield[i]).OriginalText();
					// Note: if there is no lemma, then we use the surface form.
					Pair<string, string> lemmaTag = MorphoFeatureSpecification.SplitMorphString(word, morph);
					string lemma = lemmaTag.First();
					string richTag = lemmaTag.Second();
					// WSGDEBUG
					if (tag.Contains("MW"))
					{
						lemma += "-MWE";
					}
					lemmaCounter.IncrementCount(lemma);
					lemmaTagCounter.IncrementCount(lemma + tag);
					richTagCounter.IncrementCount(richTag);
					string reducedTag = morphoSpec.StrToFeatures(richTag).ToString();
					reducedTagCounter.IncrementCount(reducedTag);
					reducedTagLemmaCounter.IncrementCount(reducedTag + lemma);
					wordTagCounter.IncrementCount(word + tag);
					morphTagCounter.IncrementCount(morph + tag);
					morphCounter.IncrementCount(morph);
					wordCounter.IncrementCount(word);
					tagCounter.IncrementCount(tag);
					reducedTag = reducedTag.Equals(string.Empty) ? "NONE" : reducedTag;
					if (wordLemmaMap.Contains(word))
					{
						wordLemmaMap[word].Add(lemma);
					}
					else
					{
						ICollection<string> lemmas = Generics.NewHashSet(1);
						wordLemmaMap[word] = lemmas;
					}
					lemmaReducedTagCounter.IncrementCount(lemma, reducedTag);
					reducedTagTagCounter.IncrementCount(lemma + reducedTag, tag);
					tagReducedTagCounter.IncrementCount(tag, reducedTag);
				}
				++numTrees;
			}
			// Barf...
			System.Console.Out.WriteLine("Language: " + language.ToString());
			System.Console.Out.Printf("#trees:\t%d%n", numTrees);
			System.Console.Out.Printf("#tokens:\t%d%n", (int)wordCounter.TotalCount());
			System.Console.Out.Printf("#words:\t%d%n", wordCounter.KeySet().Count);
			System.Console.Out.Printf("#tags:\t%d%n", tagCounter.KeySet().Count);
			System.Console.Out.Printf("#wordTagPairs:\t%d%n", wordTagCounter.KeySet().Count);
			System.Console.Out.Printf("#lemmas:\t%d%n", lemmaCounter.KeySet().Count);
			System.Console.Out.Printf("#lemmaTagPairs:\t%d%n", lemmaTagCounter.KeySet().Count);
			System.Console.Out.Printf("#feattags:\t%d%n", reducedTagCounter.KeySet().Count);
			System.Console.Out.Printf("#feattag+lemmas:\t%d%n", reducedTagLemmaCounter.KeySet().Count);
			System.Console.Out.Printf("#richtags:\t%d%n", richTagCounter.KeySet().Count);
			System.Console.Out.Printf("#richtag+lemma:\t%d%n", morphCounter.KeySet().Count);
			System.Console.Out.Printf("#richtag+lemmaTagPairs:\t%d%n", morphTagCounter.KeySet().Count);
			// Extra
			System.Console.Out.WriteLine("==================");
			StringBuilder sbNoLemma = new StringBuilder();
			StringBuilder sbMultLemmas = new StringBuilder();
			foreach (KeyValuePair<string, ICollection<string>> wordLemmas in wordLemmaMap)
			{
				string word = wordLemmas.Key;
				ICollection<string> lemmas = wordLemmas.Value;
				if (lemmas.Count == 0)
				{
					sbNoLemma.Append("NO LEMMAS FOR WORD: " + word + "\n");
					continue;
				}
				if (lemmas.Count > 1)
				{
					sbMultLemmas.Append("MULTIPLE LEMMAS: " + word + " " + SetToString(lemmas) + "\n");
					continue;
				}
				string lemma = lemmas.GetEnumerator().Current;
				ICollection<string> reducedTags = lemmaReducedTagCounter.GetCounter(lemma).KeySet();
				if (reducedTags.Count > 1)
				{
					System.Console.Out.Printf("%s --> %s%n", word, lemma);
					foreach (string reducedTag in reducedTags)
					{
						int count = lemmaReducedTagCounter.GetCount(lemma, reducedTag);
						string posTags = SetToString(reducedTagTagCounter.GetCounter(lemma + reducedTag).KeySet());
						System.Console.Out.Printf("\t%s\t%d\t%s%n", reducedTag, count, posTags);
					}
					System.Console.Out.WriteLine();
				}
			}
			System.Console.Out.WriteLine("==================");
			System.Console.Out.WriteLine(sbNoLemma.ToString());
			System.Console.Out.WriteLine(sbMultLemmas.ToString());
			System.Console.Out.WriteLine("==================");
			IList<string> tags = new List<string>(tagReducedTagCounter.FirstKeySet());
			tags.Sort();
			foreach (string tag_1 in tags)
			{
				System.Console.Out.WriteLine(tag_1);
				ICollection<string> reducedTags = tagReducedTagCounter.GetCounter(tag_1).KeySet();
				foreach (string reducedTag in reducedTags)
				{
					int count = tagReducedTagCounter.GetCount(tag_1, reducedTag);
					//        reducedTag = reducedTag.equals("") ? "NONE" : reducedTag;
					System.Console.Out.Printf("\t%s\t%d%n", reducedTag, count);
				}
				System.Console.Out.WriteLine();
			}
			System.Console.Out.WriteLine("==================");
		}

		private static string SetToString(ICollection<string> set)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[");
			foreach (string @string in set)
			{
				sb.Append(@string).Append(" ");
			}
			sb.Append("]");
			return sb.ToString();
		}
	}
}
