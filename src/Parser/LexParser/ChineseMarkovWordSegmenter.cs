using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// Performs word segmentation with a hierarchical markov model over POS
	/// and over characters given POS.
	/// </summary>
	/// <author>Galen Andrew</author>
	[System.Serializable]
	public class ChineseMarkovWordSegmenter : IWordSegmenter
	{
		private Distribution<string> initialPOSDist;

		private IDictionary<string, Distribution> markovPOSDists;

		private ChineseCharacterBasedLexicon lex;

		private ICollection<string> POSes;

		private readonly IIndex<string> wordIndex;

		private readonly IIndex<string> tagIndex;

		public ChineseMarkovWordSegmenter(ChineseCharacterBasedLexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			this.lex = lex;
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
		}

		public ChineseMarkovWordSegmenter(ChineseTreebankParserParams @params, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			lex = new ChineseCharacterBasedLexicon(@params, wordIndex, tagIndex);
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
		}

		[System.NonSerialized]
		private ClassicCounter<string> initial;

		[System.NonSerialized]
		private GeneralizedCounter ruleCounter;

		// Only used at training time
		public virtual void InitializeTraining(double numTrees)
		{
			lex.InitializeTraining(numTrees);
			this.initial = new ClassicCounter<string>();
			this.ruleCounter = new GeneralizedCounter(2);
		}

		public virtual void Train(ICollection<Tree> trees)
		{
			foreach (Tree tree in trees)
			{
				Train(tree);
			}
		}

		public virtual void Train(Tree tree)
		{
			Train(tree.TaggedYield());
		}

		public virtual void Train(IList<TaggedWord> sentence)
		{
			lex.Train(sentence, 1.0);
			string last = null;
			foreach (TaggedWord tagLabel in sentence)
			{
				string tag = tagLabel.Tag();
				tagIndex.Add(tag);
				if (last == null)
				{
					initial.IncrementCount(tag);
				}
				else
				{
					ruleCounter.IncrementCount2D(last, tag);
				}
				last = tag;
			}
		}

		public virtual void FinishTraining()
		{
			lex.FinishTraining();
			int numTags = tagIndex.Size();
			POSes = Generics.NewHashSet(tagIndex.ObjectsList());
			initialPOSDist = Distribution.LaplaceSmoothedDistribution(initial, numTags, 0.5);
			markovPOSDists = Generics.NewHashMap();
			ISet entries = ruleCounter.LowestLevelCounterEntrySet();
			foreach (object entry1 in entries)
			{
				DictionaryEntry entry = (DictionaryEntry)entry1;
				//      Map.Entry<List<String>, Counter> entry = (Map.Entry<List<String>, Counter>) iter.next();
				Distribution d = Distribution.LaplaceSmoothedDistribution((ClassicCounter)entry.Value, numTags, 0.5);
				markovPOSDists[((IList<string>)entry.Key)[0]] = d;
			}
		}

		public virtual IList<IHasWord> Segment(string s)
		{
			return SegmentWordsWithMarkov(s);
		}

		// CDM 2007: I wonder what this does differently from segmentWordsWithMarkov???
		private List<TaggedWord> BasicSegmentWords(string s)
		{
			// We don't want to accidentally register words that we don't know
			// about in the wordIndex, so we wrap it with a DeltaIndex
			DeltaIndex<string> deltaWordIndex = new DeltaIndex<string>(wordIndex);
			int length = s.Length;
			//    Set<String> POSes = (Set<String>) POSDistribution.keySet();  // 1.5
			// best score of span
			double[][] scores = new double[length][];
			// best (last index of) first word for this span
			int[][] splitBacktrace = new int[length][];
			// best tag for word over this span
			int[][] POSbacktrace = new int[length][];
			for (int i = 0; i < length; i++)
			{
				Arrays.Fill(scores[i], double.NegativeInfinity);
			}
			// first fill in word probabilities
			for (int diff = 1; diff <= 10; diff++)
			{
				for (int start = 0; start + diff <= length; start++)
				{
					int end = start + diff;
					StringBuilder wordBuf = new StringBuilder();
					for (int pos = start; pos < end; pos++)
					{
						wordBuf.Append(s[pos]);
					}
					string word = wordBuf.ToString();
					//        for (String tag : POSes) {  // 1.5
					foreach (string tag in POSes)
					{
						IntTaggedWord itw = new IntTaggedWord(word, tag, deltaWordIndex, tagIndex);
						double newScore = lex.Score(itw, 0, word, null) + Math.Log(lex.GetPOSDistribution().ProbabilityOf(tag));
						if (newScore > scores[start][end])
						{
							scores[start][end] = newScore;
							splitBacktrace[start][end] = end;
							POSbacktrace[start][end] = itw.Tag();
						}
					}
				}
			}
			// now fill in word combination probabilities
			for (int diff_1 = 2; diff_1 <= length; diff_1++)
			{
				for (int start = 0; start + diff_1 <= length; start++)
				{
					int end = start + diff_1;
					for (int split = start + 1; split < end && split - start <= 10; split++)
					{
						if (splitBacktrace[start][split] != split)
						{
							continue;
						}
						// only consider words on left
						double newScore = scores[start][split] + scores[split][end];
						if (newScore > scores[start][end])
						{
							scores[start][end] = newScore;
							splitBacktrace[start][end] = split;
						}
					}
				}
			}
			IList<TaggedWord> words = new List<TaggedWord>();
			int start_1 = 0;
			while (start_1 < length)
			{
				int end = splitBacktrace[start_1][length];
				StringBuilder wordBuf = new StringBuilder();
				for (int pos = start_1; pos < end; pos++)
				{
					wordBuf.Append(s[pos]);
				}
				string word = wordBuf.ToString();
				string tag = tagIndex.Get(POSbacktrace[start_1][end]);
				words.Add(new TaggedWord(word, tag));
				start_1 = end;
			}
			return new List<TaggedWord>(words);
		}

		/// <summary>Do max language model markov segmentation.</summary>
		/// <remarks>
		/// Do max language model markov segmentation.
		/// Note that this algorithm inherently tags words as it goes, but that
		/// we throw away the tags in the final result so that the segmented words
		/// are untagged.  (Note: for a couple of years till Aug 2007, a tagged
		/// result was returned, but this messed up the parser, because it could
		/// use no tagging but the given tagging, which often wasn't very good.
		/// Or in particular it was a subcategorized tagging which never worked
		/// with the current forceTags option which assumes that gold taggings are
		/// inherently basic taggings.)
		/// </remarks>
		/// <param name="s">A String to segment</param>
		/// <returns>The list of segmented words.</returns>
		private List<IHasWord> SegmentWordsWithMarkov(string s)
		{
			// We don't want to accidentally register words that we don't know
			// about in the wordIndex, so we wrap it with a DeltaIndex
			DeltaIndex<string> deltaWordIndex = new DeltaIndex<string>(wordIndex);
			int length = s.Length;
			//    Set<String> POSes = (Set<String>) POSDistribution.keySet();  // 1.5
			int numTags = POSes.Count;
			// score of span with initial word of this tag
			double[][][] scores = new double[length][][];
			// best (length of) first word for this span with this tag
			int[][][] splitBacktrace = new int[length][][];
			// best tag for second word over this span, if first is this tag
			int[][][] POSbacktrace = new int[length][][];
			for (int i = 0; i < length; i++)
			{
				for (int j = 0; j < length + 1; j++)
				{
					Arrays.Fill(scores[i][j], double.NegativeInfinity);
				}
			}
			// first fill in word probabilities
			for (int diff = 1; diff <= 10; diff++)
			{
				for (int start = 0; start + diff <= length; start++)
				{
					int end = start + diff;
					StringBuilder wordBuf = new StringBuilder();
					for (int pos = start; pos < end; pos++)
					{
						wordBuf.Append(s[pos]);
					}
					string word = wordBuf.ToString();
					foreach (string tag in POSes)
					{
						IntTaggedWord itw = new IntTaggedWord(word, tag, deltaWordIndex, tagIndex);
						double score = lex.Score(itw, 0, word, null);
						if (start == 0)
						{
							score += Math.Log(initialPOSDist.ProbabilityOf(tag));
						}
						scores[start][end][itw.Tag()] = score;
						splitBacktrace[start][end][itw.Tag()] = end;
					}
				}
			}
			// now fill in word combination probabilities
			for (int diff_1 = 2; diff_1 <= length; diff_1++)
			{
				for (int start = 0; start + diff_1 <= length; start++)
				{
					int end = start + diff_1;
					for (int split = start + 1; split < end && split - start <= 10; split++)
					{
						foreach (string tag in POSes)
						{
							int tagNum = tagIndex.AddToIndex(tag);
							if (splitBacktrace[start][split][tagNum] != split)
							{
								continue;
							}
							Distribution<string> rTagDist = markovPOSDists[tag];
							if (rTagDist == null)
							{
								continue;
							}
							// this happens with "*" POS
							foreach (string rTag in POSes)
							{
								int rTagNum = tagIndex.AddToIndex(rTag);
								double newScore = scores[start][split][tagNum] + scores[split][end][rTagNum] + Math.Log(rTagDist.ProbabilityOf(rTag));
								if (newScore > scores[start][end][tagNum])
								{
									scores[start][end][tagNum] = newScore;
									splitBacktrace[start][end][tagNum] = split;
									POSbacktrace[start][end][tagNum] = rTagNum;
								}
							}
						}
					}
				}
			}
			int nextPOS = ArrayMath.Argmax(scores[0][length]);
			List<IHasWord> words = new List<IHasWord>();
			int start_1 = 0;
			while (start_1 < length)
			{
				int split = splitBacktrace[start_1][length][nextPOS];
				StringBuilder wordBuf = new StringBuilder();
				for (int i_1 = start_1; i_1 < split; i_1++)
				{
					wordBuf.Append(s[i_1]);
				}
				string word = wordBuf.ToString();
				// String tag = tagIndex.get(nextPOS);
				// words.add(new TaggedWord(word, tag));
				words.Add(new Word(word));
				if (split < length)
				{
					nextPOS = POSbacktrace[start_1][length][nextPOS];
				}
				start_1 = split;
			}
			return words;
		}

		private Distribution<int> GetSegmentedWordLengthDistribution(Treebank tb)
		{
			// CharacterLevelTagExtender ext = new CharacterLevelTagExtender();
			ClassicCounter<int> c = new ClassicCounter<int>();
			foreach (Tree gold in tb)
			{
				StringBuilder goldChars = new StringBuilder();
				ArrayList goldYield = gold.Yield();
				foreach (object aGoldYield in goldYield)
				{
					Word word = (Word)aGoldYield;
					goldChars.Append(word);
				}
				IList<IHasWord> ourWords = Segment(goldChars.ToString());
				foreach (IHasWord ourWord in ourWords)
				{
					c.IncrementCount(int.Parse(ourWord.Word().Length));
				}
			}
			return Distribution.GetDistribution(c);
		}

		public virtual void LoadSegmenter(string filename)
		{
			throw new NotSupportedException();
		}

		private const long serialVersionUID = 1559606198270645508L;
	}
}
