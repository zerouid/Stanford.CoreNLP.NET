using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Pair is a Class for holding mutable pairs of objects.</summary>
	/// <remarks>
	/// Pair is a Class for holding mutable pairs of objects.
	/// <p>
	/// <i>Implementation note:</i>
	/// On a 32-bit JVM uses ~ 8 (this) + 4 (first) + 4 (second) = 16 bytes.
	/// On a 64-bit JVM uses ~ 16 (this) + 8 (first) + 8 (second) = 32 bytes.
	/// <p>
	/// Many applications use a lot of Pairs so it's good to keep this
	/// number small.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning (added stuff from Kristina's, rounded out)</author>
	/// <version>2002/08/25</version>
	[System.Serializable]
	public class Pair<T1, T2> : IComparable<Edu.Stanford.Nlp.Util.Pair<T1, T2>>, IPrettyLoggable
	{
		/// <summary>Direct access is deprecated.</summary>
		/// <remarks>Direct access is deprecated.  Use first().</remarks>
		/// <serial/>
		public T1 first;

		/// <summary>Direct access is deprecated.</summary>
		/// <remarks>Direct access is deprecated.  Use second().</remarks>
		/// <serial/>
		public T2 second;

		public Pair()
		{
		}

		public Pair(T1 first, T2 second)
		{
			// first = null; second = null; -- default initialization
			this.first = first;
			this.second = second;
		}

		public virtual T1 First()
		{
			return first;
		}

		public virtual T2 Second()
		{
			return second;
		}

		public virtual void SetFirst(T1 o)
		{
			first = o;
		}

		public virtual void SetSecond(T2 o)
		{
			second = o;
		}

		public override string ToString()
		{
			return "(" + first + "," + second + ")";
		}

		public override bool Equals(object o)
		{
			if (o is Edu.Stanford.Nlp.Util.Pair)
			{
				Edu.Stanford.Nlp.Util.Pair p = (Edu.Stanford.Nlp.Util.Pair)o;
				return (first == null ? p.First() == null : first.Equals(p.First())) && (second == null ? p.Second() == null : second.Equals(p.Second()));
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			int firstHash = (first == null ? 0 : first.GetHashCode());
			int secondHash = (second == null ? 0 : second.GetHashCode());
			return firstHash * 31 + secondHash;
		}

		public virtual IList<object> AsList()
		{
			return CollectionUtils.MakeList(first, second);
		}

		/// <summary>Returns a Pair constructed from X and Y.</summary>
		/// <remarks>
		/// Returns a Pair constructed from X and Y.  Convenience method; the
		/// compiler will disambiguate the classes used for you so that you
		/// don't have to write out potentially long class names.
		/// </remarks>
		public static Edu.Stanford.Nlp.Util.Pair<X, Y> MakePair<X, Y>(X x, Y y)
		{
			return new Edu.Stanford.Nlp.Util.Pair<X, Y>(x, y);
		}

		/// <summary>Write a string representation of a Pair to a DataStream.</summary>
		/// <remarks>
		/// Write a string representation of a Pair to a DataStream.
		/// The <code>toString()</code> method is called on each of the pair
		/// of objects and a <code>String</code> representation is written.
		/// This might not allow one to recover the pair of objects unless they
		/// are of type <code>String</code>.
		/// </remarks>
		public virtual void Save(DataOutputStream @out)
		{
			try
			{
				@out.WriteUTF(first.ToString());
				@out.WriteUTF(second.ToString());
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <summary>Compares this <code>Pair</code> to another object.</summary>
		/// <remarks>
		/// Compares this <code>Pair</code> to another object.
		/// If the object is a <code>Pair</code>, this function will work providing
		/// the elements of the <code>Pair</code> are themselves comparable.
		/// It will then return a value based on the pair of objects, where
		/// <code>p &gt; q iff p.first() &gt; q.first() ||
		/// (p.first().equals(q.first()) && p.second() &gt; q.second())</code>.
		/// If the other object is not a <code>Pair</code>, it throws a
		/// <code>ClassCastException</code>.
		/// </remarks>
		/// <param name="another">the <code>Object</code> to be compared.</param>
		/// <returns>
		/// the value <code>0</code> if the argument is a
		/// <code>Pair</code> equal to this <code>Pair</code>; a value less than
		/// <code>0</code> if the argument is a <code>Pair</code>
		/// greater than this <code>Pair</code>; and a value
		/// greater than <code>0</code> if the argument is a
		/// <code>Pair</code> less than this <code>Pair</code>.
		/// </returns>
		/// <exception cref="System.InvalidCastException">
		/// if the argument is not a
		/// <code>Pair</code>.
		/// </exception>
		/// <seealso cref="Java.Lang.IComparable{T}"/>
		public virtual int CompareTo(Edu.Stanford.Nlp.Util.Pair<T1, T2> another)
		{
			if (First() is IComparable)
			{
				int comp = ((IComparable<T1>)First()).CompareTo(another.First());
				if (comp != 0)
				{
					return comp;
				}
			}
			if (Second() is IComparable)
			{
				return ((IComparable<T2>)Second()).CompareTo(another.Second());
			}
			if ((!(First() is IComparable)) && (!(Second() is IComparable)))
			{
				throw new AssertionError("Neither element of pair comparable");
			}
			return 0;
		}

		/// <summary>
		/// If first and second are Strings, then this returns an MutableInternedPair
		/// where the Strings have been interned, and if this Pair is serialized
		/// and then deserialized, first and second are interned upon
		/// deserialization.
		/// </summary>
		/// <param name="p">A pair of Strings</param>
		/// <returns>MutableInternedPair, with same first and second as this.</returns>
		public static Edu.Stanford.Nlp.Util.Pair<string, string> StringIntern(Edu.Stanford.Nlp.Util.Pair<string, string> p)
		{
			return new Pair.MutableInternedPair(p);
		}

		/// <summary>Returns an MutableInternedPair where the Strings have been interned.</summary>
		/// <remarks>
		/// Returns an MutableInternedPair where the Strings have been interned.
		/// This is a factory method for creating an
		/// MutableInternedPair.  It requires the arguments to be Strings.
		/// If this Pair is serialized
		/// and then deserialized, first and second are interned upon
		/// deserialization.
		/// <p><i>Note:</i> I put this in thinking that its use might be
		/// faster than calling <code>x = new Pair(a, b).stringIntern()</code>
		/// but it's not really clear whether this is true.
		/// </remarks>
		/// <param name="first">The first object</param>
		/// <param name="second">The second object</param>
		/// <returns>An MutableInternedPair, with given first and second</returns>
		public static Edu.Stanford.Nlp.Util.Pair<string, string> InternedStringPair(string first, string second)
		{
			return new Pair.MutableInternedPair(first, second);
		}

		/// <summary>use serialVersionUID for cross version serialization compatibility</summary>
		private const long serialVersionUID = 1360822168806852921L;

		[System.Serializable]
		internal class MutableInternedPair : Pair<string, string>
		{
			private MutableInternedPair(Pair<string, string> p)
				: base(p.first, p.second)
			{
				InternStrings();
			}

			private MutableInternedPair(string first, string second)
				: base(first, second)
			{
				InternStrings();
			}

			protected internal virtual object ReadResolve()
			{
				InternStrings();
				return this;
			}

			private void InternStrings()
			{
				if (first != null)
				{
					first = string.Intern(first);
				}
				if (second != null)
				{
					second = string.Intern(second);
				}
			}

			private const long serialVersionUID = 1360822168806852922L;
			// use serialVersionUID for cross version serialization compatibility
		}

		/// <summary><inheritDoc/></summary>
		public virtual void PrettyLog(Redwood.RedwoodChannels channels, string description)
		{
			PrettyLogger.Log(channels, description, this.AsList());
		}

		/// <summary>
		/// Compares a <code>Pair</code> to another <code>Pair</code> according to the first object of the pair only
		/// This function will work providing
		/// the first element of the <code>Pair</code> is comparable, otherwise will throw a
		/// <code>ClassCastException</code>
		/// </summary>
		/// <author>jonathanberant</author>
		/// <?/>
		/// <?/>
		public class ByFirstPairComparator<T1, T2> : IComparator<Pair<T1, T2>>
		{
			public virtual int Compare(Pair<T1, T2> pair1, Pair<T1, T2> pair2)
			{
				return ((IComparable<T1>)pair1.First()).CompareTo(pair2.First());
			}
		}

		/// <summary>
		/// Compares a <code>Pair</code> to another <code>Pair</code> according to the first object of the pair only in decreasing order
		/// This function will work providing
		/// the first element of the <code>Pair</code> is comparable, otherwise will throw a
		/// <code>ClassCastException</code>
		/// </summary>
		/// <author>jonathanberant</author>
		/// <?/>
		/// <?/>
		public class ByFirstReversePairComparator<T1, T2> : IComparator<Pair<T1, T2>>
		{
			public virtual int Compare(Pair<T1, T2> pair1, Pair<T1, T2> pair2)
			{
				return -((IComparable<T1>)pair1.First()).CompareTo(pair2.First());
			}
		}

		/// <summary>
		/// Compares a <code>Pair</code> to another <code>Pair</code> according to the second object of the pair only
		/// This function will work providing
		/// the first element of the <code>Pair</code> is comparable, otherwise will throw a
		/// <code>ClassCastException</code>
		/// </summary>
		/// <author>jonathanberant</author>
		/// <?/>
		/// <?/>
		public class BySecondPairComparator<T1, T2> : IComparator<Pair<T1, T2>>
		{
			public virtual int Compare(Pair<T1, T2> pair1, Pair<T1, T2> pair2)
			{
				return ((IComparable<T2>)pair1.Second()).CompareTo(pair2.Second());
			}
		}

		/// <summary>
		/// Compares a <code>Pair</code> to another <code>Pair</code> according to the second object of the pair only in decreasing order
		/// This function will work providing
		/// the first element of the <code>Pair</code> is comparable, otherwise will throw a
		/// <code>ClassCastException</code>
		/// </summary>
		/// <author>jonathanberant</author>
		/// <?/>
		/// <?/>
		public class BySecondReversePairComparator<T1, T2> : IComparator<Pair<T1, T2>>
		{
			public virtual int Compare(Pair<T1, T2> pair1, Pair<T1, T2> pair2)
			{
				return -((IComparable<T2>)pair1.Second()).CompareTo(pair2.Second());
			}
		}
	}
}
