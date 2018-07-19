using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class RelaxedExactStringMatch : DeterministicCorefSieve
	{
		public RelaxedExactStringMatch()
			: base()
		{
			flags.UseRelaxedExactstringmatch = true;
		}
	}
}
