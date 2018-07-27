namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>
	/// A simple interface for training a Classifier from a Dataset of training
	/// examples.
	/// </summary>
	/// <author>
	/// Dan Klein
	/// Templatized by Sarah Spikes (sdspikes@cs.stanford.edu)
	/// </author>
	public interface IClassifierFactory<L, F, C>
		where C : IClassifier<L, F>
	{
		C TrainClassifier(GeneralDataset<L, F> dataset);
	}
}
