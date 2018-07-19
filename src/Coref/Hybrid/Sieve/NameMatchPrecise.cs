using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	/// <summary>Use name matcher - more precise match</summary>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class NameMatchPrecise : NameMatch
	{
		public NameMatchPrecise()
			: base()
		{
			ignoreGender = false;
			minTokens = 2;
		}
	}
}
