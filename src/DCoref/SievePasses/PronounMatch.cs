

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
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
