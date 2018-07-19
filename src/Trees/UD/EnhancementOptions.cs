using Sharpen;

namespace Edu.Stanford.Nlp.Trees.UD
{
	/// <summary>Options for enhancing a basic dependency tree.</summary>
	/// <author>Sebastian Schuster</author>
	public class EnhancementOptions
	{
		public bool processMultiWordPrepositions;

		public bool enhancePrepositionalModifiers;

		public bool enhanceConjuncts;

		public bool propagateDependents;

		public bool addReferent;

		public bool addCopyNodes;

		public bool demoteQuantMod;

		public bool addXSubj;

		public bool enhanceOnlyNmods;

		/// <summary>Constructor.</summary>
		/// <param name="processMultiWordPrepositions">Turn multi-word prepositions into flat MWE.</param>
		/// <param name="enhancePrepositionalModifiers">Add prepositions to relation labels.</param>
		/// <param name="enhanceOnlyNmods">Add prepositions only to nmod labels (and not to acl or advcl).</param>
		/// <param name="enhanceConjuncts">Add coordinating conjunctions to relation labels.</param>
		/// <param name="propagateDependents">Propagate dependents.</param>
		/// <param name="addReferent">Add "referent" relation in relative clauses.</param>
		/// <param name="addCopyNodes">Add copy nodes for conjoined Ps and PPs.</param>
		/// <param name="demoteQuantMod">Turn quantificational modifiers into flat multi-word expressions.</param>
		/// <param name="addXSubj">Add relation between controlling subject and controlled verb.</param>
		public EnhancementOptions(bool processMultiWordPrepositions, bool enhancePrepositionalModifiers, bool enhanceOnlyNmods, bool enhanceConjuncts, bool propagateDependents, bool addReferent, bool addCopyNodes, bool demoteQuantMod, bool addXSubj)
		{
			this.processMultiWordPrepositions = processMultiWordPrepositions;
			this.enhancePrepositionalModifiers = enhancePrepositionalModifiers;
			this.enhanceOnlyNmods = enhanceOnlyNmods;
			this.enhanceConjuncts = enhanceConjuncts;
			this.propagateDependents = propagateDependents;
			this.addReferent = addReferent;
			this.addCopyNodes = addCopyNodes;
			this.demoteQuantMod = demoteQuantMod;
			this.addXSubj = addXSubj;
		}

		public EnhancementOptions(Edu.Stanford.Nlp.Trees.UD.EnhancementOptions options)
		{
			this.processMultiWordPrepositions = options.processMultiWordPrepositions;
			this.enhancePrepositionalModifiers = options.enhancePrepositionalModifiers;
			this.enhanceConjuncts = options.enhanceConjuncts;
			this.propagateDependents = options.propagateDependents;
			this.addReferent = options.addReferent;
			this.addCopyNodes = options.addCopyNodes;
			this.demoteQuantMod = options.demoteQuantMod;
			this.addXSubj = options.addXSubj;
			this.enhanceOnlyNmods = options.enhanceOnlyNmods;
		}
	}
}
