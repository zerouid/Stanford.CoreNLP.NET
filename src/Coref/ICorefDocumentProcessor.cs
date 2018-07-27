using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Coref
{
	/// <summary>An interface for classes that iterate through coreference documents and process them one by one.</summary>
	/// <author>Kevin Clark</author>
	public interface ICorefDocumentProcessor
	{
		void Process(int id, Document document);

		/// <exception cref="System.Exception"/>
		void Finish();

		string GetName();

		/// <exception cref="System.Exception"/>
		void Run(Properties props, Dictionaries dictionaries);

		/// <exception cref="System.Exception"/>
		void RunFromScratch(Properties props, Dictionaries dictionaries);

		// Some annotators produce slightly different outputs when running over the same input data
		// twice. Here we first clear annotator pool to avoid this.
		/// <exception cref="System.Exception"/>
		void Run(DocumentMaker docMaker);
	}
}
