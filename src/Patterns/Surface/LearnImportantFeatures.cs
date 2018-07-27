using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>
	/// The idea is that you can learn features that are important using ML algorithm
	/// and use those features in learning weights for patterns.
	/// </summary>
	/// <author>Sonal Gupta (sonalg@stanford.edu)</author>
	public class LearnImportantFeatures
	{
		public Type answerClass = typeof(CoreAnnotations.AnswerAnnotation);

		public string answerLabel = "WORD";

		internal string wordClassClusterFile = null;

		internal double thresholdWeight = null;

		internal IDictionary<string, int> clusterIds = new Dictionary<string, int>();

		internal CollectionValuedMap<int, string> clusters = new CollectionValuedMap<int, string>();

		internal string negativeWordsFiles = null;

		internal HashSet<string> negativeWords = new HashSet<string>();

		public virtual void SetUp()
		{
			System.Diagnostics.Debug.Assert((wordClassClusterFile != null));
			if (wordClassClusterFile != null)
			{
				foreach (string line in IOUtils.ReadLines(wordClassClusterFile))
				{
					string[] t = line.Split("\\s+");
					int num = System.Convert.ToInt32(t[1]);
					clusterIds[t[0]] = num;
					clusters.Add(num, t[0]);
				}
			}
			if (negativeWordsFiles != null)
			{
				foreach (string file in negativeWordsFiles.Split("[,;]"))
				{
					Sharpen.Collections.AddAll(negativeWords, IOUtils.LinesFromFile(file));
				}
				System.Console.Out.WriteLine("number of negative words from lists " + negativeWords.Count);
			}
		}

		public static bool GetRandomBoolean(Random random, double p)
		{
			return random.NextFloat() < p;
		}

		// public void getDecisionTree(Map<String, List<CoreLabel>> sents,
		// List<Pair<String, Integer>> chosen, Counter<String> weights, String
		// wekaOptions) {
		// RVFDataset<String, String> dataset = new RVFDataset<String, String>();
		// for (Pair<String, Integer> d : chosen) {
		// CoreLabel l = sents.get(d.first).get(d.second());
		// String w = l.word();
		// Integer num = this.clusterIds.get(w);
		// if (num == null)
		// num = -1;
		// double wt = weights.getCount("Cluster-" + num);
		// String label;
		// if (l.get(answerClass).toString().equals(answerLabel))
		// label = answerLabel;
		// else
		// label = "O";
		// Counter<String> feat = new ClassicCounter<String>();
		// feat.setCount("DIST", wt);
		// dataset.add(new RVFDatum<String, String>(feat, label));
		// }
		// WekaDatumClassifierFactory wekaFactory = new
		// WekaDatumClassifierFactory("weka.classifiers.trees.J48", wekaOptions);
		// WekaDatumClassifier classifier = wekaFactory.trainClassifier(dataset);
		// Classifier cls = classifier.getClassifier();
		// J48 j48decisiontree = (J48) cls;
		// System.out.println(j48decisiontree.toSummaryString());
		// System.out.println(j48decisiontree.toString());
		//
		// }
		private int Sample(IDictionary<string, DataInstance> sents, Random r, Random rneg, double perSelectNeg, double perSelectRand, int numrand, IList<Pair<string, int>> chosen, RVFDataset<string, string> dataset)
		{
			foreach (KeyValuePair<string, DataInstance> en in sents)
			{
				CoreLabel[] sent = Sharpen.Collections.ToArray(en.Value.GetTokens(), new CoreLabel[0]);
				for (int i = 0; i < sent.Length; i++)
				{
					CoreLabel l = sent[i];
					bool chooseThis = false;
					if (l.Get(answerClass).Equals(answerLabel))
					{
						chooseThis = true;
					}
					else
					{
						if ((!l.Get(answerClass).Equals("O") || negativeWords.Contains(l.Word().ToLower())) && GetRandomBoolean(r, perSelectNeg))
						{
							chooseThis = true;
						}
						else
						{
							if (GetRandomBoolean(r, perSelectRand))
							{
								numrand++;
								chooseThis = true;
							}
							else
							{
								chooseThis = false;
							}
						}
					}
					if (chooseThis)
					{
						chosen.Add(new Pair(en.Key, i));
						RVFDatum<string, string> d = GetDatum(sent, i);
						dataset.Add(d, en.Key, int.ToString(i));
					}
				}
			}
			return numrand;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual ICounter<string> GetTopFeatures(IEnumerator<Pair<IDictionary<string, DataInstance>, File>> sentsf, double perSelectRand, double perSelectNeg, string externalFeatureWeightsFileLabel)
		{
			ICounter<string> features = new ClassicCounter<string>();
			RVFDataset<string, string> dataset = new RVFDataset<string, string>();
			Random r = new Random(10);
			Random rneg = new Random(10);
			int numrand = 0;
			IList<Pair<string, int>> chosen = new List<Pair<string, int>>();
			while (sentsf.MoveNext())
			{
				Pair<IDictionary<string, DataInstance>, File> sents = sentsf.Current;
				numrand = this.Sample(sents.First(), r, rneg, perSelectNeg, perSelectRand, numrand, chosen, dataset);
			}
			/*if(batchProcessSents){
			for(File f: sentFiles){
			Map<String, List<CoreLabel>> sentsf = IOUtils.readObjectFromFile(f);
			numrand = this.sample(sentsf, r, rneg, perSelectNeg, perSelectRand, numrand, chosen, dataset);
			}
			}else
			numrand = this.sample(sents, r, rneg, perSelectNeg, perSelectRand, numrand, chosen, dataset);
			*/
			System.Console.Out.WriteLine("num random chosen: " + numrand);
			System.Console.Out.WriteLine("Number of datums per label: " + dataset.NumDatumsPerLabel());
			LogisticClassifierFactory<string, string> logfactory = new LogisticClassifierFactory<string, string>();
			LogisticClassifier<string, string> classifier = logfactory.TrainClassifier(dataset);
			ICounter<string> weights = classifier.WeightsAsCounter();
			if (!classifier.GetLabelForInternalPositiveClass().Equals(answerLabel))
			{
				weights = Counters.Scale(weights, -1);
			}
			if (thresholdWeight != null)
			{
				HashSet<string> removeKeys = new HashSet<string>();
				foreach (KeyValuePair<string, double> en in weights.EntrySet())
				{
					if (Math.Abs(en.Value) <= thresholdWeight)
					{
						removeKeys.Add(en.Key);
					}
				}
				Counters.RemoveKeys(weights, removeKeys);
				System.Console.Out.WriteLine("Removing " + removeKeys);
			}
			IOUtils.WriteStringToFile(Counters.ToSortedString(weights, weights.Size(), "%1$s:%2$f", "\n"), externalFeatureWeightsFileLabel, "utf8");
			// getDecisionTree(sents, chosen, weights, wekaOptions);
			return features;
		}

		private RVFDatum<string, string> GetDatum(CoreLabel[] sent, int i)
		{
			ICounter<string> feat = new ClassicCounter<string>();
			CoreLabel l = sent[i];
			string label;
			if (l.Get(answerClass).ToString().Equals(answerLabel))
			{
				label = answerLabel;
			}
			else
			{
				label = "O";
			}
			CollectionValuedMap<string, CandidatePhrase> matchedPhrases = l.Get(typeof(PatternsAnnotations.MatchedPhrases));
			if (matchedPhrases == null)
			{
				matchedPhrases = new CollectionValuedMap<string, CandidatePhrase>();
				matchedPhrases.Add(label, CandidatePhrase.CreateOrGet(l.Word()));
			}
			foreach (CandidatePhrase w in matchedPhrases.AllValues())
			{
				int num = this.clusterIds[w.GetPhrase()];
				if (num == null)
				{
					num = -1;
				}
				feat.SetCount("Cluster-" + num, 1.0);
			}
			// feat.incrementCount("WORD-" + l.word());
			// feat.incrementCount("LEMMA-" + l.lemma());
			// feat.incrementCount("TAG-" + l.tag());
			int window = 0;
			for (int j = Math.Max(0, i - window); j < i; j++)
			{
				CoreLabel lj = sent[j];
				feat.IncrementCount("PREV-" + "WORD-" + lj.Word());
				feat.IncrementCount("PREV-" + "LEMMA-" + lj.Lemma());
				feat.IncrementCount("PREV-" + "TAG-" + lj.Tag());
			}
			for (int j_1 = i + 1; j_1 < sent.Length && j_1 <= i + window; j_1++)
			{
				CoreLabel lj = sent[j_1];
				feat.IncrementCount("NEXT-" + "WORD-" + lj.Word());
				feat.IncrementCount("NEXT-" + "LEMMA-" + lj.Lemma());
				feat.IncrementCount("NEXT-" + "TAG-" + lj.Tag());
			}
			// System.out.println("adding " + l.word() + " as " + label);
			return new RVFDatum<string, string>(feat, label);
		}

		public static void Main(string[] args)
		{
			try
			{
				LearnImportantFeatures lmf = new LearnImportantFeatures();
				Properties props = StringUtils.ArgsToPropertiesWithResolve(args);
				ArgumentParser.FillOptions(lmf, props);
				lmf.SetUp();
				string sentsFile = props.GetProperty("sentsFile");
				IDictionary<string, DataInstance> sents = IOUtils.ReadObjectFromFile(sentsFile);
				System.Console.Out.WriteLine("Read the sents file: " + sentsFile);
				double perSelectRand = double.ParseDouble(props.GetProperty("perSelectRand"));
				double perSelectNeg = double.ParseDouble(props.GetProperty("perSelectNeg"));
			}
			catch (Exception e)
			{
				// String wekaOptions = props.getProperty("wekaOptions");
				//lmf.getTopFeatures(false, , perSelectRand, perSelectNeg, props.getProperty("externalFeatureWeightsFile"));
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
