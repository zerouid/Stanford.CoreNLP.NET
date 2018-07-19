using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Eval
{
	/// <summary>Utility class for extracting a variety of statistics from multi-lingual treebanks.</summary>
	/// <remarks>
	/// Utility class for extracting a variety of statistics from multi-lingual treebanks.
	/// TODO(spenceg) Add sample standard deviation
	/// </remarks>
	/// <author>Spence Green</author>
	public class TreebankStats
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Eval.TreebankStats));

		private readonly Language languageName;

		private readonly ITreebankLangParserParams tlpp;

		private readonly IList<string> pathNames;

		private enum Split
		{
			Train,
			Dev,
			Test
		}

		private IDictionary<TreebankStats.Split, ICollection<string>> splitFileLists;

		private bool useSplit = false;

		private bool makeVocab = false;

		private static ICollection<string> trainVocab = null;

		public TreebankStats(Language langName, IList<string> paths, ITreebankLangParserParams tlpp)
		{
			languageName = langName;
			pathNames = paths;
			this.tlpp = tlpp;
		}

		public virtual bool UseSplit(string prefix)
		{
			IDictionary<TreebankStats.Split, File> splitMap = Generics.NewHashMap();
			splitMap[TreebankStats.Split.Train] = new File(prefix + ".train");
			splitMap[TreebankStats.Split.Test] = new File(prefix + ".test");
			splitMap[TreebankStats.Split.Dev] = new File(prefix + ".dev");
			splitFileLists = Generics.NewHashMap();
			foreach (KeyValuePair<TreebankStats.Split, File> entry in splitMap)
			{
				File f = entry.Value;
				if (!f.Exists())
				{
					return false;
				}
				ICollection<string> files = Generics.NewHashSet();
				foreach (string fileName in IOUtils.ReadLines(f))
				{
					files.Add(fileName);
				}
				splitFileLists[entry.Key] = files;
			}
			useSplit = true;
			return true;
		}

		private TreebankStats.ObservedCorpusStats GatherStats(DiskTreebank tb, string name)
		{
			TreebankStats.ObservedCorpusStats ocs = new TreebankStats.ObservedCorpusStats(name);
			if (makeVocab)
			{
				trainVocab = Generics.NewHashSet();
			}
			System.Console.Out.WriteLine("Reading treebank:");
			foreach (Tree t in tb)
			{
				Pair<int, int> treeFacts = DissectTree(t, ocs, makeVocab);
				ocs.AddStatsForTree(t.Yield().Count, treeFacts.First(), treeFacts.Second());
				if (ocs.numTrees % 100 == 0)
				{
					System.Console.Out.Write(".");
				}
				else
				{
					if (ocs.numTrees % 8001 == 0)
					{
						System.Console.Out.WriteLine();
					}
				}
			}
			ocs.ComputeFinalValues();
			System.Console.Out.WriteLine("done!");
			return ocs;
		}

		/// <summary>Returns pair of (depth,breadth) of tree.</summary>
		/// <remarks>Returns pair of (depth,breadth) of tree. Does a breadth-first search.</remarks>
		/// <param name="t"/>
		/// <param name="ocs"/>
		/// <param name="addToVocab"/>
		private static Pair<int, int> DissectTree(Tree t, TreebankStats.ObservedCorpusStats ocs, bool addToVocab)
		{
			Stack<Pair<int, Tree>> stack = new Stack<Pair<int, Tree>>();
			stack.Push(new Pair<int, Tree>(0, t));
			int maxBreadth = 0;
			int maxDepth = -1;
			if (t == null)
			{
				throw new Exception("Null tree passed to dissectTree()");
			}
			else
			{
				while (!stack.IsEmpty())
				{
					Pair<int, Tree> depthNode = stack.Pop();
					int nodeDepth = depthNode.First();
					Tree node = depthNode.Second();
					if (nodeDepth != maxDepth)
					{
						maxDepth = nodeDepth;
						if (node.IsPhrasal() && stack.Count + 1 > maxBreadth)
						{
							maxBreadth = stack.Count + 1;
						}
					}
					if (node.IsPhrasal())
					{
						ocs.AddPhrasalBranch(node.Value(), node.Children().Length);
					}
					else
					{
						if (node.IsPreTerminal())
						{
							ocs.posTags.IncrementCount(node.Value());
						}
						else
						{
							if (node.IsLeaf())
							{
								ocs.words.IncrementCount(node.Value());
								if (addToVocab)
								{
									trainVocab.Add(node.Value());
								}
							}
						}
					}
					foreach (Tree kid in node.Children())
					{
						stack.Push(new Pair<int, Tree>(nodeDepth + 1, kid));
					}
				}
			}
			return new Pair<int, int>(maxDepth, maxBreadth);
		}

		private static void Display(TreebankStats.ObservedCorpusStats corpStats, bool displayWords, bool displayOOV)
		{
			System.Console.Out.WriteLine("####################################################################");
			System.Console.Out.WriteLine("## " + corpStats.GetName());
			System.Console.Out.WriteLine("####################################################################");
			System.Console.Out.WriteLine();
			corpStats.Display(displayWords, displayOOV);
		}

		private static TreebankStats.ObservedCorpusStats AggregateStats(IList<TreebankStats.ObservedCorpusStats> allStats)
		{
			if (allStats.Count == 0)
			{
				return null;
			}
			else
			{
				if (allStats.Count == 1)
				{
					return allStats[0];
				}
			}
			TreebankStats.ObservedCorpusStats agStats = new TreebankStats.ObservedCorpusStats("CORPUS");
			foreach (TreebankStats.ObservedCorpusStats ocs in allStats)
			{
				agStats.numTrees += ocs.numTrees;
				agStats.breadth2 += ocs.breadth2;
				Sharpen.Collections.AddAll(agStats.breadths, ocs.breadths);
				agStats.depth2 += ocs.depth2;
				Sharpen.Collections.AddAll(agStats.depths, ocs.depths);
				agStats.length2 += ocs.length2;
				Sharpen.Collections.AddAll(agStats.lengths, ocs.lengths);
				if (ocs.minLength < agStats.minLength)
				{
					agStats.minLength = ocs.minLength;
				}
				if (ocs.maxLength > agStats.maxLength)
				{
					agStats.maxLength = ocs.maxLength;
				}
				if (ocs.minBreadth < agStats.minBreadth)
				{
					agStats.minBreadth = ocs.minBreadth;
				}
				if (ocs.maxBreadth > agStats.maxBreadth)
				{
					agStats.maxBreadth = ocs.maxBreadth;
				}
				if (ocs.minDepth < agStats.minDepth)
				{
					agStats.minDepth = ocs.minDepth;
				}
				if (ocs.maxDepth > agStats.maxDepth)
				{
					agStats.maxDepth = ocs.maxDepth;
				}
				agStats.words.AddAll(ocs.words);
				agStats.posTags.AddAll(ocs.posTags);
				agStats.phrasalBranching2.AddAll(ocs.phrasalBranching2);
				agStats.phrasalBranchingNum2.AddAll(ocs.phrasalBranchingNum2);
			}
			agStats.ComputeFinalValues();
			return agStats;
		}

		public virtual void Run(bool pathsAreFiles, bool displayWords, bool displayOOV)
		{
			if (useSplit)
			{
				IList<TreebankStats.ObservedCorpusStats> allSplitStats = new List<TreebankStats.ObservedCorpusStats>();
				makeVocab = true;
				foreach (KeyValuePair<TreebankStats.Split, ICollection<string>> split in splitFileLists)
				{
					DiskTreebank tb = tlpp.DiskTreebank();
					IFileFilter splitFilter = new TreebankStats.SplitFilter(split.Value);
					foreach (string path in pathNames)
					{
						tb.LoadPath(path, splitFilter);
					}
					TreebankStats.ObservedCorpusStats splitStats = GatherStats(tb, languageName.ToString() + "." + split.Key.ToString());
					allSplitStats.Add(splitStats);
					makeVocab = false;
				}
				Display(AggregateStats(allSplitStats), displayWords, displayOOV);
				foreach (TreebankStats.ObservedCorpusStats ocs in allSplitStats)
				{
					Display(ocs, displayWords, displayOOV);
				}
			}
			else
			{
				if (pathsAreFiles)
				{
					makeVocab = true;
					foreach (string path in pathNames)
					{
						DiskTreebank tb = tlpp.DiskTreebank();
						tb.LoadPath(path, null);
						TreebankStats.ObservedCorpusStats stats = GatherStats(tb, languageName.ToString() + "  " + path);
						Display(stats, displayWords, displayOOV);
						makeVocab = false;
					}
				}
				else
				{
					trainVocab = Generics.NewHashSet();
					DiskTreebank tb = tlpp.DiskTreebank();
					foreach (string path in pathNames)
					{
						tb.LoadPath(path, null);
					}
					TreebankStats.ObservedCorpusStats allStats = GatherStats(tb, languageName.ToString());
					Display(allStats, displayWords, displayOOV);
				}
			}
		}

		protected internal class SplitFilter : IFileFilter
		{
			private readonly ICollection<string> filterMap;

			public SplitFilter(ICollection<string> fileList)
			{
				filterMap = fileList;
			}

			public virtual bool Accept(File f)
			{
				return filterMap.Contains(f.GetName());
			}
		}

		protected internal class ObservedCorpusStats
		{
			private readonly string corpusName;

			public ObservedCorpusStats(string name)
			{
				corpusName = name;
				words = new ClassicCounter<string>();
				posTags = new ClassicCounter<string>();
				phrasalBranching2 = new ClassicCounter<string>();
				phrasalBranchingNum2 = new ClassicCounter<string>();
				lengths = new List<int>();
				depths = new List<int>();
				breadths = new List<int>();
			}

			public virtual string GetName()
			{
				return corpusName;
			}

			public virtual void AddStatsForTree(int yieldLength, int depth, int breadth)
			{
				numTrees++;
				breadths.Add(breadth);
				breadth2 += breadth;
				lengths.Add(yieldLength);
				length2 += yieldLength;
				depths.Add(depth);
				depth2 += depth;
				if (depth < minDepth)
				{
					minDepth = depth;
				}
				else
				{
					if (depth > maxDepth)
					{
						maxDepth = depth;
					}
				}
				if (yieldLength < minLength)
				{
					minLength = yieldLength;
				}
				else
				{
					if (yieldLength > maxLength)
					{
						maxLength = yieldLength;
					}
				}
				if (breadth < minBreadth)
				{
					minBreadth = breadth;
				}
				else
				{
					if (breadth > maxBreadth)
					{
						maxBreadth = breadth;
					}
				}
			}

			public virtual double GetPercLensLessThan(int maxLen)
			{
				int lens = 0;
				foreach (int len in lengths)
				{
					if (len <= maxLen)
					{
						lens++;
					}
				}
				return (double)lens / (double)lengths.Count;
			}

			public virtual void AddPhrasalBranch(string label, int factor)
			{
				phrasalBranching2.IncrementCount(label, factor);
				phrasalBranchingNum2.IncrementCount(label);
			}

			public virtual void Display(bool displayWords, bool displayOOV)
			{
				NumberFormat nf = new DecimalFormat("0.00");
				System.Console.Out.WriteLine("======================================================");
				System.Console.Out.WriteLine(">>> " + corpusName);
				System.Console.Out.WriteLine(" trees:\t\t" + numTrees);
				System.Console.Out.WriteLine(" words:\t\t" + words.KeySet().Count);
				System.Console.Out.WriteLine(" tokens:\t" + (int)words.TotalCount());
				System.Console.Out.WriteLine(" tags:\t\t" + posTags.Size());
				System.Console.Out.WriteLine(" phrasal types:\t" + phrasalBranchingNum2.KeySet().Count);
				System.Console.Out.WriteLine(" phrasal nodes:\t" + (int)phrasalBranchingNum2.TotalCount());
				System.Console.Out.WriteLine(" OOV rate:\t" + nf.Format(OOVRate * 100.0) + "%");
				System.Console.Out.WriteLine("======================================================");
				System.Console.Out.WriteLine(">>> Per tree means");
				System.Console.Out.Printf(" depth:\t\t%s\t{min:%d\tmax:%d}\t\ts: %s\n", nf.Format(meanDepth), minDepth, maxDepth, nf.Format(stddevDepth));
				System.Console.Out.Printf(" breadth:\t%s\t{min:%d\tmax:%d}\ts: %s\n", nf.Format(meanBreadth), minBreadth, maxBreadth, nf.Format(stddevBreadth));
				System.Console.Out.Printf(" length:\t%s\t{min:%d\tmax:%d}\ts: %s\n", nf.Format(meanLength), minLength, maxLength, nf.Format(stddevLength));
				System.Console.Out.WriteLine(" branching:\t" + nf.Format(meanBranchingFactor));
				System.Console.Out.WriteLine(" constituents:\t" + nf.Format(meanConstituents));
				System.Console.Out.WriteLine("======================================================");
				System.Console.Out.WriteLine(">>> Branching factor means by phrasal tag:");
				IList<string> sortedKeys = new List<string>(meanBranchingByLabel.KeySet());
				sortedKeys.Sort(Counters.ToComparator(phrasalBranchingNum2, false, true));
				foreach (string label in sortedKeys)
				{
					System.Console.Out.Printf(" %s:\t\t%s  /  %d instances\n", label, nf.Format(meanBranchingByLabel.GetCount(label)), (int)phrasalBranchingNum2.GetCount(label));
				}
				System.Console.Out.WriteLine("======================================================");
				System.Console.Out.WriteLine(">>> Phrasal tag counts");
				sortedKeys = new List<string>(phrasalBranchingNum2.KeySet());
				sortedKeys.Sort(Counters.ToComparator(phrasalBranchingNum2, false, true));
				foreach (string label_1 in sortedKeys)
				{
					System.Console.Out.WriteLine(" " + label_1 + ":\t\t" + (int)phrasalBranchingNum2.GetCount(label_1));
				}
				System.Console.Out.WriteLine("======================================================");
				System.Console.Out.WriteLine(">>> POS tag counts");
				sortedKeys = new List<string>(posTags.KeySet());
				sortedKeys.Sort(Counters.ToComparator(posTags, false, true));
				foreach (string posTag in sortedKeys)
				{
					System.Console.Out.WriteLine(" " + posTag + ":\t\t" + (int)posTags.GetCount(posTag));
				}
				System.Console.Out.WriteLine("======================================================");
				if (displayWords)
				{
					System.Console.Out.WriteLine(">>> Word counts");
					sortedKeys = new List<string>(words.KeySet());
					sortedKeys.Sort(Counters.ToComparator(words, false, true));
					foreach (string word in sortedKeys)
					{
						System.Console.Out.WriteLine(" " + word + ":\t\t" + (int)words.GetCount(word));
					}
					System.Console.Out.WriteLine("======================================================");
				}
				if (displayOOV)
				{
					System.Console.Out.WriteLine(">>> OOV word types");
					foreach (string word in oovWords)
					{
						System.Console.Out.WriteLine(" " + word);
					}
					System.Console.Out.WriteLine("======================================================");
				}
			}

			public virtual void ComputeFinalValues()
			{
				double denom = (double)numTrees;
				meanDepth = depth2 / denom;
				meanLength = length2 / denom;
				meanBreadth = breadth2 / denom;
				meanConstituents = phrasalBranchingNum2.TotalCount() / denom;
				meanBranchingFactor = phrasalBranching2.TotalCount() / phrasalBranchingNum2.TotalCount();
				//Compute *actual* stddev (we iterate over the whole population)
				foreach (int d in depths)
				{
					stddevDepth += Math.Pow(d - meanDepth, 2);
				}
				stddevDepth = Math.Sqrt(stddevDepth / denom);
				foreach (int l in lengths)
				{
					stddevLength += Math.Pow(l - meanLength, 2);
				}
				stddevLength = Math.Sqrt(stddevLength / denom);
				foreach (int b in breadths)
				{
					stddevBreadth += Math.Pow(b - meanBreadth, 2);
				}
				stddevBreadth = Math.Sqrt(stddevBreadth / denom);
				meanBranchingByLabel = new ClassicCounter<string>();
				foreach (string label in phrasalBranching2.KeySet())
				{
					double mean = phrasalBranching2.GetCount(label) / phrasalBranchingNum2.GetCount(label);
					meanBranchingByLabel.IncrementCount(label, mean);
				}
				oovWords = Generics.NewHashSet(words.KeySet());
				oovWords.RemoveAll(trainVocab);
				OOVRate = (double)oovWords.Count / (double)words.KeySet().Count;
			}

			public readonly ICounter<string> words;

			public readonly ICounter<string> posTags;

			private readonly ICounter<string> phrasalBranching2;

			private readonly ICounter<string> phrasalBranchingNum2;

			public int numTrees = 0;

			private double depth2 = 0.0;

			private double breadth2 = 0.0;

			private double length2 = 0.0;

			private readonly IList<int> lengths;

			private readonly IList<int> breadths;

			private readonly IList<int> depths;

			private ICounter<string> meanBranchingByLabel;

			private double meanDepth = 0.0;

			private double stddevDepth = 0.0;

			private double meanBranchingFactor = 0.0;

			private double meanConstituents = 0.0;

			private double meanLength = 0.0;

			private double stddevLength = 0.0;

			private double meanBreadth = 0.0;

			private double stddevBreadth = 0.0;

			private double OOVRate = 0.0;

			private ICollection<string> oovWords;

			public int minLength = int.MaxValue;

			public int maxLength = int.MinValue;

			public int minDepth = int.MaxValue;

			public int maxDepth = int.MinValue;

			public int minBreadth = int.MaxValue;

			public int maxBreadth = int.MinValue;
			//Corpus wide
			//Tree-level Averages
			//Mins and maxes
		}

		private const int MinArgs = 2;

		private static string Usage()
		{
			StringBuilder usage = new StringBuilder();
			string nl = Runtime.GetProperty("line.separator");
			usage.Append(string.Format("Usage: java %s [OPTS] LANG paths%n%n", typeof(TreebankStats).FullName));
			usage.Append("Options:").Append(nl);
			usage.Append(" LANG is one of " + Language.langList).Append(nl);
			usage.Append("  -s prefix : Use a split (extensions must be dev/test/train)").Append(nl);
			usage.Append("  -w        : Show word distribution").Append(nl);
			usage.Append("  -f        : Path list is a set of files, and the first file is the training set").Append(nl);
			usage.Append("  -o        : Print OOV words.").Append(nl);
			return usage.ToString();
		}

		private static IDictionary<string, int> OptArgDefs()
		{
			IDictionary<string, int> optArgDefs = Generics.NewHashMap(4);
			optArgDefs["s"] = 1;
			optArgDefs["w"] = 0;
			optArgDefs["f"] = 0;
			optArgDefs["o"] = 0;
			return optArgDefs;
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length < MinArgs)
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			Properties options = StringUtils.ArgsToProperties(args, OptArgDefs());
			string splitPrefix = options.GetProperty("s", null);
			bool ShowWords = PropertiesUtils.GetBool(options, "w", false);
			bool pathsAreFiles = PropertiesUtils.GetBool(options, "f", false);
			bool ShowOov = PropertiesUtils.GetBool(options, "o", false);
			string[] parsedArgs = options.GetProperty(string.Empty, string.Empty).Split("\\s+");
			if (parsedArgs.Length != MinArgs)
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			Language language = Language.ValueOf(parsedArgs[0]);
			IList<string> corpusPaths = new List<string>(parsedArgs.Length - 1);
			for (int i = 1; i < parsedArgs.Length; ++i)
			{
				corpusPaths.Add(parsedArgs[i]);
			}
			ITreebankLangParserParams tlpp = language.@params;
			TreebankStats cs = new TreebankStats(language, corpusPaths, tlpp);
			if (splitPrefix != null)
			{
				if (!cs.UseSplit(splitPrefix))
				{
					log.Info("Could not load split!");
				}
			}
			cs.Run(pathsAreFiles, ShowWords, ShowOov);
		}
	}
}
