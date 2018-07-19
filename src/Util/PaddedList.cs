using System;
using System.Collections.Generic;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A PaddedList wraps another list, presenting an apparently infinite
	/// list by padding outside the real confines of the list with a default
	/// value.
	/// </summary>
	/// <remarks>
	/// A PaddedList wraps another list, presenting an apparently infinite
	/// list by padding outside the real confines of the list with a default
	/// value.  Note that <code>size()</code> returns the true size, but
	/// <code>get()</code> works for any number.
	/// </remarks>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class PaddedList<E> : AbstractList<E>
	{
		private readonly IList<E> l;

		private readonly E padding;

		public virtual E GetPad()
		{
			return padding;
		}

		public override int Count
		{
			get
			{
				return l.Count;
			}
		}

		public override E Get(int i)
		{
			if (i < 0 || i >= Count)
			{
				return padding;
			}
			return l[i];
		}

		public override string ToString()
		{
			return l.ToString();
		}

		/// <summary>
		/// With this constructor, get() will return <code>null</code> for
		/// elements outside the real list.
		/// </summary>
		public PaddedList(IList<E> l)
			: this(l, null)
		{
		}

		public PaddedList(IList<E> l, E padding)
		{
			this.l = l;
			this.padding = padding;
		}

		/// <summary>This returns the inner list that was wrapped.</summary>
		/// <remarks>
		/// This returns the inner list that was wrapped.
		/// Use of this method should be avoided.  There's currently only
		/// one use.
		/// </remarks>
		/// <returns>The inner list of the PaddedList.</returns>
		[Obsolete]
		public virtual IList<E> GetWrappedList()
		{
			return l;
		}

		/// <summary>
		/// A static method that provides an easy way to create a list of a
		/// certain parametric type.
		/// </summary>
		/// <remarks>
		/// A static method that provides an easy way to create a list of a
		/// certain parametric type.
		/// This static constructor works better with generics.
		/// </remarks>
		/// <param name="list">The list to pad</param>
		/// <param name="padding">The padding element (may be null)</param>
		/// <returns>The padded list</returns>
		public static Edu.Stanford.Nlp.Util.PaddedList<IN> ValueOf<In>(IList<IN> list, IN padding)
		{
			return new Edu.Stanford.Nlp.Util.PaddedList<IN>(list, padding);
		}

		/// <summary>
		/// Returns true if this PaddedList and another are wrapping the
		/// same list.
		/// </summary>
		/// <remarks>
		/// Returns true if this PaddedList and another are wrapping the
		/// same list.  This is tested as ==. Kinda yucky, but sometimes you
		/// want to know.
		/// </remarks>
		public virtual bool SameInnerList(Edu.Stanford.Nlp.Util.PaddedList<E> p)
		{
			return p != null && l == p.l;
		}

		private const long serialVersionUID = 2064775966439971729L;
	}
}
