using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A <code>LabelFactory</code> object acts as a factory for creating
	/// objects of class <code>Label</code>, or some descendant class.
	/// </summary>
	/// <remarks>
	/// A <code>LabelFactory</code> object acts as a factory for creating
	/// objects of class <code>Label</code>, or some descendant class.
	/// It can also make Labels from Strings, optionally with options.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <version>2000/12/25</version>
	public interface ILabelFactory
	{
		/// <summary>
		/// Make a new label with this <code>String</code> as the
		/// <code>value</code>.
		/// </summary>
		/// <remarks>
		/// Make a new label with this <code>String</code> as the
		/// <code>value</code>.
		/// Any other fields of the label would normally be <code>null</code>.
		/// </remarks>
		/// <param name="labelStr">The String that will be used for value</param>
		/// <returns>The new Label</returns>
		ILabel NewLabel(string labelStr);

		/// <summary>
		/// Make a new label with this <code>String</code> as the value, and
		/// the type determined in an implementation-dependent way from the
		/// options value.
		/// </summary>
		/// <param name="labelStr">The String that will be used for value</param>
		/// <param name="options">May determine what kind of label is created</param>
		/// <returns>The new Label</returns>
		ILabel NewLabel(string labelStr, int options);

		/// <summary>Make a new label.</summary>
		/// <remarks>
		/// Make a new label.  The String argument will be decomposed into
		/// multiple fields in an implementing class-specific way, in
		/// accordance with the class's setFromString() method.
		/// </remarks>
		/// <param name="encodedLabelStr">
		/// The String that will be used for labelling the
		/// object (by decoding it into parts)
		/// </param>
		/// <returns>The new Label</returns>
		ILabel NewLabelFromString(string encodedLabelStr);

		/// <summary>
		/// Create a new <code>Label</code>, where the label is formed from
		/// the <code>Label</code> object passed in.
		/// </summary>
		/// <remarks>
		/// Create a new <code>Label</code>, where the label is formed from
		/// the <code>Label</code> object passed in.  The new Label is
		/// guaranteed to at least copy the <code>value()</code> of the
		/// source label (if non-null); it may also copy other components
		/// (this is implementation-specific).  However, if oldLabel is of
		/// the same type as is produced by the factory, then the whole
		/// label should be cloned, so that the returnedLabel.equals(oldLabel).
		/// <i>Implementation note:</i> That last sentence isn't true of all
		/// current implementations (e.g., WordTag), but we should make it
		/// so that it is true!
		/// </remarks>
		/// <param name="oldLabel">The Label that the new label is being created from</param>
		/// <returns>The new label of a particular type</returns>
		ILabel NewLabel(ILabel oldLabel);
	}
}
