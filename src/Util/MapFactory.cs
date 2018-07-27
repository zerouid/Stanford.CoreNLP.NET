using System;
using System.Collections.Generic;




namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A factory class for vending different sorts of Maps.</summary>
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	/// <author>Kayur Patel (kdpatel@cs)</author>
	[System.Serializable]
	public abstract class MapFactory<K, V>
	{
		protected internal MapFactory()
		{
		}

		private const long serialVersionUID = 4529666940763477360L;

		public static readonly Edu.Stanford.Nlp.Util.MapFactory HashMapFactory = new MapFactory.HashMapFactory();

		public static readonly Edu.Stanford.Nlp.Util.MapFactory IdentityHashMapFactory = new MapFactory.IdentityHashMapFactory();

		private static readonly Edu.Stanford.Nlp.Util.MapFactory WeakHashMapFactory = new MapFactory.WeakHashMapFactory();

		private static readonly Edu.Stanford.Nlp.Util.MapFactory TreeMapFactory = new MapFactory.TreeMapFactory();

		private static readonly Edu.Stanford.Nlp.Util.MapFactory LinkedHashMapFactory = new MapFactory.LinkedHashMapFactory();

		private static readonly Edu.Stanford.Nlp.Util.MapFactory ArrayMapFactory = new MapFactory.ArrayMapFactory();

		public static readonly Edu.Stanford.Nlp.Util.MapFactory ConcurrentMapFactory = new MapFactory.ConcurrentMapFactory();

		// allow people to write subclasses
		/// <summary>Return a MapFactory that returns a HashMap.</summary>
		/// <remarks>
		/// Return a MapFactory that returns a HashMap.
		/// <i>Implementation note: This method uses the same trick as the methods
		/// like emptyMap() introduced in the Collections class in JDK1.5 where
		/// callers can call this method with apparent type safety because this
		/// method takes the hit for the cast.
		/// </remarks>
		/// <returns>A MapFactory that makes a HashMap.</returns>
		public static Edu.Stanford.Nlp.Util.MapFactory<K, V> HashMapFactory<K, V>()
		{
			return HashMapFactory;
		}

		/// <summary>Return a MapFactory that returns an IdentityHashMap.</summary>
		/// <remarks>
		/// Return a MapFactory that returns an IdentityHashMap.
		/// <i>Implementation note: This method uses the same trick as the methods
		/// like emptyMap() introduced in the Collections class in JDK1.5 where
		/// callers can call this method with apparent type safety because this
		/// method takes the hit for the cast.
		/// </remarks>
		/// <returns>A MapFactory that makes a HashMap.</returns>
		public static Edu.Stanford.Nlp.Util.MapFactory<K, V> IdentityHashMapFactory<K, V>()
		{
			return IdentityHashMapFactory;
		}

		/// <summary>Return a MapFactory that returns a WeakHashMap.</summary>
		/// <remarks>
		/// Return a MapFactory that returns a WeakHashMap.
		/// <i>Implementation note: This method uses the same trick as the methods
		/// like emptyMap() introduced in the Collections class in JDK1.5 where
		/// callers can call this method with apparent type safety because this
		/// method takes the hit for the cast.
		/// </remarks>
		/// <returns>A MapFactory that makes a WeakHashMap.</returns>
		public static Edu.Stanford.Nlp.Util.MapFactory<K, V> WeakHashMapFactory<K, V>()
		{
			return WeakHashMapFactory;
		}

		/// <summary>Return a MapFactory that returns a TreeMap.</summary>
		/// <remarks>
		/// Return a MapFactory that returns a TreeMap.
		/// <i>Implementation note: This method uses the same trick as the methods
		/// like emptyMap() introduced in the Collections class in JDK1.5 where
		/// callers can call this method with apparent type safety because this
		/// method takes the hit for the cast.
		/// </remarks>
		/// <returns>A MapFactory that makes an TreeMap.</returns>
		public static Edu.Stanford.Nlp.Util.MapFactory<K, V> TreeMapFactory<K, V>()
		{
			return TreeMapFactory;
		}

		/// <summary>Return a MapFactory that returns a TreeMap with the given Comparator.</summary>
		public static Edu.Stanford.Nlp.Util.MapFactory<K, V> TreeMapFactory<K, V, _T2>(IComparator<_T2> comparator)
		{
			return new MapFactory.TreeMapFactory<K, V>(comparator);
		}

		/// <summary>Return a MapFactory that returns an LinkedHashMap.</summary>
		/// <remarks>
		/// Return a MapFactory that returns an LinkedHashMap.
		/// <i>Implementation note: This method uses the same trick as the methods
		/// like emptyMap() introduced in the Collections class in JDK1.5 where
		/// callers can call this method with apparent type safety because this
		/// method takes the hit for the cast.
		/// </remarks>
		/// <returns>A MapFactory that makes an LinkedHashMap.</returns>
		public static Edu.Stanford.Nlp.Util.MapFactory<K, V> LinkedHashMapFactory<K, V>()
		{
			return LinkedHashMapFactory;
		}

		/// <summary>Return a MapFactory that returns an ArrayMap.</summary>
		/// <remarks>
		/// Return a MapFactory that returns an ArrayMap.
		/// <i>Implementation note: This method uses the same trick as the methods
		/// like emptyMap() introduced in the Collections class in JDK1.5 where
		/// callers can call this method with apparent type safety because this
		/// method takes the hit for the cast.
		/// </remarks>
		/// <returns>A MapFactory that makes an ArrayMap.</returns>
		public static Edu.Stanford.Nlp.Util.MapFactory<K, V> ArrayMapFactory<K, V>()
		{
			return ArrayMapFactory;
		}

		[System.Serializable]
		private class HashMapFactory<K, V> : MapFactory<K, V>
		{
			private const long serialVersionUID = -9222344631596580863L;

			public override IDictionary<K, V> NewMap()
			{
				return Generics.NewHashMap();
			}

			public override IDictionary<K, V> NewMap(int initCapacity)
			{
				return Generics.NewHashMap(initCapacity);
			}

			public override ICollection<K> NewSet()
			{
				return Generics.NewHashSet();
			}

			public override ICollection<K> NewSet(ICollection<K> init)
			{
				return Generics.NewHashSet(init);
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map)
			{
				map = Generics.NewHashMap();
				return map;
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map, int initCapacity)
			{
				map = Generics.NewHashMap(initCapacity);
				return map;
			}
		}

		[System.Serializable]
		private class IdentityHashMapFactory<K, V> : MapFactory<K, V>
		{
			private const long serialVersionUID = -9222344631596580863L;

			// end class HashMapFactory
			public override IDictionary<K, V> NewMap()
			{
				return new IdentityHashMap<K, V>();
			}

			public override IDictionary<K, V> NewMap(int initCapacity)
			{
				return new IdentityHashMap<K, V>(initCapacity);
			}

			public override ICollection<K> NewSet()
			{
				return Java.Util.Collections.NewSetFromMap(new IdentityHashMap<K, bool>());
			}

			public override ICollection<K> NewSet(ICollection<K> init)
			{
				ICollection<K> set = Java.Util.Collections.NewSetFromMap(new IdentityHashMap<K, bool>());
				// nothing more efficient to be done here...
				Sharpen.Collections.AddAll(set, init);
				return set;
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map)
			{
				map = new IdentityHashMap<K1, V1>();
				return map;
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map, int initCapacity)
			{
				map = new IdentityHashMap<K1, V1>(initCapacity);
				return map;
			}
		}

		[System.Serializable]
		private class WeakHashMapFactory<K, V> : MapFactory<K, V>
		{
			private const long serialVersionUID = 4790014244304941000L;

			// end class IdentityHashMapFactory
			public override IDictionary<K, V> NewMap()
			{
				return new WeakHashMap<K, V>();
			}

			public override IDictionary<K, V> NewMap(int initCapacity)
			{
				return new WeakHashMap<K, V>(initCapacity);
			}

			public override ICollection<K> NewSet()
			{
				return Java.Util.Collections.NewSetFromMap(new WeakHashMap<K, bool>());
			}

			public override ICollection<K> NewSet(ICollection<K> init)
			{
				ICollection<K> set = Java.Util.Collections.NewSetFromMap(new WeakHashMap<K, bool>());
				Sharpen.Collections.AddAll(set, init);
				return set;
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map)
			{
				map = new WeakHashMap<K1, V1>();
				return map;
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map, int initCapacity)
			{
				map = new WeakHashMap<K1, V1>(initCapacity);
				return map;
			}
		}

		[System.Serializable]
		private class TreeMapFactory<K, V> : MapFactory<K, V>
		{
			private const long serialVersionUID = -9138736068025818670L;

			private readonly IComparator<K> comparator;

			public TreeMapFactory()
			{
				// end class WeakHashMapFactory
				this.comparator = null;
			}

			public TreeMapFactory(IComparator<K> comparator)
			{
				this.comparator = comparator;
			}

			public override IDictionary<K, V> NewMap()
			{
				return comparator == null ? new SortedDictionary<K, V>() : new SortedDictionary<K, V>(comparator);
			}

			public override IDictionary<K, V> NewMap(int initCapacity)
			{
				return NewMap();
			}

			public override ICollection<K> NewSet()
			{
				return comparator == null ? new TreeSet<K>() : new TreeSet<K>(comparator);
			}

			public override ICollection<K> NewSet(ICollection<K> init)
			{
				return new TreeSet<K>(init);
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map)
			{
				if (comparator == null)
				{
					throw new NotSupportedException();
				}
				map = new SortedDictionary<K1, V1>();
				return map;
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map, int initCapacity)
			{
				if (comparator == null)
				{
					throw new NotSupportedException();
				}
				map = new SortedDictionary<K1, V1>();
				return map;
			}
		}

		[System.Serializable]
		private class LinkedHashMapFactory<K, V> : MapFactory<K, V>
		{
			private const long serialVersionUID = -9138736068025818671L;

			// end class TreeMapFactory
			public override IDictionary<K, V> NewMap()
			{
				return new LinkedHashMap<K, V>();
			}

			public override IDictionary<K, V> NewMap(int initCapacity)
			{
				return NewMap();
			}

			public override ICollection<K> NewSet()
			{
				return new LinkedHashSet<K>();
			}

			public override ICollection<K> NewSet(ICollection<K> init)
			{
				return new LinkedHashSet<K>(init);
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map)
			{
				map = new LinkedHashMap<K1, V1>();
				return map;
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map, int initCapacity)
			{
				map = new LinkedHashMap<K1, V1>();
				return map;
			}
		}

		[System.Serializable]
		private class ArrayMapFactory<K, V> : MapFactory<K, V>
		{
			private const long serialVersionUID = -5855812734715185523L;

			// end class LinkedHashMapFactory
			public override IDictionary<K, V> NewMap()
			{
				return new ArrayMap<K, V>();
			}

			public override IDictionary<K, V> NewMap(int initCapacity)
			{
				return new ArrayMap<K, V>(initCapacity);
			}

			public override ICollection<K> NewSet()
			{
				return new ArraySet<K>();
			}

			public override ICollection<K> NewSet(ICollection<K> init)
			{
				return new ArraySet<K>();
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map)
			{
				return new ArrayMap<K1, V1>();
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map, int initCapacity)
			{
				map = new ArrayMap<K1, V1>(initCapacity);
				return map;
			}
		}

		[System.Serializable]
		private class ConcurrentMapFactory<K, V> : MapFactory<K, V>
		{
			private const long serialVersionUID = -5855812734715185523L;

			// end class ArrayMapFactory
			public override IDictionary<K, V> NewMap()
			{
				return new ConcurrentHashMap<K, V>();
			}

			public override IDictionary<K, V> NewMap(int initCapacity)
			{
				return new ConcurrentHashMap<K, V>(initCapacity);
			}

			public override ICollection<K> NewSet()
			{
				return Java.Util.Collections.NewSetFromMap(new ConcurrentHashMap<K, bool>());
			}

			public override ICollection<K> NewSet(ICollection<K> init)
			{
				ICollection<K> set = Java.Util.Collections.NewSetFromMap(new ConcurrentHashMap<K, bool>());
				Sharpen.Collections.AddAll(set, init);
				return set;
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map)
			{
				return new ConcurrentHashMap<K1, V1>();
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map, int initCapacity)
			{
				map = new ConcurrentHashMap<K1, V1>(initCapacity);
				return map;
			}
		}

		// end class ConcurrentMapFactory
		/// <summary>Returns a new non-parameterized map of a particular sort.</summary>
		/// <returns>A new non-parameterized map of a particular sort</returns>
		public abstract IDictionary<K, V> NewMap();

		/// <summary>Returns a new non-parameterized map of a particular sort with an initial capacity.</summary>
		/// <param name="initCapacity">initial capacity of the map</param>
		/// <returns>A new non-parameterized map of a particular sort with an initial capacity</returns>
		public abstract IDictionary<K, V> NewMap(int initCapacity);

		/// <summary>
		/// A set with the same
		/// <c>K</c>
		/// parameterization of the Maps.
		/// </summary>
		public abstract ICollection<K> NewSet();

		/// <summary>
		/// A set with the same
		/// <c>K</c>
		/// parameterization, but initialized to the given collection.
		/// </summary>
		public abstract ICollection<K> NewSet(ICollection<K> init);

		/// <summary>A method to get a parameterized (genericized) map out.</summary>
		/// <param name="map">
		/// A type-parameterized
		/// <see cref="System.Collections.IDictionary{K, V}"/>
		/// argument
		/// </param>
		/// <returns>
		/// A
		/// <see cref="System.Collections.IDictionary{K, V}"/>
		/// with type-parameterization identical to that of
		/// the argument.
		/// </returns>
		public abstract IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map);

		public abstract IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map, int initCapacity);
	}
}
