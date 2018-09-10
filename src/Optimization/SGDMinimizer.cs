using System;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>In place Stochastic Gradient Descent Minimizer.</summary>
	/// <remarks>
	/// In place Stochastic Gradient Descent Minimizer.
	/// <ul>
	/// <li> Follows weight decay and tuning of learning parameter of crfsgd of
	/// Leon Bottou: http://leon.bottou.org/projects/sgd
	/// <li> Only supports L2 regularization (QUADRATIC)
	/// <li> Requires objective function to be an AbstractStochasticCachingDiffUpdateFunction.
	/// </ul>
	/// NOTE: unlike other minimizers, regularization is done in the minimizer, not the objective function.
	/// This class was previously called StochasticInPlaceMinimizer. This is now SGDMinimizer, and the old SGDMinimizer is now InefficientSGDMinimizer.
	/// </remarks>
	/// <author>Angel Chang</author>
	public class SGDMinimizer<T> : IMinimizer<T>, IHasEvaluators
		where T : Func
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.SGDMinimizer));

		protected internal double xscale;

		protected internal double xnorm;

		protected internal double[] x;

		protected internal int t0;

		protected internal readonly double sigma;

		protected internal double lambda;

		protected internal bool quiet = false;

		private const int DefaultNumPasses = 50;

		protected internal readonly int numPasses;

		protected internal int bSize = 1;

		private const int DefaultTuningSamples = 1000;

		protected internal readonly int tuningSamples;

		protected internal Random gen = new Random(1);

		protected internal long maxTime = long.MaxValue;

		private int evaluateIters = 0;

		private IEvaluator[] evaluators;

		public SGDMinimizer(double sigma, int numPasses)
			: this(sigma, numPasses, -1, 1)
		{
		}

		public SGDMinimizer(double sigma, int numPasses, int tuningSamples)
			: this(sigma, numPasses, tuningSamples, 1)
		{
		}

		public SGDMinimizer(double sigma, int numPasses, int tuningSamples, int batchSize)
		{
			// Initial stochastic iteration count
			//-1;
			// NOTE: If bSize does not divide evenly into total number of samples,
			// some samples may get accounted for twice in one pass
			// Evaluate every x iterations (0 = no evaluation)
			// separate set of evaluators to check how optimization is going
			this.bSize = batchSize;
			this.sigma = sigma;
			if (numPasses >= 0)
			{
				this.numPasses = numPasses;
			}
			else
			{
				this.numPasses = DefaultNumPasses;
				Sayln("  SGDMinimizer: numPasses=" + numPasses + ", defaulting to " + this.numPasses);
			}
			if (tuningSamples > 0)
			{
				this.tuningSamples = tuningSamples;
			}
			else
			{
				this.tuningSamples = DefaultTuningSamples;
				Sayln("  SGDMinimizer: tuneSampleSize=" + tuningSamples + ", defaulting to " + this.tuningSamples);
			}
		}

		public SGDMinimizer(LogPrior prior, int numPasses, int batchSize, int tuningSamples)
		{
			if (LogPrior.LogPriorType.Quadratic == prior.GetType())
			{
				sigma = prior.GetSigma();
			}
			else
			{
				throw new Exception("Unsupported prior type " + prior.GetType());
			}
			if (numPasses >= 0)
			{
				this.numPasses = numPasses;
			}
			else
			{
				this.numPasses = DefaultNumPasses;
				Sayln("  SGDMinimizer: numPasses=" + numPasses + ", defaulting to " + this.numPasses);
			}
			this.bSize = batchSize;
			if (tuningSamples > 0)
			{
				this.tuningSamples = tuningSamples;
			}
			else
			{
				this.tuningSamples = DefaultTuningSamples;
				Sayln("  SGDMinimizer: tuneSampleSize=" + tuningSamples + ", defaulting to " + this.tuningSamples);
			}
		}

		public virtual void ShutUp()
		{
			this.quiet = true;
		}

		private static readonly NumberFormat nf = new DecimalFormat("0.000E0");

		protected internal virtual string GetName()
		{
			return "SGD_InPlace_b" + bSize + "_lambda" + nf.Format(lambda);
		}

		public virtual void SetEvaluators(int iters, IEvaluator[] evaluators)
		{
			this.evaluateIters = iters;
			this.evaluators = evaluators;
		}

		//This can be filled if an extending class needs to initialize things.
		protected internal virtual void Init(AbstractStochasticCachingDiffUpdateFunction func)
		{
		}

		public virtual double GetObjective(AbstractStochasticCachingDiffUpdateFunction function, double[] w, double wscale, int[] sample)
		{
			double wnorm = GetNorm(w) * wscale * wscale;
			double obj = function.ValueAt(w, wscale, sample);
			// Calculate objective with L2 regularization
			return obj + 0.5 * sample.Length * lambda * wnorm;
		}

		public virtual double TryEta(AbstractStochasticCachingDiffUpdateFunction function, double[] initial, int[] sample, double eta)
		{
			int numBatches = sample.Length / bSize;
			double[] w = new double[initial.Length];
			double wscale = 1;
			System.Array.Copy(initial, 0, w, 0, w.Length);
			int[] sampleBatch = new int[bSize];
			int sampleIndex = 0;
			for (int batch = 0; batch < numBatches; batch++)
			{
				for (int i = 0; i < bSize; i++)
				{
					sampleBatch[i] = sample[(sampleIndex + i) % sample.Length];
				}
				sampleIndex += bSize;
				double gain = eta / wscale;
				function.CalculateStochasticUpdate(w, wscale, sampleBatch, gain);
				wscale *= (1 - eta * lambda * bSize);
			}
			double obj = GetObjective(function, w, wscale, sample);
			return obj;
		}

		/// <summary>Finds a good learning rate to start with.</summary>
		/// <remarks>
		/// Finds a good learning rate to start with.
		/// eta = 1/(lambda*(t0+t)) - we find good t0
		/// </remarks>
		/// <param name="function"/>
		/// <param name="initial"/>
		/// <param name="sampleSize"/>
		/// <param name="seta"/>
		public virtual double Tune(AbstractStochasticCachingDiffUpdateFunction function, double[] initial, int sampleSize, double seta)
		{
			Timing timer = new Timing();
			int[] sample = function.GetSample(sampleSize);
			double sobj = GetObjective(function, initial, 1, sample);
			double besteta = 1;
			double bestobj = sobj;
			double eta = seta;
			int totest = 10;
			double factor = 2;
			bool phase2 = false;
			while (totest > 0 || !phase2)
			{
				double obj = TryEta(function, initial, sample, eta);
				bool okay = (obj < sobj);
				Sayln("  Trying eta=" + eta + "  obj=" + obj + ((okay) ? "(possible)" : "(too large)"));
				if (okay)
				{
					totest -= 1;
					if (obj < bestobj)
					{
						bestobj = obj;
						besteta = eta;
					}
				}
				if (!phase2)
				{
					if (okay)
					{
						eta = eta * factor;
					}
					else
					{
						phase2 = true;
						eta = seta;
					}
				}
				if (phase2)
				{
					eta = eta / factor;
				}
			}
			// take it on the safe side (implicit regularization)
			besteta /= factor;
			// determine t
			t0 = (int)(1 / (besteta * lambda));
			Sayln("  Taking eta=" + besteta + " t0=" + t0);
			Sayln("  Tuning completed in: " + Timing.ToSecondsString(timer.Report()) + " s");
			return besteta;
		}

		// really this is the square of the L2 norm....
		private static double GetNorm(double[] w)
		{
			double norm = 0;
			foreach (double aW in w)
			{
				norm += aW * aW;
			}
			return norm;
		}

		private void Rescale()
		{
			if (xscale == 1)
			{
				return;
			}
			for (int i = 0; i < x.Length; i++)
			{
				x[i] *= xscale;
			}
			xscale = 1;
		}

		private void DoEvaluation(double[] x)
		{
			// Evaluate solution
			if (evaluators == null)
			{
				return;
			}
			foreach (IEvaluator eval in evaluators)
			{
				Sayln("  Evaluating: " + eval.ToString());
				eval.Evaluate(x);
			}
		}

		public virtual double[] Minimize(Func function, double functionTolerance, double[] initial)
		{
			return Minimize(function, functionTolerance, initial, -1);
		}

		public virtual double[] Minimize(Func f, double functionTolerance, double[] initial, int maxIterations)
		{
			if (!(f is AbstractStochasticCachingDiffUpdateFunction))
			{
				throw new NotSupportedException();
			}
			AbstractStochasticCachingDiffUpdateFunction function = (AbstractStochasticCachingDiffUpdateFunction)f;
			int totalSamples = function.DataDimension();
			int tuneSampleSize = Math.Min(totalSamples, tuningSamples);
			if (tuneSampleSize < tuningSamples)
			{
				log.Info("WARNING: Total number of samples=" + totalSamples + " is smaller than requested tuning sample size=" + tuningSamples + "!!!");
			}
			lambda = 1.0 / (sigma * totalSamples);
			Sayln("Using sigma=" + sigma + " lambda=" + lambda + " tuning sample size " + tuneSampleSize);
			// tune(function, initial, tuneSampleSize, 0.1);
			t0 = (int)(1 / (0.1 * lambda));
			x = new double[initial.Length];
			System.Array.Copy(initial, 0, x, 0, x.Length);
			xscale = 1;
			xnorm = GetNorm(x);
			int numBatches = totalSamples / bSize;
			Init(function);
			bool have_max = (maxIterations > 0 || numPasses > 0);
			if (!have_max)
			{
				throw new NotSupportedException("No maximum number of iterations has been specified.");
			}
			else
			{
				maxIterations = Math.Max(maxIterations, numPasses) * numBatches;
			}
			Sayln("       Batch size of: " + bSize);
			Sayln("       Data dimension of: " + totalSamples);
			Sayln("       Batches per pass through data:  " + numBatches);
			Sayln("       Number of passes is = " + numPasses);
			Sayln("       Max iterations is = " + maxIterations);
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			//            Loop
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			Timing total = new Timing();
			Timing current = new Timing();
			int t = t0;
			int iters = 0;
			for (int pass = 0; pass < numPasses; pass++)
			{
				bool doEval = (pass > 0 && evaluateIters > 0 && pass % evaluateIters == 0);
				if (doEval)
				{
					Rescale();
					DoEvaluation(x);
				}
				double totalValue = 0;
				double lastValue = 0;
				for (int batch = 0; batch < numBatches; batch++)
				{
					iters++;
					//Get the next X
					double eta = 1 / (lambda * t);
					double gain = eta / xscale;
					lastValue = function.CalculateStochasticUpdate(x, xscale, bSize, gain);
					totalValue += lastValue;
					// weight decay (for L2 regularization)
					xscale *= (1 - eta * lambda * bSize);
					t += bSize;
				}
				if (xscale < 1e-6)
				{
					Rescale();
				}
				try
				{
					ArrayMath.AssertFinite(x, "x");
				}
				catch (ArrayMath.InvalidElementException e)
				{
					log.Info(e.ToString());
					for (int i = 0; i < x.Length; i++)
					{
						x[i] = double.NaN;
					}
					break;
				}
				xnorm = GetNorm(x) * xscale * xscale;
				// Calculate loss based on L2 regularization
				double loss = totalValue + 0.5 * xnorm * lambda * totalSamples;
				Sayln("Iter: " + iters + " pass " + pass + " batch 1 ... " + numBatches.ToString() + " [" + (total.Report()) / 1000.0 + " s " + " {" + (current.Restart() / 1000.0) + " s}] " + lastValue + " " + totalValue + " " + loss);
				if (iters >= maxIterations)
				{
					Sayln("Stochastic Optimization complete.  Stopped after max iterations");
					break;
				}
				if (total.Report() >= maxTime)
				{
					Sayln("Stochastic Optimization complete.  Stopped after max time");
					break;
				}
			}
			Rescale();
			if (evaluateIters > 0)
			{
				// do final evaluation
				DoEvaluation(x);
			}
			Sayln("Completed in: " + Timing.ToSecondsString(total.Report()) + " s");
			return x;
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
