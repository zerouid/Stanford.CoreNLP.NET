using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	[System.Serializable]
	public class ChineseHeadMatch : DeterministicCorefSieve
	{
		public ChineseHeadMatch()
			: base()
		{
			flags.UseChineseHeadMatch = true;
		}

		public ChineseHeadMatch(Properties props)
			: base(props)
		{
			// for debug
			flags.UseChineseHeadMatch = true;
		}
	}
}
