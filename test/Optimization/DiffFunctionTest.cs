using System;
using Edu.Stanford.Nlp.Math;



namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// This class both tests a particular DiffFunction and provides a basis
	/// for testing whether any DiffFunction's derivative is correct.
	/// </summary>
	/// <author>Galen Andrew</author>
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class DiffFunctionTest
	{
		private static readonly Random r = new Random();

		// private static final double EPS = 1e-6;
		private static double[] EstimateGradient(Func f, double[] x, int[] testIndices, double eps)
		{
			double[] lowAnswer = new double[testIndices.Length];
			double[] answer = new double[testIndices.Length];
			for (int i = 0; i < testIndices.Length; i++)
			{
				double orig = x[testIndices[i]];
				x[testIndices[i]] -= eps;
				lowAnswer[i] = f.ValueAt(x);
				x[testIndices[i]] = orig + eps;
				answer[i] = f.ValueAt(x);
				x[testIndices[i]] = orig;
				// restore value
				//System.err.println("new x is "+x[testIndices[i]]);
				answer[i] = (answer[i] - lowAnswer[i]) / (2.0 * eps);
			}
			// System.err.print(".");
			//System.err.print(" "+answer[i]);
			// System.err.println("Gradient estimate is: " + Arrays.toString(answer));
			return answer;
		}

		public static void GradientCheck(IDiffFunction f)
		{
			for (int deg = -2; deg > -7; deg--)
			{
				double eps = Math.Pow(10, deg);
				System.Console.Error.WriteLine("testing for eps " + eps);
				GradientCheck(f, eps);
			}
		}

		public static void GradientCheck(IDiffFunction f, double eps)
		{
			double[] x = new double[f.DomainDimension()];
			for (int i = 0; i < x.Length; i++)
			{
				x[i] = Math.Random() - 0.5;
			}
			// 0.03; (i - 0.5) * 4;
			GradientCheck(f, x, eps);
		}

		public static void GradientCheck(IDiffFunction f, double[] x, double eps)
		{
			// just check a few dimensions
			int numDim = Math.Min(10, x.Length);
			int[] ind = new int[numDim];
			if (numDim == x.Length)
			{
				for (int i = 0; i < ind.Length; i++)
				{
					ind[i] = i;
				}
			}
			else
			{
				ind[0] = 0;
				ind[1] = x.Length - 1;
				for (int i = 2; i < ind.Length; i++)
				{
					ind[i] = r.NextInt(x.Length - 2) + 1;
				}
			}
			// ind[i] = i;
			GradientCheck(f, x, ind, eps);
		}

		public static void GradientCheck(IDiffFunction f, double[] x, int[] ind, double eps)
		{
			// System.err.print("Testing grad <");
			double[] testGrad = EstimateGradient(f, x, ind, eps);
			// System.err.println(">");
			double[] fullGrad = f.DerivativeAt(x);
			double[] fGrad = new double[ind.Length];
			for (int i = 0; i < ind.Length; i++)
			{
				fGrad[i] = fullGrad[ind[i]];
			}
			double[] diff = ArrayMath.PairwiseSubtract(testGrad, fGrad);
			System.Console.Error.WriteLine("1-norm:" + ArrayMath.Norm_1(diff));
			NUnit.Framework.Assert.AreEqual(0.0, ArrayMath.Norm_1(diff), 2 * eps);
			System.Console.Error.WriteLine("2-norm:" + ArrayMath.Norm(diff));
			NUnit.Framework.Assert.AreEqual(0.0, ArrayMath.Norm(diff), 2 * eps);
			System.Console.Error.WriteLine("inf-norm:" + ArrayMath.Norm_inf(diff));
			NUnit.Framework.Assert.AreEqual(0.0, ArrayMath.Norm_inf(diff), 2 * eps);
			System.Console.Error.WriteLine("pearson:" + ArrayMath.PearsonCorrelation(testGrad, fGrad));
			NUnit.Framework.Assert.AreEqual(1.0, ArrayMath.PearsonCorrelation(testGrad, fGrad), 2 * eps);
		}

		// This could exception if all numbers were the same and so there is no standard deviation.
		// ArrayMath.standardize(fGrad);
		// ArrayMath.standardize(testGrad);
		// System.err.printf("test: %s%n", Arrays.toString(testGrad));
		// System.err.printf("full: %s%n",Arrays.toString(fGrad));
		[NUnit.Framework.Test]
		public virtual void TestXSquaredPlusOne()
		{
			GradientCheck(new _IDiffFunction_103());
		}

		private sealed class _IDiffFunction_103 : IDiffFunction
		{
			public _IDiffFunction_103()
			{
			}

			// this function does on a large vector x^2+1
			public double[] DerivativeAt(double[] x)
			{
				return ArrayMath.Add(ArrayMath.Multiply(x, 2), 1);
			}

			public double ValueAt(double[] x)
			{
				return ArrayMath.InnerProduct(x, ArrayMath.Add(x, 1));
			}

			public int DomainDimension()
			{
				return 10000;
			}
		}
	}
}
