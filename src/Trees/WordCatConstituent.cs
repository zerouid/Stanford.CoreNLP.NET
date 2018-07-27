

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A class storing information about a constituent in a character-based tree.</summary>
	/// <remarks>
	/// A class storing information about a constituent in a character-based tree.
	/// This is used for evaluation of character-based Chinese parsing.
	/// The constituent can be of type "word" (for words), "cat" (for phrases) or "tag" (for POS).
	/// </remarks>
	/// <author>Galen Andrew</author>
	public class WordCatConstituent : LabeledConstituent
	{
		public string type;

		public const string wordType = "word";

		public const string tagType = "tag";

		public const string catType = "cat";

		public const string goodWordTagType = "goodWordTag";

		public WordCatConstituent(Tree subTree, Tree root, string type)
		{
			// this one is for POS tag of correctly segmented words only
			SetStart(Edu.Stanford.Nlp.Trees.Trees.LeftEdge(subTree, root));
			SetEnd(Edu.Stanford.Nlp.Trees.Trees.RightEdge(subTree, root));
			SetFromString(subTree.Value());
			this.type = type;
		}
	}
}
