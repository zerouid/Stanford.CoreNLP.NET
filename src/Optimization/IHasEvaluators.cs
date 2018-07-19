using Sharpen;

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>Indicates that an minimizer supports evaluation periodically</summary>
	/// <author>Angel Chang</author>
	public interface IHasEvaluators
	{
		void SetEvaluators(int iters, IEvaluator[] evaluators);
	}
}
