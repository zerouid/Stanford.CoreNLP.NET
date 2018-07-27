using System;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>Stochastic Gradient Descent Minimizer.</summary>
	/// <remarks>
	/// Stochastic Gradient Descent Minimizer.
	/// <p>
	/// The basic way to use the minimizer is with a null constructor, then
	/// the simple minimize method:
	/// <p>
	/// <p>
	/// <c>Minimizer smd = new InefficientSGDMinimizer();</c>
	/// <br />
	/// <c>DiffFunction df = new SomeDiffFunction(); //Note that it must be a incidence of AbstractStochasticCachingDiffFunction</c>
	/// <br />
	/// <c>double tol = 1e-4;</c>
	/// <br />
	/// <c>double[] initial = getInitialGuess();</c>
	/// <br />
	/// <c>int maxIterations = someSafeNumber;</c>
	/// <br />
	/// <c>double[] minimum = qnm.minimize(df,tol,initial,maxIterations);</c>
	/// <p>
	/// Constructing with a null constructor will use the default values of
	/// <p>
	/// <br />
	/// <c>batchSize = 15;</c>
	/// <br />
	/// <c>initialGain = 0.1;</c>
	/// <p>
	/// <br /> NOTE: This class was previously called SGDMinimizer. SGDMinimizer is now what was StochasticInPlaceMinimizer.
	/// New projects should use that class, since it uses the ideas of Bottou's work to provide efficient SGD.
	/// </remarks>
	/// <author><a href="mailto:akleeman@stanford.edu">Alex Kleeman</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public class InefficientSGDMinimizer<T> : StochasticMinimizer<T>
		where T : IFunction
	{
		public override void ShutUp()
		{
			this.quiet = true;
		}

		public virtual void SetBatchSize(int batchSize)
		{
			bSize = batchSize;
		}

		public InefficientSGDMinimizer()
		{
		}

		public InefficientSGDMinimizer(double SGDGain, int batchSize)
			: this(SGDGain, batchSize, 50)
		{
		}

		public InefficientSGDMinimizer(double SGDGain, int batchSize, int passes)
			: this(SGDGain, batchSize, passes, long.MaxValue, false)
		{
		}

		public InefficientSGDMinimizer(double SGDGain, int batchSize, int passes, bool outputToFile)
			: this(SGDGain, batchSize, passes, long.MaxValue, outputToFile)
		{
		}

		public InefficientSGDMinimizer(double SGDGain, int batchSize, int passes, long maxTime)
			: this(SGDGain, batchSize, passes, maxTime, false)
		{
		}

		public InefficientSGDMinimizer(double SGDGain, int batchSize, int passes, long maxTime, bool outputToFile)
		{
			bSize = batchSize;
			gain = SGDGain;
			this.numPasses = passes;
			this.outputIterationsToFile = outputToFile;
			this.maxTime = maxTime;
		}

		protected internal override string GetName()
		{
			int g = (int)gain * 1000;
			return "SGD" + bSize + "_g" + g;
		}

		public virtual Pair<int, double> Tune(IFunction function, double[] initial, long msPerTest, double gainLow, double gainHigh)
		{
			this.quiet = true;
			gain = TuneGain(function, initial, msPerTest, gainLow, gainHigh);
			bSize = TuneBatch(function, initial, msPerTest, 1);
			return new Pair<int, double>(bSize, gain);
		}

		public override Pair<int, double> Tune(IFunction function, double[] initial, long msPerTest)
		{
			return this.Tune(function, initial, msPerTest, 1e-7, 1.0);
		}

		protected internal override void TakeStep(AbstractStochasticCachingDiffFunction dfunction)
		{
			for (int i = 0; i < x.Length; i++)
			{
				newX[i] = x[i] - gain * GainSchedule(k, 5 * numBatches) * grad[i];
			}
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
			double[] grads = new double[dim];
			IDiffFunction f = new _IDiffFunction_120(dim, grads, var);
			Edu.Stanford.Nlp.Optimization.InefficientSGDMinimizer<IDiffFunction> min = new Edu.Stanford.Nlp.Optimization.InefficientSGDMinimizer<IDiffFunction>();
			min.Minimize(f, 1.0E-4, init);
		}

		private sealed class _IDiffFunction_120 : IDiffFunction
		{
			public _IDiffFunction_120(int dim, double[] grads, double[] var)
			{
				this.dim = dim;
				this.grads = grads;
				this.var = var;
			}

			public double[] DerivativeAt(double[] x)
			{
				double val = Math.Pi * this.ValuePow(x, Math.Pi - 1);
				for (int i = 0; i < dim; i++)
				{
					grads[i] = x[i] * var[i] * val;
				}
				return grads;
			}

			public double ValueAt(double[] x)
			{
				return 1.0 + this.ValuePow(x, Math.Pi);
			}

			private double ValuePow(double[] x, double pow)
			{
				double val = 0.0;
				for (int i = 0; i < dim; i++)
				{
					val += x[i] * x[i] * var[i];
				}
				return Math.Pow(val * 0.5, pow);
			}

			public int DomainDimension()
			{
				return dim;
			}

			private readonly int dim;

			private readonly double[] grads;

			private readonly double[] var;
		}
	}
}
