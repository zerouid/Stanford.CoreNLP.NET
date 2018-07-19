using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Graph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Trees.UD;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A GrammaticalStructure for Universal Dependencies English.</summary>
	/// <remarks>
	/// A GrammaticalStructure for Universal Dependencies English.
	/// <p/>
	/// For feeding Stanford parser trees into this class, the Stanford parser should be run with the
	/// "-retainNPTmpSubcategories" option for best results!
	/// </remarks>
	/// <author>Bill MacCartney</author>
	/// <author>Marie-Catherine de Marneffe</author>
	/// <author>Christopher Manning</author>
	/// <author>
	/// Daniel Cer (CoNLLX format and alternative user selected dependency
	/// printer/reader interface)
	/// </author>
	/// <author>John Bauer</author>
	/// <author>Sebastian Schuster</author>
	[System.Serializable]
	public class UniversalEnglishGrammaticalStructure : GrammaticalStructure
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.UniversalEnglishGrammaticalStructure));

		private const long serialVersionUID = 1L;

		private static readonly bool Debug = Runtime.GetProperty("UniversalEnglishGrammaticalStructure", null) != null;

		private static readonly bool UseName = Runtime.GetProperty("UDUseNameRelation") != null;

		public static readonly EnhancementOptions EnhancedOptions = new EnhancementOptions(false, true, false, true, true, true, false, false, true);

		public static readonly EnhancementOptions EnhancedPlusPlusOptions = new EnhancementOptions(true, true, false, true, true, true, true, true, true);

		[Obsolete]
		public static readonly EnhancementOptions CollapsedOptions = new EnhancementOptions(true, true, true, true, false, false, true, false, false);

		/// <summary>
		/// Construct a new
		/// <c>EnglishGrammaticalStructure</c>
		/// from an existing parse
		/// tree. The new
		/// <c>GrammaticalStructure</c>
		/// has the same tree structure
		/// and label values as the given tree (but no shared storage). As part of
		/// construction, the parse tree is analyzed using definitions from
		/// <see cref="GrammaticalRelation"/>
		/// 
		/// <c>GrammaticalRelation</c>
		/// } to populate
		/// the new
		/// <c>GrammaticalStructure</c>
		/// with as many labeled grammatical
		/// relations as it can.
		/// </summary>
		/// <param name="t">Parse tree to make grammatical structure from</param>
		public UniversalEnglishGrammaticalStructure(Tree t)
			: this(t, new PennTreebankLanguagePack().PunctuationWordRejectFilter())
		{
		}

		/// <summary>This gets used by GrammaticalStructureFactory (by reflection).</summary>
		/// <remarks>This gets used by GrammaticalStructureFactory (by reflection). DON'T DELETE.</remarks>
		/// <param name="t">Parse tree to make grammatical structure from</param>
		/// <param name="tagFilter">Filter to remove punctuation dependencies</param>
		public UniversalEnglishGrammaticalStructure(Tree t, IPredicate<string> tagFilter)
			: this(t, tagFilter, new UniversalSemanticHeadFinder(true))
		{
		}

		/// <summary>
		/// Construct a new
		/// <c>GrammaticalStructure</c>
		/// from an existing parse
		/// tree. The new
		/// <c>GrammaticalStructure</c>
		/// has the same tree structure
		/// and label values as the given tree (but no shared storage). As part of
		/// construction, the parse tree is analyzed using definitions from
		/// <see cref="GrammaticalRelation"/>
		/// 
		/// <c>GrammaticalRelation</c>
		/// } to populate
		/// the new
		/// <c>GrammaticalStructure</c>
		/// with as many labeled grammatical
		/// relations as it can.
		/// This gets used by GrammaticalStructureFactory (by reflection). DON'T DELETE.
		/// </summary>
		/// <param name="t">Parse tree to make grammatical structure from</param>
		/// <param name="tagFilter">Filter for punctuation tags</param>
		/// <param name="hf">HeadFinder to use when building it</param>
		public UniversalEnglishGrammaticalStructure(Tree t, IPredicate<string> tagFilter, IHeadFinder hf)
			: base(t, UniversalEnglishGrammaticalRelations.Values(), UniversalEnglishGrammaticalRelations.ValuesLock(), new CoordinationTransformer(hf, true), hf, Filters.AcceptFilter(), tagFilter)
		{
		}

		/// <summary>Used for postprocessing CoNLL X dependencies</summary>
		public UniversalEnglishGrammaticalStructure(IList<TypedDependency> projectiveDependencies, TreeGraphNode root)
			: base(projectiveDependencies, root)
		{
		}

		/*
		* Options for "Enhanced" representation:
		*
		* - Process multi-word prepositions: No
		* - Add prepositions to relation labels: Yes
		* - Add prepositions only to nmod relations: No
		* - Add coordinating conjunctions to relation labels: Yes
		* - Propagate dependents: Yes
		* - Add "referent" relations: Yes
		* - Add copy nodes for conjoined Ps and PPs: No
		* - Turn quantificational modifiers into flat MWEs: No
		* - Add relations between controlling subject and controlled verbs: Yes
		*
		*/
		/*
		* Options for "Enhanced++" representation:
		*
		* - Process multi-word prepositions: Yes
		* - Add prepositions to relation labels: Yes
		* - Add prepositions only to nmod relations: No
		* - Add coordinating conjunctions to relation labels: Yes
		* - Propagate dependents: Yes
		* - Add "referent" relations: Yes
		* - Add copy nodes for conjoined Ps and PPs: Yes
		* - Turn quantificational modifiers into flat MWEs: Yes
		* - Add relations between controlling subject and controlled verbs: Yes
		*
		*/
		/*
		* Options for "Collapsed" representation.
		* This representation is similar to the "collapsed" SD representation
		* without any "Extra" relations.
		*
		* - Process multi-word prepositions: Yes
		* - Add prepositions to relation labels: Yes
		* - Add prepositions only to nmod relations: Yes
		* - Add coordinating conjunctions to relation labels: Yes
		* - Propagate dependents: No
		* - Add "referent" relations: No
		* - Add copy nodes for conjoined Ps and PPs: Yes
		* - Turn quantificational modifiers into flat MWEs: No
		* - Add relations between controlling subject and controlled verbs: No
		*
		*/
		// the tree is normalized (for index and functional tag stripping) inside CoordinationTransformer
		/// <summary>
		/// Returns a Filter which checks dependencies for usefulness as
		/// extra tree-based dependencies.
		/// </summary>
		/// <remarks>
		/// Returns a Filter which checks dependencies for usefulness as
		/// extra tree-based dependencies.  By default, everything is
		/// accepted.  One example of how this can be useful is in the
		/// English dependencies, where the REL dependency is used as an
		/// intermediate and we do not want this to be added when we make a
		/// second pass over the trees for missing dependencies.
		/// </remarks>
		protected internal override IPredicate<TypedDependency> ExtraTreeDepFilter()
		{
			return extraTreeDepFilter;
		}

		[System.Serializable]
		private class ExtraTreeDepFilter : IPredicate<TypedDependency>
		{
			public virtual bool Test(TypedDependency d)
			{
				return d != null && d.Reln() != Relative && d.Reln() != Preposition;
			}

			private const long serialVersionUID = 1L;
		}

		private static readonly IPredicate<TypedDependency> extraTreeDepFilter = new UniversalEnglishGrammaticalStructure.ExtraTreeDepFilter();

		protected internal override void GetTreeDeps(IList<TypedDependency> deps, DirectedMultiGraph<TreeGraphNode, GrammaticalRelation> completeGraph, IPredicate<TypedDependency> puncTypedDepFilter, IPredicate<TypedDependency> extraTreeDepFilter)
		{
		}

		//Do nothing
		protected internal override void CorrectDependencies(IList<TypedDependency> list)
		{
			SemanticGraph sg = new SemanticGraph(list);
			CorrectDependencies(sg);
			list.Clear();
			Sharpen.Collections.AddAll(list, sg.TypedDependencies());
			list.Sort();
		}

		protected internal static void CorrectDependencies(SemanticGraph sg)
		{
			if (Debug)
			{
				PrintListSorted("At correctDependencies:", sg.TypedDependencies());
			}
			CorrectSubjPass(sg);
			if (Debug)
			{
				PrintListSorted("After correctSubjPass:", sg.TypedDependencies());
			}
			ProcessNames(sg);
			if (Debug)
			{
				PrintListSorted("After processNames:", sg.TypedDependencies());
			}
			RemoveExactDuplicates(sg);
			if (Debug)
			{
				PrintListSorted("After removeExactDuplicates:", sg.TypedDependencies());
			}
		}

		private static void PrintListSorted(string title, ICollection<TypedDependency> list)
		{
			IList<TypedDependency> lis = new List<TypedDependency>(list);
			lis.Sort();
			if (title != null)
			{
				log.Info(title);
			}
			log.Info(lis);
		}

		protected internal override void PostProcessDependencies(IList<TypedDependency> list)
		{
			SemanticGraph sg = new SemanticGraph(list);
			PostProcessDependencies(sg);
			list.Clear();
			Sharpen.Collections.AddAll(list, sg.TypedDependencies());
		}

		protected internal static void PostProcessDependencies(SemanticGraph sg)
		{
			if (Debug)
			{
				PrintListSorted("At postProcessDependencies:", sg.TypedDependencies());
			}
			CorrectWHAttachment(sg);
			if (Debug)
			{
				PrintListSorted("After correcting WH attachment:", sg.TypedDependencies());
			}
			ConvertRel(sg);
			if (Debug)
			{
				PrintListSorted("After converting rel:", sg.TypedDependencies());
			}
		}

		protected internal override void GetExtras(IList<TypedDependency> list)
		{
			SemanticGraph sg = new SemanticGraph(list);
			AddRef(sg);
			if (Debug)
			{
				PrintListSorted("After adding ref:", sg.TypedDependencies());
			}
			AddExtraNSubj(sg);
			if (Debug)
			{
				PrintListSorted("After adding extra nsubj:", sg.TypedDependencies());
			}
			list.Clear();
			Sharpen.Collections.AddAll(list, sg.TypedDependencies());
		}

		private static SemgrexPattern PassiveAgentPattern = SemgrexPattern.Compile("{}=gov >nmod=reln ({}=mod >case {word:/^(?i:by)$/}=c1) >auxpass {}");

		private static SemgrexPattern[] PrepMw3Patterns = new SemgrexPattern[] { SemgrexPattern.Compile("{}=gov   [>/^nmod$/=reln ({}=mod >case ({}=c1 >mwe {}=c2 >mwe ({}=c3 !== {}=c2) ))]"), SemgrexPattern.Compile("{}=gov   [>/^(advcl|acl)$/=reln ({}=mod >/^(mark|case)$/ ({}=c1 >mwe {}=c2 >mwe ({}=c3 !== {}=c2) ))]"
			) };

		private static SemgrexPattern[] PrepMw2Patterns = new SemgrexPattern[] { SemgrexPattern.Compile("{}=gov >/^nmod$/=reln ({}=mod >case ({}=c1 >mwe {}=c2))"), SemgrexPattern.Compile("{}=gov >/^(advcl|acl)$/=reln ({}=mod >/^(mark|case)$/ ({}=c1 >mwe {}=c2))"
			) };

		private static SemgrexPattern[] PrepPatterns = new SemgrexPattern[] { SemgrexPattern.Compile("{}=gov   >/^nmod$/=reln ({}=mod >case {}=c1)"), SemgrexPattern.Compile("{}=gov   >/^(advcl|acl)$/=reln ({}=mod >/^(mark|case)$/ {}=c1)") };

		/* Semgrex patterns for prepositional phrases. */
		/// <summary>
		/// Adds the case marker(s) to all nmod, acl and advcl relations that are
		/// modified by one or more case markers(s).
		/// </summary>
		/// <param name="enhanceOnlyNmods">
		/// If this is set to true, then prepositions will only be appended to nmod
		/// relations (and not to acl or advcl) relations.
		/// </param>
		/// <seealso cref="AddCaseMarkersToReln(Edu.Stanford.Nlp.Semgraph.SemanticGraph, Edu.Stanford.Nlp.Ling.IndexedWord, Edu.Stanford.Nlp.Ling.IndexedWord, System.Collections.Generic.IList{E})"/>
		private static void AddCaseMarkerInformation(SemanticGraph sg, bool enhanceOnlyNmods)
		{
			/* Semgrexes require a graph with a root. */
			if (sg.GetRoots().IsEmpty())
			{
				return;
			}
			/* passive agent */
			SemanticGraph sgCopy = sg.MakeSoftCopy();
			SemgrexMatcher matcher = PassiveAgentPattern.Matcher(sgCopy);
			while (matcher.Find())
			{
				IndexedWord caseMarker = matcher.GetNode("c1");
				IndexedWord gov = matcher.GetNode("gov");
				IndexedWord mod = matcher.GetNode("mod");
				AddPassiveAgentToReln(sg, gov, mod, caseMarker);
			}
			IList<IndexedWord> oldCaseMarkers = Generics.NewArrayList();
			/* 3-word prepositions */
			foreach (SemgrexPattern p in PrepMw3Patterns)
			{
				sgCopy = sg.MakeSoftCopy();
				matcher = p.Matcher(sgCopy);
				while (matcher.Find())
				{
					if (enhanceOnlyNmods && !matcher.GetRelnString("reln").Equals("nmod"))
					{
						continue;
					}
					IList<IndexedWord> caseMarkers = Generics.NewArrayList(3);
					caseMarkers.Add(matcher.GetNode("c1"));
					caseMarkers.Add(matcher.GetNode("c2"));
					caseMarkers.Add(matcher.GetNode("c3"));
					caseMarkers.Sort();
					/* We only want to match every case marker once. */
					if (caseMarkers.Equals(oldCaseMarkers))
					{
						continue;
					}
					IndexedWord gov = matcher.GetNode("gov");
					IndexedWord mod = matcher.GetNode("mod");
					AddCaseMarkersToReln(sg, gov, mod, caseMarkers);
					oldCaseMarkers = caseMarkers;
				}
			}
			/* 2-word prepositions */
			foreach (SemgrexPattern p_1 in PrepMw2Patterns)
			{
				sgCopy = sg.MakeSoftCopy();
				matcher = p_1.Matcher(sgCopy);
				while (matcher.Find())
				{
					if (enhanceOnlyNmods && !matcher.GetRelnString("reln").Equals("nmod"))
					{
						continue;
					}
					IList<IndexedWord> caseMarkers = Generics.NewArrayList(2);
					caseMarkers.Add(matcher.GetNode("c1"));
					caseMarkers.Add(matcher.GetNode("c2"));
					caseMarkers.Sort();
					/* We only want to match every case marker once. */
					if (caseMarkers.Equals(oldCaseMarkers))
					{
						continue;
					}
					IndexedWord gov = matcher.GetNode("gov");
					IndexedWord mod = matcher.GetNode("mod");
					AddCaseMarkersToReln(sg, gov, mod, caseMarkers);
					oldCaseMarkers = caseMarkers;
				}
			}
			/* Single-word prepositions */
			foreach (SemgrexPattern p_2 in PrepPatterns)
			{
				sgCopy = sg.MakeSoftCopy();
				matcher = p_2.Matcher(sgCopy);
				while (matcher.Find())
				{
					if (enhanceOnlyNmods && !matcher.GetRelnString("reln").Equals("nmod"))
					{
						continue;
					}
					IList<IndexedWord> caseMarkers = Generics.NewArrayList(1);
					caseMarkers.Add(matcher.GetNode("c1"));
					if (caseMarkers.Equals(oldCaseMarkers))
					{
						continue;
					}
					IndexedWord gov = matcher.GetNode("gov");
					IndexedWord mod = matcher.GetNode("mod");
					AddCaseMarkersToReln(sg, gov, mod, caseMarkers);
					oldCaseMarkers = caseMarkers;
				}
			}
		}

		private static void AddPassiveAgentToReln(SemanticGraph sg, IndexedWord gov, IndexedWord mod, IndexedWord caseMarker)
		{
			SemanticGraphEdge edge = sg.GetEdge(gov, mod);
			GrammaticalRelation reln = UniversalEnglishGrammaticalRelations.GetNmod("agent");
			edge.SetRelation(reln);
		}

		/// <summary>Appends case marker information to nmod/acl/advcl relations.</summary>
		/// <remarks>
		/// Appends case marker information to nmod/acl/advcl relations.
		/// E.g. if there is a relation
		/// <c>nmod(gov, dep)</c>
		/// and
		/// <c>case(dep, prep)</c>
		/// , then
		/// the
		/// <c>nmod</c>
		/// relation is renamed to
		/// <c>nmod:prep</c>
		/// .
		/// </remarks>
		/// <param name="sg">semantic graph</param>
		/// <param name="gov">governor of the nmod/acl/advcl relation</param>
		/// <param name="mod">modifier of the nmod/acl/advcl relation</param>
		/// <param name="caseMarkers">
		/// 
		/// <c>List&lt;IndexedWord&gt;</c>
		/// of all the case markers that depend on mod
		/// </param>
		private static void AddCaseMarkersToReln(SemanticGraph sg, IndexedWord gov, IndexedWord mod, IList<IndexedWord> caseMarkers)
		{
			SemanticGraphEdge edge = sg.GetEdge(gov, mod);
			int lastCaseMarkerIndex = 0;
			StringBuilder sb = new StringBuilder();
			bool firstWord = true;
			foreach (IndexedWord cm in caseMarkers)
			{
				/* check for adjacency */
				if (lastCaseMarkerIndex == 0 || cm.Index() == (lastCaseMarkerIndex + 1))
				{
					if (!firstWord)
					{
						sb.Append('_');
					}
					sb.Append(cm.Value());
					firstWord = false;
				}
				else
				{
					/* Should never happen as there should be never two non-adjacent case markers.
					* If it does happen nevertheless create an additional relation.
					*/
					GrammaticalRelation reln = GetCaseMarkedRelation(edge.GetRelation(), sb.ToString().ToLower());
					sg.AddEdge(gov, mod, reln, double.NegativeInfinity, true);
					sb = new StringBuilder(cm.Value());
					firstWord = true;
				}
				lastCaseMarkerIndex = cm.Index();
			}
			GrammaticalRelation reln_1 = GetCaseMarkedRelation(edge.GetRelation(), sb.ToString().ToLower());
			edge.SetRelation(reln_1);
		}

		private static readonly SemgrexPattern PrepConjpPattern = SemgrexPattern.Compile("{} >case ({}=gov >cc {}=cc >conj {}=conj)");

		/// <summary>
		/// Expands prepositions with conjunctions such as in the sentence
		/// "Bill flies to and from Serbia." by copying the verb resulting
		/// in the following relations:
		/// <p/>
		/// <c>conj:and(flies, flies')</c>
		/// <br/>
		/// <c>case(Serbia, to)</c>
		/// <br/>
		/// <c>cc(to, and)</c>
		/// <br/>
		/// <c>conj(to, from)</c>
		/// <br/>
		/// <c>nmod(flies, Serbia)</c>
		/// <br/>
		/// <c>nmod(flies', Serbia)</c>
		/// <br/>
		/// <p/>
		/// The label of the conjunct relation includes the conjunction type
		/// because if the verb has multiple cc relations then it can be impossible
		/// to infer which coordination marker belongs to which conjuncts.
		/// </summary>
		/// <param name="sg">A SemanticGraph for a sentence</param>
		private static void ExpandPrepConjunctions(SemanticGraph sg)
		{
			/* Semgrexes require a graph with a root. */
			if (sg.GetRoots().IsEmpty())
			{
				return;
			}
			SemanticGraph sgCopy = sg.MakeSoftCopy();
			SemgrexMatcher matcher = PrepConjpPattern.Matcher(sgCopy);
			IndexedWord oldGov = null;
			IndexedWord oldCcDep = null;
			IList<IndexedWord> conjDeps = Generics.NewLinkedList();
			while (matcher.Find())
			{
				IndexedWord ccDep = matcher.GetNode("cc");
				IndexedWord conjDep = matcher.GetNode("conj");
				IndexedWord gov = matcher.GetNode("gov");
				if (oldGov != null && (!gov.Equals(oldGov) || !ccDep.Equals(oldCcDep)))
				{
					ExpandPrepConjunction(sg, oldGov, conjDeps, oldCcDep);
					conjDeps = Generics.NewLinkedList();
				}
				oldCcDep = ccDep;
				oldGov = gov;
				conjDeps.Add(conjDep);
			}
			if (oldGov != null)
			{
				ExpandPrepConjunction(sg, oldGov, conjDeps, oldCcDep);
			}
		}

		/*
		* Used by expandPrepConjunctions.
		*/
		private static void ExpandPrepConjunction(SemanticGraph sg, IndexedWord gov, IList<IndexedWord> conjDeps, IndexedWord ccDep)
		{
			IndexedWord caseGov = sg.GetParent(gov);
			if (caseGov == null)
			{
				return;
			}
			IndexedWord caseGovGov = sg.GetParent(caseGov);
			if (caseGovGov == null)
			{
				return;
			}
			IndexedWord conjGov = caseGovGov.GetOriginal() != null ? caseGovGov.GetOriginal() : caseGovGov;
			GrammaticalRelation rel = sg.Reln(caseGovGov, caseGov);
			IList<IndexedWord> newConjDeps = Generics.NewLinkedList();
			foreach (IndexedWord conjDep in conjDeps)
			{
				//IndexedWord caseGovCopy = caseGov.makeSoftCopy();
				IndexedWord caseGovGovCopy = caseGovGov.MakeSoftCopy();
				/* Change conj(prep-1, prep-2) to case(prep-1-gov-copy, prep-2) */
				//SemanticGraphEdge edge = sg.getEdge(gov, conjDep);
				//sg.removeEdge(edge);
				//sg.addEdge(caseGovCopy, conjDep, CASE_MARKER, Double.NEGATIVE_INFINITY, false);
				/* Add relation to copy node. */
				//sg.addEdge(caseGovGovCopy, caseGovCopy, rel, Double.NEGATIVE_INFINITY, false);
				sg.AddEdge(conjGov, caseGovGovCopy, Conjunct, double.NegativeInfinity, false);
				newConjDeps.Add(caseGovGovCopy);
				sg.AddEdge(caseGovGovCopy, caseGov, rel, double.NegativeInfinity, true);
				IList<IndexedWord> caseMarkers = Generics.NewArrayList();
				caseMarkers.Add(conjDep);
				AddCaseMarkersToReln(sg, caseGovGovCopy, caseGov, caseMarkers);
			}
			/* Attach all children except case markers of caseGov to caseGovCopy. */
			//for (SemanticGraphEdge e : sg.outgoingEdgeList(caseGov)) {
			//  if (e.getRelation() != CASE_MARKER && ! e.getDependent().equals(ccDep)) {
			//    sg.addEdge(caseGovCopy, e.getDependent(), e.getRelation(), Double.NEGATIVE_INFINITY, false);
			//  }
			// }
			/* Attach CC node to caseGov */
			//SemanticGraphEdge edge = sg.getEdge(gov, ccDep);
			//sg.removeEdge(edge);
			//sg.addEdge(conjGov, ccDep, COORDINATION, Double.NEGATIVE_INFINITY, false);
			/* Add conjunction information for these relations already at this point.
			* It could be that we add several coordinating conjunctions while collapsing
			* and we might not know which conjunction belongs to which conjunct at a later
			* point.
			*/
			AddConjToReln(sg, conjGov, newConjDeps, ccDep);
		}

		private static readonly SemgrexPattern PpConjpPattern = SemgrexPattern.Compile("{} >/^(nmod|acl|advcl)$/ (({}=gov >case {}) >cc {}=cc >conj ({}=conj >case {}))");

		/// <summary>
		/// Expands PPs with conjunctions such as in the sentence
		/// "Bill flies to France and from Serbia." by copying the verb
		/// that governs the prepositional phrase resulting in the following
		/// relations:
		/// <p/>
		/// <c>conj:and(flies, flies')</c>
		/// <br/>
		/// <c>case(France, to)</c>
		/// <br/>
		/// <c>cc(flies, and)</c>
		/// <br/>
		/// <c>case(Serbia, from)</c>
		/// <br/>
		/// <c>nmod(flies, France)</c>
		/// <br/>
		/// <c>nmod(flies', Serbia)</c>
		/// <br/>
		/// <p/>
		/// The label of the conjunct relation includes the conjunction type
		/// because if the verb has multiple cc relations then it can be impossible
		/// to infer which coordination marker belongs to which conjuncts.
		/// </summary>
		/// <param name="sg">SemanticGraph to operate on.</param>
		private static void ExpandPPConjunctions(SemanticGraph sg)
		{
			/* Semgrexes require a graph with a root. */
			if (sg.GetRoots().IsEmpty())
			{
				return;
			}
			SemanticGraph sgCopy = sg.MakeSoftCopy();
			SemgrexMatcher matcher = PpConjpPattern.Matcher(sgCopy);
			IndexedWord oldGov = null;
			IndexedWord oldCcDep = null;
			IList<IndexedWord> conjDeps = Generics.NewLinkedList();
			while (matcher.Find())
			{
				IndexedWord conjDep = matcher.GetNode("conj");
				IndexedWord gov = matcher.GetNode("gov");
				IndexedWord ccDep = matcher.GetNode("cc");
				if (oldGov != null && (!gov.Equals(oldGov) || !ccDep.Equals(oldCcDep)))
				{
					ExpandPPConjunction(sg, oldGov, conjDeps, oldCcDep);
					conjDeps = Generics.NewLinkedList();
				}
				oldCcDep = ccDep;
				oldGov = gov;
				conjDeps.Add(conjDep);
			}
			if (oldGov != null)
			{
				ExpandPPConjunction(sg, oldGov, conjDeps, oldCcDep);
			}
		}

		/*
		* Used by expandPPConjunction.
		*/
		private static void ExpandPPConjunction(SemanticGraph sg, IndexedWord gov, IList<IndexedWord> conjDeps, IndexedWord ccDep)
		{
			IndexedWord nmodGov = sg.GetParent(gov);
			if (nmodGov == null)
			{
				return;
			}
			IndexedWord conjGov = nmodGov.GetOriginal() != null ? nmodGov.GetOriginal() : nmodGov;
			GrammaticalRelation rel = sg.Reln(nmodGov, gov);
			IList<IndexedWord> newConjDeps = Generics.NewLinkedList();
			foreach (IndexedWord conjDep in conjDeps)
			{
				IndexedWord nmodGovCopy = nmodGov.MakeSoftCopy();
				/* Change conj(nmod-1, nmod-2) to nmod(nmod-1-gov, nmod-2) */
				SemanticGraphEdge edge = sg.GetEdge(gov, conjDep);
				if (edge != null)
				{
					sg.RemoveEdge(edge);
					sg.AddEdge(nmodGovCopy, conjDep, rel, double.NegativeInfinity, false);
				}
				/* Add relation to copy node. */
				sg.AddEdge(conjGov, nmodGovCopy, Conjunct, double.NegativeInfinity, false);
				newConjDeps.Add(nmodGovCopy);
			}
			/* Attach CC node to conjGov */
			SemanticGraphEdge edge_1 = sg.GetEdge(gov, ccDep);
			if (edge_1 != null)
			{
				sg.RemoveEdge(edge_1);
				sg.AddEdge(conjGov, ccDep, Coordination, double.NegativeInfinity, false);
			}
			/* Add conjunction information for these relations already at this point.
			* It could be that we add several coordinating conjunctions while collapsing
			* and we might not know which conjunction belongs to which conjunct at a later
			* point.
			*/
			AddConjToReln(sg, conjGov, newConjDeps, ccDep);
		}

		/// <summary>
		/// Returns a GrammaticalRelation which combines the original relation and
		/// the preposition.
		/// </summary>
		private static GrammaticalRelation GetCaseMarkedRelation(GrammaticalRelation reln, string relationName)
		{
			GrammaticalRelation newReln = reln;
			if (reln.GetSpecific() != null)
			{
				reln = reln.GetParent();
			}
			if (reln == NominalModifier)
			{
				newReln = UniversalEnglishGrammaticalRelations.GetNmod(relationName);
			}
			else
			{
				if (reln == AdvClauseModifier)
				{
					newReln = UniversalEnglishGrammaticalRelations.GetAdvcl(relationName);
				}
				else
				{
					if (reln == ClausalModifier)
					{
						newReln = UniversalEnglishGrammaticalRelations.GetAcl(relationName);
					}
				}
			}
			return newReln;
		}

		private static readonly SemgrexPattern ConjunctionPattern = SemgrexPattern.Compile("{}=gov >cc {}=cc >conj {}=conj");

		/// <summary>Adds the type of conjunction to all conjunct relations.</summary>
		/// <remarks>
		/// Adds the type of conjunction to all conjunct relations.
		/// <p/>
		/// <c>cc(Marie, and)</c>
		/// ,
		/// <c>conj(Marie, Chris)</c>
		/// and
		/// <c>conj(Marie, John)</c>
		/// become
		/// <c>cc(Marie, and)</c>
		/// ,
		/// <c>conj:and(Marie, Chris)</c>
		/// and
		/// <c>conj:and(Marie, John)</c>
		/// .
		/// <p/>
		/// In case multiple coordination marker depend on the same governor
		/// the one that precedes the conjunct is appended to the conjunction relation or the
		/// first one if no preceding marker exists.
		/// <p/>
		/// Some multi-word coordination markers are collapsed to
		/// <c>conj:and</c>
		/// or
		/// <c>conj:negcc</c>
		/// .
		/// See
		/// <see cref="ConjValue(Edu.Stanford.Nlp.Ling.IndexedWord, Edu.Stanford.Nlp.Semgraph.SemanticGraph)"/>
		/// .
		/// </remarks>
		/// <param name="sg">A SemanticGraph from a sentence</param>
		private static void AddConjInformation(SemanticGraph sg)
		{
			/* Semgrexes require a graph with a root. */
			if (sg.GetRoots().IsEmpty())
			{
				return;
			}
			SemanticGraph sgCopy = sg.MakeSoftCopy();
			SemgrexMatcher matcher = ConjunctionPattern.Matcher(sgCopy);
			IndexedWord oldGov = null;
			IndexedWord oldCcDep = null;
			IList<IndexedWord> conjDeps = Generics.NewLinkedList();
			while (matcher.Find())
			{
				IndexedWord conjDep = matcher.GetNode("conj");
				IndexedWord gov = matcher.GetNode("gov");
				IndexedWord ccDep = matcher.GetNode("cc");
				if (oldGov != null && (!gov.Equals(oldGov) || !ccDep.Equals(oldCcDep)))
				{
					AddConjToReln(sg, oldGov, conjDeps, oldCcDep);
					conjDeps = Generics.NewLinkedList();
				}
				oldCcDep = ccDep;
				conjDeps.Add(conjDep);
				oldGov = gov;
			}
			if (oldGov != null)
			{
				AddConjToReln(sg, oldGov, conjDeps, oldCcDep);
			}
		}

		/*
		* Used by addConjInformation.
		*/
		private static void AddConjToReln(SemanticGraph sg, IndexedWord gov, IList<IndexedWord> conjDeps, IndexedWord ccDep)
		{
			foreach (IndexedWord conjDep in conjDeps)
			{
				SemanticGraphEdge edge = sg.GetEdge(gov, conjDep);
				if (edge.GetRelation() == Conjunct || conjDep.Index() > ccDep.Index())
				{
					edge.SetRelation(ConjValue(ccDep, sg));
				}
			}
		}

		private static readonly SemgrexPattern XcompPattern = SemgrexPattern.Compile("{}=root >xcomp {}=embedded >/^(dep|dobj)$/ {}=wh ?>/([di]obj)/ {}=obj");

		/* Used by correctWHAttachment */
		/// <summary>
		/// Tries to correct complicated cases of WH-movement in
		/// sentences such as "What does Mary seem to have?" in
		/// which "What" should attach to "have" instead of the
		/// control verb.
		/// </summary>
		/// <param name="sg">The Semantic graph to operate on.</param>
		private static void CorrectWHAttachment(SemanticGraph sg)
		{
			/* Semgrexes require a graph with a root. */
			if (sg.GetRoots().IsEmpty())
			{
				return;
			}
			SemanticGraph sgCopy = sg.MakeSoftCopy();
			SemgrexMatcher matcher = XcompPattern.Matcher(sgCopy);
			while (matcher.FindNextMatchingNode())
			{
				IndexedWord root = matcher.GetNode("root");
				IndexedWord embeddedVerb = matcher.GetNode("embedded");
				IndexedWord wh = matcher.GetNode("wh");
				IndexedWord dobj = matcher.GetNode("obj");
				/* Check if the object is a WH-word. */
				if (wh.Tag() != null && wh.Tag().StartsWith("W"))
				{
					bool reattach = false;
					/* If the control verb already has an object, then
					we have to reattach the WH-word to the verb in the embedded clause. */
					if (dobj != null)
					{
						reattach = true;
					}
					else
					{
						/* If the control verb can't have an object, we also have to reattach. */
						if (root.Value() != null && root.Tag() != null)
						{
							string lemma = Morphology.LemmaStatic(root.Value(), root.Tag());
							if (lemma != null && lemma.Matches(EnglishPatterns.NpVSInfVerbsRegex))
							{
								reattach = true;
							}
						}
					}
					if (reattach)
					{
						SemanticGraphEdge edge = sg.GetEdge(root, wh);
						if (edge != null)
						{
							sg.RemoveEdge(edge);
							sg.AddEdge(embeddedVerb, wh, DirectObject, double.NegativeInfinity, false);
						}
					}
				}
			}
		}

		/// <summary>
		/// What we do in this method is look for temporary dependencies of
		/// the type "rel" and "prep".
		/// </summary>
		/// <remarks>
		/// What we do in this method is look for temporary dependencies of
		/// the type "rel" and "prep".  These occur in sentences such as "I saw the man
		/// who you love".  In that case, we should produce dobj(love, who).
		/// On the other hand, in the sentence "... which Mr. Bush was
		/// fighting for", we should have case(which, for).
		/// </remarks>
		private static void ConvertRel(SemanticGraph sg)
		{
			foreach (SemanticGraphEdge prep in sg.FindAllRelns(Preposition))
			{
				bool changedPrep = false;
				foreach (SemanticGraphEdge nmod in sg.OutgoingEdgeIterable(prep.GetGovernor()))
				{
					// todo: It would also be good to add a rule here to prefer ccomp nsubj over dobj if there is a ccomp with no subj
					// then we could get right: Which eco-friendly options do you think there will be on the new Lexus?
					if (nmod.GetRelation() != NominalModifier && nmod.GetRelation() != Relative)
					{
						continue;
					}
					if (prep.GetDependent().Index() < nmod.GetDependent().Index())
					{
						continue;
					}
					sg.RemoveEdge(prep);
					sg.AddEdge(nmod.GetDependent(), prep.GetDependent(), CaseMarker, double.NegativeInfinity, false);
					changedPrep = true;
					if (nmod.GetRelation() == Relative)
					{
						nmod.SetRelation(NominalModifier);
					}
					break;
				}
				if (!changedPrep)
				{
					prep.SetRelation(NominalModifier);
				}
			}
			/* Rename remaining "rel" relations. */
			foreach (SemanticGraphEdge edge in sg.FindAllRelns(Relative))
			{
				edge.SetRelation(DirectObject);
			}
		}

		protected internal override void AddEnhancements(IList<TypedDependency> list, EnhancementOptions options)
		{
			SemanticGraph sg = new SemanticGraph(list);
			if (Debug)
			{
				PrintListSorted("addEnhancements: before correctDependencies()", sg.TypedDependencies());
			}
			CorrectDependencies(sg);
			if (Debug)
			{
				PrintListSorted("addEnhancements: after correctDependencies()", sg.TypedDependencies());
			}
			/* Turn multi-word prepositions into flat mwe. */
			if (options.processMultiWordPrepositions)
			{
				ProcessMultiwordPreps(sg);
				if (Debug)
				{
					PrintListSorted("addEnhancements: after processMultiwordPreps()", sg.TypedDependencies());
				}
			}
			/* Turn quantificational modifiers into flat mwe. */
			if (options.demoteQuantMod)
			{
				DemoteQuantificationalModifiers(sg);
				if (Debug)
				{
					PrintListSorted("addEnhancements: after demoteQuantificationalModifiers()", sg.TypedDependencies());
				}
			}
			/* Add copy nodes for conjoined Ps and PPs. */
			if (options.addCopyNodes)
			{
				ExpandPPConjunctions(sg);
				if (Debug)
				{
					PrintListSorted("addEnhancements: after expandPPConjunctions()", sg.TypedDependencies());
				}
				ExpandPrepConjunctions(sg);
				if (Debug)
				{
					PrintListSorted("addEnhancements: after expandPrepConjunctions()", sg.TypedDependencies());
				}
			}
			/* Add propositions to relation names. */
			if (options.enhancePrepositionalModifiers)
			{
				AddCaseMarkerInformation(sg, options.enhanceOnlyNmods);
				if (Debug)
				{
					PrintListSorted("addEnhancements: after addCaseMarkerInformation()", sg.TypedDependencies());
				}
			}
			/* Add coordinating conjunctions to relation names. */
			if (options.enhanceConjuncts)
			{
				AddConjInformation(sg);
				if (Debug)
				{
					PrintListSorted("addEnhancements: after addConjInformation()", sg.TypedDependencies());
				}
			}
			/* Add "referent" relations. */
			if (options.addReferent)
			{
				AddRef(sg);
				if (Debug)
				{
					PrintListSorted("addEnhancements: after addRef()", sg.TypedDependencies());
				}
				CollapseReferent(sg);
				if (Debug)
				{
					PrintListSorted("addEnhancements: after collapseReferent()", sg.TypedDependencies());
				}
			}
			/* Propagate dependents. */
			if (options.propagateDependents)
			{
				TreatCC(sg);
				if (Debug)
				{
					PrintListSorted("addEnhancements: after treatCC()", sg.TypedDependencies());
				}
			}
			/* Add relations between controlling subjects and controlled verbs. */
			if (options.addXSubj)
			{
				AddExtraNSubj(sg);
				if (Debug)
				{
					PrintListSorted("addEnhancements: after addExtraNSubj()", sg.TypedDependencies());
				}
			}
			CorrectSubjPass(sg);
			list.Clear();
			Sharpen.Collections.AddAll(list, sg.TypedDependencies());
			list.Sort();
		}

		/// <summary>
		/// Destructively modifies this
		/// <c>Collection&lt;TypedDependency&gt;</c>
		/// by collapsing several types of transitive pairs of dependencies or
		/// by adding additional information from the dependents to the relation
		/// of the governor.
		/// If called with a tree of dependencies and both CCprocess and
		/// includeExtras set to false, then the tree structure is preserved.
		/// <p/>
		/// <dl>
		/// <dt>nominal modifier dependencies: nmod</dt>
		/// <dd>
		/// If there exist the relations
		/// <c>case(hat, in)</c>
		/// and
		/// <c>nmod(in, hat)</c>
		/// then
		/// the
		/// <c>nmod</c>
		/// relation is enhanced to
		/// <c>nmod:in(cat, hat)</c>
		/// .
		/// The
		/// <c>case(hat, in)</c>
		/// relation is preserved.</dd>
		/// <dt>clausal modifier of noun/adverbial clause modifier with case markers: acs/advcl</dt>
		/// <dd>
		/// If there exist the relations
		/// <c>case(attacking, of)</c>
		/// and
		/// <c>advcl(heard, attacking)</c>
		/// then
		/// the
		/// <c>nmod</c>
		/// relation is enhanced to
		/// <c>nmod:of(heard, attacking)</c>
		/// .
		/// The
		/// <c>case(attacking, of)</c>
		/// relation is preserved.</dd>
		/// <dt>conjunct dependencies</dt>
		/// <dd>
		/// If there exist the relations
		/// <c>cc(investors, and)</c>
		/// and
		/// <c>conj(investors, regulators)</c>
		/// , then the
		/// <c>conj</c>
		/// relation is
		/// enhanced to
		/// <c>conj:and(investors, regulators)</c>
		/// </dd>
		/// <dt>For relative clauses, it will collapse referent</dt>
		/// <dd>
		/// <c>ref(man, that)</c>
		/// and
		/// <c>dobj(love, that)</c>
		/// are collapsed
		/// to
		/// <c>dobj(love, man)</c>
		/// </dd>
		/// </dl>
		/// </summary>
		protected internal override void CollapseDependencies(IList<TypedDependency> list, bool CCprocess, GrammaticalStructure.Extras includeExtras)
		{
			EnhancementOptions options = new EnhancementOptions(CollapsedOptions);
			if (includeExtras.doRef)
			{
				options.addReferent = true;
			}
			if (includeExtras.doSubj)
			{
				options.addXSubj = true;
			}
			if (CCprocess)
			{
				options.propagateDependents = true;
			}
			AddEnhancements(list, options);
		}

		protected internal override void CollapseDependenciesTree(IList<TypedDependency> list)
		{
			CollapseDependencies(list, false, GrammaticalStructure.Extras.None);
		}

		/// <summary>Does some hard coding to deal with relation in CONJP.</summary>
		/// <remarks>
		/// Does some hard coding to deal with relation in CONJP. For now we deal with:
		/// but not, if not, instead of, rather than, but rather GO TO negcc <br/>
		/// as well as, not to mention, but also, & GO TO and.
		/// </remarks>
		/// <param name="cc">The head dependency of the conjunction marker</param>
		/// <param name="sg">The complete current semantic graph</param>
		/// <returns>
		/// A GrammaticalRelation made from a normalized form of that
		/// conjunction.
		/// </returns>
		private static GrammaticalRelation ConjValue(IndexedWord cc, SemanticGraph sg)
		{
			int pos = cc.Index();
			string newConj = cc.Value().ToLower();
			if (newConj.Equals("not"))
			{
				IndexedWord prevWord = sg.GetNodeByIndexSafe(pos - 1);
				if (prevWord != null && prevWord.Value().ToLower().Equals("but"))
				{
					return UniversalEnglishGrammaticalRelations.GetConj("negcc");
				}
			}
			IndexedWord secondIWord = sg.GetNodeByIndexSafe(pos + 1);
			if (secondIWord == null)
			{
				return UniversalEnglishGrammaticalRelations.GetConj(cc.Value());
			}
			string secondWord = secondIWord.Value().ToLower();
			if (newConj.Equals("but"))
			{
				if (secondWord.Equals("rather"))
				{
					newConj = "negcc";
				}
				else
				{
					if (secondWord.Equals("also"))
					{
						newConj = "and";
					}
				}
			}
			else
			{
				if (newConj.Equals("if") && secondWord.Equals("not"))
				{
					newConj = "negcc";
				}
				else
				{
					if (newConj.Equals("instead") && secondWord.Equals("of"))
					{
						newConj = "negcc";
					}
					else
					{
						if (newConj.Equals("rather") && secondWord.Equals("than"))
						{
							newConj = "negcc";
						}
						else
						{
							if (newConj.Equals("as") && secondWord.Equals("well"))
							{
								newConj = "and";
							}
							else
							{
								if (newConj.Equals("not") && secondWord.Equals("to"))
								{
									IndexedWord thirdIWord = sg.GetNodeByIndexSafe(pos + 2);
									string thirdWord = thirdIWord != null ? thirdIWord.Value().ToLower() : null;
									if (thirdWord != null && thirdWord.Equals("mention"))
									{
										newConj = "and";
									}
								}
							}
						}
					}
				}
			}
			return UniversalEnglishGrammaticalRelations.GetConj(newConj);
		}

		private static void TreatCC(SemanticGraph sg)
		{
			// Construct a map from tree nodes to the set of typed
			// dependencies in which the node appears as dependent.
			IDictionary<IndexedWord, ICollection<SemanticGraphEdge>> map = Generics.NewHashMap();
			// Construct a map of tree nodes being governor of a subject grammatical
			// relation to that relation
			IDictionary<IndexedWord, SemanticGraphEdge> subjectMap = Generics.NewHashMap();
			// Construct a set of TreeGraphNodes with a passive auxiliary on them
			ICollection<IndexedWord> withPassiveAuxiliary = Generics.NewHashSet();
			// Construct a map of tree nodes being governor of an object grammatical
			// relation to that relation
			// Map<TreeGraphNode, TypedDependency> objectMap = new
			// HashMap<TreeGraphNode, TypedDependency>();
			IList<IndexedWord> rcmodHeads = Generics.NewArrayList();
			IList<IndexedWord> prepcDep = Generics.NewArrayList();
			foreach (SemanticGraphEdge edge in sg.EdgeIterable())
			{
				if (!map.Contains(edge.GetDependent()))
				{
					// NB: Here and in other places below, we use a TreeSet (which extends
					// SortedSet) to guarantee that results are deterministic)
					map[edge.GetDependent()] = new TreeSet<SemanticGraphEdge>();
				}
				map[edge.GetDependent()].Add(edge);
				if (edge.GetRelation().Equals(AuxPassiveModifier))
				{
					withPassiveAuxiliary.Add(edge.GetGovernor());
				}
				// look for subjects
				if (edge.GetRelation().GetParent() == NominalSubject || edge.GetRelation().GetParent() == Subject || edge.GetRelation().GetParent() == ClausalSubject)
				{
					if (!subjectMap.Contains(edge.GetGovernor()))
					{
						subjectMap[edge.GetGovernor()] = edge;
					}
				}
				// look for objects
				// this map was only required by the code commented out below, so comment
				// it out too
				// if (typedDep.reln() == DIRECT_OBJECT) {
				// if (!objectMap.containsKey(typedDep.gov())) {
				// objectMap.put(typedDep.gov(), typedDep);
				// }
				// }
				// look for rcmod relations
				if (edge.GetRelation() == RelativeClauseModifier)
				{
					rcmodHeads.Add(edge.GetGovernor());
				}
				// look for prepc relations: put the dependent of such a relation in the
				// list
				// to avoid wrong propagation of dobj
				if (edge.GetRelation().ToString().StartsWith("acl:") || edge.GetRelation().ToString().StartsWith("advcl:"))
				{
					prepcDep.Add(edge.GetDependent());
				}
			}
			// log.info(map);
			// if (DEBUG) log.info("Subject map: " + subjectMap);
			// if (DEBUG) log.info("Object map: " + objectMap);
			// log.info(rcmodHeads);
			// create a new list of typed dependencies
			//Collection<TypedDependency> newTypedDeps = new ArrayList<TypedDependency>(list);
			SemanticGraph sgCopy = sg.MakeSoftCopy();
			// find typed deps of form conj(gov,dep)
			foreach (SemanticGraphEdge edge_1 in sgCopy.EdgeIterable())
			{
				if (UniversalEnglishGrammaticalRelations.GetConjs().Contains(edge_1.GetRelation()))
				{
					IndexedWord gov = edge_1.GetGovernor();
					IndexedWord dep = edge_1.GetDependent();
					// look at the dep in the conjunct
					ICollection<SemanticGraphEdge> gov_relations = map[gov];
					// log.info("gov " + gov);
					if (gov_relations != null)
					{
						foreach (SemanticGraphEdge edge1 in gov_relations)
						{
							// log.info("gov rel " + td1);
							IndexedWord newGov = edge1.GetGovernor();
							// in the case of errors in the basic dependencies, it
							// is possible to have overlapping newGov & dep
							if (newGov.Equals(dep))
							{
								continue;
							}
							GrammaticalRelation newRel = edge1.GetRelation();
							//TODO: Do we want to copy case markers here?
							if (newRel != Root && newRel != CaseMarker)
							{
								if (rcmodHeads.Contains(gov) && rcmodHeads.Contains(dep))
								{
									// to prevent wrong propagation in the case of long dependencies in relative clauses
									if (newRel != DirectObject && newRel != NominalSubject)
									{
										if (Debug)
										{
											log.Info("Adding new " + newRel + " dependency from " + newGov + " to " + dep + " (subj/obj case)");
										}
										sg.AddEdge(newGov, dep, newRel, double.NegativeInfinity, true);
									}
								}
								else
								{
									if (Debug)
									{
										log.Info("Adding new " + newRel + " dependency from " + newGov + " to " + dep);
									}
									sg.AddEdge(newGov, dep, newRel, double.NegativeInfinity, true);
								}
							}
						}
					}
					// propagate subjects
					// look at the gov in the conjunct: if it is has a subject relation,
					// the dep is a verb and the dep doesn't have a subject relation
					// then we want to add a subject relation for the dep.
					// (By testing for the dep to be a verb, we are going to miss subject of
					// copular verbs! but
					// is it safe to relax this assumption?? i.e., just test for the subject
					// part)
					// CDM 2008: I also added in JJ, since participial verbs are often
					// tagged JJ
					string tag = dep.Tag();
					if (subjectMap.Contains(gov) && (tag.StartsWith("VB") || tag.StartsWith("JJ")) && !subjectMap.Contains(dep))
					{
						SemanticGraphEdge tdsubj = subjectMap[gov];
						// check for wrong nsubjpass: if the new verb is VB or VBZ or VBP or JJ, then
						// add nsubj (if it is tagged correctly, should do this for VBD too, but we don't)
						GrammaticalRelation relation = tdsubj.GetRelation();
						if (relation == NominalPassiveSubject)
						{
							if (IsDefinitelyActive(tag))
							{
								relation = NominalSubject;
							}
						}
						else
						{
							if (relation == ClausalPassiveSubject)
							{
								if (IsDefinitelyActive(tag))
								{
									relation = ClausalSubject;
								}
							}
							else
							{
								if (relation == NominalSubject)
								{
									if (withPassiveAuxiliary.Contains(dep))
									{
										relation = NominalPassiveSubject;
									}
								}
								else
								{
									if (relation == ClausalSubject)
									{
										if (withPassiveAuxiliary.Contains(dep))
										{
											relation = ClausalPassiveSubject;
										}
									}
								}
							}
						}
						if (Debug)
						{
							log.Info("Adding new " + relation + " dependency from " + dep + " to " + tdsubj.GetDependent() + " (subj propagation case)");
						}
						sg.AddEdge(dep, tdsubj.GetDependent(), relation, double.NegativeInfinity, true);
					}
				}
			}
		}

		// propagate objects
		// cdm july 2010: This bit of code would copy a dobj from the first
		// clause to a later conjoined clause if it didn't
		// contain its own dobj or prepc. But this is too aggressive and wrong
		// if the later clause is intransitive
		// (including passivized cases) and so I think we have to not have this
		// done always, and see no good "sometimes" heuristic.
		// IF WE WERE TO REINSTATE, SHOULD ALSO NOT ADD OBJ IF THERE IS A ccomp
		// (SBAR).
		// if (objectMap.containsKey(gov) &&
		// dep.tag().startsWith("VB") && ! objectMap.containsKey(dep)
		// && ! prepcDep.contains(gov)) {
		// TypedDependency tdobj = objectMap.get(gov);
		// if (DEBUG) {
		// log.info("Adding new " + tdobj.reln() + " dependency from "
		// + dep + " to " + tdobj.dep() + " (obj propagation case)");
		// }
		// newTypedDeps.add(new TypedDependency(tdobj.reln(), dep,
		// tdobj.dep()));
		// }
		private static bool IsDefinitelyActive(string tag)
		{
			// we should include VBD, but don't as it is often a tagging mistake.
			return tag.Equals("VB") || tag.Equals("VBZ") || tag.Equals("VBP") || tag.StartsWith("JJ");
		}

		/// <summary>This method will collapse a referent relation such as follows.</summary>
		/// <remarks>
		/// This method will collapse a referent relation such as follows. e.g.:
		/// "The man that I love ... " ref(man, that) dobj(love, that) -&gt; ref(man, that) dobj(love,
		/// man)
		/// </remarks>
		private static void CollapseReferent(SemanticGraph sg)
		{
			// find typed deps of form ref(gov, dep)
			// put them in a List for processing
			IList<SemanticGraphEdge> refs = new List<SemanticGraphEdge>(sg.FindAllRelns(Referent));
			SemanticGraph sgCopy = sg.MakeSoftCopy();
			// now substitute target of referent where possible
			foreach (SemanticGraphEdge @ref in refs)
			{
				IndexedWord dep = @ref.GetDependent();
				// take the relative word
				IndexedWord ant = @ref.GetGovernor();
				// take the antecedent
				for (IEnumerator<SemanticGraphEdge> iter = sgCopy.IncomingEdgeIterator(dep); iter.MoveNext(); )
				{
					SemanticGraphEdge edge = iter.Current;
					// the last condition below maybe shouldn't be necessary, but it has
					// helped stop things going haywire a couple of times (it stops the
					// creation of a unit cycle that probably leaves something else
					// disconnected) [cdm Jan 2010]
					if (edge.GetRelation() != Referent && !edge.GetGovernor().Equals(ant))
					{
						sg.RemoveEdge(edge);
						sg.AddEdge(edge.GetGovernor(), ant, edge.GetRelation(), double.NegativeInfinity, true);
					}
				}
			}
		}

		/// <summary>Look for ref rules for a given word.</summary>
		/// <remarks>
		/// Look for ref rules for a given word.  We look through the
		/// children and grandchildren of the acl:relcl dependency, and if any
		/// children or grandchildren depend on a that/what/which/etc word,
		/// we take the leftmost that/what/which/etc word as the dependent
		/// for the ref TypedDependency.
		/// </remarks>
		private static void AddRef(SemanticGraph sg)
		{
			foreach (SemanticGraphEdge edge in sg.FindAllRelns(RelativeClauseModifier))
			{
				IndexedWord head = edge.GetGovernor();
				IndexedWord modifier = edge.GetDependent();
				SemanticGraphEdge leftChildEdge = null;
				foreach (SemanticGraphEdge childEdge in sg.OutgoingEdgeIterable(modifier))
				{
					if (EnglishPatterns.RelativizingWordPattern.Matcher(childEdge.GetDependent().Value()).Matches() && (leftChildEdge == null || childEdge.GetDependent().Index() < leftChildEdge.GetDependent().Index()))
					{
						leftChildEdge = childEdge;
					}
				}
				SemanticGraphEdge leftGrandchildEdge = null;
				foreach (SemanticGraphEdge childEdge_1 in sg.OutgoingEdgeIterable(modifier))
				{
					foreach (SemanticGraphEdge grandchildEdge in sg.OutgoingEdgeIterable(childEdge_1.GetDependent()))
					{
						if (EnglishPatterns.RelativizingWordPattern.Matcher(grandchildEdge.GetDependent().Value()).Matches() && (leftGrandchildEdge == null || grandchildEdge.GetDependent().Index() < leftGrandchildEdge.GetDependent().Index()))
						{
							leftGrandchildEdge = grandchildEdge;
						}
					}
				}
				IndexedWord newDep = null;
				if (leftGrandchildEdge != null && (leftChildEdge == null || leftGrandchildEdge.GetDependent().Index() < leftChildEdge.GetDependent().Index()))
				{
					newDep = leftGrandchildEdge.GetDependent();
				}
				else
				{
					if (leftChildEdge != null)
					{
						newDep = leftChildEdge.GetDependent();
					}
				}
				if (newDep != null && !sg.ContainsEdge(head, newDep))
				{
					sg.AddEdge(head, newDep, Referent, double.NegativeInfinity, false);
				}
			}
		}

		/// <summary>Add extra nsubj dependencies when collapsing basic dependencies.</summary>
		/// <remarks>
		/// Add extra nsubj dependencies when collapsing basic dependencies.
		/// <br/>
		/// In the general case, we look for an aux modifier under an xcomp
		/// modifier, and assuming there aren't already associated nsubj
		/// dependencies as daughters of the original xcomp dependency, we
		/// add nsubj dependencies for each nsubj daughter of the aux.
		/// <br/>
		/// There is also a special case for "to" words, in which case we add
		/// a dependency if and only if there is no nsubj associated with the
		/// xcomp and there is no other aux dependency.  This accounts for
		/// sentences such as "he decided not to" with no following verb.
		/// </remarks>
		private static void AddExtraNSubj(SemanticGraph sg)
		{
			foreach (SemanticGraphEdge xcomp in sg.FindAllRelns(XclausalComplement))
			{
				IndexedWord modifier = xcomp.GetDependent();
				IndexedWord head = xcomp.GetGovernor();
				bool hasSubjectDaughter = false;
				bool hasAux = false;
				IList<IndexedWord> subjects = Generics.NewArrayList();
				IList<IndexedWord> objects = Generics.NewArrayList();
				foreach (SemanticGraphEdge dep in sg.EdgeIterable())
				{
					// already have a subject dependency
					if ((dep.GetRelation() == NominalSubject || dep.GetRelation() == NominalPassiveSubject) && dep.GetGovernor().Equals(modifier))
					{
						hasSubjectDaughter = true;
						break;
					}
					if ((dep.GetRelation() == AuxModifier || dep.GetRelation() == Marker) && dep.GetGovernor().Equals(modifier))
					{
						hasAux = true;
					}
					if ((dep.GetRelation() == NominalSubject || dep.GetRelation() == NominalPassiveSubject) && dep.GetGovernor().Equals(head))
					{
						subjects.Add(dep.GetDependent());
					}
					if (dep.GetRelation() == DirectObject && dep.GetGovernor().Equals(head))
					{
						objects.Add(dep.GetDependent());
					}
				}
				// if we already have an nsubj dependency, no need to add an extra nsubj
				if (hasSubjectDaughter)
				{
					continue;
				}
				if ((Sharpen.Runtime.EqualsIgnoreCase(modifier.Value(), "to") && hasAux) || (!Sharpen.Runtime.EqualsIgnoreCase(modifier.Value(), "to") && !hasAux))
				{
					continue;
				}
				// In general, we find that the objects of the verb are better
				// for extra nsubj than the original nsubj of the verb.  For example,
				// "Many investors wrote asking the SEC to require ..."
				// There is no nsubj of asking, but the dobj, SEC, is the extra nsubj of require.
				// Similarly, "The law tells them when to do so"
				// Instead of nsubj(do, law) we want nsubj(do, them)
				if (!objects.IsEmpty())
				{
					foreach (IndexedWord @object in objects)
					{
						if (!sg.ContainsEdge(modifier, @object))
						{
							sg.AddEdge(modifier, @object, ControllingNominalSubject, double.NegativeInfinity, true);
						}
					}
				}
				else
				{
					foreach (IndexedWord subject in subjects)
					{
						if (!sg.ContainsEdge(modifier, subject))
						{
							sg.AddEdge(modifier, subject, ControllingNominalSubject, double.NegativeInfinity, true);
						}
					}
				}
			}
		}

		private static readonly SemgrexPattern CorrectSubjpassPattern = SemgrexPattern.Compile("{}=gov >auxpass {} >/^(nsubj|csubj).*$/ {}=subj");

		/// <summary>
		/// This method corrects subjects of verbs for which we identified an auxpass,
		/// but didn't identify the subject as passive.
		/// </summary>
		/// <param name="sg">SemanticGraph to work on</param>
		private static void CorrectSubjPass(SemanticGraph sg)
		{
			/* If the graph doesn't have a root (most likely because
			* a parsing error, we can't match Semgrexes, so do
			* nothing. */
			if (sg.GetRoots().IsEmpty())
			{
				return;
			}
			SemanticGraph sgCopy = sg.MakeSoftCopy();
			SemgrexMatcher matcher = CorrectSubjpassPattern.Matcher(sgCopy);
			while (matcher.Find())
			{
				IndexedWord gov = matcher.GetNode("gov");
				IndexedWord subj = matcher.GetNode("subj");
				SemanticGraphEdge edge = sg.GetEdge(gov, subj);
				GrammaticalRelation reln = null;
				if (edge.GetRelation() == NominalSubject)
				{
					reln = NominalPassiveSubject;
				}
				else
				{
					if (edge.GetRelation() == ClausalSubject)
					{
						reln = ClausalPassiveSubject;
					}
					else
					{
						if (edge.GetRelation() == ControllingNominalSubject)
						{
							reln = ControllingNominalPassiveSubject;
						}
						else
						{
							if (edge.GetRelation() == ControllingClausalSubject)
							{
								reln = ControllingClausalPassiveSubject;
							}
						}
					}
				}
				if (reln != null)
				{
					sg.RemoveEdge(edge);
					sg.AddEdge(gov, subj, reln, double.NegativeInfinity, false);
				}
			}
		}

		private static readonly string[] TwoWordPrepsRegular = new string[] { "across_from", "along_with", "alongside_of", "apart_from", "as_for", "as_from", "as_of", "as_per", "as_to", "aside_from", "based_on", "close_by", "close_to", "contrary_to"
			, "compared_to", "compared_with", " depending_on", "except_for", "exclusive_of", "far_from", "followed_by", "inside_of", "irrespective_of", "next_to", "near_to", "off_of", "out_of", "outside_of", "owing_to", "preliminary_to", "preparatory_to"
			, "previous_to", " prior_to", "pursuant_to", "regardless_of", "subsequent_to", "thanks_to", "together_with" };

		private static readonly string[] TwoWordPrepsComplex = new string[] { "apart_from", "as_from", "aside_from", "away_from", "close_by", "close_to", "contrary_to", "far_from", "next_to", "near_to", "out_of", "outside_of", "pursuant_to", "regardless_of"
			, "together_with" };

		private static readonly string[] ThreeWordPreps = new string[] { "by_means_of", "in_accordance_with", "in_addition_to", "in_case_of", "in_front_of", "in_lieu_of", "in_place_of", "in_spite_of", "on_account_of", "on_behalf_of", "on_top_of", "with_regard_to"
			, "with_respect_to" };

		private static readonly SemgrexPattern TwoWordPrepsRegularPattern = SemgrexPattern.Compile("{}=gov >/(case|advmod)/ ({}=w1 !> {}) >case ({}=w2 !== {}=w1 !> {})");

		private static readonly SemgrexPattern TwoWordPrepsComplexPattern = SemgrexPattern.Compile("({}=w1 >nmod ({}=gov2 >case ({}=w2 !> {}))) [ == {$} | < {}=gov ]");

		private static readonly SemgrexPattern ThreeWordPrepsPattern = SemgrexPattern.Compile("({}=w2 >/(nmod|acl|advcl)/ ({}=gov2 >/(case|mark)/ ({}=w3 !> {}))) >case ({}=w1 !> {}) [ < {}=gov | == {$} ]");

		/* These multi-word prepositions typically have a
		*   case/advmod(gov, w1)
		*   case(gov, w2)
		* structure in the basic representation.
		*
		* Kept in alphabetical order.
		*/
		/* These multi-word prepositions can have a
		*   advmod(gov1, w1)
		*   nmod(w1, gov2)
		*   case(gov2, w2)
		* structure in the basic representation.
		*
		* Kept in alphabetical order.
		*/
		/*
		* Multi-word prepositions with the structure
		*   case(w2, w1)
		*   nmod(gov, w2)
		*   case(gov2, w3)
		*   nmod(w2, gov2)
		* in the basic representations.
		*/
		/// <summary>Process multi-word prepositions.</summary>
		private static void ProcessMultiwordPreps(SemanticGraph sg)
		{
			/* Semgrexes require a graph with a root. */
			if (sg.GetRoots().IsEmpty())
			{
				return;
			}
			Dictionary<string, HashSet<int>> bigrams = new Dictionary<string, HashSet<int>>();
			Dictionary<string, HashSet<int>> trigrams = new Dictionary<string, HashSet<int>>();
			IList<IndexedWord> vertexList = sg.VertexListSorted();
			int numWords = vertexList.Count;
			for (int i = 1; i < numWords; i++)
			{
				string bigram = vertexList[i - 1].Value().ToLower() + '_' + vertexList[i].Value().ToLower();
				bigrams.PutIfAbsent(bigram, new HashSet<int>());
				bigrams[bigram].Add(vertexList[i - 1].Index());
				if (i > 1)
				{
					string trigram = vertexList[i - 2].Value().ToLower() + '_' + bigram;
					trigrams.PutIfAbsent(trigram, new HashSet<int>());
					trigrams[trigram].Add(vertexList[i - 2].Index());
				}
			}
			/* Simple two-word prepositions. */
			ProcessSimple2WP(sg, bigrams);
			/* More complex two-word prepositions in which the first
			* preposition is the head of the prepositional phrase. */
			ProcessComplex2WP(sg, bigrams);
			/* Process three-word prepositions. */
			Process3WP(sg, trigrams);
		}

		/// <summary>Processes all the two-word prepositions in TWO_WORD_PREPS_REGULAR.</summary>
		private static void ProcessSimple2WP(SemanticGraph sg, Dictionary<string, HashSet<int>> bigrams)
		{
			foreach (string bigram in TwoWordPrepsRegular)
			{
				if (bigrams[bigram] == null)
				{
					continue;
				}
				foreach (int i in bigrams[bigram])
				{
					IndexedWord w1 = sg.GetNodeByIndexSafe(i);
					IndexedWord w2 = sg.GetNodeByIndexSafe(i + 1);
					if (w1 == null || w2 == null)
					{
						continue;
					}
					SemgrexMatcher matcher = TwoWordPrepsRegularPattern.Matcher(sg);
					IndexedWord gov = null;
					while (matcher.Find())
					{
						if (w1.Equals(matcher.GetNode("w1")) && w2.Equals(matcher.GetNode("w2")))
						{
							gov = matcher.GetNode("gov");
							break;
						}
					}
					if (gov == null)
					{
						continue;
					}
					CreateMultiWordExpression(sg, gov, CaseMarker, w1, w2);
				}
			}
		}

		/// <summary>Processes all the two-word prepositions in TWO_WORD_PREPS_COMPLEX.</summary>
		private static void ProcessComplex2WP(SemanticGraph sg, Dictionary<string, HashSet<int>> bigrams)
		{
			foreach (string bigram in TwoWordPrepsComplex)
			{
				if (bigrams[bigram] == null)
				{
					continue;
				}
				foreach (int i in bigrams[bigram])
				{
					IndexedWord w1 = sg.GetNodeByIndexSafe(i);
					IndexedWord w2 = sg.GetNodeByIndexSafe(i + 1);
					if (w1 == null || w2 == null)
					{
						continue;
					}
					SemgrexMatcher matcher = TwoWordPrepsComplexPattern.Matcher(sg);
					IndexedWord gov = null;
					IndexedWord gov2 = null;
					while (matcher.Find())
					{
						if (w1.Equals(matcher.GetNode("w1")) && w2.Equals(matcher.GetNode("w2")))
						{
							gov = matcher.GetNode("gov");
							gov2 = matcher.GetNode("gov2");
							break;
						}
					}
					if (gov2 == null)
					{
						continue;
					}
					/* Attach the head of the prepositional phrase to
					* the head of w1. */
					if (sg.GetRoots().Contains(w1))
					{
						SemanticGraphEdge edge = sg.GetEdge(w1, gov2);
						if (edge == null)
						{
							continue;
						}
						sg.RemoveEdge(edge);
						sg.GetRoots().Remove(w1);
						sg.AddRoot(gov2);
					}
					else
					{
						SemanticGraphEdge edge = sg.GetEdge(w1, gov2);
						if (edge == null)
						{
							continue;
						}
						sg.RemoveEdge(edge);
						gov = gov == null ? sg.GetParent(w1) : gov;
						if (gov == null)
						{
							continue;
						}
						/* Determine the relation to use. If it is a relation that can
						* join two clauses and w1 is the head of a copular construction, then
						* use the relation of w1 and its parent. Otherwise use the relation of edge. */
						GrammaticalRelation reln = edge.GetRelation();
						if (sg.HasChildWithReln(w1, Copula))
						{
							GrammaticalRelation reln2 = sg.GetEdge(gov, w1).GetRelation();
							if (clauseRelations.Contains(reln2))
							{
								reln = reln2;
							}
						}
						sg.AddEdge(gov, gov2, reln, double.NegativeInfinity, false);
					}
					/* Make children of w1 dependents of gov2. */
					foreach (SemanticGraphEdge edge2 in sg.GetOutEdgesSorted(w1))
					{
						sg.RemoveEdge(edge2);
						sg.AddEdge(gov2, edge2.GetDependent(), edge2.GetRelation(), edge2.GetWeight(), edge2.IsExtra());
					}
					CreateMultiWordExpression(sg, gov2, CaseMarker, w1, w2);
				}
			}
		}

		/// <summary>Processes all the three-word prepositions in THREE_WORD_PREPS.</summary>
		private static void Process3WP(SemanticGraph sg, Dictionary<string, HashSet<int>> trigrams)
		{
			foreach (string trigram in ThreeWordPreps)
			{
				if (trigrams[trigram] == null)
				{
					continue;
				}
				foreach (int i in trigrams[trigram])
				{
					IndexedWord w1 = sg.GetNodeByIndexSafe(i);
					IndexedWord w2 = sg.GetNodeByIndexSafe(i + 1);
					IndexedWord w3 = sg.GetNodeByIndexSafe(i + 2);
					if (w1 == null || w2 == null || w3 == null)
					{
						continue;
					}
					SemgrexMatcher matcher = ThreeWordPrepsPattern.Matcher(sg);
					IndexedWord gov = null;
					IndexedWord gov2 = null;
					while (matcher.Find())
					{
						if (w1.Equals(matcher.GetNode("w1")) && w2.Equals(matcher.GetNode("w2")) && w3.Equals(matcher.GetNode("w3")))
						{
							gov = matcher.GetNode("gov");
							gov2 = matcher.GetNode("gov2");
							break;
						}
					}
					if (gov2 == null)
					{
						continue;
					}
					GrammaticalRelation markerReln = CaseMarker;
					if (sg.GetRoots().Contains(w2))
					{
						SemanticGraphEdge edge = sg.GetEdge(w2, gov2);
						if (edge == null)
						{
							continue;
						}
						sg.RemoveEdge(edge);
						sg.GetRoots().Remove(w2);
						sg.AddRoot(gov2);
					}
					else
					{
						SemanticGraphEdge edge = sg.GetEdge(w2, gov2);
						if (edge == null)
						{
							continue;
						}
						sg.RemoveEdge(edge);
						gov = gov == null ? sg.GetParent(w2) : gov;
						if (gov == null)
						{
							continue;
						}
						GrammaticalRelation reln = sg.GetEdge(gov, w2).GetRelation();
						if (reln == NominalModifier && (edge.GetRelation() == ClausalModifier || edge.GetRelation() == AdvClauseModifier))
						{
							reln = edge.GetRelation();
							markerReln = Marker;
						}
						sg.AddEdge(gov, gov2, reln, double.NegativeInfinity, false);
					}
					/* Make children of w2 dependents of gov2. */
					foreach (SemanticGraphEdge edge2 in sg.GetOutEdgesSorted(w2))
					{
						sg.RemoveEdge(edge2);
						sg.AddEdge(gov2, edge2.GetDependent(), edge2.GetRelation(), edge2.GetWeight(), edge2.IsExtra());
					}
					CreateMultiWordExpression(sg, gov2, markerReln, w1, w2, w3);
				}
			}
		}

		private static void CreateMultiWordExpression(SemanticGraph sg, IndexedWord gov, GrammaticalRelation reln, params IndexedWord[] words)
		{
			if (sg.GetRoots().IsEmpty() || gov == null || words.Length < 1)
			{
				return;
			}
			bool first = true;
			IndexedWord mweHead = null;
			foreach (IndexedWord word in words)
			{
				IndexedWord wordGov = sg.GetParent(word);
				if (wordGov != null)
				{
					SemanticGraphEdge edge = sg.GetEdge(wordGov, word);
					if (edge != null)
					{
						sg.RemoveEdge(edge);
					}
				}
				if (first)
				{
					sg.AddEdge(gov, word, reln, double.NegativeInfinity, false);
					mweHead = word;
					first = false;
				}
				else
				{
					sg.AddEdge(mweHead, word, MultiWordExpression, double.NegativeInfinity, false);
				}
			}
		}

		/// <summary>A lot of, an assortment of, ...</summary>
		private static readonly SemgrexPattern QuantMod3wPattern = SemgrexPattern.Compile("{word:/(?i:lot|assortment|number|couple|bunch|handful|litany|sheaf|slew|dozen|series|variety|multitude|wad|clutch|wave|mountain|array|spate|string|ton|range|plethora|heap|sort|form|kind|type|version|bit|pair|triple|total)/}=w2 >det {word:/(?i:an?)/}=w1 !>amod {} >nmod ({tag:/(NN.*|PRP.*)/}=gov >case {word:/(?i:of)/}=w3) . {}=w3"
			);

		private static readonly SemgrexPattern[] QuantMod2wPatterns = new SemgrexPattern[] { SemgrexPattern.Compile("{word:/(?i:lots|many|several|plenty|tons|dozens|multitudes|mountains|loads|pairs|tens|hundreds|thousands|millions|billions|trillions|[0-9]+s)/}=w1 >nmod ({tag:/(NN.*|PRP.*)/}=gov >case {word:/(?i:of)/}=w2) . {}=w2"
			), SemgrexPattern.Compile("{word:/(?i:some|all|both|neither|everyone|nobody|one|two|three|four|five|six|seven|eight|nine|ten|hundred|thousand|million|billion|trillion|[0-9]+)/}=w1 [>nmod ({tag:/(NN.*)/}=gov >case ({word:/(?i:of)/}=w2 $+ {}=det) >det {}=det) |  >nmod ({tag:/(PRP.*)/}=gov >case {word:/(?i:of)/}=w2)] . {}=w2"
			) };

		/* Lots of, dozens of, heaps of ... */
		/* Some of the ..., all of them, ... */
		private static void DemoteQuantificationalModifiers(SemanticGraph sg)
		{
			SemanticGraph sgCopy = sg.MakeSoftCopy();
			SemgrexMatcher matcher = QuantMod3wPattern.Matcher(sgCopy);
			while (matcher.FindNextMatchingNode())
			{
				IndexedWord w1 = matcher.GetNode("w1");
				IndexedWord w2 = matcher.GetNode("w2");
				IndexedWord w3 = matcher.GetNode("w3");
				IndexedWord gov = matcher.GetNode("gov");
				DemoteQmodParentHelper(sg, gov, w2);
				IList<IndexedWord> otherDeps = Generics.NewLinkedList();
				otherDeps.Add(w1);
				otherDeps.Add(w2);
				otherDeps.Add(w3);
				DemoteQmodMWEHelper(sg, otherDeps, gov, w2);
			}
			foreach (SemgrexPattern p in QuantMod2wPatterns)
			{
				sgCopy = sg.MakeSoftCopy();
				matcher = p.Matcher(sgCopy);
				while (matcher.FindNextMatchingNode())
				{
					IndexedWord w1 = matcher.GetNode("w1");
					IndexedWord w2 = matcher.GetNode("w2");
					IndexedWord gov = matcher.GetNode("gov");
					DemoteQmodParentHelper(sg, gov, w1);
					IList<IndexedWord> otherDeps = Generics.NewLinkedList();
					otherDeps.Add(w1);
					otherDeps.Add(w2);
					DemoteQmodMWEHelper(sg, otherDeps, gov, w1);
				}
			}
		}

		private static void DemoteQmodMWEHelper(SemanticGraph sg, IList<IndexedWord> otherDeps, IndexedWord gov, IndexedWord oldHead)
		{
			CreateMultiWordExpression(sg, gov, Qmod, Sharpen.Collections.ToArray(otherDeps, new IndexedWord[otherDeps.Count]));
		}

		private static void DemoteQmodParentHelper(SemanticGraph sg, IndexedWord gov, IndexedWord oldHead)
		{
			if (!sg.GetRoots().Contains(oldHead))
			{
				IndexedWord parent = sg.GetParent(oldHead);
				if (parent == null)
				{
					return;
				}
				SemanticGraphEdge edge = sg.GetEdge(parent, oldHead);
				sg.AddEdge(parent, gov, edge.GetRelation(), edge.GetWeight(), edge.IsExtra());
				sg.RemoveEdge(edge);
			}
			else
			{
				sg.GetRoots().Remove(oldHead);
				sg.AddRoot(gov);
			}
			//temporary relation to keep the graph connected
			sg.AddEdge(gov, oldHead, Dependent, double.NegativeInfinity, false);
			sg.RemoveEdge(sg.GetEdge(oldHead, gov));
		}

		private static readonly SemgrexPattern[] NamePatterns = new SemgrexPattern[] { SemgrexPattern.Compile("{ner:PERSON}=w1 >compound {}=w2"), SemgrexPattern.Compile("{ner:LOCATION}=w1 >compound {}=w2") };

		private static readonly IPredicate<string> PunctTagFilter = new PennTreebankLanguagePack().PunctuationWordRejectFilter();

		/// <summary>
		/// Looks for NPs that should have the
		/// <c>name</c>
		/// relation and
		/// a) changes the structure such that the leftmost token becomes the head
		/// b) changes the relation from
		/// <c>compound</c>
		/// to
		/// <c>name</c>
		/// .
		/// Requires NER tags.
		/// </summary>
		/// <param name="sg">A semantic graph.</param>
		private static void ProcessNames(SemanticGraph sg)
		{
			if (!UseName)
			{
				return;
			}
			// check whether NER tags are available
			IndexedWord rootToken = sg.GetFirstRoot();
			if (rootToken == null || !rootToken.ContainsKey(typeof(CoreAnnotations.NamedEntityTagAnnotation)))
			{
				return;
			}
			SemanticGraph sgCopy = sg.MakeSoftCopy();
			foreach (SemgrexPattern pattern in NamePatterns)
			{
				SemgrexMatcher matcher = pattern.Matcher(sgCopy);
				IList<IndexedWord> nameParts = new List<IndexedWord>();
				IndexedWord head = null;
				while (matcher.Find())
				{
					IndexedWord w1 = matcher.GetNode("w1");
					IndexedWord w2 = matcher.GetNode("w2");
					if (head != w1)
					{
						if (head != null)
						{
							ProcessNamesHelper(sg, head, nameParts);
							nameParts = new List<IndexedWord>();
						}
						head = w1;
					}
					if (w2.Ner().Equals(w1.Ner()))
					{
						nameParts.Add(w2);
					}
				}
				if (head != null)
				{
					ProcessNamesHelper(sg, head, nameParts);
					sgCopy = sg.MakeSoftCopy();
				}
			}
		}

		private static void ProcessNamesHelper(SemanticGraph sg, IndexedWord oldHead, IList<IndexedWord> nameParts)
		{
			if (nameParts.Count < 1)
			{
				// if the named entity only spans one token, change compound relations
				// to nmod relations to get the right structure for NPs with additional modifiers
				// such as "Mrs. Clinton".
				ICollection<IndexedWord> children = new HashSet<IndexedWord>(sg.GetChildren(oldHead));
				foreach (IndexedWord child in children)
				{
					SemanticGraphEdge oldEdge = sg.GetEdge(oldHead, child);
					if (oldEdge.GetRelation() == UniversalEnglishGrammaticalRelations.CompoundModifier)
					{
						sg.AddEdge(oldHead, child, UniversalEnglishGrammaticalRelations.NominalModifier, oldEdge.GetWeight(), oldEdge.IsExtra());
						sg.RemoveEdge(oldEdge);
					}
				}
				return;
			}
			// sort nameParts
			nameParts.Sort();
			// check whether {nameParts[0], ..., nameParts[n], oldHead} are a contiguous NP
			for (int i = nameParts[0].Index(); i < end; i++)
			{
				IndexedWord node = sg.GetNodeByIndexSafe(i);
				if (node == null)
				{
					return;
				}
				if (!nameParts.Contains(node) && PunctTagFilter.Test(node.Tag()))
				{
					// not in nameParts and not a punctuation mark => not a contiguous NP
					return;
				}
			}
			IndexedWord gov = sg.GetParent(oldHead);
			if (gov == null && !sg.GetRoots().Contains(oldHead))
			{
				return;
			}
			IndexedWord newHead = nameParts[0];
			ICollection<IndexedWord> children_1 = new HashSet<IndexedWord>(sg.GetChildren(oldHead));
			//change structure and relations
			foreach (IndexedWord child_1 in children_1)
			{
				if (child_1 == newHead)
				{
					// make the leftmost word the new head
					if (gov == null)
					{
						sg.GetRoots().Add(newHead);
						sg.GetRoots().Remove(oldHead);
					}
					else
					{
						SemanticGraphEdge oldEdge = sg.GetEdge(gov, oldHead);
						sg.AddEdge(gov, newHead, oldEdge.GetRelation(), oldEdge.GetWeight(), oldEdge.IsExtra());
						sg.RemoveEdge(oldEdge);
					}
					// swap direction of relation between old head and new head and change it to name relation.
					SemanticGraphEdge oldEdge_1 = sg.GetEdge(oldHead, newHead);
					sg.AddEdge(newHead, oldHead, UniversalEnglishGrammaticalRelations.NameModifier, oldEdge_1.GetWeight(), oldEdge_1.IsExtra());
					sg.RemoveEdge(oldEdge_1);
				}
				else
				{
					if (nameParts.Contains(child_1))
					{
						// remove relation between the old head and part of the name
						// and introduce new relation between new head and part of the name
						SemanticGraphEdge oldEdge = sg.GetEdge(oldHead, child_1);
						sg.AddEdge(newHead, child_1, UniversalEnglishGrammaticalRelations.NameModifier, oldEdge.GetWeight(), oldEdge.IsExtra());
						sg.RemoveEdge(oldEdge);
					}
					else
					{
						// attach word to new head
						SemanticGraphEdge oldEdge = sg.GetEdge(oldHead, child_1);
						//if not the entire compound is part of a named entity, attach the other tokens via an nmod relation
						GrammaticalRelation reln = oldEdge.GetRelation() == UniversalEnglishGrammaticalRelations.CompoundModifier ? UniversalEnglishGrammaticalRelations.NominalModifier : oldEdge.GetRelation();
						sg.AddEdge(newHead, child_1, reln, oldEdge.GetWeight(), oldEdge.IsExtra());
						sg.RemoveEdge(oldEdge);
					}
				}
			}
		}

		/// <summary>Find and remove any exact duplicates from a dependency list.</summary>
		/// <remarks>
		/// Find and remove any exact duplicates from a dependency list.
		/// For example, the method that "corrects" nsubj dependencies can
		/// turn them into nsubjpass dependencies.  If there is some other
		/// source of nsubjpass dependencies, there may now be multiple
		/// copies of the nsubjpass dependency.  If the containing data type
		/// is a List, they may both now be in the List.
		/// </remarks>
		private static void RemoveExactDuplicates(SemanticGraph sg)
		{
			sg.DeleteDuplicateEdges();
		}

		/// <exception cref="System.IO.IOException"/>
		public static IList<GrammaticalStructure> ReadCoNLLXGrammaticalStructureCollection(string fileName)
		{
			return ReadCoNLLXGrammaticalStructureCollection(fileName, UniversalEnglishGrammaticalRelations.shortNameToGRel, new UniversalEnglishGrammaticalStructure.FromDependenciesFactory());
		}

		public static UniversalEnglishGrammaticalStructure BuildCoNLLXGrammaticalStructure(IList<IList<string>> tokenFields)
		{
			return (UniversalEnglishGrammaticalStructure)BuildCoNLLXGrammaticalStructure(tokenFields, UniversalEnglishGrammaticalRelations.shortNameToGRel, new UniversalEnglishGrammaticalStructure.FromDependenciesFactory());
		}

		public class FromDependenciesFactory : IGrammaticalStructureFromDependenciesFactory
		{
			public virtual UniversalEnglishGrammaticalStructure Build(IList<TypedDependency> tdeps, TreeGraphNode root)
			{
				return new UniversalEnglishGrammaticalStructure(tdeps, root);
			}
		}
	}
}
