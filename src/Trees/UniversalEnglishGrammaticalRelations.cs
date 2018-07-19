// Universal Stanford Dependencies - Code for producing and using Universal Stanford dependencies.
// Copyright © 2005-2014 The Board of Trustees of
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
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    parser-support@lists.stanford.edu
//    http://nlp.stanford.edu/software/stanford-dependencies.shtml
using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Concurrent.Locks;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// <c>UniversalEnglishGrammaticalRelations</c>
	/// is a
	/// set of
	/// <see cref="GrammaticalRelation"/>
	/// objects according to the Universal
	/// Dependencies standard.
	/// <p/>
	/// Grammatical relations can either be shown in their basic form, where each
	/// input token receives a relation, or "collapsed" which does certain normalizations
	/// which group words or turns them into relations. See
	/// <see cref="UniversalEnglishGrammaticalStructure"/>
	/// .  What is presented here mainly
	/// shows the basic form, though there is some mixture. The "collapsed" grammatical
	/// relations primarily differ as follows:
	/// <ul>
	/// <li>Some multiword conjunctions and prepositions are treated as single
	/// words, and then processed as below.</li>
	/// <li>Prepositions are appended to nmod/acl/advcl
	/// grammatical relations..</li>
	/// <li>Coordination markers are appended to "conj"
	/// grammatical relations.</li>
	/// <li>Agents of passive sentences are recognized and marked as nmod:agent and not as nmod:by.</li>
	/// </ul>
	/// <p/>
	/// This set of English grammatical relations is not intended to be
	/// exhaustive or immutable.  It's just where we're at now.
	/// <p/>
	/// <p/>
	/// See
	/// <see cref="GrammaticalRelation"/>
	/// for details of fields and matching.
	/// <p/>
	/// <p/>
	/// If using LexicalizedParser, it should be run with the
	/// <c>-retainTmpSubcategories</c>
	/// option and one of the
	/// <c>-splitTMP</c>
	/// options (e.g.,
	/// <c>-splitTMP 1</c>
	/// ) in order to
	/// get the temporal NP dependencies maximally right!
	/// <p/>
	/// <i>Implementation notes: </i> Don't change the set of GRs without discussing it
	/// with people first.  If a change is needed, to add a new grammatical relation:
	/// <ul>
	/// <li> Governor nodes of the grammatical relations should be the lowest ones.</li>
	/// <li> Check the semantic head rules in UniversalSemanticHeadFinder and
	/// ModCollinsHeadFinder, both in the trees package. That's what will be used to
	/// match here.</li>
	/// <li> Create and define the GrammaticalRelation similarly to the others.</li>
	/// <li> Add it to the
	/// <c>values</c>
	/// array at the end of the file.</li>
	/// </ul>
	/// The patterns in this code assume that an NP may be followed by either a
	/// -ADV or -TMP functional tag but there are no other functional tags represented.
	/// This corresponds to what we currently get from NPTmpRetainingTreeNormalizer or
	/// DependencyTreeTransformer.
	/// </summary>
	/// <author>Bill MacCartney</author>
	/// <author>Marie-Catherine de Marneffe</author>
	/// <author>Christopher Manning</author>
	/// <author>Galen Andrew (refactoring English-specific stuff)</author>
	/// <author>Sebastian Schuster</author>
	/// <seealso cref="GrammaticalStructure"/>
	/// <seealso cref="GrammaticalRelation"/>
	/// <seealso cref="EnglishGrammaticalStructure"/>
	/// <seealso><a href="http://universaldependencies.github.io/docs/en/dep/">English grammatical relations documentation</a></seealso>
	public class UniversalEnglishGrammaticalRelations
	{
		/// <summary>
		/// This class is just a holder for static classes
		/// that act a bit like an enum.
		/// </summary>
		private UniversalEnglishGrammaticalRelations()
		{
		}

		private static readonly TregexPatternCompiler tregexCompiler = new TregexPatternCompiler((IHeadFinder)null);

		/// <summary>The "predicate" grammatical relation.</summary>
		/// <remarks>
		/// The "predicate" grammatical relation.  The predicate of a
		/// clause is the main VP of that clause; the predicate of a
		/// subject is the predicate of the clause to which the subject
		/// belongs.
		/// Example: <br/>
		/// "Reagan died" &rarr;
		/// <c>pred</c>
		/// (Reagan, died)
		/// </remarks>
		public static readonly GrammaticalRelation Predicate = new GrammaticalRelation(Language.UniversalEnglish, "pred", "predicate", Dependent, "S|SINV", tregexCompiler, "S|SINV <# VP=target");

		/// <summary>
		/// An auxiliary of a clause is a non-main verb of the clause,
		/// e.g., a modal auxiliary, or a form of be, do or have in a
		/// periphrastic tense.
		/// </summary>
		/// <remarks>
		/// An auxiliary of a clause is a non-main verb of the clause,
		/// e.g., a modal auxiliary, or a form of be, do or have in a
		/// periphrastic tense.
		/// Contrary to the older SD and arguments of Pullum (1982) and
		/// following, infinitive to is not analyzed as an auxiliary.
		/// Instead, it is analyzed as a mark.
		/// Example: <br/>
		/// "Reagan has died" &rarr;
		/// <c>aux</c>
		/// (died, has)
		/// </remarks>
		public static readonly GrammaticalRelation AuxModifier = new GrammaticalRelation(Language.UniversalEnglish, "aux", "auxiliary", Dependent, "VP|SQ|SINV|CONJP", tregexCompiler, "VP < VP < (/^(?:MD|VB.*|AUXG?|POS)$/=target)", "SQ|SINV < (/^(?:VB|MD|AUX)/=target $++ /^(?:VP|ADJP)/)"
			, "SINV < (VP=target < (/^(?:VB|AUX|POS)/ < " + beAuxiliaryRegex + ") $-- (VP < VBG))");

		/// <summary>The "passive auxiliary" grammatical relation.</summary>
		/// <remarks>
		/// The "passive auxiliary" grammatical relation. A passive auxiliary of a
		/// clause is a
		/// non-main verb of the clause which contains the passive information.
		/// Example: <br/>
		/// "Kennedy has been killed" &rarr;
		/// <c>auxpass</c>
		/// (killed, been)
		/// </remarks>
		public static readonly GrammaticalRelation AuxPassiveModifier = new GrammaticalRelation(Language.UniversalEnglish, "auxpass", "passive auxiliary", AuxModifier, "VP|SQ|SINV", tregexCompiler, "VP < (/^(?:VB|AUX|POS)/=target < " + passiveAuxWordRegex
			 + " ) < (VP|ADJP [ < VBN|VBD | < (VP|ADJP < VBN|VBD) < CC ] )", "SQ|SINV < (/^(?:VB|AUX|POS)/=target < " + beAuxiliaryRegex + " $++ (VP < VBD|VBN))", "SINV < (VP=target < (/^(?:VB|AUX|POS)/ < " + beAuxiliaryRegex + ") $-- (VP < VBD|VBN))", 
			"SINV < (VP=target < (VP < (/^(?:VB|AUX|POS)/ < " + beAuxiliaryRegex + ")) $-- (VP < VBD|VBN))");

		/// <summary>The "copula" grammatical relation.</summary>
		/// <remarks>
		/// The "copula" grammatical relation.  A copula is the relation between
		/// the complement of a copular verb and the copular verb.<p>
		/// <p/>
		/// Examples: <br/>
		/// "Bill is big" &rarr;
		/// <c>cop</c>
		/// (big, is) <br/>
		/// "Bill is an honest man" &rarr;
		/// <c>cop</c>
		/// (man, is)
		/// </remarks>
		public static readonly GrammaticalRelation Copula = new GrammaticalRelation(Language.UniversalEnglish, "cop", "copula", AuxModifier, "VP|SQ|SINV|SBARQ", tregexCompiler, "VP < (/^(?:VB|AUX)/=target < " + copularWordRegex + " [ $++ (/^(?:ADJP|NP$|WHNP$|PP|UCP)/ !< (VBN|VBD !$++ /^N/)) | $++ (S <: (ADJP < JJ)) ] )"
			, "SQ|SINV < (/^(?:VB|AUX)/=target < " + copularWordRegex + " [ $++ (ADJP !< VBN|VBD) | $++ (NP $++ NP) | $++ (S <: (ADJP < JJ)) ] )", "SBARQ < (/^(?:VB|AUX)/=target < " + copularWordRegex + ") < (WHNP < WP)", "SINV <# (NP $++ (NP $++ (VP=target < (/^(?:VB|AUX)/ < "
			 + copularWordRegex + "))))");

		private const string EtcPat = "(FW < /^(?i:(etc|ect))$/)";

		private const string ETC_PAT_target = "(FW=target < /^(?i:(etc|ect))$/)";

		private const string FwEtcPat = "(ADVP|NP <1 (FW < /^(?i:(etc|ect))$/))";

		private const string FW_ETC_PAT_target = "(ADVP|NP=target <1 (FW < /^(?i:(etc|ect))$/))";

		private const string NotPat = "/^(?i:n[o']?t|never)$/";

		private const string WesternSmiley = "/^(?:[<>]?[:;=8][\\-o\\*']?(?:-RRB-|-LRB-|[DPdpO\\/\\\\\\:}{@\\|\\[\\]])|(?:-RRB-|-LRB-|[DPdpO\\/\\\\\\:}{@\\|\\[\\]])[\\-o\\*']?[:;=8][<>]?)$/";

		private const string AsianSmiley = "/(?!^--$)^(?:-LRB-)?[\\-\\^x=~<>'][_.]?[\\-\\^x=~<>'](?:-RRB-)?$/";

		/// <summary>The "conjunct" grammatical relation.</summary>
		/// <remarks>
		/// The "conjunct" grammatical relation.  A conjunct is the relation between
		/// two elements connected by a conjunction word.  We treat conjunctions
		/// asymmetrically: The head of the relation is the first conjunct and other
		/// conjunctions depend on it via the <i>conj</i> relation.<p>
		/// <p/>
		/// Example: <br/>
		/// "Bill is big and honest" &rarr;
		/// <c>conj</c>
		/// (big, honest)
		/// <p/>
		/// <i>Note:</i>Modified in 2010 to exclude the case of a CC/CONJP first in its phrase: it has to conjoin things.
		/// </remarks>
		public static readonly GrammaticalRelation Conjunct = new GrammaticalRelation(Language.UniversalEnglish, "conj", "conjunct", Dependent, "VP|(?:WH)?NP(?:-TMP|-ADV)?|ADJP|PP|QP|ADVP|UCP(?:-TMP|-ADV)?|S|NX|SBAR|SBARQ|SINV|SQ|JJP|NML|RRC|PCONJP"
			, tregexCompiler, "VP|S|SBAR|SBARQ|SINV|SQ|RRC < (CC|CONJP $-- !/^(?:``|-LRB-|PRN|PP|ADVP|RB|MWE)/ $+ !/^(?:SBAR|PRN|``|''|-[LR]RB-|,|:|\\.)$/=target)", "SBAR < (CC|CONJP $-- @SBAR $+ @SBAR=target)", "VP|S|SBAR|SBARQ|SINV|SQ|RRC < (CC|CONJP $-- !/^(?:``|-LRB-|PRN|PP|ADVP|RB)/ $+ (ADVP $+ !/^(?:PRN|``|''|-[LR]RB-|,|:|\\.)$/=target))"
			, "VP|S|SBAR|SBARQ|SINV|SQ=root < (CC|CONJP $-- !/^(?:``|-LRB-|PRN|PP|ADVP|RB)/) < (/^(?:PRN|``|''|-[LR]RB-|,|:|\\.)$/ $+ (/^S|SINV$|^(?:A|N|V|PP|PRP|J|W|R)/=target [$-- (CC|CONJP $-- (__ ># =root) !$++ (/^:|,$/ $++ =target)) | $-- (/^:|,$/ $-- (__ ># =root) [!$-- /^CC|CONJP$/ | $++ (=target < (/^,$/ $++ (__ ># =target)))])] ) )"
			, "/^(?:ADJP|JJP|PP|QP|(?:WH)?NP(?:-TMP|-ADV)?|ADVP|UCP(?:-TMP|-ADV)?|NX|NML)$/ [ < (CC|CONJP $-- !/^(?:``|-LRB-|PRN)$/ $+ !/^(?:PRN|``|''|-[LR]RB-|,|:|\\.)$/=target) | < " + ETC_PAT_target + " | < " + FW_ETC_PAT_target + "]", "/^(?:ADJP|PP|(?:WH)?NP(?:-TMP|-ADV)?|ADVP|UCP(?:-TMP|-ADV)?|NX|NML)$/ < (CC|CONJP $-- !/^(?:``|-LRB-|PRN)$/ $+ (ADVP $+ !/^(?:PRN|``|''|-[LR]RB-|,|:|\\.)$/=target))"
			, "/^(?:ADJP|PP|(?:WH)?NP(?:-TMP|-ADV)?|ADVP|UCP(?:-TMP|-ADV)?|NX|NML)$/ [ < (CC|CONJP $-- !/^(?:``|-LRB-|PRN)$/) | < " + EtcPat + " | < " + FwEtcPat + "] < (/^(?:PRN|``|''|-[LR]RB-|,|:|\\.)$/ [ $+ /^S|SINV$|^(?:A|N|V|PP|PRP|J|W|R)/=target | $+ "
			 + ETC_PAT_target + " ] )", "NX|NML [ < (CC|CONJP $- __) | < " + EtcPat + "] < (/^,$/ $- /^(?:A|N|V|PP|PRP|J|W|R|S)/=target)", "/^(?:VP|S|SBAR|SBARQ|SINV|ADJP|PP|QP|(?:WH)?NP(?:-TMP|-ADV)?|ADVP|UCP(?:-TMP|-ADV)?|NX|NML)$/ [ < (CC $++ (CC|CONJP $+ !/^(?:PRN|``|''|-[LR]RB-|,|:|\\.)$/=target)) | <- "
			 + ETC_PAT_target + " | <- " + FW_ETC_PAT_target + " ]", "PCONJP < (CC $+ IN|TO=target)", "/.*/ < (/^(.*)$/#1%x $+ (/,/ < /\\// $+ /^(.*)$/#1%x=target))");

		/// <summary>The "coordination" grammatical relation.</summary>
		/// <remarks>
		/// The "coordination" grammatical relation.  A coordination is the relation
		/// between an element and a conjunction.
		/// <p/>
		/// Example: <br/>
		/// "Bill is big and honest." &rarr;
		/// <c>cc</c>
		/// (big, and)
		/// </remarks>
		public static readonly GrammaticalRelation Coordination = new GrammaticalRelation(Language.UniversalEnglish, "cc", "coordination", Dependent, ".*", tregexCompiler, "__ ([ < (CC=target !< /^(?i:either|neither|both)$/ ) | < (CONJP=target !< (RB < /^(?i:not)$/ $+ (RB|JJ < /^(?i:only|just|merely)$/))) ] [!> /PP/ | !>2 NP])"
			);

		/// <summary>The "punctuation" grammatical relation.</summary>
		/// <remarks>
		/// The "punctuation" grammatical relation.  This is used for any piece of
		/// punctuation in a clause, if punctuation is being retained in the
		/// typed dependencies.
		/// <p/>
		/// Example: <br/>
		/// "Go home!" &rarr;
		/// <c>punct</c>
		/// (Go, !)
		/// <p/>
		/// The condition for NFP to appear hear is that it does not match the emoticon patterns under discourse.
		/// </remarks>
		public static readonly GrammaticalRelation Punctuation = new GrammaticalRelation(Language.UniversalEnglish, "punct", "punctuation", Dependent, ".*", tregexCompiler, "__ < /^(?:\\.|:|,|''|``|\\*|-LRB-|-RRB-|HYPH)$/=target", "__ < (NFP=target !< "
			 + WesternSmiley + " !< " + AsianSmiley + ")");

		/// <summary>The "argument" grammatical relation.</summary>
		/// <remarks>
		/// The "argument" grammatical relation.  An argument of a VP is a
		/// subject or complement of that VP; an argument of a clause is
		/// an argument of the VP which is the predicate of that
		/// clause.<p>
		/// <p/>
		/// Example: <br/>
		/// "Clinton defeated Dole" &rarr; <code>arg</code>(defeated, Clinton),
		/// <c>arg</c>
		/// (defeated, Dole)
		/// </remarks>
		public static readonly GrammaticalRelation Argument = new GrammaticalRelation(Language.UniversalEnglish, "arg", "argument", Dependent);

		/// <summary>The "subject" grammatical relation.</summary>
		/// <remarks>
		/// The "subject" grammatical relation.  The subject of a VP is
		/// the noun or clause that performs or experiences the VP; the
		/// subject of a clause is the subject of the VP which is the
		/// predicate of that clause.<p>
		/// <p/>
		/// Examples: <br/>
		/// "Clinton defeated Dole" &rarr;
		/// <c>subj</c>
		/// (defeated, Clinton) <br/>
		/// "What she said is untrue" &rarr;
		/// <c>subj</c>
		/// (is, What she said)
		/// </remarks>
		public static readonly GrammaticalRelation Subject = new GrammaticalRelation(Language.UniversalEnglish, "subj", "subject", Argument);

		/// <summary>The "nominal subject" grammatical relation.</summary>
		/// <remarks>
		/// The "nominal subject" grammatical relation.  A nominal subject is
		/// a subject which is an noun phrase.<p>
		/// <p/>
		/// Example: <br/>
		/// "Clinton defeated Dole" &rarr;
		/// <c>nsubj</c>
		/// (defeated, Clinton)
		/// </remarks>
		public static readonly GrammaticalRelation NominalSubject = new GrammaticalRelation(Language.UniversalEnglish, "nsubj", "nominal subject", Subject, "S|SQ|SBARQ|SINV|SBAR|PRN", tregexCompiler, "S=subj < ((NP|WHNP=target !< EX !<# (/^NN/ < (" 
			+ timeWordRegex + "))) $++ VP=verb) : (=subj !> VP | !<< (=verb < TO))", "S < ( NP=target <# (/^NN/ < " + timeWordRegex + ") !$++ NP $++VP)", "SQ|PRN < (NP=target !< EX $++ VP)", "SQ < (NP=target !< EX $- (/^(?:VB|AUX)/ < " + copularWordRegex
			 + ") !$++ VP)", "SQ < (NP=target !< EX $- /^(?:VB|AUX)/ !$++ VP) !$-- NP|WHNP", "SQ < ((NP=target !< EX) $- (RB $- /^(?:VB|AUX)/) ![$++ VP])", "SBARQ < WHNP=target < (SQ < (VP !$-- NP))", "SBARQ < WHNP=target < (SQ < ((/^(?:VB)/ !< " + copularWordRegex
			 + ") !$-- NP !$++ VP))", "SBARQ < (SQ=target < (/^(?:VB|AUX)/ < " + copularWordRegex + ") !< VP)", "SINV < (NP|WHNP=target [ $- VP|VBZ|VBD|VBP|VB|MD|AUX | $- (@RB|ADVP $- VP|VBZ|VBD|VBP|VB|MD|AUX) | !$- __ !$ @NP] )", "SINV < (NP $++ (NP=target $++ (VP < (/^(?:VB|AUX)/ < "
			 + copularWordRegex + "))))", "S < (NP=target $+ NP|ADJP) > VP", "SBAR < WHNP=target [ < (S < (VP !$-- NP) !< SBAR) | < (VP !$-- NP) !< S ]", "SBAR !< WHNP < (S !< (NP $++ VP)) > (VP > (S $- WHNP=target))", "SQ < ((NP < EX) $++ NP=target)", 
			"S < (NP < EX) <+(VP) (VP < NP=target)", "SBARQ < (/^(?:VB|AUX)/ < " + copularWordRegex + ") < (WHNP < WP) < NP=target", "SBARQ < (WHNP=target $++ ((/^(?:VB|AUX)/ < " + copularWordRegex + ") $++ ADJP=adj !$++ (NP $++ =adj)))", "SBARQ <1 WHNP=target < (SQ < (/^(?:VB|AUX)/ < "
			 + copularWordRegex + ") [< (NP < EX) | < PP])");

		/// <summary>The "nominal passive subject" grammatical relation.</summary>
		/// <remarks>
		/// The "nominal passive subject" grammatical relation.  A nominal passive
		/// subject is a subject of a passive which is an noun phrase.<p>
		/// <p/>
		/// Example: <br/>
		/// "Dole was defeated by Clinton" &rarr;
		/// <c>nsubjpass</c>
		/// (defeated, Dole)
		/// <p>
		/// This pattern recognizes basic (non-coordinated) examples.  The coordinated
		/// examples are currently handled by correctDependencies() in
		/// EnglishGrammaticalStructure.  This seemed more accurate than any tregex
		/// expression we could come up with.
		/// </remarks>
		public static readonly GrammaticalRelation NominalPassiveSubject = new GrammaticalRelation(Language.UniversalEnglish, "nsubjpass", "nominal passive subject", NominalSubject, "S|SQ", tregexCompiler, "S|SQ < (WHNP|NP=target !< EX) < (VP < (/^(?:VB|AUX)/ < "
			 + passiveAuxWordRegex + ")  < (VP < VBN|VBD))");

		/// <summary>The "clausal subject" grammatical relation.</summary>
		/// <remarks>
		/// The "clausal subject" grammatical relation.  A clausal subject is
		/// a subject which is a clause.<p>
		/// <p/>
		/// Examples: (subject is "what she said" in both examples) <br/>
		/// "What she said makes sense" &rarr;
		/// <c>csubj</c>
		/// (makes, said) <br/>
		/// "What she said is untrue" &rarr;
		/// <c>csubj</c>
		/// (untrue, said)
		/// </remarks>
		public static readonly GrammaticalRelation ClausalSubject = new GrammaticalRelation(Language.UniversalEnglish, "csubj", "clausal subject", Subject, "S", tregexCompiler, "S < (SBAR|S=target !$+ /^,$/ $++ (VP !$-- NP))");

		/// <summary>The "clausal passive subject" grammatical relation.</summary>
		/// <remarks>
		/// The "clausal passive subject" grammatical relation.  A clausal passive subject is
		/// a subject of a passive verb which is a clause.<p>
		/// <p/>
		/// Example: (subject is "that she lied") <br/>
		/// "That she lied was suspected by everyone" &rarr;
		/// <c>csubjpass</c>
		/// (suspected, lied)
		/// </remarks>
		public static readonly GrammaticalRelation ClausalPassiveSubject = new GrammaticalRelation(Language.UniversalEnglish, "csubjpass", "clausal passive subject", ClausalSubject, "S", tregexCompiler, "S < (SBAR|S=target !$+ /^,$/ $++ (VP < (VP < VBN|VBD) < (/^(?:VB|AUXG?)/ < "
			 + passiveAuxWordRegex + ") !$-- NP))", "S < (SBAR|S=target !$+ /^,$/ $++ (VP <+(VP) (VP < VBN|VBD > (VP < (/^(?:VB|AUX)/ < " + passiveAuxWordRegex + "))) !$-- NP))");

		/// <summary>The "complement" grammatical relation.</summary>
		/// <remarks>
		/// The "complement" grammatical relation.  A complement of a VP
		/// is any object (direct or indirect) of that VP, or a clause or
		/// adjectival phrase which functions like an object; a complement
		/// of a clause is an complement of the VP which is the predicate
		/// of that clause.<p>
		/// <p/>
		/// Examples: <br/>
		/// "She gave me a raise" &rarr;
		/// <c>comp</c>
		/// (gave, me),
		/// <c>comp</c>
		/// (gave, a raise) <br/>
		/// "I like to swim" &rarr;
		/// <c>comp</c>
		/// (like, to swim)
		/// </remarks>
		public static readonly GrammaticalRelation Complement = new GrammaticalRelation(Language.UniversalEnglish, "comp", "complement", Argument);

		/// <summary>The "object" grammatical relation.</summary>
		/// <remarks>
		/// The "object" grammatical relation.  An object of a VP
		/// is any direct object or indirect object of that VP; an object
		/// of a clause is an object of the VP which is the predicate
		/// of that clause.<p>
		/// <p/>
		/// Examples: <br/>
		/// "She gave me a raise" &rarr;
		/// <c>obj</c>
		/// (gave, me),
		/// <c>obj</c>
		/// (gave, raise)
		/// </remarks>
		public static readonly GrammaticalRelation Object = new GrammaticalRelation(Language.UniversalEnglish, "obj", "object", Complement);

		/// <summary>The "direct object" grammatical relation.</summary>
		/// <remarks>
		/// The "direct object" grammatical relation.  The direct object
		/// of a verb is the noun phrase which is the (accusative) object of
		/// the verb; the direct object of a clause or VP is the direct object of
		/// the head predicate of that clause.<p>
		/// <p/>
		/// Example: <br/>
		/// "She gave me a raise" &rarr;
		/// <c>dobj</c>
		/// (gave, raise) <p/>
		/// Note that dobj can also be assigned by the conversion of rel in the postprocessing.
		/// </remarks>
		public static readonly GrammaticalRelation DirectObject = new GrammaticalRelation(Language.UniversalEnglish, "dobj", "direct object", Object, "VP|SQ|SBARQ?", tregexCompiler, "VP !< (/^(?:VB|AUX)/ [ < " + copularWordRegex + " | < " + clausalComplementRegex
			 + " ]) < (NP|WHNP=target [ [ !<# (/^NN/ < " + timeWordRegex + ") !$+ NP ] | $+ NP-TMP | $+ (NP <# (/^NN/ < " + timeWordRegex + ")) ] ) " + " <# (__ !$++ (NP $++ (/^[:]$/ $++ =target))) ", "VP < (S < (NP|WHNP=target $++ (VP < TO)))", "SQ < (/^(?:VB)/=verb !< "
			 + copularWordRegex + ") $-- WHNP !< VP !< (/^(?:VB)/ ! == =verb) < (NP|WHNP=target [ [ !<# (/^NN/ < " + timeWordRegex + ") !$+ NP ] | $+ NP-TMP | $+ (NP <# (/^NN/ < " + timeWordRegex + ")) ] )", "SBARQ < (WHNP=target !< WRB !<# (/^NN/ < " 
			+ timeWordRegex + ")) <+(SQ|SINV|S|VP) (VP !< NP|TO !< (S < (VP < TO)) !< (/^(?:VB|AUX)/ < " + copularWordRegex + " $++ (VP < VBN|VBD)) !< (PP <: IN|TO) $-- (NP !< /^-NONE-$/))", "SBAR < (WHNP=target !< WRB) < (S < NP < (VP !< SBAR !<+(VP) (PP <- IN|TO) !< (S < (VP < TO))))"
			, "SBARQ < (WHNP=target $++ ((/^(?:VB|AUX)/ < " + copularWordRegex + ") $++ (ADJP=adj !< (PP !< NP)) $++ (NP $++ =adj)))");

		/// <summary>The "indirect object" grammatical relation.</summary>
		/// <remarks>
		/// The "indirect object" grammatical relation.  The indirect
		/// object of a VP is the noun phrase which is the (dative) object
		/// of the verb; the indirect object of a clause is the indirect
		/// object of the VP which is the predicate of that clause.
		/// <p/>
		/// Example:  <br/>
		/// "She gave me a raise" &rarr;
		/// <c>iobj</c>
		/// (gave, me)
		/// </remarks>
		public static readonly GrammaticalRelation IndirectObject = new GrammaticalRelation(Language.UniversalEnglish, "iobj", "indirect object", Object, "VP", tregexCompiler, "VP < (NP=target !< /\\$/ !<# (/^NN/ < " + timeWordRegex + ") $+ (NP !<# (/^NN/ < "
			 + timeWordRegex + ")))", "VP < (NP=target < (NP !< /\\$/ $++ (NP !<: (PRP < " + selfRegex + ") !<: DT !< (/^NN/ < " + timeWordLotRegex + ")) !$ CC|CONJP !$ /^,$/ !$++ /^:$/))");

		/// <summary>The "clausal complement" grammatical relation.</summary>
		/// <remarks>
		/// The "clausal complement" grammatical relation.  A clausal complement of
		/// a verb or adjective is a dependent clause with an internal subject which
		/// functions like an object of the verb, or adjective.  Clausal complements
		/// for nouns are limited to complement clauses with a subset of nouns
		/// like "fact" or "report".  We analyze them the same (parallel to the
		/// analysis of this class as "content clauses" in Huddleston and Pullum 2002).
		/// Clausal complements are usually finite (though there
		/// are occasional exceptions including remnant English subjunctives, and we
		/// also classify the complement of causative "have" (She had him arrested)
		/// in this category.<p>
		/// <p/>
		/// Example: <br/>
		/// "He says that you like to swim" &rarr;
		/// <c>ccomp</c>
		/// (says, like) <br/>
		/// "I am certain that he did it" &rarr;
		/// <c>ccomp</c>
		/// (certain, did) <br/>
		/// "I admire the fact that you are honest" &rarr;
		/// <c>ccomp</c>
		/// (fact, honest)
		/// </remarks>
		public static readonly GrammaticalRelation ClausalComplement = new GrammaticalRelation(Language.UniversalEnglish, "ccomp", "clausal complement", Complement, "VP|SINV|S|ADJP|ADVP|NP(?:-.*)?", tregexCompiler, "VP < (S=target < (VP !<, TO|VBG|VBN) !$-- NP)"
			, "VP < (SBAR=target < (S <+(S) VP) <, (IN|DT < /^(?i:that|whether)$/))", "VP < (SBAR=target < (SBAR < (S <+(S) VP) <, (IN|DT < /^(?i:that|whether)$/)) < CC|CONJP)", "VP < (SBAR=target < (S < VP) !$-- NP !<, (IN|WHADVP) !<2 (IN|WHADVP $- ADVP|RB))"
			, "VP < (/^V/ < " + ccompObjVerbRegex + ") < (SBAR=target < (S < VP) $-- NP !<, (IN|WHADVP) !<2 (IN|WHADVP $- ADVP|RB))", "VP < (SBAR=target < (S < VP) !$-- NP <, (WHADVP < (WRB < /^(?i:how)$/)))", "VP < @SBARQ=target", "VP < (/^VB/ < " + haveRegex
			 + ") < (S=target < @NP < VP)", "VP < (@SBAR=target !$-- @SBAR|S !$-- /^:$/ [ == @SBAR=sbar | <# @SBAR=sbar ] ) < (/^V/ < " + ccompVerbRegex + ") [ < (/^V/ < " + ccompObjVerbRegex + ") | < (=target !$-- NP) ] : (=sbar < (WHADVP|WHNP < (WRB !< /^(?i:how)$/) !$-- /^(?!RB|ADVP).*$/) !< (S < (VP < TO)))"
			, "@S|SINV < (@S|SBARQ=target $+ /^(,|\\.|'')$/ !$- /^(?:CC|CONJP|:)$/ !$- (/^(?:,)$/ $- CC|CONJP) !< (VP < TO|VBG|VBN) !< (VP <1 (VP [ <1 VBG|VBN | <2 (VBG|VBN $-- ADVP) ]))) !< (@S !== =target $++ =target !$++ @CC|CONJP)", "ADVP < (SBAR=target [ < WHNP | ( < (IN < /^(?i:as|that)/) < (S < (VP !< TO))) ])"
			, "ADJP < (SBAR=target !< (IN < as) < S)", "S <, (SBAR=target <, (IN < /^(?i:that|whether)$/) !$+ VP)", "@NP < JJ|NN|NNS < (SBAR=target [ !<(S < (VP < TO )) | !$-- NP|NN|NNP|NNS ] )");

		/// <summary>
		/// An open clausal complement (<i>xcomp</i>) of a VP or an ADJP is a clausal
		/// complement without its own subject, whose reference is determined by an
		/// external subject.
		/// </summary>
		/// <remarks>
		/// An open clausal complement (<i>xcomp</i>) of a VP or an ADJP is a clausal
		/// complement without its own subject, whose reference is determined by an
		/// external subject.  These complements are always non-finite.
		/// The name <i>xcomp</i> is borrowed from Lexical-Functional Grammar.
		/// (Mainly "TO-clause" are recognized, but also some VBG like "stop eating")
		/// <p/>
		/// <p/>
		/// Examples: <br/>
		/// "I like to swim" &rarr;
		/// <c>xcomp</c>
		/// (like, swim) <br/>
		/// "I am ready to leave" &rarr;
		/// <c>xcomp</c>
		/// (ready, leave)
		/// </remarks>
		public static readonly GrammaticalRelation XclausalComplement = new GrammaticalRelation(Language.UniversalEnglish, "xcomp", "xclausal complement", Complement, "VP|ADJP|SINV", tregexCompiler, "VP < (S=target [ !$-- NP | $-- (/^V/ < " + xcompVerbRegex
			 + ") ] !$- (NN < order) < (VP < TO))", "ADJP < (S=target <, (VP <, TO))", "VP < (S=target !$- (NN < order) < (NP $+ NP|ADJP))", "VP <# (/^(?:VB|AUX)/ $+ (VP=target < VB|VBG))", "VP < (SBAR=target < (S !$- (NN < order) < (VP < TO))) !> (VP < (VB|AUX < be)) "
			, "VP < (S=target !$- (NN < order) <: NP) > VP", "VP < (S=target !< VP)", "VP < (/^VB/ $+ (@S=target < (@ADJP < /^JJ/ ! $-- @NP|S))) $-- (/^VB/ < " + copularWordRegex + " )", "(VP < (S=target < (VP < VBG ) !< NP !$- (/^,$/ [$- @NP|VP | $- (@PP $-- @NP ) |$- (@ADVP $-- @NP)]) !$-- /^:$/ !$-- VBG))"
			, "(VP $-- (/^(?:VB|AUX)/ < " + copularWordRegex + ") < (/^VB/ < " + clausalComplementRegex + ") < NP=target)", "VP < (/^(?:VB|AUX)/ < " + clausalComplementRegex + ") < (NP|WHNP=target [ [ !<# (/^NN/ < " + timeWordRegex + ") !$+ NP ] | $+ NP-TMP | $+ (NP <# (/^NN/ < "
			 + timeWordRegex + ")) ] ) " + " <# (__ !$++ (NP $++ (/^[:]$/ $++ =target))) ", "VP=vp < NP=target <(/^(?:VB|AUX)/ < " + copularWordRegex + " >># =vp) !$ (NP < EX)", "SINV <# (VP < (/^(?:VB|AUX)/ < " + copularWordRegex + ") $-- (NP $-- NP=target))"
			, "VP [ < ADJP=target | ( < (/^VB/ [ ( < " + clausalComplementRegex + " $++ VP=target ) | $+ (@S=target < (@ADJP < /^JJ/ ! $-- @NP|S)) ] ) !$-- (/^VB/ < " + copularWordRegex + " )) ]");

		/// <summary>
		/// The RELATIVE grammatical relation is only here as a temporary
		/// relation.
		/// </summary>
		/// <remarks>
		/// The RELATIVE grammatical relation is only here as a temporary
		/// relation.  This tregex triggering indicates either a dobj or a
		/// pobj should be here.  We figure this out in a post-processing
		/// step by looking at the surrounding dependencies.
		/// </remarks>
		public static readonly GrammaticalRelation Relative = new GrammaticalRelation(Language.UniversalEnglish, "rel", "relative", Complement, "SBAR|SBARQ", tregexCompiler, "SBAR < (WHNP=target !< WRB) < (S < NP < (VP [ < SBAR | <+(VP) (PP <- IN|TO) | < (S < (VP < TO)) ] ))"
			, "SBARQ < (WHNP=target !< WRB !<# (/^NN/ < " + timeWordRegex + ")) <+(SQ|SINV) (/^(?:VB|AUX)/ < " + copularWordRegex + " !$++ VP)");

		/// <summary>
		/// The PREPOSITION grammatical relation is only here as a temporary
		/// relation.
		/// </summary>
		/// <remarks>
		/// The PREPOSITION grammatical relation is only here as a temporary
		/// relation. It matches prepositions in sentences such as
		/// "What is the esophagus used for?" which are attached to the
		/// nominal modifier in a post-processing step.
		/// </remarks>
		public static readonly GrammaticalRelation Preposition = new GrammaticalRelation(Language.UniversalEnglish, "prep", "preposition", Complement, "VP|ADJP", tregexCompiler, "VP|ADJP < (PP=target <: IN|TO)");

		/// <summary>The "referent" grammatical relation.</summary>
		/// <remarks>
		/// The "referent" grammatical relation.  A
		/// referent of the Wh-word of a NP is  the relative word introducing the relative clause modifying the NP.
		/// <p/>
		/// Example: <br/>
		/// "I saw the book which you bought" &rarr;
		/// <c>ref</c>
		/// (book, which) <br/>
		/// "I saw the book the cover of which you designed" &rarr;
		/// <c>ref</c>
		/// (book, which)
		/// </remarks>
		public static readonly GrammaticalRelation Referent = new GrammaticalRelation(Language.UniversalEnglish, "ref", "referent", Dependent);

		/// <summary>The "expletive" grammatical relation.</summary>
		/// <remarks>
		/// The "expletive" grammatical relation.
		/// This relation captures an existential there.
		/// <p/>
		/// <p/>
		/// Example: <br/>
		/// "There is a statue in the corner" &rarr;
		/// <c>expl</c>
		/// (is, there)
		/// </remarks>
		public static readonly GrammaticalRelation Expletive = new GrammaticalRelation(Language.UniversalEnglish, "expl", "expletive", Dependent, "S|SQ|SINV", tregexCompiler, "S|SQ|SINV < (NP=target <+(NP) EX)");

		/// <summary>The "modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "modifier" grammatical relation.  A modifier of a VP is
		/// any constituent that serves to modify the meaning of the VP
		/// (but is not an
		/// <c>ARGUMENT</c>
		/// of that
		/// VP); a modifier of a clause is an modifier of the VP which is
		/// the predicate of that clause.<p>
		/// <p/>
		/// Examples: <br/>
		/// "Last night, I swam in the pool" &rarr;
		/// <c>mod</c>
		/// (swam, in the pool),
		/// <c>mod</c>
		/// (swam, last night)
		/// </remarks>
		public static readonly GrammaticalRelation Modifier = new GrammaticalRelation(Language.UniversalEnglish, "mod", "modifier", Dependent);

		/// <summary>The "nominal modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "nominal modifier" grammatical relation.  The nmod relation is
		/// used for nominal modifiers of nouns or clausal predicates.
		/// <c>nmod</c>
		/// is a noun functioning as a non-core (oblique) argument or adjunct.
		/// In English, nmod is used for prepositional complements.
		/// <p/>
		/// (The preposition in turn may be modifying a noun, verb, etc.)
		/// We here define cases of VBG quasi-prepositions like "including",
		/// "concerning", etc. as instances of pobj (unlike the Penn Treebank).
		/// <p/>
		/// Example: <br/>
		/// "I sat on the chair" &rarr;
		/// <c>nmod</c>
		/// (sat, chair)
		/// <p/>
		/// (The preposition can be called a FW for pace, versus, etc.  It can also
		/// be called a CC - but we don't currently handle that and would need to
		/// distinguish from conjoined PPs. Jan 2010 update: We now insist that the
		/// NP must follow the preposition. This prevents a preceding NP measure
		/// phrase being matched as a nmod.  We do allow a preposition tagged RB
		/// followed by an NP pobj, as happens in the Penn Treebank for adverbial uses
		/// of PP like "up 19%")
		/// </remarks>
		public static readonly GrammaticalRelation NominalModifier = new GrammaticalRelation(Language.UniversalEnglish, "nmod", "nominal modifier", Modifier, ".*", tregexCompiler, "/^(?:(?:WH)?(?:NP|ADJP|ADVP|NX|NML)(?:-TMP|-ADV)?|VP|NAC|SQ|FRAG|PRN|X|RRC)$/ < (WHPP|WHPP-TMP|PP|PP-TMP=target [< @NP|WHNP|NML | < (PP < @NP|WHNP|NML)]) !<- "
			 + EtcPat + " !<- " + FwEtcPat, "/^(?:(?:WH)?(?:NP|ADJP|ADVP|NX|NML)(?:-TMP|-ADV)?|VP|NAC|SQ|FRAG|PRN|X|RRC)$/ < (S=target <: WHPP|WHPP-TMP|PP|PP-TMP)", "WHPP|WHPP-TMP|WHPP-ADV|PP|PP-TMP|PP-ADV < (WHPP|WHPP-TMP|WHPP-ADV|PP|PP-TMP|PP-ADV=target !$- IN|VBG|VBN|TO)"
			, "S|SINV < (PP|PP-TMP=target !< SBAR|S) < VP|S", "SBAR|SBARQ < /^(?:WH)?PP/=target < S|SQ", "@NP < (@UCP|PRN=target <# @PP)", "SBARQ < (WHNP=target $++ ((/^(?:VB|AUX)/ < " + copularWordRegex + ") $++ (ADJP=adj < (PP <: IN)) $++ (NP $++ =adj)))"
			, "SBARQ < (WHNP=target [$++ (VP < (PP <: IN)) | $++ (SQ < (VP < (PP <: IN)))])");

		/// <summary>The "adverbial clause modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "adverbial clause modifier" grammatical relation. An adverbial
		/// clause modifier is a clause which modifies a verb or other predicate
		/// (adjective, etc.), as a modifier not as a core complement. This includes
		/// things such as a temporal clause, consequence, conditional clause,
		/// purpose clause, etc. The dependent must be clausal (or else it is an
		/// <c>advmod</c>
		/// ) and the dependent is the main predicate of the clause.
		/// <p/>
		/// Examples: <br/>
		/// "The accident happened as the night was falling" &rarr;
		/// <c>advcl</c>
		/// (happened, falling) <br/>
		/// "If you know who did it, you should tell the teacher" &rarr;
		/// <c>advcl</c>
		/// (tell, know)
		/// </remarks>
		public static readonly GrammaticalRelation AdvClauseModifier = new GrammaticalRelation(Language.UniversalEnglish, "advcl", "adverbial clause modifier", Modifier, "VP|S|SQ|SINV|SBARQ|NP|ADVP|ADJP", tregexCompiler, "VP < (@SBAR=target <= (@SBAR [ < (IN|MWE !< /^(?i:that|whether)$/) | <: (SINV <1 /^(?:VB|MD|AUX)/) | < (RB|IN < so|now) < (IN < that) | <1 (ADVP < (RB < now)) <2 (IN < that) ] ))"
			, "S|SQ|SINV < (SBAR|SBAR-TMP=target <, (IN|MWE !< /^(?i:that|whether)$/ !$+ (NN < order)) !$-- /^(?!CC|CONJP|``|,|INTJ|PP(-.*)?).*$/ !$+ VP)", "S|SQ|SINV < (SBAR|SBAR-TMP=target <, (IN|MWE !< /^(?i:that|whether)$/ !$+ (NN < order)) !$+ @VP $+ /^,$/ $++ @NP)"
			, "SBARQ < (SBAR|SBAR-TMP|SBAR-ADV=target <, (IN|MWE !< /^(?i:that|whether)$/ !$+ (NN < order)) $+ /^,$/ $++ @SQ|S|SBARQ)", "S|SQ < (@SBAR=target [ == @SBAR=sbar | <# @SBAR=sbar ] ): (=sbar < (WHADVP|WHNP < (WRB !< /^(?i:how)$/) !$-- /^(?!RB|ADVP).*$/) !< (S < (VP < TO)) !$-- /^:$/)"
			, "VP < (@SBAR=target !$-- /^:$/ [ == @SBAR=sbar | <# @SBAR=sbar ] ) [ !< (/^V/ < " + ccompVerbRegex + ") | < (=target $-- @SBAR|S) | ( !< (/^V/ < " + ccompObjVerbRegex + ") < (=target $-- NP)) ] : (=sbar < (WHADVP|WHNP < (WRB !< /^(?i:how)$/) !$-- /^(?!RB|ADVP).*$/) !< (S < (VP < TO)))"
			, "@S < (@SBAR=target $++ @NP $++ @VP)", "@S < (@S=target < (VP < TO) $+ (/^,$/ $++ @NP))", "NP < (NP $++ (SBAR=target < (IN|MWE < /^(?i:than)$/) !< (WHPP|WHNP|WHADVP) < (S < (@NP $++ (VP !< (/^(?:VB|AUX)/ < " + copularWordRegex + " !$+ VP)  !<+(VP) (/^(?:VB|AUX)/ < "
			 + copularWordRegex + " $+ (VP < VBN|VBD)) !<+(VP) NP !< SBAR !<+(VP) (PP <- IN|TO|MWE)))) !<: (S !< (VP < TO))) !$++ (CC $++ =target))", "ADVP < ADVP < SBAR=target", "S|SINV < (S=target (< VP=verb | < (VP <1 VP=verb)) [ $- (/^,$/ [ $- @NP | $- (@PP $ @NP) ] ) | $+ (/^,$/ $+ @NP) ] ) : (=verb [ <1 VBG|VBN | <2 (VBG|VBN $-- ADVP) ])"
			, "(VP < (@S=target < (VP [ <1 VBG|VBN | <2 (VBG|VBN $-- ADVP) ]) $- (/^,$/ [$- @NP|VP | $- (@PP $-- @NP ) |$- (@ADVP $-- @NP)])))", "(VP < (S=target < (VP < VBG) $-- VBG=ing !$-- (/^[:]$/ $-- =ing)))", "VP < (S=target $-- NP < (VP < TO) !$-- (/^V/ < "
			 + xcompVerbRegex + ") )", "SBARQ < WHNP < (S=target < (VP <1 TO))", "/^(?:(?:WH)?(?:ADJP|ADVP)(?:-TMP|-ADV)?|VP|SQ|FRAG|PRN|X|RRC|S)$/ < (WHPP|WHPP-TMP|PP|PP-TMP=target !< @NP|WHNP|NML !$- (@CC|CONJP $- __) !<: IN|TO !< @CC|CONJP < /^((?!(PP|IN)).)*$/) !<- "
			 + EtcPat + " !<- " + FwEtcPat, "VP|ADJP < /^PP(?:-TMP|-ADV)?$/=target < (@PP < @SBAR|S $++ CONJP|CC)");

		/// <summary>The "marker" grammatical relation.</summary>
		/// <remarks>
		/// The "marker" grammatical relation.  A marker is the word introducing a finite clause subordinate to another clause.
		/// For a complement clause, this will typically be "that" or "whether".
		/// For an adverbial clause, the marker is typically a preposition like "while" or "although".
		/// <p/>
		/// Example: <br/>
		/// "U.S. forces have been engaged in intense fighting after insurgents launched simultaneous attacks" &rarr;
		/// <c>mark</c>
		/// (launched, after)
		/// </remarks>
		public static readonly GrammaticalRelation Marker = new GrammaticalRelation(Language.UniversalEnglish, "mark", "marker", Modifier, "SBAR(?:-TMP)?|VP|PP(?:-TMP|-ADV)?", tregexCompiler, "VP < VP < (TO=target)", "SBAR|SBAR-TMP < (IN|DT|MWE=target $++ S|FRAG)"
			, "SBAR < (IN|DT=target < that|whether) [ $-- /^(?:VB|AUX)/ | $- NP|NN|NNS | > ADJP|PP | > (@NP|UCP|SBAR < CC|CONJP $-- /^(?:VB|AUX)/) ]", "/^PP(?:-TMP|-ADV)?$/ < (IN|TO|MWE|PCONJP|VBN|JJ=target $+ @SBAR|S)");

		/// <summary>The "adjectival modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "adjectival modifier" grammatical relation.  An adjectival
		/// modifier of an NP is any adjectival phrase that serves to modify
		/// the meaning of the NP.<p>
		/// <p/>
		/// Example: <br/>
		/// "Sam eats red meat" &rarr;
		/// <c>amod</c>
		/// (meat, red) <p/>
		/// The relation amod is also used for multiword country adjectives, despite their
		/// questionable treebank representation.
		/// <p/>
		/// Example: <br/>
		/// "the West German economy" &rarr;
		/// <c>amod</c>
		/// (German, West),
		/// <c>amod</c>
		/// (economy, German)
		/// </remarks>
		public static readonly GrammaticalRelation AdjectivalModifier = new GrammaticalRelation(Language.UniversalEnglish, "amod", "adjectival modifier", Modifier, "NP(?:-TMP|-ADV)?|NX|NML|NAC|WHNP|ADJP|INTJ", tregexCompiler, "/^(?:NP(?:-TMP|-ADV)?|NX|NML|NAC|WHNP|INTJ)$/ < (ADJP|WHADJP|JJ|JJR|JJS|JJP|VBN|VBG|VBD|IN=target !< (QP !< /^[$]$/) !$- CC)"
			, "ADJP !< CC|CONJP < (JJ|NNP $ JJ|NNP=target)", "WHNP|WHNP-TMP|WHNP-ADV|NP|NP-TMP|NP-ADV < (NP=target <: CD $- /^,$/ $-- /^(?:WH)?NP/ !$ CC|CONJP)");

		/// <summary>The "numeric modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "numeric modifier" grammatical relation.  A numeric
		/// modifier of an NP is any number phrase that serves to modify
		/// the meaning of the NP.<p>
		/// <p/>
		/// Example: <br/>
		/// "Sam eats 3 sheep" &rarr;
		/// <c>nummod</c>
		/// (sheep, 3)
		/// </remarks>
		public static readonly GrammaticalRelation NumericModifier = new GrammaticalRelation(Language.UniversalEnglish, "nummod", "numeric modifier", Modifier, "(?:WH)?NP(?:-TMP|-ADV)?|NML|NX|ADJP|WHADJP|QP", tregexCompiler, "/^(?:WH)?(?:NP|NX|NML)(?:-TMP|-ADV)?$/ < (CD|QP=target !$- CC)"
			, "/^(?:WH)?(?:NP|NX|NML)(?:-TMP|-ADV)?$/ < (ADJP=target <: (QP !< /^[$]$/))", "QP < QP=target < /^[$]$/");

		/// <summary>The "compound modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "compound modifier" grammatical relation.  A compound
		/// modifier of an NP is any noun that serves to modify the head noun.
		/// Note that this has all nouns modify the rightmost a la Penn headship
		/// rules.  There is no intelligent noun compound analysis.
		/// <p/>
		/// We eliminate nouns that are detected as part of a POS, since that
		/// will turn into the dependencies denoting possession instead.
		/// Note we have to include (VBZ &lt; /^\'s$/) as part of the POS
		/// elimination, since quite a lot of text such as
		/// "yesterday's widely published sequester" was misannotated as a
		/// VBZ instead of a POS.  TODO: remove that if a revised PTB is ever
		/// released.
		/// <p/>
		/// Example: <br/>
		/// "Oil price futures" &rarr;
		/// <c>compound</c>
		/// (futures, oil),
		/// <c>compound</c>
		/// (futures, price) <p/>
		/// Numbers consisting of multiple words are also treated as compounds.
		/// <p/>
		/// Example: <br/>
		/// "I have four thousand sheep" &rarr;
		/// <c>compound</c>
		/// (thousand, four) <p/>
		/// </remarks>
		public static readonly GrammaticalRelation CompoundModifier = new GrammaticalRelation(Language.UniversalEnglish, "compound", "compound modifier", Modifier, "(?:WH)?(?:NP|NX|NAC|NML|ADVP|ADJP|QP)(?:-TMP|-ADV)?", tregexCompiler, "/^(?:WH)?(?:NP|NX|NAC|NML)(?:-TMP|-ADV)?$/ < (NP|NML|NN|NNS|NNP|NNPS|FW|AFX=target $++ NN|NNS|NNP|NNPS|FW|CD=sister !<<- POS !<<- (VBZ < /^\'s$/) !$- /^,$/ !$++ (POS $++ =sister))"
			, "/^(?:WH)?(?:NP|NX|NAC|NML)(?:-TMP|-ADV)?$/ < JJ|JJR|JJS=sister < (NP|NML|NN|NNS|NNP|NNPS|FW=target !<<- POS !<<- (VBZ < /^\'s$/) $+ =sister) <# NN|NNS|NNP|NNPS !<<- POS !<<- (VBZ < /^\'s$/) ", "QP|ADJP < (/^(?:CD|$|#)$/=target !$- CC)", 
			"ADJP|ADVP < (FW [ $- (FW=target !< /^(?i:etc)$/) | $- (IN=target < in|In) ] )");

		/// <summary>The "name" relation.</summary>
		/// <remarks>
		/// The "name" relation. This relation is used for proper
		/// nouns constituted of multiple nominal elements.  Words joined by name should all be part of a
		/// minimal noun phrase; otherwise regular syntactic relations should be used.
		/// In general, names are annotated in a flat, head-initial structure, in which all words in the name
		/// modify the first one using the
		/// <c>name</c>
		/// label.
		/// <p/>
		/// The distinction between
		/// <c>compound</c>
		/// and
		/// <c>name</c>
		/// can only be made on the basis of NER tags.
		/// For this reason, we use the
		/// <c>compound</c>
		/// relation for all flat NPs and replace it with the
		/// <c>name</c>
		/// relation during post-processing.
		/// <p/>
		/// See also
		/// <see cref="UniversalEnglishGrammaticalStructure.ProcessNames(Edu.Stanford.Nlp.Semgraph.SemanticGraph)"/>
		/// .
		/// <p/>
		/// Example: <br/>
		/// "Hillary Rodham Clinton" &rarr;
		/// <c>name</c>
		/// (Hillary, Rodham),
		/// <c>name</c>
		/// (Hillary, Clinton)<p/>
		/// </remarks>
		public static readonly GrammaticalRelation NameModifier = new GrammaticalRelation(Language.UniversalEnglish, "name", "name", Modifier);

		/// <summary>The "appositional modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "appositional modifier" grammatical relation.  An appositional
		/// modifier of an NP is an NP that serves to modify
		/// the meaning of the NP.  It includes parenthesized examples, as well as defining abbreviations.
		/// <p/>
		/// Examples: <br/>
		/// "Sam, my brother, eats red meat" &rarr;
		/// <c>appos</c>
		/// (Sam, brother) <br/>
		/// "Bill (John's cousin)" &rarr;
		/// <c>appos</c>
		/// (Bill, cousin).
		/// "The Australian Broadcasting Corporation (ABC)" &rarr;
		/// <c>appos</c>
		/// (Corporation, ABC)
		/// </remarks>
		public static readonly GrammaticalRelation AppositionalModifier = new GrammaticalRelation(Language.UniversalEnglish, "appos", "appositional modifier", Modifier, "(?:WH)?NP(?:-TMP|-ADV)?|FRAG", tregexCompiler, "WHNP|WHNP-TMP|WHNP-ADV|NP|NP-TMP|NP-ADV|FRAG < (NP=target !<: CD $- /^,$/ $-- /^(?:WH)?NP/) !< CC|CONJP !< "
			 + FwEtcPat + " !< " + EtcPat, "WHNP|WHNP-TMP|WHNP-ADV|NP|NP-TMP|NP-ADV|FRAG < (PRN=target < (NP < /^(?:NN|CD)/ $-- /^-LRB-$/ $+ /^-RRB-$/))", "@WHNP|NP < (NP=target !<: CD <, /^-LRB-$/ <` /^-RRB-$/ $-- /^(?:WH)?NP/ !$ CC|CONJP)", "NP|NP-TMP|NP-ADV < (NNP $+ (/^,$/ $+ NNP=target)) !< CC|CONJP !< "
			 + FwEtcPat + " !< " + EtcPat, "WHNP|WHNP-TMP|WHNP-ADV|NP|NP-TMP|NP-ADV < (PRN=target <, /^-LRB-$/ <- /^-RRB-$/ !<< /^(?:POS|(?:WP|PRP)\\$|[,$#]|CC|RB|CD)$/ <+(NP) (NNP|NN < /^(?:[A-Z]\\.?){2,}/) )", "WHNP|WHNP-TMP|WHNP-ADV|NP|NP-TMP|NP-ADV < (NP=target <: NNP $- (/^(?:WH)?NP/ !< POS)) !< CC|CONJP !< "
			 + FwEtcPat + " !< " + EtcPat, "FRAG|NP < (NP $+ (/:/ $+ @SQ|S=target) <: NN|NNS)");

		/// <summary>The "discourse element" grammatical relation.</summary>
		/// <remarks>
		/// The "discourse element" grammatical relation. This is used for interjections and
		/// other discourse particles and elements (which are not clearly linked to the structure
		/// of the sentence, except in an expressive way). We generally follow the
		/// guidelines of what the Penn Treebanks count as an INTJ.  They
		/// define this to include: interjections (oh, uh-huh, Welcome), fillers (um, ah),
		/// and discourse markers (well, like, actually, but not: you know).
		/// We also use it for emoticons.
		/// </remarks>
		public static readonly GrammaticalRelation DiscourseElement = new GrammaticalRelation(Language.UniversalEnglish, "discourse", "discourse element", Modifier, ".*", tregexCompiler, "__ < (NFP=target [ < " + WesternSmiley + " | < " + AsianSmiley
			 + " ] )", "__ [ < INTJ=target | < (PRN=target <1 /^(?:,|-LRB-)$/ <2 INTJ [ !<3 __ | <3 /^(?:,|-RRB-)$/ ] ) ]");

		/// <summary>The "clausal modifier of noun" relation.</summary>
		/// <remarks>
		/// The "clausal modifier of noun" relation.
		/// <c>acl</c>
		/// is used for
		/// finite and non-finite clauses that modify a noun. Note that in
		/// English relative clauses get assigned a specific relation
		/// <code>acl:relcl</code>, a subtype of
		/// <c>acl</c>
		/// .
		/// <p/>
		/// Examples: <br/>
		/// "the issues as he sees them" &rarr;
		/// <c>acl</c>
		/// (issues, sees) <br/>
		/// </remarks>
		public static readonly GrammaticalRelation ClausalModifier = new GrammaticalRelation(Language.UniversalEnglish, "acl", "clausal modifier of noun", Modifier, "WHNP|WHNP-TMP|WHNP-ADV|NP(?:-[A-Z]+)?|NML|NX", tregexCompiler, "WHNP|WHNP-TMP|WHNP-ADV|NP|NP-TMP|NP-ADV|NML|NX < (VP=target < VBG|VBN|VBD $-- @NP|NML|NX)"
			, "WHNP|WHNP-TMP|WHNP-ADV|NP|NP-TMP|NP-ADV|NML|NX < (/^,$/ $+ (VP=target [ <1 VBG|VBN | <2 (VBG|VBN $-- ADVP) ]))", "/^(?:(?:WH)?(?:NP|NX|NML)(?:-TMP|-ADV)?)$/ < (WHPP|WHPP-TMP|PP|PP-TMP=target !< @NP|WHNP|NML !$- (@CC|CONJP $- __) < /^((?!(PP|CC|CONJP|,)).)*$/  !< (@PP <1 IN|RB|MWE|PCONJP|VBN|JJ <2 @NP))  !<- "
			 + EtcPat + " !<- " + FwEtcPat, "/^NP(?:-[A-Z]+)?$/ < (S=target < (VP < TO) $-- NP|NN|NNP|NNS)", "/^NP(?:-[A-Z]+)?$/ < (SBAR=target < (S < (VP < TO)) $-- NP|NN|NNP|NNS)");

		/// <summary>The "relative clause modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "relative clause modifier" grammatical relation.  A relative clause
		/// modifier of an NP is a relative clause modifying the NP.  The link
		/// points from the head noun of the NP to the head of the relative clause,
		/// normally a verb.
		/// <p/>
		/// <p/>
		/// Examples: <br/>
		/// "I saw the man you love" &rarr;
		/// <c>relcl</c>
		/// (man, love)  <br/>
		/// "I saw the book which you bought" &rarr;
		/// <c>relcl</c>
		/// (book, bought)
		/// </remarks>
		public static readonly GrammaticalRelation RelativeClauseModifier = new GrammaticalRelation(Language.UniversalEnglish, "acl:relcl", "relative clause modifier", ClausalModifier, "(?:WH)?(?:NP|NML|ADVP)(?:-.*)?", tregexCompiler, "@NP|WHNP|NML=np $++ (SBAR=target [ <+(SBAR) WHPP|WHNP | <: (S !< (VP < TO)) ]) !$-- @NP|WHNP|NML !$++ "
			 + EtcPat + " !$++ " + FwEtcPat + " > @NP|WHNP : (=np !$++ (CC|CONJP $++ =target))", "NP|NML $++ (SBAR=target < (WHADVP < (WRB </^(?i:where|why|when)/))) !$-- NP|NML !$++ " + EtcPat + " !$++ " + FwEtcPat + " > @NP", "@NP|WHNP < RRC=target <# NP|WHNP|NML|DT|S"
			, "@ADVP < (@ADVP < (RB < /where$/)) < @SBAR=target", "NP < (NP $++ (SBAR=target !< (IN < /^(?i:than|that|whether)$/) !< (WHPP|WHNP|WHADVP) < (S < (@NP $++ (VP !< (/^(?:VB|AUX)/ < " + copularWordRegex + " !$+ VP)  !<+(VP) (/^(?:VB|AUX)/ < "
			 + copularWordRegex + " $+ (VP < VBN|VBD)) !<+(VP) NP !< SBAR !<+(VP) (PP <- IN|TO)))) !<: (S !< (VP < TO))) !$++ (CC $++ =target))");

		/// <summary>The "adverbial modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "adverbial modifier" grammatical relation.  An adverbial
		/// modifier of a word is a (non-clausal) RB or ADVP that serves to modify
		/// the meaning of the word.<p>
		/// <p/>
		/// Examples: <br/>
		/// "genetically modified food" &rarr;
		/// <c>advmod</c>
		/// (modified, genetically) <br/>
		/// "less often" &rarr;
		/// <c>advmod</c>
		/// (often, less)
		/// </remarks>
		public static readonly GrammaticalRelation AdverbialModifier = new GrammaticalRelation(Language.UniversalEnglish, "advmod", "adverbial modifier", Modifier, "VP|ADJP|WHADJP|ADVP|WHADVP|S|SBAR|SINV|SQ|SBARQ|XS|(?:WH)?(?:PP|NP)(?:-TMP|-ADV)?|RRC|CONJP|JJP|QP"
			, tregexCompiler, "/^(?:VP|ADJP|JJP|WHADJP|SQ?|SBARQ?|SINV|XS|RRC|(?:WH)?NP(?:-TMP|-ADV)?)$/ < (RB|RBR|RBS|WRB|ADVP|WHADVP=target !< " + NotPat + " !< " + EtcPat + " [!<+(/ADVP/) (@ADVP < (IN < /(?i:at)/)) |  !<+(/ADVP/) (@ADVP < NP)] )", "QP < IN|RB|RBR|RBS|PDT|DT|JJ|JJR|JJS|XS=target"
			, "QP < (MWE=target < (JJR|RBR|IN < /^(?i)(more|less)$/) < (IN < /^(?i)than$/))", "ADVP|WHADVP < (RB|RBR|RBS|WRB|ADVP|WHADVP|JJ=target !< " + NotPat + " !< /^(?i:no)$/ !< " + EtcPat + ") [ !< /^CC|CONJP$/ | ( <#__=head !< (/^CC|CONJP$/ [ ($++ =head $-- =target) | ($-- =head $++ =target) ])) ]"
			, "SBAR < (WHNP=target < WRB)", "SBARQ <, WHADVP=target", "XS < JJ=target", "/(?:WH)?PP(?:-TMP|-ADV)?$/ <# (__ $-- (RB|RBR|RBS|WRB|ADVP|WHADVP=target !< " + NotPat + " !< " + EtcPat + "))", "/(?:WH)?PP(?:-TMP|-ADV)?$/ < @NP|WHNP < (RB|RBR|RBS|WRB|ADVP|WHADVP=target !< "
			 + NotPat + " !< " + EtcPat + ")", "CONJP < (RB=target !< " + NotPat + " !< " + EtcPat + ")");

		/// <summary>The "negation modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "negation modifier" grammatical relation.  The negation modifier
		/// is the relation between a negation word and the word it modifies.
		/// <p/>
		/// Examples: <br/>
		/// "Bill is not a scientist" &rarr;
		/// <c>neg</c>
		/// (scientist, not) <br/>
		/// "Bill doesn't drive" &rarr;
		/// <c>neg</c>
		/// (drive, n't)
		/// </remarks>
		public static readonly GrammaticalRelation NegationModifier = new GrammaticalRelation(Language.UniversalEnglish, "neg", "negation modifier", AdverbialModifier, "VP|ADJP|S|SBAR|SINV|SQ|NP(?:-TMP|-ADV)?|FRAG|CONJP|PP|NAC|NML|NX|ADVP|WHADVP", tregexCompiler
			, "/^(?:VP|NP(?:-TMP|-ADV)?|ADJP|SQ|S|FRAG|CONJP|PP)$/< (RB=target < " + NotPat + ")", "VP|ADJP|S|SBAR|SINV|FRAG < (ADVP=target <# (RB < " + NotPat + "))", "VP > SQ $-- (RB=target < " + NotPat + ")", "/^(?:NP(?:-TMP|-ADV)?|NAC|NML|NX|ADJP|ADVP)$/ < (DT|RB=target < /^(?i:no)$/ "
			 + " $++ /^(?:N[MNXP]|CD|JJ|JJR|FW|ADJP|QP|RB|RBR|PRP(?![$])|PRN)/ " + ")", "ADVP|WHADVP < (RB|RBR|RBS|WRB|ADVP|WHADVP|JJ=target < /^(?i:no)$/) !< CC|CONJP");

		/// <summary>The "noun phrase as adverbial modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "noun phrase as adverbial modifier" grammatical relation.
		/// This relation captures various places where something syntactically a noun
		/// phrase is used as an adverbial modifier in a sentence.  These usages include:
		/// <ul>
		/// <li> A measure phrase, which is the relation between
		/// the head of an ADJP/ADVP and the head of a measure-phrase modifying the ADJP/ADVP.
		/// <p/>
		/// Example: <br/>
		/// "The director is 65 years old" &rarr;
		/// <c>npadvmod</c>
		/// (old, years)
		/// </li>
		/// <li> Noun phrases giving extent inside a VP which are not objects
		/// <p/>
		/// Example: <br/>
		/// "Shares eased a fraction" &rarr;
		/// <c>npadvmod</c>
		/// (eased, fraction)
		/// </li>
		/// <li> Financial constructions involving an adverbial or PP-like NP, notably
		/// the following construction where the NP means "per share"
		/// <p/>
		/// Example: <br/>
		/// "IBM earned $ 5 a share" &rarr;
		/// <c>npadvmod</c>
		/// ($, share)
		/// </li>
		/// <li>Floating reflexives
		/// <p/>
		/// Example: <br/>
		/// "The silence is itself significant" &rarr;
		/// <c>npadvmod</c>
		/// (significant, itself)
		/// </li>
		/// <li>Certain other absolutive NP constructions.
		/// <p/>
		/// Example: <br/>
		/// "90% of Australians like him, the most of any country" &rarr;
		/// <c>npadvmod</c>
		/// (like, most)
		/// </ul>
		/// A temporal modifier (tmod) is a subclass of npadvmod which is distinguished
		/// as a separate relation.
		/// </remarks>
		public static readonly GrammaticalRelation NpAdverbialModifier = new GrammaticalRelation(Language.UniversalEnglish, "nmod:npmod", "noun phrase adverbial modifier", Modifier, "VP|(?:WH)?(?:NP|ADJP|ADVP|PP|QP)(?:-TMP|-ADV)?", tregexCompiler, "@ADVP|ADJP|WHADJP|WHADVP|PP|WHPP <# (JJ|JJR|IN|RB|RBR !< notwithstanding $- (@NP=target !< NNP|NNPS))"
			, "@ADJP < (NN=target $++ /^JJ/) !< CC|CONJP", "@ADVP <# (/^(RB|ADVP)/ $++ @NP=target)", "@NP|WHNP < /^NP-ADV/=target", "@NP|WHNP [ < (NP=target <: (PRP < " + selfRegex + ")) | < (PRP=target < " + selfRegex + ") ] : (=target $-- NP|NN|NNS|NNP|NNPS|PRP=noun !$-- (/^,|CC|CONJP$/ $-- =noun))"
			, "@NP <1 (@NP <<# /^%$/) <2 (@NP=target <<# days|month|months) !<3 __", "@VP < /^NP-ADV/=target", "@NP|ADVP|QP <+(/ADVP/) (@ADVP=target < (IN < /(?i:at)/) < NP)");

		/// <summary>The "temporal modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "temporal modifier" grammatical relation.  A temporal modifier
		/// is a subtype of the nmod relation: if the modifier is specifying
		/// a time, it is labeled as tmod.
		/// <p/>
		/// Example: <br/>
		/// "Last night, I swam in the pool" &rarr;
		/// <c>nmod:tmod</c>
		/// (swam, night)
		/// </remarks>
		public static readonly GrammaticalRelation TemporalModifier = new GrammaticalRelation(Language.UniversalEnglish, "nmod:tmod", "temporal modifier", NominalModifier, "VP|S|ADJP|PP|SBAR|SBARQ|NP|RRC", tregexCompiler, "VP|ADJP|RRC [ < NP-TMP=target | < (VP=target <# NP-TMP !$ /^,|CC|CONJP$/) | < (NP=target <# (/^NN/ < "
			 + timeWordRegex + ") !$+ (/^JJ/ < old)) ]", "@PP < (IN|TO|VBG|FW $++ (@NP [ $+ NP-TMP=target | $+ (NP=target <# (/^NN/ < " + timeWordRegex + ")) ]))", "S < (NP-TMP=target $++ VP $ NP )", "S < (NP=target <# (/^NN/ < " + timeWordRegex + ") $++ (NP $++ VP))"
			, "SBAR < (@WHADVP < (WRB < when)) < (S < (NP $+ (VP !< (/^(?:VB|AUX)/ < " + copularWordRegex + " !$+ VP) ))) !$-- CC $-- NP > NP=target", "SBARQ < (@WHNP=target <# (/^NN/ < " + timeWordRegex + ")) < (SQ < @NP)", "NP < NP-TMP=target");

		/// <summary>The "multi-word expression" grammatical relation.</summary>
		/// <remarks>
		/// The "multi-word expression" grammatical relation.
		/// This covers various multi-word constructions for which it would
		/// seem pointless or arbitrary to claim grammatical relations between words:
		/// as well as, rather than, instead of, but also;
		/// such as, because of, all but, in addition to ....
		/// <p/>
		/// Examples: <br/>
		/// "dogs as well as cats" &rarr;
		/// <c>mwe</c>
		/// (as, well)<br/>
		/// <c>mwe</c>
		/// (as, as)<p/>
		/// "fewer than 700 bottles" &rarr;
		/// <c>mwe</c>
		/// (fewer, than)
		/// </remarks>
		/// <seealso>
		/// 
		/// <see cref="CoordinationTransformer.MWETransform(Tree)"/>
		/// </seealso>
		/// <seealso><a href="http://universaldependencies.github.io/docs/en/dep/mwe.html">List of multi-word expressions</a></seealso>
		public static readonly GrammaticalRelation MultiWordExpression = new GrammaticalRelation(Language.UniversalEnglish, "mwe", "multi-word expression", Modifier, "MWE", tregexCompiler, "MWE < (IN|TO|RB|NP|NN|JJ|VB|CC|VBZ|VBD|ADVP|PP|JJS|RBS=target)"
			);

		/// <summary>The "determiner" grammatical relation.</summary>
		/// <remarks>
		/// The "determiner" grammatical relation.
		/// <p> <p/>
		/// Examples: <br/>
		/// "The man is here" &rarr;
		/// <c>det</c>
		/// (man,the) <br/>
		/// "Which man do you prefer?" &rarr;
		/// <c>det</c>
		/// (man,which) <br />
		/// (The ADVP match is because sometimes "a little" or "every time" is tagged
		/// as an AVDVP with POS tags straight under it.)
		/// </remarks>
		public static readonly GrammaticalRelation Determiner = new GrammaticalRelation(Language.UniversalEnglish, "det", "determiner", Modifier, "(?:WH)?NP(?:-TMP|-ADV)?|NAC|NML|NX|X|ADVP|ADJP", tregexCompiler, "/^(?:NP(?:-TMP|-ADV)?|NAC|NML|NX|X)$/ < (DT=target !< /^(?i:either|neither|both|no)$/ !$+ DT !$++ CC $++ /^(?:N[MNXP]|CD|JJ|FW|ADJP|QP|RB|PRP(?![$])|PRN)/=det !$++ (/^PRP[$]|POS/ $++ =det !$++ (/''/ $++ =det)))"
			, "NP|NP-TMP|NP-ADV < (DT=target [ (< /^(?i:either|neither|both)$/ !$+ DT !$++ CC $++ /^(?:NN|NX|NML)/ !$++ (NP < CC)) | " + "(!< /^(?i:either|neither|both|no)$/ $++ CC $++ /^(?:NN|NX|NML)/) | " + "(!< /^(?i:no)$/ $++ (/^JJ/ !$+ /^NN/) !$++CC !$+ DT) ] )"
			, "NP|NP-TMP|NP-ADV <<, PRP <- (NP|DT|RB=target <<- /^(?i:all|both|each)$/)", "WHNP < (NP $-- (WHNP=target < WDT))", "@WHNP|ADVP|ADJP < (/^(?:NP|NN|CD|RBS|JJ)/ $-- (DT|WDT|WP=target !< /^(?i:no)$/ [ ==WDT|WP | !$++ CC|CONJP ]))", "@NP < (/^(?:NP|NN|CD|RBS)/ $-- WDT|WP=target)"
			);

		/// <summary>The "predeterminer" grammatical relation.</summary>
		/// <remarks>
		/// The "predeterminer" grammatical relation.
		/// <p> <p/>
		/// Example: <br/>
		/// "All the boys are here" &rarr;
		/// <c>predet</c>
		/// (boys,all)
		/// </remarks>
		public static readonly GrammaticalRelation Predeterminer = new GrammaticalRelation(Language.UniversalEnglish, "det:predet", "predeterminer", Modifier, "(?:WH)?(?:NP|NX|NAC|NML)(?:-TMP|-ADV)?", tregexCompiler, "/^(?:(?:WH)?NP(?:-TMP|-ADV)?|NX|NAC|NML)$/ < (PDT|DT=target $+ /^(?:DT|WP\\$|PRP\\$)$/ $++ /^(?:NN|NX|NML)/ !$++ CC)"
			, "WHNP|WHNP-TMP|WHNP-ADV|NP|NP-TMP|NP-ADV < (PDT|DT=target $+ DT $++ (/^JJ/ !$+ /^NN/)) !$++ CC", "WHNP|WHNP-TMP|WHNP-ADV|NP|NP-TMP|NP-ADV < PDT=target <- DT");

		/// <summary>The "preconjunct" grammatical relation.</summary>
		/// <remarks>
		/// The "preconjunct" grammatical relation.
		/// <p/>
		/// Example: <br/>
		/// "Both the boys and the girls are here" &rarr;
		/// <c>cc:preconj</c>
		/// (boys,both)
		/// </remarks>
		public static readonly GrammaticalRelation Preconjunct = new GrammaticalRelation(Language.UniversalEnglish, "cc:preconj", "preconjunct", Modifier, "S|VP|ADJP|PP|ADVP|UCP(?:-TMP|-ADV)?|NX|NML|SBAR|NP(?:-TMP|-ADV)?", tregexCompiler, "NP|NP-TMP|NP-ADV|NX|NML < (PDT|CC|DT=target < /^(?i:either|neither|both)$/ $++ CC)"
			, "NP|NP-TMP|NP-ADV|NX|NML < (CONJP=target < (RB < /^(?i:not)$/) < (RB|JJ < /^(?i:only|merely|just)$/) $++ CC|CONJP)", "NP|NP-TMP|NP-ADV|NX|NML < (PDT|CC|DT=target < /^(?i:either|neither|both)$/ ) < (NP < CC)", "/^S|VP|ADJP|PP|ADVP|UCP(?:-TMP|-ADV)?|NX|NML|SBAR$/ < (PDT|DT|CC=target < /^(?i:either|neither|both)$/ $++ CC)"
			, "/^S|VP|ADJP|PP|ADVP|UCP(?:-TMP|-ADV)?|NX|NML|SBAR$/ < (CONJP=target < (RB < /^(?i:not)$/) < (RB|JJ < /^(?i:only|merely|just)$/) $++ CC|CONJP)");

		/// <summary>
		/// The "possession" grammatical relation between the possessum and the possessor.<p>
		/// </p>
		/// Examples: <br/>
		/// "their offices" &rarr;
		/// <c>poss</c>
		/// (offices, their)<br/>
		/// "Bill 's clothes" &rarr;
		/// <c>poss</c>
		/// (clothes, Bill)
		/// </summary>
		public static readonly GrammaticalRelation PossessionModifier = new GrammaticalRelation(Language.UniversalEnglish, "nmod:poss", "possession modifier", Modifier, "(?:WH)?(NP|ADJP|INTJ|PRN|NAC|NX|NML)(?:-.*)?", tregexCompiler, "/^(?:WH)?(?:NP|INTJ|ADJP|PRN|NAC|NX|NML)(?:-.*)?$/ < /^(?:WP\\$|PRP\\$)$/=target"
			, "/^(?:WH)?(?:NP|NML)(?:-.*)?$/ [ < (WHNP|WHNML|NP|NML=target [ < POS | < (VBZ < /^'s$/) ] ) !< (CC|CONJP $++ WHNP|WHNML|NP|NML) |  < (WHNP|WHNML|NP|NML=target < (CC|CONJP $++ WHNP|WHNML|NP|NML) < (WHNP|WHNML|NP|NML [ < POS | < (VBZ < /^'s$/) ] )) ]"
			, "/^(?:WH)?(?:NP|NML|NX)(?:-.*)?$/ < (/^NN|NP/=target $++ (POS=pos < /\'/ $++ /^NN/) !$++ (/^NN|NP/ $++ =pos))");

		/// <summary>The "prepositional modifier" grammatical relation.</summary>
		/// <remarks>
		/// The "prepositional modifier" grammatical relation.  A prepositional
		/// modifier of a verb, adjective, or noun is any prepositional phrase that serves to modify
		/// the meaning of the verb, adjective, or noun.
		/// We also generate prep modifiers of PPs to account for treebank (PP PP PP) constructions
		/// (from 1984 through 2002). <p>
		/// <p/>
		/// Examples: <br/>
		/// "I saw a cat in a hat" &rarr;
		/// <c>case</c>
		/// (hat, in) <br/>
		/// "I saw a cat with a telescope" &rarr;
		/// <c>case</c>
		/// (telescope, with) <br/>
		/// "He is responsible for meals" &rarr;
		/// <c>case</c>
		/// (meals, for)
		/// </remarks>
		public static readonly GrammaticalRelation CaseMarker = new GrammaticalRelation(Language.UniversalEnglish, "case", "case marker", Modifier, "(?:WH)?(?:PP.*|SBARQ|NP|NML|ADVP)(?:-TMP|-ADV)?", tregexCompiler, "/(?:WH)?PP(?:-TMP)?/ < (IN|TO|MWE|PCONJP|VBN|JJ=target !$+ @SBAR [!$+ @S | $+ (S <, (VP <, NN))] )"
			, "/^(?:WH)?(?:NP|NML)(?:-TMP|-ADV)?$/ < POS=target", "/^(?:WH)?(?:NP|NML)(?:-TMP|-ADV)?$/ < (VBZ=target < /^'s$/)", "/(?:WH)?PP(?:-TMP)?/ <1 CC=target <2 NP", "/(?:WH)?PP(?:-TMP)?/ <, VBG=target !< (@PP < @SBAR|S)", "@ADVP < IN=target");

		/// <summary>The "phrasal verb particle" grammatical relation.</summary>
		/// <remarks>
		/// The "phrasal verb particle" grammatical relation.  The "phrasal verb particle"
		/// relation identifies phrasal verb.<p>
		/// <p/>
		/// Example: <br/>
		/// "They shut down the station." &rarr;
		/// <c>prt</c>
		/// (shut, down)
		/// </remarks>
		public static readonly GrammaticalRelation PhrasalVerbParticle = new GrammaticalRelation(Language.UniversalEnglish, "compound:prt", "phrasal verb particle", Modifier, "VP|ADJP", tregexCompiler, "VP < PRT=target", "ADJP < /^VB/ < RP=target");

		/// <summary>The "parataxis" grammatical relation.</summary>
		/// <remarks>
		/// The "parataxis" grammatical relation. Relation between the main verb of a sentence
		/// and other sentential elements, such as a sentential parenthetical, a sentence after a ":" or a ";", when two
		/// sentences are juxtaposed next to each other without any coordinator or subordinator, etc.
		/// <p> <p/>
		/// Examples: <br/>
		/// "The guy, John said, left early in the morning." &rarr;
		/// <c>parataxis</c>
		/// (left,said) <br/>
		/// "
		/// </remarks>
		public static readonly GrammaticalRelation Parataxis = new GrammaticalRelation(Language.UniversalEnglish, "parataxis", "parataxis", Dependent, "S|VP|FRAG|NP", tregexCompiler, "VP < (PRN=target < S|SINV|SBAR)", "VP $ (PRN=target [ < S|SINV|SBAR | < VP < @NP ] )"
			, "S|FRAG|VP < (/^:$/ $+ /^S/=target) !<, (__ $++ CC|CONJP)", "@S|FRAG < (@S|SBARQ|SQ|FRAG $++ @S|SBARQ|SQ|FRAG=target !$++ @CC|CONJP|MWE !$++ (/:/ < /;/))", "@S|FRAG|VP < (/^:$/ $-- /^V/ $+ @NP=target) !< @CONJP|CC", "FRAG|NP < (NP $+ (/:/ $+ @SQ|S=target) << NNP|NNPS)"
			);

		/// <summary>The "goes with" grammatical relation.</summary>
		/// <remarks>
		/// The "goes with" grammatical relation.  This corresponds to use of the GW (goes with) part-of-speech tag
		/// in the recent Penn Treebanks. It marks partial words that should be combined with some other word. <p>
		/// <p/>
		/// Example: <br/>
		/// "They come here with out legal permission." &rarr;
		/// <c>goeswith</c>
		/// (out, with)
		/// </remarks>
		public static readonly GrammaticalRelation GoesWith = new GrammaticalRelation(Language.UniversalEnglish, "goeswith", "goes with", Modifier, ".*", tregexCompiler, "__ < GW=target");

		/// <summary>The "list" relation.</summary>
		public static readonly GrammaticalRelation List = new GrammaticalRelation(Language.UniversalEnglish, "list", "list", Dependent, "FRAG", tregexCompiler, "FRAG < (NP $+ (/,/ $+ (NP=target $+ (/,/ $+ NP))) !$++ CC|CONJP|MWE)", "FRAG < (NP $+ (/,/ $+ (NP $++ (/,/ $+ NP=target))) !$++ CC|CONJP|MWE)"
			);

		/// <summary>The quantificational modifier relation.</summary>
		/// <remarks>
		/// The quantificational modifier relation. Used in the enhanced++
		/// representation for the quanfiticational determiner in
		/// partitive and light noun constructions.
		/// <p/>
		/// Example: <br/>
		/// "Both of the planes" &rarr;
		/// <c>det:qmod</c>
		/// (planes, both)<br/>
		/// <c>mwe</c>
		/// (both, of)<br/>
		/// <c>mwe</c>
		/// (both, the)<br/>
		/// </remarks>
		public static readonly GrammaticalRelation Qmod = new GrammaticalRelation(Language.UniversalEnglish, "det:qmod", "quantificational modifier", Edu.Stanford.Nlp.Trees.UniversalEnglishGrammaticalRelations.Determiner);

		/// <summary>The "controlling nominal subject" relation.</summary>
		/// <remarks>
		/// The "controlling nominal subject" relation. Used in the enhanced and enhanced++
		/// representations between a controlled verb and its nominal controller.
		/// <p/>
		/// Example: <br/>
		/// "Sue wants to buy a hat." &rarr;
		/// <c>nsubj</c>
		/// (Sue, wants)<br/>
		/// <c>nsubj:xsubj</c>
		/// (Sue, wants)<br/>
		/// <c>mark</c>
		/// (to, buy)<br/>
		/// <c>xcomp</c>
		/// (buy, wants)<br/>
		/// <c>det</c>
		/// (a, hat)<br/>
		/// <c>dobj</c>
		/// (hat, buy)<br/>
		/// </remarks>
		public static readonly GrammaticalRelation ControllingNominalSubject = new GrammaticalRelation(Language.UniversalEnglish, "nsubj:xsubj", "controlling nominal subject", Edu.Stanford.Nlp.Trees.UniversalEnglishGrammaticalRelations.NominalSubject
			);

		/// <summary>The "controlling nominal passive subject" relation.</summary>
		/// <remarks>
		/// The "controlling nominal passive subject" relation.
		/// Used in the enhanced and enhanced++ representations between
		/// a controlled verb and its nominal controller, if the controlled
		/// verb is in passive voice.
		/// <p/>
		/// Example: <br/>
		/// "The hat seemed to have been bought." &rarr;
		/// <c>nsubj</c>
		/// (hat, seemed)<br/>
		/// <c>nsubjpass:xsubj</c>
		/// (hat, bought)<br/>
		/// <c>mark</c>
		/// (to, bought)<br/>
		/// <c>aux</c>
		/// (have, bought)<br/>
		/// <c>auxpass</c>
		/// (been, bought)<br/>
		/// </remarks>
		public static readonly GrammaticalRelation ControllingNominalPassiveSubject = new GrammaticalRelation(Language.UniversalEnglish, "nsubjpass:xsubj", "controlling nominal passive subject", Edu.Stanford.Nlp.Trees.UniversalEnglishGrammaticalRelations
			.NominalPassiveSubject);

		/// <summary>The "controlling clausal subject" relation.</summary>
		/// <remarks>
		/// The "controlling clausal subject" relation. Used in the enhanced and enhanced++
		/// representations between a controlled verb and its nominal controller.
		/// <p/>
		/// Example: <br/>
		/// "That they bought the company " &rarr;
		/// <c>nsubj</c>
		/// (hat, seemed)<br/>
		/// <c>nsubjpass:xsubj</c>
		/// (hat, bought)<br/>
		/// <c>mark</c>
		/// (to, bought)<br/>
		/// <c>aux</c>
		/// (have, bought)<br/>
		/// <c>auxpass</c>
		/// (been, bought)<br/>
		/// </remarks>
		public static readonly GrammaticalRelation ControllingClausalSubject = new GrammaticalRelation(Language.UniversalEnglish, "csubj:xsubj", "controlling clausal subject", Edu.Stanford.Nlp.Trees.UniversalEnglishGrammaticalRelations.NominalPassiveSubject
			);

		/// <summary>The "controlling clausal passive subject" relation.</summary>
		/// <remarks>
		/// The "controlling clausal passive subject" relation. Used in the enhanced and enhanced++
		/// representations between a controlled verb and its nominal controller, if the controlled verb is in passive voice.
		/// TODO: Is this a possible relation?
		/// </remarks>
		public static readonly GrammaticalRelation ControllingClausalPassiveSubject = new GrammaticalRelation(Language.UniversalEnglish, "csubjpass:xsubj", "controlling clausal passive subject", Edu.Stanford.Nlp.Trees.UniversalEnglishGrammaticalRelations
			.NominalPassiveSubject);

		/// <summary>
		/// The "semantic dependent" grammatical relation has been
		/// introduced as a supertype for the controlling subject relation.
		/// </summary>
		public static readonly GrammaticalRelation SemanticDependent = new GrammaticalRelation(Language.UniversalEnglish, "sdep", "semantic dependent", Dependent);

		/// <summary>The "agent" grammatical relation.</summary>
		/// <remarks>
		/// The "agent" grammatical relation. The agent of a passive VP
		/// is the complement introduced by "by" and doing the action.<p>
		/// <p/>
		/// Example: <br/>
		/// "The man has been killed by the police" &rarr;
		/// <c>agent</c>
		/// (killed, police)
		/// </remarks>
		public static readonly GrammaticalRelation Agent = new GrammaticalRelation(Language.UniversalEnglish, "agent", "agent", Dependent);

		/// <summary>A list of GrammaticalRelation values.</summary>
		/// <remarks>
		/// A list of GrammaticalRelation values.  New GrammaticalRelations must be
		/// added to this list (until we make this an enum!).
		/// The GR recognizers are tried in the order listed.  A taxonomic
		/// relationship trumps an ordering relationship, but otherwise, the first
		/// listed relation will appear in dependency output.  Known ordering
		/// constraints where both match include:
		/// <ul>
		/// <li>NUMERIC_MODIFIER &lt; ADJECTIVAL_MODIFIER
		/// </ul>
		/// </remarks>
		private static readonly IList<GrammaticalRelation> values = Generics.NewArrayList(Arrays.AsList(new GrammaticalRelation[] { Governor, Dependent, Predicate, AuxModifier, AuxPassiveModifier, Copula, Conjunct, Coordination, Punctuation, Argument
			, Subject, NominalSubject, NominalPassiveSubject, ClausalSubject, ClausalPassiveSubject, Complement, Object, DirectObject, IndirectObject, NominalModifier, ClausalComplement, XclausalComplement, Marker, Relative, Referent, Expletive, Modifier
			, AdvClauseModifier, TemporalModifier, RelativeClauseModifier, NumericModifier, AdjectivalModifier, CompoundModifier, NameModifier, AppositionalModifier, ClausalModifier, AdverbialModifier, NegationModifier, MultiWordExpression, Determiner, 
			Predeterminer, Preconjunct, PossessionModifier, CaseMarker, PhrasalVerbParticle, SemanticDependent, Agent, NpAdverbialModifier, Parataxis, DiscourseElement, GoesWith, List, Preposition, Qmod, ControllingNominalSubject, ControllingNominalPassiveSubject
			, ControllingClausalSubject, ControllingClausalPassiveSubject }));

		private static readonly IList<GrammaticalRelation> synchronizedValues = Java.Util.Collections.SynchronizedList(values);

		private static readonly IList<GrammaticalRelation> unmodifiableSynchronizedValues = Java.Util.Collections.UnmodifiableList(values);

		public static readonly IReadWriteLock valuesLock = new ReentrantReadWriteLock();

		public static readonly ICollection<GrammaticalRelation> clauseRelations = Java.Util.Collections.UnmodifiableSet(CollectionUtils.AsSet(Conjunct, XclausalComplement, ClausalComplement, ClausalModifier, AdvClauseModifier, RelativeClauseModifier
			, Parataxis, AppositionalModifier, List));

		public static readonly IDictionary<string, GrammaticalRelation> shortNameToGRel = new ConcurrentHashMap<string, GrammaticalRelation>();

		static UniversalEnglishGrammaticalRelations()
		{
			//todo: Things still to fix: comparatives, in order to clauses, automatic Vadas-like NP structure
			// By setting the HeadFinder to null, we find out right away at
			// runtime if we have incorrectly set the HeadFinder for the
			// dependency tregexes
			// add handling of tricky VP fronting cases...
			// add handling of tricky VP fronting cases...
			// matches (what, is) in "what is that" after the SQ has been flattened out of the tree
			// "Such a great idea this was"
			// ect seems to be a common misspelling for etc in the PTB
			// match "not", "n't", "nt" (for informal writing), or "never" as _complete_ string
			// This case is separated out from the previous case to
			// avoid conflicts with advcl when you have phrases such as
			// "but only because ..."
			// non-parenthetical or comma in suitable phrase with conj then adverb to left
			// content phrase to the right of a comma or a parenthetical
			// The test at the end is to make sure that a conjunction or
			// comma etc actually show up between the target of the conj
			// dependency and the head of the phrase.  Otherwise, a
			// different relationship is probably more appropriate.
			// Note that this test looks for one of two things: a
			// cc/conjp which does not have a , between it and the
			// target or a , which does not appear to the right of a
			// cc/conjp.  This test eliminates things such as
			// parenthetics which come after a list, such as in the
			// sentence "to see the market go down and dump everything,
			// which ..." where "go down and dump everything, which..."
			// is all in one VP node.
			// non-parenthetical or comma in suitable phrase with conjunction to left
			// non-parenthetical or comma in suitable phrase with conj then adverb to left
			// content phrase to the right of a comma or a parenthetical
			// content phrase to the left of a comma for at least NX
			// to take the conjunct in a preconjunct structure "either X or Y"
			// also catches some missing examples of etc as conj
			// transformed prepositional conjunction phrase in sentence such as
			// "Lufthansa flies from and to Serbia."
			//to get conjunctions in phrases such as "big / main" or "man / woman"
			// Allows us to match "Does it?" without matching "Who does it?"
			// This will capture incorrectly parsed trees in sentences
			// such as "What disease causes cancer" without capturing
			// correctly parsed trees such as "What do elephants eat?"
			// matches subj in SINV
			// Another SINV subj, such as "Such a great idea this was"
			//matches subj in xcomp like "He considered him a friend"
			// matches subj in relative clauses
			// second disjunct matches errors where there is no S under SBAR and otherwise does no harm
			// matches subj in relative clauses
			// matches subj in existential "there" SQ
			// matches subj in existential "there" S
			// matches (what, that) in "what is that" after the SQ has been flattened out of the tree
			// matches (what, wrong) in "what is wrong with ..." after the SQ has been flattened out of the tree
			// note that in that case "wrong" is taken as the head thanks to UniversalSemanticHeadFinder hackery
			// The !$++ matches against (what, worth) in What is UAL stock worth?
			// the (NP < EX) matches (is, WHNP) in "what dignity is there in ..."
			// the PP matches (is, WHNP) in "what is on the test"
			// The next qualification eliminates parentheticals that
			// come after the actual dobj
			// Examples such as "Rolls-Royce expects sales to remain steady"
			// This matches rare cases of misparses, such as "What
			// disease causes cancer?" where the "causes" does not get a
			// surrounding VP.  Hopefully it does so without overlapping
			// any other dependencies.
			// The rule for Wh-questions
			// cdm Jul 2010: No longer require WHNP as first child of SBARQ below: often not because of adverbials, quotes, etc., and removing restriction does no harm
			// this next pattern used to assume no empty NPs. Corrected.
			// One could require the VP at the end of the <+ to also be !< (/^(?:VB|AUX)/ $. SBAR) . This would be right for complement SBAR, but often avoids good matches for adverbial SBAR.  Adding it kills 4 good matches for avoiding 2 wrong matches on sum of TB3-train and EWT
			// matches direct object in relative clauses with relative pronoun "I saw the book that you bought". Seems okay. If this is changed, also change the pattern for "rel"
			// TODO: this can occasionally produce incorrect dependencies, such as the sentence
			// "with the way which his split-fingered fastball is behaving"
			// eg take a tree where the verb doesn't have an object
			// // matches direct object for long dependencies in relative clause without explicit relative pronouns
			// "SBAR !< (WHPP|WHNP|WHADVP) < (S < (@NP $++ (VP !< (/^(?:VB|AUX)/ < " + copularWordRegex + " !$+ VP)  !<+(VP) (/^(?:VB|AUX)/ < " + copularWordRegex + " $+ (VP < VBN|VBD)) !<+(VP) NP !< SBAR !<+(VP) (PP <- IN|TO)))) !$-- CC $-- NP > NP=target " +
			//   // avoid conflicts with rcmod.  TODO: we could look for
			//   // empty nodes in this kind of structure and use that to
			//   // find dobj, tmod, advmod, etc.  won't help the parser,
			//   // of course, but will help when converting a treebank
			//   // which contains empties
			//   // Example: "with the way his split-fingered fastball is behaving"
			//   "!($-- @NP|WHNP|NML > @NP|WHNP <: (S !< (VP < TO)))",
			// If there was an NP between the WHNP and the ADJP, we want
			// that NP to have the nsubj relation, and the WHNP is either
			// a dobj or a pobj instead.  For example, dobj(What, worth)
			// in "What is UAL stock worth?"
			// Now allow $++ in main pattern above so don't need this.
			// "SBAR !< (WHPP|WHNP|WHADVP) < (S < (@NP $+ (ADVP $+ (VP !< (/^(?:VB|AUX)/ < " + copularWordRegex + " !$+ VP) !<+(VP) (/^(?:VB|AUX)/ < " + copularWordRegex + " $+ (VP < VBN|VBD)) !<+(VP) NP !< SBAR !<+(VP) (PP <- IN|TO))))) !$-- CC $-- NP > NP=target"
			// Excluding BE doesn't allow cases of NP-PRD followed by NP-TMP or NP-LOC like "These are Europeans next door."
			// Doc said: case with an iobj before dobj as two regular NPs. (This won't match if second one is explicitly NP-TMP.) But basic case covers this case. Does nothing.
			// "VP < (NP $+ (NP|WHNP=target !< (/^NN/ < " + timeWordLotRegex + "))) !<(/^(?:VB|AUX)/ < " + copularWordRegex + ")",  // this time one also included "lot"
			// Doc said: match "give it next week".  CDM 2013: I think this was put in to handle parse errors where the 2 NPs of a ditransitive were grouped into 1. But it is in principle wrong, and including it seems to be a no-op on TB3 WSJ. So exclude for now.
			// "VP < (NP < (NP $+ (/^(NP|WHNP)$/=target !< (/^NN/ < " + timeWordLotRegex + "))))!< (/^(?:VB|AUX)/ < " + copularWordRegex + ")",  // this time one also included "lot"
			// Doc said: matches direct object in relative clauses "I saw the book that you said you bought". But it didn't seem to determine anything.
			// This was various attempts at handling a long distance dependency, but that doesn't work; now handled through rel mechanism.
			// "SBAR !< WHNP|WHADVP < (S < (@NP $++ (VP !$++ NP))) > (VP > (S < NP $- WHNP=target))",
			// "SBAR !< WHNP|WHADVP|IN < (S < @NP < (VP !< (NP !<<# " + timeWordRegex + "))) > (VP > (S < NP $- WHNP=target))",
			// "S < (@NP !< /^-NONE-$/) <+(VP) (VP !< (@NP !< /^-NONE-$/ < (/^VB/ !< " + copularWordRegex + ")) !< CONJP|CC|SBAR) > (@SBAR !< @WHNP|WHADVP $- /^VB/ >+(VP|S|SBAR) (S < (@NP !< /^-NONE-$/ !<<# " + timeWordRegex + ") $- (@WHNP=target !< /^-NONE-$/ !<# WRB)))",
			// we now don't match "VBG > PP $+ NP=target", since it seems better to CM to regard these quasi preposition uses (like "including soya") as prepositions rather than verbs with objects -- that's certainly what the phrase structure at least suggests in the PTB.  They're now matched as pobj
			// this next one was meant to fix common mistakes of our parser, but is perhaps too dangerous to keep
			// excluding selfRegex leaves out phrases such as "I cooked dinner myself"
			// excluding DT leaves out phrases such as "My dog ate it all""
			// Weird case of verbs with direct S complement that is not an infinitive or participle
			// ("I saw [him take the cake].", "making [him go crazy]")
			// the canonical case of a SBAR[that] with an overt "that" or "whether"
			// Conjoined SBAR otherwise in the canonical case
			// This finds most ccomp SBAR[that] with omission of that, but only ones without dobj
			// Find ccomp SBAR[that] after dobj for clear marker verbs
			// Direct question: She asked "Who is in trouble"
			// !$-- @SBAR|S handles cases where the answer to the question
			//   "What do they ccompVerb?"
			//   is already answered by a different node
			// the ccompObjVerbRegex/NP test distinguishes "He told me why ..."
			//   vs "They know my order when ..."
			// to find "...", he said or "...?" he asked.
			// We eliminate conflicts with conj by looking for CC
			// Matching against "!< (VP < TO|VBG|VBN)" matches against vmod
			// "!< (VP <1 (VP [ <1 VBG|VBN | <2 (VBG|VBN $-- ADVP) ])))" also matches against vmod
			// ADVP is things like "As long as they spend ..."
			// < WHNP captures phrases such as "no matter what", "no matter how", etc
			// ADJP is things like "sure (that) he'll lose" or for/to ones or object of comparison with than "than we were led to expect"; Leave aside as in "as clever as we thought.
			// That ... he know
			// JJ catches a couple of funny NPs with heads like "enough"
			// Note that we eliminate SBAR which also match an vmod pattern
			//"VP < (S=target [ !$-- NP $-- (/^V/ < " + xcompNoObjVerbRegex + ") | $-- (/^V/ < " + xcompVerbRegex + ") ] !$- (NN < order) < (VP < TO))",    // used to have !> (VP < (VB|AUX < be))
			// used to have !> (VP < (VB|AUX < be))
			// to find "help sustain ...
			// stop eating
			// note that we eliminate parentheticals and clauses that could match a vmod
			// the clause !$-- VBG eliminates matches such as "What are you wearing dancing tonight"
			// Detects xcomp(becoming, requirement) in "Hand-holding is becoming an investment banking job requirement"
			// Also, xcomp(becoming, problem) in "Why is Dave becoming a problem?"
			// The next qualification eliminates parentheticals that
			// come after the actual dobj
			// The old attr relation, used here to recover xcomp relations instead.
			// "Such a great idea this was" if "was" is the root, eg -makeCopulaHead
			//Former acomp expression
			// Rule for copular Wh-questions, e.g. "What am I good at?"
			// only allow a PP < PP one if there is not a verb, or other pattern that matches acl/advcl under it.  Else acl/advcl
			// to handle "What weapon is Apollo most proficient with?"
			//to handle "What is the esophagus used for"? or "What radio station did Paul Harvey work for?"
			// to get "rather than"
			//"S|SQ|SINV < (SBAR|SBAR-TMP=target <2 (IN|MWE !< /^(?i:that|whether)$/ !$+ (NN < order)) !$-- /^(?!CC|CONJP|``|,|INTJ|PP(-.*)?$).*$/)",
			// this one might just be better, but at any rate license one with quotation marks or a conjunction beforehand
			// the last part should probably only be @SQ, but this captures some strays at no cost
			// added the (S < (VP <TO)) part so that "I tell them how to do so" doesn't get a wrong advcl
			// note that we allow adverb phrases to come before the WHADVP, which allows for phrases such as "even when"
			// ":" indicates something that should be a parataxis
			// in cases where there are two SBARs conjoined, we're happy
			// to use the head SBAR as a candidate for this relation
			// "S|SQ < (PP=target <, RB < @S)", // caught as prep and pcomp.
			// fronted adverbial clause
			// part of former purpcl: This is fronted infinitives: "To find out why, we went to ..."
			// "VP > (VP < (VB|AUX < be)) < (S=target !$- /^,$/ < (VP < TO|VBG) !$-- NP)", // part of former purpcl [cdm 2010: this pattern was added by me in 2006, but it is just bad!]
			// // matches direct object for long dependencies in relative clause without explicit relative pronouns
			// "SBAR !< (WHPP|WHNP|WHADVP) < (S < (@NP $++ (VP !< (/^(?:VB|AUX)/ < " + copularWordRegex + " !$+ VP)  !<+(VP) (/^(?:VB|AUX)/ < " + copularWordRegex + " $+ (VP < VBN|VBD)) !<+(VP) NP !< SBAR !<+(VP) (PP <- IN|TO)))) !$-- CC $-- NP > NP=target " +
			//   // avoid conflicts with rcmod.  TODO: we could look for
			//   // empty nodes in this kind of structure and use that to
			//   // find dobj, tmod, advmod, etc.  won't help the parser,
			//   // of course, but will help when converting a treebank
			//   // which contains empties
			//   // Example: "with the way his split-fingered fastball is behaving"
			//   "!($-- @NP|WHNP|NML > @NP|WHNP <: (S !< (VP < TO)))",
			// this is for comparative or as ... as complements: sold more quickly [than they had expected]
			// available as long [as they install a crash barrier]
			//moved from vmod
			// to get "John, knowing ..., announced "
			// allowing both VP=verb and VP <1 VP=verb catches
			// conjunctions of two VP clauses
			// What are you wearing dancing tonight?
			// We could use something like this keying off -ADV annotation, but not yet operational, as we don't keep S-ADV, only NP-ADV
			// "VP < (/^S-ADV$/=target < (VP <, VBG|VBN) )",
			// they wrote asking the SEC to ...
			//"VP < (S=target < (VP < TO) !$-- (/^V/ < " + xcompNoObjVerbRegex + ") )",
			//former pcomp
			//infinitival to
			// IN above is needed for "next" in "next week" etc., which is often tagged IN.
			// Cover the case of "John, 34, works at Stanford" - similar to an expression for appos
			// $ is so phrases such as "$ 100 million buyout" get amod(buyout, $)
			// Phrases such as $ 100 million get converted from (QP ($ $) (CD 100) (CD million)) to
			// (QP ($ $) (QP (CD 100) (CD million))).  This next tregex covers those phrases.
			// Note that the earlier tregexes are usually enough to cover those phrases, such as when
			// the QP is by itself in an ADJP or NP, but sometimes it can have other siblings such
			// as in the phrase "$ 100 million or more".  In that case, this next expression is needed.
			//number relation in original SD
			// in vitro, in vivo, etc., in Genia
			// matches against "etc etc"
			/*
			* There used to be a relation "abbrev" for when abbreviations were defined in brackets after a noun
			* phrase, like "the Australian Broadcasting Corporation (ABC)", but it has now been disbanded, and
			* subsumed under appos.
			*/
			// NP-ADV is a npadvmod, NP-TMP is a tmod
			// TODO: next pattern with NNP doesn't work because leftmost NNP is deemed head in a
			// structure like (NP (NNP Norway) (, ,) (NNP Verdens_Gang) (, ,))
			// find abbreviations
			// for biomedical English, the former NNP heuristic really doesn't work, because they use NN for all chemical entities
			// while not unfoolable, this version produces less false positives and more true positives.
			// Handles cases such as "(NP (Her daughter) Jordan)"
			// Handle cases in the Web Treebank such as "Subject: ...."
			// also allow VBD since it quite often occurs in treebank errors and parse errors
			// to get "MBUSA, headquartered ..."
			// Allows an adverb to come before the participle
			//former pcomp
			// for case of relative clauses with no relativizer
			// (it doesn't distinguish whether actually gapped).
			//last term is to exclude "at least/most..."
			//quantmod relation in original SD
			//more than / less than
			// avoids adverb conjunctions matching as advmod; added JJ to catch How long
			// "!< no" so we can get neg instead for "no foo" when no is tagged as RB
			// we allow CC|CONJP as long as it is not between the target and the head
			// TODO: perhaps remove that last clause if we transform
			// more and more, less and less, etc.
			//this one gets "at least" advmod(at, least) or "fewer than" advmod(than, fewer)
			// for PP, only ones before head, or after NP, since others afterwards are pcomp
			// the commented out parts were relevant for the "det",
			// but don't seem to matter for the "neg" relation
			/* !$++ CC */
			/* =det !$++ (/^PRP[$]|POS/ $++ =det !$++ (/''/ $++ =det)) */
			// catches "no more", possibly others as well
			// !< CC|CONJP catches phrases such as "no more or less", which maybe should be preconj
			// one word nouns like "cost efficient", "ice-free"
			//up 20%, once a week, ...
			// Mr. Bush himself ..., in a couple different parse
			// patterns.  Looking for CC|CONJP leaves out phrases such
			// as "he and myself"
			// this next one is for weird financial listings: 4.7% three months
			//at least/most/...
			// CDM Jan 2010: For constructions like "during the same period last year"
			// combining expressions into a single disjunction should improve speed a little
			// matches when relative clauses as temporal modifiers of verbs!
			// "NP|NP-TMP|NP-ADV < (RB=target $++ (/^PDT$/ $+ /^NN/))", // todo: This matches nothing. Was it meant to be a PDT rule for (NP almost/RB no/DT chairs/NNS)?
			// we all, them all; various structures
			// testing against CC|CONJP avoids conflicts with preconj in
			// phrases such as "both foo and bar"
			// however, we allow WDT|WP to account for "what foo or bar" and "whatever foo or bar"
			//TODO: web_tbk/data/reviews/penntree/122270.xml.tree:
			// "both of the work.."
			// This matches weird/wrong NP-internal preconjuncts where you get (NP PDT (NP NP CC NP)) or similar
			//TODO: change some of it to nmod, ?also change pronouns?
			// todo: possessive pronoun under ADJP needs more work for one case of (ADJP his or her own)
			// basic NP possessive: we want to allow little conjunctions in head noun (NP (NP ... POS) NN CC NN) but not falsely match when there are conjoined NPs.  See tests.
			// handle a few too flat NPs
			// note that ' matches both ' and 's
			//todo: update documentation
			//"/(?:WH)?PP(?:-TMP)?/ !$- (@CC|CONJP $- __) < IN|TO|MWE=target",
			//"/(?:WH)?PP(?:-TMP)?/ < (IN|TO|MWE|PCONJP=target !$+ @SBAR|S)",
			//'s
			//'s
			//TODO: integrate the following into nmod???
			//"/^(?:(?:WH)?(?:NP|ADJP|ADVP|NX|NML)(?:-TMP|-ADV)?|VP|NAC|SQ|FRAG|PRN|X|RRC)$/ < (S=target <: WHPP|WHPP-TMP|PP|PP-TMP)",
			// only allow a PP < PP one if there is not a conj, verb, or other pattern that matches pcomp under it.  Else pcomp
			//"WHPP|WHPP-TMP|WHPP-ADV|PP|PP-TMP|PP-ADV < (WHPP|WHPP-TMP|WHPP-ADV|PP|PP-TMP|PP-ADV=target !$- IN|VBG|VBN|TO) !< @CC|CONJP",
			//"S|SINV < (PP|PP-TMP=target !< SBAR) < VP|S",
			//"SBAR|SBARQ < /^(?:WH)?PP/=target < S|SQ",
			// to handle "What weapon is Apollo most proficient with?"
			//"SBARQ < (WHNP $++ ((/^(?:VB|AUX)/ < " + copularWordRegex + ") $++ (ADJP=adj < (PP=target !< NP)) $++ (NP $++ =adj)))",
			// to handle "Nothing but their scratches"
			//"at most/at best/..."
			/*
			"/^(?:(?:WH)?(?:NP|ADJP|ADVP|NX|NML)(?:-TMP|-ADV)?|VP|NAC|SQ|FRAG|PRN|X|RRC)$/ < (WHPP|WHPP-TMP|PP|PP-TMP=target !$- (@CC|CONJP $- __)) !<- " + ETC_PAT + " !<- " + FW_ETC_PAT,
			"/^(?:(?:WH)?(?:NP|ADJP|ADVP|NX|NML)(?:-TMP|-ADV)?|VP|NAC|SQ|FRAG|PRN|X|RRC)$/ < (S=target <: WHPP|WHPP-TMP|PP|PP-TMP)",
			// only allow a PP < PP one if there is not a conj, verb, or other pattern that matches pcomp under it.  Else pcomp
			"WHPP|WHPP-TMP|WHPP-ADV|PP|PP-TMP|PP-ADV < (WHPP|WHPP-TMP|WHPP-ADV|PP|PP-TMP|PP-ADV=target !$- IN|VBG|VBN|TO) !< @CC|CONJP",
			"S|SINV < (PP|PP-TMP=target !< SBAR) < VP|S",
			"SBAR|SBARQ < /^(?:WH)?PP/=target < S|SQ",
			"@NP < (@UCP|PRN=target <# @PP)");
			*/
			// parenthetical
			// parenthetical
			// The next relation handles a colon between sentences
			// and similar punct such as --
			// Sometimes these are lists, especially in the case of ";",
			// so we don't trigger if there is a CC|CONJP that occurs
			// anywhere other than the first child
			// First child can occur in rare circumstances such as
			// "But even if he agrees -- which he won't -- etc etc"
			// two juxtaposed sentences; common in web materials (but this also matches quite a few wsj things)
			// sometimes CC cases are right node raising, etc.
			// TODO would be nice to have this set up automatically...
			// Cache frequently used views of the values list
			//Relations that can connect two clauses.
			// Map from English GrammaticalRelation short names to their corresponding
			// GrammaticalRelation objects
			ValuesLock().Lock();
			try
			{
				foreach (GrammaticalRelation gr in Values())
				{
					shortNameToGRel[gr.ToString().ToLower()] = gr;
				}
			}
			finally
			{
				ValuesLock().Unlock();
			}
		}

		public static IList<GrammaticalRelation> Values()
		{
			return unmodifiableSynchronizedValues;
		}

		public static ILock ValuesLock()
		{
			return valuesLock.ReadLock();
		}

		/// <summary>
		/// This method is meant to be called when you want to add a relation
		/// to the values list in a thread-safe manner.
		/// </summary>
		/// <remarks>
		/// This method is meant to be called when you want to add a relation
		/// to the values list in a thread-safe manner.  Currently, this method
		/// is always used in preference to values.add() because we expect to
		/// add new EnglishGrammaticalRelations very rarely, so the eased
		/// concurrency seems to outweigh the fairly slight cost of thread-safe
		/// access.
		/// </remarks>
		/// <param name="relation">the relation to be added to the values list</param>
		public static void ThreadSafeAddRelation(GrammaticalRelation relation)
		{
			valuesLock.WriteLock().Lock();
			try
			{
				// try-finally structure taken from Javadoc code sample for ReentrantReadWriteLock
				synchronizedValues.Add(relation);
				shortNameToGRel[relation.ToString()] = relation;
			}
			finally
			{
				valuesLock.WriteLock().Unlock();
			}
		}

		private static readonly IDictionary<string, GrammaticalRelation> conjs = Generics.NewConcurrentHashMap();

		// the exhaustive list of conjunction relations
		public static ICollection<GrammaticalRelation> GetConjs()
		{
			return conjs.Values;
		}

		/// <summary>The "conj" grammatical relation.</summary>
		/// <remarks>
		/// The "conj" grammatical relation. Used to enhance conjunct relations.
		/// They will be turned into conj:word, where "word" is a conjunction.
		/// </remarks>
		/// <param name="conjunctionString">The conjunction to make a GrammaticalRelation out of</param>
		/// <returns>A grammatical relation for this conjunction</returns>
		public static GrammaticalRelation GetConj(string conjunctionString)
		{
			GrammaticalRelation result = conjs[conjunctionString];
			if (result == null)
			{
				lock (conjs)
				{
					result = conjs[conjunctionString];
					if (result == null)
					{
						result = new GrammaticalRelation(Language.UniversalEnglish, "conj", "conj_collapsed", Conjunct, conjunctionString);
						conjs[conjunctionString] = result;
						ThreadSafeAddRelation(result);
					}
				}
			}
			return result;
		}

		private static readonly IDictionary<string, GrammaticalRelation> nmods = Generics.NewConcurrentHashMap();

		private static readonly IDictionary<string, GrammaticalRelation> acls = Generics.NewConcurrentHashMap();

		private static readonly IDictionary<string, GrammaticalRelation> advcls = Generics.NewConcurrentHashMap();

		// the exhaustive list of preposition relations
		public static ICollection<GrammaticalRelation> GetNmods()
		{
			return nmods.Values;
		}

		public static ICollection<GrammaticalRelation> GetAcls()
		{
			return acls.Values;
		}

		public static ICollection<GrammaticalRelation> GetAdvcls()
		{
			return advcls.Values;
		}

		/// <summary>The "nmod" grammatical relation.</summary>
		/// <remarks>
		/// The "nmod" grammatical relation. Used to add case marker information
		/// to nominal modifier relations.<p>
		/// They will be turned into nmod:word, where "word" is a preposition.
		/// </remarks>
		/// <param name="prepositionString">The preposition to make a GrammaticalRelation out of</param>
		/// <returns>A grammatical relation for this preposition</returns>
		public static GrammaticalRelation GetNmod(string prepositionString)
		{
			/* Check for nmod subtypes which are not stored in the `nmods` map. */
			if (prepositionString.Equals("npmod"))
			{
				return NpAdverbialModifier;
			}
			else
			{
				if (prepositionString.Equals("tmod"))
				{
					return TemporalModifier;
				}
				else
				{
					if (prepositionString.Equals("poss"))
					{
						return PossessionModifier;
					}
				}
			}
			GrammaticalRelation result = nmods[prepositionString];
			if (result == null)
			{
				lock (nmods)
				{
					result = nmods[prepositionString];
					if (result == null)
					{
						result = new GrammaticalRelation(Language.UniversalEnglish, "nmod", "nmod_preposition", NominalModifier, prepositionString);
						nmods[prepositionString] = result;
						ThreadSafeAddRelation(result);
					}
				}
			}
			return result;
		}

		/// <summary>The "advcl" grammatical relation.</summary>
		/// <remarks>
		/// The "advcl" grammatical relation. Used to add case marker information
		/// to adverbial clause relations.<p>
		/// They will be turned into advcl:word, where "word" is a preposition.
		/// </remarks>
		/// <param name="advclString">The preposition to make a GrammaticalRelation out of</param>
		/// <returns>A grammatical relation for this preposition</returns>
		public static GrammaticalRelation GetAdvcl(string advclString)
		{
			GrammaticalRelation result = advcls[advclString];
			if (result == null)
			{
				lock (advcls)
				{
					result = advcls[advclString];
					if (result == null)
					{
						result = new GrammaticalRelation(Language.UniversalEnglish, "advcl", "advcl_preposition", AdvClauseModifier, advclString);
						advcls[advclString] = result;
						ThreadSafeAddRelation(result);
					}
				}
			}
			return result;
		}

		/// <summary>The "acl" grammatical relation.</summary>
		/// <remarks>
		/// The "acl" grammatical relation. Used to add case marker information to
		/// adjectival clause relations.<p>
		/// They will be turned into acl:word, where "word" is a preposition.
		/// </remarks>
		/// <param name="aclString">The preposition to make a GrammaticalRelation out of</param>
		/// <returns>A grammatical relation for this preposition</returns>
		public static GrammaticalRelation GetAcl(string aclString)
		{
			/* Check for nmod subtypes which are not stored in the `nmods` map. */
			if (aclString.Equals("relcl"))
			{
				return RelativeClauseModifier;
			}
			GrammaticalRelation result = acls[aclString];
			if (result == null)
			{
				lock (acls)
				{
					result = acls[aclString];
					if (result == null)
					{
						result = new GrammaticalRelation(Language.UniversalEnglish, "acl", "acl_preposition", ClausalModifier, aclString);
						acls[aclString] = result;
						ThreadSafeAddRelation(result);
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Returns the EnglishGrammaticalRelation having the given string
		/// representation (e.g., "nsubj"), or null if no such is found.
		/// </summary>
		/// <param name="s">The short name of the GrammaticalRelation</param>
		/// <returns>The EnglishGrammaticalRelation with that name</returns>
		public static GrammaticalRelation ValueOf(string s)
		{
			return GrammaticalRelation.ValueOf(s, synchronizedValues, ValuesLock());
		}

		//    // TODO does this need to be changed?
		//    // modification NOTE: do not commit until go-ahead
		//    // If this is a collapsed relation (indicated by a "_" separating
		//    // the type and the dependent, instantiate a collapsed version.
		//    // Currently handcode against conjunctions and prepositions, but
		//    // should do this in a more robust fashion.
		//    String[] tuples = s.trim().split("_", 2);
		//    if (tuples.length == 2) {
		//      String reln = tuples[0];
		//      String specific = tuples[1];
		//      if (reln.equals(PREPOSITIONAL_MODIFIER.getShortName())) {
		//        return getPrep(specific);
		//      } else if (reln.equals(CONJUNCT.getShortName())) {
		//        return getConj(specific);
		//      }
		//    }
		//
		//    return null;
		/// <summary>Returns an EnglishGrammaticalRelation based on the argument.</summary>
		/// <remarks>
		/// Returns an EnglishGrammaticalRelation based on the argument.
		/// It works if passed a GrammaticalRelation or the String
		/// representation of one (e.g., "nsubj").  It returns
		/// <see langword="null"/>
		/// for other classes or if no string match is found.
		/// </remarks>
		/// <param name="o">A GrammaticalRelation or String</param>
		/// <returns>The EnglishGrammaticalRelation with that name</returns>
		public static GrammaticalRelation ValueOf(object o)
		{
			if (o is GrammaticalRelation)
			{
				return (GrammaticalRelation)o;
			}
			else
			{
				if (o is string)
				{
					return ValueOf((string)o);
				}
				else
				{
					return null;
				}
			}
		}

		/// <summary>Prints out the English grammatical relations hierarchy.</summary>
		/// <remarks>
		/// Prints out the English grammatical relations hierarchy.
		/// See
		/// <c>EnglishGrammaticalStructure</c>
		/// for a main method that
		/// will print the grammatical relations of a sentence or tree.
		/// </remarks>
		/// <param name="args">Args are ignored.</param>
		public static void Main(string[] args)
		{
			System.Console.Out.WriteLine(Dependent.ToPrettyString());
		}
	}
}
