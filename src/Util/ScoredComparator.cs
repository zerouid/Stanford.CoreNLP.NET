using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>ScoredComparator allows one to compare Scored things.</summary>
	/// <remarks>
	/// ScoredComparator allows one to compare Scored things.
	/// There are two ScoredComparators, one which sorts in ascending order and
	/// the other in descending order. They are implemented as singletons.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	/// <version>2006/08/20</version>
	[System.Serializable]
	public sealed class ScoredComparator : IComparator<IScored>
	{
		private const long serialVersionUID = 1L;

		private const bool Ascending = true;

		private const bool Descending = false;

		public static readonly Edu.Stanford.Nlp.Util.ScoredComparator AscendingComparator = new Edu.Stanford.Nlp.Util.ScoredComparator(Ascending);

		public static readonly Edu.Stanford.Nlp.Util.ScoredComparator DescendingComparator = new Edu.Stanford.Nlp.Util.ScoredComparator(Descending);

		private readonly bool ascending;

		private ScoredComparator(bool ascending)
		{
			this.ascending = ascending;
		}

		public int Compare(IScored o1, IScored o2)
		{
			if (o1 == o2)
			{
				return 0;
			}
			double d1 = o1.Score();
			double d2 = o2.Score();
			if (ascending)
			{
				if (d1 < d2)
				{
					return -1;
				}
				if (d1 > d2)
				{
					return 1;
				}
			}
			else
			{
				if (d1 < d2)
				{
					return 1;
				}
				if (d1 > d2)
				{
					return -1;
				}
			}
			return 0;
		}

		public override bool Equals(object o)
		{
			if (o is Edu.Stanford.Nlp.Util.ScoredComparator)
			{
				Edu.Stanford.Nlp.Util.ScoredComparator sc = (Edu.Stanford.Nlp.Util.ScoredComparator)o;
				if (ascending == sc.ascending)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Return the hashCode: there are only two distinct comparators by
		/// equals().
		/// </summary>
		public override int GetHashCode()
		{
			if (ascending)
			{
				return (1 << 23);
			}
			else
			{
				return (1 << 23) + 1;
			}
		}

		public override string ToString()
		{
			return "ScoredComparator(" + (ascending ? "ascending" : "descending") + ")";
		}
	}
}
