

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class RelaxedHeadMatch : DeterministicCorefSieve
	{
		public RelaxedHeadMatch()
			: base()
		{
			flags.USE_iwithini = true;
			flags.UseRelaxedHeadmatch = true;
			flags.UseWordsInclusion = true;
			flags.UseAttributesAgree = true;
		}
	}
}
