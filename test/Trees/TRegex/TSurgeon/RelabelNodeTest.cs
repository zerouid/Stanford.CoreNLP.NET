using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>Test the regex patterns in RelabelNode.</summary>
	/// <remarks>
	/// Test the regex patterns in RelabelNode.  The operation itself will
	/// be tested in TsurgeonTest.
	/// </remarks>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class RelabelNodeTest
	{
		[NUnit.Framework.Test]
		public virtual void TestRegexPattern()
		{
			Pattern pattern = RelabelNode.regexPattern;
			string[] goodLabels = new string[] { "//", "/foo/", "/\\\\/", "/\\\\\\\\/", "/foo\\\\/", "/f\\oo\\\\/", "/f\\oo/", "/f\\o/", "/f\\/oo/" };
			string[] badLabels = new string[] { "foo", "/\\/", "/\\\\\\/", "/foo\\/", "asdf" };
			RunPatternTest(pattern, goodLabels, badLabels, 1, -1);
		}

		[NUnit.Framework.Test]
		public virtual void TestNodePattern()
		{
			Pattern pattern = Pattern.Compile(RelabelNode.nodePatternString);
			string[] goodMatches = new string[] { "={foo}", "={blah}", "={z954240_fdsfgsf}" };
			string[] badMatches = new string[] { "%{foo}", "bar", "=%{blah}", "%={blah}", "=foo", "%foo" };
			RunPatternTest(pattern, goodMatches, badMatches, 0, 0);
		}

		[NUnit.Framework.Test]
		public virtual void TestVariablePattern()
		{
			Pattern pattern = Pattern.Compile(RelabelNode.variablePatternString);
			string[] goodMatches = new string[] { "%{foo}", "%{blah}", "%{z954240_fdsfgsf}" };
			string[] badMatches = new string[] { "={foo}", "{bar}", "=%{blah}", "%={blah}", "=foo", "%foo" };
			RunPatternTest(pattern, goodMatches, badMatches, 0, 0);
		}

		public virtual void RunPatternTest(Pattern pattern, string[] good, string[] bad, int startOffset, int endOffset)
		{
			foreach (string test in good)
			{
				Matcher m = pattern.Matcher(test);
				NUnit.Framework.Assert.IsTrue("Should have matched on " + test, m.Matches());
				string matched = m.Group(1);
				string expected = Sharpen.Runtime.Substring(test, startOffset, test.Length + endOffset);
				NUnit.Framework.Assert.AreEqual("Matched group wasn't " + test, expected, matched);
			}
			foreach (string test_1 in bad)
			{
				Matcher m = pattern.Matcher(test_1);
				NUnit.Framework.Assert.IsFalse("Shouldn't have matched on " + test_1, m.Matches());
			}
		}
	}
}
