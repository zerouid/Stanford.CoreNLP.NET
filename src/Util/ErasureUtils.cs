using System;
using System.Collections;
using System.Collections.Generic;
using Java.Lang.Reflect;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Class to gather unsafe operations into one place.</summary>
	/// <author>dlwh</author>
	public class ErasureUtils
	{
		private ErasureUtils()
		{
		}

		/// <summary>Casts an Object to a T</summary>
		/// <?/>
		public static T UncheckedCast<T>(object o)
		{
			return (T)o;
		}

		/// <summary>Does nothing, occasionally used to make Java happy that a value is used</summary>
		public static void Noop(object o)
		{
		}

		/// <summary>Makes an array based on klass, but casts it to be of type T[].</summary>
		/// <remarks>
		/// Makes an array based on klass, but casts it to be of type T[]. This is a very
		/// unsafe operation and should be used carefully. Namely, you should ensure that
		/// klass is a subtype of T, or that klass is a supertype of T *and* that the array
		/// will not escape the generic constant *and* that klass is the same as the erasure
		/// of T.
		/// </remarks>
		/// <?/>
		public static T[] MkTArray<T>(Type klass, int size)
		{
			return (T[])(System.Array.CreateInstance(klass, size));
		}

		public static T[][] MkT2DArray<T>(Type klass, int[] dim)
		{
			if (dim.Length != 2)
			{
				throw new Exception("dim should be an array of size 2.");
			}
			return (T[][])(System.Array.CreateInstance(klass, dim));
		}

		public static IList<T> SortedIfPossible<T>(ICollection<T> collection)
		{
			IList<T> result = new List<T>(collection);
			try
			{
				(IList)result.Sort();
			}
			catch (InvalidCastException)
			{
			}
			catch (ArgumentNullException)
			{
			}
			// unable to sort, just return the copy
			// this happens if there are null elements in the collection; just return the copy
			return result;
		}
	}
}
