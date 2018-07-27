


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>TreeLengthComparator</code> orders trees by their yield sentence
	/// lengths.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <version>2003/03/24</version>
	public class TreeLengthComparator : IComparator<Tree>
	{
		/// <summary>Create a new <code>TreeLengthComparator</code>.</summary>
		public TreeLengthComparator()
		{
		}

		/// <summary>Compare the two objects.</summary>
		public virtual int Compare(Tree t1, Tree t2)
		{
			if (t1 == t2)
			{
				return 0;
			}
			int len1 = t1.Yield().Count;
			int len2 = t2.Yield().Count;
			if (len1 > len2)
			{
				return 1;
			}
			else
			{
				if (len1 < len2)
				{
					return -1;
				}
				else
				{
					return 0;
				}
			}
		}
	}
}
