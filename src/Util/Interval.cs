using System;





namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Represents a interval of a generic type E that is comparable.</summary>
	/// <remarks>
	/// Represents a interval of a generic type E that is comparable.
	/// An interval is an ordered pair where the first element is less
	/// than the second.
	/// Only full intervals are currently supported
	/// (i.e., both endpoints have to be specified - cannot be null).
	/// Provides functions for computing relationships between intervals.
	/// For flags that indicate relationship between two intervals, the following convention is used:
	/// SS = relationship between start of first interval and start of second interval
	/// SE = relationship between start of first interval and end of second interval
	/// ES = relationship between end of first interval and start of second interval
	/// EE = relationship between end of first interval and end of second interval
	/// </remarks>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class Interval<E> : Pair<E, E>, IHasInterval<E>
		where E : IComparable<E>
	{
		/// <summary>
		/// Flag indicating that an interval's begin point is not inclusive
		/// (by default, begin points are inclusive)
		/// </summary>
		public const int IntervalOpenBegin = unchecked((int)(0x01));

		/// <summary>
		/// Flag indicating that an interval's end point is not inclusive
		/// (by default, begin points are inclusive)
		/// </summary>
		public const int IntervalOpenEnd = unchecked((int)(0x02));

		private readonly int flags;

		/// <summary>RelType gives the basic types of relations between two intervals</summary>
		public enum RelType
		{
			Before,
			After,
			Equal,
			BeginMeetEnd,
			EndMeetBegin,
			Contain,
			Inside,
			Overlap,
			Unknown,
			None
		}

		protected internal const int RelFlagsSame = unchecked((int)(0x0001));

		protected internal const int RelFlagsBefore = unchecked((int)(0x0002));

		protected internal const int RelFlagsAfter = unchecked((int)(0x0004));

		protected internal const int RelFlagsUnknown = unchecked((int)(0x0007));

		protected internal const int RelFlagsSsShift = 0;

		protected internal const int RelFlagsSeShift = 1 * 4;

		protected internal const int RelFlagsEsShift = 2 * 4;

		protected internal const int RelFlagsEeShift = 3 * 4;

		/// <summary>
		/// Both intervals have the same start point
		/// <pre>
		/// |---- interval 1 ----?
		/// |---- interval 2 ----?
		/// </pre>
		/// </summary>
		public const int RelFlagsSsSame = unchecked((int)(0x0001));

		/// <summary>
		/// The first interval starts before the second starts
		/// <pre>
		/// |---- interval 1 ----?
		/// |---- interval 2 ----?
		/// or
		/// |-- interval 1 --?
		/// |---- interval 2 ----?
		/// </pre>
		/// </summary>
		public const int RelFlagsSsBefore = unchecked((int)(0x0002));

		/// <summary>
		/// The first interval starts after the second starts
		/// <pre>
		/// |---- interval 1 ----?
		/// |---- interval 2 ----?
		/// or
		/// |---- interval 1 ----?
		/// |-- interval 2 --?
		/// </pre>
		/// </summary>
		public const int RelFlagsSsAfter = unchecked((int)(0x0004));

		/// <summary>
		/// The relationship between the start points of the
		/// two intervals is unknown (used for fuzzy intervals)
		/// </summary>
		public const int RelFlagsSsUnknown = unchecked((int)(0x0007));

		/// <summary>
		/// The start point of the first interval is the same
		/// as the end point of the second interval
		/// (the second interval is before the first)
		/// <pre>
		/// |---- interval 1 ----?
		/// ?---- interval 2 ---|
		/// </pre>
		/// </summary>
		public const int RelFlagsSeSame = unchecked((int)(0x0010));

		/// <summary>
		/// The start point of the first interval is before
		/// the end point of the second interval
		/// (the two intervals overlap)
		/// <pre>
		/// |---- interval 1 ----?
		/// ?---- interval 2 ---|
		/// </pre>
		/// </summary>
		public const int RelFlagsSeBefore = unchecked((int)(0x0020));

		/// <summary>
		/// The start point of the first interval is after
		/// the end point of the second interval
		/// (the second interval is before the first)
		/// <pre>
		/// |---- interval 1 ---?
		/// ?-- interval 2 ---|
		/// </pre>
		/// </summary>
		public const int RelFlagsSeAfter = unchecked((int)(0x0040));

		/// <summary>
		/// The relationship between the start point of the first
		/// interval and the end point of the second interval
		/// is unknown (used for fuzzy intervals)
		/// </summary>
		public const int RelFlagsSeUnknown = unchecked((int)(0x0070));

		/// <summary>
		/// The end point of the first interval is the same
		/// as the start point of the second interval
		/// (the first interval is before the second)
		/// <pre>
		/// ?---- interval 1 ---|
		/// |---- interval 2 ----?
		/// </pre>
		/// </summary>
		public const int RelFlagsEsSame = unchecked((int)(0x0100));

		/// <summary>
		/// The end point of the first interval is before
		/// the start point of the second interval
		/// (the first interval is before the second)
		/// <pre>
		/// ?-- interval 1 ---|
		/// |---- interval 2 ---?
		/// </pre>
		/// </summary>
		public const int RelFlagsEsBefore = unchecked((int)(0x0200));

		/// <summary>
		/// The end point of the first interval is after
		/// the start point of the second interval
		/// (the two intervals overlap)
		/// <pre>
		/// ?---- interval 1 ---|
		/// |---- interval 2 ----?
		/// </pre>
		/// </summary>
		public const int RelFlagsEsAfter = unchecked((int)(0x0400));

		/// <summary>
		/// The relationship between the end point of the first
		/// interval and the start point of the second interval
		/// is unknown (used for fuzzy intervals)
		/// </summary>
		public const int RelFlagsEsUnknown = unchecked((int)(0x0700));

		/// <summary>
		/// Both intervals have the same end point
		/// <pre>
		/// ?---- interval 1 ----|
		/// ?---- interval 2 ----|
		/// </pre>
		/// </summary>
		public const int RelFlagsEeSame = unchecked((int)(0x1000));

		/// <summary>
		/// The first interval ends before the second ends
		/// <pre>
		/// ?---- interval 1 ----|
		/// ?---- interval 2 ----|
		/// or
		/// ?-- interval 1 --|
		/// ?---- interval 2 ----|
		/// </pre>
		/// </summary>
		public const int RelFlagsEeBefore = unchecked((int)(0x2000));

		/// <summary>
		/// The first interval ends after the second ends
		/// <pre>
		/// ?---- interval 1 ----|
		/// ?---- interval 2 ----|
		/// or
		/// ?---- interval 1 ----|
		/// ?-- interval 2 --|
		/// </pre>
		/// </summary>
		public const int RelFlagsEeAfter = unchecked((int)(0x4000));

		/// <summary>
		/// The relationship between the end points of the
		/// two intervals is unknown (used for fuzzy intervals)
		/// </summary>
		public const int RelFlagsEeUnknown = unchecked((int)(0x7000));

		/// <summary>The intervals are the same (have the same start and end points).</summary>
		/// <remarks>
		/// The intervals are the same (have the same start and end points).
		/// When this flag is set, OVERLAP, INSIDE, and CONTAIN should also be set.
		/// <pre>
		/// |---- interval 1 ----|
		/// |---- interval 2 ----|
		/// </pre>
		/// </remarks>
		public const int RelFlagsIntervalSame = unchecked((int)(0x00010000));

		/// <summary>
		/// The first interval is entirely before the second interval
		/// (the end of the first interval happens before the start of the second)
		/// <pre>
		/// ?---- interval 1 ----|
		/// |---- interval 2 ----?
		/// </pre>
		/// </summary>
		public const int RelFlagsIntervalBefore = unchecked((int)(0x00020000));

		/// <summary>
		/// The first interval is entirely after the second interval
		/// (the start of the first interval happens after the end of the second)
		/// <pre>
		/// |---- interval 1 ----?
		/// ?---- interval 2 ----|
		/// </pre>
		/// </summary>
		public const int RelFlagsIntervalAfter = unchecked((int)(0x00040000));

		/// <summary>The first interval overlaps with the second interval.</summary>
		public const int RelFlagsIntervalOverlap = unchecked((int)(0x00100000));

		/// <summary>The first interval is inside the second interval.</summary>
		/// <remarks>
		/// The first interval is inside the second interval.
		/// When this flag is set, OVERLAP should also be set.
		/// <pre>
		/// |---- interval 1 ----|
		/// |---- interval 2 -----------|
		/// </pre>
		/// </remarks>
		public const int RelFlagsIntervalInside = unchecked((int)(0x00200000));

		/// <summary>The first interval contains the second interval.</summary>
		/// <remarks>
		/// The first interval contains the second interval.
		/// When this flag is set, OVERLAP should also be set.
		/// <pre>
		/// |---- interval 1 -----------|
		/// |---- interval 2 ----|
		/// </pre>
		/// </remarks>
		public const int RelFlagsIntervalContain = unchecked((int)(0x00400000));

		/// <summary>
		/// It is uncertain what the relationship between the
		/// two intervals are...
		/// </summary>
		public const int RelFlagsIntervalUnknown = unchecked((int)(0x00770000));

		public const int RelFlagsIntervalAlmostSame = unchecked((int)(0x01000000));

		public const int RelFlagsIntervalAlmostBefore = unchecked((int)(0x01000000));

		public const int RelFlagsIntervalAlmostAfter = unchecked((int)(0x01000000));

		public const int RelFlagsIntervalFuzzy = unchecked((int)(0x80000000));

		protected internal Interval(E a, E b, int flags)
			: base(a, b)
		{
			// Flags indicating how the endpoints of two intervals
			// are related
			// Flags indicating how two intervals are related
			// SS,EE  SAME
			// Can be set with OVERLAP, INSIDE, CONTAIN
			// ES BEFORE => SS, SE, EE BEFORE
			// SE AFTER => SS, ES, EE AFTER
			// SS SAME or AFTER, SE SAME or BEFORE
			// SS SAME or BEFORE, ES SAME or AFTER
			// SS SAME or AFTER, EE SAME or BEFORE
			// SS SAME or BEFORE, EE SAME or AFTER
			//  public final static int REL_FLAGS_INTERVAL_ALMOST_OVERLAP = 0x10000000;
			//  public final static int REL_FLAGS_INTERVAL_ALMOST_INSIDE = 0x20000000;
			//  public final static int REL_FLAGS_INTERVAL_ALMOST_CONTAIN = 0x40000000;
			this.flags = flags;
			int comp = a.CompareTo(b);
			if (comp > 0)
			{
				throw new ArgumentException("Invalid interval: " + a + "," + b);
			}
		}

		/// <summary>Create an interval with the specified endpoints in the specified order.</summary>
		/// <remarks>
		/// Create an interval with the specified endpoints in the specified order.
		/// Returns null if a does not come before b (invalid interval).
		/// </remarks>
		/// <param name="a">start endpoints</param>
		/// <param name="b">end endpoint</param>
		/// <?/>
		/// <returns>Interval with endpoints in specified order, null if a does not come before b</returns>
		public static Edu.Stanford.Nlp.Util.Interval<E> ToInterval<E>(E a, E b)
			where E : IComparable<E>
		{
			return ToInterval(a, b, 0);
		}

		/// <summary>
		/// Create an interval with the specified endpoints in the specified order,
		/// using the specified flags.
		/// </summary>
		/// <remarks>
		/// Create an interval with the specified endpoints in the specified order,
		/// using the specified flags.  Returns null if a does not come before b
		/// (invalid interval).
		/// </remarks>
		/// <param name="a">start endpoints</param>
		/// <param name="b">end endpoint</param>
		/// <param name="flags">flags characterizing the interval</param>
		/// <?/>
		/// <returns>Interval with endpoints in specified order, null if a does not come before b</returns>
		public static Edu.Stanford.Nlp.Util.Interval<E> ToInterval<E>(E a, E b, int flags)
			where E : IComparable<E>
		{
			int comp = a.CompareTo(b);
			if (comp <= 0)
			{
				return new Edu.Stanford.Nlp.Util.Interval<E>(a, b, flags);
			}
			else
			{
				return null;
			}
		}

		/// <summary>Create an interval with the specified endpoints, reordering them as needed</summary>
		/// <param name="a">one of the endpoints</param>
		/// <param name="b">the other endpoint</param>
		/// <?/>
		/// <returns>Interval with endpoints re-ordered as needed</returns>
		public static Edu.Stanford.Nlp.Util.Interval<E> ToValidInterval<E>(E a, E b)
			where E : IComparable<E>
		{
			return ToValidInterval(a, b, 0);
		}

		/// <summary>
		/// Create an interval with the specified endpoints, reordering them as needed,
		/// using the specified flags
		/// </summary>
		/// <param name="a">one of the endpoints</param>
		/// <param name="b">the other endpoint</param>
		/// <param name="flags">flags characterizing the interval</param>
		/// <?/>
		/// <returns>Interval with endpoints re-ordered as needed</returns>
		public static Edu.Stanford.Nlp.Util.Interval<E> ToValidInterval<E>(E a, E b, int flags)
			where E : IComparable<E>
		{
			int comp = a.CompareTo(b);
			if (comp <= 0)
			{
				return new Edu.Stanford.Nlp.Util.Interval<E>(a, b, flags);
			}
			else
			{
				return new Edu.Stanford.Nlp.Util.Interval<E>(b, a, flags);
			}
		}

		/// <summary>Returns this interval.</summary>
		/// <returns>this interval</returns>
		public virtual Edu.Stanford.Nlp.Util.Interval<E> GetInterval()
		{
			return this;
		}

		/// <summary>Returns the start point.</summary>
		/// <returns>the start point of this interval</returns>
		public virtual E GetBegin()
		{
			return first;
		}

		/// <summary>Returns the end point.</summary>
		/// <returns>the end point of this interval</returns>
		public virtual E GetEnd()
		{
			return second;
		}

		protected internal static E Max<E>(E a, E b)
			where E : IComparable<E>
		{
			int comp = a.CompareTo(b);
			return (comp > 0) ? a : b;
		}

		protected internal static E Min<E>(E a, E b)
			where E : IComparable<E>
		{
			int comp = a.CompareTo(b);
			return (comp < 0) ? a : b;
		}

		/// <summary>Checks whether the point p is contained inside this interval.</summary>
		/// <param name="p">point to check</param>
		/// <returns>True if the point p is contained withing the interval, false otherwise</returns>
		public virtual bool Contains(E p)
		{
			// Check that the start point is before p
			bool check1 = (IncludesBegin()) ? (first.CompareTo(p) <= 0) : (first.CompareTo(p) < 0);
			// Check that the end point is after p
			bool check2 = (IncludesEnd()) ? (second.CompareTo(p) >= 0) : (second.CompareTo(p) > 0);
			return (check1 && check2);
		}

		public virtual bool ContainsOpen(E p)
		{
			// Check that the start point is before p
			bool check1 = first.CompareTo(p) <= 0;
			// Check that the end point is after p
			bool check2 = second.CompareTo(p) >= 0;
			return (check1 && check2);
		}

		public virtual bool Contains(Edu.Stanford.Nlp.Util.Interval<E> other)
		{
			bool containsOtherBegin = (other.IncludesBegin()) ? Contains(other.GetBegin()) : ContainsOpen(other.GetBegin());
			bool containsOtherEnd = (other.IncludesEnd()) ? Contains(other.GetEnd()) : ContainsOpen(other.GetEnd());
			return (containsOtherBegin && containsOtherEnd);
		}

		/// <summary>Returns (smallest) interval that contains both this and the other interval</summary>
		/// <param name="other">- Other interval to include</param>
		/// <returns>Smallest interval that contains both this and the other interval</returns>
		public virtual Edu.Stanford.Nlp.Util.Interval Expand(Edu.Stanford.Nlp.Util.Interval<E> other)
		{
			if (other == null)
			{
				return this;
			}
			E a = Min(this.first, other.first);
			E b = Max(this.second, other.second);
			return ToInterval(a, b);
		}

		/// <summary>
		/// Returns interval that is the intersection of this and the other interval
		/// Returns null if intersect is null
		/// </summary>
		/// <param name="other">interval with which to intersect</param>
		/// <returns>interval that is the intersection of this and the other interval</returns>
		public virtual Edu.Stanford.Nlp.Util.Interval Intersect(Edu.Stanford.Nlp.Util.Interval<E> other)
		{
			if (other == null)
			{
				return null;
			}
			E a = Max(this.first, other.first);
			E b = Min(this.second, other.second);
			return ToInterval(a, b);
		}

		/// <summary>Check whether this interval overlaps with the other interval.</summary>
		/// <remarks>
		/// Check whether this interval overlaps with the other interval.
		/// (I.e. the intersect would not be null.)
		/// </remarks>
		/// <param name="other">interval to compare with</param>
		/// <returns>true if this interval overlaps the other interval</returns>
		public virtual bool Overlaps(Edu.Stanford.Nlp.Util.Interval<E> other)
		{
			if (other == null)
			{
				return false;
			}
			int comp12 = this.first.CompareTo(other.Second());
			int comp21 = this.second.CompareTo(other.First());
			if (comp12 > 0 || comp21 < 0)
			{
				return false;
			}
			else
			{
				if (comp12 == 0)
				{
					if (!this.IncludesBegin() || !other.IncludesEnd())
					{
						return false;
					}
				}
				if (comp21 == 0)
				{
					if (!this.IncludesEnd() || !other.IncludesBegin())
					{
						return false;
					}
				}
				return true;
			}
		}

		/// <summary>Returns whether the start endpoint is included in the interval</summary>
		/// <returns>true if the start endpoint is included in the interval</returns>
		public virtual bool IncludesBegin()
		{
			return ((flags & IntervalOpenBegin) == 0);
		}

		/// <summary>Returns whether the end endpoint is included in the interval</summary>
		/// <returns>true if the end endpoint is included in the interval</returns>
		public virtual bool IncludesEnd()
		{
			return ((flags & IntervalOpenEnd) == 0);
		}

		/*  // Returns true if end before (start of other)
		public boolean isEndBeforeBegin(Interval<E> other)
		{
		if (other == null) return false;
		int comp21 = this.second.compareTo(other.first());
		return (comp21 < 0);
		}
		
		// Returns true if end before or eq (start of other)
		public boolean isEndBeforeEqBegin(Interval<E> other)
		{
		if (other == null) return false;
		int comp21 = this.second.compareTo(other.first());
		return (comp21 <= 0);
		}
		
		// Returns true if end before or eq (start of other)
		public boolean isEndEqBegin(Interval<E> other)
		{
		if (other == null) return false;
		int comp21 = this.second.compareTo(other.first());
		return (comp21 == 0);
		}
		
		// Returns true if start after (end of other)
		public boolean isBeginAfterEnd(Interval<E> other)
		{
		if (other == null) return false;
		int comp12 = this.first.compareTo(other.second());
		return (comp12 > 0);
		}
		
		// Returns true if start eq(end of other)
		public boolean isBeginAfterEqEnd(Interval<E> other)
		{
		if (other == null) return false;
		int comp12 = this.first.compareTo(other.second());
		return (comp12 >= 0);
		}
		
		// Returns true if start eq(end of other)
		public boolean isBeginEqEnd(Interval<E> other)
		{
		if (other == null) return false;
		int comp12 = this.first.compareTo(other.second());
		return (comp12 >= 0);
		}
		
		// Returns true if start is the same
		public boolean isBeginSame(Interval<E> other)
		{
		if (other == null) return false;
		int comp11 = this.first.compareTo(other.first());
		return (comp11 == 0);
		}
		
		// Returns true if end is the same
		public boolean isEndSame(Interval<E> other)
		{
		if (other == null) return false;
		int comp22 = this.second.compareTo(other.second());
		return (comp22 == 0);
		} */
		/// <summary>
		/// Checks whether this interval is comparable with another interval
		/// comes before or after
		/// </summary>
		/// <param name="other">interval to compare with</param>
		public virtual bool IsIntervalComparable(Edu.Stanford.Nlp.Util.Interval<E> other)
		{
			int flags = GetRelationFlags(other);
			if (CheckMultipleBitSet(flags & RelFlagsIntervalUnknown))
			{
				return false;
			}
			return CheckFlagSet(flags, RelFlagsIntervalBefore) || CheckFlagSet(flags, RelFlagsIntervalAfter);
		}

		/// <summary>Returns order of another interval compared to this one</summary>
		/// <param name="other">Interval to compare with</param>
		/// <returns>
		/// -1 if this interval is before the other interval, 1 if this interval is after
		/// 0 otherwise (may indicate the two intervals are same or not comparable)
		/// </returns>
		public virtual int CompareIntervalOrder(Edu.Stanford.Nlp.Util.Interval<E> other)
		{
			int flags = GetRelationFlags(other);
			if (CheckFlagExclusiveSet(flags, RelFlagsIntervalBefore, RelFlagsIntervalUnknown))
			{
				return -1;
			}
			else
			{
				if (CheckFlagExclusiveSet(flags, RelFlagsIntervalAfter, RelFlagsIntervalUnknown))
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}
		}

		protected internal static int ToRelFlags(int comp, int shift)
		{
			int flags = 0;
			if (comp == 0)
			{
				flags = RelFlagsSame;
			}
			else
			{
				if (comp > 0)
				{
					flags = RelFlagsAfter;
				}
				else
				{
					flags = RelFlagsBefore;
				}
			}
			flags = flags << shift;
			return flags;
		}

		/// <summary>
		/// Return set of flags indicating possible relationships between
		/// this interval and another interval.
		/// </summary>
		/// <param name="other">Interval with which to compare with</param>
		/// <returns>flags indicating possible relationship between this interval and the other interval</returns>
		public virtual int GetRelationFlags(Edu.Stanford.Nlp.Util.Interval<E> other)
		{
			if (other == null)
			{
				return 0;
			}
			int flags = 0;
			int comp11 = this.first.CompareTo(other.First());
			// 3 choices
			flags |= ToRelFlags(comp11, RelFlagsSsShift);
			int comp22 = this.second.CompareTo(other.Second());
			// 3 choices
			flags |= ToRelFlags(comp22, RelFlagsEeShift);
			int comp12 = this.first.CompareTo(other.Second());
			// 3 choices
			flags |= ToRelFlags(comp12, RelFlagsSeShift);
			int comp21 = this.second.CompareTo(other.First());
			// 3 choices
			flags |= ToRelFlags(comp21, RelFlagsEsShift);
			flags = AddIntervalRelationFlags(flags, false);
			return flags;
		}

		protected internal static int AddIntervalRelationFlags(int flags, bool checkFuzzy)
		{
			int f11 = ExtractRelationSubflags(flags, RelFlagsSsShift);
			int f22 = ExtractRelationSubflags(flags, RelFlagsEeShift);
			int f12 = ExtractRelationSubflags(flags, RelFlagsSeShift);
			int f21 = ExtractRelationSubflags(flags, RelFlagsEsShift);
			if (checkFuzzy)
			{
				bool isFuzzy = CheckMultipleBitSet(f11) || CheckMultipleBitSet(f12) || CheckMultipleBitSet(f21) || CheckMultipleBitSet(f22);
				if (isFuzzy)
				{
					flags |= RelFlagsIntervalFuzzy;
				}
			}
			if (((f11 & RelFlagsSame) != 0) && ((f22 & RelFlagsSame) != 0))
			{
				// SS,EE SAME
				flags |= RelFlagsIntervalSame;
			}
			// Possible
			if (((f21 & RelFlagsBefore) != 0))
			{
				// ES BEFORE => SS, SE, EE BEFORE
				flags |= RelFlagsIntervalBefore;
			}
			// Possible
			if (((f12 & RelFlagsAfter) != 0))
			{
				// SE AFTER => SS, ES, EE AFTER
				flags |= RelFlagsIntervalAfter;
			}
			// Possible
			if (((f11 & (RelFlagsSame | RelFlagsAfter)) != 0) && ((f12 & (RelFlagsSame | RelFlagsBefore)) != 0))
			{
				// SS SAME or AFTER, SE SAME or BEFORE
				//     |-----|
				// |------|
				flags |= RelFlagsIntervalOverlap;
			}
			// Possible
			if (((f11 & (RelFlagsSame | RelFlagsBefore)) != 0) && ((f21 & (RelFlagsSame | RelFlagsAfter)) != 0))
			{
				// SS SAME or BEFORE, ES SAME or AFTER
				// |------|
				//     |-----|
				flags |= RelFlagsIntervalOverlap;
			}
			// Possible
			if (((f11 & (RelFlagsSame | RelFlagsAfter)) != 0) && ((f22 & (RelFlagsSame | RelFlagsBefore)) != 0))
			{
				// SS SAME or AFTER, EE SAME or BEFORE
				//     |------|
				// |---------------|
				flags |= RelFlagsIntervalInside;
			}
			// Possible
			if (((f11 & (RelFlagsSame | RelFlagsBefore)) != 0) && ((f22 & (RelFlagsSame | RelFlagsAfter)) != 0))
			{
				// SS SAME or BEFORE, EE SAME or AFTER
				flags |= RelFlagsIntervalContain;
			}
			// Possible
			// |---------------|
			//     |------|
			return flags;
		}

		public static int ExtractRelationSubflags(int flags, int shift)
		{
			return (flags >> shift) & unchecked((int)(0xf));
		}

		/// <summary>Utility function to check if multiple bits are set for flags</summary>
		/// <param name="flags">flags to check</param>
		/// <returns>true if multiple bits are set</returns>
		public static bool CheckMultipleBitSet(int flags)
		{
			bool set = false;
			while (flags != 0)
			{
				if ((flags & unchecked((int)(0x01))) != 0)
				{
					if (set)
					{
						return false;
					}
					else
					{
						set = true;
					}
				}
				flags = flags >> 1;
			}
			return false;
		}

		/// <summary>
		/// Utility function to check if a particular flag is set
		/// given a particular set of flags.
		/// </summary>
		/// <param name="flags">flags to check</param>
		/// <param name="flag">bit for flag of interest (is this flag set or not)</param>
		/// <returns>true if flag is set for flags</returns>
		public static bool CheckFlagSet(int flags, int flag)
		{
			return ((flags & flag) != 0);
		}

		/// <summary>
		/// Utility function to check if a particular flag is set exclusively
		/// given a particular set of flags and a mask.
		/// </summary>
		/// <param name="flags">flags to check</param>
		/// <param name="flag">bit for flag of interest (is this flag set or not)</param>
		/// <param name="mask">bitmask of bits to check</param>
		/// <returns>true if flag is exclusively set for flags & mask</returns>
		public static bool CheckFlagExclusiveSet(int flags, int flag, int mask)
		{
			int f = flags & flag;
			if (f != 0)
			{
				return (flags & mask & ~flag) == 0;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Returns the relationship of this interval to the other interval
		/// The most specific relationship from the following is returned.
		/// </summary>
		/// <remarks>
		/// Returns the relationship of this interval to the other interval
		/// The most specific relationship from the following is returned.
		/// NONE: the other interval is null
		/// EQUAL: this have same endpoints as other
		/// OVERLAP:  this and other overlaps
		/// BEFORE: this ends before other starts
		/// AFTER: this starts after other ends
		/// BEGIN_MEET_END: this begin is the same as the others end
		/// END_MEET_BEGIN: this end is the same as the others begin
		/// CONTAIN: this contains the other
		/// INSIDE: this is inside the other
		/// UNKNOWN: this is returned if for some reason it is not
		/// possible to determine the exact relationship
		/// of the two intervals (possible for fuzzy intervals)
		/// </remarks>
		/// <param name="other">The other interval with which to compare with</param>
		/// <returns>RelType indicating relationship between the two interval</returns>
		public virtual Interval.RelType GetRelation(Edu.Stanford.Nlp.Util.Interval<E> other)
		{
			// TODO: Handle open/closed intervals?
			if (other == null)
			{
				return Interval.RelType.None;
			}
			int comp11 = this.first.CompareTo(other.First());
			// 3 choices
			int comp22 = this.second.CompareTo(other.Second());
			// 3 choices
			if (comp11 == 0)
			{
				if (comp22 == 0)
				{
					// |---|  this
					// |---|   other
					return Interval.RelType.Equal;
				}
				if (comp22 < 0)
				{
					// SAME START - this finishes before other
					// |---|  this
					// |------|   other
					return Interval.RelType.Inside;
				}
				else
				{
					// SAME START - this finishes after other
					// |------|  this
					// |---|   other
					return Interval.RelType.Contain;
				}
			}
			else
			{
				if (comp22 == 0)
				{
					if (comp11 < 0)
					{
						// SAME FINISH - this start before other
						// |------|  this
						//    |---|   other
						return Interval.RelType.Contain;
					}
					else
					{
						/*if (comp11 > 0) */
						// SAME FINISH - this starts after other
						//    |---|  this
						// |------|   other
						return Interval.RelType.Inside;
					}
				}
				else
				{
					if (comp11 > 0 && comp22 < 0)
					{
						//    |---|  this
						// |---------|   other
						return Interval.RelType.Inside;
					}
					else
					{
						if (comp11 < 0 && comp22 > 0)
						{
							// |---------|  this
							//    |---|   other
							return Interval.RelType.Contain;
						}
						else
						{
							int comp12 = this.first.CompareTo(other.Second());
							int comp21 = this.second.CompareTo(other.First());
							if (comp12 > 0)
							{
								//           |---|  this
								// |---|   other
								return Interval.RelType.After;
							}
							else
							{
								if (comp21 < 0)
								{
									// |---|  this
									//        |---|   other
									return Interval.RelType.Before;
								}
								else
								{
									if (comp12 == 0)
									{
										//     |---|  this
										// |---|   other
										return Interval.RelType.BeginMeetEnd;
									}
									else
									{
										if (comp21 == 0)
										{
											// |---|  this
											//     |---|   other
											return Interval.RelType.EndMeetBegin;
										}
										else
										{
											return Interval.RelType.Overlap;
										}
									}
								}
							}
						}
					}
				}
			}
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
			Edu.Stanford.Nlp.Util.Interval interval = (Edu.Stanford.Nlp.Util.Interval)o;
			if (flags != interval.flags)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = base.GetHashCode();
			result = 31 * result + flags;
			return result;
		}

		public static double GetMidPoint(Edu.Stanford.Nlp.Util.Interval<int> interval)
		{
			return (interval.GetBegin() + interval.GetEnd()) / 2.0;
		}

		public static double GetRadius(Edu.Stanford.Nlp.Util.Interval<int> interval)
		{
			return (interval.GetEnd() - interval.GetBegin()) / 2.0;
		}

		public static IComparator<T> LengthEndpointsComparator<T>()
			where T : IHasInterval<int>
		{
			return ErasureUtils.UncheckedCast(HasIntervalConstants.LengthEndpointsComparator);
		}

		public static IToDoubleFunction<T> LengthScorer<T>()
			where T : IHasInterval<int>
		{
			return ErasureUtils.UncheckedCast(LengthScorer);
		}

		public static readonly IToDoubleFunction<IHasInterval<int>> LengthScorer = null;

		private const long serialVersionUID = 1;
	}
}
