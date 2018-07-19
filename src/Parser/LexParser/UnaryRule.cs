using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Unary grammar rules (with ints for parent and child).</summary>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class UnaryRule : IRule, IComparable<Edu.Stanford.Nlp.Parser.Lexparser.UnaryRule>
	{
		public int parent;

		/// <summary>Score should be a log probability</summary>
		public float score;

		public int child;

		/// <summary>The score is set to Float.NaN by default.</summary>
		/// <param name="parent">Parent state</param>
		/// <param name="child">Child state</param>
		public UnaryRule(int parent, int child)
		{
			this.parent = parent;
			this.child = child;
			this.score = float.NaN;
		}

		public UnaryRule(int parent, int child, double score)
		{
			this.parent = parent;
			this.child = child;
			this.score = (float)score;
		}

		/// <summary>
		/// Decode a UnaryRule out of a String representation with help from
		/// an Index.
		/// </summary>
		/// <param name="s">The String representation</param>
		/// <param name="index">The Index used to convert String to int</param>
		public UnaryRule(string s, IIndex<string> index)
		{
			string[] fields = StringUtils.SplitOnCharWithQuoting(s, ' ', '\"', '\\');
			//    System.out.println("fields:\n" + fields[0] + "\n" + fields[2] + "\n" + fields[3]);
			this.parent = index.IndexOf(fields[0]);
			this.child = index.IndexOf(fields[2]);
			this.score = float.ParseFloat(fields[3]);
		}

		public virtual float Score()
		{
			return score;
		}

		public virtual int Parent()
		{
			return parent;
		}

		public override int GetHashCode()
		{
			return (parent << 16) ^ child;
		}

		/// <summary>A UnaryRule is equal to another UnaryRule with the same parent and child.</summary>
		/// <remarks>
		/// A UnaryRule is equal to another UnaryRule with the same parent and child.
		/// The score is not included in the equality computation.
		/// </remarks>
		/// <param name="o">Object to be compared with</param>
		/// <returns>Whether the object is equal to this</returns>
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o is Edu.Stanford.Nlp.Parser.Lexparser.UnaryRule)
			{
				Edu.Stanford.Nlp.Parser.Lexparser.UnaryRule ur = (Edu.Stanford.Nlp.Parser.Lexparser.UnaryRule)o;
				if (parent == ur.parent && child == ur.child)
				{
					return true;
				}
			}
			return false;
		}

		public virtual int CompareTo(Edu.Stanford.Nlp.Parser.Lexparser.UnaryRule ur)
		{
			if (parent < ur.parent)
			{
				return -1;
			}
			if (parent > ur.parent)
			{
				return 1;
			}
			if (child < ur.child)
			{
				return -1;
			}
			if (child > ur.child)
			{
				return 1;
			}
			return 0;
		}

		private static readonly char[] charsToEscape = new char[] { '\"' };

		public override string ToString()
		{
			return parent + " -> " + child + ' ' + score;
		}

		public virtual string ToString(IIndex<string> index)
		{
			return '\"' + StringUtils.EscapeString(index.Get(parent), charsToEscape, '\\') + "\" -> \"" + StringUtils.EscapeString(index.Get(child), charsToEscape, '\\') + "\" " + score;
		}

		[System.NonSerialized]
		private string cached;

		// = null;
		public virtual string ToStringNoScore(IIndex<string> index)
		{
			if (cached == null)
			{
				cached = '\"' + StringUtils.EscapeString(index.Get(parent), charsToEscape, '\\') + "\" -> \"" + StringUtils.EscapeString(index.Get(child), charsToEscape, '\\');
			}
			return cached;
		}

		private const long serialVersionUID = 1L;
	}
}
