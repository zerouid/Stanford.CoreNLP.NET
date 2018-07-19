using System;
using System.Collections.Generic;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Map from keys to
	/// <see cref="System.Collections.ICollection{E}"/>
	/// s. Important methods are the
	/// <see cref="CollectionValuedMap{K, V}.Add(object, object)"/>
	/// and
	/// <see cref="Sharpen.Collections.Remove(object)"/>
	/// methods for adding and removing a value to/from the
	/// Collection associated with the key, and the
	/// <see cref="CollectionValuedMap{K, V}.Get(object)"/>
	/// method for getting
	/// the Collection associated with a key. The class is quite general, because on
	/// construction, it is possible to pass a
	/// <see cref="MapFactory{K, V}"/>
	/// which will be used
	/// to create the underlying map and a
	/// <see cref="CollectionFactory{T}"/>
	/// which will be
	/// used to create the Collections. Thus this class can be configured to act like
	/// a "HashSetValuedMap" or a "ListValuedMap", or even a
	/// "HashSetValuedIdentityHashMap". The possibilities are endless!
	/// </summary>
	/// <?/>
	/// <?/>
	/// <author>Teg Grenager (grenager@cs.stanford.edu)</author>
	/// <author>
	/// Sarah Spikes (sdspikes@cs.stanford.edu) - cleanup and filling in
	/// types
	/// </author>
	[System.Serializable]
	public class CollectionValuedMap<K, V> : IDictionary<K, ICollection<V>>
	{
		private const long serialVersionUID = -9064664153962599076L;

		private readonly IDictionary<K, ICollection<V>> map;

		protected internal readonly CollectionFactory<V> cf;

		protected internal readonly bool treatCollectionsAsImmutable;

		protected internal readonly MapFactory<K, ICollection<V>> mf;

		/// <summary>Replaces current Collection mapped to key with the specified Collection.</summary>
		/// <remarks>
		/// Replaces current Collection mapped to key with the specified Collection.
		/// Use carefully!
		/// </remarks>
		public virtual ICollection<V> Put(K key, ICollection<V> collection)
		{
			return map[key] = collection;
		}

		/// <summary>Unsupported.</summary>
		/// <remarks>
		/// Unsupported. Use
		/// <see cref="CollectionValuedMap{K, V}.AddAll(System.Collections.IDictionary{K, V})"/>
		/// instead.
		/// </remarks>
		public virtual void PutAll<_T0>(IDictionary<_T0> m)
			where _T0 : K
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// The empty collection to be returned when a
		/// <c>get</c>
		/// doesn't find
		/// the key. The collection returned should be empty, such as
		/// Collections.emptySet, for example.
		/// </summary>
		private readonly ICollection<V> emptyValue;

		/// <returns>the Collection mapped to by key, never null, but may be empty.</returns>
		public virtual ICollection<V> Get(object key)
		{
			ICollection<V> c = map[key];
			if (c == null)
			{
				c = emptyValue;
			}
			return c;
		}

		/// <summary>Adds the value to the Collection mapped to by the key.</summary>
		public virtual void Add(K key, V value)
		{
			if (treatCollectionsAsImmutable)
			{
				ICollection<V> newC = cf.NewCollection();
				ICollection<V> c = map[key];
				if (c != null)
				{
					Sharpen.Collections.AddAll(newC, c);
				}
				newC.Add(value);
				map[key] = newC;
			}
			else
			{
				// replacing the old collection
				ICollection<V> c = map[key];
				if (c == null)
				{
					c = cf.NewCollection();
					map[key] = c;
				}
				c.Add(value);
			}
		}

		// modifying the old collection
		/// <summary>Adds the values to the Collection mapped to by the key.</summary>
		public virtual void AddAll(K key, ICollection<V> values)
		{
			if (treatCollectionsAsImmutable)
			{
				ICollection<V> newC = cf.NewCollection();
				ICollection<V> c = map[key];
				if (c != null)
				{
					Sharpen.Collections.AddAll(newC, c);
				}
				Sharpen.Collections.AddAll(newC, values);
				map[key] = newC;
			}
			else
			{
				// replacing the old collection
				ICollection<V> c = map[key];
				if (c == null)
				{
					c = cf.NewCollection();
					map[key] = c;
				}
				Sharpen.Collections.AddAll(c, values);
			}
		}

		// modifying the old collection
		/// <summary>Just add the key (empty collection, but key is in the keySet).</summary>
		public virtual void AddKey(K key)
		{
			ICollection<V> c = map[key];
			if (c == null)
			{
				c = cf.NewCollection();
				map[key] = c;
			}
		}

		/// <summary>Adds all of the mappings in m to this CollectionValuedMap.</summary>
		/// <remarks>
		/// Adds all of the mappings in m to this CollectionValuedMap. If m is a
		/// CollectionValuedMap, it will behave strangely. Use the constructor instead.
		/// </remarks>
		public virtual void AddAll(IDictionary<K, V> m)
		{
			if (m is Edu.Stanford.Nlp.Util.CollectionValuedMap<object, object>)
			{
				throw new NotSupportedException();
			}
			foreach (KeyValuePair<K, V> e in m)
			{
				Add(e.Key, e.Value);
			}
		}

		public virtual void AddAll(Edu.Stanford.Nlp.Util.CollectionValuedMap<K, V> cvm)
		{
			foreach (KeyValuePair<K, ICollection<V>> entry in cvm)
			{
				K key = entry.Key;
				ICollection<V> currentCollection = this[key];
				ICollection<V> newValues = entry.Value;
				if (treatCollectionsAsImmutable)
				{
					ICollection<V> newCollection = cf.NewCollection();
					if (currentCollection != null)
					{
						Sharpen.Collections.AddAll(newCollection, currentCollection);
					}
					Sharpen.Collections.AddAll(newCollection, newValues);
					map[key] = newCollection;
				}
				else
				{
					// replacing the old collection
					bool needToAdd = false;
					if (currentCollection == emptyValue)
					{
						currentCollection = cf.NewCollection();
						needToAdd = true;
					}
					Sharpen.Collections.AddAll(currentCollection, newValues);
					// modifying the old collection
					if (needToAdd)
					{
						map[key] = currentCollection;
					}
				}
			}
		}

		/// <summary>Removes the mapping associated with this key from this Map.</summary>
		/// <returns>the Collection mapped to by this key.</returns>
		public virtual ICollection<V> Remove(object key)
		{
			return Sharpen.Collections.Remove(map, key);
		}

		/// <summary>Removes the mappings associated with the keys from this map.</summary>
		/// <param name="keys">They keys to remove</param>
		public virtual void RemoveAll(ICollection<K> keys)
		{
			foreach (K k in keys)
			{
				Sharpen.Collections.Remove(this, k);
			}
		}

		/// <summary>
		/// Removes the value from the Collection mapped to by this key, leaving the
		/// rest of the collection intact.
		/// </summary>
		/// <param name="key">The key to the Collection to remove the value from</param>
		/// <param name="value">The value to remove</param>
		public virtual void RemoveMapping(K key, V value)
		{
			if (treatCollectionsAsImmutable)
			{
				ICollection<V> c = map[key];
				if (c != null)
				{
					ICollection<V> newC = cf.NewCollection();
					Sharpen.Collections.AddAll(newC, c);
					newC.Remove(value);
					map[key] = newC;
				}
			}
			else
			{
				ICollection<V> c = this[key];
				c.Remove(value);
			}
		}

		/// <summary>Clears this Map.</summary>
		public virtual void Clear()
		{
			map.Clear();
		}

		/// <returns>true iff this key is in this map</returns>
		public virtual bool Contains(object key)
		{
			return map.Contains(key);
		}

		/// <summary>Unsupported.</summary>
		public virtual bool ContainsValue(object value)
		{
			throw new NotSupportedException();
		}

		/// <returns>true iff this Map has no mappings in it.</returns>
		public virtual bool IsEmpty()
		{
			return map.IsEmpty();
		}

		/// <summary>
		/// Each element of the Set is a Map.Entry object, where getKey() returns the
		/// key of the mapping, and getValue() returns the Collection mapped to by the
		/// key.
		/// </summary>
		/// <returns>a Set view of the mappings contained in this map.</returns>
		public virtual ICollection<KeyValuePair<K, ICollection<V>>> EntrySet()
		{
			return map;
		}

		/// <returns>a Set view of the keys in this Map.</returns>
		public virtual ICollection<K> Keys
		{
			get
			{
				return map.Keys;
			}
		}

		/// <summary>The number of keys in this map.</summary>
		public virtual int Count
		{
			get
			{
				return map.Count;
			}
		}

		/// <returns>
		/// a collection of the values (really, a collection of values) in this
		/// Map
		/// </returns>
		public virtual ICollection<ICollection<V>> Values
		{
			get
			{
				return map.Values;
			}
		}

		public virtual ICollection<V> AllValues()
		{
			ICollection<V> c = cf.NewCollection();
			foreach (ICollection<V> c1 in map.Values)
			{
				Sharpen.Collections.AddAll(c, c1);
			}
			return c;
		}

		/// <returns>
		/// true iff o is a CollectionValuedMap, and each key maps to the a
		/// Collection of the same objects in o as it does in this
		/// CollectionValuedMap.
		/// </returns>
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Util.CollectionValuedMap<object, object>))
			{
				return false;
			}
			Edu.Stanford.Nlp.Util.CollectionValuedMap<K, V> other = ErasureUtils.UncheckedCast(o);
			if (other.Count != Count)
			{
				return false;
			}
			try
			{
				foreach (KeyValuePair<K, ICollection<V>> e in this)
				{
					K key = e.Key;
					ICollection<V> value = e.Value;
					if (value == null)
					{
						if (!(other[key] == null && other.Contains(key)))
						{
							return false;
						}
					}
					else
					{
						if (!value.Equals(other[key]))
						{
							return false;
						}
					}
				}
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		/// <returns>the hashcode of the underlying Map</returns>
		public override int GetHashCode()
		{
			return map.GetHashCode();
		}

		/// <summary>
		/// Creates a "delta copy" of this Map, where only the differences
		/// from the original Map are represented.
		/// </summary>
		/// <remarks>
		/// Creates a "delta copy" of this Map, where only the differences
		/// from the original Map are represented. (This typically assumes
		/// that this map will no longer be changed.)
		/// </remarks>
		public virtual Edu.Stanford.Nlp.Util.CollectionValuedMap<K, V> DeltaCopy()
		{
			IDictionary<K, ICollection<V>> deltaMap = new DeltaMap<K, ICollection<V>>(this.map);
			return new Edu.Stanford.Nlp.Util.CollectionValuedMap<K, V>(null, cf, true, deltaMap);
		}

		/// <returns>
		/// A String representation of this CollectionValuedMap, with special
		/// machinery to avoid recursion problems
		/// </returns>
		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append('{');
			IEnumerator<KeyValuePair<K, ICollection<V>>> i = this.GetEnumerator();
			while (i.MoveNext())
			{
				KeyValuePair<K, ICollection<V>> e = i.Current;
				K key = e.Key;
				ICollection<V> value = e.Value;
				buf.Append(key == this ? "(this Map)" : key).Append('=').Append(value == this ? "(this Map)" : value);
				if (i.MoveNext())
				{
					buf.Append(", ");
				}
			}
			buf.Append('}');
			return buf.ToString();
		}

		/// <summary>Creates a new empty CollectionValuedMap.</summary>
		/// <param name="mf">A MapFactory which will be used to generate the underlying Map</param>
		/// <param name="cf">
		/// A CollectionFactory which will be used to generate the Collections
		/// in each mapping
		/// </param>
		/// <param name="treatCollectionsAsImmutable">
		/// If true, forces this Map to create new a Collection every time a
		/// new value is added to or deleted from the Collection a mapping.
		/// </param>
		public CollectionValuedMap(MapFactory<K, ICollection<V>> mf, CollectionFactory<V> cf, bool treatCollectionsAsImmutable)
			: this(mf, cf, treatCollectionsAsImmutable, null)
		{
		}

		/// <summary>Creates a new CollectionValuedMap.</summary>
		/// <param name="mf">A MapFactory which will be used to generate the underlying Map</param>
		/// <param name="cf">
		/// A CollectionFactory which will be used to generate the Collections
		/// in each mapping
		/// </param>
		/// <param name="treatCollectionsAsImmutable">
		/// If true, forces this Map to create new a Collection every time a
		/// new value is added to or deleted from the Collection a mapping.
		/// </param>
		/// <param name="map">
		/// An existing map to use rather than initializing one with mf. If this is non-null it is
		/// used to initialize the map rather than mf.
		/// </param>
		private CollectionValuedMap(MapFactory<K, ICollection<V>> mf, CollectionFactory<V> cf, bool treatCollectionsAsImmutable, IDictionary<K, ICollection<V>> map)
		{
			if (cf == null)
			{
				throw new ArgumentException();
			}
			if (mf == null && map == null)
			{
				throw new ArgumentException();
			}
			this.mf = mf;
			this.cf = cf;
			this.treatCollectionsAsImmutable = treatCollectionsAsImmutable;
			this.emptyValue = cf.NewEmptyCollection();
			if (map != null)
			{
				this.map = map;
			}
			else
			{
				this.map = Java.Util.Collections.SynchronizedMap(mf.NewMap());
			}
		}

		/// <summary>Creates a new CollectionValuedMap with all of the mappings from cvm.</summary>
		/// <param name="cvm">The CollectionValueMap to copy as this object.</param>
		public CollectionValuedMap(Edu.Stanford.Nlp.Util.CollectionValuedMap<K, V> cvm)
		{
			this.mf = cvm.mf;
			this.cf = cvm.cf;
			this.treatCollectionsAsImmutable = cvm.treatCollectionsAsImmutable;
			this.emptyValue = cvm.emptyValue;
			map = Java.Util.Collections.SynchronizedMap(mf.NewMap());
			foreach (KeyValuePair<K, ICollection<V>> entry in cvm.map)
			{
				K key = entry.Key;
				ICollection<V> c = entry.Value;
				foreach (V value in c)
				{
					Add(key, value);
				}
			}
		}

		/// <summary>
		/// Creates a new empty CollectionValuedMap which uses a HashMap as the
		/// underlying Map, and HashSets as the Collections in each mapping.
		/// </summary>
		/// <remarks>
		/// Creates a new empty CollectionValuedMap which uses a HashMap as the
		/// underlying Map, and HashSets as the Collections in each mapping. Does not
		/// treat Collections as immutable.
		/// </remarks>
		public CollectionValuedMap()
			: this(MapFactory.HashMapFactory(), CollectionFactory.HashSetFactory(), false)
		{
		}

		/// <summary>
		/// Creates a new empty CollectionValuedMap which uses a HashMap as the
		/// underlying Map.
		/// </summary>
		/// <remarks>
		/// Creates a new empty CollectionValuedMap which uses a HashMap as the
		/// underlying Map. Does not treat Collections as immutable.
		/// </remarks>
		/// <param name="cf">
		/// A CollectionFactory which will be used to generate the Collections
		/// in each mapping
		/// </param>
		public CollectionValuedMap(CollectionFactory<V> cf)
			: this(MapFactory.HashMapFactory(), cf, false)
		{
		}
	}
}
