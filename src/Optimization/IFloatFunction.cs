using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>An interface for double-valued functions over double arrays.</summary>
	/// <author><a href="mailto:klein@cs.stanford.edu">Dan Klein</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public interface IFloatFunction
	{
		/// <summary>Returns the value of the function at a single point.</summary>
		/// <param name="x">a <code>double[]</code> input</param>
		/// <returns>the function value at the input</returns>
		float ValueAt(float[] x);

		/// <summary>Returns the number of dimensions in the function's domain</summary>
		/// <returns>the number of domain dimensions</returns>
		int DomainDimension();
	}
}
