using System;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Math
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class SloppyMathTest
	{
		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
		}

		[NUnit.Framework.Test]
		public virtual void TestRound1()
		{
			NUnit.Framework.Assert.AreEqual(0.0, SloppyMath.Round(0.499));
			NUnit.Framework.Assert.AreEqual(0.0, SloppyMath.Round(-0.5));
			NUnit.Framework.Assert.AreEqual(10.0, SloppyMath.Round(10));
			NUnit.Framework.Assert.AreEqual(10.0, SloppyMath.Round(10.32));
		}

		[NUnit.Framework.Test]
		public virtual void TestRound2()
		{
			NUnit.Framework.Assert.AreEqual(3.14, SloppyMath.Round(System.Math.Pi, 2));
			NUnit.Framework.Assert.AreEqual(400.0, SloppyMath.Round(431.5, -2));
			NUnit.Framework.Assert.AreEqual(432.0, SloppyMath.Round(431.5, 0));
			NUnit.Framework.Assert.AreEqual(0.0, SloppyMath.Round(-0.05, 1));
			NUnit.Framework.Assert.AreEqual(-0.05, SloppyMath.Round(-0.05, 2));
		}

		[NUnit.Framework.Test]
		public virtual void TestMax()
		{
			NUnit.Framework.Assert.AreEqual(3, SloppyMath.Max(1, 2, 3));
		}

		[NUnit.Framework.Test]
		public virtual void TestMin()
		{
			NUnit.Framework.Assert.AreEqual(1, SloppyMath.Min(1, 2, 3));
		}

		[NUnit.Framework.Test]
		public virtual void TestIsDangerous()
		{
			NUnit.Framework.Assert.IsTrue(SloppyMath.IsDangerous(double.PositiveInfinity) && SloppyMath.IsDangerous(double.NaN) && SloppyMath.IsDangerous(0));
		}

		[NUnit.Framework.Test]
		public virtual void TestIsVeryDangerous()
		{
			NUnit.Framework.Assert.IsTrue(SloppyMath.IsDangerous(double.PositiveInfinity) && SloppyMath.IsDangerous(double.NaN));
		}

		[NUnit.Framework.Test]
		public virtual void TestLogAdd()
		{
			double d1 = 0.1;
			double d2 = 0.2;
			double lsum = SloppyMath.LogAdd(d1, d2);
			double myLsum = 0;
			myLsum += System.Math.Exp(d1);
			myLsum += System.Math.Exp(d2);
			myLsum = System.Math.Log(myLsum);
			NUnit.Framework.Assert.IsTrue(myLsum == lsum);
		}

		[NUnit.Framework.Test]
		public virtual void TestIntPow()
		{
			NUnit.Framework.Assert.IsTrue(SloppyMath.IntPow(3, 5) == System.Math.Pow(3, 5));
			NUnit.Framework.Assert.IsTrue(SloppyMath.IntPow(3.3, 5) - System.Math.Pow(3.3, 5) < 1e-4);
			NUnit.Framework.Assert.AreEqual(1, SloppyMath.IntPow(5, 0));
			NUnit.Framework.Assert.AreEqual(3125, SloppyMath.IntPow(5, 5));
			NUnit.Framework.Assert.AreEqual(32, SloppyMath.IntPow(2, 5));
			NUnit.Framework.Assert.AreEqual(3, SloppyMath.IntPow(3, 1));
			NUnit.Framework.Assert.AreEqual(1158.56201, SloppyMath.IntPow(4.1, 5), 1e-4);
			NUnit.Framework.Assert.AreEqual(1158.56201f, SloppyMath.IntPow(4.1f, 5), 1e-2);
		}

		[NUnit.Framework.Test]
		public virtual void TestArccos()
		{
			NUnit.Framework.Assert.AreEqual(System.Math.Pi, SloppyMath.Acos(-1.0), 0.001);
			NUnit.Framework.Assert.AreEqual(0, SloppyMath.Acos(1.0), 0.001);
			NUnit.Framework.Assert.AreEqual(System.Math.Pi / 2, SloppyMath.Acos(0.0), 0.001);
			for (double x = -1.0; x < 1.0; x += 0.001)
			{
				NUnit.Framework.Assert.AreEqual(System.Math.Acos(x), SloppyMath.Acos(x), 0.001);
			}
			try
			{
				SloppyMath.Acos(-1.0000001);
				NUnit.Framework.Assert.IsFalse(true);
			}
			catch (ArgumentException)
			{
			}
			try
			{
				SloppyMath.Acos(1.0000001);
				NUnit.Framework.Assert.IsFalse(true);
			}
			catch (ArgumentException)
			{
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestPythonMod()
		{
			NUnit.Framework.Assert.AreEqual(0, SloppyMath.PythonMod(9, 3));
			NUnit.Framework.Assert.AreEqual(0, SloppyMath.PythonMod(-9, 3));
			NUnit.Framework.Assert.AreEqual(0, SloppyMath.PythonMod(9, -3));
			NUnit.Framework.Assert.AreEqual(0, SloppyMath.PythonMod(-9, -3));
			NUnit.Framework.Assert.AreEqual(2, SloppyMath.PythonMod(8, 3));
			NUnit.Framework.Assert.AreEqual(1, SloppyMath.PythonMod(-8, 3));
			NUnit.Framework.Assert.AreEqual(-1, SloppyMath.PythonMod(8, -3));
			NUnit.Framework.Assert.AreEqual(-2, SloppyMath.PythonMod(-8, -3));
		}

		[NUnit.Framework.Test]
		public virtual void TestParseDouble()
		{
			for (int @base = -10; @base < 10; ++@base)
			{
				if (@base == 0)
				{
					continue;
				}
				for (int exponent = -100; exponent < 100; ++exponent)
				{
					double number = System.Math.Pow(System.Math.Pi * @base, exponent);
					Triple<bool, long, int> parts = SloppyMath.SegmentDouble(number);
					double parsed = SloppyMath.ParseDouble(parts.first, parts.second, parts.third);
					NUnit.Framework.Assert.AreEqual(number, parsed, System.Math.Abs(parsed) / 1.0e5);
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestParseInt()
		{
			NUnit.Framework.Assert.AreEqual(42, SloppyMath.ParseInt("42"));
			NUnit.Framework.Assert.AreEqual(-42, SloppyMath.ParseInt("-42"));
			NUnit.Framework.Assert.AreEqual(42000000000000l, SloppyMath.ParseInt("42000000000000"));
		}
	}
}
