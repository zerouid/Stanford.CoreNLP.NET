using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Util
{
	/// <summary>PriorityQueue with explicit double priority values.</summary>
	/// <remarks>
	/// PriorityQueue with explicit double priority values.  Larger doubles are higher priorities.  BinaryHeap-backed.
	/// For each entry, uses ~ 24 (entry) + 16? (Map.Entry) + 4 (List entry) = 44 bytes?
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	/// <?/>
	public class BinaryHeapPriorityQueue<E> : AbstractSet<E>, IPriorityQueue<E>, IEnumerator<E>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.BinaryHeapPriorityQueue));

		/// <summary>
		/// An
		/// <c>Entry</c>
		/// stores an object in the queue along with
		/// its current location (array position) and priority.
		/// uses ~ 8 (self) + 4 (key ptr) + 4 (index) + 8 (priority) = 24 bytes?
		/// </summary>
		private sealed class Entry<E>
		{
			public E key;

			public int index;

			public double priority;

			public override string ToString()
			{
				return key + " at " + index + " (" + priority + ')';
			}
		}

		public virtual bool MoveNext()
		{
			return Count > 0;
		}

		public virtual E Current
		{
			get
			{
				if (Count == 0)
				{
					throw new NoSuchElementException("Empty PQ");
				}
				return RemoveFirst();
			}
		}

		public virtual void Remove()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// <c>indexToEntry</c>
		/// maps linear array locations (not
		/// priorities) to heap entries.
		/// </summary>
		private readonly IList<BinaryHeapPriorityQueue.Entry<E>> indexToEntry;

		/// <summary>
		/// <c>keyToEntry</c>
		/// maps heap objects to their heap
		/// entries.
		/// </summary>
		private readonly IDictionary<E, BinaryHeapPriorityQueue.Entry<E>> keyToEntry;

		private BinaryHeapPriorityQueue.Entry<E> Parent(BinaryHeapPriorityQueue.Entry<E> entry)
		{
			int index = entry.index;
			return (index > 0 ? GetEntry((index - 1) / 2) : null);
		}

		private BinaryHeapPriorityQueue.Entry<E> LeftChild(BinaryHeapPriorityQueue.Entry<E> entry)
		{
			int leftIndex = entry.index * 2 + 1;
			return (leftIndex < Count ? GetEntry(leftIndex) : null);
		}

		private BinaryHeapPriorityQueue.Entry<E> RightChild(BinaryHeapPriorityQueue.Entry<E> entry)
		{
			int index = entry.index;
			int rightIndex = index * 2 + 2;
			return (rightIndex < Count ? GetEntry(rightIndex) : null);
		}

		private int Compare(BinaryHeapPriorityQueue.Entry<E> entryA, BinaryHeapPriorityQueue.Entry<E> entryB)
		{
			int result = Compare(entryA.priority, entryB.priority);
			if (result != 0)
			{
				return result;
			}
			if ((entryA.key is IComparable) && (entryB.key is IComparable))
			{
				IComparable<E> key = ErasureUtils.UncheckedCast(entryA.key);
				return key.CompareTo(entryB.key);
			}
			return result;
		}

		private static int Compare(double a, double b)
		{
			double diff = a - b;
			if (diff > 0.0)
			{
				return 1;
			}
			if (diff < 0.0)
			{
				return -1;
			}
			return 0;
		}

		/// <summary>Structural swap of two entries.</summary>
		private void Swap(BinaryHeapPriorityQueue.Entry<E> entryA, BinaryHeapPriorityQueue.Entry<E> entryB)
		{
			int indexA = entryA.index;
			int indexB = entryB.index;
			entryA.index = indexB;
			entryB.index = indexA;
			indexToEntry.Set(indexA, entryB);
			indexToEntry.Set(indexB, entryA);
		}

		/// <summary>Remove the last element of the heap (last in the index array).</summary>
		public virtual void RemoveLastEntry()
		{
			BinaryHeapPriorityQueue.Entry<E> entry = indexToEntry.Remove(Count - 1);
			Sharpen.Collections.Remove(keyToEntry, entry.key);
		}

		/// <summary>Get the entry by key (null if none).</summary>
		private BinaryHeapPriorityQueue.Entry<E> GetEntry(E key)
		{
			return keyToEntry[key];
		}

		/// <summary>Get entry by index, exception if none.</summary>
		private BinaryHeapPriorityQueue.Entry<E> GetEntry(int index)
		{
			BinaryHeapPriorityQueue.Entry<E> entry = indexToEntry[index];
			return entry;
		}

		private BinaryHeapPriorityQueue.Entry<E> MakeEntry(E key)
		{
			BinaryHeapPriorityQueue.Entry<E> entry = new BinaryHeapPriorityQueue.Entry<E>();
			entry.index = Count;
			entry.key = key;
			entry.priority = double.NegativeInfinity;
			indexToEntry.Add(entry);
			keyToEntry[key] = entry;
			return entry;
		}

		/// <summary>iterative heapify up: move item o at index up until correctly placed</summary>
		private void HeapifyUp(BinaryHeapPriorityQueue.Entry<E> entry)
		{
			while (true)
			{
				if (entry.index == 0)
				{
					break;
				}
				BinaryHeapPriorityQueue.Entry<E> parentEntry = Parent(entry);
				if (Compare(entry, parentEntry) <= 0)
				{
					break;
				}
				Swap(entry, parentEntry);
			}
		}

		/// <summary>
		/// On the assumption that
		/// leftChild(entry) and rightChild(entry) satisfy the heap property,
		/// make sure that the heap at entry satisfies this property by possibly
		/// percolating the element entry downwards.
		/// </summary>
		/// <remarks>
		/// On the assumption that
		/// leftChild(entry) and rightChild(entry) satisfy the heap property,
		/// make sure that the heap at entry satisfies this property by possibly
		/// percolating the element entry downwards.  I've replaced the obvious
		/// recursive formulation with an iterative one to gain (marginal) speed
		/// </remarks>
		private void HeapifyDown(BinaryHeapPriorityQueue.Entry<E> entry)
		{
			BinaryHeapPriorityQueue.Entry<E> bestEntry;
			do
			{
				// initialized below
				bestEntry = entry;
				BinaryHeapPriorityQueue.Entry<E> leftEntry = LeftChild(entry);
				if (leftEntry != null)
				{
					if (Compare(bestEntry, leftEntry) < 0)
					{
						bestEntry = leftEntry;
					}
				}
				BinaryHeapPriorityQueue.Entry<E> rightEntry = RightChild(entry);
				if (rightEntry != null)
				{
					if (Compare(bestEntry, rightEntry) < 0)
					{
						bestEntry = rightEntry;
					}
				}
				if (bestEntry != entry)
				{
					// Swap min and current
					Swap(bestEntry, entry);
				}
			}
			while (bestEntry != entry);
		}

		// at start of next loop, we set currentIndex to largestIndex
		// this indexation now holds current, so it is unchanged
		// log.info("Done with heapify down");
		// verify();
		private void Heapify(BinaryHeapPriorityQueue.Entry<E> entry)
		{
			HeapifyUp(entry);
			HeapifyDown(entry);
		}

		/// <summary>
		/// Finds the E with the highest priority, removes it,
		/// and returns it.
		/// </summary>
		/// <returns>the E with highest priority</returns>
		public virtual E RemoveFirst()
		{
			E first = GetFirst();
			Remove(first);
			return first;
		}

		/// <summary>
		/// Finds the E with the highest priority and returns it, without
		/// modifying the queue.
		/// </summary>
		/// <returns>the E with minimum key</returns>
		public virtual E GetFirst()
		{
			if (IsEmpty())
			{
				throw new NoSuchElementException();
			}
			return GetEntry(0).key;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double GetPriority()
		{
			if (IsEmpty())
			{
				throw new NoSuchElementException();
			}
			return GetEntry(0).priority;
		}

		/// <summary>Searches for the object in the queue and returns it.</summary>
		/// <remarks>
		/// Searches for the object in the queue and returns it.  May be useful if
		/// you can create a new object that is .equals() to an object in the queue
		/// but is not actually identical, or if you want to modify an object that is
		/// in the queue.
		/// </remarks>
		/// <returns>
		/// null if the object is not in the queue, otherwise returns the
		/// object.
		/// </returns>
		public virtual E GetObject(E key)
		{
			if (!Contains(key))
			{
				return null;
			}
			BinaryHeapPriorityQueue.Entry<E> e = GetEntry(key);
			return e.key;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double GetPriority(E key)
		{
			BinaryHeapPriorityQueue.Entry<E> entry = GetEntry(key);
			if (entry == null)
			{
				return double.NegativeInfinity;
			}
			return entry.priority;
		}

		/// <summary>
		/// Adds an object to the queue with the minimum priority
		/// (Double.NEGATIVE_INFINITY).
		/// </summary>
		/// <remarks>
		/// Adds an object to the queue with the minimum priority
		/// (Double.NEGATIVE_INFINITY).  If the object is already in the queue
		/// with worse priority, this does nothing.  If the object is
		/// already present, with better priority, it will NOT cause an
		/// a decreasePriority.
		/// </remarks>
		/// <param name="key">an <code>E</code> value</param>
		/// <returns>whether the key was present before</returns>
		public override bool Add(E key)
		{
			if (Contains(key))
			{
				return false;
			}
			MakeEntry(key);
			return true;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual bool Add(E key, double priority)
		{
			//    log.info("Adding " + key + " with priority " + priority);
			if (Add(key))
			{
				RelaxPriority(key, priority);
				return true;
			}
			return false;
		}

		public override bool Remove(object key)
		{
			E eKey = (E)key;
			BinaryHeapPriorityQueue.Entry<E> entry = GetEntry(eKey);
			if (entry == null)
			{
				return false;
			}
			RemoveEntry(entry);
			return true;
		}

		private void RemoveEntry(BinaryHeapPriorityQueue.Entry<E> entry)
		{
			BinaryHeapPriorityQueue.Entry<E> lastEntry = GetLastEntry();
			if (entry != lastEntry)
			{
				Swap(entry, lastEntry);
				RemoveLastEntry();
				Heapify(lastEntry);
			}
			else
			{
				RemoveLastEntry();
			}
		}

		private BinaryHeapPriorityQueue.Entry<E> GetLastEntry()
		{
			return GetEntry(Count - 1);
		}

		/// <summary>Promotes a key in the queue, adding it if it wasn't there already.</summary>
		/// <remarks>Promotes a key in the queue, adding it if it wasn't there already.  If the specified priority is worse than the current priority, nothing happens.  Faster than add if you don't care about whether the key is new.</remarks>
		/// <param name="key">an <code>Object</code> value</param>
		/// <returns>whether the priority actually improved.</returns>
		public virtual bool RelaxPriority(E key, double priority)
		{
			BinaryHeapPriorityQueue.Entry<E> entry = GetEntry(key);
			if (entry == null)
			{
				entry = MakeEntry(key);
			}
			if (Compare(priority, entry.priority) <= 0)
			{
				return false;
			}
			entry.priority = priority;
			HeapifyUp(entry);
			return true;
		}

		/// <summary>Demotes a key in the queue, adding it if it wasn't there already.</summary>
		/// <remarks>Demotes a key in the queue, adding it if it wasn't there already.  If the specified priority is better than the current priority, nothing happens.  If you decrease the priority on a non-present key, it will get added, but at it's old implicit priority of Double.NEGATIVE_INFINITY.
		/// 	</remarks>
		/// <param name="key">an <code>Object</code> value</param>
		/// <returns>whether the priority actually improved.</returns>
		public virtual bool DecreasePriority(E key, double priority)
		{
			BinaryHeapPriorityQueue.Entry<E> entry = GetEntry(key);
			if (entry == null)
			{
				entry = MakeEntry(key);
			}
			if (Compare(priority, entry.priority) >= 0)
			{
				return false;
			}
			entry.priority = priority;
			HeapifyDown(entry);
			return true;
		}

		/// <summary>Changes a priority, either up or down, adding the key it if it wasn't there already.</summary>
		/// <param name="key">an <code>Object</code> value</param>
		/// <returns>whether the priority actually changed.</returns>
		public virtual bool ChangePriority(E key, double priority)
		{
			BinaryHeapPriorityQueue.Entry<E> entry = GetEntry(key);
			if (entry == null)
			{
				entry = MakeEntry(key);
			}
			if (Compare(priority, entry.priority) == 0)
			{
				return false;
			}
			entry.priority = priority;
			Heapify(entry);
			return true;
		}

		/// <summary>Checks if the queue is empty.</summary>
		/// <returns>a <code>boolean</code> value</returns>
		public override bool IsEmpty()
		{
			return indexToEntry.IsEmpty();
		}

		/// <summary>Get the number of elements in the queue.</summary>
		/// <returns>queue size</returns>
		public override int Count
		{
			get
			{
				return indexToEntry.Count;
			}
		}

		/// <summary>Returns whether the queue contains the given key.</summary>
		public override bool Contains(object key)
		{
			return keyToEntry.Contains(key);
		}

		public virtual IList<E> ToSortedList()
		{
			IList<E> sortedList = new List<E>(Count);
			BinaryHeapPriorityQueue<E> queue = this.DeepCopy();
			while (!queue.IsEmpty())
			{
				sortedList.Add(queue.RemoveFirst());
			}
			return sortedList;
		}

		public virtual BinaryHeapPriorityQueue<E> DeepCopy(MapFactory<E, BinaryHeapPriorityQueue.Entry<E>> mapFactory)
		{
			BinaryHeapPriorityQueue<E> queue = new BinaryHeapPriorityQueue<E>(mapFactory);
			foreach (BinaryHeapPriorityQueue.Entry<E> entry in keyToEntry.Values)
			{
				queue.RelaxPriority(entry.key, entry.priority);
			}
			return queue;
		}

		public virtual BinaryHeapPriorityQueue<E> DeepCopy()
		{
			return DeepCopy(MapFactory.HashMapFactory<E, BinaryHeapPriorityQueue.Entry<E>>());
		}

		public override IEnumerator<E> GetEnumerator()
		{
			return Java.Util.Collections.UnmodifiableCollection(ToSortedList()).GetEnumerator();
		}

		/// <summary>Clears the queue.</summary>
		public override void Clear()
		{
			indexToEntry.Clear();
			keyToEntry.Clear();
		}

		//  private void verify() {
		//    for (int i = 0; i < indexToEntry.size(); i++) {
		//      if (i != 0) {
		//        // check ordering
		//        if (compare(getEntry(i), parent(getEntry(i))) < 0) {
		//          log.info("Error in the ordering of the heap! ("+i+")");
		//          System.exit(0);
		//        }
		//      }
		//      // check placement
		//      if (i != ((Entry)indexToEntry.get(i)).index)
		//        log.info("Error in placement in the heap!");
		//    }
		//  }
		public override string ToString()
		{
			return ToString(0);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual string ToString(int maxKeysToPrint)
		{
			if (maxKeysToPrint <= 0)
			{
				maxKeysToPrint = int.MaxValue;
			}
			IList<E> sortedKeys = ToSortedList();
			StringBuilder sb = new StringBuilder("[");
			for (int i = 0; i < maxKeysToPrint && i < sortedKeys.Count; i++)
			{
				E key = sortedKeys[i];
				sb.Append(key).Append('=').Append(GetPriority(key));
				if (i < maxKeysToPrint - 1 && i < sortedKeys.Count - 1)
				{
					sb.Append(", ");
				}
			}
			sb.Append(']');
			return sb.ToString();
		}

		public virtual string ToVerticalString()
		{
			IList<E> sortedKeys = ToSortedList();
			StringBuilder sb = new StringBuilder();
			for (IEnumerator<E> keyI = sortedKeys.GetEnumerator(); keyI.MoveNext(); )
			{
				E key = keyI.Current;
				sb.Append(key);
				sb.Append('\t');
				sb.Append(GetPriority(key));
				if (keyI.MoveNext())
				{
					sb.Append('\n');
				}
			}
			return sb.ToString();
		}

		public BinaryHeapPriorityQueue()
			: this(MapFactory.HashMapFactory<E, BinaryHeapPriorityQueue.Entry<E>>())
		{
		}

		public BinaryHeapPriorityQueue(int initCapacity)
			: this(MapFactory.HashMapFactory<E, BinaryHeapPriorityQueue.Entry<E>>(), initCapacity)
		{
		}

		public BinaryHeapPriorityQueue(MapFactory<E, BinaryHeapPriorityQueue.Entry<E>> mapFactory)
		{
			indexToEntry = new List<BinaryHeapPriorityQueue.Entry<E>>();
			keyToEntry = mapFactory.NewMap();
		}

		public BinaryHeapPriorityQueue(MapFactory<E, BinaryHeapPriorityQueue.Entry<E>> mapFactory, int initCapacity)
		{
			indexToEntry = new List<BinaryHeapPriorityQueue.Entry<E>>(initCapacity);
			keyToEntry = mapFactory.NewMap(initCapacity);
		}
	}
}
