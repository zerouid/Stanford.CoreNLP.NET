using Edu.Stanford.Nlp.Process;


namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A
	/// <c>WordLemmaTagFactory</c>
	/// acts as a factory for creating
	/// objects of class
	/// <c>WordLemmaTag</c>
	/// .
	/// </summary>
	/// <author>Marie-Catherine de Marneffe</author>
	public class WordLemmaTagFactory : ILabelFactory
	{
		public const int LemmaLabel = 1;

		public const int TagLabel = 2;

		private readonly char divider;

		/// <summary>
		/// Create a new
		/// <c>WordLemmaTagFactory</c>
		/// .
		/// The divider will be taken as '/'.
		/// </summary>
		public WordLemmaTagFactory()
			: this('/')
		{
		}

		/// <summary>
		/// Create a new
		/// <c>WordLemmaTagFactory</c>
		/// .
		/// </summary>
		/// <param name="divider">
		/// This character will be used in calls to the one
		/// argument version of
		/// <c>newLabel()</c>
		/// , to divide
		/// word from lemma and tag.
		/// </param>
		public WordLemmaTagFactory(char divider)
		{
			this.divider = divider;
		}

		/// <summary>
		/// Make a new label with this
		/// <c>String</c>
		/// as the value (word).
		/// Any other fields of the label would normally be null.
		/// </summary>
		/// <param name="labelStr">The String that will be used for value</param>
		/// <returns>
		/// The new WordLemmaTag (lemma and tag will be
		/// <see langword="null"/>
		/// )
		/// </returns>
		public virtual ILabel NewLabel(string labelStr)
		{
			return new WordLemmaTag(labelStr);
		}

		/// <summary>
		/// Make a new label with this
		/// <c>String</c>
		/// as a value component.
		/// Any other fields of the label would normally be null.
		/// </summary>
		/// <param name="labelStr">The String that will be used for value</param>
		/// <param name="options">what to make (use labelStr as word, lemma or tag)</param>
		/// <returns>
		/// The new WordLemmaTag (word or lemma or tag will be
		/// <see langword="null"/>
		/// )
		/// </returns>
		public virtual ILabel NewLabel(string labelStr, int options)
		{
			if (options == TagLabel)
			{
				return new WordLemmaTag(null, null, labelStr);
			}
			else
			{
				if (options == LemmaLabel)
				{
					return new WordLemmaTag(null, labelStr, null);
				}
				else
				{
					return new WordLemmaTag(labelStr);
				}
			}
		}

		/// <summary>
		/// Create a new word, where the label is formed from
		/// the
		/// <c>String</c>
		/// passed in.  The String is divided according
		/// to the divider character.  We assume that we can always just
		/// divide on the rightmost divider character, rather than trying to
		/// parse up escape sequences.  If the divider character isn't found
		/// in the word, then the whole string becomes the word, and lemma and tag
		/// are
		/// <see langword="null"/>
		/// .
		/// We assume that if only one divider character is found, word and tag are presents in
		/// the String, and lemma will be computed.
		/// </summary>
		/// <param name="labelStr">
		/// The word that will go into the
		/// <c>Word</c>
		/// </param>
		/// <returns>The new WordLemmaTag</returns>
		public virtual ILabel NewLabelFromString(string labelStr)
		{
			int first = labelStr.IndexOf(divider);
			int second = labelStr.LastIndexOf(divider);
			if (first == second)
			{
				return new WordLemmaTag(Sharpen.Runtime.Substring(labelStr, 0, first), Morphology.LemmaStatic(Sharpen.Runtime.Substring(labelStr, 0, first), Sharpen.Runtime.Substring(labelStr, first + 1)), Sharpen.Runtime.Substring(labelStr, first + 1));
			}
			else
			{
				if (first >= 0)
				{
					return new WordLemmaTag(Sharpen.Runtime.Substring(labelStr, 0, first), Sharpen.Runtime.Substring(labelStr, first + 1, second), Sharpen.Runtime.Substring(labelStr, second + 1));
				}
				else
				{
					return new WordLemmaTag(labelStr);
				}
			}
		}

		/// <summary>
		/// Create a new
		/// <c>WordLemmaTag Label</c>
		/// , where the label is
		/// formed from the
		/// <c>Label</c>
		/// object passed in.  Depending on what fields
		/// each label has, other things will be
		/// <see langword="null"/>
		/// .
		/// </summary>
		/// <param name="oldLabel">The Label that the new label is being created from</param>
		/// <returns>a new label of a particular type</returns>
		public virtual ILabel NewLabel(ILabel oldLabel)
		{
			return new WordLemmaTag(oldLabel);
		}
	}
}
