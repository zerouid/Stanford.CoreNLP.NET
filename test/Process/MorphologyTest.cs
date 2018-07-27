using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Process
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class MorphologyTest
	{
		private string[] exWords = new string[] { "brethren", "ducks", "saw", "saw", "running", "making", "makking", "stopped", "xopped", "cleaner", "cleaner", "took", "bought", "am", "were", "did", "n't", "wo", "'s", "'s", "ca", "her", "her", "their"
			, "Books", "light-weight", "cease-fire", "John_William_Smith", "Dogs", "were", "AM", "'d", "'s", "'s", "ai", "sha", "them", "US", "Am", "AM", "ARE", "Was", "WERE", "was", "played", "PLAYED", "<br>", "-0800", "an", "out-rode", "viii", "b-", 
			"s", "hath", "'ll", "d", "re", "no", "r", "du" };

		private string[] exTags = new string[] { "NNS", "NNS", "VBD", "NN", "VBG", "VBG", "VBG", "VBD", "VBD", "NN", "JJR", "VBD", "VBD", "VBP", "VBD", "VBD", "RB", "MD", "VBZ", "POS", "MD", "PRP", "PRP$", "PRP$", "NNPS", "JJ", "NN", "NNP", "NNS", "VBD"
			, "VBP", "MD", "VBZ", "POS", "VBP", "MD", "PRP", "PRP", "VBP", "VBP", "VBP", "VBD", "VBD", "VBD", "VBD", "VBD", "SYM", "CD", "DT", "VBD", "FW", "AFX", "VBZ", "VBP", "MD", "MD", "VBP", "VBP", "VBP", "VBP" };

		private string[] exAnswers = new string[] { "brethren", "duck", "see", "saw", "run", "make", "makk", "stop", "xopp", "cleaner", "cleaner", "take", "buy", "be", "be", "do", "not", "will", "be", "'s", "can", "she", "she", "they", "Books", "light-weight"
			, "cease-fire", "John_William_Smith", "dog", "be", "be", "would", "be", "'s", "be", "shall", "they", "we", "be", "be", "be", "be", "be", "be", "play", "play", "<br>", "-0800", "a", "out-ride", "viii", "b-", "be", "have", "will", "would", "be"
			, "know", "be", "do" };

		[NUnit.Framework.Test]
		public virtual void TestMorph()
		{
			System.Diagnostics.Debug.Assert((exWords.Length == exTags.Length));
			System.Diagnostics.Debug.Assert((exWords.Length == exAnswers.Length));
			for (int i = 0; i < exWords.Length; i++)
			{
				WordLemmaTag ans = Morphology.LemmatizeStatic(new WordTag(exWords[i], exTags[i]));
				NUnit.Framework.Assert.AreEqual("Stemmed " + exWords[i] + '/' + exTags[i] + " to lemma " + ans.Lemma() + " versus correct " + exAnswers[i], ans.Lemma(), exAnswers[i]);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestStem()
		{
			NUnit.Framework.Assert.AreEqual("John", Morphology.StemStatic(new WordTag("John", "NNP")).Word());
			NUnit.Framework.Assert.AreEqual("Corporations", Morphology.StemStatic(new WordTag("Corporations", "NNPS")).Word());
			WordTag hunt = new WordTag("hunting", "V");
			NUnit.Framework.Assert.AreEqual("hunt", Morphology.StemStatic(hunt).Word());
			NUnit.Framework.Assert.AreEqual("hunt", Morphology.LemmatizeStatic(hunt).Lemma());
		}

		[NUnit.Framework.Test]
		public virtual void TestDunno()
		{
			NUnit.Framework.Assert.AreEqual("do", Morphology.StemStatic(new WordTag("du", "VBP")).Word());
			NUnit.Framework.Assert.AreEqual("not", Morphology.StemStatic(new WordTag("n", "RB")).Word());
			NUnit.Framework.Assert.AreEqual("know", Morphology.StemStatic(new WordTag("no", "VB")).Word());
		}

		[NUnit.Framework.Test]
		public virtual void TestDash()
		{
			Morphology morpha = new Morphology();
			morpha.Stem("b-");
		}

		[NUnit.Framework.Test]
		public virtual void TestStemStatic()
		{
			WordTag wt2 = new WordTag("objecting", "VBG");
			WordTag wt = Morphology.StemStatic(wt2);
			NUnit.Framework.Assert.AreEqual("object", wt.Word());
			wt2 = new WordTag("broken", "VBN");
			wt = Morphology.StemStatic(wt2);
			NUnit.Framework.Assert.AreEqual("break", wt.Word());
			wt2 = new WordTag("topoi", "NNS");
			wt = Morphology.StemStatic(wt2);
			NUnit.Framework.Assert.AreEqual("topos", wt.Word());
			wt2 = new WordTag("radii", "NNS");
			wt = Morphology.StemStatic(wt2);
			NUnit.Framework.Assert.AreEqual("radius", wt.Word());
		}
	}
}
