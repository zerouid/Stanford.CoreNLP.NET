using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>
	/// Transition that moves a single item from the front of the queue to
	/// the top of the stack without making any other changes.
	/// </summary>
	[System.Serializable]
	public class ShiftTransition : ITransition
	{
		/// <summary>
		/// Shifting is legal as long as the state is not finished and there
		/// are more items on the queue to be shifted.
		/// </summary>
		/// <remarks>
		/// Shifting is legal as long as the state is not finished and there
		/// are more items on the queue to be shifted.
		/// TODO: go through the papers and make sure they don't mention any
		/// other conditions where one shouldn't shift
		/// </remarks>
		public virtual bool IsLegal(State state, IList<ParserConstraint> constraints)
		{
			if (state.finished)
			{
				return false;
			}
			if (state.tokenPosition >= state.sentence.Count)
			{
				return false;
			}
			// We disallow shifting when the previous transition was a right
			// head transition to a partial (binarized) state
			// TODO: I don't have an explanation for this, it was just stated
			// in Zhang & Clark 2009
			if (state.stack.Size() > 0)
			{
				Tree top = state.stack.Peek();
				// Temporary node, eg part of a binarized sequence
				if (top.Label().Value().StartsWith("@") && top.Children().Length == 2 && ShiftReduceUtils.GetBinarySide(top) == BinaryTransition.Side.Right)
				{
					return false;
				}
			}
			if (constraints == null || state.stack.Size() == 0)
			{
				return true;
			}
			Tree top_1 = state.stack.Peek();
			// If there are ParserConstraints, you can only shift if shifting
			// will not make a constraint unsolvable.  This happens if we
			// shift beyond the right end of a constraint which is not solved.
			foreach (ParserConstraint constraint in constraints)
			{
				// either went past or haven't gotten to this constraint yet
				if (ShiftReduceUtils.RightIndex(top_1) != constraint.end - 1)
				{
					continue;
				}
				int left = ShiftReduceUtils.LeftIndex(top_1);
				if (left < constraint.start)
				{
					continue;
				}
				if (left > constraint.start)
				{
					return false;
				}
				if (!ShiftReduceUtils.ConstraintMatchesTreeTop(top_1, constraint))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Add the new preterminal to the stack, increment the queue position.</summary>
		public virtual State Apply(State state)
		{
			return Apply(state, 0.0);
		}

		/// <summary>Add the new preterminal to the stack, increment the queue position.</summary>
		public virtual State Apply(State state, double scoreDelta)
		{
			Tree tagNode = state.sentence[state.tokenPosition];
			if (!tagNode.IsPreTerminal())
			{
				throw new AssertionError("Only expected preterminal nodes");
			}
			Tree wordNode = tagNode.Children()[0];
			string word = wordNode.Label().Value();
			return new State(state.stack.Push(tagNode), state.transitions.Push(this), state.separators, state.sentence, state.tokenPosition + 1, state.score + scoreDelta, false);
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (o is ShiftTransition)
			{
				return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return 900967388;
		}

		// a random int
		public override string ToString()
		{
			return "Shift";
		}

		private const long serialVersionUID = 1;
	}
}
