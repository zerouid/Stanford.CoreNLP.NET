using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex.Matcher
{
	/// <summary>Represents an approximate match with a cost</summary>
	/// <author>Angel Chang</author>
	public class ApproxMatch<K, V> : MultiMatch<K, V>
	{
		internal double cost;

		internal Interval<int>[] alignments;

		public ApproxMatch()
		{
		}

		public ApproxMatch(IList<K> matched, V value, int begin, int end, double cost)
		{
			// Tracks alignments from original sequence to matched sequence (null indicates not aligned)
			this.matched = matched;
			this.value = value;
			this.begin = begin;
			this.end = end;
			this.cost = cost;
		}

		public ApproxMatch(IList<K> matched, V value, int begin, int end, IList<Match<K, V>> multimatches, double cost)
		{
			this.matched = matched;
			this.value = value;
			this.begin = begin;
			this.end = end;
			this.multimatches = multimatches;
			this.cost = cost;
		}

		public ApproxMatch(IList<K> matched, V value, int begin, int end, IList<Match<K, V>> multimatches, double cost, Interval[] alignments)
		{
			this.matched = matched;
			this.value = value;
			this.begin = begin;
			this.end = end;
			this.multimatches = multimatches;
			this.cost = cost;
			this.alignments = alignments;
		}

		public virtual double GetCost()
		{
			return cost;
		}

		public virtual Interval<int>[] GetAlignments()
		{
			return alignments;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o == null || GetType() != o.GetType())
			{
				return false;
			}
			if (!base.Equals(o))
			{
				return false;
			}
			Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.ApproxMatch that = (Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.ApproxMatch)o;
			if (double.Compare(that.cost, cost) != 0)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = base.GetHashCode();
			long temp;
			temp = double.DoubleToLongBits(cost);
			result = 31 * result + (int)(temp ^ ((long)(((ulong)temp) >> 32)));
			return result;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("(");
			sb.Append(base.ToString());
			sb.Append(",").Append(cost);
			if (alignments != null)
			{
				sb.Append(", [").Append(StringUtils.Join(alignments, ", ")).Append("]");
			}
			sb.Append(")");
			return sb.ToString();
		}
	}
}
