using System;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>Represents a Gamma distribution.</summary>
	/// <remarks>
	/// Represents a Gamma distribution.  The way that samples are drawn is
	/// stolen from Yee Whye Teh's code.  It won't give the probability of a variable because
	/// gamma is a continuous distribution.  should it give the mass at that point?
	/// </remarks>
	/// <author>Jenny Finkel</author>
	[System.Serializable]
	public class Gamma : IProbabilityDistribution<double>
	{
		private const long serialVersionUID = -2992079318379176178L;

		public readonly double alpha;

		public Gamma(double alpha)
		{
			this.alpha = alpha;
		}

		public virtual double DrawSample(Random random)
		{
			return DrawSample(random, alpha);
		}

		public static double DrawSample(Random random, double alpha)
		{
			if (alpha <= 0.0)
			{
				/* Not well defined, set to zero and skip. */
				return 0.0;
			}
			else
			{
				if (alpha == 1.0)
				{
					/* Exponential */
					return -Math.Log(Math.Random());
				}
				else
				{
					if (alpha < 1.0)
					{
						/* Use Johnks generator */
						double cc = 1.0 / alpha;
						double dd = 1.0 / (1.0 - alpha);
						while (true)
						{
							double xx = Math.Pow(Math.Random(), cc);
							double yy = xx + Math.Pow(Math.Random(), dd);
							if (yy <= 1.0)
							{
								return -Math.Log(Math.Random()) * xx / yy;
							}
						}
					}
					else
					{
						/* Use bests algorithm */
						double bb = alpha - 1.0;
						double cc = 3.0 * alpha - 0.75;
						while (true)
						{
							double uu = Math.Random();
							double vv = Math.Random();
							double ww = uu * (1.0 - uu);
							double yy = Math.Sqrt(cc / ww) * (uu - 0.5);
							double xx = bb + yy;
							if (xx >= 0)
							{
								double zz = 64.0 * ww * ww * ww * vv * vv;
								if ((zz <= (1.0 - 2.0 * yy * yy / xx)) || (Math.Log(zz) <= 2.0 * (bb * Math.Log(xx / bb) - yy)))
								{
									return xx;
								}
							}
						}
					}
				}
			}
		}

		// Generalized Random sampling.
		public static double DrawSample(Random r, double a, double b)
		{
			return DrawSample(r, a) * b;
		}

		public virtual double ProbabilityOf(double x)
		{
			return 0.0;
		}

		// cos its not discrete
		public virtual double LogProbabilityOf(double x)
		{
			return 0.0;
		}

		// cos its not discrete
		public override int GetHashCode()
		{
			return (alpha).GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (!(o is Edu.Stanford.Nlp.Stats.Gamma))
			{
				return false;
			}
			return ((Edu.Stanford.Nlp.Stats.Gamma)o).alpha == alpha;
		}
	}
}
