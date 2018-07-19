using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <author>jrfinkel</author>
	[System.Serializable]
	public class ThreeDimensionalMap<K1, K2, K3, V>
	{
		private const long serialVersionUID = 1L;

		internal IDictionary<K1, TwoDimensionalMap<K2, K3, V>> map;

		public virtual int Size()
		{
			int size = 0;
			foreach (KeyValuePair<K1, TwoDimensionalMap<K2, K3, V>> entry in map)
			{
				size += entry.Value.Size();
			}
			return size;
		}

		public virtual bool IsEmpty()
		{
			foreach (KeyValuePair<K1, TwoDimensionalMap<K2, K3, V>> entry in map)
			{
				if (!entry.Value.IsEmpty())
				{
					return false;
				}
			}
			return true;
		}

		public virtual V Put(K1 key1, K2 key2, K3 key3, V value)
		{
			TwoDimensionalMap<K2, K3, V> m = GetTwoDimensionalMap(key1);
			return m.Put(key2, key3, value);
		}

		public virtual V Get(K1 key1, K2 key2, K3 key3)
		{
			return GetTwoDimensionalMap(key1).Get(key2, key3);
		}

		public virtual bool Contains(K1 key1, K2 key2, K3 key3)
		{
			if (!map.Contains(key1))
			{
				return false;
			}
			if (!map[key1].ContainsKey(key2))
			{
				return false;
			}
			if (!map[key1].Get(key2).Contains(key3))
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public virtual void Remove(K1 key1, K2 key2, K3 key3)
		{
			Sharpen.Collections.Remove(Get(key1, key2), key3);
		}

		public virtual IDictionary<K3, V> Get(K1 key1, K2 key2)
		{
			return Get(key1).Get(key2);
		}

		public virtual TwoDimensionalMap<K2, K3, V> Get(K1 key1)
		{
			return GetTwoDimensionalMap(key1);
		}

		public virtual TwoDimensionalMap<K2, K3, V> GetTwoDimensionalMap(K1 key1)
		{
			TwoDimensionalMap<K2, K3, V> m = map[key1];
			if (m == null)
			{
				m = new TwoDimensionalMap<K2, K3, V>();
				map[key1] = m;
			}
			return m;
		}

		public virtual ICollection<V> Values()
		{
			IList<V> s = Generics.NewArrayList();
			foreach (TwoDimensionalMap<K2, K3, V> innerMap in map.Values)
			{
				Sharpen.Collections.AddAll(s, innerMap.Values());
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
				Sharpen.Collections.AddAll(keys, Get(k1).FirstKeySet());
			}
			return keys;
		}

		public virtual ICollection<K3> ThirdKeySet()
		{
			ICollection<K3> keys = Generics.NewHashSet();
			foreach (K1 k1 in map.Keys)
			{
				TwoDimensionalMap<K2, K3, V> m = map[k1];
				foreach (K2 k2 in m.FirstKeySet())
				{
					Sharpen.Collections.AddAll(keys, m.Get(k2).Keys);
				}
			}
			return keys;
		}

		public ThreeDimensionalMap()
		{
			this.map = Generics.NewHashMap();
		}

		public override string ToString()
		{
			return map.ToString();
		}
	}
}
