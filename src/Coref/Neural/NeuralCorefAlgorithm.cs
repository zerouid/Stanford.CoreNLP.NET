using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;

namespace Edu.Stanford.Nlp.Coref.Neural
{
	/// <summary>Neural mention-ranking coreference model.</summary>
	/// <remarks>
	/// Neural mention-ranking coreference model. As described in:
	/// Kevin Clark and Christopher D. Manning. 2016.
	/// <a href="http://nlp.stanford.edu/pubs/clark2016deep.pdf">
	/// Deep Reinforcement Learning for Mention-Ranking Coreference Models</a>.
	/// In Empirical Methods on Natural Language Processing.
	/// Training code is implemented in python and is available at
	/// <a href="https://github.com/clarkkev/deep-coref">https://github.com/clarkkev/deep-coref</a>.
	/// </remarks>
	/// <author>Kevin Clark</author>
	public class NeuralCorefAlgorithm : ICorefAlgorithm
	{
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Coref.Neural.NeuralCorefAlgorithm));

		private readonly double greedyness;

		private readonly int maxMentionDistance;

		private readonly int maxMentionDistanceWithStringMatch;

		private readonly CategoricalFeatureExtractor featureExtractor;

		private readonly EmbeddingExtractor embeddingExtractor;

		private readonly NeuralCorefModel model;

		public NeuralCorefAlgorithm(Properties props, Dictionaries dictionaries)
		{
			greedyness = NeuralCorefProperties.Greedyness(props);
			maxMentionDistance = CorefProperties.MaxMentionDistance(props);
			maxMentionDistanceWithStringMatch = CorefProperties.MaxMentionDistanceWithStringMatch(props);
			model = IOUtils.ReadObjectAnnouncingTimingFromURLOrClasspathOrFileSystem(log, "Loading coref model", NeuralCorefProperties.ModelPath(props));
			embeddingExtractor = new EmbeddingExtractor(CorefProperties.Conll(props), IOUtils.ReadObjectAnnouncingTimingFromURLOrClasspathOrFileSystem(log, "Loading coref embeddings", NeuralCorefProperties.PretrainedEmbeddingsPath(props)), model.GetWordEmbeddings
				());
			featureExtractor = new CategoricalFeatureExtractor(props, dictionaries);
		}

		public virtual void RunCoref(Document document)
		{
			IList<Mention> sortedMentions = CorefUtils.GetSortedMentions(document);
			IDictionary<int, IList<Mention>> mentionsByHeadIndex = new Dictionary<int, IList<Mention>>();
			foreach (Mention m in sortedMentions)
			{
				IList<Mention> withIndex = mentionsByHeadIndex.ComputeIfAbsent(m.headIndex, null);
				withIndex.Add(m);
			}
			SimpleMatrix documentEmbedding = embeddingExtractor.GetDocumentEmbedding(document);
			IDictionary<int, SimpleMatrix> antecedentEmbeddings = new Dictionary<int, SimpleMatrix>();
			IDictionary<int, SimpleMatrix> anaphorEmbeddings = new Dictionary<int, SimpleMatrix>();
			ICounter<int> anaphoricityScores = new ClassicCounter<int>();
			foreach (Mention m_1 in sortedMentions)
			{
				SimpleMatrix mentionEmbedding = embeddingExtractor.GetMentionEmbeddings(m_1, documentEmbedding);
				antecedentEmbeddings[m_1.mentionID] = model.GetAntecedentEmbedding(mentionEmbedding);
				anaphorEmbeddings[m_1.mentionID] = model.GetAnaphorEmbedding(mentionEmbedding);
				anaphoricityScores.IncrementCount(m_1.mentionID, model.GetAnaphoricityScore(mentionEmbedding, featureExtractor.GetAnaphoricityFeatures(m_1, document, mentionsByHeadIndex)));
			}
			IDictionary<int, IList<int>> mentionToCandidateAntecedents = CorefUtils.HeuristicFilter(sortedMentions, maxMentionDistance, maxMentionDistanceWithStringMatch);
			foreach (KeyValuePair<int, IList<int>> e in mentionToCandidateAntecedents)
			{
				double bestScore = anaphoricityScores.GetCount(e.Key) - 50 * (greedyness - 0.5);
				int m_2 = e.Key;
				int antecedent = null;
				foreach (int ca in e.Value)
				{
					double score = model.GetPairwiseScore(antecedentEmbeddings[ca], anaphorEmbeddings[m_2], featureExtractor.GetPairFeatures(new Pair<int, int>(ca, m_2), document, mentionsByHeadIndex));
					if (score > bestScore)
					{
						bestScore = score;
						antecedent = ca;
					}
				}
				if (antecedent != null)
				{
					CorefUtils.MergeCoreferenceClusters(new Pair<int, int>(antecedent, m_2), document);
				}
			}
		}
	}
}
