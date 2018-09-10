// Stanford Classifier - a multiclass maxent classifier
// LinearClassifier
// Copyright (c) 2003-2007 The Board of Trustees of
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
// along with this program.  If not, see http://www.gnu.org/licenses/ .
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/classifier.html
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Implements a multiclass linear classifier.</summary>
	/// <remarks>
	/// Implements a multiclass linear classifier. At classification time this
	/// can be any generalized linear model classifier (such as a perceptron,
	/// a maxent classifier (softmax logistic regression), or an SVM).
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Jenny Finkel</author>
	/// <author>Galen Andrew (converted to arrays and indices)</author>
	/// <author>Christopher Manning (most of the printing options)</author>
	/// <author>Eric Yeh (save to text file, new constructor w/thresholds)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <author>
	/// 
	/// <literal>nmramesh@cs.stanford.edu</literal>
	/// 
	/// <see cref="LinearClassifier{L, F}.WeightsAsMapOfCounters()"/>
	/// </author>
	/// <author>Angel Chang (Add functions to get top features, and number of features with weights above a certain threshold)</author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public class LinearClassifier<L, F> : IProbabilisticClassifier<L, F>, IRVFClassifier<L, F>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.LinearClassifier));

		/// <summary>Classifier weights.</summary>
		/// <remarks>Classifier weights. First index is the featureIndex value and second index is the labelIndex value.</remarks>
		private double[][] weights;

		private IIndex<L> labelIndex;

		private IIndex<F> featureIndex;

		public bool intern = false;

		private double[] thresholds;

		private const long serialVersionUID = 8499574525453275255L;

		private const int MaxFeatureAlignWidth = 50;

		public const string TextSerializationDelimiter = "\t";

		// variable should be deleted when breaking serialization anyway....
		// = null;
		public virtual ICollection<L> Labels()
		{
			return labelIndex.ObjectsList();
		}

		public virtual ICollection<F> Features()
		{
			return featureIndex.ObjectsList();
		}

		public virtual IIndex<L> LabelIndex()
		{
			return labelIndex;
		}

		public virtual IIndex<F> FeatureIndex()
		{
			return featureIndex;
		}

		private double Weight(int iFeature, int iLabel)
		{
			if (iFeature < 0)
			{
				//logger.info("feature not seen ");
				return 0.0;
			}
			System.Diagnostics.Debug.Assert(iFeature < weights.Length);
			System.Diagnostics.Debug.Assert(iLabel < weights[iFeature].Length);
			return weights[iFeature][iLabel];
		}

		private double Weight(F feature, int iLabel)
		{
			int f = featureIndex.IndexOf(feature);
			return Weight(f, iLabel);
		}

		public virtual double Weight(F feature, L label)
		{
			int f = featureIndex.IndexOf(feature);
			int iLabel = labelIndex.IndexOf(label);
			return Weight(f, iLabel);
		}

		/* --- obsolete method from before this class was rewritten using arrays
		public Counter scoresOf(Datum example) {
		Counter scores = new Counter();
		for (L l : labels()) {
		scores.setCount(l, scoreOf(example, l));
		}
		return scores;
		}
		--- */
		/// <summary>
		/// Construct a counter with keys the labels of the classifier and
		/// values the score (unnormalized log probability) of each class.
		/// </summary>
		public virtual ICounter<L> ScoresOf(IDatum<L, F> example)
		{
			if (example is RVFDatum<object, object>)
			{
				return ScoresOfRVFDatum((RVFDatum<L, F>)example);
			}
			ICollection<F> feats = example.AsFeatures();
			int[] features = new int[feats.Count];
			int i = 0;
			foreach (F f in feats)
			{
				int index = featureIndex.IndexOf(f);
				if (index >= 0)
				{
					features[i++] = index;
				}
			}
			// } else {
			//logger.info("FEATURE LESS THAN ZERO: " + f);
			int[] activeFeatures = new int[i];
			lock (typeof(Runtime))
			{
				System.Array.Copy(features, 0, activeFeatures, 0, i);
			}
			ICounter<L> scores = new ClassicCounter<L>();
			foreach (L lab in Labels())
			{
				scores.SetCount(lab, ScoreOf(activeFeatures, lab));
			}
			return scores;
		}

		/// <summary>
		/// Given a datum's features, construct a counter with keys
		/// the labels and values the score (unnormalized log probability)
		/// for each class.
		/// </summary>
		public virtual ICounter<L> ScoresOf(int[] features)
		{
			ICounter<L> scores = new ClassicCounter<L>();
			foreach (L label in Labels())
			{
				scores.SetCount(label, ScoreOf(features, label));
			}
			return scores;
		}

		/// <summary>Returns of the score of the Datum for the specified label.</summary>
		/// <remarks>
		/// Returns of the score of the Datum for the specified label.
		/// Ignores the true label of the Datum.
		/// </remarks>
		public virtual double ScoreOf(IDatum<L, F> example, L label)
		{
			if (example is RVFDatum<object, object>)
			{
				return ScoreOfRVFDatum((RVFDatum<L, F>)example, label);
			}
			int iLabel = labelIndex.IndexOf(label);
			double score = 0.0;
			foreach (F f in example.AsFeatures())
			{
				score += Weight(f, iLabel);
			}
			return score + thresholds[iLabel];
		}

		/// <summary>
		/// Construct a counter with keys the labels of the classifier and
		/// values the score (unnormalized log probability) of each class
		/// for an RVFDatum.
		/// </summary>
		[Obsolete]
		public virtual ICounter<L> ScoresOf(RVFDatum<L, F> example)
		{
			ICounter<L> scores = new ClassicCounter<L>();
			foreach (L l in Labels())
			{
				scores.SetCount(l, ScoreOfRVFDatum(example, l));
			}
			//System.out.println("Scores are: " + scores + "   (gold: " + example.label() + ")");
			return scores;
		}

		/// <summary>
		/// Construct a counter with keys the labels of the classifier and
		/// values the score (unnormalized log probability) of each class
		/// for an RVFDatum.
		/// </summary>
		private ICounter<L> ScoresOfRVFDatum(RVFDatum<L, F> example)
		{
			ICounter<L> scores = new ClassicCounter<L>();
			// Index the features in the datum
			ICounter<F> asCounter = example.AsFeaturesCounter();
			ICounter<int> asIndexedCounter = new ClassicCounter<int>(asCounter.Size());
			foreach (KeyValuePair<F, double> entry in asCounter.EntrySet())
			{
				asIndexedCounter.SetCount(featureIndex.IndexOf(entry.Key), entry.Value);
			}
			// Set the scores appropriately
			foreach (L l in Labels())
			{
				scores.SetCount(l, ScoreOfRVFDatum(asIndexedCounter, l));
			}
			//System.out.println("Scores are: " + scores + "   (gold: " + example.label() + ")");
			return scores;
		}

		/// <summary>Returns the score of the RVFDatum for the specified label.</summary>
		/// <remarks>
		/// Returns the score of the RVFDatum for the specified label.
		/// Ignores the true label of the RVFDatum.
		/// </remarks>
		/// <param name="example">Used to get the observed x value. Its label is ignored.</param>
		/// <param name="label">The label y that the observed value is scored with.</param>
		/// <returns>A linear classifier score</returns>
		private double ScoreOfRVFDatum(RVFDatum<L, F> example, L label)
		{
			int iLabel = labelIndex.IndexOf(label);
			double score = 0.0;
			ICounter<F> features = example.AsFeaturesCounter();
			foreach (KeyValuePair<F, double> entry in features.EntrySet())
			{
				score += Weight(entry.Key, iLabel) * entry.Value;
			}
			return score + thresholds[iLabel];
		}

		/// <summary>Returns the score of the RVFDatum for the specified label.</summary>
		/// <remarks>
		/// Returns the score of the RVFDatum for the specified label.
		/// Ignores the true label of the RVFDatum.
		/// </remarks>
		private double ScoreOfRVFDatum(ICounter<int> features, L label)
		{
			int iLabel = labelIndex.IndexOf(label);
			double score = 0.0;
			foreach (KeyValuePair<int, double> entry in features.EntrySet())
			{
				score += Weight(entry.Key, iLabel) * entry.Value;
			}
			return score + thresholds[iLabel];
		}

		/// <summary>
		/// Returns of the score of the Datum as internalized features for the
		/// specified label.
		/// </summary>
		/// <remarks>
		/// Returns of the score of the Datum as internalized features for the
		/// specified label. Ignores the true label of the Datum.
		/// Doesn't consider a value for each feature.
		/// </remarks>
		private double ScoreOf(int[] feats, L label)
		{
			int iLabel = labelIndex.IndexOf(label);
			System.Diagnostics.Debug.Assert(iLabel >= 0);
			double score = 0.0;
			foreach (int feat in feats)
			{
				score += Weight(feat, iLabel);
			}
			return score + thresholds[iLabel];
		}

		/// <summary>
		/// Returns a counter mapping from each class name to the probability of
		/// that class for a certain example.
		/// </summary>
		/// <remarks>
		/// Returns a counter mapping from each class name to the probability of
		/// that class for a certain example.
		/// Looking at the the sum of each count v, should be 1.0.
		/// </remarks>
		public virtual ICounter<L> ProbabilityOf(IDatum<L, F> example)
		{
			if (example is RVFDatum<object, object>)
			{
				return ProbabilityOfRVFDatum((RVFDatum<L, F>)example);
			}
			ICounter<L> scores = LogProbabilityOf(example);
			foreach (L label in scores.KeySet())
			{
				scores.SetCount(label, Math.Exp(scores.GetCount(label)));
			}
			return scores;
		}

		/// <summary>
		/// Returns a counter mapping from each class name to the probability of
		/// that class for a certain example.
		/// </summary>
		/// <remarks>
		/// Returns a counter mapping from each class name to the probability of
		/// that class for a certain example.
		/// Looking at the the sum of each count v, should be 1.0.
		/// </remarks>
		private ICounter<L> ProbabilityOfRVFDatum(RVFDatum<L, F> example)
		{
			// NB: this duplicate method is needed so it calls the scoresOf method
			// with a RVFDatum signature
			ICounter<L> scores = LogProbabilityOfRVFDatum(example);
			foreach (L label in scores.KeySet())
			{
				scores.SetCount(label, Math.Exp(scores.GetCount(label)));
			}
			return scores;
		}

		/// <summary>
		/// Returns a counter mapping from each class name to the probability of
		/// that class for a certain example.
		/// </summary>
		/// <remarks>
		/// Returns a counter mapping from each class name to the probability of
		/// that class for a certain example.
		/// Looking at the the sum of each count v, should be 1.0.
		/// </remarks>
		[Obsolete]
		public virtual ICounter<L> ProbabilityOf(RVFDatum<L, F> example)
		{
			// NB: this duplicate method is needed so it calls the scoresOf method
			// with a RVFDatum signature
			ICounter<L> scores = LogProbabilityOf(example);
			foreach (L label in scores.KeySet())
			{
				scores.SetCount(label, Math.Exp(scores.GetCount(label)));
			}
			return scores;
		}

		/// <summary>
		/// Returns a counter mapping from each class name to the log probability of
		/// that class for a certain example.
		/// </summary>
		/// <remarks>
		/// Returns a counter mapping from each class name to the log probability of
		/// that class for a certain example.
		/// Looking at the the sum of e^v for each count v, should be 1.0.
		/// </remarks>
		public virtual ICounter<L> LogProbabilityOf(IDatum<L, F> example)
		{
			if (example is RVFDatum<object, object>)
			{
				return LogProbabilityOfRVFDatum((RVFDatum<L, F>)example);
			}
			ICounter<L> scores = ScoresOf(example);
			Counters.LogNormalizeInPlace(scores);
			return scores;
		}

		/// <summary>
		/// Given a datum's features, returns a counter mapping from each
		/// class name to the log probability of that class.
		/// </summary>
		/// <remarks>
		/// Given a datum's features, returns a counter mapping from each
		/// class name to the log probability of that class.
		/// Looking at the the sum of e^v for each count v, should be 1.
		/// </remarks>
		public virtual ICounter<L> LogProbabilityOf(int[] features)
		{
			ICounter<L> scores = ScoresOf(features);
			Counters.LogNormalizeInPlace(scores);
			return scores;
		}

		public virtual ICounter<L> ProbabilityOf(int[] features)
		{
			ICounter<L> scores = LogProbabilityOf(features);
			foreach (L label in scores.KeySet())
			{
				scores.SetCount(label, Math.Exp(scores.GetCount(label)));
			}
			return scores;
		}

		/// <summary>
		/// Returns a counter for the log probability of each of the classes
		/// looking at the the sum of e^v for each count v, should be 1
		/// </summary>
		private ICounter<L> LogProbabilityOfRVFDatum(RVFDatum<L, F> example)
		{
			// NB: this duplicate method is needed so it calls the scoresOf method
			// with an RVFDatum signature!!  Don't remove it!
			// JLS: type resolution of method parameters is static
			ICounter<L> scores = ScoresOfRVFDatum(example);
			Counters.LogNormalizeInPlace(scores);
			return scores;
		}

		/// <summary>Returns a counter for the log probability of each of the classes.</summary>
		/// <remarks>
		/// Returns a counter for the log probability of each of the classes.
		/// Looking at the the sum of e^v for each count v, should give 1.
		/// </remarks>
		[Obsolete]
		public virtual ICounter<L> LogProbabilityOf(RVFDatum<L, F> example)
		{
			// NB: this duplicate method is needed so it calls the scoresOf method
			// with an RVFDatum signature!!  Don't remove it!
			// JLS: type resolution of method parameters is static
			ICounter<L> scores = ScoresOf(example);
			Counters.LogNormalizeInPlace(scores);
			return scores;
		}

		/// <summary>Returns indices of labels</summary>
		/// <param name="labels">- Set of labels to get indices</param>
		/// <returns>Set of indices</returns>
		protected internal virtual ICollection<int> GetLabelIndices(ICollection<L> labels)
		{
			ICollection<int> iLabels = Generics.NewHashSet();
			foreach (L label in labels)
			{
				int iLabel = labelIndex.IndexOf(label);
				iLabels.Add(iLabel);
				if (iLabel < 0)
				{
					throw new ArgumentException("Unknown label " + label);
				}
			}
			return iLabels;
		}

		/// <summary>
		/// Returns number of features with weight above a certain threshold
		/// (across all labels).
		/// </summary>
		/// <param name="threshold">Threshold above which we will count the feature</param>
		/// <param name="useMagnitude">
		/// Whether the notion of "large" should ignore
		/// the sign of the feature weight.
		/// </param>
		/// <returns>number of features satisfying the specified conditions</returns>
		public virtual int GetFeatureCount(double threshold, bool useMagnitude)
		{
			int n = 0;
			foreach (double[] weightArray in weights)
			{
				foreach (double weight in weightArray)
				{
					double thisWeight = (useMagnitude) ? Math.Abs(weight) : weight;
					if (thisWeight > threshold)
					{
						n++;
					}
				}
			}
			return n;
		}

		/// <summary>Returns number of features with weight above a certain threshold.</summary>
		/// <param name="labels">
		/// Set of labels we care about when counting features
		/// Use null to get counts across all labels
		/// </param>
		/// <param name="threshold">Threshold above which we will count the feature</param>
		/// <param name="useMagnitude">
		/// Whether the notion of "large" should ignore
		/// the sign of the feature weight.
		/// </param>
		/// <returns>number of features satisfying the specified conditions</returns>
		public virtual int GetFeatureCount(ICollection<L> labels, double threshold, bool useMagnitude)
		{
			if (labels != null)
			{
				ICollection<int> iLabels = GetLabelIndices(labels);
				return GetFeatureCountLabelIndices(iLabels, threshold, useMagnitude);
			}
			else
			{
				return GetFeatureCount(threshold, useMagnitude);
			}
		}

		/// <summary>Returns number of features with weight above a certain threshold.</summary>
		/// <param name="iLabels">
		/// Set of label indices we care about when counting features
		/// Use null to get counts across all labels
		/// </param>
		/// <param name="threshold">Threshold above which we will count the feature</param>
		/// <param name="useMagnitude">
		/// Whether the notion of "large" should ignore
		/// the sign of the feature weight.
		/// </param>
		/// <returns>number of features satisfying the specified conditions</returns>
		protected internal virtual int GetFeatureCountLabelIndices(ICollection<int> iLabels, double threshold, bool useMagnitude)
		{
			int n = 0;
			foreach (double[] weightArray in weights)
			{
				foreach (int labIndex in iLabels)
				{
					double thisWeight = (useMagnitude) ? Math.Abs(weightArray[labIndex]) : weightArray[labIndex];
					if (thisWeight > threshold)
					{
						n++;
					}
				}
			}
			return n;
		}

		/// <summary>
		/// Returns list of top features with weight above a certain threshold
		/// (list is descending and across all labels).
		/// </summary>
		/// <param name="threshold">Threshold above which we will count the feature</param>
		/// <param name="useMagnitude">
		/// Whether the notion of "large" should ignore
		/// the sign of the feature weight.
		/// </param>
		/// <param name="numFeatures">How many top features to return (-1 for unlimited)</param>
		/// <returns>List of triples indicating feature, label, weight</returns>
		public virtual IList<Triple<F, L, double>> GetTopFeatures(double threshold, bool useMagnitude, int numFeatures)
		{
			return GetTopFeatures(null, threshold, useMagnitude, numFeatures, true);
		}

		/// <summary>Returns list of top features with weight above a certain threshold</summary>
		/// <param name="labels">
		/// Set of labels we care about when getting features
		/// Use null to get features across all labels
		/// </param>
		/// <param name="threshold">Threshold above which we will count the feature</param>
		/// <param name="useMagnitude">
		/// Whether the notion of "large" should ignore
		/// the sign of the feature weight.
		/// </param>
		/// <param name="numFeatures">How many top features to return (-1 for unlimited)</param>
		/// <param name="descending">Return weights in descending order</param>
		/// <returns>List of triples indicating feature, label, weight</returns>
		public virtual IList<Triple<F, L, double>> GetTopFeatures(ICollection<L> labels, double threshold, bool useMagnitude, int numFeatures, bool descending)
		{
			if (labels != null)
			{
				ICollection<int> iLabels = GetLabelIndices(labels);
				return GetTopFeaturesLabelIndices(iLabels, threshold, useMagnitude, numFeatures, descending);
			}
			else
			{
				return GetTopFeaturesLabelIndices(null, threshold, useMagnitude, numFeatures, descending);
			}
		}

		/// <summary>Returns list of top features with weight above a certain threshold</summary>
		/// <param name="iLabels">
		/// Set of label indices we care about when getting features
		/// Use null to get features across all labels
		/// </param>
		/// <param name="threshold">Threshold above which we will count the feature</param>
		/// <param name="useMagnitude">
		/// Whether the notion of "large" should ignore
		/// the sign of the feature weight.
		/// </param>
		/// <param name="numFeatures">How many top features to return (-1 for unlimited)</param>
		/// <param name="descending">Return weights in descending order</param>
		/// <returns>List of triples indicating feature, label, weight</returns>
		protected internal virtual IList<Triple<F, L, double>> GetTopFeaturesLabelIndices(ICollection<int> iLabels, double threshold, bool useMagnitude, int numFeatures, bool descending)
		{
			IPriorityQueue<Pair<int, int>> biggestKeys = new FixedPrioritiesPriorityQueue<Pair<int, int>>();
			// locate biggest keys
			for (int feat = 0; feat < weights.Length; feat++)
			{
				for (int lab = 0; lab < weights[feat].Length; lab++)
				{
					if (iLabels != null && !iLabels.Contains(lab))
					{
						continue;
					}
					double thisWeight;
					if (useMagnitude)
					{
						thisWeight = Math.Abs(weights[feat][lab]);
					}
					else
					{
						thisWeight = weights[feat][lab];
					}
					if (thisWeight > threshold)
					{
						// reverse the weight, so get smallest first
						thisWeight = -thisWeight;
						if (biggestKeys.Count == numFeatures)
						{
							// have enough features, add only if bigger
							double lowest = biggestKeys.GetPriority();
							if (thisWeight < lowest)
							{
								// remove smallest
								biggestKeys.RemoveFirst();
								biggestKeys.Add(new Pair<int, int>(feat, lab), thisWeight);
							}
						}
						else
						{
							// always add it if don't have enough features yet
							biggestKeys.Add(new Pair<int, int>(feat, lab), thisWeight);
						}
					}
				}
			}
			IList<Triple<F, L, double>> topFeatures = new List<Triple<F, L, double>>(biggestKeys.Count);
			while (!biggestKeys.IsEmpty())
			{
				Pair<int, int> p = biggestKeys.RemoveFirst();
				double weight = weights[p.First()][p.Second()];
				F feat_1 = featureIndex.Get(p.First());
				L label = labelIndex.Get(p.Second());
				topFeatures.Add(new Triple<F, L, double>(feat_1, label, weight));
			}
			if (descending)
			{
				Java.Util.Collections.Reverse(topFeatures);
			}
			return topFeatures;
		}

		/// <summary>Returns string representation of a list of top features</summary>
		/// <param name="topFeatures">List of triples indicating feature, label, weight</param>
		/// <returns>String representation of the list of features</returns>
		public virtual string TopFeaturesToString(IList<Triple<F, L, double>> topFeatures)
		{
			// find longest key length (for pretty printing) with a limit
			int maxLeng = 0;
			foreach (Triple<F, L, double> t in topFeatures)
			{
				string key = "(" + t.first + "," + t.second + ")";
				int leng = key.Length;
				if (leng > maxLeng)
				{
					maxLeng = leng;
				}
			}
			maxLeng = Math.Min(64, maxLeng);
			// set up pretty printing of weights
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMinimumFractionDigits(4);
			nf.SetMaximumFractionDigits(4);
			if (nf is DecimalFormat)
			{
				((DecimalFormat)nf).SetPositivePrefix(" ");
			}
			//print high weight features to a String
			StringBuilder sb = new StringBuilder();
			foreach (Triple<F, L, double> t_1 in topFeatures)
			{
				string key = "(" + t_1.first + "," + t_1.second + ")";
				sb.Append(StringUtils.Pad(key, maxLeng));
				sb.Append(" ");
				double cnt = t_1.Third();
				if (double.IsInfinite(cnt))
				{
					sb.Append(cnt);
				}
				else
				{
					sb.Append(nf.Format(cnt));
				}
				sb.Append("\n");
			}
			return sb.ToString();
		}

		/// <summary>Return a String that prints features with large weights.</summary>
		/// <param name="useMagnitude">
		/// Whether the notion of "large" should ignore
		/// the sign of the feature weight.
		/// </param>
		/// <param name="numFeatures">How many top features to print</param>
		/// <param name="printDescending">Print weights in descending order</param>
		/// <returns>The String representation of features with large weights</returns>
		public virtual string ToBiggestWeightFeaturesString(bool useMagnitude, int numFeatures, bool printDescending)
		{
			// this used to try to use a TreeSet, but that was WRONG....
			IPriorityQueue<Pair<int, int>> biggestKeys = new FixedPrioritiesPriorityQueue<Pair<int, int>>();
			// locate biggest keys
			for (int feat = 0; feat < weights.Length; feat++)
			{
				for (int lab = 0; lab < weights[feat].Length; lab++)
				{
					double thisWeight;
					// reverse the weight, so get smallest first
					if (useMagnitude)
					{
						thisWeight = -Math.Abs(weights[feat][lab]);
					}
					else
					{
						thisWeight = -weights[feat][lab];
					}
					if (biggestKeys.Count == numFeatures)
					{
						// have enough features, add only if bigger
						double lowest = biggestKeys.GetPriority();
						if (thisWeight < lowest)
						{
							// remove smallest
							biggestKeys.RemoveFirst();
							biggestKeys.Add(new Pair<int, int>(feat, lab), thisWeight);
						}
					}
					else
					{
						// always add it if don't have enough features yet
						biggestKeys.Add(new Pair<int, int>(feat, lab), thisWeight);
					}
				}
			}
			// Put in List either reversed or not
			// (Note: can't repeatedly iterate over PriorityQueue.)
			int actualSize = biggestKeys.Count;
			Pair<int, int>[] bigArray = ErasureUtils.MkTArray(typeof(Pair), actualSize);
			// logger.info("biggestKeys is " + biggestKeys);
			if (printDescending)
			{
				for (int j = actualSize - 1; j >= 0; j--)
				{
					bigArray[j] = biggestKeys.RemoveFirst();
				}
			}
			else
			{
				for (int j = 0; j < actualSize; j--)
				{
					bigArray[j] = biggestKeys.RemoveFirst();
				}
			}
			IList<Pair<int, int>> bigColl = Arrays.AsList(bigArray);
			// logger.info("bigColl is " + bigColl);
			// find longest key length (for pretty printing) with a limit
			int maxLeng = 0;
			foreach (Pair<int, int> p in bigColl)
			{
				string key = "(" + featureIndex.Get(p.first) + "," + labelIndex.Get(p.second) + ")";
				int leng = key.Length;
				if (leng > maxLeng)
				{
					maxLeng = leng;
				}
			}
			maxLeng = Math.Min(64, maxLeng);
			// set up pretty printing of weights
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMinimumFractionDigits(4);
			nf.SetMaximumFractionDigits(4);
			if (nf is DecimalFormat)
			{
				((DecimalFormat)nf).SetPositivePrefix(" ");
			}
			//print high weight features to a String
			StringBuilder sb = new StringBuilder("LinearClassifier [printing top " + numFeatures + " features]\n");
			foreach (Pair<int, int> p_1 in bigColl)
			{
				string key = "(" + featureIndex.Get(p_1.first) + "," + labelIndex.Get(p_1.second) + ")";
				sb.Append(StringUtils.Pad(key, maxLeng));
				sb.Append(" ");
				double cnt = weights[p_1.first][p_1.second];
				if (double.IsInfinite(cnt))
				{
					sb.Append(cnt);
				}
				else
				{
					sb.Append(nf.Format(cnt));
				}
				sb.Append("\n");
			}
			return sb.ToString();
		}

		/// <summary>
		/// Similar to histogram but exact values of the weights
		/// to see whether there are many equal weights.
		/// </summary>
		/// <returns>A human readable string about the classifier distribution.</returns>
		public virtual string ToDistributionString(int threshold)
		{
			ICounter<double> weightCounts = new ClassicCounter<double>();
			StringBuilder s = new StringBuilder();
			s.Append("Total number of weights: ").Append(TotalSize());
			foreach (double[] weightArray in weights)
			{
				foreach (double weight in weightArray)
				{
					weightCounts.IncrementCount(weight);
				}
			}
			s.Append("Counts of weights\n");
			ICollection<double> keys = Counters.KeysAbove(weightCounts, threshold);
			s.Append(keys.Count).Append(" keys occur more than ").Append(threshold).Append(" times ");
			return s.ToString();
		}

		public virtual int TotalSize()
		{
			return labelIndex.Size() * featureIndex.Size();
		}

		public virtual string ToHistogramString()
		{
			// big classifiers
			double[][] hist = new double[][] { new double[202], new double[202], new double[202] };
			object[][] histEg = new object[][] { new object[202], new object[202], new object[202] };
			int num = 0;
			int pos = 0;
			int neg = 0;
			int zero = 0;
			double total = 0.0;
			double x2total = 0.0;
			double max = 0.0;
			double min = 0.0;
			for (int f = 0; f < weights.Length; f++)
			{
				for (int l = 0; l < weights[f].Length; l++)
				{
					Pair<F, L> feat = new Pair<F, L>(featureIndex.Get(f), labelIndex.Get(l));
					num++;
					double wt = weights[f][l];
					total += wt;
					x2total += wt * wt;
					if (wt > max)
					{
						max = wt;
					}
					if (wt < min)
					{
						min = wt;
					}
					if (wt < 0.0)
					{
						neg++;
					}
					else
					{
						if (wt > 0.0)
						{
							pos++;
						}
						else
						{
							zero++;
						}
					}
					int index;
					index = BucketizeValue(wt);
					hist[0][index]++;
					if (histEg[0][index] == null)
					{
						histEg[0][index] = feat;
					}
					if (wt < 0.1 && wt >= -0.1)
					{
						index = BucketizeValue(wt * 100.0);
						hist[1][index]++;
						if (histEg[1][index] == null)
						{
							histEg[1][index] = feat;
						}
						if (wt < 0.001 && wt >= -0.001)
						{
							index = BucketizeValue(wt * 10000.0);
							hist[2][index]++;
							if (histEg[2][index] == null)
							{
								histEg[2][index] = feat;
							}
						}
					}
				}
			}
			double ave = total / num;
			double stddev = (x2total / num) - ave * ave;
			StringWriter sw = new StringWriter();
			PrintWriter pw = new PrintWriter(sw);
			pw.Println("Linear classifier with " + num + " f(x,y) features");
			pw.Println("Average weight: " + ave + "; std dev: " + stddev);
			pw.Println("Max weight: " + max + " min weight: " + min);
			pw.Println("Weights: " + neg + " negative; " + pos + " positive; " + zero + " zero.");
			PrintHistCounts(0, "Counts of lambda parameters between [-10, 10)", pw, hist, histEg);
			PrintHistCounts(1, "Closeup view of [-0.1, 0.1) depicted * 10^2", pw, hist, histEg);
			PrintHistCounts(2, "Closeup view of [-0.001, 0.001) depicted * 10^4", pw, hist, histEg);
			pw.Close();
			return sw.ToString();
		}

		/// <summary>Print out a partial representation of a linear classifier.</summary>
		/// <remarks>
		/// Print out a partial representation of a linear classifier.
		/// This just calls toString("WeightHistogram", 0)
		/// </remarks>
		public override string ToString()
		{
			return ToString("WeightHistogram", 0);
		}

		/// <summary>
		/// Print out a partial representation of a linear classifier in one of
		/// several ways.
		/// </summary>
		/// <param name="style">
		/// Options are:
		/// HighWeight: print out the param parameters with largest weights;
		/// HighMagnitude: print out the param parameters for which the absolute
		/// value of their weight is largest;
		/// AllWeights: print out the weights of all features;
		/// WeightHistogram: print out a particular hard-coded textual histogram
		/// representation of a classifier;
		/// WeightDistribution;
		/// </param>
		/// <param name="param">Determines the number of things printed in certain styles</param>
		/// <exception cref="System.ArgumentException">if the style name is unrecognized</exception>
		public virtual string ToString(string style, int param)
		{
			if (style == null || style.IsEmpty())
			{
				return "LinearClassifier with " + featureIndex.Size() + " features, " + labelIndex.Size() + " classes, and " + labelIndex.Size() * featureIndex.Size() + " parameters.\n";
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(style, "HighWeight"))
				{
					return ToBiggestWeightFeaturesString(false, param, true);
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(style, "HighMagnitude"))
					{
						return ToBiggestWeightFeaturesString(true, param, true);
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(style, "AllWeights"))
						{
							return ToAllWeightsString();
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(style, "WeightHistogram"))
							{
								return ToHistogramString();
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(style, "WeightDistribution"))
								{
									return ToDistributionString(param);
								}
								else
								{
									throw new ArgumentException("Unknown style: " + style);
								}
							}
						}
					}
				}
			}
		}

		/// <summary>Convert parameter value into number between 0 and 201</summary>
		private static int BucketizeValue(double wt)
		{
			int index;
			if (wt >= 0.0)
			{
				index = ((int)(wt * 10.0)) + 100;
			}
			else
			{
				index = ((int)(Math.Floor(wt * 10.0))) + 100;
			}
			if (index < 0)
			{
				index = 201;
			}
			else
			{
				if (index > 200)
				{
					index = 200;
				}
			}
			return index;
		}

		/// <summary>Print histogram counts from hist and examples over a certain range</summary>
		private static void PrintHistCounts(int ind, string title, StreamWriter pw, double[][] hist, object[][] histEg)
		{
			pw.Println(title);
			for (int i = 0; i < 200; i++)
			{
				int intPart;
				int fracPart;
				if (i < 100)
				{
					intPart = 10 - ((i + 9) / 10);
					fracPart = (10 - (i % 10)) % 10;
				}
				else
				{
					intPart = (i / 10) - 10;
					fracPart = i % 10;
				}
				pw.Print("[" + ((i < 100) ? "-" : string.Empty) + intPart + "." + fracPart + ", " + ((i < 100) ? "-" : string.Empty) + intPart + "." + fracPart + "+0.1): " + hist[ind][i]);
				if (histEg[ind][i] != null)
				{
					pw.Print("  [" + histEg[ind][i] + ((hist[ind][i] > 1) ? ", ..." : string.Empty) + "]");
				}
				pw.Println();
			}
		}

		//TODO: Sort of assumes that Labels are Strings...
		public virtual string ToAllWeightsString()
		{
			StringWriter sw = new StringWriter();
			PrintWriter pw = new PrintWriter(sw);
			pw.Println("Linear classifier with the following weights");
			IDatum<L, F> allFeatures = new BasicDatum<L, F>(Features(), (L)null);
			JustificationOf(allFeatures, pw);
			return sw.ToString();
		}

		/// <summary>
		/// Print all features in the classifier and the weight that they assign
		/// to each class.
		/// </summary>
		/// <remarks>
		/// Print all features in the classifier and the weight that they assign
		/// to each class. Print to stderr.
		/// </remarks>
		public virtual void Dump()
		{
			IDatum<L, F> allFeatures = new BasicDatum<L, F>(Features(), (L)null);
			JustificationOf(allFeatures);
		}

		/// <summary>
		/// Print all features in the classifier and the weight that they assign
		/// to each class.
		/// </summary>
		/// <remarks>
		/// Print all features in the classifier and the weight that they assign
		/// to each class. Print to the given PrintWriter.
		/// </remarks>
		public virtual void Dump(StreamWriter pw)
		{
			IDatum<L, F> allFeatures = new BasicDatum<L, F>(Features(), (L)null);
			JustificationOf(allFeatures, pw);
		}

		/// <summary>
		/// Print all features in the classifier and the weight that they assign
		/// to each class.
		/// </summary>
		/// <remarks>
		/// Print all features in the classifier and the weight that they assign
		/// to each class. The feature names are printed in sorted order.
		/// </remarks>
		public virtual void DumpSorted()
		{
			IDatum<L, F> allFeatures = new BasicDatum<L, F>(Features(), (L)null);
			JustificationOf(allFeatures, new PrintWriter(System.Console.Error, true), true);
		}

		/// <summary>
		/// Print all features active for a particular datum and the weight that
		/// the classifier assigns to each class for those features.
		/// </summary>
		private void JustificationOfRVFDatum(RVFDatum<L, F> example, StreamWriter pw)
		{
			int featureLength = 0;
			int labelLength = 6;
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMinimumFractionDigits(2);
			nf.SetMaximumFractionDigits(2);
			if (nf is DecimalFormat)
			{
				((DecimalFormat)nf).SetPositivePrefix(" ");
			}
			ICounter<F> features = example.AsFeaturesCounter();
			foreach (F f in features.KeySet())
			{
				featureLength = Math.Max(featureLength, f.ToString().Length + 2 + nf.Format(features.GetCount(f)).Length);
			}
			// make as wide as total printout
			featureLength = Math.Max(featureLength, "Total:".Length);
			// don't make it ridiculously wide
			featureLength = Math.Min(featureLength, MaxFeatureAlignWidth);
			foreach (L l in Labels())
			{
				labelLength = Math.Max(labelLength, l.ToString().Length);
			}
			StringBuilder header = new StringBuilder();
			for (int s = 0; s < featureLength; s++)
			{
				header.Append(' ');
			}
			foreach (L l_1 in Labels())
			{
				header.Append(' ');
				header.Append(StringUtils.Pad(l_1, labelLength));
			}
			pw.Println(header);
			foreach (F f_1 in features.KeySet())
			{
				string fStr = f_1.ToString();
				StringBuilder line = new StringBuilder(fStr);
				line.Append('[').Append(nf.Format(features.GetCount(f_1))).Append(']');
				fStr = line.ToString();
				for (int s_1 = fStr.Length; s_1 < featureLength; s_1++)
				{
					line.Append(' ');
				}
				foreach (L l_2 in Labels())
				{
					string lStr = nf.Format(Weight(f_1, l_2));
					line.Append(' ');
					line.Append(lStr);
					for (int s_2 = lStr.Length; s_2 < labelLength; s_2++)
					{
						line.Append(' ');
					}
				}
				pw.Println(line);
			}
			ICounter<L> scores = ScoresOfRVFDatum(example);
			StringBuilder footer = new StringBuilder("Total:");
			for (int s_3 = footer.Length; s_3 < featureLength; s_3++)
			{
				footer.Append(' ');
			}
			foreach (L l_3 in Labels())
			{
				footer.Append(' ');
				string str = nf.Format(scores.GetCount(l_3));
				footer.Append(str);
				for (int s_1 = str.Length; s_1 < labelLength; s_1++)
				{
					footer.Append(' ');
				}
			}
			pw.Println(footer);
			Distribution<L> distr = Distribution.DistributionFromLogisticCounter(scores);
			footer = new StringBuilder("Prob:");
			for (int s_4 = footer.Length; s_4 < featureLength; s_4++)
			{
				footer.Append(' ');
			}
			foreach (L l_4 in Labels())
			{
				footer.Append(' ');
				string str = nf.Format(distr.GetCount(l_4));
				footer.Append(str);
				for (int s_1 = str.Length; s_1 < labelLength; s_1++)
				{
					footer.Append(' ');
				}
			}
			pw.Println(footer);
		}

		public virtual void JustificationOf(IDatum<L, F> example)
		{
			PrintWriter pw = new PrintWriter(System.Console.Error, true);
			JustificationOf(example, pw);
		}

		/// <summary>
		/// Print all features active for a particular datum and the weight that
		/// the classifier assigns to each class for those features.
		/// </summary>
		public virtual void JustificationOf(IDatum<L, F> example, StreamWriter pw)
		{
			JustificationOf(example, pw, null);
		}

		/// <summary>
		/// Print all features active for a particular datum and the weight that
		/// the classifier assigns to each class for those features.
		/// </summary>
		/// <remarks>
		/// Print all features active for a particular datum and the weight that
		/// the classifier assigns to each class for those features. Sorts by feature
		/// name if 'sorted' is true.
		/// </remarks>
		public virtual void JustificationOf(IDatum<L, F> example, StreamWriter pw, bool sorted)
		{
			if (example is RVFDatum<object, object>)
			{
				JustificationOf(example, pw, null, sorted);
			}
		}

		public virtual void JustificationOf<T>(IDatum<L, F> example, StreamWriter pw, Func<F, T> printer)
		{
			JustificationOf(example, pw, printer, false);
		}

		/// <summary>
		/// Print all features active for a particular datum and the weight that
		/// the classifier assigns to each class for those features.
		/// </summary>
		/// <param name="example">The datum for which features are to be printed</param>
		/// <param name="pw">Where to print it to</param>
		/// <param name="printer">
		/// If this is non-null, then it is applied to each
		/// feature to convert it to a more readable form
		/// </param>
		/// <param name="sortedByFeature">Whether to sort by feature names</param>
		public virtual void JustificationOf<T>(IDatum<L, F> example, PrintWriter pw, Func<F, T> printer, bool sortedByFeature)
		{
			if (example is RVFDatum<object, object>)
			{
				JustificationOfRVFDatum((RVFDatum<L, F>)example, pw);
				return;
			}
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMinimumFractionDigits(2);
			nf.SetMaximumFractionDigits(2);
			if (nf is DecimalFormat)
			{
				((DecimalFormat)nf).SetPositivePrefix(" ");
			}
			// determine width for features, making it at least total's width
			int featureLength = 0;
			//TODO: not really sure what this Printer is supposed to spit out...
			foreach (F f in example.AsFeatures())
			{
				int length = f.ToString().Length;
				if (printer != null)
				{
					length = printer.Apply(f).ToString().Length;
				}
				featureLength = Math.Max(featureLength, length);
			}
			// make as wide as total printout
			featureLength = Math.Max(featureLength, "Total:".Length);
			// don't make it ridiculously wide
			featureLength = Math.Min(featureLength, MaxFeatureAlignWidth);
			// determine width for labels
			int labelLength = 6;
			foreach (L l in Labels())
			{
				labelLength = Math.Max(labelLength, l.ToString().Length);
			}
			// print header row of output listing classes
			StringBuilder header = new StringBuilder(string.Empty);
			for (int s = 0; s < featureLength; s++)
			{
				header.Append(' ');
			}
			foreach (L l_1 in Labels())
			{
				header.Append(' ');
				header.Append(StringUtils.Pad(l_1, labelLength));
			}
			pw.Println(header);
			// print active features and weights per class
			ICollection<F> featColl = example.AsFeatures();
			if (sortedByFeature)
			{
				featColl = ErasureUtils.SortedIfPossible(featColl);
			}
			foreach (F f_1 in featColl)
			{
				string fStr;
				if (printer != null)
				{
					fStr = printer.Apply(f_1).ToString();
				}
				else
				{
					fStr = f_1.ToString();
				}
				StringBuilder line = new StringBuilder(fStr);
				for (int s_1 = fStr.Length; s_1 < featureLength; s_1++)
				{
					line.Append(' ');
				}
				foreach (L l_2 in Labels())
				{
					string lStr = nf.Format(Weight(f_1, l_2));
					line.Append(' ');
					line.Append(lStr);
					for (int s_2 = lStr.Length; s_2 < labelLength; s_2++)
					{
						line.Append(' ');
					}
				}
				pw.Println(line);
			}
			// Print totals, probs, etc.
			ICounter<L> scores = ScoresOf(example);
			StringBuilder footer = new StringBuilder("Total:");
			for (int s_3 = footer.Length; s_3 < featureLength; s_3++)
			{
				footer.Append(' ');
			}
			foreach (L l_3 in Labels())
			{
				footer.Append(' ');
				string str = nf.Format(scores.GetCount(l_3));
				footer.Append(str);
				for (int s_1 = str.Length; s_1 < labelLength; s_1++)
				{
					footer.Append(' ');
				}
			}
			pw.Println(footer);
			Distribution<L> distr = Distribution.DistributionFromLogisticCounter(scores);
			footer = new StringBuilder("Prob:");
			for (int s_4 = footer.Length; s_4 < featureLength; s_4++)
			{
				footer.Append(' ');
			}
			foreach (L l_4 in Labels())
			{
				footer.Append(' ');
				string str = nf.Format(distr.GetCount(l_4));
				footer.Append(str);
				for (int s_1 = str.Length; s_1 < labelLength; s_1++)
				{
					footer.Append(' ');
				}
			}
			pw.Println(footer);
		}

		/// <summary>This method returns a map from each label to a counter of feature weights for that label.</summary>
		/// <remarks>
		/// This method returns a map from each label to a counter of feature weights for that label.
		/// Useful for feature analysis.
		/// </remarks>
		/// <returns>a map of counters</returns>
		public virtual IDictionary<L, ICounter<F>> WeightsAsMapOfCounters()
		{
			IDictionary<L, ICounter<F>> mapOfCounters = Generics.NewHashMap();
			foreach (L label in labelIndex)
			{
				int labelID = labelIndex.IndexOf(label);
				ICounter<F> c = new ClassicCounter<F>();
				mapOfCounters[label] = c;
				foreach (F f in featureIndex)
				{
					c.IncrementCount(f, weights[featureIndex.IndexOf(f)][labelID]);
				}
			}
			return mapOfCounters;
		}

		public virtual ICounter<L> ScoresOf(IDatum<L, F> example, ICollection<L> possibleLabels)
		{
			ICounter<L> scores = new ClassicCounter<L>();
			foreach (L l in possibleLabels)
			{
				if (labelIndex.IndexOf(l) == -1)
				{
					continue;
				}
				double score = ScoreOf(example, l);
				scores.SetCount(l, score);
			}
			return scores;
		}

		/* -- looks like a failed attempt at micro-optimization --
		
		public L experimentalClassOf(Datum<L,F> example) {
		if(example instanceof RVFDatum<?, ?>) {
		throw new UnsupportedOperationException();
		}
		
		int labelCount = weights[0].length;
		//System.out.printf("labelCount: %d\n", labelCount);
		Collection<F> features = example.asFeatures();
		
		int[] featureInts = new int[features.size()];
		int fI = 0;
		for (F feature : features) {
		featureInts[fI++] = featureIndex.indexOf(feature);
		}
		//System.out.println("Features: "+features);
		double bestScore = Double.NEGATIVE_INFINITY;
		int bestI = 0;
		for (int i = 0; i < labelCount; i++) {
		double score = 0;
		for (int j = 0; j < featureInts.length; j++) {
		if (featureInts[j] < 0) continue;
		score += weights[featureInts[j]][i];
		}
		if (score > bestScore) {
		bestI = i;
		bestScore = score;
		}
		//System.out.printf("Score: %s(%d): %e\n", labelIndex.get(i), i, score);
		}
		//System.out.printf("label(%d): %s\n", bestI, labelIndex.get(bestI));;
		return labelIndex.get(bestI);
		}
		-- */
		public virtual L ClassOf(IDatum<L, F> example)
		{
			if (example is RVFDatum<object, object>)
			{
				return ClassOfRVFDatum((RVFDatum<L, F>)example);
			}
			ICounter<L> scores = ScoresOf(example);
			return Counters.Argmax(scores);
		}

		private L ClassOfRVFDatum(RVFDatum<L, F> example)
		{
			ICounter<L> scores = ScoresOfRVFDatum(example);
			return Counters.Argmax(scores);
		}

		[Obsolete]
		public virtual L ClassOf(RVFDatum<L, F> example)
		{
			ICounter<L> scores = ScoresOf(example);
			return Counters.Argmax(scores);
		}

		/// <summary>For Kryo -- can be private</summary>
		private LinearClassifier()
		{
		}

		/// <summary>Make a linear classifier from the parameters.</summary>
		/// <remarks>Make a linear classifier from the parameters. The parameters are used, not copied.</remarks>
		/// <param name="weights">
		/// The parameters of the classifier. The first index is the
		/// featureIndex value and second index is the labelIndex value.
		/// </param>
		/// <param name="featureIndex">An index from F to integers used to index the features in the weights array</param>
		/// <param name="labelIndex">An index from L to integers used to index the labels in the weights array</param>
		public LinearClassifier(double[][] weights, IIndex<F> featureIndex, IIndex<L> labelIndex)
		{
			this.featureIndex = featureIndex;
			this.labelIndex = labelIndex;
			this.weights = weights;
			thresholds = new double[labelIndex.Size()];
		}

		/// <exception cref="System.Exception"/>
		public LinearClassifier(double[][] weights, IIndex<F> featureIndex, IIndex<L> labelIndex, double[] thresholds)
		{
			// Arrays.fill(thresholds, 0.0); // not needed; Java arrays zero initialized
			// todo: This is unused and seems broken (ignores passed in thresholds)
			this.featureIndex = featureIndex;
			this.labelIndex = labelIndex;
			this.weights = weights;
			if (thresholds.Length != labelIndex.Size())
			{
				throw new Exception("Number of thresholds and number of labels do not match.");
			}
			thresholds = new double[thresholds.Length];
			int curr = 0;
			foreach (double tval in thresholds)
			{
				thresholds[curr++] = tval;
			}
			Arrays.Fill(thresholds, 0.0);
		}

		private static ICounter<Pair<F, L>> MakeWeightCounter<F, L>(double[] weights, IIndex<Pair<F, L>> weightIndex)
		{
			ICounter<Pair<F, L>> weightCounter = new ClassicCounter<Pair<F, L>>();
			for (int i = 0; i < weightIndex.Size(); i++)
			{
				if (weights[i] == 0)
				{
					continue;
				}
				// no need to save 0 weights
				weightCounter.SetCount(weightIndex.Get(i), weights[i]);
			}
			return weightCounter;
		}

		public LinearClassifier(double[] weights, IIndex<Pair<F, L>> weightIndex)
			: this(MakeWeightCounter(weights, weightIndex))
		{
		}

		public LinearClassifier(ICounter<Pair<F, L>> weightCounter)
			: this(weightCounter, new ClassicCounter<L>())
		{
		}

		public LinearClassifier(ICounter<Pair<F, L>> weightCounter, ICounter<L> thresholdsC)
		{
			ICollection<Pair<F, L>> keys = weightCounter.KeySet();
			featureIndex = new HashIndex<F>();
			labelIndex = new HashIndex<L>();
			foreach (Pair<F, L> p in keys)
			{
				featureIndex.Add(p.First());
				labelIndex.Add(p.Second());
			}
			thresholds = new double[labelIndex.Size()];
			foreach (L label in labelIndex)
			{
				thresholds[labelIndex.IndexOf(label)] = thresholdsC.GetCount(label);
			}
			weights = new double[][] {  };
			Pair<F, L> tempPair = new Pair<F, L>();
			for (int f = 0; f < weights.Length; f++)
			{
				for (int l = 0; l < weights[f].Length; l++)
				{
					tempPair.first = featureIndex.Get(f);
					tempPair.second = labelIndex.Get(l);
					weights[f][l] = weightCounter.GetCount(tempPair);
				}
			}
		}

		public virtual void AdaptWeights(Dataset<L, F> adapt, LinearClassifierFactory<L, F> lcf)
		{
			logger.Info("before adapting, weights size=" + weights.Length);
			weights = lcf.AdaptWeights(weights, adapt);
			logger.Info("after adapting, weights size=" + weights.Length);
		}

		public virtual double[][] Weights()
		{
			return weights;
		}

		public virtual void SetWeights(double[][] newWeights)
		{
			weights = newWeights;
		}

		/// <summary>Loads a classifier from a file.</summary>
		/// <remarks>
		/// Loads a classifier from a file.
		/// Simple convenience wrapper for IOUtils.readFromString.
		/// </remarks>
		public static Edu.Stanford.Nlp.Classify.LinearClassifier<L, F> ReadClassifier<L, F>(string loadPath)
		{
			logger.Info("Deserializing classifier from " + loadPath + "...");
			try
			{
				ObjectInputStream ois = IOUtils.ReadStreamFromString(loadPath);
				Edu.Stanford.Nlp.Classify.LinearClassifier<L, F> classifier = ErasureUtils.UncheckedCast<Edu.Stanford.Nlp.Classify.LinearClassifier<L, F>>(ois.ReadObject());
				ois.Close();
				return classifier;
			}
			catch (Exception e)
			{
				throw new Exception("Deserialization failed: " + e.Message, e);
			}
		}

		/// <summary>Convenience wrapper for IOUtils.writeObjectToFile.</summary>
		public static void WriteClassifier<_T0>(Edu.Stanford.Nlp.Classify.LinearClassifier<_T0> classifier, string serializePath)
		{
			try
			{
				IOUtils.WriteObjectToFile(classifier, serializePath);
				logger.Info("Serializing classifier to " + serializePath + "... done.");
			}
			catch (Exception e)
			{
				throw new Exception("Serialization failed: " + e.Message, e);
			}
		}

		/// <summary>Saves this out to a standard text file, instead of as a serialized Java object.</summary>
		/// <remarks>
		/// Saves this out to a standard text file, instead of as a serialized Java object.
		/// NOTE: this currently assumes feature and weights are represented as Strings.
		/// </remarks>
		/// <param name="file">String filepath to write out to.</param>
		public virtual void SaveToFilename(string file)
		{
			try
			{
				File tgtFile = new File(file);
				BufferedWriter @out = new BufferedWriter(new FileWriter(tgtFile));
				// output index first, blank delimiter, outline feature index, then weights
				labelIndex.SaveToWriter(@out);
				featureIndex.SaveToWriter(@out);
				int numLabels = labelIndex.Size();
				int numFeatures = featureIndex.Size();
				for (int featIndex = 0; featIndex < numFeatures; featIndex++)
				{
					for (int labelIndex = 0; labelIndex < numLabels; labelIndex++)
					{
						@out.Write(featIndex.ToString());
						@out.Write(TextSerializationDelimiter);
						@out.Write(labelIndex.ToString());
						@out.Write(TextSerializationDelimiter);
						@out.Write(Weight(featIndex, labelIndex).ToString());
						@out.Write("\n");
					}
				}
				// write out thresholds: first item after blank is the number of thresholds, after is the threshold array values.
				@out.Write("\n");
				@out.Write(thresholds.Length.ToString());
				@out.Write("\n");
				foreach (double val in thresholds)
				{
					@out.Write(val.ToString());
					@out.Write("\n");
				}
				@out.Close();
			}
			catch (Exception e)
			{
				logger.Info("Error attempting to save classifier to file=" + file);
				logger.Info(e);
			}
		}
	}
}
