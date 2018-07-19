using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Test the effects of various operations using a DocumentPreprocessor.</summary>
	/// <author>John Bauer</author>
	/// <version>2010</version>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class DocumentPreprocessorTest
	{
		// Copyright 2010, Stanford University, GPLv2
		private static void RunTest(string input, string[] expected)
		{
			RunTest(input, expected, null, false);
		}

		private static void RunTest(string input, string[] expected, string[] sentenceFinalPuncWords, bool whitespaceTokenize)
		{
			IList<string> results = new List<string>();
			DocumentPreprocessor document = new DocumentPreprocessor(new BufferedReader(new StringReader(input)));
			if (sentenceFinalPuncWords != null)
			{
				document.SetSentenceFinalPuncWords(sentenceFinalPuncWords);
			}
			if (whitespaceTokenize)
			{
				document.SetTokenizerFactory(null);
				document.SetSentenceDelimiter("\n");
			}
			foreach (IList<IHasWord> sentence in document)
			{
				results.Add(SentenceUtils.ListToString(sentence));
			}
			NUnit.Framework.Assert.AreEqual("Should be " + expected.Length + " sentences but got " + results.Count + ": " + results, expected.Length, results.Count);
			for (int i = 0; i < results.Count; ++i)
			{
				NUnit.Framework.Assert.AreEqual("Failed on sentence " + i, expected[i], results[i]);
			}
		}

		/// <summary>Test to see if it is correctly splitting text readers.</summary>
		[NUnit.Framework.Test]
		public virtual void TestText()
		{
			string test = "This is a test of the preprocessor2.  It should split this text into sentences.  I like resting my feet on my desk.  Hopefully the people around my office don't hear me singing along to my music, and if they do, hopefully they aren't annoyed.  My test cases are probably terrifying looks into my psyche.";
			string[] expectedResults = new string[] { "This is a test of the preprocessor2 .", "It should split this text into sentences .", "I like resting my feet on my desk .", "Hopefully the people around my office do n't hear me singing along to my music , and if they do , hopefully they are n't annoyed ."
				, "My test cases are probably terrifying looks into my psyche ." };
			RunTest(test, expectedResults);
		}

		/// <summary>Test if fails with punctuation near end.</summary>
		/// <remarks>Test if fails with punctuation near end. We did at one point.</remarks>
		[NUnit.Framework.Test]
		public virtual void TestNearFinalPunctuation()
		{
			string test = "Mount. Annaguan";
			string[] expectedResults = new string[] { "Mount .", "Annaguan" };
			RunTest(test, expectedResults);
		}

		/// <summary>Test if fails with punctuation near end.</summary>
		/// <remarks>Test if fails with punctuation near end. We did at one point.</remarks>
		[NUnit.Framework.Test]
		public virtual void TestNearFinalPunctuation2()
		{
			string test = "(I lied.)";
			string[] expectedResults = new string[] { "-LRB- I lied . -RRB-" };
			RunTest(test, expectedResults);
		}

		[NUnit.Framework.Test]
		public virtual void TestSetSentencePunctWords()
		{
			string test = "This is a test of the preprocessor2... it should split this text into sentences? This should be a different sentence.This should be attached to the previous sentence, though. Calvin Wilson for St. Louis Post Dispatch called it one of LaBeouf's best performances.";
			string[] expectedResults = new string[] { "This is a test of the preprocessor2 ...", "it should split this text into sentences ?", "This should be a different sentence.This should be attached to the previous sentence , though .", "Calvin Wilson for St. Louis Post Dispatch called it one of LaBeouf 's best performances ."
				 };
			string[] sentenceFinalPuncWords = new string[] { ".", "?", "!", "...", "\n" };
			RunTest(test, expectedResults, sentenceFinalPuncWords, false);
		}

		[NUnit.Framework.Test]
		public virtual void TestWhitespaceTokenization()
		{
			string test = "This is a whitespace tokenized test case . \n  This should be the second sentence    . \n \n  \n\n  This should be the third sentence .  \n  This should be one sentence . The period should not break it . \n This is the fifth sentence , with a weird period at the end.";
			string[] expectedResults = new string[] { "This is a whitespace tokenized test case .", "This should be the second sentence .", "This should be the third sentence .", "This should be one sentence . The period should not break it .", "This is the fifth sentence , with a weird period at the end."
				 };
			RunTest(test, expectedResults, null, true);
		}

		private static void CompareXMLResults(string input, string element, params string[] expectedResults)
		{
			List<string> results = new List<string>();
			DocumentPreprocessor document = new DocumentPreprocessor(new BufferedReader(new StringReader(input)), DocumentPreprocessor.DocType.Xml);
			document.SetElementDelimiter(element);
			foreach (IList<IHasWord> sentence in document)
			{
				results.Add(SentenceUtils.ListToString(sentence));
			}
			NUnit.Framework.Assert.AreEqual(expectedResults.Length, results.Count);
			for (int i = 0; i < results.Count; ++i)
			{
				NUnit.Framework.Assert.AreEqual(expectedResults[i], results[i]);
			}
		}

		private const string BasicXmlTest = "<xml><text>The previous test was a lie.  I didn't make this test in my office; I made it at home.</text>\nMy home currently smells like dog vomit.\n<text apartment=\"stinky\">My dog puked everywhere after eating some carrots the other day.\n  Hopefully I have cleaned the last of it, though.</text>\n\nThis tests to see what happens on an empty tag:<text></text><text>It shouldn't include a blank sentence, but it should include this sentence.</text>this is madness...<text>no, this <text> is </text> NESTED!</text>This only prints 'no, this is' instead of 'no, this is NESTED'.  Doesn't do what i would expect, but it's consistent with the old behavior.</xml>";

		/// <summary>
		/// Tests various ways of finding sentences with an XML
		/// DocumentPreprocessor2.
		/// </summary>
		/// <remarks>
		/// Tests various ways of finding sentences with an XML
		/// DocumentPreprocessor2.  We test to make sure it does find the
		/// text between
		/// <c>&lt;text&gt;</c>
		/// tags and that it doesn't find any text if we
		/// look for
		/// <c>&lt;zzzz&gt;</c>
		/// tags.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestXMLBasic()
		{
			// This subsequent block of code can be uncommented to demonstrate
			// that the results from the new DP are the same as from the old DP.
			//
			// System.out.println("\nThis is the old behavior\n");
			// DocumentPreprocessor p = new DocumentPreprocessor();
			// List<List<? extends HasWord>> r = p.getSentencesFromXML(new BufferedReader(new StringReader(test)), "text", null, false);
			// System.out.println(r.size());
			// for (List<? extends HasWord> s : r) {
			//   System.out.println("\"" + Sentence.listToString(s) + "\"");
			// }
			//
			// System.out.println("\nThis is the new behavior\n");
			// DocumentPreprocessor2 d = new DocumentPreprocessor2(new BufferedReader(new StringReader(test)), DocumentPreprocessor2.DocType.XML);
			// d.setElementDelimiter("text");
			// for (List<HasWord> sentence : d) {
			//   System.out.println("\"" + Sentence.listToString(sentence) + "\"");
			// }
			string[] expectedResults = new string[] { "The previous test was a lie .", "I did n't make this test in my office ; I made it at home .", "My dog puked everywhere after eating some carrots the other day .", "Hopefully I have cleaned the last of it , though ."
				, "It should n't include a blank sentence , but it should include this sentence .", "no , this is" };
			CompareXMLResults(BasicXmlTest, "text", expectedResults);
		}

		[NUnit.Framework.Test]
		public virtual void TestXMLNoResults()
		{
			CompareXMLResults(BasicXmlTest, "zzzz");
		}

		/// <summary>Yeah...</summary>
		/// <remarks>
		/// Yeah... a bug that failed this test bug not the NotInText test
		/// was part of the preprocessor for a while.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestXMLElementInText()
		{
			string TagInText = "<xml><wood>There are many trees in the woods</wood></xml>";
			CompareXMLResults(TagInText, "wood", "There are many trees in the woods");
		}

		[NUnit.Framework.Test]
		public virtual void TestXMLElementNotInText()
		{
			string TagInText = "<xml><wood>There are many trees in the forest</wood></xml>";
			CompareXMLResults(TagInText, "wood", "There are many trees in the forest");
		}

		[NUnit.Framework.Test]
		public virtual void TestPlainTextIterator()
		{
			string test = "This is a one line test . \n";
			string[] expectedResults = new string[] { "This", "is", "a", "one", "line", "test", "." };
			DocumentPreprocessor document = new DocumentPreprocessor(new BufferedReader(new StringReader(test)));
			document.SetTokenizerFactory(null);
			document.SetSentenceDelimiter("\n");
			IEnumerator<IList<IHasWord>> iterator = document.GetEnumerator();
			// we test twice because this call should not eat any text
			NUnit.Framework.Assert.IsTrue(iterator.MoveNext());
			NUnit.Framework.Assert.IsTrue(iterator.MoveNext());
			IList<IHasWord> words = iterator.Current;
			NUnit.Framework.Assert.AreEqual(expectedResults.Length, words.Count);
			for (int i = 0; i < expectedResults.Length; ++i)
			{
				NUnit.Framework.Assert.AreEqual(expectedResults[i], words[i].Word());
			}
			// we test twice to make sure we don't blow up on multiple calls
			NUnit.Framework.Assert.IsFalse(iterator.MoveNext());
			NUnit.Framework.Assert.IsFalse(iterator.MoveNext());
			try
			{
				iterator.Current;
				throw new AssertionError("iterator.next() should have blown up");
			}
			catch (NoSuchElementException)
			{
			}
			// yay, this is what we want
			// just in case
			NUnit.Framework.Assert.IsFalse(iterator.MoveNext());
		}
	}
}
