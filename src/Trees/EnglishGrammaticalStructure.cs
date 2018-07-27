using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A GrammaticalStructure for English.</summary>
	/// <remarks>
	/// A GrammaticalStructure for English. This is the class that produces Stanford Dependencies.
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
	[System.Serializable]
	public class EnglishGrammaticalStructure : GrammaticalStructure
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.EnglishGrammaticalStructure));

		private const long serialVersionUID = -1866362375001969402L;

		private static readonly bool Debug = Runtime.GetProperty("EnglishGrammaticalStructure", null) != null;

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
		public EnglishGrammaticalStructure(Tree t)
			: this(t, new PennTreebankLanguagePack().PunctuationWordRejectFilter())
		{
		}

		/// <summary>This gets used by GrammaticalStructureFactory (by reflection).</summary>
		/// <remarks>This gets used by GrammaticalStructureFactory (by reflection). DON'T DELETE.</remarks>
		/// <param name="t">Parse tree to make grammatical structure from</param>
		/// <param name="puncFilter">Filter to remove punctuation dependencies</param>
		public EnglishGrammaticalStructure(Tree t, IPredicate<string> puncFilter)
			: this(t, puncFilter, new SemanticHeadFinder(true))
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
		/// Once upon a time this method had an extra parameter as to whether to operate
		/// in a threadsafe manner. We decided that that was a really bad idea, and this
		/// method now always acts in a threadsafe manner.
		/// This method gets used by GrammaticalStructureFactory (by reflection). DON'T DELETE.
		/// </summary>
		/// <param name="t">Parse tree to make grammatical structure from</param>
		/// <param name="puncFilter">Filter for punctuation words</param>
		/// <param name="hf">HeadFinder to use when building it</param>
		public EnglishGrammaticalStructure(Tree t, IPredicate<string> puncFilter, IHeadFinder hf)
			: base(t, EnglishGrammaticalRelations.Values(), EnglishGrammaticalRelations.ValuesLock(), new CoordinationTransformer(hf), hf, puncFilter, Filters.AcceptFilter())
		{
		}

		/// <summary>Used for postprocessing CoNLL X dependencies</summary>
		public EnglishGrammaticalStructure(IList<TypedDependency> projectiveDependencies, TreeGraphNode root)
			: base(projectiveDependencies, root)
		{
		}

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
				return d != null && d.Reln() != Relative;
			}

			private const long serialVersionUID = 1L;
		}

		private static readonly IPredicate<TypedDependency> extraTreeDepFilter = new EnglishGrammaticalStructure.ExtraTreeDepFilter();

		protected internal override void CorrectDependencies(IList<TypedDependency> list)
		{
			if (Debug)
			{
				PrintListSorted("At correctDependencies:", list);
			}
			CorrectSubjPass(list);
			if (Debug)
			{
				PrintListSorted("After correctSubjPass:", list);
			}
			RemoveExactDuplicates(list);
			if (Debug)
			{
				PrintListSorted("After removeExactDuplicates:", list);
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
			if (Debug)
			{
				PrintListSorted("At postProcessDependencies:", list);
			}
			SemanticGraph sg = new SemanticGraph(list);
			CorrectWHAttachment(sg);
			list.Clear();
			Sharpen.Collections.AddAll(list, sg.TypedDependencies());
			if (Debug)
			{
				PrintListSorted("After correcting WH movement", list);
			}
			ConvertRel(list);
			if (Debug)
			{
				PrintListSorted("After converting rel:", list);
			}
		}

		protected internal override void GetExtras(IList<TypedDependency> list)
		{
			AddRef(list);
			if (Debug)
			{
				PrintListSorted("After adding ref:", list);
			}
			AddExtraNSubj(list);
			if (Debug)
			{
				PrintListSorted("After adding extra nsubj:", list);
			}
			AddStrandedPobj(list);
			if (Debug)
			{
				PrintListSorted("After adding stranded pobj:", list);
			}
		}

		// Using this makes addStrandedPobj a lot cleaner looking, but it
		// makes the converter roughly 2% slower.  Might not be worth it.
		// Similar changes could be made to many of the other complicated
		// collapsing methods.
		// static final SemgrexPattern strandedPobjSemgrex = SemgrexPattern.compile("{}=head >rcmod ({} [ == {}=prepgov | >xcomp {}=prepgov | >conj {}=prepgov ]) : {}=prepgov >prep ({}=prepdep !>pcomp {} !> pobj {})");
		// // Deal with preposition stranding in relative clauses.
		// // For example, "the only thing I'm rooting for"
		// // This method will add pobj(for, thing) by connecting using the rcmod and prep
		// private static void addStrandedPobj(List<TypedDependency> list) {
		//   SemanticGraph graph = new SemanticGraph(list);
		//   SemgrexMatcher matcher = strandedPobjSemgrex.matcher(graph);
		//   while (matcher.find()) {
		//     IndexedWord gov = matcher.getNode("prepdep");
		//     IndexedWord dep = matcher.getNode("head");
		//     TypedDependency newDep = new TypedDependency(PREPOSITIONAL_OBJECT, gov, dep);
		//     newDep.setExtra();
		//     list.add(newDep);
		//   }
		// }
		// Deal with preposition stranding in relative clauses.
		// For example, "the only thing I'm rooting for"
		// This method will add pobj(for, thing) by connecting using the rcmod and prep
		private static void AddStrandedPobj(IList<TypedDependency> list)
		{
			IList<IndexedWord> depNodes = null;
			IList<TypedDependency> newDeps = null;
			foreach (TypedDependency rcmod in list)
			{
				if (rcmod.Reln() != RelativeClauseModifier)
				{
					continue;
				}
				IndexedWord head = rcmod.Gov();
				if (depNodes == null)
				{
					depNodes = Generics.NewArrayList();
				}
				else
				{
					depNodes.Clear();
				}
				depNodes.Add(rcmod.Dep());
				foreach (TypedDependency connected in list)
				{
					if (connected.Gov().Equals(rcmod.Dep()) && (connected.Reln() == XclausalComplement || connected.Reln() == Conjunct))
					{
						depNodes.Add(connected.Dep());
					}
				}
				foreach (IndexedWord dep in depNodes)
				{
					foreach (TypedDependency prep in list)
					{
						if (!prep.Gov().Equals(dep) || prep.Reln() != PrepositionalModifier)
						{
							continue;
						}
						bool found = false;
						foreach (TypedDependency other in list)
						{
							if (other.Gov().Equals(prep.Dep()) && (other.Reln() == PrepositionalComplement || other.Reln() == PrepositionalObject))
							{
								found = true;
								break;
							}
						}
						if (!found)
						{
							if (newDeps == null)
							{
								newDeps = Generics.NewArrayList();
							}
							TypedDependency newDep = new TypedDependency(PrepositionalObject, prep.Dep(), head);
							newDeps.Add(newDep);
						}
					}
				}
			}
			if (newDeps != null)
			{
				Sharpen.Collections.AddAll(list, newDeps);
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
				if (wh.Tag().StartsWith("W"))
				{
					bool reattach = false;
					/* If the control verb already has an object, then
					we have to reattach th WH-word to the verb in the embedded clause. */
					if (dobj != null)
					{
						reattach = true;
					}
					else
					{
						/* If the control verb can't have an object, we also have to reattach. */
						string lemma = Morphology.LemmaStatic(root.Value(), root.Tag());
						if (lemma.Matches(EnglishPatterns.NpVSInfVerbsRegex))
						{
							reattach = true;
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
		/// the type "rel".
		/// </summary>
		/// <remarks>
		/// What we do in this method is look for temporary dependencies of
		/// the type "rel".  These occur in sentences such as "I saw the man
		/// who you love".  In that case, we should produce dobj(love, who).
		/// On the other hand, in the sentence "... which Mr. Bush was
		/// fighting for", we should have pobj(for, which).
		/// </remarks>
		private static void ConvertRel(IList<TypedDependency> list)
		{
			IList<TypedDependency> newDeps = new List<TypedDependency>();
			foreach (TypedDependency rel in list)
			{
				if (rel.Reln() != Relative)
				{
					continue;
				}
				bool foundPrep = false;
				foreach (TypedDependency prep in list)
				{
					// todo: It would also be good to add a rule here to prefer ccomp nsubj over dobj if there is a ccomp with no subj
					// then we could get right: Which eco-friendly options do you think there will be on the new Lexus?
					if (prep.Reln() != PrepositionalModifier)
					{
						continue;
					}
					if (!prep.Gov().Equals(rel.Gov()))
					{
						//Try to find a clausal complement with a preposition without an
						//object. For sentences such as "What am I good at?"
						bool hasCompParent = false;
						foreach (TypedDependency prep2 in list)
						{
							if (prep2.Reln() == XclausalComplement || prep2.Reln() == AdjectivalComplement || prep2.Reln() == ClausalComplement || prep2.Reln() == Root)
							{
								if (prep.Gov().Equals(prep2.Dep()) && prep2.Gov().Equals(rel.Gov()))
								{
									hasCompParent = true;
									break;
								}
							}
						}
						if (!hasCompParent)
						{
							continue;
						}
					}
					// at this point, we have two dependencies as in the Mr. Bush
					// example.  it should be rel(fighting, which) and
					// prep(fighting, for).  We now look to see if there is a
					// corresponding pobj associated with the dependent of the
					// prep relation.  If not, we will connect the dep of the prep
					// relation and the head of the rel relation.  Otherwise, the
					// original rel relation will become a dobj.
					bool foundPobj = false;
					foreach (TypedDependency pobj in list)
					{
						if (pobj.Reln() != PrepositionalObject && pobj.Reln() != PrepositionalComplement)
						{
							continue;
						}
						if (!pobj.Gov().Equals(prep.Dep()))
						{
							continue;
						}
						// we did find a pobj/pcomp, so it is not necessary to
						// change this rel.
						foundPobj = true;
						break;
					}
					if (!foundPobj)
					{
						foundPrep = true;
						TypedDependency newDep = new TypedDependency(PrepositionalObject, prep.Dep(), rel.Dep());
						newDeps.Add(newDep);
						rel.SetReln(Kill);
					}
				}
				// break; // only put it in one place (or do we want to allow across-the-board effects?
				if (!foundPrep)
				{
					rel.SetReln(DirectObject);
				}
			}
			FilterKill(list);
			foreach (TypedDependency dep in newDeps)
			{
				if (!list.Contains(dep))
				{
					list.Add(dep);
				}
			}
		}

		/// <summary>Alters a list in place by removing all the KILL relations</summary>
		private static void FilterKill(ICollection<TypedDependency> deps)
		{
			IList<TypedDependency> filtered = Generics.NewArrayList();
			foreach (TypedDependency dep in deps)
			{
				if (dep.Reln() != Kill)
				{
					filtered.Add(dep);
				}
			}
			deps.Clear();
			Sharpen.Collections.AddAll(deps, filtered);
		}

		/// <summary>
		/// Destructively modifies this
		/// <c>Collection&lt;TypedDependency&gt;</c>
		/// by collapsing several types of transitive pairs of dependencies.
		/// If called with a tree of dependencies and both CCprocess and
		/// includeExtras set to false, then the tree structure is preserved.
		/// <dl>
		/// <dt>prepositional object dependencies: pobj</dt>
		/// <dd>
		/// <c>prep(cat, in)</c>
		/// and
		/// <c>pobj(in, hat)</c>
		/// are collapsed to
		/// <c>prep_in(cat, hat)</c>
		/// </dd>
		/// <dt>prepositional complement dependencies: pcomp</dt>
		/// <dd>
		/// <c>prep(heard, of)</c>
		/// and
		/// <c>pcomp(of, attacking)</c>
		/// are
		/// collapsed to
		/// <c>prepc_of(heard, attacking)</c>
		/// </dd>
		/// <dt>conjunct dependencies</dt>
		/// <dd>
		/// <c>cc(investors, and)</c>
		/// and
		/// <c>conj(investors, regulators)</c>
		/// are collapsed to
		/// <c>conj_and(investors,regulators)</c>
		/// </dd>
		/// <dt>possessive dependencies: possessive</dt>
		/// <dd>
		/// <c>possessive(Montezuma, 's)</c>
		/// will be erased. This is like a collapsing, but
		/// due to the flatness of NPs, two dependencies are not actually composed.</dd>
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
			if (Debug)
			{
				PrintListSorted("collapseDependencies: CCproc: " + CCprocess + " includeExtras: " + includeExtras, list);
			}
			CorrectDependencies(list);
			if (Debug)
			{
				PrintListSorted("After correctDependencies:", list);
			}
			EraseMultiConj(list);
			if (Debug)
			{
				PrintListSorted("After collapse multi conj:", list);
			}
			Collapse2WP(list);
			if (Debug)
			{
				PrintListSorted("After collapse2WP:", list);
			}
			CollapseFlatMWP(list);
			if (Debug)
			{
				PrintListSorted("After collapseFlatMWP:", list);
			}
			Collapse2WPbis(list);
			if (Debug)
			{
				PrintListSorted("After collapse2WPbis:", list);
			}
			Collapse3WP(list);
			if (Debug)
			{
				PrintListSorted("After collapse3WP:", list);
			}
			CollapsePrepAndPoss(list);
			if (Debug)
			{
				PrintListSorted("After PrepAndPoss:", list);
			}
			CollapseConj(list);
			if (Debug)
			{
				PrintListSorted("After conj:", list);
			}
			if (includeExtras.doRef)
			{
				AddRef(list);
				if (Debug)
				{
					PrintListSorted("After adding ref:", list);
				}
				if (includeExtras.collapseRef)
				{
					CollapseReferent(list);
					if (Debug)
					{
						PrintListSorted("After collapse referent:", list);
					}
				}
			}
			if (CCprocess)
			{
				TreatCC(list);
				if (Debug)
				{
					PrintListSorted("After treatCC:", list);
				}
			}
			if (includeExtras.doSubj)
			{
				AddExtraNSubj(list);
				if (Debug)
				{
					PrintListSorted("After adding extra nsubj:", list);
				}
				CorrectSubjPass(list);
				if (Debug)
				{
					PrintListSorted("After correctSubjPass:", list);
				}
			}
			RemoveDep(list);
			if (Debug)
			{
				PrintListSorted("After remove dep:", list);
			}
			list.Sort();
			if (Debug)
			{
				PrintListSorted("After all collapse:", list);
			}
		}

		protected internal override void CollapseDependenciesTree(IList<TypedDependency> list)
		{
			CollapseDependencies(list, false, GrammaticalStructure.Extras.None);
		}

		/// <summary>Does some hard coding to deal with relation in CONJP.</summary>
		/// <remarks>
		/// Does some hard coding to deal with relation in CONJP. For now we deal with:
		/// but not, if not, instead of, rather than, but rather GO TO negcc <br />
		/// as well as, not to mention, but also, &amp; GO TO and.
		/// </remarks>
		/// <param name="conj">The head dependency of the conjunction marker</param>
		/// <returns>
		/// A GrammaticalRelation made from a normalized form of that
		/// conjunction.
		/// </returns>
		private static GrammaticalRelation ConjValue(string conj)
		{
			string newConj = conj.ToLower();
			if (newConj.Equals("not") || newConj.Equals("instead") || newConj.Equals("rather"))
			{
				newConj = "negcc";
			}
			else
			{
				if (newConj.Equals("mention") || newConj.Equals("to") || newConj.Equals("also") || newConj.Contains("well") || newConj.Equals("&"))
				{
					newConj = "and";
				}
			}
			return EnglishGrammaticalRelations.GetConj(newConj);
		}

		private static void TreatCC(ICollection<TypedDependency> list)
		{
			// Construct a map from tree nodes to the set of typed
			// dependencies in which the node appears as dependent.
			IDictionary<IndexedWord, ICollection<TypedDependency>> map = Generics.NewHashMap();
			// Construct a map of tree nodes being governor of a subject grammatical
			// relation to that relation
			IDictionary<IndexedWord, TypedDependency> subjectMap = Generics.NewHashMap();
			// Construct a set of TreeGraphNodes with a passive auxiliary on them
			ICollection<IndexedWord> withPassiveAuxiliary = Generics.NewHashSet();
			// Construct a map of tree nodes being governor of an object grammatical
			// relation to that relation
			// Map<TreeGraphNode, TypedDependency> objectMap = new
			// HashMap<TreeGraphNode, TypedDependency>();
			IList<IndexedWord> rcmodHeads = Generics.NewArrayList();
			IList<IndexedWord> prepcDep = Generics.NewArrayList();
			foreach (TypedDependency typedDep in list)
			{
				if (!map.Contains(typedDep.Dep()))
				{
					// NB: Here and in other places below, we use a TreeSet (which extends
					// SortedSet) to guarantee that results are deterministic)
					map[typedDep.Dep()] = new TreeSet<TypedDependency>();
				}
				map[typedDep.Dep()].Add(typedDep);
				if (typedDep.Reln().Equals(AuxPassiveModifier))
				{
					withPassiveAuxiliary.Add(typedDep.Gov());
				}
				// look for subjects
				if (typedDep.Reln().GetParent() == NominalSubject || typedDep.Reln().GetParent() == Subject || typedDep.Reln().GetParent() == ClausalSubject)
				{
					if (!subjectMap.Contains(typedDep.Gov()))
					{
						subjectMap[typedDep.Gov()] = typedDep;
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
				if (typedDep.Reln() == RelativeClauseModifier)
				{
					rcmodHeads.Add(typedDep.Gov());
				}
				// look for prepc relations: put the dependent of such a relation in the
				// list
				// to avoid wrong propagation of dobj
				if (typedDep.Reln().ToString().StartsWith("prepc"))
				{
					prepcDep.Add(typedDep.Dep());
				}
			}
			// log.info(map);
			// if (DEBUG) log.info("Subject map: " + subjectMap);
			// if (DEBUG) log.info("Object map: " + objectMap);
			// log.info(rcmodHeads);
			// create a new list of typed dependencies
			ICollection<TypedDependency> newTypedDeps = new List<TypedDependency>(list);
			// find typed deps of form conj(gov,dep)
			foreach (TypedDependency td in list)
			{
				if (EnglishGrammaticalRelations.GetConjs().Contains(td.Reln()))
				{
					IndexedWord gov = td.Gov();
					IndexedWord dep = td.Dep();
					// look at the dep in the conjunct
					ICollection<TypedDependency> gov_relations = map[gov];
					// log.info("gov " + gov);
					if (gov_relations != null)
					{
						foreach (TypedDependency td1 in gov_relations)
						{
							// log.info("gov rel " + td1);
							IndexedWord newGov = td1.Gov();
							// in the case of errors in the basic dependencies, it
							// is possible to have overlapping newGov & dep
							if (newGov.Equals(dep))
							{
								continue;
							}
							GrammaticalRelation newRel = td1.Reln();
							if (newRel != Root)
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
										newTypedDeps.Add(new TypedDependency(newRel, newGov, dep));
									}
								}
								else
								{
									if (Debug)
									{
										log.Info("Adding new " + newRel + " dependency from " + newGov + " to " + dep);
									}
									newTypedDeps.Add(new TypedDependency(newRel, newGov, dep));
								}
							}
						}
					}
					// propagate subjects
					// look at the gov in the conjunct: if it is has a subject relation,
					// the dep is a verb and the dep doesn't have a subject relation
					// then we want to add a subject relation for the dep.
					// (By testing for the dep to be a verb, we are going to miss subject of
					// copula verbs! but
					// is it safe to relax this assumption?? i.e., just test for the subject
					// part)
					// CDM 2008: I also added in JJ, since participial verbs are often
					// tagged JJ
					string tag = dep.Tag();
					if (subjectMap.Contains(gov) && (tag.StartsWith("VB") || tag.StartsWith("JJ")) && !subjectMap.Contains(dep))
					{
						TypedDependency tdsubj = subjectMap[gov];
						// check for wrong nsubjpass: if the new verb is VB or VBZ or VBP or JJ, then
						// add nsubj (if it is tagged correctly, should do this for VBD too, but we don't)
						GrammaticalRelation relation = tdsubj.Reln();
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
							log.Info("Adding new " + relation + " dependency from " + dep + " to " + tdsubj.Dep() + " (subj propagation case)");
						}
						newTypedDeps.Add(new TypedDependency(relation, dep, tdsubj.Dep()));
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
			list.Clear();
			Sharpen.Collections.AddAll(list, newTypedDeps);
		}

		private static bool IsDefinitelyActive(string tag)
		{
			// we should include VBD, but don't as it is often a tagging mistake.
			return tag.Equals("VB") || tag.Equals("VBZ") || tag.Equals("VBP") || tag.StartsWith("JJ");
		}

		/// <summary>
		/// This rewrites the "conj" relation to "conj_word" and deletes cases of the
		/// "cc" relation providing this rewrite has occurred (but not if there is only
		/// something like a clause-initial and).
		/// </summary>
		/// <remarks>
		/// This rewrites the "conj" relation to "conj_word" and deletes cases of the
		/// "cc" relation providing this rewrite has occurred (but not if there is only
		/// something like a clause-initial and). For instance, cc(elected-5, and-9)
		/// conj(elected-5, re-elected-11) becomes conj_and(elected-5, re-elected-11)
		/// </remarks>
		/// <param name="list">List of dependencies.</param>
		private static void CollapseConj(ICollection<TypedDependency> list)
		{
			IList<IndexedWord> govs = Generics.NewArrayList();
			// find typed deps of form cc(gov, dep)
			foreach (TypedDependency td in list)
			{
				if (td.Reln() == Coordination)
				{
					// i.e. "cc"
					IndexedWord gov = td.Gov();
					GrammaticalRelation conj = ConjValue(td.Dep().Value());
					if (Debug)
					{
						log.Info("Set conj to " + conj + " based on " + td);
					}
					// find other deps of that gov having reln "conj"
					bool foundOne = false;
					foreach (TypedDependency td1 in list)
					{
						if (td1.Gov().Equals(gov))
						{
							if (td1.Reln() == Conjunct)
							{
								// i.e., "conj"
								// change "conj" to the actual (lexical) conjunction
								if (Debug)
								{
									log.Info("Changing " + td1 + " to have relation " + conj);
								}
								td1.SetReln(conj);
								foundOne = true;
							}
							else
							{
								if (td1.Reln() == Coordination)
								{
									conj = ConjValue(td1.Dep().Value());
									if (Debug)
									{
										log.Info("Set conj to " + conj + " based on " + td1);
									}
								}
							}
						}
					}
					// register to remove cc from this governor
					if (foundOne)
					{
						govs.Add(gov);
					}
				}
			}
			// now remove typed dependencies with reln "cc" if we have successfully
			// collapsed
			list.RemoveIf(null);
		}

		/// <summary>This method will collapse a referent relation such as follows.</summary>
		/// <remarks>
		/// This method will collapse a referent relation such as follows. e.g.:
		/// "The man that I love &hellip; " ref(man, that) dobj(love, that) -&gt;
		/// dobj(love, man)
		/// </remarks>
		private static void CollapseReferent(ICollection<TypedDependency> list)
		{
			// find typed deps of form ref(gov, dep)
			// put them in a List for processing; remove them from the set of deps
			IList<TypedDependency> refs = new List<TypedDependency>();
			for (IEnumerator<TypedDependency> iter = list.GetEnumerator(); iter.MoveNext(); )
			{
				TypedDependency td = iter.Current;
				if (td.Reln() == Referent)
				{
					refs.Add(td);
					iter.Remove();
				}
			}
			// now substitute target of referent where possible
			foreach (TypedDependency @ref in refs)
			{
				IndexedWord dep = @ref.Dep();
				// take the relative word
				IndexedWord ant = @ref.Gov();
				// take the antecedent
				foreach (TypedDependency td in list)
				{
					// the last condition below maybe shouldn't be necessary, but it has
					// helped stop things going haywire a couple of times (it stops the
					// creation of a unit cycle that probably leaves something else
					// disconnected) [cdm Jan 2010]
					if (td.Dep().Equals(dep) && td.Reln() != Referent && !td.Gov().Equals(ant))
					{
						if (Debug)
						{
							log.Info("referent: changing " + td);
						}
						td.SetDep(ant);
						td.SetExtra();
						if (Debug)
						{
							log.Info(" to " + td);
						}
					}
				}
			}
		}

		/// <summary>Look for ref rules for a given word.</summary>
		/// <remarks>
		/// Look for ref rules for a given word.  We look through the
		/// children and grandchildren of the rcmod dependency, and if any
		/// children or grandchildren depend on a that/what/which/etc word,
		/// we take the leftmost that/what/which/etc word as the dependent
		/// for the ref TypedDependency.
		/// </remarks>
		private static void AddRef(ICollection<TypedDependency> list)
		{
			IList<TypedDependency> newDeps = new List<TypedDependency>();
			foreach (TypedDependency rcmod in list)
			{
				if (rcmod.Reln() != RelativeClauseModifier)
				{
					// we only add ref dependencies across relative clauses
					continue;
				}
				IndexedWord head = rcmod.Gov();
				IndexedWord modifier = rcmod.Dep();
				TypedDependency leftChild = null;
				foreach (TypedDependency child in list)
				{
					if (child.Gov().Equals(modifier) && EnglishPatterns.RelativizingWordPattern.Matcher(child.Dep().Value()).Matches() && (leftChild == null || child.Dep().Index() < leftChild.Dep().Index()))
					{
						leftChild = child;
					}
				}
				// TODO: could be made more efficient
				TypedDependency leftGrandchild = null;
				foreach (TypedDependency child_1 in list)
				{
					if (!child_1.Gov().Equals(modifier))
					{
						continue;
					}
					foreach (TypedDependency grandchild in list)
					{
						if (grandchild.Gov().Equals(child_1.Dep()) && EnglishPatterns.RelativizingWordPattern.Matcher(grandchild.Dep().Value()).Matches() && (leftGrandchild == null || grandchild.Dep().Index() < leftGrandchild.Dep().Index()))
						{
							leftGrandchild = grandchild;
						}
					}
				}
				TypedDependency newDep = null;
				if (leftGrandchild != null && (leftChild == null || leftGrandchild.Dep().Index() < leftChild.Dep().Index()))
				{
					newDep = new TypedDependency(Referent, head, leftGrandchild.Dep());
				}
				else
				{
					if (leftChild != null)
					{
						newDep = new TypedDependency(Referent, head, leftChild.Dep());
					}
				}
				if (newDep != null)
				{
					newDeps.Add(newDep);
				}
			}
			foreach (TypedDependency newDep_1 in newDeps)
			{
				if (!list.Contains(newDep_1))
				{
					newDep_1.SetExtra();
					list.Add(newDep_1);
				}
			}
		}

		/// <summary>Add extra nsubj dependencies when collapsing basic dependencies.</summary>
		/// <remarks>
		/// Add extra nsubj dependencies when collapsing basic dependencies.
		/// <br />
		/// In the general case, we look for an aux modifier under an xcomp
		/// modifier, and assuming there aren't already associated nsubj
		/// dependencies as daughters of the original xcomp dependency, we
		/// add nsubj dependencies for each nsubj daughter of the aux.
		/// <br />
		/// There is also a special case for "to" words, in which case we add
		/// a dependency if and only if there is no nsubj associated with the
		/// xcomp and there is no other aux dependency.  This accounts for
		/// sentences such as "he decided not to" with no following verb.
		/// </remarks>
		private static void AddExtraNSubj(ICollection<TypedDependency> list)
		{
			IList<TypedDependency> newDeps = new List<TypedDependency>();
			foreach (TypedDependency xcomp in list)
			{
				if (xcomp.Reln() != XclausalComplement)
				{
					// we only add extra nsubj dependencies to some xcomp dependencies
					continue;
				}
				IndexedWord modifier = xcomp.Dep();
				IndexedWord head = xcomp.Gov();
				bool hasSubjectDaughter = false;
				bool hasAux = false;
				IList<IndexedWord> subjects = Generics.NewArrayList();
				IList<IndexedWord> objects = Generics.NewArrayList();
				foreach (TypedDependency dep in list)
				{
					// already have a subject dependency
					if ((dep.Reln() == NominalSubject || dep.Reln() == NominalPassiveSubject) && dep.Gov().Equals(modifier))
					{
						hasSubjectDaughter = true;
						break;
					}
					if (dep.Reln() == AuxModifier && dep.Gov().Equals(modifier))
					{
						hasAux = true;
					}
					if ((dep.Reln() == NominalSubject || dep.Reln() == NominalPassiveSubject) && dep.Gov().Equals(head))
					{
						subjects.Add(dep.Dep());
					}
					if (dep.Reln() == DirectObject && dep.Gov().Equals(head))
					{
						objects.Add(dep.Dep());
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
				if (objects.Count > 0)
				{
					foreach (IndexedWord @object in objects)
					{
						TypedDependency newDep = new TypedDependency(NominalSubject, modifier, @object);
						newDeps.Add(newDep);
					}
				}
				else
				{
					foreach (IndexedWord subject in subjects)
					{
						TypedDependency newDep = new TypedDependency(NominalSubject, modifier, subject);
						newDeps.Add(newDep);
					}
				}
			}
			foreach (TypedDependency newDep_1 in newDeps)
			{
				if (!list.Contains(newDep_1))
				{
					newDep_1.SetExtra();
					list.Add(newDep_1);
				}
			}
		}

		/// <summary>
		/// This method corrects subjects of verbs for which we identified an auxpass,
		/// but didn't identify the subject as passive.
		/// </summary>
		/// <param name="list">List of typedDependencies to work on</param>
		private static void CorrectSubjPass(ICollection<TypedDependency> list)
		{
			// put in a list verbs having an auxpass
			IList<IndexedWord> list_auxpass = new List<IndexedWord>();
			foreach (TypedDependency td in list)
			{
				if (td.Reln() == AuxPassiveModifier)
				{
					list_auxpass.Add(td.Gov());
				}
			}
			foreach (TypedDependency td_1 in list)
			{
				// correct nsubj
				if (td_1.Reln() == NominalSubject && list_auxpass.Contains(td_1.Gov()))
				{
					// log.info("%%% Changing subj to passive: " + td);
					td_1.SetReln(NominalPassiveSubject);
				}
				if (td_1.Reln() == ClausalSubject && list_auxpass.Contains(td_1.Gov()))
				{
					// log.info("%%% Changing subj to passive: " + td);
					td_1.SetReln(ClausalPassiveSubject);
				}
			}
		}

		// correct unretrieved poss: dep relation in which the dependent is a
		// PRP$ or WP$
		// cdm: Now done in basic rules.  The only cases that this still matches
		// are (1) tagging mistakes where PRP in dobj position is mistagged PRP$
		// or a couple of parsing errors where the dependency is wrong anyway, so
		// it's probably okay to keep it a dep.  So I'm disabling this.
		// String tag = td.dep().tag();
		// if (td.reln() == DEPENDENT && (tag.equals("PRP$") || tag.equals("WP$"))) {
		//  log.info("%%% Unrecognized basic possessive pronoun: " + td);
		//  td.setReln(POSSESSION_MODIFIER);
		// }
		private static bool InConjDeps(TypedDependency td, IList<Triple<TypedDependency, TypedDependency, bool>> conjs)
		{
			foreach (Triple<TypedDependency, TypedDependency, bool> trip in conjs)
			{
				if (td.Equals(trip.First()))
				{
					return true;
				}
			}
			return false;
		}

		private static void CollapsePrepAndPoss(ICollection<TypedDependency> list)
		{
			// Man oh man, how gnarly is the logic of this method....
			ICollection<TypedDependency> newTypedDeps = new List<TypedDependency>();
			// Construct a map from tree nodes to the set of typed
			// dependencies in which the node appears as governor.
			// cdm: could use CollectionValuedMap here!
			IDictionary<IndexedWord, ISortedSet<TypedDependency>> map = Generics.NewHashMap();
			IList<IndexedWord> vmod = Generics.NewArrayList();
			foreach (TypedDependency typedDep in list)
			{
				if (!map.Contains(typedDep.Gov()))
				{
					map[typedDep.Gov()] = new TreeSet<TypedDependency>();
				}
				map[typedDep.Gov()].Add(typedDep);
				if (typedDep.Reln() == VerbalModifier)
				{
					// look for aux deps which indicate this was a to-be verb
					bool foundAux = false;
					foreach (TypedDependency auxDep in list)
					{
						if (auxDep.Reln() != AuxModifier)
						{
							continue;
						}
						if (!auxDep.Gov().Equals(typedDep.Dep()) || !Sharpen.Runtime.EqualsIgnoreCase(auxDep.Dep().Value(), "to"))
						{
							continue;
						}
						foundAux = true;
						break;
					}
					if (!foundAux)
					{
						vmod.Add(typedDep.Dep());
					}
				}
			}
			// log.info("here's the vmod list: " + vmod);
			// Do preposition conjunction interaction for
			// governor p NP and p NP case ... a lot of special code cdm jan 2006
			foreach (TypedDependency td1 in list)
			{
				if (td1.Reln() != PrepositionalModifier)
				{
					continue;
				}
				IndexedWord td1Dep = td1.Dep();
				ISortedSet<TypedDependency> possibles = map[td1Dep];
				if (possibles == null)
				{
					continue;
				}
				// look for the "second half"
				// unique: the head prep and whether it should be pobj
				Pair<TypedDependency, bool> prepDep = null;
				TypedDependency ccDep = null;
				// treat as unique
				// list of dep and prepOtherDep and pobj (or pcomp)
				IList<Triple<TypedDependency, TypedDependency, bool>> conjs = new List<Triple<TypedDependency, TypedDependency, bool>>();
				ICollection<TypedDependency> otherDtrs = new TreeSet<TypedDependency>();
				// first look for a conj(prep, prep) (there might be several conj relations!!!)
				bool samePrepositionInEachConjunct = true;
				int conjIndex = -1;
				foreach (TypedDependency td2 in possibles)
				{
					if (td2.Reln() == Conjunct)
					{
						IndexedWord td2Dep = td2.Dep();
						string td2DepPOS = td2Dep.Tag();
						if (td2DepPOS.Equals("IN") || td2DepPOS.Equals("TO"))
						{
							samePrepositionInEachConjunct = samePrepositionInEachConjunct && td2Dep.Value().Equals(td1Dep.Value());
							ICollection<TypedDependency> possibles2 = map[td2Dep];
							bool pobj = true;
							// default of collapsing preposition is prep_
							TypedDependency prepOtherDep = null;
							if (possibles2 != null)
							{
								foreach (TypedDependency td3 in possibles2)
								{
									IndexedWord td3Dep = td3.Dep();
									string td3DepPOS = td3Dep.Tag();
									// CDM Mar 2006: I put in disjunction here when I added in
									// PREPOSITIONAL_OBJECT. If it catches all cases, we should
									// be able to delete the DEPENDENT disjunct
									// maybe better to delete the DEPENDENT disjunct - it creates
									// problem with multiple prep (mcdm)
									if ((td3.Reln() == PrepositionalObject || td3.Reln() == PrepositionalComplement) && (!(td3DepPOS.Equals("IN") || td3DepPOS.Equals("TO"))) && prepOtherDep == null)
									{
										prepOtherDep = td3;
										if (td3.Reln() == PrepositionalComplement)
										{
											pobj = false;
										}
									}
									else
									{
										otherDtrs.Add(td3);
									}
								}
							}
							if (conjIndex < td2Dep.Index())
							{
								conjIndex = td2Dep.Index();
							}
							conjs.Add(new Triple<TypedDependency, TypedDependency, bool>(td2, prepOtherDep, pobj));
						}
					}
				}
				// end td2:possibles
				if (conjs.IsEmpty())
				{
					continue;
				}
				// if we have a conj under a preposition dependency, we look for the other
				// parts
				string td1DepPOS = td1Dep.Tag();
				foreach (TypedDependency td2_1 in possibles)
				{
					// we look for the cc linked to this conjDep
					// the cc dep must have an index smaller than the dep of conjDep
					if (td2_1.Reln() == Coordination && td2_1.Dep().Index() < conjIndex)
					{
						ccDep = td2_1;
					}
					else
					{
						IndexedWord td2Dep = td2_1.Dep();
						string td2DepPOS = td2Dep.Tag();
						// log.info("prepDep find: td1.reln: " + td1.reln() +
						// "; td2.reln: " + td2.reln() + "; td1DepPos: " + td1DepPOS +
						// "; td2DepPos: " + td2DepPOS + "; index " + index +
						// "; td2.dep().index(): " + td2.dep().index());
						if ((td2_1.Reln() == Dependent || td2_1.Reln() == PrepositionalObject || td2_1.Reln() == PrepositionalComplement) && (td1DepPOS.Equals("IN") || td1DepPOS.Equals("TO") || td1DepPOS.Equals("VBG")) && prepDep == null && (!(td2DepPOS.Equals("RB"
							) || td2DepPOS.Equals("IN") || td2DepPOS.Equals("TO"))))
						{
							// same index trick, in case we have multiple deps
							// I deleted this to see if it helped [cdm Jan 2010] &&
							// td2.dep().index() < index)
							prepDep = new Pair<TypedDependency, bool>(td2_1, td2_1.Reln() != PrepositionalComplement);
						}
						else
						{
							if (!InConjDeps(td2_1, conjs))
							{
								// don't want to add the conjDep
								// again!
								otherDtrs.Add(td2_1);
							}
						}
					}
				}
				if (prepDep == null || ccDep == null)
				{
					continue;
				}
				// we can't deal with it in the hairy prep/conj interaction case!
				if (Debug)
				{
					// ccDep must be non-null given test above
					log.Info("!! Conj and prep case:");
					log.Info("  td1 (prep): " + td1);
					log.Info("  Kids of td1 are: " + possibles);
					log.Info("  prepDep: " + prepDep);
					log.Info("  ccDep: " + ccDep);
					log.Info("  conjs: " + conjs);
					log.Info("  samePrepositionInEachConjunct: " + samePrepositionInEachConjunct);
					log.Info("  otherDtrs: " + otherDtrs);
				}
				// check if we have the same prepositions in the conjunction
				if (samePrepositionInEachConjunct)
				{
					// conjDep != null && prepOtherDep !=
					// null &&
					// OK, we have a conjunction over parallel PPs: Fred flew to Greece and
					// to Serbia.
					GrammaticalRelation reln = DeterminePrepRelation(map, vmod, td1, td1, prepDep.Second());
					TypedDependency tdNew = new TypedDependency(reln, td1.Gov(), prepDep.First().Dep());
					newTypedDeps.Add(tdNew);
					if (Debug)
					{
						log.Info("PrepPoss Conj branch (two parallel PPs) adding: " + tdNew);
						log.Info("  removing: " + td1 + "  " + prepDep + "  " + ccDep);
					}
					td1.SetReln(Kill);
					// remember these are "used up"
					prepDep.First().SetReln(Kill);
					ccDep.SetReln(Kill);
					foreach (Triple<TypedDependency, TypedDependency, bool> trip in conjs)
					{
						TypedDependency conjDep = trip.First();
						TypedDependency prepOtherDep = trip.Second();
						if (prepOtherDep == null)
						{
							// CDM July 2010: I think this should only ever happen if there is a
							// misparse, but it has happened in such circumstances. You have
							// something like (PP in or in (NP Serbia)), with the two
							// prepositions the same. We just clean up the mess.
							if (Debug)
							{
								log.Info("  apparent misparse: same P twice with only one NP object (prepOtherDep is null)");
								log.Info("  removing: " + conjDep);
							}
							ccDep.SetReln(Kill);
						}
						else
						{
							TypedDependency tdNew2 = new TypedDependency(ConjValue(ccDep.Dep().Value()), prepDep.First().Dep(), prepOtherDep.Dep());
							newTypedDeps.Add(tdNew2);
							if (Debug)
							{
								log.Info("  adding: " + tdNew2);
								log.Info("  removing: " + conjDep + "  " + prepOtherDep);
							}
							prepOtherDep.SetReln(Kill);
						}
						conjDep.SetReln(Kill);
					}
					// promote dtrs that would be orphaned
					foreach (TypedDependency otd in otherDtrs)
					{
						if (Debug)
						{
							log.Info("Changed " + otd);
						}
						otd.SetGov(td1.Gov());
						if (Debug)
						{
							log.Info(" to " + otd);
						}
					}
					// Now we need to see if there are any TDs that will be "orphaned"
					// by this collapse. Example: if we have:
					// dep(drew, on)
					// dep(on, book)
					// dep(on, right)
					// the first two will be collapsed to on(drew, book), but then
					// the third one will be orphaned, since its governor no
					// longer appears. So, change its governor to 'drew'.
					// CDM Feb 2010: This used to not move COORDINATION OR CONJUNCT, but now
					// it does, since they're not automatically deleted
					// Some things in possibles may have already been changed, so check gov
					if (Debug)
					{
						log.Info("td1: " + td1 + "; possibles: " + possibles);
					}
					foreach (TypedDependency td2_2 in possibles)
					{
						// if (DEBUG) {
						// log.info("[a] td2.reln " + td2.reln() + " td2.gov " +
						// td2.gov() + " td1.dep " + td1.dep());
						// }
						if (td2_2.Reln() != Kill && td2_2.Gov().Equals(td1.Dep()))
						{
							// && td2.reln()
							// != COORDINATION
							// && td2.reln()
							// != CONJUNCT
							if (Debug)
							{
								log.Info("Changing " + td2_2 + " to have governor of " + td1 + " [a]");
							}
							td2_2.SetGov(td1.Gov());
						}
					}
					continue;
				}
				// This one has been dealt with successfully
				// end same prepositions
				// case of "Lufthansa flies to and from Serbia". Make it look like next
				// case :-)
				// that is, the prepOtherDep should be the same as prepDep !
				foreach (Triple<TypedDependency, TypedDependency, bool> trip_1 in conjs)
				{
					if (trip_1.First() != null && trip_1.Second() == null)
					{
						trip_1.SetSecond(new TypedDependency(prepDep.First().Reln(), trip_1.First().Dep(), prepDep.First().Dep()));
						trip_1.SetThird(prepDep.Second());
					}
				}
				// we have two different prepositions in the conjunction
				// in this case we need to add a node
				// "Bill jumped over the fence and through the hoop"
				// prep_over(jumped, fence)
				// conj_and(jumped, jumped)
				// prep_through(jumped, hoop)
				// Extra complication:
				// If "jumped" is already part of a conjunction, we should add the new one off that rather than chaining
				IndexedWord conjHead = td1.Gov();
				foreach (TypedDependency td3_1 in list)
				{
					if (td3_1.Dep().Equals(td1.Gov()) && td3_1.Reln().Equals(Conjunct))
					{
						conjHead = td3_1.Gov();
					}
				}
				GrammaticalRelation reln_1 = DeterminePrepRelation(map, vmod, td1, td1, prepDep.Second());
				TypedDependency tdNew_1 = new TypedDependency(reln_1, td1.Gov(), prepDep.First().Dep());
				newTypedDeps.Add(tdNew_1);
				if (Debug)
				{
					log.Info("ConjPP (different preps) adding: " + tdNew_1);
					log.Info("  deleting: " + td1 + "  " + prepDep.First() + "  " + ccDep);
				}
				td1.SetReln(Kill);
				// remember these are "used up"
				prepDep.First().SetReln(Kill);
				ccDep.SetReln(Kill);
				// so far we added the first prep grammatical relation
				int copyNumber = 1;
				foreach (Triple<TypedDependency, TypedDependency, bool> trip_2 in conjs)
				{
					TypedDependency conjDep = trip_2.First();
					TypedDependency prepOtherDep = trip_2.Second();
					bool pobj = trip_2.Third();
					// OK, we have a conjunction over different PPs
					// we create a new node;
					// in order to make a distinction between the original node and its copy
					// we set the "copyCount" variable in the IndexedWord
					// existence of copyCount > 0 is checked at printing (toString method of
					// TypedDependency)
					IndexedWord label = td1.Gov().MakeSoftCopy(copyNumber);
					copyNumber++;
					// now we add the conjunction relation between conjHead (either td1.gov
					// or what it is itself conjoined with) and the copy
					// the copy has the same label as td1.gov() but is another TreeGraphNode
					// todo: Or that's the plan; there are a couple of knock on changes to fix before we can do this!
					// TypedDependency tdNew2 = new TypedDependency(conjValue(ccDep.dep().value()), conjHead, label);
					TypedDependency tdNew2 = new TypedDependency(ConjValue(ccDep.Dep().Value()), td1.Gov(), label);
					newTypedDeps.Add(tdNew2);
					// now we still need to add the second prep grammatical relation
					// between the copy and the dependent of the prepOtherDep node
					TypedDependency tdNew3;
					GrammaticalRelation reln2 = DeterminePrepRelation(map, vmod, conjDep, td1, pobj);
					tdNew3 = new TypedDependency(reln2, label, prepOtherDep.Dep());
					newTypedDeps.Add(tdNew3);
					if (Debug)
					{
						log.Info("  adding: " + tdNew2 + "  " + tdNew3);
						log.Info("  deleting: " + conjDep + "  " + prepOtherDep);
					}
					conjDep.SetReln(Kill);
					prepOtherDep.SetReln(Kill);
					// promote dtrs that would be orphaned
					foreach (TypedDependency otd in otherDtrs)
					{
						// special treatment for prepositions: the original relation is
						// likely to be a "dep" and we want this to be a "prep"
						if (otd.Dep().Tag().Equals("IN"))
						{
							otd.SetReln(PrepositionalModifier);
						}
						otd.SetGov(td1.Gov());
					}
				}
				// Now we need to see if there are any TDs that will be "orphaned" off
				// the first preposition
				// by this collapse. Example: if we have:
				// dep(drew, on)
				// dep(on, book)
				// dep(on, right)
				// the first two will be collapsed to on(drew, book), but then
				// the third one will be orphaned, since its governor no
				// longer appears. So, change its governor to 'drew'.
				// CDM Feb 2010: This used to not move COORDINATION OR CONJUNCT, but now
				// it does, since they're not automatically deleted
				foreach (TypedDependency td2_3 in possibles)
				{
					if (td2_3.Reln() != Kill)
					{
						// && td2.reln() != COORDINATION &&
						// td2.reln() != CONJUNCT) {
						if (Debug)
						{
							log.Info("Changing " + td2_3 + " to have governor of " + td1 + " [b]");
						}
						td2_3.SetGov(td1.Gov());
					}
				}
			}
			// end for different prepositions
			// for TypedDependency td1 : list
			// below here is the single preposition/possessor basic case!!
			foreach (TypedDependency td1_1 in list)
			{
				if (td1_1.Reln() == Kill)
				{
					continue;
				}
				IndexedWord td1Dep = td1_1.Dep();
				string td1DepPOS = td1Dep.Tag();
				// find all other typedDeps having our dep as gov
				ICollection<TypedDependency> possibles = map[td1Dep];
				if (possibles != null && (td1_1.Reln() == PrepositionalModifier || td1_1.Reln() == PossessionModifier || td1_1.Reln() == Conjunct))
				{
					// look for the "second half"
					bool pobj = true;
					// default for prep relation is prep_
					foreach (TypedDependency td2 in possibles)
					{
						if (td2.Reln() != Coordination && td2.Reln() != Conjunct)
						{
							IndexedWord td2Dep = td2.Dep();
							string td2DepPOS = td2Dep.Tag();
							if ((td1_1.Reln() == PossessionModifier || td1_1.Reln() == Conjunct))
							{
								if (td2.Reln() == PossessiveModifier)
								{
									if (!map.Contains(td2Dep))
									{
										// if 's has no kids of its own (it shouldn't!)
										td2.SetReln(Kill);
									}
								}
							}
							else
							{
								if ((td2.Reln() == PrepositionalObject || td2.Reln() == PrepositionalComplement) && (td1DepPOS.Equals("IN") || td1DepPOS.Equals("TO") || td1DepPOS.Equals("VBG")) && (!(td2DepPOS.Equals("RB") || td2DepPOS.Equals("IN") || td2DepPOS.Equals("TO"
									))) && !IsConjWithNoPrep(td2.Gov(), possibles))
								{
									// we don't collapse preposition conjoined with a non-preposition
									// to avoid disconnected constituents
									// OK, we have a pair td1, td2 to collapse to td3
									if (Debug)
									{
										log.Info("(Single prep/poss base case collapsing " + td1_1 + " and " + td2);
									}
									// check whether we are in a pcomp case:
									if (td2.Reln() == PrepositionalComplement)
									{
										pobj = false;
									}
									GrammaticalRelation reln = DeterminePrepRelation(map, vmod, td1_1, td1_1, pobj);
									TypedDependency td3 = new TypedDependency(reln, td1_1.Gov(), td2.Dep());
									if (Debug)
									{
										log.Info("PP adding: " + td3 + " deleting: " + td1_1 + ' ' + td2);
									}
									// add it to map to deal with recursive cases like "achieved this (PP (PP in part) with talent)"
									map[td3.Gov()].Add(td3);
									newTypedDeps.Add(td3);
									td1_1.SetReln(Kill);
									// remember these are "used up"
									td2.SetReln(Kill);
								}
							}
						}
					}
				}
				// remember these are "used up"
				// for TypedDependency td2
				// Now we need to see if there are any TDs that will be "orphaned"
				// by this collapse. Example: if we have:
				// dep(drew, on)
				// dep(on, book)
				// dep(on, right)
				// the first two will be collapsed to on(drew, book), but then
				// the third one will be orphaned, since its governor no
				// longer appears. So, change its governor to 'drew'.
				// CDM Feb 2010: This used to not move COORDINATION OR CONJUNCT, but now
				// it does, since they're not automatically deleted
				if (possibles != null && td1_1.Reln() == Kill)
				{
					foreach (TypedDependency td2 in possibles)
					{
						if (td2.Reln() != Kill)
						{
							// && td2.reln() != COORDINATION &&
							// td2.reln() != CONJUNCT) {
							if (Debug)
							{
								log.Info("Changing " + td2 + " to have governor of " + td1_1 + " [c]");
							}
							td2.SetGov(td1_1.Gov());
						}
					}
				}
			}
			// for TypedDependency td1
			// now remove typed dependencies with reln "kill" and add new ones.
			for (IEnumerator<TypedDependency> iter = list.GetEnumerator(); iter.MoveNext(); )
			{
				TypedDependency td = iter.Current;
				if (td.Reln() == Kill)
				{
					if (Debug)
					{
						log.Info("Removing dep killed in poss/prep (conj) collapse: " + td);
					}
					iter.Remove();
				}
			}
			Sharpen.Collections.AddAll(list, newTypedDeps);
		}

		// end collapsePrepAndPoss()
		/// <summary>Work out prep relation name.</summary>
		/// <remarks>
		/// Work out prep relation name. pc is the dependency whose dep() is the
		/// preposition to do a name for. topPrep may be the same or different.
		/// Among the daughters of its gov is where to look for an auxpass.
		/// </remarks>
		private static GrammaticalRelation DeterminePrepRelation(IDictionary<IndexedWord, ICollection<TypedDependency>> map, IList<IndexedWord> vmod, TypedDependency pc, TypedDependency topPrep, bool pobj)
		{
			// handling the case of an "agent":
			// the governor of a "by" preposition must have an "auxpass" dependency
			// or be the dependent of a "vmod" relation
			// if it is the case, the "agent" variable becomes true
			bool agent = false;
			string preposition = pc.Dep().Value().ToLower();
			if (preposition.Equals("by"))
			{
				// look if we have an auxpass
				ICollection<TypedDependency> aux_pass_poss = map[topPrep.Gov()];
				if (aux_pass_poss != null)
				{
					foreach (TypedDependency td_pass in aux_pass_poss)
					{
						if (td_pass.Reln() == AuxPassiveModifier)
						{
							agent = true;
						}
					}
				}
				// look if we have a vmod
				if (!vmod.IsEmpty() && vmod.Contains(topPrep.Gov()))
				{
					agent = true;
				}
			}
			GrammaticalRelation reln;
			if (agent)
			{
				reln = Agent;
			}
			else
			{
				// for prepositions, use the preposition
				// for pobj: we collapse into "prep"; for pcomp: we collapse into "prepc"
				if (pobj)
				{
					reln = EnglishGrammaticalRelations.GetPrep(preposition);
				}
				else
				{
					reln = EnglishGrammaticalRelations.GetPrepC(preposition);
				}
			}
			return reln;
		}

		private static readonly string[][] MultiwordPreps = new string[][] { new string[] { "according", "to" }, new string[] { "across", "from" }, new string[] { "ahead", "of" }, new string[] { "along", "with" }, new string[] { "alongside", "of" }, 
			new string[] { "apart", "from" }, new string[] { "as", "for" }, new string[] { "as", "from" }, new string[] { "as", "of" }, new string[] { "as", "per" }, new string[] { "as", "to" }, new string[] { "aside", "from" }, new string[] { "away", 
			"from" }, new string[] { "based", "on" }, new string[] { "because", "of" }, new string[] { "close", "by" }, new string[] { "close", "to" }, new string[] { "contrary", "to" }, new string[] { "compared", "to" }, new string[] { "compared", "with"
			 }, new string[] { "due", "to" }, new string[] { "depending", "on" }, new string[] { "except", "for" }, new string[] { "exclusive", "of" }, new string[] { "far", "from" }, new string[] { "followed", "by" }, new string[] { "inside", "of" }, 
			new string[] { "instead", "of" }, new string[] { "irrespective", "of" }, new string[] { "next", "to" }, new string[] { "near", "to" }, new string[] { "off", "of" }, new string[] { "out", "of" }, new string[] { "outside", "of" }, new string[
			] { "owing", "to" }, new string[] { "preliminary", "to" }, new string[] { "preparatory", "to" }, new string[] { "previous", "to" }, new string[] { "prior", "to" }, new string[] { "pursuant", "to" }, new string[] { "regardless", "of" }, new 
			string[] { "subsequent", "to" }, new string[] { "such", "as" }, new string[] { "thanks", "to" }, new string[] { "together", "with" } };

		private static readonly string[][] ThreewordPreps = new string[][] { new string[] { "by", "means", "of" }, new string[] { "in", "accordance", "with" }, new string[] { "in", "addition", "to" }, new string[] { "in", "case", "of" }, new string[
			] { "in", "front", "of" }, new string[] { "in", "lieu", "of" }, new string[] { "in", "place", "of" }, new string[] { "in", "spite", "of" }, new string[] { "on", "account", "of" }, new string[] { "on", "behalf", "of" }, new string[] { "on", 
			"top", "of" }, new string[] { "with", "regard", "to" }, new string[] { "with", "respect", "to" } };

		// used by collapse2WP(), collapseFlatMWP(), collapse2WPbis() KEPT IN
		// ALPHABETICAL ORDER
		// used by collapse3WP() KEPT IN ALPHABETICAL ORDER
		/// <summary>
		/// Given a list of typedDependencies, returns true if the node "node" is the
		/// governor of a conj relation with a dependent which is not a preposition
		/// </summary>
		/// <param name="node">A node in this GrammaticalStructure</param>
		/// <param name="list">A list of typedDependencies</param>
		/// <returns>
		/// true If node is the governor of a conj relation in the list with
		/// the dep not being a preposition
		/// </returns>
		private static bool IsConjWithNoPrep(IndexedWord node, ICollection<TypedDependency> list)
		{
			foreach (TypedDependency td in list)
			{
				if (td.Gov().Equals(node) && td.Reln() == Conjunct)
				{
					// we have a conjunct
					// check the POS of the dependent
					string tdDepPOS = td.Dep().Tag();
					if (!(tdDepPOS.Equals("IN") || tdDepPOS.Equals("TO")))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Collapse multiword preposition of the following format:
		/// prep|advmod|dep|amod(gov, mwp[0]) <br/>
		/// dep(mpw[0],mwp[1]) <br/>
		/// pobj|pcomp(mwp[1], compl) or pobj|pcomp(mwp[0], compl) <br/>
		/// -&gt; prep_mwp[0]_mwp[1](gov, compl) <br/>
		/// prep|advmod|dep|amod(gov, mwp[1]) <br/>
		/// dep(mpw[1],mwp[0]) <br/>
		/// pobj|pcomp(mwp[1], compl) or pobj|pcomp(mwp[0], compl) <br/>
		/// -&gt; prep_mwp[0]_mwp[1](gov, compl)
		/// <p/>
		/// The collapsing has to be done at once in order to know exactly which node
		/// is the gov and the dep of the multiword preposition.
		/// </summary>
		/// <remarks>
		/// Collapse multiword preposition of the following format:
		/// prep|advmod|dep|amod(gov, mwp[0]) <br/>
		/// dep(mpw[0],mwp[1]) <br/>
		/// pobj|pcomp(mwp[1], compl) or pobj|pcomp(mwp[0], compl) <br/>
		/// -&gt; prep_mwp[0]_mwp[1](gov, compl) <br/>
		/// prep|advmod|dep|amod(gov, mwp[1]) <br/>
		/// dep(mpw[1],mwp[0]) <br/>
		/// pobj|pcomp(mwp[1], compl) or pobj|pcomp(mwp[0], compl) <br/>
		/// -&gt; prep_mwp[0]_mwp[1](gov, compl)
		/// <p/>
		/// The collapsing has to be done at once in order to know exactly which node
		/// is the gov and the dep of the multiword preposition. Otherwise this can
		/// lead to problems: removing a non-multiword "to" preposition for example!!!
		/// This method replaces the old "collapsedMultiWordPreps"
		/// </remarks>
		/// <param name="list">list of typedDependencies to work on</param>
		private static void Collapse2WP(ICollection<TypedDependency> list)
		{
			ICollection<TypedDependency> newTypedDeps = new List<TypedDependency>();
			foreach (string[] mwp in MultiwordPreps)
			{
				// first look for patterns such as:
				// X(gov, mwp[0])
				// Y(mpw[0],mwp[1])
				// Z(mwp[1], compl) or Z(mwp[0], compl)
				// -> prep_mwp[0]_mwp[1](gov, compl)
				CollapseMultiWordPrep(list, newTypedDeps, mwp[0], mwp[1], mwp[0], mwp[1]);
				// now look for patterns such as:
				// X(gov, mwp[1])
				// Y(mpw[1],mwp[0])
				// Z(mwp[1], compl) or Z(mwp[0], compl)
				// -> prep_mwp[0]_mwp[1](gov, compl)
				CollapseMultiWordPrep(list, newTypedDeps, mwp[0], mwp[1], mwp[1], mwp[0]);
			}
		}

		/// <summary>
		/// Collapse multiword preposition of the following format:
		/// prep|advmod|dep|amod(gov, mwp0) dep(mpw0,mwp1) pobj|pcomp(mwp1, compl) or
		/// pobj|pcomp(mwp0, compl) -&gt; prep_mwp0_mwp1(gov, compl)
		/// <p/>
		/// </summary>
		/// <param name="list">List of typedDependencies to work on,</param>
		/// <param name="newTypedDeps">List of typedDependencies that we construct</param>
		/// <param name="str_mwp0">
		/// First part of the multiword preposition to construct the collapsed
		/// preposition
		/// </param>
		/// <param name="str_mwp1">
		/// Second part of the multiword preposition to construct the
		/// collapsed preposition
		/// </param>
		/// <param name="w_mwp0">First part of the multiword preposition that we look for</param>
		/// <param name="w_mwp1">Second part of the multiword preposition that we look for</param>
		private static void CollapseMultiWordPrep(ICollection<TypedDependency> list, ICollection<TypedDependency> newTypedDeps, string str_mwp0, string str_mwp1, string w_mwp0, string w_mwp1)
		{
			// first find the multiword_preposition: dep(mpw[0], mwp[1])
			// the two words should be next to another in the sentence (difference of
			// indexes = 1)
			IndexedWord mwp0 = null;
			IndexedWord mwp1 = null;
			TypedDependency dep = null;
			foreach (TypedDependency td in list)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(td.Gov().Value(), w_mwp0) && Sharpen.Runtime.EqualsIgnoreCase(td.Dep().Value(), w_mwp1) && Math.Abs(td.Gov().Index() - td.Dep().Index()) == 1)
				{
					mwp0 = td.Gov();
					mwp1 = td.Dep();
					dep = td;
				}
			}
			if (mwp0 == null)
			{
				return;
			}
			// now search for prep|advmod|dep|amod(gov, mwp0)
			IndexedWord governor = null;
			TypedDependency prep = null;
			foreach (TypedDependency td1 in list)
			{
				if ((td1.Reln() == PrepositionalModifier || td1.Reln() == AdverbialModifier || td1.Reln() == AdjectivalModifier || td1.Reln() == Dependent || td1.Reln() == MultiWordExpression) && td1.Dep().Equals(mwp0))
				{
					// we found prep|advmod|dep|amod(gov, mwp0)
					prep = td1;
					governor = prep.Gov();
				}
			}
			if (prep == null)
			{
				return;
			}
			// search for the complement: pobj|pcomp(mwp1,X)
			// or for pobj|pcomp(mwp0,X)
			// There may be more than one in weird constructions; if there are several,
			// take the one with the LOWEST index!
			TypedDependency pobj = null;
			TypedDependency newtd = null;
			foreach (TypedDependency td2 in list)
			{
				if ((td2.Reln() == PrepositionalObject || td2.Reln() == PrepositionalComplement) && (td2.Gov().Equals(mwp1) || td2.Gov().Equals(mwp0)))
				{
					if (pobj == null || pobj.Dep().Index() > td2.Dep().Index())
					{
						pobj = td2;
						// create the new gr relation
						GrammaticalRelation gr;
						if (td2.Reln() == PrepositionalComplement)
						{
							gr = EnglishGrammaticalRelations.GetPrepC(str_mwp0 + '_' + str_mwp1);
						}
						else
						{
							gr = EnglishGrammaticalRelations.GetPrep(str_mwp0 + '_' + str_mwp1);
						}
						if (governor != null)
						{
							newtd = new TypedDependency(gr, governor, pobj.Dep());
						}
					}
				}
			}
			if (pobj == null || newtd == null)
			{
				return;
			}
			// only if we found the three parts, set to KILL and remove
			// and add the new one
			// Necessarily from the above: prep != null, dep != null, pobj != null, newtd != null
			if (Debug)
			{
				log.Info("Removing " + prep + ", " + dep + ", and " + pobj);
				log.Info("  and adding " + newtd);
			}
			prep.SetReln(Kill);
			dep.SetReln(Kill);
			pobj.SetReln(Kill);
			newTypedDeps.Add(newtd);
			// now remove typed dependencies with reln "kill"
			// and promote possible orphans
			foreach (TypedDependency td1_1 in list)
			{
				if (td1_1.Reln() != Kill)
				{
					if (td1_1.Gov().Equals(mwp0) || td1_1.Gov().Equals(mwp1))
					{
						// CDM: Thought of adding this in Jan 2010, but it causes
						// conflicting relations tmod vs. pobj. Needs more thought
						// maybe restrict pobj to first NP in PP, and allow tmod for a later
						// one?
						if (td1_1.Reln() == TemporalModifier)
						{
							// special case when an extra NP-TMP is buried in a PP for
							// "during the same period last year"
							td1_1.SetGov(pobj.Dep());
						}
						else
						{
							td1_1.SetGov(governor);
						}
					}
					if (!newTypedDeps.Contains(td1_1))
					{
						newTypedDeps.Add(td1_1);
					}
				}
			}
			list.Clear();
			Sharpen.Collections.AddAll(list, newTypedDeps);
		}

		/// <summary>
		/// Collapse multi-words preposition of the following format: advmod|prt(gov,
		/// mwp[0]) prep(gov,mwp[1]) pobj|pcomp(mwp[1], compl) -&gt;
		/// prep_mwp[0]_mwp[1](gov, compl)
		/// <p/>
		/// </summary>
		/// <param name="list">List of typedDependencies to work on</param>
		private static void Collapse2WPbis(ICollection<TypedDependency> list)
		{
			ICollection<TypedDependency> newTypedDeps = new List<TypedDependency>();
			foreach (string[] mwp in MultiwordPreps)
			{
				newTypedDeps.Clear();
				IndexedWord mwp0 = null;
				IndexedWord mwp1 = null;
				IndexedWord governor = null;
				TypedDependency prep = null;
				TypedDependency dep = null;
				TypedDependency pobj = null;
				TypedDependency newtd = null;
				// first find the first part of the multi_preposition: advmod|prt(gov, mwp[0])
				foreach (TypedDependency td in list)
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(td.Dep().Value(), mwp[0]) && (td.Reln() == PhrasalVerbParticle || td.Reln() == AdverbialModifier || td.Reln() == Dependent || td.Reln() == MultiWordExpression))
					{
						// we found advmod(gov, mwp0) or prt(gov, mwp0)
						governor = td.Gov();
						mwp0 = td.Dep();
						dep = td;
					}
				}
				// now search for the second part: prep(gov, mwp1)
				// the two words in the mwp should be next to another in the sentence
				// (difference of indexes = 1)
				if (mwp0 == null || governor == null)
				{
					continue;
				}
				foreach (TypedDependency td1 in list)
				{
					if (td1.Reln() == PrepositionalModifier && Sharpen.Runtime.EqualsIgnoreCase(td1.Dep().Value(), mwp[1]) && Math.Abs(td1.Dep().Index() - mwp0.Index()) == 1 && td1.Gov().Equals(governor))
					{
						// we
						// found
						// prep(gov,
						// mwp1)
						mwp1 = td1.Dep();
						prep = td1;
					}
				}
				if (mwp1 == null)
				{
					continue;
				}
				// search for the complement: pobj|pcomp(mwp1,X)
				foreach (TypedDependency td2 in list)
				{
					if (td2.Reln() == PrepositionalObject && td2.Gov().Equals(mwp1))
					{
						pobj = td2;
						// create the new gr relation
						GrammaticalRelation gr = EnglishGrammaticalRelations.GetPrep(mwp[0] + '_' + mwp[1]);
						newtd = new TypedDependency(gr, governor, pobj.Dep());
					}
					if (td2.Reln() == PrepositionalComplement && td2.Gov().Equals(mwp1))
					{
						pobj = td2;
						// create the new gr relation
						GrammaticalRelation gr = EnglishGrammaticalRelations.GetPrepC(mwp[0] + '_' + mwp[1]);
						newtd = new TypedDependency(gr, governor, pobj.Dep());
					}
				}
				if (pobj == null)
				{
					return;
				}
				// only if we found the three parts, set to KILL and remove
				// and add the new one
				// now prep != null, pobj != null and newtd != null
				prep.SetReln(Kill);
				dep.SetReln(Kill);
				pobj.SetReln(Kill);
				newTypedDeps.Add(newtd);
				// now remove typed dependencies with reln "kill"
				// and promote possible orphans
				foreach (TypedDependency td1_1 in list)
				{
					if (td1_1.Reln() != Kill)
					{
						if (td1_1.Gov().Equals(mwp0) || td1_1.Gov().Equals(mwp1))
						{
							td1_1.SetGov(governor);
						}
						if (!newTypedDeps.Contains(td1_1))
						{
							newTypedDeps.Add(td1_1);
						}
					}
				}
				list.Clear();
				Sharpen.Collections.AddAll(list, newTypedDeps);
			}
		}

		/// <summary>
		/// Collapse 3-word preposition of the following format: <br/>
		/// This will be the case when the preposition is analyzed as a NP <br/>
		/// prep(gov, mwp0) <br/>
		/// X(mwp0,mwp1) <br/>
		/// X(mwp1,mwp2) <br/>
		/// pobj|pcomp(mwp2, compl) <br/>
		/// -&gt; prep_mwp[0]_mwp[1]_mwp[2](gov, compl)
		/// <p/>
		/// It also takes flat annotation into account: <br/>
		/// prep(gov,mwp0) <br/>
		/// X(mwp0,mwp1) <br/>
		/// X(mwp0,mwp2) <br/>
		/// pobj|pcomp(mwp0, compl) <br/>
		/// -&gt; prep_mwp[0]_mwp[1]_mwp[2](gov, compl)
		/// <p/>
		/// </summary>
		/// <param name="list">List of typedDependencies to work on</param>
		private static void Collapse3WP(ICollection<TypedDependency> list)
		{
			ICollection<TypedDependency> newTypedDeps = new List<TypedDependency>();
			// first, loop over the prepositions for NP annotation
			foreach (string[] mwp in ThreewordPreps)
			{
				newTypedDeps.Clear();
				IndexedWord mwp0 = null;
				IndexedWord mwp1 = null;
				IndexedWord mwp2 = null;
				TypedDependency dep1 = null;
				TypedDependency dep2 = null;
				// first find the first part of the 3word preposition: dep(mpw[0], mwp[1])
				// the two words should be next to another in the sentence (difference of
				// indexes = 1)
				foreach (TypedDependency td in list)
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(td.Gov().Value(), mwp[0]) && Sharpen.Runtime.EqualsIgnoreCase(td.Dep().Value(), mwp[1]) && Math.Abs(td.Gov().Index() - td.Dep().Index()) == 1)
					{
						mwp0 = td.Gov();
						mwp1 = td.Dep();
						dep1 = td;
					}
				}
				// find the second part of the 3word preposition: dep(mpw[1], mwp[2])
				// the two words should be next to another in the sentence (difference of
				// indexes = 1)
				foreach (TypedDependency td_1 in list)
				{
					if (td_1.Gov().Equals(mwp1) && Sharpen.Runtime.EqualsIgnoreCase(td_1.Dep().Value(), mwp[2]) && Math.Abs(td_1.Gov().Index() - td_1.Dep().Index()) == 1)
					{
						mwp2 = td_1.Dep();
						dep2 = td_1;
					}
				}
				if (dep1 != null && dep2 != null)
				{
					// now search for prep(gov, mwp0)
					IndexedWord governor = null;
					TypedDependency prep = null;
					foreach (TypedDependency td1 in list)
					{
						if (td1.Reln() == PrepositionalModifier && td1.Dep().Equals(mwp0))
						{
							// we
							// found
							// prep(gov,
							// mwp0)
							prep = td1;
							governor = prep.Gov();
						}
					}
					// search for the complement: pobj|pcomp(mwp2,X)
					TypedDependency pobj = null;
					TypedDependency newtd = null;
					foreach (TypedDependency td2 in list)
					{
						if (td2.Reln() == PrepositionalObject && td2.Gov().Equals(mwp2))
						{
							pobj = td2;
							// create the new gr relation
							GrammaticalRelation gr = EnglishGrammaticalRelations.GetPrep(mwp[0] + '_' + mwp[1] + '_' + mwp[2]);
							if (governor != null)
							{
								newtd = new TypedDependency(gr, governor, pobj.Dep());
							}
						}
						if (td2.Reln() == PrepositionalComplement && td2.Gov().Equals(mwp2))
						{
							pobj = td2;
							// create the new gr relation
							GrammaticalRelation gr = EnglishGrammaticalRelations.GetPrepC(mwp[0] + '_' + mwp[1] + '_' + mwp[2]);
							if (governor != null)
							{
								newtd = new TypedDependency(gr, governor, pobj.Dep());
							}
						}
					}
					// only if we found the governor and complement parts, set to KILL and
					// remove
					// and add the new one
					if (prep != null && pobj != null && newtd != null)
					{
						prep.SetReln(Kill);
						dep1.SetReln(Kill);
						dep2.SetReln(Kill);
						pobj.SetReln(Kill);
						newTypedDeps.Add(newtd);
						// now remove typed dependencies with reln "kill"
						// and promote possible orphans
						foreach (TypedDependency td1_1 in list)
						{
							if (td1_1.Reln() != Kill)
							{
								if (td1_1.Gov().Equals(mwp0) || td1_1.Gov().Equals(mwp1) || td1_1.Gov().Equals(mwp2))
								{
									td1_1.SetGov(governor);
								}
								if (!newTypedDeps.Contains(td1_1))
								{
									newTypedDeps.Add(td1_1);
								}
							}
						}
						list.Clear();
						Sharpen.Collections.AddAll(list, newTypedDeps);
					}
				}
			}
			// second, loop again looking at flat annotation
			foreach (string[] mwp_1 in ThreewordPreps)
			{
				newTypedDeps.Clear();
				IndexedWord mwp0 = null;
				IndexedWord mwp1 = null;
				IndexedWord mwp2 = null;
				TypedDependency dep1 = null;
				TypedDependency dep2 = null;
				// first find the first part of the 3word preposition: dep(mpw[0], mwp[1])
				// the two words should be next to another in the sentence (difference of
				// indexes = 1)
				foreach (TypedDependency td in list)
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(td.Gov().Value(), mwp_1[0]) && Sharpen.Runtime.EqualsIgnoreCase(td.Dep().Value(), mwp_1[1]) && Math.Abs(td.Gov().Index() - td.Dep().Index()) == 1)
					{
						mwp0 = td.Gov();
						mwp1 = td.Dep();
						dep1 = td;
					}
				}
				// find the second part of the 3word preposition: dep(mpw[0], mwp[2])
				// the two words should be one word apart in the sentence (difference of
				// indexes = 2)
				foreach (TypedDependency td_1 in list)
				{
					if (td_1.Gov().Equals(mwp0) && Sharpen.Runtime.EqualsIgnoreCase(td_1.Dep().Value(), mwp_1[2]) && Math.Abs(td_1.Gov().Index() - td_1.Dep().Index()) == 2)
					{
						mwp2 = td_1.Dep();
						dep2 = td_1;
					}
				}
				if (dep1 != null && dep2 != null)
				{
					// now search for prep(gov, mwp0)
					IndexedWord governor = null;
					TypedDependency prep = null;
					foreach (TypedDependency td1 in list)
					{
						if (td1.Dep().Equals(mwp0) && td1.Reln() == PrepositionalModifier)
						{
							// we
							// found
							// prep(gov,
							// mwp0)
							prep = td1;
							governor = prep.Gov();
						}
					}
					// search for the complement: pobj|pcomp(mwp0,X)
					TypedDependency pobj = null;
					TypedDependency newtd = null;
					foreach (TypedDependency td2 in list)
					{
						if (td2.Gov().Equals(mwp0) && td2.Reln() == PrepositionalObject)
						{
							pobj = td2;
							// create the new gr relation
							GrammaticalRelation gr = EnglishGrammaticalRelations.GetPrep(mwp_1[0] + '_' + mwp_1[1] + '_' + mwp_1[2]);
							if (governor != null)
							{
								newtd = new TypedDependency(gr, governor, pobj.Dep());
							}
						}
						if (td2.Gov().Equals(mwp0) && td2.Reln() == PrepositionalComplement)
						{
							pobj = td2;
							// create the new gr relation
							GrammaticalRelation gr = EnglishGrammaticalRelations.GetPrepC(mwp_1[0] + '_' + mwp_1[1] + '_' + mwp_1[2]);
							if (governor != null)
							{
								newtd = new TypedDependency(gr, governor, pobj.Dep());
							}
						}
					}
					// only if we found the governor and complement parts, set to KILL and
					// remove
					// and add the new one
					if (prep != null && pobj != null && newtd != null)
					{
						prep.SetReln(Kill);
						dep1.SetReln(Kill);
						dep2.SetReln(Kill);
						pobj.SetReln(Kill);
						newTypedDeps.Add(newtd);
						// now remove typed dependencies with reln "kill"
						// and promote possible orphans
						foreach (TypedDependency td1_1 in list)
						{
							if (td1_1.Reln() != Kill)
							{
								if (td1_1.Gov().Equals(mwp0) || td1_1.Gov().Equals(mwp1) || td1_1.Gov().Equals(mwp2))
								{
									td1_1.SetGov(governor);
								}
								if (!newTypedDeps.Contains(td1_1))
								{
									newTypedDeps.Add(td1_1);
								}
							}
						}
						list.Clear();
						Sharpen.Collections.AddAll(list, newTypedDeps);
					}
				}
			}
		}

		/*
		*
		* While upgrading, here are some lists of common multiword prepositions which
		* we might try to cover better. (Also do corpus counts for same?)
		*
		* (Prague Dependency Treebank) as per CRIT except for RESTR but for RESTR
		* apart from RESTR away from RESTR aside from RESTR as from TSIN ahead of
		* TWHEN back of LOC, DIR3 exclusive of* RESTR instead of SUBS outside of LOC,
		* DIR3 off of DIR1 upwards of LOC, DIR3 as of TSIN because of CAUS inside of
		* LOC, DIR3 irrespective of REG out of LOC, DIR1 regardless of REG according
		* to CRIT due to CAUS next to LOC, RESTR owing to* CAUS preparatory to* TWHEN
		* prior to* TWHEN subsequent to* TWHEN as to/for REG contrary to* CPR close
		* to* LOC, EXT (except the case named in the next table) near to LOC, DIR3
		* nearer to LOC, DIR3 preliminary to* TWHEN previous to* TWHEN pursuant to*
		* CRIT thanks to CAUS along with ACMP together with ACMP devoid of* ACMP void
		* of* ACMP
		*
		* http://www.keepandshare.com/doc/view.php?u=13166
		*
		* according to ahead of as far as as well as by means of due to far from in
		* addition to in case of in front of in place of in spite of inside of
		* instead of in to (into) near to next to on account of on behalf of on top
		* of on to (onto) out of outside of owing to prior to with regards to
		*
		* www.eslmonkeys.com/book/learner/prepositions.pdf According to Ahead of
		* Along with Apart from As for As to Aside from Because of But for Contrary
		* to Except for Instead of Next to Out of Prior to Thanks to
		*/
		/// <summary>
		/// Collapse multi-words preposition of the following format, which comes from
		/// flat annotation.
		/// </summary>
		/// <remarks>
		/// Collapse multi-words preposition of the following format, which comes from
		/// flat annotation. This handles e.g., "because of" (PP (IN because) (IN of)
		/// ...), "such as" (PP (JJ such) (IN as) ...)
		/// <p/>
		/// prep(gov, mwp[1]) dep(mpw[1], mwp[0]) pobj(mwp[1], compl) -&gt;
		/// prep_mwp[0]_mwp[1](gov, compl)
		/// </remarks>
		/// <param name="list">List of typedDependencies to work on</param>
		private static void CollapseFlatMWP(ICollection<TypedDependency> list)
		{
			ICollection<TypedDependency> newTypedDeps = new List<TypedDependency>();
			foreach (string[] mwp in MultiwordPreps)
			{
				newTypedDeps.Clear();
				IndexedWord mwp1 = null;
				IndexedWord governor = null;
				TypedDependency prep = null;
				TypedDependency dep = null;
				TypedDependency pobj = null;
				// first find the multi_preposition: dep(mpw[1], mwp[0])
				foreach (TypedDependency td in list)
				{
					if (Math.Abs(td.Gov().Index() - td.Dep().Index()) == 1 && Sharpen.Runtime.EqualsIgnoreCase(td.Gov().Value(), mwp[1]) && Sharpen.Runtime.EqualsIgnoreCase(td.Dep().Value(), mwp[0]))
					{
						mwp1 = td.Gov();
						dep = td;
					}
				}
				if (mwp1 == null)
				{
					continue;
				}
				// now search for prep(gov, mwp1)
				foreach (TypedDependency td1 in list)
				{
					if (td1.Dep().Equals(mwp1) && td1.Reln() == PrepositionalModifier)
					{
						// we found prep(gov, mwp1)
						prep = td1;
						governor = prep.Gov();
					}
				}
				if (prep == null)
				{
					continue;
				}
				// search for the complement: pobj|pcomp(mwp1,X)
				foreach (TypedDependency td2 in list)
				{
					if (td2.Gov().Equals(mwp1) && td2.Reln() == PrepositionalObject)
					{
						pobj = td2;
						// create the new gr relation
						GrammaticalRelation gr = EnglishGrammaticalRelations.GetPrep(mwp[0] + '_' + mwp[1]);
						newTypedDeps.Add(new TypedDependency(gr, governor, pobj.Dep()));
					}
					if (td2.Gov().Equals(mwp1) && td2.Reln() == PrepositionalComplement)
					{
						pobj = td2;
						// create the new gr relation
						GrammaticalRelation gr = EnglishGrammaticalRelations.GetPrepC(mwp[0] + '_' + mwp[1]);
						newTypedDeps.Add(new TypedDependency(gr, governor, pobj.Dep()));
					}
				}
				if (pobj == null)
				{
					return;
				}
				// only if we found the three parts, set to KILL and remove
				// we know prep != null && dep != null && dep != null
				prep.SetReln(Kill);
				dep.SetReln(Kill);
				pobj.SetReln(Kill);
				// now remove typed dependencies with reln "kill"
				// and promote possible orphans
				foreach (TypedDependency td1_1 in list)
				{
					if (td1_1.Reln() != Kill)
					{
						if (td1_1.Gov().Equals(mwp1))
						{
							td1_1.SetGov(governor);
						}
						if (!newTypedDeps.Contains(td1_1))
						{
							newTypedDeps.Add(td1_1);
						}
					}
				}
				list.Clear();
				Sharpen.Collections.AddAll(list, newTypedDeps);
			}
		}

		/// <summary>
		/// This method gets rid of multiwords in conjunctions to avoid having them
		/// creating disconnected constituents e.g.,
		/// "bread-1 as-2 well-3 as-4 cheese-5" will be turned into conj_and(bread,
		/// cheese) and then dep(well-3, as-2) and dep(well-3, as-4) cannot be attached
		/// to the graph, these dependencies are erased
		/// </summary>
		/// <param name="list">List of words to get rid of multiword conjunctions from</param>
		private static void EraseMultiConj(ICollection<TypedDependency> list)
		{
			// find typed deps of form cc(gov, x)
			foreach (TypedDependency td1 in list)
			{
				if (td1.Reln() == Coordination)
				{
					IndexedWord x = td1.Dep();
					// find typed deps of form dep(x,y) and kill them
					foreach (TypedDependency td2 in list)
					{
						if (td2.Gov().Equals(x) && (td2.Reln() == Dependent || td2.Reln() == MultiWordExpression || td2.Reln() == Coordination || td2.Reln() == AdverbialModifier || td2.Reln() == NegationModifier || td2.Reln() == AuxModifier))
						{
							td2.SetReln(Kill);
						}
					}
				}
			}
			FilterKill(list);
		}

		/// <summary>
		/// Remove duplicate relations: it can happen when collapsing stranded
		/// prepositions.
		/// </summary>
		/// <remarks>
		/// Remove duplicate relations: it can happen when collapsing stranded
		/// prepositions. E.g., "What does CPR stand for?" we get dep(stand, what), and
		/// after collapsing we also get prep_for(stand, what).
		/// </remarks>
		/// <param name="list">A list of typed dependencies to check through</param>
		private static void RemoveDep(ICollection<TypedDependency> list)
		{
			ICollection<GrammaticalRelation> prepRels = Generics.NewHashSet(EnglishGrammaticalRelations.GetPreps());
			Sharpen.Collections.AddAll(prepRels, EnglishGrammaticalRelations.GetPrepsC());
			foreach (TypedDependency td1 in list)
			{
				if (prepRels.Contains(td1.Reln()))
				{
					// if we have a prep_ relation
					IndexedWord gov = td1.Gov();
					IndexedWord dep = td1.Dep();
					foreach (TypedDependency td2 in list)
					{
						if (td2.Reln() == Dependent && td2.Gov().Equals(gov) && td2.Dep().Equals(dep))
						{
							td2.SetReln(Kill);
						}
					}
				}
			}
			// now remove typed dependencies with reln "kill"
			for (IEnumerator<TypedDependency> iter = list.GetEnumerator(); iter.MoveNext(); )
			{
				TypedDependency td = iter.Current;
				if (td.Reln() == Kill)
				{
					if (Debug)
					{
						log.Info("Removing duplicate relation: " + td);
					}
					iter.Remove();
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
		private static void RemoveExactDuplicates(ICollection<TypedDependency> list)
		{
			ICollection<TypedDependency> set = new TreeSet<TypedDependency>(list);
			list.Clear();
			Sharpen.Collections.AddAll(list, set);
		}

		/// <exception cref="System.IO.IOException"/>
		public static IList<GrammaticalStructure> ReadCoNLLXGrammaticalStructureCollection(string fileName)
		{
			return ReadCoNLLXGrammaticalStructureCollection(fileName, EnglishGrammaticalRelations.shortNameToGRel, new EnglishGrammaticalStructure.FromDependenciesFactory());
		}

		public static EnglishGrammaticalStructure BuildCoNLLXGrammaticalStructure(IList<IList<string>> tokenFields)
		{
			return (EnglishGrammaticalStructure)BuildCoNLLXGrammaticalStructure(tokenFields, EnglishGrammaticalRelations.shortNameToGRel, new EnglishGrammaticalStructure.FromDependenciesFactory());
		}

		public class FromDependenciesFactory : IGrammaticalStructureFromDependenciesFactory
		{
			public virtual EnglishGrammaticalStructure Build(IList<TypedDependency> tdeps, TreeGraphNode root)
			{
				return new EnglishGrammaticalStructure(tdeps, root);
			}
		}

		public static void Main(string[] args)
		{
			GrammaticalStructureConversionUtils.ConvertTrees(args, "en-sd");
		}
	}
}
