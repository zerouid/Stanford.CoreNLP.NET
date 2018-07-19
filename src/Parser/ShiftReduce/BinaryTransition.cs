using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>Transition that makes a binary parse node in a partially finished tree.</summary>
	[System.Serializable]
	public class BinaryTransition : ITransition
	{
		public readonly string label;

		/// <summary>Which side the head is on</summary>
		public readonly BinaryTransition.Side side;

		public enum Side
		{
			Left,
			Right
		}

		public BinaryTransition(string label, BinaryTransition.Side side)
		{
			this.label = label;
			this.side = side;
		}

		/// <summary>Legal as long as there are at least two items on the state's stack.</summary>
		public virtual bool IsLegal(State state, IList<ParserConstraint> constraints)
		{
			// some of these quotes come directly from Zhang Clark 09
			if (state.finished)
			{
				return false;
			}
			if (state.stack.Size() <= 1)
			{
				return false;
			}
			// at least one of the two nodes on top of stack must be non-temporary
			if (ShiftReduceUtils.IsTemporary(state.stack.Peek()) && ShiftReduceUtils.IsTemporary(state.stack.Pop().Peek()))
			{
				return false;
			}
			if (ShiftReduceUtils.IsTemporary(state.stack.Peek()))
			{
				if (side == BinaryTransition.Side.Left)
				{
					return false;
				}
				if (!ShiftReduceUtils.IsEquivalentCategory(label, state.stack.Peek().Value()))
				{
					return false;
				}
			}
			if (ShiftReduceUtils.IsTemporary(state.stack.Pop().Peek()))
			{
				if (side == BinaryTransition.Side.Right)
				{
					return false;
				}
				if (!ShiftReduceUtils.IsEquivalentCategory(label, state.stack.Pop().Peek().Value()))
				{
					return false;
				}
			}
			// don't allow binarized labels if it makes the state have a stack
			// of size 1 and a queue of size 0
			if (state.stack.Size() == 2 && IsBinarized() && state.EndOfQueue())
			{
				return false;
			}
			// when the stack contains only two nodes, temporary resulting
			// nodes from binary reduce must be left-headed
			if (state.stack.Size() == 2 && IsBinarized() && side == BinaryTransition.Side.Right)
			{
				return false;
			}
			// when the queue is empty and the stack contains more than two
			// nodes, with the third node from the top being temporary, binary
			// reduce can be applied only if the resulting node is non-temporary
			if (state.EndOfQueue() && state.stack.Size() > 2 && ShiftReduceUtils.IsTemporary(state.stack.Pop().Pop().Peek()) && IsBinarized())
			{
				return false;
			}
			// when the stack contains more than two nodes, with the third
			// node from the top being temporary, temporary resulting nodes
			// from binary reduce must be left-headed
			if (state.stack.Size() > 2 && ShiftReduceUtils.IsTemporary(state.stack.Pop().Pop().Peek()) && IsBinarized() && side == BinaryTransition.Side.Right)
			{
				return false;
			}
			if (constraints == null)
			{
				return true;
			}
			Tree top = state.stack.Peek();
			int leftTop = ShiftReduceUtils.LeftIndex(top);
			int rightTop = ShiftReduceUtils.RightIndex(top);
			Tree next = state.stack.Pop().Peek();
			int leftNext = ShiftReduceUtils.LeftIndex(next);
			// The binary transitions are affected by constraints in the
			// following two circumstances.  If a transition would cross the
			// left boundary of a constraint, that is illegal.  If the
			// transition is exactly the right size for the constraint and
			// would make a temporary node, that is also illegal.
			foreach (ParserConstraint constraint in constraints)
			{
				if (leftTop == constraint.start)
				{
					// can't binary reduce away from a tree which doesn't match a constraint
					if (rightTop == constraint.end - 1)
					{
						if (!ShiftReduceUtils.ConstraintMatchesTreeTop(top, constraint))
						{
							return false;
						}
						else
						{
							continue;
						}
					}
					else
					{
						if (rightTop >= constraint.end)
						{
							continue;
						}
						else
						{
							// can't binary reduce if it would make the tree cross the left boundary
							return false;
						}
					}
				}
				// top element is further left than the constraint, so
				// there's no harm to be done by binary reduce
				if (leftTop < constraint.start)
				{
					continue;
				}
				// top element is past the end of the constraint, so it must already be satisfied
				if (leftTop >= constraint.end)
				{
					continue;
				}
				// now leftTop > constraint.start and < constraint.end, eg inside the constraint
				// the next case is no good because it crosses the boundary
				if (leftNext < constraint.start)
				{
					return false;
				}
				if (leftNext > constraint.start)
				{
					continue;
				}
				// can't transition to a binarized node when there's a constraint that matches.
				if (rightTop == constraint.end - 1 && IsBinarized())
				{
					return false;
				}
			}
			return true;
		}

		public virtual bool IsBinarized()
		{
			return (label[0] == '@');
		}

		/// <summary>Add a binary node to the existing node on top of the stack</summary>
		public virtual State Apply(State state)
		{
			return Apply(state, 0.0);
		}

		/// <summary>Add a binary node to the existing node on top of the stack</summary>
		public virtual State Apply(State state, double scoreDelta)
		{
			TreeShapedStack<Tree> stack = state.stack;
			Tree right = stack.Peek();
			stack = stack.Pop();
			Tree left = stack.Peek();
			stack = stack.Pop();
			Tree head;
			switch (side)
			{
				case BinaryTransition.Side.Left:
				{
					head = left;
					break;
				}

				case BinaryTransition.Side.Right:
				{
					head = right;
					break;
				}

				default:
				{
					throw new ArgumentException("Unknown side " + side);
				}
			}
			if (!(head.Label() is CoreLabel))
			{
				throw new ArgumentException("Stack should have CoreLabel nodes");
			}
			CoreLabel headLabel = (CoreLabel)head.Label();
			CoreLabel production = new CoreLabel();
			production.SetValue(label);
			production.Set(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation), headLabel.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation)));
			production.Set(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation), headLabel.Get(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation)));
			Tree newTop = new LabeledScoredTreeNode(production);
			newTop.AddChild(left);
			newTop.AddChild(right);
			stack = stack.Push(newTop);
			return new State(stack, state.transitions.Push(this), state.separators, state.sentence, state.tokenPosition, state.score + scoreDelta, false);
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Parser.Shiftreduce.BinaryTransition))
			{
				return false;
			}
			string otherLabel = ((Edu.Stanford.Nlp.Parser.Shiftreduce.BinaryTransition)o).label;
			BinaryTransition.Side otherSide = ((Edu.Stanford.Nlp.Parser.Shiftreduce.BinaryTransition)o).side;
			return otherSide.Equals(side) && label.Equals(otherLabel);
		}

		public override int GetHashCode()
		{
			switch (side)
			{
				case BinaryTransition.Side.Left:
				{
					// TODO: fix the hashcode for the side?  would require rebuilding all models
					return 97197711 ^ label.GetHashCode();
				}

				case BinaryTransition.Side.Right:
				{
					return 97197711 ^ label.GetHashCode();
				}

				default:
				{
					throw new ArgumentException("Unknown side " + side);
				}
			}
		}

		public override string ToString()
		{
			switch (side)
			{
				case BinaryTransition.Side.Left:
				{
					return "LeftBinary(" + label + ")";
				}

				case BinaryTransition.Side.Right:
				{
					return "RightBinary(" + label + ")";
				}

				default:
				{
					throw new ArgumentException("Unknown side " + side);
				}
			}
		}

		private const long serialVersionUID = 1;
	}
}
