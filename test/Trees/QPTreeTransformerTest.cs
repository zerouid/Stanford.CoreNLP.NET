using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Tests some of the various operations performed by the QPTreeTransformer.</summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class QPTreeTransformerTest
	{
		[NUnit.Framework.Test]
		public virtual void TestMoney()
		{
			string input = "(ROOT (S (NP (DT This)) (VP (VBZ costs) (NP (QP ($ $) (CD 1) (CD million)))) (. .)))";
			string output = "(ROOT (S (NP (DT This)) (VP (VBZ costs) (NP (QP ($ $) (QP (CD 1) (CD million))))) (. .)))";
			RunTest(input, output);
		}

		[NUnit.Framework.Test]
		public virtual void TestMoneyOrMore()
		{
			string input = "(ROOT (S (NP (DT This)) (VP (VBZ costs) (NP (QP ($ $) (CD 1) (CD million) (CC or) (JJR more)))) (. .)))";
			// TODO: NP for the right?
			string output = "(ROOT (S (NP (DT This)) (VP (VBZ costs) (NP (QP (QP ($ $) (QP (CD 1) (CD million))) (CC or) (NP (JJR more))))) (. .)))";
			RunTest(input, output);
			// First it gets flattened, then the CC gets broken up, but the overall result should be the same
			input = "(ROOT (S (NP (DT This)) (VP (VBZ costs) (NP (QP ($ $) (CD 1) (CD million)) (QP (CC or) (JJR more)))) (. .)))";
			RunTest(input, output);
		}

		[NUnit.Framework.Test]
		public virtual void TestCompoundModifiers()
		{
			string input = "(ROOT (S (NP (NP (DT a) (NN stake)) (PP (IN of) (NP (QP (RB just) (IN under) (CD 30)) (NN %))))))";
			string output = "(ROOT (S (NP (NP (DT a) (NN stake)) (PP (IN of) (NP (QP (XS (RB just) (IN under)) (CD 30)) (NN %))))))";
			RunTest(input, output);
		}

		private static void OutputResults(string input, string output)
		{
			Tree inputTree = Tree.ValueOf(input);
			System.Console.Error.WriteLine(inputTree);
			QPTreeTransformer qp = new QPTreeTransformer();
			Tree outputTree = qp.QPtransform(inputTree);
			System.Console.Error.WriteLine(outputTree);
			System.Console.Error.WriteLine(output);
		}

		private static void RunTest(string input, string output)
		{
			Tree inputTree = Tree.ValueOf(input);
			QPTreeTransformer qp = new QPTreeTransformer();
			Tree outputTree = qp.QPtransform(inputTree);
			NUnit.Framework.Assert.AreEqual(output, outputTree.ToString());
		}
	}
}
