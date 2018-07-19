using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class ExactStringMatch : DeterministicCorefSieve
	{
		public ExactStringMatch()
			: base()
		{
			flags.UseExactstringmatch = true;
		}
	}
}
