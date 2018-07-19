using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns
{
	public abstract class PhraseScorer<E>
		where E : Pattern
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Patterns.PhraseScorer));

		internal ConstantsAndVariables constVars;

		internal double OOVExternalFeatWt = 0.5;

		internal double OOVdictOdds = 1e-10;

		internal double OOVDomainNgramScore = 1e-10;

		internal double OOVGoogleNgramScore = 1e-10;

		public bool usePatternWeights = true;

		internal PhraseScorer.Normalization wordFreqNorm = PhraseScorer.Normalization.ValueOf("LOG");

		/// <summary>
		/// For phrases, some phrases are evaluated as a combination of their
		/// individual words.
		/// </summary>
		/// <remarks>
		/// For phrases, some phrases are evaluated as a combination of their
		/// individual words. Default is taking minimum of all the words. This flag
		/// takes average instead of the min.
		/// </remarks>
		internal bool useAvgInsteadofMinPhraseScoring = false;

		public enum Normalization
		{
			None,
			Sqrt,
			Log
		}

		public enum Similarities
		{
			Numitems,
			Avgsim,
			Maxsim
		}

		public PhraseScorer(ConstantsAndVariables constvar)
		{
			//these get overwritten in ScorePhrasesLearnFeatWt class
			this.constVars = constvar;
		}

		internal ICounter<CandidatePhrase> learnedScores = new ClassicCounter<CandidatePhrase>();

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		internal abstract ICounter<CandidatePhrase> ScorePhrases(string label, TwoDimensionalCounter<CandidatePhrase, E> terms, TwoDimensionalCounter<CandidatePhrase, E> wordsPatExtracted, ICounter<E> allSelectedPatterns, ICollection<CandidatePhrase
			> alreadyIdentifiedWords, bool forLearningPatterns);

		internal virtual ICounter<CandidatePhrase> GetLearnedScores()
		{
			return learnedScores;
		}

		internal virtual double GetPatTFIDFScore(CandidatePhrase word, ICounter<E> patsThatExtractedThis, ICounter<E> allSelectedPatterns)
		{
			if (Data.processedDataFreq.GetCount(word) == 0.0)
			{
				Redwood.Log(Redwood.Warn, "How come the processed corpus freq has count of " + word + " 0. The count in raw freq is " + Data.rawFreq.GetCount(word) + " and the Data.rawFreq size is " + Data.rawFreq.Size());
				return 0;
			}
			else
			{
				double total = 0;
				ICollection<E> rem = new HashSet<E>();
				foreach (KeyValuePair<E, double> en2 in patsThatExtractedThis.EntrySet())
				{
					double weight = 1.0;
					if (usePatternWeights)
					{
						weight = allSelectedPatterns.GetCount(en2.Key);
						if (weight == 0)
						{
							Redwood.Log(Redwood.Force, "Warning: Weight zero for " + en2.Key + ". May be pattern was removed when choosing other patterns (if subsumed by another pattern).");
							rem.Add(en2.Key);
						}
					}
					total += weight;
				}
				Counters.RemoveKeys(patsThatExtractedThis, rem);
				double score = total / Data.processedDataFreq.GetCount(word);
				return score;
			}
		}

		public static double GetGoogleNgramScore(CandidatePhrase g)
		{
			double count = GoogleNGramsSQLBacked.GetCount(g.GetPhrase().ToLower()) + GoogleNGramsSQLBacked.GetCount(g.GetPhrase());
			if (count != -1)
			{
				if (!Data.rawFreq.ContainsKey(g))
				{
					//returning 1 because usually lower this tf-idf score the better. if we don't have raw freq info, give it a bad score
					return 1;
				}
				else
				{
					return (1 + Data.rawFreq.GetCount(g) * Math.Sqrt(Data.ratioGoogleNgramFreqWithDataFreq)) / count;
				}
			}
			return 0;
		}

		public virtual double GetDomainNgramScore(string g)
		{
			string gnew = g;
			if (!Data.domainNGramRawFreq.ContainsKey(gnew))
			{
				gnew = g.ReplaceAll(" ", string.Empty);
			}
			if (!Data.domainNGramRawFreq.ContainsKey(gnew))
			{
				gnew = g.ReplaceAll("-", string.Empty);
			}
			else
			{
				g = gnew;
			}
			if (!Data.domainNGramRawFreq.ContainsKey(gnew))
			{
				log.Info("domain count 0 for " + g);
				return 0;
			}
			else
			{
				g = gnew;
			}
			return ((1 + Data.rawFreq.GetCount(g) * Math.Sqrt(Data.ratioDomainNgramFreqWithDataFreq)) / Data.domainNGramRawFreq.GetCount(g));
		}

		public virtual double GetDistSimWtScore(string ph, string label)
		{
			int num = constVars.GetWordClassClusters()[ph];
			if (num == null)
			{
				num = constVars.GetWordClassClusters()[ph.ToLower()];
			}
			if (num != null && constVars.distSimWeights[label].ContainsKey(num))
			{
				return constVars.distSimWeights[label].GetCount(num);
			}
			else
			{
				string[] t = ph.Split("\\s+");
				if (t.Length < 2)
				{
					return OOVExternalFeatWt;
				}
				double totalscore = 0;
				double minScore = double.MaxValue;
				foreach (string w in t)
				{
					double score = OOVExternalFeatWt;
					int numw = constVars.GetWordClassClusters()[w];
					if (num == null)
					{
						num = constVars.GetWordClassClusters()[w.ToLower()];
					}
					if (numw != null && constVars.distSimWeights[label].ContainsKey(numw))
					{
						score = constVars.distSimWeights[label].GetCount(numw);
					}
					if (score < minScore)
					{
						minScore = score;
					}
					totalscore += score;
				}
				if (useAvgInsteadofMinPhraseScoring)
				{
					return totalscore / ph.Length;
				}
				else
				{
					return minScore;
				}
			}
		}

		public virtual string WordShape(string word)
		{
			string wordShape = constVars.GetWordShapeCache()[word];
			if (wordShape == null)
			{
				wordShape = WordShapeClassifier.WordShape(word, constVars.wordShaper);
				constVars.GetWordShapeCache()[word] = wordShape;
			}
			return wordShape;
		}

		public virtual double GetWordShapeScore(string word, string label)
		{
			string wordShape = WordShape(word);
			double thislabel = 0;
			double alllabels = 0;
			foreach (KeyValuePair<string, ICounter<string>> en in constVars.GetWordShapesForLabels())
			{
				if (en.Key.Equals(label))
				{
					thislabel = en.Value.GetCount(wordShape);
				}
				alllabels += en.Value.GetCount(wordShape);
			}
			double score = thislabel / (alllabels + 1);
			return score;
		}

		public virtual double GetDictOddsScore(CandidatePhrase word, string label, double defaultWt)
		{
			double dscore;
			ICounter<CandidatePhrase> dictOddsWordWeights = constVars.dictOddsWeights[label];
			System.Diagnostics.Debug.Assert(dictOddsWordWeights != null, "dictOddsWordWeights is null for label " + label);
			if (dictOddsWordWeights.ContainsKey(word))
			{
				dscore = dictOddsWordWeights.GetCount(word);
			}
			else
			{
				dscore = GetPhraseWeightFromWords(dictOddsWordWeights, word, defaultWt);
			}
			return dscore;
		}

		public virtual double GetPhraseWeightFromWords(ICounter<CandidatePhrase> weights, CandidatePhrase ph, double defaultWt)
		{
			string[] t = ph.GetPhrase().Split("\\s+");
			if (t.Length < 2)
			{
				if (weights.ContainsKey(ph))
				{
					return weights.GetCount(ph);
				}
				else
				{
					return defaultWt;
				}
			}
			double totalscore = 0;
			double minScore = double.MaxValue;
			foreach (string w in t)
			{
				double score = defaultWt;
				if (weights.ContainsKey(CandidatePhrase.CreateOrGet(w)))
				{
					score = weights.GetCount(w);
				}
				if (score < minScore)
				{
					minScore = score;
				}
				totalscore += score;
			}
			if (useAvgInsteadofMinPhraseScoring)
			{
				return totalscore / ph.GetPhrase().Length;
			}
			else
			{
				return minScore;
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public abstract ICounter<CandidatePhrase> ScorePhrases(string label, ICollection<CandidatePhrase> terms, bool forLearningPatterns);

		public abstract void PrintReasonForChoosing(ICounter<CandidatePhrase> phrases);
	}
}
