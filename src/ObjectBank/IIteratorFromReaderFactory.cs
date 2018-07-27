using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Objectbank
{
	/// <summary>
	/// An IteratorFromReaderFactory is used to convert a java.io.Reader
	/// into an Iterator over the Objects of type T represented by the text
	/// in the java.io.Reader.
	/// </summary>
	/// <remarks>
	/// An IteratorFromReaderFactory is used to convert a java.io.Reader
	/// into an Iterator over the Objects of type T represented by the text
	/// in the java.io.Reader.
	/// (We have it be Serializable just to avoid non-serializable warnings;
	/// since implementations of this class normally have no state, they
	/// should be trivially serializable.)
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public interface IIteratorFromReaderFactory<T>
	{
		/// <summary>Return an iterator over the contents read from r.</summary>
		/// <param name="r">Where to read objects from</param>
		/// <returns>An Iterator over the objects</returns>
		IEnumerator<T> GetIterator(Reader r);
	}
}
