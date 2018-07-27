using System.Collections.Generic;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>For interning (canonicalizing) things.</summary>
	/// <remarks>
	/// For interning (canonicalizing) things.
	/// <p/>
	/// It maps any object to a unique interned version which .equals the
	/// presented object.  If presented with a new object which has no
	/// previous interned version, the presented object becomes the
	/// interned version.  You can tell if your object has been chosen as
	/// the new unique representative by checking whether o == intern(o).
	/// The interners use WeakHashMap, meaning that if the only pointers
	/// to an interned item are the interners' backing maps, that item can
	/// still be garbage collected.  Since the gc thread can silently
	/// remove things from the backing map, there's no public way to get
	/// the backing map, but feel free to add one at your own risk.
	/// <p/>
	/// Note that in general it is just as good or better to use the
	/// static Interner.globalIntern() method rather than making an
	/// instance of Interner and using the instance-level intern().
	/// <p/>
	/// Author: Dan Klein
	/// Date: 9/28/03
	/// </remarks>
	/// <author>Dan Klein</author>
	public class Interner<T>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Interner));

		protected internal static Interner<object> interner = Generics.NewInterner();

		/// <summary>For getting the instance that global methods use.</summary>
		public static Interner<object> GetGlobal()
		{
			return interner;
		}

		/// <summary>For supplying a new instance for the global methods to use.</summary>
		/// <returns>the previous global interner.</returns>
		public static Interner<object> SetGlobal(Interner<object> interner)
		{
			Interner<object> oldInterner = Interner.interner;
			Interner.interner = interner;
			return oldInterner;
		}

		/// <summary>Returns a unique object o' that .equals the argument o.</summary>
		/// <remarks>
		/// Returns a unique object o' that .equals the argument o.  If o
		/// itself is returned, this is the first request for an object
		/// .equals to o.
		/// </remarks>
		public static T GlobalIntern<T>(T o)
		{
			return (T)GetGlobal().Intern(o);
		}

		protected internal IDictionary<T, WeakReference<T>> map = Generics.NewWeakHashMap();

		public virtual void Clear()
		{
			map = Generics.NewWeakHashMap();
		}

		/// <summary>Returns a unique object o' that .equals the argument o.</summary>
		/// <remarks>
		/// Returns a unique object o' that .equals the argument o.  If o
		/// itself is returned, this is the first request for an object
		/// .equals to o.
		/// </remarks>
		public virtual T Intern(T o)
		{
			lock (this)
			{
				WeakReference<T> @ref = map[o];
				if (@ref == null)
				{
					@ref = Generics.NewWeakReference(o);
					map[o] = @ref;
				}
				//    else {
				//      log.info("Found dup for " + o);
				//    }
				return @ref.Get();
			}
		}

		/// <summary>
		/// Returns a <code>Set</code> such that each element in the returned set
		/// is a unique object e' that .equals the corresponding element e in the
		/// original set.
		/// </summary>
		public virtual ICollection<T> InternAll(ICollection<T> s)
		{
			ICollection<T> result = Generics.NewHashSet();
			foreach (T o in s)
			{
				result.Add(Intern(o));
			}
			return result;
		}

		public virtual int Size()
		{
			return map.Count;
		}

		/// <summary>Test method: interns its arguments and says whether they == themselves.</summary>
		public static void Main(string[] args)
		{
			foreach (string str in args)
			{
				System.Console.Out.WriteLine(Interner.GlobalIntern(str) == str);
			}
		}
	}
}
