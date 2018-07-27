using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Util
{
	/// <author>jrfinkel</author>
	[System.Serializable]
	public class FourDimensionalMap<K1, K2, K3, K4, V>
	{
		private const long serialVersionUID = 5635664746940978837L;

		internal IDictionary<K1, ThreeDimensionalMap<K2, K3, K4, V>> map;

		public virtual int Size()
		{
			return map.Count;
		}

		public virtual V Put(K1 key1, K2 key2, K3 key3, K4 key4, V value)
		{
			ThreeDimensionalMap<K2, K3, K4, V> m = GetThreeDimensionalMap(key1);
			return m.Put(key2, key3, key4, value);
		}

		public virtual V Get(K1 key1, K2 key2, K3 key3, K4 key4)
		{
			return GetThreeDimensionalMap(key1).Get(key2, key3, key4);
		}

		public virtual void Remove(K1 key1, K2 key2, K3 key3, K4 key4)
		{
			Sharpen.Collections.Remove(Get(key1, key2, key3), key4);
		}

		public virtual IDictionary<K4, V> Get(K1 key1, K2 key2, K3 key3)
		{
			return Get(key1, key2).Get(key3);
		}

		public virtual TwoDimensionalMap<K3, K4, V> Get(K1 key1, K2 key2)
		{
			return Get(key1).Get(key2);
		}

		public virtual ThreeDimensionalMap<K2, K3, K4, V> Get(K1 key1)
		{
			return GetThreeDimensionalMap(key1);
		}

		public virtual ThreeDimensionalMap<K2, K3, K4, V> GetThreeDimensionalMap(K1 key1)
		{
			ThreeDimensionalMap<K2, K3, K4, V> m = map[key1];
			if (m == null)
			{
				m = new ThreeDimensionalMap<K2, K3, K4, V>();
				map[key1] = m;
			}
			return m;
		}

		public virtual ICollection<V> Values()
		{
			IList<V> s = Generics.NewArrayList();
			foreach (ThreeDimensionalMap<K2, K3, K4, V> innerMap in map.Values)
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
				ThreeDimensionalMap<K2, K3, K4, V> m3 = map[k1];
				foreach (K2 k2 in m3.FirstKeySet())
				{
					Sharpen.Collections.AddAll(keys, m3.Get(k2).FirstKeySet());
				}
			}
			return keys;
		}

		public virtual ICollection<K4> FourthKeySet()
		{
			ICollection<K4> keys = Generics.NewHashSet();
			foreach (K1 k1 in map.Keys)
			{
				ThreeDimensionalMap<K2, K3, K4, V> m3 = map[k1];
				foreach (K2 k2 in m3.FirstKeySet())
				{
					TwoDimensionalMap<K3, K4, V> m2 = m3.Get(k2);
					foreach (K3 k3 in m2.FirstKeySet())
					{
						Sharpen.Collections.AddAll(keys, m2.Get(k3).Keys);
					}
				}
			}
			return keys;
		}

		public FourDimensionalMap()
		{
			this.map = Generics.NewHashMap();
		}

		public override string ToString()
		{
			return map.ToString();
		}
	}
}
