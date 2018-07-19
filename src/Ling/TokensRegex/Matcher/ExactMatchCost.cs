using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex.Matcher
{
	/// <summary>Exact match cost function</summary>
	/// <author>Angel Chang</author>
	public sealed class ExactMatchCost<K, V> : MatchCostFunction.AbstractMatchCostFunction<K, V>
	{
		internal readonly double mismatchCost;

		internal readonly double insCost;

		internal readonly double delCost;

		public ExactMatchCost()
			: this(1)
		{
		}

		public ExactMatchCost(double mismatchCost)
			: this(mismatchCost, 1, 1)
		{
		}

		public ExactMatchCost(double mismatchCost, double insCost, double delCost)
		{
			this.mismatchCost = mismatchCost;
			this.insCost = insCost;
			this.delCost = delCost;
		}

		public override double Cost(K k1, K k2, int n)
		{
			if (k1 != null)
			{
				if (k2 == null)
				{
					return delCost;
				}
				return (k1.Equals(k2)) ? 0 : mismatchCost;
			}
			else
			{
				return (k2 == null) ? 0 : insCost;
			}
		}
	}
}
