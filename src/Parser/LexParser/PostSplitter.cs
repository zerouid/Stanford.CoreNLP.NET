using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// This class splits on parents using the same algorithm as the earlier
	/// parent (selective) annotation algorithms, but applied AFTER the tree
	/// has been annotated.
	/// </summary>
	/// <author>Christopher Manning</author>
	internal class PostSplitter : ITreeTransformer
	{
		private readonly ClassicCounter<string> nonTerms = new ClassicCounter<string>();

		private readonly ITreebankLangParserParams tlpParams;

		private readonly IHeadFinder hf;

		private readonly TrainOptions trainOptions;

		public virtual Tree TransformTree(Tree t)
		{
			ITreeFactory tf = t.TreeFactory();
			return TransformTreeHelper(t, t, tf);
		}

		public virtual Tree TransformTreeHelper(Tree t, Tree root, ITreeFactory tf)
		{
			Tree result;
			Tree parent;
			string parentStr;
			string grandParentStr;
			if (root == null || t.Equals(root))
			{
				parent = null;
				parentStr = string.Empty;
			}
			else
			{
				parent = t.Parent(root);
				parentStr = parent.Label().Value();
			}
			if (parent == null || parent.Equals(root))
			{
				grandParentStr = string.Empty;
			}
			else
			{
				Tree grandParent = parent.Parent(root);
				grandParentStr = grandParent.Label().Value();
			}
			string cat = t.Label().Value();
			string baseParentStr = tlpParams.TreebankLanguagePack().BasicCategory(parentStr);
			string baseGrandParentStr = tlpParams.TreebankLanguagePack().BasicCategory(grandParentStr);
			if (t.IsLeaf())
			{
				return tf.NewLeaf(new Word(t.Label().Value()));
			}
			string word = t.HeadTerminal(hf).Value();
			if (t.IsPreTerminal())
			{
				nonTerms.IncrementCount(t.Label().Value());
			}
			else
			{
				nonTerms.IncrementCount(t.Label().Value());
				if (trainOptions.postPA && !trainOptions.smoothing && baseParentStr.Length > 0)
				{
					string cat2;
					if (trainOptions.postSplitWithBaseCategory)
					{
						cat2 = cat + '^' + baseParentStr;
					}
					else
					{
						cat2 = cat + '^' + parentStr;
					}
					if (!trainOptions.selectivePostSplit || trainOptions.postSplitters.Contains(cat2))
					{
						cat = cat2;
					}
				}
				if (trainOptions.postGPA && !trainOptions.smoothing && grandParentStr.Length > 0)
				{
					string cat2;
					if (trainOptions.postSplitWithBaseCategory)
					{
						cat2 = cat + '~' + baseGrandParentStr;
					}
					else
					{
						cat2 = cat + '~' + grandParentStr;
					}
					if (trainOptions.selectivePostSplit)
					{
						if (cat.Contains("^") && trainOptions.postSplitters.Contains(cat2))
						{
							cat = cat2;
						}
					}
					else
					{
						cat = cat2;
					}
				}
			}
			result = tf.NewTreeNode(new CategoryWordTag(cat, word, cat), Collections.EmptyList<Tree>());
			List<Tree> newKids = new List<Tree>();
			Tree[] kids = t.Children();
			foreach (Tree kid in kids)
			{
				newKids.Add(TransformTreeHelper(kid, root, tf));
			}
			result.SetChildren(newKids);
			return result;
		}

		public virtual void DumpStats()
		{
			System.Console.Out.WriteLine("%% Counts of nonterminals:");
			IList<string> biggestCounts = new List<string>(nonTerms.KeySet());
			biggestCounts.Sort(Counters.ToComparatorDescending(nonTerms));
			foreach (string str in biggestCounts)
			{
				System.Console.Out.WriteLine(str + ": " + nonTerms.GetCount(str));
			}
		}

		public PostSplitter(ITreebankLangParserParams tlpParams, Options op)
		{
			this.tlpParams = tlpParams;
			this.hf = tlpParams.HeadFinder();
			this.trainOptions = op.trainOptions;
		}
	}
}
