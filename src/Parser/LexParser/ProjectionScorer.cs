using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Dan Klein</author>
	internal class ProjectionScorer : IScorer
	{
		protected internal IGrammarProjection gp;

		protected internal IScorer scorer;

		protected internal readonly Options op;

		protected internal virtual Edge Project(Edge edge)
		{
			Edge tempEdge = new Edge(op.testOptions.exhaustiveTest);
			tempEdge.start = edge.start;
			tempEdge.end = edge.end;
			tempEdge.state = gp.Project(edge.state);
			tempEdge.head = edge.head;
			tempEdge.tag = edge.tag;
			return tempEdge;
		}

		protected internal virtual Hook Project(Hook hook)
		{
			Hook tempHook = new Hook(op.testOptions.exhaustiveTest);
			tempHook.start = hook.start;
			tempHook.end = hook.end;
			tempHook.state = gp.Project(hook.state);
			tempHook.head = hook.head;
			tempHook.tag = hook.tag;
			tempHook.subState = gp.Project(hook.subState);
			return tempHook;
		}

		public virtual double OScore(Edge edge)
		{
			return scorer.OScore(Project(edge));
		}

		public virtual double IScore(Edge edge)
		{
			return scorer.IScore(Project(edge));
		}

		public virtual bool OPossible(Hook hook)
		{
			return scorer.OPossible(Project(hook));
		}

		public virtual bool IPossible(Hook hook)
		{
			return scorer.IPossible(Project(hook));
		}

		public virtual bool Parse<_T0>(IList<_T0> words)
			where _T0 : IHasWord
		{
			return scorer.Parse(words);
		}

		public ProjectionScorer(IScorer scorer, IGrammarProjection gp, Options op)
		{
			this.scorer = scorer;
			this.gp = gp;
			this.op = op;
		}
	}
}
