using Sharpen;

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
	public class RelaxedExactStringMatch : DeterministicCorefSieve
	{
		public RelaxedExactStringMatch()
			: base()
		{
			flags.UseRelaxedExactstringmatch = true;
		}
	}
}
