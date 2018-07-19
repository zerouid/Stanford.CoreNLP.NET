using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>Does a delete (NOT prune!) + insert operation</summary>
	/// <author>Roger Levy (rog@stanford.edu)</author>
	internal class MoveNode : TsurgeonPattern
	{
		internal TreeLocation location;

		public MoveNode(TsurgeonPattern child, TreeLocation l)
			: base("move", new TsurgeonPattern[] { child })
		{
			this.location = l;
		}

		protected internal override void SetRoot(TsurgeonPatternRoot root)
		{
			base.SetRoot(root);
			location.SetRoot(root);
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new MoveNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			internal TreeLocation.LocationMatcher locationMatcher;

			public Matcher(MoveNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
				this.locationMatcher = this._enclosing.location.Matcher(newNodeNames, coindexer);
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				Tree nodeToMove = this.childMatcher[0].Evaluate(tree, tregex);
				Tree oldParent = nodeToMove.Parent(tree);
				oldParent.RemoveChild(Edu.Stanford.Nlp.Trees.Trees.ObjectEqualityIndexOf(oldParent, nodeToMove));
				Pair<Tree, int> position = this.locationMatcher.Evaluate(tree, tregex);
				position.First().InsertDtr(nodeToMove, position.Second());
				return tree;
			}

			private readonly MoveNode _enclosing;
		}

		public override string ToString()
		{
			return label + "(" + children[0] + " " + location + ")";
		}
	}
}
