using System;
using System.Collections.Generic;







namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A hash set supporting full concurrency of retrievals and
	/// high expected concurrency for updates but with an (adjustable) maximum size.
	/// </summary>
	/// <remarks>
	/// A hash set supporting full concurrency of retrievals and
	/// high expected concurrency for updates but with an (adjustable) maximum size.
	/// The maximum only prevents further add operations. It doesn't stop the maximum
	/// being exceeded when first loaded or via an addAll(). This is deliberate!
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <?/>
	[System.Serializable]
	public class MaxSizeConcurrentHashSet<E> : ICollection<E>
	{
		private readonly IConcurrentMap<E, bool> m;

		[System.NonSerialized]
		private ICollection<E> s;

		private int maxSize;

		/// <summary>Create a ConcurrentHashSet with no maximum size.</summary>
		public MaxSizeConcurrentHashSet()
			: this(-1)
		{
		}

		/// <summary>Create a ConcurrentHashSet with the maximum size given.</summary>
		public MaxSizeConcurrentHashSet(int maxSize)
		{
			// the keySet of the Map
			this.m = new ConcurrentHashMap<E, bool>();
			this.maxSize = maxSize;
			Init();
		}

		/// <summary>Create a ConcurrentHashSet with the elements in s.</summary>
		/// <remarks>
		/// Create a ConcurrentHashSet with the elements in s.
		/// This set has no maximum size.
		/// </remarks>
		public MaxSizeConcurrentHashSet(ICollection<E> s)
		{
			this.m = new ConcurrentHashMap<E, bool>(Math.Max(s.Count, 16));
			Init();
			Sharpen.Collections.AddAll(this, s);
			this.maxSize = -1;
		}

		private void Init()
		{
			this.s = m.Keys;
		}

		public virtual int GetMaxSize()
		{
			return maxSize;
		}

		public virtual void SetMaxSize(int maxSize)
		{
			this.maxSize = maxSize;
		}

		/// <summary>Adds the element if the set is not already full.</summary>
		/// <remarks>
		/// Adds the element if the set is not already full. Otherwise, silently
		/// doesn't add it.
		/// </remarks>
		/// <param name="e">The element</param>
		/// <returns>
		/// true iff the element was added. This is slightly different from the semantics
		/// of a normal Set which returns true if the item didn't used to be there and was added.
		/// Here it only returns true if it was added.
		/// </returns>
		public virtual bool Add(E e)
		{
			lock (this)
			{
				if (maxSize >= 0 && Count >= maxSize)
				{
					// can't put new value
					return false;
				}
				else
				{
					return m[e] = true == null;
				}
			}
		}

		public virtual void Clear()
		{
			m.Clear();
		}

		public virtual int Count
		{
			get
			{
				return m.Count;
			}
		}

		public virtual bool IsEmpty()
		{
			return m.IsEmpty();
		}

		public virtual bool Contains(object o)
		{
			return m.Contains(o);
		}

		public virtual bool Remove(object o)
		{
			return Sharpen.Collections.Remove(m, o) != null;
		}

		public virtual IEnumerator<E> GetEnumerator()
		{
			return s.GetEnumerator();
		}

		public virtual object[] ToArray()
		{
			return Sharpen.Collections.ToArray(s);
		}

		public virtual T[] ToArray<T>(T[] a)
		{
			return Sharpen.Collections.ToArray(s, a);
		}

		public override string ToString()
		{
			return s.ToString();
		}

		public override int GetHashCode()
		{
			return s.GetHashCode();
		}

		public override bool Equals(object o)
		{
			return s.Equals(o);
		}

		public virtual bool ContainsAll<_T0>(ICollection<_T0> c)
		{
			return s.ContainsAll(c);
		}

		public virtual bool RemoveAll<_T0>(ICollection<_T0> c)
		{
			return s.RemoveAll(c);
		}

		public virtual bool RetainAll<_T0>(ICollection<_T0> c)
		{
			return s.RetainAll(c);
		}

		/// <summary>Add all the items.</summary>
		/// <remarks>
		/// Add all the items.
		/// This doesn't use the add method, because we want to bypass the limit here.
		/// </remarks>
		public virtual bool AddAll<_T0>(ICollection<_T0> c)
			where _T0 : E
		{
			bool added = false;
			foreach (E item in c)
			{
				if (m[item] = true == null)
				{
					added = true;
				}
			}
			return added;
		}

		// Override default methods in Collection
		public virtual void ForEach<_T0>(IConsumer<_T0> action)
		{
			s.ForEach(action);
		}

		public virtual bool RemoveIf<_T0>(IPredicate<_T0> filter)
		{
			return s.RemoveIf(filter);
		}

		public virtual ISpliterator<E> Spliterator()
		{
			return s.Spliterator();
		}

		public virtual IEnumerable<E> Stream()
		{
			return s.Stream();
		}

		public virtual IEnumerable<E> ParallelStream()
		{
			return s.ParallelStream();
		}

		private const long serialVersionUID = 1L;

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream stream)
		{
			stream.DefaultReadObject();
			Init();
		}
	}
}
