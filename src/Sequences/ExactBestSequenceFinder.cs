using System;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>A class capable of computing the best sequence given a SequenceModel.</summary>
	/// <remarks>
	/// A class capable of computing the best sequence given a SequenceModel.
	/// Uses the Viterbi algorithm.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Teg Grenager (grenager@stanford.edu)</author>
	public class ExactBestSequenceFinder : IBestSequenceFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ExactBestSequenceFinder));

		private const bool Debug = false;

		public static Pair<int[], double> BestSequenceWithLinearConstraints(ISequenceModel ts, double[][] linearConstraints)
		{
			return BestSequence(ts, linearConstraints);
		}

		/// <summary>
		/// Runs the Viterbi algorithm on the sequence model given by the TagScorer
		/// in order to find the best sequence.
		/// </summary>
		/// <param name="ts">The SequenceModel to be used for scoring</param>
		/// <returns>An array containing the int tags of the best sequence</returns>
		public virtual int[] BestSequence(ISequenceModel ts)
		{
			return BestSequence(ts, null).First();
		}

		private static Pair<int[], double> BestSequence(ISequenceModel ts, double[][] linearConstraints)
		{
			// Set up tag options
			int length = ts.Length();
			int leftWindow = ts.LeftWindow();
			int rightWindow = ts.RightWindow();
			int padLength = length + leftWindow + rightWindow;
			if (linearConstraints != null && linearConstraints.Length != padLength)
			{
				throw new Exception("linearConstraints.length (" + linearConstraints.Length + ") does not match padLength (" + padLength + ") of SequenceModel" + ", length==" + length + ", leftW=" + leftWindow + ", rightW=" + rightWindow);
			}
			int[][] tags = new int[padLength][];
			int[] tagNum = new int[padLength];
			for (int pos = 0; pos < padLength; pos++)
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting
					throw new RuntimeInterruptedException();
				}
				tags[pos] = ts.GetPossibleValues(pos);
				tagNum[pos] = tags[pos].Length;
			}
			int[] tempTags = new int[padLength];
			// Set up product space sizes
			int[] productSizes = new int[padLength];
			int curProduct = 1;
			for (int i = 0; i < leftWindow + rightWindow; i++)
			{
				curProduct *= tagNum[i];
			}
			for (int pos_1 = leftWindow + rightWindow; pos_1 < padLength; pos_1++)
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting
					throw new RuntimeInterruptedException();
				}
				if (pos_1 > leftWindow + rightWindow)
				{
					curProduct /= tagNum[pos_1 - leftWindow - rightWindow - 1];
				}
				// shift off
				curProduct *= tagNum[pos_1];
				// shift on
				productSizes[pos_1 - rightWindow] = curProduct;
			}
			// Score all of each window's options
			double[][] windowScore = new double[padLength][];
			for (int pos_2 = leftWindow; pos_2 < leftWindow + length; pos_2++)
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting
					throw new RuntimeInterruptedException();
				}
				windowScore[pos_2] = new double[productSizes[pos_2]];
				Arrays.Fill(tempTags, tags[0][0]);
				for (int product = 0; product < productSizes[pos_2]; product++)
				{
					if (Thread.Interrupted())
					{
						// Allow interrupting
						throw new RuntimeInterruptedException();
					}
					int p = product;
					int shift = 1;
					for (int curPos = pos_2 + rightWindow; curPos >= pos_2 - leftWindow; curPos--)
					{
						tempTags[curPos] = tags[curPos][p % tagNum[curPos]];
						p /= tagNum[curPos];
						if (curPos > pos_2)
						{
							shift *= tagNum[curPos];
						}
					}
					// Here now you get ts.scoresOf() for all classifications at a position at once, whereas the old code called ts.scoreOf() on each item.
					// CDM May 2007: The way this is done gives incorrect results if there are repeated values in the values of ts.getPossibleValues(pos) -- in particular if the first value of the array is repeated later.  I tried replacing it with the modulo version, but that only worked for left-to-right, not bidirectional inference, but I still think that if you sorted things out, you should be able to do it with modulos and the result would be conceptually simpler and robust to repeated values.  But in the meantime, I fixed the POS tagger to not give repeated values (which was a bug in the tagger).
					if (tempTags[pos_2] == tags[pos_2][0])
					{
						// get all tags at once
						double[] scores = ts.ScoresOf(tempTags, pos_2);
						// fill in the relevant windowScores
						for (int t = 0; t < tagNum[pos_2]; t++)
						{
							windowScore[pos_2][product + t * shift] = scores[t];
						}
					}
				}
			}
			// Set up score and backtrace arrays
			double[][] score = new double[padLength][];
			int[][] trace = new int[padLength][];
			for (int pos_3 = 0; pos_3 < padLength; pos_3++)
			{
				if (Thread.Interrupted())
				{
					// Allow interrupting
					throw new RuntimeInterruptedException();
				}
				score[pos_3] = new double[productSizes[pos_3]];
				trace[pos_3] = new int[productSizes[pos_3]];
			}
			// Do forward Viterbi algorithm
			// loop over the classification spot
			//log.info();
			for (int pos_4 = leftWindow; pos_4 < length + leftWindow; pos_4++)
			{
				//log.info(".");
				// loop over window product types
				for (int product = 0; product < productSizes[pos_4]; product++)
				{
					if (Thread.Interrupted())
					{
						// Allow interrupting
						throw new RuntimeInterruptedException();
					}
					// check for initial spot
					if (pos_4 == leftWindow)
					{
						// no predecessor type
						score[pos_4][product] = windowScore[pos_4][product];
						if (linearConstraints != null)
						{
							score[pos_4][product] += linearConstraints[pos_4][product % tagNum[pos_4]];
						}
						trace[pos_4][product] = -1;
					}
					else
					{
						// loop over possible predecessor types
						score[pos_4][product] = double.NegativeInfinity;
						trace[pos_4][product] = -1;
						int sharedProduct = product / tagNum[pos_4 + rightWindow];
						int factor = productSizes[pos_4] / tagNum[pos_4 + rightWindow];
						for (int newTagNum = 0; newTagNum < tagNum[pos_4 - leftWindow - 1]; newTagNum++)
						{
							int predProduct = newTagNum * factor + sharedProduct;
							double predScore = score[pos_4 - 1][predProduct] + windowScore[pos_4][product];
							if (linearConstraints != null)
							{
								predScore += linearConstraints[pos_4][product % tagNum[pos_4]];
							}
							if (predScore > score[pos_4][product])
							{
								score[pos_4][product] = predScore;
								trace[pos_4][product] = predProduct;
							}
						}
					}
				}
			}
			// Project the actual tag sequence
			double bestFinalScore = double.NegativeInfinity;
			int bestCurrentProduct = -1;
			for (int product_1 = 0; product_1 < productSizes[leftWindow + length - 1]; product_1++)
			{
				if (score[leftWindow + length - 1][product_1] > bestFinalScore)
				{
					bestCurrentProduct = product_1;
					bestFinalScore = score[leftWindow + length - 1][product_1];
				}
			}
			int lastProduct = bestCurrentProduct;
			for (int last = padLength - 1; last >= length - 1 && last >= 0; last--)
			{
				tempTags[last] = tags[last][lastProduct % tagNum[last]];
				lastProduct /= tagNum[last];
			}
			for (int pos_5 = leftWindow + length - 2; pos_5 >= leftWindow; pos_5--)
			{
				int bestNextProduct = bestCurrentProduct;
				bestCurrentProduct = trace[pos_5 + 1][bestNextProduct];
				tempTags[pos_5 - leftWindow] = tags[pos_5 - leftWindow][bestCurrentProduct / (productSizes[pos_5] / tagNum[pos_5 - leftWindow])];
			}
			return new Pair<int[], double>(tempTags, bestFinalScore);
		}
	}
}
