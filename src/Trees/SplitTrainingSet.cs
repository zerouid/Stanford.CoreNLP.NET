using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Given a list of trees, splits the trees into three separate files.</summary>
	/// <remarks>
	/// Given a list of trees, splits the trees into three separate files.
	/// <br />
	/// The program uses a random seed to divide the trees.  If the input
	/// dataset is later extended, the same seed can be used and trees
	/// which did not change position in the data set will be put in the
	/// same division.
	/// <br />
	/// Example command line:
	/// <code>java edu.stanford.nlp.trees.SplitTrainingSet -input foo.mrg -output bar.mrg -seed 1000</code>
	/// </remarks>
	public class SplitTrainingSet
	{
		private static Redwood.RedwoodChannels logger = Redwood.Channels(typeof(SplitTrainingSet));

		private static string Input = null;

		private static string Output = null;

		private static string[] SplitNames = new string[] { "train", "dev", "test" };

		private static double[] SplitWeights = new double[] { 0.7, 0.15, 0.15 };

		private static long Seed = 0L;

		public static int WeightedIndex(IList<double> weights, Random random)
		{
			double offset = random.NextDouble();
			int index = 0;
			foreach (double weight in weights)
			{
				offset = offset - weight;
				if (offset < 0.0)
				{
					return index;
				}
				index = index + 1;
			}
			return weights.Count - 1;
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			// Parse the arguments
			Properties props = StringUtils.ArgsToProperties(args);
			ArgumentParser.FillOptions(new Type[] { typeof(ArgumentParser), typeof(SplitTrainingSet) }, props);
			if (SplitNames.Length != SplitWeights.Length)
			{
				throw new ArgumentException("Name and weight arrays must be of the same length");
			}
			double totalWeight = 0.0;
			foreach (double weight in SplitWeights)
			{
				totalWeight += weight;
				if (weight < 0.0)
				{
					throw new ArgumentException("Split weights cannot be negative");
				}
			}
			if (totalWeight <= 0.0)
			{
				throw new ArgumentException("Split weights must total to a positive weight");
			}
			IList<double> splitWeights = new List<double>();
			foreach (double weight_1 in SplitWeights)
			{
				splitWeights.Add(weight_1 / totalWeight);
			}
			logger.Info("Splitting into " + splitWeights.Count + " lists with weights " + splitWeights);
			if (Seed == 0L)
			{
				Seed = Runtime.NanoTime();
				logger.Info("Random seed not set by options, using " + Seed);
			}
			Random random = new Random(Seed);
			IList<IList<Tree>> splits = new List<IList<Tree>>();
			foreach (double d in splitWeights)
			{
				splits.Add(new List<Tree>());
			}
			Treebank treebank = new MemoryTreebank(null);
			treebank.LoadPath(Input);
			logger.Info("Splitting " + treebank.Count + " trees");
			foreach (Tree tree in treebank)
			{
				int index = WeightedIndex(splitWeights, random);
				splits[index].Add(tree);
			}
			for (int i = 0; i < splits.Count; ++i)
			{
				string filename = Output + "." + SplitNames[i];
				IList<Tree> split = splits[i];
				logger.Info("Writing " + split.Count + " trees to " + filename);
				FileWriter fout = new FileWriter(filename);
				BufferedWriter bout = new BufferedWriter(fout);
				foreach (Tree tree_1 in split)
				{
					bout.Write(tree_1.ToString());
					bout.NewLine();
				}
				bout.Close();
				fout.Close();
			}
		}
	}
}
