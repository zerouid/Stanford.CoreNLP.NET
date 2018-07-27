// Stanford Parser -- a probabilistic lexicalized NL CFG parser
// Copyright (c) 2002 - 2014 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/ .
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    parser-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/lex-parser.html
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Parser parameters for the Penn English Treebank (WSJ, Brown, Switchboard).</summary>
	/// <author>Roger Levy</author>
	/// <author>Christopher Manning</author>
	/// <version>03/05/2003</version>
	[System.Serializable]
	public class EnglishTreebankParserParams : AbstractTreebankParserParams
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.EnglishTreebankParserParams));

		protected internal class EnglishSubcategoryStripper : ITreeTransformer
		{
			protected internal ITreeFactory tf = new LabeledScoredTreeFactory();

			public virtual Tree TransformTree(Tree tree)
			{
				ILabel lab = tree.Label();
				string s = lab.Value();
				string tag = null;
				if (lab is IHasTag)
				{
					tag = ((IHasTag)lab).Tag();
				}
				if (tree.IsLeaf())
				{
					Tree leaf = this.tf.NewLeaf(lab);
					leaf.SetScore(tree.Score());
					return leaf;
				}
				else
				{
					if (tree.IsPhrasal())
					{
						if (this._enclosing.englishTest.retainADVSubcategories && s.Contains("-ADV"))
						{
							s = this._enclosing.tlp.BasicCategory(s);
							s += "-ADV";
						}
						else
						{
							if (this._enclosing.englishTest.retainTMPSubcategories && s.Contains("-TMP"))
							{
								s = this._enclosing.tlp.BasicCategory(s);
								s += "-TMP";
							}
							else
							{
								if (this._enclosing.englishTest.retainNPTMPSubcategories && s.StartsWith("NP-TMP"))
								{
									s = "NP-TMP";
								}
								else
								{
									s = this._enclosing.tlp.BasicCategory(s);
								}
							}
						}
						// remove the extra NPs inserted in the splitBaseNP == Collins option
						if (this._enclosing.englishTrain.splitBaseNP == 2 && s.Equals("NP"))
						{
							Tree[] kids = tree.Children();
							if (kids.Length == 1 && this._enclosing.tlp.BasicCategory(kids[0].Value()).Equals("NP"))
							{
								// go through kidkids here so as to keep any annotation on me.
								IList<Tree> kidkids = new List<Tree>();
								for (int cNum = 0; cNum < kids[0].Children().Length; cNum++)
								{
									Tree child = kids[0].Children()[cNum];
									Tree newChild = this.TransformTree(child);
									if (newChild != null)
									{
										kidkids.Add(newChild);
									}
								}
								CategoryWordTag myLabel = new CategoryWordTag(lab);
								myLabel.SetCategory(s);
								return this.tf.NewTreeNode(myLabel, kidkids);
							}
						}
						// remove the extra POSSPs inserted by restructurePossP
						if (this._enclosing.englishTrain.splitPoss == 2 && s.Equals("POSSP"))
						{
							Tree[] kids = tree.Children();
							IList<Tree> newkids = new List<Tree>();
							for (int j = 0; j < kids.Length - 1; j++)
							{
								for (int cNum = 0; cNum < kids[j].Children().Length; cNum++)
								{
									Tree child = kids[0].Children()[cNum];
									Tree newChild = this.TransformTree(child);
									if (newChild != null)
									{
										newkids.Add(newChild);
									}
								}
							}
							Tree finalChild = this.TransformTree(kids[kids.Length - 1]);
							newkids.Add(finalChild);
							CategoryWordTag myLabel = new CategoryWordTag(lab);
							myLabel.SetCategory("NP");
							return this.tf.NewTreeNode(myLabel, newkids);
						}
					}
					else
					{
						// preterminal
						s = this._enclosing.tlp.BasicCategory(s);
						if (tag != null)
						{
							tag = this._enclosing.tlp.BasicCategory(tag);
						}
					}
				}
				IList<Tree> children = new List<Tree>();
				for (int cNum_1 = 0; cNum_1 < tree.NumChildren(); cNum_1++)
				{
					Tree child = tree.GetChild(cNum_1);
					Tree newChild = this.TransformTree(child);
					if (newChild != null)
					{
						children.Add(newChild);
					}
				}
				if (children.IsEmpty())
				{
					return null;
				}
				CategoryWordTag newLabel = new CategoryWordTag(lab);
				newLabel.SetCategory(s);
				if (tag != null)
				{
					newLabel.SetTag(tag);
				}
				Tree node = this.tf.NewTreeNode(newLabel, children);
				node.SetScore(tree.Score());
				return node;
			}

			internal EnglishSubcategoryStripper(EnglishTreebankParserParams _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly EnglishTreebankParserParams _enclosing;
		}

		public EnglishTreebankParserParams()
			: base(new PennTreebankLanguagePack())
		{
			// end class EnglishSubcategoryStripper
			headFinder = new ModCollinsHeadFinder(tlp);
		}

		private IHeadFinder headFinder;

		private EnglishTreebankParserParams.EnglishTrain englishTrain = new EnglishTreebankParserParams.EnglishTrain();

		private EnglishTreebankParserParams.EnglishTest englishTest = new EnglishTreebankParserParams.EnglishTest();

		public override IHeadFinder HeadFinder()
		{
			return headFinder;
		}

		public override IHeadFinder TypedDependencyHeadFinder()
		{
			if (generateOriginalDependencies)
			{
				return new SemanticHeadFinder(TreebankLanguagePack(), !englishTest.makeCopulaHead);
			}
			else
			{
				return new UniversalSemanticHeadFinder(TreebankLanguagePack(), !englishTest.makeCopulaHead);
			}
		}

		/// <summary>Allows you to read in trees from the source you want.</summary>
		/// <remarks>
		/// Allows you to read in trees from the source you want.  It's the
		/// responsibility of treeReaderFactory() to deal properly with character-set
		/// encoding of the input.  It also is the responsibility of tr to properly
		/// normalize trees.
		/// </remarks>
		public override DiskTreebank DiskTreebank()
		{
			return new DiskTreebank(TreeReaderFactory());
		}

		/// <summary>Allows you to read in trees from the source you want.</summary>
		/// <remarks>
		/// Allows you to read in trees from the source you want.  It's the
		/// responsibility of treeReaderFactory() to deal properly with character-set
		/// encoding of the input.  It also is the responsibility of tr to properly
		/// normalize trees.
		/// </remarks>
		public override MemoryTreebank MemoryTreebank()
		{
			return new MemoryTreebank(TreeReaderFactory());
		}

		/// <summary>Makes appropriate TreeReaderFactory with all options specified</summary>
		public override ITreeReaderFactory TreeReaderFactory()
		{
			return null;
		}

		/// <summary>returns a MemoryTreebank appropriate to the testing treebank source</summary>
		public override MemoryTreebank TestMemoryTreebank()
		{
			return new MemoryTreebank(null);
		}

		/// <summary>The tree transformer used to produce trees for evaluation.</summary>
		/// <remarks>
		/// The tree transformer used to produce trees for evaluation.  It will
		/// be applied both to the parser output and the gold tree.
		/// </remarks>
		public override ITreeTransformer Collinizer()
		{
			return new TreeCollinizer(tlp, true, englishTrain.splitBaseNP == 2, englishTrain.collapseWhCategories);
		}

		public override ITreeTransformer CollinizerEvalb()
		{
			return new TreeCollinizer(tlp, true, englishTrain.splitBaseNP == 2, englishTrain.collapseWhCategories);
		}

		/// <summary>
		/// contains Treebank-specific (but not parser-specific) info such
		/// as what is punctuation, and also information about the structure
		/// of labels
		/// </summary>
		public override ITreebankLanguagePack TreebankLanguagePack()
		{
			return tlp;
		}

		public override ILexicon Lex(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			if (op.lexOptions.uwModelTrainer == null)
			{
				//use default unknown word model for English
				op.lexOptions.uwModelTrainer = "edu.stanford.nlp.parser.lexparser.EnglishUnknownWordModelTrainer";
			}
			return new BaseLexicon(op, wordIndex, tagIndex);
		}

		private static readonly string[] sisterSplit1 = new string[] { "ADJP=l=VBD", "ADJP=l=VBP", "NP=r=RBR", "PRN=r=.", "ADVP=l=PP", "PP=l=JJ", "PP=r=NP", "SBAR=l=VB", "PP=l=VBG", "ADJP=r=,", "ADVP=r=.", "ADJP=l=VB", "FRAG=l=FRAG", "FRAG=r=:", "PP=r=,"
			, "ADJP=l=,", "FRAG=r=FRAG", "FRAG=l=:", "PRN=r=VP", "PP=l=RB", "S=l=ADJP", "SBAR=l=VBN", "NP=r=NX", "SBAR=l=VBZ", "SBAR=l=ADVP", "QP=r=JJ", "SBAR=l=PP", "SBAR=l=ADJP", "NP=r=VBG", "VP=r=:", "VP=l=ADJP", "SBAR=l=VBP", "ADVP=r=NP", "PP=l=VB"
			, "VP=r=PP", "ADJP=r=SBAR", "NP=r=JJR", "SBAR=l=NN", "S=l=RB", "S=l=NNS", "S=r=SBAR", "S=l=WHPP", "VP=l=:", "ADVP=l=NP", "ADVP=r=PP", "ADJP=l=JJ", "NP=r=VBN", "NP=l=PRN", "VP=r=S", "NP=r=NNPS", "NX=r=NX", "ADJP=l=PRP$", "SBAR=l=CC", "SBAR=l=S"
			, "S=l=PRT", "ADVP=l=VB", "ADVP=r=JJ", "NP=l=DT" };

		private static readonly string[] sisterSplit2 = new string[] { "S=r=PP", "NP=r=JJS", "ADJP=r=NNP", "NP=l=PRT", "ADJP=r=PP", "ADJP=l=VBZ", "PP=r=VP", "NP=r=CD", "ADVP=l=IN", "ADVP=l=,", "ADJP=r=JJ", "ADVP=l=VBD", "PP=r=.", "S=l=ADVP", "S=l=DT"
			, "PP=l=NP", "VP=l=PRN", "NP=r=IN", "NP=r=``" };

		private static readonly string[] sisterSplit3 = new string[] { "PP=l=VBD", "ADJP=r=NNS", "S=l=:", "NP=l=ADVP", "NP=r=PRN", "NP=r=-RRB-", "NP=l=-LRB-", "NP=l=JJ", "SBAR=r=.", "S=r=:", "ADVP=r=VP", "NP=l=RB", "NP=r=RB", "S=l=VBP", "SBAR=r=,", 
			"VP=r=,", "PP=r=PP", "NP=r=S", "ADJP=l=NP", "VP=l=VBG", "PP=l=PP" };

		private static readonly string[] sisterSplit4 = new string[] { "VP=l=NP", "NP=r=NN", "NP=r=VP", "VP=r=.", "NP=r=PP", "VP=l=TO", "VP=l=MD", "NP=r=,", "NP=r=NP", "NP=r=.", "NP=l=IN", "NP=l=NP", "VP=l=,", "VP=l=S", "NP=l=,", "VP=l=VBZ", "S=r=."
			, "NP=r=NNS", "S=l=IN", "NP=r=JJ", "NP=r=NNP", "VP=l=VBD", "S=l=WHNP", "VP=r=NP", "VP=l=''", "VP=l=VBP", "NP=l=:", "S=r=,", "VP=l=``", "VP=l=VB", "NP=l=S", "NP=l=VP", "NP=l=VB", "NP=l=VBD", "NP=r=SBAR", "NP=r=:", "VP=l=PP", "NP=l=VBZ", "NP=l=CC"
			, "NP=l=''", "S=r=NP", "S=r=S", "S=l=VBN", "NP=l=``", "ADJP=r=NN", "S=r=VP", "NP=r=CC", "VP=l=RB", "S=l=S", "S=l=NP", "NP=l=TO", "S=l=,", "S=l=VBD", "S=r=''", "S=l=``", "S=r=CC", "PP=l=,", "S=l=CC", "VP=l=CC", "ADJP=l=DT", "NP=l=VBG", "VP=r=''"
			, "SBAR=l=NP", "VP=l=VP", "NP=l=PP", "S=l=VB", "SBAR=l=VBD", "VP=l=ADVP", "VP=l=VBN", "NP=r=''", "VP=l=SBAR", "SBAR=l=,", "S=l=WHADVP", "VP=r=VP", "NP=r=ADVP", "QP=r=NNS", "NP=l=VBP", "S=l=VBZ", "NP=l=VBN", "S=l=PP", "VP=r=CC", "NP=l=SBAR", 
			"SBAR=r=NP", "S=l=VBG", "SBAR=r=VP", "NP=r=ADJP", "S=l=JJ", "S=l=NN", "QP=r=NN" };

		// Automatically generated by SisterAnnotationStats -- preferably don't edit
		public override string[] SisterSplitters()
		{
			switch (englishTrain.sisterSplitLevel)
			{
				case 1:
				{
					return sisterSplit1;
				}

				case 2:
				{
					return sisterSplit2;
				}

				case 3:
				{
					return sisterSplit3;
				}

				case 4:
				{
					return sisterSplit4;
				}

				default:
				{
					return new string[0];
				}
			}
		}

		/// <summary>
		/// Returns a TreeTransformer appropriate to the Treebank which
		/// can be used to remove functional tags (such as "-TMP") from
		/// categories.
		/// </summary>
		public override ITreeTransformer SubcategoryStripper()
		{
			return new EnglishTreebankParserParams.EnglishSubcategoryStripper(this);
		}

		[System.Serializable]
		public class EnglishTest
		{
			internal EnglishTest()
			{
			}

			internal bool retainNPTMPSubcategories = false;

			internal bool retainTMPSubcategories = false;

			internal bool retainADVSubcategories = false;

			internal bool makeCopulaHead = false;

			private const long serialVersionUID = 183157656745674521L;
			/* THESE OPTIONS ARE ENGLISH-SPECIFIC AND AFFECT ONLY TEST TIME */
		}

		[System.Serializable]
		public class EnglishTrain
		{
			internal EnglishTrain()
			{
			}

			/// <summary>if true, leave all PTB (functional tag) annotations (bad)</summary>
			public int leaveItAll = 0;

			/// <summary>Annotate prepositions into subcategories.</summary>
			/// <remarks>
			/// Annotate prepositions into subcategories.  Values:
			/// 0 = no annotation
			/// 1 = IN with a ^S.* parent (putative subordinating
			/// conjunctions) marked differently from others (real prepositions). OK.
			/// 2 = Annotate IN prepositions 3 ways: ^S.* parent, ^N.* parent or rest
			/// (generally predicative ADJP, VP). Better than sIN=1.  Good.
			/// 3 = Annotate prepositions 6 ways: real feature engineering. Great.
			/// 4 = Refinement of 3: allows -SC under SINV, WHADVP for -T and no -SCC
			/// if the parent is an NP.
			/// 5 = Like 4 but maps TO to IN in a "nominal" (N*, P*, A*) context.
			/// 6 = 4, but mark V/A complement and leave noun ones unmarked instead.
			/// </remarks>
			public int splitIN = 0;

			/// <summary>Mark quote marks for single vs.</summary>
			/// <remarks>Mark quote marks for single vs. double so don't get mismatched ones.</remarks>
			public bool splitQuotes = false;

			/// <summary>Separate out sentence final punct.</summary>
			/// <remarks>Separate out sentence final punct. (. ! ?).  Doesn't help.</remarks>
			public bool splitSFP = false;

			/// <summary>Mark the nouns that are percent signs.</summary>
			/// <remarks>Mark the nouns that are percent signs.  Slightly good.</remarks>
			public bool splitPercent = false;

			/// <summary>Mark phrases that are headed by %.</summary>
			/// <remarks>
			/// Mark phrases that are headed by %.
			/// A value of 0 = do nothing, 1 = only NP, 2 = NP and ADJP,
			/// 3 = NP, ADJP and QP, 4 = any phrase.
			/// </remarks>
			public int splitNPpercent = 0;

			/// <summary>
			/// Grand parent annotate RB to try to distinguish sentential ones and
			/// ones in places like NP post modifier (things like 'very' are already
			/// distinguished as their parent is ADJP).
			/// </summary>
			public bool tagRBGPA = false;

			/// <summary>
			/// Mark NNP words as to position in phrase (single, left, right, inside)
			/// or subcategorizes NNP(S) as initials or initial/final in NP.
			/// </summary>
			public int splitNNP = 0;

			/// <summary>Join pound with dollar.</summary>
			public bool joinPound = false;

			/// <summary>Joint comparative and superlative adjective with positive.</summary>
			public bool joinJJ = false;

			/// <summary>Join proper nouns with common nouns.</summary>
			/// <remarks>
			/// Join proper nouns with common nouns. This isn't to improve
			/// performance, but because Genia doesn't use proper noun tags in
			/// general.
			/// </remarks>
			public bool joinNounTags = false;

			/// <summary>A special test for "such" mainly ("such as Fred").</summary>
			/// <remarks>A special test for "such" mainly ("such as Fred"). A wash, so omit</remarks>
			public bool splitPPJJ = false;

			/// <summary>
			/// Put a special tag on 'transitive adjectives' with NP complement, like
			/// 'due May 15' -- it also catches 'such' in 'such as NP', which may
			/// be a good.
			/// </summary>
			/// <remarks>
			/// Put a special tag on 'transitive adjectives' with NP complement, like
			/// 'due May 15' -- it also catches 'such' in 'such as NP', which may
			/// be a good.  Matches 658 times in 2-21 training corpus. Wash.
			/// </remarks>
			public bool splitTRJJ = false;

			/// <summary>Put a special tag on 'adjectives with complements'.</summary>
			/// <remarks>
			/// Put a special tag on 'adjectives with complements'.  This acts as a
			/// general subcat feature for adjectives.
			/// </remarks>
			public bool splitJJCOMP = false;

			/// <summary>
			/// Specially mark the comparative/superlative words: less, least,
			/// more, most
			/// </summary>
			public bool splitMoreLess = false;

			/// <summary>Mark "Intransitive" DT.</summary>
			/// <remarks>Mark "Intransitive" DT.  Good.</remarks>
			public bool unaryDT = false;

			/// <summary>Mark "Intransitive" RB.</summary>
			/// <remarks>Mark "Intransitive" RB.  Good.</remarks>
			public bool unaryRB = false;

			/// <summary>"Intransitive" PRP.</summary>
			/// <remarks>"Intransitive" PRP. Wash -- basically a no-op really.</remarks>
			public bool unaryPRP = false;

			/// <summary>Mark reflexive PRP words.</summary>
			public bool markReflexivePRP = false;

			/// <summary>Mark "Intransitive" IN.</summary>
			/// <remarks>Mark "Intransitive" IN. Minutely negative.</remarks>
			public bool unaryIN = false;

			/// <summary>Provide annotation of conjunctions.</summary>
			/// <remarks>
			/// Provide annotation of conjunctions.  Gives modest gains (numbers
			/// shown F1 increase with respect to goodPCFG in June 2005).  A value of
			/// 1 annotates both "and" and "or" as "CC-C" (+0.29%),
			/// 2 annotates "but" and "&amp;" separately (+0.17%),
			/// 3 annotates just "and" (equalsIgnoreCase) (+0.11%),
			/// 0 annotates nothing (+0.00%).
			/// </remarks>
			public int splitCC = 0;

			/// <summary>Annotates forms of "not" specially as tag "NOT".</summary>
			/// <remarks>Annotates forms of "not" specially as tag "NOT". BAD</remarks>
			public bool splitNOT = false;

			/// <summary>Split modifier (NP, AdjP) adverbs from others.</summary>
			/// <remarks>
			/// Split modifier (NP, AdjP) adverbs from others.
			/// This does nothing if you're already doing tagPA.
			/// </remarks>
			public bool splitRB = false;

			/// <summary>Make special tags for forms of BE and HAVE (and maybe DO/HELP, etc.).</summary>
			/// <remarks>
			/// Make special tags for forms of BE and HAVE (and maybe DO/HELP, etc.).
			/// A value of 0 is do nothing.
			/// A value of 1 is the basic form.  Positive PCFG effect,
			/// but neutral to negative in Factored, and impossible if you use gPA.
			/// A value of 2 adds in "s" = "'s"
			/// and delves further to disambiguate "'s" as BE or HAVE.  Theoretically
			/// good, but no practical gains.
			/// A value of 3 adds DO.
			/// A value of 4 adds HELP (which also takes VB form complement) as DO.
			/// A value of 5 adds LET (which also takes VB form complement) as DO.
			/// A value of 6 adds MAKE (which also takes VB form complement) as DO.
			/// A value of 7 adds WATCH, SEE (which also take VB form complement) as DO.
			/// A value of 8 adds come, go, but not inflections (which colloquially
			/// can take a VB form complement) as DO.
			/// A value of 9 adds GET as BE.
			/// Differences are small. You get about 0.3 F1 by doing something; the best
			/// appear to be 2 or 3 for sentence exact and 7 or 8 for LP/LR F1.
			/// </remarks>
			public int splitAux = 0;

			/// <summary>
			/// Pitiful attempt at marking V* preterms with their surface subcat
			/// frames.
			/// </summary>
			/// <remarks>
			/// Pitiful attempt at marking V* preterms with their surface subcat
			/// frames.  Bad so far.
			/// </remarks>
			public bool vpSubCat = false;

			/// <summary>Attempt to record ditransitive verbs.</summary>
			/// <remarks>
			/// Attempt to record ditransitive verbs.  The value 0 means do nothing;
			/// 1 records two or more NP or S* arguments, and 2 means to only record
			/// two or more NP arguments (that aren't NP-TMP).
			/// 1 gave neutral to bad results.
			/// </remarks>
			public int markDitransV = 0;

			/// <summary>Add (head) tags to VPs.</summary>
			/// <remarks>
			/// Add (head) tags to VPs.  An argument of
			/// 0 = no head-subcategorization of VPs,
			/// 1 = add head tags (anything, as given by HeadFinder),
			/// 2 = add head tags, but collapse finite verb tags (VBP, VBD, VBZ, MD)
			/// together,
			/// 3 = only annotate verbal tags, and collapse finite verb tags
			/// (annotation is VBF, TO, VBG, VBN, VB, or zero),
			/// 4 = only split on categories of VBF, TO, VBG, VBN, VB, and map
			/// cases that are not headed by a verbal category to an appropriate
			/// category based on word suffix (ing, d, t, s, to) or to VB otherwise.
			/// We usually use a value of 3; 2 or 3 is much better than 0.
			/// See also
			/// <c>splitVPNPAgr</c>
			/// . If it is true, its effects override
			/// any value set for this parameter.
			/// </remarks>
			public int splitVP = 0;

			/// <summary>Put enough marking on VP and NP to permit "agreement".</summary>
			public bool splitVPNPAgr = false;

			/// <summary>Mark S/SINV/SQ nodes according to verbal tag.</summary>
			/// <remarks>
			/// Mark S/SINV/SQ nodes according to verbal tag.  Meanings are:
			/// 0 = no subcategorization.
			/// 1 = mark with head tag
			/// 2 = mark only -VBF if VBZ/VBD/VBP/MD tag
			/// 3 = as 2 and mark -VBNF if TO/VBG/VBN/VB
			/// 4 = as 2 but only mark S not SINV/SQ
			/// 5 = as 3 but only mark S not SINV/SQ
			/// Previously seen as bad.  Option 4 might be promising now.
			/// </remarks>
			public int splitSTag = 0;

			public bool markContainedVP = false;

			public bool splitNPPRP = false;

			/// <summary>Verbal distance -- mark whether symbol dominates a verb (V*, MD).</summary>
			/// <remarks>
			/// Verbal distance -- mark whether symbol dominates a verb (V*, MD).
			/// Very good.
			/// </remarks>
			public int dominatesV = 0;

			/// <summary>Verbal distance -- mark whether symbol dominates a preposition (IN)</summary>
			public bool dominatesI = false;

			/// <summary>Verbal distance -- mark whether symbol dominates a conjunction (CC)</summary>
			public bool dominatesC = false;

			/// <summary>Mark phrases which are conjunctions.</summary>
			/// <remarks>
			/// Mark phrases which are conjunctions.
			/// 0 = No marking
			/// 1 = Any phrase with a CC daughter that isn't first or last.  Possibly marginally positive.
			/// 2 = As 0 but also a non-marginal CONJP daughter.  In principle good, but no gains.
			/// 3 = More like Charniak.  Not yet implemented.  Need to annotate _before_ annotate children!
			/// np or vp with two or more np/vp children, a comma, cc or conjp, and nothing else.
			/// </remarks>
			public int markCC = 0;

			/// <summary>Mark specially S nodes with "gapped" subject (control, raising).</summary>
			/// <remarks>
			/// Mark specially S nodes with "gapped" subject (control, raising).
			/// 1 is basic version.  2 is better mark S nodes with "gapped" subject.
			/// 3 seems best on small training set, but all of these are too similar;
			/// 4 can't be differentiated.
			/// 5 is done on tree before empty splitting. (Bad!?)
			/// </remarks>
			public int splitSGapped = 0;

			/// <summary>Mark "numeric NPs".</summary>
			/// <remarks>Mark "numeric NPs".  Probably bad?</remarks>
			public bool splitNumNP = false;

			/// <summary>Give a special tag to NPs which are possessive NPs (end in 's).</summary>
			/// <remarks>
			/// Give a special tag to NPs which are possessive NPs (end in 's).
			/// A value of 0 means do nothing, 1 means tagging possessive NPs with
			/// "-P", 2 means restructure possessive NPs so that they introduce a
			/// POSSP node that
			/// takes as children the POS and a regularly structured NP.
			/// I.e., recover standard good linguistic practice circa 1985.
			/// This seems a good idea, but is almost a no-op (modulo fine points of
			/// markovization), since the previous NP-P phrase already uniquely
			/// captured what is now a POSSP.
			/// </remarks>
			public int splitPoss = 0;

			/// <summary>Mark base NPs.</summary>
			/// <remarks>
			/// Mark base NPs.  A value of 0 = no marking, 1 = marking
			/// baseNP (ones which rewrite just as preterminals), and 2 = doing
			/// Collins-style marking, where an extra NP node is inserted above a
			/// baseNP, if it isn't
			/// already in an NP over NP construction, as in Collins 1999.
			/// <i>This option shouldn't really be in EnglishTrain since it's needed
			/// at parsing time.  But we don't currently use it....</i>
			/// A value of 1 is good.
			/// </remarks>
			public int splitBaseNP = 0;

			/// <summary>Retain NP-TMP (or maybe PP-TMP) annotation.</summary>
			/// <remarks>
			/// Retain NP-TMP (or maybe PP-TMP) annotation.  Good.
			/// The values for this parameter are defined in
			/// NPTmpRetainingTreeNormalizer.
			/// </remarks>
			public int splitTMP = NPTmpRetainingTreeNormalizer.TemporalNone;

			/// <summary>Split SBAR nodes.</summary>
			/// <remarks>
			/// Split SBAR nodes.
			/// 1 = mark 'in order to' purpose clauses; this is actually a small and
			/// inconsistent part of what is marked SBAR-PRP in the treebank, which
			/// is mainly 'because' reason clauses.
			/// 2 = mark all infinitive SBAR.
			/// 3 = do 1 and 2.
			/// A value of 1 seems minutely positive; 2 and 3 seem negative.
			/// Also get 'in case Sfin', 'In order to', and on one occasion
			/// 'in order that'
			/// </remarks>
			public int splitSbar = 0;

			/// <summary>Retain NP-ADV annotation.</summary>
			/// <remarks>
			/// Retain NP-ADV annotation.  0 means strip "-ADV" annotation.  1 means to
			/// retain it, and to percolate it down to a head tag providing it can
			/// do it through a path of only NP nodes.
			/// </remarks>
			public int splitNPADV = 0;

			/// <summary>Mark NP-NNP.</summary>
			/// <remarks>
			/// Mark NP-NNP.  0 is nothing; 1 is only NNP head, 2 is NNP and NNPS
			/// head; 3 is NNP or NNPS anywhere in local NP.  All bad!
			/// </remarks>
			public int splitNPNNP = 0;

			/// <summary>'Correct' tags to produce verbs in VPs, etc.</summary>
			/// <remarks>'Correct' tags to produce verbs in VPs, etc. where possible</remarks>
			public bool correctTags = false;

			/// <summary>Right edge has a phrasal node.</summary>
			/// <remarks>Right edge has a phrasal node.  Bad?</remarks>
			public bool rightPhrasal = false;

			/// <summary>
			/// Set the support * KL cutoff level (1-4) for sister splitting
			/// -- don't use it, as far as we can tell so far
			/// </summary>
			public int sisterSplitLevel = 1;

			/// <summary>Grand-parent annotate (root mark) VP below ROOT.</summary>
			/// <remarks>Grand-parent annotate (root mark) VP below ROOT.  Seems negative.</remarks>
			public bool gpaRootVP = false;

			/// <summary>Change TO inside PP to IN.</summary>
			public int makePPTOintoIN = 0;

			/// <summary>Collapse WHPP with PP, etc., in training and perhaps in evaluation.</summary>
			/// <remarks>
			/// Collapse WHPP with PP, etc., in training and perhaps in evaluation.
			/// 1 = collapse phrasal categories.
			/// 2 = collapse POS categories.
			/// 4 = restore them in output (not yet implemented)
			/// </remarks>
			public int collapseWhCategories = 0;

			/* THESE OPTIONS ARE ENGLISH-SPECIFIC AND AFFECT ONLY TRAIN TIME */
			//true;
			//true;
			public virtual void Display()
			{
				string englishParams = "Using EnglishTreebankParserParams" + " splitIN=" + splitIN + " sPercent=" + splitPercent + " sNNP=" + splitNNP + " sQuotes=" + splitQuotes + " sSFP=" + splitSFP + " rbGPA=" + tagRBGPA + " j#=" + joinPound + " jJJ=" + 
					joinJJ + " jNounTags=" + joinNounTags + " sPPJJ=" + splitPPJJ + " sTRJJ=" + splitTRJJ + " sJJCOMP=" + splitJJCOMP + " sMoreLess=" + splitMoreLess + " unaryDT=" + unaryDT + " unaryRB=" + unaryRB + " unaryPRP=" + unaryPRP + " reflPRP=" + markReflexivePRP
					 + " unaryIN=" + unaryIN + " sCC=" + splitCC + " sNT=" + splitNOT + " sRB=" + splitRB + " sAux=" + splitAux + " vpSubCat=" + vpSubCat + " mDTV=" + markDitransV + " sVP=" + splitVP + " sVPNPAgr=" + splitVPNPAgr + " sSTag=" + splitSTag + " mVP="
					 + markContainedVP + " sNP%=" + splitNPpercent + " sNPPRP=" + splitNPPRP + " dominatesV=" + dominatesV + " dominatesI=" + dominatesI + " dominatesC=" + dominatesC + " mCC=" + markCC + " sSGapped=" + splitSGapped + " numNP=" + splitNumNP + " sPoss="
					 + splitPoss + " baseNP=" + splitBaseNP + " sNPNNP=" + splitNPNNP + " sTMP=" + splitTMP + " sNPADV=" + splitNPADV + " cTags=" + correctTags + " rightPhrasal=" + rightPhrasal + " gpaRootVP=" + gpaRootVP + " splitSbar=" + splitSbar + " mPPTOiIN="
					 + makePPTOintoIN + " cWh=" + collapseWhCategories;
				log.Info(englishParams);
			}

			private const long serialVersionUID = 1831576434872643L;
		}

		private static readonly ITreeFactory categoryWordTagTreeFactory = new LabeledScoredTreeFactory(new CategoryWordTagFactory());

		// end class EnglishTrain
		/// <summary>
		/// This method does language-specific tree transformations such
		/// as annotating particular nodes with language-relevant features.
		/// </summary>
		/// <remarks>
		/// This method does language-specific tree transformations such
		/// as annotating particular nodes with language-relevant features.
		/// Such parameterizations should be inside the specific
		/// TreebankLangParserParams class.  This method is recursively
		/// applied to each node in the tree (depth first, left-to-right),
		/// so you shouldn't write this method to apply recursively to tree
		/// members.  This method is allowed to (and in some cases does)
		/// destructively change the input tree
		/// <paramref name="t"/>
		/// . It changes both
		/// labels and the tree shape.
		/// </remarks>
		/// <param name="t">
		/// The input tree (with non-language-specific annotation already
		/// done, so you need to strip back to basic categories)
		/// </param>
		/// <param name="root">The root of the current tree (can be null for words)</param>
		/// <returns>
		/// The fully annotated tree node (with daughters still as you
		/// want them in the final result)
		/// </returns>
		public override Tree TransformTree(Tree t, Tree root)
		{
			if (t == null || t.IsLeaf())
			{
				return t;
			}
			Tree parent;
			string parentStr;
			string grandParentStr;
			if (root == null || t.Equals(root))
			{
				parent = null;
				parentStr = string.Empty;
			}
			else
			{
				parent = t.Parent(root);
				parentStr = parent.Label().Value();
			}
			if (parent == null || parent.Equals(root))
			{
				grandParentStr = string.Empty;
			}
			else
			{
				Tree grandParent = parent.Parent(root);
				grandParentStr = grandParent.Label().Value();
			}
			string baseParentStr = tlp.BasicCategory(parentStr);
			string baseGrandParentStr = tlp.BasicCategory(grandParentStr);
			CoreLabel lab = (CoreLabel)t.Label();
			string word = lab.Word();
			string tag = lab.Tag();
			string baseTag = tlp.BasicCategory(tag);
			string cat = lab.Value();
			string baseCat = tlp.BasicCategory(cat);
			if (t.IsPreTerminal())
			{
				if (englishTrain.correctTags)
				{
					if (baseParentStr.Equals("NP"))
					{
						switch (baseCat)
						{
							case "IN":
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(word, "a") || Sharpen.Runtime.EqualsIgnoreCase(word, "that"))
								{
									cat = ChangeBaseCat(cat, "DT");
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(word, "so") || Sharpen.Runtime.EqualsIgnoreCase(word, "about"))
									{
										cat = ChangeBaseCat(cat, "RB");
									}
									else
									{
										if (word.Equals("fiscal") || Sharpen.Runtime.EqualsIgnoreCase(word, "next"))
										{
											cat = ChangeBaseCat(cat, "JJ");
										}
									}
								}
								break;
							}

							case "RB":
							{
								if (word.Equals("McNally"))
								{
									cat = ChangeBaseCat(cat, "NNP");
								}
								else
								{
									if (word.Equals("multifamily"))
									{
										cat = ChangeBaseCat(cat, "NN");
									}
									else
									{
										if (word.Equals("MORE"))
										{
											cat = ChangeBaseCat(cat, "JJR");
										}
										else
										{
											if (word.Equals("hand"))
											{
												cat = ChangeBaseCat(cat, "NN");
											}
											else
											{
												if (word.Equals("fist"))
												{
													cat = ChangeBaseCat(cat, "NN");
												}
											}
										}
									}
								}
								break;
							}

							case "RP":
							{
								if (word.Equals("Howard"))
								{
									cat = ChangeBaseCat(cat, "NNP");
								}
								else
								{
									if (word.Equals("whole"))
									{
										cat = ChangeBaseCat(cat, "JJ");
									}
								}
								break;
							}

							case "JJ":
							{
								if (word.Equals("U.S."))
								{
									cat = ChangeBaseCat(cat, "NNP");
								}
								else
								{
									if (word.Equals("ours"))
									{
										cat = ChangeBaseCat(cat, "PRP");
									}
									else
									{
										if (word.Equals("mine"))
										{
											cat = ChangeBaseCat(cat, "NN");
										}
										else
										{
											if (word.Equals("Sept."))
											{
												cat = ChangeBaseCat(cat, "NNP");
											}
										}
									}
								}
								break;
							}

							case "NN":
							{
								if (word.Equals("Chapman") || word.Equals("Jan.") || word.Equals("Sept.") || word.Equals("Oct.") || word.Equals("Nov.") || word.Equals("Dec."))
								{
									cat = ChangeBaseCat(cat, "NNP");
								}
								else
								{
									if (word.Equals("members") || word.Equals("bureaus") || word.Equals("days") || word.Equals("outfits") || word.Equals("institutes") || word.Equals("innings") || word.Equals("write-offs") || word.Equals("wines") || word.Equals("trade-offs") ||
										 word.Equals("tie-ins") || word.Equals("thrips") || word.Equals("1980s") || word.Equals("1920s"))
									{
										cat = ChangeBaseCat(cat, "NNS");
									}
									else
									{
										if (word.Equals("this"))
										{
											cat = ChangeBaseCat(cat, "DT");
										}
									}
								}
								break;
							}

							case ":":
							{
								if (word.Equals("'"))
								{
									cat = ChangeBaseCat(cat, "''");
								}
								break;
							}

							case "NNS":
							{
								if (word.Equals("start-up") || word.Equals("ground-handling") || word.Equals("word-processing") || word.Equals("T-shirt") || word.Equals("co-pilot"))
								{
									cat = ChangeBaseCat(cat, "NN");
								}
								else
								{
									if (word.Equals("Sens.") || word.Equals("Aichi"))
									{
										cat = ChangeBaseCat(cat, "NNP");
									}
								}
								//not clear why Sens not NNPS
								break;
							}

							case "VBZ":
							{
								if (word.Equals("'s"))
								{
									cat = ChangeBaseCat(cat, "POS");
								}
								else
								{
									if (!word.Equals("kills"))
									{
										// a worse PTB error
										cat = ChangeBaseCat(cat, "NNS");
									}
								}
								break;
							}

							case "VBG":
							{
								if (word.Equals("preferred"))
								{
									cat = ChangeBaseCat(cat, "VBN");
								}
								break;
							}

							case "VB":
							{
								if (word.Equals("The"))
								{
									cat = ChangeBaseCat(cat, "DT");
								}
								else
								{
									if (word.Equals("allowed"))
									{
										cat = ChangeBaseCat(cat, "VBD");
									}
									else
									{
										if (word.Equals("short") || word.Equals("key") || word.Equals("many") || word.Equals("last") || word.Equals("further"))
										{
											cat = ChangeBaseCat(cat, "JJ");
										}
										else
										{
											if (word.Equals("lower"))
											{
												cat = ChangeBaseCat(cat, "JJR");
											}
											else
											{
												if (word.Equals("Nov.") || word.Equals("Jan.") || word.Equals("Dec.") || word.Equals("Tandy") || word.Equals("Release") || word.Equals("Orkem"))
												{
													cat = ChangeBaseCat(cat, "NNP");
												}
												else
												{
													if (word.Equals("watch") || word.Equals("review") || word.Equals("risk") || word.Equals("realestate") || word.Equals("love") || word.Equals("experience") || word.Equals("control") || word.Equals("Transport") || word.Equals("mind") || word.Equals
														("term") || word.Equals("program") || word.Equals("gender") || word.Equals("audit") || word.Equals("blame") || word.Equals("stock") || word.Equals("run") || word.Equals("group") || word.Equals("affect") || word.Equals("rent") || word.Equals
														("show") || word.Equals("accord") || word.Equals("change") || word.Equals("finish") || word.Equals("work") || word.Equals("schedule") || word.Equals("influence") || word.Equals("school") || word.Equals("freight") || word.Equals("growth") ||
														 word.Equals("travel") || word.Equals("call") || word.Equals("autograph") || word.Equals("demand") || word.Equals("abuse") || word.Equals("return") || word.Equals("defeat") || word.Equals("pressure") || word.Equals("bank") || word.Equals("notice"
														) || word.Equals("tax") || word.Equals("ooze") || word.Equals("network") || word.Equals("concern") || word.Equals("pit") || word.Equals("contract") || word.Equals("cash"))
													{
														cat = ChangeBaseCat(cat, "NN");
													}
												}
											}
										}
									}
								}
								break;
							}

							case "NNP":
							{
								if (word.Equals("Officials"))
								{
									cat = ChangeBaseCat(cat, "NNS");
								}
								else
								{
									if (word.Equals("Currently"))
									{
										cat = ChangeBaseCat(cat, "RB");
									}
								}
								// should change NP-TMP to ADVP-TMP here too!
								break;
							}

							case "PRP":
							{
								if (word.Equals("her") && parent.NumChildren() > 1)
								{
									cat = ChangeBaseCat(cat, "PRP$");
								}
								else
								{
									if (word.Equals("US"))
									{
										cat = ChangeBaseCat(cat, "NNP");
									}
								}
								break;
							}
						}
					}
					else
					{
						if (baseParentStr.Equals("WHNP"))
						{
							if (baseCat.Equals("VBP") && (Sharpen.Runtime.EqualsIgnoreCase(word, "that")))
							{
								cat = ChangeBaseCat(cat, "WDT");
							}
						}
						else
						{
							if (baseParentStr.Equals("UCP"))
							{
								if (word.Equals("multifamily"))
								{
									cat = ChangeBaseCat(cat, "NN");
								}
							}
							else
							{
								if (baseParentStr.Equals("PRT"))
								{
									if (baseCat.Equals("RBR") && word.Equals("in"))
									{
										cat = ChangeBaseCat(cat, "RP");
									}
									else
									{
										if (baseCat.Equals("NNP") && word.Equals("up"))
										{
											cat = ChangeBaseCat(cat, "RP");
										}
									}
								}
								else
								{
									if (baseParentStr.Equals("PP"))
									{
										if (parentStr.Equals("PP-TMP"))
										{
											if (baseCat.Equals("RP"))
											{
												cat = ChangeBaseCat(cat, "IN");
											}
										}
										if (word.Equals("in") && (baseCat.Equals("RP") || baseCat.Equals("NN")))
										{
											cat = ChangeBaseCat(cat, "IN");
										}
										else
										{
											if (baseCat.Equals("RB"))
											{
												if (word.Equals("for") || word.Equals("After"))
												{
													cat = ChangeBaseCat(cat, "IN");
												}
											}
											else
											{
												if (word.Equals("if") && baseCat.Equals("JJ"))
												{
													cat = ChangeBaseCat(cat, "IN");
												}
											}
										}
									}
									else
									{
										if (baseParentStr.Equals("VP"))
										{
											if (baseCat.Equals("NNS"))
											{
												cat = ChangeBaseCat(cat, "VBZ");
											}
											else
											{
												if (baseCat.Equals("IN"))
												{
													switch (word)
													{
														case "complicated":
														{
															cat = ChangeBaseCat(cat, "VBD");
															break;
														}

														case "post":
														{
															cat = ChangeBaseCat(cat, "VB");
															break;
														}

														case "like":
														{
															cat = ChangeBaseCat(cat, "VB");
															// most are VB; odd VBP
															break;
														}

														case "off":
														{
															cat = ChangeBaseCat(cat, "RP");
															break;
														}
													}
												}
												else
												{
													if (baseCat.Equals("NN"))
													{
														if (word.EndsWith("ing"))
														{
															cat = ChangeBaseCat(cat, "VBG");
														}
														else
														{
															if (word.Equals("bid"))
															{
																cat = ChangeBaseCat(cat, "VBN");
															}
															else
															{
																if (word.Equals("are"))
																{
																	cat = ChangeBaseCat(cat, "VBP");
																}
																else
																{
																	if (word.Equals("lure"))
																	{
																		cat = ChangeBaseCat(cat, "VB");
																	}
																	else
																	{
																		if (word.Equals("cost"))
																		{
																			cat = ChangeBaseCat(cat, "VBP");
																		}
																		else
																		{
																			if (word.Equals("agreed"))
																			{
																				cat = ChangeBaseCat(cat, "VBN");
																			}
																			else
																			{
																				if (word.Equals("restructure"))
																				{
																					cat = ChangeBaseCat(cat, "VB");
																				}
																				else
																				{
																					if (word.Equals("rule"))
																					{
																						cat = ChangeBaseCat(cat, "VB");
																					}
																					else
																					{
																						if (word.Equals("fret"))
																						{
																							cat = ChangeBaseCat(cat, "VBP");
																						}
																						else
																						{
																							if (word.Equals("retort"))
																							{
																								cat = ChangeBaseCat(cat, "VBP");
																							}
																							else
																							{
																								if (word.Equals("draft"))
																								{
																									cat = ChangeBaseCat(cat, "VB");
																								}
																								else
																								{
																									if (word.Equals("will"))
																									{
																										cat = ChangeBaseCat(cat, "MD");
																									}
																									else
																									{
																										if (word.Equals("yield"))
																										{
																											cat = ChangeBaseCat(cat, "VBP");
																										}
																										else
																										{
																											if (word.Equals("lure"))
																											{
																												cat = ChangeBaseCat(cat, "VBP");
																											}
																											else
																											{
																												if (word.Equals("feel"))
																												{
																													cat = ChangeBaseCat(cat, "VB");
																												}
																												else
																												{
																													if (word.Equals("institutes"))
																													{
																														cat = ChangeBaseCat(cat, "VBZ");
																													}
																													else
																													{
																														if (word.Equals("share"))
																														{
																															cat = ChangeBaseCat(cat, "VBP");
																														}
																														else
																														{
																															if (word.Equals("trade"))
																															{
																																cat = ChangeBaseCat(cat, "VB");
																															}
																															else
																															{
																																if (word.Equals("beat"))
																																{
																																	cat = ChangeBaseCat(cat, "VBN");
																																}
																																else
																																{
																																	if (word.Equals("effect"))
																																	{
																																		cat = ChangeBaseCat(cat, "VB");
																																	}
																																	else
																																	{
																																		if (word.Equals("speed"))
																																		{
																																			cat = ChangeBaseCat(cat, "VB");
																																		}
																																		else
																																		{
																																			if (word.Equals("work"))
																																			{
																																				cat = ChangeBaseCat(cat, "VB");
																																			}
																																			else
																																			{
																																				// though also one VBP
																																				if (word.Equals("act"))
																																				{
																																					cat = ChangeBaseCat(cat, "VBP");
																																				}
																																				else
																																				{
																																					if (word.Equals("drop"))
																																					{
																																						cat = ChangeBaseCat(cat, "VB");
																																					}
																																					else
																																					{
																																						if (word.Equals("stand"))
																																						{
																																							cat = ChangeBaseCat(cat, "VBP");
																																						}
																																						else
																																						{
																																							if (word.Equals("push"))
																																							{
																																								cat = ChangeBaseCat(cat, "VB");
																																							}
																																							else
																																							{
																																								if (word.Equals("service"))
																																								{
																																									cat = ChangeBaseCat(cat, "VB");
																																								}
																																								else
																																								{
																																									if (word.Equals("set"))
																																									{
																																										cat = ChangeBaseCat(cat, "VBN");
																																									}
																																									else
																																									{
																																										// or VBD sometimes, sigh
																																										if (word.Equals("appeal"))
																																										{
																																											cat = ChangeBaseCat(cat, "VBP");
																																										}
																																										else
																																										{
																																											// 2 VBP, 1 VB in train
																																											if (word.Equals("mold"))
																																											{
																																												cat = ChangeBaseCat(cat, "VB");
																																											}
																																											else
																																											{
																																												if (word.Equals("mean"))
																																												{
																																													cat = ChangeBaseCat(cat, "VB");
																																												}
																																												else
																																												{
																																													if (word.Equals("reconfirm"))
																																													{
																																														cat = ChangeBaseCat(cat, "VB");
																																													}
																																													else
																																													{
																																														if (word.Equals("land"))
																																														{
																																															cat = ChangeBaseCat(cat, "VB");
																																														}
																																														else
																																														{
																																															if (word.Equals("point"))
																																															{
																																																cat = ChangeBaseCat(cat, "VBP");
																																															}
																																															else
																																															{
																																																if (word.Equals("rise"))
																																																{
																																																	cat = ChangeBaseCat(cat, "VB");
																																																}
																																																else
																																																{
																																																	if (word.Equals("pressured"))
																																																	{
																																																		cat = ChangeBaseCat(cat, "VBN");
																																																	}
																																																	else
																																																	{
																																																		if (word.Equals("smell"))
																																																		{
																																																			cat = ChangeBaseCat(cat, "VBP");
																																																		}
																																																		else
																																																		{
																																																			if (word.Equals("pay"))
																																																			{
																																																				cat = ChangeBaseCat(cat, "VBP");
																																																			}
																																																			else
																																																			{
																																																				if (word.Equals("hum"))
																																																				{
																																																					cat = ChangeBaseCat(cat, "VB");
																																																				}
																																																				else
																																																				{
																																																					if (word.Equals("shape"))
																																																					{
																																																						cat = ChangeBaseCat(cat, "VBP");
																																																					}
																																																					else
																																																					{
																																																						if (word.Equals("benefit"))
																																																						{
																																																							cat = ChangeBaseCat(cat, "VB");
																																																						}
																																																						else
																																																						{
																																																							if (word.Equals("abducted"))
																																																							{
																																																								cat = ChangeBaseCat(cat, "VBN");
																																																							}
																																																							else
																																																							{
																																																								if (word.Equals("look"))
																																																								{
																																																									cat = ChangeBaseCat(cat, "VB");
																																																								}
																																																								else
																																																								{
																																																									if (word.Equals("fare"))
																																																									{
																																																										cat = ChangeBaseCat(cat, "VB");
																																																									}
																																																									else
																																																									{
																																																										if (word.Equals("change"))
																																																										{
																																																											cat = ChangeBaseCat(cat, "VB");
																																																										}
																																																										else
																																																										{
																																																											if (word.Equals("farm"))
																																																											{
																																																												cat = ChangeBaseCat(cat, "VB");
																																																											}
																																																											else
																																																											{
																																																												if (word.Equals("increase"))
																																																												{
																																																													cat = ChangeBaseCat(cat, "VB");
																																																												}
																																																												else
																																																												{
																																																													if (word.Equals("stem"))
																																																													{
																																																														cat = ChangeBaseCat(cat, "VB");
																																																													}
																																																													else
																																																													{
																																																														// only done 200-700
																																																														if (word.Equals("rebounded"))
																																																														{
																																																															cat = ChangeBaseCat(cat, "VBD");
																																																														}
																																																														else
																																																														{
																																																															if (word.Equals("face"))
																																																															{
																																																																cat = ChangeBaseCat(cat, "VB");
																																																															}
																																																														}
																																																													}
																																																												}
																																																											}
																																																										}
																																																									}
																																																								}
																																																							}
																																																						}
																																																					}
																																																				}
																																																			}
																																																		}
																																																	}
																																																}
																																															}
																																														}
																																													}
																																												}
																																											}
																																										}
																																									}
																																								}
																																							}
																																						}
																																					}
																																				}
																																			}
																																		}
																																	}
																																}
																															}
																														}
																													}
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
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
														if (baseCat.Equals("NNP"))
														{
															switch (word)
															{
																case "GRAB":
																{
																	cat = ChangeBaseCat(cat, "VBP");
																	break;
																}

																case "mature":
																{
																	cat = ChangeBaseCat(cat, "VB");
																	break;
																}

																case "Face":
																{
																	cat = ChangeBaseCat(cat, "VBP");
																	break;
																}

																case "are":
																{
																	cat = ChangeBaseCat(cat, "VBP");
																	break;
																}

																case "Urging":
																{
																	cat = ChangeBaseCat(cat, "VBG");
																	break;
																}

																case "Finding":
																{
																	cat = ChangeBaseCat(cat, "VBG");
																	break;
																}

																case "say":
																{
																	cat = ChangeBaseCat(cat, "VBP");
																	break;
																}

																case "Added":
																{
																	cat = ChangeBaseCat(cat, "VBD");
																	break;
																}

																case "Adds":
																{
																	cat = ChangeBaseCat(cat, "VBZ");
																	break;
																}

																case "BRACED":
																{
																	cat = ChangeBaseCat(cat, "VBD");
																	break;
																}

																case "REQUIRED":
																{
																	cat = ChangeBaseCat(cat, "VBN");
																	break;
																}

																case "SIZING":
																{
																	cat = ChangeBaseCat(cat, "VBG");
																	break;
																}

																case "REVIEW":
																{
																	cat = ChangeBaseCat(cat, "VB");
																	break;
																}

																case "code-named":
																{
																	cat = ChangeBaseCat(cat, "VBN");
																	break;
																}

																case "Printed":
																{
																	cat = ChangeBaseCat(cat, "VBN");
																	break;
																}

																case "Rated":
																{
																	cat = ChangeBaseCat(cat, "VBN");
																	break;
																}

																case "FALTERS":
																{
																	cat = ChangeBaseCat(cat, "VBZ");
																	break;
																}

																case "Got":
																{
																	cat = ChangeBaseCat(cat, "VBN");
																	break;
																}

																case "JUMPING":
																{
																	cat = ChangeBaseCat(cat, "VBG");
																	break;
																}

																case "Branching":
																{
																	cat = ChangeBaseCat(cat, "VBG");
																	break;
																}

																case "Excluding":
																{
																	cat = ChangeBaseCat(cat, "VBG");
																	break;
																}

																case "OKing":
																{
																	cat = ChangeBaseCat(cat, "VBG");
																	break;
																}
															}
														}
														else
														{
															if (baseCat.Equals("POS"))
															{
																cat = ChangeBaseCat(cat, "VBZ");
															}
															else
															{
																if (baseCat.Equals("VBD"))
																{
																	if (word.Equals("heaves"))
																	{
																		cat = ChangeBaseCat(cat, "VBZ");
																	}
																}
																else
																{
																	if (baseCat.Equals("VB"))
																	{
																		if (word.Equals("allowed") || word.Equals("increased"))
																		{
																			cat = ChangeBaseCat(cat, "VBD");
																		}
																	}
																	else
																	{
																		if (baseCat.Equals("VBN"))
																		{
																			if (word.Equals("has"))
																			{
																				cat = ChangeBaseCat(cat, "VBZ");
																			}
																			else
																			{
																				if (word.Equals("grew") || word.Equals("fell"))
																				{
																					cat = ChangeBaseCat(cat, "VBD");
																				}
																			}
																		}
																		else
																		{
																			if (baseCat.Equals("JJ"))
																			{
																				if (word.Equals("own"))
																				{
																					cat = ChangeBaseCat(cat, "VB");
																				}
																			}
																			else
																			{
																				// a couple should actually be VBP, but at least verb is closer
																				if (Sharpen.Runtime.EqualsIgnoreCase(word, "being"))
																				{
																					if (!cat.Equals("VBG"))
																					{
																						cat = ChangeBaseCat(cat, "VBG");
																					}
																				}
																				else
																				{
																					if (Sharpen.Runtime.EqualsIgnoreCase(word, "all"))
																					{
																						cat = ChangeBaseCat(cat, "RB");
																					}
																				}
																			}
																		}
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
											// The below two lines seem in principle good but don't actually
											// improve parser performance; they degrade it on 2200-2219
											// } else if (baseGrandParentStr.equals("NP") && baseCat.equals("VBD")) {
											//   cat = changeBaseCat(cat, "VBN");
											if (baseParentStr.Equals("S"))
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(word, "all"))
												{
													cat = ChangeBaseCat(cat, "RB");
												}
											}
											else
											{
												if (baseParentStr.Equals("ADJP"))
												{
													switch (baseCat)
													{
														case "UH":
														{
															cat = ChangeBaseCat(cat, "JJ");
															break;
														}

														case "JJ":
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(word, "more"))
															{
																cat = ChangeBaseCat(cat, "JJR");
															}
															break;
														}

														case "RB":
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(word, "free"))
															{
																cat = ChangeBaseCat(cat, "JJ");
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(word, "clear"))
																{
																	cat = ChangeBaseCat(cat, "JJ");
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(word, "tight"))
																	{
																		cat = ChangeBaseCat(cat, "JJ");
																	}
																	else
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(word, "sure"))
																		{
																			cat = ChangeBaseCat(cat, "JJ");
																		}
																		else
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(word, "particular"))
																			{
																				cat = ChangeBaseCat(cat, "JJ");
																			}
																		}
																	}
																}
															}
															// most uses of hard/RB should be JJ but not hard put/pressed exx.
															break;
														}

														case "VB":
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(word, "stock"))
															{
																cat = ChangeBaseCat(cat, "NN");
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(word, "secure"))
																{
																	cat = ChangeBaseCat(cat, "JJ");
																}
															}
															break;
														}
													}
												}
												else
												{
													if (baseParentStr.Equals("QP"))
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(word, "about"))
														{
															cat = ChangeBaseCat(cat, "RB");
														}
														else
														{
															if (baseCat.Equals("JJ"))
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(word, "more"))
																{
																	cat = ChangeBaseCat(cat, "JJR");
																}
															}
														}
													}
													else
													{
														// this isn't right for "as much as X" constructions!
														// } else if (word.equalsIgnoreCase("as")) {
														//   cat = changeBaseCat(cat, "RB");
														if (baseParentStr.Equals("ADVP"))
														{
															if (baseCat.Equals("EX"))
															{
																cat = ChangeBaseCat(cat, "RB");
															}
															else
															{
																if (baseCat.Equals("NN") && Sharpen.Runtime.EqualsIgnoreCase(word, "that"))
																{
																	cat = ChangeBaseCat(cat, "DT");
																}
																else
																{
																	if (baseCat.Equals("NNP") && (word.EndsWith("ly") || word.Equals("Overall")))
																	{
																		cat = ChangeBaseCat(cat, "RB");
																	}
																}
															}
														}
														else
														{
															// This should be a sensible thing to do, but hurts on 2200-2219
															// } else if (baseCat.equals("RP") && word.equalsIgnoreCase("around")) {
															//   cat = changeBaseCat(cat, "RB");
															if (baseParentStr.Equals("SBAR"))
															{
																if ((Sharpen.Runtime.EqualsIgnoreCase(word, "that") || Sharpen.Runtime.EqualsIgnoreCase(word, "because") || Sharpen.Runtime.EqualsIgnoreCase(word, "while")) && !baseCat.Equals("IN"))
																{
																	cat = ChangeBaseCat(cat, "IN");
																}
																else
																{
																	if ((word.Equals("Though") || word.Equals("Whether")) && baseCat.Equals("NNP"))
																	{
																		cat = ChangeBaseCat(cat, "IN");
																	}
																}
															}
															else
															{
																if (baseParentStr.Equals("SBARQ"))
																{
																	if (baseCat.Equals("S"))
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(word, "had"))
																		{
																			cat = ChangeBaseCat(cat, "SQ");
																		}
																	}
																}
																else
																{
																	if (baseCat.Equals("JJS"))
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(word, "less"))
																		{
																			cat = ChangeBaseCat(cat, "JJR");
																		}
																	}
																	else
																	{
																		if (baseCat.Equals("JJ"))
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(word, "%"))
																			{
																				// nearly all % are NN, a handful are JJ which we 'correct'
																				cat = ChangeBaseCat(cat, "NN");
																			}
																			else
																			{
																				if (Sharpen.Runtime.EqualsIgnoreCase(word, "to"))
																				{
																					cat = ChangeBaseCat(cat, "TO");
																				}
																			}
																		}
																		else
																		{
																			if (baseCat.Equals("VB"))
																			{
																				if (Sharpen.Runtime.EqualsIgnoreCase(word, "even"))
																				{
																					cat = ChangeBaseCat(cat, "RB");
																				}
																			}
																			else
																			{
																				if (baseCat.Equals(","))
																				{
																					switch (word)
																					{
																						case "2":
																						{
																							cat = ChangeBaseCat(cat, "CD");
																							break;
																						}

																						case "an":
																						{
																							cat = ChangeBaseCat(cat, "DT");
																							break;
																						}

																						case "Wa":
																						{
																							cat = ChangeBaseCat(cat, "NNP");
																							break;
																						}

																						case "section":
																						{
																							cat = ChangeBaseCat(cat, "NN");
																							break;
																						}

																						case "underwriters":
																						{
																							cat = ChangeBaseCat(cat, "NNS");
																							break;
																						}
																					}
																				}
																				else
																				{
																					if (baseCat.Equals("CD"))
																					{
																						if (word.Equals("high-risk"))
																						{
																							cat = ChangeBaseCat(cat, "JJ");
																						}
																					}
																					else
																					{
																						if (baseCat.Equals("RB"))
																						{
																							if (word.Equals("for"))
																							{
																								cat = ChangeBaseCat(cat, "IN");
																							}
																						}
																						else
																						{
																							if (baseCat.Equals("RP"))
																							{
																								if (word.Equals("for"))
																								{
																									cat = ChangeBaseCat(cat, "IN");
																								}
																							}
																							else
																							{
																								if (baseCat.Equals("NN"))
																								{
																									if (word.Length == 2 && word[1] == '.' && char.IsUpperCase(word[0]))
																									{
																										cat = ChangeBaseCat(cat, "NNP");
																									}
																									else
																									{
																										if (word.Equals("Lorillard"))
																										{
																											cat = ChangeBaseCat(cat, "NNP");
																										}
																									}
																								}
																								else
																								{
																									if (word.Equals("for") || word.Equals("at"))
																									{
																										if (!baseCat.Equals("IN"))
																										{
																											// only non-prepositional taggings are mistaken
																											cat = ChangeBaseCat(cat, "IN");
																										}
																									}
																									else
																									{
																										if (Sharpen.Runtime.EqualsIgnoreCase(word, "and") && !baseCat.Equals("CC"))
																										{
																											cat = ChangeBaseCat(cat, "CC");
																										}
																										else
																										{
																											if (word.Equals("ago"))
																											{
																												if (!baseCat.Equals("RB"))
																												{
																													cat = ChangeBaseCat(cat, "RB");
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
					// put correct value into baseCat for later processing!
					baseCat = tlp.BasicCategory(cat);
				}
				if (englishTrain.makePPTOintoIN > 0 && baseCat.Equals("TO"))
				{
					// CONJP is for "not to mention"
					if (!(baseParentStr.Equals("VP") || baseParentStr.Equals("CONJP") || baseParentStr.StartsWith("S")))
					{
						if (englishTrain.makePPTOintoIN == 1)
						{
							cat = ChangeBaseCat(cat, "IN");
						}
						else
						{
							cat = cat + "-IN";
						}
					}
				}
				if (englishTrain.splitIN == 5 && baseCat.Equals("TO"))
				{
					if (grandParentStr[0] == 'N' && (parentStr[0] == 'P' || parentStr[0] == 'A'))
					{
						// noun postmodifier PP (or so-called ADVP like "outside India")
						cat = ChangeBaseCat(cat, "IN") + "-N";
					}
				}
				if (englishTrain.splitIN == 1 && baseCat.Equals("IN") && parentStr[0] == 'S')
				{
					cat = cat + "^S";
				}
				else
				{
					if (englishTrain.splitIN == 2 && baseCat.Equals("IN"))
					{
						if (parentStr[0] == 'S')
						{
							cat = cat + "^S";
						}
						else
						{
							if (grandParentStr[0] == 'N')
							{
								cat = cat + "^N";
							}
						}
					}
					else
					{
						if (englishTrain.splitIN == 3 && baseCat.Equals("IN"))
						{
							// 6 classes seems good!
							// but have played with joining first two, splitting out ADJP/ADVP,
							// and joining two SC cases
							if (grandParentStr[0] == 'N' && (parentStr[0] == 'P' || parentStr[0] == 'A'))
							{
								// noun postmodifier PP (or so-called ADVP like "outside India")
								cat = cat + "-N";
							}
							else
							{
								if (parentStr[0] == 'Q' && (grandParentStr[0] == 'N' || grandParentStr.StartsWith("ADJP")))
								{
									// about, than, between, etc. in a QP preceding head of NP
									cat = cat + "-Q";
								}
								else
								{
									if (grandParentStr.Equals("S"))
									{
										// the distinction here shouldn't matter given parent annotation!
										if (baseParentStr.Equals("SBAR"))
										{
											// sentential subordinating conj: although, if, until, as, while
											cat = cat + "-SCC";
										}
										else
										{
											// PP adverbial clause: among, in, for, after
											cat = cat + "-SC";
										}
									}
									else
									{
										if (baseParentStr.Equals("SBAR") || baseParentStr.Equals("WHNP"))
										{
											// that-clause complement of VP or NP (or whether, if complement)
											// but also VP adverbial because, until, as, etc.
											cat = cat + "-T";
										}
									}
								}
							}
						}
						else
						{
							// all the rest under VP, PP, ADJP, ADVP, etc. are basic case
							if (englishTrain.splitIN >= 4 && englishTrain.splitIN <= 5 && baseCat.Equals("IN"))
							{
								if (grandParentStr[0] == 'N' && (parentStr[0] == 'P' || parentStr[0] == 'A'))
								{
									// noun postmodifier PP (or so-called ADVP like "outside India")
									cat = cat + "-N";
								}
								else
								{
									if (parentStr[0] == 'Q' && (grandParentStr[0] == 'N' || grandParentStr.StartsWith("ADJP")))
									{
										// about, than, between, etc. in a QP preceding head of NP
										cat = cat + "-Q";
									}
									else
									{
										if (baseGrandParentStr[0] == 'S' && !baseGrandParentStr.Equals("SBAR"))
										{
											// the distinction here shouldn't matter given parent annotation!
											if (baseParentStr.Equals("SBAR"))
											{
												// sentential subordinating conj: although, if, until, as, while
												cat = cat + "-SCC";
											}
											else
											{
												if (!baseParentStr.Equals("NP") && !baseParentStr.Equals("ADJP"))
												{
													// PP adverbial clause: among, in, for, after
													cat = cat + "-SC";
												}
											}
										}
										else
										{
											if (baseParentStr.Equals("SBAR") || baseParentStr.Equals("WHNP") || baseParentStr.Equals("WHADVP"))
											{
												// that-clause complement of VP or NP (or whether, if complement)
												// but also VP adverbial because, until, as, etc.
												cat = cat + "-T";
											}
										}
									}
								}
							}
							else
							{
								// all the rest under VP, PP, ADJP, ADVP, etc. are basic case
								if (englishTrain.splitIN == 6 && baseCat.Equals("IN"))
								{
									if (grandParentStr[0] == 'V' || grandParentStr[0] == 'A')
									{
										cat = cat + "-V";
									}
									else
									{
										if (grandParentStr[0] == 'N' && (parentStr[0] == 'P' || parentStr[0] == 'A'))
										{
										}
										else
										{
											// noun postmodifier PP (or so-called ADVP like "outside India")
											// XXX experiment cat = cat + "-N";
											if (parentStr[0] == 'Q' && (grandParentStr[0] == 'N' || grandParentStr.StartsWith("ADJP")))
											{
												// about, than, between, etc. in a QP preceding head of NP
												cat = cat + "-Q";
											}
											else
											{
												if (baseGrandParentStr[0] == 'S' && !baseGrandParentStr.Equals("SBAR"))
												{
													// the distinction here shouldn't matter given parent annotation!
													if (baseParentStr.Equals("SBAR"))
													{
														// sentential subordinating conj: although, if, until, as, while
														cat = cat + "-SCC";
													}
													else
													{
														if (!baseParentStr.Equals("NP") && !baseParentStr.Equals("ADJP"))
														{
															// PP adverbial clause: among, in, for, after
															cat = cat + "-SC";
														}
													}
												}
												else
												{
													if (baseParentStr.Equals("SBAR") || baseParentStr.Equals("WHNP") || baseParentStr.Equals("WHADVP"))
													{
														// that-clause complement of VP or NP (or whether, if complement)
														// but also VP adverbial because, until, as, etc.
														cat = cat + "-T";
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				// all the rest under VP, PP, ADJP, ADVP, etc. are basic case
				if (englishTrain.splitPercent && word.Equals("%"))
				{
					cat += "-%";
				}
				if (englishTrain.splitNNP > 0 && baseCat.StartsWith("NNP"))
				{
					if (englishTrain.splitNNP == 1)
					{
						if (baseCat.Equals("NNP"))
						{
							if (parent.NumChildren() == 1)
							{
								cat += "-S";
							}
							else
							{
								if (parent.FirstChild().Equals(t))
								{
									cat += "-L";
								}
								else
								{
									if (parent.LastChild().Equals(t))
									{
										cat += "-R";
									}
									else
									{
										cat += "-I";
									}
								}
							}
						}
					}
					else
					{
						if (englishTrain.splitNNP == 2)
						{
							if (word.Matches("[A-Z]\\.?"))
							{
								cat = cat + "-I";
							}
							else
							{
								if (FirstOfSeveralNNP(parent, t))
								{
									cat = cat + "-B";
								}
								else
								{
									if (LastOfSeveralNNP(parent, t))
									{
										cat = cat + "-E";
									}
								}
							}
						}
					}
				}
				if (englishTrain.splitQuotes && (word.Equals("'") || word.Equals("`")))
				{
					cat += "-SG";
				}
				if (englishTrain.splitSFP && baseTag.Equals("."))
				{
					if (word.Equals("?"))
					{
						cat += "-QUES";
					}
					else
					{
						if (word.Equals("!"))
						{
							cat += "-EXCL";
						}
					}
				}
				if (englishTrain.tagRBGPA)
				{
					if (baseCat.Equals("RB"))
					{
						cat = cat + "^" + baseGrandParentStr;
					}
				}
				if (englishTrain.joinPound && baseCat.Equals("#"))
				{
					cat = ChangeBaseCat(cat, "$");
				}
				if (englishTrain.joinNounTags)
				{
					if (baseCat.Equals("NNP"))
					{
						cat = ChangeBaseCat(cat, "NN");
					}
					else
					{
						if (baseCat.Equals("NNPS"))
						{
							cat = ChangeBaseCat(cat, "NNS");
						}
					}
				}
				if (englishTrain.joinJJ && cat.StartsWith("JJ"))
				{
					cat = ChangeBaseCat(cat, "JJ");
				}
				if (englishTrain.splitPPJJ && cat.StartsWith("JJ") && parentStr.StartsWith("PP"))
				{
					cat = cat + "^S";
				}
				if (englishTrain.splitTRJJ && cat.StartsWith("JJ") && (parentStr.StartsWith("PP") || parentStr.StartsWith("ADJP")) && HeadFinder().DetermineHead(parent) == t)
				{
					// look for NP right sister of head JJ -- if so transitive adjective
					Tree[] kids = parent.Children();
					bool foundJJ = false;
					int i = 0;
					for (; i < kids.Length && !foundJJ; i++)
					{
						if (kids[i].Label().Value().StartsWith("JJ"))
						{
							foundJJ = true;
						}
					}
					if (foundJJ)
					{
						for (int j = i; j < kids.Length; j++)
						{
							if (kids[j].Label().Value().StartsWith("NP"))
							{
								cat = cat + "^T";
								break;
							}
						}
					}
				}
				if (englishTrain.splitJJCOMP && cat.StartsWith("JJ") && (parentStr.StartsWith("PP") || parentStr.StartsWith("ADJP")) && HeadFinder().DetermineHead(parent) == t)
				{
					Tree[] kids = parent.Children();
					int i = 0;
					for (bool foundJJ = false; i < kids.Length && !foundJJ; i++)
					{
						if (kids[i].Label().Value().StartsWith("JJ"))
						{
							foundJJ = true;
						}
					}
					for (int j = i; j < kids.Length; j++)
					{
						string kid = tlp.BasicCategory(kids[j].Label().Value());
						if ("S".Equals(kid) || "SBAR".Equals(kid) || "PP".Equals(kid) || "NP".Equals(kid))
						{
							// there's a complement.
							cat = cat + "^CMPL";
							break;
						}
					}
				}
				if (englishTrain.splitMoreLess)
				{
					char ch = cat[0];
					if (ch == 'R' || ch == 'J' || ch == 'C')
					{
						// adverbs, adjectives and coordination -- what you'd expect
						if (Sharpen.Runtime.EqualsIgnoreCase(word, "more") || Sharpen.Runtime.EqualsIgnoreCase(word, "most") || Sharpen.Runtime.EqualsIgnoreCase(word, "less") || Sharpen.Runtime.EqualsIgnoreCase(word, "least"))
						{
							cat = cat + "-ML";
						}
					}
				}
				if (englishTrain.unaryDT && cat.StartsWith("DT"))
				{
					if (parent.Children().Length == 1)
					{
						cat = cat + "^U";
					}
				}
				if (englishTrain.unaryRB && cat.StartsWith("RB"))
				{
					if (parent.Children().Length == 1)
					{
						cat = cat + "^U";
					}
				}
				if (englishTrain.markReflexivePRP && cat.StartsWith("PRP"))
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(word, "itself") || Sharpen.Runtime.EqualsIgnoreCase(word, "themselves") || Sharpen.Runtime.EqualsIgnoreCase(word, "himself") || Sharpen.Runtime.EqualsIgnoreCase(word, "herself") || Sharpen.Runtime.EqualsIgnoreCase
						(word, "ourselves") || Sharpen.Runtime.EqualsIgnoreCase(word, "yourself") || Sharpen.Runtime.EqualsIgnoreCase(word, "yourselves") || Sharpen.Runtime.EqualsIgnoreCase(word, "myself") || Sharpen.Runtime.EqualsIgnoreCase(word, "thyself"))
					{
						cat += "-SE";
					}
				}
				if (englishTrain.unaryPRP && cat.StartsWith("PRP"))
				{
					if (parent.Children().Length == 1)
					{
						cat = cat + "^U";
					}
				}
				if (englishTrain.unaryIN && cat.StartsWith("IN"))
				{
					if (parent.Children().Length == 1)
					{
						cat = cat + "^U";
					}
				}
				if (englishTrain.splitCC > 0 && baseCat.Equals("CC"))
				{
					if (englishTrain.splitCC == 1 && (word.Equals("and") || word.Equals("or")))
					{
						cat = cat + "-C";
					}
					else
					{
						if (englishTrain.splitCC == 2)
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(word, "but"))
							{
								cat = cat + "-B";
							}
							else
							{
								if (word.Equals("&"))
								{
									cat = cat + "-A";
								}
							}
						}
						else
						{
							if (englishTrain.splitCC == 3 && Sharpen.Runtime.EqualsIgnoreCase(word, "and"))
							{
								cat = cat + "-A";
							}
						}
					}
				}
				if (englishTrain.splitNOT && baseCat.Equals("RB") && (Sharpen.Runtime.EqualsIgnoreCase(word, "n't") || Sharpen.Runtime.EqualsIgnoreCase(word, "not") || Sharpen.Runtime.EqualsIgnoreCase(word, "nt")))
				{
					cat = cat + "-N";
				}
				else
				{
					if (englishTrain.splitRB && baseCat.Equals("RB") && (baseParentStr.Equals("NP") || baseParentStr.Equals("QP") || baseParentStr.Equals("ADJP")))
					{
						cat = cat + "^M";
					}
				}
				if (englishTrain.splitAux > 1 && (baseCat.Equals("VBZ") || baseCat.Equals("VBP") || baseCat.Equals("VBD") || baseCat.Equals("VBN") || baseCat.Equals("VBG") || baseCat.Equals("VB")))
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(word, "'s") || Sharpen.Runtime.EqualsIgnoreCase(word, "s"))
					{
						// a few times the apostrophe is missing!
						Tree[] sisters = parent.Children();
						int i = 0;
						for (bool foundMe = false; i < sisters.Length && !foundMe; i++)
						{
							if (sisters[i].Label().Value().StartsWith("VBZ"))
							{
								foundMe = true;
							}
						}
						bool annotateHave = false;
						// VBD counts as an erroneous VBN!
						for (int j = i; j < sisters.Length; j++)
						{
							if (sisters[j].Label().Value().StartsWith("VP"))
							{
								foreach (Tree kid in sisters[j].Children())
								{
									if (kid.Label().Value().StartsWith("VBN") || kid.Label().Value().StartsWith("VBD"))
									{
										annotateHave = true;
									}
								}
							}
						}
						if (annotateHave)
						{
							cat = cat + "-HV";
						}
						else
						{
							// System.out.println("Went with HAVE for " + parent);
							cat = cat + "-BE";
						}
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(word, "am") || Sharpen.Runtime.EqualsIgnoreCase(word, "is") || Sharpen.Runtime.EqualsIgnoreCase(word, "are") || Sharpen.Runtime.EqualsIgnoreCase(word, "was") || Sharpen.Runtime.EqualsIgnoreCase(word, "were"
							) || Sharpen.Runtime.EqualsIgnoreCase(word, "'m") || Sharpen.Runtime.EqualsIgnoreCase(word, "'re") || Sharpen.Runtime.EqualsIgnoreCase(word, "be") || Sharpen.Runtime.EqualsIgnoreCase(word, "being") || Sharpen.Runtime.EqualsIgnoreCase(word, 
							"been") || Sharpen.Runtime.EqualsIgnoreCase(word, "ai"))
						{
							// allow "ai n't"
							cat = cat + "-BE";
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(word, "have") || Sharpen.Runtime.EqualsIgnoreCase(word, "'ve") || Sharpen.Runtime.EqualsIgnoreCase(word, "having") || Sharpen.Runtime.EqualsIgnoreCase(word, "has") || Sharpen.Runtime.EqualsIgnoreCase(word
								, "had") || Sharpen.Runtime.EqualsIgnoreCase(word, "'d"))
							{
								cat = cat + "-HV";
							}
							else
							{
								if (englishTrain.splitAux >= 3 && (Sharpen.Runtime.EqualsIgnoreCase(word, "do") || Sharpen.Runtime.EqualsIgnoreCase(word, "did") || Sharpen.Runtime.EqualsIgnoreCase(word, "does") || Sharpen.Runtime.EqualsIgnoreCase(word, "done") || Sharpen.Runtime.EqualsIgnoreCase
									(word, "doing")))
								{
									// both DO and HELP take VB form complement VP
									cat = cat + "-DO";
								}
								else
								{
									if (englishTrain.splitAux >= 4 && (Sharpen.Runtime.EqualsIgnoreCase(word, "help") || Sharpen.Runtime.EqualsIgnoreCase(word, "helps") || Sharpen.Runtime.EqualsIgnoreCase(word, "helped") || Sharpen.Runtime.EqualsIgnoreCase(word, "helping")))
									{
										// both DO and HELP take VB form complement VP
										cat = cat + "-DO";
									}
									else
									{
										if (englishTrain.splitAux >= 5 && (Sharpen.Runtime.EqualsIgnoreCase(word, "let") || Sharpen.Runtime.EqualsIgnoreCase(word, "lets") || Sharpen.Runtime.EqualsIgnoreCase(word, "letting")))
										{
											// LET also takes VB form complement VP
											cat = cat + "-DO";
										}
										else
										{
											if (englishTrain.splitAux >= 6 && (Sharpen.Runtime.EqualsIgnoreCase(word, "make") || Sharpen.Runtime.EqualsIgnoreCase(word, "makes") || Sharpen.Runtime.EqualsIgnoreCase(word, "making") || Sharpen.Runtime.EqualsIgnoreCase(word, "made")))
											{
												// MAKE can also take VB form complement VP
												cat = cat + "-DO";
											}
											else
											{
												if (englishTrain.splitAux >= 7 && (Sharpen.Runtime.EqualsIgnoreCase(word, "watch") || Sharpen.Runtime.EqualsIgnoreCase(word, "watches") || Sharpen.Runtime.EqualsIgnoreCase(word, "watching") || Sharpen.Runtime.EqualsIgnoreCase(word, "watched"
													) || Sharpen.Runtime.EqualsIgnoreCase(word, "see") || Sharpen.Runtime.EqualsIgnoreCase(word, "sees") || Sharpen.Runtime.EqualsIgnoreCase(word, "seeing") || Sharpen.Runtime.EqualsIgnoreCase(word, "saw") || Sharpen.Runtime.EqualsIgnoreCase(word
													, "seen")))
												{
													// WATCH, SEE can also take VB form complement VP
													cat = cat + "-DO";
												}
												else
												{
													if (englishTrain.splitAux >= 8 && (Sharpen.Runtime.EqualsIgnoreCase(word, "go") || Sharpen.Runtime.EqualsIgnoreCase(word, "come")))
													{
														// go, come, but not inflections can also take VB form complement VP
														cat = cat + "-DO";
													}
													else
													{
														if (englishTrain.splitAux >= 9 && (Sharpen.Runtime.EqualsIgnoreCase(word, "get") || Sharpen.Runtime.EqualsIgnoreCase(word, "gets") || Sharpen.Runtime.EqualsIgnoreCase(word, "getting") || Sharpen.Runtime.EqualsIgnoreCase(word, "got") || Sharpen.Runtime.EqualsIgnoreCase
															(word, "gotten")))
														{
															// GET also takes a VBN form complement VP
															cat = cat + "-BE";
														}
													}
												}
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
					if (englishTrain.splitAux > 0 && (baseCat.Equals("VBZ") || baseCat.Equals("VBP") || baseCat.Equals("VBD") || baseCat.Equals("VBN") || baseCat.Equals("VBG") || baseCat.Equals("VB")))
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(word, "is") || Sharpen.Runtime.EqualsIgnoreCase(word, "am") || Sharpen.Runtime.EqualsIgnoreCase(word, "are") || Sharpen.Runtime.EqualsIgnoreCase(word, "was") || Sharpen.Runtime.EqualsIgnoreCase(word, "were"
							) || Sharpen.Runtime.EqualsIgnoreCase(word, "'m") || Sharpen.Runtime.EqualsIgnoreCase(word, "'re") || Sharpen.Runtime.EqualsIgnoreCase(word, "'s") || Sharpen.Runtime.EqualsIgnoreCase(word, "being") || Sharpen.Runtime.EqualsIgnoreCase(word, 
							"be") || Sharpen.Runtime.EqualsIgnoreCase(word, "been"))
						{
							// imperfect -- could be (ha)s
							cat = cat + "-BE";
						}
						if (Sharpen.Runtime.EqualsIgnoreCase(word, "have") || Sharpen.Runtime.EqualsIgnoreCase(word, "'ve") || Sharpen.Runtime.EqualsIgnoreCase(word, "having") || Sharpen.Runtime.EqualsIgnoreCase(word, "has") || Sharpen.Runtime.EqualsIgnoreCase(word
							, "had") || Sharpen.Runtime.EqualsIgnoreCase(word, "'d"))
						{
							cat = cat + "-HV";
						}
					}
				}
				if (englishTrain.collapseWhCategories != 0)
				{
					if ((englishTrain.collapseWhCategories & 1) != 0)
					{
						cat = cat.ReplaceAll("WH(NP|PP|ADVP|ADJP)", "$1");
					}
					if ((englishTrain.collapseWhCategories & 2) != 0)
					{
						cat = cat.ReplaceAll("WP", "PRP");
						// does both WP and WP$ !!
						cat = cat.ReplaceAll("WDT", "DT");
						cat = cat.ReplaceAll("WRB", "RB");
					}
					if ((englishTrain.collapseWhCategories & 4) != 0)
					{
						cat = cat.ReplaceAll("WH(PP|ADVP|ADJP)", "$1");
					}
				}
				// don't do NP, so it is preserved! Crucial.
				if (englishTrain.markDitransV > 0 && cat.StartsWith("VB"))
				{
					cat += Ditrans(parent);
				}
				else
				{
					if (englishTrain.vpSubCat && cat.StartsWith("VB"))
					{
						cat = cat + SubCatify(parent);
					}
				}
				// VITAL: update tag to be same as cat for when new node is created below
				tag = cat;
			}
			else
			{
				// that is, if (t.isPhrasal())
				Tree[] kids = t.Children();
				if (baseCat.Equals("VP"))
				{
					if (englishTrain.gpaRootVP)
					{
						if (tlp.IsStartSymbol(baseGrandParentStr))
						{
							cat = cat + "~ROOT";
						}
					}
					if (englishTrain.splitVPNPAgr)
					{
						switch (baseTag)
						{
							case "VBD":
							case "MD":
							{
								// don't split on weirdo categories!
								// but do preserve agreement distinctions
								// note MD is like VBD -- any subject person/number okay
								cat = cat + "-VBF";
								break;
							}

							case "VBZ":
							case "TO":
							case "VBG":
							case "VBP":
							case "VBN":
							case "VB":
							{
								cat = cat + "-" + baseTag;
								break;
							}

							default:
							{
								log.Info("XXXX Head of " + t + " is " + word + "/" + baseTag);
								break;
							}
						}
					}
					else
					{
						if (englishTrain.splitVP == 3 || englishTrain.splitVP == 4)
						{
							// don't split on weirdo categories but deduce
							if (baseTag.Equals("VBZ") || baseTag.Equals("VBD") || baseTag.Equals("VBP") || baseTag.Equals("MD"))
							{
								cat = cat + "-VBF";
							}
							else
							{
								if (baseTag.Equals("TO") || baseTag.Equals("VBG") || baseTag.Equals("VBN") || baseTag.Equals("VB"))
								{
									cat = cat + "-" + baseTag;
								}
								else
								{
									if (englishTrain.splitVP == 4)
									{
										string dTag = DeduceTag(word);
										cat = cat + "-" + dTag;
									}
								}
							}
						}
						else
						{
							if (englishTrain.splitVP == 2)
							{
								if (baseTag.Equals("VBZ") || baseTag.Equals("VBD") || baseTag.Equals("VBP") || baseTag.Equals("MD"))
								{
									cat = cat + "-VBF";
								}
								else
								{
									cat = cat + "-" + baseTag;
								}
							}
							else
							{
								if (englishTrain.splitVP == 1)
								{
									cat = cat + "-" + baseTag;
								}
							}
						}
					}
				}
				if (englishTrain.dominatesV > 0)
				{
					if (englishTrain.dominatesV == 2)
					{
						if (HasClausalV(t))
						{
							cat = cat + "-v";
						}
					}
					else
					{
						if (englishTrain.dominatesV == 3)
						{
							if (HasV(t.PreTerminalYield()) && !baseCat.Equals("WHPP") && !baseCat.Equals("RRC") && !baseCat.Equals("QP") && !baseCat.Equals("PRT"))
							{
								cat = cat + "-v";
							}
						}
						else
						{
							if (HasV(t.PreTerminalYield()))
							{
								cat = cat + "-v";
							}
						}
					}
				}
				if (englishTrain.dominatesI && HasI(t.PreTerminalYield()))
				{
					cat = cat + "-i";
				}
				if (englishTrain.dominatesC && HasC(t.PreTerminalYield()))
				{
					cat = cat + "-c";
				}
				if (englishTrain.splitNPpercent > 0 && word.Equals("%"))
				{
					if (baseCat.Equals("NP") || englishTrain.splitNPpercent > 1 && baseCat.Equals("ADJP") || englishTrain.splitNPpercent > 2 && baseCat.Equals("QP") || englishTrain.splitNPpercent > 3)
					{
						cat += "-%";
					}
				}
				if (englishTrain.splitNPPRP && baseTag.Equals("PRP"))
				{
					cat += "-PRON";
				}
				if (englishTrain.splitSbar > 0 && baseCat.Equals("SBAR"))
				{
					bool foundIn = false;
					bool foundOrder = false;
					bool infinitive = baseTag.Equals("TO");
					foreach (Tree kid in kids)
					{
						if (kid.IsPreTerminal() && Sharpen.Runtime.EqualsIgnoreCase(kid.Children()[0].Value(), "in"))
						{
							foundIn = true;
						}
						if (kid.IsPreTerminal() && Sharpen.Runtime.EqualsIgnoreCase(kid.Children()[0].Value(), "order"))
						{
							foundOrder = true;
						}
					}
					if (englishTrain.splitSbar > 1 && infinitive)
					{
						cat = cat + "-INF";
					}
					if ((englishTrain.splitSbar == 1 || englishTrain.splitSbar == 3) && foundIn && foundOrder)
					{
						cat = cat + "-PURP";
					}
				}
				if (englishTrain.splitNPNNP > 0)
				{
					if (englishTrain.splitNPNNP == 1 && baseCat.Equals("NP") && baseTag.Equals("NNP"))
					{
						cat = cat + "-NNP";
					}
					else
					{
						if (englishTrain.splitNPNNP == 2 && baseCat.Equals("NP") && baseTag.StartsWith("NNP"))
						{
							cat = cat + "-NNP";
						}
						else
						{
							if (englishTrain.splitNPNNP == 3 && baseCat.Equals("NP"))
							{
								bool split = false;
								foreach (Tree kid in kids)
								{
									if (kid.Value().StartsWith("NNP"))
									{
										split = true;
										break;
									}
								}
								if (split)
								{
									cat = cat + "-NNP";
								}
							}
						}
					}
				}
				if (englishTrain.collapseWhCategories != 0)
				{
					if ((englishTrain.collapseWhCategories & 1) != 0)
					{
						cat = cat.ReplaceAll("WH(NP|PP|ADVP|ADJP)", "$1");
					}
					if ((englishTrain.collapseWhCategories & 2) != 0)
					{
						cat = cat.ReplaceAll("WP", "PRP");
						// does both WP and WP$ !!
						cat = cat.ReplaceAll("WDT", "DT");
						cat = cat.ReplaceAll("WRB", "RB");
					}
					if ((englishTrain.collapseWhCategories & 4) != 0)
					{
						cat = cat.ReplaceAll("WH(PP|ADVP|ADJP)", "$1");
					}
				}
				// don't do NP, so it is preserved! Crucial.
				if (englishTrain.splitVPNPAgr && baseCat.Equals("NP") && baseParentStr.StartsWith("S"))
				{
					if (baseTag.Equals("NNPS") || baseTag.Equals("NNS"))
					{
						cat = cat + "-PL";
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(word, "many") || Sharpen.Runtime.EqualsIgnoreCase(word, "more") || Sharpen.Runtime.EqualsIgnoreCase(word, "most") || Sharpen.Runtime.EqualsIgnoreCase(word, "plenty"))
						{
							cat = cat + "-PL";
						}
						else
						{
							if (baseTag.Equals("NN") || baseTag.Equals("NNP") || baseTag.Equals("POS") || baseTag.Equals("CD") || baseTag.Equals("PRP$") || baseTag.Equals("JJ") || baseTag.Equals("EX") || baseTag.Equals("$") || baseTag.Equals("RB") || baseTag.Equals("FW"
								) || baseTag.Equals("VBG") || baseTag.Equals("JJS") || baseTag.Equals("JJR"))
							{
							}
							else
							{
								if (baseTag.Equals("PRP"))
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(word, "they") || Sharpen.Runtime.EqualsIgnoreCase(word, "them") || Sharpen.Runtime.EqualsIgnoreCase(word, "we") || Sharpen.Runtime.EqualsIgnoreCase(word, "us"))
									{
										cat = cat + "-PL";
									}
								}
								else
								{
									if (baseTag.Equals("DT") || baseTag.Equals("WDT"))
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(word, "these") || Sharpen.Runtime.EqualsIgnoreCase(word, "those") || Sharpen.Runtime.EqualsIgnoreCase(word, "several"))
										{
											cat += "-PL";
										}
									}
									else
									{
										log.Info("XXXX Head of " + t + " is " + word + "/" + baseTag);
									}
								}
							}
						}
					}
				}
				if (englishTrain.splitSTag > 0 && (baseCat.Equals("S") || (englishTrain.splitSTag <= 3 && (baseCat.Equals("SINV") || baseCat.Equals("SQ")))))
				{
					if (englishTrain.splitSTag == 1)
					{
						cat = cat + "-" + baseTag;
					}
					else
					{
						if (baseTag.Equals("VBZ") || baseTag.Equals("VBD") || baseTag.Equals("VBP") || baseTag.Equals("MD"))
						{
							cat = cat + "-VBF";
						}
						else
						{
							if ((englishTrain.splitSTag == 3 || englishTrain.splitSTag == 5) && ((baseTag.Equals("TO") || baseTag.Equals("VBG") || baseTag.Equals("VBN") || baseTag.Equals("VB"))))
							{
								cat = cat + "-VBNF";
							}
						}
					}
				}
				if (englishTrain.markContainedVP && ContainsVP(t))
				{
					cat = cat + "-vp";
				}
				if (englishTrain.markCC > 0)
				{
					// was: for (int i = 0; i < kids.length; i++) {
					// This second version takes an idea from Collins: don't count
					// marginal conjunctions which don't conjoin 2 things.
					for (int i = 1; i < kids.Length - 1; i++)
					{
						string cat2 = kids[i].Label().Value();
						if (cat2.StartsWith("CC"))
						{
							string word2 = kids[i].Children()[0].Value();
							// get word
							// added this if since -acl03pcfg
							if (!(word2.Equals("either") || word2.Equals("both") || word2.Equals("neither")))
							{
								cat = cat + "-CC";
								break;
							}
						}
						else
						{
							// log.info("XXX Found non-marginal either/both/neither");
							if (englishTrain.markCC > 1 && cat2.StartsWith("CONJP"))
							{
								cat = cat + "-CC";
								break;
							}
						}
					}
				}
				if (englishTrain.splitSGapped == 1 && baseCat.Equals("S") && !kids[0].Label().Value().StartsWith("NP"))
				{
					// this doesn't handle predicative NPs right yet
					// to do that, need to intervene before tree normalization
					cat = cat + "-G";
				}
				else
				{
					if (englishTrain.splitSGapped == 2 && baseCat.Equals("S"))
					{
						// better version: you're gapped if there is no NP, or there is just
						// one (putatively predicative) NP with no VP, ADJP, NP, PP, or UCP
						bool seenPredCat = false;
						int seenNP = 0;
						foreach (Tree kid in kids)
						{
							string cat2 = kid.Label().Value();
							if (cat2.StartsWith("NP"))
							{
								seenNP++;
							}
							else
							{
								if (cat2.StartsWith("VP") || cat2.StartsWith("ADJP") || cat2.StartsWith("PP") || cat2.StartsWith("UCP"))
								{
									seenPredCat = true;
								}
							}
						}
						if (seenNP == 0 || (seenNP == 1 && !seenPredCat))
						{
							cat = cat + "-G";
						}
					}
					else
					{
						if (englishTrain.splitSGapped == 3 && baseCat.Equals("S"))
						{
							// better version: you're gapped if there is no NP, or there is just
							// one (putatively predicative) NP with no VP, ADJP, NP, PP, or UCP
							// NEW: but you're not gapped if you have an S and CC daughter (coord)
							bool seenPredCat = false;
							bool seenCC = false;
							bool seenS = false;
							int seenNP = 0;
							foreach (Tree kid in kids)
							{
								string cat2 = kid.Label().Value();
								if (cat2.StartsWith("NP"))
								{
									seenNP++;
								}
								else
								{
									if (cat2.StartsWith("VP") || cat2.StartsWith("ADJP") || cat2.StartsWith("PP") || cat2.StartsWith("UCP"))
									{
										seenPredCat = true;
									}
									else
									{
										if (cat2.StartsWith("CC"))
										{
											seenCC = true;
										}
										else
										{
											if (cat2.StartsWith("S"))
											{
												seenS = true;
											}
										}
									}
								}
							}
							if ((!(seenCC && seenS)) && (seenNP == 0 || (seenNP == 1 && !seenPredCat)))
							{
								cat = cat + "-G";
							}
						}
						else
						{
							if (englishTrain.splitSGapped == 4 && baseCat.Equals("S"))
							{
								// better version: you're gapped if there is no NP, or there is just
								// one (putatively predicative) NP with no VP, ADJP, NP, PP, or UCP
								// But: not gapped if S(BAR)-NOM-SBJ constituent
								// But: you're not gapped if you have two /^S/ daughters
								bool seenPredCat = false;
								bool sawSBeforePredCat = false;
								int seenS = 0;
								int seenNP = 0;
								foreach (Tree kid in kids)
								{
									string cat2 = kid.Label().Value();
									if (cat2.StartsWith("NP"))
									{
										seenNP++;
									}
									else
									{
										if (cat2.StartsWith("VP") || cat2.StartsWith("ADJP") || cat2.StartsWith("PP") || cat2.StartsWith("UCP"))
										{
											seenPredCat = true;
										}
										else
										{
											if (cat2.StartsWith("S"))
											{
												seenS++;
												if (!seenPredCat)
												{
													sawSBeforePredCat = true;
												}
											}
										}
									}
								}
								if ((seenS < 2) && (!(sawSBeforePredCat && seenPredCat)) && (seenNP == 0 || (seenNP == 1 && !seenPredCat)))
								{
									cat = cat + "-G";
								}
							}
						}
					}
				}
				if (englishTrain.splitNumNP && baseCat.Equals("NP"))
				{
					bool seenNum = false;
					foreach (Tree kid in kids)
					{
						string cat2 = kid.Label().Value();
						if (cat2.StartsWith("QP") || cat2.StartsWith("CD") || cat2.StartsWith("$") || cat2.StartsWith("#") || (cat2.StartsWith("NN") && cat2.Contains("-%")))
						{
							seenNum = true;
							break;
						}
					}
					if (seenNum)
					{
						cat += "-NUM";
					}
				}
				if (englishTrain.splitPoss > 0 && baseCat.Equals("NP") && kids[kids.Length - 1].Label().Value().StartsWith("POS"))
				{
					if (englishTrain.splitPoss == 2)
					{
						// special case splice in a new node!  Do it all here
						ILabel labelBot;
						if (t.IsPrePreTerminal())
						{
							labelBot = new CategoryWordTag("NP^POSSP-B", word, tag);
						}
						else
						{
							labelBot = new CategoryWordTag("NP^POSSP", word, tag);
						}
						t.SetLabel(labelBot);
						IList<Tree> oldKids = t.GetChildrenAsList();
						// could I use subList() here or is a true copy better?
						// lose the last child
						IList<Tree> newKids = new List<Tree>();
						for (int i = 0; i < oldKids.Count - 1; i++)
						{
							newKids.Add(oldKids[i]);
						}
						t.SetChildren(newKids);
						cat = ChangeBaseCat(cat, "POSSP");
						ILabel labelTop = new CategoryWordTag(cat, word, tag);
						IList<Tree> newerChildren = new List<Tree>(2);
						newerChildren.Add(t);
						// add POS dtr
						Tree last = oldKids[oldKids.Count - 1];
						if (!last.Value().Equals("POS^NP"))
						{
							log.Info("Unexpected POS value (!): " + last);
						}
						last.SetValue("POS^POSSP");
						newerChildren.Add(last);
						return categoryWordTagTreeFactory.NewTreeNode(labelTop, newerChildren);
					}
					else
					{
						cat = cat + "-P";
					}
				}
				if (englishTrain.splitBaseNP > 0 && baseCat.Equals("NP") && t.IsPrePreTerminal())
				{
					if (englishTrain.splitBaseNP == 2)
					{
						if (parentStr.StartsWith("NP"))
						{
							// already got one above us
							cat = cat + "-B";
						}
						else
						{
							// special case splice in a new node!  Do it all here
							ILabel labelBot = new CategoryWordTag("NP^NP-B", word, tag);
							t.SetLabel(labelBot);
							ILabel labelTop = new CategoryWordTag(cat, word, tag);
							IList<Tree> newerChildren = new List<Tree>(1);
							newerChildren.Add(t);
							return categoryWordTagTreeFactory.NewTreeNode(labelTop, newerChildren);
						}
					}
					else
					{
						cat = cat + "-B";
					}
				}
				if (englishTrain.rightPhrasal && RightPhrasal(t))
				{
					cat = cat + "-RX";
				}
			}
			t.SetLabel(new CategoryWordTag(cat, word, tag));
			return t;
		}

		private bool ContainsVP(Tree t)
		{
			string cat = tlp.BasicCategory(t.Label().Value());
			if (cat.Equals("VP"))
			{
				return true;
			}
			else
			{
				foreach (Tree kid in t.Children())
				{
					if (ContainsVP(kid))
					{
						return true;
					}
				}
				return false;
			}
		}

		private static bool FirstOfSeveralNNP(Tree parent, Tree t)
		{
			bool firstIsT = false;
			int numNNP = 0;
			foreach (Tree kid in parent.Children())
			{
				if (kid.Value().StartsWith("NNP"))
				{
					if (t.Equals(kid) && numNNP == 0)
					{
						firstIsT = true;
					}
					numNNP++;
				}
			}
			return numNNP > 1 && firstIsT;
		}

		private static bool LastOfSeveralNNP(Tree parent, Tree t)
		{
			Tree last = null;
			int numNNP = 0;
			foreach (Tree kid in parent.Children())
			{
				if (kid.Value().StartsWith("NNP"))
				{
					numNNP++;
					last = kid;
				}
			}
			return numNNP > 1 && t.Equals(last);
		}

		// quite heuristic, but not useless given tagging errors?
		private static string DeduceTag(string w)
		{
			string word = w.ToLower();
			if (word.EndsWith("ing"))
			{
				return "VBG";
			}
			else
			{
				if (word.EndsWith("d") || word.EndsWith("t"))
				{
					return "VBN";
				}
				else
				{
					if (word.EndsWith("s"))
					{
						return "VBZ";
					}
					else
					{
						if (word.Equals("to"))
						{
							return "TO";
						}
						else
						{
							return "VB";
						}
					}
				}
			}
		}

		private static bool RightPhrasal(Tree t)
		{
			while (!t.IsLeaf())
			{
				t = t.LastChild();
				string str = t.Label().Value();
				if (str.StartsWith("NP") || str.StartsWith("PP") || str.StartsWith("VP") || str.StartsWith("S") || str.StartsWith("Q") || str.StartsWith("A"))
				{
					return true;
				}
			}
			return false;
		}

		private static string SubCatify(Tree t)
		{
			StringBuilder sb = new StringBuilder("^a");
			bool n = false;
			bool s = false;
			bool p = false;
			for (int i = 0; i < t.Children().Length; i++)
			{
				string childStr = t.Children()[i].Label().Value();
				n = (n || childStr.StartsWith("NP"));
				s = (s || childStr.StartsWith("S"));
				p = (p || childStr.StartsWith("PP"));
			}
			n = false;
			if (n)
			{
				sb.Append('N');
			}
			if (p)
			{
				sb.Append('P');
			}
			if (s)
			{
				sb.Append('S');
			}
			return sb.ToString();
		}

		private string Ditrans(Tree t)
		{
			int n = 0;
			foreach (Tree kid in t.Children())
			{
				string childStr = kid.Label().Value();
				if (childStr.StartsWith("NP") && !childStr.Contains("-TMP"))
				{
					n++;
				}
				else
				{
					if (englishTrain.markDitransV == 1 && childStr.StartsWith("S"))
					{
						n++;
					}
				}
			}
			if (n >= 2)
			{
				return "^2Arg";
			}
			else
			{
				return string.Empty;
			}
		}

		private string ChangeBaseCat(string cat, string newBaseCat)
		{
			int i = 1;
			// not 0 in case tag is annotation introducing char
			int length = cat.Length;
			for (; (i < length); i++)
			{
				if (tlp.IsLabelAnnotationIntroducingCharacter(cat[i]))
				{
					break;
				}
			}
			if (i < length)
			{
				return newBaseCat + Sharpen.Runtime.Substring(cat, i);
			}
			else
			{
				return newBaseCat;
			}
		}

		/// <summary>
		/// This version doesn't count verbs in baseNPs: they're generally
		/// gerunds in compounds like "operating income".
		/// </summary>
		/// <remarks>
		/// This version doesn't count verbs in baseNPs: they're generally
		/// gerunds in compounds like "operating income".  It would also
		/// catch modal tagging mistakes like "May/MD 15".
		/// </remarks>
		/// <param name="tree">A tree to assess</param>
		/// <returns>true if there is a verb or modal, not within a base NP</returns>
		private static bool HasClausalV(Tree tree)
		{
			// this is originally called only called on phrasal nodes
			if (tree.IsPhrasal())
			{
				if (tree.IsPrePreTerminal() && tree.Value().StartsWith("NP"))
				{
					return false;
				}
				Tree[] kids = tree.Children();
				foreach (Tree t in kids)
				{
					if (HasClausalV(t))
					{
						return true;
					}
				}
				return false;
			}
			else
			{
				string str = tree.Value();
				return str.StartsWith("VB") || str.StartsWith("MD");
			}
		}

		private static bool HasV<_T0>(IList<_T0> tags)
			where _T0 : ILabel
		{
			foreach (ILabel tag in tags)
			{
				string str = tag.ToString();
				if (str.StartsWith("V") || str.StartsWith("MD"))
				{
					return true;
				}
			}
			return false;
		}

		private static bool HasI<_T0>(IList<_T0> tags)
			where _T0 : ILabel
		{
			foreach (ILabel tag in tags)
			{
				if (tag.ToString().StartsWith("I"))
				{
					return true;
				}
			}
			return false;
		}

		private static bool HasC<_T0>(IList<_T0> tags)
			where _T0 : ILabel
		{
			foreach (ILabel tag in tags)
			{
				if (tag.ToString().StartsWith("CC"))
				{
					return true;
				}
			}
			return false;
		}

		public override void Display()
		{
			englishTrain.Display();
		}

		/// <summary>Set language-specific options according to flags.</summary>
		/// <remarks>
		/// Set language-specific options according to flags.
		/// This routine should process the option starting in args[i] (which
		/// might potentially be several arguments long if it takes arguments).
		/// It should return the index after the last index it consumed in
		/// processing.  In particular, if it cannot process the current option,
		/// the return value should be i.
		/// </remarks>
		public override int SetOptionFlag(string[] args, int i)
		{
			// [CDM 2008: there are no generic options!] first, see if it's a generic option
			// int j = super.setOptionFlag(args, i);
			// if(i != j) return j;
			//lang. specific options
			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitIN"))
			{
				englishTrain.splitIN = System.Convert.ToInt32(args[i + 1]);
				i += 2;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitPercent"))
				{
					englishTrain.splitPercent = true;
					i += 1;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitQuotes"))
					{
						englishTrain.splitQuotes = true;
						i += 1;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitSFP"))
						{
							englishTrain.splitSFP = true;
							i += 1;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitNNP"))
							{
								englishTrain.splitNNP = System.Convert.ToInt32(args[i + 1]);
								i += 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-rbGPA"))
								{
									englishTrain.tagRBGPA = true;
									i += 1;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitTRJJ"))
									{
										englishTrain.splitTRJJ = true;
										i += 1;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitJJCOMP"))
										{
											englishTrain.splitJJCOMP = true;
											i += 1;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitMoreLess"))
											{
												englishTrain.splitMoreLess = true;
												i += 1;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unaryDT"))
												{
													englishTrain.unaryDT = true;
													i += 1;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unaryRB"))
													{
														englishTrain.unaryRB = true;
														i += 1;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unaryIN"))
														{
															englishTrain.unaryIN = true;
															i += 1;
														}
														else
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markReflexivePRP"))
															{
																englishTrain.markReflexivePRP = true;
																i += 1;
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitCC") && i + 1 < args.Length)
																{
																	englishTrain.splitCC = System.Convert.ToInt32(args[i + 1]);
																	i += 2;
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitRB"))
																	{
																		englishTrain.splitRB = true;
																		i += 1;
																	}
																	else
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitAux") && i + 1 < args.Length)
																		{
																			englishTrain.splitAux = System.Convert.ToInt32(args[i + 1]);
																			i += 2;
																		}
																		else
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitSbar") && i + 1 < args.Length)
																			{
																				englishTrain.splitSbar = System.Convert.ToInt32(args[i + 1]);
																				i += 2;
																			}
																			else
																			{
																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitVP") && i + 1 < args.Length)
																				{
																					englishTrain.splitVP = System.Convert.ToInt32(args[i + 1]);
																					i += 2;
																				}
																				else
																				{
																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitVPNPAgr"))
																					{
																						englishTrain.splitVPNPAgr = true;
																						i += 1;
																					}
																					else
																					{
																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-gpaRootVP"))
																						{
																							englishTrain.gpaRootVP = true;
																							i += 1;
																						}
																						else
																						{
																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-makePPTOintoIN"))
																							{
																								englishTrain.makePPTOintoIN = System.Convert.ToInt32(args[i + 1]);
																								i += 2;
																							}
																							else
																							{
																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-collapseWhCategories") && i + 1 < args.Length)
																								{
																									englishTrain.collapseWhCategories = System.Convert.ToInt32(args[i + 1]);
																									i += 2;
																								}
																								else
																								{
																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitSTag"))
																									{
																										englishTrain.splitSTag = System.Convert.ToInt32(args[i + 1]);
																										i += 2;
																									}
																									else
																									{
																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitSGapped") && (i + 1 < args.Length))
																										{
																											englishTrain.splitSGapped = System.Convert.ToInt32(args[i + 1]);
																											i += 2;
																										}
																										else
																										{
																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitNPpercent") && (i + 1 < args.Length))
																											{
																												englishTrain.splitNPpercent = System.Convert.ToInt32(args[i + 1]);
																												i += 2;
																											}
																											else
																											{
																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitNPPRP"))
																												{
																													englishTrain.splitNPPRP = true;
																													i += 1;
																												}
																												else
																												{
																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-dominatesV") && (i + 1 < args.Length))
																													{
																														englishTrain.dominatesV = System.Convert.ToInt32(args[i + 1]);
																														i += 2;
																													}
																													else
																													{
																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-dominatesI"))
																														{
																															englishTrain.dominatesI = true;
																															i += 1;
																														}
																														else
																														{
																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-dominatesC"))
																															{
																																englishTrain.dominatesC = true;
																																i += 1;
																															}
																															else
																															{
																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitNPNNP") && (i + 1 < args.Length))
																																{
																																	englishTrain.splitNPNNP = System.Convert.ToInt32(args[i + 1]);
																																	i += 2;
																																}
																																else
																																{
																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitTMP") && (i + 1 < args.Length))
																																	{
																																		englishTrain.splitTMP = System.Convert.ToInt32(args[i + 1]);
																																		i += 2;
																																	}
																																	else
																																	{
																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitNPADV") && (i + 1 < args.Length))
																																		{
																																			englishTrain.splitNPADV = System.Convert.ToInt32(args[i + 1]);
																																			i += 2;
																																		}
																																		else
																																		{
																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markContainedVP"))
																																			{
																																				englishTrain.markContainedVP = true;
																																				i += 1;
																																			}
																																			else
																																			{
																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markDitransV") && (i + 1 < args.Length))
																																				{
																																					englishTrain.markDitransV = System.Convert.ToInt32(args[i + 1]);
																																					i += 2;
																																				}
																																				else
																																				{
																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitPoss") && (i + 1 < args.Length))
																																					{
																																						englishTrain.splitPoss = System.Convert.ToInt32(args[i + 1]);
																																						i += 2;
																																					}
																																					else
																																					{
																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-baseNP") && (i + 1 < args.Length))
																																						{
																																							englishTrain.splitBaseNP = System.Convert.ToInt32(args[i + 1]);
																																							i += 2;
																																						}
																																						else
																																						{
																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-joinNounTags"))
																																							{
																																								englishTrain.joinNounTags = true;
																																								i += 1;
																																							}
																																							else
																																							{
																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-correctTags"))
																																								{
																																									englishTrain.correctTags = true;
																																									i += 1;
																																								}
																																								else
																																								{
																																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noCorrectTags"))
																																									{
																																										englishTrain.correctTags = false;
																																										i += 1;
																																									}
																																									else
																																									{
																																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markCC") && (i + 1 < args.Length))
																																										{
																																											englishTrain.markCC = System.Convert.ToInt32(args[i + 1]);
																																											i += 2;
																																										}
																																										else
																																										{
																																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noAnnotations"))
																																											{
																																												englishTrain.splitVP = 0;
																																												englishTrain.splitTMP = NPTmpRetainingTreeNormalizer.TemporalNone;
																																												englishTrain.splitSGapped = 0;
																																												i += 1;
																																											}
																																											else
																																											{
																																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-retainNPTMPSubcategories"))
																																												{
																																													englishTest.retainNPTMPSubcategories = true;
																																													i += 1;
																																												}
																																												else
																																												{
																																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-retainTMPSubcategories"))
																																													{
																																														englishTest.retainTMPSubcategories = true;
																																														i += 1;
																																													}
																																													else
																																													{
																																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-retainADVSubcategories"))
																																														{
																																															englishTest.retainADVSubcategories = true;
																																															i += 1;
																																														}
																																														else
																																														{
																																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-leaveItAll") && (i + 1 < args.Length))
																																															{
																																																englishTrain.leaveItAll = System.Convert.ToInt32(args[i + 1]);
																																																i += 2;
																																															}
																																															else
																																															{
																																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-headFinder") && (i + 1 < args.Length))
																																																{
																																																	try
																																																	{
																																																		headFinder = (IHeadFinder)System.Activator.CreateInstance(Sharpen.Runtime.GetType(args[i + 1]));
																																																	}
																																																	catch (Exception e)
																																																	{
																																																		log.Info("Error: Unable to load HeadFinder; default HeadFinder will be used.");
																																																		Sharpen.Runtime.PrintStackTrace(e);
																																																	}
																																																	i += 2;
																																																}
																																																else
																																																{
																																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-makeCopulaHead"))
																																																	{
																																																		englishTest.makeCopulaHead = true;
																																																		i += 1;
																																																	}
																																																	else
																																																	{
																																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-originalDependencies"))
																																																		{
																																																			SetGenerateOriginalDependencies(true);
																																																			i += 1;
																																																		}
																																																		else
																																																		{
																																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-acl03pcfg"))
																																																			{
																																																				englishTrain.splitIN = 3;
																																																				englishTrain.splitPercent = true;
																																																				englishTrain.splitPoss = 1;
																																																				englishTrain.splitCC = 2;
																																																				englishTrain.unaryDT = true;
																																																				englishTrain.unaryRB = true;
																																																				englishTrain.splitAux = 1;
																																																				englishTrain.splitVP = 2;
																																																				englishTrain.splitSGapped = 3;
																																																				englishTrain.dominatesV = 1;
																																																				englishTrain.splitTMP = NPTmpRetainingTreeNormalizer.TemporalAcl03pcfg;
																																																				englishTrain.splitBaseNP = 1;
																																																				i += 1;
																																																			}
																																																			else
																																																			{
																																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-jenny"))
																																																				{
																																																					englishTrain.splitIN = 3;
																																																					englishTrain.splitPercent = true;
																																																					englishTrain.splitPoss = 1;
																																																					englishTrain.splitCC = 2;
																																																					englishTrain.unaryDT = true;
																																																					englishTrain.unaryRB = true;
																																																					englishTrain.splitAux = 1;
																																																					englishTrain.splitVP = 2;
																																																					englishTrain.splitSGapped = 3;
																																																					englishTrain.dominatesV = 1;
																																																					englishTrain.splitTMP = NPTmpRetainingTreeNormalizer.TemporalAcl03pcfg;
																																																					englishTrain.splitBaseNP = 1;
																																																					i += 1;
																																																				}
																																																				else
																																																				{
																																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-linguisticPCFG"))
																																																					{
																																																						englishTrain.splitIN = 3;
																																																						englishTrain.splitPercent = true;
																																																						englishTrain.splitPoss = 1;
																																																						englishTrain.splitCC = 2;
																																																						englishTrain.unaryDT = true;
																																																						englishTrain.unaryRB = true;
																																																						englishTrain.splitAux = 2;
																																																						englishTrain.splitVP = 3;
																																																						englishTrain.splitSGapped = 4;
																																																						englishTrain.dominatesV = 0;
																																																						// not for linguistic
																																																						englishTrain.splitTMP = NPTmpRetainingTreeNormalizer.TemporalAcl03pcfg;
																																																						englishTrain.splitBaseNP = 1;
																																																						englishTrain.splitMoreLess = true;
																																																						englishTrain.correctTags = true;
																																																						// different from acl03pcfg
																																																						i += 1;
																																																					}
																																																					else
																																																					{
																																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-goodPCFG"))
																																																						{
																																																							englishTrain.splitIN = 4;
																																																							// different from acl03pcfg
																																																							englishTrain.splitPercent = true;
																																																							englishTrain.splitNPpercent = 0;
																																																							// no longer different from acl03pcfg
																																																							englishTrain.splitPoss = 1;
																																																							englishTrain.splitCC = 1;
																																																							englishTrain.unaryDT = true;
																																																							englishTrain.unaryRB = true;
																																																							englishTrain.splitAux = 2;
																																																							// different from acl03pcfg
																																																							englishTrain.splitVP = 3;
																																																							// different from acl03pcfg
																																																							englishTrain.splitSGapped = 4;
																																																							englishTrain.dominatesV = 1;
																																																							englishTrain.splitTMP = NPTmpRetainingTreeNormalizer.TemporalAcl03pcfg;
																																																							englishTrain.splitNPADV = 1;
																																																							// different from acl03pcfg
																																																							englishTrain.splitBaseNP = 1;
																																																							// englishTrain.splitMoreLess = true;   // different from acl03pcfg
																																																							englishTrain.correctTags = true;
																																																							// different from acl03pcfg
																																																							englishTrain.markDitransV = 2;
																																																							// different from acl03pcfg
																																																							i += 1;
																																																						}
																																																						else
																																																						{
																																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-ijcai03"))
																																																							{
																																																								englishTrain.splitIN = 3;
																																																								englishTrain.splitPercent = true;
																																																								englishTrain.splitPoss = 1;
																																																								englishTrain.splitCC = 2;
																																																								englishTrain.unaryDT = false;
																																																								englishTrain.unaryRB = false;
																																																								englishTrain.splitAux = 0;
																																																								englishTrain.splitVP = 2;
																																																								englishTrain.splitSGapped = 4;
																																																								englishTrain.dominatesV = 0;
																																																								englishTrain.splitTMP = NPTmpRetainingTreeNormalizer.TemporalAcl03pcfg;
																																																								englishTrain.splitBaseNP = 1;
																																																								i += 1;
																																																							}
																																																							else
																																																							{
																																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-goodFactored"))
																																																								{
																																																									englishTrain.splitIN = 3;
																																																									englishTrain.splitPercent = true;
																																																									englishTrain.splitPoss = 1;
																																																									englishTrain.splitCC = 2;
																																																									englishTrain.unaryDT = false;
																																																									englishTrain.unaryRB = false;
																																																									englishTrain.splitAux = 0;
																																																									englishTrain.splitVP = 3;
																																																									// different from ijcai03
																																																									englishTrain.splitSGapped = 4;
																																																									englishTrain.dominatesV = 0;
																																																									englishTrain.splitTMP = NPTmpRetainingTreeNormalizer.TemporalAcl03pcfg;
																																																									englishTrain.splitBaseNP = 1;
																																																									// BAD!! englishTrain.markCC = 1;  // different from ijcai03
																																																									englishTrain.correctTags = true;
																																																									// different from ijcai03
																																																									i += 1;
																																																								}
																																																							}
																																																						}
																																																					}
																																																				}
																																																			}
																																																		}
																																																	}
																																																}
																																															}
																																														}
																																													}
																																												}
																																											}
																																										}
																																									}
																																								}
																																							}
																																						}
																																					}
																																				}
																																			}
																																		}
																																	}
																																}
																															}
																														}
																													}
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return i;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<IHasWord> DefaultTestSentence()
		{
			IList<Word> ret = new List<Word>();
			string[] sent = new string[] { "This", "is", "just", "a", "test", "." };
			foreach (string str in sent)
			{
				ret.Add(new Word(str));
			}
			return ret;
		}

		public override IList<GrammaticalStructure> ReadGrammaticalStructureFromFile(string filename)
		{
			try
			{
				if (generateOriginalDependencies)
				{
					return EnglishGrammaticalStructure.ReadCoNLLXGrammaticalStructureCollection(filename);
				}
				else
				{
					return UniversalEnglishGrammaticalStructure.ReadCoNLLXGrammaticalStructureCollection(filename);
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public override GrammaticalStructure GetGrammaticalStructure(Tree t, IPredicate<string> filter, IHeadFinder hf)
		{
			if (generateOriginalDependencies)
			{
				return new EnglishGrammaticalStructure(t, filter, hf);
			}
			else
			{
				return new UniversalEnglishGrammaticalStructure(t, filter, hf);
			}
		}

		public override bool SupportsBasicDependencies()
		{
			return true;
		}

		private static readonly string[] RetainTmpArgs = new string[] { "-retainTmpSubcategories" };

		public override string[] DefaultCoreNLPFlags()
		{
			return RetainTmpArgs;
		}

		public static void Main(string[] args)
		{
			ITreebankLangParserParams tlpp = new EnglishTreebankParserParams();
			Treebank tb = tlpp.MemoryTreebank();
			tb.LoadPath(args[0]);
			foreach (Tree t in tb)
			{
				t.PennPrint();
			}
		}

		private const long serialVersionUID = 4153878351331522581L;
	}
}
