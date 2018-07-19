using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A class which can store mappings from Object keys to
	/// <see cref="System.Collections.ICollection{E}"/>
	/// s of Object values.
	/// Important methods are the
	/// <see cref="ThreeDimensionalCollectionValuedMap{K1, K2, K3, V}.Add(object, object, object, object)"/>
	/// for adding a value
	/// to/from the Collection associated with the key, and the
	/// <see cref="ThreeDimensionalCollectionValuedMap{K1, K2, K3, V}.Get(object, object, object)"/>
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
	public class ThreeDimensionalCollectionValuedMap<K1, K2, K3, V>
	{
		private const long serialVersionUID = 1L;

		private IDictionary<K1, TwoDimensionalCollectionValuedMap<K2, K3, V>> map = Generics.NewHashMap();

		public override string ToString()
		{
			return map.ToString();
		}

		/// <returns>the Collection mapped to by key, never null, but may be empty.</returns>
		public virtual TwoDimensionalCollectionValuedMap<K2, K3, V> GetTwoDimensionalCollectionValuedMap(K1 key1)
		{
			TwoDimensionalCollectionValuedMap<K2, K3, V> cvm = map[key1];
			if (cvm == null)
			{
				cvm = new TwoDimensionalCollectionValuedMap<K2, K3, V>();
				map[key1] = cvm;
			}
			return cvm;
		}

		public virtual ICollection<V> Get(K1 key1, K2 key2, K3 key3)
		{
			return GetTwoDimensionalCollectionValuedMap(key1).GetCollectionValuedMap(key2)[key3];
		}

		/// <summary>Adds the value to the Collection mapped to by the key.</summary>
		public virtual void Add(K1 key1, K2 key2, K3 key3, V value)
		{
			TwoDimensionalCollectionValuedMap<K2, K3, V> cvm = GetTwoDimensionalCollectionValuedMap(key1);
			cvm.Add(key2, key3, value);
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

		public virtual bool ContainsKey(K1 key)
		{
			return map.Contains(key);
		}
	}
}
