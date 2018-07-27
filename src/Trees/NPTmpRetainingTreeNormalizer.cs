using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Same TreeNormalizer as BobChrisTreeNormalizer, but optionally provides
	/// four extras.
	/// </summary>
	/// <remarks>
	/// Same TreeNormalizer as BobChrisTreeNormalizer, but optionally provides
	/// four extras.  I.e., the class name is now a misnomer.<br />
	/// 1) retains -TMP labels on NP with the new identification NP-TMP,
	/// and provides various options to percolate that option downwards
	/// to the head noun, and perhaps also to inherit this from a PP-TMP.<br />
	/// 2) Annotates S nodes which contain a gapped subject: i.e.,
	/// <code>S &lt; (/^NP-SBJ/ &lt; -NONE-) --&gt; S-G</code>  <br />
	/// 3) Leave all functional tags on nodes. <br />
	/// 4) Keeps -ADV labels on NP and marks head tag with &`^ADV
	/// <p/>
	/// <i>Performance note:</i> At one point in time, PCFG labeled F1 results
	/// for the various TEMPORAL options in lexparser were:
	/// 0=86.7, 1=87.49, 2=86.87, 3=87.49, 4=87.48, 5=87.5, 6=87.07.
	/// So, mainly avoid values of 0, 2, and 6.
	/// <p/>
	/// At another point they were:
	/// 0=86.53, 1=87.1, 2=87.14, 3=87.22, 4=87.1, 5=87.13, 6=86.95, 7=87.16
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Dan Klein</author>
	[System.Serializable]
	public class NPTmpRetainingTreeNormalizer : BobChrisTreeNormalizer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.NPTmpRetainingTreeNormalizer));

		private const long serialVersionUID = 7548777133196579107L;

		public const int TemporalNone = 0;

		public const int TemporalAcl03pcfg = 1;

		public const int TemporalAnyTmpPercolated = 2;

		public const int TemporalAllTerminals = 3;

		public const int TemporalAllNp = 4;

		public const int TemporalAllNpAndPp = 5;

		public const int TemporalNpAndPpWithNpHead = 6;

		public const int TemporalAllNpEvenUnderPp = 7;

		public const int TemporalAllNpPpAdvp = 8;

		public const int Temporal9 = 9;

		private const bool onlyTagAnnotateNstar = true;

		private static readonly Pattern NPTmpPattern = Pattern.Compile("NP.*-TMP.*");

		private static readonly Pattern PPTmpPattern = Pattern.Compile("PP.*-TMP.*");

		private static readonly Pattern ADVPTmpPattern = Pattern.Compile("ADVP.*-TMP.*");

		private static readonly Pattern TmpPattern = Pattern.Compile(".*-TMP.*");

		private static readonly Pattern NPSbjPattern = Pattern.Compile("NP.*-SBJ.*");

		private static readonly Pattern NPAdvPattern = Pattern.Compile("NP.*-ADV.*");

		private readonly int temporalAnnotation;

		private readonly bool doSGappedStuff;

		private readonly int leaveItAll;

		private readonly bool doAdverbialNP;

		private readonly IHeadFinder headFinder;

		public NPTmpRetainingTreeNormalizer()
			: this(TemporalAcl03pcfg, false)
		{
		}

		public NPTmpRetainingTreeNormalizer(int temporalAnnotation, bool doSGappedStuff)
			: this(temporalAnnotation, doSGappedStuff, 0, false)
		{
		}

		public NPTmpRetainingTreeNormalizer(int temporalAnnotation, bool doSGappedStuff, int leaveItAll, bool doAdverbialNP)
			: this(temporalAnnotation, doSGappedStuff, leaveItAll, doAdverbialNP, new ModCollinsHeadFinder())
		{
		}

		/// <summary>
		/// Create a TreeNormalizer that maintains some functional annotations,
		/// particularly those involving temporal annotation.
		/// </summary>
		/// <param name="temporalAnnotation">
		/// One of the constants:
		/// TEMPORAL_NONE (no temporal annotation kept on trees),
		/// TEMPORAL_ACL03PCFG (temporal annotation on NPs, and percolated down
		/// to head of constituent until and including POS tag),
		/// TEMPORAL_ANY_TMP_PERCOLATED (temporal annotation on any phrase is
		/// kept and percolated via head chain to and including POS tag),
		/// TEMPORAL_ALL_TERMINALS (temporal annotation is kept on NPs, and
		/// is placed on all POS tag daughters of that NP (but is not
		/// percolated down a head chain through phrasal categories),
		/// TEMPORAL_ALL_NP (temporal annotation on NPs, and it is percolated
		/// down via the head chain, but only through NPs: annotation stops
		/// at either a POS tag (which is annotated) or a non-NP head
		/// (which isn't annotated)),
		/// TEMPORAL_ALL_NP_AND_PP (keeps temporal annotation on NPs and PPs,
		/// and it is percolated down via the head chain, but only through
		/// NPs: annotation stops at either a POS tag (which is annotated)
		/// or a non-NP head (which isn't annotated)).
		/// TEMPORAL_NP_AND_PP_WITH_NP_HEAD (like TEMPORAL_ALL_NP_AND_PP
		/// except an NP is regarded as the head of a PP)
		/// TEMPORAL_ALL_NP_EVEN_UNDER_PP (like TEMPORAL_ALL_NP, but a PP-TMP
		/// annotation above an NP is 'passed down' to annotate that NP
		/// as temporal (but the PP itself isn't marked))
		/// TEMPORAL_ALL_NP_PP_ADVP (keeps temporal annotation on NPs, PPs, and
		/// ADVPs
		/// and it is percolated down via the head chain, but only through
		/// those categories: annotation stops at either a POS tag
		/// (which is annotated)
		/// or a non-NP/PP/ADVP head (which isn't annotated)),
		/// TEMPORAL_9 (annotates like the previous one but
		/// does all NP inside node, and their children if
		/// pre-pre-terminal rather than only if head).
		/// </param>
		/// <param name="doSGappedStuff">
		/// Leave -SBJ marking on subject NP and then mark
		/// S-G sentences with a gapped subject.
		/// </param>
		/// <param name="leaveItAll">
		/// 0 means the usual stripping of functional tags and indices;
		/// 1 leaves all functional tags but still strips indices;
		/// 2 leaves everything
		/// </param>
		/// <param name="doAdverbialNP">
		/// Leave -ADV functional tag on adverbial NPs and
		/// maybe add it to their head
		/// </param>
		/// <param name="headFinder">
		/// A head finder that is used with some of the
		/// options for temporalAnnotation
		/// </param>
		public NPTmpRetainingTreeNormalizer(int temporalAnnotation, bool doSGappedStuff, int leaveItAll, bool doAdverbialNP, IHeadFinder headFinder)
		{
			this.temporalAnnotation = temporalAnnotation;
			this.doSGappedStuff = doSGappedStuff;
			this.leaveItAll = leaveItAll;
			this.doAdverbialNP = doAdverbialNP;
			this.headFinder = headFinder;
		}

		/// <summary>
		/// Remove things like hyphened functional tags and equals from the
		/// end of a node label.
		/// </summary>
		protected internal override string CleanUpLabel(string label)
		{
			if (label == null)
			{
				return "ROOT";
			}
			else
			{
				// String constants are always interned
				if (leaveItAll == 1)
				{
					return tlp.CategoryAndFunction(label);
				}
				else
				{
					if (leaveItAll == 2)
					{
						return label;
					}
					else
					{
						bool nptemp = NPTmpPattern.Matcher(label).Matches();
						bool pptemp = PPTmpPattern.Matcher(label).Matches();
						bool advptemp = ADVPTmpPattern.Matcher(label).Matches();
						bool anytemp = TmpPattern.Matcher(label).Matches();
						bool subj = NPSbjPattern.Matcher(label).Matches();
						bool npadv = NPAdvPattern.Matcher(label).Matches();
						label = tlp.BasicCategory(label);
						if (anytemp && temporalAnnotation == TemporalAnyTmpPercolated)
						{
							label += "-TMP";
						}
						else
						{
							if (pptemp && (temporalAnnotation == TemporalAllNpAndPp || temporalAnnotation == TemporalNpAndPpWithNpHead || temporalAnnotation == TemporalAllNpEvenUnderPp || temporalAnnotation == TemporalAllNpPpAdvp || temporalAnnotation == Temporal9))
							{
								label = label + "-TMP";
							}
							else
							{
								if (advptemp && (temporalAnnotation == TemporalAllNpPpAdvp || temporalAnnotation == Temporal9))
								{
									label = label + "-TMP";
								}
								else
								{
									if (temporalAnnotation > 0 && nptemp)
									{
										label = label + "-TMP";
									}
								}
							}
						}
						if (doAdverbialNP && npadv)
						{
							label = label + "-ADV";
						}
						if (doSGappedStuff && subj)
						{
							label = label + "-SBJ";
						}
						return label;
					}
				}
			}
		}

		private static bool IncludesEmptyNPSubj(Tree t)
		{
			if (t == null)
			{
				return false;
			}
			Tree[] kids = t.Children();
			if (kids == null)
			{
				return false;
			}
			bool foundNullSubj = false;
			foreach (Tree kid in kids)
			{
				Tree[] kidkids = kid.Children();
				if (NPSbjPattern.Matcher(kid.Value()).Matches())
				{
					kid.SetValue("NP");
					if (kidkids != null && kidkids.Length == 1 && kidkids[0].Value().Equals("-NONE-"))
					{
						// only set flag, since there are 2 a couple of times (errors)
						foundNullSubj = true;
					}
				}
			}
			return foundNullSubj;
		}

		/// <summary>Normalize a whole tree -- one can assume that this is the root.</summary>
		/// <remarks>
		/// Normalize a whole tree -- one can assume that this is the root.
		/// This implementation deletes empty elements (ones with nonterminal
		/// tag label '-NONE-') from the tree.
		/// </remarks>
		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			ITreeTransformer transformer1 = null;
			// Note: this changes the tree label, rather than
			// creating a new tree node.  Beware!
			IPredicate<Tree> subtreeFilter = new _IPredicate_218();
			// The special Switchboard non-terminals clause.
			// Note that it deletes IP which other Treebanks might use!
			//Prevents deletion of the word "IP"
			// Delete empty/trace nodes (ones marked '-NONE-')
			IPredicate<Tree> nodeFilter = new _IPredicate_238();
			// The special switchboard non-terminals clause. Try keeping EDITED for now....
			// if ("EDITED".equals(t.label().value())) {
			//   return false;
			// }
			ITreeTransformer transformer2 = null;
			// special fix for possessives! -- make noun before head
			// Note: this changes the tree label, rather than
			// creating a new tree node.  Beware!
			// look to right
			// Note: this changes the tree label, rather than
			// creating a new tree node.  Beware!
			// change all tags to -TMP
			// Note: this changes the tree label, rather
			// than creating a new tree node.  Beware!
			// Note: this changes the tree label, rather than
			// creating a new tree node.  Beware!
			// special fix for possessives! -- make noun before head
			// Note: this changes the tree label, rather than
			// creating a new tree node.  Beware!
			// also allow chain to start with PP
			// special fix for possessives! -- make noun before head
			// change the head to be NP if possible
			// Note: this next bit changes the tree label, rather
			// than creating a new tree node.  Beware!
			// also allow chain to start with PP or ADVP
			// special fix for possessives! -- make noun before head
			// Note: this next bit changes the tree label, rather
			// than creating a new tree node.  Beware!
			// also allow chain to start with PP or ADVP
			// log.info("TMP: Annotating " + t);
			// special fix for possessives! -- make noun before head
			// Note: this changes the tree label, rather than
			// creating a new tree node.  Beware!
			// special fix for possessives! -- make noun before head
			// Note: this changes the tree label, rather than
			// creating a new tree node.  Beware!
			// if there wasn't an empty nonterminal at the top, but an S, wrap it.
			if (tree.Label().Value().Equals("S"))
			{
				tree = tf.NewTreeNode("ROOT", Collections.SingletonList(tree));
			}
			// repair for the phrasal VB in Switchboard (PTB version 3) that should be a VP
			foreach (Tree subtree in tree)
			{
				if (subtree.IsPhrasal() && "VB".Equals(subtree.Label().Value()))
				{
					subtree.SetValue("VP");
				}
			}
			tree = tree.Transform(transformer1);
			if (tree == null)
			{
				return null;
			}
			tree = tree.Prune(subtreeFilter, tf);
			if (tree == null)
			{
				return null;
			}
			tree = tree.SpliceOut(nodeFilter, tf);
			if (tree == null)
			{
				return null;
			}
			return tree.Transform(transformer2, tf);
		}

		private sealed class _IPredicate_218 : IPredicate<Tree>
		{
			public _IPredicate_218()
			{
				this.serialVersionUID = -7250433816896327901L;
			}

			public bool Test(Tree t)
			{
				Tree[] kids = t.Children();
				ILabel l = t.Label();
				if ("RS".Equals(t.Label().Value()) || "RM".Equals(t.Label().Value()) || "IP".Equals(t.Label().Value()) || "CODE".Equals(t.Label().Value()))
				{
					return t.IsLeaf();
				}
				if ((l != null) && l.Value() != null && (l.Value().Equals("-NONE-")) && !t.IsLeaf() && kids.Length == 1 && kids[0].IsLeaf())
				{
					return false;
				}
				return true;
			}
		}

		private sealed class _IPredicate_238 : IPredicate<Tree>
		{
			public _IPredicate_238()
			{
				this.serialVersionUID = 9000955019205336311L;
			}

			public bool Test(Tree t)
			{
				if (t.IsLeaf() || t.IsPreTerminal())
				{
					return true;
				}
				if (t.NumChildren() != 1)
				{
					return true;
				}
				if (t.Label() != null && t.Label().Value() != null && t.Label().Value().Equals(t.Children()[0].Label().Value()))
				{
					return false;
				}
				return true;
			}
		}

		/// <summary>Add -TMP when not present within an NP</summary>
		/// <param name="tree">The tree to add temporal info to.</param>
		private void AddTMP9(Tree tree)
		{
			// do the head chain under it
			Tree ht = headFinder.DetermineHead(tree);
			// special fix for possessives! -- make noun before head
			if (ht.Value().Equals("POS"))
			{
				int j = tree.ObjectIndexOf(ht);
				if (j > 0)
				{
					ht = tree.GetChild(j - 1);
				}
			}
			// Note: this next bit changes the tree label, rather
			// than creating a new tree node.  Beware!
			if (ht.IsPreTerminal() || ht.Value().StartsWith("NP") || ht.Value().StartsWith("PP") || ht.Value().StartsWith("ADVP"))
			{
				if (!TmpPattern.Matcher(ht.Value()).Matches())
				{
					ILabelFactory lf = ht.LabelFactory();
					// log.info("TMP: Changing " + ht.value() + " to " +
					//                   ht.value() + "-TMP");
					ht.SetLabel(lf.NewLabel(ht.Value() + "-TMP"));
				}
				if (ht.Value().StartsWith("NP") || ht.Value().StartsWith("PP") || ht.Value().StartsWith("ADVP"))
				{
					AddTMP9(ht);
				}
			}
			// do the NPs under it (which may or may not be the head chain
			Tree[] kidlets = tree.Children();
			foreach (Tree kidlet in kidlets)
			{
				ht = kidlet;
				ILabelFactory lf;
				if (tree.IsPrePreTerminal() && !TmpPattern.Matcher(ht.Value()).Matches())
				{
					// log.info("TMP: Changing " + ht.value() + " to " +
					//                   ht.value() + "-TMP");
					lf = ht.LabelFactory();
					// Note: this next bit changes the tree label, rather
					// than creating a new tree node.  Beware!
					ht.SetLabel(lf.NewLabel(ht.Value() + "-TMP"));
				}
				else
				{
					if (ht.Value().StartsWith("NP"))
					{
						// don't add -TMP twice!
						if (!TmpPattern.Matcher(ht.Value()).Matches())
						{
							lf = ht.LabelFactory();
							// log.info("TMP: Changing " + ht.value() + " to " +
							//                   ht.value() + "-TMP");
							// Note: this next bit changes the tree label, rather
							// than creating a new tree node.  Beware!
							ht.SetLabel(lf.NewLabel(ht.Value() + "-TMP"));
						}
						AddTMP9(ht);
					}
				}
			}
		}

		/// <summary>
		/// Implementation of TreeReaderFactory, mainly for convenience of
		/// constructing by reflection.
		/// </summary>
		public class NPTmpRetainingTreeReaderFactory : ITreeReaderFactory
		{
			public virtual ITreeReader NewTreeReader(Reader @in)
			{
				return new PennTreeReader(@in, new LabeledScoredTreeFactory(), new NPTmpRetainingTreeNormalizer());
			}
		}

		/// <summary>
		/// Implementation of TreeReaderFactory, mainly for convenience of
		/// constructing by reflection.
		/// </summary>
		/// <remarks>
		/// Implementation of TreeReaderFactory, mainly for convenience of
		/// constructing by reflection. This one corresponds to what's currently
		/// used in englishPCFG accurate unlexicalized parser.
		/// </remarks>
		public class NPTmpAdvRetainingTreeReaderFactory : ITreeReaderFactory
		{
			public virtual ITreeReader NewTreeReader(Reader @in)
			{
				return new PennTreeReader(@in, new LabeledScoredTreeFactory(), new NPTmpRetainingTreeNormalizer(NPTmpRetainingTreeNormalizer.TemporalAcl03pcfg, false, 0, true));
			}
		}
	}
}
