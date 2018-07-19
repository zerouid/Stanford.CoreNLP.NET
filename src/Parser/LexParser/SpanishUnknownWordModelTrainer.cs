using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class SpanishUnknownWordModelTrainer : AbstractUnknownWordModelTrainer
	{
		private ClassicCounter<IntTaggedWord> seenCounter;

		private ClassicCounter<IntTaggedWord> unSeenCounter;

		private double indexToStartUnkCounting;

		private const string BoundaryTag = ".$$.";

		private IUnknownWordModel model;

		// boundary tag -- assumed not a real tag
		public override void InitializeTraining(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, double totalTrees)
		{
			base.InitializeTraining(op, lex, wordIndex, tagIndex, totalTrees);
			indexToStartUnkCounting = (totalTrees * op.trainOptions.fractionBeforeUnseenCounting);
			seenCounter = new ClassicCounter<IntTaggedWord>();
			unSeenCounter = new ClassicCounter<IntTaggedWord>();
			model = new SpanishUnknownWordModel(op, lex, wordIndex, tagIndex, unSeenCounter);
		}

		/// <summary>Trains this lexicon on the Collection of trees.</summary>
		public override void Train(TaggedWord tw, int loc, double weight)
		{
			IntTaggedWord iTW = new IntTaggedWord(tw.Word(), tw.Tag(), wordIndex, tagIndex);
			IntTaggedWord iT = new IntTaggedWord(UnknownWordModelTrainerConstants.nullWord, iTW.tag);
			IntTaggedWord iW = new IntTaggedWord(iTW.word, UnknownWordModelTrainerConstants.nullTag);
			seenCounter.IncrementCount(iW, weight);
			IntTaggedWord i = UnknownWordModelTrainerConstants.NullItw;
			if (treesRead > indexToStartUnkCounting)
			{
				// start doing this once some way through trees;
				// treesRead is 1 based counting
				if (seenCounter.GetCount(iW) < 2)
				{
					// it's an entirely unknown word
					int s = model.GetSignatureIndex(iTW.word, loc, wordIndex.Get(iTW.word));
					IntTaggedWord iTS = new IntTaggedWord(s, iTW.tag);
					IntTaggedWord iS = new IntTaggedWord(s, UnknownWordModelTrainerConstants.nullTag);
					unSeenCounter.IncrementCount(iTS, weight);
					unSeenCounter.IncrementCount(iT, weight);
					unSeenCounter.IncrementCount(iS, weight);
					unSeenCounter.IncrementCount(i, weight);
				}
			}
		}

		public override IUnknownWordModel FinishTraining()
		{
			// make sure the unseen counter isn't empty!  If it is, put in
			// a uniform unseen over tags
			if (unSeenCounter.IsEmpty())
			{
				System.Console.Error.Printf("%s: WARNING: Unseen word counter is empty!", this.GetType().FullName);
				int numTags = tagIndex.Size();
				for (int tt = 0; tt < numTags; tt++)
				{
					if (!BoundaryTag.Equals(tagIndex.Get(tt)))
					{
						IntTaggedWord iT = new IntTaggedWord(UnknownWordModelTrainerConstants.nullWord, tt);
						IntTaggedWord i = UnknownWordModelTrainerConstants.NullItw;
						unSeenCounter.IncrementCount(iT);
						unSeenCounter.IncrementCount(i);
					}
				}
			}
			return model;
		}
	}
}
