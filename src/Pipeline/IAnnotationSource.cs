using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Data source from which annotations comes from</summary>
	/// <author>Angel Chang</author>
	public interface IAnnotationSource
	{
		/// <summary>Returns a iterable of annotations given input string (i.e.</summary>
		/// <remarks>Returns a iterable of annotations given input string (i.e. filename, lucene query, etc)</remarks>
		/// <param name="selector">- selector of what annotations to return</param>
		/// <param name="limit">- limit on the number of annotations to return (0 or less for unlimited)</param>
		/// <returns>iterable of annotations</returns>
		IEnumerable<Annotation> GetAnnotations(string selector, int limit);

		/// <summary>Returns a iterable of annotations given input string (i.e.</summary>
		/// <remarks>Returns a iterable of annotations given input string (i.e. filename, lucene query, etc)</remarks>
		/// <param name="selector">- selector of what annotations to return</param>
		/// <returns>iterable of annotations</returns>
		IEnumerable<Annotation> GetAnnotations(string selector);
	}
}
