using System;
using Edu.Stanford.Nlp.International.French;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.French
{
	/// <summary>Prepares French Treebank trees for parsing.</summary>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class FrenchTreeNormalizer : BobChrisTreeNormalizer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.French.FrenchTreeNormalizer));

		private const long serialVersionUID = 7868735300308066991L;

		private readonly string rootLabel;

		private readonly MorphoFeatureSpecification morpho = new FrenchMorphoFeatureSpecification();

		private readonly bool ccTagset;

		public FrenchTreeNormalizer(bool ccTagset)
			: base(new FrenchTreebankLanguagePack())
		{
			rootLabel = tlp.StartSymbol();
			this.ccTagset = ccTagset;
			aOverAFilter = new FrenchTreeNormalizer.FrenchAOverAFilter();
			emptyFilter = new _IPredicate_45();
		}

		private sealed class _IPredicate_45 : IPredicate<Tree>
		{
			public _IPredicate_45()
			{
				this.serialVersionUID = -22673346831392110L;
			}

			private const long serialVersionUID;

			public bool Test(Tree tree)
			{
				if (tree.IsPreTerminal() && (tree.FirstChild().Value().Equals(string.Empty) || tree.FirstChild().Value().Equals("-NONE-")))
				{
					return false;
				}
				return true;
			}
		}

		public override string NormalizeTerminal(string terminal)
		{
			if (terminal == null)
			{
				return terminal;
			}
			// PTB escaping
			if (terminal.Equals(")"))
			{
				return "-RRB-";
			}
			else
			{
				if (terminal.Equals("("))
				{
					return "-LRB-";
				}
			}
			return string.Intern(base.NormalizeTerminal(terminal));
		}

		public override string NormalizeNonterminal(string category)
		{
			return string.Intern(base.NormalizeNonterminal(category));
		}

		private static void ReplacePOSTag(Tree t, MorphoFeatureSpecification morpho)
		{
			if (!t.IsPreTerminal())
			{
				throw new ArgumentException("Can only operate on preterminals");
			}
			if (!(t.Label() is CoreLabel))
			{
				throw new ArgumentException("Only operates on CoreLabels");
			}
			CoreLabel label = (CoreLabel)t.Label();
			Tree child = t.Children()[0];
			if (!(child.Label() is CoreLabel))
			{
				throw new ArgumentException("Only operates on CoreLabels");
			}
			CoreLabel childLabel = (CoreLabel)child.Label();
			// Morphological Analysis
			string morphStr = childLabel.OriginalText();
			if (morphStr == null || morphStr.Equals(string.Empty))
			{
				morphStr = label.Value();
				// POS subcategory
				string subCat = childLabel.Category();
				if (subCat != null && subCat != string.Empty)
				{
					morphStr += "-" + subCat + "--";
				}
				else
				{
					morphStr += "---";
				}
			}
			MorphoFeatures feats = morpho.StrToFeatures(morphStr);
			if (feats.GetAltTag() != null && !feats.GetAltTag().Equals(string.Empty))
			{
				label.SetValue(feats.GetAltTag());
				label.SetTag(feats.GetAltTag());
			}
		}

		/// <summary>Sets POS for punctuation to the punctuation token (like the PTB).</summary>
		/// <param name="t"/>
		private string NormalizePreterminal(Tree t)
		{
			if (ccTagset)
			{
				ReplacePOSTag(t, morpho);
			}
			if (tlp.IsPunctuationWord(t.FirstChild().Value()))
			{
				return string.Intern(tlp.PunctuationTags()[0]);
			}
			//Map to a common tag
			//      return t.firstChild().value();//Map to the punctuation item
			return t.Value();
		}

		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			tree = tree.Prune(emptyFilter, tf).SpliceOut(aOverAFilter, tf);
			foreach (Tree t in tree)
			{
				//Map punctuation tags back like the PTB
				if (t.IsPreTerminal())
				{
					string posStr = NormalizePreterminal(t);
					t.SetValue(posStr);
					if (t.Label() is IHasTag)
					{
						((IHasTag)t.Label()).SetTag(posStr);
					}
				}
				else
				{
					if (t.IsLeaf())
					{
						//Strip off morphological analyses and place them in the OriginalTextAnnotation, which is
						//specified by HasContext.
						if (t.Value().Contains(MorphoFeatureSpecification.MorphoMark))
						{
							string[] toks = t.Value().Split(MorphoFeatureSpecification.MorphoMark);
							if (toks.Length != 2)
							{
								System.Console.Error.Printf("%s: Word contains malformed morph annotation: %s%n", this.GetType().FullName, t.Value());
							}
							else
							{
								if (t.Label() is CoreLabel)
								{
									((CoreLabel)t.Label()).SetValue(string.Intern(toks[0].Trim()));
									((CoreLabel)t.Label()).SetWord(string.Intern(toks[0].Trim()));
									((CoreLabel)t.Label()).SetOriginalText(string.Intern(toks[1].Trim()));
								}
								else
								{
									System.Console.Error.Printf("%s: Cannot store morph analysis in non-CoreLabel: %s%n", this.GetType().FullName, t.Label().GetType().FullName);
								}
							}
						}
					}
				}
			}
			//Add start symbol so that the root has only one sub-state. Escape any enclosing brackets.
			//If the "tree" consists entirely of enclosing brackets e.g. ((())) then this method
			//will return null. In this case, readers e.g. PennTreeReader will try to read the next tree.
			while (tree != null && (tree.Value() == null || tree.Value().Equals(string.Empty)) && tree.NumChildren() <= 1)
			{
				tree = tree.FirstChild();
			}
			//Ensure that the tree has a top-level unary rewrite
			if (tree != null && !tree.Value().Equals(rootLabel))
			{
				tree = tf.NewTreeNode(rootLabel, Collections.SingletonList(tree));
			}
			return tree;
		}

		[System.Serializable]
		public class FrenchAOverAFilter : IPredicate<Tree>
		{
			private const long serialVersionUID = 793800623099852951L;

			/// <summary>
			/// Doesn't accept nodes that are A over A nodes (perhaps due to
			/// empty removal or are EDITED nodes).
			/// </summary>
			/// <remarks>
			/// Doesn't accept nodes that are A over A nodes (perhaps due to
			/// empty removal or are EDITED nodes).
			/// Also removes all w nodes.
			/// </remarks>
			public virtual bool Test(Tree t)
			{
				if (t.Value() != null && t.Value().Equals("w"))
				{
					return false;
				}
				if (t.IsLeaf() || t.IsPreTerminal())
				{
					return true;
				}
				return !(t.Label() != null && t.Label().Value() != null && t.Label().Value().Equals(t.GetChild(0).Label().Value()));
			}
		}
	}
}
