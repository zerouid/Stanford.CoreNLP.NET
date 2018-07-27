using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Common;


namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>Transition that finishes the processing of a state</summary>
	[System.Serializable]
	public class FinalizeTransition : ITransition
	{
		private readonly ICollection<string> rootStates;

		public FinalizeTransition(ICollection<string> rootStates)
		{
			this.rootStates = rootStates;
		}

		public virtual bool IsLegal(State state, IList<ParserConstraint> constraints)
		{
			bool legal = !state.finished && state.tokenPosition >= state.sentence.Count && state.stack.Size() == 1 && rootStates.Contains(state.stack.Peek().Value());
			if (!legal || constraints == null)
			{
				return legal;
			}
			foreach (ParserConstraint constraint in constraints)
			{
				if (constraint.start != 0 || constraint.end != state.sentence.Count)
				{
					continue;
				}
				if (!ShiftReduceUtils.ConstraintMatchesTreeTop(state.stack.Peek(), constraint))
				{
					return false;
				}
			}
			return true;
		}

		public virtual State Apply(State state)
		{
			return Apply(state, 0.0);
		}

		public virtual State Apply(State state, double scoreDelta)
		{
			return new State(state.stack, state.transitions.Push(this), state.separators, state.sentence, state.tokenPosition, state.score + scoreDelta, true);
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (o is Edu.Stanford.Nlp.Parser.Shiftreduce.FinalizeTransition)
			{
				return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return 593744340;
		}

		// a random int
		public override string ToString()
		{
			return "Finalize";
		}

		private const long serialVersionUID = 1;
	}
}
