using System;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// AdaGrad optimizer that works online, and use sparse gradients, need a
	/// function that takes a Counter<K> as argument and returns a Counter<K> as
	/// gradient
	/// </summary>
	/// <author>Sida Wang</author>
	public class SparseAdaGradMinimizer<K, F> : ISparseMinimizer<K, F>
		where F : ISparseOnlineFunction<K>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.SparseAdaGradMinimizer));

		public bool quiet = false;

		protected internal int numPasses;

		protected internal int batchSize;

		protected internal double eta;

		protected internal double lambdaL1;

		protected internal double lambdaL2;

		protected internal ICounter<K> sumGradSquare;

		protected internal ICounter<K> x;

		protected internal Random randGenerator = new Random(1);

		public readonly double Eps = 1e-15;

		public readonly double soften = 1e-4;

		public SparseAdaGradMinimizer(int numPasses)
			: this(numPasses, 0.1)
		{
		}

		public SparseAdaGradMinimizer(int numPasses, double eta)
			: this(numPasses, eta, 1, 0, 0)
		{
		}

		public SparseAdaGradMinimizer(int numPasses, double eta, int batchSize, double lambdaL1, double lambdaL2)
		{
			// use FOBOS to handle L1 or L2. The alternative is just setting these to 0,
			// and take any penalty into account through the derivative
			this.numPasses = numPasses;
			this.eta = eta;
			this.batchSize = batchSize;
			this.lambdaL1 = lambdaL1;
			this.lambdaL2 = lambdaL2;
			// can use another counter to make this thread-safe
			this.sumGradSquare = new ClassicCounter<K>();
		}

		public virtual ICounter<K> Minimize(F function, ICounter<K> initial)
		{
			return Minimize(function, initial, -1);
		}

		// Does L1 or L2 using FOBOS and lazy update, so L1 should not be handled in the
		// objective
		// Alternatively, you can handle other regularization in the objective,
		// but then, if the derivative is not sparse, this routine would not be very
		// efficient. However, might still be okay for CRFs
		public virtual ICounter<K> Minimize(F function, ICounter<K> x, int maxIterations)
		{
			Sayln("       Batch size of: " + batchSize);
			Sayln("       Data dimension of: " + function.DataSize());
			int numBatches = (function.DataSize() - 1) / this.batchSize + 1;
			Sayln("       Batches per pass through data:  " + numBatches);
			Sayln("       Number of passes is = " + numPasses);
			Sayln("       Max iterations is = " + maxIterations);
			ICounter<K> lastUpdated = new ClassicCounter<K>();
			int timeStep = 0;
			Timing total = new Timing();
			total.Start();
			for (int iter = 0; iter < numPasses; iter++)
			{
				double totalObjValue = 0;
				for (int j = 0; j < numBatches; j++)
				{
					int[] selectedData = GetSample(function, this.batchSize);
					// the core adagrad
					ICounter<K> gradient = function.DerivativeAt(x, selectedData);
					totalObjValue = totalObjValue + function.ValueAt(x, selectedData);
					foreach (K feature in gradient.KeySet())
					{
						double gradf = gradient.GetCount(feature);
						double prevrate = eta / (Math.Sqrt(sumGradSquare.GetCount(feature)) + soften);
						double sgsValue = sumGradSquare.IncrementCount(feature, gradf * gradf);
						double currentrate = eta / (Math.Sqrt(sgsValue) + soften);
						double testupdate = x.GetCount(feature) - (currentrate * gradient.GetCount(feature));
						double lastUpdateTimeStep = lastUpdated.GetCount(feature);
						double idleinterval = timeStep - lastUpdateTimeStep - 1;
						lastUpdated.SetCount(feature, (double)timeStep);
						// does lazy update using idleinterval
						double trunc = Math.Max(0.0, (Math.Abs(testupdate) - (currentrate + prevrate * idleinterval) * this.lambdaL1));
						double trunc2 = trunc * Math.Pow(1 - this.lambdaL2, currentrate + prevrate * idleinterval);
						double realupdate = Math.Signum(testupdate) * trunc2;
						if (realupdate < Eps)
						{
							x.Remove(feature);
						}
						else
						{
							x.SetCount(feature, realupdate);
						}
						// reporting
						timeStep++;
						if (timeStep > maxIterations)
						{
							Sayln("Stochastic Optimization complete.  Stopped after max iterations");
							break;
						}
						Sayln(System.Console.Out.Format("Iter %d \t batch: %d \t time=%.2f \t obj=%.4f", iter, timeStep, total.Report() / 1000.0, totalObjValue).ToString());
					}
				}
			}
			return x;
		}

		// you do not have to use this, and can handle the data pipeline yourself.
		// See AbstractStochasticCachingDiffFunction for more minibatching schemes,
		// but it really
		// should not matter very much
		private int[] GetSample(F function, int sampleSize)
		{
			int[] sample = new int[sampleSize];
			for (int i = 0; i < sampleSize; i++)
			{
				sample[i] = randGenerator.NextInt(function.DataSize());
			}
			return sample;
		}

		private static readonly NumberFormat nf = new DecimalFormat("0.000E0");

		protected internal virtual string GetName()
		{
			return "SparseAdaGrad_batchsize" + batchSize + "_eta" + nf.Format(eta) + "_lambdaL1" + nf.Format(lambdaL1) + "_lambdaL2" + nf.Format(lambdaL2);
		}

		protected internal virtual void Sayln(string s)
		{
			if (!quiet)
			{
				log.Info(s);
			}
		}
	}
}
