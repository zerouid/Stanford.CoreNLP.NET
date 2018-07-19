using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class DiscourseMatch : DeterministicCorefSieve
	{
		public DiscourseMatch()
			: base()
		{
			flags.UseDiscoursematch = true;
		}
	}
}
