

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
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
