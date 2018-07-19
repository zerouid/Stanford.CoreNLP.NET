using System;
using Edu.Stanford.Nlp.Math;
using Sharpen;

namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>This class will sample an output from a sequence model.</summary>
	/// <remarks>
	/// This class will sample an output from a sequence model.  It assumes that
	/// the scores are (unnormalized) log-probabilities.  It works by sampling
	/// each variable in order, conditioned on the previous variables.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public class SequenceSampler : IBestSequenceFinder
	{
		/// <summary>Samples each label in turn from left to right.</summary>
		/// <returns>an array containing the int tags of the best sequence</returns>
		public virtual int[] BestSequence(ISequenceModel ts)
		{
			// Also allocate space for rightWindow, just in case sequence model uses
			// it, even though this implementation doesn't. Probably it shouldn't,
			// or the left-to-right sampling is invalid, but our test classes do.
			int[] sample = new int[ts.Length() + ts.LeftWindow() + ts.RightWindow()];
			for (int pos = ts.LeftWindow(); pos < sample.Length - ts.RightWindow(); pos++)
			{
				double[] scores = ts.ScoresOf(sample, pos);
				for (int i = 0; i < scores.Length; i++)
				{
					scores[i] = Math.Exp(scores[i]);
				}
				ArrayMath.Normalize(scores);
				int l = ArrayMath.SampleFromDistribution(scores);
				sample[pos] = ts.GetPossibleValues(pos)[l];
			}
			return sample;
		}
	}
}
