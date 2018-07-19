using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Trigger for CoreMap Node Patterns.</summary>
	/// <remarks>
	/// Trigger for CoreMap Node Patterns.  Allows for fast identification of which patterns
	/// may match for one node.
	/// </remarks>
	/// <author>Angel Chang</author>
	public class CoreMapNodePatternTrigger : MultiPatternMatcher.INodePatternTrigger<ICoreMap>
	{
		internal ICollection<SequencePattern<ICoreMap>> patterns;

		internal ICollection<SequencePattern<ICoreMap>> alwaysTriggered = new LinkedHashSet<SequencePattern<ICoreMap>>();

		internal TwoDimensionalCollectionValuedMap<Type, object, SequencePattern<ICoreMap>> annotationTriggers = new TwoDimensionalCollectionValuedMap<Type, object, SequencePattern<ICoreMap>>();

		internal TwoDimensionalCollectionValuedMap<Type, string, SequencePattern<ICoreMap>> lowercaseStringTriggers = new TwoDimensionalCollectionValuedMap<Type, string, SequencePattern<ICoreMap>>();

		public CoreMapNodePatternTrigger(params SequencePattern<ICoreMap>[] patterns)
			: this(Arrays.AsList(patterns))
		{
		}

		public CoreMapNodePatternTrigger(ICollection<SequencePattern<ICoreMap>> patterns)
		{
			this.patterns = patterns;
			IFunction<NodePattern<ICoreMap>, CoreMapNodePatternTrigger.StringTriggerCandidate> stringTriggerFilter = null;
			foreach (SequencePattern<ICoreMap> pattern in patterns)
			{
				// Look for first string...
				ICollection<CoreMapNodePatternTrigger.StringTriggerCandidate> triggerCandidates = pattern.FindNodePatterns(stringTriggerFilter, false, true);
				// TODO: Select most unlikely to trigger trigger from the triggerCandidates
				//  (if we had some statistics on most frequent annotation values...., then pick least frequent)
				// For now, just pick the longest: going from (text or lemma) to rest
				CoreMapNodePatternTrigger.StringTriggerCandidate trigger = triggerCandidates.Stream().Max(StringTriggerCandidateComparator).OrElse(null);
				if (!triggerCandidates.IsEmpty())
				{
					if (trigger.ignoreCase)
					{
						lowercaseStringTriggers.Add(trigger.key, trigger.value.ToLower(), pattern);
					}
					else
					{
						annotationTriggers.Add(trigger.key, trigger.value, pattern);
					}
				}
				else
				{
					alwaysTriggered.Add(pattern);
				}
			}
		}

		private class StringTriggerCandidate
		{
			internal Type key;

			internal string value;

			internal bool ignoreCase;

			internal int keyLevel;

			internal int effectiveValueLength;

			public StringTriggerCandidate(Type key, string value, bool ignoreCase)
			{
				this.key = key;
				this.value = value;
				this.ignoreCase = ignoreCase;
				// Favor text and lemma (more likely to be unique)
				this.keyLevel = (typeof(CoreAnnotations.TextAnnotation).Equals(key) || typeof(CoreAnnotations.LemmaAnnotation).Equals(key)) ? 1 : 0;
				// Special case for -LRB- ( and -RRB- )
				this.effectiveValueLength = ("-LRB-".Equals(value) || "-RRB-".Equals(value)) ? 1 : value.Length;
			}
		}

		private sealed class _IComparator_80 : IComparator<CoreMapNodePatternTrigger.StringTriggerCandidate>
		{
			public _IComparator_80()
			{
			}

			public int Compare(CoreMapNodePatternTrigger.StringTriggerCandidate o1, CoreMapNodePatternTrigger.StringTriggerCandidate o2)
			{
				if (o1.keyLevel != o2.keyLevel)
				{
					return (o1.keyLevel < o2.keyLevel) ? -1 : 1;
				}
				else
				{
					int v1 = o1.effectiveValueLength;
					int v2 = o2.effectiveValueLength;
					if (v1 != v2)
					{
						return (v1 < v2) ? -1 : 1;
					}
					else
					{
						return 0;
					}
				}
			}
		}

		private static readonly IComparator<CoreMapNodePatternTrigger.StringTriggerCandidate> StringTriggerCandidateComparator = new _IComparator_80();

		public virtual ICollection<SequencePattern<ICoreMap>> Apply(ICoreMap @in)
		{
			ICollection<SequencePattern<ICoreMap>> triggeredPatterns = new LinkedHashSet<SequencePattern<ICoreMap>>();
			Sharpen.Collections.AddAll(triggeredPatterns, alwaysTriggered);
			foreach (Type key in annotationTriggers.FirstKeySet())
			{
				object value = @in.Get(key);
				if (value != null)
				{
					ICollection<SequencePattern<ICoreMap>> triggered = annotationTriggers.Get(key, value);
					if (triggered != null)
					{
						Sharpen.Collections.AddAll(triggeredPatterns, triggered);
					}
				}
			}
			foreach (Type key_1 in lowercaseStringTriggers.FirstKeySet())
			{
				object value = @in.Get(key_1);
				if (value != null && value is string)
				{
					ICollection<SequencePattern<ICoreMap>> triggered = lowercaseStringTriggers.Get(key_1, ((string)value).ToLower());
					if (triggered != null)
					{
						Sharpen.Collections.AddAll(triggeredPatterns, triggered);
					}
				}
			}
			// TODO: triggers for normalized patterns...
			return triggeredPatterns;
		}
	}
}
