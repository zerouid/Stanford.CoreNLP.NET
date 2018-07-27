using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Common;


namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>Transition that literally does nothing</summary>
	[System.Serializable]
	public class IdleTransition : ITransition
	{
		/// <summary>Legal only if the state is already finished</summary>
		public virtual bool IsLegal(State state, IList<ParserConstraint> constraints)
		{
			return state.finished;
		}

		/// <summary>Do nothing</summary>
		public virtual State Apply(State state)
		{
			return Apply(state, 0.0);
		}

		/// <summary>Do nothing</summary>
		public virtual State Apply(State state, double scoreDelta)
		{
			return new State(state.stack, state.transitions.Push(this), state.separators, state.sentence, state.tokenPosition, state.score + scoreDelta, state.finished);
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (o is IdleTransition)
			{
				return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return 586866881;
		}

		// a random int
		public override string ToString()
		{
			return "Idle";
		}

		private const long serialVersionUID = 1;
	}
}
