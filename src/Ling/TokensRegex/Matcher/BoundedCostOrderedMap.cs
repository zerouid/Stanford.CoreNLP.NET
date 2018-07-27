using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Ling.Tokensregex.Matcher
{
	/// <summary>Map that is sorted by cost.</summary>
	/// <remarks>
	/// Map that is sorted by cost. Keeps lowest scores.
	/// When deciding what item to keep with the same cost, ties are arbitrarily broken.
	/// </remarks>
	/// <author>Angel Chang</author>
	public class BoundedCostOrderedMap<K, V> : AbstractMap<K, V>
	{
		/// <summary>Limit on the size of the map</summary>
		private readonly int maxSize;

		/// <summary>Limit on the maximum allowed cost</summary>
		private readonly double maxCost;

		/// <summary>
		/// Priority queue on the keys - note that the priority queue only orders on the cost,
		/// We can't control the ordering of keys with the same cost
		/// </summary>
		private IPriorityQueue<K> priorityQueue = new BinaryHeapPriorityQueue<K>();

		/// <summary>Map of keys to their values</summary>
		private IDictionary<K, V> valueMap = new Dictionary<K, V>();

		/// <summary>Cost function on the values</summary>
		private IToDoubleFunction<V> costFunction;

		public BoundedCostOrderedMap(IToDoubleFunction<V> costFunction, int maxSize, double maxCost)
		{
			this.costFunction = costFunction;
			this.maxSize = maxSize;
			this.maxCost = maxCost;
		}

		public override int Count
		{
			get
			{
				return valueMap.Count;
			}
		}

		public override bool IsEmpty()
		{
			return valueMap.IsEmpty();
		}

		public override bool Contains(object key)
		{
			return valueMap.Contains(key);
		}

		public override bool ContainsValue(object value)
		{
			return valueMap.ContainsValue(value);
		}

		public override V Get(object key)
		{
			return valueMap[key];
		}

		public virtual double GetCost(V value)
		{
			return costFunction.ApplyAsDouble(value);
		}

		public override V Put(K key, V value)
		{
			double cost = GetCost(value);
			if (cost >= maxCost)
			{
				return null;
			}
			V v = valueMap[key];
			if (v != null && GetCost(v) < cost)
			{
				return null;
			}
			if (maxSize > 0 && priorityQueue.Count >= maxSize)
			{
				if (priorityQueue.GetPriority() > cost)
				{
					K k = priorityQueue.RemoveFirst();
					Sharpen.Collections.Remove(valueMap, k);
					// keep maxSize lowest scores
					priorityQueue.ChangePriority(key, cost);
					return valueMap[key] = value;
				}
			}
			else
			{
				priorityQueue.ChangePriority(key, cost);
				return valueMap[key] = value;
			}
			return null;
		}

		public override V Remove(object key)
		{
			priorityQueue.Remove(key);
			return Sharpen.Collections.Remove(valueMap, key);
		}

		public override void PutAll<_T0>(IDictionary<_T0> m)
		{
			foreach (KeyValuePair<K, V> entry in m)
			{
				this[entry.Key] = entry.Value;
			}
		}

		public override void Clear()
		{
			valueMap.Clear();
			priorityQueue.Clear();
		}

		public override ICollection<K> Keys
		{
			get
			{
				return valueMap.Keys;
			}
		}

		public override ICollection<V> Values
		{
			get
			{
				return valueMap.Values;
			}
		}

		public virtual IList<V> ValuesList()
		{
			IList<V> list = new List<V>();
			foreach (K k in priorityQueue.ToSortedList())
			{
				list.Add(valueMap[k]);
			}
			Java.Util.Collections.Reverse(list);
			return list;
		}

		public override ICollection<KeyValuePair<K, V>> EntrySet()
		{
			return valueMap;
		}

		public virtual double TopCost()
		{
			return priorityQueue.GetPriority();
		}

		public virtual K TopKey()
		{
			return priorityQueue.GetFirst();
		}
	}
}
