

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>A <code>LabeledWord</code> object contains a word and its tag.</summary>
	/// <remarks>
	/// A <code>LabeledWord</code> object contains a word and its tag.
	/// The <code>value()</code> of a TaggedWord is the Word.  The tag
	/// is, and is a Label instead of a String
	/// </remarks>
	[System.Serializable]
	public class LabeledWord : Word
	{
		private ILabel tag;

		private const string Divider = "/";

		/// <summary>Create a new <code>TaggedWord</code>.</summary>
		/// <remarks>
		/// Create a new <code>TaggedWord</code>.
		/// It will have <code>null</code> for its content fields.
		/// </remarks>
		public LabeledWord()
			: base()
		{
		}

		/// <summary>Create a new <code>TaggedWord</code>.</summary>
		/// <param name="word">The word, which will have a <code>null</code> tag</param>
		public LabeledWord(string word)
			: base(word)
		{
		}

		/// <summary>Create a new <code>TaggedWord</code>.</summary>
		/// <param name="word">The word</param>
		/// <param name="tag">The tag</param>
		public LabeledWord(string word, ILabel tag)
			: base(word)
		{
			this.tag = tag;
		}

		public LabeledWord(ILabel word, ILabel tag)
			: base(word)
		{
			this.tag = tag;
		}

		public virtual ILabel Tag()
		{
			return tag;
		}

		public virtual void SetTag(ILabel tag)
		{
			this.tag = tag;
		}

		public override string ToString()
		{
			return ToString(Divider);
		}

		public virtual string ToString(string divider)
		{
			return Word() + divider + tag;
		}

		private class LabelFactoryHolder
		{
			private LabelFactoryHolder()
			{
			}

			private static readonly ILabelFactory lf = new TaggedWordFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// Return a factory for this kind of label
		/// (i.e., <code>TaggedWord</code>).
		/// </summary>
		/// <remarks>
		/// Return a factory for this kind of label
		/// (i.e., <code>TaggedWord</code>).
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The label factory</returns>
		public override ILabelFactory LabelFactory()
		{
			return LabeledWord.LabelFactoryHolder.lf;
		}

		/// <summary>Return a factory for this kind of label.</summary>
		/// <returns>The label factory</returns>
		public static ILabelFactory Factory()
		{
			return LabeledWord.LabelFactoryHolder.lf;
		}

		private const long serialVersionUID = -7252006452127051085L;
	}
}
