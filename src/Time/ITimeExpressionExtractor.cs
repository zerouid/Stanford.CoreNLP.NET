using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Time
{
	/// <summary>A TimeExpressionExtractor extracts a list of time expressions from a document annotation.</summary>
	/// <author>Angel Chang</author>
	public interface ITimeExpressionExtractor
	{
		void Init(string name, Properties props);

		void Init(Options options);

		/// <summary>Extract time expressions from a sentence in a document.</summary>
		/// <remarks>
		/// Extract time expressions from a sentence in a document.  The document is assumed to contain the document date.
		/// The document is also used to hold stateful information (e.g. the index used by SUTime to generate timex ids).
		/// Both the sentence and document are provided as a CoreMap Annotation.
		/// </remarks>
		/// <param name="annotation">- Annotation holding tokenized text from which the time expressions are to be extracted</param>
		/// <param name="docAnnotation">
		/// - Annotation for the entire document
		/// Uses the following annotations:
		/// CoreAnnotations.DocDateAnnotation.class (String representing document date)
		/// TimeExpression.TimeIndexAnnotation.class (Holds index used to generate tids)
		/// </param>
		/// <returns>List of CoreMaps</returns>
		IList<ICoreMap> ExtractTimeExpressionCoreMaps(ICoreMap annotation, ICoreMap docAnnotation);

		/// <summary>Extract time expressions in a document (provided as a CoreMap Annotation).</summary>
		/// <param name="annotation">The annotation to run time expression extraction over</param>
		/// <param name="docDate">A date for the document to be used as a reference time.</param>
		/// <returns>
		/// A list of CoreMap.  Each CoreMap represents a detected temporal
		/// expression.  Each CoreMap is a pipeline.Annotation, and you can get
		/// various attributes of the temporal expression out of it. For example,
		/// you can get the list of tokens with:
		/// <pre>
		/// <c>
		/// List&lt;CoreMap&gt; cm = extractTimeExpressionCoreMaps(annotation, docDate);
		/// List&lt;CoreLabel&gt; tokens = cm.get(CoreAnnotations.TokensAnnotation.class);
		/// </c>
		/// </pre>
		/// </returns>
		IList<ICoreMap> ExtractTimeExpressionCoreMaps(ICoreMap annotation, string docDate);

		~ITimeExpressionExtractor()
		{
		}
	}
}
