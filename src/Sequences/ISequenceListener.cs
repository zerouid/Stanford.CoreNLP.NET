

namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>
	/// A class capable of listening to changes about a sequence,
	/// represented as an array of type int.
	/// </summary>
	/// <author>grenager</author>
	public interface ISequenceListener
	{
		/// <summary>Informs this sequence listener that the value of the element at position pos has changed.</summary>
		/// <remarks>
		/// Informs this sequence listener that the value of the element at position pos has changed.
		/// This allows this sequence model to update its internal model if desired.
		/// </remarks>
		void UpdateSequenceElement(int[] sequence, int pos, int oldVal);

		/// <summary>Informs this sequence listener that the value of the whole sequence is initialized to sequence.</summary>
		void SetInitialSequence(int[] sequence);
	}
}
