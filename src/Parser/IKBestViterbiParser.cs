using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser
{
	/// <summary>
	/// An interface that supports finding k best and/or k good
	/// parses and parse sampling.
	/// </summary>
	/// <remarks>
	/// An interface that supports finding k best and/or k good
	/// parses and parse sampling.
	/// These operations are specified by separate methods,
	/// but it is expected that many parsers will return
	/// an UnsupportedOperationException for some of these methods.
	/// This has some other methods that essentially provide a rich
	/// parser interface which is used by certain parsers in lexparser,
	/// including other convenience methods like hasParse() and
	/// getBestScore().
	/// </remarks>
	/// <author>Christopher Manning</author>
	public interface IKBestViterbiParser : IViterbiParser
	{
		/// <summary>Get the exact k best parses for the sentence.</summary>
		/// <param name="k">The number of best parses to return</param>
		/// <returns>
		/// The exact k best parses for the sentence, with
		/// each accompanied by its score (typically a
		/// negative log probability).
		/// </returns>
		IList<ScoredObject<Tree>> GetKBestParses(int k);

		/// <summary>
		/// Get a complete set of the maximally scoring parses for a sentence,
		/// rather than one chosen at random.
		/// </summary>
		/// <remarks>
		/// Get a complete set of the maximally scoring parses for a sentence,
		/// rather than one chosen at random.  This set may be of size 1 or larger.
		/// </remarks>
		/// <returns>
		/// All the equal best parses for a sentence, with each
		/// accompanied by its score
		/// </returns>
		IList<ScoredObject<Tree>> GetBestParses();

		/// <summary>Get k good parses for the sentence.</summary>
		/// <remarks>
		/// Get k good parses for the sentence.  It is expected that the
		/// parses returned approximate the k best parses, but without any
		/// guarantee that the exact list of k best parses has been produced.
		/// If a class really provides k best parses functionality, it is
		/// reasonable to also return this output as the k good parses.
		/// </remarks>
		/// <param name="k">The number of good parses to return</param>
		/// <returns>
		/// A list of k good parses for the sentence, with
		/// each accompanied by its score
		/// </returns>
		IList<ScoredObject<Tree>> GetKGoodParses(int k);

		/// <summary>Get k parse samples for the sentence.</summary>
		/// <remarks>
		/// Get k parse samples for the sentence.  It is expected that the
		/// parses are sampled based on their relative probability.
		/// </remarks>
		/// <param name="k">The number of sampled parses to return</param>
		/// <returns>
		/// A list of k parse samples for the sentence, with
		/// each accompanied by its score
		/// </returns>
		IList<ScoredObject<Tree>> GetKSampledParses(int k);

		/// <summary>
		/// Does the sentence in the last call to parse() have a parse?
		/// In theory this method shouldn't be here, but it seemed a
		/// convenient place to put it for our more general parser interface.
		/// </summary>
		/// <returns>Whether the last sentence parsed had a parse</returns>
		bool HasParse();

		/// <summary>
		/// Gets the score (typically a log probability) of the best
		/// parse of a sentence.
		/// </summary>
		/// <returns>The score for the last sentence parsed.</returns>
		double GetBestScore();
	}
}
