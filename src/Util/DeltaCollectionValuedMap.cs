using System;
using System.Collections;
using System.Collections.Generic;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Implementation of CollectionValuedMap that appears to store an "original"
	/// map and changes to that map.
	/// </summary>
	/// <remarks>
	/// Implementation of CollectionValuedMap that appears to store an "original"
	/// map and changes to that map. No one currently uses it. See
	/// <see cref="DeltaMap{K, V}"/>
	/// .
	/// </remarks>
	/// <author>Teg Grenager (grenager@cs.stanford.edu)</author>
	/// <version>Jan 14, 2004</version>
	[System.Serializable]
	public class DeltaCollectionValuedMap<K, V> : CollectionValuedMap<K, V>
	{
		private const long serialVersionUID = 1L;

		private readonly CollectionValuedMap<K, V> originalMap;

		private readonly IDictionary<K, ICollection<V>> deltaMap;

		private static readonly object removedValue = new object();

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
				DictionaryEntry e = ErasureUtils.UncheckedCast(o);
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

		public override ICollection<V> Get(object key)
		{
			// key could be not in original or in deltaMap
			// key could be not in original but in deltaMap
			// key could be in original but removed from deltaMap
			// key could be in original but mapped to something else in deltaMap
			ICollection<V> deltaResult = deltaMap[key];
			if (deltaResult == null)
			{
				return originalMap[key];
			}
			if (deltaResult == removedValue)
			{
				return cf.NewEmptyCollection();
			}
			return deltaResult;
		}

		// Modification Operations
		public override ICollection<V> Put(K key, ICollection<V> value)
		{
			throw new NotSupportedException();
		}

		public override void PutAll<_T0>(IDictionary<_T0> m)
		{
			throw new NotSupportedException();
		}

		public override void Add(K key, V value)
		{
			ICollection<V> deltaC = deltaMap[key];
			if (deltaC == null)
			{
				deltaC = cf.NewCollection();
				ICollection<V> originalC = originalMap[key];
				if (originalC != null)
				{
					Sharpen.Collections.AddAll(deltaC, originalC);
				}
				deltaMap[key] = deltaC;
			}
			deltaC.Add(value);
		}

		/// <summary>Adds all of the mappings in m to this CollectionValuedMap.</summary>
		/// <remarks>
		/// Adds all of the mappings in m to this CollectionValuedMap.
		/// If m is a CollectionValuedMap, it will behave strangely. Use the constructor instead.
		/// </remarks>
		public override void AddAll(IDictionary<K, V> m)
		{
			foreach (KeyValuePair<K, V> e in m)
			{
				Add(e.Key, e.Value);
			}
		}

		public override ICollection<V> Remove(object key)
		{
			ICollection<V> result = this[key];
			deltaMap[ErasureUtils.UncheckedCast(key)] = ErasureUtils.UncheckedCast(removedValue);
			return result;
		}

		public override void RemoveMapping(K key, V value)
		{
			ICollection<V> deltaC = deltaMap[key];
			if (deltaC == null)
			{
				ICollection<V> originalC = originalMap[key];
				if (originalC != null && originalC.Contains(value))
				{
					deltaC = cf.NewCollection();
					Sharpen.Collections.AddAll(deltaC, originalC);
					deltaMap[key] = deltaC;
				}
			}
			if (deltaC != null)
			{
				deltaC.Remove(value);
			}
		}

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

		public override bool ContainsValue(object value)
		{
			throw new NotSupportedException();
		}

		// Bulk Operations
		/// <summary>This is more expensive than normal.</summary>
		public override void Clear()
		{
			// iterate over all keys in originalMap and set them to null in deltaMap
			foreach (K key in originalMap.Keys)
			{
				deltaMap[key] = ErasureUtils.UncheckedCast(removedValue);
			}
		}

		public override bool IsEmpty()
		{
			return Count == 0;
		}

		public override int Count
		{
			get
			{
				return this.Count;
			}
		}

		public override ICollection<ICollection<V>> Values
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Views
		/// <summary>This is cheap.</summary>
		/// <returns>A set view of the mappings contained in this map.</returns>
		public override ICollection<KeyValuePair<K, ICollection<V>>> EntrySet()
		{
			return new _AbstractSet_217(this);
		}

		private sealed class _AbstractSet_217 : AbstractSet<KeyValuePair<K, ICollection<V>>>
		{
			public _AbstractSet_217(DeltaCollectionValuedMap<K, V> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override IEnumerator<KeyValuePair<K, ICollection<V>>> GetEnumerator()
			{
				IPredicate<KeyValuePair<K, ICollection<V>>> filter1 = null;
				IEnumerator<KeyValuePair<K, ICollection<V>>> iter1 = new FilteredIterator<KeyValuePair<K, ICollection<V>>>(this._enclosing.originalMap.GetEnumerator(), filter1);
				IPredicate<KeyValuePair<K, ICollection<V>>> filter2 = null;
				IEnumerator<KeyValuePair<K, ICollection<V>>> iter2 = new FilteredIterator<KeyValuePair<K, ICollection<V>>>(this._enclosing.deltaMap.GetEnumerator(), filter2);
				return new ConcatenationIterator<KeyValuePair<K, ICollection<V>>>(iter1, iter2);
			}

			public override int Count
			{
				get
				{
					int size = 0;
					foreach (KeyValuePair<K, ICollection<V>> ignored in this)
					{
						size++;
					}
					return size;
				}
			}

			private readonly DeltaCollectionValuedMap<K, V> _enclosing;
		}

		public DeltaCollectionValuedMap(CollectionValuedMap<K, V> originalMap)
			: base(originalMap.mf, originalMap.cf, originalMap.treatCollectionsAsImmutable)
		{
			this.originalMap = originalMap;
			this.deltaMap = mf.NewMap();
		}
	}
}
