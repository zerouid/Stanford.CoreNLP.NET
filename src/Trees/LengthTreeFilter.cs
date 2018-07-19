using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Only accept trees that are short enough (less than or equal to length).</summary>
	/// <remarks>
	/// Only accept trees that are short enough (less than or equal to length).
	/// It's not always about length, but in this case it is.
	/// </remarks>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class LengthTreeFilter : IPredicate<Tree>
	{
		private int length;

		public LengthTreeFilter(int length)
		{
			this.length = length;
		}

		public virtual bool Test(Tree tree)
		{
			return tree.Yield().Count <= length;
		}

		private const long serialVersionUID = 1;
	}
}
