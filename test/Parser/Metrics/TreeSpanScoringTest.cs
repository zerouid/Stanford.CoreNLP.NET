using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Metrics
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TreeSpanScoringTest
	{
		internal ITreebankLanguagePack tlp = new PennTreebankLanguagePack();

		[NUnit.Framework.Test]
		public virtual void TestNoErrors()
		{
			Tree t1 = Tree.ValueOf("(ROOT (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage))))) (. .)))");
			NUnit.Framework.Assert.AreEqual(0, TreeSpanScoring.CountSpanErrors(tlp, t1, t1));
		}

		[NUnit.Framework.Test]
		public virtual void TestTagErrors()
		{
			Tree t1 = Tree.ValueOf("(ROOT (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage))))) (. .)))");
			Tree t2 = Tree.ValueOf("(ROOT (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (VBG sausage))))) (. .)))");
			NUnit.Framework.Assert.AreEqual(2, TreeSpanScoring.CountSpanErrors(tlp, t1, t2));
			NUnit.Framework.Assert.AreEqual(2, TreeSpanScoring.CountSpanErrors(tlp, t2, t1));
		}

		[NUnit.Framework.Test]
		public virtual void TestMislabeledSpans()
		{
			Tree t1 = Tree.ValueOf("(ROOT (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage))))) (. .)))");
			Tree t2 = Tree.ValueOf("(ROOT (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (ADVP (VP (VBG eating) (NP (NN sausage))))) (. .)))");
			NUnit.Framework.Assert.AreEqual(2, TreeSpanScoring.CountSpanErrors(tlp, t1, t2));
			NUnit.Framework.Assert.AreEqual(2, TreeSpanScoring.CountSpanErrors(tlp, t2, t1));
		}

		[NUnit.Framework.Test]
		public virtual void TestExtraSpan()
		{
			Tree t1 = Tree.ValueOf("(ROOT (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage))))) (. .)))");
			Tree t2 = Tree.ValueOf("(ROOT (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (ADVP (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage)))))) (. .)))");
			NUnit.Framework.Assert.AreEqual(1, TreeSpanScoring.CountSpanErrors(tlp, t1, t2));
		}

		[NUnit.Framework.Test]
		public virtual void TestMissingSpan()
		{
			Tree t1 = Tree.ValueOf("(ROOT (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (S (VP (VBG eating) (NP (NN sausage))))) (. .)))");
			Tree t2 = Tree.ValueOf("(ROOT (S (NP (PRP$ My) (NN dog)) (ADVP (RB also)) (VP (VBZ likes) (VP (VBG eating) (NP (NN sausage)))) (. .)))");
			NUnit.Framework.Assert.AreEqual(1, TreeSpanScoring.CountSpanErrors(tlp, t1, t2));
		}
	}
}
