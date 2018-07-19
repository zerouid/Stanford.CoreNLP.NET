using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.Statistical;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Neural;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Org.Ejml.Simple;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Neural
{
	/// <summary>Extracts string matching, speaker, distance, and document genre features from mentions.</summary>
	/// <author>Kevin Clark</author>
	public class CategoricalFeatureExtractor
	{
		private readonly Dictionaries dictionaries;

		private readonly IDictionary<string, int> genres;

		private readonly bool conll;

		public CategoricalFeatureExtractor(Properties props, Dictionaries dictionaries)
		{
			this.dictionaries = dictionaries;
			conll = CorefProperties.Conll(props);
			if (conll)
			{
				genres = new Dictionary<string, int>();
				genres["bc"] = 0;
				genres["bn"] = 1;
				genres["mz"] = 2;
				genres["nw"] = 3;
				bool english = CorefProperties.GetLanguage(props) == Locale.English;
				if (english)
				{
					genres["pt"] = 4;
				}
				genres["tc"] = english ? 5 : 4;
				genres["wb"] = english ? 6 : 5;
			}
			else
			{
				genres = null;
			}
		}

		public virtual SimpleMatrix GetPairFeatures(Pair<int, int> pair, Document document, IDictionary<int, IList<Mention>> mentionsByHeadIndex)
		{
			Mention m1 = document.predictedMentionsByID[pair.first];
			Mention m2 = document.predictedMentionsByID[pair.second];
			IList<int> featureVals = PairwiseFeatures(document, m1, m2, dictionaries, conll);
			SimpleMatrix features = new SimpleMatrix(featureVals.Count, 1);
			for (int i = 0; i < featureVals.Count; i++)
			{
				features.Set(i, featureVals[i]);
			}
			features = NeuralUtils.Concatenate(features, EncodeDistance(m2.sentNum - m1.sentNum), EncodeDistance(m2.mentionNum - m1.mentionNum - 1), new SimpleMatrix(new double[][] { new double[] { m1.sentNum == m2.sentNum && m1.endIndex > m2.startIndex
				 ? 1 : 0 } }), GetMentionFeatures(m1, document, mentionsByHeadIndex), GetMentionFeatures(m2, document, mentionsByHeadIndex), EncodeGenre(document));
			return features;
		}

		public static IList<int> PairwiseFeatures(Document document, Mention m1, Mention m2, Dictionaries dictionaries, bool isConll)
		{
			string speaker1 = m1.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation));
			string speaker2 = m2.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation));
			IList<int> features = new List<int>();
			features.Add(isConll ? (speaker1.Equals(speaker2) ? 1 : 0) : 0);
			features.Add(isConll ? (CorefRules.AntecedentIsMentionSpeaker(document, m2, m1, dictionaries) ? 1 : 0) : 0);
			features.Add(isConll ? (CorefRules.AntecedentIsMentionSpeaker(document, m1, m2, dictionaries) ? 1 : 0) : 0);
			features.Add(m1.HeadsAgree(m2) ? 1 : 0);
			features.Add(m1.ToString().Trim().ToLower().Equals(m2.ToString().Trim().ToLower()) ? 1 : 0);
			features.Add(FeatureExtractor.RelaxedStringMatch(m1, m2) ? 1 : 0);
			return features;
		}

		public virtual SimpleMatrix GetAnaphoricityFeatures(Mention m, Document document, IDictionary<int, IList<Mention>> mentionsByHeadIndex)
		{
			return NeuralUtils.Concatenate(GetMentionFeatures(m, document, mentionsByHeadIndex), EncodeGenre(document));
		}

		private SimpleMatrix GetMentionFeatures(Mention m, Document document, IDictionary<int, IList<Mention>> mentionsByHeadIndex)
		{
			return NeuralUtils.Concatenate(NeuralUtils.OneHot((int)(m.mentionType), 4), EncodeDistance(m.endIndex - m.startIndex - 1), new SimpleMatrix(new double[][] { new double[] { m.mentionNum / (double)document.predictedMentionsByID.Count }, new double
				[] { mentionsByHeadIndex[m.headIndex].Stream().AnyMatch(null) ? 1 : 0 } }));
		}

		private static SimpleMatrix EncodeDistance(int d)
		{
			SimpleMatrix m = new SimpleMatrix(11, 1);
			if (d < 5)
			{
				m.Set(d, 1);
			}
			else
			{
				if (d < 8)
				{
					m.Set(5, 1);
				}
				else
				{
					if (d < 16)
					{
						m.Set(6, 1);
					}
					else
					{
						if (d < 32)
						{
							m.Set(7, 1);
						}
						else
						{
							if (d < 64)
							{
								m.Set(8, 1);
							}
							else
							{
								m.Set(9, 1);
							}
						}
					}
				}
			}
			m.Set(10, Math.Min(d, 64) / 64.0);
			return m;
		}

		private SimpleMatrix EncodeGenre(Document document)
		{
			return conll ? NeuralUtils.OneHot(genres[document.docInfo["DOC_ID"].Split("/")[0]], genres.Count) : new SimpleMatrix(1, 1);
		}
	}
}
