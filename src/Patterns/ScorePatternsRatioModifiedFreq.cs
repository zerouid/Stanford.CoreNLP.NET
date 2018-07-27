using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Patterns
{
	public class ScorePatternsRatioModifiedFreq<E> : ScorePatterns<E>
	{
		public ScorePatternsRatioModifiedFreq(ConstantsAndVariables constVars, GetPatternsFromDataMultiClass.PatternScoring patternScoring, string label, ICollection<CandidatePhrase> allCandidatePhrases, TwoDimensionalCounter<E, CandidatePhrase> patternsandWords4Label
			, TwoDimensionalCounter<E, CandidatePhrase> negPatternsandWords4Label, TwoDimensionalCounter<E, CandidatePhrase> unLabeledPatternsandWords4Label, TwoDimensionalCounter<CandidatePhrase, ConstantsAndVariables.ScorePhraseMeasures> phInPatScores
			, ScorePhrases scorePhrases, Properties props)
			: base(constVars, patternScoring, label, allCandidatePhrases, patternsandWords4Label, negPatternsandWords4Label, unLabeledPatternsandWords4Label, props)
		{
			this.phInPatScores = phInPatScores;
			this.scorePhrases = scorePhrases;
		}

		private TwoDimensionalCounter<CandidatePhrase, ConstantsAndVariables.ScorePhraseMeasures> phInPatScores;

		private ScorePhrases scorePhrases;

		// cached values
		public override void SetUp(Properties props)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public override ICounter<E> Score()
		{
			ICounter<CandidatePhrase> externalWordWeightsNormalized = null;
			if (constVars.dictOddsWeights.Contains(label))
			{
				externalWordWeightsNormalized = GetPatternsFromDataMultiClass.NormalizeSoftMaxMinMaxScores(constVars.dictOddsWeights[label], true, true, false);
			}
			ICounter<E> currentPatternWeights4Label = new ClassicCounter<E>();
			bool useFreqPhraseExtractedByPat = false;
			if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.SqrtAllRatio))
			{
				useFreqPhraseExtractedByPat = true;
			}
			IToDoubleFunction<Pair<E, CandidatePhrase>> numeratorScore = null;
			ICounter<E> numeratorPatWt = this.Convert2OneDim(label, numeratorScore, allCandidatePhrases, patternsandWords4Label, constVars.sqrtPatScore, false, null, useFreqPhraseExtractedByPat);
			ICounter<E> denominatorPatWt = null;
			IToDoubleFunction<Pair<E, CandidatePhrase>> denoScore;
			if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PosNegUnlabOdds))
			{
				denoScore = null;
				denominatorPatWt = this.Convert2OneDim(label, denoScore, allCandidatePhrases, patternsandWords4Label, constVars.sqrtPatScore, false, externalWordWeightsNormalized, useFreqPhraseExtractedByPat);
			}
			else
			{
				if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.RatioAll))
				{
					denoScore = null;
					denominatorPatWt = this.Convert2OneDim(label, denoScore, allCandidatePhrases, patternsandWords4Label, constVars.sqrtPatScore, false, externalWordWeightsNormalized, useFreqPhraseExtractedByPat);
				}
				else
				{
					if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PosNegOdds))
					{
						denoScore = null;
						denominatorPatWt = this.Convert2OneDim(label, denoScore, allCandidatePhrases, patternsandWords4Label, constVars.sqrtPatScore, false, externalWordWeightsNormalized, useFreqPhraseExtractedByPat);
					}
					else
					{
						if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PhEvalInPat) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PhEvalInPatLogP) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.
							Logreg) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.LOGREGlogP))
						{
							denoScore = null;
							denominatorPatWt = this.Convert2OneDim(label, denoScore, allCandidatePhrases, patternsandWords4Label, constVars.sqrtPatScore, true, externalWordWeightsNormalized, useFreqPhraseExtractedByPat);
						}
						else
						{
							if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.SqrtAllRatio))
							{
								denoScore = null;
								denominatorPatWt = this.Convert2OneDim(label, denoScore, allCandidatePhrases, patternsandWords4Label, true, false, externalWordWeightsNormalized, useFreqPhraseExtractedByPat);
							}
							else
							{
								throw new Exception("Cannot understand patterns scoring");
							}
						}
					}
				}
			}
			currentPatternWeights4Label = Counters.DivisionNonNaN(numeratorPatWt, denominatorPatWt);
			//Multiplying by logP
			if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PhEvalInPatLogP) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.LOGREGlogP))
			{
				ICounter<E> logpos_i = new ClassicCounter<E>();
				foreach (KeyValuePair<E, ClassicCounter<CandidatePhrase>> en in patternsandWords4Label.EntrySet())
				{
					logpos_i.SetCount(en.Key, Math.Log(en.Value.Size()));
				}
				Counters.MultiplyInPlace(currentPatternWeights4Label, logpos_i);
			}
			Counters.RetainNonZeros(currentPatternWeights4Label);
			return currentPatternWeights4Label;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		internal virtual ICounter<E> Convert2OneDim(string label, IToDoubleFunction<Pair<E, CandidatePhrase>> scoringFunction, ICollection<CandidatePhrase> allCandidatePhrases, TwoDimensionalCounter<E, CandidatePhrase> positivePatternsAndWords, bool
			 sqrtPatScore, bool scorePhrasesInPatSelection, ICounter<CandidatePhrase> dictOddsWordWeights, bool useFreqPhraseExtractedByPat)
		{
			//    if (Data.googleNGram.size() == 0 && Data.googleNGramsFile != null) {
			//      Data.loadGoogleNGrams();
			//    }
			ICounter<E> patterns = new ClassicCounter<E>();
			ICounter<CandidatePhrase> googleNgramNormScores = new ClassicCounter<CandidatePhrase>();
			ICounter<CandidatePhrase> domainNgramNormScores = new ClassicCounter<CandidatePhrase>();
			ICounter<CandidatePhrase> externalFeatWtsNormalized = new ClassicCounter<CandidatePhrase>();
			ICounter<CandidatePhrase> editDistanceFromOtherSemanticBinaryScores = new ClassicCounter<CandidatePhrase>();
			ICounter<CandidatePhrase> editDistanceFromAlreadyExtractedBinaryScores = new ClassicCounter<CandidatePhrase>();
			double externalWtsDefault = 0.5;
			ICounter<string> classifierScores = null;
			if ((patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PhEvalInPat) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PhEvalInPatLogP)) && scorePhrasesInPatSelection)
			{
				foreach (CandidatePhrase gc in allCandidatePhrases)
				{
					string g = gc.GetPhrase();
					if (constVars.usePatternEvalEditDistOther)
					{
						editDistanceFromOtherSemanticBinaryScores.SetCount(gc, constVars.GetEditDistanceScoresOtherClassThreshold(label, g));
					}
					if (constVars.usePatternEvalEditDistSame)
					{
						editDistanceFromAlreadyExtractedBinaryScores.SetCount(gc, 1 - constVars.GetEditDistanceScoresThisClassThreshold(label, g));
					}
					if (constVars.usePatternEvalGoogleNgram)
					{
						googleNgramNormScores.SetCount(gc, PhraseScorer.GetGoogleNgramScore(gc));
					}
					if (constVars.usePatternEvalDomainNgram)
					{
						// calculate domain-ngram wts
						if (Data.domainNGramRawFreq.ContainsKey(g))
						{
							System.Diagnostics.Debug.Assert((Data.rawFreq.ContainsKey(gc)));
							domainNgramNormScores.SetCount(gc, scorePhrases.phraseScorer.GetDomainNgramScore(g));
						}
					}
					if (constVars.usePatternEvalWordClass)
					{
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
							externalFeatWtsNormalized.SetCount(gc, externalWtsDefault);
						}
					}
				}
				if (constVars.usePatternEvalGoogleNgram)
				{
					googleNgramNormScores = GetPatternsFromDataMultiClass.NormalizeSoftMaxMinMaxScores(googleNgramNormScores, true, true, false);
				}
				if (constVars.usePatternEvalDomainNgram)
				{
					domainNgramNormScores = GetPatternsFromDataMultiClass.NormalizeSoftMaxMinMaxScores(domainNgramNormScores, true, true, false);
				}
				if (constVars.usePatternEvalWordClass)
				{
					externalFeatWtsNormalized = GetPatternsFromDataMultiClass.NormalizeSoftMaxMinMaxScores(externalFeatWtsNormalized, true, true, false);
				}
			}
			else
			{
				if ((patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.Logreg) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.LOGREGlogP)) && scorePhrasesInPatSelection)
				{
					Properties props2 = new Properties();
					props2.PutAll(props);
					props2.SetProperty("phraseScorerClass", "edu.stanford.nlp.patterns.ScorePhrasesLearnFeatWt");
					ScorePhrases scoreclassifier = new ScorePhrases(props2, constVars);
					System.Console.Out.WriteLine("file is " + props.GetProperty("domainNGramsFile"));
					ArgumentParser.FillOptions(typeof(Data), props2);
					classifierScores = scoreclassifier.phraseScorer.ScorePhrases(label, allCandidatePhrases, true);
				}
			}
			ICounter<CandidatePhrase> cachedScoresForThisIter = new ClassicCounter<CandidatePhrase>();
			foreach (KeyValuePair<E, ClassicCounter<CandidatePhrase>> en in positivePatternsAndWords.EntrySet())
			{
				foreach (KeyValuePair<CandidatePhrase, double> en2 in en.Value.EntrySet())
				{
					CandidatePhrase word = en2.Key;
					ICounter<ConstantsAndVariables.ScorePhraseMeasures> scoreslist = new ClassicCounter<ConstantsAndVariables.ScorePhraseMeasures>();
					double score = 1;
					if ((patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PhEvalInPat) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.PhEvalInPatLogP)) && scorePhrasesInPatSelection)
					{
						if (cachedScoresForThisIter.ContainsKey(word))
						{
							score = cachedScoresForThisIter.GetCount(word);
						}
						else
						{
							if (constVars.GetOtherSemanticClassesWords().Contains(word) || constVars.GetCommonEngWords().Contains(word))
							{
								score = 1;
							}
							else
							{
								if (constVars.usePatternEvalSemanticOdds)
								{
									double semanticClassOdds = 1;
									if (dictOddsWordWeights.ContainsKey(word))
									{
										semanticClassOdds = 1 - dictOddsWordWeights.GetCount(word);
									}
									scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Semanticodds, semanticClassOdds);
								}
								if (constVars.usePatternEvalGoogleNgram)
								{
									double gscore = 0;
									if (googleNgramNormScores.ContainsKey(word))
									{
										gscore = 1 - googleNgramNormScores.GetCount(word);
									}
									scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Googlengram, gscore);
								}
								if (constVars.usePatternEvalDomainNgram)
								{
									double domainscore;
									if (domainNgramNormScores.ContainsKey(word))
									{
										domainscore = 1 - domainNgramNormScores.GetCount(word);
									}
									else
									{
										domainscore = 1 - scorePhrases.phraseScorer.GetPhraseWeightFromWords(domainNgramNormScores, word, scorePhrases.phraseScorer.OOVDomainNgramScore);
									}
									scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Domainngram, domainscore);
								}
								if (constVars.usePatternEvalWordClass)
								{
									double externalFeatureWt = externalWtsDefault;
									if (externalFeatWtsNormalized.ContainsKey(word))
									{
										externalFeatureWt = 1 - externalFeatWtsNormalized.GetCount(word);
									}
									scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Distsim, externalFeatureWt);
								}
								if (constVars.usePatternEvalEditDistOther)
								{
									System.Diagnostics.Debug.Assert(editDistanceFromOtherSemanticBinaryScores.ContainsKey(word), "How come no edit distance info for word " + word + string.Empty);
									scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Editdistother, editDistanceFromOtherSemanticBinaryScores.GetCount(word));
								}
								if (constVars.usePatternEvalEditDistSame)
								{
									scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Editdistsame, editDistanceFromAlreadyExtractedBinaryScores.GetCount(word));
								}
								// taking average
								score = Counters.Mean(scoreslist);
								phInPatScores.SetCounter(word, scoreslist);
							}
							cachedScoresForThisIter.SetCount(word, score);
						}
					}
					else
					{
						if ((patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.Logreg) || patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.LOGREGlogP)) && scorePhrasesInPatSelection)
						{
							score = 1 - classifierScores.GetCount(word);
						}
					}
					// score = 1 - scorePhrases.scoreUsingClassifer(classifier,
					// e.getKey(), label, true, null, null, dictOddsWordWeights);
					// throw new RuntimeException("not implemented yet");
					if (useFreqPhraseExtractedByPat)
					{
						score = score * scoringFunction.ApplyAsDouble(new Pair<E, CandidatePhrase>(en.Key, word));
					}
					if (constVars.sqrtPatScore)
					{
						patterns.IncrementCount(en.Key, Math.Sqrt(score));
					}
					else
					{
						patterns.IncrementCount(en.Key, score);
					}
				}
			}
			return patterns;
		}
	}
}
