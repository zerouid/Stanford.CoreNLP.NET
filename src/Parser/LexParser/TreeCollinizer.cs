using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// Does detransformations to a parsed sentence to map it back to the
	/// standard treebank form for output or evaluation.
	/// </summary>
	/// <remarks>
	/// Does detransformations to a parsed sentence to map it back to the
	/// standard treebank form for output or evaluation.
	/// This version has Penn-Treebank-English-specific details, but can probably
	/// be used without harm on other treebanks.
	/// Returns labels to their basic category, removes punctuation (should be with
	/// respect to a gold tree, but currently isn't), deletes the boundary symbol,
	/// changes PRT labels to ADVP.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	public class TreeCollinizer : ITreeTransformer
	{
		private readonly ITreebankLanguagePack tlp;

		private readonly bool deletePunct;

		private readonly bool fixCollinsBaseNP;

		/// <summary>
		/// whOption: 0 = do nothing, 1 = also collapse WH phrasal categories in gold tree,
		/// 2 = also collapse WH tags in gold tree,
		/// 4 = attempt to restore WH categories in parse trees (not yet implemented)
		/// </summary>
		private readonly int whOption;

		public TreeCollinizer(ITreebankLanguagePack tlp)
			: this(tlp, true, false)
		{
		}

		public TreeCollinizer(ITreebankLanguagePack tlp, bool deletePunct, bool fixCollinsBaseNP)
			: this(tlp, deletePunct, fixCollinsBaseNP, 0)
		{
		}

		public TreeCollinizer(ITreebankLanguagePack tlp, bool deletePunct, bool fixCollinsBaseNP, int whOption)
		{
			this.tlp = tlp;
			this.deletePunct = deletePunct;
			this.fixCollinsBaseNP = fixCollinsBaseNP;
			this.whOption = whOption;
		}

		public virtual Tree TransformTree(Tree tree)
		{
			if (tree == null)
			{
				return null;
			}
			ITreeFactory tf = tree.TreeFactory();
			string s = tree.Value();
			if (tlp.IsStartSymbol(s))
			{
				return TransformTree(tree.FirstChild());
			}
			if (tree.IsLeaf())
			{
				return tf.NewLeaf(tree.Label());
			}
			s = tlp.BasicCategory(s);
			if (((whOption & 1) != 0) && s.StartsWith("WH"))
			{
				s = Sharpen.Runtime.Substring(s, 2);
			}
			if ((whOption & 2) != 0)
			{
				s = s.ReplaceAll("^WP", "PRP");
				// does both WP and WP$ !!
				s = s.ReplaceAll("^WDT", "DT");
				s = s.ReplaceAll("^WRB", "RB");
			}
			if (((whOption & 4) != 0) && s.StartsWith("WH"))
			{
				s = Sharpen.Runtime.Substring(s, 2);
			}
			// wsg2010: Might need a better way to deal with tag ambiguity. This still doesn't handle the
			// case where the GOLD tree does not label a punctuation mark as such (common in French), and
			// the guess tree does.
			if (deletePunct && tree.IsPreTerminal() && (tlp.IsEvalBIgnoredPunctuationTag(s) || tlp.IsPunctuationWord(tree.FirstChild().Value())))
			{
				return null;
			}
			// remove the extra NPs inserted in the collinsBaseNP option
			if (fixCollinsBaseNP && s.Equals("NP"))
			{
				Tree[] kids = tree.Children();
				if (kids.Length == 1 && tlp.BasicCategory(kids[0].Value()).Equals("NP"))
				{
					return TransformTree(kids[0]);
				}
			}
			// Magerman erased this distinction, and everyone else has followed like sheep...
			if (s.Equals("PRT"))
			{
				s = "ADVP";
			}
			IList<Tree> children = new List<Tree>();
			for (int cNum = 0; cNum < numKids; cNum++)
			{
				Tree child = tree.Children()[cNum];
				Tree newChild = TransformTree(child);
				if (newChild != null)
				{
					children.Add(newChild);
				}
			}
			if (children.IsEmpty())
			{
				return null;
			}
			Tree node = tf.NewTreeNode(tree.Label(), children);
			node.SetValue(s);
			return node;
		}
	}
}
