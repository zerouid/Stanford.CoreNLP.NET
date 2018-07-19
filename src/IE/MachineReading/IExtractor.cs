using Edu.Stanford.Nlp.Pipeline;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading
{
	public interface IExtractor
	{
		// TODO make this an abstract class instead so setLoggerLevel doesn't have to be implemented by all subclasses.
		// also, should add the load() method here -- though it can't be static, so maybe we need a different approach.
		// Extractors should have a logger as an instance attribute.
		/// <summary>Trains one extractor model using the given dataset</summary>
		/// <param name="dataset">
		/// dataset to train from (this should already have annotations and
		/// will typically be created by a reader)
		/// </param>
		void Train(Annotation dataset);

		/// <summary>
		/// Annotates the given dataset with the current model This works in place,
		/// i.e., it adds ExtractionObject objects to the sentences in the dataset To
		/// make sure you are not messing with gold annotation create a copy of the
		/// ExtractionDataSet first!
		/// </summary>
		/// <param name="dataset">dataset to annotate</param>
		void Annotate(Annotation dataset);

		/// <summary>Serializes this extractor to a file</summary>
		/// <param name="path">where to save the extractor</param>
		/// <exception cref="System.IO.IOException"/>
		void Save(string path);

		void SetLoggerLevel(Level level);
	}
}
