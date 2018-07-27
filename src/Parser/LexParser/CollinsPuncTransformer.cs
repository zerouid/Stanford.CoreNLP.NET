using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// This class manipulates punctuation in trees (used with training trees)
	/// in the same manner that Collins manipulated punctuation in trees when
	/// building his parsing model.
	/// </summary>
	/// <remarks>
	/// This class manipulates punctuation in trees (used with training trees)
	/// in the same manner that Collins manipulated punctuation in trees when
	/// building his parsing model.  This is the same punctuation that is
	/// the punctuation ignored in the standard EvalB evaluation is promoted
	/// as high in the tree as possible.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	public class CollinsPuncTransformer : ITreeTransformer
	{
		private ITreebankLanguagePack tlp;

		internal virtual bool IsPunc(Tree t)
		{
			if (t.IsPreTerminal())
			{
				string s = t.Label().Value();
				if (tlp.IsEvalBIgnoredPunctuationTag(s))
				{
					return true;
				}
			}
			return false;
		}

		internal static LinkedList<Tree> PreTerms(Tree t)
		{
			LinkedList<Tree> l = new LinkedList<Tree>();
			PreTermHelper(t, l);
			return l;
		}

		internal static void PreTermHelper(Tree t, IList<Tree> l)
		{
			if (t.IsLeaf())
			{
				return;
			}
			if (t.IsPreTerminal())
			{
				l.Add(t);
				return;
			}
			Tree[] children = t.Children();
			foreach (Tree child in children)
			{
				PreTermHelper(child, l);
			}
		}

		internal virtual Tree TransformRoot(Tree tree, ITreeFactory tf)
		{
			// XXXX TODO: use tlp and don't assume 1 daughter of ROOT!
			// leave the root intact
			// if (tlp.isStartSymbol(tlp.basicCategory(tree.label().value())))
			if (tree.Label().ToString().StartsWith("ROOT"))
			{
				return tf.NewTreeNode(tree.Label(), Java.Util.Collections.SingletonList(TransformNode(tree.Children()[0], tf)));
			}
			return TransformNode(tree, tf);
		}

		internal virtual Tree TransformNode(Tree tree, ITreeFactory tf)
		{
			if (tree.IsLeaf())
			{
				return tf.NewLeaf(tree.Label());
			}
			if (tree.IsPreTerminal())
			{
				return tf.NewTreeNode(tree.Label(), Java.Util.Collections.SingletonList(tf.NewLeaf(tree.Children()[0].Label())));
			}
			IList<Tree> children = tree.GetChildrenAsList();
			LinkedList<Tree> newChildren = new LinkedList<Tree>();
			// promote lower punctuation
			foreach (Tree child in children)
			{
				LinkedList<Tree> preTerms = PreTerms(child);
				while (!preTerms.IsEmpty() && IsPunc(preTerms.GetFirst()))
				{
					newChildren.Add(preTerms.GetFirst());
					preTerms.RemoveFirst();
				}
				Tree newChild = TransformNode(child, tf);
				LinkedList<Tree> temp = new LinkedList<Tree>();
				if (newChild.Children().Length > 0)
				{
					newChildren.Add(newChild);
				}
				while (!preTerms.IsEmpty() && IsPunc(preTerms.GetLast()))
				{
					temp.AddFirst(preTerms.GetLast());
					preTerms.RemoveLast();
				}
				Sharpen.Collections.AddAll(newChildren, temp);
			}
			// remove local punctuation
			while (!newChildren.IsEmpty() && IsPunc(newChildren.GetFirst()))
			{
				newChildren.RemoveFirst();
			}
			while (!newChildren.IsEmpty() && IsPunc(newChildren.GetLast()))
			{
				newChildren.RemoveLast();
			}
			return tf.NewTreeNode(tree.Label(), newChildren);
		}

		//   public Tree transformTree(Tree tree) {
		//     //System.out.println("PUNCTUATION TRANSFORM:");
		//     //tree.pennPrint();
		//     //System.out.println("BECOMES:");
		//     //transformRoot(tree, tf).pennPrint();
		//     return transformRoot(tree, tf);
		//   }
		public virtual Tree TransformTree(Tree tree)
		{
			return TransformRoot(tree, tree.TreeFactory());
		}

		public CollinsPuncTransformer(ITreebankLanguagePack tlp)
		{
			this.tlp = tlp;
		}
	}
}
