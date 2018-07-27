using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>
	/// An AbstractEval which doesn't just evaluate all constituents, but
	/// lets you provide a filter to only pay attention to constituents
	/// formed from certain subtrees.
	/// </summary>
	/// <remarks>
	/// An AbstractEval which doesn't just evaluate all constituents, but
	/// lets you provide a filter to only pay attention to constituents
	/// formed from certain subtrees.  For example, one provided filter
	/// lets you limit the evaluation to subtrees which contain a
	/// particular kind of node.
	/// </remarks>
	/// <author>John Bauer</author>
	public class FilteredEval : AbstractEval
	{
		internal IPredicate<Tree> subtreeFilter;

		private readonly IConstituentFactory cf = new LabeledScoredConstituentFactory();

		public FilteredEval(string str, bool runningAverages, IPredicate<Tree> subtreeFilter)
			: base(str, runningAverages)
		{
			this.subtreeFilter = subtreeFilter;
		}

		protected internal override ICollection<object> MakeObjects(Tree tree)
		{
			ICollection<Constituent> set = Generics.NewHashSet();
			if (tree != null)
			{
				Sharpen.Collections.AddAll(set, tree.Constituents(cf, false, subtreeFilter));
			}
			return set;
		}

		/// <summary>
		/// Returns an eval which is good for counting the attachment of
		/// specific node types.
		/// </summary>
		/// <remarks>
		/// Returns an eval which is good for counting the attachment of
		/// specific node types.  For example, suppose you want to count the
		/// attachment of PP in an English parsing.  You could create one
		/// with PP as the child pattern, and then it would give you p/r/f1
		/// for just nodes which have a PP as a child.
		/// </remarks>
		public static Edu.Stanford.Nlp.Parser.Metrics.FilteredEval ChildFilteredEval(string str, bool runningAverages, ITreebankLanguagePack tlp, string childPattern)
		{
			return new Edu.Stanford.Nlp.Parser.Metrics.FilteredEval(str, runningAverages, new TreeFilters.HasMatchingChild(tlp, childPattern));
		}
	}
}
