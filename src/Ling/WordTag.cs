using System;




namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A WordTag corresponds to a tagged (e.g., for part of speech) word
	/// and is implemented with String-valued word and tag.
	/// </summary>
	/// <remarks>
	/// A WordTag corresponds to a tagged (e.g., for part of speech) word
	/// and is implemented with String-valued word and tag.  It implements
	/// the Label interface; the
	/// <c>value()</c>
	/// method for that
	/// interface corresponds to the word of the WordTag.
	/// <p>
	/// The equality relation for WordTag is defined as identity of both
	/// word and tag.  Note that this is different from
	/// <c>TaggedWord</c>
	/// , for which equality derives from
	/// <c>ValueLabel</c>
	/// and requires only identity of value.
	/// </remarks>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public class WordTag : ILabel, IHasWord, IHasTag, IComparable<Edu.Stanford.Nlp.Ling.WordTag>
	{
		private string word;

		private string tag;

		private const string Divider = "/";

		/// <summary>
		/// Create a new
		/// <c>WordTag</c>
		/// .
		/// </summary>
		/// <param name="word">This word is passed to the supertype constructor</param>
		/// <param name="tag">
		/// The
		/// <c>value()</c>
		/// of this label is set as the
		/// tag of this Label
		/// </param>
		public WordTag(string word, string tag)
		{
			SetWord(word);
			SetTag(tag);
		}

		public WordTag(string word)
			: this(word, null)
		{
		}

		public WordTag(E word)
			: this(word.Value(), word.Tag())
		{
		}

		private WordTag()
		{
		}

		/// <summary>
		/// Create a new
		/// <c>WordTag</c>
		/// from a Label.  The value of
		/// the Label corresponds to the word of the WordTag.
		/// </summary>
		/// <param name="word">
		/// The
		/// <c>value()</c>
		/// of this label is set as the
		/// word of the
		/// <c>WordTag</c>
		/// </param>
		/// <param name="tag">
		/// The
		/// <c>value()</c>
		/// of this label is set as the
		/// tag of the
		/// <c>WordTag</c>
		/// </param>
		public WordTag(ILabel word, ILabel tag)
			: this(word.Value(), tag.Value())
		{
		}

		// only used internally for doing setFromString()
		public static Edu.Stanford.Nlp.Ling.WordTag ValueOf(string s)
		{
			Edu.Stanford.Nlp.Ling.WordTag result = new Edu.Stanford.Nlp.Ling.WordTag();
			result.SetFromString(s);
			return result;
		}

		public static Edu.Stanford.Nlp.Ling.WordTag ValueOf(string s, string tagDivider)
		{
			Edu.Stanford.Nlp.Ling.WordTag result = new Edu.Stanford.Nlp.Ling.WordTag();
			result.SetFromString(s, tagDivider);
			return result;
		}

		/// <summary>Return a String representation of just the "main" value of this label.</summary>
		/// <returns>the "value" of the label</returns>
		public virtual string Value()
		{
			return word;
		}

		public virtual string Word()
		{
			return Value();
		}

		/// <summary>Set the value for the label (if one is stored).</summary>
		/// <param name="value">- the value for the label</param>
		public virtual void SetValue(string value)
		{
			word = value;
		}

		public virtual string Tag()
		{
			return tag;
		}

		public virtual void SetWord(string word)
		{
			SetValue(word);
		}

		public virtual void SetTag(string tag)
		{
			this.tag = tag;
		}

		/// <summary>Return a String representation of the label.</summary>
		/// <remarks>
		/// Return a String representation of the label.  For a multipart label,
		/// this will return all parts.  The
		/// <c>toString()</c>
		/// method
		/// causes a label to spill its guts.  It should always return an
		/// empty string rather than
		/// <see langword="null"/>
		/// if there is no value.
		/// </remarks>
		/// <returns>a text representation of the full label contents</returns>
		public override string ToString()
		{
			return ToString(Divider);
		}

		public virtual string ToString(string divider)
		{
			string tag = Tag();
			if (tag == null)
			{
				return Word();
			}
			else
			{
				return Word() + divider + tag;
			}
		}

		/// <summary>
		/// Sets a WordTag from decoding
		/// the
		/// <c>String</c>
		/// passed in.  The String is divided according
		/// to the divider character (usually, "/").  We assume that we can
		/// always just
		/// divide on the rightmost divider character, rather than trying to
		/// parse up escape sequences.  If the divider character isn't found
		/// in the word, then the whole string becomes the word, and the tag
		/// is
		/// <see langword="null"/>
		/// .
		/// </summary>
		/// <param name="wordTagString">
		/// The word that will go into the
		/// <c>Word</c>
		/// </param>
		public virtual void SetFromString(string wordTagString)
		{
			SetFromString(wordTagString, Divider);
		}

		public virtual void SetFromString(string wordTagString, string divider)
		{
			int where = wordTagString.LastIndexOf(divider);
			if (where >= 0)
			{
				SetWord(string.Intern(Sharpen.Runtime.Substring(wordTagString, 0, where)));
				SetTag(string.Intern(Sharpen.Runtime.Substring(wordTagString, where + 1)));
			}
			else
			{
				SetWord(string.Intern(wordTagString));
				SetTag(null);
			}
		}

		/// <summary>A WordTag is equal only to another WordTag with the same word and tag values.</summary>
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Ling.WordTag))
			{
				return false;
			}
			Edu.Stanford.Nlp.Ling.WordTag wordTag = (Edu.Stanford.Nlp.Ling.WordTag)o;
			if (tag != null ? !tag.Equals(wordTag.tag) : wordTag.tag != null)
			{
				return false;
			}
			if (word != null ? !word.Equals(wordTag.word) : wordTag.word != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result;
			result = (word != null ? word.GetHashCode() : 0);
			result = 29 * result + (tag != null ? tag.GetHashCode() : 0);
			return result;
		}

		/// <summary>Orders first by word, then by tag.</summary>
		/// <param name="wordTag">object to compare to</param>
		/// <returns>
		/// result (positive if
		/// <c>this</c>
		/// is greater than
		/// <c>obj</c>
		/// , 0 if equal, negative otherwise)
		/// </returns>
		public virtual int CompareTo(Edu.Stanford.Nlp.Ling.WordTag wordTag)
		{
			int first = (word != null ? string.CompareOrdinal(Word(), wordTag.Word()) : 0);
			if (first != 0)
			{
				return first;
			}
			else
			{
				if (Tag() == null)
				{
					if (wordTag.Tag() == null)
					{
						return 0;
					}
					else
					{
						return -1;
					}
				}
				return string.CompareOrdinal(Tag(), wordTag.Tag());
			}
		}

		private class LabelFactoryHolder
		{
			private static readonly ILabelFactory lf = new WordTagFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// Return a factory for this kind of label
		/// (i.e.,
		/// <c>TaggedWord</c>
		/// ).
		/// The factory returned is always the same one (a singleton).
		/// </summary>
		/// <returns>The label factory</returns>
		public virtual ILabelFactory LabelFactory()
		{
			return WordTag.LabelFactoryHolder.lf;
		}

		/// <summary>Return a factory for this kind of label.</summary>
		/// <returns>The label factory</returns>
		public static ILabelFactory Factory()
		{
			return WordTag.LabelFactoryHolder.lf;
		}

		public virtual void Read(DataInputStream @in)
		{
			try
			{
				word = @in.ReadUTF();
				tag = @in.ReadUTF();
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		public virtual void Save(DataOutputStream @out)
		{
			try
			{
				@out.WriteUTF(word);
				@out.WriteUTF(tag);
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		private const long serialVersionUID = -1859527239216813742L;
	}
}
