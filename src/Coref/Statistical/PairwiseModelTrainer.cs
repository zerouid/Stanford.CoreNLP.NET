using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>Class for training coreference models</summary>
	/// <author>Kevin Clark</author>
	public class PairwiseModelTrainer
	{
		/// <exception cref="System.Exception"/>
		public static void TrainRanking(PairwiseModel model)
		{
			Redwood.Log("scoref-train", "Reading compression...");
			Compressor<string> compressor = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.compressorFile);
			Redwood.Log("scoref-train", "Reading train data...");
			IList<DocumentExamples> trainDocuments = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.extractedFeaturesFile);
			Redwood.Log("scoref-train", "Training...");
			for (int i = 0; i < model.GetNumEpochs(); i++)
			{
				Java.Util.Collections.Shuffle(trainDocuments);
				int j = 0;
				foreach (DocumentExamples doc in trainDocuments)
				{
					j++;
					Redwood.Log("scoref-train", "On epoch: " + i + " / " + model.GetNumEpochs() + ", document: " + j + " / " + trainDocuments.Count);
					IDictionary<int, IList<Example>> mentionToPotentialAntecedents = new Dictionary<int, IList<Example>>();
					foreach (Example e in doc.examples)
					{
						int mention = e.mentionId2;
						IList<Example> potentialAntecedents = mentionToPotentialAntecedents[mention];
						if (potentialAntecedents == null)
						{
							potentialAntecedents = new List<Example>();
							mentionToPotentialAntecedents[mention] = potentialAntecedents;
						}
						potentialAntecedents.Add(e);
					}
					IList<IList<Example>> examples = new List<IList<Example>>(mentionToPotentialAntecedents.Values);
					Java.Util.Collections.Shuffle(examples);
					foreach (IList<Example> es in examples)
					{
						if (es.Count == 0)
						{
							continue;
						}
						if (model is MaxMarginMentionRanker)
						{
							MaxMarginMentionRanker ranker = (MaxMarginMentionRanker)model;
							bool noAntecedent = es.Stream().AllMatch(null);
							es.Add(new Example(es[0], noAntecedent));
							double maxPositiveScore = -double.MaxValue;
							Example maxScoringPositive = null;
							foreach (Example e_1 in es)
							{
								double score = model.Predict(e_1, doc.mentionFeatures, compressor);
								if (e_1.label == 1)
								{
									System.Diagnostics.Debug.Assert((!noAntecedent ^ e_1.IsNewLink()));
									if (score > maxPositiveScore)
									{
										maxPositiveScore = score;
										maxScoringPositive = e_1;
									}
								}
							}
							System.Diagnostics.Debug.Assert((maxScoringPositive != null));
							double maxNegativeScore = -double.MaxValue;
							Example maxScoringNegative = null;
							MaxMarginMentionRanker.ErrorType maxScoringEt = null;
							foreach (Example e_2 in es)
							{
								double score = model.Predict(e_2, doc.mentionFeatures, compressor);
								if (e_2.label != 1)
								{
									System.Diagnostics.Debug.Assert((!(noAntecedent && e_2.IsNewLink())));
									MaxMarginMentionRanker.ErrorType et = MaxMarginMentionRanker.ErrorType.Wl;
									if (noAntecedent && !e_2.IsNewLink())
									{
										et = MaxMarginMentionRanker.ErrorType.Fl;
									}
									else
									{
										if (!noAntecedent && e_2.IsNewLink())
										{
											if (e_2.mentionType2 == Dictionaries.MentionType.Pronominal)
											{
												et = MaxMarginMentionRanker.ErrorType.FnPron;
											}
											else
											{
												et = MaxMarginMentionRanker.ErrorType.Fn;
											}
										}
									}
									if (ranker.multiplicativeCost)
									{
										score = ranker.costs[et.id] * (1 - maxPositiveScore + score);
									}
									else
									{
										score += ranker.costs[et.id];
									}
									if (score > maxNegativeScore)
									{
										maxNegativeScore = score;
										maxScoringNegative = e_2;
										maxScoringEt = et;
									}
								}
							}
							System.Diagnostics.Debug.Assert((maxScoringNegative != null));
							ranker.Learn(maxScoringPositive, maxScoringNegative, doc.mentionFeatures, compressor, maxScoringEt);
						}
						else
						{
							double maxPositiveScore = -double.MaxValue;
							double maxNegativeScore = -double.MaxValue;
							Example maxScoringPositive = null;
							Example maxScoringNegative = null;
							foreach (Example e_1 in es)
							{
								double score = model.Predict(e_1, doc.mentionFeatures, compressor);
								if (e_1.label == 1)
								{
									if (score > maxPositiveScore)
									{
										maxPositiveScore = score;
										maxScoringPositive = e_1;
									}
								}
								else
								{
									if (score > maxNegativeScore)
									{
										maxNegativeScore = score;
										maxScoringNegative = e_1;
									}
								}
							}
							model.Learn(maxScoringPositive, maxScoringNegative, doc.mentionFeatures, compressor, 1);
						}
					}
				}
			}
			Redwood.Log("scoref-train", "Writing models...");
			model.WriteModel();
		}

		public static IList<Pair<Example, IDictionary<int, CompressedFeatureVector>>> GetAnaphoricityExamples(IList<DocumentExamples> documents)
		{
			int p = 0;
			int t = 0;
			IList<Pair<Example, IDictionary<int, CompressedFeatureVector>>> examples = new List<Pair<Example, IDictionary<int, CompressedFeatureVector>>>();
			while (!documents.IsEmpty())
			{
				DocumentExamples doc = documents.Remove(documents.Count - 1);
				IDictionary<int, bool> areAnaphoric = new Dictionary<int, bool>();
				foreach (Example e in doc.examples)
				{
					bool isAnaphoric = areAnaphoric[e.mentionId2];
					if (isAnaphoric == null)
					{
						areAnaphoric[e.mentionId2] = false;
					}
					if (e.label == 1)
					{
						areAnaphoric[e.mentionId2] = true;
					}
				}
				foreach (KeyValuePair<int, bool> e_1 in areAnaphoric)
				{
					if (e_1.Value)
					{
						p++;
					}
					t++;
				}
				foreach (Example e_2 in doc.examples)
				{
					bool isAnaphoric = areAnaphoric[e_2.mentionId2];
					if (isAnaphoric != null)
					{
						Sharpen.Collections.Remove(areAnaphoric, e_2.mentionId2);
						examples.Add(new Pair<Example, IDictionary<int, CompressedFeatureVector>>(new Example(e_2, isAnaphoric), doc.mentionFeatures));
					}
				}
			}
			Redwood.Log("scoref-train", "Num anaphoricity examples " + p + " positive, " + t + " total");
			return examples;
		}

		public static IList<Pair<Example, IDictionary<int, CompressedFeatureVector>>> GetExamples(IList<DocumentExamples> documents)
		{
			IList<Pair<Example, IDictionary<int, CompressedFeatureVector>>> examples = new List<Pair<Example, IDictionary<int, CompressedFeatureVector>>>();
			while (!documents.IsEmpty())
			{
				DocumentExamples doc = documents.Remove(documents.Count - 1);
				IDictionary<int, CompressedFeatureVector> mentionFeatures = doc.mentionFeatures;
				foreach (Example e in doc.examples)
				{
					examples.Add(new Pair<Example, IDictionary<int, CompressedFeatureVector>>(e, mentionFeatures));
				}
			}
			return examples;
		}

		/// <exception cref="System.Exception"/>
		public static void TrainClassification(PairwiseModel model, bool anaphoricityModel)
		{
			int numTrainingExamples = model.GetNumTrainingExamples();
			Redwood.Log("scoref-train", "Reading compression...");
			Compressor<string> compressor = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.compressorFile);
			Redwood.Log("scoref-train", "Reading train data...");
			IList<DocumentExamples> trainDocuments = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.extractedFeaturesFile);
			Redwood.Log("scoref-train", "Building train set...");
			IList<Pair<Example, IDictionary<int, CompressedFeatureVector>>> allExamples = anaphoricityModel ? GetAnaphoricityExamples(trainDocuments) : GetExamples(trainDocuments);
			Redwood.Log("scoref-train", "Training...");
			Random random = new Random(0);
			int i = 0;
			bool stopTraining = false;
			while (!stopTraining)
			{
				Java.Util.Collections.Shuffle(allExamples, random);
				foreach (Pair<Example, IDictionary<int, CompressedFeatureVector>> pair in allExamples)
				{
					if (i++ > numTrainingExamples)
					{
						stopTraining = true;
						break;
					}
					if (i % 10000 == 0)
					{
						Redwood.Log("scoref-train", string.Format("On train example %d/%d = %.2f%%", i, numTrainingExamples, 100.0 * i / numTrainingExamples));
					}
					model.Learn(pair.first, pair.second, compressor);
				}
			}
			Redwood.Log("scoref-train", "Writing models...");
			model.WriteModel();
		}

		/// <exception cref="System.Exception"/>
		public static void Test(PairwiseModel model, string predictionsName, bool anaphoricityModel)
		{
			Redwood.Log("scoref-train", "Reading compression...");
			Compressor<string> compressor = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.compressorFile);
			Redwood.Log("scoref-train", "Reading test data...");
			IList<DocumentExamples> testDocuments = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.extractedFeaturesFile);
			Redwood.Log("scoref-train", "Building test set...");
			IList<Pair<Example, IDictionary<int, CompressedFeatureVector>>> allExamples = anaphoricityModel ? GetAnaphoricityExamples(testDocuments) : GetExamples(testDocuments);
			Redwood.Log("scoref-train", "Testing...");
			PrintWriter writer = new PrintWriter(model.GetDefaultOutputPath() + predictionsName);
			IDictionary<int, ICounter<Pair<int, int>>> scores = new Dictionary<int, ICounter<Pair<int, int>>>();
			WriteScores(allExamples, compressor, model, writer, scores);
			if (model is MaxMarginMentionRanker)
			{
				writer.Close();
				writer = new PrintWriter(model.GetDefaultOutputPath() + predictionsName + "_anaphoricity");
				testDocuments = IOUtils.ReadObjectFromFile(StatisticalCorefTrainer.extractedFeaturesFile);
				allExamples = GetAnaphoricityExamples(testDocuments);
				WriteScores(allExamples, compressor, model, writer, scores);
			}
			IOUtils.WriteObjectToFile(scores, model.GetDefaultOutputPath() + predictionsName + ".ser");
			writer.Close();
		}

		public static void WriteScores(IList<Pair<Example, IDictionary<int, CompressedFeatureVector>>> examples, Compressor<string> compressor, PairwiseModel model, PrintWriter writer, IDictionary<int, ICounter<Pair<int, int>>> scores)
		{
			int i = 0;
			foreach (Pair<Example, IDictionary<int, CompressedFeatureVector>> pair in examples)
			{
				if (i++ % 10000 == 0)
				{
					Redwood.Log("scoref-train", string.Format("On test example %d/%d = %.2f%%", i, examples.Count, 100.0 * i / examples.Count));
				}
				Example example = pair.first;
				IDictionary<int, CompressedFeatureVector> mentionFeatures = pair.second;
				double p = model.Predict(example, mentionFeatures, compressor);
				writer.Println(example.docId + " " + example.mentionId1 + "," + example.mentionId2 + " " + p + " " + example.label);
				ICounter<Pair<int, int>> docScores = scores[example.docId];
				if (docScores == null)
				{
					docScores = new ClassicCounter<Pair<int, int>>();
					scores[example.docId] = docScores;
				}
				docScores.IncrementCount(new Pair<int, int>(example.mentionId1, example.mentionId2), p);
			}
		}
	}
}
