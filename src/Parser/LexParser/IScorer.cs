using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Interface for supporting A* scoring.</summary>
	/// <author>Dan Klein</author>
	public interface IScorer
	{
		double OScore(Edge edge);

		double IScore(Edge edge);

		bool OPossible(Hook hook);

		bool IPossible(Hook hook);

		bool Parse<_T0>(IList<_T0> words)
			where _T0 : IHasWord;
	}
}
