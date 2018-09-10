using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// Online Limited-Memory Quasi-Newton BFGS implementation based on the algorithms in
	/// <p>
	/// Nocedal, Jorge, and Stephen J.
	/// </summary>
	/// <remarks>
	/// Online Limited-Memory Quasi-Newton BFGS implementation based on the algorithms in
	/// <p>
	/// Nocedal, Jorge, and Stephen J. Wright.  2000.  Numerical Optimization.  Springer.  pp. 224--
	/// <p>
	/// and modified to the online version presented in
	/// <p>
	/// A Stocahstic Quasi-Newton Method for Online Convex Optimization
	/// Schraudolph, Yu, Gunter (2007)
	/// <p>
	/// As of now, it requires a
	/// Stochastic differentiable function (AbstractStochasticCachingDiffFunction) as input.
	/// <p/>
	/// The basic way to use the minimizer is with a null constructor, then
	/// the simple minimize method:
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// THIS IS NOT UPDATE FOR THE STOCHASTIC VERSION YET.
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// <p/>
	/// <p><code>Minimizer qnm = new QNMinimizer();</code>
	/// <br /><code>DiffFunction df = new SomeDiffFunction();</code>
	/// <br /><code>double tol = 1e-4;</code>
	/// <br /><code>double[] initial = getInitialGuess();</code>
	/// <br /><code>double[] minimum = qnm.minimize(df,tol,initial);</code>
	/// <p/>
	/// <p/>
	/// If you do not choose a value of M, it will use the max amount of memory
	/// available, up to M of 20.  This will slow things down a bit at first due
	/// to forced garbage collection, but is probably faster overall b/c you are
	/// guaranteed the largest possible M.
	/// The Stochastic version was written by Alex Kleeman, but about 95% of the code
	/// was taken directly from the previous QNMinimizer written mostly by Jenny.
	/// </remarks>
	/// <author><a href="mailto:jrfinkel@stanford.edu">Jenny Finkel</a></author>
	/// <author>Galen Andrew</author>
	/// <author><a href="mailto:akleeman@stanford.edu">Alex Kleeman</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public class SQNMinimizer<T> : StochasticMinimizer<T>
		where T : Func
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Optimization.SQNMinimizer));

		private int M = 0;

		private double lambda = 1.0;

		private double cPosDef = 1;

		private double epsilon = 1e-10;

		private IList<double[]> sList = new List<double[]>();

		private IList<double[]> yList = new List<double[]>();

		private IList<double> roList = new List<double>();

		internal double[] dir;

		internal double[] s;

		internal double[] y;

		internal double ro;

		public virtual void SetM(int m)
		{
			M = m;
		}

		public SQNMinimizer(int m)
		{
			M = m;
		}

		public SQNMinimizer()
		{
		}

		public SQNMinimizer(int mem, double initialGain, int batchSize, bool output)
		{
			gain = initialGain;
			bSize = batchSize;
			this.M = mem;
			this.outputIterationsToFile = output;
		}

		protected internal override string GetName()
		{
			int g = (int)(gain * 1000.0);
			return "SQN" + bSize + "_g" + g;
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

		public override Pair<int, double> Tune(Func function, double[] initial, long msPerTest)
		{
			log.Info("No tuning set yet");
			return new Pair<int, double>(bSize, gain);
		}

		/// <exception cref="Edu.Stanford.Nlp.Optimization.SQNMinimizer.SurpriseConvergence"/>
		private void ComputeDir(double[] dir, double[] fg)
		{
			System.Array.Copy(fg, 0, dir, 0, fg.Length);
			int mmm = sList.Count;
			double[] @as = new double[mmm];
			double[] factors = new double[dir.Length];
			for (int i = mmm - 1; i >= 0; i--)
			{
				@as[i] = roList[i] * ArrayMath.InnerProduct(sList[i], dir);
				PlusAndConstMult(dir, yList[i], -@as[i], dir);
			}
			// multiply by hessian approximation
			if (mmm != 0)
			{
				double[] y = yList[mmm - 1];
				double yDotY = ArrayMath.InnerProduct(y, y);
				if (yDotY == 0)
				{
					throw new SQNMinimizer.SurpriseConvergence("Y is 0!!");
				}
				double gamma = ArrayMath.InnerProduct(sList[mmm - 1], y) / yDotY;
				ArrayMath.MultiplyInPlace(dir, gamma);
			}
			else
			{
				if (mmm == 0)
				{
					//This is a safety feature preventing too large of an initial step (see Yu Schraudolph Gunter)
					ArrayMath.MultiplyInPlace(dir, epsilon);
				}
			}
			for (int i_1 = 0; i_1 < mmm; i_1++)
			{
				double b = roList[i_1] * ArrayMath.InnerProduct(yList[i_1], dir);
				PlusAndConstMult(dir, sList[i_1], cPosDef * @as[i_1] - b, dir);
				PlusAndConstMult(ArrayMath.PairwiseMultiply(yList[i_1], sList[i_1]), factors, 1, factors);
			}
			ArrayMath.MultiplyInPlace(dir, -1);
		}

		protected internal override void Init(AbstractStochasticCachingDiffFunction func)
		{
			sList = new List<double[]>();
			yList = new List<double[]>();
			dir = new double[func.DomainDimension()];
		}

		protected internal override void TakeStep(AbstractStochasticCachingDiffFunction dfunction)
		{
			try
			{
				ComputeDir(dir, newGrad);
			}
			catch (SQNMinimizer.SurpriseConvergence)
			{
				ClearStuff();
			}
			double thisGain = gain * GainSchedule(k, 5 * numBatches);
			for (int i = 0; i < x.Length; i++)
			{
				newX[i] = x[i] + thisGain * dir[i];
			}
			//Get a new pair...
			Say(" A ");
			if (M > 0 && sList.Count == M || sList.Count == M)
			{
				s = sList.Remove(0);
				y = yList.Remove(0);
			}
			else
			{
				s = new double[x.Length];
				y = new double[x.Length];
			}
			dfunction.recalculatePrevBatch = true;
			System.Array.Copy(dfunction.DerivativeAt(newX, bSize), 0, y, 0, grad.Length);
			// compute s_k, y_k
			ro = 0;
			for (int i_1 = 0; i_1 < x.Length; i_1++)
			{
				s[i_1] = newX[i_1] - x[i_1];
				y[i_1] = y[i_1] - newGrad[i_1] + lambda * s[i_1];
				ro += s[i_1] * y[i_1];
			}
			ro = 1.0 / ro;
			sList.Add(s);
			yList.Add(y);
			roList.Add(ro);
		}

		private void ClearStuff()
		{
			sList = null;
			yList = null;
			roList = null;
		}

		[System.Serializable]
		private class SurpriseConvergence : Exception
		{
			private const long serialVersionUID = -4377976289620760327L;

			public SurpriseConvergence(string s)
				: base(s)
			{
			}
		}
	}
}
