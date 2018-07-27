using System.Collections.Generic;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>
	/// This interface is used for reading data and writing output into and out of sequence
	/// classifiers.
	/// </summary>
	/// <remarks>
	/// This interface is used for reading data and writing output into and out of sequence
	/// classifiers. If you subclass this interface, all of the other mechanisms necessary
	/// for getting your data into a sequence classifier will be taken care of for you.
	/// Subclasses <b>MUST</b> have an empty constructor so they can be instantiated by
	/// reflection, and there is a promise that the init method will be called
	/// immediately after construction.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public interface IDocumentReaderAndWriter<In> : IIteratorFromReaderFactory<IList<In>>
		where In : ICoreMap
	{
		/* Serializable, */
		/// <summary>This will be called immediately after construction.</summary>
		/// <remarks>
		/// This will be called immediately after construction.  It's easier having
		/// an init() method because DocumentReaderAndWriter objects are usually
		/// created using reflection.
		/// </remarks>
		/// <param name="flags">Flags specifying behavior</param>
		void Init(SeqClassifierFlags flags);

		/// <summary>
		/// This method prints the output of the classifier to a
		/// <see cref="Java.IO.PrintWriter"/>
		/// .
		/// </summary>
		/// <param name="doc">The document which has answers (it has been classified)</param>
		/// <param name="out">Where to send the output</param>
		void PrintAnswers(IList<In> doc, PrintWriter @out);
	}
}
