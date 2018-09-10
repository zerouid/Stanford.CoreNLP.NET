using System;
using System.Collections.Generic;




namespace Edu.Stanford.Nlp.Util
{
	/// <author>grenager</author>
	[System.Serializable]
	public class TwoDimensionalMap<K1, K2, V> : IEnumerable<TwoDimensionalMap.Entry<K1, K2, V>>
	{
		private const long serialVersionUID = 2L;

		private readonly MapFactory<K1, IDictionary<K2, V>> mf1;

		private readonly MapFactory<K2, V> mf2;

		internal IDictionary<K1, IDictionary<K2, V>> map;

		public virtual int Size()
		{
			int size = 0;
			foreach (KeyValuePair<K1, IDictionary<K2, V>> entry in map)
			{
				size += (entry.Value.Count);
			}
			return size;
		}

		public virtual bool IsEmpty()
		{
			foreach (KeyValuePair<K1, IDictionary<K2, V>> entry in map)
			{
				if (!entry.Value.IsEmpty())
				{
					return false;
				}
			}
			return true;
		}

		public virtual V Put(K1 key1, K2 key2, V value)
		{
			IDictionary<K2, V> m = GetMap(key1);
			return m[key2] = value;
		}

		// adds empty hashmap for key key1
		public virtual void Put(K1 key1)
		{
			map[key1] = mf2.NewMap();
		}

		public virtual bool Contains(K1 key1, K2 key2)
		{
			if (!ContainsKey(key1))
			{
				return false;
			}
			return GetMap(key1).Contains(key2);
		}

		public virtual V Get(K1 key1, K2 key2)
		{
			IDictionary<K2, V> m = GetMap(key1);
			return m[key2];
		}

		public virtual V Remove(K1 key1, K2 key2)
		{
			return Sharpen.Collections.Remove(Get(key1), key2);
		}

		/// <summary>Removes all of the data associated with the first key in the map</summary>
		public virtual void Remove(K1 key1)
		{
			Sharpen.Collections.Remove(map, key1);
		}

		public virtual void Clear()
		{
			map.Clear();
		}

		public virtual bool ContainsKey(K1 key1)
		{
			return map.Contains(key1);
		}

		public virtual IDictionary<K2, V> Get(K1 key1)
		{
			return GetMap(key1);
		}

		public virtual IDictionary<K2, V> GetMap(K1 key1)
		{
			IDictionary<K2, V> m = map[key1];
			if (m == null)
			{
				m = mf2.NewMap();
				map[key1] = m;
			}
			return m;
		}

		public virtual ICollection<V> Values()
		{
			// TODO: Should return a specialized class
			IList<V> s = Generics.NewArrayList();
			foreach (IDictionary<K2, V> innerMap in map.Values)
			{
				Sharpen.Collections.AddAll(s, innerMap.Values);
			}
			return s;
		}

		public virtual ICollection<K1> FirstKeySet()
		{
			return map.Keys;
		}

		public virtual ICollection<K2> SecondKeySet()
		{
			ICollection<K2> keys = Generics.NewHashSet();
			foreach (K1 k1 in map.Keys)
			{
				Sharpen.Collections.AddAll(keys, Get(k1).Keys);
			}
			return keys;
		}

		/// <summary>
		/// Adds all of the entries in the <code>other</code> map, performing
		/// <code>function</code> on them to transform the values
		/// </summary>
		public virtual void AddAll<V2, _T1>(Edu.Stanford.Nlp.Util.TwoDimensionalMap<_T1> other, Func<V2, V> function)
			where _T1 : K1
		{
			foreach (TwoDimensionalMap.Entry<K1, K2, V2> entry in other)
			{
				Put(entry.GetFirstKey(), entry.GetSecondKey(), function.Apply(entry.GetValue()));
			}
		}

		public TwoDimensionalMap()
			: this(MapFactory.HashMapFactory<K1, IDictionary<K2, V>>(), MapFactory.HashMapFactory<K2, V>())
		{
		}

		public TwoDimensionalMap(Edu.Stanford.Nlp.Util.TwoDimensionalMap<K1, K2, V> tdm)
			: this(tdm.mf1, tdm.mf2)
		{
			foreach (K1 k1 in tdm.map.Keys)
			{
				IDictionary<K2, V> m = tdm.map[k1];
				IDictionary<K2, V> copy = mf2.NewMap();
				copy.PutAll(m);
				this.map[k1] = copy;
			}
		}

		public TwoDimensionalMap(MapFactory<K1, IDictionary<K2, V>> mf1, MapFactory<K2, V> mf2)
		{
			this.mf1 = mf1;
			this.mf2 = mf2;
			this.map = mf1.NewMap();
		}

		public static Edu.Stanford.Nlp.Util.TwoDimensionalMap<K1, K2, V> HashMap<K1, K2, V>()
		{
			return new Edu.Stanford.Nlp.Util.TwoDimensionalMap<K1, K2, V>(MapFactory.HashMapFactory<K1, IDictionary<K2, V>>(), MapFactory.HashMapFactory<K2, V>());
		}

		public static Edu.Stanford.Nlp.Util.TwoDimensionalMap<K1, K2, V> TreeMap<K1, K2, V>()
		{
			return new Edu.Stanford.Nlp.Util.TwoDimensionalMap<K1, K2, V>(MapFactory.TreeMapFactory<K1, IDictionary<K2, V>>(), MapFactory.TreeMapFactory<K2, V>());
		}

		public static Edu.Stanford.Nlp.Util.TwoDimensionalMap<K1, K2, V> IdentityHashMap<K1, K2, V>()
		{
			return new Edu.Stanford.Nlp.Util.TwoDimensionalMap<K1, K2, V>(MapFactory.IdentityHashMapFactory<K1, IDictionary<K2, V>>(), MapFactory.IdentityHashMapFactory<K2, V>());
		}

		public override string ToString()
		{
			return map.ToString();
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Util.TwoDimensionalMap))
			{
				return false;
			}
			Edu.Stanford.Nlp.Util.TwoDimensionalMap<object, object, object> other = (Edu.Stanford.Nlp.Util.TwoDimensionalMap<object, object, object>)o;
			return map.Equals(other.map);
		}

		public override int GetHashCode()
		{
			return map.GetHashCode();
		}

		/// <summary>Iterate over the map using the iterator and entry inner classes.</summary>
		public virtual IEnumerator<TwoDimensionalMap.Entry<K1, K2, V>> GetEnumerator()
		{
			return new TwoDimensionalMap.TwoDimensionalMapIterator<K1, K2, V>(this);
		}

		public virtual IEnumerator<V> ValueIterator()
		{
			return new TwoDimensionalMap.TwoDimensionalMapValueIterator<K1, K2, V>(this);
		}

		internal class TwoDimensionalMapValueIterator<K1, K2, V> : IEnumerator<V>
		{
			internal IEnumerator<TwoDimensionalMap.Entry<K1, K2, V>> entryIterator;

			internal TwoDimensionalMapValueIterator(TwoDimensionalMap<K1, K2, V> map)
			{
				entryIterator = map.GetEnumerator();
			}

			public virtual bool MoveNext()
			{
				return entryIterator.MoveNext();
			}

			public virtual V Current
			{
				get
				{
					TwoDimensionalMap.Entry<K1, K2, V> next = entryIterator.Current;
					return next.GetValue();
				}
			}

			public virtual void Remove()
			{
				entryIterator.Remove();
			}
		}

		/// <summary>This inner class represents a single entry in the TwoDimensionalMap.</summary>
		/// <remarks>
		/// This inner class represents a single entry in the TwoDimensionalMap.
		/// Iterating over the map will give you these.
		/// </remarks>
		public class Entry<K1, K2, V>
		{
			internal K1 firstKey;

			internal K2 secondKey;

			internal V value;

			internal Entry(K1 k1, K2 k2, V v)
			{
				firstKey = k1;
				secondKey = k2;
				value = v;
			}

			public virtual K1 GetFirstKey()
			{
				return firstKey;
			}

			public virtual K2 GetSecondKey()
			{
				return secondKey;
			}

			public virtual V GetValue()
			{
				return value;
			}

			public override string ToString()
			{
				return "(" + firstKey + "," + secondKey + "," + value + ")";
			}
		}

		/// <summary>
		/// Internal class which represents an iterator over the data in the
		/// TwoDimensionalMap.
		/// </summary>
		/// <remarks>
		/// Internal class which represents an iterator over the data in the
		/// TwoDimensionalMap.  It keeps state in the form of an iterator
		/// over the outer map, which maps keys to inner maps, and an
		/// iterator over the most recent inner map seen.  When the inner map
		/// has been completely iterated over, the outer map iterator
		/// advances one step.  The iterator is finished when all key pairs
		/// have been returned once.
		/// </remarks>
		internal class TwoDimensionalMapIterator<K1, K2, V> : IEnumerator<TwoDimensionalMap.Entry<K1, K2, V>>
		{
			internal IEnumerator<KeyValuePair<K1, IDictionary<K2, V>>> outerIterator;

			internal IEnumerator<KeyValuePair<K2, V>> innerIterator;

			internal TwoDimensionalMap.Entry<K1, K2, V> next;

			internal TwoDimensionalMapIterator(TwoDimensionalMap<K1, K2, V> map)
			{
				outerIterator = map.map.GetEnumerator();
				PrimeNext();
			}

			public virtual bool MoveNext()
			{
				return next != null;
			}

			public virtual TwoDimensionalMap.Entry<K1, K2, V> Current
			{
				get
				{
					if (next == null)
					{
						throw new NoSuchElementException();
					}
					TwoDimensionalMap.Entry<K1, K2, V> result = next;
					PrimeNext();
					return result;
				}
			}

			private void PrimeNext()
			{
				K1 k1 = null;
				if (next != null)
				{
					k1 = next.GetFirstKey();
				}
				while (innerIterator == null || !innerIterator.MoveNext())
				{
					if (!outerIterator.MoveNext())
					{
						next = null;
						return;
					}
					KeyValuePair<K1, IDictionary<K2, V>> outerEntry = outerIterator.Current;
					k1 = outerEntry.Key;
					innerIterator = outerEntry.Value.GetEnumerator();
				}
				KeyValuePair<K2, V> innerEntry = innerIterator.Current;
				next = new TwoDimensionalMap.Entry<K1, K2, V>(k1, innerEntry.Key, innerEntry.Value);
			}

			public virtual void Remove()
			{
				throw new NotSupportedException();
			}
		}
	}
}
