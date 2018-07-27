using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.International.Spanish.Pipeline
{
	/// <summary>
	/// Provides routines for "decompressing" further the expanded trees
	/// formed by multiword token splitting.
	/// </summary>
	/// <remarks>
	/// Provides routines for "decompressing" further the expanded trees
	/// formed by multiword token splitting.
	/// Multiword token expansion leaves constituent words as siblings in a
	/// "flat" tree structure. This often represents an incorrect parse of
	/// the sentence. For example, the phrase "Ministerio de Finanzas" should
	/// not be parsed as a flat structure like
	/// (grup.nom (np00000 Ministerio) (sp000 de) (np00000 Finanzas))
	/// but rather a "deep" structure like
	/// (grup.nom (sp (prep (sp000 de))
	/// (sn (grup.nom (np0000 Finanzas)))))
	/// This class provides methods for detecting common linguistic patterns
	/// that should be expanded in this way.
	/// </remarks>
	/// <author>Jon Gauthier</author>
	public class MultiWordTreeExpander
	{
		/// <summary>Regular expression to match groups inside which we want to expand things</summary>
		private const string CandidateGroups = "(^grup\\.(adv|c[cs]|[iwz]|nom|prep|pron|verb)|\\.inter)";

		private const string Prepositions = "(por|para|pro|al?|del?|con(?:tra)?|sobre|en(?:tre)?|hacia|sin|según|hasta|bajo)";

		private readonly TregexPattern parentheticalExpression = TregexPattern.Compile("fpa=left > /^grup\\.nom$/ " + "$++ fpt=right");

		private readonly TsurgeonPattern groupParentheticalExpression = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree grup.nom.inter4 left right");

		/// <summary>Yes, some multiword tokens contain multiple clauses..</summary>
		private readonly TregexPattern multipleClauses = TregexPattern.Compile("/^grup\\.nom/ > /^grup\\.nom/ < (fp !$-- fp $- /^[^g]/=right1 $+ __=left2)" + " <, __=left1 <` __=right2");

		private readonly TsurgeonPattern expandMultipleClauses = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[createSubtree grup.nom left1 right1]" + "[createSubtree grup.nom left2 right2]");

		private readonly TregexPattern prepositionalPhrase = TregexPattern.Compile("sp000=tag < /(?iu)^" + Prepositions + "$/" + " > (/" + CandidateGroups + "/ <- __=right)" + " $+ /^([adnswz]|p[ipr])/=left !$-- sp000");

		private readonly TregexPattern leadingPrepositionalPhrase = TregexPattern.Compile("sp000=tag < /(?iu)^" + Prepositions + "$/" + " >, (/" + CandidateGroups + "/ <- __=right)" + " $+ /^([adnswz]|p[ipr])/=left !$-- sp000");

		/// <summary>
		/// First step in expanding prepositional phrases: group NP to right of
		/// preposition under a `grup.nom` subtree (specially labeled for now
		/// so that we can target it in the next step)
		/// </summary>
		private readonly TsurgeonPattern expandPrepositionalPhrase1 = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[createSubtree grup.nom.inter left right]");

		/// <summary>
		/// Matches intermediate prepositional phrase structures as produced by
		/// the first step of expansion.
		/// </summary>
		private readonly TregexPattern intermediatePrepositionalPhrase = TregexPattern.Compile("sp000=preptag $+ /^grup\\.nom\\.inter$/=gn");

		/// <summary>
		/// Second step: replace intermediate prepositional phrase structure
		/// with final result.
		/// </summary>
		private readonly TsurgeonPattern expandPrepositionalPhrase2 = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoinF (sp (prep T=preptarget) (sn foot@)) gn]" + "[relabel gn /.inter$//]" + "[replace preptarget preptag]" + "[delete preptag]"
			);

		private readonly TregexPattern prepositionalVP = TregexPattern.Compile("sp000=tag < /(?i)^(para|al?|del?)$/" + " > (/" + CandidateGroups + "/ <- __=right)" + " $+ vmn0000=left !$-- sp000");

		private readonly TsurgeonPattern expandPrepositionalVP1 = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[createSubtree S.inter left right]" + "[adjoinF (infinitiu foot@) left]");

		private readonly TregexPattern intermediatePrepositionalVP = TregexPattern.Compile("sp000=preptag $+ /^S\\.inter$/=si");

		private readonly TsurgeonPattern expandPrepositionalVP2 = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoin (sp prep=target S@) si] [move preptag >0 target]");

		private readonly TregexPattern conjunctPhrase = TregexPattern.Compile("cc=cc" + " > (/^grup\\.nom/ <, __=left1 <` __=right2)" + " $- /^[^g]/=right1 $+ /^[^g]/=left2");

		private readonly TsurgeonPattern expandConjunctPhrase = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoinF (conj foot@) cc]" + "[createSubtree grup.nom.inter2 left1 right1]" + "[createSubtree grup.nom.inter2 left2 right2]"
			);

		/// <summary>
		/// Simple intermediate conjunct: a constituent which heads a single
		/// substantive
		/// </summary>
		private readonly TregexPattern intermediateSubstantiveConjunct = TregexPattern.Compile("/grup\\.nom\\.inter2/=target <: /^[dnpw]/");

		/// <summary>Rename simple intermediate conjunct as a `grup.nom`</summary>
		private readonly TsurgeonPattern expandIntermediateSubstantiveConjunct = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel target /grup.nom/]");

		/// <summary>
		/// Simple intermediate conjunct: a constituent which heads a single
		/// adjective
		/// </summary>
		private readonly TregexPattern intermediateAdjectiveConjunct = TregexPattern.Compile("/^grup\\.nom\\.inter2$/=target <: /^a/");

		/// <summary>Rename simple intermediate adjective conjunct as a `grup.a`</summary>
		private readonly TsurgeonPattern expandIntermediateAdjectiveConjunct = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel target /grup.a/]");

		/// <summary>
		/// Match parts of an expanded conjunct which must be labeled as a noun
		/// phrase given their children.
		/// </summary>
		private readonly TregexPattern intermediateNounPhraseConjunct = TregexPattern.Compile("/^grup\\.nom\\.inter2$/=target < /^s[pn]$/");

		private readonly TsurgeonPattern expandIntermediateNounPhraseConjunct = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel target sn]");

		/// <summary>Intermediate conjunct: verb</summary>
		private readonly TregexPattern intermediateVerbConjunct = TregexPattern.Compile("/^grup\\.nom\\.inter2$/=gn <: /^vmi/");

		private readonly TsurgeonPattern expandIntermediateVerbConjunct = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoin (S (grup.verb@)) gn]");

		/// <summary>
		/// Match parts of an expanded conjunct which should be labeled as
		/// nominal groups.
		/// </summary>
		private readonly TregexPattern intermediateNominalGroupConjunct = TregexPattern.Compile("/^grup\\.nom\\.inter2$/=target !< /^[^n]/");

		private readonly TsurgeonPattern expandIntermediateNominalGroupConjunct = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel target /grup.nom/]");

		/// <summary>
		/// Match articles contained within nominal groups of substantives so
		/// that they can be moved out
		/// </summary>
		private readonly TregexPattern articleLeadingNominalGroup = TregexPattern.Compile("/^d[aip]/=art >, (/^grup\\.nom$/=ng > sn)");

		private readonly TsurgeonPattern expandArticleLeadingNominalGroup = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[insert (spec=target) $+ ng] [move art >0 target]");

		private readonly TregexPattern articleInsideOrphanedNominalGroup = TregexPattern.Compile("/^d[aip]/=d >, (/^grup\\.nom/=ng !> sn)");

		private readonly TsurgeonPattern expandArticleInsideOrphanedNominalGroup = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoinF (sn=sn spec=spec foot@) ng] [move d >0 spec]");

		private readonly TregexPattern determinerInsideNominalGroup = TregexPattern.Compile("/^d[^n]/=det >, (/^grup\\.nom/=ng > sn) $ __");

		private readonly TsurgeonPattern expandDeterminerInsideNominalGroup = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[insert (spec=target) $+ ng] [move det >0 target]");

		private readonly TregexPattern contractionTrailingIdiomBeforeNominalGroup = TregexPattern.Compile("sp000 >` (/^grup\\.prep$/ > (__ $+ /^grup\\.nom/=ng)) < /^(de|a)l$/=contraction");

		private readonly TsurgeonPattern joinArticleWithNominalGroup = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel contraction /l//] [adjoinF (sn (spec (da0000 el)) foot@) ng]");

		private readonly TregexPattern contractionInSpecifier = TregexPattern.Compile("sp000=parent < /(?i)^(a|de)l$/=contraction > spec");

		private readonly TregexPattern delTodo = TregexPattern.Compile("del=contraction . todo > sp000=parent");

		private readonly TregexPattern contractionInRangePhrase = TregexPattern.Compile("sp000 < /(?i)^(a|de)l$/=contraction >: (conj $+ (/^grup\\.(w|nom)/=group))");

		private readonly TsurgeonPattern expandContractionInRangePhrase = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel contraction /(?i)l//] [adjoinF (sn (spec (da0000 el)) foot@) group]");

		/// <summary>Operation to extract article from contraction and just put it next to the container</summary>
		private readonly TsurgeonPattern extendContraction = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel contraction /l//] [insert (da0000 el) $- parent]");

		private readonly TregexPattern terminalPrepositions = TregexPattern.Compile("sp000=sp < /" + Prepositions + "/ >- (/^grup\\.nom/ >+(/^grup\\.nom/) sn=sn >>- =sn)");

		private readonly TsurgeonPattern extractTerminalPrepositions = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[insert (prep=prep) $- sn] [move sp >0 prep]");

		/// <summary>Match terminal prepositions in prepositional phrases: "a lo largo de"</summary>
		private readonly TregexPattern terminalPrepositions2 = TregexPattern.Compile("prep=prep >` (/^grup\\.nom$/ >: (sn=sn > /^(grup\\.prep|sp)$/))");

		private readonly TsurgeonPattern extractTerminalPrepositions2 = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("move prep $- sn");

		/// <summary>Match terminal prepositions in infinitive clause within prepositional phrase: "a partir de," etc.</summary>
		private readonly TregexPattern terminalPrepositions3 = TregexPattern.Compile("sp000=sp $- infinitiu >` (S=S >` /^(grup\\.prep|sp)$/)");

		private readonly TsurgeonPattern extractTerminalPrepositions3 = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[insert (prep=prep) $- S] [move sp >0 prep]");

		private readonly TregexPattern adverbNominalGroups = TregexPattern.Compile("/^grup\\.nom./=ng <: /^r[gn]/=r");

		private readonly TsurgeonPattern replaceAdverbNominalGroup = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace ng r");

		/// <summary>Match blocks of only adjectives (one or more) with a nominal group parent.</summary>
		/// <remarks>
		/// Match blocks of only adjectives (one or more) with a nominal group parent. These constituents should be rewritten
		/// beneath an adjectival group constituent.
		/// </remarks>
		private readonly TregexPattern adjectiveSpanInNominalGroup = TregexPattern.Compile("/^grup\\.nom/=ng <, aq0000=left <` aq0000=right !< /^[^a]/");

		/// <summary>Match dependent clauses mistakenly held under nominal groups ("lo que X")</summary>
		private readonly TregexPattern clauseInNominalGroup = TregexPattern.Compile("lo . (que > (pr000000=pr >, /^grup\\.nom/=ng $+ (/^v/=vb >` =ng)))");

		private readonly TsurgeonPattern labelClause = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel ng S] [adjoinF (relatiu foot@) pr] [adjoinF (grup.verb foot@) vb]");

		/// <summary>Infinitive clause mistakenly held under nominal group</summary>
		private readonly TregexPattern clauseInNominalGroup2 = TregexPattern.Compile("/^grup\\.nom/=gn $- spec <: /^vmn/");

		private readonly TsurgeonPattern labelClause2 = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoin (S (infinitiu@)) gn]");

		private readonly TregexPattern clauseInNominalGroup3 = TregexPattern.Compile("sn=sn <, (/^vmn/=inf $+ (sp >` =sn))");

		private readonly TsurgeonPattern labelClause3 = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel sn S] [adjoinF (infinitiu foot@) inf]");

		private readonly TregexPattern loneAdjectiveInNominalGroup = TregexPattern.Compile("/^a/=a > /^grup\\.nom/ $ /^([snwz]|p[ipr])/ !$ /^a/");

		private readonly TsurgeonPattern labelAdjective = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoinF (s.a (grup.a foot@)) a]");

		private readonly TsurgeonPattern groupAdjectives = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree (s.a grup.a@) left right");

		/// <summary>Some brute-force fixes:</summary>
		private readonly TregexPattern alMenos = TregexPattern.Compile("/(?i)^al$/ . /(?i)^menos$/ > (sp000 $+ rg > /^grup\\.adv$/=ga)");

		private readonly TsurgeonPattern fixAlMenos = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace ga (grup.adv (sp (prep (sp000 a)) (sn (spec (da0000 lo)) (grup.nom (s.a (grup.a (aq0000 menos)))))))");

		private readonly TregexPattern todoLoContrario = TregexPattern.Compile("(__=ttodo < /(?i)^todo$/) $+ (__=tlo < /(?i)^lo$/ $+ (__=tcon < /(?i)^contrario$/))");

		private readonly TsurgeonPattern fixTodoLoContrario = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoin (sn (grup.nom (pp000000@))) tlo] [adjoin (grup.a (aq0000@)) tcon]");

		/// <summary>Mark infinitives within verb groups ("hacer ver", etc.)</summary>
		private readonly TregexPattern infinitiveInVerbGroup = TregexPattern.Compile("/^grup\\.verb$/=grup < (/^v/ !$-- /^v/ $++ (/^vmn/=target !$++ /^vmn/))");

		private readonly TsurgeonPattern markInfinitive = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoinF (infinitiu foot@) target]");

		/// <summary>
		/// The corpus marks entire multiword verb tokens like "teniendo en
		/// cuenta" as gerunds / infinitives (by heading them with a
		/// constituent "gerundi" / "infinitiu").
		/// </summary>
		/// <remarks>
		/// The corpus marks entire multiword verb tokens like "teniendo en
		/// cuenta" as gerunds / infinitives (by heading them with a
		/// constituent "gerundi" / "infinitiu"). Now that we've split into
		/// separate words, transfer this gerund designation so that it heads
		/// the verb only.
		/// </remarks>
		private readonly TregexPattern floppedGerund = TregexPattern.Compile("/^grup\\.verb$/=grup >: gerundi=ger < (/^vmg/=vb !$ /^vmg/)");

		private readonly TsurgeonPattern unflopFloppedGerund = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoinF (gerundi foot@) vb] [replace ger grup]");

		private readonly TregexPattern floppedInfinitive = TregexPattern.Compile("/^grup\\.verb$/=grup >: infinitiu=inf < (/^vmn/=vb !$ /^vmn/)");

		private readonly TsurgeonPattern unflopFloppedInfinitive = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[adjoinF (infinitiu foot@) vb] [replace inf grup]");

		/// <summary>Match `sn` constituents which can (should) be rewritten as nominal groups</summary>
		private readonly TregexPattern nominalGroupSubstantives = TregexPattern.Compile("sn=target < /^[adnwz]/ !< /^([^adnswz]|neg)/");

		private readonly TregexPattern leftoverIntermediates = TregexPattern.Compile("/^grup\\.nom\\.inter/=target");

		private readonly TsurgeonPattern makeNominalGroup = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel target /grup.nom/]");

		private readonly TregexPattern redundantNominalRewrite = TregexPattern.Compile("/^grup\\.nom$/ <: sn=child >: sn=parent");

		private readonly TsurgeonPattern fixRedundantNominalRewrite = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[replace parent child]");

		private readonly TregexPattern redundantPrepositionGroupRewrite = TregexPattern.Compile("/^grup\\.prep$/=parent <: sp=child >: prep");

		private readonly TsurgeonPattern fixRedundantPrepositionGroupRewrite = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel child /grup.prep/] [replace parent child]");

		private readonly TregexPattern redundantPrepositionGroupRewrite2 = TregexPattern.Compile("/^grup\\.prep$/=gp <: sp=sp");

		private readonly TsurgeonPattern fixRedundantPrepositionGroupRewrite2 = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("replace gp sp");

		/// <summary>
		/// Patterns in this list turn flat structures into intermediate forms
		/// which will eventually become deep phrase structures.
		/// </summary>
		private readonly IList<Pair<TregexPattern, TsurgeonPattern>> firstStepExpansions;

		/// <summary>
		/// Patterns in this list clean up "intermediate" phrase structures
		/// produced by previous step and produce something from them that
		/// looks like the rest of the corpus.
		/// </summary>
		private readonly IList<Pair<TregexPattern, TsurgeonPattern>> intermediateExpansions;

		/// <summary>
		/// Patterns in this list perform last-minute cleanup of leftover
		/// grammar mistakes which this class created.
		/// </summary>
		private readonly IList<Pair<TregexPattern, TsurgeonPattern>> finalCleanup;

		// Nested nominal group containing period punctuation
		// Match boundaries for subtrees created
		// Match candidate preposition
		// Headed by a group that was generated from
		// multi-word token expansion and that we
		// wish to expand further
		// With an NP on the left (-> this is a
		// prep. phrase) and not preceded by any
		// other prepositions
		// Match candidate preposition
		// Which is the first child in a group that
		// was generated from multi-word token
		// expansion and that we wish to expand
		// further
		// With an NP on the left (-> this is a
		// prep. phrase) and not preceded by any
		// other prepositions
		// In one of our expanded phrases (match
		// bounds of this expanded phrase; these form
		// the left edge of first new subtree and the
		// right edge of the second new subtree)
		// Fetch more bounds: node to immediate left
		// of cc is the right edge of the first new
		// subtree, and node to right of cc is the
		// left edge of the second new subtree
		//
		// NB: left1 may the same as right1; likewise
		// for the second tree
		// "en opinion del X," "además del Y"
		// -> "(en opinion de) (el X)," "(además de) (el Y)"
		// "del X al Y"
		// ---------
		// Final cleanup operations
		// Should be first-ish
		// Should not happen until the last moment! The function words
		// being targeted have weaker "scope" than others earlier
		// targeted, and so we don't want to clump things around them
		// until we know we have the right to clump
		// Verb phrase-related cleanup.. order is important!
		// Fixes for specific common phrases
		// Lastly..
		//
		// These final fixes are not at all linguistically motivated -- just need to make the trees less dirty
		/// <summary>
		/// Recognize candidate patterns for expansion in the given tree and
		/// perform the expansions.
		/// </summary>
		/// <remarks>
		/// Recognize candidate patterns for expansion in the given tree and
		/// perform the expansions. See the class documentation for more
		/// information.
		/// </remarks>
		public virtual Tree ExpandPhrases(Tree t, TreeNormalizer tn, ITreeFactory tf)
		{
			// Keep running this sequence of patterns until no changes are
			// affected. We need this for nested expressions like "para tratar
			// de regresar al empleo." This first step produces lots of
			// "intermediate" tree structures which need to be cleaned up later.
			Tree oldTree;
			do
			{
				oldTree = t.DeepCopy();
				t = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(firstStepExpansions, t);
			}
			while (!t.Equals(oldTree));
			// Now clean up intermediate tree structures
			t = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(intermediateExpansions, t);
			// Normalize first to allow for contraction expansion, etc.
			t = tn.NormalizeWholeTree(t, tf);
			// Final cleanup
			t = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(finalCleanup, t);
			return t;
		}

		public MultiWordTreeExpander()
		{
			firstStepExpansions = Arrays.AsList(new Pair<TregexPattern, TsurgeonPattern>(parentheticalExpression, groupParentheticalExpression), new Pair<TregexPattern, TsurgeonPattern>(multipleClauses, expandMultipleClauses), new Pair<TregexPattern, TsurgeonPattern
				>(leadingPrepositionalPhrase, expandPrepositionalPhrase1), new Pair<TregexPattern, TsurgeonPattern>(conjunctPhrase, expandConjunctPhrase), new Pair<TregexPattern, TsurgeonPattern>(prepositionalPhrase, expandPrepositionalPhrase1), new Pair<TregexPattern
				, TsurgeonPattern>(prepositionalVP, expandPrepositionalVP1), new Pair<TregexPattern, TsurgeonPattern>(contractionTrailingIdiomBeforeNominalGroup, joinArticleWithNominalGroup), new Pair<TregexPattern, TsurgeonPattern>(contractionInSpecifier, 
				extendContraction), new Pair<TregexPattern, TsurgeonPattern>(delTodo, extendContraction), new Pair<TregexPattern, TsurgeonPattern>(contractionInRangePhrase, expandContractionInRangePhrase), new Pair<TregexPattern, TsurgeonPattern>(articleLeadingNominalGroup
				, expandArticleLeadingNominalGroup), new Pair<TregexPattern, TsurgeonPattern>(articleInsideOrphanedNominalGroup, expandArticleInsideOrphanedNominalGroup), new Pair<TregexPattern, TsurgeonPattern>(determinerInsideNominalGroup, expandDeterminerInsideNominalGroup
				));
			intermediateExpansions = Arrays.AsList(new Pair<TregexPattern, TsurgeonPattern>(intermediatePrepositionalPhrase, expandPrepositionalPhrase2), new Pair<TregexPattern, TsurgeonPattern>(intermediatePrepositionalVP, expandPrepositionalVP2), new 
				Pair<TregexPattern, TsurgeonPattern>(intermediateSubstantiveConjunct, expandIntermediateSubstantiveConjunct), new Pair<TregexPattern, TsurgeonPattern>(intermediateAdjectiveConjunct, expandIntermediateAdjectiveConjunct), new Pair<TregexPattern
				, TsurgeonPattern>(intermediateNounPhraseConjunct, expandIntermediateNounPhraseConjunct), new Pair<TregexPattern, TsurgeonPattern>(intermediateVerbConjunct, expandIntermediateVerbConjunct), new Pair<TregexPattern, TsurgeonPattern>(intermediateNominalGroupConjunct
				, expandIntermediateNominalGroupConjunct));
			finalCleanup = Arrays.AsList(new Pair<TregexPattern, TsurgeonPattern>(terminalPrepositions, extractTerminalPrepositions), new Pair<TregexPattern, TsurgeonPattern>(terminalPrepositions2, extractTerminalPrepositions2), new Pair<TregexPattern, 
				TsurgeonPattern>(terminalPrepositions3, extractTerminalPrepositions3), new Pair<TregexPattern, TsurgeonPattern>(nominalGroupSubstantives, makeNominalGroup), new Pair<TregexPattern, TsurgeonPattern>(adverbNominalGroups, replaceAdverbNominalGroup
				), new Pair<TregexPattern, TsurgeonPattern>(adjectiveSpanInNominalGroup, groupAdjectives), new Pair<TregexPattern, TsurgeonPattern>(clauseInNominalGroup, labelClause), new Pair<TregexPattern, TsurgeonPattern>(clauseInNominalGroup2, labelClause2
				), new Pair<TregexPattern, TsurgeonPattern>(clauseInNominalGroup3, labelClause3), new Pair<TregexPattern, TsurgeonPattern>(loneAdjectiveInNominalGroup, labelAdjective), new Pair<TregexPattern, TsurgeonPattern>(infinitiveInVerbGroup, markInfinitive
				), new Pair<TregexPattern, TsurgeonPattern>(floppedGerund, unflopFloppedGerund), new Pair<TregexPattern, TsurgeonPattern>(floppedInfinitive, unflopFloppedInfinitive), new Pair<TregexPattern, TsurgeonPattern>(alMenos, fixAlMenos), new Pair<TregexPattern
				, TsurgeonPattern>(todoLoContrario, fixTodoLoContrario), new Pair<TregexPattern, TsurgeonPattern>(redundantNominalRewrite, fixRedundantNominalRewrite), new Pair<TregexPattern, TsurgeonPattern>(redundantPrepositionGroupRewrite, fixRedundantPrepositionGroupRewrite
				), new Pair<TregexPattern, TsurgeonPattern>(redundantPrepositionGroupRewrite2, fixRedundantPrepositionGroupRewrite2), new Pair<TregexPattern, TsurgeonPattern>(leftoverIntermediates, makeNominalGroup));
		}
	}
}
