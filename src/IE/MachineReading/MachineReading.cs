using System;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang.Reflect;
using Java.Util;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading
{
	/// <summary>Main driver for Machine Reading training, annotation, and evaluation.</summary>
	/// <remarks>
	/// Main driver for Machine Reading training, annotation, and evaluation. Does
	/// entity, relation, and event extraction for all corpora.
	/// This code has been adapted for 4 domains, all defined in the edu.stanford.nlp.ie.machinereading.domains package.
	/// For each domain, you need a properties file that is the only command line parameter for MachineReading.
	/// Minimally, for each domain you need to define a reader class that extends the GenericDataSetReader class
	/// and overrides the public Annotation read(String path) method.
	/// How to run: java edu.stanford.nlp.ie.machinereading.MachineReading -arguments propertiesFile
	/// This method creates an Annotation with additional objects per sentence: EntityMentions and RelationMentions.
	/// Using these objects, the classifiers that get called from MachineReading train entity and relation extractors.
	/// The simplest example domain currently is in edu.stanford.nlp.ie.machinereading.domains.roth,
	/// which is a simple entity and relation extraction using a dataset created by Dan Roth. The properties file for the domain is at
	/// projects/more/src/edu/stanford/nlp/ie/machinereading/domains/roth/roth.properties
	/// </remarks>
	/// <author>David McCLosky</author>
	/// <author>mrsmith</author>
	/// <author>Mihai</author>
	public class MachineReading
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.MachineReading));

		/// <summary>Store command-line args so they can be passed to other classes</summary>
		private readonly string[] args;

		private GenericDataSetReader reader;

		private GenericDataSetReader auxReader;

		private IExtractor entityExtractor;

		private IExtractor relationExtractor;

		private IExtractor relationExtractionPostProcessor;

		private IExtractor eventExtractor;

		private IExtractor consistencyChecker;

		private bool forceRetraining;

		private bool forceParseSentences;

		/// <summary>
		/// Array of pairs of datasets (training, testing)
		/// If cross validation is enabled, the length of this array is the number of folds; otherwise it is 1
		/// The first element in each pair is the training corpus; the second is testing
		/// </summary>
		private Pair<Annotation, Annotation>[] datasets;

		/// <summary>
		/// Stores the predictions of the extractors
		/// The first index is the partition number (of length 1 is cross validation is not enabled)
		/// The second index is the task: 0 - entities, 1 - relations, 2 - events
		/// Note: we need to store separate predictions per task because they may not be compatible with each other.
		/// </summary>
		/// <remarks>
		/// Stores the predictions of the extractors
		/// The first index is the partition number (of length 1 is cross validation is not enabled)
		/// The second index is the task: 0 - entities, 1 - relations, 2 - events
		/// Note: we need to store separate predictions per task because they may not be compatible with each other.
		/// For example, we may have predicted entities in task 0 but use gold entities for task 1.
		/// </remarks>
		private Annotation[][] predictions;

		private ICollection<ResultsPrinter> entityResultsPrinterSet;

		private ICollection<ResultsPrinter> relationResultsPrinterSet;

		private ICollection<ResultsPrinter> eventResultsPrinterSet;

		private const int EntityLevel = 0;

		private const int RelationLevel = 1;

		private const int EventLevel = 2;

		/*
		* class attributes
		*/
		// TODO could add an entityExtractorPostProcessor if we need one
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.IE.Machinereading.MachineReading mr = MakeMachineReading(args);
			mr.Run();
		}

		public static void SetLoggerLevel(Level level)
		{
			SetConsoleLevel(Level.Finest);
			MachineReadingProperties.logger.SetLevel(level);
		}

		public static void SetConsoleLevel(Level level)
		{
			// get the top Logger:
			Logger topLogger = Logger.GetLogger(string.Empty);
			// Handler for console (reuse it if it already exists)
			Handler consoleHandler = null;
			// see if there is already a console handler
			foreach (Handler handler in topLogger.GetHandlers())
			{
				if (handler is ConsoleHandler)
				{
					// found the console handler
					consoleHandler = handler;
					break;
				}
			}
			if (consoleHandler == null)
			{
				// there was no console handler found, create a new one
				consoleHandler = new ConsoleHandler();
				topLogger.AddHandler(consoleHandler);
			}
			// set the console handler level:
			consoleHandler.SetLevel(level);
			consoleHandler.SetFormatter(new SimpleFormatter());
		}

		/// <summary>Use the makeMachineReading* methods to create MachineReading objects!</summary>
		private MachineReading(string[] args)
		{
			this.args = args;
		}

		protected internal MachineReading()
		{
			this.args = StringUtils.EmptyStringArray;
		}

		/// <summary>
		/// Creates a MR object to be used only for annotation purposes (no training)
		/// This is needed in order to integrate MachineReading with BaselineNLProcessor
		/// </summary>
		public static Edu.Stanford.Nlp.IE.Machinereading.MachineReading MakeMachineReadingForAnnotation(GenericDataSetReader reader, IExtractor entityExtractor, IExtractor relationExtractor, IExtractor eventExtractor, IExtractor consistencyChecker, 
			IExtractor relationPostProcessor, bool testRelationsUsingPredictedEntities, bool verbose)
		{
			Edu.Stanford.Nlp.IE.Machinereading.MachineReading mr = new Edu.Stanford.Nlp.IE.Machinereading.MachineReading();
			// readers needed to assign syntactic heads to predicted entities
			mr.reader = reader;
			mr.auxReader = null;
			// no results printers needed
			mr.entityResultsPrinterSet = new HashSet<ResultsPrinter>();
			mr.SetRelationResultsPrinterSet(new HashSet<ResultsPrinter>());
			// create the storage for the generated annotations
			mr.predictions = new Annotation[][] { new Annotation[1], new Annotation[1], new Annotation[1] };
			// create the entity/relation classifiers
			mr.entityExtractor = entityExtractor;
			MachineReadingProperties.extractEntities = entityExtractor != null;
			mr.relationExtractor = relationExtractor;
			MachineReadingProperties.extractRelations = relationExtractor != null;
			MachineReadingProperties.testRelationsUsingPredictedEntities = testRelationsUsingPredictedEntities;
			mr.eventExtractor = eventExtractor;
			MachineReadingProperties.extractEvents = eventExtractor != null;
			mr.consistencyChecker = consistencyChecker;
			mr.relationExtractionPostProcessor = relationPostProcessor;
			Level level = verbose ? Level.Finest : Level.Severe;
			if (entityExtractor != null)
			{
				entityExtractor.SetLoggerLevel(level);
			}
			if (mr.relationExtractor != null)
			{
				mr.relationExtractor.SetLoggerLevel(level);
			}
			if (mr.eventExtractor != null)
			{
				mr.eventExtractor.SetLoggerLevel(level);
			}
			return mr;
		}

		/// <exception cref="System.IO.IOException"/>
		public static Edu.Stanford.Nlp.IE.Machinereading.MachineReading MakeMachineReading(string[] args)
		{
			// install global parameters
			Edu.Stanford.Nlp.IE.Machinereading.MachineReading mr = new Edu.Stanford.Nlp.IE.Machinereading.MachineReading(args);
			//TODO:
			ArgumentParser.FillOptions(typeof(MachineReadingProperties), args);
			//Arguments.parse(args, mr);
			log.Info("PERCENTAGE OF TRAIN: " + MachineReadingProperties.percentageOfTrain);
			// convert args to properties
			Properties props = StringUtils.ArgsToProperties(args);
			if (props == null)
			{
				throw new Exception("ERROR: failed to find Properties in the given arguments!");
			}
			string logLevel = props.GetProperty("logLevel", "INFO");
			SetLoggerLevel(Level.Parse(logLevel.ToUpper()));
			// install reader specific parameters
			GenericDataSetReader reader = mr.MakeReader(props);
			GenericDataSetReader auxReader = mr.MakeAuxReader();
			Level readerLogLevel = Level.Parse(MachineReadingProperties.readerLogLevel.ToUpper());
			reader.SetLoggerLevel(readerLogLevel);
			if (auxReader != null)
			{
				auxReader.SetLoggerLevel(readerLogLevel);
			}
			log.Info("The reader log level is set to " + readerLogLevel);
			//Execution.fillOptions(GenericDataSetReaderProps.class, args);
			//Arguments.parse(args, reader);
			// create the pre-processing pipeline
			StanfordCoreNLP pipe = new StanfordCoreNLP(props, false);
			reader.SetProcessor(pipe);
			if (auxReader != null)
			{
				auxReader.SetProcessor(pipe);
			}
			// create the results printers
			mr.MakeResultsPrinters(args);
			return mr;
		}

		/// <summary>Performs extraction.</summary>
		/// <remarks>
		/// Performs extraction. This will train a new extraction model and evaluate
		/// the model on the test set. Depending on the MachineReading instance's
		/// parameters, it may skip training if a model already exists or skip
		/// evaluation.
		/// returns results string, can be compared in a utest
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public virtual IList<string> Run()
		{
			this.forceRetraining = !MachineReadingProperties.loadModel;
			if (MachineReadingProperties.trainOnly)
			{
				this.forceRetraining = true;
			}
			IList<string> retMsg = new List<string>();
			bool haveSerializedEntityExtractor = SerializedModelExists(MachineReadingProperties.serializedEntityExtractorPath);
			bool haveSerializedRelationExtractor = SerializedModelExists(MachineReadingProperties.serializedRelationExtractorPath);
			bool haveSerializedEventExtractor = SerializedModelExists(MachineReadingProperties.serializedEventExtractorPath);
			Annotation training = null;
			Annotation aux = null;
			if ((MachineReadingProperties.extractEntities && !haveSerializedEntityExtractor) || (MachineReadingProperties.extractRelations && !haveSerializedRelationExtractor) || (MachineReadingProperties.extractEvents && !haveSerializedEventExtractor) 
				|| this.forceRetraining || MachineReadingProperties.crossValidate)
			{
				// load training sentences
				training = LoadOrMakeSerializedSentences(MachineReadingProperties.trainPath, reader, new File(MachineReadingProperties.serializedTrainingSentencesPath));
				if (auxReader != null)
				{
					MachineReadingProperties.logger.Severe("Reading auxiliary dataset from " + MachineReadingProperties.auxDataPath + "...");
					aux = LoadOrMakeSerializedSentences(MachineReadingProperties.auxDataPath, auxReader, new File(MachineReadingProperties.serializedAuxTrainingSentencesPath));
					MachineReadingProperties.logger.Severe("Done reading auxiliary dataset.");
				}
			}
			Annotation testing = null;
			if (!MachineReadingProperties.trainOnly && !MachineReadingProperties.crossValidate)
			{
				// load test sentences
				File serializedTestSentences = new File(MachineReadingProperties.serializedTestSentencesPath);
				testing = LoadOrMakeSerializedSentences(MachineReadingProperties.testPath, reader, serializedTestSentences);
			}
			//
			// create the actual datasets to be used for training and annotation
			//
			MakeDataSets(training, testing, aux);
			//
			// process (training + annotate) one partition at a time
			//
			for (int partition = 0; partition < datasets.Length; partition++)
			{
				System.Diagnostics.Debug.Assert((datasets.Length > partition));
				System.Diagnostics.Debug.Assert((datasets[partition] != null));
				System.Diagnostics.Debug.Assert((MachineReadingProperties.trainOnly || datasets[partition].Second() != null));
				// train all models
				Train(datasets[partition].First(), (MachineReadingProperties.crossValidate ? partition : -1));
				// annotate using all models
				if (!MachineReadingProperties.trainOnly)
				{
					MachineReadingProperties.logger.Info("annotating partition " + partition);
					Annotate(datasets[partition].Second(), (MachineReadingProperties.crossValidate ? partition : -1));
				}
			}
			//
			// now report overall results
			//
			if (!MachineReadingProperties.trainOnly)
			{
				// merge test sets for the gold data
				Annotation gold = new Annotation(string.Empty);
				foreach (Pair<Annotation, Annotation> dataset in datasets)
				{
					AnnotationUtils.AddSentences(gold, dataset.Second().Get(typeof(CoreAnnotations.SentencesAnnotation)));
				}
				// merge test sets with predicted annotations
				Annotation[] mergedPredictions = new Annotation[3];
				System.Diagnostics.Debug.Assert((predictions != null));
				for (int taskLevel = 0; taskLevel < mergedPredictions.Length; taskLevel++)
				{
					mergedPredictions[taskLevel] = new Annotation(string.Empty);
					for (int fold = 0; fold < predictions[taskLevel].Length; fold++)
					{
						if (predictions[taskLevel][fold] == null)
						{
							continue;
						}
						AnnotationUtils.AddSentences(mergedPredictions[taskLevel], predictions[taskLevel][fold].Get(typeof(CoreAnnotations.SentencesAnnotation)));
					}
				}
				//
				// evaluate all tasks: entity, relation, and event recognition
				//
				if (MachineReadingProperties.extractEntities && !entityResultsPrinterSet.IsEmpty())
				{
					Sharpen.Collections.AddAll(retMsg, PrintTask("entity extraction", entityResultsPrinterSet, gold, mergedPredictions[EntityLevel]));
				}
				if (MachineReadingProperties.extractRelations && !GetRelationResultsPrinterSet().IsEmpty())
				{
					Sharpen.Collections.AddAll(retMsg, PrintTask("relation extraction", GetRelationResultsPrinterSet(), gold, mergedPredictions[RelationLevel]));
				}
				//
				// Save the sentences with the predicted annotations
				//
				if (MachineReadingProperties.extractEntities && MachineReadingProperties.serializedEntityExtractionResults != null)
				{
					IOUtils.WriteObjectToFile(mergedPredictions[EntityLevel], MachineReadingProperties.serializedEntityExtractionResults);
				}
				if (MachineReadingProperties.extractRelations && MachineReadingProperties.serializedRelationExtractionResults != null)
				{
					IOUtils.WriteObjectToFile(mergedPredictions[RelationLevel], MachineReadingProperties.serializedRelationExtractionResults);
				}
				if (MachineReadingProperties.extractEvents && MachineReadingProperties.serializedEventExtractionResults != null)
				{
					IOUtils.WriteObjectToFile(mergedPredictions[EventLevel], MachineReadingProperties.serializedEventExtractionResults);
				}
			}
			return retMsg;
		}

		private static IList<string> PrintTask(string taskName, ICollection<ResultsPrinter> printers, Annotation gold, Annotation pred)
		{
			IList<string> retMsg = new List<string>();
			foreach (ResultsPrinter rp in printers)
			{
				string msg = rp.PrintResults(gold, pred);
				retMsg.Add(msg);
				MachineReadingProperties.logger.Severe("Overall " + taskName + " results, using printer " + rp.GetType() + ":\n" + msg);
			}
			return retMsg;
		}

		/// <exception cref="System.Exception"/>
		protected internal virtual void Train(Annotation training, int partition)
		{
			//
			// train entity extraction
			//
			if (MachineReadingProperties.extractEntities)
			{
				MachineReadingProperties.logger.Info("Training entity extraction model(s)");
				if (partition != -1)
				{
					MachineReadingProperties.logger.Info("In partition #" + partition);
				}
				string modelName = MachineReadingProperties.serializedEntityExtractorPath;
				if (partition != -1)
				{
					modelName += "." + partition;
				}
				File modelFile = new File(modelName);
				MachineReadingProperties.logger.Fine("forceRetraining = " + this.forceRetraining + ", modelFile.exists = " + modelFile.Exists());
				if (!this.forceRetraining && modelFile.Exists())
				{
					MachineReadingProperties.logger.Info("Loading entity extraction model from " + modelName + " ...");
					entityExtractor = BasicEntityExtractor.Load(modelName, MachineReadingProperties.entityClassifier, false);
				}
				else
				{
					MachineReadingProperties.logger.Info("Training entity extraction model...");
					entityExtractor = MakeEntityExtractor(MachineReadingProperties.entityClassifier, MachineReadingProperties.entityGazetteerPath);
					entityExtractor.Train(training);
					MachineReadingProperties.logger.Info("Serializing entity extraction model to " + modelName + " ...");
					entityExtractor.Save(modelName);
				}
			}
			//
			// train relation extraction
			//
			if (MachineReadingProperties.extractRelations)
			{
				MachineReadingProperties.logger.Info("Training relation extraction model(s)");
				if (partition != -1)
				{
					MachineReadingProperties.logger.Info("In partition #" + partition);
				}
				string modelName = MachineReadingProperties.serializedRelationExtractorPath;
				if (partition != -1)
				{
					modelName += "." + partition;
				}
				if (MachineReadingProperties.useRelationExtractionModelMerging)
				{
					string[] modelNames = MachineReadingProperties.serializedRelationExtractorPath.Split(",");
					if (partition != -1)
					{
						for (int i = 0; i < modelNames.Length; i++)
						{
							modelNames[i] += "." + partition;
						}
					}
					relationExtractor = ExtractorMerger.BuildRelationExtractorMerger(modelNames);
				}
				else
				{
					if (!this.forceRetraining && new File(modelName).Exists())
					{
						MachineReadingProperties.logger.Info("Loading relation extraction model from " + modelName + " ...");
						//TODO change this to load any type of BasicRelationExtractor
						relationExtractor = BasicRelationExtractor.Load(modelName);
					}
					else
					{
						RelationFeatureFactory rff = MakeRelationFeatureFactory(MachineReadingProperties.relationFeatureFactoryClass, MachineReadingProperties.relationFeatures, MachineReadingProperties.doNotLexicalizeFirstArg);
						ArgumentParser.FillOptions(rff, args);
						Annotation predicted = null;
						if (MachineReadingProperties.trainRelationsUsingPredictedEntities)
						{
							// generate predicted entities
							System.Diagnostics.Debug.Assert((entityExtractor != null));
							predicted = AnnotationUtils.DeepMentionCopy(training);
							entityExtractor.Annotate(predicted);
							foreach (ResultsPrinter rp in entityResultsPrinterSet)
							{
								string msg = rp.PrintResults(training, predicted);
								MachineReadingProperties.logger.Info("Training relation extraction using predicted entitities: entity scores using printer " + rp.GetType() + ":\n" + msg);
							}
							// change relation mentions to use predicted entity mentions rather than gold ones
							try
							{
								ChangeGoldRelationArgsToPredicted(predicted);
							}
							catch (Exception e)
							{
								// we may get here for unknown EntityMentionComparator class
								throw new Exception(e);
							}
						}
						Annotation dataset;
						if (MachineReadingProperties.trainRelationsUsingPredictedEntities)
						{
							dataset = predicted;
						}
						else
						{
							dataset = training;
						}
						ICollection<string> relationsToSkip = new HashSet<string>(StringUtils.Split(MachineReadingProperties.relationsToSkipDuringTraining, ","));
						IList<IList<RelationMention>> backedUpRelations = new List<IList<RelationMention>>();
						if (relationsToSkip.Count > 0)
						{
							// we need to backup the relations since removeSkippableRelations modifies dataset in place and we can't duplicate CoreMaps safely (or can we?)
							foreach (ICoreMap sent in dataset.Get(typeof(CoreAnnotations.SentencesAnnotation)))
							{
								IList<RelationMention> relationMentions = sent.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
								backedUpRelations.Add(relationMentions);
							}
							RemoveSkippableRelations(dataset, relationsToSkip);
						}
						//relationExtractor = new BasicRelationExtractor(rff, MachineReadingProperties.createUnrelatedRelations, makeRelationMentionFactory(MachineReadingProperties.relationMentionFactoryClass));
						relationExtractor = MakeRelationExtractor(MachineReadingProperties.relationClassifier, rff, MachineReadingProperties.createUnrelatedRelations, MakeRelationMentionFactory(MachineReadingProperties.relationMentionFactoryClass));
						ArgumentParser.FillOptions(relationExtractor, args);
						//Arguments.parse(args,relationExtractor);
						MachineReadingProperties.logger.Info("Training relation extraction model...");
						relationExtractor.Train(dataset);
						MachineReadingProperties.logger.Info("Serializing relation extraction model to " + modelName + " ...");
						relationExtractor.Save(modelName);
						if (relationsToSkip.Count > 0)
						{
							// restore backed up relations into dataset
							int sentenceIndex = 0;
							foreach (ICoreMap sentence in dataset.Get(typeof(CoreAnnotations.SentencesAnnotation)))
							{
								IList<RelationMention> relationMentions = backedUpRelations[sentenceIndex];
								sentence.Set(typeof(MachineReadingAnnotations.RelationMentionsAnnotation), relationMentions);
								sentenceIndex++;
							}
						}
					}
				}
			}
			//
			// train event extraction -- currently just works with MSTBasedEventExtractor
			//
			if (MachineReadingProperties.extractEvents)
			{
				MachineReadingProperties.logger.Info("Training event extraction model(s)");
				if (partition != -1)
				{
					MachineReadingProperties.logger.Info("In partition #" + partition);
				}
				string modelName = MachineReadingProperties.serializedEventExtractorPath;
				if (partition != -1)
				{
					modelName += "." + partition;
				}
				File modelFile = new File(modelName);
				if (!this.forceRetraining && modelFile.Exists())
				{
					MachineReadingProperties.logger.Info("Loading event extraction model from " + modelName + " ...");
					MethodInfo mstLoader = (Sharpen.Runtime.GetType("MSTBasedEventExtractor")).GetMethod("load", typeof(string));
					eventExtractor = (IExtractor)mstLoader.Invoke(null, modelName);
				}
				else
				{
					Annotation predicted = null;
					if (MachineReadingProperties.trainEventsUsingPredictedEntities)
					{
						// generate predicted entities
						System.Diagnostics.Debug.Assert((entityExtractor != null));
						predicted = AnnotationUtils.DeepMentionCopy(training);
						entityExtractor.Annotate(predicted);
						foreach (ResultsPrinter rp in entityResultsPrinterSet)
						{
							string msg = rp.PrintResults(training, predicted);
							MachineReadingProperties.logger.Info("Training event extraction using predicted entitities: entity scores using printer " + rp.GetType() + ":\n" + msg);
						}
					}
					// TODO: need an equivalent of changeGoldRelationArgsToPredicted here?
					Constructor<object> mstConstructor = (Sharpen.Runtime.GetType("edu.stanford.nlp.ie.machinereading.MSTBasedEventExtractor")).GetConstructor(typeof(bool));
					eventExtractor = (IExtractor)mstConstructor.NewInstance(MachineReadingProperties.trainEventsUsingPredictedEntities);
					MachineReadingProperties.logger.Info("Training event extraction model...");
					if (MachineReadingProperties.trainRelationsUsingPredictedEntities)
					{
						eventExtractor.Train(predicted);
					}
					else
					{
						eventExtractor.Train(training);
					}
					MachineReadingProperties.logger.Info("Serializing event extraction model to " + modelName + " ...");
					eventExtractor.Save(modelName);
				}
			}
		}

		/// <summary>Removes any relations with relation types in relationsToSkip from a dataset.</summary>
		/// <remarks>Removes any relations with relation types in relationsToSkip from a dataset.  Dataset is modified in place.</remarks>
		private static void RemoveSkippableRelations(Annotation dataset, ICollection<string> relationsToSkip)
		{
			if (relationsToSkip == null || relationsToSkip.IsEmpty())
			{
				return;
			}
			foreach (ICoreMap sent in dataset.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				IList<RelationMention> relationMentions = sent.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
				if (relationMentions == null)
				{
					continue;
				}
				IList<RelationMention> newRelationMentions = new List<RelationMention>();
				foreach (RelationMention rm in relationMentions)
				{
					if (!relationsToSkip.Contains(rm.GetType()))
					{
						newRelationMentions.Add(rm);
					}
				}
				sent.Set(typeof(MachineReadingAnnotations.RelationMentionsAnnotation), newRelationMentions);
			}
		}

		/// <summary>Replaces all relation arguments with predicted entities</summary>
		private static void ChangeGoldRelationArgsToPredicted(Annotation dataset)
		{
			foreach (ICoreMap sent in dataset.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				IList<EntityMention> entityMentions = sent.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
				IList<RelationMention> relationMentions = sent.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
				IList<RelationMention> newRels = new List<RelationMention>();
				foreach (RelationMention rm in relationMentions)
				{
					rm.SetSentence(sent);
					if (rm.ReplaceGoldArgsWithPredicted(entityMentions))
					{
						MachineReadingProperties.logger.Info("Successfully mapped all arguments in relation mention: " + rm);
						newRels.Add(rm);
					}
					else
					{
						MachineReadingProperties.logger.Info("Dropped relation mention due to failed argument mapping: " + rm);
					}
				}
				sent.Set(typeof(MachineReadingAnnotations.RelationMentionsAnnotation), newRels);
				// we may have added new mentions to the entity list, so let's store it again
				sent.Set(typeof(MachineReadingAnnotations.EntityMentionsAnnotation), entityMentions);
			}
		}

		public virtual Annotation Annotate(Annotation testing)
		{
			return Annotate(testing, -1);
		}

		protected internal virtual Annotation Annotate(Annotation testing, int partition)
		{
			int partitionIndex = (partition != -1 ? partition : 0);
			//
			// annotate entities
			//
			if (MachineReadingProperties.extractEntities)
			{
				System.Diagnostics.Debug.Assert((entityExtractor != null));
				Annotation predicted = AnnotationUtils.DeepMentionCopy(testing);
				entityExtractor.Annotate(predicted);
				foreach (ResultsPrinter rp in entityResultsPrinterSet)
				{
					string msg = rp.PrintResults(testing, predicted);
					MachineReadingProperties.logger.Info("Entity extraction results " + (partition != -1 ? "for partition #" + partition : string.Empty) + " using printer " + rp.GetType() + ":\n" + msg);
				}
				predictions[EntityLevel][partitionIndex] = predicted;
			}
			//
			// annotate relations
			//
			if (MachineReadingProperties.extractRelations)
			{
				System.Diagnostics.Debug.Assert((relationExtractor != null));
				Annotation predicted = (MachineReadingProperties.testRelationsUsingPredictedEntities ? predictions[EntityLevel][partitionIndex] : AnnotationUtils.DeepMentionCopy(testing));
				// make sure the entities have the syntactic head and span set. we need this for relation extraction features
				AssignSyntacticHeadToEntities(predicted);
				relationExtractor.Annotate(predicted);
				if (relationExtractionPostProcessor == null)
				{
					relationExtractionPostProcessor = MakeExtractor(MachineReadingProperties.relationExtractionPostProcessorClass);
				}
				if (relationExtractionPostProcessor != null)
				{
					MachineReadingProperties.logger.Info("Using relation extraction post processor: " + MachineReadingProperties.relationExtractionPostProcessorClass);
					relationExtractionPostProcessor.Annotate(predicted);
				}
				foreach (ResultsPrinter rp in GetRelationResultsPrinterSet())
				{
					string msg = rp.PrintResults(testing, predicted);
					MachineReadingProperties.logger.Info("Relation extraction results " + (partition != -1 ? "for partition #" + partition : string.Empty) + " using printer " + rp.GetType() + ":\n" + msg);
				}
				//
				// apply the domain-specific consistency checks
				//
				if (consistencyChecker == null)
				{
					consistencyChecker = MakeExtractor(MachineReadingProperties.consistencyCheck);
				}
				if (consistencyChecker != null)
				{
					MachineReadingProperties.logger.Info("Using consistency checker: " + MachineReadingProperties.consistencyCheck);
					consistencyChecker.Annotate(predicted);
					foreach (ResultsPrinter rp_1 in entityResultsPrinterSet)
					{
						string msg = rp_1.PrintResults(testing, predicted);
						MachineReadingProperties.logger.Info("Entity extraction results AFTER consistency checks " + (partition != -1 ? "for partition #" + partition : string.Empty) + " using printer " + rp_1.GetType() + ":\n" + msg);
					}
					foreach (ResultsPrinter rp_2 in GetRelationResultsPrinterSet())
					{
						string msg = rp_2.PrintResults(testing, predicted);
						MachineReadingProperties.logger.Info("Relation extraction results AFTER consistency checks " + (partition != -1 ? "for partition #" + partition : string.Empty) + " using printer " + rp_2.GetType() + ":\n" + msg);
					}
				}
				predictions[RelationLevel][partitionIndex] = predicted;
			}
			//
			// TODO: annotate events
			//
			return predictions[RelationLevel][partitionIndex];
		}

		private void AssignSyntacticHeadToEntities(Annotation corpus)
		{
			System.Diagnostics.Debug.Assert((corpus != null));
			System.Diagnostics.Debug.Assert((corpus.Get(typeof(CoreAnnotations.SentencesAnnotation)) != null));
			foreach (ICoreMap sent in corpus.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				IList<CoreLabel> tokens = sent.Get(typeof(CoreAnnotations.TokensAnnotation));
				System.Diagnostics.Debug.Assert((tokens != null));
				Tree tree = sent.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
				if (MachineReadingProperties.forceGenerationOfIndexSpans)
				{
					tree.IndexSpans(0);
				}
				System.Diagnostics.Debug.Assert((tree != null));
				if (sent.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation)) != null)
				{
					foreach (EntityMention e in sent.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation)))
					{
						reader.AssignSyntacticHead(e, tree, tokens, true);
					}
				}
			}
		}

		private static IExtractor MakeExtractor(Type extractorClass)
		{
			if (extractorClass == null)
			{
				return null;
			}
			IExtractor ex;
			try
			{
				ex = extractorClass.GetConstructor().NewInstance();
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
			return ex;
		}

		private void MakeDataSets(Annotation training, Annotation testing, Annotation auxDataset)
		{
			if (!MachineReadingProperties.crossValidate)
			{
				datasets = new Pair[1];
				Annotation trainingEnhanced = training;
				if (auxDataset != null)
				{
					trainingEnhanced = new Annotation(training.Get(typeof(CoreAnnotations.TextAnnotation)));
					for (int i = 0; i < AnnotationUtils.SentenceCount(training); i++)
					{
						AnnotationUtils.AddSentence(trainingEnhanced, AnnotationUtils.GetSentence(training, i));
					}
					for (int ind = 0; ind < AnnotationUtils.SentenceCount(auxDataset); ind++)
					{
						AnnotationUtils.AddSentence(trainingEnhanced, AnnotationUtils.GetSentence(auxDataset, ind));
					}
				}
				datasets[0] = new Pair<Annotation, Annotation>(trainingEnhanced, testing);
				predictions = new Annotation[][] { new Annotation[1], new Annotation[1], new Annotation[1] };
			}
			else
			{
				System.Diagnostics.Debug.Assert((MachineReadingProperties.kfold > 1));
				datasets = new Pair[MachineReadingProperties.kfold];
				AnnotationUtils.ShuffleSentences(training);
				for (int partition = 0; partition < MachineReadingProperties.kfold; partition++)
				{
					int begin = AnnotationUtils.SentenceCount(training) * partition / MachineReadingProperties.kfold;
					int end = AnnotationUtils.SentenceCount(training) * (partition + 1) / MachineReadingProperties.kfold;
					MachineReadingProperties.logger.Info("Creating partition #" + partition + " using offsets [" + begin + ", " + end + ") out of " + AnnotationUtils.SentenceCount(training));
					Annotation partitionTrain = new Annotation(string.Empty);
					Annotation partitionTest = new Annotation(string.Empty);
					for (int i = 0; i < AnnotationUtils.SentenceCount(training); i++)
					{
						if (i < begin)
						{
							AnnotationUtils.AddSentence(partitionTrain, AnnotationUtils.GetSentence(training, i));
						}
						else
						{
							if (i < end)
							{
								AnnotationUtils.AddSentence(partitionTest, AnnotationUtils.GetSentence(training, i));
							}
							else
							{
								AnnotationUtils.AddSentence(partitionTrain, AnnotationUtils.GetSentence(training, i));
							}
						}
					}
					// for learning curve experiments
					// partitionTrain = keepPercentage(partitionTrain, percentageOfTrain);
					partitionTrain = KeepPercentage(partitionTrain, MachineReadingProperties.percentageOfTrain);
					if (auxDataset != null)
					{
						for (int ind = 0; ind < AnnotationUtils.SentenceCount(auxDataset); ind++)
						{
							AnnotationUtils.AddSentence(partitionTrain, AnnotationUtils.GetSentence(auxDataset, ind));
						}
					}
					datasets[partition] = new Pair<Annotation, Annotation>(partitionTrain, partitionTest);
				}
				predictions = new Annotation[][] { new Annotation[MachineReadingProperties.kfold], new Annotation[MachineReadingProperties.kfold], new Annotation[MachineReadingProperties.kfold] };
			}
		}

		/// <summary>Keeps only the first percentage sentences from the given corpus</summary>
		private static Annotation KeepPercentage(Annotation corpus, double percentage)
		{
			log.Info("Using fraction of train: " + percentage);
			if (percentage >= 1.0)
			{
				return corpus;
			}
			Annotation smaller = new Annotation(string.Empty);
			IList<ICoreMap> sents = new List<ICoreMap>();
			IList<ICoreMap> fullSents = corpus.Get(typeof(CoreAnnotations.SentencesAnnotation));
			double smallSize = (double)fullSents.Count * percentage;
			for (int i = 0; i < smallSize; i++)
			{
				sents.Add(fullSents[i]);
			}
			log.Info("TRAIN corpus size reduced from " + fullSents.Count + " to " + sents.Count);
			smaller.Set(typeof(CoreAnnotations.SentencesAnnotation), sents);
			return smaller;
		}

		private static bool SerializedModelExists(string prefix)
		{
			if (!MachineReadingProperties.crossValidate)
			{
				File f = new File(prefix);
				return f.Exists();
			}
			// in cross validation we serialize models to prefix.<FOLD COUNT>
			for (int i = 0; i < MachineReadingProperties.kfold; i++)
			{
				File f = new File(prefix + "." + int.ToString(i));
				if (!f.Exists())
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Creates ResultsPrinter instances based on the resultsPrinters argument</summary>
		/// <param name="args"/>
		private void MakeResultsPrinters(string[] args)
		{
			entityResultsPrinterSet = MakeResultsPrinters(MachineReadingProperties.entityResultsPrinters, args);
			SetRelationResultsPrinterSet(MakeResultsPrinters(MachineReadingProperties.relationResultsPrinters, args));
			eventResultsPrinterSet = MakeResultsPrinters(MachineReadingProperties.eventResultsPrinters, args);
		}

		private static ICollection<ResultsPrinter> MakeResultsPrinters(string classes, string[] args)
		{
			MachineReadingProperties.logger.Info("Making result printers from " + classes);
			string[] printerClassNames = classes.Trim().Split(",\\s*");
			HashSet<ResultsPrinter> printers = new HashSet<ResultsPrinter>();
			foreach (string printerClassName in printerClassNames)
			{
				if (printerClassName.IsEmpty())
				{
					continue;
				}
				ResultsPrinter rp;
				try
				{
					rp = (ResultsPrinter)Sharpen.Runtime.GetType(printerClassName).GetConstructor().NewInstance();
					printers.Add(rp);
				}
				catch (Exception e)
				{
					throw new Exception(e);
				}
			}
			//Execution.fillOptions(ResultsPrinterProps.class, args);
			//Arguments.parse(args,rp);
			return printers;
		}

		/// <summary>Constructs the corpus reader class and sets it as the reader for this MachineReading instance.</summary>
		/// <returns>corpus reader specified by datasetReaderClass</returns>
		private GenericDataSetReader MakeReader(Properties props)
		{
			try
			{
				if (reader == null)
				{
					try
					{
						reader = MachineReadingProperties.datasetReaderClass.GetConstructor(typeof(Properties)).NewInstance(props);
					}
					catch (MissingMethodException)
					{
						// if no c'tor with props found let's use the default one
						reader = MachineReadingProperties.datasetReaderClass.GetConstructor().NewInstance();
					}
				}
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
			reader.SetUseNewHeadFinder(MachineReadingProperties.useNewHeadFinder);
			return reader;
		}

		/// <summary>Constructs the corpus reader class and sets it as the reader for this MachineReading instance.</summary>
		/// <returns>corpus reader specified by datasetAuxReaderClass</returns>
		private GenericDataSetReader MakeAuxReader()
		{
			try
			{
				if (auxReader == null)
				{
					if (MachineReadingProperties.datasetAuxReaderClass != null)
					{
						auxReader = MachineReadingProperties.datasetAuxReaderClass.GetConstructor().NewInstance();
					}
				}
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
			return auxReader;
		}

		public static IExtractor MakeEntityExtractor(Type entityExtractorClass, string gazetteerPath)
		{
			if (entityExtractorClass == null)
			{
				return null;
			}
			BasicEntityExtractor ex;
			try
			{
				ex = entityExtractorClass.GetConstructor(typeof(string)).NewInstance(gazetteerPath);
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
			return ex;
		}

		private static IExtractor MakeRelationExtractor(Type relationExtractorClass, RelationFeatureFactory featureFac, bool createUnrelatedRelations, RelationMentionFactory factory)
		{
			if (relationExtractorClass == null)
			{
				return null;
			}
			BasicRelationExtractor ex;
			try
			{
				ex = relationExtractorClass.GetConstructor(typeof(RelationFeatureFactory), typeof(bool), typeof(RelationMentionFactory)).NewInstance(featureFac, createUnrelatedRelations, factory);
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
			return ex;
		}

		public static RelationFeatureFactory MakeRelationFeatureFactory(Type relationFeatureFactoryClass, string relationFeatureList, bool doNotLexicalizeFirstArg)
		{
			if (relationFeatureList == null || relationFeatureFactoryClass == null)
			{
				return null;
			}
			object[] featureList = new object[] { relationFeatureList.Trim().Split(",\\s*") };
			RelationFeatureFactory rff;
			try
			{
				rff = relationFeatureFactoryClass.GetConstructor(typeof(string[])).NewInstance(featureList);
				rff.SetDoNotLexicalizeFirstArgument(doNotLexicalizeFirstArg);
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
			return rff;
		}

		private static RelationMentionFactory MakeRelationMentionFactory(Type relationMentionFactoryClass)
		{
			RelationMentionFactory rmf;
			try
			{
				rmf = relationMentionFactoryClass.GetConstructor().NewInstance();
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
			return rmf;
		}

		/// <summary>Gets the serialized sentences for a data set.</summary>
		/// <remarks>
		/// Gets the serialized sentences for a data set. If the serialized sentences
		/// are already on disk, it loads them from there. Otherwise, the data set is
		/// read with the corpus reader and the serialized sentences are saved to disk.
		/// </remarks>
		/// <param name="sentencesPath">Llocation of the raw data set</param>
		/// <param name="reader">The corpus reader</param>
		/// <param name="serializedSentences">Where the serialized sentences should be stored on disk</param>
		/// <returns>A list of RelationsSentences</returns>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private Annotation LoadOrMakeSerializedSentences(string sentencesPath, GenericDataSetReader reader, File serializedSentences)
		{
			Annotation corpusSentences;
			// if the serialized file exists, just read it. otherwise read the source
			// and and save the serialized file to disk
			if (MachineReadingProperties.serializeCorpora && serializedSentences.Exists() && !forceParseSentences)
			{
				MachineReadingProperties.logger.Info("Loaded serialized sentences from " + serializedSentences.GetAbsolutePath() + "...");
				corpusSentences = IOUtils.ReadObjectFromFile(serializedSentences);
				MachineReadingProperties.logger.Info("Done. Loaded " + corpusSentences.Get(typeof(CoreAnnotations.SentencesAnnotation)).Count + " sentences.");
			}
			else
			{
				// read the corpus
				MachineReadingProperties.logger.Info("Parsing corpus sentences...");
				if (MachineReadingProperties.serializeCorpora)
				{
					MachineReadingProperties.logger.Info("These sentences will be serialized to " + serializedSentences.GetAbsolutePath());
				}
				corpusSentences = reader.Parse(sentencesPath);
				MachineReadingProperties.logger.Info("Done. Parsed " + AnnotationUtils.SentenceCount(corpusSentences) + " sentences.");
				// save corpusSentences
				if (MachineReadingProperties.serializeCorpora)
				{
					MachineReadingProperties.logger.Info("Serializing parsed sentences to " + serializedSentences.GetAbsolutePath() + "...");
					IOUtils.WriteObjectToFile(corpusSentences, serializedSentences);
					MachineReadingProperties.logger.Info("Done. Serialized " + AnnotationUtils.SentenceCount(corpusSentences) + " sentences.");
				}
			}
			return corpusSentences;
		}

		public virtual void SetExtractEntities(bool extractEntities)
		{
			MachineReadingProperties.extractEntities = extractEntities;
		}

		public virtual void SetExtractRelations(bool extractRelations)
		{
			MachineReadingProperties.extractRelations = extractRelations;
		}

		public virtual void SetExtractEvents(bool extractEvents)
		{
			MachineReadingProperties.extractEvents = extractEvents;
		}

		public virtual void SetForceParseSentences(bool forceParseSentences)
		{
			this.forceParseSentences = forceParseSentences;
		}

		public virtual void SetDatasets(Pair<Annotation, Annotation>[] datasets)
		{
			this.datasets = datasets;
		}

		public virtual Pair<Annotation, Annotation>[] GetDatasets()
		{
			return datasets;
		}

		public virtual void SetPredictions(Annotation[][] predictions)
		{
			this.predictions = predictions;
		}

		public virtual Annotation[][] GetPredictions()
		{
			return predictions;
		}

		public virtual void SetReader(GenericDataSetReader reader)
		{
			this.reader = reader;
		}

		public virtual GenericDataSetReader GetReader()
		{
			return reader;
		}

		public virtual void SetAuxReader(GenericDataSetReader auxReader)
		{
			this.auxReader = auxReader;
		}

		public virtual GenericDataSetReader GetAuxReader()
		{
			return auxReader;
		}

		public virtual void SetEntityResultsPrinterSet(ICollection<ResultsPrinter> entityResultsPrinterSet)
		{
			this.entityResultsPrinterSet = entityResultsPrinterSet;
		}

		public virtual ICollection<ResultsPrinter> GetEntityResultsPrinterSet()
		{
			return entityResultsPrinterSet;
		}

		public virtual void SetRelationResultsPrinterSet(ICollection<ResultsPrinter> relationResultsPrinterSet)
		{
			this.relationResultsPrinterSet = relationResultsPrinterSet;
		}

		public virtual ICollection<ResultsPrinter> GetRelationResultsPrinterSet()
		{
			return relationResultsPrinterSet;
		}
	}
}
