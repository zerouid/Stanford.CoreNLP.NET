using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// This is an interface for probability measures, which will allow
	/// samples to be drawn and the probability of objects computed.
	/// </summary>
	/// <author>Jenny Finkel</author>
	public interface IProbabilityDistribution<E>
	{
		double ProbabilityOf(E @object);

		double LogProbabilityOf(E @object);

		E DrawSample(Random random);
	}
}
