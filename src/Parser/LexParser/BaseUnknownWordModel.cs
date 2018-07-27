using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>An unknown word model for a generic language.</summary>
	/// <remarks>
	/// An unknown word model for a generic language.  This was originally designed for
	/// German, changing only to remove German-specific numeric features.  Models unknown
	/// words based on their prefix and suffixes, as well as capital letters.
	/// </remarks>
	/// <author>Roger Levy</author>
	/// <author>Greg Donaker (corrections and modeling improvements)</author>
	/// <author>Christopher Manning (generalized and improved what Greg did)</author>
	/// <author>Anna Rafferty</author>
	[System.Serializable]
	public class BaseUnknownWordModel : IUnknownWordModel
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.BaseUnknownWordModel));

		private const long serialVersionUID = 6355171148751673822L;

		protected internal const bool Verbose = false;

		protected internal bool useFirst;

		private readonly bool useEnd;

		protected internal bool useGT;

		private readonly bool useFirstCap;

		private int endLength = 2;

		/// <summary>What type of equivalence classing is done in getSignature</summary>
		protected internal readonly int unknownLevel;

		protected internal const string unknown = "UNK";

		protected internal const int nullWord = -1;

		protected internal const short nullTag = -1;

		protected internal static readonly IntTaggedWord NullItw = new IntTaggedWord(nullWord, nullTag);

		protected internal readonly TrainOptions trainOptions;

		protected internal readonly IIndex<string> wordIndex;

		protected internal readonly IIndex<string> tagIndex;

		/// <summary>Has counts for taggings in terms of unseen signatures.</summary>
		/// <remarks>
		/// Has counts for taggings in terms of unseen signatures. The IntTagWords are
		/// for (tag,sig), (tag,null), (null,sig), (null,null). (None for basic UNK if
		/// there are signatures.)
		/// </remarks>
		protected internal readonly ClassicCounter<IntTaggedWord> unSeenCounter;

		/// <summary>
		/// This maps from a tag (as a label) to a Counter from word signatures to
		/// their P(sig|tag), as estimated in the model.
		/// </summary>
		/// <remarks>
		/// This maps from a tag (as a label) to a Counter from word signatures to
		/// their P(sig|tag), as estimated in the model. For Chinese, the word
		/// signature is just the first character or its unicode type for things
		/// that aren't Chinese characters.
		/// </remarks>
		protected internal readonly IDictionary<ILabel, ClassicCounter<string>> tagHash;

		/// <summary>This is the set of all signatures that we have seen.</summary>
		private readonly ICollection<string> seenEnd;

		internal readonly IDictionary<string, float> unknownGT;

		/// <summary>All classes that implement UnknownWordModel must call the constructor that initializes this variable.</summary>
		private readonly ILexicon lex;

		public BaseUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, ClassicCounter<IntTaggedWord> unSeenCounter, IDictionary<ILabel, ClassicCounter<string>> tagHash, IDictionary<string, float> unknownGT, 
			ICollection<string> seenEnd)
		{
			//= true;
			// Only care if first is capitalized
			// only used if useEnd==true
			endLength = op.lexOptions.unknownSuffixSize;
			// TODO: refactor these terms into BaseUnknownWordModelTrainer
			useEnd = (op.lexOptions.unknownSuffixSize > 0 && op.lexOptions.useUnknownWordSignatures > 0);
			useFirstCap = op.lexOptions.useUnknownWordSignatures > 0;
			useGT = (op.lexOptions.useUnknownWordSignatures == 0);
			useFirst = false;
			this.lex = lex;
			this.trainOptions = op.trainOptions;
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
			this.unSeenCounter = unSeenCounter;
			this.tagHash = tagHash;
			this.seenEnd = seenEnd;
			this.unknownGT = unknownGT;
			unknownLevel = op.lexOptions.useUnknownWordSignatures;
		}

		/// <summary>This constructor creates an UWM with empty data structures.</summary>
		/// <remarks>
		/// This constructor creates an UWM with empty data structures.  Only
		/// use if loading in the data separately, such as by reading in text
		/// lines containing the data.
		/// </remarks>
		public BaseUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: this(op, lex, wordIndex, tagIndex, new ClassicCounter<IntTaggedWord>(), Generics.NewHashMap<ILabel, ClassicCounter<string>>(), Generics.NewHashMap<string, float>(), Generics.NewHashSet<string>())
		{
		}

		/// <summary>
		/// Currently we don't consider loc or the other parameters in determining
		/// score in the default implementation; only English uses them.
		/// </summary>
		public virtual float Score(IntTaggedWord itw, int loc, double c_Tseen, double total, double smooth, string word)
		{
			return Score(itw, word);
		}

		// todo [cdm 2010]: Recheck that this method really does the right thing in making a P(W|T) estimate....
		public virtual float Score(IntTaggedWord itw, string word)
		{
			float logProb;
			// Label tag = itw.tagLabel();
			string tagStr = itw.TagString(tagIndex);
			ILabel tag = new Tag(tagStr);
			// testing
			//EncodingPrintWriter.out.println("Scoring unknown word " + word + " with tag " + tag,encoding);
			// end testing
			if (useEnd || useFirst || useFirstCap)
			{
				string end = GetSignature(word, -1);
				// The getSignature here doesn't use sentence position
				if (useGT && !seenEnd.Contains(end))
				{
					logProb = ScoreGT(tagStr);
				}
				else
				{
					if (!seenEnd.Contains(end))
					{
						end = unknown;
					}
					//System.out.println("using end-character model for for unknown word "+  word + " for tag " + tag);
					/* get the Counter of terminal rewrites for the relevant tag */
					ClassicCounter<string> wordProbs = tagHash[tag];
					/* if the proposed tag has never been seen before, issue a
					* warning and return probability 0
					*/
					if (wordProbs == null)
					{
						log.Info("Warning: proposed tag is unseen in training data:\t" + tagStr);
						logProb = float.NegativeInfinity;
					}
					else
					{
						if (wordProbs.KeySet().Contains(end))
						{
							logProb = (float)wordProbs.GetCount(end);
						}
						else
						{
							logProb = (float)wordProbs.GetCount(unknown);
						}
					}
				}
			}
			else
			{
				if (useGT)
				{
					logProb = ScoreGT(tagStr);
				}
				else
				{
					log.Info("Warning: no unknown word model in place!\nGiving the combination " + word + ' ' + tagStr + " zero probability.");
					logProb = float.NegativeInfinity;
				}
			}
			// should never get this!
			//EncodingPrintWriter.out.println("Unknown word estimate for " + word + " as " + tag + ": " + logProb,encoding); //debugging
			return logProb;
		}

		/// <summary>Calculate P(Tag|Signature) with Bayesian smoothing via just P(Tag|Unknown)</summary>
		public virtual double ScoreProbTagGivenWordSignature(IntTaggedWord iTW, int loc, double smooth, string word)
		{
			throw new NotSupportedException();
		}

		// todo [cdm 2012, based on error report from Thang]: this is broken because the Label passed in is a Tag, which will never match on the CoreLabel's now in unknownGT.keySet()
		// todo [cdm 2012]: But see if this bug is only if you use Lexicon's main method, or also when training a parser in the usual way.
		protected internal virtual float ScoreGT(string tag)
		{
			if (unknownGT.Contains(tag))
			{
				return unknownGT[tag];
			}
			else
			{
				return float.NegativeInfinity;
			}
		}

		/// <summary>Signature for a specific word; loc parameter is ignored.</summary>
		/// <param name="word">The word</param>
		/// <param name="loc">Its sentence position</param>
		/// <returns>A "signature" (which represents an equivalence class of Strings), e.g., a suffix of the string</returns>
		public virtual string GetSignature(string word, int loc)
		{
			StringBuilder subStr = new StringBuilder("UNK-");
			int n = word.Length - 1;
			char first = word[0];
			if (useFirstCap)
			{
				if (char.IsUpperCase(first) || char.IsTitleCase(first))
				{
					subStr.Append('C');
				}
				else
				{
					subStr.Append('c');
				}
			}
			if (useFirst)
			{
				subStr.Append(first);
			}
			if (useEnd)
			{
				subStr.Append(Sharpen.Runtime.Substring(word, n - endLength > 0 ? n - endLength : 0, n));
			}
			return subStr.ToString();
		}

		public virtual int GetSignatureIndex(int wordIndex, int sentencePosition, string word)
		{
			return 0;
		}

		/// <summary>
		/// Get the lexicon associated with this unknown word model; usually not used, but
		/// might be useful to tell you if a related word is known or unknown, for example.
		/// </summary>
		public virtual ILexicon GetLexicon()
		{
			return lex;
		}

		public virtual int GetUnknownLevel()
		{
			return unknownLevel;
		}

		/// <summary>Adds the tagging with count to the data structures in this Lexicon.</summary>
		public virtual void AddTagging(bool seen, IntTaggedWord itw, double count)
		{
			if (seen)
			{
				log.Info("UWM.addTagging: Shouldn't call with seen word!");
			}
			else
			{
				unSeenCounter.IncrementCount(itw, count);
			}
		}

		// if (itw.tag() == nullTag) {
		// sigs.add(itw);
		// }
		public virtual ICounter<IntTaggedWord> UnSeenCounter()
		{
			return unSeenCounter;
		}
	}
}
