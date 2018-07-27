

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Tree normalizer for cleaning up labels and preserving the whole node label,
	/// the grammatical function and category information from the label, or only
	/// the category information.
	/// </summary>
	/// <remarks>
	/// Tree normalizer for cleaning up labels and preserving the whole node label,
	/// the grammatical function and category information from the label, or only
	/// the category information.  Only normalization occurs on nonterminals.
	/// </remarks>
	/// <author>Anna Rafferty</author>
	[System.Serializable]
	public class GrammaticalFunctionTreeNormalizer : TreeNormalizer
	{
		private const long serialVersionUID = -2270472762938163327L;

		/// <summary>
		/// How to clean up node labels: 0 = do nothing, 1 = keep category and
		/// function, 2 = just category.
		/// </summary>
		private readonly int nodeCleanup;

		private readonly string root;

		protected internal readonly ITreebankLanguagePack tlp;

		public GrammaticalFunctionTreeNormalizer(ITreebankLanguagePack tlp, int nodeCleanup)
		{
			this.tlp = tlp;
			this.nodeCleanup = nodeCleanup;
			root = tlp.StartSymbol();
		}

		/// <summary>Normalizes a nonterminal contents.</summary>
		/// <remarks>
		/// Normalizes a nonterminal contents.
		/// This implementation strips functional tags, etc. and interns the
		/// nonterminal.
		/// </remarks>
		public override string NormalizeNonterminal(string category)
		{
			return string.Intern(CleanUpLabel(category));
		}

		/// <summary>
		/// Remove things like hyphened functional tags and equals from the
		/// end of a node label.
		/// </summary>
		protected internal virtual string CleanUpLabel(string label)
		{
			if (label == null)
			{
				return root;
			}
			else
			{
				if (nodeCleanup == 1)
				{
					return tlp.CategoryAndFunction(label);
				}
				else
				{
					if (nodeCleanup == 2)
					{
						return tlp.BasicCategory(label);
					}
					else
					{
						return label;
					}
				}
			}
		}
	}
}
