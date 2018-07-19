using System.Collections.Generic;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ComparatorsTest
	{
		[NUnit.Framework.Test]
		public virtual void TestNullSafeComparator()
		{
			IComparator<int> comp = Comparators.NullSafeNaturalComparator();
			NUnit.Framework.Assert.AreEqual(0, comp.Compare(null, null));
			NUnit.Framework.Assert.AreEqual(-1, comp.Compare(null, int.Parse(42)));
			NUnit.Framework.Assert.AreEqual(1, comp.Compare(int.Parse(42), null));
			NUnit.Framework.Assert.AreEqual(-1, comp.Compare(11, 18));
			NUnit.Framework.Assert.AreEqual(0, comp.Compare(11, 11));
		}

		[NUnit.Framework.Test]
		public virtual void TestListComparator()
		{
			IComparator<IList<string>> lc = Comparators.GetListComparator();
			string[] one = new string[] { "hello", "foo" };
			string[] two = new string[] { "hi", "foo" };
			string[] three = new string[] { "hi", "foo", "bar" };
			NUnit.Framework.Assert.IsTrue(lc.Compare(Arrays.AsList(one), Arrays.AsList(one)) == 0);
			NUnit.Framework.Assert.IsTrue(lc.Compare(Arrays.AsList(one), Arrays.AsList(two)) < 0);
			NUnit.Framework.Assert.IsTrue(lc.Compare(Arrays.AsList(one), Arrays.AsList(three)) < 0);
			NUnit.Framework.Assert.IsTrue(lc.Compare(Arrays.AsList(three), Arrays.AsList(two)) > 0);
		}

		private static void Compare<C>(C[] a1, C[] a2)
			where C : IComparable
		{
			System.Console.Out.Printf("compare(%s, %s) = %d%n", Arrays.ToString(a1), Arrays.ToString(a2), ArrayUtils.CompareArrays(a1, a2));
		}

		[NUnit.Framework.Test]
		public virtual void TestArrayComparator()
		{
			IComparator<bool[]> ac = Comparators.GetArrayComparator();
			NUnit.Framework.Assert.IsTrue(ac.Compare(new bool[] { true, false, true }, new bool[] { true, false, true }) == 0);
			NUnit.Framework.Assert.IsTrue(ac.Compare(new bool[] { true, false, true }, new bool[] { true, false }) > 0);
			NUnit.Framework.Assert.IsTrue(ac.Compare(new bool[] { true, false, true }, new bool[] { true, false, true, false }) < 0);
			NUnit.Framework.Assert.IsTrue(ac.Compare(new bool[] { false, false, true }, new bool[] { true, false, true }) < 0);
		}
	}
}
