using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A <code>CategoryWordTagFactory</code> is a factory that makes
	/// a <code>Label</code> which is a <code>CategoryWordTag</code> triplet.
	/// </summary>
	/// <author>Christopher Manning</author>
	public class CategoryWordTagFactory : ILabelFactory
	{
		/// <summary>Make a new label with this <code>String</code> as the "name".</summary>
		/// <param name="labelStr">The string to use as a label</param>
		/// <returns>The newly created Label</returns>
		public virtual ILabel NewLabel(string labelStr)
		{
			return new CategoryWordTag(labelStr);
		}

		/// <summary>Make a new label with this <code>String</code> as the value.</summary>
		/// <remarks>
		/// Make a new label with this <code>String</code> as the value.
		/// This implementation ignores the options
		/// </remarks>
		/// <param name="labelStr">The String that will be used for balue</param>
		/// <param name="options">This argument is ignored</param>
		/// <returns>The newly created Label</returns>
		public virtual ILabel NewLabel(string labelStr, int options)
		{
			return new CategoryWordTag(labelStr);
		}

		/// <summary>Make a new label with this <code>String</code> as the "name".</summary>
		/// <param name="labelStr">The string to use as a label</param>
		/// <returns>The newly created Label</returns>
		public virtual ILabel NewLabelFromString(string labelStr)
		{
			CategoryWordTag cwt = new CategoryWordTag();
			cwt.SetFromString(labelStr);
			return cwt;
		}

		/// <summary>
		/// Create a new CategoryWordTag label, where the label is formed from
		/// the various <code>String</code> objects passed in.
		/// </summary>
		/// <param name="word">The word part of the label</param>
		/// <param name="tag">The tag part of the label</param>
		/// <param name="category">The category part of the label</param>
		/// <returns>The newly created Label</returns>
		public virtual ILabel NewLabel(string word, string tag, string category)
		{
			// System.out.println("Making new CWT label: " + category + " | " +
			//		   word + " | " + tag);
			return new CategoryWordTag(category, word, tag);
		}

		/// <summary>
		/// Create a new <code>CategoryWordTag Label</code>, where the label is
		/// formed from
		/// the <code>Label</code> object passed in.
		/// </summary>
		/// <remarks>
		/// Create a new <code>CategoryWordTag Label</code>, where the label is
		/// formed from
		/// the <code>Label</code> object passed in.  Depending on what fields
		/// each label has, other things will be <code>null</code>.
		/// </remarks>
		/// <param name="oldLabel">The Label that the new label is being created from</param>
		/// <returns>a new label of a particular type</returns>
		public virtual ILabel NewLabel(ILabel oldLabel)
		{
			return new CategoryWordTag(oldLabel);
		}
	}
}
