using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.IE.Crf
{
	/// <summary>For sequence model inference at test-time.</summary>
	/// <author>Spence Green</author>
	public class TestSequenceModel : ISequenceModel
	{
		private readonly int window;

		private readonly CRFCliqueTree<ICharSequence> cliqueTree;

		private readonly int[] backgroundTag;

		private readonly int[] allTags;

		private readonly int[][] allowedTagsAtPosition;

		public TestSequenceModel(CRFCliqueTree<ICharSequence> cliqueTree)
			: this(cliqueTree, null, null)
		{
		}

		public TestSequenceModel(CRFCliqueTree<ICharSequence> cliqueTree, LabelDictionary labelDictionary, IList<ICoreMap> document)
		{
			// private final int numClasses;
			// todo [cdm 2014]: Just make String?
			// this.factorTables = factorTables;
			this.cliqueTree = cliqueTree;
			// this.window = factorTables[0].windowSize();
			this.window = cliqueTree.Window();
			// this.numClasses = factorTables[0].numClasses();
			int numClasses = cliqueTree.GetNumClasses();
			this.backgroundTag = new int[] { cliqueTree.BackgroundIndex() };
			allTags = new int[numClasses];
			for (int i = 0; i < allTags.Length; i++)
			{
				allTags[i] = i;
			}
			if (labelDictionary != null)
			{
				// Constrained
				allowedTagsAtPosition = new int[document.Count][];
				for (int i_1 = 0; i_1 < allowedTagsAtPosition.Length; ++i_1)
				{
					ICoreMap token = document[i_1];
					string observation = token.Get(typeof(CoreAnnotations.TextAnnotation));
					allowedTagsAtPosition[i_1] = labelDictionary.IsConstrained(observation) ? labelDictionary.GetConstrainedSet(observation) : allTags;
				}
			}
			else
			{
				allowedTagsAtPosition = null;
			}
		}

		public virtual int Length()
		{
			return cliqueTree.Length();
		}

		public virtual int LeftWindow()
		{
			return window - 1;
		}

		public virtual int RightWindow()
		{
			return 0;
		}

		public virtual int[] GetPossibleValues(int pos)
		{
			if (pos < LeftWindow())
			{
				return backgroundTag;
			}
			int realPos = pos - window + 1;
			return allowedTagsAtPosition == null ? allTags : allowedTagsAtPosition[realPos];
		}

		/// <summary>Return the score of the proposed tags for position given.</summary>
		/// <param name="tags">is an array indicating the assignment of labels to score.</param>
		/// <param name="pos">is the position to return a score for.</param>
		public virtual double ScoreOf(int[] tags, int pos)
		{
			int[] previous = new int[window - 1];
			int realPos = pos - window + 1;
			for (int i = 0; i < window - 1; i++)
			{
				previous[i] = tags[realPos + i];
			}
			return cliqueTree.CondLogProbGivenPrevious(realPos, tags[pos], previous);
		}

		public virtual double[] ScoresOf(int[] tags, int pos)
		{
			int[] allowedTags = GetPossibleValues(pos);
			int realPos = pos - window + 1;
			int[] previous = new int[window - 1];
			for (int i = 0; i < window - 1; i++)
			{
				previous[i] = tags[realPos + i];
			}
			double[] scores = new double[allowedTags.Length];
			for (int i_1 = 0; i_1 < allowedTags.Length; i_1++)
			{
				scores[i_1] = cliqueTree.CondLogProbGivenPrevious(realPos, allowedTags[i_1], previous);
			}
			return scores;
		}

		public virtual double ScoreOf(int[] sequence)
		{
			throw new NotSupportedException();
		}
	}
}
