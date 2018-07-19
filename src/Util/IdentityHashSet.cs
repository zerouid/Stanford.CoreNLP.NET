using System;
using System.Collections.Generic;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// This class provides a <code>IdentityHashMap</code>-backed
	/// implementation of the <code>Set</code> interface.
	/// </summary>
	/// <remarks>
	/// This class provides a <code>IdentityHashMap</code>-backed
	/// implementation of the <code>Set</code> interface.  This means that
	/// whether an object is an element of the set depends on whether it is ==
	/// (rather than <code>equals()</code>) to an element of the set.  This is
	/// different from a normal <code>HashSet</code>, where set membership
	/// depends on <code>equals()</code>, rather than ==.
	/// Each element in the set is a key in the backing IdentityHashMap; each key
	/// maps to a static token, denoting that the key does, in fact, exist.
	/// Most operations are O(1), assuming no hash collisions.  In the worst
	/// case (where all hashes collide), operations are O(n).
	/// </remarks>
	/// <author>Bill MacCartney</author>
	[System.Serializable]
	public class IdentityHashSet<E> : AbstractSet<E>, ICloneable
	{
		[System.NonSerialized]
		private IdentityHashMap<E, bool> map;

		private const long serialVersionUID = -5024744406713321676L;

		/// <summary>
		/// Construct a new, empty IdentityHashSet whose backing IdentityHashMap
		/// has the default expected maximum size (21);
		/// </summary>
		public IdentityHashSet()
		{
			// todo: The Java bug database notes that "From 1.6, an identity hash set can be created by Collections.newSetFromMap(new IdentityHashMap())."
			// INSTANCE VARIABLES -------------------------------------------------
			// the IdentityHashMap which backs this set
			// CONSTRUCTORS ---------------------------------------------------------
			map = new IdentityHashMap<E, bool>();
		}

		/// <summary>
		/// Construct a new, empty IdentityHashSet whose backing IdentityHashMap
		/// has the specified expected maximum size.
		/// </summary>
		/// <remarks>
		/// Construct a new, empty IdentityHashSet whose backing IdentityHashMap
		/// has the specified expected maximum size.  Putting more than the
		/// expected number of elements into the set may cause the internal data
		/// structure to grow, which may be somewhat time-consuming.
		/// </remarks>
		/// <param name="expectedMaxSize">the expected maximum size of the set.</param>
		public IdentityHashSet(int expectedMaxSize)
		{
			map = new IdentityHashMap<E, bool>(expectedMaxSize);
		}

		/// <summary>
		/// Construct a new IdentityHashSet with the same elements as the supplied
		/// Collection (eliminating any duplicates, of course); the backing
		/// IdentityHashMap will have the default expected maximum size (21).
		/// </summary>
		/// <param name="c">
		/// a Collection containing the elements with which this set will
		/// be initialized.
		/// </param>
		public IdentityHashSet(ICollection<E> c)
		{
			map = new IdentityHashMap<E, bool>();
			Sharpen.Collections.AddAll(this, c);
		}

		// PUBLIC METHODS ---------------------------------------------------------
		/// <summary>Adds the specified element to this set if it is not already present.</summary>
		/// <remarks>
		/// Adds the specified element to this set if it is not already present.
		/// Remember that this set implementation uses == (not
		/// <code>equals()</code>) to test whether an element is present in the
		/// set.
		/// </remarks>
		/// <param name="o">element to add to this set</param>
		/// <returns>
		/// true          if the element was added,
		/// false         otherwise
		/// </returns>
		public override bool Add(E o)
		{
			if (map.Contains(o))
			{
				return false;
			}
			else
			{
				InternalAdd(o);
				return true;
			}
		}

		/// <summary>Removes all of the elements from this set.</summary>
		public override void Clear()
		{
			map.Clear();
		}

		/// <summary>
		/// Returns a shallow copy of this <code>IdentityHashSet</code> instance:
		/// the elements themselves are not cloned.
		/// </summary>
		/// <returns>a shallow copy of this set.</returns>
		public virtual object Clone()
		{
			IEnumerator<E> it = GetEnumerator();
			Edu.Stanford.Nlp.Util.IdentityHashSet<E> clone = new Edu.Stanford.Nlp.Util.IdentityHashSet<E>(Count * 2);
			while (it.MoveNext())
			{
				clone.InternalAdd(it.Current);
			}
			return clone;
		}

		/// <summary>Returns true if this set contains the specified element.</summary>
		/// <remarks>
		/// Returns true if this set contains the specified element.
		/// Remember that this set implementation uses == (not
		/// <code>equals()</code>) to test whether an element is present in the
		/// set.
		/// </remarks>
		/// <param name="o">
		/// Element whose presence in this set is to be
		/// tested.
		/// </param>
		/// <returns><code>true</code> if this set contains the specified element.</returns>
		public override bool Contains(object o)
		{
			return map.Contains(o);
		}

		/// <summary>Returns <code>true</code> if this set contains no elements.</summary>
		/// <returns><code>true</code> if this set contains no elements.</returns>
		public override bool IsEmpty()
		{
			return map.IsEmpty();
		}

		/// <summary>Returns an iterator over the elements in this set.</summary>
		/// <remarks>
		/// Returns an iterator over the elements in this set. The elements are
		/// returned in no particular order.
		/// </remarks>
		/// <returns>an <code>Iterator</code> over the elements in this set.</returns>
		public override IEnumerator<E> GetEnumerator()
		{
			return map.Keys.GetEnumerator();
		}

		/// <summary>Removes the specified element from this set if it is present.</summary>
		/// <remarks>
		/// Removes the specified element from this set if it is present.
		/// Remember that this set implementation uses == (not
		/// <code>equals()</code>) to test whether an element is present in the
		/// set.
		/// </remarks>
		/// <param name="o">Object to be removed from this set, if present.</param>
		/// <returns><code>true</code> if the set contained the specified element.</returns>
		public override bool Remove(object o)
		{
			return (Sharpen.Collections.Remove(map, o) != null);
		}

		/// <summary>Returns the number of elements in this set (its cardinality).</summary>
		/// <returns>the number of elements in this set (its cardinality).</returns>
		public override int Count
		{
			get
			{
				return map.Count;
			}
		}

		/// <summary>Just for testing.</summary>
		public static void Main(string[] args)
		{
			int x = int.Parse(3);
			int y = int.Parse(4);
			int z = int.Parse(5);
			IList<int> a = Arrays.AsList(new int[] { x, y, z });
			IList<string> b = Arrays.AsList(new string[] { "Larry", "Moe", "Curly" });
			IList<int> c = Arrays.AsList(new int[] { x, y, z });
			IList<string> d = Arrays.AsList(new string[] { "Larry", "Moe", "Curly" });
			ICollection<IList<object>> hs = Generics.NewHashSet();
			Edu.Stanford.Nlp.Util.IdentityHashSet<IList<object>> ihs = new Edu.Stanford.Nlp.Util.IdentityHashSet<IList<object>>();
			hs.Add(a);
			hs.Add(b);
			ihs.Add(a);
			ihs.Add(b);
			System.Console.Out.WriteLine("List a is " + a);
			System.Console.Out.WriteLine("List b is " + b);
			System.Console.Out.WriteLine("List c is " + c);
			System.Console.Out.WriteLine("List d is " + d);
			System.Console.Out.WriteLine("HashSet hs contains a and b: " + hs);
			System.Console.Out.WriteLine("IdentityHashSet ihs contains a and b: " + ihs);
			System.Console.Out.WriteLine("hs contains a? " + hs.Contains(a));
			System.Console.Out.WriteLine("hs contains b? " + hs.Contains(b));
			System.Console.Out.WriteLine("hs contains c? " + hs.Contains(c));
			System.Console.Out.WriteLine("hs contains d? " + hs.Contains(d));
			System.Console.Out.WriteLine("ihs contains a? " + ihs.Contains(a));
			System.Console.Out.WriteLine("ihs contains b? " + ihs.Contains(b));
			System.Console.Out.WriteLine("ihs contains c? " + ihs.Contains(c));
			System.Console.Out.WriteLine("ihs contains d? " + ihs.Contains(d));
		}

		// PRIVATE METHODS -----------------------------------------------------------
		/// <summary>Adds the supplied element to this set.</summary>
		/// <remarks>
		/// Adds the supplied element to this set.  This private method is used
		/// internally [by clone()] instead of add(), because add() can be
		/// overridden to do unexpected things.
		/// </remarks>
		/// <param name="o">the element to add to this set</param>
		private void InternalAdd(E o)
		{
			map[o] = true;
		}

		/// <summary>
		/// Serialize this Object in a manner which is binary-compatible with the
		/// JDK.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		private void WriteObject(ObjectOutputStream s)
		{
			IEnumerator<E> it = GetEnumerator();
			s.WriteInt(Count * 2);
			// expectedMaxSize
			s.WriteInt(Count);
			while (it.MoveNext())
			{
				s.WriteObject(it.Current);
			}
		}

		/// <summary>
		/// Deserialize this Object in a manner which is binary-compatible with
		/// the JDK.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream s)
		{
			int size;
			int expectedMaxSize;
			object o;
			expectedMaxSize = s.ReadInt();
			size = s.ReadInt();
			map = new IdentityHashMap<E, bool>(expectedMaxSize);
			for (int i = 0; i < size; i++)
			{
				o = s.ReadObject();
				InternalAdd(ErasureUtils.UncheckedCast<E>(o));
			}
		}
	}
}
