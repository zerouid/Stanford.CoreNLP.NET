using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A <code>WordFactory</code> acts as a factory for creating objects of
	/// class <code>Word</code>.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <version>2000/12/20</version>
	public class WordFactory : ILabelFactory
	{
		/// <summary>Creates a new WordFactory.</summary>
		public WordFactory()
		{
		}

		/// <summary>
		/// Create a new word, where the label is formed from
		/// the <code>String</code> passed in.
		/// </summary>
		/// <param name="word">The word that will go into the <code>Word</code></param>
		/// <returns>The new label</returns>
		public virtual ILabel NewLabel(string word)
		{
			return new Word(word);
		}

		/// <summary>
		/// Create a new word, where the label is formed from
		/// the <code>String</code> passed in.
		/// </summary>
		/// <param name="word">The word that will go into the <code>Word</code></param>
		/// <param name="options">is ignored by a WordFactory</param>
		/// <returns>The new label</returns>
		public virtual ILabel NewLabel(string word, int options)
		{
			return new Word(word);
		}

		/// <summary>
		/// Create a new word, where the label is formed from
		/// the <code>String</code> passed in.
		/// </summary>
		/// <param name="word">The word that will go into the <code>Word</code></param>
		/// <returns>The new label</returns>
		public virtual ILabel NewLabelFromString(string word)
		{
			return new Word(word);
		}

		/// <summary>
		/// Create a new <code>Word Label</code>, where the label is
		/// formed from
		/// the <code>Label</code> object passed in.
		/// </summary>
		/// <remarks>
		/// Create a new <code>Word Label</code>, where the label is
		/// formed from
		/// the <code>Label</code> object passed in.  Depending on what fields
		/// each label has, other things will be <code>null</code>.
		/// </remarks>
		/// <param name="oldLabel">The Label that the new label is being created from</param>
		/// <returns>a new label of a particular type</returns>
		public virtual ILabel NewLabel(ILabel oldLabel)
		{
			return new Word(oldLabel);
		}
	}
}
