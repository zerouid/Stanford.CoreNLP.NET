


namespace Edu.Stanford.Nlp.Util
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class XMLUtilsTest
	{
		// Copyright 2010, Stanford NLP
		// Author: John Bauer
		// So far, this test only tests the stripTags() method.
		// TODO: test everything else
		[NUnit.Framework.Test]
		public virtual void TestStripTags()
		{
			string text = "<song><lyrics>Do you think I'm special</lyrics><br><lyrics>Do you think I'm nice</lyrics><br><lyrics whining=\"excessive\">Am I bright enough to shine in your spaces?</lyrics></song>";
			string expectedBreakingResult = "Do you think I'm special\nDo you think I'm nice\nAm I bright enough to shine in your spaces?";
			string result = XMLUtils.StripTags(new BufferedReader(new StringReader(text)), null, true);
			NUnit.Framework.Assert.AreEqual(expectedBreakingResult, result);
			string expectedNoBreakingResult = "Do you think I'm specialDo you think I'm niceAm I bright enough to shine in your spaces?";
			result = XMLUtils.StripTags(new BufferedReader(new StringReader(text)), null, false);
			NUnit.Framework.Assert.AreEqual(expectedNoBreakingResult, result);
		}

		[NUnit.Framework.Test]
		public virtual void TestXMLTag()
		{
			XMLUtils.XMLTag foo = new XMLUtils.XMLTag("<br />");
			NUnit.Framework.Assert.AreEqual("br", foo.name);
			NUnit.Framework.Assert.IsTrue(foo.isSingleTag);
			foo = new XMLUtils.XMLTag("<List  name  =   \"Fruit List\"    >");
			NUnit.Framework.Assert.AreEqual("List", foo.name);
			NUnit.Framework.Assert.IsFalse(foo.isSingleTag);
			NUnit.Framework.Assert.IsFalse(foo.isEndTag);
			NUnit.Framework.Assert.AreEqual("Fruit List", foo.attributes["name"]);
			foo = new XMLUtils.XMLTag("</life  >");
			NUnit.Framework.Assert.AreEqual("life", foo.name);
			NUnit.Framework.Assert.IsTrue(foo.isEndTag);
			NUnit.Framework.Assert.IsFalse(foo.isSingleTag);
			NUnit.Framework.Assert.IsTrue(foo.attributes.IsEmpty());
			foo = new XMLUtils.XMLTag("<P>");
			NUnit.Framework.Assert.AreEqual("P", foo.name);
		}
	}
}
