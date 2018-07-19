using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Base implementation of
	/// <see cref="ICoreMap"/>
	/// backed by two Java arrays.
	/// Reasonable care has been put into ensuring that this class is both fast and
	/// has a light memory footprint.
	/// Note that like the base classes in the Collections API, this implementation
	/// is <em>not thread-safe</em>. For speed reasons, these methods are not
	/// synchronized. A synchronized wrapper could be developed by anyone so
	/// inclined.
	/// Equality is defined over the complete set of keys and values currently
	/// stored in the map.  Because this class is mutable, it should not be used
	/// as a key in a Map.
	/// </summary>
	/// <author>dramage</author>
	/// <author>rafferty</author>
	[System.Serializable]
	public class ArrayCoreMap : ICoreMap
	{
		/// <summary>A listener for when a key is retrieved by the CoreMap.</summary>
		/// <remarks>
		/// A listener for when a key is retrieved by the CoreMap.
		/// This should only be used for testing.
		/// </remarks>
		public static IConsumer<Type> listener;

		/// <summary>Initial capacity of the array</summary>
		private const int InitialCapacity = 4;

		/// <summary>Array of keys</summary>
		private Type[] keys;

		/// <summary>Array of values</summary>
		private object[] values;

		/// <summary>Total number of elements actually in keys,values</summary>
		private int size;

		/// <summary>
		/// Default constructor - initializes with default initial annotation
		/// capacity of 4.
		/// </summary>
		public ArrayCoreMap()
			: this(InitialCapacity)
		{
		}

		/// <summary>
		/// Initializes this ArrayCoreMap, pre-allocating arrays to hold
		/// up to capacity key,value pairs.
		/// </summary>
		/// <remarks>
		/// Initializes this ArrayCoreMap, pre-allocating arrays to hold
		/// up to capacity key,value pairs.  This array will grow if necessary.
		/// </remarks>
		/// <param name="capacity">Initial capacity of object in key,value pairs</param>
		public ArrayCoreMap(int capacity)
		{
			/*, Serializable */
			// = null;
			// = 0;
			keys = ErasureUtils.UncheckedCast(new Type[capacity]);
			values = new object[capacity];
		}

		/// <summary>Copy constructor.</summary>
		/// <param name="other">The ArrayCoreMap to copy. It may not be null.</param>
		public ArrayCoreMap(Edu.Stanford.Nlp.Util.ArrayCoreMap other)
		{
			// size starts at 0
			size = other.size;
			keys = Arrays.CopyOf(other.keys, size);
			values = Arrays.CopyOf(other.values, size);
		}

		/// <summary>Copy constructor.</summary>
		/// <param name="other">The ArrayCoreMap to copy. It may not be null.</param>
		public ArrayCoreMap(ICoreMap other)
		{
			ICollection<Type> otherKeys = other.KeySet();
			size = otherKeys.Count;
			keys = new Type[size];
			values = new object[size];
			int i = 0;
			foreach (Type key in otherKeys)
			{
				this.keys[i] = key;
				this.values[i] = other.Get(key);
				i++;
			}
		}

		/// <summary><inheritDoc/></summary>
		public virtual VALUE Get<Value>(Type key)
		{
			for (int i = 0; i < size; i++)
			{
				if (key == keys[i])
				{
					if (listener != null)
					{
						listener.Accept(key);
					}
					// For tracking which entities were returned by the CoreMap
					return (VALUE)values[i];
				}
			}
			return null;
		}

		/// <summary><inheritDoc/></summary>
		public virtual VALUE Set<Value>(Type key, VALUE value)
		{
			// search array for existing value to replace
			for (int i = 0; i < size; i++)
			{
				if (keys[i] == key)
				{
					VALUE rv = (VALUE)values[i];
					values[i] = value;
					return rv;
				}
			}
			// not found in arrays, add to end ...
			// increment capacity of arrays if necessary
			if (size >= keys.Length)
			{
				int capacity = keys.Length + (keys.Length < 16 ? 4 : 8);
				Type[] newKeys = new Type[capacity];
				object[] newValues = new object[capacity];
				System.Array.Copy(keys, 0, newKeys, 0, size);
				System.Array.Copy(values, 0, newValues, 0, size);
				keys = newKeys;
				values = newValues;
			}
			// store value
			keys[size] = key;
			values[size] = value;
			size++;
			return null;
		}

		/// <summary><inheritDoc/></summary>
		public virtual ICollection<Type> KeySet()
		{
			return new _AbstractSet_162(this);
		}

		private sealed class _AbstractSet_162 : AbstractSet<Type>
		{
			public _AbstractSet_162(ArrayCoreMap _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override IEnumerator<Type> GetEnumerator()
			{
				return new _IEnumerator_165(this);
			}

			private sealed class _IEnumerator_165 : IEnumerator<Type>
			{
				public _IEnumerator_165(_AbstractSet_162 _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private int i;

				// = 0;
				public bool MoveNext()
				{
					return this.i < this._enclosing._enclosing.size;
				}

				public Type Current
				{
					get
					{
						try
						{
							return this._enclosing._enclosing.keys[this.i++];
						}
						catch (IndexOutOfRangeException)
						{
							throw new NoSuchElementException("ArrayCoreMap keySet iterator exhausted");
						}
					}
				}

				public void Remove()
				{
					this._enclosing._enclosing.Remove((Type)this._enclosing._enclosing.keys[this.i]);
				}

				private readonly _AbstractSet_162 _enclosing;
			}

			public override int Count
			{
				get
				{
					return this._enclosing.size;
				}
			}

			private readonly ArrayCoreMap _enclosing;
		}

		/// <summary>Return a set of keys such that the value of that key is not null.</summary>
		/// <returns>
		/// A hash set such that each element of the set is a key in this CoreMap that has a
		/// non-null value.
		/// </returns>
		public virtual ICollection<Type> KeySetNotNull()
		{
			ICollection<Type> mapKeys = new IdentityHashSet<Type>();
			for (int i = 0; i < Size(); ++i)
			{
				if (values[i] != null)
				{
					mapKeys.Add(this.keys[i]);
				}
			}
			return mapKeys;
		}

		/// <summary><inheritDoc/></summary>
		public virtual VALUE Remove<Value>(Type key)
		{
			object rv = null;
			for (int i = 0; i < size; i++)
			{
				if (keys[i] == key)
				{
					rv = values[i];
					if (i < size - 1)
					{
						System.Array.Copy(keys, i + 1, keys, i, size - (i + 1));
						System.Array.Copy(values, i + 1, values, i, size - (i + 1));
					}
					size--;
					break;
				}
			}
			return (VALUE)rv;
		}

		/// <summary><inheritDoc/></summary>
		public virtual bool ContainsKey<Value>(Type key)
		{
			for (int i = 0; i < size; i++)
			{
				if (keys[i] == key)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Reduces memory consumption to the minimum for representing the values
		/// currently stored stored in this object.
		/// </summary>
		public virtual void Compact()
		{
			if (keys.Length > size)
			{
				Type[] newKeys = new Type[size];
				object[] newValues = new object[size];
				System.Array.Copy(keys, 0, newKeys, 0, size);
				System.Array.Copy(values, 0, newValues, 0, size);
				keys = ErasureUtils.UncheckedCast(newKeys);
				values = newValues;
			}
		}

		public virtual void SetCapacity(int newSize)
		{
			if (size > newSize)
			{
				throw new Exception("You cannot set capacity to smaller than the current size.");
			}
			Type[] newKeys = new Type[newSize];
			object[] newValues = new object[newSize];
			System.Array.Copy(keys, 0, newKeys, 0, size);
			System.Array.Copy(values, 0, newValues, 0, size);
			keys = ErasureUtils.UncheckedCast(newKeys);
			values = newValues;
		}

		/// <summary>Returns the number of elements in this map.</summary>
		/// <returns>The number of elements in this map.</returns>
		public virtual int Size()
		{
			return size;
		}

		/// <summary>
		/// Keeps track of which ArrayCoreMaps have had toString called on
		/// them.
		/// </summary>
		/// <remarks>
		/// Keeps track of which ArrayCoreMaps have had toString called on
		/// them.  We do not want to loop forever when there are cycles in
		/// the annotation graph.  This is kept on a per-thread basis so that
		/// each thread where toString gets called can keep track of its own
		/// state.  When a call to toString is about to return, this is reset
		/// to null for that particular thread.
		/// </remarks>
		private static readonly ThreadLocal<IdentityHashSet<ICoreMap>> toStringCalled = ThreadLocal.WithInitial(null);

		/// <summary>Prints a full dump of a CoreMap.</summary>
		/// <remarks>
		/// Prints a full dump of a CoreMap. This method is robust to
		/// circularity in the CoreMap.
		/// </remarks>
		/// <returns>A String representation of the CoreMap</returns>
		public override string ToString()
		{
			IdentityHashSet<ICoreMap> calledSet = toStringCalled.Get();
			bool createdCalledSet = calledSet.IsEmpty();
			if (calledSet.Contains(this))
			{
				return "[...]";
			}
			calledSet.Add(this);
			StringBuilder s = new StringBuilder("[");
			for (int i = 0; i < size; i++)
			{
				s.Append(keys[i].GetSimpleName());
				s.Append('=');
				s.Append(values[i]);
				if (i < size - 1)
				{
					s.Append(' ');
				}
			}
			s.Append(']');
			if (createdCalledSet)
			{
				toStringCalled.Remove();
			}
			else
			{
				// Remove the object from the already called set so that
				// potential later calls in this object graph have something
				// more description than [...]
				calledSet.Remove(this);
			}
			return s.ToString();
		}

		private static readonly ConcurrentHashMap<Type, string> shortNames = new ConcurrentHashMap<Type, string>(12, 0.75f, 1);

		private const int ShorterStringCharstringStartSize = 64;

		private const int ShorterStringMaxSizeBeforeHashing = 5;

		// support caching of String form of keys for speedier printing
		/// <summary><inheritDoc/></summary>
		public virtual string ToShorterString(params string[] what)
		{
			StringBuilder s = new StringBuilder(ShorterStringCharstringStartSize);
			s.Append('[');
			ICollection<string> whatSet = null;
			if (size > ShorterStringMaxSizeBeforeHashing && what.Length > ShorterStringMaxSizeBeforeHashing)
			{
				// if there's a lot of stuff, hash.
				whatSet = new HashSet<string>(Arrays.AsList(what));
			}
			for (int i = 0; i < size; i++)
			{
				Type klass = keys[i];
				string name = shortNames[klass];
				if (name == null)
				{
					name = klass.GetSimpleName();
					int annoIdx = name.LastIndexOf("Annotation");
					if (annoIdx >= 0)
					{
						name = Sharpen.Runtime.Substring(name, 0, annoIdx);
					}
					shortNames[klass] = name;
				}
				bool include;
				if (what.Length == 0)
				{
					include = true;
				}
				else
				{
					if (whatSet != null)
					{
						include = whatSet.Contains(name);
					}
					else
					{
						include = false;
						foreach (string item in what)
						{
							if (item.Equals(name))
							{
								include = true;
								break;
							}
						}
					}
				}
				if (include)
				{
					if (s.Length > 1)
					{
						s.Append(' ');
					}
					s.Append(name);
					s.Append('=');
					s.Append(values[i]);
				}
			}
			s.Append(']');
			return s.ToString();
		}

		/// <summary>
		/// This gives a very short String representation of a CoreMap
		/// by leaving it to the content to reveal what field is being printed.
		/// </summary>
		/// <param name="what">
		/// An array (varargs) of Strings that say what annotation keys
		/// to print.  These need to be provided in a shortened form where you
		/// are just giving the part of the class name without package and up to
		/// "Annotation". That is,
		/// edu.stanford.nlp.ling.CoreAnnotations.PartOfSpeechAnnotation
		/// ➔ PartOfSpeech . As a special case, an empty array means
		/// to print everything, not nothing.
		/// </param>
		/// <returns>
		/// Brief string where the field values are just separated by a
		/// character. If the string contains spaces, it is wrapped in "{...}".
		/// </returns>
		public virtual string ToShortString(params string[] what)
		{
			return ToShortString('/', what);
		}

		/// <summary>
		/// This gives a very short String representation of a CoreMap
		/// by leaving it to the content to reveal what field is being printed.
		/// </summary>
		/// <param name="separator">Character placed between fields in output</param>
		/// <param name="what">
		/// An array (varargs) of Strings that say what annotation keys
		/// to print.  These need to be provided in a shortened form where you
		/// are just giving the part of the class name without package and up to
		/// "Annotation". That is,
		/// edu.stanford.nlp.ling.CoreAnnotations.PartOfSpeechAnnotation
		/// ➔ PartOfSpeech . As a special case, an empty array means
		/// to print everything, not nothing.
		/// </param>
		/// <returns>
		/// Brief string where the field values are just separated by a
		/// character. If the string contains spaces, it is wrapped in "{...}".
		/// </returns>
		public virtual string ToShortString(char separator, params string[] what)
		{
			StringBuilder s = new StringBuilder();
			for (int i = 0; i < size; i++)
			{
				bool include;
				if (what.Length > 0)
				{
					string name = keys[i].GetSimpleName();
					int annoIdx = name.LastIndexOf("Annotation");
					if (annoIdx >= 0)
					{
						name = Sharpen.Runtime.Substring(name, 0, annoIdx);
					}
					include = false;
					foreach (string item in what)
					{
						if (item.Equals(name))
						{
							include = true;
							break;
						}
					}
				}
				else
				{
					include = true;
				}
				if (include)
				{
					if (s.Length > 0)
					{
						s.Append(separator);
					}
					s.Append(values[i]);
				}
			}
			string answer = s.ToString();
			if (answer.IndexOf(' ') < 0)
			{
				return answer;
			}
			else
			{
				return '{' + answer + '}';
			}
		}

		/// <summary>
		/// Keeps track of which pairs of ArrayCoreMaps have had equals
		/// called on them.
		/// </summary>
		/// <remarks>
		/// Keeps track of which pairs of ArrayCoreMaps have had equals
		/// called on them.  We do not want to loop forever when there are
		/// cycles in the annotation graph.  This is kept on a per-thread
		/// basis so that each thread where equals gets called can keep
		/// track of its own state.  When a call to toString is about to
		/// return, this is reset to null for that particular thread.
		/// </remarks>
		private static readonly ThreadLocal<TwoDimensionalMap<ICoreMap, ICoreMap, bool>> equalsCalled = new ThreadLocal<TwoDimensionalMap<ICoreMap, ICoreMap, bool>>();

		/// <summary>Two CoreMaps are equal iff all keys and values are .equal.</summary>
		public override bool Equals(object obj)
		{
			if (!(obj is ICoreMap))
			{
				return false;
			}
			if (obj is HashableCoreMap)
			{
				// overridden behavior for HashableCoreMap
				return obj.Equals(this);
			}
			if (obj is Edu.Stanford.Nlp.Util.ArrayCoreMap)
			{
				// specialized equals for ArrayCoreMap
				return Equals((Edu.Stanford.Nlp.Util.ArrayCoreMap)obj);
			}
			// TODO: make the general equality work in the situation of loops in the object graph
			// general equality
			ICoreMap other = (ICoreMap)obj;
			if (!this.KeySet().Equals(other.KeySet()))
			{
				return false;
			}
			foreach (Type key in this.KeySet())
			{
				if (!other.ContainsKey(key))
				{
					return false;
				}
				object thisV = this.Get(key);
				object otherV = other.Get(key);
				if (thisV == otherV)
				{
					continue;
				}
				// the two values must be unequal, so if either is null, the other isn't
				if (thisV == null || otherV == null)
				{
					return false;
				}
				if (!thisV.Equals(otherV))
				{
					return false;
				}
			}
			return true;
		}

		private bool Equals(Edu.Stanford.Nlp.Util.ArrayCoreMap other)
		{
			TwoDimensionalMap<ICoreMap, ICoreMap, bool> calledMap = equalsCalled.Get();
			bool createdCalledMap = (calledMap == null);
			if (createdCalledMap)
			{
				calledMap = TwoDimensionalMap.IdentityHashMap();
				equalsCalled.Set(calledMap);
			}
			// Note that for the purposes of recursion, we assume the two maps
			// are equals.  The two maps will therefore be equal if they
			// encounter each other again during the recursion unless there is
			// some other key that causes the equality to fail.
			// We do not need to later put false, as the entire call to equals
			// will unwind with false if any one equality check returns false.
			// TODO: since we only ever keep "true", we would rather use a
			// TwoDimensionalSet, but no such thing exists
			if (calledMap.Contains(this, other))
			{
				return true;
			}
			bool result = true;
			calledMap.Put(this, other, true);
			calledMap.Put(other, this, true);
			if (this.size != other.size)
			{
				result = false;
			}
			else
			{
				for (int i = 0; i < this.size; i++)
				{
					// test if other contains this key,value pair
					bool matched = false;
					for (int j = 0; j < other.size; j++)
					{
						if (this.keys[i] == other.keys[j])
						{
							if ((this.values[i] == null && other.values[j] != null) || (this.values[i] != null && other.values[j] == null))
							{
								matched = false;
								break;
							}
							if ((this.values[i] == null && other.values[j] == null) || (this.values[i].Equals(other.values[j])))
							{
								matched = true;
								break;
							}
						}
					}
					if (!matched)
					{
						result = false;
						break;
					}
				}
			}
			if (createdCalledMap)
			{
				equalsCalled.Set(null);
			}
			return result;
		}

		/// <summary>
		/// Keeps track of which ArrayCoreMaps have had hashCode called on
		/// them.
		/// </summary>
		/// <remarks>
		/// Keeps track of which ArrayCoreMaps have had hashCode called on
		/// them.  We do not want to loop forever when there are cycles in
		/// the annotation graph.  This is kept on a per-thread basis so that
		/// each thread where hashCode gets called can keep track of its own
		/// state.  When a call to toString is about to return, this is reset
		/// to null for that particular thread.
		/// </remarks>
		private static readonly ThreadLocal<IdentityHashSet<ICoreMap>> hashCodeCalled = new ThreadLocal<IdentityHashSet<ICoreMap>>();

		/// <summary>
		/// Returns a composite hashCode over all the keys and values currently
		/// stored in the map.
		/// </summary>
		/// <remarks>
		/// Returns a composite hashCode over all the keys and values currently
		/// stored in the map.  Because they may change over time, this class
		/// is not appropriate for use as map keys.
		/// </remarks>
		public override int GetHashCode()
		{
			IdentityHashSet<ICoreMap> calledSet = hashCodeCalled.Get();
			bool createdCalledSet = (calledSet == null);
			if (createdCalledSet)
			{
				calledSet = new IdentityHashSet<ICoreMap>();
				hashCodeCalled.Set(calledSet);
			}
			if (calledSet.Contains(this))
			{
				return 0;
			}
			calledSet.Add(this);
			int keysCode = 0;
			int valuesCode = 0;
			for (int i = 0; i < size; i++)
			{
				keysCode += (i < keys.Length && values[i] != null ? keys[i].GetHashCode() : 0);
				valuesCode += (i < values.Length && values[i] != null ? values[i].GetHashCode() : 0);
			}
			if (createdCalledSet)
			{
				hashCodeCalled.Set(null);
			}
			else
			{
				// Remove the object after processing is complete so that if
				// there are multiple instances of this CoreMap in the overall
				// object graph, they each have their hash code calculated.
				// TODO: can we cache this for later?
				calledSet.Remove(this);
			}
			return keysCode * 37 + valuesCode;
		}

		/// <summary>Serialization version id</summary>
		private const long serialVersionUID = 1L;

		//
		// serialization magic
		//
		/// <summary>Overridden serialization method: compacts our map before writing.</summary>
		/// <param name="out">Stream to write to</param>
		/// <exception cref="System.IO.IOException">If IO error</exception>
		private void WriteObject(ObjectOutputStream @out)
		{
			Compact();
			@out.DefaultWriteObject();
		}

		// TODO: make prettyLog work in the situation of loops in the object graph
		/// <summary><inheritDoc/></summary>
		public virtual void PrettyLog(Redwood.RedwoodChannels channels, string description)
		{
			Redwood.StartTrack(description);
			// sort keys by class name
			IList<Type> sortedKeys = new List<Type>(this.KeySet());
			sortedKeys.Sort(IComparer.Comparing(null));
			// log key/value pairs
			foreach (Type key in sortedKeys)
			{
				string keyName = key.GetCanonicalName().Replace("class ", string.Empty);
				object value = this.Get(key);
				if (PrettyLogger.Dispatchable(value))
				{
					PrettyLogger.Log(channels, keyName, value);
				}
				else
				{
					channels.Logf("%s = %s", keyName, value);
				}
			}
			Redwood.EndTrack(description);
		}
	}
}
