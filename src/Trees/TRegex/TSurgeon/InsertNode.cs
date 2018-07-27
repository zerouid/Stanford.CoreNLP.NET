using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <author>Roger Levy (rog@stanford.edu)</author>
	internal class InsertNode : TsurgeonPattern
	{
		internal TreeLocation location;

		/// <summary>
		/// Does the item being inserted need to be deep-copied before
		/// insertion?
		/// </summary>
		internal bool needsCopy = true;

		public InsertNode(TsurgeonPattern child, TreeLocation l)
			: base("insert", new TsurgeonPattern[] { child })
		{
			this.location = l;
		}

		protected internal override void SetRoot(TsurgeonPatternRoot root)
		{
			base.SetRoot(root);
			location.SetRoot(root);
		}

		public InsertNode(AuxiliaryTree t, TreeLocation l)
			: this(new HoldTreeNode(t), l)
		{
			// Copy occurs in HoldTreeNode's `evaluate` method
			needsCopy = false;
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new InsertNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			internal TreeLocation.LocationMatcher locationMatcher;

			public Matcher(InsertNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
				this.locationMatcher = this._enclosing.location.Matcher(newNodeNames, coindexer);
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				Tree nodeToInsert = this.childMatcher[0].Evaluate(tree, tregex);
				Pair<Tree, int> position = this.locationMatcher.Evaluate(tree, tregex);
				position.First().InsertDtr(this._enclosing.needsCopy ? nodeToInsert.DeepCopy() : nodeToInsert, position.Second());
				return tree;
			}

			private readonly InsertNode _enclosing;
		}

		public override string ToString()
		{
			return label + '(' + children[0] + ',' + location + ')';
		}
	}
}
