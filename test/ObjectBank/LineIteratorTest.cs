using System;
using System.Collections.Generic;
using Java.IO;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Objectbank
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	public class LineIteratorTest
	{
		[Test]
		public virtual void TestLineIterator()
		{
			string s = "\n\n@@123\nthis\nis\na\nsentence\n\n@@124\nThis\nis another\n.\n\n@125\nThis is the\tlast\n";
			string[] output = new string[] { string.Empty, string.Empty, "@@123", "this", "is", "a", "sentence", string.Empty, "@@124", "This", "is another", ".", string.Empty, "@125", "This is the\tlast" };
			IEnumerator<string> di = new LineIterator<string>(new StringReader(s), new IdentityFunction<string>());
			try
			{
				foreach (string @out in output)
				{
					string ans = di.Current;
					// System.out.println(ans);
					NUnit.Framework.Assert.AreEqual(@out, ans, "Wrong line");
				}
				if (di.MoveNext())
				{
					NUnit.Framework.Assert.Fail("Too many things in iterator: " + di.Current);
				}
			}
			catch (Exception e)
			{
				NUnit.Framework.Assert.Fail("Probably too few things in iterator: " + e);
			}
		}
	}
}
