using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class SpanishUnknownWordModelTest
	{
		private SpanishUnknownWordModel uwm;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			// Build dummy UWM
			Options op = new Options();
			op.lexOptions.useUnknownWordSignatures = 1;
			IIndex<string> wordIndex = new HashIndex<string>();
			IIndex<string> tagIndex = new HashIndex<string>();
			uwm = new SpanishUnknownWordModel(op, new BaseLexicon(op, wordIndex, tagIndex), wordIndex, tagIndex, new ClassicCounter<IntTaggedWord>());
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestGetSignature()
		{
			NUnit.Framework.Assert.AreEqual("UNK-cond-c", uwm.GetSignature("marcaría", 0));
			NUnit.Framework.Assert.AreEqual("UNK-imp-c", uwm.GetSignature("marcaba", 0));
			NUnit.Framework.Assert.AreEqual("UNK-imp-c", uwm.GetSignature("marcábamos", 0));
			NUnit.Framework.Assert.AreEqual("UNK-imp-c", uwm.GetSignature("vivías", 0));
			NUnit.Framework.Assert.AreEqual("UNK-imp-c", uwm.GetSignature("vivíamos", 0));
			NUnit.Framework.Assert.AreEqual("UNK-inf-c", uwm.GetSignature("brindar", 0));
			NUnit.Framework.Assert.AreEqual("UNK-adv-c", uwm.GetSignature("rápidamente", 0));
			// Broad-coverage patterns
			NUnit.Framework.Assert.AreEqual("UNK-vb1p-c", uwm.GetSignature("mandamos", 0));
			NUnit.Framework.Assert.AreEqual("UNK-s-c", uwm.GetSignature("últimos", 0));
			NUnit.Framework.Assert.AreEqual("UNK-ger-c", uwm.GetSignature("marcando", 0));
			NUnit.Framework.Assert.AreEqual("UNK-s-c", uwm.GetSignature("marcados", 0));
		}
	}
}
