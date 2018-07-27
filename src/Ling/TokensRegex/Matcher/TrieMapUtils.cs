using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Ling.Tokensregex.Matcher
{
	/// <summary>Utility functions for using trie maps</summary>
	/// <author>Angel Chang</author>
	public class TrieMapUtils
	{
		public static ICounter<IEnumerable<K>> TrieMapCounter<K>()
		{
			return new ClassicCounter<IEnumerable<K>>(TrieMapUtils.TrieMapFactory<K, MutableDouble>());
		}

		public static CollectionValuedMap<IEnumerable<K>, V> CollectionValuedTrieMap<K, V>()
		{
			return new CollectionValuedMap<IEnumerable<K>, V>(TrieMapUtils.TrieMapFactory<K, ICollection<V>>(), CollectionFactory.HashSetFactory<V>(), false);
		}

		public static CollectionValuedMap<IEnumerable<K>, V> CollectionValuedTrieMap<K, V>(CollectionFactory<V> collectionFactory)
		{
			return new CollectionValuedMap<IEnumerable<K>, V>(TrieMapUtils.TrieMapFactory<K, ICollection<V>>(), collectionFactory, false);
		}

		public static MapFactory<IEnumerable<K>, V> TrieMapFactory<K, V>()
		{
			return TrieMapFactory;
		}

		private static readonly MapFactory TrieMapFactory = new TrieMapUtils.TrieMapFactory();

		[System.Serializable]
		private class TrieMapFactory<K, V> : MapFactory<IEnumerable<K>, V>
		{
			private const long serialVersionUID = 1;

			public override IDictionary<IEnumerable<K>, V> NewMap()
			{
				return new TrieMap<K, V>();
			}

			public override IDictionary<IEnumerable<K>, V> NewMap(int initCapacity)
			{
				return new TrieMap<K, V>(initCapacity);
			}

			public override ICollection<IEnumerable<K>> NewSet()
			{
				return Java.Util.Collections.NewSetFromMap(new TrieMap<K, bool>());
			}

			public override ICollection<IEnumerable<K>> NewSet(ICollection<IEnumerable<K>> init)
			{
				ICollection<IEnumerable<K>> set = Java.Util.Collections.NewSetFromMap(new TrieMap<K, bool>());
				Sharpen.Collections.AddAll(init, init);
				return set;
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map)
			{
				throw new NotSupportedException();
			}

			public override IDictionary<K1, V1> SetMap<K1, V1>(IDictionary<K1, V1> map, int initCapacity)
			{
				throw new NotSupportedException();
			}
		}
		// end class TrieMapFactory
	}
}
