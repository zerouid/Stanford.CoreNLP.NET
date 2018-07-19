using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Neural.Rnn;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Sentiment
{
	/// <summary>
	/// In the Sentiment dataset converted to tree form, the labels on the
	/// intermediate nodes are the sentiment scores and the leaves are the
	/// text of the sentence.
	/// </summary>
	/// <remarks>
	/// In the Sentiment dataset converted to tree form, the labels on the
	/// intermediate nodes are the sentiment scores and the leaves are the
	/// text of the sentence.  This class provides routines to read a file
	/// of those trees and attach the sentiment score as the GoldLabel
	/// annotation.
	/// </remarks>
	/// <author>John Bauer</author>
	public class SentimentUtils
	{
		private SentimentUtils()
		{
		}

		// static methods only
		public static void AttachLabels(Tree tree, Type annotationClass)
		{
			if (tree.IsLeaf())
			{
				return;
			}
			foreach (Tree child in tree.Children())
			{
				AttachLabels(child, annotationClass);
			}
			// In the sentiment data set, the node labels are simply the gold
			// class labels.  There are no categories encoded.
			int numericLabel = int.Parse(tree.Label().Value());
			ILabel label = tree.Label();
			if (!(label is CoreLabel))
			{
				throw new ArgumentException("CoreLabels required!");
			}
			((CoreLabel)label).Set(annotationClass, numericLabel);
		}

		/// <summary>Given a file name, reads in those trees and returns them as a List</summary>
		public static IList<Tree> ReadTreesWithGoldLabels(string path)
		{
			return ReadTreesWithLabels(path, typeof(RNNCoreAnnotations.GoldClass));
		}

		/// <summary>
		/// Given a file name, reads in those trees and returns them as list with
		/// labels attached as predictions
		/// </summary>
		public static IList<Tree> ReadTreesWithPredictedLabels(string path)
		{
			return ReadTreesWithLabels(path, typeof(RNNCoreAnnotations.PredictedClass));
		}

		/// <summary>Given a file name, reads in those trees and returns them as a List</summary>
		public static IList<Tree> ReadTreesWithLabels(string path, Type annotationClass)
		{
			IList<Tree> trees = Generics.NewArrayList();
			MemoryTreebank treebank = new MemoryTreebank("utf-8");
			treebank.LoadPath(path, null);
			foreach (Tree tree in treebank)
			{
				AttachLabels(tree, annotationClass);
				trees.Add(tree);
			}
			return trees;
		}

		internal static readonly IPredicate<Tree> UnknownRootFilter = null;

		public static IList<Tree> FilterUnknownRoots(IList<Tree> trees)
		{
			return CollectionUtils.FilterAsList(trees, UnknownRootFilter);
		}

		public static string SentimentString(SentimentModel model, int sentiment)
		{
			string[] classNames = model.op.classNames;
			if (sentiment < 0 || sentiment > classNames.Length)
			{
				return "Unknown sentiment label " + sentiment;
			}
			return classNames[sentiment];
		}
	}
}
