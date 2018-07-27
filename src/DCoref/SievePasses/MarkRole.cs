

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
	public class MarkRole : DeterministicCorefSieve
	{
		public MarkRole()
			: base()
		{
			flags.UseRoleSkip = true;
		}
	}
}
