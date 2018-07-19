using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// A test for
	/// <see cref="JSONOutputter"/>
	/// .
	/// </summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class JSONOutputterTest
	{
		// -----
		// BEGIN TESTS FOR JSON WRITING
		// -----
		private static string Indent(string @in)
		{
			return @in.Replace("\t", JSONOutputter.IndentChar);
		}

		private static void TestEscape(string input, string expected)
		{
			NUnit.Framework.Assert.AreEqual(1, input.Length);
			// make sure I'm escaping right
			NUnit.Framework.Assert.AreEqual(2, expected.Length);
			// make sure I'm escaping right
			NUnit.Framework.Assert.AreEqual(expected, StringUtils.EscapeJsonString(input));
		}

		private static void TestNoEscape(string input, string expected)
		{
			NUnit.Framework.Assert.AreEqual(1, input.Length);
			// make sure I'm escaping right
			NUnit.Framework.Assert.AreEqual(1, expected.Length);
			// make sure I'm escaping right
			NUnit.Framework.Assert.AreEqual(expected, StringUtils.EscapeJsonString(input));
		}

		[NUnit.Framework.Test]
		public virtual void TestSanitizeJSONString()
		{
			TestEscape("\b", "\\b");
			TestEscape("\f", "\\f");
			TestEscape("\n", "\\n");
			TestEscape("\r", "\\r");
			TestEscape("\t", "\\t");
			TestNoEscape("'", "'");
			TestEscape("\"", "\\\"");
			TestEscape("\\", "\\\\");
			NUnit.Framework.Assert.AreEqual("\\\\b", StringUtils.EscapeJsonString("\\b"));
		}

		[NUnit.Framework.Test]
		public virtual void TestSimpleJSON()
		{
			NUnit.Framework.Assert.AreEqual(Indent("{\n\t\"foo\": \"bar\"\n}"), JSONOutputter.JSONWriter.ObjectToJSON(null));
			NUnit.Framework.Assert.AreEqual(Indent("{\n\t\"foo\": \"bar\",\n\t\"baz\": \"hazzah\"\n}"), JSONOutputter.JSONWriter.ObjectToJSON(null));
		}

		[NUnit.Framework.Test]
		public virtual void TestCollectionJSON()
		{
			NUnit.Framework.Assert.AreEqual(Indent("{\n\t\"foo\": [\n\t\t\"bar\",\n\t\t\"baz\"\n\t]\n}"), JSONOutputter.JSONWriter.ObjectToJSON(null));
		}

		[NUnit.Framework.Test]
		public virtual void TestNestedJSON()
		{
			NUnit.Framework.Assert.AreEqual(Indent("{\n\t\"foo\": {\n\t\t\"bar\": \"baz\"\n\t}\n}"), JSONOutputter.JSONWriter.ObjectToJSON(null));
		}

		[NUnit.Framework.Test]
		public virtual void TestComplexJSON()
		{
			NUnit.Framework.Assert.AreEqual(Indent("{\n\t\"1.1\": {\n\t\t\"2.1\": [\n\t\t\t\"a\",\n\t\t\t\"b\",\n\t\t\t{\n\t\t\t\t\"3.1\": \"v3.1\"\n\t\t\t}\n\t\t],\n\t\t\"2.2\": \"v2.2\"\n\t}\n}"), JSONOutputter.JSONWriter.ObjectToJSON(null));
		}

		// -----
		// BEGIN TESTS FOR ANNOTATION WRITING
		// -----
		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestSimpleDocument()
		{
			Annotation ann = new Annotation("JSON is neat. Better than XML.");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(new _Properties_88());
			pipeline.Annotate(ann);
			string actual = new JSONOutputter().Print(ann);
			string expected = Indent("{\n" + "\t\"sentences\": [\n" + "\t\t{\n" + "\t\t\t\"index\": 0,\n" + "\t\t\t\"tokens\": [\n" + "\t\t\t\t{\n" + "\t\t\t\t\t\"index\": 1,\n" + "\t\t\t\t\t\"word\": \"JSON\",\n" + "\t\t\t\t\t\"originalText\": \"JSON\",\n"
				 + "\t\t\t\t\t\"characterOffsetBegin\": 0,\n" + "\t\t\t\t\t\"characterOffsetEnd\": 4,\n" + "\t\t\t\t\t\"before\": \"\",\n" + "\t\t\t\t\t\"after\": \" \"\n" + "\t\t\t\t},\n" + "\t\t\t\t{\n" + "\t\t\t\t\t\"index\": 2,\n" + "\t\t\t\t\t\"word\": \"is\",\n"
				 + "\t\t\t\t\t\"originalText\": \"is\",\n" + "\t\t\t\t\t\"characterOffsetBegin\": 5,\n" + "\t\t\t\t\t\"characterOffsetEnd\": 7,\n" + "\t\t\t\t\t\"before\": \" \",\n" + "\t\t\t\t\t\"after\": \" \"\n" + "\t\t\t\t},\n" + "\t\t\t\t{\n" + "\t\t\t\t\t\"index\": 3,\n"
				 + "\t\t\t\t\t\"word\": \"neat\",\n" + "\t\t\t\t\t\"originalText\": \"neat\",\n" + "\t\t\t\t\t\"characterOffsetBegin\": 8,\n" + "\t\t\t\t\t\"characterOffsetEnd\": 12,\n" + "\t\t\t\t\t\"before\": \" \",\n" + "\t\t\t\t\t\"after\": \"\"\n" + "\t\t\t\t},\n"
				 + "\t\t\t\t{\n" + "\t\t\t\t\t\"index\": 4,\n" + "\t\t\t\t\t\"word\": \".\",\n" + "\t\t\t\t\t\"originalText\": \".\",\n" + "\t\t\t\t\t\"characterOffsetBegin\": 12,\n" + "\t\t\t\t\t\"characterOffsetEnd\": 13,\n" + "\t\t\t\t\t\"before\": \"\",\n"
				 + "\t\t\t\t\t\"after\": \" \"\n" + "\t\t\t\t}\n" + "\t\t\t]\n" + "\t\t},\n" + "\t\t{\n" + "\t\t\t\"index\": 1,\n" + "\t\t\t\"tokens\": [\n" + "\t\t\t\t{\n" + "\t\t\t\t\t\"index\": 1,\n" + "\t\t\t\t\t\"word\": \"Better\",\n" + "\t\t\t\t\t\"originalText\": \"Better\",\n"
				 + "\t\t\t\t\t\"characterOffsetBegin\": 14,\n" + "\t\t\t\t\t\"characterOffsetEnd\": 20,\n" + "\t\t\t\t\t\"before\": \" \",\n" + "\t\t\t\t\t\"after\": \" \"\n" + "\t\t\t\t},\n" + "\t\t\t\t{\n" + "\t\t\t\t\t\"index\": 2,\n" + "\t\t\t\t\t\"word\": \"than\",\n"
				 + "\t\t\t\t\t\"originalText\": \"than\",\n" + "\t\t\t\t\t\"characterOffsetBegin\": 21,\n" + "\t\t\t\t\t\"characterOffsetEnd\": 25,\n" + "\t\t\t\t\t\"before\": \" \",\n" + "\t\t\t\t\t\"after\": \" \"\n" + "\t\t\t\t},\n" + "\t\t\t\t{\n" + "\t\t\t\t\t\"index\": 3,\n"
				 + "\t\t\t\t\t\"word\": \"XML\",\n" + "\t\t\t\t\t\"originalText\": \"XML\",\n" + "\t\t\t\t\t\"characterOffsetBegin\": 26,\n" + "\t\t\t\t\t\"characterOffsetEnd\": 29,\n" + "\t\t\t\t\t\"before\": \" \",\n" + "\t\t\t\t\t\"after\": \"\"\n" + "\t\t\t\t},\n"
				 + "\t\t\t\t{\n" + "\t\t\t\t\t\"index\": 4,\n" + "\t\t\t\t\t\"word\": \".\",\n" + "\t\t\t\t\t\"originalText\": \".\",\n" + "\t\t\t\t\t\"characterOffsetBegin\": 29,\n" + "\t\t\t\t\t\"characterOffsetEnd\": 30,\n" + "\t\t\t\t\t\"before\": \"\",\n"
				 + "\t\t\t\t\t\"after\": \"\"\n" + "\t\t\t\t}\n" + "\t\t\t]\n" + "\t\t}\n" + "\t]\n" + "}");
			NUnit.Framework.Assert.AreEqual(expected, actual);
		}

		private sealed class _Properties_88 : Properties
		{
			public _Properties_88()
			{
				{
					this.SetProperty("annotators", "tokenize, ssplit");
				}
			}
		}
	}
}
