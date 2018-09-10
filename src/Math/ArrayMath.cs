using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Math
{
	/// <summary>Methods for operating on numerical arrays as vectors and matrices.</summary>
	/// <author>Teg Grenager</author>
	public class ArrayMath
	{
		private static readonly Random rand = new Random();

		private ArrayMath()
		{
		}

		// not instantiable
		// BASIC INFO -----------------------------------------------------------------
		public static int NumRows(double[] v)
		{
			return v.Length;
		}

		// GENERATION -----------------------------------------------------------------
		/// <summary>Generate a range of integers from start (inclusive) to end (exclusive).</summary>
		/// <remarks>
		/// Generate a range of integers from start (inclusive) to end (exclusive).
		/// Similar to the Python range() builtin function.
		/// </remarks>
		/// <param name="start">Beginning number (inclusive)</param>
		/// <param name="end">Ending number (exclusive)</param>
		/// <returns>integers from [start...end)</returns>
		public static int[] Range(int start, int end)
		{
			System.Diagnostics.Debug.Assert(end > start);
			int len = end - start;
			int[] range = new int[len];
			for (int i = 0; i < range.Length; ++i)
			{
				range[i] = i + start;
			}
			return range;
		}

		// CASTS ----------------------------------------------------------------------
		public static float[] DoubleArrayToFloatArray(double[] a)
		{
			float[] result = new float[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = (float)a[i];
			}
			return result;
		}

		public static double[] FloatArrayToDoubleArray(float[] a)
		{
			double[] result = new double[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = a[i];
			}
			return result;
		}

		public static double[][] FloatArrayToDoubleArray(float[][] a)
		{
			double[][] result = new double[a.Length][];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = new double[a[i].Length];
				for (int j = 0; j < a[i].Length; j++)
				{
					result[i][j] = a[i][j];
				}
			}
			return result;
		}

		public static float[][] DoubleArrayToFloatArray(double[][] a)
		{
			float[][] result = new float[a.Length][];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = new float[a[i].Length];
				for (int j = 0; j < a[i].Length; j++)
				{
					result[i][j] = (float)a[i][j];
				}
			}
			return result;
		}

		// OPERATIONS ON AN ARRAY - NONDESTRUCTIVE
		public static double[] Exp(double[] a)
		{
			double[] result = new double[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = System.Math.Exp(a[i]);
			}
			return result;
		}

		public static double[] Log(double[] a)
		{
			double[] result = new double[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = System.Math.Log(a[i]);
			}
			return result;
		}

		// OPERATIONS ON AN ARRAY - DESTRUCTIVE
		public static void ExpInPlace(double[] a)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = System.Math.Exp(a[i]);
			}
		}

		public static void LogInPlace(double[] a)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = System.Math.Log(a[i]);
			}
		}

		public static double[] Softmax(double[] scales)
		{
			double[] newScales = new double[scales.Length];
			double sum = 0;
			for (int i = 0; i < scales.Length; i++)
			{
				newScales[i] = System.Math.Exp(scales[i]);
				sum += newScales[i];
			}
			for (int i_1 = 0; i_1 < scales.Length; i_1++)
			{
				newScales[i_1] /= sum;
			}
			return newScales;
		}

		// OPERATIONS WITH SCALAR - DESTRUCTIVE
		/// <summary>Increases the values in the first array a by b.</summary>
		/// <remarks>Increases the values in the first array a by b. Does it in place.</remarks>
		/// <param name="a">The array</param>
		/// <param name="b">The amount by which to increase each item</param>
		public static void AddInPlace(double[] a, double b)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = a[i] + b;
			}
		}

		/// <summary>Increases the values in this array by b.</summary>
		/// <remarks>Increases the values in this array by b. Does it in place.</remarks>
		/// <param name="a">The array</param>
		/// <param name="b">The amount by which to increase each item</param>
		public static void AddInPlace(float[] a, double b)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = (float)(a[i] + b);
			}
		}

		/// <summary>Add c times the array b to array a.</summary>
		/// <remarks>Add c times the array b to array a. Does it in place.</remarks>
		public static void AddMultInPlace(double[] a, double[] b, double c)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] += b[i] * c;
			}
		}

		/// <summary>Scales the values in this array by b.</summary>
		/// <remarks>Scales the values in this array by b. Does it in place.</remarks>
		public static void MultiplyInPlace(double[] a, double b)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = a[i] * b;
			}
		}

		/// <summary>Scales the values in this array by b.</summary>
		/// <remarks>Scales the values in this array by b. Does it in place.</remarks>
		public static void MultiplyInPlace(float[] a, double b)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = (float)(a[i] * b);
			}
		}

		/// <summary>Divides the values in this array by b.</summary>
		/// <remarks>Divides the values in this array by b. Does it in place.</remarks>
		public static void DivideInPlace(double[] a, double b)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = a[i] / b;
			}
		}

		/// <summary>Scales the values in this array by c.</summary>
		public static void PowInPlace(double[] a, double c)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = System.Math.Pow(a[i], c);
			}
		}

		/// <summary>Sets the values in this array by to their value taken to cth power.</summary>
		public static void PowInPlace(float[] a, float c)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = (float)System.Math.Pow(a[i], c);
			}
		}

		// OPERATIONS WITH SCALAR - NONDESTRUCTIVE
		public static double[] Add(double[] a, double c)
		{
			double[] result = new double[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = a[i] + c;
			}
			return result;
		}

		public static float[] Add(float[] a, double c)
		{
			float[] result = new float[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = (float)(a[i] + c);
			}
			return result;
		}

		/// <summary>Scales the values in this array by c.</summary>
		public static double[] Multiply(double[] a, double c)
		{
			double[] result = new double[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = a[i] * c;
			}
			return result;
		}

		/// <summary>Scales the values in this array by c.</summary>
		public static float[] Multiply(float[] a, float c)
		{
			float[] result = new float[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = a[i] * c;
			}
			return result;
		}

		/// <summary>raises each entry in array a by power c</summary>
		public static double[] Pow(double[] a, double c)
		{
			double[] result = new double[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = System.Math.Pow(a[i], c);
			}
			return result;
		}

		/// <summary>raises each entry in array a by power c</summary>
		public static float[] Pow(float[] a, float c)
		{
			float[] result = new float[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = (float)System.Math.Pow(a[i], c);
			}
			return result;
		}

		// OPERATIONS WITH TWO ARRAYS - DESTRUCTIVE
		public static void PairwiseAddInPlace(float[] to, float[] from)
		{
			if (to.Length != from.Length)
			{
				throw new ArgumentException("to length:" + to.Length + " from length:" + from.Length);
			}
			for (int i = 0; i < to.Length; i++)
			{
				to[i] = to[i] + from[i];
			}
		}

		/// <summary>
		/// Add the two 1d arrays in place of
		/// <paramref name="to"/>
		/// .
		/// </summary>
		/// <exception cref="System.ArgumentException">
		/// If
		/// <paramref name="to"/>
		/// and
		/// <paramref name="from"/>
		/// are not of the same dimensions
		/// </exception>
		public static void PairwiseAddInPlace(double[] to, double[] from)
		{
			if (to.Length != from.Length)
			{
				throw new ArgumentException("to length:" + to.Length + " from length:" + from.Length);
			}
			for (int i = 0; i < to.Length; i++)
			{
				to[i] = to[i] + from[i];
			}
		}

		public static void PairwiseAddInPlace(double[] to, int[] from)
		{
			if (to.Length != from.Length)
			{
				throw new ArgumentException();
			}
			for (int i = 0; i < to.Length; i++)
			{
				to[i] = to[i] + from[i];
			}
		}

		public static void PairwiseAddInPlace(double[] to, short[] from)
		{
			if (to.Length != from.Length)
			{
				throw new ArgumentException();
			}
			for (int i = 0; i < to.Length; i++)
			{
				to[i] = to[i] + from[i];
			}
		}

		/// <summary>
		/// Add the two 2d arrays and write the answer in place of
		/// <paramref name="m1"/>
		/// .
		/// </summary>
		/// <exception cref="System.ArgumentException">
		/// If
		/// <paramref name="m1"/>
		/// and
		/// <paramref name="m2"/>
		/// are not of the same dimensions
		/// </exception>
		public static void AddInPlace(double[][] m1, double[][] m2)
		{
			if (m1.Length != m2.Length)
			{
				throw new ArgumentException();
			}
			for (int i = 0; i < m1.Length; i++)
			{
				PairwiseAddInPlace(m1[i], m2[i]);
			}
		}

		public static void PairwiseSubtractInPlace(double[] to, double[] from)
		{
			if (to.Length != from.Length)
			{
				throw new Exception();
			}
			for (int i = 0; i < to.Length; i++)
			{
				to[i] = to[i] - from[i];
			}
		}

		public static void PairwiseScaleAddInPlace(double[] to, double[] from, double fromScale)
		{
			if (to.Length != from.Length)
			{
				throw new Exception();
			}
			for (int i = 0; i < to.Length; i++)
			{
				to[i] = to[i] + fromScale * from[i];
			}
		}

		// OPERATIONS WITH TWO ARRAYS - NONDESTRUCTIVE
		public static int[] PairwiseAdd(int[] a, int[] b)
		{
			int[] result = new int[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = a[i] + b[i];
			}
			return result;
		}

		public static double[] PairwiseAdd(double[] a, double[] b)
		{
			double[] result = new double[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				if (i < b.Length)
				{
					result[i] = a[i] + b[i];
				}
				else
				{
					result[i] = a[i];
				}
			}
			return result;
		}

		public static float[] PairwiseAdd(float[] a, float[] b)
		{
			float[] result = new float[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = a[i] + b[i];
			}
			return result;
		}

		public static double[] PairwiseScaleAdd(double[] a, double[] b, double bScale)
		{
			double[] result = new double[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				result[i] = a[i] + bScale * b[i];
			}
			return result;
		}

		public static double[] PairwiseSubtract(double[] a, double[] b)
		{
			double[] c = new double[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				c[i] = a[i] - b[i];
			}
			return c;
		}

		public static float[] PairwiseSubtract(float[] a, float[] b)
		{
			float[] c = new float[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				c[i] = a[i] - b[i];
			}
			return c;
		}

		/// <summary>Assumes that both arrays have same length.</summary>
		public static double DotProduct(double[] a, double[] b)
		{
			if (a.Length != b.Length)
			{
				throw new Exception("Can't calculate dot product of multiple different lengths: a.length=" + a.Length + " b.length=" + b.Length);
			}
			double result = 0;
			for (int i = 0; i < a.Length; i++)
			{
				result += a[i] * b[i];
			}
			return result;
		}

		/// <summary>Assumes that both arrays have same length.</summary>
		public static double[] PairwiseMultiply(double[] a, double[] b)
		{
			if (a.Length != b.Length)
			{
				throw new Exception("Can't pairwise multiple different lengths: a.length=" + a.Length + " b.length=" + b.Length);
			}
			double[] result = new double[a.Length];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = a[i] * b[i];
			}
			return result;
		}

		/// <summary>Assumes that both arrays have same length.</summary>
		public static float[] PairwiseMultiply(float[] a, float[] b)
		{
			if (a.Length != b.Length)
			{
				throw new Exception();
			}
			float[] result = new float[a.Length];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = a[i] * b[i];
			}
			return result;
		}

		/// <summary>Puts the result in the result array.</summary>
		/// <remarks>
		/// Puts the result in the result array.
		/// Assumes that all arrays have same length.
		/// </remarks>
		public static void PairwiseMultiply(double[] a, double[] b, double[] result)
		{
			if (a.Length != b.Length)
			{
				throw new Exception();
			}
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = a[i] * b[i];
			}
		}

		/// <summary>Puts the result in the result array.</summary>
		/// <remarks>
		/// Puts the result in the result array.
		/// Assumes that all arrays have same length.
		/// </remarks>
		public static void PairwiseMultiply(float[] a, float[] b, float[] result)
		{
			if (a.Length != b.Length)
			{
				throw new Exception();
			}
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = a[i] * b[i];
			}
		}

		/// <summary>
		/// Divide the first array by the second elementwise,
		/// and store results in place.
		/// </summary>
		/// <remarks>
		/// Divide the first array by the second elementwise,
		/// and store results in place. Assume arrays have
		/// the same length
		/// </remarks>
		public static void PairwiseDivideInPlace(double[] a, double[] b)
		{
			if (a.Length != b.Length)
			{
				throw new Exception();
			}
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = a[i] / b[i];
			}
		}

		// ERROR CHECKING
		public static bool HasNaN(double[] a)
		{
			foreach (double x in a)
			{
				if (double.IsNaN(x))
				{
					return true;
				}
			}
			return false;
		}

		public static bool HasInfinite(double[] a)
		{
			foreach (double anA in a)
			{
				if (double.IsInfinite(anA))
				{
					return true;
				}
			}
			return false;
		}

		public static bool HasNaN(float[] a)
		{
			foreach (float x in a)
			{
				if (float.IsNaN(x))
				{
					return true;
				}
			}
			return false;
		}

		// methods for filtering vectors ------------------------------------------
		public static int CountNaN(double[] v)
		{
			int c = 0;
			foreach (double d in v)
			{
				if (double.IsNaN(d))
				{
					c++;
				}
			}
			return c;
		}

		public static double[] FilterNaN(double[] v)
		{
			double[] u = new double[NumRows(v) - CountNaN(v)];
			int j = 0;
			foreach (double d in v)
			{
				if (!double.IsNaN(d))
				{
					u[j++] = d;
				}
			}
			return u;
		}

		public static int CountInfinite(double[] v)
		{
			int c = 0;
			foreach (double aV in v)
			{
				if (double.IsInfinite(aV))
				{
					c++;
				}
			}
			return c;
		}

		public static int CountNonZero(double[] v)
		{
			int c = 0;
			foreach (double aV in v)
			{
				if (aV != 0.0)
				{
					++c;
				}
			}
			return c;
		}

		public static int CountCloseToZero(double[] v, double epsilon)
		{
			int c = 0;
			foreach (double aV in v)
			{
				if (System.Math.Abs(aV) < epsilon)
				{
					++c;
				}
			}
			return c;
		}

		public static int CountPositive(double[] v)
		{
			int c = 0;
			foreach (double a in v)
			{
				if (a > 0.0)
				{
					++c;
				}
			}
			return c;
		}

		public static int CountNegative(double[] v)
		{
			int c = 0;
			foreach (double aV in v)
			{
				if (aV < 0.0)
				{
					++c;
				}
			}
			return c;
		}

		public static double[] FilterInfinite(double[] v)
		{
			double[] u = new double[NumRows(v) - CountInfinite(v)];
			int j = 0;
			foreach (double aV in v)
			{
				if (!double.IsInfinite(aV))
				{
					u[j++] = aV;
				}
			}
			return u;
		}

		public static double[] FilterNaNAndInfinite(double[] v)
		{
			return FilterInfinite(FilterNaN(v));
		}

		// VECTOR PROPERTIES
		/// <summary>Returns the sum of an array of doubles.</summary>
		public static double Sum(double[] a)
		{
			return Sum(a, 0, a.Length);
		}

		/// <summary>
		/// Returns the sum of the portion of an array of numbers between
		/// <paramref name="fromIndex"/>
		/// , inclusive, and
		/// <paramref name="toIndex"/>
		/// , exclusive.
		/// Returns 0 if
		/// <c>fromIndex &gt;= toIndex</c>
		/// .
		/// </summary>
		public static double Sum(double[] a, int fromIndex, int toIndex)
		{
			double result = 0.0;
			for (int i = fromIndex; i < toIndex; i++)
			{
				result += a[i];
			}
			return result;
		}

		public static int Sum(int[] a)
		{
			int result = 0;
			foreach (int i in a)
			{
				result += i;
			}
			return result;
		}

		public static float Sum(float[] a)
		{
			float result = 0.0F;
			foreach (float f in a)
			{
				result += f;
			}
			return result;
		}

		public static int Sum(int[][] a)
		{
			int result = 0;
			foreach (int[] v in a)
			{
				foreach (int item in v)
				{
					result += item;
				}
			}
			return result;
		}

		/// <summary>Returns diagonal elements of the given (square) matrix.</summary>
		public static int[] Diag(int[][] a)
		{
			int[] rv = new int[a.Length];
			for (int i = 0; i < a.Length; i++)
			{
				rv[i] = a[i][i];
			}
			return rv;
		}

		public static double Average(double[] a)
		{
			double total = Edu.Stanford.Nlp.Math.ArrayMath.Sum(a);
			return total / a.Length;
		}

		/// <summary>This version avoids any possibility of overflow.</summary>
		public static double IterativeAverage(double[] a)
		{
			double avg = 0.0;
			int t = 1;
			foreach (double x in a)
			{
				avg += (x - avg) / t;
				t++;
			}
			return avg;
		}

		/// <summary>Computes inf-norm of vector.</summary>
		/// <remarks>
		/// Computes inf-norm of vector.
		/// This is just the largest absolute value of an element.
		/// </remarks>
		/// <param name="a">Array of double</param>
		/// <returns>inf-norm of a</returns>
		public static double Norm_inf(double[] a)
		{
			double max = double.NegativeInfinity;
			foreach (double d in a)
			{
				if (System.Math.Abs(d) > max)
				{
					max = System.Math.Abs(d);
				}
			}
			return max;
		}

		/// <summary>Computes inf-norm of vector.</summary>
		/// <returns>inf-norm of a</returns>
		public static double Norm_inf(float[] a)
		{
			double max = double.NegativeInfinity;
			foreach (float anA in a)
			{
				if (System.Math.Abs(anA) > max)
				{
					max = System.Math.Abs(anA);
				}
			}
			return max;
		}

		/// <summary>Computes 1-norm of vector.</summary>
		/// <param name="a">A vector of double</param>
		/// <returns>1-norm of a</returns>
		public static double Norm_1(double[] a)
		{
			double sum = 0.0;
			foreach (double anA in a)
			{
				sum += System.Math.Abs(anA);
			}
			return sum;
		}

		/// <summary>Computes 1-norm of vector.</summary>
		/// <param name="a">A vector of floats</param>
		/// <returns>1-norm of a</returns>
		public static double Norm_1(float[] a)
		{
			double sum = 0.0;
			foreach (float anA in a)
			{
				sum += System.Math.Abs(anA);
			}
			return sum;
		}

		/// <summary>Computes 2-norm of vector.</summary>
		/// <param name="a">A vector of double</param>
		/// <returns>Euclidean norm of a</returns>
		public static double Norm(double[] a)
		{
			double squaredSum = 0.0;
			foreach (double anA in a)
			{
				squaredSum += anA * anA;
			}
			return System.Math.Sqrt(squaredSum);
		}

		/// <summary>Computes 2-norm of vector.</summary>
		/// <param name="a">A vector of floats</param>
		/// <returns>Euclidean norm of a</returns>
		public static double Norm(float[] a)
		{
			double squaredSum = 0.0;
			foreach (float anA in a)
			{
				squaredSum += anA * anA;
			}
			return System.Math.Sqrt(squaredSum);
		}

		/// <returns>the index of the max value; if max is a tie, returns the first one.</returns>
		public static int Argmax(double[] a)
		{
			double max = double.NegativeInfinity;
			int argmax = 0;
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] > max)
				{
					max = a[i];
					argmax = i;
				}
			}
			return argmax;
		}

		/// <returns>the index of the max value; if max is a tie, returns the last one.</returns>
		public static int Argmax_tieLast(double[] a)
		{
			double max = double.NegativeInfinity;
			int argmax = 0;
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] >= max)
				{
					max = a[i];
					argmax = i;
				}
			}
			return argmax;
		}

		public static double Max(double[] a)
		{
			return a[Argmax(a)];
		}

		public static double Max(ICollection<double> a)
		{
			double max = double.NegativeInfinity;
			foreach (double d in a)
			{
				if (d > max)
				{
					max = d;
				}
			}
			return max;
		}

		/// <returns>the index of the max value; if max is a tie, returns the first one.</returns>
		public static int Argmax(float[] a)
		{
			float max = float.NegativeInfinity;
			int argmax = 0;
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] > max)
				{
					max = a[i];
					argmax = i;
				}
			}
			return argmax;
		}

		public static float Max(float[] a)
		{
			return a[Argmax(a)];
		}

		/// <returns>the index of the min value; if min is a tie, returns the lowest index one.</returns>
		public static int Argmin(double[] a)
		{
			double min = double.PositiveInfinity;
			int argmin = 0;
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] < min)
				{
					min = a[i];
					argmin = i;
				}
			}
			return argmin;
		}

		/// <returns>The minimum value in an array.</returns>
		public static double Min(params double[] vector)
		{
			double min = double.PositiveInfinity;
			foreach (double x in vector)
			{
				if (x < min)
				{
					min = x;
				}
			}
			return min;
		}

		/// <summary>Returns the smallest value in a vector of doubles.</summary>
		/// <remarks>
		/// Returns the smallest value in a vector of doubles.  Any values which
		/// are NaN or infinite are ignored.  If the vector is empty, 0.0 is
		/// returned.
		/// </remarks>
		public static double SafeMin(double[] v)
		{
			double[] u = FilterNaNAndInfinite(v);
			if (NumRows(u) == 0)
			{
				return 0.0;
			}
			return Min(u);
		}

		/// <returns>the index of the min value; if min is a tie, returns the first one.</returns>
		public static int Argmin(float[] a)
		{
			float min = float.PositiveInfinity;
			int argmin = 0;
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] < min)
				{
					min = a[i];
					argmin = i;
				}
			}
			return argmin;
		}

		public static float Min(float[] a)
		{
			return a[Argmin(a)];
		}

		/// <returns>the index of the min value; if min is a tie, returns the first one.</returns>
		public static int Argmin(int[] a)
		{
			int min = int.MaxValue;
			int argmin = 0;
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] < min)
				{
					min = a[i];
					argmin = i;
				}
			}
			return argmin;
		}

		/// <returns>the min value.</returns>
		public static int Min(params int[] vector)
		{
			int min = int.MaxValue;
			foreach (int x in vector)
			{
				if (x < min)
				{
					min = x;
				}
			}
			return min;
		}

		/// <returns>the index of the max value; if max is a tie, returns the first one.</returns>
		public static int Argmax(int[] a)
		{
			int max = int.MinValue;
			int argmax = 0;
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] > max)
				{
					max = a[i];
					argmax = i;
				}
			}
			return argmax;
		}

		/// <returns>the index of the max value; if max is a tie, returns the first one.</returns>
		public static int Max(params int[] vector)
		{
			int max = int.MinValue;
			foreach (int x in vector)
			{
				if (x > max)
				{
					max = x;
				}
			}
			return max;
		}

		/// <summary>Returns the smallest element of the matrix</summary>
		public static int Min(int[][] matrix)
		{
			int min = int.MaxValue;
			foreach (int[] row in matrix)
			{
				foreach (int elem in row)
				{
					min = System.Math.Min(min, elem);
				}
			}
			return min;
		}

		/// <summary>Returns the smallest element of the matrix</summary>
		public static int Max(int[][] matrix)
		{
			int max = int.MinValue;
			foreach (int[] row in matrix)
			{
				foreach (int elem in row)
				{
					max = System.Math.Max(max, elem);
				}
			}
			return max;
		}

		/// <summary>Returns the largest value in a vector of doubles.</summary>
		/// <remarks>
		/// Returns the largest value in a vector of doubles.  Any values which
		/// are NaN or infinite are ignored.  If the vector is empty, 0.0 is
		/// returned.
		/// </remarks>
		public static double SafeMax(double[] v)
		{
			double[] u = FilterNaNAndInfinite(v);
			if (NumRows(u) == 0)
			{
				return 0.0;
			}
			return Max(u);
		}

		/// <summary>
		/// Returns the log of the sum of an array of numbers, which are
		/// themselves input in log form.
		/// </summary>
		/// <remarks>
		/// Returns the log of the sum of an array of numbers, which are
		/// themselves input in log form.  This is all natural logarithms.
		/// Reasonable care is taken to do this as efficiently as possible
		/// (under the assumption that the numbers might differ greatly in
		/// magnitude), with high accuracy, and without numerical overflow.
		/// </remarks>
		/// <param name="logInputs">An array of numbers [log(x1), ..., log(xn)]</param>
		/// <returns>
		/// 
		/// <literal>log(x1 + ... + xn)</literal>
		/// </returns>
		public static double LogSum(params double[] logInputs)
		{
			return LogSum(logInputs, 0, logInputs.Length);
		}

		/// <summary>
		/// Returns the log of the portion between
		/// <paramref name="fromIndex"/>
		/// , inclusive, and
		/// <paramref name="toIndex"/>
		/// , exclusive, of an array of numbers, which are
		/// themselves input in log form.  This is all natural logarithms.
		/// Reasonable care is taken to do this as efficiently as possible
		/// (under the assumption that the numbers might differ greatly in
		/// magnitude), with high accuracy, and without numerical overflow.  Throws an
		/// <see cref="System.ArgumentException"/>
		/// if
		/// <paramref name="logInputs"/>
		/// is of length zero.
		/// Otherwise, returns Double.NEGATIVE_INFINITY if
		/// <paramref name="fromIndex"/>
		/// &gt;=
		/// <paramref name="toIndex"/>
		/// .
		/// </summary>
		/// <param name="logInputs">An array of numbers [log(x1), ..., log(xn)]</param>
		/// <param name="fromIndex">The array index to start the sum from</param>
		/// <param name="toIndex">The array index after the last element to be summed</param>
		/// <returns>
		/// 
		/// <literal>log(x1 + ... + xn)</literal>
		/// </returns>
		public static double LogSum(double[] logInputs, int fromIndex, int toIndex)
		{
			if (Thread.Interrupted())
			{
				// A good place to check for interrupts -- many functions call this
				throw new RuntimeInterruptedException();
			}
			if (logInputs.Length == 0)
			{
				throw new ArgumentException();
			}
			if (fromIndex >= 0 && toIndex < logInputs.Length && fromIndex >= toIndex)
			{
				return double.NegativeInfinity;
			}
			int maxIdx = fromIndex;
			double max = logInputs[fromIndex];
			for (int i = fromIndex + 1; i < toIndex; i++)
			{
				if (logInputs[i] > max)
				{
					maxIdx = i;
					max = logInputs[i];
				}
			}
			bool haveTerms = false;
			double intermediate = 0.0;
			double cutoff = max - SloppyMath.Logtolerance;
			// we avoid rearranging the array and so test indices each time!
			for (int i_1 = fromIndex; i_1 < toIndex; i_1++)
			{
				if (i_1 != maxIdx && logInputs[i_1] > cutoff)
				{
					haveTerms = true;
					intermediate += System.Math.Exp(logInputs[i_1] - max);
				}
			}
			if (haveTerms)
			{
				return max + System.Math.Log(1.0 + intermediate);
			}
			else
			{
				return max;
			}
		}

		/// <summary>
		/// Returns the log of the portion between
		/// <paramref name="fromIndex"/>
		/// , inclusive, and
		/// <c>toIndex</c>
		/// , exclusive, of an array of numbers, which are
		/// themselves input in log form.  This is all natural logarithms.
		/// This version incorporates a stride, so you can sum only select numbers.
		/// Reasonable care is taken to do this as efficiently as possible
		/// (under the assumption that the numbers might differ greatly in
		/// magnitude), with high accuracy, and without numerical overflow.  Throws an
		/// <see cref="System.ArgumentException"/>
		/// if
		/// <paramref name="logInputs"/>
		/// is of length zero.
		/// Otherwise, returns Double.NEGATIVE_INFINITY if
		/// <paramref name="fromIndex"/>
		/// &gt;=
		/// <c>toIndex</c>
		/// .
		/// </summary>
		/// <param name="logInputs">An array of numbers [log(x1), ..., log(xn)]</param>
		/// <param name="fromIndex">The array index to start the sum from</param>
		/// <param name="afterIndex">The array index after the last element to be summed</param>
		/// <returns>
		/// 
		/// <literal>log(x1 + ... + xn)</literal>
		/// </returns>
		public static double LogSum(double[] logInputs, int fromIndex, int afterIndex, int stride)
		{
			if (logInputs.Length == 0)
			{
				throw new ArgumentException();
			}
			if (fromIndex >= 0 && afterIndex < logInputs.Length && fromIndex >= afterIndex)
			{
				return double.NegativeInfinity;
			}
			int maxIdx = fromIndex;
			double max = logInputs[fromIndex];
			for (int i = fromIndex + stride; i < afterIndex; i += stride)
			{
				if (logInputs[i] > max)
				{
					maxIdx = i;
					max = logInputs[i];
				}
			}
			bool haveTerms = false;
			double intermediate = 0.0;
			double cutoff = max - SloppyMath.Logtolerance;
			// we avoid rearranging the array and so test indices each time!
			for (int i_1 = fromIndex; i_1 < afterIndex; i_1 += stride)
			{
				if (i_1 != maxIdx && logInputs[i_1] > cutoff)
				{
					haveTerms = true;
					intermediate += System.Math.Exp(logInputs[i_1] - max);
				}
			}
			if (haveTerms)
			{
				return max + System.Math.Log(1.0 + intermediate);
			}
			else
			{
				// using Math.log1p(intermediate) may be more accurate, but is slower
				return max;
			}
		}

		public static double LogSum(IList<double> logInputs)
		{
			return LogSum(logInputs, 0, logInputs.Count);
		}

		public static double LogSum(IList<double> logInputs, int fromIndex, int toIndex)
		{
			int length = logInputs.Count;
			if (length == 0)
			{
				throw new ArgumentException();
			}
			if (fromIndex >= 0 && toIndex < length && fromIndex >= toIndex)
			{
				return double.NegativeInfinity;
			}
			int maxIdx = fromIndex;
			double max = logInputs[fromIndex];
			for (int i = fromIndex + 1; i < toIndex; i++)
			{
				double d = logInputs[i];
				if (d > max)
				{
					maxIdx = i;
					max = d;
				}
			}
			bool haveTerms = false;
			double intermediate = 0.0;
			double cutoff = max - SloppyMath.Logtolerance;
			// we avoid rearranging the array and so test indices each time!
			for (int i_1 = fromIndex; i_1 < toIndex; i_1++)
			{
				double d = logInputs[i_1];
				if (i_1 != maxIdx && d > cutoff)
				{
					haveTerms = true;
					intermediate += System.Math.Exp(d - max);
				}
			}
			if (haveTerms)
			{
				return max + System.Math.Log(1.0 + intermediate);
			}
			else
			{
				return max;
			}
		}

		/// <summary>
		/// Returns the log of the sum of an array of numbers, which are
		/// themselves input in log form.
		/// </summary>
		/// <remarks>
		/// Returns the log of the sum of an array of numbers, which are
		/// themselves input in log form.  This is all natural logarithms.
		/// Reasonable care is taken to do this as efficiently as possible
		/// (under the assumption that the numbers might differ greatly in
		/// magnitude), with high accuracy, and without numerical overflow.
		/// </remarks>
		/// <param name="logInputs">An array of numbers [log(x1), ..., log(xn)]</param>
		/// <returns>
		/// 
		/// <literal>log(x1 + ... + xn)</literal>
		/// </returns>
		public static float LogSum(float[] logInputs)
		{
			int leng = logInputs.Length;
			if (leng == 0)
			{
				throw new ArgumentException();
			}
			int maxIdx = 0;
			float max = logInputs[0];
			for (int i = 1; i < leng; i++)
			{
				if (logInputs[i] > max)
				{
					maxIdx = i;
					max = logInputs[i];
				}
			}
			bool haveTerms = false;
			double intermediate = 0.0f;
			float cutoff = max - SloppyMath.LogtoleranceF;
			// we avoid rearranging the array and so test indices each time!
			for (int i_1 = 0; i_1 < leng; i_1++)
			{
				if (i_1 != maxIdx && logInputs[i_1] > cutoff)
				{
					haveTerms = true;
					intermediate += System.Math.Exp(logInputs[i_1] - max);
				}
			}
			if (haveTerms)
			{
				return max + (float)System.Math.Log(1.0 + intermediate);
			}
			else
			{
				return max;
			}
		}

		// LINEAR ALGEBRAIC FUNCTIONS
		public static double InnerProduct(double[] a, double[] b)
		{
			double result = 0.0;
			int len = System.Math.Min(a.Length, b.Length);
			for (int i = 0; i < len; i++)
			{
				result += a[i] * b[i];
			}
			return result;
		}

		public static double InnerProduct(float[] a, float[] b)
		{
			double result = 0.0;
			int len = System.Math.Min(a.Length, b.Length);
			for (int i = 0; i < len; i++)
			{
				result += a[i] * b[i];
			}
			return result;
		}

		// UTILITIES
		/// <exception cref="System.IO.IOException"/>
		public static double[][] Load2DMatrixFromFile(string filename)
		{
			string s = IOUtils.SlurpFile(filename);
			string[] rows = s.Split("[\r\n]+");
			double[][] result = new double[rows.Length][];
			for (int i = 0; i < result.Length; i++)
			{
				string[] columns = rows[i].Split("\\s+");
				result[i] = new double[columns.Length];
				for (int j = 0; j < result[i].Length; j++)
				{
					result[i][j] = double.Parse(columns[j]);
				}
			}
			return result;
		}

		public static int[] Box(int[] assignment)
		{
			int[] result = new int[assignment.Length];
			for (int i = 0; i < assignment.Length; i++)
			{
				result[i] = int.Parse(assignment[i]);
			}
			return result;
		}

		public static int[] UnboxToInt(ICollection<int> list)
		{
			int[] result = new int[list.Count];
			int i = 0;
			foreach (int v in list)
			{
				result[i++] = v;
			}
			return result;
		}

		public static double[] Box(double[] assignment)
		{
			double[] result = new double[assignment.Length];
			for (int i = 0; i < assignment.Length; i++)
			{
				result[i] = double.ValueOf(assignment[i]);
			}
			return result;
		}

		public static double[] Unbox(ICollection<double> list)
		{
			double[] result = new double[list.Count];
			int i = 0;
			foreach (double v in list)
			{
				result[i++] = v;
			}
			return result;
		}

		public static int IndexOf(int n, int[] a)
		{
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] == n)
				{
					return i;
				}
			}
			return -1;
		}

		public static int[][] CastToInt(double[][] doubleCounts)
		{
			int[][] result = new int[doubleCounts.Length][];
			for (int i = 0; i < doubleCounts.Length; i++)
			{
				result[i] = new int[doubleCounts[i].Length];
				for (int j = 0; j < doubleCounts[i].Length; j++)
				{
					result[i][j] = (int)doubleCounts[i][j];
				}
			}
			return result;
		}

		// PROBABILITY FUNCTIONS
		/// <summary>Makes the values in this array sum to 1.0.</summary>
		/// <remarks>
		/// Makes the values in this array sum to 1.0. Does it in place.
		/// If the total is 0.0 or NaN, throws an RuntimeException.
		/// </remarks>
		public static void Normalize(double[] a)
		{
			double total = Sum(a);
			if (total == 0.0 || double.IsNaN(total))
			{
				throw new Exception("Can't normalize an array with sum 0.0 or NaN: " + Arrays.ToString(a));
			}
			MultiplyInPlace(a, 1.0 / total);
		}

		// divide each value by total
		public static void L1normalize(double[] a)
		{
			double total = L1Norm(a);
			if (total == 0.0 || double.IsNaN(total))
			{
				if (a.Length < 100)
				{
					throw new Exception("Can't normalize an array with sum 0.0 or NaN: " + Arrays.ToString(a));
				}
				else
				{
					double[] aTrunc = new double[100];
					System.Array.Copy(a, 0, aTrunc, 0, 100);
					throw new Exception("Can't normalize an array with sum 0.0 or NaN: " + Arrays.ToString(aTrunc) + " ... ");
				}
			}
			MultiplyInPlace(a, 1.0 / total);
		}

		// divide each value by total
		public static void L2normalize(double[] a)
		{
			double total = L2Norm(a);
			if (total == 0.0 || double.IsNaN(total))
			{
				if (a.Length < 100)
				{
					throw new Exception("Can't normalize an array with sum 0.0 or NaN: " + Arrays.ToString(a));
				}
				else
				{
					double[] aTrunc = new double[100];
					System.Array.Copy(a, 0, aTrunc, 0, 100);
					throw new Exception("Can't normalize an array with sum 0.0 or NaN: " + Arrays.ToString(aTrunc) + " ... ");
				}
			}
			MultiplyInPlace(a, 1.0 / total);
		}

		// divide each value by total
		/// <summary>Makes the values in this array sum to 1.0.</summary>
		/// <remarks>
		/// Makes the values in this array sum to 1.0. Does it in place.
		/// If the total is 0.0 or NaN, throws an RuntimeException.
		/// </remarks>
		public static void Normalize(float[] a)
		{
			float total = Sum(a);
			if (total == 0.0f || double.IsNaN(total))
			{
				throw new Exception("Can't normalize an array with sum 0.0 or NaN");
			}
			MultiplyInPlace(a, 1.0f / total);
		}

		// divide each value by total
		public static void L2normalize(float[] a)
		{
			float total = L2Norm(a);
			if (total == 0.0 || float.IsNaN(total))
			{
				if (a.Length < 100)
				{
					throw new Exception("Can't normalize an array with sum 0.0 or NaN: " + Arrays.ToString(a));
				}
				else
				{
					float[] aTrunc = new float[100];
					System.Array.Copy(a, 0, aTrunc, 0, 100);
					throw new Exception("Can't normalize an array with sum 0.0 or NaN: " + Arrays.ToString(aTrunc) + " ... ");
				}
			}
			MultiplyInPlace(a, 1.0 / total);
		}

		// divide each value by total
		/// <summary>Standardize values in this array, i.e., subtract the mean and divide by the standard deviation.</summary>
		/// <remarks>
		/// Standardize values in this array, i.e., subtract the mean and divide by the standard deviation.
		/// If standard deviation is 0.0, throws a RuntimeException.
		/// </remarks>
		public static void Standardize(double[] a)
		{
			double m = Mean(a);
			if (double.IsNaN(m))
			{
				throw new Exception("Can't standardize array whose mean is NaN");
			}
			double s = Stdev(a);
			if (s == 0.0 || double.IsNaN(s))
			{
				throw new Exception("Can't standardize array whose standard deviation is 0.0 or NaN");
			}
			AddInPlace(a, -m);
			// subtract mean
			MultiplyInPlace(a, 1.0 / s);
		}

		// divide by standard deviation
		public static double L2Norm(double[] a)
		{
			double result = 0.0;
			foreach (double d in a)
			{
				result += System.Math.Pow(d, 2);
			}
			return System.Math.Sqrt(result);
		}

		public static float L2Norm(float[] a)
		{
			double result = 0;
			foreach (float d in a)
			{
				result += System.Math.Pow(d, 2);
			}
			return (float)System.Math.Sqrt(result);
		}

		public static double L1Norm(double[] a)
		{
			double result = 0.0;
			foreach (double d in a)
			{
				result += System.Math.Abs(d);
			}
			return result;
		}

		/// <summary>Makes the values in this array sum to 1.0.</summary>
		/// <remarks>
		/// Makes the values in this array sum to 1.0. Does it in place.
		/// If the total is 0.0, throws a RuntimeException.
		/// If the total is Double.NEGATIVE_INFINITY, then it replaces the
		/// array with a normalized uniform distribution. CDM: This last bit is
		/// weird!  Do we really want that?
		/// </remarks>
		public static void LogNormalize(double[] a)
		{
			double logTotal = LogSum(a);
			if (logTotal == double.NegativeInfinity)
			{
				// to avoid NaN values
				double v = -System.Math.Log(a.Length);
				for (int i = 0; i < a.Length; i++)
				{
					a[i] = v;
				}
				return;
			}
			AddInPlace(a, -logTotal);
		}

		// subtract log total from each value
		/// <summary>Samples from the distribution over values 0 through d.length given by d.</summary>
		/// <remarks>
		/// Samples from the distribution over values 0 through d.length given by d.
		/// Assumes that the distribution sums to 1.0.
		/// </remarks>
		/// <param name="d">the distribution to sample from</param>
		/// <returns>a value from 0 to d.length</returns>
		public static int SampleFromDistribution(double[] d)
		{
			return SampleFromDistribution(d, rand);
		}

		/// <summary>Samples from the distribution over values 0 through d.length given by d.</summary>
		/// <remarks>
		/// Samples from the distribution over values 0 through d.length given by d.
		/// Assumes that the distribution sums to 1.0.
		/// </remarks>
		/// <param name="d">the distribution to sample from</param>
		/// <returns>a value from 0 to d.length</returns>
		public static int SampleFromDistribution(double[] d, Random random)
		{
			// sample from the uniform [0,1]
			double r = random.NextDouble();
			// now compare its value to cumulative values to find what interval it falls in
			double total = 0;
			for (int i = 0; i < d.Length - 1; i++)
			{
				if (double.IsNaN(d[i]))
				{
					throw new Exception("Can't sample from NaN");
				}
				total += d[i];
				if (r < total)
				{
					return i;
				}
			}
			return d.Length - 1;
		}

		// in case the "double-math" didn't total to exactly 1.0
		/// <summary>Samples from the distribution over values 0 through d.length given by d.</summary>
		/// <remarks>
		/// Samples from the distribution over values 0 through d.length given by d.
		/// Assumes that the distribution sums to 1.0.
		/// </remarks>
		/// <param name="d">the distribution to sample from</param>
		/// <returns>a value from 0 to d.length</returns>
		public static int SampleFromDistribution(float[] d, Random random)
		{
			// sample from the uniform [0,1]
			double r = random.NextDouble();
			// now compare its value to cumulative values to find what interval it falls in
			double total = 0;
			for (int i = 0; i < d.Length - 1; i++)
			{
				if (float.IsNaN(d[i]))
				{
					throw new Exception("Can't sample from NaN");
				}
				total += d[i];
				if (r < total)
				{
					return i;
				}
			}
			return d.Length - 1;
		}

		// in case the "double-math" didn't total to exactly 1.0
		public static double KlDivergence(double[] from, double[] to)
		{
			double kl = 0.0;
			double tot = Sum(from);
			double tot2 = Sum(to);
			// System.out.println("tot is " + tot + " tot2 is " + tot2);
			for (int i = 0; i < from.Length; i++)
			{
				if (from[i] == 0.0)
				{
					continue;
				}
				double num = from[i] / tot;
				double num2 = to[i] / tot2;
				// System.out.println("num is " + num + " num2 is " + num2);
				kl += num * (System.Math.Log(num / num2) / System.Math.Log(2.0));
			}
			return kl;
		}

		/// <summary>
		/// Returns the Jensen Shannon divergence (information radius) between
		/// a and b, defined as the average of the kl divergences from a to b
		/// and from b to a.
		/// </summary>
		public static double JensenShannonDivergence(double[] a, double[] b)
		{
			double[] average = PairwiseAdd(a, b);
			MultiplyInPlace(average, .5);
			return .5 * KlDivergence(a, average) + .5 * KlDivergence(b, average);
		}

		public static void SetToLogDeterministic(float[] a, int i)
		{
			for (int j = 0; j < a.Length; j++)
			{
				if (j == i)
				{
					a[j] = 0.0F;
				}
				else
				{
					a[j] = float.NegativeInfinity;
				}
			}
		}

		public static void SetToLogDeterministic(double[] a, int i)
		{
			for (int j = 0; j < a.Length; j++)
			{
				if (j == i)
				{
					a[j] = 0.0;
				}
				else
				{
					a[j] = double.NegativeInfinity;
				}
			}
		}

		// SAMPLE ANALYSIS
		public static double Mean(double[] a)
		{
			return Sum(a) / a.Length;
		}

		/// <summary>Return the mean of an array of int.</summary>
		public static double Mean(int[] a)
		{
			return ((double)Sum(a)) / a.Length;
		}

		public static double Median(double[] a)
		{
			double[] b = new double[a.Length];
			System.Array.Copy(a, 0, b, 0, b.Length);
			Arrays.Sort(b);
			int mid = b.Length / 2;
			if (b.Length % 2 == 0)
			{
				return (b[mid - 1] + b[mid]) / 2.0;
			}
			else
			{
				return b[mid];
			}
		}

		/// <summary>Returns the mean of a vector of doubles.</summary>
		/// <remarks>
		/// Returns the mean of a vector of doubles.  Any values which are NaN or
		/// infinite are ignored.  If the vector is empty, 0.0 is returned.
		/// </remarks>
		public static double SafeMean(double[] v)
		{
			double[] u = FilterNaNAndInfinite(v);
			if (NumRows(u) == 0)
			{
				return 0.0;
			}
			return Mean(u);
		}

		public static double SumSquaredError(double[] a)
		{
			double mean = Mean(a);
			double result = 0.0;
			foreach (double anA in a)
			{
				double diff = anA - mean;
				result += (diff * diff);
			}
			return result;
		}

		public static double SumSquared(double[] a)
		{
			double result = 0.0;
			foreach (double anA in a)
			{
				result += (anA * anA);
			}
			return result;
		}

		public static double Variance(double[] a)
		{
			return SumSquaredError(a) / (a.Length - 1);
		}

		public static double Stdev(double[] a)
		{
			return System.Math.Sqrt(Variance(a));
		}

		/// <summary>Returns the standard deviation of a vector of doubles.</summary>
		/// <remarks>
		/// Returns the standard deviation of a vector of doubles.  Any values which
		/// are NaN or infinite are ignored.  If the vector contains fewer than two
		/// values, 1.0 is returned.
		/// </remarks>
		public static double SafeStdev(double[] v)
		{
			double[] u = FilterNaNAndInfinite(v);
			if (NumRows(u) < 2)
			{
				return 1.0;
			}
			return Stdev(u);
		}

		public static double StandardErrorOfMean(double[] a)
		{
			return Stdev(a) / System.Math.Sqrt(a.Length);
		}

		/// <summary>Fills the array with sample from 0 to numArgClasses-1 without replacement.</summary>
		public static void SampleWithoutReplacement(int[] array, int numArgClasses)
		{
			SampleWithoutReplacement(array, numArgClasses, rand);
		}

		/// <summary>Fills the array with sample from 0 to numArgClasses-1 without replacement.</summary>
		public static void SampleWithoutReplacement(int[] array, int numArgClasses, Random rand)
		{
			int[] temp = new int[numArgClasses];
			for (int i = 0; i < temp.Length; i++)
			{
				temp[i] = i;
			}
			Shuffle(temp, rand);
			System.Array.Copy(temp, 0, array, 0, array.Length);
		}

		public static void Shuffle(int[] a)
		{
			Shuffle(a, rand);
		}

		/* Shuffle the integers in an array using a source of randomness.
		* Uses the Fisher-Yates shuffle. Makes all orderings equally likely, iff
		* the randomizer is good.
		*
		* @param a The array to shuffle
		* @param rand The source of randomness
		*/
		public static void Shuffle(int[] a, Random rand)
		{
			for (int i = a.Length - 1; i > 0; i--)
			{
				int j = rand.NextInt(i + 1);
				// a random index from 0 to i inclusive, may shuffle with itself
				int tmp = a[i];
				a[i] = a[j];
				a[j] = tmp;
			}
		}

		public static void Reverse(int[] a)
		{
			for (int i = 0; i < a.Length / 2; i++)
			{
				int j = a.Length - i - 1;
				int tmp = a[i];
				a[i] = a[j];
				a[j] = tmp;
			}
		}

		public static bool Contains(int[] a, int i)
		{
			foreach (int k in a)
			{
				if (k == i)
				{
					return true;
				}
			}
			return false;
		}

		public static bool ContainsInSubarray(int[] a, int begin, int end, int i)
		{
			for (int j = begin; j < end; j++)
			{
				if (a[j] == i)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Direct computation of Pearson product-moment correlation coefficient.</summary>
		/// <remarks>
		/// Direct computation of Pearson product-moment correlation coefficient.
		/// Note that if x and y are involved in several computations of
		/// pearsonCorrelation, it is perhaps more advisable to first standardize
		/// x and y, then compute innerProduct(x,y)/(x.length-1).
		/// </remarks>
		public static double PearsonCorrelation(double[] x, double[] y)
		{
			double result;
			double sum_sq_x = 0;
			double sum_sq_y = 0;
			double mean_x = x[0];
			double mean_y = y[0];
			double sum_coproduct = 0;
			for (int i = 2; i < x.Length + 1; ++i)
			{
				double w = (i - 1) * 1.0 / i;
				double delta_x = x[i - 1] - mean_x;
				double delta_y = y[i - 1] - mean_y;
				sum_sq_x += delta_x * delta_x * w;
				sum_sq_y += delta_y * delta_y * w;
				sum_coproduct += delta_x * delta_y * w;
				mean_x += delta_x / i;
				mean_y += delta_y / i;
			}
			double pop_sd_x = System.Math.Sqrt(sum_sq_x / x.Length);
			double pop_sd_y = System.Math.Sqrt(sum_sq_y / y.Length);
			double cov_x_y = sum_coproduct / x.Length;
			double denom = pop_sd_x * pop_sd_y;
			if (denom == 0.0)
			{
				return 0.0;
			}
			result = cov_x_y / denom;
			return result;
		}

		/// <summary>
		/// Computes the significance level by approximate randomization, using a
		/// default value of 1000 iterations.
		/// </summary>
		/// <remarks>
		/// Computes the significance level by approximate randomization, using a
		/// default value of 1000 iterations.  See documentation for other version
		/// of method.
		/// </remarks>
		public static double SigLevelByApproxRand(double[] A, double[] B)
		{
			return SigLevelByApproxRand(A, B, 1000);
		}

		/// <summary>
		/// Takes a pair of arrays, A and B, which represent corresponding
		/// outcomes of a pair of random variables: say, results for two different
		/// classifiers on a sequence of inputs.
		/// </summary>
		/// <remarks>
		/// Takes a pair of arrays, A and B, which represent corresponding
		/// outcomes of a pair of random variables: say, results for two different
		/// classifiers on a sequence of inputs.  Returns the estimated
		/// probability that the difference between the means of A and B is not
		/// significant, that is, the significance level.  This is computed by
		/// "approximate randomization".  The test statistic is the absolute
		/// difference between the means of the two arrays.  A randomized test
		/// statistic is computed the same way after initially randomizing the
		/// arrays by swapping each pair of elements with 50% probability.  For
		/// the given number of iterations, we generate a randomized test
		/// statistic and compare it to the actual test statistic.  The return
		/// value is the proportion of iterations in which a randomized test
		/// statistic was found to exceed the actual test statistic.
		/// </remarks>
		/// <param name="A">Outcome of one r.v.</param>
		/// <param name="B">Outcome of another r.v.</param>
		/// <returns>Significance level by randomization</returns>
		public static double SigLevelByApproxRand(double[] A, double[] B, int iterations)
		{
			if (A.Length == 0)
			{
				throw new ArgumentException("Input arrays must not be empty!");
			}
			if (A.Length != B.Length)
			{
				throw new ArgumentException("Input arrays must have equal length!");
			}
			if (iterations <= 0)
			{
				throw new ArgumentException("Number of iterations must be positive!");
			}
			double testStatistic = AbsDiffOfMeans(A, B, false);
			// not randomized
			int successes = 0;
			for (int i = 0; i < iterations; i++)
			{
				double t = AbsDiffOfMeans(A, B, true);
				// randomized
				if (t >= testStatistic)
				{
					successes++;
				}
			}
			return (double)(successes + 1) / (double)(iterations + 1);
		}

		public static double SigLevelByApproxRand(int[] A, int[] B)
		{
			return SigLevelByApproxRand(A, B, 1000);
		}

		public static double SigLevelByApproxRand(int[] A, int[] B, int iterations)
		{
			if (A.Length == 0)
			{
				throw new ArgumentException("Input arrays must not be empty!");
			}
			if (A.Length != B.Length)
			{
				throw new ArgumentException("Input arrays must have equal length!");
			}
			if (iterations <= 0)
			{
				throw new ArgumentException("Number of iterations must be positive!");
			}
			double[] X = new double[A.Length];
			double[] Y = new double[B.Length];
			for (int i = 0; i < A.Length; i++)
			{
				X[i] = A[i];
				Y[i] = B[i];
			}
			return SigLevelByApproxRand(X, Y, iterations);
		}

		public static double SigLevelByApproxRand(bool[] A, bool[] B)
		{
			return SigLevelByApproxRand(A, B, 1000);
		}

		public static double SigLevelByApproxRand(bool[] A, bool[] B, int iterations)
		{
			if (A.Length == 0)
			{
				throw new ArgumentException("Input arrays must not be empty!");
			}
			if (A.Length != B.Length)
			{
				throw new ArgumentException("Input arrays must have equal length!");
			}
			if (iterations <= 0)
			{
				throw new ArgumentException("Number of iterations must be positive!");
			}
			double[] X = new double[A.Length];
			double[] Y = new double[B.Length];
			for (int i = 0; i < A.Length; i++)
			{
				X[i] = (A[i] ? 1.0 : 0.0);
				Y[i] = (B[i] ? 1.0 : 0.0);
			}
			return SigLevelByApproxRand(X, Y, iterations);
		}

		// Returns the absolute difference between the means of arrays A and B.
		// If 'randomize' is true, swaps matched A & B entries with 50% probability
		// Assumes input arrays have equal, non-zero length.
		private static double AbsDiffOfMeans(double[] A, double[] B, bool randomize)
		{
			Random random = new Random();
			double aTotal = 0.0;
			double bTotal = 0.0;
			for (int i = 0; i < A.Length; i++)
			{
				if (randomize && random.NextBoolean())
				{
					aTotal += B[i];
					bTotal += A[i];
				}
				else
				{
					aTotal += A[i];
					bTotal += B[i];
				}
			}
			double aMean = aTotal / A.Length;
			double bMean = bTotal / B.Length;
			return System.Math.Abs(aMean - bMean);
		}

		// PRINTING FUNCTIONS
		public static string ToBinaryString(byte[] b)
		{
			StringBuilder s = new StringBuilder();
			foreach (byte by in b)
			{
				for (int j = 7; j >= 0; j--)
				{
					if ((by & (1 << j)) > 0)
					{
						s.Append('1');
					}
					else
					{
						s.Append('0');
					}
				}
				s.Append(' ');
			}
			return s.ToString();
		}

		public static string ToString(double[] a)
		{
			return ToString(a, null);
		}

		public static string ToString(double[] a, NumberFormat nf)
		{
			if (a == null)
			{
				return null;
			}
			if (a.Length == 0)
			{
				return "[]";
			}
			StringBuilder b = new StringBuilder();
			b.Append('[');
			for (int i = 0; i < a.Length - 1; i++)
			{
				string s;
				if (nf == null)
				{
					s = a[i].ToString();
				}
				else
				{
					s = nf.Format(a[i]);
				}
				b.Append(s);
				b.Append(", ");
			}
			string s_1;
			if (nf == null)
			{
				s_1 = a[a.Length - 1].ToString();
			}
			else
			{
				s_1 = nf.Format(a[a.Length - 1]);
			}
			b.Append(s_1);
			b.Append(']');
			return b.ToString();
		}

		public static string ToString(float[] a)
		{
			return ToString(a, null);
		}

		public static string ToString(float[] a, NumberFormat nf)
		{
			if (a == null)
			{
				return null;
			}
			if (a.Length == 0)
			{
				return "[]";
			}
			StringBuilder b = new StringBuilder();
			b.Append('[');
			for (int i = 0; i < a.Length - 1; i++)
			{
				string s;
				if (nf == null)
				{
					s = a[i].ToString();
				}
				else
				{
					s = nf.Format(a[i]);
				}
				b.Append(s);
				b.Append(", ");
			}
			string s_1;
			if (nf == null)
			{
				s_1 = a[a.Length - 1].ToString();
			}
			else
			{
				s_1 = nf.Format(a[a.Length - 1]);
			}
			b.Append(s_1);
			b.Append(']');
			return b.ToString();
		}

		public static string ToString(int[] a)
		{
			return ToString(a, null);
		}

		public static string ToString(int[] a, NumberFormat nf)
		{
			if (a == null)
			{
				return null;
			}
			if (a.Length == 0)
			{
				return "[]";
			}
			StringBuilder b = new StringBuilder();
			b.Append('[');
			for (int i = 0; i < a.Length - 1; i++)
			{
				string s;
				if (nf == null)
				{
					s = a[i].ToString();
				}
				else
				{
					s = nf.Format(a[i]);
				}
				b.Append(s);
				b.Append(", ");
			}
			string s_1;
			if (nf == null)
			{
				s_1 = a[a.Length - 1].ToString();
			}
			else
			{
				s_1 = nf.Format(a[a.Length - 1]);
			}
			b.Append(s_1);
			b.Append(']');
			return b.ToString();
		}

		public static string ToString(byte[] a)
		{
			return ToString(a, null);
		}

		public static string ToString(byte[] a, NumberFormat nf)
		{
			if (a == null)
			{
				return null;
			}
			if (a.Length == 0)
			{
				return "[]";
			}
			StringBuilder b = new StringBuilder();
			b.Append('[');
			for (int i = 0; i < a.Length - 1; i++)
			{
				string s;
				if (nf == null)
				{
					s = a[i].ToString();
				}
				else
				{
					s = nf.Format(a[i]);
				}
				b.Append(s);
				b.Append(", ");
			}
			string s_1;
			if (nf == null)
			{
				s_1 = a[a.Length - 1].ToString();
			}
			else
			{
				s_1 = nf.Format(a[a.Length - 1]);
			}
			b.Append(s_1);
			b.Append(']');
			return b.ToString();
		}

		public static string ToString(int[][] counts)
		{
			return ToString(counts, null, null, 10, 10, NumberFormat.GetInstance(), false);
		}

		public static string ToString(int[][] counts, object[] rowLabels, object[] colLabels, int labelSize, int cellSize, NumberFormat nf, bool printTotals)
		{
			// first compute row totals and column totals
			if (counts.Length == 0 || counts[0].Length == 0)
			{
				return string.Empty;
			}
			int[] rowTotals = new int[counts.Length];
			int[] colTotals = new int[counts[0].Length];
			// assume it's square
			int total = 0;
			for (int i = 0; i < counts.Length; i++)
			{
				for (int j = 0; j < counts[i].Length; j++)
				{
					rowTotals[i] += counts[i][j];
					colTotals[j] += counts[i][j];
					total += counts[i][j];
				}
			}
			StringBuilder result = new StringBuilder();
			// column labels
			if (colLabels != null)
			{
				result.Append(StringUtils.PadLeft(string.Empty, labelSize));
				// spacing for the row labels!
				for (int j = 0; j < counts[0].Length; j++)
				{
					string s = (colLabels[j] == null ? "null" : colLabels[j].ToString());
					if (s.Length > cellSize - 1)
					{
						s = Sharpen.Runtime.Substring(s, 0, cellSize - 1);
					}
					s = StringUtils.PadLeft(s, cellSize);
					result.Append(s);
				}
				if (printTotals)
				{
					result.Append(StringUtils.PadLeftOrTrim("Total", cellSize));
				}
				result.Append('\n');
			}
			for (int i_1 = 0; i_1 < counts.Length; i_1++)
			{
				// row label
				if (rowLabels != null)
				{
					string s = (rowLabels[i_1] == null ? "null" : rowLabels[i_1].ToString());
					s = StringUtils.PadOrTrim(s, labelSize);
					// left align this guy only
					result.Append(s);
				}
				// value
				for (int j = 0; j < counts[i_1].Length; j++)
				{
					result.Append(StringUtils.PadLeft(nf.Format(counts[i_1][j]), cellSize));
				}
				// the row total
				if (printTotals)
				{
					result.Append(StringUtils.PadLeft(nf.Format(rowTotals[i_1]), cellSize));
				}
				result.Append('\n');
			}
			// the col totals
			if (printTotals)
			{
				result.Append(StringUtils.Pad("Total", labelSize));
				foreach (int colTotal in colTotals)
				{
					result.Append(StringUtils.PadLeft(nf.Format(colTotal), cellSize));
				}
				result.Append(StringUtils.PadLeft(nf.Format(total), cellSize));
			}
			return result.ToString();
		}

		public static string ToString(double[][] counts)
		{
			return ToString(counts, 10, null, null, NumberFormat.GetInstance(), false);
		}

		public static string ToString(double[][] counts, int cellSize, object[] rowLabels, object[] colLabels, NumberFormat nf, bool printTotals)
		{
			if (counts == null)
			{
				return null;
			}
			// first compute row totals and column totals
			double[] rowTotals = new double[counts.Length];
			double[] colTotals = new double[counts[0].Length];
			// assume it's square
			double total = 0.0;
			for (int i = 0; i < counts.Length; i++)
			{
				for (int j = 0; j < counts[i].Length; j++)
				{
					rowTotals[i] += counts[i][j];
					colTotals[j] += counts[i][j];
					total += counts[i][j];
				}
			}
			StringBuilder result = new StringBuilder();
			// column labels
			if (colLabels != null)
			{
				result.Append(StringUtils.PadLeft(string.Empty, cellSize));
				for (int j = 0; j < counts[0].Length; j++)
				{
					string s = colLabels[j].ToString();
					if (s.Length > cellSize - 1)
					{
						s = Sharpen.Runtime.Substring(s, 0, cellSize - 1);
					}
					s = StringUtils.PadLeft(s, cellSize);
					result.Append(s);
				}
				if (printTotals)
				{
					result.Append(StringUtils.PadLeftOrTrim("Total", cellSize));
				}
				result.Append('\n');
			}
			for (int i_1 = 0; i_1 < counts.Length; i_1++)
			{
				// row label
				if (rowLabels != null)
				{
					string s = rowLabels[i_1].ToString();
					s = StringUtils.PadOrTrim(s, cellSize);
					// left align this guy only
					result.Append(s);
				}
				// value
				for (int j = 0; j < counts[i_1].Length; j++)
				{
					result.Append(StringUtils.PadLeft(nf.Format(counts[i_1][j]), cellSize));
				}
				// the row total
				if (printTotals)
				{
					result.Append(StringUtils.PadLeft(nf.Format(rowTotals[i_1]), cellSize));
				}
				result.Append('\n');
			}
			// the col totals
			if (printTotals)
			{
				result.Append(StringUtils.Pad("Total", cellSize));
				foreach (double colTotal in colTotals)
				{
					result.Append(StringUtils.PadLeft(nf.Format(colTotal), cellSize));
				}
				result.Append(StringUtils.PadLeft(nf.Format(total), cellSize));
			}
			return result.ToString();
		}

		public static string ToString(float[][] counts)
		{
			return ToString(counts, 10, null, null, NumberFormat.GetIntegerInstance(), false);
		}

		public static string ToString(float[][] counts, int cellSize, object[] rowLabels, object[] colLabels, NumberFormat nf, bool printTotals)
		{
			// first compute row totals and column totals
			double[] rowTotals = new double[counts.Length];
			double[] colTotals = new double[counts[0].Length];
			// assume it's square
			double total = 0.0;
			for (int i = 0; i < counts.Length; i++)
			{
				for (int j = 0; j < counts[i].Length; j++)
				{
					rowTotals[i] += counts[i][j];
					colTotals[j] += counts[i][j];
					total += counts[i][j];
				}
			}
			StringBuilder result = new StringBuilder();
			// column labels
			if (colLabels != null)
			{
				result.Append(StringUtils.PadLeft(string.Empty, cellSize));
				for (int j = 0; j < counts[0].Length; j++)
				{
					string s = colLabels[j].ToString();
					s = StringUtils.PadLeftOrTrim(s, cellSize);
					result.Append(s);
				}
				if (printTotals)
				{
					result.Append(StringUtils.PadLeftOrTrim("Total", cellSize));
				}
				result.Append('\n');
			}
			for (int i_1 = 0; i_1 < counts.Length; i_1++)
			{
				// row label
				if (rowLabels != null)
				{
					string s = rowLabels[i_1].ToString();
					s = StringUtils.Pad(s, cellSize);
					// left align this guy only
					result.Append(s);
				}
				// value
				for (int j = 0; j < counts[i_1].Length; j++)
				{
					result.Append(StringUtils.PadLeft(nf.Format(counts[i_1][j]), cellSize));
				}
				// the row total
				if (printTotals)
				{
					result.Append(StringUtils.PadLeft(nf.Format(rowTotals[i_1]), cellSize));
				}
				result.Append('\n');
			}
			// the col totals
			if (printTotals)
			{
				result.Append(StringUtils.Pad("Total", cellSize));
				foreach (double colTotal in colTotals)
				{
					result.Append(StringUtils.PadLeft(nf.Format(colTotal), cellSize));
				}
				result.Append(StringUtils.PadLeft(nf.Format(total), cellSize));
			}
			return result.ToString();
		}

		/// <summary>For testing only.</summary>
		/// <param name="args">Ignored</param>
		public static void Main(string[] args)
		{
			Random random = new Random();
			int length = 100;
			double[] A = new double[length];
			double[] B = new double[length];
			double aAvg = 70.0;
			double bAvg = 70.5;
			for (int i = 0; i < length; i++)
			{
				A[i] = aAvg + random.NextGaussian();
				B[i] = bAvg + random.NextGaussian();
			}
			System.Console.Out.WriteLine("A has length " + A.Length + " and mean " + Mean(A));
			System.Console.Out.WriteLine("B has length " + B.Length + " and mean " + Mean(B));
			for (int t = 0; t < 10; t++)
			{
				System.Console.Out.WriteLine("p-value: " + SigLevelByApproxRand(A, B));
			}
		}

		public static int[][] DeepCopy(int[][] counts)
		{
			int[][] result = new int[counts.Length][];
			for (int i = 0; i < counts.Length; i++)
			{
				result[i] = new int[counts[i].Length];
				System.Array.Copy(counts[i], 0, result[i], 0, counts[i].Length);
			}
			return result;
		}

		public static void AddMultInto(double[] a, double[] b, double[] c, double d)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = b[i] + c[i] * d;
			}
		}

		public static void MultiplyInto(double[] a, double[] b, double c)
		{
			for (int i = 0; i < a.Length; i++)
			{
				a[i] = b[i] * c;
			}
		}

		public static double Entropy(double[] probs)
		{
			double e = 0.0;
			foreach (double p in probs)
			{
				if (p != 0.0)
				{
					e -= p * System.Math.Log(p);
				}
			}
			return e;
		}

		/// <exception cref="Edu.Stanford.Nlp.Math.ArrayMath.InvalidElementException"/>
		public static void AssertFinite(double[] vector, string vectorName)
		{
			for (int i = 0; i < vector.Length; i++)
			{
				if (double.IsNaN(vector[i]))
				{
					throw new ArrayMath.InvalidElementException("NaN found in " + vectorName + " element " + i);
				}
				else
				{
					if (double.IsInfinite(vector[i]))
					{
						throw new ArrayMath.InvalidElementException("Infinity found in " + vectorName + " element " + i);
					}
				}
			}
		}

		[System.Serializable]
		public class InvalidElementException : Exception
		{
			private const long serialVersionUID = 1647150702529757545L;

			public InvalidElementException(string s)
				: base(s)
			{
			}
		}
	}
}
