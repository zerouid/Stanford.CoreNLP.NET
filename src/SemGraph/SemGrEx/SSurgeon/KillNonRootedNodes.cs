using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>This is used to clean up a graph, removing nodes that cannot possibly reach a root.</summary>
	/// <remarks>
	/// This is used to clean up a graph, removing nodes that cannot possibly reach a root.
	/// The intended usage is for the user to be able to perform cuts, select the nodes to
	/// keep by manually choosing the root, and then dropping the nodes that cannot
	/// reach those new nodes.
	/// </remarks>
	/// <author>Eric Yeh</author>
	public class KillNonRootedNodes : SsurgeonEdit
	{
		public const string Label = "killNonRooted";

		public override void Evaluate(SemanticGraph sg, SemgrexMatcher sm)
		{
			IList<IndexedWord> nodes = new List<IndexedWord>(sg.VertexSet());
			foreach (IndexedWord node in nodes)
			{
				IList<IndexedWord> rootPath = sg.GetPathToRoot(node);
				if (rootPath == null)
				{
					sg.RemoveVertex(node);
				}
			}
		}

		public override string ToEditString()
		{
			StringWriter buf = new StringWriter();
			buf.Append(Label);
			buf.Append("\t");
			buf.Append(Label);
			return buf.ToString();
		}
	}
}
