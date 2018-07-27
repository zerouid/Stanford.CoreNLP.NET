using System;


namespace Edu.Stanford.Nlp.Util
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class PriorityQueueTest
	{
		[NUnit.Framework.Test]
		public virtual void TestBinaryHeapPriorityQueue()
		{
			RunBasicTests("edu.stanford.nlp.util.BinaryHeapPriorityQueue");
			RunRelaxingTests("edu.stanford.nlp.util.BinaryHeapPriorityQueue");
		}

		[NUnit.Framework.Test]
		public virtual void TestFixedPrioritiesPriorityQueue()
		{
			RunBasicTests("edu.stanford.nlp.util.FixedPrioritiesPriorityQueue");
			RunNotRelaxingTests("edu.stanford.nlp.util.FixedPrioritiesPriorityQueue");
		}

		private static void RunBasicTests(string className)
		{
			IPriorityQueue<string> queue;
			try
			{
				queue = ErasureUtils.UncheckedCast(System.Activator.CreateInstance(Sharpen.Runtime.GetType(className)));
			}
			catch (Exception e)
			{
				Fail(e.ToString());
				return;
			}
			RunBasicTests(queue);
		}

		protected internal static void RunBasicTests(IPriorityQueue<string> queue)
		{
			queue.Add("a", 1.0);
			NUnit.Framework.Assert.AreEqual("Added a:1", "[a=1.0]", queue.ToString());
			queue.Add("b", 2.0);
			NUnit.Framework.Assert.AreEqual("Added b:2", "[b=2.0, a=1.0]", queue.ToString());
			queue.Add("c", 1.5);
			NUnit.Framework.Assert.AreEqual("Added c:1.5", "[b=2.0, c=1.5, a=1.0]", queue.ToString());
			NUnit.Framework.Assert.AreEqual("removeFirst()", "b", queue.RemoveFirst());
			NUnit.Framework.Assert.AreEqual("queue", "[c=1.5, a=1.0]", queue.ToString());
			NUnit.Framework.Assert.AreEqual("removeFirst()", "c", queue.RemoveFirst());
			NUnit.Framework.Assert.AreEqual("queue", "[a=1.0]", queue.ToString());
			NUnit.Framework.Assert.AreEqual("removeFirst()", "a", queue.RemoveFirst());
			NUnit.Framework.Assert.IsTrue(queue.IsEmpty());
		}

		private static void RunRelaxingTests(string className)
		{
			BinaryHeapPriorityQueue<string> queue;
			try
			{
				queue = ErasureUtils.UncheckedCast(System.Activator.CreateInstance(Sharpen.Runtime.GetType(className)));
			}
			catch (Exception e)
			{
				Fail(e.ToString());
				return;
			}
			RunRelaxingTests(queue);
		}

		protected internal static void RunRelaxingTests(BinaryHeapPriorityQueue<string> queue)
		{
			queue.Add("a", 1.0);
			NUnit.Framework.Assert.AreEqual("Added a:1", "[a=1.0]", queue.ToString());
			queue.Add("b", 2.0);
			NUnit.Framework.Assert.AreEqual("Added b:2", "[b=2.0, a=1.0]", queue.ToString());
			queue.Add("c", 1.5);
			NUnit.Framework.Assert.AreEqual("Added c:1.5", "[b=2.0, c=1.5, a=1.0]", queue.ToString());
			queue.RelaxPriority("a", 3.0);
			NUnit.Framework.Assert.AreEqual("Increased a to 3", "[a=3.0, b=2.0, c=1.5]", queue.ToString());
			queue.DecreasePriority("b", 0.0);
			NUnit.Framework.Assert.AreEqual("Decreased b to 0", "[a=3.0, c=1.5, b=0.0]", queue.ToString());
			NUnit.Framework.Assert.AreEqual("removeFirst()", "a", queue.RemoveFirst());
			NUnit.Framework.Assert.AreEqual("queue", "[c=1.5, b=0.0]", queue.ToString());
			NUnit.Framework.Assert.AreEqual("removeFirst()", "c", queue.RemoveFirst());
			NUnit.Framework.Assert.AreEqual("queue", "[b=0.0]", queue.ToString());
			NUnit.Framework.Assert.AreEqual("removeFirst()", "b", queue.RemoveFirst());
			NUnit.Framework.Assert.IsTrue(queue.IsEmpty());
		}

		private static void RunNotRelaxingTests(string className)
		{
			FixedPrioritiesPriorityQueue<string> pq;
			try
			{
				pq = ErasureUtils.UncheckedCast(System.Activator.CreateInstance(Sharpen.Runtime.GetType(className)));
			}
			catch (Exception e)
			{
				Fail(e.ToString());
				return;
			}
			NUnit.Framework.Assert.AreEqual("[]", pq.ToString());
			pq.Add("one", 1);
			NUnit.Framework.Assert.AreEqual("[one=1.0]", pq.ToString());
			pq.Add("three", 3);
			NUnit.Framework.Assert.AreEqual("[three=3.0, one=1.0]", pq.ToString());
			pq.Add("one", 1.1);
			NUnit.Framework.Assert.AreEqual("[three=3.0, one=1.1, one=1.0]", pq.ToString());
			pq.Add("two", 2);
			NUnit.Framework.Assert.AreEqual("[three=3.0, two=2.0, one=1.1, one=1.0]", pq.ToString());
			NUnit.Framework.Assert.AreEqual("[three=3.000, two=2.000, ...]", pq.ToString(2));
			FixedPrioritiesPriorityQueue<string> clone = pq.Clone();
			NUnit.Framework.Assert.AreEqual(3.0, clone.GetPriority());
			NUnit.Framework.Assert.AreEqual(pq.Current, clone.Current);
			NUnit.Framework.Assert.AreEqual(2.0, clone.GetPriority());
			NUnit.Framework.Assert.AreEqual(pq.Current, clone.Current);
			NUnit.Framework.Assert.AreEqual(1.1, clone.GetPriority());
			NUnit.Framework.Assert.AreEqual(pq.Current, clone.Current);
			NUnit.Framework.Assert.AreEqual(1.0, clone.GetPriority());
			NUnit.Framework.Assert.AreEqual(pq.Current, clone.Current);
			NUnit.Framework.Assert.IsFalse(clone.MoveNext());
			NUnit.Framework.Assert.IsTrue(clone.IsEmpty());
		}
	}
}
