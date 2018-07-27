using System;
using System.Collections.Generic;
using System.IO;



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A class that has a backing index, such as a hash index you don't
	/// want changed, and another index which will hold extra entries that
	/// get added during the life of the index.
	/// </summary>
	/// <remarks>
	/// A class that has a backing index, such as a hash index you don't
	/// want changed, and another index which will hold extra entries that
	/// get added during the life of the index.
	/// <br />
	/// It is important that nothing else changes the backing index while
	/// a DeltaIndex is in use.  The behavior of this index is
	/// undefined if the backing index changes, although in general the new
	/// entries in the backing index will be ignored.
	/// </remarks>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class DeltaIndex<E> : AbstractCollection<E>, IIndex<E>
	{
		private const long serialVersionUID = -1459230891686013411L;

		private readonly IIndex<E> backingIndex;

		private readonly IIndex<E> spilloverIndex;

		private readonly int backingIndexSize;

		private bool locked;

		public DeltaIndex(IIndex<E> backingIndex)
			: this(backingIndex, new HashIndex<E>())
		{
		}

		public DeltaIndex(IIndex<E> backingIndex, IIndex<E> spilloverIndex)
		{
			this.backingIndex = backingIndex;
			this.spilloverIndex = spilloverIndex;
			backingIndexSize = backingIndex.Size();
		}

		public override int Count
		{
			get
			{
				return backingIndex.Size() + spilloverIndex.Size();
			}
		}

		public virtual E Get(int i)
		{
			if (i < backingIndexSize)
			{
				return backingIndex.Get(i);
			}
			else
			{
				return spilloverIndex.Get(i - backingIndexSize);
			}
		}

		public virtual int IndexOf(E o)
		{
			int index = backingIndex.IndexOf(o);
			if (index >= 0)
			{
				return index;
			}
			index = spilloverIndex.IndexOf(o);
			if (index >= 0)
			{
				return index + backingIndexSize;
			}
			return index;
		}

		// i.e., return -1
		public virtual int AddToIndex(E o)
		{
			int index = backingIndex.IndexOf(o);
			if (index >= 0)
			{
				return index;
			}
			if (locked)
			{
				index = spilloverIndex.IndexOf(o);
			}
			else
			{
				index = spilloverIndex.AddToIndex(o);
			}
			if (index >= 0)
			{
				return index + backingIndexSize;
			}
			return index;
		}

		// i.e., return -1
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

		public virtual IList<E> ObjectsList()
		{
			IList<E> result = new List<E>();
			if (result.Count > backingIndexSize)
			{
				// we told you not to do this
				Sharpen.Collections.AddAll(result, backingIndex.ObjectsList().SubList(0, backingIndexSize));
			}
			else
			{
				Sharpen.Collections.AddAll(result, backingIndex.ObjectsList());
			}
			Sharpen.Collections.AddAll(result, spilloverIndex.ObjectsList());
			return Java.Util.Collections.UnmodifiableList(result);
		}

		public virtual ICollection<E> Objects(int[] indices)
		{
			IList<E> result = new List<E>();
			foreach (int index in indices)
			{
				result.Add(Get(index));
			}
			return result;
		}

		public virtual bool IsLocked()
		{
			return locked;
		}

		public virtual void Lock()
		{
			locked = true;
		}

		public virtual void Unlock()
		{
			locked = false;
		}

		public virtual void SaveToWriter(TextWriter @out)
		{
			throw new NotSupportedException();
		}

		public virtual void SaveToFilename(string s)
		{
			throw new NotSupportedException();
		}

		public override bool Contains(object o)
		{
			return backingIndex.Contains(o) || spilloverIndex.Contains(o);
		}

		public override bool Add(E e)
		{
			if (backingIndex.Contains(e))
			{
				return false;
			}
			return spilloverIndex.Add(e);
		}

		public override bool AddAll<_T0>(ICollection<_T0> c)
		{
			bool changed = false;
			foreach (E e in c)
			{
				if (Add(e))
				{
					changed = true;
				}
			}
			return changed;
		}

		/// <summary>
		/// We don't want to change the backing index in any way, and "clear"
		/// would have to entail doing that, so we just throw an
		/// UnsupportedOperationException instead
		/// </summary>
		public override void Clear()
		{
			throw new NotSupportedException();
		}

		public override bool IsEmpty()
		{
			return backingIndexSize == 0 && spilloverIndex.Size() == 0;
		}

		/// <summary>
		/// This is one instance where elements added to the backing index
		/// will show up in this index's operations
		/// </summary>
		public override IEnumerator<E> GetEnumerator()
		{
			return new _IEnumerator_196(this);
		}

		private sealed class _IEnumerator_196 : IEnumerator<E>
		{
			public _IEnumerator_196()
			{
				this.backingIterator = this._enclosing.backingIndex.GetEnumerator();
				this.spilloverIterator = this._enclosing.spilloverIndex.GetEnumerator();
			}

			internal IEnumerator<E> backingIterator;

			internal IEnumerator<E> spilloverIterator;

			public bool MoveNext()
			{
				return this.backingIterator.MoveNext() || this.spilloverIterator.MoveNext();
			}

			public E Current
			{
				get
				{
					if (this.backingIterator.MoveNext())
					{
						return this.backingIterator.Current;
					}
					else
					{
						return this.spilloverIterator.Current;
					}
				}
			}

			public void Remove()
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>super ghetto</summary>
		public override string ToString()
		{
			return backingIndex.ToString() + "," + spilloverIndex.ToString();
		}
	}
}
