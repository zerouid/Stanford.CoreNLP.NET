using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Very simple test case - checks that reading from a reader produces
	/// the right number of trees with the right text.
	/// </summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class PennTreeReaderTest
	{
		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestRead()
		{
			string treeText = "(1 (2 This)) (3 (4 is) (5 a)) (6 (\\* small) (7 \\/test))";
			StringReader reader = new StringReader(treeText);
			PennTreeReader treeReader = new PennTreeReader(reader);
			string[] expected = new string[] { "(1 (2 This))", "(3 (4 is) (5 a))", "(6 (* small) (7 /test))" };
			for (int i = 0; i < expected.Length; ++i)
			{
				Tree tree = treeReader.ReadTree();
				NUnit.Framework.Assert.IsTrue(tree != null);
				NUnit.Framework.Assert.AreEqual(expected[i], tree.ToString());
			}
			Tree tree_1 = treeReader.ReadTree();
			NUnit.Framework.Assert.IsFalse(tree_1 != null);
		}
	}
}
