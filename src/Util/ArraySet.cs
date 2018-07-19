using System.Collections.Generic;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>An array-backed set.</summary>
	/// <author>Roger Levy (rog@stanford.edu)</author>
	[System.Serializable]
	public class ArraySet<E> : AbstractSet<E>
	{
		private readonly IList<E> backer;

		/// <summary>Constructs an ArraySet.</summary>
		public ArraySet()
			: this(10)
		{
		}

		/// <summary>Constructs an ArraySet, using the given list as the backing collection.</summary>
		/// <remarks>
		/// Constructs an ArraySet, using the given list as the backing collection.
		/// Note that this is not a copy constructor!
		/// </remarks>
		public ArraySet(IList<E> source)
		{
			this.backer = source;
		}

		/// <summary>Constructs an ArraySet with specified initial size of backing array.</summary>
		/// <param name="initialSize">initial size of the backing array.</param>
		public ArraySet(int initialSize)
		{
			backer = new List<E>(initialSize);
		}

		/// <summary>Constructs an ArraySet with the specified elements.</summary>
		/// <param name="elements">the elements to be put in the set.</param>
		[SafeVarargs]
		public ArraySet(params E[] elements)
			: this(elements.Length)
		{
			foreach (E element in elements)
			{
				Add(element);
			}
		}

		/// <summary>Returns iterator over elements of the set.</summary>
		public override IEnumerator<E> GetEnumerator()
		{
			return backer.GetEnumerator();
		}

		/// <summary>Adds element to set.</summary>
		/// <param name="e">the element to be added.</param>
		/// <returns><code>false</code> if the set already contained (vis. <code>.equals()</code>) the specified element; <code>true</code> otherwise.</returns>
		public override bool Add(E e)
		{
			if (backer.Contains(e))
			{
				return false;
			}
			else
			{
				return backer.Add(e);
			}
		}

		/// <summary>Returns size of set.</summary>
		/// <returns>number of elements in set.</returns>
		public override int Count
		{
			get
			{
				return backer.Count;
			}
		}

		private const long serialVersionUID = 1L;
	}
}
