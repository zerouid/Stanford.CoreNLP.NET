using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;








namespace Edu.Stanford.Nlp.Util.Concurrent
{
	/// <summary>A fast threadsafe index that supports constant-time lookup in both directions.</summary>
	/// <remarks>
	/// A fast threadsafe index that supports constant-time lookup in both directions. This
	/// index is tuned for circumstances in which readers significantly outnumber writers.
	/// </remarks>
	/// <author>Spence Green</author>
	/// <?/>
	[System.Serializable]
	public class ConcurrentHashIndex<E> : AbstractCollection<E>, IIndex<E>, IRandomAccess
	{
		private const long serialVersionUID = 6465313844985269109L;

		public const int UnknownId = -1;

		private const int DefaultInitialCapacity = 100;

		private readonly ConcurrentHashMap<E, int> item2Index;

		private int indexSize;

		private readonly ReentrantLock Lock;

		private readonly AtomicReference<object[]> index2Item;

		/// <summary>Constructor.</summary>
		public ConcurrentHashIndex()
			: this(DefaultInitialCapacity)
		{
		}

		/// <summary>Constructor.</summary>
		/// <param name="initialCapacity"/>
		public ConcurrentHashIndex(int initialCapacity)
		{
			item2Index = new ConcurrentHashMap<E, int>(initialCapacity);
			indexSize = 0;
			Lock = new ReentrantLock();
			object[] arr = new object[initialCapacity];
			index2Item = new AtomicReference<object[]>(arr);
		}

		public virtual E Get(int i)
		{
			object[] arr = index2Item.Get();
			if (i < indexSize)
			{
				// arr.length guaranteed to be == to size() given the
				// implementation of indexOf below.
				return (E)arr[i];
			}
			throw new IndexOutOfRangeException(string.Format("Out of bounds: %d >= %d", i, indexSize));
		}

		public virtual int IndexOf(E o)
		{
			int id = item2Index[o];
			return id == null ? UnknownId : id;
		}

		public virtual int AddToIndex(E o)
		{
			int index = item2Index[o];
			if (index != null)
			{
				return index;
			}
			Lock.Lock();
			try
			{
				// Recheck state
				if (item2Index.Contains(o))
				{
					return item2Index[o];
				}
				else
				{
					int newIndex = indexSize++;
					object[] arr = index2Item.Get();
					System.Diagnostics.Debug.Assert(newIndex <= arr.Length);
					if (newIndex == arr.Length)
					{
						// Increase size of array if necessary
						object[] newArr = new object[2 * newIndex];
						System.Array.Copy(arr, 0, newArr, 0, arr.Length);
						arr = newArr;
					}
					arr[newIndex] = o;
					index2Item.Set(arr);
					item2Index[o] = newIndex;
					return newIndex;
				}
			}
			finally
			{
				Lock.Unlock();
			}
		}

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

		public override bool Add(E o)
		{
			return AddToIndex(o) != UnknownId;
		}

		public override bool AddAll<_T0>(ICollection<_T0> c)
		{
			bool changed = false;
			foreach (E element in c)
			{
				changed |= Add(element);
			}
			return changed;
		}

		public virtual IList<E> ObjectsList()
		{
			return Generics.NewArrayList(((ConcurrentHashMap.KeySetView<E, int>)item2Index.Keys));
		}

		public virtual ICollection<E> Objects(int[] indices)
		{
			return new _AbstractList_142(this, indices);
		}

		private sealed class _AbstractList_142 : AbstractList<E>
		{
			public _AbstractList_142(ConcurrentHashIndex<E> _enclosing, int[] indices)
			{
				this._enclosing = _enclosing;
				this.indices = indices;
			}

			public override E Get(int index)
			{
				return this._enclosing._enclosing.Get(indices[index]);
			}

			public override int Count
			{
				get
				{
					return indices.Length;
				}
			}

			private readonly ConcurrentHashIndex<E> _enclosing;

			private readonly int[] indices;
		}

		public virtual bool IsLocked()
		{
			return false;
		}

		public virtual void Lock()
		{
			throw new NotSupportedException();
		}

		public virtual void Unlock()
		{
			throw new NotSupportedException();
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void SaveToWriter(TextWriter @out)
		{
			string nl = Runtime.GetProperty("line.separator");
			for (int i = 0; i < sz; i++)
			{
				E o = Get(i);
				if (o != null)
				{
					@out.Write(i + "=" + Get(i) + nl);
				}
			}
		}

		public virtual void SaveToFilename(string s)
		{
			PrintWriter bw = null;
			try
			{
				bw = IOUtils.GetPrintWriter(s);
				for (int i = 0; i < size; i++)
				{
					E o = Get(i);
					if (o != null)
					{
						bw.Printf("%d=%s%n", i, o.ToString());
					}
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
					bw.Close();
				}
			}
		}

		public override IEnumerator<E> GetEnumerator()
		{
			return new _IEnumerator_203(this);
		}

		private sealed class _IEnumerator_203 : IEnumerator<E>
		{
			public _IEnumerator_203(ConcurrentHashIndex<E> _enclosing)
			{
				this._enclosing = _enclosing;
				this.index = 0;
				this.size = this._enclosing._enclosing.Count;
			}

			private int index;

			private int size;

			public bool MoveNext()
			{
				return this.index < this.size;
			}

			public E Current
			{
				get
				{
					return this._enclosing._enclosing.Get(this.index++);
				}
			}

			public void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly ConcurrentHashIndex<E> _enclosing;
		}

		public override int Count
		{
			get
			{
				return indexSize;
			}
		}

		public override string ToString()
		{
			StringBuilder buff = new StringBuilder("[");
			int i;
			int size = Count;
			for (i = 0; i < size; i++)
			{
				E e = Get(i);
				if (e != null)
				{
					buff.Append(i).Append('=').Append(e);
					if (i < (size - 1))
					{
						buff.Append(',');
					}
				}
			}
			if (i < Count)
			{
				buff.Append("...");
			}
			buff.Append(']');
			return buff.ToString();
		}

		public override bool Contains(object o)
		{
			return IndexOf((E)o) != UnknownId;
		}

		public override void Clear()
		{
			Lock.Lock();
			try
			{
				item2Index.Clear();
				indexSize = 0;
				object[] arr = new object[DefaultInitialCapacity];
				index2Item.Set(arr);
			}
			finally
			{
				Lock.Unlock();
			}
		}
	}
}
