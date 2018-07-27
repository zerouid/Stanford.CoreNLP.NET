

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
	public class LexicalChainMatch : DeterministicCorefSieve
	{
		public LexicalChainMatch()
			: base()
		{
			flags.USE_iwithini = true;
			flags.UseAttributesAgree = true;
			flags.UseWnHypernym = true;
			flags.UseWnSynonym = true;
			flags.UseDifferentLocation = true;
			flags.UseNumberInMention = true;
		}
	}
}
