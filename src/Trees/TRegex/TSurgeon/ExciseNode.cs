using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>Excises all nodes from the top to the bottom, and puts all the children of bottom node in where the top was.</summary>
	/// <author>Roger Levy (rog@stanford.edu)</author>
	public class ExciseNode : TsurgeonPattern
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.ExciseNode));

		/// <summary>Top should evaluate to a node that dominates bottom, but this is not checked!</summary>
		public ExciseNode(TsurgeonPattern top, TsurgeonPattern bottom)
			: base("excise", new TsurgeonPattern[] { top, bottom })
		{
		}

		/// <summary>Excises only the directed node.</summary>
		public ExciseNode(TsurgeonPattern node)
			: base("excise", new TsurgeonPattern[] { node, node })
		{
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new ExciseNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(ExciseNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				Tree topNode = this.childMatcher[0].Evaluate(tree, tregex);
				Tree bottomNode = this.childMatcher[1].Evaluate(tree, tregex);
				if (Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.verbose)
				{
					ExciseNode.log.Info("Excising...original tree:");
					tree.PennPrint(System.Console.Error);
					ExciseNode.log.Info("top: " + topNode + "\nbottom:" + bottomNode);
				}
				if (topNode == tree)
				{
					if (bottomNode.Children().Length == 1)
					{
						return bottomNode.Children()[0];
					}
					else
					{
						return null;
					}
				}
				Tree parent = topNode.Parent(tree);
				if (Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.verbose)
				{
					ExciseNode.log.Info("Parent: " + parent);
				}
				int i = Edu.Stanford.Nlp.Trees.Trees.ObjectEqualityIndexOf(parent, topNode);
				parent.RemoveChild(i);
				foreach (Tree child in bottomNode.Children())
				{
					parent.AddChild(i, child);
					i++;
				}
				if (Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.verbose)
				{
					tree.PennPrint(System.Console.Error);
				}
				return tree;
			}

			private readonly ExciseNode _enclosing;
		}
	}
}
