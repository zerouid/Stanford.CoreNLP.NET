using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>
	/// Transition that makes a compound unary parse node in a partially
	/// finished tree.
	/// </summary>
	/// <remarks>
	/// Transition that makes a compound unary parse node in a partially
	/// finished tree.  It potentially adds multiple unary layers to the
	/// current tree.
	/// </remarks>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class CompoundUnaryTransition : ITransition
	{
		/// <summary>labels[0] is the top of the unary chain.</summary>
		/// <remarks>
		/// labels[0] is the top of the unary chain.
		/// A unary chain that results in a ROOT will have labels[0] == ROOT, for example.
		/// </remarks>
		public readonly string[] labels;

		/// <summary>root transitions are illegal in the middle of the tree, naturally</summary>
		public readonly bool isRoot;

		public CompoundUnaryTransition(IList<string> labels, bool isRoot)
		{
			this.labels = new string[labels.Count];
			for (int i = 0; i < labels.Count; ++i)
			{
				this.labels[i] = labels[i];
			}
			this.isRoot = isRoot;
		}

		/// <summary>
		/// Legal as long as there is at least one item on the state's stack
		/// and that item has not already been unary transformed.
		/// </summary>
		public virtual bool IsLegal(State state, IList<ParserConstraint> constraints)
		{
			if (state.finished)
			{
				return false;
			}
			if (state.stack.Size() == 0)
			{
				return false;
			}
			Tree top = state.stack.Peek();
			if (top.Children().Length == 1 && !top.IsPreTerminal())
			{
				// Disallow unary transitions after we've already had a unary transition
				return false;
			}
			if (top.Label().Value().Equals(labels[0]))
			{
				// Disallow unary transitions where the final label doesn't change
				return false;
			}
			// TODO: need to think more about when a unary transition is
			// allowed if the top of the stack is temporary
			if (top.Label().Value().StartsWith("@") && !labels[labels.Length - 1].Equals(Sharpen.Runtime.Substring(top.Label().Value(), 1)))
			{
				// Disallow a transition if the top is a binarized node and the
				// bottom of the unary transition chain isn't the same type
				return false;
			}
			if (isRoot && (state.stack.Size() > 1 || !state.EndOfQueue()))
			{
				return false;
			}
			// Now we check the constraints...
			// Constraints only apply to CompoundUnaryTransitions if the tree
			// is exactly the right size and the tree has not already been
			// constructed to match the constraint.  In that case, we check to
			// see if the candidate transition contains the desired label.
			if (constraints == null)
			{
				return true;
			}
			foreach (ParserConstraint constraint in constraints)
			{
				if (ShiftReduceUtils.LeftIndex(top) != constraint.start || ShiftReduceUtils.RightIndex(top) != constraint.end - 1)
				{
					continue;
				}
				if (constraint.state.Matcher(top.Value()).Matches())
				{
					continue;
				}
				bool found = false;
				foreach (string label in labels)
				{
					if (constraint.state.Matcher(label).Matches())
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Add a unary node to the existing node on top of the stack</summary>
		public virtual State Apply(State state)
		{
			return Apply(state, 0.0);
		}

		/// <summary>Add a unary node to the existing node on top of the stack</summary>
		public virtual State Apply(State state, double scoreDelta)
		{
			Tree top = state.stack.Peek();
			for (int i = labels.Length - 1; i >= 0; --i)
			{
				top = UnaryTransition.AddUnaryNode(top, labels[i]);
			}
			TreeShapedStack<Tree> stack = state.stack.Pop();
			stack = stack.Push(top);
			return new State(stack, state.transitions.Push(this), state.separators, state.sentence, state.tokenPosition, state.score + scoreDelta, false);
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Parser.Shiftreduce.CompoundUnaryTransition))
			{
				return false;
			}
			string[] otherLabels = ((Edu.Stanford.Nlp.Parser.Shiftreduce.CompoundUnaryTransition)o).labels;
			return Arrays.Equals(labels, otherLabels);
		}

		public override int GetHashCode()
		{
			return 29467607 ^ Arrays.HashCode(labels);
		}

		public override string ToString()
		{
			return "CompoundUnary" + (isRoot ? "*" : string.Empty) + "(" + Arrays.AsList(labels).ToString() + ")";
		}

		private const long serialVersionUID = 1;
	}
}
