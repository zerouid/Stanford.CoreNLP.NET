using System.Collections.Generic;
using Java.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A test for the
	/// <see cref="IterableIterator{E}"/>
	/// .
	/// Notably, I don't entirely trust myself to implement the
	/// <see cref="System.Collections.IEnumerable{T}.Spliterator()"/>
	/// } function
	/// properly.
	/// </summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class IterableIteratorTest
	{
		[NUnit.Framework.Test]
		public virtual void TestBasic()
		{
			string[] strings = new string[] { "do", "re", "mi", "fa", "so", "la", "ti", "do" };
			IEnumerator<string> it = Arrays.AsList(strings).GetEnumerator();
			IterableIterator<string> iterit = new IterableIterator<string>(it);
			NUnit.Framework.Assert.AreEqual("do", iterit.Current);
			NUnit.Framework.Assert.AreEqual("re", iterit.Current);
			NUnit.Framework.Assert.AreEqual("mi", iterit.Current);
			NUnit.Framework.Assert.AreEqual("fa", iterit.Current);
			NUnit.Framework.Assert.AreEqual("so", iterit.Current);
			NUnit.Framework.Assert.AreEqual("la", iterit.Current);
			NUnit.Framework.Assert.AreEqual("ti", iterit.Current);
			NUnit.Framework.Assert.AreEqual("do", iterit.Current);
			NUnit.Framework.Assert.IsFalse(iterit.MoveNext());
		}

		[NUnit.Framework.Test]
		public virtual void TestSpliteratorInSequence()
		{
			List<int> x = new List<int>();
			for (int i = 0; i < 1000; ++i)
			{
				x.Add(i);
			}
			IterableIterator<int> iter = new IterableIterator<int>(x.GetEnumerator());
			ISpliterator<int> spliterator = iter.Spliterator();
			IStream<int> stream = StreamSupport.Stream(spliterator, false);
			int[] next = new int[] { 0 };
			stream.ForEach(null);
		}

		[NUnit.Framework.Test]
		public virtual void TestSpliteratorInParallel()
		{
			List<int> x = new List<int>();
			for (int i = 0; i < 1000; ++i)
			{
				x.Add(i);
			}
			IterableIterator<int> iter = new IterableIterator<int>(x.GetEnumerator());
			ISpliterator<int> spliterator = iter.Spliterator();
			IStream<int> stream = StreamSupport.Stream(spliterator, true);
			bool[] seen = new bool[1000];
			stream.ForEach(null);
			for (int i_1 = 0; i_1 < 1000; ++i_1)
			{
				NUnit.Framework.Assert.IsTrue(seen[i_1]);
			}
		}
	}
}
