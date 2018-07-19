using System;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref
{
	/// <summary>Manages the properties for running coref.</summary>
	/// <author>Kevin Clark</author>
	public class CorefProperties
	{
		private CorefProperties()
		{
		}

		public enum CorefAlgorithmType
		{
			Clustering,
			Statistical,
			Neural,
			Hybrid
		}

		// static methods
		//---------- Coreference Algorithms ----------
		public static CorefProperties.CorefAlgorithmType Algorithm(Properties props)
		{
			string type = PropertiesUtils.GetString(props, "coref.algorithm", GetLanguage(props) == Locale.English ? "statistical" : "neural");
			return CorefProperties.CorefAlgorithmType.ValueOf(type.ToUpper());
		}

		//---------- General Coreference Options ----------
		/// <summary>
		/// When conll() is true, coref models:
		/// <ul>
		/// <li>Use provided POS, NER, Parsing, etc.
		/// </summary>
		/// <remarks>
		/// When conll() is true, coref models:
		/// <ul>
		/// <li>Use provided POS, NER, Parsing, etc. (instead of using CoreNLP annotators)</li>
		/// <li>Use provided speaker annotations</li>
		/// <li>Use provided document type and genre information</li>
		/// </ul>
		/// </remarks>
		public static bool Conll(Properties props)
		{
			return PropertiesUtils.GetBool(props, "coref.conll", false);
		}

		public static bool UseConstituencyParse(Properties props)
		{
			return PropertiesUtils.GetBool(props, "coref.useConstituencyParse", Algorithm(props) != CorefProperties.CorefAlgorithmType.Statistical || Conll(props));
		}

		public static bool Verbose(Properties props)
		{
			return PropertiesUtils.GetBool(props, "coref.verbose", false);
		}

		public static bool RemoveSingletonClusters(Properties props)
		{
			return PropertiesUtils.GetBool(props, "coref.removeSingletonClusters", true);
		}

		// ---------- Heuristic Mention Filtering ----------
		public static int MaxMentionDistance(Properties props)
		{
			return PropertiesUtils.GetInt(props, "coref.maxMentionDistance", Conll(props) ? int.MaxValue : 50);
		}

		public static int MaxMentionDistanceWithStringMatch(Properties props)
		{
			return PropertiesUtils.GetInt(props, "coref.maxMentionDistanceWithStringMatch", 500);
		}

		public enum MentionDetectionType
		{
			Rule,
			Hybrid,
			Dependency
		}

		// ---------- Mention Detection ----------
		public static CorefProperties.MentionDetectionType MdType(Properties props)
		{
			string type = PropertiesUtils.GetString(props, "coref.md.type", UseConstituencyParse(props) ? "RULE" : "dep");
			if (Sharpen.Runtime.EqualsIgnoreCase(type, "dep"))
			{
				type = "DEPENDENCY";
			}
			return CorefProperties.MentionDetectionType.ValueOf(type.ToUpper());
		}

		public static string GetMentionDetectionModel(Properties props)
		{
			return PropertiesUtils.GetString(props, "coref.md.model", UseConstituencyParse(props) ? "edu/stanford/nlp/models/coref/md-model.ser" : "edu/stanford/nlp/models/coref/md-model-dep.ser.gz");
		}

		public static bool IsMentionDetectionTraining(Properties props)
		{
			return PropertiesUtils.GetBool(props, "coref.md.isTraining", false);
		}

		public static void SetMentionDetectionTraining(Properties props, bool val)
		{
			props.SetProperty("coref.md.isTraining", val.ToString());
		}

		public static bool RemoveNestedMentions(Properties props)
		{
			return PropertiesUtils.GetBool(props, "removeNestedMentions", true);
		}

		public static void SetRemoveNestedMentions(Properties props, bool val)
		{
			props.SetProperty("removeNestedMentions", val.ToString());
		}

		public static bool LiberalMD(Properties props)
		{
			return PropertiesUtils.GetBool(props, "coref.md.liberalMD", false);
		}

		public static bool UseGoldMentions(Properties props)
		{
			return PropertiesUtils.GetBool(props, "coref.md.useGoldMentions", false);
		}

		public const string OutputPathProp = "coref.conllOutputPath";

		// ---------- Input and Output Data ----------
		public static string ConllOutputPath(Properties props)
		{
			string returnPath = props.GetProperty("coref.conllOutputPath", "/u/scr/nlp/coref/logs/");
			if (!returnPath.EndsWith("/"))
			{
				returnPath += "/";
			}
			return returnPath;
		}

		public enum Dataset
		{
			Train,
			Dev,
			Test
		}

		public static void SetInput(Properties props, CorefProperties.Dataset d)
		{
			props.SetProperty("coref.inputPath", d == CorefProperties.Dataset.Train ? GetTrainDataPath(props) : (d == CorefProperties.Dataset.Dev ? GetDevDataPath(props) : GetTestDataPath(props)));
		}

		private static string GetDataPath(Properties props)
		{
			string returnPath = props.GetProperty("coref.data", "/u/scr/nlp/data/conll-2012/");
			if (!returnPath.EndsWith("/"))
			{
				returnPath += "/";
			}
			return returnPath;
		}

		public static string GetTrainDataPath(Properties props)
		{
			return props.GetProperty("coref.trainData", GetDataPath(props) + "v4/data/train/data/" + GetLanguageStr(props) + "/annotations/");
		}

		public static string GetDevDataPath(Properties props)
		{
			return props.GetProperty("coref.devData", GetDataPath(props) + "v4/data/development/data/" + GetLanguageStr(props) + "/annotations/");
		}

		public static string GetTestDataPath(Properties props)
		{
			return props.GetProperty("coref.testData", GetDataPath(props) + "v9/data/test/data/" + GetLanguageStr(props) + "/annotations");
		}

		public static string GetInputPath(Properties props)
		{
			string input = props.GetProperty("coref.inputPath", GetTestDataPath(props));
			return input;
		}

		public static string GetScorerPath(Properties props)
		{
			return props.GetProperty("coref.scorer", "/u/scr/nlp/data/conll-2012/scorer/v8.01/scorer.pl");
		}

		public static Locale GetLanguage(Properties props)
		{
			string lang = PropertiesUtils.GetString(props, "coref.language", "en");
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
					throw new ArgumentException("unsupported language");
				}
			}
		}

		private static string GetLanguageStr(Properties props)
		{
			return GetLanguage(props).GetDisplayName().ToLower();
		}

		public static IHeadFinder GetHeadFinder(Properties props)
		{
			Locale lang = GetLanguage(props);
			if (lang == Locale.English)
			{
				return new SemanticHeadFinder();
			}
			else
			{
				if (lang == Locale.Chinese)
				{
					return new ChineseSemanticHeadFinder();
				}
				else
				{
					throw new Exception("Invalid language setting: cannot load HeadFinder");
				}
			}
		}
	}
}
