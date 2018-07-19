using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Java.Util.Stream;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	public class CleanXmlAnnotatorTest
	{
		private static IAnnotator ptbInvertible;

		private static IAnnotator ptbNotInvertible;

		private static IAnnotator cleanXmlAllTags;

		private static IAnnotator cleanXmlSomeTags;

		private static IAnnotator cleanXmlEndSentences;

		private static IAnnotator cleanXmlWithFlaws;

		private static IAnnotator wtsSplitter;

		// = null;
		// = null;
		// = null;
		// = null;
		// = null;
		// = null;
		// = null;
		/// <summary>Initialize the annotators at the start of the unit test.</summary>
		/// <remarks>
		/// Initialize the annotators at the start of the unit test.
		/// If they've already been initialized, do nothing.
		/// </remarks>
		/// <exception cref="System.Exception"/>
		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			lock (typeof(CleanXmlAnnotatorTest))
			{
				if (ptbInvertible == null)
				{
					ptbInvertible = new TokenizerAnnotator(false, "en", "invertible,ptb3Escaping=true");
				}
				if (ptbNotInvertible == null)
				{
					ptbNotInvertible = new TokenizerAnnotator(false, "en", "invertible=false,ptb3Escaping=true");
				}
				if (cleanXmlAllTags == null)
				{
					cleanXmlAllTags = new CleanXmlAnnotator(".*", string.Empty, string.Empty, false);
				}
				if (cleanXmlSomeTags == null)
				{
					cleanXmlSomeTags = new CleanXmlAnnotator("p", string.Empty, string.Empty, false);
				}
				if (cleanXmlEndSentences == null)
				{
					cleanXmlEndSentences = new CleanXmlAnnotator(".*", "p", string.Empty, false);
				}
				if (cleanXmlWithFlaws == null)
				{
					cleanXmlWithFlaws = new CleanXmlAnnotator(".*", string.Empty, string.Empty, true);
				}
				if (wtsSplitter == null)
				{
					wtsSplitter = new WordsToSentencesAnnotator(false);
				}
			}
		}

		public static Annotation Annotate(string text, IAnnotator tokenizer, IAnnotator xmlRemover, IAnnotator splitter)
		{
			Annotation annotation = new Annotation(text);
			tokenizer.Annotate(annotation);
			if (xmlRemover != null)
			{
				xmlRemover.Annotate(annotation);
			}
			if (splitter != null)
			{
				splitter.Annotate(annotation);
			}
			return annotation;
		}

		private static void CheckResult(Annotation annotation, params string[] gold)
		{
			IList<CoreLabel> goldTokens = new List<CoreLabel>();
			Annotation[] goldAnnotations = new Annotation[gold.Length];
			for (int i = 0; i < gold.Length; ++i)
			{
				goldAnnotations[i] = Annotate(gold[i], ptbInvertible, null, null);
				Sharpen.Collections.AddAll(goldTokens, goldAnnotations[i].Get(typeof(CoreAnnotations.TokensAnnotation)));
			}
			IList<CoreLabel> annotationLabels = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			if (goldTokens.Count != annotationLabels.Count)
			{
				foreach (CoreLabel annotationLabel in annotationLabels)
				{
					System.Console.Error.Write(annotationLabel.Word());
					System.Console.Error.Write(' ');
				}
				System.Console.Error.WriteLine();
				foreach (CoreLabel goldToken in goldTokens)
				{
					System.Console.Error.Write(goldToken.Word());
					System.Console.Error.Write(' ');
				}
				System.Console.Error.WriteLine();
			}
			NUnit.Framework.Assert.AreEqual(goldTokens.Count, annotationLabels.Count, "Token count mismatch (gold vs: actual)");
			for (int i_1 = 0; i_1 < annotationLabels.Count; ++i_1)
			{
				NUnit.Framework.Assert.AreEqual(goldTokens[i_1].Word(), annotationLabels[i_1].Word());
			}
			if (annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)) != null)
			{
				IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
				NUnit.Framework.Assert.AreEqual(gold.Length, sentences.Count, "Sentence count mismatch");
			}
		}

		private static void CheckInvert(Annotation annotation, string gold)
		{
			IList<CoreLabel> annotationLabels = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			StringBuilder original = new StringBuilder();
			foreach (CoreLabel label in annotationLabels)
			{
				original.Append(label.Get(typeof(CoreAnnotations.BeforeAnnotation)));
				original.Append(label.Get(typeof(CoreAnnotations.OriginalTextAnnotation)));
			}
			original.Append(annotationLabels[annotationLabels.Count - 1].Get(typeof(CoreAnnotations.AfterAnnotation)));
			NUnit.Framework.Assert.AreEqual(gold, original.ToString());
		}

		private static void CheckContext(CoreLabel label, params string[] expectedContext)
		{
			IList<string> xmlContext = label.Get(typeof(CoreAnnotations.XmlContextAnnotation));
			NUnit.Framework.Assert.AreEqual(expectedContext.Length, xmlContext.Count);
			for (int i = 0; i < expectedContext.Length; ++i)
			{
				NUnit.Framework.Assert.AreEqual(expectedContext[i], xmlContext[i]);
			}
		}

		[Test]
		public virtual void TestRemoveXML()
		{
			string testString = "<xml>This is a test string.</xml>";
			CheckResult(Annotate(testString, ptbInvertible, cleanXmlAllTags, wtsSplitter), "This is a test string.");
		}

		[Test]
		public virtual void TestExtractSpecificTag()
		{
			string testString = ("<p>This is a test string.</p>" + "<foo>This should not be found</foo>");
			CheckResult(Annotate(testString, ptbInvertible, cleanXmlSomeTags, wtsSplitter), "This is a test string.");
		}

		[Test]
		public virtual void TestSentenceSplitting()
		{
			string testString = ("<p>This sentence is split</p>" + "<foo>over two tags</foo>");
			CheckResult(Annotate(testString, ptbInvertible, cleanXmlAllTags, wtsSplitter), "This sentence is split over two tags");
			CheckResult(Annotate(testString, ptbInvertible, cleanXmlEndSentences, wtsSplitter), "This sentence is split", "over two tags");
		}

		[Test]
		public virtual void TestNestedTags()
		{
			string testString = "<p><p>This text is in a</p>nested tag</p>";
			CheckResult(Annotate(testString, ptbInvertible, cleanXmlAllTags, wtsSplitter), "This text is in a nested tag");
			CheckResult(Annotate(testString, ptbInvertible, cleanXmlEndSentences, wtsSplitter), "This text is in a", "nested tag");
		}

		[Test]
		public virtual void TestMissingCloseTags()
		{
			string testString = "<text><p>This text <p>has closing tags wrong</text>";
			CheckResult(Annotate(testString, ptbInvertible, cleanXmlWithFlaws, wtsSplitter), "This text has closing tags wrong");
			try
			{
				CheckResult(Annotate(testString, ptbInvertible, cleanXmlAllTags, wtsSplitter), "This text has closing tags wrong");
				throw new Exception("it was supposed to barf");
			}
			catch (ArgumentException)
			{
			}
		}

		// this is what was supposed to happen
		[Test]
		public virtual void TestEarlyEnd()
		{
			string testString = "<text>This text ends before all tags closed";
			CheckResult(Annotate(testString, ptbInvertible, cleanXmlWithFlaws, wtsSplitter), "This text ends before all tags closed");
			try
			{
				CheckResult(Annotate(testString, ptbInvertible, cleanXmlAllTags, wtsSplitter), "This text ends before all tags closed");
				throw new Exception("it was supposed to barf");
			}
			catch (ArgumentException)
			{
			}
		}

		// this is what was supposed to happen
		[Test]
		public virtual void TestInvertible()
		{
			string testNoTags = "This sentence should be invertible.";
			string testTags = "  <xml>  This sentence should  be  invertible.  </xml>  ";
			string testManyTags = " <xml>   <foo>       <bar>This sentence should  " + "   </bar>be invertible.   </foo>   </xml> ";
			Annotation annotation = Annotate(testNoTags, ptbInvertible, cleanXmlAllTags, wtsSplitter);
			CheckResult(annotation, testNoTags);
			CheckInvert(annotation, testNoTags);
			annotation = Annotate(testTags, ptbInvertible, cleanXmlAllTags, wtsSplitter);
			CheckResult(annotation, testNoTags);
			CheckInvert(annotation, testTags);
			annotation = Annotate(testManyTags, ptbInvertible, cleanXmlAllTags, wtsSplitter);
			CheckResult(annotation, testNoTags);
			CheckInvert(annotation, testManyTags);
		}

		[Test]
		public virtual void TestContext()
		{
			string testManyTags = " <xml>   <foo>       <bar>This sentence should  " + "   </bar>be invertible.   </foo>   </xml> ";
			Annotation annotation = Annotate(testManyTags, ptbInvertible, cleanXmlAllTags, wtsSplitter);
			IList<CoreLabel> annotationLabels = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			for (int i = 0; i < 3; ++i)
			{
				CheckContext(annotationLabels[i], "xml", "foo", "bar");
			}
			for (int i_1 = 3; i_1 < 5; ++i_1)
			{
				CheckContext(annotationLabels[i_1], "xml", "foo");
			}
		}

		[Test]
		public virtual void TestOffsets()
		{
			string testString = "<p><p>This text is in a</p>nested tag</p>";
			Annotation annotation = Annotate(testString, ptbInvertible, cleanXmlAllTags, wtsSplitter);
			CheckResult(annotation, "This text is in a nested tag");
			IList<CoreLabel> labels = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			NUnit.Framework.Assert.AreEqual(6, labels[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)));
			NUnit.Framework.Assert.AreEqual(10, labels[0].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
		}

		[Test]
		public virtual void TestAttributes()
		{
			string testString = "<p a=\"b\">This text has an attribute</p>";
			Annotation annotation = Annotate(testString, ptbInvertible, cleanXmlAllTags, wtsSplitter);
			CheckResult(annotation, "This text has an attribute");
		}

		[Test]
		public virtual void TestViaCoreNlp()
		{
			string testManyTags = " <xml>   <foo>       <bar>This sentence should  " + "   </bar>be invertible.   </foo>   </xml> ";
			Annotation anno = new Annotation(testManyTags);
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize, ssplit, cleanxml", "tokenizer.options", "invertible,ptb3Escaping=true", "cleanxml.xmltags", ".*", "cleanxml.sentenceendingtags", "p", "cleanxml.datetags", string.Empty, 
				"cleanxml.allowflawedxml", "false");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			pipeline.Annotate(anno);
			CheckInvert(anno, testManyTags);
			IList<CoreLabel> annotationLabels = anno.Get(typeof(CoreAnnotations.TokensAnnotation));
			for (int i = 0; i < 3; ++i)
			{
				CheckContext(annotationLabels[i], "xml", "foo", "bar");
			}
			for (int i_1 = 3; i_1 < 5; ++i_1)
			{
				CheckContext(annotationLabels[i_1], "xml", "foo");
			}
		}

		[Test]
		public virtual void TestKbpSectionMatching()
		{
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize,cleanxml,ssplit", "tokenize.language", "es", "tokenize.options", "tokenizeNLs,ptb3Escaping=true", "ssplit.newlineIsSentenceBreak", "two", "ssplit.tokenPatternsToDiscard"
				, "\\n,\\*NL\\*", "ssplit.boundaryMultiTokenRegex", "/\\*NL\\*/ /\\p{Lu}[-\\p{L}]+/+ /,/ ( /[-\\p{L}]+/+ /,/ )? " + "/[1-3]?[0-9]/ /\\p{Ll}{3,5}/ /=LRB=/ /\\p{Lu}\\p{L}+/ /=RRB=/ /--/", "clean.xmltags", "headline|text|post", "clean.singlesentencetags"
				, "HEADLINE|AUTHOR", "clean.sentenceendingtags", "TEXT|POST|QUOTE", "clean.turntags", "POST|QUOTE", "clean.speakertags", "AUTHOR", "clean.datetags", "DATE_TIME", "clean.doctypetags", "DOC", "clean.docAnnotations", "docID=doc[id]", "clean.sectiontags"
				, "HEADLINE|POST", "clean.sectionAnnotations", "sectionID=post[id],sectionDate=post[datetime],author=post[author]", "clean.quotetags", "quote", "clean.quoteauthorattributes", "orig_author", "clean.tokenAnnotations", "link=a[href],speaker=post[author],speaker=quote[orig_author]"
				);
			string document = "<doc id=\"SPA_DF_000389_20090909_G00A09SM4\">\n" + "<headline>\n" + "Problema para Activar Restaurar Sistema En Win Ue\n" + "</headline>\n" + "<post author=\"mysecondskin\" datetime=\"2009-09-09T00:00:00\" id=\"p1\">\n" + 
				"hola portalianos tengo un problemita,mi vieja tiene un pc en su casa y no tiene activado restaurar sistema ya que el pc tiene el xp ue v5,he tratado de arreglárselo pero no he podido dar con la solución y no he querido formatearle el pc porque tiene un sin numero de programas que me da paja reinstalar\n"
				 + "ojala alguien me pueda ayudar\n" + "vale socios\n" + "</post>\n" + "<post author=\"pajenri\" datetime=\"2009-09-09T00:00:00\" id=\"p2\">\n" + "<quote orig_author=\"mysecondskin\">\n" + "hola portalianos tengo un problemita,mi vieja tiene un pc en su casa y no tiene activado restaurar sistema ya que el pc tiene el xp ue v5,he tratado de arreglárselo pero no he podido dar con la solución y no he querido formatearle el pc porque tiene un sin numero de programas que me da paja reinstalar\n"
				 + "ojala alguien me pueda ayudar\n" + "vale socios\n" + "</quote>\n" + "\n" + "por lo que tengo entendido esa opcion en los win ue vienen eliminadas no desactivadas, asi que para activarla habria que reinstalar un xp limpio no tuneado. como dato es tipico en sistemas tuneados comos el win ue que suceda esto. el restaurador salva mas de lo que se cree. si toy equibocado con la info que alguien me corrija\n"
				 + "</post>\n" + "<post author=\"UnknownCnR\" datetime=\"2009-09-09T00:00:00\" id=\"p3\">\n" + "<a href=\"http://www.sendspace.com/file/54pxbl\">http://www.sendspace.com/file/54pxbl</a>\n" + "\n" + "Con este registro podras activarlo ;)\n" 
				+ "</post>\n" + "<post author=\"mysecondskin\" datetime=\"2009-09-11T00:00:00\" id=\"p4\">\n" + "gracias pero de verdad esa solucion no sirve\n" + "</post>\n" + "</doc>\n";
			string[][] sections = new string[][] { new string[] { null, null, "Problema para Activar Restaurar Sistema En Win Ue\n" }, new string[] { "mysecondskin", "2009-09-09T00:00:00", "hola portalianos tengo un problemita , mi vieja tiene un pc en su casa y no tiene activado restaurar sistema ya que el pc tiene el xp ue v5 , he tratado de arreglárselo pero no he podido dar con la solución y no he querido formatearle el pc porque tiene un sin numero de programas que me da paja reinstalar ojala alguien me pueda ayudar vale socios\n"
				 }, new string[] { "pajenri", "2009-09-09T00:00:00", "(QUOTING: mysecondskin) hola portalianos tengo un problemita , mi vieja tiene un pc en su casa y no tiene activado restaurar sistema ya que el pc tiene el xp ue v5 , he tratado de arreglárselo pero no he podido dar con la solución y no he querido formatearle el pc porque tiene un sin numero de programas que me da paja reinstalar ojala alguien me pueda ayudar vale socios\n"
				 + "por lo que tengo entendido esa opcion en los win ue vienen eliminadas no desactivadas , asi que para activarla habria que reinstalar un xp limpio no tuneado .\n" + "como dato es tipico en sistemas tuneados comos el win ue que suceda esto .\n"
				 + "el restaurador salva mas de lo que se cree .\n" + "si toy equibocado con la info que alguien me corrija\n" }, new string[] { "UnknownCnR", "2009-09-09T00:00:00", "http://www.sendspace.com/file/54pxbl\n" + "Con este registro podras activarlo ;=RRB=\n"
				 }, new string[] { "mysecondskin", "2009-09-11T00:00:00", "gracias pero de verdad esa solucion no sirve\n" } };
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			Annotation testDocument = new Annotation(document);
			pipeline.Annotate(testDocument);
			// check the forum posts
			int num = 0;
			foreach (ICoreMap discussionForumPost in testDocument.Get(typeof(CoreAnnotations.SectionsAnnotation)))
			{
				NUnit.Framework.Assert.AreEqual(sections[num][0], discussionForumPost.Get(typeof(CoreAnnotations.AuthorAnnotation)));
				NUnit.Framework.Assert.AreEqual(sections[num][1], discussionForumPost.Get(typeof(CoreAnnotations.SectionDateAnnotation)));
				StringBuilder sb = new StringBuilder();
				foreach (ICoreMap sentence in discussionForumPost.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					bool sentenceQuoted = (sentence.Get(typeof(CoreAnnotations.QuotedAnnotation)) != null) && sentence.Get(typeof(CoreAnnotations.QuotedAnnotation));
					System.Console.Error.WriteLine("Sentence " + sentence + " quoted=" + sentenceQuoted);
					string sentenceAuthor = sentence.Get(typeof(CoreAnnotations.AuthorAnnotation));
					string potentialQuoteText = sentenceQuoted ? "(QUOTING: " + sentenceAuthor + ") " : string.Empty;
					sb.Append(potentialQuoteText);
					sb.Append(sentence.Get(typeof(CoreAnnotations.TokensAnnotation)).Stream().Map(null).Collect(Collectors.Joining(" ")));
					sb.Append('\n');
				}
				NUnit.Framework.Assert.AreEqual(sections[num][2], sb.ToString());
				num++;
			}
			NUnit.Framework.Assert.AreEqual(sections.Length, num, "Too few sections");
		}
	}
}
