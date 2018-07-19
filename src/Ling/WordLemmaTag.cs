using Edu.Stanford.Nlp.Process;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A WordLemmaTag corresponds to a pair of a tagged (e.g., for part of speech)
	/// word and its lemma.
	/// </summary>
	/// <remarks>
	/// A WordLemmaTag corresponds to a pair of a tagged (e.g., for part of speech)
	/// word and its lemma. WordLemmaTag is implemented with String-valued word,
	/// lemma and tag.
	/// It implements the Label interface; the
	/// <c>value()</c>
	/// method for that
	/// interface corresponds to the word of the WordLemmaTag.
	/// <p>
	/// The equality relation for WordLemmaTag is defined as identity of
	/// word, lemma and tag.
	/// </remarks>
	/// <author>Marie-Catherine de Marneffe</author>
	[System.Serializable]
	public class WordLemmaTag : ILabel, IComparable<Edu.Stanford.Nlp.Ling.WordLemmaTag>, IHasWord, IHasTag
	{
		private string word;

		private string lemma;

		private string tag;

		private const string Divider = "/";

		public WordLemmaTag(string word)
		{
			this.word = word;
			this.lemma = null;
			SetTag(null);
		}

		public WordLemmaTag(ILabel word)
			: this(word.Value())
		{
		}

		public WordLemmaTag()
		{
		}

		/// <summary>
		/// Create a new
		/// <c>WordLemmaTag</c>
		/// .
		/// </summary>
		/// <param name="word">This word is set as the word of this Label</param>
		/// <param name="tag">
		/// The
		/// <c>value()</c>
		/// of this Label is set as the
		/// tag of this Label
		/// </param>
		public WordLemmaTag(string word, string tag)
		{
			WordTag wT = new WordTag(word, tag);
			this.word = word;
			this.lemma = Morphology.StemStatic(wT).Word();
			SetTag(tag);
		}

		/// <summary>
		/// Create a new
		/// <c>WordLemmaTag</c>
		/// .
		/// </summary>
		/// <param name="word">This word is passed to the supertype constructor</param>
		/// <param name="lemma">The lemma is set as the lemma of this Label</param>
		/// <param name="tag">
		/// The
		/// <c>value()</c>
		/// of this Label is set as the
		/// tag of this Label
		/// </param>
		public WordLemmaTag(string word, string lemma, string tag)
			: this(word)
		{
			this.lemma = lemma;
			SetTag(tag);
		}

		/// <summary>
		/// Create a new
		/// <c>WordLemmaTag</c>
		/// from a Label.  The value of
		/// the Label corresponds to the word of the WordLemmaTag.
		/// </summary>
		/// <param name="word">This word is passed to the supertype constructor</param>
		/// <param name="tag">
		/// The
		/// <c>value()</c>
		/// of this Label is set as the
		/// tag of this Label
		/// </param>
		public WordLemmaTag(ILabel word, ILabel tag)
			: this(word)
		{
			WordTag wT = new WordTag(word, tag);
			this.lemma = Morphology.StemStatic(wT).Word();
			SetTag(tag.Value());
		}

		/// <summary>Return a String representation of just the "main" value of this Label.</summary>
		/// <returns>the "value" of the Label</returns>
		public virtual string Value()
		{
			return word;
		}

		public virtual string Word()
		{
			return Value();
		}

		/// <summary>Set the value for the Label.</summary>
		/// <param name="value">the value for the Label</param>
		public virtual void SetValue(string value)
		{
			word = value;
		}

		public virtual void SetWord(string word)
		{
			SetValue(word);
		}

		public virtual void SetLemma(string lemma)
		{
			this.lemma = lemma;
		}

		/// <summary>Set the tag for the Label.</summary>
		/// <param name="tag">the value for the Label</param>
		public void SetTag(string tag)
		{
			this.tag = tag;
		}

		public virtual string Tag()
		{
			return tag;
		}

		public virtual string Lemma()
		{
			return lemma;
		}

		/// <summary>Return a String representation of the Label.</summary>
		/// <remarks>
		/// Return a String representation of the Label.  For a multipart Label,
		/// this will return all parts.
		/// </remarks>
		/// <returns>a text representation of the full label contents: word/lemma/tag</returns>
		public override string ToString()
		{
			return ToString(Divider);
		}

		public virtual string ToString(string divider)
		{
			return Word() + divider + lemma + divider + tag;
		}

		/// <summary>The String is divided according to the divider character (usually, "/").</summary>
		/// <remarks>
		/// The String is divided according to the divider character (usually, "/").
		/// We assume that we can always just divide on the rightmost divider character,
		/// rather than trying to parse up escape sequences.  If the divider character isn't found
		/// in the word, then the whole string becomes the word, and lemma and tag
		/// are
		/// <see langword="null"/>
		/// .
		/// We assume that if only one divider character is found, word and tag are present in
		/// the String, and lemma will be computed.
		/// </remarks>
		/// <param name="labelStr">
		/// The word that will go into the
		/// <c>WordLemmaTag</c>
		/// </param>
		public virtual void SetFromString(string labelStr)
		{
			SetFromString(labelStr, Divider);
		}

		public virtual void SetFromString(string labelStr, string divider)
		{
			int first = labelStr.IndexOf(divider);
			int second = labelStr.LastIndexOf(divider);
			if (first == second)
			{
				SetWord(Sharpen.Runtime.Substring(labelStr, 0, first));
				SetTag(Sharpen.Runtime.Substring(labelStr, first + 1));
				SetLemma(Morphology.LemmaStatic(Sharpen.Runtime.Substring(labelStr, 0, first), Sharpen.Runtime.Substring(labelStr, first + 1)));
			}
			else
			{
				if (first >= 0)
				{
					SetWord(Sharpen.Runtime.Substring(labelStr, 0, first));
					SetLemma(Sharpen.Runtime.Substring(labelStr, first + 1, second));
					SetTag(Sharpen.Runtime.Substring(labelStr, second + 1));
				}
				else
				{
					SetWord(labelStr);
					SetLemma(null);
					SetTag(null);
				}
			}
		}

		/// <summary>
		/// Equality is satisfied only if the compared object is a WordLemmaTag
		/// and has String-equal word, lemma and tag fields.
		/// </summary>
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Ling.WordLemmaTag))
			{
				return false;
			}
			Edu.Stanford.Nlp.Ling.WordLemmaTag other = (Edu.Stanford.Nlp.Ling.WordLemmaTag)o;
			return Word().Equals(other.Word()) && Lemma().Equals(other.Lemma()) && Tag().Equals(other.Tag());
		}

		public override int GetHashCode()
		{
			int result;
			result = (word != null ? word.GetHashCode() : 3);
			result = 29 * result + (tag != null ? tag.GetHashCode() : 0);
			result = 29 * result + (lemma != null ? lemma.GetHashCode() : 0);
			return result;
		}

		/// <summary>Orders first by word, then by lemma, then by tag.</summary>
		/// <param name="wordLemmaTag">object to compare to</param>
		/// <returns>
		/// result (positive if
		/// <c>this</c>
		/// is greater than
		/// <c>obj</c>
		/// , 0 if equal, negative otherwise)
		/// </returns>
		public virtual int CompareTo(Edu.Stanford.Nlp.Ling.WordLemmaTag wordLemmaTag)
		{
			int first = string.CompareOrdinal(Word(), wordLemmaTag.Word());
			if (first != 0)
			{
				return first;
			}
			int second = string.CompareOrdinal(Lemma(), wordLemmaTag.Lemma());
			if (second != 0)
			{
				return second;
			}
			else
			{
				return string.CompareOrdinal(Tag(), wordLemmaTag.Tag());
			}
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
			return new WordLemmaTagFactory();
		}

		/*for debugging only*/
		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Ling.WordLemmaTag wLT = new Edu.Stanford.Nlp.Ling.WordLemmaTag();
			wLT.SetFromString("hunter/NN");
			System.Console.Out.WriteLine(wLT.Word());
			System.Console.Out.WriteLine(wLT.Lemma());
			System.Console.Out.WriteLine(wLT.Tag());
			Edu.Stanford.Nlp.Ling.WordLemmaTag wLT2 = new Edu.Stanford.Nlp.Ling.WordLemmaTag();
			wLT2.SetFromString("bought/buy/V");
			System.Console.Out.WriteLine(wLT2.Word());
			System.Console.Out.WriteLine(wLT2.Lemma());
			System.Console.Out.WriteLine(wLT2.Tag());
			Edu.Stanford.Nlp.Ling.WordLemmaTag wLT3 = new Edu.Stanford.Nlp.Ling.WordLemmaTag();
			wLT2.SetFromString("life");
			System.Console.Out.WriteLine(wLT3.Word());
			System.Console.Out.WriteLine(wLT3.Lemma());
			System.Console.Out.WriteLine(wLT3.Tag());
		}

		private const long serialVersionUID = -5993410244163988138L;
	}
}
