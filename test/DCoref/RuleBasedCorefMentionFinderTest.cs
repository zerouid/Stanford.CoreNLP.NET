using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>Test some of the routines used in the coref system</summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class RuleBasedCorefMentionFinderTest
	{
		[NUnit.Framework.Test]
		public virtual void TestFindTreeWithSmallestSpan()
		{
			Tree tree = Tree.ValueOf("(ROOT (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage))))) (. .)))");
			tree.IndexSpans();
			Tree subtree = RuleBasedCorefMentionFinder.FindTreeWithSmallestSpan(tree, 0, 2);
			NUnit.Framework.Assert.AreEqual("(NP (PRP$ My) (NN dog))", subtree.ToString());
			subtree = RuleBasedCorefMentionFinder.FindTreeWithSmallestSpan(tree, 0, 1);
			NUnit.Framework.Assert.AreEqual("My", subtree.ToString());
		}
	}
}
