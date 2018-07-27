using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// A class for keeping double counts of
	/// <see cref="System.Collections.IList{E}"/>
	/// s of a
	/// prespecified length.  A depth <i>n</i> GeneralizedCounter can be
	/// thought of as a conditionalized count over <i>n</i> classes of
	/// objects, in a prespecified order.  Also offers a read-only view as
	/// a Counter.
	/// <p>
	/// This class is serializable but no guarantees are
	/// made about compatibility version to version.
	/// <p>
	/// This is the worst class. Use TwoDimensionalCounter. If you need a third,
	/// write ThreeDimensionalCounter, but don't use this.
	/// </summary>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public class GeneralizedCounter<K>
	{
		private const long serialVersionUID = 1;

		private static readonly object[] zeroKey = new object[0];

		private IDictionary<K, object> map = Generics.NewHashMap();

		private int depth;

		private double total;

		/// <summary>GeneralizedCounter must be constructed with a depth parameter</summary>
		private GeneralizedCounter()
		{
		}

		/// <summary>Constructs a new GeneralizedCounter of a specified depth</summary>
		/// <param name="depth">the depth of the GeneralizedCounter</param>
		public GeneralizedCounter(int depth)
		{
			this.depth = depth;
		}

		/// <summary>Returns the set of entries in the GeneralizedCounter.</summary>
		/// <remarks>
		/// Returns the set of entries in the GeneralizedCounter.
		/// Here, each key is a read-only
		/// <see cref="System.Collections.IList{E}"/>
		/// of size equal to the depth of the GeneralizedCounter, and
		/// each value is a
		/// <see cref="double"/>
		/// .  Each entry is a
		/// <see cref="System.Collections.DictionaryEntry{K, V}"/>
		/// object,
		/// but these objects
		/// do not support the
		/// <see cref="System.Collections.DictionaryEntry{K, V}.SetValue(object)"/>
		/// method; attempts to call
		/// that method with result
		/// in an
		/// <see cref="System.NotSupportedException"/>
		/// being thrown.
		/// </remarks>
		public virtual ICollection<KeyValuePair<IList<K>, double>> EntrySet()
		{
			return ErasureUtils.UncheckedCast<ICollection<KeyValuePair<IList<K>, double>>>(EntrySet(new HashSet<KeyValuePair<object, double>>(), zeroKey, true));
		}

		/* this is (non-tail) recursive right now, haven't figured out a way
		* to speed it up */
		private ICollection<KeyValuePair<object, double>> EntrySet(ICollection<KeyValuePair<object, double>> s, object[] key, bool useLists)
		{
			if (depth == 1)
			{
				//System.out.println("key is long enough to add to set");
				ICollection<K> keys = map.Keys;
				foreach (K finalKey in keys)
				{
					// array doesn't escape
					K[] newKey = ErasureUtils.MkTArray<K>(typeof(object), key.Length + 1);
					if (key.Length > 0)
					{
						System.Array.Copy(key, 0, newKey, 0, key.Length);
					}
					newKey[key.Length] = finalKey;
					MutableDouble value = (MutableDouble)map[finalKey];
					double value1 = value;
					if (useLists)
					{
						s.Add(new GeneralizedCounter.Entry<object, double>(Arrays.AsList(newKey), value1));
					}
					else
					{
						s.Add(new GeneralizedCounter.Entry<object, double>(newKey[0], value1));
					}
				}
			}
			else
			{
				ICollection<K> keys = map.Keys;
				//System.out.println("key length " + key.length);
				//System.out.println("keyset level " + depth + " " + keys);
				foreach (K o in keys)
				{
					object[] newKey = new object[key.Length + 1];
					if (key.Length > 0)
					{
						System.Array.Copy(key, 0, newKey, 0, key.Length);
					}
					newKey[key.Length] = o;
					//System.out.println("level " + key.length + " current key " + Arrays.asList(newKey));
					ConditionalizeHelper(o).EntrySet(s, newKey, true);
				}
			}
			//System.out.println("leaving key length " + key.length);
			return s;
		}

		/// <summary>
		/// Returns a set of entries, where each key is a read-only
		/// <see cref="System.Collections.IList{E}"/>
		/// of size one less than the depth of the GeneralizedCounter, and
		/// each value is a
		/// <see cref="ClassicCounter{E}"/>
		/// .  Each entry is a
		/// <see cref="System.Collections.DictionaryEntry{K, V}"/>
		/// object, but these objects
		/// do not support the
		/// <see cref="System.Collections.DictionaryEntry{K, V}.SetValue(object)"/>
		/// method; attempts to call that method with result
		/// in an
		/// <see cref="System.NotSupportedException"/>
		/// being thrown.
		/// </summary>
		public virtual ICollection<KeyValuePair<IList<K>, ClassicCounter<K>>> LowestLevelCounterEntrySet()
		{
			return ErasureUtils.UncheckedCast<ICollection<KeyValuePair<IList<K>, ClassicCounter<K>>>>(LowestLevelCounterEntrySet(new HashSet<KeyValuePair<object, ClassicCounter<K>>>(), zeroKey, true));
		}

		/* this is (non-tail) recursive right now, haven't figured out a way
		* to speed it up */
		private ICollection<KeyValuePair<object, ClassicCounter<K>>> LowestLevelCounterEntrySet(ICollection<KeyValuePair<object, ClassicCounter<K>>> s, object[] key, bool useLists)
		{
			ICollection<K> keys = map.Keys;
			if (depth == 2)
			{
				// add these counters to set
				foreach (K finalKey in keys)
				{
					K[] newKey = ErasureUtils.MkTArray<K>(typeof(object), key.Length + 1);
					if (key.Length > 0)
					{
						System.Array.Copy(key, 0, newKey, 0, key.Length);
					}
					newKey[key.Length] = finalKey;
					ClassicCounter<K> c = ConditionalizeHelper(finalKey).OneDimensionalCounterView();
					if (useLists)
					{
						s.Add(new GeneralizedCounter.Entry<object, ClassicCounter<K>>(Arrays.AsList(newKey), c));
					}
					else
					{
						s.Add(new GeneralizedCounter.Entry<object, ClassicCounter<K>>(newKey[0], c));
					}
				}
			}
			else
			{
				//System.out.println("key length " + key.length);
				//System.out.println("keyset level " + depth + " " + keys);
				foreach (K o in keys)
				{
					object[] newKey = new object[key.Length + 1];
					if (key.Length > 0)
					{
						System.Array.Copy(key, 0, newKey, 0, key.Length);
					}
					newKey[key.Length] = o;
					//System.out.println("level " + key.length + " current key " + Arrays.asList(newKey));
					ConditionalizeHelper(o).LowestLevelCounterEntrySet(s, newKey, true);
				}
			}
			//System.out.println("leaving key length " + key.length);
			return s;
		}

		private class Entry<K, V> : KeyValuePair<K, V>
		{
			private K key;

			private V value;

			internal Entry(K key, V value)
			{
				this.key = key;
				this.value = value;
			}

			public virtual K Key
			{
				get
				{
					return key;
				}
			}

			public virtual V Value
			{
				get
				{
					return value;
				}
			}

			public virtual V SetValue(V value)
			{
				throw new NotSupportedException();
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is GeneralizedCounter.Entry))
				{
					return false;
				}
				GeneralizedCounter.Entry<K, V> e = ErasureUtils.UncheckedCast<GeneralizedCounter.Entry<K, V>>(o);
				object key1 = e.Key;
				if (!(key != null && key.Equals(key1)))
				{
					return false;
				}
				object value1 = e.Value;
				if (!(value != null && value.Equals(value1)))
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				if (key == null || value == null)
				{
					return 0;
				}
				return key.GetHashCode() ^ value.GetHashCode();
			}

			public override string ToString()
			{
				return key.ToString() + "=" + value.ToString();
			}
		}

		// end static class Entry
		/// <summary>returns the total count of objects in the GeneralizedCounter.</summary>
		public virtual double TotalCount()
		{
			if (Depth() == 1)
			{
				return total;
			}
			else
			{
				// I think this one is always OK.  Not very principled here, though.
				double result = 0.0;
				foreach (K o in TopLevelKeySet())
				{
					result += ConditionalizeOnce(o).TotalCount();
				}
				return result;
			}
		}

		/// <summary>
		/// Returns the set of elements that occur in the 0th position of a
		/// <see cref="System.Collections.IList{E}"/>
		/// key in the GeneralizedCounter.
		/// </summary>
		/// <seealso cref="GeneralizedCounter{K}.Conditionalize(System.Collections.IList{E})"/>
		/// <seealso cref="GeneralizedCounter{K}.GetCount(object)"/>
		public virtual ICollection<K> TopLevelKeySet()
		{
			return map.Keys;
		}

		/// <summary>
		/// Returns the set of keys, as read-only
		/// <see cref="System.Collections.IList{E}"/>
		/// s of size
		/// equal to the depth of the GeneralizedCounter.
		/// </summary>
		public virtual ICollection<IList<K>> KeySet()
		{
			return ErasureUtils.UncheckedCast<ICollection<IList<K>>>(KeySet(Generics.NewHashSet(), zeroKey, true));
		}

		/* this is (non-tail) recursive right now, haven't figured out a way
		* to speed it up */
		private ICollection<object> KeySet(ICollection<object> s, object[] key, bool useList)
		{
			if (depth == 1)
			{
				//System.out.println("key is long enough to add to set");
				ICollection<K> keys = map.Keys;
				foreach (object oldKey in keys)
				{
					object[] newKey = new object[key.Length + 1];
					if (key.Length > 0)
					{
						System.Array.Copy(key, 0, newKey, 0, key.Length);
					}
					newKey[key.Length] = oldKey;
					if (useList)
					{
						s.Add(Arrays.AsList(newKey));
					}
					else
					{
						s.Add(newKey[0]);
					}
				}
			}
			else
			{
				ICollection<K> keys = map.Keys;
				//System.out.println("key length " + key.length);
				//System.out.println("keyset level " + depth + " " + keys);
				foreach (K o in keys)
				{
					object[] newKey = new object[key.Length + 1];
					if (key.Length > 0)
					{
						System.Array.Copy(key, 0, newKey, 0, key.Length);
					}
					newKey[key.Length] = o;
					//System.out.println("level " + key.length + " current key " + Arrays.asList(newKey));
					ConditionalizeHelper(o).KeySet(s, newKey, true);
				}
			}
			//System.out.println("leaving key length " + key.length);
			return s;
		}

		/// <summary>
		/// Returns the depth of the GeneralizedCounter (i.e., the dimension
		/// of the distribution).
		/// </summary>
		public virtual int Depth()
		{
			return depth;
		}

		/// <summary>Returns true if nothing has a count.</summary>
		public virtual bool IsEmpty()
		{
			return map.IsEmpty();
		}

		/// <summary>
		/// Equivalent to <code>
		/// <see cref="GeneralizedCounter{K}.GetCounts(System.Collections.IList{E})"/>
		/// ({o})</code>; works only
		/// for depth 1 GeneralizedCounters
		/// </summary>
		public virtual double GetCount(object o)
		{
			if (depth > 1)
			{
				WrongDepth();
			}
			Number count = (Number)map[o];
			if (count != null)
			{
				return count;
			}
			else
			{
				return 0.0;
			}
		}

		/// <summary>
		/// A convenience method equivalent to <code>
		/// <see cref="GeneralizedCounter{K}.GetCounts(System.Collections.IList{E})"/>
		/// ({o1,o2})</code>; works only for depth 2
		/// GeneralizedCounters
		/// </summary>
		public virtual double GetCount(K o1, K o2)
		{
			if (depth != 2)
			{
				WrongDepth();
			}
			GeneralizedCounter<K> gc1 = ErasureUtils.UncheckedCast<GeneralizedCounter<K>>(map[o1]);
			if (gc1 == null)
			{
				return 0.0;
			}
			else
			{
				return gc1.GetCount(o2);
			}
		}

		/// <summary>
		/// A convenience method equivalent to <code>
		/// <see cref="GeneralizedCounter{K}.GetCounts(System.Collections.IList{E})"/>
		/// ({o1,o2,o3})</code>; works only for depth 3
		/// GeneralizedCounters
		/// </summary>
		public virtual double GetCount(K o1, K o2, K o3)
		{
			if (depth != 3)
			{
				WrongDepth();
			}
			GeneralizedCounter<K> gc1 = ErasureUtils.UncheckedCast<GeneralizedCounter<K>>(map[o1]);
			if (gc1 == null)
			{
				return 0.0;
			}
			else
			{
				return gc1.GetCount(o2, o3);
			}
		}

		/// <summary>
		/// returns a
		/// <c>double[]</c>
		/// array of length
		/// <c>depth+1</c>
		/// , containing the conditional counts on a
		/// <c>depth</c>
		/// -length list given each level of conditional
		/// distribution from 0 to
		/// <c>depth</c>
		/// .
		/// </summary>
		public virtual double[] GetCounts(IList<K> l)
		{
			if (l.Count != depth)
			{
				WrongDepth();
			}
			//throws exception
			double[] counts = new double[depth + 1];
			GeneralizedCounter<K> next = this;
			counts[0] = next.TotalCount();
			IEnumerator<K> i = l.GetEnumerator();
			int j = 1;
			K o = i.Current;
			while (i.MoveNext())
			{
				next = next.ConditionalizeHelper(o);
				counts[j] = next.TotalCount();
				o = i.Current;
				j++;
			}
			counts[depth] = next.GetCount(o);
			return counts;
		}

		/* haven't decided about access for this one yet */
		private GeneralizedCounter<K> ConditionalizeHelper(K o)
		{
			if (depth > 1)
			{
				GeneralizedCounter<K> next = ErasureUtils.UncheckedCast(map[o]);
				if (next == null)
				{
					// adds a new GeneralizedCounter if needed
					map[o] = (next = new GeneralizedCounter<K>(depth - 1));
				}
				return next;
			}
			else
			{
				throw new Exception("Error -- can't conditionalize a distribution of depth 1");
			}
		}

		/// <summary>
		/// returns a GeneralizedCounter conditioned on the objects in the
		/// <see cref="System.Collections.IList{E}"/>
		/// argument. The length of the argument
		/// <see cref="System.Collections.IList{E}"/>
		/// must be less than the depth of the GeneralizedCounter.
		/// </summary>
		public virtual GeneralizedCounter<K> Conditionalize(IList<K> l)
		{
			int n = l.Count;
			if (n >= Depth())
			{
				throw new Exception("Error -- attempted to conditionalize a GeneralizedCounter of depth " + Depth() + " on a vector of length " + n);
			}
			else
			{
				GeneralizedCounter<K> next = this;
				foreach (K o in l)
				{
					next = next.ConditionalizeHelper(o);
				}
				return next;
			}
		}

		/// <summary>Returns a GeneralizedCounter conditioned on the given top level object.</summary>
		/// <remarks>
		/// Returns a GeneralizedCounter conditioned on the given top level object.
		/// This is just shorthand (and more efficient) for <code>conditionalize(new Object[] { o })</code>.
		/// </remarks>
		public virtual GeneralizedCounter<K> ConditionalizeOnce(K o)
		{
			if (Depth() < 1)
			{
				throw new Exception("Error -- attempted to conditionalize a GeneralizedCounter of depth " + Depth());
			}
			else
			{
				return ConditionalizeHelper(o);
			}
		}

		/// <summary>equivalent to incrementCount(l,o,1.0).</summary>
		public virtual void IncrementCount(IList<K> l, K o)
		{
			IncrementCount(l, o, 1.0);
		}

		/// <summary>same as incrementCount(List, double) but as if Object o were at the end of the list</summary>
		public virtual void IncrementCount(IList<K> l, K o, double count)
		{
			if (l.Count != depth - 1)
			{
				WrongDepth();
			}
			GeneralizedCounter<K> next = this;
			foreach (K o2 in l)
			{
				next.AddToTotal(count);
				next = next.ConditionalizeHelper(o2);
			}
			next.AddToTotal(count);
			next.IncrementCount1D(o, count);
		}

		/// <summary>Equivalent to incrementCount(l, 1.0).</summary>
		public virtual void IncrementCount(IList<K> l)
		{
			IncrementCount(l, 1.0);
		}

		/// <summary>
		/// Adds to count for the
		/// <see cref="GeneralizedCounter{K}.Depth()"/>
		/// -dimensional key
		/// <paramref name="l"/>
		/// .
		/// </summary>
		public virtual void IncrementCount(IList<K> l, double count)
		{
			if (l.Count != depth)
			{
				WrongDepth();
			}
			//throws exception
			GeneralizedCounter<K> next = this;
			IEnumerator<K> i = l.GetEnumerator();
			K o = i.Current;
			while (i.MoveNext())
			{
				next.AddToTotal(count);
				next = next.ConditionalizeHelper(o);
				o = i.Current;
			}
			next.IncrementCount1D(o, count);
		}

		/// <summary>Equivalent to incrementCount2D(first,second,1.0).</summary>
		public virtual void IncrementCount2D(K first, K second)
		{
			IncrementCount2D(first, second, 1.0);
		}

		/// <summary>Equivalent to incrementCount( new Object[] { first, second }, count ).</summary>
		/// <remarks>
		/// Equivalent to incrementCount( new Object[] { first, second }, count ).
		/// Makes the special case easier, and also more efficient.
		/// </remarks>
		public virtual void IncrementCount2D(K first, K second, double count)
		{
			if (depth != 2)
			{
				WrongDepth();
			}
			//throws exception
			this.AddToTotal(count);
			GeneralizedCounter<K> next = this.ConditionalizeHelper(first);
			next.IncrementCount1D(second, count);
		}

		/// <summary>Equivalent to incrementCount3D(first,second,1.0).</summary>
		public virtual void IncrementCount3D(K first, K second, K third)
		{
			IncrementCount3D(first, second, third, 1.0);
		}

		/// <summary>Equivalent to incrementCount( new Object[] { first, second, third }, count ).</summary>
		/// <remarks>
		/// Equivalent to incrementCount( new Object[] { first, second, third }, count ).
		/// Makes the special case easier, and also more efficient.
		/// </remarks>
		public virtual void IncrementCount3D(K first, K second, K third, double count)
		{
			if (depth != 3)
			{
				WrongDepth();
			}
			//throws exception
			this.AddToTotal(count);
			GeneralizedCounter<K> next = this.ConditionalizeHelper(first);
			next.IncrementCount2D(second, third, count);
		}

		private void AddToTotal(double d)
		{
			total += d;
		}

		[System.NonSerialized]
		private MutableDouble tempMDouble = null;

		// for more efficient memory usage
		/// <summary>Equivalent to incrementCount1D(o, 1.0).</summary>
		public virtual void IncrementCount1D(K o)
		{
			IncrementCount1D(o, 1.0);
		}

		/// <summary>
		/// Equivalent to <code>
		/// <see cref="GeneralizedCounter{K}.IncrementCount(System.Collections.IList{E})"/>
		/// ({o}, count)</code>;
		/// only works for a depth 1 GeneralizedCounter.
		/// </summary>
		public virtual void IncrementCount1D(K o, double count)
		{
			if (depth > 1)
			{
				WrongDepth();
			}
			AddToTotal(count);
			if (tempMDouble == null)
			{
				tempMDouble = new MutableDouble();
			}
			tempMDouble.Set(count);
			MutableDouble oldMDouble = (MutableDouble)map[o] = tempMDouble;
			if (oldMDouble != null)
			{
				tempMDouble.Set(count + oldMDouble);
			}
			tempMDouble = oldMDouble;
		}

		/// <summary>
		/// Like
		/// <see cref="ClassicCounter{E}"/>
		/// , this currently returns true if the count is
		/// explicitly 0.0 for something
		/// </summary>
		public virtual bool ContainsKey(IList<K> key)
		{
			//     if(! (key instanceof Object[]))
			//       return false;
			//    Object[] o = (Object[]) key;
			GeneralizedCounter<K> next = this;
			for (int i = 0; i < key.Count - 1; i++)
			{
				next = next.ConditionalizeHelper(key[i]);
				if (next == null)
				{
					return false;
				}
			}
			return next.map.Contains(key[key.Count - 1]);
		}

		public virtual GeneralizedCounter<K> ReverseKeys()
		{
			GeneralizedCounter<K> result = new GeneralizedCounter<K>();
			ICollection<KeyValuePair<IList<K>, double>> entries = EntrySet();
			foreach (KeyValuePair<IList<K>, double> entry in entries)
			{
				IList<K> list = entry.Key;
				double count = entry.Value;
				Java.Util.Collections.Reverse(list);
				result.IncrementCount(list, count);
			}
			return result;
		}

		private void WrongDepth()
		{
			throw new Exception("Error -- attempt to operate with key of wrong length. depth=" + depth);
		}

		/// <summary>
		/// Returns a read-only synchronous view (not a snapshot) of
		/// <c>this</c>
		/// as a
		/// <see cref="ClassicCounter{E}"/>
		/// .  Any calls to
		/// count-changing or entry-removing operations will result in an
		/// <see cref="System.NotSupportedException"/>
		/// .  At some point in the
		/// future, this view may gain limited writable functionality.
		/// </summary>
		public virtual ClassicCounter<IList<K>> CounterView()
		{
			return new GeneralizedCounter.CounterView(this);
		}

		[System.Serializable]
		private class CounterView : ClassicCounter<IList<K>>
		{
			private const long serialVersionUID = -1241712543674668918L;

			public override double IncrementCount(IList<K> o, double count)
			{
				throw new NotSupportedException();
			}

			public override void SetCount(IList<K> o, double count)
			{
				throw new NotSupportedException();
			}

			public override double TotalCount()
			{
				return this._enclosing._enclosing.TotalCount();
			}

			public override double GetCount(object o)
			{
				IList<K> l = (IList<K>)o;
				if (l.Count != this._enclosing.depth)
				{
					return 0.0;
				}
				else
				{
					return this._enclosing._enclosing.GetCounts(l)[this._enclosing.depth];
				}
			}

			public override int Size()
			{
				return this._enclosing._enclosing.map.Count;
			}

			public override ICollection<IList<K>> KeySet()
			{
				return this._enclosing._enclosing.KeySet();
			}

			public override double Remove(IList<K> o)
			{
				throw new NotSupportedException();
			}

			public override bool ContainsKey(IList<K> key)
			{
				return this._enclosing._enclosing.ContainsKey(key);
			}

			public override void Clear()
			{
				throw new NotSupportedException();
			}

			public override bool IsEmpty()
			{
				return this._enclosing._enclosing.IsEmpty();
			}

			public override ICollection<KeyValuePair<IList<K>, double>> EntrySet()
			{
				return this._enclosing._enclosing.EntrySet();
			}

			public override bool Equals(object o)
			{
				if (o == this)
				{
					return true;
				}
				//return false;
				if (!(o is ClassicCounter))
				{
					return false;
				}
				else
				{
					// System.out.println("it's a counter!");
					// Set e = entrySet();
					// Set e1 = ((Counter) o).entrySet();
					// System.out.println(e + "\n" + e1);
					return this.EntrySet().Equals(((ClassicCounter<object>)o).EntrySet());
				}
			}

			public override int GetHashCode()
			{
				int total = 17;
				foreach (object o in this.EntrySet())
				{
					total = 37 * total + o.GetHashCode();
				}
				return total;
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder("{");
				for (IEnumerator<KeyValuePair<IList<K>, double>> i = this.EntrySet().GetEnumerator(); i.MoveNext(); )
				{
					KeyValuePair<IList<K>, double> e = i.Current;
					sb.Append(e);
					if (i.MoveNext())
					{
						sb.Append(',');
					}
				}
				sb.Append("}");
				return sb.ToString();
			}

			internal CounterView(GeneralizedCounter<K> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly GeneralizedCounter<K> _enclosing;
		}

		// end class CounterView
		/// <summary>
		/// Returns a read-only synchronous view (not a snapshot) of
		/// <c>this</c>
		/// as a
		/// <see cref="ClassicCounter{E}"/>
		/// .  Works only with one-dimensional
		/// GeneralizedCounters.  Exactly like
		/// <see cref="GeneralizedCounter{K}.CounterView()"/>
		/// , except
		/// that
		/// <see cref="GeneralizedCounter{K}.GetCount(object)"/>
		/// operates on primitive objects of the counter instead
		/// of singleton lists.  Any calls to
		/// count-changing or entry-removing operations will result in an
		/// <see cref="System.NotSupportedException"/>
		/// .  At some point in the
		/// future, this view may gain limited writable functionality.
		/// </summary>
		public virtual ClassicCounter<K> OneDimensionalCounterView()
		{
			if (depth != 1)
			{
				throw new NotSupportedException();
			}
			return new GeneralizedCounter.OneDimensionalCounterView(this);
		}

		[System.Serializable]
		private class OneDimensionalCounterView : ClassicCounter<K>
		{
			private const long serialVersionUID = 5628505169749516972L;

			public override double IncrementCount(K o, double count)
			{
				throw new NotSupportedException();
			}

			public override void SetCount(K o, double count)
			{
				throw new NotSupportedException();
			}

			public override double TotalCount()
			{
				return this._enclosing._enclosing.TotalCount();
			}

			public override double GetCount(object o)
			{
				return this._enclosing._enclosing.GetCount(o);
			}

			public override int Size()
			{
				return this._enclosing._enclosing.map.Count;
			}

			public override ICollection<K> KeySet()
			{
				return ErasureUtils.UncheckedCast<ICollection<K>>(this._enclosing._enclosing.KeySet(Generics.NewHashSet(), GeneralizedCounter.zeroKey, false));
			}

			public override double Remove(object o)
			{
				throw new NotSupportedException();
			}

			public override bool ContainsKey(object key)
			{
				return this._enclosing._enclosing.map.Contains(key);
			}

			public override void Clear()
			{
				throw new NotSupportedException();
			}

			public override bool IsEmpty()
			{
				return this._enclosing._enclosing.IsEmpty();
			}

			public override ICollection<KeyValuePair<K, double>> EntrySet()
			{
				return ErasureUtils.UncheckedCast<ICollection<KeyValuePair<K, double>>>(this._enclosing._enclosing.EntrySet(new HashSet<KeyValuePair<object, double>>(), GeneralizedCounter.zeroKey, false));
			}

			public override bool Equals(object o)
			{
				if (o == this)
				{
					return true;
				}
				//return false;
				if (!(o is ClassicCounter))
				{
					return false;
				}
				else
				{
					// System.out.println("it's a counter!");
					// Set e = entrySet();
					// Set e1 = ((Counter) o).map.entrySet();
					// System.out.println(e + "\n" + e1);
					return this.EntrySet().Equals(((ClassicCounter<object>)o).EntrySet());
				}
			}

			public override int GetHashCode()
			{
				int total = 17;
				foreach (object o in this.EntrySet())
				{
					total = 37 * total + o.GetHashCode();
				}
				return total;
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder("{");
				for (IEnumerator<KeyValuePair<K, double>> i = this.EntrySet().GetEnumerator(); i.MoveNext(); )
				{
					KeyValuePair<K, double> e = i.Current;
					sb.Append(e.ToString());
					if (i.MoveNext())
					{
						sb.Append(",");
					}
				}
				sb.Append("}");
				return sb.ToString();
			}

			internal OneDimensionalCounterView(GeneralizedCounter<K> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly GeneralizedCounter<K> _enclosing;
		}

		// end class OneDimensionalCounterView
		public override string ToString()
		{
			return map.ToString();
		}

		public virtual string ToString(string param)
		{
			switch (param)
			{
				case "contingency":
				{
					StringBuilder sb = new StringBuilder();
					foreach (K obj in ErasureUtils.SortedIfPossible(TopLevelKeySet()))
					{
						sb.Append(obj);
						sb.Append(" = ");
						GeneralizedCounter<K> gc = ConditionalizeOnce(obj);
						sb.Append(gc);
						sb.Append("\n");
					}
					return sb.ToString();
				}

				case "sorted":
				{
					StringBuilder sb = new StringBuilder();
					sb.Append("{\n");
					foreach (K obj in ErasureUtils.SortedIfPossible(TopLevelKeySet()))
					{
						sb.Append(obj);
						sb.Append(" = ");
						GeneralizedCounter<K> gc = ConditionalizeOnce(obj);
						sb.Append(gc);
						sb.Append("\n");
					}
					sb.Append("}\n");
					return sb.ToString();
				}

				default:
				{
					return ToString();
				}
			}
		}

		/// <summary>for testing purposes only</summary>
		public static void Main(string[] args)
		{
			object[] a1 = new object[] { "a", "b" };
			object[] a2 = new object[] { "a", "b" };
			System.Console.Out.WriteLine(Arrays.Equals(a1, a2));
			GeneralizedCounter<string> gc = new GeneralizedCounter<string>(3);
			gc.IncrementCount(Arrays.AsList(new string[] { "a", "j", "x" }), 3.0);
			gc.IncrementCount(Arrays.AsList(new string[] { "a", "l", "x" }), 3.0);
			gc.IncrementCount(Arrays.AsList(new string[] { "b", "k", "y" }), 3.0);
			gc.IncrementCount(Arrays.AsList(new string[] { "b", "k", "z" }), 3.0);
			System.Console.Out.WriteLine("incremented counts.");
			System.Console.Out.WriteLine(gc.DumpKeys());
			System.Console.Out.WriteLine("string representation of generalized counter:");
			System.Console.Out.WriteLine(gc.ToString());
			gc.PrintKeySet();
			System.Console.Out.WriteLine("entry set:\n" + gc.EntrySet());
			ArrayPrintDouble(gc.GetCounts(Arrays.AsList(new string[] { "a", "j", "x" })));
			ArrayPrintDouble(gc.GetCounts(Arrays.AsList(new string[] { "a", "j", "z" })));
			ArrayPrintDouble(gc.GetCounts(Arrays.AsList(new string[] { "b", "k", "w" })));
			ArrayPrintDouble(gc.GetCounts(Arrays.AsList(new string[] { "b", "k", "z" })));
			GeneralizedCounter<string> gc1 = gc.Conditionalize(Arrays.AsList(new string[] { "a" }));
			gc1.IncrementCount(Arrays.AsList(new string[] { "j", "x" }));
			gc1.IncrementCount2D("j", "z");
			GeneralizedCounter<string> gc2 = gc1.Conditionalize(Arrays.AsList(new string[] { "j" }));
			gc2.IncrementCount1D("x");
			System.Console.Out.WriteLine("Pretty-printing gc after incrementing gc1:");
			gc.PrettyPrint();
			System.Console.Out.WriteLine("Total: " + gc.TotalCount());
			gc1.PrintKeySet();
			System.Console.Out.WriteLine("another entry set:\n" + gc1.EntrySet());
			ClassicCounter<IList<string>> c = gc.CounterView();
			System.Console.Out.WriteLine("string representation of counter view:");
			System.Console.Out.WriteLine(c.ToString());
			double d1 = c.GetCount(Arrays.AsList(new string[] { "a", "j", "x" }));
			double d2 = c.GetCount(Arrays.AsList(new string[] { "a", "j", "w" }));
			System.Console.Out.WriteLine(d1 + " " + d2);
			ClassicCounter<IList<string>> c1 = gc1.CounterView();
			System.Console.Out.WriteLine("Count of {j,x} -- should be 3.0\t" + c1.GetCount(Arrays.AsList(new string[] { "j", "x" })));
			System.Console.Out.WriteLine(c.KeySet() + " size " + c.KeySet().Count);
			System.Console.Out.WriteLine(c1.KeySet() + " size " + c1.KeySet().Count);
			System.Console.Out.WriteLine(c1.Equals(c));
			System.Console.Out.WriteLine(c.Equals(c1));
			System.Console.Out.WriteLine(c.Equals(c));
			System.Console.Out.WriteLine("### testing equality of regular Counter...");
			ClassicCounter<string> z1 = new ClassicCounter<string>();
			ClassicCounter<string> z2 = new ClassicCounter<string>();
			z1.IncrementCount("a1");
			z1.IncrementCount("a2");
			z2.IncrementCount("b");
			System.Console.Out.WriteLine(z1.Equals(z2));
			System.Console.Out.WriteLine(z1.ToString());
			System.Console.Out.WriteLine(z1.KeySet().ToString());
		}

		// below is testing code
		private void PrintKeySet()
		{
			ICollection<object> keys = KeySet();
			System.Console.Out.WriteLine("printing keyset:");
			foreach (object o in keys)
			{
				//System.out.println(Arrays.asList((Object[]) i.next()));
				System.Console.Out.WriteLine(o);
			}
		}

		private static void ArrayPrintDouble(double[] o)
		{
			foreach (double anO in o)
			{
				System.Console.Out.Write(anO + "\t");
			}
			System.Console.Out.WriteLine();
		}

		private ICollection<object> DumpKeys()
		{
			return map.Keys;
		}

		/// <summary>
		/// pretty-prints the GeneralizedCounter to
		/// <see cref="System.Console.Out"/>
		/// .
		/// </summary>
		public virtual void PrettyPrint()
		{
			PrettyPrint(new PrintWriter(System.Console.Out, true));
		}

		/// <summary>pretty-prints the GeneralizedCounter, using a buffer increment of two spaces.</summary>
		public virtual void PrettyPrint(PrintWriter pw)
		{
			PrettyPrint(pw, "  ");
		}

		/// <summary>pretty-prints the GeneralizedCounter.</summary>
		public virtual void PrettyPrint(PrintWriter pw, string bufferIncrement)
		{
			PrettyPrint(pw, string.Empty, bufferIncrement);
		}

		private void PrettyPrint(PrintWriter pw, string buffer, string bufferIncrement)
		{
			if (depth == 1)
			{
				foreach (KeyValuePair<object, double> e in EntrySet())
				{
					object key = e.Key;
					double count = e.Value;
					pw.Println(buffer + key + "\t" + count);
				}
			}
			else
			{
				foreach (K key in TopLevelKeySet())
				{
					GeneralizedCounter<K> gc1 = Conditionalize(Arrays.AsList(ErasureUtils.UncheckedCast<K[]>(new object[] { key })));
					pw.Println(buffer + key + "\t" + gc1.TotalCount());
					gc1.PrettyPrint(pw, buffer + bufferIncrement, bufferIncrement);
				}
			}
		}
	}
}
