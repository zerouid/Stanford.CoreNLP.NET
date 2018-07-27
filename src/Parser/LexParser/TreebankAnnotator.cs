using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Class for getting an annotated treebank.</summary>
	/// <author>Dan Klein</author>
	public class TreebankAnnotator
	{
		internal readonly ITreeTransformer treeTransformer;

		internal readonly ITreeTransformer treeUnTransformer;

		internal readonly ITreeTransformer collinizer;

		internal readonly Options op;

		// todo [cdm 2014]: This class is all but dead. Delete it.
		public virtual IList<Tree> AnnotateTrees(IList<Tree> trees)
		{
			IList<Tree> annotatedTrees = new List<Tree>();
			foreach (Tree tree in trees)
			{
				annotatedTrees.Add(treeTransformer.TransformTree(tree));
			}
			return annotatedTrees;
		}

		public virtual IList<Tree> DeannotateTrees(IList<Tree> trees)
		{
			IList<Tree> deannotatedTrees = new List<Tree>();
			foreach (Tree tree in trees)
			{
				deannotatedTrees.Add(treeUnTransformer.TransformTree(tree));
			}
			return deannotatedTrees;
		}

		public static IList<Tree> GetTrees(string path, int low, int high, int minLength, int maxLength)
		{
			Treebank treebank = new DiskTreebank(null);
			treebank.LoadPath(path, new NumberRangeFileFilter(low, high, true));
			IList<Tree> trees = new List<Tree>();
			foreach (Tree tree in treebank)
			{
				if (tree.Yield().Count <= maxLength && tree.Yield().Count >= minLength)
				{
					trees.Add(tree);
				}
			}
			return trees;
		}

		public static IList<Tree> RemoveDependencyRoots(IList<Tree> trees)
		{
			IList<Tree> prunedTrees = new List<Tree>();
			foreach (Tree tree in trees)
			{
				prunedTrees.Add(RemoveDependencyRoot(tree));
			}
			return prunedTrees;
		}

		internal static Tree RemoveDependencyRoot(Tree tree)
		{
			IList<Tree> childList = tree.GetChildrenAsList();
			Tree last = childList[childList.Count - 1];
			if (!last.Label().Value().Equals(LexiconConstants.BoundaryTag))
			{
				return tree;
			}
			IList<Tree> lastGoneList = childList.SubList(0, childList.Count - 1);
			tree.SetChildren(lastGoneList);
			return tree;
		}

		public virtual Tree Collinize(Tree tree)
		{
			return collinizer.TransformTree(tree);
		}

		public TreebankAnnotator(Options op, string treebankRoot)
		{
			//    op.tlpParams = new EnglishTreebankParserParams();
			// CDM: Aug 2004: With new implementation of treebank split categories,
			// I've hardwired this to load English ones.  Otherwise need training data.
			// op.trainOptions.splitters = Generics.newHashSet(Arrays.asList(op.tlpParams.splitters()));
			op.trainOptions.splitters = ParentAnnotationStats.GetEnglishSplitCategories(treebankRoot);
			op.trainOptions.sisterSplitters = Generics.NewHashSet(Arrays.AsList(op.tlpParams.SisterSplitters()));
			op.SetOptions("-acl03pcfg", "-cnf");
			treeTransformer = new TreeAnnotatorAndBinarizer(op.tlpParams, op.forceCNF, !op.trainOptions.OutsideFactor(), true, op);
			//    BinarizerFactory.TreeAnnotator.setTreebankLang(op.tlpParams);
			treeUnTransformer = new Debinarizer(op.forceCNF);
			collinizer = op.tlpParams.Collinizer();
			this.op = op;
		}

		public static void Main(string[] args)
		{
			CategoryWordTag.printWordTag = false;
			string path = args[0];
			IList<Tree> trees = GetTrees(path, 200, 219, 0, 10);
			trees.GetEnumerator().Current.PennPrint();
			Options op = new Options();
			IList<Tree> annotatedTrees = Edu.Stanford.Nlp.Parser.Lexparser.TreebankAnnotator.RemoveDependencyRoots(new Edu.Stanford.Nlp.Parser.Lexparser.TreebankAnnotator(op, path).AnnotateTrees(trees));
			annotatedTrees.GetEnumerator().Current.PennPrint();
		}
	}
}
