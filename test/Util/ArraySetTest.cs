


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Tests the ArraySet class by running it through some standard
	/// set operations.
	/// </summary>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ArraySetTest
	{
		internal ArraySet<int> set;

		/// <summary>Creates a small set of 3 elements.</summary>
		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			set = new ArraySet<int>();
			set.Add(5);
			set.Add(10);
			set.Add(8);
		}

		[NUnit.Framework.Test]
		public virtual void TestEquals()
		{
			NUnit.Framework.Assert.IsTrue(set.Equals(set));
			HashSet<int> hset = new HashSet<int>();
			Sharpen.Collections.AddAll(hset, set);
			NUnit.Framework.Assert.IsTrue(set.Equals(hset));
			NUnit.Framework.Assert.IsTrue(hset.Equals(set));
		}

		/// <summary>Tests the set add function.</summary>
		/// <remarks>
		/// Tests the set add function.
		/// Note that add is probably already tested by the combination of
		/// setUp and testEquals.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestAdd()
		{
			NUnit.Framework.Assert.IsTrue(set.Contains(5));
			NUnit.Framework.Assert.IsFalse(set.Contains(4));
			for (int i = 0; i < 11; ++i)
			{
				set.Add(i);
			}
			// 0..10, existing elements should not be readded
			NUnit.Framework.Assert.AreEqual(11, set.Count);
			NUnit.Framework.Assert.IsTrue(set.Contains(5));
			NUnit.Framework.Assert.IsTrue(set.Contains(4));
		}

		[NUnit.Framework.Test]
		public virtual void TestRemove()
		{
			NUnit.Framework.Assert.IsFalse(set.Contains(2));
			NUnit.Framework.Assert.IsTrue(set.Contains(5));
			set.Remove(5);
			NUnit.Framework.Assert.AreEqual(2, set.Count);
			NUnit.Framework.Assert.IsFalse(set.Contains(2));
			NUnit.Framework.Assert.IsFalse(set.Contains(5));
		}

		[NUnit.Framework.Test]
		public virtual void TestClear()
		{
			NUnit.Framework.Assert.IsFalse(set.IsEmpty());
			NUnit.Framework.Assert.AreEqual(3, set.Count);
			set.Clear();
			NUnit.Framework.Assert.IsTrue(set.IsEmpty());
			NUnit.Framework.Assert.AreEqual(0, set.Count);
		}
	}
}
