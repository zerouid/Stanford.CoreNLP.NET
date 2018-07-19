using Sharpen;

namespace Edu.Stanford.Nlp.Fsm
{
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	public interface IAutomatonMinimizer
	{
		TransducerGraph MinimizeFA(TransducerGraph unminimizedFA);
	}
}
