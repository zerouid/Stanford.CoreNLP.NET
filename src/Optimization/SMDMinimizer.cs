using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// <p>
	/// Stochastic Meta Descent Minimizer based on
	/// <p>
	/// Accelerated training of conditional random fields with stochastic gradient methods
	/// S.
	/// </summary>
	/// <remarks>
	/// <p>
	/// Stochastic Meta Descent Minimizer based on
	/// <p>
	/// Accelerated training of conditional random fields with stochastic gradient methods
	/// S. V. N. Vishwanathan, Nicol N. Schraudolph, Mark W. Schmidt, Kevin P. Murphy
	/// June 2006 	 	Proceedings of the 23rd international conference on Machine learning ICML '06
	/// Publisher: ACM Press
	/// <p/>
	/// The basic way to use the minimizer is with a null constructor, then
	/// the simple minimize method:
	/// <p/>
	/// <p><code>Minimizer smd = new SMDMinimizer();</code>
	/// <br /><code>DiffFunction df = new SomeDiffFunction();</code>
	/// <br /><code>double tol = 1e-4;</code>
	/// <br /><code>double[] initial = getInitialGuess();</code>
	/// <br /><code>int maxIterations = someSafeNumber;
	/// <br /><code>double[] minimum = qnm.minimize(df,tol,initial,maxIterations);</code>
	/// <p/>
	/// Constructing with a null constructor will use the default values of
	/// <p>
	/// <br /><code>batchSize = 15;</code>
	/// <br /><code>initialGain = 0.1;</code>
	/// <br /><code>useAlgorithmicDifferentiation = true;</code>
	/// <p/>
	/// <p/>
	/// </remarks>
	/// <author><a href="mailto:akleeman@stanford.edu">Alex Kleeman</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public class SMDMinimizer<T> : StochasticMinimizer<T>
		where T : Func
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.SMDMinimizer));

		public double mu = 0.01;

		public double lam = 1.0;

		public double cPosDef = 0.00;

		public double meta;

		public bool printMinMax = false;

		private double[] Hv;

		private double[] gains;

		internal StochasticCalculateMethods method;

		//DEBUG ONLY
		// = null;
		public override void ShutUp()
		{
			this.quiet = true;
		}

		public virtual void SetBatchSize(int batchSize)
		{
			bSize = batchSize;
		}

		public SMDMinimizer()
		{
		}

		public SMDMinimizer(double initialSMDGain, int batchSize, StochasticCalculateMethods method, int passes)
			: this(initialSMDGain, batchSize, method, passes, false)
		{
		}

		public SMDMinimizer(double initGain, int batchSize, StochasticCalculateMethods method, int passes, bool outputToFile)
		{
			bSize = batchSize;
			gain = initGain;
			this.method = method;
			this.numPasses = passes;
			this.outputIterationsToFile = outputToFile;
		}

		public override double[] Minimize(Func function, double functionTolerance, double[] initial)
		{
			return Minimize(function, functionTolerance, initial, -1);
		}

		protected internal override void Init(AbstractStochasticCachingDiffFunction func)
		{
			func.method = this.method;
			gains = new double[x.Length];
			v = new double[x.Length];
			Hv = new double[x.Length];
			for (int i = 0; i < v.Length; i++)
			{
				gains[i] = gain;
			}
		}

		private class SetMu : StochasticMinimizer.IPropertySetter<double>
		{
			internal SMDMinimizer<T> parent;

			public SetMu(SMDMinimizer<T> _enclosing, SMDMinimizer<T> smd)
			{
				this._enclosing = _enclosing;
				// = null;
				this.parent = smd;
			}

			public virtual void Set(double @in)
			{
				this.parent.mu = @in;
			}

			private readonly SMDMinimizer<T> _enclosing;
		}

		private class SetLam : StochasticMinimizer.IPropertySetter<double>
		{
			internal SMDMinimizer<T> parent;

			public SetLam(SMDMinimizer<T> _enclosing, SMDMinimizer<T> smd)
			{
				this._enclosing = _enclosing;
				// = null;
				this.parent = smd;
			}

			public virtual void Set(double @in)
			{
				this.parent.lam = @in;
			}

			private readonly SMDMinimizer<T> _enclosing;
		}

		public override Pair<int, double> Tune(Func function, double[] initial, long msPerTest)
		{
			this.quiet = true;
			this.lam = 0.9;
			this.mu = TuneDouble(function, initial, msPerTest, new SMDMinimizer.SetMu(this, this), 1e-8, 1e-2);
			this.lam = TuneDouble(function, initial, msPerTest, new SMDMinimizer.SetLam(this, this), 0.1, 1.0);
			gain = TuneGain(function, initial, msPerTest, 1e-8, 1.0);
			bSize = TuneBatch(function, initial, msPerTest, 1);
			log.Info("Results:  gain: " + nf.Format(gain) + "  batch " + bSize + "   mu" + nf.Format(this.mu) + "  lam" + nf.Format(this.lam));
			return new Pair<int, double>(bSize, gain);
		}

		protected internal override void TakeStep(AbstractStochasticCachingDiffFunction dfunction)
		{
			dfunction.returnPreviousValues = true;
			System.Array.Copy(dfunction.HdotVAt(x, v, grad, bSize), 0, Hv, 0, Hv.Length);
			//Update the weights
			for (int i = 0; i < x.Length; i++)
			{
				meta = 1 - mu * grad[i] * v[i];
				if (0.5 > meta)
				{
					gains[i] = gains[i] * 0.5;
				}
				else
				{
					gains[i] = gains[i] * meta;
				}
				//Update gain history
				v[i] = lam * (1 + cPosDef * gains[i]) * v[i] - gains[i] * (grad[i] + lam * Hv[i]);
				//Get the next X
				newX[i] = x[i] - gains[i] * grad[i];
			}
			if (printMinMax)
			{
				Say("vMin = " + ArrayMath.Min(v) + "  ");
				Say("vMax = " + ArrayMath.Max(v) + "  ");
				Say("gainMin = " + ArrayMath.Min(gains) + "  ");
				Say("gainMax = " + ArrayMath.Max(gains) + "  ");
			}
		}

		protected internal override string GetName()
		{
			int m = (int)(mu * 1000);
			int l = (int)(lam * 1000);
			int g = (int)(gain * 10000);
			return "SMD" + bSize + "_mu" + m + "_lam" + l + "_g" + g;
		}

		public static void Main(string[] args)
		{
			// optimizes test function using doubles and floats
			// test function is (0.5 sum(x_i^2 * var_i)) ^ PI
			// where var is a vector of random nonnegative numbers
			// dimensionality is variable.
			int dim = 500000;
			double maxVar = 5;
			double[] var = new double[dim];
			double[] init = new double[dim];
			for (int i = 0; i < dim; i++)
			{
				init[i] = ((i + 1) / (double)dim - 0.5);
				//init[i] = (Math.random() - 0.5);
				var[i] = maxVar * (i + 1) / dim;
			}
			IDiffFunction f = new _IDiffFunction_193(dim, var);
			SMDMinimizer<IDiffFunction> min = new SMDMinimizer<IDiffFunction>();
			min.Minimize(f, 1.0E-4, init);
		}

		private sealed class _IDiffFunction_193 : IDiffFunction
		{
			public _IDiffFunction_193(int dim, double[] var)
			{
				this.dim = dim;
				this.var = var;
			}

			public double[] DerivativeAt(double[] x)
			{
				double val = System.Math.Pi * this.ValuePow(x, System.Math.Pi - 1);
				double[] grads = new double[dim];
				for (int i = 0; i < dim; i++)
				{
					grads[i] = x[i] * var[i] * val;
				}
				return grads;
			}

			public double ValueAt(double[] x)
			{
				return 1.0 + this.ValuePow(x, System.Math.Pi);
			}

			private double ValuePow(double[] x, double pow)
			{
				double val = 0.0;
				for (int i = 0; i < dim; i++)
				{
					val += x[i] * x[i] * var[i];
				}
				return System.Math.Pow(val * 0.5, pow);
			}

			public int DomainDimension()
			{
				return dim;
			}

			private readonly int dim;

			private readonly double[] var;
		}
	}
}
