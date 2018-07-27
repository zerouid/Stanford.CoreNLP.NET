using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;


namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <author>Roger Levy (rog@nlp.stanford.edu)</author>
	internal class CoindexNodes : TsurgeonPattern
	{
		private const string coindexationIntroductionString = "-";

		public CoindexNodes(TsurgeonPattern[] children)
			: base("coindex", children)
		{
		}

		protected internal override void SetRoot(TsurgeonPatternRoot root)
		{
			base.SetRoot(root);
			root.SetCoindexes();
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new CoindexNodes.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(CoindexNodes _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				int newIndex = this.coindexer.GenerateIndex();
				foreach (TsurgeonMatcher child in this.childMatcher)
				{
					Tree node = child.Evaluate(tree, tregex);
					node.Label().SetValue(node.Label().Value() + CoindexNodes.coindexationIntroductionString + newIndex);
				}
				return tree;
			}

			private readonly CoindexNodes _enclosing;
		}
	}
}
