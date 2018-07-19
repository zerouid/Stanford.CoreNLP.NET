using System;
using System.Collections;
using System.Collections.Generic;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	public class Comparators
	{
		private Comparators()
		{
		}

		// Originally from edu.stanford.nlp.natlog.util
		// class of static methods
		/// <summary>
		/// Returns a new
		/// <c>Comparator</c>
		/// which is the result of chaining the
		/// given
		/// <c>Comparator</c>
		/// s.  If the first
		/// <c>Comparator</c>
		/// considers two objects unequal, its result is returned; otherwise, the
		/// result of the second
		/// <c>Comparator</c>
		/// is returned.  Facilitates
		/// sorting on primary and secondary keys.
		/// </summary>
		public static IComparator<T> Chain<T, _T1, _T2>(IComparator<_T1> c1, IComparator<_T2> c2)
		{
			return null;
		}

		/// <summary>
		/// Returns a new
		/// <c>Comparator</c>
		/// which is the result of chaining the
		/// given
		/// <c>Comparator</c>
		/// s.  Facilitates sorting on multiple keys.
		/// </summary>
		public static IComparator<T> Chain<T>(IList<IComparator<T>> c)
		{
			return null;
		}

		[SafeVarargs]
		public static IComparator<T> Chain<T>(params IComparator<T>[] c)
		{
			return Chain(Arrays.AsList(c));
		}

		/// <summary>
		/// Returns a new
		/// <c>Comparator</c>
		/// which is the reverse of the
		/// given
		/// <c>Comparator</c>
		/// .
		/// </summary>
		public static IComparator<T> Reverse<T, _T1>(IComparator<_T1> c)
		{
			return null;
		}

		public static IComparator<T> NullSafeNaturalComparator<T>()
			where T : IComparable<T>
		{
			return null;
		}

		/// <summary>
		/// Returns a consistent ordering over two elements even if one of them is null
		/// (as long as compareTo() is stable, of course).
		/// </summary>
		/// <remarks>
		/// Returns a consistent ordering over two elements even if one of them is null
		/// (as long as compareTo() is stable, of course).
		/// There's a "trickier" solution with xor at http://stackoverflow.com/a/481836
		/// but the straightforward answer seems better.
		/// </remarks>
		public static int NullSafeCompare<T>(T one, T two)
			where T : IComparable<T>
		{
			if (one == null)
			{
				if (two == null)
				{
					return 0;
				}
				return -1;
			}
			else
			{
				if (two == null)
				{
					return 1;
				}
				return one.CompareTo(two);
			}
		}

		private static int CompareLists<X, _T1, _T2>(IList<_T1> list1, IList<_T2> list2)
			where X : IComparable<X>
			where _T1 : X
			where _T2 : X
		{
			// if (list1 == null && list2 == null) return 0;  // seems better to regard all nulls as out of domain or none, not some
			if (list1 == null || list2 == null)
			{
				throw new ArgumentException();
			}
			int size1 = list1.Count;
			int size2 = list2.Count;
			int size = Math.Min(size1, size2);
			for (int i = 0; i < size; i++)
			{
				int c = list1[i].CompareTo(list2[i]);
				if (c != 0)
				{
					return c;
				}
			}
			return int.Compare(size1, size2);
		}

		public static IComparator<IList<C>> GetListComparator<C>()
			where C : IComparable
		{
			return null;
		}

		/// <summary>
		/// A
		/// <c>Comparator</c>
		/// that compares objects by comparing their
		/// <c>String</c>
		/// representations, as determined by invoking
		/// <c>toString()</c>
		/// on the objects in question.
		/// </summary>
		public static IComparer GetStringRepresentationComparator()
		{
			return IComparer.Comparing(null);
		}

		public static IComparator<bool[]> GetBooleanArrayComparator()
		{
			return null;
		}

		public static IComparator<C[]> GetArrayComparator<C>()
			where C : IComparable
		{
			return null;
		}
	}
}
