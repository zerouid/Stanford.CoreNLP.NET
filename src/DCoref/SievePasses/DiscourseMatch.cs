

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
	public class DiscourseMatch : DeterministicCorefSieve
	{
		public DiscourseMatch()
			: base()
		{
			flags.UseDiscoursematch = true;
		}
	}
}
