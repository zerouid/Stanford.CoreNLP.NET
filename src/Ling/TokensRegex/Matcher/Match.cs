using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Ling.Tokensregex.Matcher
{
	/// <summary>Represent a matched span over sequence of elements</summary>
	/// <author>Angel Chang</author>
	public class Match<K, V> : IHasInterval<int>
	{
		internal IList<K> matched;

		internal V value;

		internal int begin;

		internal int end;

		internal object customMatchObject;

		[System.NonSerialized]
		internal Interval<int> span;

		public Match()
		{
		}

		public Match(IList<K> matched, V value, int begin, int end)
		{
			/* List of elements that were actually matched */
			/* Value corresponding to the matched span */
			/* Start offset of the span */
			/* End offset of the span */
			// Custom match object
			this.matched = matched;
			this.value = value;
			this.begin = begin;
			this.end = end;
		}

		public virtual IList<K> GetMatched()
		{
			return matched;
		}

		public virtual int GetMatchedLength()
		{
			return (matched != null) ? matched.Count : 0;
		}

		public virtual V GetValue()
		{
			return value;
		}

		public virtual int GetBegin()
		{
			return begin;
		}

		public virtual int GetEnd()
		{
			return end;
		}

		public virtual object GetCustom()
		{
			return customMatchObject;
		}

		public virtual void SetCustom(object customMatchObject)
		{
			this.customMatchObject = customMatchObject;
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
			Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.Match match = (Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.Match)o;
			if (begin != match.begin)
			{
				return false;
			}
			if (end != match.end)
			{
				return false;
			}
			if (matched != null ? !matched.Equals(match.matched) : match.matched != null)
			{
				return false;
			}
			if (value != null ? !value.Equals(match.value) : match.value != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = matched != null ? matched.GetHashCode() : 0;
			result = 31 * result + begin;
			result = 31 * result + end;
			return result;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[" + ((matched != null) ? StringUtils.Join(matched, " - ") : string.Empty) + "]");
			sb.Append(" -> ").Append(value);
			sb.Append(" at (").Append(begin);
			sb.Append(",").Append(end).Append(")");
			return sb.ToString();
		}

		public virtual Interval<int> GetInterval()
		{
			if (span == null)
			{
				span = Interval.ToInterval(begin, end, Interval.IntervalOpenEnd);
			}
			return span;
		}
	}
}
