using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Implements a 'semantic head' variant of the the HeadFinder found
	/// in Michael Collins' 1999 thesis.
	/// </summary>
	/// <remarks>
	/// Implements a 'semantic head' variant of the the HeadFinder found
	/// in Michael Collins' 1999 thesis.
	/// This version chooses the semantic head verb rather than the verb form
	/// for cases with verbs.  And it makes similar themed changes to other
	/// categories: e.g., in question phrases, like "Which Brazilian game", the
	/// head is made "game" not "Which" as in common PTB head rules.
	/// <p>
	/// By default the SemanticHeadFinder uses a treatment of copula where the
	/// complement of the copula is taken as the head.  That is, a sentence like
	/// "Bill is big" will be analyzed as:
	/// <p>
	/// <c>nsubj</c>
	/// (big, Bill) <br />
	/// <c>cop</c>
	/// (big, is)
	/// <p>
	/// This analysis is used for questions and declaratives for adjective
	/// complements and declarative nominal complements.  However Wh-sentences
	/// with nominal complements do not receive this treatment.
	/// "Who is the president?" is analyzed with "the president" as nsubj and "who"
	/// as "attr" of the copula:
	/// <p>
	/// <c>nsubj</c>
	/// (is, president)<br />
	/// <c>attr</c>
	/// (is, Who)
	/// <p>
	/// (Such nominal copula sentences are complex: arguably, depending on the
	/// circumstances, several analyses are possible, with either the overt NP able
	/// to be any of the subject, the predicate, or one of two referential entities
	/// connected by an equational copula.  These uses aren't differentiated.)
	/// <p>
	/// Existential sentences are treated as follows:
	/// <p>
	/// "There is a man" <br />
	/// <c>expl</c>
	/// (is, There) <br />
	/// <c>det</c>
	/// (man-4, a-3) <br />
	/// <c>nsubj</c>
	/// (is-2, man-4)<br />
	/// </remarks>
	/// <author>John Rappaport</author>
	/// <author>Marie-Catherine de Marneffe</author>
	/// <author>Anna Rafferty</author>
	/// <author>Sebastian Schuster</author>
	[System.Serializable]
	public class UniversalSemanticHeadFinder : ModCollinsHeadFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.UniversalSemanticHeadFinder));

		private static readonly bool Debug = Runtime.GetProperty("SemanticHeadFinder", null) != null;

		private static readonly string[] auxiliaries = new string[] { "will", "wo", "shall", "sha", "may", "might", "should", "would", "can", "could", "ca", "must", "'ll", "ll", "-ll", "cold", "has", "have", "had", "having", "'ve", "ve", "v", "of", 
			"hav", "hvae", "as", "get", "gets", "getting", "got", "gotten", "do", "does", "did", "'d", "d", "du", "to", "2", "na", "a", "ot", "ta", "the", "too" };

		private static readonly string[] verbTags = new string[] { "TO", "MD", "VB", "VBD", "VBP", "VBZ", "VBG", "VBN", "AUX", "AUXG" };

		private static readonly string[] unambiguousAuxTags = new string[] { "TO", "MD", "AUX", "AUXG" };

		private readonly ICollection<string> verbalAuxiliaries;

		private readonly ICollection<string> copulars;

		private readonly ICollection<string> passiveAuxiliaries;

		private readonly ICollection<string> verbalTags;

		private readonly ICollection<string> unambiguousAuxiliaryTags;

		private readonly bool makeCopulaHead;

		public UniversalSemanticHeadFinder()
			: this(new PennTreebankLanguagePack(), true)
		{
		}

		public UniversalSemanticHeadFinder(bool noCopulaHead)
			: this(new PennTreebankLanguagePack(), noCopulaHead)
		{
		}

		/// <summary>Create a SemanticHeadFinder.</summary>
		/// <param name="tlp">
		/// The TreebankLanguagePack, used by the superclass to get basic
		/// category of constituents.
		/// </param>
		/// <param name="noCopulaHead">
		/// If true, a copular verb (a form of be)
		/// is not treated as head when it has an AdjP or NP complement.  If false,
		/// a copula verb is still always treated as a head.  But it will still
		/// be treated as an auxiliary in periphrastic tenses with a VP complement.
		/// </param>
		public UniversalSemanticHeadFinder(ITreebankLanguagePack tlp, bool noCopulaHead)
			: base(tlp)
		{
			/* A few times the apostrophe is missing on "'s", so we have "s" */
			/* Tricky auxiliaries: "a", "na" is from "(gon|wan)na", "ve" from "Weve", etc.  "of" as non-standard for "have" */
			/* "as" is "has" with missing first letter. "to" is rendered "the" once in EWT. */
			// include Charniak tags (AUX, AUXG) so can do BLLIP right
			// These ones are always auxiliaries, even if the word is "too", "my", or whatever else appears in web text.
			// TODO: reverse the polarity of noCopulaHead
			this.makeCopulaHead = !noCopulaHead;
			RuleChanges();
			// make a distinction between auxiliaries and copula verbs to
			// get the NP has semantic head in sentences like "Bill is an honest man".  (Added "sha" for "shan't" May 2009
			verbalAuxiliaries = Generics.NewHashSet(Arrays.AsList(auxiliaries));
			passiveAuxiliaries = Generics.NewHashSet(Arrays.AsList(EnglishPatterns.beGetVerbs));
			//copula verbs having an NP complement
			copulars = Generics.NewHashSet();
			if (noCopulaHead)
			{
				Sharpen.Collections.AddAll(copulars, Arrays.AsList(EnglishPatterns.copularVerbs));
			}
			verbalTags = Generics.NewHashSet(Arrays.AsList(verbTags));
			unambiguousAuxiliaryTags = Generics.NewHashSet(Arrays.AsList(unambiguousAuxTags));
		}

		public override bool MakesCopulaHead()
		{
			return makeCopulaHead;
		}

		//makes modifications of Collins' rules to better fit with semantic notions of heads
		private void RuleChanges()
		{
			//  NP: don't want a POS to be the head
			nonTerminalInfo["NP"] = new string[][] { new string[] { "rightdis", "NN", "NNP", "NNPS", "NNS", "NX", "NML", "JJR", "WP" }, new string[] { "left", "NP", "PRP" }, new string[] { "rightdis", "$", "ADJP", "FW", "CD", "JJ", "QP" }, new string[] 
				{ "rightdis", "JJS", "DT", "WDT", "NML", "PRN", "RB", "RBR", "ADVP" }, new string[] { "left", "POS" } };
			nonTerminalInfo["NX"] = nonTerminalInfo["NP"];
			nonTerminalInfo["NML"] = nonTerminalInfo["NP"];
			// WHNP clauses should have the same sort of head as an NP
			// but it a WHNP has a NP and a WHNP under it, the WHNP should be the head.  E.g.,  (WHNP (WHNP (WP$ whose) (JJ chief) (JJ executive) (NN officer))(, ,) (NP (NNP James) (NNP Gatward))(, ,))
			nonTerminalInfo["WHNP"] = new string[][] { new string[] { "rightdis", "NN", "NNP", "NNPS", "NNS", "NX", "NML", "JJR", "WP" }, new string[] { "left", "WHNP", "NP" }, new string[] { "rightdis", "$", "ADJP", "PRN", "FW" }, new string[] { "right"
				, "CD" }, new string[] { "rightdis", "JJ", "JJS", "RB", "QP" }, new string[] { "left", "WHPP", "WHADJP", "WP$", "WDT" } };
			//WHADJP
			nonTerminalInfo["WHADJP"] = new string[][] { new string[] { "left", "ADJP", "JJ", "JJR", "WP" }, new string[] { "right", "RB" }, new string[] { "right" } };
			//WHADJP
			nonTerminalInfo["WHADVP"] = new string[][] { new string[] { "rightdis", "WRB", "WHADVP", "RB", "JJ" } };
			// if not WRB or WHADVP, probably has flat NP structure, allow JJ for "how long" constructions
			// QP: we don't want the first CD to be the semantic head (e.g., "three billion": head should be "billion"), so we go from right to left
			nonTerminalInfo["QP"] = new string[][] { new string[] { "right", "$", "NNS", "NN", "CD", "JJ", "PDT", "DT", "IN", "RB", "NCD", "QP", "JJR", "JJS" } };
			// S, SBAR and SQ clauses should prefer the main verb as the head
			// S: "He considered him a friend" -> we want a friend to be the head
			nonTerminalInfo["S"] = new string[][] { new string[] { "left", "VP", "S", "FRAG", "SBAR", "ADJP", "UCP", "TO" }, new string[] { "right", "NP" } };
			nonTerminalInfo["SBAR"] = new string[][] { new string[] { "left", "S", "SQ", "SINV", "SBAR", "FRAG", "VP", "WHNP", "WHPP", "WHADVP", "WHADJP", "IN", "DT" } };
			// VP shouldn't be needed in SBAR, but occurs in one buggy tree in PTB3 wsj_1457 and otherwise does no harm
			if (makeCopulaHead)
			{
				nonTerminalInfo["SQ"] = new string[][] { new string[] { "left", "VP", "SQ", "VB", "VBZ", "VBD", "VBP", "MD", "AUX", "AUXG", "ADJP" } };
			}
			else
			{
				nonTerminalInfo["SQ"] = new string[][] { new string[] { "left", "VP", "SQ", "ADJP", "VB", "VBZ", "VBD", "VBP", "MD", "AUX", "AUXG" } };
			}
			// UCP take the first element as head
			nonTerminalInfo["UCP"] = new string[][] { new string[] { "left" } };
			// CONJP: We generally want the rightmost particle or the leftmost conjunction as head
			// JJ is for weird tagging of "not only" in PTB
			nonTerminalInfo["CONJP"] = new string[][] { new string[] { "right", "JJ", "RB" }, new string[] { "left", "CC", "IN" }, new string[] { "right", "VB" } };
			// FRAG: crap rule needs to be change if you want to parse
			// glosses; but it is correct to have ADJP and ADVP before S
			// because of weird parses of reduced sentences.
			nonTerminalInfo["FRAG"] = new string[][] { new string[] { "left", "IN" }, new string[] { "right", "RB" }, new string[] { "left", "NP" }, new string[] { "left", "ADJP", "ADVP", "FRAG", "S", "SBAR", "VP" } };
			// PRN: sentence first
			nonTerminalInfo["PRN"] = new string[][] { new string[] { "left", "VP", "SQ", "S", "SINV", "SBAR", "NP", "ADJP", "PP", "ADVP", "INTJ", "WHNP", "NAC", "VBP", "JJ", "NN", "NNP" } };
			// add the constituent XS (special node to add a layer in a QP tree introduced in our QPTreeTransformer)
			nonTerminalInfo["XS"] = new string[][] { new string[] { "right", "IN" } };
			// add a rule to deal with the CoNLL data
			nonTerminalInfo["EMBED"] = new string[][] { new string[] { "right", "INTJ" } };
			// USD: NP is head of PP
			nonTerminalInfo["PP"] = new string[][] { new string[] { "left", "NP", "S", "SBAR", "SBARQ", "ADVP", "PP", "VP", "ADJP", "FRAG", "UCP", "PRN" }, new string[] { "right" } };
			nonTerminalInfo["WHPP"] = nonTerminalInfo["PP"];
			// Special constituent for multi-word expressions
			nonTerminalInfo["MWE"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["PCONJP"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["ADJP"] = new string[][] { new string[] { "left", "$" }, new string[] { "rightdis", "NNS", "NN", "NNP", "JJ", "QP", "VBN", "VBG" }, new string[] { "left", "ADJP" }, new string[] { "rightdis", "JJP", "JJR", "JJS", "DT", "RB", 
				"RBR", "CD", "IN", "VBD" }, new string[] { "left", "ADVP", "NP" } };
			nonTerminalInfo["INTJ"] = new string[][] { new string[] { "rightdis", "NNS", "NN", "NNP" }, new string[] { "left" } };
			nonTerminalInfo["ADVP"] = new string[][] { new string[] { "rightdis", "RB", "RBR", "RBS", "JJ", "JJR", "JJS" }, new string[] { "rightdis", "RP", "DT", "NN", "CD", "NP", "VBN", "NNP", "CC", "FW", "NNS", "ADJP", "NML" }, new string[] { "left" }
				 };
		}

		private bool ShouldSkip(Tree t, bool origWasInterjection)
		{
			return t.IsPreTerminal() && (tlp.IsPunctuationTag(t.Value()) || !origWasInterjection && "UH".Equals(t.Value())) || "INTJ".Equals(t.Value()) && !origWasInterjection;
		}

		private int FindPreviousHead(int headIdx, Tree[] daughterTrees, bool origWasInterjection)
		{
			bool seenSeparator = false;
			int newHeadIdx = headIdx;
			while (newHeadIdx >= 0)
			{
				newHeadIdx = newHeadIdx - 1;
				if (newHeadIdx < 0)
				{
					return newHeadIdx;
				}
				string label = tlp.BasicCategory(daughterTrees[newHeadIdx].Value());
				if (",".Equals(label) || ":".Equals(label))
				{
					seenSeparator = true;
				}
				else
				{
					if (daughterTrees[newHeadIdx].IsPreTerminal() && (tlp.IsPunctuationTag(label) || !origWasInterjection && "UH".Equals(label)) || "INTJ".Equals(label) && !origWasInterjection)
					{
					}
					else
					{
						// keep looping
						if (!seenSeparator)
						{
							newHeadIdx = -1;
						}
						break;
					}
				}
			}
			return newHeadIdx;
		}

		/// <summary>Overwrite the postOperationFix method.</summary>
		/// <remarks>Overwrite the postOperationFix method.  For "a, b and c" or similar: we want "a" to be the head.</remarks>
		protected internal override int PostOperationFix(int headIdx, Tree[] daughterTrees)
		{
			if (headIdx >= 2)
			{
				string prevLab = tlp.BasicCategory(daughterTrees[headIdx - 1].Value());
				if (prevLab.Equals("CC") || prevLab.Equals("CONJP"))
				{
					bool origWasInterjection = "UH".Equals(tlp.BasicCategory(daughterTrees[headIdx].Value()));
					int newHeadIdx = headIdx - 2;
					// newHeadIdx is now left of conjunction.  Now try going back over commas, etc. for 3+ conjuncts
					// Don't allow INTJ unless conjoined with INTJ - important in informal genres "Oh and don't forget to call!"
					while (newHeadIdx >= 0 && ShouldSkip(daughterTrees[newHeadIdx], origWasInterjection))
					{
						newHeadIdx--;
					}
					// We're now at newHeadIdx < 0 or have found a left head
					// Now consider going back some number of punct that includes a , or : tagged thing and then find non-punct
					while (newHeadIdx >= 2)
					{
						int nextHead = FindPreviousHead(newHeadIdx, daughterTrees, origWasInterjection);
						if (nextHead < 0)
						{
							break;
						}
						newHeadIdx = nextHead;
					}
					if (newHeadIdx >= 0)
					{
						headIdx = newHeadIdx;
					}
				}
			}
			return headIdx;
		}

		internal static readonly TregexPattern[] headOfCopulaTregex = new TregexPattern[] { TregexPattern.Compile("SBARQ < (WHNP $++ (/^VB/ < " + EnglishPatterns.copularWordRegex + " $++ ADJP=head))"), TregexPattern.Compile("SBARQ < (WHNP=head $++ (/^VB/ < "
			 + EnglishPatterns.copularWordRegex + " $+ NP !$++ ADJP))"), TregexPattern.Compile("SINV < (NP=head $++ (NP $++ (VP < (/^(?:VB|AUX)/ < " + EnglishPatterns.copularWordRegex + "))))") };

		internal static readonly TregexPattern[] headOfConjpTregex = new TregexPattern[] { TregexPattern.Compile("CONJP < (CC <: /^(?i:but|and)$/ $+ (RB=head <: /^(?i:not)$/))"), TregexPattern.Compile("CONJP < (CC <: /^(?i:but)$/ [ ($+ (RB=head <: /^(?i:also|rather)$/)) | ($+ (ADVP=head <: (RB <: /^(?i:also|rather)$/))) ])"
			), TregexPattern.Compile("CONJP < (CC <: /^(?i:and)$/ [ ($+ (RB=head <: /^(?i:yet)$/)) | ($+ (ADVP=head <: (RB <: /^(?i:yet)$/))) ])") };

		internal static readonly TregexPattern noVerbOverTempTregex = TregexPattern.Compile("/^VP/ < NP-TMP !< /^V/ !< NNP|NN|NNPS|NNS|NP|JJ|ADJP|S");

		/// <summary>We use this to avoid making a -TMP or -ADV the head of a copular phrase.</summary>
		/// <remarks>
		/// We use this to avoid making a -TMP or -ADV the head of a copular phrase.
		/// For example, in the sentence "It is hands down the best dessert ...",
		/// we want to avoid using "hands down" as the head.
		/// </remarks>
		internal static readonly IPredicate<Tree> RemoveTmpAndAdv = null;

		// Note: The first two SBARQ patterns only work when the SQ
		// structure has already been removed in CoordinationTransformer.
		// Matches phrases such as "what is wrong"
		// matches WHNP $+ VB<copula $+ NP
		// for example, "Who am I to judge?"
		// !$++ ADJP matches against "Why is the dog pink?"
		// Actually somewhat limited in scope, this detects "Tuesday it is",
		// "Such a great idea this was", etc
		/// <summary>
		/// Determine which daughter of the current parse tree is the
		/// head.
		/// </summary>
		/// <remarks>
		/// Determine which daughter of the current parse tree is the
		/// head.  It assumes that the daughters already have had their
		/// heads determined.  Uses special rule for VP heads
		/// </remarks>
		/// <param name="t">
		/// The parse tree to examine the daughters of.
		/// This is assumed to never be a leaf
		/// </param>
		/// <returns>The parse tree that is the head</returns>
		protected internal override Tree DetermineNonTrivialHead(Tree t, Tree parent)
		{
			string motherCat = tlp.BasicCategory(t.Label().Value());
			if (Debug)
			{
				log.Info("At " + motherCat + ", my parent is " + parent);
			}
			// Some conj expressions seem to make more sense with the "not" or
			// other key words as the head.  For example, "and not" means
			// something completely different than "and".  Furthermore,
			// downstream code was written assuming "not" would be the head...
			if (motherCat.Equals("CONJP"))
			{
				foreach (TregexPattern pattern in headOfConjpTregex)
				{
					TregexMatcher matcher = pattern.Matcher(t);
					if (matcher.MatchesAt(t))
					{
						return matcher.GetNode("head");
					}
				}
			}
			// if none of the above patterns match, use the standard method
			if (motherCat.Equals("SBARQ") || motherCat.Equals("SINV"))
			{
				if (!makeCopulaHead)
				{
					foreach (TregexPattern pattern in headOfCopulaTregex)
					{
						TregexMatcher matcher = pattern.Matcher(t);
						if (matcher.MatchesAt(t))
						{
							return matcher.GetNode("head");
						}
					}
				}
			}
			// if none of the above patterns match, use the standard method
			// do VPs with auxiliary as special case
			if ((motherCat.Equals("VP") || motherCat.Equals("SQ") || motherCat.Equals("SINV")))
			{
				Tree[] kids = t.Children();
				// try to find if there is an auxiliary verb
				if (Debug)
				{
					log.Info("Semantic head finder: at VP");
					log.Info("Class is " + t.GetType().FullName);
					t.PennPrint(System.Console.Error);
				}
				//log.info("hasVerbalAuxiliary = " + hasVerbalAuxiliary(kids, verbalAuxiliaries));
				// looks for auxiliaries
				Tree[] tmpFilteredChildren = null;
				if (HasVerbalAuxiliary(kids, verbalAuxiliaries, true) || HasPassiveProgressiveAuxiliary(kids))
				{
					// String[] how = new String[] {"left", "VP", "ADJP", "NP"};
					// Including NP etc seems okay for copular sentences but is
					// problematic for other auxiliaries, like 'he has an answer'
					string[] how;
					if (HasVerbalAuxiliary(kids, copulars, true))
					{
						// Only allow ADJP in copular constructions
						// In constructions like "It gets cold", "get" should be the head
						how = new string[] { "left", "VP", "ADJP" };
					}
					else
					{
						how = new string[] { "left", "VP" };
					}
					if (tmpFilteredChildren == null)
					{
						tmpFilteredChildren = ArrayUtils.Filter(kids, RemoveTmpAndAdv);
					}
					Tree pti = TraverseLocate(tmpFilteredChildren, how, false);
					if (Debug)
					{
						log.Info("Determined head (case 1) for " + t.Value() + " is: " + pti);
					}
					if (pti != null)
					{
						return pti;
					}
				}
				// } else {
				// log.info("------");
				// log.info("SemanticHeadFinder failed to reassign head for");
				// t.pennPrint(System.err);
				// log.info("------");
				// looks for copular verbs
				if (HasVerbalAuxiliary(kids, copulars, false) && !IsExistential(t, parent) && !IsWHQ(t, parent))
				{
					string[][] how;
					//TODO: also allow ADVP to be heads
					if (motherCat.Equals("SQ"))
					{
						how = new string[][] { new string[] { "right", "VP", "ADJP", "NP", "UCP", "PP", "WHADJP", "WHNP" } };
					}
					else
					{
						how = new string[][] { new string[] { "left", "VP", "ADJP", "NP", "UCP", "PP", "WHADJP", "WHNP" } };
					}
					// Avoid undesirable heads by filtering them from the list of potential children
					if (tmpFilteredChildren == null)
					{
						tmpFilteredChildren = ArrayUtils.Filter(kids, RemoveTmpAndAdv);
					}
					Tree pti = null;
					for (int i = 0; i < how.Length && pti == null; i++)
					{
						pti = TraverseLocate(tmpFilteredChildren, how[i], false);
					}
					// In SQ, only allow an NP to become head if there is another one to the left (then it's probably predicative)
					if (motherCat.Equals("SQ") && pti != null && pti.Label() != null && pti.Label().Value().StartsWith("NP"))
					{
						bool foundAnotherNp = false;
						foreach (Tree kid in kids)
						{
							if (kid == pti)
							{
								break;
							}
							else
							{
								if (kid.Label() != null && kid.Label().Value().StartsWith("NP"))
								{
									foundAnotherNp = true;
									break;
								}
							}
						}
						if (!foundAnotherNp)
						{
							pti = null;
						}
					}
					if (Debug)
					{
						log.Info("Determined head (case 2) for " + t.Value() + " is: " + pti);
					}
					if (pti != null)
					{
						return pti;
					}
					else
					{
						if (Debug)
						{
							log.Info("------");
							log.Info("SemanticHeadFinder failed to reassign head for");
							t.PennPrint(System.Console.Error);
							log.Info("------");
						}
					}
				}
			}
			Tree hd = base.DetermineNonTrivialHead(t, parent);
			/* ----
			// This should now be handled at the AbstractCollinsHeadFinder level, so see if we can comment this out
			// Heuristically repair punctuation heads
			Tree[] hdChildren = hd.children();
			if (hdChildren != null && hdChildren.length > 0 &&
			hdChildren[0].isLeaf()) {
			if (tlp.isPunctuationWord(hdChildren[0].label().value())) {
			Tree[] tChildren = t.children();
			if (DEBUG) {
			System.err.printf("head is punct: %s\n", hdChildren[0].label());
			}
			for (int i = tChildren.length - 1; i >= 0; i--) {
			if (!tlp.isPunctuationWord(tChildren[i].children()[0].label().value())) {
			hd = tChildren[i];
			if (DEBUG) {
			System.err.printf("New head of %s is %s%n", hd.label(), hd.children()[0].label());
			}
			break;
			}
			}
			}
			}
			*/
			if (Debug)
			{
				log.Info("Determined head (case 3) for " + t.Value() + " is: " + hd);
			}
			return hd;
		}

		/* Checks whether the tree t is an existential constituent
		* There are two cases:
		* -- affirmative sentences in which "there" is a left sister of the VP
		* -- questions in which "there" is a daughter of the SQ.
		*
		*/
		private bool IsExistential(Tree t, Tree parent)
		{
			if (Debug)
			{
				log.Info("isExistential: " + t + ' ' + parent);
			}
			bool toReturn = false;
			string motherCat = tlp.BasicCategory(t.Label().Value());
			// affirmative case
			if (motherCat.Equals("VP") && parent != null)
			{
				//take t and the sisters
				Tree[] kids = parent.Children();
				// iterate over the sisters before t and checks if existential
				foreach (Tree kid in kids)
				{
					if (!kid.Value().Equals("VP"))
					{
						IList<ILabel> tags = kid.PreTerminalYield();
						foreach (ILabel tag in tags)
						{
							if (tag.Value().Equals("EX"))
							{
								toReturn = true;
							}
						}
					}
					else
					{
						break;
					}
				}
			}
			else
			{
				// question case
				if (motherCat.StartsWith("SQ") && parent != null)
				{
					//take the daughters
					Tree[] kids = parent.Children();
					// iterate over the daughters and checks if existential
					foreach (Tree kid in kids)
					{
						if (!kid.Value().StartsWith("VB"))
						{
							//not necessary to look into the verb
							IList<ILabel> tags = kid.PreTerminalYield();
							foreach (ILabel tag in tags)
							{
								if (tag.Value().Equals("EX"))
								{
									toReturn = true;
								}
							}
						}
					}
				}
			}
			if (Debug)
			{
				log.Info("decision " + toReturn);
			}
			return toReturn;
		}

		/* Is the tree t a WH-question?
		*  At present this is only true if the tree t is a SQ having a WH.* sister
		*  and headed by a SBARQ.
		* (It was changed to looser definition in Feb 2006.)
		*
		*/
		private static bool IsWHQ(Tree t, Tree parent)
		{
			if (t == null)
			{
				return false;
			}
			bool toReturn = false;
			if (t.Value().StartsWith("SQ"))
			{
				if (parent != null && parent.Value().Equals("SBARQ"))
				{
					Tree[] kids = parent.Children();
					foreach (Tree kid in kids)
					{
						// looks for a WH.*
						if (kid.Value().StartsWith("WH"))
						{
							toReturn = true;
						}
					}
				}
			}
			if (Debug)
			{
				log.Info("in isWH, decision: " + toReturn + " for node " + t);
			}
			return toReturn;
		}

		private bool IsVerbalAuxiliary(Tree preterminal, ICollection<string> verbalSet, bool allowJustTagMatch)
		{
			if (preterminal.IsPreTerminal())
			{
				ILabel kidLabel = preterminal.Label();
				string tag = null;
				if (kidLabel is IHasTag)
				{
					tag = ((IHasTag)kidLabel).Tag();
				}
				if (tag == null)
				{
					tag = preterminal.Value();
				}
				ILabel wordLabel = preterminal.FirstChild().Label();
				string word = null;
				if (wordLabel is IHasWord)
				{
					word = ((IHasWord)wordLabel).Word();
				}
				if (word == null)
				{
					word = wordLabel.Value();
				}
				if (Debug)
				{
					log.Info("Checking " + preterminal.Value() + " head is " + word + '/' + tag);
				}
				string lcWord = word.ToLower();
				if (allowJustTagMatch && unambiguousAuxiliaryTags.Contains(tag) || verbalTags.Contains(tag) && verbalSet.Contains(lcWord))
				{
					if (Debug)
					{
						log.Info("isAuxiliary found desired type of aux");
					}
					return true;
				}
			}
			return false;
		}

		/// <summary>Returns true if this tree is a preterminal that is a verbal auxiliary.</summary>
		/// <param name="t">A tree to examine for being an auxiliary.</param>
		/// <returns>Whether it is a verbal auxiliary (be, do, have, get)</returns>
		public virtual bool IsVerbalAuxiliary(Tree t)
		{
			return IsVerbalAuxiliary(t, verbalAuxiliaries, true);
		}

		// now overly complex so it deals with coordinations.  Maybe change this class to use tregrex?
		private bool HasPassiveProgressiveAuxiliary(Tree[] kids)
		{
			if (Debug)
			{
				log.Info("Checking for passive/progressive auxiliary");
			}
			bool foundPassiveVP = false;
			bool foundPassiveAux = false;
			foreach (Tree kid in kids)
			{
				if (Debug)
				{
					log.Info("  checking in " + kid);
				}
				if (IsVerbalAuxiliary(kid, passiveAuxiliaries, false))
				{
					foundPassiveAux = true;
				}
				else
				{
					if (kid.IsPhrasal())
					{
						ILabel kidLabel = kid.Label();
						string cat = null;
						if (kidLabel is IHasCategory)
						{
							cat = ((IHasCategory)kidLabel).Category();
						}
						if (cat == null)
						{
							cat = kid.Value();
						}
						if (!cat.StartsWith("VP"))
						{
							continue;
						}
						if (Debug)
						{
							log.Info("hasPassiveProgressiveAuxiliary found VP");
						}
						Tree[] kidkids = kid.Children();
						bool foundParticipleInVp = false;
						foreach (Tree kidkid in kidkids)
						{
							if (Debug)
							{
								log.Info("  hasPassiveProgressiveAuxiliary examining " + kidkid);
							}
							if (kidkid.IsPreTerminal())
							{
								ILabel kidkidLabel = kidkid.Label();
								string tag = null;
								if (kidkidLabel is IHasTag)
								{
									tag = ((IHasTag)kidkidLabel).Tag();
								}
								if (tag == null)
								{
									tag = kidkid.Value();
								}
								// we allow in VBD because of frequent tagging mistakes
								if ("VBN".Equals(tag) || "VBG".Equals(tag) || "VBD".Equals(tag))
								{
									foundPassiveVP = true;
									if (Debug)
									{
										log.Info("hasPassiveAuxiliary found VBN/VBG/VBD VP");
									}
									break;
								}
								else
								{
									if ("CC".Equals(tag) && foundParticipleInVp)
									{
										foundPassiveVP = true;
										if (Debug)
										{
											log.Info("hasPassiveAuxiliary [coordination] found (VP (VP[VBN/VBG/VBD] CC");
										}
										break;
									}
								}
							}
							else
							{
								if (kidkid.IsPhrasal())
								{
									string catcat = null;
									if (kidLabel is IHasCategory)
									{
										catcat = ((IHasCategory)kidLabel).Category();
									}
									if (catcat == null)
									{
										catcat = kid.Value();
									}
									if ("VP".Equals(catcat))
									{
										if (Debug)
										{
											log.Info("hasPassiveAuxiliary found (VP (VP)), recursing");
										}
										foundParticipleInVp = VpContainsParticiple(kidkid);
									}
									else
									{
										if (("CONJP".Equals(catcat) || "PRN".Equals(catcat)) && foundParticipleInVp)
										{
											// occasionally get PRN in CONJ-like structures
											foundPassiveVP = true;
											if (Debug)
											{
												log.Info("hasPassiveAuxiliary [coordination] found (VP (VP[VBN/VBG/VBD] CONJP");
											}
											break;
										}
									}
								}
							}
						}
					}
				}
				if (foundPassiveAux && foundPassiveVP)
				{
					break;
				}
			}
			// end for (Tree kid : kids)
			if (Debug)
			{
				log.Info("hasPassiveProgressiveAuxiliary returns " + (foundPassiveAux && foundPassiveVP));
			}
			return foundPassiveAux && foundPassiveVP;
		}

		private static bool VpContainsParticiple(Tree t)
		{
			foreach (Tree kid in t.Children())
			{
				if (Debug)
				{
					log.Info("vpContainsParticiple examining " + kid);
				}
				if (kid.IsPreTerminal())
				{
					ILabel kidLabel = kid.Label();
					string tag = null;
					if (kidLabel is IHasTag)
					{
						tag = ((IHasTag)kidLabel).Tag();
					}
					if (tag == null)
					{
						tag = kid.Value();
					}
					if ("VBN".Equals(tag) || "VBG".Equals(tag) || "VBD".Equals(tag))
					{
						if (Debug)
						{
							log.Info("vpContainsParticiple found VBN/VBG/VBD VP");
						}
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// This looks to see whether any of the children is a preterminal headed by a word
		/// which is within the set verbalSet (which in practice is either
		/// auxiliary or copula verbs).
		/// </summary>
		/// <remarks>
		/// This looks to see whether any of the children is a preterminal headed by a word
		/// which is within the set verbalSet (which in practice is either
		/// auxiliary or copula verbs).  It only returns true if it's a preterminal head, since
		/// you don't want to pick things up in phrasal daughters.  That is an error.
		/// </remarks>
		/// <param name="kids">The child trees</param>
		/// <param name="verbalSet">The set of words</param>
		/// <param name="allowTagOnlyMatch">
		/// If true, it's sufficient to match on an unambiguous auxiliary tag.
		/// Make true iff verbalSet is "all auxiliaries"
		/// </param>
		/// <returns>
		/// Returns true if one of the child trees is a preterminal verb headed
		/// by a word in verbalSet
		/// </returns>
		private bool HasVerbalAuxiliary(Tree[] kids, ICollection<string> verbalSet, bool allowTagOnlyMatch)
		{
			if (Debug)
			{
				log.Info("Checking for verbal auxiliary");
			}
			foreach (Tree kid in kids)
			{
				if (Debug)
				{
					log.Info("  checking in " + kid);
				}
				if (IsVerbalAuxiliary(kid, verbalSet, allowTagOnlyMatch))
				{
					return true;
				}
			}
			if (Debug)
			{
				log.Info("hasVerbalAuxiliary returns false");
			}
			return false;
		}

		private const long serialVersionUID = 5721799188009249808L;
	}
}
