using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Test some acronyms.</summary>
	/// <remarks>Test some acronyms. Taken mostly from the 2013 KBP results.</remarks>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class AcronymMatcherTest
	{
		[Test]
		public virtual void TestBasic()
		{
			NUnit.Framework.Assert.IsTrue(AcronymMatcher.IsAcronym("IBM", "International Business Machines".Split("\\s+")));
			NUnit.Framework.Assert.IsTrue(AcronymMatcher.IsAcronym("SIWI", "Stockholm International Water Institute".Split("\\s+")));
			NUnit.Framework.Assert.IsTrue(AcronymMatcher.IsAcronym("CBRC", "China Banking Regulatory Commission".Split("\\s+")));
			NUnit.Framework.Assert.IsTrue(AcronymMatcher.IsAcronym("ECC", "Election Complaints Commission".Split("\\s+")));
		}

		[Test]
		public virtual void TestFilterStopWords()
		{
			NUnit.Framework.Assert.IsTrue(AcronymMatcher.IsAcronym("CML", "Council of Mortgage Lenders".Split("\\s+")));
			NUnit.Framework.Assert.IsTrue(AcronymMatcher.IsAcronym("AAAS", "American Association for the Advancement of Science".Split("\\s+")));
		}

		[Test]
		public virtual void TestStripCorp()
		{
			NUnit.Framework.Assert.IsTrue(AcronymMatcher.IsAcronym("FCI", "Fake Company International Corp.".Split("\\s+")));
		}
	}
}
