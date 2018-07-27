using System;
using System.Collections;
using System.Collections.Generic;




namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A Map which wraps an original Map, and only stores the changes (deltas) from
	/// the original Map.
	/// </summary>
	/// <remarks>
	/// A Map which wraps an original Map, and only stores the changes (deltas) from
	/// the original Map. This increases Map access time (roughly doubles it) but eliminates
	/// Map creation time and decreases memory usage (if you're keeping the original Map in memory
	/// anyway).
	/// <p/>
	/// </remarks>
	/// <author>Teg Grenager (grenager@cs.stanford.edu)</author>
	/// <version>Jan 9, 2004 9:19:06 AM</version>
	public class DeltaMap<K, V> : AbstractMap<K, V>
	{
		private IDictionary<K, V> originalMap;

		private IDictionary<K, V> deltaMap;

		private static object nullValue = new object();

		private static object removedValue = new object();

		internal class SimpleEntry<K, V> : KeyValuePair<K, V>
		{
			internal K key;

			internal V value;

			public SimpleEntry(K key, V value)
			{
				this.key = key;
				this.value = value;
			}

			public SimpleEntry(KeyValuePair<K, V> e)
			{
				this.key = e.Key;
				this.value = e.Value;
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
				V oldValue = this.value;
				this.value = value;
				return oldValue;
			}

			public override bool Equals(object o)
			{
				if (!(o is DictionaryEntry))
				{
					return false;
				}
				KeyValuePair<K, V> e = (KeyValuePair<K, V>)o;
				return Eq(key, e.Key) && Eq(value, e.Value);
			}

			public override int GetHashCode()
			{
				return ((key == null) ? 0 : key.GetHashCode()) ^ ((value == null) ? 0 : value.GetHashCode());
			}

			public override string ToString()
			{
				return key + "=" + value;
			}

			private static bool Eq(object o1, object o2)
			{
				return (o1 == null ? o2 == null : o1.Equals(o2));
			}
		}

		/// <summary>This is more expensive.</summary>
		/// <param name="key">key whose presence in this map is to be tested.</param>
		/// <returns>
		/// <tt>true</tt> if this map contains a mapping for the specified
		/// key.
		/// </returns>
		public override bool Contains(object key)
		{
			// key could be not in original or in deltaMap
			// key could be not in original but in deltaMap
			// key could be in original but removed from deltaMap
			// key could be in original but mapped to something else in deltaMap
			object value = deltaMap[key];
			if (value == null)
			{
				return originalMap.Contains(key);
			}
			return value != removedValue;
		}

		/// <summary>This may cost twice what it would in the original Map.</summary>
		/// <param name="key">key whose associated value is to be returned.</param>
		/// <returns>
		/// the value to which this map maps the specified key, or
		/// <see langword="null"/>
		/// if the map contains no mapping for this key.
		/// </returns>
		public override V Get(object key)
		{
			// key could be not in original or in deltaMap
			// key could be not in original but in deltaMap
			// key could be in original but removed from deltaMap
			// key could be in original but mapped to something else in deltaMap
			V deltaResult = deltaMap[key];
			if (deltaResult == null)
			{
				return originalMap[key];
			}
			if (deltaResult == nullValue)
			{
				return null;
			}
			if (deltaResult == removedValue)
			{
				return null;
			}
			return deltaResult;
		}

		// Modification Operations
		/// <summary>
		/// This may cost twice what it would in the original Map because we have to find
		/// the original value for this key.
		/// </summary>
		/// <param name="key">key with which the specified value is to be associated.</param>
		/// <param name="value">value to be associated with the specified key.</param>
		/// <returns>
		/// previous value associated with specified key, or <tt>null</tt>
		/// if there was no mapping for key.  A <tt>null</tt> return can
		/// also indicate that the map previously associated <tt>null</tt>
		/// with the specified key, if the implementation supports
		/// <tt>null</tt> values.
		/// </returns>
		public override V Put(K key, V value)
		{
			if (value == null)
			{
				return this[key] = (V)nullValue;
			}
			// key could be not in original or in deltaMap
			// key could be not in original but in deltaMap
			// key could be in original but removed from deltaMap
			// key could be in original but mapped to something else in deltaMap
			V result = deltaMap[key] = value;
			if (result == null)
			{
				return originalMap[key];
			}
			if (result == nullValue)
			{
				return null;
			}
			if (result == removedValue)
			{
				return null;
			}
			return result;
		}

		public override V Remove(object key)
		{
			// always put it locally
			return this[(K)key] = (V)removedValue;
		}

		// Bulk Operations
		/// <summary>This is more expensive than normal.</summary>
		public override void Clear()
		{
			// iterate over all keys in originalMap and set them to null in deltaMap
			foreach (K key in originalMap.Keys)
			{
				deltaMap[key] = (V)removedValue;
			}
		}

		// Views
		/// <summary>This is cheap.</summary>
		/// <returns>a set view of the mappings contained in this map.</returns>
		public override ICollection<KeyValuePair<K, V>> EntrySet()
		{
			return new _AbstractSet_199(this);
		}

		private sealed class _AbstractSet_199 : AbstractSet<KeyValuePair<K, V>>
		{
			public _AbstractSet_199(DeltaMap<K, V> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override IEnumerator<KeyValuePair<K, V>> GetEnumerator()
			{
				IPredicate<KeyValuePair<K, V>> filter1 = null;
				IEnumerator<KeyValuePair<K, V>> iter1 = new FilteredIterator<KeyValuePair<K, V>>(this._enclosing.originalMap.GetEnumerator(), filter1);
				IPredicate<KeyValuePair<K, V>> filter2 = null;
				// end class NullingIterator
				IEnumerator<KeyValuePair<K, V>> iter2 = new FilteredIterator<KeyValuePair<K, V>>(new _T1529859365<K, V>(this, this._enclosing.deltaMap.GetEnumerator()), filter2);
				return new ConcatenationIterator<KeyValuePair<K, V>>(iter1, iter2);
			}

			internal class _T795568854<Kk, Vv> : IEnumerator<KeyValuePair<KK, VV>>
			{
				private IEnumerator<KeyValuePair<KK, VV>> i;

				private _T795568854(_AbstractSet_199 _enclosing, IEnumerator<KeyValuePair<KK, VV>> i)
				{
					this._enclosing = _enclosing;
					this.i = i;
				}

				public virtual bool MoveNext()
				{
					return this.i.MoveNext();
				}

				public virtual KeyValuePair<KK, VV> Current
				{
					get
					{
						KeyValuePair<KK, VV> e = this.i.Current;
						object o = e.Value;
						if (o == DeltaMap.nullValue)
						{
							return new DeltaMap.SimpleEntry<KK, VV>(e.Key, null);
						}
						return e;
					}
				}

				public virtual void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly _AbstractSet_199 _enclosing;
			}

			public override int Count
			{
				get
				{
					int size = 0;
					foreach (KeyValuePair<K, V> kvEntry in this)
					{
						ErasureUtils.Noop(kvEntry);
						size++;
					}
					return size;
				}
			}

			private readonly DeltaMap<K, V> _enclosing;
		}

		/// <summary>This is very cheap.</summary>
		/// <param name="originalMap">will serve as the basis for this DeltaMap</param>
		public DeltaMap(IDictionary<K, V> originalMap, MapFactory<K, V> mf)
		{
			this.originalMap = Java.Util.Collections.UnmodifiableMap(originalMap);
			// unmodifiable for debugging only
			this.deltaMap = mf.NewMap();
		}

		public DeltaMap(IDictionary<K, V> originalMap)
			: this(originalMap, MapFactory.HashMapFactory())
		{
		}
	}
}
