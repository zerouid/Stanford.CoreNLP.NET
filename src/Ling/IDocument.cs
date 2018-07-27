using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>Represents a text document as a list of Words with a String title.</summary>
	/// <author>Sepandar Kamvar (sdkamvar@stanford.edu)</author>
	/// <author>Joseph Smarr (jsmarr@stanford.edu)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization - added another parameter)</author>
	/// <?/>
	/// <?/>
	public interface IDocument<L, F, T> : IDatum<L, F>, IList<T>
	{
		/// <summary>Returns title of document, or "" if the document has no title.</summary>
		/// <remarks>
		/// Returns title of document, or "" if the document has no title.
		/// Implementations should never return <tt>null</tt>.
		/// </remarks>
		/// <returns>The document's title</returns>
		string Title();

		/// <summary>
		/// Returns a new empty Document with the same meta-data (title, labels, etc)
		/// as this Document.
		/// </summary>
		/// <remarks>
		/// Returns a new empty Document with the same meta-data (title, labels, etc)
		/// as this Document. Subclasses that store extra state should provide custom
		/// implementations of this method. This method is primarily used by the
		/// processing API, so the input document can be preserved and the output
		/// document can maintain the meta-data of the in document.
		/// </remarks>
		/// <returns>An empty document of the right sort.</returns>
		IDocument<L, F, OUT> BlankDocument<Out>();
	}
}
