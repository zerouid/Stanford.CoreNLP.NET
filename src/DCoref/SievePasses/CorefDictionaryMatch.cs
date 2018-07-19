using Sharpen;

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
	/// <summary>
	/// Sieve that uses the coreference dictionary for the technical domain
	/// developed by Recasens, Can and Jurafsky (NAACL 2013).
	/// </summary>
	/// <author>recasens</author>
	public class CorefDictionaryMatch : DeterministicCorefSieve
	{
		public CorefDictionaryMatch()
			: base()
		{
			flags.USE_iwithini = true;
			flags.UseDifferentLocation = true;
			flags.UseNumberInMention = true;
			flags.UseDistance = true;
			flags.UseAttributesAgree = true;
			flags.UseCorefDict = true;
		}
	}
}
