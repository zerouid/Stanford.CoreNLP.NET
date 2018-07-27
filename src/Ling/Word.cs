

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A
	/// <c>Word</c>
	/// object acts as a Label by containing a String.
	/// This class is in essence identical to a
	/// <c>StringLabel</c>
	/// , but
	/// it also uses the value to implement the
	/// <c>HasWord</c>
	/// interface.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <version>2000/12/20</version>
	[System.Serializable]
	public class Word : StringLabel, IHasWord
	{
		/// <summary>String representation of an empty.</summary>
		private const string EmptyString = "*t*";

		/// <summary>Word representation of an empty.</summary>
		public static readonly Edu.Stanford.Nlp.Ling.Word Empty = new Edu.Stanford.Nlp.Ling.Word(EmptyString);

		/// <summary>
		/// Construct a new word with a
		/// <see langword="null"/>
		/// value.
		/// </summary>
		public Word()
			: base()
		{
		}

		/// <summary>Construct a new word, with the given value.</summary>
		/// <param name="word">String value of the Word</param>
		public Word(string word)
			: base(word)
		{
		}

		/// <summary>Construct a new word, with the given value.</summary>
		/// <param name="word">String value of the Word</param>
		public Word(string word, int beginPosition, int endPosition)
			: base(word, beginPosition, endPosition)
		{
		}

		/// <summary>
		/// Creates a new word whose word value is the value of any
		/// class that supports the
		/// <c>Label</c>
		/// interface.
		/// </summary>
		/// <param name="lab">The label to be used as the basis of the new Word</param>
		public Word(ILabel lab)
			: base(lab)
		{
		}

		public virtual string Word()
		{
			return Value();
		}

		public virtual void SetWord(string word)
		{
			SetValue(word);
		}

		private class WordFactoryHolder
		{
			private static readonly ILabelFactory lf = new WordFactory();

			private WordFactoryHolder()
			{
			}
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// Return a factory for this kind of label (i.e.,
		/// <c>Word</c>
		/// ).
		/// The factory returned is always the same one (a singleton).
		/// </summary>
		/// <returns>The label factory</returns>
		public override ILabelFactory LabelFactory()
		{
			return Word.WordFactoryHolder.lf;
		}

		/// <summary>Return a factory for this kind of label.</summary>
		/// <returns>The label factory</returns>
		public static ILabelFactory Factory()
		{
			return Word.WordFactoryHolder.lf;
		}

		private const long serialVersionUID = -4817252915997034058L;
	}
}
