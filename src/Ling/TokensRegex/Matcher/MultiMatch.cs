using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Ling.Tokensregex.Matcher
{
	/// <summary>Represent multimatches</summary>
	/// <author>Angel Chang</author>
	public class MultiMatch<K, V> : Match<K, V>
	{
		internal IList<Match<K, V>> multimatches;

		public MultiMatch()
		{
		}

		public MultiMatch(IList<K> matched, V value, int begin, int end, IList<Match<K, V>> multimatches)
		{
			this.matched = matched;
			this.value = value;
			this.begin = begin;
			this.end = end;
			this.multimatches = multimatches;
		}

		public virtual IList<Match<K, V>> GetMultimatches()
		{
			return multimatches;
		}

		public virtual IList<IList<K>> GetMultimatched()
		{
			if (multimatches == null)
			{
				return null;
			}
			IList<IList<K>> multimatched = new List<IList<K>>(multimatches.Count);
			foreach (Match<K, V> m in multimatches)
			{
				multimatched.Add(m.GetMatched());
			}
			return multimatched;
		}

		public virtual IList<V> GetMultivalues()
		{
			if (multimatches == null)
			{
				return null;
			}
			IList<V> multivalues = new List<V>(multimatches.Count);
			foreach (Match<K, V> m in multimatches)
			{
				multivalues.Add(m.GetValue());
			}
			return multivalues;
		}

		// Offsets in the original string to which each multimatch is aligned to
		public virtual IList<IHasInterval<int>> GetMultioffsets()
		{
			if (multimatches == null)
			{
				return null;
			}
			IList<IHasInterval<int>> multioffsets = new List<IHasInterval<int>>(multimatches.Count);
			foreach (Match<K, V> m in multimatches)
			{
				multioffsets.Add(m.GetInterval());
			}
			return multioffsets;
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
			Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.MultiMatch that = (Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.MultiMatch)o;
			if (multimatches != null ? !multimatches.Equals(that.multimatches) : that.multimatches != null)
			{
				return false;
			}
			return true;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			if (multimatches != null)
			{
				sb.Append("[" + StringUtils.Join(GetMultimatches(), ", ") + "]");
			}
			else
			{
				sb.Append(base.ToString());
			}
			return sb.ToString();
		}
	}
}
