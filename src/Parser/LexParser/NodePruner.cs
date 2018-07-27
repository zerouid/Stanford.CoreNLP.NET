using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Gets rid of extra NP under NP nodes.</summary>
	/// <author>Dan Klein</author>
	public class NodePruner
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.NodePruner));

		private readonly ExhaustivePCFGParser parser;

		private readonly ITreeTransformer debinarizer;

		internal virtual IList<Tree> Prune(IList<Tree> treeList, ILabel label, int start, int end)
		{
			// get reference tree
			if (treeList.Count == 1)
			{
				return treeList;
			}
			Tree testTree = treeList[0].TreeFactory().NewTreeNode(label, treeList);
			Tree tempTree = parser.ExtractBestParse(label.Value(), start, end);
			// parser.restoreUnaries(tempTree);
			Tree pcfgTree = debinarizer.TransformTree(tempTree);
			ICollection<Constituent> pcfgConstituents = pcfgTree.Constituents(new LabeledScoredConstituentFactory());
			// delete child labels that are not in reference but do not cross reference
			IList<Tree> prunedChildren = new List<Tree>();
			int childStart = 0;
			for (int c = 0; c < numCh; c++)
			{
				Tree child = testTree.GetChild(c);
				bool isExtra = true;
				int childEnd = childStart + child.Yield().Count;
				Constituent childConstituent = new LabeledScoredConstituent(childStart, childEnd, child.Label(), 0);
				if (pcfgConstituents.Contains(childConstituent))
				{
					isExtra = false;
				}
				if (childConstituent.Crosses(pcfgConstituents))
				{
					isExtra = false;
				}
				if (child.IsLeaf() || child.IsPreTerminal())
				{
					isExtra = false;
				}
				if (pcfgTree.Yield().Count != testTree.Yield().Count)
				{
					isExtra = false;
				}
				if (!label.Value().StartsWith("NP^NP"))
				{
					isExtra = false;
				}
				if (isExtra)
				{
					log.Info("Pruning: " + child.Label() + " from " + (childStart + start) + " to " + (childEnd + start));
					log.Info("Was: " + testTree + " vs " + pcfgTree);
					Sharpen.Collections.AddAll(prunedChildren, child.GetChildrenAsList());
				}
				else
				{
					prunedChildren.Add(child);
				}
				childStart = childEnd;
			}
			return prunedChildren;
		}

		private IList<Tree> Helper(IList<Tree> treeList, int start)
		{
			IList<Tree> newTreeList = new List<Tree>(treeList.Count);
			foreach (Tree tree in treeList)
			{
				int end = start + tree.Yield().Count;
				newTreeList.Add(Prune(tree, start));
				start = end;
			}
			return newTreeList;
		}

		public virtual Tree Prune(Tree tree)
		{
			return Prune(tree, 0);
		}

		internal virtual Tree Prune(Tree tree, int start)
		{
			if (tree.IsLeaf() || tree.IsPreTerminal())
			{
				return tree;
			}
			// check each node's children for deletion
			IList<Tree> children = Helper(tree.GetChildrenAsList(), start);
			children = Prune(children, tree.Label(), start, start + tree.Yield().Count);
			return tree.TreeFactory().NewTreeNode(tree.Label(), children);
		}

		public NodePruner(ExhaustivePCFGParser parser, ITreeTransformer debinarizer)
		{
			this.parser = parser;
			this.debinarizer = debinarizer;
		}
	}
}
