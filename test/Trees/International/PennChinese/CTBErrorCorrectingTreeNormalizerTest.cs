using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class CTBErrorCorrectingTreeNormalizerTest
	{
		[NUnit.Framework.Test]
		public virtual void TestNormalDelete()
		{
			string input = "(ROOT (IP (FLR (PU 〈) (VV ｔｕｒｎ) (PU 〉)) (PP (ADVP (AD 就)) (PP (P 在) (NP (CP (IP (NP (NR 北韩)) (VP (DVP (VP (VA " + "积极)) (DEV 的)) (VP (VV 走向) (NP (NN 国际) (NN 社会))))) (DEC 的)) (NP (NN 同时))))) (PU ，) (NP (CP (IP (VP (ADVP (AD" +
				 " 刚刚)) (VP (VV 渡过) (NP (QP (CD 一) (CLP (M 场))) (NP (NN 大选) (NN 危机)))))) (DEC 的)) (NP (NR 南斯拉夫))) (VP (ADVP" + " (AD 也)) (ADVP (AD 在)) (VP (VV 寻求) (NP (DNP (LCP (NP (NN 国际)) (LC 间)) (DEG 的)) (NP (NN 协助))))) (PU 。)))";
			string output = "(ROOT (IP (PP (ADVP (AD 就)) (PP (P 在) (NP (CP (IP (NP (NR 北韩)) (VP (DVP (VP (VA " + "积极)) (DEV 的)) (VP (VV 走向) (NP (NN 国际) (NN 社会))))) (DEC 的)) (NP (NN 同时))))) (PU ，) (NP (CP (IP (VP (ADVP (AD" + " 刚刚)) (VP (VV 渡过) (NP (QP (CD 一) (CLP (M 场))) (NP (NN 大选) (NN 危机)))))) (DEC 的)) (NP (NR 南斯拉夫))) (VP (ADVP"
				 + " (AD 也)) (ADVP (AD 在)) (VP (VV 寻求) (NP (DNP (LCP (NP (NN 国际)) (LC 间)) (DEG 的)) (NP (NN 协助))))) (PU 。)))";
			RunTest(input, output);
		}

		[NUnit.Framework.Test]
		public virtual void TestFixSplitElement()
		{
			string input = "(ROOT (IP (IP (NP (NN 下面)) (VP (VV 请) (VP (VV 听) (NP (DNP (NP (NN 报道)) (DEG 的)) (ADJP (JJ 详细)) (NP (NN 内容))))" + ")) (PU ：) (FLR (PU 〈)) (VV ｔｕｒｎ) (PU 〉) (IP (NP (NP (NP (NR 法国)) (NP (NN 外交) (NN 部长))) (NP (NR 韦里德纳))) " + "(PU ，) (VP (VC 是) (NP (PP (P 自从) (LCP (IP (NP (NT 去年)) (NP (NR 北约)) (VP (VV 轰炸) (NP (NR 南斯拉夫)))) (LC 以来)"
				 + ")) (PU ，) (QP (OD 第一) (CLP (M 位))) (CP (IP (VP (VV 访问) (NP (NP (NR 南斯拉夫)) (NP (NN 首都))))) (DEC 的)) (ADJP (JJ" + " 主要)) (NP (NN 西方) (NN 国家) (NN 外交官))))) (PU 。)))";
			string output = "(ROOT (IP (IP (NP (NN 下面)) (VP (VV 请) (VP (VV 听) (NP (DNP (NP (NN 报道)) (DEG 的)) (ADJP (JJ 详细)) (NP (NN 内容))))" + ")) (PU ：) (IP (NP (NP (NP (NR 法国)) (NP (NN 外交) (NN 部长))) (NP (NR 韦里德纳))) " + "(PU ，) (VP (VC 是) (NP (PP (P 自从) (LCP (IP (NP (NT 去年)) (NP (NR 北约)) (VP (VV 轰炸) (NP (NR 南斯拉夫)))) (LC 以来)"
				 + ")) (PU ，) (QP (OD 第一) (CLP (M 位))) (CP (IP (VP (VV 访问) (NP (NP (NR 南斯拉夫)) (NP (NN 首都))))) (DEC 的)) (ADJP (JJ" + " 主要)) (NP (NN 西方) (NN 国家) (NN 外交官))))) (PU 。)))";
			RunTest(input, output);
		}

		[NUnit.Framework.Test]
		public virtual void TestAnotherSplit()
		{
			string input = "(ROOT (IP (LCP (IP (FLR (PU ＜) (NR Ｅｎｇｌｉｓｈ) (PU ＞)) (NP (NP (NR ＡＰＥＣ) (PU ＜)) (FLR (PU ／) (NT Ｅｎｇｌｉｓｈ) (PU ＞)) (NP (NN 会议))) (VP (VV 举行))) (LC 前)) (PU ，) (NP (NP (NP (NR 日本)) (NP (NN 首相))) (NP (NR 小泉纯一郎))) (VP (VP (QP (OD 第五) (CLP (M 度))) (VP (VV 参拜) (NP (NN 靖国神社)))) (PU ，) (VP (VV 受到) (IP (NP (NP (NR 中) (NR 韩) (ETC 等)) (NP (NR 亚洲)) (NP (NN 国家))) (VP (ADJP (AD 严厉)) (VP (VV 谴责)))))) (PU 。)))";
			string output = "(ROOT (IP (LCP (IP (NP (NP (NR ＡＰＥＣ)) (NP (NN 会议))) (VP (VV 举行))) (LC 前)) (PU ，) (NP (NP (NP (NR 日本)) (NP (NN 首相))) (NP (NR 小泉纯一郎))) (VP (VP (QP (OD 第五) (CLP (M 度))) (VP (VV 参拜) (NP (NN 靖国神社)))) (PU ，) (VP (VV 受到) (IP (NP (NP (NR 中) (NR 韩) (ETC 等)) (NP (NR 亚洲)) (NP (NN 国家))) (VP (ADJP (AD 严厉)) (VP (VV 谴责)))))) (PU 。)))";
			RunTest(input, output);
		}

		[NUnit.Framework.Test]
		public virtual void TestNothingLeftTree()
		{
			string input = "( (FLR (PU ＜) (VA ｆｏｒｅｉｇｎ) (PU ＞) (PU （) (PU （) (PU ）) (PU ）) (PU ＜) (PU ／) (VA ｆｏｒｅｉｇｎ) (PU ＞)))";
			RunTest(input, null);
		}

		private static void RunTest(string input, string output)
		{
			Tree inputTree = Tree.ValueOf(input);
			ITreeTransformer tt = new CTBErrorCorrectingTreeNormalizer(false, false, false, false);
			Tree outputTree = tt.Apply(inputTree);
			if (output == null)
			{
				NUnit.Framework.Assert.IsNull(outputTree);
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(output, outputTree.ToString());
			}
		}
	}
}
