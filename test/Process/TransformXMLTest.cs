using System.IO;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Process
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TransformXMLTest
	{
		private string testCase = "<doc><el arg=\"funny&amp;'&gt;&quot;stuff\">yo! C&amp;C! </el></doc>";

		private string expectedAnswer = "<doc> <el arg=\"funny&amp;&apos;&gt;&quot;stuff\"> yo! C&amp;C! </el> </doc>";

		private string expectedAnswer2 = "<doc> <el arg=\"funny&amp;&apos;&gt;&quot;stuff\"> yo! C&amp;C!yo! C&amp;C! </el> </doc>";

		private IFunction<string, string> duplicate = null;

		[NUnit.Framework.Test]
		public virtual void TestTransformXML1()
		{
			TransformXML<string> tx = new TransformXML<string>();
			StringWriter sw = new StringWriter();
			tx.TransformXML(StringUtils.EmptyStringArray, new IdentityFunction<string>(), new ByteArrayInputStream(Sharpen.Runtime.GetBytesForString(testCase)), sw);
			string answer = sw.ToString().ReplaceAll("\\s+", " ").Trim();
			NUnit.Framework.Assert.AreEqual("Bad XML transform", expectedAnswer, answer);
			sw = new StringWriter();
			tx.TransformXML(new string[] { "el" }, duplicate, new ByteArrayInputStream(Sharpen.Runtime.GetBytesForString(testCase)), sw);
			string answer2 = sw.ToString().ReplaceAll("\\s+", " ").Trim();
			NUnit.Framework.Assert.AreEqual("Bad XML transform", expectedAnswer2, answer2);
		}
	}
}
