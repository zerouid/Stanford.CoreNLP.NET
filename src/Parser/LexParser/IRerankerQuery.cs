using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Process a Tree and return a score.</summary>
	/// <remarks>
	/// Process a Tree and return a score.  Typically constructed by the
	/// Reranker, possibly given some extra information about the sentence
	/// being parsed.
	/// </remarks>
	/// <author>John Bauer</author>
	public interface IRerankerQuery
	{
		double Score(Tree tree);
	}
}
