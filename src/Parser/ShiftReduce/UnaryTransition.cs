using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>Transition that makes a unary parse node in a partially finished tree.</summary>
	[System.Serializable]
	public class UnaryTransition : ITransition
	{
		public readonly string label;

		/// <summary>root transitions are illegal in the middle of the tree, naturally</summary>
		public readonly bool isRoot;

		public UnaryTransition(string label, bool isRoot)
		{
			this.label = label;
			this.isRoot = isRoot;
		}

		/// <summary>Legal as long as there is at least one item on the state's stack.</summary>
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
			if (top.Label().Value().Equals(label))
			{
				// Disallow unary transitions where the label doesn't change
				return false;
			}
			if (top.Label().Value().StartsWith("@") && !label.Equals(Sharpen.Runtime.Substring(top.Label().Value(), 1)))
			{
				return false;
			}
			if (top.Children().Length == 1)
			{
				Tree child = top.Children()[0];
				if (child.Children().Length == 1)
				{
					Tree grandChild = child.Children()[0];
					if (grandChild.Children().Length == 1)
					{
						// Three consecutive unary trees.  Not legal to keep adding unaries.
						// TODO: do preterminals count in that equation?
						return false;
					}
				}
			}
			if (isRoot && (state.stack.Size() > 1 || !state.EndOfQueue()))
			{
				return false;
			}
			// UnaryTransition actually doesn't care about the constraints.
			// If the constraint winds up unsatisfied, we'll get stuck and
			// have to do an "emergency transition" to fix the situation.
			return true;
		}

		/// <summary>Add a unary node to the existing node on top of the stack</summary>
		public virtual State Apply(State state)
		{
			return Apply(state, 0.0);
		}

		internal static Tree AddUnaryNode(Tree top, string label)
		{
			if (!(top.Label() is CoreLabel))
			{
				throw new ArgumentException("Stack should have CoreLabel nodes");
			}
			Tree newTop = CreateNode(top, label, top);
			return newTop;
		}

		internal static Tree CreateNode(Tree top, string label, params Tree[] children)
		{
			CoreLabel headLabel = (CoreLabel)top.Label();
			CoreLabel production = new CoreLabel();
			production.SetValue(label);
			production.Set(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation), headLabel.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation)));
			production.Set(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation), headLabel.Get(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation)));
			Tree newTop = new LabeledScoredTreeNode(production);
			foreach (Tree child in children)
			{
				newTop.AddChild(child);
			}
			return newTop;
		}

		/// <summary>Add a unary node to the existing node on top of the stack</summary>
		public virtual State Apply(State state, double scoreDelta)
		{
			Tree top = state.stack.Peek();
			Tree newTop = AddUnaryNode(top, label);
			TreeShapedStack<Tree> stack = state.stack.Pop();
			stack = stack.Push(newTop);
			return new State(stack, state.transitions.Push(this), state.separators, state.sentence, state.tokenPosition, state.score + scoreDelta, false);
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Parser.Shiftreduce.UnaryTransition))
			{
				return false;
			}
			string otherLabel = ((Edu.Stanford.Nlp.Parser.Shiftreduce.UnaryTransition)o).label;
			return label.Equals(otherLabel);
		}

		public override int GetHashCode()
		{
			return 29467607 ^ label.GetHashCode();
		}

		public override string ToString()
		{
			return "Unary" + (isRoot ? "*" : string.Empty) + "(" + label + ")";
		}

		private const long serialVersionUID = 1;
	}
}
