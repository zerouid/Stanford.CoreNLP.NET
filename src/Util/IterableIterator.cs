using System.Collections.Generic;
using Java.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// This cures a pet peeve of mine: that you can't use an Iterator directly in
	/// Java 5's foreach construct.
	/// </summary>
	/// <remarks>
	/// This cures a pet peeve of mine: that you can't use an Iterator directly in
	/// Java 5's foreach construct.  Well, this one you can, dammit.
	/// </remarks>
	/// <author>Bill MacCartney</author>
	public class IterableIterator<E> : IEnumerator<E>, IEnumerable<E>
	{
		private IEnumerator<E> it;

		private IEnumerable<E> iterable;

		private IStream<E> stream;

		public IterableIterator(IEnumerator<E> it)
		{
			this.it = it;
		}

		public IterableIterator(IEnumerable<E> iterable)
		{
			this.iterable = iterable;
			this.it = iterable.GetEnumerator();
		}

		public IterableIterator(IStream<E> stream)
		{
			this.stream = stream;
			this.it = stream.Iterator();
		}

		public virtual bool MoveNext()
		{
			return it.MoveNext();
		}

		public virtual E Current
		{
			get
			{
				return it.Current;
			}
		}

		public virtual void Remove()
		{
			it.Remove();
		}

		public virtual IEnumerator<E> GetEnumerator()
		{
			if (iterable != null)
			{
				return iterable.GetEnumerator();
			}
			else
			{
				if (stream != null)
				{
					return stream.Iterator();
				}
				else
				{
					return this;
				}
			}
		}

		public virtual ISpliterator<E> Spliterator()
		{
			if (iterable != null)
			{
				return iterable.Spliterator();
			}
			else
			{
				if (stream != null)
				{
					return stream.Spliterator();
				}
				else
				{
					return Spliterators.SpliteratorUnknownSize(it, SpliteratorConstants.Ordered | SpliteratorConstants.Concurrent);
				}
			}
		}
	}
}
