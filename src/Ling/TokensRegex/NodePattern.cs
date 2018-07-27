using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Matches a Node (i.e a Token).</summary>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public abstract class NodePattern<T>
	{
		public static readonly NodePattern AnyNode = new NodePattern.AnyNodePattern();

		public const int CaseInsensitive = unchecked((int)(0x02));

		public const int Normalize = unchecked((int)(0x04));

		public const int UnicodeCase = unchecked((int)(0x40));

		// Flags for string annotations
		/// <summary>Returns true if the input node matches this pattern</summary>
		/// <param name="node">- node to match</param>
		/// <returns>true if the node matches the pattern, false otherwise</returns>
		public abstract bool Match(T node);

		/// <summary>Returns result associated with the match</summary>
		/// <param name="node">node to match</param>
		/// <returns>
		/// null if not matched, TRUE if there is a match but no other result associated with the match.
		/// Any other value is treated as the result value of the match.
		/// </returns>
		public virtual object MatchWithResult(T node)
		{
			if (Match(node))
			{
				return true;
			}
			else
			{
				return null;
			}
		}

		/// <summary>Matches any node</summary>
		/// <?/>
		[System.Serializable]
		public class AnyNodePattern<T> : NodePattern<T>
		{
			protected internal AnyNodePattern()
			{
			}

			public override bool Match(T node)
			{
				return true;
			}

			public override string ToString()
			{
				return "*";
			}
		}

		/// <summary>Matches a constant value of type T using equals()</summary>
		/// <?/>
		[System.Serializable]
		public class EqualsNodePattern<T> : NodePattern<T>
		{
			internal T t;

			public EqualsNodePattern(T t)
			{
				this.t = t;
			}

			public override bool Match(T node)
			{
				return t.Equals(node);
			}

			public override string ToString()
			{
				return "[" + t + "]";
			}
		}

		/// <summary>Given a node pattern p, a node x matches if p does not match x</summary>
		/// <?/>
		[System.Serializable]
		public class NegateNodePattern<T> : NodePattern<T>
		{
			internal NodePattern<T> p;

			public NegateNodePattern(NodePattern<T> p)
			{
				this.p = p;
			}

			public override bool Match(T node)
			{
				return !p.Match(node);
			}

			public override string ToString()
			{
				return "!" + p;
			}
		}

		/// <summary>Given a list of patterns p1,...,pn, matches if all patterns p1,...,pn matches</summary>
		/// <?/>
		[System.Serializable]
		public class ConjNodePattern<T> : NodePattern<T>
		{
			internal IList<NodePattern<T>> nodePatterns;

			public ConjNodePattern(IList<NodePattern<T>> nodePatterns)
			{
				this.nodePatterns = nodePatterns;
			}

			public override bool Match(T node)
			{
				bool matched = true;
				foreach (NodePattern<T> p in nodePatterns)
				{
					if (!p.Match(node))
					{
						matched = false;
						break;
					}
				}
				return matched;
			}

			public override string ToString()
			{
				return StringUtils.Join(nodePatterns, " & ");
			}
		}

		/// <summary>Given a list of patterns p1,...,pn, matches if one of the patterns p1,...,pn matches</summary>
		/// <?/>
		[System.Serializable]
		public class DisjNodePattern<T> : NodePattern<T>
		{
			internal IList<NodePattern<T>> nodePatterns;

			public DisjNodePattern(IList<NodePattern<T>> nodePatterns)
			{
				this.nodePatterns = nodePatterns;
			}

			public override bool Match(T node)
			{
				bool matched = false;
				foreach (NodePattern<T> p in nodePatterns)
				{
					if (p.Match(node))
					{
						matched = true;
						break;
					}
				}
				return matched;
			}

			public override string ToString()
			{
				return StringUtils.Join(nodePatterns, " | ");
			}
		}
	}
}
