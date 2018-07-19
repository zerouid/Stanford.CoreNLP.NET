using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>This implements a map to a set of lists.</summary>
	/// <author>Eric Yeh</author>
	/// <?/>
	/// <?/>
	public class MapList<U, V>
	{
		protected internal IDictionary<U, IList<V>> map = Generics.NewHashMap();

		public MapList()
		{
		}

		public virtual void Add(U key, V val)
		{
			EnsureList(key).Add(val);
		}

		/// <summary>
		/// Using the iterator order of values in the value, adds the
		/// individual elements into the list under the given key.
		/// </summary>
		public virtual void Add(U key, ICollection<V> vals)
		{
			Sharpen.Collections.AddAll(EnsureList(key), vals);
		}

		public virtual int Size(U key)
		{
			if (map.Contains(key))
			{
				return map[key].Count;
			}
			return 0;
		}

		public virtual bool ContainsKey(U key)
		{
			return map.Contains(key);
		}

		public virtual ICollection<U> KeySet()
		{
			return map.Keys;
		}

		public virtual V Get(U key, int index)
		{
			if (map.Contains(key))
			{
				IList<V> list = map[key];
				if (index < list.Count)
				{
					return map[key][index];
				}
			}
			return null;
		}

		protected internal virtual IList<V> EnsureList(U key)
		{
			if (map.Contains(key))
			{
				return map[key];
			}
			IList<V> newList = new List<V>();
			map[key] = newList;
			return newList;
		}
	}
}
