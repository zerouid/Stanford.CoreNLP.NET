using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>An abstract superclass for parser classes that extract counts from Trees.</summary>
	/// <author>grenager</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) - cleanup and filling in types</author>
	public abstract class AbstractTreeExtractor<T> : IExtractor<T>
	{
		protected internal readonly Options op;

		protected internal AbstractTreeExtractor(Options op)
		{
			this.op = op;
		}

		protected internal virtual void TallyLeaf(Tree lt, double weight)
		{
		}

		protected internal virtual void TallyPreTerminal(Tree lt, double weight)
		{
		}

		protected internal virtual void TallyInternalNode(Tree lt, double weight)
		{
		}

		protected internal virtual void TallyRoot(Tree lt, double weight)
		{
		}

		public virtual T FormResult()
		{
			return null;
		}

		protected internal virtual void TallyLocalTree(Tree lt, double weight)
		{
			// printTrainTree(null, "Tallying local tree:", lt);
			if (lt.IsLeaf())
			{
				//      System.out.println("it's a leaf");
				TallyLeaf(lt, weight);
			}
			else
			{
				if (lt.IsPreTerminal())
				{
					//      System.out.println("it's a preterminal");
					TallyPreTerminal(lt, weight);
				}
				else
				{
					//      System.out.println("it's a internal node");
					TallyInternalNode(lt, weight);
				}
			}
		}

		public virtual void TallyTree(Tree t, double weight)
		{
			TallyRoot(t, weight);
			foreach (Tree localTree in t.SubTreeList())
			{
				TallyLocalTree(localTree, weight);
			}
		}

		protected internal virtual void TallyTrees(ICollection<Tree> trees, double weight)
		{
			foreach (Tree tree in trees)
			{
				TallyTree(tree, weight);
			}
		}

		protected internal virtual void TallyTreeIterator(IEnumerator<Tree> treeIterator, IFunction<Tree, Tree> f, double weight)
		{
			while (treeIterator.MoveNext())
			{
				Tree tree = treeIterator.Current;
				try
				{
					tree = f.Apply(tree);
				}
				catch (Exception e)
				{
					if (op.testOptions.verbose)
					{
						Sharpen.Runtime.PrintStackTrace(e);
					}
				}
				TallyTree(tree, weight);
			}
		}

		public virtual T Extract()
		{
			return FormResult();
		}

		public virtual T Extract(ICollection<Tree> treeList)
		{
			TallyTrees(treeList, 1.0);
			return FormResult();
		}

		public virtual T Extract(ICollection<Tree> trees1, double weight1, ICollection<Tree> trees2, double weight2)
		{
			TallyTrees(trees1, weight1);
			TallyTrees(trees2, weight2);
			return FormResult();
		}

		public virtual T Extract(IEnumerator<Tree> treeIterator, IFunction<Tree, Tree> f, double weight)
		{
			TallyTreeIterator(treeIterator, f, weight);
			return FormResult();
		}

		public virtual T Extract(IEnumerator<Tree> iterator, IFunction<Tree, Tree> f)
		{
			return Extract(iterator, f, 1.0);
		}
	}
}
