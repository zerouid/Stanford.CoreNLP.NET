using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>
	/// This adds a given GrammaticalRelation between
	/// two named nodes in the graph.
	/// </summary>
	/// <remarks>
	/// This adds a given GrammaticalRelation between
	/// two named nodes in the graph.
	/// If one already exists, does not add.
	/// TODO: add position (a la Tregex)
	/// TODO: determine consistent and intuitive arguments
	/// TODO: figure out a way of ordering edges, so constituents are moved into proper
	/// place s.t. a vertexList() will return the correct ordering.
	/// </remarks>
	/// <author>yeh1</author>
	public class AddEdge : SsurgeonEdit
	{
		public const string Label = "addEdge";

		protected internal string govName;

		protected internal string depName;

		protected internal GrammaticalRelation relation;

		protected internal double weight;

		public AddEdge(string govName, string depName, GrammaticalRelation relation)
		{
			// Name of governor of this reln, in match
			// Name of the dependent in this reln, in match
			// Type of relation to add between these edges 
			this.govName = govName;
			this.depName = depName;
			this.relation = relation;
			this.weight = 0;
		}

		public AddEdge(string govName, string depName, GrammaticalRelation relation, double weight)
			: this(govName, depName, relation)
		{
			this.weight = weight;
		}

		public override string ToEditString()
		{
			StringWriter buf = new StringWriter();
			buf.Write(Label);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.GovNodenameArg);
			buf.Write(" ");
			buf.Write(govName);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.DepNodenameArg);
			buf.Write(" ");
			buf.Write(depName);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.RelnArg);
			buf.Write(" ");
			buf.Write(relation.ToString());
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.WeightArg);
			buf.Write(" ");
			buf.Write(weight.ToString());
			return buf.ToString();
		}

		public static Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.AddEdge CreateEngAddEdge(string govName, string depName, string engRelnName)
		{
			GrammaticalRelation reln = EnglishGrammaticalRelations.ValueOf(engRelnName);
			return new Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.AddEdge(govName, depName, reln);
		}

		public static Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.AddEdge CreateEngAddEdge(string govName, string depName, string engRelnName, double weight)
		{
			GrammaticalRelation reln = EnglishGrammaticalRelations.ValueOf(engRelnName);
			return new Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.AddEdge(govName, depName, reln, weight);
		}

		public override void Evaluate(SemanticGraph sg, SemgrexMatcher sm)
		{
			IndexedWord govNode = GetNamedNode(govName, sm);
			IndexedWord depNode = GetNamedNode(depName, sm);
			SemanticGraphEdge existingEdge = sg.GetEdge(govNode, depNode, relation);
			if (existingEdge == null)
			{
				// When adding the edge, check to see if the gov/dep nodes are presently in the graph.
				// 
				if (!sg.ContainsVertex(govNode))
				{
					sg.AddVertex(govNode);
				}
				if (!sg.ContainsVertex(depNode))
				{
					sg.AddVertex(depNode);
				}
				sg.AddEdge(govNode, depNode, relation, weight, false);
			}
		}
	}
}
