using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class ArabicUnknownWordModelTrainer : AbstractUnknownWordModelTrainer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(BaseUnknownWordModelTrainer));

		internal ClassicCounter<IntTaggedWord> seenCounter;

		internal ClassicCounter<IntTaggedWord> unSeenCounter;

		internal double indexToStartUnkCounting;

		private const bool DocumentUnknowns = false;

		private const string UnknownWord = "UNK";

		private const string BoundaryTag = ".$$.";

		internal IUnknownWordModel model;

		// Records the number of times word/tag pair was seen in training data.
		// if UNK were a word, counts would merge
		// boundary tag -- assumed not a real tag
		public override void InitializeTraining(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, double totalTrees)
		{
			base.InitializeTraining(op, lex, wordIndex, tagIndex, totalTrees);
			this.totalTrees = totalTrees;
			indexToStartUnkCounting = (totalTrees * op.trainOptions.fractionBeforeUnseenCounting);
			seenCounter = new ClassicCounter<IntTaggedWord>(20000);
			unSeenCounter = new ClassicCounter<IntTaggedWord>(20000);
			model = new ArabicUnknownWordModel(op, lex, wordIndex, tagIndex, unSeenCounter);
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

		// else {
		public override IUnknownWordModel FinishTraining()
		{
			// make sure the unseen counter isn't empty!  If it is, put in
			// a uniform unseen over tags
			if (unSeenCounter.IsEmpty())
			{
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
			// index the possible tags for each word
			// numWords = wordIndex.size();
			// unknownWordIndex = wordIndex.indexOf(Lexicon.UNKNOWN_WORD, true);
			// initRulesWithWord();
			return model;
		}
	}
}
