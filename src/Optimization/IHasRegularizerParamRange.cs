using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>
	/// Indicates that a Function should only be regularized on a subset
	/// of its parameters.
	/// </summary>
	/// <author>Mengqiu Wang</author>
	public interface IHasRegularizerParamRange
	{
		ICollection<int> GetRegularizerParamRange(double[] x);
	}
}
