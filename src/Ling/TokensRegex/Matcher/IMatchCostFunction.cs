using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Ling.Tokensregex.Matcher
{
	/// <summary>Represents the cost of a match</summary>
	/// <author>Angel Chang</author>
	public interface IMatchCostFunction<K, V>
	{
		// pairwise cost of replacing k1 with k2 at position n
		double Cost(K k1, K k2, int n);

		// cost of adding the sequence k with value v to the match
		double MultiMatchDeltaCost(IList<K> k, V v, IList<Match<K, V>> prevMultiMatch, IList<Match<K, V>> curMultiMatch);

		public abstract class AbstractMatchCostFunction<K, V> : IMatchCostFunction<K, V>
		{
			// pairwise cost of replacing k1 with k2,k3 at position n
			//public double cost(K k1, K k2, K k3, int n);
			// pairwise cost of replacing k1 with k2,k3 at position n
			public virtual double Cost(K k1, K k2, int n)
			{
				return 0;
			}

			public virtual double Cost(K k1, K k2, K k3, int n)
			{
				return 0;
			}

			public virtual double MultiMatchDeltaCost(IList<K> k, V v, IList<Match<K, V>> prevMultiMatch, IList<Match<K, V>> curMultiMatch)
			{
				return 0;
			}
		}
	}

	public static class MatchCostFunctionConstants
	{
	}
}
