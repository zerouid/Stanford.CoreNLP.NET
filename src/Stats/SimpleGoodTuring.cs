using System;
using System.Collections.Generic;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// Simple Good-Turing smoothing, based on code from Sampson, available at:
	/// ftp://ftp.informatics.susx.ac.uk/pub/users/grs2/SGT.c <p/>
	/// See also http://www.grsampson.net/RGoodTur.html
	/// </summary>
	/// <author>Bill MacCartney (wcmac@cs.stanford.edu)</author>
	public class SimpleGoodTuring
	{
		private const int MinInput = 5;

		private const double ConfidFactor = 1.96;

		private const double Tolerance = 1e-12;

		private int[] r;

		private int[] n;

		private int rows;

		private int bigN = 0;

		private double pZero;

		private double bigNPrime;

		private double slope;

		private double intercept;

		private double[] z;

		private double[] logR;

		private double[] logZ;

		private double[] rStar;

		private double[] p;

		/// <summary>
		/// Each instance of this class encapsulates the computation of the smoothing
		/// for one probability distribution.
		/// </summary>
		/// <remarks>
		/// Each instance of this class encapsulates the computation of the smoothing
		/// for one probability distribution.  The constructor takes two arguments
		/// which are two parallel arrays.  The first is an array of counts, which must
		/// be positive and in ascending order.  The second is an array of
		/// corresponding counts of counts; that is, for each i, n[i] represents the
		/// number of types which occurred with count r[i] in the underlying
		/// collection.  See the documentation for main() for a concrete example.
		/// </remarks>
		public SimpleGoodTuring(int[] r, int[] n)
		{
			// for each bucket, a frequency
			// for each bucket, number of items w that frequency
			// number of frequency buckets
			// total count of all items
			// probability of unseen items
			if (r == null)
			{
				throw new ArgumentException("r must not be null!");
			}
			if (n == null)
			{
				throw new ArgumentException("n must not be null!");
			}
			if (r.Length != n.Length)
			{
				throw new ArgumentException("r and n must have same size!");
			}
			if (r.Length < MinInput)
			{
				throw new ArgumentException("r must have size >= " + MinInput + "!");
			}
			this.r = new int[r.Length];
			this.n = new int[n.Length];
			System.Array.Copy(r, 0, this.r, 0, r.Length);
			// defensive copy
			System.Array.Copy(n, 0, this.n, 0, n.Length);
			// defensive copy
			this.rows = r.Length;
			Compute();
			Validate(Tolerance);
		}

		/// <summary>
		/// Returns the probability allocated to types not seen in the underlying
		/// collection.
		/// </summary>
		public virtual double GetProbabilityForUnseen()
		{
			return pZero;
		}

		/// <summary>
		/// Returns the probabilities allocated to each type, according to their count
		/// in the underlying collection.
		/// </summary>
		/// <remarks>
		/// Returns the probabilities allocated to each type, according to their count
		/// in the underlying collection.  The returned array parallels the arrays
		/// passed in to the constructor.  If the returned array is designated p, then
		/// for all i, p[i] represents the smoothed probability assigned to types which
		/// occurred r[i] times in the underlying collection (where r is the first
		/// argument to the constructor).
		/// </remarks>
		public virtual double[] GetProbabilities()
		{
			return p;
		}

		private void Compute()
		{
			int i;
			int j;
			int next_n;
			double k;
			double x;
			double y;
			bool indiffValsSeen = false;
			z = new double[rows];
			logR = new double[rows];
			logZ = new double[rows];
			rStar = new double[rows];
			p = new double[rows];
			for (j = 0; j < rows; ++j)
			{
				bigN += r[j] * n[j];
			}
			// count all items
			next_n = Row(1);
			pZero = (next_n < 0) ? 0 : n[next_n] / (double)bigN;
			for (j = 0; j < rows; ++j)
			{
				i = (j == 0 ? 0 : r[j - 1]);
				if (j == rows - 1)
				{
					k = (double)(2 * r[j] - i);
				}
				else
				{
					k = (double)r[j + 1];
				}
				z[j] = 2 * n[j] / (k - i);
				logR[j] = Math.Log(r[j]);
				logZ[j] = Math.Log(z[j]);
			}
			FindBestFit();
			for (j = 0; j < rows; ++j)
			{
				y = (r[j] + 1) * Smoothed(r[j] + 1) / Smoothed(r[j]);
				if (Row(r[j] + 1) < 0)
				{
					indiffValsSeen = true;
				}
				if (!indiffValsSeen)
				{
					x = (r[j] + 1) * (next_n = n[Row(r[j] + 1)]) / (double)n[j];
					if (Math.Abs(x - y) <= ConfidFactor * Math.Sqrt(Sq(r[j] + 1.0) * next_n / (Sq((double)n[j])) * (1 + next_n / (double)n[j])))
					{
						indiffValsSeen = true;
					}
					else
					{
						rStar[j] = x;
					}
				}
				if (indiffValsSeen)
				{
					rStar[j] = y;
				}
			}
			bigNPrime = 0.0;
			for (j = 0; j < rows; ++j)
			{
				bigNPrime += n[j] * rStar[j];
			}
			for (j = 0; j < rows; ++j)
			{
				p[j] = (1 - pZero) * rStar[j] / bigNPrime;
			}
		}

		/// <summary>
		/// Returns the index of the bucket having the given frequency, or else -1 if no
		/// bucket has the given frequency.
		/// </summary>
		private int Row(int freq)
		{
			int i = 0;
			while (i < rows && r[i] < freq)
			{
				i++;
			}
			return ((i < rows && r[i] == freq) ? i : -1);
		}

		private void FindBestFit()
		{
			double XYs;
			double Xsquares;
			double meanX;
			double meanY;
			int i;
			XYs = Xsquares = meanX = meanY = 0.0;
			for (i = 0; i < rows; ++i)
			{
				meanX += logR[i];
				meanY += logZ[i];
			}
			meanX /= rows;
			meanY /= rows;
			for (i = 0; i < rows; ++i)
			{
				XYs += (logR[i] - meanX) * (logZ[i] - meanY);
				Xsquares += Sq(logR[i] - meanX);
			}
			slope = XYs / Xsquares;
			intercept = meanY - slope * meanX;
		}

		private double Smoothed(int i)
		{
			return (Math.Exp(intercept + slope * Math.Log(i)));
		}

		private static double Sq(double x)
		{
			return (x * x);
		}

		private void Print()
		{
			int i;
			System.Console.Out.Printf("%6s %6s %8s %8s%n", "r", "n", "p", "p*");
			System.Console.Out.Printf("%6s %6s %8s %8s%n", "----", "----", "----", "----");
			System.Console.Out.Printf("%6d %6d %8.4g %8.4g%n", 0, 0, 0.0, pZero);
			for (i = 0; i < rows; ++i)
			{
				System.Console.Out.Printf("%6d %6d %8.4g %8.4g%n", r[i], n[i], 1.0 * r[i] / bigN, p[i]);
			}
		}

		/// <summary>Ensures that we have a proper probability distribution.</summary>
		private void Validate(double tolerance)
		{
			double sum = pZero;
			for (int i = 0; i < n.Length; i++)
			{
				sum += (n[i] * p[i]);
			}
			double err = 1.0 - sum;
			if (Math.Abs(err) > tolerance)
			{
				throw new InvalidOperationException("ERROR: the probability distribution sums to " + sum);
			}
		}

		// static methods -------------------------------------------------------------
		/// <summary>
		/// Reads from STDIN a sequence of lines, each containing two integers,
		/// separated by whitespace.
		/// </summary>
		/// <remarks>
		/// Reads from STDIN a sequence of lines, each containing two integers,
		/// separated by whitespace.  Returns a pair of int arrays containing the
		/// values read.
		/// </remarks>
		/// <exception cref="System.Exception"/>
		private static int[][] ReadInput()
		{
			IList<int> rVals = new List<int>();
			IList<int> nVals = new List<int>();
			BufferedReader @in = new BufferedReader(new InputStreamReader(Runtime.@in));
			string line;
			while ((line = @in.ReadLine()) != null)
			{
				string[] tokens = line.Trim().Split("\\s+");
				if (tokens.Length != 2)
				{
					throw new Exception("Line doesn't contain two tokens: " + line);
				}
				int r = int.Parse(tokens[0]);
				int n = int.Parse(tokens[1]);
				rVals.Add(r);
				nVals.Add(n);
			}
			@in.Close();
			int[][] result = new int[2][];
			result[0] = IntegerList2IntArray(rVals);
			result[1] = IntegerList2IntArray(nVals);
			return result;
		}

		/// <summary>Helper to readInput().</summary>
		private static int[] IntegerList2IntArray(IList<int> integers)
		{
			int[] ints = new int[integers.Count];
			int i = 0;
			foreach (int integer in integers)
			{
				ints[i++] = integer;
			}
			return ints;
		}

		// main =======================================================================
		/// <summary>
		/// Like Sampson's SGT program, reads data from STDIN and writes results to
		/// STDOUT.
		/// </summary>
		/// <remarks>
		/// Like Sampson's SGT program, reads data from STDIN and writes results to
		/// STDOUT.  The input should contain two integers on each line, separated by
		/// whitespace.  The first integer is a count; the second is a count for that
		/// count.  The input must be sorted in ascending order, and should not contain
		/// 0s.  For example, valid input is: <p/>
		/// <pre>
		/// 1 10
		/// 2 6
		/// 3 4
		/// 5 2
		/// 8 1
		/// </pre>
		/// This represents a collection in which 10 types occur once each, 6 types
		/// occur twice each, 4 types occur 3 times each, 2 types occur 5 times each,
		/// and one type occurs 10 times, for a total count of 52.  This input will
		/// produce the following output: </p>
		/// <pre>
		/// r      n        p       p
		/// ----   ----     ----     ----
		/// 0      0    0.000   0.1923
		/// 1     10  0.01923  0.01203
		/// 2      6  0.03846  0.02951
		/// 3      4  0.05769  0.04814
		/// 5      2  0.09615  0.08647
		/// 8      1   0.1538   0.1448
		/// </pre>
		/// The last column represents the smoothed probabilities, and the first item
		/// in this column represents the probability assigned to unseen items.
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			int[][] input = ReadInput();
			Edu.Stanford.Nlp.Stats.SimpleGoodTuring sgt = new Edu.Stanford.Nlp.Stats.SimpleGoodTuring(input[0], input[1]);
			sgt.Print();
		}
	}
}
