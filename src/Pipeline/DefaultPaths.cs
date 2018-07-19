using System.Reflection;
using Edu.Stanford.Nlp.Parser.Nndep;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// Default model paths for StanfordCoreNLP
	/// All these paths point to files distributed with the model jar file (stanford-corenlp-models-*.jar)
	/// </summary>
	public class DefaultPaths
	{
		public const string DefaultPosModel = "edu/stanford/nlp/models/pos-tagger/english-left3words/english-left3words-distsim.tagger";

		public const string DefaultParserModel = "edu/stanford/nlp/models/lexparser/englishPCFG.ser.gz";

		public const string DefaultDependencyParserModel = DependencyParser.DefaultModel;

		public const string DefaultNerThreeclassModel = "edu/stanford/nlp/models/ner/english.all.3class.distsim.crf.ser.gz";

		public const string DefaultNerConllModel = "edu/stanford/nlp/models/ner/english.conll.4class.distsim.crf.ser.gz";

		public const string DefaultNerMucModel = "edu/stanford/nlp/models/ner/english.muc.7class.distsim.crf.ser.gz";

		public const string DefaultNerGazetteMapping = "edu/stanford/nlp/models/ner/regexner.patterns";

		public const string DefaultRegexnerRules = "edu/stanford/nlp/models/kbp/english/gazetteers/regexner_caseless.tab";

		public const string DefaultGenderFirstNames = "edu/stanford/nlp/models/gender/first_name_map_small";

		public const string DefaultTruecaseModel = "edu/stanford/nlp/models/truecase/truecasing.fast.caseless.qn.ser.gz";

		public const string DefaultTruecaseDisambiguationList = "edu/stanford/nlp/models/truecase/MixDisambiguation.list";

		public const string DefaultDcorefAnimate = "edu/stanford/nlp/models/dcoref/animate.unigrams.txt";

		public const string DefaultDcorefDemonym = "edu/stanford/nlp/models/dcoref/demonyms.txt";

		public const string DefaultDcorefInanimate = "edu/stanford/nlp/models/dcoref/inanimate.unigrams.txt";

		public const string DefaultDcorefStates = "edu/stanford/nlp/models/dcoref/state-abbreviations.txt";

		public const string DefaultDcorefCountries = "edu/stanford/nlp/models/dcoref/countries";

		public const string DefaultDcorefStatesAndProvinces = "edu/stanford/nlp/models/dcoref/statesandprovinces";

		public const string DefaultDcorefGenderNumber = "edu/stanford/nlp/models/dcoref/gender.map.ser.gz";

		public const string DefaultDcorefSingletonModel = "edu/stanford/nlp/models/dcoref/singleton.predictor.ser";

		public const string DefaultDcorefDict1 = "edu/stanford/nlp/models/dcoref/coref.dict1.tsv";

		public const string DefaultDcorefDict2 = "edu/stanford/nlp/models/dcoref/coref.dict2.tsv";

		public const string DefaultDcorefDict3 = "edu/stanford/nlp/models/dcoref/coref.dict3.tsv";

		public const string DefaultDcorefDict4 = "edu/stanford/nlp/models/dcoref/coref.dict4.tsv";

		public const string DefaultDcorefNeSignatures = "edu/stanford/nlp/models/dcoref/ne.signatures.txt";

		public const string DefaultNflEntityModel = "edu/stanford/nlp/models/machinereading/nfl/nfl_entity_model.ser";

		public const string DefaultNflRelationModel = "edu/stanford/nlp/models/machinereading/nfl/nfl_relation_model.ser";

		public const string DefaultNflGazetteer = "edu/stanford/nlp/models/machinereading/nfl/NFLgazetteer.txt";

		public const string DefaultSupRelationExRelationModel = "edu/stanford/nlp/models/supervised_relation_extractor/roth_relation_model_pipelineNER.ser";

		public const string DefaultNaturalliAffinities = "edu/stanford/nlp/models/naturalli/affinities";

		public const string DefaultOpenieClauseSearcher = "edu/stanford/nlp/models/naturalli/clauseSearcherModel.ser.gz";

		public const string DefaultKbpClassifier = "edu/stanford/nlp/models/kbp/english/tac-re-lr.ser.gz";

		public const string DefaultKbpRegexnerCased = "edu/stanford/nlp/models/kbp/english/gazetteers/regexner_cased.tab";

		public const string DefaultKbpRegexnerCaseless = "edu/stanford/nlp/models/kbp/english/gazetteers/regexner_caseless.tab";

		public const string DefaultKbpSemgrexDir = "edu/stanford/nlp/models/kbp/english/semgrex";

		public const string DefaultKbpTokensregexDir = "edu/stanford/nlp/models/kbp/english/tokensregex";

		public const string DefaultKbpTokensregexNerSettings = "ignorecase=true,validpospattern=^(NN|JJ).*,edu/stanford/nlp/models/kbp/english/gazetteers/regexner_caseless.tab;" + "edu/stanford/nlp/models/kbp/english/gazetteers/regexner_cased.tab";

		public const string DefaultWikidictTsv = "edu/stanford/nlp/models/kbp/english/wikidict.tab.gz";

		private DefaultPaths()
		{
		}

		// Used in a script
		// If you change this key, also change bin/mkopenie.sh
		// If you change this key, also change bin/mkopenie.sh
		/// <summary>Go through all of the paths via reflection, and print them out in a TSV format.</summary>
		/// <remarks>
		/// Go through all of the paths via reflection, and print them out in a TSV format.
		/// This is useful for command line scripts.
		/// </remarks>
		/// <param name="args">Ignored.</param>
		/// <exception cref="System.MemberAccessException"/>
		public static void Main(string[] args)
		{
			foreach (FieldInfo field in typeof(Edu.Stanford.Nlp.Pipeline.DefaultPaths).GetFields())
			{
				System.Console.Out.WriteLine(field.Name + "\t" + field.GetValue(null));
			}
		}
	}
}
