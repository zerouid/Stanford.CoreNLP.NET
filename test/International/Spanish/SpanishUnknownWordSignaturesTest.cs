

namespace Edu.Stanford.Nlp.International.Spanish
{
	/// <author>Jon Gauthier</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class SpanishUnknownWordSignaturesTest
	{
		[NUnit.Framework.Test]
		public virtual void TestHasConditionalSuffix()
		{
			NUnit.Framework.Assert.IsTrue(SpanishUnknownWordSignatures.HasConditionalSuffix("debería"));
			NUnit.Framework.Assert.IsTrue(SpanishUnknownWordSignatures.HasConditionalSuffix("deberías"));
			NUnit.Framework.Assert.IsTrue(SpanishUnknownWordSignatures.HasConditionalSuffix("deberíamos"));
			NUnit.Framework.Assert.IsTrue(SpanishUnknownWordSignatures.HasConditionalSuffix("deberíais"));
			NUnit.Framework.Assert.IsTrue(SpanishUnknownWordSignatures.HasConditionalSuffix("deberían"));
			NUnit.Framework.Assert.IsFalse(SpanishUnknownWordSignatures.HasConditionalSuffix("debía"));
			NUnit.Framework.Assert.IsFalse(SpanishUnknownWordSignatures.HasConditionalSuffix("debías"));
			NUnit.Framework.Assert.IsFalse(SpanishUnknownWordSignatures.HasConditionalSuffix("debíamos"));
			NUnit.Framework.Assert.IsFalse(SpanishUnknownWordSignatures.HasConditionalSuffix("debíais"));
			NUnit.Framework.Assert.IsFalse(SpanishUnknownWordSignatures.HasConditionalSuffix("debían"));
		}

		[NUnit.Framework.Test]
		public virtual void TestHasImperfectErIrSuffix()
		{
			NUnit.Framework.Assert.IsTrue(SpanishUnknownWordSignatures.HasImperfectErIrSuffix("vivía"));
			NUnit.Framework.Assert.IsFalse(SpanishUnknownWordSignatures.HasImperfectErIrSuffix("viviría"));
		}
	}
}
