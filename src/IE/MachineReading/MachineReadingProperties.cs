using System;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading
{
	public class MachineReadingProperties
	{
		public static Logger logger = Logger.GetLogger(typeof(MachineReading).FullName);

		public static Type datasetReaderClass;

		public static Type datasetAuxReaderClass;

		public static bool useNewHeadFinder = true;

		public static string readerLogLevel = "SEVERE";

		public static bool serializeCorpora = true;

		public static bool forceGenerationOfIndexSpans = true;

		protected internal static string serializedEntityExtractorPath = string.Empty;

		protected internal static string serializedEntityExtractionResults;

		public static string entityGazetteerPath;

		public static Type entityClassifier = typeof(BasicEntityExtractor);

		public static string entityResultsPrinters = string.Empty;

		protected internal static string serializedRelationExtractorPath = null;

		protected internal static string serializedRelationExtractionResults = null;

		public static Type relationFeatureFactoryClass = typeof(BasicRelationFeatureFactory);

		public static Type relationMentionFactoryClass = typeof(RelationMentionFactory);

		public static string relationFeatures = "all";

		public static string relationResultsPrinters = "edu.stanford.nlp.ie.machinereading.RelationExtractorResultsPrinter";

		public static bool trainRelationsUsingPredictedEntities = false;

		public static bool testRelationsUsingPredictedEntities = false;

		public static bool createUnrelatedRelations = true;

		public static bool doNotLexicalizeFirstArg = false;

		public static bool useRelationExtractionModelMerging = false;

		public static string relationsToSkipDuringTraining = string.Empty;

		public static Type relationExtractionPostProcessorClass;

		public static Type relationClassifier = typeof(BasicRelationExtractor);

		protected internal static string serializedEventExtractorPath = string.Empty;

		protected internal static string serializedEventExtractionResults;

		public static string eventResultsPrinters = string.Empty;

		public static bool trainEventsUsingPredictedEntities = false;

		public static bool testEventsUsingPredictedEntities = false;

		public static Type consistencyCheck;

		protected internal static string trainPath;

		protected internal static string auxDataPath;

		protected internal static string serializedTrainingSentencesPath;

		protected internal static string serializedAuxTrainingSentencesPath;

		protected internal static bool loadModel = false;

		public static bool trainUsePipelineNER = false;

		/// <summary>evaluation options (ignored if trainOnly is true)</summary>
		protected internal static bool trainOnly = false;

		protected internal static string testPath;

		protected internal static string serializedTestSentencesPath;

		protected internal static bool extractEntities = true;

		protected internal static bool extractRelations = true;

		protected internal static bool extractEvents = true;

		protected internal static bool crossValidate = false;

		public static int kfold = 5;

		public static double percentageOfTrain = 1.0;

		/// <summary>Additional features, may not necessarily be used in the public release</summary>
		public static double featureSimilarityThreshold = 0.2;

		public static bool computeFeatSimilarity = true;

		public static double featureSelectionNumFeaturesRatio = 0.7;

		public static bool L1Reg = false;

		public static bool L2Reg = true;

		public static double L1RegLambda = 1.0;

		private MachineReadingProperties()
		{
		}
		/*
		* general options
		*/
		/*
		* entity extraction options
		*/
		// TODO this option is temporary and should be removed when (if?) gazetteers get
		// folded into feature factories
		/*
		* relation extraction options
		*/
		// TODO: temporary NFL deadline based hack. remove it.
		/*
		* event extraction options
		*/
		/*
		* global, domain-dependent options
		*/
		/*
		* training options
		*/
		/*
		* cross-validation options
		*/
		// class of static option variables.
	}
}
