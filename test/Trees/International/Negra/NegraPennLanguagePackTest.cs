using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Negra
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class NegraPennLanguagePackTest
	{
		[NUnit.Framework.Test]
		public virtual void TestBasicCategory()
		{
			NegraPennLanguagePack lp1 = new NegraPennLanguagePack(false);
			// leave(some)GF=false
			NegraPennLanguagePack lp2 = new NegraPennLanguagePack(true);
			// leave(some)GF = true
			NegraPennTreeReaderFactory trf01 = new NegraPennTreeReaderFactory(0, false, false, lp1);
			// do nothing
			NegraPennTreeReaderFactory trf02 = new NegraPennTreeReaderFactory(0, false, false, lp2);
			NegraPennTreeReaderFactory trf11 = new NegraPennTreeReaderFactory(1, false, false, lp1);
			// category and function
			NegraPennTreeReaderFactory trf12 = new NegraPennTreeReaderFactory(1, false, false, lp2);
			NegraPennTreeReaderFactory trf21 = new NegraPennTreeReaderFactory(2, false, false, lp1);
			// just category
			NegraPennTreeReaderFactory trf22 = new NegraPennTreeReaderFactory(2, false, false, lp2);
			string tree = "( (S (NE-SB Kronos) (VAFIN-HD haben) (VP-OC (PP-MO (APPR-AC mit) (PPOSAT-NK ihrer) (NN-NK Musik)) (NN-OA Brücken) (VVPP-HD geschlagen))) ($. .))";
			string ans1 = "(ROOT (S (NE-SB Kronos) (VAFIN-HD haben) (VP-OC (PP-MO (APPR-AC mit) (PPOSAT-NK ihrer) (NN-NK Musik)) (NN-OA Brücken) (VVPP-HD geschlagen)) ($. .)))";
			string ans21 = "(ROOT (S (NE Kronos) (VAFIN haben) (VP (PP (APPR mit) (PPOSAT ihrer) (NN Musik)) (NN Brücken) (VVPP geschlagen)) ($. .)))";
			string ans22 = "(ROOT (S (NE-SB Kronos) (VAFIN haben) (VP (PP (APPR mit) (PPOSAT ihrer) (NN Musik)) (NN-OA Brücken) (VVPP geschlagen)) ($. .)))";
			Tree t01 = Tree.ValueOf(tree, trf01);
			Tree t02 = Tree.ValueOf(tree, trf02);
			Tree t11 = Tree.ValueOf(tree, trf11);
			Tree t12 = Tree.ValueOf(tree, trf12);
			Tree t21 = Tree.ValueOf(tree, trf21);
			Tree t22 = Tree.ValueOf(tree, trf22);
			NUnit.Framework.Assert.AreEqual("T01", ans1, t01.ToString());
			NUnit.Framework.Assert.AreEqual("T02", ans1, t02.ToString());
			NUnit.Framework.Assert.AreEqual("T11", ans1, t11.ToString());
			NUnit.Framework.Assert.AreEqual("T12", ans1, t12.ToString());
			NUnit.Framework.Assert.AreEqual("T21", ans21, t21.ToString());
			NUnit.Framework.Assert.AreEqual("T22", ans22, t22.ToString());
			string ans = lp1.BasicCategory("---CJ");
			NUnit.Framework.Assert.AreEqual("BC1", "-", ans);
		}
	}
}
