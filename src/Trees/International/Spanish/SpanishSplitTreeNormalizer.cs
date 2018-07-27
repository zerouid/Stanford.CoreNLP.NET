using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;


namespace Edu.Stanford.Nlp.Trees.International.Spanish
{
	/// <summary>
	/// A tree normalizer made to be used immediately on trees which have
	/// been split apart.
	/// </summary>
	/// <remarks>
	/// A tree normalizer made to be used immediately on trees which have
	/// been split apart.
	/// This is used in AnCora processing in order to fix some common
	/// problems with splitting multi-sentence trees.
	/// </remarks>
	/// <author>Jon Gauthier</author>
	[System.Serializable]
	public class SpanishSplitTreeNormalizer : SpanishTreeNormalizer
	{
		private static readonly TregexPattern nonsensicalClauseRewrite = TregexPattern.Compile("sentence=sentence < (S=S !$ /^[^f]/)");

		private static readonly TsurgeonPattern eraseClause = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("excise S S");

		private const long serialVersionUID = -3237606914912983720L;

		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			tree = base.NormalizeWholeTree(tree, tf);
			tree = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(nonsensicalClauseRewrite, eraseClause, tree);
			return tree;
		}
	}
}
