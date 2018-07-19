using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class NegraPennCollinizer : ITreeTransformer
	{
		/// <summary>A logger for this class</summary>
		internal Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.NegraPennCollinizer));

		private ITreebankLangParserParams tlpp;

		private readonly bool deletePunct;

		public NegraPennCollinizer(ITreebankLangParserParams tlpp)
			: this(tlpp, true)
		{
		}

		public NegraPennCollinizer(ITreebankLangParserParams tlpp, bool deletePunct)
		{
			this.tlpp = tlpp;
			this.deletePunct = deletePunct;
		}

		protected internal ITreeFactory tf = new LabeledScoredTreeFactory();

		public virtual Tree TransformTree(Tree tree)
		{
			ILabel l = tree.Label();
			if (tree.IsLeaf())
			{
				return tf.NewLeaf(l);
			}
			string s = l.Value();
			s = tlpp.TreebankLanguagePack().BasicCategory(s);
			if (deletePunct)
			{
				// this is broken as it's not the right thing to do when there
				// is any tag ambiguity -- and there is for ' (POS/'').  Sentences
				// can then have more or less words.  It's also unnecessary for EVALB,
				// since it ignores punctuation anyway
				if (tree.IsPreTerminal() && tlpp.TreebankLanguagePack().IsEvalBIgnoredPunctuationTag(s))
				{
					return null;
				}
			}
			// TEMPORARY: eliminate the TOPP constituent
			if (tree.Children()[0].Label().Value().Equals("TOPP"))
			{
				log.Info("Found a TOPP");
				tree.SetChildren(tree.Children()[0].Children());
			}
			// Negra has lots of non-unary roots; delete unary roots
			if (tlpp.TreebankLanguagePack().IsStartSymbol(s) && tree.NumChildren() == 1)
			{
				// NB: This deletes the boundary symbol, which is in the tree!
				return TransformTree(tree.GetChild(0));
			}
			IList<Tree> children = new List<Tree>();
			for (int cNum = 0; cNum < numC; cNum++)
			{
				Tree child = tree.GetChild(cNum);
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
			return tf.NewTreeNode(new StringLabel(s), children);
		}
	}
}
