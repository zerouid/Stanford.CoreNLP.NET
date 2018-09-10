// Stanford Classifier - a multiclass maxent classifier
// LogisticClassifier
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
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;

namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>A classifier for binary logistic regression problems.</summary>
	/// <remarks>
	/// A classifier for binary logistic regression problems.
	/// This uses the standard statistics textbook formulation of binary
	/// logistic regression, which is more efficient than using the
	/// LinearClassifier class.
	/// </remarks>
	/// <author>Galen Andrew</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <author>
	/// Ramesh Nallapati nmramesh@cs.stanford.edu
	/// <see cref="LogisticClassifier{L, F}.JustificationOf(System.Collections.ICollection{E})"/>
	/// </author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public class LogisticClassifier<L, F> : IClassifier<L, F>, IRVFClassifier<L, F>
	{
		private const long serialVersionUID = 6672245467246897192L;

		private double[] weights;

		private IIndex<F> featureIndex;

		private L[] classes = ErasureUtils.MkTArray<L>(typeof(object), 2);

		[Obsolete]
		private LogPrior prior;

		[Obsolete]
		private bool biased = false;

		/* Serializable */
		//TODO make it implement ProbabilisticClassifier as well. --Ramesh 12/03/2009.
		public override string ToString()
		{
			if (featureIndex == null)
			{
				return string.Empty;
			}
			StringBuilder sb = new StringBuilder();
			foreach (F f in featureIndex)
			{
				sb.Append(classes[1]).Append(" / ").Append(f).Append(" = ").Append(weights[featureIndex.IndexOf(f)]);
			}
			return sb.ToString();
		}

		public virtual L GetLabelForInternalPositiveClass()
		{
			return classes[1];
		}

		public virtual L GetLabelForInternalNegativeClass()
		{
			return classes[0];
		}

		public virtual ICounter<F> WeightsAsCounter()
		{
			ICounter<F> c = new ClassicCounter<F>();
			foreach (F f in featureIndex)
			{
				double w = weights[featureIndex.IndexOf(f)];
				if (w != 0.0)
				{
					c.SetCount(f, w);
				}
			}
			return c;
		}

		public virtual IIndex<F> GetFeatureIndex()
		{
			return featureIndex;
		}

		public virtual double[] GetWeights()
		{
			return weights;
		}

		public LogisticClassifier(double[] weights, IIndex<F> featureIndex, L[] classes)
		{
			this.weights = weights;
			this.featureIndex = featureIndex;
			this.classes = classes;
		}

		[Obsolete]
		public LogisticClassifier(bool biased)
			: this(new LogPrior(LogPrior.LogPriorType.Quadratic), biased)
		{
		}

		[Obsolete]
		public LogisticClassifier(LogPrior prior)
		{
			//use  LogisticClassifierFactory instead
			//use  in LogisticClassifierFactory instead.
			this.prior = prior;
		}

		[Obsolete]
		public LogisticClassifier(LogPrior prior, bool biased)
		{
			//use  in LogisticClassifierFactory instead
			this.prior = prior;
			this.biased = biased;
		}

		public virtual ICollection<L> Labels()
		{
			ICollection<L> l = new LinkedList<L>();
			l.Add(classes[0]);
			l.Add(classes[1]);
			return l;
		}

		public virtual L ClassOf(IDatum<L, F> datum)
		{
			if (datum is RVFDatum<object, object>)
			{
				return ClassOfRVFDatum((RVFDatum<L, F>)datum);
			}
			return ClassOf(datum.AsFeatures());
		}

		[Obsolete]
		public virtual L ClassOf(RVFDatum<L, F> example)
		{
			//use classOf(Datum) instead.
			return ClassOf(example.AsFeaturesCounter());
		}

		private L ClassOfRVFDatum(RVFDatum<L, F> example)
		{
			return ClassOf(example.AsFeaturesCounter());
		}

		public virtual L ClassOf(ICounter<F> features)
		{
			if (ScoreOf(features) > 0)
			{
				return classes[1];
			}
			return classes[0];
		}

		public virtual L ClassOf(ICollection<F> features)
		{
			if (ScoreOf(features) > 0)
			{
				return classes[1];
			}
			return classes[0];
		}

		public virtual double ScoreOf(ICollection<F> features)
		{
			double sum = 0;
			foreach (F feature in features)
			{
				int f = featureIndex.IndexOf(feature);
				if (f >= 0)
				{
					sum += weights[f];
				}
			}
			return sum;
		}

		public virtual double ScoreOf(ICounter<F> features)
		{
			double sum = 0;
			foreach (F feature in features.KeySet())
			{
				int f = featureIndex.IndexOf(feature);
				if (f >= 0)
				{
					sum += weights[f] * features.GetCount(feature);
				}
			}
			return sum;
		}

		/*
		* returns the weights to each feature assigned by the classifier
		* nmramesh@cs.stanford.edu
		*/
		public virtual ICounter<F> JustificationOf(ICounter<F> features)
		{
			ICounter<F> fWts = new ClassicCounter<F>();
			foreach (F feature in features.KeySet())
			{
				int f = featureIndex.IndexOf(feature);
				if (f >= 0)
				{
					fWts.IncrementCount(feature, weights[f] * features.GetCount(feature));
				}
			}
			return fWts;
		}

		/// <summary>returns the weights assigned by the classifier to each feature</summary>
		public virtual ICounter<F> JustificationOf(ICollection<F> features)
		{
			ICounter<F> fWts = new ClassicCounter<F>();
			foreach (F feature in features)
			{
				int f = featureIndex.IndexOf(feature);
				if (f >= 0)
				{
					fWts.IncrementCount(feature, weights[f]);
				}
			}
			return fWts;
		}

		/// <summary>returns the scores for both the classes</summary>
		public virtual ICounter<L> ScoresOf(IDatum<L, F> datum)
		{
			if (datum is RVFDatum<object, object>)
			{
				return ScoresOfRVFDatum((RVFDatum<L, F>)datum);
			}
			ICollection<F> features = datum.AsFeatures();
			double sum = ScoreOf(features);
			ICounter<L> c = new ClassicCounter<L>();
			c.SetCount(classes[0], -sum);
			c.SetCount(classes[1], sum);
			return c;
		}

		[Obsolete]
		public virtual ICounter<L> ScoresOf(RVFDatum<L, F> example)
		{
			//use scoresOfDatum(Datum) instead.
			return ScoresOfRVFDatum(example);
		}

		private ICounter<L> ScoresOfRVFDatum(RVFDatum<L, F> example)
		{
			ICounter<F> features = example.AsFeaturesCounter();
			double sum = ScoreOf(features);
			ICounter<L> c = new ClassicCounter<L>();
			c.SetCount(classes[0], -sum);
			c.SetCount(classes[1], sum);
			return c;
		}

		public virtual double ProbabilityOf(IDatum<L, F> example)
		{
			if (example is RVFDatum<object, object>)
			{
				return ProbabilityOfRVFDatum((RVFDatum<L, F>)example);
			}
			return ProbabilityOf(example.AsFeatures(), example.Label());
		}

		public virtual double ProbabilityOf(ICollection<F> features, L label)
		{
			short sign = (short)(label.Equals(classes[0]) ? 1 : -1);
			return 1.0 / (1.0 + Math.Exp(sign * ScoreOf(features)));
		}

		public virtual double ProbabilityOf(RVFDatum<L, F> example)
		{
			return ProbabilityOfRVFDatum(example);
		}

		private double ProbabilityOfRVFDatum(RVFDatum<L, F> example)
		{
			return ProbabilityOf(example.AsFeaturesCounter(), example.Label());
		}

		public virtual double ProbabilityOf(ICounter<F> features, L label)
		{
			short sign = (short)(label.Equals(classes[0]) ? 1 : -1);
			return 1.0 / (1.0 + Math.Exp(sign * ScoreOf(features)));
		}

		/// <summary>Trains on weighted dataset.</summary>
		/// <param name="dataWeights">weights of the data.</param>
		[Obsolete]
		public virtual void TrainWeightedData(GeneralDataset<L, F> data, float[] dataWeights)
		{
			//Use LogisticClassifierFactory to train instead.
			if (data.labelIndex.Size() != 2)
			{
				throw new Exception("LogisticClassifier is only for binary classification!");
			}
			IMinimizer<IDiffFunction> minim;
			LogisticObjectiveFunction lof = null;
			if (data is Dataset<object, object>)
			{
				lof = new LogisticObjectiveFunction(data.NumFeatureTypes(), data.GetDataArray(), data.GetLabelsArray(), prior, dataWeights);
			}
			else
			{
				if (data is RVFDataset<object, object>)
				{
					lof = new LogisticObjectiveFunction(data.NumFeatureTypes(), data.GetDataArray(), data.GetValuesArray(), data.GetLabelsArray(), prior, dataWeights);
				}
			}
			minim = new QNMinimizer(lof);
			weights = minim.Minimize(lof, 1e-4, new double[data.NumFeatureTypes()]);
			featureIndex = data.featureIndex;
			classes[0] = data.labelIndex.Get(0);
			classes[1] = data.labelIndex.Get(1);
		}

		[Obsolete]
		public virtual void Train(GeneralDataset<L, F> data)
		{
			//Use LogisticClassifierFactory to train instead.
			Train(data, 0.0, 1e-4);
		}

		[Obsolete]
		public virtual void Train(GeneralDataset<L, F> data, double l1reg, double tol)
		{
			//Use LogisticClassifierFactory to train instead.
			if (data.labelIndex.Size() != 2)
			{
				throw new Exception("LogisticClassifier is only for binary classification!");
			}
			IMinimizer<IDiffFunction> minim;
			if (!biased)
			{
				LogisticObjectiveFunction lof = null;
				if (data is Dataset<object, object>)
				{
					lof = new LogisticObjectiveFunction(data.NumFeatureTypes(), data.GetDataArray(), data.GetLabelsArray(), prior);
				}
				else
				{
					if (data is RVFDataset<object, object>)
					{
						lof = new LogisticObjectiveFunction(data.NumFeatureTypes(), data.GetDataArray(), data.GetValuesArray(), data.GetLabelsArray(), prior);
					}
				}
				if (l1reg > 0.0)
				{
					minim = ReflectionLoading.LoadByReflection("edu.stanford.nlp.optimization.OWLQNMinimizer", l1reg);
				}
				else
				{
					minim = new QNMinimizer(lof);
				}
				weights = minim.Minimize(lof, tol, new double[data.NumFeatureTypes()]);
			}
			else
			{
				BiasedLogisticObjectiveFunction lof = new BiasedLogisticObjectiveFunction(data.NumFeatureTypes(), data.GetDataArray(), data.GetLabelsArray(), prior);
				if (l1reg > 0.0)
				{
					minim = ReflectionLoading.LoadByReflection("edu.stanford.nlp.optimization.OWLQNMinimizer", l1reg);
				}
				else
				{
					minim = new QNMinimizer(lof);
				}
				weights = minim.Minimize(lof, tol, new double[data.NumFeatureTypes()]);
			}
			featureIndex = data.featureIndex;
			classes[0] = data.labelIndex.Get(0);
			classes[1] = data.labelIndex.Get(1);
		}

		/// <summary>This runs a simple train and test regime.</summary>
		/// <remarks>
		/// This runs a simple train and test regime.
		/// The data file format is one item per line, space separated, with first the class label
		/// and then a bunch of (categorical) string features.
		/// </remarks>
		/// <param name="args">The arguments/flags are: -trainFile trainFile -testFile testFile [-l1reg num] [-biased]</param>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Properties prop = StringUtils.ArgsToProperties(args);
			double l1reg = double.Parse(prop.GetProperty("l1reg", "0.0"));
			Dataset<string, string> ds = new Dataset<string, string>();
			foreach (string line in ObjectBank.GetLineIterator(new File(prop.GetProperty("trainFile"))))
			{
				string[] bits = line.Split("\\s+");
				ICollection<string> f = new LinkedList<string>(Arrays.AsList(bits).SubList(1, bits.Length));
				string l = bits[0];
				ds.Add(f, l);
			}
			ds.SummaryStatistics();
			bool biased = prop.GetProperty("biased", "false").Equals("true");
			LogisticClassifierFactory<string, string> factory = new LogisticClassifierFactory<string, string>();
			Edu.Stanford.Nlp.Classify.LogisticClassifier<string, string> lc = factory.TrainClassifier(ds, l1reg, 1e-4, biased);
			foreach (string line_1 in ObjectBank.GetLineIterator(new File(prop.GetProperty("testFile"))))
			{
				string[] bits = line_1.Split("\\s+");
				ICollection<string> f = new LinkedList<string>(Arrays.AsList(bits).SubList(1, bits.Length));
				//String l = bits[0];
				string g = lc.ClassOf(f);
				double prob = lc.ProbabilityOf(f, g);
				System.Console.Out.Printf("%4.3f\t%s\t%s%n", prob, g, line_1);
			}
		}
	}
}
