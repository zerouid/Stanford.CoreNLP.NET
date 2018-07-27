

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class MarkRole : DeterministicCorefSieve
	{
		public MarkRole()
			: base()
		{
			flags.UseRoleSkip = true;
		}
	}
}
