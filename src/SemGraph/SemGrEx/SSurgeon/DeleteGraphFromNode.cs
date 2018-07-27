using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>This destroys the subgraph starting from the given node.</summary>
	/// <remarks>
	/// This destroys the subgraph starting from the given node.  Use this when
	/// the SemanticGraph has been cut and separated into two separate graphs,
	/// and you wish to destroy one of them.
	/// </remarks>
	/// <author>yeh1</author>
	public class DeleteGraphFromNode : SsurgeonEdit
	{
		public const string Label = "delete";

		internal string destroyNodeName;

		public DeleteGraphFromNode(string destroyNodeName)
		{
			this.destroyNodeName = destroyNodeName;
		}

		public static Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.DeleteGraphFromNode FromArgs(string args)
		{
			return new Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.DeleteGraphFromNode(args.Trim());
		}

		public override string ToEditString()
		{
			StringWriter buf = new StringWriter();
			buf.Write(Label);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.NodenameArg);
			buf.Write(" ");
			buf.Write(destroyNodeName);
			return buf.ToString();
		}

		protected internal static void Crawl(IndexedWord vertex, SemanticGraph sg, ICollection<IndexedWord> seenVerts)
		{
			seenVerts.Add(vertex);
			foreach (SemanticGraphEdge edge in sg.IncomingEdgeIterable(vertex))
			{
				IndexedWord gov = edge.GetGovernor();
				if (!seenVerts.Contains(gov))
				{
					Crawl(gov, sg, seenVerts);
				}
			}
			foreach (SemanticGraphEdge edge_1 in sg.OutgoingEdgeIterable(vertex))
			{
				IndexedWord dep = edge_1.GetDependent();
				if (!seenVerts.Contains(dep))
				{
					Crawl(dep, sg, seenVerts);
				}
			}
		}

		protected internal static ICollection<IndexedWord> Crawl(IndexedWord vertex, SemanticGraph sg)
		{
			ICollection<IndexedWord> seen = Generics.NewHashSet();
			Crawl(vertex, sg, seen);
			return seen;
		}

		public override void Evaluate(SemanticGraph sg, SemgrexMatcher sm)
		{
			IndexedWord seedNode = GetNamedNode(destroyNodeName, sm);
			// TODO: do not execute if seedNode if not in graph (or just error?)
			if (sg.ContainsVertex(seedNode))
			{
				ICollection<IndexedWord> nodesToDestroy = Crawl(seedNode, sg);
				foreach (IndexedWord node in nodesToDestroy)
				{
					sg.RemoveVertex(node);
				}
				// After destroy nodes, need to reset the roots, since it's possible a root node
				// was destroyed.
				sg.ResetRoots();
			}
		}
	}
}
