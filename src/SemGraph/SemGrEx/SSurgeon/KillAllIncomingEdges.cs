using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>This action removes all incoming edges for the given node.</summary>
	/// <author>lumberjack</author>
	public class KillAllIncomingEdges : SsurgeonEdit
	{
		public const string Label = "killAllIncomingEdges";

		protected internal string nodeName;

		public KillAllIncomingEdges(string nodeName)
		{
			// name of this node
			this.nodeName = nodeName;
		}

		public override void Evaluate(SemanticGraph sg, SemgrexMatcher sm)
		{
			IndexedWord tgtNode = GetNamedNode(nodeName, sm);
			foreach (SemanticGraphEdge edge in sg.IncomingEdgeIterable(tgtNode))
			{
				sg.RemoveEdge(edge);
			}
		}

		public override string ToEditString()
		{
			StringWriter buf = new StringWriter();
			buf.Write(Label);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.NodenameArg);
			buf.Write("\t");
			buf.Write(nodeName);
			return buf.ToString();
		}
	}
}
