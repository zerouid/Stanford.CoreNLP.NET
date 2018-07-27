using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Trees.International.Negra
{
	/// <summary>Tree normalizer for Negra Penn Treebank format.</summary>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public class NegraPennTreeNormalizer : TreeNormalizer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Negra.NegraPennTreeNormalizer));

		/// <summary>
		/// How to clean up node labels: 0 = do nothing, 1 = keep category and
		/// function, 2 = just category
		/// </summary>
		private readonly int nodeCleanup;

		private const string nonUnaryRoot = "NUR";

		protected internal readonly ITreebankLanguagePack tlp;

		private bool insertNPinPP = false;

		private readonly IPredicate<Tree> emptyFilter;

		private readonly IPredicate<Tree> aOverAFilter;

		public NegraPennTreeNormalizer()
			: this(new NegraPennLanguagePack())
		{
		}

		public NegraPennTreeNormalizer(ITreebankLanguagePack tlp)
			: this(tlp, 0)
		{
		}

		public NegraPennTreeNormalizer(ITreebankLanguagePack tlp, int nodeCleanup)
		{
			// non-unary root
			this.tlp = tlp;
			this.nodeCleanup = nodeCleanup;
			emptyFilter = new _IPredicate_46();
			aOverAFilter = new _IPredicate_56();
		}

		private sealed class _IPredicate_46 : IPredicate<Tree>
		{
			public _IPredicate_46()
			{
				this.serialVersionUID = -606371737889816130L;
			}

			public bool Test(Tree t)
			{
				Tree[] kids = t.Children();
				ILabel l = t.Label();
				if ((l != null) && l.Value() != null && (l.Value().Matches("^\\*T.*$")) && !t.IsLeaf() && kids.Length == 1 && kids[0].IsLeaf())
				{
					return false;
				}
				return true;
			}
		}

		private sealed class _IPredicate_56 : IPredicate<Tree>
		{
			public _IPredicate_56()
			{
				this.serialVersionUID = -606371737889816130L;
			}

			public bool Test(Tree t)
			{
				if (t.IsLeaf() || t.IsPreTerminal() || t.Children().Length != 1)
				{
					return true;
				}
				if (t.Label() != null && t.Label().Equals(t.Children()[0].Label()))
				{
					return false;
				}
				return true;
			}
		}

		public virtual string RootSymbol()
		{
			return tlp.StartSymbol();
		}

		public virtual string NonUnaryRootSymbol()
		{
			return nonUnaryRoot;
		}

		public virtual void SetInsertNPinPP(bool b)
		{
			insertNPinPP = b;
		}

		public virtual bool GetInsertNPinPP()
		{
			return insertNPinPP;
		}

		/// <summary>Normalizes a leaf contents.</summary>
		/// <remarks>
		/// Normalizes a leaf contents.
		/// This implementation interns the leaf.
		/// </remarks>
		public override string NormalizeTerminal(string leaf)
		{
			return string.Intern(leaf);
		}

		private const string junkCPP = "---CJ";

		private const string cpp = "CPP";

		/// <summary>Normalizes a nonterminal contents.</summary>
		/// <remarks>
		/// Normalizes a nonterminal contents.
		/// This implementation strips functional tags, etc. and interns the
		/// nonterminal.
		/// </remarks>
		public override string NormalizeNonterminal(string category)
		{
			if (junkCPP.Equals(category))
			{
				// one garbage category cleanup here.
				category = cpp;
			}
			//Accommodate the null root nodes in Negra/Tiger trees
			category = CleanUpLabel(category);
			return (category == null) ? null : string.Intern(category);
		}

		private Tree FixNonUnaryRoot(Tree t, ITreeFactory tf)
		{
			IList<Tree> kids = t.GetChildrenAsList();
			if (kids.Count == 2 && t.FirstChild().IsPhrasal() && tlp.IsSentenceFinalPunctuationTag(t.LastChild().Value()))
			{
				IList<Tree> grandKids = t.FirstChild().GetChildrenAsList();
				grandKids.Add(t.LastChild());
				t.FirstChild().SetChildren(grandKids);
				kids.Remove(kids.Count - 1);
				t.SetChildren(kids);
				t.SetValue(tlp.StartSymbol());
			}
			else
			{
				t.SetValue(nonUnaryRoot);
				t = tf.NewTreeNode(tlp.StartSymbol(), Java.Util.Collections.SingletonList(t));
			}
			return t;
		}

		/// <summary>
		/// Normalize a whole tree -- one can assume that this is the
		/// root.
		/// </summary>
		/// <remarks>
		/// Normalize a whole tree -- one can assume that this is the
		/// root.  This implementation deletes empty elements (ones with
		/// nonterminal tag label starting with '*T') from the tree.  It
		/// does work for a null tree.
		/// </remarks>
		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			// add an extra root to non-unary roots
			if (tree.Value() == null)
			{
				tree = FixNonUnaryRoot(tree, tf);
			}
			else
			{
				if (!tree.Value().Equals(tlp.StartSymbol()))
				{
					tree = tf.NewTreeNode(tlp.StartSymbol(), Java.Util.Collections.SingletonList(tree));
				}
			}
			tree = tree.Prune(emptyFilter, tf).SpliceOut(aOverAFilter, tf);
			// insert NPs in PPs if you're supposed to do that
			if (insertNPinPP)
			{
				InsertNPinPPall(tree);
			}
			foreach (Tree t in tree)
			{
				if (t.IsLeaf() || t.IsPreTerminal())
				{
					continue;
				}
				if (t.Value() == null || t.Value().Equals(string.Empty))
				{
					t.SetValue("DUMMY");
				}
				// there's also a '--' category
				if (t.Value().Matches("--.*"))
				{
					continue;
				}
				// fix a bug in the ACL08 German tiger treebank
				string cat = t.Value();
				if (cat == null || cat.Equals(string.Empty))
				{
					if (t.NumChildren() == 3 && t.FirstChild().Label().Value().Equals("NN") && t.GetChild(1).Label().Value().Equals("$."))
					{
						log.Info("Correcting treebank error: giving phrase label DL to " + t);
						t.Label().SetValue("DL");
					}
				}
			}
			return tree;
		}

		private ICollection<string> prepositionTags = Generics.NewHashSet(Arrays.AsList(new string[] { "APPR", "APPRART" }));

		private ICollection<string> postpositionTags = Generics.NewHashSet(Arrays.AsList(new string[] { "APPO", "APZR" }));

		private void InsertNPinPPall(Tree t)
		{
			Tree[] kids = t.Children();
			foreach (Tree kid in kids)
			{
				InsertNPinPPall(kid);
			}
			InsertNPinPP(t);
		}

		private void InsertNPinPP(Tree t)
		{
			if (tlp.BasicCategory(t.Label().Value()).Equals("PP"))
			{
				Tree[] kids = t.Children();
				int i = 0;
				int j = kids.Length - 1;
				while (i < j && prepositionTags.Contains(tlp.BasicCategory(kids[i].Label().Value())))
				{
					i++;
				}
				// i now indexes first dtr of new NP
				while (i < j && postpositionTags.Contains(tlp.BasicCategory(kids[j].Label().Value())))
				{
					j--;
				}
				// j now indexes last dtr of new NP
				if (i > j)
				{
					log.Info("##### Warning -- no NP material here!");
					return;
				}
				// there is no NP material!
				int npKidsLength = j - i + 1;
				Tree[] npKids = new Tree[npKidsLength];
				System.Array.Copy(kids, i, npKids, 0, npKidsLength);
				Tree np = t.TreeFactory().NewTreeNode(t.Label().LabelFactory().NewLabel("NP"), Arrays.AsList(npKids));
				Tree[] newPPkids = new Tree[kids.Length - npKidsLength + 1];
				System.Array.Copy(kids, 0, newPPkids, 0, i + 1);
				newPPkids[i] = np;
				System.Array.Copy(kids, j + 1, newPPkids, i + 1, kids.Length - j - 1);
				t.SetChildren(newPPkids);
				System.Console.Out.WriteLine("#### inserted NP in PP");
				t.PennPrint();
			}
		}

		/// <summary>
		/// Remove things like hyphened functional tags and equals from the
		/// end of a node label.
		/// </summary>
		protected internal virtual string CleanUpLabel(string label)
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
			}
			return label;
		}

		private const long serialVersionUID = 8529514903815041064L;
	}
}
