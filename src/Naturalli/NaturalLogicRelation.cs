using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>The catalog of the seven Natural Logic relations.</summary>
	/// <remarks>
	/// The catalog of the seven Natural Logic relations.
	/// Set-theoretically, if we assume A and B are two sets (e.g., denotations),
	/// and D is the universe of discourse,
	/// then the relations between A and B are defined as follows:
	/// <ul>
	/// <li>Equivalence: A = B</li>
	/// <li>Forward entailment: A \\subset B</li>
	/// <li>Reverse entailment: A \\supset B</li>
	/// <li>Negation: A \\intersect B = \\empty \\land A \\union B = D </li>
	/// <li>Alternation: A \\intersect B = \\empty </li>
	/// <li>Cover: A \\union B = D </li>
	/// </ul>
	/// </remarks>
	/// <author>Gabor Angeli</author>
	[System.Serializable]
	public sealed class NaturalLogicRelation
	{
		public static readonly Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation Equivalent = new Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation(0, true, false, true, false);

		public static readonly Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation ForwardEntailment = new Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation(1, true, false, false, false);

		public static readonly Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation ReverseEntailment = new Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation(2, false, false, true, false);

		public static readonly Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation Negation = new Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation(3, false, true, false, true);

		public static readonly Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation Alternation = new Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation(4, false, true, false, false);

		public static readonly Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation Cover = new Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation(5, false, false, false, true);

		public static readonly Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation Independence = new Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation(6, false, false, false, false);

		/// <summary>A fixed index for this relation, so that it can be serialized more efficiently.</summary>
		/// <remarks>
		/// A fixed index for this relation, so that it can be serialized more efficiently.
		/// DO NOT CHANGE THIS INDEX or you will break existing serialization, and probably a bunch of other stuff too.
		/// Otherwise, the index is arbitrary.
		/// </remarks>
		public readonly int fixedIndex;

		/// <summary>Determines whether this relation maintains the truth of a fact in a true context.</summary>
		/// <remarks>
		/// Determines whether this relation maintains the truth of a fact in a true context.
		/// So, if the premise is true, and this relation is applied, the conclusion remains true.
		/// </remarks>
		public readonly bool maintainsTruth;

		/// <summary>Determines whether this relation negates the truth of a fact in a true context.</summary>
		/// <remarks>
		/// Determines whether this relation negates the truth of a fact in a true context.
		/// So, if the premise is true, and this relation is applied, the conclusion becomes false.
		/// </remarks>
		public readonly bool negatesTruth;

		/// <summary>Determines whether this relation maintains the falsehood of a false fact.</summary>
		/// <remarks>
		/// Determines whether this relation maintains the falsehood of a false fact.
		/// So, if the premise is false, and this relation is applied, the conclusion remains false.
		/// </remarks>
		public readonly bool maintainsFalsehood;

		/// <summary>Determines whether this relation negates the truth of a fact in a false context.</summary>
		/// <remarks>
		/// Determines whether this relation negates the truth of a fact in a false context.
		/// So, if the premise is false, and this relation is applied, the conclusion becomes true.
		/// </remarks>
		public readonly bool negatesFalsehood;

		internal NaturalLogicRelation(int fixedIndex, bool maintainsTruth, bool negatesTruth, bool maintainsFalsehood, bool negatesFalsehood)
		{
			this.fixedIndex = fixedIndex;
			this.maintainsTruth = maintainsTruth;
			this.negatesTruth = negatesTruth;
			this.maintainsFalsehood = maintainsFalsehood;
			this.negatesFalsehood = negatesFalsehood;
		}

		protected internal static Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation ByFixedIndex(int index)
		{
			switch (index)
			{
				case 0:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent;
				}

				case 1:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
				}

				case 2:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
				}

				case 3:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation;
				}

				case 4:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation;
				}

				case 5:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover;
				}

				case 6:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
				}

				default:
				{
					throw new ArgumentException("Unknown index for Natural Logic relation: " + index);
				}
			}
		}

		/// <summary>The MacCartney "join table" -- this determines the transitivity of entailment if we chain two relations together.</summary>
		/// <remarks>
		/// The MacCartney "join table" -- this determines the transitivity of entailment if we chain two relations together.
		/// These should already be projected up through the sentence, so that the relations being joined are relations between
		/// <i>sentences</i> rather than relations between <i>lexical items</i> (see
		/// <see cref="Polarity.ProjectLexicalRelation(NaturalLogicRelation)"/>
		/// ,
		/// set by
		/// <see cref="NaturalLogicAnnotator"/>
		/// using the
		/// <see cref="PolarityAnnotation"/>
		/// ).
		/// </remarks>
		/// <param name="other">The relation to join this relation with.</param>
		/// <returns>The new joined relation.</returns>
		public Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation Join(Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation other)
		{
			switch (this)
			{
				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent:
				{
					return other;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment:
				{
					switch (other)
					{
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
						}
					}
					goto case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment:
				{
					switch (other)
					{
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
						}
					}
					goto case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation:
				{
					switch (other)
					{
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
						}
					}
					goto case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation:
				{
					switch (other)
					{
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
						}
					}
					goto case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover:
				{
					switch (other)
					{
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
						}

						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover:
						case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence:
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
						}
					}
					goto case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
				}
			}
			throw new InvalidOperationException("[should be impossible]: Incomplete join table for " + this + " joined with " + other);
		}

		/// <summary>
		/// Implements the finite state automata of composing the truth value of a sentence with a natural logic relation being
		/// applied.
		/// </summary>
		/// <param name="initialTruthValue">The truth value of the premise (the original sentence).</param>
		/// <returns>
		/// The truth value of the consequent -- that is, the sentence once it's been modified with this relation.
		/// A value of
		/// <see cref="Edu.Stanford.Nlp.Util.Trilean.Unknown"/>
		/// indicates that natural logic cannot either confirm or disprove the truth
		/// of the consequent.
		/// </returns>
		public Trilean ApplyToTruthValue(bool initialTruthValue)
		{
			if (initialTruthValue)
			{
				if (Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.maintainsTruth)
				{
					return Trilean.True;
				}
				else
				{
					if (Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.negatesTruth)
					{
						return Trilean.False;
					}
					else
					{
						return Trilean.Unknown;
					}
				}
			}
			else
			{
				if (Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.maintainsFalsehood)
				{
					return Trilean.False;
				}
				else
				{
					if (Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.negatesFalsehood)
					{
						return Trilean.True;
					}
					else
					{
						return Trilean.Unknown;
					}
				}
			}
		}

		private sealed class _Dictionary_201 : Dictionary<string, Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation>
		{
			public _Dictionary_201()
			{
				{
					this["acomp"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["advcl"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["acl"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["acl:relcl"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["advmod"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["agent"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					//
					this["amod"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["appos"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["aux"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					// he left -/-> he should leave
					this["auxpass"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					// some cat adopts -/-> some cat got adopted
					this["comp"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["ccomp"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// interesting project here... "he said x" -> "x"?
					this["cc"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// match dep_conj
					this["compound"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					//
					this["name"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					//
					this["mwe"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					//
					this["conj:and\\/or"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["conj:and"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["conj:both"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["conj:but"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["conj:nor"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
					//
					this["conj:or"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
					//
					this["conj:plus"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
					//
					this["conj"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// match dep_cc
					this["conj_x"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["cop"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					//
					this["csubj"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// clausal subject is split by clauses
					this["csubjpass"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					// as above
					this["dep"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// allow cutting these off, else we just miss a bunch of sentences
					this["det"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
					// todo(gabor) better treatment of generics?
					this["discourse"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent;
					//
					this["dobj"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// but, "he studied NLP at Stanford" -> "he studied NLP"
					this["expl"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent;
					// though we shouldn't see this...
					this["goeswith"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent;
					// also shouldn't see this
					this["infmod"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// deprecated into vmod
					this["iobj"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// she gave me a raise -> she gave a raise
					this["mark"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// he says that you like to swim -> he says you like to swim
					this["mwe"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					// shouldn't see this
					this["neg"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation;
					//
					this["nn"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					//
					this["npadvmod"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// "9 months after his election, <main clause>"
					this["nsubj"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// Note[gabor]: Only true for _duplicate_ nsubj relations. @see NaturalLogicWeights.
					this["nsubjpass"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					//
					this["number"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					//
					this["num"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					// gets a bit too vague if we allow deleting this? "he served three terms" -?-> "he served terms"
					this["op"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					//
					this["parataxis"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// we split on clauses on this
					this["partmod"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// deprecated into vmod
					this["pcomp"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					// though, not so in collapsed dependencies
					this["pobj"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					// must delete whole preposition
					this["possessive"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					// see dep_poss
					this["poss"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
					//
					this["nmod:poss"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
					//
					this["preconj"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					// forbidden to see this
					this["predet"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					// forbidden to see this
					this["case"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					//
					this["nmod:aboard"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:about"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:above"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:according_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:across_from"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:across"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:after"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:against"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:ahead_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:along"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:alongside_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:alongside"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:along_with"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:amid"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:among"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:anti"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:apart_from"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:around"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:as_for"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:as_from"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:aside_from"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:as_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:as_per"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:as"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:as_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:at"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:away_from"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:based_on"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:because_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:before"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:behind"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:below"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:beneath"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:beside"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:besides"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:between"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:beyond"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:but"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:by_means_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:by"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:depending_on"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:dep"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:despite"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:down"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:due_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:during"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:en"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:except_for"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:excepting"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:except"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:excluding"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:exclusive_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:followed_by"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:following"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:for"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:from"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:if"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:in_accordance_with"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:in_addition_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:in_case_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:including"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:in_front_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:in_lieu_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:in_place_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:in"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:inside_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:inside"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:in_spite_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:instead_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:into"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:irrespective_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:like"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:minus"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:near"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:near_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:next_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:off_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:off"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:on_account_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:on_behalf_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:on"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:on_top_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:onto"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:opposite"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:out_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:out"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:outside_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:outside"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:over"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:owing_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:past"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:per"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:plus"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:preliminary_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:preparatory_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:previous_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:prior_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:pursuant_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:regarding"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:regardless_of"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:round"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:save"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:since"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:subsequent_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:such_as"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:thanks_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:than"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:throughout"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:through"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:together_with"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:toward"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:towards"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:underneath"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:under"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:unlike"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:until"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:upon"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:up"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:versus"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:via"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:vs."] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:whether"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:within"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:without"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:with_regard_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:with_respect_to"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["nmod:with"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["prt"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					//
					this["punct"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent;
					//
					this["purpcl"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// deprecated into advmod
					this["quantmod"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
					//
					this["ref"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// Delete thigns like 'which' referring back to a subject.
					this["rcmod"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					// "there are great tenors --rcmod--> who are modest"
					this["root"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
					// err.. never delete
					this["tmod"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["vmod"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					//
					this["xcomp"] = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
				}
			}
		}

		private static readonly IDictionary<string, Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation> insertArcToNaturalLogicRelation = Java.Util.Collections.UnmodifiableMap(new _Dictionary_201());

		//
		/// <summary>Returns whether this is a known dependency arc.</summary>
		public static bool KnownDependencyArc(string dependencyLabel)
		{
			return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.insertArcToNaturalLogicRelation.Contains(dependencyLabel.ToLower());
		}

		/// <summary>Returns the natural logic relation corresponding to the given dependency arc being inserted into a sentence.</summary>
		public static Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation ForDependencyInsertion(string dependencyLabel)
		{
			return ForDependencyInsertion(dependencyLabel, true);
		}

		/// <summary>Returns the natural logic relation corresponding to the given dependency arc being inserted into a sentence.</summary>
		/// <param name="dependencyLabel">The label we are checking the relation for.</param>
		/// <param name="isSubject">Whether this is on the subject side of a relation (e.g., for CONJ_OR edges)</param>
		public static Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation ForDependencyInsertion(string dependencyLabel, bool isSubject)
		{
			return ForDependencyInsertion(dependencyLabel, isSubject, Optional.Empty());
		}

		/// <summary>Returns the natural logic relation corresponding to the given dependency arc being inserted into a sentence.</summary>
		/// <param name="dependencyLabel">The label we are checking the relation for.</param>
		/// <param name="isSubject">Whether this is on the subject side of a relation (e.g., for CONJ_OR edges)</param>
		/// <param name="dependent">The dependent word of the dependency label.</param>
		public static Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation ForDependencyInsertion(string dependencyLabel, bool isSubject, Optional<string> dependent)
		{
			if (!isSubject)
			{
				switch (dependencyLabel)
				{
					case "conj:or":
					case "conj:nor":
					{
						// 'or' in the object position behaves as and.
						return ForDependencyInsertion("conj:and", false);
					}

					case "cc:preconj":
					{
						if (dependent.IsPresent() && Sharpen.Runtime.EqualsIgnoreCase("neither", dependent.Get()))
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
						}
						else
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
						}
						break;
					}
				}
			}
			Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation rel = Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.insertArcToNaturalLogicRelation[dependencyLabel.ToLower()];
			if (rel != null)
			{
				return rel;
			}
			else
			{
				//      log.info("Unknown dependency arc for NaturalLogicRelation: " + dependencyLabel);
				if (dependencyLabel.StartsWith("nmod:"))
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
				}
				else
				{
					if (dependencyLabel.StartsWith("conj"))
					{
						return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
					}
					else
					{
						if (dependencyLabel.StartsWith("advcl"))
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
						}
						else
						{
							return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
						}
					}
				}
			}
		}

		private static Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation InsertionToDeletion(Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation insertionRel)
		{
			switch (insertionRel)
			{
				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Equivalent;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ReverseEntailment:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.ForwardEntailment;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Negation;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Cover:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Alternation;
				}

				case Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence:
				{
					return Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation.Independence;
				}

				default:
				{
					throw new InvalidOperationException("Unhandled natural logic relation: " + insertionRel);
				}
			}
		}

		/// <summary>Returns the natural logic relation corresponding to the given dependency arc being deleted from a sentence.</summary>
		public static Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation ForDependencyDeletion(string dependencyLabel)
		{
			return ForDependencyDeletion(dependencyLabel, true);
		}

		/// <summary>Returns the natural logic relation corresponding to the given dependency arc being deleted from a sentence.</summary>
		/// <param name="dependencyLabel">The label we are checking the relation for</param>
		/// <param name="isSubject">Whether this is on the subject side of a relation (e.g., for CONJ_OR edges)</param>
		public static Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation ForDependencyDeletion(string dependencyLabel, bool isSubject)
		{
			Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation rel = ForDependencyInsertion(dependencyLabel, isSubject);
			return InsertionToDeletion(rel);
		}

		/// <summary>Returns the natural logic relation corresponding to the given dependency arc being deleted from a sentence.</summary>
		/// <param name="dependencyLabel">The label we are checking the relation for</param>
		/// <param name="isSubject">Whether this is on the subject side of a relation (e.g., for CONJ_OR edges)</param>
		/// <param name="dependent">The dependent word of the dependency label.</param>
		public static Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation ForDependencyDeletion(string dependencyLabel, bool isSubject, Optional<string> dependent)
		{
			Edu.Stanford.Nlp.Naturalli.NaturalLogicRelation rel = ForDependencyInsertion(dependencyLabel, isSubject, dependent);
			return InsertionToDeletion(rel);
		}
	}
}
