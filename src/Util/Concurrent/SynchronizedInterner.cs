using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Concurrent
{
	/// <summary>
	/// <p>
	/// For interning (canonicalizing) things in a multi-threaded environment.
	/// </summary>
	/// <remarks>
	/// <p>
	/// For interning (canonicalizing) things in a multi-threaded environment.
	/// </p>
	/// <p>
	/// Maps any object to a unique interned version which .equals the
	/// presented object.  If presented with a new object which has no
	/// previous interned version, the presented object becomes the
	/// interned version.  You can tell if your object has been chosen as
	/// the new unique representative by checking whether o == intern(o).
	/// The interners use a concurrent map with weak references, meaning that
	/// if the only pointers to an interned item are the interners' backing maps,
	/// that item can still be garbage collected.  Since the gc thread can
	/// silently remove things from the backing map, there's no public way to
	/// get the backing map, but feel free to add one at your own risk.
	/// </p>
	/// Note that in general it is just as good or better to use the
	/// static SynchronizedInterner.globalIntern() method rather than making an
	/// instance of SynchronizedInterner and using the instance-level intern().
	/// <p/>
	/// </remarks>
	/// <author>Ilya Sherman</author>
	/// <seealso cref="Edu.Stanford.Nlp.Util.Interner{T}"/>
	public class SynchronizedInterner<T>
	{
		protected internal static readonly object globalMutex = new object();

		protected internal static Edu.Stanford.Nlp.Util.Concurrent.SynchronizedInterner<object> interner = Generics.NewSynchronizedInterner(Interner.GetGlobal(), globalMutex);

		// TODO would be nice to have this share an interface with Interner
		/// <summary>For getting the instance that global methods use.</summary>
		public static Edu.Stanford.Nlp.Util.Concurrent.SynchronizedInterner<object> GetGlobal()
		{
			lock (globalMutex)
			{
				return interner;
			}
		}

		/// <summary>For supplying a new instance for the global methods to use.</summary>
		/// <returns>the previous global interner.</returns>
		public static Edu.Stanford.Nlp.Util.Concurrent.SynchronizedInterner<object> SetGlobal(Interner<object> delegate_)
		{
			lock (globalMutex)
			{
				Edu.Stanford.Nlp.Util.Concurrent.SynchronizedInterner<object> oldInterner = Edu.Stanford.Nlp.Util.Concurrent.SynchronizedInterner.interner;
				Edu.Stanford.Nlp.Util.Concurrent.SynchronizedInterner.interner = Generics.NewSynchronizedInterner(delegate_);
				return oldInterner;
			}
		}

		/// <summary>Returns a unique object o' that .equals the argument o.</summary>
		/// <remarks>
		/// Returns a unique object o' that .equals the argument o.  If o
		/// itself is returned, this is the first request for an object
		/// .equals to o.
		/// </remarks>
		public static T GlobalIntern<T>(T o)
		{
			lock (globalMutex)
			{
				return (T)GetGlobal().Intern(o);
			}
		}

		protected internal readonly Interner<T> delegate_;

		protected internal readonly object mutex;

		public SynchronizedInterner(Interner<T> delegate_)
		{
			if (delegate_ == null)
			{
				throw new ArgumentNullException();
			}
			this.delegate_ = delegate_;
			this.mutex = this;
		}

		public SynchronizedInterner(Interner<T> delegate_, object mutex)
		{
			if (delegate_ == null)
			{
				throw new ArgumentNullException();
			}
			this.delegate_ = delegate_;
			this.mutex = mutex;
		}

		public virtual void Clear()
		{
			lock (mutex)
			{
				delegate_.Clear();
			}
		}

		/// <summary>Returns a unique object o' that .equals the argument o.</summary>
		/// <remarks>
		/// Returns a unique object o' that .equals the argument o.  If o
		/// itself is returned, this is the first request for an object
		/// .equals to o.
		/// </remarks>
		public virtual T Intern(T o)
		{
			lock (mutex)
			{
				return delegate_.Intern(o);
			}
		}

		/// <summary>
		/// Returns a <code>Set</code> such that each element in the returned set
		/// is a unique object e' that .equals the corresponding element e in the
		/// original set.
		/// </summary>
		public virtual ICollection<T> InternAll(ICollection<T> s)
		{
			lock (mutex)
			{
				return delegate_.InternAll(s);
			}
		}

		public virtual int Size()
		{
			lock (mutex)
			{
				return delegate_.Size();
			}
		}

		/// <summary>Test method: interns its arguments and says whether they == themselves.</summary>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Thread[] threads = new Thread[100];
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new Thread(null);
			}
			foreach (Thread thread in threads)
			{
				thread.Start();
			}
			foreach (Thread thread_1 in threads)
			{
				thread_1.Join();
			}
		}
	}
}
