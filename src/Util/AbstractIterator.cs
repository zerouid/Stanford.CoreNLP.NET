using System;
using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Iterator with <code>remove()</code> defined to throw an
	/// <code>UnsupportedOperationException</code>.
	/// </summary>
	public abstract class AbstractIterator<E> : IEnumerator<E>
	{
		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public abstract bool MoveNext();

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public abstract E Current
		{
			get;
		}

		/// <summary>Throws an <code>UnsupportedOperationException</code>.</summary>
		public virtual void Remove()
		{
			throw new NotSupportedException();
		}
	}
}
