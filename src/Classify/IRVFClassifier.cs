using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;


namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// A simple interface for classifying and scoring data points with
	/// real-valued features.
	/// </summary>
	/// <remarks>
	/// A simple interface for classifying and scoring data points with
	/// real-valued features.  Implemented by the linear classifier.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	public interface IRVFClassifier<L, F>
	{
		L ClassOf(RVFDatum<L, F> example);

		ICounter<L> ScoresOf(RVFDatum<L, F> example);
	}
}
