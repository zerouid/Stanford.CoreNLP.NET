using System;
using Edu.Stanford.Nlp.Stats;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>A SequenceFinder which can efficiently return a k-best list of sequence labellings.</summary>
	/// <author>Jenny Finkel</author>
	/// <author>Sven Zethelius</author>
	public class KBestSequenceFinder : IBestSequenceFinder
	{
		/// <summary>
		/// Runs the Viterbi algorithm on the sequence model
		/// in order to find the best sequence.
		/// </summary>
		/// <remarks>
		/// Runs the Viterbi algorithm on the sequence model
		/// in order to find the best sequence.
		/// This sequence finder only works on SequenceModel's with rightWindow == 0.
		/// </remarks>
		/// <returns>An array containing the int tags of the best sequence</returns>
		public virtual int[] BestSequence(ISequenceModel ts)
		{
			return Counters.Argmax(KBestSequences(ts, 1));
		}

		/// <summary>
		/// Runs the Viterbi algorithm on the sequence model, and then proceeds to efficiently
		/// backwards decode the best k label sequence assignments.
		/// </summary>
		/// <remarks>
		/// Runs the Viterbi algorithm on the sequence model, and then proceeds to efficiently
		/// backwards decode the best k label sequence assignments.
		/// This sequence finder only works on SequenceModel's with rightWindow == 0.
		/// </remarks>
		/// <param name="ts">The SequenceModel to find the best k label sequence assignments of</param>
		/// <param name="k">The number of top-scoring assignments to find.</param>
		/// <returns>A Counter with k entries that map from a sequence assignment (int array) to a double score</returns>
		public virtual ICounter<int[]> KBestSequences(ISequenceModel ts, int k)
		{
			// Set up tag options
			int length = ts.Length();
			int leftWindow = ts.LeftWindow();
			int rightWindow = ts.RightWindow();
			if (rightWindow != 0)
			{
				throw new ArgumentException("KBestSequenceFinder only works with rightWindow == 0 not " + rightWindow);
			}
			int padLength = length + leftWindow + rightWindow;
			int[][] tags = new int[padLength][];
			int[] tagNum = new int[padLength];
			for (int pos = 0; pos < padLength; pos++)
			{
				tags[pos] = ts.GetPossibleValues(pos);
				tagNum[pos] = tags[pos].Length;
			}
			int[] tempTags = new int[padLength];
			// Set up product space sizes
			int[] productSizes = new int[padLength];
			int curProduct = 1;
			for (int i = 0; i < leftWindow; i++)
			{
				curProduct *= tagNum[i];
			}
			for (int pos_1 = leftWindow; pos_1 < padLength; pos_1++)
			{
				if (pos_1 > leftWindow + rightWindow)
				{
					curProduct /= tagNum[pos_1 - leftWindow - rightWindow - 1];
				}
				// shift off
				curProduct *= tagNum[pos_1];
				// shift on
				productSizes[pos_1 - rightWindow] = curProduct;
			}
			double[][] windowScore = new double[padLength][];
			// Score all of each window's options
			for (int pos_2 = leftWindow; pos_2 < leftWindow + length; pos_2++)
			{
				windowScore[pos_2] = new double[productSizes[pos_2]];
				Arrays.Fill(tempTags, tags[0][0]);
				for (int product = 0; product < productSizes[pos_2]; product++)
				{
					int p = product;
					int shift = 1;
					for (int curPos = pos_2; curPos >= pos_2 - leftWindow; curPos--)
					{
						tempTags[curPos] = tags[curPos][p % tagNum[curPos]];
						p /= tagNum[curPos];
						if (curPos > pos_2)
						{
							shift *= tagNum[curPos];
						}
					}
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
			double[][][] score = new double[padLength][][];
			int[][][][] trace = new int[padLength][][][];
			int[][] numWaysToMake = new int[padLength][];
			for (int pos_3 = 0; pos_3 < padLength; pos_3++)
			{
				score[pos_3] = new double[productSizes[pos_3]][];
				trace[pos_3] = new int[productSizes[pos_3]][][];
				// the 2 is for backtrace, and which of the k best for that backtrace
				numWaysToMake[pos_3] = new int[productSizes[pos_3]];
				Arrays.Fill(numWaysToMake[pos_3], 1);
				for (int product = 0; product < productSizes[pos_3]; product++)
				{
					if (pos_3 > leftWindow)
					{
						// loop over possible predecessor types
						int sharedProduct = product / tagNum[pos_3];
						int factor = productSizes[pos_3] / tagNum[pos_3];
						numWaysToMake[pos_3][product] = 0;
						for (int newTagNum = 0; newTagNum < tagNum[pos_3 - leftWindow - 1] && numWaysToMake[pos_3][product] < k; newTagNum++)
						{
							int predProduct = newTagNum * factor + sharedProduct;
							numWaysToMake[pos_3][product] += numWaysToMake[pos_3 - 1][predProduct];
						}
						if (numWaysToMake[pos_3][product] > k)
						{
							numWaysToMake[pos_3][product] = k;
						}
					}
					score[pos_3][product] = new double[numWaysToMake[pos_3][product]];
					Arrays.Fill(score[pos_3][product], double.NegativeInfinity);
					trace[pos_3][product] = new int[numWaysToMake[pos_3][product]][];
					Arrays.Fill(trace[pos_3][product], new int[] { -1, -1 });
				}
			}
			// Do forward Viterbi algorithm
			// this is the hottest loop, so cache loop control variables hoping for a little speed....
			// loop over the classification spot
			for (int pos_4 = leftWindow; pos_4 < posMax; pos_4++)
			{
				// loop over window product types
				for (int product = 0; product < productMax; product++)
				{
					// check for initial spot
					double[] scorePos = score[pos_4][product];
					int[][] tracePos = trace[pos_4][product];
					if (pos_4 == leftWindow)
					{
						// no predecessor type
						scorePos[0] = windowScore[pos_4][product];
					}
					else
					{
						// loop over possible predecessor types/k-best
						int sharedProduct = product / tagNum[pos_4 + rightWindow];
						int factor = productSizes[pos_4] / tagNum[pos_4 + rightWindow];
						for (int newTagNum = 0; newTagNum < maxTagNum; newTagNum++)
						{
							int predProduct = newTagNum * factor + sharedProduct;
							double[] scorePosPrev = score[pos_4 - 1][predProduct];
							for (int k1 = 0; k1 < scorePosPrev.Length; k1++)
							{
								double predScore = scorePosPrev[k1] + windowScore[pos_4][product];
								if (predScore > scorePos[0])
								{
									// new value higher then lowest value we should keep
									int k2 = Arrays.BinarySearch(scorePos, predScore);
									k2 = k2 < 0 ? -k2 - 2 : k2 - 1;
									// open a spot at k2 by shifting off the lowest value
									System.Array.Copy(scorePos, 1, scorePos, 0, k2);
									System.Array.Copy(tracePos, 1, tracePos, 0, k2);
									scorePos[k2] = predScore;
									tracePos[k2] = new int[] { predProduct, k1 };
								}
							}
						}
					}
				}
			}
			// Project the actual tag sequence
			int[] whichDerivation = new int[k];
			int[] bestCurrentProducts = new int[k];
			double[] bestFinalScores = new double[k];
			Arrays.Fill(bestFinalScores, double.NegativeInfinity);
			// just the last guy
			for (int product_1 = 0; product_1 < productSizes[padLength - 1]; product_1++)
			{
				double[] scorePos = score[padLength - 1][product_1];
				for (int k1 = scorePos.Length - 1; k1 >= 0 && scorePos[k1] > bestFinalScores[0]; k1--)
				{
					int k2 = Arrays.BinarySearch(bestFinalScores, scorePos[k1]);
					k2 = k2 < 0 ? -k2 - 2 : k2 - 1;
					// open a spot at k2 by shifting off the lowest value
					System.Array.Copy(bestFinalScores, 1, bestFinalScores, 0, k2);
					System.Array.Copy(whichDerivation, 1, whichDerivation, 0, k2);
					System.Array.Copy(bestCurrentProducts, 1, bestCurrentProducts, 0, k2);
					bestCurrentProducts[k2] = product_1;
					whichDerivation[k2] = k1;
					bestFinalScores[k2] = scorePos[k1];
				}
			}
			ClassicCounter<int[]> kBestWithScores = new ClassicCounter<int[]>();
			for (int k1_1 = k - 1; k1_1 >= 0 && bestFinalScores[k1_1] > double.NegativeInfinity; k1_1--)
			{
				int lastProduct = bestCurrentProducts[k1_1];
				for (int last = padLength - 1; last >= length - 1 && last >= 0; last--)
				{
					tempTags[last] = tags[last][lastProduct % tagNum[last]];
					lastProduct /= tagNum[last];
				}
				for (int pos_5 = leftWindow + length - 2; pos_5 >= leftWindow; pos_5--)
				{
					int bestNextProduct = bestCurrentProducts[k1_1];
					bestCurrentProducts[k1_1] = trace[pos_5 + 1][bestNextProduct][whichDerivation[k1_1]][0];
					whichDerivation[k1_1] = trace[pos_5 + 1][bestNextProduct][whichDerivation[k1_1]][1];
					tempTags[pos_5 - leftWindow] = tags[pos_5 - leftWindow][bestCurrentProducts[k1_1] / (productSizes[pos_5] / tagNum[pos_5 - leftWindow])];
				}
				kBestWithScores.SetCount(Arrays.CopyOf(tempTags, tempTags.Length), bestFinalScores[k1_1]);
			}
			return kBestWithScores;
		}
	}
}
