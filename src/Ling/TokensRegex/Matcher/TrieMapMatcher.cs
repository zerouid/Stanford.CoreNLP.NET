using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex.Matcher
{
	/// <summary>
	/// The
	/// <c>TrieMapMatcher</c>
	/// provides functions to match against a trie.
	/// It can be used to:
	/// <ul>
	/// <li> Find matches in a document (findAllMatches and findNonOverlapping) </li>
	/// <li> Find approximate matches in a document (findClosestMatches) </li>
	/// <li> Segment a sequence based on entries in the trie (segment) </li>
	/// </ul>
	/// TODO: Have TrieMapMatcher implement a matcher interface
	/// </summary>
	/// <author>Angel Chang</author>
	public class TrieMapMatcher<K, V>
	{
		private readonly TrieMap<K, V> root;

		private readonly TrieMap<K, V> rootWithDelimiter;

		private IList<K> multimatchDelimiter;

		public TrieMapMatcher(TrieMap<K, V> root)
		{
			this.root = root;
			this.rootWithDelimiter = root;
		}

		public TrieMapMatcher(TrieMap<K, V> root, IList<K> multimatchDelimiter)
		{
			this.root = root;
			this.multimatchDelimiter = multimatchDelimiter;
			if (multimatchDelimiter != null && !multimatchDelimiter.IsEmpty())
			{
				// Create a new root that always starts with the delimiter
				rootWithDelimiter = new TrieMap<K, V>();
				rootWithDelimiter.PutChildTrie(multimatchDelimiter, root);
			}
			else
			{
				rootWithDelimiter = root;
			}
		}

		/// <summary>Given a target sequence, returns the n closes matches (or sequences of matches) from the trie.</summary>
		/// <remarks>
		/// Given a target sequence, returns the n closes matches (or sequences of matches) from the trie.
		/// The cost function used is a exact match cost function (exact match has cost 0, otherwise, cost is 1)
		/// </remarks>
		/// <param name="target">Target sequence to match</param>
		/// <param name="n">Number of matches to return. The actual number of matches may be less.</param>
		/// <returns>List of approximate matches</returns>
		public virtual IList<ApproxMatch<K, V>> FindClosestMatches(K[] target, int n)
		{
			return FindClosestMatches(Arrays.AsList(target), n);
		}

		/// <summary>Given a target sequence, returns the n closes matches (or sequences of matches) from the trie.</summary>
		/// <remarks>
		/// Given a target sequence, returns the n closes matches (or sequences of matches) from the trie.
		/// The cost function used is a exact match cost function (exact match has cost 0, otherwise, cost is 1)
		/// </remarks>
		/// <param name="target">Target sequence to match</param>
		/// <param name="n">Number of matches to return. The actual number of matches may be less.</param>
		/// <param name="multimatch">
		/// If true, attempt to return matches with sequences of elements from the trie.
		/// Otherwise, only each match will contain one element from the trie.
		/// </param>
		/// <param name="keepAlignments">If true, alignment information is returned</param>
		/// <returns>List of approximate matches</returns>
		public virtual IList<ApproxMatch<K, V>> FindClosestMatches(K[] target, int n, bool multimatch, bool keepAlignments)
		{
			return FindClosestMatches(Arrays.AsList(target), n, multimatch, keepAlignments);
		}

		/// <summary>
		/// Given a target sequence, returns the n closes matches (or sequences of matches) from the trie
		/// based on the cost function (lower cost mean better match).
		/// </summary>
		/// <param name="target">Target sequence to match</param>
		/// <param name="costFunction">Cost function to use</param>
		/// <param name="maxCost">Matches with a cost higher than this are discarded</param>
		/// <param name="n">Number of matches to return. The actual number of matches may be less.</param>
		/// <param name="multimatch">
		/// If true, attempt to return matches with sequences of elements from the trie.
		/// Otherwise, only each match will contain one element from the trie.
		/// </param>
		/// <param name="keepAlignments">If true, alignment information is returned</param>
		/// <returns>List of approximate matches</returns>
		public virtual IList<ApproxMatch<K, V>> FindClosestMatches(K[] target, IMatchCostFunction<K, V> costFunction, double maxCost, int n, bool multimatch, bool keepAlignments)
		{
			return FindClosestMatches(Arrays.AsList(target), costFunction, maxCost, n, multimatch, keepAlignments);
		}

		/// <summary>Given a target sequence, returns the n closes matches (or sequences of matches) from the trie.</summary>
		/// <remarks>
		/// Given a target sequence, returns the n closes matches (or sequences of matches) from the trie.
		/// The cost function used is a exact match cost function (exact match has cost 0, otherwise, cost is 1)
		/// </remarks>
		/// <param name="target">Target sequence to match</param>
		/// <param name="n">Number of matches to return. The actual number of matches may be less.</param>
		/// <returns>List of approximate matches</returns>
		public virtual IList<ApproxMatch<K, V>> FindClosestMatches(IList<K> target, int n)
		{
			return FindClosestMatches(target, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMapMatcher.DefaultCost<K, V>(), double.MaxValue, n, false, false);
		}

		/// <summary>Given a target sequence, returns the n closes matches (or sequences of matches) from the trie.</summary>
		/// <remarks>
		/// Given a target sequence, returns the n closes matches (or sequences of matches) from the trie.
		/// The cost function used is a exact match cost function (exact match has cost 0, otherwise, cost is 1)
		/// </remarks>
		/// <param name="target">Target sequence to match</param>
		/// <param name="n">Number of matches to return. The actual number of matches may be less.</param>
		/// <param name="multimatch">
		/// If true, attempt to return matches with sequences of elements from the trie.
		/// Otherwise, only each match will contain one element from the trie.
		/// </param>
		/// <param name="keepAlignments">If true, alignment information is returned</param>
		/// <returns>List of approximate matches</returns>
		public virtual IList<ApproxMatch<K, V>> FindClosestMatches(IList<K> target, int n, bool multimatch, bool keepAlignments)
		{
			return FindClosestMatches(target, Edu.Stanford.Nlp.Ling.Tokensregex.Matcher.TrieMapMatcher.DefaultCost<K, V>(), double.MaxValue, n, multimatch, keepAlignments);
		}

		/// <summary>
		/// Given a target sequence, returns the n closes matches (or sequences of matches) from the trie
		/// based on the cost function (lower cost mean better match).
		/// </summary>
		/// <param name="target">Target sequence to match</param>
		/// <param name="costFunction">Cost function to use</param>
		/// <param name="maxCost">Matches with a cost higher than this are discarded</param>
		/// <param name="n">Number of matches to return. The actual number of matches may be less.</param>
		/// <param name="multimatch">
		/// If true, attempt to return matches with sequences of elements from the trie.
		/// Otherwise, only each match will contain one element from the trie.
		/// </param>
		/// <param name="keepAlignments">If true, alignment information is returned</param>
		/// <returns>List of approximate matches</returns>
		public virtual IList<ApproxMatch<K, V>> FindClosestMatches(IList<K> target, IMatchCostFunction<K, V> costFunction, double maxCost, int n, bool multimatch, bool keepAlignments)
		{
			if (root.IsEmpty())
			{
				return null;
			}
			int extra = 3;
			// Find the closest n options to the key in the trie based on the given cost function for substitution
			// matches[i][j] stores the top n partial matches for i elements from the target
			//   and j elements from the partial matches from trie keys
			// At any time, we only keep track of the last two rows
			// (prevMatches (matches[i-1][j]), curMatches (matches[i][j]) that we are working on
			TrieMapMatcher.MatchQueue<K, V> best = new TrieMapMatcher.MatchQueue<K, V>(n, maxCost);
			IList<TrieMapMatcher.PartialApproxMatch<K, V>>[] prevMatches = null;
			for (int i = 0; i <= target.Count; i++)
			{
				IList<TrieMapMatcher.PartialApproxMatch<K, V>>[] curMatches = new IList[target.Count + 1 + extra];
				for (int j = 0; j <= target.Count + extra; j++)
				{
					if (j > 0)
					{
						bool complete = (i == target.Count);
						// Try to pick best match from trie
						K t = (i > 0 && i <= target.Count) ? target[i - 1] : null;
						// Look at the top n choices we saved away and pick n new options
						TrieMapMatcher.MatchQueue<K, V> queue = (multimatch) ? new TrieMapMatcher.MultiMatchQueue<K, V>(n, maxCost) : new TrieMapMatcher.MatchQueue<K, V>(n, maxCost);
						if (i > 0)
						{
							foreach (TrieMapMatcher.PartialApproxMatch<K, V> pam in prevMatches[j - 1])
							{
								if (pam.trie != null)
								{
									if (pam.trie.children != null)
									{
										foreach (K k in pam.trie.children.Keys)
										{
											AddToQueue(queue, best, costFunction, pam, t, k, multimatch, complete);
										}
									}
								}
							}
						}
						foreach (TrieMapMatcher.PartialApproxMatch<K, V> pam_1 in curMatches[j - 1])
						{
							if (pam_1.trie != null)
							{
								if (pam_1.trie.children != null)
								{
									foreach (K k in pam_1.trie.children.Keys)
									{
										AddToQueue(queue, best, costFunction, pam_1, null, k, multimatch, complete);
									}
								}
							}
						}
						if (i > 0)
						{
							foreach (TrieMapMatcher.PartialApproxMatch<K, V> pam in prevMatches[j])
							{
								AddToQueue(queue, best, costFunction, pam_1, t, null, multimatch, complete);
							}
						}
						curMatches[j] = queue.ToSortedList();
					}
					else
					{
						curMatches[0] = new List<TrieMapMatcher.PartialApproxMatch<K, V>>();
						if (i > 0)
						{
							K t = (i < target.Count) ? target[i - 1] : null;
							foreach (TrieMapMatcher.PartialApproxMatch<K, V> pam in prevMatches[0])
							{
								TrieMapMatcher.PartialApproxMatch<K, V> npam = pam.WithMatch(costFunction, costFunction.Cost(t, null, pam.GetMatchedLength()), t, null);
								if (npam.cost <= maxCost)
								{
									curMatches[0].Add(npam);
								}
							}
						}
						else
						{
							curMatches[0].Add(new TrieMapMatcher.PartialApproxMatch<K, V>(0, root, keepAlignments ? target.Count : 0));
						}
					}
				}
				//        System.out.println("i=" + i + ",j=" + j + "," + matches[i][j]);
				prevMatches = curMatches;
			}
			// Get the best matches
			IList<ApproxMatch<K, V>> res = new List<ApproxMatch<K, V>>();
			foreach (TrieMapMatcher.PartialApproxMatch<K, V> m in best.ToSortedList())
			{
				res.Add(m.ToApproxMatch());
			}
			return res;
		}

		/// <summary>Given a sequence to search through (e.g.</summary>
		/// <remarks>
		/// Given a sequence to search through (e.g. piece of text would be a sequence of words),
		/// finds all matching sub-sequences that matches entries in the trie.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <returns>List of matches</returns>
		[SafeVarargs]
		public IList<Match<K, V>> FindAllMatches(params K[] list)
		{
			return FindAllMatches(Arrays.AsList(list));
		}

		/// <summary>Given a sequence to search through (e.g.</summary>
		/// <remarks>
		/// Given a sequence to search through (e.g. piece of text would be a sequence of words),
		/// finds all matching sub-sequences that matches entries in the trie.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <returns>List of matches</returns>
		public virtual IList<Match<K, V>> FindAllMatches(IList<K> list)
		{
			return FindAllMatches(list, 0, list.Count);
		}

		/// <summary>Given a sequence to search through (e.g.</summary>
		/// <remarks>
		/// Given a sequence to search through (e.g. piece of text would be a sequence of words),
		/// finds all matching sub-sequences that matches entries in the trie.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <param name="start">start index to start search at</param>
		/// <param name="end">end index (exclusive) to end search at</param>
		/// <returns>List of matches</returns>
		public virtual IList<Match<K, V>> FindAllMatches(IList<K> list, int start, int end)
		{
			IList<Match<K, V>> allMatches = new List<Match<K, V>>();
			UpdateAllMatches(root, allMatches, new List<K>(), list, start, end);
			return allMatches;
		}

		/// <summary>Given a sequence to search through (e.g.</summary>
		/// <remarks>
		/// Given a sequence to search through (e.g. piece of text would be a sequence of words),
		/// finds all non-overlapping matching sub-sequences that matches entries in the trie.
		/// Sub-sequences that are longer are preferred, then sub-sequences that starts earlier.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <returns>List of matches sorted by start position</returns>
		[SafeVarargs]
		public IList<Match<K, V>> FindNonOverlapping(params K[] list)
		{
			return FindNonOverlapping(Arrays.AsList(list));
		}

		/// <summary>Given a sequence to search through (e.g.</summary>
		/// <remarks>
		/// Given a sequence to search through (e.g. piece of text would be a sequence of words),
		/// finds all non-overlapping matching sub-sequences that matches entries in the trie.
		/// Sub-sequences that are longer are preferred, then sub-sequences that starts earlier.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <returns>List of matches sorted by start position</returns>
		public virtual IList<Match<K, V>> FindNonOverlapping(IList<K> list)
		{
			return FindNonOverlapping(list, 0, list.Count);
		}

		public static readonly IComparator<Match> MatchLengthEndpointsComparator = Interval.LengthEndpointsComparator();

		public static readonly IToDoubleFunction<Match> MatchLengthScorer = Interval.LengthScorer();

		/// <summary>Given a sequence to search through (e.g.</summary>
		/// <remarks>
		/// Given a sequence to search through (e.g. piece of text would be a sequence of words),
		/// finds all non-overlapping matching sub-sequences that matches entries in the trie.
		/// Sub-sequences that are longer are preferred, then sub-sequences that starts earlier.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <param name="start">start index to start search at</param>
		/// <param name="end">end index (exclusive) to end search at</param>
		/// <returns>List of matches sorted by start position</returns>
		public virtual IList<Match<K, V>> FindNonOverlapping(IList<K> list, int start, int end)
		{
			return FindNonOverlapping(list, start, end, MatchLengthEndpointsComparator);
		}

		/// <summary>Given a sequence to search through (e.g.</summary>
		/// <remarks>
		/// Given a sequence to search through (e.g. piece of text would be a sequence of words),
		/// finds all non-overlapping matching sub-sequences that matches entries in the trie.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <param name="start">start index to start search at</param>
		/// <param name="end">end index (exclusive) to end search at</param>
		/// <param name="compareFunc">
		/// Comparison function to use for evaluating which overlapping sub-sequence to keep.
		/// Earlier sub-sequences based on the comparison function are favored.
		/// </param>
		/// <returns>List of matches sorted by start position</returns>
		public virtual IList<Match<K, V>> FindNonOverlapping<_T0>(IList<K> list, int start, int end, IComparator<_T0> compareFunc)
		{
			IList<Match<K, V>> allMatches = FindAllMatches(list, start, end);
			return GetNonOverlapping(allMatches, compareFunc);
		}

		/// <summary>Given a sequence to search through (e.g.</summary>
		/// <remarks>
		/// Given a sequence to search through (e.g. piece of text would be a sequence of words),
		/// finds all non-overlapping matching sub-sequences that matches entries in the trie while attempting to maximize the scoreFunc.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <param name="start">start index to start search at</param>
		/// <param name="end">end index (exclusive) to end search at</param>
		/// <param name="scoreFunc">Scoring function indicating how good the match is</param>
		/// <returns>List of matches sorted by start position</returns>
		public virtual IList<Match<K, V>> FindNonOverlapping<_T0>(IList<K> list, int start, int end, IToDoubleFunction<_T0> scoreFunc)
		{
			IList<Match<K, V>> allMatches = FindAllMatches(list, start, end);
			return GetNonOverlapping(allMatches, scoreFunc);
		}

		/// <summary>
		/// Segment a sequence into sequence of sub-sequences by attempting to find the longest non-overlapping
		/// sub-sequences.
		/// </summary>
		/// <remarks>
		/// Segment a sequence into sequence of sub-sequences by attempting to find the longest non-overlapping
		/// sub-sequences.  Non-matched parts will be included as a match with a null value.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <returns>List of segments (as matches) sorted by start position</returns>
		[SafeVarargs]
		public IList<Match<K, V>> Segment(params K[] list)
		{
			return Segment(Arrays.AsList(list));
		}

		/// <summary>
		/// Segment a sequence into sequence of sub-sequences by attempting to find the longest non-overlapping
		/// sub-sequences.
		/// </summary>
		/// <remarks>
		/// Segment a sequence into sequence of sub-sequences by attempting to find the longest non-overlapping
		/// sub-sequences.  Non-matched parts will be included as a match with a null value.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <returns>List of segments (as matches) sorted by start position</returns>
		public virtual IList<Match<K, V>> Segment(IList<K> list)
		{
			return Segment(list, 0, list.Count);
		}

		/// <summary>
		/// Segment a sequence into sequence of sub-sequences by attempting to find the longest non-overlapping
		/// sub-sequences.
		/// </summary>
		/// <remarks>
		/// Segment a sequence into sequence of sub-sequences by attempting to find the longest non-overlapping
		/// sub-sequences.  Non-matched parts will be included as a match with a null value.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <param name="start">start index to start search at</param>
		/// <param name="end">end index (exclusive) to end search at</param>
		/// <returns>List of segments (as matches) sorted by start position</returns>
		public virtual IList<Match<K, V>> Segment(IList<K> list, int start, int end)
		{
			return Segment(list, start, end, MatchLengthScorer);
		}

		/// <summary>
		/// Segment a sequence into sequence of sub-sequences by attempting to find the non-overlapping
		/// sub-sequences that comes earlier using the compareFunc.
		/// </summary>
		/// <remarks>
		/// Segment a sequence into sequence of sub-sequences by attempting to find the non-overlapping
		/// sub-sequences that comes earlier using the compareFunc.
		/// Non-matched parts will be included as a match with a null value.
		/// </remarks>
		/// <param name="list">Sequence to search through</param>
		/// <param name="start">start index to start search at</param>
		/// <param name="end">end index (exclusive) to end search at</param>
		/// <param name="compareFunc">
		/// Comparison function to use for evaluating which overlapping sub-sequence to keep.
		/// Earlier sub-sequences based on the comparison function are favored.
		/// </param>
		/// <returns>List of segments (as matches) sorted by start position</returns>
		public virtual IList<Match<K, V>> Segment<_T0>(IList<K> list, int start, int end, IComparator<_T0> compareFunc)
		{
			IList<Match<K, V>> nonOverlapping = FindNonOverlapping(list, start, end, compareFunc);
			IList<Match<K, V>> segments = new List<Match<K, V>>(nonOverlapping.Count);
			int last = 0;
			foreach (Match<K, V> match in nonOverlapping)
			{
				if (match.begin > last)
				{
					// Create empty match and add to segments
					Match<K, V> empty = new Match<K, V>(list.SubList(last, match.begin), null, last, match.begin);
					segments.Add(empty);
				}
				segments.Add(match);
				last = match.end;
			}
			if (list.Count > last)
			{
				Match<K, V> empty = new Match<K, V>(list.SubList(last, list.Count), null, last, list.Count);
				segments.Add(empty);
			}
			return segments;
		}

		/// <summary>
		/// Segment a sequence into sequence of sub-sequences by attempting to maximize the total score
		/// Non-matched parts will be included as a match with a null value.
		/// </summary>
		/// <param name="list">Sequence to search through</param>
		/// <param name="start">start index to start search at</param>
		/// <param name="end">end index (exclusive) to end search at</param>
		/// <param name="scoreFunc">Scoring function indicating how good the match is</param>
		/// <returns>List of segments (as matches) sorted by start position</returns>
		public virtual IList<Match<K, V>> Segment<_T0>(IList<K> list, int start, int end, IToDoubleFunction<_T0> scoreFunc)
		{
			IList<Match<K, V>> nonOverlapping = FindNonOverlapping(list, start, end, scoreFunc);
			IList<Match<K, V>> segments = new List<Match<K, V>>(nonOverlapping.Count);
			int last = 0;
			foreach (Match<K, V> match in nonOverlapping)
			{
				if (match.begin > last)
				{
					// Create empty match and add to segments
					Match<K, V> empty = new Match<K, V>(list.SubList(last, match.begin), null, last, match.begin);
					segments.Add(empty);
				}
				segments.Add(match);
				last = match.end;
			}
			if (list.Count > last)
			{
				Match<K, V> empty = new Match<K, V>(list.SubList(last, list.Count), null, last, list.Count);
				segments.Add(empty);
			}
			return segments;
		}

		public virtual IList<Match<K, V>> Segment<_T0>(IList<K> list, IToDoubleFunction<_T0> scoreFunc)
		{
			return Segment(list, 0, list.Count, scoreFunc);
		}

		/// <summary>Given a list of matches, returns all non-overlapping matches.</summary>
		/// <remarks>
		/// Given a list of matches, returns all non-overlapping matches.
		/// Matches that are longer are preferred, then matches that starts earlier.
		/// </remarks>
		/// <param name="allMatches">List of matches</param>
		/// <returns>List of matches sorted by start position</returns>
		public virtual IList<Match<K, V>> GetNonOverlapping(IList<Match<K, V>> allMatches)
		{
			return GetNonOverlapping(allMatches, MatchLengthEndpointsComparator);
		}

		/// <summary>Given a list of matches, returns all non-overlapping matches.</summary>
		/// <param name="allMatches">List of matches</param>
		/// <param name="compareFunc">
		/// Comparison function to use for evaluating which overlapping sub-sequence to keep.
		/// Earlier sub-sequences based on the comparison function are favored.
		/// </param>
		/// <returns>List of matches sorted by start position</returns>
		public virtual IList<Match<K, V>> GetNonOverlapping<_T0>(IList<Match<K, V>> allMatches, IComparator<_T0> compareFunc)
		{
			if (allMatches.Count > 1)
			{
				IList<Match<K, V>> nonOverlapping = IntervalTree.GetNonOverlapping(allMatches, compareFunc);
				nonOverlapping.Sort(HasIntervalConstants.EndpointsComparator);
				return nonOverlapping;
			}
			else
			{
				return allMatches;
			}
		}

		public virtual IList<Match<K, V>> GetNonOverlapping<_T0>(IList<Match<K, V>> allMatches, IToDoubleFunction<_T0> scoreFunc)
		{
			return IntervalTree.GetNonOverlappingMaxScore(allMatches, scoreFunc);
		}

		protected internal virtual void UpdateAllMatches(TrieMap<K, V> trie, IList<Match<K, V>> matches, IList<K> matched, IList<K> list, int start, int end)
		{
			for (int i = start; i < end; i++)
			{
				UpdateAllMatchesWithStart(trie, matches, matched, list, i, end);
			}
		}

		protected internal virtual void UpdateAllMatchesWithStart(TrieMap<K, V> trie, IList<Match<K, V>> matches, IList<K> matched, IList<K> list, int start, int end)
		{
			if (start > end)
			{
				return;
			}
			if (trie.children != null && start < end)
			{
				K key = list[start];
				TrieMap<K, V> child = trie.children[key];
				if (child != null)
				{
					IList<K> p = new List<K>(matched.Count + 1);
					Sharpen.Collections.AddAll(p, matched);
					p.Add(key);
					UpdateAllMatchesWithStart(child, matches, p, list, start + 1, end);
				}
			}
			if (trie.IsLeaf())
			{
				matches.Add(new Match<K, V>(matched, trie.value, start - matched.Count, start));
			}
		}

		private class PartialApproxMatch<K, V> : ApproxMatch<K, V>
		{
			internal TrieMap<K, V> trie;

			internal int lastMultimatchedMatchedStartIndex = 0;

			internal int lastMultimatchedOriginalStartIndex = 0;

			private PartialApproxMatch()
			{
			}

			private PartialApproxMatch(double cost, TrieMap<K, V> trie, int alignmentLength)
			{
				// Helper class for keeping track of partial matches with TrieMatcher
				this.trie = trie;
				this.cost = cost;
				this.value = (trie != null) ? this.trie.value : null;
				if (alignmentLength > 0)
				{
					this.alignments = new Interval[alignmentLength];
				}
			}

			private TrieMapMatcher.PartialApproxMatch<K, V> WithMatch(IMatchCostFunction<K, V> costFunction, double deltaCost, K t, K k)
			{
				TrieMapMatcher.PartialApproxMatch<K, V> res = new TrieMapMatcher.PartialApproxMatch<K, V>();
				res.matched = matched;
				if (k != null)
				{
					if (res.matched == null)
					{
						res.matched = new List<K>(1);
					}
					else
					{
						res.matched = new List<K>(matched.Count + 1);
						Sharpen.Collections.AddAll(res.matched, matched);
					}
					res.matched.Add(k);
				}
				res.begin = begin;
				res.end = (t != null) ? end + 1 : end;
				res.cost = cost + deltaCost;
				res.trie = (k != null) ? trie.GetChildTrie(k) : trie;
				res.value = (res.trie != null) ? res.trie.value : null;
				res.multimatches = multimatches;
				res.lastMultimatchedMatchedStartIndex = lastMultimatchedMatchedStartIndex;
				res.lastMultimatchedOriginalStartIndex = lastMultimatchedOriginalStartIndex;
				if (res.lastMultimatchedOriginalStartIndex == end && k == null && t != null)
				{
					res.lastMultimatchedOriginalStartIndex++;
				}
				// Update alignments
				if (alignments != null)
				{
					res.alignments = new Interval[alignments.Length];
					System.Array.Copy(alignments, 0, res.alignments, 0, alignments.Length);
					if (k != null && res.end > 0)
					{
						int p = res.end - 1;
						if (res.alignments[p] == null)
						{
							res.alignments[p] = Interval.ToInterval(res.matched.Count - 1, res.matched.Count);
						}
						else
						{
							res.alignments[p] = Interval.ToInterval(res.alignments[p].GetBegin(), res.alignments[p].GetEnd() + 1);
						}
					}
				}
				return res;
			}

			private ApproxMatch<K, V> ToApproxMatch()
			{
				// Makes a copy of this partial approx match that can be returned to the caller
				return new ApproxMatch<K, V>(matched, value, begin, end, multimatches, cost, alignments);
			}

			private TrieMapMatcher.PartialApproxMatch<K, V> WithMatch(IMatchCostFunction<K, V> costFunction, double deltaCost, K t, K k, bool multimatch, TrieMap<K, V> root)
			{
				TrieMapMatcher.PartialApproxMatch<K, V> res = WithMatch(costFunction, deltaCost, t, k);
				if (multimatch && res.matched != null && res.value != null)
				{
					// Update tracking of matched keys and values for multiple entry matches
					if (res.multimatches == null)
					{
						res.multimatches = new List<Match<K, V>>(1);
					}
					else
					{
						res.multimatches = new List<Match<K, V>>(multimatches.Count + 1);
						Sharpen.Collections.AddAll(res.multimatches, multimatches);
					}
					IList<K> newlyMatched = res.matched.SubList(lastMultimatchedMatchedStartIndex, res.matched.Count);
					res.multimatches.Add(new Match<K, V>(newlyMatched, res.value, lastMultimatchedOriginalStartIndex, res.end));
					res.cost += costFunction.MultiMatchDeltaCost(newlyMatched, res.value, multimatches, res.multimatches);
					res.lastMultimatchedMatchedStartIndex = res.matched.Count;
					res.lastMultimatchedOriginalStartIndex = res.end;
					// Reset current value/key being matched
					res.trie = root;
				}
				return res;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (o == null || GetType() != o.GetType())
				{
					return false;
				}
				if (!base.Equals(o))
				{
					return false;
				}
				TrieMapMatcher.PartialApproxMatch that = (TrieMapMatcher.PartialApproxMatch)o;
				if (lastMultimatchedMatchedStartIndex != that.lastMultimatchedMatchedStartIndex)
				{
					return false;
				}
				if (lastMultimatchedOriginalStartIndex != that.lastMultimatchedOriginalStartIndex)
				{
					return false;
				}
				if (trie != null ? !trie.Equals(that.trie) : that.trie != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = base.GetHashCode();
				result = 31 * result + lastMultimatchedMatchedStartIndex;
				result = 31 * result + lastMultimatchedOriginalStartIndex;
				return result;
			}
		}

		private class MatchQueue<K, V>
		{
			private readonly BoundedCostOrderedMap<Match<K, V>, TrieMapMatcher.PartialApproxMatch<K, V>> queue;

			protected internal readonly int maxSize;

			protected internal readonly double maxCost;

			public readonly IToDoubleFunction<TrieMapMatcher.PartialApproxMatch<K, V>> MatchCostFunction = null;

			public MatchQueue(int maxSize, double maxCost)
			{
				this.maxSize = maxSize;
				this.maxCost = maxCost;
				this.queue = new BoundedCostOrderedMap<Match<K, V>, TrieMapMatcher.PartialApproxMatch<K, V>>(MatchCostFunction, maxSize, maxCost);
			}

			public virtual void Add(TrieMapMatcher.PartialApproxMatch<K, V> pam)
			{
				IList<Match<K, V>> multiMatchesWithoutOffsets = null;
				if (pam.multimatches != null)
				{
					multiMatchesWithoutOffsets = new List<Match<K, V>>(pam.multimatches.Count);
					foreach (Match<K, V> m in pam.multimatches)
					{
						multiMatchesWithoutOffsets.Add(new Match<K, V>(m.matched, m.value, 0, 0));
					}
				}
				Match<K, V> m_1 = new MultiMatch<K, V>(pam.matched, pam.value, pam.begin, pam.end, multiMatchesWithoutOffsets);
				queue[m_1] = pam;
			}

			public virtual double TopCost()
			{
				return queue.TopCost();
			}

			public virtual int Size()
			{
				return queue.Count;
			}

			public virtual bool IsEmpty()
			{
				return queue.IsEmpty();
			}

			public virtual IList<TrieMapMatcher.PartialApproxMatch<K, V>> ToSortedList()
			{
				IList<TrieMapMatcher.PartialApproxMatch<K, V>> res = queue.ValuesList();
				res.Sort(TrieMapMatcher.PartialMatchComparator<K, V>());
				return res;
			}
		}

		private class MultiMatchQueue<K, V> : TrieMapMatcher.MatchQueue<K, V>
		{
			private readonly IDictionary<int, BoundedCostOrderedMap<Match<K, V>, TrieMapMatcher.PartialApproxMatch<K, V>>> multimatchQueues;

			public MultiMatchQueue(int maxSize, double maxCost)
				: base(maxSize, maxCost)
			{
				this.multimatchQueues = new Dictionary<int, BoundedCostOrderedMap<Match<K, V>, TrieMapMatcher.PartialApproxMatch<K, V>>>();
			}

			public override void Add(TrieMapMatcher.PartialApproxMatch<K, V> pam)
			{
				Match<K, V> m = new MultiMatch<K, V>(pam.matched, pam.value, pam.begin, pam.end, pam.multimatches);
				int key = (pam.multimatches != null) ? pam.multimatches.Count : 0;
				if (pam.value == null)
				{
					key = key + 1;
				}
				BoundedCostOrderedMap<Match<K, V>, TrieMapMatcher.PartialApproxMatch<K, V>> mq = multimatchQueues[key];
				if (mq == null)
				{
					multimatchQueues[key] = mq = new BoundedCostOrderedMap<Match<K, V>, TrieMapMatcher.PartialApproxMatch<K, V>>(MatchCostFunction, maxSize, maxCost);
				}
				mq[m] = pam;
			}

			public override double TopCost()
			{
				double cost = double.MinValue;
				foreach (BoundedCostOrderedMap<Match<K, V>, TrieMapMatcher.PartialApproxMatch<K, V>> q in multimatchQueues.Values)
				{
					if (q.TopCost() > cost)
					{
						cost = q.TopCost();
					}
				}
				return cost;
			}

			public override int Size()
			{
				int sz = 0;
				foreach (BoundedCostOrderedMap<Match<K, V>, TrieMapMatcher.PartialApproxMatch<K, V>> q in multimatchQueues.Values)
				{
					sz += q.Count;
				}
				return sz;
			}

			public override IList<TrieMapMatcher.PartialApproxMatch<K, V>> ToSortedList()
			{
				IList<TrieMapMatcher.PartialApproxMatch<K, V>> all = new List<TrieMapMatcher.PartialApproxMatch<K, V>>(Size());
				foreach (BoundedCostOrderedMap<Match<K, V>, TrieMapMatcher.PartialApproxMatch<K, V>> q in multimatchQueues.Values)
				{
					Sharpen.Collections.AddAll(all, q.ValuesList());
				}
				all.Sort(TrieMapMatcher.PartialMatchComparator<K, V>());
				return all;
			}
		}

		private bool AddToQueue(TrieMapMatcher.MatchQueue<K, V> queue, TrieMapMatcher.MatchQueue<K, V> best, IMatchCostFunction<K, V> costFunction, TrieMapMatcher.PartialApproxMatch<K, V> pam, K a, K b, bool multimatch, bool complete)
		{
			double deltaCost = costFunction.Cost(a, b, pam.GetMatchedLength());
			double newCost = pam.cost + deltaCost;
			if (queue.maxCost != double.MaxValue && newCost > queue.maxCost)
			{
				return false;
			}
			if (best.Size() >= queue.maxSize && newCost > best.TopCost())
			{
				return false;
			}
			TrieMapMatcher.PartialApproxMatch<K, V> npam = pam.WithMatch(costFunction, deltaCost, a, b);
			if (!multimatch || (npam.trie != null && npam.trie.children != null))
			{
				if (!multimatch && complete && npam.value != null)
				{
					best.Add(npam);
				}
				queue.Add(npam);
			}
			if (multimatch && npam.value != null)
			{
				npam = pam.WithMatch(costFunction, deltaCost, a, b, multimatch, rootWithDelimiter);
				if (complete && npam.value != null)
				{
					best.Add(npam);
				}
				queue.Add(npam);
			}
			return true;
		}

		public static IMatchCostFunction<K, V> DefaultCost<K, V>()
		{
			return ErasureUtils.UncheckedCast(DefaultCost);
		}

		public static IComparator<TrieMapMatcher.PartialApproxMatch<K, V>> PartialMatchComparator<K, V>()
		{
			return ErasureUtils.UncheckedCast(PartialMatchComparator);
		}

		private static readonly IMatchCostFunction DefaultCost = new ExactMatchCost();

		private static readonly IComparator<TrieMapMatcher.PartialApproxMatch> PartialMatchComparator = null;
	}
}
