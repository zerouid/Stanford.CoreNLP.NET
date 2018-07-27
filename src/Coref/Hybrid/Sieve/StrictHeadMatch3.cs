

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class StrictHeadMatch3 : DeterministicCorefSieve
	{
		public StrictHeadMatch3()
			: base()
		{
			flags.USE_iwithini = true;
			flags.UseInclusionHeadmatch = true;
			flags.UseIncompatibleModifier = true;
		}
	}
}
