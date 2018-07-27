

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>The interface for unconstrained function minimizers.</summary>
	/// <remarks>
	/// The interface for unconstrained function minimizers.
	/// Implementations may also vary in their requirements for the
	/// arguments.  For example, implementations may or may not care if the
	/// <c>initial</c>
	/// feasible vector turns out to be non-feasible
	/// (or
	/// <see langword="null"/>
	/// !).  Similarly, some methods may insist that objectives
	/// and/or constraint
	/// <c>Function</c>
	/// objects actually be
	/// <c>DiffFunction</c>
	/// objects.
	/// </remarks>
	/// <author><a href="mailto:klein@cs.stanford.edu">Dan Klein</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public interface IMinimizer<T>
		where T : IFunction
	{
		/// <summary>
		/// Attempts to find an unconstrained minimum of the objective
		/// <paramref name="function"/>
		/// starting at
		/// <paramref name="initial"/>
		/// , accurate to
		/// within
		/// <paramref name="functionTolerance"/>
		/// (normally implemented as
		/// a multiplier of the range value to give range tolerance).
		/// </summary>
		/// <param name="function">The objective function</param>
		/// <param name="functionTolerance">
		/// A
		/// <c>double</c>
		/// value
		/// </param>
		/// <param name="initial">An initial feasible point</param>
		/// <returns>Unconstrained minimum of function</returns>
		double[] Minimize(T function, double functionTolerance, double[] initial);

		/// <summary>
		/// Attempts to find an unconstrained minimum of the objective
		/// <paramref name="function"/>
		/// starting at
		/// <paramref name="initial"/>
		/// , accurate to
		/// within
		/// <paramref name="functionTolerance"/>
		/// (normally implemented as
		/// a multiplier of the range value to give range tolerance), but
		/// running only for at most
		/// <paramref name="maxIterations"/>
		/// iterations.
		/// </summary>
		/// <param name="function">The objective function</param>
		/// <param name="functionTolerance">
		/// A
		/// <c>double</c>
		/// value
		/// </param>
		/// <param name="initial">An initial feasible point</param>
		/// <param name="maxIterations">Maximum number of iterations</param>
		/// <returns>Unconstrained minimum of function</returns>
		double[] Minimize(T function, double functionTolerance, double[] initial, int maxIterations);
	}
}
