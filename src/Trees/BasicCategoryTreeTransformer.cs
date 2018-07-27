using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Transforms trees by turning the labels into their basic categories
	/// according to the
	/// <see cref="ITreebankLanguagePack"/>
	/// </summary>
	/// <author>John Bauer</author>
	public class BasicCategoryTreeTransformer : RecursiveTreeTransformer, IFunction<Tree, Tree>
	{
		internal readonly ITreebankLanguagePack tlp;

		public BasicCategoryTreeTransformer(ITreebankLanguagePack tlp)
		{
			this.tlp = tlp;
		}

		public override ILabel TransformNonterminalLabel(Tree tree)
		{
			if (tree.Label() == null)
			{
				return null;
			}
			return tree.Label().LabelFactory().NewLabel(tlp.BasicCategory(tree.Label().Value()));
		}

		public override Tree Apply(Tree tree)
		{
			return TransformTree(tree);
		}
	}
}
