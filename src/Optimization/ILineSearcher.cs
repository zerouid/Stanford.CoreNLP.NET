using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>The interface for one variable function minimizers.</summary>
	/// <author>Jenny Finkel</author>
	public interface ILineSearcher
	{
		/// <summary>
		/// Attempts to find an unconstrained minimum of the objective
		/// <paramref name="function"/>
		/// starting at
		/// <c>initial</c>
		/// , within
		/// <c>functionTolerance</c>
		/// .
		/// </summary>
		/// <param name="function">the objective function</param>
		double Minimize(IDoubleUnaryOperator function);
	}
}
