using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using NUnit.Framework;


namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>
	/// A test for the
	/// <see cref="Polarity"/>
	/// class.
	/// This is primarily just spot-checking the projection table, and then some of the utility functions.
	/// </summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class PolarityTest
	{
		private sealed class _List_19 : List<Pair<Monotonicity, MonotonicityType>>
		{
			public _List_19()
			{
				{
				}
			}
		}

		private static readonly Polarity none = new Polarity(new _List_19());

		private sealed class _List_22 : List<Pair<Monotonicity, MonotonicityType>>
		{
			public _List_22()
			{
				{
					this.Add(Pair.MakePair(Monotonicity.Monotone, MonotonicityType.Additive));
				}
			}
		}

		private static readonly Polarity additive = new Polarity(new _List_22());

		private sealed class _List_26 : List<Pair<Monotonicity, MonotonicityType>>
		{
			public _List_26()
			{
				{
					this.Add(Pair.MakePair(Monotonicity.Monotone, MonotonicityType.Multiplicative));
				}
			}
		}

		private static readonly Polarity multiplicative = new Polarity(new _List_26());

		private sealed class _List_30 : List<Pair<Monotonicity, MonotonicityType>>
		{
			public _List_30()
			{
				{
					this.Add(Pair.MakePair(Monotonicity.Monotone, MonotonicityType.Additive));
					this.Add(Pair.MakePair(Monotonicity.Antitone, MonotonicityType.Multiplicative));
				}
			}
		}

		private static readonly Polarity antimultiplicative = new Polarity(new _List_30());

		private sealed class _List_35 : List<Pair<Monotonicity, MonotonicityType>>
		{
			public _List_35()
			{
				{
					this.Add(Pair.MakePair(Monotonicity.Monotone, MonotonicityType.Additive));
					this.Add(Pair.MakePair(Monotonicity.Antitone, MonotonicityType.Multiplicative));
				}
			}
		}

		private static readonly Polarity additiveAntiMultiplicative = new Polarity(new _List_35());

		private sealed class _List_40 : List<Pair<Monotonicity, MonotonicityType>>
		{
			public _List_40()
			{
				{
					this.Add(Pair.MakePair(Monotonicity.Monotone, MonotonicityType.Multiplicative));
					this.Add(Pair.MakePair(Monotonicity.Antitone, MonotonicityType.Multiplicative));
				}
			}
		}

		private static readonly Polarity multiplicativeAntiMultiplicative = new Polarity(new _List_40());

		[Test]
		public virtual void Equals()
		{
			NUnit.Framework.Assert.AreEqual(multiplicative, multiplicative);
			NUnit.Framework.Assert.AreEqual(multiplicative, new Polarity(new _List_49()));
		}

		private sealed class _List_49 : List<Pair<Monotonicity, MonotonicityType>>
		{
			public _List_49()
			{
				{
					this.Add(Pair.MakePair(Monotonicity.Monotone, MonotonicityType.Multiplicative));
				}
			}
		}

		[Test]
		public virtual void EqualsString()
		{
			NUnit.Framework.Assert.AreEqual(multiplicative, "up");
			NUnit.Framework.Assert.AreEqual(multiplicative, "upwards");
		}

		[Test]
		public virtual void NoneProject()
		{
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Equivalent, none.ProjectLexicalRelation(NaturalLogicRelation.Equivalent));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ForwardEntailment, none.ProjectLexicalRelation(NaturalLogicRelation.ForwardEntailment));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ReverseEntailment, none.ProjectLexicalRelation(NaturalLogicRelation.ReverseEntailment));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Negation, none.ProjectLexicalRelation(NaturalLogicRelation.Negation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Alternation, none.ProjectLexicalRelation(NaturalLogicRelation.Alternation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Cover, none.ProjectLexicalRelation(NaturalLogicRelation.Cover));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, none.ProjectLexicalRelation(NaturalLogicRelation.Independence));
		}

		[Test]
		public virtual void Additive_antimultiplicativeProject()
		{
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Equivalent, additiveAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Equivalent));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ReverseEntailment, additiveAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.ForwardEntailment));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ForwardEntailment, additiveAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.ReverseEntailment));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Cover, additiveAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Negation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Cover, additiveAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Alternation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, additiveAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Cover));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, additiveAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Independence));
		}

		[Test]
		public virtual void Multiplicative_antimultiplicativeProject()
		{
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Equivalent, multiplicativeAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Equivalent));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ReverseEntailment, multiplicativeAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.ForwardEntailment));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ForwardEntailment, multiplicativeAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.ReverseEntailment));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, multiplicativeAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Negation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, multiplicativeAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Alternation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, multiplicativeAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Cover));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, multiplicativeAntiMultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Independence));
		}

		[Test]
		public virtual void AdditiveProject()
		{
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Equivalent, additive.ProjectLexicalRelation(NaturalLogicRelation.Equivalent));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ForwardEntailment, additive.ProjectLexicalRelation(NaturalLogicRelation.ForwardEntailment));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ReverseEntailment, additive.ProjectLexicalRelation(NaturalLogicRelation.ReverseEntailment));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Cover, additive.ProjectLexicalRelation(NaturalLogicRelation.Negation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, additive.ProjectLexicalRelation(NaturalLogicRelation.Alternation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Cover, additive.ProjectLexicalRelation(NaturalLogicRelation.Cover));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, additive.ProjectLexicalRelation(NaturalLogicRelation.Independence));
		}

		[Test]
		public virtual void AntimultiplicativeProject()
		{
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Equivalent, antimultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Equivalent));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ReverseEntailment, antimultiplicative.ProjectLexicalRelation(NaturalLogicRelation.ForwardEntailment));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ForwardEntailment, antimultiplicative.ProjectLexicalRelation(NaturalLogicRelation.ReverseEntailment));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Cover, antimultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Negation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Cover, antimultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Alternation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, antimultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Cover));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, antimultiplicative.ProjectLexicalRelation(NaturalLogicRelation.Independence));
		}

		[Test]
		public virtual void MultiplicativeTruth()
		{
			NUnit.Framework.Assert.AreEqual(true, multiplicative.MaintainsTruth(NaturalLogicRelation.Equivalent));
			NUnit.Framework.Assert.AreEqual(true, multiplicative.MaintainsTruth(NaturalLogicRelation.ForwardEntailment));
			NUnit.Framework.Assert.AreEqual(false, multiplicative.MaintainsTruth(NaturalLogicRelation.ReverseEntailment));
			NUnit.Framework.Assert.AreEqual(false, multiplicative.MaintainsTruth(NaturalLogicRelation.Negation));
			NUnit.Framework.Assert.AreEqual(false, multiplicative.MaintainsTruth(NaturalLogicRelation.Alternation));
			NUnit.Framework.Assert.AreEqual(false, multiplicative.MaintainsTruth(NaturalLogicRelation.Cover));
			NUnit.Framework.Assert.AreEqual(false, multiplicative.MaintainsTruth(NaturalLogicRelation.Independence));
			NUnit.Framework.Assert.AreEqual(false, multiplicative.NegatesTruth(NaturalLogicRelation.Equivalent));
			NUnit.Framework.Assert.AreEqual(false, multiplicative.NegatesTruth(NaturalLogicRelation.ForwardEntailment));
			NUnit.Framework.Assert.AreEqual(false, multiplicative.NegatesTruth(NaturalLogicRelation.ReverseEntailment));
			NUnit.Framework.Assert.AreEqual(true, multiplicative.NegatesTruth(NaturalLogicRelation.Negation));
			NUnit.Framework.Assert.AreEqual(true, multiplicative.NegatesTruth(NaturalLogicRelation.Alternation));
			NUnit.Framework.Assert.AreEqual(false, multiplicative.NegatesTruth(NaturalLogicRelation.Cover));
			NUnit.Framework.Assert.AreEqual(false, multiplicative.NegatesTruth(NaturalLogicRelation.Independence));
		}

		[Test]
		public virtual void UpwardDownward()
		{
			NUnit.Framework.Assert.AreEqual(true, multiplicative.IsUpwards());
			NUnit.Framework.Assert.AreEqual(true, additive.IsUpwards());
			NUnit.Framework.Assert.AreEqual(false, antimultiplicative.IsUpwards());
			NUnit.Framework.Assert.AreEqual(false, multiplicativeAntiMultiplicative.IsUpwards());
			NUnit.Framework.Assert.AreEqual(false, additiveAntiMultiplicative.IsUpwards());
			NUnit.Framework.Assert.AreEqual(false, multiplicative.IsDownwards());
			NUnit.Framework.Assert.AreEqual(false, additive.IsDownwards());
			NUnit.Framework.Assert.AreEqual(true, antimultiplicative.IsDownwards());
			NUnit.Framework.Assert.AreEqual(true, multiplicativeAntiMultiplicative.IsDownwards());
			NUnit.Framework.Assert.AreEqual(true, additiveAntiMultiplicative.IsDownwards());
		}
	}
}
