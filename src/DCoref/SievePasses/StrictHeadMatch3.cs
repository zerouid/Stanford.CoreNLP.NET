

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
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
