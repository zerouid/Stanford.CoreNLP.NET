using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Common;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>
	/// An interface which defines a transition type in the shift-reduce
	/// parser.
	/// </summary>
	/// <remarks>
	/// An interface which defines a transition type in the shift-reduce
	/// parser.  Expected transition types are shift, unary, binary,
	/// finalize, and idle.
	/// <br />
	/// There is also a compound unary transition for combining multiple
	/// unary transitions into one, which lets us prevent the parser from
	/// creating arbitrary unary transition sequences.
	/// </remarks>
	public interface ITransition
	{
		/// <summary>Whether or not it is legal to apply this transition to this state.</summary>
		bool IsLegal(State state, IList<ParserConstraint> constraints);

		/// <summary>Applies this transition to get a new state.</summary>
		State Apply(State state);

		/// <summary>Applies this transition to get a new state.</summary>
		State Apply(State state, double scoreDelta);
	}
}
