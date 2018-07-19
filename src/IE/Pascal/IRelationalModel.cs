using Sharpen;

namespace Edu.Stanford.Nlp.IE.Pascal
{
	/// <summary>An interface for the relational models in phase 2 of the pascal system.</summary>
	/// <author>Jamie Nicolson</author>
	public interface IRelationalModel
	{
		/// <param name="temp">template to be scored</param>
		/// <returns>its score</returns>
		double ComputeProb(PascalTemplate temp);
	}
}
