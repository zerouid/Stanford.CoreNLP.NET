using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>This was originally written to correct a few errors Galen found in CTB3.</summary>
	/// <remarks>
	/// This was originally written to correct a few errors Galen found in CTB3.
	/// The thinking was that perhaps when we get CTB4 they would be gone and we
	/// could revert to BobChris.  Alas, CTB4 contained only more errors....
	/// It has since been extended to allow some functional tags from CTB to be
	/// maintained.  This is so far much easier than in NPTmpRetainingTN, since
	/// we don't do any tag percolation (helped by CTB marking temporal nouns).
	/// <p>
	/// <i>Implementation note:</i> This now loads CharacterLevelTagExtender by
	/// reflection if that option is invoked.
	/// </remarks>
	/// <author>Galen Andrew</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class CTBErrorCorrectingTreeNormalizer : BobChrisTreeNormalizer
	{
		private const long serialVersionUID = -8203853817025401845L;

		private static readonly Pattern NPTmpPattern = Pattern.Compile("NP.*-TMP.*");

		private static readonly Pattern PPTmpPattern = Pattern.Compile("PP.*-TMP.*");

		private static readonly Pattern TmpPattern = Pattern.Compile(".*-TMP.*");

		private static readonly bool Debug = Runtime.GetProperty("CTBErrorCorrectingTreeNormalizer") != null;

		private readonly ITreeTransformer tagExtender;

		private readonly bool splitNPTMP;

		private readonly bool splitPPTMP;

		private readonly bool splitXPTMP;

		/// <summary>Constructor with all of the options of the other constructor false</summary>
		public CTBErrorCorrectingTreeNormalizer()
			: this(false, false, false, false)
		{
		}

		/// <summary>Build a CTBErrorCorrectingTreeNormalizer.</summary>
		/// <param name="splitNPTMP">Temporal annotation on NPs</param>
		/// <param name="splitPPTMP">Temporal annotation on PPs</param>
		/// <param name="splitXPTMP">Temporal annotation on any phrase marked in CTB</param>
		/// <param name="charTags">
		/// Whether you wish to push POS tags down on to the
		/// characters of a word (for unsegmented text)
		/// </param>
		public CTBErrorCorrectingTreeNormalizer(bool splitNPTMP, bool splitPPTMP, bool splitXPTMP, bool charTags)
		{
			this.splitNPTMP = splitNPTMP;
			this.splitPPTMP = splitPPTMP;
			this.splitXPTMP = splitXPTMP;
			if (charTags)
			{
				try
				{
					tagExtender = (ITreeTransformer)System.Activator.CreateInstance(Sharpen.Runtime.GetType("edu.stanford.nlp.trees.international.pennchinese.CharacterLevelTagExtender"));
				}
				catch (Exception e)
				{
					throw new Exception(e);
				}
			}
			else
			{
				tagExtender = null;
			}
		}

		/// <summary>
		/// Remove things like hyphened functional tags and equals from the
		/// end of a node label.
		/// </summary>
		/// <remarks>
		/// Remove things like hyphened functional tags and equals from the
		/// end of a node label.  But keep occasional functional tags as
		/// determined by class parameters, particularly TMP
		/// </remarks>
		/// <param name="label">The label to be cleaned up</param>
		protected internal override string CleanUpLabel(string label)
		{
			if (label == null)
			{
				return "ROOT";
			}
			else
			{
				bool nptemp = NPTmpPattern.Matcher(label).Matches();
				bool pptemp = PPTmpPattern.Matcher(label).Matches();
				bool anytemp = TmpPattern.Matcher(label).Matches();
				label = tlp.BasicCategory(label);
				if (anytemp && splitXPTMP)
				{
					label += "-TMP";
				}
				else
				{
					if (pptemp && splitPPTMP)
					{
						label = label + "-TMP";
					}
					else
					{
						if (nptemp && splitNPTMP)
						{
							label = label + "-TMP";
						}
					}
				}
				return label;
			}
		}

		[System.Serializable]
		private class ChineseEmptyFilter : IPredicate<Tree>
		{
			private const long serialVersionUID = 8914098359495987617L;

			/// <summary>Doesn't accept nodes that only cover an empty.</summary>
			public virtual bool Test(Tree t)
			{
				Tree[] kids = t.Children();
				ILabel l = t.Label();
				if ((l != null) && l.Value() != null && (l.Value().Matches("-NONE-.*")) && !t.IsLeaf() && kids.Length == 1 && kids[0].IsLeaf())
				{
					// there appears to be a mistake in CTB3 where the label "-NONE-1" is used once
					// presumably it should be "-NONE-" and be spliced out here.
					// Delete empty/trace nodes (ones marked '-NONE-')
					if (!l.Value().Equals("-NONE-"))
					{
						EncodingPrintWriter.Err.Println("Deleting errant node " + l.Value() + " as if -NONE-: " + t, ChineseTreebankLanguagePack.Encoding);
					}
					return false;
				}
				return true;
			}
		}

		private readonly IPredicate<Tree> chineseEmptyFilter = new CTBErrorCorrectingTreeNormalizer.ChineseEmptyFilter();

		private static readonly TregexPattern[] fixupTregex = new TregexPattern[] { TregexPattern.Compile("PU=punc < 她｛"), TregexPattern.Compile("@NP <1 (@NP <1 NR <2 (PU=bad < /^＜$/)) <2 (FLR=dest <2 (NT < /Ｅｎｇｌｉｓｈ/))"), TregexPattern.Compile("@IP < (FLR=dest <: (PU < /^〈$/) $. (__=bad1 $. (PU=bad2 < /^〉$/)))"
			), TregexPattern.Compile("@DFL|FLR|IMG|SKIP=junk <<, (PU < /^[〈｛{＜\\[［]$/) <<- (PU < /^[〉｝}＞\\]］]$/)  <3 __"), TregexPattern.Compile("WHPP=bad") };

		private static readonly TsurgeonPattern[] fixupTsurgeon = new TsurgeonPattern[] { Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace punc (PN 她) (PU ｛)"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("move bad >1 dest"
			), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[move bad1 >-1 dest] [move bad2 >-1 dest]"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("delete junk"), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation
			("relabel bad PP") };

		static CTBErrorCorrectingTreeNormalizer()
		{
			if (fixupTregex.Length != fixupTsurgeon.Length)
			{
				throw new AssertionError("fixupTregex and fixupTsurgeon have different lengths in CTBErrorCorrectingTreeNormalizer.");
			}
		}

		// We delete the most egregious non-speech DFL, FLR, IMG, and SKIP constituents, according to the Tregex
		// expression above. Maybe more should be deleted really. I don't understand this very well, and there is no documentation.
		// New phrasal categories in CTB 7 and later:
		// DFL = Disfluency. Generally keep but delete for ones that are things like (FLR (PU <) (VV turn) (PU >)).
		// EMO = Emoticon. For emoticons. Fine to keep.
		// FLR = Filler.  Generally keep but delete for ones that are things like (FLR (PU <) (VV turn) (PU >)).
		// IMG = ?Image?. Appear to all be of form (IMG (PU [) (NN 图片) (PU ])). Delete all those.
		// INC = Incomplete (more incomplete than a FRAG which is only syntactically incomplete). Just keep.
		// INTJ = Interjection. Fine to keep.
		// META = Just one of these in chtb_5200.df. Delete whole tree. Should have been turned into XML metadata
		// OTH = ??. Weird but just leave.
		// SKIP = ??. Always has NOI under it. Omit or keep?
		// TYPO = seems like should mainly go, but sometimes a branching node??
		// WHPP = ??. Just one of these. Over a -NONE- so will go if empties are deleted. But should just be PP.
		//
		// There is a tree in chtb_2856.bn which has IP -> ... PU (FLR (PU <)) (VV turn) (PU >)
		// which just seems an error - should all be under FLR.
		//
		// POS tags are now 38. Original 33 plus these:
		// EM = Emoticon. Often but not always under EMO.
		// IC = Incomplete word rendered in pinyin, usually under DFL.
		// NOI =
		// URL = URL.
		// X = In practice currently used only for "x" in constructions like "30 x 25 cm". Shouldn't exist!
		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			Tree newTree = tree.Prune(chineseEmptyFilter, tf).SpliceOut(aOverAFilter);
			// Report non-unary initial rewrites & fix 'obvious ones'
			Tree[] kids = newTree.Children();
			if (kids.Length > 1)
			{
				/* -------------- don't do this as probably shouldn't for test set (and doesn't help anyway)
				if (kids.length == 2 &&
				"PU".equals(kids[kids.length - 1].value()) &&
				kids[0].isPhrasal()) {
				printlnErr("Correcting error: non-unary initial rewrite fixed by tucking punctuation inside constituent: " + newTree.localTree());
				List kidkids = kids[0].getChildrenAsList();
				kidkids.add(kids[1]);
				Tree bigger = tf.newTreeNode(kids[0].label(), kidkids);
				newTree = tf.newTreeNode(newTree.label(), Collections.singletonList(bigger));
				} else {
				-------------------- */
				EncodingPrintWriter.Err.Println("Possible error: non-unary initial rewrite: " + newTree.LocalTree(), ChineseTreebankLanguagePack.Encoding);
			}
			else
			{
				// }
				if (kids.Length > 0)
				{
					// ROOT has 1 child - the normal case
					Tree child = kids[0];
					if (!child.IsPhrasal())
					{
						if (Debug)
						{
							EncodingPrintWriter.Err.Println("Correcting error: treebank tree is not phrasal; wrapping in FRAG: " + child, ChineseTreebankLanguagePack.Encoding);
						}
						Tree added = tf.NewTreeNode("FRAG", Arrays.AsList(kids));
						newTree.SetChild(0, added);
					}
					else
					{
						if (child.Label().Value().Equals("META"))
						{
							// Delete the one bogus META tree in CTB 9
							EncodingPrintWriter.Err.Println("Deleting META tree that should be XML metadata in chtb_5200.df: " + child, ChineseTreebankLanguagePack.Encoding);
							return null;
						}
					}
				}
				else
				{
					EncodingPrintWriter.Err.Println("Error: tree with no children: " + tree, ChineseTreebankLanguagePack.Encoding);
				}
			}
			// note that there's also at least 1 tree that is an IP with no surrounding ROOT node
			// there are also several places where "NP" is used as a preterminal tag
			// and presumably should be "NN"
			// a couple of other random errors are corrected here
			foreach (Tree subtree in newTree)
			{
				if (subtree.Value().Equals("CP") && subtree.NumChildren() == 1)
				{
					Tree subsubtree = subtree.FirstChild();
					if (subsubtree.Value().Equals("ROOT"))
					{
						if (subsubtree.FirstChild().IsLeaf() && "CP".Equals(subsubtree.FirstChild().Value()))
						{
							EncodingPrintWriter.Err.Println("Correcting error: seriously messed up tree in CTB6 (chtb_3095.bn): " + newTree, ChineseTreebankLanguagePack.Encoding);
							IList<Tree> children = subsubtree.GetChildrenAsList();
							children = children.SubList(1, children.Count);
							subtree.SetChildren(children);
							EncodingPrintWriter.Err.Println("  Corrected as:                                                    " + newTree, ChineseTreebankLanguagePack.Encoding);
						}
					}
				}
				// spaced to align with above
				// All the stuff below here seems to have been fixed in CTB 9. Maybe reporting errors sometimes does help.
				if (subtree.IsPreTerminal())
				{
					if (subtree.Value().Matches("NP"))
					{
						if (ChineseTreebankLanguagePack.ChineseDouHaoAcceptFilter().Test(subtree.FirstChild().Value()))
						{
							if (Debug)
							{
								EncodingPrintWriter.Err.Println("Correcting error: NP preterminal over douhao; preterminal changed to PU: " + subtree, ChineseTreebankLanguagePack.Encoding);
							}
							subtree.SetValue("PU");
						}
						else
						{
							if (subtree.Parent(newTree).Value().Matches("NP"))
							{
								if (Debug)
								{
									EncodingPrintWriter.Err.Println("Correcting error: NP preterminal w/ NP parent; preterminal changed to NN: " + subtree.Parent(newTree), ChineseTreebankLanguagePack.Encoding);
								}
								subtree.SetValue("NN");
							}
							else
							{
								if (Debug)
								{
									EncodingPrintWriter.Err.Println("Correcting error: NP preterminal w/o NP parent, changing preterminal to NN: " + subtree.Parent(newTree), ChineseTreebankLanguagePack.Encoding);
								}
								// Tree newChild = tf.newTreeNode("NN", Collections.singletonList(subtree.firstChild()));
								// subtree.setChildren(Collections.singletonList(newChild));
								subtree.SetValue("NN");
							}
						}
					}
					else
					{
						if (subtree.Value().Matches("PU"))
						{
							if (subtree.FirstChild().Value().Matches("他"))
							{
								if (Debug)
								{
									EncodingPrintWriter.Err.Println("Correcting error: \"他\" under PU tag; tag changed to PN: " + subtree, ChineseTreebankLanguagePack.Encoding);
								}
								subtree.SetValue("PN");
							}
							else
							{
								if (subtree.FirstChild().Value().Equals("里"))
								{
									if (Debug)
									{
										EncodingPrintWriter.Err.Println("Correcting error: \"" + subtree.FirstChild().Value() + "\" under PU tag; tag changed to LC: " + subtree, ChineseTreebankLanguagePack.Encoding);
									}
									subtree.SetValue("LC");
								}
								else
								{
									if (subtree.FirstChild().Value().Equals("是"))
									{
										if (Debug)
										{
											EncodingPrintWriter.Err.Println("Correcting error: \"" + subtree.FirstChild().Value() + "\" under PU tag; tag changed to VC: " + subtree, ChineseTreebankLanguagePack.Encoding);
										}
										subtree.SetValue("VC");
									}
									else
									{
										if (subtree.FirstChild().Value().Matches("tw|半穴式"))
										{
											if (Debug)
											{
												EncodingPrintWriter.Err.Println("Correcting error: \"" + subtree.FirstChild().Value() + "\" under PU tag; tag changed to NN: " + subtree, ChineseTreebankLanguagePack.Encoding);
											}
											subtree.SetValue("NN");
										}
										else
										{
											if (subtree.FirstChild().Value().Matches("33"))
											{
												if (Debug)
												{
													EncodingPrintWriter.Err.Println("Correcting error: \"33\" under PU tag; tag changed to CD: " + subtree, ChineseTreebankLanguagePack.Encoding);
												}
												subtree.SetValue("CD");
											}
										}
									}
								}
							}
						}
					}
				}
				else
				{
					if (subtree.Value().Matches("NN"))
					{
						if (Debug)
						{
							EncodingPrintWriter.Err.Println("Correcting error: NN phrasal tag changed to NP: " + subtree, ChineseTreebankLanguagePack.Encoding);
						}
						subtree.SetValue("NP");
					}
					else
					{
						if (subtree.Value().Matches("MSP"))
						{
							if (Debug)
							{
								EncodingPrintWriter.Err.Println("Correcting error: MSP phrasal tag changed to VP: " + subtree, ChineseTreebankLanguagePack.Encoding);
							}
							subtree.SetValue("VP");
						}
					}
				}
			}
			for (int i = 0; i < fixupTregex.Length; ++i)
			{
				if (Debug)
				{
					Tree preProcessed = newTree.DeepCopy();
					newTree = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(fixupTregex[i], fixupTsurgeon[i], newTree);
					if (!preProcessed.Equals(newTree))
					{
						EncodingPrintWriter.Err.Println("Correcting error: Updated tree using tregex " + fixupTregex[i] + " and tsurgeon " + fixupTsurgeon[i], ChineseTreebankLanguagePack.Encoding);
						EncodingPrintWriter.Err.Println("  from: " + preProcessed, ChineseTreebankLanguagePack.Encoding);
						EncodingPrintWriter.Err.Println("    to: " + newTree, ChineseTreebankLanguagePack.Encoding);
					}
				}
				else
				{
					newTree = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(fixupTregex[i], fixupTsurgeon[i], newTree);
				}
			}
			// at least once we just end up deleting everything under ROOT. In which case, we should just get rid of the tree.
			if (newTree.NumChildren() == 0)
			{
				if (Debug)
				{
					EncodingPrintWriter.Err.Println("Deleting tree that now has no contents: " + newTree, ChineseTreebankLanguagePack.Encoding);
				}
				return null;
			}
			if (tagExtender != null)
			{
				newTree = tagExtender.TransformTree(newTree);
			}
			return newTree;
		}

		/// <summary>So you can create a TreeReaderFactory using this TreeNormalizer easily by reflection.</summary>
		public class CTBErrorCorrectingTreeReaderFactory : CTBTreeReaderFactory
		{
			public CTBErrorCorrectingTreeReaderFactory()
				: base(new CTBErrorCorrectingTreeNormalizer(false, false, false, false))
			{
			}
		}
		// end class CTBErrorCorrectingTreeReaderFactory
	}
}
