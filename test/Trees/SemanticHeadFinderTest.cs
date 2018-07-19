using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class SemanticHeadFinderTest
	{
		private readonly IHeadFinder shf = new SemanticHeadFinder();

		private readonly IHeadFinder shfc = new SemanticHeadFinder(false);

		private Tree[] testTrees = new Tree[] { Tree.ValueOf("(WHNP (WHADJP (WRB How) (JJ many)) (NNS cars))"), Tree.ValueOf("(VP (VBZ is) (NP-PRD (DT a) (NN champion)))"), Tree.ValueOf("(VP (VBZ has) (VP (VBN been) (VP (VBG feeling) (ADJP (JJ unwell)))))"
			), Tree.ValueOf("(VP (VBG being) (NP (DT an) (NN idiot)))"), Tree.ValueOf("(SBAR (WHNP (WDT that)) (S (NP (PRP you)) (VP (VB understand) (NP (PRP me)))))"), Tree.ValueOf("(VP (VBD was) (VP (VBN defeated) (PP (IN by) (NP (NNP Clinton)))))"), 
			Tree.ValueOf("(VP (VBD was) (VP (VBG eating) (NP (NN pizza))))"), Tree.ValueOf("(VP (VBN been) (VP (VBN overtaken)))"), Tree.ValueOf("(VP (VBN been) (NP (DT a) (NN liar)))"), Tree.ValueOf("(VP (VBZ is) (VP (VP (VBN purged) (PP (IN of) (NP (JJ threatening) (NNS elements)))) (, ,) (VP (VBN served) (PRT (RP up)) (PP (IN in) (NP (JJ bite-sized) (NNS morsels)))) (CC and) (VP (VBN accompanied) (PP (IN by) (NP-LGS (NNS visuals))))))"
			), Tree.ValueOf("(VP (TO na) (VP (VB say) (NP (WP who)))))"), Tree.ValueOf("(VP (VBZ s) (RB not) (NP-PRD (NP (DT any)) (PP (IN of) (NP (PRP you)))))"), Tree.ValueOf("(VP (VBZ ve) (VP (VBN been) (VP (VBG feeling) (ADJP (JJ unwell)))))"), Tree
			.ValueOf("(PP (SYM -) (NP (CD 3))))"), Tree.ValueOf("(VP (`` \") (VBN forced) ('' \") (PP-CLR (IN into) (S-NOM (VP (VBG taking) (NP (DT a) (JJ hawkish) (NN line))))))") };

		private string[] shfHeads = new string[] { "NNS", "NP", "VP", "NP", "S", "VP", "VP", "VP", "NP", "VP", "VP", "NP", "VP", "SYM", "VBN" };

		private string[] shfcHeads = new string[] { "NNS", "VBZ", "VP", "VBG", "S", "VP", "VP", "VP", "VBN", "VP", "VP", "VBZ", "VP", "SYM", "VBN" };

		// complement in "I 'm not gon na say who"
		// complement of "Its not any of you
		// complement of "Ive been feeling unwell
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
		public virtual void TestRegularSemanticHeadFinder()
		{
			RunTesting(shf, shfHeads);
		}

		[NUnit.Framework.Test]
		public virtual void TestCopulaHeadSemanticHeadFinder()
		{
			RunTesting(shfc, shfcHeads);
		}
	}
}
