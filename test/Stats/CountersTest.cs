using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;


using NUnit.Framework;


namespace Edu.Stanford.Nlp.Stats
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class CountersTest
	{
		private ICounter<string> c1;

		private ICounter<string> c2;

		private ICounter<string> c8;

		private ICounter<string> c9;

		private const double Tolerance = 0.001;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			Locale.SetDefault(Locale.Us);
			c1 = new ClassicCounter<string>();
			c1.SetCount("p", 1.0);
			c1.SetCount("q", 2.0);
			c1.SetCount("r", 3.0);
			c1.SetCount("s", 4.0);
			c2 = new ClassicCounter<string>();
			c2.SetCount("p", 5.0);
			c2.SetCount("q", 6.0);
			c2.SetCount("r", 7.0);
			c2.SetCount("t", 8.0);
			c8 = new ClassicCounter<string>();
			c8.SetCount("r", 2.0);
			c8.SetCount("z", 4.0);
			c9 = new ClassicCounter<string>();
			c9.SetCount("z", 4.0);
		}

		[NUnit.Framework.Test]
		public virtual void TestUnion()
		{
			ICounter<string> c3 = Counters.Union(c1, c2);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("p"), 6.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("s"), 4.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("t"), 8.0);
			NUnit.Framework.Assert.AreEqual(c3.TotalCount(), 36.0);
		}

		[NUnit.Framework.Test]
		public virtual void TestIntersection()
		{
			ICounter<string> c3 = Counters.Intersection(c1, c2);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("p"), 1.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("q"), 2.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("s"), 0.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("t"), 0.0);
			NUnit.Framework.Assert.AreEqual(c3.TotalCount(), 6.0);
		}

		[NUnit.Framework.Test]
		public virtual void TestProduct()
		{
			ICounter<string> c3 = Counters.Product(c1, c2);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("p"), 5.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("q"), 12.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("r"), 21.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("s"), 0.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("t"), 0.0);
		}

		[NUnit.Framework.Test]
		public virtual void TestDotProduct()
		{
			double d1 = Counters.DotProduct(c1, c2);
			NUnit.Framework.Assert.AreEqual(38.0, d1);
			double d2 = Counters.DotProduct(c1, c1);
			NUnit.Framework.Assert.AreEqual(30.0, d2);
			double d3 = Counters.OptimizedDotProduct(c1, c2);
			NUnit.Framework.Assert.AreEqual(38.0, d3);
			double d4 = Counters.OptimizedDotProduct(c1, c1);
			NUnit.Framework.Assert.AreEqual(30.0, d4);
			NUnit.Framework.Assert.AreEqual(14.0, Counters.OptimizedDotProduct(c2, c8));
			NUnit.Framework.Assert.AreEqual(14.0, Counters.OptimizedDotProduct(c8, c2));
			NUnit.Framework.Assert.AreEqual(0.0, Counters.OptimizedDotProduct(c2, c9));
			NUnit.Framework.Assert.AreEqual(0.0, Counters.OptimizedDotProduct(c9, c2));
		}

		[NUnit.Framework.Test]
		public virtual void TestAbsoluteDifference()
		{
			ICounter<string> c3 = Counters.AbsoluteDifference(c1, c2);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("p"), 4.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("q"), 4.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("r"), 4.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("s"), 4.0);
			NUnit.Framework.Assert.AreEqual(c3.GetCount("t"), 8.0);
			ICounter<string> c4 = Counters.AbsoluteDifference(c2, c1);
			NUnit.Framework.Assert.AreEqual(c4.GetCount("p"), 4.0);
			NUnit.Framework.Assert.AreEqual(c4.GetCount("q"), 4.0);
			NUnit.Framework.Assert.AreEqual(c4.GetCount("r"), 4.0);
			NUnit.Framework.Assert.AreEqual(c4.GetCount("s"), 4.0);
			NUnit.Framework.Assert.AreEqual(c4.GetCount("t"), 8.0);
		}

		[NUnit.Framework.Test]
		public virtual void TestSerialization()
		{
			try
			{
				ByteArrayOutputStream bout = new ByteArrayOutputStream();
				ObjectOutputStream oout = new ObjectOutputStream(bout);
				oout.WriteObject(c1);
				byte[] bleh = bout.ToByteArray();
				ByteArrayInputStream bin = new ByteArrayInputStream(bleh);
				ObjectInputStream oin = new ObjectInputStream(bin);
				ClassicCounter<string> c3 = (ClassicCounter<string>)oin.ReadObject();
				NUnit.Framework.Assert.AreEqual(c3, c1);
			}
			catch (Exception e)
			{
				NUnit.Framework.Assert.Fail(e.Message);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestMin()
		{
			NUnit.Framework.Assert.AreEqual(Counters.Min(c1), 1.0);
			NUnit.Framework.Assert.AreEqual(Counters.Min(c2), 5.0);
		}

		[NUnit.Framework.Test]
		public virtual void TestArgmin()
		{
			NUnit.Framework.Assert.AreEqual(Counters.Argmin(c1), "p");
			NUnit.Framework.Assert.AreEqual(Counters.Argmin(c2), "p");
		}

		[NUnit.Framework.Test]
		public virtual void TestL2Norm()
		{
			ClassicCounter<string> c = new ClassicCounter<string>();
			c.IncrementCount("a", 3);
			c.IncrementCount("b", 4);
			NUnit.Framework.Assert.AreEqual(5.0, Counters.L2Norm(c), Tolerance);
			c.IncrementCount("c", 6);
			c.IncrementCount("d", 4);
			c.IncrementCount("e", 2);
			NUnit.Framework.Assert.AreEqual(9.0, Counters.L2Norm(c), Tolerance);
		}

		[NUnit.Framework.Test]
		public virtual void TestLogNormalize()
		{
			ClassicCounter<string> c = new ClassicCounter<string>();
			c.IncrementCount("a", Math.Log(4.0));
			c.IncrementCount("b", Math.Log(2.0));
			c.IncrementCount("c", Math.Log(1.0));
			c.IncrementCount("d", Math.Log(1.0));
			Counters.LogNormalizeInPlace(c);
			NUnit.Framework.Assert.AreEqual(c.GetCount("a"), -0.693, Tolerance);
			NUnit.Framework.Assert.AreEqual(c.GetCount("b"), -1.386, Tolerance);
			NUnit.Framework.Assert.AreEqual(c.GetCount("c"), -2.079, Tolerance);
			NUnit.Framework.Assert.AreEqual(c.GetCount("d"), -2.079, Tolerance);
			NUnit.Framework.Assert.AreEqual(Counters.LogSum(c), 0.0, Tolerance);
		}

		[NUnit.Framework.Test]
		public virtual void TestL2Normalize()
		{
			ClassicCounter<string> c = new ClassicCounter<string>();
			c.IncrementCount("a", 4.0);
			c.IncrementCount("b", 2.0);
			c.IncrementCount("c", 1.0);
			c.IncrementCount("d", 2.0);
			ICounter<string> d = Counters.L2Normalize(c);
			NUnit.Framework.Assert.AreEqual(d.GetCount("a"), 0.8, Tolerance);
			NUnit.Framework.Assert.AreEqual(d.GetCount("b"), 0.4, Tolerance);
			NUnit.Framework.Assert.AreEqual(d.GetCount("c"), 0.2, Tolerance);
			NUnit.Framework.Assert.AreEqual(d.GetCount("d"), 0.4, Tolerance);
		}

		[NUnit.Framework.Test]
		public virtual void TestRetainAbove()
		{
			c1 = new ClassicCounter<string>();
			c1.IncrementCount("a", 1.1);
			c1.IncrementCount("b", 1.0);
			c1.IncrementCount("c", 0.9);
			c1.IncrementCount("d", 0);
			ICollection<string> removed = Counters.RetainAbove(c1, 1.0);
			ICollection<string> expected = new HashSet<string>();
			expected.Add("c");
			expected.Add("d");
			NUnit.Framework.Assert.AreEqual(expected, removed);
			NUnit.Framework.Assert.AreEqual(1.1, c1.GetCount("a"));
			NUnit.Framework.Assert.AreEqual(1.0, c1.GetCount("b"));
			NUnit.Framework.Assert.IsFalse(c1.ContainsKey("c"));
			NUnit.Framework.Assert.IsFalse(c1.ContainsKey("d"));
		}

		private readonly string[] ascending = new string[] { "e", "d", "a", "b", "c" };

		[NUnit.Framework.Test]
		public virtual void TestToSortedList()
		{
			c1 = new ClassicCounter<string>();
			c1.IncrementCount("a", 0.9);
			c1.IncrementCount("b", 1.0);
			c1.IncrementCount("c", 1.5);
			c1.IncrementCount("d", 0.0);
			c1.IncrementCount("e", -2.0);
			IList<string> ascendList = Counters.ToSortedList(c1, true);
			IList<string> descendList = Counters.ToSortedList(c1);
			for (int i = 0; i < ascending.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual(ascending[i], ascendList[i]);
				NUnit.Framework.Assert.AreEqual(ascending[i], descendList[ascending.Length - i - 1]);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestRetainTop()
		{
			c1 = new ClassicCounter<string>();
			c1.IncrementCount("a", 0.9);
			c1.IncrementCount("b", 1.0);
			c1.IncrementCount("c", 1.5);
			c1.IncrementCount("d", 0.0);
			c1.IncrementCount("e", -2.0);
			Counters.RetainTop(c1, 3);
			NUnit.Framework.Assert.AreEqual(3, c1.Size());
			NUnit.Framework.Assert.IsTrue(c1.ContainsKey("a"));
			NUnit.Framework.Assert.IsFalse(c1.ContainsKey("d"));
			Counters.RetainTop(c1, 1);
			NUnit.Framework.Assert.AreEqual(1, c1.Size());
			NUnit.Framework.Assert.IsTrue(c1.ContainsKey("c"));
			NUnit.Framework.Assert.AreEqual(1.5, c1.GetCount("c"));
		}

		[NUnit.Framework.Test]
		public virtual void TestPointwiseMutualInformation()
		{
			ICounter<string> x = new ClassicCounter<string>();
			x.IncrementCount("0", 0.8);
			x.IncrementCount("1", 0.2);
			ICounter<int> y = new ClassicCounter<int>();
			y.IncrementCount(0, 0.25);
			y.IncrementCount(1, 0.75);
			ICounter<Pair<string, int>> joint;
			joint = new ClassicCounter<Pair<string, int>>();
			joint.IncrementCount(new Pair<string, int>("0", 0), 0.1);
			joint.IncrementCount(new Pair<string, int>("0", 1), 0.7);
			joint.IncrementCount(new Pair<string, int>("1", 0), 0.15);
			joint.IncrementCount(new Pair<string, int>("1", 1), 0.05);
			// Check that correct PMI values are calculated, using tables from
			// http://en.wikipedia.org/wiki/Pointwise_mutual_information
			double pmi;
			Pair<string, int> pair;
			pair = new Pair<string, int>("0", 0);
			pmi = Counters.PointwiseMutualInformation(x, y, joint, pair);
			NUnit.Framework.Assert.AreEqual(-1, pmi, 10e-5);
			pair = new Pair<string, int>("0", 1);
			pmi = Counters.PointwiseMutualInformation(x, y, joint, pair);
			NUnit.Framework.Assert.AreEqual(0.222392421, pmi, 10e-5);
			pair = new Pair<string, int>("1", 0);
			pmi = Counters.PointwiseMutualInformation(x, y, joint, pair);
			NUnit.Framework.Assert.AreEqual(1.584962501, pmi, 10e-5);
			pair = new Pair<string, int>("1", 1);
			pmi = Counters.PointwiseMutualInformation(x, y, joint, pair);
			NUnit.Framework.Assert.AreEqual(-1.584962501, pmi, 10e-5);
		}

		[NUnit.Framework.Test]
		public virtual void TestToSortedString()
		{
			ICounter<string> c = new ClassicCounter<string>();
			c.SetCount("b", 0.25);
			c.SetCount("a", 0.5);
			c.SetCount("c", 1.0);
			// check full argument version
			string result = Counters.ToSortedString(c, 5, "%s%.1f", ":", "{%s}");
			NUnit.Framework.Assert.AreEqual("{c1.0:a0.5:b0.3}", result);
			// check version with no wrapper
			result = Counters.ToSortedString(c, 2, "%2$f %1$s", "\n");
			NUnit.Framework.Assert.AreEqual("1.000000 c\n0.500000 a", result);
			// check some equivalences to other Counters methods
			int k = 2;
			result = Counters.ToSortedString(c, k, "%s=%s", ", ", "[%s]");
			NUnit.Framework.Assert.AreEqual(Counters.ToString(c, k), result);
			NUnit.Framework.Assert.AreEqual(Counters.ToBiggestValuesFirstString(c, k), result);
			result = Counters.ToSortedString(c, k, "%2$g\t%1$s", "\n", "%s\n");
			NUnit.Framework.Assert.AreEqual(Counters.ToVerticalString(c, k), result);
			// test sorting by keys
			result = Counters.ToSortedByKeysString(c, "%s=>%.2f", "; ", "<%s>");
			NUnit.Framework.Assert.AreEqual("<a=>0.50; b=>0.25; c=>1.00>", result);
		}

		[NUnit.Framework.Test]
		public virtual void TestHIndex()
		{
			// empty counter
			ICounter<string> c = new ClassicCounter<string>();
			NUnit.Framework.Assert.AreEqual(0, Counters.HIndex(c));
			// two items with 2 or more citations
			c.SetCount("X", 3);
			c.SetCount("Y", 2);
			c.SetCount("Z", 1);
			NUnit.Framework.Assert.AreEqual(2, Counters.HIndex(c));
			// 14 items with 14 or more citations
			for (int i = 0; i < 14; ++i)
			{
				c.SetCount(i.ToString(), 15);
			}
			NUnit.Framework.Assert.AreEqual(14, Counters.HIndex(c));
			// 15 items with 15 or more citations
			c.SetCount("15", 15);
			NUnit.Framework.Assert.AreEqual(15, Counters.HIndex(c));
		}

		[NUnit.Framework.Test]
		public virtual void TestAddInPlaceCollection()
		{
			// initialize counter
			SetUp();
			IList<string> collection = new List<string>();
			collection.Add("p");
			collection.Add("p");
			collection.Add("s");
			Counters.AddInPlace(c1, collection);
			NUnit.Framework.Assert.AreEqual(3.0, c1.GetCount("p"));
			NUnit.Framework.Assert.AreEqual(5.0, c1.GetCount("s"));
		}

		[NUnit.Framework.Test]
		public virtual void TestRemoveKeys()
		{
			SetUp();
			ICollection<string> c = new List<string>();
			c.Add("p");
			c.Add("r");
			c.Add("s");
			Counters.RemoveKeys(c1, c);
			NUnit.Framework.Assert.AreEqual(c1.KeySet().Count, 1);
			object[] keys = Sharpen.Collections.ToArray(c1.KeySet());
			NUnit.Framework.Assert.AreEqual(keys[0], "q");
		}

		[NUnit.Framework.Test]
		public virtual void TestRetainTopMass()
		{
			SetUp();
			System.Console.Out.WriteLine(Counters.ToString(c1, c1.Size()));
			Counters.RetainTopMass(c1, 3);
			NUnit.Framework.Assert.AreEqual(Sharpen.Collections.ToArray(c1.KeySet())[0], "s");
			NUnit.Framework.Assert.AreEqual(c1.Size(), 1);
		}

		[NUnit.Framework.Test]
		public virtual void TestDivideInPlace()
		{
			TwoDimensionalCounter<string, string> a = new TwoDimensionalCounter<string, string>();
			a.SetCount("a", "b", 1);
			a.SetCount("a", "c", 1);
			a.SetCount("c", "a", 1);
			a.SetCount("c", "b", 1);
			Counters.DivideInPlace(a, a.TotalCount());
			NUnit.Framework.Assert.AreEqual(1.0, a.TotalCount());
			NUnit.Framework.Assert.AreEqual(0.25, a.GetCount("a", "b"));
		}

		[NUnit.Framework.Test]
		public virtual void TestPearsonsCorrelationCoefficient()
		{
			SetUp();
			Counters.PearsonsCorrelationCoefficient(c1, c2);
		}

		[NUnit.Framework.Test]
		public virtual void TestToTiedRankCounter()
		{
			SetUp();
			c1.SetCount("t", 1.0);
			c1.SetCount("u", 1.0);
			c1.SetCount("v", 2.0);
			c1.SetCount("z", 4.0);
			ICounter<string> rank = Counters.ToTiedRankCounter(c1);
			NUnit.Framework.Assert.AreEqual(1.5, rank.GetCount("z"));
			NUnit.Framework.Assert.AreEqual(7.0, rank.GetCount("t"));
		}

		[NUnit.Framework.Test]
		public virtual void TestTransformWithValuesAdd()
		{
			SetUp();
			c1.SetCount("P", 2.0);
			System.Console.Out.WriteLine(c1);
			c1 = Counters.TransformWithValuesAdd(c1, null);
			System.Console.Out.WriteLine(c1);
		}

		[NUnit.Framework.Test]
		public virtual void TestEquals()
		{
			SetUp();
			c1.Clear();
			c2.Clear();
			c1.SetCount("p", 1.0);
			c1.SetCount("q", 2.0);
			c1.SetCount("r", 3.0);
			c1.SetCount("s", 4.0);
			c2.SetCount("p", 1.0);
			c2.SetCount("q", 2.0);
			c2.SetCount("r", 3.0);
			c2.SetCount("s", 4.0);
			NUnit.Framework.Assert.IsTrue(Counters.Equals(c1, c2));
			c2.SetCount("s", 4.1);
			NUnit.Framework.Assert.IsFalse(Counters.Equals(c1, c2));
			c2.Remove("s");
			NUnit.Framework.Assert.IsFalse(Counters.Equals(c1, c2));
			c2.SetCount("s", 4.0 + 1e-10);
			NUnit.Framework.Assert.IsFalse(Counters.Equals(c1, c2));
			NUnit.Framework.Assert.IsTrue(Counters.Equals(c1, c2, 1e-5));
			c2.SetCount("2", 3.0 + 8e-5);
			c2.SetCount("s", 4.0 + 8e-5);
			NUnit.Framework.Assert.IsFalse(Counters.Equals(c1, c2, 1e-5));
		}

		// fails totalCount() equality check
		[NUnit.Framework.Test]
		public virtual void TestJensenShannonDivergence()
		{
			// borrow from ArrayMathTest
			ICounter<string> a = new ClassicCounter<string>();
			a.SetCount("a", 1.0);
			a.SetCount("b", 1.0);
			a.SetCount("c", 7.0);
			a.SetCount("d", 1.0);
			ICounter<string> b = new ClassicCounter<string>();
			b.SetCount("b", 1.0);
			b.SetCount("c", 1.0);
			b.SetCount("d", 7.0);
			b.SetCount("e", 1.0);
			b.SetCount("f", 0.0);
			NUnit.Framework.Assert.AreEqual(0.46514844544032313, Counters.JensenShannonDivergence(a, b), 1e-5);
			ICounter<string> c = new ClassicCounter<string>(Java.Util.Collections.SingletonList("A"));
			ICounter<string> d = new ClassicCounter<string>(Arrays.AsList("B", "C"));
			NUnit.Framework.Assert.AreEqual(1.0, Counters.JensenShannonDivergence(c, d), 1e-5);
		}

		[NUnit.Framework.Test]
		public virtual void TestFlatten()
		{
			IDictionary<string, ICounter<string>> h = new Dictionary<string, ICounter<string>>();
			ICounter<string> a = new ClassicCounter<string>();
			a.SetCount("a", 1.0);
			a.SetCount("b", 1.0);
			a.SetCount("c", 7.0);
			a.SetCount("d", 1.0);
			ICounter<string> b = new ClassicCounter<string>();
			b.SetCount("b", 1.0);
			b.SetCount("c", 1.0);
			b.SetCount("d", 7.0);
			b.SetCount("e", 1.0);
			b.SetCount("f", 1.0);
			h["first"] = a;
			h["second"] = b;
			ICounter<string> flat = Counters.Flatten(h);
			NUnit.Framework.Assert.AreEqual(6, flat.Size());
			NUnit.Framework.Assert.AreEqual(2.0, flat.GetCount("b"));
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestSerializeStringCounter()
		{
			ICounter<string> counts = new ClassicCounter<string>();
			for (int @base = -10; @base < 10; ++@base)
			{
				if (@base == 0)
				{
					continue;
				}
				for (int exponent = -100; exponent < 100; ++exponent)
				{
					double number = Math.Pow(Math.Pi * @base, exponent);
					counts.SetCount(double.ToString(number), number);
				}
			}
			File tmp = File.CreateTempFile("counts", ".tab.gz");
			tmp.DeleteOnExit();
			Counters.SerializeStringCounter(counts, tmp.GetPath());
			ICounter<string> reread = Counters.DeserializeStringCounter(tmp.GetPath());
			foreach (KeyValuePair<string, double> entry in reread.EntrySet())
			{
				double old = counts.GetCount(entry.Key);
				NUnit.Framework.Assert.AreEqual(old, entry.Value, Math.Abs(old) / 1e5);
			}
		}
	}
}
