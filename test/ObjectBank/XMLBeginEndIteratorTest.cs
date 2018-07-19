using System.Collections.Generic;
using Java.IO;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Objectbank
{
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	public class XMLBeginEndIteratorTest
	{
		private const string TestString = "<xml><tagger>\n  <text>\n    This tests the xml input.\n  </text>  \n  This should not be found.  \n  <text>\n    This should be found.\n  </text>\n  <text>\n    The dog's barking kept the\n neighbors up all night.\n  </text>\n</tagging></xml>";

		private const string EmptyTestString = "<text></text>";

		private const string SingleTagTestString = "<xml><text>This tests the xml input with single tags<text/>, which should not close the input</text><text/>and should not open it either.</xml>";

		private const string NestingTestString = "<xml><text>A<text>B</text>C</text>D <text>A<text>B</text>C<text>D</text>E</text>F <text>A<text>B</text>C<text>D<text/></text>E</text>F</xml>";

		private const string TagInTextString = "<xml><bar>The dog's barking kept the neighbors up all night</bar></xml>";

		private const string TwoTagsString = "<xml><foo>This is the first sentence</foo><bar>The dog's barking kept the neighbors up all night</bar><foo>The owner could not stop the dog from barking</foo></xml>";

		// Copyright 2010, Stanford NLP
		// Test that the XMLBeginEndIterator will successfully find a bunch of
		// text inside xml tags.
		// TODO: can add tests for the String->Object conversion and some of
		// the other options the XMLBeginEndIterator has
		private static List<string> GetResults(XMLBeginEndIterator<string> iterator)
		{
			List<string> results = new List<string>();
			while (iterator.MoveNext())
			{
				results.Add(iterator.Current);
			}
			return results;
		}

		private static void CompareResults(XMLBeginEndIterator<string> iterator, params string[] expectedResults)
		{
			List<string> results = GetResults(iterator);
			NUnit.Framework.Assert.AreEqual(expectedResults.Length, results.Count);
			for (int i = 0; i < expectedResults.Length; ++i)
			{
				NUnit.Framework.Assert.AreEqual(expectedResults[i], results[i]);
			}
		}

		[Test]
		public virtual void TestNotFound()
		{
			XMLBeginEndIterator<string> iterator = new XMLBeginEndIterator<string>(new BufferedReader(new StringReader(TestString)), "zzzz");
			CompareResults(iterator);
		}

		// eg, should be empty
		[Test]
		public virtual void TestFound()
		{
			XMLBeginEndIterator<string> iterator = new XMLBeginEndIterator<string>(new BufferedReader(new StringReader(TestString)), "text");
			CompareResults(iterator, "\n    This tests the xml input.\n  ", "\n    This should be found.\n  ", "\n    The dog's barking kept the\n neighbors up all night.\n  ");
		}

		[Test]
		public virtual void TestEmpty()
		{
			XMLBeginEndIterator<string> iterator = new XMLBeginEndIterator<string>(new BufferedReader(new StringReader(EmptyTestString)), "text");
			CompareResults(iterator, string.Empty);
		}

		[Test]
		public virtual void TestSingleTags()
		{
			XMLBeginEndIterator<string> iterator = new XMLBeginEndIterator<string>(new BufferedReader(new StringReader(SingleTagTestString)), "text");
			CompareResults(iterator, "This tests the xml input with single tags, which should not close the input");
		}

		[Test]
		public virtual void TestNesting()
		{
			XMLBeginEndIterator<string> iterator = new XMLBeginEndIterator<string>(new BufferedReader(new StringReader(NestingTestString)), "text", false, false, true);
			CompareResults(iterator, "ABC", "ABCDE", "ABCDE");
		}

		[Test]
		public virtual void TestInternalTags()
		{
			XMLBeginEndIterator<string> iterator = new XMLBeginEndIterator<string>(new BufferedReader(new StringReader(NestingTestString)), "text", true, false, true);
			CompareResults(iterator, "A<text>B</text>C", "A<text>B</text>C<text>D</text>E", "A<text>B</text>C<text>D<text/></text>E");
		}

		[Test]
		public virtual void TestContainingTags()
		{
			XMLBeginEndIterator<string> iterator = new XMLBeginEndIterator<string>(new BufferedReader(new StringReader(NestingTestString)), "text", true, true, true);
			CompareResults(iterator, "<text>A<text>B</text>C</text>", "<text>A<text>B</text>C<text>D</text>E</text>", "<text>A<text>B</text>C<text>D<text/></text>E</text>");
		}

		[Test]
		public virtual void TestTagInText()
		{
			XMLBeginEndIterator<string> iterator = new XMLBeginEndIterator<string>(new BufferedReader(new StringReader(TagInTextString)), "bar");
			CompareResults(iterator, "The dog's barking kept the neighbors up all night");
		}

		[Test]
		public virtual void TestTwoTags()
		{
			XMLBeginEndIterator<string> iterator = new XMLBeginEndIterator<string>(new BufferedReader(new StringReader(TwoTagsString)), "foo|bar");
			CompareResults(iterator, "This is the first sentence", "The dog's barking kept the neighbors up all night", "The owner could not stop the dog from barking");
		}
	}
}
