using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Sequences
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class KBestSequenceFinderTest
	{
		private const bool Debug = false;

		private const int K2nr = 20;

		private static string[] test2nrAnswers = new string[] { "[0, 0, 7, 8, 9]", "[0, 0, 6, 7, 8]", "[0, 0, 5, 6, 7]", "[0, 0, 4, 5, 6]", "[0, 0, 8, 9, 1]", "[0, 0, 1, 8, 9]", "[0, 0, 2, 8, 9]", "[0, 0, 8, 9, 2]", "[0, 0, 8, 9, 3]", "[0, 0, 3, 8, 9]"
			, "[0, 0, 4, 8, 9]", "[0, 0, 8, 9, 4]", "[0, 0, 3, 4, 5]", "[0, 0, 8, 9, 5]", "[0, 0, 5, 8, 9]", "[0, 0, 6, 8, 9]", "[0, 0, 8, 9, 6]", "[0, 0, 8, 9, 7]", "[0, 0, 8, 8, 9]", "[0, 0, 8, 9, 8]" };

		private static double[] test2nrScores = new double[] { 17.142857142857142, 15.166666666666668, 13.2, 11.25, 10.125, 10.125, 9.625, 9.625, 9.458333333333334, 9.458333333333334, 9.375, 9.375, 9.333333333333332, 9.325, 9.325, 9.291666666666666, 
			9.291666666666666, 9.267857142857142, 9.25, 9.25 };

		[NUnit.Framework.Test]
		public virtual void TestPerStateBestSequenceFinder()
		{
			KBestSequenceFinder bsf = new KBestSequenceFinder();
			BestSequenceFinderTest.ITestSequenceModel tsm2nr = new BestSequenceFinderTest.TestSequenceModel2nr();
			RunSequencesFinder(tsm2nr, bsf);
			BestSequenceFinderTest.RunPossibleValuesChecker(tsm2nr, bsf);
		}

		public static void RunSequencesFinder(BestSequenceFinderTest.ITestSequenceModel tsm, KBestSequenceFinder sf)
		{
			ICounter<int[]> bestLabelsCounter = sf.KBestSequences(tsm, K2nr);
			IList<int[]> topValues = Counters.ToSortedList(bestLabelsCounter);
			IEnumerator<int[]> iter = topValues.GetEnumerator();
			for (int i = 0; i < K2nr; i++)
			{
				int[] sequence = iter.Current;
				string strSequence = Arrays.ToString(sequence);
				double score = bestLabelsCounter.GetCount(sequence);
				// Deal with ties in the scoring ... only tied pairs handled.
				bool found = false;
				if (strSequence.Equals(test2nrAnswers[i]))
				{
					found = true;
				}
				else
				{
					if (i > 0 && Math.Abs(score - test2nrScores[i - 1]) < 1e-8 && strSequence.Equals(test2nrAnswers[i - 1]))
					{
						found = true;
					}
					else
					{
						if (i + 1 < test2nrScores.Length && Math.Abs(score - test2nrScores[i + 1]) < 1e-8 && strSequence.Equals(test2nrAnswers[i + 1]))
						{
							found = true;
						}
					}
				}
				NUnit.Framework.Assert.IsTrue("Best sequence is wrong. Correct: " + test2nrAnswers[i] + ", found: " + strSequence, found);
				NUnit.Framework.Assert.AreEqual("Best sequence score is wrong.", test2nrScores[i], score, 1e-8);
			}
		}
	}
}
