using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class AliasMatch : DeterministicCorefSieve
	{
		public AliasMatch()
			: base()
		{
			flags.USE_iwithini = true;
			flags.UseAttributesAgree = true;
			flags.UseAlias = true;
			flags.UseDifferentLocation = true;
			flags.UseNumberInMention = true;
		}
	}
}
