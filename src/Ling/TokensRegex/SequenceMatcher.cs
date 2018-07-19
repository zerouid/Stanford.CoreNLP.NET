using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>A generic sequence matcher.</summary>
	/// <remarks>
	/// A generic sequence matcher.
	/// Similar to Java's
	/// <c>Matcher</c>
	/// except it matches sequences over an arbitrary type
	/// <c>T</c>
	/// instead of characters.
	/// For a type
	/// <c>T</c>
	/// to be matchable, it has to have a corresponding
	/// <c>NodePattern&lt;T&gt;</c>
	/// that indicates
	/// whether a node is matched or not.
	/// A matcher is created as follows:
	/// <c>
	/// SequencePattern&lt;T&gt; p = SequencePattern&lt;T&gt;.compile("...");
	/// SequencePattern&lt;T&gt; m = p.getMatcher(List&lt;T&gt; sequence);
	/// </c>
	/// Functions for searching
	/// <pre>
	/// <c>
	/// boolean matches()
	/// boolean find()
	/// boolean find(int start)
	/// </c>
	/// </pre>
	/// Functions for retrieving matched patterns
	/// <pre>
	/// <c>
	/// int groupCount()
	/// List&lt;T&gt; groupNodes(), List&lt;T&gt; groupNodes(int g)
	/// String group(), String group(int g)
	/// int start(), int start(int g), int end(), int end(int g)
	/// </c>
	/// </pre>
	/// Functions for replacing
	/// <pre>
	/// <c>
	/// List&lt;T&gt; replaceFirst(List&lt;T&gt; seq), List replaceAll(List&lt;T&gt; seq)
	/// List&lt;T&gt; replaceFirstExtended(List&lt;MatchReplacement&lt;T&gt;&gt; seq), List&lt;T&gt; replaceAllExtended(List&lt;MatchReplacement&lt;T&gt;&gt; seq)
	/// </c>
	/// </pre>
	/// Functions for defining the region of the sequence to search over
	/// (default region is entire sequence)
	/// <pre>
	/// <c>
	/// void region(int start, int end)
	/// int regionStart()
	/// int regionEnd()
	/// </c>
	/// </pre>
	/// NOTE: When find is used, matches are attempted starting from the specified start index of the sequence
	/// The match with the earliest starting index is returned.
	/// </remarks>
	/// <author>Angel Chang</author>
	public class SequenceMatcher<T> : BasicSequenceMatchResult<T>
	{
		private static readonly Logger logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.Ling.Tokensregex.SequenceMatcher).FullName);

		internal bool includeEmptyMatches = false;

		internal bool matchingCompleted = false;

		internal bool matched = false;

		internal bool matchWithResult = false;

		internal int nextMatchStart = 0;

		internal int regionStart = 0;

		internal int regionEnd = -1;

		/// <summary>
		/// Type of search to perform
		/// <ul>
		/// <li>FIND_NONOVERLAPPING - Find nonoverlapping matches (default)</li>
		/// <li>FIND_ALL - Find all potential matches
		/// Greedy/reluctant quantifiers are not enforced
		/// (perhaps should add syntax where some of them are enforced...)</li>
		/// </ul>
		/// </summary>
		public enum FindType
		{
			FindNonoverlapping,
			FindAll
		}

		internal SequenceMatcher.FindType findType = SequenceMatcher.FindType.FindNonoverlapping;

		internal IEnumerator<int> curMatchIter = null;

		internal SequenceMatcher.MatchedStates<T> curMatchStates = null;

		internal ICollection<string> prevMatchedSignatures = new HashSet<string>();

		internal int branchLimit = 32;

		protected internal SequenceMatcher(SequencePattern<T> pattern, IList<T> elements)
		{
			// If result of matches should be kept
			// TODO: Check and fix implementation for FIND_ALL
			// For FIND_ALL
			// Branching limit for searching with back tracking. Higher value makes the search faster but uses more memory.
			this.pattern = pattern;
			// NOTE: It is important elements DO NOT change as we do matches
			// TODO: Should we just make a copy of the elements?
			this.elements = elements;
			if (elements == null)
			{
				throw new ArgumentException("Cannot match against null elements");
			}
			this.regionEnd = elements.Count;
			this.priority = pattern.priority;
			this.score = pattern.weight;
			this.varGroupBindings = pattern.varGroupBindings;
			matchedGroups = new BasicSequenceMatchResult.MatchedGroup[pattern.totalGroups];
		}

		public virtual void SetBranchLimit(int blimit)
		{
			this.branchLimit = blimit;
		}

		/// <summary>Interface that specifies what to replace a matched pattern with</summary>
		/// <?/>
		public interface IMatchReplacement<T>
		{
			/// <summary>Append to replacement list</summary>
			/// <param name="match">Current matched sequence</param>
			/// <param name="list">replacement list</param>
			void Append(ISequenceMatchResult<T> match, IList list);
		}

		/// <summary>Replacement item is a sequence of items</summary>
		/// <?/>
		public class BasicMatchReplacement<T> : SequenceMatcher.IMatchReplacement<T>
		{
			internal IList<T> replacement;

			[SafeVarargs]
			public BasicMatchReplacement(params T[] replacement)
			{
				this.replacement = Arrays.AsList(replacement);
			}

			public BasicMatchReplacement(IList<T> replacement)
			{
				this.replacement = replacement;
			}

			/// <summary>Append to replacement list our list of replacement items</summary>
			/// <param name="match">Current matched sequence</param>
			/// <param name="list">replacement list</param>
			public virtual void Append(ISequenceMatchResult<T> match, IList list)
			{
				Sharpen.Collections.AddAll(list, replacement);
			}
		}

		/// <summary>Replacement item is a matched group specified with a group name</summary>
		/// <?/>
		public class NamedGroupMatchReplacement<T> : SequenceMatcher.IMatchReplacement<T>
		{
			internal string groupName;

			public NamedGroupMatchReplacement(string groupName)
			{
				this.groupName = groupName;
			}

			/// <summary>Append to replacement list the matched group with the specified group name</summary>
			/// <param name="match">Current matched sequence</param>
			/// <param name="list">replacement list</param>
			public virtual void Append(ISequenceMatchResult<T> match, IList list)
			{
				Sharpen.Collections.AddAll(list, match.GroupNodes(groupName));
			}
		}

		/// <summary>Replacement item is a matched group specified with a group id</summary>
		/// <?/>
		public class GroupMatchReplacement<T> : SequenceMatcher.IMatchReplacement<T>
		{
			internal int group;

			public GroupMatchReplacement(int group)
			{
				this.group = group;
			}

			/// <summary>Append to replacement list the matched group with the specified group id</summary>
			/// <param name="match">Current matched sequence</param>
			/// <param name="list">replacement list</param>
			public virtual void Append(ISequenceMatchResult<T> match, IList list)
			{
				Sharpen.Collections.AddAll(list, match.GroupNodes(group));
			}
		}

		/// <summary>
		/// Replaces all occurrences of the pattern with the specified list
		/// of replacement items (can include matched groups).
		/// </summary>
		/// <param name="replacement">What to replace the matched sequence with</param>
		/// <returns>New list with all occurrences of the pattern replaced</returns>
		/// <seealso cref="SequenceMatcher{T}.ReplaceFirst(System.Collections.IList{E})"/>
		/// <seealso cref="SequenceMatcher{T}.ReplaceFirstExtended(System.Collections.IList{E})"/>
		/// <seealso cref="SequenceMatcher{T}.ReplaceAllExtended(System.Collections.IList{E})"/>
		public virtual IList<T> ReplaceAllExtended(IList<SequenceMatcher.IMatchReplacement<T>> replacement)
		{
			IList<T> res = new List<T>();
			SequenceMatcher.FindType oldFindType = findType;
			findType = SequenceMatcher.FindType.FindNonoverlapping;
			int index = 0;
			while (Find())
			{
				// Copy from current index to found index
				Sharpen.Collections.AddAll(res, Elements().SubList(index, Start()));
				foreach (SequenceMatcher.IMatchReplacement<T> r in replacement)
				{
					r.Append(this, res);
				}
				index = End();
			}
			Sharpen.Collections.AddAll(res, Elements().SubList(index, Elements().Count));
			findType = oldFindType;
			return res;
		}

		/// <summary>
		/// Replaces the first occurrence of the pattern with the specified list
		/// of replacement items (can include matched groups).
		/// </summary>
		/// <param name="replacement">What to replace the matched sequence with</param>
		/// <returns>New list with the first occurrence of the pattern replaced</returns>
		/// <seealso cref="SequenceMatcher{T}.ReplaceFirst(System.Collections.IList{E})"/>
		/// <seealso cref="SequenceMatcher{T}.ReplaceAll(System.Collections.IList{E})"/>
		/// <seealso cref="SequenceMatcher{T}.ReplaceAllExtended(System.Collections.IList{E})"/>
		public virtual IList<T> ReplaceFirstExtended(IList<SequenceMatcher.IMatchReplacement<T>> replacement)
		{
			IList<T> res = new List<T>();
			SequenceMatcher.FindType oldFindType = findType;
			findType = SequenceMatcher.FindType.FindNonoverlapping;
			int index = 0;
			if (Find())
			{
				// Copy from current index to found index
				Sharpen.Collections.AddAll(res, Elements().SubList(index, Start()));
				foreach (SequenceMatcher.IMatchReplacement<T> r in replacement)
				{
					r.Append(this, res);
				}
				index = End();
			}
			Sharpen.Collections.AddAll(res, Elements().SubList(index, Elements().Count));
			findType = oldFindType;
			return res;
		}

		/// <summary>Replaces all occurrences of the pattern with the specified list.</summary>
		/// <remarks>
		/// Replaces all occurrences of the pattern with the specified list.
		/// Use
		/// <see cref="SequenceMatcher{T}.ReplaceAllExtended(System.Collections.IList{E})"/>
		/// to replace with matched groups.
		/// </remarks>
		/// <param name="replacement">What to replace the matched sequence with</param>
		/// <returns>New list with all occurrences of the pattern replaced</returns>
		/// <seealso cref="SequenceMatcher{T}.ReplaceAllExtended(System.Collections.IList{E})"/>
		/// <seealso cref="SequenceMatcher{T}.ReplaceFirst(System.Collections.IList{E})"/>
		/// <seealso cref="SequenceMatcher{T}.ReplaceFirstExtended(System.Collections.IList{E})"/>
		public virtual IList<T> ReplaceAll(IList<T> replacement)
		{
			IList<T> res = new List<T>();
			SequenceMatcher.FindType oldFindType = findType;
			findType = SequenceMatcher.FindType.FindNonoverlapping;
			int index = 0;
			while (Find())
			{
				// Copy from current index to found index
				Sharpen.Collections.AddAll(res, Elements().SubList(index, Start()));
				Sharpen.Collections.AddAll(res, replacement);
				index = End();
			}
			Sharpen.Collections.AddAll(res, Elements().SubList(index, Elements().Count));
			findType = oldFindType;
			return res;
		}

		/// <summary>Replaces the first occurrence of the pattern with the specified list.</summary>
		/// <remarks>
		/// Replaces the first occurrence of the pattern with the specified list.
		/// Use
		/// <see cref="SequenceMatcher{T}.ReplaceFirstExtended(System.Collections.IList{E})"/>
		/// to replace with matched groups.
		/// </remarks>
		/// <param name="replacement">What to replace the matched sequence with</param>
		/// <returns>New list with the first occurrence of the pattern replaced</returns>
		/// <seealso cref="SequenceMatcher{T}.ReplaceAll(System.Collections.IList{E})"/>
		/// <seealso cref="SequenceMatcher{T}.ReplaceAllExtended(System.Collections.IList{E})"/>
		/// <seealso cref="SequenceMatcher{T}.ReplaceFirstExtended(System.Collections.IList{E})"/>
		public virtual IList<T> ReplaceFirst(IList<T> replacement)
		{
			IList<T> res = new List<T>();
			SequenceMatcher.FindType oldFindType = findType;
			findType = SequenceMatcher.FindType.FindNonoverlapping;
			int index = 0;
			if (Find())
			{
				// Copy from current index to found index
				Sharpen.Collections.AddAll(res, Elements().SubList(index, Start()));
				Sharpen.Collections.AddAll(res, replacement);
				index = End();
			}
			Sharpen.Collections.AddAll(res, Elements().SubList(index, Elements().Count));
			findType = oldFindType;
			return res;
		}

		public virtual SequenceMatcher.FindType GetFindType()
		{
			return findType;
		}

		public virtual void SetFindType(SequenceMatcher.FindType findType)
		{
			this.findType = findType;
		}

		public virtual bool IsMatchWithResult()
		{
			return matchWithResult;
		}

		public virtual void SetMatchWithResult(bool matchWithResult)
		{
			this.matchWithResult = matchWithResult;
		}

		/// <summary>Reset the matcher and then searches for pattern at the specified start index</summary>
		/// <param name="start">- Index at which to start the search</param>
		/// <returns>true if a match is found (false otherwise)</returns>
		/// <exception cref="System.IndexOutOfRangeException">
		/// if start is
		/// <literal>&lt;</literal>
		/// 0 or larger then the size of the sequence
		/// </exception>
		/// <seealso cref="SequenceMatcher{T}.Find()"/>
		public virtual bool Find(int start)
		{
			if (start < 0 || start > elements.Count)
			{
				throw new IndexOutOfRangeException("Invalid region start=" + start + ", need to be between 0 and " + elements.Count);
			}
			Reset();
			return Find(start, false);
		}

		protected internal virtual bool Find(int start, bool matchStart)
		{
			bool done = false;
			while (!done)
			{
				bool res = Find0(start, matchStart);
				if (res)
				{
					bool empty = this.Group().IsEmpty();
					if (!empty || includeEmptyMatches)
					{
						return res;
					}
					else
					{
						start = start + 1;
					}
				}
				done = !res;
			}
			return false;
		}

		protected internal virtual bool Find0(int start, bool matchStart)
		{
			bool match = false;
			matched = false;
			matchingCompleted = false;
			if (matchStart)
			{
				match = FindMatchStart(start, false);
			}
			else
			{
				for (int i = start; i < regionEnd; i++)
				{
					match = FindMatchStart(i, false);
					if (match)
					{
						break;
					}
				}
			}
			matched = match;
			matchingCompleted = true;
			if (matched)
			{
				nextMatchStart = (findType == SequenceMatcher.FindType.FindNonoverlapping) ? End() : Start() + 1;
			}
			else
			{
				nextMatchStart = -1;
			}
			return match;
		}

		/// <summary>
		/// Searches for pattern in the region starting
		/// at the next index
		/// </summary>
		/// <returns>true if a match is found (false otherwise)</returns>
		private bool FindNextNonOverlapping()
		{
			if (nextMatchStart < 0)
			{
				return false;
			}
			return Find(nextMatchStart, false);
		}

		private bool FindNextAll()
		{
			if (curMatchIter != null && curMatchIter.MoveNext())
			{
				while (curMatchIter.MoveNext())
				{
					int next = curMatchIter.Current;
					curMatchStates.SetMatchedGroups(next);
					string sig = GetMatchedSignature();
					if (!prevMatchedSignatures.Contains(sig))
					{
						prevMatchedSignatures.Add(sig);
						return true;
					}
				}
			}
			if (nextMatchStart < 0)
			{
				return false;
			}
			prevMatchedSignatures.Clear();
			bool matched = Find(nextMatchStart, false);
			if (matched)
			{
				ICollection<int> matchedBranches = curMatchStates.GetMatchIndices();
				curMatchIter = matchedBranches.GetEnumerator();
				int next = curMatchIter.Current;
				curMatchStates.SetMatchedGroups(next);
				prevMatchedSignatures.Add(GetMatchedSignature());
			}
			return matched;
		}

		/// <summary>Applies the matcher and returns all non overlapping matches</summary>
		/// <returns>a Iterable of match results</returns>
		public virtual IEnumerable<ISequenceMatchResult<T>> FindAllNonOverlapping()
		{
			IEnumerator<ISequenceMatchResult<T>> iter = new _IEnumerator_413(this);
			return new IterableIterator<ISequenceMatchResult<T>>(iter);
		}

		private sealed class _IEnumerator_413 : IEnumerator<ISequenceMatchResult<T>>
		{
			public _IEnumerator_413(SequenceMatcher<T> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			internal ISequenceMatchResult<T> next;

			private ISequenceMatchResult<T> GetNext()
			{
				bool found = this._enclosing.Find();
				if (found)
				{
					return this._enclosing.ToBasicSequenceMatchResult();
				}
				else
				{
					return null;
				}
			}

			public bool MoveNext()
			{
				if (this.next == null)
				{
					this.next = this.GetNext();
					return (this.next != null);
				}
				else
				{
					return true;
				}
			}

			public ISequenceMatchResult<T> Current
			{
				get
				{
					if (!this.MoveNext())
					{
						throw new NoSuchElementException();
					}
					ISequenceMatchResult<T> res = this.next;
					this.next = null;
					return res;
				}
			}

			public void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly SequenceMatcher<T> _enclosing;
		}

		/// <summary>Searches for the next occurrence of the pattern</summary>
		/// <returns>true if a match is found (false otherwise)</returns>
		/// <seealso cref="SequenceMatcher{T}.Find(int)"/>
		public virtual bool Find()
		{
			switch (findType)
			{
				case SequenceMatcher.FindType.FindNonoverlapping:
				{
					return FindNextNonOverlapping();
				}

				case SequenceMatcher.FindType.FindAll:
				{
					return FindNextAll();
				}

				default:
				{
					throw new NotSupportedException("Unsupported findType " + findType);
				}
			}
		}

		protected internal virtual bool FindMatchStart(int start, bool matchAllTokens)
		{
			switch (findType)
			{
				case SequenceMatcher.FindType.FindNonoverlapping:
				{
					return FindMatchStartBacktracking(start, matchAllTokens);
				}

				case SequenceMatcher.FindType.FindAll:
				{
					// TODO: Should use backtracking here too, need to keep track of todo stack
					// so we can recover after finding a match
					return FindMatchStartNoBacktracking(start, matchAllTokens);
				}

				default:
				{
					throw new NotSupportedException("Unsupported findType " + findType);
				}
			}
		}

		// Does not do backtracking - alternative matches are stored as we go
		protected internal virtual bool FindMatchStartNoBacktracking(int start, bool matchAllTokens)
		{
			bool matchAll = true;
			SequenceMatcher.MatchedStates<T> cStates = GetStartStates();
			cStates.matchLongest = matchAllTokens;
			// Save cStates for FIND_ALL ....
			curMatchStates = cStates;
			for (int i = start; i < regionEnd; i++)
			{
				bool match = cStates.Match(i);
				if (cStates == null || cStates.Size() == 0)
				{
					break;
				}
				if (!matchAllTokens)
				{
					if ((matchAll && cStates.IsAllMatch()) || (!matchAll && cStates.IsMatch()))
					{
						cStates.CompleteMatch();
						return true;
					}
				}
			}
			cStates.CompleteMatch();
			return cStates.IsMatch();
		}

		// Does some backtracking...
		protected internal virtual bool FindMatchStartBacktracking(int start, bool matchAllTokens)
		{
			bool matchAll = true;
			Stack<SequenceMatcher.MatchedStates> todo = new Stack<SequenceMatcher.MatchedStates>();
			SequenceMatcher.MatchedStates cStates = GetStartStates();
			cStates.matchLongest = matchAllTokens;
			cStates.curPosition = start - 1;
			todo.Push(cStates);
			while (!todo.Empty())
			{
				cStates = todo.Pop();
				int s = cStates.curPosition + 1;
				for (int i = s; i < regionEnd; i++)
				{
					if (Thread.Interrupted())
					{
						throw new RuntimeInterruptedException();
					}
					cStates.Match(i);
					if (cStates.Size() == 0)
					{
						break;
					}
					if (!matchAllTokens)
					{
						if ((matchAll && cStates.IsAllMatch()) || (!matchAll && cStates.IsMatch()))
						{
							cStates.CompleteMatch();
							return true;
						}
					}
					if (branchLimit >= 0 && cStates.BranchSize() > branchLimit)
					{
						SequenceMatcher.MatchedStates s2 = cStates.Split(branchLimit);
						todo.Push(s2);
					}
				}
				if (cStates.IsMatch())
				{
					cStates.CompleteMatch();
					return true;
				}
				cStates.Clean();
			}
			return false;
		}

		/// <summary>Checks if the pattern matches the entire sequence</summary>
		/// <returns>true if the entire sequence is matched (false otherwise)</returns>
		/// <seealso cref="SequenceMatcher{T}.Find()"/>
		public virtual bool Matches()
		{
			matched = false;
			matchingCompleted = false;
			bool status = FindMatchStart(0, true);
			if (status)
			{
				// Check if entire region is matched
				status = ((matchedGroups[0].matchBegin == regionStart) && (matchedGroups[0].matchEnd == regionEnd));
			}
			matchingCompleted = true;
			matched = status;
			return status;
		}

		private void ClearMatched()
		{
			for (int i = 0; i < matchedGroups.Length; i++)
			{
				matchedGroups[i] = null;
			}
			if (matchedResults != null)
			{
				for (int i_1 = 0; i_1 < matchedResults.Length; i_1++)
				{
					matchedResults[i_1] = null;
				}
			}
		}

		private string GetStateMessage()
		{
			if (!matchingCompleted)
			{
				return "Matching not completed";
			}
			else
			{
				if (!matched)
				{
					return "No match found";
				}
				else
				{
					return "Match successful";
				}
			}
		}

		/// <summary>Set region to search in</summary>
		/// <param name="start">- start index</param>
		/// <param name="end">- end index (exclusive)</param>
		public virtual void Region(int start, int end)
		{
			if (start < 0 || start > elements.Count)
			{
				throw new IndexOutOfRangeException("Invalid region start=" + start + ", need to be between 0 and " + elements.Count);
			}
			if (end < 0 || end > elements.Count)
			{
				throw new IndexOutOfRangeException("Invalid region end=" + end + ", need to be between 0 and " + elements.Count);
			}
			if (start > end)
			{
				throw new IndexOutOfRangeException("Invalid region end=" + end + ", need to be larger then start=" + start);
			}
			this.regionStart = start;
			this.nextMatchStart = start;
			this.regionEnd = end;
		}

		public virtual int RegionEnd()
		{
			return regionEnd;
		}

		public virtual int RegionStart()
		{
			return regionStart;
		}

		/// <summary>Returns a copy of the current match results.</summary>
		/// <remarks>
		/// Returns a copy of the current match results.  Use this method
		/// to save away match results for later use, since future operations
		/// using the SequenceMatcher changes the match results.
		/// </remarks>
		/// <returns>Copy of the the current match results</returns>
		public override BasicSequenceMatchResult<T> ToBasicSequenceMatchResult()
		{
			if (matchingCompleted && matched)
			{
				return base.ToBasicSequenceMatchResult();
			}
			else
			{
				string message = GetStateMessage();
				throw new InvalidOperationException(message);
			}
		}

		public override int Start(int group)
		{
			if (matchingCompleted && matched)
			{
				return base.Start(group);
			}
			else
			{
				string message = GetStateMessage();
				throw new InvalidOperationException(message);
			}
		}

		public override int End(int group)
		{
			if (matchingCompleted && matched)
			{
				return base.End(group);
			}
			else
			{
				string message = GetStateMessage();
				throw new InvalidOperationException(message);
			}
		}

		public override IList<T> GroupNodes(int group)
		{
			if (matchingCompleted && matched)
			{
				return base.GroupNodes(group);
			}
			else
			{
				string message = GetStateMessage();
				throw new InvalidOperationException(message);
			}
		}

		public override object GroupValue(int group)
		{
			if (matchingCompleted && matched)
			{
				return base.GroupValue(group);
			}
			else
			{
				string message = GetStateMessage();
				throw new InvalidOperationException(message);
			}
		}

		public override SequenceMatchResult.MatchedGroupInfo<T> GroupInfo(int group)
		{
			if (matchingCompleted && matched)
			{
				return base.GroupInfo(group);
			}
			else
			{
				string message = GetStateMessage();
				throw new InvalidOperationException(message);
			}
		}

		public override IList<object> GroupMatchResults(int group)
		{
			if (matchingCompleted && matched)
			{
				return base.GroupMatchResults(group);
			}
			else
			{
				string message = GetStateMessage();
				throw new InvalidOperationException(message);
			}
		}

		public override object GroupMatchResult(int group, int index)
		{
			if (matchingCompleted && matched)
			{
				return base.GroupMatchResult(group, index);
			}
			else
			{
				string message = GetStateMessage();
				throw new InvalidOperationException(message);
			}
		}

		public override object NodeMatchResult(int index)
		{
			if (matchingCompleted && matched)
			{
				return base.NodeMatchResult(index);
			}
			else
			{
				string message = GetStateMessage();
				throw new InvalidOperationException(message);
			}
		}

		/// <summary>
		/// Clears matcher
		/// - Clears matched groups, reset region to be entire sequence
		/// </summary>
		public virtual void Reset()
		{
			regionStart = 0;
			regionEnd = elements.Count;
			nextMatchStart = 0;
			matchingCompleted = false;
			matched = false;
			ClearMatched();
			// Clearing for FIND_ALL
			prevMatchedSignatures.Clear();
			curMatchIter = null;
			curMatchStates = null;
		}

		/// <summary>Returns the ith element</summary>
		/// <param name="i">- index</param>
		/// <returns>ith element</returns>
		public virtual T Get(int i)
		{
			return elements[i];
		}

		/// <summary>Returns a non-null MatchedStates, which has a non-empty states list inside.</summary>
		private SequenceMatcher.MatchedStates<T> GetStartStates()
		{
			return new SequenceMatcher.MatchedStates<T>(this, pattern.root);
		}

		/// <summary>Contains information about a branch of running the NFA matching</summary>
		private class BranchState
		{
			internal int bid;

			internal SequenceMatcher.BranchState parent;

			internal IDictionary<int, BasicSequenceMatchResult.MatchedGroup> matchedGroups;

			internal IDictionary<int, object> matchedResults;

			internal IDictionary<SequencePattern.State, object> matchStateInfo;

			internal ICollection<int> bidsToCollapse;

			internal ICollection<int> collapsedBids;

			public BranchState(int bid)
				: this(bid, null)
			{
			}

			public BranchState(int bid, SequenceMatcher.BranchState parent)
			{
				// Branch id
				// Parent branch state
				// Map of group id to matched group
				// Map of sequence index id to matched node result
				// Map of state to object storing information about the state for this branch of execution
				// Used for states corresponding to
				//    repeating patterns: key is RepeatState, object is Pair<Integer,Boolean>
				//                        pair indicates sequence index and whether the match was complete
				//    multinode patterns: key is MultiNodePatternState, object is Interval<Integer>
				//                        the interval indicates the start and end node indices for the multinode pattern
				//    conjunction patterns: key is ConjStartState, object is ConjMatchStateInfo
				//Map<SequencePattern.State, Pair<Integer,Boolean>> matchStateCount;
				// Branch ids to collapse together with this branch
				// Used for conjunction states, which requires multiple paths
				// through the NFA to hold
				// Set of Branch ids that has already been collapsed ...
				// assumes that after being collapsed no more collapsing required
				this.bid = bid;
				this.parent = parent;
				if (parent != null)
				{
					if (parent.matchedGroups != null)
					{
						matchedGroups = new LinkedHashMap<int, BasicSequenceMatchResult.MatchedGroup>(parent.matchedGroups);
					}
					if (parent.matchedResults != null)
					{
						matchedResults = new LinkedHashMap<int, object>(parent.matchedResults);
					}
					/*        if (parent.matchStateCount != null) {
					matchStateCount = new LinkedHashMap<SequencePattern.State, Pair<Integer,Boolean>>(parent.matchStateCount);
					}      */
					if (parent.matchStateInfo != null)
					{
						matchStateInfo = new LinkedHashMap<SequencePattern.State, object>(parent.matchStateInfo);
					}
					if (parent.bidsToCollapse != null)
					{
						bidsToCollapse = new ArraySet<int>(parent.bidsToCollapse.Count);
						Sharpen.Collections.AddAll(bidsToCollapse, parent.bidsToCollapse);
					}
					if (parent.collapsedBids != null)
					{
						collapsedBids = new ArraySet<int>(parent.collapsedBids.Count);
						Sharpen.Collections.AddAll(collapsedBids, parent.collapsedBids);
					}
				}
			}

			// Add to list of related branch ids that we would like to keep...
			private void UpdateKeepBids(BitSet bids)
			{
				if (matchStateInfo != null)
				{
					// TODO: Make values of matchStateInfo more organized (implement some interface) so we don't
					// need this kind of specialized code
					foreach (SequencePattern.State s in matchStateInfo.Keys)
					{
						if (s is SequencePattern.ConjStartState)
						{
							SequencePattern.ConjMatchStateInfo info = (SequencePattern.ConjMatchStateInfo)matchStateInfo[s];
							info.UpdateKeepBids(bids);
						}
					}
				}
			}

			private void AddBidsToCollapse(int[] bids)
			{
				if (bidsToCollapse == null)
				{
					bidsToCollapse = new ArraySet<int>(bids.Length);
				}
				foreach (int b in bids)
				{
					if (b != bid)
					{
						bidsToCollapse.Add(b);
					}
				}
			}

			private void AddMatchedGroups(IDictionary<int, BasicSequenceMatchResult.MatchedGroup> g)
			{
				foreach (int k in g.Keys)
				{
					if (!matchedGroups.Contains(k))
					{
						matchedGroups[k] = g[k];
					}
				}
			}

			private void AddMatchedResults(IDictionary<int, object> res)
			{
				if (res != null)
				{
					foreach (int k in res.Keys)
					{
						if (!matchedResults.Contains(k))
						{
							matchedResults[k] = res[k];
						}
					}
				}
			}
		}

		private class State
		{
			internal int bid;

			internal SequencePattern.State tstate;

			public State(int bid, SequencePattern.State tstate)
			{
				this.bid = bid;
				this.tstate = tstate;
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
				SequenceMatcher.State state = (SequenceMatcher.State)o;
				if (bid != state.bid)
				{
					return false;
				}
				if (tstate != null ? !tstate.Equals(state.tstate) : state.tstate != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = bid;
				result = 31 * result + (tstate != null ? tstate.GetHashCode() : 0);
				return result;
			}
		}

		/// <summary>
		/// Overall information about the branching of paths through the NFA
		/// (maintained for one attempt at matching, with multiple MatchedStates)
		/// </summary>
		internal class BranchStates
		{
			internal HashIndex<Pair<int, int>> bidIndex = new HashIndex<Pair<int, int>>(512);

			internal IDictionary<int, SequenceMatcher.BranchState> branchStates = new Dictionary<int, SequenceMatcher.BranchState>();

			internal ICollection<SequenceMatcher.MatchedStates> activeMatchedStates = new List<SequenceMatcher.MatchedStates>();

			// Index of global branch id to pair of parent branch id and branch index
			// (the branch index is with respect to parent, from 1 to number of branches the parent has)
			// TODO: This index can grow rather large, use index that allows for shrinkage
			//       (has remove function and generate new id every time)
			// Map of branch id to branch state
			//Generics.newHashMap();
			// The activeMatchedStates is only kept to determine what branch states are still needed
			// It's okay if it overly conservative and has more states than needed,
			// And while ideally a set, it's okay to have duplicates (esp if it is a bit faster for normal cases).
			//= Generics.newHashSet();
			/// <summary>
			/// Links specified MatchedStates to us (list of MatchedStates
			/// is used to determine what branch states still need to be kept)
			/// </summary>
			/// <param name="s"/>
			private void Link(SequenceMatcher.MatchedStates s)
			{
				activeMatchedStates.Add(s);
			}

			/// <summary>
			/// Unlinks specified MatchedStates to us (list of MatchedStates
			/// is used to determine what branch states still need to be kept)
			/// </summary>
			/// <param name="s"/>
			private void Unlink(SequenceMatcher.MatchedStates s)
			{
				// Make sure all instances of s are removed
				while (activeMatchedStates.Remove(s))
				{
				}
			}

			protected internal virtual int GetBid(int parent, int child)
			{
				return bidIndex.IndexOf(new Pair<int, int>(parent, child));
			}

			protected internal virtual int NewBid(int parent, int child)
			{
				return bidIndex.AddToIndexUnsafe(new Pair<int, int>(parent, child));
			}

			protected internal virtual int Size()
			{
				return branchStates.Count;
			}

			/// <summary>Removes branch states are are no longer needed</summary>
			private void Condense()
			{
				BitSet keepBidStates = new BitSet();
				//      Set<Integer> curBidSet = new HashSet<Integer>();//Generics.newHashSet();
				//      Set<Integer> keepBidStates = new HashSet<Integer>();//Generics.newHashSet();
				foreach (SequenceMatcher.MatchedStates ms in activeMatchedStates)
				{
					// Trim out unneeded states info
					IList<SequenceMatcher.State> states = ms.states;
					if (logger.IsLoggable(Level.Finest))
					{
						logger.Finest("Condense matched state: curPosition=" + ms.curPosition + ", totalTokens=" + ms.matcher.elements.Count + ", nStates=" + states.Count);
					}
					foreach (SequenceMatcher.State state in states)
					{
						keepBidStates.Set(state.bid);
					}
				}
				foreach (SequenceMatcher.MatchedStates ms_1 in activeMatchedStates)
				{
					foreach (SequenceMatcher.State state in (IList<SequenceMatcher.State>)ms_1.states)
					{
						int bid = state.bid;
						SequenceMatcher.BranchState bs = GetBranchState(bid);
						if (bs != null)
						{
							keepBidStates.Set(bs.bid);
							bs.UpdateKeepBids(keepBidStates);
							if (bs.bidsToCollapse != null)
							{
								MergeBranchStates(bs);
							}
						}
					}
				}
				IEnumerator<int> iter = branchStates.Keys.GetEnumerator();
				while (iter.MoveNext())
				{
					int bid = iter.Current;
					if (!keepBidStates.Get(bid))
					{
						if (logger.IsLoggable(Level.Finest))
						{
							logger.Finest("Remove state for bid=" + bid);
						}
						iter.Remove();
					}
				}
			}

			/* note[gabor]: replaced code below with the above
			Collection<Integer> curBidStates = new ArrayList<Integer>(branchStates.keySet());
			for (int bid:curBidStates) {
			if (!keepBidStates.get(bid)) {
			if (logger.isLoggable(Level.FINEST)) {
			logger.finest("Remove state for bid=" + bid);
			}
			branchStates.remove(bid);
			}
			}  */
			// TODO: We should be able to trim some bids from our bidIndex as well....
			/*
			if (bidIndex.size() > 1000) {
			logger.warning("Large bid index of size " + bidIndex.size());
			}
			*/
			/// <summary>
			/// A safe version of
			/// <see cref="GetParents(int, int[])"/>
			/// 
			/// </summary>
			private IList<int> GetParents(int bid)
			{
				IList<int> pids = new List<int>();
				Pair<int, int> p = bidIndex.Get(bid);
				while (p != null && p.First() >= 0)
				{
					pids.Add(p.First());
					p = bidIndex.Get(p.First());
				}
				Java.Util.Collections.Reverse(pids);
				return pids;
			}

			/// <summary>Given a branch id, return a list of parent branches</summary>
			/// <param name="bid">branch id</param>
			/// <returns>list of parent branch ids</returns>
			private IList<int> GetParents(int bid, int[] buffer)
			{
				int index = buffer.Length - 1;
				buffer[index] = bid;
				index -= 1;
				Pair<int, int> p = bidIndex.Get(bid);
				while (p != null && p.First() >= 0)
				{
					buffer[index] = p.first;
					index -= 1;
					if (index < 0)
					{
						return GetParents(bid);
					}
					// optimization failed -- back off to the old version
					p = bidIndex.Get(p.First());
				}
				return Arrays.AsList(buffer).SubList(index + 1, buffer.Length);
			}

			/// <summary>
			/// Returns the branch state for a given branch id
			/// (the appropriate ancestor branch state is returned if
			/// there is no branch state associated with the given branch id)
			/// </summary>
			/// <param name="bid">branch id</param>
			/// <returns>BranchState associated with the given branch id</returns>
			protected internal virtual SequenceMatcher.BranchState GetBranchState(int bid)
			{
				SequenceMatcher.BranchState bs = branchStates[bid];
				if (bs == null)
				{
					SequenceMatcher.BranchState pbs = null;
					int id = bid;
					while (pbs == null && id >= 0)
					{
						Pair<int, int> p = bidIndex.Get(id);
						id = p.first;
						pbs = branchStates[id];
					}
					bs = pbs;
				}
				return bs;
			}

			/// <summary>
			/// Returns the branch state for a given branch id
			/// (the appropriate ancestor branch state is returned if
			/// there is no branch state associated with the given branch id)
			/// If add is true, then adds a new branch state for this branch id
			/// (ensuring that the returned branch state is for the specified branch id)
			/// </summary>
			/// <param name="bid">branch id</param>
			/// <param name="add">whether a new branched state should be added</param>
			/// <returns>BranchState associated with the given branch id</returns>
			protected internal virtual SequenceMatcher.BranchState GetBranchState(int bid, bool add)
			{
				SequenceMatcher.BranchState bs = GetBranchState(bid);
				if (add)
				{
					if (bs == null)
					{
						bs = new SequenceMatcher.BranchState(bid);
					}
					else
					{
						if (bs.bid != bid)
						{
							bs = new SequenceMatcher.BranchState(bid, bs);
						}
					}
					branchStates[bid] = bs;
				}
				return bs;
			}

			protected internal virtual IDictionary<int, BasicSequenceMatchResult.MatchedGroup> GetMatchedGroups(int bid, bool add)
			{
				SequenceMatcher.BranchState bs = GetBranchState(bid, add);
				if (bs == null)
				{
					return null;
				}
				if (add && bs.matchedGroups == null)
				{
					bs.matchedGroups = new LinkedHashMap<int, BasicSequenceMatchResult.MatchedGroup>();
				}
				return bs.matchedGroups;
			}

			protected internal virtual BasicSequenceMatchResult.MatchedGroup GetMatchedGroup(int bid, int groupId)
			{
				IDictionary<int, BasicSequenceMatchResult.MatchedGroup> map = GetMatchedGroups(bid, false);
				if (map != null)
				{
					return map[groupId];
				}
				else
				{
					return null;
				}
			}

			protected internal virtual void SetGroupStart(int bid, int captureGroupId, int curPosition)
			{
				if (captureGroupId >= 0)
				{
					IDictionary<int, BasicSequenceMatchResult.MatchedGroup> matchedGroups = GetMatchedGroups(bid, true);
					BasicSequenceMatchResult.MatchedGroup mg = matchedGroups[captureGroupId];
					if (mg != null)
					{
						// This is possible if we have patterns like "( ... )+" in which case multiple nodes can match as the subgroup
						// We will match the first occurrence and use that as the subgroup  (Java uses the last match as the subgroup)
						logger.Fine("Setting matchBegin=" + curPosition + ": Capture group " + captureGroupId + " already exists: " + mg);
					}
					matchedGroups[captureGroupId] = new BasicSequenceMatchResult.MatchedGroup(curPosition, -1, null);
				}
			}

			protected internal virtual void SetGroupEnd(int bid, int captureGroupId, int curPosition, object value)
			{
				if (captureGroupId >= 0)
				{
					IDictionary<int, BasicSequenceMatchResult.MatchedGroup> matchedGroups = GetMatchedGroups(bid, true);
					BasicSequenceMatchResult.MatchedGroup mg = matchedGroups[captureGroupId];
					int end = curPosition + 1;
					if (mg != null)
					{
						if (mg.matchEnd == -1)
						{
							matchedGroups[captureGroupId] = new BasicSequenceMatchResult.MatchedGroup(mg.matchBegin, end, value);
						}
						else
						{
							if (mg.matchEnd != end)
							{
								logger.Warning("Cannot set matchEnd=" + end + ": Capture group " + captureGroupId + " already ended: " + mg);
							}
						}
					}
					else
					{
						logger.Warning("Cannot set matchEnd=" + end + ": Capture group " + captureGroupId + " is null");
					}
				}
			}

			protected internal virtual void ClearGroupStart(int bid, int captureGroupId)
			{
				if (captureGroupId >= 0)
				{
					IDictionary<int, BasicSequenceMatchResult.MatchedGroup> matchedGroups = GetMatchedGroups(bid, false);
					if (matchedGroups != null)
					{
						Sharpen.Collections.Remove(matchedGroups, captureGroupId);
					}
				}
			}

			protected internal virtual IDictionary<int, object> GetMatchedResults(int bid, bool add)
			{
				SequenceMatcher.BranchState bs = GetBranchState(bid, add);
				if (bs == null)
				{
					return null;
				}
				if (add && bs.matchedResults == null)
				{
					bs.matchedResults = new LinkedHashMap<int, object>();
				}
				return bs.matchedResults;
			}

			protected internal virtual object GetMatchedResult(int bid, int index)
			{
				IDictionary<int, object> map = GetMatchedResults(bid, false);
				if (map != null)
				{
					return map[index];
				}
				else
				{
					return null;
				}
			}

			protected internal virtual void SetMatchedResult(int bid, int index, object obj)
			{
				if (index >= 0)
				{
					IDictionary<int, object> matchedResults = GetMatchedResults(bid, true);
					object oldObj = matchedResults[index];
					if (oldObj != null)
					{
						logger.Warning("Setting matchedResult=" + obj + ": index " + index + " already exists: " + oldObj);
					}
					matchedResults[index] = obj;
				}
			}

			protected internal virtual int GetBranchId(int bid, int nextBranchIndex, int nextTotal)
			{
				if (nextBranchIndex <= 0 || nextBranchIndex > nextTotal)
				{
					throw new ArgumentException("Invalid nextBranchIndex=" + nextBranchIndex + ", nextTotal=" + nextTotal);
				}
				if (nextTotal == 1)
				{
					return bid;
				}
				else
				{
					Pair<int, int> p = new Pair<int, int>(bid, nextBranchIndex);
					int i = bidIndex.IndexOf(p);
					if (i < 0)
					{
						for (int j = 0; j < nextTotal; j++)
						{
							bidIndex.Add(new Pair<int, int>(bid, j + 1));
						}
						i = bidIndex.IndexOf(p);
					}
					return i;
				}
			}

			protected internal virtual IDictionary<SequencePattern.State, object> GetMatchStateInfo(int bid, bool add)
			{
				SequenceMatcher.BranchState bs = GetBranchState(bid, add);
				if (bs == null)
				{
					return null;
				}
				if (add && bs.matchStateInfo == null)
				{
					bs.matchStateInfo = new LinkedHashMap<SequencePattern.State, object>();
				}
				return bs.matchStateInfo;
			}

			protected internal virtual object GetMatchStateInfo(int bid, SequencePattern.State node)
			{
				IDictionary<SequencePattern.State, object> matchStateInfo = GetMatchStateInfo(bid, false);
				return (matchStateInfo != null) ? matchStateInfo[node] : null;
			}

			protected internal virtual void RemoveMatchStateInfo(int bid, SequencePattern.State node)
			{
				object obj = GetMatchStateInfo(bid, node);
				if (obj != null)
				{
					IDictionary<SequencePattern.State, object> matchStateInfo = GetMatchStateInfo(bid, true);
					Sharpen.Collections.Remove(matchStateInfo, node);
				}
			}

			protected internal virtual void SetMatchStateInfo(int bid, SequencePattern.State node, object obj)
			{
				IDictionary<SequencePattern.State, object> matchStateInfo = GetMatchStateInfo(bid, true);
				matchStateInfo[node] = obj;
			}

			protected internal virtual void StartMatchedCountInc(int bid, SequencePattern.State node)
			{
				StartMatchedCountInc(bid, node, 1, 1);
			}

			protected internal virtual void StartMatchedCountDec(int bid, SequencePattern.State node)
			{
				StartMatchedCountInc(bid, node, 0, -1);
			}

			protected internal virtual void StartMatchedCountInc(int bid, SequencePattern.State node, int initialValue, int delta)
			{
				IDictionary<SequencePattern.State, object> matchStateCount = GetMatchStateInfo(bid, true);
				Pair<int, bool> p = (Pair<int, bool>)matchStateCount[node];
				if (p == null)
				{
					matchStateCount[node] = new Pair<int, bool>(initialValue, false);
				}
				else
				{
					matchStateCount[node] = new Pair<int, bool>(p.First() + delta, false);
				}
			}

			protected internal virtual int EndMatchedCountInc(int bid, SequencePattern.State node)
			{
				IDictionary<SequencePattern.State, object> matchStateCount = GetMatchStateInfo(bid, false);
				if (matchStateCount == null)
				{
					return 0;
				}
				matchStateCount = GetMatchStateInfo(bid, true);
				Pair<int, bool> p = (Pair<int, bool>)matchStateCount[node];
				if (p != null)
				{
					int v = p.First();
					matchStateCount[node] = new Pair<int, bool>(v, true);
					return v;
				}
				else
				{
					return 0;
				}
			}

			protected internal virtual void ClearMatchedCount(int bid, SequencePattern.State node)
			{
				RemoveMatchStateInfo(bid, node);
			}

			protected internal virtual void SetMatchedInterval(int bid, SequencePattern.State node, IHasInterval<int> interval)
			{
				IDictionary<SequencePattern.State, object> matchStateInfo = GetMatchStateInfo(bid, true);
				IHasInterval<int> p = (IHasInterval<int>)matchStateInfo[node];
				if (p == null)
				{
					matchStateInfo[node] = interval;
				}
				else
				{
					logger.Warning("Interval already exists for bid=" + bid);
				}
			}

			protected internal virtual IHasInterval<int> GetMatchedInterval(int bid, SequencePattern.State node)
			{
				IDictionary<SequencePattern.State, object> matchStateInfo = GetMatchStateInfo(bid, true);
				IHasInterval<int> p = (IHasInterval<int>)matchStateInfo[node];
				return p;
			}

			protected internal virtual void AddBidsToCollapse(int bid, int[] bids)
			{
				SequenceMatcher.BranchState bs = GetBranchState(bid, true);
				bs.AddBidsToCollapse(bids);
			}

			private void MergeBranchStates(SequenceMatcher.BranchState bs)
			{
				if (bs.bidsToCollapse != null && bs.bidsToCollapse.Count > 0)
				{
					foreach (int cbid in bs.bidsToCollapse)
					{
						// Copy over the matched group info
						if (cbid != bs.bid)
						{
							SequenceMatcher.BranchState cbs = GetBranchState(cbid);
							if (cbs != null)
							{
								bs.AddMatchedGroups(cbs.matchedGroups);
								bs.AddMatchedResults(cbs.matchedResults);
							}
							else
							{
								logger.Finest("Unable to find state info for bid=" + cbid);
							}
						}
					}
					if (bs.collapsedBids == null)
					{
						bs.collapsedBids = bs.bidsToCollapse;
					}
					else
					{
						Sharpen.Collections.AddAll(bs.collapsedBids, bs.bidsToCollapse);
					}
					bs.bidsToCollapse = null;
				}
			}
		}

		private string GetMatchedSignature()
		{
			if (matchedGroups == null)
			{
				return null;
			}
			StringBuilder sb = new StringBuilder();
			foreach (BasicSequenceMatchResult.MatchedGroup g in matchedGroups)
			{
				sb.Append("(").Append(g.matchBegin).Append(",").Append(g.matchEnd).Append(")");
			}
			return sb.ToString();
		}

		/// <summary>
		/// Utility class that helps us perform pattern matching against a sequence
		/// Keeps information about:
		/// <ul>
		/// <li>the states we need to visit</li>
		/// <li>the current position in the sequence we are at</li>
		/// <li>state for each branch we took</li>
		/// </ul>
		/// </summary>
		/// <?/>
		internal class MatchedStates<T>
		{
			internal readonly SequenceMatcher<T> matcher;

			internal SequenceMatcher.BranchStates branchStates;

			internal IList<SequenceMatcher.State> oldStates;

			internal IList<SequenceMatcher.State> states;

			internal int curPosition = -1;

			internal bool matchLongest;

			protected internal MatchedStates(SequenceMatcher<T> matcher, SequencePattern.State state)
				: this(matcher, new SequenceMatcher.BranchStates())
			{
				// Sequence matcher with pattern that we are matching against and sequence
				// Branch states
				// set of old states along with their branch ids (used to avoid reallocating mem)
				// new states to be explored (along with their branch ids)
				// Current position to match
				// Favor matching longest
				int bid = branchStates.NewBid(-1, 0);
				states.Add(new SequenceMatcher.State(bid, state));
			}

			private MatchedStates(SequenceMatcher<T> matcher, SequenceMatcher.BranchStates branchStates)
			{
				this.matcher = matcher;
				states = new List<SequenceMatcher.State>();
				oldStates = new List<SequenceMatcher.State>();
				this.branchStates = branchStates;
				branchStates.Link(this);
			}

			protected internal virtual SequenceMatcher.BranchStates GetBranchStates()
			{
				return branchStates;
			}

			/// <summary>Split part of the set of states to explore into another MatchedStates</summary>
			/// <param name="branchLimit">
			/// - rough limit on the number of branches we want
			/// to keep in each MatchedStates
			/// </param>
			/// <returns>new MatchedStates with part of the states still to be explored</returns>
			protected internal virtual SequenceMatcher.MatchedStates Split(int branchLimit)
			{
				ICollection<int> curBidSet = new HashSet<int>();
				//Generics.newHashSet();
				foreach (SequenceMatcher.State state in states)
				{
					curBidSet.Add(state.bid);
				}
				IList<int> bids = new List<int>(curBidSet);
				bids.Sort(null);
				SequenceMatcher.MatchedStates<T> newStates = new SequenceMatcher.MatchedStates<T>(matcher, branchStates);
				int v = Math.Min(branchLimit, (bids.Count + 1) / 2);
				ICollection<int> keepBidSet = new HashSet<int>();
				//Generics.newHashSet();
				Sharpen.Collections.AddAll(keepBidSet, bids.SubList(0, v));
				SwapAndClear();
				foreach (SequenceMatcher.State s in oldStates)
				{
					if (keepBidSet.Contains(s.bid))
					{
						states.Add(s);
					}
					else
					{
						newStates.states.Add(s);
					}
				}
				newStates.curPosition = curPosition;
				branchStates.Condense();
				return newStates;
			}

			protected internal virtual IList<T> Elements()
			{
				return matcher.elements;
			}

			protected internal virtual T Get()
			{
				return matcher.Get(curPosition);
			}

			protected internal virtual int Size()
			{
				return states.Count;
			}

			protected internal virtual int BranchSize()
			{
				return branchStates.Size();
			}

			private void Swap()
			{
				IList<SequenceMatcher.State> tmpStates = oldStates;
				oldStates = states;
				states = tmpStates;
			}

			private void SwapAndClear()
			{
				Swap();
				states.Clear();
			}

			// Attempts to match element at the specified position
			private bool Match(int position)
			{
				curPosition = position;
				bool matched = false;
				SwapAndClear();
				// Start with old state, and try to match next element
				// New states to search after successful match will be updated during the match process
				foreach (SequenceMatcher.State state in oldStates)
				{
					if (state.tstate.Match(state.bid, this))
					{
						matched = true;
					}
				}
				// Run NFA to process non consuming states
				bool done = false;
				while (!done)
				{
					SwapAndClear();
					bool matched0 = false;
					foreach (SequenceMatcher.State state_1 in oldStates)
					{
						if (state_1.tstate.Match0(state_1.bid, this))
						{
							matched0 = true;
						}
					}
					done = !matched0;
				}
				branchStates.Condense();
				return matched;
			}

			private readonly int[] p1Buffer = new int[128];

			private readonly int[] p2Buffer = new int[128];

			protected internal virtual int CompareMatches(int bid1, int bid2)
			{
				if (bid1 == bid2)
				{
					return 0;
				}
				IList<int> p1 = branchStates.GetParents(bid1, p1Buffer);
				//      p1.add(bid1);
				IList<int> p2 = branchStates.GetParents(bid2, p2Buffer);
				//      p2.add(bid2);
				int n = Math.Min(p1.Count, p2.Count);
				for (int i = 0; i < n; i++)
				{
					if (p1[i] < p2[i])
					{
						return -1;
					}
					if (p1[i] > p2[i])
					{
						return 1;
					}
				}
				if (p1.Count < p2.Count)
				{
					return -1;
				}
				if (p1.Count > p2.Count)
				{
					return 1;
				}
				return 0;
			}

			/// <summary>Returns index of state that results in match (-1 if no matches)</summary>
			private int GetMatchIndex()
			{
				for (int i = 0; i < states.Count; i++)
				{
					SequenceMatcher.State state = states[i];
					if (state.tstate.Equals(SequencePattern.MatchState))
					{
						return i;
					}
				}
				return -1;
			}

			/// <summary>Returns a set of indices that results in a match</summary>
			private ICollection<int> GetMatchIndices()
			{
				HashSet<int> allMatchIndices = new LinkedHashSet<int>();
				// Generics.newHashSet();
				for (int i = 0; i < states.Count; i++)
				{
					SequenceMatcher.State state = states[i];
					if (state.tstate.Equals(SequencePattern.MatchState))
					{
						allMatchIndices.Add(i);
					}
				}
				return allMatchIndices;
			}

			/// <summary>
			/// Of the potential match indices, selects one and returns it
			/// (returns -1 if no matches)
			/// </summary>
			private int SelectMatchIndex()
			{
				int best = -1;
				int bestbid = -1;
				BasicSequenceMatchResult.MatchedGroup bestMatched = null;
				int bestMatchedLength = -1;
				for (int i = 0; i < states.Count; i++)
				{
					SequenceMatcher.State state = states[i];
					if (state.tstate.Equals(SequencePattern.MatchState))
					{
						if (best < 0)
						{
							best = i;
							bestbid = state.bid;
							bestMatched = branchStates.GetMatchedGroup(bestbid, 0);
							bestMatchedLength = (bestMatched != null) ? bestMatched.MatchLength() : -1;
						}
						else
						{
							// Compare if this match is better?
							int bid = state.bid;
							BasicSequenceMatchResult.MatchedGroup mg = branchStates.GetMatchedGroup(bid, 0);
							int matchLength = (mg != null) ? mg.MatchLength() : -1;
							// Select the branch that matched the most
							// TODO: Do we need to roll the matchedLength to bestMatchedLength check into the compareMatches?
							bool better;
							if (matchLongest)
							{
								better = (matchLength > bestMatchedLength || (matchLength == bestMatchedLength && CompareMatches(bestbid, bid) > 0));
							}
							else
							{
								better = CompareMatches(bestbid, bid) > 0;
							}
							if (better)
							{
								bestbid = bid;
								best = i;
								bestMatched = branchStates.GetMatchedGroup(bestbid, 0);
								bestMatchedLength = (bestMatched != null) ? bestMatched.MatchLength() : -1;
							}
						}
					}
				}
				return best;
			}

			private void CompleteMatch()
			{
				int matchStateIndex = SelectMatchIndex();
				SetMatchedGroups(matchStateIndex);
			}

			/// <summary>Set the indices of the matched groups</summary>
			/// <param name="matchStateIndex"/>
			private void SetMatchedGroups(int matchStateIndex)
			{
				matcher.ClearMatched();
				if (matchStateIndex >= 0)
				{
					SequenceMatcher.State state = states[matchStateIndex];
					int bid = state.bid;
					SequenceMatcher.BranchState bs = branchStates.GetBranchState(bid);
					if (bs != null)
					{
						branchStates.MergeBranchStates(bs);
						IDictionary<int, BasicSequenceMatchResult.MatchedGroup> matchedGroups = bs.matchedGroups;
						if (matchedGroups != null)
						{
							foreach (int group in matchedGroups.Keys)
							{
								matcher.matchedGroups[group] = matchedGroups[group];
							}
						}
						IDictionary<int, object> matchedResults = bs.matchedResults;
						if (matchedResults != null)
						{
							if (matcher.matchedResults == null)
							{
								matcher.matchedResults = new object[matcher.Elements().Count];
							}
							foreach (int index in matchedResults.Keys)
							{
								matcher.matchedResults[index] = matchedResults[index];
							}
						}
					}
				}
			}

			private bool IsAllMatch()
			{
				bool allMatch = true;
				if (states.Count > 0)
				{
					foreach (SequenceMatcher.State state in states)
					{
						if (!state.tstate.Equals(SequencePattern.MatchState))
						{
							allMatch = false;
							break;
						}
					}
				}
				else
				{
					allMatch = false;
				}
				return allMatch;
			}

			private bool IsMatch()
			{
				int matchStateIndex = GetMatchIndex();
				return (matchStateIndex >= 0);
			}

			protected internal virtual void AddStates(int bid, ICollection<SequencePattern.State> newStates)
			{
				int i = 0;
				foreach (SequencePattern.State s in newStates)
				{
					i++;
					int id = branchStates.GetBranchId(bid, i, newStates.Count);
					states.Add(new SequenceMatcher.State(id, s));
				}
			}

			protected internal virtual void AddState(int bid, SequencePattern.State state)
			{
				this.states.Add(new SequenceMatcher.State(bid, state));
			}

			private void Clean()
			{
				branchStates.Unlink(this);
				branchStates = null;
			}

			protected internal virtual void SetGroupStart(int bid, int captureGroupId)
			{
				branchStates.SetGroupStart(bid, captureGroupId, curPosition);
			}

			protected internal virtual void SetGroupEnd(int bid, int captureGroupId, object value)
			{
				branchStates.SetGroupEnd(bid, captureGroupId, curPosition, value);
			}

			protected internal virtual void SetGroupEnd(int bid, int captureGroupId, int position, object value)
			{
				branchStates.SetGroupEnd(bid, captureGroupId, position, value);
			}

			protected internal virtual void ClearGroupStart(int bid, int captureGroupId)
			{
				branchStates.ClearGroupStart(bid, captureGroupId);
			}
		}
		// end static class MatchedStates
	}
}
