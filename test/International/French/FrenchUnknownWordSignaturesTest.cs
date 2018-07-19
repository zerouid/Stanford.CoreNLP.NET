using Sharpen;

namespace Edu.Stanford.Nlp.International.French
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class FrenchUnknownWordSignaturesTest
	{
		[NUnit.Framework.Test]
		public virtual void TestHasPunc()
		{
			NUnit.Framework.Assert.AreEqual("-hpunc", FrenchUnknownWordSignatures.HasPunc("Yes!"));
			NUnit.Framework.Assert.AreEqual("-hpunc", FrenchUnknownWordSignatures.HasPunc("["));
			NUnit.Framework.Assert.AreEqual("-hpunc", FrenchUnknownWordSignatures.HasPunc("40%"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.HasPunc("B"));
			NUnit.Framework.Assert.AreEqual("-hpunc", FrenchUnknownWordSignatures.HasPunc("BQ_BD"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.HasPunc("BQBD"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.HasPunc("0"));
			NUnit.Framework.Assert.AreEqual("-hpunc", FrenchUnknownWordSignatures.HasPunc("\\"));
			NUnit.Framework.Assert.AreEqual("-hpunc", FrenchUnknownWordSignatures.HasPunc("]aeiou"));
			NUnit.Framework.Assert.AreEqual("-hpunc", FrenchUnknownWordSignatures.HasPunc("]"));
			NUnit.Framework.Assert.AreEqual("-hpunc", FrenchUnknownWordSignatures.HasPunc("÷"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.HasPunc("ø"));
		}

		[NUnit.Framework.Test]
		public virtual void TestIsPunc()
		{
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.IsPunc("Yes!"));
			NUnit.Framework.Assert.AreEqual("-ipunc", FrenchUnknownWordSignatures.IsPunc("["));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.IsPunc("40%"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.IsPunc("B"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.IsPunc("BQ_BD"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.IsPunc("BQBD"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.IsPunc("0"));
			NUnit.Framework.Assert.AreEqual("-ipunc", FrenchUnknownWordSignatures.IsPunc("\\"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.IsPunc("]aeiou"));
			NUnit.Framework.Assert.AreEqual("-ipunc", FrenchUnknownWordSignatures.IsPunc("]"));
			NUnit.Framework.Assert.AreEqual("-ipunc", FrenchUnknownWordSignatures.IsPunc("÷"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.IsPunc("ø"));
		}

		[NUnit.Framework.Test]
		public virtual void TestIsAllCaps()
		{
			NUnit.Framework.Assert.AreEqual("-allcap", FrenchUnknownWordSignatures.IsAllCaps("YO"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.IsAllCaps("\\\\"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.IsAllCaps("0D"));
			NUnit.Framework.Assert.AreEqual(string.Empty, FrenchUnknownWordSignatures.IsAllCaps("×"));
			NUnit.Framework.Assert.AreEqual("-allcap", FrenchUnknownWordSignatures.IsAllCaps("ÀÅÆÏÜÝÞ"));
		}
	}
}
