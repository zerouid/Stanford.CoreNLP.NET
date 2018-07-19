using Sharpen;

namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>
	/// An interface for classes capable of computing the best sequence given
	/// a SequenceModel.
	/// </summary>
	/// <remarks>
	/// An interface for classes capable of computing the best sequence given
	/// a SequenceModel.
	/// Or it turns out that some implementations don't actually find the best
	/// sequence but just sample a sequence.  (SequenceSampler, I'm looking at
	/// you.)  I guess this makes sense if all sequences are scored equally.
	/// </remarks>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	public interface IBestSequenceFinder
	{
		/// <summary>Finds the best sequence for the sequence model based on its scoring.</summary>
		/// <returns>The sequence which is scored highest by the SequenceModel</returns>
		int[] BestSequence(ISequenceModel sequenceModel);
	}
}
