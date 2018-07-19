using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>Base tests that should work on any type of Counter.</summary>
	/// <remarks>
	/// Base tests that should work on any type of Counter.  This class
	/// is subclassed by e.g.,
	/// <see cref="ClassicCounterTest"/>
	/// to provide the
	/// particular Counter instance being tested.
	/// </remarks>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public abstract class CounterTestBase
	{
		private ICounter<string> c;

		private readonly bool integral;

		private const double Tolerance = 0.001;

		public CounterTestBase(ICounter<string> c)
			: this(c, false)
		{
		}

		public CounterTestBase(ICounter<string> c, bool integral)
		{
			this.c = c;
			this.integral = integral;
		}

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			c.Clear();
		}

		[NUnit.Framework.Test]
		public virtual void TestClassicCounterHistoricalMain()
		{
			c.SetCount("p", 0);
			c.SetCount("q", 2);
			ClassicCounter<string> small_c = new ClassicCounter<string>(c);
			ICounter<string> c7 = c.GetFactory().Create();
			c7.AddAll(c);
			NUnit.Framework.Assert.AreEqual(c.TotalCount(), 2.0);
			c.IncrementCount("p");
			NUnit.Framework.Assert.AreEqual(c.TotalCount(), 3.0);
			c.IncrementCount("p", 2.0);
			NUnit.Framework.Assert.AreEqual(Counters.Min(c), 2.0);
			NUnit.Framework.Assert.AreEqual(Counters.Argmin(c), "q");
			// Now p is p=3.0, q=2.0
			c.SetCount("w", -5.0);
			c.SetCount("x", -4.5);
			IList<string> biggestKeys = new List<string>(c.KeySet());
			NUnit.Framework.Assert.AreEqual(biggestKeys.Count, 4);
			biggestKeys.Sort(Counters.ToComparator(c, false, true));
			NUnit.Framework.Assert.AreEqual("w", biggestKeys[0]);
			NUnit.Framework.Assert.AreEqual("x", biggestKeys[1]);
			NUnit.Framework.Assert.AreEqual("p", biggestKeys[2]);
			NUnit.Framework.Assert.AreEqual("q", biggestKeys[3]);
			NUnit.Framework.Assert.AreEqual(Counters.Min(c), -5.0, Tolerance);
			NUnit.Framework.Assert.AreEqual(Counters.Argmin(c), "w");
			NUnit.Framework.Assert.AreEqual(Counters.Max(c), 3.0, Tolerance);
			NUnit.Framework.Assert.AreEqual(Counters.Argmax(c), "p");
			if (integral)
			{
				NUnit.Framework.Assert.AreEqual(Counters.Mean(c), -1.0);
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(Counters.Mean(c), -1.125, Tolerance);
			}
			if (!integral)
			{
				// only do this for floating point counters.  Too much bother to rewrite
				c.SetCount("x", -2.5);
				ClassicCounter<string> c2 = new ClassicCounter<string>(c);
				NUnit.Framework.Assert.AreEqual(3.0, c2.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(2.0, c2.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(-5.0, c2.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(-2.5, c2.GetCount("x"));
				ICounter<string> c3 = c.GetFactory().Create();
				foreach (string str in c2.KeySet())
				{
					c3.IncrementCount(str);
				}
				NUnit.Framework.Assert.AreEqual(1.0, c3.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(1.0, c3.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(1.0, c3.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(1.0, c3.GetCount("x"));
				Counters.AddInPlace(c2, c3, 10.0);
				NUnit.Framework.Assert.AreEqual(13.0, c2.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(12.0, c2.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(5.0, c2.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(7.5, c2.GetCount("x"));
				c3.AddAll(c);
				NUnit.Framework.Assert.AreEqual(4.0, c3.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(3.0, c3.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(-4.0, c3.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(-1.5, c3.GetCount("x"));
				Counters.SubtractInPlace(c3, c);
				NUnit.Framework.Assert.AreEqual(1.0, c3.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(1.0, c3.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(1.0, c3.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(1.0, c3.GetCount("x"));
				foreach (string str_1 in c.KeySet())
				{
					c3.IncrementCount(str_1);
				}
				NUnit.Framework.Assert.AreEqual(2.0, c3.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(2.0, c3.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(2.0, c3.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(2.0, c3.GetCount("x"));
				Counters.DivideInPlace(c2, c3);
				NUnit.Framework.Assert.AreEqual(6.5, c2.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(6.0, c2.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(2.5, c2.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(3.75, c2.GetCount("x"));
				Counters.DivideInPlace(c2, 0.5);
				NUnit.Framework.Assert.AreEqual(13.0, c2.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(12.0, c2.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(5.0, c2.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(7.5, c2.GetCount("x"));
				Counters.MultiplyInPlace(c2, 2.0);
				NUnit.Framework.Assert.AreEqual(26.0, c2.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(24.0, c2.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(10.0, c2.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(15.0, c2.GetCount("x"));
				Counters.DivideInPlace(c2, 2.0);
				NUnit.Framework.Assert.AreEqual(13.0, c2.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(12.0, c2.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(5.0, c2.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(7.5, c2.GetCount("x"));
				foreach (string str_2 in c2.KeySet())
				{
					c2.IncrementCount(str_2);
				}
				NUnit.Framework.Assert.AreEqual(14.0, c2.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(13.0, c2.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(6.0, c2.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(8.5, c2.GetCount("x"));
				foreach (string str_3 in c.KeySet())
				{
					c2.IncrementCount(str_3);
				}
				NUnit.Framework.Assert.AreEqual(15.0, c2.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(14.0, c2.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(7.0, c2.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(9.5, c2.GetCount("x"));
				c2.AddAll(small_c);
				NUnit.Framework.Assert.AreEqual(15.0, c2.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(16.0, c2.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(7.0, c2.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(9.5, c2.GetCount("x"));
				NUnit.Framework.Assert.AreEqual(new HashSet<string>(Arrays.AsList("p", "q")), Counters.KeysAbove(c2, 14));
				NUnit.Framework.Assert.AreEqual(new HashSet<string>(Arrays.AsList("q")), Counters.KeysAt(c2, 16));
				NUnit.Framework.Assert.AreEqual(new HashSet<string>(Arrays.AsList("x", "w")), Counters.KeysBelow(c2, 9.5));
				Counters.AddInPlace(c2, small_c, -6);
				NUnit.Framework.Assert.AreEqual(15.0, c2.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(4.0, c2.GetCount("q"));
				NUnit.Framework.Assert.AreEqual(7.0, c2.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(9.5, c2.GetCount("x"));
				Counters.SubtractInPlace(c2, small_c);
				Counters.SubtractInPlace(c2, small_c);
				Counters.RetainNonZeros(c2);
				NUnit.Framework.Assert.AreEqual(15.0, c2.GetCount("p"));
				NUnit.Framework.Assert.IsFalse(c2.ContainsKey("q"));
				NUnit.Framework.Assert.AreEqual(7.0, c2.GetCount("w"));
				NUnit.Framework.Assert.AreEqual(9.5, c2.GetCount("x"));
			}
			// serialize to Stream
			if (c is ISerializable)
			{
				try
				{
					ByteArrayOutputStream baos = new ByteArrayOutputStream();
					ObjectOutputStream @out = new ObjectOutputStream(new BufferedOutputStream(baos));
					@out.WriteObject(c);
					@out.Close();
					// reconstitute
					byte[] bytes = baos.ToByteArray();
					ObjectInputStream @in = new ObjectInputStream(new BufferedInputStream(new ByteArrayInputStream(bytes)));
					c = IOUtils.ReadObjectFromObjectStream(@in);
					@in.Close();
					if (!this.integral)
					{
						NUnit.Framework.Assert.AreEqual(-2.5, c.TotalCount());
						NUnit.Framework.Assert.AreEqual(-5.0, Counters.Min(c));
						NUnit.Framework.Assert.AreEqual("w", Counters.Argmin(c));
					}
					c.Clear();
					if (!this.integral)
					{
						NUnit.Framework.Assert.AreEqual(0.0, c.TotalCount());
					}
				}
				catch (IOException ioe)
				{
					Fail("IOException: " + ioe);
				}
				catch (TypeLoadException cce)
				{
					Fail("ClassNotFoundException: " + cce);
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestFactory()
		{
			IFactory<ICounter<string>> fcs = c.GetFactory();
			ICounter<string> c2 = fcs.Create();
			c2.IncrementCount("fr");
			c2.IncrementCount("de");
			c2.IncrementCount("es", -3);
			ICounter<string> c3 = fcs.Create();
			c3.DecrementCount("es");
			ICounter<string> c4 = fcs.Create();
			c4.IncrementCount("fr");
			c4.SetCount("es", -3);
			c4.SetCount("de", 1.0);
			NUnit.Framework.Assert.AreEqual("Testing factory and counter equality", c2, c4);
			NUnit.Framework.Assert.AreEqual("Testing factory", c2.TotalCount(), -1.0);
			c3.AddAll(c2);
			NUnit.Framework.Assert.AreEqual(c3.KeySet().Count, 3);
			NUnit.Framework.Assert.AreEqual(c3.Size(), 3);
			NUnit.Framework.Assert.AreEqual("Testing addAll", -2.0, c3.TotalCount());
		}

		[NUnit.Framework.Test]
		public virtual void TestReturnValue()
		{
			c.SetDefaultReturnValue(-1);
			NUnit.Framework.Assert.AreEqual(c.DefaultReturnValue(), -1.0);
			NUnit.Framework.Assert.AreEqual(c.GetCount("-!-"), -1.0);
			c.SetDefaultReturnValue(0.0);
			NUnit.Framework.Assert.AreEqual(c.GetCount("-!-"), 0.0);
		}

		[NUnit.Framework.Test]
		public virtual void TestSetCount()
		{
			c.Clear();
			c.SetCount("p", 0);
			c.SetCount("q", 2);
			NUnit.Framework.Assert.AreEqual("Failed setCount", 2.0, c.TotalCount());
			NUnit.Framework.Assert.AreEqual("Failed setCount", 2.0, c.GetCount("q"));
		}

		[NUnit.Framework.Test]
		public virtual void TestIncrement()
		{
			c.Clear();
			NUnit.Framework.Assert.AreEqual(0., c.GetCount("r"));
			NUnit.Framework.Assert.AreEqual(1., c.IncrementCount("r"));
			NUnit.Framework.Assert.AreEqual(1., c.GetCount("r"));
			c.SetCount("p", 0);
			c.SetCount("q", 2);
			NUnit.Framework.Assert.AreEqual(true, c.ContainsKey("q"));
			NUnit.Framework.Assert.AreEqual(false, c.ContainsKey("!!!"));
			NUnit.Framework.Assert.AreEqual(0., c.GetCount("p"));
			NUnit.Framework.Assert.AreEqual(1., c.IncrementCount("p"));
			NUnit.Framework.Assert.AreEqual(1., c.GetCount("p"));
			NUnit.Framework.Assert.AreEqual(4., c.TotalCount());
			c.DecrementCount("s", 5.0);
			NUnit.Framework.Assert.AreEqual(-5.0, c.GetCount("s"));
			c.Remove("s");
			NUnit.Framework.Assert.AreEqual(4.0, c.TotalCount());
		}

		[NUnit.Framework.Test]
		public virtual void TestIncrement2()
		{
			c.Clear();
			c.SetCount("p", .5);
			c.SetCount("q", 2);
			if (integral)
			{
				NUnit.Framework.Assert.AreEqual(3., c.IncrementCount("p", 3.5));
				NUnit.Framework.Assert.AreEqual(3., c.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(5., c.TotalCount());
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(4., c.IncrementCount("p", 3.5));
				NUnit.Framework.Assert.AreEqual(4., c.GetCount("p"));
				NUnit.Framework.Assert.AreEqual(6., c.TotalCount());
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestLogIncrement()
		{
			c.Clear();
			c.SetCount("p", Math.Log(.5));
			// System.out.println(c.getCount("p"));
			c.SetCount("q", Math.Log(.2));
			// System.out.println(c.getCount("q"));
			if (integral)
			{
				// 0.5 gives 0 and 0.3 gives -1, so -1
				double ans = c.LogIncrementCount("p", Math.Log(.3));
				// System.out.println(ans);
				NUnit.Framework.Assert.AreEqual(0., ans, .0001);
				NUnit.Framework.Assert.AreEqual(-1., c.TotalCount(), .0001);
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(Math.Log(.5 + .3), c.LogIncrementCount("p", Math.Log(.3)), .0001);
				NUnit.Framework.Assert.AreEqual(Math.Log(.5 + .3) + Math.Log(.2), c.TotalCount(), .0001);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestEntrySet()
		{
			c.Clear();
			c.SetCount("r", 3.0);
			c.SetCount("p", 1.0);
			c.SetCount("q", 2.0);
			c.SetCount("s", 4.0);
			NUnit.Framework.Assert.AreEqual(10.0, c.TotalCount());
			NUnit.Framework.Assert.AreEqual(1.0, c.GetCount("p"));
			foreach (KeyValuePair<string, double> entry in c.EntrySet())
			{
				if (entry.Key.Equals("p"))
				{
					NUnit.Framework.Assert.AreEqual(1.0, entry.SetValue(3.0));
					NUnit.Framework.Assert.AreEqual(3.0, entry.Value);
				}
			}
			NUnit.Framework.Assert.AreEqual(3.0, c.GetCount("p"));
			NUnit.Framework.Assert.AreEqual(12.0, c.TotalCount());
			ICollection<double> vals = c.Values();
			double tot = 0.0;
			foreach (double d in vals)
			{
				tot += d;
			}
			NUnit.Framework.Assert.AreEqual("Testing values()", 12.0, tot);
		}

		[NUnit.Framework.Test]
		public virtual void TestComparators()
		{
			c.Clear();
			c.SetCount("b", 3.0);
			c.SetCount("p", -5.0);
			c.SetCount("a", 2.0);
			c.SetCount("s", 4.0);
			IList<string> list = new List<string>(c.KeySet());
			IComparator<string> cmp = Counters.ToComparator(c);
			list.Sort(cmp);
			NUnit.Framework.Assert.AreEqual(4, list.Count);
			NUnit.Framework.Assert.AreEqual("p", list[0]);
			NUnit.Framework.Assert.AreEqual("a", list[1]);
			NUnit.Framework.Assert.AreEqual("b", list[2]);
			NUnit.Framework.Assert.AreEqual("s", list[3]);
			IComparator<string> cmp2 = Counters.ToComparatorDescending(c);
			list.Sort(cmp2);
			NUnit.Framework.Assert.AreEqual(4, list.Count);
			NUnit.Framework.Assert.AreEqual("p", list[3]);
			NUnit.Framework.Assert.AreEqual("a", list[2]);
			NUnit.Framework.Assert.AreEqual("b", list[1]);
			NUnit.Framework.Assert.AreEqual("s", list[0]);
			IComparator<string> cmp3 = Counters.ToComparator(c, true, true);
			list.Sort(cmp3);
			NUnit.Framework.Assert.AreEqual(4, list.Count);
			NUnit.Framework.Assert.AreEqual("p", list[3]);
			NUnit.Framework.Assert.AreEqual("a", list[0]);
			NUnit.Framework.Assert.AreEqual("b", list[1]);
			NUnit.Framework.Assert.AreEqual("s", list[2]);
			IComparator<string> cmp4 = Counters.ToComparator(c, false, true);
			list.Sort(cmp4);
			NUnit.Framework.Assert.AreEqual(4, list.Count);
			NUnit.Framework.Assert.AreEqual("p", list[0]);
			NUnit.Framework.Assert.AreEqual("a", list[3]);
			NUnit.Framework.Assert.AreEqual("b", list[2]);
			NUnit.Framework.Assert.AreEqual("s", list[1]);
			IComparator<string> cmp5 = Counters.ToComparator(c, false, false);
			list.Sort(cmp5);
			NUnit.Framework.Assert.AreEqual(4, list.Count);
			NUnit.Framework.Assert.AreEqual("p", list[3]);
			NUnit.Framework.Assert.AreEqual("a", list[2]);
			NUnit.Framework.Assert.AreEqual("b", list[1]);
			NUnit.Framework.Assert.AreEqual("s", list[0]);
		}

		[NUnit.Framework.Test]
		public virtual void TestClear()
		{
			c.IncrementCount("xy", 30);
			c.Clear();
			NUnit.Framework.Assert.AreEqual(0.0, c.TotalCount());
		}
	}
}
