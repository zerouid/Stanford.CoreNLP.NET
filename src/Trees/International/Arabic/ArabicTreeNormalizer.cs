using System.Collections.Generic;
using Edu.Stanford.Nlp.International.Arabic.Pipeline;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Treebank;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util.Function;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Arabic
{
	/// <summary>
	/// Normalizes both terminals and non-terminals in Penn Arabic Treebank (ATB)
	/// trees.
	/// </summary>
	/// <remarks>
	/// Normalizes both terminals and non-terminals in Penn Arabic Treebank (ATB)
	/// trees. Among the normalizations that can be performed:
	/// <ul>
	/// <li> Adds a ROOT node to the top of every tree
	/// <li> Strips all the interesting stuff off of the POS tags.
	/// <li> Can keep NP-TMP annotations (retainNPTmp parameter)
	/// <li> Can keep whatever annotations there are on verbs that are sisters
	/// to predicatively marked (-PRD) elements (markPRDverb parameter)
	/// [Chris Nov 2006: I'm a bit unsure on that one!]
	/// <li> Can keep categories unchanged, i.e., not mapped to basic categories
	/// (changeNoLabels parameter)
	/// <li> Counts pronoun deletions ("nullp" and "_") as empty; filters
	/// </ul>
	/// </remarks>
	/// <author>Roger Levy</author>
	/// <author>Anna Rafferty</author>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class ArabicTreeNormalizer : BobChrisTreeNormalizer
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Arabic.ArabicTreeNormalizer));

		private readonly bool retainNPTmp;

		private readonly bool retainNPSbj;

		private readonly bool markPRDverb;

		private readonly bool changeNoLabels;

		private readonly bool retainPPClr;

		private readonly Pattern prdPattern;

		private readonly TregexPattern prdVerbPattern;

		private readonly TregexPattern npSbjPattern;

		private readonly string rootLabel;

		private readonly IMapper lexMapper = new DefaultLexicalMapper();

		public ArabicTreeNormalizer(bool retainNPTmp, bool markPRDverb, bool changeNoLabels, bool retainNPSbj, bool retainPPClr)
			: base(new ArabicTreebankLanguagePack())
		{
			this.retainNPTmp = retainNPTmp;
			this.retainNPSbj = retainNPSbj;
			this.markPRDverb = markPRDverb;
			this.changeNoLabels = changeNoLabels;
			this.retainPPClr = retainPPClr;
			rootLabel = tlp.StartSymbol();
			prdVerbPattern = TregexPattern.Compile("/^V[^P]/ > VP $ /-PRD$/=prd");
			prdPattern = Pattern.Compile("^[A-Z]+-PRD");
			//Marks NP subjects that *do not* occur in verb-initial clauses
			npSbjPattern = TregexPattern.Compile("/^NP-SBJ/ !> @VP");
			emptyFilter = new ArabicTreeNormalizer.ArabicEmptyFilter();
		}

		public ArabicTreeNormalizer(bool retainNPTmp, bool markPRDverb, bool changeNoLabels)
			: this(retainNPTmp, markPRDverb, changeNoLabels, false, false)
		{
		}

		public ArabicTreeNormalizer(bool retainNPTmp, bool markPRDverb)
			: this(retainNPTmp, markPRDverb, false)
		{
		}

		public ArabicTreeNormalizer(bool retainNPTmp)
			: this(retainNPTmp, false)
		{
		}

		public ArabicTreeNormalizer()
			: this(false)
		{
		}

		public override string NormalizeNonterminal(string category)
		{
			string normalizedString;
			if (changeNoLabels)
			{
				normalizedString = category;
			}
			else
			{
				if (retainNPTmp && category != null && category.StartsWith("NP-TMP"))
				{
					normalizedString = "NP-TMP";
				}
				else
				{
					if (retainNPSbj && category != null && category.StartsWith("NP-SBJ"))
					{
						normalizedString = "NP-SBJ";
					}
					else
					{
						if (retainPPClr && category != null && category.StartsWith("PP-CLR"))
						{
							normalizedString = "PP-CLR";
						}
						else
						{
							if (markPRDverb && category != null && prdPattern.Matcher(category).Matches())
							{
								normalizedString = category;
							}
							else
							{
								// otherwise, return the basicCategory (and turn null to ROOT)
								normalizedString = base.NormalizeNonterminal(category);
							}
						}
					}
				}
			}
			return string.Intern(normalizedString);
		}

		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			tree = tree.Prune(emptyFilter, tf).SpliceOut(aOverAFilter, tf);
			foreach (Tree t in tree)
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
							log.Err(string.Format("%s: Word contains malformed morph annotation: %s", this.GetType().FullName, t.Value()));
						}
						else
						{
							if (t.Label() is CoreLabel)
							{
								CoreLabel cl = (CoreLabel)t.Label();
								cl.SetValue(string.Intern(toks[0].Trim()));
								cl.SetWord(string.Intern(toks[0].Trim()));
								Pair<string, string> lemmaMorph = MorphoFeatureSpecification.SplitMorphString(toks[0], toks[1]);
								string lemma = lemmaMorph.First();
								string morphAnalysis = lemmaMorph.Second();
								if (lemma.Equals(toks[0]))
								{
									cl.SetOriginalText(string.Intern(toks[1].Trim()));
								}
								else
								{
									// TODO(spenceg): Does this help?
									string newLemma = lexMapper.Map(null, lemma);
									if (newLemma == null || newLemma.Trim().IsEmpty())
									{
										newLemma = lemma;
									}
									string newMorphAnalysis = newLemma + MorphoFeatureSpecification.LemmaMark + morphAnalysis;
									cl.SetOriginalText(string.Intern(newMorphAnalysis));
								}
							}
							else
							{
								log.Error(string.Format("%s: Cannot store morph analysis in non-CoreLabel: %s", this.GetType().FullName, t.Label().GetType().FullName));
							}
						}
					}
				}
				else
				{
					if (t.IsPreTerminal())
					{
						if (t.Value() == null || t.Value().IsEmpty())
						{
							log.Warn(string.Format("%s: missing tag for %s", this.GetType().FullName, t.PennString()));
						}
						else
						{
							if (t.Label() is IHasTag)
							{
								((IHasTag)t.Label()).SetTag(t.Value());
							}
						}
					}
					else
					{
						//Phrasal nodes
						// there are some nodes "/" missing preterminals.  We'll splice in a tag for these.
						int nk = t.NumChildren();
						IList<Tree> newKids = new List<Tree>(nk);
						for (int j = 0; j < nk; j++)
						{
							Tree child = t.GetChild(j);
							if (child.IsLeaf())
							{
								log.Warn(string.Format("%s: Splicing in DUMMYTAG for %s", this.GetType().FullName, t.ToString()));
								newKids.Add(tf.NewTreeNode("DUMMYTAG", Java.Util.Collections.SingletonList(child)));
							}
							else
							{
								newKids.Add(child);
							}
						}
						t.SetChildren(newKids);
					}
				}
			}
			//Every node in the tree has now been processed
			//
			// Additional processing for specific phrasal annotations
			//
			// special global coding for moving PRD annotation from constituent to verb tag.
			if (markPRDverb)
			{
				TregexMatcher m = prdVerbPattern.Matcher(tree);
				Tree match = null;
				while (m.Find())
				{
					if (m.GetMatch() != match)
					{
						match = m.GetMatch();
						match.Label().SetValue(match.Label().Value() + "-PRDverb");
						Tree prd = m.GetNode("prd");
						prd.Label().SetValue(base.NormalizeNonterminal(prd.Label().Value()));
					}
				}
			}
			//Mark *only* subjects in verb-initial clauses
			if (retainNPSbj)
			{
				TregexMatcher m = npSbjPattern.Matcher(tree);
				while (m.Find())
				{
					Tree match = m.GetMatch();
					match.Label().SetValue("NP");
				}
			}
			if (tree.IsPreTerminal())
			{
				// The whole tree is a bare tag: bad!
				string val = tree.Label().Value();
				if (val.Equals("CC") || val.StartsWith("PUNC") || val.Equals("CONJ"))
				{
					log.Warn(string.Format("%s: Bare tagged word being wrapped in FRAG %s", this.GetType().FullName, tree.PennString()));
					tree = tf.NewTreeNode("FRAG", Java.Util.Collections.SingletonList(tree));
				}
				else
				{
					log.Warn(string.Format("%s: Bare tagged word %s", this.GetType().FullName, tree.PennString()));
				}
			}
			//Add start symbol so that the root has only one sub-state. Escape any enclosing brackets.
			//If the "tree" consists entirely of enclosing brackets e.g. ((())) then this method
			//will return null. In this case, readers e.g. PennTreeReader will try to read the next tree.
			while (tree != null && (tree.Value() == null || tree.Value().IsEmpty()) && tree.NumChildren() <= 1)
			{
				tree = tree.FirstChild();
			}
			if (tree != null && !tree.Value().Equals(rootLabel))
			{
				tree = tf.NewTreeNode(rootLabel, Java.Util.Collections.SingletonList(tree));
			}
			return tree;
		}

		/// <summary>Remove traces and pronoun deletion markers.</summary>
		[System.Serializable]
		public class ArabicEmptyFilter : IPredicate<Tree>
		{
			private const long serialVersionUID = 7417844982953945964L;

			public virtual bool Test(Tree t)
			{
				// Pronoun deletions
				if (t.IsPreTerminal() && (t.Value().Equals("PRON_1S") || t.Value().Equals("PRP")) && (t.FirstChild().Value().Equals("nullp") || t.FirstChild().Value().Equals("نللة") || t.FirstChild().Value().Equals("-~a")))
				{
					return false;
				}
				else
				{
					// Traces
					if (t.IsPreTerminal() && t.Value() != null && t.Value().Equals("-NONE-"))
					{
						return false;
					}
				}
				return true;
			}
		}

		private const long serialVersionUID = -1592231121068698494L;
	}
}
