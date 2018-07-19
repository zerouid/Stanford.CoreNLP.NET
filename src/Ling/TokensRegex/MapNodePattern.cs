using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Pattern for matching a Map from keys K to objects</summary>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class MapNodePattern<M, K> : ComplexNodePattern<M, K>
		where M : IDictionary<K, object>
	{
		private static IBiFunction<M, K, object> CreateGetter<M, K>()
			where M : IDictionary<K, object>
		{
			return new _IBiFunction_17();
		}

		private sealed class _IBiFunction_17 : IBiFunction<M, K, object>
		{
			public _IBiFunction_17()
			{
			}

			public object Apply(M m, K k)
			{
				return m[k];
			}
		}

		public MapNodePattern(IList<Pair<K, NodePattern>> annotationPatterns)
			: base(CreateGetter(), annotationPatterns)
		{
		}

		public MapNodePattern(params Pair<K, NodePattern>[] annotationPatterns)
			: base(CreateGetter(), annotationPatterns)
		{
		}

		public MapNodePattern(K key, NodePattern pattern)
			: base(CreateGetter(), key, pattern)
		{
		}
	}
}
