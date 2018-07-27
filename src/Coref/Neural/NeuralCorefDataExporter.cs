using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util;

namespace Edu.Stanford.Nlp.Coref.Neural
{
	/// <summary>
	/// Outputs the CoNLL data for training the neural coreference system
	/// (implemented in python/theano).
	/// </summary>
	/// <remarks>
	/// Outputs the CoNLL data for training the neural coreference system
	/// (implemented in python/theano).
	/// See <a href="https://github.com/clarkkev/deep-coref">https://github.com/clarkkev/deep-coref</a>
	/// for the training code.
	/// </remarks>
	/// <author>Kevin Clark</author>
	public class NeuralCorefDataExporter : ICorefDocumentProcessor
	{
		private readonly bool conll;

		private readonly PrintWriter dataWriter;

		private readonly PrintWriter goldClusterWriter;

		private readonly Dictionaries dictionaries;

		public NeuralCorefDataExporter(Properties props, Dictionaries dictionaries, string dataPath, string goldClusterPath)
		{
			conll = CorefProperties.Conll(props);
			this.dictionaries = dictionaries;
			try
			{
				dataWriter = IOUtils.GetPrintWriter(dataPath);
				goldClusterWriter = IOUtils.GetPrintWriter(goldClusterPath);
			}
			catch (Exception e)
			{
				throw new Exception("Error creating data exporter", e);
			}
		}

		public virtual void Process(int id, Document document)
		{
			IJsonArrayBuilder clusters = Javax.Json.Json.CreateArrayBuilder();
			foreach (CorefCluster gold in document.goldCorefClusters.Values)
			{
				IJsonArrayBuilder c = Javax.Json.Json.CreateArrayBuilder();
				foreach (Mention m in gold.corefMentions)
				{
					c.Add(m.mentionID);
				}
				clusters.Add(c.Build());
			}
			goldClusterWriter.Println(Javax.Json.Json.CreateObjectBuilder().Add(id.ToString(), clusters.Build()).Build());
			IDictionary<Pair<int, int>, bool> mentionPairs = CorefUtils.GetLabeledMentionPairs(document);
			IList<Mention> mentionsList = CorefUtils.GetSortedMentions(document);
			IDictionary<int, IList<Mention>> mentionsByHeadIndex = new Dictionary<int, IList<Mention>>();
			foreach (Mention m_1 in mentionsList)
			{
				IList<Mention> withIndex = mentionsByHeadIndex.ComputeIfAbsent(m_1.headIndex, null);
				withIndex.Add(m_1);
			}
			IJsonObjectBuilder docFeatures = Javax.Json.Json.CreateObjectBuilder();
			docFeatures.Add("doc_id", id);
			docFeatures.Add("type", document.docType == Document.DocType.Article ? 1 : 0);
			docFeatures.Add("source", document.docInfo["DOC_ID"].Split("/")[0]);
			IJsonArrayBuilder sentences = Javax.Json.Json.CreateArrayBuilder();
			foreach (ICoreMap sentence in document.annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				sentences.Add(GetSentenceArray(sentence.Get(typeof(CoreAnnotations.TokensAnnotation))));
			}
			IJsonObjectBuilder mentions = Javax.Json.Json.CreateObjectBuilder();
			foreach (Mention m_2 in document.predictedMentionsByID.Values)
			{
				IEnumerator<SemanticGraphEdge> iterator = m_2.enhancedDependency.IncomingEdgeIterator(m_2.headIndexedWord);
				SemanticGraphEdge relation = iterator.MoveNext() ? iterator.Current : null;
				string depRelation = relation == null ? "no-parent" : relation.GetRelation().ToString();
				string depParent = relation == null ? "<missing>" : relation.GetSource().Word();
				mentions.Add(m_2.mentionNum.ToString(), Javax.Json.Json.CreateObjectBuilder().Add("doc_id", id).Add("mention_id", m_2.mentionID).Add("mention_num", m_2.mentionNum).Add("sent_num", m_2.sentNum).Add("start_index", m_2.startIndex).Add("end_index"
					, m_2.endIndex).Add("head_index", m_2.headIndex).Add("mention_type", m_2.mentionType.ToString()).Add("dep_relation", depRelation).Add("dep_parent", depParent).Add("sentence", GetSentenceArray(m_2.sentenceWords)).Add("contained-in-other-mention"
					, mentionsByHeadIndex[m_2.headIndex].Stream().AnyMatch(null) ? 1 : 0).Build());
			}
			IJsonArrayBuilder featureNames = Javax.Json.Json.CreateArrayBuilder().Add("same-speaker").Add("antecedent-is-mention-speaker").Add("mention-is-antecedent-speaker").Add("relaxed-head-match").Add("exact-string-match").Add("relaxed-string-match"
				);
			IJsonObjectBuilder features = Javax.Json.Json.CreateObjectBuilder();
			IJsonObjectBuilder labels = Javax.Json.Json.CreateObjectBuilder();
			foreach (KeyValuePair<Pair<int, int>, bool> e in mentionPairs)
			{
				Mention m1 = document.predictedMentionsByID[e.Key.first];
				Mention m2 = document.predictedMentionsByID[e.Key.second];
				string key = m1.mentionNum + " " + m2.mentionNum;
				IJsonArrayBuilder builder = Javax.Json.Json.CreateArrayBuilder();
				foreach (int val in CategoricalFeatureExtractor.PairwiseFeatures(document, m1, m2, dictionaries, conll))
				{
					builder.Add(val);
				}
				features.Add(key, builder.Build());
				labels.Add(key, e.Value ? 1 : 0);
			}
			IJsonObject docData = Javax.Json.Json.CreateObjectBuilder().Add("sentences", sentences.Build()).Add("mentions", mentions.Build()).Add("labels", labels.Build()).Add("pair_feature_names", featureNames.Build()).Add("pair_features", features.Build
				()).Add("document_features", docFeatures.Build()).Build();
			dataWriter.Println(docData);
		}

		/// <exception cref="System.Exception"/>
		public virtual void Finish()
		{
			dataWriter.Close();
			goldClusterWriter.Close();
		}

		private static IJsonArray GetSentenceArray(IList<CoreLabel> sentence)
		{
			IJsonArrayBuilder sentenceBuilder = Javax.Json.Json.CreateArrayBuilder();
			sentence.Stream().Map(null).Map(null).Map(null).ForEach(null);
			return sentenceBuilder.Build();
		}

		/// <exception cref="System.Exception"/>
		public static void ExportData(string outputPath, CorefProperties.Dataset dataset, Properties props, Dictionaries dictionaries)
		{
			CorefProperties.SetInput(props, dataset);
			string dataPath = outputPath + "/data_raw/";
			string goldClusterPath = outputPath + "/gold/";
			IOUtils.EnsureDir(new File(outputPath));
			IOUtils.EnsureDir(new File(dataPath));
			IOUtils.EnsureDir(new File(goldClusterPath));
			new Edu.Stanford.Nlp.Coref.Neural.NeuralCorefDataExporter(props, dictionaries, dataPath + dataset.ToString().ToLower(), goldClusterPath + dataset.ToString().ToLower()).Run(props, dictionaries);
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties("-props", args[0]);
			Dictionaries dictionaries = new Dictionaries(props);
			string outputPath = args[1];
			ExportData(outputPath, CorefProperties.Dataset.Train, props, dictionaries);
			ExportData(outputPath, CorefProperties.Dataset.Dev, props, dictionaries);
			ExportData(outputPath, CorefProperties.Dataset.Test, props, dictionaries);
		}
	}
}
