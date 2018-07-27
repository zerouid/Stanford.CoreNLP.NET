using System.Collections.Generic;




namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Utilities for Maps, including inverting, composing, and support for list/set values.</summary>
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	public class Maps
	{
		private Maps()
		{
		}

		/// <summary>Adds the value to the HashSet given by map.get(key), creating a new HashMap if needed.</summary>
		public static void PutIntoValueHashSet<K, V>(IDictionary<K, ICollection<V>> map, K key, V value)
		{
			CollectionFactory<V> factory = CollectionFactory.HashSetFactory();
			PutIntoValueCollection(map, key, value, factory);
		}

		/// <summary>Adds the value to the ArrayList given by map.get(key), creating a new ArrayList if needed.</summary>
		public static void PutIntoValueArrayList<K, V>(IDictionary<K, IList<V>> map, K key, V value)
		{
			CollectionFactory<V> factory = CollectionFactory.ArrayListFactory();
			PutIntoValueCollection(map, key, value, factory);
		}

		/// <summary>Adds the value to the collection given by map.get(key).</summary>
		/// <remarks>Adds the value to the collection given by map.get(key).  A new collection is created using the supplied CollectionFactory.</remarks>
		public static void PutIntoValueCollection<K, V, C>(IDictionary<K, C> map, K key, V value, CollectionFactory<V> cf)
			where C : ICollection<V>
		{
			C c = map[key];
			if (c == null)
			{
				c = ErasureUtils.UncheckedCast<C>(cf.NewCollection());
				map[key] = c;
			}
			c.Add(value);
		}

		/// <summary>Compose two maps map1:x-&gt;y and map2:y-&gt;z to get a map x-&gt;z</summary>
		/// <returns>The composed map</returns>
		public static IDictionary<X, Z> Compose<X, Y, Z>(IDictionary<X, Y> map1, IDictionary<Y, Z> map2)
		{
			IDictionary<X, Z> composedMap = Generics.NewHashMap();
			foreach (X key in map1.Keys)
			{
				composedMap[key] = map2[map1[key]];
			}
			return composedMap;
		}

		/// <summary>Inverts a map x-&gt;y to a map y-&gt;x assuming unique preimages.</summary>
		/// <remarks>Inverts a map x-&gt;y to a map y-&gt;x assuming unique preimages.  If they are not unique, you get an arbitrary ones as the values in the inverted map.</remarks>
		/// <returns>The inverted map</returns>
		public static IDictionary<Y, X> Invert<X, Y>(IDictionary<X, Y> map)
		{
			IDictionary<Y, X> invertedMap = Generics.NewHashMap();
			foreach (KeyValuePair<X, Y> entry in map)
			{
				X key = entry.Key;
				Y value = entry.Value;
				invertedMap[value] = key;
			}
			return invertedMap;
		}

		/// <summary>Inverts a map x-&gt;y to a map y-&gt;pow(x) not assuming unique preimages.</summary>
		/// <returns>The inverted set</returns>
		public static IDictionary<Y, ICollection<X>> InvertSet<X, Y>(IDictionary<X, Y> map)
		{
			IDictionary<Y, ICollection<X>> invertedMap = Generics.NewHashMap();
			foreach (KeyValuePair<X, Y> entry in map)
			{
				X key = entry.Key;
				Y value = entry.Value;
				PutIntoValueHashSet(invertedMap, value, key);
			}
			return invertedMap;
		}

		/// <summary>Sorts a list of entries.</summary>
		/// <remarks>Sorts a list of entries.  This method is here since the entries might come from a Counter.</remarks>
		public static IList<KeyValuePair<K, V>> SortedEntries<K, V>(ICollection<KeyValuePair<K, V>> entries)
			where K : IComparable<K>
		{
			IList<KeyValuePair<K, V>> entriesList = new List<KeyValuePair<K, V>>(entries);
			entriesList.Sort(null);
			return entriesList;
		}

		/// <summary>Returns a List of entries in the map, sorted by key.</summary>
		public static IList<KeyValuePair<K, V>> SortedEntries<K, V>(IDictionary<K, V> map)
			where K : IComparable<K>
		{
			return SortedEntries(map);
		}

		/// <summary>Stringifies a Map in a stable fashion.</summary>
		public static void ToStringSorted<K, V>(IDictionary<K, V> map, StringBuilder builder)
			where K : IComparable<K>
		{
			builder.Append("{");
			IList<KeyValuePair<K, V>> sortedProperties = Edu.Stanford.Nlp.Util.Maps.SortedEntries(map);
			int index = 0;
			foreach (KeyValuePair<K, V> entry in sortedProperties)
			{
				if (index > 0)
				{
					builder.Append(", ");
				}
				builder.Append(entry.Key).Append("=").Append(entry.Value);
				index++;
			}
			builder.Append("}");
		}

		/// <summary>Stringifies a Map in a stable fashion.</summary>
		public static string ToStringSorted<K, V>(IDictionary<K, V> map)
			where K : IComparable<K>
		{
			StringBuilder builder = new StringBuilder();
			ToStringSorted(map, builder);
			return builder.ToString();
		}

		/// <summary>Removes keys from the map</summary>
		public static void RemoveKeys<K, V>(IDictionary<K, V> map, ICollection<K> removekeys)
		{
			foreach (K k in removekeys)
			{
				Sharpen.Collections.Remove(map, k);
			}
		}

		/// <summary>
		/// Adds all of the keys in <code>from</code> to <code>to</code>,
		/// applying <code>function</code> to the values to transform them
		/// from <code>V2</code> to <code>V1</code>.
		/// </summary>
		public static void AddAll<K, V1, V2>(IDictionary<K, V1> to, IDictionary<K, V2> from, IFunction<V2, V1> function)
		{
			foreach (KeyValuePair<K, V2> entry in from)
			{
				to[entry.Key] = function.Apply(entry.Value);
			}
		}

		/// <summary>get all values corresponding to the indices (if they exist in the map)</summary>
		/// <param name="map"/>
		/// <param name="indices"/>
		/// <returns>a submap corresponding to the indices</returns>
		public static IDictionary<T, V> GetAll<T, V>(IDictionary<T, V> map, ICollection<T> indices)
		{
			IDictionary<T, V> result = new Dictionary<T, V>();
			foreach (T i in indices)
			{
				if (map.Contains(i))
				{
					result[i] = map[i];
				}
			}
			return result;
		}

		/// <summary>Pretty print a Counter.</summary>
		/// <remarks>
		/// Pretty print a Counter. This one has more flexibility in formatting, and
		/// doesn't sort the keys.
		/// </remarks>
		public static string ToString<T, V>(IDictionary<T, V> map, string preAppend, string postAppend, string keyValSeparator, string itemSeparator)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(preAppend);
			int i = 0;
			foreach (KeyValuePair<T, V> en in map)
			{
				if (i != 0)
				{
					sb.Append(itemSeparator);
				}
				sb.Append(en.Key);
				sb.Append(keyValSeparator);
				sb.Append(en.Value);
				i++;
			}
			sb.Append(postAppend);
			return sb.ToString();
		}

		public static void Main(string[] args)
		{
			IDictionary<string, string> map1 = Generics.NewHashMap();
			map1["a"] = "1";
			map1["b"] = "2";
			map1["c"] = "2";
			map1["d"] = "4";
			IDictionary<string, string> map2 = Generics.NewHashMap();
			map2["1"] = "x";
			map2["2"] = "y";
			map2["3"] = "z";
			System.Console.Out.WriteLine("map1: " + map1);
			System.Console.Out.WriteLine("invert(map1): " + Edu.Stanford.Nlp.Util.Maps.Invert(map1));
			System.Console.Out.WriteLine("invertSet(map1): " + Edu.Stanford.Nlp.Util.Maps.InvertSet(map1));
			System.Console.Out.WriteLine("map2: " + map2);
			System.Console.Out.WriteLine("compose(map1,map2): " + Edu.Stanford.Nlp.Util.Maps.Compose(map1, map2));
			IDictionary<string, ICollection<string>> setValues = Generics.NewHashMap();
			IDictionary<string, IList<string>> listValues = Generics.NewHashMap();
			Edu.Stanford.Nlp.Util.Maps.PutIntoValueArrayList(listValues, "a", "1");
			Edu.Stanford.Nlp.Util.Maps.PutIntoValueArrayList(listValues, "a", "1");
			Edu.Stanford.Nlp.Util.Maps.PutIntoValueArrayList(listValues, "a", "2");
			Edu.Stanford.Nlp.Util.Maps.PutIntoValueHashSet(setValues, "a", "1");
			Edu.Stanford.Nlp.Util.Maps.PutIntoValueHashSet(setValues, "a", "1");
			Edu.Stanford.Nlp.Util.Maps.PutIntoValueHashSet(setValues, "a", "2");
			System.Console.Out.WriteLine("listValues: " + listValues);
			System.Console.Out.WriteLine("setValues: " + setValues);
		}
	}
}
