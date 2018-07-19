using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns
{
	public class ScorePatternsFreqBased<E> : ScorePatterns<E>
	{
		public ScorePatternsFreqBased(ConstantsAndVariables constVars, GetPatternsFromDataMultiClass.PatternScoring patternScoring, string label, ICollection<CandidatePhrase> allCandidatePhrases, TwoDimensionalCounter<E, CandidatePhrase> patternsandWords4Label
			, TwoDimensionalCounter<E, CandidatePhrase> negPatternsandWords4Label, TwoDimensionalCounter<E, CandidatePhrase> unLabeledPatternsandWords4Label, Properties props)
			: base(constVars, patternScoring, label, allCandidatePhrases, patternsandWords4Label, negPatternsandWords4Label, unLabeledPatternsandWords4Label, props)
		{
		}

		public override void SetUp(Properties props)
		{
		}

		public override ICounter<E> Score()
		{
			ICounter<E> currentPatternWeights4Label = new ClassicCounter<E>();
			ICounter<E> pos_i = new ClassicCounter<E>();
			ICounter<E> neg_i = new ClassicCounter<E>();
			ICounter<E> unlab_i = new ClassicCounter<E>();
			foreach (KeyValuePair<E, ClassicCounter<CandidatePhrase>> en in negPatternsandWords4Label.EntrySet())
			{
				neg_i.SetCount(en.Key, en.Value.Size());
			}
			foreach (KeyValuePair<E, ClassicCounter<CandidatePhrase>> en_1 in unLabeledPatternsandWords4Label.EntrySet())
			{
				unlab_i.SetCount(en_1.Key, en_1.Value.Size());
			}
			foreach (KeyValuePair<E, ClassicCounter<CandidatePhrase>> en_2 in patternsandWords4Label.EntrySet())
			{
				pos_i.SetCount(en_2.Key, en_2.Value.Size());
			}
			ICounter<E> all_i = Counters.Add(pos_i, neg_i);
			all_i.AddAll(unlab_i);
			//    for (Entry<Integer, ClassicCounter<String>> en : allPatternsandWords4Label
			//        .entrySet()) {
			//      all_i.setCount(en.getKey(), en.getValue().size());
			//    }
			ICounter<E> posneg_i = Counters.Add(pos_i, neg_i);
			ICounter<E> logFi = new ClassicCounter<E>(pos_i);
			Counters.LogInPlace(logFi);
			if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.RlogF))
			{
				currentPatternWeights4Label = Counters.Product(Counters.Division(pos_i, all_i), logFi);
			}
			else
			{
				if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.RlogFPosNeg))
				{
					Redwood.Log("extremePatDebug", "computing rlogfposneg");
					currentPatternWeights4Label = Counters.Product(Counters.Division(pos_i, posneg_i), logFi);
				}
				else
				{
					if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.RlogFUnlabNeg))
					{
						Redwood.Log("extremePatDebug", "computing rlogfunlabeg");
						currentPatternWeights4Label = Counters.Product(Counters.Division(pos_i, Counters.Add(neg_i, unlab_i)), logFi);
					}
					else
					{
						if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.RlogFNeg))
						{
							Redwood.Log("extremePatDebug", "computing rlogfneg");
							currentPatternWeights4Label = Counters.Product(Counters.Division(pos_i, neg_i), logFi);
						}
						else
						{
							if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.YanGarber02))
							{
								ICounter<E> acc = Counters.Division(pos_i, Counters.Add(pos_i, neg_i));
								double thetaPrecision = 0.8;
								Counters.RetainAbove(acc, thetaPrecision);
								ICounter<E> conf = Counters.Product(Counters.Division(pos_i, all_i), logFi);
								foreach (E p in acc.KeySet())
								{
									currentPatternWeights4Label.SetCount(p, conf.GetCount(p));
								}
							}
							else
							{
								if (patternScoring.Equals(GetPatternsFromDataMultiClass.PatternScoring.LinICML03))
								{
									ICounter<E> acc = Counters.Division(pos_i, Counters.Add(pos_i, neg_i));
									double thetaPrecision = 0.8;
									Counters.RetainAbove(acc, thetaPrecision);
									ICounter<E> conf = Counters.Product(Counters.Division(Counters.Add(pos_i, Counters.Scale(neg_i, -1)), all_i), logFi);
									foreach (E p in acc.KeySet())
									{
										currentPatternWeights4Label.SetCount(p, conf.GetCount(p));
									}
								}
								else
								{
									throw new Exception("not implemented " + patternScoring + " . check spelling!");
								}
							}
						}
					}
				}
			}
			return currentPatternWeights4Label;
		}
	}
}
