using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Wrap a TwoDimensionalMap as a TwoDimensionalSet.</summary>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class TwoDimensionalSet<K1, K2> : IEnumerable<Pair<K1, K2>>
	{
		private const long serialVersionUID = 2L;

		private readonly TwoDimensionalMap<K1, K2, bool> backingMap;

		public TwoDimensionalSet()
			: this(new TwoDimensionalMap<K1, K2, bool>())
		{
		}

		public TwoDimensionalSet(TwoDimensionalMap<K1, K2, bool> backingMap)
		{
			this.backingMap = backingMap;
		}

		public static Edu.Stanford.Nlp.Util.TwoDimensionalSet<K1, K2> TreeSet<K1, K2>()
		{
			return new Edu.Stanford.Nlp.Util.TwoDimensionalSet<K1, K2>(TwoDimensionalMap.TreeMap<K1, K2, bool>());
		}

		public static Edu.Stanford.Nlp.Util.TwoDimensionalSet<K1, K2> HashSet<K1, K2>()
		{
			return new Edu.Stanford.Nlp.Util.TwoDimensionalSet<K1, K2>(TwoDimensionalMap.HashMap<K1, K2, bool>());
		}

		public virtual bool Add(K1 k1, K2 k2)
		{
			return (backingMap.Put(k1, k2, true) != null);
		}

		public virtual bool AddAll<_T0>(Edu.Stanford.Nlp.Util.TwoDimensionalSet<_T0> set)
			where _T0 : K1
		{
			bool result = false;
			foreach (Pair<K1, K2> pair in set)
			{
				if (Add(pair.first, pair.second))
				{
					result = true;
				}
			}
			return result;
		}

		/// <summary>Adds all the keys in the given TwoDimensionalMap.</summary>
		/// <remarks>Adds all the keys in the given TwoDimensionalMap.  Returns true iff at least one key is added.</remarks>
		public virtual bool AddAllKeys<_T0>(TwoDimensionalMap<_T0> map)
			where _T0 : K1
		{
			bool result = false;
			foreach (TwoDimensionalMap.Entry<K1, K2, object> entry in map)
			{
				if (Add(entry.GetFirstKey(), entry.GetSecondKey()))
				{
					result = true;
				}
			}
			return result;
		}

		public virtual void Clear()
		{
			backingMap.Clear();
		}

		public virtual bool Contains(K1 k1, K2 k2)
		{
			return backingMap.Contains(k1, k2);
		}

		public virtual bool ContainsAll<_T0>(Edu.Stanford.Nlp.Util.TwoDimensionalSet<_T0> set)
			where _T0 : K1
		{
			foreach (Pair<K1, K2> pair in set)
			{
				if (!Contains(pair.first, pair.second))
				{
					return false;
				}
			}
			return true;
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Util.TwoDimensionalSet))
			{
				return false;
			}
			Edu.Stanford.Nlp.Util.TwoDimensionalSet<object, object> other = (Edu.Stanford.Nlp.Util.TwoDimensionalSet)o;
			return backingMap.Equals(other.backingMap);
		}

		public override int GetHashCode()
		{
			return backingMap.GetHashCode();
		}

		public virtual bool IsEmpty()
		{
			return backingMap.IsEmpty();
		}

		public virtual bool Remove(K1 k1, K2 k2)
		{
			return backingMap.Remove(k1, k2);
		}

		public virtual bool RemoveAll<_T0>(Edu.Stanford.Nlp.Util.TwoDimensionalSet<_T0> set)
			where _T0 : K1
		{
			bool removed = false;
			foreach (Pair<K1, K2> pair in set)
			{
				if (Remove(pair.first, pair.second))
				{
					removed = true;
				}
			}
			return removed;
		}

		public virtual int Size()
		{
			return backingMap.Size();
		}

		public virtual ICollection<K1> FirstKeySet()
		{
			return backingMap.FirstKeySet();
		}

		public virtual ICollection<K2> SecondKeySet(K1 k1)
		{
			return backingMap.GetMap(k1).Keys;
		}

		/// <summary>Iterate over the map using the iterator and entry inner classes.</summary>
		public virtual IEnumerator<Pair<K1, K2>> GetEnumerator()
		{
			return new TwoDimensionalSet.TwoDimensionalSetIterator<K1, K2>(this);
		}

		internal class TwoDimensionalSetIterator<K1, K2> : IEnumerator<Pair<K1, K2>>
		{
			internal IEnumerator<TwoDimensionalMap.Entry<K1, K2, bool>> backingIterator;

			internal TwoDimensionalSetIterator(TwoDimensionalSet<K1, K2> set)
			{
				backingIterator = set.backingMap.GetEnumerator();
			}

			public virtual bool MoveNext()
			{
				return backingIterator.MoveNext();
			}

			public virtual Pair<K1, K2> Current
			{
				get
				{
					TwoDimensionalMap.Entry<K1, K2, bool> entry = backingIterator.Current;
					return Pair.MakePair(entry.GetFirstKey(), entry.GetSecondKey());
				}
			}

			public virtual void Remove()
			{
				backingIterator.Remove();
			}
		}
	}
}
