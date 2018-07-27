using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>A class which encodes the current state of the parsing.</summary>
	/// <remarks>
	/// A class which encodes the current state of the parsing.  This can
	/// be used either for direct search or for beam search.
	/// <br />
	/// Important information which needs to be encoded:
	/// <ul>
	/// <li>A stack.  This needs to be updatable in O(1) time to keep the
	/// parser's run time linear.  This is done by using a linked-list type
	/// stack in which new states are created by the <code>push</code>
	/// operation.
	/// <li>A queue.  This also needs to be updatable in O(1) time.  This
	/// is accomplished by having all the states share the same list of
	/// queued items, with different states only changing an index into the
	/// queue.
	/// <li>The score of the current state.  This is useful in beam searches.
	/// <li>Whether or not the current state is "finalized".  If so, the
	/// only thing that can be done from now on is to idle.
	/// </ul>
	/// </remarks>
	public class State : IScored
	{
		/// <summary>Expects a list of preterminals.</summary>
		/// <remarks>
		/// Expects a list of preterminals.  The preterminals should be built
		/// with CoreLabels and have HeadWord and HeadTag annotations set.
		/// </remarks>
		public State(IList<Tree> sentence)
			: this(new TreeShapedStack<Tree>(), new TreeShapedStack<ITransition>(), FindSeparators(sentence), sentence, 0, 0.0, false)
		{
		}

		internal State(TreeShapedStack<Tree> stack, TreeShapedStack<ITransition> transitions, SortedDictionary<int, string> separators, IList<Tree> sentence, int tokenPosition, double score, bool finished)
		{
			this.stack = stack;
			this.transitions = transitions;
			this.separators = separators;
			this.sentence = sentence;
			this.tokenPosition = tokenPosition;
			this.score = score;
			this.finished = finished;
		}

		/// <summary>The stack of Tree pieces we have already assembled.</summary>
		internal readonly TreeShapedStack<Tree> stack;

		/// <summary>The transition sequence used to get to the current position</summary>
		internal readonly TreeShapedStack<ITransition> transitions;

		/// <summary>Used to describe the relative location of separators to the head of a subtree</summary>
		public enum HeadPosition
		{
			None,
			Left,
			Right,
			Both,
			Head
		}

		/// <summary>
		/// A description of where the separators such as ,;:- are in a
		/// subtree, relative to the head of the subtree
		/// </summary>
		internal readonly SortedDictionary<int, string> separators;

		internal virtual Tree GetStackNode(int depth)
		{
			if (depth >= stack.Size())
			{
				return null;
			}
			TreeShapedStack<Tree> node = stack;
			for (int i = 0; i < depth; ++i)
			{
				node = node.Pop();
			}
			return node.Peek();
		}

		internal virtual Tree GetQueueNode(int depth)
		{
			if (tokenPosition + depth >= sentence.Count)
			{
				return null;
			}
			return sentence[tokenPosition + depth];
		}

		/// <summary>
		/// Returns the first separator between two nodes or returns null if
		/// such a thing does not exist
		/// </summary>
		internal virtual string GetSeparatorBetween(int right, int left)
		{
			if (right >= left)
			{
				throw new AssertionError("Expected right < left");
			}
			return GetSeparatorBetween(GetStackNode(right), GetStackNode(left));
		}

		internal virtual string GetSeparatorBetween(Tree right, Tree left)
		{
			if (right == null || left == null)
			{
				return null;
			}
			int leftHead = ShiftReduceUtils.HeadIndex(left);
			int rightHead = ShiftReduceUtils.HeadIndex(right);
			KeyValuePair<int, string> nextSeparator = separators.CeilingEntry(leftHead);
			if (nextSeparator == null || nextSeparator.Key > rightHead)
			{
				return null;
			}
			return Sharpen.Runtime.Substring(nextSeparator.Value, 0, 1);
		}

		/// <summary>
		/// Returns the separator count between two nodes
		/// (0 if any of the nodes don't exist)
		/// </summary>
		internal virtual int GetSeparatorCount(int right, int left)
		{
			if (right >= left)
			{
				throw new AssertionError("Expected right < left");
			}
			return GetSeparatorCount(GetStackNode(right), GetStackNode(left));
		}

		internal virtual int GetSeparatorCount(Tree right, Tree left)
		{
			if (right == null || left == null)
			{
				return 0;
			}
			int leftHead = ShiftReduceUtils.HeadIndex(left);
			int rightHead = ShiftReduceUtils.HeadIndex(right);
			int nextSeparator = separators.HigherKey(leftHead);
			int count = 0;
			while (nextSeparator != null && nextSeparator < rightHead)
			{
				++count;
				nextSeparator = separators.HigherKey(nextSeparator);
			}
			return count;
		}

		internal virtual State.HeadPosition GetSeparator(int nodeNum)
		{
			if (nodeNum >= stack.Size())
			{
				return null;
			}
			TreeShapedStack<Tree> stack = this.stack;
			for (int i = 0; i < nodeNum; ++i)
			{
				stack = stack.Pop();
			}
			Tree node = stack.Peek();
			int head = ShiftReduceUtils.HeadIndex(node);
			if (separators[head] != null)
			{
				return State.HeadPosition.Head;
			}
			int left = ShiftReduceUtils.LeftIndex(node);
			int nextLeft = separators.FloorKey(head);
			bool hasLeft = (nextLeft != null && nextLeft >= left);
			int right = ShiftReduceUtils.RightIndex(node);
			int nextRight = separators.CeilingKey(head);
			bool hasRight = (nextRight != null && nextRight <= right);
			if (hasLeft && hasRight)
			{
				return State.HeadPosition.Both;
			}
			else
			{
				if (hasLeft)
				{
					return State.HeadPosition.Left;
				}
				else
				{
					if (hasRight)
					{
						return State.HeadPosition.Right;
					}
					else
					{
						return State.HeadPosition.None;
					}
				}
			}
		}

		internal static readonly Pattern separatorRegex = Pattern.Compile("^[,;:-]+$");

		internal static readonly char[][] equivalentSeparators = new char[][] { new char[] { '，', ',' }, new char[] { '；', ';' }, new char[] { '：', ':' } };

		internal static SortedDictionary<int, string> FindSeparators(IList<Tree> sentence)
		{
			SortedDictionary<int, string> separators = Generics.NewTreeMap();
			for (int index = 0; index < sentence.Count; ++index)
			{
				Tree leaf = sentence[index].Children()[0];
				string value = leaf.Value();
				foreach (char[] equivalentSeparator in equivalentSeparators)
				{
					value = value.Replace(equivalentSeparator[0], equivalentSeparator[1]);
				}
				if (separatorRegex.Matcher(value).Matches())
				{
					// TODO: put "value" instead?  Perhaps do this next time we rebuild all models
					separators[index] = leaf.Value();
				}
			}
			return separators;
		}

		/// <summary>The words we are parsing.</summary>
		/// <remarks>
		/// The words we are parsing.  They need to be tagged before we can
		/// parse.  The words are stored as preterminal Trees whose only
		/// nodes are the tag node and the word node.
		/// </remarks>
		internal readonly IList<Tree> sentence;

		/// <summary>Essentially, the position in the queue part of the state.</summary>
		/// <remarks>
		/// Essentially, the position in the queue part of the state.
		/// 0 represents that we are at the start of the queue and nothing
		/// has been shifted yet.
		/// </remarks>
		internal readonly int tokenPosition;

		/// <summary>
		/// The score of the current state based on the transitions that were
		/// used to create it.
		/// </summary>
		internal readonly double score;

		public virtual double Score()
		{
			return score;
		}

		/// <summary>Whether or not processing has finished.</summary>
		/// <remarks>
		/// Whether or not processing has finished.  Once that is true, only
		/// idle transitions are allowed.
		/// </remarks>
		internal readonly bool finished;

		public virtual bool IsFinished()
		{
			return finished;
		}

		public virtual bool EndOfQueue()
		{
			return tokenPosition == sentence.Count;
		}

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			result.Append("State summary\n");
			result.Append("  Tokens: " + sentence + "\n");
			result.Append("  Token position: " + tokenPosition + "\n");
			result.Append("  Current stack contents: " + stack.ToString("\n") + "\n");
			result.Append("  Component transitions: " + transitions + "\n");
			result.Append("  Score: " + score + "\n");
			result.Append("  " + ((finished) ? string.Empty : "not ") + "finished\n");
			return result.ToString();
		}

		/// <summary>
		/// Whether or not the transitions that built the two states are
		/// equal.
		/// </summary>
		/// <remarks>
		/// Whether or not the transitions that built the two states are
		/// equal.  Doesn't check anything else.  Useful for training using
		/// an agenda, for example, when you know the underlying information
		/// such as the words are the same and all you care about checking is
		/// the transition sequence
		/// </remarks>
		public virtual bool AreTransitionsEqual(Edu.Stanford.Nlp.Parser.Shiftreduce.State other)
		{
			return transitions.Equals(other.transitions);
		}
	}
}
