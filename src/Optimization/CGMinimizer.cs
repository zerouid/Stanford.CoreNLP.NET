using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// Conjugate-gradient implementation based on the code in Numerical
	/// Recipes in C.
	/// </summary>
	/// <remarks>
	/// Conjugate-gradient implementation based on the code in Numerical
	/// Recipes in C.  (See p. 423 and others.)  As of now, it requires a
	/// differentiable function (DiffFunction) as input.  Equality
	/// constraints are supported; inequality constraints may soon be
	/// added.
	/// <p>
	/// The basic way to use the minimizer is with a null constructor, then
	/// the simple minimize method:
	/// <p>
	/// <p>
	/// <c>Minimizer cgm = new CGMinimizer();</c>
	/// <br />
	/// <c>DiffFunction df = new SomeDiffFunction();</c>
	/// <br />
	/// <c>double tol = 1e-4;</c>
	/// <br />
	/// <c>double[] initial = getInitialGuess();</c>
	/// <br />
	/// <c>double[] minimum = cgm.minimize(df,tol,initial);</c>
	/// </remarks>
	/// <author><a href="mailto:klein@cs.stanford.edu">Dan Klein</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public class CGMinimizer : IMinimizer<IDiffFunction>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.CGMinimizer));

		private static readonly NumberFormat nf = new DecimalFormat("0.000E0");

		private readonly IFunction monitor;

		[System.NonSerialized]
		private CallbackFunction iterationCallbackFunction;

		private readonly bool silent;

		private const int numToPrint = 5;

		private const bool simpleGD = false;

		private const bool checkSimpleGDConvergence = true;

		private const bool verbose = false;

		private const int Itmax = 2000;

		private const double Eps = 1.0e-30;

		private const int resetFrequency = 10;

		// a different value is used in dbrent(); made bigger
		private static double[] CopyArray(double[] a)
		{
			return Arrays.CopyOf(a, a.Length);
		}

		//  private static String arrayToString(double[] x) {
		//    return arrayToString(x, x.length);
		//  }
		private static string ArrayToString(double[] x, int num)
		{
			StringBuilder sb = new StringBuilder("(");
			if (num > x.Length)
			{
				num = x.Length;
			}
			for (int j = 0; j < num; j++)
			{
				sb.Append(x[j]);
				if (j != x.Length - 1)
				{
					sb.Append(", ");
				}
			}
			if (num < x.Length)
			{
				sb.Append("...");
			}
			sb.Append(')');
			return sb.ToString();
		}

		private static double Fabs(double x)
		{
			if (x < 0)
			{
				return -x;
			}
			return x;
		}

		private static double Fmax(double x, double y)
		{
			if (x < y)
			{
				return y;
			}
			return x;
		}

		//  private static double fmin(double x, double y) {
		//    if (x>y)
		//      return y;
		//    return x;
		//  }
		private static double Sign(double x, double y)
		{
			if (y >= 0.0)
			{
				return Fabs(x);
			}
			return -Fabs(x);
		}

		internal class OneDimDiffFunction
		{
			private IDiffFunction function;

			private double[] initial;

			private double[] direction;

			private double[] tempVector;

			//  private static double arrayMax(double[] x) {
			//    double max = Double.NEGATIVE_INFINITY;
			//    for (int i=0; i<x.length; i++) {
			//      if (max < x[i])
			//	max = x[i];
			//    }
			//    return max;
			//  }
			//
			//  private static int arrayArgMax(double[] x) {
			//    double max = Double.NEGATIVE_INFINITY;
			//    int index = -1;
			//    for (int i=0; i<x.length; i++) {
			//      if (max < x[i]) {
			//	max = x[i];
			//	index = i;
			//      }
			//    }
			//    return index;
			//  }
			//
			//  private static double arrayMin(double[] x) {
			//    double min = Double.POSITIVE_INFINITY;
			//    for (int i=0; i<x.length; i++) {
			//      if (min > x[i])
			//	min = x[i];
			//    }
			//    return min;
			//  }
			//
			//  private static int arrayArgMin(double[] x) {
			//    double min = Double.POSITIVE_INFINITY;
			//    int index = -1;
			//    for (int i=0; i<x.length; i++) {
			//      if (min > x[i]) {
			//	min = x[i];
			//	index = i;
			//      }
			//    }
			//    return index;
			//  }
			private double[] VectorOf(double x)
			{
				for (int j = 0; j < initial.Length; j++)
				{
					tempVector[j] = initial[j] + x * direction[j];
				}
				//log.info("Tmp "+arrayToString(tempVector,10));
				//log.info("Dir "+arrayToString(direction,10));
				return tempVector;
			}

			internal virtual double ValueAt(double x)
			{
				return function.ValueAt(VectorOf(x));
			}

			internal virtual double DerivativeAt(double x)
			{
				double[] g = function.DerivativeAt(VectorOf(x));
				double d = 0.0;
				for (int j = 0; j < g.Length; j++)
				{
					d += g[j] * direction[j];
				}
				return d;
			}

			internal OneDimDiffFunction(IDiffFunction function, double[] initial, double[] direction)
			{
				this.function = function;
				this.initial = CopyArray(initial);
				this.direction = CopyArray(direction);
				this.tempVector = new double[function.DomainDimension()];
			}
		}

		private const double Gold = 1.618034;

		private const double Glimit = 100.0;

		private const double Tiny = 1.0e-20;

		// end class OneDimDiffFunction
		// constants
		private static CGMinimizer.Triple Mnbrak(CGMinimizer.Triple abc, CGMinimizer.OneDimDiffFunction function)
		{
			// inputs
			double ax = abc.a;
			double fa = function.ValueAt(ax);
			double bx = abc.b;
			double fb = function.ValueAt(bx);
			if (fb > fa)
			{
				// swap
				double temp = fa;
				fa = fb;
				fb = temp;
				temp = ax;
				ax = bx;
				bx = temp;
			}
			// guess cx
			double cx = bx + Gold * (bx - ax);
			double fc = function.ValueAt(cx);
			// loop until we get a bracket
			while (fb > fc)
			{
				double r = (bx - ax) * (fb - fc);
				double q = (bx - cx) * (fb - fa);
				double u = bx - ((bx - cx) * q - (bx - ax) * r) / (2.0 * Sign(Fmax(Fabs(q - r), Tiny), q - r));
				double fu;
				double ulim = bx + Glimit * (cx - bx);
				if ((bx - u) * (u - cx) > 0.0)
				{
					fu = function.ValueAt(u);
					if (fu < fc)
					{
						//Ax = new Double(bx);
						//Bx = new Double(u);
						//Cx = new Double(cx);
						//log.info("\nReturning3: a="+bx+" ("+fb+") b="+u+"("+fu+") c="+cx+" ("+fc+")");
						return new CGMinimizer.Triple(bx, u, cx);
					}
					else
					{
						if (fu > fb)
						{
							//Cx = new Double(u);
							//Ax = new Double(ax);
							//Bx = new Double(bx);
							//log.info("\nReturning2: a="+ax+" ("+fa+") b="+bx+"("+fb+") c="+u+" ("+fu+")");
							return new CGMinimizer.Triple(ax, bx, u);
						}
					}
					u = cx + Gold * (cx - bx);
					fu = function.ValueAt(u);
				}
				else
				{
					if ((cx - u) * (u - ulim) > 0.0)
					{
						fu = function.ValueAt(u);
						if (fu < fc)
						{
							bx = cx;
							cx = u;
							u = cx + Gold * (cx - bx);
							fb = fc;
							fc = fu;
							fu = function.ValueAt(u);
						}
					}
					else
					{
						if ((u - ulim) * (ulim - cx) >= 0.0)
						{
							u = ulim;
							fu = function.ValueAt(u);
						}
						else
						{
							u = cx + Gold * (cx - bx);
							fu = function.ValueAt(u);
						}
					}
				}
				ax = bx;
				bx = cx;
				cx = u;
				fa = fb;
				fb = fc;
				fc = fu;
			}
			//log.info("\nReturning: a="+ax+" ("+fa+") b="+bx+"("+fb+") c="+cx+" ("+fc+")");
			return new CGMinimizer.Triple(ax, bx, cx);
		}

		private static double Dbrent(CGMinimizer.OneDimDiffFunction function, double ax, double bx, double cx)
		{
			// constants
			bool dbVerbose = false;
			int Itmax = 100;
			double Tol = 1.0e-4;
			double d = 0.0;
			double e = 0.0;
			double a = (ax < cx ? ax : cx);
			double b = (ax > cx ? ax : cx);
			double x = bx;
			double v = bx;
			double w = bx;
			double fx = function.ValueAt(x);
			double fv = fx;
			double fw = fx;
			double dx = function.DerivativeAt(x);
			double dv = dx;
			double dw = dx;
			for (int iteration = 0; iteration < Itmax; iteration++)
			{
				//log.info("dbrent "+iteration+" x "+x+" fx "+fx);
				double xm = 0.5 * (a + b);
				double tol1 = Tol * Fabs(x);
				double tol2 = 2.0 * tol1;
				if (Fabs(x - xm) <= (tol2 - 0.5 * (b - a)))
				{
					return x;
				}
				double u;
				if (Fabs(e) > tol1)
				{
					double d1 = 2.0 * (b - a);
					double d2 = d1;
					if (dw != dx)
					{
						d1 = (w - x) * dx / (dx - dw);
					}
					if (dv != dx)
					{
						d2 = (v - x) * dx / (dx - dv);
					}
					double u1 = x + d1;
					double u2 = x + d2;
					bool ok1 = ((a - u1) * (u1 - b) > 0.0 && dx * d1 <= 0.0);
					bool ok2 = ((a - u2) * (u2 - b) > 0.0 && dx * d2 <= 0.0);
					double olde = e;
					e = d;
					if (ok1 || ok2)
					{
						if (ok1 && ok2)
						{
							d = (Fabs(d1) < Fabs(d2) ? d1 : d2);
						}
						else
						{
							if (ok1)
							{
								d = d1;
							}
							else
							{
								d = d2;
							}
						}
						if (Fabs(d) <= Fabs(0.5 * olde))
						{
							u = x + d;
							if (u - a < tol2 || b - u < tol2)
							{
								d = Sign(tol1, xm - x);
							}
						}
						else
						{
							e = (dx >= 0.0 ? a - x : b - x);
							d = 0.5 * e;
						}
					}
					else
					{
						e = (dx >= 0.0 ? a - x : b - x);
						d = 0.5 * e;
					}
				}
				else
				{
					e = (dx >= 0.0 ? a - x : b - x);
					d = 0.5 * e;
				}
				double fu;
				if (Fabs(d) >= tol1)
				{
					u = x + d;
					fu = function.ValueAt(u);
				}
				else
				{
					u = x + Sign(tol1, d);
					fu = function.ValueAt(u);
					if (fu > fx)
					{
						return x;
					}
				}
				double du = function.DerivativeAt(u);
				if (fu <= fx)
				{
					if (u >= x)
					{
						a = x;
					}
					else
					{
						b = x;
					}
					v = w;
					fv = fw;
					dv = dw;
					w = x;
					fw = fx;
					dw = dx;
					x = u;
					fx = fu;
					dx = du;
				}
				else
				{
					if (u < x)
					{
						a = u;
					}
					else
					{
						b = u;
					}
					if (fu <= fw || w == x)
					{
						v = w;
						fv = fw;
						dv = dw;
						w = u;
						fw = fu;
						dw = du;
					}
					else
					{
						if (fu < fv || v == x || v == w)
						{
							v = u;
							fv = fu;
							dv = du;
						}
					}
				}
			}
			// dan's addition:
			if (fx < function.ValueAt(0.0))
			{
				return x;
			}
			return 0.0;
		}

		private class Triple
		{
			public double a;

			public double b;

			public double c;

			public Triple(double a, double b, double c)
			{
				this.a = a;
				this.b = b;
				this.c = c;
			}
		}

		//public double lastXx = 1.0;
		private double[] LineMinimize(IDiffFunction function, double[] initial, double[] direction)
		{
			// make a 1-dim function along the direction line
			// THIS IS A HACK (but it's the NRiC peoples' hack)
			CGMinimizer.OneDimDiffFunction oneDim = new CGMinimizer.OneDimDiffFunction(function, initial, direction);
			// do a 1-dim line min on this function
			//Double Ax = new Double(0.0);
			//Double Xx = new Double(1.0);
			//Double Bx = new Double(0.0);
			// bracket the extreme pt
			double guess = 0.01;
			//log.info("Current "+oneDim.valueAt(0)+" nudge "+(oneDim.smallestZeroPositiveLocation()*1e-2)+" "+oneDim.valueAt(oneDim.smallestZeroPositiveLocation()*1e-5));
			if (!silent)
			{
				log.Info("[");
			}
			CGMinimizer.Triple bracketing = Mnbrak(new CGMinimizer.Triple(0, guess, 0), oneDim);
			if (!silent)
			{
				log.Info("]");
			}
			double ax = bracketing.a;
			double xx = bracketing.b;
			double bx = bracketing.c;
			//lastXx = xx;
			// CHECK FOR END OF WORLD
			if (!(ax <= xx && xx <= bx) && !(bx <= xx && xx <= ax))
			{
				log.Info("Bad bracket order!");
			}
			//log.info("Bracketing found: "+arrayToString(oneDim.vectorOf(ax),3)+" "+arrayToString(oneDim.vectorOf(xx),3)+" "+arrayToString(oneDim.vectorOf(bx),3));
			// find the extreme pt
			if (!silent)
			{
				log.Info("<");
			}
			double xmin = Dbrent(oneDim, ax, xx, bx);
			if (!silent)
			{
				log.Info(">");
			}
			// return the full vector
			//log.info("Went "+xmin+" during lineMinimize");
			return oneDim.VectorOf(xmin);
		}

		public virtual double[] Minimize(IDiffFunction function, double functionTolerance, double[] initial)
		{
			return Minimize(function, functionTolerance, initial, Itmax);
		}

		public virtual double[] Minimize(IDiffFunction dFunction, double functionTolerance, double[] initial, int maxIterations)
		{
			// check for derivatives
			int dimension = dFunction.DomainDimension();
			//lastXx = 1.0;
			// evaluate function
			double fp = dFunction.ValueAt(initial);
			double[] xi = CopyArray(dFunction.DerivativeAt(initial));
			// make some vectors
			double[] g = new double[dimension];
			double[] h = new double[dimension];
			double[] p = new double[dimension];
			for (int j = 0; j < dimension; j++)
			{
				g[j] = -xi[j];
				xi[j] = g[j];
				h[j] = g[j];
				p[j] = initial[j];
			}
			// iterations
			bool simpleGDStep = false;
			for (int iterations = 1; iterations < maxIterations; iterations++)
			{
				if (!silent)
				{
					log.Info("Iter " + iterations + ' ');
				}
				// do a line min along descent direction
				//log.info("Minimizing from ("+p[0]+","+p[1]+") along ("+xi[0]+","+xi[1]+")\n");
				//log.info("Current is "+fp);
				double[] p2 = LineMinimize(dFunction, p, xi);
				double fp2 = dFunction.ValueAt(p2);
				//log.info("Result is "+fp2+" (from "+fp+") at ("+p2[0]+","+p2[1]+")\n");
				//log.info(fp2+"|"+(int)(Math.log((fabs(fp2-fp)+1e-100)/(fabs(fp)+fabs(fp2)+1e-100))/Math.log(10)));
				if (!silent)
				{
					System.Console.Error.Printf(" %s (delta: %s)\n", nf.Format(fp2), nf.Format(fp - fp2));
				}
				if (monitor != null)
				{
					double monitorReturn = monitor.ValueAt(p2);
					if (monitorReturn < functionTolerance)
					{
						return p2;
					}
				}
				// check convergence
				if (2.0 * Fabs(fp2 - fp) <= functionTolerance * (Fabs(fp2) + Fabs(fp) + Eps))
				{
					// convergence
					if (!checkSimpleGDConvergence || simpleGDStep || simpleGD)
					{
						return p2;
					}
					simpleGDStep = true;
				}
				else
				{
					//log.info("Switched to GD for a step.");
					//if (!simpleGD)
					//log.info("Switching to CGD.");
					simpleGDStep = false;
				}
				// shift variables
				for (int j_1 = 0; j_1 < dimension; j_1++)
				{
					xi[j_1] = p2[j_1] - p[j_1];
					p[j_1] = p2[j_1];
				}
				fp = fp2;
				// find the new gradient
				xi = CopyArray(dFunction.DerivativeAt(p));
				if (iterationCallbackFunction != null)
				{
					iterationCallbackFunction.Callback(p2, iterations, fp2, xi);
				}
				//log.info("mx "+arrayMax(xi)+" mn "+arrayMin(xi));
				if (!simpleGDStep && !simpleGD && (iterations % resetFrequency != 0))
				{
					// do the magic -- part i
					// (calculate some dot products we'll need)
					double dgg = 0.0;
					double gg = 0.0;
					for (int j_2 = 0; j_2 < dimension; j_2++)
					{
						// g dot g
						gg += g[j_2] * g[j_2];
						// grad dot grad
						// FR method is:
						// dgg += x[j]*x[j];
						// PR method is:
						dgg += (xi[j_2] + g[j_2]) * xi[j_2];
					}
					// check for miraculous convergence
					if (gg == 0.0)
					{
						return p;
					}
					// magic part ii
					// (update the sequence in a way that tries to preserve conjugacy)
					double gam = dgg / gg;
					for (int j_3 = 0; j_3 < dimension; j_3++)
					{
						g[j_3] = -xi[j_3];
						h[j_3] = g[j_3] + gam * h[j_3];
						xi[j_3] = h[j_3];
					}
				}
				else
				{
					// miraculous simpleGD convergence
					double xixi = 0.0;
					for (int j_2 = 0; j_2 < dimension; j_2++)
					{
						xixi += xi[j_2] * xi[j_2];
					}
					// reset cgd
					for (int j_3 = 0; j_3 < dimension; j_3++)
					{
						g[j_3] = -xi[j_3];
						xi[j_3] = g[j_3];
						h[j_3] = g[j_3];
					}
					if (xixi == 0.0)
					{
						return p;
					}
				}
			}
			// too many iterations
			log.Info("Warning: exiting minimize because ITER exceeded!");
			return p;
		}

		public virtual void SetIterationCallbackFunction(CallbackFunction func)
		{
			this.iterationCallbackFunction = func;
		}

		/// <summary>Basic constructor, use this.</summary>
		public CGMinimizer()
			: this(true, null)
		{
		}

		/// <summary>
		/// Pass in
		/// <see langword="false"/>
		/// to get per-iteration progress reports
		/// (to stderr).
		/// </summary>
		/// <param name="silent">
		/// a
		/// <c>boolean</c>
		/// value
		/// </param>
		public CGMinimizer(bool silent)
			: this(silent, null)
		{
		}

		/// <summary>Perform minimization with monitoring.</summary>
		/// <remarks>
		/// Perform minimization with monitoring.  After each iteration,
		/// monitor.valueAt(x) gets called, with the double array
		/// <c>x</c>
		/// being that iteration's ending point.  A return
		/// <c>&lt; tol</c>
		/// forces convergence (terminates the CG procedure).
		/// Specially for Kristina.
		/// </remarks>
		/// <param name="monitor">
		/// a
		/// <c>Function</c>
		/// value
		/// </param>
		public CGMinimizer(IFunction monitor)
			: this(false, monitor)
		{
		}

		/// <summary>Perform minimization perhaps with monitoring.</summary>
		/// <remarks>
		/// Perform minimization perhaps with monitoring.  After each iteration,
		/// monitor.valueAt(x) gets called, with the double array
		/// <c>x</c>
		/// being that iteration's ending point.  A return
		/// <c>&lt; tol</c>
		/// forces convergence (terminates the CG procedure).
		/// </remarks>
		/// <param name="silent">Whether to run silently or not</param>
		/// <param name="monitor">
		/// A
		/// <c>Function</c>
		/// value
		/// </param>
		private CGMinimizer(bool silent, IFunction monitor)
		{
			this.silent = silent;
			this.monitor = monitor;
		}
	}
}
