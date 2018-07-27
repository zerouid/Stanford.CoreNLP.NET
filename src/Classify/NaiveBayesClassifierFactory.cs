// Stanford Classifier - a multiclass maxent classifier
// NaiveBayesClassifierFactory
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
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Classify
{
	/// <summary>Creates a NaiveBayesClassifier given an RVFDataset.</summary>
	/// <author>Kristina Toutanova (kristina@cs.stanford.edu)</author>
	[System.Serializable]
	public class NaiveBayesClassifierFactory<L, F> : IClassifierFactory<L, F, NaiveBayesClassifier<L, F>>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Classify.NaiveBayesClassifierFactory));

		private const long serialVersionUID = -8164165428834534041L;

		public const int Jl = 0;

		public const int Cl = 1;

		public const int Ucl = 2;

		private int kind = Jl;

		private double alphaClass;

		private double alphaFeature;

		private double sigma;

		private int prior = (int)(LogPrior.LogPriorType.Null);

		private IIndex<L> labelIndex;

		private IIndex<F> featureIndex;

		public NaiveBayesClassifierFactory()
		{
		}

		public NaiveBayesClassifierFactory(double alphaC, double alphaF, double sigma, int prior, int kind)
		{
			alphaClass = alphaC;
			alphaFeature = alphaF;
			this.sigma = sigma;
			this.prior = prior;
			this.kind = kind;
		}

		private NaiveBayesClassifier<L, F> TrainClassifier(int[][] data, int[] labels, int numFeatures, int numClasses, IIndex<L> labelIndex, IIndex<F> featureIndex)
		{
			ICollection<L> labelSet = Generics.NewHashSet();
			NaiveBayesClassifierFactory.NBWeights nbWeights = TrainWeights(data, labels, numFeatures, numClasses);
			ICounter<L> priors = new ClassicCounter<L>();
			double[] pr = nbWeights.priors;
			for (int i = 0; i < pr.Length; i++)
			{
				priors.IncrementCount(labelIndex.Get(i), pr[i]);
				labelSet.Add(labelIndex.Get(i));
			}
			ICounter<Pair<Pair<L, F>, Number>> weightsCounter = new ClassicCounter<Pair<Pair<L, F>, Number>>();
			double[][][] wts = nbWeights.weights;
			for (int c = 0; c < numClasses; c++)
			{
				L label = labelIndex.Get(c);
				for (int f = 0; f < numFeatures; f++)
				{
					F feature = featureIndex.Get(f);
					Pair<L, F> p = new Pair<L, F>(label, feature);
					for (int val = 0; val < wts[c][f].Length; val++)
					{
						Pair<Pair<L, F>, Number> key = new Pair<Pair<L, F>, Number>(p, int.Parse(val));
						weightsCounter.IncrementCount(key, wts[c][f][val]);
					}
				}
			}
			return new NaiveBayesClassifier<L, F>(weightsCounter, priors, labelSet);
		}

		/// <summary>The examples are assumed to be a list of RFVDatum.</summary>
		/// <remarks>
		/// The examples are assumed to be a list of RFVDatum.
		/// The datums are assumed to not contain the zeroes and then they are added to each instance.
		/// </remarks>
		public virtual NaiveBayesClassifier<L, F> TrainClassifier(GeneralDataset<L, F> examples, ICollection<F> featureSet)
		{
			int numFeatures = featureSet.Count;
			int[][] data = new int[][] {  };
			int[] labels = new int[examples.Size()];
			labelIndex = new HashIndex<L>();
			featureIndex = new HashIndex<F>();
			foreach (F feat in featureSet)
			{
				featureIndex.Add(feat);
			}
			for (int d = 0; d < examples.Size(); d++)
			{
				RVFDatum<L, F> datum = examples.GetRVFDatum(d);
				ICounter<F> c = datum.AsFeaturesCounter();
				foreach (F feature in c.KeySet())
				{
					int fNo = featureIndex.IndexOf(feature);
					int value = (int)c.GetCount(feature);
					data[d][fNo] = value;
				}
				labelIndex.Add(datum.Label());
				labels[d] = labelIndex.IndexOf(datum.Label());
			}
			int numClasses = labelIndex.Size();
			return TrainClassifier(data, labels, numFeatures, numClasses, labelIndex, featureIndex);
		}

		/// <summary>
		/// Here the data is assumed to be for every instance, array of length numFeatures
		/// and the value of the feature is stored including zeroes.
		/// </summary>
		/// <returns>
		/// 
		/// <literal>label,fno,value -&gt; weight</literal>
		/// </returns>
		private NaiveBayesClassifierFactory.NBWeights TrainWeights(int[][] data, int[] labels, int numFeatures, int numClasses)
		{
			if (kind == Jl)
			{
				return TrainWeightsJL(data, labels, numFeatures, numClasses);
			}
			if (kind == Ucl)
			{
				return TrainWeightsUCL(data, labels, numFeatures, numClasses);
			}
			if (kind == Cl)
			{
				return TrainWeightsCL(data, labels, numFeatures, numClasses);
			}
			return null;
		}

		private NaiveBayesClassifierFactory.NBWeights TrainWeightsJL(int[][] data, int[] labels, int numFeatures, int numClasses)
		{
			int[] numValues = NumberValues(data, numFeatures);
			double[] priors = new double[numClasses];
			double[][][] weights = new double[numClasses][][];
			//init weights array
			for (int cl = 0; cl < numClasses; cl++)
			{
				for (int fno = 0; fno < numFeatures; fno++)
				{
					weights[cl][fno] = new double[numValues[fno]];
				}
			}
			for (int i = 0; i < data.Length; i++)
			{
				priors[labels[i]]++;
				for (int fno = 0; fno < numFeatures; fno++)
				{
					weights[labels[i]][fno][data[i][fno]]++;
				}
			}
			for (int cl_1 = 0; cl_1 < numClasses; cl_1++)
			{
				for (int fno = 0; fno < numFeatures; fno++)
				{
					for (int val = 0; val < numValues[fno]; val++)
					{
						weights[cl_1][fno][val] = Math.Log((weights[cl_1][fno][val] + alphaFeature) / (priors[cl_1] + alphaFeature * numValues[fno]));
					}
				}
				priors[cl_1] = Math.Log((priors[cl_1] + alphaClass) / (data.Length + alphaClass * numClasses));
			}
			return new NaiveBayesClassifierFactory.NBWeights(priors, weights);
		}

		private NaiveBayesClassifierFactory.NBWeights TrainWeightsUCL(int[][] data, int[] labels, int numFeatures, int numClasses)
		{
			int[] numValues = NumberValues(data, numFeatures);
			int[] sumValues = new int[numFeatures];
			//how many feature-values are before this feature
			for (int j = 1; j < numFeatures; j++)
			{
				sumValues[j] = sumValues[j - 1] + numValues[j - 1];
			}
			int[][] newdata = new int[data.Length][];
			for (int i = 0; i < data.Length; i++)
			{
				newdata[i][0] = 0;
				for (int j_1 = 0; j_1 < numFeatures; j_1++)
				{
					newdata[i][j_1 + 1] = sumValues[j_1] + data[i][j_1] + 1;
				}
			}
			int totalFeatures = sumValues[numFeatures - 1] + numValues[numFeatures - 1] + 1;
			logger.Info("total feats " + totalFeatures);
			LogConditionalObjectiveFunction<L, F> objective = new LogConditionalObjectiveFunction<L, F>(totalFeatures, numClasses, newdata, labels, prior, sigma, 0.0);
			IMinimizer<IDiffFunction> min = new QNMinimizer();
			double[] argmin = min.Minimize(objective, 1e-4, objective.Initial());
			double[][] wts = objective.To2D(argmin);
			System.Console.Out.WriteLine("weights have dimension " + wts.Length);
			return new NaiveBayesClassifierFactory.NBWeights(wts, numValues);
		}

		private NaiveBayesClassifierFactory.NBWeights TrainWeightsCL(int[][] data, int[] labels, int numFeatures, int numClasses)
		{
			LogConditionalEqConstraintFunction objective = new LogConditionalEqConstraintFunction(numFeatures, numClasses, data, labels, prior, sigma, 0.0);
			IMinimizer<IDiffFunction> min = new QNMinimizer();
			double[] argmin = min.Minimize(objective, 1e-4, objective.Initial());
			double[][][] wts = objective.To3D(argmin);
			double[] priors = objective.Priors(argmin);
			return new NaiveBayesClassifierFactory.NBWeights(priors, wts);
		}

		internal static int[] NumberValues(int[][] data, int numFeatures)
		{
			int[] numValues = new int[numFeatures];
			foreach (int[] row in data)
			{
				for (int j = 0; j < row.Length; j++)
				{
					if (numValues[j] < row[j] + 1)
					{
						numValues[j] = row[j] + 1;
					}
				}
			}
			return numValues;
		}

		internal class NBWeights
		{
			internal double[] priors;

			internal double[][][] weights;

			internal NBWeights(double[] priors, double[][][] weights)
			{
				this.priors = priors;
				this.weights = weights;
			}

			/// <summary>
			/// create the parameters from a coded representation
			/// where feature 0 is the prior etc.
			/// </summary>
			internal NBWeights(double[][] wts, int[] numValues)
			{
				int numClasses = wts[0].Length;
				priors = new double[numClasses];
				lock (typeof(Runtime))
				{
					System.Array.Copy(wts[0], 0, priors, 0, numClasses);
				}
				int[] sumValues = new int[numValues.Length];
				for (int j = 1; j < numValues.Length; j++)
				{
					sumValues[j] = sumValues[j - 1] + numValues[j - 1];
				}
				weights = new double[priors.Length][][];
				for (int fno = 0; fno < numValues.Length; fno++)
				{
					for (int c = 0; c < numClasses; c++)
					{
						weights[c][fno] = new double[numValues[fno]];
					}
					for (int val = 0; val < numValues[fno]; val++)
					{
						int code = sumValues[fno] + val + 1;
						for (int cls = 0; cls < numClasses; cls++)
						{
							weights[cls][fno][val] = wts[code][cls];
						}
					}
				}
			}
		}

		//  public static void main(String[] args) {
		//    List examples = new ArrayList();
		//    String leftLight = "leftLight";
		//    String rightLight = "rightLight";
		//    String broken = "BROKEN";
		//    String ok = "OK";
		//    Counter c1 = new ClassicCounter<>();
		//    c1.incrementCount(leftLight, 0);
		//    c1.incrementCount(rightLight, 0);
		//    RVFDatum d1 = new RVFDatum(c1, broken);
		//    examples.add(d1);
		//    Counter c2 = new ClassicCounter<>();
		//    c2.incrementCount(leftLight, 1);
		//    c2.incrementCount(rightLight, 1);
		//    RVFDatum d2 = new RVFDatum(c2, ok);
		//    examples.add(d2);
		//    Counter c3 = new ClassicCounter<>();
		//    c3.incrementCount(leftLight, 0);
		//    c3.incrementCount(rightLight, 1);
		//    RVFDatum d3 = new RVFDatum(c3, ok);
		//    examples.add(d3);
		//    Counter c4 = new ClassicCounter<>();
		//    c4.incrementCount(leftLight, 1);
		//    c4.incrementCount(rightLight, 0);
		//    RVFDatum d4 = new RVFDatum(c4, ok);
		//    examples.add(d4);
		//    Dataset data = new Dataset(examples.size());
		//    data.addAll(examples);
		//    NaiveBayesClassifier classifier = (NaiveBayesClassifier)
		//        new NaiveBayesClassifierFactory(200, 200, 1.0,
		//              LogPrior.LogPriorType.QUADRATIC.ordinal(),
		//              NaiveBayesClassifierFactory.CL)
		//            .trainClassifier(data);
		//    classifier.print();
		//    //now classifiy
		//    for (int i = 0; i < examples.size(); i++) {
		//      RVFDatum d = (RVFDatum) examples.get(i);
		//      Counter scores = classifier.scoresOf(d);
		//      System.out.println("for datum " + d + " scores are " + scores.toString());
		//      System.out.println(" class is " + Counters.topKeys(scores, 1));
		//      System.out.println(" class should be " + d.label());
		//    }
		//  }
		//    String trainFile = args[0];
		//    String testFile = args[1];
		//    NominalDataReader nR = new NominalDataReader();
		//    Map<Integer, Index<String>> indices = Generics.newHashMap();
		//    List<RVFDatum<String, Integer>> train = nR.readData(trainFile, indices);
		//    List<RVFDatum<String, Integer>> test = nR.readData(testFile, indices);
		//    System.out.println("Constrained conditional likelihood no prior :");
		//    for (int j = 0; j < 100; j++) {
		//      NaiveBayesClassifier<String, Integer> classifier = new NaiveBayesClassifierFactory<String, Integer>(0.1, 0.01, 0.6, LogPrior.LogPriorType.NULL.ordinal(), NaiveBayesClassifierFactory.CL).trainClassifier(train);
		//      classifier.print();
		//      //now classifiy
		//
		//      float accTrain = classifier.accuracy(train.iterator());
		//      log.info("training accuracy " + accTrain);
		//      float accTest = classifier.accuracy(test.iterator());
		//      log.info("test accuracy " + accTest);
		//
		//    }
		//    System.out.println("Unconstrained conditional likelihood no prior :");
		//    for (int j = 0; j < 100; j++) {
		//      NaiveBayesClassifier<String, Integer> classifier = new NaiveBayesClassifierFactory<String, Integer>(0.1, 0.01, 0.6, LogPrior.LogPriorType.NULL.ordinal(), NaiveBayesClassifierFactory.UCL).trainClassifier(train);
		//      classifier.print();
		//      //now classify
		//
		//      float accTrain = classifier.accuracy(train.iterator());
		//      log.info("training accuracy " + accTrain);
		//      float accTest = classifier.accuracy(test.iterator());
		//      log.info("test accuracy " + accTest);
		//    }
		//  }
		public virtual NaiveBayesClassifier<L, F> TrainClassifier(GeneralDataset<L, F> dataset)
		{
			if (dataset is RVFDataset)
			{
				throw new Exception("Not sure if RVFDataset runs correctly in this method. Please update this code if it does.");
			}
			return TrainClassifier(dataset.GetDataArray(), dataset.labels, dataset.NumFeatures(), dataset.NumClasses(), dataset.labelIndex, dataset.featureIndex);
		}
	}
}
