using System.Collections.Generic;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Test for the interval tree.</summary>
	/// <author>Angel Chang</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class IntervalTreeTest
	{
		private static void CheckOverlapping(ICollection<Interval<int>> all, ICollection<Interval<int>> overlapping, Interval<int> target)
		{
			foreach (Interval<int> interval in all)
			{
				NUnit.Framework.Assert.IsNotNull(interval);
			}
			foreach (Interval<int> interval_1 in overlapping)
			{
				NUnit.Framework.Assert.IsTrue(interval_1.Overlaps(target));
			}
			IList<Interval<int>> rest = new List<Interval<int>>(all);
			rest.RemoveAll(overlapping);
			foreach (Interval<int> interval_2 in rest)
			{
				NUnit.Framework.Assert.IsNotNull(interval_2);
				NUnit.Framework.Assert.IsFalse("Should not overlap: " + interval_2 + " with " + target, interval_2.Overlaps(target));
			}
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestGetOverlapping()
		{
			Interval<int> a = Interval.ToInterval(249210699, 249212659);
			Interval<int> before = Interval.ToInterval(249210000, 249210600);
			Interval<int> included = Interval.ToInterval(249210800, 249212000);
			Interval<int> after = Interval.ToInterval(249213000, 249214000);
			IntervalTree<int, Interval<int>> tree = new IntervalTree<int, Interval<int>>();
			tree.Add(a);
			IList<Interval<int>> overlapping1 = tree.GetOverlapping(before);
			NUnit.Framework.Assert.IsTrue(overlapping1.IsEmpty());
			IList<Interval<int>> overlapping2 = tree.GetOverlapping(included);
			NUnit.Framework.Assert.IsTrue(overlapping2.Count == 1);
			IList<Interval<int>> overlapping3 = tree.GetOverlapping(after);
			NUnit.Framework.Assert.IsTrue(overlapping3.IsEmpty());
			// Remove a
			tree.Remove(a);
			NUnit.Framework.Assert.IsTrue(tree.Count == 0);
			int n = 20000;
			// Add a bunch of interval before adding a
			for (int i = 0; i < n; i++)
			{
				int x = i;
				int y = i + 1;
				Interval<int> interval = Interval.ToInterval(x, y);
				tree.Add(interval);
			}
			tree.Add(a);
			overlapping1 = tree.GetOverlapping(before);
			NUnit.Framework.Assert.IsTrue(overlapping1.IsEmpty());
			overlapping2 = tree.GetOverlapping(included);
			NUnit.Framework.Assert.IsTrue(overlapping2.Count == 1);
			overlapping3 = tree.GetOverlapping(after);
			NUnit.Framework.Assert.IsTrue(overlapping3.IsEmpty());
			NUnit.Framework.Assert.IsTrue(tree.Height() < 20);
			// Try balancing the tree
			//    System.out.println("Height is " + tree.height());
			tree.Check();
			tree.Balance();
			int height = tree.Height();
			NUnit.Framework.Assert.IsTrue(height < 20);
			tree.Check();
			overlapping1 = tree.GetOverlapping(before);
			NUnit.Framework.Assert.IsTrue(overlapping1.IsEmpty());
			overlapping2 = tree.GetOverlapping(included);
			NUnit.Framework.Assert.IsTrue(overlapping2.Count == 1);
			overlapping3 = tree.GetOverlapping(after);
			NUnit.Framework.Assert.IsTrue(overlapping3.IsEmpty());
			// Clear tree
			tree.Clear();
			NUnit.Framework.Assert.IsTrue(tree.Count == 0);
			// Add a bunch of random interval before adding a
			Random rand = new Random();
			IList<Interval<int>> list = new List<Interval<int>>(n + 1);
			for (int i_1 = 0; i_1 < n; i_1++)
			{
				int x = rand.NextInt();
				int y = rand.NextInt();
				Interval<int> interval = Interval.ToValidInterval(x, y);
				tree.Add(interval);
				list.Add(interval);
			}
			tree.Add(a);
			list.Add(a);
			overlapping1 = tree.GetOverlapping(before);
			CheckOverlapping(list, overlapping1, before);
			overlapping2 = tree.GetOverlapping(included);
			CheckOverlapping(list, overlapping2, included);
			overlapping3 = tree.GetOverlapping(after);
			CheckOverlapping(list, overlapping3, after);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestIteratorRandom()
		{
			int n = 1000;
			IntervalTree<int, Interval<int>> tree = new IntervalTree<int, Interval<int>>();
			Random rand = new Random();
			IList<Interval<int>> list = new List<Interval<int>>(n + 1);
			for (int i = 0; i < n; i++)
			{
				int x = rand.NextInt();
				int y = rand.NextInt();
				Interval<int> interval = Interval.ToValidInterval(x, y);
				tree.Add(interval);
				list.Add(interval);
			}
			list.Sort();
			Interval<int> next = null;
			IEnumerator<Interval<int>> iterator = tree.GetEnumerator();
			for (int i_1 = 0; i_1 < list.Count; i_1++)
			{
				NUnit.Framework.Assert.IsTrue("HasItem " + i_1, iterator.MoveNext());
				next = iterator.Current;
				NUnit.Framework.Assert.AreEqual("Item " + i_1, list[i_1], next);
			}
			NUnit.Framework.Assert.IsFalse("No more items", iterator.MoveNext());
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestIteratorOrdered()
		{
			int n = 1000;
			IntervalTree<int, Interval<int>> tree = new IntervalTree<int, Interval<int>>();
			IList<Interval<int>> list = new List<Interval<int>>(n + 1);
			for (int i = 0; i < n; i++)
			{
				int x = i;
				int y = i + 1;
				Interval<int> interval = Interval.ToValidInterval(x, y);
				tree.Add(interval);
				list.Add(interval);
			}
			list.Sort();
			Interval<int> next = null;
			IEnumerator<Interval<int>> iterator = tree.GetEnumerator();
			for (int i_1 = 0; i_1 < list.Count; i_1++)
			{
				NUnit.Framework.Assert.IsTrue("HasItem " + i_1, iterator.MoveNext());
				next = iterator.Current;
				NUnit.Framework.Assert.AreEqual("Item " + i_1, list[i_1], next);
			}
			NUnit.Framework.Assert.IsFalse("No more items", iterator.MoveNext());
		}
	}
}
