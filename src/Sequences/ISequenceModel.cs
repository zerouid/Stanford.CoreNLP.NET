

namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>Interface for scoring the labeling of sequences of a fixed length.</summary>
	/// <remarks>
	/// Interface for scoring the labeling of sequences of a fixed length.
	/// Each label is represented by integers, typically generated using an
	/// Index. Usually, labeling is done via a probability distribution over
	/// labels for sequences.
	/// </remarks>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	public interface ISequenceModel
	{
		/// <returns>
		/// The length of the sequences modeled by this SequenceModel.
		/// This is its real length, not including the leftWindow() +
		/// rightWindow() of extra padding positions.
		/// </returns>
		int Length();

		/// <summary>
		/// How many label positions to the left influence the label assignment
		/// at a particular position.
		/// </summary>
		/// <returns>The size of the left window used by this sequence model</returns>
		int LeftWindow();

		/// <summary>
		/// How many label positions to the right influence the label assignment
		/// at a particular position.
		/// </summary>
		/// <returns>the size of the right window used by this sequence model</returns>
		int RightWindow();

		/// <summary>
		/// Return the valid sequence labels (as integer indices) for a particular
		/// position in the sequence.
		/// </summary>
		/// <remarks>
		/// Return the valid sequence labels (as integer indices) for a particular
		/// position in the sequence. Since the sequence is padded at each end,
		/// typically sequence items 0...leftWindow-1 are null,
		/// leftWindow...length+leftWindow-1 are words,
		/// length+leftWindow...length+leftWindow+rightWindow-1 are null.
		/// </remarks>
		/// <param name="position">The position</param>
		/// <returns>The set of possible int values at this position, as an int array</returns>
		int[] GetPossibleValues(int position);

		/// <summary>Computes the score assigned by this model to the whole sequence.</summary>
		/// <remarks>
		/// Computes the score assigned by this model to the whole sequence.
		/// Typically this will be an unnormalized
		/// probability in log space (since the probabilities are small).
		/// </remarks>
		/// <param name="sequence">The sequence of labels to compute a score for</param>
		/// <returns>The score for the entire sequence</returns>
		double ScoreOf(int[] sequence);

		/// <summary>
		/// Computes the score of the element at the given position in the sequence,
		/// conditioned on the values of the elements in all other positions of the
		/// provided sequence.
		/// </summary>
		/// <remarks>
		/// Computes the score of the element at the given position in the sequence,
		/// conditioned on the values of the elements in all other positions of the
		/// provided sequence. Typically, this is an unnormalized log conditional
		/// probability of the label at the given position in the sequence, given the
		/// input data nd the other labels.
		/// </remarks>
		/// <param name="sequence">
		/// The sequence containing the prediction and the rest of the
		/// labels to condition on
		/// </param>
		/// <param name="position">The position of the element to give a score for</param>
		/// <returns>The score of the label at the specified position in the sequence</returns>
		double ScoreOf(int[] sequence, int position);

		/// <summary>
		/// Computes the scores of labels for the element at the given position in
		/// the sequence, conditioned on the values of the labels at all other
		/// positions of the provided sequence.
		/// </summary>
		/// <remarks>
		/// Computes the scores of labels for the element at the given position in
		/// the sequence, conditioned on the values of the labels at all other
		/// positions of the provided sequence. The returned array elements
		/// correspond index-by-index to the possible values returned by
		/// getPossibleValues(int). The label at sequence[position] is ignored in
		/// the calculations.  The scores are often given as an unnormalized log
		/// conditional distribution over possible labels. Otherwise, they may be
		/// probabilities. If it is a probability distribution, conceptually, the
		/// scores should sum to 1 (perhaps after transforming them). Note that
		/// implementations often alter sequence[position] to do their calculations,
		/// and so access to the array isn't threadsafe, but should restore its
		/// value afterwards.
		/// </remarks>
		/// <param name="sequence">The sequence containing the rest of the values to condition on</param>
		/// <param name="position">The position of the element to give a distribution for</param>
		/// <returns>The scores of the possible tokens at the specified position in the sequence</returns>
		double[] ScoresOf(int[] sequence, int position);
	}
}
