using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Matches potentially multiple node (i.e does match across multiple tokens)</summary>
	/// <author>Angel Chang</author>
	public abstract class MultiNodePattern<T>
	{
		internal int minNodes = 1;

		internal int maxNodes = -1;

		internal bool greedyMatch = true;

		// Set the max number of nodes this pattern can match 
		/// <summary>
		/// Tries to match sequence of nodes starting of start
		/// Returns intervals (token offsets) of when the nodes matches
		/// </summary>
		/// <param name="nodes"/>
		/// <param name="start"/>
		protected internal abstract ICollection<Interval<int>> Match<_T0>(IList<_T0> nodes, int start)
			where _T0 : T;

		public virtual int GetMinNodes()
		{
			return minNodes;
		}

		public virtual void SetMinNodes(int minNodes)
		{
			this.minNodes = minNodes;
		}

		public virtual int GetMaxNodes()
		{
			return maxNodes;
		}

		public virtual void SetMaxNodes(int maxNodes)
		{
			this.maxNodes = maxNodes;
		}

		public virtual bool IsGreedyMatch()
		{
			return greedyMatch;
		}

		public virtual void SetGreedyMatch(bool greedyMatch)
		{
			this.greedyMatch = greedyMatch;
		}

		protected internal class IntersectMultiNodePattern<T> : MultiNodePattern<T>
		{
			internal IList<MultiNodePattern<T>> nodePatterns;

			protected internal IntersectMultiNodePattern(IList<MultiNodePattern<T>> nodePatterns)
			{
				this.nodePatterns = nodePatterns;
			}

			protected internal override ICollection<Interval<int>> Match<_T0>(IList<_T0> nodes, int start)
			{
				ICollection<Interval<int>> matched = null;
				foreach (MultiNodePattern<T> p in nodePatterns)
				{
					ICollection<Interval<int>> m = p.Match(nodes, start);
					if (m == null || m.Count == 0)
					{
						return null;
					}
					if (matched == null)
					{
						matched = m;
					}
					else
					{
						matched.RetainAll(m);
						if (m.Count == 0)
						{
							return null;
						}
					}
				}
				return matched;
			}
		}

		protected internal class UnionMultiNodePattern<T> : MultiNodePattern<T>
		{
			internal IList<MultiNodePattern<T>> nodePatterns;

			protected internal UnionMultiNodePattern(IList<MultiNodePattern<T>> nodePatterns)
			{
				this.nodePatterns = nodePatterns;
			}

			protected internal override ICollection<Interval<int>> Match<_T0>(IList<_T0> nodes, int start)
			{
				ICollection<Interval<int>> matched = null;
				foreach (MultiNodePattern<T> p in nodePatterns)
				{
					ICollection<Interval<int>> m = p.Match(nodes, start);
					if (m != null && m.Count > 0)
					{
						if (matched == null)
						{
							matched = m;
						}
						else
						{
							foreach (Interval<int> i in m)
							{
								if (!matched.Contains(i))
								{
									matched.Add(i);
								}
							}
						}
					}
				}
				return matched;
			}
		}
	}
}
