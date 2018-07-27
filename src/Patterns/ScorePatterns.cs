using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;



namespace Edu.Stanford.Nlp.Patterns
{
	public abstract class ScorePatterns<E>
	{
		internal ConstantsAndVariables constVars;

		protected internal GetPatternsFromDataMultiClass.PatternScoring patternScoring;

		protected internal Properties props;

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public abstract ICounter<E> Score();

		protected internal TwoDimensionalCounter<E, CandidatePhrase> patternsandWords4Label = new TwoDimensionalCounter<E, CandidatePhrase>();

		protected internal TwoDimensionalCounter<E, CandidatePhrase> negPatternsandWords4Label = new TwoDimensionalCounter<E, CandidatePhrase>();

		protected internal TwoDimensionalCounter<E, CandidatePhrase> unLabeledPatternsandWords4Label = new TwoDimensionalCounter<E, CandidatePhrase>();

		protected internal string label;

		protected internal ICollection<CandidatePhrase> allCandidatePhrases;

		public ScorePatterns(ConstantsAndVariables constVars, GetPatternsFromDataMultiClass.PatternScoring patternScoring, string label, ICollection<CandidatePhrase> allCandidatePhrases, TwoDimensionalCounter<E, CandidatePhrase> patternsandWords4Label
			, TwoDimensionalCounter<E, CandidatePhrase> negPatternsandWords4Label, TwoDimensionalCounter<E, CandidatePhrase> unLabeledPatternsandWords4Label, Properties props)
		{
			// protected TwoDimensionalCounter<SurfacePattern, String>
			// posnegPatternsandWords4Label = new TwoDimensionalCounter<SurfacePattern,
			// String>();
			//protected TwoDimensionalCounter<E, String> negandUnLabeledPatternsandWords4Label = new TwoDimensionalCounter<E, String>();
			//protected TwoDimensionalCounter<E, String> allPatternsandWords4Label = new TwoDimensionalCounter<E, String>();
			this.constVars = constVars;
			this.patternScoring = patternScoring;
			this.label = label;
			this.allCandidatePhrases = allCandidatePhrases;
			this.patternsandWords4Label = patternsandWords4Label;
			this.negPatternsandWords4Label = negPatternsandWords4Label;
			this.unLabeledPatternsandWords4Label = unLabeledPatternsandWords4Label;
			this.props = props;
		}

		public abstract void SetUp(Properties props);
	}
}
