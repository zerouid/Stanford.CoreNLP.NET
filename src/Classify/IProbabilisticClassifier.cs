using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	public interface IProbabilisticClassifier<L, F> : IClassifier<L, F>
	{
		ICounter<L> ProbabilityOf(IDatum<L, F> example);

		ICounter<L> LogProbabilityOf(IDatum<L, F> example);
	}
}
