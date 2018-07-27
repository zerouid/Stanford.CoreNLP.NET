

namespace Edu.Stanford.Nlp.Fsm
{
	/// <summary>Compacts a weighted finite automaton.</summary>
	/// <remarks>
	/// Compacts a weighted finite automaton. The returned automaton accepts at least the language
	/// accepted by the uncompactedFA (and perhaps more). The returned automaton is also weighted.
	/// </remarks>
	/// <author>Dan Klein (klein@cs.stanford.edu)</author>
	public interface IAutomatonCompactor
	{
		TransducerGraph CompactFA(TransducerGraph ucompactedFA);
	}
}
