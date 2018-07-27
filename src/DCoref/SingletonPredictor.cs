// StanfordCoreNLP -- a suite of NLP tools
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>
	/// Train the singleton predictor using a logistic regression classifier as
	/// described in Recasens, de Marneffe and Potts, NAACL 2013
	/// Label 0 = Singleton mention.
	/// </summary>
	/// <remarks>
	/// Train the singleton predictor using a logistic regression classifier as
	/// described in Recasens, de Marneffe and Potts, NAACL 2013
	/// Label 0 = Singleton mention.
	/// Label 1 = Coreferent mention.
	/// This is an example of the properties file for this class:
	/// # This is set to true so that gold POS, gold parses and gold NEs are used.
	/// dcoref.replicate.conll = true
	/// # Path to the directory containing the CoNLL training files.
	/// dcoref.conll2011 = /path/to/conll-2012/v4/data/train/data/english/annotations/
	/// # Output file where the serialized model is saved.
	/// singleton.predictor.output = /path/to/predictor.output
	/// </remarks>
	/// <author>Marta Recasens</author>
	/// <author>Marie-Catherine de Marneffe</author>
	public class SingletonPredictor
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Dcoref.SingletonPredictor));

		private SingletonPredictor()
		{
		}

		// static main utility
		/// <summary>Set index for each token and sentence in the document.</summary>
		/// <param name="doc"/>
		private static void SetTokenIndices(Document doc)
		{
			int token_index = 0;
			foreach (ICoreMap sent in doc.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (CoreLabel token in sent.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					token.Set(typeof(CoreAnnotations.TokenBeginAnnotation), token_index++);
				}
			}
		}

		/// <summary>Generate the training features from the CoNLL input file.</summary>
		/// <returns>Dataset of feature vectors</returns>
		/// <exception cref="System.Exception"/>
		private static GeneralDataset<string, string> GenerateFeatureVectors(Properties props)
		{
			GeneralDataset<string, string> dataset = new Dataset<string, string>();
			Dictionaries dict = new Dictionaries(props);
			MentionExtractor mentionExtractor = new CoNLLMentionExtractor(dict, props, new Semantics(dict));
			Document document;
			while ((document = mentionExtractor.NextDoc()) != null)
			{
				SetTokenIndices(document);
				document.ExtractGoldCorefClusters();
				IDictionary<int, CorefCluster> entities = document.goldCorefClusters;
				// Generate features for coreferent mentions with class label 1
				foreach (CorefCluster entity in entities.Values)
				{
					foreach (Mention mention in entity.GetCorefMentions())
					{
						// Ignore verbal mentions
						if (mention.headWord.Tag().StartsWith("V"))
						{
							continue;
						}
						IndexedWord head = mention.dependency.GetNodeByIndexSafe(mention.headWord.Index());
						if (head == null)
						{
							continue;
						}
						List<string> feats = mention.GetSingletonFeatures(dict);
						dataset.Add(new BasicDatum<string, string>(feats, "1"));
					}
				}
				// Generate features for singletons with class label 0
				List<CoreLabel> gold_heads = new List<CoreLabel>();
				foreach (Mention gold_men in document.allGoldMentions.Values)
				{
					gold_heads.Add(gold_men.headWord);
				}
				foreach (Mention predicted_men in document.allPredictedMentions.Values)
				{
					SemanticGraph dep = predicted_men.dependency;
					IndexedWord head = dep.GetNodeByIndexSafe(predicted_men.headWord.Index());
					if (head == null)
					{
						continue;
					}
					// Ignore verbal mentions
					if (predicted_men.headWord.Tag().StartsWith("V"))
					{
						continue;
					}
					// If the mention is in the gold set, it is not a singleton and thus ignore
					if (gold_heads.Contains(predicted_men.headWord))
					{
						continue;
					}
					dataset.Add(new BasicDatum<string, string>(predicted_men.GetSingletonFeatures(dict), "0"));
				}
			}
			dataset.SummaryStatistics();
			return dataset;
		}

		/// <summary>Train the singleton predictor using a logistic regression classifier.</summary>
		/// <param name="pDataset">Dataset of features</param>
		/// <returns>Singleton predictor</returns>
		public static LogisticClassifier<string, string> Train(GeneralDataset<string, string> pDataset)
		{
			LogisticClassifierFactory<string, string> lcf = new LogisticClassifierFactory<string, string>();
			LogisticClassifier<string, string> classifier = lcf.TrainClassifier(pDataset);
			return classifier;
		}

		/// <summary>Saves the singleton predictor model to the given filename.</summary>
		/// <remarks>
		/// Saves the singleton predictor model to the given filename.
		/// If there is an error, a RuntimeIOException is thrown.
		/// </remarks>
		private static void SaveToSerialized(LogisticClassifier<string, string> predictor, string filename)
		{
			try
			{
				log.Info("Writing singleton predictor in serialized format to file " + filename + ' ');
				ObjectOutputStream @out = IOUtils.WriteStreamFromString(filename);
				@out.WriteObject(predictor);
				@out.Close();
				log.Info("done.");
			}
			catch (IOException ioe)
			{
				throw new RuntimeIOException(ioe);
			}
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Properties props;
			if (args.Length > 0)
			{
				props = StringUtils.ArgsToProperties(args);
			}
			else
			{
				props = new Properties();
			}
			if (!props.Contains("dcoref.conll2011"))
			{
				log.Info("-dcoref.conll2011 [input_CoNLL_corpus]: was not specified");
				return;
			}
			if (!props.Contains("singleton.predictor.output"))
			{
				log.Info("-singleton.predictor.output [output_model_file]: was not specified");
				return;
			}
			GeneralDataset<string, string> data = Edu.Stanford.Nlp.Dcoref.SingletonPredictor.GenerateFeatureVectors(props);
			LogisticClassifier<string, string> classifier = Edu.Stanford.Nlp.Dcoref.SingletonPredictor.Train(data);
			Edu.Stanford.Nlp.Dcoref.SingletonPredictor.SaveToSerialized(classifier, props.GetProperty("singleton.predictor.output"));
		}
	}
}
