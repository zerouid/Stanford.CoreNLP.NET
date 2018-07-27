

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// An interace for drawing samples from the label
	/// space of an object.
	/// </summary>
	/// <remarks>
	/// An interace for drawing samples from the label
	/// space of an object.  The classifiers themselves are
	/// <see cref="Sampleable"/>
	/// .  For instance, a parser can
	/// <see cref="Sampleable"/>
	/// and then vends Sampler instances
	/// based on specific inputs (words in the sentence).
	/// The Sampler would then return parse trees (over
	/// that particular sentence, not over all sentences)
	/// drawn from
	/// the underlying distribution.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public interface ISampler<T>
	{
		/// <returns>
		/// labels (of type T) drawn from the underlying
		/// distribution for the observation this Sampler was
		/// created for.
		/// </returns>
		T DrawSample();
	}
}
