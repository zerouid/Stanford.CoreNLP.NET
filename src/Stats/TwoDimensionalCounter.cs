using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// A class representing a mapping between pairs of typed objects and double
	/// values.
	/// </summary>
	/// <author>Teg Grenager</author>
	[System.Serializable]
	public class TwoDimensionalCounter<K1, K2> : ITwoDimensionalCounterInterface<K1, K2>
	{
		private const long serialVersionUID = 1L;

		private IDictionary<K1, ClassicCounter<K2>> map;

		private double total;

		private MapFactory<K1, ClassicCounter<K2>> outerMF;

		private MapFactory<K2, MutableDouble> innerMF;

		private double defaultValue = 0.0;

		// the outermost Map
		// the total of all counts
		// the MapFactory used to make new maps to counters
		// the MapFactory used to make new maps in the inner counter
		public virtual void DefaultReturnValue(double rv)
		{
			defaultValue = rv;
		}

		public virtual double DefaultReturnValue()
		{
			return defaultValue;
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Stats.TwoDimensionalCounter))
			{
				return false;
			}
			return ((Edu.Stanford.Nlp.Stats.TwoDimensionalCounter<object, object>)o).map.Equals(map);
		}

		public override int GetHashCode()
		{
			return map.GetHashCode() + 17;
		}

		/// <returns>the inner Counter associated with key o</returns>
		public virtual ClassicCounter<K2> GetCounter(K1 o)
		{
			ClassicCounter<K2> c = map[o];
			if (c == null)
			{
				c = new ClassicCounter<K2>(innerMF);
				c.SetDefaultReturnValue(defaultValue);
				map[o] = c;
			}
			return c;
		}

		public virtual ICollection<KeyValuePair<K1, ClassicCounter<K2>>> EntrySet()
		{
			return map;
		}

		/// <returns>total number of entries (key pairs)</returns>
		public virtual int Size()
		{
			int result = 0;
			foreach (K1 o in FirstKeySet())
			{
				ClassicCounter<K2> c = map[o];
				result += c.Size();
			}
			return result;
		}

		/// <returns>size of the outer map</returns>
		public virtual int SizeOuterMap()
		{
			return map.Count;
		}

		public virtual bool ContainsKey(K1 o1, K2 o2)
		{
			if (!map.Contains(o1))
			{
				return false;
			}
			ClassicCounter<K2> c = map[o1];
			return c.ContainsKey(o2);
		}

		public virtual bool ContainsFirstKey(K1 o1)
		{
			return map.Contains(o1);
		}

		public virtual void IncrementCount(K1 o1, K2 o2)
		{
			IncrementCount(o1, o2, 1.0);
		}

		public virtual void IncrementCount(K1 o1, K2 o2, double count)
		{
			ClassicCounter<K2> c = GetCounter(o1);
			c.IncrementCount(o2, count);
			total += count;
		}

		public virtual void DecrementCount(K1 o1, K2 o2)
		{
			IncrementCount(o1, o2, -1.0);
		}

		public virtual void DecrementCount(K1 o1, K2 o2, double count)
		{
			IncrementCount(o1, o2, -count);
		}

		public virtual void SetCount(K1 o1, K2 o2, double count)
		{
			ClassicCounter<K2> c = GetCounter(o1);
			double oldCount = GetCount(o1, o2);
			total -= oldCount;
			c.SetCount(o2, count);
			total += count;
		}

		public virtual double Remove(K1 o1, K2 o2)
		{
			ClassicCounter<K2> c = GetCounter(o1);
			double oldCount = GetCount(o1, o2);
			total -= oldCount;
			c.Remove(o2);
			if (c.Size() == 0)
			{
				Sharpen.Collections.Remove(map, o1);
			}
			return oldCount;
		}

		public virtual double GetCount(K1 o1, K2 o2)
		{
			ClassicCounter<K2> c = GetCounter(o1);
			if (c.TotalCount() == 0.0 && !c.KeySet().Contains(o2))
			{
				return DefaultReturnValue();
			}
			return c.GetCount(o2);
		}

		/// <summary>Takes linear time.</summary>
		public virtual double TotalCount()
		{
			return total;
		}

		public virtual double TotalCount(K1 k1)
		{
			ClassicCounter<K2> c = GetCounter(k1);
			return c.TotalCount();
		}

		public virtual ICollection<K1> FirstKeySet()
		{
			return map.Keys;
		}

		/// <summary>replace the counter for K1-index o by new counter c</summary>
		public virtual ClassicCounter<K2> SetCounter(K1 o, ICounter<K2> c)
		{
			ClassicCounter<K2> old = GetCounter(o);
			total -= old.TotalCount();
			if (c is ClassicCounter)
			{
				map[o] = (ClassicCounter<K2>)c;
			}
			else
			{
				map[o] = new ClassicCounter<K2>(c);
			}
			total += c.TotalCount();
			return old;
		}

		/// <summary>Produces a new ConditionalCounter.</summary>
		/// <returns>a new ConditionalCounter, where order of indices is reversed</returns>
		public static Edu.Stanford.Nlp.Stats.TwoDimensionalCounter<K2, K1> ReverseIndexOrder<K1, K2>(Edu.Stanford.Nlp.Stats.TwoDimensionalCounter<K1, K2> cc)
		{
			// they typing on the outerMF is violated a bit, but it'll work....
			Edu.Stanford.Nlp.Stats.TwoDimensionalCounter<K2, K1> result = new Edu.Stanford.Nlp.Stats.TwoDimensionalCounter((MapFactory)cc.outerMF, (MapFactory)cc.innerMF);
			foreach (K1 key1 in cc.FirstKeySet())
			{
				ClassicCounter<K2> c = cc.GetCounter(key1);
				foreach (K2 key2 in c.KeySet())
				{
					double count = c.GetCount(key2);
					result.SetCount(key2, key1, count);
				}
			}
			return result;
		}

		/// <summary>
		/// A simple String representation of this TwoDimensionalCounter, which has the
		/// String representation of each key pair on a separate line, followed by the
		/// count for that pair.
		/// </summary>
		/// <remarks>
		/// A simple String representation of this TwoDimensionalCounter, which has the
		/// String representation of each key pair on a separate line, followed by the
		/// count for that pair. The items are tab separated, so the result is a
		/// tab-separated value (TSV) file. Iff none of the keys contain spaces, it
		/// will also be possible to treat this as whitespace separated fields.
		/// </remarks>
		public override string ToString()
		{
			StringBuilder buff = new StringBuilder();
			foreach (K1 key1 in map.Keys)
			{
				ClassicCounter<K2> c = GetCounter(key1);
				foreach (K2 key2 in c.KeySet())
				{
					double score = c.GetCount(key2);
					buff.Append(key1).Append('\t').Append(key2).Append('\t').Append(score).Append('\n');
				}
			}
			return buff.ToString();
		}

		public virtual string ToMatrixString(int cellSize)
		{
			return ToMatrixString(cellSize, new DecimalFormat());
		}

		public virtual string ToMatrixString(int cellSize, NumberFormat nf)
		{
			IList<K1> firstKeys = new List<K1>(FirstKeySet());
			IList<K2> secondKeys = new List<K2>(SecondKeySet());
			(IList<IComparable>)firstKeys.Sort();
			(IList<IComparable>)secondKeys.Sort();
			double[][] counts = ToMatrix(firstKeys, secondKeys);
			return ArrayMath.ToString(counts, cellSize, Sharpen.Collections.ToArray(firstKeys), Sharpen.Collections.ToArray(secondKeys), nf, true);
		}

		/// <summary>
		/// Given an ordering of the first (row) and second (column) keys, will produce
		/// a double matrix.
		/// </summary>
		public virtual double[][] ToMatrix(IList<K1> firstKeys, IList<K2> secondKeys)
		{
			double[][] counts = new double[][] {  };
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
			b.Append(StringUtils.ToCSVString(headerRow)).Append('\n');
			foreach (K1 rowLabel in firstKeys)
			{
				string[] row = new string[secondKeys.Count + 1];
				row[0] = rowLabel.ToString();
				for (int j_1 = 0; j_1 < secondKeys.Count; j_1++)
				{
					K2 colLabel = secondKeys[j_1];
					row[j_1 + 1] = nf.Format(GetCount(rowLabel, colLabel));
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

		public virtual ClassicCounter<Pair<K1, K2>> Flatten()
		{
			ClassicCounter<Pair<K1, K2>> result = new ClassicCounter<Pair<K1, K2>>();
			result.SetDefaultReturnValue(defaultValue);
			foreach (K1 key1 in FirstKeySet())
			{
				ClassicCounter<K2> inner = GetCounter(key1);
				foreach (K2 key2 in inner.KeySet())
				{
					result.SetCount(new Pair<K1, K2>(key1, key2), inner.GetCount(key2));
				}
			}
			return result;
		}

		public virtual void AddAll(ITwoDimensionalCounterInterface<K1, K2> c)
		{
			foreach (K1 key in c.FirstKeySet())
			{
				ICounter<K2> inner = c.GetCounter(key);
				ClassicCounter<K2> myInner = GetCounter(key);
				Counters.AddInPlace(myInner, inner);
				total += inner.TotalCount();
			}
		}

		public virtual void AddAll(K1 key, ICounter<K2> c)
		{
			ClassicCounter<K2> myInner = GetCounter(key);
			Counters.AddInPlace(myInner, c);
			total += c.TotalCount();
		}

		public virtual void SubtractAll(K1 key, ICounter<K2> c)
		{
			ClassicCounter<K2> myInner = GetCounter(key);
			Counters.SubtractInPlace(myInner, c);
			total -= c.TotalCount();
		}

		public virtual void SubtractAll(ITwoDimensionalCounterInterface<K1, K2> c, bool removeKeys)
		{
			foreach (K1 key in c.FirstKeySet())
			{
				ICounter<K2> inner = c.GetCounter(key);
				ClassicCounter<K2> myInner = GetCounter(key);
				Counters.SubtractInPlace(myInner, inner);
				if (removeKeys)
				{
					Counters.RetainNonZeros(myInner);
				}
				total -= inner.TotalCount();
			}
		}

		/// <summary>
		/// Returns the counters with keys as the first key and count as the
		/// total count of the inner counter for that key
		/// </summary>
		/// <returns>counter of type K1</returns>
		public virtual ICounter<K1> SumInnerCounter()
		{
			ICounter<K1> summed = new ClassicCounter<K1>();
			foreach (K1 key in this.FirstKeySet())
			{
				summed.IncrementCount(key, this.GetCounter(key).TotalCount());
			}
			return summed;
		}

		public virtual void RemoveZeroCounts()
		{
			ICollection<K1> firstKeySet = Generics.NewHashSet(FirstKeySet());
			foreach (K1 k1 in firstKeySet)
			{
				ClassicCounter<K2> c = GetCounter(k1);
				Counters.RetainNonZeros(c);
				if (c.Size() == 0)
				{
					Sharpen.Collections.Remove(map, k1);
				}
			}
		}

		// it's empty, get rid of it!
		public virtual void Remove(K1 key)
		{
			ClassicCounter<K2> counter = map[key];
			if (counter != null)
			{
				total -= counter.TotalCount();
			}
			Sharpen.Collections.Remove(map, key);
		}

		/// <summary>clears the map, total and default value</summary>
		public virtual void Clear()
		{
			map.Clear();
			total = 0;
			defaultValue = 0;
		}

		public virtual void Clean()
		{
			foreach (K1 key1 in Generics.NewHashSet(map.Keys))
			{
				ClassicCounter<K2> c = map[key1];
				foreach (K2 key2 in Generics.NewHashSet(c.KeySet()))
				{
					if (SloppyMath.IsCloseTo(0.0, c.GetCount(key2)))
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

		public virtual MapFactory<K1, ClassicCounter<K2>> GetOuterMapFactory()
		{
			return outerMF;
		}

		public virtual MapFactory<K2, MutableDouble> GetInnerMapFactory()
		{
			return innerMF;
		}

		public TwoDimensionalCounter()
			: this(MapFactory.HashMapFactory<K1, ClassicCounter<K2>>(), MapFactory.HashMapFactory<K2, MutableDouble>())
		{
		}

		public TwoDimensionalCounter(MapFactory<K1, ClassicCounter<K2>> outerFactory, MapFactory<K2, MutableDouble> innerFactory)
		{
			innerMF = innerFactory;
			outerMF = outerFactory;
			map = outerFactory.NewMap();
			total = 0.0;
		}

		public static Edu.Stanford.Nlp.Stats.TwoDimensionalCounter<K1, K2> IdentityHashMapCounter<K1, K2>()
		{
			return new Edu.Stanford.Nlp.Stats.TwoDimensionalCounter<K1, K2>(MapFactory.IdentityHashMapFactory<K1, ClassicCounter<K2>>(), MapFactory.IdentityHashMapFactory<K2, MutableDouble>());
		}

		public virtual void RecomputeTotal()
		{
			total = 0;
			foreach (KeyValuePair<K1, ClassicCounter<K2>> c in map)
			{
				total += c.Value.TotalCount();
			}
		}

		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Stats.TwoDimensionalCounter<string, string> cc = new Edu.Stanford.Nlp.Stats.TwoDimensionalCounter<string, string>();
			cc.SetCount("a", "c", 1.0);
			cc.SetCount("b", "c", 1.0);
			cc.SetCount("a", "d", 1.0);
			cc.SetCount("a", "d", -1.0);
			cc.SetCount("b", "d", 1.0);
			System.Console.Out.WriteLine(cc);
			cc.IncrementCount("b", "d", 1.0);
			System.Console.Out.WriteLine(cc);
			Edu.Stanford.Nlp.Stats.TwoDimensionalCounter<string, string> cc2 = Edu.Stanford.Nlp.Stats.TwoDimensionalCounter.ReverseIndexOrder(cc);
			System.Console.Out.WriteLine(cc2);
		}
	}
}
