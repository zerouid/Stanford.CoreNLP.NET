// CMMClassifier -- a conditional maximum-entropy markov model, mainly used for NER.
// Copyright (c) 2002-2014 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation,
// Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.IE.Ner
{
	/// <summary>Does Sequence Classification using a Conditional Markov Model.</summary>
	/// <remarks>
	/// Does Sequence Classification using a Conditional Markov Model.
	/// It could be used for other purposes, but the provided features
	/// are aimed at doing Named Entity Recognition.
	/// The code has functionality for different document encodings, but when
	/// using the standard
	/// <c>ColumnDocumentReader</c>
	/// ,
	/// input files are expected to
	/// be one word per line with the columns indicating things like the word,
	/// POS, chunk, and class.
	/// <b>Typical usage</b>
	/// For running a trained model with a provided serialized classifier:
	/// <c>
	/// java -server -mx1000m edu.stanford.nlp.ie.ner.CMMClassifier -loadClassifier
	/// conll.ner.gz -textFile samplesentences.txt
	/// </c>
	/// When specifying all parameters in a properties file (train, test, or
	/// runtime):
	/// <c>java -mx1000m edu.stanford.nlp.ie.ner.CMMClassifier -prop propFile</c>
	/// To train and test a model from the command line:
	/// <c>
	/// java -mx1000m edu.stanford.nlp.ie.ner.CMMClassifier
	/// -trainFile trainFile -testFile testFile -goodCoNLL &gt; output
	/// </c>
	/// Features are defined by a
	/// <see cref="Edu.Stanford.Nlp.Sequences.FeatureFactory{IN}"/>
	/// ; the
	/// <see cref="Edu.Stanford.Nlp.Sequences.FeatureFactory{IN}"/>
	/// which is used by default is
	/// <see cref="Edu.Stanford.Nlp.IE.NERFeatureFactory{IN}"/>
	/// , and you should look there for feature templates.
	/// Features are specified either by a Properties file (which is the
	/// recommended method) or on the command line.  The features are read into
	/// a
	/// <see cref="Edu.Stanford.Nlp.Sequences.SeqClassifierFlags"/>
	/// object, which the
	/// user need not know much about, unless one wishes to add new features.
	/// CMMClassifier may also be used programmatically.  When creating a new instance, you
	/// <i>must</i> specify a properties file.  The other way to get a CMMClassifier is to
	/// deserialize one via
	/// <see cref="CMMClassifier{IN}.GetClassifier(string)"/>
	/// , which returns a
	/// deserialized classifier.  You may then tag sentences using either the assorted
	/// <c>test</c>
	/// or
	/// <c>testSentence</c>
	/// methods.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Jenny Finkel</author>
	/// <author>Christopher Manning</author>
	/// <author>Shipra Dingare</author>
	/// <author>Huy Nguyen</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) - cleanup and filling in types</author>
	public class CMMClassifier<In> : AbstractSequenceClassifier<In>
		where In : CoreLabel
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Ner.CMMClassifier));

		private IProbabilisticClassifier<string, string> classifier;

		/// <summary>
		/// The set of empirically legal label sequences (of length (order) at most
		/// <c>flags.maxLeft</c>
		/// ).  Used to filter valid class sequences if
		/// <c>useObservedSequencesOnly</c>
		/// is set.
		/// </summary>
		internal ICollection<IList<string>> answerArrays;

		/// <summary>Default place to look in Jar file for classifier.</summary>
		public const string DefaultClassifier = "edu/stanford/nlp/models/ner/ner-eng-ie.cmm-3-all2006.ser.gz";

		protected internal CMMClassifier()
			: base(new SeqClassifierFlags())
		{
		}

		public CMMClassifier(Properties props)
			: base(props)
		{
		}

		public CMMClassifier(SeqClassifierFlags flags)
			: base(flags)
		{
		}

		/// <summary>Returns the Set of entities recognized by this Classifier.</summary>
		/// <returns>The Set of entities recognized by this Classifier.</returns>
		public virtual ICollection<string> GetTags()
		{
			ICollection<string> tags = Generics.NewHashSet(classIndex.ObjectsList());
			tags.Remove(flags.backgroundSymbol);
			return tags;
		}

		/// <summary>
		/// Classify a
		/// <see cref="System.Collections.IList{E}"/>
		/// of
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s.
		/// </summary>
		/// <param name="document">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s
		/// to be classified.
		/// </param>
		public override IList<In> Classify(IList<In> document)
		{
			if (flags.useSequences)
			{
				ClassifySeq(document);
			}
			else
			{
				ClassifyNoSeq(document);
			}
			return document;
		}

		/// <summary>
		/// Classify a List of
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s without using sequence information
		/// (i.e. no Viterbi algorithm, just distribution over next class).
		/// </summary>
		/// <param name="document">
		/// a List of
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s to be classified
		/// </param>
		private void ClassifyNoSeq(IList<In> document)
		{
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
			if (flags.lowerNewgeneThreshold)
			{
				// Used to raise recall for task 1B
				log.Info("Using NEWGENE threshold: " + flags.newgeneThreshold);
				for (int i = 0; i < docSize; i++)
				{
					CoreLabel wordInfo = document[i];
					IDatum<string, string> d = MakeDatum(document, i, featureFactories);
					ICounter<string> scores = classifier.ScoresOf(d);
					//String answer = BACKGROUND;
					string answer = flags.backgroundSymbol;
					// HN: The evaluation of scoresOf seems to result in some
					// kind of side effect.  Specifically, the symptom is that
					// if scoresOf is not evaluated at every position, the
					// answers are different
					if ("NEWGENE".Equals(wordInfo.Get(typeof(CoreAnnotations.GazAnnotation))))
					{
						foreach (string label in scores.KeySet())
						{
							if ("G".Equals(label))
							{
								log.Info(wordInfo.Word() + ':' + scores.GetCount(label));
								if (scores.GetCount(label) > flags.newgeneThreshold)
								{
									answer = label;
								}
							}
						}
					}
					wordInfo.Set(typeof(CoreAnnotations.AnswerAnnotation), answer);
				}
			}
			else
			{
				for (int i = 0; i < listSize; i++)
				{
					string answer = ClassOf(document, i);
					CoreLabel wordInfo = document[i];
					//log.info("XXX answer for " +
					//        wordInfo.word() + " is " + answer);
					wordInfo.Set(typeof(CoreAnnotations.AnswerAnnotation), answer);
				}
				if (flags.justify && (classifier is LinearClassifier))
				{
					LinearClassifier<string, string> lc = (LinearClassifier<string, string>)classifier;
					for (int i_1 = 0; i_1 < lsize; i_1++)
					{
						CoreLabel lineInfo = document[i_1];
						log.Info("@@ Position " + i_1 + ": ");
						log.Info(lineInfo.Word() + " chose " + lineInfo.Get(typeof(CoreAnnotations.AnswerAnnotation)));
						lc.JustificationOf(MakeDatum(document, i_1, featureFactories));
					}
				}
			}
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
		}

		/// <summary>Returns the most likely class for the word at the given position.</summary>
		protected internal virtual string ClassOf(IList<In> lineInfos, int pos)
		{
			IDatum<string, string> d = MakeDatum(lineInfos, pos, featureFactories);
			return classifier.ClassOf(d);
		}

		/// <summary>Returns the log conditional likelihood of the given dataset.</summary>
		/// <returns>The log conditional likelihood of the given dataset.</returns>
		public virtual double Loglikelihood(IList<In> lineInfos)
		{
			double cll = 0.0;
			for (int i = 0; i < lineInfos.Count; i++)
			{
				IDatum<string, string> d = MakeDatum(lineInfos, i, featureFactories);
				ICounter<string> c = classifier.LogProbabilityOf(d);
				double total = double.NegativeInfinity;
				foreach (string s in c.KeySet())
				{
					total = SloppyMath.LogAdd(total, c.GetCount(s));
				}
				cll -= c.GetCount(d.Label()) - total;
			}
			// quadratic prior
			// HN: TODO: add other priors
			if (classifier is LinearClassifier)
			{
				double sigmaSq = flags.sigma * flags.sigma;
				LinearClassifier<string, string> lc = (LinearClassifier<string, string>)classifier;
				foreach (string feature in lc.Features())
				{
					foreach (string classLabel in classIndex)
					{
						double w = lc.Weight(feature, classLabel);
						cll += w * w / 2.0 / sigmaSq;
					}
				}
			}
			return cll;
		}

		public override ISequenceModel GetSequenceModel(IList<In> document)
		{
			//log.info(flags.useReverse);
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
			// cdm Aug 2005: why is this next line needed?  Seems really ugly!!!  [2006: it broke things! removed]
			// document.add(0, new CoreLabel());
			ISequenceModel ts = new CMMClassifier.Scorer<In>(document, classIndex, this, (!flags.useTaggySequences ? (flags.usePrevSequences ? 1 : 0) : flags.maxLeft), (flags.useNextSequences ? 1 : 0), answerArrays);
			return ts;
		}

		/// <summary>
		/// Classify a List of
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s using sequence information
		/// (i.e. Viterbi or Beam Search).
		/// </summary>
		/// <param name="document">
		/// A List of
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s to be classified
		/// </param>
		private void ClassifySeq(IList<In> document)
		{
			if (document.IsEmpty())
			{
				return;
			}
			ISequenceModel ts = GetSequenceModel(document);
			//    TagScorer ts = new PrevOnlyScorer(document, tagIndex, this, (!flags.useTaggySequences ? (flags.usePrevSequences ? 1 : 0) : flags.maxLeft), 0, answerArrays);
			int[] tags;
			//log.info("***begin test***");
			if (flags.useViterbi)
			{
				ExactBestSequenceFinder ti = new ExactBestSequenceFinder();
				tags = ti.BestSequence(ts);
			}
			else
			{
				BeamBestSequenceFinder ti = new BeamBestSequenceFinder(flags.beamSize, true, true);
				tags = ti.BestSequence(ts, document.Count);
			}
			//log.info("***end test***");
			// used to improve recall in task 1b
			if (flags.lowerNewgeneThreshold)
			{
				log.Info("Using NEWGENE threshold: " + flags.newgeneThreshold);
				int[] copy = new int[tags.Length];
				System.Array.Copy(tags, 0, copy, 0, tags.Length);
				// for each sequence marked as NEWGENE in the gazette
				// tag the entire sequence as NEWGENE and sum the score
				// if the score is greater than newgeneThreshold, accept
				int ngTag = classIndex.IndexOf("G");
				//int bgTag = classIndex.indexOf(BACKGROUND);
				int bgTag = classIndex.IndexOf(flags.backgroundSymbol);
				for (int i = 0; i < dSize; i++)
				{
					CoreLabel wordInfo = document[i];
					if ("NEWGENE".Equals(wordInfo.Get(typeof(CoreAnnotations.GazAnnotation))))
					{
						int start = i;
						int j;
						for (j = i; j < document.Count; j++)
						{
							wordInfo = document[j];
							if (!"NEWGENE".Equals(wordInfo.Get(typeof(CoreAnnotations.GazAnnotation))))
							{
								break;
							}
						}
						int end = j;
						//int end = i + 1;
						int winStart = System.Math.Max(0, start - 4);
						int winEnd = System.Math.Min(tags.Length, end + 4);
						// clear a window around the sequences
						for (j = winStart; j < winEnd; j++)
						{
							copy[j] = bgTag;
						}
						// score as nongene
						double bgScore = 0.0;
						for (j = start; j < end; j++)
						{
							double[] scores = ts.ScoresOf(copy, j);
							scores = CMMClassifier.Scorer.Recenter(scores);
							bgScore += scores[bgTag];
						}
						// first pass, compute all of the scores
						ClassicCounter<Pair<int, int>> prevScores = new ClassicCounter<Pair<int, int>>();
						for (j = start; j < end; j++)
						{
							// clear the sequence
							for (int k = start; k < end; k++)
							{
								copy[k] = bgTag;
							}
							// grow the sequence from j until the end
							for (int k_1 = j; k_1 < end; k_1++)
							{
								copy[k_1] = ngTag;
								// score the sequence
								double ngScore = 0.0;
								for (int m = start; m < end; m++)
								{
									double[] scores = ts.ScoresOf(copy, m);
									scores = CMMClassifier.Scorer.Recenter(scores);
									ngScore += scores[tags[m]];
								}
								prevScores.IncrementCount(new Pair<int, int>(int.Parse(j), int.Parse(k_1)), ngScore - bgScore);
							}
						}
						for (j = start; j < end; j++)
						{
							// grow the sequence from j until the end
							for (int k = j; k < end; k++)
							{
								double score = prevScores.GetCount(new Pair<int, int>(int.Parse(j), int.Parse(k)));
								Pair<int, int> al = new Pair<int, int>(int.Parse(j - 1), int.Parse(k));
								// adding a word to the left
								Pair<int, int> ar = new Pair<int, int>(int.Parse(j), int.Parse(k + 1));
								// adding a word to the right
								Pair<int, int> sl = new Pair<int, int>(int.Parse(j + 1), int.Parse(k));
								// subtracting word from left
								Pair<int, int> sr = new Pair<int, int>(int.Parse(j), int.Parse(k - 1));
								// subtracting word from right
								// make sure the score is greater than all its neighbors (one add or subtract)
								if (score >= flags.newgeneThreshold && (!prevScores.ContainsKey(al) || score > prevScores.GetCount(al)) && (!prevScores.ContainsKey(ar) || score > prevScores.GetCount(ar)) && (!prevScores.ContainsKey(sl) || score > prevScores.GetCount(sl)) &&
									 (!prevScores.ContainsKey(sr) || score > prevScores.GetCount(sr)))
								{
									StringBuilder sb = new StringBuilder();
									wordInfo = document[j];
									string docId = wordInfo.Get(typeof(CoreAnnotations.IDAnnotation));
									string startIndex = wordInfo.Get(typeof(CoreAnnotations.PositionAnnotation));
									wordInfo = document[k];
									string endIndex = wordInfo.Get(typeof(CoreAnnotations.PositionAnnotation));
									for (int m = j; m <= k; m++)
									{
										wordInfo = document[m];
										sb.Append(wordInfo.Word());
										sb.Append(' ');
									}
									/*log.info(sb.toString()+"score:"+score+
									" al:"+prevScores.getCount(al)+
									" ar:"+prevScores.getCount(ar)+
									"  sl:"+prevScores.getCount(sl)+" sr:"+ prevScores.getCount(sr));*/
									System.Console.Out.WriteLine(docId + '|' + startIndex + ' ' + endIndex + '|' + sb.ToString().Trim());
								}
							}
						}
						// restore the original tags
						for (j = winStart; j < winEnd; j++)
						{
							copy[j] = tags[j];
						}
						i = end;
					}
				}
			}
			for (int i_1 = 0; i_1 < docSize; i_1++)
			{
				CoreLabel lineInfo = document[i_1];
				string answer = classIndex.Get(tags[i_1]);
				lineInfo.Set(typeof(CoreAnnotations.AnswerAnnotation), answer);
			}
			if (flags.justify && classifier is LinearClassifier)
			{
				LinearClassifier<string, string> lc = (LinearClassifier<string, string>)classifier;
				if (flags.dump)
				{
					lc.Dump();
				}
				for (int i = 0; i_1 < docSize; i_1++)
				{
					CoreLabel lineInfo = document[i_1];
					log.Info("@@ Position is: " + i_1 + ": ");
					log.Info(lineInfo.Word() + ' ' + lineInfo.Get(typeof(CoreAnnotations.AnswerAnnotation)));
					lc.JustificationOf(MakeDatum(document, i_1, featureFactories));
				}
			}
			// document.remove(0);
			if (flags.useReverse)
			{
				Java.Util.Collections.Reverse(document);
			}
		}

		// end testSeq
		/// <param name="filename">adaptation file</param>
		/// <param name="trainDataset">original dataset (used in training)</param>
		public virtual void Adapt(string filename, Dataset<string, string> trainDataset, IDocumentReaderAndWriter<In> readerWriter)
		{
			// flags.ocrTrain = false;  // ?? Do we need this? (Pi-Chuan Sat Nov  5 15:42:49 2005)
			ObjectBank<IList<In>> docs = MakeObjectBankFromFile(filename, readerWriter);
			Adapt(docs, trainDataset);
		}

		/// <param name="featureLabels">adaptation docs</param>
		/// <param name="trainDataset">original dataset (used in training)</param>
		public virtual void Adapt(ObjectBank<IList<In>> featureLabels, Dataset<string, string> trainDataset)
		{
			Dataset<string, string> adapt = GetDataset(featureLabels, trainDataset);
			Adapt(adapt);
		}

		/// <param name="featureLabels">retrain docs</param>
		/// <param name="featureIndex">featureIndex of original dataset (used in training)</param>
		/// <param name="labelIndex">labelIndex of original dataset (used in training)</param>
		public virtual void Retrain(ObjectBank<IList<In>> featureLabels, IIndex<string> featureIndex, IIndex<string> labelIndex)
		{
			int fs = featureIndex.Size();
			// old dim
			int ls = labelIndex.Size();
			// old dim
			Dataset<string, string> adapt = GetDataset(featureLabels, featureIndex, labelIndex);
			int prior = (int)(LogPrior.LogPriorType.Quadratic);
			LinearClassifier<string, string> lc = (LinearClassifier<string, string>)classifier;
			LinearClassifierFactory<string, string> lcf = new LinearClassifierFactory<string, string>(flags.tolerance, flags.useSum, prior, flags.sigma, flags.epsilon, flags.QNsize);
			double[][] weights = lc.Weights();
			// old dim
			IIndex<string> newF = adapt.featureIndex;
			IIndex<string> newL = adapt.labelIndex;
			int newFS = newF.Size();
			int newLS = newL.Size();
			double[] x = new double[newFS * newLS];
			// new dim
			//log.info("old  ["+fs+"]"+"["+ls+"]");
			//log.info("new  ["+newFS+"]"+"["+newLS+"]");
			//log.info("new  ["+newFS*newLS+"]");
			for (int i = 0; i < fs; i++)
			{
				for (int j = 0; j < ls; j++)
				{
					string f = featureIndex.Get(i);
					string l = labelIndex.Get(j);
					int newi = newF.IndexOf(f) * newLS + newL.IndexOf(l);
					x[newi] = weights[i][j];
				}
			}
			//if (newi == 144745*2) {
			//log.info("What??"+i+"\t"+j);
			//}
			//log.info("x[144745*2]"+x[144745*2]);
			weights = lcf.TrainWeights(adapt, x);
			//log.info("x[144745*2]"+x[144745*2]);
			//log.info("weights[144745]"+"[0]="+weights[144745][0]);
			lc.SetWeights(weights);
		}

		/*
		int delme = 0;
		if (true) {
		for (double[] dd : weights) {
		delme++;
		for (double d : dd) {
		}
		}
		}
		log.info(weights[delme-1][0]);
		log.info("size of weights: "+delme);
		*/
		public virtual void Retrain(ObjectBank<IList<In>> doc)
		{
			if (classifier == null)
			{
				throw new NotSupportedException("Cannot retrain before you train!");
			}
			IIndex<string> findex = ((LinearClassifier<string, string>)classifier).FeatureIndex();
			IIndex<string> lindex = ((LinearClassifier<string, string>)classifier).LabelIndex();
			log.Info("Starting retrain:\t# of original features" + findex.Size() + ", # of original labels" + lindex.Size());
			Retrain(doc, findex, lindex);
		}

		public override void Train(ICollection<IList<In>> wordInfos, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			Dataset<string, string> train = GetDataset(wordInfos);
			//train.summaryStatistics();
			//train.printSVMLightFormat();
			// wordInfos = null;  // cdm: I think this does no good as ptr exists in caller (could empty the list or better refactor so conversion done earlier?)
			Train(train);
			for (int i = 0; i < flags.numTimesPruneFeatures; i++)
			{
				IIndex<string> featuresAboveThreshold = GetFeaturesAboveThreshold(train, flags.featureDiffThresh);
				log.Info("Removing features with weight below " + flags.featureDiffThresh + " and retraining...");
				train = GetDataset(train, featuresAboveThreshold);
				int tmp = flags.QNsize;
				flags.QNsize = flags.QNsize2;
				Train(train);
				flags.QNsize = tmp;
			}
			if (flags.doAdaptation && flags.adaptFile != null)
			{
				Adapt(flags.adaptFile, train, readerAndWriter);
			}
			log.Info("Built this classifier: ");
			if (classifier is LinearClassifier)
			{
				string classString = ((LinearClassifier<string, string>)classifier).ToString(flags.printClassifier, flags.printClassifierParam);
				log.Info(classString);
			}
			else
			{
				string classString = classifier.ToString();
				log.Info(classString);
			}
		}

		private IIndex<string> GetFeaturesAboveThreshold(Dataset<string, string> dataset, double thresh)
		{
			if (!(classifier is LinearClassifier))
			{
				throw new Exception("Attempting to remove features based on weight from a non-linear classifier");
			}
			IIndex<string> featureIndex = dataset.featureIndex;
			IIndex<string> labelIndex = dataset.labelIndex;
			IIndex<string> features = new HashIndex<string>();
			IEnumerator<string> featureIt = featureIndex.GetEnumerator();
			LinearClassifier<string, string> lc = (LinearClassifier<string, string>)classifier;
			while (featureIt.MoveNext())
			{
				string f = featureIt.Current;
				double smallest = double.PositiveInfinity;
				double biggest = double.NegativeInfinity;
				foreach (string l in labelIndex)
				{
					double weight = lc.Weight(f, l);
					if (weight < smallest)
					{
						smallest = weight;
					}
					if (weight > biggest)
					{
						biggest = weight;
					}
					if (biggest - smallest > thresh)
					{
						features.Add(f);
						goto LOOP_continue;
					}
				}
LOOP_continue: ;
			}
LOOP_break: ;
			return features;
		}

		/// <summary>Build a Dataset from some data.</summary>
		/// <remarks>Build a Dataset from some data. Used for training a classifier.</remarks>
		/// <param name="data">
		/// This variable is a list of lists of CoreLabel.  That is,
		/// it is a collection of documents, each of which is represented
		/// as a sequence of CoreLabel objects.
		/// </param>
		/// <returns>
		/// The Dataset which is an efficient encoding of the information
		/// in a List of Datums
		/// </returns>
		public virtual Dataset<string, string> GetDataset(ICollection<IList<In>> data)
		{
			return GetDataset(data, null, null);
		}

		/// <summary>Build a Dataset from some data.</summary>
		/// <remarks>
		/// Build a Dataset from some data. Used for training a classifier.
		/// By passing in extra featureIndex and classIndex, you can get a Dataset based on featureIndex and
		/// classIndex.
		/// </remarks>
		/// <param name="data">
		/// This variable is a list of lists of CoreLabel.  That is,
		/// it is a collection of documents, each of which is represented
		/// as a sequence of CoreLabel objects.
		/// </param>
		/// <param name="classIndex">
		/// if you want to get a Dataset based on featureIndex and
		/// classIndex in an existing origDataset
		/// </param>
		/// <returns>
		/// The Dataset which is an efficient encoding of the information
		/// in a List of Datums
		/// </returns>
		public virtual Dataset<string, string> GetDataset(ICollection<IList<In>> data, IIndex<string> featureIndex, IIndex<string> classIndex)
		{
			MakeAnswerArraysAndTagIndex(data);
			int size = 0;
			foreach (IList<In> doc in data)
			{
				size += doc.Count;
			}
			log.Info("Making Dataset ... ");
			System.Console.Error.Flush();
			Dataset<string, string> train;
			if (featureIndex != null && classIndex != null)
			{
				log.Info("  Using feature/class Index from existing Dataset...");
				log.Info("  (This is used when getting Dataset from adaptation set. We want to make the index consistent.)");
				//pichuan
				train = new Dataset<string, string>(size, featureIndex, classIndex);
			}
			else
			{
				train = new Dataset<string, string>(size);
			}
			foreach (IList<In> doc_1 in data)
			{
				if (flags.useReverse)
				{
					Java.Util.Collections.Reverse(doc_1);
				}
				for (int i = 0; i < dSize; i++)
				{
					IDatum<string, string> d = MakeDatum(doc_1, i, featureFactories);
					//CoreLabel fl = doc.get(i);
					train.Add(d);
				}
				if (flags.useReverse)
				{
					Java.Util.Collections.Reverse(doc_1);
				}
			}
			log.Info("done.");
			if (flags.featThreshFile != null)
			{
				log.Info("applying thresholds...");
				IList<Pair<Pattern, int>> thresh = GetThresholds(flags.featThreshFile);
				train.ApplyFeatureCountThreshold(thresh);
			}
			else
			{
				if (flags.featureThreshold > 1)
				{
					log.Info("Removing Features with counts < " + flags.featureThreshold);
					train.ApplyFeatureCountThreshold(flags.featureThreshold);
				}
			}
			train.SummaryStatistics();
			return train;
		}

		public virtual Dataset<string, string> GetBiasedDataset(ObjectBank<IList<In>> data, IIndex<string> featureIndex, IIndex<string> classIndex)
		{
			MakeAnswerArraysAndTagIndex(data);
			IIndex<string> origFeatIndex = new HashIndex<string>(featureIndex.ObjectsList());
			// mg2009: TODO: check
			int size = 0;
			foreach (IList<In> doc in data)
			{
				size += doc.Count;
			}
			log.Info("Making Dataset ... ");
			System.Console.Error.Flush();
			Dataset<string, string> train = new Dataset<string, string>(size, featureIndex, classIndex);
			foreach (IList<In> doc_1 in data)
			{
				if (flags.useReverse)
				{
					Java.Util.Collections.Reverse(doc_1);
				}
				for (int i = 0; i < dsize; i++)
				{
					IDatum<string, string> d = MakeDatum(doc_1, i, featureFactories);
					ICollection<string> newFeats = new List<string>();
					foreach (string f in d.AsFeatures())
					{
						if (!origFeatIndex.Contains(f))
						{
							newFeats.Add(f);
						}
					}
					//        log.info(d.label()+"\t"+d.asFeatures()+"\n\t"+newFeats);
					//        d = new BasicDatum(newFeats, d.label());
					train.Add(d);
				}
				if (flags.useReverse)
				{
					Java.Util.Collections.Reverse(doc_1);
				}
			}
			log.Info("done.");
			if (flags.featThreshFile != null)
			{
				log.Info("applying thresholds...");
				IList<Pair<Pattern, int>> thresh = GetThresholds(flags.featThreshFile);
				train.ApplyFeatureCountThreshold(thresh);
			}
			else
			{
				if (flags.featureThreshold > 1)
				{
					log.Info("Removing Features with counts < " + flags.featureThreshold);
					train.ApplyFeatureCountThreshold(flags.featureThreshold);
				}
			}
			train.SummaryStatistics();
			return train;
		}

		/// <summary>Build a Dataset from some data.</summary>
		/// <remarks>
		/// Build a Dataset from some data. Used for training a classifier.
		/// By passing in an extra origDataset, you can get a Dataset based on featureIndex and
		/// classIndex in an existing origDataset.
		/// </remarks>
		/// <param name="data">
		/// This variable is a list of lists of CoreLabel.  That is,
		/// it is a collection of documents, each of which is represented
		/// as a sequence of CoreLabel objects.
		/// </param>
		/// <param name="origDataset">
		/// if you want to get a Dataset based on featureIndex and
		/// classIndex in an existing origDataset
		/// </param>
		/// <returns>
		/// The Dataset which is an efficient encoding of the information
		/// in a List of Datums
		/// </returns>
		public virtual Dataset<string, string> GetDataset(ObjectBank<IList<In>> data, Dataset<string, string> origDataset)
		{
			if (origDataset == null)
			{
				return GetDataset(data);
			}
			return GetDataset(data, origDataset.featureIndex, origDataset.labelIndex);
		}

		/// <summary>Build a Dataset from some data.</summary>
		/// <param name="oldData">
		/// This
		/// <see cref="Edu.Stanford.Nlp.Classify.Dataset{L, F}"/>
		/// represents data for which we which to
		/// some features, specifically those features not in the
		/// <see cref="Edu.Stanford.Nlp.Util.IIndex{E}"/>
		/// goodFeatures.
		/// </param>
		/// <param name="goodFeatures">
		/// An
		/// <see cref="Edu.Stanford.Nlp.Util.IIndex{E}"/>
		/// of features we wish to retain.
		/// </param>
		/// <returns>
		/// A new
		/// <see cref="Edu.Stanford.Nlp.Classify.Dataset{L, F}"/>
		/// wheres each data point contains only features
		/// which were in goodFeatures.
		/// </returns>
		public virtual Dataset<string, string> GetDataset(Dataset<string, string> oldData, IIndex<string> goodFeatures)
		{
			//public Dataset getDataset(List data, Collection goodFeatures) {
			//makeAnswerArraysAndTagIndex(data);
			int[][] oldDataArray = oldData.GetDataArray();
			int[] oldLabelArray = oldData.GetLabelsArray();
			IIndex<string> oldFeatureIndex = oldData.featureIndex;
			int[] oldToNewFeatureMap = new int[oldFeatureIndex.Size()];
			int[][] newDataArray = new int[oldDataArray.Length][];
			log.Info("Building reduced dataset...");
			int size = oldFeatureIndex.Size();
			int max = 0;
			for (int i = 0; i < size; i++)
			{
				oldToNewFeatureMap[i] = goodFeatures.IndexOf(oldFeatureIndex.Get(i));
				if (oldToNewFeatureMap[i] > max)
				{
					max = oldToNewFeatureMap[i];
				}
			}
			for (int i_1 = 0; i_1 < oldDataArray.Length; i_1++)
			{
				int[] data = oldDataArray[i_1];
				size = 0;
				foreach (int oldF in data)
				{
					if (oldToNewFeatureMap[oldF] > 0)
					{
						size++;
					}
				}
				int[] newData = new int[size];
				int index = 0;
				foreach (int oldF_1 in data)
				{
					int f = oldToNewFeatureMap[oldF_1];
					if (f > 0)
					{
						newData[index++] = f;
					}
				}
				newDataArray[i_1] = newData;
			}
			Dataset<string, string> train = new Dataset<string, string>(oldData.labelIndex, oldLabelArray, goodFeatures, newDataArray, newDataArray.Length);
			log.Info("done.");
			if (flags.featThreshFile != null)
			{
				log.Info("applying thresholds...");
				IList<Pair<Pattern, int>> thresh = GetThresholds(flags.featThreshFile);
				train.ApplyFeatureCountThreshold(thresh);
			}
			else
			{
				if (flags.featureThreshold > 1)
				{
					log.Info("Removing Features with counts < " + flags.featureThreshold);
					train.ApplyFeatureCountThreshold(flags.featureThreshold);
				}
			}
			train.SummaryStatistics();
			return train;
		}

		private void Adapt(Dataset<string, string> adapt)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(flags.classifierType, "SVM"))
			{
				throw new NotSupportedException();
			}
			AdaptMaxEnt(adapt);
		}

		private void AdaptMaxEnt(Dataset<string, string> adapt)
		{
			if (classifier is LinearClassifier)
			{
				// So far the adaptation is only done on Gaussian Prior. Haven't checked how it'll work on other kinds of priors. -pichuan
				int prior = (int)(LogPrior.LogPriorType.Quadratic);
				if (flags.useHuber)
				{
					throw new NotSupportedException();
				}
				else
				{
					if (flags.useQuartic)
					{
						throw new NotSupportedException();
					}
				}
				LinearClassifierFactory<string, string> lcf = new LinearClassifierFactory<string, string>(flags.tolerance, flags.useSum, prior, flags.adaptSigma, flags.epsilon, flags.QNsize);
				((LinearClassifier<string, string>)classifier).AdaptWeights(adapt, lcf);
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		private void Train(Dataset<string, string> train)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(flags.classifierType, "SVM"))
			{
				TrainSVM(train);
			}
			else
			{
				TrainMaxEnt(train);
			}
		}

		private void TrainSVM(Dataset<string, string> train)
		{
			SVMLightClassifierFactory<string, string> fact = new SVMLightClassifierFactory<string, string>();
			classifier = fact.TrainClassifier(train);
		}

		private void TrainMaxEnt(Dataset<string, string> train)
		{
			int prior = (int)(LogPrior.LogPriorType.Quadratic);
			if (flags.useHuber)
			{
				prior = (int)(LogPrior.LogPriorType.Huber);
			}
			else
			{
				if (flags.useQuartic)
				{
					prior = (int)(LogPrior.LogPriorType.Quartic);
				}
			}
			LinearClassifier<string, string> lc;
			if (flags.useNB)
			{
				lc = new NBLinearClassifierFactory<string, string>(flags.sigma).TrainClassifier(train);
			}
			else
			{
				LinearClassifierFactory<string, string> lcf = new LinearClassifierFactory<string, string>(flags.tolerance, flags.useSum, prior, flags.sigma, flags.epsilon, flags.QNsize);
				lcf.SetVerbose(true);
				if (flags.useQN)
				{
					lcf.UseQuasiNewton(flags.useRobustQN);
				}
				else
				{
					if (flags.useStochasticQN)
					{
						lcf.UseStochasticQN(flags.initialGain, flags.stochasticBatchSize);
					}
					else
					{
						if (flags.useSMD)
						{
							lcf.UseStochasticMetaDescent(flags.initialGain, flags.stochasticBatchSize, flags.stochasticMethod, flags.SGDPasses);
						}
						else
						{
							if (flags.useSGD)
							{
								lcf.UseStochasticGradientDescent(flags.gainSGD, flags.stochasticBatchSize);
							}
							else
							{
								if (flags.useSGDtoQN)
								{
									lcf.UseStochasticGradientDescentToQuasiNewton(flags.initialGain, flags.stochasticBatchSize, flags.SGDPasses, flags.QNPasses, flags.SGD2QNhessSamples, flags.QNsize, flags.outputIterationsToFile);
								}
								else
								{
									if (flags.useHybrid)
									{
										lcf.UseHybridMinimizer(flags.initialGain, flags.stochasticBatchSize, flags.stochasticMethod, flags.hybridCutoffIteration);
									}
									else
									{
										lcf.UseConjugateGradientAscent();
									}
								}
							}
						}
					}
				}
				lc = lcf.TrainClassifier(train);
			}
			this.classifier = lc;
		}

		private void TrainSemiSup(Dataset<string, string> data, Dataset<string, string> biasedData, double[][] confusionMatrix)
		{
			int prior = (int)(LogPrior.LogPriorType.Quadratic);
			if (flags.useHuber)
			{
				prior = (int)(LogPrior.LogPriorType.Huber);
			}
			else
			{
				if (flags.useQuartic)
				{
					prior = (int)(LogPrior.LogPriorType.Quartic);
				}
			}
			LinearClassifierFactory<string, string> lcf;
			lcf = new LinearClassifierFactory<string, string>(flags.tolerance, flags.useSum, prior, flags.sigma, flags.epsilon, flags.QNsize);
			if (flags.useQN)
			{
				lcf.UseQuasiNewton();
			}
			else
			{
				lcf.UseConjugateGradientAscent();
			}
			this.classifier = (LinearClassifier<string, string>)lcf.TrainClassifierSemiSup(data, biasedData, confusionMatrix, null);
		}

		//   public void crossValidateTrainAndTest() throws Exception {
		//     crossValidateTrainAndTest(flags.trainFile);
		//   }
		//   public void crossValidateTrainAndTest(String filename) throws Exception {
		//     // wordshapes
		//     for (int fold = flags.startFold; fold <= flags.endFold; fold++) {
		//       log.info("fold " + fold + " of " + flags.endFold);
		//       // train
		//       List = makeObjectBank(filename);
		//       List folds = split(data, flags.numFolds);
		//       data = null;
		//       List train = new ArrayList();
		//       for (int i = 0; i < flags.numFolds; i++) {
		//         List docs = (List) folds.get(i);
		//         if (i != fold) {
		//           train.addAll(docs);
		//         }
		//       }
		//       folds = null;
		//       train(train);
		//       train = null;
		//       List test = new ArrayList();
		//       data = makeObjectBank(filename);
		//       folds = split(data, flags.numFolds);
		//       data = null;
		//       for (int i = 0; i < flags.numFolds; i++) {
		//         List docs = (List) folds.get(i);
		//         if (i == fold) {
		//           test.addAll(docs);
		//         }
		//       }
		//       folds = null;
		//       // test
		//       test(test);
		//       writeAnswers(test);
		//     }
		//   }
		//   /**
		//    * Splits the given train corpus into a train and a test corpus based on the fold number.
		//    * 1 / numFolds documents are held out for test, with the offset determined by the fold number.
		//    *
		//    * @param data     The original data
		//    * @param numFolds The number of folds to split the data into
		//    * @return A list of folds giving the new training set
		//    */
		//   private List split(List data, int numFolds) {
		//     List folds = new ArrayList();
		//     int foldSize = data.size() / numFolds;
		//     int r = data.size() - (numFolds * foldSize);
		//     int index = 0;
		//     for (int i = 0; i < numFolds; i++) {
		//       List fold = new ArrayList();
		//       int end = (i < r ? foldSize + 1 : foldSize);
		//       for (int j = 0; j < end; j++) {
		//         fold.add(data.get(index++));
		//       }
		//       folds.add(fold);
		//     }
		//     return folds;
		//   }
		public override void SerializeClassifier(string serializePath)
		{
			log.Info("Serializing classifier to " + serializePath + "...");
			try
			{
				ObjectOutputStream oos = IOUtils.WriteStreamFromString(serializePath);
				oos.WriteObject(classifier);
				oos.WriteObject(flags);
				oos.WriteObject(featureFactories);
				oos.WriteObject(classIndex);
				oos.WriteObject(answerArrays);
				//oos.writeObject(WordShapeClassifier.getKnownLowerCaseWords());
				oos.WriteObject(knownLCWords);
				oos.Close();
				log.Info("Done.");
			}
			catch (Exception e)
			{
				log.Info("Error serializing to " + serializePath);
				log.Err(e);
			}
		}

		public override void SerializeClassifier(ObjectOutputStream oos)
		{
			//log.info("Serializing classifier to " + serializePath + "...");
			try
			{
				//ObjectOutputStream oos = IOUtils.writeStreamFromString(oos);
				oos.WriteObject(classifier);
				oos.WriteObject(flags);
				oos.WriteObject(featureFactories);
				oos.WriteObject(classIndex);
				oos.WriteObject(answerArrays);
				//oos.writeObject(WordShapeClassifier.getKnownLowerCaseWords());
				oos.WriteObject(knownLCWords);
				oos.Close();
				log.Info("Done.");
			}
			catch (Exception e)
			{
				//log.info("Error serializing to " + serializePath);
				log.Err(e);
			}
		}

		/// <summary>Used to load the default supplied classifier.</summary>
		/// <remarks>
		/// Used to load the default supplied classifier.  **THIS FUNCTION
		/// WILL ONLY WORK IF RUN INSIDE A JAR FILE
		/// </remarks>
		public virtual void LoadDefaultClassifier()
		{
			LoadClassifierNoExceptions(DefaultClassifier, null);
		}

		/// <summary>
		/// Used to obtain the default classifier which is
		/// stored inside a jar file.
		/// </summary>
		/// <remarks>
		/// Used to obtain the default classifier which is
		/// stored inside a jar file.  <i>THIS FUNCTION
		/// WILL ONLY WORK IF RUN INSIDE A JAR FILE.</i>
		/// </remarks>
		/// <returns>A Default CMMClassifier from a jar file</returns>
		public static Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> GetDefaultClassifier()
		{
			Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> cmm = new Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel>();
			cmm.LoadDefaultClassifier();
			return cmm;
		}

		/// <summary>Load a classifier from the given Stream.</summary>
		/// <remarks>
		/// Load a classifier from the given Stream.
		/// <i>Implementation note: </i> This method <i>does not</i> close the
		/// Stream that it reads from.
		/// </remarks>
		/// <param name="ois">The ObjectInputStream to load the serialized classifier from</param>
		/// <exception cref="System.IO.IOException">If there are problems accessing the input stream</exception>
		/// <exception cref="System.InvalidCastException">If there are problems interpreting the serialized data</exception>
		/// <exception cref="System.TypeLoadException">If there are problems interpreting the serialized data</exception>
		public override void LoadClassifier(ObjectInputStream ois, Properties props)
		{
			classifier = (LinearClassifier<string, string>)ois.ReadObject();
			flags = (SeqClassifierFlags)ois.ReadObject();
			object featureFactory = ois.ReadObject();
			if (featureFactory is IList)
			{
				featureFactories = ErasureUtils.UncheckedCast(featureFactory);
			}
			else
			{
				if (featureFactory is FeatureFactory)
				{
					featureFactories = Generics.NewArrayList();
					featureFactories.Add((FeatureFactory)featureFactory);
				}
			}
			if (props != null)
			{
				flags.SetProperties(props);
			}
			Reinit();
			classIndex = (IIndex<string>)ois.ReadObject();
			answerArrays = (ICollection<IList<string>>)ois.ReadObject();
			knownLCWords = (MaxSizeConcurrentHashSet<string>)ois.ReadObject();
		}

		public static Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> GetClassifierNoExceptions(File file)
		{
			Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> cmm = new Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel>();
			cmm.LoadClassifierNoExceptions(file);
			return cmm;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> GetClassifier(File file)
		{
			Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> cmm = new Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel>();
			cmm.LoadClassifier(file);
			return cmm;
		}

		public static Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> GetClassifierNoExceptions(string loadPath)
		{
			Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> cmm = new Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel>();
			cmm.LoadClassifierNoExceptions(loadPath);
			return cmm;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> GetClassifier(string loadPath)
		{
			Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> cmm = new Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel>();
			cmm.LoadClassifier(loadPath);
			return cmm;
		}

		public static Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> GetClassifierNoExceptions(InputStream @in)
		{
			Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> cmm = new Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel>();
			cmm.LoadClassifierNoExceptions(new BufferedInputStream(@in), null);
			return cmm;
		}

		// new method for getting a CMM from an ObjectInputStream - by JB
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> GetClassifier<Inn>(ObjectInputStream ois)
			where Inn : ICoreMap
		{
			Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> cmm = new Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel>();
			cmm.LoadClassifier(ois, null);
			return cmm;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> GetClassifier<Inn>(ObjectInputStream ois, Properties props)
			where Inn : ICoreMap
		{
			Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> cmm = new Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel>();
			cmm.LoadClassifier(ois, props);
			return cmm;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> GetClassifier(InputStream @in)
		{
			Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel> cmm = new Edu.Stanford.Nlp.IE.Ner.CMMClassifier<CoreLabel>();
			cmm.LoadClassifier(new BufferedInputStream(@in));
			return cmm;
		}

		/// <summary>
		/// This routine builds the
		/// <c>answerArrays</c>
		/// which give the
		/// empirically legal label sequences (of length (order) at most
		/// <c>flags.maxLeft</c>
		/// ) and the
		/// <c>classIndex</c>
		/// ,
		/// which indexes known answer classes.
		/// </summary>
		/// <param name="docs">The training data: A List of List of CoreLabel</param>
		private void MakeAnswerArraysAndTagIndex(ICollection<IList<In>> docs)
		{
			if (answerArrays == null)
			{
				answerArrays = Generics.NewHashSet();
			}
			if (classIndex == null)
			{
				classIndex = new HashIndex<string>();
			}
			foreach (IList<In> doc in docs)
			{
				if (flags.useReverse)
				{
					Java.Util.Collections.Reverse(doc);
				}
				int leng = doc.Count;
				for (int start = 0; start < leng; start++)
				{
					for (int diff = 1; diff <= flags.maxLeft && start + diff <= leng; diff++)
					{
						string[] seq = new string[diff];
						for (int i = start; i < start + diff; i++)
						{
							seq[i - start] = doc[i].Get(typeof(CoreAnnotations.AnswerAnnotation));
						}
						answerArrays.Add(Arrays.AsList(seq));
					}
				}
				foreach (IN wordInfo in doc)
				{
					classIndex.Add(wordInfo.Get(typeof(CoreAnnotations.AnswerAnnotation)));
				}
				if (flags.useReverse)
				{
					Java.Util.Collections.Reverse(doc);
				}
			}
		}

		/// <summary>Make an individual Datum out of the data list info, focused at position loc.</summary>
		/// <param name="info">A List of IN objects</param>
		/// <param name="loc">The position in the info list to focus feature creation on</param>
		/// <param name="featureFactories">The factory that constructs features out of the item</param>
		/// <returns>A Datum (BasicDatum) representing this data instance</returns>
		public virtual IDatum<string, string> MakeDatum(IList<In> info, int loc, IList<FeatureFactory<In>> featureFactories)
		{
			PaddedList<In> pInfo = new PaddedList<In>(info, pad);
			ICollection<string> features = new List<string>();
			foreach (FeatureFactory<In> featureFactory in featureFactories)
			{
				IList<Clique> cliques = featureFactory.GetCliques();
				foreach (Clique c in cliques)
				{
					ICollection<string> feats = featureFactory.GetCliqueFeatures(pInfo, loc, c);
					feats = AddOtherClasses(feats, pInfo, loc, c);
					Sharpen.Collections.AddAll(features, feats);
				}
			}
			PrintFeatures(pInfo[loc], features);
			CoreLabel c_1 = info[loc];
			return new BasicDatum<string, string>(features, c_1.Get(typeof(CoreAnnotations.AnswerAnnotation)));
		}

		/// <summary>
		/// This adds to the feature name the name of classes that are other than
		/// the current class that are involved in the clique.
		/// </summary>
		/// <remarks>
		/// This adds to the feature name the name of classes that are other than
		/// the current class that are involved in the clique.  In the CMM, these
		/// other classes become part of the conditioning feature, and only the
		/// class of the current position is being predicted.
		/// </remarks>
		/// <returns>
		/// A collection of features with extra class information put
		/// into the feature name.
		/// </returns>
		private static ICollection<string> AddOtherClasses<_T0>(ICollection<string> feats, IList<_T0> info, int loc, Clique c)
			where _T0 : CoreLabel
		{
			string addend = null;
			string pAnswer = info[loc - 1].Get(typeof(CoreAnnotations.AnswerAnnotation));
			string p2Answer = info[loc - 2].Get(typeof(CoreAnnotations.AnswerAnnotation));
			string p3Answer = info[loc - 3].Get(typeof(CoreAnnotations.AnswerAnnotation));
			string p4Answer = info[loc - 4].Get(typeof(CoreAnnotations.AnswerAnnotation));
			string p5Answer = info[loc - 5].Get(typeof(CoreAnnotations.AnswerAnnotation));
			string nAnswer = info[loc + 1].Get(typeof(CoreAnnotations.AnswerAnnotation));
			// cdm 2009: Is this really right? Do we not need to differentiate names that would collide???
			if (c == FeatureFactory.cliqueCpC)
			{
				addend = '|' + pAnswer;
			}
			else
			{
				if (c == FeatureFactory.cliqueCp2C)
				{
					addend = '|' + p2Answer;
				}
				else
				{
					if (c == FeatureFactory.cliqueCp3C)
					{
						addend = '|' + p3Answer;
					}
					else
					{
						if (c == FeatureFactory.cliqueCp4C)
						{
							addend = '|' + p4Answer;
						}
						else
						{
							if (c == FeatureFactory.cliqueCp5C)
							{
								addend = '|' + p5Answer;
							}
							else
							{
								if (c == FeatureFactory.cliqueCpCp2C)
								{
									addend = '|' + pAnswer + '-' + p2Answer;
								}
								else
								{
									if (c == FeatureFactory.cliqueCpCp2Cp3C)
									{
										addend = '|' + pAnswer + '-' + p2Answer + '-' + p3Answer;
									}
									else
									{
										if (c == FeatureFactory.cliqueCpCp2Cp3Cp4C)
										{
											addend = '|' + pAnswer + '-' + p2Answer + '-' + p3Answer + '-' + p4Answer;
										}
										else
										{
											if (c == FeatureFactory.cliqueCpCp2Cp3Cp4Cp5C)
											{
												addend = '|' + pAnswer + '-' + p2Answer + '-' + p3Answer + '-' + p4Answer + '-' + p5Answer;
											}
											else
											{
												if (c == FeatureFactory.cliqueCnC)
												{
													addend = '|' + nAnswer;
												}
												else
												{
													if (c == FeatureFactory.cliqueCpCnC)
													{
														addend = '|' + pAnswer + '-' + nAnswer;
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			if (addend == null)
			{
				return feats;
			}
			ICollection<string> newFeats = Generics.NewHashSet();
			foreach (string feat in feats)
			{
				string newFeat = feat + addend;
				newFeats.Add(newFeat);
			}
			return newFeats;
		}

		private static IList<Pair<Pattern, int>> GetThresholds(string filename)
		{
			BufferedReader @in = null;
			try
			{
				@in = IOUtils.ReaderFromString(filename);
				IList<Pair<Pattern, int>> thresholds = new List<Pair<Pattern, int>>();
				for (string line; (line = @in.ReadLine()) != null; )
				{
					int i = line.LastIndexOf(' ');
					Pattern p = Pattern.Compile(Sharpen.Runtime.Substring(line, 0, i));
					//log.info(":"+line.substring(0,i)+":");
					int t = int.Parse(Sharpen.Runtime.Substring(line, i + 1));
					Pair<Pattern, int> pair = new Pair<Pattern, int>(p, t);
					thresholds.Add(pair);
				}
				@in.Close();
				return thresholds;
			}
			catch (IOException e)
			{
				throw new RuntimeIOException("Error reading threshold file", e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(@in);
			}
		}

		public virtual void TrainSemiSup()
		{
			IDocumentReaderAndWriter<In> readerAndWriter = MakeReaderAndWriter();
			string filename = flags.trainFile;
			string biasedFilename = flags.biasedTrainFile;
			ObjectBank<IList<In>> data = MakeObjectBankFromFile(filename, readerAndWriter);
			ObjectBank<IList<In>> biasedData = MakeObjectBankFromFile(biasedFilename, readerAndWriter);
			IIndex<string> featureIndex = new HashIndex<string>();
			IIndex<string> classIndex = new HashIndex<string>();
			Dataset<string, string> dataset = GetDataset(data, featureIndex, classIndex);
			Dataset<string, string> biasedDataset = GetBiasedDataset(biasedData, featureIndex, classIndex);
			double[][] confusionMatrix = new double[][] {  };
			for (int i = 0; i < confusionMatrix.Length; i++)
			{
				// Arrays.fill(confusionMatrix[i], 0.0);  // not needed; Java arrays zero initialized
				confusionMatrix[i][i] = 1.0;
			}
			string cm = flags.confusionMatrix;
			string[] bits = cm.Split(":");
			foreach (string bit in bits)
			{
				string[] bits1 = bit.Split("\\|");
				int i1 = classIndex.IndexOf(bits1[0]);
				int i2 = classIndex.IndexOf(bits1[1]);
				double d = double.ParseDouble(bits1[2]);
				confusionMatrix[i2][i1] = d;
			}
			foreach (double[] row in confusionMatrix)
			{
				ArrayMath.Normalize(row);
			}
			for (int i_1 = 0; i_1 < confusionMatrix.Length; i_1++)
			{
				for (int j = 0; j < i_1; j++)
				{
					double d = confusionMatrix[i_1][j];
					confusionMatrix[i_1][j] = confusionMatrix[j][i_1];
					confusionMatrix[j][i_1] = d;
				}
			}
			for (int i_2 = 0; i_2 < confusionMatrix.Length; i_2++)
			{
				for (int j = 0; j < confusionMatrix.Length; j++)
				{
					log.Info("P(" + classIndex.Get(j) + '|' + classIndex.Get(i_2) + ") = " + confusionMatrix[j][i_2]);
				}
			}
			TrainSemiSup(dataset, biasedDataset, confusionMatrix);
		}

		public virtual double Weight(string feature, string label)
		{
			return ((LinearClassifier<string, string>)classifier).Weight(feature, label);
		}

		public virtual double[][] Weights()
		{
			return ((LinearClassifier<string, string>)classifier).Weights();
		}

		public override IList<In> ClassifyWithGlobalInformation(IList<In> tokenSeq, ICoreMap doc, ICoreMap sent)
		{
			return Classify(tokenSeq);
		}

		internal class Scorer<Inn> : ISequenceModel
			where Inn : CoreLabel
		{
			private readonly CMMClassifier<INN> classifier;

			private readonly int[] tagArray;

			private readonly int[] backgroundTags;

			private readonly IIndex<string> tagIndex;

			private readonly IList<INN> lineInfos;

			private readonly int pre;

			private readonly int post;

			private readonly ICollection<IList<string>> legalTags;

			private const bool Verbose = false;

			private static int[] BuildTagArray(int sz)
			{
				int[] temp = new int[sz];
				for (int i = 0; i < sz; i++)
				{
					temp[i] = i;
				}
				return temp;
			}

			public virtual int Length()
			{
				return lineInfos.Count - pre - post;
			}

			public virtual int LeftWindow()
			{
				return pre;
			}

			public virtual int RightWindow()
			{
				return post;
			}

			public virtual int[] GetPossibleValues(int position)
			{
				// if (position == 0 || position == lineInfos.size() - 1) {
				//   int[] a = new int[1];
				//   a[0] = tagIndex.indexOf(BACKGROUND);
				//   return a;
				// }
				// if (tagArray == null) {
				//   buildTagArray();
				// }
				if (position < pre)
				{
					return backgroundTags;
				}
				return tagArray;
			}

			public virtual double ScoreOf(int[] sequence)
			{
				throw new NotSupportedException();
			}

			private double[] scoreCache = null;

			private int[] lastWindow = null;

			//private int lastPos = -1;
			public virtual double ScoreOf(int[] tags, int pos)
			{
				if (false)
				{
					return ScoresOf(tags, pos)[tags[pos]];
				}
				if (lastWindow == null)
				{
					lastWindow = new int[LeftWindow() + RightWindow() + 1];
					Arrays.Fill(lastWindow, -1);
				}
				bool match = (pos == lastPos);
				for (int i = pos - LeftWindow(); i <= pos + RightWindow(); i++)
				{
					if (i == pos || i < 0)
					{
						continue;
					}
					/*log.info("p:"+pos);
					log.info("lw:"+leftWindow());
					log.info("i:"+i);*/
					match &= tags[i] == lastWindow[i - pos + LeftWindow()];
				}
				if (!match)
				{
					scoreCache = ScoresOf(tags, pos);
					for (int i_1 = pos - LeftWindow(); i_1 <= pos + RightWindow(); i_1++)
					{
						if (i_1 < 0)
						{
							continue;
						}
						lastWindow[i_1 - pos + LeftWindow()] = tags[i_1];
					}
					lastPos = pos;
				}
				return scoreCache[tags[pos]];
			}

			private int percent = -1;

			private int num = 0;

			private long secs = Runtime.CurrentTimeMillis();

			private long hit = 0;

			private long tot = 0;

			public virtual double[] ScoresOf(int[] tags, int pos)
			{
				// + "% [hit=" + hit + ", tot=" + tot + "]");
				string[] answers = new string[1 + LeftWindow() + RightWindow()];
				string[] pre = new string[LeftWindow()];
				for (int i = 0; i < 1 + LeftWindow() + RightWindow(); i++)
				{
					int absPos = pos - LeftWindow() + i;
					if (absPos < 0)
					{
						continue;
					}
					answers[i] = tagIndex.Get(tags[absPos]);
					CoreLabel li = lineInfos[absPos];
					li.Set(typeof(CoreAnnotations.AnswerAnnotation), answers[i]);
					if (i < LeftWindow())
					{
						pre[i] = answers[i];
					}
				}
				double[] scores = new double[tagIndex.Size()];
				//System.out.println("Considering: "+Arrays.asList(pre));
				if (!legalTags.Contains(Arrays.AsList(pre)) && classifier.flags.useObservedSequencesOnly)
				{
					// System.out.println("Rejecting: " + Arrays.asList(pre));
					// System.out.println(legalTags);
					Arrays.Fill(scores, -1000);
					// Double.NEGATIVE_INFINITY;
					return scores;
				}
				num++;
				hit++;
				ICounter<string> c = classifier.ScoresOf(lineInfos, pos);
				//System.out.println("Pos "+pos+" hist "+Arrays.asList(pre)+" result "+c);
				//System.out.println(c);
				//if (false && flags.justify) {
				//    System.out.println("Considering position " + pos + ", word is " + ((CoreLabel) lineInfos.get(pos)).word());
				//    //System.out.println("Datum is "+d.asFeatures());
				//    System.out.println("History: " + Arrays.asList(pre));
				//}
				foreach (string s in c.KeySet())
				{
					int t = tagIndex.IndexOf(s);
					if (t > -1)
					{
						int[] tA = GetPossibleValues(pos);
						for (int j = 0; j < tA.Length; j++)
						{
							if (tA[j] == t)
							{
								scores[j] = c.GetCount(s);
							}
						}
					}
				}
				//if (false && flags.justify) {
				//    System.out.println("Label " + s + " got score " + scores[j]);
				//}
				// normalize?
				if (classifier.Normalize())
				{
					ArrayMath.LogNormalize(scores);
				}
				return scores;
			}

			internal static double[] Recenter(double[] x)
			{
				double[] r = new double[x.Length];
				// double logTotal = Double.NEGATIVE_INFINITY;
				// for (int i = 0; i < x.length; i++)
				//    logTotal = SloppyMath.logAdd(logTotal, x[i]);
				double logTotal = ArrayMath.LogSum(x);
				for (int i = 0; i < x.Length; i++)
				{
					r[i] = x[i] - logTotal;
				}
				return r;
			}

			/// <summary>Build a Scorer.</summary>
			/// <param name="lineInfos">List of INN data items to classify</param>
			/// <param name="classifier">The trained Classifier</param>
			/// <param name="pre">Number of previous tags that condition current tag</param>
			/// <param name="post">
			/// Number of following tags that condition previous tag
			/// (if pre and post are both nonzero, then you have a
			/// dependency network tagger)
			/// </param>
			internal Scorer(IList<INN> lineInfos, IIndex<string> tagIndex, CMMClassifier<INN> classifier, int pre, int post, ICollection<IList<string>> legalTags)
			{
				this.pre = pre;
				this.post = post;
				this.lineInfos = lineInfos;
				this.tagIndex = tagIndex;
				this.classifier = classifier;
				this.legalTags = legalTags;
				backgroundTags = new int[] { tagIndex.IndexOf(classifier.flags.backgroundSymbol) };
				tagArray = BuildTagArray(tagIndex.Size());
			}
		}

		// end static class Scorer
		private bool Normalize()
		{
			return flags.normalize;
		}

		private static int lastPos = -1;

		// TODO: Looks like CMMClassifier still isn't threadsafe!
		public virtual ICounter<string> ScoresOf(IList<In> lineInfos, int pos)
		{
			//     if (pos != lastPos) {
			//       log.info(pos+".");
			//       lastPos = pos;
			//     }
			//     log.info("!");
			IDatum<string, string> d = MakeDatum(lineInfos, pos, featureFactories);
			return classifier.LogProbabilityOf(d);
		}

		/// <summary>
		/// Takes a
		/// <see cref="System.Collections.IList{E}"/>
		/// of
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s and prints the likelihood
		/// of each possible label at each point.
		/// TODO: Write this method!
		/// </summary>
		/// <param name="document">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s.
		/// </param>
		public override Triple<ICounter<int>, ICounter<int>, TwoDimensionalCounter<int, string>> PrintProbsDocument(IList<In> document)
		{
			//ClassicCounter<String> c = scoresOf(document, 0);
			throw new NotSupportedException();
		}

		/// <summary>Command-line version of the classifier.</summary>
		/// <remarks>
		/// Command-line version of the classifier.  See the class
		/// comments for examples of use, and SeqClassifierFlags
		/// for more information on supported flags.
		/// </remarks>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			StringUtils.LogInvocationString(log, args);
			Properties props = StringUtils.ArgsToProperties(args);
			CMMClassifier<CoreLabel> cmm = new CMMClassifier<CoreLabel>(props);
			string testFile = cmm.flags.testFile;
			string textFile = cmm.flags.textFile;
			string loadPath = cmm.flags.loadClassifier;
			string serializeTo = cmm.flags.serializeTo;
			// cmm.crossValidateTrainAndTest(trainFile);
			if (loadPath != null)
			{
				cmm.LoadClassifierNoExceptions(loadPath, props);
			}
			else
			{
				if (cmm.flags.loadJarClassifier != null)
				{
					// legacy option support
					cmm.LoadClassifierNoExceptions(cmm.flags.loadJarClassifier, props);
				}
				else
				{
					if (cmm.flags.trainFile != null)
					{
						if (cmm.flags.biasedTrainFile != null)
						{
							cmm.TrainSemiSup();
						}
						else
						{
							cmm.Train();
						}
					}
					else
					{
						cmm.LoadDefaultClassifier();
					}
				}
			}
			if (serializeTo != null)
			{
				cmm.SerializeClassifier(serializeTo);
			}
			if (testFile != null)
			{
				cmm.ClassifyAndWriteAnswers(testFile, cmm.MakeReaderAndWriter(), true);
			}
			else
			{
				if (cmm.flags.testFiles != null)
				{
					cmm.ClassifyAndWriteAnswers(cmm.flags.baseTestDir, cmm.flags.testFiles, cmm.MakeReaderAndWriter(), true);
				}
			}
			if (textFile != null)
			{
				IDocumentReaderAndWriter<CoreLabel> readerAndWriter = new PlainTextDocumentReaderAndWriter<CoreLabel>();
				cmm.ClassifyAndWriteAnswers(textFile, readerAndWriter, false);
			}
		}
		// end main
	}
}
