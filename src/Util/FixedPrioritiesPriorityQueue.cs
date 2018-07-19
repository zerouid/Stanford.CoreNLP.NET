using System;
using System.Collections.Generic;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A priority queue based on a binary heap.</summary>
	/// <remarks>
	/// A priority queue based on a binary heap.  This implementation trades
	/// flexibility for speed: while it is up to 2x faster than
	/// <see cref="BinaryHeapPriorityQueue{E}"/>
	/// and nearly as fast as
	/// <see cref="Java.Util.PriorityQueue{E}"/>
	/// , it does not support removing or changing the
	/// priority of an element.  Also, while
	/// <see cref="FixedPrioritiesPriorityQueue{E}.GetPriority(object)">getPriority(Object key)</see>
	/// is supported, performance will be linear, not
	/// constant.
	/// </remarks>
	/// <author>Dan Klein, Bill MacCartney</author>
	[System.Serializable]
	public class FixedPrioritiesPriorityQueue<E> : AbstractSet<E>, IPriorityQueue<E>, IEnumerator<E>, ICloneable
	{
		private const long serialVersionUID = 1L;

		private int size;

		private int capacity;

		private IList<E> elements;

		private double[] priorities;

		public FixedPrioritiesPriorityQueue()
			: this(15)
		{
		}

		public FixedPrioritiesPriorityQueue(int capacity)
		{
			// constructors ----------------------------------------------------------
			int legalCapacity = 0;
			while (legalCapacity < capacity)
			{
				legalCapacity = 2 * legalCapacity + 1;
			}
			Grow(legalCapacity);
		}

		// iterator methods ------------------------------------------------------
		/// <summary>Returns true if the priority queue is non-empty</summary>
		public virtual bool MoveNext()
		{
			return !IsEmpty();
		}

		/// <summary>
		/// Returns the element in the queue with highest priority, and pops it from
		/// the queue.
		/// </summary>
		/// <exception cref="Java.Util.NoSuchElementException"/>
		public virtual E Current
		{
			get
			{
				return RemoveFirst();
			}
		}

		/// <summary>Not supported -- next() already removes the head of the queue.</summary>
		public virtual void Remove()
		{
			throw new NotSupportedException();
		}

		// PriorityQueue methods -------------------------------------------------
		/// <summary>Adds a key to the queue with the given priority.</summary>
		/// <remarks>
		/// Adds a key to the queue with the given priority.  If the key is already in
		/// the queue, it will be added an additional time, NOT promoted/demoted.
		/// </remarks>
		public virtual bool Add(E key, double priority)
		{
			if (size == capacity)
			{
				Grow(2 * capacity + 1);
			}
			elements.Add(key);
			priorities[size] = priority;
			HeapifyUp(size);
			size++;
			return true;
		}

		/// <summary>Not supported in this implementation.</summary>
		public virtual bool ChangePriority(E key, double priority)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Returns the highest-priority element without removing it from the
		/// queue.
		/// </summary>
		public virtual E GetFirst()
		{
			if (Count > 0)
			{
				return elements[0];
			}
			throw new NoSuchElementException();
		}

		/// <summary>
		/// Note that this method will be linear (not constant) time in this
		/// implementation!  Better not to use it.
		/// </summary>
		public virtual double GetPriority(object key)
		{
			for (int i = 0; i < sz; i++)
			{
				if (elements[i].Equals(key))
				{
					return priorities[i];
				}
			}
			throw new NoSuchElementException();
		}

		/// <summary>Gets the priority of the highest-priority element of the queue.</summary>
		public virtual double GetPriority()
		{
			// check empty other way around
			if (Count > 0)
			{
				return priorities[0];
			}
			throw new NoSuchElementException();
		}

		/// <summary>Not supported in this implementation.</summary>
		public virtual bool RelaxPriority(E key, double priority)
		{
			throw new NotSupportedException();
		}

		/// <summary>Returns the highest-priority element and removes it from the queue.</summary>
		/// <exception cref="Java.Util.NoSuchElementException"/>
		public virtual E RemoveFirst()
		{
			E first = GetFirst();
			Swap(0, size - 1);
			size--;
			elements.Remove(size);
			HeapifyDown(0);
			return first;
		}

		public virtual IList<E> ToSortedList()
		{
			// initialize with size
			IList<E> list = new List<E>();
			while (MoveNext())
			{
				list.Add(Current);
			}
			return list;
		}

		/// <summary>Number of elements in the queue.</summary>
		public override int Count
		{
			get
			{
				// Set methods -----------------------------------------------------------
				return size;
			}
		}

		public override void Clear()
		{
			size = 0;
			Grow(15);
		}

		public override IEnumerator<E> GetEnumerator()
		{
			return Java.Util.Collections.UnmodifiableCollection(ToSortedList()).GetEnumerator();
		}

		// -----------------------------------------------------------------------
		private void Grow(int newCapacity)
		{
			IList<E> newElements = new List<E>(newCapacity);
			double[] newPriorities = new double[newCapacity];
			if (size > 0)
			{
				Sharpen.Collections.AddAll(newElements, elements);
				System.Array.Copy(priorities, 0, newPriorities, 0, priorities.Length);
			}
			elements = newElements;
			priorities = newPriorities;
			capacity = newCapacity;
		}

		private static int Parent(int loc)
		{
			return (loc - 1) / 2;
		}

		private static int LeftChild(int loc)
		{
			return 2 * loc + 1;
		}

		private static int RightChild(int loc)
		{
			return 2 * loc + 2;
		}

		private void HeapifyUp(int loc)
		{
			if (loc == 0)
			{
				return;
			}
			int parent = Parent(loc);
			if (priorities[loc] > priorities[parent])
			{
				Swap(loc, parent);
				HeapifyUp(parent);
			}
		}

		private void HeapifyDown(int loc)
		{
			int max = loc;
			int leftChild = LeftChild(loc);
			if (leftChild < Count)
			{
				double priority = priorities[loc];
				double leftChildPriority = priorities[leftChild];
				if (leftChildPriority > priority)
				{
					max = leftChild;
				}
				int rightChild = RightChild(loc);
				if (rightChild < Count)
				{
					double rightChildPriority = priorities[RightChild(loc)];
					if (rightChildPriority > priority && rightChildPriority > leftChildPriority)
					{
						max = rightChild;
					}
				}
			}
			if (max == loc)
			{
				return;
			}
			Swap(loc, max);
			HeapifyDown(max);
		}

		private void Swap(int loc1, int loc2)
		{
			double tempPriority = priorities[loc1];
			E tempElement = elements[loc1];
			priorities[loc1] = priorities[loc2];
			elements.Set(loc1, elements[loc2]);
			priorities[loc2] = tempPriority;
			elements.Set(loc2, tempElement);
		}

		// -----------------------------------------------------------------------
		/// <summary>Returns a representation of the queue in decreasing priority order.</summary>
		public override string ToString()
		{
			return ToString(Count, null);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual string ToString(int maxKeysToPrint)
		{
			return ToString(maxKeysToPrint, "%.3f");
		}

		/// <summary>
		/// Returns a representation of the queue in decreasing priority order,
		/// displaying at most maxKeysToPrint elements.
		/// </summary>
		public virtual string ToString(int maxKeysToPrint, string dblFmt)
		{
			if (maxKeysToPrint <= 0)
			{
				maxKeysToPrint = int.MaxValue;
			}
			Edu.Stanford.Nlp.Util.FixedPrioritiesPriorityQueue<E> pq = Clone();
			StringBuilder sb = new StringBuilder("[");
			int numKeysPrinted = 0;
			while (numKeysPrinted < maxKeysToPrint && pq.MoveNext())
			{
				double priority = pq.GetPriority();
				E element = pq.Current;
				sb.Append(element);
				sb.Append('=');
				if (dblFmt == null)
				{
					sb.Append(priority);
				}
				else
				{
					sb.Append(string.Format(dblFmt, priority));
				}
				if (numKeysPrinted < Count - 1)
				{
					sb.Append(", ");
				}
				numKeysPrinted++;
			}
			if (numKeysPrinted < Count)
			{
				sb.Append("...");
			}
			sb.Append(']');
			return sb.ToString();
		}

		/// <summary>Returns a clone of this priority queue.</summary>
		/// <remarks>
		/// Returns a clone of this priority queue.  Modifications to one will not
		/// affect modifications to the other.
		/// </remarks>
		public Edu.Stanford.Nlp.Util.FixedPrioritiesPriorityQueue<E> Clone()
		{
			Edu.Stanford.Nlp.Util.FixedPrioritiesPriorityQueue<E> clonePQ;
			clonePQ = ErasureUtils.UncheckedCast(base.MemberwiseClone());
			clonePQ.elements = new List<E>(capacity);
			clonePQ.priorities = new double[capacity];
			if (Count > 0)
			{
				Sharpen.Collections.AddAll(clonePQ.elements, elements);
				System.Array.Copy(priorities, 0, clonePQ.priorities, 0, Count);
			}
			return clonePQ;
		}
	}
}
