using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>Adjoin in a tree (like in TAG), but retain the target of adjunction as the foot of the auxiliary tree.</summary>
	/// <author>Roger Levy (rog@nlp.stanford.edu)</author>
	public class AdjoinToFootNode : AdjoinNode
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.AdjoinToFootNode));

		public AdjoinToFootNode(AuxiliaryTree t, TsurgeonPattern p)
			: base("adjoinF", t, p)
		{
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new AdjoinToFootNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(AdjoinToFootNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				// find match and get its parent
				Tree targetNode = this.childMatcher[0].Evaluate(tree, tregex);
				Tree parent = targetNode.Parent(tree);
				// substitute original node for foot of auxiliary tree.  Foot node is ignored
				AuxiliaryTree ft = this._enclosing.AdjunctionTree().Copy(this, tree.TreeFactory(), tree.Label().LabelFactory());
				// log.info("ft=" + ft + "; ft.foot=" + ft.foot + "; ft.tree=" + ft.tree);
				Tree parentOfFoot = ft.foot.Parent(ft.tree);
				if (parentOfFoot == null)
				{
					AdjoinToFootNode.log.Info("Warning: adjoin to foot for depth-1 auxiliary tree has no effect.");
					return tree;
				}
				int i = parentOfFoot.ObjectIndexOf(ft.foot);
				if (parent == null)
				{
					parentOfFoot.SetChild(i, targetNode);
					return ft.tree;
				}
				else
				{
					int j = parent.ObjectIndexOf(targetNode);
					parent.SetChild(j, ft.tree);
					parentOfFoot.SetChild(i, targetNode);
					return tree;
				}
			}

			private readonly AdjoinToFootNode _enclosing;
		}
	}
}
