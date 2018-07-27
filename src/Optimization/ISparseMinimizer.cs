using Edu.Stanford.Nlp.Stats;


namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// The interface for unconstrained function minimizers with sparse parameters
	/// like Minimizer, except with sparse parameters
	/// </summary>
	/// <author><a href="mailto:sidaw@cs.stanford.edu">Sida Wang</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public interface ISparseMinimizer<K, T>
		where T : ISparseOnlineFunction<K>
	{
		/// <summary>
		/// Attempts to find an unconstrained minimum of the objective
		/// <code>function</code> starting at <code>initial</code>, within
		/// <code>functionTolerance</code>.
		/// </summary>
		/// <param name="function">the objective function</param>
		/// <param name="initial">a initial feasible point</param>
		/// <returns>Unconstrained minimum of function</returns>
		ICounter<K> Minimize(T function, ICounter<K> initial);

		ICounter<K> Minimize(T function, ICounter<K> initial, int maxIterations);
	}
}
