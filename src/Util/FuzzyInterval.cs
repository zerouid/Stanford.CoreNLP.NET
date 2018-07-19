using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A FuzzyInterval is an extension of Interval where not all endpoints are always
	/// specified or comparable.
	/// </summary>
	/// <remarks>
	/// A FuzzyInterval is an extension of Interval where not all endpoints are always
	/// specified or comparable.  It is assumed that most endpoints will be comparable
	/// so that there is some meaningful relationship between most FuzzyIntervals
	/// </remarks>
	/// <?/>
	[System.Serializable]
	public class FuzzyInterval<E> : Interval<E>
		where E : FuzzyInterval.IFuzzyComparable<E>
	{
		/// <summary>Interface with a looser ordering than Comparable.</summary>
		/// <remarks>
		/// Interface with a looser ordering than Comparable.
		/// If two objects are clearly comparable, compareTo will return -1,1,0 as before
		/// If two objects are not quite comparable, compareTo will return it's best guess
		/// </remarks>
		/// <?/>
		public interface IFuzzyComparable<T> : IComparable<T>
		{
			/// <summary>Returns whether this object is comparable with another object</summary>
			/// <param name="other"/>
			/// <returns>Returns true if two objects are comparable, false otherwise</returns>
			bool IsComparable(T other);
		}

		private FuzzyInterval(E a, E b, int flags)
			: base(a, b, flags)
		{
		}

		public static FuzzyInterval<E> ToInterval<E>(E a, E b)
			where E : FuzzyInterval.IFuzzyComparable<E>
		{
			return ToInterval(a, b, 0);
		}

		public static FuzzyInterval<E> ToInterval<E>(E a, E b, int flags)
			where E : FuzzyInterval.IFuzzyComparable<E>
		{
			int comp = a.CompareTo(b);
			if (comp <= 0)
			{
				return new FuzzyInterval<E>(a, b, flags);
			}
			else
			{
				return null;
			}
		}

		public static FuzzyInterval<E> ToValidInterval<E>(E a, E b)
			where E : FuzzyInterval.IFuzzyComparable<E>
		{
			return ToValidInterval(a, b, 0);
		}

		public static FuzzyInterval<E> ToValidInterval<E>(E a, E b, int flags)
			where E : FuzzyInterval.IFuzzyComparable<E>
		{
			int comp = a.CompareTo(b);
			if (comp <= 0)
			{
				return new FuzzyInterval<E>(a, b, flags);
			}
			else
			{
				return new FuzzyInterval<E>(b, a, flags);
			}
		}

		public override int GetRelationFlags(Interval<E> other)
		{
			if (other == null)
			{
				return 0;
			}
			int flags = 0;
			bool hasUnknown = false;
			if (this.first.IsComparable(other.First()))
			{
				int comp11 = this.first.CompareTo(other.First());
				// 3 choices
				flags |= ToRelFlags(comp11, RelFlagsSsShift);
			}
			else
			{
				flags |= RelFlagsSsUnknown;
				hasUnknown = true;
			}
			if (this.second.IsComparable(other.Second()))
			{
				int comp22 = this.second.CompareTo(other.Second());
				// 3 choices
				flags |= ToRelFlags(comp22, RelFlagsEeShift);
			}
			else
			{
				flags |= RelFlagsEeUnknown;
				hasUnknown = true;
			}
			if (this.first.IsComparable(other.Second()))
			{
				int comp12 = this.first.CompareTo(other.Second());
				// 3 choices
				flags |= ToRelFlags(comp12, RelFlagsSeShift);
			}
			else
			{
				flags |= RelFlagsSeUnknown;
				hasUnknown = true;
			}
			if (this.second.IsComparable(other.First()))
			{
				int comp21 = this.second.CompareTo(other.First());
				// 3 choices
				flags |= ToRelFlags(comp21, RelFlagsEsShift);
			}
			else
			{
				flags |= RelFlagsEsUnknown;
				hasUnknown = true;
			}
			if (hasUnknown)
			{
				flags = RestrictFlags(flags);
			}
			flags = AddIntervalRelationFlags(flags, hasUnknown);
			return flags;
		}

		private int RestrictFlags(int flags)
		{
			// Eliminate inconsistent choices in flags
			int f11 = ExtractRelationSubflags(flags, RelFlagsSsShift);
			int f22 = ExtractRelationSubflags(flags, RelFlagsEeShift);
			int f12 = ExtractRelationSubflags(flags, RelFlagsSeShift);
			int f21 = ExtractRelationSubflags(flags, RelFlagsEsShift);
			if (f12 == RelFlagsAfter)
			{
				f11 = f11 & RelFlagsAfter;
				f21 = f21 & RelFlagsAfter;
				f22 = f22 & RelFlagsAfter;
			}
			else
			{
				if ((f12 & RelFlagsBefore) == 0)
				{
					f11 = f11 & (RelFlagsSame | RelFlagsAfter);
					f21 = f21 & (RelFlagsSame | RelFlagsAfter);
					f22 = f22 & (RelFlagsSame | RelFlagsAfter);
				}
			}
			if (f11 == RelFlagsAfter)
			{
				f21 = f21 & RelFlagsAfter;
			}
			else
			{
				if (f11 == RelFlagsBefore)
				{
					f12 = f12 & RelFlagsBefore;
				}
				else
				{
					if ((f11 & RelFlagsBefore) == 0)
					{
						f21 = f21 & (RelFlagsSame | RelFlagsAfter);
					}
					else
					{
						if ((f11 & RelFlagsAfter) == 0)
						{
							f12 = f12 & (RelFlagsSame | RelFlagsBefore);
						}
					}
				}
			}
			if (f21 == RelFlagsBefore)
			{
				f11 = f11 & RelFlagsBefore;
				f12 = f12 & RelFlagsBefore;
				f22 = f22 & RelFlagsBefore;
			}
			else
			{
				if ((f12 & RelFlagsAfter) == 0)
				{
					f11 = f11 & (RelFlagsSame | RelFlagsBefore);
					f12 = f12 & (RelFlagsSame | RelFlagsBefore);
					f22 = f22 & (RelFlagsSame | RelFlagsBefore);
				}
			}
			if (f22 == RelFlagsAfter)
			{
				f21 = f21 & RelFlagsAfter;
			}
			else
			{
				if (f22 == RelFlagsBefore)
				{
					f12 = f12 & RelFlagsBefore;
				}
				else
				{
					if ((f22 & RelFlagsBefore) == 0)
					{
						f21 = f21 & (RelFlagsSame | RelFlagsAfter);
					}
					else
					{
						if ((f22 & RelFlagsAfter) == 0)
						{
							f12 = f12 & (RelFlagsSame | RelFlagsBefore);
						}
					}
				}
			}
			return ((f11 << RelFlagsSsShift) & (f12 << RelFlagsSeShift) & (f21 << RelFlagsEsShift) & (f22 << RelFlagsEeShift));
		}

		public override Interval.RelType GetRelation(Interval<E> other)
		{
			if (other == null)
			{
				return Interval.RelType.None;
			}
			int flags = GetRelationFlags(other);
			if ((flags & RelFlagsIntervalFuzzy) != 0)
			{
				return Interval.RelType.Unknown;
			}
			else
			{
				if ((flags & RelFlagsIntervalUnknown) != 0)
				{
					return Interval.RelType.Before;
				}
				else
				{
					if ((flags & RelFlagsIntervalBefore) != 0)
					{
						return Interval.RelType.After;
					}
					else
					{
						if ((flags & RelFlagsIntervalAfter) != 0)
						{
							return Interval.RelType.Equal;
						}
						else
						{
							if ((flags & RelFlagsIntervalInside) != 0)
							{
								return Interval.RelType.Inside;
							}
							else
							{
								if ((flags & RelFlagsIntervalContain) != 0)
								{
									return Interval.RelType.Contain;
								}
								else
								{
									if ((flags & RelFlagsIntervalOverlap) != 0)
									{
										return Interval.RelType.Overlap;
									}
									else
									{
										return Interval.RelType.Unknown;
									}
								}
							}
						}
					}
				}
			}
		}

		private const long serialVersionUID = 1;
	}
}
