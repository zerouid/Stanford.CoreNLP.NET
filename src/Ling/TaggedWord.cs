using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>A <code>TaggedWord</code> object contains a word and its tag.</summary>
	/// <remarks>
	/// A <code>TaggedWord</code> object contains a word and its tag.
	/// The <code>value()</code> of a TaggedWord is the Word.  The tag
	/// is secondary.
	/// </remarks>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class TaggedWord : Word, IHasTag
	{
		private string tag;

		private const string Divider = "/";

		/// <summary>Create a new <code>TaggedWord</code>.</summary>
		/// <remarks>
		/// Create a new <code>TaggedWord</code>.
		/// It will have <code>null</code> for its content fields.
		/// </remarks>
		public TaggedWord()
			: base()
		{
		}

		/// <summary>Create a new <code>TaggedWord</code>.</summary>
		/// <param name="word">The word, which will have a <code>null</code> tag</param>
		public TaggedWord(string word)
			: base(word)
		{
		}

		/// <summary>Create a new <code>TaggedWord</code>.</summary>
		/// <param name="word">The word</param>
		/// <param name="tag">The tag</param>
		public TaggedWord(string word, string tag)
			: base(word)
		{
			this.tag = tag;
		}

		/// <summary>Create a new <code>TaggedWord</code>.</summary>
		/// <param name="oldLabel">
		/// A Label.  If it implements the HasWord and/or
		/// HasTag interface, then the corresponding value will be set
		/// </param>
		public TaggedWord(ILabel oldLabel)
			: base(oldLabel.Value())
		{
			if (oldLabel is IHasTag)
			{
				this.tag = ((IHasTag)oldLabel).Tag();
			}
		}

		/// <summary>Create a new <code>TaggedWord</code>.</summary>
		/// <param name="word">This word is passed to the supertype constructor</param>
		/// <param name="tag">
		/// The <code>value()</code> of this label is set as the
		/// tag of this Label
		/// </param>
		public TaggedWord(ILabel word, ILabel tag)
			: base(word)
		{
			this.tag = tag.Value();
		}

		public virtual string Tag()
		{
			return tag;
		}

		public virtual void SetTag(string tag)
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

		/// <summary>
		/// Sets a TaggedWord from decoding
		/// the <code>String</code> passed in.
		/// </summary>
		/// <remarks>
		/// Sets a TaggedWord from decoding
		/// the <code>String</code> passed in.  The String is divided according
		/// to the divider character (usually, "/").  We assume that we can
		/// always just
		/// divide on the rightmost divider character, rather than trying to
		/// parse up escape sequences.  If the divider character isn't found
		/// in the word, then the whole string becomes the word, and the tag
		/// is <code>null</code>.
		/// </remarks>
		/// <param name="taggedWord">The word that will go into the <code>Word</code></param>
		public override void SetFromString(string taggedWord)
		{
			SetFromString(taggedWord, Divider);
		}

		public virtual void SetFromString(string taggedWord, string divider)
		{
			int where = taggedWord.LastIndexOf(divider);
			if (where >= 0)
			{
				SetWord(Sharpen.Runtime.Substring(taggedWord, 0, where));
				SetTag(Sharpen.Runtime.Substring(taggedWord, where + 1));
			}
			else
			{
				SetWord(taggedWord);
				SetTag(null);
			}
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
			return TaggedWord.LabelFactoryHolder.lf;
		}

		/// <summary>Return a factory for this kind of label.</summary>
		/// <returns>The label factory</returns>
		public static ILabelFactory Factory()
		{
			return TaggedWord.LabelFactoryHolder.lf;
		}

		private const long serialVersionUID = -7252006452127051085L;
	}
}
