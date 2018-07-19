using System;
using System.Collections.Generic;
using System.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A collection that maps between a vocabulary of type E and a
	/// continuous non-negative integer index series beginning (inclusively) at 0.
	/// </summary>
	/// <remarks>
	/// A collection that maps between a vocabulary of type E and a
	/// continuous non-negative integer index series beginning (inclusively) at 0.
	/// <p>Often one uses a List to associate a unique index with each Object
	/// (e.g. controlled vocabulary, feature map, etc.). Index offers constant-time
	/// performance for both <code>index -&gt; Object</code> (
	/// <see cref="IIndex{E}.Get(int)"/>
	/// ) and <code>
	/// Object -&gt; index</code> (
	/// <see cref="IIndex{E}.IndexOf(object)"/>
	/// ) as well as for
	/// <see cref="IIndex{E}.Contains(object)"/>
	/// .
	/// Otherwise it behaves like a normal list. Index also
	/// supports
	/// <see cref="IIndex{E}.Lock()"/>
	/// and
	/// <see cref="IIndex{E}.Unlock()"/>
	/// to ensure that it's
	/// only modified when desired.</p>
	/// </remarks>
	/// <author>Daniel Cer</author>
	/// <?/>
	public interface IIndex<E> : IEnumerable<E>
	{
		/// <summary>Returns the number of indexed objects.</summary>
		/// <returns>the number of indexed objects.</returns>
		int Size();

		/// <summary>Gets the object whose index is the integer argument.</summary>
		/// <param name="i">the integer index to be queried for the corresponding argument</param>
		/// <returns>the object whose index is the integer argument.</returns>
		E Get(int i);

		/// <summary>
		/// Returns the integer index of the Object in the Index or -1 if the Object
		/// is not already in the Index.
		/// </summary>
		/// <remarks>
		/// Returns the integer index of the Object in the Index or -1 if the Object
		/// is not already in the Index. This operation never changes the Index.
		/// </remarks>
		/// <param name="o">The Object whose index is desired.</param>
		/// <returns>The index of the Object argument. Returns -1 if the object is not in the index.</returns>
		int IndexOf(E o);

		/// <summary>Takes an Object and returns the integer index of the Object.</summary>
		/// <remarks>
		/// Takes an Object and returns the integer index of the Object.
		/// If the object was already in the index, it returns its existing
		/// index, otherwise it adds it to the index first.
		/// Except if the index is locked, and then it returns -1 if the
		/// object is not already in the index.
		/// </remarks>
		/// <param name="o">the Object whose index is desired.</param>
		/// <returns>
		/// the index of the Object argument. Normally a non-negative integer.
		/// Returns -1 if the object is not in the index and the Index is locked.
		/// </returns>
		int AddToIndex(E o);

		/// <summary>
		/// Takes an Object and returns the integer index of the Object,
		/// perhaps adding it to the index first.
		/// </summary>
		/// <remarks>
		/// Takes an Object and returns the integer index of the Object,
		/// perhaps adding it to the index first.
		/// Returns -1 if the Object is not in the Index.
		/// (Note: indexOf(x, true) is the direct replacement for the number(x)
		/// method in the old Numberer class.)
		/// </remarks>
		/// <param name="o">the Object whose index is desired.</param>
		/// <param name="add">Whether it is okay to add new items to the index</param>
		/// <returns>
		/// the index of the Object argument.  Returns -1 if the object is not in the index
		/// or if the Index is locked.
		/// </returns>
		[System.ObsoleteAttribute(@"You should use either the addToIndex(E) or indexOf(E) methods instead")]
		int IndexOf(E o, bool add);

		// mg2009. Methods below were temporarily added when IndexInterface was renamed
		// to Index. These methods are currently (2009-03-09) needed in order to have core classes
		// of JavaNLP (Dataset, LinearClassifier, etc.) use Index instead of HashIndex.
		// Possible JavaNLP task: delete some of these methods.
		/// <summary>
		/// Returns a complete
		/// <see cref="System.Collections.IList{E}"/>
		/// of indexed objects, in the order of their indices.
		/// </summary>
		/// <returns>
		/// a complete
		/// <see cref="System.Collections.IList{E}"/>
		/// of indexed objects
		/// </returns>
		IList<E> ObjectsList();

		/// <summary>
		/// Looks up the objects corresponding to an array of indices, and returns them in a
		/// <see cref="System.Collections.ICollection{E}"/>
		/// .
		/// </summary>
		/// <param name="indices">An array of indices</param>
		/// <returns>
		/// a
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of the objects corresponding to the indices argument.
		/// </returns>
		ICollection<E> Objects(int[] indices);

		/// <summary>Queries the Index for whether it's locked or not.</summary>
		/// <returns>whether or not the Index is locked</returns>
		bool IsLocked();

		/// <summary>Locks the Index.</summary>
		/// <remarks>
		/// Locks the Index.  A locked index cannot have new elements added to it (calls to
		/// <see cref="IIndex{E}.Add(object)"/>
		/// will
		/// leave the Index unchanged and return <code>false</code>).
		/// </remarks>
		void Lock();

		/// <summary>Unlocks the Index.</summary>
		/// <remarks>
		/// Unlocks the Index.  A locked index cannot have new elements added to it (calls to
		/// <see cref="IIndex{E}.Add(object)"/>
		/// will
		/// leave the Index unchanged and return <code>false</code>).
		/// </remarks>
		void Unlock();

		/// <summary>
		/// Save the contents of this index into string form, as part of a larger
		/// text-serialization.
		/// </summary>
		/// <param name="out">Writer to save to.</param>
		/// <exception cref="System.IO.IOException">Exception thrown if cannot save.</exception>
		void SaveToWriter(TextWriter @out);

		/// <summary>Save the contents of this index into a file.</summary>
		/// <param name="s">File name.</param>
		void SaveToFilename(string s);

		// Subset of the Collection interface.  These come from old uses of HashIndex. Avoid using these.
		bool Contains(object o);

		// cdm: keep this, it seems reasonable
		bool Add(E e);

		// cdm: Many, many uses; could be replaced with indexOf, but why bother?
		bool AddAll<_T0>(ICollection<_T0> c)
			where _T0 : E;

		// okay to have.
		void Clear();
		// cdm: barely used.
	}
}
