using NUnit.Framework;


namespace Edu.Stanford.Nlp.Math
{
	[NUnit.Framework.TestFixture]
	public class ArrayMathTest
	{
		private double[] d1 = new double[3];

		private double[] d2 = new double[3];

		private double[] d3 = new double[3];

		private double[] d4 = new double[3];

		private double[] d5 = new double[4];

		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			d1[0] = 1.0;
			d1[1] = 343.33;
			d1[2] = -13.1;
			d2[0] = 1.0;
			d2[1] = 343.33;
			d2[2] = -13.1;
			d3[0] = double.NaN;
			d3[1] = double.PositiveInfinity;
			d3[2] = 2;
			d4[0] = 0.1;
			d4[1] = 0.2;
			d4[2] = 0.3;
			d5[0] = 0.1;
			d5[1] = 0.2;
			d5[2] = 0.3;
			d5[3] = 0.8;
		}

		[Test]
		public virtual void TestInnerProduct()
		{
			double inner = ArrayMath.InnerProduct(d4, d4);
			NUnit.Framework.Assert.AreEqual("Wrong inner product", 0.14, inner, 1e-6);
			inner = ArrayMath.InnerProduct(d5, d5);
			NUnit.Framework.Assert.AreEqual("Wrong inner product", 0.78, inner, 1e-6);
		}

		[Test]
		public virtual void TestNumRows()
		{
			int nRows = ArrayMath.NumRows(d1);
			NUnit.Framework.Assert.AreEqual(nRows, 3);
		}

		[Test]
		public virtual void TestExpLog()
		{
			double[] d1prime = ArrayMath.Log(ArrayMath.Exp(d1));
			double[] diff = ArrayMath.PairwiseSubtract(d1, d1prime);
			double norm2 = ArrayMath.Norm(diff);
			NUnit.Framework.Assert.AreEqual(norm2, 1e-5, 0.0);
		}

		[Test]
		public virtual void TestExpLogInplace()
		{
			ArrayMath.ExpInPlace(d1);
			ArrayMath.LogInPlace(d1);
			ArrayMath.PairwiseSubtractInPlace(d1, d2);
			double norm2 = ArrayMath.Norm(d1);
			NUnit.Framework.Assert.AreEqual(norm2, 1e-5, 0.0);
		}

		[Test]
		public virtual void TestAddInPlace()
		{
			ArrayMath.AddInPlace(d1, 3);
			for (int i = 0; i < ArrayMath.NumRows(d1); i++)
			{
				NUnit.Framework.Assert.AreEqual(d2[i] + 3, 1e-5, d1[i]);
			}
		}

		[Test]
		public virtual void TestMultiplyInPlace()
		{
			ArrayMath.MultiplyInPlace(d1, 3);
			for (int i = 0; i < ArrayMath.NumRows(d1); i++)
			{
				NUnit.Framework.Assert.AreEqual(d2[i] * 3, 1e-5, d1[i]);
			}
		}

		[Test]
		public virtual void TestPowInPlace()
		{
			ArrayMath.PowInPlace(d1, 3);
			for (int i = 0; i < ArrayMath.NumRows(d1); i++)
			{
				NUnit.Framework.Assert.AreEqual(System.Math.Pow(d2[i], 3), 1e-5, d1[i]);
			}
		}

		[Test]
		public virtual void TestAdd()
		{
			double[] d1prime = ArrayMath.Add(d1, 3);
			for (int i = 0; i < ArrayMath.NumRows(d1prime); i++)
			{
				NUnit.Framework.Assert.AreEqual(d1[i] + 3, 1e-5, d1prime[i]);
			}
		}

		[Test]
		public virtual void TestMultiply()
		{
			double[] d1prime = ArrayMath.Multiply(d1, 3);
			for (int i = 0; i < ArrayMath.NumRows(d1prime); i++)
			{
				NUnit.Framework.Assert.AreEqual(d1[i] * 3, 1e-5, d1prime[i]);
			}
		}

		[Test]
		public virtual void TestPow()
		{
			double[] d1prime = ArrayMath.Pow(d1, 3);
			for (int i = 0; i < ArrayMath.NumRows(d1prime); i++)
			{
				NUnit.Framework.Assert.AreEqual(System.Math.Pow(d1[i], 3), 1e-5, d1prime[i]);
			}
		}

		[Test]
		public virtual void TestPairwiseAdd()
		{
			double[] sum = ArrayMath.PairwiseAdd(d1, d2);
			for (int i = 0; i < ArrayMath.NumRows(d1); i++)
			{
				NUnit.Framework.Assert.AreEqual(d1[i] + d2[i], 1e-5, sum[i]);
			}
		}

		[Test]
		public virtual void TestPairwiseSubtract()
		{
			double[] diff = ArrayMath.PairwiseSubtract(d1, d2);
			for (int i = 0; i < ArrayMath.NumRows(d1); i++)
			{
				NUnit.Framework.Assert.AreEqual(d1[i] - d2[i], 1e-5, diff[i]);
			}
		}

		[Test]
		public virtual void TestPairwiseMultiply()
		{
			double[] product = ArrayMath.PairwiseMultiply(d1, d2);
			for (int i = 0; i < ArrayMath.NumRows(d1); i++)
			{
				NUnit.Framework.Assert.AreEqual(d1[i] * d2[i], 1e-5, product[i]);
			}
		}

		[Test]
		public virtual void TestHasNaN()
		{
			NUnit.Framework.Assert.IsFalse(ArrayMath.HasNaN(d1));
			NUnit.Framework.Assert.IsFalse(ArrayMath.HasNaN(d2));
			NUnit.Framework.Assert.IsTrue(ArrayMath.HasNaN(d3));
		}

		[Test]
		public virtual void TestHasInfinite()
		{
			NUnit.Framework.Assert.IsFalse(ArrayMath.HasInfinite(d1));
			NUnit.Framework.Assert.IsFalse(ArrayMath.HasInfinite(d2));
			NUnit.Framework.Assert.IsTrue(ArrayMath.HasInfinite(d3));
		}

		[Test]
		public virtual void TestCountNaN()
		{
			NUnit.Framework.Assert.AreEqual(ArrayMath.CountNaN(d1), 0);
			NUnit.Framework.Assert.AreEqual(ArrayMath.CountNaN(d2), 0);
			NUnit.Framework.Assert.AreEqual(ArrayMath.CountNaN(d3), 1);
		}

		[Test]
		public virtual void TestFliterNaN()
		{
			double[] f_d3 = ArrayMath.FilterNaN(d3);
			NUnit.Framework.Assert.AreEqual(ArrayMath.NumRows(f_d3), 2);
			NUnit.Framework.Assert.AreEqual(ArrayMath.CountNaN(f_d3), 0);
		}

		[Test]
		public virtual void TestCountInfinite()
		{
			NUnit.Framework.Assert.AreEqual(ArrayMath.CountInfinite(d1), 0);
			NUnit.Framework.Assert.AreEqual(ArrayMath.CountInfinite(d2), 0);
			NUnit.Framework.Assert.AreEqual(ArrayMath.CountInfinite(d3), 1);
		}

		[Test]
		public virtual void TestFliterInfinite()
		{
			double[] f_d3 = ArrayMath.FilterInfinite(d3);
			NUnit.Framework.Assert.AreEqual(ArrayMath.NumRows(f_d3), 2);
			NUnit.Framework.Assert.AreEqual(ArrayMath.CountInfinite(f_d3), 0);
		}

		[Test]
		public virtual void TestFliterNaNAndInfinite()
		{
			double[] f_d3 = ArrayMath.FilterNaNAndInfinite(d3);
			NUnit.Framework.Assert.AreEqual(ArrayMath.NumRows(f_d3), 1);
			NUnit.Framework.Assert.AreEqual(ArrayMath.CountInfinite(f_d3), 0);
			NUnit.Framework.Assert.AreEqual(ArrayMath.CountNaN(f_d3), 0);
		}

		[Test]
		public virtual void TestSum()
		{
			double sum = ArrayMath.Sum(d1);
			double mySum = 0.0;
			foreach (double d in d1)
			{
				mySum += d;
			}
			NUnit.Framework.Assert.AreEqual(mySum, 1e-6, sum);
		}

		[Test]
		public virtual void TestNorm_inf()
		{
			double ninf = ArrayMath.Norm_inf(d1);
			double max = ArrayMath.Max(d1);
			NUnit.Framework.Assert.AreEqual(max, 1e-6, ninf);
			ninf = ArrayMath.Norm_inf(d2);
			max = ArrayMath.Max(d2);
			NUnit.Framework.Assert.AreEqual(max, 1e-6, ninf);
			ninf = ArrayMath.Norm_inf(d3);
			max = ArrayMath.Max(d3);
			NUnit.Framework.Assert.AreEqual(max, 1e-6, ninf);
		}

		[Test]
		public virtual void TestArgmax()
		{
			NUnit.Framework.Assert.AreEqual(d1[ArrayMath.Argmax(d1)], 1e-5, ArrayMath.Max(d1));
			NUnit.Framework.Assert.AreEqual(d2[ArrayMath.Argmax(d2)], 1e-5, ArrayMath.Max(d2));
			NUnit.Framework.Assert.AreEqual(d3[ArrayMath.Argmax(d3)], 1e-5, ArrayMath.Max(d3));
		}

		[Test]
		public virtual void TestArgmin()
		{
			NUnit.Framework.Assert.AreEqual(d1[ArrayMath.Argmin(d1)], 1e-5, ArrayMath.Min(d1));
			NUnit.Framework.Assert.AreEqual(d2[ArrayMath.Argmin(d2)], 1e-5, ArrayMath.Min(d2));
			NUnit.Framework.Assert.AreEqual(d3[ArrayMath.Argmin(d3)], 1e-5, ArrayMath.Min(d3));
		}

		[Test]
		public virtual void TestLogSum()
		{
			double lsum = ArrayMath.LogSum(d4);
			double myLsum = 0;
			foreach (double d in d4)
			{
				myLsum += System.Math.Exp(d);
			}
			myLsum = System.Math.Log(myLsum);
			NUnit.Framework.Assert.AreEqual(lsum, 1e-5, myLsum);
		}

		[Test]
		public virtual void TestNormalize()
		{
			double tol = 1e-5;
			ArrayMath.Normalize(d1);
			ArrayMath.Normalize(d2);
			//ArrayMath.normalize(d3);
			ArrayMath.Normalize(d4);
			NUnit.Framework.Assert.AreEqual(ArrayMath.Sum(d1), tol, 1.0);
			NUnit.Framework.Assert.AreEqual(ArrayMath.Sum(d2), tol, 1.0);
			// assertEquals(1.0, ArrayMath.sum(d3), tol);
			NUnit.Framework.Assert.AreEqual(ArrayMath.Sum(d4), tol, 1.0);
		}

		[Test]
		public virtual void TestKLDivergence()
		{
			double kld = ArrayMath.KlDivergence(d1, d2);
			NUnit.Framework.Assert.AreEqual(kld, 1e-5, 0.0);
		}

		[Test]
		public virtual void TestSumAndMean()
		{
			NUnit.Framework.Assert.AreEqual(ArrayMath.Mean(d1) * d1.Length, 1e-5, ArrayMath.Sum(d1));
			NUnit.Framework.Assert.AreEqual(ArrayMath.Mean(d2) * d2.Length, 1e-5, ArrayMath.Sum(d2));
			NUnit.Framework.Assert.AreEqual(ArrayMath.Mean(d3) * d3.Length, 1e-5, ArrayMath.Sum(d3));
			// comes out as NaN but works!
			NUnit.Framework.Assert.AreEqual(ArrayMath.Mean(d4) * d4.Length, 1e-5, ArrayMath.Sum(d4));
		}

		private static void HelpTestSafeSumAndMean(double[] d)
		{
			double[] dprime = ArrayMath.FilterNaNAndInfinite(d);
			NUnit.Framework.Assert.AreEqual(ArrayMath.Sum(dprime), 1e-5, ArrayMath.SafeMean(d) * ArrayMath.NumRows(dprime));
		}

		[Test]
		public virtual void TestSafeSumAndMean()
		{
			HelpTestSafeSumAndMean(d1);
			HelpTestSafeSumAndMean(d2);
			HelpTestSafeSumAndMean(d3);
			HelpTestSafeSumAndMean(d4);
		}

		[Test]
		public virtual void TestJensenShannon()
		{
			double[] a = new double[] { 0.1, 0.1, 0.7, 0.1, 0.0, 0.0 };
			double[] b = new double[] { 0.0, 0.1, 0.1, 0.7, 0.1, 0.0 };
			NUnit.Framework.Assert.AreEqual(ArrayMath.JensenShannonDivergence(a, b), 1e-5, 0.46514844544032313);
			double[] c = new double[] { 1.0, 0.0, 0.0 };
			double[] d = new double[] { 0.0, 0.5, 0.5 };
			NUnit.Framework.Assert.AreEqual(ArrayMath.JensenShannonDivergence(c, d), 1e-5, 1.0);
		}

		[Test]
		public virtual void Test2dAdd()
		{
			double[][] d6 = new double[][] { new double[] { 0.26, 0.87, -1.26 }, new double[] { 0.17, 3.21, -1.8 } };
			double[][] d7 = new double[][] { new double[] { 0.26, 0.07, -1.26 }, new double[] { 0.17, -3.21, -1.8 } };
			double[][] d8 = new double[][] { new double[] { 0.52, 0.94, -2.52 }, new double[] { 0.34, 0.0, -3.6 } };
			ArrayMath.AddInPlace(d6, d7);
			for (int i = 0; i < d8.Length; i++)
			{
				for (int j = 0; j < d8[i].Length; j++)
				{
					NUnit.Framework.Assert.AreEqual(d8[i][j], 1e-5, d6[i][j]);
				}
			}
		}
	}
}
