using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Parser
{
	/// <summary>The interface for Viterbi parsers.</summary>
	/// <remarks>
	/// The interface for Viterbi parsers.  Viterbi parsers support
	/// getBestParse, which returns a best parse of the input, or
	/// <code>null</code> if no parse exists.
	/// </remarks>
	/// <author>Dan Klein</author>
	public interface IViterbiParser : IParser
	{
		/// <summary>
		/// Returns a best parse of the last sentence on which <code>parse</code> was
		/// called, or null if none exists.
		/// </summary>
		/// <returns>The tree for the best parse</returns>
		Tree GetBestParse();
	}
}
