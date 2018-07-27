

namespace Edu.Stanford.Nlp.Util
{
	/// <author>Sebastian Riedel</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class BeamTest
	{
		protected internal Beam<ScoredObject<string>> beam;

		protected internal ScoredObject<string> object1;

		protected internal ScoredObject<string> object0;

		protected internal ScoredObject<string> object2;

		protected internal ScoredObject<string> object3;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			beam = new Beam<ScoredObject<string>>(2, ScoredComparator.AscendingComparator);
			object1 = new ScoredObject<string>("1", 1.0);
			object2 = new ScoredObject<string>("2", 2.0);
			object3 = new ScoredObject<string>("3", 3.0);
			object0 = new ScoredObject<string>("0", 0.0);
			beam.Add(object1);
			beam.Add(object2);
			beam.Add(object3);
			beam.Add(object0);
		}

		[NUnit.Framework.Test]
		public virtual void TestSize()
		{
			NUnit.Framework.Assert.AreEqual(2, beam.Count);
		}

		[NUnit.Framework.Test]
		public virtual void TestContent()
		{
			NUnit.Framework.Assert.IsTrue(beam.Contains(object2));
			NUnit.Framework.Assert.IsTrue(beam.Contains(object3));
			NUnit.Framework.Assert.IsFalse(beam.Contains(object1));
			NUnit.Framework.Assert.IsFalse(beam.Contains(object0));
		}
	}
}
