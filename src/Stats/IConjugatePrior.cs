

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>An interface for classes which are probability distributions over other probability distributions.</summary>
	/// <remarks>
	/// An interface for classes which are probability distributions over other probability distributions.
	/// This class could also represent non-conjugate priors, but we thought this name would be more
	/// representative of the common case.
	/// </remarks>
	/// <author>grenager</author>
	/// <author>jrfinkel</author>
	/// <?/>
	/// <?/>
	public interface IConjugatePrior<T, E> : IProbabilityDistribution<T>
		where T : IProbabilityDistribution<E>
	{
		/// <summary>
		/// Marginalizes over all possible likelihood distributions to give the marginal probability of
		/// the observation.
		/// </summary>
		double GetPredictiveProbability(E observation);

		double GetPredictiveLogProbability(E observation);

		/// <summary>Gets the posterior probability of the observation, after conditioning on all of the evidence.</summary>
		/// <remarks>
		/// Gets the posterior probability of the observation, after conditioning on all of the evidence.
		/// Marginalizes over all possible likelihood distributions.
		/// </remarks>
		double GetPosteriorPredictiveProbability(ICounter<E> evidence, E observation);

		double GetPosteriorPredictiveLogProbability(ICounter<E> evidence, E observation);

		/// <summary>Gets the ConjugatePrior which results from conditioning on all of these evidence.</summary>
		IConjugatePrior<T, E> GetPosteriorDistribution(ICounter<E> evidence);
	}
}
