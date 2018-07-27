using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>
	/// Removes the edge with the given relation type (string name), between
	/// two named nodes in a graph match.
	/// </summary>
	/// <author>yeh1</author>
	public class RemoveEdge : SsurgeonEdit
	{
		public const string Label = "removeEdge";

		protected internal GrammaticalRelation relation;

		protected internal string govName;

		protected internal string depName;

		public RemoveEdge(GrammaticalRelation relation, string govName, string depName)
		{
			// Name of the matched relation type
			// Name of governor of this reln, in match
			// Name of the dependent in this reln, in match
			this.relation = relation;
			this.govName = govName;
			this.depName = depName;
		}

		public override string ToEditString()
		{
			StringWriter buf = new StringWriter();
			buf.Write(Label);
			buf.Write("\t");
			buf.Write(Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Ssurgeon.RelnArg);
			buf.Write(" ");
			buf.Write(relation.ToString());
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

		public const string WildcardNode = "**WILDNODE**";

		public override void Evaluate(SemanticGraph sg, SemgrexMatcher sm)
		{
			bool govWild = govName.Equals(WildcardNode);
			bool depWild = depName.Equals(WildcardNode);
			IndexedWord govNode = GetNamedNode(govName, sm);
			IndexedWord depNode = GetNamedNode(depName, sm);
			if (govNode != null && depNode != null)
			{
				SemanticGraphEdge edge = sg.GetEdge(govNode, depNode, relation);
				if (edge != null)
				{
					bool successFlag = sg.RemoveEdge(edge);
				}
			}
			else
			{
				if (depNode != null && govWild)
				{
					// dep known, wildcard gov
					foreach (SemanticGraphEdge edge in sg.IncomingEdgeIterable(depNode))
					{
						if (edge.GetRelation().Equals(relation) && sg.ContainsEdge(edge))
						{
							sg.RemoveEdge(edge);
						}
					}
				}
				else
				{
					if (govNode != null && depWild)
					{
						// gov known, wildcard dep
						foreach (SemanticGraphEdge edge in sg.OutgoingEdgeIterable(govNode))
						{
							if (edge.GetRelation().Equals(relation) && sg.ContainsEdge(edge))
							{
								sg.RemoveEdge(edge);
							}
						}
					}
				}
			}
		}

		public virtual string GetDepName()
		{
			return depName;
		}

		public virtual string GetGovName()
		{
			return govName;
		}

		public virtual string GetRelationName()
		{
			return relation.ToString();
		}
	}
}
