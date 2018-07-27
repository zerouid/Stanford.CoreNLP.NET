using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Trees.International.Tuebadz
{
	/// <summary>Tree normalizer for the TueBaDZ treebank.</summary>
	/// <remarks>
	/// Tree normalizer for the TueBaDZ treebank.
	/// (An adaptation of Roger Levy's NegraPennTreeNormalizer.)
	/// </remarks>
	/// <author>Wolfgang Maier (wmaier@sfs.uni-tuebingen.de)</author>
	[System.Serializable]
	public class TueBaDZPennTreeNormalizer : TreeNormalizer
	{
		/// <summary>
		/// How to clean up node labels: 0 = do nothing, 1 = keep category and
		/// function, 2 = just category.
		/// </summary>
		private readonly int nodeCleanup;

		private readonly string root;

		protected internal readonly ITreebankLanguagePack tlp;

		private IList<TreeNormalizer> tns = new List<TreeNormalizer>();

		public virtual string RootSymbol()
		{
			return root;
		}

		public TueBaDZPennTreeNormalizer(ITreebankLanguagePack tlp, int nodeCleanup)
		{
			//  public TueBaDZPennTreeNormalizer() {
			//    this(new TueBaDZLanguagePack(), 0);
			//  }
			this.tlp = tlp;
			this.nodeCleanup = nodeCleanup;
			root = tlp.StartSymbol();
		}

		public TueBaDZPennTreeNormalizer(ITreebankLanguagePack tlp, int nodeCleanup, IList<TreeNormalizer> tns)
		{
			this.tlp = tlp;
			this.nodeCleanup = nodeCleanup;
			root = tlp.StartSymbol();
			Sharpen.Collections.AddAll(this.tns, tns);
		}

		/// <summary>Normalizes a leaf contents.</summary>
		/// <remarks>
		/// Normalizes a leaf contents.
		/// This implementation interns the leaf.
		/// </remarks>
		public override string NormalizeTerminal(string leaf)
		{
			// We could unquote * and / with backslash \ in front of them
			return string.Intern(leaf);
		}

		/// <summary>Normalizes a nonterminal contents.</summary>
		/// <remarks>
		/// Normalizes a nonterminal contents.
		/// This implementation strips functional tags, etc. and interns the
		/// nonterminal.
		/// </remarks>
		public override string NormalizeNonterminal(string category)
		{
			return string.Intern(CleanUpLabel(category));
		}

		/// <summary>
		/// Remove things like hyphened functional tags and equals from the
		/// end of a node label.
		/// </summary>
		protected internal virtual string CleanUpLabel(string label)
		{
			if (label == null)
			{
				return root;
			}
			else
			{
				if (nodeCleanup == 1)
				{
					return tlp.CategoryAndFunction(label);
				}
				else
				{
					if (nodeCleanup == 2)
					{
						return tlp.BasicCategory(label);
					}
					else
					{
						return label;
					}
				}
			}
		}

		/// <summary>Normalize a whole tree.</summary>
		/// <remarks>
		/// Normalize a whole tree.
		/// TueBa-D/Z adaptation. Fixes trees with non-unary roots, does nothing else.
		/// </remarks>
		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			if (tree.Label().Value().Equals(root) && tree.Children().Length > 1)
			{
				Tree underRoot = tree.TreeFactory().NewTreeNode(root, tree.GetChildrenAsList());
				tree.SetChildren(new Tree[1]);
				tree.SetChild(0, underRoot);
			}
			// we just want the non-unary root fixed.
			return tree;
		}

		private const long serialVersionUID = 8009544230321390490L;
	}
}
