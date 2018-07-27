

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
	public class StrictHeadMatch1 : DeterministicCorefSieve
	{
		public StrictHeadMatch1()
			: base()
		{
			flags.USE_iwithini = true;
			flags.UseInclusionHeadmatch = true;
			flags.UseIncompatibleModifier = true;
			flags.UseWordsInclusion = true;
		}
	}
}
