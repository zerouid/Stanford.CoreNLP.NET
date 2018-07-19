using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Common
{
	/// <summary>
	/// Simple variant of the ModCollinsHeadFinder avoids supplying punctuation tags
	/// as heads whenever possible.
	/// </summary>
	/// <author>David McClosky (mcclosky@stanford.edu)</author>
	[System.Serializable]
	public class NoPunctuationHeadFinder : ModCollinsHeadFinder
	{
		private const long serialVersionUID = 1201891305937180385L;

		/// <summary>
		/// Returns whether a part of speech tag is the tag for a punctuation mark (by
		/// checking whether the first character is a letter.
		/// </summary>
		/// <param name="label">part of speech tag</param>
		/// <returns>whether the tag is (typically) assigned to punctuation</returns>
		private bool IsPunctuationLabel(string label)
		{
			return !char.IsLetter(label[0]) && !(label.Equals("$") || label.Equals("%"));
		}

		protected internal override int PostOperationFix(int headIdx, Tree[] daughterTrees)
		{
			int index = base.PostOperationFix(headIdx, daughterTrees);
			// if the current index is a punctuation mark, we search left until we
			// find a non-punctuation mark tag or hit the left end of the sentence
			while (index > 0)
			{
				string label = daughterTrees[index].Label().Value();
				if (IsPunctuationLabel(label))
				{
					index--;
				}
				else
				{
					break;
				}
			}
			return index;
		}

		public static void Main(string[] args)
		{
			// simple testing code
			Treebank treebank = new DiskTreebank();
			CategoryWordTag.suppressTerminalDetails = true;
			treebank.LoadPath(args[0]);
			IHeadFinder chf = new NoPunctuationHeadFinder();
			treebank.Apply(null);
		}
	}
}
