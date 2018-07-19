using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>Manages the properties for training and running statistical coreference systems.</summary>
	/// <author>Kevin Clark</author>
	public class StatisticalCorefProperties
	{
		public static string TrainingPath(Properties props)
		{
			return props.GetProperty("coref.statistical.trainingPath");
		}

		private static string GetDefaultModelPath(Properties props, string modelName)
		{
			return "edu/stanford/nlp/models/coref/statistical/" + modelName + (CorefProperties.Conll(props) ? "_conll" : string.Empty) + ".ser.gz";
		}

		public static string ClassificationModelPath(Properties props)
		{
			return PropertiesUtils.GetString(props, "coref.statistical.classificationModel", GetDefaultModelPath(props, "classification_model"));
		}

		public static string RankingModelPath(Properties props)
		{
			return PropertiesUtils.GetString(props, "coref.statistical.rankingModel", GetDefaultModelPath(props, "ranking_model"));
		}

		public static string AnaphoricityModelPath(Properties props)
		{
			return PropertiesUtils.GetString(props, "coref.statistical.anaphoricityModel", GetDefaultModelPath(props, "anaphoricity_model"));
		}

		public static string ClusteringModelPath(Properties props)
		{
			return PropertiesUtils.GetString(props, "coref.statistical.clusteringModel", GetDefaultModelPath(props, "clustering_model"));
		}

		public static string WordCountsPath(Properties props)
		{
			return PropertiesUtils.GetString(props, "coref.statistical.wordCounts", "edu/stanford/nlp/models/coref/statistical/word_counts.ser.gz");
		}

		public static double[] PairwiseScoreThresholds(Properties props)
		{
			string thresholdsProp = props.GetProperty("coref.statistical.pairwiseScoreThresholds");
			if (thresholdsProp != null)
			{
				string[] split = thresholdsProp.Split(",");
				if (split.Length == 4)
				{
					return Arrays.Stream(split).MapToDouble(null).ToArray();
				}
			}
			double threshold = PropertiesUtils.GetDouble(props, "coref.statistical.pairwiseScoreThresholds", 0.35);
			return new double[] { threshold, threshold, threshold, threshold };
		}

		public static double MinClassImbalance(Properties props)
		{
			return PropertiesUtils.GetDouble(props, "coref.statistical.minClassImbalance", 0);
		}

		public static int MaxTrainExamplesPerDocument(Properties props)
		{
			return PropertiesUtils.GetInt(props, "coref.statistical.maxTrainExamplesPerDocument", int.MaxValue);
		}
	}
}
