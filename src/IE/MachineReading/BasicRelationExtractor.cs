using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading
{
	[System.Serializable]
	public class BasicRelationExtractor : IExtractor
	{
		private const long serialVersionUID = 2606577772115897869L;

		private static readonly Logger logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.IE.Machinereading.BasicRelationExtractor).FullName);

		protected internal LinearClassifier<string, string> classifier;

		public int featureCountThreshold = 2;

		public RelationFeatureFactory featureFactory;

		/// <summary>strength of the prior on the linear classifier (passed to LinearClassifierFactory) or the C constant if relationExtractorClassifierType=svm</summary>
		public double sigma = 1.0;

		/// <summary>which classifier to use (can be 'linear' or 'svm')</summary>
		public string relationExtractorClassifierType = "linear";

		/// <summary>
		/// If true, it creates automatically negative examples by generating all combinations between EntityMentions in a sentence
		/// This is the common behavior, but for some domain (i.e., KBP) it must disabled.
		/// </summary>
		/// <remarks>
		/// If true, it creates automatically negative examples by generating all combinations between EntityMentions in a sentence
		/// This is the common behavior, but for some domain (i.e., KBP) it must disabled. In these domains, the negative relation examples are created in the reader
		/// </remarks>
		protected internal bool createUnrelatedRelations;

		/// <summary>Verifies that predicted labels are compatible with the relation arguments</summary>
		private ILabelValidator validator;

		protected internal RelationMentionFactory relationMentionFactory;

		public virtual void SetValidator(ILabelValidator lv)
		{
			validator = lv;
		}

		public virtual void SetRelationExtractorClassifierType(string s)
		{
			relationExtractorClassifierType = s;
		}

		public virtual void SetFeatureCountThreshold(int i)
		{
			featureCountThreshold = i;
		}

		public virtual void SetSigma(double d)
		{
			sigma = d;
		}

		public BasicRelationExtractor(RelationFeatureFactory featureFac, bool createUnrelatedRelations, RelationMentionFactory factory)
		{
			featureFactory = featureFac;
			this.createUnrelatedRelations = createUnrelatedRelations;
			this.relationMentionFactory = factory;
			logger.SetLevel(Level.Info);
		}

		public virtual void SetCreateUnrelatedRelations(bool b)
		{
			createUnrelatedRelations = b;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.IE.Machinereading.BasicRelationExtractor Load(string modelPath)
		{
			return IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(modelPath);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void Save(string modelpath)
		{
			// make sure modelpath directory exists
			int lastSlash = modelpath.LastIndexOf(File.separator);
			if (lastSlash > 0)
			{
				string path = Sharpen.Runtime.Substring(modelpath, 0, lastSlash);
				File f = new File(path);
				if (!f.Exists())
				{
					f.Mkdirs();
				}
			}
			FileOutputStream fos = new FileOutputStream(modelpath);
			ObjectOutputStream @out = new ObjectOutputStream(fos);
			@out.WriteObject(this);
			@out.Close();
		}

		/// <summary>Train on a list of ExtractionSentence containing labeled RelationMention objects</summary>
		public virtual void Train(Annotation sentences)
		{
			// Train a single multi-class classifier
			GeneralDataset<string, string> trainSet = CreateDataset(sentences);
			TrainMulticlass(trainSet);
		}

		public virtual void TrainMulticlass(GeneralDataset<string, string> trainSet)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(relationExtractorClassifierType, "linear"))
			{
				LinearClassifierFactory<string, string> lcFactory = new LinearClassifierFactory<string, string>(1e-4, false, sigma);
				lcFactory.SetVerbose(false);
				// use in-place SGD instead of QN. this is faster but much worse!
				// lcFactory.useInPlaceStochasticGradientDescent(-1, -1, 1.0);
				// use a hybrid minimizer: start with in-place SGD, continue with QN
				// lcFactory.useHybridMinimizerWithInPlaceSGD(50, -1, sigma);
				classifier = lcFactory.TrainClassifier(trainSet);
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(relationExtractorClassifierType, "svm"))
				{
					SVMLightClassifierFactory<string, string> svmFactory = new SVMLightClassifierFactory<string, string>();
					svmFactory.SetC(sigma);
					classifier = svmFactory.TrainClassifier(trainSet);
				}
				else
				{
					throw new Exception("Invalid classifier type: " + relationExtractorClassifierType);
				}
			}
			if (logger.IsLoggable(Level.Fine))
			{
				ReportWeights(classifier, null);
			}
		}

		protected internal static void ReportWeights(LinearClassifier<string, string> classifier, string classLabel)
		{
			if (classLabel != null)
			{
				logger.Fine("CLASSIFIER WEIGHTS FOR LABEL " + classLabel);
			}
			IDictionary<string, ICounter<string>> labelsToFeatureWeights = classifier.WeightsAsMapOfCounters();
			IList<string> labels = new List<string>(labelsToFeatureWeights.Keys);
			labels.Sort();
			foreach (string label in labels)
			{
				ICounter<string> featWeights = labelsToFeatureWeights[label];
				IList<Pair<string, double>> sorted = Counters.ToSortedListWithCounts(featWeights);
				StringBuilder bos = new StringBuilder();
				bos.Append("WEIGHTS FOR LABEL ").Append(label).Append(':');
				foreach (Pair<string, double> feat in sorted)
				{
					bos.Append(' ').Append(feat.First()).Append(':').Append(feat.Second() + "\n");
				}
				logger.Fine(bos.ToString());
			}
		}

		protected internal virtual string ClassOf(IDatum<string, string> datum, ExtractionObject rel)
		{
			ICounter<string> probs = classifier.ProbabilityOf(datum);
			IList<Pair<string, double>> sortedProbs = Counters.ToDescendingMagnitudeSortedListWithCounts(probs);
			double nrProb = probs.GetCount(RelationMention.Unrelated);
			foreach (Pair<string, double> choice in sortedProbs)
			{
				if (choice.first.Equals(RelationMention.Unrelated))
				{
					return choice.first;
				}
				if (nrProb >= choice.second)
				{
					return RelationMention.Unrelated;
				}
				// no prediction, all probs have the same value
				if (CompatibleLabel(choice.first, rel))
				{
					return choice.first;
				}
			}
			return RelationMention.Unrelated;
		}

		private bool CompatibleLabel(string label, ExtractionObject rel)
		{
			if (rel == null)
			{
				return true;
			}
			if (validator != null)
			{
				return validator.ValidLabel(label, rel);
			}
			return true;
		}

		protected internal virtual ICounter<string> ProbabilityOf(IDatum<string, string> testDatum)
		{
			return classifier.ProbabilityOf(testDatum);
		}

		protected internal virtual void JustificationOf(IDatum<string, string> testDatum, PrintWriter pw, string label)
		{
			classifier.JustificationOf(testDatum, pw);
		}

		/// <summary>Predict a relation for each pair of entities in the sentence; including relations of type unrelated.</summary>
		/// <remarks>
		/// Predict a relation for each pair of entities in the sentence; including relations of type unrelated.
		/// This creates new RelationMention objects!
		/// </remarks>
		protected internal virtual IList<RelationMention> ExtractAllRelations(ICoreMap sentence)
		{
			IList<RelationMention> extractions = new List<RelationMention>();
			IList<RelationMention> cands = null;
			if (createUnrelatedRelations)
			{
				// creates all possible relations between all entities in the sentence
				cands = AnnotationUtils.GetAllUnrelatedRelations(relationMentionFactory, sentence, false);
			}
			else
			{
				// just take the candidates produced by the reader (in KBP)
				cands = sentence.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
				if (cands == null)
				{
					cands = new List<RelationMention>();
				}
			}
			// the actual classification takes place here!
			foreach (RelationMention rel in cands)
			{
				IDatum<string, string> testDatum = CreateDatum(rel);
				string label = ClassOf(testDatum, rel);
				ICounter<string> probs = ProbabilityOf(testDatum);
				double prob = probs.GetCount(label);
				StringWriter sw = new StringWriter();
				PrintWriter pw = new PrintWriter(sw);
				if (logger.IsLoggable(Level.Info))
				{
					JustificationOf(testDatum, pw, label);
				}
				logger.Info("Current sentence: " + AnnotationUtils.TokensAndNELabelsToString(rel.GetArg(0).GetSentence()) + "\n" + "Classifying relation: " + rel + "\n" + "JUSTIFICATION for label GOLD:" + rel.GetType() + " SYS:" + label + " (prob:" + prob +
					 "):\n" + sw.ToString());
				logger.Info("Justification done.");
				RelationMention relation = relationMentionFactory.ConstructRelationMention(rel.GetObjectId(), sentence, rel.GetExtent(), label, null, rel.GetArgs(), probs);
				extractions.Add(relation);
				if (!relation.GetType().Equals(rel.GetType()))
				{
					logger.Info("Classification: found different type " + relation.GetType() + " for relation: " + rel);
					logger.Info("The predicted relation is: " + relation);
					logger.Info("Current sentence: " + AnnotationUtils.TokensAndNELabelsToString(rel.GetArg(0).GetSentence()));
				}
				else
				{
					logger.Info("Classification: found similar type " + relation.GetType() + " for relation: " + rel);
					logger.Info("The predicted relation is: " + relation);
					logger.Info("Current sentence: " + AnnotationUtils.TokensAndNELabelsToString(rel.GetArg(0).GetSentence()));
				}
			}
			return extractions;
		}

		public virtual IList<string> AnnotateMulticlass(IList<IDatum<string, string>> testDatums)
		{
			IList<string> predictedLabels = new List<string>();
			foreach (IDatum<string, string> testDatum in testDatums)
			{
				string label = ClassOf(testDatum, null);
				ICounter<string> probs = ProbabilityOf(testDatum);
				double prob = probs.GetCount(label);
				StringWriter sw = new StringWriter();
				PrintWriter pw = new PrintWriter(sw);
				if (logger.IsLoggable(Level.Fine))
				{
					JustificationOf(testDatum, pw, label);
				}
				logger.Fine("JUSTIFICATION for label GOLD:" + testDatum.Label() + " SYS:" + label + " (prob:" + prob + "):\n" + sw.ToString() + "\nJustification done.");
				predictedLabels.Add(label);
				if (!testDatum.Label().Equals(label))
				{
					logger.Info("Classification: found different type " + label + " for relation: " + testDatum);
				}
				else
				{
					logger.Info("Classification: found similar type " + label + " for relation: " + testDatum);
				}
			}
			return predictedLabels;
		}

		public virtual void AnnotateSentence(ICoreMap sentence)
		{
			// this stores all relation mentions generated by this extractor
			IList<RelationMention> relations = new List<RelationMention>();
			// extractAllRelations creates new objects for every predicted relation
			foreach (RelationMention rel in ExtractAllRelations(sentence))
			{
				// add all relations. potentially useful for a joint model
				// if (! RelationMention.isUnrelatedLabel(rel.getType()))
				relations.Add(rel);
			}
			// caution: this removes the old list of relation mentions!
			foreach (RelationMention r in relations)
			{
				if (!r.GetType().Equals(RelationMention.Unrelated))
				{
					logger.Fine("Found positive relation in annotateSentence: " + r);
				}
			}
			sentence.Set(typeof(MachineReadingAnnotations.RelationMentionsAnnotation), relations);
		}

		public virtual void Annotate(Annotation dataset)
		{
			foreach (ICoreMap sentence in dataset.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				AnnotateSentence(sentence);
			}
		}

		protected internal virtual GeneralDataset<string, string> CreateDataset(Annotation corpus)
		{
			GeneralDataset<string, string> dataset = new RVFDataset<string, string>();
			foreach (ICoreMap sentence in corpus.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (RelationMention rel in AnnotationUtils.GetAllRelations(relationMentionFactory, sentence, createUnrelatedRelations))
				{
					dataset.Add(CreateDatum(rel));
				}
			}
			dataset.ApplyFeatureCountThreshold(featureCountThreshold);
			return dataset;
		}

		protected internal virtual IDatum<string, string> CreateDatum(RelationMention rel)
		{
			System.Diagnostics.Debug.Assert((featureFactory != null));
			return featureFactory.CreateDatum(rel);
		}

		protected internal virtual IDatum<string, string> CreateDatum(RelationMention rel, string label)
		{
			System.Diagnostics.Debug.Assert((featureFactory != null));
			IDatum<string, string> datum = featureFactory.CreateDatum(rel, label);
			return datum;
		}

		public virtual void SetLoggerLevel(Level level)
		{
			logger.SetLevel(level);
		}
	}
}
