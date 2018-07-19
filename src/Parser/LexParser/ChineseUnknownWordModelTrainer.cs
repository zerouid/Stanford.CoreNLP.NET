using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class ChineseUnknownWordModelTrainer : AbstractUnknownWordModelTrainer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ChineseUnknownWordModelTrainer));

		private ClassicCounter<IntTaggedWord> seenCounter;

		private ClassicCounter<IntTaggedWord> unSeenCounter;

		private IDictionary<ILabel, ClassicCounter<string>> c;

		private ClassicCounter<ILabel> tc;

		private bool useFirst;

		private bool useGT;

		private bool useUnicodeType;

		private IDictionary<ILabel, ClassicCounter<string>> tagHash;

		private ICollection<string> seenFirst;

		private double indexToStartUnkCounting;

		private UnknownGTTrainer unknownGTTrainer;

		private IntTaggedWord iTotal = new IntTaggedWord(UnknownWordModelTrainerConstants.nullWord, UnknownWordModelTrainerConstants.nullTag);

		private IUnknownWordModel model;

		// Records the number of times word/tag pair was seen in training data.
		// c has a map from tags as Label to a Counter from word
		// signatures to Strings; it is used to collect counts that will
		// initialize the probabilities in tagHash
		// tc record the marginal counts for each tag as an unknown.  It
		// should be the same as c's totalCount ??
		public override void InitializeTraining(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, double totalTrees)
		{
			base.InitializeTraining(op, lex, wordIndex, tagIndex, totalTrees);
			bool useGoodTuringUnknownWordModel = ChineseTreebankParserParams.DefaultUseGoodTurningUnknownWordModel;
			useFirst = true;
			useGT = (op.lexOptions.useUnknownWordSignatures == 0);
			if (lex is ChineseLexicon)
			{
				useGoodTuringUnknownWordModel = ((ChineseLexicon)lex).useGoodTuringUnknownWordModel;
			}
			else
			{
				if (op.tlpParams is ChineseTreebankParserParams)
				{
					useGoodTuringUnknownWordModel = ((ChineseTreebankParserParams)op.tlpParams).useGoodTuringUnknownWordModel;
				}
			}
			if (useGoodTuringUnknownWordModel)
			{
				this.useGT = true;
				this.useFirst = false;
			}
			this.useUnicodeType = op.lexOptions.useUnicodeType;
			if (useFirst)
			{
				log.Info("ChineseUWM: treating unknown word as the average of their equivalents by first-character identity. useUnicodeType: " + useUnicodeType);
			}
			if (useGT)
			{
				log.Info("ChineseUWM: using Good-Turing smoothing for unknown words.");
			}
			this.c = Generics.NewHashMap();
			this.tc = new ClassicCounter<ILabel>();
			this.unSeenCounter = new ClassicCounter<IntTaggedWord>();
			this.seenCounter = new ClassicCounter<IntTaggedWord>();
			this.seenFirst = Generics.NewHashSet();
			this.tagHash = Generics.NewHashMap();
			this.indexToStartUnkCounting = (totalTrees * op.trainOptions.fractionBeforeUnseenCounting);
			this.unknownGTTrainer = (useGT) ? new UnknownGTTrainer() : null;
			IDictionary<string, float> unknownGT = null;
			if (useGT)
			{
				unknownGT = unknownGTTrainer.unknownGT;
			}
			this.model = new ChineseUnknownWordModel(op, lex, wordIndex, tagIndex, unSeenCounter, tagHash, unknownGT, useGT, seenFirst);
		}

		/// <summary>Trains the first-character based unknown word model.</summary>
		/// <param name="tw">The word we are currently training on</param>
		/// <param name="loc">The position of that word</param>
		/// <param name="weight">The weight to give this word in terms of training</param>
		public override void Train(TaggedWord tw, int loc, double weight)
		{
			if (useGT)
			{
				unknownGTTrainer.Train(tw, weight);
			}
			string word = tw.Word();
			ILabel tagL = new Tag(tw.Tag());
			string first = Sharpen.Runtime.Substring(word, 0, 1);
			if (useUnicodeType)
			{
				char ch = word[0];
				int type = char.GetType(ch);
				if (type != char.OtherLetter)
				{
					// standard Chinese characters are of type "OTHER_LETTER"!!
					first = int.ToString(type);
				}
			}
			string tag = tw.Tag();
			if (!c.Contains(tagL))
			{
				c[tagL] = new ClassicCounter<string>();
			}
			c[tagL].IncrementCount(first, weight);
			tc.IncrementCount(tagL, weight);
			seenFirst.Add(first);
			IntTaggedWord iW = new IntTaggedWord(word, IntTaggedWord.Any, wordIndex, tagIndex);
			seenCounter.IncrementCount(iW, weight);
			if (treesRead > indexToStartUnkCounting)
			{
				// start doing this once some way through trees;
				// treesRead is 1 based counting
				if (seenCounter.GetCount(iW) < 2)
				{
					IntTaggedWord iT = new IntTaggedWord(IntTaggedWord.Any, tag, wordIndex, tagIndex);
					unSeenCounter.IncrementCount(iT, weight);
					unSeenCounter.IncrementCount(iTotal, weight);
				}
			}
		}

		public override IUnknownWordModel FinishTraining()
		{
			// Map<String,Float> unknownGT = null;
			if (useGT)
			{
				unknownGTTrainer.FinishTraining();
			}
			// unknownGT = unknownGTTrainer.unknownGT;
			foreach (ILabel tagLab in c.Keys)
			{
				// outer iteration is over tags as Labels
				ClassicCounter<string> wc = c[tagLab];
				// counts for words given a tag
				if (!tagHash.Contains(tagLab))
				{
					tagHash[tagLab] = new ClassicCounter<string>();
				}
				// the UNKNOWN first character is assumed to be seen once in
				// each tag
				// this is really sort of broken!  (why??)
				tc.IncrementCount(tagLab);
				wc.SetCount(UnknownWordModelTrainerConstants.unknown, 1.0);
				// inner iteration is over words  as strings
				foreach (string first in wc.KeySet())
				{
					double prob = Math.Log(((wc.GetCount(first))) / tc.GetCount(tagLab));
					tagHash[tagLab].SetCount(first, prob);
				}
			}
			//if (Test.verbose)
			//EncodingPrintWriter.out.println(tag + " rewrites as " + first + " first char with probability " + prob,encoding);
			return model;
		}
	}
}
