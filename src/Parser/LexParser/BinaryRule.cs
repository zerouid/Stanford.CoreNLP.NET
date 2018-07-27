using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Binary rules (ints for parent, left and right children)</summary>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class BinaryRule : IRule, IComparable<Edu.Stanford.Nlp.Parser.Lexparser.BinaryRule>
	{
		public int parent;

		/// <summary>Score should be a log probability</summary>
		public float score;

		public int leftChild;

		public int rightChild;

		/// <summary>Create a new BinaryRule with the parent and children coded as ints.</summary>
		/// <remarks>
		/// Create a new BinaryRule with the parent and children coded as ints.
		/// Score defaults to Float.NaN.
		/// </remarks>
		/// <param name="parent">The parent int</param>
		/// <param name="leftChild">The left child int</param>
		/// <param name="rightChild">The right child int</param>
		public BinaryRule(int parent, int leftChild, int rightChild)
		{
			this.parent = parent;
			this.leftChild = leftChild;
			this.rightChild = rightChild;
			this.score = float.NaN;
		}

		public BinaryRule(int parent, int leftChild, int rightChild, double score)
		{
			this.parent = parent;
			this.leftChild = leftChild;
			this.rightChild = rightChild;
			this.score = (float)score;
		}

		/// <summary>Creates a BinaryRule from String s, assuming it was created using toString().</summary>
		/// <param name="s">
		/// A String in which the binary rule is represented as parent,
		/// left-child, right-child, score, with the items quoted as needed
		/// </param>
		/// <param name="index">Index used to convert String names to ints</param>
		public BinaryRule(string s, IIndex<string> index)
		{
			string[] fields = StringUtils.SplitOnCharWithQuoting(s, ' ', '\"', '\\');
			//    System.out.println("fields:\n" + fields[0] + "\n" + fields[2] + "\n" + fields[3] + "\n" + fields[4]);
			this.parent = index.AddToIndex(fields[0]);
			this.leftChild = index.AddToIndex(fields[2]);
			this.rightChild = index.AddToIndex(fields[3]);
			this.score = float.ParseFloat(fields[4]);
		}

		public virtual float Score()
		{
			return score;
		}

		public virtual int Parent()
		{
			return parent;
		}

		private int hashCode = -1;

		public override int GetHashCode()
		{
			if (hashCode < 0)
			{
				hashCode = (parent << 16) ^ (leftChild << 8) ^ rightChild;
			}
			return hashCode;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o is Edu.Stanford.Nlp.Parser.Lexparser.BinaryRule)
			{
				Edu.Stanford.Nlp.Parser.Lexparser.BinaryRule br = (Edu.Stanford.Nlp.Parser.Lexparser.BinaryRule)o;
				if (parent == br.parent && leftChild == br.leftChild && rightChild == br.rightChild)
				{
					return true;
				}
			}
			return false;
		}

		private static readonly char[] charsToEscape = new char[] { '\"' };

		public override string ToString()
		{
			return parent + " -> " + leftChild + ' ' + rightChild + ' ' + score;
		}

		public virtual string ToString(IIndex<string> index)
		{
			return '\"' + StringUtils.EscapeString(index.Get(parent), charsToEscape, '\\') + "\" -> \"" + StringUtils.EscapeString(index.Get(leftChild), charsToEscape, '\\') + "\" \"" + StringUtils.EscapeString(index.Get(rightChild), charsToEscape, '\\'
				) + "\" " + score;
		}

		[System.NonSerialized]
		private string cached;

		// = null;
		public virtual string ToStringNoScore(IIndex<string> index)
		{
			if (cached == null)
			{
				cached = '\"' + StringUtils.EscapeString(index.Get(parent), charsToEscape, '\\') + "\" -> \"" + StringUtils.EscapeString(index.Get(leftChild), charsToEscape, '\\') + "\" \"" + StringUtils.EscapeString(index.Get(rightChild), charsToEscape, '\\'
					);
			}
			return cached;
		}

		public virtual int CompareTo(Edu.Stanford.Nlp.Parser.Lexparser.BinaryRule br)
		{
			if (parent < br.parent)
			{
				return -1;
			}
			if (parent > br.parent)
			{
				return 1;
			}
			if (leftChild < br.leftChild)
			{
				return -1;
			}
			if (leftChild > br.leftChild)
			{
				return 1;
			}
			if (rightChild < br.rightChild)
			{
				return -1;
			}
			if (rightChild > br.rightChild)
			{
				return 1;
			}
			return 0;
		}

		private const long serialVersionUID = 1L;
	}
}
