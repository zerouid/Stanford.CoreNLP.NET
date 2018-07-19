using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ModCollinsHeadFinderTest
	{
		private IHeadFinder hf = new ModCollinsHeadFinder();

		private Tree[] testTrees = new Tree[] { Tree.ValueOf("(PRN (: --) (S-ADV (NP-SBJ (DT that)) (VP (VBZ is))) (, ,) (SQ (MD can) (NP-SBJ (NP (DT the) (NN network)) (ADVP (RB alone))) (VP (VB make) (NP (DT a) (NN profit)) (PP-CLR (IN on) (NP (PRP it))))))))))))"
			), Tree.ValueOf("(NP (NP (NML (DT the) (NNP Secretary)) (POS 's)) (NML (`` ``) (JJ discretionary) (NN fund) (, ,) ('' '')))"), Tree.ValueOf("(S (NP (NP (NNP Sam)) (, ,) (NP (PRP$ my) (NN brother)) (, ,)) (VP (VBZ eats) (NP (JJ red) (NN meat))) (. .))"
			), Tree.ValueOf("(NP (NP (DT The) (JJ Australian) (NNP Broadcasting) (NNP Corporation)) (PRN (-LRB- -LRB-) (NP (NNP ABC)) (-RRB- -RRB-)) (. .))"), Tree.ValueOf("(PRN (-LRB- -LRB-) (NP (NNP ABC)) (-RRB- -RRB-))"), Tree.ValueOf("(NP (. .) (. .) (VBZ eats) (. .) (. .))"
			), Tree.ValueOf("(PP (SYM -) (NP (CD 3))))") };

		private string[] hfHeads = new string[] { "SQ", "NML", "VP", "NP", "NP", "VBZ", "SYM" };

		// junk tree just for testing setCategoriesToAvoid (NP never matches VBZ but shouldn't pick the punctuation marks)
		// Tree.valueOf("(FOO (BAR a) (BAZ b))")  // todo: If change to always do something rather than Exception (and edit hfFeads array)
		// , "BAR"
		private void RunTesting(IHeadFinder hf, string[] heads)
		{
			NUnit.Framework.Assert.AreEqual("Test arrays out of balance", testTrees.Length, heads.Length);
			for (int i = 0; i < testTrees.Length; i++)
			{
				Tree h = hf.DetermineHead(testTrees[i]);
				string headCat = h.Value();
				NUnit.Framework.Assert.AreEqual("Wrong head found", heads[i], headCat);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestModCollinsHeadFinder()
		{
			RunTesting(hf, hfHeads);
		}
	}
}
