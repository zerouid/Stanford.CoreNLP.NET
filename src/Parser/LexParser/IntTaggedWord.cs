using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// Represents a WordTag (in the sense that equality is defined
	/// on both components), where each half is represented by an
	/// int indexed by a Index.
	/// </summary>
	/// <remarks>
	/// Represents a WordTag (in the sense that equality is defined
	/// on both components), where each half is represented by an
	/// int indexed by a Index.  In this representation, -1 is
	/// used to represent the wildcard ANY value, and -2 is used
	/// to represent a STOP value (i.e., no more dependents).
	/// TODO: does that cause any problems regarding unseen words also being -1?
	/// TODO: any way to not have links to the Index in each object?
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class IntTaggedWord : IComparable<Edu.Stanford.Nlp.Parser.Lexparser.IntTaggedWord>
	{
		public const int AnyWordInt = -1;

		public const int AnyTagInt = -1;

		public const int StopWordInt = -2;

		public const int StopTagInt = -2;

		public const string Any = ".*.";

		public const string Stop = "STOP";

		public readonly int word;

		public readonly short tag;

		public virtual int Tag()
		{
			return tag;
		}

		public virtual int Word()
		{
			return word;
		}

		public virtual string WordString(IIndex<string> wordIndex)
		{
			string wordStr;
			if (word >= 0)
			{
				wordStr = wordIndex.Get(word);
			}
			else
			{
				if (word == AnyWordInt)
				{
					wordStr = Any;
				}
				else
				{
					wordStr = Stop;
				}
			}
			return wordStr;
		}

		public virtual string TagString(IIndex<string> tagIndex)
		{
			string tagStr;
			if (tag >= 0)
			{
				tagStr = tagIndex.Get(tag);
			}
			else
			{
				if (tag == AnyTagInt)
				{
					tagStr = Any;
				}
				else
				{
					tagStr = Stop;
				}
			}
			return tagStr;
		}

		public override int GetHashCode()
		{
			return word ^ (tag << 16);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			else
			{
				if (o is Edu.Stanford.Nlp.Parser.Lexparser.IntTaggedWord)
				{
					Edu.Stanford.Nlp.Parser.Lexparser.IntTaggedWord i = (Edu.Stanford.Nlp.Parser.Lexparser.IntTaggedWord)o;
					return (word == i.word && tag == i.tag);
				}
				else
				{
					return false;
				}
			}
		}

		public virtual int CompareTo(Edu.Stanford.Nlp.Parser.Lexparser.IntTaggedWord that)
		{
			if (tag != that.tag)
			{
				return tag - that.tag;
			}
			else
			{
				return word - that.word;
			}
		}

		private static readonly char[] charsToEscape = new char[] { '\"' };

		public virtual string ToLexicalEntry(IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			string wordStr = WordString(wordIndex);
			string tagStr = TagString(tagIndex);
			return '\"' + StringUtils.EscapeString(tagStr, charsToEscape, '\\') + "\" -> \"" + StringUtils.EscapeString(wordStr, charsToEscape, '\\') + '\"';
		}

		public override string ToString()
		{
			return word + "/" + tag;
		}

		public virtual string ToString(IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			return WordString(wordIndex) + '/' + TagString(tagIndex);
		}

		public virtual string ToString(string arg, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			if (arg.Equals("verbose"))
			{
				return (WordString(wordIndex) + '[' + word + "]/" + TagString(tagIndex) + '[' + tag + ']');
			}
			else
			{
				return ToString(wordIndex, tagIndex);
			}
		}

		public IntTaggedWord(int word, int tag)
		{
			this.word = word;
			this.tag = (short)tag;
		}

		public virtual TaggedWord ToTaggedWord(IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			string wordStr = WordString(wordIndex);
			string tagStr = TagString(tagIndex);
			return new TaggedWord(wordStr, tagStr);
		}

		/// <summary>
		/// Creates an IntTaggedWord given by the String representation
		/// of the form &lt;word&gt;|&lt;tag*gt;
		/// </summary>
		public IntTaggedWord(string s, char splitChar, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: this(ExtractWord(s, splitChar), ExtractTag(s, splitChar), wordIndex, tagIndex)
		{
		}

		// awkward, calls s.indexOf(splitChar) twice
		//    System.out.println("s: " + s);
		//    System.out.println("tagIndex: " + tagIndex);
		//    System.out.println("word: " + word);
		//    System.out.println("tag: " + tag);
		private static string ExtractWord(string s, char splitChar)
		{
			int n = s.LastIndexOf(splitChar);
			string result = Sharpen.Runtime.Substring(s, 0, n);
			//    System.out.println("extracted word: " + result);
			return result;
		}

		private static string ExtractTag(string s, char splitChar)
		{
			int n = s.LastIndexOf(splitChar);
			string result = Sharpen.Runtime.Substring(s, n + 1);
			//    System.out.println("extracted tag: " + result);
			return result;
		}

		/// <summary>Creates an IntTaggedWord given by the tagString and wordString</summary>
		public IntTaggedWord(string wordString, string tagString, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			switch (wordString)
			{
				case Any:
				{
					word = AnyWordInt;
					break;
				}

				case Stop:
				{
					word = StopWordInt;
					break;
				}

				default:
				{
					word = wordIndex.AddToIndex(wordString);
					break;
				}
			}
			switch (tagString)
			{
				case Any:
				{
					tag = (short)AnyTagInt;
					break;
				}

				case Stop:
				{
					tag = (short)StopTagInt;
					break;
				}

				default:
				{
					tag = (short)tagIndex.AddToIndex(tagString);
					break;
				}
			}
		}

		private const long serialVersionUID = 1L;
	}
}
