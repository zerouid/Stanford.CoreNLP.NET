using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <author>Angel Chang</author>
	public interface IEvaluator
	{
		double Evaluate(double[] x);
	}
}
