using System;
using System.Collections.Generic;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Implements a finite beam, taking a comparator (default is
	/// ScoredComparator.ASCENDING_COMPARATOR, the MAX object according to
	/// the comparator is the one to be removed) and a beam size on
	/// construction (default is 100).
	/// </summary>
	/// <remarks>
	/// Implements a finite beam, taking a comparator (default is
	/// ScoredComparator.ASCENDING_COMPARATOR, the MAX object according to
	/// the comparator is the one to be removed) and a beam size on
	/// construction (default is 100).  Adding an object may cause the
	/// worst-scored object to be removed from the beam (and that object
	/// may well be the newly added object itself).
	/// </remarks>
	/// <author><a href="mailto:klein@cs.stanford.edu">Dan Klein</a></author>
	/// <version>1.0</version>
	public class Beam<T> : AbstractSet<T>
	{
		protected internal readonly int maxBeamSize;

		protected internal readonly IHeap<T> elements;

		public virtual int Capacity()
		{
			return maxBeamSize;
		}

		public override int Count
		{
			get
			{
				return elements.Size();
			}
		}

		public override IEnumerator<T> GetEnumerator()
		{
			return AsSortedList().GetEnumerator();
		}

		public virtual IList<T> AsSortedList()
		{
			LinkedList<T> list = new LinkedList<T>();
			for (IEnumerator<T> i = elements.Iterator(); i.MoveNext(); )
			{
				list.AddFirst(i.Current);
			}
			return list;
		}

		public override bool Add(T o)
		{
			bool added = true;
			elements.Add(o);
			while (Count > Capacity())
			{
				object dumped = elements.ExtractMin();
				if (dumped.Equals(o))
				{
					added = false;
				}
			}
			return added;
		}

		public override bool Remove(object o)
		{
			//return elements.remove(o);
			throw new NotSupportedException();
		}

		public Beam()
			: this(100)
		{
		}

		public Beam(int maxBeamSize)
			: this(maxBeamSize, ErasureUtils.UncheckedCast<IComparator<T>>(ScoredComparator.AscendingComparator))
		{
		}

		public Beam(int maxBeamSize, IComparator<T> cmp)
		{
			// TODO dlwh: This strikes me as unsafe even now.
			elements = new ArrayHeap<T>(cmp);
			this.maxBeamSize = maxBeamSize;
		}
		/*
		* This is a test
		public static void main(String[] args) {
		Beam<ScoredObject> b = new Beam<ScoredObject>(2, ScoredComparator.ASCENDING_COMPARATOR);
		b.add(new ScoredObject<String>("1", 1.0));
		b.add(new ScoredObject<String>("2", 2.0));
		b.add(new ScoredObject<String>("3", 3.0));
		b.add(new ScoredObject<String>("0", 0.0));
		for (Iterator<ScoredObject> bI = b.iterator(); bI.hasNext();) {
		ScoredObject sO = bI.next();
		System.out.println(sO);
		}
		}
		*/
	}
}
