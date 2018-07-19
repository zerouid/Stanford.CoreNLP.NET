using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>
	/// <p>
	/// A class intended to be attached to a lexical item, determining what mutations are valid on it while
	/// maintaining valid Natural Logic inference.
	/// </summary>
	/// <remarks>
	/// <p>
	/// A class intended to be attached to a lexical item, determining what mutations are valid on it while
	/// maintaining valid Natural Logic inference.
	/// </p>
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class Polarity
	{
		/// <summary>The default (very permissive) polarity.</summary>
		public static readonly Edu.Stanford.Nlp.Naturalli.Polarity Default = new Edu.Stanford.Nlp.Naturalli.Polarity(Collections.SingletonList(Pair.MakePair(Monotonicity.Monotone, MonotonicityType.Both)));

		/// <summary>The projection function, as a table from a relations fixed index to the projected fixed index</summary>
		private readonly byte[] projectionFunction = new byte[7];

		/// <summary>Create a polarity from a list of operators in scope</summary>
		protected internal Polarity(IList<Pair<Monotonicity, MonotonicityType>> operatorsInNarrowingScopeOrder)
		{
			if (operatorsInNarrowingScopeOrder.IsEmpty())
			{
				for (byte i = 0; ((sbyte)i) < projectionFunction.Length; ++i)
				{
					projectionFunction[i] = i;
				}
			}
			else
			{
				for (int rel = 0; rel < 7; ++rel)
				{
					NaturalLogicRelation relation = NaturalLogicRelation.ByFixedIndex(rel);
					for (int op = operatorsInNarrowingScopeOrder.Count - 1; op >= 0; --op)
					{
						relation = Project(relation, operatorsInNarrowingScopeOrder[op].first, operatorsInNarrowingScopeOrder[op].second);
					}
					projectionFunction[rel] = unchecked((byte)relation.fixedIndex);
				}
			}
		}

		/// <summary>
		/// Create a polarity item by directly copying the projection function from
		/// <see cref="NaturalLogicRelation"/>
		/// s to
		/// their projected relation.
		/// </summary>
		public Polarity(byte[] projectionFunction)
		{
			if (projectionFunction.Length != 7)
			{
				throw new ArgumentException("Invalid projection function: " + Arrays.ToString(projectionFunction));
			}
			for (int i = 0; i < 7; ++i)
			{
				if (((sbyte)projectionFunction[i]) < 0 || projectionFunction[i] > 6)
				{
					throw new ArgumentException("Invalid projection function: " + Arrays.ToString(projectionFunction));
				}
			}
			System.Array.Copy(projectionFunction, 0, this.projectionFunction, 0, 7);
		}

		/// <summary>Encode the projection table in painful detail.</summary>
		/// <param name="input">The input natural logic relation to project up through the operator.</param>
		/// <param name="mono">The monotonicity of the operator we are projecting through.</param>
		/// <param name="type">The monotonicity type of the operator we are projecting through.</param>
		/// <returns>The projected relation, once passed through an operator with the given specifications.</returns>
		private NaturalLogicRelation Project(NaturalLogicRelation input, Monotonicity mono, MonotonicityType type)
		{
			switch (input)
			{
				case NaturalLogicRelation.Equivalent:
				{
					return NaturalLogicRelation.Equivalent;
				}

				case NaturalLogicRelation.ForwardEntailment:
				{
					switch (mono)
					{
						case Monotonicity.Monotone:
						{
							return NaturalLogicRelation.ForwardEntailment;
						}

						case Monotonicity.Antitone:
						{
							return NaturalLogicRelation.ReverseEntailment;
						}

						case Monotonicity.Nonmonotone:
						case Monotonicity.Invalid:
						{
							return NaturalLogicRelation.Independence;
						}
					}
					goto case NaturalLogicRelation.ReverseEntailment;
				}

				case NaturalLogicRelation.ReverseEntailment:
				{
					switch (mono)
					{
						case Monotonicity.Monotone:
						{
							return NaturalLogicRelation.ReverseEntailment;
						}

						case Monotonicity.Antitone:
						{
							return NaturalLogicRelation.ForwardEntailment;
						}

						case Monotonicity.Nonmonotone:
						case Monotonicity.Invalid:
						{
							return NaturalLogicRelation.Independence;
						}
					}
					goto case NaturalLogicRelation.Negation;
				}

				case NaturalLogicRelation.Negation:
				{
					switch (type)
					{
						case MonotonicityType.None:
						{
							return NaturalLogicRelation.Independence;
						}

						case MonotonicityType.Additive:
						{
							switch (mono)
							{
								case Monotonicity.Monotone:
								{
									return NaturalLogicRelation.Cover;
								}

								case Monotonicity.Antitone:
								{
									return NaturalLogicRelation.Alternation;
								}

								case Monotonicity.Nonmonotone:
								case Monotonicity.Invalid:
								{
									return NaturalLogicRelation.Independence;
								}
							}
							goto case MonotonicityType.Multiplicative;
						}

						case MonotonicityType.Multiplicative:
						{
							switch (mono)
							{
								case Monotonicity.Monotone:
								{
									return NaturalLogicRelation.Alternation;
								}

								case Monotonicity.Antitone:
								{
									return NaturalLogicRelation.Cover;
								}

								case Monotonicity.Nonmonotone:
								case Monotonicity.Invalid:
								{
									return NaturalLogicRelation.Independence;
								}
							}
							break;
						}

						case MonotonicityType.Both:
						{
							return NaturalLogicRelation.Negation;
						}
					}
					break;
				}

				case NaturalLogicRelation.Alternation:
				{
					switch (mono)
					{
						case Monotonicity.Monotone:
						{
							switch (type)
							{
								case MonotonicityType.None:
								case MonotonicityType.Additive:
								{
									return NaturalLogicRelation.Independence;
								}

								case MonotonicityType.Multiplicative:
								case MonotonicityType.Both:
								{
									return NaturalLogicRelation.Alternation;
								}
							}
							goto case Monotonicity.Antitone;
						}

						case Monotonicity.Antitone:
						{
							switch (type)
							{
								case MonotonicityType.None:
								case MonotonicityType.Additive:
								{
									return NaturalLogicRelation.Independence;
								}

								case MonotonicityType.Multiplicative:
								case MonotonicityType.Both:
								{
									return NaturalLogicRelation.Cover;
								}
							}
							goto case Monotonicity.Nonmonotone;
						}

						case Monotonicity.Nonmonotone:
						case Monotonicity.Invalid:
						{
							return NaturalLogicRelation.Independence;
						}
					}
					goto case NaturalLogicRelation.Cover;
				}

				case NaturalLogicRelation.Cover:
				{
					switch (mono)
					{
						case Monotonicity.Monotone:
						{
							switch (type)
							{
								case MonotonicityType.None:
								case MonotonicityType.Multiplicative:
								{
									return NaturalLogicRelation.Independence;
								}

								case MonotonicityType.Additive:
								case MonotonicityType.Both:
								{
									return NaturalLogicRelation.Cover;
								}
							}
							goto case Monotonicity.Antitone;
						}

						case Monotonicity.Antitone:
						{
							switch (type)
							{
								case MonotonicityType.None:
								case MonotonicityType.Multiplicative:
								{
									return NaturalLogicRelation.Independence;
								}

								case MonotonicityType.Additive:
								case MonotonicityType.Both:
								{
									return NaturalLogicRelation.Alternation;
								}
							}
							goto case Monotonicity.Nonmonotone;
						}

						case Monotonicity.Nonmonotone:
						case Monotonicity.Invalid:
						{
							return NaturalLogicRelation.Independence;
						}
					}
					goto case NaturalLogicRelation.Independence;
				}

				case NaturalLogicRelation.Independence:
				{
					return NaturalLogicRelation.Independence;
				}
			}
			throw new InvalidOperationException("[should not happen!] Projection table is incomplete for " + mono + " : " + type + " on relation " + input);
		}

		/// <summary>Project the given natural logic lexical relation on this word.</summary>
		/// <remarks>
		/// Project the given natural logic lexical relation on this word. So, for example, if we want to go up the
		/// Hypernymy hierarchy (
		/// <see cref="NaturalLogicRelation.ForwardEntailment"/>
		/// ) on this word,
		/// then this function will tell you what relation holds between the new mutated fact and this fact.
		/// </remarks>
		/// <param name="lexicalRelation">The lexical relation we are applying to this word.</param>
		/// <returns>The relation between the mutated sentence and the original sentence.</returns>
		public virtual NaturalLogicRelation ProjectLexicalRelation(NaturalLogicRelation lexicalRelation)
		{
			return NaturalLogicRelation.ByFixedIndex(projectionFunction[lexicalRelation.fixedIndex]);
		}

		/// <summary>
		/// If true, applying this lexical relation to this word creates a sentence which is entailed by the original sentence,
		/// Note that both this, and
		/// <see cref="NegatesTruth(NaturalLogicRelation)"/>
		/// can be false. If this is the case, then
		/// natural logic can neither verify nor disprove this mutation.
		/// </summary>
		public virtual bool MaintainsTruth(NaturalLogicRelation lexicalRelation)
		{
			return ProjectLexicalRelation(lexicalRelation).maintainsTruth;
		}

		/// <summary>
		/// If true, applying this lexical relation to this word creates a sentence which is negated by the original sentence
		/// Note that both this, and
		/// <see cref="MaintainsTruth(NaturalLogicRelation)"/>
		/// } can be false. If this is the case, then
		/// natural logic can neither verify nor disprove this mutation.
		/// </summary>
		public virtual bool NegatesTruth(NaturalLogicRelation lexicalRelation)
		{
			return ProjectLexicalRelation(lexicalRelation).negatesTruth;
		}

		/// <seealso cref="MaintainsTruth(NaturalLogicRelation)"/>
		public virtual bool MaintainsFalsehood(NaturalLogicRelation lexicalRelation)
		{
			return ProjectLexicalRelation(lexicalRelation).maintainsFalsehood;
		}

		/// <seealso cref="NegatesTruth(NaturalLogicRelation)"/>
		public virtual bool NegatesFalsehood(NaturalLogicRelation lexicalRelation)
		{
			return ProjectLexicalRelation(lexicalRelation).negatesFalsehood;
		}

		/// <summary>Ignoring exclusion, determine if this word has upward polarity.</summary>
		public virtual bool IsUpwards()
		{
			return ProjectLexicalRelation(NaturalLogicRelation.ForwardEntailment) == NaturalLogicRelation.ForwardEntailment && ProjectLexicalRelation(NaturalLogicRelation.ReverseEntailment) == NaturalLogicRelation.ReverseEntailment;
		}

		/// <summary>Ignoring exclusion, determine if this word has downward polarity.</summary>
		public virtual bool IsDownwards()
		{
			return ProjectLexicalRelation(NaturalLogicRelation.ForwardEntailment) == NaturalLogicRelation.ReverseEntailment && ProjectLexicalRelation(NaturalLogicRelation.ReverseEntailment) == NaturalLogicRelation.ForwardEntailment;
		}

		public override string ToString()
		{
			if (IsUpwards())
			{
				return "up";
			}
			else
			{
				if (IsDownwards())
				{
					return "down";
				}
				else
				{
					return "flat";
				}
			}
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o is string)
			{
				switch (((string)o).ToLower())
				{
					case "down":
					case "downward":
					case "downwards":
					case "v":
					{
						return this.IsDownwards();
					}

					case "up":
					case "upward":
					case "upwards":
					case "^":
					{
						return this.IsUpwards();
					}

					case "flat":
					case "none":
					case "-":
					{
						return !this.IsDownwards() && !this.IsUpwards();
					}

					default:
					{
						return false;
					}
				}
			}
			if (!(o is Edu.Stanford.Nlp.Naturalli.Polarity))
			{
				return false;
			}
			Edu.Stanford.Nlp.Naturalli.Polarity polarity = (Edu.Stanford.Nlp.Naturalli.Polarity)o;
			return Arrays.Equals(projectionFunction, polarity.projectionFunction);
		}

		public override int GetHashCode()
		{
			return projectionFunction != null ? Arrays.HashCode(projectionFunction) : 0;
		}
	}
}
