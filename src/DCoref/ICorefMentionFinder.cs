using System.Collections.Generic;
using Edu.Stanford.Nlp.Pipeline;


namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>Interface for finding coref mentions in a document.</summary>
	/// <author>Angel Chang</author>
	public interface ICorefMentionFinder
	{
		/// <summary>Get all the predicted mentions for a document.</summary>
		/// <param name="doc">The syntactically annotated document</param>
		/// <param name="maxGoldID">The last mention ID assigned.  New ones are assigned starting one above this number.</param>
		/// <param name="dict">Dictionaries for coref.</param>
		/// <returns>For each of the List of sentences in the document, a List of Mention objects</returns>
		IList<IList<Mention>> ExtractPredictedMentions(Annotation doc, int maxGoldID, Dictionaries dict);
	}
}
