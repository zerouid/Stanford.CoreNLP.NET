using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TreebankLanguagePackTest
	{
		[NUnit.Framework.Test]
		public virtual void TestBasicCategory()
		{
			ITreebankLanguagePack tlp = new PennTreebankLanguagePack();
			NUnit.Framework.Assert.AreEqual("NP", tlp.BasicCategory("NP-SBJ-R"));
			NUnit.Framework.Assert.AreEqual("-", tlp.BasicCategory("-"));
			NUnit.Framework.Assert.AreEqual("-LRB-", tlp.BasicCategory("-LRB-"));
			NUnit.Framework.Assert.AreEqual("-", tlp.BasicCategory("--PU"));
			NUnit.Framework.Assert.AreEqual("-", tlp.BasicCategory("--PU-U"));
			NUnit.Framework.Assert.AreEqual("-LRB-", tlp.BasicCategory("-LRB--PU"));
			NUnit.Framework.Assert.AreEqual("-LRB-", tlp.BasicCategory("-LRB--PU-U"));
		}
	}
}
