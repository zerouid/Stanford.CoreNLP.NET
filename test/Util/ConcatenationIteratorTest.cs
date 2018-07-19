using System.Collections.Generic;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <author>Sebastian Riedel</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ConcatenationIteratorTest
	{
		[NUnit.Framework.Test]
		public virtual void TestIterator()
		{
			ICollection<string> c1 = Collections.Singleton("a");
			ICollection<string> c2 = Java.Util.Collections.Singleton("b");
			IEnumerator<string> i = new ConcatenationIterator<string>(c1.GetEnumerator(), c2.GetEnumerator());
			NUnit.Framework.Assert.AreEqual("a", i.Current);
			NUnit.Framework.Assert.AreEqual("b", i.Current);
			NUnit.Framework.Assert.IsFalse(i.MoveNext());
		}
	}
}
