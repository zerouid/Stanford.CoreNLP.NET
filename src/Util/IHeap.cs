using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Heap interface.</summary>
	/// <remarks>
	/// Heap interface.
	/// These heaps implement a decreaseKey operation, which requires
	/// a separate Object to Index map, and for objects to be unique in the Heap.
	/// <p/>
	/// An interface cannot specify constructors, but it is nevertheless
	/// expected that an implementation of this interface has a constructor
	/// that takes a Comparator, which is used for ordering ("scoring")
	/// objects:
	/// <code>public Heap(Comparator cmp) {}</code>
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <version>12/14/00</version>
	public interface IHeap<E>
	{
		/// <summary>Returns the minimum object, then removes that object from the heap.</summary>
		/// <returns>the minimum object</returns>
		E ExtractMin();

		/// <summary>Returns the minimum Object in this heap.</summary>
		/// <remarks>Returns the minimum Object in this heap. The heap is not modified.</remarks>
		/// <returns>the minimum object</returns>
		E Min();

		/// <summary>Adds the object to the heap.</summary>
		/// <remarks>
		/// Adds the object to the heap.  If the object is in the heap, this
		/// should act as a decrease-key (if the new version has better
		/// priority) or a no-op (otherwise).
		/// </remarks>
		/// <param name="o">a new element</param>
		/// <returns>true, always</returns>
		bool Add(E o);

		/// <summary>The number of elements currently in the heap.</summary>
		/// <returns>the heap's size</returns>
		int Size();

		/// <summary>Returns true iff the heap is empty.</summary>
		/// <returns>a <code>boolean</code> value</returns>
		bool IsEmpty();

		/// <summary>Raises the priority of an object in the heap.</summary>
		/// <remarks>
		/// Raises the priority of an object in the heap.  This works in a
		/// somewhat unusual way -- the object <code>o</code> should have
		/// changed with respect to the comparator passed in to the heap on
		/// construction.  However, it should NOT have changed with respect
		/// to its equals() method.  This is unlike the Java SortedSet where
		/// the comparator should be consistent with equals(); here they
		/// should not match.
		/// </remarks>
		/// <param name="o">an <code>Object</code> value which has changed wrt the heap's ordering</param>
		/// <returns>the cost of the decrease-key operation, for analysis</returns>
		int DecreaseKey(E o);

		// should be void; int for analysis
		/// <summary>Returns an iterator over its elements, in order.</summary>
		/// <returns>an <code>Iterator</code> value</returns>
		IEnumerator<E> Iterator();
	}
}
