using System.Collections.Generic;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Performs a action on a matched sequence</summary>
	/// <author>Angel Chang</author>
	public abstract class CoreMapSequenceMatchAction<T> : ISequenceMatchAction<T>
		where T : ICoreMap
	{
		public sealed class AnnotateAction<T> : CoreMapSequenceMatchAction<T>
			where T : ICoreMap
		{
			internal IDictionary<string, string> attributes;

			public AnnotateAction(IDictionary<string, string> attributes)
			{
				// TODO: Preconvert, handle when to overwrite existing attributes
				this.attributes = attributes;
			}

			public override ISequenceMatchResult<T> Apply(ISequenceMatchResult<T> matchResult, params int[] groups)
			{
				foreach (int group in groups)
				{
					int groupStart = matchResult.Start(group);
					if (groupStart >= 0)
					{
						int groupEnd = matchResult.End(group);
						ChunkAnnotationUtils.AnnotateChunks(matchResult.Elements(), groupStart, groupEnd, attributes);
					}
				}
				return matchResult;
			}
		}

		public static readonly CoreMapSequenceMatchAction.MergeAction DefaultMergeAction = new CoreMapSequenceMatchAction.MergeAction();

		public sealed class MergeAction : CoreMapSequenceMatchAction<ICoreMap>
		{
			internal CoreMapAggregator aggregator = CoreMapAggregator.GetDefaultAggregator();

			public MergeAction()
			{
			}

			public MergeAction(CoreMapAggregator aggregator)
			{
				this.aggregator = aggregator;
			}

			public override ISequenceMatchResult<ICoreMap> Apply(ISequenceMatchResult<ICoreMap> matchResult, params int[] groups)
			{
				BasicSequenceMatchResult<ICoreMap> res = matchResult.ToBasicSequenceMatchResult();
				IList<ICoreMap> elements = matchResult.Elements();
				IList<ICoreMap> mergedElements = new List<ICoreMap>();
				res.elements = mergedElements;
				int last = 0;
				int mergedGroup = 0;
				int offset = 0;
				IList<int> orderedGroups = CollectionUtils.AsList(groups);
				orderedGroups.Sort();
				foreach (int group in orderedGroups)
				{
					int groupStart = matchResult.Start(group);
					if (groupStart >= last)
					{
						// Add elements from last to start of group to merged elements
						Sharpen.Collections.AddAll(mergedElements, elements.SubList(last, groupStart));
						// Fiddle with matched group indices
						for (; mergedGroup < group; mergedGroup++)
						{
							if (res.matchedGroups[mergedGroup] != null)
							{
								res.matchedGroups[mergedGroup].matchBegin -= offset;
								res.matchedGroups[mergedGroup].matchEnd -= offset;
							}
						}
						// Get merged element
						int groupEnd = matchResult.End(group);
						if (groupEnd - groupStart >= 1)
						{
							ICoreMap merged = aggregator.Merge(elements, groupStart, groupEnd);
							mergedElements.Add(merged);
							last = groupEnd;
							// Fiddle with matched group indices
							res.matchedGroups[mergedGroup].matchBegin = mergedElements.Count - 1;
							res.matchedGroups[mergedGroup].matchEnd = mergedElements.Count;
							mergedGroup++;
							while (mergedGroup < res.matchedGroups.Length)
							{
								if (res.matchedGroups[mergedGroup] != null)
								{
									if (res.matchedGroups[mergedGroup].matchBegin == matchResult.Start(group) && res.matchedGroups[mergedGroup].matchEnd == matchResult.End(group))
									{
										res.matchedGroups[mergedGroup].matchBegin = res.matchedGroups[group].matchBegin;
										res.matchedGroups[mergedGroup].matchEnd = res.matchedGroups[group].matchEnd;
									}
									else
									{
										if (res.matchedGroups[mergedGroup].matchEnd <= matchResult.End(group))
										{
											res.matchedGroups[mergedGroup] = null;
										}
										else
										{
											break;
										}
									}
								}
								mergedGroup++;
							}
							offset = matchResult.End(group) - res.matchedGroups[group].matchEnd;
						}
					}
				}
				// Add rest of elements
				Sharpen.Collections.AddAll(mergedElements, elements.SubList(last, elements.Count));
				// Fiddle with matched group indices
				for (; mergedGroup < res.matchedGroups.Length; mergedGroup++)
				{
					if (res.matchedGroups[mergedGroup] != null)
					{
						res.matchedGroups[mergedGroup].matchBegin -= offset;
						res.matchedGroups[mergedGroup].matchEnd -= offset;
					}
				}
				return res;
			}
		}

		public abstract ISequenceMatchResult<T> Apply(ISequenceMatchResult<T> arg1, int[] arg2);
	}
}
