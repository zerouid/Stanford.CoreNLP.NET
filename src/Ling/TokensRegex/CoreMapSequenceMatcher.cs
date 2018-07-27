using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>CoreMap Sequence Matcher for regular expressions for sequences over CoreMaps.</summary>
	/// <author>Angel Chang</author>
	public class CoreMapSequenceMatcher<T> : SequenceMatcher<T>
		where T : ICoreMap
	{
		protected internal static readonly IFunction<IList<ICoreMap>, string> CoremapListToStringConverter = null;

		public CoreMapSequenceMatcher(SequencePattern<T> pattern, IList<T> tokens)
			: base(pattern, tokens)
		{
		}

		public class BasicCoreMapSequenceMatcher : CoreMapSequenceMatcher<ICoreMap>
		{
			internal ICoreMap annotation;

			public BasicCoreMapSequenceMatcher(SequencePattern<ICoreMap> pattern, ICoreMap annotation)
				: base(pattern, annotation.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				// this.nodesToStringConverter = COREMAP_LIST_TO_STRING_CONVERTER;
				this.annotation = annotation;
				this.nodesToStringConverter = CoremapListToStringConverter;
			}
		}

		public virtual void AnnotateGroup(IDictionary<string, string> attributes)
		{
			AnnotateGroup(0, attributes);
		}

		public virtual void AnnotateGroup(int group, IDictionary<string, string> attributes)
		{
			int groupStart = Start(group);
			if (groupStart >= 0)
			{
				int groupEnd = End(group);
				ChunkAnnotationUtils.AnnotateChunks(elements, groupStart, groupEnd, attributes);
			}
		}

		public virtual IList<ICoreMap> GetMergedList()
		{
			return GetMergedList(0);
		}

		public virtual IList<ICoreMap> GetMergedList(params int[] groups)
		{
			IList<ICoreMap> res = new List<ICoreMap>();
			int last = 0;
			IList<int> orderedGroups = CollectionUtils.AsList(groups);
			orderedGroups.Sort();
			foreach (int group in orderedGroups)
			{
				int groupStart = Start(group);
				if (groupStart >= last)
				{
					Sharpen.Collections.AddAll(res, elements.SubList(last, groupStart));
					int groupEnd = End(group);
					if (groupEnd - groupStart >= 1)
					{
						ICoreMap merged = CreateMergedChunk(groupStart, groupEnd);
						res.Add(merged);
						last = groupEnd;
					}
				}
			}
			Sharpen.Collections.AddAll(res, elements.SubList(last, elements.Count));
			return res;
		}

		public virtual ICoreMap MergeGroup()
		{
			return MergeGroup(0);
		}

		private ICoreMap CreateMergedChunk(int groupStart, int groupEnd)
		{
			ICoreMap merged = null;
			/*  if (annotation != null) {
			// Take start and end
			merged = ChunkAnnotationUtils.getMergedChunk(elements, annotation.get(CoreAnnotations.TextAnnotation.class), groupStart, groupEnd);
			}  */
			if (merged == null)
			{
				// Okay, have to go through these one by one and merge them
				merged = CoreMapAggregator.GetDefaultAggregator().Merge(elements, groupStart, groupEnd);
			}
			return merged;
		}

		public virtual ICoreMap MergeGroup(int group)
		{
			int groupStart = Start(group);
			if (groupStart >= 0)
			{
				int groupEnd = End(group);
				if (groupEnd - groupStart >= 1)
				{
					return CreateMergedChunk(groupStart, groupEnd);
				}
			}
			return null;
		}
	}
}
