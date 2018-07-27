


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A priority queue that has a fixed bounded size.</summary>
	/// <remarks>
	/// A priority queue that has a fixed bounded size.
	/// Notice that this class is implemented using a sorted set, which
	/// requires consistency between euqals() and compareTo() method.
	/// It decides whether two objects are equal based on their compareTo
	/// value; in other words, if two objects have the same priority,
	/// only one will be stored.
	/// </remarks>
	/// <author>Mengqiu Wang</author>
	[System.Serializable]
	public class BoundedPriorityQueue<E> : TreeSet<E>
	{
		private int remainingCapacity;

		private int initialCapacity;

		public BoundedPriorityQueue(int maxSize)
			: base()
		{
			this.initialCapacity = maxSize;
			this.remainingCapacity = maxSize;
		}

		public BoundedPriorityQueue(int maxSize, IComparator<E> comparator)
			: base(comparator)
		{
			this.initialCapacity = maxSize;
			this.remainingCapacity = maxSize;
		}

		public override void Clear()
		{
			base.Clear();
			remainingCapacity = initialCapacity;
		}

		/// <returns>true if element was successfully added, false otherwise</returns>
		public override bool Add(E e)
		{
			if (remainingCapacity == 0 && Count == 0)
			{
				return false;
			}
			else
			{
				if (remainingCapacity > 0)
				{
					// still has room, add element 
					bool added = base.Add(e);
					if (added)
					{
						remainingCapacity--;
					}
					return added;
				}
				else
				{
					// compare new element with least element in queue
					int compared = base.Comparator().Compare(e, this.First());
					if (compared == 1)
					{
						// new element is larger, replace old element 
						PollFirst();
						base.Add(e);
						return true;
					}
					else
					{
						// new element is smaller, discard
						return false;
					}
				}
			}
		}
	}
}
