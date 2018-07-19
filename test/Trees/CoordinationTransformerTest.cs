using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Test some various error and success cases for the CoordinationTransformer</summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class CoordinationTransformerTest
	{
		internal const string SymDontMoveRb = "(ROOT (S (NP (NP (NN fire) (NN gear)) (, ,) (ADVP (RB annually)) (SYM fy) (: -)) (VP (NN fy) (: :))))";

		[NUnit.Framework.Test]
		public virtual void TestMoveRB()
		{
			Tree test = Tree.ValueOf(SymDontMoveRb);
			Tree result = CoordinationTransformer.MoveRB(test);
			NUnit.Framework.Assert.AreEqual(test.ToString(), result.ToString());
		}
	}
}
