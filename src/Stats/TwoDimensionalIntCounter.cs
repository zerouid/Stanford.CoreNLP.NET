using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>A class representing a mapping between pairs of typed objects and int values.</summary>
	/// <remarks>
	/// A class representing a mapping between pairs of typed objects and int values.
	/// (Copied from TwoDimensionalCounter)
	/// </remarks>
	/// <author>Teg Grenager</author>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class TwoDimensionalIntCounter<K1, K2>
	{
		private const long serialVersionUID = 1L;

		private IDictionary<K1, IntCounter<K2>> map;

		private int total;

		private MapFactory<K1, IntCounter<K2>> outerMF;

		private MapFactory<K2, MutableInteger> innerMF;

		private int defaultValue = 0;

		// the outermost Map
		// the total of all counts
		// the MapFactory used to make new maps to counters
		// the MapFactory used to make new maps in the inner counter
		public virtual void DefaultReturnValue(double rv)
		{
			defaultValue = (int)rv;
		}

		public virtual void DefaultReturnValue(int rv)
		{
			defaultValue = rv;
		}

		public virtual int DefaultReturnValue()
		{
			return defaultValue;
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Stats.TwoDimensionalIntCounter))
			{
				return false;
			}
			return ((Edu.Stanford.Nlp.Stats.TwoDimensionalIntCounter<object, object>)o).map.Equals(map);
		}

		public override int GetHashCode()
		{
			return map.GetHashCode() + 17;
		}

		/// <returns>the inner Counter associated with key o</returns>
		public virtual IntCounter<K2> GetCounter(K1 o)
		{
			IntCounter<K2> c = map[o];
			if (c == null)
			{
				c = new IntCounter<K2>(innerMF);
				c.SetDefaultReturnValue(defaultValue);
				map[o] = c;
			}
			return c;
		}

		public virtual ICollection<KeyValuePair<K1, IntCounter<K2>>> EntrySet()
		{
			return map;
		}

		/// <returns>total number of entries (key pairs)</returns>
		public virtual int Size()
		{
			int result = 0;
			foreach (K1 o in FirstKeySet())
			{
				IntCounter<K2> c = map[o];
				result += c.Size();
			}
			return result;
		}

		public virtual bool ContainsKey(K1 o1, K2 o2)
		{
			if (!map.Contains(o1))
			{
				return false;
			}
			IntCounter<K2> c = map[o1];
			return c.ContainsKey(o2);
		}

		public virtual void IncrementCount(K1 o1, K2 o2)
		{
			IncrementCount(o1, o2, 1);
		}

		public virtual void IncrementCount(K1 o1, K2 o2, double count)
		{
			IncrementCount(o1, o2, (int)count);
		}

		public virtual void IncrementCount(K1 o1, K2 o2, int count)
		{
			IntCounter<K2> c = GetCounter(o1);
			c.IncrementCount(o2, count);
			total += count;
		}

		public virtual void DecrementCount(K1 o1, K2 o2)
		{
			IncrementCount(o1, o2, -1);
		}

		public virtual void DecrementCount(K1 o1, K2 o2, double count)
		{
			IncrementCount(o1, o2, -count);
		}

		public virtual void DecrementCount(K1 o1, K2 o2, int count)
		{
			IncrementCount(o1, o2, -count);
		}

		public virtual void SetCount(K1 o1, K2 o2, double count)
		{
			SetCount(o1, o2, (int)count);
		}

		public virtual void SetCount(K1 o1, K2 o2, int count)
		{
			IntCounter<K2> c = GetCounter(o1);
			int oldCount = GetCount(o1, o2);
			total -= oldCount;
			c.SetCount(o2, count);
			total += count;
		}

		public virtual int Remove(K1 o1, K2 o2)
		{
			IntCounter<K2> c = GetCounter(o1);
			int oldCount = GetCount(o1, o2);
			total -= oldCount;
			c.Remove(o2);
			if (c.IsEmpty())
			{
				Sharpen.Collections.Remove(map, o1);
			}
			return oldCount;
		}

		public virtual int GetCount(K1 o1, K2 o2)
		{
			IntCounter<K2> c = GetCounter(o1);
			if (c.TotalCount() == 0 && !c.KeySet().Contains(o2))
			{
				return DefaultReturnValue();
			}
			return c.GetIntCount(o2);
		}

		/// <summary>Takes linear time.</summary>
		public virtual int TotalCount()
		{
			return total;
		}

		public virtual int TotalCount(K1 k1)
		{
			IntCounter<K2> c = GetCounter(k1);
			return c.TotalIntCount();
		}

		public virtual IntCounter<K1> TotalCounts()
		{
			IntCounter<K1> tc = new IntCounter<K1>();
			foreach (K1 k1 in map.Keys)
			{
				tc.SetCount(k1, map[k1].TotalCount());
			}
			return tc;
		}

		public virtual ICollection<K1> FirstKeySet()
		{
			return map.Keys;
		}

		/// <summary>replace the counter for K1-index o by new counter c</summary>
		public virtual IntCounter<K2> SetCounter(K1 o, IntCounter<K2> c)
		{
			IntCounter<K2> old = GetCounter(o);
			total -= old.TotalIntCount();
			map[o] = c;
			total += c.TotalIntCount();
			return old;
		}

		/// <summary>Produces a new ConditionalCounter.</summary>
		/// <returns>a new ConditionalCounter, where order of indices is reversed</returns>
		public static Edu.Stanford.Nlp.Stats.TwoDimensionalIntCounter<K2, K1> ReverseIndexOrder<K1, K2>(Edu.Stanford.Nlp.Stats.TwoDimensionalIntCounter<K1, K2> cc)
		{
			// the typing on the outerMF is violated a bit, but it'll work....
			Edu.Stanford.Nlp.Stats.TwoDimensionalIntCounter<K2, K1> result = new Edu.Stanford.Nlp.Stats.TwoDimensionalIntCounter((MapFactory)cc.outerMF, (MapFactory)cc.innerMF);
			foreach (K1 key1 in cc.FirstKeySet())
			{
				IntCounter<K2> c = cc.GetCounter(key1);
				foreach (K2 key2 in c.KeySet())
				{
					int count = c.GetIntCount(key2);
					result.SetCount(key2, key1, count);
				}
			}
			return result;
		}

		/// <summary>
		/// A simple String representation of this TwoDimensionalCounter, which has
		/// the String representation of each key pair
		/// on a separate line, followed by the count for that pair.
		/// </summary>
		/// <remarks>
		/// A simple String representation of this TwoDimensionalCounter, which has
		/// the String representation of each key pair
		/// on a separate line, followed by the count for that pair.
		/// The items are tab separated, so the result is a tab-separated value (TSV)
		/// file.  Iff none of the keys contain spaces, it will also be possible to
		/// treat this as whitespace separated fields.
		/// </remarks>
		public override string ToString()
		{
			StringBuilder buff = new StringBuilder();
			foreach (K1 key1 in map.Keys)
			{
				IntCounter<K2> c = GetCounter(key1);
				foreach (K2 key2 in c.KeySet())
				{
					double score = c.GetCount(key2);
					buff.Append(key1).Append("\t").Append(key2).Append("\t").Append(score).Append("\n");
				}
			}
			return buff.ToString();
		}

		public virtual string ToMatrixString(int cellSize)
		{
			IList<K1> firstKeys = new List<K1>(FirstKeySet());
			IList<K2> secondKeys = new List<K2>(SecondKeySet());
			(IList<IComparable>)firstKeys.Sort();
			(IList<IComparable>)secondKeys.Sort();
			int[][] counts = ToMatrix(firstKeys, secondKeys);
			return ArrayMath.ToString(counts, Sharpen.Collections.ToArray(firstKeys), Sharpen.Collections.ToArray(secondKeys), cellSize, cellSize, new DecimalFormat(), true);
		}

		/// <summary>Given an ordering of the first (row) and second (column) keys, will produce a double matrix.</summary>
		public virtual int[][] ToMatrix(IList<K1> firstKeys, IList<K2> secondKeys)
		{
			int[][] counts = new int[][] {  };
			for (int i = 0; i < firstKeys.Count; i++)
			{
				for (int j = 0; j < secondKeys.Count; j++)
				{
					counts[i][j] = GetCount(firstKeys[i], secondKeys[j]);
				}
			}
			return counts;
		}

		public virtual string ToCSVString(NumberFormat nf)
		{
			IList<K1> firstKeys = new List<K1>(FirstKeySet());
			IList<K2> secondKeys = new List<K2>(SecondKeySet());
			(IList<IComparable>)firstKeys.Sort();
			(IList<IComparable>)secondKeys.Sort();
			StringBuilder b = new StringBuilder();
			string[] headerRow = new string[secondKeys.Count + 1];
			headerRow[0] = string.Empty;
			for (int j = 0; j < secondKeys.Count; j++)
			{
				headerRow[j + 1] = secondKeys[j].ToString();
			}
			b.Append(StringUtils.ToCSVString(headerRow)).Append("\n");
			foreach (K1 rowLabel in firstKeys)
			{
				string[] row = new string[secondKeys.Count + 1];
				row[0] = rowLabel.ToString();
				for (int j_1 = 0; j_1 < secondKeys.Count; j_1++)
				{
					K2 colLabel = secondKeys[j_1];
					row[j_1 + 1] = nf.Format(GetCount(rowLabel, colLabel));
				}
				b.Append(StringUtils.ToCSVString(row)).Append("\n");
			}
			return b.ToString();
		}

		public static string ToCSVString<Ck1, Ck2>(Edu.Stanford.Nlp.Stats.TwoDimensionalIntCounter<CK1, CK2> counter, NumberFormat nf, IComparator<CK1> key1Comparator, IComparator<CK2> key2Comparator)
			where Ck1 : IComparable<CK1>
			where Ck2 : IComparable<CK2>
		{
			IList<CK1> firstKeys = new List<CK1>(counter.FirstKeySet());
			IList<CK2> secondKeys = new List<CK2>(counter.SecondKeySet());
			firstKeys.Sort(key1Comparator);
			secondKeys.Sort(key2Comparator);
			StringBuilder b = new StringBuilder();
			int secondKeysSize = secondKeys.Count;
			string[] headerRow = new string[secondKeysSize + 1];
			headerRow[0] = string.Empty;
			for (int j = 0; j < secondKeysSize; j++)
			{
				headerRow[j + 1] = secondKeys[j].ToString();
			}
			b.Append(StringUtils.ToCSVString(headerRow)).Append('\n');
			foreach (CK1 rowLabel in firstKeys)
			{
				string[] row = new string[secondKeysSize + 1];
				row[0] = rowLabel.ToString();
				for (int j_1 = 0; j_1 < secondKeysSize; j_1++)
				{
					CK2 colLabel = secondKeys[j_1];
					row[j_1 + 1] = nf.Format(counter.GetCount(rowLabel, colLabel));
				}
				b.Append(StringUtils.ToCSVString(row)).Append('\n');
			}
			return b.ToString();
		}

		public virtual ICollection<K2> SecondKeySet()
		{
			ICollection<K2> result = Generics.NewHashSet();
			foreach (K1 k1 in FirstKeySet())
			{
				foreach (K2 k2 in GetCounter(k1).KeySet())
				{
					result.Add(k2);
				}
			}
			return result;
		}

		public virtual bool IsEmpty()
		{
			return map.IsEmpty();
		}

		public virtual IntCounter<Pair<K1, K2>> Flatten()
		{
			IntCounter<Pair<K1, K2>> result = new IntCounter<Pair<K1, K2>>();
			result.SetDefaultReturnValue(defaultValue);
			foreach (K1 key1 in FirstKeySet())
			{
				IntCounter<K2> inner = GetCounter(key1);
				foreach (K2 key2 in inner.KeySet())
				{
					result.SetCount(new Pair<K1, K2>(key1, key2), inner.GetIntCount(key2));
				}
			}
			return result;
		}

		public virtual void AddAll(Edu.Stanford.Nlp.Stats.TwoDimensionalIntCounter<K1, K2> c)
		{
			foreach (K1 key in c.FirstKeySet())
			{
				IntCounter<K2> inner = c.GetCounter(key);
				IntCounter<K2> myInner = GetCounter(key);
				Counters.AddInPlace(myInner, inner);
				total += inner.TotalIntCount();
			}
		}

		public virtual void AddAll(K1 key, IntCounter<K2> c)
		{
			IntCounter<K2> myInner = GetCounter(key);
			Counters.AddInPlace(myInner, c);
			total += c.TotalIntCount();
		}

		public virtual void SubtractAll(K1 key, IntCounter<K2> c)
		{
			IntCounter<K2> myInner = GetCounter(key);
			Counters.SubtractInPlace(myInner, c);
			total -= c.TotalIntCount();
		}

		public virtual void SubtractAll(Edu.Stanford.Nlp.Stats.TwoDimensionalIntCounter<K1, K2> c, bool removeKeys)
		{
			foreach (K1 key in c.FirstKeySet())
			{
				IntCounter<K2> inner = c.GetCounter(key);
				IntCounter<K2> myInner = GetCounter(key);
				Counters.SubtractInPlace(myInner, inner);
				if (removeKeys)
				{
					Counters.RetainNonZeros(myInner);
				}
				total -= inner.TotalIntCount();
			}
		}

		public virtual void RemoveZeroCounts()
		{
			ICollection<K1> firstKeySet = Generics.NewHashSet(FirstKeySet());
			foreach (K1 k1 in firstKeySet)
			{
				IntCounter<K2> c = GetCounter(k1);
				Counters.RetainNonZeros(c);
				if (c.IsEmpty())
				{
					Sharpen.Collections.Remove(map, k1);
				}
			}
		}

		// it's empty, get rid of it!
		public virtual void Remove(K1 key)
		{
			IntCounter<K2> counter = map[key];
			if (counter != null)
			{
				total -= counter.TotalIntCount();
			}
			Sharpen.Collections.Remove(map, key);
		}

		public virtual void Clean()
		{
			foreach (K1 key1 in Generics.NewHashSet(map.Keys))
			{
				IntCounter<K2> c = map[key1];
				foreach (K2 key2 in Generics.NewHashSet(c.KeySet()))
				{
					if (c.GetIntCount(key2) == 0)
					{
						c.Remove(key2);
					}
				}
				if (c.KeySet().IsEmpty())
				{
					Sharpen.Collections.Remove(map, key1);
				}
			}
		}

		public virtual MapFactory<K1, IntCounter<K2>> GetOuterMapFactory()
		{
			return outerMF;
		}

		public virtual MapFactory<K2, MutableInteger> GetInnerMapFactory()
		{
			return innerMF;
		}

		public TwoDimensionalIntCounter()
			: this(MapFactory.HashMapFactory<K1, IntCounter<K2>>(), MapFactory.HashMapFactory<K2, MutableInteger>())
		{
		}

		public TwoDimensionalIntCounter(int initialCapacity)
			: this(MapFactory.HashMapFactory<K1, IntCounter<K2>>(), MapFactory.HashMapFactory<K2, MutableInteger>(), initialCapacity)
		{
		}

		public TwoDimensionalIntCounter(MapFactory<K1, IntCounter<K2>> outerFactory, MapFactory<K2, MutableInteger> innerFactory)
			: this(outerFactory, innerFactory, 100)
		{
		}

		public TwoDimensionalIntCounter(MapFactory<K1, IntCounter<K2>> outerFactory, MapFactory<K2, MutableInteger> innerFactory, int initialCapacity)
		{
			innerMF = innerFactory;
			outerMF = outerFactory;
			map = outerFactory.NewMap(initialCapacity);
			total = 0;
		}
	}
}
