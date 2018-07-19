using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Top-level interface for transforming Documents.</summary>
	/// <author>Sepandar Kamvar (sdkamvar@stanford.edu)</author>
	/// <seealso cref="IDocumentProcessor{IN, OUT, L, F}.ProcessDocument(Edu.Stanford.Nlp.Ling.IDocument{L, F, T})"/>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	public interface IDocumentProcessor<In, Out, L, F>
	{
		/// <summary>
		/// Converts a Document to a different Document, by transforming
		/// or filtering the original Document.
		/// </summary>
		/// <remarks>
		/// Converts a Document to a different Document, by transforming
		/// or filtering the original Document. The general contract of this method
		/// is to not modify the <code>in</code> Document in any way, and to
		/// preserve the metadata of the <code>in</code> Document in the
		/// returned Document.
		/// </remarks>
		/// <seealso cref="FunctionProcessor"/>
		IDocument<L, F, OUT> ProcessDocument(IDocument<L, F, IN> @in);
	}
}
