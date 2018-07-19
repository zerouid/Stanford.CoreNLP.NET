using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>Score phrases by averaging scores of individual features.</summary>
	/// <author>Sonal Gupta (sonalg@stanford.edu)</author>
	public class ScorePhrasesAverageFeatures<E> : PhraseScorer<E>
		where E : Pattern
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Patterns.ScorePhrasesAverageFeatures));

		public ScorePhrasesAverageFeatures(ConstantsAndVariables constvar)
			: base(constvar)
		{
		}

		private TwoDimensionalCounter<CandidatePhrase, ConstantsAndVariables.ScorePhraseMeasures> phraseScoresNormalized = new TwoDimensionalCounter<CandidatePhrase, ConstantsAndVariables.ScorePhraseMeasures>();

		internal override ICounter<CandidatePhrase> ScorePhrases(string label, TwoDimensionalCounter<CandidatePhrase, E> terms, TwoDimensionalCounter<CandidatePhrase, E> wordsPatExtracted, ICounter<E> allSelectedPatterns, ICollection<CandidatePhrase
			> alreadyIdentifiedWords, bool forLearningPatterns)
		{
			IDictionary<CandidatePhrase, ICounter<ConstantsAndVariables.ScorePhraseMeasures>> scores = new Dictionary<CandidatePhrase, ICounter<ConstantsAndVariables.ScorePhraseMeasures>>();
			if (Data.domainNGramsFile != null)
			{
				Data.LoadDomainNGrams();
			}
			Redwood.Log(ConstantsAndVariables.extremedebug, "Considering terms: " + terms.FirstKeySet());
			// calculate TF-IDF like scores
			ICounter<CandidatePhrase> tfidfScores = new ClassicCounter<CandidatePhrase>();
			if (constVars.usePhraseEvalPatWtByFreq)
			{
				foreach (KeyValuePair<CandidatePhrase, ClassicCounter<E>> en in terms.EntrySet())
				{
					double score = GetPatTFIDFScore(en.Key, en.Value, allSelectedPatterns);
					tfidfScores.SetCount(en.Key, score);
				}
				Redwood.Log(ConstantsAndVariables.extremedebug, "BEFORE IDF " + Counters.ToSortedString(tfidfScores, 100, "%1$s:%2$f", "\t"));
				Counters.DivideInPlace(tfidfScores, Data.processedDataFreq);
			}
			ICounter<CandidatePhrase> externalFeatWtsNormalized = new ClassicCounter<CandidatePhrase>();
			ICounter<CandidatePhrase> domainNgramNormScores = new ClassicCounter<CandidatePhrase>();
			ICounter<CandidatePhrase> googleNgramNormScores = new ClassicCounter<CandidatePhrase>();
			ICounter<CandidatePhrase> editDistanceOtherBinaryScores = new ClassicCounter<CandidatePhrase>();
			ICounter<CandidatePhrase> editDistanceSameBinaryScores = new ClassicCounter<CandidatePhrase>();
			foreach (CandidatePhrase gc in terms.FirstKeySet())
			{
				string g = gc.GetPhrase();
				if (constVars.usePhraseEvalEditDistOther)
				{
					editDistanceOtherBinaryScores.SetCount(gc, 1 - constVars.GetEditDistanceScoresOtherClassThreshold(label, g));
				}
				if (constVars.usePhraseEvalEditDistSame)
				{
					editDistanceSameBinaryScores.SetCount(gc, constVars.GetEditDistanceScoresThisClassThreshold(label, g));
				}
				if (constVars.usePhraseEvalDomainNgram)
				{
					// calculate domain-ngram wts
					if (Data.domainNGramRawFreq.ContainsKey(g))
					{
						System.Diagnostics.Debug.Assert((Data.rawFreq.ContainsKey(gc)));
						domainNgramNormScores.SetCount(gc, GetDomainNgramScore(g));
					}
					else
					{
						log.Info("why is " + g + " not present in domainNgram");
					}
				}
				if (constVars.usePhraseEvalGoogleNgram)
				{
					googleNgramNormScores.SetCount(gc, GetGoogleNgramScore(gc));
				}
				if (constVars.usePhraseEvalWordClass)
				{
					// calculate dist sim weights
					int num = constVars.GetWordClassClusters()[g];
					if (num == null)
					{
						num = constVars.GetWordClassClusters()[g.ToLower()];
					}
					if (num != null && constVars.distSimWeights[label].ContainsKey(num))
					{
						externalFeatWtsNormalized.SetCount(gc, constVars.distSimWeights[label].GetCount(num));
					}
					else
					{
						externalFeatWtsNormalized.SetCount(gc, OOVExternalFeatWt);
					}
				}
			}
			ICounter<CandidatePhrase> normTFIDFScores = GetPatternsFromDataMultiClass.NormalizeSoftMaxMinMaxScores(tfidfScores, true, true, false);
			ICounter<CandidatePhrase> dictOdddsScores = null;
			if (constVars.usePhraseEvalSemanticOdds)
			{
				System.Diagnostics.Debug.Assert(constVars.dictOddsWeights != null, "usePhraseEvalSemanticOdds is true but dictOddsWeights is null for the label " + label);
				dictOdddsScores = GetPatternsFromDataMultiClass.NormalizeSoftMaxMinMaxScores(constVars.dictOddsWeights[label], true, true, false);
			}
			domainNgramNormScores = GetPatternsFromDataMultiClass.NormalizeSoftMaxMinMaxScores(domainNgramNormScores, true, true, false);
			googleNgramNormScores = GetPatternsFromDataMultiClass.NormalizeSoftMaxMinMaxScores(googleNgramNormScores, true, true, false);
			externalFeatWtsNormalized = GetPatternsFromDataMultiClass.NormalizeSoftMaxMinMaxScores(externalFeatWtsNormalized, true, true, false);
			// Counters.max(googleNgramNormScores);
			// Counters.max(externalFeatWtsNormalized);
			foreach (CandidatePhrase word in terms.FirstKeySet())
			{
				if (alreadyIdentifiedWords.Contains(word))
				{
					continue;
				}
				ICounter<ConstantsAndVariables.ScorePhraseMeasures> scoreslist = new ClassicCounter<ConstantsAndVariables.ScorePhraseMeasures>();
				System.Diagnostics.Debug.Assert(normTFIDFScores.ContainsKey(word), "NormTFIDF score does not contain" + word);
				double tfscore = normTFIDFScores.GetCount(word);
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Patwtbyfreq, tfscore);
				if (constVars.usePhraseEvalSemanticOdds)
				{
					double dscore;
					if (dictOdddsScores.ContainsKey(word))
					{
						dscore = dictOdddsScores.GetCount(word);
					}
					else
					{
						dscore = GetPhraseWeightFromWords(dictOdddsScores, word, OOVdictOdds);
					}
					scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Semanticodds, dscore);
				}
				if (constVars.usePhraseEvalDomainNgram)
				{
					double domainscore;
					if (domainNgramNormScores.ContainsKey(word))
					{
						domainscore = domainNgramNormScores.GetCount(word);
					}
					else
					{
						domainscore = GetPhraseWeightFromWords(domainNgramNormScores, word, OOVDomainNgramScore);
					}
					scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Domainngram, domainscore);
				}
				if (constVars.usePhraseEvalGoogleNgram)
				{
					double googlescore;
					if (googleNgramNormScores.ContainsKey(word))
					{
						googlescore = googleNgramNormScores.GetCount(word);
					}
					else
					{
						googlescore = GetPhraseWeightFromWords(googleNgramNormScores, word, OOVGoogleNgramScore);
					}
					scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Googlengram, googlescore);
				}
				if (constVars.usePhraseEvalWordClass)
				{
					double externalFeatureWt;
					if (externalFeatWtsNormalized.ContainsKey(word))
					{
						externalFeatureWt = externalFeatWtsNormalized.GetCount(word);
					}
					else
					{
						externalFeatureWt = GetPhraseWeightFromWords(externalFeatWtsNormalized, word, OOVExternalFeatWt);
					}
					scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Distsim, externalFeatureWt);
				}
				if (constVars.usePhraseEvalEditDistOther)
				{
					System.Diagnostics.Debug.Assert(editDistanceOtherBinaryScores.ContainsKey(word), "How come no edit distance info?");
					double editD = editDistanceOtherBinaryScores.GetCount(word);
					scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Editdistother, editD);
				}
				if (constVars.usePhraseEvalEditDistSame)
				{
					double editDSame = editDistanceSameBinaryScores.GetCount(word);
					scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Editdistsame, editDSame);
				}
				if (constVars.usePhraseEvalWordShape)
				{
					scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Wordshape, this.GetWordShapeScore(word.GetPhrase(), label));
				}
				scores[word] = scoreslist;
				phraseScoresNormalized.SetCounter(word, scoreslist);
			}
			ICounter<CandidatePhrase> phraseScores = new ClassicCounter<CandidatePhrase>();
			foreach (KeyValuePair<CandidatePhrase, ICounter<ConstantsAndVariables.ScorePhraseMeasures>> wEn in scores)
			{
				double avgScore = Counters.Mean(wEn.Value);
				if (!avgScore.IsInfinite() && !double.IsNaN(avgScore))
				{
					phraseScores.SetCount(wEn.Key, avgScore);
				}
				else
				{
					Redwood.Log(Redwood.Dbg, "Ignoring " + wEn.Key + " because score is " + avgScore);
				}
			}
			return phraseScores;
		}

		/// <exception cref="System.IO.IOException"/>
		public override ICounter<CandidatePhrase> ScorePhrases(string label, ICollection<CandidatePhrase> terms, bool forLearningPatterns)
		{
			throw new Exception("not implemented");
		}

		public override void PrintReasonForChoosing(ICounter<CandidatePhrase> phrases)
		{
		}
		//TODO
	}
}
