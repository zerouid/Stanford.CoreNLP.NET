using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Neural.Rnn
{
	/// <summary>This class stores the best K ngrams for each class for a model.</summary>
	/// <remarks>
	/// This class stores the best K ngrams for each class for a model.  It
	/// does so by keeping priority queues of the best K trees seen,
	/// eliminating duplicates.
	/// <br />
	/// The interface is not too advanced at the moment.  To use, first
	/// create it with the number of classes and the counts of the ngrams,
	/// and then add trees one at a time with countTree.  The results can
	/// be extracted with toString().  There is no way to directly access
	/// the internal results.
	/// </remarks>
	/// <author>John Bauer</author>
	public class TopNGramRecord
	{
		/// <summary>how many ngrams to store for each class</summary>
		private readonly int ngramCount;

		/// <summary>How many classes we are storing</summary>
		private readonly int numClasses;

		/// <summary>Longest ngram to keep</summary>
		private readonly int maximumLength;

		internal IDictionary<int, IDictionary<int, PriorityQueue<Tree>>> classToNGrams = Generics.NewHashMap();

		public TopNGramRecord(int numClasses, int ngramCount, int maximumLength)
		{
			this.numClasses = numClasses;
			this.ngramCount = ngramCount;
			this.maximumLength = maximumLength;
			for (int i = 0; i < numClasses; ++i)
			{
				IDictionary<int, PriorityQueue<Tree>> innerMap = Generics.NewHashMap();
				classToNGrams[i] = innerMap;
			}
		}

		/// <summary>
		/// Adds the tree and all its subtrees to the appropriate
		/// PriorityQueues for each predicted class.
		/// </summary>
		public virtual void CountTree(Tree tree)
		{
			Tree simplified = SimplifyTree(tree);
			for (int i = 0; i < numClasses; ++i)
			{
				CountTreeHelper(simplified, i, classToNGrams[i]);
			}
		}

		/// <summary>Remove everything but the skeleton, the predictions, and the labels</summary>
		private Tree SimplifyTree(Tree tree)
		{
			CoreLabel newLabel = new CoreLabel();
			newLabel.Set(typeof(RNNCoreAnnotations.Predictions), RNNCoreAnnotations.GetPredictions(tree));
			newLabel.SetValue(tree.Label().Value());
			if (tree.IsLeaf())
			{
				return tree.TreeFactory().NewLeaf(newLabel);
			}
			IList<Tree> children = Generics.NewArrayList(tree.Children().Length);
			for (int i = 0; i < tree.Children().Length; ++i)
			{
				children.Add(SimplifyTree(tree.Children()[i]));
			}
			return tree.TreeFactory().NewTreeNode(newLabel, children);
		}

		private int CountTreeHelper(Tree tree, int prediction, IDictionary<int, PriorityQueue<Tree>> ngrams)
		{
			if (tree.IsLeaf())
			{
				return 1;
			}
			int treeSize = 0;
			foreach (Tree child in tree.Children())
			{
				treeSize += CountTreeHelper(child, prediction, ngrams);
			}
			if (maximumLength > 0 && treeSize > maximumLength)
			{
				return treeSize;
			}
			PriorityQueue<Tree> queue = GetPriorityQueue(treeSize, prediction, ngrams);
			// TODO: should we allow classes which aren't the best possible
			// class for this tree to be included in the results?
			if (!queue.Contains(tree))
			{
				queue.Add(tree);
			}
			if (queue.Count > ngramCount)
			{
				queue.Poll();
			}
			return treeSize;
		}

		private PriorityQueue<Tree> GetPriorityQueue(int size, int prediction, IDictionary<int, PriorityQueue<Tree>> ngrams)
		{
			PriorityQueue<Tree> queue = ngrams[size];
			if (queue != null)
			{
				return queue;
			}
			queue = new PriorityQueue<Tree>(ngramCount + 1, ScoreComparator(prediction));
			ngrams[size] = queue;
			return queue;
		}

		private IComparator<Tree> ScoreComparator(int prediction)
		{
			return null;
		}

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			for (int prediction = 0; prediction < numClasses; ++prediction)
			{
				result.Append("Best scores for class " + prediction + "\n");
				IDictionary<int, PriorityQueue<Tree>> ngrams = classToNGrams[prediction];
				foreach (KeyValuePair<int, PriorityQueue<Tree>> entry in ngrams)
				{
					IList<Tree> trees = Generics.NewArrayList(entry.Value);
					trees.Sort(ScoreComparator(prediction));
					result.Append("  Len " + entry.Key + "\n");
					for (int i = trees.Count - 1; i >= 0; i--)
					{
						Tree tree = trees[i];
						result.Append("    " + SentenceUtils.ListToString(tree.Yield()) + "  [" + RNNCoreAnnotations.GetPredictions(tree).Get(prediction) + "]\n");
					}
				}
			}
			return result.ToString();
		}
	}
}
