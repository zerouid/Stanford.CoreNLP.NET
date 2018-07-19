using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Spence Green</author>
	public interface ILatticeScorer : IScorer
	{
		Item ConvertItemSpan(Item item);
	}
}
