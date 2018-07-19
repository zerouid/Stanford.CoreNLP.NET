using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// Static methods for operating on
	/// <see cref="Distributions"/>
	/// s.
	/// In general, if a method is operating on a pair of Distribution objects, we imagine that the
	/// set of possible keys for each Distribution is the same.
	/// Therefore we require that d1.numberOFKeys = d2.numberOfKeys and that the number of keys in the union
	/// of the two key sets &lt;= numKeys
	/// </summary>
	/// <author>Jeff Michels (jmichels@stanford.edu)</author>
	public class Distributions
	{
		private Distributions()
		{
		}

		protected internal static ICollection<K> GetSetOfAllKeys<K>(Distribution<K> d1, Distribution<K> d2)
		{
			if (d1.GetNumberOfKeys() != d2.GetNumberOfKeys())
			{
				throw new Exception("Tried to compare two Distribution<K> objects but d1.numberOfKeys != d2.numberOfKeys");
			}
			ICollection<K> allKeys = Generics.NewHashSet(d1.GetCounter().KeySet());
			Sharpen.Collections.AddAll(allKeys, d2.GetCounter().KeySet());
			if (allKeys.Count > d1.GetNumberOfKeys())
			{
				throw new Exception("Tried to compare two Distribution<K> objects but d1.counter intersect d2.counter > numberOfKeys");
			}
			return allKeys;
		}

		/// <summary>Returns a double between 0 and 1 representing the overlap of d1 and d2.</summary>
		/// <remarks>
		/// Returns a double between 0 and 1 representing the overlap of d1 and d2.
		/// Equals 0 if there is no overlap, equals 1 iff d1==d2
		/// </remarks>
		public static double Overlap<K>(Distribution<K> d1, Distribution<K> d2)
		{
			ICollection<K> allKeys = GetSetOfAllKeys(d1, d2);
			double result = 0.0;
			double remainingMass1 = 1.0;
			double remainingMass2 = 1.0;
			foreach (K key in allKeys)
			{
				double p1 = d1.ProbabilityOf(key);
				double p2 = d2.ProbabilityOf(key);
				remainingMass1 -= p1;
				remainingMass2 -= p2;
				result += Math.Min(p1, p2);
			}
			result += Math.Min(remainingMass1, remainingMass2);
			return result;
		}

		/// <summary>Returns a new Distribution<K> with counts averaged from the two given Distributions.</summary>
		/// <remarks>
		/// Returns a new Distribution<K> with counts averaged from the two given Distributions.
		/// The average Distribution<K> will contain the union of keys in both
		/// source Distributions, and each count will be the weighted average of the two source
		/// counts for that key,  a missing count in one Distribution
		/// is treated as if it has probability equal to that returned by the probabilityOf() function.
		/// </remarks>
		/// <returns>
		/// A new distribution with counts that are the mean of the resp. counts
		/// in the given distributions with the remaining probability mass adjusted accordingly.
		/// </returns>
		public static Distribution<K> WeightedAverage<K>(Distribution<K> d1, double w1, Distribution<K> d2)
		{
			double w2 = 1.0 - w1;
			ICollection<K> allKeys = GetSetOfAllKeys(d1, d2);
			int numKeys = d1.GetNumberOfKeys();
			ICounter<K> c = new ClassicCounter<K>();
			foreach (K key in allKeys)
			{
				double newProbability = d1.ProbabilityOf(key) * w1 + d2.ProbabilityOf(key) * w2;
				c.SetCount(key, newProbability);
			}
			return (Distribution.GetDistributionFromPartiallySpecifiedCounter(c, numKeys));
		}

		public static Distribution<K> Average<K>(Distribution<K> d1, Distribution<K> d2)
		{
			return WeightedAverage(d1, 0.5, d2);
		}

		/// <summary>Calculates the KL divergence between the two distributions.</summary>
		/// <remarks>
		/// Calculates the KL divergence between the two distributions.
		/// That is, it calculates KL(from || to).
		/// In other words, how well can d1 be represented by d2.
		/// if there is some value in d1 that gets zero prob in d2, then return positive infinity.
		/// </remarks>
		/// <returns>The KL divergence between the distributions</returns>
		public static double KlDivergence<K>(Distribution<K> from, Distribution<K> to)
		{
			ICollection<K> allKeys = GetSetOfAllKeys(from, to);
			int numKeysRemaining = from.GetNumberOfKeys();
			double result = 0.0;
			double assignedMass1 = 0.0;
			double assignedMass2 = 0.0;
			double log2 = Math.Log(2.0);
			double p1;
			double p2;
			double epsilon = 1e-10;
			foreach (K key in allKeys)
			{
				p1 = from.ProbabilityOf(key);
				p2 = to.ProbabilityOf(key);
				numKeysRemaining--;
				assignedMass1 += p1;
				assignedMass2 += p2;
				if (p1 < epsilon)
				{
					continue;
				}
				double logFract = Math.Log(p1 / p2);
				if (logFract == double.PositiveInfinity)
				{
					System.Console.Out.WriteLine("Didtributions.kldivergence returning +inf: p1=" + p1 + ", p2=" + p2);
					System.Console.Out.Flush();
					return double.PositiveInfinity;
				}
				// can't recover
				result += p1 * (logFract / log2);
			}
			// express it in log base 2
			if (numKeysRemaining != 0)
			{
				p1 = (1.0 - assignedMass1) / numKeysRemaining;
				if (p1 > epsilon)
				{
					p2 = (1.0 - assignedMass2) / numKeysRemaining;
					double logFract = Math.Log(p1 / p2);
					if (logFract == double.PositiveInfinity)
					{
						System.Console.Out.WriteLine("Distributions.klDivergence (remaining mass) returning +inf: p1=" + p1 + ", p2=" + p2);
						System.Console.Out.Flush();
						return double.PositiveInfinity;
					}
					// can't recover
					result += numKeysRemaining * p1 * (logFract / log2);
				}
			}
			// express it in log base 2
			return result;
		}

		/// <summary>Calculates the Jensen-Shannon divergence between the two distributions.</summary>
		/// <remarks>
		/// Calculates the Jensen-Shannon divergence between the two distributions.
		/// That is, it calculates 1/2 [KL(d1 || avg(d1,d2)) + KL(d2 || avg(d1,d2))] .
		/// </remarks>
		/// <returns>The KL divergence between the distributions</returns>
		public static double JensenShannonDivergence<K>(Distribution<K> d1, Distribution<K> d2)
		{
			Distribution<K> average = Average(d1, d2);
			double kl1 = KlDivergence(d1, average);
			double kl2 = KlDivergence(d2, average);
			double js = (kl1 + kl2) / 2.0;
			return js;
		}

		/// <summary>Calculates the skew divergence between the two distributions.</summary>
		/// <remarks>
		/// Calculates the skew divergence between the two distributions.
		/// That is, it calculates KL(d1 || (d2*skew + d1*(1-skew))) .
		/// In other words, how well can d1 be represented by a "smoothed" d2.
		/// </remarks>
		/// <returns>The skew divergence between the distributions</returns>
		public static double SkewDivergence<K>(Distribution<K> d1, Distribution<K> d2, double skew)
		{
			Distribution<K> average = WeightedAverage(d2, skew, d1);
			return KlDivergence(d1, average);
		}

		/// <summary>
		/// Calculates the information radius (aka the Jensen-Shannon divergence)
		/// between the two Distributions.
		/// </summary>
		/// <remarks>
		/// Calculates the information radius (aka the Jensen-Shannon divergence)
		/// between the two Distributions.  This measure is defined as:
		/// <blockquote> iRad(p,q) = D(p||(p+q)/2)+D(q,(p+q)/2) </blockquote>
		/// where p is one Distribution, q is the other distribution, and D(p||q) is the
		/// KL divergence bewteen p and q.  Note that iRad(p,q) = iRad(q,p).
		/// </remarks>
		/// <returns>The information radius between the distributions</returns>
		public static double InformationRadius<K>(Distribution<K> d1, Distribution<K> d2)
		{
			Distribution<K> avg = Average(d1, d2);
			// (p+q)/2
			return (KlDivergence(d1, avg) + KlDivergence(d2, avg));
		}
	}
}
