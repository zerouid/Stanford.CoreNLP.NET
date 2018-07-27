using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns.Dep;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>Learn a logistic regression classifier to combine weights to score a phrase.</summary>
	/// <author>Sonal Gupta (sonalg@stanford.edu)</author>
	public class ScorePhrasesLearnFeatWt<E> : PhraseScorer<E>
		where E : Pattern
	{
		private ScorePhrasesLearnFeatWt.ClassifierType scoreClassifierType = ScorePhrasesLearnFeatWt.ClassifierType.Lr;

		private static IDictionary<string, double[]> wordVectors = null;

		public ScorePhrasesLearnFeatWt(ConstantsAndVariables constvar)
			: base(constvar)
		{
			if (constvar.useWordVectorsToComputeSim && (constvar.subsampleUnkAsNegUsingSim || constvar.expandPositivesWhenSampling || constvar.expandNegativesWhenSampling || constVars.usePhraseEvalWordVector) && wordVectors == null)
			{
				if (Data.rawFreq == null)
				{
					Data.rawFreq = new ClassicCounter<CandidatePhrase>();
					Data.ComputeRawFreqIfNull(PatternFactory.numWordsCompoundMax, constvar.batchProcessSents);
				}
				Redwood.Log(Redwood.Dbg, "Reading word vectors");
				wordVectors = new Dictionary<string, double[]>();
				foreach (string line in IOUtils.ReadLines(constVars.wordVectorFile))
				{
					string[] tok = line.Split("\\s+");
					string word = tok[0];
					CandidatePhrase p = CandidatePhrase.CreateOrGet(word);
					//save the vector if it occurs in the rawFreq, seed set, stop words, english words
					if (Data.rawFreq.ContainsKey(p) || ConstantsAndVariables.GetStopWords().Contains(p) || constvar.GetEnglishWords().Contains(word) || constvar.HasSeedWordOrOtherSem(p))
					{
						double[] d = new double[tok.Length - 1];
						for (int i = 1; i < tok.Length; i++)
						{
							d[i - 1] = double.ValueOf(tok[i]);
						}
						wordVectors[word] = d;
					}
					else
					{
						CandidatePhrase.DeletePhrase(p);
					}
				}
				Redwood.Log(Redwood.Dbg, "Read " + wordVectors.Count + " word vectors");
			}
			OOVExternalFeatWt = 0;
			OOVdictOdds = 0;
			OOVDomainNgramScore = 0;
			OOVGoogleNgramScore = 0;
		}

		public enum ClassifierType
		{
			Dt,
			Lr,
			Rf,
			Svm,
			Shiftlr,
			Linear
		}

		public TwoDimensionalCounter<CandidatePhrase, ConstantsAndVariables.ScorePhraseMeasures> phraseScoresRaw = new TwoDimensionalCounter<CandidatePhrase, ConstantsAndVariables.ScorePhraseMeasures>();

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual IClassifier LearnClassifier(string label, bool forLearningPatterns, TwoDimensionalCounter<CandidatePhrase, E> wordsPatExtracted, ICounter<E> allSelectedPatterns)
		{
			phraseScoresRaw.Clear();
			learnedScores.Clear();
			if (Data.domainNGramsFile != null)
			{
				Data.LoadDomainNGrams();
			}
			bool computeRawFreq = false;
			if (Data.rawFreq == null)
			{
				Data.rawFreq = new ClassicCounter<CandidatePhrase>();
				computeRawFreq = true;
			}
			GeneralDataset<string, ConstantsAndVariables.ScorePhraseMeasures> dataset = Choosedatums(forLearningPatterns, label, wordsPatExtracted, allSelectedPatterns, computeRawFreq);
			IClassifier classifier;
			if (scoreClassifierType.Equals(ScorePhrasesLearnFeatWt.ClassifierType.Lr))
			{
				LogisticClassifierFactory<string, ConstantsAndVariables.ScorePhraseMeasures> logfactory = new LogisticClassifierFactory<string, ConstantsAndVariables.ScorePhraseMeasures>();
				LogPrior lprior = new LogPrior();
				lprior.SetSigma(constVars.LRSigma);
				classifier = logfactory.TrainClassifier(dataset, lprior, false);
				LogisticClassifier logcl = ((LogisticClassifier)classifier);
				string l = (string)logcl.GetLabelForInternalPositiveClass();
				ICounter<string> weights = logcl.WeightsAsCounter();
				if (l.Equals(false.ToString()))
				{
					Counters.MultiplyInPlace(weights, -1);
				}
				IList<Pair<string, double>> wtd = Counters.ToDescendingMagnitudeSortedListWithCounts(weights);
				Redwood.Log(ConstantsAndVariables.minimaldebug, "The weights are " + StringUtils.Join(wtd.SubList(0, Math.Min(wtd.Count, 600)), "\n"));
			}
			else
			{
				if (scoreClassifierType.Equals(ScorePhrasesLearnFeatWt.ClassifierType.Svm))
				{
					SVMLightClassifierFactory<string, ConstantsAndVariables.ScorePhraseMeasures> svmcf = new SVMLightClassifierFactory<string, ConstantsAndVariables.ScorePhraseMeasures>(true);
					classifier = svmcf.TrainClassifier(dataset);
					ICollection<string> labels = Generics.NewHashSet(Arrays.AsList("true"));
					IList<Triple<ConstantsAndVariables.ScorePhraseMeasures, string, double>> topfeatures = ((SVMLightClassifier<string, ConstantsAndVariables.ScorePhraseMeasures>)classifier).GetTopFeatures(labels, 0, true, 600, true);
					Redwood.Log(ConstantsAndVariables.minimaldebug, "The weights are " + StringUtils.Join(topfeatures, "\n"));
				}
				else
				{
					if (scoreClassifierType.Equals(ScorePhrasesLearnFeatWt.ClassifierType.Shiftlr))
					{
						//change the dataset to basic dataset because currently ShiftParamsLR doesn't support RVFDatum
						GeneralDataset<string, ConstantsAndVariables.ScorePhraseMeasures> newdataset = new Dataset<string, ConstantsAndVariables.ScorePhraseMeasures>();
						IEnumerator<RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures>> iter = dataset.GetEnumerator();
						while (iter.MoveNext())
						{
							RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures> inst = iter.Current;
							newdataset.Add(new BasicDatum<string, ConstantsAndVariables.ScorePhraseMeasures>(inst.AsFeatures(), inst.Label()));
						}
						ShiftParamsLogisticClassifierFactory<string, ConstantsAndVariables.ScorePhraseMeasures> factory = new ShiftParamsLogisticClassifierFactory<string, ConstantsAndVariables.ScorePhraseMeasures>();
						classifier = factory.TrainClassifier(newdataset);
						//print weights
						MultinomialLogisticClassifier<string, ConstantsAndVariables.ScorePhraseMeasures> logcl = ((MultinomialLogisticClassifier)classifier);
						ICounter<ConstantsAndVariables.ScorePhraseMeasures> weights = logcl.WeightsAsGenericCounter()["true"];
						IList<Pair<ConstantsAndVariables.ScorePhraseMeasures, double>> wtd = Counters.ToDescendingMagnitudeSortedListWithCounts(weights);
						Redwood.Log(ConstantsAndVariables.minimaldebug, "The weights are " + StringUtils.Join(wtd.SubList(0, Math.Min(wtd.Count, 600)), "\n"));
					}
					else
					{
						if (scoreClassifierType.Equals(ScorePhrasesLearnFeatWt.ClassifierType.Linear))
						{
							LinearClassifierFactory<string, ConstantsAndVariables.ScorePhraseMeasures> lcf = new LinearClassifierFactory<string, ConstantsAndVariables.ScorePhraseMeasures>();
							classifier = lcf.TrainClassifier(dataset);
							ICollection<string> labels = Generics.NewHashSet(Arrays.AsList("true"));
							IList<Triple<ConstantsAndVariables.ScorePhraseMeasures, string, double>> topfeatures = ((LinearClassifier<string, ConstantsAndVariables.ScorePhraseMeasures>)classifier).GetTopFeatures(labels, 0, true, 600, true);
							Redwood.Log(ConstantsAndVariables.minimaldebug, "The weights are " + StringUtils.Join(topfeatures, "\n"));
						}
						else
						{
							throw new Exception("cannot identify classifier " + scoreClassifierType);
						}
					}
				}
			}
			//    else if (scoreClassifierType.equals(ClassifierType.RF)) {
			//      ClassifierFactory wekaFactory = new WekaDatumClassifierFactory<String, ScorePhraseMeasures>("weka.classifiers.trees.RandomForest", constVars.wekaOptions);
			//      classifier = wekaFactory.trainClassifier(dataset);
			//      Classifier cls = ((WekaDatumClassifier) classifier).getClassifier();
			//      RandomForest rf = (RandomForest) cls;
			//    }
			BufferedWriter w = new BufferedWriter(new FileWriter("tempscorestrainer.txt"));
			System.Console.Out.WriteLine("size of learned scores is " + phraseScoresRaw.Size());
			foreach (CandidatePhrase s in phraseScoresRaw.FirstKeySet())
			{
				w.Write(s + "\t" + phraseScoresRaw.GetCounter(s) + "\n");
			}
			w.Close();
			return classifier;
		}

		public override void PrintReasonForChoosing(ICounter<CandidatePhrase> phrases)
		{
			Redwood.Log(Redwood.Dbg, "Features of selected phrases");
			foreach (KeyValuePair<CandidatePhrase, double> pEn in phrases.EntrySet())
			{
				Redwood.Log(Redwood.Dbg, pEn.Key.GetPhrase() + "\t" + pEn.Value + "\t" + phraseScoresRaw.GetCounter(pEn.Key));
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		internal override ICounter<CandidatePhrase> ScorePhrases(string label, TwoDimensionalCounter<CandidatePhrase, E> terms, TwoDimensionalCounter<CandidatePhrase, E> wordsPatExtracted, ICounter<E> allSelectedPatterns, ICollection<CandidatePhrase
			> alreadyIdentifiedWords, bool forLearningPatterns)
		{
			GetAllLabeledWordsCluster();
			ICounter<CandidatePhrase> scores = new ClassicCounter<CandidatePhrase>();
			IClassifier classifier = LearnClassifier(label, forLearningPatterns, wordsPatExtracted, allSelectedPatterns);
			foreach (KeyValuePair<CandidatePhrase, ClassicCounter<E>> en in terms.EntrySet())
			{
				double score = this.ScoreUsingClassifer(classifier, en.Key, label, forLearningPatterns, en.Value, allSelectedPatterns);
				if (!double.IsNaN(score) && !score.IsInfinite())
				{
					scores.SetCount(en.Key, score);
				}
				else
				{
					Redwood.Log(Redwood.Dbg, "Ignoring " + en.Key + " because score is " + score);
				}
			}
			return scores;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public override ICounter<CandidatePhrase> ScorePhrases(string label, ICollection<CandidatePhrase> terms, bool forLearningPatterns)
		{
			GetAllLabeledWordsCluster();
			ICounter<CandidatePhrase> scores = new ClassicCounter<CandidatePhrase>();
			IClassifier classifier = LearnClassifier(label, forLearningPatterns, null, null);
			foreach (CandidatePhrase en in terms)
			{
				double score = this.ScoreUsingClassifer(classifier, en, label, forLearningPatterns, null, null);
				scores.SetCount(en, score);
			}
			return scores;
		}

		public static bool GetRandomBoolean(Random random, double p)
		{
			return random.NextFloat() < p;
		}

		internal static double Logistic(double d)
		{
			return 1 / (1 + Math.Exp(-1 * d));
		}

		internal ConcurrentHashMap<CandidatePhrase, ICounter<int>> wordClassClustersForPhrase = new ConcurrentHashMap<CandidatePhrase, ICounter<int>>();

		internal virtual ICounter<int> WordClass(string phrase, string phraseLemma)
		{
			ICounter<int> cl = new ClassicCounter<int>();
			string[] phl = null;
			if (phraseLemma != null)
			{
				phl = phraseLemma.Split("\\s+");
			}
			int i = 0;
			foreach (string w in phrase.Split("\\s+"))
			{
				int cluster = constVars.GetWordClassClusters()[w];
				if (cluster == null && phl != null)
				{
					cluster = constVars.GetWordClassClusters()[phl[i]];
				}
				//try lowercase
				if (cluster == null)
				{
					cluster = constVars.GetWordClassClusters()[w.ToLower()];
					if (cluster == null && phl != null)
					{
						cluster = constVars.GetWordClassClusters()[phl[i].ToLower()];
					}
				}
				if (cluster != null)
				{
					cl.IncrementCount(cluster);
				}
				i++;
			}
			return cl;
		}

		internal virtual void GetAllLabeledWordsCluster()
		{
			foreach (string label in constVars.GetLabels())
			{
				foreach (KeyValuePair<CandidatePhrase, double> p in constVars.GetLearnedWords(label).EntrySet())
				{
					wordClassClustersForPhrase[p.Key] = WordClass(p.Key.GetPhrase(), p.Key.GetPhraseLemma());
				}
				foreach (CandidatePhrase p_1 in constVars.GetSeedLabelDictionary()[label])
				{
					wordClassClustersForPhrase[p_1] = WordClass(p_1.GetPhrase(), p_1.GetPhraseLemma());
				}
			}
		}

		private ICounter<CandidatePhrase> ComputeSimWithWordVectors(ICollection<CandidatePhrase> candidatePhrases, ICollection<CandidatePhrase> otherPhrases, bool ignoreWordRegex, string label)
		{
			ICounter<CandidatePhrase> sims = new ClassicCounter<CandidatePhrase>(candidatePhrases.Count);
			foreach (CandidatePhrase p in candidatePhrases)
			{
				IDictionary<string, double[]> simsAvgMaxAllLabels = similaritiesWithLabeledPhrases[p.GetPhrase()];
				if (simsAvgMaxAllLabels == null)
				{
					simsAvgMaxAllLabels = new Dictionary<string, double[]>();
				}
				double[] simsAvgMax = simsAvgMaxAllLabels[label];
				if (simsAvgMax == null)
				{
					simsAvgMax = new double[PhraseScorer.Similarities.Values().Length];
				}
				// Arrays.fill(simsAvgMax, 0); // not needed; Java arrays zero initialized
				if (wordVectors.Contains(p.GetPhrase()) && (!ignoreWordRegex || !PatternFactory.ignoreWordRegex.Matcher(p.GetPhrase()).Matches()))
				{
					double[] d1 = wordVectors[p.GetPhrase()];
					BinaryHeapPriorityQueue<CandidatePhrase> topSimPhs = new BinaryHeapPriorityQueue<CandidatePhrase>(constVars.expandPhrasesNumTopSimilar);
					double allsum = 0;
					double max = double.MinValue;
					bool donotuse = false;
					foreach (CandidatePhrase other in otherPhrases)
					{
						if (p.Equals(other))
						{
							donotuse = true;
							break;
						}
						if (!wordVectors.Contains(other.GetPhrase()))
						{
							continue;
						}
						double sim;
						ScorePhrasesLearnFeatWt.PhrasePair pair = new ScorePhrasesLearnFeatWt.PhrasePair(p.GetPhrase(), other.GetPhrase());
						if (cacheSimilarities.ContainsKey(pair))
						{
							sim = cacheSimilarities.GetCount(pair);
						}
						else
						{
							double[] d2 = wordVectors[other.GetPhrase()];
							double sum = 0;
							double d1sq = 0;
							double d2sq = 0;
							for (int i = 0; i < d1.Length; i++)
							{
								sum += d1[i] * d2[i];
								d1sq += d1[i] * d1[i];
								d2sq += d2[i] * d2[i];
							}
							sim = sum / (Math.Sqrt(d1sq) * Math.Sqrt(d2sq));
							cacheSimilarities.SetCount(pair, sim);
						}
						topSimPhs.Add(other, sim);
						if (topSimPhs.Count > constVars.expandPhrasesNumTopSimilar)
						{
							topSimPhs.RemoveLastEntry();
						}
						//avgSim /= otherPhrases.size();
						allsum += sim;
						if (sim > max)
						{
							max = sim;
						}
					}
					double finalSimScore = 0;
					int numEl = 0;
					while (topSimPhs.MoveNext())
					{
						finalSimScore += topSimPhs.GetPriority();
						topSimPhs.Current;
						numEl++;
					}
					finalSimScore /= numEl;
					double prevNumItems = simsAvgMax[(int)(PhraseScorer.Similarities.Numitems)];
					double prevAvg = simsAvgMax[(int)(PhraseScorer.Similarities.Avgsim)];
					double prevMax = simsAvgMax[(int)(PhraseScorer.Similarities.Maxsim)];
					double newNumItems = prevNumItems + otherPhrases.Count;
					double newAvg = (prevAvg * prevNumItems + allsum) / (newNumItems);
					double newMax = prevMax > max ? prevMax : max;
					simsAvgMax[(int)(PhraseScorer.Similarities.Numitems)] = newNumItems;
					simsAvgMax[(int)(PhraseScorer.Similarities.Avgsim)] = newAvg;
					simsAvgMax[(int)(PhraseScorer.Similarities.Maxsim)] = newMax;
					if (!donotuse)
					{
						sims.SetCount(p, finalSimScore);
					}
				}
				else
				{
					sims.SetCount(p, double.MinValue);
				}
				simsAvgMaxAllLabels[label] = simsAvgMax;
				similaritiesWithLabeledPhrases[p.GetPhrase()] = simsAvgMaxAllLabels;
			}
			return sims;
		}

		private Pair<ICounter<CandidatePhrase>, ICounter<CandidatePhrase>> ComputeSimWithWordVectors(IList<CandidatePhrase> candidatePhrases, ICollection<CandidatePhrase> positivePhrases, IDictionary<string, ICollection<CandidatePhrase>> allPossibleNegativePhrases
			, string label)
		{
			System.Diagnostics.Debug.Assert(wordVectors != null, "Why are word vectors null?");
			ICounter<CandidatePhrase> posSims = ComputeSimWithWordVectors(candidatePhrases, positivePhrases, true, label);
			ICounter<CandidatePhrase> negSims = new ClassicCounter<CandidatePhrase>();
			foreach (KeyValuePair<string, ICollection<CandidatePhrase>> en in allPossibleNegativePhrases)
			{
				negSims.AddAll(ComputeSimWithWordVectors(candidatePhrases, en.Value, true, en.Key));
			}
			IPredicate<CandidatePhrase> retainPhrasesNotCloseToNegative = null;
			Counters.RetainKeys(posSims, retainPhrasesNotCloseToNegative);
			return new Pair(posSims, negSims);
		}

		internal virtual Pair<ICounter<CandidatePhrase>, ICounter<CandidatePhrase>> ComputeSimWithWordCluster(ICollection<CandidatePhrase> candidatePhrases, ICollection<CandidatePhrase> positivePhrases, AtomicDouble allMaxSim)
		{
			ICounter<CandidatePhrase> sims = new ClassicCounter<CandidatePhrase>(candidatePhrases.Count);
			foreach (CandidatePhrase p in candidatePhrases)
			{
				ICounter<int> feat = wordClassClustersForPhrase[p];
				if (feat == null)
				{
					feat = WordClass(p.GetPhrase(), p.GetPhraseLemma());
					wordClassClustersForPhrase[p] = feat;
				}
				double avgSim = 0;
				// Double.MIN_VALUE;
				if (feat.Size() > 0)
				{
					foreach (CandidatePhrase pos in positivePhrases)
					{
						if (p.Equals(pos))
						{
							continue;
						}
						ICounter<int> posfeat = wordClassClustersForPhrase[pos];
						if (posfeat == null)
						{
							posfeat = WordClass(pos.GetPhrase(), pos.GetPhraseLemma());
							wordClassClustersForPhrase[pos] = feat;
						}
						if (posfeat.Size() > 0)
						{
							double j = Counters.JaccardCoefficient(posfeat, feat);
							//System.out.println("clusters for positive phrase " + pos + " is " +wordClassClustersForPhrase.get(pos) + " and the features for unknown are "  + feat + " for phrase " + p);
							if (!j.IsInfinite() && !double.IsNaN(j))
							{
								avgSim += j;
							}
						}
					}
					//if (j > maxSim)
					//  maxSim = j;
					avgSim /= positivePhrases.Count;
				}
				sims.SetCount(p, avgSim);
				if (allMaxSim.Get() < avgSim)
				{
					allMaxSim.Set(avgSim);
				}
			}
			//TODO: compute similarity with neg phrases
			return new Pair(sims, null);
		}

		internal class ComputeSim : ICallable<Pair<ICounter<CandidatePhrase>, ICounter<CandidatePhrase>>>
		{
			internal IList<CandidatePhrase> candidatePhrases;

			internal string label;

			internal AtomicDouble allMaxSim;

			internal ICollection<CandidatePhrase> positivePhrases;

			internal IDictionary<string, ICollection<CandidatePhrase>> knownNegativePhrases;

			public ComputeSim(ScorePhrasesLearnFeatWt<E> _enclosing, string label, IList<CandidatePhrase> candidatePhrases, AtomicDouble allMaxSim, ICollection<CandidatePhrase> positivePhrases, IDictionary<string, ICollection<CandidatePhrase>> knownNegativePhrases
				)
			{
				this._enclosing = _enclosing;
				this.label = label;
				this.candidatePhrases = candidatePhrases;
				this.allMaxSim = allMaxSim;
				this.positivePhrases = positivePhrases;
				this.knownNegativePhrases = knownNegativePhrases;
			}

			/// <exception cref="System.Exception"/>
			public virtual Pair<ICounter<CandidatePhrase>, ICounter<CandidatePhrase>> Call()
			{
				if (this._enclosing.constVars.useWordVectorsToComputeSim)
				{
					Pair<ICounter<CandidatePhrase>, ICounter<CandidatePhrase>> phs = this._enclosing.ComputeSimWithWordVectors(this.candidatePhrases, this.positivePhrases, this.knownNegativePhrases, this.label);
					Redwood.Log(Redwood.Dbg, "Computed similarities with positive and negative phrases");
					return phs;
				}
				else
				{
					//TODO: knownnegaitvephrases
					return this._enclosing.ComputeSimWithWordCluster(this.candidatePhrases, this.positivePhrases, this.allMaxSim);
				}
			}

			private readonly ScorePhrasesLearnFeatWt<E> _enclosing;
		}

		//this chooses the ones that are not close to the positive phrases!
		/// <exception cref="System.IO.IOException"/>
		internal virtual ICollection<CandidatePhrase> ChooseUnknownAsNegatives(ICollection<CandidatePhrase> candidatePhrases, string label, ICollection<CandidatePhrase> positivePhrases, IDictionary<string, ICollection<CandidatePhrase>> knownNegativePhrases
			, BufferedWriter logFile)
		{
			IList<IList<CandidatePhrase>> threadedCandidates = GetPatternsFromDataMultiClass.GetThreadBatches(CollectionUtils.ToList(candidatePhrases), constVars.numThreads);
			ICounter<CandidatePhrase> sims = new ClassicCounter<CandidatePhrase>();
			AtomicDouble allMaxSim = new AtomicDouble(double.MinValue);
			IExecutorService executor = Executors.NewFixedThreadPool(constVars.numThreads);
			IList<IFuture<Pair<ICounter<CandidatePhrase>, ICounter<CandidatePhrase>>>> list = new List<IFuture<Pair<ICounter<CandidatePhrase>, ICounter<CandidatePhrase>>>>();
			//multi-threaded choose positive, negative and unknown
			foreach (IList<CandidatePhrase> keys in threadedCandidates)
			{
				ICallable<Pair<ICounter<CandidatePhrase>, ICounter<CandidatePhrase>>> task = new ScorePhrasesLearnFeatWt.ComputeSim(this, label, keys, allMaxSim, positivePhrases, knownNegativePhrases);
				IFuture<Pair<ICounter<CandidatePhrase>, ICounter<CandidatePhrase>>> submit = executor.Submit(task);
				list.Add(submit);
			}
			// Now retrieve the result
			foreach (IFuture<Pair<ICounter<CandidatePhrase>, ICounter<CandidatePhrase>>> future in list)
			{
				try
				{
					sims.AddAll(future.Get().First());
				}
				catch (Exception e)
				{
					executor.ShutdownNow();
					throw new Exception(e);
				}
			}
			executor.Shutdown();
			if (allMaxSim.Get() == double.MinValue)
			{
				Redwood.Log(Redwood.Dbg, "No similarity recorded between the positives and the unknown!");
			}
			CandidatePhrase k = Counters.Argmax(sims);
			System.Console.Out.WriteLine("Maximum similarity was " + sims.GetCount(k) + " for word " + k);
			ICounter<CandidatePhrase> removed = Counters.RetainBelow(sims, constVars.positiveSimilarityThresholdLowPrecision);
			System.Console.Out.WriteLine("removing phrases as negative phrases that were higher that positive similarity threshold of " + constVars.positiveSimilarityThresholdLowPrecision + removed);
			if (logFile != null && wordVectors != null)
			{
				foreach (KeyValuePair<CandidatePhrase, double> en in removed.EntrySet())
				{
					if (wordVectors.Contains(en.Key.GetPhrase()))
					{
						logFile.Write(en.Key + "-PN " + ArrayUtils.ToString(wordVectors[en.Key.GetPhrase()], " ") + "\n");
					}
				}
			}
			//Collection<CandidatePhrase> removed = Counters.retainBottom(sims, (int) (sims.size() * percentage));
			//System.out.println("not choosing " + removed + " as the negative phrases. percentage is " + percentage + " and allMaxsim was " + allMaxSim);
			return sims.KeySet();
		}

		internal virtual ICollection<CandidatePhrase> ChooseUnknownPhrases(DataInstance sent, Random random, double perSelect, Type positiveClass, string label, int maxNum)
		{
			ICollection<CandidatePhrase> unknownSamples = new HashSet<CandidatePhrase>();
			if (maxNum == 0)
			{
				return unknownSamples;
			}
			IPredicate<CoreLabel> acceptWord = null;
			Random r = new Random(0);
			IList<int> lengths = new List<int>();
			for (int i = 1; i <= PatternFactory.numWordsCompoundMapped[label]; i++)
			{
				lengths.Add(i);
			}
			int length = CollectionUtils.Sample(lengths, r);
			if (constVars.patternType.Equals(PatternFactory.PatternType.Dep))
			{
				ExtractPhraseFromPattern extract = new ExtractPhraseFromPattern(true, length);
				SemanticGraph g = ((DataInstanceDep)sent).GetGraph();
				ICollection<CoreLabel> sampledHeads = CollectionUtils.SampleWithoutReplacement(sent.GetTokens(), Math.Min(maxNum, (int)(perSelect * sent.GetTokens().Count)), random);
				//TODO: change this for more efficient implementation
				IList<string> textTokens = sent.GetTokens().Stream().Map(null).Collect(Collectors.ToList());
				foreach (CoreLabel l in sampledHeads)
				{
					if (!acceptWord.Test(l))
					{
						continue;
					}
					IndexedWord w = g.GetNodeByIndex(l.Index());
					IList<string> outputPhrases = new List<string>();
					IList<ExtractedPhrase> extractedPhrases = new List<ExtractedPhrase>();
					IList<IntPair> outputIndices = new List<IntPair>();
					extract.PrintSubGraph(g, w, new List<string>(), textTokens, outputPhrases, outputIndices, new List<IndexedWord>(), new List<IndexedWord>(), false, extractedPhrases, null, acceptWord);
					foreach (ExtractedPhrase p in extractedPhrases)
					{
						unknownSamples.Add(CandidatePhrase.CreateOrGet(p.GetValue(), null, p.GetFeatures()));
					}
				}
			}
			else
			{
				if (constVars.patternType.Equals(PatternFactory.PatternType.Surface))
				{
					CoreLabel[] tokens = Sharpen.Collections.ToArray(sent.GetTokens(), new CoreLabel[0]);
					for (int i_1 = 0; i_1 < tokens.Length; i_1++)
					{
						if (random.NextDouble() < perSelect)
						{
							int left = (int)((length - 1) / 2.0);
							int right = length - 1 - left;
							string ph = string.Empty;
							bool haspositive = false;
							for (int j = Math.Max(0, i_1 - left); j < tokens.Length && j <= i_1 + right; j++)
							{
								if (tokens[j].Get(positiveClass).Equals(label))
								{
									haspositive = true;
									break;
								}
								ph += " " + tokens[j].Word();
							}
							ph = ph.Trim();
							if (!haspositive && !ph.Trim().IsEmpty() && !constVars.functionWords.Contains(ph))
							{
								unknownSamples.Add(CandidatePhrase.CreateOrGet(ph));
							}
						}
					}
				}
				else
				{
					throw new Exception("not yet implemented");
				}
			}
			return unknownSamples;
		}

		private static bool HasElement<E, F>(IDictionary<E, ICollection<F>> values, F value, E ignoreLabel)
		{
			foreach (KeyValuePair<E, ICollection<F>> en in values)
			{
				if (en.Key.Equals(ignoreLabel))
				{
					continue;
				}
				if (en.Value.Contains(value))
				{
					return true;
				}
			}
			return false;
		}

		internal virtual ICounter<string> NumLabeledTokens()
		{
			ICounter<string> counter = new ClassicCounter<string>();
			ConstantsAndVariables.DataSentsIterator data = new ConstantsAndVariables.DataSentsIterator(constVars.batchProcessSents);
			while (data.MoveNext())
			{
				IDictionary<string, DataInstance> sentsf = data.Current.First();
				foreach (KeyValuePair<string, DataInstance> en in sentsf)
				{
					foreach (CoreLabel l in en.Value.GetTokens())
					{
						foreach (KeyValuePair<string, Type> enc in constVars.GetAnswerClass())
						{
							if (l.Get(enc.Value).Equals(enc.Key))
							{
								counter.IncrementCount(enc.Key);
							}
						}
					}
				}
			}
			return counter;
		}

		internal ICounter<CandidatePhrase> closeToPositivesFirstIter = null;

		internal ICounter<CandidatePhrase> closeToNegativesFirstIter = null;

		public class ChooseDatumsThread : ICallable
		{
			internal ICollection<string> keys;

			internal IDictionary<string, DataInstance> sents;

			internal Type answerClass;

			internal string answerLabel;

			internal TwoDimensionalCounter<CandidatePhrase, E> wordsPatExtracted;

			internal ICounter<E> allSelectedPatterns;

			internal ICounter<int> wordClassClustersOfPositive;

			internal IDictionary<string, ICollection<CandidatePhrase>> allPossiblePhrases;

			internal bool expandPos;

			internal bool expandNeg;

			public ChooseDatumsThread(ScorePhrasesLearnFeatWt<E> _enclosing, string label, IDictionary<string, DataInstance> sents, ICollection<string> keys, TwoDimensionalCounter<CandidatePhrase, E> wordsPatExtracted, ICounter<E> allSelectedPatterns, ICounter
				<int> wordClassClustersOfPositive, IDictionary<string, ICollection<CandidatePhrase>> allPossiblePhrases, bool expandPos, bool expandNeg)
			{
				this._enclosing = _enclosing;
				this.answerLabel = label;
				this.sents = sents;
				this.keys = keys;
				this.wordsPatExtracted = wordsPatExtracted;
				this.allSelectedPatterns = allSelectedPatterns;
				this.wordClassClustersOfPositive = wordClassClustersOfPositive;
				this.allPossiblePhrases = allPossiblePhrases;
				this.answerClass = this._enclosing.constVars.GetAnswerClass()[this.answerLabel];
				this.expandNeg = expandNeg;
				this.expandPos = expandPos;
			}

			/// <exception cref="System.Exception"/>
			public virtual Quintuple<ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICounter<CandidatePhrase>, ICounter<CandidatePhrase>> Call()
			{
				Random r = new Random(10);
				Random rneg = new Random(10);
				ICollection<CandidatePhrase> allPositivePhrases = new HashSet<CandidatePhrase>();
				ICollection<CandidatePhrase> allNegativePhrases = new HashSet<CandidatePhrase>();
				ICollection<CandidatePhrase> allUnknownPhrases = new HashSet<CandidatePhrase>();
				ICounter<CandidatePhrase> allCloseToPositivePhrases = new ClassicCounter<CandidatePhrase>();
				ICounter<CandidatePhrase> allCloseToNegativePhrases = new ClassicCounter<CandidatePhrase>();
				ICollection<CandidatePhrase> knownPositivePhrases = CollectionUtils.UnionAsSet(this._enclosing.constVars.GetLearnedWords(this.answerLabel).KeySet(), this._enclosing.constVars.GetSeedLabelDictionary()[this.answerLabel]);
				ICollection<CandidatePhrase> allConsideredPhrases = new HashSet<CandidatePhrase>();
				IDictionary<Type, object> otherIgnoreClasses = this._enclosing.constVars.GetIgnoreWordswithClassesDuringSelection()[this.answerLabel];
				int numlabeled = 0;
				foreach (string sentid in this.keys)
				{
					DataInstance sentInst = this.sents[sentid];
					IList<CoreLabel> value = sentInst.GetTokens();
					CoreLabel[] sent = Sharpen.Collections.ToArray(value, new CoreLabel[value.Count]);
					for (int i = 0; i < sent.Length; i++)
					{
						CoreLabel l = sent[i];
						if (l.Get(this.answerClass).Equals(this.answerLabel))
						{
							numlabeled++;
							CandidatePhrase candidate = l.Get(typeof(PatternsAnnotations.LongestMatchedPhraseForEachLabel))[this.answerLabel];
							if (candidate == null)
							{
								throw new Exception("for sentence id " + sentid + " and token id " + i + " candidate is null for " + l.Word() + " and longest matching" + l.Get(typeof(PatternsAnnotations.LongestMatchedPhraseForEachLabel)) + " and matched phrases are " + l.Get
									(typeof(PatternsAnnotations.MatchedPhrases)));
							}
							//candidate = CandidatePhrase.createOrGet(l.word());
							//If the phrase does not exist in its form in the datset (happens when fuzzy matching etc).
							if (!Data.rawFreq.ContainsKey(candidate))
							{
								candidate = CandidatePhrase.CreateOrGet(l.Word());
							}
							//Do not add to positive if the word is a "negative" (stop word, english word, ...)
							if (ScorePhrasesLearnFeatWt.HasElement(this.allPossiblePhrases, candidate, this.answerLabel) || PatternFactory.ignoreWordRegex.Matcher(candidate.GetPhrase()).Matches())
							{
								continue;
							}
							allPositivePhrases.Add(candidate);
						}
						else
						{
							IDictionary<string, CandidatePhrase> longestMatching = l.Get(typeof(PatternsAnnotations.LongestMatchedPhraseForEachLabel));
							bool ignoreclass = false;
							CandidatePhrase candidate = CandidatePhrase.CreateOrGet(l.Word());
							foreach (Type cl in otherIgnoreClasses.Keys)
							{
								if ((bool)l.Get(cl))
								{
									ignoreclass = true;
									candidate = longestMatching.Contains("OTHERSEM") ? longestMatching["OTHERSEM"] : candidate;
									break;
								}
							}
							if (!ignoreclass)
							{
								ignoreclass = this._enclosing.constVars.functionWords.Contains(l.Word());
							}
							bool negative = false;
							bool add = false;
							foreach (KeyValuePair<string, CandidatePhrase> lo in longestMatching)
							{
								//assert !lo.getValue().getPhrase().isEmpty() : "How is the longestmatching phrase for " + l.word() + " empty ";
								if (!lo.Key.Equals(this.answerLabel) && lo.Value != null)
								{
									negative = true;
									add = true;
									//If the phrase does not exist in its form in the datset (happens when fuzzy matching etc).
									if (Data.rawFreq.ContainsKey(lo.Value))
									{
										candidate = lo.Value;
									}
								}
							}
							if (!negative && ignoreclass)
							{
								add = true;
							}
							if (add && rneg.NextDouble() < this._enclosing.constVars.perSelectNeg)
							{
								System.Diagnostics.Debug.Assert(!candidate.GetPhrase().IsEmpty());
								allNegativePhrases.Add(candidate);
							}
							if (!negative && !ignoreclass && (this.expandPos || this.expandNeg) && !ScorePhrasesLearnFeatWt.HasElement(this.allPossiblePhrases, candidate, this.answerLabel) && !PatternFactory.ignoreWordRegex.Matcher(candidate.GetPhrase()).Matches())
							{
								if (!allConsideredPhrases.Contains(candidate))
								{
									Pair<ICounter<CandidatePhrase>, ICounter<CandidatePhrase>> sims;
									System.Diagnostics.Debug.Assert(candidate != null);
									if (this._enclosing.constVars.useWordVectorsToComputeSim)
									{
										sims = this._enclosing.ComputeSimWithWordVectors(Arrays.AsList(candidate), knownPositivePhrases, this.allPossiblePhrases, this.answerLabel);
									}
									else
									{
										sims = this._enclosing.ComputeSimWithWordCluster(Arrays.AsList(candidate), knownPositivePhrases, new AtomicDouble());
									}
									bool addedAsPos = false;
									if (this.expandPos)
									{
										double sim = sims.First().GetCount(candidate);
										if (sim > this._enclosing.constVars.similarityThresholdHighPrecision)
										{
											allCloseToPositivePhrases.SetCount(candidate, sim);
											addedAsPos = true;
										}
									}
									if (this.expandNeg && !addedAsPos)
									{
										double simneg = sims.Second().GetCount(candidate);
										if (simneg > this._enclosing.constVars.similarityThresholdHighPrecision)
										{
											allCloseToNegativePhrases.SetCount(candidate, simneg);
										}
									}
									allConsideredPhrases.Add(candidate);
								}
							}
						}
					}
					Sharpen.Collections.AddAll(allUnknownPhrases, this._enclosing.ChooseUnknownPhrases(sentInst, r, this._enclosing.constVars.perSelectRand, this._enclosing.constVars.GetAnswerClass()[this.answerLabel], this.answerLabel, Math.Max(0, int.MaxValue
						)));
				}
				//
				//        if (negative && getRandomBoolean(rneg, perSelectNeg)) {
				//          numneg++;
				//        } else if (getRandomBoolean(r, perSelectRand)) {
				//          candidate = CandidatePhrase.createOrGet(l.word());
				//          numneg++;
				//        } else {
				//          continue;
				//        }
				//
				//
				//          chosen.add(new Pair<String, Integer>(en.getKey(), i));
				return new Quintuple(allPositivePhrases, allNegativePhrases, allUnknownPhrases, allCloseToPositivePhrases, allCloseToNegativePhrases);
			}

			private readonly ScorePhrasesLearnFeatWt<E> _enclosing;
		}

		private class PhrasePair
		{
			internal readonly string p1;

			internal readonly string p2;

			internal readonly int hashCode;

			public PhrasePair(string p1, string p2)
			{
				if (string.CompareOrdinal(p1, p2) <= 0)
				{
					this.p1 = p1;
					this.p2 = p2;
				}
				else
				{
					this.p1 = p2;
					this.p2 = p1;
				}
				this.hashCode = p1.GetHashCode() + p2.GetHashCode() + 331;
			}

			public override int GetHashCode()
			{
				return hashCode;
			}

			public override bool Equals(object o)
			{
				if (!(o is ScorePhrasesLearnFeatWt.PhrasePair))
				{
					return false;
				}
				ScorePhrasesLearnFeatWt.PhrasePair p = (ScorePhrasesLearnFeatWt.PhrasePair)o;
				if (p.GetPhrase1().Equals(this.GetPhrase1()) && p.GetPhrase2().Equals(this.GetPhrase2()))
				{
					return true;
				}
				return false;
			}

			public virtual string GetPhrase1()
			{
				return p1;
			}

			public virtual string GetPhrase2()
			{
				return p2;
			}
		}

		internal static ICounter<ScorePhrasesLearnFeatWt.PhrasePair> cacheSimilarities = new ConcurrentHashCounter<ScorePhrasesLearnFeatWt.PhrasePair>();

		internal static IDictionary<string, IDictionary<string, double[]>> similaritiesWithLabeledPhrases = new ConcurrentHashMap<string, IDictionary<string, double[]>>();

		//First map is phrase, second map is label to similarity stats
		internal virtual IDictionary<string, ICollection<CandidatePhrase>> GetAllPossibleNegativePhrases(string answerLabel)
		{
			//make all possible negative phrases
			IDictionary<string, ICollection<CandidatePhrase>> allPossiblePhrases = new Dictionary<string, ICollection<CandidatePhrase>>();
			ICollection<CandidatePhrase> negPhrases = new HashSet<CandidatePhrase>();
			//negPhrases.addAll(constVars.getOtherSemanticClassesWords());
			Sharpen.Collections.AddAll(negPhrases, ConstantsAndVariables.GetStopWords());
			Sharpen.Collections.AddAll(negPhrases, CandidatePhrase.ConvertStringPhrases(constVars.functionWords));
			Sharpen.Collections.AddAll(negPhrases, CandidatePhrase.ConvertStringPhrases(constVars.GetEnglishWords()));
			allPossiblePhrases["NEGATIVE"] = negPhrases;
			foreach (string label in constVars.GetLabels())
			{
				if (!label.Equals(answerLabel))
				{
					allPossiblePhrases[label] = new HashSet<CandidatePhrase>();
					if (constVars.GetLearnedWordsEachIter().Contains(label))
					{
						Sharpen.Collections.AddAll(allPossiblePhrases[label], constVars.GetLearnedWords(label).KeySet());
					}
					Sharpen.Collections.AddAll(allPossiblePhrases[label], constVars.GetSeedLabelDictionary()[label]);
				}
			}
			allPossiblePhrases["OTHERSEM"] = constVars.GetOtherSemanticClassesWords();
			return allPossiblePhrases;
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual GeneralDataset<string, ConstantsAndVariables.ScorePhraseMeasures> Choosedatums(bool forLearningPattern, string answerLabel, TwoDimensionalCounter<CandidatePhrase, E> wordsPatExtracted, ICounter<E> allSelectedPatterns, bool computeRawFreq
			)
		{
			bool expandNeg = false;
			if (closeToNegativesFirstIter == null)
			{
				closeToNegativesFirstIter = new ClassicCounter<CandidatePhrase>();
				if (constVars.expandNegativesWhenSampling)
				{
					expandNeg = true;
				}
			}
			bool expandPos = false;
			if (closeToPositivesFirstIter == null)
			{
				closeToPositivesFirstIter = new ClassicCounter<CandidatePhrase>();
				if (constVars.expandPositivesWhenSampling)
				{
					expandPos = true;
				}
			}
			ICounter<int> distSimClustersOfPositive = new ClassicCounter<int>();
			if ((expandPos || expandNeg) && !constVars.useWordVectorsToComputeSim)
			{
				foreach (CandidatePhrase s in CollectionUtils.Union(constVars.GetLearnedWords(answerLabel).KeySet(), constVars.GetSeedLabelDictionary()[answerLabel]))
				{
					string[] toks = s.GetPhrase().Split("\\s+");
					int num = constVars.GetWordClassClusters()[s.GetPhrase()];
					if (num == null)
					{
						num = constVars.GetWordClassClusters()[s.GetPhrase().ToLower()];
					}
					if (num == null)
					{
						foreach (string tok in toks)
						{
							int toknum = constVars.GetWordClassClusters()[tok];
							if (toknum == null)
							{
								toknum = constVars.GetWordClassClusters()[tok.ToLower()];
							}
							if (toknum != null)
							{
								distSimClustersOfPositive.IncrementCount(toknum);
							}
						}
					}
					else
					{
						distSimClustersOfPositive.IncrementCount(num);
					}
				}
			}
			//computing this regardless of expandpos and expandneg because we reject all positive words that occur in negatives (can happen in multi word phrases etc)
			IDictionary<string, ICollection<CandidatePhrase>> allPossibleNegativePhrases = GetAllPossibleNegativePhrases(answerLabel);
			GeneralDataset<string, ConstantsAndVariables.ScorePhraseMeasures> dataset = new RVFDataset<string, ConstantsAndVariables.ScorePhraseMeasures>();
			int numpos = 0;
			ICollection<CandidatePhrase> allNegativePhrases = new HashSet<CandidatePhrase>();
			ICollection<CandidatePhrase> allUnknownPhrases = new HashSet<CandidatePhrase>();
			ICollection<CandidatePhrase> allPositivePhrases = new HashSet<CandidatePhrase>();
			//Counter<CandidatePhrase> allCloseToPositivePhrases = new ClassicCounter<CandidatePhrase>();
			//Counter<CandidatePhrase> allCloseToNegativePhrases = new ClassicCounter<CandidatePhrase>();
			//for all sentences brtch
			ConstantsAndVariables.DataSentsIterator sentsIter = new ConstantsAndVariables.DataSentsIterator(constVars.batchProcessSents);
			while (sentsIter.MoveNext())
			{
				Pair<IDictionary<string, DataInstance>, File> sentsf = sentsIter.Current;
				IDictionary<string, DataInstance> sents = sentsf.First();
				Redwood.Log(Redwood.Dbg, "Sampling datums from " + sentsf.Second());
				if (computeRawFreq)
				{
					Data.ComputeRawFreqIfNull(sents, PatternFactory.numWordsCompoundMax);
				}
				IList<IList<string>> threadedSentIds = GetPatternsFromDataMultiClass.GetThreadBatches(new List<string>(sents.Keys), constVars.numThreads);
				IExecutorService executor = Executors.NewFixedThreadPool(constVars.numThreads);
				IList<IFuture<Quintuple<ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICounter<CandidatePhrase>, ICounter<CandidatePhrase>>>> list = new List<IFuture<Quintuple<ICollection<CandidatePhrase>, ICollection
					<CandidatePhrase>, ICollection<CandidatePhrase>, ICounter<CandidatePhrase>, ICounter<CandidatePhrase>>>>();
				//multi-threaded choose positive, negative and unknown
				foreach (IList<string> keys in threadedSentIds)
				{
					ICallable<Quintuple<ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICounter<CandidatePhrase>, ICounter<CandidatePhrase>>> task = new ScorePhrasesLearnFeatWt.ChooseDatumsThread(this, answerLabel, sents
						, keys, wordsPatExtracted, allSelectedPatterns, distSimClustersOfPositive, allPossibleNegativePhrases, expandPos, expandNeg);
					IFuture<Quintuple<ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICounter<CandidatePhrase>, ICounter<CandidatePhrase>>> submit = executor.Submit(task);
					list.Add(submit);
				}
				// Now retrieve the result
				foreach (IFuture<Quintuple<ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICounter<CandidatePhrase>, ICounter<CandidatePhrase>>> future in list)
				{
					try
					{
						Quintuple<ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICollection<CandidatePhrase>, ICounter<CandidatePhrase>, ICounter<CandidatePhrase>> result = future.Get();
						Sharpen.Collections.AddAll(allPositivePhrases, result.First());
						Sharpen.Collections.AddAll(allNegativePhrases, result.Second());
						Sharpen.Collections.AddAll(allUnknownPhrases, result.Third());
						if (expandPos)
						{
							foreach (KeyValuePair<CandidatePhrase, double> en in result.Fourth().EntrySet())
							{
								closeToPositivesFirstIter.SetCount(en.Key, en.Value);
							}
						}
						if (expandNeg)
						{
							foreach (KeyValuePair<CandidatePhrase, double> en_1 in result.Fifth().EntrySet())
							{
								closeToNegativesFirstIter.SetCount(en_1.Key, en_1.Value);
							}
						}
					}
					catch (Exception e)
					{
						executor.ShutdownNow();
						throw new Exception(e);
					}
				}
				executor.Shutdown();
			}
			//Set<CandidatePhrase> knownPositivePhrases = CollectionUtils.unionAsSet(constVars.getLearnedWords().get(answerLabel).keySet(), constVars.getSeedLabelDictionary().get(answerLabel));
			//TODO: this is kinda not nice; how is allpositivephrases different from positivephrases again?
			Sharpen.Collections.AddAll(allPositivePhrases, constVars.GetLearnedWords(answerLabel).KeySet());
			//allPositivePhrases.addAll(knownPositivePhrases);
			BufferedWriter logFile = null;
			BufferedWriter logFileFeat = null;
			if (constVars.logFileVectorSimilarity != null)
			{
				logFile = new BufferedWriter(new FileWriter(constVars.logFileVectorSimilarity));
				logFileFeat = new BufferedWriter(new FileWriter(constVars.logFileVectorSimilarity + "_feat"));
				if (wordVectors != null)
				{
					foreach (CandidatePhrase p in allPositivePhrases)
					{
						if (wordVectors.Contains(p.GetPhrase()))
						{
							logFile.Write(p.GetPhrase() + "-P " + ArrayUtils.ToString(wordVectors[p.GetPhrase()], " ") + "\n");
						}
					}
				}
			}
			if (constVars.expandPositivesWhenSampling)
			{
				//TODO: patwtbyfrew
				//Counters.retainTop(allCloseToPositivePhrases, (int) (allCloseToPositivePhrases.size()*constVars.subSampleUnkAsPosUsingSimPercentage));
				Redwood.Log("Expanding positives by adding " + Counters.ToSortedString(closeToPositivesFirstIter, closeToPositivesFirstIter.Size(), "%1$s:%2$f", "\t") + " phrases");
				Sharpen.Collections.AddAll(allPositivePhrases, closeToPositivesFirstIter.KeySet());
				//write log
				if (logFile != null && wordVectors != null && expandNeg)
				{
					foreach (CandidatePhrase p in closeToPositivesFirstIter.KeySet())
					{
						if (wordVectors.Contains(p.GetPhrase()))
						{
							logFile.Write(p.GetPhrase() + "-PP " + ArrayUtils.ToString(wordVectors[p.GetPhrase()], " ") + "\n");
						}
					}
				}
			}
			if (constVars.expandNegativesWhenSampling)
			{
				//TODO: patwtbyfrew
				//Counters.retainTop(allCloseToPositivePhrases, (int) (allCloseToPositivePhrases.size()*constVars.subSampleUnkAsPosUsingSimPercentage));
				Redwood.Log("Expanding negatives by adding " + Counters.ToSortedString(closeToNegativesFirstIter, closeToNegativesFirstIter.Size(), "%1$s:%2$f", "\t") + " phrases");
				Sharpen.Collections.AddAll(allNegativePhrases, closeToNegativesFirstIter.KeySet());
				//write log
				if (logFile != null && wordVectors != null && expandNeg)
				{
					foreach (CandidatePhrase p in closeToNegativesFirstIter.KeySet())
					{
						if (wordVectors.Contains(p.GetPhrase()))
						{
							logFile.Write(p.GetPhrase() + "-NN " + ArrayUtils.ToString(wordVectors[p.GetPhrase()], " ") + "\n");
						}
					}
				}
			}
			System.Console.Out.WriteLine("all positive phrases of size " + allPositivePhrases.Count + " are  " + allPositivePhrases);
			foreach (CandidatePhrase candidate in allPositivePhrases)
			{
				ICounter<ConstantsAndVariables.ScorePhraseMeasures> feat;
				//CandidatePhrase candidate = new CandidatePhrase(l.word());
				if (forLearningPattern)
				{
					feat = GetPhraseFeaturesForPattern(answerLabel, candidate);
				}
				else
				{
					feat = GetFeatures(answerLabel, candidate, wordsPatExtracted.GetCounter(candidate), allSelectedPatterns);
				}
				RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures> datum = new RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures>(feat, "true");
				dataset.Add(datum);
				numpos += 1;
				if (logFileFeat != null)
				{
					logFileFeat.Write("POSITIVE " + candidate.GetPhrase() + "\t" + Counters.ToSortedByKeysString(feat, "%1$s:%2$.0f", ";", "%s") + "\n");
				}
			}
			Redwood.Log(Redwood.Dbg, "Number of pure negative phrases is " + allNegativePhrases.Count);
			Redwood.Log(Redwood.Dbg, "Number of unknown phrases is " + allUnknownPhrases.Count);
			if (constVars.subsampleUnkAsNegUsingSim)
			{
				ICollection<CandidatePhrase> chosenUnknown = ChooseUnknownAsNegatives(allUnknownPhrases, answerLabel, allPositivePhrases, allPossibleNegativePhrases, logFile);
				Redwood.Log(Redwood.Dbg, "Choosing " + chosenUnknown.Count + " unknowns as negative based to their similarity to the positive phrases");
				Sharpen.Collections.AddAll(allNegativePhrases, chosenUnknown);
			}
			else
			{
				Sharpen.Collections.AddAll(allNegativePhrases, allUnknownPhrases);
			}
			if (allNegativePhrases.Count > numpos)
			{
				Redwood.Log(Redwood.Warn, "Num of negative (" + allNegativePhrases.Count + ") is higher than number of positive phrases (" + numpos + ") = " + (allNegativePhrases.Count / (double)numpos) + ". " + "Capping the number by taking the first numPositives as negative. Consider decreasing perSelectRand"
					);
				int i = 0;
				ICollection<CandidatePhrase> selectedNegPhrases = new HashSet<CandidatePhrase>();
				foreach (CandidatePhrase p in allNegativePhrases)
				{
					if (i >= numpos)
					{
						break;
					}
					selectedNegPhrases.Add(p);
					i++;
				}
				allNegativePhrases.Clear();
				allNegativePhrases = selectedNegPhrases;
			}
			System.Console.Out.WriteLine("all negative phrases are " + allNegativePhrases);
			foreach (CandidatePhrase negative in allNegativePhrases)
			{
				ICounter<ConstantsAndVariables.ScorePhraseMeasures> feat;
				//CandidatePhrase candidate = new CandidatePhrase(l.word());
				if (forLearningPattern)
				{
					feat = GetPhraseFeaturesForPattern(answerLabel, negative);
				}
				else
				{
					feat = GetFeatures(answerLabel, negative, wordsPatExtracted.GetCounter(negative), allSelectedPatterns);
				}
				RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures> datum = new RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures>(feat, "false");
				dataset.Add(datum);
				if (logFile != null && wordVectors != null && wordVectors.Contains(negative.GetPhrase()))
				{
					logFile.Write(negative.GetPhrase() + "-N" + " " + ArrayUtils.ToString(wordVectors[negative.GetPhrase()], " ") + "\n");
				}
				if (logFileFeat != null)
				{
					logFileFeat.Write("NEGATIVE " + negative.GetPhrase() + "\t" + Counters.ToSortedByKeysString(feat, "%1$s:%2$.0f", ";", "%s") + "\n");
				}
			}
			if (logFile != null)
			{
				logFile.Close();
			}
			if (logFileFeat != null)
			{
				logFileFeat.Close();
			}
			System.Console.Out.WriteLine("Before feature count threshold, dataset stats are ");
			dataset.SummaryStatistics();
			dataset.ApplyFeatureCountThreshold(constVars.featureCountThreshold);
			System.Console.Out.WriteLine("AFTER feature count threshold of " + constVars.featureCountThreshold + ", dataset stats are ");
			dataset.SummaryStatistics();
			Redwood.Log(Redwood.Dbg, "Eventually, number of positive datums:  " + numpos + " and number of negative datums: " + allNegativePhrases.Count);
			return dataset;
		}

		//Map of label to an array of values -- num_items, avg similarity, max similarity
		private static IDictionary<string, double[]> GetSimilarities(string phrase)
		{
			return similaritiesWithLabeledPhrases[phrase];
		}

		internal virtual ICounter<ConstantsAndVariables.ScorePhraseMeasures> GetPhraseFeaturesForPattern(string label, CandidatePhrase word)
		{
			if (phraseScoresRaw.ContainsFirstKey(word))
			{
				return phraseScoresRaw.GetCounter(word);
			}
			ICounter<ConstantsAndVariables.ScorePhraseMeasures> scoreslist = new ClassicCounter<ConstantsAndVariables.ScorePhraseMeasures>();
			//Add features on the word, if any!
			if (word.GetFeatures() != null)
			{
				scoreslist.AddAll(Counters.Transform(word.GetFeatures(), null));
			}
			else
			{
				Redwood.Log(ConstantsAndVariables.extremedebug, "features are null for " + word);
			}
			if (constVars.usePatternEvalSemanticOdds)
			{
				double dscore = this.GetDictOddsScore(word, label, 0);
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Semanticodds, dscore);
			}
			if (constVars.usePatternEvalGoogleNgram)
			{
				double gscore = GetGoogleNgramScore(word);
				if (gscore.IsInfinite() || double.IsNaN(gscore))
				{
					throw new Exception("how is the google ngrams score " + gscore + " for " + word);
				}
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Googlengram, gscore);
			}
			if (constVars.usePatternEvalDomainNgram)
			{
				double gscore = GetDomainNgramScore(word.GetPhrase());
				if (gscore.IsInfinite() || double.IsNaN(gscore))
				{
					throw new Exception("how is the domain ngrams score " + gscore + " for " + word + " when domain raw freq is " + Data.domainNGramRawFreq.GetCount(word) + " and raw freq is " + Data.rawFreq.GetCount(word));
				}
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Domainngram, gscore);
			}
			if (constVars.usePatternEvalWordClass)
			{
				int wordclass = constVars.GetWordClassClusters()[word.GetPhrase()];
				if (wordclass == null)
				{
					wordclass = constVars.GetWordClassClusters()[word.GetPhrase().ToLower()];
				}
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Create(ConstantsAndVariables.ScorePhraseMeasures.Distsim.ToString() + "-" + wordclass), 1.0);
			}
			if (constVars.usePatternEvalEditDistSame)
			{
				double ed = constVars.GetEditDistanceScoresThisClass(label, word.GetPhrase());
				System.Diagnostics.Debug.Assert(ed <= 1, " how come edit distance from the true class is " + ed + " for word " + word);
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Editdistsame, ed);
			}
			if (constVars.usePatternEvalEditDistOther)
			{
				double ed = constVars.GetEditDistanceScoresOtherClass(label, word.GetPhrase());
				System.Diagnostics.Debug.Assert(ed <= 1, " how come edit distance from the true class is " + ed + " for word " + word);
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Editdistother, ed);
			}
			if (constVars.usePatternEvalWordShape)
			{
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Wordshape, this.GetWordShapeScore(word.GetPhrase(), label));
			}
			if (constVars.usePatternEvalWordShapeStr)
			{
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Create(ConstantsAndVariables.ScorePhraseMeasures.Wordshapestr + "-" + this.WordShape(word.GetPhrase())), 1.0);
			}
			if (constVars.usePatternEvalFirstCapital)
			{
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Isfirstcapital, StringUtils.IsCapitalized(word.GetPhrase()) ? 1.0 : 0);
			}
			if (constVars.usePatternEvalBOW)
			{
				foreach (string s in word.GetPhrase().Split("\\s+"))
				{
					scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Create(ConstantsAndVariables.ScorePhraseMeasures.Bow + "-" + s), 1.0);
				}
			}
			phraseScoresRaw.SetCounter(word, scoreslist);
			//System.out.println("scores for " + word + " are " + scoreslist);
			return scoreslist;
		}

		/*
		Counter<ScorePhraseMeasures> getPhraseFeaturesForPattern(String label, CandidatePhrase word) {
		
		if (phraseScoresRaw.containsFirstKey(word))
		return phraseScoresRaw.getCounter(word);
		
		Counter<ScorePhraseMeasures> scoreslist = new ClassicCounter<ScorePhraseMeasures>();
		
		if (constVars.usePatternEvalSemanticOdds) {
		assert constVars.dictOddsWeights != null : "usePatternEvalSemanticOdds is true but dictOddsWeights is null for the label " + label;
		double dscore = this.getDictOddsScore(word, label, 0);
		dscore = logistic(dscore);
		scoreslist.setCount(ScorePhraseMeasures.SEMANTICODDS, dscore);
		}
		
		if (constVars.usePatternEvalGoogleNgram) {
		Double gscore = getGoogleNgramScore(word);
		if (gscore.isInfinite() || gscore.isNaN()) {
		throw new RuntimeException("how is the google ngrams score " + gscore + " for " + word);
		}
		gscore = logistic(gscore);
		scoreslist.setCount(ScorePhraseMeasures.GOOGLENGRAM, gscore);
		}
		
		if (constVars.usePatternEvalDomainNgram) {
		Double gscore = getDomainNgramScore(word.getPhrase());
		if (gscore.isInfinite() || gscore.isNaN()) {
		throw new RuntimeException("how is the domain ngrams score " + gscore + " for " + word + " when domain raw freq is " + Data.domainNGramRawFreq.getCount(word)
		+ " and raw freq is " + Data.rawFreq.getCount(word));
		
		}
		gscore = logistic(gscore);
		scoreslist.setCount(ScorePhraseMeasures.DOMAINNGRAM, gscore);
		}
		
		if (constVars.usePatternEvalWordClass) {
		double distSimWt = getDistSimWtScore(word.getPhrase(), label);
		distSimWt = logistic(distSimWt);
		scoreslist.setCount(ScorePhraseMeasures.DISTSIM, distSimWt);
		}
		
		if (constVars.usePatternEvalEditDistSame) {
		scoreslist.setCount(ScorePhraseMeasures.EDITDISTSAME, constVars.getEditDistanceScoresThisClass(label, word.getPhrase()));
		}
		if (constVars.usePatternEvalEditDistOther)
		scoreslist.setCount(ScorePhraseMeasures.EDITDISTOTHER, constVars.getEditDistanceScoresOtherClass(label, word.getPhrase()));
		
		if(constVars.usePatternEvalWordShape){
		scoreslist.setCount(ScorePhraseMeasures.WORDSHAPE, this.getWordShapeScore(word.getPhrase(), label));
		}
		
		if(constVars.usePatternEvalWordShapeStr){
		scoreslist.setCount(ScorePhraseMeasures.create(ScorePhraseMeasures.WORDSHAPE +"-"+ this.wordShape(word.getPhrase())), 1.0);
		}
		
		if(constVars.usePatternEvalFirstCapital){
		scoreslist.setCount(ScorePhraseMeasures.ISFIRSTCAPITAL, StringUtils.isCapitalized(word.getPhrase())?1.0:0.0);
		}
		
		if(constVars.usePatternEvalBOW){
		for(String s: word.getPhrase().split("\\s+"))
		scoreslist.setCount(ScorePhraseMeasures.create(ScorePhraseMeasures.BOW +"-"+ s.toLowerCase()), 1.0);
		}
		
		phraseScoresRaw.setCounter(word, scoreslist);
		return scoreslist;
		}
		*/
		public virtual double ScoreUsingClassifer(IClassifier classifier, CandidatePhrase word, string label, bool forLearningPatterns, ICounter<E> patternsThatExtractedPat, ICounter<E> allSelectedPatterns)
		{
			if (learnedScores.ContainsKey(word))
			{
				return learnedScores.GetCount(word);
			}
			double score;
			if (scoreClassifierType.Equals(ScorePhrasesLearnFeatWt.ClassifierType.Dt))
			{
				ICounter<ConstantsAndVariables.ScorePhraseMeasures> feat = null;
				if (forLearningPatterns)
				{
					feat = GetPhraseFeaturesForPattern(label, word);
				}
				else
				{
					feat = this.GetFeatures(label, word, patternsThatExtractedPat, allSelectedPatterns);
				}
				RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures> d = new RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures>(feat, false.ToString());
				ICounter<string> sc = classifier.ScoresOf(d);
				score = sc.GetCount(true.ToString());
			}
			else
			{
				if (scoreClassifierType.Equals(ScorePhrasesLearnFeatWt.ClassifierType.Lr))
				{
					LogisticClassifier logcl = ((LogisticClassifier)classifier);
					string l = (string)logcl.GetLabelForInternalPositiveClass();
					ICounter<ConstantsAndVariables.ScorePhraseMeasures> feat;
					if (forLearningPatterns)
					{
						feat = GetPhraseFeaturesForPattern(label, word);
					}
					else
					{
						feat = this.GetFeatures(label, word, patternsThatExtractedPat, allSelectedPatterns);
					}
					RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures> d = new RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures>(feat, true.ToString());
					score = logcl.ProbabilityOf(d);
				}
				else
				{
					if (scoreClassifierType.Equals(ScorePhrasesLearnFeatWt.ClassifierType.Shiftlr))
					{
						//convert to basicdatum -- restriction of ShiftLR right now
						ICounter<ConstantsAndVariables.ScorePhraseMeasures> feat;
						if (forLearningPatterns)
						{
							feat = GetPhraseFeaturesForPattern(label, word);
						}
						else
						{
							feat = this.GetFeatures(label, word, patternsThatExtractedPat, allSelectedPatterns);
						}
						BasicDatum<string, ConstantsAndVariables.ScorePhraseMeasures> d = new BasicDatum<string, ConstantsAndVariables.ScorePhraseMeasures>(feat.KeySet(), false.ToString());
						ICounter<string> sc = ((MultinomialLogisticClassifier)classifier).ProbabilityOf(d);
						score = sc.GetCount(true.ToString());
					}
					else
					{
						if (scoreClassifierType.Equals(ScorePhrasesLearnFeatWt.ClassifierType.Svm) || scoreClassifierType.Equals(ScorePhrasesLearnFeatWt.ClassifierType.Rf) || scoreClassifierType.Equals(ScorePhrasesLearnFeatWt.ClassifierType.Linear))
						{
							ICounter<ConstantsAndVariables.ScorePhraseMeasures> feat = null;
							if (forLearningPatterns)
							{
								feat = GetPhraseFeaturesForPattern(label, word);
							}
							else
							{
								feat = this.GetFeatures(label, word, patternsThatExtractedPat, allSelectedPatterns);
							}
							RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures> d = new RVFDatum<string, ConstantsAndVariables.ScorePhraseMeasures>(feat, false.ToString());
							ICounter<string> sc = classifier.ScoresOf(d);
							score = sc.GetCount(true.ToString());
						}
						else
						{
							throw new Exception("cannot identify classifier " + scoreClassifierType);
						}
					}
				}
			}
			this.learnedScores.SetCount(word, score);
			return score;
		}

		internal virtual ICounter<ConstantsAndVariables.ScorePhraseMeasures> GetFeatures(string label, CandidatePhrase word, ICounter<E> patThatExtractedWord, ICounter<E> allSelectedPatterns)
		{
			if (phraseScoresRaw.ContainsFirstKey(word))
			{
				return phraseScoresRaw.GetCounter(word);
			}
			ICounter<ConstantsAndVariables.ScorePhraseMeasures> scoreslist = new ClassicCounter<ConstantsAndVariables.ScorePhraseMeasures>();
			//Add features on the word, if any!
			if (word.GetFeatures() != null)
			{
				scoreslist.AddAll(Counters.Transform(word.GetFeatures(), null));
			}
			else
			{
				Redwood.Log(ConstantsAndVariables.extremedebug, "features are null for " + word);
			}
			if (constVars.usePhraseEvalPatWtByFreq)
			{
				double tfscore = GetPatTFIDFScore(word, patThatExtractedWord, allSelectedPatterns);
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Patwtbyfreq, tfscore);
			}
			if (constVars.usePhraseEvalSemanticOdds)
			{
				double dscore = this.GetDictOddsScore(word, label, 0);
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Semanticodds, dscore);
			}
			if (constVars.usePhraseEvalGoogleNgram)
			{
				double gscore = GetGoogleNgramScore(word);
				if (gscore.IsInfinite() || double.IsNaN(gscore))
				{
					throw new Exception("how is the google ngrams score " + gscore + " for " + word);
				}
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Googlengram, gscore);
			}
			if (constVars.usePhraseEvalDomainNgram)
			{
				double gscore = GetDomainNgramScore(word.GetPhrase());
				if (gscore.IsInfinite() || double.IsNaN(gscore))
				{
					throw new Exception("how is the domain ngrams score " + gscore + " for " + word + " when domain raw freq is " + Data.domainNGramRawFreq.GetCount(word) + " and raw freq is " + Data.rawFreq.GetCount(word));
				}
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Domainngram, gscore);
			}
			if (constVars.usePhraseEvalWordClass)
			{
				int wordclass = constVars.GetWordClassClusters()[word.GetPhrase()];
				if (wordclass == null)
				{
					wordclass = constVars.GetWordClassClusters()[word.GetPhrase().ToLower()];
				}
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Create(ConstantsAndVariables.ScorePhraseMeasures.Distsim.ToString() + "-" + wordclass), 1.0);
			}
			if (constVars.usePhraseEvalWordVector)
			{
				IDictionary<string, double[]> sims = GetSimilarities(word.GetPhrase());
				if (sims == null)
				{
					//TODO: make more efficient
					IDictionary<string, ICollection<CandidatePhrase>> allPossibleNegativePhrases = GetAllPossibleNegativePhrases(label);
					ICollection<CandidatePhrase> knownPositivePhrases = CollectionUtils.UnionAsSet(constVars.GetLearnedWords(label).KeySet(), constVars.GetSeedLabelDictionary()[label]);
					ComputeSimWithWordVectors(Arrays.AsList(word), knownPositivePhrases, allPossibleNegativePhrases, label);
					sims = GetSimilarities(word.GetPhrase());
				}
				System.Diagnostics.Debug.Assert(sims != null, " Why are there no similarities for " + word);
				double avgPosSim = sims[label][(int)(PhraseScorer.Similarities.Avgsim)];
				double maxPosSim = sims[label][(int)(PhraseScorer.Similarities.Maxsim)];
				double sumNeg = 0;
				double maxNeg = double.MinValue;
				double allNumItems = 0;
				foreach (KeyValuePair<string, double[]> simEn in sims)
				{
					if (simEn.Key.Equals(label))
					{
						continue;
					}
					double numItems = simEn.Value[(int)(PhraseScorer.Similarities.Numitems)];
					sumNeg += simEn.Value[(int)(PhraseScorer.Similarities.Avgsim)] * numItems;
					allNumItems += numItems;
					double maxNegLabel = simEn.Value[(int)(PhraseScorer.Similarities.Maxsim)];
					if (maxNeg < maxNegLabel)
					{
						maxNeg = maxNegLabel;
					}
				}
				double avgNegSim = sumNeg / allNumItems;
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Wordvecpossimavg, avgPosSim);
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Wordvecpossimmax, maxPosSim);
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Wordvecnegsimavg, avgNegSim);
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Wordvecnegsimavg, maxNeg);
			}
			if (constVars.usePhraseEvalEditDistSame)
			{
				double ed = constVars.GetEditDistanceScoresThisClass(label, word.GetPhrase());
				System.Diagnostics.Debug.Assert(ed <= 1, " how come edit distance from the true class is " + ed + " for word " + word);
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Editdistsame, ed);
			}
			if (constVars.usePhraseEvalEditDistOther)
			{
				double ed = constVars.GetEditDistanceScoresOtherClass(label, word.GetPhrase());
				System.Diagnostics.Debug.Assert(ed <= 1, " how come edit distance from the true class is " + ed + " for word " + word);
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Editdistother, ed);
			}
			if (constVars.usePhraseEvalWordShape)
			{
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Wordshape, this.GetWordShapeScore(word.GetPhrase(), label));
			}
			if (constVars.usePhraseEvalWordShapeStr)
			{
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Create(ConstantsAndVariables.ScorePhraseMeasures.Wordshapestr + "-" + this.WordShape(word.GetPhrase())), 1.0);
			}
			if (constVars.usePhraseEvalFirstCapital)
			{
				scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Isfirstcapital, StringUtils.IsCapitalized(word.GetPhrase()) ? 1.0 : 0);
			}
			if (constVars.usePhraseEvalBOW)
			{
				foreach (string s in word.GetPhrase().Split("\\s+"))
				{
					scoreslist.SetCount(ConstantsAndVariables.ScorePhraseMeasures.Create(ConstantsAndVariables.ScorePhraseMeasures.Bow + "-" + s), 1.0);
				}
			}
			phraseScoresRaw.SetCounter(word, scoreslist);
			//System.out.println("scores for " + word + " are " + scoreslist);
			return scoreslist;
		}
	}
}
