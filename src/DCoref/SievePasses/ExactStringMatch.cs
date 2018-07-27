

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
	public class ExactStringMatch : DeterministicCorefSieve
	{
		public ExactStringMatch()
			: base()
		{
			flags.UseExactstringmatch = true;
		}
	}
}
