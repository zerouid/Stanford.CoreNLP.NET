using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.Hybrid.Sieve;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Hybrid
{
	/// <summary>Properties for the hybrid coref system.</summary>
	/// <author>Heeyoung Lee</author>
	/// <author>Kevin Clark</author>
	public class HybridCorefProperties
	{
		public const string LangProp = "coref.language";

		private const string SievesProp = "coref.sieves";

		private const string ScoreProp = "coref.doScore";

		private const string ThreadsProp = "coref.threadCount";

		private const string PostprocessingProp = "coref.postprocessing";

		private const string SeedProp = "coref.seed";

		private const string ConllAutoProp = "coref.conll.auto";

		private const string UseSemanticsProp = "coref.useSemantics";

		public const string CurrentSieveForTrainProp = "coref.currentSieveForTrain";

		private const string StoreTraindataProp = "coref.storeTrainData";

		private const string AddMissingAnnotations = "coref.addMissingAnnotations";

		private const string DebugProp = "coref.debug";

		public const string LogProp = "coref.logFile";

		private const string TimerProp = "coref.checkTime";

		private const string MemoryProp = "coref.checkMemory";

		private const string PrintMdlogProp = "coref.print.md.log";

		private const string CalculateImportanceProp = "coref.calculateFeatureImportance";

		private const string DoAnalysisProp = "coref.analysis.doAnalysis";

		private const string AnalysisSkipMtypeProp = "coref.analysis.skip.mType";

		private const string AnalysisSkipAtypeProp = "coref.analysis.skip.aType";

		public const string StatesProp = "coref.states";

		public const string DemonymProp = "coref.demonym";

		public const string AnimateProp = "coref.animate";

		public const string InanimateProp = "coref.inanimate";

		public const string MaleProp = "coref.male";

		public const string NeutralProp = "coref.neutral";

		public const string FemaleProp = "coref.female";

		public const string PluralProp = "coref.plural";

		public const string SingularProp = "coref.singular";

		public const string GenderNumberProp = "coref.big.gender.number";

		public const string CountriesProp = "coref.countries";

		public const string StatesProvincesProp = "coref.states.provinces";

		public const string DictListProp = "coref.dictlist";

		public const string DictPmiProp = "coref.dictpmi";

		public const string SignaturesProp = "coref.signatures";

		public const string LoadWordEmbeddingProp = "coref.loadWordEmbedding";

		private const string Word2vecProp = "coref.path.word2vec";

		private const string Word2vecSerializedProp = "coref.path.word2vecSerialized";

		private const string PathSerializedProp = "coref.path.serialized";

		private const string PathModelProp = "coref.SIEVENAME.model";

		private const string ClassifierTypeProp = "coref.SIEVENAME.classifierType";

		private const string NumTreeProp = "coref.SIEVENAME.numTrees";

		private const string NumFeaturesProp = "coref.SIEVENAME.numFeatures";

		private const string TreeDepthProp = "coref.SIEVENAME.treeDepth";

		private const string MaxSentDistProp = "coref.SIEVENAME.maxSentDist";

		private const string MtypeProp = "coref.SIEVENAME.mType";

		private const string AtypeProp = "coref.SIEVENAME.aType";

		private const string DownsampleRateProp = "coref.SIEVENAME.downsamplingRate";

		private const string ThresFeaturecountProp = "coref.SIEVENAME.thresFeatureCount";

		private const string FeatureSelectionProp = "coref.SIEVENAME.featureSelection";

		private const string ThresMergeProp = "coref.SIEVENAME.merge.thres";

		private const string ThresFeatureSelectionProp = "coref.SIEVENAME.pmi.thres";

		private const string DefaultPronounAgreementProp = "coref.defaultPronounAgreement";

		private const string UseBasicFeaturesProp = "coref.SIEVENAME.useBasicFeatures";

		private const string CombineObjectroleProp = "coref.SIEVENAME.combineObjectRole";

		private const string UseMdFeaturesProp = "coref.SIEVENAME.useMentionDetectionFeatures";

		private const string UseDcorefruleFeaturesProp = "coref.SIEVENAME.useDcorefRuleFeatures";

		private const string UsePosFeaturesProp = "coref.SIEVENAME.usePOSFeatures";

		private const string UseLexicalFeaturesProp = "coref.SIEVENAME.useLexicalFeatures";

		private const string UseWordEmbeddingFeaturesProp = "coref.SIEVENAME.useWordEmbeddingFeatures";

		public static readonly Locale LanguageDefault = Locale.English;

		/// <summary>if true, remove appositives, predicate nominatives in post processing</summary>
		public const bool RemoveAppositionPredicatenominatives = true;

		/// <summary>if true, remove singletons in post processing</summary>
		public const bool RemoveSingletons = true;

		private static readonly ICollection<string> dcorefSieveNames = new HashSet<string>(Arrays.AsList("MarkRole", "DiscourseMatch", "ExactStringMatch", "RelaxedExactStringMatch", "PreciseConstructs", "StrictHeadMatch1", "StrictHeadMatch2", "StrictHeadMatch3"
			, "StrictHeadMatch4", "RelaxedHeadMatch", "PronounMatch", "SpeakerMatch", "ChineseHeadMatch"));

		private HybridCorefProperties()
		{
		}

		// public enum CorefInputType { RAW, CONLL, ACE, MUC }
		// general
		// load semantics if true
		// logging & system check & analysis
		// data & io
		// models
		// sieve option
		// features
		// current list of dcoref sieves
		// static methods/ constants
		public static bool DoScore(Properties props)
		{
			return PropertiesUtils.GetBool(props, ScoreProp, false);
		}

		public static bool CheckTime(Properties props)
		{
			return PropertiesUtils.GetBool(props, TimerProp, false);
		}

		public static bool CheckMemory(Properties props)
		{
			return PropertiesUtils.GetBool(props, MemoryProp, false);
		}

		public static int GetThreadCounts(Properties props)
		{
			return PropertiesUtils.GetInt(props, ThreadsProp, Runtime.GetRuntime().AvailableProcessors());
		}

		public static Locale GetLanguage(Properties props)
		{
			string lang = PropertiesUtils.GetString(props, LangProp, "en");
			if (Sharpen.Runtime.EqualsIgnoreCase(lang, "en") || Sharpen.Runtime.EqualsIgnoreCase(lang, "english"))
			{
				return Locale.English;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(lang, "zh") || Sharpen.Runtime.EqualsIgnoreCase(lang, "chinese"))
				{
					return Locale.Chinese;
				}
				else
				{
					throw new Exception("unsupported language");
				}
			}
		}

		public static bool PrintMDLog(Properties props)
		{
			return PropertiesUtils.GetBool(props, PrintMdlogProp, false);
		}

		public static bool DoPostProcessing(Properties props)
		{
			return PropertiesUtils.GetBool(props, PostprocessingProp, false);
		}

		/// <summary>if true, use conll auto files, else use conll gold files</summary>
		public static bool UseCoNLLAuto(Properties props)
		{
			return PropertiesUtils.GetBool(props, ConllAutoProp, true);
		}

		public static string GetPathModel(Properties props, string sievename)
		{
			return props.GetProperty(PathSerializedProp) + File.separator + props.GetProperty(PathModelProp.Replace("SIEVENAME", sievename), "MISSING_MODEL_FOR_" + sievename);
		}

		public static bool Debug(Properties props)
		{
			return PropertiesUtils.GetBool(props, DebugProp, false);
		}

		public static Sieve.ClassifierType GetClassifierType(Properties props, string sievename)
		{
			if (dcorefSieveNames.Contains(sievename))
			{
				return Sieve.ClassifierType.Rule;
			}
			if (sievename.ToLower().EndsWith("-rf"))
			{
				return Sieve.ClassifierType.Rf;
			}
			if (sievename.ToLower().EndsWith("-oracle"))
			{
				return Sieve.ClassifierType.Oracle;
			}
			string classifierType = PropertiesUtils.GetString(props, ClassifierTypeProp.Replace("SIEVENAME", sievename), null);
			return Sieve.ClassifierType.ValueOf(classifierType);
		}

		public static double GetMergeThreshold(Properties props, string sievename)
		{
			string key = ThresMergeProp.Replace("SIEVENAME", sievename);
			return PropertiesUtils.GetDouble(props, key, 0.3);
		}

		public static void SetMergeThreshold(Properties props, string sievename, double value)
		{
			string key = ThresMergeProp.Replace("SIEVENAME", sievename);
			props.SetProperty(key, value.ToString());
		}

		public static int GetNumTrees(Properties props, string sievename)
		{
			return PropertiesUtils.GetInt(props, NumTreeProp.Replace("SIEVENAME", sievename), 100);
		}

		public static int GetSeed(Properties props)
		{
			return PropertiesUtils.GetInt(props, SeedProp, 1);
		}

		public static int GetNumFeatures(Properties props, string sievename)
		{
			return PropertiesUtils.GetInt(props, NumFeaturesProp.Replace("SIEVENAME", sievename), 30);
		}

		public static int GetTreeDepth(Properties props, string sievename)
		{
			return PropertiesUtils.GetInt(props, TreeDepthProp.Replace("SIEVENAME", sievename), 0);
		}

		public static bool CalculateFeatureImportance(Properties props)
		{
			return PropertiesUtils.GetBool(props, CalculateImportanceProp, false);
		}

		public static int GetMaxSentDistForSieve(Properties props, string sievename)
		{
			return PropertiesUtils.GetInt(props, MaxSentDistProp.Replace("SIEVENAME", sievename), 1000);
		}

		public static ICollection<Dictionaries.MentionType> GetMentionType(Properties props, string sievename)
		{
			return GetMentionTypes(props, MtypeProp.Replace("SIEVENAME", sievename));
		}

		public static ICollection<Dictionaries.MentionType> GetAntecedentType(Properties props, string sievename)
		{
			return GetMentionTypes(props, AtypeProp.Replace("SIEVENAME", sievename));
		}

		private static ICollection<Dictionaries.MentionType> GetMentionTypes(Properties props, string propKey)
		{
			if (!props.Contains(propKey) || Sharpen.Runtime.EqualsIgnoreCase(props.GetProperty(propKey), "all"))
			{
				return new HashSet<Dictionaries.MentionType>(Arrays.AsList(Dictionaries.MentionType.Values()));
			}
			ICollection<Dictionaries.MentionType> types = new HashSet<Dictionaries.MentionType>();
			foreach (string type in props.GetProperty(propKey).Trim().Split(",\\s*"))
			{
				if (type.ToLower().Matches("i|you|we|they|it|she|he"))
				{
					type = "PRONOMINAL";
				}
				types.Add(Dictionaries.MentionType.ValueOf(type));
			}
			return types;
		}

		public static double GetDownsamplingRate(Properties props, string sievename)
		{
			return PropertiesUtils.GetDouble(props, DownsampleRateProp.Replace("SIEVENAME", sievename), 1);
		}

		public static int GetFeatureCountThreshold(Properties props, string sievename)
		{
			return PropertiesUtils.GetInt(props, ThresFeaturecountProp.Replace("SIEVENAME", sievename), 20);
		}

		public static bool UseBasicFeatures(Properties props, string sievename)
		{
			return PropertiesUtils.GetBool(props, UseBasicFeaturesProp.Replace("SIEVENAME", sievename), true);
		}

		public static bool CombineObjectRoles(Properties props, string sievename)
		{
			return PropertiesUtils.GetBool(props, CombineObjectroleProp.Replace("SIEVENAME", sievename), true);
		}

		public static bool UseMentionDetectionFeatures(Properties props, string sievename)
		{
			return PropertiesUtils.GetBool(props, UseMdFeaturesProp.Replace("SIEVENAME", sievename), true);
		}

		public static bool UseDcorefRules(Properties props, string sievename)
		{
			return PropertiesUtils.GetBool(props, UseDcorefruleFeaturesProp.Replace("SIEVENAME", sievename), true);
		}

		public static bool UsePOSFeatures(Properties props, string sievename)
		{
			return PropertiesUtils.GetBool(props, UsePosFeaturesProp.Replace("SIEVENAME", sievename), true);
		}

		public static bool UseLexicalFeatures(Properties props, string sievename)
		{
			return PropertiesUtils.GetBool(props, UseLexicalFeaturesProp.Replace("SIEVENAME", sievename), true);
		}

		public static bool UseWordEmbedding(Properties props, string sievename)
		{
			return PropertiesUtils.GetBool(props, UseWordEmbeddingFeaturesProp.Replace("SIEVENAME", sievename), true);
		}

		private static ICollection<string> GetMentionTypeStr(Properties props, string sievename, string whichMention)
		{
			ICollection<string> strs = Generics.NewHashSet();
			string propKey = whichMention;
			if (!props.Contains(propKey))
			{
				string prefix = "coref." + sievename + ".";
				propKey = prefix + propKey;
			}
			if (props.Contains(propKey))
			{
				Sharpen.Collections.AddAll(strs, Arrays.AsList(props.GetProperty(propKey).Split(",")));
			}
			return strs;
		}

		public static ICollection<string> GetMentionTypeStr(Properties props, string sievename)
		{
			return GetMentionTypeStr(props, sievename, "mType");
		}

		public static ICollection<string> GetAntecedentTypeStr(Properties props, string sievename)
		{
			return GetMentionTypeStr(props, sievename, "aType");
		}

		public static string GetSieves(Properties props)
		{
			return PropertiesUtils.GetString(props, SievesProp, "SpeakerMatch,PreciseConstructs,pp-rf,cc-rf,pc-rf,ll-rf,pr-rf");
		}

		public static string GetPathSerialized(Properties props)
		{
			return props.GetProperty(PathSerializedProp);
		}

		public static bool DoPMIFeatureSelection(Properties props, string sievename)
		{
			return Sharpen.Runtime.EqualsIgnoreCase(PropertiesUtils.GetString(props, FeatureSelectionProp.Replace("SIEVENAME", sievename), "pmi"), "pmi");
		}

		public static double GetPMIThres(Properties props, string sievename)
		{
			return PropertiesUtils.GetDouble(props, ThresFeatureSelectionProp.Replace("SIEVENAME", sievename), 0.0001);
		}

		public static bool DoAnalysis(Properties props)
		{
			return PropertiesUtils.GetBool(props, DoAnalysisProp, false);
		}

		public static string GetSkipMentionType(Properties props)
		{
			return PropertiesUtils.GetString(props, AnalysisSkipMtypeProp, null);
		}

		public static string GetSkipAntecedentType(Properties props)
		{
			return PropertiesUtils.GetString(props, AnalysisSkipAtypeProp, null);
		}

		public static bool UseSemantics(Properties props)
		{
			return PropertiesUtils.GetBool(props, UseSemanticsProp, false);
		}

		public static string GetPathSerializedWordVectors(Properties props)
		{
			return PropertiesUtils.GetString(props, Word2vecSerializedProp, "/u/scr/nlp/data/coref/wordvectors/en/vector.ser.gz");
		}

		public static string GetCurrentSieveForTrain(Properties props)
		{
			return PropertiesUtils.GetString(props, CurrentSieveForTrainProp, null);
		}

		//  public static String getCurrentSieve(Properties props) {
		//    return PropertiesUtils.getString(props, CURRENT_SIEVE_PROP, null);
		//  }
		public static bool LoadWordEmbedding(Properties props)
		{
			return PropertiesUtils.GetBool(props, LoadWordEmbeddingProp, true);
		}

		public static string GetPathWord2Vec(Properties props)
		{
			return PropertiesUtils.GetString(props, Word2vecProp, null);
		}

		public static string GetGenderNumber(Properties props)
		{
			return PropertiesUtils.GetString(props, GenderNumberProp, "edu/stanford/nlp/models/dcoref/gender.data.gz");
		}

		public static bool StoreTrainData(Properties props)
		{
			return PropertiesUtils.GetBool(props, StoreTraindataProp, false);
		}

		public static bool UseDefaultPronounAgreement(Properties props)
		{
			return PropertiesUtils.GetBool(props, Edu.Stanford.Nlp.Coref.Hybrid.HybridCorefProperties.DefaultPronounAgreementProp, false);
		}

		public static bool AddMissingAnnotations(Properties props)
		{
			return PropertiesUtils.GetBool(props, AddMissingAnnotations, false);
		}
	}
}
