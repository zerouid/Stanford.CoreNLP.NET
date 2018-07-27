using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Util
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class SetsTest
	{
		private ICollection<string> s1;

		private ICollection<string> s2;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			s1 = new HashSet<string>();
			s1.Add("apple");
			s1.Add("banana");
			s1.Add("cherry");
			s1.Add("dingleberry");
			s2 = new HashSet<string>();
			s2.Add("apple");
			s2.Add("banana");
			s2.Add("cranberry");
		}

		[NUnit.Framework.Test]
		public virtual void TestCross()
		{
			ICollection<Pair<string, string>> cross = Sets.Cross(s1, s2);
			NUnit.Framework.Assert.AreEqual(cross.Count, 12);
			Pair<string, string> p = new Pair<string, string>("dingleberry", "cranberry");
			NUnit.Framework.Assert.IsTrue(cross.Contains(p));
		}

		[NUnit.Framework.Test]
		public virtual void TestDiff()
		{
			ICollection<string> diff = Sets.Diff(s1, s2);
			NUnit.Framework.Assert.AreEqual(diff.Count, 2);
			NUnit.Framework.Assert.IsTrue(diff.Contains("cherry"));
			NUnit.Framework.Assert.IsFalse(diff.Contains("apple"));
		}

		[NUnit.Framework.Test]
		public virtual void TestUnion()
		{
			ICollection<string> union = Sets.Union(s1, s2);
			NUnit.Framework.Assert.AreEqual(union.Count, 5);
			NUnit.Framework.Assert.IsTrue(union.Contains("cherry"));
			NUnit.Framework.Assert.IsFalse(union.Contains("fungus"));
		}

		[NUnit.Framework.Test]
		public virtual void TestIntersection()
		{
			ICollection<string> intersection = Sets.Intersection(s1, s2);
			NUnit.Framework.Assert.AreEqual(intersection.Count, 2);
			NUnit.Framework.Assert.IsTrue(intersection.Contains("apple"));
			NUnit.Framework.Assert.IsFalse(intersection.Contains("cherry"));
		}

		[NUnit.Framework.Test]
		public virtual void TestPowerset()
		{
			ICollection<ICollection<string>> pow = Sets.PowerSet(s1);
			NUnit.Framework.Assert.AreEqual(pow.Count, 16);
		}
	}
}
