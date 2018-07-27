using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>
	/// Performs collinization operations on Chinese trees similar to
	/// those for English Namely: <ul>
	/// <li> strips all functional &amp; automatically-added tags
	/// <li> strips all punctuation
	/// <li> merges PRN and ADVP
	/// <li> eliminates ROOT (note that there are a few non-unary ROOT nodes;
	/// these are not eliminated)
	/// </ul>
	/// </summary>
	/// <author>Roger Levy</author>
	/// <author>Christopher Manning</author>
	public class ChineseCollinizer : ITreeTransformer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Pennchinese.ChineseCollinizer));

		private const bool Verbose = false;

		private readonly bool deletePunct;

		internal ChineseTreebankLanguagePack ctlp;

		protected internal ITreeFactory tf = new LabeledScoredTreeFactory();

		public ChineseCollinizer(ChineseTreebankLanguagePack ctlp)
			: this(ctlp, true)
		{
		}

		public ChineseCollinizer(ChineseTreebankLanguagePack ctlp, bool deletePunct)
		{
			this.deletePunct = deletePunct;
			this.ctlp = ctlp;
		}

		public virtual Tree TransformTree(Tree tree)
		{
			return TransformTree(tree, true);
		}

		private Tree TransformTree(Tree tree, bool isRoot)
		{
			string label = tree.Label().Value();
			// log.info("ChineseCollinizer: Node label is " + label);
			if (tree.IsLeaf())
			{
				if (deletePunct && ctlp.IsPunctuationWord(label))
				{
					return null;
				}
				else
				{
					return tf.NewLeaf(new StringLabel(label));
				}
			}
			if (tree.IsPreTerminal() && deletePunct && ctlp.IsPunctuationTag(label))
			{
				// System.out.println("Deleting punctuation");
				return null;
			}
			IList<Tree> children = new List<Tree>();
			if (label.Matches("ROOT.*") && tree.NumChildren() == 1)
			{
				// keep non-unary roots for now
				return TransformTree(tree.Children()[0], true);
			}
			//System.out.println("Enhanced label is " + label);
			// remove all functional and machine-generated annotations
			label = label.ReplaceFirst("[^A-Z].*$", string.Empty);
			// merge parentheticals with adverb phrases
			label = label.ReplaceFirst("PRN", "ADVP");
			//System.out.println("New label is " + label);
			for (int cNum = 0; cNum < tree.Children().Length; cNum++)
			{
				Tree child = tree.Children()[cNum];
				Tree newChild = TransformTree(child, false);
				if (newChild != null)
				{
					children.Add(newChild);
				}
			}
			// We don't delete the root because there are trees in the
			// Chinese treebank that only have punctuation in them!!!
			if (children.IsEmpty() && !isRoot)
			{
				return null;
			}
			return tf.NewTreeNode(new StringLabel(label), children);
		}
	}
}
