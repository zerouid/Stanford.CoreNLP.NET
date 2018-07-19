using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>Used if patternScoring flag is set to F1 with the seed pattern.</summary>
	/// <remarks>
	/// Used if patternScoring flag is set to F1 with the seed pattern. See
	/// <see cref="Edu.Stanford.Nlp.Patterns.GetPatternsFromDataMultiClass.PatternScoring"/>
	/// enum.
	/// </remarks>
	/// <author>Sonal Gupta (sonalg@stanford.edu)</author>
	public class ScorePatternsF1<E> : ScorePatterns<E>
	{
		internal ICounter<CandidatePhrase> p0Set = null;

		internal E p0;

		public ScorePatternsF1(ConstantsAndVariables constVars, GetPatternsFromDataMultiClass.PatternScoring patternScoring, string label, ICollection<CandidatePhrase> allCandidatePhrases, TwoDimensionalCounter<E, CandidatePhrase> patternsandWords4Label
			, TwoDimensionalCounter<E, CandidatePhrase> negPatternsandWords4Label, TwoDimensionalCounter<E, CandidatePhrase> unLabeledPatternsandWords4Label, Properties props, ICounter<CandidatePhrase> p0Set, E p0)
			: base(constVars, patternScoring, label, allCandidatePhrases, patternsandWords4Label, negPatternsandWords4Label, unLabeledPatternsandWords4Label, props)
		{
			this.p0 = p0;
			this.p0Set = p0Set;
		}

		public override void SetUp(Properties props)
		{
		}

		public override ICounter<E> Score()
		{
			ICounter<E> specificity = new ClassicCounter<E>();
			ICounter<E> sensitivity = new ClassicCounter<E>();
			if (p0Set.KeySet().Count == 0)
			{
				throw new Exception("how come p0set size is empty for " + p0 + "?");
			}
			foreach (KeyValuePair<E, ClassicCounter<CandidatePhrase>> en in patternsandWords4Label.EntrySet())
			{
				int common = CollectionUtils.Intersection(en.Value.KeySet(), p0Set.KeySet()).Count;
				if (common == 0)
				{
					continue;
				}
				if (en.Value.KeySet().Count == 0)
				{
					throw new Exception("how come counter for " + en.Key + " is empty?");
				}
				specificity.SetCount(en.Key, common / (double)en.Value.KeySet().Count);
				sensitivity.SetCount(en.Key, common / (double)p0Set.Size());
			}
			Counters.RetainNonZeros(specificity);
			Counters.RetainNonZeros(sensitivity);
			ICounter<E> add = Counters.Add(sensitivity, specificity);
			ICounter<E> product = Counters.Product(sensitivity, specificity);
			Counters.RetainNonZeros(product);
			Counters.RetainKeys(product, add.KeySet());
			ICounter<E> finalPat = Counters.Scale(Counters.Division(product, add), 2);
			return finalPat;
		}
	}
}
