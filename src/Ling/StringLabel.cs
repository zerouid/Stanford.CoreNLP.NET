

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A <code>StringLabel</code> object acts as a Label by containing a
	/// single String, which it sets or returns in response to requests.
	/// </summary>
	/// <remarks>
	/// A <code>StringLabel</code> object acts as a Label by containing a
	/// single String, which it sets or returns in response to requests.
	/// The hashCode() and compareTo() methods for this class assume that this
	/// string value is non-null.  equals() is correctly implemented
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <version>2000/12/20</version>
	[System.Serializable]
	public class StringLabel : ValueLabel, IHasOffset
	{
		private string str;

		/// <summary>Start position of the word in the original input string</summary>
		private int beginPosition = -1;

		/// <summary>End position of the word in the original input string</summary>
		private int endPosition = -1;

		/// <summary>Create a new <code>StringLabel</code> with a null content (i.e., str).</summary>
		public StringLabel()
		{
		}

		/// <summary>Create a new <code>StringLabel</code> with the given content.</summary>
		/// <param name="str">The new label's content</param>
		public StringLabel(string str)
		{
			this.str = str;
		}

		/// <summary>Create a new <code>StringLabel</code> with the given content.</summary>
		/// <param name="str">The new label's content</param>
		/// <param name="beginPosition">Start offset in original text</param>
		/// <param name="endPosition">End offset in original text</param>
		public StringLabel(string str, int beginPosition, int endPosition)
		{
			this.str = str;
			SetBeginPosition(beginPosition);
			SetEndPosition(endPosition);
		}

		/// <summary>
		/// Create a new <code>StringLabel</code> with the
		/// <code>value()</code> of another label as its label.
		/// </summary>
		/// <param name="label">The other label</param>
		public StringLabel(ILabel label)
		{
			this.str = label.Value();
			if (label is IHasOffset)
			{
				IHasOffset ofs = (IHasOffset)label;
				SetBeginPosition(ofs.BeginPosition());
				SetEndPosition(ofs.EndPosition());
			}
		}

		/// <summary>Return the word value of the label (or null if none).</summary>
		/// <returns>String the word value for the label</returns>
		public override string Value()
		{
			return str;
		}

		/// <summary>Set the value for the label.</summary>
		/// <param name="value">The value for the label</param>
		public override void SetValue(string value)
		{
			str = value;
		}

		/// <summary>Set the label from a String.</summary>
		/// <param name="str">The str for the label</param>
		public override void SetFromString(string str)
		{
			this.str = str;
		}

		public override string ToString()
		{
			return str;
		}

		private class StringLabelFactoryHolder
		{
			private StringLabelFactoryHolder()
			{
			}

			internal static readonly ILabelFactory lf = new StringLabelFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// Return a factory for this kind of label
		/// (i.e., <code>StringLabel</code>).
		/// </summary>
		/// <remarks>
		/// Return a factory for this kind of label
		/// (i.e., <code>StringLabel</code>).
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The label factory</returns>
		public override ILabelFactory LabelFactory()
		{
			return StringLabel.StringLabelFactoryHolder.lf;
		}

		/// <summary>Return a factory for this kind of label.</summary>
		/// <returns>The label factory</returns>
		public static ILabelFactory Factory()
		{
			return StringLabel.StringLabelFactoryHolder.lf;
		}

		public virtual int BeginPosition()
		{
			return beginPosition;
		}

		public virtual int EndPosition()
		{
			return endPosition;
		}

		public virtual void SetBeginPosition(int beginPosition)
		{
			this.beginPosition = beginPosition;
		}

		public virtual void SetEndPosition(int endPosition)
		{
			this.endPosition = endPosition;
		}

		private const long serialVersionUID = -4153619273767524247L;
	}
}
