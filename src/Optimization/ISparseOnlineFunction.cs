using Edu.Stanford.Nlp.Stats;
using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>An interface for functions over sparse parameters.</summary>
	/// <remarks>
	/// An interface for functions over sparse parameters.
	/// the point is to run this online, and data control is passed to the optimizer
	/// K is probably a String or an int
	/// selectedData are the data points used in the current evaluation,
	/// which is more naturally handled by the minimizers instead of the implementation
	/// though if one prefers, one can implement that elsewhere, and make valueAt,
	/// derivativeAt independent of selectedData
	/// </remarks>
	/// <author><a href="mailto:sidaw@cs.stanford.edu">Sida Wang</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public interface ISparseOnlineFunction<K>
	{
		/// <summary>Returns the value of the function at a single point.</summary>
		/// <param name="x">a <code>double[]</code> input</param>
		/// <returns>the function value at the input</returns>
		double ValueAt(ICounter<K> x, int[] selectedData);

		ICounter<K> DerivativeAt(ICounter<K> x, int[] selectedData);

		// return the size of the data, return -1 if you want to handle data selection yourself
		int DataSize();
	}
}
