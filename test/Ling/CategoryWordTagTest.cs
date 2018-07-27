

namespace Edu.Stanford.Nlp.Ling
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class CategoryWordTagTest
	{
		[NUnit.Framework.Test]
		public virtual void TestCopy()
		{
			CategoryWordTag tag = new CategoryWordTag("A", "B", "C");
			NUnit.Framework.Assert.AreEqual("A", tag.Category());
			NUnit.Framework.Assert.AreEqual("B", tag.Word());
			NUnit.Framework.Assert.AreEqual("C", tag.Tag());
			CategoryWordTag tag2 = new CategoryWordTag(tag);
			NUnit.Framework.Assert.AreEqual("A", tag2.Category());
			NUnit.Framework.Assert.AreEqual("B", tag2.Word());
			NUnit.Framework.Assert.AreEqual("C", tag2.Tag());
		}
	}
}
