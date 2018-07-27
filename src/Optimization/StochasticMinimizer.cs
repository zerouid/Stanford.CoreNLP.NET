using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>Stochastic Gradient Descent Minimizer.</summary>
	/// <remarks>
	/// Stochastic Gradient Descent Minimizer.
	/// Note: If you want a fast SGD minimizer, then you probably want to use
	/// StochasticInPlaceMinimizer, not this class!
	/// The basic way to use the minimizer is with a null constructor, then
	/// the simple minimize method:
	/// <p/>
	/// <p><code>Minimizer smd = new SGDMinimizer();</code>
	/// <br /><code>DiffFunction df = new SomeDiffFunction(); //Note that it must be a incidence of AbstractStochasticCachingDiffFunction</code>
	/// <br /><code>double tol = 1e-4;</code>
	/// <br /><code>double[] initial = getInitialGuess();</code>
	/// <br /><code>int maxIterations = someSafeNumber;</code>
	/// <br /><code>double[] minimum = qnm.minimize(df,tol,initial,maxIterations);</code>
	/// <p/>
	/// Constructing with a null constructor will use the default values of
	/// <p>
	/// <br /><code>batchSize = 15;</code>
	/// <br /><code>initialGain = 0.1;</code>
	/// <p/>
	/// </remarks>
	/// <author><a href="mailto:akleeman@stanford.edu">Alex Kleeman</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public abstract class StochasticMinimizer<T> : IMinimizer<T>, IHasEvaluators
		where T : IFunction
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(StochasticMinimizer));

		public bool outputIterationsToFile = false;

		public int outputFrequency = 1000;

		public double gain = 0.1;

		protected internal double[] x;

		protected internal double[] newX;

		protected internal double[] grad;

		protected internal double[] newGrad;

		protected internal double[] v;

		protected internal int numBatches;

		protected internal int k;

		protected internal int bSize = 15;

		protected internal bool quiet = false;

		protected internal IList<double[]> gradList = null;

		protected internal int memory = 10;

		protected internal int numPasses = -1;

		protected internal Random gen = new Random(1);

		protected internal PrintWriter file = null;

		protected internal PrintWriter infoFile = null;

		protected internal long maxTime = long.MaxValue;

		private int evaluateIters = 0;

		private IEvaluator[] evaluators;

		// Evaluate every x iterations (0 = no evaluation)
		// separate set of evaluators to check how optimization is going
		public virtual void ShutUp()
		{
			this.quiet = true;
		}

		protected internal static readonly NumberFormat nf = new DecimalFormat("0.000E0");

		protected internal abstract string GetName();

		protected internal abstract void TakeStep(AbstractStochasticCachingDiffFunction dfunction);

		public virtual void SetEvaluators(int iters, IEvaluator[] evaluators)
		{
			this.evaluateIters = iters;
			this.evaluators = evaluators;
		}

		/*
		This is the scaling factor for the gains to ensure convergence
		*/
		protected internal static double GainSchedule(int it, double tau)
		{
			return (tau / (tau + it));
		}

		/*
		* This is used to smooth the gradients, providing a more robust calculation which
		* generally leads to a better routine.
		*/
		protected internal static double[] Smooth(IList<double[]> toSmooth)
		{
			double[] smoothed = new double[toSmooth[0].Length];
			foreach (double[] thisArray in toSmooth)
			{
				ArrayMath.PairwiseAddInPlace(smoothed, thisArray);
			}
			ArrayMath.MultiplyInPlace(smoothed, 1 / ((double)toSmooth.Count));
			return smoothed;
		}

		private void InitFiles()
		{
			if (outputIterationsToFile)
			{
				string fileName = GetName() + ".output";
				string infoName = GetName() + ".info";
				try
				{
					file = new PrintWriter(new FileOutputStream(fileName), true);
					infoFile = new PrintWriter(new FileOutputStream(infoName), true);
				}
				catch (IOException e)
				{
					log.Info("Caught IOException outputting data to file: " + e.Message);
					System.Environment.Exit(1);
				}
			}
		}

		public abstract Pair<int, double> Tune(IFunction function, double[] initial, long msPerTest);

		public virtual double TuneDouble(IFunction function, double[] initial, long msPerTest, StochasticMinimizer.IPropertySetter<double> ps, double lower, double upper)
		{
			return this.TuneDouble(function, initial, msPerTest, ps, lower, upper, 1e-3 * System.Math.Abs(upper - lower));
		}

		public virtual double TuneDouble(IFunction function, double[] initial, long msPerTest, StochasticMinimizer.IPropertySetter<double> ps, double lower, double upper, double Tol)
		{
			double[] xtest = new double[initial.Length];
			this.maxTime = msPerTest;
			// check for stochastic derivatives
			if (!(function is AbstractStochasticCachingDiffFunction))
			{
				throw new NotSupportedException();
			}
			AbstractStochasticCachingDiffFunction dfunction = (AbstractStochasticCachingDiffFunction)function;
			IList<Pair<double, double>> res = new List<Pair<double, double>>();
			Pair<double, double> best = new Pair<double, double>(lower, double.PositiveInfinity);
			//this is set to lower because the first it will always use the lower first, so it has to be best
			Pair<double, double> low = new Pair<double, double>(lower, double.PositiveInfinity);
			Pair<double, double> high = new Pair<double, double>(upper, double.PositiveInfinity);
			Pair<double, double> cur = new Pair<double, double>();
			Pair<double, double> tmp = new Pair<double, double>();
			IList<double> queue = new List<double>();
			queue.Add(lower);
			queue.Add(upper);
			//queue.add(0.5* (lower + upper));
			bool toContinue = true;
			this.numPasses = 10000;
			do
			{
				System.Array.Copy(initial, 0, xtest, 0, initial.Length);
				if (queue.Count != 0)
				{
					cur.first = queue.Remove(0);
				}
				else
				{
					cur.first = 0.5 * (low.First() + high.First());
				}
				ps.Set(cur.First());
				log.Info(string.Empty);
				log.Info("About to test with batch size:  " + bSize + "  gain: " + gain + " and  " + ps.ToString() + " set to  " + cur.First());
				xtest = this.Minimize(function, 1e-100, xtest);
				if (double.IsNaN(xtest[0]))
				{
					cur.second = double.PositiveInfinity;
				}
				else
				{
					cur.second = dfunction.ValueAt(xtest);
				}
				if (cur.Second() < best.Second())
				{
					CopyPair(best, tmp);
					CopyPair(cur, best);
					if (tmp.First() > best.First())
					{
						CopyPair(tmp, high);
					}
					else
					{
						// The old best is now the upper bound
						CopyPair(tmp, low);
					}
					// The old best is now the lower bound
					queue.Add(0.5 * (cur.First() + high.First()));
				}
				else
				{
					// check in the right interval next
					if (cur.First() < best.First())
					{
						CopyPair(cur, low);
					}
					else
					{
						if (cur.First() > best.First())
						{
							CopyPair(cur, high);
						}
					}
				}
				if (System.Math.Abs(low.First() - high.First()) < Tol)
				{
					toContinue = false;
				}
				res.Add(new Pair<double, double>(cur.First(), cur.Second()));
				log.Info(string.Empty);
				log.Info("Final value is: " + nf.Format(cur.Second()));
				log.Info("Optimal so far using " + ps.ToString() + " is: " + best.First());
			}
			while (toContinue);
			//output the results to screen.
			log.Info("-------------");
			log.Info(" RESULTS          ");
			log.Info(ps.GetType().ToString());
			log.Info("-------------");
			log.Info("  val    ,    function after " + msPerTest + " ms");
			foreach (Pair<double, double> re in res)
			{
				log.Info(re.First() + "    ,    " + re.Second());
			}
			log.Info(string.Empty);
			log.Info(string.Empty);
			return best.First();
		}

		private static void CopyPair(Pair<double, double> from, Pair<double, double> to)
		{
			to.first = from.First();
			to.second = from.Second();
		}

		private class SetGain : StochasticMinimizer.IPropertySetter<double>
		{
			internal StochasticMinimizer<T> parent = null;

			public SetGain(StochasticMinimizer<T> _enclosing, StochasticMinimizer<T> min)
			{
				this._enclosing = _enclosing;
				this.parent = min;
			}

			public virtual void Set(double @in)
			{
				this._enclosing.gain = @in;
			}

			private readonly StochasticMinimizer<T> _enclosing;
		}

		public virtual double TuneGain(IFunction function, double[] initial, long msPerTest, double lower, double upper)
		{
			return TuneDouble(function, initial, msPerTest, new StochasticMinimizer.SetGain(this, this), lower, upper);
		}

		// [cdm 2012: The version that used to be here was clearly buggy;
		// I changed it a little, but didn't test it. It's now more correct, but
		// I think it is still conceptually faulty, since it will keep growing the
		// batch size so long as any minute improvement in the function value is
		// obtained, whereas the whole point of using a small batch is to get speed
		// at the cost of small losses.]
		public virtual int TuneBatch(IFunction function, double[] initial, long msPerTest, int bStart)
		{
			double[] xTest = new double[initial.Length];
			int bOpt = 0;
			double min = double.PositiveInfinity;
			this.maxTime = msPerTest;
			double prev = double.PositiveInfinity;
			// check for stochastic derivatives
			if (!(function is AbstractStochasticCachingDiffFunction))
			{
				throw new NotSupportedException();
			}
			AbstractStochasticCachingDiffFunction dFunction = (AbstractStochasticCachingDiffFunction)function;
			int b = bStart;
			bool toContinue = true;
			do
			{
				System.Array.Copy(initial, 0, xTest, 0, initial.Length);
				log.Info(string.Empty);
				log.Info("Testing with batch size:  " + b);
				bSize = b;
				ShutUp();
				this.Minimize(function, 1e-5, xTest);
				double result = dFunction.ValueAt(xTest);
				if (result < min)
				{
					min = result;
					bOpt = bSize;
					b *= 2;
					prev = result;
				}
				else
				{
					if (result < prev)
					{
						b *= 2;
						prev = result;
					}
					else
					{
						if (result > prev)
						{
							toContinue = false;
						}
					}
				}
				log.Info(string.Empty);
				log.Info("Final value is: " + nf.Format(result));
				log.Info("Optimal so far is:  batch size: " + bOpt);
			}
			while (toContinue);
			return bOpt;
		}

		public virtual Pair<int, double> Tune(IFunction function, double[] initial, long msPerTest, IList<int> batchSizes, IList<double> gains)
		{
			double[] xtest = new double[initial.Length];
			int bOpt = 0;
			double gOpt = 0.0;
			double min = double.PositiveInfinity;
			double[][] results = new double[][] {  };
			this.maxTime = msPerTest;
			for (int b = 0; b < batchSizes.Count; b++)
			{
				for (int g = 0; g < gains.Count; g++)
				{
					System.Array.Copy(initial, 0, xtest, 0, initial.Length);
					bSize = batchSizes[b];
					gain = gains[g];
					log.Info(string.Empty);
					log.Info("Testing with batch size: " + bSize + "    gain:  " + nf.Format(gain));
					this.quiet = true;
					this.Minimize(function, 1e-100, xtest);
					results[b][g] = function.ValueAt(xtest);
					if (results[b][g] < min)
					{
						min = results[b][g];
						bOpt = bSize;
						gOpt = gain;
					}
					log.Info(string.Empty);
					log.Info("Final value is: " + nf.Format(results[b][g]));
					log.Info("Optimal so far is:  batch size: " + bOpt + "   gain:  " + nf.Format(gOpt));
				}
			}
			return new Pair<int, double>(bOpt, gOpt);
		}

		//This can be filled if an extending class needs to initialize things.
		protected internal virtual void Init(AbstractStochasticCachingDiffFunction func)
		{
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

		public virtual double[] Minimize(IFunction function, double functionTolerance, double[] initial)
		{
			return Minimize(function, functionTolerance, initial, -1);
		}

		public virtual double[] Minimize(IFunction function, double functionTolerance, double[] initial, int maxIterations)
		{
			// check for stochastic derivatives
			if (!(function is AbstractStochasticCachingDiffFunction))
			{
				throw new NotSupportedException();
			}
			AbstractStochasticCachingDiffFunction dfunction = (AbstractStochasticCachingDiffFunction)function;
			dfunction.method = StochasticCalculateMethods.GradientOnly;
			/* ---
			StochasticDiffFunctionTester sdft = new StochasticDiffFunctionTester(dfunction);
			ArrayMath.add(initial, gen.nextDouble() ); // to make sure that priors are working.
			sdft.testSumOfBatches(initial, 1e-4);
			System.exit(1);
			--- */
			x = initial;
			grad = new double[x.Length];
			newX = new double[x.Length];
			gradList = new List<double[]>();
			numBatches = dfunction.DataDimension() / bSize;
			outputFrequency = (int)System.Math.Ceil(((double)numBatches) / ((double)outputFrequency));
			Init(dfunction);
			InitFiles();
			bool have_max = (maxIterations > 0 || numPasses > 0);
			if (!have_max)
			{
				throw new NotSupportedException("No maximum number of iterations has been specified.");
			}
			else
			{
				maxIterations = System.Math.Max(maxIterations, numPasses) * numBatches;
			}
			Sayln("       Batchsize of: " + bSize);
			Sayln("       Data dimension of: " + dfunction.DataDimension());
			Sayln("       Batches per pass through data:  " + numBatches);
			Sayln("       Max iterations is = " + maxIterations);
			if (outputIterationsToFile)
			{
				infoFile.Println(function.DomainDimension() + "; DomainDimension ");
				infoFile.Println(bSize + "; batchSize ");
				infoFile.Println(maxIterations + "; maxIterations");
				infoFile.Println(numBatches + "; numBatches ");
				infoFile.Println(outputFrequency + "; outputFrequency");
			}
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			//            Loop
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			Timing total = new Timing();
			Timing current = new Timing();
			total.Start();
			current.Start();
			for (k = 0; k < maxIterations; k++)
			{
				try
				{
					bool doEval = (k > 0 && evaluateIters > 0 && k % evaluateIters == 0);
					if (doEval)
					{
						DoEvaluation(x);
					}
					int pass = k / numBatches;
					int batch = k % numBatches;
					Say("Iter: " + k + " pass " + pass + " batch " + batch);
					// restrict number of saved gradients
					//  (recycle memory of first gradient in list for new gradient)
					if (k > 0 && gradList.Count >= memory)
					{
						newGrad = gradList.Remove(0);
					}
					else
					{
						newGrad = new double[grad.Length];
					}
					dfunction.hasNewVals = true;
					System.Array.Copy(dfunction.DerivativeAt(x, v, bSize), 0, newGrad, 0, newGrad.Length);
					ArrayMath.AssertFinite(newGrad, "newGrad");
					gradList.Add(newGrad);
					grad = Smooth(gradList);
					//Get the next X
					TakeStep(dfunction);
					ArrayMath.AssertFinite(newX, "newX");
					//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
					// THIS IS FOR DEBUG ONLY
					//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
					if (outputIterationsToFile && (k % outputFrequency == 0) && k != 0)
					{
						double curVal = dfunction.ValueAt(x);
						Say(" TrueValue{ " + curVal + " } ");
						file.Println(k + " , " + curVal + " , " + total.Report());
					}
					//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
					// END OF DEBUG STUFF
					//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
					if (k >= maxIterations)
					{
						Sayln("Stochastic Optimization complete.  Stopped after max iterations");
						x = newX;
						break;
					}
					if (total.Report() >= maxTime)
					{
						Sayln("Stochastic Optimization complete.  Stopped after max time");
						x = newX;
						break;
					}
					System.Array.Copy(newX, 0, x, 0, x.Length);
					Say("[" + (total.Report()) / 1000.0 + " s ");
					Say("{" + (current.Restart() / 1000.0) + " s}] ");
					Say(" " + dfunction.LastValue());
					if (quiet)
					{
						log.Info(".");
					}
					else
					{
						Sayln(string.Empty);
					}
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
			}
			if (evaluateIters > 0)
			{
				// do final evaluation
				DoEvaluation(x);
			}
			if (outputIterationsToFile)
			{
				infoFile.Println(k + "; Iterations");
				infoFile.Println((total.Report()) / 1000.0 + "; Completion Time");
				infoFile.Println(dfunction.ValueAt(x) + "; Finalvalue");
				infoFile.Close();
				file.Close();
				log.Info("Output Files Closed");
			}
			//System.exit(1);
			Say("Completed in: " + (total.Report()) / 1000.0 + " s");
			return x;
		}

		public interface IPropertySetter<T1>
		{
			void Set(T1 @in);
		}

		protected internal virtual void Sayln(string s)
		{
			if (!quiet)
			{
				log.Info(s);
			}
		}

		protected internal virtual void Say(string s)
		{
			if (!quiet)
			{
				log.Info(s);
			}
		}
	}
}
