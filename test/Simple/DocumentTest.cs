using System.Collections.Generic;

using NUnit.Framework;


namespace Edu.Stanford.Nlp.Simple
{
	/// <summary>
	/// A test for aspects of
	/// <see cref="Document"/>
	/// which do not require loading the NLP models.
	/// </summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class DocumentTest
	{
		[Test]
		public virtual void TestCreateFromText()
		{
			Document doc = new Document("the quick brown fox jumped over the lazy dog");
			NUnit.Framework.Assert.IsNotNull(doc);
		}

		[Test]
		public virtual void TestText()
		{
			Document doc = new Document("the quick brown fox jumped over the lazy dog");
			NUnit.Framework.Assert.AreEqual("the quick brown fox jumped over the lazy dog", doc.Text());
		}

		[Test]
		public virtual void TestDocid()
		{
			Document doc = new Document("the quick brown fox jumped over the lazy dog");
			NUnit.Framework.Assert.AreEqual(Optional.Empty<string>(), doc.Docid());
			NUnit.Framework.Assert.AreEqual(Optional.Of("foo"), doc.SetDocid("foo").Docid());
		}

		[Test]
		public virtual void TestSentences()
		{
			Document doc = new Document("the quick brown fox jumped over the lazy dog. The lazy dog was not impressed.");
			IList<Sentence> sentences = doc.Sentences();
			NUnit.Framework.Assert.AreEqual(2, sentences.Count);
			NUnit.Framework.Assert.AreEqual("the quick brown fox jumped over the lazy dog.", sentences[0].Text());
			NUnit.Framework.Assert.AreEqual("The lazy dog was not impressed.", sentences[1].Text());
		}
	}
}
