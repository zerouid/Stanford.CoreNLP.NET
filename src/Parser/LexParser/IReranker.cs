using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Metrics;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// A scorer which the RerankingParserQuery can use to rescore
	/// sentences.
	/// </summary>
	/// <remarks>
	/// A scorer which the RerankingParserQuery can use to rescore
	/// sentences.  process(sentence) will be called with the words in the
	/// sentence before score(tree) is called for any candidate trees for
	/// that sentence.
	/// <br />
	/// For example, TaggerReranker is a Reranker that adds a score based
	/// on how well a tree sentence matches the result of running a tagger,
	/// although this does not help the basic parser.
	/// <br />
	/// We want the interface to be threadsafe, so process() should return
	/// a RerankerQuery in a threadsafe manner.  The resulting
	/// RerankerQuery should store any needed temporary data about the
	/// sentence, etc.  For example, the TaggerReranker returns a
	/// RerankerQuery which stores the output of the tagger.  This way,
	/// subsequent calls to process() will not clobber existing data, and
	/// the RerankerQuery can potentially have RerankerQuery.score() called
	/// for different trees from different threads.
	/// <br />
	/// getEvals should return a list of Eval objects specific to this reranker.
	/// </remarks>
	/// <author>John Bauer</author>
	public interface IReranker
	{
		IRerankerQuery Process<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord;

		IList<IEval> GetEvals();
	}
}
