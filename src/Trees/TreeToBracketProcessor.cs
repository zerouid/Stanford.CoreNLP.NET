using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <author>Galen Andrew</author>
	public class TreeToBracketProcessor
	{
		private readonly IList evalTypes;

		private static readonly CharacterLevelTagExtender ext = new CharacterLevelTagExtender();

		public TreeToBracketProcessor(IList evalTypes)
		{
			this.evalTypes = evalTypes;
		}

		public virtual ICollection AllBrackets(Tree root)
		{
			bool words = evalTypes.Contains(WordCatConstituent.wordType);
			bool tags = evalTypes.Contains(WordCatConstituent.tagType);
			bool cats = evalTypes.Contains(WordCatConstituent.catType);
			IList<WordCatConstituent> brackets = new List<WordCatConstituent>();
			if (words || cats || tags)
			{
				root = ext.TransformTree(root);
				foreach (Tree tree in root)
				{
					if (tree.IsPrePreTerminal() && !tree.Value().Equals("ROOT"))
					{
						if (words)
						{
							brackets.Add(new WordCatConstituent(tree, root, WordCatConstituent.wordType));
						}
						if (tags)
						{
							brackets.Add(new WordCatConstituent(tree, root, WordCatConstituent.tagType));
						}
					}
					else
					{
						if (cats && tree.IsPhrasal() && !tree.Value().Equals("ROOT"))
						{
							brackets.Add(new WordCatConstituent(tree, root, WordCatConstituent.catType));
						}
					}
				}
			}
			return brackets;
		}

		public static ICollection CommonWordTagTypeBrackets(Tree root1, Tree root2)
		{
			root1 = ext.TransformTree(root1);
			root2 = ext.TransformTree(root2);
			IList<Tree> firstPreTerms = new List<Tree>();
			foreach (Tree tree in root1)
			{
				if (tree.IsPrePreTerminal())
				{
					firstPreTerms.Add(tree);
				}
			}
			IList<WordCatConstituent> brackets = new List<WordCatConstituent>();
			foreach (Tree preTerm in firstPreTerms)
			{
				foreach (Tree tree_1 in root2)
				{
					if (!tree_1.IsPrePreTerminal())
					{
						continue;
					}
					if (Edu.Stanford.Nlp.Trees.Trees.LeftEdge(tree_1, root2) == Edu.Stanford.Nlp.Trees.Trees.LeftEdge(preTerm, root1) && Edu.Stanford.Nlp.Trees.Trees.RightEdge(tree_1, root2) == Edu.Stanford.Nlp.Trees.Trees.RightEdge(preTerm, root1))
					{
						brackets.Add(new WordCatConstituent(preTerm, root1, WordCatConstituent.goodWordTagType));
						break;
					}
				}
			}
			return brackets;
		}
	}
}
