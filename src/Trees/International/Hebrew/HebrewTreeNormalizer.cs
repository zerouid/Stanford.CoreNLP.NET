using Edu.Stanford.Nlp.Trees;




namespace Edu.Stanford.Nlp.Trees.International.Hebrew
{
	/// <author>Spence Green</author>
	[System.Serializable]
	public class HebrewTreeNormalizer : BobChrisTreeNormalizer
	{
		private const long serialVersionUID = -3129547164200725933L;

		private readonly IPredicate<Tree> hebrewEmptyFilter;

		public HebrewTreeNormalizer()
			: base(new HebrewTreebankLanguagePack())
		{
			hebrewEmptyFilter = new HebrewTreeNormalizer.HebrewEmptyFilter();
		}

		/// <summary>Remove traces and pronoun deletion markers.</summary>
		[System.Serializable]
		public class HebrewEmptyFilter : IPredicate<Tree>
		{
			private const long serialVersionUID = -7256461296718287280L;

			public virtual bool Test(Tree t)
			{
				return !(t.IsPreTerminal() && t.Value().Equals("-NONE-"));
			}
		}

		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			tree = tree.Prune(hebrewEmptyFilter, tf).SpliceOut(aOverAFilter, tf);
			//Add start symbol so that the root has only one sub-state. Escape any enclosing brackets.
			//If the "tree" consists entirely of enclosing brackets e.g. ((())) then this method
			//will return null. In this case, readers e.g. PennTreeReader will try to read the next tree.
			while (tree != null && (tree.Value() == null || tree.Value().Equals(string.Empty)) && tree.NumChildren() <= 1)
			{
				tree = tree.FirstChild();
			}
			if (tree != null && !tree.Value().Equals(tlp.StartSymbol()))
			{
				tree = tf.NewTreeNode(tlp.StartSymbol(), Collections.SingletonList(tree));
			}
			return tree;
		}
	}
}
