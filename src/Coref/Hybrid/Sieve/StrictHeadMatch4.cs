using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
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
