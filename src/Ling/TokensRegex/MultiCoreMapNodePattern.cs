using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Pattern for matching across multiple core maps.</summary>
	/// <remarks>
	/// Pattern for matching across multiple core maps.
	/// <p>
	/// This class allows for string matches across tokens.  It is not implemented efficiently
	/// (it basically creates a big pretend token and tries to do string match on that)
	/// so can be expensive to use.  Whenever possible, <code>SequencePattern</code> should be used instead.
	/// </p>
	/// </remarks>
	/// <author>Angel Chang</author>
	public class MultiCoreMapNodePattern : MultiNodePattern<ICoreMap>
	{
		internal IDictionary<Type, CoreMapAttributeAggregator> aggregators = CoreMapAttributeAggregator.GetDefaultAggregators();

		internal NodePattern nodePattern;

		public MultiCoreMapNodePattern()
		{
		}

		public MultiCoreMapNodePattern(NodePattern nodePattern)
		{
			this.nodePattern = nodePattern;
		}

		public MultiCoreMapNodePattern(NodePattern nodePattern, IDictionary<Type, CoreMapAttributeAggregator> aggregators)
		{
			this.nodePattern = nodePattern;
			this.aggregators = aggregators;
		}

		protected internal override ICollection<Interval<int>> Match<_T0>(IList<_T0> nodes, int start)
		{
			IList<Interval<int>> matched = new List<Interval<int>>();
			int minEnd = start + minNodes;
			int maxEnd = nodes.Count;
			if (maxNodes >= 0 && maxNodes + start < nodes.Count)
			{
				maxEnd = maxNodes + start;
			}
			for (int end = minEnd; end <= maxEnd; end++)
			{
				ICoreMap chunk = ChunkAnnotationUtils.GetMergedChunk(nodes, start, end, aggregators, null);
				if (nodePattern.Match(chunk))
				{
					matched.Add(Interval.ToInterval(start, end));
				}
			}
			return matched;
		}

		public class StringSequenceAnnotationPattern : MultiNodePattern<ICoreMap>
		{
			internal Type textKey;

			internal PhraseTable phraseTable;

			public StringSequenceAnnotationPattern(Type textKey, ICollection<IList<string>> targets, bool ignoreCase)
			{
				this.textKey = textKey;
				phraseTable = new PhraseTable(false, ignoreCase, false);
				foreach (IList<string> target in targets)
				{
					phraseTable.AddPhrase(target);
					if (maxNodes < 0 || target.Count > maxNodes)
					{
						maxNodes = target.Count;
					}
				}
			}

			public StringSequenceAnnotationPattern(Type textKey, ICollection<IList<string>> targets)
				: this(textKey, targets, false)
			{
			}

			public StringSequenceAnnotationPattern(Type textKey, IDictionary<IList<string>, object> targets, bool ignoreCase)
			{
				this.textKey = textKey;
				phraseTable = new PhraseTable(false, ignoreCase, false);
				foreach (IList<string> target in targets.Keys)
				{
					phraseTable.AddPhrase(target, null, targets[target]);
					if (maxNodes < 0 || target.Count > maxNodes)
					{
						maxNodes = target.Count;
					}
				}
			}

			public StringSequenceAnnotationPattern(Type textKey, IDictionary<IList<string>, object> targets)
				: this(textKey, targets, false)
			{
			}

			protected internal override ICollection<Interval<int>> Match<_T0>(IList<_T0> nodes, int start)
			{
				PhraseTable.IWordList words = new PhraseTable.TokenList(nodes, textKey);
				IList<PhraseTable.PhraseMatch> matches = phraseTable.FindMatches(words, start, nodes.Count, false);
				ICollection<Interval<int>> intervals = new List<Interval<int>>(matches.Count);
				foreach (PhraseTable.PhraseMatch match in matches)
				{
					intervals.Add(match.GetInterval());
				}
				return intervals;
			}

			public override string ToString()
			{
				return ":" + phraseTable;
			}
		}
	}
}
