// Stanford JavaNLP support classes
// Copyright (c) 2001-2008 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    java-nlp-support@lists.stanford.edu
//    http://nlp.stanford.edu/software/
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// A specialized kind of hash table (or map) for storing numeric counts for
	/// objects.
	/// </summary>
	/// <remarks>
	/// A specialized kind of hash table (or map) for storing numeric counts for
	/// objects. It works like a Map,
	/// but with different methods for easily getting/setting/incrementing counts
	/// for objects and computing various functions with the counts.
	/// The Counter constructor
	/// and <tt>addAll</tt> method can be used to copy another Counter's contents
	/// over.
	/// <p>
	/// <i>Implementation notes:</i>
	/// You shouldn't casually add further methods to
	/// this interface. Rather, they should be added to the
	/// <see cref="Counters"/>
	/// class.
	/// Note that this class stores a
	/// <c>totalCount</c>
	/// field as well as the map.  This makes certain
	/// operations much more efficient, but means that any methods that change the
	/// map must also update
	/// <c>totalCount</c>
	/// appropriately. If you use the
	/// <c>setCount</c>
	/// method, then you cannot go wrong.
	/// This class is not threadsafe: If multiple threads are accessing the same
	/// counter, then access should be synchronized externally to this class.
	/// </remarks>
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	/// <author>Joseph Smarr (jsmarr@stanford.edu)</author>
	/// <author>Teg Grenager</author>
	/// <author>Galen Andrew</author>
	/// <author>Christopher Manning</author>
	/// <author>Kayur Patel (kdpatel@cs)</author>
	[System.Serializable]
	public class ClassicCounter<E> : ICounter<E>, IEnumerable<E>
	{
		internal IDictionary<E, MutableDouble> map;

		private readonly MapFactory<E, MutableDouble> mapFactory;

		private double totalCount;

		private double defaultValue;

		private const long serialVersionUID = 4L;

		[System.NonSerialized]
		private MutableDouble tempMDouble;

		/// <summary>Constructs a new (empty) Counter backed by a HashMap.</summary>
		public ClassicCounter()
			: this(MapFactory.HashMapFactory())
		{
		}

		public ClassicCounter(int initialCapacity)
			: this(MapFactory.HashMapFactory(), initialCapacity)
		{
		}

		/// <summary>Pass in a MapFactory and the map it vends will back your Counter.</summary>
		/// <param name="mapFactory">The Map this factory vends will back your Counter.</param>
		public ClassicCounter(MapFactory<E, MutableDouble> mapFactory)
		{
			// todo [cdm 2016]: Get rid of all the tempMDouble stuff. It just can't be the best way in 2016 - use new Map methods?
			// accessed by DeltaCounter
			// = 0.0
			// = 0.0;
			// for more efficient speed/memory usage
			// = null;
			// CONSTRUCTORS
			this.mapFactory = mapFactory;
			this.map = mapFactory.NewMap();
		}

		/// <summary>Pass in a MapFactory and the map it vends will back your Counter.</summary>
		/// <param name="mapFactory">The Map this factory vends will back your Counter.</param>
		/// <param name="initialCapacity">initial capacity of the counter</param>
		public ClassicCounter(MapFactory<E, MutableDouble> mapFactory, int initialCapacity)
		{
			this.mapFactory = mapFactory;
			this.map = mapFactory.NewMap(initialCapacity);
		}

		/// <summary>Constructs a new Counter with the contents of the given Counter.</summary>
		/// <remarks>
		/// Constructs a new Counter with the contents of the given Counter.
		/// <i>Implementation note:</i> A new Counter is allocated with its
		/// own counts, but keys will be shared and should be an immutable class.
		/// </remarks>
		/// <param name="c">The Counter which will be copied.</param>
		public ClassicCounter(ICounter<E> c)
			: this()
		{
			Counters.AddInPlace(this, c);
			SetDefaultReturnValue(c.DefaultReturnValue());
		}

		/// <summary>Constructs a new Counter by counting the elements in the given Collection.</summary>
		/// <remarks>
		/// Constructs a new Counter by counting the elements in the given Collection.
		/// The Counter is backed by a HashMap.
		/// </remarks>
		/// <param name="collection">
		/// Each item in the Collection is made a key in the
		/// Counter with count being its multiplicity in the Collection.
		/// </param>
		public ClassicCounter(ICollection<E> collection)
			: this()
		{
			foreach (E key in collection)
			{
				IncrementCount(key);
			}
		}

		public static Edu.Stanford.Nlp.Stats.ClassicCounter<E> IdentityHashMapCounter<E>()
		{
			return new Edu.Stanford.Nlp.Stats.ClassicCounter<E>(MapFactory.IdentityHashMapFactory<E, MutableDouble>());
		}

		// STANDARD ACCESS MODIFICATION METHODS
		/// <summary>Get the MapFactory for this Counter.</summary>
		/// <remarks>
		/// Get the MapFactory for this Counter.
		/// This method is needed by the DeltaCounter implementation.
		/// </remarks>
		/// <returns>The MapFactory</returns>
		internal virtual MapFactory<E, MutableDouble> GetMapFactory()
		{
			return mapFactory;
		}

		// METHODS NEEDED BY THE Counter INTERFACE
		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual IFactory<ICounter<E>> GetFactory()
		{
			return new ClassicCounter.ClassicCounterFactory<E>(GetMapFactory());
		}

		[System.Serializable]
		private class ClassicCounterFactory<E> : IFactory<ICounter<E>>
		{
			private const long serialVersionUID = 1L;

			private readonly MapFactory<E, MutableDouble> mf;

			private ClassicCounterFactory(MapFactory<E, MutableDouble> mf)
			{
				this.mf = mf;
			}

			public virtual ICounter<E> Create()
			{
				return new ClassicCounter<E>(mf);
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public void SetDefaultReturnValue(double rv)
		{
			defaultValue = rv;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double DefaultReturnValue()
		{
			return defaultValue;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double GetCount(object key)
		{
			Number count = map[key];
			if (count == null)
			{
				return defaultValue;
			}
			// haven't seen this object before -> default count
			return count;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual void SetCount(E key, double count)
		{
			if (tempMDouble == null)
			{
				//System.out.println("creating mdouble");
				tempMDouble = new MutableDouble();
			}
			//System.out.println("setting mdouble");
			tempMDouble.Set(count);
			//System.out.println("putting mdouble in map");
			tempMDouble = map[key] = tempMDouble;
			//System.out.println("placed mDouble in map");
			totalCount += count;
			if (tempMDouble != null)
			{
				totalCount -= tempMDouble;
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double IncrementCount(E key, double count)
		{
			if (tempMDouble == null)
			{
				tempMDouble = new MutableDouble();
			}
			MutableDouble oldMDouble = map[key] = tempMDouble;
			totalCount += count;
			if (oldMDouble != null)
			{
				count += oldMDouble;
			}
			tempMDouble.Set(count);
			tempMDouble = oldMDouble;
			return count;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public double IncrementCount(E key)
		{
			return IncrementCount(key, 1.0);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double DecrementCount(E key, double count)
		{
			return IncrementCount(key, -count);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double DecrementCount(E key)
		{
			return IncrementCount(key, -1.0);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double LogIncrementCount(E key, double count)
		{
			if (tempMDouble == null)
			{
				tempMDouble = new MutableDouble();
			}
			MutableDouble oldMDouble = map[key] = tempMDouble;
			if (oldMDouble != null)
			{
				count = SloppyMath.LogAdd(count, oldMDouble);
				totalCount += count - oldMDouble;
			}
			else
			{
				totalCount += count;
			}
			tempMDouble.Set(count);
			tempMDouble = oldMDouble;
			return count;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual void AddAll(ICounter<E> counter)
		{
			Counters.AddInPlace(this, counter);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double Remove(E key)
		{
			MutableDouble d = MutableRemove(key);
			// this also updates totalCount
			if (d != null)
			{
				return d;
			}
			return defaultValue;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual bool ContainsKey(E key)
		{
			return map.Contains(key);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ICollection<E> KeySet()
		{
			return map.Keys;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ICollection<double> Values()
		{
			return new _AbstractCollection_316(this);
		}

		private sealed class _AbstractCollection_316 : AbstractCollection<double>
		{
			public _AbstractCollection_316(ClassicCounter<E> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override IEnumerator<double> GetEnumerator()
			{
				return new _IEnumerator_319(this);
			}

			private sealed class _IEnumerator_319 : IEnumerator<double>
			{
				public _IEnumerator_319()
				{
					this.inner = this._enclosing._enclosing.map.Values.GetEnumerator();
				}

				internal IEnumerator<MutableDouble> inner;

				public bool MoveNext()
				{
					return this.inner.MoveNext();
				}

				public double Current
				{
					get
					{
						// copy so as to give safety to mutable internal representation
						return double.ValueOf(this.inner.Current);
					}
				}

				public void Remove()
				{
					throw new NotSupportedException();
				}
			}

			public override int Count
			{
				get
				{
					return this._enclosing.map.Count;
				}
			}

			public override bool Contains(object v)
			{
				return v is double && this._enclosing.map.Values.Contains(new MutableDouble((double)v));
			}

			private readonly ClassicCounter<E> _enclosing;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ICollection<KeyValuePair<E, double>> EntrySet()
		{
			return new _AbstractSet_356(this);
		}

		private sealed class _AbstractSet_356 : AbstractSet<KeyValuePair<E, double>>
		{
			public _AbstractSet_356(ClassicCounter<E> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override IEnumerator<KeyValuePair<E, double>> GetEnumerator()
			{
				return new _IEnumerator_359(this);
			}

			private sealed class _IEnumerator_359 : IEnumerator<KeyValuePair<E, double>>
			{
				public _IEnumerator_359(_AbstractSet_356 _enclosing)
				{
					this._enclosing = _enclosing;
					this.inner = this._enclosing._enclosing.map.GetEnumerator();
				}

				internal readonly IEnumerator<KeyValuePair<E, MutableDouble>> inner;

				public bool MoveNext()
				{
					return this.inner.MoveNext();
				}

				public KeyValuePair<E, double> Current
				{
					get
					{
						return new _KeyValuePair_369(this);
					}
				}

				private sealed class _KeyValuePair_369 : KeyValuePair<E, double>
				{
					public _KeyValuePair_369(_IEnumerator_359 _enclosing)
					{
						this._enclosing = _enclosing;
						this.e = this._enclosing.inner.Current;
					}

					internal readonly KeyValuePair<E, MutableDouble> e;

					public double GetDoubleValue()
					{
						return this.e.Value;
					}

					public double SetValue(double value)
					{
						double old = this.e.Value;
						this.e.Value.Set(value);
						this._enclosing._enclosing._enclosing.totalCount = this._enclosing._enclosing._enclosing.totalCount - old + value;
						return old;
					}

					public E Key
					{
						get
						{
							return this.e.Key;
						}
					}

					public double Value
					{
						get
						{
							return this.GetDoubleValue();
						}
					}

					public double SetValue(double value)
					{
						return this.SetValue(value);
					}

					private readonly _IEnumerator_359 _enclosing;
				}

				public void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly _AbstractSet_356 _enclosing;
			}

			public override int Count
			{
				get
				{
					return this._enclosing.map.Count;
				}
			}

			private readonly ClassicCounter<E> _enclosing;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual void Clear()
		{
			map.Clear();
			totalCount = 0.0;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual int Size()
		{
			return map.Count;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual double TotalCount()
		{
			return totalCount;
		}

		// ADDITIONAL MAP LIKE OPERATIONS (NOT IN Counter INTERFACE)
		// THEIR USE IS DISCOURAGED, BUT THEY HAVEN'T (YET) BEEN REMOVED.
		/// <summary>This is a shorthand for keySet.iterator().</summary>
		/// <remarks>
		/// This is a shorthand for keySet.iterator(). It's not really clear that
		/// this method should be here, as the Map interface has no such shortcut,
		/// but it's used in a number of places, and I've left it in for now.
		/// Use is discouraged.
		/// </remarks>
		/// <returns>An Iterator over the keys in the Counter.</returns>
		public virtual IEnumerator<E> GetEnumerator()
		{
			return KeySet().GetEnumerator();
		}

		/// <summary>
		/// This is used internally to the class for getting back a
		/// MutableDouble in a remove operation.
		/// </summary>
		/// <remarks>
		/// This is used internally to the class for getting back a
		/// MutableDouble in a remove operation.  Not for public use.
		/// </remarks>
		/// <param name="key">The key to remove</param>
		/// <returns>Its value as a MutableDouble</returns>
		private MutableDouble MutableRemove(E key)
		{
			MutableDouble md = Sharpen.Collections.Remove(map, key);
			if (md != null)
			{
				totalCount -= md;
			}
			return md;
		}

		/// <summary>Removes all the given keys from this Counter.</summary>
		/// <remarks>
		/// Removes all the given keys from this Counter.
		/// Keys may be included that are not actually in the
		/// Counter - no action is taken in response to those
		/// keys.  This behavior should be retained in future
		/// revisions of Counter (matches HashMap).
		/// </remarks>
		/// <param name="keys">
		/// The keys to remove from the Counter. Their values are
		/// subtracted from the total count mass of the Counter.
		/// </param>
		public virtual void RemoveAll(ICollection<E> keys)
		{
			foreach (E key in keys)
			{
				MutableRemove(key);
			}
		}

		/// <summary>Returns whether a Counter has no keys in it.</summary>
		/// <returns>true iff a Counter has no keys in it.</returns>
		public virtual bool IsEmpty()
		{
			return Size() == 0;
		}

		// OBJECT STUFF
		// NOTE: Using @inheritdoc to get back to Object's javadoc doesn't work
		// on a class that implements an interface in 1.6.  Weird, but there you go.
		/// <summary>Equality is defined over all Counter implementations.</summary>
		/// <remarks>
		/// Equality is defined over all Counter implementations.
		/// Two Counters are equal if they have the same keys explicitly stored
		/// with the same values.
		/// <p>
		/// Note that a Counter with a key with value defaultReturnValue will not
		/// be judged equal to a Counter that is lacking that key. In order for
		/// two Counters to be correctly judged equal in such cases, you should
		/// call Counters.retainNonDefaultValues() on both Counters first.
		/// </remarks>
		/// <param name="o">Object to compare for equality</param>
		/// <returns>Whether this is equal to o</returns>
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			else
			{
				if (!(o is ICounter))
				{
					return false;
				}
				else
				{
					if (!(o is ClassicCounter))
					{
						return Counters.Equals(this, (ICounter<E>)o);
					}
				}
			}
			ClassicCounter<E> counter = (ClassicCounter<E>)o;
			return totalCount == counter.totalCount && map.Equals(counter.map);
		}

		/// <summary>Returns a hashCode which is the underlying Map's hashCode.</summary>
		/// <returns>A hashCode.</returns>
		public override int GetHashCode()
		{
			return map.GetHashCode();
		}

		/// <summary>
		/// Returns a String representation of the Counter, as formatted by
		/// the underlying Map.
		/// </summary>
		/// <returns>A String representation of the Counter.</returns>
		public override string ToString()
		{
			return map.ToString();
		}

		// EXTRA I/O METHODS
		/// <summary>Returns the Counter over Strings specified by this String.</summary>
		/// <remarks>
		/// Returns the Counter over Strings specified by this String.
		/// The String is often the whole contents of a file.
		/// The file can include comments if each line of comment starts with
		/// a hash (#) symbol, and does not contain any TAB characters.
		/// Otherwise, the format is one entry per line.  Each line must contain
		/// precisely one tab separating a key and a value, giving a format of:
		/// <blockquote>
		/// StringKey\tdoubleValue\n
		/// </blockquote>
		/// </remarks>
		/// <param name="s">
		/// String representation of a Counter, where entries are one per
		/// line such that each line is either a comment (begins with #)
		/// or key \t value
		/// </param>
		/// <returns>The Counter with String keys</returns>
		public static ClassicCounter<string> ValueOfIgnoreComments(string s)
		{
			ClassicCounter<string> result = new ClassicCounter<string>();
			string[] lines = s.Split("\n");
			foreach (string line in lines)
			{
				string[] fields = line.Split("\t");
				if (fields.Length != 2)
				{
					if (line.StartsWith("#"))
					{
						continue;
					}
					else
					{
						throw new Exception("Got unsplittable line: \"" + line + '\"');
					}
				}
				result.SetCount(fields[0], double.ParseDouble(fields[1]));
			}
			return result;
		}

		/// <summary>
		/// Converts from the format printed by the toString method back into
		/// a Counter&lt;String&gt;.
		/// </summary>
		/// <remarks>
		/// Converts from the format printed by the toString method back into
		/// a Counter&lt;String&gt;.  The toString() doesn't escape, so this only
		/// works providing the keys of the Counter do not have commas or equals signs
		/// in them.
		/// </remarks>
		/// <param name="s">A String representation of a Counter</param>
		/// <returns>The Counter</returns>
		public static ClassicCounter<string> FromString(string s)
		{
			ClassicCounter<string> result = new ClassicCounter<string>();
			if (!s.StartsWith("{") || !s.EndsWith("}"))
			{
				throw new Exception("invalid format: ||" + s + "||");
			}
			s = Sharpen.Runtime.Substring(s, 1, s.Length - 1);
			string[] lines = s.Split(", ");
			foreach (string line in lines)
			{
				string[] fields = line.Split("=");
				if (fields.Length != 2)
				{
					throw new Exception("Got unsplittable line: \"" + line + '\"');
				}
				result.SetCount(fields[0], double.ParseDouble(fields[1]));
			}
			return result;
		}

		/// <summary><inheritDoc/></summary>
		public virtual void PrettyLog(Redwood.RedwoodChannels channels, string description)
		{
			PrettyLogger.Log(channels, description, Counters.AsMap(this));
		}
	}
}
