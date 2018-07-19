using Edu.Stanford.Nlp.Coref.Data;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid.Sieve
{
	/// <summary>Are two mentions compatible</summary>
	/// <author>Angel Chang</author>
	public interface IMentionMatcher
	{
		/// <summary>Determines if two mentions are compatible</summary>
		/// <param name="m1">First mention to compare</param>
		/// <param name="m2">Second mention to compare</param>
		/// <returns>true if compatible, false if incompatible, null if not sure</returns>
		bool IsCompatible(Mention m1, Mention m2);
	}
}
