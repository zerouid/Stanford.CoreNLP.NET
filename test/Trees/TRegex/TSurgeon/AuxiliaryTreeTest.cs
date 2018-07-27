


namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>Test the regex patterns in AuxiliaryTree.</summary>
	/// <remarks>
	/// Test the regex patterns in AuxiliaryTree.  The tree itself will be
	/// tested indirectly by seeing that Tsurgeon works, via TsurgeonTest.
	/// </remarks>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class AuxiliaryTreeTest
	{
		[NUnit.Framework.Test]
		public virtual void TestNamePattern()
		{
			RunNamePatternTrue("abcd=efgh", "abcd", "efgh");
			RunNamePatternFalse("abcd\\=efgh");
			RunNamePatternTrue("abcd\\\\=efgh", "abcd\\\\", "efgh");
			RunNamePatternFalse("abcd\\\\\\=efgh");
			RunNamePatternTrue("abcd\\\\\\\\=efgh", "abcd\\\\\\\\", "efgh");
		}

		public static void RunNamePatternFalse(string input)
		{
			Matcher m = AuxiliaryTree.namePattern.Matcher(input);
			NUnit.Framework.Assert.IsFalse(m.Find());
		}

		public static void RunNamePatternTrue(string input, string leftover, string name)
		{
			Matcher m = AuxiliaryTree.namePattern.Matcher(input);
			NUnit.Framework.Assert.IsTrue(m.Find());
			NUnit.Framework.Assert.AreEqual(leftover, m.Group(1));
			NUnit.Framework.Assert.AreEqual(name, m.Group(2));
		}

		[NUnit.Framework.Test]
		public virtual void TestUnescape()
		{
			NUnit.Framework.Assert.AreEqual("asdf", AuxiliaryTree.Unescape("asdf"));
			NUnit.Framework.Assert.AreEqual("asdf=", AuxiliaryTree.Unescape("asdf\\="));
			NUnit.Framework.Assert.AreEqual("asdf\\=", AuxiliaryTree.Unescape("asdf\\\\="));
		}
	}
}
