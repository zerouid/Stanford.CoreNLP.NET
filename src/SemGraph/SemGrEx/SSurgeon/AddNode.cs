using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	public class AddNode : SsurgeonEdit
	{
		public const string Label = "addNode";

		internal string nodeString = null;

		internal string nodeName = null;

		public AddNode(string nodeString, string nodeName)
		{
			this.nodeString = nodeString;
			this.nodeName = nodeName;
		}

		public static Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.AddNode CreateAddNode(string nodeString, string nodeName)
		{
			return new Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.AddNode(nodeString, nodeName);
		}

		public static Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.AddNode CreateAddNode(IndexedWord node, string nodeName)
		{
			string nodeString = AddDep.CheapWordToString(node);
			return new Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.AddNode(nodeString, nodeName);
		}

		public override void Evaluate(SemanticGraph sg, SemgrexMatcher sm)
		{
			IndexedWord newNode = AddDep.FromCheapString(nodeString);
			sg.AddVertex(newNode);
			AddNamedNode(newNode, nodeName);
		}

		public override string ToEditString()
		{
			StringWriter buf = new StringWriter();
			buf.Write(Label);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.NodeProtoArg);
			buf.Write(" ");
			buf.Write("\"");
			buf.Write(nodeString);
			buf.Write("\"\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.NameArg);
			buf.Write("\t");
			buf.Write(nodeName);
			return buf.ToString();
		}
	}
}
