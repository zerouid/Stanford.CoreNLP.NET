using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A <code>StringLabelFactory</code> object makes a simple
	/// <code>StringLabel</code> out of a <code>String</code>.
	/// </summary>
	/// <author>Christopher Manning</author>
	public class StringLabelFactory : ILabelFactory
	{
		/// <summary>Make a new label with this <code>String</code> as the "name".</summary>
		/// <param name="labelStr">
		/// A string that determines the content of the label.
		/// For a StringLabel, it is exactly the given string
		/// </param>
		/// <returns>The created label</returns>
		public virtual ILabel NewLabel(string labelStr)
		{
			return new StringLabel(labelStr);
		}

		/// <summary>Make a new label with this <code>String</code> as the "name".</summary>
		/// <param name="labelStr">
		/// A string that determines the content of the label.
		/// For a StringLabel, it is exactly the given string
		/// </param>
		/// <param name="options">The options are ignored by a StringLabelFactory</param>
		/// <returns>The created label</returns>
		public virtual ILabel NewLabel(string labelStr, int options)
		{
			return new StringLabel(labelStr);
		}

		/// <summary>Make a new label with this <code>String</code> as the "name".</summary>
		/// <remarks>
		/// Make a new label with this <code>String</code> as the "name".
		/// This version does no decoding -- StringLabels just have a value.
		/// </remarks>
		/// <param name="labelStr">
		/// A string that determines the content of the label.
		/// For a StringLabel, it is exactly the given string
		/// </param>
		/// <returns>The created label</returns>
		public virtual ILabel NewLabelFromString(string labelStr)
		{
			return new StringLabel(labelStr);
		}

		/// <summary>
		/// Create a new <code>StringLabel</code>, where the label is
		/// formed from
		/// the <code>Label</code> object passed in.
		/// </summary>
		/// <remarks>
		/// Create a new <code>StringLabel</code>, where the label is
		/// formed from
		/// the <code>Label</code> object passed in.  Depending on what fields
		/// each label has, other things will be <code>null</code>.
		/// </remarks>
		/// <param name="oldLabel">The Label that the new label is being created from</param>
		/// <returns>a new label of a particular type</returns>
		public virtual ILabel NewLabel(ILabel oldLabel)
		{
			return new StringLabel(oldLabel);
		}
	}
}
