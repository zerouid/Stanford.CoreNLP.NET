// Stanford JavaNLP support classes
// Copyright (c) 2004-2008 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    java-nlp-support@lists.stanford.edu
//    http://nlp.stanford.edu/software/
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Lang.Reflect;
using Java.Text;
using Java.Util;
using Java.Util.Function;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// Static methods for operating on a
	/// <see cref="ICounter{E}"/>
	/// .
	/// All methods that change their arguments change the <i>first</i> argument
	/// (only), and have "InPlace" in their name. This class also provides access to
	/// Comparators that can be used to sort the keys or entries of this Counter by
	/// the counts, in either ascending or descending order.
	/// </summary>
	/// <author>Galen Andrew (galand@cs.stanford.edu)</author>
	/// <author>Jeff Michels (jmichels@stanford.edu)</author>
	/// <author>dramage</author>
	/// <author>daniel cer (http://dmcer.net)</author>
	/// <author>Christopher Manning</author>
	/// <author>stefank (Optimized dot product)</author>
	public class Counters
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Stats.Counters));

		private static readonly double LogE2 = Math.Log(2.0);

		private Counters()
		{
		}

		// only static methods
		//
		// Log arithmetic operations
		//
		/// <summary>Returns ArrayMath.logSum of the values in this counter.</summary>
		/// <param name="c">Argument counter (which is not modified)</param>
		/// <returns>ArrayMath.logSum of the values in this counter.</returns>
		public static double LogSum<E>(ICounter<E> c)
		{
			return ArrayMath.LogSum(ArrayMath.Unbox(c.Values()));
		}

		/// <summary>Transform log space values into a probability distribution in place.</summary>
		/// <remarks>
		/// Transform log space values into a probability distribution in place. On the
		/// assumption that the values in the Counter are in log space, this method
		/// calculates their sum, and then subtracts the log of their sum from each
		/// element. That is, if a counter has keys c1, c2, c3 with values v1, v2, v3,
		/// the value of c1 becomes v1 - log(e^v1 + e^v2 + e^v3). After this, e^v1 +
		/// e^v2 + e^v3 = 1.0, so Counters.logSum(c) = 0.0 (approximately).
		/// </remarks>
		/// <param name="c">The Counter to log normalize in place</param>
		public static void LogNormalizeInPlace<E>(ICounter<E> c)
		{
			double logsum = LogSum(c);
			// for (E key : c.keySet()) {
			// c.incrementCount(key, -logsum);
			// }
			// This should be faster
			foreach (KeyValuePair<E, double> e in c.EntrySet())
			{
				e.SetValue(e.Value - logsum);
			}
		}

		//
		// Query operations
		//
		/// <summary>Returns the value of the maximum entry in this counter.</summary>
		/// <remarks>
		/// Returns the value of the maximum entry in this counter. This is also the
		/// L_infinity norm. An empty counter is given a max value of
		/// Double.NEGATIVE_INFINITY.
		/// </remarks>
		/// <param name="c">The Counter to find the max of</param>
		/// <returns>The maximum value of the Counter</returns>
		public static double Max<E>(ICounter<E> c)
		{
			return Max(c, double.NegativeInfinity);
		}

		// note[gabor]: Should the default actually be 0 rather than negative_infinity?
		/// <summary>Returns the value of the maximum entry in this counter.</summary>
		/// <remarks>
		/// Returns the value of the maximum entry in this counter. This is also the
		/// L_infinity norm. An empty counter is given a max value of
		/// Double.NEGATIVE_INFINITY.
		/// </remarks>
		/// <param name="c">The Counter to find the max of</param>
		/// <param name="valueIfEmpty">The value to return if this counter is empty (i.e., the maximum is not well defined.</param>
		/// <returns>The maximum value of the Counter</returns>
		public static double Max<E>(ICounter<E> c, double valueIfEmpty)
		{
			if (c.Size() == 0)
			{
				return valueIfEmpty;
			}
			else
			{
				double max = double.NegativeInfinity;
				foreach (double v in c.Values())
				{
					max = System.Math.Max(max, v);
				}
				return max;
			}
		}

		/// <summary>
		/// Takes in a Collection of something and makes a counter, incrementing once
		/// for each object in the collection.
		/// </summary>
		/// <param name="c">The Collection to turn into a counter</param>
		/// <returns>The counter made out of the collection</returns>
		public static ICounter<E> AsCounter<E>(ICollection<E> c)
		{
			ICounter<E> count = new ClassicCounter<E>();
			foreach (E elem in c)
			{
				count.IncrementCount(elem);
			}
			return count;
		}

		/// <summary>Returns the value of the smallest entry in this counter.</summary>
		/// <param name="c">The Counter (not modified)</param>
		/// <returns>The minimum value in the Counter</returns>
		public static double Min<E>(ICounter<E> c)
		{
			double min = double.PositiveInfinity;
			foreach (double v in c.Values())
			{
				min = System.Math.Min(min, v);
			}
			return min;
		}

		/// <summary>Finds and returns the key in the Counter with the largest count.</summary>
		/// <remarks>
		/// Finds and returns the key in the Counter with the largest count. Returning
		/// null if count is empty.
		/// </remarks>
		/// <param name="c">The Counter</param>
		/// <returns>The key in the Counter with the largest count.</returns>
		public static E Argmax<E>(ICounter<E> c)
		{
			return Argmax(c, null, null);
		}

		/// <summary>Finds and returns the key in this Counter with the smallest count.</summary>
		/// <param name="c">The Counter</param>
		/// <returns>The key in the Counter with the smallest count.</returns>
		public static E Argmin<E>(ICounter<E> c)
		{
			double min = double.PositiveInfinity;
			E argmin = null;
			foreach (E key in c.KeySet())
			{
				double count = c.GetCount(key);
				if (argmin == null || count < min)
				{
					// || (count == min && tieBreaker.compare(key, argmin) < 0)
					min = count;
					argmin = key;
				}
			}
			return argmin;
		}

		/// <summary>Finds and returns the key in the Counter with the largest count.</summary>
		/// <remarks>
		/// Finds and returns the key in the Counter with the largest count. Returning
		/// null if count is empty.
		/// </remarks>
		/// <param name="c">The Counter</param>
		/// <param name="tieBreaker">the tie breaker for when elements have the same value.</param>
		/// <returns>The key in the Counter with the largest count.</returns>
		public static E Argmax<E>(ICounter<E> c, IComparator<E> tieBreaker)
		{
			return Argmax(c, tieBreaker, (E)null);
		}

		/// <summary>Finds and returns the key in the Counter with the largest count.</summary>
		/// <remarks>
		/// Finds and returns the key in the Counter with the largest count. Returning
		/// null if count is empty.
		/// </remarks>
		/// <param name="c">The Counter</param>
		/// <param name="tieBreaker">the tie breaker for when elements have the same value.</param>
		/// <param name="defaultIfEmpty">The value to return if the counter is empty.</param>
		/// <returns>The key in the Counter with the largest count.</returns>
		public static E Argmax<E>(ICounter<E> c, IComparator<E> tieBreaker, E defaultIfEmpty)
		{
			if (Thread.Interrupted())
			{
				// A good place to check for interrupts -- called from many annotators
				throw new RuntimeInterruptedException();
			}
			if (c.Size() == 0)
			{
				return defaultIfEmpty;
			}
			double max = double.NegativeInfinity;
			E argmax = null;
			foreach (E key in c.KeySet())
			{
				double count = c.GetCount(key);
				if (argmax == null || count > max || (count == max && tieBreaker.Compare(key, argmax) < 0))
				{
					max = count;
					argmax = key;
				}
			}
			return argmax;
		}

		/// <summary>Finds and returns the key in this Counter with the smallest count.</summary>
		/// <param name="c">The Counter</param>
		/// <returns>The key in the Counter with the smallest count.</returns>
		public static E Argmin<E>(ICounter<E> c, IComparator<E> tieBreaker)
		{
			double min = double.PositiveInfinity;
			E argmin = null;
			foreach (E key in c.KeySet())
			{
				double count = c.GetCount(key);
				if (argmin == null || count < min || (count == min && tieBreaker.Compare(key, argmin) < 0))
				{
					min = count;
					argmin = key;
				}
			}
			return argmin;
		}

		/// <summary>Returns the mean of all the counts (totalCount/size).</summary>
		/// <param name="c">The Counter to find the mean of.</param>
		/// <returns>The mean of all the counts (totalCount/size).</returns>
		public static double Mean<E>(ICounter<E> c)
		{
			return c.TotalCount() / c.Size();
		}

		public static double StandardDeviation<E>(ICounter<E> c)
		{
			double std = 0;
			double mean = c.TotalCount() / c.Size();
			foreach (KeyValuePair<E, double> en in c.EntrySet())
			{
				std += (en.Value - mean) * (en.Value - mean);
			}
			return System.Math.Sqrt(std / c.Size());
		}

		//
		// In-place arithmetic
		//
		/// <summary>
		/// Sets each value of target to be target[k]+scale*arg[k] for all keys k in
		/// target.
		/// </summary>
		/// <param name="target">A Counter that is modified</param>
		/// <param name="arg">The Counter whose contents are added to target</param>
		/// <param name="scale">How the arg Counter is scaled before being added</param>
		public static void AddInPlace<E>(ICounter<E> target, ICounter<E> arg, double scale)
		{
			// TODO: Rewrite to use arg.entrySet()
			foreach (E key in arg.KeySet())
			{
				target.IncrementCount(key, scale * arg.GetCount(key));
			}
		}

		/// <summary>Sets each value of target to be target[k]+arg[k] for all keys k in arg.</summary>
		public static void AddInPlace<E>(ICounter<E> target, ICounter<E> arg)
		{
			foreach (KeyValuePair<E, double> entry in arg.EntrySet())
			{
				double count = entry.Value;
				if (count != 0)
				{
					target.IncrementCount(entry.Key, count);
				}
			}
		}

		/// <summary>
		/// Sets each value of double[] target to be
		/// target[idx.indexOf(k)]+a.getCount(k) for all keys k in arg
		/// </summary>
		public static void AddInPlace<E>(double[] target, ICounter<E> arg, IIndex<E> idx)
		{
			foreach (KeyValuePair<E, double> entry in arg.EntrySet())
			{
				target[idx.IndexOf(entry.Key)] += entry.Value;
			}
		}

		/// <summary>For all keys (u,v) in arg1 and arg2, sets return[u,v] to be summation of both.</summary>
		/// <?/>
		/// <?/>
		public static TwoDimensionalCounter<T1, T2> Add<T1, T2>(TwoDimensionalCounter<T1, T2> arg1, TwoDimensionalCounter<T1, T2> arg2)
		{
			TwoDimensionalCounter<T1, T2> add = new TwoDimensionalCounter<T1, T2>();
			Edu.Stanford.Nlp.Stats.Counters.AddInPlace(add, arg1);
			Edu.Stanford.Nlp.Stats.Counters.AddInPlace(add, arg2);
			return add;
		}

		/// <summary>
		/// For all keys (u,v) in arg, sets target[u,v] to be target[u,v] + scale
		/// arg[u,v].
		/// </summary>
		/// <?/>
		/// <?/>
		public static void AddInPlace<T1, T2>(TwoDimensionalCounter<T1, T2> target, TwoDimensionalCounter<T1, T2> arg, double scale)
		{
			foreach (T1 outer in arg.FirstKeySet())
			{
				foreach (T2 inner in arg.SecondKeySet())
				{
					target.IncrementCount(outer, inner, scale * arg.GetCount(outer, inner));
				}
			}
		}

		/// <summary>For all keys (u,v) in arg, sets target[u,v] to be target[u,v] + arg[u,v].</summary>
		/// <?/>
		/// <?/>
		public static void AddInPlace<T1, T2>(TwoDimensionalCounter<T1, T2> target, TwoDimensionalCounter<T1, T2> arg)
		{
			foreach (T1 outer in arg.FirstKeySet())
			{
				foreach (T2 inner in arg.SecondKeySet())
				{
					target.IncrementCount(outer, inner, arg.GetCount(outer, inner));
				}
			}
		}

		/// <summary>
		/// Sets each value of target to be target[k]+
		/// value*(num-of-times-it-occurs-in-collection) if the key is present in the arg
		/// collection.
		/// </summary>
		public static void AddInPlace<E>(ICounter<E> target, ICollection<E> arg, double value)
		{
			foreach (E key in arg)
			{
				target.IncrementCount(key, value);
			}
		}

		/// <summary>For all keys (u,v) in target, sets target[u,v] to be target[u,v] + value</summary>
		/// <?/>
		/// <?/>
		public static void AddInPlace<T1, T2>(TwoDimensionalCounter<T1, T2> target, double value)
		{
			foreach (T1 outer in target.FirstKeySet())
			{
				AddInPlace(target.GetCounter(outer), value);
			}
		}

		/// <summary>
		/// Sets each value of target to be target[k]+
		/// num-of-times-it-occurs-in-collection if the key is present in the arg
		/// collection.
		/// </summary>
		public static void AddInPlace<E>(ICounter<E> target, ICollection<E> arg)
		{
			foreach (E key in arg)
			{
				target.IncrementCount(key, 1);
			}
		}

		/// <summary>Increments all keys in a Counter by a specific value.</summary>
		public static void AddInPlace<E>(ICounter<E> target, double value)
		{
			foreach (E key in target.KeySet())
			{
				target.IncrementCount(key, value);
			}
		}

		/// <summary>Sets each value of target to be target[k]-arg[k] for all keys k in target.</summary>
		public static void SubtractInPlace<E>(ICounter<E> target, ICounter<E> arg)
		{
			foreach (E key in arg.KeySet())
			{
				target.DecrementCount(key, arg.GetCount(key));
			}
		}

		/// <summary>
		/// Sets each value of double[] target to be
		/// target[idx.indexOf(k)]-a.getCount(k) for all keys k in arg
		/// </summary>
		public static void SubtractInPlace<E>(double[] target, ICounter<E> arg, IIndex<E> idx)
		{
			foreach (KeyValuePair<E, double> entry in arg.EntrySet())
			{
				target[idx.IndexOf(entry.Key)] -= entry.Value;
			}
		}

		/// <summary>
		/// Divides every non-zero count in target by the corresponding value in the
		/// denominator Counter.
		/// </summary>
		/// <remarks>
		/// Divides every non-zero count in target by the corresponding value in the
		/// denominator Counter. Beware that this can give NaN values for zero counts
		/// in the denominator counter!
		/// </remarks>
		public static void DivideInPlace<E>(ICounter<E> target, ICounter<E> denominator)
		{
			foreach (E key in target.KeySet())
			{
				target.SetCount(key, target.GetCount(key) / denominator.GetCount(key));
			}
		}

		/// <summary>
		/// Multiplies every count in target by the corresponding value in the term
		/// Counter.
		/// </summary>
		public static void DotProductInPlace<E>(ICounter<E> target, ICounter<E> term)
		{
			foreach (E key in target.KeySet())
			{
				target.SetCount(key, target.GetCount(key) * term.GetCount(key));
			}
		}

		/// <summary>Divides each value in target by the given divisor, in place.</summary>
		/// <param name="target">
		/// The values in this Counter will be changed throughout by the
		/// multiplier
		/// </param>
		/// <param name="divisor">The number by which to change each number in the Counter</param>
		/// <returns>The target Counter is returned (for easier method chaining)</returns>
		public static ICounter<E> DivideInPlace<E>(ICounter<E> target, double divisor)
		{
			foreach (KeyValuePair<E, double> entry in target.EntrySet())
			{
				target.SetCount(entry.Key, entry.Value / divisor);
			}
			return target;
		}

		/// <summary>Multiplies each value in target by the given multiplier, in place.</summary>
		/// <param name="target">
		/// The values in this Counter will be multiplied by the
		/// multiplier
		/// </param>
		/// <param name="multiplier">The number by which to change each number in the Counter</param>
		public static ICounter<E> MultiplyInPlace<E>(ICounter<E> target, double multiplier)
		{
			foreach (KeyValuePair<E, double> entry in target.EntrySet())
			{
				target.SetCount(entry.Key, entry.Value * multiplier);
			}
			return target;
		}

		/// <summary>Multiplies each value in target by the count of the key in mult, in place.</summary>
		/// <remarks>Multiplies each value in target by the count of the key in mult, in place. Returns non zero entries</remarks>
		/// <param name="target">The counter</param>
		/// <param name="mult">The counter you want to multiply with target</param>
		public static ICounter<E> MultiplyInPlace<E>(ICounter<E> target, ICounter<E> mult)
		{
			foreach (KeyValuePair<E, double> entry in target.EntrySet())
			{
				target.SetCount(entry.Key, entry.Value * mult.GetCount(entry.Key));
			}
			Edu.Stanford.Nlp.Stats.Counters.RetainNonZeros(target);
			return target;
		}

		/// <summary>
		/// Normalizes the target counter in-place, so the sum of the resulting values
		/// equals 1.
		/// </summary>
		/// <?/>
		public static void Normalize<E>(ICounter<E> target)
		{
			DivideInPlace(target, target.TotalCount());
		}

		/// <summary>L1 normalize a counter.</summary>
		/// <remarks>
		/// L1 normalize a counter. Return a counter that is a probability distribution,
		/// so the sum of the resulting value equals 1.
		/// </remarks>
		/// <param name="c">
		/// The
		/// <see cref="ICounter{E}"/>
		/// to be L1 normalized. This counter is not
		/// modified.
		/// </param>
		/// <returns>A new L1-normalized Counter based on c.</returns>
		public static C AsNormalizedCounter<E, C>(C c)
			where C : ICounter<E>
		{
			return Scale(c, 1.0 / c.TotalCount());
		}

		/// <summary>
		/// Normalizes the target counter in-place, so the sum of the resulting values
		/// equals 1.
		/// </summary>
		/// <?/>
		/// <?/>
		public static void Normalize<E, F>(TwoDimensionalCounter<E, F> target)
		{
			Edu.Stanford.Nlp.Stats.Counters.DivideInPlace(target, target.TotalCount());
		}

		public static void LogInPlace<E>(ICounter<E> target)
		{
			foreach (E key in target.KeySet())
			{
				target.SetCount(key, System.Math.Log(target.GetCount(key)));
			}
		}

		//
		// Selection Operators
		//
		/// <summary>
		/// Delete 'top' and 'bottom' number of elements from the top and bottom
		/// respectively
		/// </summary>
		public static IList<E> DeleteOutofRange<E>(ICounter<E> c, int top, int bottom)
		{
			IList<E> purgedItems = new List<E>();
			int numToPurge = top + bottom;
			if (numToPurge <= 0)
			{
				return purgedItems;
			}
			IList<E> l = Edu.Stanford.Nlp.Stats.Counters.ToSortedList(c);
			for (int i = 0; i < top; i++)
			{
				E item = l[i];
				purgedItems.Add(item);
				c.Remove(item);
			}
			int size = c.Size();
			for (int i_1 = c.Size() - 1; i_1 >= (size - bottom); i_1--)
			{
				E item = l[i_1];
				purgedItems.Add(item);
				c.Remove(item);
			}
			return purgedItems;
		}

		/// <summary>
		/// Removes all entries from c except for the top
		/// <paramref name="num"/>
		/// .
		/// </summary>
		public static void RetainTop<E>(ICounter<E> c, int num)
		{
			int numToPurge = c.Size() - num;
			if (numToPurge <= 0)
			{
				return;
			}
			IList<E> l = Edu.Stanford.Nlp.Stats.Counters.ToSortedList(c, true);
			for (int i = 0; i < numToPurge; i++)
			{
				c.Remove(l[i]);
			}
		}

		/// <summary>
		/// Removes all entries from c except for the top
		/// <paramref name="num"/>
		/// .
		/// </summary>
		public static void RetainTopKeyComparable<E>(ICounter<E> c, int num)
			where E : IComparable<E>
		{
			int numToPurge = c.Size() - num;
			if (numToPurge <= 0)
			{
				return;
			}
			IList<E> l = Edu.Stanford.Nlp.Stats.Counters.ToSortedListKeyComparable(c);
			Java.Util.Collections.Reverse(l);
			for (int i = 0; i < numToPurge; i++)
			{
				c.Remove(l[i]);
			}
		}

		/// <summary>
		/// Removes all entries from c except for the bottom
		/// <paramref name="num"/>
		/// .
		/// </summary>
		public static IList<E> RetainBottom<E>(ICounter<E> c, int num)
		{
			int numToPurge = c.Size() - num;
			if (numToPurge <= 0)
			{
				return Generics.NewArrayList();
			}
			IList<E> removed = new List<E>();
			IList<E> l = Edu.Stanford.Nlp.Stats.Counters.ToSortedList(c);
			for (int i = 0; i < numToPurge; i++)
			{
				E rem = l[i];
				removed.Add(rem);
				c.Remove(rem);
			}
			return removed;
		}

		/// <summary>
		/// Removes all entries with 0 count in the counter, returning the set of
		/// removed entries.
		/// </summary>
		public static ICollection<E> RetainNonZeros<E>(ICounter<E> counter)
		{
			ICollection<E> removed = Generics.NewHashSet();
			foreach (E key in counter.KeySet())
			{
				if (counter.GetCount(key) == 0.0)
				{
					removed.Add(key);
				}
			}
			foreach (E key_1 in removed)
			{
				counter.Remove(key_1);
			}
			return removed;
		}

		/// <summary>
		/// Removes all entries with counts below the given threshold, returning the
		/// set of removed entries.
		/// </summary>
		/// <param name="counter">The counter.</param>
		/// <param name="countThreshold">
		/// The minimum count for an entry to be kept. Entries (strictly) less
		/// than this threshold are discarded.
		/// </param>
		/// <returns>The set of discarded entries.</returns>
		public static ICollection<E> RetainAbove<E>(ICounter<E> counter, double countThreshold)
		{
			ICollection<E> removed = Generics.NewHashSet();
			foreach (E key in counter.KeySet())
			{
				if (counter.GetCount(key) < countThreshold)
				{
					removed.Add(key);
				}
			}
			foreach (E key_1 in removed)
			{
				counter.Remove(key_1);
			}
			return removed;
		}

		/// <summary>
		/// Removes all entries with counts below the given threshold, returning the
		/// set of removed entries.
		/// </summary>
		/// <param name="counter">The counter.</param>
		/// <param name="countThreshold">
		/// The minimum count for an entry to be kept. Entries (strictly) less
		/// than this threshold are discarded.
		/// </param>
		/// <returns>The set of discarded entries.</returns>
		public static ICollection<Pair<E1, E2>> RetainAbove<E1, E2>(TwoDimensionalCounter<E1, E2> counter, double countThreshold)
		{
			ICollection<Pair<E1, E2>> removed = new HashSet<Pair<E1, E2>>();
			foreach (KeyValuePair<E1, ClassicCounter<E2>> en in counter.EntrySet())
			{
				foreach (KeyValuePair<E2, double> en2 in en.Value.EntrySet())
				{
					if (counter.GetCount(en.Key, en2.Key) < countThreshold)
					{
						removed.Add(new Pair<E1, E2>(en.Key, en2.Key));
					}
				}
			}
			foreach (Pair<E1, E2> key in removed)
			{
				counter.Remove(key.First(), key.Second());
			}
			return removed;
		}

		/// <summary>
		/// Removes all entries with counts above the given threshold, returning the
		/// set of removed entries.
		/// </summary>
		/// <param name="counter">The counter.</param>
		/// <param name="countMaxThreshold">
		/// The maximum count for an entry to be kept. Entries (strictly) more
		/// than this threshold are discarded.
		/// </param>
		/// <returns>The set of discarded entries.</returns>
		public static ICounter<E> RetainBelow<E>(ICounter<E> counter, double countMaxThreshold)
		{
			ICounter<E> removed = new ClassicCounter<E>();
			foreach (E key in counter.KeySet())
			{
				double count = counter.GetCount(key);
				if (counter.GetCount(key) > countMaxThreshold)
				{
					removed.SetCount(key, count);
				}
			}
			foreach (KeyValuePair<E, double> key_1 in removed.EntrySet())
			{
				counter.Remove(key_1.Key);
			}
			return removed;
		}

		/// <summary>Removes all entries with keys that does not match one of the given patterns.</summary>
		/// <param name="counter">The counter.</param>
		/// <param name="matchPatterns">pattern for key to match</param>
		/// <returns>The set of discarded entries.</returns>
		public static ICollection<string> RetainMatchingKeys(ICounter<string> counter, IList<Pattern> matchPatterns)
		{
			ICollection<string> removed = Generics.NewHashSet();
			foreach (string key in counter.KeySet())
			{
				bool matched = false;
				foreach (Pattern pattern in matchPatterns)
				{
					if (pattern.Matcher(key).Matches())
					{
						matched = true;
						break;
					}
				}
				if (!matched)
				{
					removed.Add(key);
				}
			}
			foreach (string key_1 in removed)
			{
				counter.Remove(key_1);
			}
			return removed;
		}

		/// <summary>Removes all entries with keys that does not match the given set of keys.</summary>
		/// <param name="counter">The counter</param>
		/// <param name="matchKeys">Keys to match</param>
		/// <returns>The set of discarded entries.</returns>
		public static ICollection<E> RetainKeys<E>(ICounter<E> counter, ICollection<E> matchKeys)
		{
			ICollection<E> removed = Generics.NewHashSet();
			foreach (E key in counter.KeySet())
			{
				bool matched = matchKeys.Contains(key);
				if (!matched)
				{
					removed.Add(key);
				}
			}
			foreach (E key_1 in removed)
			{
				counter.Remove(key_1);
			}
			return removed;
		}

		/// <summary>Removes all entries with keys in the given collection</summary>
		/// <?/>
		/// <param name="counter"/>
		/// <param name="removeKeysCollection"/>
		public static void RemoveKeys<E>(ICounter<E> counter, ICollection<E> removeKeysCollection)
		{
			foreach (E key in removeKeysCollection)
			{
				counter.Remove(key);
			}
		}

		/// <summary>Removes all entries with keys (first key set) in the given collection</summary>
		/// <?/>
		/// <param name="counter"/>
		/// <param name="removeKeysCollection"/>
		public static void RemoveKeys<E, F>(TwoDimensionalCounter<E, F> counter, ICollection<E> removeKeysCollection)
		{
			foreach (E key in removeKeysCollection)
			{
				counter.Remove(key);
			}
		}

		/// <summary>Returns the set of keys whose counts are at or above the given threshold.</summary>
		/// <remarks>
		/// Returns the set of keys whose counts are at or above the given threshold.
		/// This set may have 0 elements but will not be null.
		/// </remarks>
		/// <param name="c">The Counter to examine</param>
		/// <param name="countThreshold">Items equal to or above this number are kept</param>
		/// <returns>
		/// A (non-null) Set of keys whose counts are at or above the given
		/// threshold.
		/// </returns>
		public static ICollection<E> KeysAbove<E>(ICounter<E> c, double countThreshold)
		{
			ICollection<E> keys = Generics.NewHashSet();
			foreach (E key in c.KeySet())
			{
				if (c.GetCount(key) >= countThreshold)
				{
					keys.Add(key);
				}
			}
			return (keys);
		}

		/// <summary>Returns the set of keys whose counts are at or below the given threshold.</summary>
		/// <remarks>
		/// Returns the set of keys whose counts are at or below the given threshold.
		/// This set may have 0 elements but will not be null.
		/// </remarks>
		public static ICollection<E> KeysBelow<E>(ICounter<E> c, double countThreshold)
		{
			ICollection<E> keys = Generics.NewHashSet();
			foreach (E key in c.KeySet())
			{
				if (c.GetCount(key) <= countThreshold)
				{
					keys.Add(key);
				}
			}
			return (keys);
		}

		/// <summary>Returns the set of keys that have exactly the given count.</summary>
		/// <remarks>
		/// Returns the set of keys that have exactly the given count. This set may
		/// have 0 elements but will not be null.
		/// </remarks>
		public static ICollection<E> KeysAt<E>(ICounter<E> c, double count)
		{
			ICollection<E> keys = Generics.NewHashSet();
			foreach (E key in c.KeySet())
			{
				if (c.GetCount(key) == count)
				{
					keys.Add(key);
				}
			}
			return (keys);
		}

		//
		// Transforms
		//
		/// <summary>Returns the counter with keys modified according to function F.</summary>
		/// <remarks>
		/// Returns the counter with keys modified according to function F. Eager
		/// evaluation. If two keys are same after the transformation, one of the values is randomly chosen (depending on how the keyset is traversed)
		/// </remarks>
		public static ICounter<T2> Transform<T1, T2>(ICounter<T1> c, IFunction<T1, T2> f)
		{
			ICounter<T2> c2 = new ClassicCounter<T2>();
			foreach (T1 key in c.KeySet())
			{
				c2.SetCount(f.Apply(key), c.GetCount(key));
			}
			return c2;
		}

		/// <summary>Returns the counter with keys modified according to function F.</summary>
		/// <remarks>Returns the counter with keys modified according to function F. If two keys are same after the transformation, their values get added up.</remarks>
		public static ICounter<T2> TransformWithValuesAdd<T1, T2>(ICounter<T1> c, IFunction<T1, T2> f)
		{
			ICounter<T2> c2 = new ClassicCounter<T2>();
			foreach (T1 key in c.KeySet())
			{
				c2.IncrementCount(f.Apply(key), c.GetCount(key));
			}
			return c2;
		}

		//
		// Conversion to other types
		//
		/// <summary>
		/// Returns a comparator backed by this counter: two objects are compared by
		/// their associated values stored in the counter.
		/// </summary>
		/// <remarks>
		/// Returns a comparator backed by this counter: two objects are compared by
		/// their associated values stored in the counter. This comparator returns keys
		/// by ascending numeric value. Note that this ordering is not fixed, but
		/// depends on the mutable values stored in the Counter. Doing this comparison
		/// does not depend on the type of the key, since it uses the numeric value,
		/// which is always Comparable.
		/// </remarks>
		/// <param name="counter">The Counter whose values are used for ordering the keys</param>
		/// <returns>A Comparator using this ordering</returns>
		public static IComparator<E> ToComparator<E>(ICounter<E> counter)
		{
			return null;
		}

		/// <summary>
		/// Returns a comparator backed by this counter: two objects are compared by
		/// their associated values stored in the counter.
		/// </summary>
		/// <remarks>
		/// Returns a comparator backed by this counter: two objects are compared by
		/// their associated values stored in the counter. This comparator returns keys
		/// by ascending numeric value. Note that this ordering is not fixed, but
		/// depends on the mutable values stored in the Counter. Doing this comparison
		/// does not depend on the type of the key, since it uses the numeric value,
		/// which is always Comparable.
		/// </remarks>
		/// <param name="counter">The Counter whose values are used for ordering the keys</param>
		/// <returns>A Comparator using this ordering</returns>
		public static IComparator<E> ToComparatorWithKeys<E>(ICounter<E> counter)
			where E : IComparable<E>
		{
			return null;
		}

		/// <summary>
		/// Returns a comparator backed by this counter: two objects are compared by
		/// their associated values stored in the counter.
		/// </summary>
		/// <remarks>
		/// Returns a comparator backed by this counter: two objects are compared by
		/// their associated values stored in the counter. This comparator returns keys
		/// by descending numeric value. Note that this ordering is not fixed, but
		/// depends on the mutable values stored in the Counter. Doing this comparison
		/// does not depend on the type of the key, since it uses the numeric value,
		/// which is always Comparable.
		/// </remarks>
		/// <param name="counter">The Counter whose values are used for ordering the keys</param>
		/// <returns>A Comparator using this ordering</returns>
		public static IComparator<E> ToComparatorDescending<E>(ICounter<E> counter)
		{
			return null;
		}

		/// <summary>
		/// Returns a comparator suitable for sorting this Counter's keys or entries by
		/// their respective value or magnitude (by absolute value).
		/// </summary>
		/// <remarks>
		/// Returns a comparator suitable for sorting this Counter's keys or entries by
		/// their respective value or magnitude (by absolute value). If
		/// <tt>ascending</tt> is true, smaller magnitudes will be returned first,
		/// otherwise higher magnitudes will be returned first.
		/// <p>
		/// Sample usage:
		/// <pre>
		/// Counter c = new Counter();
		/// // add to the counter...
		/// List biggestAbsKeys = new ArrayList(c.keySet());
		/// Collections.sort(biggestAbsKeys, Counters.comparator(c, false, true));
		/// List smallestEntries = new ArrayList(c.entrySet());
		/// Collections.sort(smallestEntries, Counters.comparator(c, true, false));
		/// </pre>
		/// </remarks>
		public static IComparator<E> ToComparator<E>(ICounter<E> counter, bool ascending, bool useMagnitude)
		{
			return null;
		}

		// Descending
		/// <summary>A List of the keys in c, sorted from highest count to lowest.</summary>
		/// <remarks>
		/// A List of the keys in c, sorted from highest count to lowest.
		/// So note that the default is descending!
		/// </remarks>
		/// <returns>A List of the keys in c, sorted from highest count to lowest.</returns>
		public static IList<E> ToSortedList<E>(ICounter<E> c)
		{
			return ToSortedList(c, false);
		}

		/// <summary>A List of the keys in c, sorted from highest count to lowest.</summary>
		/// <returns>A List of the keys in c, sorted from highest count to lowest.</returns>
		public static IList<E> ToSortedList<E>(ICounter<E> c, bool ascending)
		{
			IList<E> l = new List<E>(c.KeySet());
			IComparator<E> comp = ascending ? ToComparator(c) : ToComparatorDescending(c);
			l.Sort(comp);
			return l;
		}

		/// <summary>A List of the keys in c, sorted from highest count to lowest.</summary>
		/// <returns>A List of the keys in c, sorted from highest count to lowest.</returns>
		public static IList<E> ToSortedListKeyComparable<E>(ICounter<E> c)
			where E : IComparable<E>
		{
			IList<E> l = new List<E>(c.KeySet());
			IComparator<E> comp = ToComparatorWithKeys(c);
			l.Sort(comp);
			Java.Util.Collections.Reverse(l);
			return l;
		}

		/// <summary>Converts a counter to ranks; ranks start from 0</summary>
		/// <returns>A counter where the count is the rank in the original counter</returns>
		public static IntCounter<E> ToRankCounter<E>(ICounter<E> c)
		{
			IntCounter<E> rankCounter = new IntCounter<E>();
			IList<E> sortedList = ToSortedList(c);
			for (int i = 0; i < sortedList.Count; i++)
			{
				rankCounter.SetCount(sortedList[i], i);
			}
			return rankCounter;
		}

		/// <summary>Converts a counter to tied ranks; ranks start from 1</summary>
		/// <returns>A counter where the count is the rank in the original counter; when values are tied, the rank is the average of the ranks of the tied values</returns>
		public static ICounter<E> ToTiedRankCounter<E>(ICounter<E> c)
		{
			ICounter<E> rankCounter = new ClassicCounter<E>();
			IList<Pair<E, double>> sortedList = ToSortedListWithCounts(c);
			int i = 0;
			IEnumerator<Pair<E, double>> it = sortedList.GetEnumerator();
			while (it.MoveNext())
			{
				Pair<E, double> iEn = it.Current;
				double icount = iEn.Second();
				E iKey = iEn.First();
				IList<int> l = new List<int>();
				IList<E> keys = new List<E>();
				l.Add(i + 1);
				keys.Add(iKey);
				for (int j = i + 1; j < sortedList.Count; j++)
				{
					Pair<E, double> jEn = sortedList[j];
					if (icount == jEn.Second())
					{
						l.Add(j + 1);
						keys.Add(jEn.First());
					}
					else
					{
						break;
					}
				}
				if (l.Count > 1)
				{
					double sum = 0;
					foreach (int d in l)
					{
						sum += d;
					}
					double avgRank = sum / l.Count;
					for (int k = 0; k < l.Count; k++)
					{
						rankCounter.SetCount(keys[k], avgRank);
						if (k != l.Count - 1 && it.MoveNext())
						{
							it.Current;
						}
						i++;
					}
				}
				else
				{
					rankCounter.SetCount(iKey, i + 1);
					i++;
				}
			}
			return rankCounter;
		}

		public static IList<Pair<E, double>> ToDescendingMagnitudeSortedListWithCounts<E>(ICounter<E> c)
		{
			IList<E> keys = new List<E>(c.KeySet());
			keys.Sort(ToComparator(c, false, true));
			IList<Pair<E, double>> l = new List<Pair<E, double>>(keys.Count);
			foreach (E key in keys)
			{
				l.Add(new Pair<E, double>(key, c.GetCount(key)));
			}
			return l;
		}

		/// <summary>
		/// A List of the keys in c, sorted from highest count to lowest, paired with
		/// counts
		/// </summary>
		/// <returns>A List of the keys in c, sorted from highest count to lowest.</returns>
		public static IList<Pair<E, double>> ToSortedListWithCounts<E>(ICounter<E> c)
		{
			IList<Pair<E, double>> l = new List<Pair<E, double>>(c.Size());
			foreach (E e in c.KeySet())
			{
				l.Add(new Pair<E, double>(e, c.GetCount(e)));
			}
			// descending order
			l.Sort(null);
			return l;
		}

		/// <summary>
		/// A List of the keys in c, sorted by the given comparator, paired with
		/// counts.
		/// </summary>
		/// <returns>A List of the keys in c, sorted from highest count to lowest.</returns>
		public static IList<Pair<E, double>> ToSortedListWithCounts<E>(ICounter<E> c, IComparator<Pair<E, double>> comparator)
		{
			IList<Pair<E, double>> l = new List<Pair<E, double>>(c.Size());
			foreach (E e in c.KeySet())
			{
				l.Add(new Pair<E, double>(e, c.GetCount(e)));
			}
			// descending order
			l.Sort(comparator);
			return l;
		}

		/// <summary>
		/// Returns a
		/// <see cref="Edu.Stanford.Nlp.Util.IPriorityQueue{E}"/>
		/// whose elements are
		/// the keys of Counter c, and the score of each key in c becomes its priority.
		/// </summary>
		/// <param name="c">Input Counter</param>
		/// <returns>A PriorityQueue where the count is a key's priority</returns>
		public static IPriorityQueue<E> ToPriorityQueue<E>(ICounter<E> c)
		{
			// TODO: rewrite to use entrySet()
			IPriorityQueue<E> queue = new BinaryHeapPriorityQueue<E>();
			foreach (E key in c.KeySet())
			{
				double count = c.GetCount(key);
				queue.Add(key, count);
			}
			return queue;
		}

		//
		// Other Utilities
		//
		/// <summary>
		/// Returns a Counter that is the union of the two Counters passed in (counts
		/// are added).
		/// </summary>
		/// <returns>
		/// A Counter that is the union of the two Counters passed in (counts
		/// are added).
		/// </returns>
		public static C Union<E, C>(C c1, C c2)
			where C : ICounter<E>
		{
			C result = (C)c1.GetFactory().Create();
			AddInPlace(result, c1);
			AddInPlace(result, c2);
			return result;
		}

		/// <summary>Returns a counter that is the intersection of c1 and c2.</summary>
		/// <remarks>
		/// Returns a counter that is the intersection of c1 and c2. If both c1 and c2
		/// contain a key, the min of the two counts is used.
		/// </remarks>
		/// <returns>A counter that is the intersection of c1 and c2</returns>
		public static ICounter<E> Intersection<E>(ICounter<E> c1, ICounter<E> c2)
		{
			ICounter<E> result = c1.GetFactory().Create();
			foreach (E key in Sets.Union(c1.KeySet(), c2.KeySet()))
			{
				double count1 = c1.GetCount(key);
				double count2 = c2.GetCount(key);
				double minCount = (count1 < count2 ? count1 : count2);
				if (minCount > 0)
				{
					result.SetCount(key, minCount);
				}
			}
			return result;
		}

		/// <summary>Returns the Jaccard Coefficient of the two counters.</summary>
		/// <remarks>
		/// Returns the Jaccard Coefficient of the two counters. Calculated as |c1
		/// intersect c2| / ( |c1| + |c2| - |c1 intersect c2|
		/// </remarks>
		/// <returns>The Jaccard Coefficient of the two counters</returns>
		public static double JaccardCoefficient<E>(ICounter<E> c1, ICounter<E> c2)
		{
			double minCount = 0.0;
			double maxCount = 0.0;
			foreach (E key in Sets.Union(c1.KeySet(), c2.KeySet()))
			{
				double count1 = c1.GetCount(key);
				double count2 = c2.GetCount(key);
				minCount += (count1 < count2 ? count1 : count2);
				maxCount += (count1 > count2 ? count1 : count2);
			}
			return minCount / maxCount;
		}

		/// <summary>Returns the product of c1 and c2.</summary>
		/// <returns>The product of c1 and c2.</returns>
		public static ICounter<E> Product<E>(ICounter<E> c1, ICounter<E> c2)
		{
			ICounter<E> result = c1.GetFactory().Create();
			foreach (E key in Sets.Intersection(c1.KeySet(), c2.KeySet()))
			{
				result.SetCount(key, c1.GetCount(key) * c2.GetCount(key));
			}
			return result;
		}

		/// <summary>Returns the product of c1 and c2.</summary>
		/// <returns>The product of c1 and c2.</returns>
		public static double DotProduct<E>(ICounter<E> c1, ICounter<E> c2)
		{
			double dotProd = 0.0;
			if (c1.Size() > c2.Size())
			{
				ICounter<E> tmpCnt = c1;
				c1 = c2;
				c2 = tmpCnt;
			}
			foreach (E key in c1.KeySet())
			{
				double count1 = c1.GetCount(key);
				if (double.IsNaN(count1) || double.IsInfinite(count1))
				{
					throw new Exception("Counters.dotProduct infinite or NaN value for key: " + key + '\t' + c1.GetCount(key) + '\t' + c2.GetCount(key));
				}
				if (count1 != 0.0)
				{
					double count2 = c2.GetCount(key);
					if (double.IsNaN(count2) || double.IsInfinite(count2))
					{
						throw new Exception("Counters.dotProduct infinite or NaN value for key: " + key + '\t' + c1.GetCount(key) + '\t' + c2.GetCount(key));
					}
					if (count2 != 0.0)
					{
						// this is the inner product
						dotProd += (count1 * count2);
					}
				}
			}
			return dotProd;
		}

		/// <summary>
		/// Returns the product of Counter c and double[] a, using Index idx to map
		/// entries in C onto a.
		/// </summary>
		/// <returns>The product of c and a.</returns>
		public static double DotProduct<E>(ICounter<E> c, double[] a, IIndex<E> idx)
		{
			double dotProd = 0.0;
			foreach (KeyValuePair<E, double> entry in c.EntrySet())
			{
				int keyIdx = idx.IndexOf(entry.Key);
				if (keyIdx >= 0)
				{
					dotProd += entry.Value * a[keyIdx];
				}
			}
			return dotProd;
		}

		public static double SumEntries<E>(ICounter<E> c1, ICollection<E> entries)
		{
			double dotProd = 0.0;
			foreach (E entry in entries)
			{
				dotProd += c1.GetCount(entry);
			}
			return dotProd;
		}

		public static ICounter<E> Add<E>(ICounter<E> c1, ICollection<E> c2)
		{
			ICounter<E> result = c1.GetFactory().Create();
			AddInPlace(result, c1);
			foreach (E key in c2)
			{
				result.IncrementCount(key, 1);
			}
			return result;
		}

		public static ICounter<E> Add<E>(ICounter<E> c1, ICounter<E> c2)
		{
			ICounter<E> result = c1.GetFactory().Create();
			foreach (E key in Sets.Union(c1.KeySet(), c2.KeySet()))
			{
				result.SetCount(key, c1.GetCount(key) + c2.GetCount(key));
			}
			RetainNonZeros(result);
			return result;
		}

		/// <summary>increments every key in the counter by value</summary>
		public static ICounter<E> Add<E>(ICounter<E> c1, double value)
		{
			ICounter<E> result = c1.GetFactory().Create();
			foreach (E key in c1.KeySet())
			{
				result.SetCount(key, c1.GetCount(key) + value);
			}
			return result;
		}

		/// <summary>
		/// This method does not check entries for NAN or INFINITY values in the
		/// doubles returned.
		/// </summary>
		/// <remarks>
		/// This method does not check entries for NAN or INFINITY values in the
		/// doubles returned. It also only iterates over the counter with the smallest
		/// number of keys to help speed up computation. Pair this method with
		/// normalizing your counters before hand and you have a reasonably quick
		/// implementation of cosine.
		/// </remarks>
		/// <?/>
		/// <param name="c1"/>
		/// <param name="c2"/>
		/// <returns>The dot product of the two counter (as vectors)</returns>
		public static double OptimizedDotProduct<E>(ICounter<E> c1, ICounter<E> c2)
		{
			int size1 = c1.Size();
			int size2 = c2.Size();
			if (size1 < size2)
			{
				return GetDotProd(c1, c2);
			}
			else
			{
				return GetDotProd(c2, c1);
			}
		}

		private static double GetDotProd<E>(ICounter<E> c1, ICounter<E> c2)
		{
			double dotProd = 0.0;
			foreach (E key in c1.KeySet())
			{
				double count1 = c1.GetCount(key);
				if (count1 != 0.0)
				{
					double count2 = c2.GetCount(key);
					if (count2 != 0.0)
					{
						dotProd += (count1 * count2);
					}
				}
			}
			return dotProd;
		}

		/// <summary>Returns |c1 - c2|.</summary>
		/// <returns>The difference between sets c1 and c2.</returns>
		public static ICounter<E> AbsoluteDifference<E>(ICounter<E> c1, ICounter<E> c2)
		{
			ICounter<E> result = c1.GetFactory().Create();
			foreach (E key in Sets.Union(c1.KeySet(), c2.KeySet()))
			{
				double newCount = System.Math.Abs(c1.GetCount(key) - c2.GetCount(key));
				if (newCount > 0)
				{
					result.SetCount(key, newCount);
				}
			}
			return result;
		}

		/// <summary>Returns c1 divided by c2.</summary>
		/// <remarks>
		/// Returns c1 divided by c2. Note that this can create NaN if c1 has non-zero
		/// counts for keys that c2 has zero counts.
		/// </remarks>
		/// <returns>c1 divided by c2.</returns>
		public static ICounter<E> Division<E>(ICounter<E> c1, ICounter<E> c2)
		{
			ICounter<E> result = c1.GetFactory().Create();
			foreach (E key in Sets.Union(c1.KeySet(), c2.KeySet()))
			{
				result.SetCount(key, c1.GetCount(key) / c2.GetCount(key));
			}
			return result;
		}

		/// <summary>Returns c1 divided by c2.</summary>
		/// <remarks>Returns c1 divided by c2. Safe - will not calculate scores for keys that are zero or that do not exist in c2</remarks>
		/// <returns>c1 divided by c2.</returns>
		public static ICounter<E> DivisionNonNaN<E>(ICounter<E> c1, ICounter<E> c2)
		{
			ICounter<E> result = c1.GetFactory().Create();
			foreach (E key in Sets.Union(c1.KeySet(), c2.KeySet()))
			{
				if (c2.GetCount(key) != 0)
				{
					result.SetCount(key, c1.GetCount(key) / c2.GetCount(key));
				}
			}
			return result;
		}

		/// <summary>Calculates the entropy of the given counter (in bits).</summary>
		/// <remarks>
		/// Calculates the entropy of the given counter (in bits). This method
		/// internally uses normalized counts (so they sum to one), but the value
		/// returned is meaningless if some of the counts are negative.
		/// </remarks>
		/// <returns>The entropy of the given counter (in bits)</returns>
		public static double Entropy<E>(ICounter<E> c)
		{
			double entropy = 0.0;
			double total = c.TotalCount();
			foreach (E key in c.KeySet())
			{
				double count = c.GetCount(key);
				if (count == 0)
				{
					continue;
				}
				// 0.0 doesn't add entropy but may cause -Inf
				count /= total;
				// use normalized count
				entropy -= count * (System.Math.Log(count) / LogE2);
			}
			return entropy;
		}

		/// <summary>Note that this implementation doesn't normalize the "from" Counter.</summary>
		/// <remarks>
		/// Note that this implementation doesn't normalize the "from" Counter. It
		/// does, however, normalize the "to" Counter. Result is meaningless if any of
		/// the counts are negative.
		/// </remarks>
		/// <returns>The cross entropy of H(from, to)</returns>
		public static double CrossEntropy<E>(ICounter<E> from, ICounter<E> to)
		{
			double tot2 = to.TotalCount();
			double result = 0.0;
			foreach (E key in from.KeySet())
			{
				double count1 = from.GetCount(key);
				if (count1 == 0.0)
				{
					continue;
				}
				double count2 = to.GetCount(key);
				double logFract = System.Math.Log(count2 / tot2);
				if (logFract == double.NegativeInfinity)
				{
					return double.NegativeInfinity;
				}
				// can't recover
				result += count1 * (logFract / LogE2);
			}
			// express it in log base 2
			return result;
		}

		/// <summary>Calculates the KL divergence between the two counters.</summary>
		/// <remarks>
		/// Calculates the KL divergence between the two counters. That is, it
		/// calculates KL(from || to). This method internally uses normalized counts
		/// (so they sum to one), but the value returned is meaningless if any of the
		/// counts are negative. In other words, how well can c1 be represented by c2.
		/// if there is some value in c1 that gets zero prob in c2, then return
		/// positive infinity.
		/// </remarks>
		/// <returns>The KL divergence between the distributions</returns>
		public static double KlDivergence<E>(ICounter<E> from, ICounter<E> to)
		{
			double result = 0.0;
			double tot = (from.TotalCount());
			double tot2 = (to.TotalCount());
			// System.out.println("tot is " + tot + " tot2 is " + tot2);
			foreach (E key in from.KeySet())
			{
				double num = (from.GetCount(key));
				if (num == 0)
				{
					continue;
				}
				num /= tot;
				double num2 = (to.GetCount(key));
				num2 /= tot2;
				// System.out.println("num is " + num + " num2 is " + num2);
				double logFract = System.Math.Log(num / num2);
				if (logFract == double.NegativeInfinity)
				{
					return double.NegativeInfinity;
				}
				// can't recover
				result += num * (logFract / LogE2);
			}
			// express it in log base 2
			return result;
		}

		/// <summary>Calculates the Jensen-Shannon divergence between the two counters.</summary>
		/// <remarks>
		/// Calculates the Jensen-Shannon divergence between the two counters. That is,
		/// it calculates 1/2 [KL(c1 || avg(c1,c2)) + KL(c2 || avg(c1,c2))] .
		/// This code assumes that the Counters have only non-negative values in them.
		/// </remarks>
		/// <returns>The Jensen-Shannon divergence between the distributions</returns>
		public static double JensenShannonDivergence<E>(ICounter<E> c1, ICounter<E> c2)
		{
			// need to normalize the counters first before averaging them! Else buggy if not a probability distribution
			ICounter<E> d1 = AsNormalizedCounter(c1);
			ICounter<E> d2 = AsNormalizedCounter(c2);
			ICounter<E> average = Average(d1, d2);
			double kl1 = KlDivergence(d1, average);
			double kl2 = KlDivergence(d2, average);
			return (kl1 + kl2) / 2.0;
		}

		/// <summary>Calculates the skew divergence between the two counters.</summary>
		/// <remarks>
		/// Calculates the skew divergence between the two counters. That is, it
		/// calculates KL(c1 || (c2*skew + c1*(1-skew))) . In other words, how well can
		/// c1 be represented by a "smoothed" c2.
		/// </remarks>
		/// <returns>The skew divergence between the distributions</returns>
		public static double SkewDivergence<E>(ICounter<E> c1, ICounter<E> c2, double skew)
		{
			ICounter<E> d1 = AsNormalizedCounter(c1);
			ICounter<E> d2 = AsNormalizedCounter(c2);
			ICounter<E> average = LinearCombination(d2, skew, d1, (1.0 - skew));
			return KlDivergence(d1, average);
		}

		/// <summary>Return the l2 norm (Euclidean vector length) of a Counter.</summary>
		/// <remarks>
		/// Return the l2 norm (Euclidean vector length) of a Counter.
		/// <i>Implementation note:</i> The method name favors legibility of the L over
		/// the convention of using lowercase names for methods.
		/// </remarks>
		/// <param name="c">The Counter</param>
		/// <returns>Its length</returns>
		public static double L2Norm<E, C>(C c)
			where C : ICounter<E>
		{
			return System.Math.Sqrt(Edu.Stanford.Nlp.Stats.Counters.SumSquares(c));
		}

		/// <summary>Return the sum of squares (squared L2 norm).</summary>
		/// <param name="c">The Counter</param>
		/// <returns>the L2 norm of the values in c</returns>
		public static double SumSquares<E, C>(C c)
			where C : ICounter<E>
		{
			double lenSq = 0.0;
			foreach (E key in c.KeySet())
			{
				double count = c.GetCount(key);
				lenSq += (count * count);
			}
			return lenSq;
		}

		/// <summary>Return the L1 norm of a counter.</summary>
		/// <remarks>
		/// Return the L1 norm of a counter. <i>Implementation note:</i> The method
		/// name favors legibility of the L over the convention of using lowercase
		/// names for methods.
		/// </remarks>
		/// <param name="c">The Counter</param>
		/// <returns>Its length</returns>
		public static double L1Norm<E, C>(C c)
			where C : ICounter<E>
		{
			double sumAbs = 0.0;
			foreach (E key in c.KeySet())
			{
				double count = c.GetCount(key);
				if (count != 0.0)
				{
					sumAbs += System.Math.Abs(count);
				}
			}
			return sumAbs;
		}

		/// <summary>L2 normalize a counter.</summary>
		/// <param name="c">
		/// The
		/// <see cref="ICounter{E}"/>
		/// to be L2 normalized. This counter is not
		/// modified.
		/// </param>
		/// <returns>A new l2-normalized Counter based on c.</returns>
		public static C L2Normalize<E, C>(C c)
			where C : ICounter<E>
		{
			return Scale(c, 1.0 / L2Norm(c));
		}

		/// <summary>L2 normalize a counter in place.</summary>
		/// <param name="c">
		/// The
		/// <see cref="ICounter{E}"/>
		/// to be L2 normalized. This counter is modified
		/// </param>
		/// <returns>the passed in counter l2-normalized</returns>
		public static ICounter<E> L2NormalizeInPlace<E>(ICounter<E> c)
		{
			return MultiplyInPlace(c, 1.0 / L2Norm(c));
		}

		/// <summary>
		/// For counters with large # of entries, this scales down each entry in the
		/// sum, to prevent an extremely large sum from building up and overwhelming
		/// the max double.
		/// </summary>
		/// <remarks>
		/// For counters with large # of entries, this scales down each entry in the
		/// sum, to prevent an extremely large sum from building up and overwhelming
		/// the max double. This may also help reduce error by preventing loss of SD's
		/// with extremely large values.
		/// </remarks>
		/// <?/>
		/// <?/>
		public static double SaferL2Norm<E, C>(C c)
			where C : ICounter<E>
		{
			double maxVal = 0.0;
			foreach (E key in c.KeySet())
			{
				double value = System.Math.Abs(c.GetCount(key));
				if (value > maxVal)
				{
					maxVal = value;
				}
			}
			double sqrSum = 0.0;
			foreach (E key_1 in c.KeySet())
			{
				double count = c.GetCount(key_1);
				sqrSum += System.Math.Pow(count / maxVal, 2);
			}
			return maxVal * System.Math.Sqrt(sqrSum);
		}

		/// <summary>L2 normalize a counter, using the "safer" L2 normalizer.</summary>
		/// <param name="c">
		/// The
		/// <see cref="ICounter{E}"/>
		/// to be L2 normalized. This counter is not
		/// modified.
		/// </param>
		/// <returns>A new L2-normalized Counter based on c.</returns>
		public static C SaferL2Normalize<E, C>(C c)
			where C : ICounter<E>
		{
			return Scale(c, 1.0 / SaferL2Norm(c));
		}

		public static double Cosine<E>(ICounter<E> c1, ICounter<E> c2)
		{
			double dotProd = 0.0;
			double lsq1 = 0.0;
			double lsq2 = 0.0;
			foreach (E key in c1.KeySet())
			{
				double count1 = c1.GetCount(key);
				if (count1 != 0.0)
				{
					lsq1 += (count1 * count1);
					double count2 = c2.GetCount(key);
					if (count2 != 0.0)
					{
						// this is the inner product
						dotProd += (count1 * count2);
					}
				}
			}
			foreach (E key_1 in c2.KeySet())
			{
				double count2 = c2.GetCount(key_1);
				if (count2 != 0.0)
				{
					lsq2 += (count2 * count2);
				}
			}
			if (lsq1 != 0.0 && lsq2 != 0.0)
			{
				double denom = (System.Math.Sqrt(lsq1) * System.Math.Sqrt(lsq2));
				return dotProd / denom;
			}
			return 0.0;
		}

		/// <summary>Returns a new Counter with counts averaged from the two given Counters.</summary>
		/// <remarks>
		/// Returns a new Counter with counts averaged from the two given Counters. The
		/// average Counter will contain the union of keys in both source Counters, and
		/// each count will be the average of the two source counts for that key, where
		/// as usual a missing count in one Counter is treated as count 0.
		/// </remarks>
		/// <returns>
		/// A new counter with counts that are the mean of the resp. counts in
		/// the given counters.
		/// </returns>
		public static ICounter<E> Average<E>(ICounter<E> c1, ICounter<E> c2)
		{
			ICounter<E> average = c1.GetFactory().Create();
			ICollection<E> allKeys = Generics.NewHashSet(c1.KeySet());
			Sharpen.Collections.AddAll(allKeys, c2.KeySet());
			foreach (E key in allKeys)
			{
				average.SetCount(key, (c1.GetCount(key) + c2.GetCount(key)) * 0.5);
			}
			return average;
		}

		/// <summary>Returns a Counter which is a weighted average of c1 and c2.</summary>
		/// <remarks>
		/// Returns a Counter which is a weighted average of c1 and c2. Counts from c1
		/// are weighted with weight w1 and counts from c2 are weighted with w2.
		/// </remarks>
		public static ICounter<E> LinearCombination<E>(ICounter<E> c1, double w1, ICounter<E> c2, double w2)
		{
			ICounter<E> result = c1.GetFactory().Create();
			foreach (E o in c1.KeySet())
			{
				result.IncrementCount(o, c1.GetCount(o) * w1);
			}
			foreach (E o_1 in c2.KeySet())
			{
				result.IncrementCount(o_1, c2.GetCount(o_1) * w2);
			}
			return result;
		}

		public static double PointwiseMutualInformation<T1, T2>(ICounter<T1> var1Distribution, ICounter<T2> var2Distribution, ICounter<Pair<T1, T2>> jointDistribution, Pair<T1, T2> values)
		{
			double var1Prob = var1Distribution.GetCount(values.first);
			double var2Prob = var2Distribution.GetCount(values.second);
			double jointProb = jointDistribution.GetCount(values);
			double pmi = System.Math.Log(jointProb) - System.Math.Log(var1Prob) - System.Math.Log(var2Prob);
			return pmi / LogE2;
		}

		/// <summary>Calculate h-Index (Hirsch, 2005) of an author.</summary>
		/// <remarks>
		/// Calculate h-Index (Hirsch, 2005) of an author.
		/// A scientist has index h if h of their Np papers have at least h citations
		/// each, and the other (Np − h) papers have at most h citations each.
		/// </remarks>
		/// <param name="citationCounts">
		/// Citation counts for each of the articles written by the author.
		/// The keys can be anything, but the values should be integers.
		/// </param>
		/// <returns>The h-Index of the author.</returns>
		public static int HIndex<E>(ICounter<E> citationCounts)
		{
			ICounter<int> countCounts = new ClassicCounter<int>();
			foreach (double value in citationCounts.Values())
			{
				for (int i = 0; i <= value; ++i)
				{
					countCounts.IncrementCount(i);
				}
			}
			IList<int> citationCountValues = CollectionUtils.Sorted(countCounts.KeySet());
			Java.Util.Collections.Reverse(citationCountValues);
			foreach (int citationCount in citationCountValues)
			{
				double occurrences = countCounts.GetCount(citationCount);
				if (occurrences >= citationCount)
				{
					return citationCount;
				}
			}
			return 0;
		}

		public static C PerturbCounts<E, C>(C c, Random random, double p)
			where C : ICounter<E>
		{
			C result = (C)c.GetFactory().Create();
			foreach (E key in c.KeySet())
			{
				double count = c.GetCount(key);
				double noise = -System.Math.Log(1.0 - random.NextDouble());
				// inverse of CDF for
				// exponential
				// distribution
				// log.info("noise=" + noise);
				double perturbedCount = count + noise * p;
				result.SetCount(key, perturbedCount);
			}
			return result;
		}

		/// <summary>Great for debugging.</summary>
		public static void PrintCounterComparison<E>(ICounter<E> a, ICounter<E> b)
		{
			PrintCounterComparison(a, b, System.Console.Error);
		}

		/// <summary>Great for debugging.</summary>
		public static void PrintCounterComparison<E>(ICounter<E> a, ICounter<E> b, TextWriter @out)
		{
			PrintCounterComparison(a, b, new PrintWriter(@out, true));
		}

		/// <summary>
		/// Prints one or more lines (with a newline at the end) describing the
		/// difference between the two Counters.
		/// </summary>
		/// <remarks>
		/// Prints one or more lines (with a newline at the end) describing the
		/// difference between the two Counters. Great for debugging.
		/// </remarks>
		public static void PrintCounterComparison<E>(ICounter<E> a, ICounter<E> b, PrintWriter @out)
		{
			if (a.Equals(b))
			{
				@out.Println("Counters are equal.");
				return;
			}
			foreach (E key in a.KeySet())
			{
				double aCount = a.GetCount(key);
				double bCount = b.GetCount(key);
				if (System.Math.Abs(aCount - bCount) > 1e-5)
				{
					@out.Println("Counters differ on key " + key + '\t' + a.GetCount(key) + " vs. " + b.GetCount(key));
				}
			}
			// left overs
			ICollection<E> rest = Generics.NewHashSet(b.KeySet());
			rest.RemoveAll(a.KeySet());
			foreach (E key_1 in rest)
			{
				double aCount = a.GetCount(key_1);
				double bCount = b.GetCount(key_1);
				if (System.Math.Abs(aCount - bCount) > 1e-5)
				{
					@out.Println("Counters differ on key " + key_1 + '\t' + a.GetCount(key_1) + " vs. " + b.GetCount(key_1));
				}
			}
		}

		public static ICounter<double> GetCountCounts<E>(ICounter<E> c)
		{
			ICounter<double> result = new ClassicCounter<double>();
			foreach (double v in c.Values())
			{
				result.IncrementCount(v);
			}
			return result;
		}

		/// <summary>Returns a new Counter which is scaled by the given scale factor.</summary>
		/// <param name="c">The counter to scale. It is not changed</param>
		/// <param name="s">The constant to scale the counter by</param>
		/// <returns>
		/// A new Counter which is the argument scaled by the given scale
		/// factor.
		/// </returns>
		public static C Scale<E, C>(C c, double s)
			where C : ICounter<E>
		{
			C scaled = (C)c.GetFactory().Create();
			foreach (E key in c.KeySet())
			{
				scaled.SetCount(key, c.GetCount(key) * s);
			}
			return scaled;
		}

		/// <summary>Returns a new Counter which is the input counter with log tf scaling</summary>
		/// <param name="c">The counter to scale. It is not changed</param>
		/// <param name="base">The base of the logarithm used for tf scaling by 1 + log tf</param>
		/// <returns>
		/// A new Counter which is the argument scaled by the given scale
		/// factor.
		/// </returns>
		public static C TfLogScale<E, C>(C c, double @base)
			where C : ICounter<E>
		{
			C scaled = (C)c.GetFactory().Create();
			foreach (E key in c.KeySet())
			{
				double cnt = c.GetCount(key);
				double scaledCnt = 0.0;
				if (cnt > 0)
				{
					scaledCnt = 1.0 + SloppyMath.Log(cnt, @base);
				}
				scaled.SetCount(key, scaledCnt);
			}
			return scaled;
		}

		public static void PrintCounterSortedByKeys<E>(ICounter<E> c)
			where E : IComparable<E>
		{
			IList<E> keyList = new List<E>(c.KeySet());
			keyList.Sort();
			foreach (E o in keyList)
			{
				System.Console.Out.WriteLine(o + ":" + c.GetCount(o));
			}
		}

		/// <summary>Loads a Counter from a text file.</summary>
		/// <remarks>
		/// Loads a Counter from a text file. File must have the format of one
		/// key/count pair per line, separated by whitespace.
		/// </remarks>
		/// <param name="filename">The path to the file to load the Counter from</param>
		/// <param name="c">
		/// The Class to instantiate each member of the set. Must have a
		/// String constructor.
		/// </param>
		/// <returns>The counter loaded from the file.</returns>
		/// <exception cref="System.Exception"/>
		public static ClassicCounter<E> LoadCounter<E>(string filename)
		{
			System.Type c = typeof(E);
			ClassicCounter<E> counter = new ClassicCounter<E>();
			LoadIntoCounter(filename, c, counter);
			return counter;
		}

		/// <summary>Loads a Counter from a text file.</summary>
		/// <remarks>
		/// Loads a Counter from a text file. File must have the format of one
		/// key/count pair per line, separated by whitespace.
		/// </remarks>
		/// <param name="filename">The path to the file to load the Counter from</param>
		/// <param name="c">
		/// The Class to instantiate each member of the set. Must have a
		/// String constructor.
		/// </param>
		/// <returns>The counter loaded from the file.</returns>
		/// <exception cref="System.Exception"/>
		public static IntCounter<E> LoadIntCounter<E>(string filename)
		{
			System.Type c = typeof(E);
			IntCounter<E> counter = new IntCounter<E>();
			LoadIntoCounter(filename, c, counter);
			return counter;
		}

		/// <summary>Loads a file into an GenericCounter.</summary>
		/// <exception cref="System.Exception"/>
		private static void LoadIntoCounter<E>(string filename, ICounter<E> counter)
		{
			System.Type c = typeof(E);
			try
			{
				Constructor<E> m = c.GetConstructor(typeof(string));
				BufferedReader @in = IOUtils.GetBufferedFileReader(filename);
				for (string line; (line = @in.ReadLine()) != null; )
				{
					string[] tokens = line.Trim().Split("\\s+");
					if (tokens.Length != 2)
					{
						throw new Exception();
					}
					double value = double.ParseDouble(tokens[1]);
					counter.SetCount(m.NewInstance(tokens[0]), value);
				}
				@in.Close();
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		/// <summary>
		/// Saves a Counter as one key/count pair per line separated by white space to
		/// the given OutputStream.
		/// </summary>
		/// <remarks>
		/// Saves a Counter as one key/count pair per line separated by white space to
		/// the given OutputStream. Does not close the stream.
		/// </remarks>
		public static void SaveCounter<E>(ICounter<E> c, OutputStream stream)
		{
			TextWriter @out = new TextWriter(stream);
			foreach (E key in c.KeySet())
			{
				@out.WriteLine(key + " " + c.GetCount(key));
			}
		}

		/// <summary>Saves a Counter to a text file.</summary>
		/// <remarks>
		/// Saves a Counter to a text file. Counter written as one key/count pair per
		/// line, separated by whitespace.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static void SaveCounter<E>(ICounter<E> c, string filename)
		{
			FileOutputStream fos = new FileOutputStream(filename);
			SaveCounter(c, fos);
			fos.Close();
		}

		/// <exception cref="System.Exception"/>
		public static TwoDimensionalCounter<T1, T2> Load2DCounter<T1, T2>(string filename)
		{
			System.Type t1 = typeof(T1);
			System.Type t2 = typeof(T2);
			try
			{
				TwoDimensionalCounter<T1, T2> tdc = new TwoDimensionalCounter<T1, T2>();
				LoadInto2DCounter(filename, t1, t2, tdc);
				return tdc;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		/// <exception cref="System.Exception"/>
		public static void LoadInto2DCounter<T1, T2>(string filename, TwoDimensionalCounter<T1, T2> tdc)
		{
			System.Type t1 = typeof(T1);
			System.Type t2 = typeof(T2);
			try
			{
				Constructor<T1> m1 = t1.GetConstructor(typeof(string));
				Constructor<T2> m2 = t2.GetConstructor(typeof(string));
				BufferedReader @in = IOUtils.GetBufferedFileReader(filename);
				// new
				// BufferedReader(new
				// FileReader(filename));
				for (string line; (line = @in.ReadLine()) != null; )
				{
					string[] tuple = line.Trim().Split("\t");
					string outer = tuple[0];
					string inner = tuple[1];
					string valStr = tuple[2];
					tdc.SetCount(m1.NewInstance(outer.Trim()), m2.NewInstance(inner.Trim()), double.ParseDouble(valStr.Trim()));
				}
				@in.Close();
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		/// <exception cref="System.Exception"/>
		public static void LoadIncInto2DCounter<T1, T2>(string filename, ITwoDimensionalCounterInterface<T1, T2> tdc)
		{
			System.Type t1 = typeof(T1);
			System.Type t2 = typeof(T2);
			try
			{
				Constructor<T1> m1 = t1.GetConstructor(typeof(string));
				Constructor<T2> m2 = t2.GetConstructor(typeof(string));
				BufferedReader @in = IOUtils.GetBufferedFileReader(filename);
				// new
				// BufferedReader(new
				// FileReader(filename));
				for (string line; (line = @in.ReadLine()) != null; )
				{
					string[] tuple = line.Trim().Split("\t");
					string outer = tuple[0];
					string inner = tuple[1];
					string valStr = tuple[2];
					tdc.IncrementCount(m1.NewInstance(outer.Trim()), m2.NewInstance(inner.Trim()), double.ParseDouble(valStr.Trim()));
				}
				@in.Close();
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Save2DCounter<T1, T2>(TwoDimensionalCounter<T1, T2> tdc, string filename)
		{
			PrintWriter @out = new PrintWriter(new FileWriter(filename));
			foreach (T1 outer in tdc.FirstKeySet())
			{
				foreach (T2 inner in tdc.SecondKeySet())
				{
					@out.Println(outer + "\t" + inner + '\t' + tdc.GetCount(outer, inner));
				}
			}
			@out.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Save2DCounterSorted<T1, T2>(ITwoDimensionalCounterInterface<T1, T2> tdc, string filename)
		{
			PrintWriter @out = new PrintWriter(new FileWriter(filename));
			foreach (T1 outer in tdc.FirstKeySet())
			{
				ICounter<T2> c = tdc.GetCounter(outer);
				IList<T2> keys = Edu.Stanford.Nlp.Stats.Counters.ToSortedList(c);
				foreach (T2 inner in keys)
				{
					@out.Println(outer + "\t" + inner + '\t' + c.GetCount(inner));
				}
			}
			@out.Close();
		}

		/// <summary>Serialize a counter into an efficient string TSV</summary>
		/// <param name="c">The counter to serialize</param>
		/// <param name="filename">The file to serialize to</param>
		/// <param name="minMagnitude">Ignore values under this magnitude</param>
		/// <exception cref="System.IO.IOException"/>
		/// <seealso cref="DeserializeStringCounter(string)"/>
		public static void SerializeStringCounter(ICounter<string> c, string filename, double minMagnitude)
		{
			PrintWriter writer = IOUtils.GetPrintWriter(filename);
			foreach (KeyValuePair<string, double> entry in c.EntrySet())
			{
				if (System.Math.Abs(entry.Value) < minMagnitude)
				{
					continue;
				}
				Triple<bool, long, int> parts = SloppyMath.SegmentDouble(entry.Value);
				writer.Println(entry.Key.Replace('\t', 'ߝ') + "\t" + (parts.first ? '-' : '+') + "\t" + parts.second + "\t" + parts.third);
			}
			writer.Close();
		}

		/// <seealso cref="SerializeStringCounter(ICounter{E}, string, double)"></seealso>
		/// <exception cref="System.IO.IOException"/>
		public static void SerializeStringCounter(ICounter<string> c, string filename)
		{
			SerializeStringCounter(c, filename, 0.0);
		}

		/// <summary>Read a Counter from a serialized file</summary>
		/// <param name="filename">The file to read from</param>
		/// <seealso cref="SerializeStringCounter(ICounter{E}, string, double)"/>
		/// <exception cref="System.IO.IOException"/>
		public static ClassicCounter<string> DeserializeStringCounter(string filename)
		{
			string[] fields = new string[4];
			using (BufferedReader reader = IOUtils.ReaderFromString(filename))
			{
				string line;
				ClassicCounter<string> counts = new ClassicCounter<string>(1000000);
				while ((line = reader.ReadLine()) != null)
				{
					StringUtils.SplitOnChar(fields, line, '\t');
					long mantissa = SloppyMath.ParseInt(fields[2]);
					int exponent = (int)SloppyMath.ParseInt(fields[3]);
					double value = SloppyMath.ParseDouble(fields[1].Equals("-"), mantissa, exponent);
					counts.SetCount(fields[0], value);
				}
				return counts;
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static void SerializeCounter<T>(ICounter<T> c, string filename)
		{
			// serialize to file
			ObjectOutputStream @out = new ObjectOutputStream(new BufferedOutputStream(new FileOutputStream(filename)));
			@out.WriteObject(c);
			@out.Close();
		}

		/// <exception cref="System.Exception"/>
		public static ClassicCounter<T> DeserializeCounter<T>(string filename)
		{
			// reconstitute
			ObjectInputStream @in = new ObjectInputStream(new BufferedInputStream(new FileInputStream(filename)));
			ClassicCounter<T> c = ErasureUtils.UncheckedCast(@in.ReadObject());
			@in.Close();
			return c;
		}

		/// <summary>
		/// Returns a string representation of a Counter, displaying the keys and their
		/// counts in decreasing order of count.
		/// </summary>
		/// <remarks>
		/// Returns a string representation of a Counter, displaying the keys and their
		/// counts in decreasing order of count. At most k keys are displayed.
		/// Note that this method subsumes many of the other toString methods, e.g.:
		/// toString(c, k) and toBiggestValuesFirstString(c, k) =&gt; toSortedString(c, k,
		/// "%s=%f", ", ", "[%s]")
		/// toVerticalString(c, k) =&gt; toSortedString(c, k, "%2$g\t%1$s", "\n", "%s\n")
		/// </remarks>
		/// <param name="counter">A Counter.</param>
		/// <param name="k">
		/// The number of keys to include. Use Integer.MAX_VALUE to include
		/// all keys.
		/// </param>
		/// <param name="itemFormat">
		/// The format string for key/count pairs, where the key is first and
		/// the value is second. To display the value first, use argument
		/// indices, e.g. "%2$f %1$s".
		/// </param>
		/// <param name="joiner">The string used between pairs of key/value strings.</param>
		/// <param name="wrapperFormat">
		/// The format string for wrapping text around the joined items, where
		/// the joined item string value is "%s".
		/// </param>
		/// <returns>The top k values from the Counter, formatted as specified.</returns>
		public static string ToSortedString<T>(ICounter<T> counter, int k, string itemFormat, string joiner, string wrapperFormat)
		{
			IPriorityQueue<T> queue = ToPriorityQueue(counter);
			IList<string> strings = new List<string>();
			for (int rank = 0; rank < k && !queue.IsEmpty(); ++rank)
			{
				T key = queue.RemoveFirst();
				double value = counter.GetCount(key);
				strings.Add(string.Format(itemFormat, key, value));
			}
			return string.Format(wrapperFormat, StringUtils.Join(strings, joiner));
		}

		/// <summary>
		/// Returns a string representation of a Counter, displaying the keys and their
		/// counts in decreasing order of count.
		/// </summary>
		/// <remarks>
		/// Returns a string representation of a Counter, displaying the keys and their
		/// counts in decreasing order of count. At most k keys are displayed.
		/// </remarks>
		/// <param name="counter">A Counter.</param>
		/// <param name="k">
		/// The number of keys to include. Use Integer.MAX_VALUE to include
		/// all keys.
		/// </param>
		/// <param name="itemFormat">
		/// The format string for key/count pairs, where the key is first and
		/// the value is second. To display the value first, use argument
		/// indices, e.g. "%2$f %1$s".
		/// </param>
		/// <param name="joiner">The string used between pairs of key/value strings.</param>
		/// <returns>The top k values from the Counter, formatted as specified.</returns>
		public static string ToSortedString<T>(ICounter<T> counter, int k, string itemFormat, string joiner)
		{
			return ToSortedString(counter, k, itemFormat, joiner, "%s");
		}

		/// <summary>
		/// Returns a string representation of a Counter, where (key, value) pairs are
		/// sorted by key, and formatted as specified.
		/// </summary>
		/// <param name="counter">The Counter.</param>
		/// <param name="itemFormat">
		/// The format string for key/count pairs, where the key is first and
		/// the value is second. To display the value first, use argument
		/// indices, e.g. "%2$f %1$s".
		/// </param>
		/// <param name="joiner">The string used between pairs of key/value strings.</param>
		/// <param name="wrapperFormat">
		/// The format string for wrapping text around the joined items, where
		/// the joined item string value is "%s".
		/// </param>
		/// <returns>The Counter, formatted as specified.</returns>
		public static string ToSortedByKeysString<T>(ICounter<T> counter, string itemFormat, string joiner, string wrapperFormat)
			where T : IComparable<T>
		{
			IList<string> strings = new List<string>();
			foreach (T key in CollectionUtils.Sorted(counter.KeySet()))
			{
				strings.Add(string.Format(itemFormat, key, counter.GetCount(key)));
			}
			return string.Format(wrapperFormat, StringUtils.Join(strings, joiner));
		}

		/// <summary>
		/// Returns a string representation which includes no more than the
		/// maxKeysToPrint elements with largest counts.
		/// </summary>
		/// <remarks>
		/// Returns a string representation which includes no more than the
		/// maxKeysToPrint elements with largest counts. If maxKeysToPrint is
		/// non-positive, all elements are printed.
		/// </remarks>
		/// <param name="counter">The Counter</param>
		/// <param name="maxKeysToPrint">Max keys to print</param>
		/// <returns>A partial string representation</returns>
		public static string ToString<E>(ICounter<E> counter, int maxKeysToPrint)
		{
			return Edu.Stanford.Nlp.Stats.Counters.ToPriorityQueue(counter).ToString(maxKeysToPrint);
		}

		public static string ToString<E>(ICounter<E> counter, NumberFormat nf)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append('{');
			IList<E> list = ErasureUtils.SortedIfPossible(counter.KeySet());
			// */
			for (IEnumerator<E> iter = list.GetEnumerator(); iter.MoveNext(); )
			{
				E key = iter.Current;
				sb.Append(key);
				sb.Append('=');
				sb.Append(nf.Format(counter.GetCount(key)));
				if (iter.MoveNext())
				{
					sb.Append(", ");
				}
			}
			sb.Append('}');
			return sb.ToString();
		}

		/// <summary>Pretty print a Counter.</summary>
		/// <remarks>
		/// Pretty print a Counter. This one has more flexibility in formatting, and
		/// doesn't sort the keys.
		/// </remarks>
		public static string ToString<E>(ICounter<E> counter, NumberFormat nf, string preAppend, string postAppend, string keyValSeparator, string itemSeparator)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(preAppend);
			// List<E> list = new ArrayList<E>(map.keySet());
			// try {
			// Collections.sort(list); // see if it can be sorted
			// } catch (Exception e) {
			// }
			for (IEnumerator<E> iter = counter.KeySet().GetEnumerator(); iter.MoveNext(); )
			{
				E key = iter.Current;
				double d = counter.GetCount(key);
				sb.Append(key);
				sb.Append(keyValSeparator);
				sb.Append(nf.Format(d));
				if (iter.MoveNext())
				{
					sb.Append(itemSeparator);
				}
			}
			sb.Append(postAppend);
			return sb.ToString();
		}

		public static string ToBiggestValuesFirstString<E>(ICounter<E> c)
		{
			return ToPriorityQueue(c).ToString();
		}

		// TODO this method seems badly written. It should exploit topK printing of PriorityQueue
		public static string ToBiggestValuesFirstString<E>(ICounter<E> c, int k)
		{
			IPriorityQueue<E> pq = ToPriorityQueue(c);
			IPriorityQueue<E> largestK = new BinaryHeapPriorityQueue<E>();
			// TODO: Is there any reason the original (commented out) line is better
			// than the one replacing it?
			// while (largestK.size() < k && ((Iterator<E>)pq).hasNext()) {
			while (largestK.Count < k && !pq.IsEmpty())
			{
				double firstScore = pq.GetPriority(pq.GetFirst());
				E first = pq.RemoveFirst();
				largestK.ChangePriority(first, firstScore);
			}
			return largestK.ToString();
		}

		public static string ToBiggestValuesFirstString<T>(ICounter<int> c, int k, IIndex<T> index)
		{
			IPriorityQueue<int> pq = ToPriorityQueue(c);
			IPriorityQueue<T> largestK = new BinaryHeapPriorityQueue<T>();
			// while (largestK.size() < k && ((Iterator)pq).hasNext()) { //same as above
			while (largestK.Count < k && !pq.IsEmpty())
			{
				double firstScore = pq.GetPriority(pq.GetFirst());
				int first = pq.RemoveFirst();
				largestK.ChangePriority(index.Get(first), firstScore);
			}
			return largestK.ToString();
		}

		public static string ToVerticalString<E>(ICounter<E> c)
		{
			return ToVerticalString(c, int.MaxValue);
		}

		public static string ToVerticalString<E>(ICounter<E> c, int k)
		{
			return ToVerticalString(c, k, "%g\t%s", false);
		}

		public static string ToVerticalString<E>(ICounter<E> c, string fmt)
		{
			return ToVerticalString(c, int.MaxValue, fmt, false);
		}

		public static string ToVerticalString<E>(ICounter<E> c, int k, string fmt)
		{
			return ToVerticalString(c, k, fmt, false);
		}

		/// <summary>
		/// Returns a
		/// <c>String</c>
		/// representation of the
		/// <paramref name="k"/>
		/// keys
		/// with the largest counts in the given
		/// <see cref="ICounter{E}"/>
		/// , using the given
		/// format string.
		/// </summary>
		/// <param name="c">A Counter</param>
		/// <param name="k">How many keys to print</param>
		/// <param name="fmt">
		/// A format string, such as "%.0f\t%s" (do not include final "%n").
		/// If swap is false, you will get val, key as arguments, if true, key, val.
		/// </param>
		/// <param name="swap">Whether the count should appear after the key</param>
		public static string ToVerticalString<E>(ICounter<E> c, int k, string fmt, bool swap)
		{
			IPriorityQueue<E> q = Edu.Stanford.Nlp.Stats.Counters.ToPriorityQueue(c);
			IList<E> sortedKeys = q.ToSortedList();
			StringBuilder sb = new StringBuilder();
			int i = 0;
			for (IEnumerator<E> keyI = sortedKeys.GetEnumerator(); keyI.MoveNext() && i < k; i++)
			{
				E key = keyI.Current;
				double val = q.GetPriority(key);
				if (swap)
				{
					sb.Append(string.Format(fmt, key, val));
				}
				else
				{
					sb.Append(string.Format(fmt, val, key));
				}
				if (keyI.MoveNext())
				{
					sb.Append('\n');
				}
			}
			return sb.ToString();
		}

		/// <returns>
		/// Returns the maximum element of c that is within the restriction
		/// Collection
		/// </returns>
		public static E RestrictedArgMax<E>(ICounter<E> c, ICollection<E> restriction)
		{
			E maxKey = null;
			double max = double.NegativeInfinity;
			foreach (E key in restriction)
			{
				double count = c.GetCount(key);
				if (count > max)
				{
					max = count;
					maxKey = key;
				}
			}
			return maxKey;
		}

		public static ICounter<T> ToCounter<T>(double[] counts, IIndex<T> index)
		{
			if (index.Size() < counts.Length)
			{
				throw new ArgumentException("Index not large enough to name all the array elements!");
			}
			ICounter<T> c = new ClassicCounter<T>();
			for (int i = 0; i < counts.Length; i++)
			{
				if (counts[i] != 0.0)
				{
					c.SetCount(index.Get(i), counts[i]);
				}
			}
			return c;
		}

		/// <summary>Turns the given map and index into a counter instance.</summary>
		/// <remarks>
		/// Turns the given map and index into a counter instance. For each entry in
		/// counts, its key is converted to a counter key via lookup in the given
		/// index.
		/// </remarks>
		public static ICounter<E> ToCounter<E>(IDictionary<int, Number> counts, IIndex<E> index)
		{
			ICounter<E> counter = new ClassicCounter<E>();
			foreach (KeyValuePair<int, Number> entry in counts)
			{
				counter.SetCount(index.Get(entry.Key), entry.Value);
			}
			return counter;
		}

		/// <summary>Convert a counter to an array using a specified key index.</summary>
		/// <remarks>
		/// Convert a counter to an array using a specified key index. Infer the dimension of
		/// the returned vector from the index.
		/// </remarks>
		public static double[] AsArray<E>(ICounter<E> counter, IIndex<E> index)
		{
			return Edu.Stanford.Nlp.Stats.Counters.AsArray(counter, index, index.Size());
		}

		/// <summary>Convert a counter to an array using a specified key index.</summary>
		/// <remarks>
		/// Convert a counter to an array using a specified key index. This method does *not* expand
		/// the index, so all keys in the set keys(counter) - keys(index) are not added to the
		/// output array. Also note that if counter is being used as a sparse array, the result
		/// will be a dense array with zero entries.
		/// </remarks>
		/// <returns>the values corresponding to the index</returns>
		public static double[] AsArray<E>(ICounter<E> counter, IIndex<E> index, int dimension)
		{
			if (index.Size() == 0)
			{
				throw new ArgumentException("Empty index");
			}
			ICollection<E> keys = counter.KeySet();
			double[] array = new double[dimension];
			foreach (E key in keys)
			{
				int i = index.IndexOf(key);
				if (i >= 0)
				{
					array[i] = counter.GetCount(key);
				}
			}
			return array;
		}

		/// <summary>Convert a counter to an array, the order of the array is random</summary>
		public static double[] AsArray<E>(ICounter<E> counter)
		{
			ICollection<E> keys = counter.KeySet();
			double[] array = new double[counter.Size()];
			int i = 0;
			foreach (E key in keys)
			{
				array[i] = counter.GetCount(key);
				i++;
			}
			return array;
		}

		/// <summary>Creates a new TwoDimensionalCounter where all the counts are scaled by d.</summary>
		/// <remarks>
		/// Creates a new TwoDimensionalCounter where all the counts are scaled by d.
		/// Internally, uses Counters.scale();
		/// </remarks>
		/// <returns>The TwoDimensionalCounter</returns>
		public static TwoDimensionalCounter<T1, T2> Scale<T1, T2>(TwoDimensionalCounter<T1, T2> c, double d)
		{
			TwoDimensionalCounter<T1, T2> result = new TwoDimensionalCounter<T1, T2>(c.GetOuterMapFactory(), c.GetInnerMapFactory());
			foreach (T1 key in c.FirstKeySet())
			{
				ClassicCounter<T2> ctr = c.GetCounter(key);
				result.SetCounter(key, Scale(ctr, d));
			}
			return result;
		}

		internal static readonly Random Rand = new Random();

		/// <summary>Does not assumes c is normalized.</summary>
		/// <returns>A sample from c</returns>
		public static T Sample<T>(ICounter<T> c, Random rand)
		{
			// OMITTED: Seems like there should be a way to directly check if T is comparable
			// Set<T> keySet = c.keySet();
			// if (!keySet.isEmpty() && keySet.iterator().next() instanceof Comparable) {
			//   List l = new ArrayList<T>(keySet);
			//   Collections.sort(l);
			//   objects = l;
			// } else {
			//   throw new RuntimeException("Results won't be stable since Counters keys are comparable.");
			// }
			if (rand == null)
			{
				rand = Rand;
			}
			double r = rand.NextDouble() * c.TotalCount();
			double total = 0.0;
			foreach (T t in c.KeySet())
			{
				// arbitrary ordering, but presumably stable
				total += c.GetCount(t);
				if (total >= r)
				{
					return t;
				}
			}
			// only chance of reaching here is if c isn't properly normalized, or if
			// double math makes total<1.0
			return c.KeySet().GetEnumerator().Current;
		}

		/// <summary>Does not assumes c is normalized.</summary>
		/// <returns>A sample from c</returns>
		public static T Sample<T>(ICounter<T> c)
		{
			return Sample(c, null);
		}

		/// <summary>
		/// Returns a counter where each element corresponds to the normalized count of
		/// the corresponding element in c raised to the given power.
		/// </summary>
		public static ICounter<E> PowNormalized<E>(ICounter<E> c, double temp)
		{
			ICounter<E> d = c.GetFactory().Create();
			double total = c.TotalCount();
			foreach (E e in c.KeySet())
			{
				d.SetCount(e, System.Math.Pow(c.GetCount(e) / total, temp));
			}
			return d;
		}

		public static ICounter<T> Pow<T>(ICounter<T> c, double temp)
		{
			ICounter<T> d = c.GetFactory().Create();
			foreach (T t in c.KeySet())
			{
				d.SetCount(t, System.Math.Pow(c.GetCount(t), temp));
			}
			return d;
		}

		public static void PowInPlace<T>(ICounter<T> c, double temp)
		{
			foreach (T t in c.KeySet())
			{
				c.SetCount(t, System.Math.Pow(c.GetCount(t), temp));
			}
		}

		public static ICounter<T> Exp<T>(ICounter<T> c)
		{
			ICounter<T> d = c.GetFactory().Create();
			foreach (T t in c.KeySet())
			{
				d.SetCount(t, System.Math.Exp(c.GetCount(t)));
			}
			return d;
		}

		public static void ExpInPlace<T>(ICounter<T> c)
		{
			foreach (T t in c.KeySet())
			{
				c.SetCount(t, System.Math.Exp(c.GetCount(t)));
			}
		}

		public static ICounter<T> Diff<T>(ICounter<T> goldFeatures, ICounter<T> guessedFeatures)
		{
			ICounter<T> result = goldFeatures.GetFactory().Create();
			foreach (T key in Sets.Union(goldFeatures.KeySet(), guessedFeatures.KeySet()))
			{
				result.SetCount(key, goldFeatures.GetCount(key) - guessedFeatures.GetCount(key));
			}
			RetainNonZeros(result);
			return result;
		}

		/// <summary>
		/// Default equality comparison for two counters potentially backed by
		/// alternative implementations.
		/// </summary>
		public static bool Equals<E>(ICounter<E> o1, ICounter<E> o2)
		{
			return Equals(o1, o2, 0.0);
		}

		/// <summary>Equality comparison between two counters, allowing for a tolerance fudge factor.</summary>
		public static bool Equals<E>(ICounter<E> o1, ICounter<E> o2, double tolerance)
		{
			if (o1 == o2)
			{
				return true;
			}
			if (System.Math.Abs(o1.TotalCount() - o2.TotalCount()) > tolerance)
			{
				return false;
			}
			if (!o1.KeySet().Equals(o2.KeySet()))
			{
				return false;
			}
			foreach (E key in o1.KeySet())
			{
				if (System.Math.Abs(o1.GetCount(key) - o2.GetCount(key)) > tolerance)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Returns unmodifiable view of the counter.</summary>
		/// <remarks>
		/// Returns unmodifiable view of the counter. changes to the underlying Counter
		/// are written through to this Counter.
		/// </remarks>
		/// <param name="counter">The counter</param>
		/// <returns>unmodifiable view of the counter</returns>
		public static ICounter<T> UnmodifiableCounter<T>(ICounter<T> counter)
		{
			return new _AbstractCounter_2456(this, counter);
		}

		private sealed class _AbstractCounter_2456 : AbstractCounter<T>
		{
			public _AbstractCounter_2456(Counters _enclosing, ICounter<T> counter)
			{
				this._enclosing = _enclosing;
				this.counter = counter;
			}

			public override void Clear()
			{
				throw new NotSupportedException();
			}

			public override bool ContainsKey(T key)
			{
				return counter.ContainsKey(key);
			}

			public override double GetCount(object key)
			{
				return counter.GetCount(key);
			}

			public override IFactory<ICounter<T>> GetFactory()
			{
				return counter.GetFactory();
			}

			public override double Remove(T key)
			{
				throw new NotSupportedException();
			}

			public override void SetCount(T key, double value)
			{
				throw new NotSupportedException();
			}

			public override double IncrementCount(T key, double value)
			{
				throw new NotSupportedException();
			}

			public override double IncrementCount(T key)
			{
				throw new NotSupportedException();
			}

			public override double LogIncrementCount(T key, double value)
			{
				throw new NotSupportedException();
			}

			public override int Size()
			{
				return counter.Size();
			}

			public override double TotalCount()
			{
				return counter.TotalCount();
			}

			public override ICollection<double> Values()
			{
				return counter.Values();
			}

			public override ICollection<T> KeySet()
			{
				return Java.Util.Collections.UnmodifiableSet(counter.KeySet());
			}

			public override ICollection<KeyValuePair<T, double>> EntrySet()
			{
				return Java.Util.Collections.UnmodifiableSet(new _AbstractSet_2514(this, counter));
			}

			private sealed class _AbstractSet_2514 : AbstractSet<KeyValuePair<T, double>>
			{
				public _AbstractSet_2514(_AbstractCounter_2456 _enclosing, ICounter<T> counter)
				{
					this._enclosing = _enclosing;
					this.counter = counter;
				}

				public override IEnumerator<KeyValuePair<T, double>> GetEnumerator()
				{
					return new _IEnumerator_2517(this, counter);
				}

				private sealed class _IEnumerator_2517 : IEnumerator<KeyValuePair<T, double>>
				{
					public _IEnumerator_2517(_AbstractSet_2514 _enclosing, ICounter<T> counter)
					{
						this._enclosing = _enclosing;
						this.counter = counter;
						this.inner = counter.EntrySet().GetEnumerator();
					}

					internal readonly IEnumerator<KeyValuePair<T, double>> inner;

					public bool MoveNext()
					{
						return this.inner.MoveNext();
					}

					public KeyValuePair<T, double> Current
					{
						get
						{
							return new _KeyValuePair_2525(this);
						}
					}

					private sealed class _KeyValuePair_2525 : KeyValuePair<T, double>
					{
						public _KeyValuePair_2525()
						{
							this.e = this._enclosing.inner.Current;
						}

						internal readonly KeyValuePair<T, double> e;

						public T Key
						{
							get
							{
								return this.e.Key;
							}
						}

						public double Value
						{
							get
							{
								return double.ValueOf(this.e.Value);
							}
						}

						public double SetValue(double value)
						{
							throw new NotSupportedException();
						}
					}

					public void Remove()
					{
						throw new NotSupportedException();
					}

					private readonly _AbstractSet_2514 _enclosing;

					private readonly ICounter<T> counter;
				}

				public override int Count
				{
					get
					{
						return counter.Size();
					}
				}

				private readonly _AbstractCounter_2456 _enclosing;

				private readonly ICounter<T> counter;
			}

			public override void SetDefaultReturnValue(double rv)
			{
				throw new NotSupportedException();
			}

			public override double DefaultReturnValue()
			{
				return counter.DefaultReturnValue();
			}

			/// <summary><inheritDoc/></summary>
			public override void PrettyLog(Redwood.RedwoodChannels channels, string description)
			{
				PrettyLogger.Log(channels, description, Edu.Stanford.Nlp.Stats.Counters.AsMap(this));
			}

			private readonly Counters _enclosing;

			private readonly ICounter<T> counter;
		}

		// end unmodifiableCounter()
		/// <summary>
		/// Returns a counter whose keys are the elements in this priority queue, and
		/// whose counts are the priorities in this queue.
		/// </summary>
		/// <remarks>
		/// Returns a counter whose keys are the elements in this priority queue, and
		/// whose counts are the priorities in this queue. In the event there are
		/// multiple instances of the same element in the queue, the counter's count
		/// will be the sum of the instances' priorities.
		/// </remarks>
		public static ICounter<E> AsCounter<E>(FixedPrioritiesPriorityQueue<E> p)
		{
			FixedPrioritiesPriorityQueue<E> pq = p.Clone();
			ClassicCounter<E> counter = new ClassicCounter<E>();
			while (pq.MoveNext())
			{
				double priority = pq.GetPriority();
				E element = pq.Current;
				counter.IncrementCount(element, priority);
			}
			return counter;
		}

		/// <summary>Returns a counter view of the given map.</summary>
		/// <remarks>
		/// Returns a counter view of the given map. Infers the numeric type of the
		/// values from the first element in map.values().
		/// </remarks>
		public static ICounter<E> FromMap<E, N>(IDictionary<E, N> map)
			where N : Number
		{
			if (map.IsEmpty())
			{
				throw new ArgumentException("Map must have at least one element" + " to infer numeric type; add an element first or use e.g." + " fromMap(map, Integer.class)");
			}
			return FromMap(map, (Type)map.Values.GetEnumerator().Current.GetType());
		}

		/// <summary>Returns a counter view of the given map.</summary>
		/// <remarks>
		/// Returns a counter view of the given map. The type parameter is the type of
		/// the values in the map, which because of Java's generics type erasure, can't
		/// be discovered by reflection if the map is currently empty.
		/// </remarks>
		public static ICounter<E> FromMap<E, N>(IDictionary<E, N> map)
			where N : Number
		{
			System.Type type = typeof(N);
			// get our initial total
			double initialTotal = 0.0;
			foreach (KeyValuePair<E, N> entry in map)
			{
				initialTotal += entry.Value;
			}
			// and pass it in to the returned inner class with a final variable
			double initialTotalFinal = initialTotal;
			return new _AbstractCounter_2624(this, initialTotalFinal, map, type);
		}

		private sealed class _AbstractCounter_2624 : AbstractCounter<E>
		{
			public _AbstractCounter_2624(Counters _enclosing, double initialTotalFinal, IDictionary<E, N> map, Type type)
			{
				this._enclosing = _enclosing;
				this.initialTotalFinal = initialTotalFinal;
				this.map = map;
				this.type = type;
				this.total = initialTotalFinal;
				this.defRV = 0.0;
			}

			internal double total;

			internal double defRV;

			public override void Clear()
			{
				map.Clear();
				this.total = 0.0;
			}

			public override bool ContainsKey(E key)
			{
				return map.Contains(key);
			}

			public override void SetDefaultReturnValue(double rv)
			{
				this.defRV = rv;
			}

			public override double DefaultReturnValue()
			{
				return this.defRV;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				else
				{
					if (!(o is ICounter))
					{
						return false;
					}
					else
					{
						return Edu.Stanford.Nlp.Stats.Counters.Equals(this, (ICounter<E>)o);
					}
				}
			}

			public override int GetHashCode()
			{
				return map.GetHashCode();
			}

			public override ICollection<KeyValuePair<E, double>> EntrySet()
			{
				return new _AbstractSet_2667(this, map, type);
			}

			private sealed class _AbstractSet_2667 : AbstractSet<KeyValuePair<E, double>>
			{
				public _AbstractSet_2667(_AbstractCounter_2624 _enclosing, IDictionary<E, N> map, Type type)
				{
					this._enclosing = _enclosing;
					this.map = map;
					this.type = type;
					this.entries = map;
				}

				internal ICollection<KeyValuePair<E, N>> entries;

				public override IEnumerator<KeyValuePair<E, double>> GetEnumerator()
				{
					return new _IEnumerator_2672(this, type);
				}

				private sealed class _IEnumerator_2672 : IEnumerator<KeyValuePair<E, double>>
				{
					public _IEnumerator_2672(_AbstractSet_2667 _enclosing, Type type)
					{
						this._enclosing = _enclosing;
						this.type = type;
						this.it = this._enclosing.entries.GetEnumerator();
					}

					internal IEnumerator<KeyValuePair<E, N>> it;

					internal KeyValuePair<E, N> lastEntry;

					// = null;
					public bool MoveNext()
					{
						return this.it.MoveNext();
					}

					public KeyValuePair<E, double> Current
					{
						get
						{
							KeyValuePair<E, N> entry = this.it.Current;
							this.lastEntry = entry;
							return new _KeyValuePair_2684(this, entry, type);
						}
					}

					private sealed class _KeyValuePair_2684 : KeyValuePair<E, double>
					{
						public _KeyValuePair_2684(_IEnumerator_2672 _enclosing, KeyValuePair<E, N> entry, Type type)
						{
							this._enclosing = _enclosing;
							this.entry = entry;
							this.type = type;
						}

						public E Key
						{
							get
							{
								return entry.Key;
							}
						}

						public double Value
						{
							get
							{
								return entry.Value;
							}
						}

						public double SetValue(double value)
						{
							double lastValue = entry.Value;
							double rv;
							if (type == typeof(double))
							{
								rv = ErasureUtils.UncheckedCast<KeyValuePair<E, double>>(entry).SetValue(value);
							}
							else
							{
								if (type == typeof(int))
								{
									rv = ErasureUtils.UncheckedCast<KeyValuePair<E, int>>(entry).SetValue(value);
								}
								else
								{
									if (type == typeof(float))
									{
										rv = ErasureUtils.UncheckedCast<KeyValuePair<E, float>>(entry).SetValue(value);
									}
									else
									{
										if (type == typeof(long))
										{
											rv = ErasureUtils.UncheckedCast<KeyValuePair<E, long>>(entry).SetValue(value);
										}
										else
										{
											if (type == typeof(short))
											{
												rv = ErasureUtils.UncheckedCast<KeyValuePair<E, short>>(entry).SetValue(value);
											}
											else
											{
												throw new Exception("Unrecognized numeric type in wrapped counter");
											}
										}
									}
								}
							}
							// need to call getValue().doubleValue() to make sure
							// we keep the same precision as the underlying map
							this._enclosing._enclosing._enclosing.total += entry.Value - lastValue;
							return rv;
						}

						private readonly _IEnumerator_2672 _enclosing;

						private readonly KeyValuePair<E, N> entry;

						private readonly Type type;
					}

					public void Remove()
					{
						this._enclosing._enclosing.total -= this.lastEntry.Value;
						this.it.Remove();
					}

					private readonly _AbstractSet_2667 _enclosing;

					private readonly Type type;
				}

				public override int Count
				{
					get
					{
						return map.Count;
					}
				}

				private readonly _AbstractCounter_2624 _enclosing;

				private readonly IDictionary<E, N> map;

				private readonly Type type;
			}

			public override double GetCount(object key)
			{
				Number value = map[key];
				return value != null ? value : this.defRV;
			}

			public override IFactory<ICounter<E>> GetFactory()
			{
				return new _IFactory_2740(type);
			}

			private sealed class _IFactory_2740 : IFactory<ICounter<E>>
			{
				public _IFactory_2740(Type type)
				{
					this.type = type;
					this.serialVersionUID = -4063129407369590522L;
				}

				private const long serialVersionUID;

				public ICounter<E> Create()
				{
					// return a HashMap backed by the same numeric type to
					// keep the precision of the returned counter consistent with
					// this one's precision
					return Edu.Stanford.Nlp.Stats.Counters.FromMap(Generics.NewHashMap<E, N>(), type);
				}

				private readonly Type type;
			}

			public override ICollection<E> KeySet()
			{
				return new _AbstractSet_2754(map);
			}

			private sealed class _AbstractSet_2754 : AbstractSet<E>
			{
				public _AbstractSet_2754(IDictionary<E, N> map)
				{
					this.map = map;
				}

				public override IEnumerator<E> GetEnumerator()
				{
					return new _IEnumerator_2757(map);
				}

				private sealed class _IEnumerator_2757 : IEnumerator<E>
				{
					public _IEnumerator_2757(IDictionary<E, N> map)
					{
						this.map = map;
						this.it = map.Keys.GetEnumerator();
					}

					internal IEnumerator<E> it;

					public bool MoveNext()
					{
						return this.it.MoveNext();
					}

					public E Current
					{
						get
						{
							return this.it.Current;
						}
					}

					public void Remove()
					{
						throw new NotSupportedException("Cannot remove from key set");
					}

					private readonly IDictionary<E, N> map;
				}

				public override int Count
				{
					get
					{
						return map.Count;
					}
				}

				private readonly IDictionary<E, N> map;
			}

			public override double Remove(E key)
			{
				Number removed = Sharpen.Collections.Remove(map, key);
				if (removed != null)
				{
					double rv = removed;
					this.total -= rv;
					return rv;
				}
				return this.defRV;
			}

			public override void SetCount(E key, double value)
			{
				double lastValue;
				double newValue;
				if (type == typeof(double))
				{
					lastValue = ErasureUtils.UncheckedCast<IDictionary<E, double>>(map)[key] = value;
					newValue = value;
				}
				else
				{
					if (type == typeof(int))
					{
						int last = ErasureUtils.UncheckedCast<IDictionary<E, int>>(map)[key] = (int)value;
						lastValue = last != null ? last : null;
						newValue = ((int)value);
					}
					else
					{
						if (type == typeof(float))
						{
							float last = ErasureUtils.UncheckedCast<IDictionary<E, float>>(map)[key] = (float)value;
							lastValue = last != null ? last : null;
							newValue = ((float)value);
						}
						else
						{
							if (type == typeof(long))
							{
								long last = ErasureUtils.UncheckedCast<IDictionary<E, long>>(map)[key] = (long)value;
								lastValue = last != null ? last : null;
								newValue = ((long)value);
							}
							else
							{
								if (type == typeof(short))
								{
									short last = ErasureUtils.UncheckedCast<IDictionary<E, short>>(map)[key] = (short)value;
									lastValue = last != null ? last : null;
									newValue = ((short)value);
								}
								else
								{
									throw new Exception("Unrecognized numeric type in wrapped counter");
								}
							}
						}
					}
				}
				// need to use newValue instead of value to make sure we
				// keep same precision as underlying map.
				this.total += newValue - (lastValue != null ? lastValue : 0);
			}

			public override int Size()
			{
				return map.Count;
			}

			public override double TotalCount()
			{
				return this.total;
			}

			public override ICollection<double> Values()
			{
				return new _AbstractCollection_2832(map);
			}

			private sealed class _AbstractCollection_2832 : AbstractCollection<double>
			{
				public _AbstractCollection_2832(IDictionary<E, N> map)
				{
					this.map = map;
				}

				public override IEnumerator<double> GetEnumerator()
				{
					return new _IEnumerator_2835(map);
				}

				private sealed class _IEnumerator_2835 : IEnumerator<double>
				{
					public _IEnumerator_2835(IDictionary<E, N> map)
					{
						this.map = map;
						this.it = map.Values.GetEnumerator();
					}

					internal readonly IEnumerator<N> it;

					public bool MoveNext()
					{
						return this.it.MoveNext();
					}

					public double Current
					{
						get
						{
							return this.it.Current;
						}
					}

					public void Remove()
					{
						throw new NotSupportedException("Cannot remove from values collection");
					}

					private readonly IDictionary<E, N> map;
				}

				public override int Count
				{
					get
					{
						return map.Count;
					}
				}

				private readonly IDictionary<E, N> map;
			}

			/// <summary><inheritDoc/></summary>
			public override void PrettyLog(Redwood.RedwoodChannels channels, string description)
			{
				PrettyLogger.Log(channels, description, map);
			}

			private readonly Counters _enclosing;

			private readonly double initialTotalFinal;

			private readonly IDictionary<E, N> map;

			private readonly Type type;
		}

		// end fromMap()
		/// <summary>Returns a map view of the given counter.</summary>
		public static IDictionary<E, double> AsMap<E>(ICounter<E> counter)
		{
			return new _AbstractMap_2872(counter);
		}

		private sealed class _AbstractMap_2872 : AbstractMap<E, double>
		{
			public _AbstractMap_2872(ICounter<E> counter)
			{
				this.counter = counter;
			}

			public override int Count
			{
				get
				{
					return counter.Size();
				}
			}

			public override ICollection<KeyValuePair<E, double>> EntrySet()
			{
				return counter.EntrySet();
			}

			public override bool Contains(object key)
			{
				return counter.ContainsKey((E)key);
			}

			public override double Get(object key)
			{
				return counter.GetCount((E)key);
			}

			public override double Put(E key, double value)
			{
				double last = counter.GetCount(key);
				counter.SetCount(key, value);
				return last;
			}

			public override double Remove(object key)
			{
				return counter.Remove((E)key);
			}

			public override ICollection<E> Keys
			{
				get
				{
					return counter.KeySet();
				}
			}

			private readonly ICounter<E> counter;
		}

		/// <summary>Check if this counter is a uniform distribution.</summary>
		/// <remarks>
		/// Check if this counter is a uniform distribution.
		/// That is, it should sum to 1.0, and every value should be equal to every other value.
		/// </remarks>
		/// <param name="distribution">The distribution to check.</param>
		/// <param name="tolerance">The tolerance for floating point error, in both the equality and total count checks.</param>
		/// <?/>
		/// <returns>True if this counter is the uniform distribution over its domain.</returns>
		public static bool IsUniformDistribution<E>(ICounter<E> distribution, double tolerance)
		{
			double value = double.NaN;
			double totalCount = 0.0;
			foreach (double val in distribution.Values())
			{
				if (double.IsNaN(value))
				{
					value = val;
				}
				if (System.Math.Abs(val - value) > tolerance)
				{
					return false;
				}
				totalCount += val;
			}
			return System.Math.Abs(totalCount - 1.0) < tolerance;
		}

		/// <summary>Comparator that uses natural ordering.</summary>
		/// <remarks>Comparator that uses natural ordering. Returns 0 if o1 is not Comparable.</remarks>
		internal class NaturalComparator<E> : IComparator<E>
		{
			public NaturalComparator()
			{
			}

			public override string ToString()
			{
				return "NaturalComparator";
			}

			public virtual int Compare(E o1, E o2)
			{
				if (o1 is IComparable)
				{
					return (((IComparable<E>)o1).CompareTo(o2));
				}
				return 0;
			}
			// soft-fail
		}

		/// <?/>
		/// <param name="originalCounter"/>
		/// <returns>a copy of the original counter</returns>
		public static ICounter<E> GetCopy<E>(ICounter<E> originalCounter)
		{
			ICounter<E> copyCounter = new ClassicCounter<E>();
			copyCounter.AddAll(originalCounter);
			return copyCounter;
		}

		/// <summary>Places the maximum of first and second keys values in the first counter.</summary>
		/// <?/>
		public static void MaxInPlace<E>(ICounter<E> target, ICounter<E> other)
		{
			foreach (E e in CollectionUtils.Union(other.KeySet(), target.KeySet()))
			{
				target.SetCount(e, System.Math.Max(target.GetCount(e), other.GetCount(e)));
			}
		}

		/// <summary>Places the minimum of first and second keys values in the first counter.</summary>
		/// <?/>
		public static void MinInPlace<E>(ICounter<E> target, ICounter<E> other)
		{
			foreach (E e in CollectionUtils.Union(other.KeySet(), target.KeySet()))
			{
				target.SetCount(e, System.Math.Min(target.GetCount(e), other.GetCount(e)));
			}
		}

		/// <summary>Retains the minimal set of top keys such that their count sum is more than thresholdCount.</summary>
		/// <param name="counter"/>
		/// <param name="thresholdCount"/>
		public static void RetainTopMass<E>(ICounter<E> counter, double thresholdCount)
		{
			IPriorityQueue<E> queue = Counters.ToPriorityQueue(counter);
			counter.Clear();
			double mass = 0;
			while (mass < thresholdCount && !queue.IsEmpty())
			{
				double value = queue.GetPriority();
				E key = queue.RemoveFirst();
				counter.SetCount(key, value);
				mass += value;
			}
		}

		public static void DivideInPlace<A, B>(TwoDimensionalCounter<A, B> counter, double divisor)
		{
			foreach (KeyValuePair<A, ClassicCounter<B>> c in counter.EntrySet())
			{
				Counters.DivideInPlace(c.Value, divisor);
			}
			counter.RecomputeTotal();
		}

		public static double PearsonsCorrelationCoefficient<E>(ICounter<E> x, ICounter<E> y)
		{
			double stddevX = Counters.StandardDeviation(x);
			double stddevY = Counters.StandardDeviation(y);
			double meanX = Counters.Mean(x);
			double meanY = Counters.Mean(y);
			ICounter<E> t1 = Counters.Add(x, -meanX);
			ICounter<E> t2 = Counters.Add(y, -meanY);
			Counters.DivideInPlace(t1, stddevX);
			Counters.DivideInPlace(t2, stddevY);
			return Counters.DotProduct(t1, t2) / (double)(x.Size() - 1);
		}

		public static double SpearmanRankCorrelation<E>(ICounter<E> x, ICounter<E> y)
		{
			ICounter<E> xrank = Counters.ToTiedRankCounter(x);
			ICounter<E> yrank = Counters.ToTiedRankCounter(y);
			return Counters.PearsonsCorrelationCoefficient(xrank, yrank);
		}

		/// <summary>ensures that counter t has all keys in keys.</summary>
		/// <remarks>
		/// ensures that counter t has all keys in keys. If the counter does not have the keys, then add the key with count value.
		/// Note that it does not change counts that exist in the counter
		/// </remarks>
		public static void EnsureKeys<E>(ICounter<E> t, ICollection<E> keys, double value)
		{
			foreach (E k in keys)
			{
				if (!t.ContainsKey(k))
				{
					t.SetCount(k, value);
				}
			}
		}

		public static IList<E> TopKeys<E>(ICounter<E> t, int topNum)
		{
			IList<E> list = new List<E>();
			IPriorityQueue<E> q = Counters.ToPriorityQueue(t);
			int num = 0;
			while (!q.IsEmpty() && num < topNum)
			{
				num++;
				list.Add(q.RemoveFirst());
			}
			return list;
		}

		public static IList<Pair<E, double>> TopKeysWithCounts<E>(ICounter<E> t, int topNum)
		{
			IList<Pair<E, double>> list = new List<Pair<E, double>>();
			IPriorityQueue<E> q = Counters.ToPriorityQueue(t);
			int num = 0;
			while (!q.IsEmpty() && num < topNum)
			{
				num++;
				E k = q.RemoveFirst();
				list.Add(new Pair<E, double>(k, t.GetCount(k)));
			}
			return list;
		}

		public static ICounter<E> GetFCounter<E>(ICounter<E> precision, ICounter<E> recall, double beta)
		{
			ICounter<E> fscores = new ClassicCounter<E>();
			foreach (E k in precision.KeySet())
			{
				fscores.SetCount(k, precision.GetCount(k) * recall.GetCount(k) * (1 + beta * beta) / (beta * beta * precision.GetCount(k) + recall.GetCount(k)));
			}
			return fscores;
		}

		public static void TransformValuesInPlace<E>(ICounter<E> counter, IDoubleUnaryOperator func)
		{
			foreach (E key in counter.KeySet())
			{
				counter.SetCount(key, func.ApplyAsDouble(counter.GetCount(key)));
			}
		}

		public static ICounter<E> GetCounts<E>(ICounter<E> c, ICollection<E> keys)
		{
			ICounter<E> newcounter = new ClassicCounter<E>();
			foreach (E k in keys)
			{
				newcounter.SetCount(k, c.GetCount(k));
			}
			return newcounter;
		}

		public static void RetainKeys<E>(ICounter<E> counter, IPredicate<E> retainFunction)
		{
			ICollection<E> remove = new HashSet<E>();
			foreach (KeyValuePair<E, double> en in counter.EntrySet())
			{
				if (!retainFunction.Test(en.Key))
				{
					remove.Add(en.Key);
				}
			}
			Counters.RemoveKeys(counter, remove);
		}

		public static ICounter<E> Flatten<E, E2>(IDictionary<E2, ICounter<E>> hier)
		{
			ICounter<E> flat = new ClassicCounter<E>();
			foreach (KeyValuePair<E2, ICounter<E>> en in hier)
			{
				flat.AddAll(en.Value);
			}
			return flat;
		}

		/// <summary>Returns true if the given counter contains only finite, non-NaN values.</summary>
		/// <param name="counts">The counter to validate.</param>
		/// <?/>
		/// <returns>True if the counter is finite and not NaN on every value.</returns>
		public static bool IsFinite<E>(ICounter<E> counts)
		{
			foreach (double value in counts.Values())
			{
				if (double.IsInfinite(value) || double.IsNaN(value))
				{
					return false;
				}
			}
			return true;
		}
	}
}
