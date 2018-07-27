using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A collection of utilities to make dealing with Java generics less
	/// painful and verbose.
	/// </summary>
	/// <remarks>
	/// A collection of utilities to make dealing with Java generics less
	/// painful and verbose.  For example, rather than declaring
	/// <pre>
	/// <c>Map&lt;String,ClassicCounter&lt;List&lt;String&gt;&gt;&gt; = new HashMap&lt;String,ClassicCounter&lt;List&lt;String&gt;&gt;&gt;()</c>
	/// </pre>
	/// you just call <code>Generics.newHashMap()</code>:
	/// <pre>
	/// <c>Map&lt;String,ClassicCounter&lt;List&lt;String&gt;&gt;&gt; = Generics.newHashMap()</c>
	/// </pre>
	/// Java type-inference will almost always just <em>do the right thing</em>
	/// (every once in a while, the compiler will get confused before you do,
	/// so you might still occasionally have to specify the appropriate types).
	/// This class is based on the examples in Brian Goetz's article
	/// <a href="http://www.ibm.com/developerworks/library/j-jtp02216.html">Java
	/// theory and practice: The pseudo-typedef antipattern</a>.
	/// </remarks>
	/// <author>Ilya Sherman</author>
	public class Generics
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.Generics));

		private Generics()
		{
		}

		// static class
		/* Collections */
		public static List<E> NewArrayList<E>()
		{
			return new List<E>();
		}

		public static List<E> NewArrayList<E>(int size)
		{
			return new List<E>(size);
		}

		public static List<E> NewArrayList<E, _T1>(ICollection<_T1> c)
			where _T1 : E
		{
			return new List<E>(c);
		}

		public static LinkedList<E> NewLinkedList<E>()
		{
			return new LinkedList<E>();
		}

		public static LinkedList<E> NewLinkedList<E, _T1>(ICollection<_T1> c)
			where _T1 : E
		{
			return new LinkedList<E>(c);
		}

		public static Stack<E> NewStack<E>()
		{
			return new Stack<E>();
		}

		public static BinaryHeapPriorityQueue<E> NewBinaryHeapPriorityQueue<E>()
		{
			return new BinaryHeapPriorityQueue<E>();
		}

		public static TreeSet<E> NewTreeSet<E>()
		{
			return new TreeSet<E>();
		}

		public static TreeSet<E> NewTreeSet<E, _T1>(IComparator<_T1> comparator)
		{
			return new TreeSet<E>(comparator);
		}

		public static TreeSet<E> NewTreeSet<E>(ISortedSet<E> s)
		{
			return new TreeSet<E>(s);
		}

		public const string HashSetProperty = "edu.stanford.nlp.hashset.impl";

		public static readonly string HashSetClassname = Runtime.GetProperty(HashSetProperty);

		private static readonly Type HashSetClass = GetHashSetClass();

		private static readonly ConstructorInfo HashSetSizeConstructor = GetHashSetSizeConstructor();

		private static readonly ConstructorInfo HashSetCollectionConstructor = GetHashSetCollectionConstructor();

		private static Type GetHashSetClass()
		{
			try
			{
				if (HashSetClassname == null)
				{
					return typeof(HashSet);
				}
				else
				{
					return Sharpen.Runtime.GetType(HashSetClassname);
				}
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		// must be called after HASH_SET_CLASS is defined
		private static ConstructorInfo GetHashSetSizeConstructor()
		{
			try
			{
				return HashSetClass.GetConstructor(typeof(int));
			}
			catch (Exception)
			{
				log.Info("Warning: could not find a constructor for objects of " + HashSetClass + " which takes an integer argument.  Will use the no argument constructor instead.");
			}
			return null;
		}

		// must be called after HASH_SET_CLASS is defined
		private static ConstructorInfo GetHashSetCollectionConstructor()
		{
			try
			{
				return HashSetClass.GetConstructor(typeof(ICollection));
			}
			catch (Exception e)
			{
				throw new Exception("Error: could not find a constructor for objects of " + HashSetClass + " which takes an existing collection argument.", e);
			}
		}

		public static ICollection<E> NewHashSet<E>()
		{
			try
			{
				return ErasureUtils.UncheckedCast(System.Activator.CreateInstance(HashSetClass));
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public static ICollection<E> NewHashSet<E>(int initialCapacity)
		{
			if (HashSetSizeConstructor == null)
			{
				return NewHashSet();
			}
			try
			{
				return ErasureUtils.UncheckedCast(HashSetSizeConstructor.NewInstance(initialCapacity));
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public static ICollection<E> NewHashSet<E, _T1>(ICollection<_T1> c)
			where _T1 : E
		{
			try
			{
				return ErasureUtils.UncheckedCast(HashSetCollectionConstructor.NewInstance(c));
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public const string HashMapProperty = "edu.stanford.nlp.hashmap.impl";

		public static readonly string HashMapClassname = Runtime.GetProperty(HashMapProperty);

		private static readonly Type HashMapClass = GetHashMapClass();

		private static readonly ConstructorInfo HashMapSizeConstructor = GetHashMapSizeConstructor();

		private static readonly ConstructorInfo HashMapFromMapConstructor = GetHashMapFromMapConstructor();

		private static Type GetHashMapClass()
		{
			try
			{
				if (HashMapClassname == null)
				{
					return typeof(Hashtable);
				}
				else
				{
					return Sharpen.Runtime.GetType(HashMapClassname);
				}
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		// must be called after HASH_MAP_CLASS is defined
		private static ConstructorInfo GetHashMapSizeConstructor()
		{
			try
			{
				return HashMapClass.GetConstructor(typeof(int));
			}
			catch (Exception)
			{
				log.Info("Warning: could not find a constructor for objects of " + HashMapClass + " which takes an integer argument.  Will use the no argument constructor instead.");
			}
			return null;
		}

		// must be called after HASH_MAP_CLASS is defined
		private static ConstructorInfo GetHashMapFromMapConstructor()
		{
			try
			{
				return HashMapClass.GetConstructor(typeof(IDictionary));
			}
			catch (Exception e)
			{
				throw new Exception("Error: could not find a constructor for objects of " + HashMapClass + " which takes an existing Map argument.", e);
			}
		}

		/* Maps */
		public static IDictionary<K, V> NewHashMap<K, V>()
		{
			try
			{
				return ErasureUtils.UncheckedCast(System.Activator.CreateInstance(HashMapClass));
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public static IDictionary<K, V> NewHashMap<K, V>(int initialCapacity)
		{
			if (HashMapSizeConstructor == null)
			{
				return NewHashMap();
			}
			try
			{
				return ErasureUtils.UncheckedCast(HashMapSizeConstructor.NewInstance(initialCapacity));
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public static IDictionary<K, V> NewHashMap<K, V, _T2>(IDictionary<_T2> m)
			where _T2 : K
		{
			try
			{
				return ErasureUtils.UncheckedCast(HashMapFromMapConstructor.NewInstance(m));
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		public static IdentityHashMap<K, V> NewIdentityHashMap<K, V>()
		{
			return new IdentityHashMap<K, V>();
		}

		public static ICollection<K> NewIdentityHashSet<K>()
		{
			return Java.Util.Collections.NewSetFromMap(Edu.Stanford.Nlp.Util.Generics.NewIdentityHashMap<K, bool>());
		}

		public static WeakHashMap<K, V> NewWeakHashMap<K, V>()
		{
			return new WeakHashMap<K, V>();
		}

		public static ConcurrentHashMap<K, V> NewConcurrentHashMap<K, V>()
		{
			return new ConcurrentHashMap<K, V>();
		}

		public static ConcurrentHashMap<K, V> NewConcurrentHashMap<K, V>(int initialCapacity)
		{
			return new ConcurrentHashMap<K, V>(initialCapacity);
		}

		public static ConcurrentHashMap<K, V> NewConcurrentHashMap<K, V>(int initialCapacity, float loadFactor, int concurrencyLevel)
		{
			return new ConcurrentHashMap<K, V>(initialCapacity, loadFactor, concurrencyLevel);
		}

		public static SortedDictionary<K, V> NewTreeMap<K, V>()
		{
			return new SortedDictionary<K, V>();
		}

		public static IIndex<E> NewIndex<E>()
		{
			return new HashIndex<E>();
		}

		public static ICollection<E> NewConcurrentHashSet<E>()
		{
			return Java.Util.Collections.NewSetFromMap(new ConcurrentHashMap<E, bool>());
		}

		public static ICollection<E> NewConcurrentHashSet<E>(ICollection<E> set)
		{
			ICollection<E> ret = Java.Util.Collections.NewSetFromMap(new ConcurrentHashMap<E, bool>());
			Sharpen.Collections.AddAll(ret, set);
			return ret;
		}

		/* Other */
		public static Pair<T1, T2> NewPair<T1, T2>(T1 first, T2 second)
		{
			return new Pair<T1, T2>(first, second);
		}

		public static Triple<T1, T2, T3> NewTriple<T1, T2, T3>(T1 first, T2 second, T3 third)
		{
			return new Triple<T1, T2, T3>(first, second, third);
		}

		public static Interner<T> NewInterner<T>()
		{
			return new Interner<T>();
		}

		public static SynchronizedInterner<T> NewSynchronizedInterner<T>(Interner<T> interner)
		{
			return new SynchronizedInterner<T>(interner);
		}

		public static SynchronizedInterner<T> NewSynchronizedInterner<T>(Interner<T> interner, object mutex)
		{
			return new SynchronizedInterner<T>(interner, mutex);
		}

		public static WeakReference<T> NewWeakReference<T>(T referent)
		{
			return new WeakReference<T>(referent);
		}
	}
}
