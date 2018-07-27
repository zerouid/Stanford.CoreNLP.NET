using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Normalizes trees in the way used in Manning and Carpenter 1997.</summary>
	/// <remarks>
	/// Normalizes trees in the way used in Manning and Carpenter 1997.
	/// NB: This implementation is still incomplete!
	/// The normalizations performed are: (i) terminals are interned, (ii)
	/// nonterminals are stripped of alternants, functional tags and
	/// cross-reference codes, and then interned, (iii) empty
	/// elements (ones with nonterminal label "-NONE-") are deleted from the
	/// tree, (iv) the null label at the root node is replaced with the label
	/// "ROOT". <br />
	/// 17 Apr 2001: This was fixed to work with different kinds of labels,
	/// by making proper use of the Label interface, after it was moved into
	/// the trees module.
	/// <p>
	/// The normalizations of the original (Prolog) BobChrisNormalize were:
	/// 1. Remap the root node to be called 'ROOT'
	/// 2. Truncate all nonterminal labels before characters introducing
	/// annotations according to TreebankLanguagePack
	/// (traditionally, -, =, | or # (last for BLLIP))
	/// 3. Remap the representation of certain leaf symbols (brackets etc.)
	/// 4. Map to lowercase all leaf nodes
	/// 5. Delete empty/trace nodes (ones marked '-NONE-')
	/// 6. Recursively delete any nodes that do not dominate any words
	/// 7. Delete A over A nodes where the top A dominates nothing else
	/// 8. Remove backslashes from lexical items
	/// (the Treebank inserts them to escape slashes (/) and stars (*)).
	/// 4 is deliberately omitted, and a few things are purely aesthetic.
	/// <p>
	/// 14 June 2002: It now deletes unary A over A if both nodes' labels are equal
	/// (7), and (6) was always part of the Tree.prune() functionality...
	/// 30 June 2005: Also splice out an EDITED node, just in case you're parsing
	/// the Brown corpus.
	/// </remarks>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class BobChrisTreeNormalizer : TreeNormalizer, ITreeTransformer
	{
		protected internal readonly ITreebankLanguagePack tlp;

		public BobChrisTreeNormalizer()
			: this(new PennTreebankLanguagePack())
		{
		}

		public BobChrisTreeNormalizer(ITreebankLanguagePack tlp)
		{
			this.tlp = tlp;
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
		/// <remarks>
		/// Remove things like hyphened functional tags and equals from the
		/// end of a node label.  This version always just returns the phrase
		/// structure category, or "ROOT" if the label was
		/// <see langword="null"/>
		/// .
		/// </remarks>
		/// <param name="label">The label from the treebank</param>
		/// <returns>The cleaned up label (phrase structure category)</returns>
		protected internal virtual string CleanUpLabel(string label)
		{
			if (label == null || label.IsEmpty())
			{
				return "ROOT";
			}
			else
			{
				// String constants are always interned
				return tlp.BasicCategory(label);
			}
		}

		/// <summary>
		/// Normalize a whole tree -- one can assume that this is the
		/// root.
		/// </summary>
		/// <remarks>
		/// Normalize a whole tree -- one can assume that this is the
		/// root.  This implementation deletes empty elements (ones with
		/// nonterminal tag label '-NONE-') from the tree, and splices out
		/// unary A over A nodes.  It assumes that it is not given a
		/// null tree, but it may return one if there are no real words.
		/// </remarks>
		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			Tree middle = tree.Prune(emptyFilter, tf);
			if (middle == null)
			{
				return null;
			}
			else
			{
				return middle.SpliceOut(aOverAFilter, tf);
			}
		}

		public virtual Tree TransformTree(Tree tree)
		{
			return NormalizeWholeTree(tree, tree.TreeFactory());
		}

		protected internal IPredicate<Tree> emptyFilter = new BobChrisTreeNormalizer.EmptyFilter();

		protected internal IPredicate<Tree> aOverAFilter = new BobChrisTreeNormalizer.AOverAFilter();

		private const long serialVersionUID = -1005188028979810143L;

		[System.Serializable]
		public class EmptyFilter : IPredicate<Tree>
		{
			private const long serialVersionUID = 8914098359495987617L;

			/// <summary>Doesn't accept nodes that only cover an empty.</summary>
			public virtual bool Test(Tree t)
			{
				Tree[] kids = t.Children();
				ILabel l = t.Label();
				// Delete (return false for) empty/trace nodes (ones marked '-NONE-')
				return !((l != null) && "-NONE-".Equals(l.Value()) && !t.IsLeaf() && kids.Length == 1 && kids[0].IsLeaf());
			}
		}

		[System.Serializable]
		public class AOverAFilter : IPredicate<Tree>
		{
			// end class EmptyFilter
			/// <summary>
			/// Doesn't accept nodes that are A over A nodes (perhaps due to
			/// empty removal or are EDITED nodes).
			/// </summary>
			public virtual bool Test(Tree t)
			{
				if (t.IsLeaf() || t.IsPreTerminal())
				{
					return true;
				}
				// The special switchboard non-terminals clause
				if ("EDITED".Equals(t.Label().Value()) || "CODE".Equals(t.Label().Value()))
				{
					return false;
				}
				if (t.NumChildren() != 1)
				{
					return true;
				}
				return !(t.Label() != null && t.Label().Value() != null && t.Label().Value().Equals(t.GetChild(0).Label().Value()));
			}

			private const long serialVersionUID = 1L;
		}
		// end static class AOverAFilter
	}
}
