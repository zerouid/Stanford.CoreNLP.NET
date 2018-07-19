using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// A differentiable function that caches the last evaluation of its value and
	/// derivative.
	/// </summary>
	/// <author>Dan Klein</author>
	public abstract class AbstractCachingDiffFunction : IDiffFunction, IHasInitial
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(AbstractCachingDiffFunction));

		private double[] lastX;

		private int fEvaluations;

		protected internal double[] derivative;

		protected internal double value;

		private readonly Random generator = new Random(2147483647L);

		// = null;
		// = 0;
		// = null;
		// = 0.0;
		public virtual bool GradientCheck()
		{
			return GradientCheck(100, 50, Initial());
		}

		public virtual bool GradientCheck(int numOfChecks, int numOfRandomChecks, double[] x)
		{
			double epsilon = 1e-5;
			double diffThreshold = 0.01;
			double diffPctThreshold = 0.1;
			double twoEpsilon = epsilon * 2;
			int xLen = x.Length;
			// log.info("\n\n\ncalling derivativeAt");
			DerivativeAt(x);
			double[] savedDeriv = new double[xLen];
			System.Array.Copy(derivative, 0, savedDeriv, 0, derivative.Length);
			int interval = Math.Max(1, x.Length / numOfChecks);
			ICollection<int> indicesToCheck = new TreeSet<int>();
			for (int paramIndex = 0; paramIndex < xLen; paramIndex += interval)
			{
				indicesToCheck.Add(paramIndex);
			}
			for (int i = xLen - 1; i >= 0 && i > xLen - numOfChecks; i--)
			{
				indicesToCheck.Add(i);
			}
			for (int i_1 = 1; i_1 < xLen && i_1 < numOfChecks; i_1++)
			{
				indicesToCheck.Add(i_1);
			}
			for (int i_2 = 0; i_2 < numOfRandomChecks; i_2++)
			{
				indicesToCheck.Add(generator.NextInt(xLen));
			}
			bool returnVal = true;
			IList<int> badIndices = new List<int>();
			foreach (int paramIndex_1 in indicesToCheck)
			{
				double oldX = x[paramIndex_1];
				x[paramIndex_1] = oldX + epsilon;
				// log.info("\n\n\ncalling valueAt1");
				double plusVal = ValueAt(x);
				x[paramIndex_1] = oldX - epsilon;
				// log.info("\n\n\ncalling valueAt2");
				double minusVal = ValueAt(x);
				double appDeriv = (plusVal - minusVal) / twoEpsilon;
				double calcDeriv = savedDeriv[paramIndex_1];
				double diff = Math.Abs(appDeriv - calcDeriv);
				double pct = diff / Math.Min(Math.Abs(appDeriv), Math.Abs(calcDeriv));
				if (diff > diffThreshold && pct > diffPctThreshold)
				{
					System.Console.Error.Printf("Grad fail at %2d, appGrad=%9.7f, calcGrad=%9.7f, diff=%9.7f, pct=%9.7f\n", paramIndex_1, appDeriv, calcDeriv, diff, pct);
					badIndices.Add(paramIndex_1);
					returnVal = false;
				}
				else
				{
					System.Console.Error.Printf("Grad good at %2d, appGrad=%9.7f, calcGrad=%9.7f, diff=%9.7f, pct=%9.7f\n", paramIndex_1, appDeriv, calcDeriv, diff, pct);
				}
				x[paramIndex_1] = oldX;
			}
			if (returnVal)
			{
				System.Console.Error.Printf("ALL gradients passed. Yay!\n");
			}
			else
			{
				log.Info("Bad indices: ");
				for (int i_3 = 0; i_3 < badIndices.Count && i_3 < 10; ++i_3)
				{
					log.Info(" " + badIndices[i_3]);
				}
				if (badIndices.Count >= 10)
				{
					log.Info(" (...)");
				}
				log.Info();
			}
			return returnVal;
		}

		/// <summary>
		/// Calculate the value at x and the derivative
		/// and save them in the respective fields.
		/// </summary>
		/// <param name="x">The point at which to calculate the function</param>
		protected internal abstract void Calculate(double[] x);

		/// <summary>Clears the cache in a way that doesn't require reallocation :-)</summary>
		protected internal virtual void ClearCache()
		{
			if (lastX != null)
			{
				lastX[0] = double.NaN;
			}
		}

		public virtual double[] Initial()
		{
			double[] initial = new double[DomainDimension()];
			// Arrays.fill(initial, 0.0); // You get zero fill of array for free in Java! (Like it or not....)
			return initial;
		}

		public virtual double[] RandomInitial()
		{
			double[] initial = new double[DomainDimension()];
			for (int i = 0; i < initial.Length; i++)
			{
				initial[i] = generator.NextDouble();
			}
			return initial;
		}

		protected internal static void Copy(double[] copy, double[] orig)
		{
			System.Array.Copy(orig, 0, copy, 0, orig.Length);
		}

		public virtual void Ensure(double[] x)
		{
			if (Arrays.Equals(x, lastX))
			{
				return;
			}
			if (lastX == null)
			{
				lastX = new double[DomainDimension()];
			}
			if (derivative == null)
			{
				derivative = new double[DomainDimension()];
			}
			Copy(lastX, x);
			fEvaluations += 1;
			Calculate(x);
		}

		public virtual double ValueAt(double[] x)
		{
			Ensure(x);
			return value;
		}

		public virtual double[] DerivativeAt(double[] x)
		{
			Ensure(x);
			return derivative;
		}

		public virtual double LastValue()
		{
			return value;
		}

		public virtual double[] GetDerivative()
		{
			return derivative;
		}

		public abstract int DomainDimension();
	}
}
