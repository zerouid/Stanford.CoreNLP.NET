using Edu.Stanford.Nlp.Semgraph.Semgrex;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Pred
{
	public interface ISsurgPred
	{
		// Given the current setup (each of the args in place), what is the truth value?  
		/// <exception cref="System.Exception"/>
		bool Test(SemgrexMatcher matched);
	}
}
