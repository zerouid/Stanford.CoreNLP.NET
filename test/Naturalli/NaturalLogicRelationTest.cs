using NUnit.Framework;


namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>
	/// A test for
	/// <see cref="NaturalLogicRelation"/>
	/// .
	/// </summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class NaturalLogicRelationTest
	{
		[Test]
		public virtual void FixedIndex()
		{
			foreach (NaturalLogicRelation rel in NaturalLogicRelation.Values())
			{
				NUnit.Framework.Assert.AreEqual(rel, NaturalLogicRelation.ByFixedIndex(rel.fixedIndex));
			}
		}

		[Test]
		public virtual void SpotTestJoinTable()
		{
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Cover, NaturalLogicRelation.Negation.Join(NaturalLogicRelation.ForwardEntailment));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ForwardEntailment, NaturalLogicRelation.Alternation.Join(NaturalLogicRelation.Negation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ReverseEntailment, NaturalLogicRelation.Cover.Join(NaturalLogicRelation.Alternation));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Equivalent, NaturalLogicRelation.Negation.Join(NaturalLogicRelation.Negation));
			foreach (NaturalLogicRelation rel in NaturalLogicRelation.Values())
			{
				NUnit.Framework.Assert.AreEqual(rel, NaturalLogicRelation.Equivalent.Join(rel));
				NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, NaturalLogicRelation.Independence.Join(rel));
				NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.Independence, rel.Join(NaturalLogicRelation.Independence));
			}
		}

		[Test]
		public virtual void EntailmentState()
		{
			NUnit.Framework.Assert.IsTrue(NaturalLogicRelation.Equivalent.maintainsTruth);
			NUnit.Framework.Assert.IsTrue(NaturalLogicRelation.ForwardEntailment.maintainsTruth);
			NUnit.Framework.Assert.IsTrue(NaturalLogicRelation.Negation.negatesTruth);
			NUnit.Framework.Assert.IsTrue(NaturalLogicRelation.Alternation.negatesTruth);
			NUnit.Framework.Assert.IsFalse(NaturalLogicRelation.Equivalent.negatesTruth);
			NUnit.Framework.Assert.IsFalse(NaturalLogicRelation.ForwardEntailment.negatesTruth);
			NUnit.Framework.Assert.IsFalse(NaturalLogicRelation.Negation.maintainsTruth);
			NUnit.Framework.Assert.IsFalse(NaturalLogicRelation.Alternation.maintainsTruth);
			NUnit.Framework.Assert.IsFalse(NaturalLogicRelation.Cover.maintainsTruth);
			NUnit.Framework.Assert.IsFalse(NaturalLogicRelation.Cover.negatesTruth);
			NUnit.Framework.Assert.IsFalse(NaturalLogicRelation.Independence.maintainsTruth);
			NUnit.Framework.Assert.IsFalse(NaturalLogicRelation.Independence.negatesTruth);
		}

		[Test]
		public virtual void SomeInsertionRelations()
		{
			//    assertEquals(NaturalLogicRelation.INDEPENDENCE, NaturalLogicRelation.forDependencyInsertion("nsubj"));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ForwardEntailment, NaturalLogicRelation.ForDependencyInsertion("quantmod"));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ReverseEntailment, NaturalLogicRelation.ForDependencyInsertion("amod"));
		}

		[Test]
		public virtual void ConjOrPeculiarities()
		{
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ForwardEntailment, NaturalLogicRelation.ForDependencyInsertion("conj:or"));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ForwardEntailment, NaturalLogicRelation.ForDependencyInsertion("conj:or", true));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ReverseEntailment, NaturalLogicRelation.ForDependencyInsertion("conj:or", false));
		}

		[Test]
		public virtual void SomeDeletionRelations()
		{
			//    assertEquals(NaturalLogicRelation.INDEPENDENCE, NaturalLogicRelation.forDependencyDeletion("nsubj"));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ReverseEntailment, NaturalLogicRelation.ForDependencyDeletion("quantmod"));
			NUnit.Framework.Assert.AreEqual(NaturalLogicRelation.ForwardEntailment, NaturalLogicRelation.ForDependencyDeletion("amod"));
		}
	}
}
