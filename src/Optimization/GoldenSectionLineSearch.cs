using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>A class to do golden section line search.</summary>
	/// <remarks>A class to do golden section line search.  Should it implement Minimizer?  Prob. not.</remarks>
	/// <author>Galen Andrew</author>
	public class GoldenSectionLineSearch : ILineSearcher
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.GoldenSectionLineSearch));

		private static readonly double GoldenRatio = (1.0 + Math.Sqrt(5.0)) / 2.0;

		private static readonly double GoldenSection = (GoldenRatio / (1.0 + GoldenRatio));

		private static bool Verbose = true;

		private IDictionary<double, double> memory = Generics.NewHashMap();

		private bool geometric;

		private double tol;

		private double low;

		private double high;

		public GoldenSectionLineSearch(double tol, double low, double high)
			: this(false, tol, low, high)
		{
		}

		public GoldenSectionLineSearch(double tol, double low, double high, bool verbose)
			: this(false, tol, low, high, verbose)
		{
		}

		public GoldenSectionLineSearch(bool geometric)
			: this(geometric, 1e-4, 1e-2, 10)
		{
		}

		public GoldenSectionLineSearch(bool geometric, double tol, double low, double high)
		{
			//remember where it was called and what were the values
			this.geometric = geometric;
			this.tol = tol;
			this.low = low;
			this.high = high;
		}

		public GoldenSectionLineSearch(bool geometric, double tol, double low, double high, bool verbose)
		{
			this.geometric = geometric;
			this.tol = tol;
			this.low = low;
			this.high = high;
			Edu.Stanford.Nlp.Optimization.GoldenSectionLineSearch.Verbose = verbose;
		}

		private static readonly NumberFormat nf = new DecimalFormat("0.000");

		public virtual double Minimize(IDoubleUnaryOperator function, double tol, double low, double high)
		{
			this.tol = tol;
			this.low = low;
			this.high = high;
			return Minimize(function);
		}

		public virtual double Minimize(IDoubleUnaryOperator function)
		{
			double tol = this.tol;
			double low = this.low;
			double high = this.high;
			// cdm Oct 2006: The code used to do nothing to find or check
			// the validity of an initial
			// bracketing; it just blindly placed the midpoint at the golden ratio
			// I now try to grid search a little in case the function is very flat
			// (RTE contradictions).
			double flow = function.ApplyAsDouble(low);
			double fhigh = function.ApplyAsDouble(high);
			if (Verbose)
			{
				log.Info("Finding min between " + low + " (value: " + flow + ") and " + high + " (value: " + fhigh + ')');
			}
			double mid;
			double oldY;
			bool searchRight;
			if (false)
			{
				// initialize with golden means
				mid = GoldenMean(low, high);
				oldY = function.ApplyAsDouble(mid);
				if (Verbose)
				{
					log.Info("Initially probed at " + mid + ", value is " + oldY);
				}
				if (oldY < flow || oldY < fhigh)
				{
					searchRight = false;
				}
				else
				{
					// Galen had this true; should be false
					mid = GoldenMean(high, low);
					oldY = function.ApplyAsDouble(mid);
					if (Verbose)
					{
						log.Info("Probed at " + mid + ", value is " + oldY);
					}
					searchRight = true;
					if (!(oldY < flow || oldY < fhigh))
					{
						log.Info("Warning: GoldenSectionLineSearch init didn't find slope!!");
					}
				}
			}
			else
			{
				// grid search a little; this case doesn't do geometric differently...
				if (Verbose)
				{
					log.Info("20 point gridsearch for good mid point....");
				}
				double bestPoint = low;
				double bestVal = flow;
				double incr = (high - low) / 22.0;
				for (mid = low + incr; mid < high; mid += incr)
				{
					oldY = function.ApplyAsDouble(mid);
					if (Verbose)
					{
						log.Info("Probed at " + mid + ", value is " + oldY);
					}
					if (oldY < bestVal)
					{
						bestPoint = mid;
						bestVal = oldY;
						if (Verbose)
						{
							log.Info(" [best so far!]");
						}
					}
					if (Verbose)
					{
						log.Info();
					}
				}
				mid = bestPoint;
				oldY = bestVal;
				searchRight = mid < low + (high - low) / 2.0;
				if (oldY < flow && oldY < fhigh)
				{
					if (Verbose)
					{
						log.Info("Found a good mid point at (" + mid + ", " + oldY + ')');
					}
				}
				else
				{
					log.Info("Warning: GoldenSectionLineSearch grid search couldn't find slope!!");
					// revert to initial positioning and pray
					mid = GoldenMean(low, high);
					oldY = function.ApplyAsDouble(mid);
					searchRight = false;
				}
			}
			memory[mid] = oldY;
			while (geometric ? (high / low > 1 + tol) : high - low > tol)
			{
				if (Verbose)
				{
					log.Info("Current low, mid, high: " + nf.Format(low) + ' ' + nf.Format(mid) + ' ' + nf.Format(high));
				}
				double newX = GoldenMean(searchRight ? high : low, mid);
				double newY = function.ApplyAsDouble(newX);
				memory[newX] = newY;
				if (Verbose)
				{
					log.Info("Probed " + (searchRight ? "right" : "left") + " at " + newX + ", value is " + newY);
				}
				if (newY < oldY)
				{
					// keep going in this direction
					if (searchRight)
					{
						low = mid;
					}
					else
					{
						high = mid;
					}
					mid = newX;
					oldY = newY;
				}
				else
				{
					// go the other way
					if (searchRight)
					{
						high = newX;
					}
					else
					{
						low = newX;
					}
					searchRight = !searchRight;
				}
			}
			return mid;
		}

		/// <summary>
		/// dump the
		/// <c>&lt;x,y&gt;</c>
		/// pairs it computed found
		/// </summary>
		public virtual void DumpMemory()
		{
			double[] keys = Sharpen.Collections.ToArray(memory.Keys, new double[memory.Keys.Count]);
			Arrays.Sort(keys);
			foreach (double key in keys)
			{
				log.Info(key + "\t" + memory[key]);
			}
		}

		public virtual void DiscretizeCompute(IDoubleUnaryOperator function, int numPoints, double low, double high)
		{
			double inc = (high - low) / numPoints;
			memory = Generics.NewHashMap();
			for (int i = 0; i < numPoints; i++)
			{
				double x = low + i * inc;
				double y = function.ApplyAsDouble(x);
				memory[x] = y;
				log.Info("for point " + x + '\t' + y);
			}
			DumpMemory();
		}

		/// <summary>The point that is the GOLDEN_SECTION along the way from a to b.</summary>
		/// <remarks>
		/// The point that is the GOLDEN_SECTION along the way from a to b.
		/// a may be less or greater than b, you find the point 60-odd percent
		/// of the way from a to b.
		/// </remarks>
		/// <param name="a">Interval minimum</param>
		/// <param name="b">Interval maximum</param>
		/// <returns>The GOLDEN_SECTION along the way from a to b.</returns>
		private double GoldenMean(double a, double b)
		{
			if (geometric)
			{
				return a * Math.Pow(b / a, GoldenSection);
			}
			else
			{
				return a + (b - a) * GoldenSection;
			}
		}

		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Optimization.GoldenSectionLineSearch min = new Edu.Stanford.Nlp.Optimization.GoldenSectionLineSearch(true, 0.00001, 0.001, 121.0);
			IDoubleUnaryOperator f1 = null;
			System.Console.Out.WriteLine(min.Minimize(f1));
			System.Console.Out.WriteLine();
			min = new Edu.Stanford.Nlp.Optimization.GoldenSectionLineSearch(false, 0.00001, 0.0, 1.0);
			IDoubleUnaryOperator f2 = null;
			System.Console.Out.WriteLine(min.Minimize(f2));
		}
		// end main
	}
}
