

namespace Edu.Stanford.Nlp.Dcoref.Sievepasses
{
	/// <summary>Use name matcher - more precise match</summary>
	/// <author>Angel Chang</author>
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
