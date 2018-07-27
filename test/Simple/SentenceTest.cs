using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;

using NUnit.Framework;


namespace Edu.Stanford.Nlp.Simple
{
	/// <summary>
	/// A test for aspects of
	/// <see cref="Sentence"/>
	/// which do not require loading the NLP models.
	/// </summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class SentenceTest
	{
		[Test]
		public virtual void TestCreateFromText()
		{
			Sentence sent = new Sentence("the quick brown fox jumped over the lazy dog");
			NUnit.Framework.Assert.IsNotNull(sent);
		}

		[Test]
		public virtual void TestText()
		{
			Sentence sent = new Sentence("the quick brown fox jumped over the lazy dog");
			NUnit.Framework.Assert.AreEqual("the quick brown fox jumped over the lazy dog", sent.Text());
		}

		[Test]
		public virtual void TestLength()
		{
			Sentence sent = new Sentence("the quick brown fox jumped over the lazy dog");
			NUnit.Framework.Assert.AreEqual(9, sent.Length());
		}

		[Test]
		public virtual void TestDocumentLinking()
		{
			Sentence sent = new Sentence("the quick brown fox jumped over the lazy dog");
			NUnit.Framework.Assert.AreEqual(sent, sent.document.Sentence(0));
		}

		[Test]
		public virtual void TestBasicTokenization()
		{
			Sentence sent = new Sentence("the quick brown fox jumped over the lazy dog.");
			NUnit.Framework.Assert.AreEqual("the", sent.Word(0));
			NUnit.Framework.Assert.AreEqual("quick", sent.Word(1));
			NUnit.Framework.Assert.AreEqual("dog", sent.Word(8));
			NUnit.Framework.Assert.AreEqual(".", sent.Word(9));
		}

		[Test]
		public virtual void TestWeirdTokens()
		{
			Sentence sent = new Sentence("United States of America (USA) it's a country.");
			NUnit.Framework.Assert.AreEqual("-LRB-", sent.Word(4));
			NUnit.Framework.Assert.AreEqual("-RRB-", sent.Word(6));
			NUnit.Framework.Assert.AreEqual("'s", sent.Word(8));
		}

		[Test]
		public virtual void TestOriginalText()
		{
			Sentence sent = new Sentence("United States of America (USA) it's a country.");
			NUnit.Framework.Assert.AreEqual("(", sent.OriginalText(4));
			NUnit.Framework.Assert.AreEqual(")", sent.OriginalText(6));
			NUnit.Framework.Assert.AreEqual("it", sent.OriginalText(7));
			NUnit.Framework.Assert.AreEqual("'s", sent.OriginalText(8));
		}

		[Test]
		public virtual void TestCharacterOffsets()
		{
			Sentence sent = new Sentence("United States of America (USA) it's a country.");
			NUnit.Framework.Assert.AreEqual(0, sent.CharacterOffsetBegin(0));
			NUnit.Framework.Assert.AreEqual(6, sent.CharacterOffsetEnd(0));
			NUnit.Framework.Assert.AreEqual(7, sent.CharacterOffsetBegin(1));
			NUnit.Framework.Assert.AreEqual(25, sent.CharacterOffsetBegin(4));
			NUnit.Framework.Assert.AreEqual(26, sent.CharacterOffsetEnd(4));
		}

		[Test]
		public virtual void TestSentenceIndex()
		{
			Sentence sent = new Sentence("the quick brown fox jumped over the lazy dog");
			NUnit.Framework.Assert.AreEqual(0, sent.SentenceIndex());
			Document doc = new Document("the quick brown fox jumped over the lazy dog. The lazy dog was not impressed.");
			IList<Sentence> sentences = doc.Sentences();
			NUnit.Framework.Assert.AreEqual(0, sentences[0].SentenceIndex());
			NUnit.Framework.Assert.AreEqual(1, sentences[1].SentenceIndex());
		}

		[Test]
		public virtual void TestSentenceTokenOffsets()
		{
			Sentence sent = new Sentence("the quick brown fox jumped over the lazy dog");
			NUnit.Framework.Assert.AreEqual(0, sent.SentenceTokenOffsetBegin());
			Document doc = new Document("the quick brown fox jumped over the lazy dog. The lazy dog was not impressed.");
			IList<Sentence> sentences = doc.Sentences();
			NUnit.Framework.Assert.AreEqual(0, sentences[0].SentenceTokenOffsetBegin());
			NUnit.Framework.Assert.AreEqual(10, sentences[0].SentenceTokenOffsetEnd());
			NUnit.Framework.Assert.AreEqual(10, sentences[1].SentenceTokenOffsetBegin());
			NUnit.Framework.Assert.AreEqual(17, sentences[1].SentenceTokenOffsetEnd());
		}

		[Test]
		public virtual void TestFromCoreMapCrashCheck()
		{
			StanfordCoreNLP pipeline = new StanfordCoreNLP(new _Properties_107());
			Annotation ann = new Annotation("This is a sentence.");
			pipeline.Annotate(ann);
			ICoreMap map = ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[0];
			new Sentence(map);
		}

		private sealed class _Properties_107 : Properties
		{
			public _Properties_107()
			{
				{
					this.SetProperty("annotators", "tokenize,ssplit");
				}
			}
		}

		[Test]
		public virtual void TestFromCoreMapCorrectnessCheck()
		{
			StanfordCoreNLP pipeline = new StanfordCoreNLP(new _Properties_119());
			Annotation ann = new Annotation("This is a sentence.");
			pipeline.Annotate(ann);
			ICoreMap map = ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[0];
			Sentence s = new Sentence(map);
			NUnit.Framework.Assert.AreEqual(ann.Get(typeof(CoreAnnotations.TextAnnotation)), s.Text());
			NUnit.Framework.Assert.AreEqual("This", s.Word(0));
			NUnit.Framework.Assert.AreEqual(5, s.Length());
		}

		private sealed class _Properties_119 : Properties
		{
			public _Properties_119()
			{
				{
					this.SetProperty("annotators", "tokenize,ssplit");
				}
			}
		}

		[Test]
		public virtual void TestTokenizeWhitespaceSimple()
		{
			Sentence s = new Sentence(new _List_134());
			NUnit.Framework.Assert.AreEqual("foo", s.Word(0));
			NUnit.Framework.Assert.AreEqual("bar", s.Word(1));
		}

		private sealed class _List_134 : List<string>
		{
			public _List_134()
			{
				{
					this.Add("foo");
					this.Add("bar");
				}
			}
		}

		[Test]
		public virtual void TestTokenizeWhitespaceWithSpaces()
		{
			Sentence s = new Sentence(new _List_141());
			NUnit.Framework.Assert.AreEqual("foo", s.Word(0));
			NUnit.Framework.Assert.AreEqual("with whitespace", s.Word(1));
			NUnit.Framework.Assert.AreEqual("baz", s.Word(2));
		}

		private sealed class _List_141 : List<string>
		{
			public _List_141()
			{
				{
					this.Add("foo");
					this.Add("with whitespace");
					this.Add("baz");
				}
			}
		}
	}
}
