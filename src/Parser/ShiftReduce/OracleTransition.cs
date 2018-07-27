

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>Used internally by the Oracle.</summary>
	/// <remarks>
	/// Used internally by the Oracle.  If the next oracle transition is
	/// known, that is stored.  Otherwise, general classes of transitions
	/// are allowed.
	/// </remarks>
	/// <author>John Bauer</author>
	internal class OracleTransition
	{
		internal readonly ITransition transition;

		internal readonly bool allowsShift;

		internal readonly bool allowsBinary;

		internal readonly bool allowsEitherSide;

		internal OracleTransition(ITransition transition, bool allowsShift, bool allowsBinary, bool allowsEitherSide)
		{
			this.transition = transition;
			this.allowsShift = allowsShift;
			this.allowsBinary = allowsBinary;
			this.allowsEitherSide = allowsEitherSide;
		}

		internal virtual bool IsCorrect(ITransition other)
		{
			if (transition != null && transition.Equals(other))
			{
				return true;
			}
			if (allowsShift && (other is ShiftTransition))
			{
				return true;
			}
			if (allowsBinary && (other is BinaryTransition))
			{
				return true;
			}
			if (allowsEitherSide && (other is BinaryTransition) && (transition is BinaryTransition))
			{
				if (((BinaryTransition)other).label.Equals(((BinaryTransition)transition).label))
				{
					return true;
				}
			}
			return false;
		}
	}
}
