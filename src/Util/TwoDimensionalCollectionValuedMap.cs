using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A class which can store mappings from Object keys to
	/// <see cref="System.Collections.ICollection{E}"/>
	/// s of Object values.
	/// Important methods are the
	/// <see cref="TwoDimensionalCollectionValuedMap{K1, K2, V}.Add(object, object, object)"/>
	/// and for adding a value
	/// to/from the Collection associated with the key, and the
	/// <see cref="TwoDimensionalCollectionValuedMap{K1, K2, V}.Get(object, object)"/>
	/// method for
	/// getting the Collection associated with a key.
	/// The class is quite general, because on construction, it is possible to pass a
	/// <see cref="MapFactory{K, V}"/>
	/// which will be used to create the underlying map and a
	/// <see cref="CollectionFactory{T}"/>
	/// which will
	/// be used to create the Collections. Thus this class can be configured to act like a "HashSetValuedMap"
	/// or a "ListValuedMap", or even a "HashSetValuedIdentityHashMap". The possibilities are endless!
	/// </summary>
	/// <author>Teg Grenager (grenager@cs.stanford.edu)</author>
	[System.Serializable]
	public class TwoDimensionalCollectionValuedMap<K1, K2, V>
	{
		private const long serialVersionUID = 1L;

		private IDictionary<K1, CollectionValuedMap<K2, V>> map = Generics.NewHashMap();

		protected internal MapFactory<K2, ICollection<V>> mf;

		protected internal CollectionFactory<V> cf;

		private bool treatCollectionsAsImmutable;

		/// <summary>
		/// Creates a new empty TwoDimensionalCollectionValuedMap which uses a HashMap as the
		/// underlying Map, and HashSets as the Collections in each mapping.
		/// </summary>
		/// <remarks>
		/// Creates a new empty TwoDimensionalCollectionValuedMap which uses a HashMap as the
		/// underlying Map, and HashSets as the Collections in each mapping. Does not
		/// treat Collections as immutable.
		/// </remarks>
		public TwoDimensionalCollectionValuedMap()
			: this(MapFactory.HashMapFactory<K2, ICollection<V>>(), CollectionFactory.HashSetFactory<V>(), false)
		{
		}

		/// <summary>
		/// Creates a new empty TwoDimensionalCollectionValuedMap which uses a HashMap as the
		/// underlying Map.
		/// </summary>
		/// <remarks>
		/// Creates a new empty TwoDimensionalCollectionValuedMap which uses a HashMap as the
		/// underlying Map.  Does not treat Collections as immutable.
		/// </remarks>
		/// <param name="cf">
		/// a CollectionFactory which will be used to generate the
		/// Collections in each mapping
		/// </param>
		public TwoDimensionalCollectionValuedMap(CollectionFactory<V> cf)
			: this(MapFactory.HashMapFactory<K2, ICollection<V>>(), cf, false)
		{
		}

		/// <summary>Creates a new empty TwoDimensionalCollectionValuedMap.</summary>
		/// <remarks>
		/// Creates a new empty TwoDimensionalCollectionValuedMap.
		/// Does not treat Collections as immutable.
		/// </remarks>
		/// <param name="mf">a MapFactory which will be used to generate the underlying Map</param>
		/// <param name="cf">a CollectionFactory which will be used to generate the Collections in each mapping</param>
		public TwoDimensionalCollectionValuedMap(MapFactory<K2, ICollection<V>> mf, CollectionFactory<V> cf)
			: this(mf, cf, false)
		{
		}

		/// <summary>Creates a new empty TwoDimensionalCollectionValuedMap.</summary>
		/// <param name="mf">a MapFactory which will be used to generate the underlying Map</param>
		/// <param name="cf">a CollectionFactory which will be used to generate the Collections in each mapping</param>
		/// <param name="treatCollectionsAsImmutable">
		/// if true, forces this Map to create new a Collection everytime
		/// a new value is added to or deleted from the Collection a mapping.
		/// </param>
		public TwoDimensionalCollectionValuedMap(MapFactory<K2, ICollection<V>> mf, CollectionFactory<V> cf, bool treatCollectionsAsImmutable)
		{
			this.mf = mf;
			this.cf = cf;
			this.treatCollectionsAsImmutable = treatCollectionsAsImmutable;
		}

		public override string ToString()
		{
			return map.ToString();
		}

		public virtual void PutAll(IDictionary<K1, CollectionValuedMap<K2, V>> toAdd)
		{
			map.PutAll(toAdd);
		}

		/// <returns>the Collection mapped to by key, never null, but may be empty.</returns>
		public virtual CollectionValuedMap<K2, V> GetCollectionValuedMap(K1 key1)
		{
			CollectionValuedMap<K2, V> cvm = map[key1];
			if (cvm == null)
			{
				cvm = new CollectionValuedMap<K2, V>(mf, cf, treatCollectionsAsImmutable);
				map[key1] = cvm;
			}
			return cvm;
		}

		public virtual ICollection<V> Get(K1 key1, K2 key2)
		{
			return GetCollectionValuedMap(key1)[key2];
		}

		/// <summary>Adds the value to the Collection mapped to by the key.</summary>
		public virtual void Add(K1 key1, K2 key2, V value)
		{
			CollectionValuedMap<K2, V> cvm = map[key1];
			if (cvm == null)
			{
				cvm = new CollectionValuedMap<K2, V>(mf, cf, treatCollectionsAsImmutable);
				map[key1] = cvm;
			}
			cvm.Add(key2, value);
		}

		/// <summary>Adds a collection of values to the Collection mapped to by the key.</summary>
		public virtual void Add(K1 key1, K2 key2, ICollection<V> value)
		{
			CollectionValuedMap<K2, V> cvm = map[key1];
			if (cvm == null)
			{
				cvm = new CollectionValuedMap<K2, V>(mf, cf, treatCollectionsAsImmutable);
				map[key1] = cvm;
			}
			foreach (V v in value)
			{
				cvm.Add(key2, v);
			}
		}

		/// <summary>yes, this is a weird method, but i need it.</summary>
		public virtual void AddKey(K1 key1)
		{
			CollectionValuedMap<K2, V> cvm = map[key1];
			if (cvm == null)
			{
				cvm = new CollectionValuedMap<K2, V>(mf, cf, treatCollectionsAsImmutable);
				map[key1] = cvm;
			}
		}

		public virtual void Clear()
		{
			map.Clear();
		}

		/// <returns>a Set view of the keys in this Map.</returns>
		public virtual ICollection<K1> KeySet()
		{
			return map.Keys;
		}

		public virtual ICollection<KeyValuePair<K1, CollectionValuedMap<K2, V>>> EntrySet()
		{
			return map;
		}

		public virtual bool ContainsKey(K1 key)
		{
			return map.Contains(key);
		}

		public virtual void RetainAll(ICollection<K1> keys)
		{
			foreach (K1 key in new LinkedList<K1>(map.Keys))
			{
				if (!keys.Contains(key))
				{
					Sharpen.Collections.Remove(map, key);
				}
			}
		}

		public virtual ICollection<K1> FirstKeySet()
		{
			return KeySet();
		}

		public virtual ICollection<K2> SecondKeySet()
		{
			ICollection<K2> keys = Generics.NewHashSet();
			foreach (K1 k1 in map.Keys)
			{
				Sharpen.Collections.AddAll(keys, GetCollectionValuedMap(k1).Keys);
			}
			return keys;
		}

		public virtual ICollection<V> Values()
		{
			ICollection<V> allValues = Generics.NewHashSet();
			foreach (K1 k1 in map.Keys)
			{
				ICollection<ICollection<V>> collectionOfValues = GetCollectionValuedMap(k1).Values;
				foreach (ICollection<V> values in collectionOfValues)
				{
					Sharpen.Collections.AddAll(allValues, values);
				}
			}
			return allValues;
		}
	}
}
