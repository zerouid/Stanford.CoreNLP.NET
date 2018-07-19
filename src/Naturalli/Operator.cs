using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>A collection of quantifiers.</summary>
	/// <remarks>A collection of quantifiers. This is the exhaustive list of quantifiers our system knows about.</remarks>
	/// <author>Gabor Angeli</author>
	[System.Serializable]
	public sealed class Operator
	{
		public static readonly Edu.Stanford.Nlp.Naturalli.Operator All = new Edu.Stanford.Nlp.Naturalli.Operator("all", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Every = new Edu.Stanford.Nlp.Naturalli.Operator("every", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Any = new Edu.Stanford.Nlp.Naturalli.Operator("any", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Each = new Edu.Stanford.Nlp.Naturalli.Operator("each", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator TheLotOf = new Edu.Stanford.Nlp.Naturalli.Operator("the lot of", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator AllOf = new Edu.Stanford.Nlp.Naturalli.Operator("all of", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator EachOf = new Edu.Stanford.Nlp.Naturalli.Operator("each of", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator ForAll = new Edu.Stanford.Nlp.Naturalli.Operator("for all", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator ForEvery = new Edu.Stanford.Nlp.Naturalli.Operator("for every", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator ForEach = new Edu.Stanford.Nlp.Naturalli.Operator("for each", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Everyone = new Edu.Stanford.Nlp.Naturalli.Operator("everyone", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Num = new Edu.Stanford.Nlp.Naturalli.Operator("--num--", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator NumNum = new Edu.Stanford.Nlp.Naturalli.Operator("--num-- --num--", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator NumNumNum = new Edu.Stanford.Nlp.Naturalli.Operator("--num-- --num-- --num--", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator NumNumNumNum = new Edu.Stanford.Nlp.Naturalli.Operator("--num-- --num-- --num-- --num--", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Few = new Edu.Stanford.Nlp.Naturalli.Operator("few", NaturalLogicRelation.ForwardEntailment, "anti-additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator ImplicitNamedEntity = new Edu.Stanford.Nlp.Naturalli.Operator("__implicit_named_entity__", NaturalLogicRelation.ForwardEntailment, "additive", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator No = new Edu.Stanford.Nlp.Naturalli.Operator("no", NaturalLogicRelation.Independence, "anti-additive", "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Neither = new Edu.Stanford.Nlp.Naturalli.Operator("neither", NaturalLogicRelation.Independence, "anti-additive", "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator NoOne = new Edu.Stanford.Nlp.Naturalli.Operator("no one", NaturalLogicRelation.Independence, "anti-additive", "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Nobody = new Edu.Stanford.Nlp.Naturalli.Operator("nobody", NaturalLogicRelation.Independence, "anti-additive", "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Not = new Edu.Stanford.Nlp.Naturalli.Operator("not", NaturalLogicRelation.Independence, "anti-additive", "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator But = new Edu.Stanford.Nlp.Naturalli.Operator("but", NaturalLogicRelation.Independence, "anti-additive", "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Except = new Edu.Stanford.Nlp.Naturalli.Operator("except", NaturalLogicRelation.Independence, "anti-additive", "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator UnaryNo = new Edu.Stanford.Nlp.Naturalli.Operator("no", NaturalLogicRelation.Independence, "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator UnaryNot = new Edu.Stanford.Nlp.Naturalli.Operator("not", NaturalLogicRelation.Independence, "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator UnaryNoOne = new Edu.Stanford.Nlp.Naturalli.Operator("no one", NaturalLogicRelation.Independence, "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator UnaryNt = new Edu.Stanford.Nlp.Naturalli.Operator("n't", NaturalLogicRelation.Independence, "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator UnaryBut = new Edu.Stanford.Nlp.Naturalli.Operator("but", NaturalLogicRelation.Independence, "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator UnaryExcept = new Edu.Stanford.Nlp.Naturalli.Operator("except", NaturalLogicRelation.Independence, "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator GeneralNegPolarity = new Edu.Stanford.Nlp.Naturalli.Operator("neg_polarity_trigger", NaturalLogicRelation.Independence, "anti-additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Some = new Edu.Stanford.Nlp.Naturalli.Operator("some", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Several = new Edu.Stanford.Nlp.Naturalli.Operator("several", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Either = new Edu.Stanford.Nlp.Naturalli.Operator("either", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator A = new Edu.Stanford.Nlp.Naturalli.Operator("a", NaturalLogicRelation.ForwardEntailment, "additive-multiplicative", "additive-multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator The = new Edu.Stanford.Nlp.Naturalli.Operator("the", NaturalLogicRelation.ForwardEntailment, "additive-multiplicative", "additive-multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator LessThan = new Edu.Stanford.Nlp.Naturalli.Operator("less than --num--", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator SomeOf = new Edu.Stanford.Nlp.Naturalli.Operator("some of", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator OneOf = new Edu.Stanford.Nlp.Naturalli.Operator("one of", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator AtLeast = new Edu.Stanford.Nlp.Naturalli.Operator("at least --num--", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator AFew = new Edu.Stanford.Nlp.Naturalli.Operator("a few", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator AtLeastAFew = new Edu.Stanford.Nlp.Naturalli.Operator("at least a few", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator ThereBe = new Edu.Stanford.Nlp.Naturalli.Operator("there be", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator ThereBeAFew = new Edu.Stanford.Nlp.Naturalli.Operator("there be a few", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator ThereExist = new Edu.Stanford.Nlp.Naturalli.Operator("there exist", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator NumOf = new Edu.Stanford.Nlp.Naturalli.Operator("--num-- of", NaturalLogicRelation.ForwardEntailment, "additive", "additive");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator NotAll = new Edu.Stanford.Nlp.Naturalli.Operator("not all", NaturalLogicRelation.Independence, "additive", "anti-multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator NotEvery = new Edu.Stanford.Nlp.Naturalli.Operator("not every", NaturalLogicRelation.Independence, "additive", "anti-multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Most = new Edu.Stanford.Nlp.Naturalli.Operator("most", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator More = new Edu.Stanford.Nlp.Naturalli.Operator("more", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Many = new Edu.Stanford.Nlp.Naturalli.Operator("many", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Enough = new Edu.Stanford.Nlp.Naturalli.Operator("enough", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator MoreThan = new Edu.Stanford.Nlp.Naturalli.Operator("more than __num_", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator LotsOf = new Edu.Stanford.Nlp.Naturalli.Operator("lots of", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator PlentyOf = new Edu.Stanford.Nlp.Naturalli.Operator("plenty of", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator HeapsOf = new Edu.Stanford.Nlp.Naturalli.Operator("heap of", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator ALoadOf = new Edu.Stanford.Nlp.Naturalli.Operator("a load of", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator LoadsOf = new Edu.Stanford.Nlp.Naturalli.Operator("load of", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator TonsOf = new Edu.Stanford.Nlp.Naturalli.Operator("ton of", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator Both = new Edu.Stanford.Nlp.Naturalli.Operator("both", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator JustNum = new Edu.Stanford.Nlp.Naturalli.Operator("just --num--", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator OnlyNum = new Edu.Stanford.Nlp.Naturalli.Operator("only --num--", NaturalLogicRelation.ForwardEntailment, "nonmonotone", "multiplicative");

		public static readonly Edu.Stanford.Nlp.Naturalli.Operator AtMostNum = new Edu.Stanford.Nlp.Naturalli.Operator("at most --num--", NaturalLogicRelation.ForwardEntailment, "anti-additive", "anti-additive");

		private sealed class _HashSet_95 : HashSet<string>
		{
			public _HashSet_95()
			{
				{
					// "All" quantifiers
					// TODO check me
					// TODO check me
					// TODO check me
					// TODO check me
					// TODO check me
					// TODO check me
					// "No" quantifiers
					// A general quantifier for all "doubt"-like words
					// "Some" quantifiers
					// "Not All" quantifiers
					// "Most" quantifiers
					// TODO(gabor) check these
					// Strange cases
					foreach (Edu.Stanford.Nlp.Naturalli.Operator q in Edu.Stanford.Nlp.Naturalli.Operator.Values())
					{
						this.Add(q.surfaceForm);
					}
				}
			}
		}

		public static readonly ICollection<string> Glosses = Java.Util.Collections.UnmodifiableSet(new _HashSet_95());

		private sealed class _List_105 : List<Edu.Stanford.Nlp.Naturalli.Operator>
		{
			public _List_105()
			{
				{
					foreach (Edu.Stanford.Nlp.Naturalli.Operator op in Edu.Stanford.Nlp.Naturalli.Operator.Values())
					{
						this.Add(op);
					}
					this.Sort(null);
				}
			}
		}

		/// <summary>An ordered list of the known operators, by token length (descending).</summary>
		/// <remarks>
		/// An ordered list of the known operators, by token length (descending). This ensures that we're matching the
		/// widest scoped operator.
		/// </remarks>
		public static readonly IList<Edu.Stanford.Nlp.Naturalli.Operator> valuesByLengthDesc = Java.Util.Collections.UnmodifiableList(new _List_105());

		public readonly string surfaceForm;

		public readonly Monotonicity subjMono;

		public readonly MonotonicityType subjType;

		public readonly Monotonicity objMono;

		public readonly MonotonicityType objType;

		public readonly NaturalLogicRelation deleteRelation;

		internal Operator(string surfaceForm, NaturalLogicRelation deleteRelation, string subjMono, string objMono)
		{
			this.surfaceForm = surfaceForm;
			this.deleteRelation = deleteRelation;
			Pair<Monotonicity, MonotonicityType> subj = MonoFromString(subjMono);
			this.subjMono = subj.first;
			this.subjType = subj.second;
			Pair<Monotonicity, MonotonicityType> obj = MonoFromString(objMono);
			this.objMono = obj.first;
			this.objType = obj.second;
		}

		internal Operator(string surfaceForm, NaturalLogicRelation deleteRelation, string subjMono)
		{
			this.surfaceForm = surfaceForm;
			this.deleteRelation = deleteRelation;
			Pair<Monotonicity, MonotonicityType> subj = MonoFromString(subjMono);
			this.subjMono = subj.first;
			this.subjType = subj.second;
			this.objMono = Monotonicity.Invalid;
			this.objType = MonotonicityType.None;
		}

		public bool IsUnary()
		{
			return Edu.Stanford.Nlp.Naturalli.Operator.objMono == Monotonicity.Invalid;
		}

		public static Pair<Monotonicity, MonotonicityType> MonoFromString(string mono)
		{
			switch (mono)
			{
				case "nonmonotone":
				{
					return Pair.MakePair(Monotonicity.Nonmonotone, MonotonicityType.None);
				}

				case "additive":
				{
					return Pair.MakePair(Monotonicity.Monotone, MonotonicityType.Additive);
				}

				case "multiplicative":
				{
					return Pair.MakePair(Monotonicity.Monotone, MonotonicityType.Multiplicative);
				}

				case "additive-multiplicative":
				{
					return Pair.MakePair(Monotonicity.Monotone, MonotonicityType.Both);
				}

				case "anti-additive":
				{
					return Pair.MakePair(Monotonicity.Antitone, MonotonicityType.Additive);
				}

				case "anti-multiplicative":
				{
					return Pair.MakePair(Monotonicity.Antitone, MonotonicityType.Multiplicative);
				}

				case "anti-additive-multiplicative":
				{
					return Pair.MakePair(Monotonicity.Antitone, MonotonicityType.Both);
				}

				default:
				{
					throw new ArgumentException("Unknown monotonicity: " + mono);
				}
			}
		}

		public static string MonotonicitySignature(Monotonicity mono, MonotonicityType type)
		{
			switch (mono)
			{
				case Monotonicity.Monotone:
				{
					switch (type)
					{
						case MonotonicityType.None:
						{
							return "nonmonotone";
						}

						case MonotonicityType.Additive:
						{
							return "additive";
						}

						case MonotonicityType.Multiplicative:
						{
							return "multiplicative";
						}

						case MonotonicityType.Both:
						{
							return "additive-multiplicative";
						}
					}
					goto case Monotonicity.Antitone;
				}

				case Monotonicity.Antitone:
				{
					switch (type)
					{
						case MonotonicityType.None:
						{
							return "nonmonotone";
						}

						case MonotonicityType.Additive:
						{
							return "anti-additive";
						}

						case MonotonicityType.Multiplicative:
						{
							return "anti-multiplicative";
						}

						case MonotonicityType.Both:
						{
							return "anti-additive-multiplicative";
						}
					}
					goto case Monotonicity.Nonmonotone;
				}

				case Monotonicity.Nonmonotone:
				{
					return "nonmonotone";
				}
			}
			throw new InvalidOperationException("Unhandled case: " + mono + " and " + type);
		}

		private sealed class _HashSet_179 : HashSet<string>
		{
			public _HashSet_179()
			{
				{
					foreach (Edu.Stanford.Nlp.Naturalli.Operator @operator in Edu.Stanford.Nlp.Naturalli.Operator.Values())
					{
						this.Add(@operator.surfaceForm);
					}
				}
			}
		}

		public static readonly ICollection<string> quantifierGlosses = Java.Util.Collections.UnmodifiableSet(new _HashSet_179());

		public static Optional<Edu.Stanford.Nlp.Naturalli.Operator> FromString(string word)
		{
			string wordToLowerCase = word.ToLower().ReplaceAll("[0-9]", "--num-- ").Trim();
			foreach (Edu.Stanford.Nlp.Naturalli.Operator candidate in Edu.Stanford.Nlp.Naturalli.Operator.Values())
			{
				if (candidate.surfaceForm.Equals(wordToLowerCase))
				{
					return Optional.Of(candidate);
				}
			}
			return Optional.Empty();
		}
	}
}
