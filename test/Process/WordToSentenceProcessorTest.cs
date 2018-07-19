using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class WordToSentenceProcessorTest
	{
		private static readonly IAnnotator ptb = new TokenizerAnnotator(false, "en");

		private static readonly IAnnotator ptbNL = new TokenizerAnnotator(false, "en", "invertible,ptb3Escaping=true,tokenizeNLs=true");

		private static readonly IAnnotator wsNL = new TokenizerAnnotator(false, PropertiesUtils.AsProperties("tokenize.whitespace", "true", "invertible", "true", "tokenizeNLs", "true"));

		private static readonly WordToSentenceProcessor<CoreLabel> wts = new WordToSentenceProcessor<CoreLabel>();

		private static readonly WordToSentenceProcessor<CoreLabel> wtsNull = new WordToSentenceProcessor<CoreLabel>(true);

		private static readonly WordToSentenceProcessor<CoreLabel> cwts = new WordToSentenceProcessor<CoreLabel>("[.。]|[!?！？]+", WordToSentenceProcessor.NewlineIsSentenceBreak.TwoConsecutive, false);

		// treat input as one sentence
		private static void CheckResult(WordToSentenceProcessor<CoreLabel> wts, string testSentence, params string[] gold)
		{
			CheckResult(wts, ptb, testSentence, gold);
		}

		private static void CheckResult(WordToSentenceProcessor<CoreLabel> wts, IAnnotator tokenizer, string testSentence, params string[] gold)
		{
			Annotation annotation = new Annotation(testSentence);
			ptbNL.Annotate(annotation);
			IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<IList<CoreLabel>> sentences = wts.Process(tokens);
			NUnit.Framework.Assert.AreEqual("Output number of sentences didn't match:\n" + Arrays.ToString(gold) + " vs. \n" + sentences + '\n', gold.Length, sentences.Count);
			Annotation[] goldAnnotations = new Annotation[gold.Length];
			for (int i = 0; i < gold.Length; ++i)
			{
				goldAnnotations[i] = new Annotation(gold[i]);
				tokenizer.Annotate(goldAnnotations[i]);
				IList<CoreLabel> goldTokens = goldAnnotations[i].Get(typeof(CoreAnnotations.TokensAnnotation));
				IList<CoreLabel> testTokens = sentences[i];
				int goldTokensSize = goldTokens.Count;
				NUnit.Framework.Assert.AreEqual("Sentence lengths didn't match:\n" + goldTokens + " vs. \n" + testTokens + '\n', goldTokensSize, testTokens.Count);
				for (int j = 0; j < goldTokensSize; ++j)
				{
					NUnit.Framework.Assert.AreEqual(goldTokens[j].Word(), testTokens[j].Word());
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestNoSplitting()
		{
			CheckResult(wts, "This should only be one sentence.", "This should only be one sentence.");
		}

		[NUnit.Framework.Test]
		public virtual void TestTwoSentences()
		{
			CheckResult(wts, "This should be two sentences.  There is a split.", "This should be two sentences.", "There is a split.");
			CheckResult(wts, "This should be two sentences!  There is a split.", "This should be two sentences!", "There is a split.");
			CheckResult(wts, "This should be two sentences?  There is a split.", "This should be two sentences?", "There is a split.");
			CheckResult(wts, "This should be two sentences!!!?!!  There is a split.", "This should be two sentences!!!?!!", "There is a split.");
		}

		[NUnit.Framework.Test]
		public virtual void TestEdgeCases()
		{
			CheckResult(wts, "This should be two sentences.  Second one incomplete", "This should be two sentences.", "Second one incomplete");
			CheckResult(wts, "One incomplete sentence", "One incomplete sentence");
			CheckResult(wts, "(Break after a parenthesis.)  (Or after \"quoted stuff!\")", "(Break after a parenthesis.)", "(Or after \"quoted stuff!\")");
			CheckResult(wts, "  ");
			CheckResult(wts, "This should be\n one sentence.", "This should be one sentence.");
			CheckResult(wts, "'') Funny stuff joined on.", "'') Funny stuff joined on.");
		}

		[NUnit.Framework.Test]
		public virtual void TestMr()
		{
			CheckResult(wts, "Mr. White got a loaf of bread", "Mr. White got a loaf of bread");
		}

		[NUnit.Framework.Test]
		public virtual void TestNullSplitter()
		{
			CheckResult(wtsNull, "This should be one sentence.  There is no split.", "This should be one sentence.  There is no split.");
		}

		[NUnit.Framework.Test]
		public virtual void TestParagraphStrategies()
		{
			WordToSentenceProcessor<CoreLabel> wtsNever = new WordToSentenceProcessor<CoreLabel>(WordToSentenceProcessor.NewlineIsSentenceBreak.Never);
			WordToSentenceProcessor<CoreLabel> wtsAlways = new WordToSentenceProcessor<CoreLabel>(WordToSentenceProcessor.NewlineIsSentenceBreak.Always);
			WordToSentenceProcessor<CoreLabel> wtsTwo = new WordToSentenceProcessor<CoreLabel>(WordToSentenceProcessor.NewlineIsSentenceBreak.TwoConsecutive);
			string input1 = "Depending on the options,\nthis could be all sorts of things,\n\n as I like chocolate. And cookies.";
			string input2 = "Depending on the options,\nthis could be all sorts of things,\n as I like chocolate. And cookies.";
			CheckResult(wtsNever, input1, "Depending on the options,\nthis could be all sorts of things,\n\nas I like chocolate.", "And cookies.");
			CheckResult(wtsAlways, input1, "Depending on the options,", "this could be all sorts of things,", "as I like chocolate.", "And cookies.");
			CheckResult(wtsTwo, input1, "Depending on the options, this could be all sorts of things,", "as I like chocolate.", "And cookies.");
			CheckResult(wtsNever, input2, "Depending on the options,\nthis could be all sorts of things,\nas I like chocolate.", "And cookies.");
			CheckResult(wtsAlways, input2, "Depending on the options,", "this could be all sorts of things,", "as I like chocolate.", "And cookies.");
			CheckResult(wtsTwo, input2, "Depending on the options,\nthis could be all sorts of things,\nas I like chocolate.", "And cookies.");
			string input3 = "Specific descriptions are absent.\n\n''Mossy Head Industrial Park'' it says.";
			CheckResult(wtsTwo, input3, "Specific descriptions are absent.", "''Mossy Head Industrial Park'' it says.");
		}

		[NUnit.Framework.Test]
		public virtual void TestXmlElements()
		{
			WordToSentenceProcessor<CoreLabel> wtsXml = new WordToSentenceProcessor<CoreLabel>(null, null, null, Generics.NewHashSet(Arrays.AsList("p", "chapter")), WordToSentenceProcessor.NewlineIsSentenceBreak.Never, null, null);
			string input1 = "<chapter>Chapter 1</chapter><p>This is text. So is this.</p> <p>One without end</p><p>Another</p><p>And another</p>";
			CheckResult(wtsXml, input1, "Chapter 1", "This is text.", "So is this.", "One without end", "Another", "And another");
		}

		[NUnit.Framework.Test]
		public virtual void TestRegion()
		{
			WordToSentenceProcessor<CoreLabel> wtsRegion = new WordToSentenceProcessor<CoreLabel>(WordToSentenceProcessor.DefaultBoundaryRegex, WordToSentenceProcessor.DefaultBoundaryFollowersRegex, WordToSentenceProcessor.DefaultSentenceBoundariesToDiscard
				, Generics.NewHashSet(Java.Util.Collections.SingletonList("p")), "chapter|preface", WordToSentenceProcessor.NewlineIsSentenceBreak.Never, null, null, false, false);
			string input1 = "<title>Chris rules!</title><preface><p>Para one</p><p>Para two</p></preface>" + "<chapter><p>Text we like. Two sentences \n\n in it.</p></chapter><coda>Some more text here</coda>";
			CheckResult(wtsRegion, input1, "Para one", "Para two", "Text we like.", "Two sentences in it.");
		}

		[NUnit.Framework.Test]
		public virtual void TestBlankLines()
		{
			WordToSentenceProcessor<CoreLabel> wtsLines = new WordToSentenceProcessor<CoreLabel>(Generics.NewHashSet(WordToSentenceProcessor.DefaultSentenceBoundariesToDiscard));
			string input1 = "Depending on the options,\nthis could be all sorts of things,\n\n as I like chocolate. And cookies.";
			CheckResult(wtsLines, input1, "Depending on the options,", "this could be all sorts of things,", string.Empty, "as I like chocolate. And cookies.");
			string input2 = "Depending on the options,\nthis could be all sorts of things,\n\n as I like chocolate. And cookies.\n";
			CheckResult(wtsLines, input2, "Depending on the options,", "this could be all sorts of things,", string.Empty, "as I like chocolate. And cookies.");
			string input3 = "Depending on the options,\nthis could be all sorts of things,\n\n as I like chocolate. And cookies.\n\n";
			CheckResult(wtsLines, input3, "Depending on the options,", "this could be all sorts of things,", string.Empty, "as I like chocolate. And cookies.", string.Empty);
		}

		[NUnit.Framework.Test]
		public virtual void TestExclamationPoint()
		{
			Annotation annotation = new Annotation("Foo!!");
			ptb.Annotate(annotation);
			IList list = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			NUnit.Framework.Assert.AreEqual("Wrong double bang", "[Foo, !!]", list.ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestChinese()
		{
			CheckResult(cwts, wsNL, "巴拉特 说 ： 「 我们 未 再 获得 任何 结果 。 」 ＜ 金融时报 ？ ＞ 《 金融时报 》 周三", "巴拉特 说 ： 「 我们 未 再 获得 任何 结果 。 」", "＜ 金融时报 ？ ＞", "《 金融时报 》 周三");
		}

		/// <summary>
		/// Ensure that the unicode paragraph separator always
		/// starts a new sentence.
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestParagraphSeparator()
		{
			CheckResult(wts, "Hello\u2029World.", "Hello", "World.");
			CheckResult(wts, "Hello.\u2029World.", "Hello.", "World.");
			CheckResult(wts, "Hello  \u2029World.", "Hello", "World.");
		}
	}
}
