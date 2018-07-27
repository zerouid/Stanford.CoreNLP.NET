using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Function that aggregates several core maps into one</summary>
	/// <author>Angel Chang</author>
	public class CoreMapAggregator : IFunction<IList<ICoreMap>, ICoreMap>
	{
		public static readonly Edu.Stanford.Nlp.Pipeline.CoreMapAggregator DefaultAggregator = GetAggregator(CoreMapAttributeAggregator.GetDefaultAggregators());

		public static readonly Edu.Stanford.Nlp.Pipeline.CoreMapAggregator DefaultNumericTokensAggregator = GetAggregator(CoreMapAttributeAggregator.DefaultNumericTokensAggregators);

		internal IDictionary<Type, CoreMapAttributeAggregator> aggregators;

		internal Type mergedKey = null;

		internal CoreLabelTokenFactory tokenFactory = null;

		public CoreMapAggregator(IDictionary<Type, CoreMapAttributeAggregator> aggregators)
		{
			// Keeps chunks that were merged to form this one
			// Should we be creating tokens?
			this.aggregators = aggregators;
		}

		public CoreMapAggregator(IDictionary<Type, CoreMapAttributeAggregator> aggregators, Type mergedKey, CoreLabelTokenFactory tokenFactory)
		{
			this.aggregators = aggregators;
			this.mergedKey = mergedKey;
			this.tokenFactory = tokenFactory;
		}

		public virtual ICoreMap Merge<_T0>(IList<_T0> @in, int start, int end)
			where _T0 : ICoreMap
		{
			ICoreMap merged = ChunkAnnotationUtils.GetMergedChunk(@in, start, end, aggregators, tokenFactory);
			if (mergedKey != null)
			{
				merged.Set(mergedKey, new List<_T2094911265>(@in.SubList(start, end)));
			}
			return merged;
		}

		public virtual ICoreMap Merge<_T0>(IList<_T0> @in)
			where _T0 : ICoreMap
		{
			return Merge(@in, 0, @in.Count);
		}

		public virtual ICoreMap Apply<_T0>(IList<_T0> @in)
			where _T0 : ICoreMap
		{
			return Merge(@in, 0, @in.Count);
		}

		public static Edu.Stanford.Nlp.Pipeline.CoreMapAggregator GetDefaultAggregator()
		{
			return DefaultAggregator;
		}

		public static Edu.Stanford.Nlp.Pipeline.CoreMapAggregator GetAggregator(IDictionary<Type, CoreMapAttributeAggregator> aggregators)
		{
			return new Edu.Stanford.Nlp.Pipeline.CoreMapAggregator(aggregators);
		}

		public static Edu.Stanford.Nlp.Pipeline.CoreMapAggregator GetAggregator(IDictionary<Type, CoreMapAttributeAggregator> aggregators, Type key)
		{
			return new Edu.Stanford.Nlp.Pipeline.CoreMapAggregator(aggregators, key, null);
		}

		public static Edu.Stanford.Nlp.Pipeline.CoreMapAggregator GetAggregator(IDictionary<Type, CoreMapAttributeAggregator> aggregators, Type key, CoreLabelTokenFactory tokenFactory)
		{
			return new Edu.Stanford.Nlp.Pipeline.CoreMapAggregator(aggregators, key, tokenFactory);
		}

		public virtual IList<ICoreMap> Merge<_T0, _T1>(IList<_T0> list, IList<_T1> matched)
			where _T0 : ICoreMap
			where _T1 : IHasInterval<int>
		{
			return CollectionUtils.MergeList(list, matched, this);
		}

		public virtual IList<ICoreMap> Merge<M, _T1>(IList<_T1> list, IList<M> matched, IFunction<M, Interval<int>> toIntervalFunc)
			where _T1 : ICoreMap
		{
			return CollectionUtils.MergeList(list, matched, toIntervalFunc, this);
		}
	}
}
