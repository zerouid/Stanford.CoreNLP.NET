namespace Edu.Stanford.Nlp.Dcoref
{
	public class Constants
	{
		protected internal Constants()
		{
		}

		/// <summary>if true, use truecase annotator</summary>
		public const bool UseTruecase = false;

		/// <summary>if true, use gold speaker tags</summary>
		public const bool UseGoldSpeakerTags = false;

		/// <summary>if false, use Stanford NER to predict NE labels</summary>
		public const bool UseGoldNe = false;

		/// <summary>if false, use Stanford parse to parse</summary>
		public const bool UseGoldParses = false;

		/// <summary>if false, use Stanford tagger to tag</summary>
		public const bool UseGoldPos = false;

		/// <summary>if false, use mention prediction</summary>
		public const bool UseGoldMentions = false;

		/// <summary>if true, use given mention boundaries</summary>
		public const bool UseGoldMentionBoundaries = false;

		/// <summary>Flag for using discourse salience</summary>
		public const bool UseDiscourseSalience = true;

		/// <summary>Use person attributes in pronoun matching</summary>
		public const bool UseDiscourseConstraints = true;

		/// <summary>if true, remove appositives, predicate nominatives in post processing</summary>
		public const bool RemoveAppositionPredicatenominatives = true;

		/// <summary>if true, remove singletons in post processing</summary>
		public const bool RemoveSingletons = true;

		/// <summary>if true, read *auto_conll, if false, read *gold_conll</summary>
		public const bool UseConllAuto = true;

		/// <summary>if true, print in conll output format</summary>
		public const bool PrintConllOutput = false;

		/// <summary>Default path for conll scorer script</summary>
		public const string conllMentionEvalScript = "/u/scr/nlp/data/conll-2011/scorer/v4/scorer.pl";

		/// <summary>if true, skip coreference resolution.</summary>
		/// <remarks>if true, skip coreference resolution. do mention detection only</remarks>
		public const bool SkipCoref = false;

		/// <summary>Default sieve passes</summary>
		public const string Sievepasses = "MarkRole, DiscourseMatch, ExactStringMatch, RelaxedExactStringMatch, PreciseConstructs, StrictHeadMatch1, StrictHeadMatch2, StrictHeadMatch3, StrictHeadMatch4, RelaxedHeadMatch, PronounMatch";

		/// <summary>Use animacy list (Bergsma and Lin, 2006; Ji and Lin, 2009)</summary>
		public const bool UseAnimacyList = true;

		/// <summary>Share attributes between coreferent mentions</summary>
		public const bool ShareAttributes = true;

		/// <summary>Whether or not the RuleBasedCorefMentionFinder can reparse a phrase to find its head</summary>
		public const bool AllowReparsing = true;

		/// <summary>Default language</summary>
		public static readonly Locale LanguageDefault = Locale.English;

		public const string LanguageProp = "coref.language";

		public const string StatesProp = "dcoref.states";

		public const string DemonymProp = "dcoref.demonym";

		public const string AnimateProp = "dcoref.animate";

		public const string InanimateProp = "dcoref.inanimate";

		public const string MaleProp = "dcoref.male";

		public const string NeutralProp = "dcoref.neutral";

		public const string FemaleProp = "dcoref.female";

		public const string PluralProp = "dcoref.plural";

		public const string SingularProp = "dcoref.singular";

		public const string SievesProp = "dcoref.sievePasses";

		public const string MentionFinderProp = "dcoref.mentionFinder";

		public const string MentionFinderPropfileProp = "dcoref.mentionFinder.props";

		public const string ScoreProp = "dcoref.score";

		public const string LogProp = "dcoref.logFile";

		public const string Ace2004Prop = "dcoref.ace2004";

		public const string Ace2005Prop = "dcoref.ace2005";

		public const string MucProp = "dcoref.muc";

		public const string Conll2011Prop = "dcoref.conll2011";

		public const string ConllOutputProp = "dcoref.conll.output";

		public const string ConllScorer = "dcoref.conll.scorer";

		public const string ParserModelProp = "parse.model";

		public const string ParserMaxlenProp = "parse.maxlen";

		public const string PostprocessingProp = "dcoref.postprocessing";

		public const string MaxdistProp = "dcoref.maxdist";

		public const string ReplicateconllProp = "dcoref.replicate.conll";

		public const string GenderNumberProp = "dcoref.big.gender.number";

		public const string CountriesProp = "dcoref.countries";

		public const string StatesProvincesProp = "dcoref.states.provinces";

		public const string OptimizeSievesProp = "dcoref.optimize.sieves";

		public const string OptimizeSievesKeepOrderProp = "dcoref.optimize.sieves.keepOrder";

		public const string OptimizeSievesScoreProp = "dcoref.optimize.sieves.score";

		public const string RunDistCmdProp = "dcoref.dist.cmd";

		public const string RunDistCmdWorkDir = "dcoref.dist.workdir";

		public const string ScoreFileProp = "dcoref.score.output";

		public const string SingletonProp = "dcoref.singleton.predictor";

		public const string SingletonModelProp = "dcoref.singleton.model";

		public const string DictListProp = "dcoref.dictlist";

		public const string DictPmiProp = "dcoref.dictpmi";

		public const string SignaturesProp = "dcoref.signatures";

		public const string AllowReparsingProp = "dcoref.allowReparsing";

		public const int MonitorDistCmdFinishedWaitMillis = 60000;

		// static class but extended by jcoref
		//
		// note that default paths for all dictionaries used are in
		// pipeline.DefaultPaths
		//
		/// <summary>print the values of variables in this class</summary>
		public static void PrintConstants(Logger logger)
		{
			logger.Info("USE_ANIMACY_LIST on");
			logger.Info("USE_ANIMACY_LIST on");
			logger.Info("use discourse salience");
			logger.Info("not use truecase annotator");
			logger.Info("USE_DISCOURSE_CONSTRAINTS on");
			logger.Info("USE_GOLD_POS off");
			logger.Info("use Stanford NER");
			logger.Info("USE_GOLD_PARSES off");
			logger.Info("USE_GOLD_SPEAKER_TAGS off");
			logger.Info("USE_GOLD_MENTIONS off");
			logger.Info("USE_GOLD_MENTION_BOUNDARIES off");
			logger.Info("use conll auto set -> if GOLD_NE, GOLD_PARSE, GOLD_POS, etc turned on, use auto");
			logger.Info("REMOVE_SINGLETONS on");
			logger.Info("REMOVE_APPOSITION_PREDICATENOMINATIVES on");
			logger.Info("=================================================================");
		}
	}
}
