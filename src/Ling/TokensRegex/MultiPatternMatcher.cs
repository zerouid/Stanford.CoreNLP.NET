using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Matcher that takes in multiple patterns.</summary>
	/// <author>Angel Chang</author>
	public class MultiPatternMatcher<T>
	{
		internal ICollection<SequencePattern<T>> patterns;

		private MultiPatternMatcher.ISequencePatternTrigger<T> patternTrigger;

		private bool matchWithResult = false;

		public MultiPatternMatcher(MultiPatternMatcher.ISequencePatternTrigger<T> patternTrigger, ICollection<SequencePattern<T>> patterns)
		{
			this.patterns = new List<SequencePattern<T>>();
			Sharpen.Collections.AddAll(this.patterns, patterns);
			this.patternTrigger = patternTrigger;
		}

		[SafeVarargs]
		public MultiPatternMatcher(MultiPatternMatcher.ISequencePatternTrigger<T> patternTrigger, params SequencePattern<T>[] patterns)
			: this(patterns)
		{
			this.patternTrigger = patternTrigger;
		}

		public MultiPatternMatcher(ICollection<SequencePattern<T>> patterns)
		{
			this.patterns = patterns;
		}

		[SafeVarargs]
		public MultiPatternMatcher(params SequencePattern<T>[] patterns)
		{
			this.patterns = new List<SequencePattern<T>>(patterns.Length);
			Java.Util.Collections.AddAll(this.patterns, patterns);
		}

		/// <summary>
		/// Given a sequence, applies our patterns over the sequence and returns
		/// all non overlapping matches.
		/// </summary>
		/// <remarks>
		/// Given a sequence, applies our patterns over the sequence and returns
		/// all non overlapping matches.  When multiple patterns overlaps,
		/// matched patterns are selected by
		/// the highest priority/score is selected,
		/// then the longest pattern,
		/// then the starting offset,
		/// then the original order.
		/// </remarks>
		/// <param name="elements">input sequence to match against</param>
		/// <returns>list of match results that are non-overlapping</returns>
		public virtual IList<ISequenceMatchResult<T>> FindNonOverlapping<_T0>(IList<_T0> elements)
			where _T0 : T
		{
			return FindNonOverlapping(elements, SequenceMatchResultConstants.DefaultComparator);
		}

		/// <summary>
		/// Given a sequence, applies our patterns over the sequence and returns
		/// all non overlapping matches.
		/// </summary>
		/// <remarks>
		/// Given a sequence, applies our patterns over the sequence and returns
		/// all non overlapping matches.  When multiple patterns overlaps,
		/// matched patterns are selected by order specified by the comparator
		/// </remarks>
		/// <param name="elements">input sequence to match against</param>
		/// <param name="cmp">comparator indicating order that overlapped sequences should be selected.</param>
		/// <returns>list of match results that are non-overlapping</returns>
		public virtual IList<ISequenceMatchResult<T>> FindNonOverlapping<_T0, _T1>(IList<_T0> elements, IComparator<_T1> cmp)
			where _T0 : T
		{
			ICollection<SequencePattern<T>> triggered = GetTriggeredPatterns(elements);
			IList<ISequenceMatchResult<T>> all = new List<ISequenceMatchResult<T>>();
			int i = 0;
			foreach (SequencePattern<T> p in triggered)
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting
					throw new RuntimeInterruptedException();
				}
				SequenceMatcher<T> m = p.GetMatcher(elements);
				m.SetMatchWithResult(matchWithResult);
				m.SetOrder(i);
				while (m.Find())
				{
					all.Add(m.ToBasicSequenceMatchResult());
				}
				i++;
			}
			IList<ISequenceMatchResult<T>> res = IntervalTree.GetNonOverlapping(all, SequenceMatchResultConstants.ToInterval, cmp);
			res.Sort(SequenceMatchResultConstants.OffsetComparator);
			return res;
		}

		/// <summary>
		/// Given a sequence, applies our patterns over the sequence and returns
		/// all matches, depending on the findType.
		/// </summary>
		/// <remarks>
		/// Given a sequence, applies our patterns over the sequence and returns
		/// all matches, depending on the findType.  When multiple patterns overlaps,
		/// matched patterns are selected by order specified by the comparator
		/// </remarks>
		/// <param name="elements">input sequence to match against</param>
		/// <param name="findType">whether FindType.FIND_ALL or FindType.FIND_NONOVERLAPPING</param>
		/// <returns>list of match results</returns>
		public virtual IList<ISequenceMatchResult<T>> Find<_T0>(IList<_T0> elements, SequenceMatcher.FindType findType)
			where _T0 : T
		{
			ICollection<SequencePattern<T>> triggered = GetTriggeredPatterns(elements);
			IList<ISequenceMatchResult<T>> all = new List<ISequenceMatchResult<T>>();
			int i = 0;
			foreach (SequencePattern<T> p in triggered)
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting
					throw new RuntimeInterruptedException();
				}
				SequenceMatcher<T> m = p.GetMatcher(elements);
				m.SetMatchWithResult(matchWithResult);
				m.SetFindType(findType);
				m.SetOrder(i);
				while (m.Find())
				{
					all.Add(m.ToBasicSequenceMatchResult());
				}
				i++;
			}
			IList<ISequenceMatchResult<T>> res = IntervalTree.GetNonOverlapping(all, SequenceMatchResultConstants.ToInterval, SequenceMatchResultConstants.DefaultComparator);
			res.Sort(SequenceMatchResultConstants.OffsetComparator);
			return res;
		}

		/// <summary>
		/// Given a sequence, applies our patterns over the sequence and returns
		/// all non overlapping matches.
		/// </summary>
		/// <remarks>
		/// Given a sequence, applies our patterns over the sequence and returns
		/// all non overlapping matches.  When multiple patterns overlaps,
		/// matched patterns are selected to give the overall maximum score
		/// </remarks>
		/// <param name="elements">input sequence to match against</param>
		/// <returns>list of match results that are non-overlapping</returns>
		public virtual IList<ISequenceMatchResult<T>> FindNonOverlappingMaxScore<_T0>(IList<_T0> elements)
			where _T0 : T
		{
			return FindNonOverlappingMaxScore(elements, SequenceMatchResultConstants.Scorer);
		}

		/// <summary>
		/// Given a sequence, applies our patterns over the sequence and returns
		/// all non overlapping matches.
		/// </summary>
		/// <remarks>
		/// Given a sequence, applies our patterns over the sequence and returns
		/// all non overlapping matches.  When multiple patterns overlaps,
		/// matched patterns are selected to give the overall maximum score.
		/// </remarks>
		/// <param name="elements">input sequence to match against</param>
		/// <param name="scorer">scorer for scoring each match</param>
		/// <returns>list of match results that are non-overlapping</returns>
		public virtual IList<ISequenceMatchResult<T>> FindNonOverlappingMaxScore<_T0, _T1>(IList<_T0> elements, IToDoubleFunction<_T1> scorer)
			where _T0 : T
		{
			ICollection<SequencePattern<T>> triggered = GetTriggeredPatterns(elements);
			IList<ISequenceMatchResult<T>> all = new List<ISequenceMatchResult<T>>();
			int i = 0;
			foreach (SequencePattern<T> p in triggered)
			{
				SequenceMatcher<T> m = p.GetMatcher(elements);
				m.SetMatchWithResult(matchWithResult);
				m.SetOrder(i);
				while (m.Find())
				{
					all.Add(m.ToBasicSequenceMatchResult());
				}
				i++;
			}
			IList<ISequenceMatchResult<T>> res = IntervalTree.GetNonOverlappingMaxScore(all, SequenceMatchResultConstants.ToInterval, scorer);
			res.Sort(SequenceMatchResultConstants.OffsetComparator);
			return res;
		}

		/// <summary>
		/// Given a sequence, applies each of our patterns over the sequence and returns
		/// all non overlapping matches for each of the patterns.
		/// </summary>
		/// <remarks>
		/// Given a sequence, applies each of our patterns over the sequence and returns
		/// all non overlapping matches for each of the patterns.
		/// Unlike #findAllNonOverlapping, overlapping matches from different patterns are kept.
		/// </remarks>
		/// <param name="elements">input sequence to match against</param>
		/// <returns>iterable of match results that are non-overlapping</returns>
		public virtual IEnumerable<ISequenceMatchResult<T>> FindAllNonOverlappingMatchesPerPattern<_T0>(IList<_T0> elements)
			where _T0 : T
		{
			ICollection<SequencePattern<T>> triggered = GetTriggeredPatterns(elements);
			IList<IEnumerable<ISequenceMatchResult<T>>> allMatches = new List<IEnumerable<ISequenceMatchResult<T>>>(elements.Count);
			foreach (SequencePattern<T> p in triggered)
			{
				SequenceMatcher<T> m = p.GetMatcher(elements);
				m.SetMatchWithResult(matchWithResult);
				IEnumerable<ISequenceMatchResult<T>> matches = m.FindAllNonOverlapping();
				allMatches.Add(matches);
			}
			return Iterables.Chain(allMatches);
		}

		/// <summary>
		/// Given a sequence, return the collection of patterns that are triggered by the sequence
		/// (these patterns are the ones that may potentially match a subsequence in the sequence)
		/// </summary>
		/// <param name="elements">Input sequence</param>
		/// <returns>Collection of triggered patterns</returns>
		public virtual ICollection<SequencePattern<T>> GetTriggeredPatterns<_T0>(IList<_T0> elements)
			where _T0 : T
		{
			if (patternTrigger != null)
			{
				return patternTrigger.Apply(elements);
			}
			else
			{
				return patterns;
			}
		}

		public virtual bool IsMatchWithResult()
		{
			return matchWithResult;
		}

		public virtual void SetMatchWithResult(bool matchWithResult)
		{
			this.matchWithResult = matchWithResult;
		}

		/// <summary>
		/// A function which returns a collections of patterns that may match when
		/// given a single node from a larger sequence.
		/// </summary>
		/// <?/>
		public interface INodePatternTrigger<T> : IFunction<T, ICollection<SequencePattern<T>>>
		{
			/* Interfaces for optimizing application of many SequencePatterns over a particular sequence */
		}

		/// <summary>
		/// A function which returns a collections of patterns that may match when
		/// a sequence of nodes.
		/// </summary>
		/// <remarks>
		/// A function which returns a collections of patterns that may match when
		/// a sequence of nodes.  Note that this function needs to be conservative
		/// and should return ALL patterns that may match.
		/// </remarks>
		/// <?/>
		public interface ISequencePatternTrigger<T> : IFunction<IList<T>, ICollection<SequencePattern<T>>>
		{
		}

		/// <summary>
		/// Simple SequencePatternTrigger that looks at each node, and identifies which
		/// patterns may potentially match each node, and then aggregates (union)
		/// all these patterns together.
		/// </summary>
		/// <remarks>
		/// Simple SequencePatternTrigger that looks at each node, and identifies which
		/// patterns may potentially match each node, and then aggregates (union)
		/// all these patterns together.  Original ordering of patterns is preserved.
		/// </remarks>
		/// <?/>
		public class BasicSequencePatternTrigger<T> : MultiPatternMatcher.ISequencePatternTrigger<T>
		{
			internal MultiPatternMatcher.INodePatternTrigger<T> trigger;

			public BasicSequencePatternTrigger(MultiPatternMatcher.INodePatternTrigger<T> trigger)
			{
				this.trigger = trigger;
			}

			public virtual ICollection<SequencePattern<T>> Apply<_T0>(IList<_T0> elements)
				where _T0 : T
			{
				// Use LinkedHashSet to preserve original ordering of patterns.
				ICollection<SequencePattern<T>> triggeredPatterns = new LinkedHashSet<SequencePattern<T>>();
				foreach (T node in elements)
				{
					if (Thread.Interrupted())
					{
						// Allow interrupting
						throw new RuntimeInterruptedException();
					}
					ICollection<SequencePattern<T>> triggered = trigger.Apply(node);
					Sharpen.Collections.AddAll(triggeredPatterns, triggered);
				}
				return triggeredPatterns;
			}
		}
	}
}
