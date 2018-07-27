using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>Collapses a subtree into a single node.</summary>
	/// <remarks>
	/// Collapses a subtree into a single node.
	/// The new node has the POS tag and index of the root node
	/// and the value and the lemma of the concatenation of the subnodes.
	/// One intended use is to collapse multi-word expressions into one node
	/// to facilitate relation extraction and related tasks.
	/// </remarks>
	/// <author>Sebastian Schuster</author>
	public class CollapseSubtree : SsurgeonEdit
	{
		public const string Label = "collapseSubtree";

		protected internal string rootName;

		public CollapseSubtree(string rootNodeName)
		{
			// Name of the root node in match
			this.rootName = rootNodeName;
		}

		public override void Evaluate(SemanticGraph sg, SemgrexMatcher sm)
		{
			IndexedWord rootNode = this.GetNamedNode(rootName, sm);
			ICollection<IndexedWord> subgraphNodeSet = sg.GetSubgraphVertices(rootNode);
			if (!sg.IsDag(rootNode))
			{
				/* Check if there is a cycle going back to the root. */
				foreach (IndexedWord child in sg.GetChildren(rootNode))
				{
					ICollection<IndexedWord> reachableSet = sg.GetSubgraphVertices(child);
					if (reachableSet.Contains(rootNode))
					{
						throw new ArgumentException("Subtree cannot contain cycle leading back to root node!");
					}
				}
			}
			IList<IndexedWord> sortedSubgraphNodes = Generics.NewArrayList(subgraphNodeSet);
			sortedSubgraphNodes.Sort();
			IndexedWord newNode = new IndexedWord(rootNode.DocID(), rootNode.SentIndex(), rootNode.Index());
			/* Copy all attributes from rootNode. */
			foreach (Type key in newNode.BackingLabel().KeySet())
			{
				newNode.Set(key, rootNode.Get(key));
			}
			newNode.SetValue(StringUtils.Join(sortedSubgraphNodes.Stream().Map(null), " "));
			newNode.SetWord(StringUtils.Join(sortedSubgraphNodes.Stream().Map(null), " "));
			newNode.SetLemma(StringUtils.Join(sortedSubgraphNodes.Stream().Map(null), " "));
			if (sg.GetRoots().Contains(rootNode))
			{
				sg.GetRoots().Remove(rootNode);
				sg.AddRoot(rootNode);
			}
			foreach (SemanticGraphEdge edge in sg.IncomingEdgeIterable(rootNode))
			{
				sg.AddEdge(edge.GetGovernor(), newNode, edge.GetRelation(), edge.GetWeight(), edge.IsExtra());
			}
			foreach (IndexedWord node in sortedSubgraphNodes)
			{
				sg.RemoveVertex(node);
			}
		}

		public override string ToEditString()
		{
			StringWriter buf = new StringWriter();
			buf.Write(Label);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.NodenameArg);
			buf.Write(" ");
			buf.Write(rootName);
			return buf.ToString();
		}
	}
}
