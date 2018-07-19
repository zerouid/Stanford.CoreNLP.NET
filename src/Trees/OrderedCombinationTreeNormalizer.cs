using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>This class combines multiple tree normalizers.</summary>
	/// <remarks>
	/// This class combines multiple tree normalizers.  Given a list of tree normalizer,
	/// it applies each tree normalizer from the first to the last for each of the normalize
	/// nonterminal, normalize terminal, and normalize whole tree methods.
	/// </remarks>
	/// <author>Anna Rafferty</author>
	[System.Serializable]
	public class OrderedCombinationTreeNormalizer : TreeNormalizer
	{
		private const long serialVersionUID = 326L;

		private IList<TreeNormalizer> tns = new List<TreeNormalizer>();

		public OrderedCombinationTreeNormalizer()
		{
		}

		public OrderedCombinationTreeNormalizer(IList<TreeNormalizer> tns)
		{
			this.tns = tns;
		}

		/// <summary>
		/// Adds the given tree normalizer to this combination; the tree normalizers
		/// are applied in the order they were added, with the first to be added being
		/// the first to be applied.
		/// </summary>
		public virtual void AddTreeNormalizer(TreeNormalizer tn)
		{
			this.tns.Add(tn);
		}

		public override string NormalizeNonterminal(string category)
		{
			foreach (TreeNormalizer tn in tns)
			{
				category = tn.NormalizeNonterminal(category);
			}
			return category;
		}

		public override string NormalizeTerminal(string leaf)
		{
			foreach (TreeNormalizer tn in tns)
			{
				leaf = tn.NormalizeTerminal(leaf);
			}
			return leaf;
		}

		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			foreach (TreeNormalizer tn in tns)
			{
				tree = tn.NormalizeWholeTree(tree, tf);
			}
			return tree;
		}
	}
}
