using System;
using Edu.Stanford.Nlp.Trees;




namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <author>Roger Levy (rog@nlp.stanford.edu)</author>
	internal class CoindexationGenerator
	{
		/// <summary>
		/// We require at least one character before the - so that negative
		/// numbers do not get treated as indexed nodes.
		/// </summary>
		/// <remarks>
		/// We require at least one character before the - so that negative
		/// numbers do not get treated as indexed nodes.  This seems more
		/// likely than a node having an index on an otherwise blank label.
		/// </remarks>
		private static readonly Pattern coindexationPattern = Pattern.Compile(".+?-([0-9]+)$");

		private int lastIndex;

		public virtual void SetLastIndex(Tree t)
		{
			lastIndex = 0;
			foreach (Tree node in t)
			{
				string value = node.Label().Value();
				if (value != null)
				{
					Matcher m = coindexationPattern.Matcher(value);
					if (m.Find())
					{
						int thisIndex = 0;
						try
						{
							thisIndex = System.Convert.ToInt32(m.Group(1));
						}
						catch (NumberFormatException)
						{
						}
						// Ignore this exception.  This kind of exception can
						// happen if there are nodes that happen to have the
						// indexing character attached, even despite the attempt
						// to ignore those nodes in the pattern above.
						lastIndex = Math.Max(thisIndex, lastIndex);
					}
				}
			}
		}

		public virtual int GenerateIndex()
		{
			lastIndex = lastIndex + 1;
			return lastIndex;
		}
	}
}
