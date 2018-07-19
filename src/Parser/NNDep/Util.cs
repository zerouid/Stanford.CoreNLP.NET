using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Nndep
{
	/// <summary>Some utility functions for the neural network dependency parser.</summary>
	/// <author>Danqi Chen</author>
	/// <author>Jon Gauthier</author>
	public class Util
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Nndep.Util));

		private Util()
		{
		}

		private static Random random;

		// static methods
		/// <summary>Normalize word embeddings by setting mean = rMean, std = rStd</summary>
		public static double[][] Scaling(double[][] A, double rMean, double rStd)
		{
			int count = 0;
			double mean = 0.0;
			double std = 0.0;
			foreach (double[] aA in A)
			{
				foreach (double v in aA)
				{
					count += 1;
					mean += v;
					std += v * v;
				}
			}
			mean = mean / count;
			std = Math.Sqrt(std / count - mean * mean);
			log.Info("Scaling word embeddings:");
			log.Info(string.Format("(mean = %.2f, std = %.2f) -> (mean = %.2f, std = %.2f)", mean, std, rMean, rStd));
			double[][] rA = new double[A.Length][];
			for (int i = 0; i < rA.Length; ++i)
			{
				for (int j = 0; j < rA[i].Length; ++j)
				{
					rA[i][j] = (A[i][j] - mean) * rStd / std + rMean;
				}
			}
			return rA;
		}

		/// <summary>Normalize word embeddings by setting mean = 0, std = 1</summary>
		public static double[][] Scaling(double[][] A)
		{
			return Scaling(A, 0.0, 1.0);
		}

		// return strings sorted by frequency, and filter out those with freq. less than cutOff.
		/// <summary>Build a dictionary of words collected from a corpus.</summary>
		/// <remarks>
		/// Build a dictionary of words collected from a corpus.
		/// <p>
		/// Filters out words with a frequency below the given
		/// <paramref name="cutOff"/>
		/// .
		/// </remarks>
		/// <returns>
		/// Words sorted by decreasing frequency, filtered to remove
		/// any words with a frequency below
		/// <paramref name="cutOff"/>
		/// </returns>
		public static IList<string> GenerateDict(IList<string> str, int cutOff)
		{
			ICounter<string> freq = new IntCounter<string>();
			foreach (string aStr in str)
			{
				freq.IncrementCount(aStr);
			}
			IList<string> keys = Counters.ToSortedList(freq, false);
			IList<string> dict = new List<string>();
			foreach (string word in keys)
			{
				if (freq.GetCount(word) >= cutOff)
				{
					dict.Add(word);
				}
			}
			return dict;
		}

		public static IList<string> GenerateDict(IList<string> str)
		{
			return GenerateDict(str, 1);
		}

		/// <returns>Shared random generator used in this package</returns>
		internal static Random GetRandom()
		{
			if (random != null)
			{
				return random;
			}
			else
			{
				return GetRandom(Runtime.CurrentTimeMillis());
			}
		}

		/// <summary>Set up shared random generator to use the given seed.</summary>
		/// <returns>Shared random generator object</returns>
		private static Random GetRandom(long seed)
		{
			random = new Random(seed);
			log.Info(string.Format("Random generator initialized with seed %d%n", seed));
			return random;
		}

		public static IList<T> GetRandomSubList<T>(IList<T> input, int subsetSize)
		{
			int inputSize = input.Count;
			if (subsetSize > inputSize)
			{
				subsetSize = inputSize;
			}
			Random random = GetRandom();
			for (int i = 0; i < subsetSize; i++)
			{
				int indexToSwap = i + random.NextInt(inputSize - i);
				T temp = input[i];
				input.Set(i, input[indexToSwap]);
				input.Set(indexToSwap, temp);
			}
			return input.SubList(0, subsetSize);
		}

		// TODO replace with GrammaticalStructure#readCoNLLGrammaticalStructureCollection
		public static void LoadConllFile(string inFile, IList<ICoreMap> sents, IList<DependencyTree> trees, bool unlabeled, bool cPOS)
		{
			CoreLabelTokenFactory tf = new CoreLabelTokenFactory(false);
			try
			{
				using (BufferedReader reader = IOUtils.ReaderFromString(inFile))
				{
					IList<CoreLabel> sentenceTokens = new List<CoreLabel>();
					DependencyTree tree = new DependencyTree();
					foreach (string line in IOUtils.GetLineIterable(reader, false))
					{
						string[] splits = line.Split("\t");
						if (splits.Length < 10)
						{
							if (sentenceTokens.Count > 0)
							{
								trees.Add(tree);
								ICoreMap sentence = new CoreLabel();
								sentence.Set(typeof(CoreAnnotations.TokensAnnotation), sentenceTokens);
								sents.Add(sentence);
								tree = new DependencyTree();
								sentenceTokens = new List<CoreLabel>();
							}
						}
						else
						{
							string word = splits[1];
							string pos = cPOS ? splits[3] : splits[4];
							string depType = splits[7];
							int head = -1;
							try
							{
								head = System.Convert.ToInt32(splits[6]);
							}
							catch (NumberFormatException)
							{
								continue;
							}
							CoreLabel token = tf.MakeToken(word, 0, 0);
							token.SetTag(pos);
							token.Set(typeof(CoreAnnotations.CoNLLDepParentIndexAnnotation), head);
							token.Set(typeof(CoreAnnotations.CoNLLDepTypeAnnotation), depType);
							sentenceTokens.Add(token);
							if (!unlabeled)
							{
								tree.Add(head, depType);
							}
							else
							{
								tree.Add(head, Config.Unknown);
							}
						}
					}
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public static void LoadConllFile(string inFile, IList<ICoreMap> sents, IList<DependencyTree> trees)
		{
			LoadConllFile(inFile, sents, trees, false, false);
		}

		public static void WriteConllFile(string outFile, IList<ICoreMap> sentences, IList<DependencyTree> trees)
		{
			try
			{
				PrintWriter output = IOUtils.GetPrintWriter(outFile);
				for (int i = 0; i < sentences.Count; i++)
				{
					ICoreMap sentence = sentences[i];
					DependencyTree tree = trees[i];
					IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
					for (int j = 1; j <= size; ++j)
					{
						CoreLabel token = tokens[j - 1];
						output.Printf("%d\t%s\t_\t%s\t%s\t_\t%d\t%s\t_\t_%n", j, token.Word(), token.Tag(), token.Tag(), tree.GetHead(j), tree.GetLabel(j));
					}
					output.Println();
				}
				output.Close();
			}
			catch (Exception e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public static void PrintTreeStats(string str, IList<DependencyTree> trees)
		{
			log.Info(Config.Separator + ' ' + str);
			int nTrees = trees.Count;
			int nonTree = 0;
			int multiRoot = 0;
			int nonProjective = 0;
			foreach (DependencyTree tree in trees)
			{
				if (!tree.IsTree())
				{
					++nonTree;
				}
				else
				{
					if (!tree.IsProjective())
					{
						++nonProjective;
					}
					if (!tree.IsSingleRoot())
					{
						++multiRoot;
					}
				}
			}
			log.Info(string.Format("#Trees: %d%n", nTrees));
			log.Info(string.Format("%d tree(s) are illegal (%.2f%%).%n", nonTree, nonTree * 100.0 / nTrees));
			log.Info(string.Format("%d tree(s) are legal but have multiple roots (%.2f%%).%n", multiRoot, multiRoot * 100.0 / nTrees));
			log.Info(string.Format("%d tree(s) are legal but not projective (%.2f%%).%n", nonProjective, nonProjective * 100.0 / nTrees));
		}

		public static void PrintTreeStats(IList<DependencyTree> trees)
		{
			PrintTreeStats(string.Empty, trees);
		}
	}
}
