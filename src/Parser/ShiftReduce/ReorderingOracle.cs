using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>A second attempt at making an oracle.</summary>
	/// <remarks>
	/// A second attempt at making an oracle.  Instead of always trying to
	/// return the best transition, it simply rearranges the transition
	/// lists after an incorrect transition.  If this is not possible,
	/// training will be halted as in the case of early update.
	/// </remarks>
	/// <author>John Bauer</author>
	public class ReorderingOracle
	{
		internal ShiftReduceOptions op;

		public ReorderingOracle(ShiftReduceOptions op)
		{
			this.op = op;
		}

		/// <summary>
		/// Given a predicted transition and a state, this method rearranges
		/// the list of transitions and returns whether or not training can
		/// continue.
		/// </summary>
		internal virtual bool Reorder(State state, ITransition chosenTransition, IList<ITransition> transitions)
		{
			if (transitions.Count == 0)
			{
				throw new AssertionError();
			}
			ITransition goldTransition = transitions[0];
			// If the transition is gold, we are already satisfied.
			if (chosenTransition.Equals(goldTransition))
			{
				transitions.Remove(0);
				return true;
			}
			// If the transition should have been a Unary/CompoundUnary
			// transition and it was something else or a different Unary
			// transition, see if the transition sequence can be continued
			// after skipping past the unary
			if ((goldTransition is UnaryTransition) || (goldTransition is CompoundUnaryTransition))
			{
				transitions.Remove(0);
				return Reorder(state, chosenTransition, transitions);
			}
			// If the chosen transition was an incorrect Unary/CompoundUnary
			// transition, skip past it and hope to continue the gold
			// transition sequence.  However, if we have Unary/CompoundUnary
			// in a row, we have to return false to prevent loops.
			// Also, if the state stack size is 0, can't keep going
			if ((chosenTransition is UnaryTransition) || (chosenTransition is CompoundUnaryTransition))
			{
				if (state.transitions.Size() > 0)
				{
					ITransition previous = state.transitions.Peek();
					if ((previous is UnaryTransition) || (previous is CompoundUnaryTransition))
					{
						return false;
					}
				}
				if (state.stack.Size() == 0)
				{
					return false;
				}
				return true;
			}
			if (chosenTransition is BinaryTransition)
			{
				if (state.stack.Size() < 2)
				{
					return false;
				}
				if (goldTransition is ShiftTransition)
				{
					// Helps, but adds quite a bit of size to the model and only helps a tiny bit
					return op.TrainOptions().oracleBinaryToShift && ReorderIncorrectBinaryTransition(transitions);
				}
				if (!(goldTransition is BinaryTransition))
				{
					return false;
				}
				BinaryTransition chosenBinary = (BinaryTransition)chosenTransition;
				BinaryTransition goldBinary = (BinaryTransition)goldTransition;
				if (chosenBinary.IsBinarized())
				{
					// Binarized labels only work (for now, at least) if the side
					// is wrong but the label itself is correct
					if (goldBinary.IsBinarized() && chosenBinary.label.Equals(goldBinary.label))
					{
						transitions.Remove(0);
						return true;
					}
					else
					{
						return false;
					}
				}
				// In all other binarized situations, essentially what has
				// happened is we added a bracket error, but future brackets can
				// still wind up being correct
				transitions.Remove(0);
				return true;
			}
			if ((chosenTransition is ShiftTransition) && (goldTransition is BinaryTransition))
			{
				// can't shift at the end of the queue
				if (state.EndOfQueue())
				{
					return false;
				}
				// doesn't help, sadly
				BinaryTransition goldBinary = (BinaryTransition)goldTransition;
				if (!goldBinary.IsBinarized())
				{
					return op.TrainOptions().oracleShiftToBinary && ReorderIncorrectShiftTransition(transitions);
				}
			}
			return false;
		}

		internal static bool ReorderIncorrectBinaryTransition(IList<ITransition> transitions)
		{
			int shiftCount = 0;
			IListIterator<ITransition> cursor = transitions.ListIterator();
			do
			{
				if (!cursor.MoveNext())
				{
					return false;
				}
				ITransition next = cursor.Current;
				if (next is ShiftTransition)
				{
					++shiftCount;
				}
				else
				{
					if (next is BinaryTransition)
					{
						--shiftCount;
						if (shiftCount <= 0)
						{
							cursor.Remove();
						}
					}
				}
			}
			while (shiftCount > 0);
			if (!cursor.MoveNext())
			{
				return false;
			}
			ITransition next_1 = cursor.Current;
			while ((next_1 is UnaryTransition) || (next_1 is CompoundUnaryTransition))
			{
				cursor.Remove();
				if (!cursor.MoveNext())
				{
					return false;
				}
				next_1 = cursor.Current;
			}
			// At this point, the rest of the transition sequence should suffice
			return true;
		}

		/// <summary>
		/// In this case, we are starting to build a new subtree when instead
		/// we should have been combining existing trees.
		/// </summary>
		/// <remarks>
		/// In this case, we are starting to build a new subtree when instead
		/// we should have been combining existing trees.  What we can do is
		/// find the transitions that build up the next subtree in the gold
		/// transition list, figure out how it gets applied to a
		/// BinaryTransition, and make that the next BinaryTransition we
		/// perform after finishing the subtree.  If there are multiple
		/// BinaryTransitions in a row, we ignore any associated
		/// UnaryTransitions (unfixable) and try to transition to the final
		/// state.  The assumption is that we can't do anything about the
		/// incorrect subtrees any more, so we skip them all.
		/// <br />
		/// Sadly, this does not seem to help - the parser gets worse when it
		/// learns these states
		/// </remarks>
		internal static bool ReorderIncorrectShiftTransition(IList<ITransition> transitions)
		{
			IList<BinaryTransition> leftoverBinary = Generics.NewArrayList();
			while (transitions.Count > 0)
			{
				ITransition head = transitions.Remove(0);
				if (head is ShiftTransition)
				{
					break;
				}
				if (head is BinaryTransition)
				{
					leftoverBinary.Add((BinaryTransition)head);
				}
			}
			if (transitions.Count == 0 || leftoverBinary.Count == 0)
			{
				// honestly this is an error we should probably just throw
				return false;
			}
			int shiftCount = 0;
			IListIterator<ITransition> cursor = transitions.ListIterator();
			BinaryTransition lastBinary = null;
			while (cursor.MoveNext() && shiftCount >= 0)
			{
				ITransition next = cursor.Current;
				if (next is ShiftTransition)
				{
					++shiftCount;
				}
				else
				{
					if (next is BinaryTransition)
					{
						--shiftCount;
						if (shiftCount < 0)
						{
							lastBinary = (BinaryTransition)next;
							cursor.Remove();
						}
					}
				}
			}
			if (!cursor.MoveNext() || lastBinary == null)
			{
				// once again, an error.  even if the sequence of tree altering
				// gold transitions ends with a BinaryTransition, there should
				// be a FinalizeTransition after that
				return false;
			}
			string label = lastBinary.label;
			if (lastBinary.IsBinarized())
			{
				label = Sharpen.Runtime.Substring(label, 1);
			}
			if (lastBinary.side == BinaryTransition.Side.Right)
			{
				// When we finally transition all the binary transitions, we
				// will want to have the new node be the right head.  Therefore,
				// we add a bunch of temporary binary transitions with a right
				// head, ending up with a binary transition with a right head
				for (int i = 0; i < leftoverBinary.Count; ++i)
				{
					cursor.Add(new BinaryTransition("@" + label, BinaryTransition.Side.Right));
				}
				// use lastBinary.label in case the last transition is temporary
				cursor.Add(new BinaryTransition(lastBinary.label, BinaryTransition.Side.Right));
			}
			else
			{
				cursor.Add(new BinaryTransition("@" + label, BinaryTransition.Side.Left));
				for (int i = 0; i < leftoverBinary.Count - 1; ++i)
				{
					cursor.Add(new BinaryTransition("@" + label, leftoverBinary[i].side));
				}
				cursor.Add(new BinaryTransition(lastBinary.label, leftoverBinary[leftoverBinary.Count - 1].side));
			}
			return true;
		}
	}
}
