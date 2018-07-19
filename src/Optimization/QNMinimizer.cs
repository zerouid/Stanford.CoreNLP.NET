using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>An implementation of L-BFGS for Quasi Newton unconstrained minimization.</summary>
	/// <remarks>
	/// An implementation of L-BFGS for Quasi Newton unconstrained minimization.
	/// Also now has support for OWL-QN (Orthant-Wise Limited memory Quasi Newton)
	/// for L1 regularization.
	/// The general outline of the algorithm is taken from:
	/// <blockquote>
	/// <i>Numerical Optimization</i> (second edition) 2006
	/// Jorge Nocedal and Stephen J. Wright
	/// </blockquote>
	/// A variety of different options are available.
	/// <h3>LINESEARCHES</h3>
	/// BACKTRACKING: This routine
	/// simply starts with a guess for step size of 1. If the step size doesn't
	/// supply a sufficient decrease in the function value the step is updated
	/// through step = 0.1*step. This method is certainly simpler, but doesn't allow
	/// for an increase in step size, and isn't well suited for Quasi Newton methods.
	/// MINPACK: This routine is based off of the implementation used in MINPACK.
	/// This routine finds a point satisfying the Wolfe conditions, which state that
	/// a point must have a sufficiently smaller function value, and a gradient of
	/// smaller magnitude. This provides enough to prove theoretically quadratic
	/// convergence. In order to find such a point the line search first finds an
	/// interval which must contain a satisfying point, and then progressively
	/// reduces that interval all using cubic or quadratic interpolation.
	/// SCALING: L-BFGS allows the initial guess at the hessian to be updated at each
	/// step. Standard BFGS does this by approximating the hessian as a scaled
	/// identity matrix. To use this method set the scaleOpt to SCALAR. A better way
	/// of approximate the hessian is by using a scaling diagonal matrix. The
	/// diagonal can then be updated as more information comes in. This method can be
	/// used by setting scaleOpt to DIAGONAL.
	/// CONVERGENCE: Previously convergence was gauged by looking at the average
	/// decrease per step dividing that by the current value and terminating when
	/// that value because smaller than TOL. This method fails when the function
	/// value approaches zero, so two other convergence criteria are used. The first
	/// stores the initial gradient norm |g0|, then terminates when the new gradient
	/// norm, |g| is sufficiently smaller: i.e., |g| &lt; eps*|g0| the second checks if
	/// |g| &lt; eps*max( 1 , |x| ) which is essentially checking to see if the gradient
	/// is numerically zero.
	/// Another convergence criteria is added where termination is triggered if no
	/// improvements are observed after X (set by terminateOnEvalImprovementNumOfEpoch)
	/// iterations over some validation test set as evaluated by Evaluator
	/// Each of these convergence criteria can be turned on or off by setting the
	/// flags:
	/// <blockquote><code>
	/// private boolean useAveImprovement = true;
	/// private boolean useRelativeNorm = true;
	/// private boolean useNumericalZero = true;
	/// private boolean useEvalImprovement = false;
	/// </code></blockquote>
	/// To use the QNMinimizer first construct it using
	/// <blockquote><code>
	/// QNMinimizer qn = new QNMinimizer(mem, true)
	/// </code></blockquote>
	/// mem - the number of previous estimate vector pairs to
	/// store, generally 15 is plenty. true - this tells the QN to use the MINPACK
	/// linesearch with DIAGONAL scaling. false would lead to the use of the criteria
	/// used in the old QNMinimizer class.
	/// Then call:
	/// <blockquote><code>
	/// qn.minimize(dfunction,convergenceTolerance,initialGuess,maxFunctionEvaluations);
	/// </code></blockquote>
	/// </remarks>
	/// <author>akleeman</author>
	public class QNMinimizer : IMinimizer<IDiffFunction>, IHasEvaluators
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.QNMinimizer));

		private int fevals = 0;

		private int maxFevals = -1;

		private int mem = 10;

		private int its;

		private readonly IFunction monitor;

		private bool quiet;

		private static readonly NumberFormat nf = new DecimalFormat("0.000E0");

		private static readonly NumberFormat nfsec = new DecimalFormat("0.00");

		private const double ftol = 1e-4;

		private double gtol = 0.9;

		private const double aMin = 1e-12;

		private const double aMax = 1e12;

		private const double p66 = 0.66;

		private const double p5 = 0.5;

		private const int a = 0;

		private const int f = 1;

		private const int g = 2;

		public bool outputToFile = false;

		private bool success = false;

		private bool bracketed = false;

		private QNMinimizer.QNInfo presetInfo = null;

		private bool noHistory = true;

		private bool useOWLQN = false;

		private double lambdaOWL = 0;

		private bool useAveImprovement = true;

		private bool useRelativeNorm = true;

		private bool useNumericalZero = true;

		private bool useEvalImprovement = false;

		private bool useMaxItr = false;

		private int maxItr = 0;

		private bool suppressTestPrompt = false;

		private int terminateOnEvalImprovementNumOfEpoch = 1;

		private int evaluateIters = 0;

		private int startEvaluateIters = 0;

		private IEvaluator[] evaluators;

		[System.NonSerialized]
		private CallbackFunction iterCallbackFunction = null;

		public enum EState
		{
			TerminateMaxevals,
			TerminateRelativenorm,
			TerminateGradnorm,
			TerminateAverageimprove,
			Continue,
			TerminateEvalimprove,
			TerminateMaxitr
		}

		public enum ELineSearch
		{
			Backtrack,
			Minpack
		}

		public enum EScaling
		{
			Diagonal,
			Scalar
		}

		private QNMinimizer.ELineSearch lsOpt = QNMinimizer.ELineSearch.Minpack;

		private QNMinimizer.EScaling scaleOpt = QNMinimizer.EScaling.Diagonal;

		public QNMinimizer()
			: this((IFunction)null)
		{
		}

		public QNMinimizer(int m)
			: this(null, m)
		{
		}

		public QNMinimizer(int m, bool useRobustOptions)
			: this(null, m, useRobustOptions)
		{
		}

		public QNMinimizer(IFunction monitor)
		{
			// the number of function evaluations
			// the number of s,y pairs to retain for BFGS
			// = 0; // the number of iterations through the main do-while loop of L-BFGS's minimize()
			// = false
			// for times
			// Linesearch parameters
			// Min step size
			// Max step size
			// used to check getting more than 2/3 of width improvement
			// Some other magic constant
			// used as array index
			// used as array index
			// used as array index
			// used for linesearch
			// parameters for OWL-QN (L-BFGS with L1-regularization)
			// Evaluate every x iterations (0 = no evaluation)
			// starting evaluation after x iterations
			// separate set of evaluators to check how optimization is going
			this.monitor = monitor;
		}

		public QNMinimizer(IFunction monitor, int m)
			: this(monitor, m, false)
		{
		}

		public QNMinimizer(IFunction monitor, int m, bool useRobustOptions)
		{
			this.monitor = monitor;
			mem = m;
			if (useRobustOptions)
			{
				this.SetRobustOptions();
			}
		}

		public QNMinimizer(IFloatFunction monitor)
		{
			throw new NotSupportedException("Doesn't support floats yet");
		}

		public virtual void SetOldOptions()
		{
			useAveImprovement = true;
			useRelativeNorm = false;
			useNumericalZero = false;
			lsOpt = QNMinimizer.ELineSearch.Backtrack;
			scaleOpt = QNMinimizer.EScaling.Scalar;
		}

		public void SetRobustOptions()
		{
			useAveImprovement = true;
			useRelativeNorm = true;
			useNumericalZero = true;
			lsOpt = QNMinimizer.ELineSearch.Minpack;
			scaleOpt = QNMinimizer.EScaling.Diagonal;
		}

		public virtual void SetEvaluators(int iters, IEvaluator[] evaluators)
		{
			this.evaluateIters = iters;
			this.evaluators = evaluators;
		}

		public virtual void SetEvaluators(int iters, int startEvaluateIters, IEvaluator[] evaluators)
		{
			this.evaluateIters = iters;
			this.startEvaluateIters = startEvaluateIters;
			this.evaluators = evaluators;
		}

		public virtual void SetIterationCallbackFunction(CallbackFunction func)
		{
			iterCallbackFunction = func;
		}

		public virtual void TerminateOnRelativeNorm(bool toTerminate)
		{
			useRelativeNorm = toTerminate;
		}

		public virtual void TerminateOnNumericalZero(bool toTerminate)
		{
			useNumericalZero = toTerminate;
		}

		public virtual void TerminateOnAverageImprovement(bool toTerminate)
		{
			useAveImprovement = toTerminate;
		}

		public virtual void TerminateOnEvalImprovement(bool toTerminate)
		{
			useEvalImprovement = toTerminate;
		}

		public virtual void TerminateOnMaxItr(int maxItr)
		{
			if (maxItr > 0)
			{
				useMaxItr = true;
				this.maxItr = maxItr;
			}
		}

		public virtual void SuppressTestPrompt(bool suppressTestPrompt)
		{
			this.suppressTestPrompt = suppressTestPrompt;
		}

		public virtual void SetTerminateOnEvalImprovementNumOfEpoch(int terminateOnEvalImprovementNumOfEpoch)
		{
			this.terminateOnEvalImprovementNumOfEpoch = terminateOnEvalImprovementNumOfEpoch;
		}

		public virtual void UseMinPackSearch()
		{
			lsOpt = QNMinimizer.ELineSearch.Minpack;
		}

		public virtual void UseBacktracking()
		{
			lsOpt = QNMinimizer.ELineSearch.Backtrack;
		}

		public virtual void UseDiagonalScaling()
		{
			scaleOpt = QNMinimizer.EScaling.Diagonal;
		}

		public virtual void UseScalarScaling()
		{
			scaleOpt = QNMinimizer.EScaling.Scalar;
		}

		public virtual bool WasSuccessful()
		{
			return success;
		}

		public virtual void ShutUp()
		{
			this.quiet = true;
		}

		public virtual void SetM(int m)
		{
			mem = m;
		}

		[System.Serializable]
		public class SurpriseConvergence : Exception
		{
			private const long serialVersionUID = 4290178321643529559L;

			public SurpriseConvergence(string s)
				: base(s)
			{
			}
		}

		[System.Serializable]
		private class MaxEvaluationsExceeded : Exception
		{
			private const long serialVersionUID = 8044806163343218660L;

			public MaxEvaluationsExceeded(string s)
				: base(s)
			{
			}
		}

		/// <summary>
		/// The Record class is used to collect information about the function value
		/// over a series of iterations.
		/// </summary>
		/// <remarks>
		/// The Record class is used to collect information about the function value
		/// over a series of iterations. This information is used to determine
		/// convergence, and to (attempt to) ensure numerical errors are not an issue.
		/// It can also be used for plotting the results of the optimization routine.
		/// </remarks>
		/// <author>akleeman</author>
		internal class Record
		{
			private readonly IList<double> evals = new List<double>();

			private readonly IList<double> values = new List<double>();

			private IList<double> gNorms = new List<double>();

			private readonly IList<int> funcEvals = new List<int>();

			private readonly IList<double> time = new List<double>();

			private double gNormInit = double.MinValue;

			private double relativeTOL = 1e-8;

			private double Tol = 1e-6;

			private double Eps = 1e-6;

			private long startTime;

			private double gNormLast;

			private double[] xLast;

			private int maxSize = 100;

			private IFunction mon = null;

			private bool quiet = false;

			private bool memoryConscious = true;

			private PrintWriter outputFile = null;

			private double[] xBest;

			internal Record(QNMinimizer _enclosing, IFunction monitor, double tolerance, PrintWriter output)
			{
				this._enclosing = _enclosing;
				// convergence options.
				// have average difference like before
				// zero gradient.
				// for convergence test
				// List<Double> xNorms = new ArrayList<Double>();
				// gNormInit: This makes it so that if for some reason
				// you try and divide by the initial norm before it's been
				// initialized you don't get a NAN but you will also never
				// get false convergence.
				// This is used for convergence.
				// This will control the number of func values /
				// gradients to retain.
				// private int noImproveItrCount = 0;
				this.mon = monitor;
				this.Tol = tolerance;
				this.outputFile = output;
			}

			internal Record(QNMinimizer _enclosing, IFunction monitor, double tolerance, double eps)
			{
				this._enclosing = _enclosing;
				this.mon = monitor;
				this.Tol = tolerance;
				this.Eps = eps;
			}

			internal virtual void SetEPS(double eps)
			{
				this.Eps = eps;
			}

			internal virtual void SetTOL(double tolerance)
			{
				this.Tol = tolerance;
			}

			internal virtual void Start(double val, double[] grad)
			{
				this.Start(val, grad, null);
			}

			/*
			* Initialize the class, this starts the timer, and initiates the gradient
			* norm for use with convergence.
			*/
			internal virtual void Start(double val, double[] grad, double[] x)
			{
				this.startTime = Runtime.CurrentTimeMillis();
				this.gNormInit = ArrayMath.Norm(grad);
				this.xLast = x;
				this.WriteToFile(1, val, this.gNormInit, 0.0);
				if (x != null)
				{
					this.MonitorX(x);
				}
			}

			private void WriteToFile(double fevals, double val, double gNorm, double time)
			{
				if (this.outputFile != null)
				{
					this.outputFile.Println(fevals + "," + val + ',' + gNorm + ',' + time);
				}
			}

			private void Add(double val, double[] grad, double[] x, int fevals, double evalScore, StringBuilder sb)
			{
				if (!this.memoryConscious)
				{
					if (this.gNorms.Count > this.maxSize)
					{
						this.gNorms.Remove(0);
					}
					if (this.time.Count > this.maxSize)
					{
						this.time.Remove(0);
					}
					if (this.funcEvals.Count > this.maxSize)
					{
						this.funcEvals.Remove(0);
					}
					this.gNorms.Add(this.gNormLast);
					this.time.Add(this.HowLong());
					this.funcEvals.Add(fevals);
				}
				else
				{
					this.maxSize = 10;
				}
				this.gNormLast = ArrayMath.Norm(grad);
				if (this.values.Count > this.maxSize)
				{
					this.values.Remove(0);
				}
				this.values.Add(val);
				if (evalScore != double.NegativeInfinity)
				{
					this.evals.Add(evalScore);
				}
				this.WriteToFile(fevals, val, this.gNormLast, this.HowLong());
				sb.Append(QNMinimizer.nf.Format(val)).Append(' ').Append(QNMinimizer.nfsec.Format(this.HowLong())).Append('s');
				this.xLast = x;
				this.MonitorX(x);
			}

			internal virtual void MonitorX(double[] x)
			{
				if (this.mon != null)
				{
					this.mon.ValueAt(x);
				}
			}

			/// <summary>
			/// This function checks for convergence through first
			/// order optimality,  numerical convergence (i.e., zero numerical
			/// gradient), and also by checking the average improvement.
			/// </summary>
			/// <returns>
			/// A value of the enumeration type <b>eState</b> which tells the
			/// state of the optimization routine indicating whether the routine should
			/// terminate, and if so why.
			/// </returns>
			private QNMinimizer.EState ToContinue(StringBuilder sb)
			{
				double relNorm = this.gNormLast / this.gNormInit;
				int size = this.values.Count;
				double newestVal = this.values[size - 1];
				double previousVal = (size >= 10 ? this.values[size - 10] : this.values[0]);
				double averageImprovement = (previousVal - newestVal) / (size >= 10 ? 10 : size);
				int evalsSize = this.evals.Count;
				if (this._enclosing.useMaxItr && this._enclosing.its >= this._enclosing.maxItr)
				{
					return QNMinimizer.EState.TerminateMaxitr;
				}
				if (this._enclosing.useEvalImprovement)
				{
					int bestInd = -1;
					double bestScore = double.NegativeInfinity;
					for (int i = 0; i < evalsSize; i++)
					{
						if (this.evals[i] >= bestScore)
						{
							bestScore = this.evals[i];
							bestInd = i;
						}
					}
					if (bestInd == evalsSize - 1)
					{
						// copy xBest
						if (this.xBest == null)
						{
							this.xBest = Arrays.CopyOf(this.xLast, this.xLast.Length);
						}
						else
						{
							System.Array.Copy(this.xLast, 0, this.xBest, 0, this.xLast.Length);
						}
					}
					if ((evalsSize - bestInd) >= this._enclosing.terminateOnEvalImprovementNumOfEpoch)
					{
						return QNMinimizer.EState.TerminateEvalimprove;
					}
				}
				// This is used to be able to reproduce results that were trained on the
				// QNMinimizer before
				// convergence criteria was updated.
				if (this._enclosing.useAveImprovement && (size > 5 && System.Math.Abs(averageImprovement / newestVal) < this.Tol))
				{
					return QNMinimizer.EState.TerminateAverageimprove;
				}
				// Check to see if the gradient is sufficiently small
				if (this._enclosing.useRelativeNorm && relNorm <= this.relativeTOL)
				{
					return QNMinimizer.EState.TerminateRelativenorm;
				}
				if (this._enclosing.useNumericalZero)
				{
					// This checks if the gradient is sufficiently small compared to x that
					// it is treated as zero.
					if (this.gNormLast < this.Eps * System.Math.Max(1.0, ArrayMath.Norm_1(this.xLast)))
					{
						// |g| < |x|_1
						// First we do the one norm, because that's easiest, and always bigger.
						if (this.gNormLast < this.Eps * System.Math.Max(1.0, ArrayMath.Norm(this.xLast)))
						{
							// |g| < max(1,|x|)
							// Now actually compare with the two norm if we have to.
							QNMinimizer.log.Warn("Gradient is numerically zero, stopped on machine epsilon.");
							return QNMinimizer.EState.TerminateGradnorm;
						}
					}
				}
				// give user information about the norms.
				sb.Append(" |").Append(QNMinimizer.nf.Format(this.gNormLast)).Append("| {").Append(QNMinimizer.nf.Format(relNorm)).Append("} ");
				sb.Append(QNMinimizer.nf.Format(System.Math.Abs(averageImprovement / newestVal))).Append(' ');
				sb.Append(evalsSize > 0 ? this.evals[evalsSize - 1].ToString() : "-").Append(' ');
				return QNMinimizer.EState.Continue;
			}

			/// <summary>Return the time in seconds since this class was created.</summary>
			/// <returns>The time in seconds since this class was created.</returns>
			internal virtual double HowLong()
			{
				return (Runtime.CurrentTimeMillis() - this.startTime) / 1000.0;
			}

			internal virtual double[] GetBest()
			{
				return this.xBest;
			}

			private readonly QNMinimizer _enclosing;
		}

		/// <summary>
		/// The QNInfo class is used to store information about the Quasi Newton
		/// update.
		/// </summary>
		/// <remarks>
		/// The QNInfo class is used to store information about the Quasi Newton
		/// update. it holds all the s,y pairs, updates the diagonal and scales
		/// everything as needed.
		/// </remarks>
		internal class QNInfo
		{
			private IList<double[]> s = null;

			private IList<double[]> y = null;

			private IList<double> rho = null;

			private double gamma;

			public double[] d = null;

			private int mem;

			private int maxMem = 20;

			public QNMinimizer.EScaling scaleOpt = QNMinimizer.EScaling.Scalar;

			internal QNInfo(QNMinimizer _enclosing, int size)
			{
				this._enclosing = _enclosing;
				// end class Record
				// Diagonal Options
				// Line search Options
				// Memory stuff
				this.s = new List<double[]>();
				this.y = new List<double[]>();
				this.rho = new List<double>();
				this.gamma = 1;
				this.mem = size;
			}

			internal QNInfo(QNMinimizer _enclosing, IList<double[]> sList, IList<double[]> yList)
			{
				this._enclosing = _enclosing;
				this.s = new List<double[]>();
				this.y = new List<double[]>();
				this.rho = new List<double>();
				this.gamma = 1;
				this.SetHistory(sList, yList);
			}

			internal virtual int Size()
			{
				return this.s.Count;
			}

			internal virtual double GetRho(int ind)
			{
				return this.rho[ind];
			}

			internal virtual double[] GetS(int ind)
			{
				return this.s[ind];
			}

			internal virtual double[] GetY(int ind)
			{
				return this.y[ind];
			}

			internal virtual void UseDiagonalScaling()
			{
				this.scaleOpt = QNMinimizer.EScaling.Diagonal;
			}

			internal virtual void UseScalarScaling()
			{
				this.scaleOpt = QNMinimizer.EScaling.Scalar;
			}

			/*
			* Free up that memory.
			*/
			internal virtual void Free()
			{
				this.s = null;
				this.y = null;
				this.rho = null;
				this.d = null;
			}

			internal virtual void Clear()
			{
				this.s.Clear();
				this.y.Clear();
				this.rho.Clear();
				this.d = null;
			}

			/// <summary>
			/// This function
			/// <c>applyInitialHessian(double[] x)</c>
			/// takes the vector
			/// <c>x</c>
			/// , and applies the best guess at the
			/// initial hessian to this vector, based off available information from
			/// previous updates.
			/// </summary>
			internal virtual void SetHistory(IList<double[]> sList, IList<double[]> yList)
			{
				int size = sList.Count;
				for (int i = 0; i < size; i++)
				{
					this.Update(sList[i], yList[i], ArrayMath.InnerProduct(yList[i], yList[i]), ArrayMath.InnerProduct(sList[i], yList[i]), 0, 1.0);
				}
			}

			internal virtual double[] ApplyInitialHessian(double[] x, StringBuilder sb)
			{
				switch (this.scaleOpt)
				{
					case QNMinimizer.EScaling.Scalar:
					{
						sb.Append('I');
						ArrayMath.MultiplyInPlace(x, this.gamma);
						break;
					}

					case QNMinimizer.EScaling.Diagonal:
					{
						sb.Append('D');
						if (this.d != null)
						{
							// Check sizes
							if (x.Length != this.d.Length)
							{
								throw new ArgumentException("Vector of incorrect size passed to applyInitialHessian in QNInfo class");
							}
							// Scale element-wise
							for (int i = 0; i < x.Length; i++)
							{
								x[i] = x[i] / (this.d[i]);
							}
						}
						break;
					}
				}
				return x;
			}

			/*
			* The update function is used to update the hessian approximation used by
			* the quasi newton optimization routine.
			*
			* If everything has behaved nicely, this involves deciding on a new initial
			* hessian through scaling or diagonal update, and then storing of the
			* secant pairs s = x - previousX and y = grad - previousGrad.
			*
			* Things can go wrong, if any non convex behavior is detected (s^T y &lt; 0)
			* or numerical errors are likely the update is skipped.
			*/
			/// <exception cref="Edu.Stanford.Nlp.Optimization.QNMinimizer.SurpriseConvergence"/>
			internal virtual int Update(double[] newX, double[] x, double[] newGrad, double[] grad, double step)
			{
				// todo: add OutOfMemory error.
				double[] newS;
				double[] newY;
				double sy;
				double yy;
				double sg;
				// allocate arrays for new s,y pairs (or replace if the list is already full)
				if (this.mem > 0 && this.s.Count == this.mem || this.s.Count == this.maxMem)
				{
					newS = this.s.Remove(0);
					newY = this.y.Remove(0);
					this.rho.Remove(0);
				}
				else
				{
					newS = new double[x.Length];
					newY = new double[x.Length];
				}
				// Here we construct the new pairs, and check for positive definiteness.
				sy = 0;
				yy = 0;
				sg = 0;
				for (int i = 0; i < x.Length; i++)
				{
					newS[i] = newX[i] - x[i];
					newY[i] = newGrad[i] - grad[i];
					sy += newS[i] * newY[i];
					yy += newY[i] * newY[i];
					sg += newS[i] * newGrad[i];
				}
				// Apply the updates used for the initial hessian.
				return this.Update(newS, newY, yy, sy, sg, step);
			}

			[System.Serializable]
			private class NegativeCurvature : Exception
			{
				private const long serialVersionUID = 4676562552506850519L;

				public NegativeCurvature(QNInfo _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly QNInfo _enclosing;
			}

			[System.Serializable]
			private class ZeroGradient : Exception
			{
				private const long serialVersionUID = -4001834044987928521L;

				public ZeroGradient(QNInfo _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly QNInfo _enclosing;
			}

			internal virtual int Update(double[] newS, double[] newY, double yy, double sy, double sg, double step)
			{
				// Initialize diagonal to the identity
				if (this.scaleOpt == QNMinimizer.EScaling.Diagonal && this.d == null)
				{
					this.d = new double[newS.Length];
					for (int i = 0; i < this.d.Length; i++)
					{
						this.d[i] = 1.0;
					}
				}
				try
				{
					if (sy < 0)
					{
						throw new QNMinimizer.QNInfo.NegativeCurvature(this);
					}
					if (yy == 0.0)
					{
						throw new QNMinimizer.QNInfo.ZeroGradient(this);
					}
					switch (this.scaleOpt)
					{
						case QNMinimizer.EScaling.Scalar:
						{
							/*
							* SCALAR: The standard L-BFGS initial approximation which is just a
							* scaled identity.
							*/
							this.gamma = sy / yy;
							break;
						}

						case QNMinimizer.EScaling.Diagonal:
						{
							/*
							* DIAGONAL: A diagonal scaling matrix is used as the initial
							* approximation. The updating method used is used thanks to Andrew
							* Bradley of the ICME dept.
							*/
							double sDs;
							// Gamma is designed to scale such that a step length of one is
							// generally accepted.
							this.gamma = sy / (step * (sy - sg));
							sDs = 0.0;
							for (int i = 0; i < this.d.Length; i++)
							{
								this.d[i] = this.gamma * this.d[i];
								sDs += newS[i] * this.d[i] * newS[i];
							}
							// This diagonal update was introduced by Andrew Bradley
							for (int i_1 = 0; i_1 < this.d.Length; i_1++)
							{
								this.d[i_1] = (1 - this.d[i_1] * newS[i_1] * newS[i_1] / sDs) * this.d[i_1] + newY[i_1] * newY[i_1] / sy;
							}
							// Here we make sure that the diagonal is alright
							double minD = ArrayMath.Min(this.d);
							double maxD = ArrayMath.Max(this.d);
							// If things have gone bad, just fill with the SCALAR approx.
							if (minD <= 0 || double.IsInfinite(maxD) || maxD / minD > 1e12)
							{
								QNMinimizer.log.Warn("QNInfo:update() : PROBLEM WITH DIAGONAL UPDATE");
								double fill = yy / sy;
								for (int i_2 = 0; i_2 < this.d.Length; i_2++)
								{
									this.d[i_2] = fill;
								}
							}
							break;
						}
					}
					// If s is already of size mem, remove the oldest vector and free it up.
					if (this.mem > 0 && this.s.Count == this.mem || this.s.Count == this.maxMem)
					{
						this.s.Remove(0);
						this.y.Remove(0);
						this.rho.Remove(0);
					}
					// Actually add the pair.
					this.s.Add(newS);
					this.y.Add(newY);
					this.rho.Add(1 / sy);
				}
				catch (QNMinimizer.QNInfo.NegativeCurvature)
				{
					// NOTE: if applying QNMinimizer to a non convex problem, we would still
					// like to update the matrix
					// or we could get stuck in a series of skipped updates.
					this._enclosing.Sayln(" Negative curvature detected, update skipped ");
				}
				catch (QNMinimizer.QNInfo.ZeroGradient)
				{
					this._enclosing.Sayln(" Either convergence, or floating point errors combined with extremely linear region ");
				}
				return this.s.Count;
			}

			private readonly QNMinimizer _enclosing;
			// end update
		}

		// end class QNInfo
		public virtual void SetHistory(IList<double[]> s, IList<double[]> y)
		{
			presetInfo = new QNMinimizer.QNInfo(this, s, y);
		}

		/// <summary>
		/// computeDir()
		/// This function will calculate an approximation of the inverse hessian based
		/// off the seen s,y vector pairs.
		/// </summary>
		/// <remarks>
		/// computeDir()
		/// This function will calculate an approximation of the inverse hessian based
		/// off the seen s,y vector pairs. This particular approximation uses the BFGS
		/// update.
		/// </remarks>
		/// <exception cref="Edu.Stanford.Nlp.Optimization.QNMinimizer.SurpriseConvergence"/>
		private void ComputeDir(double[] dir, double[] fg, double[] x, QNMinimizer.QNInfo qn, IFunction func, StringBuilder sb)
		{
			System.Array.Copy(fg, 0, dir, 0, fg.Length);
			int mmm = qn.Size();
			double[] @as = new double[mmm];
			for (int i = mmm - 1; i >= 0; i--)
			{
				@as[i] = qn.GetRho(i) * ArrayMath.InnerProduct(qn.GetS(i), dir);
				PlusAndConstMult(dir, qn.GetY(i), -@as[i], dir);
			}
			// multiply by hessian approximation
			qn.ApplyInitialHessian(dir, sb);
			for (int i_1 = 0; i_1 < mmm; i_1++)
			{
				double b = qn.GetRho(i_1) * ArrayMath.InnerProduct(qn.GetY(i_1), dir);
				PlusAndConstMult(dir, qn.GetS(i_1), @as[i_1] - b, dir);
			}
			ArrayMath.MultiplyInPlace(dir, -1);
			if (useOWLQN)
			{
				// step (2) in Galen & Gao 2007
				ConstrainSearchDir(dir, fg, x, func);
			}
		}

		// computes d = a + b * c
		private static double[] PlusAndConstMult(double[] a, double[] b, double c, double[] d)
		{
			for (int i = 0; i < a.Length; i++)
			{
				d[i] = a[i] + c * b[i];
			}
			return d;
		}

		private double DoEvaluation(double[] x)
		{
			// Evaluate solution
			if (evaluators == null)
			{
				return double.NegativeInfinity;
			}
			double score = 0;
			foreach (IEvaluator eval in evaluators)
			{
				if (!suppressTestPrompt)
				{
					Sayln("  Evaluating: " + eval.ToString());
				}
				score = eval.Evaluate(x);
			}
			return score;
		}

		public virtual float[] Minimize(IDiffFloatFunction function, float functionTolerance, float[] initial)
		{
			throw new NotSupportedException("Float not yet supported for QN");
		}

		public virtual double[] Minimize(IDiffFunction function, double functionTolerance, double[] initial)
		{
			return Minimize(function, functionTolerance, initial, -1);
		}

		public virtual double[] Minimize(IDiffFunction dFunction, double functionTolerance, double[] initial, int maxFunctionEvaluations)
		{
			return Minimize(dFunction, functionTolerance, initial, maxFunctionEvaluations, null);
		}

		public virtual double[] Minimize(IDiffFunction dFunction, double functionTolerance, double[] initial, int maxFunctionEvaluations, QNMinimizer.QNInfo qn)
		{
			if (mem > 0)
			{
				Sayln("QNMinimizer called on double function of " + dFunction.DomainDimension() + " variables, using M = " + mem + '.');
			}
			else
			{
				Sayln("QNMinimizer called on double function of " + dFunction.DomainDimension() + " variables, using dynamic setting of M.");
			}
			if (qn == null && presetInfo == null)
			{
				qn = new QNMinimizer.QNInfo(this, mem);
				noHistory = true;
			}
			else
			{
				if (presetInfo != null)
				{
					qn = presetInfo;
					noHistory = false;
				}
				else
				{
					if (qn != null)
					{
						noHistory = false;
					}
				}
			}
			its = 0;
			fevals = 0;
			success = false;
			qn.scaleOpt = scaleOpt;
			// initialize weights
			double[] x = initial;
			// initialize gradient
			double[] rawGrad = new double[x.Length];
			double[] newGrad = new double[x.Length];
			double[] newX = new double[x.Length];
			double[] dir = new double[x.Length];
			// initialize function value and gradient (gradient is stored in grad inside
			// evaluateFunction)
			double value = EvaluateFunction(dFunction, x, rawGrad);
			double[] grad;
			if (useOWLQN)
			{
				double norm = L1NormOWL(x, dFunction);
				value += norm * lambdaOWL;
				// step (1) in Galen & Gao except we are not computing v yet
				grad = PseudoGradientOWL(x, rawGrad, dFunction);
			}
			else
			{
				grad = rawGrad;
			}
			PrintWriter outFile = null;
			PrintWriter infoFile = null;
			if (outputToFile)
			{
				try
				{
					string baseName = "QN_m" + mem + '_' + lsOpt.ToString() + '_' + scaleOpt.ToString();
					outFile = new PrintWriter(new FileOutputStream(baseName + ".output"), true);
					infoFile = new PrintWriter(new FileOutputStream(baseName + ".info"), true);
					infoFile.Println(dFunction.DomainDimension() + "; DomainDimension ");
					infoFile.Println(mem + "; memory");
				}
				catch (IOException e)
				{
					throw new RuntimeIOException("Caught IOException outputting QN data to file", e);
				}
			}
			QNMinimizer.Record rec = new QNMinimizer.Record(this, monitor, functionTolerance, outFile);
			// sets the original gradient and x. Also stores the monitor.
			rec.Start(value, rawGrad, x);
			// Check if max Evaluations and Iterations have been provided.
			maxFevals = (maxFunctionEvaluations > 0) ? maxFunctionEvaluations : int.MaxValue;
			// maxIterations = (maxIterations > 0) ? maxIterations : Integer.MAX_VALUE;
			Sayln("               An explanation of the output:");
			Sayln("Iter           The number of iterations");
			Sayln("evals          The number of function evaluations");
			Sayln("SCALING        <D> Diagonal scaling was used; <I> Scaled Identity");
			Sayln("LINESEARCH     [## M steplength]  Minpack linesearch");
			Sayln("                   1-Function value was too high");
			Sayln("                   2-Value ok, gradient positive, positive curvature");
			Sayln("                   3-Value ok, gradient negative, positive curvature");
			Sayln("                   4-Value ok, gradient negative, negative curvature");
			Sayln("               [.. B]  Backtracking");
			Sayln("VALUE          The current function value");
			Sayln("TIME           Total elapsed time");
			Sayln("|GNORM|        The current norm of the gradient");
			Sayln("{RELNORM}      The ratio of the current to initial gradient norms");
			Sayln("AVEIMPROVE     The average improvement / current value");
			Sayln("EVALSCORE      The last available eval score");
			Sayln();
			Sayln("Iter ## evals ## <SCALING> [LINESEARCH] VALUE TIME |GNORM| {RELNORM} AVEIMPROVE EVALSCORE");
			StringBuilder sb = new StringBuilder();
			QNMinimizer.EState state = QNMinimizer.EState.Continue;
			do
			{
				// Beginning of the loop.
				try
				{
					if (!quiet)
					{
						Sayln(sb.ToString());
					}
					sb = new StringBuilder();
					bool doEval = (its >= 0 && its >= startEvaluateIters && evaluateIters > 0 && its % evaluateIters == 0);
					its += 1;
					double newValue;
					sb.Append("Iter ").Append(its).Append(" evals ").Append(fevals).Append(' ');
					// Compute the search direction
					sb.Append('<');
					ComputeDir(dir, grad, x, qn, dFunction, sb);
					sb.Append("> ");
					// sanity check dir
					bool hasNaNDir = false;
					bool hasNaNGrad = false;
					for (int i = 0; i < dir.Length; i++)
					{
						if (dir[i] != dir[i])
						{
							hasNaNDir = true;
						}
						if (grad[i] != grad[i])
						{
							hasNaNGrad = true;
						}
					}
					if (hasNaNDir && !hasNaNGrad)
					{
						Sayln("(NaN dir likely due to Hessian approx - resetting) ");
						qn.Clear();
						// re-compute the search direction
						sb.Append('<');
						ComputeDir(dir, grad, x, qn, dFunction, sb);
						sb.Append("> ");
					}
					// perform line search
					sb.Append('[');
					double[] newPoint;
					// initialized in if/else/switch below
					if (useOWLQN)
					{
						// only linear search is allowed for OWL-QN
						newPoint = LineSearchBacktrackOWL(dFunction, dir, x, newX, grad, value, sb);
						sb.Append('B');
					}
					else
					{
						switch (lsOpt)
						{
							case QNMinimizer.ELineSearch.Backtrack:
							{
								// switch between line search options.
								newPoint = LineSearchBacktrack(dFunction, dir, x, newX, grad, value, sb);
								sb.Append('B');
								break;
							}

							case QNMinimizer.ELineSearch.Minpack:
							{
								newPoint = LineSearchMinPack(dFunction, dir, x, newX, grad, value, functionTolerance, sb);
								sb.Append('M');
								break;
							}

							default:
							{
								throw new ArgumentException("Invalid line search option for QNMinimizer.");
							}
						}
					}
					newValue = newPoint[f];
					sb.Append(' ');
					sb.Append(nf.Format(newPoint[a]));
					sb.Append("] ");
					// This shouldn't actually evaluate anything since that should have been
					// done in the lineSearch.
					System.Array.Copy(dFunction.DerivativeAt(newX), 0, newGrad, 0, newGrad.Length);
					// This is where all the s, y updates are applied.
					qn.Update(newX, x, newGrad, rawGrad, newPoint[a]);
					// step (4) in Galen & Gao 2007
					if (useOWLQN)
					{
						System.Array.Copy(newGrad, 0, rawGrad, 0, newGrad.Length);
						// pseudo gradient
						newGrad = PseudoGradientOWL(newX, newGrad, dFunction);
					}
					double evalScore = double.NegativeInfinity;
					if (doEval)
					{
						evalScore = DoEvaluation(newX);
					}
					// Add the current value and gradient to the records, this also monitors
					// X and writes to output
					rec.Add(newValue, newGrad, newX, fevals, evalScore, sb);
					// If you want to call a function and do whatever with the information ...
					if (iterCallbackFunction != null)
					{
						iterCallbackFunction.Callback(newX, its, newValue, newGrad);
					}
					// shift
					value = newValue;
					// double[] temp = x;
					// x = newX;
					// newX = temp;
					System.Array.Copy(newX, 0, x, 0, x.Length);
					System.Array.Copy(newGrad, 0, grad, 0, newGrad.Length);
					if (fevals > maxFevals)
					{
						throw new QNMinimizer.MaxEvaluationsExceeded("Exceeded in minimize() loop.");
					}
				}
				catch (QNMinimizer.SurpriseConvergence)
				{
					Sayln("QNMinimizer aborted due to surprise convergence");
					break;
				}
				catch (QNMinimizer.MaxEvaluationsExceeded m)
				{
					Sayln("QNMinimizer aborted due to maximum number of function evaluations");
					Sayln(m.ToString());
					Sayln("** This is not an acceptable termination of QNMinimizer, consider");
					Sayln("** increasing the max number of evaluations, or safeguarding your");
					Sayln("** program by checking the QNMinimizer.wasSuccessful() method.");
					break;
				}
				catch (OutOfMemoryException oome)
				{
					if (qn.s.Count > 1)
					{
						qn.s.Remove(0);
						qn.y.Remove(0);
						qn.rho.Remove(0);
						sb.Append("{Caught OutOfMemory, changing m from ").Append(qn.mem).Append(" to ").Append(qn.s.Count).Append("}]");
						qn.mem = qn.s.Count;
					}
					else
					{
						throw;
					}
				}
			}
			while ((state = rec.ToContinue(sb)) == QNMinimizer.EState.Continue);
			// end do while
			if (evaluateIters > 0)
			{
				// do final evaluation
				double evalScore = (useEvalImprovement ? DoEvaluation(rec.GetBest()) : DoEvaluation(x));
				Sayln("final evalScore is: " + evalScore);
			}
			switch (state)
			{
				case QNMinimizer.EState.TerminateGradnorm:
				{
					//
					// Announce the reason minimization has terminated.
					//
					Sayln("QNMinimizer terminated due to numerically zero gradient: |g| < EPS  max(1,|x|) ");
					success = true;
					break;
				}

				case QNMinimizer.EState.TerminateRelativenorm:
				{
					Sayln("QNMinimizer terminated due to sufficient decrease in gradient norms: |g|/|g0| < TOL ");
					success = true;
					break;
				}

				case QNMinimizer.EState.TerminateAverageimprove:
				{
					Sayln("QNMinimizer terminated due to average improvement: | newest_val - previous_val | / |newestVal| < TOL ");
					success = true;
					break;
				}

				case QNMinimizer.EState.TerminateMaxitr:
				{
					Sayln("QNMinimizer terminated due to reached max iteration " + maxItr);
					success = true;
					break;
				}

				case QNMinimizer.EState.TerminateEvalimprove:
				{
					Sayln("QNMinimizer terminated due to no improvement on eval ");
					success = true;
					x = rec.GetBest();
					break;
				}

				default:
				{
					log.Warn("QNMinimizer terminated without converging");
					success = false;
					break;
				}
			}
			double completionTime = rec.HowLong();
			Sayln("Total time spent in optimization: " + nfsec.Format(completionTime) + 's');
			if (outputToFile)
			{
				infoFile.Println(completionTime + "; Total Time ");
				infoFile.Println(fevals + "; Total evaluations");
				infoFile.Close();
				outFile.Close();
			}
			qn.Free();
			return x;
		}

		// end minimize()
		private void Sayln()
		{
			if (!quiet)
			{
				log.Info(" ");
			}
		}

		// no argument seems to cause Redwoods to act weird (in 2016)
		private void Sayln(string s)
		{
			if (!quiet)
			{
				log.Info(s);
			}
		}

		// todo [cdm 2013]: Can this be sped up by returning a Pair rather than copying array?
		private double EvaluateFunction(IDiffFunction dfunc, double[] x, double[] grad)
		{
			System.Array.Copy(dfunc.DerivativeAt(x), 0, grad, 0, grad.Length);
			fevals += 1;
			return dfunc.ValueAt(x);
		}

		/// <summary>
		/// To set QNMinimizer to use L1 regularization, call this method before use,
		/// with the boolean set true, and the appropriate lambda parameter.
		/// </summary>
		/// <param name="use">Whether to use Orthant-wise optimization</param>
		/// <param name="lambda">The L1 regularization parameter.</param>
		public virtual void UseOWLQN(bool use, double lambda)
		{
			this.useOWLQN = use;
			this.lambdaOWL = lambda;
		}

		private static double[] ProjectOWL(double[] x, double[] orthant, IFunction func)
		{
			if (func is IHasRegularizerParamRange)
			{
				ICollection<int> paramRange = ((IHasRegularizerParamRange)func).GetRegularizerParamRange(x);
				foreach (int i in paramRange)
				{
					if (x[i] * orthant[i] <= 0.0)
					{
						x[i] = 0.0;
					}
				}
			}
			else
			{
				for (int i = 0; i < x.Length; i++)
				{
					if (x[i] * orthant[i] <= 0.0)
					{
						x[i] = 0.0;
					}
				}
			}
			return x;
		}

		private static double L1NormOWL(double[] x, IFunction func)
		{
			double sum = 0.0;
			if (func is IHasRegularizerParamRange)
			{
				ICollection<int> paramRange = ((IHasRegularizerParamRange)func).GetRegularizerParamRange(x);
				foreach (int i in paramRange)
				{
					sum += System.Math.Abs(x[i]);
				}
			}
			else
			{
				foreach (double v in x)
				{
					sum += System.Math.Abs(v);
				}
			}
			return sum;
		}

		private static void ConstrainSearchDir(double[] dir, double[] fg, double[] x, IFunction func)
		{
			if (func is IHasRegularizerParamRange)
			{
				ICollection<int> paramRange = ((IHasRegularizerParamRange)func).GetRegularizerParamRange(x);
				foreach (int i in paramRange)
				{
					if (dir[i] * fg[i] >= 0.0)
					{
						dir[i] = 0.0;
					}
				}
			}
			else
			{
				for (int i = 0; i < x.Length; i++)
				{
					if (dir[i] * fg[i] >= 0.0)
					{
						dir[i] = 0.0;
					}
				}
			}
		}

		private double[] PseudoGradientOWL(double[] x, double[] grad, IFunction func)
		{
			ICollection<int> paramRange = func is IHasRegularizerParamRange ? ((IHasRegularizerParamRange)func).GetRegularizerParamRange(x) : null;
			double[] newGrad = new double[grad.Length];
			// compute pseudo gradient
			for (int i = 0; i < x.Length; i++)
			{
				if (paramRange == null || paramRange.Contains(i))
				{
					if (x[i] < 0.0)
					{
						// Differentiable
						newGrad[i] = grad[i] - lambdaOWL;
					}
					else
					{
						if (x[i] > 0.0)
						{
							// Differentiable
							newGrad[i] = grad[i] + lambdaOWL;
						}
						else
						{
							if (grad[i] < -lambdaOWL)
							{
								// Take the right partial derivative
								newGrad[i] = grad[i] + lambdaOWL;
							}
							else
							{
								if (grad[i] > lambdaOWL)
								{
									// Take the left partial derivative
									newGrad[i] = grad[i] - lambdaOWL;
								}
								else
								{
									newGrad[i] = 0.0;
								}
							}
						}
					}
				}
				else
				{
					newGrad[i] = grad[i];
				}
			}
			return newGrad;
		}

		/// <summary>lineSearchBacktrackOWL is the linesearch used for L1 regularization.</summary>
		/// <remarks>
		/// lineSearchBacktrackOWL is the linesearch used for L1 regularization.
		/// it only satisfies sufficient descent not the Wolfe conditions.
		/// </remarks>
		/// <exception cref="Edu.Stanford.Nlp.Optimization.QNMinimizer.MaxEvaluationsExceeded"/>
		private double[] LineSearchBacktrackOWL(IFunction func, double[] dir, double[] x, double[] newX, double[] grad, double lastValue, StringBuilder sb)
		{
			/* Choose the orthant for the new point. */
			double[] orthant = new double[x.Length];
			for (int i = 0; i < orthant.Length; i++)
			{
				orthant[i] = (x[i] == 0.0) ? -grad[i] : x[i];
			}
			// c1 can be anything between 0 and 1, exclusive (usu. 1/10 - 1/2)
			double step;
			double c1;
			// for first few steps, we have less confidence in our initial step-size a
			// so scale back quicker
			if (its <= 2)
			{
				step = 0.1;
				c1 = 0.1;
			}
			else
			{
				step = 1.0;
				c1 = 0.1;
			}
			// should be small e.g. 10^-5 ... 10^-1
			double c = 0.01;
			// c = c * normGradInDir;
			double[] newPoint = new double[3];
			while (true)
			{
				PlusAndConstMult(x, dir, step, newX);
				// The current point is projected onto the orthant
				ProjectOWL(newX, orthant, func);
				// step (3) in Galen & Gao 2007
				// Evaluate the function and gradient values
				double value = func.ValueAt(newX);
				// Compute the L1 norm of the variables and add it to the object value
				double norm = L1NormOWL(newX, func);
				value += norm * lambdaOWL;
				newPoint[f] = value;
				double dgtest = 0.0;
				for (int i_1 = 0; i_1 < x.Length; i_1++)
				{
					dgtest += (newX[i_1] - x[i_1]) * grad[i_1];
				}
				if (newPoint[f] <= lastValue + c * dgtest)
				{
					break;
				}
				else
				{
					if (newPoint[f] < lastValue)
					{
						// an improvement, but not good enough... suspicious!
						sb.Append('!');
					}
					else
					{
						sb.Append('.');
					}
				}
				step = c1 * step;
			}
			newPoint[a] = step;
			fevals += 1;
			if (fevals > maxFevals)
			{
				throw new QNMinimizer.MaxEvaluationsExceeded("Exceeded during linesearch() Function.");
			}
			return newPoint;
		}

		/*
		* lineSearchBacktrack is the original line search used for the first version
		* of QNMinimizer. It only satisfies sufficient descent not the Wolfe conditions.
		*/
		/// <exception cref="Edu.Stanford.Nlp.Optimization.QNMinimizer.MaxEvaluationsExceeded"/>
		private double[] LineSearchBacktrack(IFunction func, double[] dir, double[] x, double[] newX, double[] grad, double lastValue, StringBuilder sb)
		{
			double normGradInDir = ArrayMath.InnerProduct(dir, grad);
			sb.Append('(').Append(nf.Format(normGradInDir)).Append(')');
			if (normGradInDir > 0)
			{
				Sayln("{WARNING--- direction of positive gradient chosen!}");
			}
			// c1 can be anything between 0 and 1, exclusive (usu. 1/10 - 1/2)
			double step;
			double c1;
			// for first few steps, we have less confidence in our initial step-size a
			// so scale back quicker
			if (its <= 2)
			{
				step = 0.1;
				c1 = 0.1;
			}
			else
			{
				step = 1.0;
				c1 = 0.1;
			}
			// should be small e.g. 10^-5 ... 10^-1
			double c = 0.01;
			// double v = func.valueAt(x);
			// c = c * mult(grad, dir);
			c = c * normGradInDir;
			double[] newPoint = new double[3];
			while ((newPoint[f] = func.ValueAt((PlusAndConstMult(x, dir, step, newX)))) > lastValue + c * step)
			{
				fevals += 1;
				if (newPoint[f] < lastValue)
				{
					// an improvement, but not good enough... suspicious!
					sb.Append('!');
				}
				else
				{
					sb.Append('.');
				}
				step = c1 * step;
			}
			newPoint[a] = step;
			fevals += 1;
			if (fevals > maxFevals)
			{
				throw new QNMinimizer.MaxEvaluationsExceeded("Exceeded during lineSearch() Function.");
			}
			return newPoint;
		}

		/// <exception cref="Edu.Stanford.Nlp.Optimization.QNMinimizer.MaxEvaluationsExceeded"/>
		private double[] LineSearchMinPack(IDiffFunction dfunc, double[] dir, double[] x, double[] newX, double[] grad, double f0, double tol, StringBuilder sb)
		{
			double xtrapf = 4.0;
			int info = 0;
			int infoc = 1;
			bracketed = false;
			bool stage1 = true;
			double width = aMax - aMin;
			double width1 = 2 * width;
			// double[] wa = x;
			// Should check input parameters
			double g0 = ArrayMath.InnerProduct(grad, dir);
			if (g0 >= 0)
			{
				// We're looking in a direction of positive gradient. This won't work.
				// set dir = -grad
				for (int i = 0; i < x.Length; i++)
				{
					dir[i] = -grad[i];
				}
				g0 = ArrayMath.InnerProduct(grad, dir);
			}
			double gTest = ftol * g0;
			double[] newPt = new double[3];
			double[] bestPt = new double[3];
			double[] endPt = new double[3];
			newPt[a] = 1.0;
			// Always guess 1 first, this should be right if the
			// function is "nice" and BFGS is working.
			if (its == 1 && noHistory)
			{
				newPt[a] = 1e-1;
			}
			bestPt[a] = 0.0;
			bestPt[f] = f0;
			bestPt[g] = g0;
			endPt[a] = 0.0;
			endPt[f] = f0;
			endPt[g] = g0;
			do
			{
				// int cnt = 0;
				double stpMin;
				// = aMin; [cdm: this initialization was always overridden below]
				double stpMax;
				// = aMax; [cdm: this initialization was always overridden below]
				if (bracketed)
				{
					stpMin = System.Math.Min(bestPt[a], endPt[a]);
					stpMax = System.Math.Max(bestPt[a], endPt[a]);
				}
				else
				{
					stpMin = bestPt[a];
					stpMax = newPt[a] + xtrapf * (newPt[a] - bestPt[a]);
				}
				newPt[a] = System.Math.Max(newPt[a], aMin);
				newPt[a] = System.Math.Min(newPt[a], aMax);
				// Use the best point if we have some sort of strange termination
				// conditions.
				if ((bracketed && (newPt[a] <= stpMin || newPt[a] >= stpMax)) || fevals >= maxFevals || infoc == 0 || (bracketed && stpMax - stpMin <= tol * stpMax))
				{
					// todo: below..
					PlusAndConstMult(x, dir, bestPt[a], newX);
					newPt[f] = bestPt[f];
					newPt[a] = bestPt[a];
				}
				newPt[f] = dfunc.ValueAt((PlusAndConstMult(x, dir, newPt[a], newX)));
				newPt[g] = ArrayMath.InnerProduct(dfunc.DerivativeAt(newX), dir);
				double fTest = f0 + newPt[a] * gTest;
				fevals += 1;
				// Check and make sure everything is normal.
				if ((bracketed && (newPt[a] <= stpMin || newPt[a] >= stpMax)) || infoc == 0)
				{
					info = 6;
					Sayln(" line search failure: bracketed but no feasible found ");
				}
				if (newPt[a] == aMax && newPt[f] <= fTest && newPt[g] <= gTest)
				{
					info = 5;
					Sayln(" line search failure: sufficient decrease, but gradient is more negative ");
				}
				if (newPt[a] == aMin && (newPt[f] > fTest || newPt[g] >= gTest))
				{
					info = 4;
					Sayln(" line search failure: minimum step length reached ");
				}
				if (fevals >= maxFevals)
				{
					// info = 3;
					throw new QNMinimizer.MaxEvaluationsExceeded("Exceeded during lineSearchMinPack() Function.");
				}
				if (bracketed && stpMax - stpMin <= tol * stpMax)
				{
					info = 2;
					Sayln(" line search failure: interval is too small ");
				}
				if (newPt[f] <= fTest && System.Math.Abs(newPt[g]) <= -gtol * g0)
				{
					info = 1;
				}
				if (info != 0)
				{
					return newPt;
				}
				// this is the first stage where we look for a point that is lower and
				// increasing
				if (stage1 && newPt[f] <= fTest && newPt[g] >= System.Math.Min(ftol, gtol) * g0)
				{
					stage1 = false;
				}
				// A modified function is used to predict the step only if
				// we have not obtained a step for which the modified
				// function has a non-positive function value and non-negative
				// derivative, and if a lower function value has been
				// obtained but the decrease is not sufficient.
				if (stage1 && newPt[f] <= bestPt[f] && newPt[f] > fTest)
				{
					newPt[f] = newPt[f] - newPt[a] * gTest;
					bestPt[f] = bestPt[f] - bestPt[a] * gTest;
					endPt[f] = endPt[f] - endPt[a] * gTest;
					newPt[g] = newPt[g] - gTest;
					bestPt[g] = bestPt[g] - gTest;
					endPt[g] = endPt[g] - gTest;
					infoc = GetStep(newPt, bestPt, endPt, stpMin, stpMax, sb);
					/* x, dir, newX, f0, g0, */
					bestPt[f] = bestPt[f] + bestPt[a] * gTest;
					endPt[f] = endPt[f] + endPt[a] * gTest;
					bestPt[g] = bestPt[g] + gTest;
					endPt[g] = endPt[g] + gTest;
				}
				else
				{
					infoc = GetStep(newPt, bestPt, endPt, stpMin, stpMax, sb);
				}
				/* x, dir, newX, f0, g0, */
				if (bracketed)
				{
					if (System.Math.Abs(endPt[a] - bestPt[a]) >= p66 * width1)
					{
						newPt[a] = bestPt[a] + p5 * (endPt[a] - bestPt[a]);
					}
					width1 = width;
					width = System.Math.Abs(endPt[a] - bestPt[a]);
				}
			}
			while (true);
		}

		/// <summary>
		/// getStep()
		/// THIS FUNCTION IS A TRANSLATION OF A TRANSLATION OF THE MINPACK SUBROUTINE
		/// cstep().
		/// </summary>
		/// <remarks>
		/// getStep()
		/// THIS FUNCTION IS A TRANSLATION OF A TRANSLATION OF THE MINPACK SUBROUTINE
		/// cstep(). Dianne O'Leary July 1991
		/// It was then interpreted from the implementation supplied by Andrew
		/// Bradley. Modifications have been made for this particular application.
		/// This function is used to find a new safe guarded step to be used for
		/// line search procedures.
		/// </remarks>
		/// <exception cref="Edu.Stanford.Nlp.Optimization.QNMinimizer.MaxEvaluationsExceeded"/>
		private int GetStep(double[] newPt, double[] bestPt, double[] endPt, double stpMin, double stpMax, StringBuilder sb)
		{
			/* double[] x, double[] dir, double[] newX, double f0,
			double g0, // None of these were used */
			// Should check for input errors.
			int info;
			// = 0; always set in the if below
			bool bound;
			// = false; always set in the if below
			double theta;
			double gamma;
			double p;
			double q;
			double r;
			double s;
			double stpc;
			double stpq;
			double stpf;
			double signG = newPt[g] * bestPt[g] / System.Math.Abs(bestPt[g]);
			//
			// First case. A higher function value.
			// The minimum is bracketed. If the cubic step is closer
			// to stx than the quadratic step, the cubic step is taken,
			// else the average of the cubic and quadratic steps is taken.
			//
			if (newPt[f] > bestPt[f])
			{
				info = 1;
				bound = true;
				theta = 3 * (bestPt[f] - newPt[f]) / (newPt[a] - bestPt[a]) + bestPt[g] + newPt[g];
				s = System.Math.Max(System.Math.Max(theta, newPt[g]), bestPt[g]);
				gamma = s * System.Math.Sqrt((theta / s) * (theta / s) - (bestPt[g] / s) * (newPt[g] / s));
				if (newPt[a] < bestPt[a])
				{
					gamma = -gamma;
				}
				p = (gamma - bestPt[g]) + theta;
				q = ((gamma - bestPt[g]) + gamma) + newPt[g];
				r = p / q;
				stpc = bestPt[a] + r * (newPt[a] - bestPt[a]);
				stpq = bestPt[a] + ((bestPt[g] / ((bestPt[f] - newPt[f]) / (newPt[a] - bestPt[a]) + bestPt[g])) / 2) * (newPt[a] - bestPt[a]);
				if (System.Math.Abs(stpc - bestPt[a]) < System.Math.Abs(stpq - bestPt[a]))
				{
					stpf = stpc;
				}
				else
				{
					stpf = stpq;
				}
				// stpf = stpc + (stpq - stpc)/2;
				bracketed = true;
				if (newPt[a] < 0.1)
				{
					stpf = 0.01 * stpf;
				}
			}
			else
			{
				if (signG < 0.0)
				{
					//
					// Second case. A lower function value and derivatives of
					// opposite sign. The minimum is bracketed. If the cubic
					// step is closer to stx than the quadratic (secant) step,
					// the cubic step is taken, else the quadratic step is taken.
					//
					info = 2;
					bound = false;
					theta = 3 * (bestPt[f] - newPt[f]) / (newPt[a] - bestPt[a]) + bestPt[g] + newPt[g];
					s = System.Math.Max(System.Math.Max(theta, bestPt[g]), newPt[g]);
					gamma = s * System.Math.Sqrt((theta / s) * (theta / s) - (bestPt[g] / s) * (newPt[g] / s));
					if (newPt[a] > bestPt[a])
					{
						gamma = -gamma;
					}
					p = (gamma - newPt[g]) + theta;
					q = ((gamma - newPt[g]) + gamma) + bestPt[g];
					r = p / q;
					stpc = newPt[a] + r * (bestPt[a] - newPt[a]);
					stpq = newPt[a] + (newPt[g] / (newPt[g] - bestPt[g])) * (bestPt[a] - newPt[a]);
					if (System.Math.Abs(stpc - newPt[a]) > System.Math.Abs(stpq - newPt[a]))
					{
						stpf = stpc;
					}
					else
					{
						stpf = stpq;
					}
					bracketed = true;
				}
				else
				{
					if (System.Math.Abs(newPt[g]) < System.Math.Abs(bestPt[g]))
					{
						//
						// Third case. A lower function value, derivatives of the
						// same sign, and the magnitude of the derivative decreases.
						// The cubic step is only used if the cubic tends to infinity
						// in the direction of the step or if the minimum of the cubic
						// is beyond stp. Otherwise the cubic step is defined to be
						// either stpmin or stpmax. The quadratic (secant) step is also
						// computed and if the minimum is bracketed then the the step
						// closest to stx is taken, else the step farthest away is taken.
						//
						info = 3;
						bound = true;
						theta = 3 * (bestPt[f] - newPt[f]) / (newPt[a] - bestPt[a]) + bestPt[g] + newPt[g];
						s = System.Math.Max(System.Math.Max(theta, bestPt[g]), newPt[g]);
						gamma = s * System.Math.Sqrt(System.Math.Max(0.0, (theta / s) * (theta / s) - (bestPt[g] / s) * (newPt[g] / s)));
						if (newPt[a] < bestPt[a])
						{
							gamma = -gamma;
						}
						p = (gamma - bestPt[g]) + theta;
						q = ((gamma - bestPt[g]) + gamma) + newPt[g];
						r = p / q;
						if (r < 0.0 && gamma != 0.0)
						{
							stpc = newPt[a] + r * (bestPt[a] - newPt[a]);
						}
						else
						{
							if (newPt[a] > bestPt[a])
							{
								stpc = stpMax;
							}
							else
							{
								stpc = stpMin;
							}
						}
						stpq = newPt[a] + (newPt[g] / (newPt[g] - bestPt[g])) * (bestPt[a] - newPt[a]);
						if (bracketed)
						{
							if (System.Math.Abs(newPt[a] - stpc) < System.Math.Abs(newPt[a] - stpq))
							{
								stpf = stpc;
							}
							else
							{
								stpf = stpq;
							}
						}
						else
						{
							if (System.Math.Abs(newPt[a] - stpc) > System.Math.Abs(newPt[a] - stpq))
							{
								stpf = stpc;
							}
							else
							{
								stpf = stpq;
							}
						}
					}
					else
					{
						//
						// Fourth case. A lower function value, derivatives of the
						// same sign, and the magnitude of the derivative does
						// not decrease. If the minimum is not bracketed, the step
						// is either stpmin or stpmax, else the cubic step is taken.
						//
						info = 4;
						bound = false;
						if (bracketed)
						{
							theta = 3 * (bestPt[f] - newPt[f]) / (newPt[a] - bestPt[a]) + bestPt[g] + newPt[g];
							s = System.Math.Max(System.Math.Max(theta, bestPt[g]), newPt[g]);
							gamma = s * System.Math.Sqrt((theta / s) * (theta / s) - (bestPt[g] / s) * (newPt[g] / s));
							if (newPt[a] > bestPt[a])
							{
								gamma = -gamma;
							}
							p = (gamma - newPt[g]) + theta;
							q = ((gamma - newPt[g]) + gamma) + bestPt[g];
							r = p / q;
							stpc = newPt[a] + r * (bestPt[a] - newPt[a]);
							stpf = stpc;
						}
						else
						{
							if (newPt[a] > bestPt[a])
							{
								stpf = stpMax;
							}
							else
							{
								stpf = stpMin;
							}
						}
					}
				}
			}
			//
			// Update the interval of uncertainty. This update does not
			// depend on the new step or the case analysis above.
			//
			if (newPt[f] > bestPt[f])
			{
				Copy(newPt, endPt);
			}
			else
			{
				if (signG < 0.0)
				{
					Copy(bestPt, endPt);
				}
				Copy(newPt, bestPt);
			}
			sb.Append(info.ToString());
			//
			// Compute the new step and safeguard it.
			//
			stpf = System.Math.Min(stpMax, stpf);
			stpf = System.Math.Max(stpMin, stpf);
			newPt[a] = stpf;
			if (bracketed && bound)
			{
				if (endPt[a] > bestPt[a])
				{
					newPt[a] = System.Math.Min(bestPt[a] + p66 * (endPt[a] - bestPt[a]), newPt[a]);
				}
				else
				{
					newPt[a] = System.Math.Max(bestPt[a] + p66 * (endPt[a] - bestPt[a]), newPt[a]);
				}
			}
			return info;
		}

		private static void Copy(double[] src, double[] dest)
		{
			System.Array.Copy(src, 0, dest, 0, src.Length);
		}
		//
		//
		//
		// private double[] lineSearchNocedal(DiffFunction dfunc, double[] dir,
		// double[] x, double[] newX, double[] grad, double f0) throws
		// MaxEvaluationsExceeded {
		//
		//
		// double g0 = ArrayMath.innerProduct(grad,dir);
		// if(g0 > 0){
		// //We're looking in a direction of positive gradient. This wont' work.
		// //set dir = -grad
		// plusAndConstMult(new double[x.length],grad,-1,dir);
		// g0 = ArrayMath.innerProduct(grad,dir);
		// }
		// say("(" + nf.format(g0) + ")");
		//
		//
		// double[] newPoint = new double[3];
		// double[] prevPoint = new double[3];
		// newPoint[a] = 1.0; //Always guess 1 first, this should be right if the
		// function is "nice" and BFGS is working.
		//
		// //Special guess for the first iteration.
		// if(its == 1){
		// double aLin = - f0 / (ftol*g0);
		// //Keep aLin within aMin and 1 for the first guess. But make a more
		// intelligent guess based off the gradient
		// aLin = Math.min(1.0, aLin);
		// aLin = Math.max(aMin, aLin);
		// newPoint[a] = aLin; // Guess low at first since we have no idea of scale at
		// first.
		// }
		//
		// prevPoint[a] = 0.0;
		// prevPoint[f] = f0;
		// prevPoint[g] = g0;
		//
		// int cnt = 0;
		//
		// do{
		// newPoint[f] = dfunc.valueAt((plusAndConstMult(x, dir, newPoint[a], newX)));
		// newPoint[g] = ArrayMath.innerProduct(dfunc.derivativeAt(newX),dir);
		// fevals += 1;
		//
		// //If fNew > f0 + small*aNew*g0 or fNew > fPrev
		// if( (newPoint[f] > f0 + ftol*newPoint[a]*g0) || newPoint[f] > prevPoint[f]
		// ){
		// //We know there must be a point that satisfies the strong wolfe conditions
		// between
		// //the previous and new point, so search between these points.
		// say("->");
		// return zoom(dfunc,x,dir,newX,f0,g0,prevPoint,newPoint);
		// }
		//
		// //Here we check if the magnitude of the gradient has decreased, if
		// //it is more negative we can expect to find a much better point
		// //by stepping a little farther.
		//
		// //If |gNew| < 0.9999 |g0|
		// if( Math.abs(newPoint[g]) <= -gtol*g0 ){
		// //This is exactly what we wanted
		// return newPoint;
		// }
		//
		// if (newPoint[g] > 0){
		// //Hmm, our step is too big to be a satisfying point, lets look backwards.
		// say("<-");//say("^");
		//
		// return zoom(dfunc,x,dir,newX,f0,g0,newPoint,prevPoint);
		// }
		//
		// //if we made it here, our function value has decreased enough, but the
		// gradient is more negative.
		// //we should increase our step size, since we have potential to decrease the
		// function
		// //value a lot more.
		// newPoint[a] *= 10; // this is stupid, we should interpolate it. since we
		// already have info for quadratic at least.
		// newPoint[f] = Double.NaN;
		// newPoint[g] = Double.NaN;
		// cnt +=1;
		// say("*");
		//
		// //if(cnt > 10 || fevals > maxFevals){
		// if(fevals > maxFevals){ throw new MaxEvaluationsExceeded(" Exceeded during
		// zoom() Function ");}
		//
		// if(newPoint[a] > aMax){
		// log.info(" max stepsize reached. This is unusual. ");
		// System.exit(1);
		// }
		//
		// }while(true);
		//
		// }
		// private double interpolate( double[] point0, double[] point1){
		// double newAlpha;
		// double intvl = Math.abs(point0[a] -point1[a]);
		// //if(point2 == null){
		// if( Double.isNaN(point0[g]) ){
		// //We dont know the gradient at aLow so do bisection
		// newAlpha = 0.5*(point0[a] + point1[a]);
		// }else{
		// //We know the gradient so do Quadratic 2pt
		// newAlpha = interpolateQuadratic2pt(point0,point1);
		// }
		// //If the newAlpha is outside of the bounds just do bisection.
		// if( ((newAlpha > point0[a]) && (newAlpha > point1[a])) ||
		// ((newAlpha < point0[a]) && (newAlpha < point1[a])) ){
		// //bisection.
		// return 0.5*(point0[a] + point1[a]);
		// }
		// //If we aren't moving fast enough, revert to bisection.
		// if( ((newAlpha/intvl) < 1e-6) || ((newAlpha/intvl) > (1- 1e-6)) ){
		// //say("b");
		// return 0.5*(point0[a] + point1[a]);
		// }
		// return newAlpha;
		// }
		/*
		* private double interpolate( List<double[]> pointList ,) {
		*
		* int n = pointList.size(); double newAlpha = 0.0;
		*
		* if( n > 2){ newAlpha =
		* interpolateCubic(pointList.get(0),pointList.get(n-2),pointList.get(n-1));
		* }else if(n == 2){
		*
		* //Only have two points
		*
		* if( Double.isNaN(pointList.get(0)[gInd]) ){ // We don't know the gradient at
		* aLow so do bisection newAlpha = 0.5*(pointList.get(0)[aInd] +
		* pointList.get(1)[aInd]); }else{ // We know the gradient so do Quadratic 2pt
		* newAlpha = interpolateQuadratic2pt(pointList.get(0),pointList.get(1)); }
		*
		* }else { //not enough info to interpolate with!
		* log.info("QNMinimizer:interpolate() attempt to interpolate with
		* only one point."); System.exit(1); }
		*
		* return newAlpha;
		*  }
		*/
		// Returns the minimizer of a quadratic running through point (a0,f0) with
		// derivative g0 and passing through (a1,f1).
		// private double interpolateQuadratic2pt(double[] pt0, double[] pt1){
		// if( Double.isNaN(pt0[g]) ){
		// log.info("QNMinimizer:interpolateQuadratic - Gradient at point
		// zero doesn't exist, interpolation failed");
		// System.exit(1);
		// }
		// double aDif = pt1[a]-pt0[a];
		// double fDif = pt1[f]-pt0[f];
		// return (- pt0[g]*aDif*aDif)/(2*(fDif-pt0[g]*aDif)) + pt0[a];
		// }
		// private double interpolateCubic(double[] pt0, double[] pt1, double[] pt2){
		// double a0 = pt1[a]-pt0[a];
		// double a1 = pt2[a]-pt0[a];
		// double f0 = pt1[f]-pt0[f];
		// double f1 = pt2[f]-pt0[f];
		// double g0 = pt0[g];
		// double[][] mat = new double[2][2];
		// double[] rhs = new double[2];
		// double[] coefs = new double[2];
		// double scale = 1/(a0*a0*a1*a1*(a1-a0));
		// mat[0][0] = a0*a0;
		// mat[0][1] = -a1*a1;
		// mat[1][0] = -a0*a0*a0;
		// mat[1][1] = a1*a1*a1;
		// rhs[0] = f1 - g0*a1;
		// rhs[1] = f0 - g0*a0;
		// for(int i=0;i<2;i++){
		// for(int j=0;j<2;j++){
		// coefs[i] += mat[i][j]*rhs[j];
		// }
		// coefs[i] *= scale;
		// }
		// double a = coefs[0];
		// double b = coefs[1];
		// double root = b*b-3*a*g0;
		// if( root < 0 ){
		// log.info("QNminimizer:interpolateCubic - interpolate failed");
		// System.exit(1);
		// }
		// return (-b+Math.sqrt(root))/(3*a);
		// }
		// private double[] zoom(DiffFunction dfunc, double[] x, double[] dir,
		// double[] newX, double f0, double g0, double[] bestPoint, double[] endPoint)
		// throws MaxEvaluationsExceeded {
		// return zoom(dfunc,x, dir, newX,f0,g0, bestPoint, endPoint,null);
		// }
		// private double[] zoom(DiffFunction dfunc, double[] x, double[] dir,
		// double[] newX, double f0, double g0, double[] bestPt, double[] endPt,
		// double[] newPt) throws MaxEvaluationsExceeded {
		// double width = Math.abs(bestPt[a] - endPt[a]);
		// double reduction = 1.0;
		// double p66 = 0.66;
		// int info = 0;
		// double stpf;
		// double theta,gamma,s,p,q,r,stpc,stpq;
		// boolean bound = false;
		// boolean bracketed = false;
		// int cnt = 1;
		// if(newPt == null){ newPt = new double[3]; newPt[a] =
		// interpolate(bestPt,endPt);}// quadratic interp
		// do{
		// say(".");
		// newPt[f] = dfunc.valueAt((plusAndConstMult(x, dir, newPt[a] , newX)));
		// newPt[g] = ArrayMath.innerProduct(dfunc.derivativeAt(newX),dir);
		// fevals += 1;
		// //If we have satisfied Wolfe...
		// //fNew <= f0 + small*aNew*g0
		// //|gNew| <= 0.9999*|g0|
		// //return the point.
		// if( (newPt[f] <= f0 + ftol*newPt[a]*g0) && Math.abs(newPt[g]) <= -gtol*g0
		// ){
		// //Sweet, we found a point that satisfies the strong wolfe conditions!!!
		// lets return it.
		// return newPt;
		// }else{
		// double signG = newPt[g]*bestPt[g]/Math.abs(bestPt[g]);
		// //Our new point has a higher function value
		// if( newPt[f] > bestPt[f]){
		// info = 1;
		// bound = true;
		// theta = 3*(bestPt[f] - newPt[f])/(newPt[a] - bestPt[a]) + bestPt[g] +
		// newPt[g];
		// s = Math.max(Math.max(theta,newPt[g]), bestPt[g]);
		// gamma = s*Math.sqrt( (theta/s)*(theta/s) - (bestPt[g]/s)*(newPt[g]/s) );
		// if (newPt[a] < bestPt[a]){
		// gamma = -gamma;
		// }
		// p = (gamma - bestPt[g]) + theta;
		// q = ((gamma-bestPt[g]) + gamma) + newPt[g];
		// r = p/q;
		// stpc = bestPt[a] + r*(newPt[a] - bestPt[a]);
		// stpq = bestPt[a] +
		// ((bestPt[g]/((bestPt[f]-newPt[f])/(newPt[a]-bestPt[a])+bestPt[g]))/2)*(newPt[a]
		// - bestPt[a]);
		// if ( Math.abs(stpc-bestPt[a]) < Math.abs(stpq - bestPt[a] )){
		// stpf = stpc;
		// } else{
		// stpf = stpq;
		// //stpf = stpc + (stpq - stpc)/2;
		// }
		// bracketed = true;
		// if (newPt[a] < 0.1){
		// stpf = 0.01*stpf;
		// }
		// } else if (signG < 0.0){
		// info = 2;
		// bound = false;
		// theta = 3*(bestPt[f] - newPt[f])/(newPt[a] - bestPt[a]) + bestPt[g] +
		// newPt[g];
		// s = Math.max(Math.max(theta,bestPt[g]),newPt[g]);
		// gamma = s*Math.sqrt((theta/s)*(theta/s) - (bestPt[g]/s)*(newPt[g]/s));
		// if (newPt[a] > bestPt[a]) {
		// gamma = -gamma;
		// }
		// p = (gamma - newPt[g]) + theta;
		// q = ((gamma - newPt[g]) + gamma) + bestPt[g];
		// r = p/q;
		// stpc = newPt[a] + r*(bestPt[a] - newPt[a]);
		// stpq = newPt[a] + (newPt[g]/(newPt[g]-bestPt[g]))*(bestPt[a] - newPt[a]);
		// if (Math.abs(stpc-newPt[a]) > Math.abs(stpq-newPt[a])){
		// stpf = stpc;
		// } else {
		// stpf = stpq;
		// }
		// bracketed = true;
		// } else if ( Math.abs(newPt[g]) < Math.abs(bestPt[g])){
		// info = 3;
		// bound = true;
		// theta = 3*(bestPt[f] - newPt[f])/(newPt[a] - bestPt[a]) + bestPt[g] +
		// newPt[g];
		// s = Math.max(Math.max(theta,bestPt[g]),newPt[g]);
		// gamma = s*Math.sqrt(Math.max(0.0,(theta/s)*(theta/s) -
		// (bestPt[g]/s)*(newPt[g]/s)));
		// if (newPt[a] < bestPt[a]){
		// gamma = -gamma;
		// }
		// p = (gamma - bestPt[g]) + theta;
		// q = ((gamma-bestPt[g]) + gamma) + newPt[g];
		// r = p/q;
		// if (r < 0.0 && gamma != 0.0){
		// stpc = newPt[a] + r*(bestPt[a] - newPt[a]);
		// } else if (newPt[a] > bestPt[a]){
		// stpc = aMax;
		// } else{
		// stpc = aMin;
		// }
		// stpq = newPt[a] + (newPt[g]/(newPt[g]-bestPt[g]))*(bestPt[a] - newPt[a]);
		// if(bracketed){
		// if (Math.abs(newPt[a]-stpc) < Math.abs(newPt[a]-stpq)){
		// stpf = stpc;
		// } else {
		// stpf = stpq;
		// }
		// } else{
		// if (Math.abs(newPt[a]-stpc) > Math.abs(newPt[a]-stpq)){
		// stpf = stpc;
		// } else {
		// stpf = stpq;
		// }
		// }
		// }else{
		// info = 4;
		// bound = false;
		// if (bracketed){
		// theta = 3*(bestPt[f] - newPt[f])/(newPt[a] - bestPt[a]) + bestPt[g] +
		// newPt[g];
		// s = Math.max(Math.max(theta,bestPt[g]),newPt[g]);
		// gamma = s*Math.sqrt((theta/s)*(theta/s) - (bestPt[g]/s)*(newPt[g]/s));
		// if (newPt[a] > bestPt[a]) {
		// gamma = -gamma;
		// }
		// p = (gamma - newPt[g]) + theta;
		// q = ((gamma - newPt[g]) + gamma) + bestPt[g];
		// r = p/q;
		// stpc = newPt[a] + r*(bestPt[a] - newPt[a]);
		// stpf = stpc;
		// }else if( newPt[a] > bestPt[a]){
		// stpf = aMax;
		// } else {
		// stpf = aMin;
		// }
		// }
		// //Reduce the interval of uncertainty
		// if (newPt[f] > bestPt[f]) {
		// copy(newPt,endPt);
		// }else{
		// if (signG < 0.0){
		// copy(bestPt,endPt);
		// }
		// copy(newPt,bestPt);
		// }
		// say("" + info );
		// newPt[a] = stpf;
		// if(bracketed && bound){
		// if (endPt[a] > bestPt[a]){
		// newPt[a] = Math.min(bestPt[a]+p66*(endPt[a]-bestPt[a]),newPt[a]);
		// }else{
		// newPt[a] = Math.max(bestPt[a]+p66*(endPt[a]-bestPt[a]),newPt[a]);
		// }
		// }
		// }
		// //Check to see if the step has reached an extreme.
		// newPt[a] = Math.max(aMin, newPt[a]);
		// newPt[a] = Math.min(aMax,newPt[a]);
		// if( newPt[a] == aMin || newPt[a] == aMax){
		// return newPt;
		// }
		// cnt +=1;
		// if(fevals > maxFevals){
		// throw new MaxEvaluationsExceeded(" Exceeded during zoom() Function ");}
		// }while(true);
		// }
		// private double[] zoom2(DiffFunction dfunc, double[] x, double[] dir,
		// double[] newX, double f0, double g0, double[] bestPoint, double[] endPoint)
		// throws MaxEvaluationsExceeded {
		//
		// double[] newPoint = new double[3];
		// double width = Math.abs(bestPoint[a] - endPoint[a]);
		// double reduction = 0.0;
		//
		// int cnt = 1;
		//
		// //make sure the interval reduces enough.
		// //if(reduction >= 0.66){
		// //say(" |" + nf.format(reduction)+"| ");
		// //newPoint[a] = 0.5*(bestPoint[a]+endPoint[a]);
		// //} else{
		// newPoint[a] = interpolate(bestPoint,endPoint);// quadratic interp
		// //}
		//
		// do{
		// //Check to see if the step has reached an extreme.
		// newPoint[a] = Math.max(aMin, newPoint[a]);
		// newPoint[a] = Math.min(aMax,newPoint[a]);
		//
		// newPoint[f] = dfunc.valueAt((plusAndConstMult(x, dir, newPoint[a] ,
		// newX)));
		// newPoint[g] = ArrayMath.innerProduct(dfunc.derivativeAt(newX),dir);
		// fevals += 1;
		//
		// //fNew > f0 + small*aNew*g0 or fNew > fLow
		// if( (newPoint[f] > f0 + ftol*newPoint[a]*g0) || newPoint[f] > bestPoint[f]
		// ){
		// //Our new point didn't beat the best point, so just reduce the interval
		// copy(newPoint,endPoint);
		// say(".");//say("l");
		// }else{
		//
		// //if |gNew| <= 0.9999*|g0| If gNew is slightly smaller than g0
		// if( Math.abs(newPoint[g]) <= -gtol*g0 ){
		// //Sweet, we found a point that satisfies the strong wolfe conditions!!!
		// lets return it.
		// return newPoint;
		// }
		//
		// //If we made it this far, we've found a point that has satisfied descent,
		// but hasn't satsified
		// //the decrease in gradient. if the new gradient is telling us >0 we need to
		// look behind us
		// //if the new gradient is negative still we can increase the step.
		// if(newPoint[g]*(endPoint[a] - bestPoint[a] ) >= 0){
		// //Get going the right way.
		// say(".");//say("f");
		// copy(bestPoint,endPoint);
		// }
		//
		// if( (Math.abs(newPoint[a]-bestPoint[a]) < 1e-6) ||
		// (Math.abs(newPoint[a]-endPoint[a]) < 1e-6) ){
		// //Not moving fast enough.
		// sayln("had to improvise a bit");
		// newPoint[a] = 0.5*(bestPoint[a] + endPoint[a]);
		// }
		//
		// say(".");//say("r");
		// copy(newPoint,bestPoint);
		// }
		//
		//
		// if( newPoint[a] == aMin || newPoint[a] == aMax){
		// return newPoint;
		// }
		//
		// reduction = Math.abs(bestPoint[a] - endPoint[a]) / width;
		// width = Math.abs(bestPoint[a] - endPoint[a]);
		//
		// cnt +=1;
		//
		//
		// //if(Math.abs(bestPoint[a] -endPoint[a]) < 1e-12 ){
		// //sayln();
		// //sayln("!!!!!!!!!!!!!!!!!!");
		// //sayln("points are too close");
		// //sayln("!!!!!!!!!!!!!!!!!!");
		// //sayln("f0 " + nf.format(f0));
		// //sayln("f0+crap " + nf.format(f0 + cVal*bestPoint[a]*g0));
		// //sayln("g0 " + nf.format(g0));
		// //sayln("ptLow");
		// //printPt(bestPoint);
		// //sayln();
		// //sayln("ptHigh");
		// //printPt(endPoint);
		// //sayln();
		//
		// //DiffFunctionTester.test(dfunc, x,1e-4);
		// //System.exit(1);
		// ////return dfunc.valueAt((plusAndConstMult(x, dir, aMin , newX)));
		// //}
		//
		// //if( (cnt > 20) ){
		//
		// //sayln("!!!!!!!!!!!!!!!!!!");
		// //sayln("! " + cnt + " iterations. I think we're out of luck");
		// //sayln("!!!!!!!!!!!!!!!!!!");
		// //sayln("f0" + nf.format(f0));
		// //sayln("f0+crap" + nf.format(f0 + cVal*bestPoint[a]*g0));
		// //sayln("g0 " + nf.format(g0));
		// //sayln("bestPoint");
		// //printPt(bestPoint);
		// //sayln();
		// //sayln("ptHigh");
		// //printPt(endPoint);
		// //sayln();
		//
		//
		//
		// ////if( cnt > 25 || fevals > maxFevals){
		// ////log.info("Max evaluations exceeded.");
		// ////System.exit(1);
		// ////return dfunc.valueAt((plusAndConstMult(x, dir, aMin , newX)));
		// ////}
		// //}
		//
		// if(fevals > maxFevals){ throw new MaxEvaluationsExceeded(" Exceeded during
		// zoom() Function ");}
		//
		// }while(true);
		//
		// }
		//
		// private double lineSearchNocedal(DiffFunction dfunc, double[] dir, double[]
		// x, double[] newX, double[] grad, double f0, int maxEvals){
		//
		// boolean bracketed = false;
		// boolean stage1 = false;
		// double width = aMax - aMin;
		// double width1 = 2*width;
		// double stepMin = 0.0;
		// double stepMax = 0.0;
		// double xtrapf = 4.0;
		// int nFevals = 0;
		// double TOL = 1e-4;
		// double X_TOL = 1e-8;
		// int info = 0;
		// int infoc = 1;
		//
		// double g0 = ArrayMath.innerProduct(grad,dir);
		// if(g0 > 0){
		// //We're looking in a direction of positive gradient. This wont' work.
		// //set dir = -grad
		// plusAndConstMult(new double[x.length],grad,-1,dir);
		// g0 = ArrayMath.innerProduct(grad,dir);
		// log.info("Searching in direction of positive gradient.");
		// }
		// say("(" + nf.format(g0) + ")");
		//
		//
		// double[] newPt = new double[3];
		// double[] bestPt = new double[3];
		// double[] endPt = new double[3];
		//
		// newPt[a] = 1.0; //Always guess 1 first, this should be right if the
		// function is "nice" and BFGS is working.
		//
		// if(its == 1){
		// newPt[a] = 1e-6; // Guess low at first since we have no idea of scale.
		// }
		//
		// bestPt[a] = 0.0;
		// bestPt[f] = f0;
		// bestPt[g] = g0;
		//
		// endPt[a] = 0.0;
		// endPt[f] = f0;
		// endPt[g] = g0;
		//
		// int cnt = 0;
		//
		// do{
		// //Determine the max and min step size given what we know already.
		// if(bracketed){
		// stepMin = Math.min(bestPt[a], endPt[a]);
		// stepMax = Math.max(bestPt[a], endPt[a]);
		// } else{
		// stepMin = bestPt[a];
		// stepMax = newPt[a] + xtrapf*(newPt[a] - bestPt[a]);
		// }
		//
		// //Make sure our next guess is within the bounds
		// newPt[a] = Math.max(newPt[a], stepMin);
		// newPt[a] = Math.min(newPt[a], stepMax);
		//
		// if( (bracketed && (newPt[a] <= stepMin || newPt[a] >= stepMax) )
		// || nFevals > maxEvals || (bracketed & (stepMax-stepMin) <= TOL*stepMax)){
		// log.info("Linesearch for QN, Need to make srue that newX is set
		// before returning bestPt. -akleeman");
		// System.exit(1);
		// return bestPt[f];
		// }
		//
		//
		// newPt[f] = dfunc.valueAt((plusAndConstMult(x, dir, newPt[a], newX)));
		// newPt[g] = ArrayMath.innerProduct(dfunc.derivativeAt(newX),dir);
		// nFevals += 1;
		//
		// double fTest = f0 + newPt[a]*g0;
		//
		// log.info("fTest " + fTest + " new" + newPt[a] + " newf" +
		// newPt[f] + " newg" + newPt[g] );
		//
		// if( ( bracketed && (newPt[a] <= stepMin | newPt[a] >= stepMax )) || infoc
		// == 0){
		// info = 6;
		// }
		//
		// if( newPt[a] == stepMax && ( newPt[f] <= fTest || newPt[g] >= ftol*g0 )){
		// info = 5;
		// }
		//
		// if( (newPt[a] == stepMin && ( newPt[f] > fTest || newPt[g] >= ftol*g0 ) )){
		// info = 4;
		// }
		//
		// if( (nFevals >= maxEvals)){
		// info = 3;
		// }
		//
		// if( bracketed && stepMax-stepMin <= X_TOL*stepMax){
		// info = 2;
		// }
		//
		// if( (newPt[f] <= fTest) && (Math.abs(newPt[g]) <= - gtol*g0) ){
		// info = 1;
		// }
		//
		// if(info != 0){
		// return newPt[f];
		// }
		//
		// if(stage1 && newPt[f]< fTest && newPt[g] >= ftol*g0){
		// stage1 = false;
		// }
		//
		//
		// if( stage1 && f<= bestPt[f] && f > fTest){
		//
		// double[] newPtMod = new double[3];
		// double[] bestPtMod = new double[3];
		// double[] endPtMod = new double[3];
		//
		// newPtMod[f] = newPt[f] - newPt[a]*ftol*g0;
		// newPtMod[g] = newPt[g] - ftol*g0;
		// bestPtMod[f] = bestPt[f] - bestPt[a]*ftol*g0;
		// bestPtMod[g] = bestPt[g] - ftol*g0;
		// endPtMod[f] = endPt[f] - endPt[a]*ftol*g0;
		// endPtMod[g] = endPt[g] - ftol*g0;
		//
		// //this.cstep(newPtMod, bestPtMod, endPtMod, bracketed);
		//
		// bestPt[f] = bestPtMod[f] + bestPt[a]*ftol*g0;
		// bestPt[g] = bestPtMod[g] + ftol*g0;
		// endPt[f] = endPtMod[f] + endPt[a]*ftol*g0;
		// endPt[g] = endPtMod[g] + ftol*g0;
		//
		// }else{
		// //this.cstep(newPt, bestPt, endPt, bracketed);
		// }
		//
		// double p66 = 0.66;
		// double p5 = 0.5;
		//
		// if(bracketed){
		// if ( Math.abs(endPt[a] - bestPt[a]) >= p66*width1){
		// newPt[a] = bestPt[a] + p5*(endPt[a]-bestPt[a]);
		// }
		// width1 = width;
		// width = Math.abs(endPt[a]-bestPt[a]);
		// }
		//
		//
		//
		// }while(true);
		//
		// }
		//
		// private double cstepBackup( double[] newPt, double[] bestPt, double[]
		// endPt, boolean bracketed ){
		//
		// double p66 = 0.66;
		// int info = 0;
		// double stpf;
		// double theta,gamma,s,p,q,r,stpc,stpq;
		// boolean bound = false;
		//
		// double signG = newPt[g]*bestPt[g]/Math.abs(bestPt[g]);
		//
		//
		// //Our new point has a higher function value
		// if( newPt[f] > bestPt[f]){
		// info = 1;
		// bound = true;
		// theta = 3*(bestPt[f] - newPt[f])/(newPt[a] - bestPt[a]) + bestPt[g] +
		// newPt[g];
		// s = Math.max(Math.max(theta,newPt[g]), bestPt[g]);
		// gamma = s*Math.sqrt( (theta/s)*(theta/s) - (bestPt[g]/s)*(newPt[g]/s) );
		// if (newPt[a] < bestPt[a]){
		// gamma = -gamma;
		// }
		// p = (gamma - bestPt[g]) + theta;
		// q = ((gamma-bestPt[g]) + gamma) + newPt[g];
		// r = p/q;
		// stpc = bestPt[a] + r*(newPt[a] - bestPt[a]);
		// stpq = bestPt[a] +
		// ((bestPt[g]/((bestPt[f]-newPt[f])/(newPt[a]-bestPt[a])+bestPt[g]))/2)*(newPt[a]
		// - bestPt[a]);
		//
		// if ( Math.abs(stpc-bestPt[a]) < Math.abs(stpq - bestPt[a] )){
		// stpf = stpc;
		// } else{
		// stpf = stpc + (stpq - stpc)/2;
		// }
		// bracketed = true;
		//
		// } else if (signG < 0.0){
		//
		// info = 2;
		// bound = false;
		// theta = 3*(bestPt[f] - newPt[f])/(newPt[a] - bestPt[a]) + bestPt[g] +
		// newPt[g];
		// s = Math.max(Math.max(theta,bestPt[g]),newPt[g]);
		// gamma = s*Math.sqrt((theta/s)*(theta/s) - (bestPt[g]/s)*(newPt[g]/s));
		// if (newPt[a] > bestPt[a]) {
		// gamma = -gamma;
		// }
		// p = (gamma - newPt[g]) + theta;
		// q = ((gamma - newPt[g]) + gamma) + bestPt[g];
		// r = p/q;
		// stpc = newPt[a] + r*(bestPt[a] - newPt[a]);
		// stpq = newPt[a] + (newPt[g]/(newPt[g]-bestPt[g]))*(bestPt[a] - newPt[a]);
		// if (Math.abs(stpc-newPt[a]) > Math.abs(stpq-newPt[a])){
		// stpf = stpc;
		// } else {
		// stpf = stpq;
		// }
		// bracketed = true;
		// } else if ( Math.abs(newPt[g]) < Math.abs(bestPt[g])){
		// info = 3;
		// bound = true;
		// theta = 3*(bestPt[f] - newPt[f])/(newPt[a] - bestPt[a]) + bestPt[g] +
		// newPt[g];
		// s = Math.max(Math.max(theta,bestPt[g]),newPt[g]);
		// gamma = s*Math.sqrt(Math.max(0.0,(theta/s)*(theta/s) -
		// (bestPt[g]/s)*(newPt[g]/s)));
		// if (newPt[a] < bestPt[a]){
		// gamma = -gamma;
		// }
		// p = (gamma - bestPt[g]) + theta;
		// q = ((gamma-bestPt[g]) + gamma) + newPt[g];
		// r = p/q;
		// if (r < 0.0 && gamma != 0.0){
		// stpc = newPt[a] + r*(bestPt[a] - newPt[a]);
		// } else if (newPt[a] > bestPt[a]){
		// stpc = aMax;
		// } else{
		// stpc = aMin;
		// }
		// stpq = newPt[a] + (newPt[g]/(newPt[g]-bestPt[g]))*(bestPt[a] - newPt[a]);
		// if (bracketed){
		// if (Math.abs(newPt[a]-stpc) < Math.abs(newPt[a]-stpq)){
		// stpf = stpc;
		// } else {
		// stpf = stpq;
		// }
		// } else {
		// if (Math.abs(newPt[a]-stpc) > Math.abs(newPt[a]-stpq)){
		// log.info("modified to take only quad");
		// stpf = stpq;
		// }else{
		// stpf = stpq;
		// }
		// }
		//
		//
		// }else{
		// info = 4;
		// bound = false;
		//
		// if(bracketed){
		// theta = 3*(bestPt[f] - newPt[f])/(newPt[a] - bestPt[a]) + bestPt[g] +
		// newPt[g];
		// s = Math.max(Math.max(theta,bestPt[g]),newPt[g]);
		// gamma = s*Math.sqrt((theta/s)*(theta/s) - (bestPt[g]/s)*(newPt[g]/s));
		// if (newPt[a] > bestPt[a]) {
		// gamma = -gamma;
		// }
		// p = (gamma - newPt[g]) + theta;
		// q = ((gamma - newPt[g]) + gamma) + bestPt[g];
		// r = p/q;
		// stpc = newPt[a] + r*(bestPt[a] - newPt[a]);
		// stpf = stpc;
		// }else if (newPt[a] > bestPt[a]){
		// stpf = aMax;
		// }else{
		// stpf = aMin;
		// }
		//
		// }
		//
		//
		// if (newPt[f] > bestPt[f]) {
		// copy(newPt,endPt);
		// }else{
		// if (signG < 0.0){
		// copy(bestPt,endPt);
		// }
		// copy(newPt,bestPt);
		// }
		//
		// stpf = Math.min(aMax,stpf);
		// stpf = Math.max(aMin,stpf);
		// newPt[a] = stpf;
		// if (bracketed & bound){
		// if (endPt[a] > bestPt[a]){
		// newPt[a] = Math.min(bestPt[a]+p66*(endPt[a]-bestPt[a]),newPt[a]);
		// }else{
		// newPt[a] = Math.max(bestPt[a]+p66*(endPt[a]-bestPt[a]),newPt[a]);
		// }
		// }
		//
		// //newPt[f] =
		// log.info("cstep " + nf.format(newPt[a]) + " info " + info);
		// return newPt[a];
		//
		// }
	}
}
