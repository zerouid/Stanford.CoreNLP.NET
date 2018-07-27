

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>HeadFinder that always returns the leftmost daughter as head.</summary>
	/// <remarks>
	/// HeadFinder that always returns the leftmost daughter as head.  For
	/// testing purposes.
	/// </remarks>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public class LeftHeadFinder : IHeadFinder
	{
		private const long serialVersionUID = 8453889846239508208L;

		public virtual Tree DetermineHead(Tree t)
		{
			if (t.IsLeaf())
			{
				return null;
			}
			else
			{
				return t.Children()[0];
			}
		}

		public virtual Tree DetermineHead(Tree t, Tree parent)
		{
			return DetermineHead(t);
		}
	}
}
