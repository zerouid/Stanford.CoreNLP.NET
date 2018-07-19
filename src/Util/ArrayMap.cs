using System;
using System.Collections;
using System.Collections.Generic;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Map backed by an Array.</summary>
	/// <author>Dan Klein</author>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public sealed class ArrayMap<K, V> : AbstractMap<K, V>
	{
		private const long serialVersionUID = 1L;

		private ArrayMap.Entry<K, V>[] entryArray;

		private int capacity;

		private int size;

		[System.Serializable]
		internal sealed class Entry<K, V> : KeyValuePair<K, V>
		{
			private const long serialVersionUID = 1L;

			private readonly K key;

			private V value;

			public K Key
			{
				get
				{
					return key;
				}
			}

			public V Value
			{
				get
				{
					return value;
				}
			}

			public V SetValue(V o)
			{
				V old = value;
				value = o;
				return old;
			}

			public override int GetHashCode()
			{
				return (Key == null ? 0 : Key.GetHashCode()) ^ (Value == null ? 0 : Value.GetHashCode());
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is ArrayMap.Entry))
				{
					return false;
				}
				ArrayMap.Entry e = (ArrayMap.Entry)o;
				return (Key == null ? e.Key == null : Key.Equals(e.Key)) && (Value == null ? e.Value == null : Value.Equals(e.Value));
			}

			internal Entry(K key, V value)
			{
				this.key = key;
				this.value = value;
			}

			public override string ToString()
			{
				return key + "=" + value;
			}
		}

		public ArrayMap()
		{
			size = 0;
			capacity = 2;
			entryArray = new ArrayMap.Entry[2];
		}

		public ArrayMap(int capacity)
		{
			size = 0;
			this.capacity = capacity;
			entryArray = new ArrayMap.Entry[capacity];
		}

		public ArrayMap(IDictionary<K, V> m)
		{
			size = 0;
			capacity = m.Count;
			entryArray = new ArrayMap.Entry[m.Count];
			this.PutAll(m);
		}

		public ArrayMap(K[] keys, V[] values)
		{
			if (keys.Length != values.Length)
			{
				throw new ArgumentException("different number of keys and values.");
			}
			size = keys.Length;
			capacity = size;
			entryArray = new ArrayMap.Entry[size];
			for (int i = 0; i < keys.Length; i++)
			{
				entryArray[i] = new ArrayMap.Entry(keys[i], values[i]);
			}
		}

		public static ArrayMap<K, V> NewArrayMap<K, V>()
		{
			return new ArrayMap<K, V>();
		}

		public static ArrayMap<K, V> NewArrayMap<K, V>(int capacity)
		{
			return new ArrayMap<K, V>(capacity);
		}

		public override ICollection<KeyValuePair<K, V>> EntrySet()
		{
			//throw new java.lang.UnsupportedOperationException();
			return new _HashSet_116(this, Arrays.AsList(entryArray).SubList(0, size));
		}

		private sealed class _HashSet_116 : HashSet<KeyValuePair<K, V>>
		{
			public _HashSet_116(ArrayMap<K, V> _enclosing, ICollection<KeyValuePair<K, V>> baseArg1)
				: base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.serialVersionUID = 2746535724049192751L;
			}

			private const long serialVersionUID;

			public override bool Remove(object o)
			{
				if (o is DictionaryEntry)
				{
					DictionaryEntry entry = (DictionaryEntry)o;
					Sharpen.Collections.Remove(this._enclosing._enclosing, entry.Key);
					return base.Remove(o);
				}
				else
				{
					return false;
				}
			}

			public override void Clear()
			{
				base.Clear();
				this._enclosing._enclosing.Clear();
			}

			private readonly ArrayMap<K, V> _enclosing;
		}

		public override int Count
		{
			get
			{
				return size;
			}
		}

		public override bool IsEmpty()
		{
			return size == 0;
		}

		private void Resize()
		{
			ArrayMap.Entry<K, V>[] oldEntryArray = entryArray;
			int newCapacity = 2 * size;
			if (newCapacity == 0)
			{
				newCapacity = 1;
			}
			entryArray = new ArrayMap.Entry[newCapacity];
			System.Array.Copy(oldEntryArray, 0, entryArray, 0, size);
			capacity = newCapacity;
		}

		public override void Clear()
		{
			size = 0;
		}

		public override V Put(K key, V val)
		{
			for (int i = 0; i < size; i++)
			{
				if (key.Equals(entryArray[i].Key))
				{
					return entryArray[i].SetValue(val);
				}
			}
			if (capacity <= size)
			{
				Resize();
			}
			entryArray[size] = new ArrayMap.Entry<K, V>(key, val);
			size++;
			return null;
		}

		public override V Get(object key)
		{
			for (int i = 0; i < size; i++)
			{
				if (key == null ? entryArray[i].Key == null : key.Equals(entryArray[i].Key))
				{
					return entryArray[i].Value;
				}
			}
			return null;
		}

		public override V Remove(object key)
		{
			for (int i = 0; i < size; i++)
			{
				if (key == null ? entryArray[i].Key == null : key.Equals(entryArray[i].Key))
				{
					V value = entryArray[i].Value;
					if (size > 1)
					{
						entryArray[i] = entryArray[size - 1];
					}
					size--;
					return value;
				}
			}
			return null;
		}

		protected internal int hashCodeCache;

		// = 0;
		public override int GetHashCode()
		{
			if (hashCodeCache == 0)
			{
				// this is now the djb2 (Dan Bernstein) hash; it used to be the awful K&R 1st ed. hash which simply summed hash codes, but that's very bad form.
				int hashCode = 5381;
				for (int i = 0; i < size; i++)
				{
					hashCode = hashCode * 33 + entryArray[i].GetHashCode();
				}
				hashCodeCache = hashCode;
			}
			return hashCodeCache;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is IDictionary))
			{
				return false;
			}
			IDictionary<K, V> m = (IDictionary<K, V>)o;
			for (int i = 0; i < size; i++)
			{
				object mVal = m[entryArray[i].Key];
				if (mVal == null)
				{
					if (entryArray[i] != null)
					{
						return false;
					}
					else
					{
						continue;
					}
				}
				if (!m[entryArray[i].Key].Equals(entryArray[i].Value))
				{
					return false;
				}
			}
			return true;
		}
	}
}
