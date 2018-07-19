using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Transforms an English structure parse tree in order to get the dependencies right:  <br />
	/// -- put a ROOT node  <br />
	/// -- remove NONE nodes  <br />
	/// -- retain only NP-TMP, NP-ADV, UCP-TMP tags  <br />
	/// The UCP- tags will later be turned into NP- anyway <br />
	/// (Note [cdm]: A lot of this overlaps other existing functionality in trees.
	/// </summary>
	/// <remarks>
	/// Transforms an English structure parse tree in order to get the dependencies right:  <br />
	/// -- put a ROOT node  <br />
	/// -- remove NONE nodes  <br />
	/// -- retain only NP-TMP, NP-ADV, UCP-TMP tags  <br />
	/// The UCP- tags will later be turned into NP- anyway <br />
	/// (Note [cdm]: A lot of this overlaps other existing functionality in trees.
	/// Could aim to unify it.)
	/// </remarks>
	/// <author>mcdm</author>
	public class DependencyTreeTransformer : ITreeTransformer
	{
		private static readonly Pattern TmpPattern = Pattern.Compile("(NP|UCP).*-TMP.*");

		private static readonly Pattern AdvPattern = Pattern.Compile("(NP|UCP).*-ADV.*");

		protected internal readonly ITreebankLanguagePack tlp;

		public DependencyTreeTransformer()
		{
			tlp = new PennTreebankLanguagePack();
		}

		public virtual Tree TransformTree(Tree t)
		{
			//deal with empty root
			t.SetValue(CleanUpRoot(t.Value()));
			//strips tags
			StripTag(t);
			// strip empty nodes
			return StripEmptyNode(t);
		}

		protected internal static string CleanUpRoot(string label)
		{
			if (label == null || label.Equals("TOP"))
			{
				return "ROOT";
			}
			else
			{
				// String constants are always interned
				return label;
			}
		}

		// only leaves NP-TMP and NP-ADV
		protected internal virtual string CleanUpLabel(string label)
		{
			if (label == null)
			{
				return string.Empty;
			}
			// This shouldn't really happen, but can happen if there are unlabeled nodes further down a tree, as apparently happens in at least the 20100730 era American National Corpus
			bool nptemp = TmpPattern.Matcher(label).Matches();
			bool npadv = AdvPattern.Matcher(label).Matches();
			label = tlp.BasicCategory(label);
			if (nptemp)
			{
				label = label + "-TMP";
			}
			else
			{
				if (npadv)
				{
					label = label + "-ADV";
				}
			}
			return label;
		}

		protected internal virtual void StripTag(Tree t)
		{
			if (!t.IsLeaf())
			{
				string label = CleanUpLabel(t.Value());
				t.SetValue(label);
				foreach (Tree child in t.GetChildrenAsList())
				{
					StripTag(child);
				}
			}
		}

		private static readonly TregexPattern matchPattern = TregexPattern.SafeCompile("-NONE-=none", true);

		private static readonly TsurgeonPattern operation = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("prune none");

		protected internal static Tree StripEmptyNode(Tree t)
		{
			return Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(matchPattern, operation, t);
		}
	}
}
