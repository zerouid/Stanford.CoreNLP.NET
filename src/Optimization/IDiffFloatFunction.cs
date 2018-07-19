using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// An interface for once-differentiable double-valued functions over
	/// double arrays.
	/// </summary>
	/// <remarks>
	/// An interface for once-differentiable double-valued functions over
	/// double arrays.  NOTE: it'd be good to have an AbstractDiffFunction
	/// that wrapped a Function with a finite-difference approximation.
	/// </remarks>
	/// <author><a href="mailto:klein@cs.stanford.edu">Dan Klein</a></author>
	/// <version>1.0</version>
	/// <seealso cref="IFunction"/>
	/// <since>1.0</since>
	public interface IDiffFloatFunction : IFloatFunction
	{
		/// <summary>Returns the first-derivative vector at the input location.</summary>
		/// <param name="x">a <code>double[]</code> input vector</param>
		/// <returns>the vector of first partial derivatives.</returns>
		float[] DerivativeAt(float[] x);
	}
}
