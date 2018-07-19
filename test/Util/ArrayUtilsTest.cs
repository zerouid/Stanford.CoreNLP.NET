using System;
using System.Collections.Generic;
using Java.Util;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <author>Christopher Manning</author>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	public class ArrayUtilsTest
	{
		private int[] sampleGaps = new int[] { 1, 5, 6, 10, 17, 22, 29, 33, 100, 1000, 10000, 9999999 };

		private int[] sampleBadGaps = new int[] { 1, 6, 5, 10, 17 };

		[Test]
		public virtual void TestEqualContentsInt()
		{
			NUnit.Framework.Assert.IsTrue(ArrayUtils.EqualContents(sampleGaps, sampleGaps));
			NUnit.Framework.Assert.IsTrue(ArrayUtils.EqualContents(sampleBadGaps, sampleBadGaps));
			NUnit.Framework.Assert.IsFalse(ArrayUtils.EqualContents(sampleGaps, sampleBadGaps));
		}

		[Test]
		public virtual void TestGaps()
		{
			byte[] encoded = ArrayUtils.GapEncode(sampleGaps);
			int[] decoded = ArrayUtils.GapDecode(encoded);
			NUnit.Framework.Assert.IsTrue(ArrayUtils.EqualContents(decoded, sampleGaps));
			try
			{
				ArrayUtils.GapEncode(sampleBadGaps);
				throw new Exception("Expected an IllegalArgumentException");
			}
			catch (ArgumentException)
			{
			}
		}

		// yay, we passed
		[Test]
		public virtual void TestDelta()
		{
			byte[] encoded = ArrayUtils.DeltaEncode(sampleGaps);
			int[] decoded = ArrayUtils.DeltaDecode(encoded);
			NUnit.Framework.Assert.IsTrue(ArrayUtils.EqualContents(decoded, sampleGaps));
			try
			{
				ArrayUtils.DeltaEncode(sampleBadGaps);
				throw new Exception("Expected an IllegalArgumentException");
			}
			catch (ArgumentException)
			{
			}
		}

		// yay, we passed
		[Test]
		public virtual void TestRemoveAt()
		{
			string[] strings = new string[] { "a", "b", "c" };
			strings = (string[])ArrayUtils.RemoveAt(strings, 2);
			int i = 0;
			foreach (string @string in strings)
			{
				if (i == 0)
				{
					NUnit.Framework.Assert.AreEqual("a", @string);
				}
				else
				{
					if (i == 1)
					{
						NUnit.Framework.Assert.AreEqual("b", @string);
					}
					else
					{
						NUnit.Framework.Assert.Fail("Array is too big!");
					}
				}
				i++;
			}
		}

		[Test]
		public virtual void TestAsSet()
		{
			string[] items = new string[] { "larry", "moe", "curly" };
			ICollection<string> set = new HashSet<string>(Arrays.AsList(items));
			NUnit.Framework.Assert.AreEqual(set, ArrayUtils.AsSet(items));
		}

		[Test]
		public virtual void TestgetSubListIndex()
		{
			string[] t1 = new string[] { "this", "is", "test" };
			string[] t2 = new string[] { "well", "this", "is", "not", "this", "is", "test", "also" };
			NUnit.Framework.Assert.AreEqual(4, (ArrayUtils.GetSubListIndex(t1, t2)[0]));
			string[] t3 = new string[] { "cough", "increased" };
			string[] t4 = new string[] { "i", "dont", "really", "cough" };
			NUnit.Framework.Assert.AreEqual(0, ArrayUtils.GetSubListIndex(t3, t4).Count);
			string[] t5 = new string[] { "cough", "increased" };
			string[] t6 = new string[] { "cough", "aggravated" };
			NUnit.Framework.Assert.AreEqual(0, ArrayUtils.GetSubListIndex(t5, t6).Count);
			string[] t7 = new string[] { "cough", "increased" };
			string[] t8 = new string[] { "cough", "aggravated", "cough", "increased", "and", "cough", "increased", "and", "cough", "and", "increased" };
			NUnit.Framework.Assert.AreEqual(2, ArrayUtils.GetSubListIndex(t7, t8)[0]);
			NUnit.Framework.Assert.AreEqual(5, ArrayUtils.GetSubListIndex(t7, t8)[1]);
		}
	}
}
