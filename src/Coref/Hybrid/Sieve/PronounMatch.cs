

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class PronounMatch : DeterministicCorefSieve
	{
		public PronounMatch()
			: base()
		{
			flags.USE_iwithini = true;
			flags.DoPronoun = true;
		}
	}
}
