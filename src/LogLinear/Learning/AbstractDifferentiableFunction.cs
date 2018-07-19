using Edu.Stanford.Nlp.Loglinear.Model;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Learning
{
	/// <summary>Created on 8/26/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This provides a separation between the functions and optimizers, that lets us test optimizers more effectively by
	/// generating convex functions that are solvable in closed form, then checking the optimizer arrives at the same
	/// solution.
	/// </author>
	public abstract class AbstractDifferentiableFunction<T>
	{
		/// <summary>Gets a summary of the function of a singe data instance at a single point</summary>
		/// <param name="dataPoint">the data point we want a summary for</param>
		/// <param name="weights">the weights to use</param>
		/// <param name="gradient">the gradient to use, will be updated by accumulating the gradient from this instance</param>
		/// <returns>value of the function at this point</returns>
		public abstract double GetSummaryForInstance(T dataPoint, ConcatVector weights, ConcatVector gradient);
	}
}
