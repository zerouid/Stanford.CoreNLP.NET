using System;
using System.Collections.Generic;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Surface
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class AnnotatedTextReaderTest
	{
		[NUnit.Framework.Test]
		public virtual void TestParse()
		{
			try
			{
				string text = "I am going to be in <LOC> Italy </LOC> sometime, soon. Specifically in <LOC> Tuscany </LOC> .";
				ICollection<string> labels = new HashSet<string>();
				labels.Add("LOC");
				System.Console.Out.WriteLine(AnnotatedTextReader.ParseFile(new BufferedReader(new StringReader(text)), labels, null, true, string.Empty));
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
