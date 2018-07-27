using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// A specialized kind of hash table (or map) for storing numeric counts for
	/// objects.
	/// </summary>
	/// <remarks>
	/// A specialized kind of hash table (or map) for storing numeric counts for
	/// objects. It works like a Map,
	/// but with different methods for easily getting/setting/incrementing counts
	/// for objects and computing various functions with the counts.
	/// The Counter constructor
	/// and
	/// <c>addAll</c>
	/// method can be used to copy another Counter's contents
	/// over. This class also provides access
	/// to Comparators that can be used to sort the keys or entries of this Counter
	/// by the counts, in either ascending or descending order.
	/// </remarks>
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	/// <author>Joseph Smarr (jsmarr@stanford.edu)</author>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	/// <author>Galen Andrew</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class IntCounter<E> : AbstractCounter<E>
	{
		private IDictionary<E, MutableInteger> map;

		private MapFactory mapFactory;

		private int totalCount;

		private int defaultValue;

		/// <summary>Default comparator for breaking ties in argmin and argmax.</summary>
		private static readonly IComparator<object> naturalComparator = new IntCounter.NaturalComparator<object>();

		private const long serialVersionUID = 4;

		/// <summary>Constructs a new (empty) Counter.</summary>
		public IntCounter()
			: this(MapFactory.HashMapFactory<E, MutableInteger>())
		{
		}

		/// <summary>Pass in a MapFactory and the map it vends will back your counter.</summary>
		public IntCounter(MapFactory<E, MutableInteger> mapFactory)
		{
			// = 0;
			// CONSTRUCTORS
			this.mapFactory = mapFactory;
			map = mapFactory.NewMap();
			totalCount = 0;
		}

		/// <summary>Constructs a new Counter with the contents of the given Counter.</summary>
		public IntCounter(Edu.Stanford.Nlp.Stats.IntCounter<E> c)
			: this()
		{
			AddAll(c);
		}

		// STANDARD ACCESS MODIFICATION METHODS
		public virtual MapFactory<E, MutableInteger> GetMapFactory()
		{
			return ErasureUtils.UncheckedCast<MapFactory<E, MutableInteger>>(mapFactory);
		}

		public override void SetDefaultReturnValue(double rv)
		{
			defaultValue = (int)rv;
		}

		public virtual void SetDefaultReturnValue(int rv)
		{
			defaultValue = rv;
		}

		public override double DefaultReturnValue()
		{
			return defaultValue;
		}

		/// <summary>Returns the current total count for all objects in this Counter.</summary>
		/// <remarks>
		/// Returns the current total count for all objects in this Counter.
		/// All counts are summed each time, so cache it if you need it repeatedly.
		/// </remarks>
		/// <returns>The current total count for all objects in this Counter.</returns>
		public virtual int TotalIntCount()
		{
			return totalCount;
		}

		public virtual double TotalDoubleCount()
		{
			return totalCount;
		}

		/// <summary>
		/// Returns the total count for all objects in this Counter that pass the
		/// given Filter.
		/// </summary>
		/// <remarks>
		/// Returns the total count for all objects in this Counter that pass the
		/// given Filter. Passing in a filter that always returns true is equivalent
		/// to calling
		/// <see cref="IntCounter{E}.TotalCount()"/>
		/// .
		/// </remarks>
		public virtual int TotalIntCount(IPredicate<E> filter)
		{
			int total = 0;
			foreach (E key in map.Keys)
			{
				if (filter.Test(key))
				{
					total += GetIntCount(key);
				}
			}
			return (total);
		}

		public virtual double TotalDoubleCount(IPredicate<E> filter)
		{
			return TotalIntCount(filter);
		}

		public virtual double TotalCount(IPredicate<E> filter)
		{
			return TotalDoubleCount(filter);
		}

		/// <summary>Returns the mean of all the counts (totalCount/size).</summary>
		public virtual double AverageCount()
		{
			return TotalCount() / map.Count;
		}

		/// <summary>
		/// Returns the current count for the given key, which is 0 if it hasn't
		/// been
		/// seen before.
		/// </summary>
		/// <remarks>
		/// Returns the current count for the given key, which is 0 if it hasn't
		/// been
		/// seen before. This is a convenient version of
		/// <c>get</c>
		/// that casts
		/// and extracts the primitive value.
		/// </remarks>
		public override double GetCount(object key)
		{
			return GetIntCount(key);
		}

		public virtual string GetCountAsString(E key)
		{
			return int.ToString(GetIntCount(key));
		}

		/// <summary>
		/// Returns the current count for the given key, which is 0 if it hasn't
		/// been
		/// seen before.
		/// </summary>
		/// <remarks>
		/// Returns the current count for the given key, which is 0 if it hasn't
		/// been
		/// seen before. This is a convenient version of
		/// <c>get</c>
		/// that casts
		/// and extracts the primitive value.
		/// </remarks>
		public virtual int GetIntCount(object key)
		{
			MutableInteger count = map[key];
			if (count == null)
			{
				return defaultValue;
			}
			// haven't seen this object before -> 0 count
			return count;
		}

		/// <summary>
		/// This has been de-deprecated in order to reduce compilation warnings, but
		/// really you should create a
		/// <see cref="Distribution{E}"/>
		/// instead of using this method.
		/// </summary>
		public virtual double GetNormalizedCount(E key)
		{
			return GetCount(key) / (TotalCount());
		}

		/// <summary>Sets the current count for the given key.</summary>
		/// <remarks>
		/// Sets the current count for the given key. This will wipe out any existing
		/// count for that key.
		/// <p>
		/// To add to a count instead of replacing it, use
		/// <see cref="IntCounter{E}.IncrementCount(object, int)"/>
		/// .
		/// </remarks>
		public virtual void SetCount(E key, int count)
		{
			if (tempMInteger == null)
			{
				tempMInteger = new MutableInteger();
			}
			tempMInteger.Set(count);
			tempMInteger = map[key] = tempMInteger;
			totalCount += count;
			if (tempMInteger != null)
			{
				totalCount -= tempMInteger;
			}
		}

		public virtual void SetCount(E key, string s)
		{
			SetCount(key, System.Convert.ToInt32(s));
		}

		[System.NonSerialized]
		private MutableInteger tempMInteger = null;

		// for more efficient memory usage
		/// <summary>Sets the current count for each of the given keys.</summary>
		/// <remarks>
		/// Sets the current count for each of the given keys. This will wipe out
		/// any existing counts for these keys.
		/// <p>
		/// To add to the counts of a collection of objects instead of replacing them,
		/// use
		/// <see cref="IntCounter{E}.IncrementCounts(System.Collections.ICollection{E}, int)"/>
		/// .
		/// </remarks>
		public virtual void SetCounts(ICollection<E> keys, int count)
		{
			foreach (E key in keys)
			{
				SetCount(key, count);
			}
		}

		/// <summary>Adds the given count to the current count for the given key.</summary>
		/// <remarks>
		/// Adds the given count to the current count for the given key. If the key
		/// hasn't been seen before, it is assumed to have count 0, and thus this
		/// method will set its count to the given amount. Negative increments are
		/// equivalent to calling
		/// <c>decrementCount</c>
		/// .
		/// <p>
		/// To more conveniently increment the count by 1, use
		/// <see cref="IntCounter{E}.IncrementCount(object)"/>
		/// .
		/// <p>
		/// To set a count to a specific value instead of incrementing it, use
		/// <see cref="IntCounter{E}.SetCount(object, int)"/>
		/// .
		/// </remarks>
		public virtual int IncrementCount(E key, int count)
		{
			if (tempMInteger == null)
			{
				tempMInteger = new MutableInteger();
			}
			MutableInteger oldMInteger = map[key] = tempMInteger;
			totalCount += count;
			if (oldMInteger != null)
			{
				count += oldMInteger;
			}
			tempMInteger.Set(count);
			tempMInteger = oldMInteger;
			return count;
		}

		/// <summary>Adds 1 to the count for the given key.</summary>
		/// <remarks>
		/// Adds 1 to the count for the given key. If the key hasn't been seen
		/// before, it is assumed to have count 0, and thus this method will set
		/// its count to 1.
		/// <p>
		/// To increment the count by a value other than 1, use
		/// <see cref="IntCounter{E}.IncrementCount(object, int)"/>
		/// .
		/// <p>
		/// To set a count to a specific value instead of incrementing it, use
		/// <see cref="IntCounter{E}.SetCount(object, int)"/>
		/// .
		/// </remarks>
		public override double IncrementCount(E key)
		{
			return IncrementCount(key, 1);
		}

		/// <summary>Adds the given count to the current counts for each of the given keys.</summary>
		/// <remarks>
		/// Adds the given count to the current counts for each of the given keys.
		/// If any of the keys haven't been seen before, they are assumed to have
		/// count 0, and thus this method will set their counts to the given
		/// amount. Negative increments are equivalent to calling
		/// <c>decrementCounts</c>
		/// .
		/// <p>
		/// To more conveniently increment the counts of a collection of objects by
		/// 1, use
		/// <see cref="IntCounter{E}.IncrementCounts(System.Collections.ICollection{E})"/>
		/// .
		/// <p>
		/// To set the counts of a collection of objects to a specific value instead
		/// of incrementing them, use
		/// <see cref="IntCounter{E}.SetCounts(System.Collections.ICollection{E}, int)"/>
		/// .
		/// </remarks>
		public virtual void IncrementCounts(ICollection<E> keys, int count)
		{
			foreach (E key in keys)
			{
				IncrementCount(key, count);
			}
		}

		/// <summary>Adds 1 to the counts for each of the given keys.</summary>
		/// <remarks>
		/// Adds 1 to the counts for each of the given keys. If any of the keys
		/// haven't been seen before, they are assumed to have count 0, and thus
		/// this method will set their counts to 1.
		/// <p>
		/// To increment the counts of a collection of object by a value other
		/// than 1, use
		/// <see cref="IntCounter{E}.IncrementCounts(System.Collections.ICollection{E}, int)"/>
		/// .
		/// <p>
		/// To set the counts of a collection of objects  to a specific value instead
		/// of incrementing them, use
		/// <see cref="IntCounter{E}.SetCounts(System.Collections.ICollection{E}, int)"/>
		/// .
		/// </remarks>
		public virtual void IncrementCounts(ICollection<E> keys)
		{
			IncrementCounts(keys, 1);
		}

		/// <summary>Subtracts the given count from the current count for the given key.</summary>
		/// <remarks>
		/// Subtracts the given count from the current count for the given key.
		/// If the key hasn't been seen before, it is assumed to have count 0, and
		/// thus this  method will set its count to the negative of the given amount.
		/// Negative increments are equivalent to calling
		/// <c>incrementCount</c>
		/// .
		/// <p>
		/// To more conveniently decrement the count by 1, use
		/// <see cref="IntCounter{E}.DecrementCount(object)"/>
		/// .
		/// <p>
		/// To set a count to a specifc value instead of decrementing it, use
		/// <see cref="IntCounter{E}.SetCount(object, int)"/>
		/// .
		/// </remarks>
		public virtual int DecrementCount(E key, int count)
		{
			return IncrementCount(key, -count);
		}

		/// <summary>Subtracts 1 from the count for the given key.</summary>
		/// <remarks>
		/// Subtracts 1 from the count for the given key. If the key hasn't been
		/// seen  before, it is assumed to have count 0, and thus this method will
		/// set its count to -1.
		/// <p>
		/// To decrement the count by a value other than 1, use
		/// <see cref="IntCounter{E}.DecrementCount(object, int)"/>
		/// .
		/// <p>
		/// To set a count to a specifc value instead of decrementing it, use
		/// <see cref="IntCounter{E}.SetCount(object, int)"/>
		/// .
		/// </remarks>
		public override double DecrementCount(E key)
		{
			return DecrementCount(key, 1);
		}

		/// <summary>Subtracts the given count from the current counts for each of the given keys.</summary>
		/// <remarks>
		/// Subtracts the given count from the current counts for each of the given keys.
		/// If any of the keys haven't been seen before, they are assumed to have
		/// count 0, and thus this method will set their counts to the negative of the given
		/// amount. Negative increments are equivalent to calling
		/// <c>incrementCount</c>
		/// .
		/// <p>
		/// To more conveniently decrement the counts of a collection of objects by
		/// 1, use
		/// <see cref="IntCounter{E}.DecrementCounts(System.Collections.ICollection{E})"/>
		/// .
		/// <p>
		/// To set the counts of a collection of objects to a specific value instead
		/// of decrementing them, use
		/// <see cref="IntCounter{E}.SetCounts(System.Collections.ICollection{E}, int)"/>
		/// .
		/// </remarks>
		public virtual void DecrementCounts(ICollection<E> keys, int count)
		{
			IncrementCounts(keys, -count);
		}

		/// <summary>Subtracts 1 from the counts of each of the given keys.</summary>
		/// <remarks>
		/// Subtracts 1 from the counts of each of the given keys. If any of the keys
		/// haven't been seen before, they are assumed to have count 0, and thus
		/// this method will set their counts to -1.
		/// <p>
		/// To decrement the counts of a collection of object by a value other
		/// than 1, use
		/// <see cref="IntCounter{E}.DecrementCounts(System.Collections.ICollection{E}, int)"/>
		/// .
		/// <p>
		/// To set the counts of a collection of objects  to a specifc value instead
		/// of decrementing them, use
		/// <see cref="IntCounter{E}.SetCounts(System.Collections.ICollection{E}, int)"/>
		/// .
		/// </remarks>
		public virtual void DecrementCounts(ICollection<E> keys)
		{
			DecrementCounts(keys, 1);
		}

		/// <summary>Adds the counts in the given Counter to the counts in this Counter.</summary>
		/// <remarks>
		/// Adds the counts in the given Counter to the counts in this Counter.
		/// <p>
		/// To copy the values from another Counter rather than adding them, use
		/// </remarks>
		public virtual void AddAll(Edu.Stanford.Nlp.Stats.IntCounter<E> counter)
		{
			foreach (E key in counter.KeySet())
			{
				int count = counter.GetIntCount(key);
				IncrementCount(key, count);
			}
		}

		/// <summary>Subtracts the counts in the given Counter from the counts in this Counter.</summary>
		/// <remarks>
		/// Subtracts the counts in the given Counter from the counts in this Counter.
		/// <p>
		/// To copy the values from another Counter rather than subtracting them, use
		/// </remarks>
		public virtual void SubtractAll(Edu.Stanford.Nlp.Stats.IntCounter<E> counter)
		{
			foreach (E key in map.Keys)
			{
				DecrementCount(key, counter.GetIntCount(key));
			}
		}

		// MAP LIKE OPERATIONS
		public override bool ContainsKey(E key)
		{
			return map.Contains(key);
		}

		/// <summary>Removes the given key from this Counter.</summary>
		/// <remarks>
		/// Removes the given key from this Counter. Its count will now be 0 and it
		/// will no longer be considered previously seen.
		/// </remarks>
		public override double Remove(E key)
		{
			totalCount -= GetCount(key);
			// subtract removed count from total (may be 0)
			MutableInteger val = Sharpen.Collections.Remove(map, key);
			if (val == null)
			{
				return double.NaN;
			}
			else
			{
				return val;
			}
		}

		/// <summary>Removes all the given keys from this Counter.</summary>
		public virtual void RemoveAll(ICollection<E> c)
		{
			foreach (E key in c)
			{
				Remove(key);
			}
		}

		/// <summary>Removes all counts from this Counter.</summary>
		public override void Clear()
		{
			map.Clear();
			totalCount = 0;
		}

		public override int Size()
		{
			return map.Count;
		}

		public virtual bool IsEmpty()
		{
			return Size() == 0;
		}

		public override ICollection<E> KeySet()
		{
			return map.Keys;
		}

		/// <summary>Returns a view of the doubles in this map.</summary>
		/// <remarks>Returns a view of the doubles in this map.  Can be safely modified.</remarks>
		public override ICollection<KeyValuePair<E, double>> EntrySet()
		{
			return new _AbstractSet_436(this);
		}

		private sealed class _AbstractSet_436 : AbstractSet<KeyValuePair<E, double>>
		{
			public _AbstractSet_436(IntCounter<E> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override IEnumerator<KeyValuePair<E, double>> GetEnumerator()
			{
				return new _IEnumerator_439(this);
			}

			private sealed class _IEnumerator_439 : IEnumerator<KeyValuePair<E, double>>
			{
				public _IEnumerator_439(_AbstractSet_436 _enclosing)
				{
					this._enclosing = _enclosing;
					this.inner = this._enclosing._enclosing.map.GetEnumerator();
				}

				internal readonly IEnumerator<KeyValuePair<E, MutableInteger>> inner;

				public bool MoveNext()
				{
					return this.inner.MoveNext();
				}

				public KeyValuePair<E, double> Current
				{
					get
					{
						return new _KeyValuePair_447(this);
					}
				}

				private sealed class _KeyValuePair_447 : KeyValuePair<E, double>
				{
					public _KeyValuePair_447(_IEnumerator_439 _enclosing)
					{
						this._enclosing = _enclosing;
						this.e = this._enclosing.inner.Current;
					}

					internal readonly KeyValuePair<E, MutableInteger> e;

					public E Key
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
							return this.e.Value;
						}
					}

					public double SetValue(double value)
					{
						double old = this.e.Value;
						this.e.Value.Set(value);
						this._enclosing._enclosing._enclosing.totalCount = this._enclosing._enclosing._enclosing.totalCount - (int)old + value;
						return old;
					}

					private readonly _IEnumerator_439 _enclosing;
				}

				public void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly _AbstractSet_436 _enclosing;
			}

			public override int Count
			{
				get
				{
					return this._enclosing.map.Count;
				}
			}

			private readonly IntCounter<E> _enclosing;
		}

		// OBJECT STUFF
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Stats.IntCounter))
			{
				return false;
			}
			Edu.Stanford.Nlp.Stats.IntCounter counter = (Edu.Stanford.Nlp.Stats.IntCounter)o;
			return map.Equals(counter.map);
		}

		public override int GetHashCode()
		{
			return map.GetHashCode();
		}

		public override string ToString()
		{
			return map.ToString();
		}

		public virtual string ToString(NumberFormat nf, string preAppend, string postAppend, string keyValSeparator, string itemSeparator)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(preAppend);
			IList<E> list = new List<E>(map.Keys);
			try
			{
				(IList)list.Sort();
			}
			catch (Exception)
			{
			}
			// see if it can be sorted
			for (IEnumerator<E> iter = list.GetEnumerator(); iter.MoveNext(); )
			{
				object key = iter.Current;
				MutableInteger d = map[key];
				sb.Append(key + keyValSeparator);
				sb.Append(nf.Format(d));
				if (iter.MoveNext())
				{
					sb.Append(itemSeparator);
				}
			}
			sb.Append(postAppend);
			return sb.ToString();
		}

		public virtual string ToString(NumberFormat nf)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			IList<E> list = new List<E>(map.Keys);
			try
			{
				(IList)list.Sort();
			}
			catch (Exception)
			{
			}
			// see if it can be sorted
			for (IEnumerator<E> iter = list.GetEnumerator(); iter.MoveNext(); )
			{
				object key = iter.Current;
				MutableInteger d = map[key];
				sb.Append(key + "=");
				sb.Append(nf.Format(d));
				if (iter.MoveNext())
				{
					sb.Append(", ");
				}
			}
			sb.Append("}");
			return sb.ToString();
		}

		public virtual object Clone()
		{
			return new Edu.Stanford.Nlp.Stats.IntCounter<E>(this);
		}

		// EXTRA CALCULATION METHODS
		/// <summary>Removes all keys whose count is 0.</summary>
		/// <remarks>
		/// Removes all keys whose count is 0. After incrementing and decrementing
		/// counts or adding and subtracting Counters, there may be keys left whose
		/// count is 0, though normally this is undesirable. This method cleans up
		/// the map.
		/// <p>
		/// Maybe in the future we should try to do this more on-the-fly, though it's
		/// not clear whether a distinction should be made between "never seen" (i.e.
		/// null count) and "seen with 0 count". Certainly there's no distinction in
		/// getCount() but there is in containsKey().
		/// </remarks>
		public virtual void RemoveZeroCounts()
		{
			map.Keys.RemoveIf(null);
		}

		/// <summary>Finds and returns the largest count in this Counter.</summary>
		public virtual int Max()
		{
			int max = int.MinValue;
			foreach (E key in map.Keys)
			{
				max = Math.Max(max, GetIntCount(key));
			}
			return max;
		}

		public virtual double DoubleMax()
		{
			return Max();
		}

		/// <summary>Finds and returns the smallest count in this Counter.</summary>
		public virtual int Min()
		{
			int min = int.MaxValue;
			foreach (E key in map.Keys)
			{
				min = Math.Min(min, GetIntCount(key));
			}
			return min;
		}

		/// <summary>Finds and returns the key in this Counter with the largest count.</summary>
		/// <remarks>
		/// Finds and returns the key in this Counter with the largest count.
		/// Ties are broken by comparing the objects using the given tie breaking
		/// Comparator, favoring Objects that are sorted to the front. This is useful
		/// if the keys are numeric and there is a bias to prefer smaller or larger
		/// values, and can be useful in other circumstances where random tie-breaking
		/// is not desirable. Returns null if this Counter is empty.
		/// </remarks>
		public virtual E Argmax(IComparator<E> tieBreaker)
		{
			int max = int.MinValue;
			E argmax = null;
			foreach (E key in KeySet())
			{
				int count = GetIntCount(key);
				if (argmax == null || count > max || (count == max && tieBreaker.Compare(key, argmax) < 0))
				{
					max = count;
					argmax = key;
				}
			}
			return argmax;
		}

		/// <summary>Finds and returns the key in this Counter with the largest count.</summary>
		/// <remarks>
		/// Finds and returns the key in this Counter with the largest count.
		/// Ties are broken according to the natural ordering of the objects.
		/// This will prefer smaller numeric keys and lexicographically earlier
		/// String keys. To use a different tie-breaking Comparator, use
		/// <see cref="IntCounter{E}.Argmax(System.Collections.IComparer{T})"/>
		/// . Returns null if this Counter is empty.
		/// </remarks>
		public virtual E Argmax()
		{
			return Argmax(ErasureUtils.UncheckedCast<IComparator<E>>(naturalComparator));
		}

		/// <summary>Finds and returns the key in this Counter with the smallest count.</summary>
		/// <remarks>
		/// Finds and returns the key in this Counter with the smallest count.
		/// Ties are broken by comparing the objects using the given tie breaking
		/// Comparator, favoring Objects that are sorted to the front. This is useful
		/// if the keys are numeric and there is a bias to prefer smaller or larger
		/// values, and can be useful in other circumstances where random tie-breaking
		/// is not desirable. Returns null if this Counter is empty.
		/// </remarks>
		public virtual E Argmin(IComparator<E> tieBreaker)
		{
			int min = int.MaxValue;
			E argmin = null;
			foreach (E key in map.Keys)
			{
				int count = GetIntCount(key);
				if (argmin == null || count < min || (count == min && tieBreaker.Compare(key, argmin) < 0))
				{
					min = count;
					argmin = key;
				}
			}
			return argmin;
		}

		/// <summary>Finds and returns the key in this Counter with the smallest count.</summary>
		/// <remarks>
		/// Finds and returns the key in this Counter with the smallest count.
		/// Ties are broken according to the natural ordering of the objects.
		/// This will prefer smaller numeric keys and lexicographically earlier
		/// String keys. To use a different tie-breaking Comparator, use
		/// <see cref="IntCounter{E}.Argmin(System.Collections.IComparer{T})"/>
		/// . Returns null if this Counter is empty.
		/// </remarks>
		public virtual E Argmin()
		{
			return Argmin(ErasureUtils.UncheckedCast<IComparator<E>>(naturalComparator));
		}

		/// <summary>Returns the set of keys whose counts are at or above the given threshold.</summary>
		/// <remarks>
		/// Returns the set of keys whose counts are at or above the given threshold.
		/// This set may have 0 elements but will not be null.
		/// </remarks>
		public virtual ICollection<E> KeysAbove(int countThreshold)
		{
			ICollection<E> keys = Generics.NewHashSet();
			foreach (E key in map.Keys)
			{
				if (GetIntCount(key) >= countThreshold)
				{
					keys.Add(key);
				}
			}
			return keys;
		}

		/// <summary>Returns the set of keys whose counts are at or below the given threshold.</summary>
		/// <remarks>
		/// Returns the set of keys whose counts are at or below the given threshold.
		/// This set may have 0 elements but will not be null.
		/// </remarks>
		public virtual ICollection<E> KeysBelow(int countThreshold)
		{
			ICollection<E> keys = Generics.NewHashSet();
			foreach (E key in map.Keys)
			{
				if (GetIntCount(key) <= countThreshold)
				{
					keys.Add(key);
				}
			}
			return keys;
		}

		/// <summary>Returns the set of keys that have exactly the given count.</summary>
		/// <remarks>
		/// Returns the set of keys that have exactly the given count.
		/// This set may have 0 elements but will not be null.
		/// </remarks>
		public virtual ICollection<E> KeysAt(int count)
		{
			ICollection<E> keys = Generics.NewHashSet();
			foreach (E key in map.Keys)
			{
				if (GetIntCount(key) == count)
				{
					keys.Add(key);
				}
			}
			return keys;
		}

		/// <summary>Comparator that uses natural ordering.</summary>
		/// <remarks>
		/// Comparator that uses natural ordering.
		/// Returns 0 if o1 is not Comparable.
		/// </remarks>
		private class NaturalComparator<T> : IComparator<T>
		{
			public virtual int Compare(T o1, T o2)
			{
				if (o1 is IComparable)
				{
					return ErasureUtils.UncheckedCast<IComparable<T>>(o1).CompareTo(o2);
				}
				return 0;
			}
			// soft-fail
		}

		//
		// For compatibilty with the Counter interface
		//
		public override IFactory<ICounter<E>> GetFactory()
		{
			return new _IFactory_726(this);
		}

		private sealed class _IFactory_726 : IFactory<ICounter<E>>
		{
			private const long serialVersionUID = 7470763055803428477L;

			public _IFactory_726(IntCounter<E> _enclosing)
			{
				this._enclosing = _enclosing;
				this.serialVersionUID = serialVersionUID;
			}

			public ICounter<E> Create()
			{
				return new IntCounter<E>(this._enclosing.GetMapFactory());
			}

			private readonly IntCounter<E> _enclosing;
		}

		public override void SetCount(E key, double value)
		{
			SetCount(key, (int)value);
		}

		public override double IncrementCount(E key, double value)
		{
			IncrementCount(key, (int)value);
			return GetCount(key);
		}

		public override double TotalCount()
		{
			return TotalDoubleCount();
		}

		public override ICollection<double> Values()
		{
			return new _AbstractCollection_750(this);
		}

		private sealed class _AbstractCollection_750 : AbstractCollection<double>
		{
			public _AbstractCollection_750(IntCounter<E> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override IEnumerator<double> GetEnumerator()
			{
				return new _IEnumerator_753(this);
			}

			private sealed class _IEnumerator_753 : IEnumerator<double>
			{
				public _IEnumerator_753()
				{
					this.inner = this._enclosing._enclosing.map.Values.GetEnumerator();
				}

				internal IEnumerator<MutableInteger> inner;

				public bool MoveNext()
				{
					return this.inner.MoveNext();
				}

				public double Current
				{
					get
					{
						return this.inner.Current;
					}
				}

				public void Remove()
				{
					throw new NotSupportedException();
				}
			}

			public override int Count
			{
				get
				{
					return this._enclosing.map.Count;
				}
			}

			private readonly IntCounter<E> _enclosing;
		}

		public virtual IEnumerator<E> Iterator()
		{
			return KeySet().GetEnumerator();
		}

		/// <summary><inheritDoc/></summary>
		public override void PrettyLog(Redwood.RedwoodChannels channels, string description)
		{
			PrettyLogger.Log(channels, description, Counters.AsMap(this));
		}
	}
}
