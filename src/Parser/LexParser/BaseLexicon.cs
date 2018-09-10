using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;








namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>This is the default concrete instantiation of the Lexicon interface.</summary>
	/// <remarks>
	/// This is the default concrete instantiation of the Lexicon interface. It was
	/// originally built for Penn Treebank English.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Galen Andrew</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class BaseLexicon : ILexicon
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.BaseLexicon));

		protected internal IUnknownWordModel uwModel;

		protected internal readonly string uwModelTrainerClass;

		[System.NonSerialized]
		protected internal IUnknownWordModelTrainer uwModelTrainer;

		protected internal const bool DebugLexicon = false;

		protected internal const bool DebugLexiconScore = false;

		protected internal const int nullWord = -1;

		protected internal const short nullTag = -1;

		protected internal static readonly IntTaggedWord NullItw = new IntTaggedWord(nullWord, nullTag);

		protected internal readonly TrainOptions trainOptions;

		protected internal readonly TestOptions testOptions;

		protected internal readonly Options op;

		/// <summary>
		/// If a word has been seen more than this many times, then relative
		/// frequencies of tags are used for POS assignment; if not, they are smoothed
		/// with tag priors.
		/// </summary>
		protected internal int smoothInUnknownsThreshold;

		/// <summary>
		/// Have tags changeable based on statistics on word types having various
		/// taggings.
		/// </summary>
		protected internal bool smartMutation;

		protected internal readonly IIndex<string> wordIndex;

		protected internal readonly IIndex<string> tagIndex;

		/// <summary>An array of Lists of rules (IntTaggedWord), indexed by word.</summary>
		[System.NonSerialized]
		public IList<IntTaggedWord>[] rulesWithWord;

		/// <summary>Set of all tags as IntTaggedWord.</summary>
		/// <remarks>
		/// Set of all tags as IntTaggedWord. Alive in both train and runtime
		/// phases, but transient.
		/// </remarks>
		[System.NonSerialized]
		protected internal ICollection<IntTaggedWord> tags = Generics.NewHashSet();

		[System.NonSerialized]
		protected internal ICollection<IntTaggedWord> words = Generics.NewHashSet();

		/// <summary>Records the number of times word/tag pair was seen in training data.</summary>
		/// <remarks>
		/// Records the number of times word/tag pair was seen in training data.
		/// Includes word/tag pairs where one is a wildcard not a real word/tag.
		/// </remarks>
		public ClassicCounter<IntTaggedWord> seenCounter = new ClassicCounter<IntTaggedWord>();

		internal double[] smooth = new double[] { 1.0, 1.0 };

		[System.NonSerialized]
		internal double[][] m_TT;

		[System.NonSerialized]
		internal double[] m_T;

		protected internal bool flexiTag;

		protected internal bool useSignatureForKnownSmoothing;

		/// <summary>
		/// Only used when training, specifically when training on sentences
		/// that weren't part of annotated (e.g., markovized, etc.) data.
		/// </summary>
		private IDictionary<string, ICounter<string>> baseTagCounts = Generics.NewHashMap();

		public BaseLexicon(IIndex<string> wordIndex, IIndex<string> tagIndex)
			: this(new Options(), wordIndex, tagIndex)
		{
		}

		public BaseLexicon(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			// protected transient Set<IntTaggedWord> rules = new
			// HashSet<IntTaggedWord>();
			// When it existed, rules somehow held a few less things than rulesWithWord
			// I never figured out why [cdm, Dec 2004]
			// protected transient Set<IntTaggedWord> sigs=Generics.newHashSet();
			// these next two are used for smartMutation calculation
			// = null;
			// = null;
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
			flexiTag = op.lexOptions.flexiTag;
			useSignatureForKnownSmoothing = op.lexOptions.useSignatureForKnownSmoothing;
			this.smoothInUnknownsThreshold = op.lexOptions.smoothInUnknownsThreshold;
			this.smartMutation = op.lexOptions.smartMutation;
			this.trainOptions = op.trainOptions;
			this.testOptions = op.testOptions;
			this.op = op;
			// Construct UnknownWordModel by reflection -- a right pain
			// Lexicons and UnknownWordModels aren't very well encapsulated
			// from each other!
			if (op.lexOptions.uwModelTrainer == null)
			{
				this.uwModelTrainerClass = "edu.stanford.nlp.parser.lexparser.BaseUnknownWordModelTrainer";
			}
			else
			{
				this.uwModelTrainerClass = op.lexOptions.uwModelTrainer;
			}
		}

		/// <summary>Checks whether a word is in the lexicon.</summary>
		/// <remarks>
		/// Checks whether a word is in the lexicon. This version will compile the
		/// lexicon into the rulesWithWord array, if that hasn't already happened
		/// </remarks>
		/// <param name="word">The word as an int index to an Index</param>
		/// <returns>Whether the word is in the lexicon</returns>
		public virtual bool IsKnown(int word)
		{
			return (word < rulesWithWord.Length && word >= 0 && !rulesWithWord[word].IsEmpty());
		}

		/// <summary>Checks whether a word is in the lexicon.</summary>
		/// <remarks>
		/// Checks whether a word is in the lexicon. This version works even while
		/// compiling lexicon with current counters (rather than using the compiled
		/// rulesWithWord array).
		/// TODO: The previous version would insert rules into the
		/// wordNumberer.  Is that the desired behavior?  Why not test in
		/// some way that doesn't affect the index?  For example, start by
		/// testing wordIndex.contains(word).
		/// </remarks>
		/// <param name="word">The word as a String</param>
		/// <returns>Whether the word is in the lexicon</returns>
		public virtual bool IsKnown(string word)
		{
			if (!wordIndex.Contains(word))
			{
				return false;
			}
			IntTaggedWord iW = new IntTaggedWord(wordIndex.IndexOf(word), nullTag);
			return seenCounter.GetCount(iW) > 0.0;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ICollection<string> TagSet(Func<string, string> basicCategoryFunction)
		{
			ICollection<string> tagSet = new HashSet<string>();
			foreach (string tag in tagIndex.ObjectsList())
			{
				tagSet.Add(basicCategoryFunction.Apply(tag));
			}
			return tagSet;
		}

		/// <summary>Returns the possible POS taggings for a word.</summary>
		/// <param name="word">The word, represented as an integer in wordIndex</param>
		/// <param name="loc">
		/// The position of the word in the sentence (counting from 0).
		/// <i>Implementation note: The BaseLexicon class doesn't actually
		/// make use of this position information.</i>
		/// </param>
		/// <returns>
		/// An Iterator over a List ofIntTaggedWords, which pair the word with
		/// possible taggings as integer pairs. (Each can be thought of as a
		/// <code>tag -&gt; word<code> rule.)
		/// </returns>
		public virtual IEnumerator<IntTaggedWord> RuleIteratorByWord(string word, int loc)
		{
			return RuleIteratorByWord(wordIndex.AddToIndex(word), loc, null);
		}

		/// <summary>Generate the possible taggings for a word at a sentence position.</summary>
		/// <remarks>
		/// Generate the possible taggings for a word at a sentence position.
		/// This may either be based on a strict lexicon or an expanded generous
		/// set of possible taggings. <p>
		/// <i>Implementation note:</i> Expanded sets of possible taggings are
		/// calculated dynamically at runtime, so as to reduce the memory used by
		/// the lexicon (a space/time tradeoff).
		/// </remarks>
		/// <param name="word">The word (as an int)</param>
		/// <param name="loc">Its index in the sentence (usually only relevant for unknown words)</param>
		/// <returns>A list of possible taggings</returns>
		public virtual IEnumerator<IntTaggedWord> RuleIteratorByWord(int word, int loc, string featureSpec)
		{
			// if (rulesWithWord == null) { // tested in isKnown already
			// initRulesWithWord();
			// }
			IList<IntTaggedWord> wordTaggings;
			if (IsKnown(word))
			{
				if (!flexiTag)
				{
					// Strict lexical tagging for seen items
					wordTaggings = rulesWithWord[word];
				}
				else
				{
					/* Allow all tags with same basicCategory */
					/* Allow all scored taggings, unless very common */
					IntTaggedWord iW = new IntTaggedWord(word, nullTag);
					if (seenCounter.GetCount(iW) > smoothInUnknownsThreshold)
					{
						return rulesWithWord[word].GetEnumerator();
					}
					else
					{
						// give it flexible tagging not just lexicon
						wordTaggings = new List<IntTaggedWord>(40);
						foreach (IntTaggedWord iTW2 in tags)
						{
							IntTaggedWord iTW = new IntTaggedWord(word, iTW2.tag);
							if (Score(iTW, loc, wordIndex.Get(word), null) > float.NegativeInfinity)
							{
								wordTaggings.Add(iTW);
							}
						}
					}
				}
			}
			else
			{
				// we copy list so we can insert correct word in each item
				wordTaggings = new List<IntTaggedWord>(40);
				foreach (IntTaggedWord iTW in rulesWithWord[wordIndex.IndexOf(LexiconConstants.UnknownWord)])
				{
					wordTaggings.Add(new IntTaggedWord(word, iTW.tag));
				}
			}
			return wordTaggings.GetEnumerator();
		}

		public virtual IEnumerator<IntTaggedWord> RuleIteratorByWord(string word, int loc, string featureSpec)
		{
			return RuleIteratorByWord(wordIndex.AddToIndex(word), loc, featureSpec);
		}

		protected internal virtual void InitRulesWithWord()
		{
			if (testOptions.verbose || DebugLexicon)
			{
				log.Info("Initializing lexicon scores ... ");
			}
			// int numWords = words.size()+sigs.size()+1;
			int unkWord = wordIndex.AddToIndex(LexiconConstants.UnknownWord);
			int numWords = wordIndex.Size();
			rulesWithWord = new IList[numWords];
			for (int w = 0; w < numWords; w++)
			{
				rulesWithWord[w] = new List<IntTaggedWord>(1);
			}
			// most have 1 or 2
			// items in them
			// for (Iterator ruleI = rules.iterator(); ruleI.hasNext();) {
			tags = Generics.NewHashSet();
			foreach (IntTaggedWord iTW in seenCounter.KeySet())
			{
				if (iTW.Word() == nullWord && iTW.Tag() != nullTag)
				{
					tags.Add(iTW);
				}
			}
			// tags for unknown words
			foreach (IntTaggedWord iT in tags)
			{
				double types = uwModel.UnSeenCounter().GetCount(iT);
				if (types > trainOptions.openClassTypesThreshold)
				{
					// Number of types before it's treated as open class
					IntTaggedWord iTW_1 = new IntTaggedWord(unkWord, iT.tag);
					rulesWithWord[iTW_1.word].Add(iTW_1);
				}
			}
			if (testOptions.verbose || DebugLexicon)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("The ").Append(rulesWithWord[unkWord].Count).Append(" open class tags are: [");
				foreach (IntTaggedWord item in rulesWithWord[unkWord])
				{
					sb.Append(' ').Append(tagIndex.Get(item.Tag()));
				}
				sb.Append(" ]");
				log.Info(sb.ToString());
			}
			foreach (IntTaggedWord iTW_2 in seenCounter.KeySet())
			{
				if (iTW_2.Tag() != nullTag && iTW_2.Word() != nullWord)
				{
					rulesWithWord[iTW_2.word].Add(iTW_2);
				}
			}
		}

		protected internal virtual IList<IntTaggedWord> TreeToEvents(Tree tree)
		{
			IList<TaggedWord> taggedWords = tree.TaggedYield();
			return ListToEvents(taggedWords);
		}

		protected internal virtual IList<IntTaggedWord> ListToEvents(IList<TaggedWord> taggedWords)
		{
			IList<IntTaggedWord> itwList = new List<IntTaggedWord>();
			foreach (TaggedWord tw in taggedWords)
			{
				IntTaggedWord iTW = new IntTaggedWord(tw.Word(), tw.Tag(), wordIndex, tagIndex);
				itwList.Add(iTW);
			}
			return itwList;
		}

		/// <summary>Not yet implemented.</summary>
		public virtual void AddAll(IList<TaggedWord> tagWords)
		{
			AddAll(tagWords, 1.0);
		}

		/// <summary>Not yet implemented.</summary>
		public virtual void AddAll(IList<TaggedWord> taggedWords, double weight)
		{
			IList<IntTaggedWord> tagWords = ListToEvents(taggedWords);
		}

		/// <summary>Not yet implemented.</summary>
		public virtual void TrainWithExpansion(ICollection<TaggedWord> taggedWords)
		{
		}

		public virtual void InitializeTraining(double numTrees)
		{
			this.uwModelTrainer = ReflectionLoading.LoadByReflection(uwModelTrainerClass);
			uwModelTrainer.InitializeTraining(op, this, wordIndex, tagIndex, numTrees);
		}

		/// <summary>Trains this lexicon on the Collection of trees.</summary>
		public virtual void Train(ICollection<Tree> trees)
		{
			Train(trees, 1.0);
		}

		/// <summary>Trains this lexicon on the Collection of trees.</summary>
		/// <remarks>
		/// Trains this lexicon on the Collection of trees.
		/// Also trains the unknown word model pointed to by this lexicon.
		/// </remarks>
		public virtual void Train(ICollection<Tree> trees, double weight)
		{
			// scan data
			foreach (Tree tree in trees)
			{
				Train(tree, weight);
			}
		}

		public virtual void Train(Tree tree, double weight)
		{
			Train(tree.TaggedYield(), weight);
		}

		public void Train(IList<TaggedWord> sentence, double weight)
		{
			uwModelTrainer.IncrementTreesRead(weight);
			int loc = 0;
			foreach (TaggedWord tw in sentence)
			{
				Train(tw, loc, weight);
				++loc;
			}
		}

		public void IncrementTreesRead(double weight)
		{
			uwModelTrainer.IncrementTreesRead(weight);
		}

		public void TrainUnannotated(IList<TaggedWord> sentence, double weight)
		{
			uwModelTrainer.IncrementTreesRead(weight);
			int loc = 0;
			foreach (TaggedWord tw in sentence)
			{
				string baseTag = op.Langpack().BasicCategory(tw.Tag());
				ICounter<string> counts = baseTagCounts[baseTag];
				if (counts == null)
				{
					++loc;
					continue;
				}
				double totalCount = counts.TotalCount();
				if (totalCount == 0)
				{
					++loc;
					continue;
				}
				foreach (string tag in counts.KeySet())
				{
					TaggedWord newTW = new TaggedWord(tw.Word(), tag);
					Train(newTW, loc, weight * counts.GetCount(tag) / totalCount);
				}
				++loc;
			}
		}

		public virtual void Train(TaggedWord tw, int loc, double weight)
		{
			uwModelTrainer.Train(tw, loc, weight);
			IntTaggedWord iTW = new IntTaggedWord(tw.Word(), tw.Tag(), wordIndex, tagIndex);
			seenCounter.IncrementCount(iTW, weight);
			IntTaggedWord iT = new IntTaggedWord(nullWord, iTW.tag);
			seenCounter.IncrementCount(iT, weight);
			IntTaggedWord iW = new IntTaggedWord(iTW.word, nullTag);
			seenCounter.IncrementCount(iW, weight);
			IntTaggedWord i = new IntTaggedWord(nullWord, nullTag);
			seenCounter.IncrementCount(i, weight);
			// rules.add(iTW);
			tags.Add(iT);
			words.Add(iW);
			string tag = tw.Tag();
			string baseTag = op.Langpack().BasicCategory(tag);
			ICounter<string> counts = baseTagCounts[baseTag];
			if (counts == null)
			{
				counts = new ClassicCounter<string>();
				baseTagCounts[baseTag] = counts;
			}
			counts.IncrementCount(tag, weight);
		}

		public virtual void FinishTraining()
		{
			uwModel = uwModelTrainer.FinishTraining();
			Tune();
			// index the possible tags for each word
			InitRulesWithWord();
		}

		/// <summary>Adds the tagging with count to the data structures in this Lexicon.</summary>
		protected internal virtual void AddTagging(bool seen, IntTaggedWord itw, double count)
		{
			if (seen)
			{
				seenCounter.IncrementCount(itw, count);
				if (itw.Tag() == nullTag)
				{
					words.Add(itw);
				}
				else
				{
					if (itw.Word() == nullWord)
					{
						tags.Add(itw);
					}
				}
			}
			else
			{
				// rules.add(itw);
				uwModel.AddTagging(seen, itw, count);
			}
		}

		// if (itw.tag() == nullTag) {
		// sigs.add(itw);
		// }
		/// <summary>
		/// This records how likely it is for a word with one tag to also have another
		/// tag.
		/// </summary>
		/// <remarks>
		/// This records how likely it is for a word with one tag to also have another
		/// tag. This won't work after serialization/deserialization, but that is how
		/// it is currently called....
		/// </remarks>
		internal virtual void BuildPT_T()
		{
			int numTags = tagIndex.Size();
			m_TT = new double[numTags][];
			m_T = new double[numTags];
			double[] tmp = new double[numTags];
			foreach (IntTaggedWord word in words)
			{
				double tot = 0.0;
				for (int t = 0; t < numTags; t++)
				{
					IntTaggedWord iTW = new IntTaggedWord(word.word, t);
					tmp[t] = seenCounter.GetCount(iTW);
					tot += tmp[t];
				}
				if (tot < 10)
				{
					continue;
				}
				for (int t_1 = 0; t_1 < numTags; t_1++)
				{
					for (int t2 = 0; t2 < numTags; t2++)
					{
						if (tmp[t2] > 0.0)
						{
							double c = tmp[t_1] / tot;
							m_T[t_1] += c;
							m_TT[t2][t_1] += c;
						}
					}
				}
			}
		}

		/// <summary>
		/// Get the score of this word with this tag (as an IntTaggedWord) at this
		/// location.
		/// </summary>
		/// <remarks>
		/// Get the score of this word with this tag (as an IntTaggedWord) at this
		/// location. (Presumably an estimate of P(word | tag).)
		/// <p>
		/// <i>Implementation documentation:</i>
		/// Seen:
		/// c_W = count(W)      c_TW = count(T,W)
		/// c_T = count(T)      c_Tunseen = count(T) among new words in 2nd half
		/// total = count(seen words)   totalUnseen = count("unseen" words)
		/// p_T_U = Pmle(T|"unseen")
		/// pb_T_W = P(T|W). If (c_W &gt; smoothInUnknownsThreshold) = c_TW/c_W
		/// Else (if not smart mutation) pb_T_W = bayes prior smooth[1] with p_T_U
		/// p_T= Pmle(T)          p_W = Pmle(W)
		/// pb_W_T = log(pb_T_W * p_W / p_T) [Bayes rule]
		/// Note that this doesn't really properly reserve mass to unknowns.
		/// Unseen:
		/// c_TS = count(T,Sig|Unseen)      c_S = count(Sig)   c_T = count(T|Unseen)
		/// c_U = totalUnseen above
		/// p_T_U = Pmle(T|Unseen)
		/// pb_T_S = Bayes smooth of Pmle(T|S) with P(T|Unseen) [smooth[0]]
		/// pb_W_T = log(P(W|T)) inverted
		/// </remarks>
		/// <param name="iTW">An IntTaggedWord pairing a word and POS tag</param>
		/// <param name="loc">
		/// The position in the sentence. <i>In the default implementation
		/// this is used only for unknown words to change their probability
		/// distribution when sentence initial</i>
		/// </param>
		/// <returns>A float score, usually, log P(word|tag)</returns>
		public virtual float Score(IntTaggedWord iTW, int loc, string word, string featureSpec)
		{
			// both actual
			double c_TW = seenCounter.GetCount(iTW);
			// double x_TW = xferCounter.getCount(iTW);
			IntTaggedWord temp = new IntTaggedWord(iTW.word, nullTag);
			// word counts
			double c_W = seenCounter.GetCount(temp);
			// double x_W = xferCounter.getCount(temp);
			// totals
			double total = seenCounter.GetCount(NullItw);
			double totalUnseen = uwModel.UnSeenCounter().GetCount(NullItw);
			temp = new IntTaggedWord(nullWord, iTW.tag);
			// tag counts
			double c_T = seenCounter.GetCount(temp);
			double c_Tunseen = uwModel.UnSeenCounter().GetCount(temp);
			double pb_W_T;
			// always set below
			// dump info about last word
			// the 2nd conjunct in test above handles older serialized files
			bool seen = (c_W > 0.0);
			if (seen)
			{
				// known word model for P(T|W)
				// c_TW = Math.sqrt(c_TW); [cdm: funny math scaling? dunno who played with this]
				// c_TW += 0.5;
				double p_T_U;
				if (useSignatureForKnownSmoothing)
				{
					// only works for English currently
					p_T_U = GetUnknownWordModel().ScoreProbTagGivenWordSignature(iTW, loc, smooth[0], word);
				}
				else
				{
					p_T_U = c_Tunseen / totalUnseen;
				}
				double pb_T_W;
				// always set below
				if (c_W > smoothInUnknownsThreshold && c_TW > 0.0 && c_W > 0.0)
				{
					// we've seen the word enough times to have confidence in its tagging
					pb_T_W = c_TW / c_W;
				}
				else
				{
					// we haven't seen the word enough times to have confidence in its
					// tagging
					if (smartMutation)
					{
						int numTags = tagIndex.Size();
						if (m_TT == null || numTags != m_T.Length)
						{
							BuildPT_T();
						}
						p_T_U *= 0.1;
						// System.out.println("Checking "+iTW);
						for (int t = 0; t < numTags; t++)
						{
							IntTaggedWord iTW2 = new IntTaggedWord(iTW.word, t);
							double p_T_W2 = seenCounter.GetCount(iTW2) / c_W;
							if (p_T_W2 > 0)
							{
								// System.out.println(" Observation of "+tagIndex.get(t)+"
								// ("+seenCounter.getCount(iTW2)+") mutated to
								// "+tagIndex.get(iTW.tag)+" at rate
								// "+(m_TT[tag][t]/m_T[t]));
								p_T_U += p_T_W2 * m_TT[iTW.tag][t] / m_T[t] * 0.9;
							}
						}
					}
					// double pb_T_W = (c_TW+smooth[1]*x_TW)/(c_W+smooth[1]*x_W);
					pb_T_W = (c_TW + smooth[1] * p_T_U) / (c_W + smooth[1]);
				}
				double p_T = (c_T / total);
				double p_W = (c_W / total);
				pb_W_T = Math.Log(pb_T_W * p_W / p_T);
			}
			else
			{
				// debugProbs.append("\n" + "smartMutation=" + smartMutation + "
				// smoothInUnknownsThreshold=" + smoothInUnknownsThreshold + "
				// smooth0=" + smooth[0] + "smooth1=" + smooth[1] + " p_T_U=" + p_T_U
				// + " c_W=" + c_W);
				// end if (DEBUG_LEXICON)
				// when unseen
				if (loc >= 0)
				{
					pb_W_T = GetUnknownWordModel().Score(iTW, loc, c_T, total, smooth[0], word);
				}
				else
				{
					// For negative we now do a weighted average for the dependency grammar :-)
					double pb_W0_T = GetUnknownWordModel().Score(iTW, 0, c_T, total, smooth[0], word);
					double pb_W1_T = GetUnknownWordModel().Score(iTW, 1, c_T, total, smooth[0], word);
					pb_W_T = Math.Log((Math.Exp(pb_W0_T) + 2 * Math.Exp(pb_W1_T)) / 3);
				}
			}
			string tag = tagIndex.Get(iTW.Tag());
			// Categorical cutoff if score is too low
			if (pb_W_T > -100.0)
			{
				return (float)pb_W_T;
			}
			return float.NegativeInfinity;
		}

		[System.NonSerialized]
		private int debugLastWord = -1;

		[System.NonSerialized]
		private int debugLoc = -1;

		[System.NonSerialized]
		private StringBuilder debugProbs;

		[System.NonSerialized]
		private StringBuilder debugNoProbs;

		[System.NonSerialized]
		private string debugPrefix;

		// end score()
		/// <summary>TODO: this used to actually score things based on the original trees</summary>
		public void Tune()
		{
			double bestScore = double.NegativeInfinity;
			double[] bestSmooth = new double[] { 0.0, 0.0 };
			for (smooth[0] = 1; smooth[0] <= 1; smooth[0] *= 2.0)
			{
				// 64
				for (smooth[1] = 0.2; smooth[1] <= 0.2; smooth[1] *= 2.0)
				{
					// 3
					// for (smooth[0]=0.5; smooth[0]<=64; smooth[0] *= 2.0) {//64
					// for (smooth[1]=0.1; smooth[1]<=12.8; smooth[1] *= 2.0) {//3
					double score = 0.0;
					// score = scoreAll(trees);
					if (testOptions.verbose)
					{
						log.Info("Tuning lexicon: s0 " + smooth[0] + " s1 " + smooth[1] + " is " + score);
					}
					if (score > bestScore)
					{
						System.Array.Copy(smooth, 0, bestSmooth, 0, smooth.Length);
						bestScore = score;
					}
				}
			}
			System.Array.Copy(bestSmooth, 0, smooth, 0, bestSmooth.Length);
			if (smartMutation)
			{
				smooth[0] = 8.0;
				// smooth[1] = 1.6;
				// smooth[0] = 0.5;
				smooth[1] = 0.1;
			}
			if (testOptions.unseenSmooth > 0.0)
			{
				smooth[0] = testOptions.unseenSmooth;
			}
			if (testOptions.verbose)
			{
				log.Info("Tuning selected smoothUnseen " + smooth[0] + " smoothSeen " + smooth[1] + " at " + bestScore);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream ois)
		{
			ois.DefaultReadObject();
			// Reinitialize the transient objects.  This must be done here
			// rather than lazily so that there is no race condition to
			// reinitialize them later.
			InitRulesWithWord();
		}

		/// <summary>
		/// Populates data in this Lexicon from the character stream given by the
		/// Reader r.
		/// </summary>
		/// <remarks>
		/// Populates data in this Lexicon from the character stream given by the
		/// Reader r.
		/// TODO: this doesn't appear to correctly read in the
		/// UnknownWordModel in the case of a model more complicated than the
		/// unSeenCounter
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadData(BufferedReader @in)
		{
			string Seen = "SEEN";
			string line;
			int lineNum = 1;
			// all lines have one tagging with raw count per line
			line = @in.ReadLine();
			Pattern p = Pattern.Compile("^smooth\\[([0-9])\\] = (.*)$");
			while (line != null && line.Length > 0)
			{
				try
				{
					Matcher m = p.Matcher(line);
					if (m.Matches())
					{
						int i = System.Convert.ToInt32(m.Group(1));
						smooth[i] = double.Parse(m.Group(2));
					}
					else
					{
						// split on spaces, quote with doublequote, and escape with backslash
						string[] fields = StringUtils.SplitOnCharWithQuoting(line, ' ', '\"', '\\');
						// System.out.println("fields:\n" + fields[0] + "\n" + fields[1] +
						// "\n" + fields[2] + "\n" + fields[3] + "\n" + fields[4]);
						bool seen = fields[3].Equals(Seen);
						AddTagging(seen, new IntTaggedWord(fields[2], fields[0], wordIndex, tagIndex), double.Parse(fields[4]));
					}
				}
				catch (Exception e)
				{
					throw new IOException("Error on line " + lineNum + ": " + line, e);
				}
				lineNum++;
				line = @in.ReadLine();
			}
			InitRulesWithWord();
		}

		/// <summary>Writes out data from this Object to the Writer w.</summary>
		/// <remarks>
		/// Writes out data from this Object to the Writer w. Rules are separated by
		/// newline, and rule elements are delimited by \t.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public virtual void WriteData(TextWriter w)
		{
			PrintWriter @out = new PrintWriter(w);
			foreach (IntTaggedWord itw in seenCounter.KeySet())
			{
				@out.Println(itw.ToLexicalEntry(wordIndex, tagIndex) + " SEEN " + seenCounter.GetCount(itw));
			}
			foreach (IntTaggedWord itw_1 in GetUnknownWordModel().UnSeenCounter().KeySet())
			{
				@out.Println(itw_1.ToLexicalEntry(wordIndex, tagIndex) + " UNSEEN " + GetUnknownWordModel().UnSeenCounter().GetCount(itw_1));
			}
			for (int i = 0; i < smooth.Length; i++)
			{
				@out.Println("smooth[" + i + "] = " + smooth[i]);
			}
			@out.Flush();
		}

		/// <summary>Returns the number of rules (tag rewrites as word) in the Lexicon.</summary>
		/// <remarks>
		/// Returns the number of rules (tag rewrites as word) in the Lexicon.
		/// This method assumes that the lexicon has been initialized.
		/// </remarks>
		public virtual int NumRules()
		{
			int accumulated = 0;
			foreach (IList<IntTaggedWord> lis in rulesWithWord)
			{
				accumulated += lis.Count;
			}
			return accumulated;
		}

		private const int StatsBins = 15;

		protected internal static void ExamineIntersection(ICollection<string> s1, ICollection<string> s2)
		{
			ICollection<string> knownTypes = Generics.NewHashSet(s1);
			knownTypes.RetainAll(s2);
			if (knownTypes.Count != 0)
			{
				System.Console.Error.Printf("|intersect|: %d%n", knownTypes.Count);
				foreach (string word in knownTypes)
				{
					log.Info(word + " ");
				}
				log.Info();
			}
		}

		/// <summary>Print some statistics about this lexicon.</summary>
		public virtual void PrintLexStats()
		{
			System.Console.Out.WriteLine("BaseLexicon statistics");
			System.Console.Out.WriteLine("unknownLevel is " + GetUnknownWordModel().GetUnknownLevel());
			// System.out.println("Rules size: " + rules.size());
			System.Console.Out.WriteLine("Sum of rulesWithWord: " + NumRules());
			System.Console.Out.WriteLine("Tags size: " + tags.Count);
			int wsize = words.Count;
			System.Console.Out.WriteLine("Words size: " + wsize);
			// System.out.println("Unseen Sigs size: " + sigs.size() +
			// " [number of unknown equivalence classes]");
			System.Console.Out.WriteLine("rulesWithWord length: " + rulesWithWord.Length + " [should be sum of words + unknown sigs]");
			int[] lengths = new int[StatsBins];
			List<string>[] wArr = new ArrayList[StatsBins];
			for (int j = 0; j < StatsBins; j++)
			{
				wArr[j] = new List<string>();
			}
			for (int i = 0; i < rulesWithWord.Length; i++)
			{
				int num = rulesWithWord[i].Count;
				if (num > StatsBins - 1)
				{
					num = StatsBins - 1;
				}
				lengths[num]++;
				if (wsize <= 20 || num >= StatsBins / 2)
				{
					wArr[num].Add(wordIndex.Get(i));
				}
			}
			System.Console.Out.WriteLine("Stats on how many taggings for how many words");
			for (int j_1 = 0; j_1 < StatsBins; j_1++)
			{
				System.Console.Out.Write(j_1 + " taggings: " + lengths[j_1] + " words ");
				if (wsize <= 20 || j_1 >= StatsBins / 2)
				{
					System.Console.Out.Write(wArr[j_1]);
				}
				System.Console.Out.WriteLine();
			}
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(0);
			System.Console.Out.WriteLine("Unseen counter: " + Counters.ToString(uwModel.UnSeenCounter(), nf));
			if (wsize < 50 && tags.Count < 10)
			{
				nf.SetMaximumFractionDigits(3);
				StringWriter sw = new StringWriter();
				PrintWriter pw = new PrintWriter(sw);
				pw.Println("Tagging probabilities log P(word|tag)");
				for (int t = 0; t < tags.Count; t++)
				{
					pw.Print('\t');
					pw.Print(tagIndex.Get(t));
				}
				pw.Println();
				for (int w = 0; w < wsize; w++)
				{
					pw.Print(wordIndex.Get(w));
					pw.Print('\t');
					for (int t_1 = 0; t_1 < tags.Count; t_1++)
					{
						IntTaggedWord iTW = new IntTaggedWord(w, t_1);
						pw.Print(nf.Format(Score(iTW, 1, wordIndex.Get(w), null)));
						if (t_1 == tags.Count - 1)
						{
							pw.Println();
						}
						else
						{
							pw.Print('\t');
						}
					}
				}
				pw.Close();
				System.Console.Out.WriteLine(sw.ToString());
			}
		}

		/// <summary>
		/// Evaluates how many words (= terminals) in a collection of trees are
		/// covered by the lexicon.
		/// </summary>
		/// <remarks>
		/// Evaluates how many words (= terminals) in a collection of trees are
		/// covered by the lexicon. First arg is the collection of trees; second
		/// through fourth args get the results. Currently unused; this probably
		/// only works if train and test at same time so tags and words variables
		/// are initialized.
		/// </remarks>
		public virtual double EvaluateCoverage(ICollection<Tree> trees, ICollection<string> missingWords, ICollection<string> missingTags, ICollection<IntTaggedWord> missingTW)
		{
			IList<IntTaggedWord> iTW1 = new List<IntTaggedWord>();
			foreach (Tree t in trees)
			{
				Sharpen.Collections.AddAll(iTW1, TreeToEvents(t));
			}
			int total = 0;
			int unseen = 0;
			foreach (IntTaggedWord itw in iTW1)
			{
				total++;
				if (!words.Contains(new IntTaggedWord(itw.Word(), nullTag)))
				{
					missingWords.Add(wordIndex.Get(itw.Word()));
				}
				if (!tags.Contains(new IntTaggedWord(nullWord, itw.Tag())))
				{
					missingTags.Add(tagIndex.Get(itw.Tag()));
				}
				// if (!rules.contains(itw)) {
				if (seenCounter.GetCount(itw) == 0.0)
				{
					unseen++;
					missingTW.Add(itw);
				}
			}
			return (double)unseen / total;
		}

		internal int[] tagsToBaseTags = null;

		public virtual int GetBaseTag(int tag, ITreebankLanguagePack tlp)
		{
			if (tagsToBaseTags == null)
			{
				PopulateTagsToBaseTags(tlp);
			}
			return tagsToBaseTags[tag];
		}

		private void PopulateTagsToBaseTags(ITreebankLanguagePack tlp)
		{
			int total = tagIndex.Size();
			tagsToBaseTags = new int[total];
			for (int i = 0; i < total; i++)
			{
				string tag = tagIndex.Get(i);
				string baseTag = tlp.BasicCategory(tag);
				int j = tagIndex.AddToIndex(baseTag);
				tagsToBaseTags[i] = j;
			}
		}

		/// <summary>
		/// Provides some testing and opportunities for exploration of the
		/// probabilities of a BaseLexicon.
		/// </summary>
		/// <remarks>
		/// Provides some testing and opportunities for exploration of the
		/// probabilities of a BaseLexicon.  What's here currently probably
		/// only works for the English Penn Treeebank, as it uses default
		/// constructors.  Of the words given to test on,
		/// the first is treated as sentence initial, and the rest as not
		/// sentence initial.
		/// </remarks>
		/// <param name="args">
		/// The command line arguments:
		/// java BaseLexicon treebankPath fileRange unknownWordModel words
		/// </param>
		public static void Main(string[] args)
		{
			if (args.Length < 3)
			{
				log.Info("java BaseLexicon treebankPath fileRange unknownWordModel words*");
				return;
			}
			System.Console.Out.Write("Training BaseLexicon from " + args[0] + ' ' + args[1] + " ... ");
			Treebank tb = new DiskTreebank();
			tb.LoadPath(args[0], new NumberRangesFileFilter(args[1], true));
			// TODO: change this interface so the lexicon creates its own indices?
			IIndex<string> wordIndex = new HashIndex<string>();
			IIndex<string> tagIndex = new HashIndex<string>();
			Options op = new Options();
			op.lexOptions.useUnknownWordSignatures = System.Convert.ToInt32(args[2]);
			Edu.Stanford.Nlp.Parser.Lexparser.BaseLexicon lex = new Edu.Stanford.Nlp.Parser.Lexparser.BaseLexicon(op, wordIndex, tagIndex);
			lex.InitializeTraining(tb.Count);
			lex.Train(tb);
			lex.FinishTraining();
			System.Console.Out.WriteLine("done.");
			System.Console.Out.WriteLine();
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(4);
			IList<string> impos = new List<string>();
			for (int i = 3; i < args.Length; i++)
			{
				if (lex.IsKnown(args[i]))
				{
					System.Console.Out.WriteLine(args[i] + " is a known word.  Log probabilities [log P(w|t)] for its taggings are:");
					for (IEnumerator<IntTaggedWord> it = lex.RuleIteratorByWord(wordIndex.AddToIndex(args[i]), i - 3, null); it.MoveNext(); )
					{
						IntTaggedWord iTW = it.Current;
						System.Console.Out.WriteLine(StringUtils.Pad(iTW, 24) + nf.Format(lex.Score(iTW, i - 3, wordIndex.Get(iTW.word), null)));
					}
				}
				else
				{
					string sig = lex.GetUnknownWordModel().GetSignature(args[i], i - 3);
					System.Console.Out.WriteLine(args[i] + " is an unknown word.  Signature with uwm " + lex.GetUnknownWordModel().GetUnknownLevel() + ((i == 3) ? " init" : "non-init") + " is: " + sig);
					impos.Clear();
					IList<string> lis = new List<string>(tagIndex.ObjectsList());
					lis.Sort();
					foreach (string tStr in lis)
					{
						IntTaggedWord iTW = new IntTaggedWord(args[i], tStr, wordIndex, tagIndex);
						double score = lex.Score(iTW, 1, args[i], null);
						if (score == float.NegativeInfinity)
						{
							impos.Add(tStr);
						}
						else
						{
							System.Console.Out.WriteLine(StringUtils.Pad(iTW, 24) + nf.Format(score));
						}
					}
					if (impos.Count > 0)
					{
						System.Console.Out.WriteLine(args[i] + " impossible tags: " + impos);
					}
				}
				System.Console.Out.WriteLine();
			}
		}

		public virtual IUnknownWordModel GetUnknownWordModel()
		{
			return uwModel;
		}

		public void SetUnknownWordModel(IUnknownWordModel uwm)
		{
			this.uwModel = uwm;
		}

		// TODO(spenceg): Debug method for getting a treebank with CoreLabels. This is for training
		// the FactoredLexicon.
		public virtual void Train(ICollection<Tree> trees, ICollection<Tree> rawTrees)
		{
			Train(trees);
		}

		private const long serialVersionUID = 40L;
	}
}
