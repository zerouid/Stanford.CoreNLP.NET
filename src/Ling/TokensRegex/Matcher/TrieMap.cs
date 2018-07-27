using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Ling.Tokensregex.Matcher
{
	/// <summary>Map that takes an Iterable as key, and maps it to an value.</summary>
	/// <remarks>
	/// Map that takes an Iterable as key, and maps it to an value.
	/// This implementation is not particularly memory efficient, but will have relatively
	/// fast lookup times for sequences where there are many possible keys (e.g. sequences over Strings).
	/// Can be used for fairly efficient look up of a sequence by prefix.
	/// </remarks>
	/// <author>Angel Chang</author>
	/// <?/>
	/// <?/>
	public class TrieMap<K, V> : AbstractMap<IEnumerable<K>, V>
	{
		/// <summary>Child tries</summary>
		protected internal IDictionary<K, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V>> children;

		/// <summary>Value at a leaf node (leaf node is indicated by non-null value)</summary>
		protected internal V value;

		public TrieMap()
		{
		}

		public TrieMap(int initialCapacity)
		{
		}

		// Should we have explicit marking if this element is a leaf node without requiring value?
		// TODO: initial capacity implementation
		// Trie specific functions
		public virtual Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> GetChildTrie(K key)
		{
			return (children != null) ? children[key] : null;
		}

		public virtual Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> GetChildTrie(IEnumerable<K> key)
		{
			Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> curTrie = this;
			// go through each element
			foreach (K element in key)
			{
				curTrie = (curTrie.children != null) ? curTrie.children[element] : null;
				if (curTrie == null)
				{
					return null;
				}
			}
			return curTrie;
		}

		public virtual Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> PutChildTrie(IEnumerable<K> key, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> child)
		{
			Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> parentTrie = null;
			Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> curTrie = this;
			IEnumerator<K> keyIter = key.GetEnumerator();
			// go through each element
			while (keyIter.MoveNext())
			{
				K element = keyIter.Current;
				bool isLast = !keyIter.MoveNext();
				if (curTrie.children == null)
				{
					curTrie.children = new ConcurrentHashMap<K, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V>>();
				}
				//Generics.newConcurrentHashMap();
				parentTrie = curTrie;
				curTrie = curTrie.children[element];
				if (isLast)
				{
					parentTrie.children[element] = child;
				}
				else
				{
					if (curTrie == null)
					{
						parentTrie.children[element] = curTrie = new Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V>();
					}
				}
			}
			if (parentTrie == null)
			{
				throw new ArgumentException("Cannot put a child trie with no keys");
			}
			return curTrie;
		}

		public virtual IDictionary<K, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V>> GetChildren()
		{
			return children;
		}

		public virtual V GetValue()
		{
			return value;
		}

		public virtual bool IsLeaf()
		{
			return value != null;
		}

		public virtual string ToFormattedString()
		{
			IList<string> strings = new LinkedList<string>();
			UpdateTrieStrings(strings, string.Empty);
			return StringUtils.Join(strings, "\n");
		}

		protected internal virtual void UpdateTrieStrings(IList<string> strings, string prefix)
		{
			if (children != null)
			{
				foreach (KeyValuePair<K, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V>> kTrieMapEntry in children)
				{
					kTrieMapEntry.Value.UpdateTrieStrings(strings, prefix + " - " + kTrieMapEntry.Key);
				}
			}
			if (IsLeaf())
			{
				strings.Add(prefix + " -> " + value);
			}
		}

		public override int Count
		{
			get
			{
				// Functions to support map interface to lookup using sequence
				int s = 0;
				if (children != null)
				{
					foreach (KeyValuePair<K, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V>> kTrieMapEntry in children)
					{
						s += kTrieMapEntry.Value.Count;
					}
				}
				if (IsLeaf())
				{
					s++;
				}
				return s;
			}
		}

		public override bool IsEmpty()
		{
			return (children == null && !IsLeaf());
		}

		public override bool Contains(object key)
		{
			return this[key] != null;
		}

		public override bool ContainsValue(object value)
		{
			return Values.Contains(value);
		}

		public override V Get(object key)
		{
			if (key is IEnumerable)
			{
				return Get((IEnumerable<K>)key);
			}
			else
			{
				if (key is object[])
				{
					return this[Arrays.AsList((object[])key)];
				}
			}
			return null;
		}

		public virtual V Get(IEnumerable<K> key)
		{
			Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> curTrie = GetChildTrie(key);
			return (curTrie != null) ? curTrie.value : null;
		}

		public virtual V Get(K[] key)
		{
			return Get(Arrays.AsList(key));
		}

		public override V Put(IEnumerable<K> key, V value)
		{
			if (value == null)
			{
				throw new ArgumentException("Value cannot be null");
			}
			Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> curTrie = this;
			// go through each element
			foreach (K element in key)
			{
				if (curTrie.children == null)
				{
					curTrie.children = new ConcurrentHashMap<K, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V>>();
				}
				//Generics.newConcurrentHashMap();
				Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> parent = curTrie;
				curTrie = curTrie.children[element];
				if (curTrie == null)
				{
					parent.children[element] = curTrie = new Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V>();
				}
			}
			V oldValue = curTrie.value;
			curTrie.value = value;
			return oldValue;
		}

		public virtual V Put(K[] key, V value)
		{
			return this[Arrays.AsList(key)] = value;
		}

		public override V Remove(object key)
		{
			if (key is IEnumerable)
			{
				return Remove((IEnumerable)key);
			}
			return null;
		}

		public virtual V Remove(IEnumerable key)
		{
			Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> parent = null;
			Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V> curTrie = this;
			object lastKey = null;
			// go through each element
			foreach (object element in key)
			{
				if (curTrie.children == null)
				{
					return null;
				}
				lastKey = element;
				parent = curTrie;
				curTrie = curTrie.children[element];
				if (curTrie == null)
				{
					return null;
				}
			}
			V v = curTrie.value;
			if (parent != null)
			{
				Sharpen.Collections.Remove(parent.children, lastKey);
			}
			else
			{
				value = null;
			}
			return v;
		}

		public virtual V Remove(K[] key)
		{
			return Remove(Arrays.AsList(key));
		}

		public override void PutAll<_T0>(IDictionary<_T0> m)
		{
			foreach (KeyValuePair<IEnumerable<K>, V> entry in m)
			{
				this[entry.Key] = entry.Value;
			}
		}

		public override void Clear()
		{
			value = null;
			children = null;
		}

		public override ICollection<IEnumerable<K>> Keys
		{
			get
			{
				ICollection<IEnumerable<K>> keys = new LinkedHashSet<IEnumerable<K>>();
				UpdateKeys(keys, new List<K>());
				return keys;
			}
		}

		protected internal virtual void UpdateKeys(ICollection<IEnumerable<K>> keys, IList<K> prefix)
		{
			if (children != null)
			{
				foreach (KeyValuePair<K, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V>> kTrieMapEntry in children)
				{
					IList<K> p = new List<K>(prefix.Count + 1);
					Sharpen.Collections.AddAll(p, prefix);
					p.Add(kTrieMapEntry.Key);
					kTrieMapEntry.Value.UpdateKeys(keys, p);
				}
			}
			if (value != null)
			{
				keys.Add(prefix);
			}
		}

		public override ICollection<V> Values
		{
			get
			{
				IList<V> values = new List<V>();
				UpdateValues(values);
				return values;
			}
		}

		protected internal virtual void UpdateValues(IList<V> values)
		{
			if (children != null)
			{
				foreach (KeyValuePair<K, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V>> kTrieMapEntry in children)
				{
					kTrieMapEntry.Value.UpdateValues(values);
				}
			}
			if (value != null)
			{
				values.Add(value);
			}
		}

		public override ICollection<KeyValuePair<IEnumerable<K>, V>> EntrySet()
		{
			ICollection<KeyValuePair<IEnumerable<K>, V>> entries = new LinkedHashSet<KeyValuePair<IEnumerable<K>, V>>();
			UpdateEntries(entries, new List<K>());
			return entries;
		}

		protected internal virtual void UpdateEntries(ICollection<KeyValuePair<IEnumerable<K>, V>> entries, IList<K> prefix)
		{
			if (children != null)
			{
				foreach (KeyValuePair<K, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMap<K, V>> kTrieMapEntry in children)
				{
					IList<K> p = new List<K>(prefix.Count + 1);
					Sharpen.Collections.AddAll(p, prefix);
					p.Add(kTrieMapEntry.Key);
					kTrieMapEntry.Value.UpdateEntries(entries, p);
				}
			}
			if (value != null)
			{
				entries.Add(new _KeyValuePair_289(this, prefix));
			}
		}

		private sealed class _KeyValuePair_289 : KeyValuePair<IEnumerable<K>, V>
		{
			public _KeyValuePair_289(TrieMap<K, V> _enclosing, IList<K> prefix)
			{
				this._enclosing = _enclosing;
				this.prefix = prefix;
			}

			public IEnumerable<K> Key
			{
				get
				{
					return prefix;
				}
			}

			public V Value
			{
				get
				{
					return this._enclosing.value;
				}
			}

			public V SetValue(V value)
			{
				throw new NotSupportedException();
			}

			private readonly TrieMap<K, V> _enclosing;

			private readonly IList<K> prefix;
		}
	}
}
