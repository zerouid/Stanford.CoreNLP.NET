using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Collection of useful static methods for working with Collections.</summary>
	/// <remarks>
	/// Collection of useful static methods for working with Collections. Includes
	/// methods to increment counts in maps and cast list/map elements to common
	/// types.
	/// </remarks>
	/// <author>Joseph Smarr (jsmarr@stanford.edu)</author>
	public class CollectionUtils
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Util.CollectionUtils));

		/// <summary>Private constructor to prevent direct instantiation.</summary>
		private CollectionUtils()
		{
		}

		// Utils for making collections out of arrays of primitive types.
		public static IList<int> AsList(int[] a)
		{
			IList<int> result = new List<int>(a.Length);
			foreach (int j in a)
			{
				result.Add(int.Parse(j));
			}
			return result;
		}

		public static IList<double> AsList(double[] a)
		{
			IList<double> result = new List<double>(a.Length);
			foreach (double v in a)
			{
				result.Add(v);
			}
			return result;
		}

		// Inverses of the above
		public static int[] AsIntArray(ICollection<int> coll)
		{
			int[] result = new int[coll.Count];
			int index = 0;
			foreach (int element in coll)
			{
				result[index] = element;
				index++;
			}
			return result;
		}

		public static double[] AsDoubleArray(ICollection<double> coll)
		{
			double[] result = new double[coll.Count];
			int index = 0;
			foreach (double element in coll)
			{
				result[index] = element;
				index++;
			}
			return result;
		}

		/// <summary>Returns a new List containing the given objects.</summary>
		[SafeVarargs]
		public static IList<T> MakeList<T>(params T[] items)
		{
			return new List<T>(Arrays.AsList(items));
		}

		/// <summary>Returns a new Set containing all the objects in the specified array.</summary>
		[SafeVarargs]
		public static ICollection<T> AsSet<T>(params T[] o)
		{
			return Generics.NewHashSet(Arrays.AsList(o));
		}

		public static ICollection<T> Intersection<T>(ICollection<T> set1, ICollection<T> set2)
		{
			ICollection<T> intersect = Generics.NewHashSet();
			foreach (T t in set1)
			{
				if (set2.Contains(t))
				{
					intersect.Add(t);
				}
			}
			return intersect;
		}

		public static ICollection<T> Union<T>(ICollection<T> set1, ICollection<T> set2)
		{
			ICollection<T> union = new List<T>();
			foreach (T t in set1)
			{
				union.Add(t);
			}
			foreach (T t_1 in set2)
			{
				union.Add(t_1);
			}
			return union;
		}

		public static ICollection<T> UnionAsSet<T>(ICollection<T> set1, ICollection<T> set2)
		{
			ICollection<T> union = Generics.NewHashSet();
			foreach (T t in set1)
			{
				union.Add(t);
			}
			foreach (T t_1 in set2)
			{
				union.Add(t_1);
			}
			return union;
		}

		[SafeVarargs]
		public static ICollection<T> UnionAsSet<T>(params ICollection<T>[] sets)
		{
			ICollection<T> union = Generics.NewHashSet();
			foreach (ICollection<T> set in sets)
			{
				foreach (T t in set)
				{
					union.Add(t);
				}
			}
			return union;
		}

		/// <summary>Returns all objects in list1 that are not in list2.</summary>
		/// <?/>
		/// <param name="list1">First collection</param>
		/// <param name="list2">Second collection</param>
		/// <returns>The collection difference list1 - list2</returns>
		public static ICollection<T> Diff<T>(ICollection<T> list1, ICollection<T> list2)
		{
			ICollection<T> diff = new List<T>();
			foreach (T t in list1)
			{
				if (!list2.Contains(t))
				{
					diff.Add(t);
				}
			}
			return diff;
		}

		/// <summary>Returns all objects in list1 that are not in list2.</summary>
		/// <?/>
		/// <param name="list1">First collection</param>
		/// <param name="list2">Second collection</param>
		/// <returns>The collection difference list1 - list2</returns>
		public static ICollection<T> DiffAsSet<T>(ICollection<T> list1, ICollection<T> list2)
		{
			ICollection<T> diff = new HashSet<T>();
			foreach (T t in list1)
			{
				if (!list2.Contains(t))
				{
					diff.Add(t);
				}
			}
			return diff;
		}

		// Utils for loading and saving Collections to/from text files
		/// <param name="filename">The path to the file to load the List from</param>
		/// <param name="c">
		/// The Class to instantiate each member of the List. Must have a
		/// String constructor.
		/// </param>
		/// <exception cref="System.Exception"/>
		public static ICollection<T> LoadCollection<T>(string filename, CollectionFactory<T> cf)
		{
			System.Type c = typeof(T);
			return LoadCollection(new File(filename), c, cf);
		}

		/// <param name="file">The file to load the List from</param>
		/// <param name="c">
		/// The Class to instantiate each member of the List. Must have a
		/// String constructor.
		/// </param>
		/// <exception cref="System.Exception"/>
		public static ICollection<T> LoadCollection<T>(File file, CollectionFactory<T> cf)
		{
			System.Type c = typeof(T);
			Constructor<T> m = c.GetConstructor(new Type[] { typeof(string) });
			ICollection<T> result = cf.NewCollection();
			BufferedReader @in = new BufferedReader(new FileReader(file));
			string line = @in.ReadLine();
			while (line != null && line.Length > 0)
			{
				try
				{
					T o = m.NewInstance(line);
					result.Add(o);
				}
				catch (Exception e)
				{
					log.Info("Couldn't build object from line: " + line);
					Sharpen.Runtime.PrintStackTrace(e);
				}
				line = @in.ReadLine();
			}
			@in.Close();
			return result;
		}

		/// <summary>Adds the items from the file to the collection.</summary>
		/// <?/>
		/// <param name="fileName">The name of the file from which items should be loaded.</param>
		/// <param name="itemClass">The class of the items (must have a constructor that accepts a String).</param>
		/// <param name="collection">The collection to which items should be added.</param>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="System.IO.IOException"/>
		public static void LoadCollection<T>(string fileName, ICollection<T> collection)
		{
			System.Type itemClass = typeof(T);
			LoadCollection(new File(fileName), itemClass, collection);
		}

		/// <summary>Adds the items from the file to the collection.</summary>
		/// <?/>
		/// <param name="file">The file from which items should be loaded.</param>
		/// <param name="itemClass">The class of the items (must have a constructor that accepts a String).</param>
		/// <param name="collection">The collection to which items should be added.</param>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="System.IO.IOException"/>
		public static void LoadCollection<T>(File file, ICollection<T> collection)
		{
			System.Type itemClass = typeof(T);
			Constructor<T> itemConstructor = itemClass.GetConstructor(typeof(string));
			BufferedReader @in = new BufferedReader(new FileReader(file));
			string line = @in.ReadLine();
			while (line != null && line.Length > 0)
			{
				T t = itemConstructor.NewInstance(line);
				collection.Add(t);
				line = @in.ReadLine();
			}
			@in.Close();
		}

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		public static IDictionary<K, V> GetMapFromString<K, V>(string s, MapFactory<K, V> mapFactory)
		{
			System.Type keyClass = typeof(K);
			System.Type valueClass = typeof(V);
			Constructor<K> keyC = keyClass.GetConstructor(new Type[] { typeof(string) });
			Constructor<V> valueC = valueClass.GetConstructor(new Type[] { typeof(string) });
			if (s[0] != '{')
			{
				throw new Exception(string.Empty);
			}
			s = Sharpen.Runtime.Substring(s, 1);
			// get rid of first brace
			string[] fields = s.Split("\\s+");
			IDictionary<K, V> m = mapFactory.NewMap();
			// populate m
			for (int i = 0; i < fields.Length; i++)
			{
				// log.info("Parsing " + fields[i]);
				fields[i] = Sharpen.Runtime.Substring(fields[i], 0, fields[i].Length - 1);
				// get rid of
				// following
				// comma or
				// brace
				string[] a = fields[i].Split("=");
				K key = keyC.NewInstance(a[0]);
				V value;
				if (a.Length > 1)
				{
					value = valueC.NewInstance(a[1]);
				}
				else
				{
					value = valueC.NewInstance(string.Empty);
				}
				m[key] = value;
			}
			return m;
		}

		/// <summary>Checks whether a Collection contains a specified Object.</summary>
		/// <remarks>
		/// Checks whether a Collection contains a specified Object. Object equality
		/// (==), rather than .equals(), is used.
		/// </remarks>
		public static bool ContainsObject<T>(ICollection<T> c, T o)
		{
			foreach (object o1 in c)
			{
				if (o == o1)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Removes the first occurrence in the list of the specified object, using
		/// object identity (==) not equality as the criterion for object presence.
		/// </summary>
		/// <remarks>
		/// Removes the first occurrence in the list of the specified object, using
		/// object identity (==) not equality as the criterion for object presence. If
		/// this list does not contain the element, it is unchanged.
		/// </remarks>
		/// <param name="l">
		/// The
		/// <see cref="System.Collections.IList{E}"/>
		/// from which to remove the object
		/// </param>
		/// <param name="o">The object to be removed.</param>
		/// <returns>Whether or not the List was changed.</returns>
		public static bool RemoveObject<T>(IList<T> l, T o)
		{
			int i = 0;
			foreach (object o1 in l)
			{
				if (o == o1)
				{
					l.Remove(i);
					return true;
				}
				else
				{
					i++;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns the index of the first occurrence in the list of the specified
		/// object, using object identity (==) not equality as the criterion for object
		/// presence.
		/// </summary>
		/// <remarks>
		/// Returns the index of the first occurrence in the list of the specified
		/// object, using object identity (==) not equality as the criterion for object
		/// presence. If this list does not contain the element, return -1.
		/// </remarks>
		/// <param name="l">
		/// The
		/// <see cref="System.Collections.IList{E}"/>
		/// to find the object in.
		/// </param>
		/// <param name="o">The sought-after object.</param>
		/// <returns>Whether or not the List was changed.</returns>
		public static int GetIndex<T>(IList<T> l, T o)
		{
			int i = 0;
			foreach (object o1 in l)
			{
				if (o == o1)
				{
					return i;
				}
				else
				{
					i++;
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns the index of the first occurrence after the startIndex (exclusive)
		/// in the list of the specified object, using object equals function.
		/// </summary>
		/// <remarks>
		/// Returns the index of the first occurrence after the startIndex (exclusive)
		/// in the list of the specified object, using object equals function. If this
		/// list does not contain the element, return -1.
		/// </remarks>
		/// <param name="l">
		/// The
		/// <see cref="System.Collections.IList{E}"/>
		/// to find the object in.
		/// </param>
		/// <param name="o">The sought-after object.</param>
		/// <param name="fromIndex">The start index</param>
		/// <returns>Whether or not the List was changed.</returns>
		public static int GetIndex<T>(IList<T> l, T o, int fromIndex)
		{
			int i = -1;
			foreach (T o1 in l)
			{
				i++;
				if (i < fromIndex)
				{
					continue;
				}
				if (o.Equals(o1))
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>Samples without replacement from a collection.</summary>
		/// <param name="c">The collection to be sampled from</param>
		/// <param name="n">The number of samples to take</param>
		/// <returns>a new collection with the sample</returns>
		public static ICollection<E> SampleWithoutReplacement<E>(ICollection<E> c, int n)
		{
			return SampleWithoutReplacement(c, n, new Random());
		}

		/// <summary>
		/// Samples without replacement from a collection, using your own
		/// <see cref="Java.Util.Random"/>
		/// number generator.
		/// </summary>
		/// <param name="c">The collection to be sampled from</param>
		/// <param name="n">The number of samples to take</param>
		/// <param name="r">The random number generator</param>
		/// <returns>a new collection with the sample</returns>
		public static ICollection<E> SampleWithoutReplacement<E>(ICollection<E> c, int n, Random r)
		{
			if (n < 0)
			{
				throw new ArgumentException("n < 0: " + n);
			}
			if (n > c.Count)
			{
				throw new ArgumentException("n > size of collection: " + n + ", " + c.Count);
			}
			IList<E> copy = new List<E>(c.Count);
			Sharpen.Collections.AddAll(copy, c);
			ICollection<E> result = new List<E>(n);
			for (int k = 0; k < n; k++)
			{
				double d = r.NextDouble();
				int x = (int)(d * copy.Count);
				result.Add(copy.Remove(x));
			}
			return result;
		}

		public static E Sample<E>(IList<E> l, Random r)
		{
			int i = r.NextInt(l.Count);
			return l[i];
		}

		/// <summary>Samples with replacement from a collection.</summary>
		/// <param name="c">The collection to be sampled from</param>
		/// <param name="n">The number of samples to take</param>
		/// <returns>a new collection with the sample</returns>
		public static ICollection<E> SampleWithReplacement<E>(ICollection<E> c, int n)
		{
			return SampleWithReplacement(c, n, new Random());
		}

		/// <summary>
		/// Samples with replacement from a collection, using your own
		/// <see cref="Java.Util.Random"/>
		/// number generator.
		/// </summary>
		/// <param name="c">The collection to be sampled from</param>
		/// <param name="n">The number of samples to take</param>
		/// <param name="r">The random number generator</param>
		/// <returns>a new collection with the sample</returns>
		public static ICollection<E> SampleWithReplacement<E>(ICollection<E> c, int n, Random r)
		{
			if (n < 0)
			{
				throw new ArgumentException("n < 0: " + n);
			}
			IList<E> copy = new List<E>(c.Count);
			Sharpen.Collections.AddAll(copy, c);
			ICollection<E> result = new List<E>(n);
			for (int k = 0; k < n; k++)
			{
				double d = r.NextDouble();
				int x = (int)(d * copy.Count);
				result.Add(copy[x]);
			}
			return result;
		}

		/// <summary>
		/// Returns true iff l1 is a sublist of l (i.e., every member of l1 is in l,
		/// and for every e1 &lt; e2 in l1, there is an e1 &lt; e2 occurrence in l).
		/// </summary>
		public static bool IsSubList<T, _T1>(IList<T> l1, IList<_T1> l)
		{
			IEnumerator<T> it = l.GetEnumerator();
			foreach (T o1 in l1)
			{
				if (!it.MoveNext())
				{
					return false;
				}
				object o = it.Current;
				while ((o == null && !(o1 == null)) || (o != null && !o.Equals(o1)))
				{
					if (!it.MoveNext())
					{
						return false;
					}
					o = it.Current;
				}
			}
			return true;
		}

		public static string ToVerticalString<K, V>(IDictionary<K, V> m)
		{
			StringBuilder b = new StringBuilder();
			ICollection<KeyValuePair<K, V>> entries = m;
			foreach (KeyValuePair<K, V> e in entries)
			{
				b.Append(e.Key).Append('=').Append(e.Value).Append('\n');
			}
			return b.ToString();
		}

		/// <summary>Provides a consistent ordering over lists.</summary>
		/// <remarks>
		/// Provides a consistent ordering over lists. First compares by the first
		/// element. If that element is equal, the next element is considered, and so
		/// on.
		/// </remarks>
		public static int CompareLists<T>(IList<T> list1, IList<T> list2)
			where T : IComparable<T>
		{
			if (list1 == null && list2 == null)
			{
				return 0;
			}
			if (list1 == null || list2 == null)
			{
				throw new ArgumentException();
			}
			int size1 = list1.Count;
			int size2 = list2.Count;
			int size = Math.Min(size1, size2);
			for (int i = 0; i < size; i++)
			{
				int c = list1[i].CompareTo(list2[i]);
				if (c != 0)
				{
					return c;
				}
			}
			if (size1 < size2)
			{
				return -1;
			}
			if (size1 > size2)
			{
				return 1;
			}
			return 0;
		}

		public static IComparator<IList<C>> GetListComparator<C>()
			where C : IComparable<C>
		{
			return null;
		}

		/// <summary>Return the items of an Iterable as a sorted list.</summary>
		/// <?/>
		/// <param name="items">The collection to be sorted.</param>
		/// <returns>A list containing the same items as the Iterable, but sorted.</returns>
		public static IList<T> Sorted<T>(IEnumerable<T> items)
			where T : IComparable<T>
		{
			IList<T> result = ToList(items);
			result.Sort();
			return result;
		}

		/// <summary>Return the items of an Iterable as a sorted list.</summary>
		/// <?/>
		/// <param name="items">The collection to be sorted.</param>
		/// <returns>A list containing the same items as the Iterable, but sorted.</returns>
		public static IList<T> Sorted<T>(IEnumerable<T> items, IComparator<T> comparator)
		{
			IList<T> result = ToList(items);
			result.Sort(comparator);
			return result;
		}

		/// <summary>Create a list out of the items in the Iterable.</summary>
		/// <?/>
		/// <param name="items">The items to be made into a list.</param>
		/// <returns>A list consisting of the items of the Iterable, in the same order.</returns>
		public static IList<T> ToList<T>(IEnumerable<T> items)
		{
			IList<T> list = new List<T>();
			AddAll(list, items);
			return list;
		}

		/// <summary>Create a set out of the items in the Iterable.</summary>
		/// <?/>
		/// <param name="items">The items to be made into a set.</param>
		/// <returns>A set consisting of the items from the Iterable.</returns>
		public static ICollection<T> ToSet<T>(IEnumerable<T> items)
		{
			ICollection<T> set = Generics.NewHashSet();
			AddAll(set, items);
			return set;
		}

		/// <summary>Add all the items from an iterable to a collection.</summary>
		/// <?/>
		/// <param name="collection">The collection to which the items should be added.</param>
		/// <param name="items">The items to add to the collection.</param>
		public static void AddAll<T, _T1>(ICollection<T> collection, IEnumerable<_T1> items)
			where _T1 : T
		{
			foreach (T item in items)
			{
				collection.Add(item);
			}
		}

		/// <summary>Get all sub-lists of the given list of the given sizes.</summary>
		/// <remarks>
		/// Get all sub-lists of the given list of the given sizes.
		/// For example:
		/// <pre>
		/// List&lt;String&gt; items = Arrays.asList(&quot;a&quot;, &quot;b&quot;, &quot;c&quot;, &quot;d&quot;);
		/// System.out.println(CollectionUtils.getNGrams(items, 1, 2));
		/// </pre>
		/// would print out:
		/// <pre>
		/// [[a], [a, b], [b], [b, c], [c], [c, d], [d]]
		/// </pre>
		/// </remarks>
		/// <?/>
		/// <param name="items">The list of items.</param>
		/// <param name="minSize">The minimum size of an ngram.</param>
		/// <param name="maxSize">The maximum size of an ngram.</param>
		/// <returns>All sub-lists of the given sizes.</returns>
		public static IList<IList<T>> GetNGrams<T>(IList<T> items, int minSize, int maxSize)
		{
			IList<IList<T>> ngrams = new List<IList<T>>();
			int listSize = items.Count;
			for (int i = 0; i < listSize; ++i)
			{
				for (int ngramSize = minSize; ngramSize <= maxSize; ++ngramSize)
				{
					if (i + ngramSize <= listSize)
					{
						IList<T> ngram = new List<T>();
						for (int j = i; j < i + ngramSize; ++j)
						{
							ngram.Add(items[j]);
						}
						ngrams.Add(ngram);
					}
				}
			}
			return ngrams;
		}

		/// <summary>Get all prefix/suffix combinations from a list.</summary>
		/// <remarks>
		/// Get all prefix/suffix combinations from a list. It can extract just
		/// prefixes, just suffixes, or prefixes and suffixes of the same length.
		/// For example:
		/// <pre>
		/// List&lt;String&gt; items = Arrays.asList(&quot;a&quot;, &quot;b&quot;, &quot;c&quot;, &quot;d&quot;);
		/// System.out.println(CollectionUtils.getPrefixesAndSuffixes(items, 1, 2, null, true, true));
		/// </pre>
		/// would print out:
		/// <pre>
		/// [[d], [a], [a, d], [d, c], [a, b], [a, b, c, d]]
		/// </pre>
		/// and
		/// <pre>
		/// List&lt;String&gt; items2 = Arrays.asList(&quot;a&quot;);
		/// System.out.println(CollectionUtils.getPrefixesAndSuffixes(items2, 1, 2, null, true, true));
		/// </pre>
		/// would print:
		/// <pre>
		/// [[a], [a], [a, a], [a, null], [a, null], [a, null, a, null]]
		/// </pre>
		/// </remarks>
		/// <?/>
		/// <param name="items">The list of items.</param>
		/// <param name="minSize">The minimum length of a prefix/suffix span (should be at least 1)</param>
		/// <param name="maxSize">The maximum length of a prefix/suffix span</param>
		/// <param name="paddingSymbol">
		/// Symbol to be included if we run out of bounds (e.g. if items has
		/// size 3 and we try to extract a span of length 4).
		/// </param>
		/// <param name="includePrefixes">Whether to extract prefixes</param>
		/// <param name="includeSuffixes">Whether to extract suffixes</param>
		/// <returns>All prefix/suffix combinations of the given sizes.</returns>
		public static IList<IList<T>> GetPrefixesAndSuffixes<T>(IList<T> items, int minSize, int maxSize, T paddingSymbol, bool includePrefixes, bool includeSuffixes)
		{
			System.Diagnostics.Debug.Assert(minSize > 0);
			System.Diagnostics.Debug.Assert(maxSize >= minSize);
			System.Diagnostics.Debug.Assert(includePrefixes || includeSuffixes);
			IList<IList<T>> prefixesAndSuffixes = new List<IList<T>>();
			for (int span = minSize - 1; span < maxSize; span++)
			{
				IList<int> indices = new List<int>();
				IList<T> seq = new List<T>();
				if (includePrefixes)
				{
					for (int i = 0; i <= span; i++)
					{
						indices.Add(i);
					}
				}
				if (includeSuffixes)
				{
					int maxIndex = items.Count - 1;
					for (int i = span; i >= 0; i--)
					{
						indices.Add(maxIndex - i);
					}
				}
				foreach (int i_1 in indices)
				{
					try
					{
						seq.Add(items[i_1]);
					}
					catch (IndexOutOfRangeException)
					{
						seq.Add(paddingSymbol);
					}
				}
				prefixesAndSuffixes.Add(seq);
			}
			return prefixesAndSuffixes;
		}

		public static IList<T> MergeList<T, M, _T2>(IList<_T2> list, ICollection<M> matched, IFunction<M, Interval<int>> toIntervalFunc, IFunction<IList<T>, T> aggregator)
			where _T2 : T
		{
			IList<Interval<int>> matchedIntervals = new List<Interval<int>>(matched.Count);
			foreach (M m in matched)
			{
				matchedIntervals.Add(toIntervalFunc.Apply(m));
			}
			return MergeList(list, matchedIntervals, aggregator);
		}

		public static IList<T> MergeList<T, _T1, _T2>(IList<_T1> list, IList<_T2> matched, IFunction<IList<T>, T> aggregator)
			where _T1 : T
			where _T2 : IHasInterval<int>
		{
			matched.Sort(HasIntervalConstants.EndpointsComparator);
			return MergeListWithSortedMatched(list, matched, aggregator);
		}

		public static IList<T> MergeListWithSortedMatched<T, _T1, _T2>(IList<_T1> list, IList<_T2> matched, IFunction<IList<T>, T> aggregator)
			where _T1 : T
			where _T2 : IHasInterval<int>
		{
			IList<T> merged = new List<T>(list.Count);
			// Approximate size
			int last = 0;
			foreach (IHasInterval<int> m in matched)
			{
				Interval<int> interval = m.GetInterval();
				int start = interval.GetBegin();
				int end = interval.GetEnd();
				if (start >= last)
				{
					Sharpen.Collections.AddAll(merged, list.SubList(last, start));
					T t = aggregator.Apply(list.SubList(start, end));
					merged.Add(t);
					last = end;
				}
			}
			// Add rest of elements
			if (last < list.Count)
			{
				Sharpen.Collections.AddAll(merged, list.SubList(last, list.Count));
			}
			return merged;
		}

		public static IList<T> MergeListWithSortedMatchedPreAggregated<T, _T1, _T2>(IList<_T1> list, IList<_T2> matched, IFunction<T, Interval<int>> toIntervalFunc)
			where _T1 : T
			where _T2 : T
		{
			IList<T> merged = new List<T>(list.Count);
			// Approximate size
			int last = 0;
			foreach (T m in matched)
			{
				Interval<int> interval = toIntervalFunc.Apply(m);
				int start = interval.GetBegin();
				int end = interval.GetEnd();
				if (start >= last)
				{
					Sharpen.Collections.AddAll(merged, list.SubList(last, start));
					merged.Add(m);
					last = end;
				}
			}
			// Add rest of elements
			if (last < list.Count)
			{
				Sharpen.Collections.AddAll(merged, list.SubList(last, list.Count));
			}
			return merged;
		}

		/// <summary>Combines all the lists in a collection to a single list.</summary>
		public static IList<T> Flatten<T>(ICollection<IList<T>> nestedList)
		{
			IList<T> result = new List<T>();
			foreach (IList<T> list in nestedList)
			{
				Sharpen.Collections.AddAll(result, list);
			}
			return result;
		}

		/// <summary>
		/// Makes it possible to uniquify a collection of objects which are normally
		/// non-hashable.
		/// </summary>
		/// <remarks>
		/// Makes it possible to uniquify a collection of objects which are normally
		/// non-hashable. Alternatively, it lets you define an alternate hash function
		/// for them for limited-use hashing.
		/// </remarks>
		public static ICollection<ObjType> UniqueNonhashableObjects<ObjType, Hashable>(ICollection<ObjType> objects, IFunction<ObjType, Hashable> customHasher)
		{
			IDictionary<Hashable, ObjType> hashesToObjects = Generics.NewHashMap();
			foreach (ObjType @object in objects)
			{
				hashesToObjects[customHasher.Apply(@object)] = @object;
			}
			return hashesToObjects.Values;
		}

		/// <summary>if any item in toCheck is present in collection</summary>
		/// <param name="collection"/>
		/// <param name="toCheck"/>
		public static bool ContainsAny<T>(ICollection<T> collection, ICollection<T> toCheck)
		{
			foreach (T c in toCheck)
			{
				if (collection.Contains(c))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Split a list into numFolds (roughly) equally sized folds.</summary>
		/// <remarks>
		/// Split a list into numFolds (roughly) equally sized folds. The earlier folds
		/// may have one more item in them than later folds.
		/// <br />
		/// The lists returned are subList()s of the original list.
		/// Therefore, don't try to modify the sublists, and don't modify the
		/// original list while the sublists are in use.
		/// </remarks>
		public static IList<IList<T>> PartitionIntoFolds<T>(IList<T> values, int numFolds)
		{
			IList<IList<T>> folds = Generics.NewArrayList();
			int numValues = values.Count;
			int foldSize = numValues / numFolds;
			int remainder = numValues % numFolds;
			int start = 0;
			int end = foldSize;
			for (int foldNum = 0; foldNum < numFolds; foldNum++)
			{
				// if we're in the first 'remainder' folds, we get an extra item
				if (foldNum < remainder)
				{
					end++;
				}
				folds.Add(values.SubList(start, end));
				start = end;
				end += foldSize;
			}
			return folds;
		}

		/// <summary>Split a list into train, test pairs for use in k-fold crossvalidation.</summary>
		/// <remarks>
		/// Split a list into train, test pairs for use in k-fold crossvalidation. This
		/// returns a list of numFold (train, test) pairs where each train list will
		/// contain (numFolds-1)/numFolds of the original values and the test list will
		/// contain the remaining 1/numFolds of the original values.
		/// </remarks>
		public static ICollection<Pair<ICollection<T>, ICollection<T>>> TrainTestFoldsForCV<T>(IList<T> values, int numFolds)
		{
			ICollection<Pair<ICollection<T>, ICollection<T>>> trainTestPairs = new List<Pair<ICollection<T>, ICollection<T>>>();
			IList<IList<T>> folds = PartitionIntoFolds(values, numFolds);
			for (int splitNum = 0; splitNum < numFolds; splitNum++)
			{
				ICollection<T> test = folds[splitNum];
				ICollection<T> train = new List<T>();
				for (int foldNum = 0; foldNum < numFolds; foldNum++)
				{
					if (foldNum != splitNum)
					{
						Sharpen.Collections.AddAll(train, folds[foldNum]);
					}
				}
				trainTestPairs.Add(new Pair<ICollection<T>, ICollection<T>>(train, test));
			}
			return trainTestPairs;
		}

		/// <summary>Returns a list of all modes in the Collection.</summary>
		/// <remarks>
		/// Returns a list of all modes in the Collection.  (If the Collection has multiple items with the
		/// highest frequency, all of them will be returned.)
		/// </remarks>
		public static ICollection<T> Modes<T>(ICollection<T> values)
		{
			ICounter<T> counter = new ClassicCounter<T>(values);
			IList<double> sortedCounts = Edu.Stanford.Nlp.Util.CollectionUtils.Sorted(counter.Values());
			double highestCount = sortedCounts[sortedCounts.Count - 1];
			Counters.RetainAbove(counter, highestCount);
			return counter.KeySet();
		}

		/// <summary>Returns the mode in the Collection.</summary>
		/// <remarks>
		/// Returns the mode in the Collection.  If the Collection has multiple modes, this method picks one
		/// arbitrarily.
		/// </remarks>
		public static T Mode<T>(ICollection<T> values)
		{
			ICollection<T> modes = Modes(values);
			return modes.GetEnumerator().Current;
		}

		/// <summary>Transforms the keyset of collection according to the given Function and returns a set of the keys.</summary>
		public static ICollection<T2> TransformAsSet<T1, T2, _T2>(ICollection<_T2> original, IFunction<T1, T2> f)
			where _T2 : T1
		{
			ICollection<T2> transformed = Generics.NewHashSet();
			foreach (T1 t in original)
			{
				transformed.Add(f.Apply(t));
			}
			return transformed;
		}

		/// <summary>Transforms the keyset of collection according to the given Function and returns a list.</summary>
		public static IList<T2> TransformAsList<T1, T2, _T2>(ICollection<_T2> original, IFunction<T1, T2> f)
			where _T2 : T1
		{
			IList<T2> transformed = new List<T2>();
			foreach (T1 t in original)
			{
				transformed.Add(f.Apply(t));
			}
			return transformed;
		}

		/// <summary>Filters the objects in the collection according to the given Filter and returns a list.</summary>
		public static IList<T> FilterAsList<T, _T1, _T2>(ICollection<_T1> original, IPredicate<_T2> f)
			where _T1 : T
		{
			IList<T> transformed = new List<T>();
			foreach (T t in original)
			{
				if (f.Test(t))
				{
					transformed.Add(t);
				}
			}
			return transformed;
		}

		/// <summary>Get all values corresponding to the indices (if they exist in the map).</summary>
		/// <param name="map">Any map from T to V</param>
		/// <param name="indices">A collection of indices of type T</param>
		/// <returns>The corresponding list of values of type V</returns>
		public static IList<V> GetAll<T, V>(IDictionary<T, V> map, ICollection<T> indices)
		{
			IList<V> result = new List<V>();
			foreach (T i in indices)
			{
				if (map.Contains(i))
				{
					result.Add(map[i]);
				}
			}
			return result;
		}

		public static int MaxIndex<T>(IList<T> list)
			where T : IComparable<T>
		{
			T max = null;
			int i = 0;
			int maxIndex = -1;
			foreach (T t in list)
			{
				if (max == null || t.CompareTo(max) > 0)
				{
					max = t;
					maxIndex = i;
				}
				i++;
			}
			return maxIndex;
		}

		/// <summary>Concatenate a number of iterators together, to form one big iterator.</summary>
		/// <remarks>
		/// Concatenate a number of iterators together, to form one big iterator.
		/// This should respect the remove() functionality of the constituent iterators.
		/// </remarks>
		/// <param name="iterators">The iterators to concatenate.</param>
		/// <?/>
		/// <returns>An iterator consisting of all the component iterators concatenated together in order.</returns>
		[SafeVarargs]
		public static IEnumerator<E> ConcatIterators<E>(params IEnumerator<E>[] iterators)
		{
			return new _IEnumerator_910(iterators);
		}

		private sealed class _IEnumerator_910 : IEnumerator<E>
		{
			public _IEnumerator_910(IEnumerator<E>[] iterators)
			{
				this.iterators = iterators;
				this.lastIter = null;
				this.iters = new LinkedList<IEnumerator<E>>(Arrays.AsList(iterators));
			}

			internal IEnumerator<E> lastIter;

			internal IList<IEnumerator<E>> iters;

			public bool MoveNext()
			{
				return !this.iters.IsEmpty() && this.iters[0].MoveNext();
			}

			public E Current
			{
				get
				{
					if (!this.MoveNext())
					{
						throw new ArgumentException("Iterator is empty!");
					}
					E next = this.iters[0].Current;
					this.lastIter = this.iters[0];
					while (!this.iters.IsEmpty() && !this.iters[0].MoveNext())
					{
						this.iters.Remove(0);
					}
					return next;
				}
			}

			public void Remove()
			{
				if (this.lastIter == null)
				{
					throw new InvalidOperationException("Call next() before calling remove()!");
				}
				this.lastIter.Remove();
			}

			private readonly IEnumerator<E>[] iterators;
		}

		public static IEnumerator<E> IteratorFromEnumerator<E>(IEnumeration<E> lst_)
		{
			return new _IEnumerator_940(lst_);
		}

		private sealed class _IEnumerator_940 : IEnumerator<E>
		{
			public _IEnumerator_940(IEnumeration<E> lst_)
			{
				this.lst_ = lst_;
				this.lst = lst_;
			}

			private readonly IEnumeration<E> lst;

			public bool MoveNext()
			{
				return this.lst.MoveNext();
			}

			public E Current
			{
				get
				{
					return this.lst.Current;
				}
			}

			private readonly IEnumeration<E> lst_;
		}

		public static IEnumerable<E> IterableFromEnumerator<E>(IEnumeration<E> lst)
		{
			return new IterableIterator<E>(IteratorFromEnumerator(lst));
		}
	}
}
