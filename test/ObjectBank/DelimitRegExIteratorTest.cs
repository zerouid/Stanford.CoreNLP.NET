using System.Collections;
using System.Collections.Generic;


using NUnit.Framework;


namespace Edu.Stanford.Nlp.Objectbank
{
	[NUnit.Framework.TestFixture]
	public class DelimitRegExIteratorTest
	{
		private static readonly string[] testCases = new string[] { "@@123\nthis\nis\na\nsentence\n\n@@124\nThis\nis\nanother\n.\n\n@125\nThis\nis\nthe\nlast\n", "@@123\nthis\nis\na\nsentence\n\n@@124\nThis\nis\nanother\n.\n\n@125\nThis\nis\nthe\nlast\n"
			 };

		private static readonly string[] delimiterCases = new string[] { "\n\n", "a|e" };

		private static readonly IList[] answerCases = new IList[] { Arrays.AsList("@@123\nthis\nis\na\nsentence", "@@124\nThis\nis\nanother\n.", "@125\nThis\nis\nthe\nlast\n"), Arrays.AsList("@@123\nthis\nis\n", "\ns", "nt", "nc", "\n\n@@124\nThis\nis\n"
			, "noth", "r\n.\n\n@125\nThis\nis\nth", "\nl", "st\n") };

		[Test]
		public virtual void TestFunctionality()
		{
			NUnit.Framework.Assert.AreEqual(testCases.Length, delimiterCases.Length);
			NUnit.Framework.Assert.AreEqual(testCases.Length, answerCases.Length);
			for (int i = 0; i < testCases.Length; i++)
			{
				string s = testCases[i];
				DelimitRegExIterator<string> di = DelimitRegExIterator.DefaultDelimitRegExIterator(new StringReader(s), delimiterCases[i]);
				IList<string> answer = new List<string>();
				while (di.MoveNext())
				{
					answer.Add(di.Current);
				}
				NUnit.Framework.Assert.AreEqual(answerCases[i], answer);
			}
		}
	}
}
