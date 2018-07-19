// Stanford Classifier - a multiclass maxent classifier
// NaiveBayesClassifier
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
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>A Naive Bayes classifier with a fixed number of features.</summary>
	/// <remarks>
	/// A Naive Bayes classifier with a fixed number of features.
	/// The features are assumed to have integer values even though RVFDatum will return doubles.
	/// </remarks>
	/// <author>Kristina Toutanova (kristina@cs.stanford.edu)</author>
	/// <author>
	/// Sarah Spikes (sdspikes@cs.stanford.edu) - Templatization.  Not sure what the weights counter
	/// is supposed to hold; given the weights function it seems to hold
	/// <c>Pair&lt;Pair&lt;L, F&gt;, Object&gt;</c>
	/// but this seems like a strange thing to expect to be passed in.
	/// </author>
	[System.Serializable]
	public class NaiveBayesClassifier<L, F> : IClassifier<L, F>, IRVFClassifier<L, F>
	{
		private const long serialVersionUID = 1544820342684024068L;

		private ICounter<Pair<Pair<L, F>, Number>> weights;

		private ICounter<L> priors;

		private ICollection<F> features;

		private bool addZeroValued;

		private ICounter<L> priorZero;

		private ICollection<L> labels;

		private readonly int zero = int.Parse(0);

		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.NaiveBayesClassifier));

		//the keys will be class and feature and value
		// we need all features to add the weights for zero-valued ones
		// whether to add features as having value 0 if they are not in Datum/RFVDatum
		//if we need to add the zeros, pre-compute the weight for all zeros for each class
		public virtual ICollection<L> Labels()
		{
			return labels;
		}

		public virtual L ClassOf(RVFDatum<L, F> example)
		{
			ICounter<L> scores = ScoresOf(example);
			return Counters.Argmax(scores);
		}

		public virtual ClassicCounter<L> ScoresOf(RVFDatum<L, F> example)
		{
			ClassicCounter<L> scores = new ClassicCounter<L>();
			Counters.AddInPlace(scores, priors);
			if (addZeroValued)
			{
				Counters.AddInPlace(scores, priorZero);
			}
			foreach (L l in labels)
			{
				double score = 0.0;
				ICounter<F> features = example.AsFeaturesCounter();
				foreach (F f in features.KeySet())
				{
					int value = (int)features.GetCount(f);
					score += Weight(l, f, int.Parse(value));
					if (addZeroValued)
					{
						score -= Weight(l, f, zero);
					}
				}
				scores.IncrementCount(l, score);
			}
			return scores;
		}

		public virtual L ClassOf(IDatum<L, F> example)
		{
			RVFDatum<L, F> rvf = new RVFDatum<L, F>(example);
			return ClassOf(rvf);
		}

		public virtual ClassicCounter<L> ScoresOf(IDatum<L, F> example)
		{
			RVFDatum<L, F> rvf = new RVFDatum<L, F>(example);
			return ScoresOf(rvf);
		}

		public NaiveBayesClassifier(ICounter<Pair<Pair<L, F>, Number>> weights, ICounter<L> priors, ICollection<L> labels, ICollection<F> features, bool addZero)
		{
			this.weights = weights;
			this.features = features;
			this.priors = priors;
			this.labels = labels;
			addZeroValued = addZero;
			if (addZeroValued)
			{
				InitZeros();
			}
		}

		public virtual float Accuracy(IEnumerator<RVFDatum<L, F>> exampleIterator)
		{
			int correct = 0;
			int total = 0;
			for (; exampleIterator.MoveNext(); )
			{
				RVFDatum<L, F> next = exampleIterator.Current;
				L guess = ClassOf(next);
				if (guess.Equals(next.Label()))
				{
					correct++;
				}
				total++;
			}
			logger.Info("correct " + correct + " out of " + total);
			return correct / (float)total;
		}

		public virtual void Print(TextWriter pw)
		{
			pw.WriteLine("priors ");
			pw.WriteLine(priors.ToString());
			pw.WriteLine("weights ");
			pw.WriteLine(weights.ToString());
		}

		public virtual void Print()
		{
			Print(System.Console.Out);
		}

		private double Weight(L label, F feature, Number val)
		{
			Pair<Pair<L, F>, Number> p = new Pair<Pair<L, F>, Number>(new Pair<L, F>(label, feature), val);
			double v = weights.GetCount(p);
			return v;
		}

		public NaiveBayesClassifier(ICounter<Pair<Pair<L, F>, Number>> weights, ICounter<L> priors, ICollection<L> labels)
			: this(weights, priors, labels, null, false)
		{
		}

		/// <summary>
		/// In case the features for which there is a value 0 in an example need to have their coefficients multiplied in,
		/// we need to pre-compute the addition
		/// priorZero(l)=sum_{features} wt(l,feat=0)
		/// </summary>
		private void InitZeros()
		{
			priorZero = new ClassicCounter<L>();
			foreach (L label in labels)
			{
				double score = 0;
				foreach (F feature in features)
				{
					score += Weight(label, feature, zero);
				}
				priorZero.SetCount(label, score);
			}
		}
	}
}
