using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class BaseUnknownWordModelTrainer : AbstractUnknownWordModelTrainer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(BaseUnknownWordModelTrainer));

		internal ClassicCounter<IntTaggedWord> seenCounter;

		internal ClassicCounter<ILabel> tc;

		internal IDictionary<ILabel, ClassicCounter<string>> c;

		internal ClassicCounter<IntTaggedWord> unSeenCounter;

		internal IDictionary<ILabel, ClassicCounter<string>> tagHash;

		internal ICollection<string> seenEnd;

		internal double indexToStartUnkCounting = 0;

		internal UnknownGTTrainer unknownGTTrainer;

		internal bool useEnd;

		internal bool useFirst;

		internal bool useFirstCap;

		internal bool useGT;

		internal IUnknownWordModel model;

		// Records the number of times word/tag pair was seen in training data.
		// Counts of each tag (stored as a Label) on unknown words.
		// tag (Label) --> signature --> count
		public override void InitializeTraining(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, double totalTrees)
		{
			base.InitializeTraining(op, lex, wordIndex, tagIndex, totalTrees);
			seenCounter = new ClassicCounter<IntTaggedWord>();
			unSeenCounter = new ClassicCounter<IntTaggedWord>();
			tagHash = Generics.NewHashMap();
			tc = new ClassicCounter<ILabel>();
			c = Generics.NewHashMap();
			seenEnd = Generics.NewHashSet();
			useEnd = (op.lexOptions.unknownSuffixSize > 0 && op.lexOptions.useUnknownWordSignatures > 0);
			useFirstCap = op.lexOptions.useUnknownWordSignatures > 0;
			useGT = (op.lexOptions.useUnknownWordSignatures == 0);
			useFirst = false;
			if (useFirst)
			{
				log.Info("Including first letter for unknown words.");
			}
			if (useFirstCap)
			{
				log.Info("Including whether first letter is capitalized for unknown words");
			}
			if (useEnd)
			{
				log.Info("Classing unknown word as the average of their equivalents by identity of last " + op.lexOptions.unknownSuffixSize + " letters.");
			}
			if (useGT)
			{
				log.Info("Using Good-Turing smoothing for unknown words.");
			}
			this.indexToStartUnkCounting = (totalTrees * op.trainOptions.fractionBeforeUnseenCounting);
			this.unknownGTTrainer = (useGT) ? new UnknownGTTrainer() : null;
			this.model = BuildUWM();
		}

		public override void Train(TaggedWord tw, int loc, double weight)
		{
			if (useGT)
			{
				unknownGTTrainer.Train(tw, weight);
			}
			// scan data
			string word = tw.Word();
			string subString = model.GetSignature(word, loc);
			ILabel tag = new Tag(tw.Tag());
			if (!c.Contains(tag))
			{
				c[tag] = new ClassicCounter<string>();
			}
			c[tag].IncrementCount(subString, weight);
			tc.IncrementCount(tag, weight);
			seenEnd.Add(subString);
			string tagStr = tw.Tag();
			IntTaggedWord iW = new IntTaggedWord(word, IntTaggedWord.Any, wordIndex, tagIndex);
			seenCounter.IncrementCount(iW, weight);
			if (treesRead > indexToStartUnkCounting)
			{
				// start doing this once some way through trees;
				// treesRead is 1 based counting
				if (seenCounter.GetCount(iW) < 2)
				{
					IntTaggedWord iT = new IntTaggedWord(IntTaggedWord.Any, tagStr, wordIndex, tagIndex);
					unSeenCounter.IncrementCount(iT, weight);
					unSeenCounter.IncrementCount(UnknownWordModelTrainerConstants.NullItw, weight);
				}
			}
		}

		public override IUnknownWordModel FinishTraining()
		{
			if (useGT)
			{
				unknownGTTrainer.FinishTraining();
			}
			foreach (KeyValuePair<ILabel, ClassicCounter<string>> entry in c)
			{
				/* outer iteration is over tags */
				ILabel key = entry.Key;
				ClassicCounter<string> wc = entry.Value;
				// counts for words given a tag
				if (!tagHash.Contains(key))
				{
					tagHash[key] = new ClassicCounter<string>();
				}
				/* the UNKNOWN sequence is assumed to be seen once in each tag */
				// This is sort of broken, but you can regard it as a Dirichlet prior.
				tc.IncrementCount(key);
				wc.SetCount(UnknownWordModelTrainerConstants.unknown, 1.0);
				/* inner iteration is over words */
				foreach (string end in wc.KeySet())
				{
					double prob = Math.Log((wc.GetCount(end)) / (tc.GetCount(key)));
					// p(sig|tag)
					tagHash[key].SetCount(end, prob);
				}
			}
			//if (Test.verbose)
			//EncodingPrintWriter.out.println(tag + " rewrites as " + end + " endchar with probability " + prob,encoding);
			return model;
		}

		protected internal virtual IUnknownWordModel BuildUWM()
		{
			IDictionary<string, float> unknownGT = null;
			if (useGT)
			{
				unknownGT = unknownGTTrainer.unknownGT;
			}
			return new BaseUnknownWordModel(op, lex, wordIndex, tagIndex, unSeenCounter, tagHash, unknownGT, seenEnd);
		}
	}
}
