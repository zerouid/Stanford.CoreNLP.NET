using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A <code>TaggedWordFactory</code> acts as a factory for creating objects of
	/// class <code>TaggedWord</code>.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <version>2000/12/21</version>
	public class TaggedWordFactory : ILabelFactory
	{
		public const int TagLabel = 2;

		private readonly char divider;

		/// <summary>Create a new <code>TaggedWordFactory</code>.</summary>
		/// <remarks>
		/// Create a new <code>TaggedWordFactory</code>.
		/// The divider will be taken as '/'.
		/// </remarks>
		public TaggedWordFactory()
			: this('/')
		{
		}

		/// <summary>Create a new <code>TaggedWordFactory</code>.</summary>
		/// <param name="divider">
		/// This character will be used in calls to the one
		/// argument version of <code>newLabel()</code>, to divide
		/// the word from the tag.  Stuff after the last instance of this
		/// character will become the tag, and stuff before it will
		/// become the label.
		/// </param>
		public TaggedWordFactory(char divider)
		{
			this.divider = divider;
		}

		/// <summary>Make a new label with this <code>String</code> as the value (word).</summary>
		/// <remarks>
		/// Make a new label with this <code>String</code> as the value (word).
		/// Any other fields of the label would normally be null.
		/// </remarks>
		/// <param name="labelStr">The String that will be used for value</param>
		/// <returns>The new TaggedWord (tag will be <code>null</code>)</returns>
		public virtual ILabel NewLabel(string labelStr)
		{
			return new TaggedWord(labelStr);
		}

		/// <summary>Make a new label with this <code>String</code> as a value component.</summary>
		/// <remarks>
		/// Make a new label with this <code>String</code> as a value component.
		/// Any other fields of the label would normally be null.
		/// </remarks>
		/// <param name="labelStr">The String that will be used for value</param>
		/// <param name="options">what to make (use labelStr as word or tag)</param>
		/// <returns>The new TaggedWord (tag or word will be <code>null</code>)</returns>
		public virtual ILabel NewLabel(string labelStr, int options)
		{
			if (options == TagLabel)
			{
				return new TaggedWord(null, labelStr);
			}
			return new TaggedWord(labelStr);
		}

		/// <summary>
		/// Create a new word, where the label is formed from
		/// the <code>String</code> passed in.
		/// </summary>
		/// <remarks>
		/// Create a new word, where the label is formed from
		/// the <code>String</code> passed in.  The String is divided according
		/// to the divider character.  We assume that we can always just
		/// divide on the rightmost divider character, rather than trying to
		/// parse up escape sequences.  If the divider character isn't found
		/// in the word, then the whole string becomes the word, and the tag
		/// is <code>null</code>.
		/// </remarks>
		/// <param name="word">The word that will go into the <code>Word</code></param>
		/// <returns>The new TaggedWord</returns>
		public virtual ILabel NewLabelFromString(string word)
		{
			int where = word.LastIndexOf(divider);
			if (where >= 0)
			{
				return new TaggedWord(Sharpen.Runtime.Substring(word, 0, where), Sharpen.Runtime.Substring(word, where + 1));
			}
			else
			{
				return new TaggedWord(word);
			}
		}

		/// <summary>
		/// Create a new <code>TaggedWord Label</code>, where the label is
		/// formed from
		/// the <code>Label</code> object passed in.
		/// </summary>
		/// <remarks>
		/// Create a new <code>TaggedWord Label</code>, where the label is
		/// formed from
		/// the <code>Label</code> object passed in.  Depending on what fields
		/// each label has, other things will be <code>null</code>.
		/// </remarks>
		/// <param name="oldLabel">The Label that the new label is being created from</param>
		/// <returns>a new label of a particular type</returns>
		public virtual ILabel NewLabel(ILabel oldLabel)
		{
			return new TaggedWord(oldLabel);
		}
	}
}
