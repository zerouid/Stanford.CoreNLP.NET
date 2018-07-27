using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Maintains a dependency between head and dependent where they are each an IntTaggedWord.</summary>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class IntDependency
	{
		public const string Left = "left";

		public const string Right = "right";

		public const int AnyDistanceInt = -1;

		public readonly IntTaggedWord head;

		public readonly IntTaggedWord arg;

		public readonly bool leftHeaded;

		public readonly short distance;

		public override int GetHashCode()
		{
			return head.GetHashCode() ^ (arg.GetHashCode() << 8) ^ ((leftHeaded ? 1 : 0) << 15) ^ (distance << 16);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o is Edu.Stanford.Nlp.Parser.Lexparser.IntDependency)
			{
				Edu.Stanford.Nlp.Parser.Lexparser.IntDependency d = (Edu.Stanford.Nlp.Parser.Lexparser.IntDependency)o;
				return (head.Equals(d.head) && arg.Equals(d.arg) && distance == d.distance && leftHeaded == d.leftHeaded);
			}
			else
			{
				return false;
			}
		}

		private static readonly char[] charsToEscape = new char[] { '\"' };

		public override string ToString()
		{
			return "\"" + StringUtils.EscapeString(head.ToString(), charsToEscape, '\\') + "\" -> \"" + StringUtils.EscapeString(arg.ToString(), charsToEscape, '\\') + "\" " + (leftHeaded ? Left : Right) + " " + distance;
		}

		public virtual string ToString(IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			return "\"" + StringUtils.EscapeString(head.ToString(wordIndex, tagIndex), charsToEscape, '\\') + "\" -> \"" + StringUtils.EscapeString(arg.ToString(wordIndex, tagIndex), charsToEscape, '\\') + "\" " + (leftHeaded ? Left : Right) + " " + distance;
		}

		public IntDependency(IntTaggedWord head, IntTaggedWord arg, bool leftHeaded, int distance)
		{
			this.head = head;
			this.arg = arg;
			this.distance = (short)distance;
			this.leftHeaded = leftHeaded;
		}

		public IntDependency(int headWord, int headTag, int argWord, int argTag, bool leftHeaded, int distance)
		{
			this.head = new IntTaggedWord(headWord, headTag);
			this.arg = new IntTaggedWord(argWord, argTag);
			this.distance = (short)distance;
			this.leftHeaded = leftHeaded;
		}

		private const long serialVersionUID = 1L;
	}
}
