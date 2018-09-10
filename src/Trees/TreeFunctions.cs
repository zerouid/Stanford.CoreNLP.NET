using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;





namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This is a utility class which vends tree transformers to translate
	/// trees from one factory type to trees of another.
	/// </summary>
	/// <remarks>
	/// This is a utility class which vends tree transformers to translate
	/// trees from one factory type to trees of another.  For example,
	/// StringLabel trees need to be made into CategoryWordTag trees before
	/// they can be head-percolated.  Enter
	/// LabeledTreeToCategoryWordTagTreeFunction.
	/// </remarks>
	/// <author><a href="mailto:klein@cs.stanford.edu">Dan Klein</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public class TreeFunctions
	{
		private TreeFunctions()
		{
		}

		private class LabeledTreeToStringLabeledTreeFunction : Func<Tree, Tree>
		{
			protected internal ITreeFactory tf = new LabeledScoredTreeFactory();

			public virtual Tree Helper(Tree t)
			{
				if (t == null)
				{
					return null;
				}
				if (t.IsLeaf())
				{
					return tf.NewLeaf(new StringLabel(t.Label().Value()));
				}
				if (t.IsPreTerminal())
				{
					return tf.NewTreeNode(new StringLabel(t.Label().Value()), Collections.SingletonList(Helper(t.Children()[0])));
				}
				int numKids = t.NumChildren();
				IList<Tree> children = new List<Tree>(numKids);
				for (int k = 0; k < numKids; k++)
				{
					children.Add(Helper(t.Children()[k]));
				}
				return tf.NewTreeNode(new StringLabel(t.Label().Value()), children);
			}

			public virtual Tree Apply(Tree t)
			{
				return Helper(t);
			}
		}

		// end static class
		/// <summary>
		/// Return an Function that maps from Label-labeled trees (any
		/// implementing class) to LabeledScored trees with a StringLabel
		/// label.
		/// </summary>
		/// <returns>The Function object</returns>
		public static Func<Tree, Tree> GetLabeledTreeToStringLabeledTreeFunction()
		{
			return new TreeFunctions.LabeledTreeToStringLabeledTreeFunction();
		}

		private class LabeledTreeToCategoryWordTagTreeFunction : Func<Tree, Tree>
		{
			protected internal ITreeFactory tf = new LabeledScoredTreeFactory(new CategoryWordTagFactory());

			public virtual Tree Helper(Tree t)
			{
				if (t == null)
				{
					return null;
				}
				else
				{
					if (t.IsLeaf())
					{
						return tf.NewLeaf(t.Label().Value());
					}
					else
					{
						if (t.IsPreTerminal())
						{
							return tf.NewTreeNode(t.Label().Value(), Java.Util.Collections.SingletonList(Helper(t.Children()[0])));
						}
						else
						{
							int numKids = t.NumChildren();
							IList<Tree> children = new List<Tree>(numKids);
							for (int k = 0; k < numKids; k++)
							{
								children.Add(Helper(t.Children()[k]));
							}
							return tf.NewTreeNode(t.Label().Value(), children);
						}
					}
				}
			}

			public virtual Tree Apply(Tree o)
			{
				return Helper(o);
			}
		}

		// end static class
		/// <summary>
		/// Return a Function that maps from StringLabel labeled trees to
		/// LabeledScoredTrees with a CategoryWordTag label.
		/// </summary>
		/// <returns>The Function object</returns>
		public static Func<Tree, Tree> GetLabeledTreeToCategoryWordTagTreeFunction()
		{
			return new TreeFunctions.LabeledTreeToCategoryWordTagTreeFunction();
		}

		/// <summary>
		/// This function recursively goes through the tree and builds a new
		/// copy with CoreLabels containing the toString() of the original label.
		/// </summary>
		private class LabeledToDescriptiveCoreLabelTreeFunction : Func<Tree, Tree>
		{
			protected internal ITreeFactory tf = new LabeledScoredTreeFactory(CoreLabel.Factory());

			public virtual Tree Apply(Tree t)
			{
				if (t == null)
				{
					return null;
				}
				else
				{
					if (t.IsLeaf())
					{
						return tf.NewLeaf(t.Label().ToString());
					}
					else
					{
						if (t.IsPreTerminal())
						{
							return tf.NewTreeNode(t.Label().ToString(), Java.Util.Collections.SingletonList(Apply(t.Children()[0])));
						}
						else
						{
							int numKids = t.NumChildren();
							IList<Tree> children = new List<Tree>(numKids);
							for (int k = 0; k < numKids; k++)
							{
								children.Add(Apply(t.Children()[k]));
							}
							return tf.NewTreeNode(t.Label().ToString(), children);
						}
					}
				}
			}
		}

		/// <summary>
		/// Returns a function which takes a tree with any label class
		/// where the labels might have an interesting description, such
		/// as a CategoryWordTag which goes "cat [T/W]", and returns a new
		/// tree with CoreLabels which contain the toString() of each of
		/// the input labels.
		/// </summary>
		public static Func<Tree, Tree> GetLabeledToDescriptiveCoreLabelTreeFunction()
		{
			return new TreeFunctions.LabeledToDescriptiveCoreLabelTreeFunction();
		}

		/// <summary>This method just tests the functionality of the included transformers.</summary>
		public static void Main(string[] args)
		{
			//TreeFactory tf = new LabeledScoredTreeFactory();
			Tree stringyTree = null;
			try
			{
				stringyTree = (new PennTreeReader(new StringReader("(S (VP (VBZ Try) (NP (DT this))) (. .))"), new LabeledScoredTreeFactory(new StringLabelFactory()))).ReadTree();
			}
			catch (IOException)
			{
			}
			// do nothing
			System.Console.Out.WriteLine(stringyTree);
			Func<Tree, Tree> a = GetLabeledTreeToCategoryWordTagTreeFunction();
			Tree adaptyTree = a.Apply(stringyTree);
			System.Console.Out.WriteLine(adaptyTree);
			adaptyTree.PercolateHeads(new CollinsHeadFinder());
			System.Console.Out.WriteLine(adaptyTree);
			Func<Tree, Tree> b = GetLabeledTreeToStringLabeledTreeFunction();
			Tree stringLabelTree = b.Apply(adaptyTree);
			System.Console.Out.WriteLine(stringLabelTree);
		}
	}
}
