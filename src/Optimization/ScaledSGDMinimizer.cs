using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>Stochastic Gradient Descent To Quasi Newton Minimizer.</summary>
	/// <remarks>
	/// Stochastic Gradient Descent To Quasi Newton Minimizer.
	/// An experimental minimizer which takes a stochastic function (one implementing AbstractStochasticCachingDiffFunction)
	/// and executes SGD for the first couple passes,  During the final iterations a series of approximate hessian vector
	/// products are built up...  These are then passed to the QNMinimizer so that it can start right up without the typical
	/// delay.
	/// </remarks>
	/// <author><a href="mailto:akleeman@stanford.edu">Alex Kleeman</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public class ScaledSGDMinimizer<Q> : StochasticMinimizer<Q>
		where Q : AbstractStochasticCachingDiffFunction
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.ScaledSGDMinimizer));

		private static int method = 1;

		public IList<double[]> yList = null;

		public IList<double[]> sList = null;

		public double[] diag;

		private double fixedGain = 0.99;

		private static int pairMem = 20;

		private double aMax = 1e6;

		// 0=MinErr  1=Bradley
		public virtual double TuneFixedGain(IFunction function, double[] initial, long msPerTest, double fixedStart)
		{
			double[] xtest = new double[initial.Length];
			double fOpt = 0.0;
			double factor = 1.2;
			double min = double.PositiveInfinity;
			this.maxTime = msPerTest;
			double prev = double.PositiveInfinity;
			// check for stochastic derivatives
			if (!(function is AbstractStochasticCachingDiffFunction))
			{
				throw new NotSupportedException();
			}
			AbstractStochasticCachingDiffFunction dfunction = (AbstractStochasticCachingDiffFunction)function;
			int it = 1;
			bool toContinue = true;
			double f = fixedStart;
			do
			{
				System.Array.Copy(initial, 0, xtest, 0, initial.Length);
				log.Info(string.Empty);
				this.fixedGain = f;
				log.Info("Testing with batchsize: " + bSize + "    gain:  " + gain + "  fixedGain:  " + nf.Format(fixedGain));
				this.numPasses = 10000;
				this.Minimize(function, 1e-100, xtest);
				double result = dfunction.ValueAt(xtest);
				if (it == 1)
				{
					f = f / factor;
				}
				if (result < min)
				{
					min = result;
					fOpt = this.fixedGain;
					f = f / factor;
					prev = result;
				}
				else
				{
					if (result < prev)
					{
						f = f / factor;
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
				it += 1;
				log.Info(string.Empty);
				log.Info("Final value is: " + nf.Format(result));
				log.Info("Optimal so far is:  fixedgain: " + fOpt);
			}
			while (toContinue);
			return fOpt;
		}

		private class SetFixedGain : StochasticMinimizer.IPropertySetter<double>
		{
			internal ScaledSGDMinimizer parent = null;

			public SetFixedGain(ScaledSGDMinimizer<Q> _enclosing, ScaledSGDMinimizer min)
			{
				this._enclosing = _enclosing;
				this.parent = min;
			}

			public virtual void Set(double @in)
			{
				this.parent.fixedGain = @in;
			}

			private readonly ScaledSGDMinimizer<Q> _enclosing;
		}

		public override Pair<int, double> Tune(IFunction function, double[] initial, long msPerTest)
		{
			this.quiet = true;
			for (int i = 0; i < 2; i++)
			{
				this.fixedGain = TuneDouble(function, initial, msPerTest, new ScaledSGDMinimizer.SetFixedGain(this, this), 0.1, 1.0);
				gain = TuneGain(function, initial, msPerTest, 1e-7, 1.0);
				bSize = TuneBatch(function, initial, msPerTest, 1);
				log.Info("Results:  fixedGain: " + nf.Format(this.fixedGain) + "  gain: " + nf.Format(gain) + "  batch " + bSize);
			}
			return new Pair<int, double>(bSize, gain);
		}

		public override void ShutUp()
		{
			this.quiet = true;
		}

		public virtual void SetBatchSize(int batchSize)
		{
			bSize = batchSize;
		}

		public ScaledSGDMinimizer(double SGDGain, int batchSize, int sgdPasses)
			: this(SGDGain, batchSize, sgdPasses, 1, false)
		{
		}

		public ScaledSGDMinimizer(double SGDGain, int batchSize, int sgdPasses, int method)
			: this(SGDGain, batchSize, sgdPasses, method, false)
		{
		}

		public ScaledSGDMinimizer(double SGDGain, int batchSize, int sgdPasses, int method, bool outputToFile)
		{
			bSize = batchSize;
			gain = SGDGain;
			this.numPasses = sgdPasses;
			ScaledSGDMinimizer.method = method;
			this.outputIterationsToFile = outputToFile;
		}

		public ScaledSGDMinimizer(double SGDGain, int batchSize)
			: this(SGDGain, batchSize, 50)
		{
		}

		public virtual void SetMaxTime(long max)
		{
			maxTime = max;
		}

		protected internal override string GetName()
		{
			int g = (int)(gain * 1000.0);
			int f = (int)(fixedGain * 1000.0);
			return "ScaledSGD" + bSize + "_g" + g + "_f" + f;
		}

		protected internal override void TakeStep(AbstractStochasticCachingDiffFunction dfunction)
		{
			for (int i = 0; i < x.Length; i++)
			{
				double thisGain = fixedGain * GainSchedule(k, 5 * numBatches) / (diag[i]);
				newX[i] = x[i] - thisGain * grad[i];
			}
			//Get a new pair...
			Say(" A ");
			double[] s;
			double[] y;
			if (pairMem > 0 && sList.Count == pairMem || sList.Count == pairMem)
			{
				s = sList.Remove(0);
				y = yList.Remove(0);
			}
			else
			{
				s = new double[x.Length];
				y = new double[x.Length];
			}
			s = ArrayMath.PairwiseSubtract(newX, x);
			dfunction.recalculatePrevBatch = true;
			System.Array.Copy(dfunction.DerivativeAt(newX, bSize), 0, y, 0, grad.Length);
			ArrayMath.PairwiseSubtractInPlace(y, newGrad);
			// newY = newY-newGrad
			double[] comp = new double[x.Length];
			sList.Add(s);
			yList.Add(y);
			UpdateDiag(diag, s, y);
		}

		protected internal override void Init(AbstractStochasticCachingDiffFunction func)
		{
			diag = new double[x.Length];
			memory = 1;
			for (int i = 0; i < x.Length; i++)
			{
				diag[i] = fixedGain / gain;
			}
			sList = new List<double[]>();
			yList = new List<double[]>();
		}

		private void UpdateDiag(double[] diag, double[] s, double[] y)
		{
			if (method == 0)
			{
				UpdateDiagMinErr(diag, s, y);
			}
			else
			{
				if (method == 1)
				{
					UpdateDiagBFGS(diag, s, y);
				}
			}
		}

		public virtual void UpdateDiagBFGS(double[] diag, double[] s, double[] y)
		{
			double sDs = 0.0;
			double sy = 0.0;
			for (int i = 0; i < s.Length; i++)
			{
				sDs += s[i] * diag[i] * s[i];
				sy += s[i] * y[i];
			}
			Say("B");
			double[] newDiag = new double[s.Length];
			bool updateDiag = true;
			for (int i_1 = 0; i_1 < s.Length; i_1++)
			{
				newDiag[i_1] = (1 - diag[i_1] * s[i_1] * s[i_1] / sDs) * diag[i_1] + y[i_1] * y[i_1] / sy;
				if (newDiag[i_1] < 0)
				{
					updateDiag = false;
					break;
				}
			}
			if (updateDiag)
			{
				System.Array.Copy(newDiag, 0, diag, 0, s.Length);
			}
			else
			{
				Say("!");
			}
		}

		private void UpdateDiagMinErr(double[] diag, double[] s, double[] y)
		{
			double low = 0.0;
			double high = 0.0;
			for (int i = 0; i < s.Length; i++)
			{
				double tmp = s[i] * (y[i] - diag[i]);
				high += tmp * tmp;
			}
			Say("M");
			double alpha = System.Math.Sqrt((ArrayMath.Norm(y) / ArrayMath.Norm(s))) * System.Math.Sqrt((50.0 / (50.0 + k)));
			alpha = alpha * System.Math.Sqrt(ArrayMath.Average(diag));
			Say(" alpha " + nf.Format(alpha));
			high = System.Math.Sqrt(high) / (2 * alpha);
			IDoubleUnaryOperator func = new ScaledSGDMinimizer.Lagrange(s, y, diag, alpha);
			double lamStar;
			if (func.ApplyAsDouble(low) > 0)
			{
				lamStar = GetRoot(func, low, high);
			}
			else
			{
				lamStar = 0.0;
				Say(" * ");
			}
			for (int i_1 = 0; i_1 < s.Length; i_1++)
			{
				diag[i_1] = (System.Math.Abs(y[i_1] * s[i_1]) + 2 * lamStar * diag[i_1]) / (s[i_1] * s[i_1] + 1e-8 + 2 * lamStar);
				//diag[i] = (y[i]*s[i] + 2*lamStar*diag[i])/(s[i]*s[i] + 2*lamStar);
				if (diag[i_1] <= 1.0 / aMax)
				{
					diag[i_1] = 1.0 / gain;
				}
			}
		}

		private double GetRoot(IDoubleUnaryOperator func, double lower, double upper)
		{
			double mid = 0.5 * (lower + upper);
			double Tol = 1e-8;
			double skew = 0.4;
			int count = 0;
			if (func.ApplyAsDouble(upper) > 0 || func.ApplyAsDouble(lower) < 0)
			{
				Say("LOWER AND UPPER SUPPLIED TO GET ROOT DO NOT BOUND THE ROOT.");
			}
			double fval = func.ApplyAsDouble(mid);
			while (System.Math.Abs(fval) > Tol)
			{
				count += 1;
				if (fval > 0)
				{
					lower = mid;
				}
				else
				{
					if (fval < 0)
					{
						upper = mid;
					}
				}
				mid = skew * lower + (1 - skew) * upper;
				fval = func.ApplyAsDouble(mid);
				if (count > 100)
				{
					break;
				}
			}
			Say("   " + nf.Format(mid) + "  f" + nf.Format(fval));
			return mid;
		}

		internal class Lagrange : IDoubleUnaryOperator
		{
			private readonly double[] s;

			private readonly double[] y;

			private readonly double[] d;

			private readonly double a;

			public Lagrange(double[] s, double[] y, double[] d, double a)
			{
				this.s = s;
				this.y = y;
				this.d = d;
				this.a = a;
			}

			public virtual double ApplyAsDouble(double lam)
			{
				double val = 0.0;
				for (int i = 0; i < s.Length; i++)
				{
					double tmp = (y[i] * s[i] + 2 * lam * d[i]) / (s[i] * s[i] + 2 * lam) - d[i];
					val += tmp * tmp;
				}
				val -= a * a;
				return val;
			}
		}

		[System.Serializable]
		public class Weights
		{
			public double[] w;

			public double[] d;

			private const long serialVersionUID = 814182172645533781L;

			public Weights(double[] wt)
			{
				// end static class lagrange
				w = wt;
			}

			public Weights(double[] wt, double[] di)
			{
				w = wt;
				d = di;
			}
		}

		public static void SerializeWeights(string serializePath, double[] weights)
		{
			SerializeWeights(serializePath, weights, null);
		}

		public static void SerializeWeights(string serializePath, double[] weights, double[] diag)
		{
			log.Info("Serializing weights to " + serializePath + "...");
			try
			{
				ScaledSGDMinimizer.Weights @out = new ScaledSGDMinimizer.Weights(weights, diag);
				IOUtils.WriteObjectToFile(@out, serializePath);
			}
			catch (Exception e)
			{
				log.Info("Error serializing to " + serializePath);
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static double[] GetWeights(string loadPath)
		{
			log.Info("Loading weights from " + loadPath + "...");
			double[] wt;
			ScaledSGDMinimizer.Weights w;
			w = IOUtils.ReadObjectFromFile(loadPath);
			wt = w.w;
			return wt;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static double[] GetDiag(string loadPath)
		{
			log.Info("Loading weights from " + loadPath + "...");
			double[] diag;
			ScaledSGDMinimizer.Weights w;
			w = IOUtils.ReadObjectFromFile(loadPath);
			diag = w.d;
			return diag;
		}
	}
}
