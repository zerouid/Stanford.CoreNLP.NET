using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Tests some basic operations on the TreeShapedStack</summary>
	/// <author>Danqi Chen</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TreeShapedStackTest
	{
		[NUnit.Framework.Test]
		public virtual void TestEquals()
		{
			TreeShapedStack<string> t1 = new TreeShapedStack<string>();
			t1 = t1.Push("foo");
			t1 = t1.Push("bar");
			t1 = t1.Push("bar");
			t1 = t1.Push("diet");
			t1 = t1.Push("coke");
			TreeShapedStack<string> t2 = new TreeShapedStack<string>();
			t2 = t2.Push("foo");
			t2 = t2.Push("bar");
			t2 = t2.Push("bar");
			t2 = t2.Push("diet");
			t2 = t2.Push("coke");
			TreeShapedStack<string> t3 = t2.Pop().Push("pepsi");
			NUnit.Framework.Assert.AreEqual(t1, t2);
			NUnit.Framework.Assert.IsFalse(t1.Pop().Equals(t2));
			NUnit.Framework.Assert.IsFalse(t2.Pop().Equals(t1));
			NUnit.Framework.Assert.IsFalse(t2.Equals(t3));
		}

		[NUnit.Framework.Test]
		public virtual void TestBasicOperations()
		{
			TreeShapedStack<string> tss = new TreeShapedStack<string>();
			NUnit.Framework.Assert.AreEqual(tss.size, 0);
			TreeShapedStack<string> tss1 = tss.Push("1");
			NUnit.Framework.Assert.AreEqual(tss1.size, 1);
			NUnit.Framework.Assert.AreEqual(tss1.Peek(), "1");
			TreeShapedStack<string> tss2 = tss1.Push("2");
			NUnit.Framework.Assert.AreEqual(tss2.size, 2);
			NUnit.Framework.Assert.AreEqual(tss2.Peek(), "2");
			NUnit.Framework.Assert.AreEqual(tss2.previous.Peek(), "1");
			TreeShapedStack<string> tss3 = tss2.Push("3");
			NUnit.Framework.Assert.AreEqual(tss3.size, 3);
			NUnit.Framework.Assert.AreEqual(tss3.Peek(), "3");
			NUnit.Framework.Assert.AreEqual(tss3.previous.Peek(), "2");
			tss3 = tss3.Pop();
			NUnit.Framework.Assert.AreEqual(tss3.Peek(), "2");
			NUnit.Framework.Assert.AreEqual(tss3.previous.Peek(), "1");
			NUnit.Framework.Assert.AreEqual(tss3.Peek(), "2");
			TreeShapedStack<string> tss4 = tss3.Push("4");
			NUnit.Framework.Assert.AreEqual(tss4.Peek(), "4");
			NUnit.Framework.Assert.AreEqual(tss4.Peek(), "4");
			NUnit.Framework.Assert.AreEqual(tss4.previous.Peek(), "2");
			tss4 = tss4.Pop();
			NUnit.Framework.Assert.AreEqual(tss4.Peek(), "2");
			tss4 = tss4.Pop();
			NUnit.Framework.Assert.AreEqual(tss4.Peek(), "1");
			tss4 = tss4.Pop();
			NUnit.Framework.Assert.AreEqual(tss4.size, 0);
		}
	}
}
