using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Math
{
	/// <summary>
	/// The class
	/// <c>SloppyMath</c>
	/// contains methods for performing basic
	/// numeric operations.  In some cases, such as max and min, they cut a few
	/// corners in
	/// the implementation for the sake of efficiency.  In particular, they may
	/// not handle special notions like NaN and -0.0 correctly.  This was the
	/// origin of the class name, but many other methods are just useful
	/// math additions, such as logAdd.  This class just has static math methods.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <version>2003/01/02</version>
	public sealed class SloppyMath
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Math.SloppyMath));

		private SloppyMath()
		{
		}

		// this class is just static methods.
		/// <summary>
		/// Round a double to the nearest integer, via conventional rules
		/// (.5 rounds up, .49 rounds down), and return the result, still as a double.
		/// </summary>
		/// <param name="x">What to round</param>
		/// <returns>The rounded value</returns>
		public static double Round(double x)
		{
			return System.Math.Floor(x + 0.5d);
		}

		/// <summary>
		/// Round a double to the given number of decimal places,
		/// rounding to the nearest value via conventional rules (5 rounds up, 49
		/// rounds down).
		/// </summary>
		/// <remarks>
		/// Round a double to the given number of decimal places,
		/// rounding to the nearest value via conventional rules (5 rounds up, 49
		/// rounds down).
		/// E.g. round(3.1416, 2) == 3.14, round(431.5, -2) == 400,
		/// round(431.5, 0) = 432
		/// </remarks>
		public static double Round(double x, int precision)
		{
			double power = System.Math.Pow(10.0, precision);
			return Round(x * power) / power;
		}

		/* --- extra min and max methods; see also ArrayMath for ones that operate on arrays and varargs */
		/* Note: Math.max(a, b) and Math.min(a, b) do no extra checks when
		* a and b are int or long; they are simply {@code a >= b ? a : b},
		* so you can just call those methods and no extra methods for these
		* are needed here.
		*/
		/// <summary>max() that works on three integers.</summary>
		/// <remarks>
		/// max() that works on three integers.  Like many of the other max() functions in this class,
		/// doesn't perform special checks like NaN or -0.0f to save time.
		/// </remarks>
		/// <returns>The maximum of three int values.</returns>
		public static int Max(int a, int b, int c)
		{
			int ma;
			ma = a;
			if (b > ma)
			{
				ma = b;
			}
			if (c > ma)
			{
				ma = c;
			}
			return ma;
		}

		public static int Max(ICollection<int> vals)
		{
			if (vals.IsEmpty())
			{
				throw new Exception();
			}
			int max = int.MinValue;
			foreach (int i in vals)
			{
				if (i > max)
				{
					max = i;
				}
			}
			return max;
		}

		/// <summary>
		/// Returns the greater of two
		/// <c>float</c>
		/// values.  That is,
		/// the result is the argument closer to positive infinity. If the
		/// arguments have the same value, the result is that same
		/// value.  Does none of the special checks for NaN or -0.0f that
		/// <c>Math.max</c>
		/// does.
		/// </summary>
		/// <param name="a">an argument.</param>
		/// <param name="b">another argument.</param>
		/// <returns>
		/// the larger of
		/// <paramref name="a"/>
		/// and
		/// <paramref name="b"/>
		/// .
		/// </returns>
		public static float Max(float a, float b)
		{
			return (a >= b) ? a : b;
		}

		/// <summary>
		/// Returns the greater of two
		/// <c>double</c>
		/// values.  That
		/// is, the result is the argument closer to positive infinity. If
		/// the arguments have the same value, the result is that same
		/// value.  Does none of the special checks for NaN or -0.0f that
		/// <c>Math.max</c>
		/// does.
		/// </summary>
		/// <param name="a">an argument.</param>
		/// <param name="b">another argument.</param>
		/// <returns>
		/// the larger of
		/// <paramref name="a"/>
		/// and
		/// <paramref name="b"/>
		/// .
		/// </returns>
		public static double Max(double a, double b)
		{
			return (a >= b) ? a : b;
		}

		/// <summary>Returns the minimum of three int values.</summary>
		public static int Min(int a, int b, int c)
		{
			int mi;
			mi = a;
			if (b < mi)
			{
				mi = b;
			}
			if (c < mi)
			{
				mi = c;
			}
			return mi;
		}

		/// <summary>
		/// Returns the smaller of two
		/// <c>float</c>
		/// values.  That is,
		/// the result is the value closer to negative infinity. If the
		/// arguments have the same value, the result is that same
		/// value.  Does none of the special checks for NaN or -0.0f that
		/// <c>Math.max</c>
		/// does.
		/// </summary>
		/// <param name="a">an argument.</param>
		/// <param name="b">another argument.</param>
		/// <returns>
		/// the smaller of
		/// <paramref name="a"/>
		/// and
		/// <c>b.</c>
		/// </returns>
		public static float Min(float a, float b)
		{
			return (a <= b) ? a : b;
		}

		/// <summary>
		/// Returns the smaller of two
		/// <c>double</c>
		/// values.  That
		/// is, the result is the value closer to negative infinity. If the
		/// arguments have the same value, the result is that same
		/// value.  Does none of the special checks for NaN or -0.0f that
		/// <c>Math.max</c>
		/// does.
		/// </summary>
		/// <param name="a">an argument.</param>
		/// <param name="b">another argument.</param>
		/// <returns>
		/// the smaller of
		/// <paramref name="a"/>
		/// and
		/// <paramref name="b"/>
		/// .
		/// </returns>
		public static double Min(double a, double b)
		{
			return (a <= b) ? a : b;
		}

		/// <summary>
		/// Returns a mod where the sign of the answer is the same as the sign of the second
		/// argument.
		/// </summary>
		/// <remarks>
		/// Returns a mod where the sign of the answer is the same as the sign of the second
		/// argument.  This is how languages like Python do it. Helpful for array accesses.
		/// </remarks>
		/// <param name="num">The number</param>
		/// <param name="modulus">The modulus</param>
		/// <returns>num mod modulus, where the sign of the answer is the same as the sign of modulus</returns>
		public static int PythonMod(int num, int modulus)
		{
			// This is: num < 0 ? num % modulus + modulus: num % modulus, but avoids a test-and-branch
			return (num % modulus + modulus) % modulus;
		}

		/// <returns>
		/// an approximation of the log of the Gamma function of x.  Laczos Approximation
		/// Reference: Numerical Recipes in C
		/// http://www.library.cornell.edu/nr/cbookcpdf.html
		/// from www.cs.berkeley.edu/~milch/blog/versions/blog-0.1.3/blog/distrib
		/// </returns>
		public static double Lgamma(double x)
		{
			double[] cof = new double[] { 76.18009172947146, -86.50532032941677, 24.01409824083091, -1.231739572450155, 0.1208650973866179e-2, -0.5395239384953e-5 };
			double xxx = x;
			double tmp = x + 5.5;
			tmp -= ((x + 0.5) * System.Math.Log(tmp));
			double ser = 1.000000000190015;
			for (int j = 0; j < 6; j++)
			{
				xxx++;
				ser += cof[j] / xxx;
			}
			return -tmp + System.Math.Log(2.5066282746310005 * ser / x);
		}

		/// <summary>
		/// Returns true if the argument is a "dangerous" double to have
		/// around, namely one that is infinite, NaN or zero.
		/// </summary>
		public static bool IsDangerous(double d)
		{
			return double.IsInfinite(d) || double.IsNaN(d) || d == 0.0;
		}

		/// <summary>
		/// Returns true if the argument is a "very dangerous" double to have
		/// around, namely one that is infinite or NaN.
		/// </summary>
		public static bool IsVeryDangerous(double d)
		{
			return double.IsInfinite(d) || double.IsNaN(d);
		}

		public static bool IsCloseTo(double a, double b)
		{
			if (a > b)
			{
				return (a - b) < 1e-4;
			}
			else
			{
				return (b - a) < 1e-4;
			}
		}

		/// <summary>
		/// If a difference is bigger than this in log terms, then the sum or
		/// difference of them will just be the larger (to 12 or so decimal
		/// places for double, and 7 or 8 for float).
		/// </summary>
		internal const double Logtolerance = 30.0;

		internal const float LogtoleranceF = 20.0f;

		/// <summary>Approximation to gamma function.</summary>
		/// <remarks>
		/// Approximation to gamma function.  See e.g., http://www.rskey.org/CMS/index.php/the-library/11 .
		/// Fairly accurate, especially for n greater than 8.
		/// </remarks>
		public static double Gamma(double n)
		{
			return System.Math.Sqrt(2.0 * System.Math.Pi / n) * System.Math.Pow((n / System.Math.E) * System.Math.Sqrt(n * System.Math.Sinh((1.0 / n) + (1 / (810 * System.Math.Pow(n, 6))))), n);
		}

		/// <summary>Convenience method for log to a different base.</summary>
		public static double Log(double num, double @base)
		{
			return System.Math.Log(num) / System.Math.Log(@base);
		}

		/// <summary>
		/// Returns the log of the sum of two numbers, which are
		/// themselves input in log form.
		/// </summary>
		/// <remarks>
		/// Returns the log of the sum of two numbers, which are
		/// themselves input in log form.  This uses natural logarithms.
		/// Reasonable care is taken to do this as efficiently as possible
		/// (under the assumption that the numbers might differ greatly in
		/// magnitude), with high accuracy, and without numerical overflow.
		/// Also, handle correctly the case of arguments being -Inf (e.g.,
		/// probability 0).
		/// </remarks>
		/// <param name="lx">First number, in log form</param>
		/// <param name="ly">Second number, in log form</param>
		/// <returns>
		/// 
		/// <c>log(exp(lx) + exp(ly))</c>
		/// </returns>
		public static float LogAdd(float lx, float ly)
		{
			float max;
			float negDiff;
			if (lx > ly)
			{
				max = lx;
				negDiff = ly - lx;
			}
			else
			{
				max = ly;
				negDiff = lx - ly;
			}
			if (max == double.NegativeInfinity)
			{
				return max;
			}
			else
			{
				if (negDiff < -LogtoleranceF)
				{
					return max;
				}
				else
				{
					return max + (float)System.Math.Log(1.0 + System.Math.Exp(negDiff));
				}
			}
		}

		/// <summary>
		/// Returns the log of the sum of two numbers, which are
		/// themselves input in log form.
		/// </summary>
		/// <remarks>
		/// Returns the log of the sum of two numbers, which are
		/// themselves input in log form.  This uses natural logarithms.
		/// Reasonable care is taken to do this as efficiently as possible
		/// (under the assumption that the numbers might differ greatly in
		/// magnitude), with high accuracy, and without numerical overflow.
		/// Also, handle correctly the case of arguments being -Inf (e.g.,
		/// probability 0).
		/// </remarks>
		/// <param name="lx">First number, in log form</param>
		/// <param name="ly">Second number, in log form</param>
		/// <returns>
		/// 
		/// <c>log(exp(lx) + exp(ly))</c>
		/// </returns>
		public static double LogAdd(double lx, double ly)
		{
			double max;
			double negDiff;
			if (lx > ly)
			{
				max = lx;
				negDiff = ly - lx;
			}
			else
			{
				max = ly;
				negDiff = lx - ly;
			}
			if (max == double.NegativeInfinity)
			{
				return max;
			}
			else
			{
				if (negDiff < -Logtolerance)
				{
					return max;
				}
				else
				{
					return max + System.Math.Log(1.0 + System.Math.Exp(negDiff));
				}
			}
		}

		/// <summary>Computes n choose k in an efficient way.</summary>
		/// <remarks>
		/// Computes n choose k in an efficient way.  Works with
		/// k == 0 or k == n but undefined if k &lt; 0 or k &gt; n
		/// </remarks>
		/// <returns>fact(n) / fact(k) * fact(n-k)</returns>
		public static int NChooseK(int n, int k)
		{
			k = System.Math.Min(k, n - k);
			if (k == 0)
			{
				return 1;
			}
			int accum = n;
			for (int i = 1; i < k; i++)
			{
				accum *= (n - i);
				accum /= i;
			}
			return accum / k;
		}

		/// <summary>
		/// Returns an approximation to Math.pow(a,b) that is ~27x faster
		/// with a margin of error possibly around ~10%.
		/// </summary>
		/// <remarks>
		/// Returns an approximation to Math.pow(a,b) that is ~27x faster
		/// with a margin of error possibly around ~10%.  From
		/// http://martin.ankerl.com/2007/10/04/optimized-pow-approximation-for-java-and-c-c/
		/// </remarks>
		public static double Pow(double a, double b)
		{
			int x = (int)(double.DoubleToLongBits(a) >> 32);
			int y = (int)(b * (x - 1072632447) + 1072632447);
			return double.LongBitsToDouble(((long)y) << 32);
		}

		/// <summary>
		/// Exponentiation like we learned in grade school:
		/// multiply b by itself e times.
		/// </summary>
		/// <remarks>
		/// Exponentiation like we learned in grade school:
		/// multiply b by itself e times.  Uses power of two trick.
		/// e must be nonnegative!!!  no checking!!!  For e &lt;= 0,
		/// the exponent is treated as 0, and 1 is returned.  0^0 also
		/// returns 1. Biased to do quickly small exponents, like the CRF needs.
		/// Note that some code claims you can get more speed ups with special cases:
		/// http://sourceforge.net/p/jafama/code/ci/master/tree/src/net/jafama/FastMath.java
		/// but I couldn't verify any gains beyond special casing 2. May depend on workload.
		/// </remarks>
		/// <param name="b">base</param>
		/// <param name="e">exponent</param>
		/// <returns>b^e</returns>
		public static int IntPow(int b, int e)
		{
			if (e <= 1)
			{
				if (e == 1)
				{
					return b;
				}
				else
				{
					return 1;
				}
			}
			else
			{
				// this is also what you get for e < 0 !
				if (e == 2)
				{
					return b * b;
				}
				else
				{
					int result = 1;
					while (e > 0)
					{
						if ((e & 1) != 0)
						{
							result *= b;
						}
						b *= b;
						e >>= 1;
					}
					return result;
				}
			}
		}

		/// <summary>
		/// Exponentiation like we learned in grade school:
		/// multiply b by itself e times.
		/// </summary>
		/// <remarks>
		/// Exponentiation like we learned in grade school:
		/// multiply b by itself e times.  Uses power of two trick.
		/// e must be nonnegative!!!  no checking!!!
		/// </remarks>
		/// <param name="b">base</param>
		/// <param name="e">exponent</param>
		/// <returns>b^e</returns>
		public static float IntPow(float b, int e)
		{
			float result = 1.0f;
			float currPow = b;
			while (e > 0)
			{
				if ((e & 1) != 0)
				{
					result *= currPow;
				}
				currPow *= currPow;
				e >>= 1;
			}
			return result;
		}

		/// <summary>
		/// Exponentiation like we learned in grade school:
		/// multiply b by itself e times.
		/// </summary>
		/// <remarks>
		/// Exponentiation like we learned in grade school:
		/// multiply b by itself e times.  Uses power of two trick.
		/// e must be nonnegative!!!  no checking!!!
		/// </remarks>
		/// <param name="b">base</param>
		/// <param name="e">exponent</param>
		/// <returns>b^e</returns>
		public static double IntPow(double b, int e)
		{
			double result = 1.0;
			double currPow = b;
			while (e > 0)
			{
				if ((e & 1) != 0)
				{
					result *= currPow;
				}
				currPow *= currPow;
				e >>= 1;
			}
			return result;
		}

		/// <summary>Find a hypergeometric distribution.</summary>
		/// <remarks>
		/// Find a hypergeometric distribution.  This uses exact math, trying
		/// fairly hard to avoid numeric overflow by interleaving
		/// multiplications and divisions.
		/// (To do: make it even better at avoiding overflow, by using loops
		/// that will do either a multiple or divide based on the size of the
		/// intermediate result.)
		/// </remarks>
		/// <param name="k">The number of black balls drawn</param>
		/// <param name="n">The total number of balls</param>
		/// <param name="r">The number of black balls</param>
		/// <param name="m">The number of balls drawn</param>
		/// <returns>The hypergeometric value</returns>
		public static double Hypergeometric(int k, int n, int r, int m)
		{
			if (k < 0 || r > n || m > n || n <= 0 || m < 0 || r < 0)
			{
				throw new ArgumentException("Invalid hypergeometric");
			}
			// exploit symmetry of problem
			if (m > n / 2)
			{
				m = n - m;
				k = r - k;
			}
			if (r > n / 2)
			{
				r = n - r;
				k = m - k;
			}
			if (m > r)
			{
				int temp = m;
				m = r;
				r = temp;
			}
			// now we have that k <= m <= r <= n/2
			if (k < (m + r) - n || k > m)
			{
				return 0.0;
			}
			// Do limit cases explicitly
			// It's unclear whether this is a good idea.  I put it in fearing
			// numerical errors when the numbers seemed off, but actually there
			// was a bug in the Fisher's exact routine.
			if (r == n)
			{
				if (k == m)
				{
					return 1.0;
				}
				else
				{
					return 0.0;
				}
			}
			else
			{
				if (r == n - 1)
				{
					if (k == m)
					{
						return (n - m) / (double)n;
					}
					else
					{
						if (k == m - 1)
						{
							return m / (double)n;
						}
						else
						{
							return 0.0;
						}
					}
				}
				else
				{
					if (m == 1)
					{
						if (k == 0)
						{
							return (n - r) / (double)n;
						}
						else
						{
							if (k == 1)
							{
								return r / (double)n;
							}
							else
							{
								return 0.0;
							}
						}
					}
					else
					{
						if (m == 0)
						{
							if (k == 0)
							{
								return 1.0;
							}
							else
							{
								return 0.0;
							}
						}
						else
						{
							if (k == 0)
							{
								double ans = 1.0;
								for (int m0 = 0; m0 < m; m0++)
								{
									ans *= ((n - r) - m0);
									ans /= (n - m0);
								}
								return ans;
							}
						}
					}
				}
			}
			double ans_1 = 1.0;
			// do (n-r)x...x((n-r)-((m-k)-1))/n x...x (n-((m-k-1)))
			// leaving rest of denominator to get to multiply by (n-(m-1))
			// that's k things which goes into next loop
			for (int nr = n - r; nr > (n - r) - (m - k); nr--, n0--)
			{
				// System.out.println("Multiplying by " + nr);
				ans_1 *= nr;
				// System.out.println("Dividing by " + n0);
				ans_1 /= n0;
			}
			// System.out.println("Done phase 1");
			for (int k0 = 0; k0 < k; k0++)
			{
				ans_1 *= (m - k0);
				// System.out.println("Multiplying by " + (m-k0));
				ans_1 /= ((n - (m - k0)) + 1);
				// System.out.println("Dividing by " + ((n-(m+k0)+1)));
				ans_1 *= (r - k0);
				// System.out.println("Multiplying by " + (r-k0));
				ans_1 /= (k0 + 1);
			}
			// System.out.println("Dividing by " + (k0+1));
			return ans_1;
		}

		/// <summary>Find a one tailed exact binomial test probability.</summary>
		/// <remarks>
		/// Find a one tailed exact binomial test probability.  Finds the chance
		/// of this or a higher result
		/// </remarks>
		/// <param name="k">number of successes</param>
		/// <param name="n">Number of trials</param>
		/// <param name="p">Probability of a success</param>
		public static double ExactBinomial(int k, int n, double p)
		{
			double total = 0.0;
			for (int m = k; m <= n; m++)
			{
				double nChooseM = 1.0;
				for (int r = 1; r <= m; r++)
				{
					nChooseM *= (n - r) + 1;
					nChooseM /= r;
				}
				// System.out.println(n + " choose " + m + " is " + nChooseM);
				// System.out.println("prob contribution is " +
				//	       (nChooseM * Math.pow(p, m) * Math.pow(1.0-p, n - m)));
				total += nChooseM * System.Math.Pow(p, m) * System.Math.Pow(1.0 - p, n - m);
			}
			return total;
		}

		/// <summary>Find a one-tailed Fisher's exact probability.</summary>
		/// <remarks>
		/// Find a one-tailed Fisher's exact probability.  Chance of having seen
		/// this or a more extreme departure from what you would have expected
		/// given independence.  I.e., k &ge; the value passed in.
		/// Warning: this was done just for collocations, where you are
		/// concerned with the case of k being larger than predicted.  It doesn't
		/// correctly handle other cases, such as k being smaller than expected.
		/// </remarks>
		/// <param name="k">The number of black balls drawn</param>
		/// <param name="n">The total number of balls</param>
		/// <param name="r">The number of black balls</param>
		/// <param name="m">The number of balls drawn</param>
		/// <returns>The Fisher's exact p-value</returns>
		public static double OneTailedFishersExact(int k, int n, int r, int m)
		{
			if (k < 0 || k < (m + r) - n || k > r || k > m || r > n || m > n)
			{
				throw new ArgumentException("Invalid Fisher's exact: " + "k=" + k + " n=" + n + " r=" + r + " m=" + m + " k<0=" + (k < 0) + " k<(m+r)-n=" + (k < (m + r) - n) + " k>r=" + (k > r) + " k>m=" + (k > m) + " r>n=" + (r > n) + "m>n=" + (m > n));
			}
			// exploit symmetry of problem
			if (m > n / 2)
			{
				m = n - m;
				k = r - k;
			}
			if (r > n / 2)
			{
				r = n - r;
				k = m - k;
			}
			if (m > r)
			{
				int temp = m;
				m = r;
				r = temp;
			}
			// now we have that k <= m <= r <= n/2
			double total = 0.0;
			if (k > m / 2)
			{
				// sum from k to m
				for (int k0 = k; k0 <= m; k0++)
				{
					// System.out.println("Calling hypg(" + k0 + "; " + n +
					// 		   ", " + r + ", " + m + ")");
					total += Edu.Stanford.Nlp.Math.SloppyMath.Hypergeometric(k0, n, r, m);
				}
			}
			else
			{
				// sum from max(0, (m+r)-n) to k-1, and then subtract from 1
				int min = System.Math.Max(0, (m + r) - n);
				for (int k0 = min; k0 < k; k0++)
				{
					// System.out.println("Calling hypg(" + k0 + "; " + n +
					// 		   ", " + r + ", " + m + ")");
					total += Edu.Stanford.Nlp.Math.SloppyMath.Hypergeometric(k0, n, r, m);
				}
				total = 1.0 - total;
			}
			return total;
		}

		/// <summary>Find a 2x2 chi-square value.</summary>
		/// <remarks>
		/// Find a 2x2 chi-square value.
		/// Note: could do this more neatly using simplified formula for 2x2 case.
		/// </remarks>
		/// <param name="k">The number of black balls drawn</param>
		/// <param name="n">The total number of balls</param>
		/// <param name="r">The number of black balls</param>
		/// <param name="m">The number of balls drawn</param>
		/// <returns>The Fisher's exact p-value</returns>
		public static double ChiSquare2by2(int k, int n, int r, int m)
		{
			int[][] cg = new int[][] { new int[] { k, r - k }, new int[] { m - k, n - (k + (r - k) + (m - k)) } };
			int[] cgr = new int[] { r, n - r };
			int[] cgc = new int[] { m, n - m };
			double total = 0.0;
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					double exp = (double)cgr[i] * cgc[j] / n;
					total += (cg[i][j] - exp) * (cg[i][j] - exp) / exp;
				}
			}
			return total;
		}

		/// <summary>Compute the sigmoid function with mean zero.</summary>
		/// <remarks>
		/// Compute the sigmoid function with mean zero.
		/// Care is taken to compute an accurate answer without
		/// numerical overflow. (Added by rajatr)
		/// </remarks>
		/// <param name="x">Point to compute sigmoid at.</param>
		/// <returns>Value of the sigmoid, given by 1/(1+exp(-x))</returns>
		public static double Sigmoid(double x)
		{
			if (x < 0)
			{
				double num = System.Math.Exp(x);
				return num / (1.0 + num);
			}
			else
			{
				double den = 1.0 + System.Math.Exp(-x);
				return 1.0 / den;
			}
		}

		private static float[] acosCache;

		// = null;
		/// <summary>Compute acos very quickly by directly looking up the value.</summary>
		/// <param name="cosValue">The cosine of the angle to fine.</param>
		/// <returns>The angle corresponding to the cosine value.</returns>
		/// <exception cref="System.ArgumentException">if cosValue is not between -1 and 1</exception>
		public static double Acos(double cosValue)
		{
			if (cosValue < -1.0 || cosValue > 1.0)
			{
				throw new ArgumentException("Cosine is not between -1 and 1: " + cosValue);
			}
			int numSamples = 10000;
			if (acosCache == null)
			{
				acosCache = new float[numSamples + 1];
				for (int i = 0; i <= numSamples; ++i)
				{
					double x = 2.0 / ((double)numSamples) * ((double)i) - 1.0;
					acosCache[i] = (float)System.Math.Acos(x);
				}
			}
			int i_1 = ((int)(((cosValue + 1.0) / 2.0) * ((double)numSamples)));
			return acosCache[i_1];
		}

		public static double Poisson(int x, double lambda)
		{
			if (x < 0 || lambda <= 0.0)
			{
				throw new Exception("Bad arguments: " + x + " and " + lambda);
			}
			double p = (System.Math.Exp(-lambda) * System.Math.Pow(lambda, x)) / Factorial(x);
			if (double.IsInfinite(p) || p <= 0.0)
			{
				throw new Exception(System.Math.Exp(-lambda) + " " + System.Math.Pow(lambda, x) + ' ' + Factorial(x));
			}
			return p;
		}

		/// <summary>Uses floating point so that it can represent the really big numbers that come up.</summary>
		/// <param name="x">Argument to take factorial of</param>
		/// <returns>Factorial of argument</returns>
		public static double Factorial(int x)
		{
			double result = 1.0;
			for (int i = x; i > 1; i--)
			{
				result *= i;
			}
			return result;
		}

		/// <summary>Taken from http://nerds-central.blogspot.com/2011/05/high-speed-parse-double-for-jvm.html</summary>
		private static readonly double[] exps = new double[617];

		static SloppyMath()
		{
			for (int i = -308; i < 308; ++i)
			{
				string toParse = "1.0e" + i;
				exps[(i + 308)] = double.ParseDouble("1.0e" + i);
			}
		}

		/// <summary>Taken from http://nerds-central.blogspot.com/2011/05/high-speed-parse-double-for-jvm.html</summary>
		public static double ParseDouble(bool negative, long mantissa, int exponent)
		{
			// Do this with no locals other than the arguments to make it stupid easy
			// for the JIT compiler to inline the code.
			int e = -16;
			return (negative ? -1.0 : 1.0) * (((double)mantissa) * exps[(e + 308)]) * exps[(exponent + 308)];
		}

		/// <summary>Segment a double into a mantissa and exponent.</summary>
		public static Triple<bool, long, int> SegmentDouble(double d)
		{
			if (double.IsInfinite(d) || double.IsNaN(d))
			{
				throw new ArgumentException("Cannot handle weird double: " + d);
			}
			bool negative = d < 0;
			d = System.Math.Abs(d);
			int exponent = 0;
			while (d >= 10.0)
			{
				exponent += 1;
				d = d / 10.0;
			}
			while (d < 1.0)
			{
				exponent -= 1;
				d = d * 10.0;
			}
			return Triple.MakeTriple(negative, (long)(d * 10000000000000000.0), exponent);
		}

		/// <summary>
		/// From http://nadeausoftware.com/articles/2009/08/java_tip_how_parse_integers_quickly
		/// Parse an integer very quickly, without sanity checks.
		/// </summary>
		public static long ParseInt(string s)
		{
			// Check for a sign.
			long num = 0;
			long sign = -1;
			int len = s.Length;
			char ch = s[0];
			if (ch == '-')
			{
				sign = 1;
			}
			else
			{
				long d = ch - '0';
				num = -d;
			}
			// Build the number.
			long max = (sign == -1) ? -long.MaxValue : long.MinValue;
			long multmax = max / 10;
			int i = 1;
			while (i < len)
			{
				long d = s[i++] - '0';
				num *= 10;
				num -= d;
			}
			return sign * num;
		}

		/// <summary>
		/// Tests the hypergeometric distribution code, or other functions
		/// provided in this module.
		/// </summary>
		/// <param name="args">
		/// Either none, and the log add routines are tested, or the
		/// following 4 arguments: k (cell), n (total), r (row), m (col)
		/// </param>
		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				log.Info("Usage: java edu.stanford.nlp.math.SloppyMath " + "[-logAdd|-fishers k n r m|-binomial r n p");
			}
			else
			{
				if (args[0].Equals("-logAdd"))
				{
					System.Console.Out.WriteLine("Log adds of neg infinity numbers, etc.");
					System.Console.Out.WriteLine("(logs) -Inf + -Inf = " + LogAdd(double.NegativeInfinity, double.NegativeInfinity));
					System.Console.Out.WriteLine("(logs) -Inf + -7 = " + LogAdd(double.NegativeInfinity, -7.0));
					System.Console.Out.WriteLine("(logs) -7 + -Inf = " + LogAdd(-7.0, double.NegativeInfinity));
					System.Console.Out.WriteLine("(logs) -50 + -7 = " + LogAdd(-50.0, -7.0));
					System.Console.Out.WriteLine("(logs) -11 + -7 = " + LogAdd(-11.0, -7.0));
					System.Console.Out.WriteLine("(logs) -7 + -11 = " + LogAdd(-7.0, -11.0));
					System.Console.Out.WriteLine("real 1/2 + 1/2 = " + LogAdd(System.Math.Log(0.5), System.Math.Log(0.5)));
				}
				else
				{
					if (args[0].Equals("-fishers"))
					{
						int k = System.Convert.ToInt32(args[1]);
						int n = System.Convert.ToInt32(args[2]);
						int r = System.Convert.ToInt32(args[3]);
						int m = System.Convert.ToInt32(args[4]);
						double ans = Edu.Stanford.Nlp.Math.SloppyMath.Hypergeometric(k, n, r, m);
						System.Console.Out.WriteLine("hypg(" + k + "; " + n + ", " + r + ", " + m + ") = " + ans);
						ans = Edu.Stanford.Nlp.Math.SloppyMath.OneTailedFishersExact(k, n, r, m);
						System.Console.Out.WriteLine("1-tailed Fisher's exact(" + k + "; " + n + ", " + r + ", " + m + ") = " + ans);
						double ansChi = Edu.Stanford.Nlp.Math.SloppyMath.ChiSquare2by2(k, n, r, m);
						System.Console.Out.WriteLine("chiSquare(" + k + "; " + n + ", " + r + ", " + m + ") = " + ansChi);
						System.Console.Out.WriteLine("Swapping arguments should give same hypg:");
						ans = Edu.Stanford.Nlp.Math.SloppyMath.Hypergeometric(k, n, r, m);
						System.Console.Out.WriteLine("hypg(" + k + "; " + n + ", " + m + ", " + r + ") = " + ans);
						int othrow = n - m;
						int othcol = n - r;
						int cell12 = m - k;
						int cell21 = r - k;
						int cell22 = othrow - (r - k);
						ans = Edu.Stanford.Nlp.Math.SloppyMath.Hypergeometric(cell12, n, othcol, m);
						System.Console.Out.WriteLine("hypg(" + cell12 + "; " + n + ", " + othcol + ", " + m + ") = " + ans);
						ans = Edu.Stanford.Nlp.Math.SloppyMath.Hypergeometric(cell21, n, r, othrow);
						System.Console.Out.WriteLine("hypg(" + cell21 + "; " + n + ", " + r + ", " + othrow + ") = " + ans);
						ans = Edu.Stanford.Nlp.Math.SloppyMath.Hypergeometric(cell22, n, othcol, othrow);
						System.Console.Out.WriteLine("hypg(" + cell22 + "; " + n + ", " + othcol + ", " + othrow + ") = " + ans);
					}
					else
					{
						if (args[0].Equals("-binomial"))
						{
							int k = System.Convert.ToInt32(args[1]);
							int n = System.Convert.ToInt32(args[2]);
							double p = double.ParseDouble(args[3]);
							double ans = Edu.Stanford.Nlp.Math.SloppyMath.ExactBinomial(k, n, p);
							System.Console.Out.WriteLine("Binomial p(X >= " + k + "; " + n + ", " + p + ") = " + ans);
						}
						else
						{
							log.Info("Unknown option: " + args[0]);
						}
					}
				}
			}
		}
	}
}
