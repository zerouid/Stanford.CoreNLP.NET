using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;







namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Implements an Index that supports constant-time lookup in
	/// both directions (via
	/// <c>get(int)</c>
	/// and
	/// <c>indexOf(E)</c>
	/// .
	/// The
	/// <c>indexOf(E)</c>
	/// method compares objects by
	/// <c>equals()</c>
	/// , as other Collections.
	/// <p/>
	/// The typical usage would be:
	/// <p>
	/// <c>Index&lt;String&gt; index = new Index&lt;String&gt;(collection);</c>
	/// <p> followed by
	/// <p>
	/// <c>int i = index.indexOf(str);</c>
	/// <p> or
	/// <p>
	/// <c>String s = index.get(i);</c>
	/// <p>An Index can be locked or unlocked: a locked index cannot have new
	/// items added to it.
	/// </summary>
	/// <author><a href="mailto:klein@cs.stanford.edu">Dan Klein</a></author>
	/// <version>1.0</version>
	/// <seealso cref="Java.Util.AbstractCollection{E}"/>
	/// <since>1.0</since>
	/// <author><a href="mailto:yeh1@stanford.edu">Eric Yeh</a> (added write to/load from buffer)</author>
	[System.Serializable]
	public class HashIndex<E> : AbstractCollection<E>, IIndex<E>, IRandomAccess
	{
		private readonly IList<E> objects;

		private readonly IDictionary<E, int> indexes;

		private bool locked;

		private const long serialVersionUID = 5398562825928375260L;

		// todo [cdm 2014]: Delete "extends AbstractCollection<E>" but this will break serialization....
		// these variables are also used in IntArrayIndex
		// <-- Should really almost always be an ArrayList
		// = false; // Mutable
		/// <summary>Clears this Index.</summary>
		public override void Clear()
		{
			objects.Clear();
			indexes.Clear();
		}

		/// <summary>Returns the index of each elem in a List.</summary>
		/// <param name="elements">The list of items</param>
		/// <returns>An array of indices</returns>
		public virtual int[] Indices(ICollection<E> elements)
		{
			int[] indices = new int[elements.Count];
			int i = 0;
			foreach (E elem in elements)
			{
				indices[i++] = IndexOf(elem);
			}
			return indices;
		}

		/// <summary>
		/// Looks up the objects corresponding to an array of indices, and returns them in a
		/// <see cref="System.Collections.ICollection{E}"/>
		/// .
		/// This collection is not a copy, but accesses the data structures of the Index.
		/// </summary>
		/// <param name="indices">An array of indices</param>
		/// <returns>
		/// a
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of the objects corresponding to the indices argument.
		/// </returns>
		public virtual ICollection<E> Objects(int[] indices)
		{
			return new _AbstractList_74(this, indices);
		}

		private sealed class _AbstractList_74 : AbstractList<E>
		{
			public _AbstractList_74(HashIndex<E> _enclosing, int[] indices)
			{
				this._enclosing = _enclosing;
				this.indices = indices;
			}

			public override E Get(int index)
			{
				return this._enclosing.objects[indices[index]];
			}

			public override int Count
			{
				get
				{
					return indices.Length;
				}
			}

			private readonly HashIndex<E> _enclosing;

			private readonly int[] indices;
		}

		/// <summary>Returns the number of indexed objects.</summary>
		/// <returns>the number of indexed objects.</returns>
		public override int Count
		{
			get
			{
				return objects.Count;
			}
		}

		/// <summary>Gets the object whose index is the integer argument.</summary>
		/// <param name="i">the integer index to be queried for the corresponding argument</param>
		/// <returns>the object whose index is the integer argument.</returns>
		public virtual E Get(int i)
		{
			if (i < 0 || i >= objects.Count)
			{
				throw new IndexOutOfRangeException("Index " + i + " outside the bounds [0," + Count + ")");
			}
			return objects[i];
		}

		/// <summary>
		/// Returns a complete
		/// <see cref="System.Collections.IList{E}"/>
		/// of indexed objects, in the order of their indices.  <b>DANGER!</b>
		/// The current implementation returns the actual index list, not a defensive copy.  Messing with this List
		/// can seriously screw up the state of the Index.  (perhaps this method needs to be eliminated? I don't think it's
		/// ever used in ways that we couldn't use the Index itself for directly.  --Roger, 12/29/04)
		/// </summary>
		/// <returns>
		/// a complete
		/// <see cref="System.Collections.IList{E}"/>
		/// of indexed objects
		/// </returns>
		public virtual IList<E> ObjectsList()
		{
			return objects;
		}

		/// <summary>Queries the Index for whether it's locked or not.</summary>
		/// <returns>whether or not the Index is locked</returns>
		public virtual bool IsLocked()
		{
			return locked;
		}

		/// <summary>Locks the Index.</summary>
		/// <remarks>
		/// Locks the Index.  A locked index cannot have new elements added to it (calls to
		/// <see cref="HashIndex{E}.Add(object)"/>
		/// will
		/// leave the Index unchanged and return
		/// <see langword="false"/>
		/// ).
		/// </remarks>
		public virtual void Lock()
		{
			locked = true;
		}

		/// <summary>Unlocks the Index.</summary>
		/// <remarks>
		/// Unlocks the Index.  A locked index cannot have new elements added to it (calls to
		/// <see cref="HashIndex{E}.Add(object)"/>
		/// will
		/// leave the Index unchanged and return
		/// <see langword="false"/>
		/// ).
		/// </remarks>
		public virtual void Unlock()
		{
			locked = false;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual int IndexOf(E o)
		{
			int index = indexes[o];
			if (index == null)
			{
				return -1;
			}
			return index;
		}

		public virtual int AddToIndex(E o)
		{
			int index = indexes[o];
			if (index == null)
			{
				if (!locked)
				{
					try
					{
						semaphore.Acquire();
						index = indexes[o];
						if (index == null)
						{
							index = objects.Count;
							objects.Add(o);
							indexes[o] = index;
						}
						semaphore.Release();
					}
					catch (Exception e)
					{
						throw new RuntimeInterruptedException(e);
					}
				}
				else
				{
					return -1;
				}
			}
			return index;
		}

		/// <summary>Add the given item to the index, but without taking any locks.</summary>
		/// <remarks>
		/// Add the given item to the index, but without taking any locks.
		/// Use this method with care!
		/// But, this offers a noticable performance improvement if it is safe to use.
		/// </remarks>
		/// <seealso cref="IIndex{E}.AddToIndex(object)"/>
		public virtual int AddToIndexUnsafe(E o)
		{
			if (indexes.IsEmpty())
			{
				// a surprisingly common case in TokensRegex
				objects.Add(o);
				indexes[o] = 0;
				return 0;
			}
			else
			{
				int index = indexes[o];
				if (index == null)
				{
					if (locked)
					{
						index = -1;
					}
					else
					{
						index = objects.Count;
						objects.Add(o);
						indexes[o] = index;
					}
				}
				return index;
			}
		}

		/// <summary>
		/// Takes an Object and returns the integer index of the Object,
		/// perhaps adding it to the index first.
		/// </summary>
		/// <remarks>
		/// Takes an Object and returns the integer index of the Object,
		/// perhaps adding it to the index first.
		/// Returns -1 if the Object is not in the Index.
		/// <p>
		/// <i>Notes:</i> The method indexOf(x, true) is the direct replacement for
		/// the number(x) method in the old Numberer class.  This method now uses a
		/// Semaphore object to make the index safe for concurrent multithreaded
		/// usage. (CDM: Is this better than using a synchronized block?)
		/// </remarks>
		/// <param name="o">the Object whose index is desired.</param>
		/// <param name="add">Whether it is okay to add new items to the index</param>
		/// <returns>The index of the Object argument.  Returns -1 if the object is not in the index.</returns>
		[Obsolete]
		public virtual int IndexOf(E o, bool add)
		{
			if (add)
			{
				return AddToIndex(o);
			}
			else
			{
				return IndexOf(o);
			}
		}

		private readonly Semaphore semaphore = new Semaphore(1);

		// TODO: delete this when breaking serialization because we can leach off of AbstractCollection
		/// <summary>Adds every member of Collection to the Index.</summary>
		/// <remarks>Adds every member of Collection to the Index. Does nothing for members already in the Index.</remarks>
		/// <returns>
		/// true if some item was added to the index and false if no
		/// item was already in the index or if the index is locked
		/// </returns>
		public override bool AddAll<_T0>(ICollection<_T0> c)
		{
			bool changed = false;
			foreach (E element in c)
			{
				changed |= Add(element);
			}
			//changed &= add(element);
			return changed;
		}

		/// <summary>Adds an object to the Index.</summary>
		/// <remarks>
		/// Adds an object to the Index. If it was already in the Index,
		/// then nothing is done.  If it is not in the Index, then it is
		/// added iff the Index hasn't been locked.
		/// </remarks>
		/// <returns>
		/// true if the item was added to the index and false if the
		/// item was already in the index or if the index is locked
		/// </returns>
		public override bool Add(E o)
		{
			int index = indexes[o];
			if (index == null && !locked)
			{
				index = objects.Count;
				objects.Add(o);
				indexes[o] = index;
				return true;
			}
			return false;
		}

		/// <summary>Checks whether an Object already has an index in the Index</summary>
		/// <param name="o">the object to be queried.</param>
		/// <returns>true iff there is an index for the queried object.</returns>
		public override bool Contains(object o)
		{
			return indexes.Contains(o);
		}

		/// <summary>Creates a new Index.</summary>
		public HashIndex()
			: base()
		{
			objects = new List<E>();
			indexes = Generics.NewHashMap();
		}

		/// <summary>Creates a new Index.</summary>
		/// <param name="capacity">Initial capacity of Index.</param>
		public HashIndex(int capacity)
			: base()
		{
			objects = new List<E>(capacity);
			indexes = Generics.NewHashMap(capacity);
		}

		/// <summary>Create a new <code>HashIndex</code>, backed by the given collection types.</summary>
		/// <param name="objLookupFactory">
		/// The constructor for the object lookup -- traditionally an
		/// <see cref="System.Collections.ArrayList{E}"/>
		/// .
		/// </param>
		/// <param name="indexLookupFactory">
		/// The constructor for the index lookup -- traditionally a
		/// <see cref="System.Collections.Hashtable{K, V}"/>
		/// .
		/// </param>
		public HashIndex(ISupplier<IList<E>> objLookupFactory, ISupplier<IDictionary<E, int>> indexLookupFactory)
			: this(objLookupFactory.Get(), indexLookupFactory.Get())
		{
		}

		/// <summary>Private constructor for supporting the unmodifiable view.</summary>
		private HashIndex(IList<E> objects, IDictionary<E, int> indexes)
			: base()
		{
			this.objects = objects;
			this.indexes = indexes;
		}

		/// <summary>Creates a new Index and adds every member of c to it.</summary>
		/// <param name="c">A collection of objects</param>
		public HashIndex(ICollection<E> c)
			: this()
		{
			Sharpen.Collections.AddAll(this, c);
		}

		public HashIndex(IIndex<E> index)
			: this()
		{
			// TODO: this assumes that no index supports deletion
			Sharpen.Collections.AddAll(this, index.ObjectsList());
		}

		public virtual void SaveToFilename(string file)
		{
			BufferedWriter bw = null;
			try
			{
				bw = new BufferedWriter(new FileWriter(file));
				for (int i = 0; i < sz; i++)
				{
					bw.Write(i + "=" + Get(i) + '\n');
				}
				bw.Close();
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			finally
			{
				if (bw != null)
				{
					try
					{
						bw.Close();
					}
					catch (IOException)
					{
					}
				}
			}
		}

		// give up
		/// <summary>This assumes each line is of the form (number=value) and it adds each value in order of the lines in the file.</summary>
		/// <remarks>
		/// This assumes each line is of the form (number=value) and it adds each value in order of the lines in the file.
		/// Warning: This ignores the value of number, and just indexes each value it encounters in turn!
		/// </remarks>
		/// <param name="file">Which file to load</param>
		/// <returns>An index built out of the lines in the file</returns>
		public static IIndex<string> LoadFromFilename(string file)
		{
			IIndex<string> index = new Edu.Stanford.Nlp.Util.HashIndex<string>();
			BufferedReader br = null;
			try
			{
				br = IOUtils.ReaderFromString(file);
				for (string line; (line = br.ReadLine()) != null; )
				{
					int start = line.IndexOf('=');
					if (start == -1 || start == line.Length - 1)
					{
						continue;
					}
					index.Add(Sharpen.Runtime.Substring(line, start + 1));
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(br);
			}
			return index;
		}

		/// <summary>
		/// This saves the contents of this index into string form, as part of a larger
		/// text-serialization.
		/// </summary>
		/// <remarks>
		/// This saves the contents of this index into string form, as part of a larger
		/// text-serialization.  This is not intended to act as a standalone routine,
		/// instead being called from the text-serialization routine for a component
		/// that makes use of an Index, so everything can be stored in one file.  This is
		/// similar to
		/// <c>saveToFileName</c>
		/// .
		/// </remarks>
		/// <param name="bw">Writer to save to.</param>
		/// <exception cref="System.IO.IOException">Exception thrown if cannot save.</exception>
		public virtual void SaveToWriter(TextWriter bw)
		{
			for (int i = 0; i < sz; i++)
			{
				bw.Write(i + "=" + Get(i) + '\n');
			}
		}

		/// <summary>
		/// This is the analogue of
		/// <c>loadFromFilename</c>
		/// , and is intended to be included in a routine
		/// that unpacks a text-serialized form of an object that incorporates an Index.
		/// NOTE: presumes that the next readLine() will read in the first line of the
		/// portion of the text file representing the saved Index.  Currently reads until it
		/// encounters a blank line, consuming that line and returning the Index.
		/// TODO: figure out how best to terminate: currently a blank line is considered to be a terminator.
		/// </summary>
		/// <param name="br">The Reader to read the index from</param>
		/// <returns>An Index read from a file</returns>
		/// <exception cref="System.IO.IOException"/>
		public static IIndex<string> LoadFromReader(BufferedReader br)
		{
			Edu.Stanford.Nlp.Util.HashIndex<string> index = new Edu.Stanford.Nlp.Util.HashIndex<string>();
			string line = br.ReadLine();
			// terminate if EOF reached, or if a blank line is encountered.
			while ((line != null) && (line.Length > 0))
			{
				int start = line.IndexOf('=');
				if (start == -1 || start == line.Length - 1)
				{
					continue;
				}
				index.Add(Sharpen.Runtime.Substring(line, start + 1));
				line = br.ReadLine();
			}
			return index;
		}

		/// <summary>Returns a readable version of the Index contents</summary>
		/// <returns>A String showing the full index contents</returns>
		public override string ToString()
		{
			return ToString(int.MaxValue);
		}

		public virtual string ToStringOneEntryPerLine()
		{
			return ToStringOneEntryPerLine(int.MaxValue);
		}

		/// <summary>Returns a readable version of at least part of the Index contents.</summary>
		/// <param name="n">Show the first <i>n</i> items in the Index</param>
		/// <returns>A String showing some of the index contents</returns>
		public virtual string ToString(int n)
		{
			StringBuilder buff = new StringBuilder("[");
			int sz = objects.Count;
			if (n > sz)
			{
				n = sz;
			}
			int i;
			for (i = 0; i < n; i++)
			{
				E e = objects[i];
				buff.Append(i).Append('=').Append(e);
				if (i < (sz - 1))
				{
					buff.Append(',');
				}
			}
			if (i < sz)
			{
				buff.Append("...");
			}
			buff.Append(']');
			return buff.ToString();
		}

		public virtual string ToStringOneEntryPerLine(int n)
		{
			StringBuilder buff = new StringBuilder();
			int sz = objects.Count;
			if (n > sz)
			{
				n = sz;
			}
			int i;
			for (i = 0; i < n; i++)
			{
				E e = objects[i];
				buff.Append(e);
				if (i < (sz - 1))
				{
					buff.Append('\n');
				}
			}
			if (i < sz)
			{
				buff.Append("...");
			}
			return buff.ToString();
		}

		/// <summary>Returns an iterator over the elements of the collection.</summary>
		/// <returns>An iterator over the objects indexed</returns>
		public override IEnumerator<E> GetEnumerator()
		{
			return objects.GetEnumerator();
		}

		/// <summary>Returns an unmodifiable view of the Index.</summary>
		/// <remarks>
		/// Returns an unmodifiable view of the Index.  It is just
		/// a locked index that cannot be unlocked, so if you
		/// try to add something, nothing will happen (it won't throw
		/// an exception).  Trying to unlock it will throw an
		/// UnsupportedOperationException.  If the
		/// underlying Index is modified, the change will
		/// "write-through" to the view.
		/// </remarks>
		/// <returns>An unmodifiable view of the Index</returns>
		public virtual Edu.Stanford.Nlp.Util.HashIndex<E> UnmodifiableView()
		{
			Edu.Stanford.Nlp.Util.HashIndex<E> newIndex = new _HashIndex_498(objects, indexes);
			newIndex.Lock();
			return newIndex;
		}

		private sealed class _HashIndex_498 : Edu.Stanford.Nlp.Util.HashIndex<E>
		{
			public _HashIndex_498(IList<E> baseArg1, IDictionary<E, int> baseArg2)
				: base(baseArg1, baseArg2)
			{
				this.serialVersionUID = 3415903369787491736L;
			}

			public override void Unlock()
			{
				throw new NotSupportedException("This is an unmodifiable view!");
			}

		}

		/// <summary>This assumes each line is one value and creates index by adding values in the order of the lines in the file</summary>
		/// <param name="file">Which file to load</param>
		/// <returns>An index built out of the lines in the file</returns>
		public static IIndex<string> LoadFromFileWithList(string file)
		{
			IIndex<string> index = new Edu.Stanford.Nlp.Util.HashIndex<string>();
			BufferedReader br = null;
			try
			{
				br = new BufferedReader(new FileReader(file));
				for (string line; (line = br.ReadLine()) != null; )
				{
					index.Add(line.Trim());
				}
				br.Close();
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			finally
			{
				if (br != null)
				{
					try
					{
						br.Close();
					}
					catch (IOException)
					{
					}
				}
			}
			// forget it
			return index;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			// TODO: why not allow equality to non-HashIndex indices?
			if (!(o is Edu.Stanford.Nlp.Util.HashIndex))
			{
				return false;
			}
			Edu.Stanford.Nlp.Util.HashIndex hashIndex = (Edu.Stanford.Nlp.Util.HashIndex)o;
			return indexes.Equals(hashIndex.indexes) && objects.Equals(hashIndex.objects);
		}

		public override int GetHashCode()
		{
			int result = objects.GetHashCode();
			result = 31 * result + indexes.GetHashCode();
			return result;
		}
	}
}
