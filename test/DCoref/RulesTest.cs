using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>Test some of the "rules" which compose the coref system</summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class RulesTest
	{
		internal IList<CoreLabel> Ibm = SentenceUtils.ToCoreLabelList("IBM");

		internal IList<CoreLabel> Ibm2 = SentenceUtils.ToCoreLabelList("International", "Business", "Machines");

		internal IList<CoreLabel> Ibmm = SentenceUtils.ToCoreLabelList("IBMM");

		internal IList<CoreLabel> Mibm = SentenceUtils.ToCoreLabelList("MIBM");

		[NUnit.Framework.Test]
		public virtual void TestIsAcronym()
		{
			NUnit.Framework.Assert.IsTrue(Rules.IsAcronym(Ibm, Ibm2));
			NUnit.Framework.Assert.IsTrue(Rules.IsAcronym(Ibm2, Ibm));
			NUnit.Framework.Assert.IsFalse(Rules.IsAcronym(Ibm, Ibmm));
			NUnit.Framework.Assert.IsFalse(Rules.IsAcronym(Ibm2, Ibmm));
			NUnit.Framework.Assert.IsFalse(Rules.IsAcronym(Ibm, Mibm));
			NUnit.Framework.Assert.IsFalse(Rules.IsAcronym(Ibm2, Mibm));
		}

		[NUnit.Framework.Test]
		public virtual void TestMentionMatchesSpeakerAnnotation()
		{
			Mention g1 = new Mention(0, 0, 0, null);
			Mention m1 = new Mention(0, 0, 0, null);
			Mention m2 = new Mention(0, 0, 0, null);
			Mention m3 = new Mention(0, 0, 0, null);
			Mention m4 = new Mention(0, 0, 0, null);
			Mention m5 = new Mention(0, 0, 0, null);
			Mention m6 = new Mention(0, 0, 0, null);
			Mention m7 = new Mention(0, 0, 0, null);
			Mention m8 = new Mention(0, 0, 0, null);
			Mention g2 = new Mention(0, 0, 0, null);
			Mention g3 = new Mention(0, 0, 0, null);
			Mention g4 = new Mention(0, 0, 0, null);
			g1.headWord = new CoreLabel();
			g1.headWord.Set(typeof(CoreAnnotations.SpeakerAnnotation), "john abraham bauer");
			m1.headString = "john";
			m2.headString = "bauer";
			m3.headString = "foo";
			m4.headString = "abraham";
			m5.headString = "braham";
			m6.headString = "zabraham";
			m7.headString = "abraha";
			m8.headString = "abrahamz";
			g2.headWord = new CoreLabel();
			g2.headWord.Set(typeof(CoreAnnotations.SpeakerAnnotation), "john");
			g3.headWord = new CoreLabel();
			g3.headWord.Set(typeof(CoreAnnotations.SpeakerAnnotation), "joh");
			g4.headWord = new CoreLabel();
			g4.headWord.Set(typeof(CoreAnnotations.SpeakerAnnotation), "johnz");
			NUnit.Framework.Assert.IsTrue(Rules.AntecedentMatchesMentionSpeakerAnnotation(g1, m1));
			NUnit.Framework.Assert.IsTrue(Rules.AntecedentMatchesMentionSpeakerAnnotation(g1, m2));
			NUnit.Framework.Assert.IsFalse(Rules.AntecedentMatchesMentionSpeakerAnnotation(g1, m3));
			NUnit.Framework.Assert.IsTrue(Rules.AntecedentMatchesMentionSpeakerAnnotation(g1, m4));
			NUnit.Framework.Assert.IsFalse(Rules.AntecedentMatchesMentionSpeakerAnnotation(g1, m5));
			NUnit.Framework.Assert.IsFalse(Rules.AntecedentMatchesMentionSpeakerAnnotation(g1, m6));
			NUnit.Framework.Assert.IsFalse(Rules.AntecedentMatchesMentionSpeakerAnnotation(g1, m7));
			NUnit.Framework.Assert.IsFalse(Rules.AntecedentMatchesMentionSpeakerAnnotation(g1, m8));
			NUnit.Framework.Assert.IsTrue(Rules.AntecedentMatchesMentionSpeakerAnnotation(g2, m1));
			NUnit.Framework.Assert.IsFalse(Rules.AntecedentMatchesMentionSpeakerAnnotation(g3, m1));
			NUnit.Framework.Assert.IsFalse(Rules.AntecedentMatchesMentionSpeakerAnnotation(g4, m1));
			// not symmetrical
			// also, shouldn't blow up if the annotation isn't set
			NUnit.Framework.Assert.IsFalse(Rules.AntecedentMatchesMentionSpeakerAnnotation(m1, g1));
		}
	}
}
