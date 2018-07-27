using System.Collections.Generic;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Implements a heap as an ArrayList.</summary>
	/// <remarks>
	/// Implements a heap as an ArrayList.
	/// Values are all implicit in the comparator
	/// passed in on construction.  Decrease key is supported, though only
	/// lg(n).  Unlike the previous implementation of this class, this
	/// heap interprets the addition of an existing element as a "change
	/// key" which gets ignored unless it actually turns out to be a
	/// decrease key.  Note that in this implementation, changing the key
	/// of an object should trigger a change in the comparator's ordering
	/// for that object, but should NOT change the equality of that
	/// object.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	/// <version>1.2, 07/31/02</version>
	public class ArrayHeap<E> : AbstractSet<E>, IHeap<E>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.ArrayHeap));

		/// <summary>
		/// A <code>HeapEntry</code> stores an object in the heap along with
		/// its current location (array position) in the heap.
		/// </summary>
		/// <author><a href="mailto:klein@cs.stanford.edu">Dan Klein</a></author>
		/// <version>1.2</version>
		private sealed class HeapEntry<E>
		{
			public E @object;

			public int index;
		}

		/// <summary>
		/// <code>indexToEntry</code> maps linear array locations (not
		/// priorities) to heap entries.
		/// </summary>
		private readonly List<ArrayHeap.HeapEntry<E>> indexToEntry;

		/// <summary>
		/// <code>objectToEntry</code> maps heap objects to their heap
		/// entries.
		/// </summary>
		private readonly IDictionary<E, ArrayHeap.HeapEntry<E>> objectToEntry;

		/// <summary><code>cmp</code> is the comparator passed on construction.</summary>
		private readonly IComparator<E> cmp;

		// Primitive Heap Operations
		private static int Parent(int index)
		{
			return (index - 1) / 2;
		}

		private ArrayHeap.HeapEntry<E> Parent(ArrayHeap.HeapEntry<E> entry)
		{
			int index = entry.index;
			return (index > 0 ? indexToEntry[(index - 1) / 2] : null);
		}

		private ArrayHeap.HeapEntry<E> LeftChild(ArrayHeap.HeapEntry<E> entry)
		{
			int index = entry.index;
			int leftIndex = index * 2 + 1;
			return (leftIndex < Count ? indexToEntry[leftIndex] : null);
		}

		private ArrayHeap.HeapEntry<E> RightChild(ArrayHeap.HeapEntry<E> entry)
		{
			int index = entry.index;
			int rightIndex = index * 2 + 2;
			return (rightIndex < Count ? indexToEntry[rightIndex] : null);
		}

		private int Compare(ArrayHeap.HeapEntry<E> entryA, ArrayHeap.HeapEntry<E> entryB)
		{
			return cmp.Compare(entryA.@object, entryB.@object);
		}

		private void Swap(ArrayHeap.HeapEntry<E> entryA, ArrayHeap.HeapEntry<E> entryB)
		{
			int indexA = entryA.index;
			int indexB = entryB.index;
			entryA.index = indexB;
			entryB.index = indexA;
			indexToEntry.Set(indexA, entryB);
			indexToEntry.Set(indexB, entryA);
		}

		/// <summary>Remove the last element of the heap (last in the index array).</summary>
		/// <remarks>
		/// Remove the last element of the heap (last in the index array).
		/// Do not call this on other entries; the last entry is only passed
		/// in for efficiency.
		/// </remarks>
		/// <param name="entry">the last entry in the array</param>
		private void RemoveLast(ArrayHeap.HeapEntry<E> entry)
		{
			indexToEntry.Remove(entry.index);
			Sharpen.Collections.Remove(objectToEntry, entry.@object);
		}

		private ArrayHeap.HeapEntry<E> GetEntry(E o)
		{
			ArrayHeap.HeapEntry<E> entry = objectToEntry[o];
			if (entry == null)
			{
				entry = new ArrayHeap.HeapEntry<E>();
				entry.index = Count;
				entry.@object = o;
				indexToEntry.Add(entry);
				objectToEntry[o] = entry;
			}
			return entry;
		}

		/// <summary>iterative heapify up: move item o at index up until correctly placed</summary>
		private int HeapifyUp(ArrayHeap.HeapEntry<E> entry)
		{
			int numSwaps = 0;
			while (true)
			{
				if (entry.index == 0)
				{
					break;
				}
				ArrayHeap.HeapEntry<E> parentEntry = Parent(entry);
				if (Compare(entry, parentEntry) >= 0)
				{
					break;
				}
				numSwaps++;
				Swap(entry, parentEntry);
			}
			return numSwaps;
		}

		/// <summary>
		/// On the assumption that
		/// leftChild(entry) and rightChild(entry) satisfy the heap property,
		/// make sure that the heap at entry satisfies this property by possibly
		/// percolating the element o downwards.
		/// </summary>
		/// <remarks>
		/// On the assumption that
		/// leftChild(entry) and rightChild(entry) satisfy the heap property,
		/// make sure that the heap at entry satisfies this property by possibly
		/// percolating the element o downwards.  I've replaced the obvious
		/// recursive formulation with an iterative one to gain (marginal) speed
		/// </remarks>
		private void HeapifyDown(ArrayHeap.HeapEntry<E> entry)
		{
			// int size = size();
			ArrayHeap.HeapEntry<E> minEntry;
			do
			{
				// = null;
				minEntry = entry;
				ArrayHeap.HeapEntry<E> leftEntry = LeftChild(entry);
				if (leftEntry != null)
				{
					if (Compare(minEntry, leftEntry) > 0)
					{
						minEntry = leftEntry;
					}
				}
				ArrayHeap.HeapEntry<E> rightEntry = RightChild(entry);
				if (rightEntry != null)
				{
					if (Compare(minEntry, rightEntry) > 0)
					{
						minEntry = rightEntry;
					}
				}
				if (minEntry != entry)
				{
					// Swap min and current
					Swap(minEntry, entry);
				}
			}
			while (minEntry != entry);
		}

		// at start of next loop, we set currentIndex to largestIndex
		// this indexation now holds current, so it is unchanged
		// log.info("Done with heapify down");
		// verify();
		/// <summary>
		/// Finds the object with the minimum key, removes it from the heap,
		/// and returns it.
		/// </summary>
		/// <returns>The object with minimum key</returns>
		public virtual E ExtractMin()
		{
			if (IsEmpty())
			{
				throw new NoSuchElementException();
			}
			ArrayHeap.HeapEntry<E> minEntry = indexToEntry[0];
			int lastIndex = Count - 1;
			if (lastIndex > 0)
			{
				ArrayHeap.HeapEntry<E> lastEntry = indexToEntry[lastIndex];
				Swap(lastEntry, minEntry);
				RemoveLast(minEntry);
				HeapifyDown(lastEntry);
			}
			else
			{
				RemoveLast(minEntry);
			}
			return minEntry.@object;
		}

		/// <summary>
		/// Finds the object with the minimum key and returns it, without
		/// modifying the heap.
		/// </summary>
		/// <returns>The object with minimum key</returns>
		public virtual E Min()
		{
			ArrayHeap.HeapEntry<E> minEntry = indexToEntry[0];
			return minEntry.@object;
		}

		/// <summary>Adds an object to the heap.</summary>
		/// <remarks>
		/// Adds an object to the heap.  If the object is already in the heap
		/// with worse score, this acts as a decrease key.  If the object is
		/// already present, with better score, it will NOT cause an
		/// "increase key".
		/// </remarks>
		/// <param name="o">an <code>Object</code> value</param>
		public override bool Add(E o)
		{
			DecreaseKey(o);
			return true;
		}

		/// <summary>
		/// Changes the position of an element o in the heap based on a
		/// change in the ordering of o.
		/// </summary>
		/// <remarks>
		/// Changes the position of an element o in the heap based on a
		/// change in the ordering of o.  If o's key has actually increased,
		/// it will do nothing, particularly not an "increase key".
		/// </remarks>
		/// <param name="o">An <code>Object</code> value</param>
		/// <returns>The number of swaps done on decrease.</returns>
		public virtual int DecreaseKey(E o)
		{
			ArrayHeap.HeapEntry<E> entry = GetEntry(o);
			if (o != entry.@object)
			{
				if (cmp.Compare(o, entry.@object) < 0)
				{
					entry.@object = o;
				}
			}
			return HeapifyUp(entry);
		}

		/// <summary>Checks if the heap is empty.</summary>
		/// <returns>a <code>boolean</code> value</returns>
		public override bool IsEmpty()
		{
			return indexToEntry.IsEmpty();
		}

		/// <summary>Get the number of elements in the heap.</summary>
		/// <returns>an <code>int</code> value</returns>
		public override int Count
		{
			get
			{
				return indexToEntry.Count;
			}
		}

		public override IEnumerator<E> GetEnumerator()
		{
			IHeap<E> tempHeap = new ArrayHeap<E>(cmp, Count);
			IList<E> tempList = new List<E>(Count);
			foreach (E obj in objectToEntry.Keys)
			{
				tempHeap.Add(obj);
			}
			while (!tempHeap.IsEmpty())
			{
				tempList.Add(tempHeap.ExtractMin());
			}
			return tempList.GetEnumerator();
		}

		/// <summary>Clears the heap.</summary>
		/// <remarks>
		/// Clears the heap.  Equivalent to calling extractMin repeatedly
		/// (but faster).
		/// </remarks>
		public override void Clear()
		{
			indexToEntry.Clear();
			objectToEntry.Clear();
		}

		public virtual void Dump()
		{
			for (int j = 0; j < indexToEntry.Count; j++)
			{
				log.Info(" " + j + " " + ((IScored)indexToEntry[j].@object).Score());
			}
		}

		public virtual void Verify()
		{
			for (int i = 0; i < indexToEntry.Count; i++)
			{
				if (i != 0)
				{
					// check ordering
					if (Compare(indexToEntry[i], indexToEntry[Parent(i)]) < 0)
					{
						log.Info("Error in the ordering of the heap! (" + i + ")");
						Dump();
						System.Environment.Exit(0);
					}
				}
				// check placement
				if (i != indexToEntry[i].index)
				{
					log.Info("Error in placement in the heap!");
				}
			}
		}

		/// <summary>Create an ArrayHeap.</summary>
		/// <param name="cmp">The objects added will be ordered using the <code>Comparator</code>.</param>
		public ArrayHeap(IComparator<E> cmp)
		{
			this.cmp = cmp;
			indexToEntry = new List<ArrayHeap.HeapEntry<E>>();
			objectToEntry = Generics.NewHashMap();
		}

		public ArrayHeap(IComparator<E> cmp, int initCapacity)
		{
			this.cmp = cmp;
			indexToEntry = new List<ArrayHeap.HeapEntry<E>>(initCapacity);
			objectToEntry = Generics.NewHashMap(initCapacity);
		}

		public virtual IList<E> AsList()
		{
			return new LinkedList<E>(this);
		}

		/// <summary>Prints the array entries in sorted comparator order.</summary>
		/// <returns>The array entries in sorted comparator order.</returns>
		public override string ToString()
		{
			List<E> result = new List<E>();
			foreach (E key in objectToEntry.Keys)
			{
				result.Add(key);
			}
			result.Sort(cmp);
			return result.ToString();
		}
	}
}
