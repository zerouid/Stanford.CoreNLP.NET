using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>
	/// Given a named edge, governor, and dependent, removes that edge
	/// from the SemanticGraph.
	/// </summary>
	/// <remarks>
	/// Given a named edge, governor, and dependent, removes that edge
	/// from the SemanticGraph.
	/// NOTE: you should manually reassign roots for dangling subtrees,
	/// or delete them outright.  This does not perform any new root
	/// assignments.
	/// TODO: implement logging functionality
	/// </remarks>
	/// <author>yeh1</author>
	public class RemoveNamedEdge : SsurgeonEdit
	{
		public const string Label = "removeNamedEdge";

		protected internal string edgeName;

		protected internal string govName;

		protected internal string depName;

		public RemoveNamedEdge(string edgeName, string govName, string depName)
		{
			// Name of the matched edge in the SemgrexPattern
			// Name of governor of this reln, in match
			// Name of the dependent in this reln, in match
			this.edgeName = edgeName;
			this.govName = govName;
			this.depName = depName;
		}

		public override string ToEditString()
		{
			StringWriter buf = new StringWriter();
			buf.Write(Label);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.EdgeNameArg);
			buf.Write(" ");
			buf.Write(edgeName);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.GovNodenameArg);
			buf.Write(" ");
			buf.Write(govName);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.DepNodenameArg);
			buf.Write(" ");
			buf.Write(depName);
			return buf.ToString();
		}

		public override void Evaluate(SemanticGraph sg, SemgrexMatcher sm)
		{
			string relation = sm.GetRelnString(edgeName);
			IndexedWord govNode = GetNamedNode(govName, sm);
			IndexedWord depNode = GetNamedNode(depName, sm);
			SemanticGraphEdge edge = sg.GetEdge(govNode, depNode, GrammaticalRelation.ValueOf(relation));
			if (edge != null)
			{
				sg.RemoveEdge(edge);
			}
		}

		public virtual string GetDepName()
		{
			return depName;
		}

		public virtual string GetEdgeName()
		{
			return edgeName;
		}

		public virtual string GetGovName()
		{
			return govName;
		}
	}
}
