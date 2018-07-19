using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// Something that implements the <code>Label</code> interface can act as a
	/// constituent, node, or word label with linguistic attributes.
	/// </summary>
	/// <remarks>
	/// Something that implements the <code>Label</code> interface can act as a
	/// constituent, node, or word label with linguistic attributes.
	/// A <code>Label</code> is required to have a "primary" <code>String</code>
	/// <code>value()</code> (although this may be null).  This is referred to as
	/// its <code>value</code>.
	/// <p>
	/// Implementations of Label split into two groups with
	/// respect to equality. Classes that extend ValueLabel define equality
	/// solely in terms of String equality of its value (secondary facets may be
	/// present but are ignored for purposes of equality), and have equals and
	/// compareTo defined across all subclasses of ValueLabel. This behavior
	/// should not be changed. Other classes that implement Label define equality only
	/// with their own type and require all fields of the type to be equal.
	/// <p>
	/// A subclass that extends another Label class <i>should</i> override
	/// the definition of <code>labelFactory()</code>, since the contract for
	/// this method is that it should return a factory for labels of the
	/// exact same object type.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public interface ILabel
	{
		/// <summary>Return a String representation of just the "main" value of this label.</summary>
		/// <returns>the "value" of the label</returns>
		string Value();

		/// <summary>Set the value for the label (if one is stored).</summary>
		/// <param name="value">- the value for the label</param>
		void SetValue(string value);

		/// <summary>Return a String representation of the label.</summary>
		/// <remarks>
		/// Return a String representation of the label.  For a multipart label,
		/// this will return all parts.  The <code>toString()</code> method
		/// causes a label to spill its guts.  It should always return an
		/// empty string rather than <code>null</code> if there is no value.
		/// </remarks>
		/// <returns>a text representation of the full label contents</returns>
		string ToString();

		/// <summary>
		/// Set the contents of this label to this <code>String</code>
		/// representing the
		/// complete contents of the label.
		/// </summary>
		/// <remarks>
		/// Set the contents of this label to this <code>String</code>
		/// representing the
		/// complete contents of the label.  A class implementing label may
		/// throw an <code>UnsupportedOperationException</code> for this
		/// method (only).  Typically, this method would do
		/// some appropriate decoding of the string in a way that sets
		/// multiple fields in an inverse of the <code>toString()</code>
		/// method.
		/// </remarks>
		/// <param name="labelStr">
		/// the String that translates into the content of the
		/// label
		/// </param>
		void SetFromString(string labelStr);

		/// <summary>Returns a factory that makes labels of the exact same type as this one.</summary>
		/// <remarks>
		/// Returns a factory that makes labels of the exact same type as this one.
		/// May return <code>null</code> if no appropriate factory is known.
		/// </remarks>
		/// <returns>the LabelFactory for this kind of label</returns>
		ILabelFactory LabelFactory();
	}
}
