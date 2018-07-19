using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Common
{
	/// <summary>
	/// A Constraint represents a restriction on possible parse trees to
	/// consider.
	/// </summary>
	/// <remarks>
	/// A Constraint represents a restriction on possible parse trees to
	/// consider.  It says that a parse cannot be postulated that would
	/// contradict having a constituent from position start to end, and
	/// that any constituent postulated with this span must match the
	/// regular expression given by state.  Note, however, that it does not
	/// strictly guarantee that such a constituent exists in a returned
	/// parse.
	/// <br />
	/// The words in constraint bounds are counted starting from 0.
	/// Furthermore, the end number is not included in the constraint.
	/// For example, a constraint covering the first two words of a
	/// sentence has a start of 0 and an end of 2.
	/// <br />
	/// state must successfully match states used internal to the parser,
	/// not just the final states, so the pattern "NP" will not match
	/// anything.  Better is "NP.*".  Note that this does run the risk of
	/// matching states with the same prefix.  For example, there is SBAR
	/// and SBARQ, so "SBAR.*" could have unexpected results.  The states
	/// used internal to the parser extend the state name with
	/// non-alphanumeric characters, so a fancy expression such as
	/// "SBAR|SBAR[^a-zA-Z].*" would match SBAR but not SBARQ constituents.
	/// </remarks>
	[System.Serializable]
	public class ParserConstraint
	{
		public readonly int start;

		public readonly int end;

		public readonly Pattern state;

		private const long serialVersionUID = 2;

		public ParserConstraint(int start, int end, string pattern)
			: this(start, end, Pattern.Compile(pattern))
		{
		}

		public ParserConstraint(int start, int end, Pattern state)
		{
			//public ParserConstraint() {}
			this.start = start;
			this.end = end;
			this.state = state;
		}

		public override string ToString()
		{
			return "ParserConstraint(" + start + "," + end + ":" + state + ")";
		}
	}
}
