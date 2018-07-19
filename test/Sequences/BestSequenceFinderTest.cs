using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Sequences
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class BestSequenceFinderTest
	{
		private const bool Debug = false;

		public interface ITestSequenceModel : ISequenceModel
		{
			/// <summary>Returns the best label sequence.</summary>
			int[] CorrectAnswers();

			/// <summary>Returns the score for the best label sequence.</summary>
			double BestSequenceScore();
		}

		/// <summary>A class for testing best sequence finding on a SequenceModel.</summary>
		/// <remarks>
		/// A class for testing best sequence finding on a SequenceModel.
		/// This one isn't very tricky. It scores the correct answer with a label
		/// and all other answers as 0.  So you're pretty broken if you can't
		/// follow it.
		/// In the padding area you can only have tag 0. Otherwise, it likes the tag to match correctTags
		/// </remarks>
		public class TestSequenceModel1 : BestSequenceFinderTest.ITestSequenceModel
		{
			private readonly int[] correctTags = new int[] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 6, 5, 4, 3, 2, 1, 0, 0 };

			private readonly int[] allTags = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

			private readonly int[] midTags = new int[] { 0, 1, 2, 3 };

			private readonly int[] nullTags = new int[] { 0 };

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int Length()
			{
				return correctTags.Length - LeftWindow() - RightWindow();
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int LeftWindow()
			{
				return 2;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int RightWindow()
			{
				return 2;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int[] GetPossibleValues(int pos)
			{
				if (pos < LeftWindow() || pos >= LeftWindow() + Length())
				{
					return nullTags;
				}
				if (correctTags[pos] < 4)
				{
					return midTags;
				}
				return allTags;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double ScoreOf(int[] tags, int pos)
			{
				//System.out.println("Was asked: "+arrayToString(tags)+" at "+pos);
				bool match = true;
				for (int loc = pos - LeftWindow(); loc <= pos + RightWindow(); loc++)
				{
					if (tags[loc] != correctTags[loc])
					{
						match = false;
					}
				}
				if (match)
				{
					return pos;
				}
				return 0.0;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double ScoreOf(int[] sequence)
			{
				double score = 0.0;
				for (int i = LeftWindow(); i < LeftWindow() + Length(); i++)
				{
					score += ScoreOf(sequence, i);
				}
				return score;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double[] ScoresOf(int[] tags, int pos)
			{
				int[] tagsAtPos = GetPossibleValues(pos);
				double[] scores = new double[tagsAtPos.Length];
				for (int t = 0; t < tagsAtPos.Length; t++)
				{
					tags[pos] = tagsAtPos[t];
					scores[t] = ScoreOf(tags, pos);
				}
				return scores;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int[] CorrectAnswers()
			{
				return correctTags;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double BestSequenceScore()
			{
				return ScoreOf(correctTags);
			}
		}

		/// <summary>A second class for testing best sequence finding on a SequenceModel.</summary>
		/// <remarks>
		/// A second class for testing best sequence finding on a SequenceModel.
		/// This wants 0 in padding and a maximal ascending sequence inside, so gets 7, 8, 9
		/// </remarks>
		public class TestSequenceModel2 : BestSequenceFinderTest.ITestSequenceModel
		{
			private readonly int[] correctTags = new int[] { 0, 0, 7, 8, 9, 0, 0 };

			private readonly int[] allTags = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

			private readonly int[] midTags = new int[] { 0, 1, 2, 3, 4, 5 };

			private readonly int[] nullTags = new int[] { 0 };

			// end class TestSequenceModel
			// private final int[] correctTags = {0, 0, 7, 8, 9, 3, 4, 5, 0, 0};
			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int Length()
			{
				return correctTags.Length - LeftWindow() - RightWindow();
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int LeftWindow()
			{
				return 2;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int RightWindow()
			{
				return 2;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int[] GetPossibleValues(int pos)
			{
				if (pos < LeftWindow() || pos >= LeftWindow() + Length())
				{
					return nullTags;
				}
				if (pos < 5)
				{
					return allTags;
				}
				return midTags;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double ScoreOf(int[] tags, int pos)
			{
				double score;
				if (tags[pos] > tags[pos - 1] && tags[pos] <= tags[pos - 1] + 1)
				{
					//       if (tags[pos] <= tags[pos-1] + 1 && tags[pos] <= tags[pos-2] + 1) {
					score = tags[pos];
				}
				else
				{
					score = tags[pos] == 0 ? 0.0 : 1.0 / tags[pos];
				}
				// System.out.printf("Score of label %d for position %d in %s is %.2f%n",
				//     tags[pos], pos, Arrays.toString(tags), score);
				return score;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double ScoreOf(int[] sequence)
			{
				double score = 0.0;
				for (int i = LeftWindow(); i < LeftWindow() + Length(); i++)
				{
					score += ScoreOf(sequence, i);
				}
				return score;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double[] ScoresOf(int[] tags, int pos)
			{
				int[] tagsAtPos = GetPossibleValues(pos);
				double[] scores = new double[tagsAtPos.Length];
				for (int t = 0; t < tagsAtPos.Length; t++)
				{
					tags[pos] = tagsAtPos[t];
					scores[t] = ScoreOf(tags, pos);
				}
				return scores;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int[] CorrectAnswers()
			{
				return correctTags;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double BestSequenceScore()
			{
				return ScoreOf(correctTags);
			}
		}

		/// <summary>A variant of second class for testing best sequence finding on a SequenceModel.</summary>
		/// <remarks>
		/// A variant of second class for testing best sequence finding on a SequenceModel.
		/// This version has rightWindow == 0, which is sometimes needed.
		/// This wants 0 in padding and a maximal ascending sequence inside, so gets 7, 8, 9
		/// </remarks>
		public class TestSequenceModel2nr : BestSequenceFinderTest.ITestSequenceModel
		{
			private readonly int[] correctTags = new int[] { 0, 0, 7, 8, 9 };

			private readonly int[] allTags = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

			private readonly int[] midTags = new int[] { 0, 1, 2, 3, 4, 5 };

			private readonly int[] nullTags = new int[] { 0 };

			// end class TestSequenceModel2
			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int Length()
			{
				return correctTags.Length - LeftWindow() - RightWindow();
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int LeftWindow()
			{
				return 2;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int RightWindow()
			{
				return 0;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int[] GetPossibleValues(int pos)
			{
				if (pos < LeftWindow() || pos >= LeftWindow() + Length())
				{
					return nullTags;
				}
				if (pos < 5)
				{
					return allTags;
				}
				return midTags;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double ScoreOf(int[] tags, int pos)
			{
				double score;
				if (tags[pos] > tags[pos - 1] && tags[pos] <= tags[pos - 1] + 1)
				{
					//       if (tags[pos] <= tags[pos-1] + 1 && tags[pos] <= tags[pos-2] + 1) {
					score = tags[pos];
				}
				else
				{
					score = tags[pos] == 0 ? 0.0 : 1.0 / tags[pos];
				}
				// System.out.printf("Score of label %d for position %d in %s is %.2f%n",
				//     tags[pos], pos, Arrays.toString(tags), score);
				return score;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double ScoreOf(int[] sequence)
			{
				double score = 0.0;
				for (int i = LeftWindow(); i < LeftWindow() + Length(); i++)
				{
					score += ScoreOf(sequence, i);
				}
				return score;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double[] ScoresOf(int[] tags, int pos)
			{
				int[] tagsAtPos = GetPossibleValues(pos);
				double[] scores = new double[tagsAtPos.Length];
				for (int t = 0; t < tagsAtPos.Length; t++)
				{
					tags[pos] = tagsAtPos[t];
					scores[t] = ScoreOf(tags, pos);
				}
				return scores;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int[] CorrectAnswers()
			{
				return correctTags;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double BestSequenceScore()
			{
				return ScoreOf(correctTags);
			}
		}

		/// <summary>A third class for testing best sequence finding on a SequenceModel.</summary>
		public class TestSequenceModel3 : BestSequenceFinderTest.ITestSequenceModel
		{
			private readonly int[] correctTags = new int[] { 0, 1, 1, 1, 1, 1, 2, 2, 1, 2, 2, 1, 1, 1, 0 };

			private readonly int[] data = new int[] { 0, 5, 3, 7, 9, 4, 7, 8, 3, 7, 8, 3, 7, 3, 0 };

			private readonly int[] allTags = new int[] { 0, 1, 2 };

			private readonly int[] nullTags = new int[] { 0 };

			// end class TestSequenceModel2nr
			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int Length()
			{
				return correctTags.Length - LeftWindow() - RightWindow();
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int LeftWindow()
			{
				return 1;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int RightWindow()
			{
				return 1;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int[] GetPossibleValues(int pos)
			{
				if (pos < LeftWindow() || pos >= LeftWindow() + Length())
				{
					return nullTags;
				}
				return allTags;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double ScoreOf(int[] tags, int pos)
			{
				double score;
				if (data[pos] == 7 && tags[pos] == 2 && tags[pos + 1] == 2)
				{
					score = 1.0;
				}
				else
				{
					if (data[pos] == 8 && tags[pos] == 2)
					{
						score = 0.5;
					}
					else
					{
						if (tags[pos] == 1)
						{
							score = 0.1;
						}
						else
						{
							if (tags[pos] == 2)
							{
								score = -5.0;
							}
							else
							{
								score = 0.0;
							}
						}
					}
				}
				// System.out.printf("Score of label %d for position %d in %s is %.2f%n",
				//     tags[pos], pos, Arrays.toString(tags), score);
				return score;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double ScoreOf(int[] sequence)
			{
				double score = 0.0;
				for (int i = LeftWindow(); i < LeftWindow() + Length(); i++)
				{
					score += ScoreOf(sequence, i);
				}
				return score;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double[] ScoresOf(int[] tags, int pos)
			{
				int[] tagsAtPos = GetPossibleValues(pos);
				double[] scores = new double[tagsAtPos.Length];
				for (int t = 0; t < tagsAtPos.Length; t++)
				{
					tags[pos] = tagsAtPos[t];
					scores[t] = ScoreOf(tags, pos);
				}
				return scores;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual int[] CorrectAnswers()
			{
				return correctTags;
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			public virtual double BestSequenceScore()
			{
				return ScoreOf(correctTags);
			}
		}

		// end class TestSequenceModel2
		public static void RunSequenceFinder(BestSequenceFinderTest.ITestSequenceModel tsm, IBestSequenceFinder sf)
		{
			int[] bestLabels = sf.BestSequence(tsm);
			NUnit.Framework.Assert.IsTrue("Best sequence is wrong. Correct: " + Arrays.ToString(tsm.CorrectAnswers()) + ", found: " + Arrays.ToString(bestLabels), Arrays.Equals(tsm.CorrectAnswers(), bestLabels));
			NUnit.Framework.Assert.AreEqual("Best sequence score is wrong.", tsm.BestSequenceScore(), tsm.ScoreOf(bestLabels));
		}

		public static void RunPossibleValuesChecker(BestSequenceFinderTest.ITestSequenceModel tsm, IBestSequenceFinder sf)
		{
			int[] bestLabels = sf.BestSequence(tsm);
			// System.out.println("The best sequence is ... " + Arrays.toString(bestLabels));
			for (int i = 0; i < bestLabels.Length; i++)
			{
				int[] possibleValues = tsm.GetPossibleValues(i);
				bool found = false;
				foreach (int possible in possibleValues)
				{
					if (bestLabels[i] == possible)
					{
						found = true;
					}
				}
				if (!found)
				{
					Fail("Returned impossible label " + bestLabels[i] + " for position " + i);
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestExactBestSequenceFinder()
		{
			IBestSequenceFinder bsf = new ExactBestSequenceFinder();
			BestSequenceFinderTest.ITestSequenceModel tsm = new BestSequenceFinderTest.TestSequenceModel1();
			RunSequenceFinder(tsm, bsf);
			RunPossibleValuesChecker(tsm, bsf);
			BestSequenceFinderTest.ITestSequenceModel tsm2 = new BestSequenceFinderTest.TestSequenceModel2();
			RunSequenceFinder(tsm2, bsf);
			RunPossibleValuesChecker(tsm2, bsf);
			BestSequenceFinderTest.ITestSequenceModel tsm2nr = new BestSequenceFinderTest.TestSequenceModel2nr();
			RunSequenceFinder(tsm2nr, bsf);
			RunPossibleValuesChecker(tsm2nr, bsf);
			BestSequenceFinderTest.ITestSequenceModel tsm3 = new BestSequenceFinderTest.TestSequenceModel3();
			RunSequenceFinder(tsm3, bsf);
			RunPossibleValuesChecker(tsm3, bsf);
		}

		// This doesn't seem to work either.  Dodgy stuff in our BestSequenceFinder's
		/*
		public void testKBestSequenceFinder() {
		BestSequenceFinder bsf = new KBestSequenceFinder();
		TestSequenceModel tsm = new TestSequenceModel1();
		runSequenceFinder(tsm, bsf);
		TestSequenceModel tsm2 = new TestSequenceModel2();
		runSequenceFinder(tsm2, bsf);
		}
		*/
		[NUnit.Framework.Test]
		public virtual void TestBeamBestSequenceFinder()
		{
			IBestSequenceFinder bsf = new BeamBestSequenceFinder(5, true);
			BestSequenceFinderTest.ITestSequenceModel tsm = new BestSequenceFinderTest.TestSequenceModel1();
			RunSequenceFinder(tsm, bsf);
			RunPossibleValuesChecker(tsm, bsf);
		}

		// This one doesn't seem to work with any reasonable parameters.
		// And what is returned is non-deterministic. Heap of crap.
		// BestSequenceFinder bsf2 = new BeamBestSequenceFinder(5000000, false, false);
		// TestSequenceModel tsm2 = new TestSequenceModel2();
		// runSequenceFinder(tsm2, bsf);
		/// <summary>
		/// For a sequence sampler, we just check that the returned values are
		/// valid values.
		/// </summary>
		/// <remarks>
		/// For a sequence sampler, we just check that the returned values are
		/// valid values. We don't test the sampling distribution.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestSequenceSampler()
		{
			IBestSequenceFinder bsf = new SequenceSampler();
			BestSequenceFinderTest.ITestSequenceModel tsm = new BestSequenceFinderTest.TestSequenceModel1();
			RunPossibleValuesChecker(tsm, bsf);
			BestSequenceFinderTest.ITestSequenceModel tsm2 = new BestSequenceFinderTest.TestSequenceModel2();
			RunPossibleValuesChecker(tsm2, bsf);
			BestSequenceFinderTest.ITestSequenceModel tsm3 = new BestSequenceFinderTest.TestSequenceModel3();
			RunPossibleValuesChecker(tsm3, bsf);
		}
	}
}
