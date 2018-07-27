using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>The result of a match against a sequence.</summary>
	/// <remarks>
	/// The result of a match against a sequence.
	/// Similar to Java's
	/// <see cref="Java.Util.Regex.IMatchResult"/>
	/// except it is for sequences
	/// over arbitrary types T instead of just characters.
	/// This interface contains query methods used to determine the
	/// results of a match against a regular expression against an sequence.
	/// The match boundaries, groups and group boundaries can be seen
	/// but not modified through a
	/// <see cref="ISequenceMatchResult{T}"/>
	/// .
	/// </remarks>
	/// <author>Angel Chang</author>
	/// <seealso cref="SequenceMatcher{T}"/>
	public interface ISequenceMatchResult<T> : IMatchResult, IHasInterval<int>
	{
		// TODO: Need to be careful with GROUP_BEFORE_MATCH/GROUP_AFTER_MATCH
		// Special match groups (before match)
		// Special match groups (after match)
		double Score();

		double Priority();

		/// <summary>Returns the original sequence the match was performed on.</summary>
		/// <returns>The list that the match was performed on</returns>
		IList<T> Elements();

		/// <summary>Returns pattern used to create this sequence match result</summary>
		/// <returns>the SequencePattern against which this sequence match result was matched</returns>
		SequencePattern<T> Pattern();

		/// <summary>Returns the entire matched subsequence as a list.</summary>
		/// <returns>the matched subsequence as a list</returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		IList<T> GroupNodes();

		/// <summary>Returns the matched group as a list.</summary>
		/// <param name="group">The index of a capturing group in this matcher's pattern</param>
		/// <returns>the matched group as a list</returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		/// <exception cref="System.IndexOutOfRangeException">
		/// If there is no capturing group in the pattern
		/// with the given index
		/// </exception>
		IList<T> GroupNodes(int group);

		BasicSequenceMatchResult<T> ToBasicSequenceMatchResult();

		// String lookup versions using variables
		/// <summary>Returns the matched group as a list.</summary>
		/// <param name="groupVar">The name of the capturing group in this matcher's pattern</param>
		/// <returns>
		/// the matched group as a list
		/// or
		/// <see langword="null"/>
		/// if there is no capturing group in the pattern
		/// with the given name
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		IList<T> GroupNodes(string groupVar);

		/// <summary>
		/// Returns the
		/// <c>String</c>
		/// representing the matched group.
		/// </summary>
		/// <param name="groupVar">The name of the capturing group in this matcher's pattern</param>
		/// <returns>
		/// the matched group as a
		/// <c>String</c>
		/// or
		/// <see langword="null"/>
		/// if there is no capturing group in the pattern
		/// with the given name
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		string Group(string groupVar);

		/// <summary>
		/// Returns the start index of the subsequence captured by the given group
		/// during this match.
		/// </summary>
		/// <param name="groupVar">The name of the capturing group in this matcher's pattern</param>
		/// <returns>
		/// the index of the first element captured by the group,
		/// or
		/// <c>-1</c>
		/// if the match was successful but the group
		/// itself did not match anything
		/// or if there is no capturing group in the pattern
		/// with the given name
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		int Start(string groupVar);

		/// <summary>
		/// Returns the index of the next element after the subsequence captured by the given group
		/// during this match.
		/// </summary>
		/// <param name="groupVar">The name of the capturing group in this matcher's pattern</param>
		/// <returns>
		/// the index of the next element after the subsequence captured by the group,
		/// or
		/// <c>-1</c>
		/// if the match was successful but the group
		/// itself did not match anything
		/// or if there is no capturing group in the pattern
		/// with the given name
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		int End(string groupVar);

		int GetOrder();

		/// <summary>Returns an Object representing the result for the match for a particular node.</summary>
		/// <remarks>
		/// Returns an Object representing the result for the match for a particular node.
		/// (actual Object returned depends on the type T of the nodes).  For instance,
		/// for a CoreMap, the match result is returned as a
		/// <c>Map&lt;Class, Object&gt;</c>
		/// , while
		/// for String, the match result is typically a MatchResult.
		/// </remarks>
		/// <param name="index">The index of the element in the original sequence.</param>
		/// <returns>The match result associated with the node at the given index.</returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		/// <exception cref="System.IndexOutOfRangeException">If the index is out of range</exception>
		object NodeMatchResult(int index);

		/// <summary>Returns an Object representing the result for the match for a particular node in a group.</summary>
		/// <remarks>
		/// Returns an Object representing the result for the match for a particular node in a group.
		/// (actual Object returned depends on the type T of the nodes.  For instance,
		/// for a CoreMap, the match result is returned as a
		/// <c>Map&lt;Class, Object&gt;</c>
		/// , while
		/// for String, the match result is typically a MatchResult.
		/// </remarks>
		/// <param name="groupid">The index of a capturing group in this matcher's pattern</param>
		/// <param name="index">The index of the element in the captured subsequence.</param>
		/// <returns>
		/// the match result associated with the node
		/// at the given index for the captured group.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		/// <exception cref="System.IndexOutOfRangeException">
		/// If there is no capturing group in the pattern
		/// with the given groupid or if the index is out of range
		/// </exception>
		object GroupMatchResult(int groupid, int index);

		/// <summary>Returns an Object representing the result for the match for a particular node in a group.</summary>
		/// <remarks>
		/// Returns an Object representing the result for the match for a particular node in a group.
		/// (actual Object returned depends on the type T of the nodes.  For instance,
		/// for a CoreMap, the match result is returned as a
		/// <c>Map&lt;Class, Object&gt;</c>
		/// , while
		/// for String, the match result is typically a MatchResult.
		/// </remarks>
		/// <param name="groupVar">The name of the capturing group in this matcher's pattern</param>
		/// <param name="index">The index of the element in the captured subsequence.</param>
		/// <returns>
		/// the match result associated with the node
		/// at the given index for the captured group.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		/// <exception cref="System.IndexOutOfRangeException">if the index is out of range</exception>
		object GroupMatchResult(string groupVar, int index);

		/// <summary>Returns a list of Objects representing the match results for the entire sequence.</summary>
		/// <returns>the list of match results associated with the entire sequence</returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		IList<object> GroupMatchResults();

		/// <summary>Returns a list of Objects representing the match results for the nodes in the group.</summary>
		/// <param name="group">The index of a capturing group in this matcher's pattern</param>
		/// <returns>
		/// the list of match results associated with the nodes
		/// for the captured group.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		/// <exception cref="System.IndexOutOfRangeException">
		/// If there is no capturing group in the pattern
		/// with the given index
		/// </exception>
		IList<object> GroupMatchResults(int group);

		/// <summary>Returns a list of Objects representing the match results for the nodes in the group.</summary>
		/// <param name="groupVar">The name of the capturing group in this matcher's pattern</param>
		/// <returns>
		/// the list of match results associated with the nodes
		/// for the captured group.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		IList<object> GroupMatchResults(string groupVar);

		/// <summary>Returns the value (some Object) associated with the entire matched sequence.</summary>
		/// <returns>value associated with the matched sequence.</returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		object GroupValue();

		/// <summary>Returns the value (some Object) associated with the captured group.</summary>
		/// <param name="group">The index of a capturing group in this matcher's pattern</param>
		/// <returns>value associated with the captured group.</returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		object GroupValue(int group);

		/// <summary>Returns the value (some Object) associated with the captured group.</summary>
		/// <param name="var">The name of the capturing group in this matcher's pattern</param>
		/// <returns>value associated with the captured group.</returns>
		/// <exception cref="System.InvalidOperationException">
		/// If no match has yet been attempted,
		/// or if the previous match operation failed
		/// </exception>
		object GroupValue(string var);

		SequenceMatchResult.MatchedGroupInfo<T> GroupInfo();

		SequenceMatchResult.MatchedGroupInfo<T> GroupInfo(int group);

		SequenceMatchResult.MatchedGroupInfo<T> GroupInfo(string var);

		public class GroupToIntervalFunc<Mr> : IFunction<MR, Interval<int>>
			where Mr : IMatchResult
		{
			internal int group;

			public GroupToIntervalFunc(int group)
			{
				this.group = group;
			}

			public virtual Interval<int> Apply(MR @in)
			{
				return Interval.ToInterval(@in.Start(group), @in.End(group), Interval.IntervalOpenEnd);
			}
		}

		/// <summary>Information about a matched group.</summary>
		/// <?/>
		public sealed class MatchedGroupInfo<T>
		{
			public readonly string text;

			public readonly IList<T> nodes;

			public readonly IList<object> matchResults;

			public readonly object value;

			public readonly string varName;

			public MatchedGroupInfo(string text, IList<T> nodes, IList<object> matchResults, object value, string varName)
			{
				this.text = text;
				this.nodes = nodes;
				this.matchResults = matchResults;
				this.value = value;
				this.varName = varName;
			}
		}
		// end class MatchedGroupInfo
	}

	public static class SequenceMatchResultConstants
	{
		public const int GroupBeforeMatch = int.MinValue;

		public const int GroupAfterMatch = int.MinValue + 1;

		public const SequenceMatchResult.GroupToIntervalFunc ToInterval = new SequenceMatchResult.GroupToIntervalFunc(0);

		public const IComparator<IMatchResult> PriorityComparator = null;

		public const IComparator<IMatchResult> ScoreComparator = null;

		public const IComparator<IMatchResult> OrderComparator = null;

		/// <summary>Compares two match results.</summary>
		/// <remarks>
		/// Compares two match results.
		/// Use to order match results by: length (longest first)
		/// </remarks>
		public const IComparator<IMatchResult> LengthComparator = null;

		public const IComparator<IMatchResult> OffsetComparator = null;

		/// <summary>Compares two match results.</summary>
		/// <remarks>
		/// Compares two match results. Use to order match results by:
		/// priority (highest first), score (highest first), length (longest first),
		/// and then beginning token offset (smaller offset first), original order (smaller first)
		/// </remarks>
		public const IComparator<IMatchResult> PriorityScoreLengthOrderOffsetComparator = Comparators.Chain(SequenceMatchResult<T>Constants.PriorityComparator, SequenceMatchResult<T>Constants.ScoreComparator, SequenceMatchResult<T>Constants.LengthComparator
			, SequenceMatchResult<T>Constants.OrderComparator, SequenceMatchResult<T>Constants.OffsetComparator);

		public const IComparator<IMatchResult> DefaultComparator = SequenceMatchResult<T>Constants.PriorityScoreLengthOrderOffsetComparator;

		public const IToDoubleFunction<IMatchResult> Scorer = null;
	}
}
