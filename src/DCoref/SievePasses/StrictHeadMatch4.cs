

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
	public class StrictHeadMatch4 : DeterministicCorefSieve
	{
		public StrictHeadMatch4()
			: base()
		{
			flags.USE_iwithini = true;
			flags.UseInclusionHeadmatch = true;
			flags.UseProperheadAtLast = true;
			flags.UseDifferentLocation = true;
			flags.UseNumberInMention = true;
			flags.UseAttributesAgree = true;
		}
	}
}
