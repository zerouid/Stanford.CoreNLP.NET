using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Maps between the states of a more split and less split grammar.</summary>
	/// <remarks>
	/// Maps between the states of a more split and less split grammar.
	/// (Sort of a precursor to the idea of "coarse-to-fine" parsing.)
	/// </remarks>
	/// <author>Dan Klein</author>
	public interface IGrammarProjection
	{
		int Project(int state);

		UnaryGrammar SourceUG();

		BinaryGrammar SourceBG();

		UnaryGrammar TargetUG();

		BinaryGrammar TargetBG();
	}
}
