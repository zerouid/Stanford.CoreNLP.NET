using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Util
{
	/// <author>jrfinkel</author>
	[System.Serializable]
	public class FiveDimensionalMap<K1, K2, K3, K4, K5, V>
	{
		private const long serialVersionUID = 1L;

		internal IDictionary<K1, FourDimensionalMap<K2, K3, K4, K5, V>> map;

		public virtual V Put(K1 key1, K2 key2, K3 key3, K4 key4, K5 key5, V value)
		{
			FourDimensionalMap<K2, K3, K4, K5, V> m = GetFourDimensionalMap(key1);
			return m.Put(key2, key3, key4, key5, value);
		}

		public virtual V Get(K1 key1, K2 key2, K3 key3, K4 key4, K5 key5)
		{
			return GetFourDimensionalMap(key1).Get(key2, key3, key4, key5);
		}

		public virtual IDictionary<K5, V> Get(K1 key1, K2 key2, K3 key3, K4 key4)
		{
			return Get(key1, key2, key3).Get(key4);
		}

		public virtual TwoDimensionalMap<K4, K5, V> Get(K1 key1, K2 key2, K3 key3)
		{
			return Get(key1, key2).Get(key3);
		}

		public virtual ThreeDimensionalMap<K3, K4, K5, V> Get(K1 key1, K2 key2)
		{
			return Get(key1).Get(key2);
		}

		public virtual FourDimensionalMap<K2, K3, K4, K5, V> Get(K1 key1)
		{
			return GetFourDimensionalMap(key1);
		}

		public virtual FourDimensionalMap<K2, K3, K4, K5, V> GetFourDimensionalMap(K1 key1)
		{
			FourDimensionalMap<K2, K3, K4, K5, V> m = map[key1];
			if (m == null)
			{
				m = new FourDimensionalMap<K2, K3, K4, K5, V>();
				map[key1] = m;
			}
			return m;
		}

		public virtual ICollection<V> Values()
		{
			IList<V> s = Generics.NewArrayList();
			foreach (FourDimensionalMap<K2, K3, K4, K5, V> innerMap in map.Values)
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
				FourDimensionalMap<K2, K3, K4, K5, V> m4 = map[k1];
				foreach (K2 k2 in m4.FirstKeySet())
				{
					Sharpen.Collections.AddAll(keys, m4.Get(k2).FirstKeySet());
				}
			}
			return keys;
		}

		public virtual ICollection<K4> FourthKeySet()
		{
			ICollection<K4> keys = Generics.NewHashSet();
			foreach (K1 k1 in map.Keys)
			{
				FourDimensionalMap<K2, K3, K4, K5, V> m4 = map[k1];
				foreach (K2 k2 in m4.FirstKeySet())
				{
					ThreeDimensionalMap<K3, K4, K5, V> m3 = m4.Get(k2);
					foreach (K3 k3 in m3.FirstKeySet())
					{
						Sharpen.Collections.AddAll(keys, m3.Get(k3).FirstKeySet());
					}
				}
			}
			return keys;
		}

		public virtual ICollection<K5> FifthKeySet()
		{
			ICollection<K5> keys = Generics.NewHashSet();
			foreach (K1 k1 in map.Keys)
			{
				FourDimensionalMap<K2, K3, K4, K5, V> m4 = map[k1];
				foreach (K2 k2 in m4.FirstKeySet())
				{
					ThreeDimensionalMap<K3, K4, K5, V> m3 = m4.Get(k2);
					foreach (K3 k3 in m3.FirstKeySet())
					{
						TwoDimensionalMap<K4, K5, V> m2 = m3.Get(k3);
						foreach (K4 k4 in m2.FirstKeySet())
						{
							Sharpen.Collections.AddAll(keys, m2.Get(k4).Keys);
						}
					}
				}
			}
			return keys;
		}

		public FiveDimensionalMap()
		{
			this.map = Generics.NewHashMap();
		}

		public override string ToString()
		{
			return map.ToString();
		}
	}
}
