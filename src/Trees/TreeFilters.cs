using Edu.Stanford.Nlp.Ling;




namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A location for general implementations of Filter&lt;Tree&gt;.</summary>
	/// <remarks>
	/// A location for general implementations of Filter&lt;Tree&gt;.  For
	/// example, we provide a tree which filters trees so they are only
	/// accepted if it has a child with a label that matches a particular
	/// regex.
	/// </remarks>
	/// <author>John Bauer</author>
	public class TreeFilters
	{
		[System.Serializable]
		public class HasMatchingChild : IPredicate<Tree>
		{
			internal ITreebankLanguagePack tlp;

			internal Pattern pattern;

			public HasMatchingChild(ITreebankLanguagePack tlp, string regex)
			{
				this.pattern = Pattern.Compile(regex);
				this.tlp = tlp;
			}

			public virtual bool Test(Tree tree)
			{
				if (tree == null)
				{
					return false;
				}
				foreach (Tree child in tree.Children())
				{
					ILabel label = child.Label();
					string value = (label == null) ? null : label.Value();
					if (value == null)
					{
						continue;
					}
					if (pattern.Matcher(value).Matches())
					{
						return true;
					}
					string basic = tlp.BasicCategory(value);
					if (pattern.Matcher(basic).Matches())
					{
						return true;
					}
				}
				return false;
			}

			private const long serialVersionUID = 1L;
		}

		private TreeFilters()
		{
		}
	}
}
