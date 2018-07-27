using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;



namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>Filters the fragments which end documents in Chinese Treebank</summary>
	[System.Serializable]
	public class FragmentTreeFilter : IPredicate<Tree>
	{
		internal static readonly TregexPattern threeNodePattern = TregexPattern.Compile("FRAG=root <, (PU <: /（/) <2 (VV <: /完/) <- (PU=a <: /）/) <3 =a : =root !> (__ > __)");

		internal static readonly TregexPattern oneNodePattern = TregexPattern.Compile("FRAG=root <: (VV <: /完/) : =root !> (__ > __)");

		internal static readonly TregexPattern automaticInitialPattern = TregexPattern.Compile("automatic=root <: (initial !< __) : =root !> __");

		internal static readonly TregexPattern manuallySegmentedPattern = TregexPattern.Compile("manually=root <: (segmented !< __) : =root !> __");

		internal static readonly TregexPattern onthewayPattern = TregexPattern.Compile("FRAG=root <: (NR <: (ontheway !< __)) : =root !> (__ > __)");

		internal static readonly TregexPattern singlePuncFragPattern = TregexPattern.Compile("__ !> __ <: (PU=punc <: __)");

		internal static readonly TregexPattern singlePuncPattern = TregexPattern.Compile("PU=punc !> __ <: __");

		internal static readonly TregexPattern metaPattern = TregexPattern.Compile("META !> __ <: NN");

		internal static readonly TregexPattern bracketPattern = TregexPattern.Compile("/[<>]/");

		internal static readonly TregexPattern[] patterns = new TregexPattern[] { threeNodePattern, oneNodePattern, automaticInitialPattern, manuallySegmentedPattern, onthewayPattern, singlePuncFragPattern, singlePuncPattern, metaPattern, bracketPattern
			 };

		// The ctb tree reader uses CHTBTokenizer, which filters out SGML
		// and accidentally catches five trees in ctb7.  
		// TODO: One alternative would be to get rid of the specialized tokenizer
		public virtual bool Test(Tree tree)
		{
			foreach (TregexPattern pattern in patterns)
			{
				if (pattern.Matcher(tree).Find())
				{
					return false;
				}
			}
			return true;
		}

		private const long serialVersionUID = 1L;
	}
}
