

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class StrictHeadMatch2 : DeterministicCorefSieve
	{
		public StrictHeadMatch2()
			: base()
		{
			flags.USE_iwithini = true;
			flags.UseInclusionHeadmatch = true;
			flags.UseWordsInclusion = true;
		}
	}
}
