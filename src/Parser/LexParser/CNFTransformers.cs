using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class CNFTransformers
	{
		private CNFTransformers()
		{
		}

		internal class ToCNFTransformer : ITreeTransformer
		{
			public virtual Tree TransformTree(Tree t)
			{
				if (t.IsLeaf())
				{
					return t.TreeFactory().NewLeaf(t.Label());
				}
				Tree[] children = t.Children();
				if (children.Length > 1 || t.IsPreTerminal() || t.Label().Value().StartsWith("ROOT"))
				{
					ILabel label = t.Label();
					Tree[] transformedChildren = new Tree[children.Length];
					for (int childIndex = 0; childIndex < children.Length; childIndex++)
					{
						Tree child = children[childIndex];
						transformedChildren[childIndex] = TransformTree(child);
					}
					return t.TreeFactory().NewTreeNode(label, Arrays.AsList(transformedChildren));
				}
				Tree tree = t;
				IList<string> conjoinedList = new List<string>();
				while (tree.Children().Length == 1 && !tree.IsPrePreTerminal())
				{
					string nodeString = tree.Label().Value();
					if (!nodeString.StartsWith("@"))
					{
						conjoinedList.Add(nodeString);
					}
					tree = tree.Children()[0];
				}
				string nodeString_1 = tree.Label().Value();
				if (!nodeString_1.StartsWith("@"))
				{
					conjoinedList.Add(nodeString_1);
				}
				string conjoinedLabels;
				if (conjoinedList.Count > 1)
				{
					StringBuilder conjoinedLabelsBuilder = new StringBuilder();
					foreach (string s in conjoinedList)
					{
						conjoinedLabelsBuilder.Append("&");
						conjoinedLabelsBuilder.Append(s);
					}
					conjoinedLabels = conjoinedLabelsBuilder.ToString();
				}
				else
				{
					if (conjoinedList.Count == 1)
					{
						conjoinedLabels = conjoinedList.GetEnumerator().Current;
					}
					else
					{
						return TransformTree(t.Children()[0]);
					}
				}
				children = tree.Children();
				ILabel label_1 = t.Label().LabelFactory().NewLabel(conjoinedLabels);
				Tree[] transformedChildren_1 = new Tree[children.Length];
				for (int childIndex_1 = 0; childIndex_1 < children.Length; childIndex_1++)
				{
					Tree child = children[childIndex_1];
					transformedChildren_1[childIndex_1] = TransformTree(child);
				}
				return t.TreeFactory().NewTreeNode(label_1, Arrays.AsList(transformedChildren_1));
			}
		}

		internal class FromCNFTransformer : ITreeTransformer
		{
			public virtual Tree TransformTree(Tree t)
			{
				if (t.IsLeaf())
				{
					return t.TreeFactory().NewLeaf(t.Label());
				}
				Tree[] children = t.Children();
				Tree[] transformedChildren = new Tree[children.Length];
				for (int childIndex = 0; childIndex < children.Length; childIndex++)
				{
					Tree child = children[childIndex];
					transformedChildren[childIndex] = TransformTree(child);
				}
				ILabel label = t.Label();
				if (!label.Value().StartsWith("&"))
				{
					return t.TreeFactory().NewTreeNode(label, Arrays.AsList(transformedChildren));
				}
				string[] nodeStrings = label.Value().Split("&");
				int i = nodeStrings.Length - 1;
				label = t.Label().LabelFactory().NewLabel(nodeStrings[i]);
				Tree result = t.TreeFactory().NewTreeNode(label, Arrays.AsList(transformedChildren));
				while (i > 1)
				{
					i--;
					label = t.Label().LabelFactory().NewLabel(nodeStrings[i]);
					result = t.TreeFactory().NewTreeNode(label, Java.Util.Collections.SingletonList(result));
				}
				return result;
			}
		}

		public static void Main(string[] args)
		{
			CategoryWordTag.printWordTag = false;
			string path = args[0];
			IList<Tree> trees = TreebankAnnotator.GetTrees(path, 200, 219, 0, 10);
			IList<Tree> annotatedTrees = new TreebankAnnotator(new Options(), path).AnnotateTrees(trees);
			foreach (Tree tree in annotatedTrees)
			{
				System.Console.Out.WriteLine("ORIGINAL:\n");
				tree.PennPrint();
				System.Console.Out.WriteLine("CNFed:\n");
				Tree cnfTree = new CNFTransformers.ToCNFTransformer().TransformTree(tree);
				cnfTree.PennPrint();
				System.Console.Out.WriteLine("UnCNFed:\n");
				Tree unCNFTree = new CNFTransformers.FromCNFTransformer().TransformTree(cnfTree);
				unCNFTree.PennPrint();
				System.Console.Out.WriteLine("\n\n");
			}
		}
	}
}
