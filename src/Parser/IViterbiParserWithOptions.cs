

namespace Edu.Stanford.Nlp.Parser
{
	/// <summary>The interface for Viterbi parsers with options.</summary>
	/// <remarks>
	/// The interface for Viterbi parsers with options.  Viterbi parsers support
	/// getBestParse, which returns a best parse of the input, or
	/// <code>null</code> if no parse exists.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public interface IViterbiParserWithOptions : IViterbiParser
	{
		/// <summary>
		/// This will set options to a parser, in a way generally equivalent to
		/// passing in the same sequence of command-line arguments.
		/// </summary>
		/// <remarks>
		/// This will set options to a parser, in a way generally equivalent to
		/// passing in the same sequence of command-line arguments.  This is a useful
		/// convenience method when building a parser programmatically. The options
		/// passed in should
		/// be specified like command-line arguments, including with an initial
		/// minus sign.
		/// </remarks>
		/// <param name="flags">
		/// Arguments to the parser, for example,
		/// {"-outputFormat", "typedDependencies", "-maxLength", "70"}
		/// </param>
		/// <exception cref="System.ArgumentException">If an unknown flag is passed in</exception>
		void SetOptionFlags(params string[] flags);
	}
}
