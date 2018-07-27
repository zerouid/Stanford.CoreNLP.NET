using System;
using System.Reflection;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>Main class for training new statistical coreference systems.</summary>
	/// <author>Kevin Clark</author>
	public class StatisticalCorefTrainer
	{
		public const string ClassificationModel = "classification";

		public const string RankingModel = "ranking";

		public const string AnaphoricityModel = "anaphoricity";

		public const string ClusteringModelName = "clusterer";

		public const string ExtractedFeaturesName = "features";

		public static string trainingPath;

		public static string pairwiseModelsPath;

		public static string clusteringModelsPath;

		public static string predictionsName;

		public static string datasetFile;

		public static string goldClustersFile;

		public static string wordCountsFile;

		public static string mentionTypesFile;

		public static string compressorFile;

		public static string extractedFeaturesFile;

		private static void MakeDir(string path)
		{
			File outDir = new File(path);
			if (!outDir.Exists())
			{
				outDir.Mkdir();
			}
		}

		public static void SetTrainingPath(Properties props)
		{
			trainingPath = StatisticalCorefProperties.TrainingPath(props);
			pairwiseModelsPath = trainingPath + "pairwise_models/";
			clusteringModelsPath = trainingPath + "clustering_models/";
			MakeDir(pairwiseModelsPath);
			MakeDir(clusteringModelsPath);
		}

		public static void SetDataPath(string name)
		{
			string dataPath = trainingPath + name + "/";
			string extractedFeaturesPath = dataPath + ExtractedFeaturesName + "/";
			MakeDir(dataPath);
			MakeDir(extractedFeaturesPath);
			datasetFile = dataPath + "dataset.ser";
			predictionsName = name + "_predictions";
			goldClustersFile = dataPath + "gold_clusters.ser";
			mentionTypesFile = dataPath + "mention_types.ser";
			compressorFile = extractedFeaturesPath + "compressor.ser";
			extractedFeaturesFile = extractedFeaturesPath + "compressed_features.ser";
		}

		public static string FieldValues(object o)
		{
			string s = string.Empty;
			FieldInfo[] fields = Sharpen.Runtime.GetDeclaredFields(o.GetType());
			foreach (FieldInfo field in fields)
			{
				try
				{
					field.SetAccessible(true);
					s += field.Name + " = " + field.GetValue(o) + "\n";
				}
				catch (Exception e)
				{
					throw new Exception("Error getting field value for " + field.Name, e);
				}
			}
			return s;
		}

		/// <exception cref="System.Exception"/>
		private static void Preprocess(Properties props, Dictionaries dictionaries, bool isTrainSet)
		{
			(isTrainSet ? new DatasetBuilder(StatisticalCorefProperties.MinClassImbalance(props), StatisticalCorefProperties.MaxTrainExamplesPerDocument(props)) : new DatasetBuilder()).RunFromScratch(props, dictionaries);
			new MetadataWriter(isTrainSet).RunFromScratch(props, dictionaries);
			new FeatureExtractorRunner(props, dictionaries).RunFromScratch(props, dictionaries);
		}

		/// <exception cref="System.Exception"/>
		public static void DoTraining(Properties props)
		{
			SetTrainingPath(props);
			Dictionaries dictionaries = new Dictionaries(props);
			SetDataPath("train");
			wordCountsFile = trainingPath + "train/word_counts.ser";
			CorefProperties.SetInput(props, CorefProperties.Dataset.Train);
			Preprocess(props, dictionaries, true);
			SetDataPath("dev");
			CorefProperties.SetInput(props, CorefProperties.Dataset.Dev);
			Preprocess(props, dictionaries, false);
			SetDataPath("train");
			dictionaries = null;
			PairwiseModel classificationModel = PairwiseModel.NewBuilder(ClassificationModel, MetaFeatureExtractor.NewBuilder().Build()).Build();
			PairwiseModel rankingModel = PairwiseModel.NewBuilder(RankingModel, MetaFeatureExtractor.NewBuilder().Build()).Build();
			PairwiseModel anaphoricityModel = PairwiseModel.NewBuilder(AnaphoricityModel, MetaFeatureExtractor.AnaphoricityMFE()).TrainingExamples(5000000).Build();
			PairwiseModelTrainer.TrainRanking(rankingModel);
			PairwiseModelTrainer.TrainClassification(classificationModel, false);
			PairwiseModelTrainer.TrainClassification(anaphoricityModel, true);
			SetDataPath("dev");
			PairwiseModelTrainer.Test(classificationModel, predictionsName, false);
			PairwiseModelTrainer.Test(rankingModel, predictionsName, false);
			PairwiseModelTrainer.Test(anaphoricityModel, predictionsName, true);
			new Clusterer().DoTraining(ClusteringModelName);
		}

		/// <summary>Run the training.</summary>
		/// <remarks>
		/// Run the training. Main options:
		/// <ul>
		/// <li>-coref.data: location of training data (CoNLL format)</li>
		/// <li>-coref.statistical.trainingPath: where to write trained models and temporary files</li>
		/// <li>-coref.statistical.minClassImbalance: use this to downsample negative examples to
		/// speed up and reduce the memory footprint of training</li>
		/// <li>-coref.statistical.maxTrainExamplesPerDocument: use this to downsample examples from
		/// each document to speed up and reduce the memory footprint training</li>
		/// </ul>
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			DoTraining(StringUtils.ArgsToProperties(args));
		}
	}
}
