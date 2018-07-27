using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Dan Klein</author>
	internal class TwinScorer : IScorer
	{
		private IScorer scorer1;

		private IScorer scorer2;

		public virtual double OScore(Edge edge)
		{
			return scorer1.OScore(edge) + scorer2.OScore(edge);
		}

		public virtual double IScore(Edge edge)
		{
			return scorer1.IScore(edge) + scorer2.IScore(edge);
		}

		public virtual bool OPossible(Hook hook)
		{
			return scorer1.OPossible(hook) && scorer2.OPossible(hook);
		}

		public virtual bool IPossible(Hook hook)
		{
			return scorer1.IPossible(hook) && scorer2.IPossible(hook);
		}

		public virtual bool Parse<_T0>(IList<_T0> words)
			where _T0 : IHasWord
		{
			bool b1 = scorer1.Parse(words);
			bool b2 = scorer2.Parse(words);
			return (b1 && b2);
		}

		public TwinScorer(IScorer scorer1, IScorer scorer2)
		{
			this.scorer1 = scorer1;
			this.scorer2 = scorer2;
		}
	}
}
